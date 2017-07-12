/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Integrated Development Environment
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.IO;
using SolutionMan;
using System.Globalization;
using LimnorCompiler;
using LimnorDesigner;
using System.Xml;
using XmlUtility;
using System.Media;
using VSPrj;
using TraceLog;

namespace LimnorVOB
{
	public partial class FormBatchBuilder : Form, ICompilerLog
	{
		#region field sna dconstructors
		private LimnorVOBMain _main;
		private BuildResult _generic;
		private BuildItemResult _currentProject;
		private bool _cancel;
		public FormBatchBuilder()
		{
			InitializeComponent();
#if DOTNET40
			txtBatch.Text = @"C:\Samples\batch40.txt";
#else
			txtBatch.Text = @"C:\Samples\batch35.txt";
#endif
		}
		#endregion
		#region Methods
		public void SetVOBMain(LimnorVOBMain main)
		{
			_main = main;
		}
		#endregion
		#region event handlers
		private void buttonCancel_Click(object sender, EventArgs e)
		{
			_cancel = true;
		}
		private void buttonDir_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				textBoxRootDir.Text = dlg.SelectedPath;
			}
		}
		private void buttonStart_Click(object sender, EventArgs e)
		{
			buttonStart.Enabled = false;
			buttonCancel.Enabled = true;
			_cancel = false;
			_generic = new BuildResult("Batch Builder");
			bool b = TraceLogClass.TraceLog.ShowMessageBox;
			TraceLogClass.TraceLog.ShowMessageBox = false;
			try
			{
				string bf = txtBatch.Text.Trim();
				treeView1.Nodes.Clear();
				treeView1.Nodes.Add(_generic.Node);
				if (bf.Length > 0)
				{
					textBoxRootDir.ReadOnly = true;
					StreamReader sr = new StreamReader(bf);
					while (!sr.EndOfStream)
					{
						string line = sr.ReadLine();
						if (!string.IsNullOrEmpty(line))
						{
							textBoxRootDir.Text = line;
							textBoxRootDir.Refresh();
							processFolder(line);
						}
					}
					sr.Close();
				}
				else
				{
					processFolder(textBoxRootDir.Text);
				}
			}
			catch (Exception err)
			{
				_generic.SetError(err.Message);
			}
			finally
			{
				TraceLogClass.TraceLog.ShowMessageBox = b;
			}
			_generic.End();

			for (int i = 1; i < 8; i++)
			{
				SystemSounds.Exclamation.Play();
			}
			MessageBox.Show("Finished");
			textBoxRootDir.ReadOnly = false;
			buttonStart.Enabled = true;
			buttonCancel.Enabled = false;
		}
		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e != null && e.Node != null)
			{
				BuildItemResult br = e.Node.Tag as BuildItemResult;
				if (br != null)
				{
					listBox1.Items.Clear();
					foreach (string s in br.Errors)
					{
						listBox1.Items.Add(s);
					}
					foreach (string s in br.Messages)
					{
						listBox1.Items.Add(s);
					}
				}
			}
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			textBoxMsg.Text = listBox1.Text;
		}
		#endregion
		#region private methods
		private void showInfo(string msg, params object[] values)
		{
			if (values != null && values.Length > 0)
			{
				msg = string.Format(CultureInfo.InvariantCulture, msg, values);
			}
			lblInfo.Text = msg;
			lblInfo.Refresh();
		}
		private void buildProject(ProjectNode pn, string cfg, BuildSolutionResult sln)
		{
			if (_cancel)
				return;
			LimnorXmlCompiler builder = null;
			FrmObjectExplorer.RemoveDialogCaches(Guid.Empty, 0);
			showInfo(pn.Project.ProjectFile);
			_currentProject = sln.AddProject(pn.Project.ProjectFile);
			ProjectNodeData pnd = pn.PropertyObject as ProjectNodeData;
			try
			{
				builder = new LimnorXmlCompiler(pnd.ProjectFile, this);
			}
			catch (Exception err2)
			{
				_currentProject.SetError("Error creating ILimnorBuilder from [LimnorXmlCompiler]. {1} ", err2.Message);
				builder = null;
			}
			if (builder != null)
			{
				builder.SetProject(pnd.Project, cfg);
				if (builder.Execute())
				{
				}
				else
				{
					_currentProject.SetError("Finished building [{0}]. Failed ===============", pnd.Project.ProjectFile);
				}
			}
			_currentProject.End();
			Application.DoEvents();
		}
		private void buildSolution(string slnFile)
		{
			if (_cancel)
				return;
			showInfo("Build solution starts for {0}", slnFile);
			LimnorSolution.ResetSolution();
			BuildSolutionResult sln = _generic.AddSolution(slnFile);
			treeView1.Nodes.Add(sln.Node);
			SolutionNode slnNode = _main.LoadSolution(slnFile, false);
			if (slnNode != null)
			{
				ProjectNode[] projects = slnNode.GetProjectBuilderOrder();
				if (projects.Length > 0)
				{
					string cfg = "Release";
					StringCollection _compilerErrors = new StringCollection();
					for (int i = 0; i < projects.Length; i++)
					{
						if (slnNode.IsProjectBuildIncluded(projects[i]))
						{
							buildProject(projects[i], cfg, sln);
						}
						if (_cancel)
							break;
					}
					if (_compilerErrors.Count > 0)
					{
						sln.AppendErrors(_compilerErrors);
					}
				}
			}
			else
			{
				sln.SetError("Solution cannot be loaded");
			}
			sln.End();
			Application.DoEvents();
		}
		private void processFolder(string folder)
		{
			if (_cancel)
				return;
			string buildOrder = Path.Combine(folder, "build_order.xml");
			if (File.Exists(buildOrder))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(buildOrder);
				if (doc.DocumentElement != null)
				{
					XmlNodeList nds = doc.DocumentElement.SelectNodes(XmlTags.XML_Item);
					for (int i = 0; i < nds.Count; i++)
					{
						string slnFolder = XmlUtil.GetAttribute(nds[i], "folder");
						string dir = Path.Combine(folder, slnFolder);
						string file = XmlUtil.GetAttribute(nds[i], "file");
						file = Path.Combine(dir, file);
						buildSolution(file);
						if (_cancel)
							break;
					}
				}
			}
			else
			{
				string[] slnFiles = Directory.GetFiles(folder, "*.LimnorMain_sln");
				if (slnFiles != null && slnFiles.Length > 0)
				{
					for (int i = 0; i < slnFiles.Length; i++)
					{
						buildSolution(slnFiles[i]);
						if (_cancel)
							break;
					}
				}
				string[] subFolders = Directory.GetDirectories(folder);
				if (subFolders != null && subFolders.Length > 0)
				{
					for (int i = 0; i < subFolders.Length; i++)
					{
						processFolder(subFolders[i]);
						if (_cancel)
							break;
					}
				}
			}
		}
		#endregion

		class BuildItemResult
		{
			private StringCollection _errors;
			private StringCollection _messages;
			private TreeNode _treeNode;
			private string _name;
			private DateTime _start;
			private DateTime _end;
			public BuildItemResult(string name)
			{
				_errors = new StringCollection();
				_messages = new StringCollection();
				_treeNode = new TreeNode(name);
				_treeNode.Tag = this;
				_name = name;
				_start = DateTime.Now;
				AppendMessage("Start {0}", _start.ToString("yyyy-MM-dd hh:mm:ss.fff", CultureInfo.InvariantCulture));
			}
			public TreeNode Node { get { return _treeNode; } }
			public bool Failed
			{
				get;
				set;
			}
			public virtual bool OK
			{
				get
				{
					if (Errors.Count > 0)
						return false;
					return !Failed;
				}
			}
			public string Name
			{
				get { return _name; }
			}
			public StringCollection Errors { get { return _errors; } }
			public StringCollection Messages { get { return _messages; } }
			public void AppendMessage(string msg, params object[] values)
			{
				if (values != null && values.Length > 0)
				{
					msg = string.Format(CultureInfo.InvariantCulture, msg, values);
				}
				_messages.Add(msg);
			}
			public void SetError(string err, params object[] values)
			{
				Failed = true;
				if (values != null && values.Length > 0)
				{
					err = string.Format(CultureInfo.InvariantCulture, err, values);
				}
				_errors.Add(err);
			}
			public void AppendErrors(StringCollection errs)
			{
				foreach (string s in errs)
				{
					_errors.Add(s);
				}
			}
			public virtual void End()
			{
				_end = DateTime.Now;
				AppendMessage("End {0}", _end.ToString("yyyy-MM-dd hh:mm:ss.fff", CultureInfo.InvariantCulture));
				TimeSpan ts = _end.Subtract(_start);
				AppendMessage("Time used: {0} hours {1} minutes {2} seconds", ts.Hours, ts.Minutes, ts.Seconds);
				ShowImage();
			}
			public void ShowImage()
			{
				if (!OK || _errors.Count > 0)
				{
					_treeNode.ImageIndex = 1;
				}
				else
				{
					_treeNode.ImageIndex = 2;
				}
				_treeNode.SelectedImageIndex = _treeNode.ImageIndex;
			}
		}
		class BuildResult : BuildItemResult
		{
			private List<BuildSolutionResult> _solutions;
			public BuildResult(string name)
				: base(name)
			{
				_solutions = new List<BuildSolutionResult>();
			}

			public override bool OK
			{
				get
				{
					if (Failed)
						return false;
					if (Errors.Count > 0)
						return false;
					foreach (BuildSolutionResult s in _solutions)
					{
						if (s.Failed)
							return false;
						if (s.Errors.Count > 0)
							return false;
					}
					return true;
				}
			}
			public override void End()
			{
				AppendMessage("Solutions: {0}", _solutions.Count);
				base.End();
				List<TreeNode> failed = new List<TreeNode>();
				foreach (BuildSolutionResult s in _solutions)
				{
					if (!s.OK)
					{
						failed.Add(s.Node);
						this.Node.TreeView.Nodes.Remove(s.Node);
					}
				}
				foreach (TreeNode n in failed)
				{
					this.Node.TreeView.Nodes.Insert(1, n);
				}
			}
			public BuildSolutionResult AddSolution(string name)
			{
				BuildSolutionResult s = new BuildSolutionResult(name);
				_solutions.Add(s);
				return s;
			}
			public IList<BuildSolutionResult> Solutions
			{
				get
				{
					return _solutions;
				}
			}
		}
		class BuildSolutionResult : BuildItemResult
		{
			private List<BuildItemResult> _projects;
			public BuildSolutionResult(string name)
				: base(name)
			{
				_projects = new List<BuildItemResult>();
			}
			public IList<BuildItemResult> Projects { get { return _projects; } }
			public BuildItemResult AddProject(string name)
			{
				BuildItemResult p = new BuildItemResult(name);
				_projects.Add(p);
				this.Node.Nodes.Add(p.Node);
				return p;
			}
			public override void End()
			{
				AppendMessage("Project: {0}", _projects.Count);
				base.End();
			}
			public override bool OK
			{
				get
				{
					if (base.Failed)
						return false;
					if (Errors.Count > 0)
						return false;
					foreach (BuildItemResult s in _projects)
					{
						if (s.Failed)
							return false;
						if (s.Errors.Count > 0)
							return false;
					}
					return true;
				}
			}
		}

		#region ICompilerLog Members

		public void LogError(string msg)
		{
			_currentProject.SetError(msg);
		}

		public void LogWarning(string msg, params object[] values)
		{
			_currentProject.SetError(msg, values);
		}

		public bool HasLoggedErrors
		{
			get { return _currentProject.Failed; }
		}

		public void LogErrorFromException(Exception e)
		{
			_currentProject.SetError(DesignerException.FormExceptionText(e));
		}

		#endregion

		private void btBatchFile_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				txtBatch.Text = dlg.SelectedPath;
			}
		}
	}
}
