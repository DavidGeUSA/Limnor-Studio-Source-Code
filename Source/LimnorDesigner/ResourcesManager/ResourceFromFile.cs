/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace LimnorDesigner.ResourcesManager
{
	public abstract class ResourceFromFile : ResourcePointer
	{
		#region fields and constructors
		public ResourceFromFile()
		{
		}
		public ResourceFromFile(ProjectResources owner)
			: base(owner)
		{
		}
		#endregion
		#region Method
		public override bool SelectResourceFile(Form caller, string languageName)
		{
			try
			{
				OpenFileDialog dlg = new OpenFileDialog();
				dlg.Title = FileSelectionTitle;
				dlg.FileName = GetExistFilename(languageName);
				dlg.Filter = FileSelectionFilter;
				if (dlg.ShowDialog(caller) == DialogResult.OK)
				{
					if (Manager.CopyResourceFile(caller, dlg.FileName))
					{
						SetResourceString(languageName, System.IO.Path.GetFileName(dlg.FileName));
					}
					else
					{
						SetResourceString(languageName, dlg.FileName);
					}
					IsChanged = true;
					return OnFileSelected(dlg.FileName, languageName);
				}
			}
			catch (Exception err)
			{
				MessageBox.Show(caller, err.Message, "Select file", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			return false;
		}
		public string GetExistFilename(string languagename)
		{
			string f = GetResourceString(languagename);
			if (string.IsNullOrEmpty(f))
			{
				f = GetResourceString(string.Empty);
			}
			if (!string.IsNullOrEmpty(f))
			{
				string folder = System.IO.Path.GetDirectoryName(f);
				if (string.IsNullOrEmpty(folder))
				{
					string f0 = System.IO.Path.Combine(Manager.ResourcesFolder, f);
					if (System.IO.File.Exists(f0))
					{
						return f0;
					}
				}
				if (System.IO.File.Exists(f))
				{
					return f;
				}
			}
			return string.Empty;
		}
		public override void OnSelected(TextBoxResEditor textBoxDefault, TextBoxResEditor textBoxLocal, PictureBoxResEditor pictureBoxDefault, PictureBoxResEditor pictureBoxLocal, CultureInfo c)
		{
			pictureBoxDefault.SetResourceOwner(this, null);
			pictureBoxDefault.Visible = true;
			pictureBoxDefault.SetReadOnly(c != null);
			if (c == null)
			{
				//for default, disable local
				pictureBoxLocal.SetResourceOwner(null, null);
			}
			else
			{
				//for local
				pictureBoxLocal.SetResourceOwner(this, c);
			}
			pictureBoxLocal.Visible = true;
			pictureBoxLocal.SetReadOnly(c == null);

			textBoxDefault.Visible = false;
			textBoxLocal.Visible = false;
		}
		#endregion

		#region Properties
		public override bool IsFile { get { return true; } }
		protected abstract string FileSelectionTitle { get; }
		protected abstract string FileSelectionFilter { get; }
		protected abstract bool OnFileSelected(string selectedFile, string languagename);
		/// <summary>
		/// if file does not exist then returns empty
		/// </summary>
		public string CurrentExistFilename
		{
			get
			{
				string f = CurrentResourceString;
				if (!string.IsNullOrEmpty(f))
				{
					if (System.IO.File.Exists(f))
					{
						return f;
					}
				}
				return string.Empty;
			}
		}
		public override string ValueDisplay
		{
			get
			{
				string s = GetResourceString(string.Empty);
				if (string.IsNullOrEmpty(s))
				{
					return string.Empty;
				}
				return System.IO.Path.GetFileName(s);
			}
		}

		#endregion
	}
}
