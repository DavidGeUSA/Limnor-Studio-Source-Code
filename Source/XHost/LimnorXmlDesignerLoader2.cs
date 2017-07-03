/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Designer Host
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design.Serialization;
using LimnorDesigner;
using VSPrj;
using XmlSerializer;
using System.ComponentModel;
using System.Xml;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.IO;
using XmlUtility;
using System.Diagnostics;
using LimnorDesigner.Property;
using VPL;
using MathExp;
using Limnor.Drawing2D;
using LimnorDatabase;
using System.Runtime.InteropServices;
using System.Drawing;
using LimnorDesigner.Interface;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using LimnorDesigner.Event;
using VOB;
using Limnor.CopyProtection;
using Limnor.WebBuilder;
using Limnor.WebServerBuilder;
using LimnorDesigner.MenuUtil;
using LimnorDesigner.Web;
using System.Globalization;
using TraceLog;

namespace XHost
{
	public class LimnorXmlDesignerLoader2 : BasicDesignerLoader, ILimnorDesignerLoader, ILimnorLoader, IChangeControl, IPropertyListChangeNotify
	{
		#region members
		const string LS = "\r\n";
		private XmlObjectWriter _writer;
		private ObjectIDmap _objMap;
		private IComponent _rootComponent;
		private ClassPointer _rootPointer;
		private ClassData _classData;
		private bool _isDisposed;
		private bool _verified;
		private bool _isFlushing;
		private object _dummy;
		private bool _isSetup;
		private bool _errorSaving;
		private bool loadingDesigner;
		private Dictionary<DataGridView, Dictionary<UInt32, string>> _columns;
		private const string hostedBaseClassName = "LimnorFormDesigner";
		public string DocumentMoniker { get { return _classData.ComponentFile; } }

		public static Point MenuPoint;

		public UInt32 ClassID { get { return _classData.ComponentId; } }

		public XmlNode Node { get { return _classData.ComponentXmlNode; } }

		public LimnorXmlPane2 ViewerHolder { get; set; }

		//
		public bool IsSetup
		{
			get
			{
				return _isSetup;
			}
		}
		public bool IsWebApp
		{
			get
			{
				return (_rootComponent is LimnorWebApp);
			}
		}
		public bool IsWebPage
		{
			get
			{
				return (_rootComponent is IWebPage);
			}
		}
		public object RootObject
		{
			get
			{
				return _rootComponent;
			}
		}
		public bool IsControl
		{
			get
			{
				return (_rootComponent is Control);
			}
		}
		public ILimnorDesignPane DesignPane
		{
			get
			{
				return ViewerHolder;
			}
		}
		public object DesignerObject
		{
			get
			{
				return ViewerHolder;
			}
		}
		public bool CanBeChild
		{
			get
			{
				if (_rootComponent is LimnorApp)
					return false;
				if (_rootComponent is Form)
					return false;
				return true;
			}
		}
		public string ObjectName
		{
			get
			{
				if (_rootComponent != null && _rootComponent.Site != null)
				{
					return _rootComponent.Site.Name;
				}
				return null;
			}
		}
		private bool _inDesign = true;
		public bool IsInDesign
		{
			get
			{
				return _inDesign;
			}
		}
		public string Namespace
		{
			get
			{
				return Project.Namespace;
			}
		}
		public LimnorProject Project
		{
			get
			{
				return _classData.Project;
			}
		}

		public ObjectIDmap ObjectMap
		{
			get
			{
				initialize();
				return _objMap;
			}
		}

		public IDesignerLoaderHost Host
		{
			get
			{
				try
				{
					return this.LoaderHost;
				}
				catch
				{
					return null;
				}
			}
		}

		#endregion

		#region construction and dispose
		/// <summary>
		/// Holds a reference for every designer loader opened. This map is indexed by itemId.
		/// </summary>
		private static SortedDictionary<uint, LimnorXmlDesignerLoader2> designerLoaders = new SortedDictionary<uint, LimnorXmlDesignerLoader2>();
		/// <summary>
		/// Represents the ItemID of the active designer. It is set by the SelectionEventsMonitor
		/// </summary>
		/// <value>The active designer's itemID.</value>
		public static uint ActiveItemID { get; set; }
		public static ILimnorDesignerLoader GetLoaderByClassId(UInt32 classId)
		{
			foreach (KeyValuePair<UInt32, LimnorXmlDesignerLoader2> kv in designerLoaders)
			{
				if (kv.Value.ClassID == classId)
				{
					return kv.Value;
				}
			}
			return null;
		}
		public static void RemoveLoader(UInt32 classId)
		{
			if (designerLoaders.ContainsKey(classId))
			{
				designerLoaders.Remove(classId);
			}
		}
		static LimnorXmlDesignerLoader2()
		{
			DesignUtil.GetLoaderByClassId = GetLoaderByClassId;
			LimnorProject.DatabaseListChanged = onDatabaseListChanged;
		}
		/// <summary>
		/// Gets the active designer loader.
		/// </summary>
		/// <value>The active designer loader.</value>
		public static LimnorXmlDesignerLoader2 ActiveDesignerLoader
		{
			get
			{
				if (designerLoaders.ContainsKey(ActiveItemID))
					return designerLoaders[ActiveItemID];
				return null;
			}
		}

		public LimnorXmlDesignerLoader2(ClassData data)
		{
			_classData = data;
			FormLoadProgress.ShowLoadProgress();
			FormLoadProgress.SetProgInfo("Loader starting...");
			LimnorProject.SetActiveProject(_classData.Project);
			loadingDesigner = false;
			_classData.ComponentXmlNode.OwnerDocument.PreserveWhitespace = false;
			XmlAttribute xa = _classData.ComponentXmlNode.OwnerDocument.CreateAttribute(XmlTags.XMLATT_filename);
			Node.Attributes.Append(xa);

			_isSetup = XmlUtil.GetAttributeBoolDefFalse(Node, XmlTags.XMLATT_isSetup);
			if (_isSetup)
			{
			}
			else
			{
				xa.Value = _classData.ComponentFile;
				initialize();
				if (designerLoaders.Remove(ClassID))
				{
					Trace.WriteLine(String.Format("A designer loader with the id {0} already existsed and has been removed.", ClassID));
				}
				designerLoaders.Add(ClassID, this);
				ActiveItemID = ClassID;
				Writer.CleanupBinaryResouces();
				FormLoadProgress.SetProgInfo("Loader constructed...");
			}
		}


		public override void Dispose()
		{
			try
			{
				_isDisposed = true;
				designerLoaders.Remove(this.ClassID);
				if (_objMap != null)
				{
					_objMap.Dispose();
					_objMap = null;
					_rootComponent = null;
					_rootPointer = null;
				}
			}
			finally
			{
				base.Dispose();
			}
		}
		#endregion

		#region Changes in Component
		public void AddComponentChangeEventHandler()
		{
			IComponentChangeService componentChangeService = (IComponentChangeService)Host.GetService(typeof(IComponentChangeService));
			componentChangeService.ComponentRename += new ComponentRenameEventHandler(componentChangeService_ComponentRename);
			componentChangeService.ComponentRemoving += new ComponentEventHandler(componentChangeService_ComponentRemoving);
			componentChangeService.ComponentRemoved += new ComponentEventHandler(componentChangeService_ComponentRemoved);
			componentChangeService.ComponentChanged += new ComponentChangedEventHandler(componentChangeService_ComponentChanged);
			componentChangeService.ComponentAdded += new ComponentEventHandler(componentChangeService_ComponentAdded);
			componentChangeService.ComponentAdding += new ComponentEventHandler(componentChangeService_ComponentAdding);

			this.Host.Activated += new EventHandler(Host_Activated);
			//
			XPropertyGrid pg = Host.GetService(typeof(PropertyGrid)) as XPropertyGrid;
			if (pg != null)
			{
				pg.LeaveItems += new EventHandler(pg_LeaveItems);
			}
		}

