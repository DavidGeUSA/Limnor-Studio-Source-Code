/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom;
using ProgElements;
using MathExp;
using System.Drawing;
using System.Xml;
using XmlSerializer;
using XmlUtility;
using System.ComponentModel;
using LimnorDesigner.MenuUtil;
using System.Drawing.Design;
using VPL;
using System.Collections.Specialized;
using System.Globalization;
using LimnorDesigner.Action;
using System.Collections;
using System.Reflection;
using Limnor.WebBuilder;

namespace LimnorDesigner.MethodBuilder
{
	/// <summary>
	/// a local variable in a method.
	/// it represnts the type of the variable only. the constructor action and parameters are in the constructor action.
	/// </summary>
	[UseParentObject]
	public class LocalVariable : IClass, IXmlNodeSerializable, ILocalvariable
	{
		#region fields and constructors
		const char INVCHAR = '_';
		private IObjectPointer _owner; //a MethodClass
		private DataTypePointer _type;
		private EnumWebRunAt _runAt = EnumWebRunAt.Unknown;
		private string _name;
		private UInt32 _classId;
		private UInt32 _memberId;
		private object _instance;
		private object _debug;
		private EventHandler _nameChecker;
		private EventHandler _typeChangeNotify;
		public LocalVariable()
		{
		}
		public LocalVariable(DataTypePointer type, string name, UInt32 classId, UInt32 memberId)
		{
			_type = type;
			_name = name;
			_classId = classId;
			_memberId = memberId;
		}
		private void onTypeChanged(object sender, EventArgs e)
		{
			if (_typeChangeNotify != null)
			{
				_typeChangeNotify(this, e);
			}
		}
		#endregion

		#region static help
		//hold variables not saved yet
		private static Dictionary<UInt32, Dictionary<UInt32, LocalVariable>> _variables;
		public static void SaveLocalVariable(LocalVariable v)
		{
			if (_variables == null)
			{
				_variables = new Dictionary<uint, Dictionary<uint, LocalVariable>>();
			}
			Dictionary<UInt32, LocalVariable> vs;
			if (!_variables.TryGetValue(v.ClassId, out vs))
			{
				vs = new Dictionary<uint, LocalVariable>();
				_variables.Add(v.ClassId, vs);
			}
			if (!vs.ContainsKey(v.MemberId))
			{
				vs.Add(v.MemberId, v);
			}
		}
		public static void RemoveLocalVariable(LocalVariable v)
		{
			if (_variables != null)
			{
				Dictionary<UInt32, LocalVariable> vs;
				if (_variables.TryGetValue(v.ClassId, out vs))
				{
					if (vs.ContainsKey(v.MemberId))
					{
						vs.Remove(v.MemberId);
					}
				}
			}
		}
		public static LocalVariable GetUnsavedLocalVariable(UInt32 classId, UInt32 memberId)
		{
			if (_variables != null)
			{
				Dictionary<UInt32, LocalVariable> vs;
				if (_variables.TryGetValue(classId, out vs))
				{
					LocalVariable v;
					if (vs.TryGetValue(memberId, out v))
					{
						return v;
					}
				}
			}
			else
			{
			}
			return null;
		}
		#endregion

