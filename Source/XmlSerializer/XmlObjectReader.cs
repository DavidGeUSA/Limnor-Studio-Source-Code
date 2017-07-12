/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Project Serialization in XML
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Xml;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.ComponentModel.Design.Serialization;
using System.Collections.Specialized;
using System.Collections;
using System.Globalization;
using VSPrj;
using TraceLog;
using XmlUtility;
using VPL;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.CodeDom;
using ProgElements;
using System.Xml.Serialization;
using LFilePath;

namespace XmlSerializer
{
	/// <summary>
	/// designer can be plugged in through this interface
	/// </summary>
	public interface IObjectFactory
	{
		IContainer ComponentContainer { get; set; }
		IComponent CreateInstance(Type type, string name);
	}
	public delegate void fnAfterReadRootComponent(IComponent root, XmlNode Node, XmlObjectReader reader);
	public class XmlObjectReader : IXmlCodeReader
	{
		public static bool ADJUSTPATH = false;
		public const string PRJPATHSYMBOLE = "$$$";
		public const string PRJRESOURCESFOLDERNAME = "DesignTimeResources";
		private static readonly Attribute[] propertyAttributes = new Attribute[] 
		{
			DesignOnlyAttribute.No
		};
		private Stack _objectStack;
		private List<IPostRootDeserialize> _postRootDeserializers;
		private Stack<List<IPostOwnersSerialize>> _postOwnersDeserializers;
		//
		static private LimnorProject _prj;
		private Dictionary<object, Dictionary<PropertyDescriptor, List<BindingLoader>>> _bindings;
		private Dictionary<object, Dictionary<PropertyDescriptor, string>> _references;

		private Dictionary<PropertyDescriptor, object> _propertiesNotSet;
		private List<CodeStatementCollectionDeserializer> _customSerilizers;
		private IObjectFactory _objectFactory;
		private ObjectIDmap _objMap;
		private object _rootObject; //for resolving CodeDom
		private int _traceLevel;
		private bool _lightRead;
		private StringCollection errors;
		private StringCollection _criticalErrors;
		private fnAfterReadRootComponent _onReadRoot;
		private fnAfterReadRootComponent _onRootObjectCreated;
		private Dictionary<Guid, Dictionary<IDelayedInitialize, XmlNode>> _delayedInitializers;
		private Guid _delayedReadingOwner = Guid.Empty; //set by ClearDelayedInitializers
		private object _lockObject = new object();
		const int TraceLevelBasic = 15;
		public XmlObjectReader(ObjectIDmap objectMap)
		{
			_objMap = objectMap;
			if (_objMap != null)
			{
				_prj = _objMap.Project;
				_traceLevel = _objMap.TraceLevel;
			}
		}
		public XmlObjectReader(ObjectIDmap objectMap, fnAfterReadRootComponent onReadRoot, fnAfterReadRootComponent onRootCreated)
			: this(objectMap)
		{
			_onReadRoot = onReadRoot;
			_onRootObjectCreated = onRootCreated;
		}
		public Stack ObjectStack { get { return _objectStack; } }
		public IList<IPostRootDeserialize> PostRootDeserializers { get { return _postRootDeserializers; } }
		public void Reset()
		{
			ResetKeepErrors();
			errors = null;
		}
		public void ResetKeepErrors()
		{
			_bindings = null;
			_references = null;
			_delayedInitializers = null;
			_propertiesNotSet = null;
			_customSerilizers = null;
		}
		public void PushPostOwnersDeserializers(List<IPostOwnersSerialize> objs)
		{
			if (_postOwnersDeserializers == null)
			{
				_postOwnersDeserializers = new Stack<List<IPostOwnersSerialize>>();
			}
			_postOwnersDeserializers.Push(objs);
		}
		public IPostOwnersSerialize[] PopPostSerializers()
		{
			if (_postOwnersDeserializers != null && _postOwnersDeserializers.Count > 0)
			{
				List<IPostOwnersSerialize> objs = _postOwnersDeserializers.Pop();
				if (objs != null)
				{
					return objs.ToArray();
				}
			}
			return null;
		}
		public void AddPostOwnersDeserializers(IPostOwnersSerialize obj)
		{
			if (_postOwnersDeserializers != null && _postOwnersDeserializers.Count > 0)
			{
				List<IPostOwnersSerialize> objs = _postOwnersDeserializers.Peek();
				if (objs != null)
				{
					objs.Add(obj);
				}
			}
		}
		public bool GetAttributeBoolDefFalse(XmlNode node, string name)
		{
			return XmlUtil.GetAttributeBoolDefFalse(node, name);
		}
		public void AddPostRootDeserializer(IPostRootDeserialize obj)
		{
			if (_postRootDeserializers == null)
			{
				_postRootDeserializers = new List<IPostRootDeserialize>();
			}
			_postRootDeserializers.Add(obj);
		}
		public void AddDelayedInitializer(IDelayedInitialize obj, XmlNode node)
		{
			if (_delayedInitializers == null)
			{
				_delayedInitializers = new Dictionary<Guid, Dictionary<IDelayedInitialize, XmlNode>>();
			}
			Dictionary<IDelayedInitialize, XmlNode> d;
			if (!_delayedInitializers.TryGetValue(_delayedReadingOwner, out d))
			{
				d = new Dictionary<IDelayedInitialize, XmlNode>();
				_delayedInitializers.Add(_delayedReadingOwner, d);
			}
			if (!d.ContainsKey(obj))
			{
				d.Add(obj, node);
			}
		}
		public void ClearDelayedInitializers(Guid owner)
		{
			lock (_lockObject)
			{
				if (_delayedInitializers == null)
				{
					_delayedInitializers = new Dictionary<Guid, Dictionary<IDelayedInitialize, XmlNode>>();
				}
				Dictionary<IDelayedInitialize, XmlNode> d;
				if (_delayedInitializers.TryGetValue(owner, out d))
				{
					d.Clear();
				}
				else
				{
					d = new Dictionary<IDelayedInitialize, XmlNode>();
					_delayedInitializers.Add(owner, d);
				}
				_delayedReadingOwner = owner;
			}
		}