		void pg_LeaveItems(object sender, EventArgs e)
		{
			this.Modified = true;
			doFlush();
		}
		void onCustomPropertyValueChange(object sender, EventArgs e)
		{
			PropertyClass p = sender as PropertyClass;
			if (p != null)
			{
				NotifyChanges();
			}
		}//
		private string getComponentDisplay(ComponentRenameEventArgs e)
		{
			return getComponentDisplay(e.Component as IComponent);
		}
		private string getComponentDisplay(ComponentChangedEventArgs e)
		{
			if (e.Member != null)
				return getComponentDisplay(e.Component as IComponent) + ":" + e.Member.DisplayName;
			return getComponentDisplay(e.Component as IComponent) + ":null";
		}
		private string getComponentDisplay(ComponentEventArgs e)
		{
			return getComponentDisplay(e.Component);
		}
		internal static string getComponentDisplay(IComponent c)
		{
			StringBuilder sb = new StringBuilder();
			if (c == null)
			{
				sb.Append("Unknow component");
			}
			else
			{
				sb.Append(c.GetType().Name);
				sb.Append(" ");
				if (c.Site == null)
				{
					sb.Append("Not sited");
				}
				else
				{
					if (string.IsNullOrEmpty(c.Site.Name))
					{
						sb.Append("no name");
					}
					else
					{
						sb.Append(c.Site.Name);
					}
				}
			}
			return sb.ToString();
		}
		void componentChangeService_ComponentAdding(object sender, ComponentEventArgs e)
		{
			if (_isDisposed)
				return;
			try
			{
#if DEBUGX
				FormStringList.ShowDebugMessage("Start: adding component {0}", getComponentDisplay(e));
#endif
				if (ViewerHolder != null)
				{
					ViewerHolder.HideToolbox();
				}
				if (IsWebApp)
				{
					throw new ExceptionNormal("The component cannot be added to the web application.");
				}
				if (IsWebPage)
				{
					if (e.Component is IWebClientComponent)
					{
					}
					else
					{
						IWebClientControl wc = e.Component as IWebClientControl;
						if (wc == null)
						{
							if (e.Component is Control)
							{
								if (!(e.Component is TabPage))
								{
									throw new ExceptionNormal("The Form UI component cannot be added to a web page.");
								}
							}
							else
							{
								IWebServerProgrammingSupport wp = e.Component as IWebServerProgrammingSupport;
								if (wp == null)
								{
									if (this.Project.ProjectType == EnumProjectType.WebAppPhp)
									{
										throw new ExceptionNormal("This component cannot be used for PHP web applicaton.");
									}
								}
								else
								{
									if (this.Project.ProjectType == EnumProjectType.WebAppPhp)
									{
										if (!wp.IsWebServerProgrammingSupported(EnumWebServerProcessor.PHP))
										{
											throw new ExceptionNormal("This component cannot be used for PHP web applicaton.");
										}
									}
									else if (this.Project.ProjectType == EnumProjectType.WebAppAspx)
									{
										if (!wp.IsWebServerProgrammingSupported(EnumWebServerProcessor.Aspx))
										{
											throw new ExceptionNormal("This component is not supposed to be used for .Net Framework web applicaton.");
										}
									}
								}
							}
						}
					}
				}
				else
				{
					IWebClientControl wc = e.Component as IWebClientControl;
					if (wc != null)
					{
						throw new ExceptionNormal("A web client component can only be used in a web page.");
					}
					IWebServerProgrammingSupport wp = e.Component as IWebServerProgrammingSupport;
					if (wp != null)
					{
						bool supported = false;
						if (Project.ProjectType == EnumProjectType.WebAppPhp)
							supported = wp.IsWebServerProgrammingSupported(EnumWebServerProcessor.PHP);
						else
							supported = wp.IsWebServerProgrammingSupported(EnumWebServerProcessor.Aspx);
						if (!supported)
						{
							throw new ExceptionNormal("A web server component can only be used in a web page.");
						}
					}
				}
				if (ToolboxItemXType.SelectedToolboxClassId == 0)
				{
					ClassInstancePointer cr = e.Component as ClassInstancePointer;
					if (cr != null)
					{
						throw new ExceptionNormal("Error adding component. Newly added component may not work properly. You may delete it. Please try again using drag-drop operations.");
					}
				}
				ISingleton sl = e.Component as ISingleton;
				if (sl != null)
				{
					Type tsl = e.Component.GetType();
					foreach (object v in _objMap.Keys)
					{
						ISingleton sl0 = v as ISingleton;
						if (sl0 != null)
						{
							if (tsl.Equals(sl0.GetType()))
							{
								throw new ExceptionNormal("There is already an instance of [{0}] created. This kind of component allows only one instance.", tsl.FullName);
							}
						}
					}
				}
#if DEBUGX
				FormStringList.ShowDebugMessage("End: adding component {0}", getComponentDisplay(e));
#endif
			}
			catch (Exception err)
			{
#if DEBUGX
				FormStringList.ShowDebugMessage("error: adding component. {0}", DesignerException.FormExceptionText(err));
#else
				if (!(err is ExceptionNormal))
				{
					MathNode.Log(TraceLogClass.MainForm, err);
				}
#endif
				throw;
			}
		}
		IComponent _componentRemoving;
		void componentChangeService_ComponentRemoving(object sender, ComponentEventArgs e)
		{
			if (_isDisposed)
				return;
#if DEBUGX
			FormStringList.ShowDebugMessage("Start: removing component {0}", getComponentDisplay(e));
#endif
			if (e.Component is ToolboxItemXType)
			{
				return;
			}
			if (e.Component is ClassInstancePointer)
			{
				return;
			}
			if (_deletingImage || ViewerHolder.IsAtImageProperty)
			{
				if (ViewerHolder.IsAtImageProperty)
					_deletingImage = true;
				else
				{
					TimeSpan ts = DateTime.Now.Subtract(_dtDelImage);
					if (ts.TotalMilliseconds > 1000)
					{
						_deletingImage = false;
					}
				}
				if (_deletingImage)
				{
					//throw new ExceptionIgnore();
					return;
				}
			}
			IDrawDesignControl ddc = e.Component as IDrawDesignControl;
			if (ddc != null)
			{
				Control c = ddc as Control;
				IDrawDesignControl cp = c.Parent as IDrawDesignControl;
				if (cp != null)
				{
					DrawGroupBox dgb = cp.Item as DrawGroupBox;
					if (dgb != null)
					{
						dgb.OnRemoveItem(ddc);
					}
				}
			}

			if (ViewerHolder != null && !ViewerHolder.IsClosing)
			{
				DataGridViewColumn dvc = e.Component as DataGridViewColumn;
				if (dvc != null)
				{
				}
				else
				{
					//check usage
					uint id = ObjectMap.GetObjectID(e.Component);
					if (id == 0)
					{
						DesignUtil.WriteToOutputWindow("id not found on deleting component {0}", e.Component.Site.Name);
					}
					else
					{
						ClassPointer cp = GetRootId();
						List<ObjectTextID> list = cp.GetComponentUsage(id);
						if (list.Count > 0)
						{
							dlgObjectUsage dlg = new dlgObjectUsage();
							dlg.LoadData("Cannot delete this component. It is being used by the following objects", string.Format("Component - {0}", e.Component.Site.Name), list);
							dlg.ShowDialog();
							throw new DesignerException("Cannot delete component [{0}] at this time.", e.Component.Site.Name);
						}
					}
				}
				_componentRemoving = e.Component;
			}
		}

