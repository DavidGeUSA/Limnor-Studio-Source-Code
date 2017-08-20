/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using System.Globalization;
using XmlSerializer;
using System.Xml;
using DynamicEventLinker;
using System.CodeDom.Compiler;
using MathExp;
using System.Runtime.Serialization;
using System.CodeDom;
using VSPrj;
using XmlUtility;
using ProgElements;
using VPL;
using LimnorDesigner.MenuUtil;
using LimnorDesigner.Property;
using LimnorDesigner.Event;
using LimnorDesigner.MethodBuilder;
using System.Collections.Specialized;
using LimnorDesigner.Interface;
using LimnorDesigner.Action;
using Limnor.Drawing2D;
using System.ComponentModel.Design;
using System.Web.Services.Protocols;
using System.Web.Services;
using LimnorDatabase;
using LimnorDesigner.ResourcesManager;
using Limnor.WebBuilder;
using System.Collections;
using Limnor.WebServerBuilder;
using System.ServiceModel;
using Limnor.Remoting.Host;
using LimnorDesigner.Web;
using LimnorDesigner.EventMap;
using LimnorDesigner.DesignTimeType;
using TraceLog;
using Limnor.Reporting;
using WindowsUtility;
using System.Xml.Serialization;

namespace LimnorDesigner
{
	public enum EnumObjectSelectType { All, Object, Method, Event, Action, Type, BaseClass, InstanceType, Interface }
	public enum EnumObjectSelectSubType { All, Property, Component }
	/// <summary>
	/// pointer to custom developed objects, such as class, property, method, event.
	/// it cannot be used to point to a property/method/event from a library.
	/// </summary>
	public interface ICustomObject
	{
		UInt64 WholeId { get; }
		UInt32 ClassId { get; }
		UInt32 MemberId { get; }
		string Name { get; }
	}
	/// <summary>
	/// pointer to objects, including classes, properties, events, and methods.
	/// the objects can be from libraries or by development
	/// </summary>
	public interface IObjectPointer : IObjectIdentity, ISerializerProcessor, IPropertyPointer
	{
		/// <summary>
		/// the class holding (not neccerily declaring) this pointer
		/// </summary>
		ClassPointer RootPointer { get; }
		/// <summary>
		/// direct owner 
		/// </summary>
		IObjectPointer Owner { get; set; }
		/// <summary>
		/// for visual debug, this is the object created
		/// </summary>
		object ObjectDebug { get; set; }
		/// <summary>
		/// fully qualified variable name
		/// </summary>
		string ReferenceName { get; }
		/// <summary>
		/// firendly name formed by {Property name}:{type name}
		/// </summary>
		string DisplayName { get; }
		/// <summary>
		/// {object name}.{Property name}:{type name}
		/// </summary>
		string LongDisplayName { get; }
		/// <summary>
		/// representation in a math expression
		/// </summary>
		string ExpressionDisplay { get; }
		/// <summary>
		/// test whether it is for the targeted selection
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		bool IsTargeted(EnumObjectSelectType target);
		/// <summary>
		/// uniquely identify the object
		/// </summary>
		string ObjectKey { get; }
		/// <summary>
		/// class type in string so that none-compiled classes can be referenced when compiling
		/// </summary>
		string TypeString { get; }
		/// <summary>
		/// indicates whether the pointer points to a valid value
		/// </summary>
		bool IsValid { get; }
		/// <summary>
		/// compile the code
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue);
		void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver);
		void CreateActionPhpScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver);
		string GetJavaScriptReferenceCode(StringCollection code);
		string GetPhpScriptReferenceCode(StringCollection code);
		EnumWebRunAt RunAt { get; }
	}
	/// <summary>
	/// represents a PME holder formed by IObjectPointer and ICustomObject
	/// </summary>
	public interface IClass : IObjectPointer, ICustomObject
	{
		Image ImageIcon { get; set; }
		//a PME holder can only be one of the following types.==============
		//only one of the following 3 properties can be non-null
		Type VariableLibType { get; }
		ClassPointer VariableCustomType { get; }
		IClassWrapper VariableWrapperType { get; }
		//==================================================================
		UInt32 DefinitionClassId { get; }
		//==================================================================
		/// <summary>
		/// The container, can be ClassPointer or ClassInstancePointer
		/// </summary>
		IClass Host { get; }
	}
	/// <summary>
	/// implemented by ClassPointer and ClassInstancePointer
	/// </summary>
	public interface IClassContainer : IClass
	{
		ClassPointer Definition { get; }
		ClassPointer RootHost { get; }
	}
	public interface IGenericTypePointer
	{
		/// <summary>
		/// for a generic type returns corresponding concrete types for generic arguments
		/// </summary>
		/// <returns></returns>
		DataTypePointer[] GetConcreteTypes();
		/// <summary>
		/// for a generic type returns corresponding concrete type for the given generic argument
		/// </summary>
		/// <param name="typeParameter"></param>
		/// <returns></returns>
		DataTypePointer GetConcreteType(Type typeParameter);
		/// <summary>
		/// for a non-generic type returns normal CodeTypeReference
		/// for a generic type returns the CodeTypeReference taking the concrete types into consideration
		/// </summary>
		/// <returns></returns>
		CodeTypeReference GetCodeTypeReference();
		/// <summary>
		/// each generic type is defined by a DataTypePointer with its BaseClassType being the generic parameter and its _concretTypeForTypeParameter being the concrete type
		/// </summary>
		/// <returns></returns>
		IList<DataTypePointer> GetGenericTypes();
	}
	/// <summary>
	/// pointer to an object added by the developer.
	/// A root object is a self-contained entity in a project. One root object is saved in one xml file.
	/// One root object represents one class to be developed.
	/// Other objects may be added to a root object. 
	/// IComponent is added in design mode. A non-IComponent is added as a private field and a public property.
	/// Xml content is the only holder for the design process.
	/// Every root object has a unique int ID among the project.
	/// Every object contained in a root object has a unique int ID among the objects contained in the root object.
	/// 
	/// A class is a type. So usually a class ID is not the same as the id for an instance even the instance is of the type.
	/// The only class and instance have the same id are the Application and its auto-created instance.
	/// 
	/// </summary>
	//=========================
	/// <summary>
	/// a pointer to a class in the project.
	/// SaveAsProperties:it is saved in a ObjProperty element via WriteObjectToNode
	/// saved:     Serializable properties
	///     
	/// </summary>
	[NotForProgramming]
	[SaveAsProperties]
	public class ClassPointer : IClassContainer, IWithProject, IClassPointer, IXmlNodeSerializable, ICustomTypeDescriptor, IAttributeHolder, IWithCollection, IDrawingOwnerPointer, IDrawingPageHolder, IActionsHolder, IDevClass
	{
		#region fields and constructors
		private UInt64 _wholeId;
		private Image _icon;
		private bool _isStatic;
		private ObjectIDmap _objectList;
		private Dictionary<UInt32, IAction> _actions; //
		private List<EventAction> _eventActions;
		private UInt32 _baseClassId; //saved
		private ClassPointer _baseClassPointer; //not saved
		//
		private UInt32 _editVersion;
		//
		private object _lockObject = new object();
		private bool _loaded; //whether ReadRootObject is called
		//
		private HtmlElement_Base _selectedHtmlElement;
		private List<HtmlElement_BodyBase> _usedHtmlElements;
		private bool bCyclicCheck_getCustomProperties;
		private bool bCyclicCheck_getCustomMethods;
		//
		//========properties==========================================
		//
		private Dictionary<string, PropertyPointer> _libProperties;//used by interfaces
		//
		private List<PropertyClassInherited> _propertyOverrides;

		//custom properties defined in this class, including new and overrides
		private List<PropertyClass> _currentLevelPropertyList;
		//custom properties defined in this class and in the base classes, properties from base classes and not overriden are created as PropertyClassInherited
		private List<PropertyClass> _wholePropertyList;
		//includes _wholePropertyList and PropertyClassInherited instances from BaseType
		//use dictionary so that it can handle large number of virtual properties
		private Dictionary<string, PropertyClass> _customPropertyList;
		//=======methods==============================================
		//
		private Dictionary<string, MethodInfoPointer> _libMethods;//used by interfaces
		//
		private List<MethodClassInherited> _methodOverrides;
		private List<MethodClass> _currentLevelMethodList;
		private List<MethodClass> _wholeMethodList;
		private Dictionary<string, MethodClass> _customMethodList;
		//======events================================================
		private Dictionary<string, EventPointer> _libEvents;//used by interfaces
		private List<EventClass> _currentLevelEventList;
		private List<EventClass> _wholeEventList;
		private Dictionary<string, EventClass> _customEventList;
		//============================================================
		private bool _usePageNavigator;
		//=====resources mapping======================================
		private Dictionary<IProperty, UInt32> _resourceMap; //property -> resource id
		//============================================================
		/// <summary>
		/// for deserialization of derived types (CustomEventHandlerType)
		/// </summary>
		public ClassPointer()
		{
		}
		/// <summary>
		/// it does not load custom P/M/E so as to avoid potential cyclic loading.
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="memberId"></param>
		/// <param name="name"></param>
		/// <param name="objList"></param>
		/// <param name="xml"></param>
		/// <param name="obj"></param>
		public ClassPointer(UInt32 classId, UInt32 memberId, ObjectIDmap objList, List<UInt32> derivedClassIdList)
		{
			_wholeId = DesignUtil.MakeDDWord(memberId, classId);
			_objectList = objList;
			Type rootType = _objectList.RootObjectType;
			if (rootType == null)
			{
				throw new DesignerException("Class base type not resolved:class id:{0}, member id:{1}, name:{2}", classId, memberId, XmlUtil.GetNameAttribute(_objectList.XmlData));
			}
			if (DesignUtil.IsApp(rootType))
			{
				IsStatic = true;
				AlwaysStatic = true;
			}
			_isStatic = SerializeUtil.IsStatic(_objectList.XmlData);
			_objectList.SetTypedData<ClassPointer>(this);
			_baseClassId = XmlUtil.GetAttributeUInt(_objectList.XmlData, XmlTags.XMLATT_BaseClassId);
			if (_baseClassId != 0)
			{
				//load base class
				if (derivedClassIdList == null)
				{
					derivedClassIdList = new List<uint>();
				}
				derivedClassIdList.Add(classId);
				_baseClassPointer = ClassPointer.CreateClassPointer(_baseClassId, _objectList.Project, derivedClassIdList);
			}
		}
		public ClassPointer(ObjectIDmap objList, List<UInt32> derivedClassIdList)
			: this(
			objList.ClassId,
			objList.MemberId,
			objList,
			derivedClassIdList
			)
		{
		}
		public ClassPointer(ObjectIDmap objList)
			: this(objList, null)
		{
		}
		#endregion
		#region static ClassPointer Factory
		public static ObjectIDmap GetObjectmap(LimnorProject project, XmlNode classNode)
		{
			if (project == null)
			{
				throw new DesignerException("Calling ClassPointer.GetObjectmap with null project");
			}
			if (classNode == null)
			{
				throw new DesignerException("Calling ClassPointer.GetObjectmap with null xml node");
			}
			UInt32 classId = XmlUtil.GetAttributeUInt(classNode, XmlTags.XMLATT_ClassID);
			ObjectIDmap map = project.GetTypedData<ObjectIDmap>(classId);
			if (map == null)
			{
				UInt32 memberId = XmlUtil.GetAttributeUInt(classNode, XmlTags.XMLATT_ComponentID);
				map = new ObjectIDmap(project, XmlUtil.MakeDDWord(memberId, classId), classNode);
				XmlObjectReader xr = new XmlObjectReader(map, ClassPointer.OnAfterReadRootComponent, ClassPointer.OnRootComponentCreated);
				map.SetReader(xr);
				map.SetTypedData<ObjectIDmap>(map);
			}
			map.SetXmlData(classNode);
			return map;
		}
		public static ClassPointer CreateClassPointer(ObjectIDmap map)
		{
			return CreateClassPointer(map.ClassId, map.Project);
		}

		static public ClassPointer CreateClassPointer(LimnorProject project, XmlNode rootNode)
		{
			UInt32 classId = XmlUtil.GetAttributeUInt(rootNode, XmlTags.XMLATT_ClassID);
			return CreateClassPointer(classId, project, null);
		}
		static public ClassPointer CreateClassPointer(UInt32 classId, LimnorProject project)
		{
			return CreateClassPointer(classId, project, null);
		}
		static private ClassPointer CreateClassPointer(UInt32 classId, LimnorProject project, List<UInt32> derivedClassIdList)
		{
			if (classId == 0)
				return null; //when load base class it indicates no base class
			if (derivedClassIdList != null)
			{
				if (derivedClassIdList.Contains(classId))
				{
					throw new DesignerException("Class [{0}] is used as its own base class", classId);
				}
			}
			ClassPointer cp = project.GetTypedData<ClassPointer>(classId);
			if (cp != null)
			{
				return cp;
			}
			ObjectIDmap map = project.GetTypedData<ObjectIDmap>(classId);
			if (map != null)
			{
				//this constructor will call this method back to load its base class
				cp = new ClassPointer(map, derivedClassIdList);
			}
			else
			{
				//not in cache, load it from file
				XmlNode node = GetRootNodeByClassId(classId, project);
				if (node != null)
				{
					map = ClassPointer.GetObjectmap(project, node);
					UInt32 memberId = map.MemberId;
					cp = new ClassPointer(classId, memberId, map, derivedClassIdList);
				}
			}
			return cp;
		}

		public static XmlNode GetRootNodeByClassId(UInt32 classId, LimnorProject project)
		{
			ILimnorDesignPane pane = project.GetTypedData<ILimnorDesignPane>(classId);
			if (pane != null)
			{
				return pane.RootXmlNode;
			}
			ObjectIDmap map = project.GetTypedData<ObjectIDmap>(classId);
			if (map != null)
			{
				return map.XmlData;
			}
			XmlDocument doc = project.GetDocumentByClassId(classId);
			if (doc == null)
			{
				DesignUtil.WriteToOutputWindow("Class [{0}] is not in the project", classId);
				return null;
			}
			return doc.DocumentElement;
		}
		/// <summary>
		/// reset all designers
		/// </summary>
		/// <param name="project"></param>
		public static void OnCompileFinish(LimnorProject project)
		{
			Dictionary<UInt32, ILimnorDesignPane> panes = project.GetTypedDataList<ILimnorDesignPane>();
			if (panes != null)
			{
				foreach (KeyValuePair<UInt32, ILimnorDesignPane> kv in panes)
				{
					kv.Value.Loader.ResetObjectMap();
				}
			}
		}
		static public void OnRootComponentCreated(IComponent root, XmlNode Node, XmlObjectReader reader)
		{
			ClassPointer cp = ClassPointer.CreateClassPointer(reader.ObjectList.Project, Node);
			List<HtmlElement_BodyBase> hs = new List<HtmlElement_BodyBase>();
			XmlNodeList hnodes = Node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}", XmlTags.XML_HtmlElementList, XmlTags.XML_HtmlElement));
			if (hnodes != null && hnodes.Count > 0)
			{
				foreach (XmlNode hn in hnodes)
				{
					string acid;
					Type t = XmlUtil.GetLibTypeAttribute(hn, out acid);
					HtmlElement_BodyBase element = Activator.CreateInstance(t, cp) as HtmlElement_BodyBase;
					element.OnReadFromXmlNode(reader, hn);
					hs.Add(element);
				}
			}
			cp.SetUsedElements(hs);
		}
		/// <summary>
		/// called after XmlObjectReader.ReadRootObject()
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Node"></param>
		/// <param name="reader"></param>
		static public void OnAfterReadRootComponent(IComponent root, XmlNode Node, XmlObjectReader reader)
		{
			if (reader.ObjectList == null)
			{
				throw new DesignerException("Invalid reader: map is null");
			}
			ClassPointer cp = ClassPointer.CreateClassPointer(reader.ObjectList.Project, Node);
			cp.SetLoaded();
			//read class references
			XmlNodeList nl = SerializeUtil.GetClassRefNodeList(Node);
			foreach (XmlNode nd in nl)
			{
				ClassInstancePointer cr = reader.ReadObject<ClassInstancePointer>(nd, cp);
				if (cr.ObjectInstance == null)
				{
					MathNode.Log(TraceLogClass.MainForm, new DesignerException("instance not created for [{0},{1}] in class [{2}]", cr.ClassId, cr.MemberId, reader.ObjectList.ClassId));
				}
				reader.ObjectList.SetClassRefMap(cr.ObjectInstance, cr);
			}
			if (root is LimnorApp)
			{
				reader.ReadStaticProperties(Node);
				//for showing app name in property grid
				if (root.Site != null)
				{
					LimnorApp.SetStaticName(root.Site.Name);
				}
			}
			else
			{
				if (reader.ObjectList.Project.ProjectType == EnumProjectType.ClassDLL)
				{
					using (XmlDoc d = reader.ObjectList.Project.GetVob())
					{
						XmlNode vobNode = d.Doc.DocumentElement;
						if (vobNode != null)
						{
							reader.ReadStaticProperties(vobNode);
						}
					}
				}
			}
			//load used types
			XmlNodeList nodes = SerializeUtil.GetExternalTypes(Node);
			if (nodes != null && nodes.Count > 0)
			{
				TypePointerCollection tpC = new TypePointerCollection();
				List<Type> tpList = new List<Type>();
				foreach (XmlNode nd in nodes)
				{
					string acid;
					Type t = XmlUtil.GetLibTypeAttribute(nd, out acid);
					if (t != null)
					{
						tpList.Add(t);
						tpC.Add(new TypePointer(t));
					}
				}
				reader.ObjectList.Project.AddToolboxItems(tpList.ToArray(), true);
				reader.ObjectList.SetTypedData<TypePointerCollection>(tpC);
			}
			//resolve references
			if (reader.References != null)
			{
				//reference-owner, Dictionary<prop-of-ref-owner, ref-name>
				foreach (KeyValuePair<object, Dictionary<PropertyDescriptor, string>> kv in reader.References)
				{
					//kv.Key is the ref-owner, kv2.Key is the prop, kv2.Value is the ref-name
					foreach (KeyValuePair<PropertyDescriptor, string> kv2 in kv.Value)
					{
						//find referenced component by name
						IComponent ds = null;
						foreach (object obj in reader.ObjectList.Keys)
						{
							IComponent dc = obj as IComponent;
							if (dc != null && dc.Site != null)
							{
								//matching the name
								if (string.Compare(dc.Site.Name, kv2.Value, StringComparison.Ordinal) == 0)
								{
									ds = dc;
									break;
								}
							}
						}
						if (ds == null)
						{
							//special case: ref-owner is a ContextMenuStrip, prop name is OwnerItem
							//prop type is ToolStripMenuItem (ToolStripItem?)
							//find a ToolStripMenuItem with its Text == the reference name
							if (string.CompareOrdinal(kv2.Key.Name, "OwnerItem") == 0)
							{
								ContextMenuStrip cms = kv.Key as ContextMenuStrip;
								if (cms != null)
								{
									foreach (object obj in reader.ObjectList.Keys)
									{
										ToolStripMenuItem tsmi = obj as ToolStripMenuItem;
										if (tsmi != null)
										{
											if (string.Compare(tsmi.Text, kv2.Value, StringComparison.Ordinal) == 0)
											{
												ds = tsmi;
												break;
											}
										}
									}
								}
							}
						}
						if (ds == null)
						{
							if ((kv.Key is ContextMenuStrip) && (string.CompareOrdinal(kv2.Key.Name, "OwnerItem") == 0))
							{
							}
							else
							{
								reader.addErrStr2("Referenced component {0} not found", kv2.Value);
							}
						}
						else
						{
							kv2.Key.SetValue(kv.Key, ds);
						}
					}
				}
			}
			//resolve instance references
			object[] objs = null;
			foreach (object obj in reader.ObjectList.Keys)
			{
				IComponentReferenceHolder crh = obj as IComponentReferenceHolder;
				if (crh != null)
				{
					if (objs == null)
					{
						objs = new object[reader.ObjectList.Keys.Count];
						reader.ObjectList.Keys.CopyTo(objs, 0);
					}
					crh.ResolveComponentReferences(objs);
				}
				IDevClassReferencer dcr = VPLUtil.GetObject(obj) as IDevClassReferencer;
				if (dcr != null)
				{
					dcr.SetDevClass(cp);
				}
				IXmlCodeReaderWriterHolder wh = obj as IXmlCodeReaderWriterHolder;
				if (wh != null)
				{
					wh.SetWriter(new XmlObjectWriter(reader.ObjectList));
					wh.SetReader(reader);
				}
			}
			//initialize database accessors
			Dictionary<EasyGrid, List<DataGridViewColumn>> egcols = new Dictionary<EasyGrid, List<DataGridViewColumn>>();
			foreach (object obj in reader.ObjectList.Keys)
			{
				IDatabaseAccess eds = obj as IDatabaseAccess;
				if (eds != null)
				{
					EasyGrid eg = eds as EasyGrid;
					List<DataGridViewColumn> cols = null;
					if (eg != null)
					{
						cols = new List<DataGridViewColumn>();
						foreach (object obj2 in reader.ObjectList.Keys)
						{
							DataGridViewColumn col0 = obj2 as DataGridViewColumn;
							if (col0 != null && col0.DataGridView == eg)
							{
								cols.Add(col0);
							}
						}
						egcols.Add(eg, cols);
					}
					eds.CreateDataTable();
				}
			}
			foreach (KeyValuePair<EasyGrid, List<DataGridViewColumn>> kv0 in egcols)
			{
				EasyGrid eg = kv0.Key;
				List<DataGridViewColumn> cols = kv0.Value;
				for (int i = 0; i < cols.Count; i++)
				{
					for (int j = 0; j < eg.Columns.Count; j++)
					{
						if (string.CompareOrdinal(cols[i].DataPropertyName, eg.Columns[j].DataPropertyName) == 0)
						{
							eg.Columns[j].Name = cols[i].Name;
							if (eg.Columns[j].Site != null)
							{
								eg.Columns[j].Site.Name = cols[i].Name;
							}
							foreach (KeyValuePair<object, UInt32> kv in reader.ObjectList)
							{
								if (kv.Key == cols[i])
								{
									UInt32 id = kv.Value;
									reader.ObjectList.Remove(kv.Key);
									reader.ObjectList.Add(eg.Columns[j], id);
									break;
								}
							}
							break;
						}
					}
				}
			}
			//initialize data-binding
			if (reader.Bindings != null)
			{
				foreach (KeyValuePair<object, Dictionary<PropertyDescriptor, List<BindingLoader>>> kv in reader.Bindings)
				{
					foreach (KeyValuePair<PropertyDescriptor, List<BindingLoader>> kv2 in kv.Value)
					{
						ControlBindingsCollection cbc = kv2.Key.GetValue(kv.Key) as ControlBindingsCollection;
						if (cbc == null)
						{
							reader.addErrStr2("ControlBindingsCollection not found for {0}.{1}", kv.Key, kv2.Key.Name);
						}
						foreach (BindingLoader bl in kv2.Value)
						{
							IComponent ds = null;
							foreach (object obj in reader.ObjectList.Keys)
							{
								IComponent dc = obj as IComponent;
								if (dc != null && dc.Site != null)
								{
									if (string.CompareOrdinal(dc.Site.Name, bl.SourceName) == 0)
									{
										ds = dc;
										break;
									}
								}
							}
							if (ds == null)
							{
								reader.addErrStr2("Binding source {0} not found", bl.SourceName);
							}
							else
							{
								try
								{
									cbc.Add(new Binding(bl.Property, ds, bl.Member));
								}
								catch (Exception err)
								{
									reader.addErrStr2("Binding source {0}.{1} not found: {2}", bl.SourceName, bl.Member, err.Message);
								}
							}
						}
					}
				}
			}
			//initialize drawing page
			DrawingPage dp = root as DrawingPage;
			if (dp != null)
			{
				//support 2D drawing
				dp.DrawingOwnerDesinTimePointer = cp;
				dp.OnDeserialize();
				foreach (object obj in reader.ObjectList.Keys)
				{
					DrawingItem draw = obj as DrawingItem;
					if (draw != null)
					{
						DrawingLayer layer = dp.DrawingLayers[0];
						if (draw.LayerId != Guid.Empty)
						{
							foreach (DrawingLayer l in dp.DrawingLayers)
							{
								if (l.LayerId == draw.LayerId)
								{
									layer = l;
									break;
								}
							}
						}
						layer.Add(draw);
					}
				}
				dp.SetPage();
			}
			//read custom property values
			cp.LoadCustomPropertyValues(reader);
			//post deserialize
			foreach (object obj in reader.ObjectList.Keys)
			{
				IPostDeserializeProcess pdp = obj as IPostDeserializeProcess;
				if (pdp != null)
				{
					pdp.OnDeserialize(cp);
				}
			}
			IList<IPostDeserializeProcess> prs = reader.ObjectList.GetPostProcessors();
			if (prs != null)
			{
				foreach (IPostDeserializeProcess pdp in prs)
				{
					pdp.OnDeserialize(cp);
				}
			}
			string[] errors = null;
			if (reader.Errors != null)
			{
				errors = new string[reader.Errors.Count];
				reader.Errors.CopyTo(errors, 0);
			}
			reader.ObjectList.ClearPostPprocessors();
			//
			cp.GetCurrentLevelCustomMethods();
			cp.GetCurrentLevelCustomEvents();
			cp.GetCurrentLevelCustomProperties();
			//
			cp.LoadActionInstances();
			//
			cp.ObjectList.RefreshObjectTree();
			if (reader.PostRootDeserializers != null)
			{
				IPostRootDeserialize[] prds = new IPostRootDeserialize[reader.PostRootDeserializers.Count];
				reader.PostRootDeserializers.CopyTo(prds, 0);
				for (int i = 0; i < prds.Length; i++)
				{
					prds[i].OnPostRootDeserialize();
				}
			}
			if (errors != null)
			{
				for (int i = 0; i < errors.Length; i++)
				{
					reader.addErrStr2(errors[i]);
				}
			}
		}

		#endregion
		#region Properties
		[Browsable(false)]
		[XmlIgnore()]
		[ReadOnly(true)]
		public bool InBatchSaving { get; set; }
		private TypeX _designtimeType;
		[Browsable(false)]
		public TypeX TypeDesigntime
		{
			get
			{
				if (_designtimeType == null)
				{
					_designtimeType = new TypeX(this);
				}
				return _designtimeType;
			}
		}
		[Browsable(false)]
		public virtual bool IsInterface
		{
			get
			{
				Type t = BaseClassType;
				if (t != null)
				{
					if (t.IsInterface)
					{
						return true;
					}
					if (typeof(InterfaceClass).Equals(t))
					{
						return true;
					}
				}
				if (ObjectInstance != null)
				{
					if (typeof(InterfaceClass).Equals(VPLUtil.GetObjectType(ObjectInstance)))
					{
						return true;
					}
				}
				return false;
			}
		}
		[Browsable(false)]
		public UInt32 EditVersion
		{
			get
			{
				return _editVersion;
			}
		}
		[Browsable(false)]
		public bool IsWebService
		{
			get
			{
				return typeof(WebService).IsAssignableFrom(BaseClassType);
			}
		}
		[Browsable(false)]
		public bool IsWebPage
		{
			get
			{
				return typeof(WebPage).IsAssignableFrom(BaseClassType);
			}
		}
		[Browsable(false)]
		public bool IsApp
		{
			get
			{
				return typeof(LimnorApp).IsAssignableFrom(BaseClassType);
			}
		}
		[Browsable(false)]
		public bool IsWebApp
		{
			get
			{
				return typeof(LimnorWebApp).IsAssignableFrom(BaseClassType);
			}
		}
		[Browsable(false)]
		public bool IsWebObject
		{
			get
			{
				return (IsWebPage || IsWebApp);
			}
		}
		[Browsable(false)]
		public bool IsRemotingHost
		{
			get
			{
				return typeof(RemotingHost).IsAssignableFrom(BaseClassType);
			}
		}
		[Browsable(false)]
		public bool ActionsLoaded
		{
			get
			{
				return (_actions != null);
			}
		}
		[Browsable(false)]
		public MethodClass MethodInEditing { get; set; }
		/// <summary>
		/// if this class defines an interface then this is the Interface definition represented by an InterfaceClass object
		/// </summary>
		[Browsable(false)]
		public virtual InterfaceClass Interface
		{
			get
			{
				InterfaceClass ifc = VPLUtil.GetObject(ObjectInstance) as InterfaceClass;
				if (ifc != null)
				{
					ifc.Holder = this;
				}
				return ifc;
			}
		}
		[Browsable(false)]
		[Description("The data type of the base class")]
		public Type BaseClassType
		{
			get
			{
				return VPLUtil.GetObjectType(ObjectType);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[ReadOnly(true)]
		[Browsable(false)]
		public XmlNode XmlData
		{
			get
			{
				if (_objectList != null)
					return _objectList.XmlData;
				return null;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public ObjectIDmap ObjectList
		{
			get
			{
				return _objectList;
			}
		}
		[Browsable(false)]
		public string ContextMenuFile
		{
			get
			{
				if (_objectList == null)
					return null;
				string f = _objectList.ClassFile;
				if (!string.IsNullOrEmpty(f))
				{
					return LimnorContextMenuCollection.GetTypePathDirect(this.BaseClassType);
				}
				return f;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual string TypeString
		{
			get
			{
				if (string.IsNullOrEmpty(Namespace))
					return Name;
				return Namespace + "." + Name;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public bool AlwaysStatic { get; set; }

		[Browsable(false)]
		public IObjectIdentity IdentityOwner
		{
			get
			{
				return Owner;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public IObjectPointer Owner
		{
			get
			{
				return null;
			}
			set
			{
			}
		}
		[Browsable(false)]
		public IPropertyPointer PropertyOwner { get { return Owner; } }

		[Browsable(false)]
		public Guid ClassGuid
		{
			get
			{
				if (_objectList == null)
					return Guid.Empty;
				return _objectList.ClassGuid;
			}
		}

		/// <summary>
		/// class id of the base class. 0: base class is from lib
		/// </summary>
		[Browsable(false)]
		public UInt32 BaseClassId
		{
			get
			{
				return _baseClassId;
			}
		}
		/// <summary>
		/// base class. null: base class is from lib, ObjectType
		/// </summary>
		[Browsable(false)]
		public ClassPointer BaseClassPointer
		{
			get
			{
				return _baseClassPointer;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public UInt64 WholeId
		{
			get
			{
				return _wholeId;
			}
			set
			{
				_wholeId = value;
			}
		}

		[ReadOnly(true)]
		[ParenthesizePropertyName(true)]
		[Description("Class or member name")]
		public virtual string Name
		{
			get
			{
				return XmlUtil.GetNameAttribute(_objectList.XmlData);
			}
			set
			{
			}
		}
		[Browsable(false)]
		public bool IconFileExists
		{
			get
			{
				string s = FullIconPath;
				if (!string.IsNullOrEmpty(s))
				{
					return System.IO.File.Exists(s);
				}
				return false;
			}
		}
		[Browsable(false)]
		public bool IsIconFile
		{
			get
			{
				string s = FullIconPath;
				if (!string.IsNullOrEmpty(s))
				{
					return string.Compare(System.IO.Path.GetExtension(s), ".ico", StringComparison.OrdinalIgnoreCase) == 0;
				}
				return false;
			}
		}
		[Browsable(false)]
		public string FullIconPath
		{
			get
			{
				string s = IconFilename;
				if (!string.IsNullOrEmpty(s) && _objectList != null)
				{
					return System.IO.Path.Combine(_objectList.Project.ProjectFolder, s);
				}
				return null;
			}
		}
		[Browsable(false)]
		protected virtual string IconFilename
		{
			get
			{
				return SerializeUtil.ReadIconFilename(this.XmlData);
			}
		}
		public List<EventAction> EventHandlers
		{
			get
			{
				if (_eventActions == null)
				{
					ReloadEventHandlers();
				}
				return _eventActions;
			}
		}
		[Browsable(false)]
		public bool UseDrawing
		{
			get
			{
				if (typeof(DrawingPage).IsAssignableFrom(BaseClassType))
				{
					return true; //TBD: load all levels of actions to check methods and parameters for drawing usages
				}
				XmlNodeList nds = XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					"//*[@{0}]", XmlTags.XMLATT_type));
				foreach (XmlNode nd in nds)
				{
					string acid;
					Type t = XmlUtil.GetLibTypeAttribute(nd, out acid);
					if (t != null)
					{
						if (typeof(DrawingItem).IsAssignableFrom(t))
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		public bool UsingPageNavigator
		{
			get
			{
				return _usePageNavigator;
			}
		}
		public bool UsingCom
		{
			get
			{
				if (this.BaseClassType.IsCOMObject)
				{
					return true;
				}
				XmlNodeList ns = XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture, "//*[@{0}]", XmlTags.XMLATT_type));
				if (ns != null && ns.Count > 0)
				{
					foreach (XmlNode nd in ns)
					{
						if (string.CompareOrdinal(nd.ParentNode.Name, "Types") != 0)
						{
							string acid;
							Type t = XmlUtil.GetLibTypeAttribute(nd, out acid);
							if (t != null)
							{
								if (typeof(AxHost).IsAssignableFrom(t))
								{
									return true;
								}
							}
						}
					}
				}
				return false;
			}
		}
		[Browsable(false)]
		public IList<HtmlElement_BodyBase> UsedHtmlElements
		{
			get
			{
				if (_usedHtmlElements == null)
				{
					_usedHtmlElements = new List<HtmlElement_BodyBase>();
				}
				bool hasBody = false;
				foreach (HtmlElement_Base he in _usedHtmlElements)
				{
					if (he is HtmlElement_body)
					{
						hasBody = true;
						break;
					}
				}
				if (!hasBody)
				{
					_usedHtmlElements.Add(new HtmlElement_body(this));
				}
				return _usedHtmlElements;
			}
		}
		#endregion
		#region Serializable properties
		[Browsable(false)]
		public string Namespace { get; set; }
		[Browsable(false)]
		public UInt32 DefinitionClassId
		{
			get
			{
				return ClassId;
			}
		}
		[Browsable(false)]
		public virtual UInt32 ClassId
		{
			get
			{
				UInt32 c, a;
				DesignUtil.ParseDDWord(WholeId, out a, out c);
				return c;
			}
			set
			{
				UInt32 c, a;
				DesignUtil.ParseDDWord(WholeId, out a, out c);
				_wholeId = DesignUtil.MakeDDWord(a, value);
			}
		}
		[Browsable(false)]
		public virtual UInt32 MemberId
		{
			get
			{
				UInt32 c, a;
				DesignUtil.ParseDDWord(WholeId, out a, out c);
				return a;
			}
			set
			{
				UInt32 c, a;
				DesignUtil.ParseDDWord(WholeId, out a, out c);
				_wholeId = DesignUtil.MakeDDWord(value, c);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		[Browsable(false)]
		[Description("A static variable does not belong to an instance of class. There will be just one instance of a static variable")]
		public bool IsStatic
		{
			get
			{
				if (AlwaysStatic)
					return true;
				return _isStatic;
			}
			set
			{
				if (AlwaysStatic)
				{
					_isStatic = true;
				}
				else
				{
					_isStatic = value;
				}
			}
		}
		#endregion
		#region Event and Action
		public ActionClass CreateNewAction(object target, XmlObjectWriter xw, IMethod scopeMethod, IActionsHolder holder, Form caller)
		{
			bool bOK;
			if (target == null)
			{
				bOK = false;
			}
			else
			{
				ActionClass act = new ActionClass(this);
				act.ActionHolder = holder;
				IMethod method = target as IMethod;
				if (method != null)
				{
					act.ActionMethod = method.CreateMethodPointer(act) as IActionMethodPointer;
					bOK = true;
				}
				else
				{
					IActionMethodPointer amp = target as IActionMethodPointer;
					if (amp != null)
					{
						act.ActionMethod = amp;
						bOK = true;
					}
					else
					{
						MethodDataTransfer mdt = target as MethodDataTransfer;
						if (mdt != null)
						{
							act.ActionMethod = mdt;
							mdt.Action = act;
							bOK = true;
						}
						else
						{
							IPropertyEx pp = target as IPropertyEx;
							if (pp != null)
							{
								act.ActionMethod = pp.CreateSetterMethodPointer(act);
								bOK = true;
							}
							else
							{
								PropertyClass pc = target as PropertyClass;
								if (pc != null)
								{
									act.ActionMethod = pc.CreateSetterMethodPointer(act);
									bOK = true;
								}
								else
								{
									CustomPropertyPointer cpp = target as CustomPropertyPointer;
									if (cpp != null)
									{
										SetterPointer mp = new SetterPointer(act);
										mp.SetProperty = cpp;
										bOK = true;
									}
									else
									{
										CustomEventPointer cep = target as CustomEventPointer;
										if (cep != null)
										{
											FireEventMethod fem = new FireEventMethod(cep);
											act.ActionMethod = fem;
											bOK = true;
										}
										else
										{
											bOK = false;
										}
									}
								}
							}
						}
					}
				}
				if (bOK)
				{
					act.ActionName = act.ActionMethod.DefaultActionName;
					bOK = CreateNewAction(act, xw, scopeMethod, caller);
					if (bOK)
					{
						return act;
					}
				}
			}
			return null;
		}
		public bool CreateNewAction(IAction actNew, XmlObjectWriter xw, IMethod scopeMethod, Form caller)
		{
			XmlNode rootNode = _objectList.XmlData;
			if (actNew.ActionMethod.Owner == this && string.CompareOrdinal(actNew.ActionMethod.MethodName, "ShowDialog") == 0 && (actNew.ActionMethod.Owner.ObjectInstance is Form))
			{
				if (MessageBox.Show(caller, "Calling ShowDialog on itself may cause runtime error. ShowDialog is usually used on other form instances. Do you want to continue?", "Create action", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				{
					return false;
				}
			}
			DesignUtil.InitializeNewAction(rootNode, actNew);//create new ID and name
			if (scopeMethod != null)
			{
				actNew.ScopeMethod = scopeMethod;
				MethodClass mc = scopeMethod as MethodClass;
				if (mc != null && mc.MemberId == 0)
				{
					actNew.AsLocal = false;
				}
				else
				{
					actNew.AsLocal = true;
				}
				if (actNew.ActionHolder == null)
				{
					actNew.ActionHolder = scopeMethod as IActionsHolder;
				}
			}
			else
			{
				if (actNew.ActionHolder == null)
				{
					actNew.ActionHolder = this;
				}
			}

			if (actNew.Edit(xw, scopeMethod, caller, true))
			{
				_editVersion++;
				return true;
			}
			return false;
		}
		public ActionClass CreateDataTransferAction(Form caller)
		{
			ActionClass act = new ActionClass(this);
			act.ActionHolder = this;
			act.ActionMethod = new MethodDataTransfer(this);
			act.ActionName = act.ActionMethod.DefaultActionName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return act;
		}
		public ActionClass CreateSetPropertyAction(PropertyClass property)
		{
			ActionClass act = new ActionClass(this);
			act.ActionMethod = property.CreateSetterMethodPointer(act);
			act.ActionName = act.ActionMethod.DefaultActionName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return act;
		}

		public void SetEventLink(IEvent e)
		{
			IEventPointer ep = e as IEventPointer;
			if (ep != null)
			{
				CompilerErrorCollection errors = DynamicLink.LinkEvent(ep, taskExecuter);
				if (errors != null && errors.Count > 0)
				{
					StringCollection sc = new StringCollection();
					for (int i = 0; i < errors.Count; i++)
					{
						sc.Add(errors[i].ErrorText);
					}
					MathNode.Log(sc);
				}
			}
		}
		public void SaveEventBreakPointsToXml(string eventName, string objKey)
		{
			if (_eventActions == null)
			{
				ReloadEventHandlers();
			}
			foreach (EventAction ea in _eventActions)
			{
				if (ea.Event.Name == eventName)
				{
					if (ea.Event.Owner.ObjectKey == objKey)
					{
						ea.SaveEventBreakPointsToXml();
						break;
					}
				}
			}
		}
		public EventAction GetEventHandler(IEvent ep)
		{
			if (_eventActions == null)
			{
				ReloadEventHandlers();
			}
			foreach (EventAction ea in _eventActions)
			{
				if (ea.Event.IsSameObjectRef(ep))
				{
					return ea;
				}
			}
			return null;
		}
		public EventAction GetEventHandler(string eventName, string objKey)
		{
			if (_eventActions == null)
			{
				ReloadEventHandlers();
			}
			foreach (EventAction ea in _eventActions)
			{
				if (ea.Event.Name == eventName)
				{
					if (ea.Event.Owner.ObjectKey == objKey)
					{
						return ea;
					}
				}
			}
			return null;
		}

		private void taskExecuter(IEventPointer eventPointer, object[] eventParameters)
		{
			try
			{
				foreach (EventAction ea in _eventActions)
				{
					EventPointer ep = eventPointer as EventPointer;
					if (ea.Event.IsSameObjectRef(ep))
					{
						List<ParameterClass> eventValues = new List<ParameterClass>();
						if (eventParameters != null && eventParameters.Length > 0)
						{
							ParameterInfo[] pifs = ep.Parameters;
							if (pifs.Length != eventParameters.Length)
							{
								throw new DesignerException("Event {0} parameter count mismatch", ep.MemberName);
							}
							for (int i = 0; i < pifs.Length; i++)
							{
								ParameterClass p = new ParameterClass(new TypePointer(pifs[i].ParameterType), null);
								p.Name = pifs[i].Name;
								p.ObjectInstance = eventParameters[i];
								eventValues.Add(p);
							}
						}
						//execute the event handlers
						TaskIdList actIdList = ea.TaskIDList;
						foreach (TaskID tid in actIdList)
						{
							UInt32 classId = tid.ClassId;
							//find the action in the list of all actions
							Dictionary<UInt32, IAction> acts = null;
							if (classId == ClassId)
							{
								acts = _actions;
							}
							else
							{
								ClassPointer cp = _objectList.Project.GetTypedData<ClassPointer>(classId);
								if (cp != null)
								{
									acts = cp.GetActions();
								}
							}
							if (acts != null)
							{
								IAction a;
								if (acts.TryGetValue(tid.ActionId, out a))
								{
									if (a != null)
									{
										a.Execute(eventValues);
									}
								}
							}
						}
						break;
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		[Browsable(false)]
		public void SetUsedElements(List<HtmlElement_BodyBase> usedElements)
		{
			_usedHtmlElements = usedElements;
		}
		[Browsable(false)]
		public IAction GetActionInstance(UInt32 actId)
		{
			return GetActionObject(actId, this, this);
		}
		[Browsable(false)]
		public void AddActionInstance(IAction action)
		{
			if (_actions == null)
			{
				_actions = new Dictionary<uint, IAction>();
			}
			bool found = _actions.ContainsKey(action.ActionId);
			if (!found)
			{
				_actions.Add(action.ActionId, action);
			}
		}
		[Browsable(false)]
		public static IAction GetActionObject(UInt32 actId, IActionsHolder actsHolder, ClassPointer root)
		{
			if (actId == 0)
			{
				return NoAction.Value;
			}
			Dictionary<UInt32, IAction> acts = actsHolder.ActionInstances;
			if (acts != null)
			{
				IAction a;
				if (acts.TryGetValue(actId, out a))
				{
					return a;
				}
			}
			if (actsHolder.ActionsNode != null)
			{
				XmlObjectReader xr = root.ObjectList.Reader;
				xr.ResetErrors();
				List<IPostOwnersSerialize> objs = new List<IPostOwnersSerialize>();
				xr.PushPostOwnersDeserializers(objs);
				XmlNode actNode = actsHolder.ActionsNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
					"{0}[@{1}='{2}']",
					XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, actId));
				if (actNode == null)
				{
					actNode = actsHolder.ActionsNode.OwnerDocument.DocumentElement.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
					"//{0}[@{1}='{2}']",
					XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, actId));
				}
				if (actNode != null)
				{
					acts = new Dictionary<uint, IAction>();
					root.loadAction(actNode, actsHolder, acts);
					IAction a;
					if (acts.TryGetValue(actId, out a))
					{
						return a;
					}
				}
			}
			return null;
		}
		public void SaveEventHandlers()
		{
			if (_eventActions != null)
			{
				ObjectIDmap objMap = _objectList;
				XmlNode rootNode = objMap.XmlData;
				XmlNode ndHandlerList = rootNode.SelectSingleNode(XmlTags.XML_HANDLERLISTS);
				if (ndHandlerList != null)
				{
					rootNode.RemoveChild(ndHandlerList);
				}
				List<EventAction> list = new List<EventAction>();
				foreach (EventAction ea in _eventActions)
				{
					list.Add(ea);
				}
				foreach (EventAction ea in list)
				{
					SaveEventHandler(ea);
				}
			}
		}
		public void SaveEventHandler(EventAction handler)
		{
			try
			{
				ObjectIDmap objMap = _objectList;
				XmlNode rootNode = objMap.XmlData;
				XmlObjectReader xr = objMap.Reader;
				XmlObjectWriter xw = new XmlObjectWriter(objMap);
				XmlNode ndHandlerList = rootNode.SelectSingleNode(XmlTags.XML_HANDLERLISTS);
				if (ndHandlerList == null)
				{
					ndHandlerList = rootNode.OwnerDocument.CreateElement(XmlTags.XML_HANDLERLISTS);
					rootNode.AppendChild(ndHandlerList);
				}
				XmlNode nodeHandler = null;
				XmlNodeList ndHandlers = ndHandlerList.SelectNodes(XmlTags.XML_HANDLER);
				UInt32 abId = handler.GetAssignActionId();
				foreach (XmlNode nd in ndHandlers)
				{
					EventAction ea = (EventAction)xr.ReadObject(nd, null);
					if (ea != null)
					{
						if (ea.GetAssignActionId() == abId && ea.Event.IsSameObjectRef(handler.Event))
						{
							nodeHandler = nd;
							break;
						}
					}
				}
				if (xr.Errors != null && xr.Errors.Count > 0)
				{
					MathNode.Log(xr.Errors);
				}
				if (nodeHandler == null)
				{
					nodeHandler = ndHandlerList.OwnerDocument.CreateElement(XmlTags.XML_HANDLER);
					ndHandlerList.AppendChild(nodeHandler);
				}
				else
				{
					if (nodeHandler.ParentNode == null)
					{
						ndHandlerList.AppendChild(nodeHandler);
					}
				}
				handler.XmlNode = nodeHandler;
				WebPage wpage = this.ObjectInstance as WebPage;
				if (wpage != null)
				{
					HtmlElement_BodyBase heb = handler.Event.Owner as HtmlElement_BodyBase;
					if (heb != null)
					{
						OnUseHtmlElement(heb);
					}
				}
				if (abId != 0)
				{
					XmlUtil.SetAttribute(nodeHandler, XmlTags.XMLATT_ActionID, abId);
				}
				xw.WriteObjectToNode(nodeHandler, handler);
				if (xw.HasErrors)
				{
					MathNode.Log(xw.ErrorCollection);
				}
				LoadActionInstances();
				if (_eventActions != null)
				{
					foreach (EventAction ea in _eventActions)
					{
						if (abId == ea.GetAssignActionId() && ea.Event.IsSameObjectRef(handler.Event))
						{
							_eventActions.Remove(ea);
							break;
						}
					}
					_eventActions.Add(handler);
				}
			}
			catch (Exception e)
			{
				MathNode.Log(TraceLogClass.MainForm, e);
			}
		}
		#endregion
		#region public methods
		public XmlObjectWriter CreateWriter()
		{
			if (_objectList != null)
				return new XmlObjectWriter(_objectList);
			return null;
		}
		public XmlObjectReader CreateReader()
		{
			if (_objectList != null)
				return _objectList.Reader;
			return null;
		}
		public bool HasFormSubmitter()
		{
			if (_objectList != null)
			{
				foreach (object v in _objectList.Keys)
				{
					IFormSubmitter ifs = v as IFormSubmitter;
					if (ifs != null)
					{
						return true;
					}
				}
			}
			if (_usedHtmlElements != null)
			{
				foreach (HtmlElement_Base he in _usedHtmlElements)
				{
					if (he is IFormSubmitter)
					{
						return true;
					}
				}
			}
			return false;
		}
		public IComponent GetComponentByName(string name)
		{
			if (string.CompareOrdinal(name, this.Name) == 0)
				return this.ObjectInstance as IComponent;
			if (_objectList != null)
			{
				foreach (object v in _objectList.Keys)
				{
					IComponent ic = v as IComponent;
					if (ic != null && ic.Site != null && string.CompareOrdinal(name, ic.Site.Name) == 0)
					{
						return ic;
					}
				}
			}
			return null;
		}
		public void SetRunContext()
		{
			if (this.Project.IsWebApplication)
			{
				if (this.RunAt == EnumWebRunAt.Client)
				{
					VPLUtil.CurrentRunContext = EnumRunContext.Client;
				}
				else
				{
					if (this.ObjectType != null && this.ObjectType.GetInterface("IWebClientComponent") != null)
					{
						VPLUtil.CurrentRunContext = EnumRunContext.Client;
					}
					else
					{
						VPLUtil.CurrentRunContext = EnumRunContext.Server;
					}
				}
			}
			else
			{
				VPLUtil.CurrentRunContext = EnumRunContext.Server;
			}
		}
		public void ResetDefaultInstance()
		{
			FrmObjectExplorer.RemoveDialogCaches(this.Project.ProjectGuid, this.ClassId);
		}
		public bool IsLoadingMethods()
		{
			if (_wholeMethodList == null)
			{
				return bCyclicCheck_getCustomMethods;
			}
			return false;
		}
		public void AddComponent(Form caller)
		{
			try
			{
				ILimnorDesignPane pane = getDesigner("AddComponent");
				if (pane == null) return;
				if (caller == null)
				{
					caller = pane.PaneHolder.FindForm();
				}
				DataTypePointer dp = DesignUtil.SelectDataType(this.Project, this, null, EnumObjectSelectType.Type, null, null, null, caller);
				if (dp != null)
				{
					string name = pane.Loader.CreateNewComponentName(dp.Name, null);
					if (dp.IsLibType)
					{
						IComponent newIc;
						ToolboxItemXType.SelectedToolboxClassId = 0;
						ToolboxItemXType.SelectedToolboxClassName = string.Empty;
						Type t = dp.BaseClassType;
						if (t.GetInterface("IComponent") == null)
						{
							t = VPLUtil.GetXClassType(t);
						}
#if DEBUGX
                        FormStringList.ShowDebugMessage("Start: call AddComponent {0}", t.Name);
                        try
                        {
                            object obj = Activator.CreateInstance(t);
                            if (obj != null)
                            {
                                FormStringList.ShowDebugMessage("Test OK");
                            }
                            else
                            {
                                FormStringList.ShowDebugMessage("Test cannot create a value");
                            }
                        }
                        catch (Exception err)
                        {
                            FormStringList.ShowDebugMessage("Test Failed: {0}", DesignerException.FormExceptionText(err));
                        }
#endif
						object va = Activator.CreateInstance(t);
						if (va != null)
						{
						}
						newIc = pane.Loader.Host.CreateComponent(t, name);
						Control c = newIc as Control;
						if (c != null && c.Parent == null)
						{
							if (!(c is TabPage) && !(c is ContextMenuStrip))
							{
								Control parentC = pane.Loader.RootObject as Control;
								if (parentC != null)
								{
									parentC.Controls.Add(c);
									c.Location = parentC.PointToClient(Cursor.Position);
									if (c.Location.X < 0 || c.Location.X > parentC.ClientSize.Width)
									{
										c.Left = parentC.ClientSize.Width / 2;
									}
									if (c.Location.Y < 0 || c.Location.Y > parentC.ClientSize.Height)
									{
										c.Top = parentC.ClientSize.Height / 2;
									}
									c.Visible = true;
								}
							}
						}
					}
					else
					{
						ClassPointer defClass = dp.ClassTypePointer;
						ToolboxItemXType.SelectedToolboxClassId = defClass.ClassId;
						ToolboxItemXType.SelectedToolboxClassName = defClass.Name;
						pane.Loader.Host.CreateComponent(typeof(ClassInstancePointer), name);
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
		}
		public bool OnBeforeUseComponent(IClass obj, Form caller)
		{
			if (obj != null && !(obj is HtmlElementUnknown))
			{
				HtmlElement_Base heb = obj as HtmlElement_Base;
				EnumElementValidation eev = heb.Validate(caller);
				switch (eev)
				{
					case EnumElementValidation.Fail:
						return false;
					case EnumElementValidation.New: //guid created
						break;
					case EnumElementValidation.Pass: // existing guid
						break;
				}
				return true;
			}
			return false;
		}
		public IGuidIdentified GetElementByGuid(Guid g)
		{
			return FindHtmlElementByGuid(g);
		}
		public HtmlElement_BodyBase FindHtmlElementByGuid(Guid g)
		{
			if (_usedHtmlElements != null && _usedHtmlElements.Count > 0)
			{
				foreach (HtmlElement_BodyBase he in _usedHtmlElements)
				{
					if (he.ElementGuid == g)
					{
						return he;
					}
				}
			}
			return null;
		}
		public void OnUseHtmlElement(HtmlElement_BodyBase element)
		{
			HtmlElement_BodyBase hei = element as HtmlElement_BodyBase;
			if (hei != null)
			{
				if (hei.ElementGuid == Guid.Empty)
				{
					Guid g = Guid.NewGuid();
					WebPage wpage = this.ObjectInstance as WebPage;
					hei.SetGuid(g);
					HtmlElement_ItemBase item = hei as HtmlElement_ItemBase;
					if (item != null)
					{
						if (string.IsNullOrEmpty(item.id))
						{
							string id = wpage.CreateOrGetIdForCurrentElement(hei.ElementGuidString);
							item.SetId(id);
						}
						else
						{
							wpage.SetGuidById(item.id, hei.ElementGuidString);
						}
					}
				}
			}
			if (string.IsNullOrEmpty(element.ReferenceName))
			{
				throw new DesignerException("Html element does not have an id");
			}
			if (element.ElementGuid == Guid.Empty)
			{
				throw new DesignerException("Html element [{0}] is not prepared.", element.ReferenceName);
			}
			SaveHtmlElement(element);
			ILimnorDesignPane dp = getDesigner("on use html element");
			if (dp != null)
			{
				dp.OnUseHtmlElement(element);
			}
		}
		public void SaveHtmlElement(HtmlElement_BodyBase element)
		{
			if (_usedHtmlElements == null)
			{
				_usedHtmlElements = new List<HtmlElement_BodyBase>();
			}
			bool found = false;
			for (int i = 0; i < UsedHtmlElements.Count; i++)
			{
				if (_usedHtmlElements[i].ElementGuid == element.ElementGuid)
				{
					_usedHtmlElements[i] = element;
					found = true;
					break;
				}
			}
			if (!found)
			{
				_usedHtmlElements.Add(element);
			}
			XmlObjectWriter xw = new XmlObjectWriter(this.ObjectList);
			WriteUsedHtmlElement(element, xw);
			if (xw.HasErrors)
			{
				MathNode.Log(xw.ErrorCollection);
			}
		}
		public void WriteUsedHtmlElement(HtmlElement_BodyBase element, XmlObjectWriter xw)
		{
			XmlNode htmlElementsNode = XmlData.SelectSingleNode(XmlTags.XML_HtmlElementList);
			if (htmlElementsNode == null)
			{
				htmlElementsNode = XmlData.OwnerDocument.CreateElement(XmlTags.XML_HtmlElementList);
				XmlData.AppendChild(htmlElementsNode);
			}
			XmlNode eNode = htmlElementsNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
				"{0}[@{1}='{2}']", XmlTags.XML_HtmlElement, XmlTags.XMLATT_guid, element.ElementGuidString));
			if (eNode == null)
			{
				eNode = XmlData.OwnerDocument.CreateElement(XmlTags.XML_HtmlElement);
				htmlElementsNode.AppendChild(eNode);
			}
			element.OnWriteToXmlNode(xw, eNode);
		}
		public void SaveHtmlElements(IXmlCodeWriter xw0)
		{
			if (_usedHtmlElements != null)
			{
				XmlObjectWriter xw = (XmlObjectWriter)xw0;
				for (int i = 0; i < _usedHtmlElements.Count; i++)
				{
					WriteUsedHtmlElement(_usedHtmlElements[i], xw);
				}
			}
		}
		public void RemoveHtmlElement(HtmlElement_ItemBase element)
		{
			if (_usedHtmlElements != null)
			{
				for (int i = 0; i < _usedHtmlElements.Count; i++)
				{
					if (_usedHtmlElements[i].ElementGuid == element.ElementGuid)
					{
						_usedHtmlElements.RemoveAt(i);
						break;
					}
				}
			}
			XmlNode eNode = XmlData.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}[@{2}='{3}']", XmlTags.XML_HtmlElementList, XmlTags.XML_HtmlElement, XmlTags.XMLATT_guid, element.ElementGuidString));
			if (eNode != null)
			{
				XmlNode pNode = eNode.ParentNode;
				pNode.RemoveChild(eNode);
			}
			XmlNodeList enodes = XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}[@{2}='{3}']", EventPath.XML_EventPath, XmlTags.XML_Item, XmlTags.XMLATT_guid, element.ElementGuidString));
			if (enodes != null && enodes.Count > 0)
			{
				foreach (XmlNode en in enodes)
				{
					string acid;
					Type t = XmlUtil.GetLibTypeAttribute(en, out acid);
					if (t != null && typeof(HtmlElement_ItemBase).IsAssignableFrom(t))
					{
						XmlNode pNode = en.ParentNode;
						pNode.RemoveChild(en);
					}
				}
			}
			List<EventAction> invalidEHs = new List<EventAction>();
			List<EventAction> eas = EventHandlers;
			foreach (EventAction ea in eas)
			{
				if (ea != null && ea.Event != null)
				{
					HtmlElement_ItemBase hei = ea.Event.Owner as HtmlElement_ItemBase;
					if (hei != null && hei.ElementGuid == element.ElementGuid)
					{
						invalidEHs.Add(ea);
					}
				}
			}
			if (invalidEHs.Count > 0)
			{
				foreach (EventAction ea in invalidEHs)
				{
					if (ea.XmlData != null)
					{
						XmlNode pNode = ea.XmlData.ParentNode;
						pNode.RemoveChild(ea.XmlData);
					}
					eas.Remove(ea);
				}
			}
		}
		public void OnSelectHtmlElement(string selectedElement)
		{
			if (!string.IsNullOrEmpty(selectedElement))
			{
				if (selectedElement.StartsWith("html,", StringComparison.OrdinalIgnoreCase))
				{
					ILimnorDesignPane pane = getDesigner("OnSelectHtmlElement");
					if (pane == null) return;
					pane.Loader.NotifySelection(this.ObjectInstance);
				}
				else
				{
					HtmlElement_Base selected = HtmlElement_Base.CreateHtmlElement(this, selectedElement);
					if (selected != null)
					{
						_selectedHtmlElement = selected;
						ILimnorDesignPane pane = getDesigner("OnSelectHtmlElement");
						if (pane == null) return;
						pane.Loader.NotifySelection(_selectedHtmlElement);
						pane.OnHtmlElementSelected(_selectedHtmlElement);
					}
				}
			}
		}
		private void mnu_useHtmlElement(object sender, EventArgs e)
		{
			HtmlElement_BodyBase bb = _selectedHtmlElement as HtmlElement_BodyBase;
			if (bb != null)
			{
				WebPage wpage = this.ObjectInstance as WebPage;
				UseHtmlElement(bb, wpage);
			}
		}
		private void mnu_doHtmlCopy(object sender, EventArgs e)
		{
			WebPage wpage = this.ObjectInstance as WebPage;
			if (wpage != null)
			{
				wpage.OnDoCopy();
			}
		}
		private void mnu_doHtmlPaste(object sender, EventArgs e)
		{
			WebPage wpage = this.ObjectInstance as WebPage;
			if (wpage != null)
			{
				wpage.OnDoPaste();
			}
		}
		public void OnRightClickElement(string selectedElement, int x, int y)
		{
			OnSelectHtmlElement(selectedElement);
			if (_selectedHtmlElement != null)
			{
				WebPage wpage = this.ObjectInstance as WebPage;
				if (wpage != null)
				{
					Point p = Point.Empty;
					if (x != 0 && y != 0)
					{
						p = new Point(x, y);
					}
					if (p.IsEmpty)
					{
						p = System.Windows.Forms.Cursor.Position;
					}
					ContextMenu cmn = new ContextMenu();
					CreateContextMenu(_selectedHtmlElement, cmn, p, wpage.FindForm());
					if (cmn.MenuItems.Count > 0)
					{
						cmn.MenuItems.Add(new MenuItem("-"));
					}
					MenuItemWithBitmap mi = new MenuItemWithBitmap("Copy", mnu_doHtmlCopy, Resources._copy.ToBitmap());
					cmn.MenuItems.Add(mi);
					mi = new MenuItemWithBitmap("Paste", mnu_doHtmlPaste, Resources.paste);
					cmn.MenuItems.Add(mi);
					if (_selectedHtmlElement.ElementGuid == Guid.Empty)
					{
						HtmlElement_BodyBase bb = _selectedHtmlElement as HtmlElement_BodyBase;
						if (bb != null)
						{
							if (cmn.MenuItems.Count > 0)
							{
								cmn.MenuItems.Add(new MenuItem("-"));
							}
							mi = new MenuItemWithBitmap("Use for programming", mnu_useHtmlElement, Resources.a_actiongroup);
							cmn.MenuItems.Add(mi);
						}
					}
					if (cmn.MenuItems.Count > 0)
					{
						//html editor is visiable, wpage is not visible
						cmn.Show(wpage.GetHtmlEditor() as Control, p);
					}
				}
			}
		}
		public HtmlElement_Base ParseHtmlElement(string s)
		{
			return HtmlElement_Base.CreateHtmlElement(this, s);
		}
		public void OnSelectHtmlLink(string selectedLink)
		{
			HtmlElement_Base selected = HtmlElement_Base.CreateHtmlHeadElement(this, selectedLink);
			if (selected != null)
			{
				_selectedHtmlElement = selected;
				ILimnorDesignPane pane = getDesigner("OnSelectHtmlElement");
				if (pane == null) return;
				pane.Loader.NotifySelection(_selectedHtmlElement);
				pane.OnHtmlElementSelected(_selectedHtmlElement);
			}
		}
		public void RefreshHtmlElementSelection()
		{
			if (_selectedHtmlElement != null)
			{
				ILimnorDesignPane pane = getDesigner("OnSelectHtmlElement");
				if (pane == null) return;
				pane.Loader.NotifySelection(_selectedHtmlElement);
			}
		}
		public void OnElementIdChanged(Guid g, string id)
		{
			HtmlElement_ItemBase e = FindHtmlElementByGuid(g) as HtmlElement_ItemBase;
			if (e != null)
			{
				e.SetId(id);
				ILimnorDesignPane pane = getDesigner("OnElementIdChanged");
				if (pane == null) return;
				pane.Loader.NotifyChanges();
				pane.OnComponentRename(e, id);
			}
		}
		public void RemoveInvalidHtmlComponentIcons()
		{
			XmlNodeList nodes = XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}[@{2}='ComponentIconList']/{3}", EventPath.XML_EventPath, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_Item));
			if (nodes != null && nodes.Count > 0)
			{
				List<XmlNode> invalids = new List<XmlNode>();
				foreach (XmlNode nd in nodes)
				{
					string acid;
					Type t = XmlUtil.GetLibTypeAttribute(nd, out acid);
					if (t != null && typeof(ComponentIconHtmlElement).IsAssignableFrom(t) && !typeof(ComponentIconHtmlElementCurrent).IsAssignableFrom(t))
					{
						string g = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_guid);
						if (!string.IsNullOrEmpty(g))
						{
							Guid gid = new Guid(g);
							HtmlElement_BodyBase heb = FindHtmlElementByGuid(gid);
							if (heb == null)
							{
								invalids.Add(nd);
							}
						}
					}
				}
				if (invalids.Count > 0)
				{
					foreach (XmlNode nd in invalids)
					{
						XmlNode pn = nd.ParentNode;
						pn.RemoveChild(nd);
					}
				}
			}
		}
		public void RemoveInvalidHtmlEventHandlers()
		{
			List<EventAction> ehs = EventHandlers;
			if (ehs.Count > 0)
			{
				List<XmlNode> invalidehs = new List<XmlNode>();
				foreach (EventAction ea in ehs)
				{
					HtmlElement_ItemBase hei = ea.Event.Owner as HtmlElement_ItemBase;
					if (hei != null)
					{
						if (hei.ElementGuid == Guid.Empty)
						{
							invalidehs.Add(ea.XmlData);
						}
						else
						{
							HtmlElement_BodyBase heb = FindHtmlElementByGuid(hei.ElementGuid);
							if (heb == null)
							{
								invalidehs.Add(ea.XmlData);
							}
						}
					}
				}
				if (invalidehs.Count > 0)
				{
					foreach (XmlNode nd in invalidehs)
					{
						XmlNode pn = nd.ParentNode;
						pn.RemoveChild(nd);
					}
				}
			}
		}
		public void AddUsedHtmlElement(HtmlElement_BodyBase element)
		{
			if (element.ElementGuid == Guid.Empty)
			{
				throw new DesignerException("Invalid call to AddUsedHtmlElement. Guid is empty");
			}
			SaveHtmlElement(element);
			ILimnorDesignPane dp = getDesigner("on use html element");
			if (dp != null)
			{
				dp.OnUseHtmlElement(element);
			}
		}
		public void UseHtmlElement(HtmlElement_BodyBase element, Form caller)
		{
			WebPage page = this.ObjectInstance as WebPage;
			if (page != null)
			{
				if (page.EditorStarted)
				{
					EnumElementValidation eev = element.Validate(caller);
					switch (eev)
					{
						case EnumElementValidation.Fail:
							MessageBox.Show(caller, "The operation failed.", "Use Html Element", MessageBoxButtons.OK, MessageBoxIcon.Error);
							break;
						case EnumElementValidation.New: //guid created
							this.OnUseHtmlElement(element);
							break;
						case EnumElementValidation.Pass: // existing guid
							break;
					}
				}
				else
				{
					MessageBox.Show(caller, "Please switch to the Visual Html Editor mode..", "Use Html Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}
		public void EditWebPage()
		{
			DialogHtml dlg = new DialogHtml();
			dlg.LoadWebPage("");
			if (dlg.ShowDialog() == DialogResult.OK)
			{
			}
		}
		public ILimnorDesignerLoader GetCurrentLoader()
		{
			ILimnorDesignPane pane = Project.GetTypedData<ILimnorDesignPane>(ClassId);
			if (pane != null)
			{
				return pane.Loader;
			}
			return null;
		}
		public void SetLoaded()
		{
			_loaded = true;
		}
		public void EnsureLoaded()
		{
			if (!_loaded)
			{
				_objectList.LoadObjects();
			}
		}
		public void Reset()
		{
			_actions = null;
			_eventActions = null;
			_libProperties = null;
			_propertyOverrides = null;
			_currentLevelPropertyList = null;
			_wholePropertyList = null;
			_customPropertyList = null;
			_libMethods = null;
			_methodOverrides = null;
			_currentLevelMethodList = null;
			_wholeMethodList = null;
			_customMethodList = null;
			_libEvents = null;
			_currentLevelEventList = null;
			_wholeEventList = null;
			_customEventList = null;
			_resourceMap = null;
			_editVersion++;
		}
		/// <summary>
		/// it may only be called after the class is loaded into designer
		/// </summary>
		public void ValidateInterfaceImplementations()
		{
			//fix missing interface members
			IList<InterfacePointer> ips = GetAddedInterfacesFromAllLevels();
			if (ips != null && ips.Count > 0)
			{
				foreach (InterfacePointer ip in ips)
				{
					List<InterfaceElementMethod> methods = ip.Methods;
					if (methods != null)
					{
						foreach (InterfaceElementMethod m in methods)
						{
							MethodClass mc = implementMethod(m);
							if (mc != null)
							{
								if (_currentLevelMethodList == null)
								{
									_currentLevelMethodList = new List<MethodClass>();
								}
								_currentLevelMethodList.Add(mc);
							}
						}
					}
					List<InterfaceElementProperty> properties = ip.GetProperties();
					if (properties != null)
					{
						foreach (InterfaceElementProperty p in properties)
						{
							PropertyClass pc = implementProperty(p);
							if (pc != null)
							{
								if (_currentLevelPropertyList == null)
								{
									_currentLevelPropertyList = new List<PropertyClass>();
								}
								_currentLevelPropertyList.Add(pc);
							}
						}
					}
					List<InterfaceElementEvent> events = ip.Events;
					if (events != null)
					{
						foreach (InterfaceElementEvent e in events)
						{
							EventClass ec = implementEvent(e);
							if (ec != null)
							{
								if (_currentLevelEventList == null)
								{
									_currentLevelEventList = new List<EventClass>();
								}
								_currentLevelEventList.Add(ec);
							}
						}
					}
				}
			}
		}
		public ClassPointer GetExternalExecuterClass(IAction act)
		{
			if (act == null)
			{
				return null;
			}
			SetterPointer sp = act.ActionMethod as SetterPointer;
			if (sp != null)
			{
				ClassPointer cp0 = sp.SetProperty.Declarer;
				if (cp0 != null)
				{
					if (cp0.ClassId != ClassId)
					{
						return cp0;
					}
				}
			}
			ActionAttachEvent aae = act as ActionAttachEvent;
			if (aae != null)
			{
				if (aae.ClassId != this.ClassId)
				{
					return aae.Class;
				}
				else
				{
					return null;
				}
			}
			if (act.ActionMethod == null)
			{
			}
			else
			{
				ClassPointer cp = act.ActionMethod.Owner as ClassPointer;
				if (cp != null)
				{
					if (act.ActionMethod.MethodDeclarer != null && act.ActionMethod.MethodDeclarer.RootPointer != null)
					{
						UInt32 eid = act.ActionMethod.MethodDeclarer.RootPointer.ClassId;// cp.RootPointer.ClassId;
						if (eid != ClassId)
						{
							cp = ClassPointer.CreateClassPointer(eid, Project);
						}
						else
						{
							cp = null;
						}
					}
					else
					{
						cp = null;
					}
				}
				else
				{
					ClassInstancePointer cip = act.ActionMethod.Owner as ClassInstancePointer;
					if (cip != null)
					{
						if (cip.MemberId == 0)
						{
							if (cip.DefinitionClassId != ClassId)
							{
								if (cip.Definition == null)
								{
									cip.ReplaceDeclaringClassPointer(ClassPointer.CreateClassPointer(cip.DefinitionClassId, this.Project));
								}
								cp = cip.Definition;
							}
						}
						else
						{
							if (cip.RootPointer != null)
							{
								if (cip.RootPointer.ClassId != ClassId)
								{
									cp = cip.RootPointer;
								}
							}
						}
					}
					else
					{
						MemberComponentId mcid = act.ActionMethod.Owner as MemberComponentId;
						if (mcid != null)
						{
							if (mcid.DefinitionClassId != 0 && mcid.DefinitionClassId != this.ClassId)
							{
								cp = mcid.RootPointer;
							}
						}
					}
				}
				return cp;
			}
			return null;
		}
		public string Report32Usage()
		{
			foreach (object v in _objectList.Keys)
			{
				IReport32Usage r = v as IReport32Usage;
				if (r != null)
				{
					string s = r.Report32Usage();
					if (!string.IsNullOrEmpty(s))
					{
						return s;
					}
				}
			}
			if (UsingCom)
			{
				return "ActiveX is used";
			}
			return string.Empty;
		}
		public Type GetMemberObjectType(UInt32 memberId)
		{
			XmlNode objNode = XmlData.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}[@{1}='{2}']",
				XmlTags.XML_Object, XmlTags.XMLATT_ComponentID, memberId));
			if (objNode == null)
			{
				objNode = XmlData.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"//{0}[@{1}='Controls']/{2}[@{3}='{4}']",
				XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_Item, XmlTags.XMLATT_ComponentID, memberId));
			}
			if (objNode != null)
			{
				string acid;
				return XmlUtil.GetLibTypeAttribute(objNode, out acid);
			}
			return null;
		}
		/// <summary>
		/// use ConnectionItem.GetConnectionFilename(Guid) to get full file
		/// </summary>
		/// <returns></returns>
		public IList<Guid> GetDatabaseConnectionsUsed()
		{
			List<Guid> lst = new List<Guid>();
			foreach (object obj in ObjectList.Keys)
			{
				IDatabaseConnectionUser da = obj as IDatabaseConnectionUser;
				if (da != null)
				{
					IList<Guid> l = da.DatabaseConnectionsUsed;
					if (l != null)
					{
						foreach (Guid g in l)
						{
							if (!lst.Contains(g))
							{
								lst.Add(g);
							}
						}
					}
				}
			}
			return lst;
		}
		public List<IClass> GetClassList()
		{
			ClassPointer root = ClassPointer.CreateClassPointer(ObjectList);
			List<IClass> list = new List<IClass>();
			list.Add(root);
			foreach (KeyValuePair<object, uint> kv in ObjectList)
			{
				if (kv.Value != ObjectList.MemberId) //not the root
				{
					MemberComponentId mc = MemberComponentId.CreateMemberComponentId(root, kv.Key, kv.Value);
					list.Add(mc);
				}
			}
			if (_usedHtmlElements != null)
			{
				foreach (HtmlElement_Base he in _usedHtmlElements)
				{
					list.Add(he);
				}
			}
			return list;
		}
		public IClass CreateMemberPointer(object obj)
		{
			ClassPointer cp = obj as ClassPointer;
			if (cp != null)
				return cp;
			if (obj == this.ObjectInstance)
				return this;
			IClassRef cr = _objectList.GetClassRefByObject(obj);
			if (cr != null)
			{
				ClassInstancePointer ci = cr as ClassInstancePointer;
				if (ci != null)
				{
					return ci;
				}
			}
			foreach (KeyValuePair<object, uint> kv in ObjectList)
			{
				if (obj == kv.Key)
				{
					MemberComponentId mc = MemberComponentId.CreateMemberComponentId(this, kv.Key, kv.Value);
					return mc;
				}
			}
			return null;
		}
		public ActionClass CreateFireEventAction(EventClass eventObject, XmlObjectWriter writer, MethodClass scopeMethod, Form caller)
		{
			FireEventMethod fe = new FireEventMethod(new CustomEventPointer(eventObject, this));
			ActionClass act = new ActionClass(this);
			act.ActionMethod = fe;
			act.ActionName = fe.DefaultActionName;
			act.ActionHolder = this;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			fe.Action = act;
			fe.ValidateParameterValues(act.ParameterValues);
			if (CreateNewAction(act, writer, scopeMethod, caller))
			{
				return act;
			}
			return null;
		}
		public ActionAttachEvent AddDynamicHandler(Type webClientEventHandlerType, IEvent e, Rectangle rc, Form caller)
		{
			ActionAttachEvent aae = new ActionAttachEvent(this);
			EventHandlerMethod ehm = AddEventhandlerMethod(webClientEventHandlerType, e, aae.ActionId, rc, caller);
			if (ehm != null)
			{
				aae.SetHandlerOwner(ehm.GetHandlerOwner());
				SaveAction(aae, null);
				return aae;
			}
			return null;
		}
		public EventHandlerMethod AddEventhandlerMethod(Type webClientEventHandlerType, IEvent e, UInt32 actionBranchId, Rectangle rc, Form caller)
		{
			ILimnorDesignPane pane = getDesigner("AddEventhandlerMethod");
			if (pane == null) return null;
			EventHandlerMethod eh;
			if (IsWebPage)
			{
				eh = (EventHandlerMethod)Activator.CreateInstance(webClientEventHandlerType, this, e);
			}
			else
			{
				eh = new EventHandlerMethod(this, e);
			}
			eh.Name = string.Format(CultureInfo.InvariantCulture, "on{0}{1}", e.Holder.Name, e.Name);
			eh.ActionBranchId = actionBranchId;
			if ((eh.Edit(actionBranchId, rc, pane.Loader, caller)))
			{
				_editVersion++;
				return eh;
			}
			return null;
		}
		public EventAction RemoveEventHandlers(IEvent e, List<TaskID> handlers)
		{
			EventAction ret = null;
			List<EventAction> eas = this.EventHandlers;
			if (eas != null)
			{
				foreach (EventAction ea in eas)
				{
					if (ea.Event.IsSameObjectRef(e))
					{
						ret = ea;
						foreach (TaskID tid in handlers)
						{
							ea.RemoveAction(tid.TaskId);
						}
						_editVersion++;
						break;
					}
				}
			}
			return ret;
		}
		public void RemoveEventHandlers(IEvent e)
		{
			List<EventAction> eas = this.EventHandlers;
			if (eas != null)
			{
				EventAction e0 = null;
				foreach (EventAction ea in eas)
				{
					if (e.IsSameObjectRef(ea.Event))
					{
						e0 = ea;
						break;
					}
				}
				if (e0 != null)
				{
					eas.Remove(e0);
					XmlNode ndHandlerList = XmlData.SelectSingleNode(XmlTags.XML_HANDLERLISTS);
					if (ndHandlerList != null)
					{
						XmlObjectReader xr = _objectList.Reader;
						XmlNodeList ndHandlers = ndHandlerList.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"{0}/{1}[@{2}='Event']",
							XmlTags.XML_HANDLER, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
						foreach (XmlNode nd in ndHandlers)
						{
							XmlNode en = nd.SelectSingleNode(XmlTags.XML_ObjProperty);
							if (en == null)
								continue;
							IEvent ev = (IEvent)xr.ReadObject(en, null);
							if (ev.IsSameObjectRef(e))
							{
								//remove local scope actions:
								// <Property name="TaskIDList"><Item type="HandlerMathodID"><Property name="HandlerMethod"><ObjProperty type="EventHandlerMethod"><Property name="MemberId">
								XmlNode taskList = nd.ParentNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
									"{0}[@{1}='TaskIDList']", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
								if (taskList != null)
								{
									XmlNodeList hidNodes = taskList.SelectNodes(string.Format(CultureInfo.InvariantCulture,
										"{0}[@{1}='HandlerMathodID']/{2}[@{3}='HandlerMethod']/{4}[@{1}='EventHandlerMethod']/{2}[@{3}='MemberId']",
										XmlTags.XML_Item, XmlTags.XMLATT_type, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_ObjProperty));
									foreach (XmlNode ndId in hidNodes)
									{
										XmlNode actNode = ndId.OwnerDocument.DocumentElement.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
											"{0}/{1}[@{2}='{3}']",
											XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ScopeId, ndId.InnerText));
										if (actNode != null)
										{
											actNode.ParentNode.RemoveChild(actNode);
										}
									}
								}
								//
								//remove <Handler type="EventAction">
								ndHandlerList.RemoveChild(nd.ParentNode);
								break;
							}
						}
					}
					_editVersion++;
				}
			}
		}
		public void ChangeIconFile(string file, ILimnorDesignerLoader loader, Form caller)
		{
			try
			{
				Bitmap bmp;
				if (string.Compare(System.IO.Path.GetExtension(file), ".ico", StringComparison.OrdinalIgnoreCase) == 0)
				{
					Icon ico = Icon.ExtractAssociatedIcon(file);
					Form f2 = ObjectInstance as Form;
					if (f2 != null)
					{
						f2.Icon = ico;
					}
					bmp = Bitmap.FromHicon(ico.Handle);
				}
				else
				{
					bmp = (Bitmap)System.Drawing.Bitmap.FromFile(file);
				}
				if (bmp != null)
				{
					if (loader != null)
					{
						string fname = System.IO.Path.GetFileName(file);
						string target = System.IO.Path.Combine(Project.ProjectFolder, fname);
						if (string.Compare(file, target, StringComparison.OrdinalIgnoreCase) != 0)
						{
							if (System.IO.File.Exists(target))
							{
								if (MessageBox.Show(caller,
									string.Format("File exists:{0}\r\nDo you want to overwrite it?", target),
									"Change Icon", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
								{
									System.IO.File.Copy(file, target, true);
								}
							}
							else
							{
								System.IO.File.Copy(file, target, false);
							}
						}
						XmlObjectWriter xw = loader.Writer;
						xw.WriteIcon(XmlData, fname);
					}
					_icon = bmp;
				}
			}
			catch (Exception err)
			{
				MessageBox.Show(caller, err.Message, "Change Icon", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		public void AddExternalType(Type t)
		{
			TypePointerCollection tpc = this.ObjectList.GetTypedData<TypePointerCollection>();
			if (tpc == null)
			{
				tpc = new TypePointerCollection();
				this.ObjectList.SetTypedData<TypePointerCollection>(tpc);
			}
			bool bExist = false;
			TypePointer tp = new TypePointer(t);
			foreach (TypePointer t0 in tpc)
			{
				if (t0.IsSameObjectRef(tp))
				{
					bExist = true;
					break;
				}
			}
			if (!bExist)
			{
				tpc.Add(tp);
				SerializeUtil.SetExternalType(XmlData, t);
			}
		}
		public void SetClassPointerReferences(ClassPointer pointer)
		{
			if (_objectList.ClassRefList != null)
			{
				foreach (IClassRef v in _objectList.ClassRefList)
				{
					ClassInstancePointer cip = v as ClassInstancePointer;
					if (cip != null)
					{
						if (cip.ClassId == pointer.ClassId)
						{
							cip.ReplaceDeclaringClassPointer(pointer);
						}
					}
				}
			}
		}
		public void SetUsePageNavigator()
		{
			_usePageNavigator = true;
		}
		public bool UsePageNavigator()
		{
			if (typeof(DrawingPage).IsAssignableFrom(this.BaseClassType))
			{
				Dictionary<UInt32, IAction> acts = GetActions();
				foreach (IAction a in acts.Values)
				{
					if (a != null && a.ActionMethod != null && a.ActionMethod.Owner == this)
					{
						if (string.Compare(a.ActionMethod.MethodName, "ShowPreviousForm", StringComparison.Ordinal) == 0
							|| string.Compare(a.ActionMethod.MethodName, "ShowNextForm", StringComparison.Ordinal) == 0)
						{
							return true;
						}
					}
				}
				foreach (KeyValuePair<object, uint> kv in _objectList)
				{
					DrawingPage dp = VPLUtil.GetObject(kv.Key) as DrawingPage;
					if (dp != null && dp != kv.Key)
					{
						XmlNode node = _objectList.XmlData.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"{0}[@{1}={2}]",
							XmlTags.XML_ClassRef, XmlTags.XMLATT_ComponentID, kv.Value));
						if (node != null)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		public static bool IsRelatedAction(UInt32 objectId, XmlNode actNode)
		{
			if (actNode != null)
			{
				if (objectId == XmlUtil.GetAttributeUInt(actNode, XmlTags.XMLATT_ComponentID))
				{
					return true;
				}
				XmlNode nd = actNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
							".//*[@{0}='{1}']",
							XmlTags.XMLATT_ComponentID, objectId));
				return (nd != null);
			}
			return false;
		}
		public List<IAction> GetRelatedActions(UInt32 objectId)
		{
			List<IAction> list = new List<IAction>();
			Dictionary<UInt32, IAction> acts = GetActions();
			foreach (IAction a in acts.Values)
			{
				if (a != null)
				{
					if (IsRelatedAction(objectId, a.CurrentXmlData))
					{
						list.Add(a);
					}
				}
			}
			return list;
		}
		public void ResetCustomPropertyCollection()
		{
			_customProperties = null;
		}
		public void SaveCustomPropertyValues(XmlObjectWriter writer)
		{
			PropertyDescriptorCollection pdc = CustomPropertyCollection;
			if (pdc.Count > 0)
			{
				XmlNode propNode = _objectList.XmlData.SelectSingleNode(XmlTags.XML_PropertyValues);
				if (propNode == null)
				{
					propNode = _objectList.XmlData.OwnerDocument.CreateElement(XmlTags.XML_PropertyValues);
					_objectList.XmlData.AppendChild(propNode);
				}
				else
				{
					propNode.RemoveAll();
				}
				writer.WriteProperties(propNode, pdc, this, XmlTags.XML_PROPERTY);
			}
		}
		public void LoadCustomPropertyValues(XmlObjectReader reader)
		{
			PropertyDescriptorCollection pdc = CustomPropertyCollection;
			if (pdc.Count > 0)
			{
				XmlNode propNode = _objectList.XmlData.SelectSingleNode(XmlTags.XML_PropertyValues);
				if (propNode != null)
				{
					reader.ReadProperties(propNode, this, pdc, XmlTags.XML_PROPERTY);
				}
			}
		}
		public void HookCustomPropertyValueChange(EventHandler h)
		{
			PropertyDescriptorCollection props = CustomPropertyCollection;
			foreach (PropertyDescriptor p in props)
			{
				PropertyClassDescriptor c = p as PropertyClassDescriptor;
				c.HookValueChange(h);
			}
		}
		/// <summary>
		/// this level or parent level property name changed.
		/// if this class overrides the changed property then we need to reset the name
		/// </summary>
		/// <param name="property"></param>
		public void OnCustomPropertyNameChanged(PropertyClass property)
		{
			UInt64 id = property.WholeId;
			if (_currentLevelPropertyList != null)
			{
				foreach (PropertyClass p in _currentLevelPropertyList)
				{
					PropertyOverride po = p as PropertyOverride;
					if (po != null)
					{
						if (po.BaseClassId == property.ClassId)
						{
							if (po.BasePropertyId == property.MemberId)
							{
								p.SetName(property.Name);//p is derived from the changed property 
								break;
							}
						}
					}
				}
			}
			if (_wholePropertyList != null)
			{
				foreach (PropertyClass p in _wholePropertyList)
				{
					if (p.WholeId == property.WholeId) //PropertyClassInherited will have the the same WholeId
					{
						p.SetName(property.Name);
						break;
					}
					PropertyOverride po = p as PropertyOverride;
					if (po != null)
					{
						if (po.BaseClassId == property.ClassId)
						{
							if (po.BasePropertyId == property.MemberId)
							{
								p.SetName(property.Name);//p is derived from the changed property 
								break;
							}
						}
					}
				}
			}
			if (_customPropertyList != null)
			{
				string oldName = "";
				foreach (KeyValuePair<string, PropertyClass> kv in _customPropertyList)
				{
					if (kv.Value.WholeId == id)
					{
						oldName = kv.Key;
						kv.Value.SetName(property.Name);
						break;
					}
					PropertyOverride po = kv.Value as PropertyOverride;
					if (po != null)
					{
						if (po.BaseClassId == property.ClassId)
						{
							if (po.BasePropertyId == property.MemberId)
							{
								oldName = kv.Key;
								po.SetName(property.Name);
								break;
							}
						}
					}
				}
				if (!string.IsNullOrEmpty(oldName)) //found existing name
				{
					_customPropertyList.Remove(oldName);
				}
				_customPropertyList.Add(property.Name, property);
			}
			_propertyOverrides = null;
			_editVersion++;
		}
		public void OnPropertyChanged(INonHostedObject property, string name)
		{
			PropertyClass pc = property as PropertyClass;
			if (pc != null)
			{
				if (_currentLevelPropertyList != null)
				{
					foreach (PropertyClass p in _currentLevelPropertyList)
					{
						PropertyOverride po = p as PropertyOverride;
						if (po != null)
						{
							if (po.BaseClassId == pc.ClassId)
							{
								if (po.BasePropertyId == pc.MemberId)
								{
									po.Update(pc);//p is derived from the changed property, pc
									break;
								}
							}
						}
					}
				}
				if (_wholePropertyList != null)
				{
					foreach (PropertyClass p in _wholePropertyList)
					{
						PropertyClassInherited pi = p as PropertyClassInherited;
						if (pi != null)
						{
							if (pi.WholeId == pc.WholeId) //PropertyClassInherited will have the the same WholeId
							{
								pi.Update(pc);
								break;
							}
						}
						PropertyOverride po = p as PropertyOverride;
						if (po != null)
						{
							if (po.BaseClassId == property.ClassId)
							{
								if (po.BasePropertyId == property.MemberId)
								{
									po.Update(pc);//p is derived from the changed property 
									break;
								}
							}
						}
					}
				}
			}
			_propertyOverrides = null;
			_customProperties = null;
			_editVersion++;
		}
		public void OnComponentRemoved(object obj)
		{
			UInt32 id = _objectList.GetObjectID(obj);
			if (id != 0)
			{
				//remove it from ComponentIconList of Methods
				//Methods/Method//Item[@objectID='id']
				XmlNodeList objNodes = XmlData.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}//{2}[@{3}='{4}']",
					XmlTags.XML_METHODS, XmlTags.XML_METHOD, XmlTags.XML_Item, XmlTags.XMLATT_ComponentID, id
					));
				if (objNodes != null && objNodes.Count > 0)
				{
					foreach (XmlNode nd in objNodes)
					{
						XmlNode pn = nd.ParentNode;
						pn.RemoveChild(nd);
					}
				}
				if (_currentLevelMethodList != null && _currentLevelMethodList.Count > 0)
				{
					foreach (MethodClass mc in _currentLevelMethodList)
					{
						mc.OnComponentRemoved(id);
					}
				}
				if (_methodOverrides != null && _methodOverrides.Count > 0)
				{
					foreach (MethodClass mc in _methodOverrides)
					{
						mc.OnComponentRemoved(id);
					}
				}
				if (_wholeMethodList != null && _wholeMethodList.Count > 0)
				{
					foreach (MethodClass mc in _wholeMethodList)
					{
						mc.OnComponentRemoved(id);
					}
				}
				if (_customMethodList != null && _customMethodList.Count > 0)
				{
					foreach (MethodClass mc in _customMethodList.Values)
					{
						mc.OnComponentRemoved(id);
					}
				}
				//remove it from EventHandlers
				//HandlerList/Handler/<Property name="Event">//<Property name="MemberId">{id}</Property>
				XmlNodeList ehNodes = XmlData.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}//{2}[@{3}='MemberId' and text() = '{4}']",
					XmlTags.XML_HANDLERLISTS, XmlTags.XML_HANDLER, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, id
					));
				if (ehNodes != null && ehNodes.Count > 0)
				{
					foreach (XmlNode nd in ehNodes)
					{
						XmlNode pn = nd.ParentNode;
						while (string.CompareOrdinal(pn.Name, XmlTags.XML_HANDLER) != 0)
						{
							pn = pn.ParentNode;
						}
						if (pn != null)
						{
							XmlNode pnp = pn.ParentNode;
							if (pnp != null)
							{
								pnp.RemoveChild(pn);
							}
						}
					}
				}
			}
			_editVersion++;
		}
		public string GetTypeName(string scopeNamespace)
		{
			if (string.IsNullOrEmpty(Namespace))
				return CodeName;
			if (string.CompareOrdinal(scopeNamespace, Namespace) != 0)
				return TypeString;
			return CodeName;
		}
		public override string ToString()
		{
			return CodeName;
		}
		public virtual UInt32 GetClassIdFromXmlNode(XmlNode node)
		{
			return DesignUtil.GetClassId(node);
		}
		[Browsable(false)]
		public virtual void SetData(ObjectIDmap objMap)
		{
			_objectList = objMap;
		}
		[Browsable(false)]
		public Dictionary<string, MethodClass> CustomMethods
		{
			get
			{
				if (_customMethodList == null)
				{
					XmlObjectReader xr = _objectList.Reader;
					Guid g = Guid.NewGuid();
					xr.ClearDelayedInitializers(g);
					//if this property is used before OnDesignPaneLoaded is called
					//then get custom properties without overriding the abstract base properties and without including un-overriden virtual properties
					//because overriding must be done in OnDesignPaneLoaded when the class is loaded for design
					List<MethodClass> lst = getCustomMethods();
					Dictionary<string, MethodClass> ms = new Dictionary<string, MethodClass>();
					foreach (MethodClass m in lst)
					{
						int n = 1;
						string sn = m.MethodName;
						string mk = m.GetMethodSignature();
						while (ms.ContainsKey(mk))
						{
							n++;
							m.MethodName = sn + n.ToString();
							mk = m.GetMethodSignature();
						}
						if (n > 1)
						{
							adjustMethodName(m);
						}
						ms.Add(mk, m);
					}
					_customMethodList = ms;
					xr.OnDelayedInitializeObjects(g);
				}
				return _customMethodList;
			}
		}


		public bool AskDeleteAction(IAction act, Form caller)
		{
			ILimnorDesignPane pane = getDesigner("AskDeleteAction");
			if (pane == null) return false;
			List<ObjectTextID> usages = GetActionUsage(act);
			if (usages.Count > 0)
			{
				dlgObjectUsage dlg = new dlgObjectUsage();
				dlg.LoadData("Cannot delete this action. It is being used by the following objects", string.Format("Action - {0}", act), usages);
				dlg.ShowDialog(caller);
			}
			else
			{
				if (MessageBox.Show(caller, "Do you want to delete this action?", "Delete Action", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					List<UInt32> aids = new List<uint>();
					aids.Add(act.ActionId);
					act.ActionHolder.DeleteActions(aids);
					//backward compatible
					if (_actions != null)
					{
						if (_actions.ContainsKey(act.ActionId))
						{
							_actions.Remove(act.ActionId);
						}
					}
					if (_eventActions != null)
					{
						foreach (EventAction ea in _eventActions)
						{
							ea.RemoveAction(act);
						}
					}
					DesignUtil.RemoveActionFromXmlNode(this.XmlData, this.ClassId, act.ActionId);
					pane.Loader.NotifyChanges();
					pane.OnActionDeleted(act);
					_editVersion++;
					return true;
				}
			}
			return false;
		}
		public void RemoveOverrideMethod(MethodOverride method)
		{
			MethodClassInherited baseMethod = GetBaseMethod(method.MethodSignature, method.BaseClassId, method.BaseMethodId);
			if (baseMethod == null)
			{
				throw new DesignerException("Cannot remove method override. Virtual method not found [{0}]", method.Name);
			}
			//
			XmlNode nodeMethod = method.XmlData;
			if (nodeMethod == null)
			{
				throw new DesignerException("Cannot remove method override. Method Node not found [{0},{1}], [{0}]", this.ClassId, method.MemberId, method.Name);
			}
			//
			XmlNode p = nodeMethod.ParentNode;
			p.RemoveChild(nodeMethod);
			//
			//remove actions in the method
			XmlNodeList nodes = SerializeUtil.GetMethodActionNodes(this.XmlData, method.MemberId);
			foreach (XmlNode nd in nodes)
			{
				XmlNode p0 = nd.ParentNode;
				p0.RemoveChild(nd);
			}
			ILimnorDesignPane pane = getDesigner("RemoveOverrideMethod");
			if (pane == null) return;
			pane.Loader.NotifyChanges();
			//update method lists
			updateCustomMethodListOnRemoveOverride(baseMethod);
			//do not need to notify other classes
			pane.OnDeleteMethod(method);
			_editVersion++;
		}
		public bool AskDeleteMethod(MethodClass method, Form caller)
		{
			List<ObjectTextID> usages = GetMethodUsages(method);
			if (usages.Count > 0)
			{
				dlgObjectUsage dlg = new dlgObjectUsage();
				dlg.LoadData("Cannot delete this method. It is being used by the following objects", string.Format("Method - {0}", method), usages);
				dlg.ShowDialog(caller);
				return false;
			}
			if (MessageBox.Show(caller, "Do you want to delete this method?", "Method", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
			{
				DeleteMethod(method);
				_editVersion++;
			}
			return true;
		}
		public void DeleteMethod(MethodClass method)
		{
			method.RemoveMethodXmlNode(XmlData);
			SerializeUtil.RemoveMethodActions(XmlData, method.MemberId);
			_methodOverrides = null;
			ILimnorDesignPane pane = getDesigner("DeleteMethod");
			if (pane == null) return;
			pane.Loader.ClearMethodNames();
			pane.Loader.NotifyChanges();
			//update lists=============
			MethodOverride mo = method as MethodOverride;
			if (mo != null)
			{
				MethodClassInherited baseMethod = GetBaseMethod(mo.MethodSignature, mo.BaseClassId, mo.BaseMethodId);
				if (baseMethod == null)
				{
					throw new DesignerException("Cannot remove method override. Base method not found [{0},{1},{2}]", mo.MethodSignature, mo.BaseClassId, mo.BaseMethodId);
				}
				if (_customMethodList != null)
				{
					_customMethodList.Remove(mo.MethodSignature);
					_customMethodList.Add(baseMethod.MethodSignature, baseMethod);
				}
				if (_wholeMethodList != null)
				{
					MethodClass m0 = null;
					foreach (MethodClass mx in _wholeMethodList)
					{
						if (mx.WholeId == method.WholeId)
						{
							m0 = mx;
							break;
						}
					}
					if (m0 != null)
					{
						_wholeMethodList.Remove(m0);
					}
					_wholeMethodList.Add(baseMethod);
				}
			}
			else
			{
				if (_customMethodList != null)
				{
					_customMethodList.Remove(method.MethodSignature);
				}
				if (_wholeMethodList != null)
				{
					MethodClass m0 = null;
					foreach (MethodClass mx in _wholeMethodList)
					{
						if (mx.WholeId == method.WholeId)
						{
							m0 = mx;
							break;
						}
					}
					if (m0 != null)
					{
						_wholeMethodList.Remove(m0);
					}
				}
			}
			if (_currentLevelMethodList != null)
			{
				MethodClass m0 = null;
				foreach (MethodClass mx in _currentLevelMethodList)
				{
					if (mx.WholeId == method.WholeId)
					{
						m0 = mx;
						break;
					}
				}
				if (m0 != null)
				{
					_currentLevelMethodList.Remove(m0);
				}
			}
			//
			EventHandlerMethod ehm = method as EventHandlerMethod;
			if (ehm != null)
			{
				if (_eventActions != null)
				{
					foreach (EventAction ea in _eventActions)
					{
						List<TaskID> toDels = new List<TaskID>();
						TaskIdList idList = ea.TaskIDList;
						for (int i = 0; i < idList.Count; i++)
						{
							TaskID taskId = idList[i];
							HandlerMethodID hmd = taskId as HandlerMethodID;
							if (hmd != null)
							{
								if (hmd.HandlerMethod != null && hmd.HandlerMethod.MethodID == ehm.MethodID)
								{
									toDels.Add(taskId);
								}
							}
						}
						if (toDels.Count > 0)
						{
							foreach (TaskID taskId in toDels)
							{
								idList.Remove(taskId);
							}
						}
					}
				}
			}
			//=========================
			pane.OnDeleteMethod(method);
			//notify all classes
			Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
			foreach (ILimnorDesignPane pn in designPanes.Values)
			{
				if (pn.Loader.ClassID != method.ClassId)
				{
					pn.OnDeleteMethod(method);
				}
			}
			ResetDefaultInstance();
			_editVersion++;
		}
		public bool AskDeleteProperty(PropertyClass property, Form caller)
		{
			List<ObjectTextID> usages = DesignUtil.GetPropertyUsage(Project, property);
			List<ObjectTextID> usages2 = GetPropertyUsages(property);
			usages.AddRange(usages2);
			if (usages.Count > 0)
			{
				dlgObjectUsage dlg = new dlgObjectUsage();
				dlg.LoadData("Cannot delete this property. It is being used by the following objects", string.Format("Property - {0}", property.Name), usages);
				dlg.ShowDialog(caller);
			}
			else
			{
				if (MessageBox.Show(caller, "Do you want to delete this property?", "Property", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					DeleteProperty(property);
					_editVersion++;
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// delete a custom property or remove an override
		/// </summary>
		/// <param name="property"></param>
		public void DeleteProperty(PropertyClass property)
		{
			//
			XmlNode nodeProp = SerializeUtil.GetCustomPropertyNode(this.XmlData, property.MemberId);
			if (nodeProp == null)
			{
				throw new DesignerException("Cannot remove property. Property Node not found [{0},{1}], [{0}]", this.ClassId, property.MemberId, property.Name);
			}
			//
			XmlNode p = nodeProp.ParentNode;
			p.RemoveChild(nodeProp);
			//
			//remove actions in the getter
			UInt32 memberId;
			if (property.Getter != null)
			{
				memberId = property.Getter.MemberId;
				SerializeUtil.RemoveMethodActions(this.XmlData, memberId);
			}
			//remove actions in the setter
			if (property.Setter != null)
			{
				memberId = property.Setter.MemberId;
				SerializeUtil.RemoveMethodActions(this.XmlData, memberId);
			}
			//no need to remove other actions using the property because the base version is still valid for those usages
			//
			PropertyOverride prop = property as PropertyOverride;
			if (prop != null)
			{
				PropertyClassInherited baseProperty = GetBaseProperty(prop.Name, prop.BaseClassId, prop.BasePropertyId);
				if (baseProperty == null)
				{
					throw new DesignerException("Cannot remove property override. Virtual property not found [{0}]", property.Name);
				}
				if (_customPropertyList != null)
				{
					_customPropertyList.Remove(property.Name);
					_customPropertyList.Add(property.Name, baseProperty);
				}
				if (_wholePropertyList != null)
				{
					PropertyClass p0 = null;
					foreach (PropertyClass px in _wholePropertyList)
					{
						if (px.WholeId == property.WholeId)
						{
							p0 = px;
							break;
						}
					}
					if (p0 != null)
					{
						_wholePropertyList.Remove(p0);
					}
					_wholePropertyList.Add(baseProperty);
				}
			}
			else
			{
				if (_customPropertyList != null)
				{
					_customPropertyList.Remove(property.Name);
				}
				if (_wholePropertyList != null)
				{
					PropertyClass p0 = null;
					foreach (PropertyClass px in _wholePropertyList)
					{
						if (px.WholeId == property.WholeId)
						{
							p0 = px;
							break;
						}
					}
					if (p0 != null)
					{
						_wholePropertyList.Remove(p0);
					}
				}
			}
			if (_currentLevelPropertyList != null)
			{
				PropertyClass p0 = null;
				foreach (PropertyClass px in _currentLevelPropertyList)
				{
					if (px.WholeId == property.WholeId)
					{
						p0 = px;
						break;
					}
				}
				if (p0 != null)
				{
					_currentLevelPropertyList.Remove(p0);
				}
			}
			_propertyOverrides = null;
			_customProperties = null;
			ILimnorDesignPane pane = getDesigner("DeleteProperty");
			if (pane == null) return;
			pane.Loader.NotifyChanges();
			pane.OnDeleteProperty(property);
			Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
			foreach (ILimnorDesignPane pn in designPanes.Values)
			{
				if (pn.Loader.ClassID != this.ClassId)
				{
					pn.OnDeleteProperty(property);
				}
			}
			ResetDefaultInstance();
			_editVersion++;
		}
		public PropertyOverride CreateOverrideProperty(PropertyClassInherited baseProperty)
		{
			PropertyOverride prop = new PropertyOverride(this);
			prop.CopyFromInherited(baseProperty);
			prop.MemberId = (UInt32)(Guid.NewGuid().GetHashCode());
			saveNewProperty(prop);
			return prop;
		}
		public void SaveMethod(MethodClass method, ExceptionHandler exceptionHandler)
		{
			bool isNew = false;
			XmlNode nodeMethod = method.XmlData;
			if (nodeMethod == null)
			{
				XmlNode nodeMethodCollection = _objectList.XmlData.SelectSingleNode(XmlTags.XML_METHODS);
				if (nodeMethodCollection == null)
				{
					nodeMethodCollection = _objectList.XmlData.OwnerDocument.CreateElement(XmlTags.XML_METHODS);
					_objectList.XmlData.AppendChild(nodeMethodCollection);
				}
				nodeMethod = nodeMethodCollection.SelectSingleNode(
					string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}[@{1}='{2}']",
					XmlTags.XML_METHOD, XmlTags.XMLATT_MethodID, method.MemberId));
				if (nodeMethod == null)
				{
					if (method.XmlData != null)
					{
						nodeMethod = method.XmlData;
					}
					else
					{
						nodeMethod = nodeMethodCollection.OwnerDocument.CreateElement(XmlTags.XML_METHOD);
					}
					nodeMethodCollection.AppendChild(nodeMethod);
					isNew = true;
				}
			}
			else
			{
				if (nodeMethod.ParentNode == null)
				{
					XmlNode nodeMethodCollection = _objectList.XmlData.SelectSingleNode(XmlTags.XML_METHODS);
					if (nodeMethodCollection == null)
					{
						nodeMethodCollection = _objectList.XmlData.OwnerDocument.CreateElement(XmlTags.XML_METHODS);
						_objectList.XmlData.AppendChild(nodeMethodCollection);
					}
					nodeMethodCollection.AppendChild(nodeMethod);
					isNew = true;
				}
			}
			bool bSaveAll = true;
			if (!isNew)
			{
				if (exceptionHandler == null)
				{
				}
				else
				{
					bSaveAll = false;
				}
			}
			if (bSaveAll)
			{
				XmlUtil.SetAttribute(nodeMethod, XmlTags.XMLATT_MethodID, method.MemberId);
				XmlUtil.SetAttribute(nodeMethod, XmlTags.XMLATT_NAME, method.Name);
			}
			XmlObjectWriter wr = new XmlObjectWriter(this._objectList);// pane.Loader.Writer;
			if (bSaveAll)
			{
				wr.WriteObjectToNode(nodeMethod, method);
				List<ConstObjectPointer> coLst = method.CustomAttributeList;
				if (coLst != null && coLst.Count > 0)
				{
					foreach (ConstObjectPointer attr in coLst)
					{
						XmlNode node = SerializeUtil.CreateAttributeNode(nodeMethod, attr.ValueId);
						wr.WriteObjectToNode(node, attr);
					}
				}
			}
			else
			{
				//save excption handler
				wr.WriteObject(exceptionHandler.DataXmlNode, exceptionHandler, null);
			}
			if (wr.HasErrors)
			{
				MathNode.Log(wr.ErrorCollection);
			}
			ILimnorDesignerLoader loader = GetDesignerLoader();
			if (loader != null)
			{
				loader.DesignPane.OnNotifyChanges();
				if (bSaveAll)
				{
					OnMethodSaved(method, isNew);
				}
			}
		}
		public void OnMethodSaved(MethodClass method, bool isNew)
		{
			ILimnorDesignPane pane = getDesigner("SaveMethod");
			if (pane == null) return;
			updateCustomMethodList(method);
			pane.OnMethodChanged(method, isNew);
			Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
			foreach (ILimnorDesignPane pn in designPanes.Values)
			{
				if (pn.Loader.ClassID != this.ClassId)
				{
					pn.OnMethodChanged(method, isNew);
				}
			}
		}
		public MethodOverride ImplementOverrideMethod(MethodClassInherited baseMethod)
		{
			ILimnorDesignPane pane = getDesigner("ImplementOverrideMethod");
			if (pane == null) return null;
			MethodOverride mo = new MethodOverride(this);
			mo.CopyFromInherited(baseMethod);
			mo.MemberId = (UInt32)(Guid.NewGuid().GetHashCode());
			mo.IsNewMethod = true;
			if (!baseMethod.IsAbstract)
			{
				//add calling base version
				ActionClass act = new ActionClass(this);
				act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
				act.ScopeMethodId = mo.MemberId;
				act.ScopeMethod = mo;
				act.ActionMethod = baseMethod.CreateBaseMethod();
				act.ActionName = act.ActionMethod.DefaultActionName;
				act.ActionHolder = mo;
				mo.IsNewMethod = true;
				//
				BranchList bl = new BranchList(mo);
				mo.ActionList = bl;
				//
				AB_SingleAction ab = new AB_SingleAction(mo);
				ab.ActionId = new TaskID(act.WholeActionId);
				ab.ActionData = act;
				ab.Location = new Point(100, 100);
				ab.Size = new Size(230, 30);//(32, 32);
				ActionPortIn api = new ActionPortIn(ab);
				api.Location = new Point(ab.Location.X + ab.Size.Width / 2 - ActionPortIn.dotSize / 2, ab.Location.Y - ActionPortIn.dotSize / 2);
				api.Position = ab.Size.Width / 2 - ActionPortIn.dotSize / 2;
				ab.InPortList = new List<ActionPortIn>();
				ab.InPortList.Add(api);
				ActionPortOut apo = new ActionPortOut(ab);
				apo.Location = new Point(ab.Location.X + ab.Size.Width / 2 - ActionPortIn.dotSize / 2, ab.Location.Y + ab.Size.Height);
				apo.Position = ab.Size.Width / 2 - ActionPortIn.dotSize / 2;
				ab.OutPortList = new List<ActionPortOut>();
				ab.OutPortList.Add(apo);
				bl.Add(ab);
				//
				SaveAction(act, pane.Loader.Writer);
			}
			XmlNode node = SerializeUtil.CreateCustomMethodNode(this.XmlData, mo.MemberId);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_MethodID, mo.MemberId);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_NAME, mo.Name);
			pane.Loader.Writer.WriteObjectToNode(node, mo);
			if (pane.Loader.Writer.HasErrors)
			{
				MathNode.Log(pane.Loader.Writer.ErrorCollection);
			}
			//
			pane.Loader.NotifyChanges();
			//
			updateCustomMethodListOnOverride(mo);
			//only notify viewers of this class because
			//overriding a method does not affect other classes
			pane.OnMethodChanged(mo, true);
			_editVersion++;
			return mo;
		}
		/// <summary>
		/// called when a new method is added or modified
		/// </summary>
		/// <param name="m"></param>
		private void updateCustomMethodList(MethodClass m)
		{
			if (_customMethodList != null)
			{
				foreach (KeyValuePair<string, MethodClass> mx in _customMethodList)
				{
					if (mx.Value.WholeId == m.WholeId)
					{
						_customMethodList.Remove(mx.Key);
						break;
					}
				}
				_customMethodList.Add(m.GetMethodSignature(), m);
			}
			if (_wholeMethodList != null)
			{
				foreach (MethodClass mx in _wholeMethodList)
				{
					if (mx.WholeId == m.WholeId)
					{
						_wholeMethodList.Remove(mx);
						break;
					}
				}
				_wholeMethodList.Add(m);
			}
			if (_currentLevelMethodList != null)
			{
				foreach (MethodClass mx in _currentLevelMethodList)
				{
					if (mx.WholeId == m.WholeId)
					{
						_currentLevelMethodList.Remove(mx);
						break;
					}
				}
				_currentLevelMethodList.Add(m);
			}
			_methodOverrides = null;
		}
		/// <summary>
		/// called after overriding the method
		/// </summary>
		/// <param name="m"></param>
		private void updateCustomMethodListOnOverride(MethodOverride mo)
		{
			if (_customMethodList != null)
			{
				string sg = mo.GetMethodSignature();
				if (_customMethodList.ContainsKey(sg))
				{
					_customMethodList.Remove(sg);
				}
				else
				{
					foreach (KeyValuePair<string, MethodClass> mx in _customMethodList)
					{
						if (mx.Value.ClassId == mo.BaseClassId && mx.Value.MemberId == mo.BaseMethodId)
						{
							_customMethodList.Remove(mx.Key);
							break;
						}
					}
				}
				_customMethodList.Add(sg, mo);
			}
			if (_wholeMethodList != null)
			{
				foreach (MethodClass mx in _wholeMethodList)
				{
					if (mx.ClassId == mo.BaseClassId && mx.MemberId == mo.BaseMethodId)
					{
						_wholeMethodList.Remove(mx);
						break;
					}
				}
				_wholeMethodList.Add(mo);
			}
			if (_currentLevelMethodList != null)
			{
				_currentLevelMethodList.Add(mo);
			}
			_methodOverrides = null;
		}
		/// <summary>
		/// called after removing the override
		/// </summary>
		/// <param name="m"></param>
		private void updateCustomMethodListOnRemoveOverride(MethodClassInherited m)
		{
			if (_customMethodList != null)
			{
				foreach (KeyValuePair<string, MethodClass> mx in _customMethodList)
				{
					MethodOverride mo = mx.Value as MethodOverride;
					if (mo != null)
					{
						if (mo.BaseMethodId == m.MemberId && mo.BaseClassId == m.ClassId)
						{
							_customMethodList.Remove(mx.Key);
							break;
						}
					}
				}
				_customMethodList.Add(m.GetMethodSignature(), m);
			}
			if (_wholeMethodList != null)
			{
				foreach (MethodClass mx in _wholeMethodList)
				{
					MethodOverride mo = mx as MethodOverride;
					if (mo != null)
					{
						if (mo.BaseMethodId == m.MemberId && mo.BaseClassId == m.ClassId)
						{
							_wholeMethodList.Remove(mx);
							break;
						}
					}
				}
				_wholeMethodList.Add(m);
			}
			if (_currentLevelMethodList != null)
			{
				foreach (MethodClass mx in _currentLevelMethodList)
				{
					MethodOverride mo = mx as MethodOverride;
					if (mo != null)
					{
						if (mo.BaseMethodId == m.MemberId && mo.BaseClassId == m.ClassId)
						{
							_currentLevelMethodList.Remove(mx);
							break;
						}
					}
				}
			}
			_methodOverrides = null;
		}
		public PropertyClass CreateNewProperty(DataTypePointer type, bool isOverride, string name, bool hasBaseImplementation, bool canRead, bool canWrite, bool isStatic, EnumWebRunAt runAt)
		{
			PropertyClass prop;
			if (isOverride)
			{
				PropertyClassInherited pb = GetBaseProperty(name, 0, 0);
				PropertyOverride prop0 = new PropertyOverride(this);
				prop0.CopyFromInherited(pb);
				prop = prop0;
			}
			else
			{
				InterfaceElementProperty ip = type as InterfaceElementProperty;
				if (ip != null)
				{
					InterfaceCustomProperty ifp = new InterfaceCustomProperty(this);
					ifp.Interface = ip.Interface;
					prop = ifp;
					prop.CanWrite = ip.CanWrite;
					prop.CanRead = ip.CanRead;
				}
				else
				{
					if (runAt == EnumWebRunAt.Server)
					{
						prop = new PropertyClassWebServer(this);
					}
					else if (runAt == EnumWebRunAt.Client)
					{
						prop = new PropertyClassWebClient(this);
					}
					else
					{
						prop = new PropertyClass(this);
					}
					prop.IsStatic = this.IsStatic;
					prop.CanWrite = canWrite;
					prop.CanRead = canRead;
				}
				if (this.IsStatic)
				{
					prop.IsStatic = true;
				}
				else
				{
					prop.IsStatic = isStatic;
				}
				prop.Name = name;
				if (type.ClassTypePointer != null)
				{
					prop.PropertyType = new DataTypePointer(type.ClassTypePointer);
				}
				else
				{
					prop.PropertyType = new DataTypePointer(type.LibTypePointer);
				}

				prop.MemberId = (UInt32)Guid.NewGuid().GetHashCode();
			}
			saveNewProperty(prop);
			//
			_editVersion++;
			return prop;
		}
		public MethodClass MakeMethodCopy(MethodClass method, Form caller)
		{
			XmlNode methodNode = method.XmlData;
			if (methodNode == null)
			{
				methodNode = this.XmlData.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
					"{0}/{1}[{2}='{3}']",
					XmlTags.XML_METHODS, XmlTags.XML_METHOD, XmlTags.XMLATT_MethodID, method.MethodID));
			}
			if (methodNode == null)
			{
				MessageBox.Show(caller, "Method xml data not found", "Duplicate method", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				XmlNode newMethodNode = methodNode.CloneNode(true);
				ILimnorDesignPane pane = getDesigner("CreateNewMethod");
				if (pane == null) return null;
				string name = pane.Loader.CreateMethodName(method.Name, null);
				XmlUtil.SetNameAttribute(newMethodNode, name);
				//adjust id
				UInt32 oldMethodId = XmlUtil.GetAttributeUInt(methodNode, XmlTags.XMLATT_MethodID);
				UInt32 newMethodId = (UInt32)(Guid.NewGuid().GetHashCode());
				XmlUtil.SetAttribute(newMethodNode, XmlTags.XMLATT_MethodID, newMethodId);
				XmlNode propNode = newMethodNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
					"{0}[@name='MemberId']",
					XmlTags.XML_PROPERTY));
				if (propNode != null)
				{
					propNode.InnerText = newMethodId.ToString(CultureInfo.InvariantCulture);
				}
				propNode = newMethodNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
					"{0}[@name='Name']",
					XmlTags.XML_PROPERTY));
				if (propNode != null)
				{
					propNode.InnerText = name;
				}
				//
				method.LoadActionInstances();
				List<IAction> acts = method.GetActions();
				if (acts != null && acts.Count > 0)
				{
					XmlNode methodActListNode = newMethodNode.SelectSingleNode(XmlTags.XML_ACTIONS);
					if (methodActListNode == null)
					{
						methodActListNode = newMethodNode.OwnerDocument.CreateElement(XmlTags.XML_ACTIONS);
						newMethodNode.AppendChild(methodActListNode);
					}
					XmlNode rootActListNode = newMethodNode.OwnerDocument.DocumentElement.SelectSingleNode(XmlTags.XML_ACTIONS);
					foreach (IAction act in acts)
					{
						XmlNode actNode = methodActListNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
							"{0}[@{1}='{2}']",
							XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, act.ActionId));
						if (actNode == null && rootActListNode != null)
						{
							XmlNode actNode0 = rootActListNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
							"{0}[@{1}='{2}']",
							XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, act.ActionId));
							if (actNode0 != null)
							{
								actNode = actNode0.CloneNode(true);
								methodActListNode.AppendChild(actNode);
								XmlUtil.SetAttribute(actNode, XmlTags.XMLATT_ScopeId, newMethodId);
							}
						}
						if (actNode != null)
						{
							UInt32 oldActId = act.ActionId;
							UInt32 newActId = (UInt32)(Guid.NewGuid().GetHashCode());
							act.ActionId = newActId;
							XmlNodeList nds = newMethodNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
							  ".//*[@{0}='{1}']",
							  XmlTags.XMLATT_ActionID, oldActId));
							if (nds != null && nds.Count > 0)
							{
								foreach (XmlNode nd in nds)
								{
									XmlUtil.SetAttribute(nd, XmlTags.XMLATT_ActionID, newActId);
								}
							}
							nds = newMethodNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
							   ".//{0}[@name='ActionId']",
							   XmlTags.XML_PROPERTY));
							foreach (XmlNode nd in nds)
							{
								UInt32 id0 = 0;
								if (UInt32.TryParse(nd.InnerText, out id0))
								{
									if (id0 == oldActId)
									{
										nd.InnerText = newActId.ToString(CultureInfo.InvariantCulture);
									}
								}
							}
						}
					}
				}
				XmlNodeList nodes = newMethodNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					".//*[@{0}='{1}']",
					XmlTags.XMLATT_ScopeId, oldMethodId));
				foreach (XmlNode nd in nodes)
				{
					XmlUtil.SetAttribute(nd, XmlTags.XMLATT_ScopeId, newMethodId);
				}
				nodes = newMethodNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					".//*[@{0}='{1}']",
					XmlTags.XMLATT_handlerId, oldMethodId));
				foreach (XmlNode nd in nodes)
				{
					XmlUtil.SetAttribute(nd, XmlTags.XMLATT_handlerId, newMethodId);
				}
				nodes = newMethodNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					".//*[@{0}='{1}']",
					XmlTags.XMLATT_MethodID, oldMethodId));
				foreach (XmlNode nd in nodes)
				{
					XmlUtil.SetAttribute(nd, XmlTags.XMLATT_MethodID, newMethodId);
				}
				//
				methodNode.ParentNode.AppendChild(newMethodNode);
				//
				pane.Loader.NotifyChanges();
				//
				MethodClass newMethod = new MethodClass(this);
				newMethod.MemberId = newMethodId;
				newMethod.SetReader(this.ObjectList, newMethodNode, pane.Loader.Reader);
				Guid g = Guid.NewGuid();
				pane.Loader.Reader.ClearDelayedInitializers(g);
				newMethod.ReloadFromXmlNode();
				pane.Loader.Reader.OnDelayedInitializeObjects(g);
				//
				OnMethodSaved(newMethod, true);
				//
				return newMethod;
			}
			return null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="forStatic"></param>
		/// <param name="forWebService">True: create a web service method</param>
		/// <param name="rcStart"></param>
		/// <param name="caller"></param>
		/// <returns></returns>
		public MethodClass CreateNewMethod(bool forStatic, EnumMethodStyle style, EnumMethodWebUsage webUsage, Rectangle rcStart, Form caller)
		{
			ILimnorDesignPane pane = getDesigner("CreateNewMethod");
			if (pane == null) return null;
			MethodClass method = new MethodClass(this);
			method.WebUsage = webUsage;
			method.IsStatic = forStatic;

			if (method.Edit(0, rcStart, pane.Loader, caller))
			{
				if (style != EnumMethodStyle.Normal)
				{
					ConstObjectPointer cop;
					ConstructorInfo cif;
					if (style == EnumMethodStyle.WebService)
					{
						cop = new ConstObjectPointer(new TypePointer(typeof(WebMethodAttribute)));
						cif = typeof(WebMethodAttribute).GetConstructor(Type.EmptyTypes);
					}
					else
					{
						cop = new ConstObjectPointer(new TypePointer(typeof(OperationContractAttribute)));
						cif = typeof(OperationContractAttribute).GetConstructor(Type.EmptyTypes);
					}
					cop.SetValue(ConstObjectPointer.VALUE_Constructor, cif);
					method.AddAttribute(cop);
					pane.Loader.NotifyChanges();
				}
				FrmObjectExplorer.RemoveDialogCaches(this.Project.ProjectGuid, this.ClassId);
				_editVersion++;
				//
				return method;
			}
			return null;
		}
		public MethodClass CreateNewMethodFixParams(EnumMethodWebUsage webUsage, Type[] paramTypes, string[] paramNames, Rectangle rcStart, Form caller)
		{
			ILimnorDesignPane pane = getDesigner("CreateNewMethodFixParams");
			if (pane == null) return null;
			MethodClass method = new MethodClass(this);
			method.WebUsage = webUsage;
			method.IsStatic = false;
			if (paramTypes != null && paramTypes.Length > 0)
			{
				List<ParameterClass> ps = new List<ParameterClass>();
				for (int i = 0; i < paramTypes.Length; i++)
				{
					ps.Add(new ParameterClass(paramTypes[i], paramNames[i], method));
				}
				method.Parameters = ps;
			}
			method.ParameterEditType = EnumParameterEditType.ReadOnly;
			if (method.Edit(0, rcStart, pane.Loader, caller))
			{
				_editVersion++;
				//
				return method;
			}
			return null;
		}
		public EventClass CreateNewEvent(DataTypePointer handlerType, string name, bool isStatic)//, ILimnorDesignerLoader loader, XmlObjectWriter writer)
		{
			EventClass ev = new EventClass(this);
			ev.IsStatic = isStatic;
			ev.MemberId = (UInt32)Guid.NewGuid().GetHashCode();
			ev.Owner = this;
			ev.Name = name;
			InterfaceElementEvent ee = handlerType as InterfaceElementEvent;
			if (ee != null)
			{
				if (ee.IsLibType)
				{
					ev.EventHandlerType = new NamedDataType(ee.LibTypePointer, ee.Name);
				}
				else
				{
					ev.EventHandlerType = new NamedDataType(ee.ClassTypePointer);
				}
			}
			else
			{
				ev.EventHandlerType = handlerType;
			}
			//
			XmlNode node = SerializeUtil.CreateCustomEventNode(XmlData, ev.MemberId, ev.Name);
			// 
			XmlObjectWriter writer = new XmlObjectWriter(this._objectList);
			writer.WriteObjectToNode(node, ev);
			//
			ILimnorDesignerLoader loader = GetDesignerLoader();
			if (loader != null)
			{
				loader.NotifyChanges();
				//
				_customEventList = null;
				_wholeEventList = null;
				_currentLevelEventList = null;
				//
				if (loader.DesignPane != null)
				{
					loader.DesignPane.OnAddEvent(ev);
				}
				Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
				foreach (ILimnorDesignPane pn in designPanes.Values)
				{
					if (pn.Loader.ClassID != this.ClassId)
					{
						pn.OnAddEvent(ev);
					}
				}
				_editVersion++;
			}
			FrmObjectExplorer.RemoveDialogCaches(this.Project.ProjectGuid, this.ClassId);
			return ev;
		}
		public bool AskDeleteEvent(EventClass eventObject, Form caller)
		{
			//check event usage
			List<ObjectTextID> usages = new List<ObjectTextID>();
			Dictionary<UInt32, IAction> actions = GetActions();
			foreach (IAction act in actions.Values)
			{
				if (act != null)
				{
					FireEventMethod fe = act.ActionMethod as FireEventMethod;
					if (fe != null)
					{
						if (fe.IsSameEvent(eventObject))
						{
							usages.Add(new ObjectTextID("FireEventMethod", "Action", act.ActionName));
						}
					}
				}
			}
			EventAction ea = GetEventHandler(eventObject);
			if (ea != null)
			{
				TaskIdList idList = ea.TaskIDList;
				foreach (TaskID taskId in idList)
				{
					usages.Add(new ObjectTextID("Event Action", "Action", taskId.ToString()));
				}
			}
			List<ObjectTextID> usages2 = GetEventUsages(eventObject);
			usages.AddRange(usages2);
			if (usages.Count > 0)
			{
				dlgObjectUsage dlg = new dlgObjectUsage();
				dlg.LoadData("Cannot delete this event. It is being used by the following objects", string.Format("Event - {0}", eventObject.Name), usages);
				dlg.ShowDialog(caller);
			}
			else
			{
				if (MessageBox.Show(caller, "Do you want to delete this event?", "Event", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					if (DeleteEvent(eventObject))
					{
						_editVersion++;
						return true;
					}
				}
			}
			return false;
		}
		public bool DeleteEvent(EventClass eventObject)
		{
			try
			{
				if (eventObject == null)
				{
					throw new DesignerException("Calling DeleteEvent with null eventObject object");
				}
				ILimnorDesignPane pane = getDesigner("DeleteEvent");
				if (pane == null) return false;
				XmlNode nodeEvent = SerializeUtil.GetCustomEventNode(pane.Loader.Node, eventObject.MemberId);
				if (nodeEvent != null)
				{
					XmlNode p = nodeEvent.ParentNode;
					p.RemoveChild(nodeEvent);
					//
					_customEventList = null;
					_wholeEventList = null;
					_currentLevelEventList = null;
					//
					pane.OnDeleteEvent(eventObject);
					pane.OnNotifyChanges();
					Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
					foreach (ILimnorDesignPane pn in designPanes.Values)
					{
						if (pn.Loader.ClassID != eventObject.ClassId)
						{
							pn.OnDeleteEvent(eventObject);
						}
					}
					_editVersion++;
					ResetDefaultInstance();
					return true;
				}
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm, err);
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		public void OnDesignPaneLoaded()
		{
			#region properties
			List<PropertyClass> lst = getCustomProperties();
			Dictionary<string, PropertyClass> props = new Dictionary<string, PropertyClass>();
			foreach (PropertyClass p in lst)
			{
				props.Add(p.Name, p);
			}
			Type t = this.BaseClassType;
			PropertyInfo[] pifs = t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (pifs != null && pifs.Length > 0)
			{
				for (int i = 0; i < pifs.Length; i++)
				{
					if (!pifs[i].IsSpecialName)
					{
						if (TypeUtil.GetAccessControl(pifs[i]) == EnumAccessControl.Private)
						{
							continue;
						}
						if (props.ContainsKey(pifs[i].Name))
						{
							continue;
						}
						bool isVirtual = false;
						bool isAbstract = false;
						if (pifs[i].CanRead)
						{
							MethodInfo mif = pifs[i].GetGetMethod(true);
							if ((mif.Attributes & MethodAttributes.Final) == MethodAttributes.Final)
							{
								continue;
							}
							if ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
							{
								isAbstract = true;
							}
							else if ((mif.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual)
							{
								isVirtual = true;
							}
						}
						else if (pifs[i].CanWrite)
						{
							MethodInfo mif = pifs[i].GetSetMethod(true);
							if ((mif.Attributes & MethodAttributes.Final) == MethodAttributes.Final)
							{
								continue;
							}
							if ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
							{
								isAbstract = true;
							}
							else if ((mif.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual)
							{
								isVirtual = true;
							}
						}
						if (isAbstract)
						{
							bool b = props.ContainsKey(pifs[i].Name);
							if (!b)
							{
								PropertyOverride po = (PropertyOverride)CreateNewProperty(
									new DataTypePointer(new TypePointer(pifs[i].PropertyType)),
									true,
									pifs[i].Name,
									!isAbstract, pifs[i].CanRead, pifs[i].CanWrite, false, EnumWebRunAt.Inherit);
								if (po != null)
								{
									props.Add(po.Name, po);
								}
							}
						}
						else if (isVirtual)
						{
							bool b = props.ContainsKey(pifs[i].Name);
							if (!b)
							{
								PropertyClassInherited pci = createInheritedProperty(pifs[i]);
								props.Add(pci.Name, pci);
							}
						}
					}
				}
			}
			_customPropertyList = props;
			_propertyOverrides = null;
			#endregion
			#region methods
			List<MethodClass> lst2 = getCustomMethods();
			Dictionary<string, MethodClass> methods = new Dictionary<string, MethodClass>();
			foreach (MethodClass m in lst2)
			{
				methods.Add(m.MethodSignature, m);
			}
			t = this.BaseClassType;
			MethodInfo[] mifs = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (mifs != null && mifs.Length > 0)
			{
				for (int i = 0; i < mifs.Length; i++)
				{
					if (!mifs[i].IsSpecialName)
					{
						if (TypeUtil.GetAccessControl(mifs[i]) == EnumAccessControl.Private)
						{
							continue;
						}
						if (this.IsWebPage)
						{
							object[] attrs = mifs[i].GetCustomAttributes(typeof(WebServerMemberAttribute), true);
							if (attrs == null || attrs.Length == 0)
							{
								attrs = mifs[i].GetCustomAttributes(typeof(WebClientMemberAttribute), true);
								if (attrs == null || attrs.Length == 0)
								{
									continue;
								}
							}
						}
						string sm = MethodPointer.GetMethodSignature(mifs[i]);
						if (methods.ContainsKey(sm))
						{
							continue;
						}
						bool isVirtual = false;
						bool isAbstract = VPLUtil.IsAbstract(mifs[i]);
						if (!isAbstract)
						{
							if ((mifs[i].Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual)
							{
								isVirtual = true;
							}
						}
						if (isAbstract || isVirtual)
						{

							MethodClassInherited pci = createInheritedMethod(mifs[i]);
							if (isAbstract)
							{
								MethodOverride po = ImplementOverrideMethod(pci);
								methods.Add(po.MethodSignature, po);
							}
							else
							{
								methods.Add(pci.MethodSignature, pci);
							}

						}
					}
				}
			}
			_customMethodList = methods;
			_methodOverrides = null;
			#endregion
			#region events
			List<EventClass> lst3 = getCustomEvents();
			Dictionary<string, EventClass> events = new Dictionary<string, EventClass>();
			foreach (EventClass e in lst3)
			{
				events.Add(e.Name, e);
			}
			_customEventList = events;
			#endregion
		}
		public MethodClass GetCustomMethodBySignature(string signature)
		{
			Dictionary<string, MethodClass> ms = CustomMethods;
			return ms[signature];
		}
		public MethodOverride GetCustomMethodByBaseMethodId(UInt32 classId, UInt32 methodId)
		{
			Dictionary<string, MethodClass> ms = CustomMethods;
			foreach (MethodClass m in ms.Values)
			{
				MethodOverride mo = m as MethodOverride;
				if (mo != null)
				{
					if (mo.BaseClassId == classId && mo.BaseMethodId == methodId)
					{
						return mo;
					}
				}
			}
			return null;
		}
		public EventClass GetCustomEventById(UInt32 eventId)
		{
			Dictionary<string, EventClass> es = CustomEvents;
			foreach (EventClass e in es.Values)
			{
				if (e.MemberId == eventId)
				{
					return e;
				}
			}
			return null;
		}
		public List<VplMethodPointer> GetCustomMethodsByParamTypes(bool runAtWebClient, Type[] types)
		{
			int np = 0;
			if (types != null && types != Type.EmptyTypes)
			{
				np = types.Length;
			}
			List<VplMethodPointer> ret = new List<VplMethodPointer>();
			Dictionary<string, MethodClass> ms = CustomMethods;
			foreach (MethodClass m in ms.Values)
			{
				if (m.ParameterCount == np)
				{
					if (runAtWebClient)
					{
						if (m.RunAt != EnumWebRunAt.Client)
						{
							continue;
						}
					}
					else
					{
						if (m.RunAt != EnumWebRunAt.Server)
						{
							continue;
						}
					}
					if (np > 0)
					{
						bool match = true;
						for (int i = 0; i < np; i++)
						{
							if (!types[i].IsAssignableFrom(m.Parameters[i].BaseClassType))
							{
								match = false;
								break;
							}
						}
						if (!match)
						{
							continue;
						}
					}
					ret.Add(new VplMethodPointer(m.MethodID, m.MethodName));
				}
			}
			return ret;
		}
		public MethodClass GetCustomMethodById(UInt32 methodId)
		{
			if (methodId != 0)
			{
				if (MethodInEditing != null && MethodInEditing.MethodID == methodId)
				{
					return MethodInEditing;
				}
				Dictionary<string, MethodClass> ms = CustomMethods;
				foreach (MethodClass m in ms.Values)
				{
					if (m.MemberId == methodId)
					{
						return m;
					}
				}
				Dictionary<string, PropertyClass> pcs = CustomProperties;
				foreach (PropertyClass pc in pcs.Values)
				{
					if (pc.Getter != null)
					{
						if (pc.Getter.MemberId == methodId)
						{
							return pc.Getter;
						}
					}
					if (pc.Setter != null)
					{
						if (pc.Setter.MemberId == methodId)
						{
							return pc.Setter;
						}
					}
				}
				List<EventAction> ehs = EventHandlers;
				foreach (EventAction ea in ehs)
				{
					foreach (TaskID tid in ea.TaskIDList)
					{
						HandlerMethodID hid = tid as HandlerMethodID;
						if (hid != null)
						{
							if (hid.HandlerMethod.MethodID == methodId)
							{
								return hid.HandlerMethod;
							}
						}
					}
				}
			}
			List<ConstructorClass> clst = GetConstructors();
			if (clst != null && clst.Count > 0)
			{
				foreach (ConstructorClass cc in clst)
				{
					if (cc.MethodID == methodId)
					{
						return cc;
					}
				}
			}
			return null;
		}
		public List<MethodClassInherited> GetMethodOverrides()
		{
			if (_methodOverrides == null)
			{
				List<MethodClassInherited> props = new List<MethodClassInherited>();
				Dictionary<string, MethodClass> lst = CustomMethods;
				foreach (MethodClass p in lst.Values)
				{
					MethodClassInherited po = p as MethodClassInherited;
					if (po != null)
					{
						props.Add(po);
					}
				}
				_methodOverrides = props;
			}
			return _methodOverrides;
		}
		public List<PropertyClassInherited> GetPropertyOverrides()
		{
			if (_propertyOverrides == null)
			{
				List<PropertyClassInherited> props = new List<PropertyClassInherited>();
				Dictionary<string, PropertyClass> lst = CustomProperties;
				foreach (PropertyClass p in lst.Values)
				{
					PropertyClassInherited po = p as PropertyClassInherited;
					if (po != null)
					{
						props.Add(po);
					}
				}
				_propertyOverrides = props;
			}
			return _propertyOverrides;
		}
		/// <summary>
		/// includes _wholePropertyList and PropertyClassInherited instances from BaseType
		/// </summary>
		[Browsable(false)]
		public Dictionary<string, PropertyClass> CustomProperties
		{
			get
			{
				if (_customPropertyList == null)
				{
					//if this property is used before OnDesignPaneLoaded is called
					//then get custom properties without overriding the abstract base properties and without including un-overriden virtual properties
					//because overriding must be done in OnDesignPaneLoaded when the class is loaded for design
					List<PropertyClass> lst = getCustomProperties();
					Dictionary<string, PropertyClass> props = new Dictionary<string, PropertyClass>();
					foreach (PropertyClass p in lst)
					{
						props.Add(p.Name, p);
					}
					return props;
				}
				return _customPropertyList;
			}
		}
		private List<PropertyClass> getCustomProperties()
		{
			if (_wholePropertyList == null)
			{
				List<PropertyClass> lst = GetCurrentLevelCustomProperties();
				List<PropertyClass> props = new List<PropertyClass>();
				props.AddRange(lst);
				ClassPointer cp = this.BaseClassPointer;
				while (cp != null)
				{
					List<PropertyClass> props0 = cp.GetCurrentLevelCustomProperties();
					//check the inheritance if it is not final. not final means overridable
					List<PropertyClass> fromBase = new List<PropertyClass>();
					foreach (PropertyClass p in props0)
					{
						bool exists = false;
						foreach (PropertyClass p0 in props)
						{
							if (p.Name == p0.Name)
							{
								exists = true;
								break;
							}
							PropertyOverride pov = p0 as PropertyOverride;
							if (pov != null)
							{
								if (pov.BaseClassId == p.ClassId && pov.BasePropertyId == p.MemberId)
								{
									exists = true;
									pov.SetName(p.Name);
									pov.SetMembers(p.CanRead, p.CanWrite, p.AccessControl, p.PropertyType);
									break;
								}
							}
						}
						if (!exists)
						{
							PropertyClassInherited pi = p.CreateInherited(this);
							fromBase.Add(pi);
						}
					}
					props.AddRange(fromBase);
					cp = cp.BaseClassPointer;
				}
				_wholePropertyList = props;
			}
			return _wholePropertyList;
		}
		public Dictionary<string, EventClass> CustomEvents
		{
			get
			{
				if (_customEventList == null)
				{
					//if this event is used before OnDesignPaneLoaded is called
					//then get custom events without overriding the abstract base properties and without including un-overriden virtual properties
					//because overriding must be done in OnDesignPaneLoaded when the class is loaded for design
					List<EventClass> lst = getCustomEvents();
					Dictionary<string, EventClass> es = new Dictionary<string, EventClass>();
					foreach (EventClass e in lst)
					{
						es.Add(e.Name, e);
					}
					_customEventList = es;
				}
				return _customEventList;
			}
		}
		private List<EventClass> getCustomEvents()
		{
			if (_wholeEventList == null)
			{
				List<EventClass> lst = GetCurrentLevelCustomEvents();
				List<EventClass> events = new List<EventClass>();
				events.AddRange(lst);
				ClassPointer cp = this.BaseClassPointer;
				while (cp != null)
				{
					List<EventClass> events0 = cp.GetCurrentLevelCustomEvents();//parent event
					List<EventClass> fromBase = new List<EventClass>();
					foreach (EventClass e in events0)
					{
						bool exists = false;
						foreach (EventClass e0 in events)
						{
							if (e.Name == e0.Name)
							{
								exists = true;
								break;
							}
						}
						if (!exists)
						{
							EventClassInherited ei = e.CreateInherited(this);
							fromBase.Add(ei);
						}
					}
					events.AddRange(fromBase);
					cp = cp.BaseClassPointer;
				}
				_wholeEventList = events;
			}
			return _wholeEventList;
		}

		/// <summary>
		/// ObjectExplorer
		///   BaseSignature1
		///     BaseSignature1+sub11
		///     BaseSignature1+sub12
		///   BaseSignature2
		///     BaseSignature2+sub21
		///     BaseSignature2+sub22
		/// this function only gets implemented constructors.
		/// ObjectExplorer should call BaseClassPointer.GetConstructors() to show 
		/// available constructors. if BaseClassPointer is null then the available
		/// constructors are from BaseClassType. This logic is implemented in GetBaseConstructors()
		/// </summary>
		/// <returns></returns>
		public List<ConstructorClass> GetConstructors()
		{
			List<ConstructorClass> constructors = new List<ConstructorClass>();
			XmlObjectReader xr = _objectList.Reader;
			XmlNodeList nodes = SerializeUtil.GetConstructorNodeList(_objectList.XmlData);
			if (nodes.Count == 0)
			{
				ConstructorInfo[] cifs = this.BaseClassType.GetConstructors();
				for (int i = 0; i < cifs.Length; i++)
				{
					ConstructorClass a = new ConstructorClass(this, cifs[i]);
					constructors.Add(a);
				}
			}
			else
			{
				constructors = GetCustomConstructors();
			}
			return constructors;
		}
		public List<ConstructorClass> GetCustomConstructors()
		{
			List<ConstructorClass> constructors = new List<ConstructorClass>();

			XmlNodeList nodes = SerializeUtil.GetConstructorNodeList(_objectList.XmlData);
			if (nodes.Count > 0)
			{
				XmlObjectReader xr = _objectList.Reader;
				Guid g = Guid.NewGuid();
				xr.ClearDelayedInitializers(g);
				foreach (XmlNode nd in nodes)
				{
					ConstructorClass a = xr.ReadObject<ConstructorClass>(nd, this);
					constructors.Add(a);
				}

				if (xr.HasErrors)
				{
					MathNode.Log(xr.Errors);
				}
				xr.OnDelayedInitializeObjects(g);
			}
			return constructors;
		}
		public SortedList<int, ConstructorClass> GetConstructorsSortedByParameterCount()
		{
			List<ConstructorClass> list = GetConstructors();
			SortedList<int, ConstructorClass> sorted = new SortedList<int, ConstructorClass>();
			foreach (ConstructorClass c in list)
			{
				sorted.Add(c.ParameterCount, c);
			}
			return sorted;
		}
		/// <summary>
		/// Get available construtors from the base class
		/// </summary>
		/// <returns></returns>
		public List<ConstructorClass> GetBaseConstructors()
		{
			List<ConstructorClass> baseConstructors;
			ClassPointer cp = this._baseClassPointer;
			if (cp != null)
			{
				baseConstructors = cp.GetConstructors();
			}
			else
			{
				baseConstructors = new List<ConstructorClass>();
				ConstructorInfo[] cifs = this.BaseClassType.GetConstructors();
				if (cifs != null && cifs.Length > 0)
				{
					for (int i = 0; i < cifs.Length; i++)
					{
						ConstructorClass a = new ConstructorClass(this, cifs[i]);
						baseConstructors.Add(a);
					}
				}
				else
				{
					//add a default parameterless constructor
					ConstructorInfo cif = typeof(object).GetConstructor(new Type[] { });
					ConstructorClass a = new ConstructorClass(this, cif);
					baseConstructors.Add(a);
				}
			}
			return baseConstructors;
		}
		public SortedList<string, ConstructorClass> GetBaseConstructorsSortedByParameterCount()
		{
			List<ConstructorClass> list = GetBaseConstructors();
			SortedList<string, ConstructorClass> sorted = new SortedList<string, ConstructorClass>();
			foreach (ConstructorClass c in list)
			{
				sorted.Add(c.GetMethodSignature(), c);
			}
			return sorted;
		}
		public StringCollection GetDocumentFiles()
		{
			StringCollection sc = new StringCollection();
			if (XmlData != null)
			{
				XmlNodeList xmls = XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					"{0}/{1}", XmlTags.XML_DOCS, XmlTags.XML_Item));
				if (xmls != null && xmls.Count > 0)
				{
					foreach (XmlNode nd in xmls)
					{
						sc.Add(nd.InnerText);
					}
				}
			}
			return sc;
		}
		public void AddDocumentFile(string file)
		{
			if (XmlData != null)
			{
				XmlNode docsNode = XmlData.SelectSingleNode(XmlTags.XML_DOCS);
				if (docsNode == null)
				{
					docsNode = XmlData.OwnerDocument.CreateElement(XmlTags.XML_DOCS);
					XmlData.AppendChild(docsNode);
				}
				XmlNodeList ns = docsNode.SelectNodes(XmlTags.XML_Item);
				if (ns != null && ns.Count > 0)
				{
					foreach (XmlNode nd in ns)
					{
						if (string.Compare(file, nd.InnerText, StringComparison.OrdinalIgnoreCase) == 0)
						{
							return;
						}
					}
				}
				XmlNode n = docsNode.OwnerDocument.CreateElement(XmlTags.XML_Item);
				docsNode.AppendChild(n);
				n.InnerText = file;
				ILimnorDesignerLoader loader = GetDesignerLoader();
				if (loader != null)
				{
					loader.NotifyChanges();
				}
			}
		}
		public void DeleteDocumentFile(string file)
		{
			if (XmlData != null)
			{
				XmlNode docsNode = XmlData.SelectSingleNode(XmlTags.XML_DOCS);
				if (docsNode != null)
				{
					XmlNodeList ns = docsNode.SelectNodes(XmlTags.XML_Item);
					if (ns != null && ns.Count > 0)
					{
						XmlNode n = null;
						foreach (XmlNode nd in ns)
						{
							if (string.Compare(file, nd.InnerText, StringComparison.OrdinalIgnoreCase) == 0)
							{
								n = nd;
								break;
							}
						}
						if (n != null)
						{
							docsNode.RemoveChild(n);
							ILimnorDesignerLoader loader = GetDesignerLoader();
							if (loader != null)
							{
								loader.NotifyChanges();
							}
						}
					}
				}
			}
		}
		public bool DocumentExists(string file)
		{
			if (XmlData != null)
			{
				XmlNodeList xmls = XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					"{0}/{1}", XmlTags.XML_DOCS, XmlTags.XML_Item));
				if (xmls != null && xmls.Count > 0)
				{
					foreach (XmlNode nd in xmls)
					{
						if (string.Compare(file, nd.InnerText, StringComparison.OrdinalIgnoreCase) == 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		public void RenameDocumentFile(string file, string newFile)
		{
			if (XmlData != null)
			{
				XmlNode docsNode = XmlData.SelectSingleNode(XmlTags.XML_DOCS);
				if (docsNode == null)
				{
					docsNode = XmlData.OwnerDocument.CreateElement(XmlTags.XML_DOCS);
					XmlData.AppendChild(docsNode);
				}
				ILimnorDesignerLoader loader;
				XmlNodeList ns = docsNode.SelectNodes(XmlTags.XML_Item);
				if (ns != null && ns.Count > 0)
				{
					foreach (XmlNode nd in ns)
					{
						if (string.Compare(file, nd.InnerText, StringComparison.OrdinalIgnoreCase) == 0)
						{
							nd.InnerText = newFile;
							loader = GetDesignerLoader();
							if (loader != null)
							{
								loader.NotifyChanges();
							}
							return;
						}
					}
				}
				XmlNode n = docsNode.OwnerDocument.CreateElement(XmlTags.XML_Item);
				docsNode.AppendChild(n);
				n.InnerText = newFile;
				loader = GetDesignerLoader();
				if (loader != null)
				{
					loader.NotifyChanges();
				}
			}
		}
		public static void DeleteAction(XmlNode holderNode, UInt32 actionId)
		{
			XmlNode rootNode = holderNode;
			XmlNode actNode = rootNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}[@{2}='{3}']", XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, actionId));
			if (actNode != null)
			{
				XmlNode p = actNode.ParentNode;
				p.RemoveChild(actNode);
			}
		}
		public void DeleteActions(List<UInt32> actionIds)//, bool includePublic)
		{
			if (_actions != null)
			{
				foreach (UInt32 id in actionIds)
				{
					IAction a;
					if (_actions.TryGetValue(id, out a))
					{
						if (a != null)
						{
							DeleteAction(_objectList.XmlData, id);
						}
						_actions.Remove(id);
					}
				}
			}
		}

		public void SaveAction(IAction action, XmlObjectWriter xw)
		{
			try
			{
				if (action.ActionId == 0)
				{
					return;
				}
				if (xw == null)
				{
					xw = new XmlObjectWriter(this.ObjectList);
				}
				IActionsHolder holder = action.ActionHolder;
				if (holder == null)
				{
					throw new DesignerException("Action missing holder. {0}", action.ActionId);
				}
				holder.AddActionInstance(action);
				XmlNode ndActionList = holder.ActionsNode;
				XmlNode ndAction = ndActionList.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}[@{1}='{2}']", XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, action.ActionId));
				if (ndAction == null)
				{
					ndAction = action.XmlData;
					if (ndAction == null)
					{
						ndAction = ndActionList.OwnerDocument.CreateElement(XmlTags.XML_ACTION);
						ndActionList.AppendChild(ndAction);
					}
					else
					{
						if (ndAction.ParentNode != ndActionList)
						{
							ndActionList.AppendChild(ndAction);
						}
					}
				}
				else
				{
					if (ndAction.ParentNode == null)
					{
						ndActionList.AppendChild(ndAction);
					}
				}
				HtmlElement_BodyBase he = action.MethodOwner as HtmlElement_BodyBase;
				if (he == null)
				{
					HtmlElementPointer hep = action.MethodOwner as HtmlElementPointer;
					if (hep != null)
					{
						if (hep.Element == null)
						{
							throw new DesignerException("Html element pointer does not point to a valid html element");
						}
						he = hep.Element;
					}
				}
				if (he != null)
				{
					OnUseHtmlElement(he);
				}
				XmlUtil.SetAttribute(ndAction, XmlTags.XMLATT_ActionID, action.ActionId);
				XmlUtil.SetAttribute(ndAction, XmlTags.XMLATT_NAME, action.ActionName);
				xw.WriteObjectToNode(ndAction, action);
				if (xw.HasErrors)
				{
					MathNode.Log(xw.ErrorCollection);
				}
				ILimnorDesignPane pane = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
				if (pane != null)
				{
					if (!(action is MethodActionForeach))
					{
						pane.OnActionChanged(this.ClassId, action, false);
					}
				}
			}
			catch (Exception e)
			{
				MathNode.Log(TraceLogClass.MainForm, e);
			}
		}
		public Dictionary<UInt32, IAction> GetActions()
		{
			if (_actions == null)
			{
				LoadActionInstances();
			}
			return _actions;
		}
		public List<ObjectTextID> GetComponentUsage(UInt32 componentId)
		{
			List<ObjectTextID> list = new List<ObjectTextID>();
			string xp = string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}//{2}[@{3}='Owner']/{4}[@type='MemberComponentId']/{2}[@{3}='MemberId' and text()='{5}']",
				XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_ObjProperty, componentId);
			//Actions.../Property name="Owner"/ObjProperty type="MemberComponentId"/Property name="MemberId"/{id}
			XmlNodeList nodeList = XmlData.SelectNodes(xp);
			if (nodeList != null && nodeList.Count > 0)
			{
				foreach (XmlNode nd in nodeList)
				{
					XmlNode actNode = nd.ParentNode;
					while (string.CompareOrdinal(XmlTags.XML_ACTION, actNode.Name) != 0)
					{
						actNode = actNode.ParentNode;
					}
					list.Add(new ObjectTextID("Action", XmlUtil.GetAttribute(actNode, XmlTags.XMLATT_type), XmlUtil.GetNameAttribute(actNode)));
				}
			}
			xp = string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}//*[@{2}='{3}']", XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_instanceId, componentId);
			nodeList = XmlData.SelectNodes(xp);
			if (nodeList != null && nodeList.Count > 0)
			{
				foreach (XmlNode nd in nodeList)
				{
					XmlNode actNode = nd.ParentNode;
					while (string.CompareOrdinal(XmlTags.XML_ACTION, actNode.Name) != 0)
					{
						actNode = actNode.ParentNode;
					}
					list.Add(new ObjectTextID("Action", XmlUtil.GetAttribute(actNode, XmlTags.XMLATT_type), XmlUtil.GetNameAttribute(actNode)));
				}
			}
			return list;
		}
		[Browsable(false)]
		public void ReloadEventHandlers()
		{
			XmlNode rootNode = _objectList.XmlData;
			_eventActions = new List<EventAction>();
			if (rootNode != null)
			{
				XmlObjectReader xr = _objectList.Reader;
				Guid g = Guid.NewGuid();
				xr.ClearDelayedInitializers(g);
				List<XmlNode> invalidNodes = new List<XmlNode>();
				XmlNodeList nodes = rootNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					"{0}/{1}", XmlTags.XML_HANDLERLISTS, XmlTags.XML_HANDLER));
				foreach (XmlNode nd in nodes)
				{
					bool isValid = true;
					EventAction a = (EventAction)xr.ReadObject(nd, null);
					UInt32 abId = a.GetAssignActionId();
					if (abId != 0)
					{
						if (a.Event == null || !a.Event.IsValid)
						{
							XmlNode abNode = rootNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
								"//{0}[@name='BranchId' and text()='{1}']",
								XmlTags.XML_PROPERTY, abId));
							if (abNode == null)
							{
								abNode = rootNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
								"{0}/{1}[@{2}='{3}']",
								XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, abId));
								if (abNode == null)
								{
									isValid = false;
								}
							}
						}
					}
					if (isValid)
					{
						a.TaskIDList.RemoveInvalidTasks();
						_eventActions.Add(a);
					}
					else
					{
						invalidNodes.Add(nd);
					}
				}
				foreach (XmlNode nd in invalidNodes)
				{
					XmlNode p = nd.ParentNode;
					p.RemoveChild(nd);
				}
				xr.OnDelayedInitializeObjects(g);
				if (xr.Errors != null && xr.Errors.Count > 0)
				{
					MathNode.Log(xr.Errors);
				}
				foreach (EventAction ea in _eventActions)
				{
					EventPointer ep = ea.Event as EventPointer;
					if (ep != null)
					{
						if (ep.IsValid)
						{
							if (ep.Info != null) //force it to load _eif
							{
							}
						}
					}
				}
			}
		}
		private void loadAction(XmlNode nd, IActionsHolder actsHolder, Dictionary<UInt32, IAction> acts)
		{
			XmlObjectReader xr = _objectList.Reader;
			IAction a = null;
			UInt32 actId = XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_ActionID);
			if (acts.TryGetValue(actId, out a))
			{
				if (a != null)
				{
					Guid g = Guid.NewGuid();
					xr.ResetErrors();
					xr.ClearDelayedInitializers(g);
					a.ReloadFromXmlNode();
					a.ActionHolder = actsHolder;
					xr.OnDelayedInitializeObjects(g);
					if (xr.HasErrors)
					{
						MathNode.Log(xr.Errors);
					}
				}
			}
			else
			{
				acts.Add(actId, null);
				try
				{
					Guid g = Guid.NewGuid();
					xr.ResetErrors();
					xr.ClearDelayedInitializers(g);
					a = (IAction)xr.ReadObject(nd, actsHolder);
					xr.OnDelayedInitializeObjects(g);
					if (xr.HasErrors)
					{
						MathNode.Log(xr.Errors);
					}
				}
				catch (Exception err)
				{
					DesignUtil.WriteToOutputWindowAndLog("Error reading action [{0}]. {1}", actId, DesignerException.FormExceptionText(err));
					a = new VoidAction(this, actId);
				}
				if (a == null)
				{
					DesignUtil.WriteToOutputWindow("Error reading action [{0}] from [{1}]", XmlUtil.GetLibTypeAttributeString(nd), nd.Name);
				}
				else
				{
					try
					{
						if (a.ActionId != actId)
						{
							a.ActionId = actId;
						}
						a.ActionName = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_NAME);
					}
					catch (Exception err2)
					{
						DesignUtil.WriteToOutputWindowAndLog("Error setting action id action [{0}]. {1}", actId, DesignerException.FormExceptionText(err2));
						a = new VoidAction(this, actId);
						a.ActionName = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_NAME);
					}
					a.ActionHolder = actsHolder;
					if (acts.ContainsKey(actId))
					{
						acts[actId] = a;
					}
					else
					{
						acts.Add(actId, a);
					}
				}
			}
			if (a != null)
			{
				actsHolder.AddActionInstance(a);
				a.ValidateParameterValues();
			}
		}
		private IAction _modifyingAction;
		public void LoadActions(IActionsHolder actsHolder)
		{
			Dictionary<UInt32, IAction> acts = actsHolder.ActionInstances;
			if (acts == null)
			{
				acts = new Dictionary<uint, IAction>();
			}
			if (actsHolder.ActionsNode != null)
			{
				XmlObjectReader xr = _objectList.Reader;
				xr.ResetErrors();
				List<IPostOwnersSerialize> objs = new List<IPostOwnersSerialize>();
				xr.PushPostOwnersDeserializers(objs);
				XmlNodeList nodes = actsHolder.ActionsNode.SelectNodes(XmlTags.XML_ACTION);
				foreach (XmlNode nd in nodes)
				{
					//backword compatible
					if (actsHolder.ScopeId == 0)
					{
						UInt32 scopeMethodId = XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_ScopeId);
						if (scopeMethodId != 0)
						{
							continue;
						}
					}
					if (_modifyingAction != null)
					{
						if (XmlUtil.GetAttributeUInt(nd, "ActionId") == _modifyingAction.ActionId)
						{
							if (acts.ContainsKey(_modifyingAction.ActionId))
							{
								acts[_modifyingAction.ActionId] = _modifyingAction;
							}
							continue;
						}
					}
					loadAction(nd, actsHolder, acts);
				}
				//backword compatible
				if (actsHolder.ScopeId != 0)
				{
					nodes = actsHolder.ActionsNode.OwnerDocument.DocumentElement.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}", XmlTags.XML_ACTIONS, XmlTags.XML_ACTION));
					foreach (XmlNode nd in nodes)
					{
						UInt32 scopeMethodId = XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_ScopeId);
						if (scopeMethodId != actsHolder.ScopeId)
						{
							continue;
						}
						UInt32 aid = XmlUtil.GetAttributeUInt(nd, "ActionId");
						if (_modifyingAction != null)
						{
							if (aid == _modifyingAction.ActionId)
							{
								if (acts.ContainsKey(_modifyingAction.ActionId))
								{
									acts[_modifyingAction.ActionId] = _modifyingAction;
								}
								continue;
							}
						}
						loadAction(nd, actsHolder, acts);
					}
				}
				if (xr.Errors != null && xr.Errors.Count > 0)
				{
					MathNode.Log(xr.Errors);
				}
				actsHolder.SetActionInstances(acts);
				foreach (IAction act in acts.Values)
				{
					act.OnAfterDeserialize(actsHolder);
				}
				IPostOwnersSerialize[] pos = xr.PopPostSerializers();
				if (pos != null && pos.Length > 0)
				{
					for (int i = 0; i < pos.Length; i++)
					{
						pos[i].OnAfterReadOwners(xr, actsHolder.ActionsNode, new object[] { actsHolder });
					}
				}
				foreach (IAction act in acts.Values)
				{
					try
					{
						if (act != null)
						{
							act.ValidateParameterValues();
						}
					}
					catch (Exception err)
					{
						DesignUtil.WriteToOutputWindowAndLog("Error Validating parameter values for action [{0},{1}]. {2}", act.ActionId, act.ActionName, DesignerException.FormExceptionText(err));
					}
				}
				List<UInt32> actIdList = new List<uint>();
				foreach (KeyValuePair<UInt32, IAction> kv in acts)
				{
					if (kv.Value == null)
					{
						actIdList.Add(kv.Key);
					}
					else
					{
						MethodActionForeach ma = kv.Value as MethodActionForeach;
						if (ma != null)
						{
							if (ma != actsHolder)
							{
								ma.LoadActionInstances();
							}
						}
						else
						{
							CustomMethodPointer cmp = kv.Value.ActionMethod as CustomMethodPointer;
							if (cmp != null)
							{
								if (!cmp.IsValid)
								{
									cmp.ReadLoad(xr);
									kv.Value.ValidateParameterValues();
								}
							}
						}
					}
				}
				foreach (UInt32 id in actIdList)
				{
					loadAction(id, actsHolder);
				}
			}
		}
		[Browsable(false)]
		public IAction TryGetActionInstance(UInt32 actId)
		{
			IAction act = null;
			if (_actions != null)
			{
				_actions.TryGetValue(actId, out act);
			}
			return act;
		}
		private IAction loadAction(UInt32 actId, IActionsHolder actsHolder)
		{
			IAction act = actsHolder.TryGetActionInstance(actId);
			if (act != null)
			{
				act.ReloadFromXmlNode();
			}
			else
			{
				XmlNode actsNode = actsHolder.ActionsNode;
				if (actsNode != null)
				{
					XmlObjectReader xr = _objectList.Reader;
					XmlNode node = actsNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}[@{2}='{3}']", XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, actId));
					if (node != null)
					{
						try
						{
							act = (IAction)xr.ReadObject(node, actsHolder);
							if (xr.HasErrors)
							{
								MathNode.Log(xr.Errors);
								xr.ResetErrors();
							}
							if (act != null)
							{
								act.ValidateParameterValues();
							}
						}
						catch (Exception err)
						{
							DesignUtil.WriteToOutputWindowAndLog("Error reading action [{0}]. {1}", actId, DesignerException.FormExceptionText(err));
							act = new VoidAction(this, actId);
						}
						if (act == null)
						{
							DesignUtil.WriteToOutputWindow("Error reading action [{0}] from [{1}]", XmlUtil.GetLibTypeAttributeString(node), node.Name);
						}
						else
						{
							try
							{
								act.ActionName = XmlUtil.GetAttribute(node, XmlTags.XMLATT_NAME);
							}
							catch (Exception err2)
							{
								DesignUtil.WriteToOutputWindowAndLog("Error setting action id action [{0}]. {1}", actId, DesignerException.FormExceptionText(err2));
								act = new VoidAction(this, actId);
								act.ActionName = XmlUtil.GetAttribute(node, XmlTags.XMLATT_NAME);
							}
							actsHolder.AddActionInstance(act);
						}
					}
				}
			}
			return act;
		}
		public void ReloadEventActions(IAction modifyingAction)
		{
			if (_modifyingAction != null && _modifyingAction == modifyingAction)
			{
				return;
			}
			_modifyingAction = modifyingAction;
			LoadActionInstances();
			ReloadEventHandlers();
			_modifyingAction = null;
		}
		[Browsable(false)]
		public bool Contains(IObjectPointer pointer)
		{
			IObjectPointer p = pointer;
			ClassPointer cm = pointer as ClassPointer;
			while (cm == null && p != null)
			{
				p = p.Owner;
				cm = p as ClassPointer;
			}
			if (cm != null)
			{
				if (this.IsSameObjectRef(cm))
				{
					return true;
				}
				if (cm.Owner != null)
				{
					return Contains(cm.Owner);
				}
			}
			return false;
		}
		public MethodClassInherited GetBaseMethod(string signature, UInt32 baseClassId, UInt32 baseMethodId)
		{
			if (baseClassId != 0 && baseClassId != ClassId)
			{
				ClassPointer baseC = GetBaseClass(baseClassId);
				if (baseC == null)
				{
					throw new DesignerException("GetBaseMethod: Base class [{0}] not found for class [{1}]", baseClassId, ClassId);
				}
				if (baseMethodId != 0)
				{
					MethodClass m = baseC.GetCustomMethodById(baseMethodId);
					if (m == null)
					{
						throw new DesignerException("GetBaseMethod: base method [{0},{1}] not found for class [{2}]", baseClassId, baseMethodId, ClassId);
					}
					MethodClassInherited mi = m.CreateInherited(this);
					return mi;
				}
				else
				{
					MethodClass m = baseC.GetCustomMethodBySignature(signature);
					if (m == null)
					{
						throw new DesignerException("GetBaseMethod: base method [{0}][{1},{2}] not found for class [{3}]", signature, baseClassId, baseMethodId, ClassId);
					}
					MethodClassInherited mi = m.CreateInherited(this);
					return mi;
				}
			}
			else
			{
				return createInheritedMethodBySignature(signature);
			}
		}
		public void OnDeleteBaseProperty(PropertyClass baseProperty)
		{
			if (GetBaseClass(baseProperty.ClassId) == null)
			{
				return;
			}
			if (_wholePropertyList != null)
			{
				PropertyClass px = null;
				foreach (PropertyClass p in _wholePropertyList)
				{
					if (p.WholeId == baseProperty.WholeId)
					{
						px = p;
						break;
					}
				}
				if (px != null)
				{
					_wholePropertyList.Remove(px);
				}
			}
			if (_customPropertyList != null)
			{
				PropertyClass px = null;
				foreach (PropertyClass p in _customPropertyList.Values)
				{
					if (p.WholeId == baseProperty.WholeId)
					{
						px = p;
						break;
					}
				}
				if (px != null)
				{
					_customPropertyList.Remove(px.Name);
				}
			}
			_propertyOverrides = null;
			_customProperties = null;
		}
		public void OnDeleteBaseMethod(MethodClass baseMethod)
		{
			if (GetBaseClass(baseMethod.ClassId) == null)
			{
				return;
			}
			if (_wholeMethodList != null)
			{
				MethodClass mx = null;
				foreach (MethodClass m in _wholeMethodList)
				{
					if (m.WholeId == baseMethod.WholeId)
					{
						mx = m;
						break;
					}
				}
				if (mx != null)
				{
					_wholeMethodList.Remove(mx);
				}
			}
			if (_customMethodList != null)
			{
				foreach (KeyValuePair<string, MethodClass> m in _customMethodList)
				{
					if (m.Value.WholeId == baseMethod.WholeId)
					{
						_customMethodList.Remove(m.Key);
						break;
					}
				}
			}
			_methodOverrides = null;
		}
		public void OnBaseMethodChanged(MethodClass baseMethod)
		{
			if (GetBaseClass(baseMethod.ClassId) == null)
			{
				return;
			}
			MethodOverride mov = null;
			MethodClassInherited mbi = baseMethod.CreateInherited(this);
			if (_wholeMethodList != null)
			{
				MethodClass mx = null;
				foreach (MethodClass m in _wholeMethodList)
				{
					if (m.WholeId == baseMethod.WholeId)
					{
						MethodClassInherited mi = m as MethodClassInherited;
						if (mi != null)
						{
							_wholeMethodList.Remove(m);
						}
						break;
					}
					MethodOverride mo = m as MethodOverride;
					if (mo != null)
					{
						if (mo.BaseClassId == baseMethod.ClassId && mo.BaseMethodId == baseMethod.MemberId)
						{
							mo.CopyFromInherited(mbi);
							mov = mo;
							mx = m;
							break;
						}
					}
				}
				if (mx == null)
				{
					_wholeMethodList.Add(mbi);
				}
			}
			if (_customMethodList != null)
			{
				foreach (KeyValuePair<string, MethodClass> m in _customMethodList)
				{
					if (m.Value.WholeId == baseMethod.WholeId)
					{
						_customMethodList.Remove(m.Key);
						break;
					}
					MethodOverride mo = m.Value as MethodOverride;
					if (mo != null)
					{
						if (mo.BaseClassId == baseMethod.ClassId && mo.BaseMethodId == baseMethod.MemberId)
						{
							mo.CopyFromInherited(mbi);
							mov = mo;
							_customMethodList.Remove(m.Key);
							break;
						}
					}
				}
				if (mov != null)
				{
					_customMethodList.Add(mov.MethodSignature, mov);
				}
				else
				{
					_customMethodList.Add(baseMethod.MethodSignature, mbi);
				}
			}
			if (_currentLevelMethodList != null)
			{
				foreach (MethodClass mx in _currentLevelMethodList)
				{
					MethodOverride mo = mx as MethodOverride;
					if (mo != null)
					{
						if (mo.BaseClassId == baseMethod.ClassId && mo.BaseMethodId == baseMethod.MemberId)
						{
							_currentLevelMethodList.Remove(mx);
							break;
						}
					}
				}
				if (mov != null)
				{
					_currentLevelMethodList.Add(mov);
				}
			}
			_methodOverrides = null;
			if (mov != null)
			{
				DataTypePointer[] dps = new DataTypePointer[baseMethod.ParameterCount];
				for (int i = 0; i < baseMethod.ParameterCount; i++)
				{
					dps[i] = baseMethod.Parameters[i];
				}
				//update base method call
				if (mov.ActionList != null)
				{
					mov.ActionList.SetActions(GetActions());
				}
			}
		}
		/// <summary>
		/// called when a base class adds a new property
		/// </summary>
		/// <param name="baseProperty"></param>
		public void OnAddBaseProperty(PropertyClassInherited baseProperty)
		{
			if (GetBaseClass(baseProperty.ClassId) == null)
			{
				return;
			}
			if (_wholePropertyList != null)
			{
				bool bFound = false;
				foreach (PropertyClass p in _wholePropertyList)
				{
					if (p.WholeId == baseProperty.WholeId)
					{
						bFound = true;
						break;
					}
					PropertyOverride po = p as PropertyOverride;
					if (po != null)
					{
						if (po.BaseClassId == baseProperty.ClassId && po.BasePropertyId == baseProperty.MemberId)
						{
							bFound = true;
							break;
						}
					}
				}
				if (!bFound)
				{
					_wholePropertyList.Add(baseProperty);
				}
			}
			if (_customPropertyList != null)
			{
				bool bFound = false;
				foreach (PropertyClass p in _customPropertyList.Values)
				{
					if (p.WholeId == baseProperty.WholeId)
					{
						bFound = true;
						break;
					}
					PropertyOverride po = p as PropertyOverride;
					if (po != null)
					{
						if (po.BaseClassId == baseProperty.ClassId && po.BasePropertyId == baseProperty.MemberId)
						{
							bFound = true;
							break;
						}
					}
				}
				if (!bFound)
				{
					_customPropertyList.Add(baseProperty.Name, baseProperty);
				}
			}
			_customProperties = null;
			_propertyOverrides = null;
		}
		private MethodClassInherited createInheritedMethodBySignature(string signature)
		{
			MethodInfo mif = null;
			MethodInfo[] mifs = BaseClassType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (mifs != null && mifs.Length > 0)
			{
				for (int i = 0; i < mifs.Length; i++)
				{
					if (signature == MethodPointer.GetMethodSignature(mifs[i]))
					{
						mif = mifs[i];
						break;
					}
				}
			}
			if (mif == null)
			{
				throw new DesignerException("Base method [{0}] not found in type [{1}]", signature, BaseClassType.FullName);
			}
			return createInheritedMethod(mif);
		}
		private MethodClassInherited createInheritedMethod(MethodInfo mif)
		{
			MethodClassInherited prop = new MethodClassInherited(this);
			prop.ReturnValue = new ParameterClass(mif.ReturnType, "return", prop);
			prop.Name = mif.Name;
			prop.SetDeclarer(this);
			prop.IsAbstract = VPLUtil.IsAbstract(mif);
			prop.AccessControl = TypeUtil.GetAccessControl(mif);
			ParameterInfo[] ps = mif.GetParameters();
			if (ps != null && ps.Length > 0)
			{
				List<ParameterClass> list = new List<ParameterClass>();
				for (int i = 0; i < ps.Length; i++)
				{
					list.Add(new ParameterClass(ps[i].ParameterType, ps[i].Name, prop));
				}
				prop.Parameters = list;
			}
			return prop;
		}
		public PropertyClass GetCustomPropertyById(UInt32 propId)
		{
			Dictionary<string, PropertyClass> ps = CustomProperties;
			foreach (PropertyClass p in ps.Values)
			{
				if (p.MemberId == propId)
				{
					return p;
				}
			}
			return null;
		}
		public PropertyClass GetCustomPropertyByName(string name)
		{
			return CustomProperties[name];
		}
		public Dictionary<string, PropertyPointer> GetLibraryProperties()
		{
			if (_libProperties == null)
			{
				_libProperties = new Dictionary<string, PropertyPointer>();
				PropertyDescriptorCollection props = VPLUtil.GetProperties(this, EnumReflectionMemberInfoSelectScope.Both, false, false, false);
				foreach (PropertyDescriptor p in props)
				{
					if (!_libProperties.ContainsKey(p.Name))
					{
						PropertyPointer pp;
						pp = new PropertyPointer();
						pp.Owner = this;
						pp.SetPropertyInfo(p);
						_libProperties.Add(p.Name, pp);
					}
				}
			}
			return _libProperties;
		}
		public Dictionary<string, MethodInfoPointer> GetLibMethods()
		{
			if (_libMethods == null)
			{
				_libMethods = new Dictionary<string, MethodInfoPointer>();
				MethodInfo[] mifs = ObjectType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (mifs.Length > 0)
				{
					for (int i = 0; i < mifs.Length; i++)
					{
						if (!mifs[i].IsSpecialName)
						{
							MethodInfoPointer mp = new MethodInfoPointer(mifs[i]);
							mp.Owner = this;
							string key = mp.MethodSignature;
							if (!_libMethods.ContainsKey(key))
							{
								_libMethods.Add(key, mp);
							}
						}
					}
				}
			}
			return _libMethods;
		}
		public Dictionary<string, EventPointer> GetLibEvents()
		{
			if (_libEvents == null)
			{
				_libEvents = new Dictionary<string, EventPointer>();
				EventInfo[] eifs = ObjectType.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				if (eifs.Length > 0)
				{
					for (int i = 0; i < eifs.Length; i++)
					{
						if (!eifs[i].IsSpecialName)
						{
							EventPointer ep = new EventPointer();
							ep.SetEventInfo(eifs[i]);
							ep.Owner = this;
							if (!_libEvents.ContainsKey(ep.Name))
							{
								_libEvents.Add(ep.Name, ep);
							}
						}
					}
				}
			}
			return _libEvents;
		}
		public ClassPointer GetBaseClass(UInt32 classId)
		{
			if (BaseClassPointer != null)
			{
				if (BaseClassPointer.ClassId == classId)
				{
					return BaseClassPointer;
				}
				return BaseClassPointer.GetBaseClass(classId);
			}
			return null;
		}
		public PropertyClassInherited GetBaseProperty(string name, UInt32 baseClassId, UInt32 basePropertyId)
		{
			if (baseClassId != 0 && baseClassId != ClassId)
			{
				ClassPointer baseC = GetBaseClass(baseClassId);
				if (baseC == null)
				{
					throw new DesignerException("GetBaseProperty: Base class [{0}] not found for class [{1}]", baseClassId, ClassId);
				}
				if (basePropertyId != 0)
				{
					PropertyClass p = baseC.GetCustomPropertyById(basePropertyId);
					if (p != null)
					{
						PropertyClassInherited pi = p.CreateInherited(this);
						return pi;
					}
					throw new DesignerException("GetBaseProperty: base property [{0},{1}] not found for class [{2}]", basePropertyId, name, baseClassId);
				}
				else
				{
					PropertyClass p = baseC.GetCustomPropertyByName(name);
					if (p != null)
					{
						PropertyClassInherited pi = p.CreateInherited(this);
						return pi;
					}
					throw new DesignerException("GetBaseProperty: base property [{0},{1}] not found for class [{2}]", basePropertyId, name, baseClassId);
				}
			}
			else
			{
				return createInheritedProperty(name);
			}
		}


		public LocalVariable GetLocalVariable(UInt32 id)
		{
			List<MethodClass> methods = GetCurrentLevelCustomMethods();
			foreach (MethodClass m in methods)
			{
				LocalVariable v = m.GetLocalVariable(id);
				if (v != null)
				{
					return v;
				}
			}
			Dictionary<string, PropertyClass> pcs = CustomProperties;
			foreach (PropertyClass pc in pcs.Values)
			{
				if (pc.Getter != null)
				{
					LocalVariable v = pc.Getter.GetLocalVariable(id);
					if (v != null)
					{
						return v;
					}
				}
				if (pc.Setter != null)
				{
					LocalVariable v = pc.Setter.GetLocalVariable(id);
					if (v != null)
					{
						return v;
					}
				}
			}
			List<EventAction> ehs = EventHandlers;
			foreach (EventAction ea in ehs)
			{
				foreach (TaskID tid in ea.TaskIDList)
				{
					HandlerMethodID hid = tid as HandlerMethodID;
					if (hid != null)
					{
						LocalVariable v = hid.HandlerMethod.GetLocalVariable(id);
						if (v != null)
						{
							return v;
						}
					}
				}
			}
			return null;
		}
		public string CreateNewActionName(string baseName)
		{
			string name = baseName + "1";
			int n = 1;
			Dictionary<UInt32, IAction> acts = GetActions();
			bool b = true;
			while (b)
			{
				b = false;
				foreach (IAction a in acts.Values)
				{
					if (a != null && a.ActionName == name)
					{
						b = true;
						n++;
						name = baseName + n.ToString();
					}
				}
			}
			return name;
		}

		public List<PropertyClass> GetCurrentLevelCustomProperties()
		{
			if (_currentLevelPropertyList == null)
			{
				lock (_lockObject)
				{
					if (bCyclicCheck_getCustomProperties)
					{
						bCyclicCheck_getCustomProperties = false;
						throw new DesignerException("cyclic call for GetCurrentLevelCustomProperties detected");
					}
					bCyclicCheck_getCustomProperties = true;
					try
					{
						List<PropertyClass> propertyList = new List<PropertyClass>();
						XmlObjectReader xr = _objectList.Reader;
						Guid g = Guid.NewGuid();
						xr.ClearDelayedInitializers(g);
						XmlNodeList nodes = SerializeUtil.GetCustomPropertyNodeList(XmlData);
						foreach (XmlNode nd in nodes)
						{
							PropertyClass a = xr.ReadObject<PropertyClass>(nd, this);
							propertyList.Add(a);
						}
						_currentLevelPropertyList = propertyList;
						xr.OnDelayedInitializeObjects(g);
					}
					catch (Exception e)
					{
						DesignUtil.WriteToOutputWindowAndLog("Error getting customer properties. {0}", DesignerException.FormExceptionText(e));
					}
					bCyclicCheck_getCustomProperties = false;
				}
			}
			return _currentLevelPropertyList;
		}
		/// <summary>
		/// get custom properties defined by this class
		/// </summary>
		/// <param name="leaf">the leaf ClassPointer that initiated the call</param>
		/// <returns></returns>
		public List<EventClass> GetCurrentLevelCustomEvents()
		{
			if (_currentLevelEventList == null)
			{
				List<EventClass> eventList = new List<EventClass>();
				XmlObjectReader xr = _objectList.Reader;
				XmlNodeList nodes = SerializeUtil.GetCustomEventNodeList(XmlData);
				foreach (XmlNode nd in nodes)
				{
					EventClass a = xr.ReadObject<EventClass>(nd, this);
					//prevent duplicate event name
					string newName = null;
					if (string.IsNullOrEmpty(a.Name))
					{
						newName = string.Format(CultureInfo.InvariantCulture, "event{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					}
					else
					{
						for (int i = 0; i < eventList.Count; i++)
						{
							if (string.CompareOrdinal(eventList[i].Name, a.Name) == 0)
							{
								if (a.Name.Length > 20)
								{
									newName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", a.Name.Substring(0, 20), Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
								}
								else
								{
									newName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", a.Name, Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
								}
								break;
							}
						}
					}
					if (newName != null)
					{
						a.Name = newName;
						XmlNode nnd = nd.SelectSingleNode("Property[@name='Name']");
						if (nnd != null)
						{
							nnd.InnerText = newName;
						}
						XmlUtil.SetNameAttribute(nd, newName);
					}
					eventList.Add(a);
				}
				_currentLevelEventList = eventList;
			}
			return _currentLevelEventList;
		}
		public PropertyClass implementProperty(InterfaceElementProperty p)
		{
			object prop = InterfacePropertyImplemented(p);
			if (prop != null)
			{
				return null; //already implemented
			}
			return CreateNewProperty(p, false, p.Name, false, p.CanRead, p.CanWrite, false, EnumWebRunAt.Inherit);
		}
		public MethodClass implementMethod(InterfaceElementMethod m)
		{
			if (MethodSignatureExists(m.GetMethodSignature()))
			{
				return null; //already implemented
			}
			MethodClass method;
			method = new MethodClass(this);
			method.Name = m.Name;
			method.MemberId = (UInt32)(Guid.NewGuid().GetHashCode());
			method.IsNewMethod = true;
			method.ReturnValue.SetDataType(m.ReturnType);
			List<InterfaceElementMethodParameter> ps = m.Parameters;
			if (ps != null && ps.Count > 0)
			{
				List<ParameterClass> pcs = new List<ParameterClass>();
				foreach (InterfaceElementMethodParameter p in ps)
				{
					ParameterClass p0 = new ParameterClass(method);
					p0.SetDataType(p);
					p0.SetName(p.Name);
					pcs.Add(p0);
				}
				method.Parameters = pcs;
			}
			SaveMethod(method, null);
			return method;
		}
		public EventClass implementEvent(InterfaceElementEvent e)
		{
			if (EventExists(e.Name))
			{
				return null; //already implemented
			}
			return CreateNewEvent(e, e.Name, false);
		}
		#endregion
		#region Usages
		private void checkActionUsage(StringCollection NAMES, XmlNode nd, List<ObjectTextID> list, IAction act)
		{
			if (string.CompareOrdinal(nd.Name, XmlTags.XML_PROPERTY) == 0)
			{
				if (string.CompareOrdinal("ActionId", XmlUtil.GetNameAttribute(nd)) == 0)
				{
					XmlNode np = nd.ParentNode;
					if (string.CompareOrdinal(np.Name, "Action") == 0) //it is the action definition XmlNode
					{
						UInt32 aid = XmlUtil.GetAttributeUInt(np, XmlTags.XMLATT_ActionID);
						if (aid == act.ActionId)
						{
							return;
						}
					}
				}
			}
			string objType;
			string name;
			string cname = XmlUtil.GetNameAttribute(nd.OwnerDocument.DocumentElement);
			XmlNode nodeUsage = nd;
			string handlerName = string.Empty;
			while (nodeUsage != null && !NAMES.Contains(nodeUsage.Name))
			{
				if (string.IsNullOrEmpty(handlerName))
				{
					if (string.CompareOrdinal(nodeUsage.Name, XmlTags.XML_ObjProperty) == 0)
					{
						string acid;
						Type t = XmlUtil.GetLibTypeAttribute(nodeUsage, out acid);
						if (t != null)
						{
							if (t.Equals(typeof(EventHandlerMethod)))
							{
								XmlNode nd0 = nodeUsage.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
									"{0}[@{1}='Name']", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
								if (nd0 != null)
								{
									handlerName = nd0.InnerText;
								}
							}
						}
					}
				}
				nodeUsage = nodeUsage.ParentNode;
			}
			if (nodeUsage != null)
			{
				if (string.CompareOrdinal(XmlTags.XML_HANDLER, nodeUsage.Name) == 0)
				{
					XmlNode ndEvent = nodeUsage.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}[@{1}='Event']/{2}", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_ObjProperty));
					if (ndEvent != null)
					{
						XmlObjectReader xr = new XmlObjectReader(this.ObjectList, OnAfterReadRootComponent, OnRootComponentCreated);
						IEvent e = (IEvent)xr.ReadObject(ndEvent, null);
						if (e != null)
						{
							if (string.IsNullOrEmpty(handlerName))
							{
								name = e.LongDisplayName;
							}
							else
							{
								name = string.Format(System.Globalization.CultureInfo.InvariantCulture,
									"[{0}].{1}", e.LongDisplayName, handlerName);
							}
						}
						else
						{
							if (string.IsNullOrEmpty(handlerName))
							{
								name = "?";
							}
							else
							{
								name = handlerName;
							}
						}
					}
					else
					{
						name = "?";
					}
				}
				else
				{
					name = XmlUtil.GetNameAttribute(nodeUsage);
				}
				objType = nodeUsage.Name;
			}
			else
			{
				name = XmlUtil.GetNameAttribute(nd);
				objType = nd.Name;
			}
			if (string.IsNullOrEmpty(name))
			{
				name = "?";
			}
			list.Add(new ObjectTextID(cname, objType, name));
		}
		public List<ObjectTextID> GetActionUsage(IAction act)
		{
			XmlNode node = this.XmlData;
			StringCollection NAMES = new StringCollection();
			NAMES.Add("Method");
			NAMES.Add("Handler");
			List<ObjectTextID> list = new List<ObjectTextID>();
			//find all methods that use this action
			XmlNodeList nodes = node.OwnerDocument.DocumentElement.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}//*[text()='{2}' and @{3}='{4}']",
				XmlTags.XML_METHODS, XmlTags.XML_METHOD, act.ActionId, XmlTags.XMLATT_NAME, XmlTags.XMLATT_ActionID));
			foreach (XmlNode nd in nodes)
			{
				checkActionUsage(NAMES, nd, list, act);
			}
			//find all handlers that use this action
			nodes = node.OwnerDocument.DocumentElement.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}//*[text()='{2}' and @{3}='{4}']",
				XmlTags.XML_HANDLERLISTS, XmlTags.XML_HANDLER, act.ActionId, XmlTags.XMLATT_NAME, XmlTags.XMLATT_ActionID));
			foreach (XmlNode nd in nodes)
			{
				checkActionUsage(NAMES, nd, list, act);
			}
			//find ActionAttachEvent
			nodes = node.OwnerDocument.DocumentElement.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}[@{2}='{3}']",
				XmlTags.XML_HANDLERLISTS, XmlTags.XML_HANDLER, XmlTags.XMLATT_ActionID, act.ActionId));
			foreach (XmlNode nd in nodes)
			{
				checkActionUsage(NAMES, nd, list, act);
			}
			//in other action lists
			nodes = node.OwnerDocument.DocumentElement.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"//{0}[@{1}='{2}']",
				XmlTags.XML_Item, XmlTags.XMLATT_ActionID, act.ActionId));
			foreach (XmlNode nd in nodes)
			{
				checkActionUsage(NAMES, nd, list, act);
			}
			//in other usages
			nodes = node.OwnerDocument.DocumentElement.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"//{0}/{1}[@{2}='{3}' and text()='{4}']",
				XmlTags.XML_ObjProperty, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XMLATT_ActionID, act.ActionId));
			foreach (XmlNode nd in nodes)
			{
				if (string.CompareOrdinal(nd.Name, "Property") == 0 && string.Compare(XmlUtil.GetAttribute(nd, "name"), "ActionId", StringComparison.OrdinalIgnoreCase) == 0)
				{
					continue;
				}
				checkActionUsage(NAMES, nd, list, act);
			}
			return list;
		}
		#endregion
		#region Programming Context Menu
		public void CreateContextMenu(IClass obj, ContextMenu mnu, Point location, Form caller)
		{
			LimnorContextMenuCollection menudata = LimnorContextMenuCollection.GetMenuCollection(obj);
			if (menudata != null)
			{
				MenuItem mi;
				MenuItem m0;
				MenuItem m1;
				//
				#region Create Action
				List<MenuItemDataMethod> methods = menudata.PrimaryMethods;
				if (methods.Count > 0)
				{
					mi = new MenuItemWithBitmap("Create Action", Resources._newMethodAction.ToBitmap());
					foreach (MenuItemDataMethod kv in methods)
					{
						m0 = new MenuItemWithBitmap(kv.Key, Resources._methodAction.ToBitmap());
						m0.Click += new EventHandler(miAction_Click);
						kv.Location = location;
						m0.Tag = kv;
						mi.MenuItems.Add(m0);
						kv.MenuOwner = obj;
						kv.MenuInvoker = caller;
					}
					methods = menudata.SecondaryMethods;
					if (methods.Count > 0)
					{
						m1 = new MenuItemWithBitmap("More methods", Resources._methods.ToBitmap());
						mi.MenuItems.Add(m1);
						foreach (MenuItemDataMethod kv in methods)
						{
							m0 = new MenuItemWithBitmap(kv.Key, Resources._methodAction.ToBitmap());
							m0.Click += new EventHandler(miAction_Click);
							kv.Location = location;
							m0.Tag = kv;
							m1.MenuItems.Add(m0);
							kv.MenuOwner = obj;
							kv.MenuInvoker = caller;
						}
						m0 = new MenuItemWithBitmap("*All methods* =>", Resources._dialog.ToBitmap());
						MenuItemDataMethodSelector miAll = new MenuItemDataMethodSelector(m0.Text, menudata);
						miAll.Location = location;
						m0.Tag = miAll;
						m1.MenuItems.Add(m0);
						m0.Click += new EventHandler(miSetMethods_Click);
					}
					//
					mnu.MenuItems.Add(mi);
				}
				#endregion
				//
				#region Create Set Property Action
				List<MenuItemDataProperty> properties = menudata.PrimaryProperties;
				if (properties.Count > 0)
				{
					mi = new MenuItemWithBitmap("Create Set Property Action", Resources._newPropAction.ToBitmap());
					foreach (MenuItemDataProperty kv in properties)
					{
						m0 = new MenuItemWithBitmap(kv.Key, Resources._propAction.ToBitmap());
						m0.Click += new EventHandler(miSetProperty_Click);
						kv.Location = location;
						m0.Tag = kv;
						mi.MenuItems.Add(m0);
						kv.MenuOwner = obj;
						kv.MenuInvoker = caller;
					}
					properties = menudata.SecondaryProperties;
					if (properties.Count > 0)
					{
						m1 = new MenuItemWithBitmap("More properties", Resources._properties.ToBitmap());
						mi.MenuItems.Add(m1);

						foreach (MenuItemDataProperty kv in properties)
						{
							m0 = new MenuItemWithBitmap(kv.Key, Resources._propAction.ToBitmap());
							m0.Click += new EventHandler(miSetProperty_Click);
							kv.Location = location;
							m0.Tag = kv;
							m1.MenuItems.Add(m0);
							kv.MenuOwner = obj;
							kv.MenuInvoker = caller;
						}
						m0 = new MenuItemWithBitmap("*All properties* =>", Resources._dialog.ToBitmap());
						m1.MenuItems.Add(m0);
						MenuItemDataPropertySelector pAll = new MenuItemDataPropertySelector(m0.Text, menudata);
						pAll.Location = location;
						m0.Tag = pAll;
						m0.Click += new EventHandler(miSetProperties_Click);
					}
					//
					mnu.MenuItems.Add(mi);
				}
				#endregion
				//
				#region Assign Actions
				List<MenuItemDataEvent> events = menudata.PrimaryEvents;
				if (events.Count > 0)
				{
					mi = new MenuItemWithBitmap("Assign Action", Resources._eventHandlers.ToBitmap());
					foreach (MenuItemDataEvent kv in events)
					{
						m0 = new MenuItemWithBitmap(kv.Key, Resources._event1.ToBitmap());
						m0.Click += new EventHandler(miAssignEvent_Click);
						kv.Location = location;
						m0.Tag = kv;
						mi.MenuItems.Add(m0);
						kv.MenuOwner = obj;
						kv.MenuInvoker = caller;
						EventItem ei = kv as EventItem;
						if (ei != null)
						{
							IEventInfoTree emi = ei.Value as IEventInfoTree;
							if (emi != null)
							{
								IEventInfoTree[] subs = emi.GetSubEventInfo();
								LimnorContextMenuCollection.createEventTree(m0, location, kv.MenuOwner, subs, new EventHandler(miAssignEvent_Click));
							}
						}
					}
					events = menudata.SecondaryEvents;
					if (events.Count > 0)
					{
						m1 = new MenuItemWithBitmap("More events", Resources._events.ToBitmap());
						mi.MenuItems.Add(m1);
						foreach (MenuItemDataEvent kv in events)
						{
							m0 = new MenuItemWithBitmap(kv.Key, Resources._event1.ToBitmap());
							m0.Click += new EventHandler(miAssignEvent_Click);
							kv.Location = location;
							m0.Tag = kv;
							m1.MenuItems.Add(m0);
							kv.MenuOwner = obj;
							kv.MenuInvoker = caller;
							EventItem ei = kv as EventItem;
							if (ei != null)
							{
								IEventInfoTree emi = ei.Value as IEventInfoTree;
								if (emi != null)
								{
									IEventInfoTree[] subs = emi.GetSubEventInfo();
									LimnorContextMenuCollection.createEventTree(m0, location, kv.MenuOwner, subs, new EventHandler(miAssignEvent_Click));
								}
							}
						}
						m0 = new MenuItemWithBitmap("*All events* =>", Resources._dialog.ToBitmap());
						m1.MenuItems.Add(m0);
						MenuItemDataEventSelector eAll = new MenuItemDataEventSelector(m0.Text, location, menudata);
						m0.Tag = eAll;
						m0.Click += new EventHandler(miSetEvents_Click);
					}
					mnu.MenuItems.Add(mi);
				}
				#endregion
				//
				HtmlElement_BodyBase hbb = obj as HtmlElement_BodyBase;
				if (hbb != null)
				{
					hbb.CreateContextMenu(mnu.MenuItems, miSetEvents_Click);
				}
			}
		}
		#region menu handlers
		private void createNewAction(MenuItemDataMethod data)
		{
			if (OnBeforeUseComponent(data.MenuOwner, data.MenuInvoker))
			{
				ILimnorDesignPane dp = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
				if (dp != null)
				{
					IAction act = data.CreateMethodAction(dp, data.MenuOwner, null, null);
					if (act != null)
					{

					}
				}
			}
		}
		private void miAction_Click(object sender, EventArgs e)
		{
			MenuItemDataMethod data = (MenuItemDataMethod)(((MenuItem)sender).Tag);
			createNewAction(data);
		}
		private void miSetMethods_Click(object sender, EventArgs e)
		{
			miAction_Click(sender, e);
		}
		private void miSetProperty_Click(object sender, EventArgs e)
		{
			MenuItemDataProperty data = (MenuItemDataProperty)(((MenuItem)sender).Tag);
			if (OnBeforeUseComponent(data.MenuOwner, data.MenuInvoker))
			{
				ILimnorDesignPane dp = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
				if (dp != null)
				{
					ActionClass act = data.CreateSetPropertyAction(dp, data.MenuOwner, null, null);
					if (act != null)
					{
					}
				}
			}
		}
		private void miSetProperties_Click(object sender, EventArgs e)
		{
			miSetProperty_Click(sender, e);
		}
		private void miAssignEvent_Click(object sender, EventArgs e)
		{
			MenuItemDataEvent data = (MenuItemDataEvent)(((MenuItem)sender).Tag);
			if (OnBeforeUseComponent(data.MenuOwner, data.MenuInvoker))
			{
				ILimnorDesignPane dp = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
				if (dp != null)
				{
					data.ExecuteMenuCommand(this.Project, data.MenuOwner, dp.RootXmlNode, dp.PaneHolder, null, null);
				}
			}
		}
		private void miSetEvents_Click(object sender, EventArgs e)
		{
			miAssignEvent_Click(sender, e);
		}
		#endregion
		#endregion
		#region private methods
		private bool _a()
		{
			return false;
		}
		private List<MethodClass> getCustomMethods()
		{
			if (_wholeMethodList == null)
			{
				if (System.Threading.Thread.CurrentThread.ManagedThreadId != 0)
				{
				}
				lock (_lockObject)
				{
					if (bCyclicCheck_getCustomMethods)
					{
						bCyclicCheck_getCustomMethods = false;
						throw new DesignerException("cyclic call for getCustomMethods detected");
					}
					bCyclicCheck_getCustomMethods = true;
					try
					{
						List<MethodClass> lst = GetCurrentLevelCustomMethods();
						List<MethodClass> methods = new List<MethodClass>();
						methods.AddRange(lst);
						ClassPointer cp = this.BaseClassPointer;
						while (cp != null)
						{
							List<MethodClass> methods0 = cp.GetCurrentLevelCustomMethods();
							List<MethodClass> fromBase = new List<MethodClass>();
							foreach (MethodClass m in methods0)
							{
								bool exists = false;
								string sm = m.GetMethodSignature();
								foreach (MethodClass m0 in methods)
								{
									string sm0 = m0.GetMethodSignature();
									if (string.Compare(sm, sm0, StringComparison.Ordinal) == 0)
									{
										exists = true;
										break;
									}
									MethodOverride mov = m0 as MethodOverride;
									if (mov != null)
									{
										if (mov.BaseClassId == m.ClassId && mov.BaseMethodId == m.MemberId)
										{
											exists = true;
											mov.SetMembers(m);
											break;
										}
									}
								}
								if (!exists)
								{
									MethodClassInherited mi = m.CreateInherited(this);
									fromBase.Add(mi);
								}
							}
							methods.AddRange(fromBase);
							cp = cp.BaseClassPointer;
						}
						_wholeMethodList = methods;
					}
					catch (Exception e)
					{
						DesignUtil.WriteToOutputWindowAndLog("Error getting customer methods. {0}", DesignerException.FormExceptionText(e));
					}
					bCyclicCheck_getCustomMethods = false;
				}
			}
			return _wholeMethodList;
		}
		private PropertyClassInherited createInheritedProperty(UInt32 memberId)
		{
			PropertyInfo[] pifs = BaseClassType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			for (int i = 0; i < pifs.Length; i++)
			{
				if (memberId == (UInt32)(pifs[i].Name.GetHashCode()))
				{
					return createInheritedProperty(pifs[i]);
				}
			}
			return null;
		}
		private PropertyClassInherited createInheritedProperty(string name)
		{
			PropertyInfo pif = BaseClassType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (pif == null)
			{
				throw new DesignerException("Base property [{0}] not found in type [{1}]", name, BaseClassType.FullName);
			}
			return createInheritedProperty(pif);
		}
		private PropertyClassInherited createInheritedProperty(PropertyInfo pif)
		{
			PropertyClassInherited prop = new PropertyClassInherited(this);
			prop.PropertyType = new DataTypePointer(new TypePointer(pif.PropertyType));
			prop.Name = pif.Name;
			prop.CanRead = pif.CanRead;
			prop.CanWrite = pif.CanWrite;
			prop.ShiftOwnerDeclarer(this);
			prop.IsAbstract = VPLUtil.IsAbstract(pif);
			prop.AccessControl = TypeUtil.GetAccessControl(pif);
			return prop;
		}
		private void adjustMethodName(MethodClass mc)
		{
			XmlNode node = XmlData.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}[@{2}='{3}']/{4}[@{5}='Name']",
				XmlTags.XML_METHODS, XmlTags.XML_METHOD, XmlTags.XMLATT_MethodID, mc.MethodID, XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
			if (node != null)
			{
				node.InnerText = mc.MethodName;
				XmlUtil.SetNameAttribute(node.ParentNode, mc.MethodName);
			}
			ILimnorDesignPane pane = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
			if (pane != null)
			{
				pane.OnMethodChanged(mc, false);
			}
		}
		public ILimnorDesignerLoader GetDesignerLoader()
		{
			ILimnorDesignPane pane = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
			if (pane != null)
			{
				return pane.Loader;
			}
			return null;
		}
		private ILimnorDesignPane getDesigner(string context)
		{
			ILimnorDesignPane pane = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
			if (pane == null)
			{
#if DEBUG
				MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "DEBUG: Calling {0} out of design context. Class ID:{1}. Please close the design pane and re-open it.", context, ClassId), "Design", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
			}
			else
			{
				if (pane.Loader.Node != this.XmlData)
				{
					throw new DesignerException("Calling {0} out of sync in Xml data. Class ID:{1}. Please close the design pane and re-open it.", context, ClassId);
				}
			}
			return pane;
		}
		private void loadIcon()
		{

			if (_icon == null)
			{
				string filename = FullIconPath;
				if (!string.IsNullOrEmpty(filename))
				{
					if (System.IO.File.Exists(filename))
					{
						if (string.Compare(System.IO.Path.GetExtension(filename), ".ico", StringComparison.OrdinalIgnoreCase) == 0)
						{
							Icon ic = Icon.ExtractAssociatedIcon(filename);
							_icon = Bitmap.FromHicon(ic.Handle);
							Form f = this.ObjectInstance as Form;
							if (f != null)
							{
								f.Icon = ic;
							}
						}
						else
						{
							_icon = (Bitmap)System.Drawing.Bitmap.FromFile(filename);
						}
					}
				}
			}
			if (_icon == null)
			{
				if (IsWebService)
				{
					_icon = Resources._earth.ToBitmap();
				}
				else
				{
					Type t = ObjectType;
					if (t != null)
					{
						_icon = VPL.VPLUtil.GetTypeIcon(t);
					}
				}
			}
			if (_icon == null)
			{
				Form f = ObjectInstance as Form;
				if (f != null && f.Icon != null)
				{
					_icon = f.Icon.ToBitmap();

				}
			}
			if (_icon == null)
			{
				_icon = Resources.defObjectIcon;
			}
		}
		public List<MethodClass> GetCurrentLevelCustomMethods()
		{
			if (_currentLevelMethodList == null)
			{
				List<MethodClass> methodList = new List<MethodClass>();
				XmlObjectReader xr = _objectList.Reader;
				Guid g = Guid.NewGuid();
				xr.ClearDelayedInitializers(g);
				XmlNodeList nodes = SerializeUtil.GetCustomMethodNodeList(XmlData);
				foreach (XmlNode nd in nodes)
				{
					MethodClass m = xr.ReadObject<MethodClass>(nd, this);
					methodList.Add(m);
				}
				if (xr.HasErrors)
				{
					MathNode.Log(xr.Errors);
				}
				_currentLevelMethodList = methodList;
				xr.OnDelayedInitializeObjects(g);
			}
			return _currentLevelMethodList;
		}

		private void saveNewProperty(PropertyClass prop)
		{
			XmlObjectWriter xw = new XmlObjectWriter(_objectList);
			prop.SetHolder(this);
			//=======================================================================
			if (prop.CanRead)
			{
				//getter: contains one action ActionExecMath, return valueOfProperty
				GetterClass gc = new GetterClass(prop);
				gc.MemberId = (UInt32)Guid.NewGuid().GetHashCode();
				gc.MethodName = "Get" + prop.Name;
				gc.Description = "This is a method to return the value of the property when this property is used as a parameter in actions.";
				prop.Getter = gc;
				BranchList bl = new BranchList(gc);
				gc.ActionList = bl;
				//
				ActionClass a1 = new ActionClass(this);
				a1.SetScopeMethod(gc);
				a1.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
				a1.ActionName = "return the property value";
				MethodReturnMethod mrm = new MethodReturnMethod(a1);
				a1.ActionMethod = mrm;
				a1.ScopeMethod = gc;
				a1.ScopeMethodId = gc.MethodID;
				a1.ActionHolder = gc;
				a1.AsLocal = true;
				ParameterValue pv = new ParameterValue(a1);
				CustomPropertyPointer cpp = prop.CreatePointer();
				pv.Name = cpp.ToString();
				pv.ValueType = EnumValueType.Property;
				pv.SetDataType(prop.PropertyType);
				pv.SetValue(cpp);
				pv.ScopeMethod = gc;
				a1.SetParameterValue("value", pv);
				//
				AB_MethodReturn sa = new AB_MethodReturn(gc);
				sa.ActionId = new TaskID(a1.WholeActionId);
				sa.ActionData = a1;
				sa.Location = new Point(100, 100);
				sa.Size = new Size(187, 39);//(32, 32);
				sa.IconLayout = EnumIconDrawType.Left;
				ActionPortIn api = new ActionPortIn(sa);
				api.Location = new Point(sa.Location.X + sa.Size.Width / 2 - ActionPortIn.dotSize / 2, sa.Location.Y - ActionPortIn.dotSize);
				api.Position = sa.Size.Width / 2 - ActionPortIn.dotSize / 2;
				api.SaveLocation();
				sa.InPortList = new List<ActionPortIn>();
				sa.InPortList.Add(api);
				bl.Add(sa);
				//
				SaveAction(a1, xw);// pane.Loader.Writer);
			}
			//============================================================
			if (prop.CanWrite)
			{
				//setter: contains one action: valueOfProperty = value
				SetterClass sc = new SetterClass(prop);
				sc.MethodName = "Set" + prop.Name;
				sc.MemberId = (UInt32)Guid.NewGuid().GetHashCode();
				sc.Description = "This is the method passing the value to the property when SetProperty actions is executed";
				prop.Setter = sc;
				//create one action: valueOfProperty = value

				PropertyValueClass pvc = new PropertyValueClass(sc);
				BranchList bl = new BranchList(sc);
				sc.ActionList = bl;
				ActionClass aa = new ActionClass(this);
				aa.SetScopeMethod(sc);
				aa.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
				SetterPointer cs = prop.CreateSetterMethodPointer(aa);
				aa.ActionMethod = cs;
				aa.ActionName = "set the property value";
				aa.ActionHolder = sc;
				aa.AsLocal = true;
				aa.SetParameterValue("value", pvc);
				//
				AB_SingleAction sa2 = new AB_SingleAction(sc);
				sa2.ActionId = new TaskID(aa.WholeActionId);
				sa2.ActionData = aa;
				sa2.Location = new Point(100, 100);
				sa2.Size = new Size(230, 30);//(32, 32);
				ActionPortIn api = new ActionPortIn(sa2);
				api.Location = new Point(sa2.Location.X + sa2.Size.Width / 2 - ActionPortIn.dotSize / 2, sa2.Location.Y - ActionPortIn.dotSize);
				api.Position = sa2.Size.Width / 2 - ActionPortIn.dotSize / 2;
				api.SaveLocation();
				sa2.InPortList = new List<ActionPortIn>();
				sa2.InPortList.Add(api);
				ActionPortOut apo = new ActionPortOut(sa2);
				apo.Location = new Point(sa2.Location.X + sa2.Size.Width / 2 - ActionPortIn.dotSize / 2, sa2.Location.Y + sa2.Size.Height);
				apo.Position = sa2.Size.Width / 2 - ActionPortIn.dotSize / 2;
				apo.SaveLocation();
				sa2.OutPortList = new List<ActionPortOut>();
				sa2.OutPortList.Add(apo);
				bl.Add(sa2);
				//
				SaveAction(aa, xw);
			}
			//
			XmlNode node = SerializeUtil.CreateCustomPropertyNode(this.XmlData, prop.MemberId);
			// 
			xw.WriteObjectToNode(node, prop);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_New, true);
			//
			PropertyOverride po = prop as PropertyOverride;
			if (_customPropertyList != null)
			{
				if (po != null)
				{
					_customPropertyList.Remove(prop.Name);
					_customPropertyList.Add(prop.Name, prop);
					_propertyOverrides = null;
				}
				else
				{
					_customPropertyList.Add(prop.Name, prop);
				}
			}
			if (_currentLevelPropertyList != null)
			{
				_currentLevelPropertyList.Add(prop);
			}
			if (_wholePropertyList != null)
			{
				PropertyClass p0 = null;
				if (po != null)
				{
					foreach (PropertyClass p in _wholePropertyList)
					{
						if (po.BaseClassId == p.ClassId)
						{
							if (po.BasePropertyId == p.MemberId)
							{
								p0 = p;
								break;
							}
						}
					}
					if (p0 != null)
					{
						_wholePropertyList.Remove(p0);
					}
				}
				_wholePropertyList.Add(prop);
			}
			_customProperties = null;
			_propertyOverrides = null;
			//
			ILimnorDesignPane pane = Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
			if (pane != null)
			{
				pane.Loader.NotifyChanges();
				pane.OnAddProperty(prop);
			}
			//
			if (po != null)
			{
			}
			else
			{
				Dictionary<UInt32, ILimnorDesignPane> designPanes = Project.GetTypedDataList<ILimnorDesignPane>();
				foreach (ILimnorDesignPane pn in designPanes.Values)
				{
					if (pn.Loader.ClassID != this.ClassId)
					{
						pn.OnAddProperty(prop);
					}
				}
			}
			//reset dialogues
			FrmObjectExplorer.RemoveDialogCaches(this.Project.ProjectGuid, this.ClassId);
		}
		#endregion
		#region ICloneable Members
		/// <summary>
		/// variable name
		/// </summary>
		[Browsable(false)]
		public string CodeName
		{
			get
			{
				string s = Name;
				if (!string.IsNullOrEmpty(s))
				{
					int n = s.IndexOf('.');
					if (n > 0)
					{
						return s.Substring(0, n);
					}
				}
				return s;
			}
		}
		/// <summary>
		/// fully qualified variable name
		/// </summary>
		[Browsable(false)]
		public virtual string ReferenceName
		{
			get
			{
				return CodeName;
			}
		}
		[Browsable(false)]
		public virtual void Copy(ClassPointer obj)
		{
			this._wholeId = obj._wholeId;
			this._icon = obj._icon;
			this._isStatic = obj._isStatic;
			this._objectList = obj._objectList;
			this.AlwaysStatic = obj.AlwaysStatic;
		}
		[Browsable(false)]
		public virtual object Clone()
		{
			return this; //only keep one copy in memory
		}

		#endregion
		#region IObjectPointer Members
		public EnumWebRunAt RunAt
		{
			get
			{
				if (Project.ProjectType == EnumProjectType.WebAppAspx || Project.ProjectType == EnumProjectType.WebAppPhp)
				{
					return EnumWebRunAt.Inherit;
				}
				return EnumWebRunAt.Server;
			}
		}
		[Browsable(false)]
		public HtmlElement_Base CurrentHtmlElement
		{
			get
			{
				return _selectedHtmlElement;
			}
		}
		/// <summary>
		/// the class holding (not neccerily declaring) this pointer
		/// </summary>
		[Browsable(false)]
		public ClassPointer RootPointer
		{
			get
			{
				IObjectPointer root = this.Owner;
				while (root != null && root.Owner != null)
				{
					root = root.Owner;
				}
				ClassPointer c = root as ClassPointer;
				if (c != null)
				{
					return c;
				}
				return this;
			}
		}
		[Browsable(false)]
		public ClassPointer RootHost
		{
			get
			{
				return this;
			}
		}
		[Browsable(false)]
		public IClass Host
		{
			get
			{
				return RootPointer;
			}
		}
		[Browsable(false)]
		public bool IsValid
		{
			get
			{
				if (_objectList != null && _wholeId != 0)
				{
					return true;
				}
				MathNode.LastValidationError = string.Format(CultureInfo.InvariantCulture, "Some of (_objectList,_wholeId) are null for [{0}] of [{1}]. ({2},{3})", this.ToString(), this.GetType().Name, _objectList, _wholeId);
				return false;
			}
		}
		public virtual string CreateJavaScript(StringCollection sb)
		{
			return "window";
		}
		public virtual string CreatePhpScript(StringCollection sb)
		{
			return "$this";
		}
		public virtual CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			//
			if (IsStatic)
			{
				return new CodeTypeReferenceExpression(this.TypeString);
			}
			if (method != null)
			{
				MethodClass mc = method as MethodClass;
				if (mc != null)
				{
					ClassPointer root = mc.Owner as ClassPointer;
					if (root != null)
					{
						if (root.ClassId != this.ClassId)
						{
							return new CodeTypeOfExpression(this.TypeString);
						}
					}
					else if (mc.Holder != null)
					{
						root = mc.Holder.RootPointer;
						if (root != null)
						{
							if (root.ClassId != this.ClassId)
							{
								return new CodeTypeOfExpression(this.TypeString);
							}
						}
						else if (mc.Holder.ClassId != this.ClassId)
						{
							return new CodeTypeOfExpression(this.TypeString);
						}
					}
				}
			}
			return new CodeThisReferenceExpression();
		}
		public string GetJavaScriptReferenceCode(StringCollection code)
		{
			return "document.body";
		}
		public string GetPhpScriptReferenceCode(StringCollection code)
		{
			return "$this";
		}
		public void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			IWebClientControl page = this.ObjectInstance as IWebClientControl;
			if (page != null)
			{
				page.CreateActionJavaScript(methodName, code, parameters, returnReceiver);
			}
			else
			{
				if (typeof(WebPage).IsAssignableFrom(this.BaseClassType))
				{
					if (WebPage.CreateActionJavaScriptStatic(this.CodeName, methodName, code, parameters, returnReceiver))
					{
						return;
					}
				}
				WebPageCompilerUtility.CreateActionJavaScript(this.CodeName, methodName, code, parameters, returnReceiver);
			}
		}
		public void CreateActionPhpScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
		}
		[Browsable(false)]
		public virtual string ObjectKey
		{
			get
			{
				return WholeId.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
			}
		}
		[Browsable(false)]
		public virtual bool IsTargeted(EnumObjectSelectType target)
		{
			return (target == EnumObjectSelectType.Object || target == EnumObjectSelectType.All);
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual Type ObjectType
		{
			get
			{
				return _objectList.RootObjectType;
			}
			set
			{
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public object ObjectDebug { get; set; }
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual object ObjectInstance
		{
			get
			{
				object v = _objectList.GetRootObject();
				if (v == null)
				{
					ClassPointer cp = this.Project.GetTypedData<ClassPointer>(this.ClassId);
					if (cp != null && cp != this)
					{
						v = cp.ObjectList.GetRootObject();
					}
				}
				return v;
			}
			set
			{
			}
		}
		[Browsable(false)]
		public virtual bool IsSameObjectRef(IObjectIdentity objectPointer)
		{
			ClassPointer obj = objectPointer as ClassPointer;
			if (obj == null)
			{
				DataTypePointer dp = objectPointer as DataTypePointer;
				if (dp != null)
				{
					obj = dp.ClassTypePointer;
				}
			}
			if (obj != null)
			{
				return (this.WholeId == obj.WholeId);
			}
			return false;
		}
		[Browsable(false)]
		public bool IsSameProperty(IPropertyPointer p)
		{
			IObjectIdentity objectPointer = p as IObjectIdentity;
			if (objectPointer != null)
			{
				return IsSameObjectRef(objectPointer);
			}
			return false;
		}
		[Browsable(false)]
		public virtual string DisplayName
		{
			get
			{
				string n = Name;
				if (string.IsNullOrEmpty(n))
				{
					n = DesignUtil.GetObjectNameById(MemberId, _objectList.XmlData);
					if (string.IsNullOrEmpty(n))
					{
						n = "?";
					}
				}
				if (_baseClassPointer != null)
				{
					return n + " from " + _baseClassPointer.Name;
				}
				else
				{
					return n + " from " + VPL.VPLUtil.GetObjectType(ObjectType).Name;
				}
			}
		}
		[Browsable(false)]
		public virtual string LongDisplayName
		{
			get
			{
				string n = Name;
				if (string.IsNullOrEmpty(n))
				{
					n = DesignUtil.GetObjectNameById(MemberId, _objectList.XmlData);
					if (string.IsNullOrEmpty(n))
					{
						n = "?";
					}
				}
				if (_baseClassPointer != null)
				{
					return n + " : " + _baseClassPointer.Name;
				}
				else
				{
					return n + " : " + VPL.VPLUtil.GetObjectType(ObjectType).Name;
				}
			}
		}
		[Browsable(false)]
		public string ExpressionDisplay
		{
			get
			{
				return CodeName;
			}
		}
		[Browsable(false)]
		public EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public EnumPointerType PointerType { get { return EnumPointerType.Class; } }
		#endregion
		#region ISerializerProcessor Members
		/// <summary>
		/// only ClassId and MemberId are saved.
		/// This object may appear in other classes as the declaring class in hosted components;
		/// or as a property type definition (DataTypePointer). In such cases the objectNode is not the 
		/// XmlNode for this class. 
		/// after reading ClassId and MemberId we need to reconstruct the other properties
		/// from the XmlNode and the ObjectIDmap.
		/// </summary>
		/// <param name="objMap">the root object containing this pointer</param>
		/// <param name="objectNode">XmlNode for this pointer</param>
		[Browsable(false)]
		public virtual void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
			{
			}
			else
			{
				if (IsInterface)
				{
					if (ObjectInstance == null)
					{
						this._objectList.LoadObjects();
						if (ObjectInstance == null)
						{
							//not loaded from ReadRootObject
							throw new DesignerException("Instance is null after reading, class id: {0}", ClassId);
						}
					}
					InterfaceClass ifc = VPLUtil.GetObject(ObjectInstance) as InterfaceClass;
					if (ifc == null)
					{
						throw new DesignerException("Invalid interface class with id {0}", ClassId);
					}
					ifc.Holder = this;
					ifc.ClassId = this.ClassId;
					ifc.InterfaceName = this.Name;
					if (ifc.Properties != null)
					{
						foreach (InterfaceElementProperty p in ifc.Properties)
						{
							p.SetOwner(new InterfacePointer(ifc));
						}
					}
					if (ifc.Methods != null)
					{
						foreach (InterfaceElementMethod m in ifc.Methods)
						{
							m.SetOwner(new InterfacePointer(ifc));
						}
					}
					if (ifc.Events != null)
					{
						foreach (InterfaceElementEvent e in ifc.Events)
						{
							e.SetOwner(new InterfacePointer(ifc));
						}
					}
				}
			}
		}

		#endregion
		#region IClass Members
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual Image ImageIcon
		{
			get
			{
				if (_icon == null)
				{
					loadIcon();
				}
				return _icon;
			}
			set
			{
				_icon = value;
			}
		}

		[Browsable(false)]
		public Type VariableLibType
		{
			get { return null; }
		}
		[Browsable(false)]
		public ClassPointer VariableCustomType
		{
			get { return this; }
		}
		[Browsable(false)]
		public IClassWrapper VariableWrapperType
		{
			get { return null; }
		}
		#endregion
		#region IWithProject Members
		[Browsable(false)]
		public LimnorProject Project
		{
			get
			{
				if (_objectList != null)
				{
					return _objectList.Project;
				}
				return null;
			}
		}

		#endregion
		#region Custom property values
		private PropertyDescriptorCollection _customProperties;
		[Browsable(false)]
		public PropertyDescriptorCollection CustomPropertyCollection
		{
			get
			{
				if (_customProperties == null)
				{
					List<PropertyDescriptor> list = new List<PropertyDescriptor>();
					Dictionary<string, PropertyClass> props = CustomProperties;
					foreach (PropertyClass p in props.Values)
					{
						p.SetHolder(this);
						list.Add(new PropertyClassDescriptor(p));
					}
					_customProperties = new PropertyDescriptorCollection(list.ToArray());
				}
				return _customProperties;
			}
		}
		#endregion
		#region IXmlNodeSerializable Members

		public virtual void OnWriteToXmlNode(IXmlCodeWriter writer, XmlNode node)
		{
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ClassID, ClassId);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ComponentID, MemberId);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_BaseClassId, _baseClassId);
		}

		public virtual void OnReadFromXmlNode(IXmlCodeReader reader0, XmlNode node)
		{
			XmlObjectReader reader = (XmlObjectReader)reader0;
			ClassId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
			MemberId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
			if (ClassId == 0 || MemberId == 0)
			{
				throw new DesignerException("Invalid RootClassId [{0},{1}]", ClassId, MemberId);
			}
			reader.ObjectList.Project.SetTypedData<ClassPointer>(ClassId, this);
			if (_objectList == null)
			{
				throw new DesignerException("Map not found when reading ClassPointer [{0}]", ClassId);
			}
			Type classtype = _objectList.RootObjectType;
			if (DesignUtil.IsApp(classtype))
			{
				IsStatic = true;
				AlwaysStatic = true;
			}
			_baseClassId = XmlUtil.GetAttributeUInt(_objectList.XmlData, XmlTags.XMLATT_BaseClassId);

			//
			if (_baseClassId != 0)
			{
				List<uint> derivedClassIdList = new List<uint>();
				derivedClassIdList.Add(ClassId);
				_baseClassPointer = ClassPointer.CreateClassPointer(_baseClassId, _objectList.Project, derivedClassIdList);
			}
		}

		#endregion
		#region ICustomTypeDescriptor Members

		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}

		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}

		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(this.ObjectInstance, attributes, false);
			if (CustomPropertyCollection.Count > 0)
			{
				List<PropertyDescriptor> props = new List<PropertyDescriptor>();
				for (int i = 0; i < CustomPropertyCollection.Count; i++)
				{
					props.Add(CustomPropertyCollection[i]);
				}
				for (int i = 0; i < ps.Count; i++)
				{
					bool b = false;
					for (int k = 0; k < CustomPropertyCollection.Count; k++)
					{
						if (ps[i].Name == CustomPropertyCollection[k].Name)
						{
							b = true;
							break;
						}
					}
					if (!b)
					{
						props.Add(ps[i]);
					}
				}
				return new PropertyDescriptorCollection(props.ToArray());
			}
			else
			{
				return ps;
			}
		}

		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}

		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			if (pd is PropertyClassDescriptor)
				return this;
			else
				return ObjectInstance;
		}
		public List<ObjectTextID> GetMethodUsages(MethodClass method)
		{
			string sig = method.GetMethodSignature();
			List<ObjectTextID> usages = new List<ObjectTextID>();
			List<InterfacePointer> list = GetAddedInterfaces();
			foreach (InterfacePointer ip in list)
			{
				List<InterfaceElementMethod> ms = ip.Methods;
				foreach (InterfaceElementMethod m in ms)
				{
					if (string.Compare(sig, m.GetMethodSignature(), StringComparison.Ordinal) == 0)
					{
						usages.Add(new ObjectTextID(ip.Name, ip.GetType().FullName, "interface"));
						break;
					}
				}
			}
			//
			string path;
			if (!method.IsFinal)
			{
				//find out the overrides
				List<XmlNode> rootNodeList = DesignUtil.GetAllComponentXmlNode(Project);
				foreach (XmlNode nd in rootNodeList)
				{
					string sClass = XmlUtil.GetNameAttribute(nd);
					path = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}/{2}[@{3}='BaseClassId' and text()='{4}']/../{2}[@{3}='BaseMethodId' and text()='{5}']",
						XmlTags.XML_METHODS, XmlTags.XML_METHOD, XmlTags.XML_PROPERTY,
						XmlTags.XMLATT_NAME, method.ClassId, method.MemberId);
					XmlNodeList overrideList = nd.SelectNodes(path);
					foreach (XmlNode mNode in overrideList)
					{
						string sName = XmlUtil.GetNameAttribute(mNode.ParentNode);
						usages.Add(new ObjectTextID(sClass, "override method", sName));
					}
					path = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}/{2}/{3}[@{4}='{5}' and @{6}='{7}']",
						XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XML_PROPERTY,
						XmlTags.XML_Data, XmlTags.XMLATT_ClassID,
						method.ClassId, XmlTags.XMLATT_MethodID, method.MemberId);
					XmlNodeList actList = nd.SelectNodes(path);
					foreach (XmlNode mNode in actList)
					{
						string sName = XmlUtil.GetNameAttribute(mNode.ParentNode.ParentNode);
						usages.Add(new ObjectTextID(sClass, "action", sName));
					}
				}
			}

			return usages;
		}
		public List<ObjectTextID> GetPropertyUsages(PropertyClass property)
		{
			string sig = property.Name;
			List<ObjectTextID> usages = new List<ObjectTextID>();
			List<InterfacePointer> list = GetAddedInterfaces();
			foreach (InterfacePointer ip in list)
			{
				List<InterfaceElementProperty> ps = ip.GetProperties();
				foreach (InterfaceElementProperty p in ps)
				{
					if (string.Compare(p.Name, sig, StringComparison.Ordinal) == 0)
					{
						usages.Add(new ObjectTextID(ip.Name, ip.GetType().FullName, "interface"));
						break;
					}
				}
			}
			if (!property.IsOverride)
			{
				//find out the overrides
				List<XmlNode> rootNodeList = DesignUtil.GetAllComponentXmlNode(Project);
				foreach (XmlNode nd in rootNodeList)
				{
					string path = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}/{2}[@{3}='BaseClassId' and text()='{4}']/../{2}[@{3}='BasePropertyId' and text()='{5}']",
						XmlTags.XML_PROPERTYLIST, XmlTags.XML_Item, XmlTags.XML_PROPERTY,
						XmlTags.XMLATT_NAME, property.ClassId, property.MemberId);
					XmlNodeList overrideList = nd.SelectNodes(path);
					foreach (XmlNode mNode in overrideList)
					{
						string sClass = XmlUtil.GetNameAttribute(mNode.OwnerDocument.DocumentElement);
						usages.Add(new ObjectTextID(sClass, "override property", property.Name));
					}
				}
			}
			return usages;
		}
		public List<ObjectTextID> GetEventUsages(EventClass e)
		{
			string sig = e.Name;
			List<ObjectTextID> usages = new List<ObjectTextID>();
			List<InterfacePointer> list = GetAddedInterfaces();
			foreach (InterfacePointer ip in list)
			{
				List<InterfaceElementEvent> es = ip.Events;
				foreach (InterfaceElementEvent ev in es)
				{
					if (string.Compare(ev.Name, sig, StringComparison.Ordinal) == 0)
					{
						usages.Add(new ObjectTextID(ip.Name, ip.GetType().FullName, "interface"));
						break;
					}
				}
			}
			List<XmlNode> rootNodeList = DesignUtil.GetAllComponentXmlNode(Project);
			foreach (XmlNode nd in rootNodeList)
			{
				//HandlerList/Handler/Property/Data[ClassID={} and eventId={}]
				XmlNode handlerNode = nd.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}/{2}/{3}[@{4}='{5}' and @{6}='{7}']",
					XmlTags.XML_HANDLERLISTS,
					XmlTags.XML_HANDLER,
					XmlTags.XML_PROPERTY,
					XmlTags.XML_Data,
					XmlTags.XMLATT_ClassID,
					ClassId,
					XmlTags.XMLATT_EventId,
					e.MemberId));
				if (handlerNode != null)
				{
					usages.Add(new ObjectTextID("Event handler", XmlUtil.GetAttribute(nd, XmlTags.XMLATT_type), XmlUtil.GetNameAttribute(nd)));
				}
			}
			return usages;
		}
		/// <summary>
		/// returns a PropertyClass or a PropertyInfo if it is implemented;
		/// returns null if it is not implemented
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public object InterfacePropertyImplemented(InterfaceElementProperty property)
		{
			List<PropertyClass> props = GetCurrentLevelCustomProperties();
			foreach (PropertyClass p in props)
			{
				if (p.Name == property.Name)
				{
					if (p.PropertyType.IsSameObjectRef(property.DataType))
					{
						return p;
					}
				}
			}
			if (BaseClassPointer != null)
			{
				return BaseClassPointer.InterfacePropertyImplemented(property);
			}
			if (property.IsLibType)
			{
				PropertyInfo pif = this.BaseClassType.GetProperty(property.Name);
				if (pif != null && pif.PropertyType.Equals(property.VariableLibType))
				{
					return pif;
				}
			}
			return null;
		}
		public bool MethodSignatureExists(string signature)
		{
			Dictionary<string, MethodClass> ms = CustomMethods;
			return ms.ContainsKey(signature);
		}
		public bool EventExists(string name)
		{
			if (BaseClassPointer != null)
			{
				if (BaseClassPointer.EventExists(name))
				{
					return true;
				}
			}
			XmlNode node = SerializeUtil.GetCustomEventNodeByName(_objectList.XmlData, name);
			if (node != null)
			{
				return true;
			}
			if (BaseClassPointer == null)
			{
				EventInfo eif = this.BaseClassType.GetEvent(name);
				if (eif != null)
				{
					return true;
				}
			}
			return false;
		}
		#endregion
		#region Interfaces
		public bool RemoveInterface(InterfacePointer ip, ILimnorDesignerLoader loader)
		{
			XmlNode node = null;
			XmlObjectReader xr = _objectList.Reader;
			XmlNodeList nodes = SerializeUtil.GetInterfaceNodeList(_objectList.XmlData);
			foreach (XmlNode nd in nodes)
			{
				InterfacePointer a = xr.ReadObject<InterfacePointer>(nd, this);
				a.ImplementerClassId = this.ClassId;
				if (ip.IsSameObjectRef(a))
				{
					node = nd;
					break;
				}
			}
			if (node != null)
			{
				XmlNode p = node.ParentNode;
				p.RemoveChild(node);
				loader.NotifyChanges();
				loader.DesignPane.OnRemoveInterface(ip);
				return true;
			}
			return false;
		}
		public bool AddInterface(InterfacePointer ip)
		{
			List<InterfacePointer> ips = GetInterfaces();
			foreach (InterfacePointer ifp in ips)
			{
				if (ifp.IsSameObjectRef(ip))
				{
					return false; //already implemented
				}
			}
			ip.ImplementerClassId = this.ClassId;
			//implement memebers
			//implement properties
			List<InterfaceElementProperty> properties = ip.GetProperties();
			if (properties != null)
			{
				foreach (InterfaceElementProperty p in properties)
				{
					implementProperty(p);
				}
			}
			//implement methods
			List<InterfaceElementMethod> methods = ip.Methods;
			if (methods != null)
			{
				foreach (InterfaceElementMethod m in methods)
				{
					implementMethod(m);
				}
			}
			//implement events
			List<InterfaceElementEvent> events = ip.Events;
			if (events != null)
			{
				foreach (InterfaceElementEvent e in events)
				{
					implementEvent(e);
				}
			}
			//
			XmlNode node = SerializeUtil.AddInterfaceNode(_objectList.XmlData);
			XmlObjectWriter xw = new XmlObjectWriter(this._objectList);
			xw.ClearErrors();
			xw.WriteObjectToNode(node, ip);
			//
			ILimnorDesignerLoader loader = GetDesignerLoader();
			if (loader != null)
			{
				loader.NotifyChanges();
				loader.DesignPane.OnInterfaceAdded(ip);
			}
			//
			return true;
		}
		public List<InterfacePointer> GetAddedInterfaces()
		{
			List<InterfacePointer> list = new List<InterfacePointer>();
			XmlObjectReader xr = _objectList.Reader;
			XmlNodeList nodes = SerializeUtil.GetInterfaceNodeList(_objectList.XmlData);
			foreach (XmlNode nd in nodes)
			{
				InterfacePointer a = xr.ReadObject<InterfacePointer>(nd, this);
				a.ImplementerClassId = this.ClassId;
				list.Add(a);
			}
			if (xr.HasErrors)
			{
				MathNode.Log(xr.Errors);
			}
			return list;
		}
		public IList<InterfacePointer> GetAddedInterfacesFromAllLevels()
		{
			List<InterfacePointer> list = GetAddedInterfaces();
			if (_baseClassPointer != null)
			{
				List<InterfacePointer> baseList = _baseClassPointer.GetInterfaces();
				list.AddRange(baseList);
			}
			return list;
		}
		/// <summary>
		/// get all interfaces implemented by this class
		/// </summary>
		/// <returns></returns>
		public List<InterfacePointer> GetInterfaces()
		{
			List<InterfacePointer> list = GetAddedInterfaces();
			if (_baseClassPointer != null)
			{
				List<InterfacePointer> baseList = _baseClassPointer.GetInterfaces();
				list.AddRange(baseList);
			}
			else
			{
				Type t = this.BaseClassType;
				Type[] ts = t.GetInterfaces();
				if (ts != null && ts.Length > 0)
				{
					for (int i = 0; i < ts.Length; i++)
					{
						if (ts[i].IsPublic)
						{
							InterfacePointer ip = new InterfacePointer(new TypePointer(ts[i]));
							list.Add(ip);
						}
					}
				}
			}
			return list;
		}
		#endregion
		#region IAttributeHolder Members

		public void AddAttribute(ConstObjectPointer attr)
		{
			XmlNode node = SerializeUtil.CreateAttributeNode(_objectList.XmlData, attr.ValueId);
			XmlObjectWriter wr = new XmlObjectWriter(_objectList);
			wr.WriteObjectToNode(node, attr);
		}

		public void RemoveAttribute(ConstObjectPointer attr)
		{
			SerializeUtil.RemoveAttributeNode(_objectList.XmlData, attr.ValueId);
		}
		public List<ConstObjectPointer> GetCustomAttributeList()
		{
			return GetCustomAttributeList(_objectList.Reader, _objectList.XmlData, this);
		}
		public static List<ConstObjectPointer> GetCustomAttributeList(XmlObjectReader xr, XmlNode node, object parentObject)
		{
			List<ConstObjectPointer> attributes = new List<ConstObjectPointer>();
			XmlNodeList nodes = SerializeUtil.GetAttributeNodeList(node);
			foreach (XmlNode nd in nodes)
			{
				ConstObjectPointer a = xr.ReadObject<ConstObjectPointer>(nd, parentObject);
				attributes.Add(a);
			}
			if (xr.HasErrors)
			{
				MathNode.Log(xr.Errors);
			}
			return attributes;
		}
		#endregion
		#region IWithCollection Members

		public void OnCollectionChanged(string propertyName)
		{
			DrawingPage dp = _objectList.GetRootObject() as DrawingPage;
			if (dp != null)
			{
			}
			ILimnorDesignPane pane = getDesigner("OnCollectionChanged");
			if (pane != null)
			{
				pane.Loader.NotifyChanges();
			}
		}
		[Browsable(false)]
		public void OnItemCreated(string propertyName, object obj)
		{
		}

		#endregion
		#region IDrawingOwner Members

		public void OnDrawingCollectionChanged(DrawingLayer layer)
		{
			ILimnorDesignPane pane = _objectList.GetTypedData<ILimnorDesignPane>();
			if (pane != null)
			{
				List<DrawingItem> newDrawings = new List<DrawingItem>();
				List<DrawingItem> createdDrawings = new List<DrawingItem>();
				StringCollection names = new StringCollection();
				foreach (DrawingItem item in layer)
				{
					if (!_objectList.ContainsKey(item))
					{
						newDrawings.Add(item);

					}
				}
				foreach (DrawingItem item in newDrawings)
				{
					layer.Remove(item);
					Type t = item.GetType();
					string name = pane.Loader.CreateNewComponentName(t.Name, names);
					DrawingItem newObj = pane.Loader.CreateComponent(t, name) as DrawingItem;
					names.Add(name);
					createdDrawings.Add(newObj);
					newObj.Copy(item);
				}
				foreach (DrawingItem item in createdDrawings)
				{
					if (item.LayerId != layer.LayerId)
					{
						DrawingLayer l = layer.Page.GetLayerById(item.LayerId);
						if (l != null)
						{
							if (l.Contains(item))
							{
								l.Remove(item);
							}
						}
						layer.AddDrawing(item);
					}
				}
				pane.Loader.NotifyChanges();
			}
		}

		#endregion
		#region IDrawingPageHolder Members

		public IDrawingPage GetDrawingPage()
		{
			return _objectList.GetRootObject() as IDrawingPage;
		}

		#endregion
		#region Resource map
		public Dictionary<IProperty, UInt32> ResourceMap
		{
			get
			{
				if (_resourceMap == null)
				{
					XmlNodeList ns = XmlData.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}",
						XmlTags.XML_ResourceMap, XmlTags.XML_Item));
					_resourceMap = new Dictionary<IProperty, UInt32>();
					XmlObjectReader xr = new XmlObjectReader(this.ObjectList);
					foreach (XmlNode nd in ns)
					{
						IProperty p = (IProperty)xr.ReadObject(nd, null);
						UInt32 id = XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_resId);
						_resourceMap.Add(p, id);
					}

				}
				return _resourceMap;
			}
		}
		public void SetResourceMap(Dictionary<IProperty, UInt32> map)
		{
			_resourceMap = map;
			XmlNode rn = XmlUtil.CreateSingleNewElement(XmlData, XmlTags.XML_ResourceMap);
			rn.RemoveAll();
			if (_resourceMap != null)
			{
				XmlObjectWriter xw = new XmlObjectWriter(this.ObjectList);
				Dictionary<IProperty, UInt32>.Enumerator en = _resourceMap.GetEnumerator();
				while (en.MoveNext())
				{
					XmlNode nd = rn.OwnerDocument.CreateElement(XmlTags.XML_Item);
					rn.AppendChild(nd);
					xw.WriteObjectToNode(nd, en.Current.Key);
					XmlUtil.SetAttribute(nd, XmlTags.XMLATT_resId, en.Current.Value);
				}
			}
		}
		public void MapResources(Form caller)
		{
			DlgMapResources dlg = new DlgMapResources();
			dlg.LoadData(this);
			if (dlg.ShowDialog(caller) == DialogResult.OK)
			{
				ILimnorDesignPane designer = this.Project.GetTypedData<ILimnorDesignPane>(this.ClassId);
				if (designer != null)
				{
					designer.Loader.NotifyChanges();

				}
				ProjectResources rm = Project.GetProjectSingleData<ProjectResources>();
				rm.NotifyLanguageChange();
			}
		}

		public void OnPropertyChanged(object v, string propertyname, object value)
		{
			Dictionary<IProperty, UInt32> map = this.ResourceMap;
			if (map != null && map.Count > 0)
			{
				PropertyPointer pp = null;
				if (v == this)
				{
					pp = new PropertyPointer();
					pp.Owner = this;
					pp.MemberName = propertyname;
				}
				else
				{
					UInt32 memId = _objectList.GetObjectID(v);
					if (memId != 0)
					{
						MemberComponentId mem = new MemberComponentId(this, v, memId);
						pp = new PropertyPointer();
						pp.Owner = mem;
						pp.MemberName = propertyname;
					}
				}
				if (pp != null)
				{
					Dictionary<IProperty, UInt32>.Enumerator en = map.GetEnumerator();
					while (en.MoveNext())
					{
						if (pp.IsSameObjectRef(en.Current.Key))
						{
							ProjectResources rm = Project.GetProjectSingleData<ProjectResources>();
							rm.SetResourceValue(en.Current.Value, value);
							break;
						}
					}
				}
			}
		}
		public void OnCultureChanged()
		{
			Dictionary<IProperty, UInt32> map = this.ResourceMap;
			if (map != null && map.Count > 0)
			{
				ProjectResources rm = Project.GetProjectSingleData<ProjectResources>();
				Dictionary<IProperty, UInt32>.Enumerator en = map.GetEnumerator();
				while (en.MoveNext())
				{
					ResourcePointer rp = rm.GetResourcePointerById(en.Current.Value);
					if (rp != null)
					{
						en.Current.Key.SetValue(rp.GetResourceValue(rm.DesignerLanguageName));
					}
				}
			}
		}
		#endregion
		#region IClassContainer Members

		public ClassPointer Definition
		{
			get { return this; }
		}
		public object GetOwnerMethod(UInt32 methodId)
		{
			return this.GetCustomMethodById(methodId);
		}
		#endregion
		#region IActionsHolder Members
		[Browsable(false)]
		public XmlNode ActionsNode
		{
			get
			{
				XmlNode node = this.XmlData.SelectSingleNode(XmlTags.XML_ACTIONS);
				if (node == null)
				{
					node = this.XmlData.OwnerDocument.CreateElement(XmlTags.XML_ACTIONS);
					this.XmlData.AppendChild(node);
				}
				return node;
			}
		}
		[Browsable(false)]
		public UInt32 SubScopeId
		{
			get
			{
				return 0;
			}
		}
		[Browsable(false)]
		public uint ScopeId
		{
			get { return 0; }
		}
		[Browsable(false)]
		public MethodClass OwnerMethod
		{
			get
			{
				return null;
			}
		}
		[Browsable(false)]
		public Dictionary<uint, IAction> ActionInstances
		{
			get { return _actions; }
		}
		public Dictionary<UInt32, IAction> GetVisibleActionInstances()
		{
			if (_actions == null)
			{
				LoadActionInstances();
			}
			return _actions;
		}
		public void SetActionInstances(Dictionary<uint, IAction> actions)
		{
			_actions = actions;
		}
		public void LoadActionInstances()
		{
			LoadActions(this);
		}
		public ActionBranch FindActionBranchById(UInt32 branchId)
		{
			return null;
		}
		public void GetActionNames(StringCollection sc)
		{
			if (_actions == null)
			{
				LoadActionInstances();
			}
			if (_actions != null)
			{
				foreach (IAction a in _actions.Values)
				{
					if (a != null)
					{
						if (!string.IsNullOrEmpty(a.ActionName))
						{
							if (!sc.Contains(a.ActionName))
							{
								sc.Add(a.ActionName);
							}
						}
					}
				}

			}
		}
		#endregion
		#region IDevClass Members
		public void NotifyBeforeChange(object obj, string member)
		{
			if (string.CompareOrdinal(member, "SqlQuery") == 0)
			{
				EasyDataSet eds = obj as EasyDataSet;
				if (eds != null && this.ObjectList != null)
				{
					foreach (KeyValuePair<object, uint> kv in this.ObjectList)
					{
						DataGridViewEx dgve = kv.Key as DataGridViewEx;
						if (dgve != null)
						{
							if (dgve.DataSource == eds)
							{
								dgve.PreserveColumns();
							}
						}
					}
				}
			}
		}
		public void NotifyChange(object obj, string member)
		{
			if (string.CompareOrdinal(member, "SqlQuery") == 0)
			{
				EasyDataSet eds = obj as EasyDataSet;
				if (eds != null && this.ObjectList != null)
				{
					foreach (KeyValuePair<object, uint> kv in this.ObjectList)
					{
						DataGridViewEx dgve = kv.Key as DataGridViewEx;
						if (dgve != null)
						{
							if (dgve.DataSource == eds)
							{
								dgve.DataSource = null;
								dgve.DataSource = eds;
								dgve.DataMember = eds.TableName;
								dgve.RestoreColumns();
							}
						}
					}
				}
			}
			LimnorProject p = Project;
			if (p != null)
			{
				ILimnorDesignPane pn = p.GetTypedData<ILimnorDesignPane>(this.ClassId);
				if (pn != null)
				{
					pn.Loader.NotifyChanges();
					IResetOnComponentChange rocc = obj as IResetOnComponentChange;
					if (rocc != null)
					{
						if (rocc.OnResetDesigner(member))
						{
							pn.OnResetDesigner(rocc);
						}
					}
				}
			}
		}

		#endregion
		#region Dynamic Member Info
		public MethodInfo GetMethodInfo(string name, BindingFlags bindingAttr)
		{
			if (this.IsInterface)
			{
				if (this.ObjectList.Count == 0)
				{
					this.ObjectList.LoadObjects();
				}
				InterfaceClass ifc = this.Interface;
				List<InterfaceElementMethod> ms = ifc.Methods;
				if (ms != null && ms.Count > 0)
				{
					foreach (InterfaceElementMethod im in ms)
					{
						if (string.CompareOrdinal(im.Name, name) == 0)
						{
							return new MethodInfoInterface(im);
						}
					}
				}
			}
			else
			{
				Dictionary<string, MethodClass> ms = this.CustomMethods;
				if (ms != null)
				{
					if ((bindingAttr & BindingFlags.Static) == BindingFlags.Static)
					{
						foreach (KeyValuePair<string, MethodClass> kv in ms)
						{
							if (kv.Value.IsStatic)
							{
								if (string.CompareOrdinal(name, kv.Value.Name) == 0)
								{
									return kv.Value.GetMethodInfoX();
								}
							}
						}
					}
					else
					{
						foreach (KeyValuePair<string, MethodClass> kv in ms)
						{
							if (!kv.Value.IsStatic)
							{
								if (string.CompareOrdinal(name, kv.Value.Name) == 0)
								{
									return kv.Value.GetMethodInfoX();
								}
							}
						}
					}
				}
				MethodInfo mif = this.BaseClassType.GetMethod(name, bindingAttr);
				return mif;
			}
			return null;
		}
		public MethodInfo[] GetAllMethods(BindingFlags bindingAttr)
		{
			List<MethodInfo> lst = new List<MethodInfo>();
			if (this.IsInterface)
			{
				if (this.ObjectList.Count == 0)
				{
					this.ObjectList.LoadObjects();
				}
				InterfaceClass ifc = this.Interface;
				List<InterfaceElementMethod> ms = ifc.Methods;
				if (ms != null && ms.Count > 0)
				{
					foreach (InterfaceElementMethod im in ms)
					{
						lst.Add(new MethodInfoInterface(im));
					}
				}
			}
			else
			{
				Dictionary<string, MethodClass> ms = this.CustomMethods;
				if (ms != null)
				{
					if ((bindingAttr & BindingFlags.Static) == BindingFlags.Static)
					{
						foreach (KeyValuePair<string, MethodClass> kv in ms)
						{
							if (kv.Value.IsStatic)
							{
								lst.Add(kv.Value.GetMethodInfoX());
							}
						}
					}
					else
					{
						foreach (KeyValuePair<string, MethodClass> kv in ms)
						{
							if (!kv.Value.IsStatic)
							{
								lst.Add(kv.Value.GetMethodInfoX());
							}
						}
					}
				}
				MethodInfo[] mifs = this.BaseClassType.GetMethods(bindingAttr);
				if (mifs != null)
				{
					lst.AddRange(mifs);
				}
			}
			return lst.ToArray();
		}
		public EventInfo[] GetAllEvents(BindingFlags bindingAttr)
		{
			List<EventInfo> lst = new List<EventInfo>();
			if (this.IsInterface)
			{
				if (this.ObjectList.Count == 0)
				{
					this.ObjectList.LoadObjects();
				}
				InterfaceClass ifc = this.Interface;
				List<InterfaceElementEvent> ms = ifc.Events;
				if (ms != null && ms.Count > 0)
				{
					foreach (InterfaceElementEvent im in ms)
					{
						lst.Add(new EventInfoInterface(im));
					}
				}
			}
			else
			{
				Dictionary<string, EventClass> ms = this.CustomEvents;
				if (ms != null)
				{
					if ((bindingAttr & BindingFlags.Static) == BindingFlags.Static)
					{
						foreach (KeyValuePair<string, EventClass> kv in ms)
						{
							if (kv.Value.IsStatic)
							{
								lst.Add(kv.Value.GetEventInfoX());
							}
						}
					}
					else
					{
						foreach (KeyValuePair<string, EventClass> kv in ms)
						{
							if (!kv.Value.IsStatic)
							{
								lst.Add(kv.Value.GetEventInfoX());
							}
						}
					}
				}
				EventInfo[] eifs = this.BaseClassType.GetEvents(bindingAttr);
				if (eifs != null)
				{
					lst.AddRange(eifs);
				}
			}
			return lst.ToArray();
		}
		public EventInfo GetEventInfo(string name, BindingFlags bindingAttr)
		{
			if (this.IsInterface)
			{
				if (this.ObjectList.Count == 0)
				{
					this.ObjectList.LoadObjects();
				}
				InterfaceClass ifc = this.Interface;
				List<InterfaceElementEvent> ms = ifc.Events;
				if (ms != null && ms.Count > 0)
				{
					foreach (InterfaceElementEvent im in ms)
					{
						if (string.CompareOrdinal(name, im.Name) == 0)
						{
							return new EventInfoInterface(im);
						}
					}
				}
			}
			else
			{
				Dictionary<string, EventClass> ms = this.CustomEvents;
				if (ms != null)
				{
					if ((bindingAttr & BindingFlags.Static) == BindingFlags.Static)
					{
						foreach (KeyValuePair<string, EventClass> kv in ms)
						{
							if (kv.Value.IsStatic)
							{
								if (string.CompareOrdinal(name, kv.Key) == 0)
								{
									return kv.Value.GetEventInfoX();
								}
							}
						}
					}
					else
					{
						foreach (KeyValuePair<string, EventClass> kv in ms)
						{
							if (!kv.Value.IsStatic)
							{
								if (string.CompareOrdinal(name, kv.Key) == 0)
								{
									return kv.Value.GetEventInfoX();
								}
							}
						}
					}
				}
				return this.BaseClassType.GetEvent(name, bindingAttr);
			}
			return null;
		}
		public PropertyInfo GetPropertyInfo(string name, BindingFlags bindingAttr)
		{
			if (this.IsInterface)
			{
				if (this.ObjectList.Count == 0)
				{
					this.ObjectList.LoadObjects();
				}
				InterfaceClass ifc = this.Interface;
				List<InterfaceElementProperty> ms = ifc.Properties;
				if (ms != null && ms.Count > 0)
				{
					foreach (InterfaceElementProperty im in ms)
					{
						if (string.CompareOrdinal(name, im.Name) == 0)
						{
							return new PropertyInfoInterface(im);
						}
					}
				}
			}
			else
			{
				Dictionary<string, PropertyClass> props = CustomProperties;
				if (props != null)
				{
					if ((bindingAttr & BindingFlags.Static) == BindingFlags.Static)
					{
						foreach (KeyValuePair<string, PropertyClass> kv in props)
						{
							if (kv.Value.IsStatic)
							{
								if (string.CompareOrdinal(name, kv.Key) == 0)
								{
									return kv.Value.GetPropertyInfoX();
								}
							}
						}
					}
					else
					{
						foreach (KeyValuePair<string, PropertyClass> kv in props)
						{
							if (!kv.Value.IsStatic)
							{
								if (string.CompareOrdinal(name, kv.Key) == 0)
								{
									return kv.Value.GetPropertyInfoX();
								}
							}
						}
					}
				}
				return this.BaseClassType.GetProperty(name, bindingAttr);
			}
			return null;
		}
		public PropertyInfo[] GetAllProperties(BindingFlags bindingAttr)
		{
			List<PropertyInfo> lst = new List<PropertyInfo>();
			if (this.IsInterface)
			{
				if (this.ObjectList.Count == 0)
				{
					this.ObjectList.LoadObjects();
				}
				InterfaceClass ifc = this.Interface;
				List<InterfaceElementProperty> ms = ifc.Properties;
				if (ms != null && ms.Count > 0)
				{
					foreach (InterfaceElementProperty im in ms)
					{
						lst.Add(new PropertyInfoInterface(im));
					}
				}
			}
			else
			{
				Dictionary<string, PropertyClass> props = CustomProperties;
				if (props != null)
				{
					if ((bindingAttr & BindingFlags.Static) == BindingFlags.Static)
					{
						foreach (KeyValuePair<string, PropertyClass> kv in props)
						{
							if (kv.Value.IsStatic)
							{
								lst.Add(kv.Value.GetPropertyInfoX());
							}
						}
					}
					else
					{
						foreach (KeyValuePair<string, PropertyClass> kv in props)
						{
							if (!kv.Value.IsStatic)
							{
								lst.Add(kv.Value.GetPropertyInfoX());
							}
						}
					}
					return lst.ToArray();
				}
				PropertyInfo[] mifs = this.BaseClassType.GetProperties(bindingAttr);
				if (mifs != null)
				{
					lst.AddRange(mifs);
				}
			}
			return lst.ToArray();
		}
		#endregion
	}
	/// <summary>
	/// represents a component contained in the root component,
	/// for example, a button in a form.
	/// </summary>
	[SaveAsProperties]
	[Serializable]
	public class MemberComponentId : IClass
	{
		#region fields and constructors
		private ClassPointer _root;//the container
		private object _instance;//the contained component
		private Type _type;
		private UInt32 _memberId; //member id in _root
		private UInt32 _classId;
		public MemberComponentId()
		{
		}
		public MemberComponentId(ClassPointer root, object obj, UInt32 id)
		{
			_root = root;
			_classId = _root.ClassId;
			_instance = obj;
			_memberId = id;
			setType();
		}
		private string getname()
		{
			if (_instance != null)
			{
				Type t = _instance.GetType();
				PropertyInfo pif = t.GetProperty("Name");
				if (pif != null)
				{
					object v = pif.GetValue(_instance, null);
					if (v != null)
					{
						return v.ToString();
					}
				}
			}
			return null;
		}
		#endregion
		#region Properties
		[Browsable(false)]
		public virtual ClassPointer RootHost
		{
			get
			{
				return _root;
			}
		}
		[Browsable(false)]
		public virtual bool IsClassInstance
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public object Instance
		{
			get
			{
				if (_instance == null)
				{
					if (_root != null && _memberId > 0)
					{
						_instance = _root.ObjectList.GetObjectByID(_memberId);
					}
				}
				return _instance;
			}
		}
		[Browsable(false)]
		public bool IsValid
		{
			get
			{
				if (_root != null && Instance != null)
				{
					return true;
				}
				MathNode.LastValidationError = string.Format(CultureInfo.InvariantCulture, "Some of (_root,Instance) are null for [{0}] of [{1}]. ({2},{3})", this.ToString(), this.GetType().Name, _root, Instance);
				return false;
			}
		}
		[Browsable(false)]
		public IClass Host
		{
			get
			{
				return _root;
			}
		}
		[Browsable(false)]
		public UInt64 WholeId
		{
			get
			{
				return DesignUtil.MakeDDWord(_memberId, _root.ClassId);
			}
		}
		[Browsable(false)]
		public UInt32 MemberId
		{
			get
			{
				return _memberId;
			}
			set
			{
				_memberId = value;
			}
		}
		[Browsable(false)]
		public UInt32 ClassId
		{
			get
			{
				if (_root != null)
				{
					_classId = _root.ClassId;
				}
				return _classId;
			}
			set
			{
				_classId = value;
			}
		}
		[ParenthesizePropertyName(true)]
		public string Name
		{
			get
			{
				IComponent ic = Instance as IComponent;
				if (ic != null && ic.Site != null)
				{
					return ic.Site.Name;
				}
				if (_root != null && _root.XmlData != null && _memberId != 0)
				{
					string name = DesignUtil.GetObjectNameById(_memberId, _root.XmlData);
					if (string.CompareOrdinal(name, "?") != 0)
					{
						return name;
					}
					name = getname();
					if (!string.IsNullOrEmpty(name))
					{
						return name;
					}
				}
				if (Instance != null)
					return Instance.ToString();
				if (_memberId == 0)
				{
					return "DefaultInstance";
				}
				return "?";
			}
		}

		#endregion
		#region IObjectPointer Members
		public EnumWebRunAt RunAt
		{
			get
			{
				if (_instance != null)
				{
					if (_instance is IJavascriptType)
					{
						return EnumWebRunAt.Client;
					}
					if (_instance is IPhpType)
					{
						return EnumWebRunAt.Server;
					}
					if (_instance is IWebClientComponent)
					{
						return EnumWebRunAt.Client;
					}
				}
				if (_type != null)
				{
					if (PhpTypeAttribute.IsPhpType(_type))
					{
						return EnumWebRunAt.Server;
					}
					if (JsTypeAttribute.IsJsType(_type))
					{
						return EnumWebRunAt.Client;
					}
					if (_type.GetInterface("IWebClientComponent") != null)
					{
						return EnumWebRunAt.Client;
					}
				}
				return EnumWebRunAt.Server;
			}
		}
		/// <summary>
		/// the class holding (not neccerily declaring) this pointer
		/// </summary>
		[Browsable(false)]
		public ClassPointer RootPointer
		{
			get
			{
				return _root;
			}
		}
		/// <summary>
		/// variable name
		/// </summary>
		[Browsable(false)]
		public string CodeName
		{
			get
			{
				IWebClientControl ic = Instance as IWebClientControl;
				if (ic != null)
				{
					return ic.CodeName;
				}
				ICustomCodeName icc = _instance as ICustomCodeName;
				if (icc != null)
				{
					return icc.CodeName;
				}
				IParameter ip = _instance as IParameter;
				if (ip != null)
				{
					return ip.CodeName;
				}
				return Name;
			}
		}
		/// <summary>
		/// fully qualified variable name
		/// </summary>
		[Browsable(false)]
		public virtual string ReferenceName
		{
			get
			{
				string n = Name;
				if (_root != null)
				{
					return _root.Name + "." + n;
				}
				return n;
			}
		}
		[Browsable(false)]
		public bool IsStatic
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public string TypeString
		{
			get
			{
				ClassInstancePointer cp = Instance as ClassInstancePointer;
				if (cp != null)
				{
					return cp.CodeName;
				}
				return _type.AssemblyQualifiedName;
			}
		}
		[Description("The class of the object")]
		public string ObjectClass
		{
			get
			{
				ClassInstancePointer cp = _instance as ClassInstancePointer;
				if (cp != null)
				{
					return cp.CodeName;
				}
				if (_type != null)
				{
					return _type.FullName;
				}
				return ClassType;
			}
		}
		[Browsable(false)]
		public virtual string ClassType
		{
			get
			{
				if (Instance != null)
				{
					return Instance.GetType().FullName;
				}
				return "{Unknown}";
			}
		}
		/// <summary>
		/// if the host is a static class then its components are public properties;
		/// if the host is a non-static class then its components are private variables.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public virtual CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			if (_root.IsStatic)
			{
				return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(_root.TypeString), this.Name);
			}
			else
			{
				CodeExpression ce = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), this.Name);
				//
				return ce;
			}
		}
		public virtual string GetJavaScriptReferenceCode(StringCollection code)
		{
			HtmlDataRepeater dp = null;
			Control ct = ObjectInstance as Control;
			if (ct != null)
			{
				while (ct != null)
				{
					dp = ct.Parent as HtmlDataRepeater;
					if (dp != null)
					{
						break;
					}
					ct = ct.Parent;
				}
			}
			if (dp != null)
			{
				return string.Format(CultureInfo.InvariantCulture, "document.getElementById('{0}').jsData.getElement('{1}')", dp.CodeName, this.Name);
			}
			else
			{
				IWebClientAlternative wa = this.ObjectInstance as IWebClientAlternative;
				if (wa != null)
					return string.Format(CultureInfo.InvariantCulture, "document.getElementById('{0}')", wa.RuntimeID);
				return string.Format(CultureInfo.InvariantCulture, "document.getElementById('{0}')", this.Name);
			}
		}
		public virtual string GetPhpScriptReferenceCode(StringCollection code)
		{
			return string.Format(CultureInfo.InvariantCulture, "$this->{0}", Name);
		}
		public virtual void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			IWebClientComponent wc = this.ObjectInstance as IWebClientComponent;
			if (wc != null)
			{
				wc.CreateActionJavaScript(methodName, code, parameters, returnReceiver);
			}
			else
			{
				WebPageCompilerUtility.CreateActionJavaScript(this.GetJavaScriptReferenceCode(code), methodName, code, parameters, returnReceiver);
			}
		}
		public virtual void CreateActionPhpScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			IWebServerProgrammingSupport ws = this.ObjectInstance as IWebServerProgrammingSupport;
			if (ws != null)
			{
				ws.CreateActionPhpScript(this.CodeName, methodName, code, parameters, returnReceiver);
			}
		}
		[Browsable(false)]
		public IObjectIdentity IdentityOwner
		{
			get
			{
				return Owner;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual IObjectPointer Owner
		{
			get
			{
				return _root;
			}
			set
			{
				_root = (ClassPointer)value;
			}
		}
		[Browsable(false)]
		public IPropertyPointer PropertyOwner { get { return Owner; } }
		[Browsable(false)]
		[ReadOnly(true)]
		public Type ObjectType
		{
			get
			{
				if (_type == null)
				{
					setType();
				}
				return _type;
			}
			set
			{
				_type = value;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public object ObjectDebug { get; set; }
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual object ObjectInstance
		{
			get
			{
				return Instance;
			}
			set
			{
				_instance = value;
			}
		}

		public bool IsSameObjectRef(IObjectIdentity objectPointer)
		{
			MemberComponentId c = objectPointer as MemberComponentId;
			if (c != null)
			{
				if (c.MemberId != 0 && this.MemberId != 0)
				{
					return (c.WholeId == this.WholeId);
				}
				else
				{
					return string.CompareOrdinal(c.Name, this.Name) == 0;
				}
			}
			return false;
		}
		[Browsable(false)]
		public bool IsSameProperty(IPropertyPointer p)
		{
			IObjectIdentity objectPointer = p as IObjectIdentity;
			if (objectPointer != null)
			{
				return IsSameObjectRef(objectPointer);
			}
			return false;
		}
		[Browsable(false)]
		public virtual string DisplayName
		{
			get
			{
				IComponent c = Instance as IComponent;
				if (c != null)
				{
					if (c.Site != null)
					{
						if (_type != null)
							return c.Site.Name + " of " + _type.Name;
						return c.Site.Name + " of " + Instance.GetType().Name;
					}
				}
				string name = DesignUtil.GetObjectNameById(MemberId, _root.XmlData);
				if (string.CompareOrdinal(name, "?") != 0)
				{
					return name;
				}
				name = getname();
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}
				return "?";
			}
		}
		[Browsable(false)]
		public string LongDisplayName
		{
			get
			{
				return DisplayName;
			}
		}
		[Browsable(false)]
		public string ExpressionDisplay
		{
			get
			{
				return Name;
			}
		}
		public bool IsTargeted(EnumObjectSelectType target)
		{
			return (target == EnumObjectSelectType.Object || target == EnumObjectSelectType.All);
		}
		[Browsable(false)]
		public string ObjectKey
		{
			get { return WholeId.ToString("x", CultureInfo.InvariantCulture); }
		}
		[Browsable(false)]
		public EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public EnumPointerType PointerType { get { return EnumPointerType.Class; } }
		#endregion
		#region ICloneable Members

		public virtual object Clone()
		{
			MemberComponentId obj = (MemberComponentId)Activator.CreateInstance(this.GetType(), _root, Instance, _memberId);
			return obj;
		}

		#endregion
		#region ISerializerProcessor Members
		private static Stack<UInt32> _loadingobjs = new Stack<uint>();
		public virtual void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
				return;
			if (_classId != 0)
			{
				_root = ClassPointer.CreateClassPointer(_classId, objMap.Project);
			}
			else
			{
				_root = ClassPointer.CreateClassPointer(objMap);
			}
			if (_root.ObjectList.Count == 0)
			{
				if (!_loadingobjs.Contains(_root.ClassId))
				{
					_loadingobjs.Push(_root.ClassId);
					_root.ObjectList.Reader.ReadNonControls(objectNode.OwnerDocument.DocumentElement, _root.ObjectInstance, null);
					_loadingobjs.Pop();
					if (_root.ObjectList.Reader.HasErrors)
					{
						MathNode.Log(_root.ObjectList.Reader.Errors);
						_root.ObjectList.Reader.ResetErrors();
					}
				}
			}
			if (_instance == null)
			{
				_instance = _root.ObjectList.GetObjectByID((uint)_memberId);
			}
		}

		#endregion
		#region Methods
		public override string ToString()
		{
			return DisplayName;
		}
		public void SetRoot(ClassPointer r)
		{
			_root = r;
			if (_root != null)
			{
				_classId = _root.ClassId;
			}
		}
		public void SetInstance(object p)
		{
			_instance = p;
		}
		public virtual void ReloadInstance()
		{
			_instance = _root.ObjectList.GetObjectByID(_memberId);
		}
		public void ReloadInstanceDeep()
		{
			if (_instance == null && _root != null)
			{
				if (_root.ObjectList.Count == 0)
				{
					_root.ObjectList.LoadObjects();
				}
				ReloadInstance();
			}
		}
		private void setType()
		{
			ClassInstancePointer cp = Instance as ClassInstancePointer;
			if (cp != null)
				_type = cp.ObjectType;
			else
			{
				if (_instance != null)
				{
					_type = _instance.GetType();
				}
				else
				{
					if (_root != null)
					{
						_type = _root.GetMemberObjectType(_memberId);
					}
				}
			}
		}
		#endregion
		#region factory
		public static MemberComponentId CreateMemberComponentId(ClassPointer host, object v, UInt32 memberId)
		{
			ClassInstancePointer obj = v as ClassInstancePointer;
			if (obj != null)
			{
				return new MemberComponentIdCustom(host, obj, memberId);
			}
			obj = host.ObjectList.GetClassRefById(memberId) as ClassInstancePointer;
			if (obj != null)
			{
				return new MemberComponentIdCustom(host, obj, memberId);
			}
			return new MemberComponentId(host, v, memberId);
		}
		public static MemberComponentId CreateMemberComponentId(ClassInstancePointer host, object v, UInt32 memberId)
		{
			ClassInstancePointer obj = v as ClassInstancePointer;
			if (obj == null)
			{
				obj = host.Definition.ObjectList.GetClassRefById(memberId) as ClassInstancePointer;
			}
			if (obj != null)
			{
				if (obj.ClassId != host.ClassId)
				{
					obj.Owner = host;
				}
				else
				{
					obj.Owner = host.Definition;
				}
				return new MemberComponentIdCustomInstance(host, obj, memberId);
			}
			return new MemberComponentIdInstance(host, v, memberId);
		}
		#endregion
		#region IClass Members
		[ReadOnly(true)]
		[Browsable(false)]
		public Image ImageIcon
		{
			get
			{
				if (_root != null && _root.ObjectList != null && Instance != null)
				{
					IClassRef cr = _root.ObjectList.GetClassRefByObject(_instance);
					if (cr != null)
					{
						int n = TreeViewObjectExplorer.GetTypeIcon(_root.ObjectList.Project, cr);
						return TreeViewObjectExplorer.GetTypeImage(n);
					}
				}
				if (ObjectType != null)
				{
					return TreeViewObjectExplorer.GetTypeImage(ObjectType);
				}
				return null;
			}
			set
			{
			}
		}
		[Browsable(false)]
		public virtual Type VariableLibType
		{
			get { return ObjectType; }
		}
		[Browsable(false)]
		public virtual ClassPointer VariableCustomType
		{
			get { return null; }
		}
		[Browsable(false)]
		public IClassWrapper VariableWrapperType
		{
			get { return null; }
		}
		[Browsable(false)]
		public virtual UInt32 DefinitionClassId
		{
			get
			{
				return ClassId;
			}
		}
		#endregion
	}
	/// <summary>
	/// pointing to a lib component of an instance of a custom class (Definition class). Host uses an instance of Defition.
	/// Comparing to MemberComponentId, the Definition=Host, not an instance
	/// this->ClassInstancePointer->[ClassInstancePointer->...]ClassPointer
	///         .Host ------------->.Host ---------------->...]ClassPointer
	///         .Definition         .Definition
	/// MemberId is for the first Definition
	/// InstanceId is for the first Host
	///         ClassInstancePointer._definingClassId 
	///         ClassInstancePointer._memberId is for its Host
	///         
	/// Loading: go through all the levels of {ClassContainer} until the end. 
	/// ClassInstancePointer._memberId for the last level is for ClassPointer. Set the last level Host. Cn is completed
	///   for the next last level use ClassInstancePointer._memberId to get instance
	/// </summary>
	public class MemberComponentIdInstance : MemberComponentId
	{
		#region fields and constructrs
		private ClassInstancePointer _owner;
		public MemberComponentIdInstance()
		{
		}
		public MemberComponentIdInstance(ClassInstancePointer owner, object obj, UInt32 id) :
			base(owner.Definition, obj, id)
		{
			_owner = owner;
		}
		#endregion
		#region ICloneable Members

		public override object Clone()
		{
			MemberComponentIdInstance obj = new MemberComponentIdInstance(_owner, Instance, MemberId);
			return obj;
		}

		#endregion
		#region Properties
		[Browsable(false)]
		public override ClassPointer RootHost
		{
			get
			{
				return _owner.RootHost;
			}
		}
		[Browsable(false)]
		public override bool IsClassInstance
		{
			get
			{
				return true;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public override IObjectPointer Owner
		{
			get
			{
				return _owner;
			}
			set
			{
				_owner = (ClassInstancePointer)value;
				SetRoot(_owner.Definition);
			}
		}
		#endregion
		#region Methods
		public override CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			CodeExpression co = _owner.GetReferenceCode(method, statements, forValue);
			return new CodeFieldReferenceExpression(co, this.Name);
		}
		#endregion
		#region ISerializerProcessor Members
		/// <summary>
		/// MemberId is the id for the object contained by the last level ClassPointer.
		/// The object pointed to by MemberId is an ClassInstancePointer.
		/// ClassInstancePointer.
		/// </summary>
		/// <param name="objMap"></param>
		/// <param name="objectNode"></param>
		/// <param name="saved"></param>
		/// <param name="serializer"></param>
		public override void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
			{
				_owner.OnPostSerialize(objMap, objectNode, saved, serializer);
			}
			else
			{
				if (_owner == null)
				{
					ClassInstancePointer ip = new ClassInstancePointer();
					ip.OnPostSerialize(objMap, objectNode, saved, serializer);
					_owner = ip;
					SetRoot(_owner.Definition);
					if (_owner.Definition.ObjectList.Count == 0)
					{
						if (_owner.Definition.ClassId != _owner.Host.ClassId)
						{
							_owner.Definition.ObjectList.LoadObjects();
							if (_owner.Definition.ObjectList.Reader.HasErrors)
							{
								MathNode.Log(_owner.Definition.ObjectList.Reader.Errors);
								_owner.Definition.ObjectList.Reader.ResetErrors();
							}
						}
					}
				}
				IClassRef cr = _owner.Definition.ObjectList.GetClassRefById(MemberId);
				if (cr != null)
				{
					ObjectInstance = cr;
				}
				else
				{
					ObjectInstance = _owner.Definition.ObjectList.GetObjectByID(MemberId);
				}
			}
		}

		#endregion
	}
	/// <summary>
	/// a class instance as a hosted member. it is for the first level
	/// RootPointer is the host
	/// Pointer is the classref member, identified by the MemberId
	/// </summary>
	[SaveAsProperties]
	[Serializable]
	public class MemberComponentIdCustom : MemberComponentId
	{
		public MemberComponentIdCustom()
		{
		}
		public MemberComponentIdCustom(ClassPointer root, ClassInstancePointer obj, UInt32 id)
			: base(root, obj, id)
		{
		}
		[Browsable(false)]
		public ClassInstancePointer Pointer
		{
			get
			{
				ClassInstancePointer cp = ObjectInstance as ClassInstancePointer;
				if (cp == null)
				{
					if (RootPointer != null)
					{
						cp = (ClassInstancePointer)RootPointer.ObjectList.GetClassRefById(this.MemberId);
					}
				}
				return cp;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public override object ObjectInstance
		{
			get
			{
				return Instance;
			}
			set
			{
				ClassInstancePointer cp = value as ClassInstancePointer;
				if (cp != null)
				{
					base.ObjectInstance = cp;
				}
				else
				{
					if (value != null)
					{
						throw new DesignerException("[{0}] is not ClassInstancePointer", value.GetType());
					}
				}
			}
		}
		[Browsable(false)]
		public override string DisplayName
		{
			get
			{
				ClassInstancePointer c = Pointer;
				if (c != null)
				{
					return c.DisplayName;
				}
				return base.DisplayName;
			}
		}
		public override void ReloadInstance()
		{
			ObjectInstance = RootPointer.ObjectList.GetClassRefById(MemberId);
		}
		public override string ToString()
		{
			return DisplayName;
		}
		[Browsable(false)]
		public override string ClassType
		{
			get
			{
				return Pointer.Definition.CodeName;
			}
		}
		#region ISerializerProcessor Members

		public override void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
				return;
			SetRoot(ClassPointer.CreateClassPointer(objMap));
			object v = objMap.GetObjectByID(MemberId);
			ClassInstancePointer p = objMap.GetClassRefByObject(v) as ClassInstancePointer;
			if (p == null)
			{
				throw new DesignerException("Pointer is null when reading MemberComponentIdCustom [{0},{1}]", ClassId, MemberId);
			}
			SetInstance(p);
		}

		#endregion

		[Browsable(false)]
		public override Type VariableLibType
		{
			get { return null; }
		}
		[Browsable(false)]
		public override ClassPointer VariableCustomType
		{
			get { return Pointer.Definition; }
		}
		[Browsable(false)]
		public override UInt32 DefinitionClassId
		{
			get
			{
				return VariableCustomType.ClassId;
			}
		}
		public static void AdjustHost(IObjectPointer op)
		{
			while (op != null)
			{
				MemberComponentIdCustom mc = op.Owner as MemberComponentIdCustom;
				if (mc != null)
				{
					op.Owner = mc.Pointer;
					break;
				}
				op = op.Owner;
			}
		}
	}
	public class MemberComponentIdCustomInstance : MemberComponentIdCustom
	{
		#region fields and constructors
		private ClassInstancePointer _owner;
		public MemberComponentIdCustomInstance()
		{
		}
		public MemberComponentIdCustomInstance(ClassInstancePointer owner, ClassInstancePointer obj, UInt32 id)
			: base(owner.Definition, obj, id)
		{
			_owner = owner;
		}
		#endregion
		#region Methods
		public override void ReloadInstance()
		{
			ObjectInstance = _owner.Definition.ObjectList.GetClassRefById(MemberId);
		}
		public override CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			CodeExpression c1 = _owner.GetReferenceCode(method, statements, forValue);
			CodeExpression ce = new CodeFieldReferenceExpression(c1, this.Name);
			return ce;
		}
		#endregion
		#region Properties
		[Browsable(false)]
		public override ClassPointer RootHost
		{
			get
			{
				return _owner.RootHost;
			}
		}
		[Browsable(false)]
		public override bool IsClassInstance
		{
			get
			{
				return true;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public override IObjectPointer Owner
		{
			get
			{
				return _owner;
			}
			set
			{
				_owner = (ClassInstancePointer)value;
				SetRoot(_owner.Definition);
			}
		}
		#endregion
		#region ICloneable Members

		public override object Clone()
		{
			MemberComponentIdCustomInstance obj = new MemberComponentIdCustomInstance(_owner, (ClassInstancePointer)Instance, MemberId);
			return obj;
		}
		#endregion
		#region ISerializerProcessor Members

		public override void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
			{
				_owner.OnPostSerialize(objMap, objectNode, saved, serializer);
			}
			else
			{
				if (_owner == null)
				{
					ClassInstancePointer ip = new ClassInstancePointer();
					ip.OnPostSerialize(objMap, objectNode, saved, serializer);
					_owner = ip;
					SetRoot(_owner.Definition);
					if (_owner.Definition.ObjectList.Count == 0)
					{
						if (_owner.Definition.ClassId != _owner.Host.ClassId)
						{
							_owner.Definition.ObjectList.LoadObjects();
							if (_owner.Definition.ObjectList.Reader.HasErrors)
							{
								MathNode.Log(_owner.Definition.ObjectList.Reader.Errors);
								_owner.Definition.ObjectList.Reader.ResetErrors();
							}
						}
					}
				}
				IClassRef cr = _owner.Definition.ObjectList.GetClassRefById(MemberId);
				if (cr != null)
				{
					ObjectInstance = cr;
				}
				else
				{
					ObjectInstance = _owner.Definition.ObjectList.GetObjectByID(MemberId);
				}
			}

		}

		#endregion
	}
	public class MemberComponentIdDefaultInstance : MemberComponentIdCustomInstance
	{
		public MemberComponentIdDefaultInstance()
		{
		}
		public MemberComponentIdDefaultInstance(ClassInstancePointer owner)
			: base(owner, owner, 0)
		{
		}
		public override CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			ClassInstancePointer cip = (ClassInstancePointer)Owner;
			return new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(cip.Definition.TypeString), DrawingPage.DEFAULTFORM);

		}
		#region ICloneable Members

		public override object Clone()
		{
			MemberComponentIdDefaultInstance obj = new MemberComponentIdDefaultInstance((ClassInstancePointer)Owner);
			return obj;
		}
		#endregion
		#region ISerializerProcessor Members

		public override void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
			{
				base.OnPostSerialize(objMap, objectNode, saved, serializer);
			}
			else
			{
				ClassPointer cid = ClassPointer.CreateClassPointer(this.ClassId, objMap.Project);
				ClassInstancePointer cip = new ClassInstancePointer((ClassPointer)objMap.RootPointer, cid, 0);
				cip.Name = "DefaultInstance";
				SetInstance(cip);
				Owner = cip;
			}

		}

		#endregion
	}
}
