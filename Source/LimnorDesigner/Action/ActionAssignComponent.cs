/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using LimnorDesigner.MethodBuilder;
using System.ComponentModel;
using ProgElements;
using System.Xml;
using XmlSerializer;
using System.Windows.Forms;
using XmlUtility;
using MathExp;
using System.CodeDom;
using VPL;
using System.Drawing.Design;
using VSPrj;
using System.Collections.Specialized;
using System.Globalization;

namespace LimnorDesigner.Action
{
	/// <summary>
	/// assign an instance to a local variable. it does not use ActionMethod
	/// the value can be a constant, property, or a math expression
	/// for a primary type, a constant value is directly input through PropertyGrid
	/// for a struct/class, a constant value is created by choosing a constructor
	/// for a class, a Type parameter can be used to select a subclass type, and choose
	///     a constructor according to the subclass type
	/// </summary>
	[UseParentObject]
	public class ActionAssignComponent : IAction, ICustomTypeDescriptor, ITypeScopeHolder, IScopeMethodHolder
	{
		#region fields and constructors
		public const string Instance_Type = "InstanceType";
		public const string Instance_Value = "InstanceValue";
		private const UInt32 IntanceTypeId = 1878669652;
		private const UInt32 IntanceValueId = 2522910259;
		private MemberComponentId _var; //the component to receive the value, identified by its member id, _varId
		private ExpressionValue _condition;
		private UInt32 _varId;
		private ClassPointer _class;
		private UInt32 _actId;
		private string _name;
		private string _desc;
		private ParameterValueCollection _parameters;
		private ParameterValue _valType;
		private ParameterValue _val;
		private MethodClass _scopeMethod;
		private EnumWebActionType _webActType = EnumWebActionType.Unknown;
		private bool _breakBefore;
		private bool _breakAfter;
		private EventHandler _valueChanged;
		//
		private MethodCreateComponent _actMethod;
		private XmlObjectReader _reader;
		private XmlObjectWriter _writer;
		private XmlNode _xmlNode;
		private XmlNode _xmlNodeChanged;
		public ActionAssignComponent(ClassPointer owner)
		{
			_class = owner;
			_actMethod = new MethodCreateComponent(this);
			_parameters = new ParameterValueCollection();
			_valType = new ParameterValue(this);
			_valType.Name = Instance_Type;
			_valType.ParameterID = IntanceTypeId;
			ConstObjectPointer cop = new ConstObjectPointer(Instance_Type, typeof(Type));
			_valType.ConstantValue = cop;
			_valType.ConstantValue.SetOnValueChanged(onInstanceTypeChanged);
			_val = new ParameterValue(this);
			_val.ParameterID = IntanceValueId;
			_val.Name = Instance_Value;
			_parameters.Add(_valType);
			_parameters.Add(_val);
		}
		#endregion

