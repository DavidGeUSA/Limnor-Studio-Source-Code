/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Object Builder Utility
 * License: GNU General Public License v3.0
 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;
using System.Xml;
using System.Reflection;
using System.Globalization;
using System.Drawing.Design;
using System.Windows.Forms;
using System.CodeDom;

namespace VPL
{
	public interface IConstructorUser
	{
		string GetConstructorDisplay();
		ConstructorInfo[] GetConstructors();
		void SetConstructor(ConstructorInfo c);
		void OnCustomValueChanged(string name, object value);
		object GetConstructorValue(string name);
		void ResetConstructorValue(string name);
		void SetConstructorValue(string name, object value);
		object GetValue();
	}
	public interface IConstructorOwner : IConstructorUser
	{
		ConstValueSelector GetCurrentConstructor();
	}
	public interface IXType : IConstructorOwner
	{
		Type ValueType { get; }
		void SetTypeParameters(Type[] ts);
		Type[] GetTypeParameters();
		object[] GetConstructorValues();
		CodeExpression[] GetConstructorValueExpressions();
		Dictionary<string, object> GetConstructorParameterValues();
		void OnConstructorValueChanged(string name, object value);
		object GetPropertyValue(string name);
		bool IsPropertyValueSet(string name);
		void SetPropertyValue(string name, object value);
		void ResetPropertyValue(string name);
		Dictionary<string, object> GetPropertyValues();
		void SetAsRoot();
		bool TestWriteProperty(string propertyName);
	}
	/// <summary>
	/// wrapper for non-component
	/// </summary>
	[Designer(typeof(ComponentDesigner))]
	public class XClass<T> : IComponent, ICustomTypeDescriptor, ICustomEventMethodType, IBeforeXmlNodeSerialize, IXType
	{
		#region fields and constructors
		public const string XML_REALTYPE = "ValueType";
		public const string XML_OBJVALUE = "ObjectValue";
		const string CONSTRUCTOR = "Constructor";
		private Type[] _parameterTypes; //for generic type
		private ConstValueSelector _constructor;
		private Dictionary<string, object> _propertyValues; //_obj is null
		private object _obj;
		private bool _triedCreateInstance = false;
		private bool _forStatic;
		private EventHandler _customValueChange;
		private bool _isRoot; //for the root class
		public bool NameReadOnly;
		public XClass()
		{
		}
		#endregion
		#region constructor values
		[Browsable(false)]
		[NotForProgramming]
		public object GetConstructorValue(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_constructor != null)
				{
					return _constructor.GetConstructorValue(name);
				}
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public void SetConstructorValue(string name, object v)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_constructor == null)
				{
					_constructor = new ConstValueSelector(typeof(T), this, CONSTRUCTOR);
				}
				_constructor.SetConstructorValue(name, v);
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void ResetConstructorValue(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_constructor != null)
				{
					_constructor.ResetConstructorValue(name);
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public string GetConstructorDisplay()
		{
			if (_constructor == null)
			{
				return string.Empty;
			}
			else
			{
				return _constructor.GetConstructorDisplay();
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void SetConstructor(ConstructorInfo cif)
		{
			if (_constructor == null)
			{
				_constructor = new ConstValueSelector(typeof(T), this, CONSTRUCTOR);
			}
			_constructor.SetConstructor(cif);
		}
		#endregion
		#region static functions
		public static Type GetObjectType(object instance)
		{
			if (instance is XClass<T>)
			{
				return ((XClass<T>)instance).ValueType;
			}
			else if (instance is XControl<T>)
			{
				return ((XControl<T>)instance).ValueType;
			}
			else
			{
				return instance.GetType();
			}
		}
		public static IComponent CreateComponent(IDesignerLoaderHost host, Type type, string name)
		{
			if (typeof(IComponent).IsAssignableFrom(type))
			{
				if (host != null)
				{
					INameCreationService cs = host.GetService(typeof(INameCreationService)) as INameCreationService;
					if (cs != null)
					{
						IVplNameService ns = cs as IVplNameService;
						if (ns != null)
						{
							ns.ComponentType = type;
						}
					}
					return host.CreateComponent(type, name);
				}
				else
				{
					IComponent ic = (IComponent)CreateObject(type);
					ic.Site = new XTypeSite(ic);
					ic.Site.Name = name;
					return ic;
				}
			}
			if (host != null)
			{
				bool isControl = typeof(Control).IsAssignableFrom(type);
				INameCreationService cs = host.GetService(typeof(INameCreationService)) as INameCreationService;
				if (cs != null)
				{
					IVplNameService ns = cs as IVplNameService;
					if (ns != null)
					{
						if (isControl)
						{
							ns.ComponentType = typeof(XControl<T>);
						}
						else
						{
							ns.ComponentType = typeof(XClass<T>);
						}
					}
				}
				if (isControl)
				{
					XControl<T> obj = (XControl<T>)host.CreateComponent(typeof(XControl<T>), name);
					obj.AssignValue(CreateObject(type));
					return obj;
				}
				else
				{
					XClass<T> obj = (XClass<T>)host.CreateComponent(typeof(XClass<T>), name);
					obj.AssignValue(CreateObject(type));
					return obj;
				}
			}
			else
			{
				XClass<T> obj = new XClass<T>();
				obj.Site = new XTypeSite(obj);
				obj.Site.Name = name;
				obj.AssignValue(CreateObject(type));
				return obj;
			}
		}
		public static object CreateComponent(Type type, string name)
		{
			if (typeof(Control).IsAssignableFrom(type))
			{
				XControl<T> obj = new XControl<T>();
				obj.Site = new XTypeSite(obj);
				obj.Site.Name = name;
				obj.AssignValue(Activator.CreateInstance(type));
				return obj;
			}
			else
			{
				XClass<T> obj = new XClass<T>();
				obj.Site = new XTypeSite(obj);
				obj.Site.Name = name;
				obj.AssignValue(Activator.CreateInstance(type));
				return obj;
			}
		}
		public static object CreateObject(Type type)
		{
			if (type.IsValueType)
			{
				if (type.Equals(typeof(void)))
				{
					return Activator.CreateInstance(typeof(object));
				}
				else
				{
					return Activator.CreateInstance(type);
				}
			}
			object v0 = null;
			bool bFound;
			LinkedListNode<System.Reflection.ConstructorInfo> node;
			LinkedList<System.Reflection.ConstructorInfo> constructors = new LinkedList<System.Reflection.ConstructorInfo>();
			System.Reflection.ConstructorInfo[] cis = type.GetConstructors();
			for (int i = 0; i < cis.Length; i++)
			{
				int n = 0;
				System.Reflection.ParameterInfo[] pis = cis[i].GetParameters();
				if (pis != null)
				{
					n = pis.Length;
				}
				if (constructors.Count == 0)
				{
					constructors.AddFirst(cis[i]);
				}
				else
				{
					bFound = false;
					node = constructors.First;
					while (node != null)
					{
						System.Reflection.ParameterInfo[] pis0 = node.Value.GetParameters();
						if (pis0.Length >= n)
						{
							bFound = true;
							constructors.AddBefore(node, cis[i]);
							break;
						}
						node = node.Next;
					}
					if (!bFound)
					{
						constructors.AddLast(cis[i]);
					}
				}
			}
			bFound = false;
			node = constructors.First;
			while (node != null)
			{
				System.Reflection.ParameterInfo[] pis0 = node.Value.GetParameters();
				try
				{
					if (pis0.Length == 0)
						v0 = node.Value.Invoke(null);
					else
						v0 = node.Value.Invoke(new object[pis0.Length]);
					bFound = true;
					break;
				}
				catch
				{
				}
				node = node.Next;
			}
			if (!bFound)
			{
				throw new VPLException(string.Format("The .Net Type {0} does not have a constructor we can use", type));
			}
			return v0;
		}
		#endregion
		#region properties
		[ReadOnly(true)]
		[Browsable(false)]
		[Description("Type of the object value")]
		public Type ValueType
		{
			get
			{
				return typeof(T);
			}
		}
		[Description("Name of the component")]
		[ParenthesizePropertyName(true)]
		public string Name
		{
			get
			{
				if (Site != null)
				{
					if (!string.IsNullOrEmpty(Site.Name))
					{
						return Site.Name;
					}
				}
				if (!string.IsNullOrEmpty(_name))
					return _name;
				return "";
			}
			set
			{
				if (Site != null)
					Site.Name = value;
				_name = value;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		[Description("Value of the component")]
		public object ObjectValue
		{
			get
			{
				if (_obj == null)
				{
					if (!_triedCreateInstance)
					{
						_triedCreateInstance = true;
						if (ValueType.IsAbstract)
						{
							return null;
						}
						else
						{
							_obj = GetValue(); //use construuctor parameter values to create the object
						}
					}
				}
				return _obj;
			}
			set
			{
				if (ValueType.IsAbstract)
				{
				}
				else
				{
					_obj = value;
				}
			}
		}
		#endregion
		#region methods
		public void SetAsRoot()
		{
			_isRoot = true;
		}
		public override string ToString()
		{
			return Name;
		}

		public void AssignValue(object value)
		{
			_obj = value;
		}
		public void AssignValueAndType(object value)
		{
			_obj = value;
		}
		public void OnCustomValueChanged(string name, object value)
		{
			if (_obj == null)
			{
				SetPropertyValue(name, value);
			}
			if (_customValueChange != null)
			{
				_customValueChange(this, new PropertyChangedEventArgs(name));
			}
		}
		public void OnConstructorValueChanged(string name, object value)
		{
			if (_constructor == null)
			{
				_constructor = new ConstValueSelector(typeof(T), this, CONSTRUCTOR);
			}
			_constructor.SetConstructorValue(name, value);
		}
		#endregion
		#region IComponent Members

		public event EventHandler Disposed;
		private ISite _site;
		private string _name;
		[ReadOnly(true)]
		[Browsable(false)]
		public ISite Site
		{
			get
			{
				return _site;
			}
			set
			{
				_site = value;
				if (_site != null)
				{
					if (string.IsNullOrEmpty(_site.Name))
					{
						if (!string.IsNullOrEmpty(_name))
						{
							_site.Name = _name;
						}
					}
					else
					{
						_name = _site.Name;
					}
				}
			}
		}

		#endregion
		#region IDisposable Members

		public void Dispose()
		{
			if (_obj != null)
			{
				if (_obj is IDisposable)
				{
					((IDisposable)_obj).Dispose();
				}
				_obj = null;
			}
			if (Disposed != null)
			{
				Disposed(this, new EventArgs());
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
			return GetEvents(null);
		}

		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			//determine scope and include special name
			bool includeSpecialName = false;
			EnumReflectionMemberInfoSelectScope scope = EnumReflectionMemberInfoSelectScope.InstanceOnly;
			if (attributes != null)
			{
				for (int i = 0; i < attributes.Length; i++)
				{
					ReflectionMemberInfoSelectIncludeSpecialAttribute incS = attributes[i] as ReflectionMemberInfoSelectIncludeSpecialAttribute;
					if (incS != null)
					{
						includeSpecialName = true;
					}
					else
					{
						ReflectionMemberInfoSelectScopeAttribute rs = attributes[i] as ReflectionMemberInfoSelectScopeAttribute;
						if (rs != null)
						{
							scope = rs.Scope;
						}
					}
				}
			}
			if (_forStatic)
			{
				scope = EnumReflectionMemberInfoSelectScope.StaticOnly;
			}
			//====================================================================
			//properties to be collected
			PropertyDescriptorCollection newProps = new PropertyDescriptorCollection(null);
			bool gotName = false;
			//find Name property: non-IComponent does not have a name by its own ======================================
			if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this, attributes, true);
				// For each property use a property descriptor of our own
				foreach (PropertyDescriptor oProp in baseProps)
				{
					if (string.Compare(oProp.Name, "Name", StringComparison.Ordinal) == 0)
					{
						if (oProp.SerializationVisibility == DesignerSerializationVisibility.Visible)
						{
							if (NameReadOnly)
							{
								ReadOnlyPropertyDesc rp = new ReadOnlyPropertyDesc(oProp);
								newProps.Add(rp);
							}
							else
							{
								newProps.Add(oProp);
							}
							gotName = true;
							break;
						}
					}
				}
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.StaticOnly)
			{
				string name = typeof(T).Name;
				MethodInfo mif = typeof(T).GetMethod("GetStaticName", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				if (mif != null)
				{
					object v = mif.Invoke(null, null);
					if (v != null)
					{
						name = v.ToString();
					}
				}
				PropertyInfoDescriptor<T> nameProp = new PropertyInfoDescriptor<T>("Name", new Attribute[] { ReadOnlyAttribute.Yes, new ParenthesizePropertyNameAttribute(true) }, typeof(string), name);
				newProps.Add(nameProp);
				gotName = true;
			}
			//===end of getting Name property===================================================================================
			//===get object properties=============================
			//===get instance properties
			if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				if (ValueType.IsAbstract)
				{
					createPropertyInfoDescriptor(includeSpecialName, attributes, newProps, false);
				}
				else
				{
					PropertyDescriptorCollection objProps;
					IClassInstance ci = ObjectValue as IClassInstance;
					if (ci != null)
					{
						objProps = ci.GetInstanceProperties(attributes);
					}
					else
					{
						if (ObjectValue == null) //no default constructor
						{
							PropertyInfo[] pifs = typeof(T).GetProperties();
							if (pifs == null)
							{
								objProps = new PropertyDescriptorCollection(new PropertyDescriptor[] { });
							}
							else
							{
								PropertyDescriptor[] pds = new PropertyDescriptor[pifs.Length];
								for (int i = 0; i < pifs.Length; i++)
								{
									pds[i] = new PropertyDescriptorVirtualValue(pifs[i].Name, new Attribute[] { }, pifs[i], this);
								}
								objProps = new PropertyDescriptorCollection(pds);
							}
						}
						else
						{
							objProps = TypeDescriptor.GetProperties(this.ObjectValue, new Attribute[] { DesignOnlyAttribute.No }, true);
						}
					}
					foreach (PropertyDescriptor oProp in objProps)
					{
						if (string.Compare(oProp.Name, "Name", StringComparison.Ordinal) == 0)
						{
							if (gotName)
							{
								continue;
							}
						}
						XValueDesctiptor np = new XValueDesctiptor(oProp, this);
						newProps.Add(np);
					}
				}
			}
			//===get static properties
			if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.StaticOnly)
			{
				createPropertyInfoDescriptor(includeSpecialName, attributes, newProps, true);
			}
			//===get constructors ========================
			if (!_isRoot && scope != EnumReflectionMemberInfoSelectScope.StaticOnly)
			{
				bool needConstructor = VPLUtil.NeedConstructor(typeof(T));
				if (_constructor == null)
				{
					if (needConstructor)
					{
						_constructor = new ConstValueSelector(typeof(T), this, CONSTRUCTOR);
					}
				}
				if (needConstructor)
				{
					OwnerConstructorSelectDesctiptor csd = new OwnerConstructorSelectDesctiptor(this);
					newProps.Add(csd);
				}
			}
			//===get type parameters================
			if (typeof(T).IsGenericType)
			{
				Type[] tParams = null;
				Type tg = typeof(T).GetGenericTypeDefinition();
				if (tg != null)
				{
					tParams = tg.GetGenericArguments();
					if (_parameterTypes == null || _parameterTypes.Length == 0)
					{
						_parameterTypes = typeof(T).GetGenericArguments();
					}
				}
				else
				{
					tParams = typeof(T).GetGenericArguments();
				}
				if (tParams != null)
				{
					for (int i = 0; i < tParams.Length; i++)
					{
						string sn = string.Empty;
						if (_parameterTypes != null && _parameterTypes.Length > i)
						{
							if (_parameterTypes[i] != null)
							{
								sn = _parameterTypes[i].Name;
							}
						}
						PropertyDescriptorForDisplay pdf = new PropertyDescriptorForDisplay(this.GetType(), tParams[i].Name, sn, new Attribute[] { });
						newProps.Add(pdf);
					}
				}
			}
			//===
			if (!typeof(T).Assembly.GlobalAssemblyCache)
			{
				PropertyDescriptorInformation pdi = new PropertyDescriptorInformation("LibLocation", typeof(T).Assembly.Location, this.GetType());
				newProps.Add(pdi);
			}
			return newProps;
		}
		private void createPropertyInfoDescriptor(bool includeSpecialName, Attribute[] attributes, PropertyDescriptorCollection newProps, bool forStatic)
		{
			PropertyInfo[] pifs;
			if (forStatic)
			{
				pifs = this.ValueType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			}
			else
			{
				pifs = ValueType.GetProperties();
			}
			if (pifs != null && pifs.Length > 0)
			{
				if (!includeSpecialName)
				{
					List<PropertyInfo> l = new List<PropertyInfo>();
					for (int i = 0; i < pifs.Length; i++)
					{
						if (!pifs[i].IsSpecialName)
						{
							l.Add(pifs[i]);
						}
					}
					if (l.Count < pifs.Length)
					{
						pifs = new PropertyInfo[l.Count];
						l.CopyTo(pifs);
					}
				}
				for (int i = 0; i < pifs.Length; i++)
				{
					object[] ca = pifs[i].GetCustomAttributes(true);
					Attribute[] ats;
					if (ca == null || ca.Length == 0)
						ats = attributes;
					else
					{
						int k = 0;
						if (attributes != null && attributes.Length > 0)
						{
							ats = new Attribute[attributes.Length + ca.Length];
							while (k < attributes.Length)
							{
								ats[k] = attributes[k];
								k++;
							}
						}
						else
						{
							ats = new Attribute[ca.Length];
						}
						for (int j = 0; j < ca.Length; j++)
						{
							ats[k + j] = (Attribute)ca[j];
						}
					}
					PropertyInfoDescriptor<T> prop = new PropertyInfoDescriptor<T>(pifs[i].Name, ats, pifs[i], forStatic);
					newProps.Add(prop);
				}
			}
		}
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion
		#region XValueDesctiptor
		public class XValueDesctiptor : PropertyDescriptor
		{
			XClass<T> _owner;
			PropertyDescriptor _originalDescriptor;
			public XValueDesctiptor(PropertyDescriptor d, XClass<T> owner)
				: base(d)
			{
				_owner = owner;
				_originalDescriptor = d;
			}

			public override bool CanResetValue(object component)
			{
				return _originalDescriptor.CanResetValue(((XClass<T>)component).ObjectValue);
			}

			public override Type ComponentType
			{
				get { return typeof(XClass<T>); }
			}

			public override object GetValue(object component)
			{
				try
				{
					return _originalDescriptor.GetValue(((XClass<T>)component).ObjectValue);
				}
				catch
				{
				}
				return VPLUtil.GetDefaultValue(PropertyType);
			}

			public override bool IsReadOnly
			{
				get
				{
					return _originalDescriptor.IsReadOnly;
				}
			}

			public override Type PropertyType
			{
				get
				{
					return _originalDescriptor.PropertyType;
				}
			}

			public override void ResetValue(object component)
			{
				try
				{
					_originalDescriptor.ResetValue(((XClass<T>)component).ObjectValue);
				}
				catch
				{
				}
			}

			public override void SetValue(object component, object value)
			{
				try
				{
					_originalDescriptor.SetValue(((XClass<T>)component).ObjectValue, value);
					_owner.OnCustomValueChanged(Name, value);
				}
				catch
				{
				}
			}

			public override bool ShouldSerializeValue(object component)
			{
				return _originalDescriptor.ShouldSerializeValue(((XClass<T>)component).ObjectValue);
			}
		}
		#endregion
		#region ICustomEventMethodType Members
		public void MakeStatic()
		{
			_forStatic = true;
		}
		public EventInfo[] GetEvents(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly)
		{
			EventInfo[] eArray;
			if (scope == EnumReflectionMemberInfoSelectScope.Both)
			{
				eArray = typeof(T).GetEvents();
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				eArray = typeof(T).GetEvents(BindingFlags.Public | BindingFlags.Instance);
			}
			else
			{
				eArray = typeof(T).GetEvents(BindingFlags.Public | BindingFlags.Static);
			}
			return VPLUtil.GetEvents(eArray, includeSpecialName, browsableOnly);
		}

		public MethodInfo[] GetMethods(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly, bool webOnly)
		{
			MethodInfo[] eArray;
			if (scope == EnumReflectionMemberInfoSelectScope.Both)
			{
				eArray = typeof(T).GetMethods();
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				eArray = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
			}
			else
			{
				eArray = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static);
			}
			return VPLUtil.GetMethods(eArray, includeSpecialName, browsableOnly, webOnly);
		}
		public FieldInfo[] GetFields(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly)
		{
			FieldInfo[] eArray;
			if (scope == EnumReflectionMemberInfoSelectScope.Both)
			{
				eArray = typeof(T).GetFields();
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				eArray = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
			}
			else
			{
				eArray = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);
			}
			return VPLUtil.GetFields(eArray, includeSpecialName, browsableOnly);
		}
		public PropertyDescriptorCollection GetProperties(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly, bool includeAbstract)
		{
			List<Attribute> attrList = new List<Attribute>();
			attrList.Add(DesignOnlyAttribute.No);
			attrList.Add(new ReflectionMemberInfoSelectScopeAttribute(scope));
			if (browsableOnly)
			{
				attrList.Add(BrowsableAttribute.Yes);
			}
			if (includeSpecialName)
			{
				attrList.Add(ReflectionMemberInfoSelectIncludeSpecialAttribute.Yes);
			}
			PropertyDescriptorCollection props = GetProperties(attrList.ToArray());
			return props;
		}
		public PropertyDescriptor GetProperty(string propertyName)
		{
			List<Attribute> attrList = new List<Attribute>();
			attrList.Add(DesignOnlyAttribute.No);
			PropertyDescriptorCollection props = GetProperties(attrList.ToArray());
			foreach (PropertyDescriptor p in props)
			{
				if (p.Name == propertyName)
				{
					return p;
				}
			}
			return null;
		}
		public void HookCustomPropertyValueChange(EventHandler h)
		{
			_customValueChange = h;
		}
		#endregion
		#region IBeforeXmlNodeSerialize Members
		const string XML_GenericParameters = "TypeArgs";
		public void OnReadFromXmlNode(IXmlCodeReader serializer, XmlNode node)
		{
			XmlNodeList nds = node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}", XML_GenericParameters, ConstValueSelector.XML_Item));
			if (nds != null)
			{
				_parameterTypes = new Type[nds.Count];
				for (int i = 0; i < nds.Count; i++)
				{
					_parameterTypes[i] = serializer.ReadValue<Type>(nds[i], this);
				}
			}
			_propertyValues = new Dictionary<string, object>();
			nds = node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}", ConstValueSelector.XML_ConstructorValues, ConstValueSelector.XML_Item));
			if (nds != null)
			{
				_constructor = new ConstValueSelector(typeof(T), this, CONSTRUCTOR);
				_constructor.ResetParameterTypes();
				for (int i = 0; i < nds.Count; i++)
				{
					string name = serializer.GetName(nds[i]);
					Type t = serializer.ReadValue<Type>(nds[i], this);
					_constructor.AddParameter(name, t);
					XmlNode nv = nds[i].SelectSingleNode(ConstValueSelector.XML_Value);
					if (nv != null)
					{
						object v;
						XmlNode objNode = nv.SelectSingleNode(ConstValueSelector.XML_ObjProperty);
						if (objNode != null)
						{
							ConstValueSelector cv = new ConstValueSelector(t, _constructor, name);
							serializer.ReadObjectFromXmlNode(objNode, cv, cv.GetType(), this);
							v = cv;
						}
						else
						{
							v = serializer.ReadValue(nv, this, t);
						}
						_constructor.SetConstructorValue(name, v);
					}
				}
			}
			nds = node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}", ConstValueSelector.XML_Props, ConstValueSelector.XML_Item));
			if (nds != null)
			{
				for (int i = 0; i < nds.Count; i++)
				{
					string name = serializer.GetName(nds[i]);
					object v = serializer.ReadObject(nds[i], this);
					_propertyValues.Add(name, v);
				}
			}
		}

		public void OnWriteToXmlNode(IXmlCodeWriter serializer, XmlNode node)
		{
			if (_parameterTypes != null && _parameterTypes.Length > 0)
			{
				XmlNode nps = serializer.CreateSingleNewElement(node, XML_GenericParameters);
				for (int i = 0; i < _parameterTypes.Length; i++)
				{
					XmlNode nd = node.OwnerDocument.CreateElement(ConstValueSelector.XML_Item);
					nps.AppendChild(nd);
					serializer.WriteValue(nd, _parameterTypes[i], this);
				}
			}
			if (_constructor != null && _constructor.ParameterCount > 0)
			{
				XmlNode nps = serializer.CreateSingleNewElement(node, ConstValueSelector.XML_ConstructorValues);
				foreach (KeyValuePair<string, Type> kv in _constructor.Parameters)
				{
					XmlNode nd = node.OwnerDocument.CreateElement(ConstValueSelector.XML_Item);
					nps.AppendChild(nd);
					serializer.SetName(nd, kv.Key);
					serializer.WriteValue(nd, kv.Value, this);
					if (_constructor.HasValue(kv.Key))
					{
						object v = _constructor.GetConstructorValue(kv.Key);
						XmlNode nv = serializer.CreateSingleNewElement(nd, ConstValueSelector.XML_Value);
						serializer.WriteValue(nv, v, this);
					}
				}
			}
			if (_propertyValues != null && _propertyValues.Count > 0)
			{
				XmlNode propsNode = serializer.CreateSingleNewElement(node, ConstValueSelector.XML_Props);
				foreach (KeyValuePair<string, object> kv in _propertyValues)
				{
					XmlNode nd = node.OwnerDocument.CreateElement(ConstValueSelector.XML_Item);
					propsNode.AppendChild(nd);
					serializer.SetName(nd, kv.Key);
					if (kv.Value != null)
					{
						serializer.WriteObjectToNode(nd, kv.Value, true);
					}
				}
			}
		}

		#endregion
		#region IXType Members
		[Browsable(false)]
		[NotForProgramming]
		public Dictionary<string, object> GetPropertyValues()
		{
			return _propertyValues;
		}
		[Browsable(false)]
		[NotForProgramming]
		public bool IsPropertyValueSet(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_propertyValues != null)
				{
					return _propertyValues.ContainsKey(name);
				}
			}
			return false;
		}
		[Browsable(false)]
		[NotForProgramming]
		public object GetPropertyValue(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_propertyValues != null)
				{
					object v;
					if (_propertyValues.TryGetValue(name, out v))
					{
						return v;
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public void SetPropertyValue(string name, object value)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_propertyValues == null)
				{
					_propertyValues = new Dictionary<string, object>();
				}
				if (_propertyValues.ContainsKey(name))
				{
					_propertyValues[name] = value;
				}
				else
				{
					_propertyValues.Add(name, value);
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void ResetPropertyValue(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_propertyValues != null && _propertyValues.ContainsKey(name))
				{
					_propertyValues.Remove(name);
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public ConstValueSelector GetCurrentConstructor()
		{
			if (_constructor == null)
			{
				_constructor = new ConstValueSelector(typeof(T), this, CONSTRUCTOR);
			}
			return _constructor;
		}
		[Browsable(false)]
		[NotForProgramming]
		public void SetTypeParameters(Type[] ts)
		{
			_parameterTypes = ts;
		}
		[Browsable(false)]
		[NotForProgramming]
		public ConstructorInfo[] GetConstructors()
		{
			return typeof(T).GetConstructors();
		}
		[Browsable(false)]
		[NotForProgramming]
		public Type[] GetTypeParameters()
		{
			return _parameterTypes;
		}
		[Browsable(false)]
		[NotForProgramming]
		public Dictionary<string, object> GetConstructorParameterValues()
		{
			if (_constructor != null)
			{
				return _constructor.GetConstructorParameterValues();
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public object[] GetConstructorValues()
		{
			if (_constructor != null)
			{
				object[] vs = new object[_constructor.ParameterCount];
				int n = 0;
				foreach (KeyValuePair<string, Type> kv in _constructor.Parameters)
				{
					object v = _constructor.GetConstructorValue(kv.Key);
					if (v == null)
					{
						v = VPLUtil.GetDefaultValue(kv.Value);
					}
					else
					{
						ConstValueSelector cv = v as ConstValueSelector;
						if (cv != null)
						{
							v = cv.GetValue();
						}
					}
					vs[n] = v;
					n++;
				}
				return vs;
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public CodeExpression[] GetConstructorValueExpressions()
		{
			if (_constructor != null)
			{
				CodeExpression[] vs = new CodeExpression[_constructor.ParameterCount];
				int n = 0;
				Dictionary<string, object> values = _constructor.GetConstructorParameterValues();
				foreach (KeyValuePair<string, object> kv in values)
				{
					CodeExpression exp = null;
					object v = _constructor.GetConstructorValue(kv.Key);
					if (v == null)
					{
						exp = new CodePrimitiveExpression(null);
					}
					else
					{
						ConstValueSelector cv = v as ConstValueSelector;
						if (cv != null)
						{
							exp = cv.CreateExpression();
						}
						else
						{
							exp = ObjectCreationCodeGen.ObjectCreationCode(v);
						}
					}
					vs[n] = exp;
					n++;
				}
				return vs;
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public object GetValue()
		{
			if (typeof(T).IsAbstract)
				return null;
			object[] vs = GetConstructorValues();
			if (vs == null)
			{
				if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
				{
					return Activator.CreateInstance(typeof(T));
				}
			}
			else
			{
				try
				{
					return Activator.CreateInstance(typeof(T), vs);
				}
				catch
				{
				}
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public bool TestWriteProperty(string propertyName)
		{
			bool bRet = true;
			if (_obj != null)
			{
				PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(_obj);
				foreach (PropertyDescriptor pd in pdc)
				{
					if (string.CompareOrdinal(pd.Name, propertyName) == 0)
					{
						if (pd.IsReadOnly)
						{
							bRet = false;
						}
						else
						{
							object v = pd.GetValue(_obj);
							try
							{
								pd.SetValue(_obj, v);
							}
							catch
							{
								bRet = false;
							}
						}
						break;
					}
				}
			}
			return bRet;
		}
		#endregion
	}
	/// <summary>
	/// for designing controls without root designer 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class XControl<T> : UserControl, ICustomTypeDescriptor, ICustomEventMethodType, IBeforeXmlNodeSerialize, IPostDeserializeProcess
	{
		#region fields and constructors
		private T _obj;
		private bool _forStatic;
		private EventHandler _customValueChange;
		public bool NameReadOnly;
		public XControl()
		{
		}
		private void dockControl()
		{
			if (!(_obj is TabPage))
			{
				Control c = _obj as Control;
				if (c != null && c.Parent == null)
				{
					c.Dock = DockStyle.Fill;
					c.Visible = true;
					this.Controls.Add(c);
				}
			}
		}
		#endregion
		#region properties
		[ReadOnly(true)]
		[Browsable(false)]
		[Description("Type of the object value")]
		public Type ValueType
		{
			get
			{
				return typeof(T);
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		[Description("Value of the component")]
		public object ObjectValue
		{
			get
			{
				if (_obj == null)
				{
					if (ValueType.IsAbstract)
					{
						return null;
					}
					else
					{
						_obj = Activator.CreateInstance<T>();
						dockControl();
					}
				}
				return _obj;
			}
			set
			{
				if (ValueType.IsAbstract)
				{
				}
				else
				{
					_obj = (T)value;
					dockControl();
				}
			}
		}
		#endregion
		#region methods
		public override string ToString()
		{
			return Name;
		}

		public void AssignValue(object value)
		{
			_obj = (T)value;
			dockControl();
		}
		public void AssignValueAndType(object value)
		{
			_obj = (T)value;
			dockControl();
		}
		public void OnCustomValueChanged(string name, object value)
		{
			if (_customValueChange != null)
			{
				_customValueChange(this, new PropertyChangedEventArgs(name));
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
			return GetEvents(null);
		}

		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			bool includeSpecialName = false;
			EnumReflectionMemberInfoSelectScope scope = EnumReflectionMemberInfoSelectScope.InstanceOnly;
			if (attributes != null)
			{
				for (int i = 0; i < attributes.Length; i++)
				{
					ReflectionMemberInfoSelectIncludeSpecialAttribute incS = attributes[i] as ReflectionMemberInfoSelectIncludeSpecialAttribute;
					if (incS != null)
					{
						includeSpecialName = true;
					}
					else
					{
						ReflectionMemberInfoSelectScopeAttribute rs = attributes[i] as ReflectionMemberInfoSelectScopeAttribute;
						if (rs != null)
						{
							scope = rs.Scope;
						}
					}
				}
			}
			if (_forStatic)
			{
				scope = EnumReflectionMemberInfoSelectScope.StaticOnly;
			}
			//properties to be collected
			PropertyDescriptorCollection newProps = new PropertyDescriptorCollection(null);
			bool gotName = false;
			//find Name property: non-IComponent does not have a name by its own ======================================
			if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this, attributes, true);
				// For each property use a property descriptor of our own
				foreach (PropertyDescriptor oProp in baseProps)
				{
					if (string.Compare(oProp.Name, "Name", StringComparison.Ordinal) == 0)
					{
						if (oProp.SerializationVisibility == DesignerSerializationVisibility.Visible)
						{
							if (NameReadOnly)
							{
								ReadOnlyPropertyDesc rp = new ReadOnlyPropertyDesc(oProp);
								newProps.Add(rp);
							}
							else
							{
								newProps.Add(oProp);
							}
							gotName = true;
							break;
						}
					}
				}
			}
			if (scope == EnumReflectionMemberInfoSelectScope.StaticOnly)
			{
				string name = typeof(T).Name;
				MethodInfo mif = typeof(T).GetMethod("GetStaticName", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				if (mif != null)
				{
					object v = mif.Invoke(null, null);
					if (v != null)
					{
						name = v.ToString();
					}
				}
				PropertyInfoDescriptor<T> nameProp = new PropertyInfoDescriptor<T>("Name", new Attribute[] { ReadOnlyAttribute.Yes, new ParenthesizePropertyNameAttribute(true) }, typeof(string), name);
				newProps.Add(nameProp);
				gotName = true;
			}
			//===end of getting Name property===================================================================================
			//===get object properties=============================
			if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				if (ValueType.IsAbstract)
				{
					createPropertyInfoDescriptor(includeSpecialName, attributes, newProps, false);
				}
				else
				{
					PropertyDescriptorCollection objProps;
					IClassInstance ci = ObjectValue as IClassInstance;
					if (ci != null)
					{
						objProps = ci.GetInstanceProperties(attributes);
					}
					else
					{
						if (ObjectValue == null) //no default constructor
						{
							objProps = VPLUtil.GetProperties(typeof(T), scope, false, false, false);
						}
						else
						{
							objProps = TypeDescriptor.GetProperties(this.ObjectValue, new Attribute[] { DesignOnlyAttribute.No }, true);
						}
					}
					foreach (PropertyDescriptor oProp in objProps)
					{
						if (string.Compare(oProp.Name, "Name", StringComparison.Ordinal) == 0)
						{
							if (gotName)
							{
								continue;
							}
						}
						if (string.Compare(oProp.Name, "Site", StringComparison.Ordinal) == 0
							|| string.Compare(oProp.Name, "WindowTarget", StringComparison.Ordinal) == 0)
						{
							continue;
						}
						XValueDesctiptor np = new XValueDesctiptor(oProp, this);
						newProps.Add(np);
					}
				}
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.StaticOnly)
			{
				createPropertyInfoDescriptor(includeSpecialName, attributes, newProps, true);
			}
			if (!typeof(T).Assembly.GlobalAssemblyCache)
			{
				PropertyDescriptorInformation pdi = new PropertyDescriptorInformation("LibLocation", typeof(T).Assembly.Location, this.GetType());
				newProps.Add(pdi);
			}
			return newProps;
		}
		private void createPropertyInfoDescriptor(bool includeSpecialName, Attribute[] attributes, PropertyDescriptorCollection newProps, bool forStatic)
		{
			PropertyInfo[] pifs;
			if (forStatic)
			{
				pifs = this.ValueType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			}
			else
			{
				pifs = ValueType.GetProperties();
			}
			if (pifs != null && pifs.Length > 0)
			{
				if (!includeSpecialName)
				{
					List<PropertyInfo> l = new List<PropertyInfo>();
					for (int i = 0; i < pifs.Length; i++)
					{
						if (!pifs[i].IsSpecialName)
						{
							l.Add(pifs[i]);
						}
					}
					if (l.Count < pifs.Length)
					{
						pifs = new PropertyInfo[l.Count];
						l.CopyTo(pifs);
					}
				}
				for (int i = 0; i < pifs.Length; i++)
				{
					object[] ca = pifs[i].GetCustomAttributes(true);
					Attribute[] ats;
					if (ca == null || ca.Length == 0)
						ats = attributes;
					else
					{
						int k = 0;
						if (attributes != null && attributes.Length > 0)
						{
							ats = new Attribute[attributes.Length + ca.Length];
							while (k < attributes.Length)
							{
								ats[k] = attributes[k];
								k++;
							}
						}
						else
						{
							ats = new Attribute[ca.Length];
						}
						for (int j = 0; j < ca.Length; j++)
						{
							ats[k + j] = (Attribute)ca[j];
						}
					}
					PropertyInfoDescriptor<T> prop = new PropertyInfoDescriptor<T>(pifs[i].Name, ats, pifs[i], forStatic);
					newProps.Add(prop);
				}
			}
		}
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion
		#region XValueDesctiptor
		public class XValueDesctiptor : PropertyDescriptor
		{
			XControl<T> _owner;
			PropertyDescriptor _originalDescriptor;
			public XValueDesctiptor(PropertyDescriptor d, XControl<T> owner)
				: base(d)
			{
				_owner = owner;
				_originalDescriptor = d;
			}

			public override bool CanResetValue(object component)
			{
				return _originalDescriptor.CanResetValue(((XControl<T>)component).ObjectValue);
			}

			public override Type ComponentType
			{
				get { return typeof(XControl<T>); }
			}

			public override object GetValue(object component)
			{
				return _originalDescriptor.GetValue(((XControl<T>)component).ObjectValue);
			}

			public override bool IsReadOnly
			{
				get
				{
					return _originalDescriptor.IsReadOnly;
				}
			}

			public override Type PropertyType
			{
				get
				{
					return _originalDescriptor.PropertyType;
				}
			}

			public override void ResetValue(object component)
			{
				_originalDescriptor.ResetValue(((XControl<T>)component).ObjectValue);
			}

			public override void SetValue(object component, object value)
			{
				_originalDescriptor.SetValue(((XControl<T>)component).ObjectValue, value);
				_owner.OnCustomValueChanged(Name, value);
			}

			public override bool ShouldSerializeValue(object component)
			{
				return _originalDescriptor.ShouldSerializeValue(((XControl<T>)component).ObjectValue);
			}
		}
		#endregion
		#region ICustomEventMethodType Members
		public void MakeStatic()
		{
			_forStatic = true;
		}
		public EventInfo[] GetEvents(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly)
		{
			EventInfo[] eArray;
			if (scope == EnumReflectionMemberInfoSelectScope.Both)
			{
				eArray = typeof(T).GetEvents();
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				eArray = typeof(T).GetEvents(BindingFlags.Public | BindingFlags.Instance);
			}
			else
			{
				eArray = typeof(T).GetEvents(BindingFlags.Public | BindingFlags.Static);
			}
			return VPLUtil.GetEvents(eArray, includeSpecialName, browsableOnly);
		}

		public MethodInfo[] GetMethods(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly, bool webOnly)
		{
			MethodInfo[] eArray;
			if (scope == EnumReflectionMemberInfoSelectScope.Both)
			{
				eArray = typeof(T).GetMethods();
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				eArray = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
			}
			else
			{
				eArray = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static);
			}
			return VPLUtil.GetMethods(eArray, includeSpecialName, browsableOnly, webOnly);
		}
		public FieldInfo[] GetFields(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly)
		{
			FieldInfo[] eArray;
			if (scope == EnumReflectionMemberInfoSelectScope.Both)
			{
				eArray = typeof(T).GetFields();
			}
			else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
			{
				eArray = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
			}
			else
			{
				eArray = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);
			}
			return VPLUtil.GetFields(eArray, includeSpecialName, browsableOnly);
		}
		public PropertyDescriptorCollection GetProperties(EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly, bool includeAbstract)
		{
			List<Attribute> attrList = new List<Attribute>();
			attrList.Add(DesignOnlyAttribute.No);
			attrList.Add(new ReflectionMemberInfoSelectScopeAttribute(scope));
			if (browsableOnly)
			{
				attrList.Add(BrowsableAttribute.Yes);
			}
			if (includeSpecialName)
			{
				attrList.Add(ReflectionMemberInfoSelectIncludeSpecialAttribute.Yes);
			}
			PropertyDescriptorCollection props = GetProperties(attrList.ToArray());
			return props;
		}
		public PropertyDescriptor GetProperty(string propertyName)
		{
			List<Attribute> attrList = new List<Attribute>();
			attrList.Add(DesignOnlyAttribute.No);
			PropertyDescriptorCollection props = GetProperties(attrList.ToArray());
			foreach (PropertyDescriptor p in props)
			{
				if (p.Name == propertyName)
				{
					return p;
				}
			}
			return null;
		}
		public void HookCustomPropertyValueChange(EventHandler h)
		{
			_customValueChange = h;
		}
		#endregion

		#region IBeforeXmlNodeSerialize Members

		public void OnReadFromXmlNode(IXmlCodeReader serializer, XmlNode node)
		{
			dockControl();
		}

		public void OnWriteToXmlNode(IXmlCodeWriter serializer, XmlNode node)
		{

		}

		#endregion

		#region IPostDeserializeProcess Members

		public void OnDeserialize(object context)
		{
			dockControl();
			IPostDeserializeProcess pdp = _obj as IPostDeserializeProcess;
			if (pdp != null)
			{
				pdp.OnDeserialize(context);
			}
			else
			{
				ISqlDataSet sds = _obj as ISqlDataSet;
				if (sds != null)
				{
					sds.CreateDataTable();
				}
			}
		}

		#endregion
	}
	#region ConstructorValueDesctiptor
	/// <summary>
	/// one parameter value for constructor
	/// </summary>
	public class ConstructorValueDesctiptor : PropertyDescriptor
	{
		IConstructorUser _owner;
		Type _type;
		public ConstructorValueDesctiptor(string name, Attribute[] attrs, Type valueType, IConstructorUser owner)
			: base(name, attrs)
		{
			_owner = owner;
			_type = valueType;
		}

		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get { return _owner.GetType(); }
		}

		public override object GetValue(object component)
		{
			return _owner.GetConstructorValue(Name);
		}

		public override bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public override Type PropertyType
		{
			get
			{
				object v = _owner.GetConstructorValue(Name);
				if (v != null)
				{
					return v.GetType();
				}
				return _type;
			}
		}

		public override void ResetValue(object component)
		{
			_owner.ResetConstructorValue(Name);
		}

		public override void SetValue(object component, object value)
		{
			_owner.SetConstructorValue(Name, value);
			_owner.OnCustomValueChanged(Name, value);
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}
	#endregion
	#region TypeEditorConstructorSelect
	class TypeEditorConstructorSelect : UITypeEditor
	{
		public TypeEditorConstructorSelect()
		{
		}
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			if (context != null && context.Instance != null && provider != null)
			{
				IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (edSvc != null)
				{
					IConstructorUser owner = context.Instance as IConstructorUser;
					if (owner != null)
					{
						ConstructorInfo[] cifs = owner.GetConstructors();
						if (cifs != null && cifs.Length > 0)
						{
							ValueList list = new TypeEditorConstructorSelect.ValueList(edSvc, cifs);
							edSvc.DropDownControl(list);
							if (list.MadeSelection)
							{
								value = list.Selection;
							}
						}
					}
				}
			}
			return value;
		}
		class ValueList : ListBox
		{
			public bool MadeSelection;
			public ConstructorInfo Selection;
			private IWindowsFormsEditorService _service;
			class ConstructorDisplay
			{
				public ConstructorDisplay(ConstructorInfo c)
				{
					Constructor = c;
				}
				public ConstructorInfo Constructor { get; set; }
				public override string ToString()
				{
					StringBuilder sb = new StringBuilder();
					ParameterInfo[] pifs = Constructor.GetParameters();
					if (pifs != null && pifs.Length > 0)
					{
						sb.Append(pifs[0].ParameterType.Name);
						sb.Append(" ");
						sb.Append(pifs[0].Name);
						for (int i = 1; i < pifs.Length; i++)
						{
							sb.Append(", ");
							sb.Append(pifs[i].ParameterType.Name);
							sb.Append(" ");
							sb.Append(pifs[i].Name);
						}
					}
					return sb.ToString();
				}
			}
			public ValueList(IWindowsFormsEditorService service, ConstructorInfo[] values)
			{
				_service = service;
				if (values != null && values.Length > 0)
				{
					for (int i = 0; i < values.Length; i++)
					{
						Items.Add(new ConstructorDisplay(values[i]));
					}
				}
			}
			private void finishSelection()
			{
				if (SelectedIndex >= 0)
				{
					MadeSelection = true;
					Selection = ((ConstructorDisplay)Items[SelectedIndex]).Constructor;
				}
				_service.CloseDropDown();
			}
			protected override void OnClick(EventArgs e)
			{
				base.OnClick(e);
				finishSelection();

			}
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				base.OnKeyPress(e);
				if (e.KeyChar == '\r')
				{
					finishSelection();
				}
			}
		}
	}
	#endregion
	#region ConstructorSelectDesctiptor
	public class ConstructorSelectDesctiptor : PropertyDescriptor
	{
		IConstructorUser _owner;
		public ConstructorSelectDesctiptor(IConstructorUser owner)
			: base("Constructor", new Attribute[] {
                    new RefreshPropertiesAttribute(RefreshProperties.All),
                    new EditorAttribute(typeof(TypeEditorConstructorSelect),typeof(UITypeEditor)),
                    new ParenthesizePropertyNameAttribute(true)
                })
		{
			_owner = owner;
		}

		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get { return _owner.GetType(); }
		}

		public override object GetValue(object component)
		{
			return _owner.GetConstructorDisplay();
		}

		public override bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public override Type PropertyType
		{
			get
			{
				return typeof(string);
			}
		}

		public override void ResetValue(object component)
		{
		}

		public override void SetValue(object component, object value)
		{
			ConstructorInfo c = value as ConstructorInfo;
			if (c != null)
			{
				_owner.SetConstructor(c);
				_owner.OnCustomValueChanged(Name, value);
			}
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}
	#endregion
	#region OwnerConstructorSelectDesctiptor
	public class OwnerConstructorSelectDesctiptor : PropertyDescriptor
	{
		IConstructorOwner _owner;
		public OwnerConstructorSelectDesctiptor(IConstructorOwner owner)
			: base("Constructor", new Attribute[] {
                    new RefreshPropertiesAttribute(RefreshProperties.All),
                    new EditorAttribute(typeof(TypeEditorConstructorSelect),typeof(UITypeEditor)),
                    new ParenthesizePropertyNameAttribute(true),
                    new TypeConverterAttribute(typeof(ExpandableObjectConverter))
                })
		{
			_owner = owner;
		}

		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get { return _owner.GetType(); }
		}

		public override object GetValue(object component)
		{
			return _owner.GetCurrentConstructor();
		}

		public override bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public override Type PropertyType
		{
			get
			{
				return typeof(ConstValueSelector);
			}
		}

		public override void ResetValue(object component)
		{
		}

		public override void SetValue(object component, object value)
		{
			ConstructorInfo c = value as ConstructorInfo;
			if (c != null)
			{
				_owner.SetConstructor(c);
				_owner.OnCustomValueChanged(Name, value);
			}
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}
	#endregion
	#region ConstValueSelector
	public class ConstValueSelector : ICustomTypeDescriptor, IConstructorUser, IPostXmlNodeSerialize
	{
		#region fields and constructors
		private IConstructorUser _parent;
		private string _name;
		private Type _type;
		private Dictionary<string, Type> _constructorParameters;
		private Dictionary<string, object> _constructorValues;//separate from _constructorParameters so that _constructorParameters may change without removing values
		public ConstValueSelector(IConstructorUser parent)
		{
			_parent = parent;
		}
		public ConstValueSelector(Type t, IConstructorUser parent, string name)
		{
			_parent = parent;
			_name = name;
			_type = t;
			_constructorParameters = new Dictionary<string, Type>();
			ConstructorInfo c = t.GetConstructors()[0];
			ParameterInfo[] pifs = c.GetParameters();
			if (pifs != null)
			{
				for (int i = 0; i < pifs.Length; i++)
				{
					_constructorParameters.Add(pifs[i].Name, pifs[i].ParameterType);
				}
			}
		}
		#endregion

		#region Properties
		[Browsable(false)]
		[NotForProgramming]
		public Type ValueType
		{
			get { return _type; }
			set { _type = value; }
		}
		[Browsable(false)]
		[NotForProgramming]
		public string ValueName
		{
			get { return _name; }
			set { _name = value; }
		}
		[Browsable(false)]
		[NotForProgramming]
		public int ParameterCount
		{
			get
			{
				if (_constructorParameters == null)
					return 0;
				return _constructorParameters.Count;
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public Dictionary<string, Type> Parameters
		{
			get
			{
				return _constructorParameters;
			}
		}
		#endregion
		#region Methods
		public bool HasValue(string name)
		{
			if (_constructorValues != null)
			{
				return _constructorValues.ContainsKey(name);
			}
			return false;
		}
		public void ResetParameterTypes()
		{
			_constructorParameters = new Dictionary<string, Type>();
		}
		public void AddParameter(string parameerName, Type parameterType)
		{
			_constructorParameters.Add(parameerName, parameterType);
		}
		public override string ToString()
		{
			return GetConstructorDisplay();
		}
		[Browsable(false)]
		[NotForProgramming]
		public Dictionary<string, object> GetConstructorParameterValues()
		{
			return _constructorValues;
		}
		[Browsable(false)]
		[NotForProgramming]
		public CodeExpression CreateExpression()
		{
			CodeExpression exp = null;
			if (_type != null)
			{
				CodeObjectCreateExpression coc = new CodeObjectCreateExpression();
				coc.CreateType = new CodeTypeReference(_type);
				if (_constructorValues != null && _constructorValues.Count > 0)
				{
					foreach (KeyValuePair<string, object> kv in _constructorValues)
					{
						CodeExpression pe = null;
						ConstValueSelector cvs = kv.Value as ConstValueSelector;
						if (cvs != null)
						{
							pe = cvs.CreateExpression();
						}
						else
						{
							pe = ObjectCreationCodeGen.ObjectCreationCode(kv.Value);
						}
						if (pe == null)
						{
							pe = new CodePrimitiveExpression(null);
						}
						coc.Parameters.Add(pe);
					}
				}
				exp = coc;
			}
			if (exp == null)
			{
				exp = new CodePrimitiveExpression(null);
			}
			return exp;
		}
		#endregion

		#region ICustomTypeDescriptor Members
		[Browsable(false)]
		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}
		[Browsable(false)]
		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}
		[Browsable(false)]
		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}
		[Browsable(false)]
		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}
		[Browsable(false)]
		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}
		[Browsable(false)]
		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}
		[Browsable(false)]
		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}
		[Browsable(false)]
		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}
		[Browsable(false)]
		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}
		[Browsable(false)]
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			List<PropertyDescriptor> pl = new List<PropertyDescriptor>();
			IConstructorOwner p = _parent as IConstructorOwner;
			if (p == null)
			{
				if (VPLUtil.NeedConstructor(_type))
				{
					pl.Add(new ConstructorSelectDesctiptor(this));
				}
			}
			//
			ConstructorInfo[] cifs = _type.GetConstructors();
			if (_constructorParameters == null)
			{
				if (cifs != null && cifs.Length > 0)
				{
					if (_type.GetConstructor(Type.EmptyTypes) == null)
					{
						ParameterInfo[] pifs = cifs[0].GetParameters();
						if (pifs != null && pifs.Length > 0)
						{
							_constructorParameters = new Dictionary<string, Type>();
							for (int i = 0; i < pifs.Length; i++)
							{
								_constructorParameters.Add(pifs[i].Name, pifs[i].ParameterType);
							}
						}
					}
				}
			}
			if (_constructorParameters != null)
			{
				foreach (KeyValuePair<string, Type> kv in _constructorParameters)
				{
					Attribute[] attrs;
					if (kv.Value.IsPrimitive || kv.Value.IsEnum || kv.Value.Equals(typeof(string)))
					{
						attrs = new Attribute[] { };
					}
					else
					{
						attrs = new Attribute[] {
                            new TypeConverterAttribute(typeof(ExpandableObjectConverter))
                        };
					}
					ConstructorValueDesctiptor cvd = new ConstructorValueDesctiptor(kv.Key, attrs, kv.Value, this);
					pl.Add(cvd);
				}
			}
			PropertyDescriptorCollection ps = new PropertyDescriptorCollection(pl.ToArray());
			return ps;
		}
		[Browsable(false)]
		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}
		[Browsable(false)]
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion

		#region IConstructorUser Members
		[Browsable(false)]
		[NotForProgramming]
		public ConstructorInfo[] GetConstructors()
		{
			return _type.GetConstructors();
		}
		[Browsable(false)]
		[NotForProgramming]
		public object GetConstructorValue(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				object v = null;
				if (_constructorValues != null)
				{
					if (_constructorValues.TryGetValue(name, out v))
					{
					}
				}
				if (v == null)
				{
					Type t = null;
					if (_constructorParameters != null)
					{
						_constructorParameters.TryGetValue(name, out t);
					}
					if (t != null)
					{
						if (VPLUtil.NeedConstructor(t))
						{
							ConstValueSelector cv = new ConstValueSelector(t, this, name);
							v = cv;
						}
						else
						{
							v = VPLUtil.GetDefaultValue(t);
						}
					}
					SetConstructorValue(name, v);
				}
				return v;
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public void SetConstructorValue(string name, object v)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_constructorValues == null)
				{
					_constructorValues = new Dictionary<string, object>();
				}
				if (_constructorValues.ContainsKey(name))
				{
					_constructorValues[name] = v;
				}
				else
				{
					_constructorValues.Add(name, v);
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void ResetConstructorValue(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_constructorValues != null)
				{
					if (_constructorValues.ContainsKey(name))
					{
						Type t = null;
						if (_constructorParameters != null)
						{
							_constructorParameters.TryGetValue(name, out t);
						}
						if (t != null)
						{
							_constructorValues[name] = VPLUtil.GetDefaultValue(t);
						}
					}
				}
			}
		}
		public string GetConstructorDisplay()
		{
			StringBuilder sb = new StringBuilder();
			if (_constructorParameters != null && _constructorParameters.Count > 0)
			{
				bool first = true;
				foreach (KeyValuePair<string, Type> kv in _constructorParameters)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						sb.Append(", ");
					}
					sb.Append(kv.Value.Name);
					sb.Append(" ");
					sb.Append(kv.Key);
				}
			}
			return sb.ToString();
		}

		public void SetConstructor(ConstructorInfo cif)
		{
			_constructorParameters = new Dictionary<string, Type>();
			if (cif != null)
			{
				ParameterInfo[] pifs = cif.GetParameters();
				if (pifs != null)
				{
					for (int i = 0; i < pifs.Length; i++)
					{
						_constructorParameters.Add(pifs[i].Name, pifs[i].ParameterType);
					}
				}
			}
		}

		public void OnCustomValueChanged(string name, object value)
		{
			if (_parent != null)
			{
				_parent.SetConstructorValue(name, value);
				_parent.OnCustomValueChanged(name, value);
			}
		}
		public object GetValue()
		{
			if (_type.IsAbstract)
				return null;
			object[] vs = null;
			if (_constructorParameters != null && _constructorParameters.Count > 0)
			{
				int n = 0;
				vs = new object[_constructorParameters.Count];
				foreach (KeyValuePair<string, Type> kv in _constructorParameters)
				{
					object v = null;
					if (_constructorValues != null)
					{
						_constructorValues.TryGetValue(kv.Key, out v);
					}
					if (v == null)
					{
						v = VPLUtil.GetDefaultValue(_type);
					}
					else
					{
						ConstValueSelector cv = v as ConstValueSelector;
						if (cv != null)
						{
							v = cv.GetValue();
						}
					}
					vs[n] = v;
					n++;
				}
			}
			return Activator.CreateInstance(_type, vs);
		}

		#endregion

		#region IPostXmlNodeSerialize Members
		internal const string XML_ObjProperty = "ObjProperty";
		internal const string XML_Item = "Item";
		internal const string XML_Value = "Value";
		internal const string XML_Props = "Properties";
		internal const string XML_ConstructorValues = "ConstructorValues";
		public void OnReadFromXmlNode(IXmlCodeReader serializer, XmlNode node)
		{
			XmlNodeList nds;
			_constructorParameters = new Dictionary<string, Type>();
			_constructorValues = new Dictionary<string, object>();
			nds = node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"{0}/{1}", XML_ConstructorValues, XML_Item));
			if (nds != null)
			{
				for (int i = 0; i < nds.Count; i++)
				{
					string name = serializer.GetName(nds[i]);
					Type t = serializer.ReadValue<Type>(nds[i], this);
					_constructorParameters.Add(name, t);
					XmlNode nv = nds[i].SelectSingleNode(XML_Value);
					if (nv != null)
					{
						object v;
						XmlNode objNode = nv.SelectSingleNode(ConstValueSelector.XML_ObjProperty);
						if (objNode != null)
						{
							ConstValueSelector cv = new ConstValueSelector(t, this, name);
							serializer.ReadObjectFromXmlNode(objNode, cv, cv.GetType(), this);
							v = cv;
						}
						else
						{
							v = serializer.ReadValue(nv, this, t);
						}
						_constructorValues.Add(name, v);
					}
				}
			}

		}

		public void OnWriteToXmlNode(IXmlCodeWriter serializer, XmlNode node)
		{
			if (_constructorParameters != null && _constructorParameters.Count > 0)
			{
				XmlNode nps = serializer.CreateSingleNewElement(node, ConstValueSelector.XML_ConstructorValues);
				foreach (KeyValuePair<string, Type> kv in _constructorParameters)
				{
					XmlNode nd = node.OwnerDocument.CreateElement(ConstValueSelector.XML_Item);
					nps.AppendChild(nd);
					serializer.SetName(nd, kv.Key);
					serializer.WriteValue(nd, kv.Value, this);
					if (_constructorValues != null)
					{
						object v;
						if (_constructorValues.TryGetValue(kv.Key, out v))
						{
							XmlNode nv = serializer.CreateSingleNewElement(nd, ConstValueSelector.XML_Value);
							serializer.WriteValue(nv, v, this);
						}
					}
				}
			}
		}

		#endregion
	}
	#endregion
	#region PropertyDescriptorVirtualValue
	class PropertyDescriptorVirtualValue : PropertyDescriptor
	{
		private PropertyInfo _pif;
		private IXType _owner;
		public PropertyDescriptorVirtualValue(string name, Attribute[] attrs, PropertyInfo info, IXType owner)
			: base(name, attrs)
		{
			_pif = info;
			_owner = owner;
		}

		public override bool CanResetValue(object component)
		{
			return _pif.CanWrite;
		}

		public override Type ComponentType
		{
			get { return _owner.GetType(); }
		}

		public override object GetValue(object component)
		{
			return _owner.GetPropertyValue(Name);
		}

		public override bool IsReadOnly
		{
			get { return !_pif.CanWrite; }
		}

		public override Type PropertyType
		{
			get { return _pif.PropertyType; }
		}

		public override void ResetValue(object component)
		{
			_owner.ResetPropertyValue(Name);
		}

		public override void SetValue(object component, object value)
		{
			_owner.SetPropertyValue(Name, value);
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}
	#endregion
}