		void Host_Activated(object sender, EventArgs e)
		{
			if (Project == null)
			{
				MessageBox.Show(null, "Opening component file out side of its project is not supported. \r\nPlease open its project first. \r\nUse \"Recent Projects\" instead of \"Recent Files\" to open a project.\r\nIf you just renamed this file via the Solution Explorer then save the project before opening this file.", "Limnor Studio", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				ViewerHolder.Close();
			}
			else
			{
				LimnorXmlDesignerLoader2.ActiveItemID = this.ClassID;
				SetActiveDesigner(this);
				VPLUtil.CurrentProject = Project;
				MathNode.AddService(typeof(ObjectIDmap), ObjectMap);
				ViewerHolder.LoadProjectToolbox();
				ViewerHolder.RefreshPropertyGrid();
			}
		}
		private bool _deletingImage;
		private DateTime _dtDelImage;
		/// <summary>
		/// it happens after ComponentAdded event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void componentChangeService_ComponentChanged(object sender, ComponentChangedEventArgs e)
		{
			if (_isDisposed)
				return;
			if (_isFlushing)
				return;
			if (_disableNameChangingHandler)
				return;
#if DEBUGX
            FormStringList.ShowDebugMessage("Start: changing component {0}", getComponentDisplay(e));
#endif
			if (e != null && e.Component is Control && e.Member != null && string.CompareOrdinal(e.Member.Name, "Image") == 0)
			{
				_deletingImage = true;
				_dtDelImage = DateTime.Now;
				return;
			}
			//if (e != null && !(e.OldValue == null && e.NewValue == null))
			{
				doFlush();
			}
			//
			if (ViewerHolder != null)
			{
				ViewerHolder.RefreshPropertyGrid();
				ViewerHolder.HideToolbox();
			}
			//
			IRefreshOnComponentChange roc = e.Component as IRefreshOnComponentChange;
			if (roc != null)
			{
				roc.RefreshOnComponentChange();
			}

			sendVOBnotice(enumVobNotice.ObjectChanged, this.GetRootId());
			//
			IResetOnComponentChange rocc = e.Component as IResetOnComponentChange;
			if (rocc != null)
			{
				string nm = null;
				if (e.Member != null)
				{
					nm = e.Member.Name;
				}
				if (rocc.OnResetDesigner(nm))
				{
					ViewerHolder.OnResetDesigner(rocc);
				}
			}
		}
		void componentChangeService_ComponentRemoved(object sender, ComponentEventArgs e)
		{
			if (_isDisposed)
				return;
#if DEBUGX
            FormStringList.ShowDebugMessage("Start: removed component {0}", getComponentDisplay(e));
#endif
			if (e.Component is ToolboxItemXType)
			{
				return;
			}
			if (e.Component is ClassInstancePointer)
			{
				return;
			}
			LimnorContextMenuCollection.ClearMenuCollection(this.ClassID);
			DataGridViewColumn dvc = e.Component as DataGridViewColumn;
			if (ViewerHolder != null && !ViewerHolder.IsClosing)
			{
				if (dvc == null)
				{
					Control ctrl = e.Component as Control;
					if (ctrl != null)
					{
						Control cpt = ctrl.Parent;
						if (cpt != null)
						{
							cpt.Controls.Remove(ctrl);
						}
					}
					//
					ViewerHolder.OnComponentRemoved(e.Component);
					//
					uint id = 0;
					IClassRef cr = ObjectMap.RemoveClassRef(e.Component);
					if (ObjectMap.ContainsKey(e.Component))
					{
						id = ObjectMap[e.Component];
						ObjectMap.Remove(e.Component);
					}
					else
					{
						if (cr != null)
						{
							if (ObjectMap.ContainsKey(cr.ObjectInstance))
							{
								id = ObjectMap[cr.ObjectInstance];
								ObjectMap.Remove(cr.ObjectInstance);
							}
						}
					}
					object obj = null;
					if (id == 0)
					{
						DesignUtil.WriteToOutputWindow("id not found on deleting component {0}", e.Component.Site.Name);
						obj = ObjectMap.GetObjectByName(e.Component.Site.Name);
						if (obj != null)
						{
							id = ObjectMap.GetObjectID(obj);
						}
					}
					//
					DesignUtil.RemoveComponentById(Node, id);
					//
					ObjectMap.RemoveObjectFromTree(e.Component);
					if (obj != null)
					{
						ObjectMap.RemoveObjectFromTree(obj);
					}
					if (ObjectMap.ContainsKey(e.Component))
					{
						ObjectMap.Remove(e.Component);
					}
					else if (obj != null)
					{
						if (ObjectMap.ContainsKey(obj))
						{
							ObjectMap.Remove(obj);
						}
					}
					if (cr != null)
					{
						SerializeUtil.RemoveClassRefById(Node, cr.ClassId, cr.MemberId);
					}
					try
					{
						doFlush();
					}
					catch (TargetInvocationException errt)
					{
						TargetException te = errt.InnerException as TargetException;
						if (te != null)
						{
						}
						else
						{
							MathNode.Log(TraceLogClass.MainForm, errt);
						}
					}
					catch (Exception err)
					{
						MathNode.Log(TraceLogClass.MainForm, err);
					}
					//
					DrawingItem draw = e.Component as DrawingItem;
					if (draw != null)
					{
						DrawingPage page = _rootComponent as DrawingPage;
						if (page != null)
						{
							foreach (Control c in page.Controls)
							{
								IDrawDesignControl dc0 = c as IDrawDesignControl;
								if (dc0 != null)
								{
									if (dc0.Item == draw)
									{
										if (!dc0.Destroying)
										{
											dc0.Destroying = true;
											Host.DestroyComponent(c);
										}
										break;
									}
								}
							}
						}
					}
				}
			}
			sendVOBnotice(enumVobNotice.ComponentDeleted, e.Component);
			if (_rootComponent != null)
			{
				Control c = _rootComponent as Control;
				if (c != null)
				{
					if (!c.IsDisposed)
					{
						c.Refresh();
					}
				}
			}
		}
		void componentChangeService_ComponentRename(object sender, ComponentRenameEventArgs e)
		{
#if DEBUGX
            FormStringList.ShowDebugMessage("Start: rename component {0}", getComponentDisplay(e));
#endif
			if (_disableNameChangingHandler)
				return;
			if (e.Component == Host.RootComponent)
			{
				LimnorProject prj = LimnorSolution.GetProjectByComponentFile(this.DocumentMoniker);
				if (prj != null)
				{
					if (prj.IsComponentNameInUse(new ComponentID(prj, this.ObjectMap.ClassId, e.NewName, e.Component.GetType(), this.DocumentMoniker)))
					{
						MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "Component name {0} is in used", e.NewName), "Rename", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						Host.RootComponent.Site.Name = e.OldName;
						return;
					}
					else
					{
						if (prj.ProjectType == EnumProjectType.WebAppPhp)
						{
							string sn = e.NewName.ToLowerInvariant();
							if (WebPageCompilerUtility.IsReservedPhpWord(sn))
							{
								MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "Component name {0} is a reserved word", e.NewName), "Rename", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
								Host.RootComponent.Site.Name = e.OldName;
								return;
							}
						}
					}
				}
			}
			if (ViewerHolder != null)
			{
				ViewerHolder.OnComponentRename(e.Component, e.NewName);
			}
			if (e.Component == _rootComponent)
			{
				_classData.Rename(e.NewName);
				sendVOBnotice(enumVobNotice.ObjectNameChanged, _classData);
			}
			sendVOBnotice(enumVobNotice.ComponentRenamed, e.Component);
		}
		bool _disableNameChangingHandler;
		void componentChangeService_ComponentAdded(object sender, ComponentEventArgs e)
		{
			if (_isDisposed)
				return;
			try
			{
#if DEBUGX
				FormStringList.ShowDebugMessage("Start: added component {0}", getComponentDisplay(e));
#endif
				if (ToolboxItemXType.SelectedToolboxClassId == 0)
				{
					ToolboxItemXType xt = e.Component as ToolboxItemXType;
					if (xt != null)
					{
						if (ViewerHolder != null)
						{
							ViewerHolder.OnComponentAdded(e.Component);
						}
						return;
					}
				}
				else
				{
					if (e.Component is ClassInstancePointer)
					{
						if (ViewerHolder != null)
						{
							ViewerHolder.OnComponentAdded(e.Component);
						}
						return;
					}
				}
				if (e.Component is SplitterPanel)
				{
					return;
				}
				DataGridViewColumn dvc = e.Component as DataGridViewColumn;
				if (dvc != null)
				{
					EasyGrid.OnAddCell(dvc);
					return;
				}
				IIdeSpecific idesp = e.Component as IIdeSpecific;
				if (idesp != null)
				{
					idesp.IdeEdition = VPLUtil.IDEEDITION;
				}
				///===host specific handling===========
				Type te = e.Component.GetType();
				Control ctrl = e.Component as Control;
				if (ctrl != null && ctrl.Parent == null)
				{
					if (!(ctrl is TabPage) && !(ctrl is ContextMenuStrip))
					{
						Control cparent = _rootComponent as Control;
						if (cparent != null)
						{
							if (ctrl.Location.X <= 0 || ctrl.Location.X > cparent.ClientSize.Width
								|| ctrl.Location.Y <= 0 || ctrl.Location.Y > cparent.ClientSize.Height)
							{
								if (LimnorXmlDesignerLoader2.MenuPoint != Point.Empty)
								{
									ctrl.Location = cparent.PointToClient(LimnorXmlDesignerLoader2.MenuPoint);
									LimnorXmlDesignerLoader2.MenuPoint = Point.Empty;
								}
								else
									ctrl.Location = cparent.PointToClient(Cursor.Position);
								if (ctrl.Location.X < 0 || ctrl.Location.X > cparent.ClientSize.Width)
								{
									ctrl.Left = cparent.ClientSize.Width / 2;
								}
								if (ctrl.Location.Y < 0 || ctrl.Location.Y > cparent.ClientSize.Height)
								{
									ctrl.Top = cparent.ClientSize.Height / 2;
								}
							}
							cparent.Controls.Add(ctrl);
						}
					}
				}
				INewObjectInit noi = e.Component as INewObjectInit;
				if (noi != null)
				{
					noi.OnNewInstanceCreated();
				}
				IXmlCodeReaderWriterHolder xrw = e.Component as IXmlCodeReaderWriterHolder;
				if (xrw != null)
				{
					xrw.SetReader(new XmlObjectReader(this.GetRootId().ObjectList));
					xrw.SetWriter(new XmlObjectWriter(this.GetRootId().ObjectList));
				}
				IDevClassReferencer dcr = VPLUtil.GetObject(e.Component) as IDevClassReferencer;
				if (dcr != null)
				{
					dcr.SetDevClass(this.GetRootId());
				}
				///========================================
				ToolStripMenuItem mi = e.Component as ToolStripMenuItem;
				if (mi != null)
				{
					if (string.CompareOrdinal(e.Component.Site.Name, "ContextMenuStrip") == 0)
					{
						_disableNameChangingHandler = true;
						e.Component.Site.Name = "mi" + Guid.NewGuid().GetHashCode().ToString("x");
						_disableNameChangingHandler = false;
					}
				}
				else
				{
					IDatabaseConnectionUserExt da = e.Component as IDatabaseConnectionUserExt;
					if (da != null)
					{
						IList<Guid> gs = Project.GetObjectGuidList();
						if (gs != null & gs.Count > 0)
						{
							ConnectionItem ci = ConnectionItem.LoadConnection(gs[0], false, false);
							if (ci != null)
							{
								da.DatabaseConnection = ci;
							}
						}
					}
				}
				uint id = ObjectMap.AddNewObject(e.Component);
				if (ToolboxItemXType.SelectedToolboxClassId != 0)
				{
					ClassPointer definitionClass = ClassPointer.CreateClassPointer(ToolboxItemXType.SelectedToolboxClassId, Project);
					ClassInstancePointer cr = e.Component as ClassInstancePointer;
					if (cr == null)
					{
						cr = new ClassInstancePointer(this.GetRootId(), definitionClass, id);
						cr.ObjectInstance = e.Component;
					}
					else
					{
						//it will not get into here!!!
						cr.Owner = this.GetRootId();
						cr.ClassId = ToolboxItemXType.SelectedToolboxClassId;
						cr.ReplaceDeclaringClassPointer(definitionClass);
						cr.MemberId = id;
					}
					XmlNode n = SerializeUtil.CreateClassRefNode(Node);
					XmlUtil.SetAttribute(n, XmlTags.XMLATT_ClassID, ToolboxItemXType.SelectedToolboxClassId);//definition class Id
					XmlUtil.SetAttribute(n, XmlTags.XMLATT_ComponentID, id); //instance Id
					XmlObjectWriter xw = Writer;
					ObjectMap.SetClassRefMap(e.Component, cr);
					xw.WriteObjectToNode(n, cr, false);
					ToolboxItemXType.SelectedToolboxClassId = 0;
				}
				else
				{
					ClassInstancePointer cr = e.Component as ClassInstancePointer;
					if (cr != null)
					{
						throw new DesignerException("Toolbox selection mismatch. Please try again.");
					}
					if (ToolboxItemXType.SelectedToolboxType != null)
					{
						RuntimeInstance lti = e.Component as RuntimeInstance;
						if (lti == null)
						{
							throw new DesignerException("Toolbox selection mismatch: component is not a RuntimeInstance. Please try again.");
						}
						lti.SetType(ToolboxItemXType.SelectedToolboxType, Project.ProjectFolder);
						//
						e.Component.Site.Name = CreateNewComponentName(VPLUtil.FormCodeNameFromname(VPLUtil.GetTypeDisplay(ToolboxItemXType.SelectedToolboxType)), null);
					}
				}
				IPropertyListchangeable plc = e.Component as IPropertyListchangeable;
				if (plc != null)
				{
					plc.SetPropertyListNotifyTarget(this);
				}
				if (ViewerHolder != null)
				{
					ViewerHolder.OnComponentAdded(e.Component);
				}
				EasyGrid eg = e.Component as EasyGrid;
				if (eg != null)
				{
					eg.ColumnAdded += new DataGridViewColumnEventHandler(eg_ColumnAdded);
					eg.ColumnRemoved += new DataGridViewColumnEventHandler(eg_ColumnRemoved);
					eg.SetHandlerForSQLChange(onEasyGridBeforeChangeSQL, onEasyGridAfterChangeSQL);
				}
				ILoaderInitializer li = e.Component as ILoaderInitializer;
				if (li != null)
				{
					li.OnLoaderInitialize();
				}
				sendVOBnotice(enumVobNotice.ComponentAdded, e.Component);
#if DEBUGX
                FormStringList.ShowDebugMessage("End: added component {0}", getComponentDisplay(e));
#endif
			}
			catch (Exception err)
			{
#if DEBUGX
                FormStringList.ShowDebugMessage("Error: added component {0}", DesignerException.FormExceptionText(err));
#else
				MathNode.Log(TraceLogClass.MainForm, err);
#endif
				throw;
			}
		}
		void eg_ColumnRemoved(object sender, DataGridViewColumnEventArgs e)
		{
			DataGridView dv = sender as DataGridView;
			if (dv == null)
			{
				dv = e.Column.DataGridView;
			}
			if (dv != null)
			{
				IDataGrid eg = dv as IDataGrid;
				if (eg != null && eg.DisableColumnChangeNotification)
				{
					return;
				}
				recordDataGridViewColumn(dv, e.Column);
				ViewerHolder.PaneHolder.OnComponentRemoved(e.Column);
				ObjectMap.RemoveObjectFromTree(e.Column);
			}
		}

