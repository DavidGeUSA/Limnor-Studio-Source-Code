/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Designer Host
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using LimnorDesigner;
using VSPrj;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Limnor.Drawing2D;
using System.ComponentModel.Design;
using System.Drawing;
using VPL;
using LimnorDesigner.Property;
using XmlUtility;
using System.Xml;
using LimnorDatabase;
using MathExp;
using System.Reflection;
using LimnorDesigner.EventMap;
using System.Web.Services.Protocols;
using LimnorDesigner.ResourcesManager;
using System.Windows.Forms.Design;
using System.ComponentModel;
using XmlSerializer;
using LimnorDesigner.MenuUtil;
using WindowsUtility;
using System.Collections.Specialized;
using LimnorDesigner.Interface;
using System.Data;
using System.IO;
using System.Drawing.Design;
using VOB;
using Limnor.Reporting;
using LimnorDesigner.Event;
using Limnor.CopyProtection;
using PerformerImport;
using LimnorDesigner.MethodBuilder;
using LimnorDesigner.Remoting;
using LimnorDatabase.DataTransfer;
using XToolbox2;
using System.Globalization;
using Limnor.WebBuilder;
using LimnorDesigner.Web;
using LimnorDesigner.Action;
using TraceLog;

namespace XHost
{
	delegate void fnFunctionBool(bool val);
	public class LimnorXmlPane2 : UserControl, ILimnorDesignPane, IDesignPane
	{
		#region types and interoper
		enum EnumTimerAction { None, ReadConfig, AdjustSelection, FindPropertyGrid, SelectPropertyGridItem, XTypeCreation, ClassRefCreation }
		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
		private ToolboxItemXType _newObject;
		private ClassInstancePointer _newClassRef;
		[DllImport(ExternDll.Gdi32)]
		static extern bool DeleteObject(IntPtr hObj);
		//
		[DllImport(ExternDll.User32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
		[DllImport(ExternDll.User32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
		[DllImport(ExternDll.User32)]
		static extern int GetWindowTextLength(IntPtr hWnd);
		[DllImport(ExternDll.User32)]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		#endregion

		#region fields and constructors
		private static bool _toolsLoaded;
		private Timer _tmReadConfig;
		private string _propertyItemName;
		private EnumTimerAction _timerAction = EnumTimerAction.ReadConfig;
		private MultiPanes _pane;
		private SetupContentsHolder _setupControl;
		private bool _newComponent;
		private LimnorXmlDesignerLoader2 _loader;
		private ILimnorToolbox _toolboxService;
		private Image _objectIcon;
		private MenuCommandList _contextMenus;
		private bool _vsPropertyGridHooked;
		private int _propertyGridHookCount;
		private PropertyGrid _propertyGrid;
		private FormLanguageIcon _langIndicator;
		private DesignSurface _surface;
		private Dictionary<string, Type> _loadedTypes;
		public const string WrappedType = "WrappedType";
		//
		public LimnorXmlPane2(DesignSurface surface, LimnorXmlDesignerLoader2 loader)
		{
			bool bOK = false;

			_loader = loader;
			_pane = new MultiPanes();
			_pane.Owner = this;
			_pane.Dock = DockStyle.Fill;
			_pane.Loader = _loader;
			_loader.ViewerHolder = this;
			_loader.Project.SetTypedData<ILimnorDesignPane>(_loader.ClassID, this);
			_surface = surface;

			bOK = InitializeControl();

			if (bOK)
			{
				_tmReadConfig = new Timer();
				_tmReadConfig.Enabled = false;
				_tmReadConfig.Interval = 200;
				_tmReadConfig.Tick += new EventHandler(_tmReadConfig_Tick);
				SetupCommandHandlers();

				PropertyGrid pg = findVsPropertyGrid();
				if (pg != null)
				{
					pg.PropertyValueChanged += new PropertyValueChangedEventHandler(pg_PropertyValueChanged);
					pg.SelectedObjectsChanged += new EventHandler(pg_SelectedObjectsChanged);
					pg.SelectedGridItemChanged += new SelectedGridItemChangedEventHandler(pg_SelectedGridItemChanged);
					Form f = pg.FindForm();
					if (f != null)
					{
						f.KeyDown += f_KeyDown;
						f.KeyPreview = true;
					}
					_vsPropertyGridHooked = true;
				}
			}
		}

		void f_KeyDown(object sender, KeyEventArgs e)
		{
			if (_isImageProp)
			{
				if (e.KeyCode == Keys.Back)
				{
					if (imgProp != null)
					{
						PropertyGrid pg = hookVsPropertyGrid();
						if (pg != null)
						{
							if (pg.SelectedObject != null)
							{
								try
								{
									imgProp.ResetValue(pg.SelectedObject);
								}
								catch (Exception err)
								{
									MessageBox.Show(this.FindForm(), string.Format(CultureInfo.InvariantCulture, "Error removing image. {0}", err.Message), "Reset image", MessageBoxButtons.OK, MessageBoxIcon.Error);
								}
							}
						}
					}
				}
			}
		}

		#endregion

		#region private methods
		private bool _isImageProp = false;
		private PropertyDescriptor imgProp = null;
		void pg_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
		{
			_isImageProp = false;
			if (e.NewSelection != null && e.NewSelection.PropertyDescriptor != null)
			{
				if (typeof(Image).IsAssignableFrom(e.NewSelection.PropertyDescriptor.PropertyType))
				{
					_isImageProp = true;
					imgProp = e.NewSelection.PropertyDescriptor;
				}
			}
		}
		void pg_SelectedObjectsChanged(object sender, EventArgs e)
		{
			if (_selectedCustomObject != null && _loader.ClassID == LimnorXmlDesignerLoader2.ActiveItemID)
			{
				PropertyGrid pg = hookVsPropertyGrid();
				if (pg != null)
				{
					if (pg.SelectedObjects != null && pg.SelectedObjects.Length == 1)
					{
						if (pg.SelectedObject != _selectedCustomObject)
						{
							ClassProperties cp = _selectedCustomObject as ClassProperties;
							if (cp != null)
							{
								if (cp.Pointer == pg.SelectedObject)
								{
									pg.SelectedObject = _selectedCustomObject;
								}
							}
							else
							{
								ClassPointer rp = _selectedCustomObject as ClassPointer;
								if (rp != null)
								{
									if (pg.SelectedObject == rp.ObjectInstance)
									{
										pg.SelectedObject = _selectedCustomObject;
									}
								}
							}
						}
					}
				}
			}
		}

		void pg_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			if (_loader == null || _loader.ActiveXmlDesignerLoader == null)
			{
				return;
			}
			if (_loader.ClassID != _loader.ActiveXmlDesignerLoader.ClassID)
			{
				return;
			}
			if (e.ChangedItem != null && e.ChangedItem != null && e.ChangedItem.Parent != null)
			{
				if (IsWebPage)
				{
					GridItem vp = e.ChangedItem.Parent;
					HtmlElement_BodyBase hbb = vp.Value as HtmlElement_BodyBase;
					while (hbb == null && vp.Parent != null)
					{
						vp = vp.Parent;
						hbb = vp.Value as HtmlElement_BodyBase;
					}
					if (hbb != null)
					{
						_loader.GetRootId().OnUseHtmlElement(hbb);
						_loader.DataModified = true;
					}
				}
				object val = ComponentInterfaceWrapper.GetObject(e.ChangedItem.Parent.Value);
				RootClass.OnPropertyChanged(val, e.ChangedItem.PropertyDescriptor.Name, e.ChangedItem.Value);
				ClassPointer p = val as ClassPointer;
				if (p != null)
				{
					PropertyClassDescriptor pcd = e.ChangedItem.PropertyDescriptor as PropertyClassDescriptor;
					if (pcd == null) //not a custom property
					{
						bool iconChanged = false;
						if (p.ObjectInstance is Form)
						{
							if (string.Compare(e.ChangedItem.PropertyDescriptor.Name, XmlTags.XML_Icon, StringComparison.Ordinal) == 0)
							{
								//Form's Icon property changed. Remove Icon Xml Node.
								XmlNode nodeIcon = _loader.Node.SelectSingleNode(XmlTags.XML_Icon);
								if (nodeIcon != null)
								{
									_loader.Node.RemoveChild(nodeIcon);
								}
								p.ImageIcon = null;
								iconChanged = true;
							}
						}
						_loader.NotifyChanges();
						if (iconChanged)
						{
							OnIconChanged();
							_pane.OnIconChanged(p.ClassId);
						}
					}
				}
				else
				{
					ClassProperties cp = val as ClassProperties;
					if (cp != null)
					{
						ClassInstancePointer ci = cp.Pointer as ClassInstancePointer;
						if (ci == null)
						{
							throw new DesignerException("Property change: ClassInstancePointer isnull. {0}", cp.GetClassName());
						}
						ICustomEventMethodType cem = ci.ObjectInstance as ICustomEventMethodType;
						if (cem != null)
						{
							PropertyClassDescriptor pcd = e.ChangedItem.PropertyDescriptor as PropertyClassDescriptor;
							if (pcd == null) //not a custom property
							{
								_loader.NotifyChanges();
							}
						}
					}
					else
					{
						IDrawDesignControl draw = val as IDrawDesignControl;
						if (draw != null)
						{
							FormViewer fv = _pane.GetDesigner<FormViewer>();
							if (fv != null)
							{
								fv.RefreshViewer();
								_loader.NotifyChanges();
							}
						}
						else
						{
							DrawingLayer layer = val as DrawingLayer;
							if (layer != null)
							{
								layer.Page.Refresh();
								_loader.NotifyChanges();
							}
							else
							{
								ConnectionItem ci = val as ConnectionItem;
								if (ci != null)
								{
									ci.Save();
									if (string.CompareOrdinal(e.ChangedItem.PropertyDescriptor.Name, "Name") == 0)
									{
										Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
										foreach (ILimnorDesignPane pn in designPanes.Values)
										{
											pn.OnDatabaseConnectionNameChanged(ci.ConnectionGuid, ci.Name);
										}
									}
								}
								else
								{
									GridItem gi;
									//ActionClass?
									gi = e.ChangedItem;
									ActionClass act = ComponentInterfaceWrapper.GetObject(gi.Value) as ActionClass;
									while (act == null && gi.Parent != null)
									{
										gi = gi.Parent;
										act = ComponentInterfaceWrapper.GetObject(gi.Value) as ActionClass;
									}
									if (act != null)
									{
										if (string.Compare(e.ChangedItem.PropertyDescriptor.Name, "ActionMethod", StringComparison.Ordinal) == 0)
										{
											act.ResetScopeMethod();
											if (_propertyGrid != null)
											{
												_propertyGrid.Refresh();
											}
										}
										act.UpdateXmlNode(_loader.Writer);
										_loader.NotifyChanges();
										if (string.Compare(e.ChangedItem.PropertyDescriptor.Name, "ActionName", StringComparison.Ordinal) == 0)
										{

										}
									}
									else
									{
										MethodClass mc = val as MethodClass;
										if (mc == null)
										{
											mc = gi.Value as MethodClass;
										}
										if (mc != null)
										{
											mc.UpdateXmlNode(_loader.Writer);
											if (string.CompareOrdinal(e.ChangedItem.PropertyDescriptor.Name, "Name") != 0)
											{
												_pane.OnMethodChanged(mc, false);
												if (_detachedViewers != null)
												{
													foreach (IXDesignerViewer v in _detachedViewers)
													{
														v.OnMethodChanged(mc, false);
													}
												}
											}
										}
										else
										{
											AttributeConstructor ac = val as AttributeConstructor;
											if (ac == null)
											{
												ac = gi.Value as AttributeConstructor;
											}
											if (ac != null)
											{
												ac.UpdateXml(_loader.Writer);
											}
											else
											{
												INotifyComponentChanged ncc = gi.Value as INotifyComponentChanged;
												if (ncc != null)
												{
													if (ncc.OnComponentChanged(e))
													{
														if (_propertyGrid != null)
														{
															_propertyGrid.Refresh();
														}
													}
												}
												else
												{
													IDesignerRefresh ctrl = gi.Value as IDesignerRefresh;
													if (ctrl != null)
													{
														ctrl.OnPropertyChangedInDesigner(e.ChangedItem.PropertyDescriptor.Name);
													}
													else
													{
														AttributeConstructor abc = gi.Value as AttributeConstructor;
														if (abc != null)
														{
															abc.UpdateXml(_loader.Writer);
														}
														else
														{
															ConnectionItem cni = gi.Value as ConnectionItem;
															if (cni != null)
															{
																cni.Save();
																return;
															}
														}
													}
												}
											}
										}
										_loader.NotifyChanges();
										OnResetDesigner(_loader.RootObject);
									}
								}
							}
						}
					}
				}
			}
		}
		void _tmReadConfig_Tick(object sender, EventArgs e)
		{
			_tmReadConfig.Enabled = false;
			switch (_timerAction)
			{
				case EnumTimerAction.ReadConfig:
					try
					{
						_timerAction = EnumTimerAction.None;
						if (_newComponent)
						{
							LimnorProject prj = _loader.Project;
							if (prj != null)
							{
								string newName = prj.GetNewComponentName(_loader.DocumentMoniker);
								XmlUtil.SetAttribute(_loader.Node, XmlTags.XMLATT_NAME, newName);
								XmlNode node = _loader.Node.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
									"{0}[@{1}='Name']", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
								if (node != null)
								{
									node.InnerText = newName;
								}
								_pane.SaveWidths();
								_pane.WriteConfig(_loader.Node);
								_loader.Host.RootComponent.Site.Name = newName;
								_loader.NotifyChanges();
								SaveAll();
								_pane.SetLoaded();
								prj.SetTypedData<ILimnorDesignPane>(_loader.ClassID, this);
								prj.DesignPaneCreated(this);
								InitToolbox();

							}
						}
						else
						{
							if (_pane != null)
							{
								//it will call SetLoaded()
								_pane.ApplyConfig();
							}
						}
						ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
						if (selectionService != null)
						{
							selectionService_SelectionChanged(selectionService, EventArgs.Empty);
						}
						_loader.OnDesignPaneLoaded();
						DrawingPage dpg = _loader.RootObject as DrawingPage;
						if (dpg != null)
						{
							dpg.NotifySelectionChange = onDrawingSelectionChanged;
						}
					}
					catch (Exception err)
					{
						MathNode.LogError(DesignerException.FormExceptionText(err));
					}
					break;
				case EnumTimerAction.AdjustSelection:
					_timerAction = EnumTimerAction.None;
					PropertyGrid pg2 = hookVsPropertyGrid();
					if (pg2 != null)
					{
						try
						{
							VPLUtil.AdjustImagePropertyAttribute(_selectedCustomObject);
							pg2.SelectedObject = _selectedCustomObject;
						}
						catch
						{
						}
					}
					break;
				case EnumTimerAction.FindPropertyGrid:
					_timerAction = EnumTimerAction.None;
					hookVsPropertyGrid();
					break;
				case EnumTimerAction.SelectPropertyGridItem:
					_timerAction = EnumTimerAction.None;
					if (_propertyGrid != null)
					{
						VPLUtil.SelectSiblingPropertyGridItemByName(_propertyGrid, _propertyItemName);
					}
					break;
				case EnumTimerAction.XTypeCreation:
					_timerAction = EnumTimerAction.None;
					DesignUtil.SwitchXType(_newObject, Loader, this.FindForm());
					break;
				case EnumTimerAction.ClassRefCreation:
					_timerAction = EnumTimerAction.None;
					DesignUtil.SwitchClassRef(_newClassRef, Loader, this.FindForm());
					break;
			}

		}
		private bool InitializeControl()
		{
			FormLoadProgress.SetProgInfo("Initialize ...");
			bool bRet = false;

			if (_loader.IsSetup)
			{
				_setupControl = SetupContentsHolder.CreateSetupContentsHolder();
				if (_setupControl != null)
				{
					_setupControl.Dock = DockStyle.Fill;
					bool bXmlChanged = _setupControl.LoadData(_loader.Node, _loader.DocumentMoniker);
					if (bXmlChanged)
					{
						_loader.Dirty = true;
					}
					_setupControl.PropertyChanged += new EventHandler(_setupControl_PropertyChanged);
					PropertyGrid pg = findVsPropertyGrid();
					if (pg != null)
					{
						_setupControl.SetPropertyGrid(pg);
					}
				}
				FormLoadProgress.CloseProgress();
			}
			else
			{
				LimnorProject prj = Project;
				if (_loader.ObjectMap != null)
				{
					Control c = (Control)Surface2.View;
					try
					{
						if (!_loader.IsControl)
						{
							for (int i = 0; i < c.Controls.Count; i++)
							{
								Type t = c.Controls[0].GetType();
								if (t.Name.EndsWith("WatermarkLabel"))
								{
									PropertyInfo pif = t.GetProperty("Links");
									if (pif != null)
									{
										object v = pif.GetValue(c.Controls[i], null);
										if (v != null)
										{
											t = v.GetType();
											MethodInfo mif = t.GetMethod("Clear");
											if (mif != null)
											{
												mif.Invoke(v, null);
											}
										}
									}
									c.Controls[0].Text = "To add a component to this class, drag the component from the toolbox and drop here, or right-click this pane and choose 'Add component'.";
									break;
								}
							}
						}
					}
					catch
					{
					}
					_loader.Project.VerifyWebSite(c.FindForm());
					if (VisualProgrammingDesigner.DesignerEnabled(typeof(FormViewer)))
					{
						FormViewer fv = new FormViewer(c);
						_pane.AddViewer(fv);
					}
					//
					if (VisualProgrammingDesigner.DesignerEnabled(typeof(ObjectExplorerView)))
					{
						ObjectExplorerView ov = new ObjectExplorerView(_loader);
						_pane.AddViewer(ov);
					}
					//
					if (VisualProgrammingDesigner.DesignerEnabled(typeof(EventPathHolder)))
					{
						EventPathHolder ep = new EventPathHolder();
						_pane.AddViewer(ep);
					}
					//
					FormLoadProgress.SetProgInfo("Loading plugins ...");
					//load plugins
					Dictionary<string, VisualProgrammingDesigner> designers = VisualProgrammingDesigner.Designers;
					foreach (VisualProgrammingDesigner p in designers.Values)
					{
						try
						{
							if (!p.IsBuiltIn())
							{
								Type t = p.GetDesignerType();
								if (t == null)
								{
									MessageBox.Show(p.Error, "Load Designer", MessageBoxButtons.OK, MessageBoxIcon.Error);
								}
								else
								{
									if (!VisualProgrammingDesigner.DesignerBuiltIn(t))
									{
										IXDesignerViewer ix = (IXDesignerViewer)Activator.CreateInstance(t);
										_pane.AddViewer(ix);
									}
								}
							}
						}
						catch (Exception err)
						{
							if (p != null)
							{
								MessageBox.Show(err.Message, "Load Plugin:" + p.Name);
							}
							else
							{
								MessageBox.Show(err.Message, "Load Plugin");
							}
						}
					}
					//
					FormLoadProgress.SetProgInfo("Loading viewers ...");
					//
					ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
					if (selectionService != null)
					{
						selectionService.SelectionChanged += new EventHandler(selectionService_SelectionChanged);
					}
					_pane.OnDataLoaded();
					//
					FormLoadProgress.SetProgInfo("Loading Toolbox ...");
					//
					if (prj != null)
					{
						prj.SetTypedData<ILimnorDesignPane>(_loader.ClassID, this);
						prj.DesignPaneCreated(this);
						InitToolbox();
					}
					else
					{
						SaveAll();
						_newComponent = true;
					}
					///
					FormLoadProgress.SetProgInfo("Loading tray icons ...");
					//
					if (_loader.UIDesignerNotNeeded)
					{
						HideUIDesigner();
					}
					adjustTrayControlIcon(0, null);
					adjustTrayControlIconByType(typeof(SoapHttpClientProtocol), Resource1.cloud);
					//
					foreach (object obj in _loader.ObjectMap.Keys)
					{
						ICustomEventMethodDescriptor em = obj as ICustomEventMethodDescriptor;
						if (em != null)
						{
							em.SetEventChangeMonitor(onCustomEventListChanged);
						}
					}
					//
					bRet = true;
				}
				else
				{
					bRet = false;
				}
				FormLoadProgress.CloseProgress();
				if (bRet)
				{
					IList<Guid> l = Loader.GetRootId().GetDatabaseConnectionsUsed();
					if (l.Count > 0)
					{
						bool bValid = true;
						List<ConnectionItem> cnns = new List<ConnectionItem>();
						foreach (Guid g in l)
						{
							ConnectionItem ci = ConnectionItem.LoadConnection(g, false, false);
							if (ci != null)
							{
								cnns.Add(ci);
								if (!ci.IsValid)
								{
									if (ci.HasConfiguration)
									{
										bValid = false;
									}
								}
							}
						}
						if (!bValid)
						{
							MessageBox.Show(_pane.FindForm(), "One or more database connections point to invalid databases. Please adjust database connections", "Database connections", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							DlgConnectionManager dlgDbMan = new DlgConnectionManager();
							dlgDbMan.SetConnectionList(cnns);
							dlgDbMan.ShowDialog(_pane.FindForm());
						}
						LimnorProject.MergeObjectGuidList(l);
					}
				}
				if (bRet)
				{
					IDesignerRefresh idr = null;
					List<IDesignTimeAccess> idalist = new List<IDesignTimeAccess>();
					foreach (object obj in _loader.ObjectMap.Keys)
					{
						if (idr == null)
						{
							idr = obj as IDesignerRefresh;
						}
						IDesignTimeAccess ida = obj as IDesignTimeAccess;
						if (ida != null)
						{
							if (ida.QueryOnStart)
							{
								idalist.Add(ida);

							}
						}
					}
					foreach (IDesignTimeAccess ida in idalist)
					{
						try
						{
							ida.Query();
						}
						catch (Exception err)
						{
							MathNode.Log(TraceLogClass.MainForm, err);
						}
					}
					if (idr != null)
					{
						try
						{
							idr.OnPropertyChangedInDesigner(string.Empty);
						}
						catch (Exception err)
						{
							MathNode.Log(TraceLogClass.MainForm, err);
						}
					}
				}
				if (prj != null)
				{
					ProjectResources rm = prj.GetProjectSingleData<ProjectResources>();
					if (rm.Languages.Count > 0)
					{
						loadLanguageIndicator();
					}
					rm.NotifyLanguageChange();
				}
				Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
				foreach (ILimnorDesignPane pn in designPanes.Values)
				{
					ClassPointer cp = pn.RootClass;
					if (cp.ClassId != _loader.ClassID)
					{
						pn.OnClassLoadedIntoDesigner(_loader.ClassID);
					}
				}
			}
			return bRet;
		}

		void _setupControl_PropertyChanged(object sender, EventArgs e)
		{
			_loader.DataModified = true;
		}
		private void onCustomEventListChanged(object sender, EventArgs e)
		{
			ICustomEventMethodDescriptor em = sender as ICustomEventMethodDescriptor;
			UInt32 objectId = _loader.ObjectMap[em];
			OnEventListChanged(em, objectId);
		}

		private void adjustTrayControlIconByType(Type type, Image img)
		{
			Control c = (Control)Surface2.View;
			for (int i = 0; i < c.Controls.Count; i++)
			{
				ComponentTray ct = c.Controls[i] as ComponentTray;
				if (ct != null)
				{
					for (int j = 0; j < ct.Controls.Count; j++)
					{
						adjustTrayControlIconByType(ct.Controls[j], type, img);
					}
					break;
				}
				else
				{
					adjustTrayControlIconByType(c.Controls[i], type, img);
				}
			}
		}
		/// <summary>
		/// when a ClassRef is in the ComponentTray, it is a TrayControl.
		/// we need to change its icon
		/// </summary>
		/// <param name="c">a TrayControl</param>
		private void adjustTrayControlIconByType(Control c, Type type, Image img)
		{
			Type tpC = c.GetType();
			if (string.Compare(tpC.Name, "TrayControl", StringComparison.Ordinal) == 0)
			{
				FieldInfo f = tpC.GetField("component", BindingFlags.Instance | BindingFlags.NonPublic);
				object v = f.GetValue(c);
				if (v != null)
				{
					if (type.IsAssignableFrom(v.GetType()))
					{
						f = tpC.GetField("toolboxBitmap", BindingFlags.Instance | BindingFlags.NonPublic);
						f.SetValue(c, img);
					}
				}
			}
		}


		private void adjustTrayControlIcon(UInt64 id, Image img)
		{
			Control c = (Control)Surface2.View;
			for (int i = 0; i < c.Controls.Count; i++)
			{
				ComponentTray ct = c.Controls[i] as ComponentTray;
				if (ct != null)
				{
					for (int j = 0; j < ct.Controls.Count; j++)
					{
						adjustTrayControlIcon(ct.Controls[j], id, img);
					}
					break;
				}
				else
				{
					adjustTrayControlIcon(c.Controls[i], id, img);
				}
			}
		}
		/// <summary>
		/// when a ClassRef is in the ComponentTray, it is a TrayControl.
		/// we need to change its icon
		/// </summary>
		/// <param name="c">a TrayControl</param>
		private void adjustTrayControlIcon(Control c, UInt64 id, Image img)
		{
			Type tpC = c.GetType();
			if (string.Compare(tpC.Name, "TrayControl", StringComparison.Ordinal) == 0)
			{
				FieldInfo f = tpC.GetField("component", BindingFlags.Instance | BindingFlags.NonPublic);
				IComponent ic = (IComponent)f.GetValue(c);
				if (ic != null)
				{
					IClassRef cr = _loader.GetClassRefByComponent(ic);
					if (cr == null)
					{
						if (img == null)
						{
							Type t = VPLUtil.GetObjectType(ic.GetType());
							img = VPLUtil.GetTypeIcon(t);
						}
						if (img != null)
						{
							f = tpC.GetField("toolboxBitmap", BindingFlags.Instance | BindingFlags.NonPublic);
							f.SetValue(c, img);
						}
					}
					else
					{
						UInt32 classId = 0;
						UInt32 memberId = 0;
						if (id != 0)
						{
							DesignUtil.ParseDDWord(id, out memberId, out classId);
						}
						if (id == 0 || cr.WholeId == id || (memberId == 0 && classId == cr.ClassId))
						{
							if (img == null)
							{
								img = cr.ImageIcon;
							}
							if (img != null)
							{
								f = tpC.GetField("toolboxBitmap", BindingFlags.Instance | BindingFlags.NonPublic);
								f.SetValue(c, img);
							}
						}
					}
				}
			}
		}

		object _selectedCustomObject;
		object _currentSelectedObject;
		/// <summary>
		/// construct context menu for the selected component
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void selectionService_SelectionChanged(object sender, EventArgs e)
		{
			if (IsClosing)
				return;
			//
			HideToolbox();
			//
			ISelectionService s = sender as ISelectionService;
			if (s.PrimarySelection is ToolboxItemXType)
				return;
			if (s.PrimarySelection is ClassInstancePointer)
				return;
			//
			_timerAction = EnumTimerAction.None;
			hookVsPropertyGrid();
			_selectedCustomObject = null;
			//
			if (_contextMenus == null)
			{
				_contextMenus = new MenuCommandList();
			}
			Object c = s.PrimarySelection;
			_currentSelectedObject = c;
			if (c != null)
			{
				IComponent ic = c as IComponent;
				if (ic != null)
				{
					_loader.sendVOBnotice(enumVobNotice.ComponentSelected, ic);
				}
				UndoEngine undoEngine = (UndoEngine)this.Loader.Host.GetService(typeof(UndoEngine));
				if (undoEngine != null)
				{
					if (c is ToolStrip)
						undoEngine.Enabled = false;
					else if (c is ToolStripItem)
						undoEngine.Enabled = false;
					else
						undoEngine.Enabled = true;
				}
				if (s.SelectionCount == 1)
				{
					if (c is ClassProperties)
						return;
					if (c is PropertyRepresenter)
						return;
					if (c != _loader.RootObject)
					{
						ClassInstancePointer cr = c as ClassInstancePointer;
						if (cr == null)
						{
							if (ic != null)
							{
								cr = _loader.GetClassRefByComponent(ic) as ClassInstancePointer;
							}
						}
						if (cr != null)
						{
							_selectedCustomObject = new ClassProperties(cr);
							if (_timerAction != EnumTimerAction.ReadConfig)
							{
								_timerAction = EnumTimerAction.AdjustSelection;
							}
						}
						else
						{
							if (_loader.IsWebPage)
							{
								IWebClientControl icc = c as IWebClientControl;
								if (icc == null)
								{
									TabPage tp = c as TabPage;
									if (tp != null)
									{
										_selectedCustomObject = new RepresenterTabPage(tp);
										if (_timerAction != EnumTimerAction.ReadConfig)
										{
											_timerAction = EnumTimerAction.AdjustSelection;
										}
									}
								}
							}
						}
					}
				}
				INonHostedObject nho = c as INonHostedObject;
				if (nho != null)
				{
					nho.SetChangeEvents(new EventHandler(nho_NameChanging), new EventHandler(nho_PropertyChanged));
				}
				ICustomEventMethodType cem = c as ICustomEventMethodType;
				if (cem != null)
				{
					cem.HookCustomPropertyValueChange(new EventHandler(nho_PropertyChanged));
				}
				if (_contextMenus.Owner != c)
				{
					_contextMenus.Owner = c;
					recreateContextMenu();
				}
				//adjust image editor ===============================================
				VPLUtil.AdjustImagePropertyAttribute(c);
				//======================================================
				//notify selection
				_pane.OnComponentSelected(c);
				//
			}
			Control ctrl = c as Control;
			if (ctrl != null)
			{
				ctrl.Refresh();
				FormViewer fv = _pane.GetDesigner<FormViewer>();
				if (fv != null)
				{
					fv.RefreshViewer();
				}
			}

			if (_timerAction != EnumTimerAction.None)
			{
				_tmReadConfig.Enabled = true;
			}
			else
			{
				if (_propertyGrid != null && _propertyGrid.Parent != null)
				{
					_propertyGrid.SelectedObject = c;
				}
			}
		}
		void resetContextMenu()
		{
			if (_contextMenus != null && _contextMenus.Owner == this.RootClass.ObjectInstance)
			{
				recreateContextMenu();
			}
			else
			{
				LimnorContextMenuCollection.RemoveMenuCollection(RootClass);
			}
		}
		void onDrawingSelectionChanged(object sender, EventArgs e)
		{
			ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
			if (selectionService != null)
			{
				selectionService.SetSelectedComponents(new object[] { sender });
			}
		}
		void nho_PropertyChanged(object sender, EventArgs e)
		{
			PropertyChangeEventArg pc = e as PropertyChangeEventArg;
			if (pc != null)
			{
				INonHostedObject nc = sender as INonHostedObject;
				if (nc != null)
				{
					ActionAttachEvent aae = sender as ActionAttachEvent;
					if (aae != null)
					{
						ClassPointer root = _loader.GetRootId();
						root.SaveAction(aae, _loader.Writer);
					}
					else
					{
						ActionClass ac = sender as ActionClass;
						if (ac != null)
						{
							if (_propertyGrid != null && _propertyGrid.SelectedObject == ac)
							{
								if (ac.ActionMethod != null && ac.ActionMethod.Owner != null)
								{
									IMethodParameterAttributesProvider dmp = ac.ActionMethod.Owner.ObjectInstance as IMethodParameterAttributesProvider;
									if (dmp != null)
									{
										Dictionary<string, Attribute[]> pap = dmp.GetParameterAttributes(ac.ActionMethod.MethodName);
										if (pap != null && pap.Count > 0)
										{
											_propertyGrid.Refresh();
										}
									}
								}
							}
							ClassPointer root = _loader.GetRootId();
							root.SaveAction(ac, _loader.Writer);
						}
						else
						{
							//update XML
							object prop = null;
							PropertyChangeEventArg pcea = e as PropertyChangeEventArg;
							if (pcea != null)
							{
								prop = pcea.Property;
								if (string.CompareOrdinal(pc.Name, "ExceptionHandlers") == 0)
								{
									ExceptionHandler eh = prop as ExceptionHandler;
									if (eh != null)
									{
										_propertyItemName = eh.ExceptionType.Name;
										_timerAction = EnumTimerAction.SelectPropertyGridItem;
										_tmReadConfig.Start();
									}
								}
							}
							nc.OnPropertyChanged(pc.Name, prop, _loader.Node, _loader.Writer);
						}
					}
					//update designer text
					_loader.NotifyChanges();
					//notify all viewers
					IList<ILimnorDesignPane> designPanes = _loader.GetCurrentDesignPanes();
					foreach (ILimnorDesignPane p in designPanes)
					{
						p.OnPropertyChanged(nc, pc.Name);
					}
				}
			}
		}
		void nho_NameChanging(object sender, EventArgs e)
		{
			NameBeforeChangeEventArg nc = e as NameBeforeChangeEventArg;
			if (nc != null)
			{
				if (_loader.IsNameUsed(nc.NewName, 0))
				{
					nc.Cancel = true;
					MessageBox.Show(_pane.FindForm(), string.Format(System.Globalization.CultureInfo.InvariantCulture, "The name '{0}' is in use", nc.NewName), "Change name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				else if (nc.IsIdentifierName && !VOB.VobUtil.IsGoodVarName(nc.NewName))
				{
					nc.Cancel = true;
					MessageBox.Show(_pane.FindForm(), string.Format(System.Globalization.CultureInfo.InvariantCulture, "The name '{0}' is invalid. Use only alphanumeric letters and underscores. The first letter must be an English letter.", nc.NewName), "Change name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}
		private void loadLanguageIndicator()
		{
			_langIndicator = new FormLanguageIcon();
			_langIndicator.TopLevel = true;
			_langIndicator.Location = new Point(0, 0);
			_langIndicator.Show();
			UnsafeNativeMethods.SetParent(_langIndicator.Handle, _pane.Handle);
			_langIndicator.LoadData(Project.GetProjectSingleData<ProjectResources>(), _pane);
		}
		void recreateContextMenu()
		{
			LimnorContextMenuCollection.ClearMenuCollection(_loader.ClassID);
			IMenuCommandService mcs = Surface2.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
			if (null != mcs)
			{
				return;
			}
			//this part of code is disabled
			if (null != mcs)
			{
				LimnorContextMenuCollection menuData = null;
				//IClass holder;
				object c = _contextMenus.Owner;
				if (_loader.RootObject == c)
				{
					//root pointer
					ClassPointer r = _loader.GetRootId();
					menuData = LimnorContextMenuCollection.ReloadMenuCollection(r);
				}
				else
				{
					ClassProperties cp = c as ClassProperties;
					if (cp != null)
					{
						//a pointer to an custom instance
						ClassInstancePointer cip = (ClassInstancePointer)(cp.Pointer);
						menuData = LimnorContextMenuCollection.ReloadMenuCollection(cip);
					}
					else
					{
						IClassRef cr = _loader.ObjectMap.GetClassRefByObject(c);
						if (cr != null)
						{
							menuData = LimnorContextMenuCollection.ReloadMenuCollection((ClassInstancePointer)cr);
						}
						else
						{
							UInt32 mid = _loader.ObjectMap.GetObjectID(c);
							if (mid != 0)
							{
								//a pointer to a lib-type instance
								MemberComponentId mcid = new MemberComponentId(_loader.GetRootId(), c, mid);
								menuData = LimnorContextMenuCollection.ReloadMenuCollection(mcid);//c.GetType());
							}
						}
					}
				}
				if (menuData != null)
				{
					foreach (MenuCommand cmd in _contextMenus)
					{
						mcs.RemoveCommand(cmd);
					}
					_contextMenus.Clear();
				}
			}
		}
		private void showPropertyGrid()
		{
			if (Surface2 != null)
			{
				IMenuCommandService mcs = Surface2.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
				if (mcs != null)
				{
				}
			}
		}
		/// <summary>
		/// 1. Search for "Limnor Studio" window by EnumWindows - h1
		/// 2. Search for "Properties" window by EnumChildWindows(h1) - h2
		/// 3. For each EnumChildWindows(h2), use c = Control.FromChildHandle(hWnd) 
		///     if c != null then check if c's type full name is "Microsoft.VisualStudio.PropertyBrowser.PropertyBrowser"
		/// 4. One of the child controls of the PropertyBrowser is a PropertyGrid
		/// </summary>
		/// <returns></returns>
		private PropertyGrid findVsPropertyGrid()
		{
			if (_propertyGrid == null)
			{
				_propertyGrid = (PropertyGrid)Loader.Host.GetService(typeof(PropertyGrid));
				if (_propertyGrid != null)
				{
					_propertyGrid.CommandsVisibleIfAvailable = false;
					_propertyGrid.KeyDown += _propertyGrid_KeyDown;
					_propertyGrid.KeyUp += _propertyGrid_KeyUp;
					_propertyGrid.PreviewKeyDown += _propertyGrid_PreviewKeyDown;
				}
			}
			return _propertyGrid;
		}

		void _propertyGrid_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{

			}
		}

		void _propertyGrid_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				e.Handled = true;
			}
		}

		void _propertyGrid_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				e.Handled = true;
			}
		}
		private PropertyGrid hookVsPropertyGrid()
		{
			if (!_vsPropertyGridHooked)
			{
				PropertyGrid pg = findVsPropertyGrid();
				if (pg != null)
				{
					if (_setupControl != null)
					{
						_setupControl.SetPropertyGrid(pg);
					}
					else
					{
						pg.PropertyValueChanged += new PropertyValueChangedEventHandler(pg_PropertyValueChanged);
						pg.SelectedObjectsChanged += new EventHandler(pg_SelectedObjectsChanged);
					}
					_vsPropertyGridHooked = true;
					return pg;
				}
				else
				{
					_propertyGridHookCount++;
					if (Math.IEEERemainder(_propertyGridHookCount, 10) == 0)
					{
						DesignUtil.WriteToOutputWindow("PropertyGrid not found ({0})", _propertyGridHookCount);
					}
					showPropertyGrid();
					if (_timerAction != EnumTimerAction.ReadConfig)
					{
						_timerAction = EnumTimerAction.FindPropertyGrid;
					}
					_tmReadConfig.Enabled = true;
				}
			}
			else
			{
				return _propertyGrid;
			}
			return null;
		}
		private bool findLimnorVS(IntPtr hWnd, IntPtr lParam)
		{
			int n = GetWindowTextLength(hWnd) + 1;
			StringBuilder sb = new StringBuilder(n);
			GetWindowText(hWnd, sb, n);
			if (sb.ToString().StartsWith("Limnor Studio", StringComparison.OrdinalIgnoreCase))
			{
				EnumChildWindows(hWnd, new EnumWindowsProc(findVsProperties), lParam);
				return false;
			}
			return true;
		}
		private bool findVsProperties(IntPtr hWnd, IntPtr lParam)
		{
			int n = GetWindowTextLength(hWnd) + 1;
			StringBuilder sb = new StringBuilder(n);
			GetWindowText(hWnd, sb, n);
			if (string.CompareOrdinal(sb.ToString(), "Properties") == 0)
			{
				EnumChildWindows(hWnd, new EnumWindowsProc(findVsPropertyBrowser), lParam);
				return false;
			}
			return true;
		}
		private bool findVsPropertyBrowser(IntPtr hWnd, IntPtr lParam)
		{
			Control c = Control.FromChildHandle(hWnd);
			if (c != null)
			{
				Type t = c.GetType();
				if (string.CompareOrdinal(t.FullName, "Microsoft.VisualStudio.PropertyBrowser.PropertyBrowser") == 0)
				{
					foreach (Control c0 in c.Controls)
					{
						_propertyGrid = c0 as PropertyGrid;
						if (_propertyGrid != null)
						{
							_propertyGrid.CommandsVisibleIfAvailable = false;
							return false;
						}
					}
				}
			}
			return true;
		}
		#endregion

		#region CallBack functions
		private void launch(bool debug)
		{
			System.Diagnostics.Process proc = null;
			if (debug)
			{
				string launcher = null;
				string launcherConfig = DesignUtil.GetLauncherConfigFile();
				string file = LimnorXmlDesignerLoader2.ActiveDesignerLoader.DocumentMoniker;
				LimnorProject p = LimnorSolution.GetProjectByComponentFile(file);
				if (p == null)
				{
					MessageBox.Show(string.Format("Project not found for component {0}", file), "Debug", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				}
				else
				{
					switch (p.ProjectType)
					{
						case EnumProjectType.WinForm:
							launcher = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launchW.exe");
							break;
						case EnumProjectType.Console:
							launcher = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launchC.exe");
							break;
					}
					if (!string.IsNullOrEmpty(launcher))
					{
						if (System.IO.File.Exists(launcher))
						{
							XmlDocument doc = new XmlDocument();
							doc.PreserveWhitespace = false;
							XmlNode node = doc.CreateElement("Config");
							doc.AppendChild(node);
							XmlUtil.SetAttribute(node, XmlTags.XMLATT_Debug, debug);
							XmlNode nodeStart = doc.CreateElement(XmlTags.XML_StartFile);
							node.AppendChild(nodeStart);
							nodeStart.InnerText = p.StartupComponentFile;
							doc.Save(launcherConfig);
							//
							proc = new System.Diagnostics.Process();
							proc.StartInfo.FileName = launcher;
							proc.StartInfo.Arguments = p.CommandArgumentsForDebug;
							proc.Start();
							proc.WaitForExit();
						}
						else
						{
							MessageBox.Show(string.Format("Debugger {0} not found for component {1}", launcher, file), "Debug", MessageBoxButtons.OK, MessageBoxIcon.Stop);
						}
					}
					else
					{
						MessageBox.Show(string.Format("Debugging not currently supported for {0}", p.ProjectType));
					}
				}
			}
			else
			{
			}
		}
		private void threadSaveAllLaunch(object debug)
		{
			fnSimpleFunction dgSaveAll = new fnSimpleFunction(SaveAll);
			_pane.Invoke(dgSaveAll);
		}
		private void launchApp(bool debug)
		{
			System.Threading.Thread th = new System.Threading.Thread(threadSaveAllLaunch);
			th.Start(debug);
			while (th.ThreadState == System.Threading.ThreadState.Running)
			{
				System.Threading.Thread.Sleep(0);
				Application.DoEvents();
			}
			launch(debug);
		}

		public void DebugAppCallBack(object sender, EventArgs e)
		{
			launchApp(true);
		}
		public void RunAppCallBack(object sender, EventArgs e)
		{
			launchApp(false);
		}

		private void ManageReferenceCallBack(object sender, EventArgs e)
		{
			LimnorProject prj = _loader.Project;
			if (prj != null)
			{
				DlgManageReferences dlg = new DlgManageReferences();
				StringCollection sc = new StringCollection();
				List<Type> tList = prj.GetToolboxItems(sc);
				if (sc.Count > 0)
				{
					MathNode.Log(sc);
				}
				if (tList.Count > 0)
				{
					foreach (Type t in tList)
					{
						dlg.AddType(t);
					}
				}
				if (dlg.ShowDialog(_pane.FindForm()) == DialogResult.OK)
				{
					prj.AddToolboxItems(dlg.RetTypes, false);
					prj.ToolboxLoaded = false;
					InitToolbox();
				}
			}
		}
		private void setCommandLineArgvsCallBack(object sender, EventArgs e)
		{
		}
		private void onSetCommandLineArgvs(object sender, EventArgs e)
		{
		}
		private void AddReferenceCallBack(object sender, EventArgs e)
		{
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
			try
			{
				LimnorProject prj = _loader.Project;
				if (prj != null)
				{
					IToolboxService toolboxService = this.Surface2.GetService(typeof(IToolboxService)) as IToolboxService;
					if (toolboxService != null)
					{
						string[] saTypes = TypeImporter.SelectTypes(1);
						string errMsg = DesignUtil.LoadToolBoxItemsToProject(prj, saTypes);
						if (string.IsNullOrEmpty(errMsg))
						{
							prj.ToolboxLoaded = false;
							InitToolbox();
						}
						else
						{
							MessageBox.Show(_pane.FindForm(), errMsg, "Load types", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
				}
			}
			finally
			{
				System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
			}
		}

		private void addToolboxItem(string tab, bool forProject, ILimnorToolbox toolboxService, Type tp)
		{
			if (tp == null)
				return;
			if (tp.IsAbstract)
				return;
			if (tp.Equals(typeof(InterfaceClass)))
				return;
			ConstructorInfo[] cifs = tp.GetConstructors();
			if (cifs == null || cifs.Length == 0)
			{
				return;
			}
			Image img = VPLUtil.GetActiveXIcon(tp);
			if (img == null)
			{
				System.Drawing.ToolboxBitmapAttribute tba = TypeDescriptor.GetAttributes(tp)[typeof(System.Drawing.ToolboxBitmapAttribute)] as System.Drawing.ToolboxBitmapAttribute;
				if (tba != null)
				{
					img = tba.GetImage(tp);
				}
			}
			if (tp.GetInterface("IComponent") == null)
			{
				string name = ToolboxItemXType.AddToolboxType(tp);
				addToolboxItem(tab, forProject, toolboxService, typeof(ToolboxItemXType), name, img as Bitmap, null);
			}
			else
			{
				addToolboxItem(tab, forProject, toolboxService, tp, tp.Name, img as Bitmap, null);
			}
		}
		private void addToolboxClassRefItem(string tab, bool forProject, ILimnorToolbox toolboxService, Type t, UInt32 classId, string name, Bitmap img)
		{
			Type tp = typeof(ClassInstancePointer);
			Dictionary<string, object> properties = new Dictionary<string, object>();
			properties.Add("ClassId", classId);
			properties.Add("Name", name);
			properties.Add("Type", t);
			addToolboxItem(tab, forProject, toolboxService, tp, name, img, properties);
		}
		private void addToolboxItem(string tab, bool forProject, ILimnorToolbox toolboxService, Type tp, string name, Bitmap img, Dictionary<string, object> properties)
		{
			if (img == null)
			{
				img = Resource1.Image1;
			}
			if (forProject)
			{
				toolboxService.AddToolboxItem(tab, Project.ProjectGuid, tp, name, img, properties);
			}
			else
			{
				toolboxService.AddToolboxItem(tab, tp, name, img, properties);
			}
		}
		private void ViewLogCallBack(object sender, EventArgs e)
		{
			MathNode.ViewLog();
		}
		private void DebugFormCallBack(object sender, EventArgs e)
		{
			SaveAll();
			string file = LimnorXmlDesignerLoader2.ActiveDesignerLoader.DocumentMoniker;
			///////////////////////////////////////////////
			// Construct and initialize settings for a second AppDomain.
			AppDomainSetup ads = new AppDomainSetup();
			ads.ApplicationBase =
				"file:///" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
			ads.DisallowBindingRedirects = false;
			ads.DisallowCodeDownload = true;
			ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
			//
			// Create the second AppDomain.
			AppDomain ad2 = AppDomain.CreateDomain("LimnorDebug", null, ads);
			ad2.DomainUnload += new EventHandler(ad2_DomainUnload);
			// Create an instance of MarshalbyRefType in the second AppDomain. 
			// A proxy to the object is returned.
			DebugRun debug = null; ;
			Type componentType = XmlUtil.GetLibTypeAttribute(LimnorXmlDesignerLoader2.ActiveDesignerLoader.Node);
			if (componentType.Equals(typeof(Form)) || componentType.IsSubclassOf(typeof(Form)))
			{
				debug = (DebugRunForm)ad2.CreateInstanceAndUnwrap(
						typeof(DebugRunForm).Assembly.FullName,
						typeof(DebugRunForm).FullName
					);
			}
			else if (componentType.Equals(typeof(LimnorWinApp)))
			{
				debug = (DebugRunWinApp)ad2.CreateInstanceAndUnwrap(
						typeof(DebugRunWinApp).Assembly.FullName,
						typeof(DebugRunWinApp).FullName
					);

			}
			else if (componentType.Equals(typeof(LimnorConsole)))
			{
				debug = (DebugConsole)ad2.CreateInstanceAndUnwrap(
						typeof(DebugConsole).Assembly.FullName,
						typeof(DebugConsole).FullName
					);

			}
			///////////////////////////////////////////////
			if (debug != null)
			{
				debug.Load(file);
				debug.Run();
			}
		}
		static void ad2_DomainUnload(object sender, EventArgs e)
		{
		}
		private void onQueryDebugForm(object sender, EventArgs e)
		{
		}
		public void onQueryDebugApp(object sender, EventArgs e)
		{
		}
		public void onQueryRunApp(object sender, EventArgs e)
		{
		}

		#region Build Command Handlers

		#region Solution
		/// <summary>
		/// Handler for when we want to query the status of the BuildSln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryBuildSln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our BuildSln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onBuildSln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the RebuildSln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryRebuildSln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our RebuildSln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onRebuildSln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the DeploySln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryDeploySln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our DeploySln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onDeploySln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the CleanSln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryCleanSln(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our CleanSln command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onCleanSln(object sender, EventArgs e)
		{
		}
		#endregion

		#region Selection
		/// <summary>
		/// Handler for when we want to query the status of the BuildSel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryBuildSel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our BuildSel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onBuildSel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the RebuildSel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryRebuildSel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our RebuildSel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onRebuildSel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the DeploySel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryDeploySel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our DeploySel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onDeploySel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the CleanSel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryCleanSel(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our CleanSel command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onCleanSel(object sender, EventArgs e)
		{
		}
		#endregion

		#region Cancel - Batch
		/// <summary>
		/// Handler for when we want to query the status of the CancelBuild command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryCancelBuild(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our BatchBuildDlg command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onCancelBuild(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for when we want to query the status of the BatchBuildDlg command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryBatchBuildDlg(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our BatchBuildDlg command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onBatchBuildDlg(object sender, EventArgs e)
		{
		}
		#endregion

		#endregion


		#region View Code / Open Command Handlers
		/// <summary>
		/// Handler for when we want to query the status of the ViewCode command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryViewCode(object sender, EventArgs e)
		{
		}
		private void onQueryUndo(object sender, EventArgs e)
		{
		}
		private void onQueryRedo(object sender, EventArgs e)
		{
		}
		private void onRedo(object sender, EventArgs e)
		{
		}
		private void onUndo(object sender, EventArgs e)
		{
		}
		private void onQueryAddReference(object sender, EventArgs e)
		{
		}
		/// <summary>
		/// Handler for our BatchBuildDlg command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onViewCode(object sender, EventArgs e)
		{
			return;
		}

		/// <summary>
		/// Handler for when we want to query the status of the OpenWith command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryOpenWith(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our BatchBuildDlg command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onOpenWith(object sender, EventArgs e)
		{
			return;
		}

		/// <summary>
		/// Handler for when we want to query the status of the Open command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onQueryOpen(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handler for our BatchBuildDlg command.
		/// </summary>
		/// <param name="sender">  This can be cast to an OleMenuCommand.</param>
		/// <param name="e">  Not used.</param>
		private void onOpen(object sender, EventArgs e)
		{
			return;
		}
		#endregion
		#endregion

		#region Properties
		public bool IsAtImageProperty
		{
			get
			{
				return _isImageProp;
			}
		}
		public DesignSurface Surface2
		{
			get
			{
				return _surface;
			}
		}
		public XmlNode RootXmlNode
		{
			get
			{
				return _loader.Node;
			}
		}
		private bool _isClosing;
		public bool IsClosing
		{
			get
			{
				return _isClosing;
			}
			set
			{
				_isClosing = value;
				LimnorXmlDesignerLoader2.RemoveLoader(_loader.ClassID);
			}
		}
		public MultiPanes PaneHolder
		{
			get
			{
				return _pane;
			}
		}
		private bool _a()
		{
			return false;
		}
		public Image ObjectIcon2
		{
			get
			{
				ClassPointer cp = _loader.GetRootId();
				Form f = cp.ObjectInstance as Form;
				if (f != null && f.Icon != null)
				{
					return f.Icon.ToBitmap().GetThumbnailImage(16, 16, new Image.GetThumbnailImageAbort(_a), IntPtr.Zero);
				}
				return ObjectIcon;
			}
		}
		public Image ObjectIcon
		{
			get
			{
				if (_objectIcon == null)
				{
					return _loader.GetRootId().ImageIcon;
				}
				return _objectIcon;
			}
			set
			{
				_objectIcon = value;
				if (_objectIcon != null)
				{
					if (_loader.CanBeChild)
					{
						//notify other LimnorXmlPanes
						Dictionary<UInt32, ILimnorDesignPane> xmlPanes = _loader.Project.GetTypedDataList<ILimnorDesignPane>();
						if (xmlPanes != null)
						{
							Dictionary<UInt32, ILimnorDesignPane>.Enumerator en = xmlPanes.GetEnumerator();
							while (en.MoveNext())
							{
								en.Current.Value.SetClassRefIcon(_loader.ClassID, _objectIcon);
							}
						}
					}
				}
			}
		}
		public string ObjectName
		{
			get
			{
				return _loader.ObjectName;
			}
		}
		public bool IsWebApp
		{
			get
			{
				return _loader.IsWebApp;
			}
		}
		public bool IsWebPage
		{
			get
			{
				return _loader.IsWebPage;
			}
		}
		#endregion

		#region Methods
		public void SelectComponent(IComponent c)
		{
			ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
			if (selectionService != null)
			{
				selectionService.SetSelectedComponents(new object[] { _loader.RootObject });
				Application.DoEvents();
				selectionService.SetSelectedComponents(new object[] { c });
			}
		}
		public IComponent CreateComponent(Type type, string name)
		{
			return Loader.CreateComponent(type, name);
		}
		public void OnPropertyListChanged(object owner)
		{
			ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
			if (selectionService != null)
			{
				selectionService.SetSelectedComponents(new object[] { _loader.RootObject });
				Application.DoEvents();
				selectionService.SetSelectedComponents(new object[] { owner });
			}
		}
		public void AddToolboxItem()
		{
			AddReferenceCallBack(this, EventArgs.Empty);
		}
		public void RefreshPropertyGrid()
		{
			PropertyGrid pg = hookVsPropertyGrid();
			if (pg != null)
			{
				pg.Refresh();
			}
		}
		public void SetAllowRefresh(bool allow)
		{
			FormViewer fv = _pane.GetDesigner<FormViewer>();
			if (fv != null)
			{
				fv.SetAllowRefresh(allow);
			}
		}
		public void OnBeforeSaveSetup()
		{
			if (_setupControl != null)
			{
				_setupControl.OnBeforeSave();
			}
		}
		public void OnClosing()
		{
			IWebPage wpage = _loader.RootObject as IWebPage;
			if (wpage != null)
			{
				wpage.OnClosing();
				if (wpage.HtmlChanged)
				{
					_loader.DataModified = true;
					_loader.NotifyChanges();
				}
			}
			_pane.OnClosing();
		}
		public void OnClosed()
		{
			IsClosing = true;
			if (_loader != null)
			{
				WebPage wpage = _loader.RootObject as WebPage;
				if (wpage != null)
				{
					wpage.RemoveDesignHtmlFiles();
				}
			}
			LimnorProject prj = Project;
			if (prj != null)
			{
				FrmObjectExplorer.RemoveDialogCaches(prj.ProjectGuid, _loader.ClassID);
				DesignObjects.RemoveDesignObjects(prj, _loader.ClassID);
				prj.DesignPaneClosed(this);
				if (prj.DesignPaneCount() == 0)
				{
					prj.ToolboxLoaded = false;
				}
			}
			LimnorContextMenuCollection.ClearMenuCollection(_loader.ClassID);
		}

		public void SetClassRefIcon(UInt32 classId, Image img)
		{
			adjustTrayControlIcon(DesignUtil.MakeDDWord((uint)0, classId), img);
			_pane.SetClassRefIcon(classId, img);
		}
		public void BeginApplyConfig()
		{
			if (_tmReadConfig != null)
			{
				_timerAction = EnumTimerAction.ReadConfig;
				_tmReadConfig.Enabled = true;
			}
		}

		public void ResetContextMenu()
		{
			if (_contextMenus != null && _contextMenus.Owner != null)
			{
				recreateContextMenu();
			}
		}

		public void OnComponentAdded(object obj)
		{
#if DEBUGX
			FormStringList.ShowDebugMessage("Start: call OnComponentAdded {0}", LimnorXmlDesignerLoader2.getComponentDisplay(obj as IComponent));
#endif
			object v = obj; //v is for notification, it can be changed
			ToolboxItemXType xt = v as ToolboxItemXType;
			if (xt != null)
			{
				if (string.IsNullOrEmpty(ToolboxItemXType.SelectedToolboxTypeKey))
				{
					MessageBox.Show(this.FindForm(), "Cannot add component. Component type not set for Toolbox item", "Component creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				_newObject = xt;
				_timerAction = EnumTimerAction.XTypeCreation;
				_tmReadConfig.Enabled = true;
				return;
			}
			else
			{
				if (ToolboxItemXType.SelectedToolboxClassId != 0)
				{
					ClassInstancePointer cip = obj as ClassInstancePointer;
					if (cip != null)
					{
						_newClassRef = cip;
						_timerAction = EnumTimerAction.ClassRefCreation;
						_tmReadConfig.Enabled = true;
						return;
					}
				}
			}
			//
			uint memberId = _loader.ObjectMap.GetObjectID(obj);
			if (memberId == 0)
			{
				throw new SerializerException("New component {0} not added to the map", obj.GetType().Name);
			}
			_loader.ObjectMap.TreeRoot.AddChild(obj);
			//create an XML node for this new component
			XmlNode objNode = _loader.Node.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}[@{1}='{2}']", XmlTags.XML_Object, XmlTags.XMLATT_ComponentID, memberId));
			if (objNode == null)
			{
				objNode = _loader.Node.OwnerDocument.CreateElement(XmlTags.XML_Object);
				_loader.Node.AppendChild(objNode);
				XmlUtil.SetAttribute(objNode, XmlTags.XMLATT_ComponentID, memberId);
				IComponent c = obj as IComponent;
				if (c != null && c.Site != null)
				{
					//find the IClassRef from the map.
					//if found then the new component is an instance of a dev class
					IClassRef cr = _loader.GetClassRefByComponent(c);
					if (cr != null)
					{
						//rename the compnent using the base type name
						c.Site.Name = _loader.CreateNewComponentName(ToolboxItemXType.SelectedToolboxClassName, null);
						cr.LoadProperties(_loader.Reader);
						cr.ImageIcon = null;
						adjustTrayControlIcon(cr.WholeId, null);
						v = cr; //switch to the class pointer 
					}
					else
					{
						if (VPLUtil.IsXType(c.GetType()) != EnumXType.None)
						{
							adjustTrayControlIcon(0, null);
						}
					}
					XmlUtil.SetAttribute(objNode, XmlTags.XMLATT_NAME, c.Site.Name);
				}
				XmlObjectWriter xw = _loader.Writer;
				xw.WriteObjectToNode(objNode, obj);
				if (xw.HasErrors)
				{
					MathNode.Log(xw.ErrorCollection);
				}
			}
			_pane.OnComponentAdded(v);
			_loader.NotifyChanges();
			// 
			//copy related DLL files to the project folder
			Type t = obj.GetType();
			if (!t.Assembly.GlobalAssemblyCache)
			{
				StringCollection afiles = new StringCollection();
				StringCollection errors = new StringCollection();
				string OutputFullpath;
				bool bOK = true;
				OutputFullpath = System.IO.Path.Combine(Project.ProjectFolder, "bin\\Debug");
				if (!System.IO.Directory.Exists(OutputFullpath))
				{
					try
					{
						System.IO.Directory.CreateDirectory(OutputFullpath);
					}
					catch (Exception e1)
					{
						errors.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Error creating folder:{0}. {1}", OutputFullpath, DesignerException.FormExceptionText(e1)));
						bOK = false;
					}
				}
				if (System.IO.Directory.Exists(OutputFullpath))
				{
					if (!DesignUtil.CopyAssembly(afiles, t, OutputFullpath, errors))
					{
						bOK = false;
					}
				}
				OutputFullpath = System.IO.Path.Combine(Project.ProjectFolder, "bin\\Release");
				if (!System.IO.Directory.Exists(OutputFullpath))
				{
					try
					{
						System.IO.Directory.CreateDirectory(OutputFullpath);
					}
					catch (Exception e1)
					{
						errors.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Error creating folder:{0}. {1}", OutputFullpath, DesignerException.FormExceptionText(e1)));
						bOK = false;
					}
				}
				if (System.IO.Directory.Exists(OutputFullpath))
				{
					if (!System.IO.Directory.Exists(OutputFullpath))
					{
						System.IO.Directory.CreateDirectory(OutputFullpath);
					}
				}
				if (!DesignUtil.CopyAssembly(afiles, t, OutputFullpath, errors))
				{
					bOK = false;
				}
				if (!bOK)
				{
					MathNode.Log(errors);
				}
			}
			ICustomEventMethodDescriptor em = obj as ICustomEventMethodDescriptor;
			if (em != null)
			{
				em.SetEventChangeMonitor(onCustomEventListChanged);
			}
			SoapHttpClientProtocol soap = obj as SoapHttpClientProtocol;
			if (soap != null)
			{
				adjustTrayControlIconByType(typeof(SoapHttpClientProtocol), Resource1.cloud);
			}
			CopyProtector cp = obj as CopyProtector;
			if (cp != null)
			{
				cp.SetApplicationGuid(Project.ProjectGuid);
				cp.SetApplicationName(Project.ProjectName);
			}
			//notify other classes in case this class is used as instances
			Dictionary<UInt32, ILimnorDesignPane> designPanes = _loader.Project.GetTypedDataList<ILimnorDesignPane>();
			foreach (ILimnorDesignPane pn in designPanes.Values)
			{
				pn.OnDefinitionChanged(_loader.ClassID, obj, EnumClassChangeType.ComponentAdded);
			}
		}
		public void OnComponentRename(object obj, string newName)
		{
			//
			if (obj is LimnorApp)
			{
				LimnorApp.SetStaticName(newName);
			}
			if (obj == _loader.RootObject)
			{
				XmlUtil.SetNameAttribute(_loader.Node, newName);
				if (!(obj is LimnorApp))
				{
					refreshProjectToolbox(null, newName);
				}
			}
			_pane.OnComponentRename(obj, newName);
			Dictionary<UInt32, ILimnorDesignPane> designPanes = _loader.Project.GetTypedDataList<ILimnorDesignPane>();
			foreach (ILimnorDesignPane pn in designPanes.Values)
			{
				pn.OnDefinitionChanged(_loader.ClassID, obj, EnumClassChangeType.ComponentRenamed);
			}
			HtmlElement_BodyBase heb = obj as HtmlElement_BodyBase;
			if (heb != null)
			{
				_loader.sendVOBnotice(enumVobNotice.HtmlElementRenamed, heb);
			}
		}


		public void OnComponentRemoved(object obj)
		{
			//
			_pane.OnComponentRemoved(obj);
			DrawingItem draw = obj as DrawingItem;
			if (draw != null)
			{
				DrawingPage dp = _loader.RootObject as DrawingPage;
				if (dp != null)
				{
					dp.RemoveDrawing(draw);
				}
			}
			Dictionary<UInt32, ILimnorDesignPane> designPanes = _loader.Project.GetTypedDataList<ILimnorDesignPane>();
			foreach (ILimnorDesignPane pn in designPanes.Values)
			{
				pn.OnDefinitionChanged(_loader.ClassID, obj, EnumClassChangeType.ComponentDeleted);
			}

		}
		public void OnClassLoadedIntoDesigner(UInt32 classId)
		{
			_pane.OnClassLoadedIntoDesigner(classId);
		}
		public void OnDefinitionChanged(UInt32 classId, object relatedObject, EnumClassChangeType changeMade)
		{
			ToolboxItemXType.SelectedToolboxClassId = 0;
			_pane.OnDefinitionChanged(classId, relatedObject, changeMade);
		}
		public void OnDataLoaded()
		{
			_pane.OnDataLoaded();
		}
		public void HideUIDesigner()
		{
			_pane.HideContainerByType(typeof(FormViewer));
		}
		public void WriteConfig(XmlNode node)
		{
			_pane.WriteConfig(node);
		}
		/// <summary>
		/// build toolbox tab for the project when a class is loaded into design mode
		/// </summary>
		/// <param name="prj">the project</param>
		/// <param name="newName">the class name changed. null: name not changed</param>
		public void InitToolbox()
		{
			LimnorProject prj = _loader.Project;
			if (prj != null && (!_toolsLoaded || !prj.ToolboxLoaded))
			{
				ILimnorToolbox toolboxService = this.Surface2.GetService(typeof(ILimnorToolbox)) as ILimnorToolbox;
				if (toolboxService != null)
				{
					if (!_toolsLoaded)
					{
						const string UTILITYTAB = "Miscellaneous objects";
						const string DRAWINGTAB = "Drawing objects";
						const string DATABASETAB = "Database objects";

						List<Type> tList;
						//
						int n = 0;
						//
						_toolsLoaded = true;
						//===drawing tools===========================================
						toolboxService.RemoveTab(DRAWINGTAB);
						toolboxService.AddTab(DRAWINGTAB, true, false, n++, true);
						tList = DrawingPage.GetAllDrawingItemTypes();
						SortedList<string, Type> ls = new SortedList<string, Type>();
						foreach (Type t in tList)
						{
							ls.Add(t.Name, t);
						}
						tList = DrawTable.GetAllDrawingItemTypes();
						foreach (Type t in tList)
						{
							ls.Add(t.Name, t);
						}
						IEnumerator<KeyValuePair<string, Type>> ie = ls.GetEnumerator();
						while (ie.MoveNext())
						{
							FormLoadProgress.Step();
							addToolboxItem(DRAWINGTAB, false, toolboxService, ie.Current.Value);
						}
						toolboxService.AdjustTabSize(DRAWINGTAB);
						//===database tools==========================================
						FormLoadProgress.Step();
						toolboxService.RemoveTab(DATABASETAB);
						toolboxService.AddTab(DATABASETAB, true, false, n++, true);
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyDataSet));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyGrid));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyGridDetail));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyQuery));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyTransactor));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyTransfer));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(EasyUpdator));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(DatabaseExecuter));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(DataSet));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(DataTable));
						addToolboxItem(DATABASETAB, false, toolboxService, typeof(DataGridViewEx));
						toolboxService.AdjustTabSize(DATABASETAB);
						//===utilities===============================================
						FormLoadProgress.Step();
						toolboxService.RemoveTab(UTILITYTAB);
						toolboxService.AddTab(UTILITYTAB, true, false, n++, true);
						//
						string sDir = Path.GetDirectoryName(this.GetType().Assembly.Location);
						string toolboxCfg = Path.Combine(sDir, "LimnorToolbox.xml");
						if (File.Exists(toolboxCfg))
						{
							IList<Type> lst = XmlUtil.GetTypes(toolboxCfg, true);
							foreach (Type ti in lst)
							{
								addToolboxItem(UTILITYTAB, false, toolboxService, ti);
							}
						}
						toolboxService.AdjustTabSize(UTILITYTAB);
						//===web client controls===================================================

						const string WEBTAB = "Web client controls";
						FormLoadProgress.Step();
						toolboxService.RemoveTab(WEBTAB);
						toolboxService.AddTab(WEBTAB, true, false, n++, true);
						//
						sDir = Path.GetDirectoryName(this.GetType().Assembly.Location);
						toolboxCfg = Path.Combine(sDir, "LimnorWebToolbox.xml");
						if (File.Exists(toolboxCfg))
						{
							IList<Type> lst = XmlUtil.GetTypes(toolboxCfg, true);
							foreach (Type ti in lst)
							{
								addToolboxItem(WEBTAB, false, toolboxService, ti);
							}
						}
						toolboxService.AdjustTabSize(WEBTAB);
						//==web server components ==========================================
						toolboxCfg = Path.Combine(sDir, "LimnorWebServerToolbox.xml");
						if (File.Exists(toolboxCfg))
						{
							Dictionary<string, IList<Type>> cats = XmlUtil.GetTypesInCategories(toolboxCfg, true);
							foreach (KeyValuePair<string, IList<Type>> kv in cats)
							{
								FormLoadProgress.Step();
								toolboxService.RemoveTab(kv.Key);
								toolboxService.AddTab(kv.Key, true, false, n++, true);
								foreach (Type ti in kv.Value)
								{
									addToolboxItem(kv.Key, false, toolboxService, ti);
								}
								toolboxService.AdjustTabSize(kv.Key);
							}
						}
						//
						//==================================================================
						//
						FormLoadProgress.Step();
						//
					}
					//===project=================================================
					if (!prj.ToolboxLoaded)
					{
						prj.ToolboxLoaded = true;
						refreshProjectToolbox(toolboxService, string.Empty);
					}
					//
					toolboxService.DataUsed();
					toolboxService.SetChanged(false);
					toolboxService.AdjustTabSizes();
					toolboxService.RefreshTabs();
					toolboxService.HideToolbox();
				}
			}
		}
		public void HideToolbox()
		{
			if (_toolboxService == null)
			{
				_toolboxService = this.Surface2.GetService(typeof(ILimnorToolbox)) as ILimnorToolbox;
			}
			if (_toolboxService != null)
			{
				_toolboxService.ReqestHideToolbox();
			}
		}
		public void LoadProjectToolbox()
		{
			LimnorProject prj = Project;
			if (prj != null)
			{
				if (!prj.ToolboxLoaded)
				{
					prj.ToolboxLoaded = true;
					refreshProjectToolbox(null, null);
				}
				ILimnorToolbox tb = this.Surface2.GetService(typeof(ILimnorToolbox)) as ILimnorToolbox;
				if (tb != null)
				{
					tb.ShowProjectTab(prj.ProjectGuid);
				}
			}
		}
		private void refreshProjectToolbox(ILimnorToolbox toolboxService, string newName)
		{
			bool bTopCall = (toolboxService == null);
			LimnorProject prj = Project;
			if (prj != null)
			{
				if (toolboxService == null)
				{
					toolboxService = this.Surface2.GetService(typeof(ILimnorToolbox)) as ILimnorToolbox;
				}
				if (toolboxService != null)
				{
					//clear the project toolbox tab
					toolboxService.ResetProjectTab(prj.ProjectGuid);
					//add lib types for the project
					StringCollection sc = new StringCollection();
					List<Type> tList = prj.GetToolboxItems(sc);
					if (sc.Count > 0)
					{
						MathNode.Log(sc);
					}
					foreach (Type t in tList)
					{
						FormLoadProgress.Step();
						addToolboxItem(prj.ProjectName, true, toolboxService, t);
					}
					//add classes in the project
					UInt32 myId = XmlUtil.GetAttributeUInt(this.Loader.Node, XmlTags.XMLATT_ClassID);
					UInt32 myMemberId = XmlUtil.GetAttributeUInt(this.Loader.Node, XmlTags.XMLATT_ComponentID);
					IList<ClassData> classes = prj.GetAllClasses();
					for (int i = 0; i < classes.Count; i++)
					{
						Type tp = SerializeUtil.GetComponentType(classes[i].ComponentXmlNode);
						if (tp != null)
						{
							Type t0 = VPLUtil.GetObjectType(tp);

							if (!t0.Equals(typeof(InterfaceClass)) && !tp.IsSubclassOf(typeof(LimnorApp)) && !tp.IsAbstract)
							{
								if (tp.Equals(typeof(Form)) || tp.IsSubclassOf(typeof(Form)))
								{
									tp = VPLUtil.GetXClassType(tp);
								}
								UInt32 classId = classes[i].ComponentId;
								UInt32 memberId = XmlUtil.GetAttributeUInt(classes[i].ComponentXmlNode, XmlTags.XMLATT_ComponentID);
								string name;
								Image bmp = null;
								LimnorXmlPane2 pane0 = (LimnorXmlPane2)prj.GetTypedData<ILimnorDesignPane>(classId);
								if (pane0 != null)
								{
									name = pane0.ObjectName;
									bmp = pane0.ObjectIcon2 as Bitmap;
								}
								else
								{
									name = classes[i].ComponentName;
									string filename = SerializeUtil.ReadIconFilename(classes[i].ComponentXmlNode);
									if (!string.IsNullOrEmpty(filename))
									{
										filename = System.IO.Path.Combine(prj.ProjectFolder, filename);
										if (System.IO.File.Exists(filename))
										{
											bmp = (Bitmap)System.Drawing.Bitmap.FromFile(filename);
										}
									}
									else
									{
										bmp = VPLUtil.GetTypeIcon(tp);
									}
								}
								if (!string.IsNullOrEmpty(newName))
								{
									if (classId == myId && memberId == myMemberId)
									{
										name = newName;
									}
								}
								FormLoadProgress.Step();
								addToolboxClassRefItem(prj.ProjectName, true, toolboxService, tp, classId, name, bmp as Bitmap);
							}
						}
					}
					//add web services============
					RefreshWebServiceToolbox();
					//============================
					toolboxService.AdjustTabSize(prj.ProjectGuid);
					if (bTopCall)
					{
						toolboxService.DataUsed();
						toolboxService.SetChanged(false);
						toolboxService.AdjustTabSizes();
					}
				}
			}
		}

		public void RefreshWebServiceToolbox()
		{
			const string WEBSVCTAB = "Web and Remote Services";
			LimnorProject prj = Project;
			ILimnorToolbox toolboxService = this.Surface2.GetService(typeof(ILimnorToolbox)) as ILimnorToolbox;
			if (toolboxService != null && prj != null)
			{
				toolboxService.RemoveTab(WEBSVCTAB);

				List<WebServiceProxy> proxyList = prj.GetTypedProjectData<List<WebServiceProxy>>();
				if (proxyList == null)
				{
					VSPrj.PropertyBag wsc = prj.GetWebServiceFiles();
					proxyList = new List<WebServiceProxy>();
					foreach (Dictionary<string, string> file in wsc)
					{
						Assembly a = Assembly.LoadFile(System.IO.Path.Combine(prj.ProjectFolder, file[XmlTags.XMLATT_filename]));
						WebServiceProxy proxy = new WebServiceProxy(file[XmlTags.XMLATT_filename], a, file[XmlTags.XMLATT_asmxUrl]);
						proxyList.Add(proxy);
					}
					prj.SetTypedProjectData<List<WebServiceProxy>>(proxyList);
				}
				List<WcfServiceProxy> proxyWcfList = DesignUtil.GetWcfProxyList(prj);
				if ((proxyList != null && proxyList.Count > 0) || (proxyWcfList != null && proxyWcfList.Count > 0))
				{
					toolboxService.AddTab(WEBSVCTAB, false, false, 1, true);
				}
				if (proxyList != null && proxyList.Count > 0)
				{
					foreach (WebServiceProxy proxy in proxyList)
					{
						List<Type> list = proxy.ProxyTypes;
						foreach (Type t in list)
						{
							addToolboxItem(WEBSVCTAB, false, toolboxService, t, t.Name, Resource1.cloud, null);
						}
					}
				}
				//
				if (proxyWcfList != null && proxyWcfList.Count > 0)
				{
					foreach (WcfServiceProxy proxy in proxyWcfList)
					{
						List<Type> list = proxy.ProxyTypes;
						foreach (Type t in list)
						{
							if (_loadedTypes == null)
							{
								_loadedTypes = new Dictionary<string, Type>();
							}
							if (!_loadedTypes.ContainsKey(t.Name))
							{
								_loadedTypes.Add(t.Name, t);
							}
							Dictionary<string, object> props = new Dictionary<string, object>();
							props.Add(WrappedType, t);
							addToolboxItem(WEBSVCTAB, false, toolboxService, t, t.Name, Resource1._service.ToBitmap(), props);
						}
					}
				}
				toolboxService.AdjustTabSize(WEBSVCTAB);
			}
		}
		public IWin32Window Window
		{
			get
			{
				if (_setupControl != null)
				{
					return (IWin32Window)_setupControl;
				}
				return (IWin32Window)_pane;
			}
		}
		public void SetComponentSelection(IComponent c)
		{
			ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
			if (selectionService != null)
			{
				selectionService.SetSelectedComponents(new IComponent[] { c });
			}
		}
		public void SetObjectToPropertyGrid(object val)
		{
			try
			{
				if (_propertyGrid != null)
				{
					_propertyGrid.SelectedObject = val;
					if (val != null)
					{
						INonHostedObject nho = val as INonHostedObject;
						if (nho != null)
						{
							nho.SetChangeEvents(new EventHandler(nho_NameChanging), new EventHandler(nho_PropertyChanged));
						}
						ICustomEventMethodType cem = val as ICustomEventMethodType;
						if (cem != null)
						{
							cem.HookCustomPropertyValueChange(new EventHandler(nho_PropertyChanged));
						}
						VPLUtil.AdjustImagePropertyAttribute(val);
					}
				}
			}
			catch
			{
			}
		}
		public void SaveAll()
		{
			_loader.Flush();
		}
		public void Close()
		{
		}
		public void AddWebService()
		{
			string wsdlUtil = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "wsdl.exe");
			if (!System.IO.File.Exists(wsdlUtil))
			{
				MessageBox.Show(_pane.FindForm(), string.Format(System.Globalization.CultureInfo.InvariantCulture, "WSDL utility not found: [{0}].", wsdlUtil), "Add Web Service", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				DlgAddWebService dlg = new DlgAddWebService();
				dlg.SetData(this);
				if (dlg.ShowDialog(_pane.FindForm()) == DialogResult.OK)
				{
				}
			}
		}
		public void ManageWebService()
		{
			DlgManageWebService dlg = new DlgManageWebService();
			dlg.LoadData(Project);
			if (dlg.ShowDialog(_pane.FindForm()) == DialogResult.OK)
			{
				//RefreshWebServiceToolbox(null);
				refreshProjectToolbox(null, null);
			}
		}

		#endregion

		#region dynamical menus
		public void OnMRUExec(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi == null)
				return;
			MenuItemData menuCmd = mi.Tag as MenuItemData;
			if (null == menuCmd)
				return;

			ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
			if (selectionService == null)
				return;

			Object c = selectionService.PrimarySelection;
			if (c == null)
				return;

			if (menuCmd.Owner.ObjectInstance != c)
				return;

			try
			{
				XmlNode node = _loader.Node;
				if (menuCmd.ExecuteMenuCommand(_loader.Project, menuCmd.Owner, node, _pane, null, null))
				{
					_loader.NotifyChanges();
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}

		//////////////////////////////////////////////////////////////////
		// Handle updating the text and status of a dynamic item.
		public void OnMRUQueryStatus(object sender, EventArgs e)
		{
			bool bVisible = false;
			MenuItem mi = sender as MenuItem;
			if (mi == null)
				return;
			MenuItemData menuCmd = mi.Tag as MenuItemData;
			if (null == menuCmd)
				return;
			ISelectionService selectionService = (ISelectionService)Surface2.GetService(typeof(ISelectionService));
			if (selectionService != null)
			{
				if (selectionService.PrimarySelection != null)
				{
					if (menuCmd.Owner.ObjectInstance == selectionService.PrimarySelection)
					{
						bVisible = true;
					}
				}
			}
			if (!bVisible)
			{
				bVisible = false;
			}
			mi.Visible = bVisible;
		}

		#endregion

		#region Command handlers
		internal void SetupCommandHandlers()
		{
			// Add our command handlers for menu (commands must exist in the .vsct file)
			IMenuCommandService mcs = Surface2.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
			if (null != mcs)
			{

			}
		}

		/// <summary>
		/// Helper function used to add commands using IMenuCommandService
		/// </summary>
		/// <param name="mcs"> The IMenuCommandService interface.</param>
		/// <param name="menuGroup"> This guid represents the menu group of the command.</param>
		/// <param name="cmdID"> The command ID of the command.</param>
		/// <param name="commandEvent"> An EventHandler which will be called whenever the command is invoked.</param>
		/// <param name="queryEvent"> An EventHandler which will be called whenever we want to query the status of
		/// the command.  If null is passed in here then no EventHandler will be added.</param>
		private static void addCommand(IMenuCommandService mcs, Guid menuGroup, int cmdID,
									   EventHandler commandEvent, EventHandler queryEvent)
		{
		}

		#endregion

		#region ILimnorDesignPane Members
		public bool CanBuild
		{
			get
			{
				WebPage wpage = _loader.RootObject as WebPage;
				if (wpage != null)
				{
					return wpage.CanBuild;
				}
				return true;
			}
		}
		public IList<IXDesignerViewer> GetDettachedViewers()
		{
			return _detachedViewers;
		}
		public ILimnorDesignerLoader Loader
		{
			get
			{
				return _loader;
			}
		}
		public object CurrentObject
		{
			get
			{
				return _currentSelectedObject;
			}
		}
		public ClassPointer RootClass
		{
			get
			{
				return _loader.GetRootId();
			}
		}
		public void OnAddExternalType(Type type)
		{
			_pane.OnAddExternalType(this.RootClass.ClassId, type);
		}
		public void OnCultureChanged()
		{
			ProjectResources rm = Project.GetProjectSingleData<ProjectResources>();
			if (rm.Languages.Count == 0)
			{
				if (_langIndicator != null)
				{
					_langIndicator.Hide();
				}
			}
			else
			{
				if (_langIndicator == null)
				{
					loadLanguageIndicator();
				}
				else
				{
					_langIndicator.OnLanguageChanged();
				}
			}
			ClassPointer root = this.RootClass;
			root.OnCultureChanged();//use PropertyInfo to change the property so that it will not cause change-detection
		}
		public void OnResourcesEdited()
		{
			if (_langIndicator != null)
			{
				_langIndicator.CreateContextmenu();
			}
		}

		public void OnIconChanged()
		{
			this.Project.ToolboxLoaded = false;
			refreshProjectToolbox(null, null);
		}
		List<IXDesignerViewer> _detachedViewers;
		public void AddDesigner(IXDesignerViewer designer, bool detached)
		{
			try
			{
				if (detached)
				{
					if (_detachedViewers == null)
					{
						_detachedViewers = new List<IXDesignerViewer>();
					}
					if (!_detachedViewers.Contains(designer))
					{
						_detachedViewers.Add(designer);
					}
				}
				else
				{
					_pane.AddViewer(designer);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void RemoveDesigner(IXDesignerViewer designer, bool detached)
		{
			try
			{
				if (detached)
				{
					if (_detachedViewers != null)
					{
						if (_detachedViewers.Contains(designer))
						{
							_detachedViewers.Remove(designer);
						}
					}
				}
				else
				{
					_pane.RemoveViewer(designer);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnNotifyChanges()
		{
			try
			{
				_loader.NotifyChanges();
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnActionSelected(IAction action)
		{
			try
			{
				if (action.ClassId == _loader.ClassID)
				{
					_pane.OnActionSelected(action);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		/// <summary>
		/// notify viewers
		/// </summary>
		/// <param name="ea"></param>
		public void OnAssignAction(EventAction ea)
		{
			try
			{
				UInt32 classId = 0;
				if (ea != null && ea.Event != null)
				{
					if (ea.Event.RootPointer != null)
					{
						classId = ea.Event.RootPointer.ClassId;
					}
					else
					{
						if (ea.Event.Owner != null)
						{
							IClass ic = ea.Event.Owner as IClass;
							if (ic != null)
							{
								classId = ic.ClassId;
							}
						}
					}
				}
				if (classId == _loader.ClassID)
				{
					_pane.OnAssignAction(ea);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnRemoveEventHandler(EventAction ea, TaskID task)
		{
			try
			{
				if (ea.Event.RootPointer.ClassId == _loader.ClassID)
				{
					_pane.OnRemoveEventHandler(ea, task);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnActionListOrderChanged(object sender, EventAction ea)
		{
			try
			{
				if (ea.Event.RootPointer.ClassId == _loader.ClassID)
				{
					_pane.OnActionListOrderChanged(sender, ea);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnActionChanged(UInt32 classId, IAction act, bool isNewAction)
		{
			try
			{
				if (classId == _loader.ClassID)
				{
					_pane.OnActionCreated(classId, act, isNewAction);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnMethodChanged(MethodClass method, bool isNewMethod)
		{
			try
			{
				if (method.ClassId == _loader.ClassID)
				{
					ClassPointer root = _loader.GetRootId();
					if (!method.IsOverride && root.ClassId != method.ClassId)
					{
						ClassPointer baseClass = root.GetBaseClass(method.ClassId);
						if (baseClass != null)
						{
							root.OnBaseMethodChanged(method);
						}
					}

					resetContextMenu();
				}
				else
				{
					if (_loader.ObjectMap.ClassRefList != null)
					{
						foreach (IClassRef v in _loader.ObjectMap.ClassRefList)
						{
							ClassInstancePointer cip = v as ClassInstancePointer;
							if (cip != null)
							{
								if (method.ClassId == v.ClassId)
								{
									if (cip.ObjectInstance == _contextMenus.Owner)
									{
										recreateContextMenu();
									}
									else
									{
										LimnorContextMenuCollection.RemoveMenuCollection(cip);
									}
									break;
								}
							}
						}
					}
				}
				_pane.OnMethodChanged(method, isNewMethod);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnRemoveInterface(InterfacePointer interfacePointer)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnRemoveInterface(interfacePointer);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceAdded(InterfacePointer interfacePointer)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceAdded(interfacePointer);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnBaseInterfaceAdded(InterfaceClass owner, InterfacePointer baseInterface)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnBaseInterfaceAdded(owner, baseInterface);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceEventChanged(InterfaceElementEvent eventType)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceEventChanged(eventType);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceEventDeleted(InterfaceElementEvent eventType)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceEventDeleted(eventType);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceEventAdded(InterfaceElementEvent eventType)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceEventAdded(eventType);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfacePropertyChanged(InterfaceElementProperty property)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfacePropertyChanged(property);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfacePropertyDeleted(InterfaceElementProperty property)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfacePropertyDeleted(property);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfacePropertyAdded(InterfaceElementProperty property)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfacePropertyAdded(property);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceMethodDeleted(InterfaceElementMethod method)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceMethodDeleted(method);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceMethodChanged(InterfaceElementMethod method)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceMethodChanged(method);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnInterfaceMethodCreated(InterfaceElementMethod method)
		{
			try
			{
				ClassPointer root = _loader.GetRootId();
				LimnorContextMenuCollection.RemoveMenuCollection(root);
				_pane.OnInterfaceMethodCreated(method);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnResetDesigner(object obj)
		{
			_pane.OnResetDesigner(obj);
		}
		public void OnDeleteMethod(MethodClass method)
		{
			try
			{
				if (method.ClassId == _loader.ClassID)
				{
					EventHandlerMethod ehm = method as EventHandlerMethod;
					if (ehm != null)
					{
						OnDeleteEventMethod(ehm);
					}
					else
					{
						ClassPointer root = _loader.GetRootId();
						root.ResetCustomPropertyCollection();
						if (!method.IsOverride && root.ClassId != method.ClassId)
						{
							ClassPointer baseClass = root.GetBaseClass(method.ClassId);
							if (baseClass != null)
							{
								root.OnDeleteBaseMethod(method);
							}
						}
					}
				}
				else
				{
					if (_loader.ObjectMap.ClassRefList != null)
					{
						foreach (IClassRef v in _loader.ObjectMap.ClassRefList)
						{
							ClassInstancePointer cip = v as ClassInstancePointer;
							if (cip != null)
							{
								if (method.ClassId == v.ClassId)
								{
									if (cip.ObjectInstance == _contextMenus.Owner)
									{
										recreateContextMenu();
									}
									else
									{
										LimnorContextMenuCollection.RemoveMenuCollection(cip);
									}
									break;
								}
							}
						}
					}
				}
				_pane.OnDeleteMethod(method);
				resetContextMenu();
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnDeleteEventMethod(EventHandlerMethod method)
		{
			try
			{
				if (method.ClassId == _loader.ClassID)
				{
					_pane.OnDeleteEventMethod(method);
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnHtmlElementSelected(HtmlElement_Base element)
		{
			try
			{
				_pane.OnHtmlElementSelected(element);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnSelectedHtmlElement(Guid g, object selector)
		{
			try
			{
				_pane.OnSelectedHtmlElement(g, selector);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		//an html element is used. guid and id should have been created
		public void OnUseHtmlElement(HtmlElement_BodyBase element)
		{
			try
			{
				_pane.OnUseHtmlElement(element);
				_loader.sendVOBnotice(enumVobNotice.HtmlElementUsed, element);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnDeleteProperty(PropertyClass property)
		{
			try
			{
				if (property.ClassId == _loader.ClassID)
				{
					ClassPointer root = _loader.GetRootId();
					root.ResetCustomPropertyCollection();
					if (!property.IsOverride && root.ClassId != property.ClassId)
					{
						ClassPointer baseClass = root.GetBaseClass(property.ClassId);
						if (baseClass != null)
						{
							root.OnDeleteBaseProperty(property);
						}
					}

					resetContextMenu();
				}
				else
				{
					//check instances
					if (_loader.ObjectMap.ClassRefList != null)
					{
						foreach (IClassRef v in _loader.ObjectMap.ClassRefList)
						{
							ClassInstancePointer cip = v as ClassInstancePointer;
							if (cip != null)
							{
								if (property.ClassId == v.ClassId)
								{
									if (cip.ObjectInstance == _contextMenus.Owner)
									{
										recreateContextMenu();
									}
									else
									{
										LimnorContextMenuCollection.RemoveMenuCollection(cip);
									}
									break;
								}
							}
						}
					}
				}
				_pane.OnDeleteProperty(property);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnAddProperty(PropertyClass property)
		{
			try
			{
				if (property.ClassId == _loader.ClassID)
				{
					ClassPointer root = _loader.GetRootId();

					root.ResetCustomPropertyCollection();
					if (!property.IsOverride && root.ClassId != property.ClassId)
					{
						ClassPointer baseClass = root.GetBaseClass(property.ClassId);
						if (baseClass != null)
						{
							PropertyClassInherited pi = property.CreateInherited(root);
							root.OnAddBaseProperty(pi);
						}
					}
					resetContextMenu();
				}
				else
				{
					//check instances
					if (_loader.ObjectMap.ClassRefList != null)
					{
						foreach (IClassRef v in _loader.ObjectMap.ClassRefList)
						{
							ClassInstancePointer cip = v as ClassInstancePointer;
							if (cip != null)
							{
								if (property.ClassId == v.ClassId)
								{
									if (cip.ObjectInstance == _contextMenus.Owner)
									{
										recreateContextMenu();
									}
									else
									{
										LimnorContextMenuCollection.RemoveMenuCollection(cip);
									}
								}
							}
						}
					}
				}
				_pane.OnAddProperty(property);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		/// <summary>
		/// notification of custom property definition changes
		/// </summary>
		/// <param name="property"></param>
		/// <param name="name"></param>
		public void OnPropertyChanged(INonHostedObject property, string name)
		{
			try
			{
				if (property.ClassId == _loader.ClassID)
				{
					if (string.CompareOrdinal(name, "Name") == 0)
					{
						PropertyClass prop = property as PropertyClass;
						if (prop != null)
						{
							ClassPointer root = _loader.GetRootId();
							root.OnCustomPropertyNameChanged(prop);
						}
					}
					else
					{
						PropertyClass prop = property as PropertyClass;
						if (prop != null)
						{
							ClassPointer root = _loader.GetRootId();
							root.OnPropertyChanged(property, name);
						}
					}
				}
				_pane.OnPropertyChanged(property, name);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnAddEvent(EventClass eventObject)
		{
			try
			{
				if (eventObject.ClassId == _loader.ClassID)
				{
					resetContextMenu();
				}
				else
				{
					//check instances
					if (_loader.ObjectMap.ClassRefList != null)
					{
						foreach (IClassRef v in _loader.ObjectMap.ClassRefList)
						{
							ClassInstancePointer cip = v as ClassInstancePointer;
							if (cip != null)
							{
								if (eventObject.ClassId == v.ClassId)
								{
									if (cip.ObjectInstance == _contextMenus.Owner)
									{
										recreateContextMenu();
									}
									else
									{
										LimnorContextMenuCollection.RemoveMenuCollection(cip);
									}
									break;
								}
							}
						}
					}
				}
				_pane.OnAddEvent(eventObject);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnActionDeleted(IAction action)
		{
			_pane.OnActionDeleted(action);
		}
		public void OnDeleteEvent(EventClass eventObject)
		{
			try
			{
				if (eventObject.ClassId == _loader.ClassID)
				{
					resetContextMenu();
				}
				else
				{
					if (_loader.ObjectMap.ClassRefList != null)
					{
						foreach (IClassRef v in _loader.ObjectMap.ClassRefList)
						{
							ClassInstancePointer cip = v as ClassInstancePointer;
							if (cip != null)
							{
								if (eventObject.ClassId == v.ClassId)
								{
									if (cip.ObjectInstance == _contextMenus.Owner)
									{
										recreateContextMenu();
									}
									else
									{
										LimnorContextMenuCollection.RemoveMenuCollection(cip);
									}
									break;
								}
							}
						}
					}
				}
				_pane.OnDeleteEvent(eventObject);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnResetMap()
		{
			try
			{
				_pane.OnResetMap();
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public void OnDatabaseListChanged()
		{
			_pane.OnDatabaseListChanged();
		}
		public void OnDatabaseConnectionNameChanged(Guid connectionid, string newName)
		{
			_pane.OnDatabaseConnectionNameChanged(connectionid, newName);
		}
		public void OnEventListChanged(ICustomEventMethodDescriptor owner, UInt32 objectId)
		{
			try
			{
				if (_contextMenus != null)
				{
					if (_contextMenus.Owner == owner)
					{
						recreateContextMenu();
					}
				}
				ClassPointer root = _loader.GetRootId();
				List<EventAction> eaList = root.EventHandlers;
				List<EventAction> deleted = new List<EventAction>();
				List<EventPointer> idChanged = new List<EventPointer>();
				Dictionary<EventPointer, string> changed = new Dictionary<EventPointer, string>();
				bool ehChanged = false;
				foreach (EventAction ea in eaList)
				{
					MemberComponentId mcid = ea.Event.Owner as MemberComponentId;
					if (mcid != null)
					{
						if (mcid.MemberId == objectId)
						{
							EventPointer ep = ea.Event as EventPointer;
							if (ep != null)
							{
								if (ep.EventId != 0)
								{
									string eventName = owner.GetEventNameById(ep.EventId);
									if (string.IsNullOrEmpty(eventName))
									{
										int id = owner.GetEventId(ep.MemberName);
										if (id == 0)
										{
											//deleted
											deleted.Add(ea);
											ehChanged = true;
										}
										else
										{
											//id changed
											idChanged.Add(ep);
										}
									}
									else
									{
										if (!string.IsNullOrEmpty(eventName))
										{
											if (string.Compare(ep.MemberName, eventName, StringComparison.Ordinal) != 0)
											{
												//modified
												changed.Add(ep, eventName);
												ehChanged = true;

											}
										}
										else
										{
											//deleted
											deleted.Add(ea);
											ehChanged = true;
										}
									}
								}
							}
						}
					}
				}
				foreach (EventPointer ep in idChanged)
				{
					if (ep.Info == null) //force load
					{
						throw new DesignerException("Error loading event info for {0}", ep.MemberName);
					}
				}
				foreach (KeyValuePair<EventPointer, string> kv in changed)
				{
					string old = kv.Key.MemberName;
					kv.Key.MemberName = kv.Value;
					if (kv.Key.Info == null) //force reload
					{
						throw new DesignerException("Error loading event info when changing from {0} to {1}", old, kv.Value);
					}
				}
				foreach (EventAction ea in deleted)
				{
					eaList.Remove(ea);
				}
				if (ehChanged)
				{
					root.SaveEventHandlers();
				}
				_pane.OnEventListChanged(owner, objectId);
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		#endregion

		#region ILimnorDesigner Members
		public LimnorProject Project
		{
			get
			{
				return _loader.Project;
			}
		}
		public IObjectPointer SelectDataType(Form caller)
		{
			return _loader.SelectDataType(caller);
		}

		public bool IsMethodNameUsed(string name)
		{
			return _loader.IsNameUsed(name, 0);
		}

		public string CreateMethodName(string baseName)
		{
			return _loader.CreateMethodName(baseName, null);
		}

		#endregion

	}
}