		public Dictionary<Guid, Dictionary<IDelayedInitialize, XmlNode>> DelayedInitializers
		{
			get
			{
				return _delayedInitializers;
			}
		}
		public void OnDelayedInitializeObjects(Guid owner)
		{
			if (_delayedInitializers != null)
			{
				Dictionary<IDelayedInitialize, XmlNode> d;
				if (_delayedInitializers.TryGetValue(owner, out d))
				{
					List<KeyValuePair<IDelayedInitialize, XmlNode>> list = new List<KeyValuePair<IDelayedInitialize, XmlNode>>();
					foreach (KeyValuePair<IDelayedInitialize, XmlNode> kv in d)
					{
						list.Add(kv);
					}
					_delayedInitializers.Remove(owner);
					foreach (KeyValuePair<IDelayedInitialize, XmlNode> kv in list)
					{
						kv.Key.OnDelayedPostSerialize(_objMap, kv.Value, this);
					}
				}
			}
		}
		private IDesignerSerializationManager _sm;
		public IDesignerSerializationManager SerializerManager { get { return _sm; } set { _sm = value; } }
		public UInt32 ClassId { get { return _objMap.ClassId; } }
		public string ProjectFolder { get { return _prj.ProjectFolder; } }
		public static LimnorProject CurrentProject
		{
			get
			{
				return _prj;
			}
		}
		public void EnableTrace(bool enable)
		{
			if (enable)
			{
				_traceLevel = TraceLevelBasic + 1;
			}
			else
			{
				_traceLevel = TraceLevelBasic - 1;
			}
		}
		public Dictionary<object, Dictionary<PropertyDescriptor, List<BindingLoader>>> Bindings
		{
			get
			{
				return _bindings;
			}
		}
		public Dictionary<object, Dictionary<PropertyDescriptor, string>> References
		{
			get
			{
				return _references;
			}
		}
		public ObjectIDmap ObjectList
		{
			get
			{
				return _objMap;
			}
		}
		private bool _fc;
		public bool IsForCompile { get { return _fc; } set { _fc = value; } }
		public bool TraceEnabled
		{
			get
			{
				return (_traceLevel > TraceLevelBasic);
			}
		}
		private void logTrace(string message)
		{
			if (TraceEnabled)
			{
				TraceLogClass.TraceLog.Trace(message);
			}
		}
		private void logTrace(string message, params object[] values)
		{
			if (TraceEnabled)
			{
				if (values == null || values.Length == 0)
				{
					TraceLogClass.TraceLog.Trace(message);
				}
				else
				{
					TraceLogClass.TraceLog.Trace(message, values);
				}
			}
		}
		private void logTraceIncIndent()
		{
			if (TraceEnabled)
			{
				TraceLogClass.TraceLog.IndentIncrement();
			}
		}
		private void logTraceDecIndent()
		{
			if (TraceEnabled)
			{
				TraceLogClass.TraceLog.IndentDecrement();
			}
		}
		public IObjectFactory ObjectFactory
		{
			get
			{
				return _objectFactory;
			}
		}
		public void SetMap(ObjectIDmap map)
		{
			_objMap = map;
			if (_objMap != null)
			{
				_traceLevel = _objMap.TraceLevel;
			}
		}
		public void addErrStr2(string s, params object[] values)
		{
			if (values == null || values.Length == 0)
			{
				addErrStr(s);
			}
			else
			{
				addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture, s, values));
			}
		}
		private void push(object v)
		{
			if (_objectStack == null)
			{
				_objectStack = new Stack();
			}
			_objectStack.Push(v);
		}
		private void pop()
		{
			if (_objectStack != null && _objectStack.Count > 0)
			{
				_objectStack.Pop();
			}
			else
			{
				throw new SerializerException("object stack corrupted");
			}
		}
		private void addErrStr(string s)
		{
			if (errors == null)
				errors = new StringCollection();
			errors.Add(s);
			logTrace(s);
		}
		public StringCollection Errors
		{
			get
			{
				return errors;
			}
		}
		public StringCollection CriticalErrors
		{
			get
			{
				return _criticalErrors;
			}
		}
		public bool HasErrors
		{
			get
			{
				return (errors != null && errors.Count > 0);
			}
		}
		public void ResetErrors()
		{
			errors = null;
		}
		public object ReadRootObject(IObjectFactory objectFactory)
		{
			return ReadRootObject(objectFactory, _objMap.XmlData);
		}
		public void ReadNonControls(XmlNode node, object root, Dictionary<DataGridView, List<DataGridViewColumn>> dgvs)
		{
			XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Object);
			if (nodes != null && nodes.Count > 0)
			{
				foreach (XmlNode nd in nodes)
				{
					UInt32 objId = XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_ComponentID);
					if (!_objMap.ContainsValue(objId))
					{
						object vo = ReadObject(nd, root);
						if (VPLUtil.Shutingdown)
						{
							return;
						}
						if (vo == null)
						{
							string errstr = string.Format(System.Globalization.CultureInfo.InvariantCulture,
								"Cannot load object '{0}'. Its type is [{1}]. It is ID is {2}. Please make the DLL files and other support files used by this object available. For example, copy the files to {3}. If ActiveX or COM objects are used then make sure they are registered. If web services or Windows services are used then make sure the services are installed and running.",
								XmlUtil.GetNameAttribute(nd), XmlUtil.GetAttribute(nd, XmlTags.XMLATT_type), XmlUtil.GetAttribute(nd, XmlTags.XMLATT_ComponentID), System.IO.Path.GetDirectoryName(Application.ExecutablePath));
							addErrStr(errstr);
							if (_criticalErrors == null)
							{
								_criticalErrors = new StringCollection();
							}
							_criticalErrors.Add(errstr);
						}
						else
						{
							if (dgvs != null)
							{
								Control c = vo as Control;
								if (c != null)
								{
									rememberDatagridViewComluns(c, dgvs);
								}
							}
						}
					}
				}
				if (CriticalErrors != null)
				{
					FormStringList.ShowErrors("Error loading Object", null, CriticalErrors);
				}
			}
		}
		private void rememberDatagridViewComluns(Control c, Dictionary<DataGridView, List<DataGridViewColumn>> dgvs)
		{
			DataGridView dgv = c as DataGridView;
			if (dgv != null)
			{
				List<DataGridViewColumn> cols = new List<DataGridViewColumn>();
				for (int i = 0; i < dgv.Columns.Count; i++)
				{
					cols.Add(dgv.Columns[i]);
				}
				dgvs.Add(dgv, cols);
			}
			else
			{
				foreach (Control c0 in c.Controls)
				{
					rememberDatagridViewComluns(c0, dgvs);
				}
			}
		}
		public object ReadRootObject(IObjectFactory objectFactory, XmlNode node)
		{
			Reset();
			VPLUtil.CurrentProject = _objMap.Project;
			if (node == null)
			{
				addErrStr("XmlNode is null when calling ReadRootObject");
				return null;
			}
			else
			{
				if (_objMap == null)
				{
					addErrStr("object map is null when calling ReadRootObject");
					return null;
				}
				_objectFactory = objectFactory;
				UInt32 classId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
				UInt32 memberId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
				if (classId != _objMap.ClassId || memberId != _objMap.MemberId)
				{
					addErrStr2("invalid object map [{0},{1}] is used when calling ReadRootObject [{0},{1}]", _objMap.ClassId, _objMap.MemberId, classId, memberId);
					return null;
				}
				object root = ReadObject(node, null);
				if (VPLUtil.Shutingdown)
				{
					return null;
				}
				Dictionary<DataGridView, List<DataGridViewColumn>> dgvs = new Dictionary<DataGridView, List<DataGridViewColumn>>();
				if (root != null)
				{
					IXType ix = root as IXType;
					if (ix != null)
					{
						ix.SetAsRoot();
					}
					if (_objectFactory != null)
					{
						IComponentContainer cc = root as IComponentContainer;
						if (cc != null)
						{
							_objectFactory.ComponentContainer = cc.ComponentContainer;
						}
					}
					//load non-control members
					ReadNonControls(node, root, dgvs);
				}
				if (_customSerilizers != null)
				{
					foreach (CodeStatementCollectionDeserializer cds in _customSerilizers)
					{
						cds.Deserialize();
					}

					foreach (KeyValuePair<DataGridView, List<DataGridViewColumn>> kv in dgvs)
					{
						foreach (DataGridViewColumn col in kv.Value)
						{
							bool found = false;
							for (int i = 0; i < kv.Key.Columns.Count; i++)
							{
								if (string.Compare(col.DataPropertyName, kv.Key.Columns[i].DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								kv.Key.Columns.Add(col);
							}
						}
					}

				}
				foreach (object v in _objMap.Keys)
				{
					ICustomMethodDescriptor icmd = v as ICustomMethodDescriptor;
					if (icmd != null)
					{
						icmd.LoadType(_prj.ProjectFolder);
					}
				}
				//
				OnDelayedInitializeObjects(Guid.Empty);
				//
				if (_onReadRoot != null)
				{
					IComponent ir = root as IComponent;
					if (ir != null)
					{
						_onReadRoot(ir, node, this);
					}
				}
				ResetKeepErrors();
				//
				IDrawingHolder dh = root as IDrawingHolder;
				if (dh != null)
				{
					dh.OnFinishLoad();
				}
				//
				return root;
			}
		}
		public void ReadStaticProperties(XmlNode node)
		{
			XmlNodeList nodeList = node.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}", XmlTags.XML_StaticList, XmlTags.XML_StaticValue));
			foreach (XmlNode nd in nodeList)
			{
				Type t = XmlUtil.GetLibTypeAttribute(nd);
				if (!typeof(Console).Equals(t))
				{
					if (!NotForProgrammingAttribute.IsNotForProgramming(t))
					{
						ICustomEventMethodType icme = VPLUtil.StaticOwnerForType(t) as ICustomEventMethodType;
						PropertyDescriptorCollection props = icme.GetProperties(EnumReflectionMemberInfoSelectScope.StaticOnly, false, true, false);
						ReadProperties(nd, icme, props, XmlTags.XML_PROPERTY);
					}
				}
			}
		}
		public void ReloadObjectFromXmlNode<T>(XmlNode node, T obj)
		{
			ReadObjectFromXmlNode(node, obj, typeof(T), null);
		}
		public void ReadObjectFromXmlNode(XmlNode node, object obj, Type type, object parentObject)
		{
			IProjectAccessor pa = obj as IProjectAccessor;
			if (pa != null)
			{
				pa.Project = _prj;
			}
			IBeforeXmlNodeSerialize before = obj as IBeforeXmlNodeSerialize;
			if (before != null)
			{
				before.OnReadFromXmlNode(this, node);
			}
			XmlNode CodeInitNode = node.SelectSingleNode(XmlTags.XML_Initializer);
			if (CodeInitNode != null)
			{
				try
				{
					if (SerializerManager == null)
					{
						DesignerSerializationManager dsm = new DesignerSerializationManager();
						dsm.CreateSession();
						SerializerManager = dsm;
					}
					Type tp0 = XmlUtil.GetLibTypeAttribute(CodeInitNode);
					if (tp0 == null)
					{
						object[] attris = type.GetCustomAttributes(true);
						if (attris != null && attris.Length > 0)
						{
							for (int i = 0; i < attris.Length; i++)
							{
								DesignerSerializerAttribute da = attris[i] as DesignerSerializerAttribute;
								if (da != null)
								{
									if (da.SerializerTypeName != null && !da.SerializerTypeName.Contains("ControlCodeDomSerializer"))
									{
										tp0 = Type.GetType(da.SerializerTypeName);
										break;
									}
								}
							}
						}
					}
					if (tp0 == null)
					{
						throw new XmlSerializerException("Serializer type not found for [{0}]", node.InnerXml);
					}
					object dsp = SerializerManager.GetSerializer(type, tp0);
					CodeDomSerializer ds0 = dsp as CodeDomSerializer;
					if (ds0 == null)
					{
						ds0 = (CodeDomSerializer)Activator.CreateInstance(tp0);
					}
					if (ds0 != null)
					{
						CodeStatementCollection statements = new CodeStatementCollection();
						string name = XmlUtil.GetNameAttribute(node);
						if (string.IsNullOrEmpty(name))
						{
							XmlNode pn = node.ParentNode;
							if (pn != null)
							{
								name = XmlUtil.GetNameAttribute(pn);
							}
						}
						CodeDomXml cd = new CodeDomXml(_objMap, type, CodeInitNode, name);
						cd.ReadStatementCollection(statements, CodeInitNode, XmlTags.XML_Content);

						CodeStatementCollectionDeserializer cscd = new CodeStatementCollectionDeserializer(statements, this._objMap, node.OwnerDocument.DocumentElement, _rootObject, obj);
						if (_customSerilizers == null)
						{
							_customSerilizers = new List<CodeStatementCollectionDeserializer>();
						}
						_customSerilizers.Add(cscd);
					}
				}
				catch (Exception err)
				{
					addErrStr(SerializerException.FormExceptionText(err));
				}
			}
			else
			{
				IXmlNodeHolder xmlHolder = obj as IXmlNodeHolder;
				if (xmlHolder != null)
				{
					xmlHolder.DataXmlNode = node;
				}
				ISerializeNotify notify = obj as ISerializeNotify;
				if (notify != null)
				{
					notify.ReadingProperties = true;
				}
				IBeforeSerializeNotify bsn = obj as IBeforeSerializeNotify;
				if (bsn != null)
				{
					bsn.OnBeforeRead(this, node);
				}
				IUseClassId uc = obj as IUseClassId;
				if (uc != null)
				{
					uc.SetClassId(_objMap.ClassId);
				}
				IDelayedInitialize di = obj as IDelayedInitialize;
				if (di != null)
				{
					AddDelayedInitializer(di, node);
				}
				IXmlNodeSerializable xmlReader = obj as IXmlNodeSerializable;
				if (xmlReader != null)
				{
					logTrace("IXmlNodeSerialization");
					xmlReader.OnReadFromXmlNode(this, node);
				}
				else
				{
					ICustomSerialization customerSaver = obj as ICustomSerialization;
					if (customerSaver != null)
					{
						logTrace("ICustomSerialization");
						customerSaver.OnReadFromXmlNode(this, node);
					}
					else
					{
						ICustomContentSerialization ccs = obj as ICustomContentSerialization;
						if (ccs != null)
						{
							logTrace("ICustomContentSerialization");
							XmlNode nodeContent = node.SelectSingleNode(XmlTags.XML_Content);
							if (nodeContent != null)
							{
								ReadCustomValue(nodeContent, ccs);
							}
							else
							{
								logTrace("No content");
							}
						}
						else
						{
							logTrace("Properties");
							ReadProperties(node, obj);
							if (type.IsArray)
							{
								XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
								if (nodes.Count > 0)
								{
									logTrace("Array");
									ReadArray(nodes, (Array)obj, parentObject);
								}
							}
							else if (typeof(IList).IsAssignableFrom(type))
							{
								XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
								if (nodes.Count > 0)
								{
									IList list = obj as IList;
									logTrace("IList");
									ReadList(nodes, list, parentObject);
								}
							}
						}
					}
				}
				if (notify != null)
				{
					notify.ReadingProperties = false;
				}
				try
				{
					if (_objMap != null)
					{
						ISerializerProcessor sp = obj as ISerializerProcessor;
						if (sp != null)
						{
							sp.OnPostSerialize(_objMap, node, false, this);
						}
					}
					ISerializationProcessor spr = obj as ISerializationProcessor;
					if (spr != null)
					{
						spr.OnDeserialization(node);
					}
					IPostXmlNodeSerialize px = obj as IPostXmlNodeSerialize;
					if (px != null)
					{
						px.OnReadFromXmlNode(this, node);
					}
					IPostRootDeserialize prd = obj as IPostRootDeserialize;
					if (prd != null)
					{
						AddPostRootDeserializer(prd);
					}
				}
				catch (Exception err)
				{
					addErrStr(SerializerException.FormExceptionText(err));
				}
			}
		}
		public object ReadLightObject(XmlNode node, object parentObject)
		{
			bool lightRead = _lightRead;
			_lightRead = true;
			object v = ReadObject(node, parentObject);
			_lightRead = lightRead;
			return v;
		}
		public object ReadObject(XmlNode node, object parentObject)
		{
			return ReadObject(node, parentObject, null);
		}
		public T ReadObject<T>(XmlNode node)
		{
			return (T)ReadObject(node, null, typeof(T));
		}
		public T ReadObject<T>(XmlNode node, object parentObject)
		{
			object v = ReadObject(node, parentObject, typeof(T));
			if (v != null)
			{
				Type t = v.GetType();
				if (!typeof(T).IsAssignableFrom(t))
				{
					throw new XmlSerializerException("Cannot cast {0} to {1}", t.FullName, typeof(T).FullName);
				}
			}
			return (T)v;
		}
		public object ReadObject(XmlNode node, object parentObject, Type type)
		{
			if (node == null)
			{
				addErrStr("XmlNode is null when calling ReadObject");
				return null;
			}
			else
			{
				object obj = null;
				try
				{
					if (parentObject != null)
					{
						push(parentObject);
					}
					bool designMode = false;
					Type t0 = XmlUtil.GetLibTypeAttribute(node);
					if (t0 != null)
					{
						if (type == null)
						{
							type = t0;
						}
						else
						{
							if (!t0.IsAssignableFrom(type) && !type.IsAssignableFrom(t0))
							{
								addErrStr2("Cannot load type [{0}] from [{1}]. Type mismatch: {2} and {3}", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetPath(node), type.FullName, t0.FullName);
								return null;
							}
							else
							{
								type = t0;
							}
						}
					}
					VPLUtil.LogIdeProfiling("Reading type:{0}", type);
					if (type == null)
					{
						string tn = XmlUtil.GetLibTypeAttributeString(node);
						if (string.IsNullOrEmpty(tn))
						{
						}
						else
						{
							addErrStr2("ReadObject: cannot load type with key name [{0}] from [{1}]", tn, XmlUtil.GetPath(node));
						}
					}
					else
					{
						if (type.Equals(typeof(Binding)))
						{
							return null;
						}
						if (XmlUtil.IsValueType(type))
						{
							TypeConverter converter;
							converter = TypeDescriptor.GetConverter(type);
							obj = converter.ConvertFromInvariantString(node.InnerText);
							logTrace("ReadObject: value is a value [{0}]", node.InnerText);
						}
						else
						{
							string name = XmlUtil.GetNameAttribute(node);
							designMode = XmlUtil.GetAttributeBoolDefFalse(node, XmlTags.XMLATT_designMode);
							if (!designMode)
							{
								if (IsForCompile)
								{
									if (parentObject is ISqlDataSet)
									{
										if (typeof(DataGridViewColumn).IsAssignableFrom(type))
										{
											XmlNode nameNode = node.SelectSingleNode(string.Format(CultureInfo.InvariantCulture,
												"{0}[@{1}='Name']", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
											if (nameNode != null)
											{
												name = nameNode.InnerText.Trim();
											}
											if (string.IsNullOrEmpty(name))
											{
												name = string.Format(CultureInfo.InvariantCulture, "col{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
											}
											designMode = true;
										}
									}
								}
							}
							if (designMode && _objectFactory != null)
							{
								if (!string.IsNullOrEmpty(name))
								{
									int ndot = name.IndexOf('.');
									if (ndot > 0)
									{
										name = name.Substring(0, ndot);
									}
								}
								try
								{
									Type designerType = type;
									if (!IsForCompile)
									{
										if (string.CompareOrdinal(node.Name, "Root") == 0)
										{
											if (type.IsSubclassOf(typeof(Control)))
											{
												object[] attrs0 = type.GetCustomAttributes(true);
												bool isRoot = false;
												for (int i = 0; i < attrs0.Length; i++)
												{
													System.ComponentModel.DesignerAttribute a = attrs0[i] as System.ComponentModel.DesignerAttribute;
													if (a != null)
													{
														if (a.DesignerBaseTypeName.StartsWith("System.ComponentModel.Design.IRootDesigner"))
														{
															isRoot = true;
															break;
														}
													}
												}
												if (!isRoot)
												{
													designerType = VPLUtil.GetDesignerType(type);
												}
											}
										}
									}
									SplitContainer sc = parentObject as SplitContainer;
									if (sc != null)
									{
										if (typeof(System.Windows.Forms.SplitterPanel).Equals(designerType))
										{
											if (string.CompareOrdinal(name, "Panel1") == 0)
											{
												obj = sc.Panel1;
											}
											else
											{
												obj = sc.Panel2;
											}
										}
										else
										{
											obj = _objectFactory.CreateInstance(designerType, name);
										}
									}
									else
									{
										obj = _objectFactory.CreateInstance(designerType, name);
									}
									logTrace("ReadObject: design mode {0}, {1}", designerType, name);
									IXmlNodeHolder xmlHolder = obj as IXmlNodeHolder;
									if (xmlHolder != null)
									{
										xmlHolder.DataXmlNode = node;
									}
								}
								catch (Exception err)
								{
									addErrStr(string.Format("Could not create type {0} by designer holder. name:{1}. {2}", type, name, SerializerException.FormExceptionText(err)));
								}
							}
							else
							{
								obj = createInstance(node, parentObject, type);
							}
							if (obj != null)
							{
								Form frm = null;
								IXmlCodeReaderWriterHolder wh = obj as IXmlCodeReaderWriterHolder;
								if (wh != null)
								{
									wh.SetReader(this);
								}
								push(obj);
								try
								{
									if (node.ParentNode == null || node.ParentNode is XmlDocument)
									{
										_rootObject = obj;
										frm = obj as Form;
										if (frm != null)
										{
											frm.SuspendLayout();
										}
										if (designMode)
										{
											addObjectToMap(obj, node);
										}
										if (_onRootObjectCreated != null)
										{
											IComponent ir = obj as IComponent;
											if (ir != null)
											{
												_onRootObjectCreated(ir, node, this);
											}
										}
									}
									ReadObjectFromXmlNode(node, obj, type, parentObject);
									if (frm != null)
									{
										frm.ResumeLayout(false);
									}
								}
								catch (Exception err)
								{
									addErrStr2("Error reading [{0}]. {1}", obj.GetType().FullName, SerializerException.FormExceptionText(err));
								}
								finally
								{
									pop();
								}
							}
						}
					}
					if (obj != null && designMode)
					{
						addObjectToMap(obj, node);
					}
				}
				finally
				{
					if (parentObject != null)
					{
						pop();
					}
				}
				IOwnedObject oo = obj as IOwnedObject;
				if (oo != null)
				{
					oo.Owner = this.ObjectList.RootPointer;
				}
				return obj;
			}
		}
		public object ReadControl(XmlNode node, IList list, object parentObject)
		{
			if (node == null)
			{
				addErrStr("XmlNode is null when calling ReadObject");
				return null;
			}
			else
			{
				bool designMode = false;
				object obj = null;
				Type type = XmlUtil.GetLibTypeAttribute(node);
				if (type == null)
				{
					addErrStr2("ReadControl: cannot load type [{0}] from [{1}]", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetPath(node));
				}
				else
				{
					if (XmlUtil.IsValueType(type))
					{
						TypeConverter converter = TypeDescriptor.GetConverter(type);
						obj = converter.ConvertFromInvariantString(node.InnerText);
						logTrace("ReadControl: value is a value [{0}]", node.InnerText);
					}
					else
					{
						bool objExists = false;
						string name = XmlUtil.GetNameAttribute(node);
						designMode = XmlUtil.GetAttributeBoolDefFalse(node, XmlTags.XMLATT_designMode);
						if (designMode && _objectFactory != null)
						{
							try
							{
								Type designerType = type;
								if (!IsForCompile)
								{
									if (string.CompareOrdinal(node.Name, "Root") == 0)
									{
										if (type.IsSubclassOf(typeof(Control)))
										{
											object[] attrs0 = type.GetCustomAttributes(true);
											bool isRoot = false;
											for (int i = 0; i < attrs0.Length; i++)
											{
												System.ComponentModel.DesignerAttribute a = attrs0[i] as System.ComponentModel.DesignerAttribute;
												if (a != null)
												{
													if (a.DesignerBaseTypeName.StartsWith("System.ComponentModel.Design.IRootDesigner"))
													{
														isRoot = true;
														break;
													}
												}
											}
											if (!isRoot)
											{
												designerType = VPLUtil.GetDesignerType(type);
											}
										}
									}
								}
								if (_objMap != null)
								{
									obj = _objMap.GetObjectByName(name);
								}
								if (obj == null)
								{
									obj = _objectFactory.CreateInstance(designerType, name);
									logTrace("ReadControl: design mode {0}, {1}", designerType, name);
								}
								else
								{
									objExists = true;
									logTrace("ReadControl using existing object: design mode {0}, {1}", designerType, name);
								}
								IXmlNodeHolder xmlHolder = obj as IXmlNodeHolder;
								if (xmlHolder != null)
								{
									xmlHolder.DataXmlNode = node;
								}
							}
							catch (Exception err)
							{
								addErrStr(string.Format("Could not create type {0} by designer holder. name:{1}. {2}", type, name, SerializerException.FormExceptionText(err)));
							}
						}
						else
						{
							obj = createInstance(node, parentObject, type);
						}
						if (obj != null)
						{
							IXmlCodeReaderWriterHolder wh = obj as IXmlCodeReaderWriterHolder;
							if (wh != null)
							{
								wh.SetReader(this);
							}
							push(obj);
							try
							{
								ReadObjectFromXmlNode(node, obj, type, parentObject);
								if (!objExists)
								{
									list.Add(obj);
								}
							}
							catch
							{
								throw;
							}
							finally
							{
								pop();
							}
						}
					}
				}
				if (obj != null && designMode)
				{
					//any design mode object read from XML should have an ID
					uint id = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
					if (id != 0)
					{
						if (_objMap.ContainsKey(obj))
							_objMap[obj] = id;
						else
						{
							object v = _objMap.GetObjectByID(id);
							if (v != null)
							{
								if (v != obj)
								{
									throw new SerializerException("object {0} read twice", id);
								}
							}
							else
							{
								_objMap.Add(obj, id);
							}
						}
					}
				}
				return obj;
			}
		}
		public void ReadProperties(XmlNode node, object obj)
		{
			_propertiesNotSet = null;
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj, propertyAttributes);
			ReadProperties(node, obj, properties, XmlTags.XML_PROPERTY);
			DataTable tbl = obj as DataTable;
			if (tbl != null)
			{
				if (_propertiesNotSet != null)
				{
					foreach (KeyValuePair<PropertyDescriptor, object> kv in _propertiesNotSet)
					{
						if (string.Compare(kv.Key.Name, "PrimaryKey", StringComparison.Ordinal) == 0)
						{
							DataColumn[] dcc = kv.Value as DataColumn[];
							List<DataColumn> ldc = new List<DataColumn>();
							foreach (DataColumn dc in dcc)
							{
								bool b = false;
								for (int i = 0; i < tbl.Columns.Count; i++)
								{
									if (string.Compare(tbl.Columns[i].ColumnName, dc.ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
									{
										ldc.Add(tbl.Columns[i]);
										b = true;
										break;
									}
								}
								if (!b)
								{
									tbl.Columns.Add(dc);
									ldc.Add(dc);
								}
							}
							kv.Key.SetValue(tbl, ldc.ToArray());
						}
					}
				}
			}
		}
		public void ReadProperties(XmlNode node, object obj, PropertyDescriptorCollection properties, string elementName)
		{
			if (properties.Count > 0)
			{
				bool isCustomProperty = (properties[0] is IIdentityByInteger);
				logTraceIncIndent();
				XmlNodeList propNodeList0 = node.SelectNodes(elementName);
				SortedDictionary<string, XmlNode> propNodeList = new SortedDictionary<string, XmlNode>();
				foreach (XmlNode propNode in propNodeList0)
				{
					string key = XmlUtil.GetAttribute(propNode, "name");
					propNodeList.Add(key, propNode);
				}
				SortedList<UInt32, PropertyReadOrder> sortedProperties = new SortedList<UInt32, PropertyReadOrder>();
				UInt32 idx = 0;
				foreach (KeyValuePair<string,XmlNode> kv in propNodeList)
				{
					XmlNode propNode = kv.Value;
					PropertyDescriptor prop = null;
					if (isCustomProperty)
					{
						UInt64 id = XmlUtil.GetAttributeUInt64(propNode, XmlTags.XMLATT_PropID);
						foreach (PropertyDescriptor p in properties)
						{
							IIdentityByInteger iid = p as IIdentityByInteger;
							if (iid.WholeId == id)
							{
								prop = p;
								break;
							}
						}
					}
					else
					{
						//do not include FirstDisplayedCell and CurrentCell
						string sname = XmlUtil.GetNameAttribute(propNode);
						if (!string.IsNullOrEmpty(sname))
						{
							if (string.CompareOrdinal(sname, "FirstDisplayedCell") == 0 || string.CompareOrdinal(sname, "CurrentCell") == 0)
							{
								Type t = VPLUtil.GetObjectType(obj);
								if (typeof(DataGridView).IsAssignableFrom(t))
								{
									continue;
								}
							}
							prop = properties.Find(sname, false);
						}
					}
					if (prop != null)
					{
						//backward compatibility
						XmlIgnoreAttribute xi = (XmlIgnoreAttribute)prop.Attributes[typeof(XmlIgnoreAttribute)];
						if (xi != null)
						{
							IgnoreXmlIgnoreAttribute ixi = (IgnoreXmlIgnoreAttribute)prop.Attributes[typeof(IgnoreXmlIgnoreAttribute)];
							if (ixi == null)
							{
								continue;
							}
						}
						PropertyReadOrder pro = new PropertyReadOrder(prop, propNode, idx++, (UInt32)properties.Count);
						if (!pro.Exclude)
						{
							sortedProperties.Add(pro.ReadOrder, pro);
						}
					}
				}
				push(obj);
				try
				{
					IEnumerator<KeyValuePair<UInt32, PropertyReadOrder>> ie = sortedProperties.GetEnumerator();
					while (ie.MoveNext())
					{
						ReadProperty(ie.Current.Value.Property, ie.Current.Value.Node, obj, (obj is Form) && string.CompareOrdinal(node.Name,"Root")==0, elementName);
						if (VPLUtil.Shutingdown)
						{
							return;
						}
					}
				}
				finally
				{
					pop();
				}
				logTraceDecIndent();
			}
		}
		private Type _creatingConverter = null;
		public void ReadProperty(PropertyDescriptor prop, XmlNode node, object value, bool isTopForm, string elementName)
		{
			try
			{
				if (_lightRead)
				{
					if (XmlUtil.NotForLightRead(prop.Attributes))
					{
						return;
					}
				}
				logTrace("Property {0} {1}", prop.Name, prop.PropertyType);
				if (SerializeUtil.SaveAsObject(prop.Attributes))
				{
					XmlNode nd = node.SelectSingleNode(XmlTags.XML_Data);
					if (nd != null)
					{
						object v = ReadObject(nd, value);
						prop.SetValue(value, v);
					}
					return;
				}
				DesignerSerializationVisibilityAttribute visibility = (DesignerSerializationVisibilityAttribute)prop.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
				switch (visibility.Visibility)
				{
					case DesignerSerializationVisibility.Visible:
						logTrace("Visible");
						if (prop.PropertyType.IsArray)
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							if (nodes.Count > 0)
							{
								logTrace("Array item count {0}", nodes.Count);
								Array propValue = (Array)Activator.CreateInstance(prop.PropertyType, nodes.Count);
								ReadArray(nodes, propValue, value);
								DataTable tbl = value as DataTable;
								if (tbl != null)
								{
									if (string.Compare(prop.Name, "PrimaryKey", StringComparison.Ordinal) == 0)
									{
										if (_propertiesNotSet == null)
										{
											_propertiesNotSet = new Dictionary<PropertyDescriptor, object>();
										}
										_propertiesNotSet.Add(prop, propValue);
										return;
									}
								}
								prop.SetValue(value, propValue);
							}
						}
						else if (typeof(IList).IsAssignableFrom(prop.PropertyType))
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							if (nodes.Count > 0)
							{
								logTrace("IList item count {0}", nodes.Count);
								IList list = null;
								object propValue = prop.GetValue(value);
								if (propValue != null)
								{
									list = (IList)propValue;
								}
								else
								{
									if (prop.IsReadOnly)
									{
										addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"IList property {0} is null and read-only as {1}", prop.Name, prop.PropertyType));
									}
									else
									{
										if (SerializeUtil.HasParent(prop.PropertyType))
										{
											logTrace("creating IList object instance of type {0} , paramtere {1}", prop.PropertyType, value);
											list = (IList)Activator.CreateInstance(prop.PropertyType, value);
										}
										else
										{
											logTrace("creating IList object instance of type {0}", prop.PropertyType);
											list = (IList)Activator.CreateInstance(prop.PropertyType);
										}
										prop.SetValue(value, list);
										logTrace("created IList object instance");
									}
								}
								if (list != null)
								{
									if (value is Control && string.Compare(prop.Name, "Controls", StringComparison.Ordinal) == 0)
									{
										ReadControls(nodes, list, value);
									}
									else
									{
										ReadList(nodes, list, value);
									}
									ISerializerProcessor sp = list as ISerializerProcessor;
									if (sp != null)
									{
										sp.OnPostSerialize(_objMap, node, false, this);
									}
									IPostXmlNodeSerialize px = list as IPostXmlNodeSerialize;
									if (px != null)
									{
										px.OnReadFromXmlNode(this, node);
									}
								}
							}
						}
						else if (!prop.IsReadOnly || SerializeUtil.IgnoreReadOnly(prop) || SerializeUtil.IgnoreReadOnly(prop.PropertyType))
						{
							object obj;
							logTrace("reading value of type {0} ...", prop.PropertyType);
							TypeConverter tct = null;
							for (int i = 0; i < prop.Attributes.Count; i++)
							{
								TypeConverterAttribute tcta = prop.Attributes[i] as TypeConverterAttribute;
								if (tcta != null)
								{
									Type tc = Type.GetType(tcta.ConverterTypeName);
									if (tc != null)
									{
										ConstructorInfo cif = tc.GetConstructor(new Type[] { });
										if (cif != null)
										{
											if (_creatingConverter != null)
											{
												MessageBox.Show("Stackoverflow:" + _creatingConverter.FullName);
												_creatingConverter = null;
											}
											else
											{
												_creatingConverter = tc;
												tct = Activator.CreateInstance(tc) as TypeConverter;
												_creatingConverter = null;
												if (tct != null)
												{
													break;
												}
											}
										}
									}
								}
							}
							if (tct == null)
							{
								tct = TypeDescriptor.GetConverter(prop.PropertyType);
							}
							if (ReadValue(node, tct, prop, value, out obj))
							{
								try
								{
									IPropertyValueLinkHolder plh = value as IPropertyValueLinkHolder;
									IPropertyValueLink pl = obj as IPropertyValueLink;
									if (plh != null && pl != null)
									{
										plh.SetPropertyLink(prop.Name, pl);
									}
									else
									{
										prop.SetValue(value, obj);
									}
								}
								catch (TargetInvocationException)
								{
								}
#if DEBUG
#if ASRUNTIME
								catch
								{
#else
								catch (Exception err)
								{

									addErrStr2("Debug information only. Error reading property [{0}].[{1}]. {2}", value, prop.Name, SerializerException.FormExceptionText(err));
#endif
#else
								catch
								{
#endif
								}
							}
						}
						break;
					case DesignerSerializationVisibility.Content:
						logTrace("Content");
						if (typeof(IList).IsAssignableFrom(prop.PropertyType))
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							logTrace("IList item count:{0}", nodes.Count);
							if (nodes.Count > 0)
							{
								IList list = null;
								object propValue = prop.GetValue(value);
								if (propValue != null)
								{
									list = (IList)propValue;
								}
								else
								{
									if (prop.IsReadOnly)
									{
										addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"IList property {0} is null and read-only as {1}", prop.Name, prop.PropertyType));
									}
									else
									{
										try
										{
											list = (IList)Activator.CreateInstance(prop.PropertyType);
											logTrace("created IList instance");
										}
										catch (Exception e)
										{
											addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"IList property {0}({1}) is null and failed to create it. {2}", prop.Name, prop.PropertyType, e.Message));
										}
									}
								}
								if (list != null)
								{
									if (value is Control && string.Compare(prop.Name, "Controls", StringComparison.Ordinal) == 0)
									{
										ReadControls(nodes, list, value);
									}
									else
									{
										bool canSave = true;
										if (value is DataGridView && string.Compare(prop.Name, "Columns", StringComparison.Ordinal) == 0)
										{
											canSave = DesignTimeColumnsHolderAttribute.IsDesignTimeColumnsHolder(value);
										}
										if (canSave)
										{
											ReadList(nodes, list, value);
										}
									}
								}
							}
						}
						else if (typeof(ICollection).IsAssignableFrom(prop.PropertyType))
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							logTrace("ICollection item count:{0}", nodes.Count);
							if (nodes.Count > 0)
							{
								if (typeof(ControlBindingsCollection).Equals(prop.PropertyType))
								{
									foreach (XmlNode nd in nodes)
									{
										string member = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_member);
										string propName = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_PropID);
										XmlNode nr = nd.SelectSingleNode(XmlTags.XML_Reference);
										string dataSourceName = null;
										if (nr != null)
										{
											dataSourceName = XmlUtil.GetNameAttribute(nr);
										}
										if (_bindings == null)
										{
											_bindings = new Dictionary<object, Dictionary<PropertyDescriptor, List<BindingLoader>>>();
										}
										if (!string.IsNullOrEmpty(dataSourceName) && !string.IsNullOrEmpty(member) && !string.IsNullOrEmpty(propName))
										{
											Dictionary<PropertyDescriptor, List<BindingLoader>> plist;
											if (!_bindings.TryGetValue(value, out plist))
											{
												plist = new Dictionary<PropertyDescriptor, List<BindingLoader>>();
												_bindings.Add(value, plist);
											}
											List<BindingLoader> list;
											if (!plist.TryGetValue(prop, out list))
											{
												list = new List<BindingLoader>();
												plist.Add(prop, list);
											}
											list.Add(new BindingLoader(member, propName, dataSourceName));
										}
									}
								}
								else
								{
									Dictionary<Type, MethodInfo> mifList = new Dictionary<Type, MethodInfo>();
									MethodInfo[] mifs = prop.PropertyType.GetMethods();
									for (int i = 0; i < mifs.Length; i++)
									{
										if (string.Compare(mifs[i].Name, "Add", StringComparison.Ordinal) == 0)
										{
											ParameterInfo[] pifs = mifs[i].GetParameters();
											if (pifs.Length == 1)
											{
												mifList.Add(pifs[0].ParameterType, mifs[i]);
											}
										}
									}
									object propValue = prop.GetValue(value);
									if (propValue != null && mifList.Count > 0)
									{
										ReadCollection(nodes, propValue, mifList, value);
									}
									else
									{
										ArrayList al = new ArrayList();
										int nCount = ReadCollection(nodes, al, value);
										if (nCount > 0)
										{
											ICollection list = (ICollection)Activator.CreateInstance(prop.PropertyType, al.ToArray());
											prop.SetValue(value, list);
										}
									}
								}
							}
						}
						else
						{
							logTrace("read properties");
							object propValue = prop.GetValue(value);
							ReadProperties(node, propValue);
						}
						break;
					default:
						//normally we should not reach here because we only save for the above two cases
						logTrace("not read for {0}", visibility.Visibility);
						break;
				}
			}
			catch (Exception err)
			{
				addErrStr2("Error reading property {0}.{1}. {2}\r\nXml path:{3}", value, prop.Name, SerializerException.FormExceptionText(err), XmlUtil.GetPath(node));
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="node">the Contents node</param>
		/// <param name="ccs"></param>
		private void ReadCustomValue(XmlNode node, ICustomContentSerialization ccs)
		{
			XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
			if (nodes.Count > 0)
			{
				Dictionary<string, object> vs = new Dictionary<string, object>();
				foreach (XmlNode nd in nodes)
				{
					string key = XmlUtil.GetNameAttribute(nd);
					object v = ReadObject(nd, ccs);
					vs.Add(key, v);
				}
				ccs.CustomContents = vs;
			}
		}
		public string GetName(XmlNode node)
		{
			return XmlUtil.GetNameAttribute(node);
		}
		public byte[] ReadBinary(XmlNode binNode)
		{
			byte[] data = null;
			string binId = XmlUtil.GetAttribute(binNode, XmlTags.XMLATT_ResId);
			if (!string.IsNullOrEmpty(binId))
			{
				XmlDocument docAux = XmlObjectWriter.getAuxDocument(binNode.OwnerDocument);
				if (docAux != null)
				{
					binId = binId.ToLowerInvariant();
					XmlNode resNode = docAux.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"//{0}/{1}[@{2}='{3}']", XmlTags.XML_BINS, XmlTags.XML_Binary, XmlTags.XMLATT_ResId, binId));
					if (resNode != null)
					{
						data = Convert.FromBase64String(resNode.InnerText);
					}
				}
			}
			if (data == null)
			{
				data = Convert.FromBase64String(binNode.InnerText);
			}
			return data;
		}
		public T ReadValue<T>(XmlNode node, object parentObject)
		{
			object v;
			if (typeof(Type).Equals(typeof(T)))
			{
				XmlNode nodeLib = node.SelectSingleNode(XmlTags.XML_LIBTYPE);
				if (nodeLib != null)
				{
					v = XmlUtil.GetLibTypeAttribute(nodeLib);
					if (v != null)
					{
						return (T)v;
					}
				}
			}
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
			if (ReadValue(node, converter, null, parentObject, out v))
			{
				return (T)v;
			}
			return default(T);
		}
		public object ReadValue(XmlNode node, Type t, object parentObject)
		{
			object v;
			if (typeof(Type).Equals(t))
			{
				XmlNode nodeLib = node.SelectSingleNode(XmlTags.XML_LIBTYPE);
				if (nodeLib != null)
				{
					v = XmlUtil.GetLibTypeAttribute(nodeLib);
					if (v != null)
					{
						return v;
					}
				}
			}
			TypeConverter converter = TypeDescriptor.GetConverter(t);
			if (ReadValue(node, converter, null, parentObject, out v))
			{
				return v;
			}
			return VPLUtil.GetDefaultValue(t);
		}
		public object ReadValue(XmlNode node)
		{
			return ReadValue(node, null);
		}
		public object ReadValue(XmlNode node, object parentObject)
		{
			if (XmlUtil.GetAttributeBoolDefFalse(node, XmlTags.XMLATT_IsNull))
			{
				return null;
			}
			Type t = XmlUtil.GetLibTypeAttribute(node);
			if (t == null)
			{
				return node.InnerText;
			}
			else
			{
				return ReadValue(node, parentObject, t);
			}
		}
		public object ReadValue(XmlNode node, object parentObject, Type t)
		{
			if (t == null)
			{
				t = XmlUtil.GetLibTypeAttribute(node);
			}
			if (t == null)
			{
				addErrStr2("Cannot load type [{0}] from [{1}] .2", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetAttribute(node, XmlTags.XMLATT_filename));
			}
			else
			{
				object v;
				if (typeof(Type).Equals(t))
				{
					XmlNode nodeLib = node.SelectSingleNode(XmlTags.XML_LIBTYPE);
					if (nodeLib != null)
					{
						v = XmlUtil.GetLibTypeAttribute(nodeLib);
						if (v != null)
						{
							return v;
						}
					}
				}
				TypeConverter converter = TypeDescriptor.GetConverter(t);
				if (ReadValue(node, converter, null, parentObject, out v))
				{
					return v;
				}
				return VPLUtil.GetDefaultValue(t);
			}
			return null;
		}
		private bool parseString(string txt, TypeConverter converter, PropertyDescriptor prop, object parentObject, out object value)
		{
			IFromString fs = parentObject as IFromString;
			if (fs != null && prop != null && string.CompareOrdinal(prop.Name, "ConstantValue") == 0)
			{
				value = fs.FromString(txt);
				return true;
			}
			if (prop != null && prop.Converter != null)
			{
				if (prop.Converter.CanConvertFrom(typeof(string)))
				{
					value = prop.Converter.ConvertFrom(new GenericConverterContext(prop, parentObject), CultureInfo.InvariantCulture, txt);
					return true;
				}
			}
			if (XmlUtil.GetConversionSupported(converter, typeof(string)))
			{
				GenericConverterContext gcc = null;
				if (prop != null)
				{
					if (prop.ComponentType.Name == "DTSSourceText" && prop.Name == "HasHeader")
					{
					}
					object v = prop.GetValue(parentObject);
					fs = v as IFromString;
					if (fs != null)
					{
						value = fs.FromString(txt);
						return true;
					}
					if (typeof(long).Equals(prop.PropertyType))
					{
						long nVal;
						if (long.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(ulong).Equals(prop.PropertyType))
					{
						ulong nVal;
						if (ulong.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(int).Equals(prop.PropertyType))
					{
						int nVal;
						if (int.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(uint).Equals(prop.PropertyType))
					{
						uint nVal;
						if (uint.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(short).Equals(prop.PropertyType))
					{
						short nVal;
						if (short.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(ushort).Equals(prop.PropertyType))
					{
						ushort nVal;
						if (ushort.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(sbyte).Equals(prop.PropertyType))
					{
						sbyte nVal;
						if (sbyte.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(byte).Equals(prop.PropertyType))
					{
						byte nVal;
						if (byte.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(double).Equals(prop.PropertyType))
					{
						double nVal;
						if (double.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(float).Equals(prop.PropertyType))
					{
						float nVal;
						if (float.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					else if (typeof(decimal).Equals(prop.PropertyType))
					{
						decimal nVal;
						if (decimal.TryParse(txt, out nVal))
						{
							value = nVal;
							return true;
						}
						else
						{
							value = v;
							return false;
						}
					}
					gcc = new GenericConverterContext(prop, parentObject);
				}
				value = converter.ConvertFromString(gcc, CultureInfo.InvariantCulture, txt);
				return true;
			}
			else
			{
				TypeConverterNoExpand ne = converter as TypeConverterNoExpand;
				if (ne != null)
				{
					value = txt;
				}
				else
				{
					value = converter.ConvertFromInvariantString(txt);
				}
				return true;
			}
		}

		/// <summary>
		/// Generic function to read an object value.  Returns true if the read succeeded.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="converter"></param>
		/// <param name="prop">can be null</param>
		/// <param name="parentObject"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		private bool ReadValue(XmlNode node, TypeConverter converter, PropertyDescriptor prop, object parentObject, out object value)
		{
			try
			{
				if (XmlUtil.GetAttributeBoolDefFalse(node, XmlTags.XMLATT_array))
				{
					Type t = XmlUtil.GetLibTypeAttribute(node);
					if (t == null)
					{
						addErrStr("Error ready array value. The type not set");
						value = null;
						return false;
					}
					else
					{
						XmlNodeList itemNodes = node.SelectNodes(XmlTags.XML_Item);
						Array a = Array.CreateInstance(t.GetElementType(), itemNodes.Count);
						ReadArray(itemNodes, a, parentObject);
						value = a;
						return true;
					}
				}
				foreach (XmlNode child in node.ChildNodes)
				{
					if (child.Name.Equals(XmlTags.XML_LIBTYPE))
					{
						value = XmlUtil.GetLibTypeAttribute(child);
						if (value == null)
						{
							addErrStr2("Cannot load type [{0}] from [{1}] .3", XmlUtil.GetLibTypeAttributeString(child), XmlUtil.GetAttribute(child, XmlTags.XMLATT_filename));
						}
						else
						{
							IPostRootDeserialize iprd = value as IPostRootDeserialize;
							if (iprd != null)
							{
								AddPostRootDeserializer(iprd);
							}
						}
						return true;
					}
					if (child.Name.Equals(XmlTags.XML_Binary))
					{
						byte[] data = ReadBinary(child);
						if (data == null || data.Length == 0)
						{
							value = null;
							return true;
						}
						// Binary blob.  Now, check to see if the type converter
						// can convert it.  If not, use serialization.
						//
						if (XmlUtil.GetConversionSupported(converter, typeof(byte[])))
						{
							value = converter.ConvertFrom(null, CultureInfo.InvariantCulture, data);
							if (value != null)
							{
								ISerializerProcessor sp = value as ISerializerProcessor;
								if (sp != null)
								{
									sp.OnPostSerialize(_objMap, child, false, this);
								}
								IPostXmlNodeSerialize px = value as IPostXmlNodeSerialize;
								if (px != null)
								{
									px.OnReadFromXmlNode(this, node);
								}
							}
							IPostRootDeserialize iprd = value as IPostRootDeserialize;
							if (iprd != null)
							{
								AddPostRootDeserializer(iprd);
							}
							return true;
						}
						else
						{
							BinaryFormatter formatter = new BinaryFormatter();
							MemoryStream stream = new MemoryStream(data);
							value = formatter.Deserialize(stream);
							if (value != null)
							{
								ISerializerProcessor sp = value as ISerializerProcessor;
								if (sp != null)
								{
									sp.OnPostSerialize(_objMap, child, false, this);
								}
								IPostXmlNodeSerialize px = value as IPostXmlNodeSerialize;
								if (px != null)
								{
									px.OnReadFromXmlNode(this, node);
								}
							}
							IPostRootDeserialize iprd = value as IPostRootDeserialize;
							if (iprd != null)
							{
								AddPostRootDeserializer(iprd);
							}
							return true;
						}
					}
					else if (child.Name.Equals(XmlTags.XML_ObjProperty))
					{
						value = ReadObject(child, parentObject);
						if (value != null)
						{
							ISerializerProcessor sp = value as ISerializerProcessor;
							if (sp != null)
							{
								sp.OnPostSerialize(_objMap, child, false, this);
							}
							IPostXmlNodeSerialize px = value as IPostXmlNodeSerialize;
							if (px != null)
							{
								px.OnReadFromXmlNode(this, node);
							}
							IPostRootDeserialize iprd = value as IPostRootDeserialize;
							if (iprd != null)
							{
								AddPostRootDeserializer(iprd);
							}
						}
						return (value != null);
					}
					else if (child.Name.Equals(XmlTags.XML_InstanceDescriptor))
					{
						value = ReadInstanceDescriptor(child);
						return (value != null);
					}
					else if (child.Name.Equals(XmlTags.XML_Reference))
					{
						string sref = XmlUtil.GetNameAttribute(child);
						if (_references == null)
						{
							_references = new Dictionary<object, Dictionary<PropertyDescriptor, string>>();
						}
						Dictionary<PropertyDescriptor, string> propValue;
						if (!_references.TryGetValue(parentObject, out propValue))
						{
							propValue = new Dictionary<PropertyDescriptor, string>();
							_references.Add(parentObject, propValue);
						}
						if (prop != null)
						{
							propValue.Add(prop, sref);
						}
						value = null;
						return false; //do not set the value
					}
					else if (child.Name.Equals(XmlTags.XML_Data))
					{
						bool bIsReadOnly = false;
						try
						{
							if (prop != null)
							{
								bIsReadOnly = prop.IsReadOnly;
							}
						}
						catch (Exception err)
						{
							addErrStr(string.Format(CultureInfo.InvariantCulture, "Error reading value on checking readonly attribute:{0}", err.Message));
						}
						if (bIsReadOnly)
						{
							value = null;
							try
							{
								value = prop.GetValue(parentObject);
							}
							catch
							{
							}
							if (value == null)
							{
								value = createInstance(node, parentObject, null);
							}
						}
						else
						{
							value = createInstance(node, parentObject, null);
						}
						if (value == null)
						{
							return false;
						}
						if (!(value is IClassPointer) && !(value is IGuidIdentified))
						{
							push(value);
							try
							{
								IDelayedInitialize di = value as IDelayedInitialize;
								if (di != null)
								{
									AddDelayedInitializer(di, node);
								}
								IXmlNodeSerializable xmlReader = value as IXmlNodeSerializable;
								if (xmlReader != null)
								{
									xmlReader.OnReadFromXmlNode(this, child);
								}
								else
								{
									ICustomSerialization cs = value as ICustomSerialization;
									if (cs != null)
									{
										cs.OnReadFromXmlNode(this, child);
									}
									else
									{
										IXmlNodeSerializable xs = value as IXmlNodeSerializable;
										if (xs != null)
										{
											xs.OnReadFromXmlNode(this, child);
										}
									}
								}
								ISerializerProcessor sp = value as ISerializerProcessor;
								if (sp != null)
								{
									sp.OnPostSerialize(_objMap, child, false, this);
								}
								IPostXmlNodeSerialize px = value as IPostXmlNodeSerialize;
								if (px != null)
								{
									px.OnReadFromXmlNode(this, node);
								}
							}
							catch
							{
								throw;
							}
							finally
							{
								pop();
							}
						}
						IPostRootDeserialize iprd = value as IPostRootDeserialize;
						if (iprd != null)
						{
							AddPostRootDeserializer(iprd);
						}
						return true;
					}
					else if (child.Name.Equals(XmlTags.XML_Content))
					{
						value = createInstance(node, parentObject, null);
						if (value != null)
						{
							push(value);
							try
							{
								ICustomContentSerialization ccs = value as ICustomContentSerialization;
								if (ccs != null)
								{
									ReadCustomValue(child, ccs);
								}
								IXmlNodeSerializable xs = value as IXmlNodeSerializable;
								if (xs != null)
								{
									xs.OnReadFromXmlNode(this, node);
								}
								ISerializerProcessor sp = value as ISerializerProcessor;
								if (sp != null)
								{
									sp.OnPostSerialize(_objMap, child, false, this);
								}
								IPostXmlNodeSerialize px = value as IPostXmlNodeSerialize;
								if (px != null)
								{
									px.OnReadFromXmlNode(this, node);
								}
							}
							catch
							{
								throw;
							}
							finally
							{
								pop();
							}
							IPostRootDeserialize iprd = value as IPostRootDeserialize;
							if (iprd != null)
							{
								AddPostRootDeserializer(iprd);
							}
							return true;
						}
					}
					else if (child.NodeType == XmlNodeType.Text)
					{
						try
						{
							if (parseString(node.InnerText, converter, prop, parentObject, out value))
							{
								if (prop != null && typeof(string).Equals(prop.PropertyType))
								{
									if (FilePathAttribute.IsFilePath(prop))
									{
										string sf = value as string;
										if (!string.IsNullOrEmpty(sf))
										{
											if (sf.StartsWith(PRJPATHSYMBOLE, StringComparison.Ordinal))
											{
												sf = sf.Substring(PRJPATHSYMBOLE.Length);
												if (_prj != null)
												{
													sf = Path.Combine(_prj.ProjectFolder, sf);
												}
												value = sf;
											}
										}
									}
								}
								return true;
							}
							return false;
						}
						catch (Exception erv)
						{
							addErrStr(string.Format("Read value from element type {0} failed. {1}. \r\nXmlPath:{2}", child.Name, SerializerException.FormExceptionText(erv), XmlUtil.GetPath(node)));
							value = null;
							return false;
						}
					}
					else if (child.NodeType == XmlNodeType.CDATA)
					{
						XmlCDataSection cd = child as XmlCDataSection;
						try
						{
							return parseString(cd.Value, converter, prop, parentObject, out value);
						}
						catch (Exception erv)
						{
							addErrStr(string.Format("Read value from element type {0} failed. {1}. \r\nXmlPath:{2}", child.Name, SerializerException.FormExceptionText(erv), XmlUtil.GetPath(node)));
							value = null;
							return false;
						}
					}
					else if (child.NodeType == XmlNodeType.Whitespace)
					{
						if (typeof(string).Equals(prop.PropertyType))
						{
							value = child.InnerText;
							return true;
						}
					}
					else
					{
						if (string.CompareOrdinal(child.Name, "Item") == 0)
						{
							value = child.InnerText;
							return true;
						}
						else
						{
							addErrStr(string.Format("Unexpected element type {0}", child.Name));
							value = null;
							return false;
						}
					}
				}
				// If we get here, it is because there were no nodes.  No nodes and no inner
				// text is how we signify null.
				//
				value = null;
				return true;
			}
			catch (Exception ex)
			{
				addErrStr(SerializerException.FormExceptionText(ex));
				value = null;
				return false;
			}
		}
		private object ReadInstanceDescriptor(XmlNode node)
		{
			// First, need to deserialize the member
			//
			string memberAttr = XmlUtil.GetAttribute(node, XmlTags.XMLATT_member);
			if (string.IsNullOrEmpty(memberAttr))
			{
				addErrStr("No member attribute on instance descriptor");
				return null;
			}
			else
			{
				byte[] data = Convert.FromBase64String(memberAttr);
				BinaryFormatter formatter = new BinaryFormatter();
				MemoryStream stream = new MemoryStream(data);
				MemberInfo mi = (MemberInfo)formatter.Deserialize(stream);
				object[] args = null;

				// Check to see if this member needs arguments.  If so, gather
				// them from the XML.
				if (mi is MethodBase)
				{
					ParameterInfo[] paramInfos = ((MethodBase)mi).GetParameters();

					args = new object[paramInfos.Length];

					int idx = 0;

					foreach (XmlNode child in node.ChildNodes)
					{
						if (child.Name.Equals(XmlTags.XML_Argument))
						{
							object value;
							if (!ReadValue(child, TypeDescriptor.GetConverter(paramInfos[idx].ParameterType), null, null, out value))
							{
								return null;
							}

							args[idx++] = value;
						}
					}

					if (idx != paramInfos.Length)
					{
						addErrStr(string.Format("Member {0} requires {1} arguments, not {2}.", mi.Name, args.Length, idx));
						return null;
					}
				}

				InstanceDescriptor id = new InstanceDescriptor(mi, args);
				object instance = id.Invoke();

				// Ok, we have our object.  Now, check to see if there are any properties, and if there are, 
				// set them.
				//
				ReadProperties(node, instance);
				//
				return instance;
			}
		}
		private void ReadList(XmlNodeList nodes, IList list, object parentObject)
		{
			push(parentObject);
			try
			{
				logTraceIncIndent();
				list.Clear();
				foreach (XmlNode nd in nodes)
				{
					object obj;
					if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
					{
						obj = XmlUtil.GetLibTypeAttribute(nd);
					}
					else
					{
						obj = ReadObject(nd, parentObject);
					}
					if (obj != null)
					{
						list.Add(obj);
					}
				}
				logTraceDecIndent();
			}
			catch
			{
				throw;
			}
			finally
			{
				pop();
			}
		}
		private void ReadControls(XmlNodeList nodes, IList list, object parentObject)
		{
			logTraceIncIndent();
			foreach (XmlNode nd in nodes)
			{
				ReadControl(nd, list, parentObject);
				if (VPLUtil.Shutingdown)
				{
					return;
				}
			}
			logTraceDecIndent();
		}
		private void ReadCollection(XmlNodeList nodes, object list, Dictionary<Type, MethodInfo> mifList, object parentObject)
		{
			logTraceIncIndent();
			foreach (XmlNode nd in nodes)
			{
				object obj;
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					obj = XmlUtil.GetLibTypeAttribute(nd);
				}
				else
				{
					obj = ReadObject(nd, parentObject);
				}
				if (obj != null)
				{
					MethodInfo add;
					if (mifList.TryGetValue(obj.GetType(), out add))
					{
						add.Invoke(list, new object[] { obj });
					}
					else
					{
						addErrStr2("Method {0}.Add({1}) not found", list.GetType(), obj.GetType());
					}
				}
			}
			logTraceDecIndent();
		}
		private int ReadCollection(XmlNodeList nodes, ArrayList list, object parentObject)
		{
			int nCount = 0;
			logTraceIncIndent();
			foreach (XmlNode nd in nodes)
			{
				object obj;
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					obj = XmlUtil.GetLibTypeAttribute(nd);
				}
				else
				{
					obj = ReadObject(nd, parentObject);
				}
				if (obj != null)
				{
					list.Add(obj);
					nCount++;
				}
			}
			logTraceDecIndent();
			return nCount;
		}
		public void ReadArray(XmlNodeList nodes, Array list, object parentObject)
		{
			logTraceIncIndent();
			for (int i = 0; i < nodes.Count; i++)
			{
				XmlNode nd = nodes[i];
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					list.SetValue(XmlUtil.GetLibTypeAttribute(nd), i);
				}
				else
				{
					if (!typeof(void).Equals(XmlUtil.GetLibTypeAttribute(nd)))
					{
						object obj = ReadObject(nd, parentObject);
						if (obj != null)
						{
							list.SetValue(obj, i);
						}
					}
				}
			}
			logTraceDecIndent();
		}
		const string CLASSPOINT = "IClassPointer";
		private bool tryGetIClassPointer(XmlNode node, Type t, out IClassPointer p)
		{
			if (typeof(IClassPointer).Equals(t.GetInterface(CLASSPOINT)))
			{
				UInt32 classId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
				if (classId == 0)
				{
					XmlNode dataNode = node.SelectSingleNode(XmlTags.XML_Data);
					if (dataNode != null)
					{
						classId = XmlUtil.GetAttributeUInt(dataNode, XmlTags.XMLATT_ClassID);
					}
				}
				if (classId == 0)
				{
					throw new XmlSerializerException("Class ID is missing in node [{0}]", XmlUtil.GetPath(node));
				}
				p = ObjectIDmap.DesignService.GetClassPointerFromCache(_objMap.Project, classId);
				return (p != null);
			}
			else
			{
				p = null;
			}
			return false;
		}
		private object createInstance(XmlNode node, object parentObject, Type t)
		{
			if (t == null)
			{
				t = XmlUtil.GetLibTypeAttribute(node);
				if (t == null)
				{
					addErrStr("Could not create instance. Type is null");
					return null;
				}
			}
			if (t.GetInterface("IPropertyValueLink") != null)
			{
				if (parentObject != null)
				{
					string name = XmlUtil.GetNameAttribute(node);
					if (string.IsNullOrEmpty(name))
					{
						addErrStr("Could not create property value link. Name is null");
						return null;
					}
					return Activator.CreateInstance(t, parentObject, name);
				}
			}
			IClassPointer cp;
			if (!InternalTypeAttribute.IsInternalType(t))
			{
				if (tryGetIClassPointer(node, t, out cp))
				{
					logTrace("Got IClassPointer {0} from cache", cp.ClassId);
					return cp;
				}
			}
			if (t.GetInterface("IGuidIdentified") != null)
			{
				if (_objMap != null)
				{
					cp = _objMap.RootPointer;
					if (cp != null)
					{
						string g = XmlUtil.GetAttribute(node, XmlTags.XMLATT_guid);
						if (string.IsNullOrEmpty(g))
						{
							XmlNode dataNode = node.SelectSingleNode(XmlTags.XML_Data);
							if (dataNode != null)
							{
								g = XmlUtil.GetAttribute(dataNode, XmlTags.XMLATT_guid);
							}
						}
						if (!string.IsNullOrEmpty(g))
						{
							Guid guid = new Guid(g);
							IGuidIdentified ig = cp.GetElementByGuid(guid);
							if (ig != null)
							{
								return ig;
							}
						}
					}
				}
			}
			try
			{
				object v;
				if (SerializeUtil.HasParent(t))
				{
					try
					{
						object p = parentObject;
						if (p != null)
						{
							ConstructorInfo cif = t.GetConstructor(new Type[] { p.GetType() });
							if (cif == null)
							{
								UInt32 ownerId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ownerMethodId);
								if (ownerId != 0)
								{
									p = _objMap.RootPointer.GetOwnerMethod(ownerId);
									if (p != null)
									{
										cif = t.GetConstructor(new Type[] { p.GetType() });
									}
								}
							}
							if (cif == null)
							{
								object[] vs = t.GetCustomAttributes(typeof(LookupOwnerStatckOnReadAttribute), true);
								if (vs != null && vs.Length > 0)
								{
									if (_objectStack != null && _objectStack.Count > 0)
									{
										IEnumerator ie = _objectStack.GetEnumerator();
										while (ie.MoveNext())
										{
											IOwnerProviderConstructorChild opc = ie.Current as IOwnerProviderConstructorChild;
											if (opc != null)
											{
												object o = opc.GetChildConstructorOwner();
												if (o != null)
												{
													cif = t.GetConstructor(new Type[] { o.GetType() });
													if (cif != null)
													{
														p = o;
														break;
													}
												}
											}
										}
									}
								}
							}
							if (cif == null)
							{
								p = _objMap.RootPointer;
								cif = t.GetConstructor(new Type[] { p.GetType() });
							}
							if (cif != null)
							{
								v = Activator.CreateInstance(t, p);
							}
							else
							{
								v = Activator.CreateInstance(t);
							}
						}
						else
						{
							v = Activator.CreateInstance(t);
						}
					}
					catch (Exception err)
					{
						if (parentObject == null)
						{
							addErrStr(string.Format("Could not create instance {0}(null). {1}", t.FullName, SerializerException.FormExceptionText(err)));
						}
						else
						{
							addErrStr(string.Format("Could not create instance {0}({1}). {2}", t, parentObject.GetType().FullName, SerializerException.FormExceptionText(err)));
						}
						v = null;
					}
				}
				else
				{
					try
					{
						if (canCreateInstance(t))
						{
							if (t.IsArray)
							{
								v = Array.CreateInstance(t.GetElementType(), 0);
							}
							else
							{
								v = Activator.CreateInstance(t);
							}
						}
						else
						{
#if DEBUG
#if ASRUNTIME
#else
							addErrStr(string.Format("Debug information only. Could not create instance {0}(). Parameterless constructor not found. \r\nXml path:{1}", t, XmlUtil.GetPath(node)));
#endif
#endif
							v = null;
						}
					}
					catch (Exception err)
					{
						addErrStr(string.Format("Could not create instance {0}(). {1}. \r\nXml path:{2}", t, SerializerException.FormExceptionText(err), XmlUtil.GetPath(node)));
						v = null;
					}
				}
				if (v != null)
				{
					IMethod m = v as IMethod;
					if (m != null)
					{
						m.ModuleProject = this.ObjectList.Project;
					}
					IXmlNodeHolder xmlHolder = v as IXmlNodeHolder;
					if (xmlHolder != null)
					{
						xmlHolder.DataXmlNode = node;
					}
					ISerializeParent sp = parentObject as ISerializeParent;
					if (sp != null)
					{
						sp.OnMemberCreated(v);
					}
					IBeforeSerializeNotify bs = v as IBeforeSerializeNotify;
					if (bs != null)
					{
						bs.OnBeforeRead(this, node);
					}
					IUseClassId uc = v as IUseClassId;
					if (uc != null)
					{
						uc.SetClassId(_objMap.ClassId);
					}
				}
				return v;
			}
			catch (Exception e)
			{
				addErrStr2("cannot create instance for {0}. {1}. \r\nXml path:{2}", t.FullName, SerializerException.FormExceptionText(e), XmlUtil.GetPath(node));
			}
			return null;
		}
		private bool canCreateInstance(Type t)
		{
			if (t.IsValueType)
				return true;
			if (t.IsSerializable)
				return true;
			if (t.Equals(typeof(string)))
				return true;
			ConstructorInfo cif = t.GetConstructor(Type.EmptyTypes);
			if (cif != null)
			{
				return true;
			}
			return false;
		}
		private void addObjectToMap(object obj, XmlNode node)
		{
			//any design mode object read from XML should have an ID
			uint id = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
			if (id != 0)
			{
				if (_objMap.ContainsKey(obj))
					_objMap[obj] = id;
				else
				{
					object v = _objMap.GetObjectByID(id);
					if (v != null)
					{
						if (v != obj)
						{
							throw new SerializerException("object {0} read twice", id);
						}
					}
					else
					{
						_objMap.Add(obj, id);
					}
				}
			}
		}
	}
}