		void eg_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
		{
			DataGridView dv = sender as DataGridView;
			if (dv == null)
			{
				dv = e.Column.DataGridView;
			}
			if (dv != null)
			{
				IDataGrid eg = dv as IDataGrid;
				if (eg != null && eg.DisableColumnChangeNotification)
				{
					return;
				}
				if (_columns != null)
				{
					Dictionary<UInt32, string> cls;
					if (_columns.TryGetValue(dv, out cls))
					{
						foreach (KeyValuePair<UInt32, string> kv in cls)
						{
							if (string.CompareOrdinal(e.Column.DataPropertyName, kv.Value) == 0)
							{
								ObjectMap.ReplaceObject(kv.Key, e.Column);
								break;
							}
						}
					}
				}
				if (e.Column.DataGridView != null)
				{
					ObjectMap.TreeRoot.AddChild(e.Column);
					if (e.Column.Site != null && e.Column.Site.DesignMode)
					{
						ViewerHolder.PaneHolder.OnComponentAdded(e.Column);
					}
				}
			}
		}
		#endregion

		#region private methods
		private void doFlush()
		{
			this.Project.AddModifiedClass(this.ClassID);
			try
			{
				this.Flush();
			}
			catch (TargetInvocationException)
			{
			}
			catch (Exception err)
			{
				MathNode.Log(err, "Error updating form designer [{0}, {1}]", this.ClassID, this.ObjectName);
			}
		}
		private void recordDataGridViewColumn(DataGridView dv, DataGridViewColumn c)
		{
			UInt32 dcId = ObjectMap.GetObjectID(c);
			if (dcId != 0)
			{
				if (_columns == null)
				{
					_columns = new Dictionary<DataGridView, Dictionary<uint, string>>();
				}
				Dictionary<uint, string> cls;
				if (!_columns.TryGetValue(dv, out cls))
				{
					cls = new Dictionary<uint, string>();
					_columns.Add(dv, cls);
				}
				if (cls.ContainsKey(dcId))
				{
					cls[dcId] = c.DataPropertyName;
				}
				else
				{
					cls.Add(dcId, c.DataPropertyName);
				}
			}
		}
		private void onEasyGridBeforeChangeSQL(object sender, EventArgs e)
		{
			DataGridView dv = sender as DataGridView;
			if (dv != null)
			{
				foreach (DataGridViewColumn dvc in dv.Columns)
				{
					if (dvc.Site != null && dvc.Site.DesignMode)
					{
						ViewerHolder.PaneHolder.OnComponentRemoved(dvc);
					}
				}
			}
		}
		private void onEasyGridAfterChangeSQL(object sender, EventArgs e)
		{
			List<DataGridViewColumn> l = new List<DataGridViewColumn>();
			foreach (object v in _objMap.Keys)
			{
				DataGridViewColumn c = v as DataGridViewColumn;
				if (c != null)
				{
					if (c.DataGridView == null) //removed
					{
						l.Add(c);
					}
				}
			}
			if (l.Count > 0)
			{
				foreach (DataGridViewColumn c in l)
				{
					if (ViewerHolder != null)
					{
						ViewerHolder.OnComponentRemoved(c);
					}
					_objMap.TreeRoot.RemoveTree(c);
					_objMap.Remove(c);

				}
			}
			DataGridView dv = sender as DataGridView;
			if (dv != null)
			{
				foreach (DataGridViewColumn dvc in dv.Columns)
				{
					if (dvc.Site != null && dvc.Site.DesignMode)
					{
						UInt32 id = _objMap.GetObjectID(dvc);
						if (id == 0)
						{
							_objMap.AddNewObject(dvc);
						}
						ViewerHolder.PaneHolder.OnComponentAdded(dvc);
					}
				}
			}
		}
		private void initialize()
		{
			_objMap = ClassPointer.GetObjectmap(Project, Node);
			if (_rootPointer == null)
			{
				_rootPointer = ClassPointer.CreateClassPointer(_objMap);
				_objMap.SetTypedData<ClassPointer>(_rootPointer);
				//replace other references with this instance
				Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
				foreach (ILimnorDesignPane pn in designPanes.Values)
				{
					if (pn.Loader.ClassID != _objMap.ClassId)
					{
						pn.Loader.GetRootId().SetClassPointerReferences(_rootPointer);
					}
				}
			}
			else
			{
				_rootPointer.SetData(_objMap);
			}
			if (_objMap != null)
			{
				_objMap.SetXmlData(Node);
				if (_objMap.Reader == null)
				{
					XmlObjectReader reader = new XmlObjectReader(_objMap, ClassPointer.OnAfterReadRootComponent, ClassPointer.OnRootComponentCreated);
					_objMap.SetReader(reader);
				}
			}
		}
		private static void onDatabaseListChanged(object sender, EventArgs e)
		{
			LimnorProject prj = sender as LimnorProject;
			if (prj != null)
			{
				Dictionary<UInt32, ILimnorDesignPane> designPanes = prj.GetTypedDataList<ILimnorDesignPane>();
				foreach (ILimnorDesignPane pn in designPanes.Values)
				{
					pn.OnDatabaseListChanged();
				}
			}
		}
		private void updateItems(XmlObjectReader r, XmlNode nodeCurrent, XmlNode nodeToApply, object obj, PropertyDescriptor p)
		{
			bool isForDesign = false;
			IList list = (IList)p.GetValue(obj);
			XmlNodeList ndToApplyList = SerializeUtil.GetItemNodeList(nodeToApply);
			XmlNodeList ndToUpdateList = SerializeUtil.GetItemNodeList(nodeCurrent);
			if (ndToApplyList.Count > 0)
			{
				isForDesign = SerializeUtil.IsNodeForDesign(ndToApplyList[0]);
			}
			else if (ndToUpdateList.Count > 0)
			{
				isForDesign = SerializeUtil.IsNodeForDesign(ndToUpdateList[0]);
			}
			if (isForDesign)
			{
				//remove deleted components
				bool bDeleted = false;
				foreach (XmlNode nd in ndToUpdateList)
				{
					string name = XmlUtil.GetNameAttribute(nd);
					bool bFound = false;
					foreach (XmlNode nd2 in ndToApplyList)
					{
						if (name == XmlUtil.GetNameAttribute(nd2))
						{
							bFound = true;
							break;
						}
					}
					if (!bFound)
					{
						foreach (IComponent c in Host.Container.Components)
						{
							if (c.Site.Name == name)
							{
								Host.Container.Remove(c);
								nodeCurrent.RemoveChild(nd);
								list.Remove(c);
								bDeleted = true;
								break;
							}
						}
					}
				}
				if (bDeleted)
				{
					ndToUpdateList = SerializeUtil.GetItemNodeList(nodeCurrent);
				}
				//add or update modified components
				foreach (XmlNode nd in ndToApplyList)
				{
					string name = XmlUtil.GetNameAttribute(nd);
					XmlNode ndCur = null;
					foreach (XmlNode nd2 in ndToUpdateList)
					{
						if (name == XmlUtil.GetNameAttribute(nd2))
						{
							ndCur = nd2;
							break;
						}
					}
					if (ndCur == null)
					{
						ndCur = nodeCurrent.OwnerDocument.ImportNode(nd, true);
						nodeCurrent.AppendChild(ndCur);
						object v = r.ReadObject(ndCur, obj);
						list.Add(v);
					}
					else
					{
						IComponent ic = null;
						foreach (IComponent c in Host.Container.Components)
						{
							if (c.Site.Name == name)
							{
								ic = c;
								break;
							}
						}
						updateProperties(r, ndCur, nd, ic);
					}
				}
			}
			else
			{
				if (nodeCurrent.InnerText != nodeToApply.InnerText)
				{
					XmlNode nodeCurParent = nodeCurrent.ParentNode;
					nodeCurParent.RemoveChild(nodeCurrent);
					nodeCurrent = nodeCurParent.OwnerDocument.ImportNode(nodeToApply, true);
					r.ReadProperty(p, nodeCurrent, obj, false, XmlTags.XML_PROPERTY);
				}
			}
		}
		private void updateProperties(XmlObjectReader r, XmlNode nodeCurrent, XmlNode nodeToApply, object obj)
		{
			PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
			foreach (PropertyDescriptor p in props)
			{
				XmlNode ndToApply = SerializeUtil.GetPropertyNode(nodeToApply, p.Name);
				XmlNode ndToUpdate = SerializeUtil.GetPropertyNode(nodeCurrent, p.Name);
				if (ndToUpdate != null && ndToApply == null)
				{
					nodeCurrent.RemoveChild(ndToUpdate);
					p.ResetValue(obj);
				}
				else if (ndToUpdate == null && ndToApply != null)
				{
					ndToUpdate = nodeCurrent.OwnerDocument.ImportNode(ndToApply, true);
					nodeCurrent.AppendChild(ndToUpdate);
					r.ReadProperty(p, ndToUpdate, obj, false, XmlTags.XML_PROPERTY);
				}
				else if (ndToUpdate != null && ndToApply != null)
				{
					if (typeof(IList).IsAssignableFrom(p.PropertyType))
					{
						updateItems(r, ndToUpdate, ndToApply, obj, p);
					}
					else
					{
						if (ndToUpdate.InnerText != ndToApply.InnerText)
						{
							nodeCurrent.RemoveChild(ndToUpdate);
							ndToUpdate = nodeCurrent.OwnerDocument.ImportNode(ndToApply, true);
							nodeCurrent.AppendChild(ndToUpdate);
							r.ReadProperty(p, ndToUpdate, obj, false, XmlTags.XML_PROPERTY);
						}
					}
				}
			}
		}
		#endregion

