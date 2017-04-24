/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using MathExp;
using LimnorDesigner.Action;
using LimnorDesigner.MethodBuilder;
using VSPrj;
using XmlSerializer;
using System.Xml;
using System.Windows.Forms;
using ProgElements;
using System.CodeDom;
using System.Drawing.Design;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LimnorDesigner.Property;
using VPL;
using XmlUtility;
using MathItem;
using LimnorDesigner.Event;
using Limnor.WebBuilder;
using Limnor.WebServerBuilder;
using System.Globalization;
using LimnorDatabase;
using System.Windows.Forms.Design;
using LimnorDesigner.Interface;

namespace LimnorDesigner
{
	/// <summary>
	/// execute a method. 
	/// use ICustomTypeDescriptor to design action parameters.
	/// IAction includes IObjectIdentity, IEventHandler, IBreakPointOwner, ICloneable, 
	/// </summary>
	[UseParentObject]
	public class ActionClass : IAction, ICustomTypeDescriptor, INonHostedObject, ISelectPropertySave, IPostXmlNodeSerialize, IScopeMethodHolder, IVplMethodSelector
	{
		#region fields and constructors
		private UInt32 _actId;
		private ClassPointer _class;
		private string _name;
		private bool _disableParamValidate;
		private string _desc;
		private bool _breakBefore;
		private bool _breakAfter;
		private bool _asLocal;
		private IActionMethodPointer _methodPointer;
		private ParameterValueCollection _parameterValues;
		private object _returnValue;
		private DataTypePointer _returnType;
		private PropertyDescriptor[] _paramNret;
		private string _display;
		private MethodClass _scopeMethod;
		private ExpressionValue _condition;
		private CodeExpression[] _parameterExpressions; //for using in math exp compile
		private IObjectPointer _returnReceiver;
		//
		private XmlObjectReader _reader;
		private XmlObjectWriter _writer;
		private XmlNode _xmlNode;
		private XmlNode _xmlNodeChanged;
		private List<StringCollection> _jsCodeSegments;
		//
		private EnumWebActionType _webActType;
		private bool _reading;
		private string _errmsg;
		public static bool LoadOnce = false;
		public static PropertyDescriptorCollection LastLoadedProps = null;
		//
		public ActionClass(ClassPointer owner)
		{
			_class = owner;
			if (_class == null)
			{
				throw new DesignerException("_class cannot be null for an Action");
			}
		}
		#endregion
		public static bool CheckMethodOwnerType<T>(IAction a)
		{
			if (a != null)
			{
				if (a.ActionMethod.Owner != null)
				{
					if (a.ActionMethod.Owner.ObjectInstance is T)
					{
						return true;
					}
				}
			}
			return false;
		}
		#region properties (serialized)
		[Browsable(false)]
		protected CodeExpression[] ParameterCodeExpressions
		{
			get
			{
				return _parameterExpressions;
			}
		}
		[Browsable(false)]
		public IActionMethodPointer ActionMethod
		{
			get
			{
				if (_methodPointer != null)
				{
					if (_methodPointer.Action == null)
					{
						_methodPointer.Action = this;
					}
				}
				return _methodPointer;
			}
			set
			{
				_methodPointer = value;
				if (_methodPointer != null)
				{
					_methodPointer.Action = this;
					if (_methodPointer.IsValid)
					{
						MethodPointer mp = _methodPointer as MethodPointer;
						if (mp != null)
						{
							mp.ResolveGenericParameters(this.Project);
						}
						if (_parameterValues == null)
						{
							_parameterValues = new ParameterValueCollection();
						}
						_methodPointer.ValidateParameterValues(_parameterValues);
						_parameterValues.SetParameterValueChangeEvent(_methodPointer_ParameterValueChanged);
					}
				}
			}
		}
		[Description("The name to identify this action")]
		[DesignOnly(true)]
		[ParenthesizePropertyName(true)]
		public string ActionName
		{
			get
			{
				if (string.IsNullOrEmpty(_name))
				{
					if (_methodPointer != null)
					{
						_name = _methodPointer.ToString();
					}
				}
				return _name;
			}
			set
			{
				if (_name != value)
				{
					bool cancel = false;
					if (NameChanging != null)
					{
						NameBeforeChangeEventArg nc = new NameBeforeChangeEventArg(_name, value, false);
						NameChanging(this, nc);
						cancel = nc.Cancel;
					}
					if (!cancel)
					{
						_name = value;
						fireValueChange(new PropertyChangeEventArg("ActionName"));
					}
				}
			}
		}
		[Description("Description of this action.")]
		public string Description
		{
			get
			{
				return _desc;
			}
			set
			{
				if (_desc != value)
				{
					_desc = value;
					fireValueChange(new PropertyChangeEventArg("Description"));
				}
			}
		}
		[Browsable(false)]
		public ClassPointer RootClassPointer
		{
			get
			{
				if (IdentityOwner != null)
				{
					IObjectIdentity oi = IdentityOwner;
					ClassPointer root = IdentityOwner as ClassPointer;
					while (root == null && oi != null)
					{
						oi = oi.IdentityOwner;
						root = oi as ClassPointer;
					}
					return root;
				}
				return null;
			}
		}
		[Browsable(false)]
		[Description("A static action only invokes a static method and uses only static members for action arguments")]
		public bool IsStatic
		{
			get
			{
				if (this.ActionMethod != null)
				{
					DataTypePointer dtp = this.ActionMethod.Owner as DataTypePointer;
					if (dtp != null)
					{
						return true;
					}
				}
				if (_methodPointer != null)
				{
					SetterPointer sp = _methodPointer as SetterPointer;
					if (sp != null)
					{
						ClassPointer cp0 = sp.SetProperty.Declarer;
						if (cp0 != null)
						{
							if (cp0.ClassId != ClassId)
							{
								return true;
							}
						}
					}
					ClassInstancePointer cip = _methodPointer.Owner as ClassInstancePointer;
					if (cip != null)
					{
						ClassPointer cp = cip.Owner as ClassPointer;
						if (cp != null)
						{
							if (cp.IsStatic)
							{
								return true;
							}
						}
						if (cip.MemberId == 0)
						{
							if (cip.DefinitionClassId != this.ClassId)
							{
								return true;
							}
						}
					}
					else
					{
						ClassPointer cp = _methodPointer.Owner as ClassPointer;
						if (cp != null)
						{
							if (cp.ClassId != this.ClassId)
							{
								return true;
							}
						}
					}
					return _methodPointer.IsStatic;
				}
				if (_class != null)
				{
					return _class.IsStatic;
				}
				return false;
			}
		}
		[Browsable(false)]
		public bool IsStaticAction
		{
			get
			{
				if (IsStatic)
				{
					int n = ParameterCount;
					if (n == 0)
					{
						return true;
					}
					ParameterValueCollection pvs = ParameterValues;
					for (int i = 0; i < pvs.Count; i++)
					{
						ParameterValue pv = pvs[i];
						if (pv != null)
						{
							if (!pv.IsStatic)
							{
								return false;
							}
						}
					}
					return true;
				}
				return false;
			}
		}
		#endregion
		#region properties
		[NotForProgramming]
		[Browsable(false)]
		public List<StringCollection> JavascriptCodeSegments
		{
			get
			{
				return _jsCodeSegments;
			}
		}
		[Browsable(false)]
		public virtual bool IsValid
		{
			get
			{
				if (ActionMethod != null)
				{
					if (ActionMethod.IsValid)
					{
						if (this.ParameterValues != null && this.ParameterValues.Count > 0)
						{
							for (int i = 0; i < this.ParameterValues.Count; i++)
							{
								if (!this.ParameterValues[i].IsValid)
								{
									return false;
								}
							}
						}
						if (ActionCondition != null)
						{
							return (ActionCondition.IsValid);
						}
						return true;
					}
				}
				else
				{
					MathNode.LastValidationError = string.Format(CultureInfo.InvariantCulture, "ActionMethod is null for [{0}] of [{1}]", this.ToString(), this.GetType().Name);
					_errmsg = MathNode.LastValidationError;
				}
				return false;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public MethodDiagramViewer MethodPane { get; set; }

		[DefaultValue(0)]
		[Browsable(false)]
		public UInt32 SubScopeId { get; set; }

		[Browsable(false)]
		[ReadOnly(true)]
		public UInt32 ScopeMethodId { get; set; }
		[Browsable(false)]
		[ReadOnly(true)]
		public IMethod ScopeMethod
		{
			get
			{
				if (_scopeMethod == null && ScopeMethodId != 0)
				{
					_scopeMethod = _class.GetCustomMethodById(ScopeMethodId);
				}
				return _scopeMethod;
			}
			set
			{
				_scopeMethod = value as MethodClass;
				if (_scopeMethod != null)
				{
					ScopeMethodId = _scopeMethod.MemberId;
					if (_methodPointer != null)
					{
						ParameterValueCollection ps = ParameterValues;
						if (ps != null)
						{
							foreach (ParameterValue p in ps)
							{
								p.ScopeMethod = _scopeMethod;
							}
						}
					}
				}
			}
		}
		//
		[Description("Data type of the result of this action if it returns a value")]
		public DataTypePointer ReturnValueType
		{
			get
			{
				if (_returnType == null)
				{
					CustomMethodPointer cmp = _methodPointer as CustomMethodPointer;
					if (cmp != null)
					{
						return (DataTypePointer)(cmp.MethodReturnType);
					}
					else
					{
						MethodPointer mp = _methodPointer as MethodPointer;
						if (mp != null)
						{
							if (mp.ReturnTypeConcrete != null)
							{
								_returnType = mp.ReturnTypeConcrete;
							}
							else
							{
								_returnType = new DataTypePointer(new TypePointer(mp.ReturnType));
							}
						}
						else
						{
							SetterPointer cs = _methodPointer as SetterPointer;
							if (cs != null)
							{
								_returnType = new DataTypePointer(new TypePointer(typeof(void)));
							}
						}
					}
				}
				return _returnType;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public object ReturnValue
		{
			get
			{
				return _returnValue;
			}
			set
			{
				_returnValue = value;
			}
		}
		/// <summary>
		/// owner of the action.
		/// it may provide private data to the action.
		/// </summary>
		[Browsable(false)]
		public ClassPointer Class
		{
			get
			{
				return _class;
			}
		}
		/// <summary>
		/// the class declaring this action
		/// </summary>
		[ReadOnly(true)]
		[Browsable(false)]
		public UInt32 ClassId
		{
			get
			{
				if (_class == null)
				{
					throw new DesignerException("Class owner not set for action [{0}]", ActionId);
				}
				return _class.ClassId;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public UInt64 WholeId
		{
			get
			{
				return DesignUtil.MakeDDWord(_actId, ClassId);
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public UInt64 WholeActionId
		{
			get
			{
				return WholeId;
			}
		}
		/// <summary>
		/// the class of the method/property declarer
		/// </summary>
		[Browsable(false)]
		public UInt32 ExecuterClassId
		{
			get
			{
				IClass holder = this.getActionExecuter();
				if (holder != null)
				{
					return holder.DefinitionClassId;
				}
				return 0;
			}
		}
		/// <summary>
		/// the member id for the instance among hosting class
		/// [ClassId, ExecuterMemberId] identify the object instance
		/// ExecuterClassId identifies the declaring class of the object instance
		/// </summary>
		[Browsable(false)]
		public UInt32 ExecuterMemberId
		{
			get
			{
				try
				{
					IClass holder = this.getActionExecuter();
					if (holder != null)
					{
						return holder.MemberId;
					}
				}
				catch
				{
				}
				return 0;
			}
		}
		/// <summary>
		/// find the type of the immediate property/method.
		/// Library: The immediate property/method is from library
		/// Custom: The immediate property/method is of customer
		/// </summary>
		[Browsable(false)]
		public EnumObjectDevelopType ObjectDevelopType
		{
			get
			{
				if (ActionMethod != null)
				{
					return DesignUtil.GetBaseObjectDevelopType(this.ActionMethod);
				}
				return EnumObjectDevelopType.Both;
			}
		}
		[Browsable(false)]
		public EnumPointerType PointerType { get { return EnumPointerType.Action; } }
		[ReadOnly(true)]
		[Browsable(false)]
		public IObjectIdentity ReturnPointer
		{
			get
			{
				return _returnReceiver;
			}
			set
			{
				_returnReceiver = (IObjectPointer)value;
			}
		}
		#endregion
		#region methods
		private bool hasWebParameters(bool client)
		{
			ParameterValueCollection ps;
			MethodDataTransfer mdt = this.ActionMethod as MethodDataTransfer;
			if (mdt != null)
			{
				ps = mdt.GetParameters();
			}
			else
			{
				ps = this.ParameterValues;
			}
			int pn = ps.Count;
			if (pn > 0)
			{
				List<ISourceValuePointer> list = new List<ISourceValuePointer>();
				for (int i = 0; i < pn; i++)
				{
					ps[i].GetValueSources(list);
				}
				foreach (ISourceValuePointer p in list)
				{
					IClass po = DesignUtil.GetMemberOwner(p);
					if (po != null)
					{
						if (DesignUtil.IsWebClientObject(po))
						{
							if (client)
							{
								return true;
							}
						}
						else
						{
							if (!client)
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}
		void _methodPointer_ParameterValueChanged(object sender, EventArgs e)
		{
			fireValueChange(new PropertyChangeEventArg("ActionMethod"));
		}
		public void SetParameterExpressions(CodeExpression[] ps)
		{
			_parameterExpressions = ps;
		}
		public void ResetScopeMethod()
		{
			_paramNret = null;
		}
		public void SetScopeMethod(MethodClass method)
		{
			_scopeMethod = method;
			ScopeMethodId = method.MemberId;
		}
		public override string ToString()
		{
			return Name;
		}
		#endregion
		#region ICloneable Members
		public object Clone()
		{
			//do not Clone action. keep a single instance of each action in ClassPointer so that it can be edited from many places
			return this;
		}

		#endregion
		#region ReturnReceiverPropertyDescriptor class definition
		class ReturnReceiverPropertyDescriptor : PropertyDescriptor
		{
			private IAction _owner;
			private EventHandler _onValueChanged;
			public ReturnReceiverPropertyDescriptor(IAction owner, Attribute[] attrs, EventHandler onValueChanged) :
				base("AssignTo", attrs)
			{
				_owner = owner;
				_onValueChanged = onValueChanged;
			}

			public override Type ComponentType
			{
				get { return _owner.GetType(); }
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
					return typeof(IObjectPointer);
				}
			}

			public override bool CanResetValue(object component)
			{
				return true;
			}

			public override object GetValue(object component)
			{
				return _owner.ReturnReceiver;
			}

			public override void ResetValue(object component)
			{
			}

			public override void SetValue(object component, object value)
			{
				_owner.ReturnReceiver = (IObjectPointer)value;
				if (_onValueChanged != null)
				{
					_onValueChanged(_owner, EventArgs.Empty);
				}
			}

			public override bool ShouldSerializeValue(object component)
			{
				return false;
			}
		}
		#endregion
		#region SetterTargetPropertyDescriptor class definition
		class SetterTargetPropertyDescriptor : PropertyDescriptor
		{
			private SetterPointer _owner;
			private EventHandler _onValueChanged;
			public SetterTargetPropertyDescriptor(SetterPointer owner, EventHandler h, Attribute[] attrs) :
				base("Property", attrs)
			{
				_owner = owner;
				_onValueChanged = h;
			}
			public override Type ComponentType
			{
				get { return _owner.GetType(); }
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
					return typeof(IProperty);
				}
			}

			public override bool CanResetValue(object component)
			{
				return true;
			}

			public override object GetValue(object component)
			{
				return _owner.SetProperty;
			}

			public override void ResetValue(object component)
			{
			}

			public override void SetValue(object component, object value)
			{
				_owner.SetProperty = (IProperty)value;
				if (_onValueChanged != null)
				{
					_onValueChanged(_owner, EventArgs.Empty);
				}
			}

			public override bool ShouldSerializeValue(object component)
			{
				return false;
			}
		}
		#endregion
		#region SetterValuePropertyDescriptor class definition
		class SetterValuePropertyDescriptor : PropertyDescriptor
		{
			private IAction _owner;
			public SetterValuePropertyDescriptor(IAction owner, Attribute[] attrs) :
				base(SetterPointer.PropertyValueName, attrs)
			{
				_owner = owner;
			}
			public override Type ComponentType
			{
				get { return _owner.GetType(); }
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
					return typeof(ParameterValue);
				}
			}

			public override bool CanResetValue(object component)
			{
				return true;
			}

			public override object GetValue(object component)
			{
				return _owner.GetParameterValue(SetterPointer.PropertyValueName);
			}

			public override void ResetValue(object component)
			{
			}

			public override void SetValue(object component, object value)
			{
				_owner.SetParameterValue(SetterPointer.PropertyValueName, value);
			}

			public override bool ShouldSerializeValue(object component)
			{
				return false;
			}
		}
		#endregion
		#region DataValuePropertyDescriptor class definition
		class DataValuePropertyDescriptor : PropertyDescriptor
		{
			private IAction _owner;
			private EventHandler _onValueChanged;
			public DataValuePropertyDescriptor(IAction owner, string name, Attribute[] attrs, EventHandler onValueChanged) :
				base(name, attrs)
			{
				_owner = owner;
				_onValueChanged = onValueChanged;
			}

			public override Type ComponentType
			{
				get { return _owner.GetType(); }
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
					return typeof(ParameterValue);
				}
			}

			public override bool CanResetValue(object component)
			{
				return true;
			}

			public override object GetValue(object component)
			{
				return _owner.GetParameterValue(Name);
			}

			public override void ResetValue(object component)
			{
			}

			public override void SetValue(object component, object value)
			{
				_owner.SetParameterValue(Name, value);
				if (_onValueChanged != null)
				{
					_onValueChanged(_owner, EventArgs.Empty);
				}
			}

			public override bool ShouldSerializeValue(object component)
			{
				return false;
			}
		}
		#endregion
		#region ActionMethodPropertyDescriptor class definition
		class ActionMethodPropertyDescriptor : PropertyDescriptor
		{
			private ActionClass _owner;
			public ActionMethodPropertyDescriptor(ActionClass owner, Attribute[] attrs)
				: base("ActionMethod", attrs)
			{
				_owner = owner;
			}

			public override Type ComponentType
			{
				get { return _owner.GetType(); }
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
					return typeof(IMethod);
				}
			}

			public override bool CanResetValue(object component)
			{
				return true;
			}

			public override object GetValue(object component)
			{
				return _owner.ActionMethod;
			}

			public override void ResetValue(object component)
			{
			}

			public override void SetValue(object component, object value)
			{
				_owner.ActionMethod = (IActionMethodPointer)value;
			}

			public override bool ShouldSerializeValue(object component)
			{
				return false;
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
#if DEBUG
			MathNode.Trace("Action.GetProperties {0} - start", this.Name);
#endif
			PropertyDescriptorCollection ps;
			if (!LoadOnce || LastLoadedProps == null)
			{
				ps = TypeDescriptor.GetProperties(this, attributes, true);
#if DEBUG
				MathNode.Trace("Action.GetProperties {0} - loaded defaults", this.Name);
#endif
				if (VPLUtil.GetBrowseableProperties(attributes))
				{
					//remove ReturnValueType from Action properties if no return
					List<PropertyDescriptor> list = new List<PropertyDescriptor>();
					foreach (PropertyDescriptor p in ps)
					{
						if (string.Compare(p.Name, "ReturnValueType", StringComparison.Ordinal) == 0)
						{
							if (_methodPointer == null || _methodPointer.NoReturn)
							{
								continue;
							}
						}
						if (string.Compare(p.Name, "ReturnReceiver", StringComparison.Ordinal) == 0)
						{
							if (_methodPointer == null || _methodPointer.NoReturn)
							{
								continue;
							}
						}
						list.Add(new PropertyDescriptorWrapper(p, this));
					}
					ps = new PropertyDescriptorCollection(list.ToArray());
#if DEBUG
					MathNode.Trace("Action.GetProperties {0} - removed return", this.Name);
#endif
					//add ActionMethod===================================================================
					if (_methodPointer != null)
					{
						if (_methodPointer.CanBeReplacedInEditor)
						{
							int n0;
							Attribute[] attrs0;
							if (attributes == null)
							{
								n0 = 0;
							}
							else
							{
								n0 = attributes.Length;
							}
							attrs0 = new Attribute[n0 + 3];
							if (n0 > 0)
							{
								attributes.CopyTo(attrs0, 0);
							}
							attrs0[n0] = new DescriptionAttribute("Action performer and the method to be executed");
							attrs0[n0 + 1] = new EditorAttribute(typeof(TypeEditorMethodPointer), typeof(UITypeEditor));
							attrs0[n0 + 2] = new TypeConverterAttribute(typeof(TypeConverterNoExpand));
							ActionMethodPropertyDescriptor amp = new ActionMethodPropertyDescriptor(this, attrs0);
							ps.Add(amp);
						}
						else
						{
							MethodDataTransfer mdt = _methodPointer as MethodDataTransfer;
							if (mdt != null)
							{
								PropertyDescriptorDataTransferMethod rdp = new PropertyDescriptorDataTransferMethod(mdt);
								ps.Add(rdp);
							}
							else
							{
								PropertyDescriptorForDisplay rdp = new PropertyDescriptorForDisplay(this.GetType(), "ActionMethod", _methodPointer.ToString(), new Attribute[] { });
								ps.Add(rdp);
							}
						}
					}
#if DEBUG
					MathNode.Trace("Action.GetProperties {0} - added method", this.Name);
#endif
					//========================================================================================
					//add parameters and return receiver =====================================================
					//show action parameters
					if (_methodPointer == null)
					{
						_paramNret = new PropertyDescriptor[0];
					}
					else
					{
						SetterPointer cs = _methodPointer as SetterPointer;
						if (cs != null)
						{
							DescriptionAttribute ea;
							int n;
							Attribute[] attrs;
							if (attributes == null)
								n = 0;
							else
								n = attributes.Length;
							_paramNret = new PropertyDescriptor[2];
#if DEBUG
							MathNode.Trace("Action.GetProperties {0} - preparing setter", this.Name);
#endif
							//
							ea = new DescriptionAttribute("Property to be changed");
							attrs = new Attribute[n + 3];
							if (attributes != null && attributes.Length > 0)
							{
								attributes.CopyTo(attrs, 0);
							}
							attrs[n] = ea;
							attrs[n + 1] = new EditorAttribute(typeof(PropEditorPropertyPointer), typeof(UITypeEditor));
							attrs[n + 2] = new TypeConverterAttribute(typeof(TypeConverterNoExpand));
							//property to set
							_paramNret[0] = new SetterTargetPropertyDescriptor(cs, onVauleChange, attrs);
							//add description attribute
							PropertyInfo pif = cs.GetType().GetProperty("Value");
							object[] objs = pif.GetCustomAttributes(typeof(DescriptionAttribute), false);
							if (objs != null && objs.Length > 0)
							{
								ea = objs[0] as DescriptionAttribute;
							}
							else
							{
								ea = new DescriptionAttribute();
							}
							attrs = new Attribute[n + 2];
							if (attributes != null && attributes.Length > 0)
							{
								attributes.CopyTo(attrs, 0);
							}
							attrs[n] = ea;
							attrs[n + 1] = new EditorAttribute(typeof(SelectorEnumValueType), typeof(UITypeEditor));
							if (_parameterValues == null)
							{
								_parameterValues = new ParameterValueCollection();
							}
#if DEBUG
							MathNode.Trace("Action.GetProperties {0} - created setter value", this.Name);
#endif
							cs.ValidateParameterValues(_parameterValues);
#if DEBUG
							MathNode.Trace("Action.GetProperties {0} - validated setter value", this.Name);
#endif
							_parameterValues.SetParameterValueChangeEvent(onVauleChange);
							_paramNret[1] = new SetterValuePropertyDescriptor(this, attrs);
#if DEBUG
							MathNode.Trace("Action.GetProperties {0} - created value param", this.Name);
#endif
						}
						else
						{
							IActionMethodPointer mp = _methodPointer;
							if (mp == null)
							{
								_paramNret = new PropertyDescriptor[0];
							}
							else
							{
								if (_parameterValues == null)
								{
									_parameterValues = new ParameterValueCollection();
								}
								mp.ValidateParameterValues(_parameterValues);
#if DEBUG
								MathNode.Trace("Action.GetProperties {0} - validated params", this.Name);
#endif
								_parameterValues.SetParameterValueChangeEvent(onVauleChange);
#if DEBUG
								MathNode.Trace("Action.GetProperties {0} - set params change event", this.Name);
#endif
								int nPS = _parameterValues.Count;
								if (!mp.NoReturn)
									nPS++;
								_paramNret = new PropertyDescriptor[nPS];
								if (nPS > 0)
								{
									if (_parameterValues.Count > 0)
									{
										Dictionary<string, string> paramDescs = mp.GetParameterDescriptions();
#if DEBUG
										MathNode.Trace("Action.GetProperties {0} - got paramDesc", this.Name);
#endif
										Dictionary<string, Attribute[]> pas = null;
										ISourceValueEnumProvider svep = null;
										if (_methodPointer.Owner != null)
										{
											svep = _methodPointer.Owner.ObjectInstance as ISourceValueEnumProvider;
											IMethodParameterAttributesProvider mpp = _methodPointer.Owner.ObjectInstance as IMethodParameterAttributesProvider;
											if (mpp != null)
											{
#if DEBUG
												MathNode.Trace("Action.GetProperties {0} - getting param attributes", this.Name);
#endif
												pas = mpp.GetParameterAttributes(_methodPointer.MethodName);
											}
										}
#if DEBUG
										MathNode.Trace("Action.GetProperties {0} - preparing {1} params", this.Name, _parameterValues.Count);
#endif
										for (int i = 0; i < _parameterValues.Count; i++)
										{
											if (_parameterValues[i] != null)
											{
												object[] enumValues = null;
												if (svep != null)
												{
													enumValues = svep.GetValueEnum(_methodPointer.MethodName, _parameterValues[i].Name);
												}
												_parameterValues[i].SetValueEnum("", enumValues);
												Attribute[] attrs;
												int n = 0;
												if (attributes != null)
												{
													n = attributes.Length;
												}
												attrs = new Attribute[n + 2];
												DescriptionAttribute ea;
												if (paramDescs != null && paramDescs.ContainsKey(_parameterValues[i].Name))
												{
													//add description attribute
													ea = new DescriptionAttribute(paramDescs[_parameterValues[i].Name]);
												}
												else
												{
													ea = new DescriptionAttribute(_parameterValues[i].Name);
												}
												if (n > 0)
												{
													attributes.CopyTo(attrs, 0);
												}
												attrs[n] = ea;
												attrs[n + 1] = new EditorAttribute(typeof(SelectorEnumValueType), typeof(UITypeEditor));
												Attribute[] pattrs = null;
												if (pas != null)
												{
													if (!pas.TryGetValue(_parameterValues[i].Name, out pattrs))
													{
														pattrs = null;
													}
												}
												else
												{
													if (this.ActionMethod != null && this.ActionMethod.Owner != null)
													{
														IWebClientComponent wcc = this.ActionMethod.Owner.ObjectInstance as IWebClientComponent;
														if (wcc != null)
														{
															WebPageCompilerUtility.TryGetMethodParameterAttributes(wcc.GetType(), this.ActionMethod.MethodName, _parameterValues[i].Name, out pattrs);
														}
														else
														{
															if (this.ActionMethod.Owner.ObjectType != null && this.ActionMethod.Owner.ObjectType.GetInterface("IWebClientComponent") != null)
															{
																WebPageCompilerUtility.TryGetMethodParameterAttributes(this.ActionMethod.Owner.ObjectType, this.ActionMethod.MethodName, _parameterValues[i].Name, out pattrs);
															}
															else if (this.ActionMethod.Owner.ObjectInstance is EasyDataSet)
															{
																WebPageCompilerUtility.TryGetMethodParameterAttributes(typeof(EasyDataSet), this.ActionMethod.MethodName, _parameterValues[i].Name, out pattrs);
															}
														}
													}
												}
												if (pattrs != null && pattrs.Length > 0)
												{
													_parameterValues[i].MergeValueAttributes(pattrs);
													List<Attribute> al = new List<Attribute>();
													al.AddRange(attrs);
													for (int k = 0; k < pattrs.Length; k++)
													{
														bool found = false;
														Type pat = pattrs[k].GetType();
														for (int q = 0; q < al.Count; q++)
														{
															if (pat.Equals(al[q].GetType()))
															{
																al[q] = pattrs[k];
																found = true;
																break;
															}
														}
														if (!found)
														{
															al.Add(pattrs[k]);
														}
													}
													attrs = new Attribute[al.Count];
													al.CopyTo(attrs);
												}
												_paramNret[i] = new DataValuePropertyDescriptor(this, _parameterValues[i].Name, attrs, onVauleChange);
											}
										}
									}
									if (!mp.NoReturn)
									{
#if DEBUG
										MathNode.Trace("Action.GetProperties {0} - adding return", this.Name);
#endif
										Attribute[] attrs2 = new Attribute[3];
										attrs2[0] = new DescriptionAttribute("Property or variable to receive the action result");
										if (ScopeMethod != null && _scopeMethod.CurrentEditor != null)
										{
											attrs2[1] = new EditorAttribute(typeof(AssignToSelector), typeof(UITypeEditor));
										}
										else
										{
											attrs2[1] = new EditorAttribute(typeof(LValueSelector), typeof(UITypeEditor));
										}
										attrs2[2] = new TypeConverterAttribute(typeof(TypeConverterNoExpand));
										_paramNret[nPS - 1] = new ReturnReceiverPropertyDescriptor(this, attrs2, onVauleChange);
									}
								}
							}
						}
					}
#if DEBUG
					MathNode.Trace("Action.GetProperties {0} - prepared params", this.Name);
#endif
					if (_paramNret.Length > 0)
					{
						for (int i = 0; i < _paramNret.Length; i++)
						{
							ps.Add(_paramNret[i]);
						}
					}
#if DEBUG
					MathNode.Trace("Action.GetProperties {0} - added params", this.Name);
#endif
				}
				else
				{
					SubMethodInfoPointer smi = ActionMethod as SubMethodInfoPointer;
					if (smi != null && !smi.SaveParameterValues)
					{
						List<PropertyDescriptor> list = new List<PropertyDescriptor>();
						foreach (PropertyDescriptor p in ps)
						{
							if (string.Compare(p.Name, "ParameterValues", StringComparison.Ordinal) == 0)
							{
								continue;
							}
							list.Add(new PropertyDescriptorWrapper(p, this));
						}
						ps = new PropertyDescriptorCollection(list.ToArray());
					}
				}
				IList<DataTypePointer> genericParameters = GetGenericTypes();
				if (genericParameters != null && genericParameters.Count > 0)
				{
					PropertyDescriptor[] psx = new PropertyDescriptor[ps.Count + genericParameters.Count];
					ps.CopyTo(psx, 0);
					for (int i = 0; i < genericParameters.Count; i++)
					{
						psx[ps.Count + i] = new VPL.PropertyDescriptorForDisplay(this.GetType(), genericParameters[i].DisplayName, genericParameters[i].ConcreteType.Name, new Attribute[] { new ParenthesizePropertyNameAttribute(true) });
					}
					ps = new PropertyDescriptorCollection(psx);
				}
				if (LoadOnce)
				{
					LastLoadedProps = ps;
				}
				return ps;
			}
			else
			{
				ps = LastLoadedProps;
			}
#if DEBUG
			MathNode.Trace("Action.GetProperties {0} - end", this.Name);
#endif
			return ps;
		}
		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion
		#region IEventHandler Members
		[Browsable(false)]
		public UInt32 ActionId
		{
			get { return _actId; }
			set { _actId = value; }
		}

		#endregion
		#region IAction Members
		public void OnAfterDeserialize(IActionsHolder actionsHolder)
		{
			_reading = false;
		}
		private EnumWebRunAt _scopeRunat = EnumWebRunAt.Unknown;
		[Browsable(false)]
		public EnumWebRunAt ScopeRunAt
		{
			get
			{
				if (_scopeMethod != null)
				{
					return _scopeMethod.RunAt;
				}
				return _scopeRunat;
			}
			set
			{
				_scopeRunat = value;
			}
		}
		/// <summary>
		/// An end-user programming system allows end-user to arrange actions-events relations. 
		/// By default all global(public) actions can be used by the end-users.
		/// Set this property to True to hide the action from the end-user
		/// </summary>
		[DefaultValue(false)]
		[Description("Gets and sets a Boolean indicating whether the action is hidden from the end-users. An end-user programming system allows end-users to arrange actions-events relations. By default all global(public) actions can be used by the end-users. Set this property to True to hide the action from the end-user.")]
		public bool HideFromRuntimeDesigners { get; set; }

		[Browsable(false)]
		public ClassPointer ExecuterRootHost
		{
			get
			{
				if (_methodPointer != null)
				{
					IObjectPointer p = _methodPointer.Owner;
					while (p != null)
					{
						IClassContainer ic = p as IClassContainer;
						if (ic != null)
						{
							return ic.RootHost;
						}
						p = p.Owner;
					}
				}
				return _class;
			}
		}
		[Browsable(false)]
		public IObjectPointer ReturnReceiver
		{
			get
			{
				return _returnReceiver;
			}
			set
			{
				_returnReceiver = value;
			}
		}
		/// <summary>
		/// indicates whether it is modified
		/// </summary>
		[Browsable(false)]
		[ReadOnly(true)]
		public bool Changed { get; set; }


		[Browsable(false)]
		public virtual bool IsMethodReturn
		{
			get
			{
				if (ActionMethod != null)
				{
					return ActionMethod.IsMethodReturn;
				}
				return false;
			}
		}
		[Browsable(false)]
		public bool IsPublic
		{
			get
			{
				if (ActionMethod != null)
				{
					if (!ActionMethod.IsValid)
					{
						return true;
					}
				}
				return (!IsLocal && !AsLocal);
			}
		}
		/// <summary>
		/// the action uses method/event parameters and is exclusively used for one method/event only
		/// </summary>
		[Browsable(false)]
		public bool AsLocal
		{
			get
			{
				return _asLocal;
			}
			set
			{
				_asLocal = value;
			}
		}
		/// <summary>
		/// the executer is a local variable
		/// </summary>
		[Browsable(false)]
		public virtual bool IsLocal
		{
			get
			{
				if (ScopeMethodId != 0)
					return true;
				if (_methodPointer != null)
				{
					return _methodPointer.IsForLocalAction;
				}
				return false;
			}
		}
		[Browsable(false)]
		public string Display
		{
			get
			{
				if (_returnReceiver != null && !(_returnReceiver is NullObjectPointer))
				{
					IObjectPointer p = _returnReceiver as IObjectPointer;
					if (p != null)
					{
						_display = p.DisplayName + "=" + ActionName;
					}
					else
					{
						_display = _returnReceiver.ToString() + "=" + ActionName;
					}
					return _display;
				}
				return this.ActionName;
			}
		}
		[Browsable(false)]
		public int ParameterCount
		{
			get
			{
				if (_parameterValues == null)
					return 0;
				return _parameterValues.Count;
			}
		}
		[Browsable(false)]
		public ParameterValueCollection ParameterValues
		{
			get
			{
				if (_parameterValues == null)
				{
					_parameterValues = new ParameterValueCollection();
				}
				if (!_disableParamValidate)
				{
					if (_methodPointer != null && _methodPointer.IsValid)
					{
						_methodPointer.ValidateParameterValues(_parameterValues);
					}
				}
				foreach (ParameterValue p in _parameterValues)
				{
					p.ScopeMethod = ScopeMethod;
				}
				return _parameterValues;
			}
			set
			{
				_parameterValues = value;
				if (_methodPointer != null)
				{
					_methodPointer.ValidateParameterValues(_parameterValues);
				}
			}
		}
		[Browsable(false)]
		public IObjectIdentity IdentityOwner
		{
			get
			{
				return _class;
			}
		}
		public bool IsSameObjectRef(IObjectIdentity objectIdentity)
		{
			ActionClass ac = objectIdentity as ActionClass;
			if (ac != null)
			{
				return (ac.ClassId == this.ClassId && ac.ActionId == this.ActionId);
			}
			return false;
		}

		[DefaultValue(false)]
		[Description("This property is used only for visual debugging. If it is True then before executing this action the debugger pauses and shows a break pointer at this action.")]
		public bool BreakBeforeExecute
		{
			get
			{
				return _breakBefore;
			}
			set
			{
				if (_breakBefore != value)
				{
					_breakBefore = value;
					fireValueChange(new PropertyChangeEventArg("BreakBeforeExecute"));
				}
			}
		}

		[DefaultValue(false)]
		[Description("This property is used only for visual debugging. If it is True then after executing this action the debugger pauses and shows a break pointer at this action.")]
		public bool BreakAfterExecute
		{
			get
			{
				return _breakAfter;
			}
			set
			{
				if (_breakAfter != value)
				{
					_breakAfter = value;
					fireValueChange(new PropertyChangeEventArg("BreakAfterExecute"));
				}
			}
		}

		/// <summary>
		/// compile into if(expression){...} when it is not null
		/// </summary>
		[Editor(typeof(TypeEditorExpressionValue), typeof(UITypeEditor))]
		[Description("Condition for executing this action")]
		public ExpressionValue ActionCondition
		{
			get
			{
				if (_condition == null)
				{
					_condition = new ExpressionValue();
				}
				_condition.ScopeMethod = ScopeMethod;
				return _condition;
			}
			set
			{
				if (value != null)
				{
					_condition = value;
					if (ScopeMethod != null)
					{
						_condition.ScopeMethod = ScopeMethod;
					}
				}
			}
		}
		/// <summary>
		/// XmlNode that represents action changes within a method.
		/// The changes may not have saved to the XmlDocument tree.
		/// </summary>
		[Browsable(false)]
		public XmlNode CurrentXmlData
		{
			get
			{
				if (_xmlNodeChanged != null)
				{
					return _xmlNodeChanged;
				}
				return _xmlNode;
			}
		}
		[Browsable(false)]
		public bool HasChangedXmlData
		{
			get
			{
				return (_xmlNodeChanged != null);
			}
		}
		[Browsable(false)]
		public void ResetDisplay()
		{
			_display = null;
		}
		public DataTypePointer GetConcreteType(Type type)
		{
			if (_returnType != null)
			{
				DataTypePointer dp = _returnType.GetConcreteType(type);
				if (dp != null)
				{
					return dp;
				}
			}
			IGenericTypePointer igp = _returnReceiver as IGenericTypePointer;
			if (igp != null)
			{
				DataTypePointer dp = igp.GetConcreteType(type);
				if (dp != null)
				{
					return dp;
				}
			}
			if (_parameterValues != null)
			{
				foreach (ParameterValue pv in _parameterValues)
				{
					DataTypePointer dp = pv.GetConcreteType(type);
					if (dp != null)
					{
						return dp;
					}
				}
			}
			MethodPointer mp = this.ActionMethod as MethodPointer;
			if (mp != null)
			{
				DataTypePointer dp = mp.GetConcreteType(type);
				if (dp != null)
				{
					return dp;
				}
			}
			return null;
		}
		public IList<DataTypePointer> GetGenericTypes()
		{
			List<DataTypePointer> l = new List<DataTypePointer>();
			if (_returnType != null)
			{
				IList<DataTypePointer> lst = _returnType.GetGenericTypes();
				if (lst != null && lst.Count > 0)
				{
					l.AddRange(lst);
				}
			}
			IGenericTypePointer igp = _returnReceiver as IGenericTypePointer;
			if (igp != null)
			{
				IList<DataTypePointer> lst = igp.GetGenericTypes();
				if (lst != null && lst.Count > 0)
				{
					foreach (DataTypePointer d0 in lst)
					{
						bool found = false;
						foreach (DataTypePointer d1 in l)
						{
							if (string.CompareOrdinal(d0.DisplayName, d1.DisplayName) == 0)
							{
								found = true;
								break;
							}
						}
						if (!found)
						{
							l.Add(d0);
						}
					}
				}
			}
			if (_parameterValues != null)
			{
				foreach (ParameterValue pv in _parameterValues)
				{
					if (pv == null) continue;
					IList<DataTypePointer> lst = pv.GetGenericTypes();
					if (lst != null && lst.Count > 0)
					{
						foreach (DataTypePointer d0 in lst)
						{
							bool found = false;
							foreach (DataTypePointer d1 in l)
							{
								if (string.CompareOrdinal(d0.DisplayName, d1.DisplayName) == 0)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								l.Add(d0);
							}
						}
					}
				}
			}
			MethodPointer mp = this.ActionMethod as MethodPointer;
			if (mp != null)
			{
				IList<DataTypePointer> lst = mp.GetGenericTypes();
				if (lst != null && lst.Count > 0)
				{
					foreach (DataTypePointer d0 in lst)
					{
						bool found = false;
						foreach (DataTypePointer d1 in l)
						{
							if (string.CompareOrdinal(d0.DisplayName, d1.DisplayName) == 0)
							{
								found = true;
								break;
							}
						}
						if (!found)
						{
							l.Add(d0);
						}
					}
				}
			}
			return l;
		}
		[Browsable(false)]
		public IAction CreateNewCopy()
		{
			ActionClass a = new ActionClass(_class);
			a._actId = (UInt32)(Guid.NewGuid().GetHashCode());
			a._asLocal = _asLocal;
			a._desc = _desc;
			if (_methodPointer != null)
			{
				DesignUtil.LogIdeProfile("clone method pointer");
				a._methodPointer = (IActionMethodPointer)_methodPointer.Clone();
			}
			if (string.IsNullOrEmpty(_name))
			{
				a._name = "Action" + Guid.NewGuid().GetHashCode().ToString("x");
			}
			else
			{
				if (_name.Length > 30)
				{
					a._name = _name.Substring(0, 30) + Guid.NewGuid().GetHashCode().ToString("x");
				}
				else
				{
					a._name = _name + "_" + Guid.NewGuid().GetHashCode().ToString("x");
				}
			}
			a.ScopeMethodId = ScopeMethodId;
			a.SubScopeId = SubScopeId;
			a.ActionHolder = ActionHolder;
			if (_returnType != null)
			{
				DesignUtil.LogIdeProfile("clone return type");
				a._returnType = (DataTypePointer)_returnType.Clone();
			}
			a._scopeMethod = _scopeMethod;
			if (_returnReceiver != null)
			{
				DesignUtil.LogIdeProfile("clone return pointer");
				a._returnReceiver = (IObjectPointer)_returnReceiver.Clone();
			}
			if (_parameterValues != null)
			{
				DesignUtil.LogIdeProfile("clone parameters");
				a._parameterValues = (ParameterValueCollection)_parameterValues.Clone(a);
			}
			if (_condition != null)
			{
				DesignUtil.LogIdeProfile("clone conditions");
				a._condition = (ExpressionValue)_condition.Clone();
			}
			DesignUtil.LogIdeProfile("finish create action copy");
			return a;
		}
		public void EstablishObjectOwnership(IActionsHolder scope)
		{
		}
		public bool IsSameMethod(IAction act)
		{
			return this.ActionMethod.IsSameMethod(act.ActionMethod);
		}
		public void ValidateParameterValues()
		{
			if (_parameterValues == null)
			{
				_parameterValues = new ParameterValueCollection();
			}
			if (_methodPointer != null && _methodPointer.IsValid)
			{
				_methodPointer.ValidateParameterValues(_parameterValues);
			}
			foreach (ParameterValue p in _parameterValues)
			{
				p.ScopeMethod = ScopeMethod;
			}
		}
		public bool Edit(XmlObjectWriter writer, IMethod context, Form caller, bool newAction)
		{
			try
			{
				FormProgress.ShowProgress("Loading action parameters...");
				LimnorProject project = _class.Project;
				FormActionParameters dlgData = new FormActionParameters();
				if (_parameterValues == null)
				{
					_parameterValues = new ParameterValueCollection();
				}
				ActionMethod.ValidateParameterValues(_parameterValues);
				dlgData.SetScopeMethod(context);
				dlgData.LoadAction(this, _class.XmlData);
				//
				DialogResult ret = dlgData.ShowDialog(caller);
				if (ret == DialogResult.OK)
				{
					_class.SaveAction(this, writer);
					ILimnorDesignPane pane = project.GetTypedData<ILimnorDesignPane>(_class.ClassId);
					if (pane != null)
					{
						pane.OnActionChanged(_class.ClassId, this, newAction);
						pane.OnNotifyChanges();
					}
					else
					{
						DesignUtil.WriteToOutputWindowAndLog("Error editong ActionClass. ClassPointer [{0}] is not in design mode when creating an action. Please close the design pane and re-open it.", _class.ClassId);
					}
					return true;
				}
			}
			catch (Exception err)
			{
				MessageBox.Show(caller, err.Message, "Action", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				FormProgress.HideProgress();
			}
			return false;
		}
		/// <summary>
		/// Actions do not use return values. Only math expressions use return values.
		/// If an action is the last action of a method then its return value is the return value of the method. But this logic is not implemented here. It is implemented in the higher level.
		/// </summary>
		/// <param name="compiler"></param>
		/// <param name="method"></param>
		/// <param name="statements"></param>
		public void ExportCode(ActionBranch currentAction, ActionBranch nextAction, ILimnorCodeCompiler compiler, IMethodCompile methodToCompile, CodeMemberMethod method, CodeStatementCollection statements, bool debug)
		{
			if (!IsValid)
			{
				if (VPLUtil.ErrorLogger != null)
				{
					string sn = "unknown";
					if (this.RootClassPointer != null)
					{
						sn = this.RootClassPointer.Name;
					}
					VPLUtil.ErrorLogger.LogError(".Net Compiler: Invalid action [{0},{1}] in [{2}].Fix its parameters or remove this action and recreate it.", ActionId, ActionName, sn);
				}
				return;
			}
			//=========================================================================
			if (!string.IsNullOrEmpty(Description))
			{
				statements.Add(new CodeCommentStatement(Description));
			}
			if (ActionMethod == null)
			{
				if (VPLUtil.ErrorLogger != null)
				{
					VPLUtil.ErrorLogger.LogError("ActionMethod is null for action [{0},{1}]", ActionId, ActionName);
				}
				else
				{
					throw new DesignerException("ActionMethod is null for action [{0},{1}]", ActionId, ActionName);
				}
			}
			else
			{
				IActionCompiler ac = ActionMethod as IActionCompiler;
				if (ac != null)
				{
					try
					{
						ICustomPointer icp = ac as ICustomPointer;
						CodeExpression ceCondition = null;
						if (_condition != null)
						{
							ceCondition = _condition.ExportCode(methodToCompile);
							if (ceCondition != null)
							{
								ceCondition = CompilerUtil.ConvertToBool(_condition.DataType, ceCondition);
							}
						}
						CodeExpressionCollection ps;
						int nOptionStart = -1;
						Type optionalType = null;
						ParameterInfo[] pifs = null;
						MethodInfoPointer mip = ActionMethod as MethodInfoPointer;
						if (mip != null)
						{
							if (mip.MethodInfo != null)
							{
								pifs = mip.MethodInfo.GetParameters();
								if (pifs != null && pifs.Length > 0)
								{
									if (pifs[pifs.Length - 1].ParameterType.IsArray)
									{
										nOptionStart = pifs.Length - 1;
										optionalType = pifs[pifs.Length - 1].ParameterType.GetElementType();
									}
								}
							}
						}
						int npCount = ParameterCount;
						if (string.CompareOrdinal("ShowDialog", ActionMethod.MethodName) == 0)
						{
							if (ParameterCount == 1)
							{
								if (this.Project.ProjectType == EnumProjectType.Kiosk)
								{
									npCount = 0;
								}
							}
						}
						_parameterExpressions = new CodeExpression[npCount];
						for (int k = 0; k < npCount; k++)
						{
							ParameterValue v = _parameterValues[k];
							if (v == null)
							{
								_parameterExpressions[k] = ObjectCreationCodeGen.GetDefaultValueExpression(_parameterValues[k].ObjectType);
							}
							else
							{
								DataTypePointer tg = v.DataType;
								bool isArray = false;
								if (tg != null)
								{
									isArray = tg.IsArray;
								}
								if (nOptionStart >= 0 && k >= nOptionStart && !isArray)
								{
									v.SetTargetType(optionalType);
								}
								else
								{
									if (tg != null)
									{
										if (tg.IsGenericParameter)
										{
											if (tg.ConcreteType == null)
											{
												DataTypePointer pk = this.ActionMethod.GetParameterTypeByIndex(k) as DataTypePointer;
												if (pk != null)
												{
													if (pk.IsGenericParameter && pk.ConcreteType == null)
													{
														FireEventMethod fem = this.ActionMethod as FireEventMethod;
														if (fem != null && fem.Event != null && fem.Event.Event != null && fem.Event.Event.EventHandlerType != null)
														{
															DataTypePointer cdp = fem.Event.Event.EventHandlerType.GetConcreteType(pk.BaseClassType);
															if (cdp != null)
															{
																pk.SetConcreteType(cdp);
															}
														}
														tg.SetConcreteType(pk.ConcreteType);
													}
												}
											}
										}
									}
								}
								_parameterExpressions[k] = v.GetReferenceCode(methodToCompile, statements, true);
							}
						}
						if (pifs != null && pifs.Length == _parameterExpressions.Length)
						{
							for (int k = 0; k < _parameterExpressions.Length; k++)
							{
								if (!(_parameterExpressions[k] is CodeDirectionExpression))
								{
									if (pifs[k].ParameterType.IsByRef)
									{
										if (pifs[k].IsOut)
										{
											_parameterExpressions[k] = new CodeDirectionExpression(FieldDirection.Out, _parameterExpressions[k]);
										}
										else if (pifs[k].ParameterType.IsByRef)
										{
											_parameterExpressions[k] = new CodeDirectionExpression(FieldDirection.Ref, _parameterExpressions[k]);
										}
									}
								}
							}
						}
						ps = new CodeExpressionCollection(_parameterExpressions);
						if (ceCondition == null)
						{
							ac.Compile(currentAction, nextAction, compiler, methodToCompile, method, statements, ps, _returnReceiver, debug);
						}
						else
						{
							CodeConditionStatement cs = new CodeConditionStatement();
							cs.Condition = ceCondition;
							statements.Add(cs);
							ac.Compile(currentAction, nextAction, compiler, methodToCompile, method, cs.TrueStatements, ps, _returnReceiver, debug);
						}
					}
					catch (Exception err)
					{
						if (VPLUtil.ErrorLogger != null)
						{
							VPLUtil.ErrorLogger.LogError("1. Error compiling action [{0}, {1}]. ActionMethod {2}. Error:{3}", ActionId, ActionName, ActionMethod.GetType().FullName, DesignerException.FormExceptionText(err));
						}
						else
						{
							throw new DesignerException("1. Error compiling action [{0}, {1}]. ActionMethod {2}. Error:{3}", ActionId, ActionName, ActionMethod.GetType().FullName, DesignerException.FormExceptionText(err));
						}
					}
				}
				else
				{
					if (VPLUtil.ErrorLogger != null)
					{
						VPLUtil.ErrorLogger.LogError("ActionMethod {0} does not inherits IActionCompiler", ActionMethod.GetType().FullName);
					}
					else
					{
						throw new DesignerException("ActionMethod {0} does not inherits IActionCompiler", ActionMethod.GetType().FullName);
					}
				}
			}
		}
		public void ExportJavaScriptCode(ActionBranch currentAction, ActionBranch nextAction, StringCollection jsCode, StringCollection methodToCompile, JsMethodCompiler data)
		{
			CreateJavaScript(methodToCompile, data.FormSubmissions, nextAction == null ? null : nextAction.InputName, Indentation.GetIndent());
		}
		public void ExportPhpScriptCode(ActionBranch currentAction, ActionBranch nextAction, StringCollection jsCode, StringCollection methodToCompile, JsMethodCompiler data)
		{
			CreatePhpScript(methodToCompile, nextAction == null ? null : nextAction.InputName, Indentation.GetIndent());
		}
		public void ExportClientServerCode(ActionBranch currentAction, ActionBranch nextAction, ILimnorCodeCompiler compiler, IMethodCompile methodToCompile, CodeMemberMethod method, CodeStatementCollection statements, bool debug,
			StringCollection jsCode, StringCollection methodCode, JsMethodCompiler data)
		{
		}
		public void CreateJavaScript(StringCollection sb, Dictionary<string, StringCollection> formsumissions, string nextActionInput, string indent)
		{
			_jsCodeSegments = null;
			MathNode.LastValidationError = null;
			if (!IsValid)
			{
				if (VPLUtil.ErrorLogger != null)
				{
					string sn = "unknown";
					if (this.RootClassPointer != null)
					{
						sn = this.RootClassPointer.Name;
					}
					VPLUtil.ErrorLogger.LogError("JavaScript compiler: Invalid action [{0},{1}] in [{2}]. Message:{3}. Fix its parameters or remove this action and recreate it.", ActionId, ActionName, sn, MathNode.LastValidationError);
				}
				return;
			}
			if (!string.IsNullOrEmpty(Description))
			{
				sb.Add(indent);
				sb.Add("/*\r\n");
				sb.Add(Description);
				sb.Add("\r\n");
				sb.Add(indent);
				sb.Add("*/\r\n");
			}

			if (ActionMethod == null)
			{
				if (VPLUtil.ErrorLogger != null)
				{
					VPLUtil.ErrorLogger.LogError("ActionMethod is null for action [{0},{1}]", ActionId, ActionName);
				}
				else
				{
					throw new DesignerException("ActionMethod is null for action [{0},{1}]", ActionId, ActionName);
				}
			}
			else
			{
				IActionCompiler ac = ActionMethod as IActionCompiler;
				if (ac != null)
				{
					int idtOrigin = Indentation.GetIndentation();
					if (!string.IsNullOrEmpty(indent))
					{
						Indentation.SetIndentation(indent.Length);
					}
					try
					{
						IList<ISourceValuePointer> cps = this.GetServerProperties(0);
						if (cps != null && cps.Count > 0)
						{
							if (_scopeMethod != null)
							{
								_scopeMethod.AddDownloads(cps);
							}
						}
						string ceCondition = null;
						if (_condition != null)
						{
							ceCondition = _condition.CreateJavaScriptCode(sb);
							if (!string.IsNullOrEmpty(ceCondition))
							{
								ceCondition = string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.isValueTrue({0})", ceCondition);
							}
						}
						StringCollection ps = new StringCollection();
						bool isJsCode = false;
						if (this.ActionMethod != null && this.ActionMethod.Owner != null)
						{
							if (typeof(JavascriptFiles).IsAssignableFrom(this.ActionMethod.Owner.ObjectType))
							{
								if (string.CompareOrdinal(this.ActionMethod.MethodName, "Execute") == 0)
								{
									if (ParameterCount == 1)
									{
										ParameterValue v = _parameterValues[0];
										if (v.ValueType == EnumValueType.ConstantValue)
										{
											isJsCode = true;
											if (v.ConstantValue != null && v.ConstantValue.Value != null)
											{
												ps.Add(v.ConstantValue.Value.ToString());
											}
										}
									}
								}
							}
						}
						if (VPLUtil.CompilerContext_JS)
						{
							SetterPointer sp = this.ActionMethod as SetterPointer;
							if (sp != null)
							{
								if (ParameterCount == 1)
								{
									ParameterValue v = _parameterValues[0];
									if (v != null)
									{
										if (v.ValueType == EnumValueType.Property)
										{
											PropertyPointer pp = v.Property as PropertyPointer;
											if (pp != null && pp.Owner != null)
											{
												PropertyPointer pp2 = pp.Owner as PropertyPointer;
												if (pp2 != null)
												{
													if (string.CompareOrdinal(pp2.Name, "BindingFields") == 0)
													{
														if (pp.Owner.Owner != null)
														{
															EasyDataSet eds = pp.Owner.Owner.ObjectInstance as EasyDataSet;
															if (eds != null)
															{
																string c = eds.TableName;
																if (!string.IsNullOrEmpty(c))
																{
																	string pn = sp.SetProperty.Name;
																	IDataBindingNameMapHolder dbnp = sp.SetProperty.Owner.ObjectInstance as IDataBindingNameMapHolder;
																	if (dbnp != null)
																	{
																		Dictionary<string, string> dic = dbnp.DataBindNameMap;
																		if (dic != null)
																		{
																			string pn2;
																			if (dic.TryGetValue(pn, out pn2))
																			{
																				pn = pn2;
																			}
																		}
																	}
																	if (!string.IsNullOrEmpty(ceCondition))
																	{
																		sb.Add(indent);
																		sb.Add("if(");
																		sb.Add(ceCondition);
																		sb.Add(") {\r\n");
																	}
																	string ocode = sp.SetProperty.Owner.GetJavaScriptReferenceCode(sb);
																	sb.Add(string.Format(CultureInfo.InvariantCulture,
																		"{0}.setAttribute('jsdb','{1}:{2}:{3}');\r\n", ocode, eds.TableName, pp.Name, pn));
																	sb.Add(string.Format(CultureInfo.InvariantCulture,
																		"JsonDataBinding.refreshDataBind({0},'{1}');\r\n", ocode, eds.TableName));
																	if (!string.IsNullOrEmpty(ceCondition))
																	{
																		sb.Add("\r\n}\r\n");
																	}
																}
															}
														}
														return;
													}
												}
											}
										}
									}
								}
							}
						}
						if (!isJsCode)
						{
							UInt32 pageId = 0;
							for (int k = 0; k < ParameterCount; k++)
							{
								ParameterValue v = _parameterValues[k];
								if (v == null)
								{
									ps.Add("null");
								}
								else
								{
									if (v.ValueType == EnumValueType.ConstantValue)
									{
										ComponentID cid = v.GetConstValue() as ComponentID;
										if (cid != null)
										{
											if (typeof(WebPage).IsAssignableFrom(cid.ComponentType))
											{
												pageId = cid.ComponentId;
											}
										}
									}
									if (k == ParameterCount - 1)
									{
										ConstObjectPointer o = v.ObjectInstance as ConstObjectPointer;
										if (o != null && o.ArrayValues != null && o.ArrayValues.Length > 0)
										{
											for (int m = 0; m < o.ArrayValues.Length; m++)
											{
												ps.Add(o.ArrayValues[m].GetJavaScriptReferenceCode(sb));
											}
										}
										else
										{
											ps.Add(v.GetJavaScriptReferenceCode(sb));
										}
									}
									else
									{
										ps.Add(v.GetJavaScriptReferenceCode(sb));
									}
									if (v.CompileddataType != null)
									{
										if (typeof(JsDateTime).IsAssignableFrom(v.CompileddataType.BaseClassType) || typeof(DateTime).IsAssignableFrom(v.CompileddataType.BaseClassType))
										{
											EasyDataSet eds = ActionMethod.Owner.ObjectInstance as EasyDataSet;
											if (eds == null)
											{
												ComponentPointer cp = ActionMethod.Owner.ObjectInstance as ComponentPointer;
												if (cp != null)
												{
													eds = cp.ObjectInstance as EasyDataSet;
												}
											}
											if (eds != null)
											{
												if (string.CompareOrdinal("SetFieldValue", ActionMethod.MethodName) == 0)
												{
													ps[ps.Count - 1] = string.Format(CultureInfo.InvariantCulture,
														"JsonDataBinding.datetime.toIso({0})", ps[ps.Count - 1]);
													VPLUtil.AddJsFile("dateformat_min.js");
												}
											}
										}
									}
								}
							}
							if (pageId > 0)
							{
								ps.Add(pageId.ToString(CultureInfo.InvariantCulture));
							}
						}
						IFormSubmitter fs = null;
						MemberPointer iop = ac as MemberPointer;
						if (iop != null && iop.Owner != null)
						{
							fs = iop.Owner.ObjectInstance as IFormSubmitter;
						}
						bool isSubmission = (fs != null && fs.IsSubmissionMethod(iop.MemberName));
						if (isSubmission)
						{
							StringCollection subm;
							if (!formsumissions.TryGetValue(fs.FormName, out subm))
							{
								subm = new StringCollection();
								formsumissions.Add(fs.FormName, subm);
								subm.Add(ceCondition);
							}
						}
						else
						{
							if (ActionMethod.RunAt == EnumWebRunAt.Server)
							{
								if (_returnReceiver != null)
								{
									string srName = _returnReceiver.GetJavaScriptReferenceCode(sb);
									sb.Add(indent);
									sb.Add(string.Format(CultureInfo.InvariantCulture,
										"{0}=JsonDataBinding.values.{1};\r\n",
										srName,
										((ISourceValuePointer)_returnReceiver).DataPassingCodeName));
									if (!string.IsNullOrEmpty(nextActionInput))
									{
										sb.Add(string.Format(CultureInfo.InvariantCulture,
											"{0}={1};\r\n",
											nextActionInput, srName));
									}
								}
								else if (!string.IsNullOrEmpty(nextActionInput))
								{
									sb.Add(indent);
									sb.Add(string.Format(CultureInfo.InvariantCulture,
										"{0}=JsonDataBinding.values.{0};\r\n",
										nextActionInput));
								}
							}
							else
							{
								if (this.RootClassPointer != null && this.ActionMethod != null
									&& (ActionMethod is CustomMethodPointer || ActionMethod is FireEventMethod)
									&& this.ActionMethod.Owner != null && typeof(WebPage).IsAssignableFrom(this.ActionMethod.Owner.ObjectType))
								{
									UInt32 actClassId = 0;
									ClassPointer acp = this.ActionMethod.Owner as ClassPointer;
									if (acp != null)
									{
										actClassId = acp.ClassId;
									}
									else
									{
										ClassInstancePointer cip = this.ActionMethod.Owner as ClassInstancePointer;
										if (cip != null)
										{
											actClassId = cip.DefinitionClassId;
										}
									}
									if (actClassId != 0)
									{
										StringBuilder msb = new StringBuilder();
										if (this.RootClassPointer.ClassId != actClassId)
										{
											msb.Append("JsonDataBinding.executeByPageId(");
											msb.Append(actClassId);
											msb.Append(",'");
											msb.Append(this.ActionMethod.CodeName);
											msb.Append("'");
											if (ps.Count > 0)
											{
												for (int i = 0; i < ps.Count; i++)
												{
													msb.Append(",");
													msb.Append(ps[i]);
												}
											}
											msb.Append(")");
										}
										else
										{
											FireEventMethod fem = this.ActionMethod as FireEventMethod;
											if (fem != null && fem.Event != null)
											{
												msb.Append("if(limnorPage.");
												msb.Append(fem.Event.CodeName);
												msb.Append(") limnorPage.");
												msb.Append(fem.Event.CodeName);
											}
											else
											{
												msb.Append(this.ActionMethod.MethodName);
											}
											msb.Append("(");
											if (ps.Count > 0)
											{
												msb.Append(ps[0]);
												for (int i = 1; i < ps.Count; i++)
												{
													msb.Append(",");
													msb.Append(ps[i]);
												}
											}
											msb.Append(")");
										}
										if (!string.IsNullOrEmpty(ceCondition))
										{
											sb.Add(indent);
											sb.Add("if(");
											sb.Add(ceCondition);
											sb.Add(") {\r\n");
										}
										if (_returnReceiver != null || !string.IsNullOrEmpty(nextActionInput))
										{
											string rt;
											if (_returnReceiver != null)
											{
												rt = _returnReceiver.GetJavaScriptReferenceCode(sb);
											}
											else
											{
												rt = nextActionInput;
											}
											IProperty p = _returnReceiver as IProperty;
											if (p != null)
											{
												MethodDataTransfer.CreateJavaScript(p, msb.ToString(), sb, null);
												if (!string.IsNullOrEmpty(nextActionInput))
												{
													sb.Add(indent);
													sb.Add(string.Format(CultureInfo.InvariantCulture,
														"{0}={1};\r\n", nextActionInput, rt));
												}
											}
											else
											{
												sb.Add(indent);
												sb.Add(rt);
												sb.Add("=");
												sb.Add(msb.ToString());
												sb.Add(";\r\n");
												if (_returnReceiver != null)
												{
													sb.Add(indent);
													sb.Add(string.Format(CultureInfo.InvariantCulture,
														"{0}={1};\r\n", nextActionInput, rt));
												}
											}
										}
										else
										{
											sb.Add(indent);
											sb.Add(msb.ToString());
											sb.Add(";\r\n");
										}
										if (!string.IsNullOrEmpty(ceCondition))
										{
											sb.Add("\r\n}\r\n");
										}
										return;
									}
								}
								bool isDialog = false;
								WebPage wpage = null;
								string r = nextActionInput;
								IProperty rp = _returnReceiver as IProperty;
								IWebClientSupport wco = null;
								bool isWebConfirm = false;
								if (ActionMethod != null && ActionMethod.Owner != null)
								{
									Type t = this.ActionMethod.Owner.ObjectInstance as Type;
									if (t != null)
									{
										if (t.Equals(typeof(WebMessageBox)))
										{
											if (string.CompareOrdinal(ActionMethod.MethodName, "confirm") == 0)
											{
												isWebConfirm = true;
											}
										}
										else if (typeof(WebPage).IsAssignableFrom(t))
										{

										}
									}
									if (!isWebConfirm)
									{
										if (string.CompareOrdinal(ActionMethod.MethodName, "ShowChildDialog") == 0)
										{
											wpage = this.ActionMethod.Owner.ObjectInstance as WebPage;
											isDialog = true;
											if (wpage == null)
											{
												throw new DesignerException("Error compiling ShowChildDialog for action [{0}]. WebPage is null.", this);
											}
										}
									}
									if (!isDialog)
									{
										if (rp != null)
										{
											wco = this.ActionMethod.Owner.ObjectInstance as IWebClientSupport;
										}
									}
								}
								if (_returnReceiver != null)
								{
									r = _returnReceiver.GetJavaScriptReferenceCode(sb);
								}
								string vDlg = null;
								if (isDialog)
								{
									_jsCodeSegments = new List<StringCollection>();
									StringCollection sb0 = new StringCollection();
									vDlg = wpage.CreateDialogParameters(sb0, ps, true);
									StringCollection sb1 = new StringCollection();
									sb1.Add(vDlg);
									_jsCodeSegments.Add(sb1); //name
									_jsCodeSegments.Add(sb0); //parameters
									StringCollection sb2 = new StringCollection();
									if (!string.IsNullOrEmpty(ceCondition))
									{
										sb2.Add(ceCondition);
									}
									_jsCodeSegments.Add(sb2); //condition
								}
								else
								{
									string indent2 = indent;
									if (!string.IsNullOrEmpty(ceCondition))
									{
										sb.Add(indent);
										sb.Add("if(");
										sb.Add(ceCondition);
										sb.Add(") {\r\n");
										indent2 = string.Format(CultureInfo.InvariantCulture, "{0}\t", indent);
									}
									sb.Add(indent2);
									if (isWebConfirm)
									{
										ac.CreateJavaScript(sb, ps, "JsonDataBinding.confirmResult");
										if (!string.IsNullOrEmpty(r))
										{
											sb.Add(indent2);
											sb.Add(string.Format(CultureInfo.InvariantCulture,
												"{0}=JsonDataBinding.confirmResult;\r\n", r));
											if (_returnReceiver != null && !string.IsNullOrEmpty(nextActionInput))
											{
												sb.Add(indent2);
												sb.Add(string.Format(CultureInfo.InvariantCulture,
														"{0}={1};\r\n", nextActionInput, r));
											}
										}
									}
									else
									{
										if (rp != null && wco != null)
										{
											MethodDataTransfer.CreateJavaScript(rp, wco.GetJavaScriptWebMethodReferenceCode(this.ActionMethod.Owner.CodeName, this.ActionMethod.MethodName, sb, ps), sb, ps);
										}
										else if (!string.IsNullOrEmpty(r))
										{
											ac.CreateJavaScript(sb, ps, r);
											if (_returnReceiver != null && !string.IsNullOrEmpty(nextActionInput))
											{
												sb.Add(indent2);
												sb.Add(string.Format(CultureInfo.InvariantCulture,
														"var {0}={1};\r\n", nextActionInput, r));
											}
										}
										else
										{
											bool isSetDataSource = false;
											SetterPointer sp = ac as SetterPointer;
											if (sp != null)
											{
												if (sp.SetProperty != null && sp.SetProperty.PropertyOwner != null)
												{
													IWebClientPropertyHolder wcph = sp.SetProperty.PropertyOwner.ObjectInstance as IWebClientPropertyHolder;
													if (wcph != null)
													{
														IWebClientPropertyValueHolder eds = null;
														if (this.ParameterValues != null && this.ParameterValues.Count > 0 && this.ParameterValues[0] != null)
														{
															if (this.ParameterValues[0].ValueType == EnumValueType.Property && this.ParameterValues[0].Property != null)
															{
																eds = this.ParameterValues[0].Property.ObjectInstance as IWebClientPropertyValueHolder;
															}
														}
														string sCode = wcph.CreateSetPropertyJavaScript(sp.SetProperty.PropertyOwner.CodeName, sp.SetProperty.Name, eds);
														if (!string.IsNullOrEmpty(sCode))
														{
															sb.Add(indent2);
															sb.Add(sCode);
															isSetDataSource = true;
														}
													}
												}
											}
											if (!isSetDataSource)
											{
												ac.CreateJavaScript(sb, ps, null);
											}
										}
									}
									if (!string.IsNullOrEmpty(ceCondition))
									{
										sb.Add("\r\n");
										sb.Add(indent);
										sb.Add("}\r\n");
									}
								}
							}
						}
					}
					catch (Exception err)
					{
						if (VPLUtil.ErrorLogger != null)
						{
							VPLUtil.ErrorLogger.LogError("2. Error compiling action [{0}, {1}]. ActionMethod {2}. Error:{3}", ActionId, ActionName, ActionMethod.GetType().FullName, DesignerException.FormExceptionText(err));
						}
						else
						{
							throw new DesignerException("2. Error compiling action [{0}, {1}]. ActionMethod {2}. Error:{3}", ActionId, ActionName, ActionMethod.GetType().FullName, DesignerException.FormExceptionText(err));
						}
					}
					finally
					{
						Indentation.SetIndentation(idtOrigin);
					}
				}
				else
				{
					if (VPLUtil.ErrorLogger != null)
					{
						VPLUtil.ErrorLogger.LogError("ActionMethod {0} does not inherits IActionCompiler", ActionMethod.GetType().FullName);
					}
					else
					{
						throw new DesignerException("ActionMethod {0} does not inherits IActionCompiler", ActionMethod.GetType().FullName);
					}
				}
			}
		}
		public void CreatePhpScript(StringCollection sb, string nextActionInput, string indent)
		{
			if (!IsValid)
			{
				if (VPLUtil.ErrorLogger != null)
				{
					string sn = "unknown";
					if (this.RootClassPointer != null)
					{
						sn = this.RootClassPointer.Name;
					}
					VPLUtil.ErrorLogger.LogError("Php compiler: Invalid action [{0},{1}] in [{2}]. Fix its parameters or remove this action and recreate it.", ActionId, ActionName, sn);
				}
				return;
			}
			if (!string.IsNullOrEmpty(Description))
			{
				sb.Add(indent);
				sb.Add("/*\r\n");
				sb.Add(Description);
				sb.Add("\r\n");
				sb.Add(indent);
				sb.Add("*/\r\n");
			}

			if (ActionMethod == null)
			{
				if (VPLUtil.ErrorLogger != null)
				{
					VPLUtil.ErrorLogger.LogError("ActionMethod is null for action [{0},{1}]", ActionId, ActionName);
				}
				else
				{
					throw new DesignerException("ActionMethod is null for action [{0},{1}]", ActionId, ActionName);
				}
			}
			else
			{
				IActionCompiler ac = ActionMethod as IActionCompiler;
				if (ac != null)
				{
					try
					{
						IList<ISourceValuePointer> cps = this.GetServerProperties(0);
						if (cps != null && cps.Count > 0)
						{
							if (_scopeMethod != null)
							{
								_scopeMethod.AddDownloads(cps);
							}
						}
						string ceCondition = null;
						if (_condition != null)
						{
							ceCondition = _condition.CreatePhpScriptCode(sb);
						}
						StringCollection ps = new StringCollection();
						for (int k = 0; k < ParameterCount; k++)
						{
							ParameterValue v = _parameterValues[k];
							if (v == null)
							{
								ps.Add("null");
							}
							else
							{
								ps.Add(v.CreatePhpScript(sb));
							}
						}
						if (string.IsNullOrEmpty(ceCondition))
						{
							sb.Add(indent);
							if (_returnReceiver != null || !string.IsNullOrEmpty(nextActionInput))
							{
								string r;
								if (_returnReceiver != null)
								{
									r = _returnReceiver.GetPhpScriptReferenceCode(sb);
								}
								else
								{
									r = nextActionInput;
								}
								ac.CreatePhpScript(sb, ps, r);
								if (_returnReceiver != null && !string.IsNullOrEmpty(r) && !string.IsNullOrEmpty(nextActionInput))
								{
									sb.Add(indent);
									sb.Add(string.Format(CultureInfo.InvariantCulture,
										"{0}={1};\r\n",
										nextActionInput, r));
								}
							}
							else
								ac.CreatePhpScript(sb, ps, null);
						}
						else
						{
							sb.Add(indent);
							sb.Add("if(");
							sb.Add(ceCondition);
							sb.Add(") {\r\n");
							string indent2 = string.Format(CultureInfo.InvariantCulture, "{0}\t", indent);
							sb.Add(indent2);
							if (_returnReceiver != null || !string.IsNullOrEmpty(nextActionInput))
							{
								string r;
								if (_returnReceiver != null)
								{
									r = _returnReceiver.GetPhpScriptReferenceCode(sb);
								}
								else
								{
									r = nextActionInput;
								}
								ac.CreatePhpScript(sb, ps, r);
								if (_returnReceiver != null && !string.IsNullOrEmpty(nextActionInput))
								{
									sb.Add(indent2);
									sb.Add(string.Format(CultureInfo.InvariantCulture,
										"{0}={1};\r\n",
										nextActionInput, r));
								}
							}
							else
								ac.CreatePhpScript(sb, ps, null);
							sb.Add(indent);
							sb.Add("}\r\n");
						}

					}
					catch (Exception err)
					{
						if (VPLUtil.ErrorLogger != null)
						{
							VPLUtil.ErrorLogger.LogError("3. Error compiling action [{0}, {1}]. ActionMethod {2}. Error:{3}", ActionId, ActionName, ActionMethod.GetType().FullName, DesignerException.FormExceptionText(err));
						}
						else
						{
							throw new DesignerException("3. Error compiling action [{0}, {1}]. ActionMethod {2}. Error:{3}", ActionId, ActionName, ActionMethod.GetType().FullName, DesignerException.FormExceptionText(err));
						}
					}
				}
				else
				{
					if (VPLUtil.ErrorLogger != null)
					{
						VPLUtil.ErrorLogger.LogError("ActionMethod {0} does not inherits IActionCompiler", ActionMethod.GetType().FullName);
					}
					else
					{
						throw new DesignerException("ActionMethod {0} does not inherits IActionCompiler", ActionMethod.GetType().FullName);
					}
				}
			}
		}
		public IList<ISourceValuePointer> GetUploadProperties(UInt32 taskId)
		{
			if (this.ActionMethod != null)
			{
				if (this.ActionMethod.RunAt == EnumWebRunAt.Server)
				{
					return GetClientProperties(taskId);
				}
				else if (this.ActionMethod.RunAt == EnumWebRunAt.Client)
				{
					ParameterValueCollection pvs = this.ParameterValues;
					if (pvs != null && pvs.Count > 0)
					{
						List<ISourceValuePointer> l = new List<ISourceValuePointer>();
						foreach (ParameterValue pv in pvs)
						{
							if (pv.ValueType == EnumValueType.MathExpression)
							{
								if (pv.MathExpression != null)
								{
									IList<ISourceValuePointer> ls = pv.MathExpression.GetUploadProperties(taskId);
									if (ls != null && ls.Count > 0)
									{
										l.AddRange(ls);
									}
								}
							}
						}
						return l;
					}
				}
			}
			return null;
		}
		public IList<ISourceValuePointer> GetClientProperties(UInt32 taskId)
		{
			return getSourceValuePointers(taskId, EnumWebValueSources.HasClientValues);
		}
		public IList<ISourceValuePointer> GetServerProperties(UInt32 taskId)
		{
			return getSourceValuePointers(taskId, EnumWebValueSources.HasServerValues);
		}
		public void Execute(List<ParameterClass> eventParameters)
		{
			MethodPointer mp = (MethodPointer)ActionMethod;
			MethodBase mif = mp.MethodDef;
			ParameterInfo[] pifs = mp.Info;
			object[] vs = new object[mp.ParameterCount];
			for (int k = 0; k < mp.ParameterCount; k++)
			{
				vs[k] = null;
				IEventParameter iep = ParameterValues[k].AsEventParameter();
				if (iep != null)
				{
					if (eventParameters != null)
					{
						foreach (ParameterClass p in eventParameters)
						{
							if (iep.IsSameParameter(p))
							{
								vs[k] = p.ObjectInstance;
								break;
							}
						}
					}
				}
				else
				{
					vs[k] = ParameterValues[k].ObjectInstance;
				}
				if (vs[k] != null)
				{
					if (!pifs[k].ParameterType.Equals(vs[k].GetType()))
					{
						vs[k] = ValueTypeUtil.ConvertValueByType(pifs[k].ParameterType, vs[k]);
					}
				}
			}
			object ret;
			if (mif.IsStatic)
			{
				ret = mif.Invoke(null, vs);
			}
			else
			{
				ret = mif.Invoke(mp.ObjectInstance, vs);
			}
			MethodInfo minfo = mif as MethodInfo;
			if (minfo != null)
			{
				if (!typeof(void).Equals(minfo.ReturnType))
				{
					ReturnValue = ret;
				}
			}
		}
		public object GetParameterValue(string name)
		{
			ParameterValue v = null;
			if (_parameterValues != null)
			{
				v = _parameterValues.GetParameterValue(name);
			}
			return v;
		}
		public void SetParameterValue(string name, object value)
		{
			if (_parameterValues != null)
			{
				_parameterValues.SetParameterValue(name, value);
			}
		}
		public void SetParameterTypeByIndex(int idx, DataTypePointer type)
		{
			if (_parameterValues != null && idx >= 0 && idx < _parameterValues.Count)
			{
				_parameterValues[idx].SetDataType(type);
			}
		}
		public void InsertParameterValue(int idx, ParameterValue pv)
		{
			_disableParamValidate = true;
			ParameterValue[] ps = new ParameterValue[this.ParameterCount + 1];
			if (_parameterValues != null)
			{
				for (int i = 0; i < _parameterValues.Count; i++)
				{
					if (i < idx)
					{
						ps[i] = _parameterValues[i];
					}
					else
					{
						ps[i + 1] = _parameterValues[i];
					}
				}
			}
			ps[idx] = pv;
			_parameterValues = new ParameterValueCollection();
			_parameterValues.AddRange(ps);
		}
		public void RemoveParameterValue(int idx)
		{
			if (_parameterValues != null)
			{
				if (idx >= 0 && idx < _parameterValues.Count)
				{
					if (_parameterValues.Count == 1)
					{
						_parameterValues = null;
					}
					else
					{
						ParameterValue[] ps = new ParameterValue[this.ParameterCount - 1];
						for (int i = 0; i < _parameterValues.Count; i++)
						{
							if (i < idx)
							{
								ps[i] = _parameterValues[i];
							}
							else if (i > idx)
							{
								ps[i - 1] = _parameterValues[i];
							}
						}
						_parameterValues = new ParameterValueCollection();
						_parameterValues.AddRange(ps);
					}
				}
			}
			_disableParamValidate = false;
			_userData = null;
		}
		private Dictionary<string, object> _userData;
		public object GetUserData(string name)
		{
			if (_userData != null)
			{
				if (_userData.ContainsKey(name))
				{
					return _userData[name];
				}
			}
			return null;
		}
		public void SetUserData(string name, object data)
		{
			if (_userData == null)
			{
				_userData = new Dictionary<string, object>();
			}
			if (_userData.ContainsKey(name))
			{
				_userData[name] = data;
			}
			_userData.Add(name, data);
		}
		public EventHandler GetPropertyChangeHandler()
		{
			return PropertyChanged;
		}
		private void fireValueChange(EventArgs e)
		{
			if (PropertyChanged != null && !_reading)
			{
				PropertyChanged(this, e);
			}
		}
		private void onVauleChange(object sender, EventArgs e)
		{
			if (PropertyChanged != null && !_reading)
			{
				if (_scopeMethod != null && _scopeMethod.CurrentEditor != null)
				{
					_scopeMethod.CurrentEditor.SetChangedAction(this);
				}
				PropertyChangeEventArg pe = e as PropertyChangeEventArg;
				if (pe == null)
				{
					pe = new PropertyChangeEventArg("actionParameter");
				}
				else
				{
					if (this.ActionMethod != null && this.ActionMethod.Owner != null)
					{
						IDynamicMethodParameters dmp = this.ActionMethod.Owner.ObjectInstance as IDynamicMethodParameters;
						if (dmp != null)
						{
							ParameterInfo[] pifs = dmp.GetDynamicMethodParameters(this.ActionMethod.MethodName, this);
							if (pifs != null)
							{
								ValidateParameterValues();
							}
						}
					}
				}
				PropertyChanged(this, pe);
			}
		}
		/// <summary>
		/// It can be a ClassPointer, a MemberComponentId, or a TypePointer.
		/// 
		/// </summary>
		/// <returns></returns>
		private IClass getActionExecuter()
		{
			ClassPointer root = this.RootClassPointer;
			if (root != null)
			{
				ClassPointer cpr = root.GetExternalExecuterClass(this);
				if (cpr != null)
				{
					return cpr;
				}
			}
			IPropertySetter sp = ActionMethod as IPropertySetter;
			if (sp != null)
			{
				MemberComponentIdCustom c = sp.SetProperty.Holder as MemberComponentIdCustom;
				if (c != null)
				{
					return c.Pointer;
				}
				return sp.SetProperty.Holder;
			}
			else if (ActionMethod != null)
			{
				IObjectIdentity mp = ActionMethod.IdentityOwner;
				IMemberPointer p = ActionMethod as IMemberPointer;
				if (p != null)
				{
					IClass ic = p.Holder;
					if (ic != null)
					{
						return ic;
					}
					if (_class != null)
					{
						return _class;
					}
				}
				MemberComponentIdCustom mcc = mp as MemberComponentIdCustom;
				if (mcc != null)
				{
					return mcc.Pointer;
				}
				MemberComponentId mmc = mp as MemberComponentId;
				if (mmc != null)
				{
					return mmc;
				}
				p = mp as IMemberPointer;
				if (p != null)
				{
					return p.Holder;
				}
				IObjectPointer op = mp as IObjectPointer;
				IClass co = mp as IClass;
				while (co == null && op != null)
				{
					op = op.Owner;
					co = op as IClass;
					p = op as IMemberPointer;
					if (p != null)
					{
						return p.Holder;
					}
				}
				if (co != null)
				{
					return co;
				}
				else
				{
					MethodReturnMethod mr = ActionMethod as MethodReturnMethod;
					if (mr != null)
					{
						return mr.RootPointer;
					}
					CustomMethodParameterPointer cmpp = mp as CustomMethodParameterPointer;
					if (cmpp != null)
					{
						ParameterClass pc = cmpp.Parameter;
						if (pc != null)
						{
							ClassPointer cpr = pc.VariableCustomType;
							if (cpr != null)
							{
								XClass<InterfaceClass> xi = cpr.ObjectInstance as XClass<InterfaceClass>;
								if (xi != null)
								{
									InterfaceClass ifc = xi.ObjectValue as InterfaceClass;
									if (ifc != null)
									{
										return new InterfacePointer(ifc);
									}
								}
							}
						}
					}
					throw new DesignerException("Cannot find holder for Action [{0}]", ActionId);
				}
			}
			return null;
		}
		private IList<ISourceValuePointer> getSourceValuePointers(UInt32 taskId, EnumWebValueSources scope)
		{
			List<ISourceValuePointer> list = new List<ISourceValuePointer>();
			if (_condition != null)
			{
				IList<ISourceValuePointer> l1 = _condition.GetValueSources();
				if (l1 != null && l1.Count > 0)
				{
					foreach (ISourceValuePointer p in l1)
					{
						if (taskId != 0)
						{
							p.SetTaskId(taskId);
						}
						if (scope == EnumWebValueSources.HasClientValues)
						{
							if (p.IsWebClientValue())
							{
								list.Add(p);
							}
						}
						else if (scope == EnumWebValueSources.HasServerValues)
						{
							if (!p.IsWebClientValue())
							{
								list.Add(p);
							}
						}
						else
						{
							list.Add(p);
						}
					}
				}
			}
			ParameterValueCollection ps;
			MethodDataTransfer mdt = this.ActionMethod as MethodDataTransfer;
			if (mdt != null)
			{
				ps = mdt.GetParameters();
			}
			else
			{
				ps = _parameterValues;
			}
			if (ps != null)
			{
				for (int k = 0; k < ps.Count; k++)
				{
					ParameterValue v = ps[k];
					if (v != null)
					{
						List<ISourceValuePointer> l2 = new List<ISourceValuePointer>();
						v.GetValueSources(l2);
						if (l2.Count > 0)
						{
							foreach (ISourceValuePointer p in l2)
							{
								bool found = false;
								if (taskId != 0)
								{
									p.SetTaskId(taskId);
								}
								foreach (ISourceValuePointer p2 in list)
								{
									if (p2.IsSameProperty(p))
									{
										found = true;
										break;
									}
								}
								if (!found)
								{
									if (scope == EnumWebValueSources.HasClientValues)
									{
										if (p.IsWebClientValue())
										{
											list.Add(p);
										}
									}
									else if (scope == EnumWebValueSources.HasServerValues)
									{
										if (p.IsWebServerValue())
										{
											list.Add(p);
										}
									}
									else
									{
										list.Add(p);
									}
								}
							}
						}
					}
				}
			}
			return list;
		}
		[Browsable(false)]
		public Type ViewerType
		{
			get
			{
				return typeof(ActionViewerSingleAction);
			}
		}
		[Browsable(false)]
		public Type ActionBranchType
		{
			get
			{
				return ActionMethod.ActionBranchType;
			}
		}
		[Browsable(false)]
		public IObjectPointer MethodOwner
		{
			get
			{
				if (_methodPointer != null)
				{
					CustomMethodPointer cmp = _methodPointer as CustomMethodPointer;
					if (cmp != null)
						return cmp.Owner;
					MethodPointer mp = _methodPointer as MethodPointer;
					if (mp != null)
						return mp.Owner;
					SetterPointer cs = _methodPointer as SetterPointer;
					if (cs != null)
						return cs.Owner;
					IObjectPointer op = _methodPointer as IObjectPointer;
					if (op != null)
						return op.Owner;
				}
				if (_reader != null)
				{
					return (IObjectPointer)(_reader.ObjectList.RootPointer);
				}
				return null;
			}
		}
		[Browsable(false)]
		public LimnorProject Project
		{
			get
			{
				if (_class != null)
				{
					return _class.Project;
				}
				if (ScopeMethod != null)
				{
					return _scopeMethod.Project;
				}
				return null;
			}
		}
		[Browsable(false)]
		public EnumWebActionType WebActionType
		{
			get { return _webActType; }
		}

		public void CheckWebActionType()
		{
			_webActType = EnumWebActionType.Unknown;
			EnumWebRunAt methodRunAt = EnumWebRunAt.Unknown;
			EnumWebValueSources sources = EnumWebValueSources.Unknown;
			if (_condition != null)
			{
				IList<ISourceValuePointer> conditionSource = _condition.MathExp.GetValueSources();
				if (conditionSource != null)
				{
					sources = WebBuilderUtil.GetActionTypeFromSources(conditionSource);
				}
			}
			MethodDataTransfer mdt = this.ActionMethod as MethodDataTransfer;
			if (mdt != null)
			{
				_webActType = mdt.CheckWebActionType();
				if (_webActType == EnumWebActionType.Client)
				{
					if (sources == EnumWebValueSources.HasBothValues || sources == EnumWebValueSources.HasServerValues)
					{
						_webActType = EnumWebActionType.Download;
					}
					else
					{
						if (hasWebParameters(false))
						{
							_webActType = EnumWebActionType.Download;
						}
					}
				}
				else if (_webActType == EnumWebActionType.Server)
				{
					if (sources == EnumWebValueSources.HasBothValues || sources == EnumWebValueSources.HasClientValues)
					{
						_webActType = EnumWebActionType.Upload;
					}
					else
					{
						if (hasWebParameters(true))
						{
							_webActType = EnumWebActionType.Upload;
						}
					}
				}
			}
			else
			{
				SetterPointer sp = this.ActionMethod as SetterPointer;
				if (sp != null)
				{
					if (sp.Value != null)
					{
						List<ISourceValuePointer> list = new List<ISourceValuePointer>();
						sp.Value.GetValueSources(list);
						sources = WebBuilderUtil.GetActionTypeFromSources(list, sources);
					}
					methodRunAt = sp.SetProperty.RunAt;
				}
				else
				{
					methodRunAt = this.ActionMethod.RunAt;
					ParameterValue[] pvs = ParameterValues.ToArray();
					for (int i = 0; i < pvs.Length; i++)
					{
						if (pvs[i] != null)
						{
							List<ISourceValuePointer> list = new List<ISourceValuePointer>();
							pvs[i].GetValueSources(list);
							sources = WebBuilderUtil.GetActionTypeFromSources(list, sources);
						}
					}
					if (methodRunAt == EnumWebRunAt.Unknown || methodRunAt == EnumWebRunAt.Inherit)
					{
						methodRunAt = ScopeRunAt;
					}
					if (methodRunAt == EnumWebRunAt.Unknown) //keep Inherit
					{
						if (DesignUtil.IsWebClientObject(this.ActionMethod.Owner))
						{
							methodRunAt = EnumWebRunAt.Client;
						}
						else
						{
							methodRunAt = EnumWebRunAt.Server;
						}
					}
				}
				_webActType = WebBuilderUtil.GetWebActionType(methodRunAt, sources);
				if (_webActType == EnumWebActionType.Unknown)
				{
					if (DesignUtil.IsWebClientObject(this.ActionMethod.Owner))
					{
						methodRunAt = EnumWebRunAt.Client;
					}
					else
					{
						methodRunAt = EnumWebRunAt.Server;
					}
					_webActType = WebBuilderUtil.GetWebActionType(methodRunAt, sources);
				}
			}
		}
		#endregion
		#region INonHostedObject Members

		private EventHandler NameChanging;
		private EventHandler PropertyChanged;
		/// <summary>
		/// update the action XML
		/// </summary>
		/// <param name="name"></param>
		/// <param name="rootNode"></param>
		/// <param name="writer"></param>
		public void OnPropertyChanged(string name, object property, XmlNode rootNode, XmlObjectWriter writer)
		{
			if (string.CompareOrdinal(name, "parameter") == 0)
				return;
			XmlNode actNode = SerializeUtil.GetActionNode(rootNode, this.ActionId);
			if (actNode != null)
			{
				if (string.CompareOrdinal(name, "ActionName") == 0)
				{
					XmlUtility.XmlUtil.SetNameAttribute(actNode, this.ActionName);
				}
				else if (string.CompareOrdinal(name, "Description") == 0)
				{
					XmlNode propNode = SerializeUtil.CreatePropertyNode(actNode, name);
					propNode.InnerText = this.Description;
				}
				else if (string.CompareOrdinal(name, "BreakBeforeExecute") == 0)
				{
					XmlNode propNode = SerializeUtil.CreatePropertyNode(actNode, name);
					propNode.InnerText = this.BreakBeforeExecute.ToString();
				}
				else if (string.CompareOrdinal(name, "BreakAfterExecute") == 0)
				{
					XmlNode propNode = SerializeUtil.CreatePropertyNode(actNode, name);
					propNode.InnerText = this.BreakAfterExecute.ToString();
				}
				else if (string.CompareOrdinal(name, "ActionMethod") == 0)
				{
					if (writer == null)
					{
						ILimnorDesignPane pane = Project.GetTypedData<ILimnorDesignPane>(_class.ClassId);
						if (pane != null)
						{
							writer = pane.Loader.Writer;
						}
					}
					if (writer != null)
					{
						writer.ClearErrors();
						_class.SaveAction(this, writer);
					}
				}
			}
		}
		[Browsable(false)]
		public string Name
		{
			get { return ActionName; }
		}
		[Browsable(false)]
		public uint MemberId
		{
			get { return this.ActionId; }
		}
		public void SetChangeEvents(EventHandler nameChange, EventHandler propertyChange)
		{
			NameChanging = nameChange;
			PropertyChanged = propertyChange;
		}

		#endregion
		#region IBeforeSerializeNotify Members
		[Browsable(false)]
		public XmlNode XmlData { get { return _xmlNode; } }
		public void OnBeforeRead(XmlObjectReader reader, XmlNode node)
		{
			_reading = true;
			_reader = reader;
			_xmlNode = node;
			ScopeMethodId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ScopeId);
			SubScopeId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_SubScopeId);
			_name = XmlUtil.GetNameAttribute(node);
			_actId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ActionID);
		}

		public void OnBeforeWrite(XmlObjectWriter writer, XmlNode node)
		{
			_writer = writer;
			if (_xmlNodeChanged != node)
			{
				_xmlNode = node;
			}
			if (ScopeMethodId != 0)
			{
				XmlUtil.SetAttribute(node, XmlTags.XMLATT_ScopeId, ScopeMethodId);
			}
			if (SubScopeId != 0)
			{
				XmlUtil.SetAttribute(node, XmlTags.XMLATT_SubScopeId, SubScopeId);
			}
		}
		public void ReloadFromXmlNode()
		{
			if (_reader != null && _xmlNode != null)
			{
				_desc = null;
				_breakBefore = false;
				_breakAfter = false;
				_condition = null;
				_display = null;
				_returnType = null;
				_paramNret = null;
				_returnValue = null;
				_reading = true;
				_reader.ReadObjectFromXmlNode(_xmlNode, this, this.GetType(), null);
				ActionName = XmlUtil.GetAttribute(_xmlNode, XmlTags.XMLATT_NAME);
				if (_methodPointer != null)
				{
					_methodPointer.Action = this;
				}
				Changed = false;
				_xmlNodeChanged = null;
				_reading = false;
			}
		}
		public void UpdateXmlNode(XmlObjectWriter writer)
		{
			if (_xmlNode != null)
			{
				if (writer != null)
				{
					writer.WriteObjectToNode(_xmlNode, this);
					Changed = false;
				}
				else
				{
					if (_writer == null)
					{
						_writer = XmlSerializerUtility.GetWriter(_reader) as XmlObjectWriter;
					}
					if (_writer != null)
					{
						_writer.WriteObjectToNode(_xmlNode, this);
						Changed = false;
					}
				}
			}
		}

		#endregion
		#region IActionContext Members
		[Browsable(false)]
		public UInt32 ActionContextId
		{
			get { return ActionId; }
		}

		[Browsable(false)]
		public object GetParameterType(uint id)
		{
			if (_methodPointer == null)
			{
			}
			else
			{
				return _methodPointer.GetParameterType(id);
			}
			return null;
		}

		public object GetParameterType(string name)
		{
			if (_methodPointer == null)
			{
				return null;
			}
			return _methodPointer.GetParameterType(name);
		}
		[Browsable(false)]
		public object ProjectContext
		{
			get { return Project; }
		}
		[Browsable(false)]
		public object OwnerContext
		{
			get
			{
				return IdentityOwner;
			}
		}
		[Browsable(false)]
		public IMethod ExecutionMethod
		{
			get
			{
				if (_methodPointer == null)
					return null;
				return _methodPointer.MethodPointed;
			}
		}
		[Browsable(false)]
		public void OnChangeWithinMethod(bool withinMethod)
		{
			if (_xmlNode != null)
			{
				if (_writer == null)
				{
					_writer = XmlSerializerUtility.GetWriter(_reader) as XmlObjectWriter;
				}
				if (_writer != null)
				{
					if (withinMethod)
					{
						_xmlNodeChanged = _xmlNode.OwnerDocument.CreateElement(_xmlNode.Name);
						_writer.WriteObjectToNode(_xmlNodeChanged, this);
					}
					else
					{
						_writer.WriteObjectToNode(_xmlNode, this);
						MethodDataTransfer mdt = this.ActionMethod as MethodDataTransfer;
						if (mdt != null)
						{
							mdt.OnChanged();
						}
					}
				}
			}
		}
		#endregion
		#region ISelectPropertySave Members

		public bool IsPropertyReadOnly(string propertyName)
		{
			if (string.Compare(propertyName, "ParameterValues", StringComparison.Ordinal) == 0)
			{
				SubMethodInfoPointer smi = ActionMethod as SubMethodInfoPointer;
				if (smi != null && !smi.SaveParameterValues)
				{
					return true;
				}
			}
			return false;
		}

		#endregion
		#region IAction Members
		private IActionsHolder _actsHolder;
		[Browsable(false)]
		[ReadOnly(true)]
		public IActionsHolder ActionHolder
		{
			get
			{
				return _actsHolder;
			}
			set
			{
				_actsHolder = value;
			}
		}

		#endregion
		#region IXmlNodeHolder Members
		[Browsable(false)]
		[ReadOnly(true)]
		public XmlNode DataXmlNode
		{
			get
			{
				return _xmlNode;
			}
			set
			{
				_xmlNode = value;
			}
		}

		#endregion
		#region IPostXmlNodeSerialize Members

		public void OnReadFromXmlNode(IXmlCodeReader serializer, XmlNode node)
		{
			if (this.ActionMethod != null && this.ActionMethod.Owner != null && _methodPointer != null)
			{
				IDynamicMethodParameters ds = this.ActionMethod.Owner.ObjectInstance as IDynamicMethodParameters;
				if (ds != null)
				{
					ParameterInfo[] pifs = ds.GetDynamicMethodParameters(this.ActionMethod.MethodName, null);
					if (pifs != null)
					{
						MethodPointer mp = _methodPointer as MethodPointer;
						if (mp != null && mp.AdjustParameterTypes(pifs))
						{
							bool b = _disableParamValidate;
							_disableParamValidate = true;
							_methodPointer.ValidateParameterValues(this.ParameterValues);
							_disableParamValidate = b;
						}
					}
				}
			}
		}

		public void OnWriteToXmlNode(IXmlCodeWriter serializer, XmlNode node)
		{
		}

		#endregion
		#region IScopeMethodHolder Members

		public MethodClass GetScopeMethod()
		{
			return ScopeMethod as MethodClass;
		}

		#endregion

		#region IVplMethodSelector Members

		public bool SelectMethodForParam(IWindowsFormsEditorService edSvc, bool runAtWebClient, Type t, ref object value)
		{
			if (this.ActionMethod != null && string.CompareOrdinal(this.ActionMethod.MethodName, "SwitchEventHandler") == 0)
			{
				ClassPointer root = this.ActionMethod.Owner.RootPointer;
				if (this.ParameterCount > 1)
				{
					ParameterValue pe = null;
					for (int i = 0; i < this.ParameterCount; i++)
					{
						if (this.ParameterValues[i] != null && string.CompareOrdinal(this.ParameterValues[i].Name, "eventName") == 0)
						{
							pe = this.ParameterValues[i];
							break;
						}
					}
					if (pe != null && pe.ConstantValue != null)
					{
						string ename = pe.ConstantValue.Value as string;
						if (!string.IsNullOrEmpty(ename))
						{
							EventInfo eif = t.GetEvent(ename);
							if (eif != null)
							{
								string[] pnames = null;
								Type[] tps = Type.EmptyTypes;
								ParameterInfo[] pifs = EventPointer.GetEventParameters(eif);
								if (pifs != null && pifs.Length > 0)
								{
									tps = new Type[pifs.Length];
									pnames = new string[pifs.Length];
									for (int i = 0; i < pifs.Length; i++)
									{
										tps[i] = pifs[i].ParameterType;
										pnames[i] = pifs[i].Name;
									}
								}
								UInt32 mid = 0;
								ParameterValue pv = value as ParameterValue;
								if (pv != null)
								{
									if (pv.ConstantValue != null)
									{
										VplMethodPointer vmp = pv.ConstantValue.Value as VplMethodPointer;
										if (vmp != null)
										{
											mid = vmp.MethodId;
										}
									}
								}
								DlgSelectHandler dlg = new DlgSelectHandler();
								dlg.LoadData(root, runAtWebClient, tps, pnames, mid);
								if (edSvc.ShowDialog(dlg) == DialogResult.OK)
								{
									ParameterValue pv2;
									if (pv != null)
									{
										pv2 = pv.Clone(this);
									}
									else
									{
										pv2 = new ParameterValue(this);
									}
									pv2.ValueType = EnumValueType.ConstantValue;
									if (pv2.ConstantValue == null)
									{
										pv2.ConstantValue = new ConstObjectPointer("handler", typeof(VplMethodPointer));
									}
									pv2.ConstantValue.SetValue(ConstObjectPointer.VALUE_Value, dlg.SelectedMethod);
									value = pv2;
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		#endregion
	}
}