		#region Methods
		public CodeVariableDeclarationStatement CreateVariableDeclaration()
		{
			Type t = this.GetResolvedDataType();
			if (t != null && !typeof(VoidAction).Equals(t))
			{
				CodeTypeReference ctf = this.GetCodeTypeReference();
				if (ctf != null)
				{
					ArrayPointer ap = _type as ArrayPointer;
					if (ap != null)
					{
						int[] dims = ap.Dimnesions;
						if (dims != null && dims.Length == 1)
						{
							if (dims[0] >= 0)
							{
								if (!t.FullName.EndsWith("[][]", StringComparison.Ordinal))
								{
									CodeArrayCreateExpression init = new CodeArrayCreateExpression(ap.ItemBaseType, dims[0]);
									return new CodeVariableDeclarationStatement(ctf, this.CodeName, init);
								}
							}
						}
					}
					return CompilerUtil.CreateVariableDeclaration(ctf, this.CodeName, this.BaseClassType, this.BaseClassType.IsPrimitive?this.ObjectInstance:null);
				}
			}
			return null;
		}
		public void AddVariableDeclaration(CodeStatementCollection csc)
		{
			CodeVariableDeclarationStatement p = CreateVariableDeclaration();
			if (p != null)
				csc.Add(p);
		}
		public IList<DataTypePointer> GetGenericTypes()
		{
			if (ClassType != null)
			{
				return _type.GetGenericTypes();
			}
			return null;
		}
		public DataTypePointer[] GetConcreteTypes()
		{
			if (ClassType != null)
			{
				return _type.TypeParameters;
			}
			return null;
		}
		public Type GetResolvedDataType()
		{
			if (ClassType != null && BaseClassType != null)
			{
				if (BaseClassType.IsGenericParameter)
				{
					if (_type.ConcreteType == null)
					{
						MethodClass mc = this.Owner as MethodClass;
						if (mc != null)
						{
							DataTypePointer dp = mc.GetConcreteType(BaseClassType);
							if (dp != null)
							{
								_type.SetConcreteType(dp);
							}
						}
					}
					if (_type.ConcreteType != null)
					{
						return _type.ConcreteType.BaseClassType;
					}
				}
			}
			return BaseClassType;
		}
		public DataTypePointer GetConcreteType(Type typeParameter)
		{
			if (ClassType != null)
			{
				DataTypePointer dp = _type.GetConcreteType(typeParameter);
				if (dp != null)
				{
					return dp;
				}
			}
			IGenericTypePointer igp = null;
			IObjectPointer op = Owner;
			while (op != null)
			{
				igp = op as IGenericTypePointer;
				if (igp != null)
					break;
				op = op.Owner;
			}
			if (igp != null)
			{
				return igp.GetConcreteType(typeParameter);
			}
			return null;
		}
		public void SetTypeChangeNotify(EventHandler notify)
		{
			_typeChangeNotify = notify;
			if (ClassType != null)
			{
				_type.TypeChanged += new EventHandler(onTypeChanged);
			}
		}

		public void SetNameChecker(EventHandler checker)
		{
			_nameChecker = checker;
		}
		public virtual ComponentIconLocal CreateComponentIcon(ILimnorDesigner designer, MethodClass method)
		{
			return new ComponentIconLocal(designer, this, method);
		}
		public void SetName(string name)
		{
			_name = name;
		}
		public void SetMemberId(UInt32 id)
		{
			_memberId = id;
		}
		public override string ToString()
		{
			return _name;
		}
		public virtual void SetDataType(DataTypePointer type)
		{
			if (_type == null)
			{
				_type = type;
			}
			else
			{
				_type.SetDataType(type);
			}
		}
		#endregion