		#region public methods
		public bool SaveToXmlFailed()
		{
			return _errorSaving;
		}
		public void ResetSaveXmlError()
		{
			_errorSaving = false;
		}
		public void SetRunContext()
		{
			if (_rootPointer != null)
			{
				_rootPointer.SetRunContext();
			}
		}
		public void ManageWebService()
		{
			if (ViewerHolder != null)
			{
				ViewerHolder.ManageWebService();
			}
		}
		public void AddWebService()
		{
			if (ViewerHolder != null)
			{
				ViewerHolder.AddWebService();
			}
		}
		public static void SetActiveDesigner(IXmlDesignerLoader designer)
		{
			LimnorProject.ActiveDesignerLoader = designer;
			LimnorXmlDesignerLoader2 l2 = designer as LimnorXmlDesignerLoader2;
			if (l2 != null)
			{
				ClassPointer r = l2.GetRootId();
				if (r != null)
				{
					XmlSerializerUtility.ActiveWriter = r.CreateWriter();
					XmlSerializerUtility.ActiveReader = r.CreateReader();
					l2.sendVOBnotice(enumVobNotice.ObjectActivate2, r);
				}
			}
		}
		/// <summary>
		/// called by a timer after the design pane has loaded
		/// </summary>
		public void OnDesignPaneLoaded()
		{
			if (!_verified)
			{
				_verified = true;
				GetRootId().OnDesignPaneLoaded();
				if (ViewerHolder != null)
				{
					ViewerHolder.PaneHolder.SetLoaded();
				}
			}
		}
		public void Refresh()
		{
			if (!loadingDesigner)
			{
				loadingDesigner = true;
				try
				{
					//refreshing logic here
				}
				catch (Exception err)
				{
					MathNode.Log(TraceLogClass.MainForm, err);
				}
				finally
				{
					loadingDesigner = false;
				}
			}
		}
		#endregion

