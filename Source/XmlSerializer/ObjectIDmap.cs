/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Project Serialization in XML
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using VSPrj;
using System.Collections.ObjectModel;
using System.Drawing;
using System.ComponentModel;
using System.Collections;
using ProgElements;
using XmlUtility;
using VPL;
using System.Windows.Forms;
using System.Globalization;

namespace XmlSerializer
{
	public interface ISerializerProcessor
	{
		void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer);
	}
	/// <summary>
	/// implemented by ClassInstancePointer,
	/// it represents an instance of a declaring class, which is a component member of a hosting class.
	/// it is loaded into ObjectIDmap to build a one-to-one map between the hosted component, which is
	/// created using the base library type, to the data represented by this interface.
	/// In all other situations, a class is refered to by a ClassPointer instance. For example, TreeNodes 
	/// use ClassPointer to represent the class data, not IClassRef.
	/// IClassRef refers to an instance
	/// ClassPointer refers to a definition
	/// </summary>
	public interface IClassRef
	{
		/// <summary>
		/// for definition class
		/// </summary>
		UInt32 ClassId { get; set; }
		/// <summary>
		/// unique id among instance host 
		/// </summary>
		UInt32 MemberId { get; set; }
		/// <summary>
		/// formed by (MemberId, ClassId)
		/// </summary>
		UInt64 WholeId { get; set; }
		/// <summary>
		/// the class id for the class using this instance as a component.
		/// ClassInstancePointer uses a ClassPointer to represent the hosting class
		/// </summary>
		UInt32 InstanceHostClassId { get; }
		/// <summary>
		/// same type of ClassPointer.ObjectInstance but a different instance.
		/// </summary>
		object ObjectInstance { get; set; }
		Image ImageIcon { get; set; }
		PropertyDescriptorCollection Properties { get; }
		void LoadProperties(XmlObjectReader xr);
		void HookCustomPropertyValueChange(EventHandler h);
		bool NameUsed(string name);
		/// <summary>
		/// for compiling
		/// </summary>
		string TypeString { get; }
	}
	public interface IDesignService
	{
		IClassPointer CreateClassPointer(ObjectIDmap map);
		IClassPointer GetClassPointerFromCache(ObjectIDmap map);
		IClassPointer GetClassPointerFromCache(LimnorProject proj, UInt32 classId);
	}
	/// <summary>
	/// mapping between object instances and their id's.
	/// for custom class instances also map to their ClassInstancePointer
	/// </summary>
	public class ObjectIDmap : Dictionary<object, uint>
	{
		#region fields and constructors
		private UInt64 _wholeId;
		private UInt64 _instanceId; //identify property or variable represented by ClassRef
		private string _projectFile;
		private List<ObjectIDmap> _childMaps;
		private XmlNode _xml;
		private XmlObjectReader _reader;
		private LimnorProject _project;
		//custom class instances map to their ClassInstancePointer
		private Dictionary<object, IClassRef> _classRefMap;
		private Dictionary<UInt32, Type> _typeChanges;
		private TreeRoot _treeRoot;
		private bool _isDisposed;
		private List<IPostDeserializeProcess> _postProcessors;
		private string _classIds;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="prj">project</param>
		/// <param name="componentId">root component id</param>
		/// <param name="data">root xml node</param>
		public ObjectIDmap(LimnorProject prj, UInt64 componentId, XmlNode data)
		{
			_wholeId = componentId;
			_xml = data;
			_project = prj;
		}
		public ObjectIDmap(LimnorProject prj, ObjectIDmap parent, UInt64 componentId, XmlNode data)
			: this(prj, componentId, data)
		{
			ParanetMap = parent;
			if (parent != null)
			{
				parent.AddChildMap(this);
			}
		}
		#endregion
		#region Public Methods
		public void AddPostProcessor(IPostDeserializeProcess obj)
		{
			if (_postProcessors == null)
			{
				_postProcessors = new List<IPostDeserializeProcess>();
			}
			if (!_postProcessors.Contains(obj))
			{
				_postProcessors.Add(obj);
			}
		}
		public IList<IPostDeserializeProcess> GetPostProcessors()
		{
			return _postProcessors;
		}
		public void ClearPostPprocessors()
		{
			_postProcessors = null;
		}
		public object LoadObjects()
		{
			_treeRoot = null;
			DesignerLoaderHost dlh = new DesignerLoaderHost();
			ComponentFactory of = new ComponentFactory();
			of.DesignerLoaderHost = dlh;
			of.ComponentContainer = dlh;
			object o = Reader.ReadRootObject(of, _xml);
			return o;
		}
		public void Dispose()
		{
			_isDisposed = true;
			if (_classRefMap != null)
			{
				_classRefMap.Clear();
				_classRefMap = null;
			}
			if (_childMaps != null)
			{
				foreach (ObjectIDmap map in _childMaps)
				{
					map.Dispose();
				}
				_childMaps = null;
			}
			ClearTypedData();
			Clear();
		}
		public void SetXmlData(XmlNode data)
		{
			_xml = data;
		}
		public void SetReader(XmlObjectReader reader)
		{
			_reader = reader;
		}
		public ObjectIDmap GetRootMap()
		{
			ObjectIDmap map = this;
			while (map.ParanetMap != null)
			{
				map = map.ParanetMap;
			}
			return map;
		}
		public ObjectIDmap GetMap(UInt64 componentId)
		{
			ObjectIDmap map = GetRootMap();
			if (map.WholeMemberId == componentId)
			{
				return map;
			}
			return map.GetChildMapByWholeId(componentId);
		}
		public ObjectIDmap GetMap(object obj)
		{
			ObjectIDmap map = GetRootMap();
			if (map.ContainsKey(obj))
			{
				return map;
			}
			return map.GetChildMapByObject(obj);
		}
		public ObjectIDmap GetMapByClassId(UInt32 classId)
		{
			ObjectIDmap map = GetRootMap();
			if (map.ClassId == classId)
			{
				return map;
			}
			return map.GetChildMapByClassId(classId);
		}
		public UInt32 ReplaceObject(UInt32 id, object obj)
		{
			foreach (KeyValuePair<object, UInt32> kv in this)
			{
				if (kv.Value == id)
				{
					this.Remove(kv.Key);
					this.Add(obj, id);
					return id;
				}
			}
			return AddNewObject(obj);
		}
		public uint AddNewObject(object obj)
		{
			uint id;
			if (TryGetValue(obj, out id))
			{
				return id;
			}
			//create a new id
			id = 1;
			while (true)
			{
				bool found = false;
				foreach (KeyValuePair<object, uint> kv in this)
				{
					if (kv.Value == id)
					{
						id++;
						found = true;
					}
				}
				if (!found)
					break;
			}
			Add(obj, id);
			return id;
		}
		public object GetRootObject()
		{
			return GetObjectByID((uint)(this.MemberId));
		}
		public object GetObjectByName(string name)
		{
			foreach (object v in this.Keys)
			{
				IComponent ic = v as IComponent;
				if (ic != null && ic.Site != null)
				{
					if (string.CompareOrdinal(name, ic.Site.Name) == 0)
					{
						return v;
					}
				}
			}
			return null;
		}
		public object GetObjectByID(uint id)
		{
			foreach (KeyValuePair<object, uint> kv in this)
			{
				if (kv.Value == id)
				{
					return kv.Key;
				}
			}
			return null;
		}
		public uint GetObjectID(object obj)
		{
			if (obj == null)
				return 0;
			IClassRef cr = obj as IClassRef;
			if (cr != null)
			{
				return (uint)cr.MemberId;
			}
			uint id;
			if (TryGetValue(obj, out id))
				return id;
			return 0;
		}
		public uint GetObjectIDbyName(string name)
		{
			object obj = GetObjectByName(name);
			if (obj != null)
			{
				return GetObjectID(obj);
			}
			return 0;
		}
		public IClassRef GetClassRefById(UInt32 id)
		{
			object v = GetObjectByID(id);
			if (v != null)
			{
				return GetClassRefByObject(v);
			}
			return null;
		}
		public IClassRef GetClassRefByObject(object obj)
		{
			if (obj == null)
			{
				return null;
			}
			IClassRef cr = obj as IClassRef;
			if (cr != null)
			{
				return cr;
			}
			if (_classRefMap != null)
			{
				if (_classRefMap.TryGetValue(obj, out cr))
				{
					return cr;
				}
			}
			return null;
		}
		public ReadOnlyCollection<object> GetAllObjects()
		{
			List<object> list = new List<object>();
			foreach (object o in this.Keys)
			{
				object v = o;
				if (_classRefMap != null)
				{
					IClassRef cr;
					if (_classRefMap.TryGetValue(o, out cr))
					{
						v = cr;
					}
				}
				list.Add(v);
			}
			return new ReadOnlyCollection<object>(list);
		}
		public void RefreshObjectTree()
		{
			_treeRoot = GetAllObjectsInTree();
		}
		public TreeRoot GetAllObjectsInTree()
		{
			object r = GetRootObject();
			List<object> list = new List<object>();
			foreach (object o in this.Keys)
			{
				object v = o;
				if (_classRefMap != null)
				{
					IClassRef cr;
					if (_classRefMap.TryGetValue(o, out cr))
					{
						v = cr;
					}
				}
				list.Add(v);
			}
			TreeRoot top = Tree.PopulateChildren(this, r, list);
			return top;
		}
		public bool IsSameInstance(object v1, object v2)
		{
			if (_classRefMap != null)
			{
				IClassRef cr1, cr2;
				if (_classRefMap.TryGetValue(v1, out cr1))
				{
					if (_classRefMap.TryGetValue(v2, out cr2))
					{
						return cr1.MemberId == cr2.MemberId;
					}
					return cr1.ObjectInstance == v2;
				}
				else
				{
					if (_classRefMap.TryGetValue(v2, out cr2))
					{
						return v1 == cr2.ObjectInstance;
					}
				}
			}
			return v1 == v2;
		}
		public void SetClassRefMap(object obj, IClassRef classRef)
		{
			if (obj != null)
			{
				if (_classRefMap == null)
				{
					_classRefMap = new Dictionary<object, IClassRef>();
				}
				if (_classRefMap.ContainsKey(obj))
				{
					_classRefMap[obj] = classRef;
				}
				else
				{
					_classRefMap.Add(obj, classRef);
				}
			}
			else
			{
			}
		}
		public IClassRef RemoveClassRef(object obj)
		{
			if (_classRefMap != null)
			{
				IClassRef cr;
				if (_classRefMap.TryGetValue(obj, out cr))
				{
					_classRefMap.Remove(obj);
					return cr;
				}
				cr = obj as IClassRef;
				if (cr != null)
				{
					if (_classRefMap.ContainsKey(cr.ObjectInstance))
					{
						_classRefMap.Remove(cr.ObjectInstance);
						return cr;
					}
				}
			}
			return null;
		}
		public void HookCustomPropertyValueChange(EventHandler h)
		{
			if (_classRefMap != null)
			{
				foreach (IClassRef c in _classRefMap.Values)
				{
					c.HookCustomPropertyValueChange(h);
				}
			}
		}
		public bool NameUsed(string name)
		{
			if (_classRefMap != null)
			{
				foreach (IClassRef c in _classRefMap.Values)
				{
					if (c.NameUsed(name))
					{
						return true;
					}
				}
			}
			return false;
		}
		public void AddChildMap(ObjectIDmap map)
		{
			ObjectIDmap mc = GetChildMapByWholeId(map.WholeMemberId);
			if (mc != null)
			{
				throw new SerializerException("object map for class [{0},{1}] already exists", map.ClassId, map.MemberId);
			}
			if (_childMaps == null)
			{
				_childMaps = new List<ObjectIDmap>();
			}
			_childMaps.Add(map);
		}
		public ObjectIDmap GetChildMapByWholeId(UInt64 componentId)
		{
			if (_childMaps != null)
			{
				foreach (ObjectIDmap map in _childMaps)
				{
					if (map.WholeMemberId == componentId)
					{
						return map;
					}
					ObjectIDmap mc = map.GetChildMapByWholeId(componentId);
					if (mc != null)
					{
						return mc;
					}
				}
			}
			return null;
		}
		public ObjectIDmap GetChildMapByClassId(UInt32 componentId)
		{
			if (_childMaps != null)
			{
				foreach (ObjectIDmap map in _childMaps)
				{
					if (map.ClassId == componentId)
					{
						return map;
					}
					ObjectIDmap mc = map.GetChildMapByClassId(componentId);
					if (mc != null)
					{
						return mc;
					}
				}
			}
			return null;
		}
		public ObjectIDmap GetChildMapByObject(object obj)
		{
			if (_childMaps != null)
			{
				foreach (ObjectIDmap map in _childMaps)
				{
					if (map.ContainsKey(obj))
					{
						return map;
					}
					ObjectIDmap mc = map.GetChildMapByObject(obj);
					if (mc != null)
					{
						return mc;
					}
				}
			}
			return null;
		}
		public void AddTypeChange(UInt32 id, Type type)
		{
			if (_typeChanges == null)
			{
				_typeChanges = new Dictionary<uint, Type>();
			}
			if (_typeChanges.ContainsKey(id))
			{
				_typeChanges[id] = type;
			}
			else
			{
				_typeChanges.Add(id, type);
			}
		}
		public Type GetChangedType(UInt32 id)
		{
			if (_typeChanges != null)
			{
				Type t;
				if (_typeChanges.TryGetValue(id, out t))
				{
					return t;
				}
			}
			return null;
		}
		public void RemoveObjectFromTree(object obj)
		{
			if (_treeRoot != null)
			{
				_treeRoot.RemoveTree(obj);
			}
		}
		#endregion
		#region Properties
		public TreeRoot TreeRoot
		{
			get
			{
				if (_treeRoot == null)
				{
					_treeRoot = GetAllObjectsInTree();
				}
				return _treeRoot;
			}
		}
		public bool IsDisposed
		{
			get
			{
				return _isDisposed;
			}
		}
		/// <summary>
		/// list of ClassInstancePointer
		/// </summary>
		public IList<IClassRef> ClassRefList
		{
			get
			{
				if (_classRefMap != null)
				{
					List<IClassRef> list = new List<IClassRef>();
					foreach (IClassRef cr in _classRefMap.Values)
					{
						list.Add(cr);
					}
					return list;
				}
				return null;
			}
		}

		public LimnorProject Project
		{
			get
			{
				return _project;
			}
		}
		/// <summary>
		/// when this class is used for declaring an instance, this is the id for the instance.
		/// the class id is for the class defining the class to be instantiated, the member id
		/// is defined by the class using the instance.
		/// </summary>
		public UInt64 InstanceId
		{
			get
			{
				return _instanceId;
			}
			set
			{
				_instanceId = value;
			}
		}
		public List<ObjectIDmap> ChildMaps
		{
			get
			{
				return _childMaps;
			}
		}
		public XmlObjectReader Reader
		{
			get
			{
				return _reader;
			}
		}
		public XmlNode XmlData
		{
			get
			{
				return _xml;
			}
		}
		public string Name
		{
			get
			{
				return XmlUtility.XmlUtil.GetNameAttribute(_xml);
			}
		}
		public string DocumentMoniker
		{
			get
			{
				if (string.IsNullOrEmpty(_projectFile))
				{
					if (ParanetMap != null)
					{
						return ParanetMap.DocumentMoniker;
					}
				}
				return _projectFile;
			}
			set
			{
				_projectFile = value;
			}
		}
		public string ClassFile
		{
			get
			{
				string s = DocumentMoniker;
				if (System.IO.File.Exists(s))
				{
					return s;
				}
				if (_xml != null && Project != null)
				{
					s = XmlUtility.XmlUtil.GetAttribute(_xml, XmlTags.XMLATT_filename);
					return System.IO.Path.Combine(Project.ProjectFolder, System.IO.Path.GetFileName(s));
				}
				return null;
			}
		}
		private ObjectIDmap _pmp;
		public ObjectIDmap ParanetMap { get { return _pmp; } private set { _pmp = value; } }
		/// <summary>
		/// it is the id for the class definition, not for the instances
		/// </summary>
		public UInt64 WholeMemberId
		{
			get
			{
				return _wholeId;
			}
		}
		public string ObjectKey
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, "om_{0}", _wholeId.ToString("x", CultureInfo.InvariantCulture));
			}
		}
		public UInt32 ClassId
		{
			get
			{
				UInt32 c, a;
				SerializeUtil.ParseDDWord(_wholeId, out a, out c);
				return c;
			}
			set
			{
				UInt32 c, a;
				SerializeUtil.ParseDDWord(_wholeId, out a, out c);
				_wholeId = SerializeUtil.MakeDDWord(a, value);
			}
		}
		public UInt32 MemberId
		{
			get
			{
				UInt32 c, a;
				SerializeUtil.ParseDDWord(_wholeId, out a, out c);
				return a;
			}
			set
			{
				UInt32 c, a;
				SerializeUtil.ParseDDWord(_wholeId, out a, out c);
				_wholeId = SerializeUtil.MakeDDWord(value, c);
			}
		}

		public int TraceLevel
		{
			get
			{
				if (_project != null)
				{
					return _project.TraceLevel;
				}
				return 0;
			}
		}
		public IClassPointer RootPointer
		{
			get
			{
				return DesignService.CreateClassPointer(this);
			}
		}
		public Type RootObjectType
		{
			get
			{
				return XmlUtil.GetLibTypeAttribute(XmlData, out _classIds);
			}
		}
		public string ClassIDs
		{
			get
			{
				return _classIds;
			}
		}
		public Guid ClassGuid
		{
			get
			{
				Guid g = XmlUtil.GetAttributeGuid(XmlData, XmlTags.XMLATT_guid);
				if (g == Guid.Empty)
				{
					g = Guid.NewGuid();
					XmlUtil.SetAttribute(XmlData, XmlTags.XMLATT_guid, g);
				}
				return g;
			}
		}
		#endregion
		#region Typed data
		/*-----------------------------------------------
         * the data here is pseudo-static. for all instances 
         * of ObjectIDmap the data is the same is the ClassId
         * is same
         * ----------------------------------------------
         */
		public T GetTypedData<T>()
		{
			return _project.GetTypedData<T>(ClassId);
		}
		public void SetTypedData<T>(T v)
		{
			_project.SetTypedData<T>(ClassId, v);
		}
		public void RemoveTypedData<T>()
		{
			_project.RemoveTypedData<T>(ClassId);
		}
		public void ClearTypedData()
		{
			_project.ClearTypedData(ClassId);
		}
		public void ClearItems()
		{
			Clear();
			_childMaps = null;
			_classRefMap = null;
		}
		/// <summary>
		/// get the dictionary. if it does not exist then return null.
		/// </summary>
		public Dictionary<Type, object> TypedDataCollection
		{
			get
			{
				return _project.GetTypedDataCollection(ClassId);
			}
		}
		#endregion
		#region Resent selection cache
		private static Dictionary<Guid, Dictionary<UInt32, List<IObjectIdentity>>> _recentSelectionList;
		public void AddRecentSelection(IObjectIdentity v)
		{
			if (_recentSelectionList == null)
			{
				_recentSelectionList = new Dictionary<Guid, Dictionary<uint, List<IObjectIdentity>>>();
			}
			Dictionary<uint, List<IObjectIdentity>> dataList;
			if (!_recentSelectionList.TryGetValue(_project.ProjectGuid, out dataList))
			{
				dataList = new Dictionary<uint, List<IObjectIdentity>>();
				_recentSelectionList.Add(_project.ProjectGuid, dataList);
			}
			List<IObjectIdentity> ts;
			if (!dataList.TryGetValue(ClassId, out ts))
			{
				ts = new List<IObjectIdentity>();
				dataList.Add(ClassId, ts);
			}
			foreach (IObjectIdentity id in ts)
			{
				if (id.IsSameObjectRef(v))
				{
					return;
				}
			}
			ts.Add(v);
		}
		public List<IObjectIdentity> GetRecentSelectionList()
		{
			if (_recentSelectionList != null)
			{
				Dictionary<UInt32, List<IObjectIdentity>> dataList;
				if (_recentSelectionList.TryGetValue(_project.ProjectGuid, out dataList))
				{
					List<IObjectIdentity> ts;
					if (dataList.TryGetValue(ClassId, out ts))
					{
						return ts;
					}
				}
			}
			return null;
		}
		#endregion
		#region static services
		private static IDesignService _ds;
		public static IDesignService DesignService
		{
			get { return _ds; }
			set { _ds = value; }
		}
		#endregion
	}

}