		#region Properties
		[ReadOnly(true)]
		[Editor(typeof(PropEditorDataType), typeof(UITypeEditor))]
		[Description("Data type for the variable")]
		public virtual DataTypePointer ClassType
		{
			get
			{
				if (_type == null)
				{
					OnResolveType();
				}
				return _type;
			}
			set
			{
				if (_type == null)
				{
					OnResolveType();
				}
				if (_type != null)
				{
					_type.SetDataType(value);
					if (_typeChangeNotify != null)
					{
						_typeChangeNotify(this, EventArgs.Empty);
					}
				}
			}
		}
		[Browsable(false)]
		public virtual DataTypePointer ValueType { get { return ClassType; } }
		[Browsable(false)]
		public Type BaseClassType
		{
			get
			{
				if (ObjectType != null)
				{
					return _type.BaseClassType;
				}
				return null;
			}
		}
		/// <summary>
		/// the sub method declaring the variable
		/// </summary>
		[Browsable(false)]
		public UInt32 ScopeGroupId
		{
			get;
			set;
		}
		[Browsable(false)]
		protected virtual Image IconImage
		{
			get
			{
				if (ClassType != null)
				{
					return _type.ImageIcon;
				}
				return null;
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public EnumWebRunAt RunAt0
		{
			get
			{
				return _runAt;
			}
			set
			{
				_runAt = value;
			}
		}
		#endregion

		#region IClass Members
		[Browsable(false)]
		public Image ImageIcon
		{
			get
			{
				return IconImage;
			}
			set
			{

			}
		}
		[Browsable(false)]
		public virtual Type VariableLibType
		{
			get
			{
				if (ClassType != null)
					return _type.VariableLibType;
				return null;
			}
		}
		[Browsable(false)]
		public virtual ClassPointer VariableCustomType
		{
			get
			{
				if (ClassType != null)
					return _type.VariableCustomType;
				return null;
			}
		}
		[Browsable(false)]
		public virtual IClassWrapper VariableWrapperType
		{
			get
			{
				return null;
			}
		}

		#endregion

		#region IObjectPointer Members
		public EnumWebRunAt RunAt
		{
			get
			{
				if (_runAt != EnumWebRunAt.Unknown)
				{
					return _runAt;
				}
				if (_owner != null)
				{
					return _owner.RunAt;
				}
				return EnumWebRunAt.Inherit;
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
				if (root != null)
					return root.RootPointer;
				return null;
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
		public IObjectPointer Owner
		{
			get
			{
				return _owner;
			}
			set
			{
				_owner = value;
			}
		}
		[Browsable(false)]
		public IPropertyPointer PropertyOwner { get { return Owner; } }
		[Browsable(false)]
		public virtual Type ObjectType
		{
			get
			{
				if (_type != null)
					return _type.BaseClassType;
				return null;
			}
			set
			{
				if (_type != null)
				{
					_type.SetDataType(value);
				}
				else
				{
					_type = new DataTypePointer(new TypePointer(value));
				}
			}
		}
		[Browsable(false)]
		public object ObjectInstance
		{
			get
			{
				if (_instance == null)
				{
					if (this.BaseClassType != null)
					{
						try
						{
							if (this.BaseClassType.IsValueType)
							{
								_instance = VPLUtil.GetDefaultValue(this.BaseClassType);
							}
							else
							{
								ConstructorInfo cif = this.BaseClassType.GetConstructor(Type.EmptyTypes);
								if (cif != null)
								{
									_instance = Activator.CreateInstance(this.BaseClassType);
								}
							}
						}
						catch
						{
						}
					}
				}
				return _instance;
			}
			set
			{
				_instance = value;
			}
		}
		[Browsable(false)]
		public object ObjectDebug
		{
			get
			{
				return _debug;
			}
			set
			{
				_debug = value;
			}
		}
		/// <summary>
		/// variable name
		/// </summary>
		[Browsable(false)]
		public virtual string CodeName
		{
			get
			{
				string nm;
				if (VPLUtil.CompilerContext_PHP)
				{
					nm = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"${0}{1}", _name, _memberId.ToString("x", System.Globalization.CultureInfo.InvariantCulture));
				}
				else
				{
					nm = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}{1}", _name, _memberId.ToString("x", System.Globalization.CultureInfo.InvariantCulture));
				}
				nm = VPLUtil.NameToCodeName(nm);
				return nm;
			}
		}
		[Browsable(false)]
		public string ReferenceName
		{
			get
			{
				return _name;
			}
		}
		[Browsable(false)]
		public string DisplayName
		{
			get { return _name; }
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
			get { return DisplayName; }
		}
		public bool IsTargeted(EnumObjectSelectType target)
		{
			return (target == EnumObjectSelectType.All || target == EnumObjectSelectType.Object);
		}
		[Browsable(false)]
		public string ObjectKey
		{
			get { return MemberId.ToString("x", CultureInfo.InvariantCulture); }
		}
		[Browsable(false)]
		public string TypeString
		{
			get
			{
				if (ClassType != null)
				{
					return _type.TypeString;
				}
				return "";
			}
		}
		[Browsable(false)]
		public virtual bool IsValid
		{
			get
			{
				if (_type != null && !string.IsNullOrEmpty(_name))
				{
					return true;
				}
				MathNode.LastValidationError = string.Format(CultureInfo.InvariantCulture, "(_type={2}, _name={3}) for [{0}] of [{1}]. ", this.ToString(), this.GetType().Name,_type,_name);
				return false;
			}
		}
		public bool IsValueType
		{
			get
			{
				if (ClassType != null && BaseClassType != null)
				{
					if (BaseClassType.IsGenericParameter)
					{
						if (_type.ConcreteType == null)
						{
							MethodClass mc = this.Owner as MethodClass;
							if (mc != null)
							{
								DataTypePointer dp = mc.GetConcreteType(BaseClassType);
								if (dp != null)
								{
									_type.SetConcreteType(dp);
								}
							}
						}
						if (_type.ConcreteType != null)
						{
							return _type.ConcreteType.IsValueType;
						}
					}
					else
					{
						return BaseClassType.IsValueType;
					}
				}
				return false;
			}
		}
		protected virtual void OnResolveType()
		{
		}
		public CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			return new CodeVariableReferenceExpression(CodeName);
		}
		public CodeTypeReference GetCodeTypeReference()
		{
			if (BaseClassType == null)
			{
				return new CodeTypeReference(typeof(object));
			}
			if (BaseClassType.IsGenericType)
			{
				return ClassType.GetCodeTypeReference();
			}
			else if (BaseClassType.IsGenericParameter)
			{
				if (_type.ConcreteType == null)
				{
					MethodClass mc = this.Owner as MethodClass;
					if (mc != null)
					{
						DataTypePointer dp = mc.GetConcreteType(BaseClassType);
						if (dp != null)
						{
							_type.SetConcreteType(dp);
						}
					}
				}
				if (_type.ConcreteType != null)
				{
					return _type.ConcreteType.GetCodeTypeReference();
				}
			}
			return new CodeTypeReference(this.TypeString);
		}
		public string GetJavaScriptReferenceCode(StringCollection code)
		{
			return CodeName;
		}
		public string GetPhpScriptReferenceCode(StringCollection code)
		{
			return CodeName;
		}
		public void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			if (ClassType != null && _type.BaseClassType.GetInterface("IJavascriptType") != null)
			{
				IJavascriptType js = Activator.CreateInstance(_type.BaseClassType) as IJavascriptType;
				string v = js.GetJavascriptMethodRef(this.CodeName, methodName, code, parameters);

				if (string.IsNullOrEmpty(returnReceiver))
				{
				}
				else
				{
					code.Add(returnReceiver);
					code.Add("=");
				}
				code.Add(v);
				code.Add(";\r\n");
			}
			else if (this.BaseClassType.IsArray)
			{
				if (string.CompareOrdinal(methodName, "Set") == 0)
				{
					code.Add(CodeName);
					code.Add("[");
					code.Add(parameters[0]);
					code.Add("]=");
					code.Add(parameters[1]);
					code.Add(";\r\n");
				}
				else if (string.CompareOrdinal(methodName, "Get") == 0)
				{
					if (!string.IsNullOrEmpty(returnReceiver))
					{
						code.Add(string.Format(CultureInfo.InvariantCulture,
							"{0}={1}[{2}];\r\n", returnReceiver, this.GetJavaScriptReferenceCode(code), parameters[0]));
					}
				}
			}
			else
			{
				WebPageCompilerUtility.CreateActionJavaScript(this.CodeName, methodName, code, parameters, returnReceiver);
			}
		}
		public void CreateActionPhpScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			if (ClassType != null && _type.BaseClassType.GetInterface("IPhpType") != null)
			{
				IPhpType php = Activator.CreateInstance(_type.BaseClassType) as IPhpType;
				string v = php.GetMethodRef(this.CodeName, methodName, code, parameters);

				if (string.IsNullOrEmpty(returnReceiver))
				{
				}
				else
				{
					code.Add(returnReceiver);
					code.Add("=");
				}
				code.Add(v);
				code.Add(";\r\n");
			}
		}
		#endregion