		#region property change handling
		public void RefreshPropertyBrowser()
		{
			ILimnorSudioIDE shell = this.GetService(typeof(ILimnorSudioIDE)) as ILimnorSudioIDE;
			if (shell != null)
				shell.RefreshPropertyBrowser();
		}
		#endregion

		#region BasicDesignerLoader methods

		protected override void PerformFlush(IDesignerSerializationManager serializationManager)
		{
			if (IsClosing)
				return;
			if (!_isFlushing)
			{
				_isFlushing = true;
				saveToXml(serializationManager);
				_componentRemoving = null;
				_isFlushing = false;
			}
		}

		protected override void PerformLoad(IDesignerSerializationManager serializationManager)
		{
			DesignUtil.LogIdeProfile("XML Designer loader: perform loading");
			if (_isDisposed)
				return;
			if (loadingDesigner)
				return;
			//===test code==================================================
			//if (_testRootType != null)
			//{
			//    //this.host = this.LoaderHost;
			//    ArrayList errors = new ArrayList();
			//    bool successful = true;
			//    string baseClassName;
			//    _rootComponent = LoaderHost.CreateComponent(_testRootType, "Page1");
			//    baseClassName = "Home";
			//    LoaderHost.EndLoad(baseClassName, successful, errors);
			//    return;
			//}
			//==============================================================
			if (_isSetup)
			{
				Host.EndLoad(LimnorXmlDesignerLoader2.hostedBaseClassName, true, null);
				FormLoadProgress.SetProgInfo("Load finished");
				return;
			}
			FormLoadProgress.SetProgInfo("loading...");
			loadingDesigner = true;
			if (ObjectMap != null)
			{
				DesignUtil.LogIdeProfile("XML Designer loader:Reset loader");
				ObjectMap.DocumentMoniker = DocumentMoniker;
				ObjectMap.ClearItems();
				_rootPointer.Reset();
				XmlSerializerUtility.ActiveReader = Reader;
				XmlSerializerUtility.ActiveWriter = _rootPointer.CreateWriter();
				//
				XmlObjectReader r = Reader;
				r.SerializerManager = serializationManager;
				r.Reset();
				MathNode.ResetShowErrorFlag();
				ComponentFactory o = new ComponentFactory();
				o.DesignerLoaderHost = this.Host;
				DesignUtil.LogIdeProfile("XML Designer loader:Read from XML");
				_rootComponent = (IComponent)r.ReadRootObject(o, Node);
				if (VPLUtil.Shutingdown)
				{
					return;
				}
				IWithProject2 app = _rootComponent as IWithProject2;
				if (app != null)
				{
					app.SetProject(Project);
				}
				IDevClassReferencer dcr = VPLUtil.GetObject(_rootComponent) as IDevClassReferencer;
				if (dcr != null)
				{
					dcr.SetDevClass(_rootPointer);
				}
				if (r.Errors != null && r.Errors.Count > 0)
				{
					MathNode.Log(r.Errors);
					r.ResetErrors();
				}
				DesignUtil.LogIdeProfile("XML Designer loader:validate interfaces");
				_rootPointer.ValidateInterfaceImplementations();
				if (_rootComponent != null)
				{
					DesignUtil.LogIdeProfile("XML Designer loader:handle special objects");
					foreach (object v in this.ObjectMap.Keys)
					{
						IIdeSpecific idesp = v as IIdeSpecific;
						if (idesp != null)
						{
							idesp.IdeEdition = VPLUtil.IDEEDITION;
						}
						IDeserized idr = v as IDeserized;
						if (idr != null)
						{
							idr.OnDeserized(_rootComponent);
						}
						CopyProtector cp = v as CopyProtector;
						if (cp != null)
						{
							cp.SetApplicationGuid(Project.ProjectGuid);
							cp.SetApplicationName(Project.ProjectName);
						}
						else
						{
							EasyGrid ed = v as EasyGrid;
							if (ed != null)
							{
								ed.SetHandlerForSQLChange(onEasyGridBeforeChangeSQL, onEasyGridAfterChangeSQL);
							}
							else
							{
								IPropertyListchangeable plc = v as IPropertyListchangeable;
								if (plc != null)
								{
									plc.SetPropertyListNotifyTarget(this);
								}
							}
						}
					}
					uint id = this.ObjectMap.GetObjectID(_rootComponent);
					if (id == 0)
					{
						id = this.ObjectMap.AddNewObject(_rootComponent);
						if (id != 1)
						{
							throw new Exception("Object ID 1 must be for the root object");
						}
						XmlUtil.SetAttribute(Node, XmlTags.XMLATT_ComponentID, id);
					}
					Form f = _rootComponent as Form;
					if (f != null)
					{
						ClassPointer root = this.GetRootId();
						if (!string.IsNullOrEmpty(root.FullIconPath))
						{
							if (System.IO.File.Exists(root.FullIconPath))
							{
								if (string.Compare(System.IO.Path.GetExtension(root.FullIconPath), ".ico", StringComparison.OrdinalIgnoreCase) == 0)
								{
									Icon ic = Icon.ExtractAssociatedIcon(root.FullIconPath);
									f.Icon = ic;
								}
							}
						}
					}
					//
					ObjectMap.HookCustomPropertyValueChange(new EventHandler(onCustomPropertyValueChange));
					//
					Host.EndLoad(LimnorXmlDesignerLoader2.hostedBaseClassName, true, null);
				}
			}
			loadingDesigner = false;
			if (_rootComponent != null)
			{
				InterfaceClass ifc = VPLUtil.GetObject(_rootComponent) as InterfaceClass;
				if (ifc != null)
				{
					ifc.ClassId = ObjectMap.ClassId;
					ifc.Holder = GetRootId();
				}
			}
			//
			foreach (object v in ObjectMap.Keys)
			{
				DataGridView dv = v as DataGridView;
				if (dv != null)
				{
					foreach (DataGridViewColumn dvc in dv.Columns)
					{
						recordDataGridViewColumn(dv, dvc);
					}
					dv.ColumnRemoved += new DataGridViewColumnEventHandler(eg_ColumnRemoved);
					dv.ColumnAdded += new DataGridViewColumnEventHandler(eg_ColumnAdded);
				}
			}
			//
			foreach (object v in _objMap.Keys)
			{
				ILoaderInitializer li = v as ILoaderInitializer;
				if (li != null)
				{
					li.OnLoaderInitialize();
				}
			}
			IWebPage wp = _rootComponent as IWebPage;
			if (wp != null)
			{
				wp.AddWebControl(this.Host);
			}
			//
			FormLoadProgress.SetProgInfo("Load finished");
			DesignUtil.LogIdeProfile("XML Designer loader:finish perform load");
		}

		public bool UIDesignerNotNeeded
		{
			get
			{
				if (_rootComponent != null)
				{
					Type t = VPLUtil.GetObjectType(_rootComponent);
					if (t.Equals(typeof(InterfaceClass)))
					{
						return true;
					}
				}
				return false;
			}
		}
		#endregion

		#region ILimnorDesignerLoader Members
		public string ComponentFilePath
		{
			get
			{
				return _classData.ComponentFile;
			}
		}
		public XmlObjectReader Reader
		{
			get
			{
				initialize();
				return _objMap.Reader; ;
			}
		}
		public XmlObjectWriter Writer
		{
			get
			{
				if (_writer == null)
				{
					_writer = new XmlObjectWriter(this.ObjectMap);
				}
				return _writer;
			}
		}

