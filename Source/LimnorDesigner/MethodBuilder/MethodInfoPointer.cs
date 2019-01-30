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
using LimnorDesigner.Action;
using System.CodeDom;
using MathExp;
using LimnorDesigner.MenuUtil;
using VPL;
using ProgElements;
using System.Collections.Specialized;
using Limnor.Drawing2D;
using System.Windows.Forms;
using System.Collections;
using Limnor.WebBuilder;
using XmlSerializer;
using System.Xml;
using Limnor.WebServerBuilder;
using System.Globalization;
using XmlUtility;
using LimnorDesigner.Web;
using LimnorDesigner.DesignTimeType;
using Limnor.CopyProtection;
using WindowsUtility;
using LimnorDesigner.Event;

namespace LimnorDesigner.MethodBuilder
{
	public class MethodInfoPointer : MethodPointer, IMethodMeta
	{
	
	        private bool isArrayType;
		
		private MethodInfo _mif;
		private bool _isWebServerMethod;
		//
		public MethodInfoPointer()
		{
		}
		public MethodInfoPointer(MethodInfo method)
		{
			_mif = method;
			onMethodInfoSet();
		}
		private void onMethodInfoSet()
		{
			retrieveParameters();
			if (_mif != null)
			{
				object[] objs = _mif.GetCustomAttributes(typeof(WebServerMemberAttribute), true);
				if (objs != null && objs.Length > 0)
				{
					_isWebServerMethod = true;
				}
			}
		}
		private void retrieveParameters()
		{
			if (_mif != null)
			{
				ReturnType2 = _mif.ReturnType;
				ParameterInfo[] ps = _mif.GetParameters();
				if (ps != null)
				{
					Type[] tps = new Type[ps.Length];
					for (int i = 0; i < ps.Length; i++)
					{
						tps[i] = ps[i].ParameterType;
					}
					ParameterTypes = tps;
				}
				MemberName = _mif.Name;
			}
		}
		[Browsable(false)]
		public MethodInfo MethodInfo
		{
			get
			{
				return _mif;
			}
		}
		[Browsable(false)]
		public bool IsWebServerMethod
		{
			get
			{
				return _isWebServerMethod;
			}
		}
		[Browsable(false)]
		public override Type ActionBranchType
		{
			get
			{
				if (MethodInformation is SubMethodInfo)
				{
					return typeof(AB_SubMethodAction);
				}
				return typeof(AB_SingleAction);
			}
		}
		[Browsable(false)]
		public override Type ActionType
		{
			get
			{
				if (MethodInformation is SubMethodInfo)
				{
					return typeof(ActionSubMethod);
				}
				return typeof(ActionClass);
			}
		}
		protected override void OnSetMethodDef(MethodBase method)
		{
			_mif = (MethodInfo)method;
			onMethodInfoSet();
		}
		/// <summary>
		/// when load a parameter, call this function to find out the parameter type
		/// </summary>
		/// <param name="id"></param>
		/// <returns>a Type</returns>
		public override object GetParameterType(UInt32 id)
		{
			ParameterInfo[] pifs = Info;
			if (pifs != null)
			{
				int idi = (int)id;
				for (int i = 0; i < pifs.Length; i++)
				{
					if (string.IsNullOrEmpty(pifs[i].Name))
					{
						if (pifs[i].ParameterType.GetHashCode() == idi)
						{
							return pifs[i].ParameterType;
						}
					}
					else if (pifs[i].Name.GetHashCode() == idi)
					{
						return pifs[i].ParameterType;
					}
				}
			}
			return null;
		}
		public override object GetParameterType(string name)
		{
			ParameterInfo[] pifs = Info;
			if (pifs != null)
			{
				for (int i = 0; i < pifs.Length; i++)
				{
					string nm = pifs[i].Name;
					if (string.IsNullOrEmpty(nm))
					{
						if (pifs[i].Member.DeclaringType.IsArray)
						{
							nm = string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter{0}", i);
						}
					}
					if (string.Compare(name, nm, StringComparison.Ordinal) == 0)
					{
						return pifs[i].ParameterType;
					}
				}
			}
			MethodInfo mif = MethodInformation;
			if (mif != null)
			{
				ParameterInfo[] pifs2 = mif.GetParameters();
				if (pifs2 != null && pifs2.Length > 0)
				{
					for (int i = 0; i < pifs2.Length; i++)
					{
						string nm = pifs2[i].Name;
						if (string.IsNullOrEmpty(nm))
						{
							if (pifs2[i].Member.DeclaringType.IsArray)
							{
								nm = string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter{0}", i);
							}
						}
						if (string.Compare(name, nm, StringComparison.Ordinal) == 0)
						{
							return pifs2[i].ParameterType;
						}
					}
				}
			}
			return null;
		}
		public override bool IsSameObjectRef(IObjectIdentity obj)
		{
			MethodInfoPointer mpp = obj as MethodInfoPointer;
			if (mpp != null)
			{
				if (base.IsSameObjectRef(obj))
				{
					bool bOK = DesignUtil.IsSameValueType(ReturnType, mpp.ReturnType);
					if (bOK)
					{
						bOK = DesignUtil.IsSameValueTypes(ParameterTypes, mpp.ParameterTypes);
					}
					return bOK;
				}
			}
			return false;
		}
		protected virtual CodeExpression OnGetTargetObject(CodeExpression targetObject)
		{
			return targetObject;
		}
		public override CodeExpression GetReferenceCode(IMethodCompile method, CodeStatementCollection statements, bool forValue)
		{
			MethodBase mb = MethodDef;
			CodeExpression targetObject;
			if (mb.IsStatic)
			{
				targetObject = new CodeTypeReferenceExpression(mb.DeclaringType);
			}
			else
			{
				DrawingControl dc = Owner.ObjectInstance as DrawingControl;
				CodeExpression ce = CompilerUtil.GetDrawItemExpression(dc);
				if (ce != null)
					targetObject = ce;
				else
					targetObject = Owner.GetReferenceCode(method, statements, forValue);
			}
			if (typeof(CopyProtector).Equals(Owner.ObjectType))
			{
				if (string.CompareOrdinal(mb.Name, "VerifyLicense") == 0)
				{
					VPLUtil.IsCopyProtectorClient = true;
					if (!VPLUtil.IsCompilingToDebug)
					{
						CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
						cmie.Method = new CodeMethodReferenceExpression();
						cmie.Method.MethodName = "InitApp";
						cmie.Method.TargetObject = new CodeTypeReferenceExpression(typeof(WinUtil));
						statements.Add(cmie);
					}
				}
			}
			bool b = (string.Compare(mb.Name, "get_Item", StringComparison.Ordinal) == 0);
			if (b)
			{
				b = mb.IsSpecialName;
			}
			if (!b)
			{
				b = (mb.DeclaringType.IsArray && string.Compare(mb.Name, "Get", StringComparison.Ordinal) == 0);
			}
			if (b)
			{
				CodeArrayIndexerExpression ai = new CodeArrayIndexerExpression(OnGetTargetObject(targetObject), ParameterCodeExpressions);
				return ai;
			}
			else if (isArrayType)
			{
				CodeArrayIndexerExpression ai = new CodeArrayIndexerExpression(OnGetTargetObject(targetObject), ParameterCodeExpressions[0]);
				return ai;
			}
			else
			{
				CodeMethodReferenceExpression mref = new CodeMethodReferenceExpression();
				CodeExpression ceOwner = OnGetTargetObject(targetObject);
				if (mb.DeclaringType != null)
				{
					if (mb.DeclaringType.IsInterface)
					{
						ceOwner = new CodeCastExpression(mb.DeclaringType, ceOwner);
					}
				}
				mref.TargetObject = ceOwner;
				mref.MethodName = MemberName;
				if (mb.IsGenericMethod && mb.ContainsGenericParameters)
				{
					Type[] tcs = mb.GetGenericArguments();
					if (tcs != null && tcs.Length > 0)
					{
						for (int i = 0; i < tcs.Length; i++)
						{
							DataTypePointer dp = this.GetConcreteType(tcs[i]);
							if (dp == null)
							{
								throw new DesignerException("Generic argument [{0}] not set for method [{1}]", tcs[i].Name, VPLUtil.GetMethodSignature(mb));
							}
							else
							{
								mref.TypeArguments.Add(dp.BaseClassType);
							}
						}
					}
				}
				CodeMethodInvokeExpression cmi = new CodeMethodInvokeExpression();
				cmi.Method = mref;
				if (ParameterCodeExpressions != null)
				{
					cmi.Parameters.AddRange(ParameterCodeExpressions);
				}
				return cmi;
			}
		}
		public override void CreateJavaScript(StringCollection sb, StringCollection parameters, string returnReceiver)
		{
			HtmlElement_BodyBase hbb = this.Owner as HtmlElement_BodyBase;
			if (hbb != null)
			{
				hbb.CreateActionJavaScript(this.MemberName, sb, parameters, returnReceiver);
				return;
			}
			object obj = this.Owner.ObjectInstance;
			if (obj == null)
			{
				ParameterClass pc = this.Owner as ParameterClass;
				if (pc != null && pc.ObjectType != null)
				{
					try
					{
						object[] vs = pc.ObjectType.GetCustomAttributes(typeof(UseParentObjectAttribute), true);
						if (vs != null && vs.Length > 0)
						{
							ConstructorInfo[] cifs = pc.ObjectType.GetConstructors();
							if (cifs != null && cifs.Length > 0)
							{
								Type tOwner = null;
								object objOwner = null;
								MethodInfoPointer mip = pc.Method as MethodInfoPointer;
								if (mip != null && mip.Action != null)
								{
									WebClientEventHandlerMethodDownloadActions wea = mip.Action.ActionHolder as WebClientEventHandlerMethodDownloadActions;
									if (wea != null && wea.Event != null)
									{
										if (wea.Event.Owner != null)
										{
											objOwner = wea.Event.Owner.ObjectInstance;
											if (objOwner != null)
											{
												tOwner = objOwner.GetType();
											}
										}
									}
								}
								ConstructorInfo c0 = null;
								ConstructorInfo c1 = null;
								for (int i = 0; i < cifs.Length; i++)
								{
									ParameterInfo[] pifs = cifs[i].GetParameters();
									if (pifs == null || pifs.Length == 0)
									{
										c0 = cifs[i];
									}
									else if (pifs.Length == 1)
									{
										if (tOwner != null && pifs[0].ParameterType.IsAssignableFrom(tOwner))
										{
											c1 = cifs[i];
										}
									}
								}
								if (c1 != null)
								{
									obj = Activator.CreateInstance(pc.ObjectType, objOwner);
								}
								else if (c0 != null)
								{
									obj = Activator.CreateInstance(pc.ObjectType);
								}
							}
						}
						else
						{
							obj = Activator.CreateInstance(pc.ObjectType);
						}
					}
					catch
					{
					}
					if (obj != null)
					{
						pc.ObjectInstance = obj;
					}
				}
			}
			if (obj == null)
			{
				IObjectCompiler ioc = ObjectCompilerAttribute.GetCompilerObject(this.Owner.ObjectType);
				if (ioc != null)
				{
					ioc.CreateActionJavaScript(this.Owner.CodeName, this.MemberName, sb, parameters, returnReceiver);
					return;
				}
			}
			IWebClientControl webC = obj as IWebClientControl;
			if (webC != null)
			{
				if (string.IsNullOrEmpty(webC.id) || string.CompareOrdinal(webC.id, "''") == 0)
				{
					if (this.Action != null && this.Action.MethodOwner != null)
					{
						webC.SetCodeName(this.Action.MethodOwner.CodeName);
					}
				}
				webC.CreateActionJavaScript(this.MemberName, sb, parameters, returnReceiver);
			}
			else
			{
				ISupportWebClientMethods swc = obj as ISupportWebClientMethods;
				if (swc != null)
				{
					string codeName = null;
					if (this.Action != null && this.Action.MethodOwner != null)
					{
						codeName = this.Action.MethodOwner.CodeName;
					}
					swc.CreateActionJavaScript(codeName, this.MethodName, sb, parameters, returnReceiver);
				}
				else
				{
					this.Owner.CreateActionJavaScript(this.MethodName, sb, parameters, returnReceiver);
				}
			}
		}
		public override void CreatePhpScript(StringCollection sb, StringCollection parameters, string returnReceiver)
		{
			this.Owner.CreateActionPhpScript(this.MethodName, sb, parameters, returnReceiver);
		}
		public override string GetJavaScriptReferenceCode(StringCollection code)
		{
			HtmlElement_BodyBase hbb = this.Owner as HtmlElement_BodyBase;
			if (hbb != null)
			{
				return hbb.GetJavaScriptReferenceCode(code, this.MethodName, ParameterJS);
			}
			if (this.Owner.ObjectType.GetInterface("IJavascriptType") != null)
			{
				IJavascriptType js = Activator.CreateInstance(this.Owner.ObjectType) as IJavascriptType;
				StringCollection ps = new StringCollection();
				if (ParameterJS != null)
				{
					ps.AddRange(ParameterJS);
				}
				return js.GetJavascriptMethodRef(this.Owner.GetJavaScriptReferenceCode(code), this.MemberName, code, ps);
			}
			if (typeof(JavascriptFiles).IsAssignableFrom(this.Owner.ObjectType))
			{
				if (string.CompareOrdinal(this.MethodName, "Execute") == 0)
				{
					if (ParameterJS == null || ParameterJS.Length == 0)
					{
						throw new DesignerException("Execute action missing JavaScript code");
					}
					return ParameterJS[0];
				}
			}
			if (this.RootPointer != null && this.RootPointer.Project != null && (this.RootPointer.Project.ProjectType == VSPrj.EnumProjectType.WebAppAspx || this.RootPointer.Project.ProjectType == VSPrj.EnumProjectType.WebAppPhp))
			{
				PropertyPointer pp = this.Owner as PropertyPointer;
				if (pp != null && pp.RunAt == EnumWebRunAt.Client)
				{
					IExtendedPropertyOwner epo = pp.Owner.ObjectInstance as IExtendedPropertyOwner;
					if (epo != null)
					{
						Type t = epo.PropertyCodeType(pp.MemberName);
						if (t != null)
						{
							if (t.Equals(typeof(DateTime)))
							{
								JsDateTime jd = new JsDateTime();
								string o = this.Owner.GetJavaScriptReferenceCode(code);
								StringCollection ps = new StringCollection();
								if (ParameterJS != null)
								{
									ps.AddRange(ParameterJS);
								}
								string ce = jd.GetJavascriptMethodRef(o, this.MemberName, code, ps);
								if (!string.IsNullOrEmpty(ce))
								{
									return ce;
								}
							}
						}
					}
				}
			}
			IWebClientComponent wc = this.Owner.ObjectInstance as IWebClientControl;
			if (wc == null)
			{
				if (this.Owner.ObjectInstance == null && this.Owner.ObjectType != null)
				{
					if (typeof(IWebClientComponent).Equals(this.Owner.ObjectType) || typeof(IWebClientControl).Equals(this.Owner.ObjectType))
					{
						StringCollection ps = new StringCollection();
						if (ParameterJS != null)
						{
							ps.AddRange(ParameterJS);
						}
						return WebPageCompilerUtility.GetJavaScriptWebMethodReferenceCode(this.Owner.GetJavaScriptReferenceCode(code), this.MethodName, code, ps);
					}
					if (this.Owner.ObjectType.GetInterface("IWebClientComponent") != null)
					{
						try
						{
							wc = Activator.CreateInstance(this.Owner.ObjectType) as IWebClientComponent;
							if (wc != null)
							{
								wc.SetCodeName(this.Owner.CodeName);
							}
						}
						catch
						{
						}
					}
				}
			}
			if (wc != null)
			{
				return wc.GetJavaScriptReferenceCode(code, this.MethodName, ParameterJS);
			}
			IWebClientSupport wco = this.Owner.ObjectInstance as IWebClientSupport;
			if (wco != null)
			{
				StringCollection ps = new StringCollection();
				if (ParameterJS != null)
				{
					ps.AddRange(ParameterJS);
				}
				string mr = wco.GetJavaScriptWebMethodReferenceCode(this.Owner.CodeName, this.MemberName, code, ps);
				if (!string.IsNullOrEmpty(mr))
				{
					return mr;
				}
			}
			if (this.Owner.Owner != null)
			{
				wco = this.Owner.Owner.ObjectInstance as IWebClientSupport;
				if (wco != null)
				{
					StringCollection ps = new StringCollection();
					if (ParameterJS != null)
					{
						ps.AddRange(ParameterJS);
					}
					string mr = wco.GetJavaScriptWebMethodReferenceCode(this.Owner.CodeName, this.MemberName, code, ps);
					if (!string.IsNullOrEmpty(mr))
					{
						return mr;
					}
				}
			}
			if (this.Owner.ObjectType.IsArray)
			{
				if (string.CompareOrdinal(MemberName, "Get") == 0)
				{
					StringBuilder a = new StringBuilder();
					a.Append(this.Owner.CodeName);
					a.Append("[");
					a.Append(ParameterJS[0]);
					a.Append("]");
					return a.ToString();
				}
			}
			StringBuilder sb = new StringBuilder();
			if (this.Owner != this.RootPointer)
			{
				string o = this.Owner.GetJavaScriptReferenceCode(code);
				if (!string.IsNullOrEmpty(o))
				{
					sb.Append(o);
					sb.Append(".");
				}
			}
			if (string.CompareOrdinal(MemberName, "ToString") == 0 && (ParameterJS == null || ParameterJS.Length == 0))
			{
				sb.Append("toString");
			}
			else
			{
				sb.Append(MemberName);
			}
			sb.Append("(");
			if (ParameterJS != null && ParameterJS.Length > 0)
			{
				sb.Append(ParameterJS[0]);
				for (int i = 1; i < ParameterJS.Length; i++)
				{
					sb.Append(",");
					sb.Append(ParameterJS[i]);
				}
			}
			sb.Append(")");
			return sb.ToString();
		}
		public override string GetPhpScriptReferenceCode(StringCollection code)
		{
			if (this.Owner.ObjectType.GetInterface("IPhpType") != null)
			{
				IPhpType php = Activator.CreateInstance(this.Owner.ObjectType) as IPhpType;
				StringCollection ps = new StringCollection();
				if (ParameterPhp != null)
				{
					ps.AddRange(ParameterPhp);
				}
				return php.GetMethodRef(this.Owner.CodeName, this.MemberName, code, ps);
			}
			IPhpServerObject sobj = this.Owner.ObjectInstance as IPhpServerObject;
			if (sobj != null)
			{
				string ret = sobj.GetPhpMethodRef(this.Owner.CodeName, this.MemberName, code, ParameterPhp);
				if (!string.IsNullOrEmpty(ret))
				{
					return ret;
				}
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(this.Owner.GetPhpScriptReferenceCode(code));
			sb.Append("->");
			sb.Append(MemberName);
			sb.Append("(");
			if (ParameterPhp != null && ParameterPhp.Length > 0)
			{
				sb.Append(ParameterPhp[0]);
				for (int i = 1; i < ParameterPhp.Length; i++)
				{
					sb.Append(",");
					sb.Append(ParameterPhp[i]);
				}
			}
			sb.Append(")");
			return sb.ToString();
		}
		public override void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			WebPageCompilerUtility.CreateActionJavaScript(this.GetJavaScriptReferenceCode(code), methodName, code, parameters, returnReceiver);
		}
		public override void CreateActionPhpScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
		}
		public override void Compile(ActionBranch currentAction, ActionBranch nextAction, ILimnorCodeCompiler compiler, IMethodCompile methodToCompile, CodeMemberMethod method, CodeStatementCollection statements, CodeExpressionCollection parameters, IObjectPointer returnReceiver, bool debug)
		{
			MethodInfo mif = MethodInformation;
			CodeExpression cmi = null;
			if (typeof(LimnorKioskApp).IsAssignableFrom(this.Owner.ObjectType))
			{
				if (string.CompareOrdinal(this.MemberName, "ExitKiosk") == 0)
				{
					if (string.IsNullOrEmpty(compiler.AppName))
					{
						throw new DesignerException("Error getting kiosk application name");
					}
					if (string.IsNullOrEmpty(compiler.KioskBackFormName))
					{
						throw new DesignerException("Error getting kiosk application background name");
					}
					cmi = new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(compiler.AppName), compiler.KioskBackFormName), "ExitKiosk");
				}
			}
			object[] objAttrs = mif.GetCustomAttributes(true);
			if (objAttrs != null && objAttrs.Length > 0)
			{
				for (int i = 0; i < objAttrs.Length; i++)
				{
					StaticAsInstanceAttribute sai = objAttrs[i] as StaticAsInstanceAttribute;
					if (sai != null)
					{
						cmi = sai.CreateMethodInvokeCode(this.Owner.TypeString, parameters, statements);
						break;
					}
				}
			}
			bool isCustomMethodCompiler = false;
			IClassWrapper wrapper;
			if (cmi == null)
			{
				wrapper = Owner as IClassWrapper;
				if (wrapper != null)
				{
					CodeExpression[] ps;
					if (parameters == null)
					{
						ps = new CodeExpression[] { };
					}
					else
					{
						ps = new CodeExpression[parameters.Count];
						parameters.CopyTo(ps, 0);
					}
					CodeExpression ce = wrapper.GetReferenceCode(methodToCompile, statements, this, ps, false);
					if (ce != null)
					{
						if (ce is CodeArrayIndexerExpression && string.Compare(MethodName, "Set", StringComparison.Ordinal) == 0)
						{
							CodeAssignStatement cas = new CodeAssignStatement(ce, ps[1]);
							statements.Add(cas);
							return;
						}
						cmi = ce;
					}
				}
				if (cmi == null)
				{
					ICustomMethodCompiler cmc = Owner.ObjectInstance as ICustomMethodCompiler;
					if (cmc != null)
					{
						cmi = cmc.CompileMethod(this.MemberName, Owner.CodeName, methodToCompile, statements, parameters);
						if (cmi != null)
						{
							isCustomMethodCompiler = true;
						}
					}
					if (cmi == null)
					{
						CodeExpression[] ps;
						if (parameters != null)
						{
							ps = new CodeExpression[parameters.Count];
							parameters.CopyTo(ps, 0);
						}
						else
						{
							ps = new CodeExpression[] { };
						}
						SetParameterExpressions(ps);
						
						isArrayType = string.Compare(mif.Name, "set_Item", StringComparison.Ordinal) == 0;
						isArrayType = isArrayType && mif.IsSpecialName;
						if (isArrayType)
						{
							CodeExpression tarobj = GetReferenceCode(methodToCompile, statements, false);
							CodeAssignStatement cas00 = new CodeAssignStatement(tarobj, ps[1]);
							statements.Add(cas00);
							return;
						}
						
						cmi = GetReferenceCode(methodToCompile, statements, false);
					}
				}
			}
			if (compiler.SupportPageNavigator)
			{
				if (string.Compare(this.MethodName, "Show", StringComparison.Ordinal) == 0)
				{
					if (typeof(DrawingPage).IsAssignableFrom(Owner.ObjectType))
					{
						CodeExpression oe = Owner.GetReferenceCode(methodToCompile, statements, false);
						if (!(oe is CodeThisReferenceExpression))
						{
							CodeMethodReferenceExpression me = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(FormNavigator)), "AddForm");
							CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression(me, new CodeThisReferenceExpression(), oe);
							statements.Add(new CodeExpressionStatement(mie));
						}
					}
				}
			}
			bool bThreadSafe = !methodToCompile.MakeUIThreadSafe;
			if (!bThreadSafe)
			{
				//check to see if the 
				if (!this.HasFormControlParent)
				{
					bThreadSafe = true;
				}
			}
			if (bThreadSafe && currentAction != null)
			{
				if (!currentAction.IsMainThread && mif.DeclaringType != null && typeof(Control).IsAssignableFrom(mif.DeclaringType))
				{
					bThreadSafe = false;
				}
			}
			if (mif.ReturnType == null || typeof(void).Equals(mif.ReturnType))
			{
				if (!isCustomMethodCompiler)
				{
					CodeExpressionStatement ces = new CodeExpressionStatement(cmi);
					if (!bThreadSafe)
					{
						statements.Add(new CodeSnippetStatement(CompilerUtil.InvokeStart));
					}
					statements.Add(ces);
					if (!bThreadSafe)
					{
						statements.Add(new CodeSnippetStatement(CompilerUtil.InvokeEnd));
					}
				}
			}
			else
			{
				bool useOutput = false;
				if (nextAction != null && nextAction.UseInput)
				{
					CodeVariableDeclarationStatement output = new CodeVariableDeclarationStatement(
						currentAction.OutputType.TypeString, currentAction.OutputCodeName, cmi);
					if (!bThreadSafe)
					{
						statements.Add(new CodeSnippetStatement(CompilerUtil.InvokeStart));
					}
					statements.Add(output);
					if (!bThreadSafe)
					{
						statements.Add(new CodeSnippetStatement(CompilerUtil.InvokeEnd));
					}
					cmi = new CodeVariableReferenceExpression(currentAction.OutputCodeName);
					useOutput = true;
				}
				if (returnReceiver != null && !(returnReceiver is NullObjectPointer))
				{
					CodeExpression cr = returnReceiver.GetReferenceCode(methodToCompile, statements, false);
					if (cr != null)
					{
						Type target;
						wrapper = returnReceiver as IClassWrapper;
						if (wrapper != null)
						{
							target = wrapper.WrappedType;
						}
						else
						{
							RuntimeInstance ri = returnReceiver.ObjectInstance as RuntimeInstance;
							if (ri != null)
							{
								target = ri.InstanceType;
							}
							else
							{
								target = VPLUtil.GetObjectType(returnReceiver.ObjectType);
							}
						}
						bool tsafe = bThreadSafe;
						if (!tsafe)
						{
							if (CompilerUtil.IsOwnedByControl(Owner))
							{

							}
							else if (CompilerUtil.IsOwnedByControl(returnReceiver))
							{
							}
							else
							{
								tsafe = true;
							}
						}
						LocalVariable lv = returnReceiver as LocalVariable;
						if (lv != null)
						{
							if (lv.VariableCustomType != null)
							{
								statements.Add(new CodeAssignStatement(
									cr,
									new CodeCastExpression(lv.TypeString, cmi)));
							}
							else
							{
								CompilerUtil.CreateAssignment(cr, target, cmi, mif.ReturnType, statements, tsafe);
							}
						}
						else
						{
							CompilerUtil.CreateAssignment(cr, target, cmi, mif.ReturnType, statements, tsafe);
						}
					}
				}
				else
				{
					if (!useOutput)
					{
						if (cmi is CodeMethodInvokeExpression ||
							cmi is CodeDelegateInvokeExpression)
						{
							CodeExpressionStatement ces = new CodeExpressionStatement(cmi);
							if (!bThreadSafe)
							{
								statements.Add(new CodeSnippetStatement(CompilerUtil.InvokeStart));
							}
							statements.Add(ces);
							if (!bThreadSafe)
							{
								statements.Add(new CodeSnippetStatement(CompilerUtil.InvokeEnd));
							}
						}
					}
				}
			}
		}
		private int _calledMethodsForMethodDef;
		[Browsable(false)]
		public override MethodBase MethodDef
		{
			get
			{
				if (_mif == null)
				{
					if (Owner == null)
					{
					}
					else
					{
						IClassWrapper wrapper = Owner as IClassWrapper;
						if (wrapper != null)
						{
							_mif = wrapper.GetMethod(MemberName, this.GetParameterTypes());
							if (_mif != null)
							{
								ReturnType2 = _mif.ReturnType;
							}
						}
						else
						{
							Type t;
							if (Owner.ObjectInstance != null)
							{
								t = VPLUtil.GetObjectType(Owner.ObjectInstance);
							}
							else
							{
								t = Owner.ObjectType;
							}
							if (t == null)
							{
								DesignUtil.WriteToOutputWindowDelegate("Class {0} is not fully loaded.", Owner);
							}
							else
							{
								Type t0 = t;
								if (!typeof(DrawingItem).IsAssignableFrom(t))
								{
									if (t.GetInterface("IJavascriptType") == null && t.GetInterface("IPhpType") == null)
									{
										t = VPLUtil.GetInternalType(t);
									}
								}
								if (VPLUtil.IsDynamicAssembly(t.Assembly))
								{
									t0 = ClassPointerX.GetClassTypeFromDynamicType(t);
									_mif = t0.GetMethod(MethodName);
								}
								else
								{
									IDynamicMethodParameters dmp = Owner.ObjectInstance as IDynamicMethodParameters;
									if (dmp != null)
									{
										if (dmp.IsUsingDynamicMethodParameters(MethodName))
										{
											_mif = t.GetMethod(this.MemberName, new Type[] { typeof(object[]) });
											if (_mif == null)
											{
												if (!t0.Equals(t))
												{
													_mif = t0.GetMethod(this.MemberName, new Type[] { typeof(object[]) });
												}
											}
											if (_mif == null)
											{
												_mif = t.GetMethod(this.MemberName, new Type[] { typeof(ComponentPointer), typeof(object[]) });
												if (_mif == null)
												{
													if (!t0.Equals(t))
													{
														_mif = t0.GetMethod(this.MemberName, new Type[] { typeof(ComponentPointer), typeof(object[]) });
													}
												}
												if (_mif == null)
												{
													MethodInfo[] mifs = t.GetMethods();
													if (mifs != null)
													{
														for (int i = 0; i < mifs.Length; i++)
														{
															if (string.CompareOrdinal(mifs[i].Name, this.MemberName) == 0)
															{
																_mif = mifs[i];
																break;
															}
														}
													}
													if (_mif == null)
													{
														if (!t0.Equals(t))
														{
															mifs = t0.GetMethods();
															if (mifs != null)
															{
																for (int i = 0; i < mifs.Length; i++)
																{
																	if (string.CompareOrdinal(mifs[i].Name, this.MemberName) == 0)
																	{
																		_mif = mifs[i];
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
								if (_mif == null)
								{
									_calledMethodsForMethodDef++;
									if (_calledMethodsForMethodDef > 2)
									{
									}
									else
									{
										try
										{
											_mif = VPLUtil.GetMethod(t, this.MemberName, this.GetParameterTypes(), ReturnType2);
											if (_mif == null)
											{
												if (!t0.Equals(t))
												{
													_mif = VPLUtil.GetMethod(t0, this.MemberName, this.GetParameterTypes(), ReturnType2);
												}
											}
										}
										catch (Exception err)
										{
											StringBuilder sb = new StringBuilder();
											Type[] ps = GetParameterTypes();
											if (ps == null || ps.Length == 0)
											{
												sb.Append("No parameter.");
											}
											else
											{
												sb.Append("p0:");
												if (ps[0] == null)
												{
													sb.Append("null");
												}
												else
												{
													sb.Append(ps[0].FullName);
												}
												for (int i = 1; i < ps.Length; i++)
												{
													sb.Append(",p");
													sb.Append(i.ToString());
													sb.Append(":");
													if (ps[i] == null)
													{
														sb.Append("null");
													}
													else
													{
														sb.Append(ps[i].FullName);
													}
												}
											}
											MathNode.Log(err, "Error calling MethodDef [{0}].[{1}]({2})", t.Name, this.MemberName, sb.ToString());
										}
									}
									_calledMethodsForMethodDef = 0;
								}
								if (_mif == null)
								{
									_mif = t.GetMethod(this.MemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
									if (_mif == null)
									{
										if (!t0.Equals(t))
										{
											_mif = t0.GetMethod(this.MemberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
										}
									}
									if (typeof(DateTime).Equals(t))
									{
										_mif = typeof(JsDateTime).GetMethod(MethodName);
									}
									if (_mif == null)
									{
										if (DesignUtil.WriteToOutputWindowDelegate != null)
										{
											DesignUtil.WriteToOutputWindowDelegate("Method [{0}] not found in [{1}]", MemberName, t);
										}
									}
								}
							}
						}
					}
				}
				return _mif;
			}
		}
		public virtual MethodInfo MethodInformation
		{
			get
			{
				return MethodDef as MethodInfo;
			}
		}
		[Browsable(false)]
		public override Type ReturnType
		{
			get
			{
				if (_mif != null)
					return _mif.ReturnType;
				return base.ReturnType2;
			}
		}
		[Browsable(false)]
		public override bool NoReturn
		{
			get
			{
				return typeof(void).Equals(ReturnType);
			}
		}
		[Browsable(false)]
		public override bool HasReturn
		{
			get
			{
				MethodInfo mif = MethodInformation;
				if (mif != null)
				{
					if (mif.ReturnType != null && !mif.ReturnType.Equals(typeof(void)))
					{
						return true;
					}
				}
				return false;
			}
		}
		#region ISerializerProcessor Members

		public override void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			base.OnPostSerialize(objMap, objectNode, saved, serializer);
			if (saved)
			{
			}
			else
			{
				if (_mif == null)
				{
					ClassInstancePointer cip = this.Owner as ClassInstancePointer;
					if (cip != null)
					{
						if (cip.Definition == null)
						{
							cip.ReplaceDeclaringClassPointer(ClassPointer.CreateClassPointer(cip.DefinitionClassId, objMap.Project));
						}
						if (cip.Definition != null)
						{
							_mif = VPLUtil.GetMethod(cip.Definition.BaseClassType, this.MemberName, ParameterTypes, ReturnType2);
						}
					}
					else
					{
						if (this.Owner != null)
						{
							ICustomMethodDescriptor lti = this.Owner.ObjectInstance as ICustomMethodDescriptor;
							if (lti != null)
							{

								_mif = lti.GetMethod(this.MemberName, ParameterTypes, ReturnType2);
							}
							else
							{
								Type ownerType = this.Owner.ObjectType;
								if (ownerType == null)
								{
									ownerType = DesignUtil.GetObjectType(objectNode, this.Owner, (XmlObjectReader)serializer);
								}
								if (ownerType != null)
								{
									try
									{
										_mif = VPLUtil.GetMethod(ownerType, this.MemberName, ParameterTypes, ReturnType2);
									}
									catch (Exception err)
									{
										StringBuilder sb = new StringBuilder();
										if (ParameterTypes == null || ParameterTypes.Length == 0)
										{
											sb.Append("No parameter");
										}
										else
										{
											sb.Append("p0:");
											if (ParameterTypes[0] == null)
											{
												sb.Append("null");
											}
											else
											{
												sb.Append(ParameterTypes[0].FullName);
											}
											for (int i = 1; i < ParameterTypes.Length; i++)
											{
												sb.Append(",p");
												sb.Append(i.ToString());
												sb.Append(":");
												if (ParameterTypes[i] == null)
												{
													sb.Append("null");
												}
												else
												{
													sb.Append(ParameterTypes[i].FullName);
												}
											}
										}
										MathNode.Log(err, "Error loading [{0}]({1})", this.MemberName, sb.ToString());
									}
								}
							}
						}
					}
					if (_mif != null)
					{
						onMethodInfoSet();
					}
				}
			}
		}

		#endregion

		#region IMethodMeta Members

		public MethodInfo GetMethodInfo()
		{
			return _mif;
		}

		#endregion
	}
}