		#region properties
		[Description("The name of the variable receiving the value")]
		[ParenthesizePropertyName(true)]
		public string VariableName
		{
			get
			{
				if (_var == null)
					return "?";
				return _var.Name;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public ClassPointer RootPointer
		{
			get
			{
				return _class;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public MemberComponentId ActionOwner
		{
			get
			{
				return _var;
			}
			set
			{
				_var = value;
				if (_var != null)
				{
					_varId = _var.MemberId;
				}
				//adjust type for parameters
				adjustParamType();
			}
		}
		[SaveAsObject(true)]
		[Description("Data type of the value to be assigned to the variable. It can be a sub-class of the variable class")]
		public ParameterValue InstanceType
		{
			get
			{
				if (_valType == null)
				{
					_valType = new ParameterValue(this);
					_valType.Name = ActionAssignInstance.Instance_Type;
					_valType.ParameterID = ActionAssignComponent.IntanceTypeId;
					_valType.SetDataType(new DataTypePointer(new TypePointer(typeof(Type))));
					_valType.SetValue(_var.ClassType);
					_valType.ConstantValue.SetOnValueChanged(onInstanceTypeChanged);
				}
				_valType.SetConstructorTypeScope(_var.ObjectType);
				return _valType;
			}
			set
			{
				if (value != null)
				{
					_valType = value;
					_valType.Name = ActionAssignInstance.Instance_Type;
					_valType.ParameterID = ActionAssignComponent.IntanceTypeId;
					_valType.SetDataType(new DataTypePointer(new TypePointer(typeof(Type))));
					_valType.ConstantValue.SetOnValueChanged(onInstanceTypeChanged);
					_valType.SetConstructorTypeScope(_var.ObjectType);
				}
			}
		}

		[SaveAsObject(true)]
		[PropertyReadOrder(100)]
		[Description("The value to be assigned to the variable")]
		public ParameterValue InstanceValue
		{
			get
			{
				if (_val == null)
				{
					_val = new ParameterValue(this);
					_val.Name = ActionAssignInstance.Instance_Value;
					_val.ParameterID = ActionAssignComponent.IntanceValueId;
					_val.ScopeMethod = _scopeMethod;
				}
				if (_valType != null && _valType.ConstantValue != null)
				{
					DataTypePointer dp = _valType.ConstantValue.Value as DataTypePointer;
					if (dp != null)
					{
						_val.SetDataType(dp);
					}
				}
				return _val;
			}
			set
			{
				if (value != null)
				{
					_val = value;
					_val.Name = ActionAssignInstance.Instance_Value;
					_val.ParameterID = ActionAssignComponent.IntanceValueId;
					_val.ScopeMethod = _scopeMethod;
					if (_valueChanged != null)
					{
						_valueChanged(this, EventArgs.Empty);
					}
				}
			}
		}
		#endregion

		#region private menthods
		private void onInstanceTypeChanged(object sender, EventArgs e)
		{
			if (_valType.ValueType == EnumValueType.ConstantValue)
			{
				_val.SetDataType((DataTypePointer)(_valType.ConstantValue.Value));
			}
		}
		private void adjustParamType()
		{
			if (_var != null)
			{
				bool bOK = false;
				_valType.SetDataType(new DataTypePointer(new TypePointer(typeof(Type))));
				_valType.SetConstructorTypeScope(_var.ObjectType);
				DataTypePointer vType;
				if (_var.VariableLibType == null)
				{
					vType = new DataTypePointer(_var.VariableCustomType);
				}
				else
				{
					vType = new DataTypePointer(new TypePointer(_var.VariableLibType));
				}
				DataTypePointer dp = _valType.ConstantValue.Value as DataTypePointer;
				if (dp != null)
				{
					bOK = vType.IsAssignableFrom(dp);
				}
				if (!bOK)
				{
					_valType.SetValue(vType);
					dp = vType;
				}

				if (!vType.IsAssignableFrom(_val.DataType))
				{
					_val.SetDataType(dp);
				}
			}
		}
		private IList<ISourceValuePointer> getProperties(UInt32 taskId, bool client)
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
						if (p.IsWebClientValue())
						{
							if (client)
							{
								list.Add(p);
							}
						}
						else
						{
							if (!client)
							{
								list.Add(p);
							}
						}
					}
				}
			}
			if (_val != null)
			{
				List<ISourceValuePointer> l2 = new List<ISourceValuePointer>();
				_val.GetValueSources(l2);
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
							if (p.IsWebClientValue())
							{
								if (client)
								{
									list.Add(p);
								}
							}
							else
							{
								if (!client)
								{
									list.Add(p);
								}
							}
						}
					}
				}
			}
			return list;
		}
		#endregion

		#region Methods
		public void ValidateParameterValues()
		{
			adjustParamType();
		}
		#endregion

		#region IAction Members
		public void OnAfterDeserialize(IActionsHolder actionsHolder)
		{
		}
		private EnumWebRunAt _scopeRunat = EnumWebRunAt.Unknown;
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

		public EnumWebActionType WebActionType
		{
			get { return _webActType; }
		}

		public void CheckWebActionType()
		{
			_webActType = EnumWebActionType.Unknown;
			EnumWebValueSources sources = EnumWebValueSources.Unknown;
			IList<ISourceValuePointer> conditionSource = null;
			if (_condition != null)
			{
				conditionSource = _condition.MathExp.GetValueSources();
				if (conditionSource != null)
				{
					sources = WebBuilderUtil.GetActionTypeFromSources(conditionSource);
				}
			}
			bool executerIsClient = false;
			MethodClass mc = ScopeMethod as MethodClass;
			if (mc != null)
			{
				if (mc.WebUsage == EnumMethodWebUsage.Client)
				{
					executerIsClient = true;
				}
				if (executerIsClient)
				{
					if (sources == EnumWebValueSources.HasBothValues || sources == EnumWebValueSources.HasServerValues)
					{
						_webActType = EnumWebActionType.Download;
					}
				}
				else
				{
					if (sources == EnumWebValueSources.HasBothValues || sources == EnumWebValueSources.HasClientValues)
					{
						_webActType = EnumWebActionType.Upload;
					}
				}
				if (_webActType == EnumWebActionType.Unknown)
				{
					if (_val != null)
					{
						List<ISourceValuePointer> list = new List<ISourceValuePointer>();
						_val.GetValueSources(list);
						sources = WebBuilderUtil.GetActionTypeFromSources(list, sources);
						if (executerIsClient)
						{
							if (sources == EnumWebValueSources.HasBothValues || sources == EnumWebValueSources.HasServerValues)
							{
								_webActType = EnumWebActionType.Download;
							}
						}
						else
						{
							if (sources == EnumWebValueSources.HasBothValues || sources == EnumWebValueSources.HasClientValues)
							{
								_webActType = EnumWebActionType.Upload;
							}
						}
					}
				}
			}
		}


		[Browsable(false)]
		public bool IsValid
		{
			get
			{
				if (_var != null && _valType != null && _val != null)
				{
					return true;
				}
				MathNode.LastValidationError = string.Format(CultureInfo.InvariantCulture, "Some of (_var, _valType, _val) are null for [{0}] of [{1}]. ({2},{3},{4})", this.ToString(), this.GetType().Name, _var, _valType, _val);
				return false;
			}
		}
		[Browsable(false)]
		public ClassPointer ExecuterRootHost { get { return _class; } }
		[Browsable(false)]
		[ReadOnly(true)]
		public bool Changed { get; set; }
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
				_condition.ScopeMethod = _scopeMethod;
				return _condition;
			}
			set
			{
				if (value != null)
				{
					_condition = value;
					if (_scopeMethod != null)
					{
						_condition.ScopeMethod = _scopeMethod;
					}
				}
			}
		}
		[Browsable(false)]
		public bool IsMethodReturn
		{
			get { return false; }
		}
		[Browsable(false)]
		public bool IsLocal
		{
			get { return true; }
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public bool AsLocal
		{
			get
			{
				return true;
			}
			set
			{
			}
		}
		[Browsable(false)]
		public bool IsPublic
		{
			get { return false; }
		}
		[Browsable(false)]
		public UInt32 ActionId
		{
			get
			{
				return _actId;
			}
			set
			{
				_actId = value;
			}
		}
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
		[Browsable(false)]
		public ClassPointer Class
		{
			get { return _class; }
		}
		[Browsable(false)]
		public UInt32 ExecuterClassId
		{
			get
			{
				return ClassId;
			}
		}
		[Browsable(false)]
		public UInt32 ExecuterMemberId
		{
			get
			{
				return _varId;
			}
		}
		[ParenthesizePropertyName(true)]
		public string ActionName
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}
		[Browsable(false)]
		public string Display
		{
			get { return string.Format("{0}.{1}", VariableName, _name); }
		}
		[Browsable(false)]
		public void ResetDisplay()
		{

		}
		public string Description
		{
			get
			{
				return _desc;
			}
			set
			{
				_desc = value;
			}
		}
		[Browsable(false)]
		public Type ViewerType
		{
			get { return typeof(ActionViewerSingleAction); }
		}
		[Browsable(false)]
		public Type ActionBranchType
		{
			get { return typeof(AB_SingleAction); }
		}
		[Browsable(false)]
		public IObjectPointer MethodOwner
		{
			get { return _class; }
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public IActionMethodPointer ActionMethod
		{
			get
			{
				if (_actMethod == null)
				{
					_actMethod = new MethodCreateComponent(this);
				}
				return _actMethod;
			}
			set
			{
			}
		}
		/// <summary>
		/// for a primary type, do not use value type
		/// </summary>
		[Browsable(false)]
		public int ParameterCount
		{
			get
			{
				return 2;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public ParameterValueCollection ParameterValues
		{
			get
			{
				if (_parameters == null)
				{
					_parameters = new ParameterValueCollection();
					_parameters.Add(this.InstanceType);
					_parameters.Add(this.InstanceValue);
					adjustParamType();
				}
				return _parameters;
			}
			set
			{
				_parameters = value;
			}
		}
		/// <summary>
		/// XmlNode that represents action changes within a method.
		/// The changes may not have saved to the XmlDocument tree.
		/// </summary>
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
		public bool HasChangedXmlData
		{
			get
			{
				return (_xmlNodeChanged != null);
			}
		}
		[Browsable(false)]
		public bool IsSameMethod(IAction act)
		{
			return (act.ActionMethod == null);
		}
		[Browsable(false)]
		public bool Edit(XmlObjectWriter writer, IMethod context, Form caller, bool isNewAction)
		{
			return false;
		}
		[Browsable(false)]
		public void Execute(List<ParameterClass> eventParameters)
		{

		}
		/// <summary>
		/// create an instance and assign it to _var
		/// </summary>
		/// <param name="currentAction"></param>
		/// <param name="nextAction"></param>
		/// <param name="compiler"></param>
		/// <param name="methodToCompile"></param>
		/// <param name="method"></param>
		/// <param name="statements"></param>
		/// <param name="debug"></param>
		[Browsable(false)]
		public void ExportCode(ActionBranch currentAction, ActionBranch nextAction, ILimnorCodeCompiler compiler, IMethodCompile methodToCompile, CodeMemberMethod method, CodeStatementCollection statements, bool debug)
		{
			if (IsValid)
			{
				CodeExpression ceCondition = null;
				if (_condition != null)
				{
					ceCondition = _condition.ExportCode(methodToCompile);
					if (ceCondition != null)
					{
						ceCondition = CompilerUtil.ConvertToBool(_condition.DataType, ceCondition);
					}
				}
				CodeStatementCollection sts = statements;
				if (ceCondition != null)
				{
					CodeConditionStatement cs = new CodeConditionStatement();
					cs.Condition = ceCondition;
					statements.Add(cs);
					sts = cs.TrueStatements;
				}
				CodeExpression right;
				if (_valType.ValueType == EnumValueType.ConstantValue)
				{
					right = _val.GetReferenceCode(methodToCompile, sts, true);
				}
				else
				{
					List<CodeExpression> ps = new List<CodeExpression>();
					ps.Add(_valType.GetReferenceCode(methodToCompile, sts, true));
					if (_val.ConstantValue != null)
					{
						CodeExpression[] pp = _val.ConstantValue.GetConstructorParameters(methodToCompile, sts);
						if (pp != null)
						{
							ps.AddRange(pp);
						}
					}
					right = new CodeCastExpression(_var.ObjectType,
						new CodeMethodInvokeExpression(
							new CodeTypeReferenceExpression(typeof(Activator)),
							"CreateInstance",
							ps.ToArray())
							);
				}
				CodeExpression left = _var.GetReferenceCode(methodToCompile, sts, false);
				CodeAssignStatement cas = new CodeAssignStatement(left, right);
				sts.Add(cas);
			}
		}
		public void ExportJavaScriptCode(ActionBranch currentAction, ActionBranch nextAction, StringCollection jsCode, StringCollection methodToCompile, JsMethodCompiler data)
		{
			if (IsValid)
			{
				string ceCondition = null;
				if (_condition != null)
				{
					ceCondition = _condition.CreateJavaScriptCode(methodToCompile);
				}
				StringCollection sts = methodToCompile;
				if (!string.IsNullOrEmpty(ceCondition))
				{
					sts.Add("if(");
					sts.Add(ceCondition);
					sts.Add(")\r\n{\r\n");
				}
				string right;
				if (_valType.ValueType == EnumValueType.ConstantValue)
				{
					right = _val.GetJavaScriptReferenceCode(sts);
				}
				else
				{
					right = _val.CreateJavaScript(sts);
				}
				string left = _var.GetJavaScriptReferenceCode(sts);
				sts.Add(left);
				sts.Add("=");
				sts.Add(right);
				sts.Add(";\r\n");
				if (!string.IsNullOrEmpty(ceCondition))
				{
					sts.Add("\r\n}\r\n");
				}
			}
		}
		public void ExportPhpScriptCode(ActionBranch currentAction, ActionBranch nextAction, StringCollection jsCode, StringCollection methodToCompile, JsMethodCompiler data)
		{
			if (IsValid)
			{
				string ceCondition = null;
				if (_condition != null)
				{
					ceCondition = _condition.CreatePhpScriptCode(methodToCompile);
				}
				StringCollection sts = methodToCompile;
				if (!string.IsNullOrEmpty(ceCondition))
				{
					sts.Add("if(");
					sts.Add(ceCondition);
					sts.Add(")\r\n{\r\n");
				}
				string right;
				if (_valType.ValueType == EnumValueType.ConstantValue)
				{
					right = _val.GetPhpScriptReferenceCode(sts);
				}
				else
				{
					right = _val.CreatePhpScript(sts);
				}
				string left = _var.GetPhpScriptReferenceCode(sts);
				sts.Add(left);
				sts.Add("=");
				sts.Add(right);
				sts.Add(";\r\n");
				if (!string.IsNullOrEmpty(ceCondition))
				{
					sts.Add("\r\n}\r\n");
				}
			}
		}
		public void ExportClientServerCode(ActionBranch currentAction, ActionBranch nextAction, ILimnorCodeCompiler compiler, IMethodCompile methodToCompile, CodeMemberMethod method, CodeStatementCollection statements, bool debug,
			StringCollection jsCode, StringCollection methodCode, JsMethodCompiler data)
		{
		}
		public void CreateJavaScript(StringCollection sb, Dictionary<string, StringCollection> formsumissions, string nextActionInput, string indent)
		{
		}
		public IList<ISourceValuePointer> GetUploadProperties(UInt32 taskId)
		{
			if (_var != null)
			{
				if (_var.RunAt == EnumWebRunAt.Server)
				{
					return GetClientProperties(taskId);
				}
			}
			return null;
		}
		public IList<ISourceValuePointer> GetClientProperties(UInt32 taskId)
		{
			return getProperties(taskId, true);
		}
		public IList<ISourceValuePointer> GetServerProperties(UInt32 taskId)
		{
			return getProperties(taskId, false);
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public uint ScopeMethodId
		{
			get;
			set;
		}
		[Browsable(false)]
		public UInt32 SubScopeId { get; set; }

		[Browsable(false)]
		[ReadOnly(true)]
		public IMethod ScopeMethod
		{
			get
			{
				if (_scopeMethod == null)
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
					if (_parameters != null)
					{
						foreach (ParameterValue p in _parameters)
						{
							p.ScopeMethod = _scopeMethod;
						}
					}
				}
			}
		}
		[Browsable(false)]
		public void ResetScopeMethod()
		{
		}
		public void SetScopeMethod(MethodClass method)
		{
			_scopeMethod = method;
			ScopeMethodId = method.MemberId;
		}
		[Browsable(false)]
		public IAction CreateNewCopy()
		{
			ActionAssignComponent a = new ActionAssignComponent(_class);
			a._desc = _desc;
			a._name = _name;
			a._var = (MemberComponentId)_var.Clone();
			a._varId = _varId;
			a._scopeMethod = _scopeMethod;
			a._valType.CopyData(_valType);
			a._val.CopyData(_val);
			if (_condition != null)
			{
				a._condition = (ExpressionValue)_condition.Clone();
			}
			a._webActType = _webActType;
			a._breakBefore = _breakBefore;
			a._breakAfter = _breakAfter;
			a._valueChanged = _valueChanged;
			a._reader = _reader;
			a._writer = _writer;
			a._actsHolder = _actsHolder;
			a.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return a;
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
				if (_scopeMethod != null)
				{
					return _scopeMethod.Project;
				}
				if (_actMethod != null)
				{
					if (_actMethod.RootPointer != null)
					{
						return _actMethod.RootPointer.Project;
					}
				}

				return null;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public IObjectPointer ReturnReceiver
		{
			get
			{
				return _var;
			}
			set
			{
			}
		}

		public object GetParameterValue(string name)
		{
			return InstanceValue;
		}

		public void SetParameterValue(string name, object value)
		{
			if (string.Compare(name, ActionAssignInstance.Instance_Value, StringComparison.Ordinal) == 0)
			{
				InstanceValue.SetValue(value);
				if (_valueChanged != null)
				{
					_valueChanged(this, EventArgs.Empty);
				}
			}
			if (string.Compare(name, ActionAssignInstance.Instance_Type, StringComparison.Ordinal) == 0)
			{
				bool typeChanged = false;
				DataTypePointer vType;
				if (_var.VariableLibType == null)
				{
					vType = new DataTypePointer(_var.VariableCustomType);
				}
				else
				{
					vType = new DataTypePointer(new TypePointer(_var.VariableLibType));
				}
				DataTypePointer dp = value as DataTypePointer;
				if (dp != null)
				{
					if (vType.IsAssignableFrom(dp))
					{
						InstanceType.SetValue(dp);
						typeChanged = true;
					}
				}
				if (typeChanged)
				{
					if (_valueChanged != null)
					{
						_valueChanged(this, EventArgs.Empty);
					}
				}
			}
		}
		public void SetChangeEvents(EventHandler nameChange, EventHandler propertyChange)
		{
			_valueChanged = propertyChange;
		}
		public EventHandler GetPropertyChangeHandler()
		{
			return _valueChanged;
		}
		public void CheckWebActionTypePass1()
		{
		}
		public void CheckWebActionTypePass2()
		{
		}
		public void EstablishObjectOwnership(IActionsHolder scope)
		{
		}
		#endregion

		#region IObjectIdentity Members
		[Browsable(false)]
		public bool IsSameObjectRef(IObjectIdentity objectIdentity)
		{
			ActionAssignInstance a = objectIdentity as ActionAssignInstance;
			if (a != null)
			{
				return (a.ActionId == this.ActionId);
			}
			return false;
		}
		[Browsable(false)]
		public IObjectIdentity IdentityOwner
		{
			get { return _var; }
		}
		[Browsable(false)]
		public bool IsStatic
		{
			get { return false; }
		}
		[Browsable(false)]
		public bool IsStaticAction
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Library; }
		}
		[Browsable(false)]
		public EnumPointerType PointerType
		{
			get { return EnumPointerType.Action; }
		}

		#endregion

		#region ICloneable Members

		public object Clone()
		{
			return this; //do not clone action so that it can be edited in all places
		}

		#endregion

		#region IEventHandler Members
		[Browsable(false)]
		public ulong WholeActionId
		{
			get { return DesignUtil.MakeDDWord(_actId, ClassId); }
		}

		#endregion

		#region IBreakPointOwner Members
		[Description("This property is used only for visual debugging. If it is True then before executing this action the debugger pauses and shows a break pointer at this action.")]
		public bool BreakBeforeExecute
		{
			get
			{
				return _breakBefore;
			}
			set
			{
				_breakBefore = value;
			}
		}
		[Description("This property is used only for visual debugging. If it is True then after executing this action the debugger pauses and shows a break pointer at this action.")]
		public bool BreakAfterExecute
		{
			get
			{
				return _breakAfter;
			}
			set
			{
				_breakAfter = value;
			}
		}

		#endregion

		#region IBeforeSerializeNotify Members
		[Browsable(false)]
		public void OnBeforeRead(XmlObjectReader reader, XmlNode node)
		{
			_reader = reader;
			_xmlNode = node;
			ScopeMethodId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ScopeId);
			SubScopeId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_SubScopeId);
			_varId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
			//load DataTypePointer from ComponentIconLocal
			ClassPointer root = reader.ObjectList.GetTypedData<ClassPointer>();
			_var = MemberComponentId.CreateMemberComponentId(root, root.ObjectList.GetObjectByID(_varId), _varId, null);
			adjustParamType();
		}
		[Browsable(false)]
		public void OnBeforeWrite(XmlSerializer.XmlObjectWriter writer, System.Xml.XmlNode node)
		{
			_writer = writer;
			if (_xmlNodeChanged != node)
			{
				_xmlNode = node;
			}
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ComponentID, _varId);
			if (ScopeMethodId != 0)
			{
				XmlUtil.SetAttribute(node, XmlTags.XMLATT_ScopeId, ScopeMethodId);
			}
			if (SubScopeId != 0)
			{
				XmlUtil.SetAttribute(node, XmlTags.XMLATT_SubScopeId, SubScopeId);
			}
		}
		[Browsable(false)]
		public void ReloadFromXmlNode()
		{
			if (_reader != null && _xmlNode != null)
			{
				_reader.ReadObjectFromXmlNode(_xmlNode, this, this.GetType(), null);
				ActionName = XmlUtil.GetAttribute(_xmlNode, XmlTags.XMLATT_NAME);
				adjustParamType();
				_xmlNodeChanged = null;
			}
		}
		[Browsable(false)]
		public void UpdateXmlNode(XmlObjectWriter writer)
		{
			if (_xmlNode != null)
			{
				if (writer != null)
				{
					writer.WriteObjectToNode(_xmlNode, this);
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
					}
				}
			}
		}
		[Browsable(false)]
		public XmlNode XmlData
		{
			get { return _xmlNode; }
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
			if (VPLUtil.GetBrowseableProperties(attributes))
			{
				PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(this, attributes, true);
				List<PropertyDescriptor> list = new List<PropertyDescriptor>();
				foreach (PropertyDescriptor p in ps)
				{
					if (p.Name == Instance_Type)
					{
						if (_var.ObjectType.IsPrimitive || _var.ObjectType.IsSealed || _var.ObjectType.IsValueType)
						{
							PropertyDescriptorForDisplay pd = new PropertyDescriptorForDisplay(this.GetType(), p.Name, _valType.ToString(), attributes);
							list.Add(pd);
						}
						else
						{
							list.Add(p);
						}
					}
					else
					{
						list.Add(p);
					}
				}
				ps = new PropertyDescriptorCollection(list.ToArray());
				return ps;
			}
			else
			{
				return TypeDescriptor.GetProperties(this, attributes, true);
			}
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

		#region IActionContext Members
		[Browsable(false)]
		public uint ActionContextId
		{
			get { return ActionId; }
		}

		public object GetParameterType(uint id)
		{
			if (id == ActionAssignComponent.IntanceTypeId)
			{
				return new DataTypePointer(new TypePointer(typeof(Type)));
			}
			if (id == ActionAssignComponent.IntanceValueId)
			{
				bool bOK = false;
				DataTypePointer vType;
				if (_var.VariableLibType == null)
				{
					vType = new DataTypePointer(_var.VariableCustomType);
				}
				else
				{
					vType = new DataTypePointer(new TypePointer(_var.VariableLibType));
				}
				DataTypePointer dp = _valType.ConstantValue.Value as DataTypePointer;
				if (dp != null)
				{
					bOK = vType.IsAssignableFrom(dp);
				}
				if (!bOK)
				{
					_valType.SetValue(_var.ClassType);
					dp = vType;
				}
				return dp;
			}

			return null;
		}

		public object GetParameterType(string name)
		{
			if (string.Compare(name, Instance_Type, StringComparison.Ordinal) == 0)
			{
				return typeof(int);
			}
			if (string.Compare(name, Instance_Value, StringComparison.Ordinal) == 0)
			{
				return InstanceType.DataType;
			}
			return null;
		}
		[Browsable(false)]
		public object ProjectContext
		{
			get { return Project; }
		}
		[Browsable(false)]
		public object OwnerContext
		{
			get { return _var; }
		}
		[Browsable(false)]
		public IMethod ExecutionMethod
		{
			get { return _actMethod; }
		}
		[Browsable(false)]
		public void OnChangeWithinMethod(bool withinmethod)
		{
			if (_writer == null)
			{
				_writer = XmlSerializerUtility.GetWriter(_reader) as XmlObjectWriter;
			}
			if (_writer != null && _xmlNode != null)
			{
				if (withinmethod)
				{
					_xmlNodeChanged = _xmlNode.OwnerDocument.CreateElement(_xmlNode.Name);
					_writer.WriteObjectToNode(_xmlNodeChanged, this);
				}
				else
				{
					_writer.WriteObjectToNode(_xmlNode, this);
				}
			}
		}
		#endregion

		#region ITypeScopeHolder Members

		public DataTypePointer GetTypeScope(string name)
		{
			if (string.Compare(name, Instance_Type, StringComparison.Ordinal) == 0)
			{
				if (_var.VariableLibType == null)
				{
					return new DataTypePointer(_var.VariableCustomType);
				}
				return new DataTypePointer(new TypePointer(_var.VariableLibType));
			}
			return null;
		}

		#endregion

		#region IScopeMethodHolder Members

		public MethodClass GetScopeMethod()
		{
			return _scopeMethod;
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
	}
}