		#region IObjectIdentity Members

		public bool IsSameObjectRef(IObjectIdentity objectIdentity)
		{
			LocalVariable v = objectIdentity as LocalVariable;
			if (v != null)
				return v.MemberId == this.MemberId;
			return false;
		}
		[Browsable(false)]
		public bool IsSameProperty(IPropertyPointer p)
		{
			LocalVariable v = p as LocalVariable;
			if (v != null)
				return v.MemberId == this.MemberId;
			return false;
		}
		[Browsable(false)]
		public IObjectIdentity IdentityOwner
		{
			get { return Owner; }
		}
		[Browsable(false)]
		public bool IsStatic
		{
			get { return false; }
		}
		[Browsable(false)]
		public EnumObjectDevelopType ObjectDevelopType
		{
			get
			{
				if (ClassType != null)
					return _type.ObjectDevelopType;
				return EnumObjectDevelopType.Library;
			}
		}
		[Browsable(false)]
		public EnumPointerType PointerType
		{
			get { return EnumPointerType.Unknown; }
		}

		#endregion

		#region ICloneable Members

		public virtual object Clone()
		{
			LocalVariable v = (LocalVariable)Activator.CreateInstance(this.GetType(), ClassType, _name, _classId, _memberId);
			v._owner = _owner;
			v._instance = _instance;
			v._nameChecker = _nameChecker;
			v._debug = _debug;
			return v;
		}