		public LimnorProject ActiveProject
		{
			get
			{
				if (ActiveDesignerLoader != null)
				{
					return ActiveDesignerLoader.Project;
				}
				return null;
			}
		}
		public IXmlDesignerLoader ActiveXmlDesignerLoader
		{
			get
			{
				return ActiveDesignerLoader;
			}
		}
		public bool IsClosing
		{
			get
			{
				if (ViewerHolder != null)
				{
					return ViewerHolder.IsClosing;
				}
				return true;
			}
		}
		public void UpdateHtmlFile()
		{
			IWebPage wpage = RootObject as IWebPage;
			if (wpage != null)
			{
				wpage.UpdateHtmlFile();
			}
		}
		public void SaveHtmlFile()
		{
			IWebPage wpage = RootObject as IWebPage;
			if (wpage != null)
			{
				wpage.SaveHtmlFile();
			}
		}
		public PropertyPointer CreatePropertyPointer(object v, string propertyName)
		{
			ClassPointer rid = this.GetRootId();
			ComponentCollection cc = Host.Container.Components;
			foreach (IComponent c in cc)
			{
				if (c == v)
				{
					IObjectPointer op;
					ClassInstancePointer r = c as ClassInstancePointer;
					if (r != null)
					{
						op = r;
					}
					else if (v == _rootComponent)
					{
						op = rid;
					}
					else
					{
						op = GetMemberId(v);
					}
					PropertyDescriptor p = VPLUtil.GetProperty(v, propertyName);
					if (p != null)
					{
						PropertyPointer pp = new PropertyPointer();
						pp.Owner = op;
						pp.SetPropertyInfo(p);
						return pp;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// called when a property name changes
		/// </summary>
		/// <param name="cr"></param>
		public void ReloadClassRefProperties(ClassInstancePointer cr)
		{
			//if the defining class is also in design mode, use it for this instance
			ILimnorDesignPane dp = Project.GetTypedData<ILimnorDesignPane>(cr.ClassId);
			if (dp != null)
			{
				cr.ReplaceDeclaringClassPointer(dp.Loader.ObjectMap);
			}
			//load custom property definitions
			cr.LoadProperties(Reader);
			//hook value change handler
			cr.HookCustomPropertyValueChange(new EventHandler(onCustomPropertyValueChange));
			XmlNode cn = SerializeUtil.GetClassRefNodeByObjectId(Node, cr.MemberId);
			Reader.ReadProperties(cn, cr);
			//loadpropertyValues
		}
		public IClassRef GetClassRefByComponent(IComponent c)
		{
			return this.ObjectMap.GetClassRefByObject(c);
		}
		public void DeleteComponent(IComponent component)
		{
#if DEBUGX
            FormStringList.ShowDebugMessage("Start: call DeleteComponent {0}", getComponentDisplay(component));
#endif
			if (component != null)
			{
				DesignUtil.WriteToOutputWindow("deleting component:{0}", component);
				Host.DestroyComponent(component);
			}
		}
		public IComponent AddNewComponent(Type type)
		{
			string name = CreateNewComponentName(type.Name, null);
			return Host.CreateComponent(type, name);
		}
		public IComponent CreateComponent(Type type, string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = CreateNewComponentName(type.Name, null);
			}
			return Host.CreateComponent(type, name);
		}
		public string CreateNewComponentName(string basename, StringCollection namesEx)
		{
			int n = 1;
			string name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
			StringCollection sc = new StringCollection();
			ComponentCollection cc = Host.Container.Components;
			foreach (IComponent c in cc)
			{
				sc.Add(c.Site.Name);
			}
			while (sc.Contains(name))
			{
				n++;
				name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
			}
			if (namesEx != null)
			{
				while (namesEx.Contains(name))
				{
					n++;
					name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
				}
			}
			if (_rootComponent != null)
			{
				Type t = _rootComponent.GetType();
				//all existing properties
				bool b = true;
				PropertyInfo[] pifs = t.GetProperties();
				while (b)
				{
					b = false;
					for (int i = 0; i < pifs.Length; i++)
					{
						if (string.CompareOrdinal(pifs[i].Name, name) == 0)
						{
							n++;
							name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
							b = true;
							break;
						}
					}
				}
				//all methods
				MethodInfo[] mifs = t.GetMethods();
				b = true;
				while (b)
				{
					b = false;
					for (int i = 0; i < mifs.Length; i++)
					{
						if (string.CompareOrdinal(mifs[i].Name, name) == 0)
						{
							n++;
							name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
							b = true;
							break;
						}
					}
				}
				//all events
				EventInfo[] eifs = t.GetEvents();
				b = true;
				while (b)
				{
					b = false;
					for (int i = 0; i < eifs.Length; i++)
					{
						if (string.CompareOrdinal(eifs[i].Name, name) == 0)
						{
							n++;
							name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
							b = true;
							break;
						}
					}
				}
				//All used names
				StringCollection names = SerializeUtil.GetCustomNames(Node);
				while (names.Contains(name))
				{
					n++;
					name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
				}
			}
			while (_objMap.NameUsed(name))
			{
				n++;
				name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
			}
			ClassPointer root = GetRootId();
			Dictionary<string, MethodClass> mcs = root.CustomMethods;
			foreach (MethodClass mc in mcs.Values)
			{
				if (string.CompareOrdinal(mc.MethodName, name) == 0)
				{
					n++;
					name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
				}
			}
			Dictionary<string, PropertyClass> props = root.CustomProperties;
			foreach (PropertyClass pc in props.Values)
			{
				if (string.CompareOrdinal(pc.Name, name) == 0)
				{
					n++;
					name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
				}
			}
			Dictionary<string, EventClass> events = root.CustomEvents;
			foreach (EventClass ec in events.Values)
			{
				if (string.CompareOrdinal(ec.Name, name) == 0)
				{
					n++;
					name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
				}
			}
			List<EventAction> eas = root.EventHandlers;
			if (eas != null && eas.Count > 0)
			{
				foreach (EventAction ea in eas)
				{
					if (ea.TaskIDList != null && ea.TaskIDList.Count > 0)
					{
						foreach (TaskID tid in ea.TaskIDList)
						{
							HandlerMethodID hid = tid as HandlerMethodID;
							if (hid != null)
							{
								if (string.CompareOrdinal(name, hid.HandlerMethod.MethodName) == 0)
								{
									n++;
									name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
								}
							}
						}
					}
				}
			}
			if (root.IsInterface)
			{
				InterfaceClass ifc = root.Interface;
				if (ifc != null)
				{
					List<InterfaceElementMethod> ims = ifc.Methods;
					if (ims != null && ims.Count > 0)
					{
						foreach (InterfaceElementMethod im in ims)
						{
							if (string.CompareOrdinal(im.Name, name) == 0)
							{
								n++;
								name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
							}
						}
					}
					List<InterfaceElementProperty> ips = ifc.Properties;
					if (ips != null && ips.Count > 0)
					{
						foreach (InterfaceElementProperty ip in ips)
						{
							if (string.CompareOrdinal(ip.Name, name) == 0)
							{
								n++;
								name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
							}
						}
					}
					List<InterfaceElementEvent> ies = ifc.Events;
					if (ies != null && ies.Count > 0)
					{
						foreach (InterfaceElementEvent ie in ies)
						{
							if (string.CompareOrdinal(ie.Name, name) == 0)
							{
								n++;
								name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", basename, n);
							}
						}
					}
				}
			}
			return name;
		}
		public bool IsNameUsed(string name, UInt32 methodScope)
		{
			ComponentCollection cc = Host.Container.Components;
			foreach (IComponent c in cc)
			{
				if (name == c.Site.Name)
				{
					return true;
				}
			}
			if (_rootComponent != null)
			{
				Type t = _rootComponent.GetType();
				MemberInfo[] mif = t.GetMember(name);
				if (mif != null && mif.Length > 0)
				{
					return true;
				}
				XmlNode nd = null;
				if (methodScope != 0)
				{
					nd = Node.SelectSingleNode(string.Format(CultureInfo.InvariantCulture, "{0}/{1}[@MethodID='{2}']", XmlTags.XML_METHODS, XmlTags.XML_METHOD, methodScope));
				}
				if (nd == null)
				{
					nd = Node;
				}
				if (SerializeUtil.IsNameUsed(nd, name, (methodScope == 0)))
				{
					return true;
				}
			}
			if (_objMap.NameUsed(name))
			{
				return true;
			}
			StringCollection scs = SerializeUtil.GetCustomNames(Node);
			if (scs.Contains(name))
			{
				return true;
			}
			return false;
		}
		public IList<ILimnorDesignPane> GetCurrentDesignPanes()
		{
			List<ILimnorDesignPane> list = new List<ILimnorDesignPane>();
			Dictionary<UInt32, ILimnorDesignPane> xmlPanes = Project.GetTypedDataList<ILimnorDesignPane>();
			if (xmlPanes != null)
			{
				Dictionary<UInt32, ILimnorDesignPane>.Enumerator en = xmlPanes.GetEnumerator();
				while (en.MoveNext())
				{
					list.Add(en.Current.Value);
				}
			}
			return list;
		}
		public bool DataModified
		{
			get
			{
				return this.Modified;
			}
			set
			{
				this.Modified = value;
				if (value)
				{
					sendVOBnotice(enumVobNotice.ObjectChanged, this.GetRootId());
				}
			}
		}
		public void sendVOBnotice(enumVobNotice notice, object data)
		{
			InterfaceVOB vob = (InterfaceVOB)Host.GetService(typeof(InterfaceVOB));
			if (vob != null)
			{
				vob.SendNotice(notice, data);
			}
		}
		private void saveRoot(IDesignerSerializationManager serializationManager)
		{
			if (_isDisposed)
				return;
			if (_rootComponent == null)
			{
				return;
			}
			XmlObjectWriter w = Writer;
			w.ClearErrors();
			w.SerializerManager = serializationManager;
			w.ObjectBeDeleted = _componentRemoving;
			if (_rootComponent.Site == null)
			{
				MessageBox.Show("Out of sync");
				w.WriteRootObject(Node, _rootComponent, _objMap.Name);
			}
			else
			{
				w.WriteRootObject(Node, _rootComponent, _rootComponent.Site.Name);
			}
			if (_rootComponent is LimnorApp)
			{
				w.WriteStaticProperties(Node);
			}
			else
			{
				if (Project.ProjectType == EnumProjectType.ClassDLL)
				{
					using (XmlDoc d = Project.GetVob())
					{
						XmlNode vobNode = LimnorProject.createVobRootNode(d);// Project.CreateVobRoot();
						if (vobNode != null)
						{
							w.WriteStaticProperties(vobNode);
							d.Save();
						}
					}
				}
			}
			XmlNodeList nl = SerializeUtil.GetClassRefNodeList(Node);
			foreach (XmlNode nd in nl)
			{
				XmlNode np = nd.ParentNode;
				np.RemoveChild(nd);
			}
			IList<IClassRef> classes = ObjectMap.ClassRefList;
			if (classes != null)
			{
				foreach (IClassRef cr in classes)
				{
					XmlNode nd = SerializeUtil.CreateClassRefNode(Node);
					w.WriteObjectToNode(nd, cr, false);
				}
			}
			//write custom property values
			GetRootId().SaveCustomPropertyValues(w);
			//
			if (w.HasErrors)
			{
				MathNode.Log(w.ErrorCollection);
			}
			if (ViewerHolder != null)
			{
				ViewerHolder.WriteConfig(Node);
			}
			//notify IDE
			sendVOBnotice(enumVobNotice.ObjectChanged, this.GetRootId());

		}
		private bool saveToXml(IDesignerSerializationManager serializationManager)
		{
			if (_isDisposed)
				return false;
			try
			{
				saveRoot(serializationManager);
				return true;
			}
			catch (Exception e)
			{
				_errorSaving = true;
				MathNode.Log(TraceLogClass.MainForm, e);
				string f = XmlUtil.GetAttribute(this.Node, XmlTags.XMLATT_filename);
				if (!string.IsNullOrEmpty(f) && File.Exists(f))
				{
					string fbk = string.Format(CultureInfo.InvariantCulture, "{0}.backup", f);
					try
					{
						File.Copy(f, fbk);
						MessageBox.Show(TraceLogClass.MainForm, string.Format(CultureInfo.InvariantCulture,
							"A backup file [{0}] has been created for [{1}]. You may close and re-open [{2}] and if it appears damaged then you may copy [{0}] to [{1}] to restore the file.", fbk, f, Path.GetFileNameWithoutExtension(f)),
							"File Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception err)
					{
						MessageBox.Show(TraceLogClass.MainForm, string.Format(CultureInfo.InvariantCulture,
							"You may want to make a backup file for [{0}] now. You may close and re-open [{1}] and if it appears damaged then you may restore the file [{0}] from your backup.\r\n\r\nAutomatic backup to [{2}] failed with error [{3}].", f, Path.GetFileNameWithoutExtension(f), fbk, err.Message),
							"File Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
			}
			return false;
		}
		public void NotifyChanges()
		{
			if (!_isFlushing)
			{
				this.Modified = true;
				try
				{
					this.doFlush();
				}
				catch
				{
				}
				this.Modified = true;
			}
		}

		public void NotifySelection(object selectedObject)
		{
			if (selectedObject == null)
			{
				if (_dummy == null)
				{
					_dummy = new object();
				}
				selectedObject = _dummy;
			}
			IComponent ic = selectedObject as IComponent;
			if (ic == null)
			{
				ISelectionService svc = (ISelectionService)Host.GetService(typeof(ISelectionService));
				svc.SetSelectedComponents(new object[] { });
				ViewerHolder.SetObjectToPropertyGrid(selectedObject);
			}
			else
			{
				ISelectionService svc = (ISelectionService)Host.GetService(typeof(ISelectionService));
				svc.SetSelectedComponents(new object[] { ic });
			}
			HtmlElement_BodyBase heb = selectedObject as HtmlElement_BodyBase;
			if (heb != null)
			{
				sendVOBnotice(enumVobNotice.HtmlElementSelected, heb);
			}
		}
		public ClassPointer GetRootId()
		{
			initialize();
			return _rootPointer;
		}
		/// <summary>
		/// called after compile to restore the map
		/// </summary>
		public void ResetObjectMap()
		{
			if (_rootPointer != null && _objMap != null && ViewerHolder != null)
			{
				_rootPointer.SetData(_objMap);
				_objMap.SetTypedData<ObjectIDmap>(_objMap);
				_objMap.SetTypedData<ClassPointer>(_rootPointer);
				ViewerHolder.OnResetMap();
			}
		}
		public MemberComponentId GetMemberId(object obj)
		{
			MemberComponentId member = MemberComponentId.CreateMemberComponentId(GetRootId(), obj, this.ObjectMap.GetObjectID(obj));
			return member;
		}
		#endregion

		#region ILimnorDesigner Members
		Dictionary<string, UInt32> _methodNames; //only id and names
		public IObjectPointer SelectDataType(Form caller)
		{
			return DesignUtil.SelectDataType(Project, this.GetRootId(), null, EnumObjectSelectType.Type, null, null, null, caller);
		}
		public void ClearMethodNames()
		{
			_methodNames = null;
		}
		public bool IsMethodNameUsed(UInt32 methodId, string name)
		{
			if (_methodNames == null)
			{
				_methodNames = DesignUtil.GetMethodNames(Node);
			}
			UInt32 id;
			if (_methodNames.TryGetValue(name, out id))
			{
				return (id != methodId);
			}
			return false;
		}

		public string CreateMethodName(string baseName, StringCollection names)
		{
			return CreateNewComponentName(baseName, names);
		}
		public IXmlCodeReader CodeReader
		{
			get
			{
				if (_rootPointer != null)
					return _rootPointer.CreateReader();
				return null;
			}
		}

		public IXmlCodeWriter CodeWriter
		{
			get
			{
				if (_writer != null)
					return _writer;
				if (_rootPointer != null)
					return _rootPointer.CreateWriter();
				return null;
			}
		}
		#endregion

		#region IChangeControl Members

		public bool Dirty
		{
			get
			{
				WebPage wpage = _rootComponent as WebPage;
				if (wpage != null)
				{
					if (wpage.HtmlChanged)
					{
						return true;
					}
				}
				return DataModified;
			}
			set
			{
				DataModified = value;
			}
		}

		public void ResetModified()
		{
			DataModified = false;
		}

		#endregion

		#region IPropertyListChangeNotify Members

		public void OnPropertyListChanged(object owner)
		{
			ViewerHolder.OnPropertyListChanged(owner);
		}

		#endregion

	}
}
