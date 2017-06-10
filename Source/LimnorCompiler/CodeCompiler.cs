/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Compiler
 * License: GNU General Public License v3.0
 
 */
/*
	Limnor Studio Class Compiler
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design.Serialization;
using XmlSerializer;
using LimnorDesigner;
using System.ComponentModel.Design;
using System.Collections;
using VSPrj;
using System.Xml;
using XmlUtility;
using System.IO;
using TraceLog;
using System.Windows.Forms;
using System.Diagnostics;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Specialized;
using VPL;
using LimnorKiosk;
using System.Resources;
using System.Collections.ObjectModel;
using System.Drawing;
using LimnorDesigner.Property;
using LimnorDesigner.Event;
using LimnorDesigner.MethodBuilder;
using LimnorDesigner.Interface;
using Limnor.Drawing2D;
using LimnorDatabase;
using LimnorDesigner.Action;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Services;
using Limnor.CopyProtection;
using System.Text.RegularExpressions;
using LimnorDesigner.ResourcesManager;
using System.Globalization;
using WindowsUtility;
using Limnor.WebBuilder;
using MathExp;
using System.ServiceModel;
using WebServerProcessor;
using Limnor.WebServerBuilder;
using Limnor.Remoting.Host;
using LimnorDesigner.MenuUtil;
using LimnorDesigner.Web;
using MathComponent;
using LimnorDesigner.DesignTimeType;
using LimnorVisualProgramming;
using FileUtil;
using System.Web;
using ProgElements;
using MathItem;
using Limnor.PhpComponents;
using HtmlAgilityPack;
using Limnor.Reporting;

namespace LimnorCompiler
{
	/// <summary>
	/// use it to load a class into designer for generating code
	/// this.LoaderHost is used to create hosted objects
	/// </summary>
	class CodeCompiler : BasicDesignerLoader, IObjectFactory, ILimnorCodeCompiler, IWebServerCompiler
	{
		#region fields and constructors
		const string XClassName = "VPL.XClass`1";
		const string XControlName = "VPL.XControl`1";
		const string Var_Components = "components";
		const string PROP_Namespace = "Namespace";
		private CompilerSettings _settings;
		private DesignSurface designSurface;
		private bool bRet = false;
		private bool _useDrawing;
		private bool isStaticClass;
		private bool _isWebService;
		private bool _isForm;
		private bool _isSetup;
		private bool _isWebApp;
		private bool _isWebPage;
		private bool _isConsole;
		private bool _isRemotingHost;
		private bool _useDatetimePicker;
		private string _iframeId;
		private ArrayList _errors = null;
		private string _xmlFile;
		private ObjectIDmap _objMap;
		private ClassPointer _rootClassId;
		private HtmlAgilityPack.HtmlDocument _htmlDoc = null;
		private LimnorProject _project;
		private object _rootObject;
		private Type _objectType;
		private XmlDocument doc;
		private XmlObjectReader xr;
		private CodeTypeDeclaration td;
		private CodeNamespace ns;
		private ServerScriptHolder _serverScripts; //for php
		private ServerDotNetCodeHolder _serverPageCode; //for aspx
		private List<Dictionary<string, string>> _phpFiles;
		private StringCollection _phpIncludes;
		private Dictionary<string, string> _jsFiles;
		private string _iconName;
		private bool _useResources;
		private bool _useResourcesX;
		private bool _debug;
		private string _propertyHolderName;
		private ClassCompiler _classCompiler;
		private WebClientValueCollection _bodyValues = null;
		private Dictionary<string, WebClientValueCollection> _customValues;
		private List<Dictionary<string, string>> _htmlParts;
		private Dictionary<string, Assembly> _refLocations;
		private List<ResourceFile> _resourceFileList;
		private StringCollection _staticEvents;
		private DesignObjects _existingDesignObjects;
		private bool _usePageNavigator;
		private string _use32bit;
		private IContainer _container;
		private bool _flushFinished;
		private List<Type> _referencedTypes;
		private List<IAppConfigConsumer> _appConfigConsumers;
		private Dictionary<string, Dictionary<string, StringCollection>> _customClientMethodFormSubmissions;
		private Dictionary<string, IXType> _xtypeList;
		private Dictionary<string, IClassRef> _xtypeClassPointerList;
		private string _extra_classId;
		private string _appname;
		private string _kioskBackgroundName;
		public CodeCompiler(ClassCompiler classCompiler, LimnorProject project, XmlDocument prjDoc, string xmlFile, string componentNamespace, string folder, string appName, string kioskbackgroundName, bool debug)
		{
			_classCompiler = classCompiler;
			_debug = debug;
			_project = project;
			_xmlFile = xmlFile;
			_appname = appName;
			_kioskBackgroundName = kioskbackgroundName;
			if (prjDoc != null)
			{
				doc = prjDoc;
				XmlAttribute xa = doc.CreateAttribute(XmlTags.XMLATT_filename);
				doc.DocumentElement.Attributes.Append(xa);
				xa.Value = xmlFile;
				_settings = new CompilerSettings(componentNamespace, XmlUtil.GetNameAttribute(doc.DocumentElement), folder);
				_objectType = XmlUtil.GetLibTypeAttribute(doc.DocumentElement, out _extra_classId);
				_existingDesignObjects = DesignObjects.RemoveDesignObjects(_project, XmlUtil.GetAttributeUInt(doc.DocumentElement, XmlTags.XMLATT_ClassID));
			}
			else
			{
				_settings = new CompilerSettings(componentNamespace, xmlFile, folder);
				_objectType = typeof(object);
			}
		}
		#endregion
		#region public methods
		public IWebDataRepeater GetDataRepeaterHolder(HtmlElement_Base element)
		{
			IWebDataRepeater dp = null;
			if (_htmlDoc != null)
			{
				if (element != null && !string.IsNullOrEmpty(element.id))
				{
					HtmlNode hbbNode = _htmlDoc.DocumentNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture, "//*[@id=\"{0}\"]", element.id));
					if (hbbNode != null)
					{
						HtmlNode pn = hbbNode.ParentNode;
						while (pn != null)
						{
							if (string.Compare(pn.Name, "div", StringComparison.OrdinalIgnoreCase) == 0)
							{
								string id = pn.Id;
								if (!string.IsNullOrEmpty(id))
								{
									foreach (HtmlElement_BodyBase hbb0 in _rootClassId.UsedHtmlElements)
									{
										if (string.CompareOrdinal(id, hbb0.id) == 0)
										{
											HtmlElement_div hdiv = hbb0 as HtmlElement_div;
											if (hdiv != null)
											{
												if (hdiv.IsRepeaterHolder)
												{
													dp = hdiv;
												}
											}
											break;
										}
									}
									if (dp != null)
									{
										break;
									}
								}
							}
							pn = pn.ParentNode;
						}
					}
				}
			}
			return dp;
		}
		/// <summary>
		/// for compiling purpose,
		/// find all the assemblies this component is using,
		/// include those in GAC,
		/// </summary>
		/// <param name="sc"></param>
		public void FindReferenceLocations(Dictionary<string, Assembly> sc)
		{
			if (_refLocations != null)
			{
				Dictionary<string, Assembly>.Enumerator e = _refLocations.GetEnumerator();
				while (e.MoveNext())
				{
					string loc = e.Current.Key.ToLowerInvariant();
					if (!sc.ContainsKey(loc))
					{
						sc.Add(loc, e.Current.Value);
					}
				}
			}
		}
		/// <summary>
		/// load the object into designer
		/// </summary>
		/// <returns></returns>
		public virtual bool Load()
		{
			bRet = true;
			designSurface = new DesignSurface();
			designSurface.BeginLoad(this);
			if (_errors != null && _errors.Count > 0)
				bRet = false;
			return bRet;
		}
		/// <summary>
		/// using ShowPreviousForm/ShowNextForm actions
		/// </summary>
		/// <returns></returns>
		public bool UsePageNavigator()
		{
			if (_usePageNavigator)
				return true;
			if (_rootClassId == null)
				return false;
			return _rootClassId.UsePageNavigator();
		}
		public void SetUsePageNavigator()
		{
			_usePageNavigator = true;
		}
		public bool UseCopyProtection()
		{
			foreach (object v in _objMap.Keys)
			{
				CopyProtector cp = v as CopyProtector;
				if (cp != null)
				{
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// generate source code
		/// </summary>
		/// <returns></returns>
		public bool GenerateCodeFiles()
		{
			this.Modified = true;
			_flushFinished = false;
			try
			{
				this.Flush();
			}
			catch (Exception e)
			{
				if (_flushFinished)
				{
				}
				else
				{
					if (_errors == null)
					{
						_errors = new ArrayList();
					}
					_errors.Add(TraceLogClass.ExceptionMessage0(e));
				}
			}
			if (_errors != null && _errors.Count > 0)
			{
				bRet = false;
			}
			return bRet;
		}
		public IAction GetPublicAction(TaskID taskId)
		{
			return taskId.GetPublicAction(_rootClassId);
		}
		/// <summary>
		/// if a 32-bit library component is used then the program must be compiled for x86
		/// </summary>
		/// <returns></returns>
		public string Report32Usage()
		{
			return _use32bit;
		}
		/// <summary>
		/// generate resources
		/// </summary>
		public void GenerateResourceFile()
		{
			if (_project.ProjectType != EnumProjectType.WebAppPhp)
			{
				//generate resources
				Process p = new Process();
				//
				ProcessStartInfo psI = new ProcessStartInfo("cmd");
				psI.UseShellExecute = false;
				psI.RedirectStandardInput = false;
				psI.RedirectStandardOutput = true;
				psI.RedirectStandardError = true;
				psI.CreateNoWindow = true;
				p.StartInfo = psI;
				//
				p.StartInfo.FileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "resgen.exe");
				if (!System.IO.File.Exists(p.StartInfo.FileName))
				{
					throw new DesignerException("File not found:{0}", p.StartInfo.FileName);
				}
				string stdout;
				string errout;
				if (_useResources)
				{
					p.StartInfo.Arguments = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						" /compile \"{0}\"", _settings.ResourceFilename);
					logMsg("generating resoureces by [{0}] [{1}] into [{2}]", p.StartInfo.FileName, p.StartInfo.Arguments, _settings.ResourceFilename);
					p.Start();
					stdout = p.StandardOutput.ReadToEnd();
					errout = p.StandardError.ReadToEnd();
					p.WaitForExit();
					if (p.ExitCode != 0)
					{
						logMessage("Error code {0}, output:{1}, error:{2} for calling {3} {4}", p.ExitCode, stdout, errout, p.StartInfo.FileName, p.StartInfo.Arguments);
					}
				}
				else
				{
					logMsg("Not use resources");
				}
				if (_useResourcesX)
				{
					p.StartInfo.Arguments = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"\"{0}\" \"{4}\" /str:c#,{1},{2},\"{3}.cs\"",
						_settings.ResourceFilenameX, _settings.Namespace, _settings.NameX, _settings.SourceFilenameX, _settings.ResourcesX);
					logMsg("generating resoureces X by [{0}] [{1}]", p.StartInfo.FileName, p.StartInfo.Arguments);
					p.Start();
					stdout = p.StandardOutput.ReadToEnd();
					errout = p.StandardError.ReadToEnd();
					p.WaitForExit();
					if (p.ExitCode != 0)
					{
						logMessage("Error code {0}, output:{1}, error:{2} for calling {3} {4}", p.ExitCode, stdout, errout, p.StartInfo.FileName, p.StartInfo.Arguments);
					}
				}
				else
				{
					logMsg("Not use resources X");
				}
			}
		}
		/// <summary>
		/// switch classes to design mode from compiling mode
		/// </summary>
		public void Cleanup()
		{
			RestoreDesignObjects();
		}
		#endregion
		#region BasicDesignerLoader overrides
		/// <summary>
		/// start class compiling process
		/// </summary>
		/// <param name="serializationManager"></param>
		protected override void PerformFlush(IDesignerSerializationManager serializationManager)
		{
			DateTime startTime = DateTime.Now;
			TraceLogClass.TraceLog.Trace("Compiling {0} starts ........", _settings.TypeName);
			TraceLogClass.TraceLog.IndentIncrement();
			try
			{
				_originRunContext = VPLUtil.CurrentRunContext;
				_rootClassId.SetRunContext();
				collectXTypes();
				if (_isSetup)
				{
					//msi project is not compile by this function
				}
				else
				{
					xr.EnableTrace(true);
					bool isEntryPoint = IsEntryPoint;
					if (isEntryPoint || IsWebApp)
					{
						isStaticClass = true;
					}
					_useDrawing = _rootClassId.UseDrawing;
					if (_useDrawing)
					{
						_classCompiler.SetUseDrawing();
					}
					else
					{
						DrawingPage pg = _rootObject as DrawingPage;
						if (pg != null)
						{
							pg.IsCompiling = true;
							pg.DrawingLayerList = null;
						}
					}
					//
					_use32bit = _rootClassId.Report32Usage();
					//
					CodeCompileUnit code = new CodeCompileUnit();
					ns = new CodeNamespace(_settings.Namespace);
					//
					InterfaceClass ifc = VPLUtil.GetObject(_rootObject) as InterfaceClass;
					//
					if (ifc != null)
					{
						ifc.Holder = _rootClassId;
						ifc.InterfaceName = _rootClassId.Name;
						td = ifc.Compile();
						ns.Types.Add(td);
						code.Namespaces.Add(ns);
						CompilerUtil.AddAttribute(ifc, td.CustomAttributes, _isWebService, true, _classCompiler.RootNamespace);
						addInterfaces();
					}
					else
					{
						fixDataGridViewColumnNames(_rootObject as Control);
						string serializerName;
						Type t;
#if DOTNET40
						serializerName = "System.ComponentModel.Design.Serialization.RootCodeDomSerializer, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
#else
						serializerName = "System.ComponentModel.Design.Serialization.RootCodeDomSerializer, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
#endif
						t = this.LoaderHost.GetType(serializerName);
						CodeDomSerializer cds = Activator.CreateInstance(t) as CodeDomSerializer;
						IDesignerSerializationManager manager2 = this.LoaderHost.GetService(typeof(IDesignerSerializationManager)) as IDesignerSerializationManager;
						td = cds.Serialize(manager2, _rootObject) as CodeTypeDeclaration;
						//
//#if DOTNET40
						net4_adjustFieldNames();
//#endif
						//
						HtmlElement_Base.GetDataRepeater = this.GetDataRepeaterHolder;
						if (_rootClassId.IsWebPage)
						{
							string htmlDesignFile = string.Empty;
							bool useHtmlEditor = DesignService.IsUsingHtmlEditor();
							if (useHtmlEditor)
							{
								htmlDesignFile = WebHost.GetHtmlFile(_xmlFile);
								if (File.Exists(htmlDesignFile))
								{
									_htmlDoc = new HtmlAgilityPack.HtmlDocument();
									_htmlDoc.Load(htmlDesignFile);
								}
							}
						}
						//
						removeReadOnlyProperties();
						if (IsWebObject)
						{
							adjustWebObjects();
						}
						else
						{
							_isForm = typeof(Form).IsAssignableFrom(_objectType);
							_isRemotingHost = typeof(RemotingHost).IsAssignableFrom(_objectType);
						}
						//
						fixSpecialCases();
						//
						CompilerUtil.AddAttribute(_rootClassId, td.CustomAttributes, _isWebService, false, _classCompiler.RootNamespace);
						//
						if (_isWebService)
						{
							adjustForWebService();
						}
						if (_isRemotingHost)
						{
							adjustForRemotingService();
						}
						//
						if (typeof(EasyDataSet).IsAssignableFrom(_rootClassId.BaseClassType)
							|| typeof(EasyUpdator).IsAssignableFrom(_rootClassId.BaseClassType)
							)
						{
							CodeAttributeArgument aa = new CodeAttributeArgument(new CodePrimitiveExpression("System.Windows.Forms.Design.BindingSourceDesigner, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
							CodeAttributeDeclaration ad = new CodeAttributeDeclaration(
								new CodeTypeReference(typeof(DesignerAttribute)), new CodeAttributeArgument[] { aa }
								);
							td.CustomAttributes.Add(ad);
						}
						//
						addInterfaces();
						//
						databaseSupport();
						//
						addDefaultInstance();
						//
						adjustInitFunction();
						//
						string desc = SerializeUtil.GetNodeDescription(doc.DocumentElement);
						if (!string.IsNullOrEmpty(desc))
						{
							td.Comments.Add(new CodeCommentStatement("<summary>", true));
							td.Comments.Add(new CodeCommentStatement(desc, true));
							td.Comments.Add(new CodeCommentStatement("</summary>", true));
						}
						//
						ns.Types.Add(td);
						code.Namespaces.Add(ns);
						//
						//Adjust derivation for entry point and base classes==================
						bool baseTypeProcessed = adjustDerivation();
						//Adjust XClass ==================================================
						adjustXClass(baseTypeProcessed);
						//
						//remove declarations/initializer/Control.Add for members from parents
						removeFieldsFromParents();
						//
						//add toolbox itme bitmap
						if (!string.IsNullOrEmpty(_iconName))
						{
							td.CustomAttributes.Add(new CodeAttributeDeclaration(
								new CodeTypeReference(typeof(ToolboxBitmapAttribute)),
								new CodeAttributeArgument[]{
									new CodeAttributeArgument(new CodeTypeOfExpression(_settings.TypeName)),
									new CodeAttributeArgument(new CodePrimitiveExpression(_iconName))
								}));
							addRef(typeof(ToolboxBitmapAttribute));
						}
						//
						// Create a constructor that will call the InitializeComponent()
						// that we just generated.
						//
						getDefaultConstructor();
						//
						addKioskSupport(ns);
						//
						adjustComponentsAccessLevel();
						//
						//==================================================================
						if (isEntryPoint)
						{
							//create entry point
							LimnorApp app = _rootObject as LimnorApp;
							app.Namespace = _settings.Namespace;
							app.SetProject(_project);
							IList<ComponentID> rootClasses = _project.GetAllComponents();
							app.ExportCode(td, ns, rootClasses, _classCompiler.OutputFullpath, _project.ProjectAssemblyName, Debug);
							//
							td.Attributes = MemberAttributes.Private;
						}
						//
						addDebugCode(isEntryPoint);
						//
						//adjust the type of the variables ==============================
						adjustClassInstanceComponents(true);
						//
						if (isEntryPoint)
						{
							removeComponentPointerFromWinAppInit();
						}
						if (!_isWebPage)
						{
							if (!_isWebApp || _project.ProjectType == EnumProjectType.WebAppAspx) // _builder.WebServerTechnology == Limnor.WebServerBuilder.EnumWebServerProcessor.Aspx)
							{
								//generate properties============================================
								createProperties();
								//
								//generate methods===============================================
								createMethods();
								//generate events================================================
								createEvents();
								//generate event handlers========================================
								createEventHandlers();
							}
						}
						//
						// initialize custom property values
						setCustomPropertyValues();
						//
						createConstructors();
						//
						//support drawings
						addDrawingSupport();
						//
						supportICustomEventMethodDescriptor();
						//
						addResourceMap();
						//
						if (IsForm)
						{
							adjustFormClass();
						}
						else
						{
							if (!isEntryPoint && !_isWebApp)
							{
								if (!(_rootObject is Control))
								{
									addComponents();
								}
							}
						}
						//
						if (isEntryPoint)
						{
							adjustEntryPoint();
						}
					}//interface or class
					//
					addReferences(ns);
					//
					//===============================================================
					//
					if (_isWebPage)
					{
						generateWebPage(code);
					}
					else if (_isWebApp)
					{
						generateWebApp(code);
					}
					else
					{
						generateCode(code);
					}
				}
			}
			catch (Exception er)
			{
				logException(er);
				bRet = false;
			}
			finally
			{
				HtmlElement_BodyBase.GetDataRepeater = null;
				DateTime endTime = DateTime.Now;
				TimeSpan ts = endTime.Subtract(startTime);
				TraceLogClass.TraceLog.IndentDecrement();
				if (HasErrors)
				{
					TraceLogClass.TraceLog.Trace("Compiling {0} failed. Time used:{1} seconds ........", _settings.TypeName, ts.TotalSeconds);
				}
				else
				{
					TraceLogClass.TraceLog.Trace("Compiling {0} ends. Time used:{1} seconds ........", _settings.TypeName, ts.TotalSeconds);
				}
				VPLUtil.CurrentRunContext = _originRunContext;
				_flushFinished = true;
			}
		}
		/// <summary>
		/// switch classes from compiling mode to design mode
		/// </summary>
		public void RestoreDesignObjects()
		{
			Form frm = _rootObject as Form;
			if (frm != null)
			{
				if (!frm.IsDisposed && !frm.Disposing)
				{
					IWebPage wp = frm as IWebPage;
					if (wp != null)
					{
						wp.SetClosing();
					}
				}
			}
			if (designSurface != null)
			{
				designSurface.Dispose();
			}
			if (_existingDesignObjects != null)
			{
				lock (_existingDesignObjects)
				{
					if (_existingDesignObjects != null)
					{
						_existingDesignObjects.RestoreDesignObjects(_project);
						_existingDesignObjects = null;
					}
				}
			}
		}
		private EnumRunContext _originRunContext = EnumRunContext.Server;
		/// <summary>
		/// load class for compiling
		/// </summary>
		/// <param name="serializationManager"></param>
		protected override void PerformLoad(IDesignerSerializationManager serializationManager)
		{
			try
			{
				_originRunContext = VPLUtil.CurrentRunContext;
				_useResourcesX = false;
				_useResources = false;
				if (_project.ProjectType != EnumProjectType.WebAppPhp)
				{
					this.LoaderHost.AddService(typeof(IResourceService), new ResourceService(_settings.ResourceFilename));
				}
				XmlNode rootNode = doc.DocumentElement;
				_isSetup = XmlUtil.GetAttributeBoolDefFalse(rootNode, XmlTags.XMLATT_isSetup);
				if (_isSetup)
				{
				}
				else
				{
					_objMap = new ObjectIDmap(_project, DesignUtil.GetRootComponentId(rootNode), rootNode);
					_objMap.SetTypedData<ObjectIDmap>(_objMap);
					_rootClassId = new ClassPointer(_objMap);
					_objMap.SetTypedData<ClassPointer>(_rootClassId);
					xr = new XmlObjectReader(_objMap, ClassPointer.OnAfterReadRootComponent, ClassPointer.OnRootComponentCreated);
					_objMap.SetReader(xr);
					xr.IsForCompile = true;
					_rootObject = xr.ReadRootObject(this, rootNode);
					foreach (KeyValuePair<object, UInt32> kv in _objMap)
					{
						EasyDataSet eds = kv.Key as EasyDataSet;
						if (eds != null)
						{
							eds.Tables.Clear();
						}
					}
					IWithProject2 wp = _rootObject as IWithProject2;
					if (wp != null)
					{
						wp.SetProject(_project);
					}
					_rootClassId.SetRunContext();
					_isWebApp = (_rootObject is LimnorWebApp);
					_isWebPage = (_rootObject is IWebPage);
					_isConsole = (_rootObject is LimnorConsole);
					_isWebService = (typeof(WebService).IsAssignableFrom(_rootClassId.BaseClassType));
					if (xr.Errors != null && xr.Errors.Count > 0)
					{
						if (_errors == null)
							_errors = new ArrayList();
						_errors.AddRange(xr.Errors);
						xr.ResetErrors();
						bRet = false;
					}
					else
					{
						if (_isWebApp)
						{
							LimnorWebApp wa= _rootObject as LimnorWebApp;
							VPLUtil.SessionDataStorage = wa.SessionDataStorage;
						}
						_rootClassId.ValidateInterfaceImplementations();
						foreach (object v in _objMap.Keys)
						{
							CopyProtector cp = v as CopyProtector;
							if (cp != null)
							{
								cp.SetApplicationGuid(_project.ProjectGuid);
								cp.SetApplicationName(_project.ProjectName);
							}
							EasyGrid eg = v as EasyGrid;
							if (eg != null)
							{
								for (int i = 0; i < eg.Columns.Count; i++)
								{
									if (eg.Columns[i] != null)
									{
										if (eg.Columns[i].Site == null)
										{
										}
										else
										{
											if (string.IsNullOrEmpty(eg.Columns[i].Site.Name))
											{
												eg.Columns[i].Site.Name = eg.Columns[i].Name;
											}
										}
									}
								}
							}
						}
						//
						_rootClassId.ReloadEventActions(null);
						//
						IResourceService resService = null;
						IResourceWriter resWriter = null;
						Dictionary<UInt32, IAction> actionList = _rootClassId.GetActions();
						if (actionList != null)
						{
							foreach (IAction act in actionList.Values)
							{
								if (act != null)
								{
									List<ParameterValue> paramValues = act.ParameterValues;
									if (paramValues != null)
									{
										foreach (ParameterValue v in paramValues)
										{
											if (v.ValueType == EnumValueType.ConstantValue && v.ConstantValue != null)
											{
												if (XmlObjectWriter.IsBinaryValue(v.ConstantValue.Value))
												{
													string name = VPLUtil.CreateUniqueName("r");
													if (resService == null)
													{
														resService = new ResourceService(_settings.ResourceFilenameX);
														resWriter = resService.GetResourceWriter(System.Globalization.CultureInfo.InvariantCulture);
													}
													resWriter.AddResource(name, v.ConstantValue.Value);
													v.CodeName = name;
													v.CodeTypeName = _settings.NameX;
													_useResourcesX = true;
												}
											}
										}
									}
								}
							}
						}
						if (_rootObject is LimnorApp)
						{
							string iconFile = _rootClassId.FullIconPath;
							if (System.IO.File.Exists(iconFile))
							{
								_classCompiler.SetAppIcon(iconFile);
							}
						}
						else
						{
							_useDrawing = _rootClassId.UseDrawing;
							if (_useDrawing)
							{
								_classCompiler.SetUseDrawing();
							}
						}
						string iconFile2 = SerializeUtil.ReadIconFilename(rootNode);
						if (!string.IsNullOrEmpty(iconFile2))
						{
							if (resService == null)
							{
								resService = new ResourceService(_settings.ResourceFilenameX);
								resWriter = resService.GetResourceWriter(System.Globalization.CultureInfo.InvariantCulture);
							}
							_useResourcesX = true;
						}
						if (resService != null)
						{
							resWriter.Generate();
							resWriter.Close();
						}
						//
						_iconName = addEmbededFile(iconFile2);
						bRet = true;
					}
				}
			}
			catch (Exception er)
			{
				logException(er);
				bRet = false;
			}
			finally
			{
				VPLUtil.CurrentRunContext = _originRunContext;
			}
		}
		#endregion
		#region private methods
		private StringCollection _dataGridViewUsingEasyDataSet;
		private void fixDataGridViewColumnNames(Control c)
		{
			if (c != null)
			{
				DataGridView dgv = c as DataGridView;
				if (dgv != null)
				{
					for (int i = 0; i < dgv.Columns.Count; i++)
					{
						if (dgv.Columns[i].Site != null && dgv.Columns[i].Site.DesignMode)
						{
							if (string.IsNullOrEmpty(dgv.Columns[i].Site.Name))
							{
								if (!string.IsNullOrEmpty(dgv.Columns[i].Name))
								{
									dgv.Columns[i].Site.Name = dgv.Columns[i].Name;
								}
							}
						}
					}
					EasyDataSet eds = dgv.DataSource as EasyDataSet;
					if (eds != null)
					{
						if (_dataGridViewUsingEasyDataSet == null)
							_dataGridViewUsingEasyDataSet = new StringCollection();
						if (!_dataGridViewUsingEasyDataSet.Contains(dgv.Name))
						{
							_dataGridViewUsingEasyDataSet.Add(dgv.Name);
						}
					}
				}
				else
				{
					for (int i = 0; i < c.Controls.Count; i++)
					{
						fixDataGridViewColumnNames(c.Controls[i]);
					}
				}
			}
		}
		private void logException(Exception er)
		{
			if (_errors == null)
			{
				_errors = new ArrayList();
			}
			_errors.Add(er);
		}
		private void logMessage(string msg, params object[] values)
		{
			_classCompiler.LogMessage(msg, values);
			if (_errors == null)
			{
				_errors = new ArrayList();
			}
			if (values != null && values.Length > 0)
			{
				msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, msg, values);
			}
			_errors.Add(msg);
		}
		private void logMsg(string msg, params object[] values)
		{
			_classCompiler.LogMessage(msg, values);
		}
		/// <summary>
		/// copy the file to be embeded to EmbedFileFolder
		/// </summary>
		/// <param name="filename">it is a relative path</param>
		private string addEmbededFile(string filename)
		{
			if (!string.IsNullOrEmpty(filename))
			{
				string source = System.IO.Path.Combine(_settings.Folder, filename);
				if (System.IO.File.Exists(source))
				{
					string key = filename.Replace("\\", ".");
					string targetName = _settings.Namespace + "." + key;
					string target = System.IO.Path.Combine(_settings.EmbedFileFolder, targetName);
					System.IO.File.Copy(source, target, true);
					if (_resourceFileList == null)
						_resourceFileList = new List<ResourceFile>();
					ResourceFile f = new ResourceFile();
					f.KeyName = key;
					f.ResourceFilename = source;
					f.TargetFilename = target;
					_resourceFileList.Add(f);
					return key;
				}
			}
			return null;
		}

		#endregion
		#region code generating
		/// <summary>
		/// setup parts of php
		/// </summary>
		private void setWebServerCompilerPhp()
		{
			_serverScripts = new ServerScriptHolder();
			foreach (KeyValuePair<object, UInt32> kv in _objMap)
			{
				IWebServerCompilerHolder h = kv.Key as IWebServerCompilerHolder;
				if (h != null)
				{
					h.SetWebServerCompiler(this);
					EasyDataSet eds = h as EasyDataSet;
					if (eds != null)
					{
						if (eds.Master != null && eds.MasterKeyColumns != null && eds.MasterKeyColumns.Length > 0)
						{
							string methodName = string.Format(CultureInfo.InvariantCulture, "fetchDetail{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
							//
							Dictionary<string, string> keycodes = new Dictionary<string, string>();
							eds.SetMasterFunction(methodName);
							eds.QueryDef.PrepareQuery();
							MethodClass mc = new MethodClass(_rootClassId);
							mc.MethodName = methodName;
							mc.WebUsage = EnumMethodWebUsage.Server;
							mc.Parameters.Add(new ParameterClass(typeof(string), "value", mc));
							ParameterList pl = new ParameterList();
							for (int i = 0; i < eds.MasterKeyColumns.Length; i++)
							{
								EPField f = new EPField(i, eds.MasterKeyColumns[i]);
								for (int k = 0; k < eds.Field_Name.Length; k++)
								{
									if (string.Compare(eds.MasterKeyColumns[i], eds.Field_Name[k], StringComparison.OrdinalIgnoreCase) == 0)
									{
										f.OleDbType = eds.Field_OleDbType[k];
										break;
									}
								}
								pl.Add(f);
							}
							BranchList bl = new BranchList(mc);
							AB_ActionString al = new AB_ActionString(mc);
							al.ActionList = new BranchList(mc);
							bl.Add(al);
							//QueryWithWhereDynamic
							eds.DynamicParameters = new DbParameterListExt(pl);
							ActionClass act = new ActionClass(_rootClassId);
							MethodInfo mif = typeof(EasyDataSet).GetMethod("QueryWithWhereDynamic");
							MethodInfoPointer mip = new MethodInfoPointer(mif);
							mip.ReturnType2 = typeof(bool);
							mip.MemberName = "QueryWithWhereDynamic";
							MemberComponentId mci = new MemberComponentId(_rootClassId, eds, kv.Value);
							mip.Owner = mci;
							act.ActionMethod = mip;
							act.ScopeRunAt = EnumWebRunAt.Server;
							Dictionary<string, PropertyPointer> ppl = new Dictionary<string, PropertyPointer>();
							for (int k = 0; k < act.ParameterCount; k++)
							{
								ParameterValue pv = act.ParameterValues[k];
								if (string.CompareOrdinal(pv.Name, "where") == 0)
								{
									StringBuilder sbp = new StringBuilder(eds.MasterKeyColumns[0]);
									sbp.Append("=@");
									sbp.Append(eds.MasterKeyColumns[0]);
									for (int i = 1; i < eds.MasterKeyColumns.Length; i++)
									{
										sbp.Append(" ADN ");
										sbp.Append(eds.MasterKeyColumns[i]);
										sbp.Append("=@");
										sbp.Append(eds.MasterKeyColumns[i]);
									}
									string w = eds.Where;
									if (!string.IsNullOrEmpty(w))
									{
										w = w.Trim();
									}
									if (string.IsNullOrEmpty(w))
									{
										w = sbp.ToString();
									}
									else
									{
										w = string.Format(CultureInfo.InvariantCulture, "({0}) AND {1}", w, sbp.ToString());
									}
									pv.ValueType = EnumValueType.ConstantValue;
									pv.DataType = new DataTypePointer(typeof(string));
									pv.SetValue(w);
								}
								else
								{
									PropertyPointer pp = new PropertyPointer();
									pp.MemberName = pv.Name.Substring(1);
									PropertyPointer ppo = new PropertyPointer();
									ppo.MemberName = "Fields";
									MemberComponentId mcid = new MemberComponentId(_rootClassId, eds.Master, _objMap.GetObjectID(eds.Master));
									ppo.Owner = mcid;
									pp.Owner = ppo;
									pv.SetValue(pp);
									ppl.Add(pp.MemberName, pp);
									keycodes.Add(pp.MemberName, pp.DataPassingCodeName);
								}
							}
							eds.SetKeyFieldCode(keycodes);
							AB_SingleAction abs = new AB_SingleAction(mc);
							abs.ActionData = act;
							al.ActionList.Add(abs);
							for (int k = 0; k < eds.MasterKeyColumns.Length; k++)
							{
								abs = new AB_SingleAction(mc);
								PropertyPointer pp;
								if (ppl.TryGetValue(eds.MasterKeyColumns[k], out pp))
								{
									string v = pp.GetPhpScriptReferenceCode(_serverScripts.Methods);
									abs.ActionData = new ActionScript(
										string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('masterfield_{0}_{1}',{2});\r\n", eds.TableName, eds.MasterKeyColumns[k], v),
										_rootClassId, mc, EnumWebActionType.Server);
									al.ActionList.Add(abs);
								}
							}
							mc.ActionList = bl;
							createMethodPhp(_serverScripts.Methods, mc);
							//
							StringCollection execCode = new StringCollection();
							execCode.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}($value);\r\n", methodName));
							this.AppendServerExecutePhpCode(execCode);
						}
					}
				}
			}
		}
		private void collectXTypes()
		{
			_xtypeList = new Dictionary<string, IXType>();
			_xtypeClassPointerList = new Dictionary<string, IClassRef>();
			foreach (KeyValuePair<object, UInt32> kv in _objMap)
			{
				IXType x = kv.Key as IXType;
				if (x != null)
				{
					string nm = ((IComponent)(kv.Key)).Site.Name;
					_xtypeList.Add(nm, x);
					IClassRef cr = _objMap.GetClassRefById(kv.Value);
					if (cr != null)
					{
						_xtypeClassPointerList.Add(nm, cr);
					}
				}
			}
		}
		private void addVirtualPropertyValues()
		{
			if (_xtypeList.Count > 0)
			{
				CodeMemberMethod cmmInit = getInitializeMethod();
				if (cmmInit != null)
				{
					foreach (KeyValuePair<string, IXType> kv in _xtypeList)
					{
						Dictionary<string, object> ps = kv.Value.GetPropertyValues();
						if (ps != null && ps.Count > 0)
						{
							int k = -1;
							for (int i = 0; i < cmmInit.Statements.Count; i++)
							{
								CodeAssignStatement cas = cmmInit.Statements[i] as CodeAssignStatement;
								if (cas != null)
								{
									if (cas.Right is CodeObjectCreateExpression)
									{
										CodePropertyReferenceExpression cp = cas.Left as CodePropertyReferenceExpression;
										if (cp != null)
										{
											if (string.CompareOrdinal(cp.PropertyName, kv.Key) == 0)
											{
												k = i;
												break;
											}
										}
										else
										{
											CodeFieldReferenceExpression cf = cas.Left as CodeFieldReferenceExpression;
											if (cf != null)
											{
												if (string.CompareOrdinal(cf.FieldName, kv.Key) == 0)
												{
													k = i;
													break;
												}
											}
										}
									}
								}
							}
							if (k >= 0)
							{
								foreach (KeyValuePair<string, object> kvp in ps)
								{
									CodeAssignStatement cas = new CodeAssignStatement();
									cas.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(kv.Key), kvp.Key);
									cas.Right = ObjectCreationCodeGen.ObjectCreationCode(kvp.Value);
									cmmInit.Statements.Insert(++k, cas);
								}
							}
						}
					}
				}
			}
		}
		private void adjustXClass(bool baseTypeProcessed)
		{
			if (!baseTypeProcessed)
			{
				if (td.BaseTypes != null)
				{
					foreach (CodeTypeReference tr in td.BaseTypes)
					{
						if (string.CompareOrdinal(tr.BaseType, XClassName) == 0 || string.CompareOrdinal(tr.BaseType, XControlName) == 0)
						{
							tr.BaseType = tr.TypeArguments[0].BaseType;
							tr.TypeArguments.Clear();
							Type t = VPLUtil.GetObjectType(_rootClassId.BaseClassType);
							Type[] targs = t.GetGenericArguments();
							if (targs != null)
							{
								for (int i = 0; i < targs.Length; i++)
								{
									tr.TypeArguments.Add(targs[i]);
								}
							}
							break;
						}
					}
				}
			}
			foreach (CodeTypeMember ctm in td.Members)
			{
				CodeMemberField cmf = ctm as CodeMemberField;
				if (cmf != null)
				{
					if (string.CompareOrdinal(cmf.Type.BaseType, XClassName) == 0)
					{
						IXType ix = null;
						ClassInstancePointer crp = null;
						_xtypeList.TryGetValue(cmf.Name, out ix);
						if (ix != null)
						{
							crp = _objMap.GetClassRefByObject(ix) as ClassInstancePointer;
						}
						if (crp != null)
						{
							cmf.Type.BaseType = crp.Definition.CodeName;
							cmf.Type.TypeArguments.Clear();
						}
						else
						{
							cmf.Type.BaseType = cmf.Type.TypeArguments[0].BaseType;
							cmf.Type.TypeArguments.Clear();
							if (ix != null)
							{
								Type[] args = ix.GetTypeParameters();
								if (args != null)
								{
									for (int i = 0; i < args.Length; i++)
									{
										cmf.Type.TypeArguments.Add(args[i]);
									}
								}
							}
						}
					}
				}
			}
			bool isEntryPoint = IsEntryPoint;
			CodeMemberMethod cmmInit = getInitializeMethod();
			List<CodeStatement> statementsToRemove = new List<CodeStatement>();
			foreach (CodeStatement cs1 in cmmInit.Statements)
			{
				CodeAssignStatement cas = cs1 as CodeAssignStatement;
				if (cas != null)
				{
					CodeObjectCreateExpression coc = cas.Right as CodeObjectCreateExpression;
					if (coc != null)
					{
						if (string.CompareOrdinal(coc.CreateType.BaseType, "LimnorDesigner.ClassInstancePointer") == 0)
						{
							continue;
						}
						else if (string.CompareOrdinal(coc.CreateType.BaseType, XClassName) == 0)
						{
							string xname = null;
							CodePropertyReferenceExpression cpr0 = cas.Left as CodePropertyReferenceExpression;
							if (cpr0 != null)
							{
								xname = cpr0.PropertyName;
							}
							else
							{
								CodeFieldReferenceExpression ff0 = cas.Left as CodeFieldReferenceExpression;
								if (ff0 != null)
								{
									xname = ff0.FieldName;
								}
							}
							if (string.IsNullOrEmpty(xname))
							{
								throw new DesignerException("XType [{0}] not found", coc.CreateType.BaseType);
							}
							else
							{
								IClassRef cr = null;
								if (_xtypeClassPointerList.TryGetValue(xname, out cr))
								{
									coc.CreateType.BaseType = cr.TypeString;
								}
								else
								{
									coc.CreateType.BaseType = coc.CreateType.TypeArguments[0].BaseType;
								}
								coc.CreateType.TypeArguments.Clear();
								object[] vs = null;
								CodeExpression[] exprs = null;
								IXType x;
								if (_xtypeList.TryGetValue(xname, out x))
								{
									if (cr == null)
									{
										Type[] args = x.GetTypeParameters();
										if (args != null)
										{
											for (int i = 0; i < args.Length; i++)
											{
												coc.CreateType.TypeArguments.Add(args[i]);
											}
										}
									}
									exprs = x.GetConstructorValueExpressions();
									if (exprs != null && exprs.Length>0)
									{
										for (int i = 0; i < exprs.Length; i++)
										{
											coc.Parameters.Add(exprs[i]);
										}
									}
									else
									{
										vs = x.GetConstructorValues();
										if (vs != null && vs.Length > 0)
										{
											for (int i = 0; i < vs.Length; i++)
											{
												coc.Parameters.Add(ObjectCreationCodeGen.ObjectCreationCode(vs[i]));
											}
										}
									}
								}
							}
						}
					}
					CodePropertyReferenceExpression cpr = cas.Left as CodePropertyReferenceExpression;
					if (cpr != null)
					{
						if (isEntryPoint)
						{
							if (cpr.TargetObject is CodeThisReferenceExpression)
							{
								if (string.Compare(cpr.PropertyName, "OneInstanceOnly", StringComparison.Ordinal) == 0)
								{
									statementsToRemove.Add(cs1);
									continue;
								}
							}
						}
						if (string.Compare(cpr.PropertyName, "Name", StringComparison.Ordinal) == 0)
						{
							CodeFieldReferenceExpression ff = cpr.TargetObject as CodeFieldReferenceExpression;
							if (ff != null)
							{
								if (_xtypeList.ContainsKey(ff.FieldName))
								{
									statementsToRemove.Add(cs1);
								}
							}
						}
					}
				}
			}
			foreach (CodeStatement cs1 in statementsToRemove)
			{
				cmmInit.Statements.Remove(cs1);
			}
			if (VPLUtil.StaticOwnerCount > 0)
			{
				CodeTypeConstructor ctc = null;
				Dictionary<Type, object> owners = VPLUtil.StaticOwners;
				Dictionary<Type, object>.Enumerator staticTypes = owners.GetEnumerator();
				while (staticTypes.MoveNext())
				{
					ICustomEventMethodType iemt = staticTypes.Current.Value as ICustomEventMethodType;
					if (!(iemt.ValueType.IsSubclassOf(typeof(LimnorApp))))
					{
						PropertyDescriptorCollection props = iemt.GetProperties(EnumReflectionMemberInfoSelectScope.StaticOnly, false, true, true);
						foreach (PropertyDescriptor prop in props)
						{
							if (string.CompareOrdinal(prop.Name, "Name") != 0 && !prop.IsReadOnly)
							{
								DesignerSerializationVisibilityAttribute visibility = (DesignerSerializationVisibilityAttribute)prop.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
								if (visibility.Visibility != DesignerSerializationVisibility.Hidden)
								{
									object v = null;
									try
									{
										v = prop.GetValue(iemt);
										if (VPLUtil.IsDefaultValue(v))
										{
											continue;
										}
									}
									catch
									{
										continue;
									}
									CodeExpression iniv;
									iniv = ObjectCreationCodeGen.ObjectCreationCode(v);
									CodeAssignStatement cas = new CodeAssignStatement(
										new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(iemt.ValueType), prop.Name),
										iniv
										);
									if (ctc == null)
									{
										ctc = getStaticConstructor();
									}
									ctc.Statements.Add(cas);
								}
							}
						}
					}
				}
			}
		}
		private void adjustWebObjects()
		{
			if (_project.ProjectType == EnumProjectType.WebAppAspx)
			{
				_serverPageCode = new ServerDotNetCodeHolder();
				if (IsWebApp)
				{
					List<CodeTypeMember> tdels = new List<CodeTypeMember>();
					foreach (CodeTypeMember tm in td.Members)
					{
						CodeTypeConstructor tc = tm as CodeTypeConstructor;
						if (tc != null)
						{
							if (tc.Parameters.Count > 0)
							{
								tdels.Add(tm);
							}
						}
					}
					foreach (CodeTypeMember tm in tdels)
					{
						td.Members.Remove(tm);
					}
					CodeMemberMethod initMMWebApp = getInitializeMethod();
					if (initMMWebApp != null)
					{
						initMMWebApp.Attributes |= MemberAttributes.Static;
						initMMWebApp.Statements.Clear();
					}
				}
			}
		}
		private void removeIllProperties()
		{
			CodeMemberMethod initMM = getInitializeMethod();
			if (initMM != null)
			{
				List<CodeStatement> deletes = new List<CodeStatement>();
				foreach (CodeStatement st in initMM.Statements)
				{
					CodeAssignStatement cas = st as CodeAssignStatement;
					if (cas != null)
					{
					}
				}
				foreach (CodeStatement st in deletes)
				{
					initMM.Statements.Remove(st);
				}
			}
		}
		private void removeReadOnlyProperties()
		{
			CodeMemberMethod initMM = getInitializeMethod();
			if (initMM != null)
			{
				Dictionary<string, object> nameToType = new Dictionary<string, object>();
				foreach (object v in _objMap.Keys)
				{
					IComponent ic = v as IComponent;
					if (ic != null)
					{
						if (ic.Site != null && !string.IsNullOrEmpty(ic.Site.Name))
						{
							if (!nameToType.ContainsKey(ic.Site.Name))
							{
								nameToType.Add(ic.Site.Name, v);
							}
						}
					}
				}
				List<CodeStatement> deletes = new List<CodeStatement>();
				foreach (CodeStatement st in initMM.Statements)
				{
					CodeAssignStatement cas = st as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
						if (cpre != null)
						{
							CodeFieldReferenceExpression cfre = cpre.TargetObject as CodeFieldReferenceExpression;
							if (cfre != null)
							{
								if (cfre.TargetObject is CodeThisReferenceExpression)
								{
									object obj;
									if (nameToType.TryGetValue(cfre.FieldName, out obj))
									{
										bool bDeleted = false;
										PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(obj, false);
										foreach (PropertyDescriptor pd in pdc)
										{
											if (string.CompareOrdinal(pd.Name, cpre.PropertyName) == 0)
											{
												if (pd.Attributes != null)
												{
													foreach (Attribute a in pd.Attributes)
													{
														ReadOnlyInProgrammingAttribute ro = a as ReadOnlyInProgrammingAttribute;
														if (ro != null)
														{
															deletes.Add(st);
															bDeleted = true;
															break;
														}
													}
												}
												break;
											}
										}
										if (!bDeleted)
										{
											IXType ix = obj as IXType;
											if (ix != null)
											{
												if (!ix.TestWriteProperty(cpre.PropertyName))
												{
													deletes.Add(st);
												}
											}
										}
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement st in deletes)
				{
					initMM.Statements.Remove(st);
				}
			}
		}
		private void adjustIntPtrCast(CodeMemberMethod initMM)
		{
			foreach (CodeStatement st in initMM.Statements)
			{
				CodeAssignStatement cas = st as CodeAssignStatement;
				if (cas != null)
				{
					CodeCastExpression cce = cas.Right as CodeCastExpression;
					if (cce != null)
					{
						if (string.CompareOrdinal(cce.TargetType.BaseType, "System.IntPtr") == 0)
						{
							if (cce.TargetType.ArrayRank == 0)
							{
								CodePrimitiveExpression cpe = cce.Expression as CodePrimitiveExpression;
								if (cpe != null)
								{
									if (cpe.Value == null)
									{
										cas.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(IntPtr)), "Zero");
									}
									else
									{
										if (cpe.Value.GetType().Equals(typeof(IntPtr)))
										{
											IntPtr pt = (IntPtr)(cpe.Value);
											if (pt == IntPtr.Zero)
											{
												cas.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(IntPtr)), "Zero");
											}
											else
											{
												cas.Right = new CodeObjectCreateExpression(
													typeof(IntPtr), new CodePrimitiveExpression(pt.ToInt64())
													);
											}
										}
										else
										{
											Int64 n = Convert.ToInt64(cpe.Value);
											if (n == 0)
											{
												cas.Right = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(IntPtr)), "Zero");
											}
											else
											{
												cas.Right = new CodeObjectCreateExpression(
													typeof(IntPtr), new CodePrimitiveExpression(n)
													);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		private void adjustRuntimeExecuter(CodeMemberMethod initMM)
		{
			IRuntimeExecuter re = _rootObject as IRuntimeExecuter;
			if (re != null)
			{
				bool bUseRuntimeExecuter = false;
				foreach (object v in _objMap.Keys)
				{
					IRuntimeClient rc = v as IRuntimeClient;
					if (rc != null)
					{
						bUseRuntimeExecuter = true;
						CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
						cmie.Method = new CodeMethodReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), ((IComponent)v).Site.Name), "SetExecuter");
						cmie.Parameters.Add(new CodeThisReferenceExpression());
						initMM.Statements.Add(cmie);
					}
				}
				if (bUseRuntimeExecuter)
				{
					_useDrawing = true;
					_classCompiler.SetUseDrawing();
					Dictionary<UInt32, IAction> acts = _rootClassId.GetActions();
					if (acts.Count > 0)
					{
						CodeMemberMethod cmm = new CodeMemberMethod();
						cmm.Attributes = MemberAttributes.Family | MemberAttributes.Override;
						cmm.ReturnType = new CodeTypeReference(typeof(Dictionary<UInt32, string>));
						cmm.Name = "GetAllActions";
						td.Members.Add(cmm);
						string varDic = "acts";
						CodeVariableDeclarationStatement cvd = new CodeVariableDeclarationStatement();
						cvd.Name = varDic;
						cvd.Type = new CodeTypeReference(typeof(Dictionary<UInt32, string>));
						cvd.InitExpression = new CodeObjectCreateExpression(typeof(Dictionary<UInt32, string>));
						cmm.Statements.Add(cvd);
						//
						SortedDictionary<UInt32, IAction> sortActs = new SortedDictionary<uint, IAction>();
						foreach (KeyValuePair<UInt32, IAction> kv in acts)
						{
							if (!kv.Value.HideFromRuntimeDesigners)
							{
								CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
								cmie.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(varDic), "Add");
								cmie.Parameters.Add(new CodePrimitiveExpression(kv.Key));
								cmie.Parameters.Add(new CodePrimitiveExpression(kv.Value.ActionName));
								cmm.Statements.Add(cmie);
								sortActs.Add(kv.Key, kv.Value);
							}
						}
						cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(varDic)));
						cmm = new CodeMemberMethod();
						cmm.Attributes = MemberAttributes.Family | MemberAttributes.Override;
						cmm.ReturnType = new CodeTypeReference(typeof(void));
						cmm.Name = "OnExecuteaction";
						string pn = "actionId";
						cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(UInt32), pn));
						td.Members.Add(cmm);
						//
						MethodClass mc = new MethodClass(_rootClassId);
						mc.Name = cmm.Name;
						mc.MethodName = cmm.Name; ;
						mc.SetCompilerData(this);
						mc.MethodID = (UInt32)Guid.NewGuid().GetHashCode();
						mc.Parameters.Add(new ParameterClass(typeof(UInt32), pn, mc));
						mc.ModuleProject = _project;
						mc.MethodCode = cmm;
						CodeConditionStatement ccs = null;
						foreach (KeyValuePair<UInt32, IAction> kv in sortActs)
						{
							if (ccs == null)
							{
								ccs = new CodeConditionStatement();
								cmm.Statements.Add(ccs);
							}
							else
							{
								CodeConditionStatement ccs1 = new CodeConditionStatement();
								ccs.FalseStatements.Add(ccs1);
								ccs = ccs1;
							}
							ccs.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(pn), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(kv.Key));
							kv.Value.ExportCode(null, null, this, mc, cmm, ccs.TrueStatements, Debug);
						}
					}
				}
			}
		}
		private void adjustListView(ListView lv, CodeMemberMethod initMM)
		{
			int n = -1;
			bool processed = false;
			for (int i = 0; i < initMM.Statements.Count; i++)
			{
				CodeExpressionStatement ces = initMM.Statements[i] as CodeExpressionStatement;
				if (ces != null)
				{
					CodeMethodInvokeExpression cmi = ces.Expression as CodeMethodInvokeExpression;
					if (cmi != null)
					{
						if (cmi.Method != null)
						{
							if (string.CompareOrdinal("Add", cmi.Method.MethodName) == 0)
							{
								CodePropertyReferenceExpression pre = cmi.Method.TargetObject as CodePropertyReferenceExpression;
								if (pre != null)
								{
									if (string.CompareOrdinal("Columns", pre.PropertyName) == 0)
									{
										CodeFieldReferenceExpression fre = pre.TargetObject as CodeFieldReferenceExpression;
										if (fre != null)
										{
											if (string.CompareOrdinal(fre.FieldName, lv.Name) == 0 && fre.TargetObject is CodeThisReferenceExpression)
											{
												processed = true;
												break;
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					CodeAssignStatement cas = initMM.Statements[i] as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression pre = cas.Left as CodePropertyReferenceExpression;
						if (pre != null)
						{
							CodeFieldReferenceExpression fre = pre.TargetObject as CodeFieldReferenceExpression;
							if (fre != null)
							{
								if (fre.TargetObject is CodeThisReferenceExpression)
								{
									if (string.CompareOrdinal(fre.FieldName, lv.Name) == 0)
									{
										if (i > n)
										{
											n = i;
										}
										if (string.CompareOrdinal(pre.PropertyName, "Name") == 0)
										{
											n = i;
											break;
										}
									}
								}
							}
						}
						else
						{
							if (n < 0)
							{
								CodeFieldReferenceExpression fre = cas.Left as CodeFieldReferenceExpression;
								if (fre != null)
								{
									if (fre.TargetObject is CodeThisReferenceExpression)
									{
										if (string.CompareOrdinal(fre.FieldName, lv.Name) == 0)
										{
											n = i;
										}
									}
								}
							}
						}
					}
				}
			}
			if (!processed)
			{
				if (n < 0)
					n = initMM.Statements.Count;
				else
					n++;
				for (int i = 0; i < lv.Columns.Count; i++)
				{
					CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
					cmi.Method = new CodeMethodReferenceExpression();
					cmi.Method.TargetObject = new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), lv.Name), "Columns");
					cmi.Method.MethodName = "Add";
					CodeObjectCreateExpression oce = new CodeObjectCreateExpression(typeof(ColumnHeader));
					string vn = string.Format(CultureInfo.InvariantCulture, "col{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					CodeVariableDeclarationStatement vd = new CodeVariableDeclarationStatement(typeof(ColumnHeader), vn, oce);
					initMM.Statements.Insert(n++, vd);
					if (lv.Columns[i].ImageIndex >= 0)
					{
						CodePropertyReferenceExpression cpre0 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "ImageIndex");
						CodeAssignStatement cas0 = new CodeAssignStatement(cpre0, new CodePrimitiveExpression(lv.Columns[i].ImageIndex));
						initMM.Statements.Insert(n++, cas0);
					}
					if (!string.IsNullOrEmpty(lv.Columns[i].ImageKey))
					{
						CodePropertyReferenceExpression cpre0 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "ImageKey");
						CodeAssignStatement cas0 = new CodeAssignStatement(cpre0, new CodePrimitiveExpression(lv.Columns[i].ImageKey));
						initMM.Statements.Insert(n++, cas0);
					}
					if (!string.IsNullOrEmpty(lv.Columns[i].Name))
					{
						CodePropertyReferenceExpression cpre0 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "Name");
						CodeAssignStatement cas0 = new CodeAssignStatement(cpre0, new CodePrimitiveExpression(lv.Columns[i].Name));
						initMM.Statements.Insert(n++, cas0);
					}
					if (lv.Columns[i].Tag != null)
					{
						CodePropertyReferenceExpression cpre0 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "Tag");
						CodeAssignStatement cas0 = new CodeAssignStatement(cpre0, new CodePrimitiveExpression(lv.Columns[i].Tag));
						initMM.Statements.Insert(n++, cas0);
					}
					CodePropertyReferenceExpression cpre1 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "TextAlign");
					CodeAssignStatement cas1 = new CodeAssignStatement(cpre1, new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(HorizontalAlignment)), lv.Columns[i].TextAlign.ToString()));
					initMM.Statements.Insert(n++, cas1);
					cpre1 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "Width");
					cas1 = new CodeAssignStatement(cpre1, new CodePrimitiveExpression(lv.Columns[i].Width));
					initMM.Statements.Insert(n++, cas1);

					cpre1 = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(vn), "Text");
					cas1 = new CodeAssignStatement(cpre1, new CodePrimitiveExpression(lv.Columns[i].Text));
					initMM.Statements.Insert(n++, cas1);

					cmi.Parameters.Add(new CodeVariableReferenceExpression(vn));
					initMM.Statements.Insert(n++, new CodeExpressionStatement(cmi));
				}
			}
		}
		private void adjustConsoleMemberCreation(CodeMemberMethod initMM)
		{
			if (_isConsole)
			{
			}
		}
		private void adjustDrawingRepeater(CodeMemberMethod initMM)
		{
			if (_isForm)
			{
				foreach (KeyValuePair<object, UInt32> kv in _objMap)
				{
					Draw2DDataRepeater dr = kv.Key as Draw2DDataRepeater;
					if (dr != null)
					{
						CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
						cmie.Method = new CodeMethodReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dr.Name), "SetupItemDataBind");
						initMM.Statements.Add(new CodeExpressionStatement(cmie));
					}
				}
			}
		}
		private void adjustHiddenForm(CodeMemberMethod initMM)
		{
			if (_isForm)
			{
				CodeExpressionStatement ces = new CodeExpressionStatement();
				CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
				ces.Expression = cmie;
				cmie.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "OnComponentsInitialized");
				initMM.Statements.Add(ces);
			}
		}
		private bool propertyHandledInInit(CodeMemberMethod initMM, string controlName, string propertyName)
		{
			foreach (CodeStatement st in initMM.Statements)
			{
				CodeAssignStatement cas = st as CodeAssignStatement;
				if (cas != null)
				{
					CodeFieldReferenceExpression cfre = cas.Left as CodeFieldReferenceExpression;
					if (cfre != null)
					{
						if (string.CompareOrdinal(propertyName, cfre.FieldName) == 0)
						{
							CodeFieldReferenceExpression ocfre = cfre.TargetObject as CodeFieldReferenceExpression;
							if (ocfre != null)
							{
								if (string.CompareOrdinal(controlName, ocfre.FieldName) == 0)
								{
									return true;
								}
							}
							else
							{
								CodePropertyReferenceExpression ocpre = cfre.TargetObject as CodePropertyReferenceExpression;
								if (ocpre != null)
								{
									if (string.CompareOrdinal(controlName, ocpre.PropertyName) == 0)
									{
										return true;
									}
								}
							}
						}
					}
					else
					{
						CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
						if (cpre != null)
						{
							if (string.CompareOrdinal(propertyName, cpre.PropertyName) == 0)
							{
								CodeFieldReferenceExpression ocfre = cpre.TargetObject as CodeFieldReferenceExpression;
								if (ocfre != null)
								{
									if (string.CompareOrdinal(controlName, ocfre.FieldName) == 0)
									{
										return true;
									}
								}
								else
								{
									CodePropertyReferenceExpression ocpre = cpre.TargetObject as CodePropertyReferenceExpression;
									if (ocpre != null)
									{
										if (string.CompareOrdinal(controlName, ocpre.PropertyName) == 0)
										{
											return true;
										}
									}
								}
							}
						}
					}
				}
				else
				{
					CodeExpressionStatement ces = st as CodeExpressionStatement;
					if (ces != null)
					{
						CodeMethodInvokeExpression cmie=ces.Expression as CodeMethodInvokeExpression;
						if (cmie != null)
						{
							CodePropertyReferenceExpression cpre = cmie.Method.TargetObject as CodePropertyReferenceExpression;
							if (cpre != null)
							{
								if (string.CompareOrdinal(propertyName, cpre.PropertyName) == 0)
								{
									CodeFieldReferenceExpression ocfre = cpre.TargetObject as CodeFieldReferenceExpression;
									if (ocfre != null)
									{
										if (string.CompareOrdinal(controlName, ocfre.FieldName) == 0)
										{
											return true;
										}
									}
									else
									{
										CodePropertyReferenceExpression ocpre = cpre.TargetObject as CodePropertyReferenceExpression;
										if (ocpre != null)
										{
											if (string.CompareOrdinal(controlName, ocpre.PropertyName) == 0)
											{
												return true;
											}
										}
									}
								}
							}
							else
							{
								CodeFieldReferenceExpression cfre = cmie.Method.TargetObject as CodeFieldReferenceExpression;
								if (cfre != null)
								{
									if (string.CompareOrdinal(propertyName, cfre.FieldName) == 0)
									{
										CodeFieldReferenceExpression ocfre = cfre.TargetObject as CodeFieldReferenceExpression;
										if (ocfre != null)
										{
											if (string.CompareOrdinal(controlName, ocfre.FieldName) == 0)
											{
												return true;
											}
										}
										else
										{
											CodePropertyReferenceExpression opre = cfre.TargetObject as CodePropertyReferenceExpression;
											if (opre != null)
											{
												if (string.CompareOrdinal(controlName, opre.PropertyName) == 0)
												{
													return true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return false;
		}
		private bool isAssignmentForControlName(CodeAssignStatement cas, string controlName)
		{
			CodeFieldReferenceExpression cfre = cas.Left as CodeFieldReferenceExpression;
			if (cfre != null)
			{
				CodeFieldReferenceExpression ocfre = cfre.TargetObject as CodeFieldReferenceExpression;
				if (ocfre != null)
				{
					if (string.CompareOrdinal(ocfre.FieldName, controlName) == 0)
					{
						return true;
					}
				}
				else
				{
					CodePropertyReferenceExpression ocpre = cfre.TargetObject as CodePropertyReferenceExpression;
					if (ocpre != null)
					{
						if (string.CompareOrdinal(ocpre.PropertyName, controlName) == 0)
						{
							return true;
						}
					}
				}
			}
			else
			{
				CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
				if (cpre != null)
				{
					CodeFieldReferenceExpression ocfre = cpre.TargetObject as CodeFieldReferenceExpression;
					if (ocfre != null)
					{
						if (string.CompareOrdinal(ocfre.FieldName, controlName) == 0)
						{
							return true;
						}
					}
					else
					{
						CodePropertyReferenceExpression ocpre = cpre.TargetObject as CodePropertyReferenceExpression;
						if (ocpre != null)
						{
							if (string.CompareOrdinal(ocpre.PropertyName, controlName) == 0)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		private int locateInitCodeStatementEnd(CodeMemberMethod initMM, string controlName)
		{
			bool foundControl = false;
			int loc = initMM.Statements.Count;
			for (int i = 0; i < initMM.Statements.Count; i++)
			{
				CodeAssignStatement cas = initMM.Statements[i] as CodeAssignStatement;
				if (cas != null)
				{
					if (isAssignmentForControlName(cas, controlName))
					{
						foundControl = true;
						loc = i + 1;
					}
					else
					{
						if (foundControl)
							break;
					}
				}
				else
				{
					if (foundControl)
						break;
				}
			}
			return loc;
		}
		private void adjustContainerControls(CodeMemberMethod initMM)
		{
			XmlNode nodeControls = doc.DocumentElement.SelectSingleNode("Property[@name='Controls']");
			if (nodeControls != null)
			{
				XmlNodeList controlNodes = nodeControls.SelectNodes("Item");
				foreach (XmlNode nodeC in controlNodes)
				{
					XmlNodeList propertyNodes = nodeC.SelectNodes("Property");
					foreach (XmlNode nodeP in propertyNodes)
					{
						XmlNodeList itemNodes = nodeP.SelectNodes("Item");
						if (itemNodes.Count > 0)
						{
							string propertyName = XmlUtil.GetNameAttribute(nodeP);
							if (string.CompareOrdinal("Controls", propertyName) != 0)
							{
								string controlName = XmlUtil.GetNameAttribute(nodeC);
								Control control = getControlByName(controlName);
								if (control != null)
								{
									PropertyInfo pif = control.GetType().GetProperty(propertyName);
									if (pif != null)
									{
										bool hasAddMethod = false;
										MethodInfo[] mifs = pif.PropertyType.GetMethods();
										if (mifs != null)
										{
											for (int i = 0; i < mifs.Length; i++)
											{
												if (string.CompareOrdinal(mifs[i].Name, "Add") == 0)
												{
													hasAddMethod = true;
													break;
												}
											}
										}
										if (hasAddMethod)
										{
											if (!propertyHandledInInit(initMM, controlName, propertyName))
											{
												int loc = locateInitCodeStatementEnd(initMM, controlName);
												foreach (XmlNode nodeItem in itemNodes)
												{
													string typestring = XmlUtil.GetAttribute(nodeItem, "type");
													if (!string.IsNullOrEmpty(typestring))
													{
														Type itemType = XmlUtil.GetLibTypeAttribute(nodeItem);
														string itemName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", typestring, Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
														CodeVariableDeclarationStatement cvds = new CodeVariableDeclarationStatement();
														cvds.Name = itemName;
														if (itemType != null)
															cvds.Type = new CodeTypeReference(itemType);
														else
															cvds.Type = new CodeTypeReference(typestring);
														CodeObjectCreateExpression coce = new CodeObjectCreateExpression();
														if (itemType != null)
															coce.CreateType = new CodeTypeReference(itemType);
														else
															coce.CreateType = new CodeTypeReference(typestring);
														cvds.InitExpression = coce;
														initMM.Statements.Insert(loc++, new CodeCommentStatement(""));
														initMM.Statements.Insert(loc++, new CodeCommentStatement(itemName));
														initMM.Statements.Insert(loc++, new CodeCommentStatement(""));
														initMM.Statements.Insert(loc++, cvds);
														XmlNodeList itempropnodes = nodeItem.SelectNodes("Property");
														foreach (XmlNode nodeprop in itempropnodes)
														{
															CodeAssignStatement cas = new CodeAssignStatement();
															CodePropertyReferenceExpression cpre = new CodePropertyReferenceExpression();
															cpre.PropertyName = XmlUtil.GetAttribute(nodeprop, "name");
															CodeVariableReferenceExpression cvre = new CodeVariableReferenceExpression();
															cvre.VariableName = itemName;
															cpre.TargetObject = cvre;
															//CodeFieldReferenceExpression cfre = new CodeFieldReferenceExpression();
															//cfre.FieldName = controlName;
															//cfre.TargetObject = new CodeThisReferenceExpression();
															//cpre.TargetObject = cfre;
															cas.Left = cpre;
															List<XmlElement> propElements = new List<XmlElement>();
															foreach (XmlNode nd in nodeprop.ChildNodes)
															{
																XmlElement xe = nd as XmlElement;
																if (xe != null)
																{
																	propElements.Add(xe);
																}
															}
															if (propElements.Count == 0)
															{
																cas.Right = new CodePrimitiveExpression(nodeprop.InnerText);
																initMM.Statements.Insert(loc++, cas);
															}
															else if (propElements.Count == 1)
															{
																if (string.CompareOrdinal(propElements[0].Name, "Reference") == 0)
																{
																	CodeFieldReferenceExpression vcfre = new CodeFieldReferenceExpression();
																	vcfre.FieldName = XmlUtil.GetAttribute(propElements[0], "name");
																	vcfre.TargetObject = new CodeThisReferenceExpression();
																	cas.Right = vcfre;
																	initMM.Statements.Insert(loc++, cas);
																}
															}
														}
														CodeExpressionStatement ces = new CodeExpressionStatement();
														CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
														ces.Expression = cmie;
														CodeMethodReferenceExpression cmre = new CodeMethodReferenceExpression();
														cmie.Method = cmre;
														cmre.MethodName = "Add";
														CodePropertyReferenceExpression cpreV = new CodePropertyReferenceExpression();
														cmre.TargetObject = cpreV;
														cpreV.PropertyName = propertyName;
														CodeFieldReferenceExpression cfreV = new CodeFieldReferenceExpression();
														cpreV.TargetObject = cfreV;
														cfreV.FieldName = controlName;
														cfreV.TargetObject = new CodeThisReferenceExpression();
														CodeVariableReferenceExpression cvreV = new CodeVariableReferenceExpression();
														cvreV.VariableName = itemName;
														cmie.Parameters.Add(cvreV);
														initMM.Statements.Insert(loc++, ces);
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		private void adjustComponentInits(CodeMemberMethod initMM)
		{
			if (_isForm)
			{
				//==processing ListView==============
				StringCollection scListViews = new StringCollection();
				foreach (KeyValuePair<object, UInt32> kv in _objMap)
				{
					ListView lv = kv.Key as ListView;
					if (lv != null && lv.Columns.Count>0)
					{
						adjustListView(lv, initMM);
						scListViews.Add(lv.Name);
					}
				}
				
				//==end of processing ListView=======
				int nStart = -1;
				int nEnd = -1;
				for (int i = 0; i < initMM.Statements.Count; i++)
				{
					CodeExpressionStatement ces = initMM.Statements[i] as CodeExpressionStatement;
					if (ces != null)
					{
						CodeMethodInvokeExpression cmi = ces.Expression as CodeMethodInvokeExpression;
						if (cmi != null)
						{
							if (cmi.Method != null)
							{
								if (string.CompareOrdinal("SuspendLayout", cmi.Method.MethodName) == 0)
								{
									if (cmi.Method.TargetObject is CodeThisReferenceExpression)
									{
										nStart = i;
									}
								}
								else if (string.CompareOrdinal("ResumeLayout", cmi.Method.MethodName) == 0)
								{
									if (cmi.Method.TargetObject is CodeThisReferenceExpression)
									{
										nEnd = i;
									}
								}
							}
						}
					}
				}
				if (nStart < 0) nStart = 0;
				if (nEnd < 0) nEnd = initMM.Statements.Count;
				Dictionary<object, MethodInfo> formUsers = new Dictionary<object, MethodInfo>();
				List<ISupportInitialize> iniObjs = new List<ISupportInitialize>();
				foreach (KeyValuePair<object, UInt32> kv in _objMap)
				{
					ISupportInitialize ini = kv.Key as ISupportInitialize;
					if (ini != null)
					{
						iniObjs.Add(ini);
					}
					MethodInfo mi = kv.Key.GetType().GetMethod("SetOwnerForm", new Type[] { typeof(Form) });
					if (mi != null)
					{
						formUsers.Add(kv.Key, mi);
					}
				}
				
				if (iniObjs.Count > 0)
				{
					foreach (ISupportInitialize ini in iniObjs)
					{
						IComponent ic = ini as IComponent;
						if (ic != null && ic.Site != null)
						{
							CodeMethodInvokeExpression cStart = new CodeMethodInvokeExpression(
								new CodeCastExpression(typeof(ISupportInitialize), new CodeVariableReferenceExpression(ic.Site.Name)), "BeginInit");
							initMM.Statements.Insert(nStart, new CodeExpressionStatement(cStart));
							nEnd++;
							CodeMethodInvokeExpression cEnd = new CodeMethodInvokeExpression(
								new CodeCastExpression(typeof(ISupportInitialize), new CodeVariableReferenceExpression(ic.Site.Name)), "EndInit");
							initMM.Statements.Insert(nEnd, new CodeExpressionStatement(cEnd));
						}
					}
				}
				if (formUsers.Count > 0)
				{
					foreach (KeyValuePair<object, MethodInfo> kv in formUsers)
					{
						CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
						cmie.Method = new CodeMethodReferenceExpression();
						cmie.Method.MethodName = "SetOwnerForm";
						cmie.Method.TargetObject = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), ((IComponent)kv.Key).Site.Name);
						cmie.Parameters.Add(new CodeThisReferenceExpression());
						initMM.Statements.Add(cmie);
					}
				}
			}
		}
		private void adjustComponentCreations(CodeMemberMethod initMM)
		{
			bool bComponentUsed = false;
			foreach (CodeTypeMember ctm in td.Members)
			{
				CodeMemberField cmf = ctm as CodeMemberField;
				if (cmf != null)
				{
					if (string.CompareOrdinal(cmf.Name, Var_Components) == 0)
					{
						bComponentUsed = true;
						break;
					}
				}
			}
			if (bComponentUsed)
			{
				CodeStatement cc = null;
				foreach (CodeStatement cs in initMM.Statements)
				{
					CodeAssignStatement cas = cs as CodeAssignStatement;
					if (cas != null)
					{
						if (cas.Right is CodeObjectCreateExpression)
						{
							CodeFieldReferenceExpression cfre = cas.Left as CodeFieldReferenceExpression;
							if (cfre != null)
							{
								if (cfre.TargetObject is CodeThisReferenceExpression)
								{
									if (string.CompareOrdinal(cfre.FieldName, Var_Components) == 0)
									{
										cc = cs;
										break;
									}
								}
							}
							else
							{
								CodeVariableReferenceExpression cvre = cas.Left as CodeVariableReferenceExpression;
								if (cvre != null)
								{
									if (string.CompareOrdinal(cvre.VariableName, Var_Components) == 0)
									{
										cc = cs;
										break;
									}
								}
							}
						}
					}
				}
				if (cc != null)
				{
					initMM.Statements.Remove(cc);
					initMM.Statements.Insert(0, cc);
				}
				else
				{
					CodeAssignStatement cas = new CodeAssignStatement();
					cas.Left = new CodeVariableReferenceExpression(Var_Components);
					cas.Right = new CodeObjectCreateExpression(typeof(Container), new CodeExpression[] { });
					initMM.Statements.Insert(0, cas);
				}
			}
		}
		private void addCurrentCulture(CodeMemberMethod initMM)
		{
			ProjectResources rm = _project.GetProjectSingleData<ProjectResources>();
			if (rm.HasResources)
			{
				IList<string> languages = rm.Languages;
				if (languages.Count > 0)
				{
					CodeAssignStatement cas = new CodeAssignStatement();
					cas.Left = new CodePropertyReferenceExpression(
						new CodeTypeReferenceExpression(rm.ClassName),
						"Culture"
						);
					cas.Right = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(CultureInfo)), "CurrentCulture");
					initMM.Statements.Insert(0, cas);
				}
			}
		}
		private static void removeThisInExpression(CodeExpression c)
		{
			CodePropertyReferenceExpression cpre = c as CodePropertyReferenceExpression;
			if (cpre != null)
			{
				if (cpre.TargetObject != null)
				{
					if (cpre.TargetObject is CodeThisReferenceExpression)
					{
						cpre.TargetObject = null;
					}
					else
					{
						removeThisInExpression(cpre.TargetObject);
					}
				}
			}
			else
			{
				CodeFieldReferenceExpression cfre = c as CodeFieldReferenceExpression;
				if (cfre != null && cfre.TargetObject != null)
				{
					if (cfre.TargetObject is CodeThisReferenceExpression)
					{
						cfre.TargetObject = null;
					}
					else
					{
						removeThisInExpression(cfre.TargetObject);
					}
				}
			}
		}
		private bool isThisMember(CodeExpression c)
		{
			CodeFieldReferenceExpression cfr = c as CodeFieldReferenceExpression;
			if (cfr != null)
			{
				if (cfr.TargetObject != null)
				{
					if (cfr.TargetObject is CodeThisReferenceExpression)
					{
						return true;
					}
					else if (isThisMember(cfr.TargetObject))
					{
						return true;
					}
				}
			}
			else
			{
				CodePropertyReferenceExpression cpr = c as CodePropertyReferenceExpression;
				if (cpr != null)
				{
					if (cpr.TargetObject != null)
					{
						if (cpr.TargetObject is CodeThisReferenceExpression)
						{
							return true;
						}
						else if (isThisMember(cpr.TargetObject))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		private void adjustStaticClass(CodeMemberMethod initMM)
		{
			if (isStaticClass)
			{
				//Remove "this" from references, not to delete the statements
				foreach (CodeStatement cs in initMM.Statements)
				{
					CodeAssignStatement cas = cs as CodeAssignStatement;
					if (cas != null)
					{
						CodeObjectCreateExpression coce = cas.Right as CodeObjectCreateExpression;
						if (coce != null && coce.Parameters != null)
						{
							for (int i = 0; i < coce.Parameters.Count; i++)
							{
								CodeFieldReferenceExpression cfre = coce.Parameters[i] as CodeFieldReferenceExpression;
								if (cfre != null)
								{
									CodeThisReferenceExpression ctre = cfre.TargetObject as CodeThisReferenceExpression;
									if (ctre != null)
									{
										coce.Parameters[i] = new CodeVariableReferenceExpression(cfre.FieldName);
									}
								}
							}
						}
						removeThisInExpression(cas.Left);
					}
				}
			}
		}
		private void adjustSpecialInterfaces(CodeMemberMethod initMM)
		{
			foreach (object v in _objMap.Keys)
			{
				IComponentReferenceHolder crh = v as IComponentReferenceHolder;
				if (crh != null)
				{
					processComponentReferenceHolder(crh, initMM);
				}
				else
				{
					ISerializeNotify sn = v as ISerializeNotify;
					if (sn != null)
					{
						processSerializeNotify(sn, initMM);
					}
				}
			}
		}
		private void adjustPropertyValueLinks(CodeMemberMethod initMM)
		{
			//locate the IPropertyValueLinkHolder objects and then generate code for each IPropertyValueLink
			//via IPropertyValueLinkOwner
			//call IPropertyValueLinkHolder.SetPropertyGetter
			//   IPropertyValueLink may be const, property ref, or math exp
			//
			//find the CodeExpression for each IPropertyValueLinkHolder
			//1. root object is IPropertyValueLinkHolder
			//  CodeThisExpression
			//2. a component is IPropertyValueLinkHolder
			//  CodeFieldReferenceExpression/CodePropertyReferenceExpression
			//3. a property of the root/component is IPropertyValueLinkHolder, the root/component is IPropertyValueLinkOwner
			//  CodePropertyReferenceExpression
			//4. a deeper level property is IPropertyValueLinkHolder
			//
			//Code generations are inserted at the end of initMM so that they will not be overwritten by automatic code generations
			//Properties of type IPropertyValueLink can only be used at runtime?
			//call any CodeAssignment again or move to end if Right is an IPropertyValueLink?
			List<CodeStatement> sts = new List<CodeStatement>();
			foreach (object v in _objMap.Keys)
			{
				IPropertyValueLinkOwner pvo = v as IPropertyValueLinkOwner;
				if (pvo != null)
				{
					//this component uses property links, find out the holders and the properties and the linking code expression
					Dictionary<IPropertyValueLinkHolder, Dictionary<IPropertyValueLink, CodeExpression>> pls = pvo.GetPropertyValueLinks();
					foreach (KeyValuePair<IPropertyValueLinkHolder, Dictionary<IPropertyValueLink, CodeExpression>> kv1 in pls)
					{
						//process each property
						foreach (KeyValuePair<IPropertyValueLink, CodeExpression> kv2 in kv1.Value)
						{
							addRef(typeof(fnGetPropertyValue));//add reference to VPL.DLL
							MethodClass mc = new MethodClass(_rootClassId);
							mc.MemberId = (UInt32)(Guid.NewGuid().GetHashCode());
							mc.MethodName = string.Format(CultureInfo.InvariantCulture, "m{0}", mc.MethodID.ToString("x", CultureInfo.InvariantCulture));
							CodeMemberMethod mm = createCodeDomMethod(mc, false, false);

							CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
							cmi.Method.MethodName = "SetPropertyGetter";
							//adjust the root target to the component
							CodeFieldReferenceExpression rootTarget = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), ((IComponent)v).Site.Name);
							CodeExpression holderExpression = kv2.Value;
							while (holderExpression != null)
							{
								CodePropertyReferenceExpression pre = holderExpression as CodePropertyReferenceExpression;
								if (pre != null)
								{
									if (pre.TargetObject == null)
									{
										pre.TargetObject = rootTarget;
										break;
									}
									else
									{
										holderExpression = pre.TargetObject;
									}
								}
								else
								{
									CodeMethodInvokeExpression mie = holderExpression as CodeMethodInvokeExpression;
									if (mie != null)
									{
										if (mie.Method.TargetObject == null)
										{
											mie.Method.TargetObject = rootTarget;
											break;
										}
										else
										{
											holderExpression = mie.Method.TargetObject;
										}
									}
									else
									{
										throw new DesignerException("Unsupported property link type: {0}", holderExpression.GetType().AssemblyQualifiedName);
									}
								}
							}
							cmi.Method.TargetObject = kv2.Value;
							//property name
							cmi.Parameters.Add(new CodePrimitiveExpression(kv2.Value.UserData["name"]));
							//declare delegate variable =====================
							CodeVariableDeclarationStatement vds = new CodeVariableDeclarationStatement();
							vds.Name = string.Format(CultureInfo.InvariantCulture, "v{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
							vds.Type = new CodeTypeReference(typeof(fnGetPropertyValue));
							//get the code from design time linking
							PropertyValue pv = (PropertyValue)kv2.Key;
							CodeExpression pvCode = pv.GetReferenceCode(mc, mm.Statements, true);
							CodeMethodReturnStatement mr = new CodeMethodReturnStatement(pvCode);
							//
							initMM.Statements.Add(vds);
							//===============================================
							//assign the delegate to the variable
							initMM.Statements.Add(new CodeSnippetStatement(string.Format(CultureInfo.InvariantCulture, "{0} = (fnGetPropertyValue)(delegate() {{", vds.Name)));
							foreach (CodeStatement st in mm.Statements)
							{
								initMM.Statements.Add(st);
							}
							//return from the delegate, which is from the design time linking
							initMM.Statements.Add(mr);
							initMM.Statements.Add(new CodeSnippetStatement("\r\n});"));
							//execute SetPropertyGetter
							cmi.Parameters.Add(new CodeVariableReferenceExpression(vds.Name));
							initMM.Statements.Add(cmi);
						}
					}
				}
			}
			foreach (CodeStatement s in sts)
			{
				initMM.Statements.Add(s);
			}
		}
		private void adjustInitFunction()
		{
			CodeMemberMethod initMM = getInitializeMethod();
			if (initMM != null)
			{
				adjustIntPtrCast(initMM);
				//
				adjustSpecialInterfaces(initMM);
				//
				adjustRuntimeExecuter(initMM);
				//
				adjustComponentCreations(initMM);
				//
				adjustComponentInits(initMM);
				//
				addCurrentCulture(initMM);
				//
				adjustStaticClass(initMM);
				//
				adjustPropertyValueLinks(initMM);
				//
				adjustDrawingRepeater(initMM);
				//
				adjustConsoleMemberCreation(initMM);
			}
		}
		
		private void adjustFormClass()
		{
			//if components is used then add a Dispose method
			bool componentsUsed = false;
			foreach (CodeTypeMember ctm in td.Members)
			{
				if (string.CompareOrdinal(Var_Components, ctm.Name) == 0)
				{
					componentsUsed = true;
					break;
				}
			}
			if (componentsUsed)
			{
				CodeMemberMethod mmDispose = new CodeMemberMethod();
				td.Members.Add(mmDispose);
				mmDispose.Attributes = MemberAttributes.Family | MemberAttributes.Override;
				mmDispose.Name = "Dispose";
				mmDispose.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "disposing"));
				mmDispose.ReturnType = new CodeTypeReference(typeof(void));
				CodeConditionStatement ccs = new CodeConditionStatement();
				mmDispose.Statements.Add(ccs);
				ccs.TrueStatements.Add(
					new CodeExpressionStatement(
					new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(Var_Components), "Dispose", new CodeExpression[] { })
					)
					);
				CodeBinaryOperatorExpression cboe = new CodeBinaryOperatorExpression();
				cboe.Left = new CodeVariableReferenceExpression("disposing");
				cboe.Operator = CodeBinaryOperatorType.BooleanAnd;
				CodeBinaryOperatorExpression cboe2 = new CodeBinaryOperatorExpression();
				cboe.Right = cboe2;
				cboe2.Left = new CodeVariableReferenceExpression(Var_Components);
				cboe2.Operator = CodeBinaryOperatorType.IdentityInequality;
				cboe2.Right = new CodePrimitiveExpression(null);
				ccs.Condition = cboe;
				CodeExpressionStatement ces = new CodeExpressionStatement();
				CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
				ces.Expression = cmie;
				cmie.Method = new CodeMethodReferenceExpression(new CodeBaseReferenceExpression(), "Dispose");
				cmie.Parameters.Add(new CodeVariableReferenceExpression("disposing"));
				mmDispose.Statements.Add(ces);
			}
			if (typeof(DrawingPage).IsAssignableFrom(_objectType))
			{
				CodeMemberMethod initMM = getInitializeMethod();
				if (initMM != null)
				{
					adjustHiddenForm(initMM);
					adjustContainerControls(initMM);
				}
			}
		}
		private void addComponents()
		{
			IComponent ic = VPLUtil.GetObject(_rootObject) as IComponent;
			if (ic != null)
			{
				bool bExists = false;
				List<ConstructorClass> constructorList = _rootClassId.GetConstructors();
				if (constructorList.Count > 0)
				{
					foreach (ConstructorClass c in constructorList)
					{
						if (c.ParameterCount == 1)
						{
							if (typeof(IContainer).IsAssignableFrom(c.Parameters[0].BaseClassType))
							{
								bExists = true;
								break;
							}
						}
					}
				}
				if (!bExists)
				{
					CodeConstructor cc = new CodeConstructor();
					cc.Attributes = MemberAttributes.Public;
					cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IContainer), "container"));
					CodeExpressionStatement ces = new CodeExpressionStatement();
					CodeMethodInvokeExpression cmik = new CodeMethodInvokeExpression();
					cmik.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("container"), "Add");
					cmik.Parameters.Add(new CodeThisReferenceExpression());
					ces.Expression = cmik;
					//
					CodeConditionStatement ccs = new CodeConditionStatement();
					ccs.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("container"), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
					ccs.TrueStatements.Add(ces);
					cc.Statements.Add(ccs);
					//
					CodeMemberMethod init = getInitializeMethod();
					if (init != null)
					{
						cc.Statements.Add(new CodeExpressionStatement(
							new CodeMethodInvokeExpression(_rootClassId.GetReferenceCode(null, cc.Statements, true), init.Name)));
					}
					td.Members.Add(cc);
				}
			}
		}
		private void adjustEntryPoint()
		{
			List<CodeStatement> deletes = new List<CodeStatement>();
			PropertyInfo[] pifs = _rootClassId.ObjectType.GetProperties();
			StringCollection names = new StringCollection();
			if (pifs != null)
			{
				for (int i = 0; i < pifs.Length; i++)
				{
					if (NotForProgrammingAttribute.IsNotForProgramming(pifs[i]))
					{
						names.Add(pifs[i].Name);
					}
				}
			}
			CodeMemberMethod inim = getInitializeMethod();
			if (inim != null)
			{
				foreach (CodeStatement cs in inim.Statements)
				{
					CodeAssignStatement cas = cs as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
						if (cpre != null)
						{
							if (names.Contains(cpre.PropertyName))
							{
								deletes.Add(cs);
							}
							else
							{
								if (cpre.TargetObject is CodeThisReferenceExpression)
								{
									cpre.TargetObject = new CodeTypeReferenceExpression(_rootClassId.TypeString);
								}
							}
						}
						else
						{
							CodeFieldReferenceExpression cfre = cas.Left as CodeFieldReferenceExpression;
							if (cfre != null)
							{
								if (names.Contains(cfre.FieldName))
								{
									deletes.Add(cs);
								}
								else
								{
									if (cfre.TargetObject is CodeThisReferenceExpression)
									{
										cfre.TargetObject = new CodeTypeReferenceExpression(_rootClassId.TypeString);
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement cs in deletes)
				{
					inim.Statements.Remove(cs);
				}
			}
		}
		private ClassInstancePointer getClassInstance(string name)
		{
			foreach (IComponent ic in designSurface.ComponentContainer.Components)
			{
				ClassInstancePointer cr = ic as ClassInstancePointer;
				if (cr != null && cr.Name == name)
				{
					return cr;
				}
				cr = this._objMap.GetClassRefByObject(ic) as ClassInstancePointer;
				if (cr != null && cr.Name == name)
				{
					return cr;
				}
			}
			return null;
		}
		private void adjustClassInstanceComponents(bool addProperties)
		{
			//adjust the type of the variables ==============================
			Dictionary<string, ClassInstancePointer> formInstances = new Dictionary<string, ClassInstancePointer>();
			foreach (KeyValuePair<object, UInt32> kv in _objMap)
			{
				IXType x = kv.Key as IXType;
				if (x != null)
				{
					if (typeof(Form).IsAssignableFrom(x.ValueType))
					{
						string nm = ((IComponent)(kv.Key)).Site.Name;
						ClassInstancePointer cr = _objMap.GetClassRefByObject(kv.Key) as ClassInstancePointer;
						if (cr != null)
						{
							formInstances.Add(nm, cr);
						}
					}
				}
				else
				{
					if (kv.Key is Control)
					{
						string nm = ((IComponent)(kv.Key)).Site.Name;
						ClassInstancePointer cr = _objMap.GetClassRefByObject(kv.Key) as ClassInstancePointer;
						if (cr != null)
						{
							formInstances.Add(nm, cr);
						}
					}
				}
			}
			List<ClassMemberField> crFields = new List<ClassMemberField>();
			foreach (CodeTypeMember m in td.Members)
			{
				CodeMemberField mf = m as CodeMemberField;
				if (mf != null)
				{
					ClassInstancePointer cr;
					if (formInstances.TryGetValue(mf.Name, out cr))
					{
						crFields.Add(new ClassMemberField(mf, cr));
					}
				}
			}
			StringCollection classRefNames = new StringCollection();
			foreach (ClassMemberField cmf in crFields)
			{
				CodeMemberField mf = cmf.MemberField;
				ClassInstancePointer cr = cmf.Class;
				classRefNames.Add(cr.Name);
				mf.Type.BaseType = _classCompiler.GetClassTypeName(cr.ClassId);
				string name = mf.Name;
				mf.Name = "var" + Guid.NewGuid().GetHashCode().ToString("x");
				CodeMemberProperty mp = new CodeMemberProperty();
				mp.Name = name;
				mp.Type = mf.Type;
				td.Members.Add(mp);
				mp.Attributes = MemberAttributes.Public;
				if (isStaticClass)
				{
					mf.Attributes |= MemberAttributes.Static;
					mp.Attributes |= MemberAttributes.Static;
				}
				mp.HasGet = true;
				mp.HasSet = false;
				Type tx = VPLUtil.GetObjectType(cr.ObjectType);
				CodeBinaryOperatorExpression b1 = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(mf.Name), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null));
				CodeConditionStatement ccs;
				if (tx.IsSubclassOf(typeof(Form)))
				{
					CodeBinaryOperatorExpression b2 = new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(mf.Name), "IsDisposed"), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(true));
					ccs = new CodeConditionStatement(
						 new CodeBinaryOperatorExpression(b1, CodeBinaryOperatorType.BooleanOr, b2)
						 , new CodeAssignStatement(new CodeVariableReferenceExpression(mf.Name), new CodeObjectCreateExpression(mf.Type.BaseType))
						 );
				}
				else
				{
					ccs = new CodeConditionStatement(
						 b1
						 , new CodeAssignStatement(new CodeVariableReferenceExpression(mf.Name), new CodeObjectCreateExpression(mf.Type.BaseType))
						 );
				}
				if (this._project.ProjectType == EnumProjectType.Kiosk)
				{
					if (!string.IsNullOrEmpty(_classCompiler.AppName))
					{
						ccs.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(mf.Name), "Owner")
							, LimnorKioskApp.GetBackgroundFormRef(_classCompiler.AppName)
						)
						);
					}
				}
				mp.GetStatements.Add(ccs);
				if (tx.GetInterface("IComponent") != null)
				{
					PropertyInfo pif = tx.GetProperty("IsDisposed", BindingFlags.NonPublic);
					if (pif != null)
					{
						ccs = new CodeConditionStatement(
							new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(mf.Name), "IsDisposed")
							, new CodeAssignStatement(new CodeVariableReferenceExpression(mf.Name), new CodeObjectCreateExpression(mf.Type.BaseType))
							);
						if (this._project.ProjectType == EnumProjectType.Kiosk)
						{
							if (!string.IsNullOrEmpty(_classCompiler.AppName))
							{
								ccs.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(mf.Name), "Owner")
									, LimnorKioskApp.GetBackgroundFormRef(_classCompiler.AppName)
								)
								);
							}
						}
						mp.GetStatements.Add(ccs);
					}
				}
				mp.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(mf.Name)));
			}
			//remove ClassPointer construction ====================================
			//add ClassPointer custom property initializations 
			CodeMemberMethod mmInit = getInitializeMethod();
			if (mmInit != null)
			{
				List<CodeStatement> cts = new List<CodeStatement>();
				foreach (CodeStatement cs in mmInit.Statements)
				{
					CodeAssignStatement cas = cs as CodeAssignStatement;
					if (cas != null)
					{
						CodeFieldReferenceExpression fr = cas.Left as CodeFieldReferenceExpression;
						if (fr != null)
						{
							if (fr.TargetObject is CodeThisReferenceExpression || fr.TargetObject == null)
							{
								if (classRefNames.Contains(fr.FieldName))
								{
									CodeObjectCreateExpression oc = cas.Right as CodeObjectCreateExpression;
									if (oc != null)
									{
										cts.Add(cs);
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement cs in cts)
				{
					mmInit.Statements.Remove(cs);
				}
				if (addProperties)
				{
					//add custom property values
					foreach (ClassMemberField cmf in crFields)
					{
						int n = mmInit.Statements.Count;
						for (int i = 0; i < mmInit.Statements.Count; i++)
						{
							CodeCommentStatement ccs = mmInit.Statements[i] as CodeCommentStatement;
							if (ccs != null)
							{
								if (!string.IsNullOrEmpty(ccs.Comment.Text))
								{
									if (string.CompareOrdinal(ccs.Comment.Text, cmf.Class.Name) == 0)
									{
										n = i + 1;
										ccs = mmInit.Statements[i + 1] as CodeCommentStatement;
										if (ccs != null)
										{
											n++;
										}
										break;
									}
								}
							}
						}
						MethodClass mc = createGenericMethod("cp", null);
						mc.MethodCode = mmInit;
						//find the custom properties
						List<PropertyClassDescriptor> props = cmf.Class.CustomProperties;
						if (props != null && props.Count > 0)
						{
							foreach (PropertyClassDescriptor p in props)
							{
								if (p.ShouldSerializeValue(cmf.Class))
								{
									CodeExpression ceL = p.CustomProperty.GetReferenceCode(mc, mmInit.Statements, true);
									object v = p.GetValue(cmf.Class);
									CodeExpression ceR = ObjectCreationCodeGen.ObjectCreationCode(v);
									CodeAssignStatement cas = new CodeAssignStatement(ceL, ceR);
									mmInit.Statements.Insert(n++, cas);
								}
							}
						}
					}
				}
			}
		}
		private Type getDrawingItemType(Type controlType)
		{
			Type ret = null;
			try
			{
				ret = DrawingControl.GetDrawingItemType(controlType);
			}
			catch (Exception err)
			{
				AddError(err.Message);
			}
			return ret;
		}
		private void processServerObjPHP(IWebServerProgrammingSupport srv, string name, string srvTypeName, StringCollection webClientObjectNames, StringCollection webServerObjectNames, StringCollection ret, Dictionary<string, IWebServerProgrammingSupport> phpServerObject, StringCollection serverObjects)
		{
			srv.CreateOnRequestStartPhp(_serverScripts.OnRequestStart);
			srv.CreateOnRequestClientDataPhp(_serverScripts.OnRequestClientData);
			srv.CreateOnRequestFinishPhp(_serverScripts.OnRequestFinish);
			if (srv.DoNotCreate())
			{
				webClientObjectNames.Add(name);
			}
			else
			{
				phpServerObject.Add(name, srv);
				if (!webServerObjectNames.Contains(name))
				{
					webServerObjectNames.Add(name);
				}
				string decName = string.Format(CultureInfo.InvariantCulture, "private ${0};\r\n", name);
				if (!serverObjects.Contains(decName))
				{
					serverObjects.Add(decName);
				}
				IInstanceClass iic = srv as IInstanceClass;
				if (iic != null)
				{
					if (srv.NeedObjectName)
					{
						ret.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}=new {1}('{2}');\r\n", name, iic.ClassName, name));
					}
					else
					{
						ret.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}=new {1}();\r\n", name, iic.ClassName));
					}
				}
				else
				{
					if (srv.NeedObjectName)
					{
						ret.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}=new {1}('{2}');\r\n", name, srvTypeName, name));
					}
					else
					{
						ret.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}=new {1}();\r\n", name, srvTypeName));
					}
				}
			}
			if (_phpFiles == null)
			{
				_phpFiles = new List<Dictionary<string, string>>();
			}
			IExternalPhpFileReferencer phpref = srv as IExternalPhpFileReferencer;
			if (phpref != null)
			{
				if (_phpIncludes == null)
				{
					_phpIncludes = new StringCollection();
				}
				StringCollection phps = phpref.GetPhpFiles();
				if (phps != null && phps.Count > 0)
				{
					foreach (string sf in phps)
					{
						if (!string.IsNullOrEmpty(sf))
						{
							string s0 = sf.ToLowerInvariant();
							if (!_phpIncludes.Contains(s0))
							{
								if (File.Exists(s0))
								{
									_phpIncludes.Add(s0);
								}
							}
						}
					}
				}
			}
			Dictionary<string, string> dic = srv.GetPhpFilenames();
			if (dic != null)
			{
				StringCollection phpExists = new StringCollection();
				foreach (string php in dic.Keys)
				{
					foreach (Dictionary<string, string> ds in _phpFiles)
					{
						if (ds.ContainsKey(php))
						{
							phpExists.Add(php);
							break;
						}
					}
				}
				foreach (string php in phpExists)
				{
					dic.Remove(php);
				}
				if (dic.Count > 0)
				{
					_phpFiles.Add(dic);
				}
			}
		}
		private StringCollection convertToPhpWebPage(StringCollection serverObjects)
		{
			StringCollection ret = new StringCollection();
			StringCollection webClientObjectNames = new StringCollection();
			StringCollection webServerObjectNames = new StringCollection();
			Dictionary<string, IWebServerProgrammingSupport> phpServerObject = new Dictionary<string, IWebServerProgrammingSupport>();
			foreach (object v in _objMap.Keys)
			{
				IWebClientControl wc = v as IWebClientControl;
				if (wc != null)
				{
					webClientObjectNames.Add(((IComponent)wc).Site.Name);
				}
				IWebServerInternalPhp wi = v as IWebServerInternalPhp;
				if (wi != null)
				{
					wi.CreateOnRequestExecutePhp(_serverScripts.OnRequestExecution);
				}
				IWebServerProgrammingSupport srv = v as IWebServerProgrammingSupport;
				if (srv != null)
				{
					processServerObjPHP(srv, ((IComponent)v).Site.Name,v.GetType().Name, webClientObjectNames, webServerObjectNames, ret, phpServerObject, serverObjects);
				}
			}
			foreach (HtmlElement_BodyBase hbb in _rootClassId.UsedHtmlElements)
			{
				IWebServerProgrammingSupport srv = hbb as IWebServerProgrammingSupport;
				if (srv != null)
				{
					processServerObjPHP(srv, hbb.CodeName, hbb.ServerTypeName, webClientObjectNames, webServerObjectNames, ret, phpServerObject, serverObjects);
				}
			}
			List<CodeTypeMember> webClientComponents = new List<CodeTypeMember>();
			foreach (CodeTypeMember tm in td.Members)
			{
				if (webClientObjectNames.Contains(tm.Name))
				{
					webClientComponents.Add(tm);
				}
			}
			//remove web client components. 
			foreach (CodeTypeMember tm in webClientComponents)
			{
				td.Members.Remove(tm);
			}
			//delete web client components related code
			CodeMemberMethod init = getInitializeMethod();
			if (init != null)
			{
				List<CodeStatement> deletes = new List<CodeStatement>();
				for (int i = 0; i < init.Statements.Count; i++)
				{
					CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression pr = cas.Left as CodePropertyReferenceExpression;
						if (pr != null)
						{
							CodeFieldReferenceExpression fr = pr.TargetObject as CodeFieldReferenceExpression;
							if (fr != null)
							{
								if (webClientObjectNames.Contains(fr.FieldName))
								{
									deletes.Add(cas);
								}
							}
						}
						else
						{
							CodeFieldReferenceExpression fr = cas.Left as CodeFieldReferenceExpression;
							if (fr != null)
							{
								if (webClientObjectNames.Contains(fr.FieldName))
								{
									deletes.Add(cas);
								}
							}
						}
					}
					else
					{
						CodeExpressionStatement es = init.Statements[i] as CodeExpressionStatement;
						if (es != null)
						{
							CodeMethodInvokeExpression mie = es.Expression as CodeMethodInvokeExpression;
							if (mie != null)
							{
								if (mie.Method.TargetObject is CodeThisReferenceExpression)
								{
									deletes.Add(es);
								}
								else
								{
									CodePropertyReferenceExpression pr = mie.Method.TargetObject as CodePropertyReferenceExpression;
									if (pr != null)
									{
										if (string.CompareOrdinal(pr.PropertyName, "Controls") == 0)
										{
											deletes.Add(es);
										}
									}
									else
									{
										CodeFieldReferenceExpression fr = mie.Method.TargetObject as CodeFieldReferenceExpression;
										if (fr != null)
										{
											if (webClientObjectNames.Contains(fr.FieldName))
											{
												deletes.Add(es);
											}
										}
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement s in deletes)
				{
					init.Statements.Remove(s);
				}
				for (int i = 0; i < init.Statements.Count; i++)
				{
					CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression pr = cas.Left as CodePropertyReferenceExpression;
						if (pr != null)
						{
							CodeFieldReferenceExpression fr = pr.TargetObject as CodeFieldReferenceExpression;
							if (fr != null)
							{
								if (webServerObjectNames.Contains(fr.FieldName))
								{
									object val = null;
									CodePrimitiveExpression cr = cas.Right as CodePrimitiveExpression;
									if (cr != null)
									{
										val = cr.Value;
									}
									else
									{
										CodeFieldReferenceExpression frr = cas.Right as CodeFieldReferenceExpression;
										if (frr != null)
										{
											//frr.FieldName 
											CodeTypeReferenceExpression ctr = frr.TargetObject as CodeTypeReferenceExpression;
											if (ctr != null)
											{
												Type trr = Type.GetType(ctr.Type.BaseType);
												if (trr == null)
												{
													if (string.CompareOrdinal(ctr.Type.BaseType, "Limnor.PhpComponents.EnumPhpOpenFileMode") == 0)
													{
														trr = typeof(EnumPhpOpenFileMode);
													}
													else if (string.CompareOrdinal(ctr.Type.BaseType, "WindowsUtility.EnumPasswordHash") == 0)
													{
														trr = typeof(EnumPasswordHash);
													}
												}
												if (trr != null)
												{
													if (trr.IsEnum)
													{
														val = (int)(Enum.Parse(trr, frr.FieldName));
													}
												}
											}
										}
									}
									if (val != null)
									{
										IWebServerProgrammingSupport wss;
										if (phpServerObject.TryGetValue(fr.FieldName, out wss))
										{
											if (wss.ExcludePropertyForPhp(pr.PropertyName))
											{
												continue;
											}
										}
										ret.Add(string.Format(CultureInfo.InvariantCulture,
											"$this->{0}->{1}={2};\r\n", fr.FieldName, pr.PropertyName, ValueTypeUtil.GetPhpScriptValue(val)));
									}
								}
							}
						}
					}
				}
			}
			foreach (object v in _objMap.Keys)
			{
				IWebServerComponentCreator wsc = v as IWebServerComponentCreator;
				if (wsc != null)
				{
					wsc.CreateServerComponentPhp(serverObjects, ret, _serverScripts);
				}
			}
			return ret;
		}
		private void convertToAspxWebPage()
		{
			Dictionary<string, IWebServerComponentCreatorBase> webSrvs = new Dictionary<string, IWebServerComponentCreatorBase>();
			StringCollection webClientObjectNames = new StringCollection();
			foreach (object v in _objMap.Keys)
			{
				IWebServerComponentCreatorBase sc = v as IWebServerComponentCreatorBase;
				if (sc != null)
				{
					webSrvs.Add(((IComponent)v).Site.Name, sc);
				}
				IWebClientControl wc = v as IWebClientControl;
				if (wc != null)
				{
					IWebServerProgrammingSupport ws = v as IWebServerProgrammingSupport;
					if (ws != null)
					{
						if (ws.IsWebServerProgrammingSupported(EnumWebServerProcessor.Aspx))
						{
							continue;
						}
					}
					IAspxPageUser aspx = v as IAspxPageUser;
					if (aspx != null)
					{
						continue;
					}
					webClientObjectNames.Add(((IComponent)wc).Site.Name);
				}
			}
			List<CodeTypeMember> webClientComponents = new List<CodeTypeMember>();
			foreach (CodeTypeMember tm in td.Members)
			{
				if (webClientObjectNames.Contains(tm.Name))
				{
					webClientComponents.Add(tm);
				}
			}
			//remove web client components. 
			foreach (CodeTypeMember tm in webClientComponents)
			{
				td.Members.Remove(tm);
			}
			//delete web client components related code
			CodeMemberMethod init = getInitializeMethod();
			if (init != null)
			{
				List<CodeStatement> deletes = new List<CodeStatement>();
				for (int i = 0; i < init.Statements.Count; i++)
				{
					CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression pr = cas.Left as CodePropertyReferenceExpression;
						if (pr != null)
						{
							CodeFieldReferenceExpression fr = pr.TargetObject as CodeFieldReferenceExpression;
							if (fr != null)
							{
								if (webClientObjectNames.Contains(fr.FieldName))
								{
									deletes.Add(cas);
								}
								else
								{
									IWebServerComponentCreatorBase isc;
									if (webSrvs.TryGetValue(fr.FieldName, out isc))
									{
										if (isc.RemoveFromComponentInitializer(pr.PropertyName))
										{
											deletes.Add(cas);
										}
									}
								}
							}
							else
							{
								if (pr.TargetObject is CodeThisReferenceExpression)
								{
									bool deleted = false;
									if (_isWebPage)
									{
										PropertyInfo pif = _rootClassId.BaseClassType.GetProperty(pr.PropertyName);
										if (pif != null)
										{
											object[] vs = pif.GetCustomAttributes(typeof(WebClientMemberAttribute), false);
											if (vs == null || vs.Length == 0)
											{
												deletes.Add(cas);
												deleted = true;
											}
										}
									}
									if (!deleted)
									{
										if (string.CompareOrdinal(pr.PropertyName, "LoginPageId") == 0 ||
											string.CompareOrdinal(pr.PropertyName, "LoginPage") == 0 ||
											string.CompareOrdinal(pr.PropertyName, "Cursor") == 0 ||
											string.CompareOrdinal(pr.PropertyName, "MaximumSize") == 0 ||
											string.CompareOrdinal(pr.PropertyName, "BackColor") == 0
											)
										{
											deletes.Add(cas);
										}
									}
								}
							}
						}
						else
						{
							CodeFieldReferenceExpression fr = cas.Left as CodeFieldReferenceExpression;
							if (fr != null)
							{
								if (webClientObjectNames.Contains(fr.FieldName))
								{
									deletes.Add(cas);
								}
							}
						}
					}
					else
					{
						CodeExpressionStatement es = init.Statements[i] as CodeExpressionStatement;
						if (es != null)
						{
							CodeMethodInvokeExpression mie = es.Expression as CodeMethodInvokeExpression;
							if (mie != null)
							{
								if (mie.Method.TargetObject is CodeThisReferenceExpression)
								{
									deletes.Add(es);
								}
								else
								{
									CodePropertyReferenceExpression pr = mie.Method.TargetObject as CodePropertyReferenceExpression;
									if (pr != null)
									{
										if (string.CompareOrdinal(pr.PropertyName, "Controls") == 0)
										{
											deletes.Add(es);
										}
										else
										{
											CodeFieldReferenceExpression fr = pr.TargetObject as CodeFieldReferenceExpression;
											if (fr != null)
											{
												if (webClientObjectNames.Contains(fr.FieldName))
												{
													deletes.Add(es);
												}
											}
										}
									}
									else
									{
										CodeFieldReferenceExpression fr = mie.Method.TargetObject as CodeFieldReferenceExpression;
										if (fr != null)
										{
											if (webClientObjectNames.Contains(fr.FieldName))
											{
												deletes.Add(es);
											}
										}
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement s in deletes)
				{
					init.Statements.Remove(s);
				}
				CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
				cmi.Method = new CodeMethodReferenceExpression();
				cmi.Method.MethodName = "SetCurrentPage";
				cmi.Method.TargetObject = new CodeTypeReferenceExpression(typeof(WebAppAspx));
				cmi.Parameters.Add(new CodeThisReferenceExpression());
				init.Statements.Add(new CodeExpressionStatement(cmi));
				foreach (object v in _objMap.Keys)
				{
					IAspxPageUser aspx = v as IAspxPageUser;
					if (aspx != null)
					{
						cmi = new CodeMethodInvokeExpression();
						cmi.Method = new CodeMethodReferenceExpression();
						cmi.Method.MethodName = "SetAspxPage";
						cmi.Method.TargetObject = new CodeVariableReferenceExpression(((IComponent)v).Site.Name);
						cmi.Parameters.Add(new CodeThisReferenceExpression());
						init.Statements.Add(new CodeExpressionStatement(cmi));
					}
				}
				foreach (HtmlElement_BodyBase v in _rootClassId.UsedHtmlElements)
				{
					IAspxPageUser aspx = v as IAspxPageUser;
					if (aspx != null)
					{
						cmi = new CodeMethodInvokeExpression();
						cmi.Method = new CodeMethodReferenceExpression();
						cmi.Method.MethodName = "SetAspxPage";
						cmi.Method.TargetObject = new CodeVariableReferenceExpression(v.CodeName);
						cmi.Parameters.Add(new CodeThisReferenceExpression());
						init.Statements.Add(new CodeExpressionStatement(cmi));
					}
				}
			}
			//
			StringCollection fsnames = new StringCollection();
			foreach (object v in _objMap.Keys)
			{
				IFileUploador fu = v as IFileUploador;
				if (fu != null)
				{
					fsnames.Add(((IComponent)v).Site.Name);
				}
			}
			foreach (HtmlElement_BodyBase v in _rootClassId.UsedHtmlElements)
			{
				IFileUploador fu = v as IFileUploador;
				if (fu != null)
				{
					fsnames.Add(v.CodeName);
				}
			}
			if (fsnames.Count > 0)
			{
				CodeMemberMethod cmm = new CodeMemberMethod();
				cmm.Attributes = MemberAttributes.Override | MemberAttributes.Family;
				cmm.Name = "GetFileUploador";
				cmm.ReturnType = new CodeTypeReference(typeof(IList<IFileUploador>));
				string var = string.Format(CultureInfo.InvariantCulture, "x{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				CodeVariableDeclarationStatement vd = new CodeVariableDeclarationStatement(typeof(List<IFileUploador>), var, new CodeObjectCreateExpression(typeof(List<IFileUploador>)));
				cmm.Statements.Add(vd);
				for (int i = 0; i < fsnames.Count; i++)
				{
					CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
					cmi.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(var), "Add");
					cmi.Parameters.Add(new CodeVariableReferenceExpression(fsnames[i]));
					cmm.Statements.Add(cmi);
				}
				cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(var)));
				td.Members.Add(cmm);
			}
			//
			CodeMemberMethod cmmF = new CodeMemberMethod();
			cmmF.Attributes = MemberAttributes.Override | MemberAttributes.Family;
			cmmF.Name = "CreateFileUploader";
			cmmF.ReturnType = new CodeTypeReference(typeof(IFileUploador));
			cmmF.Parameters.Add(new CodeParameterDeclarationExpression(typeof(HttpPostedFile), "f"));
			string varF = string.Format(CultureInfo.InvariantCulture, "x{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
			CodeVariableDeclarationStatement vdF = new CodeVariableDeclarationStatement(typeof(HtmlFileUpload), varF, new CodeObjectCreateExpression(typeof(HtmlFileUpload)));
			cmmF.Statements.Add(vdF);
			CodeMethodInvokeExpression cmiF = new CodeMethodInvokeExpression();
			cmiF.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(varF), "SetPostedFile");
			cmiF.Parameters.Add(new CodeVariableReferenceExpression("f"));
			cmmF.Statements.Add(cmiF);
			cmmF.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(varF)));
			td.Members.Add(cmmF);
			//adjust base type
			td.BaseTypes.Clear();
			td.BaseTypes.Add(typeof(JsonWebServerProcessor));
			//======================================================================================
			CodeConditionStatement ccs;
			CodeMethodInvokeExpression mie2;
			foreach (object v in _objMap.Keys)
			{
				IDataSetSource dss = v as IDataSetSource;
				if (dss != null)
				{
					ccs = new CodeConditionStatement();
					ccs.Condition = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(((IComponent)v).Site.Name), "IsQueryOK");
					mie2 = new CodeMethodInvokeExpression();
					mie2.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SetData");
					mie2.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(((IComponent)v).Site.Name), "DataStorage"));
					ccs.TrueStatements.Add(mie2);
					_serverPageCode.OnRequestFinish.Add(ccs);
				}
			}
			ccs = new CodeConditionStatement();
			ccs.Condition = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DEBUG");
			foreach (object v in _objMap.Keys)
			{
				IDataSetSource dss = v as IDataSetSource;
				if (dss != null)
				{
					mie2 = new CodeMethodInvokeExpression();
					mie2.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Response"), "Write");
					mie2.Parameters.Add(new CodePrimitiveExpression(string.Format(CultureInfo.InvariantCulture, "\r\n{0}.Messages ========\r\n", ((IComponent)v).Site.Name)));
					ccs.TrueStatements.Add(mie2);
					//
					mie2 = new CodeMethodInvokeExpression();
					mie2.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Response"), "Write");
					mie2.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(((IComponent)v).Site.Name), "Messages"));
					ccs.TrueStatements.Add(mie2);
					//
					mie2 = new CodeMethodInvokeExpression();
					mie2.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Response"), "Write");
					mie2.Parameters.Add(new CodePrimitiveExpression("\r\n===============================\r\n"));
					ccs.TrueStatements.Add(mie2);
					//
					CodeMethodInvokeExpression putData = new CodeMethodInvokeExpression();
					putData.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "UpdateData");
					putData.Parameters.Add(new CodeVariableReferenceExpression(((IComponent)v).Site.Name));
					putData.Parameters.Add(new CodeVariableReferenceExpression("value"));
					_serverPageCode.OnRequestPutData.Add(putData);
				}
			}
			if (ccs.TrueStatements.Count > 0)
			{
				_serverPageCode.OnRequestFinish.Add(ccs);
			}
			foreach (object v in _objMap.Keys)
			{
				if (!(v is IWebClientComponent))
				{
					IWebServerProgrammingSupport wsps = v as IWebServerProgrammingSupport;
					if (wsps != null)
					{
						mie2 = new CodeMethodInvokeExpression();
						mie2.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(((IComponent)v).Site.Name), "OnRequestStart");
						mie2.Parameters.Add(new CodeThisReferenceExpression());
						_serverPageCode.OnRequestStart.Add(mie2);
					}
				}
			}
			//======================================================================================
		}
		private void fixSpecialCases()
		{
			Dictionary<string, RuntimeInstance> specialNames = new Dictionary<string, RuntimeInstance>();
			foreach (object v in _objMap.Keys)
			{
				RuntimeInstance lti = v as RuntimeInstance;
				if (lti != null)
				{
					specialNames.Add(lti.Site.Name, lti);
				}
			}
			if (specialNames.Count > 0)
			{
				for (int i = 0; i < td.Members.Count; i++)
				{
					CodeMemberField mf = td.Members[i] as CodeMemberField;
					if (mf != null)
					{
						if (specialNames.ContainsKey(mf.Name))
						{
							RuntimeInstance lti = specialNames[mf.Name];
							mf.Type = new CodeTypeReference(lti.InstanceType);
						}
					}
				}
			}
			CodeMemberMethod init = getInitializeMethod();
			if (init != null)
			{
				if (specialNames.Count > 0)
				{
					List<CodeStatement> deletes = new List<CodeStatement>();
					for (int i = 0; i < init.Statements.Count; i++)
					{
						CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
						if (cas != null)
						{
							CodeFieldReferenceExpression cfr = cas.Left as CodeFieldReferenceExpression;
							if (cfr != null)
							{
								if (specialNames.ContainsKey(cfr.FieldName))
								{
									CodeObjectCreateExpression coce = cas.Right as CodeObjectCreateExpression;
									if (coce != null)
									{
										RuntimeInstance ri = specialNames[cfr.FieldName];
										coce.CreateType = new CodeTypeReference(ri.InstanceType);
										coce.Parameters.Clear();
										PropertyDescriptorCollection rips = ri.GetProperties();
										foreach (PropertyDescriptor p in rips)
										{
											if (string.CompareOrdinal(p.Name, "EndPointName") == 0)
											{
												string v = p.GetValue(ri) as string;
												if (string.IsNullOrEmpty(v))
												{
													string cfgfile = Path.Combine(_project.ProjectFolder, string.Format(CultureInfo.InvariantCulture, "{0}.config", Path.GetFileNameWithoutExtension(ri.Filename)));
													if (File.Exists(cfgfile))
													{
														XmlDocument doc = new XmlDocument();
														doc.Load(cfgfile);
														XmlNode bindNode = doc.SelectSingleNode("//binding");
														if (bindNode != null)
														{
															v = XmlUtil.GetNameAttribute(bindNode);
														}
													}
												}
												coce.Parameters.Add(ObjectCreationCodeGen.ObjectCreationCode(v));
												break;
											}
										}
									}
									else
									{
										deletes.Add(cas);
									}
								}
							}
							else
							{
								CodePropertyReferenceExpression cpr = cas.Left as CodePropertyReferenceExpression;
								if (cpr != null)
								{
									cfr = cpr.TargetObject as CodeFieldReferenceExpression;
									if (cfr != null)
									{
										if (specialNames.ContainsKey(cfr.FieldName))
										{
											if (specialNames[cfr.FieldName].IsDesignOnlyProperty(cpr.PropertyName))
											{
												deletes.Add(cas);
											}
										}
									}
								}
							}
						}
					}
					if (deletes.Count > 0)
					{
						foreach (CodeStatement cs in deletes)
						{
							init.Statements.Remove(cs);
						}
					}
				}
				//
				FileSystemWatcher fw = null;
				foreach (object v in _objMap.Keys)
				{
					fw = v as FileSystemWatcher;
					if (fw != null)
					{
						string name = fw.Site.Name;
						CodeStatement cs = null;
						for (int i = 0; i < init.Statements.Count; i++)
						{
							CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
							if (cas != null)
							{
								CodePropertyReferenceExpression cpr = cas.Left as CodePropertyReferenceExpression;
								if (cpr != null)
								{
									if (string.Compare("EnableRaisingEvents", cpr.PropertyName, StringComparison.OrdinalIgnoreCase) == 0)
									{
										CodeFieldReferenceExpression cfr = cpr.TargetObject as CodeFieldReferenceExpression;
										if (cfr != null)
										{
											if (string.Compare(name, cfr.FieldName, StringComparison.OrdinalIgnoreCase) == 0)
											{
												if (cfr.TargetObject is CodeThisReferenceExpression)
												{
													cs = cas;
													init.Statements.RemoveAt(i);
													break;
												}
											}
										}
									}
								}
							}
						}
						if (cs != null)
						{
							init.Statements.Add(cs);
						}
					}
				}
				IControlChild icc;
				foreach (object v in _objMap.Keys)
				{
					icc = v as IControlChild;
					if (icc != null)
					{
						string name = ((IComponent)(v)).Site.Name;
						for (int i = 0; i < init.Statements.Count; i++)
						{
							CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
							if (cas != null)
							{
								CodeFieldReferenceExpression cfr = cas.Left as CodeFieldReferenceExpression;
								if (cfr != null)
								{
									if (string.CompareOrdinal(cfr.FieldName, name) == 0)
									{
										if (cfr.TargetObject is CodeThisReferenceExpression)
										{
											CodeObjectCreateExpression coce = cas.Right as CodeObjectCreateExpression;
											if (coce != null)
											{
												CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
												cmie.Method = new CodeMethodReferenceExpression();
												cmie.Method.MethodName = "SetControlOwner";
												cmie.Method.TargetObject = cfr;
												cmie.Parameters.Add(new CodeThisReferenceExpression());
												init.Statements.Insert(i + 1, new CodeExpressionStatement(cmie));
												break;
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		private void addDrawingSupport()
		{
			DrawingPage drawingPage = _rootObject as DrawingPage;
			if (_useDrawing)
			{
				StringCollection drawingNames = new StringCollection();
				CodeExpressionStatement es;
				CodeMethodInvokeExpression mie;
				for (int i = 0; i < td.Members.Count; i++)
				{
					CodeMemberField var = td.Members[i] as CodeMemberField;
					if (var != null)
					{
						Type t = getFieldType(var.Name);
						if (t != null)
						{
							if (t.GetInterface("IDrawDesignControl") != null)
							{
								Type dt = getDrawingItemType(t);
								if (dt != null)
								{
									var.Type.BaseType = dt.FullName;
									drawingNames.Add(var.Name);
								}
							}
						}
					}
				}
				CodeMemberMethod initMethod = getInitializeMethod();
				//
				//===change type from IDrawDesignControl to DrawingItem=============
				for (int i = 0; i < initMethod.Statements.Count; i++)
				{
					CodeVariableDeclarationStatement var = initMethod.Statements[i] as CodeVariableDeclarationStatement;
					if (var != null)
					{
						CodeObjectCreateExpression oce = var.InitExpression as CodeObjectCreateExpression;
						if (oce != null)
						{
							Type t = getFieldType(var.Name);
							if (t != null)
							{
								if (t.GetInterface("IDrawDesignControl") != null)
								{
									Type dt = getDrawingItemType(t);
									if (dt != null)
									{
										oce.CreateType.BaseType = dt.FullName;
										if (!drawingNames.Contains(var.Name))
										{
											drawingNames.Add(var.Name);
										}
									}
								}
							}
						}
					}
					else
					{
						CodeAssignStatement cas = initMethod.Statements[i] as CodeAssignStatement;
						if (cas != null)
						{
							CodeObjectCreateExpression oce = cas.Right as CodeObjectCreateExpression;
							if (oce != null)
							{
								CodeVariableReferenceExpression var2 = cas.Left as CodeVariableReferenceExpression;
								if (var2 != null)
								{
									Type t = getFieldType(var2.VariableName);
									if (t != null)
									{
										if (t.GetInterface("IDrawDesignControl") != null)
										{
											Type dt = getDrawingItemType(t);
											if (dt != null)
											{
												oce.CreateType.BaseType = dt.FullName;
												if (!drawingNames.Contains(var2.VariableName))
												{
													drawingNames.Add(var2.VariableName);
												}
											}
										}
									}
								}
								else
								{
									CodeFieldReferenceExpression f2 = cas.Left as CodeFieldReferenceExpression;
									if (f2 != null)
									{
										Type t = getFieldType(f2.FieldName);
										if (t != null)
										{
											if (t.GetInterface("IDrawDesignControl") != null)
											{
												Type dt = getDrawingItemType(t);
												if (dt != null)
												{
													oce.CreateType.BaseType = dt.FullName;
													if (!drawingNames.Contains(f2.FieldName))
													{
														drawingNames.Add(f2.FieldName);
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				//===============================================================
				//===remove Add IDrawDesignControl and save databinding for drawings=============
				List<CodeExpressionStatement> drawingBindngs = new List<CodeExpressionStatement>();
				int k = 0;
				while (k < initMethod.Statements.Count)
				{
					es = initMethod.Statements[k] as CodeExpressionStatement;
					if (es != null)
					{
						mie = es.Expression as CodeMethodInvokeExpression;
						if (mie != null)
						{
							if (string.CompareOrdinal(mie.Method.MethodName, "Add") == 0)
							{
								CodePropertyReferenceExpression pre = mie.Method.TargetObject as CodePropertyReferenceExpression;
								if (pre != null)
								{
									if (pre.TargetObject is CodeThisReferenceExpression)
									{
										if (string.CompareOrdinal(pre.PropertyName, "Controls") == 0)
										{
											if (mie.Parameters.Count == 1)
											{
												CodeFieldReferenceExpression cv = mie.Parameters[0] as CodeFieldReferenceExpression;
												if (cv != null)
												{
													if (drawingNames.Contains(cv.FieldName))
													{
														initMethod.Statements.RemoveAt(k);
														continue;
													}
												}
											}
										}
									}
									else
									{
										if (string.CompareOrdinal(pre.PropertyName, "DataBindings") == 0)
										{
											CodeFieldReferenceExpression cfre = pre.TargetObject as CodeFieldReferenceExpression;
											if (cfre != null)
											{
												if (cfre.TargetObject is CodeThisReferenceExpression)
												{
													if (drawingNames.Contains(cfre.FieldName))
													{
														drawingBindngs.Add(es);
														initMethod.Statements.RemoveAt(k);
														continue;
													}
												}
											}
										}
									}
								}
							}
						}
					}
					k++;
				}
				//===remove Size assignment statements and read-only for all DrawingItem objects
				List<CodeAssignStatement> assignSizes = new List<CodeAssignStatement>();
				for (int i = 0; i < initMethod.Statements.Count; i++)
				{
					CodeAssignStatement cas = initMethod.Statements[i] as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression pre = cas.Left as CodePropertyReferenceExpression;
						if (pre != null)
						{
							CodeFieldReferenceExpression fre = pre.TargetObject as CodeFieldReferenceExpression;
							if (fre != null)
							{
								if (fre.TargetObject is CodeThisReferenceExpression)
								{
									if (drawingNames.Contains(fre.FieldName))
									{
										if (string.CompareOrdinal(pre.PropertyName, "Size") == 0)
										{
											assignSizes.Add(cas);
										}
										else
										{
											for (int h = 0; h < drawingPage.Controls.Count; h++)
											{
												IDrawDesignControl dc = drawingPage.Controls[h] as IDrawDesignControl;
												if (dc != null)
												{
													if (string.CompareOrdinal(dc.Name, fre.FieldName)==0)
													{
														if (dc.IsPropertyReadOnly(pre.PropertyName))
														{
															assignSizes.Add(cas);
														}
														else if (dc.ExcludeFromInitialize(pre.PropertyName))
														{
															assignSizes.Add(cas);
														}
														break;
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				foreach (CodeAssignStatement cas in assignSizes)
				{
					initMethod.Statements.Remove(cas);
				}
				//=====================================================================
				//===Add AddDrawing(DrawingItem) for each DrawingItem object before ResumeLayout
				int nIndex = initMethod.Statements.Count;
				for (int i = 0; i < initMethod.Statements.Count; i++)
				{
					es = initMethod.Statements[i] as CodeExpressionStatement;
					if (es != null)
					{
						mie = es.Expression as CodeMethodInvokeExpression;
						if (mie != null)
						{
							if (mie.Method.TargetObject is CodeThisReferenceExpression)
							{
								if (string.CompareOrdinal(mie.Method.MethodName, "ResumeLayout") == 0)
								{
									nIndex = i;
									break;
								}
							}
						}
					}
				}
				ArrayList sortedItems = new ArrayList();
				for (int i = 0; i < drawingPage.Controls.Count; i++)
				{
					IDrawDesignControl dc = drawingPage.Controls[i] as IDrawDesignControl;
					if (dc != null)
					{
						sortedItems.Add(dc);
					}
				}
				sortedItems.Sort();
				for (int i = 0; i < sortedItems.Count; i++)
				{
					IDrawDesignControl dc = (IDrawDesignControl)(sortedItems[i]);
					es = new CodeExpressionStatement();
					mie = new CodeMethodInvokeExpression();
					mie.Method = new CodeMethodReferenceExpression();
					mie.Method.TargetObject = new CodeThisReferenceExpression();
					mie.Method.MethodName = "AddDrawing";
					mie.Parameters.Add(new CodeVariableReferenceExpression(dc.Name));
					es.Expression = mie;
					initMethod.Statements.Insert(nIndex++, es);
				}
				for (int i = 0; i < drawingBindngs.Count; i++)
				{
					initMethod.Statements.Insert(nIndex++, drawingBindngs[i]);
				}
			}
			else
			{
				//change the Form type from DrawingPage to Form
				if (drawingPage != null)
				{
					if (!_classCompiler.UseDrawing)
					{
						if (td.BaseTypes != null)
						{
							Type t = typeof(DrawingPage);
							foreach (CodeTypeReference tr in td.BaseTypes)
							{
								if (tr.BaseType == t.FullName)
								{
									tr.BaseType = typeof(Form).FullName;
									tr.TypeArguments.Clear();
									break;
								}
							}
						}
					}
					List<CodeStatement> toDelete = new List<CodeStatement>();
					CodeMemberMethod initMethod = getInitializeMethod();
					for (int i = 0; i < initMethod.Statements.Count; i++)
					{
						CodeAssignStatement cas = initMethod.Statements[i] as CodeAssignStatement;
						if (cas != null)
						{
							CodePropertyReferenceExpression pr = cas.Left as CodePropertyReferenceExpression;
							if (pr != null)
							{
								if (pr.TargetObject is CodeThisReferenceExpression)
								{
									if (string.CompareOrdinal(pr.PropertyName, "ActiveLayerId") == 0 || string.CompareOrdinal(pr.PropertyName, "DrawingLayerList") == 0)
									{
										toDelete.Add(cas);
									}
								}
							}
						}
					}
					foreach (CodeStatement cas in toDelete)
					{
						initMethod.Statements.Remove(cas);
					}
				}
			}
		}
		/// <summary>
		/// add interfaces to td
		/// td must be created first
		/// </summary>
		private void addInterfaces()
		{
			List<InterfacePointer> interfaces = _rootClassId.GetAddedInterfaces();
			foreach (InterfacePointer ifp in interfaces)
			{
				td.BaseTypes.Add(ifp.TypeString);
			}
		}
		//
		/// <summary>
		/// handle entry point and base classes
		/// </summary>
		/// <returns>base type has processed
		/// </returns>
		private bool adjustDerivation()
		{
			if (IsEntryPoint)
			{
				td.BaseTypes.Clear();
				return true;
			}
			if (IsWebApp)
			{
				td.BaseTypes.Clear();
				td.BaseTypes.Add(typeof(WebAppAspx));
				return true;
			}
			else
			{
				if (_rootClassId.BaseClassPointer != null)
				{
					if (td.BaseTypes != null)
					{
						foreach (CodeTypeReference tr in td.BaseTypes)
						{
							if (string.CompareOrdinal(tr.BaseType, XClassName) == 0 || string.CompareOrdinal(tr.BaseType, _rootClassId.BaseClassPointer.ObjectType.Name) == 0)
							{
								tr.BaseType = _rootClassId.BaseClassPointer.Name;
								tr.TypeArguments.Clear();
								break;
							}
						}
					}
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		private void removeFieldsFromParents()
		{
			bool isEntryPoint = IsEntryPoint;
			ArrayList fieldsInParents = null;
			List<CodeMemberField> classVariables = new List<CodeMemberField>();
			List<CodeMemberField> classCreations = new List<CodeMemberField>();
			ArrayList lstDup = new ArrayList();
			foreach (CodeTypeMember m in td.Members)
			{
				if (isEntryPoint)
				{
					m.Attributes = MemberAttributes.Static | MemberAttributes.Private;
				}
				//declararion
				if (m is System.CodeDom.CodeMemberField)
				{
					System.CodeDom.CodeMemberField f = (System.CodeDom.CodeMemberField)m;
					if (f != null)
					{
						if (string.CompareOrdinal(f.Type.BaseType, "VPL.Class") == 0)
						{
							lstDup.Add(m);
							classVariables.Add(f);
							f = null;
						}
					}
					if (f != null)
					{
						if (fieldsInParents != null)
						{
							foreach (string s in fieldsInParents)
							{
								if (s == f.Name)
								{
									lstDup.Add(f);
									break;
								}
							}
						}
					}
				}
				else if (m is CodeMemberMethod)
				{
					CodeMemberMethod mm = (CodeMemberMethod)m;
					if (string.CompareOrdinal(mm.Name, "InitializeComponent") == 0)
					{
						List<CodeStatement> l0 = new List<CodeStatement>();
						if (fieldsInParents != null)
						{
							foreach (System.CodeDom.CodeStatement s in mm.Statements)
							{
								if (s is System.CodeDom.CodeAssignStatement)
								{
									System.CodeDom.CodeAssignStatement cas = (System.CodeDom.CodeAssignStatement)s;
									if (cas.Left is System.CodeDom.CodeFieldReferenceExpression)
									{
										if (cas.Right is CodeObjectCreateExpression)
										{
											System.CodeDom.CodeFieldReferenceExpression cfr = (System.CodeDom.CodeFieldReferenceExpression)(cas.Left);
											foreach (string fn in fieldsInParents)
											{
												if (cfr.FieldName == fn)
												{
													//this is a member variable initiation statement.
													//this is done in the parent class, so, remove it from this calss
													l0.Add(s);
													break;
												}
											}
										}
									}
								}
								else if (s is System.CodeDom.CodeExpressionStatement)
								{
									CodeExpressionStatement ces = (CodeExpressionStatement)s;
									if (ces.Expression is CodeMethodInvokeExpression)
									{
										CodeMethodInvokeExpression cmi = (CodeMethodInvokeExpression)(ces.Expression);
										if (string.CompareOrdinal(cmi.Method.MethodName, "Add") == 0)
										{
											if (cmi.Method.TargetObject is CodePropertyReferenceExpression)
											{
												if (string.CompareOrdinal(((CodePropertyReferenceExpression)(cmi.Method.TargetObject)).PropertyName, "Controls") == 0)
												{
													if (cmi.Parameters != null && cmi.Parameters.Count == 1)
													{
														if (cmi.Parameters[0] is System.CodeDom.CodeFieldReferenceExpression)
														{
															CodeFieldReferenceExpression cfr = (CodeFieldReferenceExpression)cmi.Parameters[0];
															foreach (string fn in fieldsInParents)
															{
																if (cfr.FieldName == fn)
																{
																	//this is a statement of Controls.Add(fn)
																	//because it is done in parent, remove it from here
																	l0.Add(s);
																	break;
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
							foreach (CodeStatement v in l0)
							{
								mm.Statements.Remove(v);
							}
						}
						//adjust VPL.Class generated code
						l0 = new List<CodeStatement>();
						Dictionary<string, string> classMemberMapping = new Dictionary<string, string>();
						foreach (System.CodeDom.CodeStatement s in mm.Statements)
						{
							if (s is CodeAssignStatement)
							{
								CodeAssignStatement cas = (CodeAssignStatement)s;
								if (cas.Left is CodeFieldReferenceExpression)
								{
									CodeFieldReferenceExpression fr = (CodeFieldReferenceExpression)cas.Left;
									foreach (CodeMemberField mf in classVariables)
									{
										if (fr.FieldName == mf.Name)
										{
											l0.Add(s);
											break;
										}
									}
								}
								else if (cas.Left is CodePropertyReferenceExpression)
								{
									CodePropertyReferenceExpression pr = (CodePropertyReferenceExpression)cas.Left;
									if (pr.TargetObject is CodeFieldReferenceExpression)
									{
										CodeFieldReferenceExpression fr = (CodeFieldReferenceExpression)pr.TargetObject;
										if (fr.TargetObject is CodeThisReferenceExpression)
										{
											foreach (CodeMemberField mf in classVariables)
											{
												if (fr.FieldName == mf.Name)
												{
													//this.Class1.?? = ??;
													l0.Add(s);
													//from ObjectValue get the variable name
													if (string.CompareOrdinal(pr.PropertyName, "ObjectValue") == 0)
													{
														if (cas.Right is CodeVariableReferenceExpression)
														{
															CodeVariableReferenceExpression vr = (CodeVariableReferenceExpression)cas.Right;
															classMemberMapping.Add(mf.Name, vr.VariableName);
														}
													}
													break;
												}
											}
										}
									}
								}
							}
						}
						//remove VPL.Class related code
						foreach (CodeStatement v in l0)
						{
							mm.Statements.Remove(v);
						}
						//create member variables
						l0 = new List<CodeStatement>();
						foreach (KeyValuePair<string, string> kv in classMemberMapping)
						{
							foreach (System.CodeDom.CodeStatement s in mm.Statements)
							{
								if (s is CodeVariableDeclarationStatement)
								{
									CodeVariableDeclarationStatement vds = (CodeVariableDeclarationStatement)s;
									if (vds.Name == kv.Value)
									{
										l0.Add(s);
									}
								}
							}
						}
						//remove VPL.Class related code
						foreach (CodeStatement v in l0)
						{
							mm.Statements.Remove(v);
							CodeVariableDeclarationStatement vds = (CodeVariableDeclarationStatement)v;
							CodeMemberField mf = new CodeMemberField(vds.Type, vds.Name);
							classCreations.Add(mf);
							CodeAssignStatement cas = new CodeAssignStatement(
								new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), vds.Name),
								vds.InitExpression);
							mm.Statements.Insert(1, cas);
						}
						Dictionary<string, Dictionary<string, object>> cdata = null;// _xtype.CompilerData;
						foreach (System.CodeDom.CodeStatement s in mm.Statements)
						{
							if (s is CodeAssignStatement)
							{
								CodeAssignStatement cas = (CodeAssignStatement)s;
								if (cdata != null)
								{
									//setting Control.Location does not always work. change code here in such situations.
									//find this.<member>.Location = new System.Drawing.Point(x, y);
									if (cas.Left is CodePropertyReferenceExpression)
									{
										CodePropertyReferenceExpression cpr = (CodePropertyReferenceExpression)(cas.Left);
										if (string.CompareOrdinal(cpr.PropertyName, "Location") == 0)
										{
											if (cpr.TargetObject is CodeFieldReferenceExpression)
											{
												CodeFieldReferenceExpression mf = (CodeFieldReferenceExpression)(cpr.TargetObject);
												if (mf.TargetObject is CodeThisReferenceExpression && cdata.ContainsKey(mf.FieldName))
												{
													Dictionary<string, object> d = cdata[mf.FieldName];
													if (d.ContainsKey("Location"))
													{
														object v = d["Location"];
														if (v is System.Drawing.Point)
														{
															System.Drawing.Point pt = (System.Drawing.Point)v;
															if (cas.Right is CodeObjectCreateExpression)
															{
																CodeObjectCreateExpression oc = (CodeObjectCreateExpression)(cas.Right);
																if (oc.CreateType.BaseType.IndexOf("Point") >= 0)
																{
																	if (oc.Parameters != null && oc.Parameters.Count == 2)
																	{
																		oc.Parameters[0] = new CodePrimitiveExpression(pt.X);
																		oc.Parameters[1] = new CodePrimitiveExpression(pt.Y);
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
							else if (s is CodeVariableDeclarationStatement)
							{
								CodeVariableDeclarationStatement vd = (CodeVariableDeclarationStatement)s;
								//detect the resource usage so that the resources will be compiled into the binary
								if (string.CompareOrdinal(vd.Type.BaseType, "System.ComponentModel.ComponentResourceManager") == 0)
								{
									_useResources = true;
								}
							}
						}
					}
				}
			}
			foreach (object v in lstDup)
			{
				td.Members.Remove((CodeTypeMember)v);
			}
			foreach (CodeMemberField mf in classCreations)
			{
				td.Members.Insert(0, mf);
			}
			//==finish adjust derivation=================================================
		}
		private void adjustForRemotingService()
		{
			RemotingHost rh = _rootObject as RemotingHost;
			if (rh == null)
			{
				throw new DesignerException("RemotingHost is not the root object for class {0}", _rootClassId.ClassId);
			}
			//create interface
			string strInterface = rh.ContractInterfaceName;
			CodeTypeDeclaration rhInterface = new CodeTypeDeclaration(strInterface);
			ns.Types.Add(rhInterface);
			rhInterface.IsInterface = true;
			rhInterface.Attributes = MemberAttributes.Public;
			rhInterface.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(ServiceContractAttribute))));
			Dictionary<string, MethodClass> methodList = _rootClassId.CustomMethods;
			if (methodList != null && methodList.Count > 0)
			{
				foreach (MethodClass mc in methodList.Values)
				{
					if (mc.DoNotCompile)
					{
						continue;
					}
					if (mc.IsRemotingServiceMethod)
					{
						CodeMemberMethod mm = createCodeDomMethod(mc, false, true);
						rhInterface.Members.Add(mm);
					}
				}
			}
			//add interface to td
			td.BaseTypes.Add(strInterface);
			//override ContractInterface
			CodeMemberProperty cmp = new CodeMemberProperty();
			cmp.Attributes = MemberAttributes.Family | MemberAttributes.Override;
			cmp.HasGet = true;
			cmp.HasSet = false;
			cmp.Name = "ContractInterface";
			cmp.Type = new CodeTypeReference(typeof(Type));
			td.Members.Add(cmp);
			cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeTypeOfExpression(strInterface)));
		}
		private void adjustForWebService()
		{
			CodeMemberMethod initMM = getInitializeMethod();
			if (initMM != null)
			{
				List<CodeStatement> list = new List<CodeStatement>();
				string resourceName = null;
				for (int i = 0; i < initMM.Statements.Count; i++)
				{
					if (string.IsNullOrEmpty(resourceName))
					{
						CodeVariableDeclarationStatement vd = initMM.Statements[i] as CodeVariableDeclarationStatement;
						if (vd != null)
						{
							if (string.Compare(vd.Type.BaseType, "System.ComponentModel.ComponentResourceManager", StringComparison.Ordinal) == 0)
							{
								resourceName = vd.Name;
								list.Add(vd);
							}
						}
					}
					else
					{
						CodeAssignStatement cas = initMM.Statements[i] as CodeAssignStatement;
						if (cas != null)
						{
							CodeCastExpression ce = cas.Right as CodeCastExpression;
							if (ce != null)
							{
								CodeMethodInvokeExpression mie = ce.Expression as CodeMethodInvokeExpression;
								if (mie != null)
								{
									CodeVariableReferenceExpression vr = mie.Method.TargetObject as CodeVariableReferenceExpression;
									if (vr != null)
									{
										if (string.CompareOrdinal(vr.VariableName, resourceName) == 0)
										{
											list.Add(initMM.Statements[i]);
										}
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement cs in list)
				{
					initMM.Statements.Remove(cs);
				}
			}
		}
		private void addResourceMap()
		{
			Dictionary<IProperty, UInt32> map = _rootClassId.ResourceMap;
			if (map != null && map.Count > 0)
			{
				ProjectResources rm = _project.GetProjectSingleData<ProjectResources>();
				if (rm.HasResources)
				{
					if (_isWebPage)
					{
						IList<string> lnames = XmlUtil.GetLanguages(_project.ResourcesXmlNode);
						if (lnames != null && lnames.Count > 0)
						{
							List<WebResourceFile> files = new List<WebResourceFile>();
							StringCollection scData;
							Dictionary<IProperty, UInt32>.Enumerator en;
							ResourcePointer rp;
							string sv;
							Dictionary<string, StringCollection> data = new Dictionary<string, StringCollection>();
							en = map.GetEnumerator();
							while (en.MoveNext())
							{
								rp = rm.GetResourcePointerById(en.Current.Value);
								if (rp != null)
								{
									if (!data.TryGetValue(string.Empty, out scData))
									{
										scData = new StringCollection();
										data.Add(string.Empty, scData);
									}
									string svDef = rp.GetResourceString(string.Empty);
									if (svDef == null)
									{
										svDef = string.Empty;
									}
									if (rp.IsFile)
									{
										if (!string.IsNullOrEmpty(svDef))
										{
											bool fexist = false;
											if (File.Exists(svDef))
											{
												fexist = true;
											}
											else
											{
												string f = Path.Combine(rm.ResourcesFolder, svDef);
												if (File.Exists(f))
												{
													svDef = f;
													fexist = true;
												}
											}
											if (fexist)
											{
												bool b;
												WebResourceFile wf = new WebResourceFile(svDef, rp.WebFolderName, out b);
												if (b)
												{
													svDef = wf.ResourceFile;
												}
												svDef = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", rp.WebFolderName, Path.GetFileName(svDef));
												files.Add(wf);
											}
										}
									}
									scData.Add(svDef.Replace("'", "\'"));
									foreach (string s in lnames)
									{
										if (!data.TryGetValue(s, out scData))
										{
											scData = new StringCollection();
											data.Add(s, scData);
										}
										sv = rp.GetResourceString(s);
										if (rp.IsFile)
										{
											if (!string.IsNullOrEmpty(sv))
											{
												bool fexist = false;
												if (File.Exists(sv))
												{
													fexist = true;
												}
												else
												{
													string f = Path.Combine(rm.ResourcesFolder, sv);
													if (File.Exists(f))
													{
														sv = f;
														fexist = true;
													}
												}
												if (fexist)
												{
													bool b;
													WebResourceFile wf = new WebResourceFile(sv, s, out b);
													if (b)
													{
														sv = wf.ResourceFile;
													}
													sv = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", s, Path.GetFileName(sv));
													files.Add(wf);
												}
											}
										}
										if (string.IsNullOrEmpty(sv))
										{
											sv = svDef;
										}
										scData.Add(sv.Replace("'", "\'"));
									}
								}
							}
							//create default js in libjs
							scData = data[string.Empty];
							savePageResourcs("", scData);
							foreach (string s in lnames)
							{
								scData = data[s];
								savePageResourcs(s, scData);
							}
							copyWebResourceFiles(files);
						}
					}
					else
					{
						bool isConsoleAppClass = (_project.ProjectType == EnumProjectType.Console && IsEntryPoint);
						MethodClass mc = new MethodClass(_rootClassId);
						mc.SetCompilerData(this);
						mc.MethodID = (UInt32)(Guid.NewGuid().GetHashCode());
						mc.Parameters = new List<ParameterClass>();
						ParameterClass p1 = new ParameterClass(typeof(object), "sender", mc);
						ParameterClass p2 = new ParameterClass(typeof(EventArgs), "e", mc);
						mc.Parameters.Add(p1);
						mc.Parameters.Add(p2);
						mc.Name = string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"res{0}", mc.MethodID.ToString("x"));
						CodeMemberMethod eh = new CodeMemberMethod();
						eh.Name = mc.Name;
						mc.MethodCode = eh;
						//
						td.Members.Add(eh);
						eh.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "sender"));
						eh.Parameters.Add(new CodeParameterDeclarationExpression(typeof(EventArgs), "e"));
						//get the method for adding event-handlder assignment code
						CodeMemberMethod cmm;
						//
						if (isConsoleAppClass)
						{
							cmm = getMainMethod();
						}
						else
						{
							CodeMemberMethod initMethod = getInitializeMethod();
							if (initMethod == null)
							{
								throw new DesignerException("InitializeComponent not found");
							}
							cmm = initMethod;
						}
						//===add property assignments==================
						Dictionary<IProperty, UInt32>.Enumerator en = map.GetEnumerator();
						Dictionary<UInt32, CodeExpression> values = new Dictionary<uint, CodeExpression>();
						while (en.MoveNext())
						{
							CodeExpression left = en.Current.Key.GetReferenceCode(mc, eh.Statements, false);
							CodeExpression right = null;
							if (!values.TryGetValue(en.Current.Value, out right))
							{
								ResourcePointer rp = rm.GetResourcePointerById(en.Current.Value);
								if (rp != null)
								{
									right = new CodePropertyReferenceExpression(
										new CodeTypeReferenceExpression(rm.ClassName),
										rp.CodeName);
									values.Add(en.Current.Value, right);
								}
							}
							if (right != null)
							{
								CodeAssignStatement cas = new CodeAssignStatement(left, right);
								eh.Statements.Add(cas);
							}
						}
						//=============================================
						if (isConsoleAppClass)
						{
							//for the consolde application class, directly add the method call 
							cmm.Statements.AddRange(eh.Statements);
							td.Members.Remove(eh);
						}
						else
						{
							//attach event
							CodeExpression methodTarget;
							CodeEventReferenceExpression ceRef = new CodeEventReferenceExpression(new CodeTypeReferenceExpression(rm.HelpClassName), "ProjectCultureChanged");
							if (_rootClassId.IsStatic)
							{
								methodTarget = new CodeTypeReferenceExpression(_rootClassId.Name);
							}
							else
							{
								methodTarget = new CodeThisReferenceExpression();
							}
							//attached method mc to the event
							//find the last assignment as the insertion point
							int k = cmm.Statements.Count - 1;
							CodeFieldReferenceExpression fr = ceRef.TargetObject as CodeFieldReferenceExpression;
							if (fr != null)
							{
								for (int i = 0; i < cmm.Statements.Count; i++)
								{
									CodeAssignStatement cas = cmm.Statements[i] as CodeAssignStatement;
									if (cas != null)
									{
										if (cas.Left == ceRef.TargetObject)
										{
											k = i;
										}
										else
										{
											CodePropertyReferenceExpression pr = cas.Left as CodePropertyReferenceExpression;
											if (pr != null)
											{
												if (pr.TargetObject == ceRef.TargetObject)
												{
													k = i;
												}
												else
												{
													CodeFieldReferenceExpression fr2 = pr.TargetObject as CodeFieldReferenceExpression;
													if (fr2 != null)
													{
														if (fr.FieldName == fr2.FieldName)
														{
															k = i;
														}
													}
												}
											}
										}
									}
								}
							}
							cmm.Statements.Insert(k++, new CodeAttachEventStatement(ceRef,
								new CodeDelegateCreateExpression(new CodeTypeReference(typeof(EventHandler)),
									methodTarget, eh.Name)));
							//call event handler
							CodeExpressionStatement ces = new CodeExpressionStatement();
							CodeMethodInvokeExpression cme = new CodeMethodInvokeExpression();
							ces.Expression = cme;
							cme.Method = new CodeMethodReferenceExpression();
							cme.Method.MethodName = eh.Name;
							if (_rootClassId.IsStatic)
							{
								cme.Method.TargetObject = new CodeTypeReferenceExpression(_rootClassId.Name);
								cme.Parameters.Add(new CodePrimitiveExpression(null));
							}
							else
							{
								cme.Method.TargetObject = new CodeThisReferenceExpression();
								cme.Parameters.Add(new CodeThisReferenceExpression());
							}
							cme.Parameters.Add(new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(EventArgs)), "Empty"));
							cmm.Statements.Insert(k++, ces);
						}
					}
				}
			}
		}
		private void supportICustomEventMethodDescriptor()
		{
			CodeMemberMethod initMM = getInitializeMethod();
			if (initMM != null)
			{
				//find ICustomEventMethodDescriptor objects
				Dictionary<string, ICustomEventMethodDescriptor> names = new Dictionary<string, ICustomEventMethodDescriptor>();
				foreach (object v in _objMap.Keys)
				{
					ICustomEventMethodDescriptor ce = v as ICustomEventMethodDescriptor;
					if (ce != null)
					{
						names.Add(((IComponent)ce).Site.Name, ce);
					}
				}
				if (names.Count > 0)
				{
					for (int i = 0; i < initMM.Statements.Count; i++)
					{
						CodeAttachEventStatement ae = initMM.Statements[i] as CodeAttachEventStatement;
						if (ae != null)
						{
							CodeFieldReferenceExpression fr = ae.Event.TargetObject as CodeFieldReferenceExpression;
							if (fr != null)
							{
								ICustomEventMethodDescriptor ce;
								if (names.TryGetValue(fr.FieldName, out ce))
								{
									if (ce.IsCustomEvent(ae.Event.EventName))
									{
										CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(ae.Event.TargetObject,
											"GetEventHolder",
											new CodePrimitiveExpression(ae.Event.EventName));
										ae.Event.EventName = "Event";
										ae.Event.TargetObject = mie;
									}
								}
							}
						}
					}
				}
			}
		}
		private void processComponentReferenceHolder(IComponentReferenceHolder crh, CodeMemberMethod initMM)
		{
			string[] sns = crh.GetComponentReferenceNames();
			IComponent ic = crh as IComponent;
			CodeExpressionStatement ces = new CodeExpressionStatement();
			CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
			ces.Expression = cmie;
			cmie.Method = new CodeMethodReferenceExpression();
			cmie.Method.MethodName = "SetComponentReferences";
			cmie.Method.TargetObject = new CodeCastExpression(typeof(IComponentReferenceHolder), new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), ic.Site.Name));
			CodeArrayCreateExpression cace = new CodeArrayCreateExpression();
			cmie.Parameters.Add(cace);
			cace.CreateType = new CodeTypeReference(typeof(object));
			for (int i = 0; i < sns.Length; i++)
			{
				cace.Initializers.Add(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), sns[i]));
			}
			//
			int nIdx = -1;
			for (int i = 0; i < initMM.Statements.Count; i++)
			{
				CodeAssignStatement cas = initMM.Statements[i] as CodeAssignStatement;
				if (cas != null)
				{
					CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
					if (cpre != null)
					{
						if (string.CompareOrdinal(cpre.PropertyName, "ComponentReferenceNames") == 0)
						{
							CodeFieldReferenceExpression cfrf = cpre.TargetObject as CodeFieldReferenceExpression;
							if (cfrf != null)
							{
								if (cfrf.TargetObject is CodeThisReferenceExpression)
								{
									if (string.CompareOrdinal(cfrf.FieldName, ic.Site.Name) == 0)
									{
										nIdx = i;
										break;
									}
								}
							}
						}
					}
				}
			}
			if (nIdx >= 0)
			{
				initMM.Statements.RemoveAt(nIdx);
				initMM.Statements.Insert(nIdx, ces);
			}
			else
			{
				initMM.Statements.Add(ces);
			}
		}
		private void processSerializeNotify(ISerializeNotify sn, CodeMemberMethod initMM)
		{
			IComponent ic = sn as IComponent;
			if (ic != null && ic.Site != null)
			{
				int nStartIndex = -1;
				int nEndIndex = -1;
				for (int i = 0; i < initMM.Statements.Count; i++)
				{
					CodeAssignStatement cas = initMM.Statements[i] as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
						if (cpre != null)
						{
							CodeFieldReferenceExpression cfre = cpre.TargetObject as CodeFieldReferenceExpression;
							if (cfre != null)
							{
								if (cfre.TargetObject is CodeThisReferenceExpression)
								{
									if (string.CompareOrdinal(cfre.FieldName, ic.Site.Name) == 0)
									{
										if (nStartIndex < 0)
										{
											nStartIndex = i;
										}
									}
									else
									{
										if (nStartIndex >= 0)
										{
											nEndIndex = i;
											break;
										}
									}
								}
								else
								{
									if (nStartIndex >= 0)
									{
										nEndIndex = i;
										break;
									}
								}
							}
							else
							{
								if (nStartIndex >= 0)
								{
									nEndIndex = i;
									break;
								}
							}
						}
						else
						{
							if (nStartIndex >= 0)
							{
								nEndIndex = i;
								break;
							}
						}
					}
					else
					{
						if (nStartIndex >= 0)
						{
							nEndIndex = i;
							break;
						}
					}
				}
				if (nStartIndex >= 0)
				{
					if (nEndIndex < 0)
					{
						nEndIndex = nStartIndex;
					}
					CodeAssignStatement cas = new CodeAssignStatement();
					CodePropertyReferenceExpression cpre = new CodePropertyReferenceExpression();
					cas.Left = cpre;
					CodeFieldReferenceExpression cfre = new CodeFieldReferenceExpression();
					CodeCastExpression cce = new CodeCastExpression(typeof(ISerializeNotify), cfre);
					cpre.TargetObject = cce;
					cpre.PropertyName = "ReadingProperties";
					cpre.TargetObject = cfre;
					cfre.TargetObject = new CodeThisReferenceExpression();
					cfre.FieldName = ic.Site.Name;
					cas.Right = new CodePrimitiveExpression(true);
					initMM.Statements.Insert(nStartIndex, cas);
					nEndIndex++;
					cas = new CodeAssignStatement();
					cpre = new CodePropertyReferenceExpression();
					cas.Left = cpre;
					cfre = new CodeFieldReferenceExpression();
					cce = new CodeCastExpression(typeof(ISerializeNotify), cfre);
					cpre.TargetObject = cce;
					cpre.PropertyName = "ReadingProperties";
					cpre.TargetObject = cfre;
					cfre.TargetObject = new CodeThisReferenceExpression();
					cfre.FieldName = ic.Site.Name;
					cas.Right = new CodePrimitiveExpression(false);
					initMM.Statements.Insert(nEndIndex + 1, cas);
				}
			}
		}
		private void databaseSupport()
		{
			Type tRoot = VPLUtil.GetObjectType(_rootObject);
			bool isGrid = typeof(DataGridView).IsAssignableFrom(tRoot);
			StringCollection datasetNames = new StringCollection();
			foreach (object v in _objMap.Keys)
			{
				EasyDataSet eds = v as EasyDataSet;
				if (eds != null)
				{
					datasetNames.Add(eds.Name);
				}
			}
			CodeMemberMethod initMM = getInitializeMethod();
			if (initMM != null)
			{
				List<CodeStatement> invalids = new List<CodeStatement>();
				//find all DataBindings.Add calls
				List<CodeStatement> bindCalls = new List<CodeStatement>();
				//find all Relations.AddRange
				List<CodeStatement> relationCalls = new List<CodeStatement>();
				List<CodeStatement> dataSourceCalls = new List<CodeStatement>();
				List<CodeStatement> dataMemberCalls = new List<CodeStatement>();
				List<CodeStatement> sqlQueryCalls = new List<CodeStatement>();
				foreach (CodeStatement cs in initMM.Statements)
				{
					CodeExpressionStatement es = cs as CodeExpressionStatement;
					if (es != null)
					{
						CodeMethodInvokeExpression mie = es.Expression as CodeMethodInvokeExpression;
						if (mie != null)
						{
							if (string.Compare(mie.Method.MethodName, "Add", StringComparison.Ordinal) == 0)
							{
								CodePropertyReferenceExpression pre = mie.Method.TargetObject as CodePropertyReferenceExpression;
								if (pre != null)
								{
									if (string.Compare(pre.PropertyName, "DataBindings", StringComparison.Ordinal) == 0)
									{
										bindCalls.Add(cs);
									}
								}
							}
							else if (string.Compare(mie.Method.MethodName, "AddRange", StringComparison.Ordinal) == 0)
							{
								CodePropertyReferenceExpression pre = mie.Method.TargetObject as CodePropertyReferenceExpression;
								if (pre != null)
								{
									if (string.Compare(pre.PropertyName, "Relations", StringComparison.Ordinal) == 0)
									{
										CodeFieldReferenceExpression fe = pre.TargetObject as CodeFieldReferenceExpression;
										if (fe != null)
										{
											if (fe.TargetObject is CodeThisReferenceExpression)
											{
												if (typeof(EasyDataSet).IsAssignableFrom(getFieldType(fe.FieldName)))
												{
													relationCalls.Add(cs);
												}
											}
										}
									}
								}
							}
						}
					}
					else
					{
						CodeAssignStatement cas = cs as CodeAssignStatement;
						if (cas != null)
						{
							CodePropertyReferenceExpression pre = cas.Left as CodePropertyReferenceExpression;
							if (pre != null)
							{
								if (pre.TargetObject is CodeThisReferenceExpression)
								{
									if (isGrid)
									{
										if (string.CompareOrdinal(pre.PropertyName, "FirstDisplayedCell") == 0 ||
											string.CompareOrdinal(pre.PropertyName, "CurrentCell") == 0)
										{
											invalids.Add(cas);
											continue;
										}
									}
								}
								if (string.Compare(pre.PropertyName, "DataSource", StringComparison.Ordinal) == 0)
								{
									dataSourceCalls.Add(cs);
								}
								else if (string.Compare(pre.PropertyName, "DisplayMember", StringComparison.Ordinal) == 0)
								{
									dataMemberCalls.Add(cs);
								}
								else if (string.Compare(pre.PropertyName, "ValueMember", StringComparison.Ordinal) == 0)
								{
									dataMemberCalls.Add(cs);
								}
								else if (string.Compare(pre.PropertyName, "DataMember", StringComparison.Ordinal) == 0)
								{
									dataMemberCalls.Add(cs);
								}
								else if (string.Compare(pre.PropertyName, "SqlQuery", StringComparison.Ordinal) == 0)
								{
									CodeFieldReferenceExpression fe = pre.TargetObject as CodeFieldReferenceExpression;
									if (fe != null)
									{
										if (fe.TargetObject is CodeThisReferenceExpression)
										{
											Type tp = getFieldType(fe.FieldName);
											if (tp != null)
											{
												if (typeof(EasyDataSet).IsAssignableFrom(tp) || typeof(EasyGrid).IsAssignableFrom(tp) || typeof(EasyQuery).IsAssignableFrom(tp))
												{
													sqlQueryCalls.Add(cs);
												}
											}
										}
									}
								}
							}
							else
							{
								CodeFieldReferenceExpression cfre = cas.Left as CodeFieldReferenceExpression;
								if (cfre != null)
								{
									if (cfre.TargetObject is CodeThisReferenceExpression)
									{
										if (isGrid)
										{
											if (string.CompareOrdinal(cfre.FieldName, "FirstDisplayedCell") == 0 ||
												string.CompareOrdinal(cfre.FieldName, "CurrentCell") == 0)
											{
												invalids.Add(cas);
												continue;
											}
										}
									}
								}
							}
						}
					}
				}
				foreach (CodeStatement cs in invalids)
				{
					initMM.Statements.Remove(cs);
				}
				foreach (CodeStatement cs in sqlQueryCalls)
				{
					initMM.Statements.Remove(cs);
				}
				foreach (CodeStatement cs in bindCalls)
				{
					initMM.Statements.Remove(cs);
				}
				foreach (CodeStatement cs in relationCalls)
				{
					initMM.Statements.Remove(cs);
				}
				foreach (CodeStatement cs in dataSourceCalls)
				{
					initMM.Statements.Remove(cs);
				}
				foreach (CodeStatement cs in dataMemberCalls)
				{
					initMM.Statements.Remove(cs);
				}
				//adjust FieldList ==============================================================
				//===add connection, Tables to EasyQuery 
				//find this.ResumeLayout(false)
				int k = 1;
				for (k = initMM.Statements.Count - 1; k >= 0; k--)
				{
					CodeExpressionStatement es = initMM.Statements[k] as CodeExpressionStatement;
					if (es != null)
					{
						CodeMethodInvokeExpression mie = es.Expression as CodeMethodInvokeExpression;
						if (mie != null)
						{
							if (string.Compare(mie.Method.MethodName, "ResumeLayout", StringComparison.Ordinal) == 0)
							{
								if (mie.Method.TargetObject is CodeThisReferenceExpression)
								{
									break;
								}
							}
						}
					}
				}
				if (k < 0)
				{
					//not found
					k = initMM.Statements.Count;
				}
				//find EasyQuery instances
				if (!_isWebPage)
				{
					foreach (object obj in _objMap.Keys)
					{
						IDatabaseAccess qry = obj as IDatabaseAccess;
						if (qry != null)
						{
							string codeName = ((IComponent)obj).Site.Name;
							CodeExpression target;
							if (qry != _rootObject)
							{
								target = new CodeVariableReferenceExpression(codeName);
							}
							else
							{
								target = new CodeThisReferenceExpression();
							}
							//create table and columns
							initMM.Statements.Insert(k++,
									new CodeExpressionStatement(
										new CodeMethodInvokeExpression(
											target,
											"CreateDataTable",
											new CodeExpression[] { }
											)
										)
									);
							if (qry != _rootObject)
							{
								//Call Query
								if (qry.QueryOnStart)
								{
									initMM.Statements.Insert(k++,
										new CodeExpressionStatement(
											new CodeMethodInvokeExpression(
												new CodeVariableReferenceExpression(codeName),
												"Query",
												new CodeExpression[] { }
												)
											)
										);
								}
							}
						}
					}
				}
				//add back all DataSource calls
				StringCollection adjustedNames = new StringCollection();
				foreach (CodeStatement cs in dataSourceCalls)
				{
					CodeAssignStatement cas = cs as CodeAssignStatement;
					if (cas != null)
					{
						CodeFieldReferenceExpression cfre = cas.Right as CodeFieldReferenceExpression;
						if (cfre != null)
						{
							if (datasetNames.Contains(cfre.FieldName))
							{
								cas.Right = new CodePropertyReferenceExpression(cfre, "DataBindingSource");
								CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
								if (cpre != null)
								{
									CodeFieldReferenceExpression acfre = cpre.TargetObject as CodeFieldReferenceExpression;
									if (acfre != null)
									{
										adjustedNames.Add(acfre.FieldName);
									}
								}
							}
						}
					}
					initMM.Statements.Insert(k++, cs);
				}
				//add back all DataMember calls
				foreach (CodeStatement cs in dataMemberCalls)
				{
					CodeAssignStatement cas = cs as CodeAssignStatement;
					if (cas != null)
					{
						CodePropertyReferenceExpression cpre = cas.Left as CodePropertyReferenceExpression;
						if (cpre != null)
						{
							CodeFieldReferenceExpression acfre = cpre.TargetObject as CodeFieldReferenceExpression;
							if (acfre != null)
							{
								if (_dataGridViewUsingEasyDataSet != null && _dataGridViewUsingEasyDataSet.Contains(acfre.FieldName))
								{
									continue;
								}
								if (adjustedNames.Contains(acfre.FieldName))
								{
									CodePrimitiveExpression cpe = cas.Right as CodePrimitiveExpression;
									if (cpe != null)
									{
										string s = cpe.Value as string;
										if (!string.IsNullOrEmpty(s))
										{
											int pos = s.LastIndexOf('.');
											if (pos > 0)
											{
												cas.Right = new CodePrimitiveExpression(s.Substring(pos + 1));
											}
										}
									}
								}
							}
						}
					}
					initMM.Statements.Insert(k++, cs);
				}
				Dictionary<string, ICustomDataSource> customDataSources = new Dictionary<string, ICustomDataSource>();
				foreach (object v in _objMap.Keys)
				{
					ICustomDataSource cds = v as ICustomDataSource;
					if (cds != null)
					{
						customDataSources.Add(((IComponent)v).Site.Name, cds);
					}
				}
				//add back all DataBindings.Add calls
				foreach (CodeStatement cs in bindCalls)
				{
					//adjust for EasyDataSet
					CodeExpressionStatement ces = cs as CodeExpressionStatement;
					if (ces != null)
					{
						CodeMethodInvokeExpression cmie = ces.Expression as CodeMethodInvokeExpression;
						if (cmie != null)
						{
							if (string.CompareOrdinal(cmie.Method.MethodName, "Add") == 0)
							{
								if (cmie.Parameters.Count == 1)
								{
									CodeObjectCreateExpression oce = cmie.Parameters[0] as CodeObjectCreateExpression;
									if (oce != null)
									{
										if (string.CompareOrdinal(oce.CreateType.BaseType, "System.Windows.Forms.Binding") == 0)
										{
											if (oce.Parameters.Count == 4)
											{
												CodeFieldReferenceExpression fre = oce.Parameters[1] as CodeFieldReferenceExpression;
												if (fre != null && fre.TargetObject is CodeThisReferenceExpression)
												{
													string dbName;
													if (customDataSources.ContainsKey(fre.FieldName))
													{
														dbName = customDataSources[fre.FieldName].DataBindingPropertyName;
													}
													else
													{
														if (typeof(EasyDataSet).IsAssignableFrom(getFieldType(fre.FieldName)))
														{
															dbName = "DataBindingSource";
														}
														else
														{
															dbName = null;
														}
													}
													if (dbName != null)
													{
														oce.Parameters[1] = new CodePropertyReferenceExpression(fre, dbName);
														CodePrimitiveExpression cpe = oce.Parameters[2] as CodePrimitiveExpression;
														if (cpe != null)
														{
															string s = cpe.Value as string;
															if (!string.IsNullOrEmpty(s))
															{
																int nPos = s.IndexOf('.');
																if (nPos > 0)
																{
																	cpe.Value = s.Substring(nPos + 1);
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					initMM.Statements.Insert(k++, cs);
				}
			}
		}
		/// <summary>
		/// generate customer properties
		/// </summary>
		private void createProperties()
		{
			if (_project.ProjectType == EnumProjectType.WebAppPhp)
			{
				return;
			}
			Dictionary<string, PropertyClass> properties = _rootClassId.CustomProperties;
			foreach (PropertyClass prop in properties.Values)
			{
				if (prop.DoNotCompile)
				{
					continue;
				}
				if (_project.ProjectType == EnumProjectType.WebAppAspx)
				{
					if (prop.RunAt == EnumWebRunAt.Client)
					{
						continue;
					}
				}
				createPropertyInAssembly(prop);
			}
		}
		private void generateWebAppClientFile()
		{
			StringCollection jscode = new StringCollection();
			Dictionary<string, PropertyClass> properties = _rootClassId.CustomProperties;
			foreach (PropertyClass prop in properties.Values)
			{
				if (prop.DoNotCompile)
				{
					continue;
				}
				if (prop.RunAt == EnumWebRunAt.Client)
				{
					createWebClientProperty(jscode, prop);
				}
			}
			Dictionary<string, MethodClass> methodList = _rootClassId.CustomMethods;
			if (methodList != null && methodList.Count > 0)
			{
				foreach (MethodClass mc in methodList.Values)
				{
					if (mc.DoNotCompile)
					{
						continue;
					}
					if (mc.RunAt == EnumWebRunAt.Client)
					{
						if (_customClientMethodFormSubmissions == null)
						{
							_customClientMethodFormSubmissions = new Dictionary<string, Dictionary<string, StringCollection>>();
						}
						Dictionary<string, StringCollection> formsumissions;
						if (_customClientMethodFormSubmissions.TryGetValue(mc.Name, out formsumissions))
						{
							formsumissions.Clear();
						}
						else
						{
							formsumissions = new Dictionary<string, StringCollection>();
							_customClientMethodFormSubmissions.Add(mc.Name, formsumissions);
						}
						createWebClientMethod(jscode, mc, formsumissions);
					}
				}
			}
			string js = Path.Combine(_classCompiler.WebJsFolder, JsFile);
			StreamWriter sw = null;
			try
			{
				sw = new StreamWriter(js, false, Encoding.UTF8);
				sw.Write("var ");
				sw.Write(_rootClassId.CodeName);
				sw.WriteLine("={};");
				foreach (string s in jscode)
				{
					sw.Write(s);
				}
			}
			catch (Exception err)
			{
				logException(err);
			}
			finally
			{
				if (sw != null)
				{
					sw.Close();
				}
			}
		}
		private void createWebPageProperties(StringCollection pageScript)
		{
			Dictionary<string, PropertyClass> properties = _rootClassId.CustomProperties;
			Dictionary<string, string> propMap = new Dictionary<string, string>();
			foreach (PropertyClass prop in properties.Values)
			{
				if (prop.DoNotCompile)
				{
					continue;
				}
				if (prop.RunAt == EnumWebRunAt.Client)
				{
					createWebClientProperty(pageScript, prop);
					propMap.Add(prop.Name, CompilerUtil.CreateJsCodeName(prop, 0));
				}
			}
			if (propMap.Count > 0)
			{
				pageScript.Add(Indentation.GetIndent());
				pageScript.Add("limnorPage.customprops = [];\r\n");
				foreach (KeyValuePair<string, string> kv in propMap)
				{
					pageScript.Add(Indentation.GetIndent());
					pageScript.Add(string.Format(CultureInfo.InvariantCulture, "limnorPage.customprops.push({{name:'{0}',code:'{1}'}});\r\n", kv.Key, kv.Value));
				}
			}
		}
		private void createWebProperties(StringCollection jsCode, ServerScriptHolder serverCode)
		{
			Dictionary<string, PropertyClass> properties = _rootClassId.CustomProperties;
			foreach (PropertyClass prop in properties.Values)
			{
				if (prop.DoNotCompile)
				{
					continue;
				}
				if (prop.RunAt == EnumWebRunAt.Client)
				{
				}
				else
				{
					if (_project.ProjectType == EnumProjectType.WebAppAspx)
					{
						VPLUtil.CompilerContext_ASPX = true;
						createPropertyInAssembly(prop);
						VPLUtil.CompilerContext_ASPX = false;
					}
					else if (_project.ProjectType == EnumProjectType.WebAppPhp)
					{
						createPropertyPhp(serverCode.Methods, prop);
					}
				}
			}
		}
		/// <summary>
		/// process custom methods
		/// </summary>
		/// <param name="jsCode">javascripts</param>
		private void createWebMethods(StringCollection jsCode, ServerScriptHolder serverCode)
		{
			bool isEntryPoint = IsEntryPoint;
			Dictionary<string, MethodClass> methodList = _rootClassId.CustomMethods;
			if (methodList != null && methodList.Count > 0)
			{
				TraceLogClass.TraceLog.Trace("Methods to be created:{0}", methodList.Count);
				foreach (MethodClass mc in methodList.Values)
				{
					if (mc.DoNotCompile)
					{
						continue;
					}
					if (mc.RunAt == EnumWebRunAt.Client)
					{
						if (_customClientMethodFormSubmissions == null)
						{
							_customClientMethodFormSubmissions = new Dictionary<string, Dictionary<string, StringCollection>>();
						}
						Dictionary<string, StringCollection> formsumissions;
						if (_customClientMethodFormSubmissions.TryGetValue(mc.Name, out formsumissions))
						{
							formsumissions.Clear();
						}
						else
						{
							formsumissions = new Dictionary<string, StringCollection>();
							_customClientMethodFormSubmissions.Add(mc.Name, formsumissions);
						}
						createWebClientMethod(jsCode, mc, formsumissions);
					}
					else if (mc.RunAt == EnumWebRunAt.Server)
					{
						if (_project.ProjectType == EnumProjectType.WebAppAspx)
						{
							VPLUtil.CompilerContext_ASPX = true;
							createMethodInAssembly(mc, true);
							VPLUtil.CompilerContext_ASPX = false;
						}
						else if (_project.ProjectType == EnumProjectType.WebAppPhp)
						{
							createMethodPhp(serverCode.Methods, mc);
						}
					}
				}
			}
			else
			{
				TraceLogClass.TraceLog.Trace("No methods to be created");
			}
			if (_project.ProjectType == EnumProjectType.WebAppPhp)
			{
				foreach (object o in _objMap.Keys)
				{
					IWebServerUser wsu = o as IWebServerUser;
					if (wsu != null)
					{
						wsu.OnGeneratePhpCode(jsCode, serverCode.Methods, serverCode.OnRequestExecution);
					}
				}
			}
		}
		private CodeMemberMethod createCodeDomMethod(MethodClass mc, bool isEntryPoint, bool includeOperationContract)
		{
			CodeMemberMethod mm = new CodeMemberMethod();
			mc.MethodCode = mm;
			mc.TypeDeclaration = td;
			mm.Name = mc.Name;
			CompilerUtil.SetReturnType(mm, mc.ReturnValue);
			//
			CompilerUtil.AddAttribute(mc, mm.CustomAttributes, _isWebService, includeOperationContract, _classCompiler.RootNamespace);
			mm.Comments.Add(new CodeCommentStatement("<summary>", true));
			if (!string.IsNullOrEmpty(mc.Description))
			{
				mm.Comments.Add(new CodeCommentStatement(mc.Description, true));
			}
			mm.Comments.Add(new CodeCommentStatement("</summary>", true));
			if (isEntryPoint)
			{
				mm.Attributes = MemberAttributes.Static;
				if (string.CompareOrdinal(mm.Name, "Main") == 0)
				{
					LimnorApp app = _rootObject as LimnorApp;
					if (app.ThreadMode == EnumThreadMode.STAThread)
					{
						mm.CustomAttributes.Add(new CodeAttributeDeclaration("STAThread"));
					}
					else if (app.ThreadMode == EnumThreadMode.MTAThread)
					{
						mm.CustomAttributes.Add(new CodeAttributeDeclaration("MTAThread"));
					}
					mm.Attributes |= MemberAttributes.Private;
				}
				else
				{
					if (mc.AccessControl == ProgElements.EnumAccessControl.Private)
					{
						mm.Attributes |= MemberAttributes.Private;
					}
					else if (mc.AccessControl == ProgElements.EnumAccessControl.Protected)
					{
						mm.Attributes |= MemberAttributes.Family;
					}
					else
					{
						mm.Attributes |= MemberAttributes.Public;
					}
				}
			}
			else
			{
				if ((td.Attributes & MemberAttributes.Static) == MemberAttributes.Static)
					mm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				else
				{
					mm.Attributes = mc.MethodAttributes;
				}
			}
			mm.ReturnType = new CodeTypeReference(mc.ReturnValue.TypeString);
			//
			TraceLogClass.TraceLog.Trace("Generate {0} method parameter(s)", mc.ParameterCount);
			TraceLogClass.TraceLog.IndentIncrement();
			if (mc.Parameters != null)
			{
				for (int i = 0; i < mc.Parameters.Count; i++)
				{
					TraceLogClass.TraceLog.Trace("generate method parameter, type:'{0}', name:'{1}'", mc.Parameters[i].ParameterTypeString, mc.Parameters[i].Name);
					CodeParameterDeclarationExpression p = new CodeParameterDeclarationExpression(new CodeTypeReference(mc.Parameters[i].ParameterTypeString), mc.Parameters[i].Name);
					//<param name="sender"></param>
					mm.Comments.Add(new CodeCommentStatement(string.Format(
						System.Globalization.CultureInfo.InvariantCulture,
						"<param name=\"{0}\">{1}</param>",
						mc.Parameters[i].Name, mc.Parameters[i].Description), true));
					mm.Parameters.Add(p);
				}
			}
			return mm;
		}
		private void createMethodPhp(StringCollection phpCode, MethodClass mc)
		{
			EnumRunContext originContext = VPLUtil.CurrentRunContext;
			VPLUtil.CurrentRunContext = EnumRunContext.Server;
			try
			{
				bool bc = VPLUtil.CompilerContext_PHP;
				VPLUtil.CompilerContext_PHP = true;
				bool isEntryPoint = IsEntryPoint;
				TraceLogClass.TraceLog.IndentIncrement();
				bool b = xr.TraceEnabled;
				xr.EnableTrace(_project.TraceMethodLoading);
				xr.EnableTrace(b);
				TraceLogClass.TraceLog.Trace("Compile php method {0}", mc.Name);
				TraceLogClass.TraceLog.IndentIncrement();
				StringCollection methodcode = new StringCollection();
				Dictionary<string, StringCollection> formSubmissions = new Dictionary<string, StringCollection>();
				JsMethodCompiler jmc = new JsMethodCompiler(mc, _rootClassId, Debug, formSubmissions, true);
				mc.ExportPhpScriptCode(phpCode, methodcode, jmc);
				string[] ss = new string[methodcode.Count];
				methodcode.CopyTo(ss, 0);
				phpCode.AddRange(ss);
				TraceLogClass.TraceLog.IndentDecrement();
				VPLUtil.CompilerContext_PHP = bc;
			}
			finally
			{
				VPLUtil.CurrentRunContext = originContext;
			}
		}
		private void createPropertyInAssembly(PropertyClass prop)
		{
			EnumRunContext originContext = VPLUtil.CurrentRunContext;
			VPLUtil.CurrentRunContext = EnumRunContext.Server;
			try
			{
				CodeMemberProperty cp = new CodeMemberProperty();
				cp.Attributes = prop.PropertyAttributes;
				cp.Name = prop.Name;
				prop.SwitchPhpType();
				cp.Type = new CodeTypeReference(prop.PropertyType.TypeString);
				td.Members.Add(cp);
				InterfaceCustomProperty ifcp = prop as InterfaceCustomProperty;
				if (ifcp != null)
				{
					cp.PrivateImplementationType = new CodeTypeReference(ifcp.Interface.TypeString);
				}
				//
				CompilerUtil.AddAttribute(prop, cp.CustomAttributes, _isWebService, false, _classCompiler.RootNamespace);
				//
				if (!string.IsNullOrEmpty(prop.Description))
				{
					cp.Comments.Add(new CodeCommentStatement("<summary>", true));
					cp.Comments.Add(new CodeCommentStatement(prop.Description, true));
					cp.Comments.Add(new CodeCommentStatement("</summary>", true));
				}
				//
				if (prop.CanRead && prop.Getter != null)
				{
					TraceLogClass.TraceLog.Trace("Compile property getter {0}", prop.Name);
					CodeMemberMethod mm = new CodeMemberMethod();
					prop.Getter.TypeDeclaration = td;
					prop.Getter.MethodCode = mm;
					mm.Name = "Get" + prop.Name;
					mm.ReturnType = new CodeTypeReference(prop.PropertyType.TypeString);
					TraceLogClass.TraceLog.IndentDecrement();
					prop.Getter.ExportCode(this, mm, false);
					for (int i = 0; i < mm.Statements.Count; i++)
					{
						cp.GetStatements.Add(mm.Statements[i]);
					}
					TraceLogClass.TraceLog.IndentDecrement();
				}
				if (prop.CanWrite && prop.Setter != null)
				{
					TraceLogClass.TraceLog.Trace("Compile property setter {0}", prop.Name);
					CodeMemberMethod mm = new CodeMemberMethod();
					prop.Setter.TypeDeclaration = td;
					prop.Setter.MethodCode = mm;
					mm.Name = "Set" + prop.Name;
					mm.ReturnType = new CodeTypeReference(typeof(void));
					TraceLogClass.TraceLog.IndentDecrement();
					prop.Setter.ExportCode(this, mm, false);
					for (int i = 0; i < mm.Statements.Count; i++)
					{
						cp.SetStatements.Add(mm.Statements[i]);
					}
					TraceLogClass.TraceLog.IndentDecrement();
				}
			}
			finally
			{
				VPLUtil.CurrentRunContext = originContext;
			}
		}
		/// <summary>
		/// create Server method
		/// </summary>
		/// <param name="mc"></param>
		private void createMethodInAssembly(MethodClass mc, bool isForWeb)
		{
			EnumRunContext originContext = VPLUtil.CurrentRunContext;
			VPLUtil.CurrentRunContext = EnumRunContext.Server;
			try
			{
				bool isEntryPoint = IsEntryPoint;
				TraceLogClass.TraceLog.IndentIncrement();
				bool b = xr.TraceEnabled;
				xr.EnableTrace(_project.TraceMethodLoading);
				xr.EnableTrace(b);
				TraceLogClass.TraceLog.Trace("Compile method {0}", mc.Name);
				CodeMemberMethod mm = createCodeDomMethod(mc, isEntryPoint, false);
				TraceLogClass.TraceLog.IndentIncrement();
				mc.ExportCode(this, mm, isForWeb);
				td.Members.Add(mm);
				TraceLogClass.TraceLog.IndentDecrement();
			}
			finally
			{
				VPLUtil.CurrentRunContext = originContext;
			}
		}
		private void createMethods()
		{
			Dictionary<string, MethodClass> methodList = _rootClassId.CustomMethods;
			if (methodList != null && methodList.Count > 0)
			{
				TraceLogClass.TraceLog.Trace("Methods to be created:{0}", methodList.Count);
				foreach (MethodClass mc in methodList.Values)
				{
					if (mc.DoNotCompile)
					{
						continue;
					}
					if (mc.RunAt == EnumWebRunAt.Client)
					{
						continue;
					}
					createMethodInAssembly(mc, false);
				}
			}
			else
			{
				TraceLogClass.TraceLog.Trace("No methods to be created");
			}
		}
		private void createWebClientProperty(StringCollection jsCode, PropertyClass prop)
		{
			if (!string.IsNullOrEmpty(prop.Description))
			{
				jsCode.Add(string.Format(CultureInfo.InvariantCulture, "{0}//{1}\r\n", Indentation.GetIndent(), prop.Description));
			}
			if (IsWebApp)
			{
				jsCode.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1} = {2};\r\n", _rootClassId.CodeName, prop.CodeName, ObjectCreationCodeGen.ObjectCreateJavaScriptCode(prop.DefaultValue)));
			}
			else
			{
				if (prop.DefaultValue == null && prop.PropertyType != null)
				{
					if (prop.PropertyType.HasInterface("IJavascriptType"))
					{
						IJavascriptType js = Activator.CreateInstance(prop.PropertyType.BaseClassType) as IJavascriptType;
						string s = js.CreateDefaultObject();
						if (!string.IsNullOrEmpty(s))
						{
							jsCode.Add(string.Format(CultureInfo.InvariantCulture, "{0}JsonDataBinding.PageValues.{1} = {2};\r\n", Indentation.GetIndent(), prop.CodeName, s));
							return;
						}
					}
				}
				jsCode.Add(string.Format(CultureInfo.InvariantCulture, "{0}JsonDataBinding.PageValues.{1} = {2};\r\n", Indentation.GetIndent(), prop.CodeName, ObjectCreationCodeGen.ObjectCreateJavaScriptCode(prop.DefaultValue)));
			}
		}
		private void createPropertyPhp(StringCollection jsCode, PropertyClass prop)
		{
			jsCode.Add(string.Format(CultureInfo.InvariantCulture, "public ${0} = {1};\r\n", prop.CodeName, ObjectCreationCodeGen.ObjectCreatePhpScriptCode(prop.DefaultValue)));
		}
		/// <summary>
		/// compile Client method
		/// </summary>
		/// <param name="jsCode"></param>
		/// <param name="mc"></param>
		private void createWebClientMethod(StringCollection jsCode, MethodClass mc, Dictionary<string, StringCollection> formSubmissions)
		{
			bool bjs = VPLUtil.CompilerContext_JS;
			VPLUtil.CompilerContext_JS = true;
			EnumRunContext origContext = VPLUtil.CurrentRunContext;
			try
			{
				VPLUtil.CurrentRunContext = EnumRunContext.Client;
				TraceLogClass.TraceLog.IndentIncrement();
				bool b = xr.TraceEnabled;
				xr.EnableTrace(_project.TraceMethodLoading);
				xr.EnableTrace(b);
				TraceLogClass.TraceLog.Trace("Compile javascript method {0}", mc.Name);
				//
				StringCollection methodcode = new StringCollection();
				JsMethodCompiler jmc = new JsMethodCompiler(mc, _rootClassId, Debug, formSubmissions, false);
				//
				mc.ExportJavaScriptCode(jsCode, methodcode, jmc);
			}
			catch
			{
				throw;
			}
			finally
			{
				TraceLogClass.TraceLog.IndentDecrement();
				VPLUtil.CompilerContext_JS = bjs;
				VPLUtil.CurrentRunContext = origContext;
			}
		}
		private void createEvents()
		{
			Dictionary<string, EventClass> events = _rootClassId.CustomEvents;
			foreach (EventClass e in events.Values)
			{
				if (e.DoNotCompile)
				{
					continue;
				}
				CodeMemberEvent cme = new CodeMemberEvent();
				if (!string.IsNullOrEmpty(e.Description))
				{
					cme.Comments.Add(new CodeCommentStatement(e.Description));
				}
				cme.Attributes = e.EventAttributes;
				if (e.IsStatic || isStaticClass)
				{
					cme.Attributes |= MemberAttributes.Static;
					if (_staticEvents == null)
					{
						_staticEvents = new StringCollection();
					}
					_staticEvents.Add(e.Name);
				}
				cme.Type = new CodeTypeReference(e.EventHandlerType.TypeString);
				if (e.EventHandlerType.IsGenericType)
				{
					DataTypePointer[] args = e.EventHandlerType.TypeParameters;
					if (args != null)
					{
						for (int i = 0; i < args.Length; i++)
						{
							cme.Type.TypeArguments.Add(args[i].BaseClassType);
						}
					}
				}
				cme.Name = e.Name;
				td.Members.Add(cme);
			}
		}
		private Dictionary<string, string> _net4_fieldNameMapping;
		/// <summary>
		/// for .Net 4
		/// </summary>
		private void net4_adjustFieldNames()
		{
			_net4_fieldNameMapping = new Dictionary<string, string>();
			PropertyInfo[] pifs = _rootObject.GetType().GetProperties();
			StringCollection names = new StringCollection();
			if (pifs != null && pifs.Length > 0)
			{
				for (int i = 0; i < pifs.Length; i++)
				{
					names.Add(pifs[i].Name);
				}
			}
			for (int i = 0; i < td.Members.Count; i++)
			{
				CodeMemberField cf = td.Members[i] as CodeMemberField;
				if (cf != null)
				{
					if (names.Contains(cf.Name))
					{
						string newName = string.Format(CultureInfo.InvariantCulture, "{0}{1}", cf.Name, Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						_net4_fieldNameMapping.Add(cf.Name, newName);
						cf.Name = newName;
					}
				}
			}
			if (_net4_fieldNameMapping.Count > 0)
			{
				CodeMemberMethod init = getInitializeMethod();
				if (init != null)
				{
					for (int i = 0; i < init.Statements.Count; i++)
					{
						CodeAssignStatement cas = init.Statements[i] as CodeAssignStatement;
						if (cas != null)
						{
							CodeFieldReferenceExpression cfr = cas.Left as CodeFieldReferenceExpression;
							if (cfr == null)
							{
								CodePropertyReferenceExpression cpr = cas.Left as CodePropertyReferenceExpression;
								if (cpr != null)
								{
									cfr = cpr.TargetObject as CodeFieldReferenceExpression;
								}
							}
							if (cfr != null)
							{
								string newName;
								if (_net4_fieldNameMapping.TryGetValue(cfr.FieldName, out newName))
								{
									CodeObjectCreateExpression coc = cas.Right as CodeObjectCreateExpression;
									if (coc != null)
									{
										cfr.FieldName = newName;
									}
								}
							}
						}
					}
				}
			}
		}
		private void addDefaultInstance()
		{
			if (_rootClassId.ObjectInstance is IWebPage)
			{
				return;
			}
			object[] attrs = _objectType.GetCustomAttributes(typeof(UseDefaultInstanceAttribute), true);
			if (attrs != null && attrs.Length > 0)
			{
				for (int i = 0; i < attrs.Length; i++)
				{
					UseDefaultInstanceAttribute defatt = attrs[i] as UseDefaultInstanceAttribute;
					if (defatt != null)
					{
						string var = string.Format(CultureInfo.InvariantCulture, "_{0}", defatt.InstanceName);
						CodeMemberField mf = new CodeMemberField(_rootClassId.TypeString, var);
						mf.Attributes = MemberAttributes.Static | MemberAttributes.Private;
						td.Members.Add(mf);
						CodeMemberProperty mp = new CodeMemberProperty();
						mp.Attributes = MemberAttributes.Public | MemberAttributes.Static;
						mp.HasSet = false;
						mp.HasGet = true;
						mp.Type = new CodeTypeReference(_rootClassId.TypeString);
						mp.Name = defatt.InstanceName;
						CodeConditionStatement ccs = new CodeConditionStatement();
						CodeAssignStatement cas = new CodeAssignStatement(new CodeVariableReferenceExpression(var), new CodeObjectCreateExpression(_rootClassId.TypeString, new CodeExpression[] { }));
						ccs.TrueStatements.Add(cas);
						CodeBinaryOperatorExpression c0 = new CodeBinaryOperatorExpression();
						ccs.Condition = c0;
						c0.Left = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(var), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null));
						c0.Operator = CodeBinaryOperatorType.BooleanOr;
						CodeBinaryOperatorExpression c1 = new CodeBinaryOperatorExpression();
						c0.Right = c1;
						c1.Left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(var), "IsDisposed");
						c1.Operator = CodeBinaryOperatorType.BooleanOr;
						c1.Right = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(var), "Disposing");
						//
						if (_project.ProjectType == EnumProjectType.Kiosk)
						{
							CodeAssignStatement cas2 = new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(var), "Owner"), LimnorKioskApp.GetBackgroundFormRef(_classCompiler.AppName));// _builder.KioskBackground);
							ccs.TrueStatements.Add(cas2);
						}
						//
						mp.GetStatements.Add(ccs);
						mp.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(var)));
						td.Members.Add(mp);
						break;
					}
				}
			}
			if (typeof(DrawingPage).IsAssignableFrom(_rootClassId.BaseClassType))
			{
				DrawingPage dp = _rootClassId.ObjectInstance as DrawingPage;
				if (dp != null)
				{
					if (dp.LoginPageId != 0)
					{
						WebLoginManager wm;
						ClassPointer cp = _project.GetTypedData<ClassPointer>(dp.LoginPageId);
						if (cp == null)
						{
							cp = ClassPointer.CreateClassPointer(dp.LoginPageId, _project);
							if (cp != null)
							{
								cp.ObjectList.LoadObjects();
							}
							else
							{
								throw new DesignerException("Cannot create login page. Login pag:[{0}, {1}]. Protected page:[{2},{3}]", dp.LoginPageId, dp.Name, _rootClassId.ClassId, _rootClassId.Name);
							}
						}
						if (cp.ObjectList.Count == 0)
						{
							cp.ObjectList.LoadObjects();
						}
						foreach (object ov in cp.ObjectList.Keys)
						{
							wm = ov as WebLoginManager;
							if (wm != null)
							{
								CodeMemberMethod defC = getDefaultConstructor();
								CodeExpressionStatement ces = new CodeExpressionStatement();
								CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
								ces.Expression = cmie;
								cmie.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "SetLoginManagerID");
								cmie.Parameters.Add(new CodeObjectCreateExpression(typeof(Guid), new CodePrimitiveExpression(wm.ID.ToString("D"))));
								defC.Statements.Add(ces);
								//
								CodeMemberMethod cmm = new CodeMemberMethod();
								cmm.Attributes = MemberAttributes.Family | MemberAttributes.Override;
								cmm.Name = "OnShowUserLogonDialog";
								cmm.ReturnType = new CodeTypeReference(typeof(void));
								ces = new CodeExpressionStatement();
								cmie = new CodeMethodInvokeExpression();
								ces.Expression = cmie;
								cmie.Method = new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(cp.Name), "DefaultForm"), "ShowDialog");
								cmie.Parameters.Add(new CodeThisReferenceExpression());
								cmm.Statements.Add(ces);
								//
								td.Members.Add(cmm);
								break;
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// get parameterless constructor. If it does not exist then create it.
		/// </summary>
		/// <returns></returns>
		private CodeMemberMethod getDefaultConstructor()
		{
			return getConstructor(new List<ParameterClass>());
		}
		/// <summary>
		/// create constructor with the given parameters
		/// </summary>
		/// <param name="ps"></param>
		/// <returns></returns>
		private CodeMemberMethod getConstructor(List<ParameterClass> ps)
		{
			bool isStatic = this.isStaticClass;
			if (IsEntryPoint)
			{
				//entry point uses Main()
				return null;
			}
			CodeMemberMethod ret = null;
			//try to find it from existing constructors
			foreach (CodeTypeMember m in td.Members)
			{
				CodeMemberMethod mm = null;
				CodeParameterDeclarationExpressionCollection ps0 = null;
				if (isStatic)
				{
					CodeTypeConstructor tc = m as CodeTypeConstructor;
					if (tc != null)
					{
						ps0 = tc.Parameters;
						mm = tc;
					}
				}
				else
				{
					CodeConstructor c = m as CodeConstructor;
					if (c != null)
					{
						ps0 = c.Parameters;
						mm = c;
					}
				}
				if (parametersMatch(ps0, ps))
				{
					ret = mm;
					break;
				}
			}
			//not found then create it
			if (ret == null)
			{
				if (isStatic)
				{
					ret = new CodeTypeConstructor();
				}
				else
				{
					ret = new CodeConstructor();
					ret.Attributes = MemberAttributes.Public;
				}
				if (ps != null)
				{
					for (int i = 0; i < ps.Count; i++)
					{
						ret.Parameters.Add(new CodeParameterDeclarationExpression(ps[i].TypeString, ps[i].Name));
					}
				}
				//call InitializeComponent function in this constructor
				CodeMemberMethod init = getInitializeMethod();
				if (init != null)
				{
					//ClassPointer.GetReferenceCode is not using the first parameter and it is OK to pass null
					ret.Statements.Add(new CodeExpressionStatement(
						new CodeMethodInvokeExpression(_rootClassId.GetReferenceCode(null, ret.Statements, true), init.Name)));
				}
				td.Members.Add(ret);
			}
			else
			{
				//call InitializeComponent function in this constructor
				CodeMemberMethod init = getInitializeMethod();
				if (init != null)
				{
					bool found = false;
					for (int i = 0; i < ret.Statements.Count; i++)
					{
						CodeExpressionStatement ces = ret.Statements[i] as CodeExpressionStatement;
						if (ces != null)
						{
							CodeMethodInvokeExpression cmie = ces.Expression as CodeMethodInvokeExpression;
							if (cmie != null)
							{
								if (string.CompareOrdinal("InitializeComponent", cmie.Method.MethodName) == 0)
								{
									if (cmie.Method.TargetObject == null ||
										cmie.Method.TargetObject is CodeThisReferenceExpression ||
										cmie.Method.TargetObject is CodeTypeReferenceExpression)
									{
										found = true;
										break;
									}
								}
							}
						}
					}
					if (!found)
					{
						ret.Statements.Add(new CodeExpressionStatement(
						new CodeMethodInvokeExpression(_rootClassId.GetReferenceCode(null, ret.Statements, true), init.Name)));
					}
				}
			}
			return ret;
		}
		private void removeComponentPointerFromWinAppInit()
		{
			LimnorWinApp app = _rootObject as LimnorWinApp;
			if (app == null)
			{
				return;
			}
			CodeMemberMethod initMethod = getInitializeMethod();
			List<CodeStatement> sts = new List<CodeStatement>();
			string name = null;
			for (int i = 0; i < initMethod.Statements.Count; i++)
			{
				CodeVariableDeclarationStatement vd = initMethod.Statements[i] as CodeVariableDeclarationStatement;
				if (vd != null)
				{
					if (string.Compare(vd.Type.BaseType, typeof(ComponentPointer).FullName, StringComparison.Ordinal) == 0)
					{
						sts.Add(vd);
						name = vd.Name;
						continue;
					}
				}
				CodeAssignStatement cas = initMethod.Statements[i] as CodeAssignStatement;
				if (cas != null)
				{
					CodePropertyReferenceExpression pe = cas.Left as CodePropertyReferenceExpression;
					if (pe != null)
					{
						if (pe.TargetObject == null && string.CompareOrdinal(pe.PropertyName, "StartClassId") == 0)
						{
							sts.Add(cas);
							continue;
						}
						CodeVariableReferenceExpression re = pe.TargetObject as CodeVariableReferenceExpression;
						if (re != null)
						{
							if (string.Compare(name, re.VariableName, StringComparison.Ordinal) == 0)
							{
								sts.Add(cas);
								continue;
							}
						}
						else
						{
							CodeFieldReferenceExpression pe0 = pe.TargetObject as CodeFieldReferenceExpression;
							if (pe0 != null)
							{
								if (pe0.TargetObject is CodeThisReferenceExpression)
								{
									if (app.StartPage != null)
									{
										if (string.Compare(pe0.FieldName, app.StartPage.CodeName, StringComparison.Ordinal) == 0)
										{
											if (string.Compare(pe.PropertyName, "ClientSize", StringComparison.Ordinal) == 0)
											{
												sts.Add(cas);
												continue;
											}
										}
										pe.TargetObject = new CodeVariableReferenceExpression(pe0.FieldName);
									}
									continue;
								}
							}
						}
					}
					CodeVariableReferenceExpression vre = cas.Right as CodeVariableReferenceExpression;
					if (vre != null)
					{
						if (string.Compare(name, vre.VariableName, StringComparison.Ordinal) == 0)
						{
							sts.Add(cas);
							continue;
						}
					}
					CodeFieldReferenceExpression fr = cas.Left as CodeFieldReferenceExpression;
					if (fr != null)
					{
						if (fr.TargetObject is CodeThisReferenceExpression)
						{
							cas.Left = new CodeVariableReferenceExpression(fr.FieldName);
							continue;
						}
					}
				}
			}
			foreach (CodeStatement st in sts)
			{
				initMethod.Statements.Remove(st);
			}
		}
		private bool parametersMatch(CodeParameterDeclarationExpressionCollection ps0, List<ParameterClass> ps)
		{
			int n = 0;
			if (ps0 == null && ps == null)
			{
				return true;
			}
			if (ps0 == null || ps == null)
			{
				return false;
			}
			n = ps0.Count;
			if (ps.Count != n)
			{
				return false;
			}
			for (int i = 0; i < n; i++)
			{
				if (string.CompareOrdinal(ps0[i].Type.BaseType, ps[i].TypeString) != 0)
				{
					return false;
				}
			}
			return true;
		}
		private bool isMethodMatch(CodeMemberMethod tc, MethodClass c)
		{
			return parametersMatch(tc.Parameters, c.Parameters);
		}
		private void createConstructors()
		{
			List<CodeTypeMember> deletes = new List<CodeTypeMember>();
			List<ConstructorClass> constructorList = _rootClassId.GetConstructors();
			foreach (CodeTypeMember m in td.Members)
			{
				if (isStaticClass)
				{
					CodeTypeConstructor tc = m as CodeTypeConstructor;
					if (tc != null)
					{
						bool match = false;
						foreach (ConstructorClass c in constructorList)
						{
							match = isMethodMatch(tc, c);
							if (match)
							{
								break;
							}
						}
						if (!match)
						{
							deletes.Add(m);
						}
					}
				}
				else
				{
					CodeConstructor tc = m as CodeConstructor;
					if (tc != null)
					{
						bool match = false;
						foreach (ConstructorClass c in constructorList)
						{
							match = isMethodMatch(tc, c);
							if (match)
							{
								break;
							}
						}
						if (!match)
						{
							deletes.Add(m);
						}
					}
				}
			}
			foreach (CodeTypeMember m in deletes)
			{
				td.Members.Remove(m);
			}
			if (constructorList.Count > 0)
			{
				foreach (ConstructorClass c in constructorList)
				{
					c.EstablishObjectOwnership();
					CodeMemberMethod cc = getConstructor(c.Parameters);
					if (cc != null)
					{
						CodeConstructor ct = cc as CodeConstructor;
						if (ct != null)
						{
							if (c.ParameterCount > 0)
							{
								int nBase = ct.BaseConstructorArgs.Count;
								for (int i = nBase; i < c.FixedParameters; i++)
								{
									ct.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression(c.Parameters[i].Name));
								}
							}
						}
						c.MethodCode = cc;
						c.SetCompilerData(this);
						cc.Comments.Add(new CodeCommentStatement("<summary>", true));
						if (!string.IsNullOrEmpty(c.Description))
						{
							cc.Comments.Add(new CodeCommentStatement(c.Description, true));
						}
						cc.Comments.Add(new CodeCommentStatement("</summary>", true));
						c.ExportCode(this, cc, false);
					}
				}
			}
		}
		private void addRef(Type t)
		{
			if (_referencedTypes == null)
			{
				_referencedTypes = new List<Type>();
			}
			if (!_referencedTypes.Contains(t))
			{
				_referencedTypes.Add(t);
			}
		}
		private void addReferences(CodeNamespace ns)
		{
			StringCollection refNames = new StringCollection();
			refNames.Add("System");
			refNames.Add("System.Xml");
			refNames.Add("System.Text");
			refNames.Add("System.Collections.Generic");
			//
			_refLocations = new Dictionary<string, Assembly>();
			//
			IList<Guid> gl = _rootClassId.GetDatabaseConnectionsUsed();
			if (gl.Count > 0)
			{
				foreach (Guid g in gl)
				{
					ConnectionItem ci = ConnectionItem.LoadConnection(g, false, false);
					if (ci != null && ci.IsValid)
					{
						DesignUtil.FindReferenceLocations(_refLocations, ci.ConnectionObject.DatabaseType.Assembly);
					}
				}
			}
			//
			//get all libType attributes
			XmlNodeList typeList = XmlUtil.GetTypeAttributeList(doc);
			if (typeList != null && typeList.Count > 0)
			{
				foreach (XmlNode na in typeList)
				{
					//external elements are for development convenience, not a real reference
					if (!SerializeUtil.IsExternalType(na))
					{
						string acid;
						Type ty = XmlUtil.GetLibTypeAttribute(na, out acid);
						if (ty == null)
						{
							continue;
						}
						else if (typeof(ClassPointerX).IsAssignableFrom(ty))
						{
							continue;
						}
						else
						{
							if (!_useDrawing)
							{
								if (string.CompareOrdinal(ty.Assembly.GetName().Name, DrawingPage.AssemblyName) == 0)
								{
									continue;
								}
							}
							if (_debug || !LimnorXmlCompiler.IsLimnorFile(ty.Assembly.Location))
							{
								ty = VPLUtil.GetObjectType(ty);
								if (!refNames.Contains(ty.Namespace))
								{
									if (string.CompareOrdinal(ty.Namespace, "Microsoft.Office.Interop.Word") != 0)
									{
										refNames.Add(ty.Namespace);
									}
								}
								DesignUtil.FindReferenceLocations(_refLocations, ty.Assembly);
							}
						}
					}
				}
			}
			typeList = SerializeUtil.GetTypeNodeList(doc);
			if (typeList != null && typeList.Count > 0)
			{
				foreach (XmlNode na in typeList)
				{
					if (!SerializeUtil.IsExternalType(na))
					{
						string acid;
						Type ty = XmlUtil.GetLibTypeAttribute(na, out acid);
						if (ty == null)
						{
							continue;
						}
						else if (VPLUtil.IsDynamicAssembly(ty.Assembly))
						{
							continue;
						}
						else
						{
							if (string.CompareOrdinal(ty.Namespace, _settings.Namespace) != 0)
							{
								if (_debug || !LimnorXmlCompiler.IsLimnorFile(ty.Assembly.Location))
								{
									ty = VPLUtil.GetObjectType(ty);
									if (!_useDrawing)
									{
										if (ty.Assembly.GetName().Name == DrawingPage.AssemblyName)
										{
											continue;
										}
									}
									if (!refNames.Contains(ty.Namespace))
									{
										refNames.Add(ty.Namespace);
									}
									DesignUtil.FindReferenceLocations(_refLocations, ty.Assembly);
								}
							}
						}
					}
				}
			}
			foreach (object obj in _objMap.Keys)
			{
				IDatabaseConnectionUserExt0 qry = obj as IDatabaseConnectionUserExt0;
				if (qry != null)
				{
					IList<Type> l = qry.DatabaseConnectionTypesUsed;
					if (l != null && l.Count > 0)
					{
						foreach (Type ty in l)
						{
							DesignUtil.FindReferenceLocations(_refLocations, ty.Assembly);
						}
					}
				}
				else
				{
					IAppConfigConsumer iac = _rootObject as IAppConfigConsumer;
					if (iac != null)
					{
						if (_appConfigConsumers == null)
						{
							_appConfigConsumers = new List<IAppConfigConsumer>();
						}
						_appConfigConsumers.Add(iac);
					}
					List<WcfServiceProxy> wcfList = DesignUtil.GetWcfProxyList(_project);
					RuntimeInstance lti = obj as RuntimeInstance;
					if (lti != null && lti.InstanceType != null)
					{
						if (_appConfigConsumers == null)
						{
							_appConfigConsumers = new List<IAppConfigConsumer>();
						}
						WcfServiceProxy wcf = null;
						foreach (WcfServiceProxy w in wcfList)
						{
							if (string.Compare(Path.GetFileName(w.ProxyDllFile), Path.GetFileName(lti.InstanceType.Assembly.Location), StringComparison.OrdinalIgnoreCase) == 0)
							{
								wcf = w;
								break;
							}
						}
						if (wcf != null)
						{
							bool bAdded = false;
							foreach (IAppConfigConsumer ac in _appConfigConsumers)
							{
								if (wcf.IsSameConsumer(ac))
								{
									bAdded = true;
									break;
								}
							}
							if (!bAdded)
							{
								_appConfigConsumers.Add(wcf);
							}
						}
						DesignUtil.FindReferenceLocations(_refLocations, lti.InstanceType.Assembly);
					}
				}
			}
			if (!IsEntryPoint)
			{
				Type tRoot;
				if (_rootObject is DrawingPage)
				{
					tRoot = typeof(Form);
				}
				else
				{
					tRoot = VPLUtil.GetInternalType(_rootObject.GetType());
				}
				DesignUtil.FindReferenceLocations(_refLocations, tRoot.Assembly);
				if (!refNames.Contains(tRoot.Namespace))
				{
					refNames.Add(tRoot.Namespace);
				}
			}
			if (_usePageNavigator)
			{
				DesignUtil.FindReferenceLocations(_refLocations, typeof(VPLUtil).Assembly);
				if (!refNames.Contains(typeof(VPLUtil).Namespace))
				{
					refNames.Add(typeof(VPLUtil).Namespace);
				}
			}
			//==========================================================
			LimnorWinApp wapp = _rootObject as LimnorWinApp;
			if (wapp != null && (wapp.OneInstanceOnly || wapp.UseDuplicationHandler()))
			{
				DesignUtil.FindReferenceLocations(_refLocations, typeof(DllImportAttribute).Assembly);
				DesignUtil.FindReferenceLocations(_refLocations, typeof(Process).Assembly);
				DesignUtil.FindReferenceLocations(_refLocations, typeof(Mutex).Assembly);
				if (!refNames.Contains(typeof(DllImportAttribute).Namespace))
				{
					refNames.Add(typeof(DllImportAttribute).Namespace);
				}
				if (!refNames.Contains(typeof(Process).Namespace))
				{
					refNames.Add(typeof(Process).Namespace);
				}
				if (!refNames.Contains(typeof(Mutex).Namespace))
				{
					refNames.Add(typeof(Mutex).Namespace);
				}
			}
			if (_referencedTypes != null)
			{
				foreach (Type tp in _referencedTypes)
				{
					if (!refNames.Contains(tp.Namespace))
					{
						refNames.Add(tp.Namespace);
					}
					DesignUtil.FindReferenceLocations(_refLocations, tp.Assembly);
				}
			}
			//remove usings which are known to be causing trouble
			StringCollection delNs = new StringCollection();
			foreach (string s in LimnorXmlCompiler.ExcludeNamespaces)
			{
				foreach (string s0 in refNames)
				{
					if (string.IsNullOrEmpty(s0))
					{
					}
					else
					{
						if (s0.StartsWith(s, StringComparison.Ordinal))
						{
							delNs.Add(s0);
						}
					}
				}
			}
			if (delNs.Count > 0)
			{
				foreach (string s0 in delNs)
				{
					refNames.Remove(s0);
				}
			}
			//==========================================================
			//
			foreach (string nm in refNames)
			{
				if (!string.IsNullOrEmpty(nm))
				{
					ns.Imports.Add(new CodeNamespaceImport(nm));
				}
			}
		}
		private void adjustComponentsAccessLevel()
		{
			if (_project.ProjectType == EnumProjectType.ClassDLL)
			{
				return;
			}
			foreach (object v in _objMap.Keys)
			{
				IComponent ic = (IComponent)v;
				if (ic.Site != null)
				{
					for (int i = 0; i < td.Members.Count; i++)
					{
						CodeMemberField cmf = td.Members[i] as CodeMemberField;
						if (cmf != null)
						{
							if (string.CompareOrdinal(cmf.Name, ic.Site.Name) == 0)
							{
								if (isStaticClass)
								{
									cmf.Attributes = MemberAttributes.FamilyOrAssembly | MemberAttributes.Static;
								}
								else
								{
									cmf.Attributes = MemberAttributes.FamilyOrAssembly;
								}
								break;
							}
						}
					}
				}
			}
			foreach (HtmlElement_BodyBase v in _rootClassId.UsedHtmlElements)
			{
				if (!string.IsNullOrEmpty(v.ServerTypeName))
				{
					CodeMemberField cmf = new CodeMemberField(v.ServerTypeName, v.CodeName);
					td.Members.Add(cmf);
				}
			}
		}
		private void addKioskSupport(CodeNamespace ns)
		{
			if (_project.ProjectType == EnumProjectType.Kiosk)
			{
				ns.Imports.Add(new CodeNamespaceImport("LimnorKiosk"));
				if (td.BaseTypes != null && td.BaseTypes.Count > 0)
				{
					for (int i = 0; i < td.BaseTypes.Count; i++)
					{
						if (string.CompareOrdinal(td.BaseTypes[i].BaseType, "LimnorKiosk.FormKioskBackground") == 0)
						{
							CodeConstructor cc = (CodeConstructor)getDefaultConstructor();
							cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(fnSimpleFunction), "loadFirstForm"));
							cc.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("loadFirstForm"));
							break;
						}
					}
				}
			}
		}
		private void copyMultiLanguageWebResourceFiles()
		{
			ProjectResources rm = _project.GetProjectSingleData<ProjectResources>();
			if (rm.HasResources)
			{
			}
		}
		private void copyWebResourceFiles(IWebResourceFileUser w)
		{
			IList<WebResourceFile> files = w.GetResourceFiles();
			copyWebResourceFiles(files);
		}
		private void copyWebResourceFiles(IList<WebResourceFile> files)
		{
			if (files != null)
			{
				foreach (WebResourceFile f in files)
				{
					if (!f.ResourceFile.StartsWith(_classCompiler.WebPhysicalFolder, StringComparison.OrdinalIgnoreCase))
					{
						string wdir = Path.Combine(_classCompiler.WebPhysicalFolder, f.WebFolder);
						if (!Directory.Exists(wdir))
						{
							Directory.CreateDirectory(wdir);
						}
						if (File.Exists(f.ResourceFile))
						{
							string target = Path.Combine(wdir, Path.GetFileName(f.ResourceFile));
							if (LimnorXmlCompiler.FileNotCopied(target))
							{
								try
								{
									File.Copy(f.ResourceFile, target, true);
								}
								catch (Exception err)
								{
									if (File.Exists(target))
									{
									}
									else
									{
										throw new DesignerException(err, "Error copying file from {0} to {1}. You may try to copy it manually.", f.ResourceFile, target);
									}
								}
							}
						}
						else
						{
							AddError(string.Format(CultureInfo.InvariantCulture, "File not found:[{0}]", f.ResourceFile));
						}
					}
				}
			}
		}
		private Dictionary<IProperty, ResourcePointer> getResourceMapping()
		{
			Dictionary<IProperty, ResourcePointer> mapping = new Dictionary<IProperty, ResourcePointer>();
			Dictionary<IProperty, UInt32> map = _rootClassId.ResourceMap;
			if (map != null && map.Count > 0)
			{
				ProjectResources rm = _project.GetProjectSingleData<ProjectResources>();
				if (rm.HasResources)
				{
					Dictionary<IProperty, UInt32>.Enumerator en = map.GetEnumerator();
					while (en.MoveNext())
					{
						ResourcePointer rp = rm.GetResourcePointerById(en.Current.Value);
						if (rp != null)
						{
							mapping.Add(en.Current.Key, rp);
						}
					}
				}
			}
			return mapping;
		}
		private void generateHtmlElements(Tree te, XmlNode parentNode, Dictionary<IProperty, ResourcePointer> resMap)
		{
			XmlNode p0 = parentNode;
			IWebClientControl webc = te.Owner as IWebClientControl;
			if (webc != null)
			{
				EnumWebElementPositionType pt = EnumWebElementPositionType.Absolute;
				XmlNode nd = parentNode.OwnerDocument.CreateElement(webc.ElementName);
				parentNode.AppendChild(nd);
				XmlUtil.SetAttribute(nd, "id", webc.CodeName);
				webc.CreateHtmlContent(nd, pt, -1);
				WebPageCompilerUtility.CreateElementAnchor(webc, nd);
				//
				StringCollection sc = new StringCollection();
				foreach (KeyValuePair<IProperty, ResourcePointer> kv in resMap)
				{
					if (string.CompareOrdinal(kv.Key.Owner.CodeName, webc.CodeName) == 0)
					{
						sc.Add(string.Format(CultureInfo.InvariantCulture,
							"_pageResources_:{0}:{1}",
							kv.Value.Name, webc.MapJavaScriptCodeName(kv.Key.Name)));
					}
				}
				if (sc.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					sb.Append(sc[0]);
					for (int i = 1; i < sc.Count; i++)
					{
						sb.Append(";");
						sb.Append(sc[i]);
					}
					XmlUtil.SetAttribute(nd, "jsdb", sb.ToString());
				}
				//
				Dictionary<string, string> hp = webc.HtmlParts;
				if (hp != null && hp.Count > 0)
				{
					_htmlParts.Add(hp);
				}
				p0 = nd;
			}
			IWebPageLayout lt = te.Owner as IWebPageLayout;
			if (lt == null)
			{
				foreach (Tree t in te)
				{
					generateHtmlElements(t, p0, resMap);
				}
			}
		}
		static public string BeautifyXml(XmlDocument doc)
		{
			StringBuilder sb = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "  ";
			settings.NewLineChars = "\r\n";
			settings.NewLineHandling = NewLineHandling.Replace;
			using (XmlWriter writer = XmlWriter.Create(sb, settings))
			{
				doc.Save(writer);
			}
			return sb.ToString();
		}
		private void generateHtmlFile(StringCollection sc)
		{
			IWebPage page = (IWebPage)_rootObject;
			//
			StringBuilder scsb = new StringBuilder();
			for (int i = 0; i < sc.Count; i++)
			{
				scsb.Append(sc[i]);
			}
			scriptNode.AppendChild(scriptNode.OwnerDocument.CreateCDataSection(scsb.ToString()));
			//
			string htmlFile = Path.Combine(_classCompiler.WebPhysicalFolder, HtmlFile);
			//
			string htmlDesignFile = string.Empty;
			bool useHtmlEditor = DesignService.IsUsingHtmlEditor();
			if (useHtmlEditor)
			{
				htmlDesignFile = WebHost.GetHtmlFile(_xmlFile);
				scriptNode.OwnerDocument.PreserveWhitespace = true;
				if (File.Exists(htmlDesignFile))
				{
					List<IHtmlElementCreateContents> heccList = new List<IHtmlElementCreateContents>();
					List<HtmlElement_BodyBase> list = new List<HtmlElement_BodyBase>();
					for (int i = 0; i < _rootClassId.UsedHtmlElements.Count; i++)
					{
						IHtmlElementCreateContents hecc = _rootClassId.UsedHtmlElements[i] as IHtmlElementCreateContents;
						if (hecc != null)
						{
							heccList.Add(hecc);
						}
						if (_rootClassId.UsedHtmlElements[i].DataBindings.Count > 0)
						{
							list.Add(_rootClassId.UsedHtmlElements[i]);
						}
						else
						{
							IDataSourceBinder dsb = _rootClassId.UsedHtmlElements[i] as IDataSourceBinder;
							if (dsb != null)
							{
								IDataSetSource ids = dsb.DataSource as IDataSetSource;
								if (ids != null && !string.IsNullOrEmpty(ids.TableName))
								{
									list.Add(_rootClassId.UsedHtmlElements[i]);
								}
							}
						}
					}
					HtmlUtility2.MergeXmlDocumentIntoHtml(htmlDesignFile, page.EnableBrowserPageCache, scriptNode.OwnerDocument, htmlFile, list, heccList, _iframeId, serverFileName());
				}
				else
				{
					useHtmlEditor = false;
				}
			}
			if(!useHtmlEditor)
			{
				XmlNode ndTitle = null;
				XmlNode ndMeta = null;
				bool bFound = false;
				XmlNode hn = scriptNode.OwnerDocument.SelectSingleNode("html/head");
				if (hn != null)
				{
					for (int i = 0; i < hn.ChildNodes.Count; i++)
					{
						if (ndTitle == null)
						{
							if (string.Compare(hn.ChildNodes[i].Name, "title", StringComparison.OrdinalIgnoreCase) == 0)
							{
								ndTitle = hn.ChildNodes[i];
							}
						}
						if (ndMeta == null)
						{
							if (string.Compare(hn.ChildNodes[i].Name, "meta", StringComparison.OrdinalIgnoreCase) == 0)
							{
								ndMeta = hn.ChildNodes[i];
							}
						}
						string cn = XmlUtil.GetAttribute(hn.ChildNodes[i], "Content");
						if (string.Compare(cn, "IE=edge", StringComparison.OrdinalIgnoreCase) == 0)
						{
							bFound = true;
							break;
						}
					}
				}
				if (!bFound)
				{
					if (hn == null)
					{
						hn = scriptNode.OwnerDocument.CreateElement("head");
						scriptNode.OwnerDocument.DocumentElement.AppendChild(hn);
					}
					XmlNode ma = scriptNode.OwnerDocument.CreateElement("meta");
					XmlUtil.SetAttribute(ma, "http-equiv", "X-UA-Compatible");
					XmlUtil.SetAttribute(ma, "content", "IE=edge");
					if (ndMeta != null)
					{
						hn.InsertBefore(ma, ndMeta);
					}
					else if (ndTitle != null)
					{
						hn.InsertAfter(ma, ndTitle);
					}
					else
					{
						if (hn.ChildNodes.Count > 0)
						{
							hn.InsertBefore(ma, hn.ChildNodes[0]);
						}
						else
						{
							hn.AppendChild(ma);
						}
					}
				}
				if (!page.EnableBrowserPageCache)
				{
					XmlNode ma = scriptNode.OwnerDocument.CreateElement("meta");
					XmlUtil.SetAttribute(ma, "http-equiv", "PRAGMA");
					XmlUtil.SetAttribute(ma, "content", "NO-CACHE");
					if (ndMeta != null)
					{
						hn.InsertBefore(ma, ndMeta);
					}
					else if (ndTitle != null)
					{
						hn.InsertAfter(ma, ndTitle);
					}
					else
					{
						if (hn.ChildNodes.Count > 0)
						{
							hn.InsertBefore(ma, hn.ChildNodes[0]);
						}
						else
						{
							hn.AppendChild(ma);
						}
					}
				}
				scriptNode.OwnerDocument.Save(htmlFile);
			}
			StreamReader sr = new StreamReader(htmlFile);
			Encoding ed = sr.CurrentEncoding;
			string html = sr.ReadToEnd();
			sr.Close();
			Application.DoEvents();
			if (html == null)
				html = string.Empty;
			html = html.Trim();
			if (!string.IsNullOrEmpty(htmlDesignFile))
			{
				string cssDesignFile = Path.Combine(Path.GetDirectoryName(htmlDesignFile),
					string.Format(CultureInfo.InvariantCulture, "{0}.css", Path.GetFileNameWithoutExtension(htmlDesignFile)));
				if (File.Exists(cssDesignFile))
				{
					string cssFile = Path.Combine(Path.GetDirectoryName(htmlFile),
						string.Format(CultureInfo.InvariantCulture, "{0}.css", Path.GetFileNameWithoutExtension(htmlFile)));
					string designcssname = Path.GetFileName(cssDesignFile);
					int pi = html.IndexOf(designcssname, StringComparison.OrdinalIgnoreCase);
					if (pi > 0)
					{
						File.Copy(cssDesignFile, cssFile, true);
						html = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", html.Substring(0, pi), Path.GetFileName(cssFile), html.Substring(pi + designcssname.Length));
					}
				}
			}
			
			html = html.Replace("<![CDATA[", "");
			html = html.Replace("]]>", "");
			html = html.Replace(WebPageCompilerUtility.AMPSAND, "&");
			if (_htmlParts.Count > 0)
			{
				foreach (Dictionary<string, string> hp in _htmlParts)
				{
					foreach (KeyValuePair<string, string> kv in hp)
					{
						html = html.Replace(kv.Key, kv.Value);
					}
				}
			}
			StreamWriter sw = new StreamWriter(htmlFile, false, ed);
			if (!html.StartsWith("<!DOCTYPE ", StringComparison.OrdinalIgnoreCase))
			{
				sw.WriteLine("<!DOCTYPE html>");
			}
			sw.Write(html);
			sw.Close();
			//
			LimnorXmlCompiler.LastCompiledHtmlFile = htmlFile;
			//
			IWebResourceFileUser wr = page as IWebResourceFileUser;
			if (wr != null)
			{
				copyWebResourceFiles(wr);
			}
			copyWebResourceFilesFromComponent();
		}
		private string serverFileName()
		{
			StringBuilder sc = new StringBuilder();
			string srvName = Path.GetFileNameWithoutExtension(HtmlFile);
			if (_project.ProjectType == EnumProjectType.WebAppPhp)
			{
				if (string.CompareOrdinal(srvName, "index") == 0)
				{
					srvName = "index0833";
				}
			}
			sc.Append(srvName);
			sc.Append(".");
			if (_project.ProjectType == EnumProjectType.WebAppPhp)
			{
				sc.Append("php");
			}
			else if (_project.ProjectType == EnumProjectType.WebAppAspx)
			{
				sc.Append("aspx");
			}
			return sc.ToString();
		}
		/// <summary>
		/// create html in xml
		/// </summary>
		/// <returns></returns>
		private XmlNode generateHtmlPage()
		{
			IWebPage page = _rootObject as IWebPage;
			if (page == null)
			{
				return null;
			}
			Dictionary<IProperty, ResourcePointer> resMap = getResourceMapping();
			XmlDocument doc = new XmlDocument();
			XmlNode xRoot = doc.CreateElement(XhtmlTags.XH_html);
			doc.AppendChild(xRoot);
			page.CreateHtmlContent(xRoot, EnumWebElementPositionType.Auto, -1);
			XmlNode xHead = xRoot.SelectSingleNode(XhtmlTags.XH_head);
			//
			_customValues = new Dictionary<string, WebClientValueCollection>();
			_jsFiles = new Dictionary<string, string>();
			foreach (object v in _rootClassId.ObjectList.Keys)
			{
				IJsFilesResource jsr = v as IJsFilesResource;
				if (jsr != null)
				{
					Dictionary<string, string> dic = jsr.GetJsFilenames();
					if (dic != null)
					{
						foreach (KeyValuePair<string, string> kv in dic)
						{
							if (!_jsFiles.ContainsKey(kv.Key))
							{
								_jsFiles.Add(kv.Key, kv.Value);
							}
						}
					}
				}
				IWebClientComponent iwc = v as IWebClientComponent;
				if (iwc != null && iwc.CustomValues != null)
				{
					if (!string.IsNullOrEmpty(iwc.tag))
					{
						if (!iwc.CustomValues.ContainsKey("tag"))
						{
							iwc.CustomValues.Add("tag", new JsString(iwc.tag));
						}
					}
					if (iwc.CustomValues.Count > 0)
					{
						if (!(v is IWebPage) && string.IsNullOrEmpty(iwc.id))
						{
							AddError(string.Format(CultureInfo.InvariantCulture, "Html item [{0}] missing id", iwc.GetType()));
						}
						else
						{
							if (_customValues.ContainsKey(iwc.id))
							{
								AddError(string.Format(CultureInfo.InvariantCulture, "Html item id [{0}] used more than once", iwc.id));
							}
							else
							{
								_customValues.Add(iwc.id, iwc.CustomValues);
							}
						}
					}
				}
			}
			IList<HtmlElement_BodyBase> lst = _rootClassId.UsedHtmlElements;
			if (lst != null && lst.Count > 0)
			{
				foreach (HtmlElement_BodyBase hbb in lst)
				{
					if (hbb != null)
					{
						if (!string.IsNullOrEmpty(hbb.tag))
						{
							if (!hbb.CustomValues.ContainsKey("tag"))
							{
								hbb.CustomValues.Add("tag", new JsString(hbb.tag));
							}
						}
						HtmlElement_body hbody = hbb as HtmlElement_body;
						if (hbody != null)
						{
							_bodyValues = hbody.CustomValues;
						}
						else
						{
							if (hbb.CustomValues.Count > 0)
							{
								if (string.IsNullOrEmpty(hbb.id))
								{
									AddError(string.Format(CultureInfo.InvariantCulture, "Html element [{0}] missing id", hbb.GetType()));
								}
								else
								{
									if (_customValues.ContainsKey(hbb.id))
									{
										AddError(string.Format(CultureInfo.InvariantCulture, "Html element id [{0}] used more than once", hbb.id));
									}
									else
									{
										_customValues.Add(hbb.id, hbb.CustomValues);
									}
								}
							}
						}
					}
				}
			}
			//
			XmlNode limnorNode = xHead.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
				"{0}[@name='webapp']", XhtmlTags.XH_script));
			if (limnorNode == null)
			{
				throw new DesignerException("WebApp node not found");
			}
			XmlUtil.RemoveAttribute(limnorNode, XmlTags.XMLATT_NAME);
			XmlUtil.SetAttribute(limnorNode, "src", string.Format(CultureInfo.InvariantCulture, "libjs/{0}.js", _classCompiler.AppName));
			foreach (object v in _rootClassId.ObjectList.Keys)
			{
				JavascriptFiles jsf = v as JavascriptFiles;
				if (jsf != null)
				{
					string[] ss = jsf.Filenames;
					for (int i = 0; i < ss.Length; i++)
					{
						if (!string.IsNullOrEmpty(ss[i]) && File.Exists(ss[i]))
						{
							string webFolder;
							if (ss[i].StartsWith(_classCompiler.WebPhysicalFolder, StringComparison.OrdinalIgnoreCase))
							{
								webFolder = ss[i].Substring(_classCompiler.WebPhysicalFolder.Length).Replace("\\", "/");
								if (webFolder.StartsWith("/", StringComparison.Ordinal))
								{
									webFolder = webFolder.Substring(1);
								}
							}
							else
							{
								webFolder = string.Format(CultureInfo.InvariantCulture, "libjs/{0}", Path.GetFileName(ss[i]));
							}
							XmlNode jsNode = doc.CreateElement(XhtmlTags.XH_script);
							XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
							XmlUtil.SetAttribute(jsNode, "src", webFolder);
							((XmlElement)jsNode).IsEmpty = false;
							xHead.InsertAfter(jsNode, limnorNode);
						}
					}
				}
			}
			foreach (object v in _rootClassId.ObjectList.Keys)
			{
				JavascriptFiles jsf = v as JavascriptFiles;
				if (jsf != null)
				{
					string jscode = jsf.PageScopeJavascriptCode;
					if (!string.IsNullOrEmpty(jscode))
					{
						XmlNode jsNode = doc.CreateElement(XhtmlTags.XH_script);
						XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
						xHead.InsertAfter(jsNode, limnorNode);
						jsNode.AppendChild(doc.CreateCDataSection(jscode));
					}
				}
			}
			foreach (object v in _rootClassId.ObjectList.Keys)
			{
				JavascriptFiles jsf = v as JavascriptFiles;
				if (jsf != null)
				{
					VplPropertyBag pb = jsf.JavascriptPageVariables;
					IList<TypedNamedValue> vars = pb.PropertyList;
					if (vars.Count > 0)
					{
						XmlNode jsNode = doc.CreateElement(XhtmlTags.XH_script);
						XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
						xHead.InsertAfter(jsNode, limnorNode);
						StringBuilder sb = new StringBuilder();
						foreach (TypedNamedValue tn in vars)
						{
							sb.Append("var ");
							sb.Append(tn.Name);
							if (tn.Value != null)
							{
								sb.Append("=");
								sb.Append(ObjectCreationCodeGen.ObjectCreateJavaScriptCode(tn.Value));
							}
							sb.Append(";\r\n");
						}
						jsNode.InnerText = sb.ToString();
					}
				}
			}
			XmlNode lastScriptNode = null;
			for (int i = 0; i < xHead.ChildNodes.Count; i++)
			{
				if (string.Compare(xHead.ChildNodes[i].Name, "script", StringComparison.OrdinalIgnoreCase) == 0)
				{
					lastScriptNode = xHead.ChildNodes[i];
					break;
				}
			}
			foreach (object v in _rootClassId.ObjectList.Keys)
			{
				JavascriptFiles jsf = v as JavascriptFiles;
				if (jsf != null)
				{
					string[] ss = jsf.CssFilenames;
					if (ss != null)
					{
						for (int i = 0; i < ss.Length; i++)
						{
							if (!string.IsNullOrEmpty(ss[i]) && File.Exists(ss[i]))
							{
								XmlNode jsNode = doc.CreateElement("link");
								XmlUtil.SetAttribute(jsNode, "rel", "stylesheet");
								XmlUtil.SetAttribute(jsNode, "href", string.Format(CultureInfo.InvariantCulture, "css/{0}", Path.GetFileName(ss[i])));
								if (lastScriptNode == null)
								{
									xHead.InsertBefore(jsNode, limnorNode);
								}
								else
								{
									xHead.InsertBefore(jsNode, lastScriptNode);
								}
							}
						}
					}
				}
			}
			//
			XmlNode bodyNode = xRoot.SelectSingleNode(XhtmlTags.XH_body);
			_htmlParts = new List<Dictionary<string, string>>();
			Dictionary<string, string> hp = page.HtmlParts;
			if (hp != null && hp.Count > 0)
			{
				_htmlParts.Add(hp);
			}
			TreeRoot tr = ObjectMap.TreeRoot;
			foreach (Tree te in tr)
			{
				generateHtmlElements(te, bodyNode, resMap);
			}
			//
			return page.SctiptNode;
		}
		private void savePageResourcs(string cultureName, StringCollection scData)
		{
			string jsFile = Path.Combine(_classCompiler.GetCultureFolder(cultureName), JsFile);
			StreamWriter sw = new StreamWriter(jsFile, false);
			sw.Write("var row_");
			if (!string.IsNullOrEmpty(cultureName))
			{
				sw.Write(cultureName.Replace("-", "_"));
			}
			sw.Write("={'ItemArray':[");
			sw.Write("'");
			sw.Write(cultureName);
			sw.Write("'");
			for (int i = 0; i < scData.Count; i++)
			{
				sw.Write(",");
				sw.Write("'");
				if (!string.IsNullOrEmpty(scData[i]))
				{
					sw.Write(scData[i].Replace("'", "\\'"));
				}
				sw.Write("'");
			}
			sw.WriteLine("]};\r\n");
			sw.Write("JsonDataBinding.AddPageCulture('");
			if (!string.IsNullOrEmpty(cultureName))
			{
				sw.Write(cultureName);
			}
			sw.WriteLine("');\r\n");
			sw.Close();
		}
		private void copyWebResourceFilesFromComponent()
		{
			TreeRoot tr = ObjectMap.TreeRoot;
			foreach (Tree te in tr)
			{
				copyWebResourceFilesFromComponent(te);
			}
		}
		private void copyWebResourceFilesFromComponent(Tree te)
		{
			IWebResourceFileUser webc = te.Owner as IWebResourceFileUser;
			if (webc != null)
			{
				copyWebResourceFiles(webc);
			}
			foreach (Tree t in te)
			{
				copyWebResourceFilesFromComponent(t);
			}
		}
		/// <summary>
		/// compile WebApp class
		/// </summary>
		/// <returns></returns>
		private bool generatePhpWebApp()
		{
			TraceLogClass.TraceLog.Trace("Compiling php web app {0} starts ........", _settings.TypeName);
			TraceLogClass.TraceLog.IndentIncrement();
			try
			{
				string phpFile = Path.Combine(_classCompiler.WebPhysicalFolder, PhpFile);
				StreamWriter sw = new StreamWriter(phpFile, false, Encoding.UTF8);
				sw.WriteLine("<?php");
				sw.WriteLine("header('Content-Type: text/html; charset=utf-8');");
				sw.WriteLine("header('Access-Control-Allow-Origin: *');");
				string desc = SerializeUtil.GetNodeDescription(doc.DocumentElement);
				if (!string.IsNullOrEmpty(desc))
				{
					sw.WriteLine("/*");
					sw.WriteLine(desc);
					sw.WriteLine("*/");
				}
				sw.WriteLine();
				sw.WriteLine("include_once 'libphp/WebApp.php';");
				///PHP contents ===============
				setWebServerCompilerPhp();
				//
				StringCollection sc = new StringCollection();
				/////
				//generate properties============================================
				createWebProperties(sc, _serverScripts);
				//generate methods===============================================
				createWebMethods(sc, _serverScripts);
				//
				sw.WriteLine();
				sw.Write("class ");
				sw.Write(_settings.Name);
				sw.WriteLine(" extends WebApp");
				sw.WriteLine("{");
				//
				sw.WriteLine("\tfunction __construct(){parent::__construct();$this->PhpPhysicalFolder = dirname(realpath(__FILE__));}");
				//
				_serverScripts.WriteCode(sw);
				//
				sw.WriteLine("}");
				///
				sw.WriteLine();
				sw.WriteLine("?>");
				sw.Close();
				bRet = true;
			}
			catch (Exception er)
			{
				logException(er);
				bRet = false;
			}
			finally
			{
			}
			TraceLogClass.TraceLog.IndentDecrement();
			if (HasErrors)
			{
				TraceLogClass.TraceLog.Trace("Compiling web page {0} failed ........", _settings.TypeName);
				bRet = false;
			}
			else
			{
				TraceLogClass.TraceLog.Trace("Compiling web page {0} ends ........", _settings.TypeName);
			}
			return bRet;
		}
		/// <summary>
		/// not for WebApp, page only
		/// </summary>
		/// <returns></returns>
		private bool generatePhpWebPage()
		{
			TraceLogClass.TraceLog.Trace("Compiling php web page {0} starts ........", _settings.TypeName);
			TraceLogClass.TraceLog.IndentIncrement();
			try
			{
				setWebServerCompilerPhp();
				//
				scriptNode = generateHtmlPage();
				//
				StringCollection sc = new StringCollection();
				//
				sc.Add("\r\nvar limnorPage={};");
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					"\r\ndocument.pageId = {0};\r\n", _rootClassId.ClassId));
				if (VPLUtil.SessionDataStorage == EnumSessionDataStorage.HTML5Storage)
				{
					sc.Add("JsonDataBinding.UseLocalStore=true;\r\n");
				}
				//
				//generate properties============================================
				createWebProperties(sc, _serverScripts);
				//generate methods===============================================
				createWebMethods(sc, _serverScripts);
				//
				//generate event handlers========================================
				XmlNode ifNode = scriptNode.OwnerDocument.SelectSingleNode("//iframe");
				_iframeId = null;
				if (ifNode != null)
				{
					_iframeId = XmlUtil.GetAttribute(ifNode, "id");
				}
				if (string.IsNullOrEmpty(_iframeId))
				{
					for (int i = 0; i < _rootClassId.UsedHtmlElements.Count; i++)
					{
						IFormSubmitter ifs = _rootClassId.UsedHtmlElements[i] as IFormSubmitter;
						if (ifs != null)
						{
							_iframeId = HtmlFileUploadGroup.CreateIFrame(scriptNode);
							break;
						}
					}
				}
				createWebClientHandlers(sc, _serverScripts, _iframeId);
				//
				addExtraJsFiles();
				//
				//write out html file
				generateHtmlFile(sc);
				//
				StringCollection serverObjects = new StringCollection();
				StringCollection init = convertToPhpWebPage(serverObjects);
				//
				//generate php page from scPhp
				string phpFile = Path.Combine(_classCompiler.WebPhysicalFolder, PhpFile);
				if (string.CompareOrdinal(_settings.Name, "index") == 0)
				{
					string oldPhp = Path.Combine(_classCompiler.WebPhysicalFolder, "index.php");
					if (File.Exists(oldPhp))
					{
						if (_errors == null)
						{
							_errors = new ArrayList();
						}
						_errors.Add(string.Format(CultureInfo.InvariantCulture, "This is not an error. File [{0}] coud be an old file no longer needed. It is renamed to [{0}.old]", oldPhp));
						try
						{
							File.Move(oldPhp, string.Format(CultureInfo.InvariantCulture, "{0}.old", oldPhp));
						}
						catch (Exception ef0)
						{
							_errors.Add(string.Format(CultureInfo.InvariantCulture, "Error moving file {0}. {1}", oldPhp, ef0.Message));
						}
					}
				}
				StreamWriter sw = new StreamWriter(phpFile, false, Encoding.UTF8);
				sw.WriteLine("<?php");
				sw.WriteLine("header('Content-Type: text/html; charset=utf-8');");
				string desc = SerializeUtil.GetNodeDescription(doc.DocumentElement);
				if (!string.IsNullOrEmpty(desc))
				{
					sw.WriteLine("/*");
					sw.WriteLine(desc);
					sw.WriteLine("*/");
				}
				sw.WriteLine();
				sw.WriteLine("include_once 'libphp/sqlClient.php';");
				sw.WriteLine("include_once 'libphp/dataSourceInterface.php';");
				sw.WriteLine("include_once 'libphp/JsonProcessPage.php';");
				sw.WriteLine("include_once 'libphp/jsonSource_mySqlI.php';");
				sw.WriteLine("include_once 'libphp/FastJSON.class.php';");
				sw.WriteLine("include_once 'libphp/jsonDataBind.php';");
				sw.WriteLine("include_once 'libphp/mySqlcredential.php';");
				sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
					"include_once '{0}.php';", _classCompiler.AppCodeName));
				if (_phpIncludes != null)
				{
					foreach (string s0 in _phpIncludes)
					{
						string php = Path.Combine(_classCompiler.WebPhpFolder, Path.GetFileName(s0));
						if (string.Compare(php, s0, StringComparison.OrdinalIgnoreCase) != 0)
						{
							File.Copy(s0, php, true);
						}
						sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "include_once 'libphp/{0}';", Path.GetFileName(s0)));
					}
				}
				if (_phpFiles != null)
				{
					foreach (Dictionary<string, string> flist in _phpFiles)
					{
						foreach (KeyValuePair<string, string> kv in flist)
						{
							string php = Path.Combine(_classCompiler.WebPhpFolder, kv.Key);
							StreamWriter phpW = new StreamWriter(php, false, Encoding.UTF8);
							phpW.Write(kv.Value);
							phpW.Close();
							//
							sw.Write("include_once 'libphp/");
							sw.Write(kv.Key);
							sw.WriteLine("';");
						}
					}
				}
				sw.WriteLine();
				sw.Write("class ");
				sw.Write(_settings.Name);
				sw.WriteLine(" extends JsonProcessPage");
				sw.WriteLine("{");
				//
				sw.WriteLine("private $WebAppPhp;");
				//
				for (int i = 0; i < serverObjects.Count; i++)
				{
					sw.Write(serverObjects[i]);
				}
				//
				StringCollection connectionNames = new StringCollection();
				IList<Guid> cns = _rootClassId.GetDatabaseConnectionsUsed();
				int cnnCount = 0;
				if (cns != null)
				{
					cnnCount = cns.Count;
				}
				for (int i = 0; i < cnnCount; i++)
				{
					string cr = ServerCodeutility.GetPhpMySqlConnectionName(cns[i]);
					connectionNames.Add(cr);
					sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "private ${0};", cr));
				}
				//
				sw.Write("function __construct(");//$c)");
				for (int i = 0; i < cnnCount; i++)
				{
					if (i > 0)
					{
						sw.Write(",");
					}
					sw.Write(String.Format(CultureInfo.InvariantCulture, "${0}", connectionNames[i]));
				}
				sw.WriteLine(")");
				sw.WriteLine("{");
				//
				sw.WriteLine("  parent::__construct();");
				sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "    $this->WebAppPhp = new {0}();", _classCompiler.AppCodeName));
				sw.WriteLine("$this->PhpPhysicalFolder = $this->WebAppPhp->PhpPhysicalFolder;");
				for (int i = 0; i < cnnCount; i++)
				{
					sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "$this->{0} = ${0};", connectionNames[i]));
				}
				//
				for (int i = 0; i < init.Count; i++)
				{
					sw.Write(init[i]);
				}
				//
				sw.WriteLine("}");
				//
				_serverScripts.WriteCode(sw);
				//
				sw.WriteLine("}");
				sw.Write("$w = new ");
				sw.Write(_settings.Name);
				sw.Write("(");
				for (int i = 0; i < cnnCount; i++)
				{
					if (i > 0)
					{
						sw.Write(",");
					}
					sw.Write(String.Format(CultureInfo.InvariantCulture, "${0}", connectionNames[i]));
				}
				sw.WriteLine(");");
				sw.WriteLine("$w->ProcessClientRequest();");
				sw.WriteLine();
				sw.WriteLine("?>");
				sw.Close();
				//
				bRet = true;
			}
			catch (Exception er)
			{
				logException(er);
				bRet = false;
			}
			finally
			{
			}
			TraceLogClass.TraceLog.IndentDecrement();
			if (HasErrors)
			{
				TraceLogClass.TraceLog.Trace("Compiling web page {0} failed ........", _settings.TypeName);
				bRet = false;
			}
			else
			{
				TraceLogClass.TraceLog.Trace("Compiling web page {0} ends ........", _settings.TypeName);
			}
			return bRet;
		}
		private XmlNode scriptNode;
		private bool generateAspxWebPage(CodeCompileUnit code, CodeNamespace ns)
		{
			TraceLogClass.TraceLog.Trace("Compiling aspx web page {0} starts ........", _settings.TypeName);
			TraceLogClass.TraceLog.IndentIncrement();
			try
			{
				convertToAspxWebPage();
				//
				scriptNode = generateHtmlPage();
				//
				addRef(typeof(System.Web.UI.Page));
				//
				DesignUtil.FindReferenceLocations(_refLocations, typeof(System.Web.UI.Page).Assembly);
				//
				td.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				//
				xr.EnableTrace(true);
				_use32bit = _rootClassId.Report32Usage();
				//
				StringCollection sc = new StringCollection();
				//
				sc.Add("\r\nvar limnorPage={};");
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					   "\r\ndocument.pageId = {0};\r\n", _rootClassId.ClassId));
				if (VPLUtil.SessionDataStorage == EnumSessionDataStorage.HTML5Storage)
				{
					sc.Add("JsonDataBinding.UseLocalStore=true;\r\n");
				}
				//
				//generate properties============================================
				createWebProperties(sc, _serverScripts);
				//
				//generate methods===============================================
				createWebMethods(sc, null);
				//
				//generate event handlers========================================
				XmlNode ifNode = scriptNode.OwnerDocument.SelectSingleNode("//iframe");
				_iframeId = null;
				if (ifNode != null)
				{
					_iframeId = XmlUtil.GetAttribute(ifNode, "id");
				}
				if (string.IsNullOrEmpty(_iframeId))
				{
					for (int i = 0; i < _rootClassId.UsedHtmlElements.Count; i++)
					{
						IFormSubmitter ifs = _rootClassId.UsedHtmlElements[i] as IFormSubmitter;
						if (ifs != null)
						{
							_iframeId = HtmlFileUploadGroup.CreateIFrame(scriptNode);
							break;
						}
					}
				}
				createWebClientHandlers(sc, null, _iframeId);
				//
				addExtraJsFiles();
				//
				_serverPageCode.WriteCode(td);
				//
				generateCode(code);
				//
				//write out html file
				generateHtmlFile(sc);
				//
				//write aspx file
				string aspxFile = Path.Combine(_classCompiler.WebPhysicalFolder, AspxFile);
				StreamWriter swa = new StreamWriter(aspxFile, false);
				swa.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					LimnorCompiler.Resources1.aspxHead, Path.GetFileName(SourceFile), string.Format(CultureInfo.InvariantCulture, "{0}.{1}", ns.Name, _rootClassId.TypeString)));
				swa.Close();
				//
				bRet = true;
			}
			catch (Exception er)
			{
				logException(er);
				bRet = false;
			}
			TraceLogClass.TraceLog.IndentDecrement();
			if (HasErrors)
			{
				TraceLogClass.TraceLog.Trace("Compiling web page {0} failed ........", _settings.TypeName);
				bRet = false;
			}
			else
			{
				TraceLogClass.TraceLog.Trace("Compiling web page {0} ends ........", _settings.TypeName);
			}
			return bRet;
		}
		private void addExtraJsFiles()
		{
			XmlNode head = scriptNode.ParentNode;
			if (VPLUtil.JsFiles != null && VPLUtil.JsFiles.Count > 0)
			{
				XmlNode sc = null;
				for (int i = 0; i < head.ChildNodes.Count; i++)
				{
					if (string.Compare(head.ChildNodes[i].Name, "script", StringComparison.OrdinalIgnoreCase) == 0)
					{
						sc = head.ChildNodes[i];
						break;
					}
				}
				foreach (string js in VPLUtil.JsFiles)
				{
					string f = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), js);
					if (File.Exists(f))
					{
						File.Copy(f, Path.Combine(_classCompiler.WebJsFolder, js), true);
						XmlNode jsNode = scriptNode.OwnerDocument.CreateElement(XhtmlTags.XH_script);
						XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
						XmlUtil.SetAttribute(jsNode, "src", string.Format(CultureInfo.InvariantCulture, "libjs/{0}", js));
						if (string.Compare(js, "pageStarter.js", StringComparison.OrdinalIgnoreCase) == 0)
						{
							XmlUtil.SetAttribute(jsNode, "id", "stData889923");
							XmlUtil.SetAttribute(jsNode, "stdlib", "2");
						}
						((XmlElement)jsNode).IsEmpty = false;
						head.InsertAfter(jsNode, sc);
					}
				}
			}
			foreach (object v in _rootClassId.ObjectList.Keys)
			{
				IExternalJavascriptIncludes jsf = v as IExternalJavascriptIncludes;
				if (jsf != null)
				{
					string[] ss = jsf.ExternalJavascriptIncludes;
					if (ss != null)
					{
						for (int i = 0; i < ss.Length; i++)
						{
							XmlNode jsNode = head.OwnerDocument.CreateElement(XhtmlTags.XH_script);
							XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
							XmlUtil.SetAttribute(jsNode, "src", ss[i]);
							((XmlElement)jsNode).IsEmpty = false;
							head.InsertBefore(jsNode, scriptNode);
						}
					}
				}
			}
			foreach (KeyValuePair<string, string> kv in _jsFiles)
			{
				XmlNode jsNode = head.OwnerDocument.CreateElement(XhtmlTags.XH_script);
				XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
				XmlUtil.SetAttribute(jsNode, "src", string.Format(CultureInfo.InvariantCulture, "libjs/{0}", kv.Key));
				((XmlElement)jsNode).IsEmpty = false;
				head.InsertBefore(jsNode, scriptNode);
				//
				string js = Path.Combine(_classCompiler.WebJavaScriptFolder, kv.Key);
				StreamWriter jsW = new StreamWriter(js, false, Encoding.UTF8);
				jsW.Write(kv.Value);
				jsW.Close();
			}
		}
		private void generateWebPage(CodeCompileUnit code)
		{
			VPLUtil.ClassCompileData = new Dictionary<string, string>();
			if (_project.ProjectType == EnumProjectType.WebAppAspx)
			{
				foreach (object v in _objMap.Keys)
				{
					IServerPageReferencer spr = v as IServerPageReferencer;
					if (spr != null)
					{
						spr.SetServerPageName(AspxFile);
					}
				}
				foreach (HtmlElement_BodyBase v in _rootClassId.UsedHtmlElements)
				{
					IServerPageReferencer spr = v as IServerPageReferencer;
					if (spr != null)
					{
						spr.SetServerPageName(AspxFile);
					}
				}
				generateAspxWebPage(code, ns);
			}
			else if (_project.ProjectType == EnumProjectType.WebAppPhp)
			{
				foreach (object v in _objMap.Keys)
				{
					IServerPageReferencer spr = v as IServerPageReferencer;
					if (spr != null)
					{
						spr.SetServerPageName(PhpFile);
					}
				}
				foreach (HtmlElement_BodyBase v in _rootClassId.UsedHtmlElements)
				{
					IServerPageReferencer spr = v as IServerPageReferencer;
					if (spr != null)
					{
						spr.SetServerPageName(PhpFile);
					}
				}
				generatePhpWebPage();
			}
		}
		private void generateWebApp(CodeCompileUnit code)
		{
			VPLUtil.JsFiles = new StringCollection();
			if (_project.ProjectType == EnumProjectType.WebAppAspx)
			{
				generateCode(code);
			}
			else if (_project.ProjectType == EnumProjectType.WebAppPhp)
			{
				generatePhpWebApp();
			}
			generateWebAppClientFile();
		}
		protected void generateCode(CodeCompileUnit code)
		{
			// The options for c# code generation.
			CodeGeneratorOptions o = new CodeGeneratorOptions();
			o.BlankLinesBetweenMembers = false;
			o.BracingStyle = "C";
			o.ElseOnClosing = false;
			o.IndentString = "    ";
			//
			CSharpCodeProvider cs = new CSharpCodeProvider();
			StringWriter sw = new StringWriter();
			try
			{
				cs.GenerateCodeFromCompileUnit(code, sw, o);
			}
			catch (Exception erCode)
			{
				logException(erCode);
				bRet = false;
			}
			sw.Flush();
			sw.Close();
			string sCode = sw.ToString();
			int pos = sCode.IndexOf("a tool.");
			if (pos > 0)
			{
				sCode = string.Format(CultureInfo.InvariantCulture, "{0}Limnor Studio.{1}", sCode.Substring(0, pos), sCode.Substring(pos + 7));
			}
			if (_staticEvents != null)
			{
				foreach (string ename in _staticEvents)
				{
					string p1 = "public event ";
					int n = 0;
					while (true)
					{
						pos = sCode.IndexOf(p1, n);
						if (pos < n)
						{
							break;
						}
						int pos1 = sCode.IndexOf(" ", pos + p1.Length);
						if (pos1 < 0)
						{
							break;
						}
						int pos2 = sCode.IndexOf(";", pos1 + 1);
						if (pos2 < 0)
						{
							break;
						}
						string name = sCode.Substring(pos1 + 1, pos2 - pos1 - 1).Trim();
						if (string.Compare(ename, name, StringComparison.Ordinal) == 0)
						{
							sCode = string.Format(CultureInfo.InvariantCulture, "{0}static {1}", sCode.Substring(0, pos), sCode.Substring(pos));
							break;
						}
						n = pos2;
					}
				}
			}
			//
			string sourceFile = _settings.SourceFilename + ".cs";
			StreamWriter sw0 = null;
			try
			{
				if (_isWebService)
				{
					int n0 = sCode.IndexOf("namespace");
					int n1 = sCode.IndexOf("{", n0 + 1);
					sCode = sCode.Substring(0, n0) + sCode.Substring(n1 + 1);
					int n2 = sCode.LastIndexOf('}');
					sCode = sCode.Substring(0, n2 - 1);
				}
				sw0 = new StreamWriter(sourceFile);
				sw0.Write(sCode);
			}
			catch (Exception ef)
			{
				logException(ef);
				bRet = false;
			}
			if (sw0 != null)
			{
				sw0.Close();
			}
			//
			if (_isWebService)
			{
				string asmxFile = _settings.SourceFilename + ".asmx";
				sw0 = null;
				try
				{
					sw0 = new StreamWriter(asmxFile);
					sw0.Write(string.Format(System.Globalization.CultureInfo.InvariantCulture, "<%@ WebService Language=\"C#\" CodeBehind=\"~/App_Code/{0}.cs\" Class=\"{1}\" %>", System.IO.Path.GetFileName(_settings.SourceFilename), _settings.Name));
				}
				catch (Exception ef)
				{
					logException(ef);
					bRet = false;
				}
				if (sw0 != null)
				{
					sw0.Close();
				}
				try
				{
					string sourceInAppCode = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(sourceFile), "App_Code");
					if (!System.IO.Directory.Exists(sourceInAppCode))
					{
						System.IO.Directory.CreateDirectory(sourceInAppCode);
					}
					sourceInAppCode = System.IO.Path.Combine(sourceInAppCode, System.IO.Path.GetFileName(sourceFile));
					System.IO.File.Copy(sourceFile, sourceInAppCode, true);
				}
				catch (Exception ef)
				{
					logException(ef);
					bRet = false;
				}
			}
			if (_objMap != null)
			{
				foreach (object obj in _objMap.Keys)
				{
					if (obj != _rootObject)
					{
						IPluginManager pm = obj as IPluginManager;
						if (pm != null)
						{
							processPluginManager(pm);
						}
					}
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pluginMan"></param>
		private void processPluginManager(IPluginManager pluginMan)
		{
			string cfgFile = pluginMan.PluginConfigurationFileFullpath;
			if (File.Exists(cfgFile))
			{
				pluginMan.RefreshPlugins();
				IList<string> dllFiles0 = pluginMan.PluginDllFiles;
				if (dllFiles0 != null && dllFiles0.Count > 0)
				{
					//merge sub folder names
					SortedList<int, List<string>> dfs = new SortedList<int, List<string>>();
					foreach (string sfx in dllFiles0)
					{
						if (!string.IsNullOrEmpty(sfx))
						{
							List<string> ss;
							if (!dfs.TryGetValue(sfx.Length, out ss))
							{
								ss = new List<string>();
								dfs.Add(sfx.Length, ss);
							}
							ss.Add(sfx);
						}
					}
					List<string> dllFolders = new List<string>();
					IEnumerator<KeyValuePair<int, List<string>>> ie = dfs.GetEnumerator();
					while (ie.MoveNext())
					{
						for (int k = 0; k < ie.Current.Value.Count; k++)
						{
							string s0 = ie.Current.Value[k];
							bool bfound = false;
							for (int i = 0; i < dllFolders.Count; i++)
							{
								if (s0.StartsWith(dllFolders[i], StringComparison.OrdinalIgnoreCase))
								{
									bfound = true;
									break;
								}
							}
							if (!bfound)
							{
								dllFolders.Add(Path.GetDirectoryName(s0));
							}
						}
					}
					//
					string relDir;
					string cfgFolderName = pluginMan.PluginConfigurationFoldername;
					string cfgDir;
					if (!Directory.Exists(_classCompiler.PluginsFolder))
					{
						Directory.CreateDirectory(_classCompiler.PluginsFolder);
					}
					if (string.IsNullOrEmpty(cfgFolderName))
					{
						cfgDir = _classCompiler.PluginsFolder;
						cfgFolderName = string.Empty;
						relDir = "plugins";
					}
					else
					{
						cfgDir = Path.Combine(_classCompiler.PluginsFolder, cfgFolderName);
						if (!Directory.Exists(cfgDir))
						{
							Directory.CreateDirectory(cfgDir);
						}
						relDir = string.Format(CultureInfo.InvariantCulture, "plugins\\{0}", cfgFolderName);
					}
					string targetConfig = Path.Combine(cfgDir, Path.GetFileName(cfgFile));
					File.Copy(cfgFile, targetConfig, true);
					string pluginRecs = Path.Combine(_classCompiler.PluginsFolder, "PluginRecords.xml");
					XmlDocument docRec = new XmlDocument();
					if (File.Exists(pluginRecs))
					{
						docRec.Load(pluginRecs);
					}
					if (docRec.DocumentElement == null)
					{
						XmlNode recsNode = docRec.CreateElement("Plugins");
						docRec.AppendChild(recsNode);
					}
					XmlNodeList recNodes = docRec.DocumentElement.SelectNodes(XmlTags.XML_Item);
					XmlNode recNd = null;
					foreach (XmlNode nd in recNodes)
					{
						string f = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_filename);
						if (!string.IsNullOrEmpty(f))
						{
							if (string.Compare(f, targetConfig, StringComparison.OrdinalIgnoreCase) == 0)
							{
								recNd = nd;
								break;
							}
						}
					}
					if (recNd == null)
					{
						recNd = docRec.CreateElement(XmlTags.XML_Item);
						docRec.DocumentElement.AppendChild(recNd);
						XmlUtil.SetAttribute(recNd, XmlTags.XMLATT_filename, targetConfig);
					}
					XmlUtil.SetAttribute(recNd, XmlTags.XMLATT_config, cfgFolderName);
					docRec.Save(pluginRecs);
					Dictionary<string, string> folderMapping = new Dictionary<string, string>();
					StringCollection scFolderNames = new StringCollection();
					foreach (string s in dllFolders)
					{
						if (Directory.Exists(s))
						{
							string sf = s.ToLowerInvariant();
							if (!folderMapping.ContainsKey(sf))
							{
								string sTargetFolder;
								string sfname = Path.GetFileName(sf);
								if (string.IsNullOrEmpty(sfname))
								{
									sTargetFolder = cfgDir;
								}
								else
								{
									int n = 1;
									string sbase = sfname.ToLowerInvariant();
									sfname = sbase;
									while (scFolderNames.Contains(sfname))
									{
										n++;
										sfname = string.Format(CultureInfo.InvariantCulture, "{0}{1}", sbase, n);
									}
									scFolderNames.Add(sfname);
									sTargetFolder = Path.Combine(cfgDir, sfname);
									if (!Directory.Exists(sTargetFolder))
									{
										Directory.CreateDirectory(sTargetFolder);
									}
								}
								folderMapping.Add(sf, sfname);
							}
						}
					}
					XmlDocument doc = new XmlDocument();
					doc.Load(targetConfig);
					XmlNodeList fnodes = doc.SelectNodes(string.Format(CultureInfo.InvariantCulture,
						"//*[@{0}]", XmlTags.XMLATT_filename));
					foreach (KeyValuePair<string, string> kv in folderMapping)
					{
						string dest = Path.Combine(cfgDir, kv.Value);
						FileUtilities.CopyDirs(kv.Key, dest, "*.*", null);
						foreach (XmlNode fn in fnodes)
						{
							string sf0 = XmlUtil.GetAttribute(fn, XmlTags.XMLATT_filename);
							if (!string.IsNullOrEmpty(sf0))
							{
								if (sf0.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
								{
									sf0 = sf0.ToLowerInvariant();
									sf0 = sf0.Replace(kv.Key, string.Format(CultureInfo.InvariantCulture,
										"{0}\\{1}\\{2}", XmlUtil.APPFOLDER, relDir, kv.Value));
									XmlUtil.SetAttribute(fn, XmlTags.XMLATT_filename, sf0);
								}
							}
						}
					}
					doc.Save(targetConfig);
				}
			}
		}
		private void addDebugCode(bool isEntryPoint)
		{
			if (_debug)
			{
				CodeMemberField cmf = new CodeMemberField(typeof(LimnorDebugger), LimnorDebugger.Debugger);
				td.Members.Add(cmf);
				if (isEntryPoint)
				{
					cmf.Attributes = (MemberAttributes.Static | MemberAttributes.Private);
					CodeEntryPointMethod cc = getEntryPoint();
					int n = 0;
					for (int i = 0; i < cc.Statements.Count; i++)
					{
						CodeExpressionStatement cesm = cc.Statements[i] as CodeExpressionStatement;
						if (cesm != null)
						{
							CodeMethodInvokeExpression cm = cesm.Expression as CodeMethodInvokeExpression;
							if (string.CompareOrdinal(cm.Method.MethodName, "SetUnhandledExceptionMode") == 0)
							{
								if (i > n)
								{
									n = i;
								}
							}
							else if (string.CompareOrdinal(cm.Method.MethodName, "SetCompatibleTextRenderingDefault") == 0)
							{
								if (i > n)
								{
									n = i;
								}
							}
						}
					}
					cc.Statements.Insert(n + 1, new CodeAssignStatement(new CodeVariableReferenceExpression(LimnorDebugger.Debugger),
						new CodeObjectCreateExpression(typeof(LimnorDebugger), new CodePrimitiveExpression(_project.ProjectFile), new CodePrimitiveExpression(this._xmlFile))
						));
				}
				else
				{
					CodeMemberMethod cc = getDefaultConstructor();
					cc.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(LimnorDebugger.Debugger),
						new CodeObjectCreateExpression(typeof(LimnorDebugger), new CodePrimitiveExpression(_project.ProjectFile), new CodePrimitiveExpression(this._xmlFile))
						));
					cc.Statements.Add(
						new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								new CodeVariableReferenceExpression(LimnorDebugger.Debugger),
								"OnCreateComponent",
								new CodePrimitiveExpression(_objMap.ObjectKey),
								new CodeThisReferenceExpression()
								)
							)
						);
				}
				if (isStaticClass)
				{
					//add property holder
					CodeMemberField cmf1 = new CodeMemberField(typeof(StaticPropertyHolder), PropertyHolderVarName);
					td.Members.Add(cmf1);
					cmf1.Attributes = (MemberAttributes.Static | MemberAttributes.Private);
					CodeMemberMethod cmm;
					if (isEntryPoint)
					{
						cmm = getEntryPoint();
					}
					else
					{
						cmm = getDefaultConstructor();
					}
					CodeStatement csH = new CodeAssignStatement(new CodeVariableReferenceExpression(PropertyHolderVarName),
						new CodeObjectCreateExpression(typeof(StaticPropertyHolder), new CodeTypeOfExpression(_settings.TypeName))
						);
					int nPos = -1;
					for (int i = 0; i < cmm.Statements.Count; i++)
					{
						CodeExpressionStatement ces = cmm.Statements[i] as CodeExpressionStatement;
						if (ces != null)
						{
							CodeMethodInvokeExpression cmie = ces.Expression as CodeMethodInvokeExpression;
							if (cmie != null)
							{
								if (string.CompareOrdinal(cmie.Method.MethodName, "Run") == 0)
								{
									CodeTypeReferenceExpression ctre = cmie.Method.TargetObject as CodeTypeReferenceExpression;
									if (string.CompareOrdinal(ctre.Type.BaseType, "System.Windows.Forms.Application") == 0)
									{
										nPos = i;
										break;
									}
								}
							}
						}
					}
					if (nPos > 0)
					{
						cmm.Statements.Insert(nPos, csH);
					}
					else
					{
						cmm.Statements.Add(
							csH
							);
					}
				}
			}
		}
		private Type getFieldType(string fieldName)
		{
			foreach (object v in _objMap.Keys)
			{
				IComponent ic = v as IComponent;
				if (ic != null)
				{
					if (ic.Site != null)
					{
						if (string.CompareOrdinal(fieldName, ic.Site.Name) == 0)
						{
							return v.GetType();
						}
					}
				}
			}
			return typeof(object);
		}
		private CodeEntryPointMethod getEntryPoint()
		{
			CodeEntryPointMethod ret = null;
			foreach (CodeTypeMember m in td.Members)
			{
				CodeEntryPointMethod c = m as CodeEntryPointMethod;
				if (c != null)
				{
					ret = c;
					break;
				}
			}
			return ret;
		}
		private CodeTypeConstructor getStaticConstructor()
		{
			CodeTypeConstructor ret = null;
			foreach (CodeTypeMember m in td.Members)
			{
				CodeTypeConstructor c = m as CodeTypeConstructor;
				if (c != null)
				{
					if ((c.Attributes & MemberAttributes.Static) == MemberAttributes.Static)
					{
						ret = c;
						break;
					}
				}
			}
			if (ret == null)
			{
				ret = new CodeTypeConstructor();
				ret.Attributes = MemberAttributes.Static;
				td.Members.Add(ret);
				ret.Attributes = MemberAttributes.Static;
			}
			return ret;
		}
		private CodeMemberMethod getInitializeMethod()
		{
			foreach (CodeTypeMember m in td.Members)
			{
				CodeMemberMethod mm = m as CodeMemberMethod;
				if (mm != null)
				{
					if (string.CompareOrdinal(mm.Name, "InitializeComponent") == 0)
					{
						return mm;
					}
				}
			}
			return null;
		}
		private Control getControlByName(string name)
		{
			foreach (object o in _objMap.Keys)
			{
				Control c = o as Control;
				if (c != null)
				{
					if (string.CompareOrdinal(name, c.Name) == 0)
					{
						return c;
					}
				}
			}
			return null;
		}
		private void setCustomPropertyValues()
		{
			PropertyDescriptorCollection props = _rootClassId.CustomPropertyCollection;
			if (props.Count > 0)
			{
				CodeMemberMethod initMethod = getInitializeMethod();
				if (initMethod != null)
				{
					MethodClass mc = createGenericMethod("cp", null);
					mc.MethodCode = initMethod;
					int n = initMethod.Statements.Count;
					for (int i = 0; i < initMethod.Statements.Count; i++)
					{
						CodeCommentStatement ccs = initMethod.Statements[i] as CodeCommentStatement;
						if (ccs != null)
						{
							if (!string.IsNullOrEmpty(ccs.Comment.Text))
							{
								if (ccs.Comment.Text == _objMap.Name)
								{
									n = i + 1;
									ccs = initMethod.Statements[i + 1] as CodeCommentStatement;
									if (ccs != null)
									{
										n++;
									}
									break;
								}
							}
						}
					}
					foreach (PropertyDescriptor p in props)
					{
						PropertyClassDescriptor pc = (PropertyClassDescriptor)p;
						if (p.ShouldSerializeValue(_rootObject))
						{
							object v = p.GetValue(null);
							if (v != null)
							{
								CodeExpression ceL = pc.CustomProperty.GetReferenceCode(mc, initMethod.Statements, true);
								CodeExpression ceR = ObjectCreationCodeGen.ObjectCreationCode(v);
								CodeAssignStatement cas = new CodeAssignStatement(ceL, ceR);
								initMethod.Statements.Insert(n++, cas);
							}
						}
					}
				}
			}
		}
		private CodeMemberMethod getMethod(string name)
		{
			foreach (CodeTypeMember m in td.Members)
			{
				CodeMemberMethod mm = m as CodeMemberMethod;
				if (mm != null)
				{
					if (string.CompareOrdinal(mm.Name, name) == 0)
					{
						return mm;
					}
				}
			}
			return null;
		}
		private CodeMemberMethod getMainMethod()
		{
			return getMethod("Main");
		}
		private MethodClass createGenericMethod(string baseName, List<ParameterClass> ps)
		{
			string methodName = baseName + "_" + Guid.NewGuid().GetHashCode().ToString("x");
			MethodClass mc = new MethodClass(_rootClassId);
			mc.MethodName = methodName;
			mc.SetCompilerData(this);
			mc.MemberId = (UInt32)(Guid.NewGuid().GetHashCode());
			mc.Parameters = ps;
			return mc;
		}
		private bool isStartEvent(IEvent e)
		{
			if (e.Owner != null)
			{
				if (typeof(LimnorConsole).IsAssignableFrom(e.Owner.ObjectType))
				{
					if (string.Compare(e.Name, "Start", StringComparison.Ordinal) == 0)
					{
						return true;
					}
				}
			}
			return false;
		}
		/// <summary>
		/// link events to delegates.
		/// 1. create a local method with the same signature as the event handler type Invoke method
		/// 2. add code for each action into this method
		/// ?? a custom event always use EventHandler and thus the method signature is always (object, EventArgs)
		/// </summary>
		private void createEventHandlers()
		{
			bool isConsoleAppClass = (_project.ProjectType == EnumProjectType.Console && IsEntryPoint);
			bool isWinAppClass = _project.IsWinFormApp && IsEntryPoint;
			bool isScreensaverAppClass = (_project.ProjectType == EnumProjectType.ScreenSaver && IsEntryPoint);
			//
			Dictionary<string, string> drawRepeatItems = new Dictionary<string, string>();
			foreach (KeyValuePair<object, UInt32> kv in _rootClassId.ObjectList)
			{
				DrawingControl di = kv.Key as DrawingControl;
				if (di != null && di.Item != null)
				{
					DrawDataRepeater ddr = di.Item.Container as DrawDataRepeater;
					if (ddr != null)
					{
						drawRepeatItems.Add(di.Name, ddr.Name);
					}
				}
			}
			List<EventAction> events = _rootClassId.EventHandlers;
			if (isConsoleAppClass)
			{
				EventAction start = null;
				List<EventAction> es = new List<EventAction>();
				foreach (EventAction ea in events)
				{
					if (isStartEvent(ea.Event))
					{
						start = ea;
					}
					else
					{
						es.Add(ea);
					}
				}
				if (start != null)
				{
					es.Add(start);
				}
				events = es;
			}
			foreach (EventAction ea in events)
			{
				if (_rootClassId.IsWebPage)
				{
					if (DesignUtil.IsWebClientObject(ea.Event.Owner))
					{
						break;
					}
				}
				Type eventArgumentType = null;
				ICustomEventMethodDescriptor ce = ea.Event.Owner.ObjectInstance as ICustomEventMethodDescriptor;
				if (ce != null)
				{
					eventArgumentType = ce.GetEventArgumentType(ea.Event.Name);
				}
				List<HandlerMethodID> handlersForType = new List<HandlerMethodID>();
				foreach (TaskID tid in ea.TaskIDList)
				{
					HandlerMethodID hmid = tid as HandlerMethodID;
					if (hmid != null)
					{
						if (hmid.HandlerMethod.ForAllTypes)
						{
							handlersForType.Add(hmid);
						}
					}
				}
				int COMPILE_SINGLE = 0;
				int COMPILE_TYPE = 1;
				int COMPILE_NONE = 2;
				int compile_target = COMPILE_SINGLE;
				while (compile_target < COMPILE_NONE)
				{
					//create an event handler method, every call generates a unique name
					MethodClass mc = ea.Event.CreateHandlerMethod(this);
					if (mc == null)
					{
						break;
					}
					string asname = ea.GetLocalHandlerName();
					if (!string.IsNullOrEmpty(asname))
					{
						if (compile_target == COMPILE_SINGLE)
						{
							mc.MethodName = asname;
						}
						else
						{
							break;
						}
					}
					bool isEmpty = true;
					mc.Owner = _rootClassId;
					CodeMemberMethod eh = new CodeMemberMethod();
					mc.MethodCode = eh;
					eh.Name = mc.Name;
					if (isStaticClass)
					{
						eh.Attributes |= MemberAttributes.Static;
					}
					td.Members.Add(eh);
					//
					if (!isStartEvent(ea.Event))
					{
						if (mc.Parameters != null && mc.Parameters.Count > 0)
						{
							bool bCastSender = (compile_target == COMPILE_TYPE);
							if (!bCastSender)
							{
								foreach (TaskID tid in ea.TaskIDList)
								{
									HandlerMethodID hmid = tid as HandlerMethodID;
									if (hmid != null)
									{
										if (!hmid.HandlerMethod.ForAllTypes)
										{
											bCastSender = true;
											break;
										}
									}
								}
							}
							for (int i = 0; i < mc.Parameters.Count; i++)
							{
								if (bCastSender)
								{
									if (i == 0)
									{
										if (typeof(object).Equals(mc.Parameters[0].ParameterLibType) && string.CompareOrdinal(mc.Parameters[0].Name, "sender") == 0)
										{
											Type senderType = VPLUtil.GetObjectType(ea.Event.Owner.ObjectType);
											if (!typeof(object).Equals(senderType))
											{
												if (senderType != null)
												{
													object[] attrs = senderType.GetCustomAttributes(typeof(TypeMappingAttribute), true);
													if (attrs != null && attrs.Length > 0)
													{
														TypeMappingAttribute tma = attrs[0] as TypeMappingAttribute;
														if (tma != null)
														{
															if (typeof(DrawingItem).IsAssignableFrom(tma.MappedType))
															{
																senderType = tma.MappedType;
															}
														}
													}
												}
												eh.Parameters.Add(new CodeParameterDeclarationExpression(mc.Parameters[i].TypeString, "sender0"));
												eh.Statements.Add(new CodeVariableDeclarationStatement(senderType, "sender", new CodeCastExpression(senderType, new CodeArgumentReferenceExpression("sender0"))));
												//bCastSender = true;
												continue;
											}
										}
									}
								}
								if (eventArgumentType != null && i == 1)
								{
									eh.Parameters.Add(new CodeParameterDeclarationExpression(mc.Parameters[i].TypeString, "e0"));
									eh.Statements.Add(new CodeVariableDeclarationStatement(eventArgumentType, mc.Parameters[i].Name, new CodeCastExpression(eventArgumentType, new CodeArgumentReferenceExpression("e0"))));
									continue;
								}
								eh.Parameters.Add(new CodeParameterDeclarationExpression(mc.Parameters[i].TypeString, mc.Parameters[i].Name));
							}
						}
					}
					//get the method for adding event-handlder assignment code
					CodeMemberMethod cmm;
					//
					if (isConsoleAppClass)
					{
						cmm = getMainMethod();
					}
					else if (isScreensaverAppClass)
					{
						if (string.CompareOrdinal(ea.Event.Name, "ScreenSaverConfiguration") == 0)
						{
							cmm = getMethod("OnConfigure");
						}
						else
						{
							//for ScreenSaverPreview
							cmm = getMethod("OnPreview");
						}
					}
					else
					{
						if (isStaticClass)
						{
							cmm = getStaticConstructor();
						}
						else
						{
							CodeMemberMethod initMethod = getInitializeMethod();
							if (initMethod == null)
							{
								throw new DesignerException("InitializeComponent not found");
							}
							cmm = initMethod;
						}
					}
					//compile handler code=====================================================
					CodeExpression sender = null;//to be used for firing debugging events
					///find out the sender
					if (_debug)
					{
						if (isStaticClass)
						{
							sender = new CodeVariableReferenceExpression(PropertyHolderVarName); //new CodeObjectCreateExpression(typeof(StaticPropertyHolder), new CodeTypeOfExpression(_settings.TypeName));
						}
						else
						{
							if (mc.ParameterCount > 0)
							{
								if (mc.Parameters[0].IsLibType && mc.Parameters[0].ObjectType.Equals(typeof(object)))
								{
									sender = new CodeArgumentReferenceExpression(mc.Parameters[0].Name);
								}
							}
							if (sender == null)
							{
								sender = new CodeThisReferenceExpression();
							}
						}
					}
					if (_debug)
					{
						//if it is alread at the break point then return immediately, preventing multiple-clicks to pass break point
						eh.Statements.Add(
							new CodeConditionStatement(
								new CodeBinaryOperatorExpression(
									 new CodeMethodInvokeExpression(DebuggerRef, "AtBreak", new CodeFieldReferenceExpression(new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(System.Threading.Thread)), "CurrentThread"), "ManagedThreadId")),
									 CodeBinaryOperatorType.ValueEquality,
									 new CodePrimitiveExpression(true)
									),
								new CodeMethodReturnStatement()
								)
							);
						//check EnterEvent break point
						eh.Statements.Add(new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								DebuggerRef, "EnterEvent",
								new CodePrimitiveExpression(ea.Event.Owner.ObjectKey),
								new CodePrimitiveExpression(ea.Event.Name),
								sender)
								)
							);
					}
					//add actions into the event handling method
					int idx = 0;
					foreach (TaskID tid in ea.TaskIDList)
					{
						if (_debug)
						{
							//check BeforeExecuteEventAction break point
							eh.Statements.Add(new CodeExpressionStatement(
								new CodeMethodInvokeExpression(
									DebuggerRef, "BeforeExecuteEventAction",
									new CodePrimitiveExpression(ea.Event.Owner.ObjectKey),
									new CodePrimitiveExpression(ea.Event.Name),
									new CodePrimitiveExpression(idx),//tid.WholeTaskId),
									sender)
									)
								);
						}
						HandlerMethodID hmid = tid as HandlerMethodID;
						if (hmid != null)
						{
							if (compile_target == COMPILE_SINGLE)
							{
								if (hmid.HandlerMethod.ForAllTypes)
								{
									continue;
								}
							}
							if (compile_target == COMPILE_TYPE)
							{
								if (!hmid.HandlerMethod.ForAllTypes)
								{
									continue;
								}
							}
							TraceLogClass.TraceLog.Trace("Compile event method {0}", hmid.HandlerMethod.Name);
							if (!string.IsNullOrEmpty(hmid.HandlerMethod.Description))
							{
								eh.Comments.Add(new CodeCommentStatement(hmid.HandlerMethod.Description, true));
							}
							TraceLogClass.TraceLog.IndentIncrement();
							hmid.HandlerMethod.MethodCode = eh;
							hmid.HandlerMethod.ExportCode(this, eh, false);
							TraceLogClass.TraceLog.IndentDecrement();
							TraceLogClass.TraceLog.Trace("End of compile event method {0}", hmid.HandlerMethod.Name);
							isEmpty = false;
						}
						else
						{
							if (compile_target != COMPILE_SINGLE)
							{
								continue;
							}
							IAction a = GetPublicAction(tid);
							if (a == null)
							{
								_classCompiler.LogError("Action {0} not found from {1}", tid.ToString(), _xmlFile);
							}
							else
							{
								MethodActionForeach loop = a as MethodActionForeach;
								if (loop != null)
								{
									loop.MethodCode = eh;
									loop.ExportCode(this, eh, false);
								}
								else
								{
									a.ExportCode(null, null, this, mc, eh, eh.Statements, _debug);
								}
								isEmpty = false;
							}
						}
						idx++;
					}
					if (_debug)
					{
						//check LeaveEvent break point
						eh.Statements.Add(new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								DebuggerRef, "LeaveEvent",
								new CodePrimitiveExpression(ea.Event.Owner.ObjectKey),
								new CodePrimitiveExpression(ea.Event.Name),
								sender)
								)
							);
					}
					//hook the handler=========================================================
					if (ea.GetAssignActionId() == 0)
					{
						if (isWinAppClass && string.Compare(ea.Event.Name, "DuplicationDetected", StringComparison.Ordinal) == 0)
						{
							LimnorWinApp wapp = (LimnorWinApp)_rootObject;
							wapp.SetDuplicationHandlers(eh.Statements);
							td.Members.Remove(eh);
							compile_target = COMPILE_NONE;
						}
						else if (isWinAppClass && string.Compare(ea.Event.Name, "BeforeStart", StringComparison.Ordinal) == 0)
						{
							LimnorWinApp wapp = (LimnorWinApp)_rootObject;
							wapp.SetBeforeStartHandlers(eh.Statements);
							td.Members.Remove(eh);
							compile_target = COMPILE_NONE;
						}
						else if (isStartEvent(ea.Event) || isScreensaverAppClass)
						{
							//for the consolde application class, directly add the method call 
							cmm.Statements.AddRange(eh.Statements);
							td.Members.Remove(eh);
							compile_target = COMPILE_NONE;
						}
						else
						{
							//attach event
							if (isEmpty)
							{
								td.Members.Remove(eh);
							}
							else
							{
								List<CodeEventReferenceExpression> evs = new List<CodeEventReferenceExpression>();
								CodeExpression methodTarget;
								if (compile_target == COMPILE_SINGLE)
								{
									evs.Add((CodeEventReferenceExpression)ea.Event.GetReferenceCode(mc, cmm.Statements, false));
								}
								else if (compile_target == COMPILE_TYPE)
								{
									Type eot = ea.Event.Holder.ObjectType;
									foreach (object v in _objMap.Keys)
									{
										if (eot.IsAssignableFrom(v.GetType()))
										{
											IClass ic = _rootClassId.CreateMemberPointer(v);
											CodeExpression targetObject = ic.GetReferenceCode(mc, cmm.Statements, false);
											evs.Add(new CodeEventReferenceExpression(targetObject, ea.Event.Name));
										}
									}
								}
								foreach (CodeEventReferenceExpression ceRef in evs)
								{
									if (isStaticClass)
										methodTarget = new CodeTypeReferenceExpression(_rootClassId.CodeName);
									else
										methodTarget = new CodeThisReferenceExpression();
									//attached method mc to the event
									//find the last assignment as the insertion point
									int k = cmm.Statements.Count - 1;
									CodeFieldReferenceExpression fr = ceRef.TargetObject as CodeFieldReferenceExpression;
									if (fr != null)
									{
										for (int i = 0; i < cmm.Statements.Count; i++)
										{
											CodeAssignStatement cas = cmm.Statements[i] as CodeAssignStatement;
											if (cas != null)
											{
												if (cas.Left == ceRef.TargetObject)
												{
													k = i;
												}
												else
												{
													CodePropertyReferenceExpression pr = cas.Left as CodePropertyReferenceExpression;
													if (pr != null)
													{
														if (pr.TargetObject == ceRef.TargetObject)
														{
															k = i;
														}
														else
														{
															CodeFieldReferenceExpression fr2 = pr.TargetObject as CodeFieldReferenceExpression;
															if (fr2 != null)
															{
																if (fr.FieldName == fr2.FieldName)
																{
																	k = i;
																}
															}
														}
													}
												}
											}
										}
									}
									CodeDelegateCreateExpression cdce = new CodeDelegateCreateExpression(new CodeTypeReference(ea.Event.EventHandlerType.TypeString),
											methodTarget, mc.Name);
									cmm.Statements.Insert(k + 1, new CodeAttachEventStatement(ceRef,cdce));
									CodeFieldReferenceExpression cfre = ceRef.TargetObject as CodeFieldReferenceExpression;
									if (cfre != null && cfre.TargetObject is CodeThisReferenceExpression)
									{
										string containerName;
										if (drawRepeatItems.TryGetValue(cfre.FieldName, out containerName))
										{
											CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
											cmie.Method = new CodeMethodReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), containerName), "AddHandler");
											cmie.Parameters.Add(new CodePrimitiveExpression(cfre.FieldName));
											cmie.Parameters.Add(new CodePrimitiveExpression(ceRef.EventName));
											cmie.Parameters.Add(cdce);
											cmm.Statements.Insert(k + 2, new CodeExpressionStatement(cmie));
										}
									}
								}
							}
						}
					}
					compile_target++;
				}// end of while (compile_target < COMPILE_NONE)
			} //end of foreach (EventAction ea in _rootClassId.EventHandlers)
			if (isWinAppClass)
			{
				LimnorWinApp wapp = (LimnorWinApp)_rootObject;
				wapp.AddSingleInstanceDetection();
			}
		}
		#endregion
		#region Webpage compiling
		private List<ActionClass> addTreeViewSupport(WebHandlerBlock block)
		{
			List<ActionClass> htmlTreeViewActions = new List<ActionClass>();
			for (int n = 0; n < 3; n++)
			{
				List<TaskID> lt = block[n];
				foreach (TaskID tid in lt)
				{
					IAction a = null;
					HandlerMethodID hmid = tid as HandlerMethodID;
					if (hmid != null)
					{
					}
					else
					{
						a = GetPublicAction(tid);
					}
					if (a != null)
					{
						if (a.MethodOwner != null && a.ActionMethod != null)
						{
							if (string.CompareOrdinal(a.ActionMethod.MethodName, "LoadFromCategoryQuery") == 0)
							{
								HtmlTreeView htv = a.MethodOwner.ObjectInstance as HtmlTreeView;
								if (htv != null)
								{
									ActionClass act = a as ActionClass;
									if (act != null)
									{
										ParameterValue pv = new ParameterValue(a);
										pv.ValueType = EnumValueType.Property;
										if (htv.PrimaryKey == null)
										{
											pv.SetDataType(typeof(string));
										}
										else
										{
											pv.SetDataType(htv.PrimaryKey.Type);
										}
										PropertyPointer pp = new PropertyPointer();
										pp.Owner = a.MethodOwner;
										pp.MemberName = "currentPrimaryKeyValue";
										pp.SetRunAt(EnumWebRunAt.Client);
										pp.SetPropertyType(pv.DataType);
										pv.Property = pp;
										act.InsertParameterValue(0, pv);
										htmlTreeViewActions.Add(act);
									}
								}
							}
						}
					}
				}
			}
			return htmlTreeViewActions;
		}
		private void collectUploadsFromActionReturns(List<TaskID> clientActions, MethodClass mcUploadedValues, Dictionary<UInt32, IAction> actions, PropertyPointerList uploads, StringCollection uploadedValuesPhp, CodeMemberMethod uploadedValuesAspx)
		{
			foreach (TaskID tid in clientActions)
			{
				HandlerMethodID hmid = tid as HandlerMethodID;
				if (hmid != null)
				{
					WebClientEventHandlerMethod wceh = hmid.HandlerMethod as WebClientEventHandlerMethod;
					if (wceh != null)
					{
						hmid.HandlerMethod.LoadActionInstances();
						hmid.HandlerMethod.SetActions(actions);
						List<IAction> acts = hmid.HandlerMethod.GetActions();
						if (acts != null && acts.Count > 0)
						{
							foreach (IAction act in acts)
							{
								if (act.ReturnReceiver != null)
								{
									ISourceValuePointer sv = act.ReturnReceiver as ISourceValuePointer;
									if (sv != null && sv.IsWebServerValue() && !sv.IsWebClientValue())
									{
										uploads.Add(sv);
										if (_project.ProjectType == EnumProjectType.WebAppAspx)
										{
											CodeExpression ce = act.ReturnReceiver.GetReferenceCode(mcUploadedValues, uploadedValuesAspx.Statements, false);
											CodeExpression cv = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("clientRequest"), "GetStringValue", new CodePrimitiveExpression(sv.DataPassingCodeName));
											//generate   TypeConverter tc = TypeDescriptor.GetConverter(type);
											//           ce = tc.ConvertFrom(cv);
											string v_tcv = string.Format(CultureInfo.InvariantCulture, "c{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
											CodeVariableDeclarationStatement tcv = new CodeVariableDeclarationStatement(typeof(TypeConverter), v_tcv, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(TypeDescriptor)), "GetConverter", new CodeTypeReferenceExpression(act.ReturnReceiver.ObjectType)));
											uploadedValuesAspx.Statements.Add(new CodeAssignStatement(ce, new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(v_tcv), "ConvertFrom", cv)));
										}
										else if (_project.ProjectType == EnumProjectType.WebAppPhp)
										{
											StringCollection svc = new StringCollection();
											string s = string.Format(CultureInfo.InvariantCulture,
												"{0}=$this->jsonFromClient->values->{1};\r\n", act.ReturnReceiver.GetPhpScriptReferenceCode(svc), sv.DataPassingCodeName);
											if (svc.Count > 0)
											{
												uploadedValuesPhp.Add(svc.ToString());
											}
											uploadedValuesPhp.Add(s);
										}
									}
								}
							}
						}
					}
				}
				else
				{
					IAction a = GetPublicAction(tid);
					if (a != null)
					{
						if (a.ReturnReceiver != null)
						{
							ISourceValuePointer sv = a.ReturnReceiver as ISourceValuePointer;
							if (sv != null && sv.IsWebServerValue() && !sv.IsWebClientValue())
							{
								uploads.Add(sv);
								if (_project.ProjectType == EnumProjectType.WebAppAspx)
								{
									CodeExpression ce = a.ReturnReceiver.GetReferenceCode(mcUploadedValues, uploadedValuesAspx.Statements, false);
									CodeExpression cv = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("clientRequest"), "GetStringValue", new CodePrimitiveExpression(sv.DataPassingCodeName));
									//generate  TypeConverter tc = TypeDescriptor.GetConverter(type);
									//          ce = tc.ConvertFrom(cv);
									string v_tcv = string.Format(CultureInfo.InvariantCulture, "c{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
									CodeVariableDeclarationStatement tcv = new CodeVariableDeclarationStatement(typeof(TypeConverter), v_tcv, new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(TypeDescriptor)), "GetConverter", new CodeTypeReferenceExpression(a.ReturnReceiver.ObjectType)));
									uploadedValuesAspx.Statements.Add(new CodeAssignStatement(ce, new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(v_tcv), "ConvertFrom", cv)));
								}
								else if (_project.ProjectType == EnumProjectType.WebAppPhp)
								{
									StringCollection svc = new StringCollection();
									string s = string.Format(CultureInfo.InvariantCulture,
										"{0}=$this->jsonFromClient->values->{1};\r\n", a.ReturnReceiver.GetPhpScriptReferenceCode(svc), sv.DataPassingCodeName);
									if (svc.Count > 0)
									{
										uploadedValuesPhp.Add(svc.ToString());
									}
									uploadedValuesPhp.Add(s);
								}
							}
						}
					}
				}
			}
		}
		private void collectUploadsFromActionParameters(List<TaskID> serverActions, List<ActionClass> htmlTreeViewActions, EventAction ea, SourceValueDictionary downloads, MethodClass mcUploadedValues, Dictionary<UInt32, IAction> actions, PropertyPointerList uploads, StringCollection uploadedValuesPhp, CodeMemberMethod uploadedValuesAspx)
		{
			foreach (TaskID tid in serverActions)
			{
				HandlerMethodID hmid = tid as HandlerMethodID;
				if (hmid != null)
				{
					WebClientEventHandlerMethod wceh = hmid.HandlerMethod as WebClientEventHandlerMethod;
					if (wceh != null)
					{
						hmid.HandlerMethod.LoadActionInstances();
						hmid.HandlerMethod.SetActions(actions);
						hmid.HandlerMethod.CollectSourceValues(tid.TaskId);
						uploads.AddPointerList(hmid.HandlerMethod.UploadProperties);
						//
						if (hmid.HandlerMethod.ParameterCount > 0)
						{
							//server method parameters must be uploaded
							foreach (ParameterClass pc in hmid.HandlerMethod.Parameters)
							{
								uploads.AddPointer(pc);
							}
						}
						//
						List<MethodClass> ml = new List<MethodClass>();
						hmid.HandlerMethod.GetCustomMethods(ml);
						foreach (MethodClass m in ml)
						{
							m.CollectSourceValues(tid.TaskId);
							IList<ISourceValuePointer> sl = m.UploadProperties;
							if (sl != null && sl.Count > 0)
							{
								uploads.AddPointerList(sl);
							}
						}
					}
				}
				else
				{
					IAction a = GetPublicAction(tid);
					if (a != null)
					{
						if (a.ActionMethod != null && a.ActionMethod.Owner != null)
						{
							IFormSubmitter fs = a.ActionMethod.Owner.ObjectInstance as IFormSubmitter;
							if (fs != null && fs.IsSubmissionMethod(a.ActionMethod.MethodName))
							{
								continue;
							}
						}
						IList<ISourceValuePointer> l = a.GetClientProperties(ea.EventHandlerId/*tid.TaskId*/);
						if (l != null && l.Count > 0)
						{
							ActionClass ac = a as ActionClass;
							if (ac != null)
							{
								if (htmlTreeViewActions.Contains(ac))
								{
									Dictionary<ISourceValuePointer, string> pcodes = new Dictionary<ISourceValuePointer, string>();
									foreach (ISourceValuePointer svp in l)
									{
										pcodes.Add(svp, null);
									}
									ac.SetUserData("PCODE", pcodes);
								}
							}
							uploads.AddPointerList(l);
						}
						ISourceValuePointer p = a.ReturnReceiver as ISourceValuePointer;
						if (p != null)
						{
							if (p.IsWebClientValue())
							{
								if (!downloads.ValueExists(p))
								{
									PropertyPointerList pl;
									if (!downloads.TryGetValue(tid.TaskOrder, out pl))
									{
										pl = new PropertyPointerList();
										downloads.Add(tid.TaskOrder, pl);
									}
									p.SetTaskId(ea.EventHandlerId/*tid.TaskId*/);
									pl.AddPointer(p);
								}
							}
						}
					}
				}
			}
		}
		private bool IsMethodAction(IObjectPointer owner, IAction a)
		{
			if (a != null)
			{
				SetterPointer sp = a.ActionMethod as SetterPointer;
				if (sp != null)
				{
					if (sp.Value != null)
					{
						IList<ISourceValuePointer> vs = sp.Value.GetValueSources();
						if (vs != null && vs.Count > 0)
						{
							foreach (ISourceValuePointer v in vs)
							{
								if (v != null)
								{
									IObjectIdentity oi = v as IObjectIdentity;
									if (owner.IsSameObjectRef(oi))
									{
										return true;
									}
									else
									{
										MathNodePointer mp = v as MathNodePointer;
										if (mp != null)
										{
											oi = mp.ValueOwner as IObjectIdentity;
											if (owner.IsSameObjectRef(oi))
											{
												return true;
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if (a.ActionMethod.Owner != null)
					{
						if (a.ActionMethod.Owner.IsSameObjectRef(owner))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		/// <summary>
		/// for a read-only download value, only download from the first block? not easy to determine
		/// only download if there is an action's owner is the value owner? it cannot cover all cases.
		/// A read-only server value used in client, add it to download list if
		/// 1. not in a download list in a previous block
		///  or
		/// 2. used in an action using a method of the same owner
		/// uncoverred case: a property using a getter and generating different values for each call
		///  a). this case does not exist in PHP because PHP does not use getter
		///  b). .Net uses getter but such a property is rare
		/// </summary>
		/// <param name="downloadActions"></param>
		/// <param name="htmlTreeViewActions"></param>
		/// <param name="ea"></param>
		/// <param name="downloads"></param>
		/// <param name="mcUploadedValues"></param>
		/// <param name="actions"></param>
		/// <param name="uploads"></param>
		/// <param name="uploadedValuesPhp"></param>
		/// <param name="uploadedValuesAspx"></param>
		private void collectDownloadValues(WebHandlerBlock block, List<ActionClass> htmlTreeViewActions, EventAction ea, SourceValueDictionary downloads, MethodClass mcUploadedValues, Dictionary<UInt32, IAction> actions, PropertyPointerList uploads, StringCollection uploadedValuesPhp, CodeMemberMethod uploadedValuesAspx)
		{
			foreach (TaskID tid in block.Sect2)
			{
				HandlerMethodID hmid = tid as HandlerMethodID;
				if (hmid != null)
				{
					WebClientEventHandlerMethod wceh = hmid.HandlerMethod as WebClientEventHandlerMethod;
					if (wceh != null)
					{
						PropertyPointerList pl;
						if (!downloads.TryGetValue(tid.TaskOrder, out pl))
						{
							pl = new PropertyPointerList();
							downloads.Add(tid.TaskOrder, pl);
						}
						hmid.HandlerMethod.LoadActionInstances();
						hmid.HandlerMethod.SetActions(actions);
						hmid.HandlerMethod.CollectSourceValues(tid.TaskId);
						IList<ISourceValuePointer> l = hmid.HandlerMethod.DownloadProperties;
						if (l != null && l.Count > 0)
						{
							foreach (ISourceValuePointer sv in l)
							{
								if (!downloads.ValueExists(sv))
								{
									pl.AddPointer(sv);
								}
							}
						}
						List<MethodClass> ml = new List<MethodClass>();
						hmid.HandlerMethod.GetCustomMethods(ml);
						foreach (MethodClass m in ml)
						{
							m.CollectSourceValues(tid.TaskId);
							l = m.DownloadProperties;
							if (l != null && l.Count > 0)
							{
								foreach (ISourceValuePointer sv in l)
								{
									if (!downloads.ValueExists(sv))
									{
										pl.AddPointer(sv);
									}
								}
							}
						}
						//this case should not present.
						//throw when l.Count > 0 ???
						l = hmid.HandlerMethod.UploadProperties;
						if (l != null && l.Count > 0)
						{
							uploads.AddPointerList(l);
						}
					}
				}
				else
				{
					IAction a = GetPublicAction(tid);
					if (a != null)
					{
						IList<ISourceValuePointer> l = a.GetServerProperties(ea.EventHandlerId/*tid.TaskId*/);
						if (l != null && l.Count > 0)
						{
							PropertyPointerList pl;
							if (!downloads.TryGetValue(tid.TaskOrder, out pl))
							{
								pl = new PropertyPointerList();
								downloads.Add(tid.TaskOrder, pl);
							}
							foreach (ISourceValuePointer sv in l)
							{
								if (!downloads.ValueExists(sv))
								{
									pl.AddPointer(sv);
								}
							}
						}
						//this case should not present.
						//throw when l.Count > 0 ???
						l = a.GetUploadProperties(tid.TaskId);
						if (l != null && l.Count > 0)
						{
							uploads.AddPointerList(l);
						}
					}
				}
			}
		}
		/// <summary>
		/// compile actions and handler methods for an event of a web client component
		/// </summary>
		/// <param name="ea">event and handler</param>
		/// <param name="sc">main js</param>
		/// <param name="sc0">custom event link</param>
		/// <param name="serverCode">for storing PHP code generated</param>
		private void createWebClientHandler(EventAction ea, StringCollection sc, StringCollection sc0, ServerScriptHolder serverCode)
		{
			//make event and handler link===========================================
			StringCollection scx0;//for storing javascript code
			IJavaScriptEventOwner ijs = ea.Event.Owner.ObjectInstance as IJavaScriptEventOwner;
			bool isWinLoad = false;
			UInt32 assignActionId = ea.GetAssignActionId();
			bool isenter = string.CompareOrdinal(ea.Event.Name, "onenterkey") == 0;
			string funcName = ea.EventHandlerName;
			StringCollection attachhandler = new StringCollection();
			int currentIndentation = Indentation.GetIndentation();
			Indentation.SetIndentation(1);
			string sIndents = Indentation.GetIndent();
			bool pageScope = false;
			StringCollection pageScopeCode = null;
			string extendedAttach = null;
			bool isBeforeInitPage = typeof(fnInitPage).Equals(ea.Event.EventHandlerType.BaseClassType);
			VPLUtil.CompilerContext_ASPX = false;
			VPLUtil.CompilerContext_PHP = false;
			if (assignActionId != 0 || isBeforeInitPage)
			{
				ea.CompileToFunction = true;
				funcName = ea.GetLocalHandlerName();
				pageScope = true;
				pageScopeCode = new StringCollection();
			}
			if (ijs == null) //standard event-handler mapping
			{
				//event owner
				int nOnLoad = 0;
				IWebPage webpage = ea.Event.Owner.ObjectInstance as IWebPage;
				if (webpage != null && string.CompareOrdinal(ea.Event.Name, "onload") == 0)
				{
					nOnLoad = 1;
				}
				else
				{
					bool isExtended = ea.IsExtendWebClientEvent();
					if (webpage != null && !isExtended && !isBeforeInitPage)
					{
						CustomEventPointer cep = ea.Event as CustomEventPointer;
						if (cep != null)
						{
						}
						else
						{
							sc.Add(sIndents);
							sc.Add(webpage.MapEventOwnerName(ea.Event.Name));
							sc.Add(".");
						}
					}
					else
					{
						StringCollection scp = sc;
						if (pageScope)
						{
							scp = pageScopeCode;
						}
						ICustomCodeName ccn = ea.Event.Owner.ObjectInstance as ICustomCodeName;
						if (ccn == null || ccn.DeclareJsVariable("webEventhandler"))
						{
							if (!(ea.Event.Owner is HtmlElement_body))
							{
								if (isExtended)
								{
									scp.Add(sIndents);
									scp.Add("var ");
									scp.Add(ea.Event.Owner.CodeName);
									scp.Add("=JsonDataBinding.getClientEventHolder('");
									scp.Add(ea.Event.Name);
									scp.Add("','");
									EasyDataSet eds = ea.Event.Owner.ObjectInstance as EasyDataSet;
									if (eds != null)
									{
										scp.Add(eds.TableName);
									}
									else
									{
										scp.Add(ea.Event.Owner.CodeName);
									}
									scp.Add("');\r\n");
									///////////////////////////
									if (assignActionId == 0)
									{
										StringBuilder sbExt = new StringBuilder();
										sbExt.Append(sIndents);
										sbExt.Append("JsonDataBinding.attachExtendedEvent('");
										sbExt.Append(ea.Event.Name);
										sbExt.Append("','");
										if (eds != null)
										{
											sbExt.Append(eds.TableName);
										}
										else
										{
											sbExt.Append(ea.Event.Owner.CodeName);
										}
										sbExt.Append("',");
										sbExt.Append(funcName);
										sbExt.Append(");\r\n");
										extendedAttach = sbExt.ToString();
									}
								}
								else
								{
									if (!ea.CompileToFunction)
									{
										scp.Add(sIndents);
										scp.Add("var ");
										scp.Add(ea.Event.Owner.CodeName);
										scp.Add("=document.getElementById('");
										scp.Add(ea.Event.Owner.CodeName);
										scp.Add("');\r\n");
									}
								}
							}
						}
						if (isBeforeInitPage)
						{
							scp.Add("function ");
							scp.Add(funcName);
							scp.Add("(pageUrl, parentUrl) {\r\n");
						}
						else if (ea.CompileToFunction || ea.IsExtendWebClientEvent())
						{
							scp.Add(sIndents);
							scp.Add("var ");
							scp.Add(funcName);
							bool isMouseKeyboardEvent = ea.IsWebMouseKeyboardEvent();
							if (isMouseKeyboardEvent)
							{
								scp.Add("=function(event) {\r\n");
								scp.Add(sIndents);
								scp.Add("\tvar evt8923 = (typeof event !== 'undefined' && event != null)?event:(window.event || (document.parentWindow? document.parentWindow.event:null));\r\n");
								scp.Add(sIndents);
								scp.Add("\tvar sender = JsonDataBinding.getSender(evt8923);\r\n");
								if (isenter)
								{
									scp.Add(sIndents);
									scp.Add("if(evt8923.keyCode!=13) return;\r\n");
								}
							}
							else
							{
								scp.Add("=function(");
								EventPointer ep = ea.Event as EventPointer;
								if (ep != null)
								{
									ParameterInfo[] pifs = ep.Parameters;
									if (pifs != null && pifs.Length > 0)
									{
										scp.Add(pifs[0].Name);
										for (int i = 1; i < pifs.Length; i++)
										{
											scp.Add(",");
											scp.Add(pifs[i].Name);
										}
									}
								}
								scp.Add(") {\r\n");
							}
							Indentation.IndentIncrease();
							sIndents = Indentation.GetIndent();
							if (ea.IsExtendWebClientEvent())
							{
								scp.Add(sIndents);
								scp.Add("var ");
								scp.Add(ea.Event.Owner.CodeName);
								scp.Add("=");
								scp.Add("JsonDataBinding.getClientEventHolder('");
								scp.Add(ea.Event.Name);
								scp.Add("','");
								EasyDataSet eds = ea.Event.Owner.ObjectInstance as EasyDataSet;
								if (eds != null)
								{
									scp.Add(eds.TableName);
								}
								else
								{
									scp.Add(ea.Event.Owner.CodeName);
								}
								scp.Add("');\r\n");
							}
							else
							{
								if (ea.Event.Owner.ObjectType != null && ea.Event.Owner.ObjectType.GetInterface("IWebClientControl") != null)
								{
									if (!(ea.Event.Owner is LocalVariable))
									{
										scp.Add(sIndents);
										scp.Add("var ");
										scp.Add(ea.Event.Owner.CodeName);
										scp.Add("=");
										scp.Add("document.getElementById('");
										scp.Add(ea.Event.Owner.CodeName);
										scp.Add("');\r\n");
									}
								}
								else
								{
									scp.Add(sIndents);
									scp.Add("var ");
									scp.Add(ea.Event.Owner.CodeName);
									scp.Add("=sender;\r\n");
								}
							}
						}
						else
						{
							scp.Add(sIndents);
							sc.Add(ea.Event.Owner.CodeName);
							sc.Add(".");
						}
					}
				}
				//event name
				if (nOnLoad == 1)
				{
					if (string.CompareOrdinal(ea.Event.Name, "onload") == 0)
					{
						isWinLoad = true;
					}
				}
				if (!isWinLoad)
				{
					if (nOnLoad == 1)
					{
						sc.Add("window.");
					}
					CustomEventPointer cep = ea.Event as CustomEventPointer;
					if (!ea.CompileToFunction)
					{
						if (cep != null)
						{
							sc.Add("limnorPage.");
						}
						if (!ea.IsExtendWebClientEvent())
						{
							sc.Add(ea.EventName);
						}
					}
					if (cep != null)
					{
						sc.Add("=function(");
						List<NamedDataType> ps = ea.Event.GetEventParameters();
						if (ps != null && ps.Count > 0)
						{
							sc.Add(ps[0].Name);
							for (int i = 1; i < ps.Count; i++)
							{
								sc.Add(",");
								sc.Add(ps[i].Name);
							}
						}
						sc.Add(") {\r\n");
						Indentation.IndentIncrease();
						sIndents = Indentation.GetIndent();
					}
					else
					{
						if (!ea.CompileToFunction)
						{
							if (isenter)
							{
								sc.Add(string.Format(CultureInfo.InvariantCulture,
									"=function({0}) {{\r\n", EventAction.JS_EVENT_KEY));
								Indentation.IndentIncrease();
								sIndents = Indentation.GetIndent();
								sc.Add(sIndents);
								sc.Add(string.Format(CultureInfo.InvariantCulture,
									"var evt8923 = {0} || window.event;\r\n", EventAction.JS_EVENT_KEY));
								sc.Add(sIndents);
								sc.Add("if(evt8923.keyCode!=13) return;\r\n");
								sc.Add("\tvar sender = JsonDataBinding.getSender(evt8923);\r\n");
							}
							else
							{
								if (string.CompareOrdinal(ea.Event.Name, "onkeyup") == 0)
								{
									sc.Add(string.Format(CultureInfo.InvariantCulture,
										"=function({0}) {{\r\n", EventAction.JS_EVENT_KEY));
									Indentation.IndentIncrease();
									sIndents = Indentation.GetIndent();
								}
								else
								{
									if (!ea.IsExtendWebClientEvent())
									{
										sc.Add("=function(event");
										EventPointer ept = ea.Event as EventPointer;
										if (ept != null)
										{
											ParameterInfo[] pifs = ept.Parameters;
											if (pifs != null && pifs.Length > 0)
											{
												if (pifs.Length == 2 && string.CompareOrdinal(pifs[0].Name, "sender") == 0 && string.CompareOrdinal(pifs[1].Name, "e") == 0)
												{
												}
												else
												{
													for (int i = 0; i < pifs.Length; i++)
													{
														sc.Add(",");
														sc.Add(pifs[i].Name);
													}
												}
											}
										}
										sc.Add(") {\r\n");
									}
									Indentation.IndentIncrease();
									sIndents = Indentation.GetIndent();
									sc.Add(sIndents);
									sc.Add("if(typeof sender === 'undefined' || sender == null) {\r\n");
									sc.Add(sIndents);
									sc.Add("\tvar e = (typeof event !== 'undefined' && event != null)?event:(window.event || (document.parentWindow? document.parentWindow.event:null));\r\n");
									sc.Add(sIndents);
									sc.Add("\tvar sender = JsonDataBinding.getSender(e);\r\n");
									sc.Add(sIndents);
									sc.Add("}\r\n");
								}
							}
						}
					}
				}
				if (pageScope)
				{
					scx0 = pageScopeCode;
				}
				else
				{
					scx0 = sc; //main js
				}
				//
				if (webpage == null)
				{
					scx0.Add(sIndents);
					scx0.Add("JsonDataBinding.");
					scx0.Add("SetEventFirer(");
					if (pageScope && ea.Event.Owner is LocalVariable)
					{
						scx0.Add("sender");
					}
					else
					{
						scx0.Add(ea.Event.Owner.CodeName);
					}
					scx0.Add(");\r\n");
				}
			}
			else //event-handler mapping will be done by the object
			{
				string hname = null;
				bool linked = false;
				EventPointer ei = ea.Event as EventPointer;
				if (assignActionId != 0)
				{
					hname = EventAction.GetAttachFunctionName(assignActionId);
				}
				if (ei != null)
				{
					IEventInfoTree emi = ei.Info as IEventInfoTree;
					if (emi != null)
					{
						if (assignActionId == 0)
						{
							hname = string.Format(CultureInfo.InvariantCulture, "e{0}{1}", emi.GetEventId().ToString("x", CultureInfo.InvariantCulture), ea.Event.Owner.ObjectKey.GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						}
						ijs.LinkJsEvent(ei.Owner.CodeName, string.Format(CultureInfo.InvariantCulture, "e{0}", emi.GetEventId().ToString("x", CultureInfo.InvariantCulture)), hname, sc, (assignActionId != 0));
						linked = true;
					}
				}
				if (!linked)
				{
					if (assignActionId == 0)
					{
						hname = string.Format(CultureInfo.InvariantCulture, "e{0}{1}", ea.Event.Name.GetHashCode().ToString("x", CultureInfo.InvariantCulture), ea.Event.Owner.ObjectKey.GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					}
					//ea.Event.Name and hname are saved into ijs for ijs to use link them together in its own way in OnWebPageLoadedAfterEventHandlerCreations override 
					ijs.LinkJsEvent(ea.Event.Owner.CodeName, ea.Event.Name, hname, sc, (assignActionId != 0));
				}
				sIndents = Indentation.GetIndent();
				scx0 = sc0; //custom event link
				scx0.Add(sIndents);
				scx0.Add("function ");
				scx0.Add(hname);
				scx0.Add("(");
				List<NamedDataType> ps = ea.Event.GetEventParameters();
				if (ps != null && ps.Count > 0)
				{
					scx0.Add(ps[0].Name);
					for (int i = 1; i < ps.Count; i++)
					{
						scx0.Add(",");
						scx0.Add(ps[i].Name);
					}
				}
				scx0.Add(") {\r\n");
				Indentation.IndentIncrease();
				sIndents = Indentation.GetIndent();
			}
			//
			TaskIdList ts = ea.TaskIDList;
			ts.SetTaskOrder();
			foreach (TaskID tid in ea.TaskIDList)
			{
				HandlerMethodID hmid = tid as HandlerMethodID;
				if (hmid != null)
				{
				}
				else
				{
					IAction a = GetPublicAction(tid);
					if (a != null)
					{
						a.ScopeRunAt = EnumWebRunAt.Client;
					}
				}
			}
			/*
			 * only the last block should close the block function. 
			 dialog: after calling showChild, add
			 * v.onDialogClose = function(){
			 next ajax: download and upload all server variables used
			 */
			//====================================================================================
			//group actions into blocks, each block contains 3 sub-groups:client-actions, server-actions, client-actions
			Dictionary<UInt32, IAction> actions = _rootClassId.GetActions();
			WebHandlerBlockList actList = ea.CreateWebClientHandlerActionBlocks(_rootClassId);
			if (actList.Count == 0)
			{
				//an empty event handler
				if (!isWinLoad) //onload will be closed after all handlers
				{
					scx0.Add(sIndents);
					scx0.Add("}\r\n");
				}
			}
			else
			{
				scx0.Add(sIndents);
				if (!isBeforeInitPage)
				{
					scx0.Add("JsonDataBinding.AbortEvent = false;\r\n");
				}
				StringCollection scLast = null;
				StringCollection scx = null;
				int baseIndents = Indentation.GetIndentation();
				//compile starting from the last block to the first block
				for (int k = actList.Count - 1; k >= 0; k--)
				{
					Indentation.SetIndentation(baseIndents + k);
					sIndents = Indentation.GetIndent();
					scLast = scx;
					scx = new StringCollection();
					//support tree view
					List<ActionClass> htmlTreeViewActions = addTreeViewSupport(actList[k]);
					//collect upload (client) variables from server actions ====================
					PropertyPointerList uploads = new PropertyPointerList();
					SourceValueDictionary downloads = new SourceValueDictionary();
					bool hasUploadedValues = false;
					StringCollection uploadedValuesPhp = new StringCollection();
					CodeMemberMethod uploadedValuesAspx = new CodeMemberMethod();
					MethodClass mcUploadedValues = new MethodClass(_rootClassId);
					if (_project.ProjectType == EnumProjectType.WebAppAspx)
					{
						mcUploadedValues.SetCompilerData(this);
						mcUploadedValues.MethodID = (UInt32)(Guid.NewGuid().GetHashCode());
						mcUploadedValues.Parameters = new List<ParameterClass>();
						mcUploadedValues.Name = string.Format(CultureInfo.InvariantCulture, "upload{0}", mcUploadedValues.MethodID);
						uploadedValuesAspx.Attributes = MemberAttributes.Public;
						uploadedValuesAspx.Name = mcUploadedValues.Name;
						mcUploadedValues.MethodCode = uploadedValuesAspx;
					}
					//check client action return values for server values
					collectUploadsFromActionReturns(actList[k].Sect0, mcUploadedValues, actions, uploads, uploadedValuesPhp, uploadedValuesAspx);
					//check server action parameters
					collectUploadsFromActionParameters(actList[k].Sect1, htmlTreeViewActions, ea, downloads, mcUploadedValues, actions, uploads, uploadedValuesPhp, uploadedValuesAspx);
					//collect download (server) variables from client actions =================================
					collectDownloadValues(actList[k], htmlTreeViewActions, ea, downloads, mcUploadedValues, actions, uploads, uploadedValuesPhp, uploadedValuesAspx);
					//
					if ((actList[k].BlockProcess & EnumWebBlockProcess.Download) == EnumWebBlockProcess.Download)
					{
						if (actList.ServerStates.Count > 0)
						{
							int km = 0;
							foreach (KeyValuePair<int, PropertyPointerList> kv in downloads)
							{
								if (kv.Key > km)
								{
									km = kv.Key;
								}
							}
							km++;
							PropertyPointerList pl = new PropertyPointerList();
							foreach (ISourceValuePointer sv in actList.ServerStates)
							{
								if (!downloads.ValueExists(sv))
								{
									pl.AddPointer(sv);
								}
							}
							downloads.Add(km, pl);
						}
					}
					//===end of collecting download values ===
					//generate the first js function which is linked to the event
					//form name and conditions. only one form is allowed; only one condition is allowed
					Dictionary<string, StringCollection> formSubmissions = new Dictionary<string, StringCollection>();
					foreach (TaskID tid in actList[k].Sect0)
					{
						HandlerMethodID hmid = tid as HandlerMethodID;
						if (hmid != null)
						{
							TraceLogClass.TraceLog.Trace("Compile event method {0}", hmid.HandlerMethod.Name);
							if (!string.IsNullOrEmpty(hmid.HandlerMethod.Description))
							{
								scx.Add(sIndents);
								scx.Add("/*\r\n");
								scx.Add(sIndents);
								scx.Add(hmid.HandlerMethod.Description);
								scx.Add("\r\n");
								scx.Add(sIndents);
								scx.Add("*/\r\n");
							}
							TraceLogClass.TraceLog.IndentIncrement();
							JsMethodCompiler jmc = new JsMethodCompiler(hmid.HandlerMethod, _rootClassId, Debug, formSubmissions, false);
							StringCollection methodcode = new StringCollection();
							//
							VPLUtil.CompilerContext_JS = true;
							if (pageScope)
							{
								hmid.HandlerMethod.ExportJavaScriptCode(pageScopeCode, methodcode, jmc);
							}
							else
							{
								hmid.HandlerMethod.ExportJavaScriptCode(sc, methodcode, jmc);
							}
							VPLUtil.CompilerContext_JS = false;
							//
							if (methodcode.Count == 0)
							{
								//codes have been collected in the method, called the method
								scx.Add(sIndents);
								scx.Add(hmid.HandlerMethod.CodeName);
								scx.Add("(");
								if (hmid.HandlerMethod != null && hmid.HandlerMethod.Parameters != null && hmid.HandlerMethod.Parameters.Count > 0)
								{
									scx.Add(hmid.HandlerMethod.Parameters[0].Name);
									for (int i = 1; i < hmid.HandlerMethod.Parameters.Count; i++)
									{
										scx.Add(",");
										scx.Add(hmid.HandlerMethod.Parameters[i].Name);
									}
								}
								scx.Add(");\r\n");
							}
							else
							{
								for (int i = 0; i < methodcode.Count; i++)
								{
									scx.Add(methodcode[i]);
								}
							}
							TraceLogClass.TraceLog.IndentDecrement();
							TraceLogClass.TraceLog.Trace("End of compile event method {0}", hmid.HandlerMethod.Name);
						}
						else
						{
							VPLUtil.CompilerContext_JS = true;
							IAction a = GetPublicAction(tid);
							if (a == null)
							{
								_classCompiler.LogError("Action {0} not found from {1}", tid.ToString(), _xmlFile);
							}
							else
							{
								MethodActionForeachAtClient loop = a as MethodActionForeachAtClient;
								if (loop != null)
								{
									JsMethodCompiler jmc = new JsMethodCompiler(loop, _rootClassId, Debug, formSubmissions, false);
									StringCollection methodcode = new StringCollection();
									////
									loop.ExportJavaScriptCode(sc, methodcode, jmc);
									////
									scx.Add(string.Format(CultureInfo.InvariantCulture,
										"{0}();\r\n", loop.MethodName));
								}
								else
								{
									MethodReturnMethod mrm = a.ActionMethod as MethodReturnMethod;
									if (mrm != null)
									{
										mrm.IsAbort = true;
									}
									a.CreateJavaScript(scx, formSubmissions, null, sIndents);
									StringCollection dlgCode = null;
									ActionClass ac = a as ActionClass;
									if (ac != null)
									{
										if (ac.JavascriptCodeSegments != null && ac.JavascriptCodeSegments.Count > 0)
										{
											string cv = null;
											if (ac.JavascriptCodeSegments[0] != null && ac.JavascriptCodeSegments[0].Count > 0)
											{
												cv = ac.JavascriptCodeSegments[0][0]; //parameter object name
												if (actList[k].BlockType == EnumWebHandlerBlockType.Dialog)
												{
													if (scLast != null)
													{
														if (a.ActionMethod != null && a.ActionMethod.Owner != null)
														{
															if (a.ActionMethod.Owner.ObjectType != null && a.ActionMethod.Owner.ObjectType.GetInterface("IWebPage") != null)
															{
																if (string.CompareOrdinal(a.ActionMethod.MethodName, "ShowChildDialog") == 0)
																{
																	dlgCode = new StringCollection();
																	dlgCode.Add(sIndents);
																	dlgCode.Add(string.Format(CultureInfo.InvariantCulture,
																		"{0}.onDialogClose=function(){{\r\n", cv));
																	foreach (string s in scLast)
																	{
																		dlgCode.Add(s);
																	}
																	dlgCode.Add("\r\n");
																	dlgCode.Add(sIndents);
																	dlgCode.Add("}\r\n");
																}
															}
														}
													}
												}
											}
											if (ac.JavascriptCodeSegments.Count > 1 && ac.JavascriptCodeSegments[1] != null)
											{
												foreach (string s in ac.JavascriptCodeSegments[1]) //parameter object creation
												{
													scx.Add(s);
												}
											}
											if (dlgCode != null)
											{
												foreach (string s in dlgCode) //code following dialog
												{
													scx.Add(s);
												}
											}
											if (ac.JavascriptCodeSegments.Count > 2 && ac.JavascriptCodeSegments[2] != null)
											{
												if (ac.JavascriptCodeSegments[2].Count > 0) //has condition
												{
													scx.Add("if(");
													scx.Add(ac.JavascriptCodeSegments[2][0]);
													scx.Add(") {\r\n");
												}
											}
											scx.Add(string.Format(CultureInfo.InvariantCulture,
												"{0}JsonDataBinding.showChild({1});\r\n", Indentation.GetIndent(), cv));
											if (ac.JavascriptCodeSegments.Count > 2 && ac.JavascriptCodeSegments[2] != null && ac.JavascriptCodeSegments[2].Count > 0) //has condition
											{
												scx.Add("}\r\nelse {\r\n");
												scx.Add(string.Format(CultureInfo.InvariantCulture,
														"\tif({0}.onDialogClose) {{\r\n", cv));
												scx.Add(string.Format(CultureInfo.InvariantCulture, "\t\t{0}.onDialogClose();\r\n", cv));
												scx.Add("\t}\r\n");
												scx.Add("}\r\n");
											}
										}
									}
								}
							}
							VPLUtil.CompilerContext_JS = false;
						}
					}
					//add upload variables--------------
					bool needServerCall = (hasUploadedValues || uploads.Count > 0 || actList[k].Sect1.Count > 0 || actList[k].Sect2.Count > 0 || formSubmissions.Count > 0);
					string uData = string.Format(CultureInfo.InvariantCulture, "u{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					if (needServerCall)
					{
						scx.Add(sIndents);
						scx.Add(string.Format(CultureInfo.InvariantCulture,
							"var {0}=new Object();\r\n", uData));
					}
					if (uploads.Count > 0)
					{
						StringCollection scUsed = new StringCollection();
						VPLUtil.CompilerContext_JS = true;
						foreach (ISourceValuePointer p in uploads)
						{
							string pcode = p.CreateJavaScript(sc);
							if (htmlTreeViewActions != null && htmlTreeViewActions.Count > 0)
							{
								foreach (ActionClass ac in htmlTreeViewActions)
								{
									Dictionary<ISourceValuePointer, string> pcodes = ac.GetUserData("PCODE") as Dictionary<ISourceValuePointer, string>;
									if (pcodes != null)
									{
										if (pcodes.ContainsKey(p))
										{
											pcodes[p] = pcode;
											scx.Add(sIndents);
											scx.Add(string.Format(CultureInfo.InvariantCulture,
												"{0}.currentPrimaryKeyValue=null;\r\n", ac.MethodOwner.CodeName));
										}
									}
								}
							}
							if (!scUsed.Contains(p.DataPassingCodeName))
							{
								scUsed.Add(p.DataPassingCodeName);
								scx.Add(sIndents);
								scx.Add(string.Format(CultureInfo.InvariantCulture,
									"{0}.{1} = {2};\r\n", uData, p.DataPassingCodeName, pcode));
							}
						}
						VPLUtil.CompilerContext_JS = false;
					}
					if (needServerCall)
					{
						//add server call-------------------
						string serverMethodName = string.Format(CultureInfo.InvariantCulture, "s{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						//
						//create server method--------------
						MethodClass mc = new MethodClass(_rootClassId);
						mc.SetCompilerData(this);
						mc.MethodID = (UInt32)(Guid.NewGuid().GetHashCode());
						mc.Parameters = new List<ParameterClass>();
						mc.Name = serverMethodName;
						//
						SortedDictionary<int, StringCollection> sortedPhpServerCode = null;
						SortedDictionary<int, CodeMemberMethod> sortedAspxServerCode = null;
						JsMethodCompiler jmcServer = null;
						if (_project.ProjectType == EnumProjectType.WebAppAspx)
						{
							sortedAspxServerCode = new SortedDictionary<int, CodeMemberMethod>();
							if (uploadedValuesAspx.Statements.Count > 0)
							{
								sortedAspxServerCode.Add(-2, uploadedValuesAspx);
							}
							CodeMemberMethod mm = new CodeMemberMethod();
							mm.Attributes = MemberAttributes.Public;
							mm.Name = serverMethodName;
							mc.MethodCode = mm;
							//
							sortedAspxServerCode.Add(-1, mm);
							//
							VPLUtil.CompilerContext_ASPX = true;
							mc.ExportCode(this, mm, true);
							VPLUtil.CompilerContext_ASPX = false;
						}
						else if (_project.ProjectType == EnumProjectType.WebAppPhp)
						{
							sortedPhpServerCode = new SortedDictionary<int, StringCollection>();
							jmcServer = new JsMethodCompiler(mc, _rootClassId, Debug, formSubmissions, true);
							if (uploadedValuesPhp.Count > 0)
							{
								sortedPhpServerCode.Add(0, uploadedValuesPhp);
							}
						}
						//server method contents
						foreach (TaskID tid in actList[k].Sect1)
						{
							HandlerMethodID hmid = tid as HandlerMethodID;
							if (hmid != null)
							{
								WebClientEventHandlerMethodServerActions ws = hmid.HandlerMethod as WebClientEventHandlerMethodServerActions;
								if (ws != null)
								{
									if (_project.ProjectType == EnumProjectType.WebAppAspx)
									{
										CodeMemberMethod mm;
										if (!sortedAspxServerCode.TryGetValue(tid.TaskOrder, out mm))
										{
											mm = new CodeMemberMethod();
											mm.Attributes = MemberAttributes.Public;
											mm.Name = serverMethodName;
											sortedAspxServerCode.Add(tid.TaskOrder, mm);
										}
										ws.MethodCode = mm;
										ws.SetCompilerData(this);
										VPLUtil.CompilerContext_ASPX = true;
										ws.ExportCode(this, mm, true);
										VPLUtil.CompilerContext_ASPX = false;
									}
									else if (_project.ProjectType == EnumProjectType.WebAppPhp)
									{
										StringCollection scv;
										if (!sortedPhpServerCode.TryGetValue(tid.TaskOrder, out scv))
										{
											scv = new StringCollection();
											sortedPhpServerCode.Add(tid.TaskOrder, scv);
										}
										VPLUtil.CompilerContext_PHP = true;
										JsMethodCompiler jmc = new JsMethodCompiler(ws, _rootClassId, Debug, formSubmissions, true);
										ws.ExportPhpScriptCode(serverCode.Methods, serverCode.Methods, jmc);
										//
										scv.Add(string.Format(CultureInfo.InvariantCulture,
											"$this->{0}(", ws.CodeName));
										if (ws.ParameterCount > 0)
										{
											scv.Add(string.Format(CultureInfo.InvariantCulture,
												"$this->jsonFromClient->values->{0}", ws.Parameters[0].DataPassingCodeName));
											for (int pi = 1; pi < ws.ParameterCount; pi++)
											{
												scv.Add(",");
												scv.Add(string.Format(CultureInfo.InvariantCulture,
													"$this->jsonFromClient->values->{0}", ws.Parameters[pi].DataPassingCodeName));
											}
										}
										scv.Add(");\r\n");
										VPLUtil.CompilerContext_PHP = false;
									}
								}
							}
							else
							{
								IAction a = GetPublicAction(tid);
								if (a != null)
								{
									if (_project.ProjectType == EnumProjectType.WebAppAspx)
									{
										CodeMemberMethod mm;
										VPLUtil.CompilerContext_ASPX = true;
										if (!sortedAspxServerCode.TryGetValue(tid.TaskOrder, out mm))
										{
											mm = new CodeMemberMethod();
											mm.Attributes = MemberAttributes.Public;
											mm.Name = serverMethodName;
											sortedAspxServerCode.Add(tid.TaskOrder, mm);
										}
										MethodActionForeachAtServer loop = a as MethodActionForeachAtServer;
										if (loop != null)
										{
											loop.MethodCode = mm;
											loop.SetCompilerData(this);
											loop.ExportCode(this, mm, true);
										}
										else
										{
											bool isHtmlTreeView = false;
											ActionClass ac = a as ActionClass;
											if (ac != null)
											{
												if (htmlTreeViewActions.Contains(ac))
												{
													isHtmlTreeView = true;
													string srvMethdName = ac.GetUserData("htmlTreeViewLoad") as string;
													if (string.IsNullOrEmpty(srvMethdName))
													{
														srvMethdName = string.Format(CultureInfo.InvariantCulture, "htv_{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
														ac.SetUserData("htmlTreeViewLoad", srvMethdName);
														MethodClass mcHTV = new MethodClass(_rootClassId);
														mcHTV.SetCompilerData(this);
														mcHTV.MethodID = (UInt32)(Guid.NewGuid().GetHashCode());
														mcHTV.Parameters = new List<ParameterClass>();
														mcHTV.Name = srvMethdName;
														//
														CodeMemberMethod mmHTV = new CodeMemberMethod();
														mmHTV.Attributes = MemberAttributes.Public;
														mmHTV.Name = mcHTV.Name;
														td.Members.Add(mmHTV);
														//
														a.ExportCode(null, null, this, mcHTV, mmHTV, mmHTV.Statements, false);
														//
														//
														HtmlTreeView htv = ac.MethodOwner.ObjectInstance as HtmlTreeView;
														htv.Loadername = srvMethdName;
													}
													CodeMethodInvokeExpression cmiehtv = new CodeMethodInvokeExpression();
													cmiehtv.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), srvMethdName);
													mm.Statements.Add(cmiehtv);
												}
											}
											if (!isHtmlTreeView)
											{
												a.ExportCode(null, null, this, mc, mm, mm.Statements, false);
											}
										}
										VPLUtil.CompilerContext_ASPX = false;
									}
									else if (_project.ProjectType == EnumProjectType.WebAppPhp)
									{
										StringCollection scv;
										if (!sortedPhpServerCode.TryGetValue(tid.TaskOrder, out scv))
										{
											scv = new StringCollection();
											sortedPhpServerCode.Add(tid.TaskOrder, scv);
										}
										VPLUtil.CompilerContext_PHP = true;
										MethodActionForeachAtServer loop = a as MethodActionForeachAtServer;
										if (loop != null)
										{
											JsMethodCompiler jmc = new JsMethodCompiler(loop, _rootClassId, Debug, formSubmissions, true);
											loop.ExportPhpScriptCode(serverCode.Methods, scv, jmc);
										}
										else
										{
											IFormSubmitter fs = null;
											if (a.ActionMethod != null && a.ActionMethod.Owner != null)
											{
												fs = a.ActionMethod.Owner.ObjectInstance as IFormSubmitter;
											}
											if (fs != null && fs.IsSubmissionMethod(a.ActionMethod.MethodName))
											{
												StringCollection subm;
												if (!formSubmissions.TryGetValue(fs.FormName, out subm))
												{
													subm = new StringCollection();
													formSubmissions.Add(fs.FormName, subm);
												}
												subm.Add(a.ActionCondition.CreateJavaScriptCode(scx));
											}
											else
											{
												if (fs != null && fs.RequireSubmissionMethod(a.ActionMethod.MethodName))
												{
													StringCollection subm;
													if (!formSubmissions.TryGetValue(fs.FormName, out subm))
													{
														subm = new StringCollection();
														formSubmissions.Add(fs.FormName, subm);
													}
													subm.Add(a.ActionCondition.CreateJavaScriptCode(scx));
												}
												bool isHtmlTreeView = false;
												ActionClass ac = a as ActionClass;
												if (ac != null)
												{
													if (htmlTreeViewActions.Contains(ac))
													{
														isHtmlTreeView = true;
														string srvMethdName = ac.GetUserData("htmlTreeViewLoad") as string;
														if (string.IsNullOrEmpty(srvMethdName))
														{
															srvMethdName = string.Format(CultureInfo.InvariantCulture, "htv_{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
															ac.SetUserData("htmlTreeViewLoad", srvMethdName);
															MethodClass mcHTV = new MethodClass(_rootClassId);
															mcHTV.SetCompilerData(this);
															mcHTV.MethodID = (UInt32)(Guid.NewGuid().GetHashCode());
															mcHTV.Parameters = new List<ParameterClass>();
															mcHTV.Name = srvMethdName;
															JsMethodCompiler htvJmc = new JsMethodCompiler(mcHTV, _rootClassId, Debug, formSubmissions, true);
															StringCollection svcHTV = new StringCollection();
															a.ExportPhpScriptCode(null, null, serverCode.Methods, svcHTV, htvJmc);
															//
															serverCode.Methods.Add("function ");
															serverCode.Methods.Add(srvMethdName);
															serverCode.Methods.Add("()\r\n{\r\n");
															//
															for (int i = 0; i < svcHTV.Count; i++)
															{
																serverCode.Methods.Add(svcHTV[i]);
															}
															//
															serverCode.Methods.Add("\r\n}\r\n");
															//
															HtmlTreeView htv = ac.MethodOwner.ObjectInstance as HtmlTreeView;
															htv.Loadername = srvMethdName;
														}
														scv.Add(string.Format(CultureInfo.InvariantCulture,
																"$this->{0}();\r\n", srvMethdName));
														serverCode.OnRequestExecution.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}();\r\n", srvMethdName));
													}
												}
												if (!isHtmlTreeView)
												{
													a.ExportPhpScriptCode(null, null, serverCode.Methods, scv, jmcServer);
												}
											}
										}
										VPLUtil.CompilerContext_PHP = false;
									}
								}
							}
						}
						if (htmlTreeViewActions.Count > 0)
						{
							foreach (ActionClass ac in htmlTreeViewActions)
							{
								string loader = ac.GetUserData("htmlTreeViewLoad") as string;
								if (!string.IsNullOrEmpty(loader))
								{
									scx.Add(Indentation.GetIndent());
									scx.Add(string.Format(CultureInfo.InvariantCulture,
										"{0}.nodesloader=function() {{\r\n", ac.MethodOwner.CodeName));
									//
									Indentation.IndentIncrease();
									scx.Add(Indentation.GetIndent());
									string u = string.Format(CultureInfo.InvariantCulture, "u{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
									scx.Add(string.Format(CultureInfo.InvariantCulture,
										"var {0} = new Object();\r\n", u));
									Dictionary<ISourceValuePointer, string> pcodes = ac.GetUserData("PCODE") as Dictionary<ISourceValuePointer, string>;
									if (pcodes != null)
									{
										foreach (KeyValuePair<ISourceValuePointer, string> kv in pcodes)
										{
											scx.Add(Indentation.GetIndent());
											if (kv.Value == null)
											{
												scx.Add(string.Format(CultureInfo.InvariantCulture,
													"{0}.{1} = null;\r\n", u, kv.Key.DataPassingCodeName));
											}
											else
											{
												scx.Add(string.Format(CultureInfo.InvariantCulture,
													"{0}.{1} = {2};\r\n", u, kv.Key.DataPassingCodeName, kv.Value));
											}
										}
									}
									scx.Add(Indentation.GetIndent());
									scx.Add(string.Format(CultureInfo.InvariantCulture,
										"JsonDataBinding.executeServerMethod('{0}',{1});", loader, u));
									//
									Indentation.IndentDecrease();
									scx.Add(Indentation.GetIndent());
									scx.Add("};\r\n");
									//
									IDataSetSourceHolder dssh = ac.MethodOwner.ObjectInstance as IDataSetSourceHolder;
									if (dssh != null)
									{
										IDataSetSource dss = dssh.GetDataSource();
										if (dss != null)
										{
											scx.Add(Indentation.GetIndent());
											scx.Add(string.Format(CultureInfo.InvariantCulture,
											"JsonDataBinding.resetDataStreaming('{0}');\r\n", dss.TableName));
											scx.Add(string.Format(CultureInfo.InvariantCulture,
											"JsonDataBinding.setTableAttribute('{0}', 'isFisrtTime', true);\r\n", dss.TableName));
										}
									}
								}
							}
						}
						if (scLast != null)
						{
							scx.Add(sIndents);
							scx.Add(string.Format(CultureInfo.InvariantCulture,
								"{0}.nextBlock=function(){{\r\n", uData));
							foreach (string s in scLast)
							{
								scx.Add(s);
							}
							scx.Add("\r\n");
							scx.Add(sIndents);
							scx.Add("}\r\n");
						}
						//collect data to be updated
						string dataVar = string.Format(CultureInfo.InvariantCulture, "data{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						scx.Add(sIndents);
						scx.Add(string.Format(CultureInfo.InvariantCulture, "var {0};\r\n", dataVar));
						StringCollection tablesTobeUpdated = actList[k].TableNames;
						if (tablesTobeUpdated.Count > 0)
						{
							scx.Add(sIndents);
							scx.Add(string.Format(CultureInfo.InvariantCulture, "{0} = new Array();\r\n", dataVar));
							for (int i = 0; i < tablesTobeUpdated.Count; i++)
							{
								scx.Add(sIndents);
								scx.Add(string.Format(CultureInfo.InvariantCulture, "{0}.push(JsonDataBinding.collectModifiedData('{1}',{2}));\r\n", dataVar, tablesTobeUpdated[i], i == 0 ? "false" : "true"));
							}
						}
						if (formSubmissions.Count > 0)
						{
							//use form submit
							if (formSubmissions.Count > 1)
							{
								throw new DesignerException("Cannot submit more than one form in one event");
							}
							Dictionary<string, StringCollection>.Enumerator en = formSubmissions.GetEnumerator();
							en.MoveNext();
							string form = en.Current.Key;
							StringCollection subm = en.Current.Value;
							StringCollection conditions = new StringCollection();
							if (subm.Count > 0)
							{
								foreach (string c in subm)
								{
									if (!string.IsNullOrEmpty(c))
									{
										conditions.Add(c);
									}
								}
							}
							if (conditions.Count == 0)
							{
								scx.Add(sIndents);
								scx.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.executeServerMethod('{0}',{1},{2},{3});\r\n", serverMethodName, uData, form, dataVar));
							}
							else
							{
								scx.Add(sIndents);
								scx.Add("if(");
								for (int m = 0; m < conditions.Count; m++)
								{
									if (m > 0)
									{
										scx.Add(" && ");
									}
									scx.Add("(");
									scx.Add(conditions[m]);
									scx.Add(")");
								}
								scx.Add(") {\r\n");
								scx.Add(sIndents);
								scx.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.executeServerMethod('{0}',{1},{2},{3});\r\n", serverMethodName, uData, form, dataVar));
								scx.Add("\r\n");
								scx.Add(sIndents);
								scx.Add("}\r\n");
							}
						}
						else
						{
							//use XMLHTTP
							scx.Add(sIndents);
							scx.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.executeServerMethod('{0}',{1},null,{2});\r\n", serverMethodName, uData, dataVar));
						}
						//---closing the event handler-------------------------------
						if (k == 0)
						{
							if (!isWinLoad)
							{
								IWebEventProcess wep = ea.Event.Owner.ObjectInstance as IWebEventProcess;
								if (wep != null)
								{
									wep.OnFinishEvent(ea.Event.Name, scx);
								}
								scx.Add("\t");
								scx.Add("}\r\n");
							}
						}
						//add downloads and client call------------------------------
						if (actList[k].Sect2.Count > 0 || downloads.Count > 0)
						{
							formSubmissions = new Dictionary<string, StringCollection>();
							string downloadJsName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", ea.Event.Name, Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
							if (_project.ProjectType == EnumProjectType.WebAppAspx)
							{
								VPLUtil.CompilerContext_ASPX = true;
								//execute AddDownloadValue(name,value) for each download value
								if (downloads.Count > 0)
								{
									foreach (KeyValuePair<int, PropertyPointerList> kv in downloads)
									{
										CodeMemberMethod mm;
										if (!sortedAspxServerCode.TryGetValue(kv.Key, out mm))
										{
											mm = new CodeMemberMethod();
											mm.Attributes = MemberAttributes.Public;
											mm.Name = serverMethodName;
											sortedAspxServerCode.Add(kv.Key, mm);
										}
										for (int i = 0; i < kv.Value.Count; i++)
										{
											MathNode mn = kv.Value[i] as MathNode;
											if (mn != null)
											{
												MathNodeRoot mr = mn.root as MathNodeRoot;
												if (mr != null)
												{
													mr.PrepareForCompile(mc);
													mr.ReturnCodeExpression(mc);
												}
											}
											CodeExpression c = kv.Value[i].GetReferenceCode(mc, mm.Statements, false);
											CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
											cmie.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "AddDownloadValue");
											cmie.Parameters.Add(new CodePrimitiveExpression(kv.Value[i].DataPassingCodeName));
											cmie.Parameters.Add(c);
											mm.Statements.Add(new CodeExpressionStatement(cmie));
										}
									}
								}
								//call AddClientScript(downloadJsName());
								int kmax = ts.Count + 10;
								CodeMemberMethod last = new CodeMemberMethod();
								last.Attributes = MemberAttributes.Public;
								last.Name = serverMethodName;
								sortedAspxServerCode.Add(kmax, last);
								CodeMethodInvokeExpression cmieJs = new CodeMethodInvokeExpression();
								cmieJs.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "AddClientScript");
								cmieJs.Parameters.Add(new CodePrimitiveExpression(string.Format(CultureInfo.InvariantCulture, "{0}();", downloadJsName)));
								last.Statements.Add(new CodeExpressionStatement(cmieJs));
								VPLUtil.CompilerContext_ASPX = false;
								//
							}
							else if (_project.ProjectType == EnumProjectType.WebAppPhp)
							{
								//execute AddDownloadValue(name,value) for each download value
								if (ea.Event.Name == "onload")
								{
								}
								StringCollection src = new StringCollection();
								if (downloads.Count > 0)
								{
									VPLUtil.CompilerContext_PHP = true;
									foreach (KeyValuePair<int, PropertyPointerList> kv in downloads)
									{
										for (int i = 0; i < kv.Value.Count; i++)
										{
											string c = kv.Value[i].CreatePhpScript(src);
											src.Add(string.Format(CultureInfo.InvariantCulture,
												"$this->AddDownloadValue('{0}',{1});\r\n", kv.Value[i].DataPassingCodeName, c));
										}
									}
									VPLUtil.CompilerContext_PHP = false;
								}
								//call AddClientScript(downloadJsName());
								int kmax = ts.Count + 10;
								StringBuilder sbPs = new StringBuilder();
								StringCollection last = new StringCollection();
								CustomEventPointer cep = ea.Event as CustomEventPointer;
								if (cep != null)
								{
									List<ParameterClass> cps = cep.Event.GetParameters(mc);
									if (cps != null && cps.Count > 0)
									{
										for (int pi = 0; pi < cps.Count; pi++)
										{
											if (pi > 0)
												sbPs.Append(",");
											string s = CompilerUtil.CreateJsCodeName(ea.Event, cps[pi].Name);
											sbPs.Append(string.Format(CultureInfo.InvariantCulture,
												"JsonDataBinding.values.{0}", s));
											last.Add(string.Format(CultureInfo.InvariantCulture,
												"$this->AddDownloadValue('{0}',$this->jsonFromClient->values->{0});\r\n", s));
										}
									}
								}
								if (actList[k].Sect2.Count > 0)
								{
									last.Add(string.Format(CultureInfo.InvariantCulture,
												"$this->AddClientScript('{0}({1});');\r\n", downloadJsName, sbPs.ToString()));
								}
								if (src != null)
								{
									foreach (string s in src)
									{
										last.Add(s);
									}
								}
								sortedPhpServerCode.Add(kmax, last);
							}
							//----------------------------------
							//create download js----------------
							//create js in sc0
							if (actList[k].Sect2.Count > 0)
							{
								sc0.Add("function ");
								sc0.Add(downloadJsName);
								sc0.Add("() {\r\n");
								VPLUtil.CompilerContext_JS = true;
								CustomEventPointer cep = ea.Event as CustomEventPointer;
								if (cep != null)
								{
									List<ParameterClass> ps = cep.Event.GetParameters(mc);
									if (ps != null && ps.Count > 0)
									{
										for (int pi = 0; pi < ps.Count; pi++)
										{
											string s = CompilerUtil.CreateJsCodeName(ea.Event, ps[pi].Name);
											sc0.Add(string.Format(CultureInfo.InvariantCulture,
												"var {0}=JsonDataBinding.values.{1};\r\n", ps[pi].Name, s));
										}
									}
								}
								foreach (TaskID tid in actList[k].Sect2)
								{
									HandlerMethodID hmid = tid as HandlerMethodID;
									if (hmid != null)
									{
										TraceLogClass.TraceLog.Trace("Compile event method {0}", hmid.HandlerMethod.Name);
										if (!string.IsNullOrEmpty(hmid.HandlerMethod.Description))
										{
											scx.Add("\t/*\r\n\t");
											scx.Add(hmid.HandlerMethod.Description);
											scx.Add("\r\n\t*/\r\n");
										}
										TraceLogClass.TraceLog.IndentIncrement();
										JsMethodCompiler jmc = new JsMethodCompiler(hmid.HandlerMethod, _rootClassId, Debug, formSubmissions, false);
										StringCollection methodcode = new StringCollection();
										//
										hmid.HandlerMethod.ExportJavaScriptCode(sc, methodcode, jmc);
										for (int i = 0; i < methodcode.Count; i++)
										{
											sc0.Add("\t");
											sc0.Add(methodcode[i]);
										}
										TraceLogClass.TraceLog.IndentDecrement();
										TraceLogClass.TraceLog.Trace("End of compile event method {0}", hmid.HandlerMethod.Name);
									}
									else
									{
										IAction a = GetPublicAction(tid);
										if (a == null)
										{
											_classCompiler.LogError("Action {0} not found from {1}", tid.ToString(), _xmlFile);
										}
										else
										{
											MethodReturnMethod mrm = a.ActionMethod as MethodReturnMethod;
											if (mrm != null)
											{
												mrm.IsAbort = true;
											}
											MethodActionForeachAtClient loop = a as MethodActionForeachAtClient;
											if (loop != null)
											{
												JsMethodCompiler jmc = new JsMethodCompiler(loop, _rootClassId, Debug, formSubmissions, false);
												StringCollection methodcode = new StringCollection();
												////
												loop.ExportJavaScriptCode(sc0, methodcode, jmc);
												////
												sc0.Add(string.Format(CultureInfo.InvariantCulture,
													"{0}();\r\n", loop.MethodName));
											}
											else
											{
												a.CreateJavaScript(sc0, formSubmissions, null, "\t");
												StringCollection dlgCode = null;
												ActionClass ac = a as ActionClass;
												if (ac != null)
												{
													if (ac.JavascriptCodeSegments != null)
													{
														string cv = ac.JavascriptCodeSegments[0][0]; //parameter object name
														if (actList[k].BlockType == EnumWebHandlerBlockType.Dialog)
														{
															if (scLast != null)
															{
																if (a.ActionMethod != null && a.ActionMethod.Owner != null)
																{
																	if (a.ActionMethod.Owner.ObjectType != null && a.ActionMethod.Owner.ObjectType.GetInterface("IWebPage") != null)
																	{
																		if (string.CompareOrdinal(a.ActionMethod.MethodName, "ShowChildDialog") == 0)
																		{
																			dlgCode = new StringCollection();
																			dlgCode.Add(string.Format(CultureInfo.InvariantCulture,
																				"\t{0}.onDialogClose=function() {{\r\n", cv));
																			foreach (string s in scLast)
																			{
																				dlgCode.Add("\t\t");
																				dlgCode.Add(s);
																			}
																			dlgCode.Add("\r\n\t}\r\n");
																		}
																	}
																}
															}
														}
														foreach (string s in ac.JavascriptCodeSegments[1]) //parameter object creation
														{
															sc0.Add(s);
														}
														if (dlgCode != null)
														{
															foreach (string s in dlgCode) //code following dialog
															{
																sc0.Add(s);
															}
														}
														if (ac.JavascriptCodeSegments[2].Count > 0) //has condition
														{
															sc0.Add("if(");
															sc0.Add(ac.JavascriptCodeSegments[2][0]);
															sc0.Add(") {\r\n");
														}
														sc0.Add(string.Format(CultureInfo.InvariantCulture,
															"{0}JsonDataBinding.showChild({1});\r\n", Indentation.GetIndent(), cv));
														if (ac.JavascriptCodeSegments[2].Count > 0) //has condition
														{
															sc0.Add("}\r\nelse {\r\n");
															sc0.Add(string.Format(CultureInfo.InvariantCulture,
																	"\tif({0}.onDialogClose) {{\r\n", cv));
															sc0.Add(string.Format(CultureInfo.InvariantCulture, "\t\t{0}.onDialogClose();\r\n", cv));
															sc0.Add("\t}\r\n");
															sc0.Add("}\r\n");
														}
													}
												}
											}
										}
									}
								}
								VPLUtil.CompilerContext_JS = false;
								sc0.Add("}\r\n");
							}
							//----------------------------------    
						}
						//--create C# server function----------------- 
						if (_project.ProjectType == EnumProjectType.WebAppAspx)
						{
							CodeMemberMethod mm = new CodeMemberMethod();
							mm.Attributes = MemberAttributes.Public;
							mm.Name = serverMethodName;
							SortedDictionary<int, CodeMemberMethod>.Enumerator en = sortedAspxServerCode.GetEnumerator();
							while (en.MoveNext())
							{
								for (int i = 0; i < en.Current.Value.Statements.Count; i++)
								{
									mm.Statements.Add(en.Current.Value.Statements[i]);
								}
							}
							td.Members.Add(mm);
						}
						else //create PHP server function-------------
						{
							serverCode.Methods.Add("function ");
							serverCode.Methods.Add(mc.Name);
							serverCode.Methods.Add("()\r\n{\r\n");
							SortedDictionary<int, StringCollection>.Enumerator en = sortedPhpServerCode.GetEnumerator();
							while (en.MoveNext())
							{
								for (int i = 0; i < en.Current.Value.Count; i++)
								{
									serverCode.Methods.Add(en.Current.Value[i]);
								}
							}
							serverCode.Methods.Add("\r\n}\r\n");
							//
							serverCode.OnRequestExecution.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}();\r\n", mc.Name));
						}
					} //end of if (needServerCall)
					else
					{
						if (k == 0)
						{
							if (!isWinLoad) //onload will be closed after all handlers
							{
								IWebEventProcess wep = ea.Event.Owner.ObjectInstance as IWebEventProcess;
								if (wep != null)
								{
									wep.OnFinishEvent(ea.Event.Name, scx);
								}
								Indentation.IndentDecrease();
								scx.Add(Indentation.GetIndent());
								scx.Add("}\r\n");
							}
						}
					}
					//clean up
					foreach (ActionClass act in htmlTreeViewActions)
					{
						act.RemoveParameterValue(0);
					}
				} //block loop
				foreach (string s in scx)
				{
					scx0.Add(s);
				}
			}// end of if (actList.Count > 0)
			if (assignActionId == 0)
			{
				if (ea.IsExtendWebClientEvent())
				{
					scx0.Add(extendedAttach);
				}
			}
			//support data repeater ==========================
			if (ea.Event != null && ea.Event.Owner != null)
			{
				string dpName = null;
				Control cw = ea.Event.Owner.ObjectInstance as Control;
				if (cw != null)
				{
					Control cp = cw.Parent;
					HtmlDataRepeater dp = null;
					while (cp != null)
					{
						dp = cp as HtmlDataRepeater;
						if (dp != null)
							break;
						cp = cp.Parent;
					}
					if (dp != null)
					{
						dpName = dp.CodeName;
					}
				}
				else
				{
					IWebDataRepeater dp = GetDataRepeaterHolder(ea.Event.Owner.ObjectInstance as HtmlElement_BodyBase);
					if (dp != null)
					{
						dpName = dp.CodeName;
					}
				}
				if (!string.IsNullOrEmpty(dpName))
				{
					scx0.Add(sIndents);
					scx0.Add(string.Format(CultureInfo.InvariantCulture, "{0}.jsData.attachElementEvent('{1}','{2}');\r\n", dpName, ea.Event.Owner.CodeName, ea.EventName));
				}
			}
			if (pageScope)
			{
				int nInsertPoint = 0;
				for (int i = 0; i < sc.Count; i++)
				{
					if (sc[i].IndexOf("window.onload=function() {", StringComparison.Ordinal) >= 0)
					{
						nInsertPoint = i;
						break;
					}
				}
				for (int i = 0; i < pageScopeCode.Count; i++)
				{
					sc.Insert(i + nInsertPoint, pageScopeCode[i]);
				}
			}
			Indentation.SetIndentation(currentIndentation);
		}
		private void createWebServerHandlerPhp(EventAction ea, ServerScriptHolder serverCode)
		{
			EnumRunContext originContext = VPLUtil.CurrentRunContext;
			VPLUtil.CurrentRunContext = EnumRunContext.Server;
			try
			{
				IWebServerCompilerHolder ch = null;
				if (ea.Event.Owner != null)
				{
					ch = ea.Event.Owner.ObjectInstance as IWebServerCompilerHolder;
				}
				foreach (TaskID tid in ea.TaskIDList)
				{
					HandlerMethodID hmid = tid as HandlerMethodID;
					if (hmid != null)
					{
					}
					else
					{
						IAction a = GetPublicAction(tid);
						if (a != null)
						{
							a.ScopeRunAt = EnumWebRunAt.Server;
						}
					}
				}
				//ea.TaskIDList 
				//add server call-------------------
				string serverMethodName = string.Format(CultureInfo.InvariantCulture, "s{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				//
				//create server method--------------
				MethodClass mc = new MethodClass(_rootClassId);
				mc.SetCompilerData(this);
				mc.MethodID = (UInt32)(Guid.NewGuid().GetHashCode());
				mc.Parameters = ea.Event.GetParameters(mc);
				mc.Name = serverMethodName;
				//
				SortedDictionary<int, StringCollection> sortedPhpServerCode = new SortedDictionary<int, StringCollection>();
				Dictionary<string, StringCollection> formSubmissions = new Dictionary<string, StringCollection>();
				JsMethodCompiler jmcServer = new JsMethodCompiler(mc, _rootClassId, Debug, formSubmissions, true);
				//
				foreach (TaskID tid in ea.TaskIDList)
				{
					HandlerMethodID hmid = tid as HandlerMethodID;
					if (hmid != null)
					{
						WebClientEventHandlerMethodServerActions ws = hmid.HandlerMethod as WebClientEventHandlerMethodServerActions;
						if (ws != null)
						{
							if (_project.ProjectType == EnumProjectType.WebAppPhp)
							{
								StringCollection scv;
								if (!sortedPhpServerCode.TryGetValue(tid.TaskOrder, out scv))
								{
									scv = new StringCollection();
									sortedPhpServerCode.Add(tid.TaskOrder, scv);
								}
								VPLUtil.CompilerContext_PHP = true;
								JsMethodCompiler jmc = new JsMethodCompiler(ws, _rootClassId, Debug, formSubmissions, true);
								ws.ExportPhpScriptCode(serverCode.Methods, serverCode.Methods, jmc);
								//
								scv.Add(string.Format(CultureInfo.InvariantCulture,
									"$this->{0}(", ws.CodeName));
								if (ws.ParameterCount > 0)
								{
									scv.Add(ws.Parameters[0].CodeName);
									for (int pi = 1; pi < ws.ParameterCount; pi++)
									{
										scv.Add(",");
										scv.Add(ws.Parameters[pi].CodeName);
									}
								}
								scv.Add(");\r\n");
								VPLUtil.CompilerContext_PHP = false;
							}
						}
					}
					else
					{
						IAction a = GetPublicAction(tid);
						if (a != null)
						{
							if (_project.ProjectType == EnumProjectType.WebAppPhp)
							{
								StringCollection scv;
								if (!sortedPhpServerCode.TryGetValue(tid.TaskOrder, out scv))
								{
									scv = new StringCollection();
									sortedPhpServerCode.Add(tid.TaskOrder, scv);
								}
								VPLUtil.CompilerContext_PHP = true;
								MethodActionForeachAtServer loop = a as MethodActionForeachAtServer;
								if (loop != null)
								{
									JsMethodCompiler jmc = new JsMethodCompiler(loop, _rootClassId, Debug, formSubmissions, true);
									loop.ExportPhpScriptCode(serverCode.Methods, scv, jmc);
								}
								else
								{
									IFormSubmitter fs = null;
									if (a.ActionMethod != null && a.ActionMethod.Owner != null)
									{
										fs = a.ActionMethod.Owner.ObjectInstance as IFormSubmitter;
									}
									if (fs != null && fs.IsSubmissionMethod(a.ActionMethod.MethodName))
									{
									}
									else
									{
										a.ExportPhpScriptCode(null, null, serverCode.Methods, scv, jmcServer);
									}
								}
								VPLUtil.CompilerContext_PHP = false;
							}
						}
					}
				}
				serverCode.Methods.Add("function ");
				serverCode.Methods.Add(mc.Name);
				serverCode.Methods.Add("(");
				for (int i = 0; i < mc.ParameterCount; i++)
				{
					if (i > 0)
					{
						serverCode.Methods.Add(",");
					}
					serverCode.Methods.Add("$");
					serverCode.Methods.Add(mc.Parameters[i].Name);
				}
				serverCode.Methods.Add(")\r\n{\r\n");
				SortedDictionary<int, StringCollection>.Enumerator en = sortedPhpServerCode.GetEnumerator();
				while (en.MoveNext())
				{
					for (int i = 0; i < en.Current.Value.Count; i++)
					{
						serverCode.Methods.Add(en.Current.Value[i]);
					}
				}
				serverCode.Methods.Add("\r\n}\r\n");
				if (ch != null)
				{
					ch.OnCreatedWebServerEventHandler(this, ea.Event.Name, mc.Name);
				}
			}
			finally
			{
				VPLUtil.CurrentRunContext = originContext;
			}
		}
		/// <summary>
		/// a web client handler must arrange the actions in the following sequence:
		/// 1. Client Actions
		/// 2. Server and Upload Actions
		/// 3. Client Handler Methods
		/// 4. Server Handler Methods
		/// 5. Download Handler Methods
		/// 6. Client and Download Actions
		/// </summary>
		/// <param name="sc"></param>
		private void createWebClientHandlers(StringCollection sc, ServerScriptHolder serverCode, string iframe)
		{
			StringCollection sc0 = new StringCollection();
			//
			bool hasBeforeInitPage = false;
			foreach (EventAction ea in _rootClassId.EventHandlers)
			{
				if (typeof(fnInitPage).Equals(ea.Event.EventHandlerType.BaseClassType))
				{
					hasBeforeInitPage = true;
					break;
				}
			}
			//
			sc.Add("\r\nwindow.onload=function() {\r\n");
			if (hasBeforeInitPage)
			{
				sc.Add("\tif(!");
				sc.Add(EventAction.FuncNameBeforeInitPage);
				sc.Add("(window.document.URL, (window == window.parent)?'':window.parent.document.URL)");
				sc.Add(") return;\r\n");
			}
			sc.Add("\r\n\tJsonDataBinding.setServerPage('");
			sc.Add(serverFileName());
			sc.Add("');\r\n");
			//
			sc.Add("\tJsonDataBinding.setupChildManager();\r\n");
			//
			if (_classCompiler.WebDebugMode)
			{
				sc.Add("\tJsonDataBinding.Debug = true;\r\n");
			}
			if (_classCompiler.AjaxTimeout > 0)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture, "\tJsonDataBinding.AjaxTimeout = {0};\r\n", _classCompiler.AjaxTimeout));
			}
			if (_classCompiler.WebClientDebugLevel > 0)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture, "\tJsonDataBinding.DebugLevel = {0};\r\n", _classCompiler.WebClientDebugLevel));
			}
			//
			if (_bodyValues != null && _bodyValues.Count > 0)
			{
				foreach (KeyValuePair<string, IJavascriptType> kv2 in _bodyValues)
				{
					sc.Add(string.Format(CultureInfo.InvariantCulture, "\tdocument.body.{0}={1};\r\n", kv2.Key, kv2.Value.GetValueJsCode()));
				}
			}
			if (_customValues != null && _customValues.Count > 0)
			{
				foreach (KeyValuePair<string, WebClientValueCollection> kv in _customValues)
				{
					if (kv.Value.Count > 0)
					{
						if (!string.IsNullOrEmpty(kv.Key))
						{
							sc.Add(string.Format(CultureInfo.InvariantCulture, "\tvar {0} = document.getElementById('{0}');\r\n", kv.Key));
						}
						foreach (KeyValuePair<string, IJavascriptType> kv2 in kv.Value)
						{
							if (string.IsNullOrEmpty(kv.Key))
							{
								sc.Add(string.Format(CultureInfo.InvariantCulture, "\tdocument.body.{0}={1};\r\n", kv2.Key, kv2.Value.GetValueJsCode()));
							}
							else
							{
								sc.Add(string.Format(CultureInfo.InvariantCulture, "\t{0}.{1}={2};\r\n", kv.Key, kv2.Key, kv2.Value.GetValueJsCode()));
							}
						}
					}
				}
			}
			//
			IWebPage webpage = _rootObject as IWebPage;
			if (webpage != null)
			{
				if (!string.IsNullOrEmpty(webpage.CloseDialogPrompt))
				{
					sc.Add(string.Format(CultureInfo.InvariantCulture,
						  "\tdocument.childManager.CloseDialogPrompt = '{0}';\r\n", webpage.CloseDialogPrompt));
				}
				if (!string.IsNullOrEmpty(webpage.CancelDialogPrompt))
				{
					sc.Add(string.Format(CultureInfo.InvariantCulture,
						  "\tdocument.childManager.CancelDialogPrompt = '{0}';\r\n", webpage.CancelDialogPrompt));
				}
			}
			//
			Indentation.IndentIncrease();
			createWebPageProperties(sc);
			Indentation.IndentDecrease();
			//
			sc.Add("\tJsonDataBinding.ProcessPageParameters();\r\n");
			if (_classCompiler.SessionTimeoutMinutes != 20)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					"\tJsonDataBinding.setSessionTimeout({0});\r\n", _classCompiler.SessionTimeoutMinutes));
			}
			for (int i = 0; i < _classCompiler.SessionVarNames.Length; i++)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture,
							"\tJsonDataBinding.initSessionVariable('{0}','{1}');\r\n",
							_classCompiler.SessionVarNames[i], _classCompiler.SessionVarValues[i]));
			}
			if (_classCompiler.SessionVarNames.Length == 0 && _classCompiler.SessionVarCount > 0)
			{
				sc.Add("\tJsonDataBinding.StartSessionWatcher();\r\n");
			}
			//
			bool usePageResources = false;
			Dictionary<IProperty, UInt32> map = _rootClassId.ResourceMap;
			if (map != null && map.Count > 0)
			{
				ProjectResources rm = _project.GetProjectSingleData<ProjectResources>();
				if (rm.HasResources)
				{
					Dictionary<IProperty, UInt32>.Enumerator en = map.GetEnumerator();
					while (en.MoveNext())
					{
						ResourcePointer rp = rm.GetResourcePointerById(en.Current.Value);
						if (rp != null)
						{
							sc.Add(string.Format(CultureInfo.InvariantCulture, "\tJsonDataBinding.AddPageResourceName('{0}','string');\r\n", rp.Name));
							usePageResources = true;
						}
					}
				}
			}
			//
			IWebPage page = _rootObject as IWebPage;
			WebLoginManager wm = null;
			if (page.LoginPageId == 0)
			{
				foreach (object ov in _objMap.Keys)
				{
					wm = ov as WebLoginManager;
					if (wm != null)
					{
						sc.Add("\r\n\tJsonDataBinding.SetLoginCookieName('");
						sc.Add(wm.COOKIE_UserLogin);
						sc.Add("');\r\n");
						sc.Add("\tJsonDataBinding.ShowPermissionError('");
						if (wm.LabelToShowLoginFailedMessage != null && wm.LabelToShowLoginFailedMessage.Site != null)
						{
							sc.Add(wm.LabelToShowLoginFailedMessage.Site.Name);
						}
						sc.Add("','");
						if (!string.IsNullOrEmpty(wm.LoginPermissionFailedMessage))
						{
							sc.Add(wm.LoginPermissionFailedMessage.Replace("'", "\\'"));
						}
						sc.Add("');\r\n");
						break;
					}
				}
			}
			else
			{
				ClassPointer cp = _project.GetTypedData<ClassPointer>(page.LoginPageId);
				if (cp == null)
				{
					cp = ClassPointer.CreateClassPointer(page.LoginPageId, _project);
					if (cp != null)
					{
						cp.ObjectList.LoadObjects();
					}
					else
					{
						throw new DesignerException("Cannot create login page. Login pag:[{0}, {1}]. Protected page:[{2},{3}]", page.LoginPageId, page.LoginPageName, _rootClassId.ClassId, _rootClassId.Name);
					}
				}
				foreach (object ov in cp.ObjectList.Keys)
				{
					wm = ov as WebLoginManager;
					if (wm != null)
					{
						sc.Add("\r\n\tJsonDataBinding.SetLoginCookieName('");
						sc.Add(wm.COOKIE_UserLogin);
						sc.Add("');\r\n");
						break;
					}
				}
				sc.Add("\r\n\tJsonDataBinding.TargetUserLevel=");
				sc.Add(page.UserLevel.ToString(CultureInfo.InvariantCulture));
				sc.Add(";\r\n");
				string logon = string.Format(CultureInfo.InvariantCulture, "{0}.html?", Path.GetFileNameWithoutExtension(page.LoginPageFileName));
				string logRet = string.Format(CultureInfo.InvariantCulture, "login{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				sc.Add("\tvar ");
				sc.Add(logRet);
				sc.Add("=JsonDataBinding.hasLoggedOn();\r\n");
				sc.Add("\tif(");
				sc.Add(logRet);
				sc.Add(" != 2) {\r\n");
				sc.Add("\t\tvar curUrl = JsonDataBinding.getPageFilename();\r\n");
				sc.Add("\t\twindow.location.href = '");
				sc.Add(logon);
				sc.Add("'+curUrl+((");
				sc.Add(logRet);
				sc.Add("==1)?'$':'');\r\n");
				sc.Add("\t}\r\n\telse {\r\n");
				sc.Add("\t\tJsonDataBinding.setupLoginWatcher();\r\n");
			}
			if (!string.IsNullOrEmpty(iframe))
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture, "\tJsonDataBinding.IFrame = {0};\r\n", iframe));
			}
			foreach (object obj in _rootClassId.ObjectList.Keys)
			{
				IWebClientInitializer wci = obj as IWebClientInitializer;
				if (wci != null)
				{
					wci.OnWebPageLoaded(sc);
				}
			}
			foreach (object obj in _rootClassId.UsedHtmlElements)
			{
				IWebClientInitializer wci = obj as IWebClientInitializer;
				if (wci != null)
				{
					wci.OnWebPageLoaded(sc);
				}
			}
			//
			_useDatetimePicker = false;
			foreach (object obj in _rootClassId.ObjectList.Keys)
			{
				IUseDatetimePicker ud = obj as IUseDatetimePicker;
				if (ud != null)
				{
					if (ud.UseDatetimePicker)
					{
						_useDatetimePicker = true;
					}
				}
			}
			if (!_useDatetimePicker)
			{
				foreach (object obj in _rootClassId.UsedHtmlElements)
				{
					IUseDatetimePicker ud = obj as IUseDatetimePicker;
					if (ud != null)
					{
						if (ud.UseDatetimePicker)
						{
							_useDatetimePicker = true;
						}
					}
				}
			}
			if (_useDatetimePicker)
			{
				sc.Add("\tJsonDataBinding.SetDatetimePicker(datePickerController);\r\n");
			}
			if (usePageResources)
			{
				sc.Add("\r\n\tJsonDataBinding.SwitchCulture();\r\n");
			}
			//merge handlers
			//if onkeyup exist
			//    onenterkey compiled into a function
			//    append an action to execute the function
			//else
			//    onenterkey compiled into onkeyup
			foreach (EventAction ea in _rootClassId.EventHandlers)
			{
				bool isWebClientEvent = ea.IsExtendWebClientEvent();
				if (!isWebClientEvent)
				{
					if (!DesignUtil.IsWebClientObject(ea.Event.Owner))
					{
						if (serverCode != null)
						{
							createWebServerHandlerPhp(ea, serverCode);
						}
						continue;
					}
				}
				HtmlElement_Base heb = ea.Event.Owner as HtmlElement_Base;
				if (heb != null)
				{
					if (!heb.IsValid)
					{
						continue;
					}
				}
				if (ea.Event.Owner.IsSameObjectRef(_rootClassId))
				{
					if (string.CompareOrdinal(ea.Event.Name, "onload") == 0)
					{
						continue;
					}
				}
				if (string.CompareOrdinal(ea.Event.Name, "onenterkey") == 0)
				{
					foreach (EventAction ea0 in _rootClassId.EventHandlers)
					{
						if (string.CompareOrdinal(ea0.Event.Name, "onkeyup") == 0)
						{
							if (ea0.Event.Owner.IsSameObjectRef(ea.Event.Owner))
							{
								ea.CompileToFunction = true;
								createWebClientHandler(ea, sc, sc0, serverCode);
								ea.Disbale = true;
								StringCollection ehsc = new StringCollection();
								ehsc.Add(string.Format(CultureInfo.InvariantCulture,
									"{0}({1});\r\n", ea.EventHandlerName, EventAction.JS_EVENT_KEY));
								JavascriptAction ja = new JavascriptAction(ehsc, _rootClassId);
								TaskID tid = new TaskID(ja.ActionId, _rootClassId.ClassId);
								tid.SetAction(ja);
								ea0.TaskIDList.Add(tid);
								break;
							}
						}
					}
				}
			}
			//
			EventAction eaOnload = null;
			foreach (EventAction ea in _rootClassId.EventHandlers)
			{
				if (ea.Disbale)
				{
					continue;
				}
				HtmlElement_Base heb = ea.Event.Owner as HtmlElement_Base;
				if (heb != null)
				{
					if (!heb.IsValid)
					{
						continue;
					}
				}
				bool isWebClientEvent = ea.IsExtendWebClientEvent();
				if (!isWebClientEvent)
				{
					if (!DesignUtil.IsWebClientObject(ea.Event.Owner))
					{
						continue;
					}
				}
				if (ea.Event.Owner.IsSameObjectRef(_rootClassId))
				{
					if (string.CompareOrdinal(ea.Event.Name, "onload") == 0)
					{
						eaOnload = ea;
						continue;
					}
				}
				if (ea.GetAssignActionId() == 0 && DesignUtil.IsLocalVarPointer(ea.Event.Owner))
				{
					continue;
				}
				createWebClientHandler(ea, sc, sc0, serverCode);
			}
			if (eaOnload != null)
			{
				createWebClientHandler(eaOnload, sc, sc0, serverCode);
			}
			foreach (object obj in _rootClassId.ObjectList.Keys)
			{
				IWebClientInitializer wci = obj as IWebClientInitializer;
				if (wci != null)
				{
					wci.OnWebPageLoadedAfterEventHandlerCreations(sc);
				}
			}
			foreach (HtmlElement_BodyBase obj in _rootClassId.UsedHtmlElements)
			{
				IWebClientInitializer wci = obj as IWebClientInitializer;
				if (wci != null)
				{
					wci.OnWebPageLoadedAfterEventHandlerCreations(sc);
				}
				if (obj.HighlightBackColor != Color.Empty && obj.HighlightBackColor != Color.Transparent)
				{
					string var = string.Format(CultureInfo.InvariantCulture, "v{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					sc.Add(string.Format(CultureInfo.InvariantCulture, "var {0}=document.getElementById('{1}');\r\n", var, obj.CodeName));
					sc.Add(string.Format(CultureInfo.InvariantCulture, "{0}.highlightbkc='{1}';\r\n", var, VPLUtil.GetColorHexString(obj.HighlightBackColor)));
					sc.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.AttachEvent({0},'onmouseover',function(){{if({0}.style.backgroundColor) {0}.restorebkcolor={0}.style.backgroundColor; {0}.style.backgroundColor={0}.highlightbkc;}});\r\n", var));
					sc.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.AttachEvent({0},'onmouseout',function(){{ {0}.style.backgroundColor=({0}.restorebkcolor?{0}.restorebkcolor:'');}});\r\n", var));
				}
			}
			//
			sc.Add("\r\n");
			if (page.LoginPageId != 0)
			{
				sc.Add("\t");
			}
			sc.Add("\tJsonDataBinding.onPageInitialize();\r\n");
			sc.Add("}\r\n");
			if (page.LoginPageId != 0)
			{
				sc.Add("\r\n}\r\n");
			}
			//sc0 collects custom event-link through IJavaScriptEventOwner
			for (int i = 0; i < sc0.Count; i++)
			{
				sc.Add(sc0[i]);
			}
		}
		#endregion
		#region public properties
		public string AppName
		{
			get
			{
				return _appname;
			}
		}
		public string KioskBackFormName
		{
			get
			{
				return _kioskBackgroundName;
			}
		}
		public bool UseDatetimePicker
		{
			get
			{
				return _useDatetimePicker;
			}
		}
		public ClassPointer ClassData
		{
			get
			{
				return _rootClassId;
			}
		}
		public ObjectIDmap ObjectMap
		{
			get
			{
				return _objMap;
			}
		}
		public object RootObject
		{
			get
			{
				return _rootObject;
			}
		}
		public virtual string ComponentFile
		{
			get
			{
				return _xmlFile;
			}
		}
		public string PropertyHolderVarName
		{
			get
			{
				if (string.IsNullOrEmpty(_propertyHolderName))
				{
					_propertyHolderName = "_p" + Guid.NewGuid().GetHashCode().ToString("x");
				}
				return _propertyHolderName;
			}
		}
		public List<IAppConfigConsumer> AppConfigConsumers
		{
			get
			{
				return _appConfigConsumers;
			}
		}
		public CodeExpression PropertyHolderRef
		{
			get
			{
				return new CodeVariableReferenceExpression(PropertyHolderVarName);
			}
		}
		public CodeExpression DebuggerRef
		{
			get
			{
				return new CodeVariableReferenceExpression(LimnorDebugger.Debugger);
			}
		}
		public bool SupportPageNavigator
		{
			get
			{
				return _usePageNavigator;
			}
		}
		public bool Debug
		{
			get
			{
				return _debug;
			}
		}
		public bool UseDrawing
		{
			get
			{
				return _useDrawing;
			}
		}
		public virtual string ResourceFile
		{
			get
			{
				return _settings.ResourceFilename;
			}
		}
		public virtual string ResourceFileX
		{
			get
			{
				return _settings.ResourceFilenameX;
			}
		}
		public virtual string Resources
		{
			get
			{
				return _settings.Resources;
			}
		}
		public virtual string ResourcesX
		{
			get
			{
				return _settings.ResourcesX;
			}
		}
		public virtual string SourceFile
		{
			get
			{
				return _settings.SourceFilename + ".cs";
			}
		}
		public virtual string SourceFileX
		{
			get
			{
				return _settings.SourceFilenameX + ".cs";
			}
		}
		public virtual string HtmlFile
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}.html", _settings.Name);
			}
		}
		public virtual string JsFile
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}.js", _settings.Name);
			}
		}
		public virtual string AspxFile
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}.aspx", _settings.Name);
			}
		}
		public virtual string PhpFile
		{
			get
			{
				if (string.CompareOrdinal(_settings.Name, "index") == 0)
				{
					return "index0833.php";
				}
				return string.Format(CultureInfo.InvariantCulture, "{0}.php", _settings.Name);
			}
		}
		public bool UseResources
		{
			get
			{
				return _useResources;
			}
		}
		public bool UseResourcesX
		{
			get
			{
				return _useResourcesX;
			}
		}
		public bool IsSetup
		{
			get
			{
				return _isSetup;
			}
		}
		public Type ObjectType
		{
			get
			{
				return _objectType;
			}
		}
		public List<ResourceFile> ResourceFiles
		{
			get
			{
				return _resourceFileList;
			}
		}
		public ArrayList Errors
		{
			get
			{
				return _errors;
			}
		}
		public bool HasErrors
		{
			get
			{
				if (_errors != null)
				{
					return (_errors.Count > 0);
				}
				return false;
			}
		}
		public bool IsEntryPoint
		{
			get
			{
				return (_rootObject is LimnorApp) && !(_rootObject is LimnorWebApp);
			}
		}
		public bool IsForm
		{
			get
			{
				return (_rootObject is Form);
			}
		}
		public bool IsWebService
		{
			get
			{
				return _isWebService;
			}
		}
		public bool IsWebApp
		{
			get
			{
				return _isWebApp;
			}
		}
		public bool IsWebObject
		{
			get
			{
				return (_isWebApp || _isWebPage);
			}
		}
		public Dictionary<string, Assembly> References
		{
			get
			{
				return _refLocations;
			}
		}
		public ClassPointer ActionEventList
		{
			get
			{
				return _rootClassId;
			}
		}
		#endregion
		#region IObjectFactory Members

		public System.ComponentModel.IComponent CreateInstance(Type type, string name)
		{
			IComponent c = this.LoaderHost.CreateComponent(type, name);
			VPLUtil.FixPropertyValues(c);
			return c;
		}
		public IContainer ComponentContainer
		{
			get
			{
				return _container;
			}
			set
			{
				_container = value;
			}
		}
		#endregion
		#region ILimnorCodeCompiler Members

		public CodeTypeDeclaration TypeDeclaration
		{
			get { return td; }
		}
		public void AddError(string error)
		{
			logMessage(error);
		}
		#endregion
		#region IWebServerCompiler Members

		public void AppendServerPagePhpCode(StringCollection code)
		{
			if (code != null)
			{
				foreach (string s in code)
				{
					_serverScripts.Methods.Add(s);
				}
			}
		}

		public void AppendServerExecutePhpCode(StringCollection code)
		{
			if (code != null)
			{
				foreach (string s in code)
				{
					_serverScripts.OnRequestExecution.Add(s);
				}
			}
		}

		#endregion
	}
}