		#endregion

		#region ISerializerProcessor Members

		public virtual void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (BaseClassType != null)
			{
				if (BaseClassType.IsGenericParameter || BaseClassType.IsGenericType)
				{
					XmlObjectReader xr = serializer as XmlObjectReader;
					if (xr != null)
					{
						if (xr.ObjectStack.Count > 0)
						{
							DataTypePointer dp = null;
							IEnumerator ie = xr.ObjectStack.GetEnumerator();
							while (ie.MoveNext())
							{
								if (ie.Current != this)
								{
									ActionClass act = ie.Current as ActionClass;
									if (act != null)
									{
										dp = act.GetConcreteType(this.BaseClassType);
										if (dp != null)
										{
											break;
										}
									}
									else
									{
										MethodClass mc = ie.Current as MethodClass;
										if (mc != null)
										{
											dp = mc.GetConcreteType(this.BaseClassType);
											if (dp != null)
											{
												break;
											}
										}
									}
								}
							}
							if (dp != null)
							{
								if (BaseClassType.IsGenericParameter)
								{
									_type.SetConcreteType(dp);
								}
								else
								{
									_type.TypeParameters = dp.TypeParameters;
								}
							}
						}
					}
				}
			}
		}

		#endregion

		#region ICustomObject Members
		[Browsable(false)]
		public ulong WholeId
		{
			get { return DesignUtil.MakeDDWord(MemberId, ClassId); }
		}
		[Browsable(false)]
		public UInt32 DefinitionClassId
		{
			get
			{
				return ClassId;
			}
		}
		[Browsable(false)]
		public uint ClassId
		{
			get
			{
				return _classId;
			}
		}
		[Browsable(false)]
		public uint MemberId
		{
			get
			{
				return _memberId;
			}
		}
		[Description("Variable name")]
		[ParenthesizePropertyName(true)]
		public string Name
		{
			get
			{
				if (!string.IsNullOrEmpty(_name))
				{
					if (_name.IndexOfAny(new char[] { '[', ']' }) >= 0)
					{
						_name = _name.Replace('[', INVCHAR).Replace(']', INVCHAR);
					}
				}
				return _name;
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					if (value.IndexOfAny(new char[] { '[', ']' }) >= 0)
					{
						value = value.Replace('[', INVCHAR).Replace(']', INVCHAR);
					}
					if (_name != value)
					{
						if (_nameChecker != null)
						{
							EventArgNameChange anc = new EventArgNameChange(value, _name);
							_nameChecker(this, anc);
							if (anc.Cancel)
							{
								return;
							}
						}
						_name = value;
					}
				}
			}
		}

		#endregion

		#region IXmlNodeSerializable Members
		protected virtual void OnWrite(IXmlCodeWriter writer, XmlNode node)
		{
		}
		public void OnWriteToXmlNode(IXmlCodeWriter writer, XmlNode node)
		{
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ClassID, ClassId);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ComponentID, MemberId);
			//
			OnWrite(writer, node);
			//
		}
		protected virtual void OnRead(IXmlCodeReader reader, XmlNode node)
		{
			//get local variable from method component list based on _memberId
			XmlNode varNode = DesignUtil.GetMethodVariableNode(node.OwnerDocument.DocumentElement, _memberId);
			if (varNode == null)
			{
				LocalVariable v = LocalVariable.GetUnsavedLocalVariable(_classId, _memberId);
				if (v == null)
				{
					//it can be a variable in an exception handler
					//get method id
					UInt32 methodId = 0;
					XmlNode mn = node;
					while (mn != null)
					{
						methodId = XmlUtil.GetAttributeUInt(mn, XmlTags.XMLATT_ScopeId);
						if (methodId != 0)
						{
							break;
						}
						mn = mn.ParentNode;
					}
					if (methodId != 0)
					{
						ClassPointer root = ((XmlObjectReader)reader).ObjectList.GetTypedData<ClassPointer>();
						MethodClass mc = root.GetCustomMethodById(methodId);
						if (mc != null)
						{
							v = mc.GetLocalVariable(_memberId);
						}
					}
					else
					{
						DesignUtil.WriteToOutputWindowAndLog("Error reading local variable. Variable node not found [{0},{1}]", _classId, _memberId);
					}
				}
				if (v != null)
				{
					_name = v.Name;
					_type = v.ClassType;
					_owner = v._owner;
					_instance = v._instance;
					_nameChecker = v._nameChecker;
					_debug = v._debug;
				}
			}
			else
			{
				_name = XmlUtil.GetNameAttribute(varNode);
				Type t = XmlUtil.GetLibTypeAttribute(varNode);
				if (t != null)
				{
					if (t.Equals(typeof(ParameterClass)))
					{
						MethodClass mc = _owner as MethodClass;
						if (mc != null)
						{
							_type = (DataTypePointer)Activator.CreateInstance(t, mc);
						}
					}
					else
					{
						_type = (DataTypePointer)Activator.CreateInstance(t);
					}
					reader.ReadObjectFromXmlNode(varNode, _type, t, this);
				}
				else
				{
					UInt32 id = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
					if (id != 0)
					{
						//try to get it from exception handler of a method
						ClassPointer root = ClassPointer.CreateClassPointer(((XmlObjectReader)reader).ObjectList);
						LocalVariable v = root.GetLocalVariable(id);
						if (v != null)
						{
							_name = v.Name;
							_type = v.ClassType;
							_owner = v._owner;
							_instance = v._instance;
							_nameChecker = v._nameChecker;
							_debug = v._debug;
						}
					}
					else
					{
#if DEBUG
						throw new DesignerException("Error reading local variable from class [{0}]. XPath:[{1}]", reader.ClassId, XmlUtil.GetPath(node));
#endif
					}
				}
			}
		}
		public void OnReadFromXmlNode(IXmlCodeReader reader, XmlNode node)
		{
			_memberId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
			_classId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
			OnRead(reader, node);
		}

		#endregion

	}
	public class PropertyDescriptorVariableValue : PropertyDescriptor
	{
		private string _category;
		private LocalVariable _data;
		private Type _componentType;
		public event EventHandler ValueChanged;
		public PropertyDescriptorVariableValue(string name, Attribute[] attrs, string category, LocalVariable data, Type componentType)
			: base(name, attrs)
		{
			_category = category;
			_data = data;
			_componentType = componentType;
		}

		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get { return _componentType; }
		}

		public override object GetValue(object component)
		{
			return _data.ObjectInstance;
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override Type PropertyType
		{
			get { return _data.ObjectType; }
		}

		public override void ResetValue(object component)
		{
			_data.ObjectInstance = VPLUtil.GetDefaultValue(_data.ObjectType);
		}

		public override void SetValue(object component, object value)
		{
			_data.ObjectInstance = value;
			if (ValueChanged != null)
			{
				ValueChanged(this, EventArgs.Empty);
			}
		}

		public override bool ShouldSerializeValue(object component)
		{
			return true;
		}
	}

	/// <summary>
	/// dynamically constructed for ActionAssignInstance
	/// </summary>
	public class MethodParamVariable : LocalVariable
	{
		private ParameterClass _param;
		public MethodParamVariable(ParameterClass p, UInt32 classId)
			: base(p, p.Name, classId, p.ParameterID)
		{
			_param = p;
		}
		[Browsable(false)]
		public override string CodeName
		{
			get
			{
				return _param.Name;
			}
		}
	}
}
