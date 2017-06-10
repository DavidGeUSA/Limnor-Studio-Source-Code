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
using MathExp;
using LimnorDesigner.Action;
using System.Xml;
using VSPrj;
using System.Drawing;
using System.Reflection;
using ProgElements;
using LimnorDesigner.MethodBuilder;
using System.CodeDom;
using System.CodeDom.Compiler;
using VPL;
using XmlSerializer;
using XmlUtility;
using System.Windows.Forms;
using LimnorDesigner.Interface;
using System.Web.Services;
using System.Xml.Serialization;
using System.Collections.Specialized;
using System.Globalization;
using TraceLog;
using System.Collections;
using System.Drawing.Design;
using System.ServiceModel;
using Limnor.WebBuilder;
using LimnorDesigner.DesignTimeType;
using LimnorDatabase;

namespace LimnorDesigner
{
	/// <summary>
	/// this is the customer method
	/// </summary>
	[UseParentObject]
	public class MethodClass : ICloneable, IEventHandler, IObjectPointer, IWithProject, IMethod, /*IActionOwner,*/ IMethodCompile, IActionGroup, ICustomObject, ICustomPointer, INonHostedObject, IAttributeHolder, IBeforeSerializeNotify, ICustomTypeDescriptor, IDelayedInitialize, IActionsHolder
	{
		#region fields and constructors
		private UInt32 _memberId;
		private string _name = "Method";
		private ParameterClass _returnValue;
		private List<ParameterClass> _parameters;
		private ClassPointer _owner;
		private IClass _holder;
		private BranchList _branchList;
		private Dictionary<UInt32, IAction> _acts;
		private SubscopeActions _finalActions;
		private ExceptionHandlerList _exceptionHandlers;
		private CodeTypeDeclaration _td;
		private CodeMemberMethod _codeMemberMethod;
		private CodeTryCatchFinallyStatement _tryCatchFinal;
		private object _compiler;
		private List<ComponentIcon> _componentIconList;
		private bool _isStatic;
		private bool _isFinal;
		private string _desc;
		private string _display;
		private Stack<IMethod0> _subMethods; //only ISubMethod should be used
		private List<IPostDeserializeProcess> _dependendDeserializers;
		//
		private XmlNode _xmlNode;
		private XmlObjectReader _reader;
		private XmlObjectWriter _writer;
		//
		private List<ConstObjectPointer> _attributes;
		//
		private EnumMethodWebUsage _webUsage = EnumMethodWebUsage.Server;
		private PropertyPointerList _uploads;
		private PropertyPointerList _downloads;
		//
		public MethodClass(ClassPointer owner)
		{
			_owner = owner;
			_exceptionHandlers = new ExceptionHandlerList(this);
			ParameterEditType = EnumParameterEditType.Edit;
		}
		#endregion
		#region editor management
		private static Dictionary<UInt32, MethodDesignerHolder> _editors;
		private static Dictionary<UInt32, Stack<MethodDesignerHolder>> _currentSubEditors;
		public static void GetActionNamesInEditors(StringCollection sc, UInt32 methodId)
		{
			if (_editors != null)
			{
				MethodDesignerHolder mv;
				if (_editors.TryGetValue(methodId, out mv))
				{
					mv.Method.GetActionNames(sc);
					mv.GetCurrentActionNames(sc);
					mv.Method.RootPointer.GetActionNames(sc);
					if (_currentSubEditors != null)
					{
						Stack<MethodDesignerHolder> smv;
						if (_currentSubEditors.TryGetValue(methodId, out smv))
						{
							Stack<MethodDesignerHolder>.Enumerator en = smv.GetEnumerator();
							while (en.MoveNext())
							{
								en.Current.GetCurrentActionNames(sc);
							}
						}
					}
				}
			}
		}
		#endregion
		#region private methods
		private void prepareJavascriptCompile(StringCollection methodCode)
		{
			//declare local variables within the method
			List<ComponentIcon> icons = this.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				foreach (ComponentIcon ci in icons)
				{
					ComponentIconLocal cil = ci as ComponentIconLocal;
					if (cil != null)
					{
						methodCode.Add("var ");
						methodCode.Add(cil.LocalPointer.CodeName);
						methodCode.Add("=");
						methodCode.Add(ValueTypeUtil.GetDefaultJavaScriptValueByType(cil.LocalPointer.BaseClassType));
						methodCode.Add(";\r\n");
					}
				}
			}
		}
		private void preparePhpScriptCompile(StringCollection methodCode)
		{
			////declare local variables within the method
			List<ComponentIcon> icons = this.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				foreach (ComponentIcon ci in icons)
				{
					ComponentIconLocal cil = ci as ComponentIconLocal;
					if (cil != null)
					{
						methodCode.Add("\t");
						methodCode.Add(cil.LocalPointer.CodeName);
						methodCode.Add("=");
						methodCode.Add(ValueTypeUtil.GetDefaultPhpScriptCodeByType(cil.LocalPointer.BaseClassType));
						methodCode.Add(";\r\n");
					}
				}
			}
		}
		private void finishAssemblyCompile(ILimnorCodeCompiler compiler, CodeMemberMethod method, bool bRet)
		{
			Dictionary<UInt32, MethodSegment> gotoBranches = CompilerUtil.GetGotoBranches(method);
			if (gotoBranches != null && gotoBranches.Count > 0)
			{
				if (!bRet)
				{
					//if the last method code is not a return then add a default method return
					//if at least one of the branches in the method (not including goBranches) is not ended (method return or go to)
					addDefaultMethodReturn(MainStatements);
				}
				foreach (KeyValuePair<UInt32, MethodSegment> kv in gotoBranches)
				{
					//if the last statement for this gotoBranch is not a method return then add a default one
					if (!kv.Value.Completed)
					{
						addDefaultMethodReturn(kv.Value.Statements);
					}
					//append the label to the end of method code
					MainStatements.Add(new CodeLabeledStatement(ActionBranch.IdToLabel(kv.Key)));
					if (kv.Value.Statements.Count == 0)
					{
						MainStatements.Add(new CodeSnippetStatement(";"));
					}
					//append the staments belonging to this gotoBranch to the end of the method
					for (int i = 0; i < kv.Value.Statements.Count; i++)
					{
						MainStatements.Add(kv.Value.Statements[i]);
					}
				}
			}
			List<ExceptionHandler> ehs = new List<ExceptionHandler>();
			foreach (ExceptionHandler eh in this.ExceptionHandlers)
			{
				bool isSub = false;
				for (int i = 0; i < ehs.Count; i++)
				{
					if (eh.IsSubExceptionOf(ehs[i]))
					{
						ehs.Insert(i, eh);
						isSub = true;
						break;
					}
				}
				if (!isSub)
				{
					ehs.Add(eh);
				}
			}
			//foreach (ExceptionHandler eh in this.ExceptionHandlers)
			for (int i = 0; i < ehs.Count; i++)
			{
				ExceptionHandler eh = ehs[i];
				if (!eh.IsDefaultExceptionType)
				{
					eh.EstablishObjectOwnership(this);
					CodeCatchClause ccc = new CodeCatchClause(eh.ExceptionObject.ClassPointer.CodeName);
					ccc.CatchExceptionType = new CodeTypeReference(eh.ExceptionType.TypeString);
					if (eh.ActionList != null && eh.ActionList.Count > 0)
					{
						List<ComponentIconSubscopeVariable> icons = eh.ComponentIconList;
						if (icons != null && icons.Count > 0)
						{
							foreach (ComponentIconSubscopeVariable ci in icons)
							{
								CodeVariableDeclarationStatement p = ci.LocalPointer.CreateVariableDeclaration();
								ccc.Statements.Add(p);
							}
						}
						eh.ActionList.ExportCode(compiler, method, ccc.Statements);
					}
					_tryCatchFinal.CatchClauses.Add(ccc);
				}
			}
			if (this.FinalActions.Actions != null && this.FinalActions.Actions.Count > 0)
			{
				FinalActions.EstablishObjectOwnership(this);
				List<ComponentIconSubscopeVariable> icons = FinalActions.ComponentIconList;
				if (icons != null && icons.Count > 0)
				{
					foreach (ComponentIconSubscopeVariable ci in icons)
					{
						CodeVariableDeclarationStatement p = ci.LocalPointer.CreateVariableDeclaration();
						_tryCatchFinal.FinallyStatements.Add(p);
					}
				}
				this.FinalActions.Actions.ExportCode(compiler, method, _tryCatchFinal.FinallyStatements);
			}
		}
		private void prepareAssemblyCompile(ILimnorCodeCompiler compiler, CodeMemberMethod method, bool isForWeb)
		{
			LoadActionInstances();
			if (_branchList != null && _branchList.Count > 0)
			{
				EstablishObjectOwnership();
				_branchList.SetActions(compiler.ActionEventList.GetActions());
				_branchList.FindoutActionThreads(true);
				if (isForWeb)
				{
					_branchList.CollectSourceValues(0);
				}
				if (_branchList.IsMultiThreads)
				{
					//declare local variables outside of the methos
					List<ComponentIcon> icons = this.ComponentIconList;
					if (icons != null && icons.Count > 0)
					{
						foreach (ComponentIcon ci in icons)
						{
							ComponentIconLocal cil = ci as ComponentIconLocal;
							if (cil != null)
							{
								Type t = cil.LocalPointer.GetResolvedDataType();
								if (t != null && !typeof(VoidAction).Equals(t))
								{
									CodeTypeReference ctf = cil.LocalPointer.GetCodeTypeReference();
									if (ctf != null)
									{
										CodeMemberField mf = new CodeMemberField(ctf, cil.LocalPointer.CodeName);
										CodeExpression ceini= ObjectCreationCodeGen.ObjectCreationCode(cil.LocalPointer.ObjectInstance);
										mf.InitExpression = ceini;
										compiler.TypeDeclaration.Members.Add(mf);
									}
								}
							}
						}
					}
				}
				else
				{
					//declare local variables within the method
					List<ComponentIcon> icons = this.ComponentIconList;
					if (icons != null && icons.Count > 0)
					{
						foreach (ComponentIcon ci in icons)
						{
							ComponentIconLocal cil = ci as ComponentIconLocal;
							if (cil != null)
							{
								ComponentIconException cie = cil as ComponentIconException;
								if (cie == null)
								{
									cil.LocalPointer.AddVariableDeclaration(method.Statements);
								}
							}
						}
					}
				}
				//add try/catch
				if (this.FinalActions.Actions.Count > 0 || !this.ExceptionHandlers.IsEmpty)
				{
					_tryCatchFinal = new CodeTryCatchFinallyStatement();
					method.Statements.Add(_tryCatchFinal);
				}
			}
			else
			{
				if (!this.NoReturn)
				{
					if (this.ReturnValue != null)
					{
						if (!typeof(void).Equals(this.ReturnValue.ObjectType))
						{
							if (this.ReturnValue.ObjectType.IsArray)
							{
								method.Statements.Add(
									new CodeMethodReturnStatement(
									new CodePrimitiveExpression(null)
									));
							}
							else
							{
								method.Statements.Add(
									new CodeMethodReturnStatement(
									ObjectCreationCodeGen.ObjectCreationCode(VPLUtil.GetDefaultValue(this.ReturnValue.ObjectType))
									));
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mc"></param>
		/// <param name="statements"></param>
		private void addDefaultMethodReturn(CodeStatementCollection statements)
		{
			if (statements.Count > 0)
			{
				//it should rely on MethodSegment.Completed
				//this checking is redundunt, just an added precausion. 
				CodeMethodReturnStatement rs;
				rs = statements[statements.Count - 1] as CodeMethodReturnStatement;
				if (rs == null)
				{
					CodeGotoStatement gs = statements[statements.Count - 1] as CodeGotoStatement;
					if (gs == null)
					{
						rs = new CodeMethodReturnStatement();
						if (ReturnValue != null && !ReturnValue.IsVoid)
						{
							rs.Expression = ObjectCreationCodeGen.GetDefaultValueExpression(ReturnValue.BaseClassType);
						}
						statements.Add(rs);
					}
				}
			}
		}
		private void addDefaultJsMethodReturn(StringCollection statements)
		{
			//remove this function because a javascript method is free to return or not return a value during runtime 
			if (statements.Count > 0)
			{
			}
		}
		private void addDefaultPhpMethodReturn(StringCollection statements)
		{
			if (statements.Count > 0)
			{
				//it should rely on MethodSegment.Completed
				//this checking is redundunt, just an added precausion. 
				string rs;
				rs = statements[statements.Count - 1];
				if (string.IsNullOrEmpty(rs) || !rs.StartsWith("return", StringComparison.Ordinal) || !rs.StartsWith("goto", StringComparison.Ordinal))
				{
					if (ReturnValue != null && !ReturnValue.IsVoid)
					{
						rs = string.Format(CultureInfo.InvariantCulture, "return {0};\r\n", VPLUtil.GetDefaultValue(ReturnValue.BaseClassType));
					}
					else
					{
						rs = "return;\r\n";
					}
					statements.Add(Indentation.GetIndent());
					statements.Add(rs);
				}
			}
		}
		/// <summary>
		/// generate js function using the js code
		/// </summary>
		/// <param name="jsCode">js code for the page</param>
		/// <param name="methodcode">js code forming the function</param>
		/// <param name="jmc">js function data</param>
		/// <param name="bRet">js code has finished</param>
		protected virtual void createJavascriptFunction(StringCollection jsCode, StringCollection methodcode, JsMethodCompiler jmc, bool bRet)
		{
			Dictionary<UInt32, JsMethodSegment> gotoBranches = jmc.GetGotoBranches();// CompilerUtil.GetGotoBranches(mm);
			if (gotoBranches != null && gotoBranches.Count > 0)
			{
				if (!bRet)
				{
					//if the last method code is not a return then add a default method return
					//if at least one of the branches in the method (not including goBranches) is not ended (method return or go to)
					addDefaultJsMethodReturn(methodcode);
				}
				foreach (KeyValuePair<UInt32, JsMethodSegment> kv in gotoBranches)
				{
					//if the last statement for this gotoBranch is not a method return then add a default one
					if (!kv.Value.Completed)
					{
						addDefaultJsMethodReturn(kv.Value.Statements);
					}
					string indent = Indentation.GetIndent();
					//append the label to the end of method code
					methodcode.Add(indent);
					methodcode.Add("function ");
					methodcode.Add(ActionBranch.IdToLabel(kv.Key));
					methodcode.Add("() {\r\n");
					//append the staments belonging to this gotoBranch to the end of the method
					for (int i = 0; i < kv.Value.Statements.Count; i++)
					{
						methodcode.Add(kv.Value.Statements[i]);
					}
					methodcode.Add(indent);
					methodcode.Add("}\r\n");
				}
			}
			if (!string.IsNullOrEmpty(Description))
			{
				jsCode.Add("/*\r\n");
				jsCode.Add(Description);
				if (Parameters != null && Parameters.Count > 0)
				{
					jsCode.Add("\r\nParametr 0:");
					jsCode.Add(Parameters[0].Name);
					jsCode.Add(" - ");
					jsCode.Add(Parameters[0].Description);
					jsCode.Add("\r\n");
					for (int i = 1; i < Parameters.Count; i++)
					{
						jsCode.Add("\r\nParametr ");
						jsCode.Add(i.ToString(CultureInfo.InvariantCulture));
						jsCode.Add(":");
						jsCode.Add(Parameters[i].Name);
						jsCode.Add(" - ");
						jsCode.Add(Parameters[i].Description);
						jsCode.Add("\r\n");
					}
				}
				jsCode.Add("\r\n*/\r\n");
			}
			jsCode.Add("\r\n");
			if (this.RootPointer.IsWebApp)
			{
				jsCode.Add(this.RootPointer.CodeName);
				jsCode.Add(".");
				jsCode.Add(Name);
				jsCode.Add("=function");
			}
			else
			{
				jsCode.Add("function ");
				jsCode.Add(Name);
			}
			//
			TraceLogClass.TraceLog.Trace("Generate {0} function parameter(s)", ParameterCount);
			TraceLogClass.TraceLog.IndentIncrement();
			//
			jsCode.Add("(");
			//
			if (Parameters != null && Parameters.Count > 0)
			{
				jsCode.Add(Parameters[0].Name);
				for (int i = 1; i < Parameters.Count; i++)
				{
					jsCode.Add(",");
					jsCode.Add(Parameters[i].Name);
				}
			}
			jsCode.Add(")\r\n{\r\n");
			TraceLogClass.TraceLog.IndentIncrement();
			for (int i = 0; i < methodcode.Count; i++)
			{
				jsCode.Add(methodcode[i]);
			}
			jsCode.Add("}\r\n");
			methodcode.Clear();
			TraceLogClass.TraceLog.IndentDecrement();
		}
		/// <summary>
		/// generate php function using the php code
		/// </summary>
		/// <param name="jsCode">php code for the page</param>
		/// <param name="methodcode">php code forming the function</param>
		/// <param name="jmc">php function data</param>
		/// <param name="bRet">php code has finished</param>
		private void createPhpScriptFunction(StringCollection jsCode, StringCollection methodcode, JsMethodCompiler jmc, bool bRet)
		{
			if (!string.IsNullOrEmpty(Description))
			{
				jsCode.Add("/*\r\n");
				jsCode.Add(Description);
				if (Parameters != null && Parameters.Count > 0)
				{
					jsCode.Add("\r\nParametr 0:");
					jsCode.Add(Parameters[0].Name);
					jsCode.Add(" - ");
					jsCode.Add(Parameters[0].Description);
					jsCode.Add("\r\n");
					for (int i = 1; i < Parameters.Count; i++)
					{
						jsCode.Add("\r\nParametr ");
						jsCode.Add(i.ToString(CultureInfo.InvariantCulture));
						jsCode.Add(":");
						jsCode.Add(Parameters[i].Name);
						jsCode.Add(" - ");
						jsCode.Add(Parameters[i].Description);
						jsCode.Add("\r\n");
					}
				}
				jsCode.Add("\r\n*/\r\n");
			}
			jsCode.Add("\r\npublic function ");
			jsCode.Add(Name);

			//
			TraceLogClass.TraceLog.Trace("Generate {0} function parameter(s)", ParameterCount);
			TraceLogClass.TraceLog.IndentIncrement();
			//
			jsCode.Add("(");
			//
			if (Parameters != null && Parameters.Count > 0)
			{
				jsCode.Add(Parameters[0].CodeName);
				for (int i = 1; i < Parameters.Count; i++)
				{
					jsCode.Add(",");
					jsCode.Add(Parameters[i].CodeName);
				}
			}
			jsCode.Add(") {\r\n");
			TraceLogClass.TraceLog.IndentIncrement();
			//
			StringCollection gotos = new StringCollection();
			Dictionary<UInt32, JsMethodSegment> gotoBranches = jmc.GetGotoBranches();
			if (gotoBranches != null && gotoBranches.Count > 0)
			{
				if (!bRet)
				{
					//if the last method code is not a return then add a default method return
					//if at least one of the branches in the method (not including goBranches) is not ended (method return or go to)
					addDefaultPhpMethodReturn(methodcode);
				}
				foreach (KeyValuePair<UInt32, JsMethodSegment> kv in gotoBranches)
				{
					//if the last statement for this gotoBranch is not a method return then add a default one
					if (!kv.Value.Completed)
					{
						addDefaultPhpMethodReturn(kv.Value.Statements);
					}
					string indent = Indentation.GetIndent();
					//append the label to the end of method code
					gotos.Add(indent);
					gotos.Add(indent);
					gotos.Add(ActionBranch.IdToLabel(kv.Key));
					gotos.Add(":\r\n");
					//append the staments belonging to this gotoBranch to the end of the method
					for (int i = 0; i < kv.Value.Statements.Count; i++)
					{
						gotos.Add(kv.Value.Statements[i]);
					}
				}
			}
			//
			for (int i = 0; i < methodcode.Count; i++)
			{
				jsCode.Add(methodcode[i]);
			}
			for (int i = 0; i < gotos.Count; i++)
			{
				jsCode.Add(gotos[i]);
			}
			jsCode.Add("}\r\n");
			TraceLogClass.TraceLog.IndentDecrement();
		}
		#endregion
		#region Methods
		public static void CollectActionsByOwnerType<T>(IAction a, List<IActionMethodPointer> results, List<UInt32> usedMethods)
		{
			if (a != null && a.ActionMethod != null)
			{
				if (ActionClass.CheckMethodOwnerType<T>(a))
				{
					results.Add(a.ActionMethod);
				}
				MethodClass mc = a.ActionMethod.MethodPointed as MethodClass;
				if (mc != null)
				{
					mc.FindActionsByOwnerType<T>(results, usedMethods);
				}
			}
		}
		public void FindActionsByOwnerType<T>(List<IActionMethodPointer> results, List<UInt32> usedMethods)
		{
			if (usedMethods.Contains(this.MethodID))
			{
				return;
			}
			usedMethods.Add(this.MethodID);
			if (_branchList != null)
			{
				_branchList.FindActionsByOwnerType<T>(results, usedMethods);
			}
		}
		public MethodInfoX GetMethodInfoX()
		{
			return new MethodInfoX(this);
		}
		public virtual void SetEditContext()
		{
			MethodEditContext.IsWebPage = this.IsWebPage;
			MethodEditContext.UseClientExecuterOnly = (this.WebUsage == EnumMethodWebUsage.Client);
			MethodEditContext.UseServerExecuterOnly = (this.WebUsage == EnumMethodWebUsage.Server);
		}
		public static void ClearEditorContext()
		{
			MethodEditContext.IsWebPage = false;
			MethodEditContext.UseClientExecuterOnly = false;
			MethodEditContext.UseClientPropertyOnly = false;
			MethodEditContext.UseServerExecuterOnly = false;
			MethodEditContext.UseServerPropertyOnly = false;
		}
		public void SetXmlNode(XmlNode node)
		{
			_xmlNode = node;
		}
		protected void FirePropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangeEventArg(name));
			}
		}
		protected XmlObjectWriter GetWriter()
		{
			if (_writer == null)
			{
				if (_reader != null && _reader.ObjectList != null)
				{
					_writer = new XmlObjectWriter(_reader.ObjectList);
				}
				if (_writer == null)
				{
					_writer = XmlSerializerUtility.ActiveWriter as XmlObjectWriter;
				}
			}
			return _writer;
		}
		/// <summary>
		/// set Owner property for all IObjectPointer instances and other types of instances such as actions and branches
		/// </summary>
		public void EstablishObjectOwnership()
		{
			//_owner should not be null because the constructor accepts it.
			//
			//_componentIconList
			//_attributes
			//_branchList
			//_finalActions
			//_exceptionHandlers
			//_parameters
			//_returnValue
			if (_returnValue != null)
			{
				_returnValue.Owner = this;
			}
			if (_parameters != null)
			{
				foreach (ParameterClass p in _parameters)
				{
					p.Owner = this;
				}
			}
			if (_attributes != null)
			{
				foreach (ConstObjectPointer c in _attributes)
				{
					c.Owner = this;
				}
			}
			if (_componentIconList != null)
			{
				foreach (ComponentIcon ci in _componentIconList)
				{
					ci.EstablishObjectOwnership(this);
				}
			}
			if (_branchList != null)
			{
				_branchList.EstablishObjectOwnership();
			}
			if (_finalActions != null)
			{
				_finalActions.EstablishObjectOwnership(this);
			}
			if (_exceptionHandlers != null)
			{
				foreach (ExceptionHandler eh in _exceptionHandlers)
				{
					if (!eh.IsDefaultExceptionType)
					{
						eh.EstablishObjectOwnership(this);
					}
				}
			}
		}
		public void RemoveExceptionCapture(ExceptionHandler eh)
		{
			if (eh.DataXmlNode != null)
			{
				if (eh.DataXmlNode.ParentNode != null)
				{
					eh.DataXmlNode.ParentNode.RemoveChild(eh.DataXmlNode);
				}
			}
			if (this.ExceptionHandlers != null)
			{
				foreach (ExceptionHandler e0 in this.ExceptionHandlers)
				{
					if (eh.IsSameException(e0))
					{
						if (!e0.IsDefaultExceptionType)
						{
							this.ExceptionHandlers.Remove(e0);
						}
						break;
					}
				}
			}
		}
		public void GetCustomMethods(List<MethodClass> list)
		{
			if (_branchList != null)
			{
				_branchList.GetCustomMethods(list);
			}
		}
		public void AddDownload(ISourceValuePointer p)
		{
			if (_downloads == null)
			{
				_downloads = new PropertyPointerList();
			}
			_downloads.AddPointer(p);
		}
		public void AddUpload(ISourceValuePointer p)
		{
			if (_uploads == null)
			{
				_uploads = new PropertyPointerList();
			}
			_uploads.AddPointer(p);
		}
		public void AddUploads(IList<ISourceValuePointer> list)
		{
			if (list != null && list.Count > 0)
			{
				foreach (ISourceValuePointer p in list)
				{
					AddUpload(p);
				}
			}
		}
		public void AddDownloads(IList<ISourceValuePointer> list)
		{
			if (list != null && list.Count > 0)
			{
				foreach (ISourceValuePointer p in list)
				{
					AddDownload(p);
				}
			}
		}
		public void AddValueSources(IList<ISourceValuePointer> list)
		{
			if (list != null && list.Count > 0)
			{
				foreach (ISourceValuePointer p in list)
				{
					if (p.IsWebClientValue())
					{
						AddUpload(p);
					}
					else
					{
						AddDownload(p);
					}
				}
			}
		}
		public void AddDeserializer(IPostDeserializeProcess obj)
		{
			if (_dependendDeserializers == null)
			{
				_dependendDeserializers = new List<IPostDeserializeProcess>();
			}
			foreach (IPostDeserializeProcess v in _dependendDeserializers)
			{
				if (v == obj)
				{
					return;
				}
			}
			_dependendDeserializers.Add(obj);
		}
		public void OnComponentRemoved(UInt32 componentId)
		{
			if (_componentIconList != null && _componentIconList.Count > 0)
			{
				foreach (ComponentIcon ci in _componentIconList)
				{
					if (ci.MemberId == componentId)
					{
						_componentIconList.Remove(ci);
						break;
					}
				}
			}
		}

		public IAction CreateAction(ILimnorDesignerLoader loader, IMethod scopeMethod, IActionsHolder holder, Form caller)
		{
			ClassPointer root = loader.GetRootId();
			ActionClass act = new ActionClass(this.RootPointer);
			act.ActionMethod = CreatePointer(root);
			act.ActionName = act.ActionMethod.DefaultActionName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			act.ActionHolder = holder;
			if (root.CreateNewAction(act, loader.Writer, scopeMethod, caller))
			{
				return act;
			}
			return null;
		}
		public void RemoveWebAttribute()
		{
			if (_attributes != null)
			{
				List<ConstObjectPointer> lst = new List<ConstObjectPointer>();
				foreach (ConstObjectPointer a in _attributes)
				{
					if (typeof(WebMethodAttribute).Equals(a.BaseClassType))
					{
						lst.Add(a);
					}
				}
				foreach (ConstObjectPointer a in lst)
				{
					RemoveAttribute(a);
				}
			}
		}
		public void AddWebAttribute()
		{
			if (_attributes == null)
			{
				_attributes = new List<ConstObjectPointer>();
			}
			foreach (ConstObjectPointer a in _attributes)
			{
				if (typeof(WebMethodAttribute).Equals(a.BaseClassType))
				{
					return;
				}
			}
			ConstObjectPointer cop = new ConstObjectPointer(new TypePointer(typeof(WebMethodAttribute)));
			ConstructorInfo cif = typeof(WebMethodAttribute).GetConstructor(Type.EmptyTypes);
			cop.SetValue(ConstObjectPointer.VALUE_Constructor, cif);
			AddAttribute(cop);
		}
		public MethodClassInherited CreateInherited(ClassPointer owner)
		{
			MethodClassInherited mi = new MethodClassInherited(this.RootPointer);
			mi._desc = _desc;
			mi._isFinal = _isFinal;
			mi._name = _name;
			mi._parameters = _parameters;
			mi._returnValue = _returnValue;
			mi._holder = owner;
			mi.AccessControl = AccessControl;
			mi.MemberId = _memberId;
			mi.SetDeclarer(owner);
			return mi;
		}
		/// <summary>
		/// when this is MethodOverride, call it to update it when base method changed
		/// </summary>
		/// <param name="baseMethod"></param>
		public virtual void SetMembers(MethodClass baseMethod)
		{
			_name = baseMethod.Name;
			_parameters = baseMethod.Parameters;
			_returnValue = baseMethod.ReturnValue;
			AccessControl = baseMethod.AccessControl;
		}
		public virtual bool ReloadMethod(ILimnorDesignerLoader loader)
		{
			try
			{
				ReloadFromXmlNode();
				return true;
			}
			catch (Exception err)
			{
				MathNode.Log(TraceLogClass.MainForm,err);
			}
			return false;
		}
		public virtual void RemoveMethodXmlNode(XmlNode rootNode)
		{
			XmlNode nodeMethod = rootNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}[@{2}='{3}']",
					XmlTags.XML_METHODS, XmlTags.XML_METHOD, XmlTags.XMLATT_MethodID, MemberId));
			if (nodeMethod != null)
			{
				XmlNode p = nodeMethod.ParentNode;
				p.RemoveChild(nodeMethod);
			}
		}
		/// <summary>
		/// set id and name for a new method
		/// </summary>
		/// <param name="rootNode"></param>
		/// <param name="method"></param>
		protected virtual void InitializeNewMethod(XmlNode rootNode, ILimnorDesignerLoader loader)
		{
			MemberId = (UInt32)(Guid.NewGuid().GetHashCode());
			_name = loader.CreateMethodName(Name, null);
		}
		public virtual DlgMethod CreateSubMethodEditor(Type designerType, Rectangle rcStart, MethodDiagramViewer parentEditor, UInt32 scopeId)
		{
			if (this.RootPointer == null)
			{
				throw new DesignerException("Cannot create sub method editor for method {0}. RootPointer is null", this.MemberId);
			}
			ILimnorDesignerLoader loader = this.RootPointer.GetCurrentLoader();
			if (loader == null)
			{
				throw new DesignerException("Cannot create method editor for method {0}. The class is not in design mode", this.MemberId);
			}
			DlgMethod dlg = new DlgMethod(loader, designerType, rcStart, parentEditor, scopeId);
			return dlg;
		}
		public virtual DlgMethod CreateBlockScopeMethodEditor(Rectangle rcStart, UInt32 scopeId)
		{
			DlgMethod dlg = CreateMethodEditor(rcStart);
			dlg.SetNameReadOnly();
			dlg.SetAttributesReadOnly(true);
			dlg.SetForSubScope();
			dlg.SetSubScopeId(scopeId);
			return dlg;
		}
		public virtual DlgMethod CreateMethodEditor(Rectangle rcStart)
		{
			if (this.RootPointer == null)
			{
				throw new DesignerException("Cannot create method editor for method {0}. RootPointer is null", this.MemberId);
			}
			ILimnorDesignerLoader loader = this.RootPointer.GetCurrentLoader();
			if (loader == null)
			{
				throw new DesignerException("Cannot create method editor for method {0}. The class is not in design mode", this.MemberId);
			}
			if (rcStart == Rectangle.Empty)
			{
				rcStart = new Rectangle(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 60, 30);
			}
			DlgMethod dlg = new DlgMethod(loader, rcStart, this.MemberId);
			if (MemberId == 0)
			{
				if (string.IsNullOrEmpty(_name))
				{
					_name = "Method";
				}
				InitializeNewMethod(loader.Node, loader);//create ID and Name
			}
			this.RootPointer.MethodInEditing = this;
			this.CurrentEditor = dlg.GetEditor();
			return dlg;
		}
		public virtual void ExitEditor()
		{
			this.CurrentEditor = null;
			this.RootPointer.MethodInEditing = null;
			ClearEditorContext();
		}
		private EnumRunContext _origiContext = EnumRunContext.Server;
		public virtual bool Edit(UInt32 actionBranchId, Rectangle rcStart, ILimnorDesignerLoader loader, Form caller)
		{
			if (Owner == null)
			{
				Owner = loader.GetRootId();
			}
			try
			{
				_origiContext = VPLUtil.CurrentRunContext;
				if (loader.Project.IsWebApplication)
				{
					if (this.RunAt == EnumWebRunAt.Client)
					{
						VPLUtil.CurrentRunContext = EnumRunContext.Client;
					}
					else
					{
						VPLUtil.CurrentRunContext = EnumRunContext.Server;
					}
				}
				else
				{
					VPLUtil.CurrentRunContext = EnumRunContext.Server;
				}
				DlgMethod dlg = this.CreateMethodEditor(rcStart);
				dlg.LoadMethod(this, this.ParameterEditType);
				if (dlg.ShowDialog(caller) == DialogResult.OK)
				{
					_display = null;
					IsNewMethod = false;
					loader.GetRootId().SaveMethod(this, null);
					return true;
				}
			}
			catch (Exception err)
			{
				MathNode.Log(caller,err);
			}
			finally
			{
				ExitEditor();
				VPLUtil.CurrentRunContext = _origiContext;
			}
			return false;
		}
		public void SetHolder(IClass holder)
		{
			_holder = holder;
		}
		public int GetParameterIndexByName(string name)
		{
			if (_parameters != null)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (_parameters[i].Name == name)
					{
						return i;
					}
				}
			}
			return -1;
		}
		public CustomMethodPointer CreatePointer(IClass holder)
		{
			return new CustomMethodPointer(this, holder);
		}
		public void AddVariable(ComponentIconLocal v)
		{
			if (_componentIconList == null)
				_componentIconList = new List<ComponentIcon>();
			_componentIconList.Add(v);
		}
		public List<ComponentIconLocal> GetLocalVariables()
		{
			List<ComponentIconLocal> vs = new List<ComponentIconLocal>();
			if (_componentIconList != null)
			{
				foreach (ComponentIcon c in _componentIconList)
				{
					ComponentIconLocal l = c as ComponentIconLocal;
					if (l != null)
					{
						vs.Add(l);
					}
				}
			}
			return vs;
		}
		public LocalVariable GetLocalVariable(UInt32 id)
		{
			if (_componentIconList != null)
			{
				foreach (ComponentIcon c in _componentIconList)
				{
					ComponentIconLocal l = c as ComponentIconLocal;
					if (l != null && l.LocalPointer != null)
					{
						if (l.LocalPointer.MemberId == id)
						{
							return l.LocalPointer;
						}
					}
				}
			}
			if (_parameters != null)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (_parameters[i].ParameterID == id)
					{
						if (_owner != null)
							return new MethodParamVariable(_parameters[i], _owner.ClassId);
						else
							return new MethodParamVariable(_parameters[i], 0);
					}
				}
			}
			if (_branchList != null)
			{
				LocalVariable lv = _branchList.GetLocalVariable(id);
				if (lv != null)
				{
					return lv;
				}
			}
			foreach (ExceptionHandler eh in this.ExceptionHandlers)
			{
				if (!eh.IsDefaultExceptionType)
				{
					if (eh.ExceptionObject.MemberId == id)
					{
						return (LocalVariable)(eh.ExceptionObject.ClassPointer);
					}
					if (eh.ComponentIconList != null)
					{
						foreach (ComponentIconSubscopeVariable sv in eh.ComponentIconList)
						{
							if (sv.LocalPointer != null && sv.LocalPointer.MemberId == id)
							{
								return sv.LocalPointer;
							}
						}
					}
				}
			}
			if (FinalActions != null)
			{
				if (FinalActions.ComponentIconList != null)
				{
					foreach (ComponentIconSubscopeVariable sv in FinalActions.ComponentIconList)
					{
						if (sv.LocalPointer != null && sv.LocalPointer.MemberId == id)
						{
							return sv.LocalPointer;
						}
					}
				}
			}
			return null;
		}
		public List<UInt32> GetActionIDs()
		{
			if (_branchList != null)
			{
				return _branchList.GetActionIDs();
			}
			return null;
		}
		public List<IAction> GetActions()
		{
			if (_branchList != null)
			{
				return _branchList.GetActions();
			}
			return null;
		}
		public IAction GetActionById(UInt32 actId)
		{
			if (_branchList != null)
			{
				return _branchList.GetActionById(actId);
			}
			return null;
		}
		public bool IsActionUsed(UInt32 actId)
		{
			if (_branchList != null)
			{
				return _branchList.IsActionUsed(actId);
			}
			return false;
		}
		public void GetActionsUseLocalVariable(UInt32 varId, Dictionary<UInt32, IAction> actions)
		{
			if (_branchList != null)
			{
				_branchList.GetActionsUseLocalVariable(varId, actions);
			}
		}
		public void ReplaceAction(UInt32 oldId, IAction newAct)
		{
			if (_branchList != null)
			{
				_branchList.ReplaceAction(oldId, newAct);
			}
		}
		public void ResetGroupId(UInt32 groupId)
		{
			this.MethodID = groupId;
		}
		public ActionBranch GetNextBranchById(UInt32 id)
		{
			if (_branchList != null)
			{
				for (int i = 0; i < _branchList.Count; i++)
				{
					if (_branchList[i].BranchId == id)
					{
						if (i < _branchList.Count - 1)
						{
							return _branchList[i + 1];
						}
						else
						{
							return null;
						}
					}
					IActionGroup g = _branchList[i] as IActionGroup;
					if (g != null)
					{
						if (g.ActionList != null)
						{
							if (g.ActionList.GetBranchById(id) != null)
							{
								return g.GetNextBranchById(id);
							}
						}
					}
					g = this;
					if (_branchList[i].GetBranchInGroup(id, ref g) != null)
					{
						if (g == this)
						{
							if (i < _branchList.Count - 1)
							{
								return _branchList[i + 1];
							}
							else
							{
								return null;
							}
						}
						else
						{
							return g.GetNextBranchById(id);
						}
					}
				}
			}
			return null;
		}
		public ActionBranch GetBranchById(UInt32 id)
		{
			if (_branchList != null)
			{
				foreach (ActionBranch a in _branchList)
				{
					if (a.BranchId == id)
					{
						return a;
					}
				}
			}
			return null;
		}
		public ActionBranch SearchBranchById(UInt32 id)
		{
			MethodDesignerHolder editor = CurrentEditor;
			if (editor != null)
			{
				ActionBranch ab = editor.SearchBranchById(id);
				if (ab != null)
				{
					return ab;
				}
			}
			if (_branchList != null)
			{
				foreach (ActionBranch a in _branchList)
				{
					ActionBranch ab = a.SearchBranchById(id);
					if (ab != null)
					{
						return ab;
					}
				}
			}
			return null;
		}
		public ActionBranch GetBranchByIdInGroup(UInt32 id, ref IActionGroup group)
		{
			if (_branchList != null)
			{
				group = this;
				return _branchList.GetBranchInGroup(id, ref group);
			}
			return null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="compiler"></param>
		/// <param name="method"></param>
		/// <returns>true:all barnches of the main thread have method return or goto; false: at least one branch of the main thread does not have method return or goto</returns>
		public void ExportCode(ILimnorCodeCompiler compiler, CodeMemberMethod method, bool isForWeb)
		{
			prepareAssemblyCompile(compiler, method, isForWeb);
			if (_branchList != null && _branchList.Count > 0)
			{
				bool bRet = _branchList.ExportCode(compiler, method, this.MainStatements);
				//
				finishAssemblyCompile(compiler, method, bRet);
				//
			}
		}
		public void ExportJavaScriptCode(StringCollection jsCode, StringCollection methodCode, JsMethodCompiler data)
		{
			bool bRet = false;
			if (Indentation.GetIndentation() <= 0)
			{
				Indentation.SetIndentation(1);
			}
			if (_branchList != null && _branchList.Count > 0)
			{
				_branchList.SetActions(data.ActionEventList.GetActions());
				_branchList.FindoutActionThreads(true);
				_downloads = null;
				_uploads = null;
				_branchList.CollectSourceValues(0);
				prepareJavascriptCompile(methodCode);
				//
				bRet = _branchList.ExportJavaScriptCode(jsCode, methodCode, data);
			}
			//
			createJavascriptFunction(jsCode, methodCode, data, bRet);
		}
		public void ExportPhpScriptCode(StringCollection phpCode, StringCollection methodCode, JsMethodCompiler data)
		{
			bool bRet = false;
			if (Indentation.GetIndentation() <= 0)
			{
				Indentation.SetIndentation(1);
			}
			StringCollection funcCode = new StringCollection();
			if (_branchList != null && _branchList.Count > 0)
			{
				_branchList.SetActions(data.ActionEventList.GetActions());
				_branchList.FindoutActionThreads(true);
				_downloads = null;
				_uploads = null;
				_branchList.CollectSourceValues(0);
				preparePhpScriptCompile(funcCode);
				//

				bRet = _branchList.ExportPhpScriptCode(phpCode, funcCode, data);
			}
			//
			if (this is IAction)
			{
				foreach (string s in funcCode)
				{
					methodCode.Add(s);
				}
			}
			else
			{
				createPhpScriptFunction(methodCode, funcCode, data, bRet);
			}
		}
		public bool UseClientValues()
		{
			if (_branchList != null && _branchList.Count > 0)
			{
				return _branchList.UseClientValues();
			}
			return false;
		}
		public bool UseServerValues()
		{
			if (_branchList != null && _branchList.Count > 0)
			{
				return _branchList.UseServerValues();
			}
			return false;
		}
		private bool _callingSetActions = false;
		public void SetActions(Dictionary<UInt32, IAction> actions)
		{
			if (!_callingSetActions)
			{
				_callingSetActions = true;
				if (_branchList != null)
				{
					_branchList.SetActions(actions);
					if (_acts != null)
					{
						_branchList.SetActions(_acts);
					}
				}
				_callingSetActions = false;
			}
		}
		public void CollectSourceValues(UInt32 taskid)
		{
			if (_branchList != null && _branchList.Count > 0)
			{
				_downloads = null;
				_uploads = null;
				_branchList.CollectSourceValues(taskid);
			}
		}
		public EnumWebValueSources CheckValueUsages()
		{
			EnumWebValueSources ret = EnumWebValueSources.Unknown;
			CollectSourceValues(0);
			if (_downloads != null && _downloads.Count > 0)
			{
				ret = EnumWebValueSources.HasServerValues;
				if (_uploads != null && _uploads.Count > 0)
				{
					ret = EnumWebValueSources.HasBothValues;
				}
			}
			else
			{
				if (_uploads != null && _uploads.Count > 0)
				{
					ret = EnumWebValueSources.HasClientValues;
				}
			}
			return ret;
		}
		public void Execute(List<ParameterClass> eventParameters)
		{
			if (_branchList != null)
			{
				_branchList.Execute(eventParameters);
			}
		}
		public string GetMethodSignature()
		{
			StringBuilder sb = new StringBuilder();
			if (string.IsNullOrEmpty(_name))
				sb.Append("?");
			else
				sb.Append(_name);
			if ((_parameters != null && _parameters.Count > 0) || (_returnValue != null && !_returnValue.IsVoid))
			{
				sb.Append("(");
				if (_parameters != null && _parameters.Count > 0)
				{
					sb.Append(_parameters[0].TypeName);
					for (int i = 1; i < _parameters.Count; i++)
					{
						sb.Append(",");
						sb.Append(_parameters[i].TypeName);
					}
				}
				sb.Append(")");
				if (_returnValue != null && !_returnValue.IsVoid)
				{
					sb.Append(_returnValue.TypeName);
				}
			}
			return sb.ToString();
		}
		public void RemoveParameter(UInt32 id)
		{
			if (_parameters != null)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (_parameters[i].ParameterID == id)
					{
						_parameters.RemoveAt(i);
						break;
					}
				}
			}
		}
		public void RemoveLocalVariable(ComponentIconLocal v)
		{
			this.ComponentIconList.Remove(v);
			if (v.ClassPointer != null)
			{
				ComponentIconLocal cx = null;
				foreach (ComponentIcon c in this.ComponentIconList)
				{
					ComponentIconLocal c0 = c as ComponentIconLocal;
					if (c0 != null && c0.ClassPointer != null)
					{
						if (c0.ClassPointer.IsSameObjectRef(v.ClassPointer))
						{
							cx = c0;
							break;
						}
					}
				}
				if (cx != null)
				{
					this.ComponentIconList.Remove(cx);
				}
			}
			//
			//remove from sub-scope
			//
			BranchList bh = this.ActionList;
			if (bh != null)
			{
				foreach (ActionBranch ab in bh)
				{
					AB_Squential abs = ab as AB_Squential;
					if (abs != null)
					{
						List<UInt32> used = new List<uint>();
						abs.RemoveLocalVariable(v, used);
					}
				}
			}
		}
		public ParameterClass GetParameterByID(UInt32 id)
		{
			if (_parameters != null)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (_parameters[i].ParameterID == id)
					{
						return _parameters[i];
					}
				}
			}
			if (_subMethods != null)
			{
				Stack<IMethod0>.Enumerator en = _subMethods.GetEnumerator();
				while (en.MoveNext())
				{
					if (en.Current.MethodParameterTypes != null)
					{
						foreach (IParameter p in en.Current.MethodParameterTypes)
						{
							if (p.ParameterID == id)
							{
								ParameterClass pc = p as ParameterClass;
								if (pc != null)
								{
									return pc;
								}
								SubMethodInfoPointer sp = en.Current as SubMethodInfoPointer;
								if (sp != null)
								{
									pc = sp.GetParameterType(id) as ParameterClass;
									if (pc != null)
									{
										return pc;
									}
									pc = sp.GetParameterType(p.Name) as ParameterClass;
									if (pc != null)
									{
										return pc;
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		public ParameterClass GetParameterByID(string name)
		{
			if (_parameters != null)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (string.Compare(name, _parameters[i].Name, StringComparison.Ordinal) == 0)
					{
						return _parameters[i];
					}
				}
			}
			if (_subMethods != null)
			{
				Stack<IMethod0>.Enumerator en = _subMethods.GetEnumerator();
				while (en.MoveNext())
				{
					if (en.Current.MethodParameterTypes != null)
					{
						foreach (IParameter p in en.Current.MethodParameterTypes)
						{
							if (string.Compare(p.Name, name, StringComparison.Ordinal) == 0)
							{
								ParameterClass pc = p as ParameterClass;
								if (pc != null)
								{
									return pc;
								}
								SubMethodInfoPointer sp = en.Current as SubMethodInfoPointer;
								if (sp != null)
								{

									pc = sp.GetParameterType(p.Name) as ParameterClass;
									if (pc != null)
									{
										return pc;
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		public ParameterClass GetParameterByName(string name)
		{
			if (_parameters != null)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (_parameters[i].Name == name)
					{
						return _parameters[i];
					}
				}
			}
			if (_subMethods != null)
			{
				Stack<IMethod0>.Enumerator en = _subMethods.GetEnumerator();
				while (en.MoveNext())
				{
					if (en.Current.MethodParameterTypes != null)
					{
						foreach (IParameter p in en.Current.MethodParameterTypes)
						{
							if (string.Compare(p.Name, name, StringComparison.Ordinal) == 0)
							{
								ParameterClass pc = p as ParameterClass;
								if (pc != null)
								{
									return pc;
								}
								SubMethodInfoPointer sp = en.Current as SubMethodInfoPointer;
								if (sp != null)
								{

									pc = sp.GetParameterType(p.Name) as ParameterClass;
									if (pc != null)
									{
										return pc;
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		public string GetParameterCodeNameById(UInt32 id)
		{
			ParameterClass p = GetParameterByID(id);
			if (p != null)
			{
				return p.Name;
			}
			return "";
		}
		public ParameterValue[] CreateParameterValues(IActionContext act, IObjectPointer owner)
		{
			ParameterValue[] ps = null;
			if (_parameters != null)
			{
				ps = new ParameterValue[_parameters.Count];
				for (int i = 0; i < _parameters.Count; i++)
				{
					ps[i] = new ParameterValue(act);
					ps[i].ParameterID = _parameters[i].ParameterID;
					ps[i].Name = _parameters[i].Name;
					ps[i].SetParameterType(_parameters[i]);
					ps[i].Property = new PropertyPointer();
					ps[i].Property.Owner = owner;
					ps[i].ValueType = EnumValueType.ConstantValue;
				}
			}
			return ps;
		}
		internal ParameterClass AddNewParameter(IObjectPointer dataType)
		{
			if (_parameters == null)
			{
				_parameters = new List<ParameterClass>();
			}
			ParameterClass p = new ParameterClass(this);
			p.SetDataType(dataType);
			p.ParameterID = (UInt32)(Guid.NewGuid().GetHashCode());
			string baseName = "Parameter";
			int n = 1;
			p.Name = baseName + n.ToString();
			while (true)
			{
				bool found = false;
				foreach (ParameterClass p0 in _parameters)
				{
					if (p0.Name == p.Name)
					{
						found = true;
						n++;
						p.Name = baseName + n.ToString();
						break;
					}
				}
				if (!found)
					break;
			}
			_parameters.Add(p);
			return p;
		}

		public IActionGroup GetThreadGroup(UInt32 branchId)
		{
			if (_branchList != null)
			{
				return _branchList.GetThreadGroup(branchId);
			}
			return null;
		}
		public void SetCompilerData(object c)
		{
			_compiler = c;
		}
		public void NotifyPropertyChange(object sender, EventArgs e)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, e);
			}
		}
		public DataTypePointer GetConcreteType(Type type)
		{
			List<IAction> acts = GetActions();
			if (acts != null)
			{
				foreach (IAction a in acts)
				{
					ActionClass act = a as ActionClass;
					if (act != null)
					{
						MethodPointer mp = act.ActionMethod as MethodPointer;
						if (mp != null)
						{
							DataTypePointer dp = mp.GetConcreteType(type);
							if (dp != null)
							{
								return dp;
							}
						}
					}
				}
			}
			return null;
		}
		#endregion
		#region Protected Methods
		protected virtual void OnAfterRead()
		{
		}
		protected virtual void OnBeforeRead()
		{
		}
		protected virtual PropertyDescriptor OnGettingPropertyDescriptor(PropertyDescriptor p, AttributeCollection attrs)
		{
			return p;
		}
		#endregion
		#region Web compile
		public void ResetCompile()
		{
		}
		public IList<IAction> FindCustomActions()
		{
			List<IAction> acts = GetActions();
			List<IAction> cacts = new List<IAction>();
			foreach (IAction a in acts)
			{
				if (a != null)
				{
					if (a.ActionMethod is CustomMethodPointer)
					{
						cacts.Add(a);
					}
				}
			}
			return cacts;
		}
		public string CheckRecursion(List<IAction> list)
		{
			IList<IAction> acts = FindCustomActions();
			foreach (IAction a in acts)
			{
				foreach (IAction a0 in list)
				{
					if (a0.ActionId == a.ActionId)
					{
						return string.Format(CultureInfo.InvariantCulture,
							"Recursion is not supported in a crossing client-server execution to avoid deny-of-service to web server. action {0}({1}) -> action {2}({3}) form a recursion", a0.ActionName, a0.ActionMethod.MethodName, a.ActionName, a.ActionMethod.MethodName);
					}
				}
			}
			foreach (IAction a in acts)
			{
				bool b = false;
				foreach (IAction a0 in list)
				{
					if (a0.ActionId == a.ActionId)
					{
						b = true;
						break;
					}
				}
				if (!b)
				{
					list.Add(a);
				}
			}
			foreach (IAction a in acts)
			{
				CustomMethodPointer cmp = a.ActionMethod as CustomMethodPointer;
				MethodClass mc = cmp.MethodPointed as MethodClass;
				string s = mc.CheckRecursion(list);
				if (!string.IsNullOrEmpty(s))
				{
					return s;
				}
			}
			return null;
		}
		#endregion
		#region Properties

		[Browsable(false)]
		protected virtual string XmlTag
		{
			get
			{
				return XmlTags.XML_METHOD;
			}
		}
		[Browsable(false)]
		public IList<ISourceValuePointer> UploadProperties
		{
			get
			{
				return _uploads;
			}
		}
		[Browsable(false)]
		public IList<ISourceValuePointer> DownloadProperties
		{
			get
			{
				return _downloads;
			}
		}
		/// <summary>
		/// a web service method exposed for external usages
		/// </summary>
		[Browsable(false)]
		public bool IsWebMethod
		{
			get
			{
				List<ConstObjectPointer> attrs = GetCustomAttributeList();
				if (attrs != null)
				{
					foreach (ConstObjectPointer a in attrs)
					{
						if (typeof(WebMethodAttribute).IsAssignableFrom(a.BaseClassType))
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		[Browsable(false)]
		public bool IsRemotingServiceMethod
		{
			get
			{
				List<ConstObjectPointer> attrs = GetCustomAttributeList();
				if (attrs != null)
				{
					foreach (ConstObjectPointer a in attrs)
					{
						if (typeof(OperationContractAttribute).IsAssignableFrom(a.BaseClassType))
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		[Browsable(false)]
		public bool IsWebPage
		{
			get
			{
				return _owner.IsWebPage;
			}
		}
		public virtual EnumWebRunAt RunAt
		{
			get
			{
				if (WebUsage == EnumMethodWebUsage.Client)
				{
					return EnumWebRunAt.Client;
				}
				if (WebUsage == EnumMethodWebUsage.Server)
				{
					return EnumWebRunAt.Server;
				}
				return EnumWebRunAt.Inherit;
			}
		}
		/// <summary>
		/// for a web page, indicate it is a client or a server
		/// </summry>
		[DefaultValue(EnumMethodWebUsage.Server)]
		[Browsable(false)]
		public virtual EnumMethodWebUsage WebUsage
		{
			get
			{
				return _webUsage;
			}
			set
			{
				_webUsage = value;
			}
		}
		[Browsable(false)]
		public virtual bool UseClientPropertyOnly
		{
			get
			{
				if (this.IsWebPage)
					return (this.WebUsage == EnumMethodWebUsage.Client);
				return false;
			}
		}

		[Browsable(false)]
		public IActionsHolder CurrentActionsHolder
		{
			get
			{
				MethodDesignerHolder editor = CurrentEditor;
				if (editor != null)
				{
					return editor.ActionsHolder;
				}
				return null;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public MethodDesignerHolder CurrentEditor
		{
			get
			{
				if (_editors != null)
				{
					MethodDesignerHolder editor;
					if (_editors.TryGetValue(this.MemberId, out editor))
					{
						return editor;
					}
				}
				return null;
			}
			set
			{
				if (value == null)
				{
					if (_editors != null)
					{
						if (_editors.ContainsKey(this.MemberId))
						{
							_editors.Remove(this.MemberId);
						}
					}
					if (_currentSubEditors != null)
					{
						if (_currentSubEditors.ContainsKey(this.MemberId))
						{
							_currentSubEditors.Remove(this.MemberId);
						}
					}
				}
				else
				{
					if (_editors == null)
					{
						_editors = new Dictionary<uint, MethodDesignerHolder>();
					}
					if (_editors.ContainsKey(this.MemberId))
					{
						_editors[this.MemberId] = value;
					}
					else
					{
						_editors.Add(this.MemberId, value);
					}
					if (_currentSubEditors != null)
					{
					}
				}
			}
		}

		[Browsable(false)]
		[ReadOnly(true)]
		public MethodDesignerHolder CurrentSubEditor
		{
			get
			{
				if (_currentSubEditors != null)
				{
					Stack<MethodDesignerHolder> editors;
					if (_currentSubEditors.TryGetValue(this.MemberId, out editors))
					{
						if (editors.Count > 0)
						{
							return editors.Peek();
						}
					}
				}
				return null;
			}
			set
			{
				if (value == null)
				{
					if (_currentSubEditors != null)
					{
						Stack<MethodDesignerHolder> editors;
						if (_currentSubEditors.TryGetValue(this.MemberId, out editors))
						{
							if (editors.Count > 0)
							{
								editors.Pop();
							}
						}
					}
				}
				else
				{
					if (_currentSubEditors == null)
					{
						_currentSubEditors = new Dictionary<uint, Stack<MethodDesignerHolder>>();
					}
					Stack<MethodDesignerHolder> editors;
					if (!_currentSubEditors.TryGetValue(this.MemberId, out editors))
					{
						editors = new Stack<MethodDesignerHolder>();
						_currentSubEditors.Add(this.MemberId, editors);
					}
					editors.Push(value);
				}
			}
		}
		[DefaultValue(false)]
		[Description("If this property is True then Control updating actions are executed under UI thread.")]
		public virtual bool MakeUIThreadSafe
		{
			get;
			set;
		}

		[Browsable(false)]
		[DefaultValue(EnumAccessControl.Public)]
		[Description("Public: all objects can access it; Protected: only this class and its derived classes can access it; Private: only this class can access it.")]
		public virtual EnumAccessControl AccessControl
		{
			get;
			set;
		}
		[Browsable(false)]
		public virtual bool HasBaseImplementation
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public virtual bool Implemented
		{
			get
			{
				return true;
			}
		}
		[Browsable(false)]
		public virtual bool IsOverride
		{
			get { return false; }
		}
		[Browsable(false)]
		public virtual bool DoNotCompile
		{
			get
			{
				if (!Implemented)
					return true;
				return false;
			}
		}
		[Browsable(false)]
		public virtual MemberAttributes MethodAttributes
		{
			get
			{
				MemberAttributes a;
				if (this.AccessControl == EnumAccessControl.Public)
				{
					a = MemberAttributes.Public;
				}
				else if (this.AccessControl == EnumAccessControl.Private)
				{
					a = MemberAttributes.Private;
				}
				else
				{
					a = MemberAttributes.Family;
				}
				if (IsStatic)
				{
					a |= MemberAttributes.Static;
				}
				else
				{
					if (IsAbstract)
					{
						a |= MemberAttributes.Abstract;
					}
					if (!IsFinal)
					{
						a |= MemberAttributes.VTableMask;
					}
				}
				return a;
			}
		}
		[DefaultValue(false)]
		[Browsable(false)]//hide it for this version
		public virtual bool IsAbstract
		{
			get;
			set;
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public bool IsNewMethod { get; set; }
		[DefaultValue(false)]
		[Browsable(false)]
		public bool GroupFinished { get; set; }
		[Browsable(false)]
		public bool IsSubGroup
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public UInt32 GroupId
		{
			get
			{
				return (UInt32)this.MethodID;
			}
		}
		[Browsable(false)]
		public string GroupName
		{
			get
			{
				return string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"Method {0}", Name);
			}
		}
		[Browsable(false)]
		public LimnorProject Project
		{
			get
			{
				if (_prj != null)
				{
					return _prj;
				}
				if (_owner != null)
				{
					if (_owner.ObjectList != null)
					{
						return _owner.ObjectList.Project;
					}
				}
				return null;
			}
		}
		[Browsable(false)]
		public XmlNode RootXmlNode
		{
			get
			{
				return _owner.XmlData;
			}
		}
		[Browsable(false)]
		public Rectangle EditorBounds
		{
			get;
			set;
		}
		[Browsable(false)]
		public int ViewerWidth
		{
			get;
			set;
		}
		[Browsable(false)]
		public int ViewerHeight
		{
			get;
			set;
		}
		[Browsable(false)]
		public int IconsHolderWidth
		{
			get;
			set;
		}

		[Browsable(false)]
		[ReadOnly(true)]
		public bool Changed { get; set; }

		[DefaultValue(false)]
		[Description("A static method is executed by the class, not by an instance of the class")]
		public virtual bool IsStatic
		{
			get
			{
				return _isStatic;
			}
			set
			{
				if (_isStatic != value)
				{
					_isStatic = value;
					if (PropertyChanged != null)
					{
						PropertyChanged(this, new PropertyChangeEventArg("IsStatic"));
					}
				}
			}
		}

		[DefaultValue(false)]
		[Browsable(false)]//hide it for this version
		[Description("A final method cannot be overriden by derived classes. A static method is always final.")]
		public bool IsFinal
		{
			get
			{
				return _isFinal;
			}
			set
			{
				if (_isFinal != value)
				{
					_isFinal = value;
					if (PropertyChanged != null)
					{
						PropertyChanged(this, new PropertyChangeEventArg("IsFinal"));
					}
				}
			}
		}
		[Description("Description of the method. It will also be the comment of the method when code is generated")]
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
					if (PropertyChanged != null)
					{
						PropertyChanged(this, new PropertyChangeEventArg("Description"));
					}
				}
			}
		}
		/// <summary>
		/// actions are loaded by XmlObjectReader.OnDelayedInitializeObjects
		/// </summary>
		[PropertyReadOrder(100, true)]
		[Browsable(false)]
		public BranchList ActionList
		{
			get
			{
				return _branchList;
			}
			set
			{
				_branchList = value;
				if (_branchList != null)
				{
					_branchList.SetOwnerMethod(this);
				}
			}
		}
		[PropertyReadOrder(2000)]
		[NoRecreateProperty]
		[Editor(typeof(TypeEditorSubscopeActions), typeof(UITypeEditor))]
		[Description("Specifies actions to be executed at the end of the method. The actions will be executed even an exception throws")]
		public SubscopeActions FinalActions
		{
			get
			{
				if (_finalActions == null)
				{
					_finalActions = new SubscopeActions(this);
				}
				return _finalActions;
			}
			set
			{
				_finalActions = value;
				if (_finalActions != null)
				{
					_finalActions.SetOwnerMethod(this);
				}
			}
		}
		[RefreshProperties(RefreshProperties.All)]
		[DefaultValue(2001)]
		[NoRecreateProperty]
		[Description("Specifies actions as exception handlers to handle generic exceptions or exceptions of specific types")]
		public ExceptionHandlerList ExceptionHandlers
		{
			get
			{
				if (_exceptionHandlers == null)
				{
					_exceptionHandlers = new ExceptionHandlerList(this);
				}
				if (_exceptionHandlers.Count == 0)
				{
					_exceptionHandlers.Add(new ExceptionHandler(this));
				}
				bool bHasEmpty = false;
				foreach (ExceptionHandler eh in _exceptionHandlers)
				{
					if (eh.IsDefaultExceptionType)
					{
						bHasEmpty = true;
						break;
					}
				}
				if (!bHasEmpty)
				{
					_exceptionHandlers.Add(new ExceptionHandler(this));
				}
				return _exceptionHandlers;
			}
			set
			{
				if (value != null)
				{
					_exceptionHandlers = value;
					_exceptionHandlers.SetOwner(this);
				}
			}
		}
		[Browsable(false)]
		public bool IsDefined
		{
			get
			{
				if (_branchList == null)
				{
					return false;
				}
				return (_branchList.Count > 0);
			}
		}
		[Browsable(false)]
		public Type ViewerHolderType
		{
			get
			{
				return typeof(MethodDesignerHolder);
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public virtual UInt32 ClassId
		{
			get
			{
				if (_owner != null)
				{
					return _owner.ClassId;
				}
				return 0;
			}
			set
			{
				throw new DesignerException("ClassId for MethodClass cannot be set");
			}
		}
		[Browsable(false)]
		public virtual UInt32 MemberId
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
		//
		[Browsable(false)]
		public virtual ParameterClass ReturnValue
		{
			get
			{
				if (_returnValue == null)
				{
					_returnValue = new ParameterClass(new TypePointer(typeof(void)), this);
					_returnValue.Name = "value";
				}
				return _returnValue;
			}
			set
			{
				if (_returnValue == null)
				{
					_returnValue = new ParameterClass(new TypePointer(typeof(void)), this);
					_returnValue.Name = "value";
				}
				DataTypePointer dp = value as DataTypePointer;
				if (dp != null)
				{
					_returnValue.SetDataType(dp);
				}
			}
		}
		[Browsable(false)]
		public virtual List<ParameterClass> Parameters
		{
			get
			{
				if (_parameters == null)
				{
					_parameters = new List<ParameterClass>();
				}
				return _parameters;
			}
			set
			{
				_parameters = value;
				if (_parameters != null)
				{
					foreach (ParameterClass p in _parameters)
					{
						p.Method = this;
					}
				}
				_display = null;
			}
		}
		[Browsable(false)]
		public int ParameterCount
		{
			get
			{
				if (_parameters == null)
					return 0;
				return _parameters.Count;
			}
		}

		[ParenthesizePropertyName(true)]
		[Description("Name of the method")]
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (_name != value)
				{
					bool cancel = false;
					if (NameChanging != null)
					{
						NameBeforeChangeEventArg nc = new NameBeforeChangeEventArg(_name, value, true);
						NameChanging(this, nc);
						cancel = nc.Cancel;
					}
					if (!cancel)
					{
						_name = value;
						_display = null;
						if (PropertyChanged != null)
						{
							PropertyChanged(this, new PropertyChangeEventArg("Name"));
						}
					}
				}
			}
		}
		[PropertyReadOrder(1000)]
		[Browsable(false)]
		public List<ComponentIcon> ComponentIconList
		{
			get
			{
				if (_componentIconList == null)
					_componentIconList = new List<ComponentIcon>();
				return _componentIconList;
			}
			set
			{
				_componentIconList = value;
			}
		}
		[Browsable(false)]
		public List<ConstObjectPointer> CustomAttributeList
		{
			get
			{
				return _attributes;
			}
		}
		#endregion
		#region IEventHandler Members

		[ReadOnly(true)]
		[Browsable(false)]
		public virtual UInt64 WholeId
		{
			get
			{
				return DesignUtil.MakeDDWord(MemberId, ClassId);
			}
			set
			{
				if (value != 0)
				{
					throw new DesignerException("MethodClass.WholeId cannot be set");
				}
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
		[Browsable(false)]
		public bool IsGroup
		{
			get { return true; }
		}

		#endregion
		#region ICloneable Members
		protected virtual void CopyFromThis(MethodClass obj)
		{
			obj.IsStatic = this.IsStatic;
			obj.MemberId = MemberId;
			obj.SetName(_name);
			obj.Description = Description;
			obj.IsNewMethod = IsNewMethod;
			obj.MakeUIThreadSafe = MakeUIThreadSafe;
			obj.WebUsage = WebUsage;
			obj._xmlNode = _xmlNode;
			obj.ParameterEditType = this.ParameterEditType;
			if (_returnValue != null)
			{
				obj.ReturnValue = (ParameterClass)_returnValue.Clone();
			}
			if (_parameters != null)
			{
				List<ParameterClass> list = new List<ParameterClass>();//[_parameters.Count];
				for (int i = 0; i < _parameters.Count; i++)
				{
					list.Add((ParameterClass)_parameters[i].Clone());
				}
				obj.Parameters = list;
			}

			if (_branchList != null)
			{
				obj.ActionList = (BranchList)_branchList.Clone();
			}
			//
			List<ComponentIcon> lst = ComponentIconList;
			foreach (ComponentIcon c in lst)
			{
				obj.ComponentIconList.Add((ComponentIcon)c.Clone());
			}
		}
		/// <summary>
		/// the derived classes need to clone TaskIdList
		/// </summary>
		/// <returns></returns>
		public virtual object Clone()
		{
			ClassPointer owner = (ClassPointer)_owner.Clone();
			MethodClass obj = (MethodClass)Activator.CreateInstance(this.GetType(), owner);
			//
			CopyFromThis(obj);
			return obj;
		}

		#endregion
		#region IObjectPointer Members
		/// <summary>
		/// the class holding (not neccerily declaring) this pointer
		/// </summary>
		[Browsable(false)]
		public ClassPointer RootPointer
		{
			get
			{
				IObjectPointer root = this.Holder;
				while (root != null && root.Owner != null)
				{
					root = root.Owner;
				}
				ClassPointer c = root as ClassPointer;
				return c;
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
				return Name;
			}
		}
		/// <summary>
		/// fully qualified variable name
		/// </summary>
		[Browsable(false)]
		public string ReferenceName
		{
			get
			{
				if (Owner != null)
					return Owner.ReferenceName + "." + Name;
				return Name;
			}
		}
		[Browsable(false)]
		public string TypeString
		{
			get
			{
				return _owner.TypeString;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="method"></param>
		/// <returns>CodeMethodReferenceExpression</returns>
		public virtual CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			CodeMethodReferenceExpression cmr;
			if (IsStatic)
			{
				cmr = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(Declarer.TypeString), this.Name);
			}
			else
			{
				cmr = new CodeMethodReferenceExpression(OnGetTargetObject(Holder.GetReferenceCode(method, statements, forValue)), this.Name);
			}
			return cmr;
		}
		public virtual string GetJavaScriptReferenceCode(StringCollection code)
		{
			return Name;
		}
		public virtual string GetPhpScriptReferenceCode(StringCollection code)
		{
			return Name;
		}
		public virtual void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
		}
		public virtual void CreateActionPhpScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
		}
		protected virtual CodeExpression OnGetTargetObject(CodeExpression targetObject)
		{
			return targetObject;
		}
		[Browsable(false)]
		public bool IsValid
		{
			get
			{
				if (!string.IsNullOrEmpty(this.Name) && _owner != null)
				{
					return true;
				}
				MathNode.LastValidationError = string.Format(CultureInfo.InvariantCulture, "(_owner={2}, Name={3}) for [{0}] of [{1}]. ", this.ToString(), this.GetType().Name,_owner,Name);
				return false;
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
		public virtual ClassPointer Declarer
		{
			get
			{
				return _owner;
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
				_owner = (ClassPointer)value;
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
				return Owner.ObjectType;
			}
			set
			{
				Owner.ObjectType = value;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public object ObjectInstance
		{
			get
			{
				return Owner.ObjectInstance;
			}
			set
			{
				Owner.ObjectInstance = value;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public object ObjectDebug
		{
			get
			{
				return Owner.ObjectDebug;
			}
			set
			{
				Owner.ObjectDebug = value;
			}
		}
		public bool IsSameObjectRef(IObjectIdentity objectPointer)
		{
			MethodClass mc = objectPointer as MethodClass;
			if (mc != null)
			{
				return (mc.WholeId == this.WholeId);
			}
			CustomMethodPointer cmp = objectPointer as CustomMethodPointer;
			if (cmp != null)
			{
				return (cmp.WholeId == this.WholeId);
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
				if (string.IsNullOrEmpty(_display))
				{
					_display = GetMethodSignature();
				}
				return _display;
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
			return (target == EnumObjectSelectType.Method || target == EnumObjectSelectType.All);
		}
		[Browsable(false)]
		public string ObjectKey
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Owner.ObjectKey, this.MemberId.ToString("x", CultureInfo.InvariantCulture));
			}
		}
		[Browsable(false)]
		public string MethodSignature
		{
			get
			{
				return GetMethodSignature();
			}
		}
		[Browsable(false)]
		public virtual EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public EnumPointerType PointerType { get { return EnumPointerType.Method; } }
		#endregion
		#region ISerializerProcessor Members

		public virtual void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
			{
				if (IsNewMethod)
				{
					XmlUtil.SetAttribute(objectNode, XmlTags.XMLATT_New, IsNewMethod);
				}
				else
				{
					XmlUtil.RemoveAttribute(objectNode, XmlTags.XMLATT_New);
				}
			}
			else
			{
				bool b = XmlUtil.GetAttributeBoolDefFalse(objectNode, XmlTags.XMLATT_New);
				if (b)
				{
					this.IsNewMethod = b;
				}
				if (_parameters != null)
				{
					foreach (ParameterClass p in _parameters)
					{
						p.Method = this;
					}
				}
			}
		}

		#endregion
		#region IMethod Members
		public bool IsParameterFilePath(string parameterName)
		{
			return false;
		}
		public string CreateWebFileAddress(string localFilePath, string parameterName)
		{
			return null;
		}
		public IMethodPointer CreateMethodPointer(IActionContext action)
		{
			CustomMethodPointer mp = new CustomMethodPointer(this, _holder);
			mp.Action = (IAction)action;
			return mp;
		}
		private LimnorProject _prj;

		[Browsable(false)]
		[ReadOnly(true)]
		[XmlIgnore]
		public object ModuleProject
		{
			get
			{
				return Project;
			}
			set
			{
				_prj = (LimnorProject)value;
			}
		}
		[Browsable(false)]
		public bool CanBeReplacedInEditor { get { return true; } }
		[Browsable(false)]
		public virtual bool IsMethodReturn { get { return false; } }
		[Browsable(false)]
		public virtual Type ActionBranchType
		{
			get
			{
				return typeof(AB_SingleAction);
			}
		}
		[Browsable(false)]
		public virtual Type ActionType
		{
			get
			{
				return typeof(ActionClass);
			}
		}
		/// <summary>
		/// return is always void
		/// </summary>
		[Browsable(false)]
		public virtual bool NoReturn
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public virtual bool HasReturn
		{
			get
			{
				if (NoReturn)
					return false;
				if (_returnValue != null)
				{
					if (typeof(void).Equals(_returnValue.BaseClassType))
					{
						return false;
					}
					if (_returnValue.IsLibType)
					{
						if (_returnValue.LibTypePointer.ObjectType.Equals(typeof(void)))
						{
							return false;
						}
						else
						{
							return true;
						}
					}
					else
					{
						return true;
					}
				}
				return false;
			}
		}
		public object GetParameterTypeByIndex(int idx)
		{
			if (idx >= 0 && _parameters != null && idx < _parameters.Count)
			{
				return _parameters[idx];
			}
			return null;
		}
		public void SetParameterTypeByIndex(int idx, object type)
		{
			if (idx >= 0 && _parameters != null && idx < _parameters.Count)
			{
				if (_parameters[idx] != null)
				{
					_parameters[idx].SetDataType(type);
				}
			}
		}
		public object GetParameterType(UInt32 id)
		{
			if (_parameters != null)
			{
				foreach (ParameterClass p in _parameters)
				{
					if (p.ParameterID == id)
					{
						return p;
					}
				}
			}
			return null;
		}
		public object GetParameterType(string name)
		{
			if (_parameters != null)
			{
				foreach (ParameterClass p in _parameters)
				{
					if (string.Compare(name, p.Name, StringComparison.Ordinal) == 0)
					{
						return p;
					}
				}
			}
			return null;
		}
		public Dictionary<string, string> GetParameterDescriptions()
		{
			if (_parameters != null)
			{
				Dictionary<string, string> desc = new Dictionary<string, string>();
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (string.IsNullOrEmpty(_parameters[i].Description))
						desc.Add(_parameters[i].Name, _parameters[i].Name);
					else
						desc.Add(_parameters[i].Name, _parameters[i].Description);
				}
				return desc;
			}
			return null;
		}
		/// <summary>
		/// avoid causing property change event
		/// </summary>
		/// <param name="name"></param>
		public void SetName(string name)
		{
			_name = name;
		}
		public void SetComponentIcons(List<ComponentIcon> icons)
		{
			_componentIconList = icons;
		}
		public string ParameterName(int i)
		{
			if (_parameters != null)
			{
				if (i >= 0 && i < _parameters.Count)
					return _parameters[i].Name;
			}
			return null;
		}
		public bool ParametersMatch(DataTypePointer[] ps)
		{
			if (_parameters == null || _parameters.Count == 0)
			{
				if (ps == null || ps.Length == 0)
				{
					return true;
				}
				return false;
			}
			else
			{
				if (ps == null)
					return false;
				if (ps.Length != _parameters.Count)
					return false;
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (!ps[i].IsSameObjectRef(_parameters[i]))
					{
						return false;
					}
				}
				return true;
			}
		}
		[Browsable(false)]
		public bool IsForLocalAction
		{
			get
			{
				return false;
			}
		}
		public bool IsSameMethod(IMethod method)
		{
			MethodClass mc = method as MethodClass;
			if (mc != null)
			{
				return (this.WholeId == mc.WholeId);
			}
			CustomMethodPointer cmp = method as CustomMethodPointer;
			if (cmp != null)
			{
				return (this.WholeId == cmp.WholeId);
			}
			return false;
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public IParameter MethodReturnType
		{
			get
			{
				return ReturnValue;
			}
			set
			{
				ReturnValue = (ParameterClass)value;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public IList<IParameter> MethodParameterTypes
		{
			get
			{
				List<IParameter> ps = null;
				if (_parameters != null)
				{
					ps = new List<IParameter>();
					foreach (ParameterClass p in _parameters)
					{
						ps.Add(p);
					}
				}
				return ps;
			}
		}
		[Browsable(false)]
		public virtual string DefaultActionName
		{
			get
			{
				return Holder.Name + "." + this.Name;
			}
		}
		#endregion
		#region IActionOwner Members
		[Browsable(false)]
		public MethodClass OwnerMethod
		{
			get { return this; }
		}
		[Browsable(false)]
		public IActionsHolder ActionsHolder
		{
			get { return this; }
		}
		#endregion
		#region IMethodCompile Members
		public object CompilerData(string key)
		{
			return _compiler;
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public CodeTypeDeclaration TypeDeclaration
		{
			get
			{
				return _td;
			}
			set
			{
				_td = value;
			}
		}
		[Browsable(false)]
		public CodeStatementCollection MainStatements
		{
			get
			{
				if (_tryCatchFinal == null)
				{
					return _codeMemberMethod.Statements;
				}
				else
				{
					return _tryCatchFinal.TryStatements;
				}
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public System.CodeDom.CodeMemberMethod MethodCode
		{
			get
			{
				return _codeMemberMethod;
			}
			set
			{
				_codeMemberMethod = value;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public UInt32 MethodID
		{
			get
			{
				return MemberId;
			}
			set
			{
				MemberId = value;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public virtual string MethodName
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
		/// <summary>
		/// when compiling a sub method, set the sub-method here to pass it into compiling elements
		/// </summary>
		[ReadOnly(true)]
		[Browsable(false)]
		public Stack<IMethod0> SubMethod
		{
			get
			{
				if (_subMethods == null)
					_subMethods = new Stack<IMethod0>();
				return _subMethods;
			}
		}
		public IMethod GetSubMethodByParameterId(UInt32 parameterId)
		{
			IMethod smi = null;
			Stack<IMethod0>.Enumerator en = SubMethod.GetEnumerator();
			while (en.MoveNext())
			{
				IMethod mp = en.Current as IMethod;
				if (mp != null)
				{
					IList<IParameter> ps = mp.MethodParameterTypes;
					if (ps != null && ps.Count > 0)
					{
						foreach (IParameter p in ps)
						{
							if (p.ParameterID == parameterId)
							{
								smi = mp;
								break;
							}
						}
					}
					if (smi != null)
					{
						break;
					}
				}
			}
			return smi;
		}
		#endregion
		#region ICustomPointer Members

		[Browsable(false)]
		public IClass Holder
		{
			get
			{
				if (_holder != null)
				{
					return _holder;
				}
				return (IClass)Owner;
			}
		}

		#endregion
		#region INonHostedObject Members
		EventHandler NameChanging;
		EventHandler PropertyChanged;
		public virtual void OnPropertyChanged(string name, object property, XmlNode rootNode, XmlObjectWriter writer)
		{
			XmlNode node = this.XmlData;
			if (node == null)
			{
				throw new DesignerException("OnPropertyChanged:method node not found for [{0},{1}]", MemberId, Name);
			}
			if (string.CompareOrdinal(name, "Name") == 0)
			{
				XmlNode nd = SerializeUtil.CreatePropertyNode(node, "Name");
				nd.InnerText = this.Name;
				XmlUtil.SetNameAttribute(node, this.Name);
			}
			else if (string.CompareOrdinal(name, "IsStatic") == 0)
			{
				XmlNode nd = SerializeUtil.CreatePropertyNode(node, "IsStatic");
				nd.InnerText = this.IsStatic.ToString();
			}
			else if (string.CompareOrdinal(name, "IsFinal") == 0)
			{
				XmlNode nd = SerializeUtil.CreatePropertyNode(node, "IsFinal");
				nd.InnerText = this.IsFinal.ToString();
			}
			else if (string.CompareOrdinal(name, "Description") == 0)
			{
				XmlNode nd = SerializeUtil.CreatePropertyNode(node, "Description");
				nd.InnerText = this.Description;
			}
			else if (string.CompareOrdinal(name, "ExceptionHandlers") == 0)
			{
				XmlNode nd = SerializeUtil.CreatePropertyNode(node, "ExceptionHandlers");
				ExceptionHandler eh = property as ExceptionHandler;
				if (eh != null)
				{
					XmlNodeList nds = nd.SelectNodes(XmlTags.XML_Item);
					//get item node
					XmlNode itemNode = null;
					foreach (XmlNode n in nds)
					{
						Type t = XmlUtil.GetLibTypeAttribute(n);
						if (eh.ExceptionType.Equals(t))
						{
							itemNode = n;
							break;
						}
					}
					if (itemNode == null)
					{
						itemNode = rootNode.OwnerDocument.CreateElement(XmlTags.XML_Item);
						nd.AppendChild(itemNode);
					}
					writer.WriteObjectToNode(itemNode, eh);
				}
			}
		}

		public void SetChangeEvents(EventHandler nameChange, EventHandler propertyChange)
		{
			NameChanging = nameChange;
			PropertyChanged = propertyChange;
		}
		public EventHandler GetPropertyChangeHandler()
		{
			return PropertyChanged;
		}
		#endregion
		#region IAttributeHolder Members

		public void AddAttribute(ConstObjectPointer attr)
		{
			XmlNode methodNode = this.XmlData;
			if (methodNode == null)
				throw new DesignerException("Method node not found for [{0},{1}] *", ClassId, MemberId);
			XmlNode node = SerializeUtil.CreateAttributeNode(methodNode, attr.ValueId);
			XmlObjectWriter wr = new XmlObjectWriter(_owner.ObjectList);
			wr.WriteObjectToNode(node, attr);
			if (_attributes == null)
			{
				_attributes = new List<ConstObjectPointer>();
			}
			_attributes.Add(attr);
		}

		public void RemoveAttribute(ConstObjectPointer attr)
		{
			XmlNode methodNode = this.XmlData;
			if (methodNode == null)
				throw new DesignerException("Method node not found for [{0},{1}]. *", ClassId, MemberId);
			SerializeUtil.RemoveAttributeNode(methodNode, attr.ValueId);
			if (_attributes != null)
			{
				foreach (ConstObjectPointer a in _attributes)
				{
					if (a.ValueId == attr.ValueId)
					{
						_attributes.Remove(a);
						break;
					}
				}
			}
		}

		public virtual List<ConstObjectPointer> GetCustomAttributeList()
		{
			if (_attributes == null)
			{
				if (MemberId != 0)
				{
					XmlNode methodNode = this.XmlData;
					if (methodNode == null)
					{
					}
					else
					{
						List<ConstObjectPointer> attributes = new List<ConstObjectPointer>();
						XmlObjectReader xr = _owner.ObjectList.Reader;
						XmlNodeList nodes = SerializeUtil.GetAttributeNodeList(methodNode);
						foreach (XmlNode nd in nodes)
						{
							ConstObjectPointer a = xr.ReadObject<ConstObjectPointer>(nd, this);
							attributes.Add(a);
						}
						if (xr.HasErrors)
						{
							MathNode.Log(xr.Errors);
						}
						_attributes = attributes;
					}
				}
				else
				{
				}
			}
			return _attributes;
		}

		#endregion
		#region IBeforeSerializeNotify Members

		public virtual void OnBeforeRead(XmlObjectReader reader, XmlNode node)
		{
			_xmlNode = node;
			_reader = reader;
		}

		public virtual void OnBeforeWrite(XmlObjectWriter writer, XmlNode node)
		{
			_xmlNode = node;
			_writer = writer;
		}
		public void ReloadFromXmlNode()
		{
			if (_reader != null && _xmlNode != null)
			{
				_branchList = null;
				_codeMemberMethod = null;
				_compiler = null;
				_componentIconList = null;
				_desc = null;
				_display = null;
				_holder = null;
				_isFinal = false;
				_isStatic = false;
				_parameters = null;
				_returnValue = null;
				_subMethods = null;
				_td = null;
				OnBeforeRead();
				Guid g = Guid.NewGuid();
				_reader.ResetErrors();
				_reader.ClearDelayedInitializers(g);
				_reader.ReadObjectFromXmlNode(_xmlNode, this, this.GetType(), _owner);
				_reader.OnDelayedInitializeObjects(g);
				if (_reader.HasErrors)
				{
					MathNode.Log(_reader.Errors);
				}
				OnAfterRead();
			}
		}
		public void UpdateXmlNode(XmlObjectWriter writer)
		{
			if (_xmlNode != null)
			{
				if (writer != null)
				{
					writer.WriteObjectToNode(_xmlNode, this);
				}
				else if (GetWriter() != null)
				{
					_writer.WriteObjectToNode(_xmlNode, this);
				}
			}
		}
		[Browsable(false)]
		public XmlNode XmlData { get { return _xmlNode; } }

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
			PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(this, attributes, true);
			if (VPLUtil.GetBrowseableProperties(attributes))
			{
				List<PropertyDescriptor> list = new List<PropertyDescriptor>();
				foreach (PropertyDescriptor p in ps)
				{
					if (IsOverride)
					{
						if (string.CompareOrdinal(p.Name, "Description") == 0)
						{
							if (Implemented)
							{
								list.Add(p);
							}
						}
						else
						{
							ReadOnlyPropertyDesc p0 = new ReadOnlyPropertyDesc(p);
							list.Add(p0);
							continue;
						}
					}
					else
					{
						PropertyDescriptor p0 = OnGettingPropertyDescriptor(p, p.Attributes);
						if (p0 != null)
						{
							list.Add(p0);
						}
					}
				}
				ps = new PropertyDescriptorCollection(list.ToArray());
			}
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
		#region IDelayedInitialize Members
		public void OnDelayedPostSerialize(ObjectIDmap objMap, XmlNode objectNode, XmlObjectReader reader)
		{
			XmlNode nd;
			_xmlNode = objectNode;
			nd = objectNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}[@name='{1}']", XmlTags.XML_PROPERTY, "ActionList"));
			if (nd == null)
			{
			}
			else
			{
				PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this, new Attribute[] { DesignOnlyAttribute.No });
				foreach (PropertyDescriptor p in properties)
				{
					if (string.CompareOrdinal(p.Name, "ActionList") == 0)
					{
						reader.ReadProperty(p, nd, this, false, XmlTags.XML_PROPERTY);
						break;
					}
				}
				foreach (ExceptionHandler eh in this.ExceptionHandlers)
				{
					if (!eh.IsDefaultExceptionType)
					{
						nd = eh.DataXmlNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"{0}[@name='{1}']", XmlTags.XML_PROPERTY, "ActionList"));
						if (nd != null)
						{
							properties = TypeDescriptor.GetProperties(eh, new Attribute[] { DesignOnlyAttribute.No });
							foreach (PropertyDescriptor p in properties)
							{
								if (string.CompareOrdinal(p.Name, "ActionList") == 0)
								{
									reader.ReadProperty(p, nd, eh, false, XmlTags.XML_PROPERTY);
									break;
								}
							}
						}
					}
				}
			}
			if (_dependendDeserializers != null)
			{
				foreach (IPostDeserializeProcess obj in _dependendDeserializers)
				{
					obj.OnDeserialize(this.RootPointer);
				}
				_dependendDeserializers = null;
			}
		}

		public void SetReader(ObjectIDmap objMap, XmlNode objectNode, XmlObjectReader reader)
		{
			_xmlNode = objectNode;
			_reader = reader;
		}

		#endregion
		#region IActionsHolder Members
		[Browsable(false)]
		public XmlNode ActionsNode
		{
			get
			{
				if (this.XmlData == null)
				{
					_xmlNode = RootXmlNode.OwnerDocument.CreateElement(XmlTag);
				}
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
			get { return this.MemberId; }
		}
		[Browsable(false)]
		public Dictionary<uint, IAction> ActionInstances
		{
			get { return _acts; }
		}
		
		[DefaultValue(EnumParameterEditType.Edit)]
		[Browsable(false)]
		public EnumParameterEditType ParameterEditType { get; set; }

		public void SetActionInstances(Dictionary<uint, IAction> actions)
		{
			_acts = actions;
		}
		public Dictionary<UInt32, IAction> GetVisibleActionInstances()
		{
			Dictionary<UInt32, IAction> lst = new Dictionary<uint, IAction>();
			if (_acts == null)
			{
				LoadActionInstances();
			}
			if (_acts != null)
			{
				foreach (KeyValuePair<UInt32, IAction> kv in _acts)
				{
					if (!lst.ContainsKey(kv.Key))
					{
						lst.Add(kv.Key, kv.Value);
					}
				}
			}
			Dictionary<uint, IAction> acts = RootPointer.ActionInstances;
			if (acts == null)
			{
				RootPointer.LoadActionInstances();
			}
			if (acts != null)
			{
				foreach (KeyValuePair<UInt32, IAction> kv in acts)
				{
					if (!lst.ContainsKey(kv.Key))
					{
						lst.Add(kv.Key, kv.Value);
					}
				}
			}
			return lst;
		}
		public void LoadActionInstances()
		{
			this.RootPointer.LoadActions(this);
			Dictionary<uint, IAction> acts = RootPointer.ActionInstances;
			if (acts == null)
			{
				RootPointer.LoadActionInstances();
			}
			acts = RootPointer.ActionInstances;
			SetActions(acts);
			if (FinalActions != null)
			{
				FinalActions.LoadActionInstances();
			}
			if (this.ExceptionHandlers != null)
			{
				foreach (ExceptionHandler eh in ExceptionHandlers)
				{
					if (!eh.IsDefaultExceptionType)
					{
						eh.LoadActionInstances();
					}
				}
			}
		}
		[Browsable(false)]
		public IAction TryGetActionInstance(UInt32 actId)
		{
			if (_branchList != null)
			{
				return _branchList.GetActionById(actId);
			}
			return null;
		}
		[Browsable(false)]
		public IAction GetActionInstance(UInt32 actId)
		{
			IAction a = ClassPointer.GetActionObject(actId, this, RootPointer);
			if (a != null)
			{
				return a;
			}
			Dictionary<uint, IAction> acts = RootPointer.ActionInstances;
			if (acts == null)
			{
				RootPointer.LoadActionInstances();
				acts = RootPointer.ActionInstances;
			}
			SetActions(acts);
			if (_branchList != null)
			{
				a = _branchList.GetActionById(actId);
			}
			return a;
		}
		public ActionBranch FindActionBranchById(UInt32 branchId)
		{
			if (_branchList != null)
			{
				return _branchList.SearchBranchById(branchId);
			}
			return null;
		}
		public void GetActionNames(StringCollection sc)
		{
			if (_branchList != null)
			{
				_branchList.GetActionNames(sc);
			}
			if (_acts == null)
			{
				LoadActionInstances();
			}
			if (_acts != null)
			{
				foreach (IAction a in _acts.Values)
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
		[Browsable(false)]
		public void AddActionInstance(IAction action)
		{
			if (_acts == null)
			{
				_acts = new Dictionary<uint, IAction>();
			}
			bool found = _acts.ContainsKey(action.ActionId);
			if (!found)
			{
				_acts.Add(action.ActionId, action);
			}
		}
		[Browsable(false)]
		public void DeleteActions(List<UInt32> actionIds)
		{

			if (_acts != null)
			{
				foreach (UInt32 id in actionIds)
				{
					IAction a;
					if (_acts.TryGetValue(id, out a))
					{
						if (a != null)
						{
							ClassPointer.DeleteAction(_xmlNode, id);
						}
						_acts.Remove(id);
					}
				}
			}
		}
		#endregion
	}
	/// <summary>
	/// represent a custom method defined in a base class.
	/// it is only dynamically loaded, not saved
	/// </summary>
	public class MethodClassInherited : MethodClass
	{
		private ClassPointer _declarer;
		public MethodClassInherited(ClassPointer owner)
			: base(owner)
		{
		}
		public void SetDeclarer(ClassPointer leaf)
		{
			_declarer = (ClassPointer)Owner;
			Owner = leaf;
		}
		public void SetDeclarerDirect(ClassPointer declarer)
		{
			_declarer = declarer;
		}
		public BaseMethod CreateBaseMethod()
		{
			BaseMethod bm = new BaseMethod((ClassPointer)(this.Owner));
			bm._declarer = _declarer;
			bm.MemberId = this.MemberId;
			bm.Name = Name;
			bm.AccessControl = AccessControl;
			bm.IsStatic = IsStatic;
			if (ReturnValue != null)
			{
				bm.ReturnValue = (ParameterClass)ReturnValue.Clone();
			}
			if (Parameters != null && Parameters.Count > 0)
			{
				List<ParameterClass> lst = new List<ParameterClass>();
				foreach (ParameterClass p in Parameters)
				{
					lst.Add((ParameterClass)(p.Clone()));
				}
				bm.Parameters = lst;
			}
			return bm;
		}
		protected override void CopyFromThis(MethodClass obj)
		{
			base.CopyFromThis(obj);
			MethodClassInherited mi = (MethodClassInherited)obj;
			mi._declarer = _declarer;
		}
		/// <summary>
		/// true: base is virtual
		/// false: base is abstract
		/// </summary>
		[Browsable(false)]
		public override bool HasBaseImplementation
		{
			get
			{
				return !IsAbstract;
			}
		}
		[Browsable(false)]
		public override bool IsOverride
		{
			get { return true; }
		}
		[ReadOnly(true)]
		public override EnumAccessControl AccessControl
		{
			get
			{
				return base.AccessControl;
			}
			set
			{
				base.AccessControl = value;
			}
		}
		[Browsable(false)]
		public override bool Implemented
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public override bool DoNotCompile
		{
			get
			{
				return true;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public override UInt32 MemberId
		{
			get
			{
				return (UInt32)MethodSignature.GetHashCode();
			}
			set
			{
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public override UInt32 ClassId
		{
			get
			{
				if (_declarer != null)
					return _declarer.ClassId;
				return 0;
			}
			set
			{
				throw new DesignerException("ClassId cannot be set for MethodClassInherited");
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public override UInt64 WholeId
		{
			get
			{
				return DesignUtil.MakeDDWord(MemberId, ClassId);
			}
			set
			{
				throw new DesignerException("WholeId cannot be set for MethodClassInherited");
			}
		}

		#region ICustomPointer Members
		[Browsable(false)]
		public override ClassPointer Declarer
		{
			get
			{
				return _declarer;
			}
		}

		#endregion

	}
}
