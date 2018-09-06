/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Expression Display and Edit
 * License: GNU General Public License v3.0
 
 */
/*
 * All rights reserved by Longflow Enterprises Ltd
 * 
 * Formula Editor
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using System.CodeDom;
using System.ComponentModel;
using MathExp.RaisTypes;
using System.Collections.Specialized;
using System.Globalization;
using VPL;
using System.Windows.Forms;

namespace MathExp
{
	public abstract class BinOperatorNode : MathNodeStandardFunction
	{
		protected string _symbol = "";
		protected Font ftParenthesis;
		protected Font ftSymbol;
		protected bool useSymbol = false;
		public BinOperatorNode(MathNode parent)
			: base(parent)
		{
		}
		[Browsable(false)]
		public abstract CodeBinaryOperatorType operaterType { get; }
		public override CodeExpression ExportCode(IMethodCompile method)
		{
			MathNode.Trace("{0}.ExportCode for {1}", this.GetType().Name, operaterType);
			//determine parameter types
			BinOperatorNode bn = this[1] as BinOperatorNode;
			MathNode mn = this[1];
			RaisDataType[] types = RequiredParameterTypes;
			switch (operaterType)
			{
				case CodeBinaryOperatorType.BooleanAnd:
				case CodeBinaryOperatorType.BooleanOr:
					types[0] = new RaisDataType(typeof(bool));
					types[1] = new RaisDataType(typeof(bool));
					CompileDataType = new RaisDataType(typeof(bool));
					break;
				case CodeBinaryOperatorType.GreaterThan:
				case CodeBinaryOperatorType.GreaterThanOrEqual:
				case CodeBinaryOperatorType.IdentityInequality:
				case CodeBinaryOperatorType.LessThan:
				case CodeBinaryOperatorType.LessThanOrEqual:
				case CodeBinaryOperatorType.ValueEquality:
					if (bn != null)
					{
						mn = bn[0];
					}
					if (!this[0].DataType.IsSameType(mn.DataType))
					{
						if (this[0].DataType.Type.Equals(typeof(bool)))
						{
							types[1] = new RaisDataType(typeof(bool));
							CompileDataType = new RaisDataType(typeof(bool));
						}
						else if (mn.DataType.Type.Equals(typeof(bool)))
						{
							types[0] = new RaisDataType(typeof(bool));
							CompileDataType = new RaisDataType(typeof(bool));
						}
						else if (this[0].DataType.Type.Equals(typeof(DateTime)))
						{
							types[1] = new RaisDataType(typeof(DateTime));
							CompileDataType = new RaisDataType(typeof(DateTime));
						}
						else if (mn.DataType.Type.Equals(typeof(DateTime)))
						{
							types[0] = new RaisDataType(typeof(DateTime));
							CompileDataType = new RaisDataType(typeof(DateTime));
						}
						else if (ValueTypeUtil.IsNumber(this[0].DataType.Type) && !ValueTypeUtil.IsNumber(mn.DataType.Type))
						{
							types[1] = new RaisDataType(this[0].DataType.Type);
							CompileDataType = new RaisDataType(this[0].DataType.Type);
						}
						else if (ValueTypeUtil.IsNumber(mn.DataType.Type) && !ValueTypeUtil.IsNumber(this[0].DataType.Type))
						{
							types[0] = new RaisDataType(mn.DataType.Type);
							CompileDataType = new RaisDataType(mn.DataType.Type);
						}
						else if (ValueTypeUtil.IsNumber(mn.DataType.Type) && ValueTypeUtil.IsNumber(this[0].DataType.Type))
						{
							EnumNumTypeCastDir tc = ValueTypeUtil.CompareNumberTypes(this[0].DataType.Type, mn.DataType.Type);
							switch (tc)
							{
								case EnumNumTypeCastDir.Type1ToType2:
									types[0] = new RaisDataType(mn.DataType.Type);
									CompileDataType = new RaisDataType(mn.DataType.Type);
									break;
								case EnumNumTypeCastDir.Type2ToType1:
									types[1] = new RaisDataType(this[0].DataType.Type);
									CompileDataType = new RaisDataType(this[0].DataType.Type);
									break;
							}
						}
						else if (this[0].DataType.Type.Equals(typeof(char)))
						{
							types[1] = new RaisDataType(typeof(char));
							CompileDataType = new RaisDataType(typeof(char));
						}
						else if (mn.DataType.Type.Equals(typeof(char)))
						{
							types[0] = new RaisDataType(typeof(char));
							CompileDataType = new RaisDataType(typeof(char));
						}
						else if (this[0].DataType.Type.Equals(typeof(string)))
						{
							types[1] = new RaisDataType(typeof(string));
							CompileDataType = new RaisDataType(typeof(string));
						}
						else if (mn.DataType.Type.Equals(typeof(string)))
						{
							types[0] = new RaisDataType(typeof(string));
							CompileDataType = new RaisDataType(typeof(string));
						}
					}
					break;
				case CodeBinaryOperatorType.Add:
				case CodeBinaryOperatorType.Divide:
				case CodeBinaryOperatorType.Multiply:
				case CodeBinaryOperatorType.Subtract:
					if (this[0].DataType.IsNumber && !this[1].DataType.IsNumber)
					{
						types[1] = this[0].DataType;
						CompileDataType = new RaisDataType(this[0].DataType.Type);
					}
					else if (this[1].DataType.IsNumber && !this[0].DataType.IsNumber)
					{
						types[0] = this[1].DataType;
						CompileDataType = new RaisDataType(this[1].DataType.Type);
					}
					else if (!this[0].DataType.IsNumber && !this[1].DataType.IsNumber)
					{
						CompileDataType = new RaisDataType(typeof(double));
						if (this[0].DataType.IsString)
						{
							types[0] = new RaisDataType(typeof(double));
						}
						else
						{
							types[0] = this[0].DataType;
							CompileDataType = this[0].DataType;
						}
						if (this[1].DataType.IsString)
						{
							types[1] = new RaisDataType(typeof(double));
						}
						else
						{
							types[1] = this[1].DataType;
							if (CompileDataType.IsNumber)
							{
								CompileDataType = this[1].DataType;
							}
						}
					}
					break;
			}
			if (typeof(string).Equals(this[0].DataType.Type) && typeof(string).Equals(this[1].DataType.Type))
			{
				switch (operaterType)
				{
					case CodeBinaryOperatorType.GreaterThan:
					case CodeBinaryOperatorType.GreaterThanOrEqual:
					case CodeBinaryOperatorType.LessThan:
					case CodeBinaryOperatorType.LessThanOrEqual:
					case CodeBinaryOperatorType.IdentityInequality:
					case CodeBinaryOperatorType.IdentityEquality:
					case CodeBinaryOperatorType.ValueEquality:
						CompileDataType = new RaisDataType(typeof(bool));
						//string.CompareOrdinal(a,b) {operator} 0
						return new CodeBinaryOperatorExpression(
							new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)), "CompareOrdinal", this.GetParameterCode(method, 0), this.GetParameterCode(method, 1)),
							operaterType,
							new CodePrimitiveExpression(0));
				}
			}
			if (operaterType == CodeBinaryOperatorType.GreaterThan ||
				operaterType == CodeBinaryOperatorType.GreaterThanOrEqual ||
				operaterType == CodeBinaryOperatorType.LessThan ||
				operaterType == CodeBinaryOperatorType.LessThanOrEqual)
			{
				if (bn != null)
				{
					if (bn.operaterType == CodeBinaryOperatorType.GreaterThan ||
						bn.operaterType == CodeBinaryOperatorType.GreaterThanOrEqual ||
						bn.operaterType == CodeBinaryOperatorType.LessThan ||
						bn.operaterType == CodeBinaryOperatorType.LessThanOrEqual)
					{
						//(this[0] {operator} bn[0]) AND (bn[0] {operator} bn[1])
						CodeBinaryOperatorExpression b1 = new CodeBinaryOperatorExpression(this.GetParameterCode(method, 0), operaterType, bn.GetParameterCode(method, 0));
						CodeBinaryOperatorExpression b2 = bn.ExportCode(method) as CodeBinaryOperatorExpression;
						return new CodeBinaryOperatorExpression(b1, CodeBinaryOperatorType.BooleanAnd, b2);
					}
				}
			}
			//detect and handle modifier keys
			//
			IPropertyPointer pp = null;
			MathNodeValue val = null;
			int ntype = 0;
			pp = this[0] as IPropertyPointer;
			if (pp != null)
			{
				ntype = 1;
			}
			else
			{
				pp = this[1] as IPropertyPointer;
				if (pp != null)
				{
					ntype = 2;
				}
			}
			if (ntype != 0)
			{
				if (typeof(Keys).IsAssignableFrom(pp.ObjectType))
				{
					if (ntype == 1)
						val = this[1] as MathNodeValue;
					else
						val = this[0] as MathNodeValue;
					if (val != null)
					{
						if (!typeof(Keys).IsAssignableFrom(val.DataType.LibType))
						{
							ntype = 0;
						}
					}
					else
					{
						ntype = 0;
					}
					if (pp.PropertyOwner != null && pp.PropertyOwner.PropertyOwner != null && typeof(KeyEventArgs).IsAssignableFrom(pp.PropertyOwner.PropertyOwner.ObjectType))
					{
					}
					else
					{
						ntype = 0;
					}
				}
				else
				{
					ntype = 0;
				}
			}
			if (ntype == 0)
			{
				return new CodeBinaryOperatorExpression(this.GetParameterCode(method, 0), operaterType, this.GetParameterCode(method, 1));
			}
			else
			{
				//
				Keys ks = (Keys)val.Value;
				Keys modifiers = (Keys)0;
				if ((ks & Keys.Shift) == Keys.Shift)
					modifiers |= Keys.Shift;
				if ((ks & Keys.LShiftKey) == Keys.LShiftKey)
					modifiers |= Keys.LShiftKey;
				if ((ks & Keys.RShiftKey) == Keys.RShiftKey)
					modifiers |= Keys.RShiftKey;
				if ((ks & Keys.ShiftKey) == Keys.ShiftKey)
					modifiers |= Keys.ShiftKey;
				if ((ks & Keys.Control) == Keys.Control)
					modifiers |= Keys.Control;
				if ((ks & Keys.ControlKey) == Keys.ControlKey)
					modifiers |= Keys.ControlKey;
				if ((ks & Keys.LControlKey) == Keys.LControlKey)
					modifiers |= Keys.LControlKey;
				if ((ks & Keys.RControlKey) == Keys.RControlKey)
					modifiers |= Keys.RControlKey;
				if ((ks & Keys.Alt) == Keys.Alt)
					modifiers |= Keys.Alt;
				ks = ks & (~modifiers);
				//use inline-if expression to replace val: ((pp & modifiers) == modifiers) ? ks: Keys.None
				//but CodeDom does not support ternary conditional expression
				//
				//use CodeScript
				if (modifiers == (Keys)0)
				{
					return new CodeBinaryOperatorExpression(this.GetParameterCode(method, 0), operaterType, this.GetParameterCode(method, 1));
				}
				else
				{
					bool isFirst = true;
					StringBuilder sb = new StringBuilder();
					sb.Append("(");
					if ((modifiers & Keys.Shift) == Keys.Shift)
					{
						sb.Append("System.Windows.Forms.Keys.Shift");
						isFirst = false;
					}
					if ((modifiers & Keys.LShiftKey) == Keys.LShiftKey)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.LShiftKey");
					}
					if ((modifiers & Keys.RShiftKey) == Keys.RShiftKey)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.RShiftKey");
					}
					if ((modifiers & Keys.ShiftKey) == Keys.ShiftKey)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.ShiftKey");
					}
					if ((modifiers & Keys.Control) == Keys.Control)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.Control");
					}
					if ((modifiers & Keys.ControlKey) == Keys.ControlKey)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.ControlKey");
					}
					if ((modifiers & Keys.LControlKey) == Keys.LControlKey)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.LControlKey");
					}
					if ((modifiers & Keys.RControlKey) == Keys.RControlKey)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.RControlKey");
					}
					if ((modifiers & Keys.Alt) == Keys.Alt)
					{
						if (isFirst)
						{
							isFirst = false;
						}
						else
						{
							sb.Append("|");
						}
						sb.Append("System.Windows.Forms.Keys.Alt");
					}
					sb.Append(")");
					CodeBinaryOperatorExpression ret = new CodeBinaryOperatorExpression();
					ret.Operator = operaterType;
					//forming ((pp & modifiers) == modifiers) ? ks: System.Windows.Forms.Keys.None
					StringBuilder sbExp = new StringBuilder();
					sbExp.Append("(");
					//
					CodeExpression ce = this.GetParameterCode(method, 0);
					sbExp.Append("((");
					sbExp.Append(pp.PropertyOwner.PropertyOwner.CodeName);
					sbExp.Append(".Modifiers & ");
					sbExp.Append(sb.ToString());
					sbExp.Append(") == ");
					sbExp.Append(sb.ToString());
					sbExp.Append(") ? System.Windows.Forms.Keys.");
					sbExp.Append(ks.ToString());
					sbExp.Append(" : System.Windows.Forms.Keys.None");
					//
					sbExp.Append(")");
					CodeSnippetExpression cse = new CodeSnippetExpression(sbExp.ToString());
					if (ntype == 1)
					{
						ret.Left = ce;
						ret.Right = cse;
					}
					else
					{
						ret.Left = cse;
						ret.Right = ce;
					}
					return ret;
				}
			}
		}
		public override string CreateJavaScript(StringCollection method)
		{
			MathNode.Trace("{0}.CreateJavaScript", this.GetType().Name);
			string e1 = VPLUtil.JavascriptConvertElementValue( this[0].CreateJavaScript(method));
			string e2 = VPLUtil.JavascriptConvertElementValue( this[1].CreateJavaScript(method));
			switch (operaterType)
			{
				case CodeBinaryOperatorType.Add:
					if (this.DataType.IsNumber)
					{
						if (!this[0].DataType.IsNumber)
						{
							e1 = string.Format(CultureInfo.InvariantCulture, "parseFloat({0})", e1);
						}
						if (!this[1].DataType.IsNumber)
						{
							e2 = string.Format(CultureInfo.InvariantCulture, "parseFloat({0})", e2);
						}
					}
					return MathNode.FormString("{0} + {1}", e1, e2);
				case CodeBinaryOperatorType.Assign:
					return MathNode.FormString("{0} = {1}", e1, e2);
				case CodeBinaryOperatorType.BitwiseAnd:
					return MathNode.FormString("({0}) & ({1})", e1, e2);
				case CodeBinaryOperatorType.BitwiseOr:
					return MathNode.FormString("({0}) | ({1})", e1, e2);
				case CodeBinaryOperatorType.BooleanAnd:
					return MathNode.FormString("({0}) && ({1})", e1, e2);
				case CodeBinaryOperatorType.BooleanOr:
					return MathNode.FormString("({0}) || ({1})", e1, e2);
				case CodeBinaryOperatorType.Divide:
					return MathNode.FormString("({0}) / ({1})", e1, e2);
				case CodeBinaryOperatorType.GreaterThan:
					return MathNode.FormString("({0}) > ({1})", e1, e2);
				case CodeBinaryOperatorType.GreaterThanOrEqual:
					return MathNode.FormString("({0}) >= ({1})", e1, e2);
				case CodeBinaryOperatorType.IdentityEquality:
					return MathNode.FormString("({0}) == ({1})", e1, e2);
				case CodeBinaryOperatorType.IdentityInequality:
					return MathNode.FormString("({0}) != ({1})", e1, e2);
				case CodeBinaryOperatorType.LessThan:
					return MathNode.FormString("({0}) < ({1})", e1, e2);
				case CodeBinaryOperatorType.LessThanOrEqual:
					return MathNode.FormString("({0}) <= ({1})", e1, e2);
				case CodeBinaryOperatorType.Modulus:
					return MathNode.FormString("({0}) % ({1})", e1, e2);
				case CodeBinaryOperatorType.Multiply:
					return MathNode.FormString("({0}) * ({1})", e1, e2);
				case CodeBinaryOperatorType.Subtract:
					return MathNode.FormString("({0}) - ({1})", e1, e2);
				case CodeBinaryOperatorType.ValueEquality:
					if (typeof(Color).Equals(this[0].DataType.LibType) || typeof(Color).Equals(this[1].DataType.LibType))
					{
						return MathNode.FormString("JsonDataBinding.compareColor(({0}),({1}))", e1, e2);
					}
					else
					{
						return MathNode.FormString("({0}) == ({1})", e1, e2);
					}
			}
			return MathNode.FormString("({0}) == ({1})", e1, e2);
		}
		public override string CreatePhpScript(StringCollection method)
		{
			MathNode.Trace("{0}.CreatePhpScript", this.GetType().Name);
			string e1 = this[0].CreatePhpScript(method);
			string e2 = this[1].CreatePhpScript(method);
			switch (operaterType)
			{
				case CodeBinaryOperatorType.Add:
					return MathNode.FormString("{0} + {1}", e1, e2);
				case CodeBinaryOperatorType.Assign:
					return MathNode.FormString("{0} = {1}", e1, e2);
				case CodeBinaryOperatorType.BitwiseAnd:
					return MathNode.FormString("({0}) & ({1})", e1, e2);
				case CodeBinaryOperatorType.BitwiseOr:
					return MathNode.FormString("({0}) | ({1})", e1, e2);
				case CodeBinaryOperatorType.BooleanAnd:
					return MathNode.FormString("({0}) && ({1})", e1, e2);
				case CodeBinaryOperatorType.BooleanOr:
					return MathNode.FormString("({0}) || ({1})", e1, e2);
				case CodeBinaryOperatorType.Divide:
					return MathNode.FormString("({0}) / ({1})", e1, e2);
				case CodeBinaryOperatorType.GreaterThan:
					return MathNode.FormString("({0}) > ({1})", e1, e2);
				case CodeBinaryOperatorType.GreaterThanOrEqual:
					return MathNode.FormString("({0}) >= ({1})", e1, e2);
				case CodeBinaryOperatorType.IdentityEquality:
					return MathNode.FormString("({0}) == ({1})", e1, e2);
				case CodeBinaryOperatorType.IdentityInequality:
					return MathNode.FormString("({0}) != ({1})", e1, e2);
				case CodeBinaryOperatorType.LessThan:
					return MathNode.FormString("({0}) < ({1})", e1, e2);
				case CodeBinaryOperatorType.LessThanOrEqual:
					return MathNode.FormString("({0}) <= ({1})", e1, e2);
				case CodeBinaryOperatorType.Modulus:
					return MathNode.FormString("({0}) % ({1})", e1, e2);
				case CodeBinaryOperatorType.Multiply:
					return MathNode.FormString("({0}) * ({1})", e1, e2);
				case CodeBinaryOperatorType.Subtract:
					return MathNode.FormString("({0}) - ({1})", e1, e2);
				case CodeBinaryOperatorType.ValueEquality:
					return MathNode.FormString("({0}) == ({1})", e1, e2);
			}
			return MathNode.FormString("({0}) == ({1})", e1, e2);
		}

		public override object CloneExp(MathNode parent)
		{
			BinOperatorNode node = (BinOperatorNode)base.CloneExp(parent);
			node._symbol = _symbol;
			return node;
		}
		private RaisDataType _dataType;
		[Description("Data type of the calculation result")]
		public override RaisDataType DataType
		{
			get
			{
				if (this.ChildNodeCount > 1 && this[0] != null && this[1] != null)
				{
					if (this[0].DataType.IsSameType(this[1].DataType))
					{
						if (string.CompareOrdinal(_symbol, "+") == 0)
						{
							if (this[0].DataType.IsString)
							{
								return new RaisDataType(typeof(double));
							}
						}
						return this[0].DataType;
					}
					if (typeof(Int64).Equals(this[0].DataType.LibType))
					{
						if (typeof(Int64).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(Int32).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(Int16).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(sbyte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(Int64).Equals(this[1].DataType.LibType))
					{
						if (typeof(Int64).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(Int32).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(Int16).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(sbyte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}

					if (typeof(Int32).Equals(this[0].DataType.LibType))
					{
						if (typeof(Int32).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(Int16).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(sbyte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(Int32).Equals(this[1].DataType.LibType))
					{
						if (typeof(Int32).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(Int16).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(sbyte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}

					if (typeof(Int16).Equals(this[0].DataType.LibType))
					{
						if (typeof(Int16).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(sbyte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(Int16).Equals(this[1].DataType.LibType))
					{
						if (typeof(Int16).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(sbyte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}

					if (typeof(sbyte).Equals(this[0].DataType.LibType))
					{
						if (typeof(sbyte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(sbyte).Equals(this[1].DataType.LibType))
					{
						if (typeof(sbyte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}
					////////////////////////////////////////////
					if (typeof(UInt64).Equals(this[0].DataType.LibType))
					{
						if (typeof(UInt64).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(UInt32).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(UInt16).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(byte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(UInt64).Equals(this[1].DataType.LibType))
					{
						if (typeof(UInt64).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(UInt32).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(UInt16).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(byte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}

					if (typeof(UInt32).Equals(this[0].DataType.LibType))
					{
						if (typeof(UInt32).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(UInt16).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(byte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(UInt32).Equals(this[1].DataType.LibType))
					{
						if (typeof(UInt32).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(UInt16).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(byte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}

					if (typeof(UInt16).Equals(this[0].DataType.LibType))
					{
						if (typeof(UInt16).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(byte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(UInt16).Equals(this[1].DataType.LibType))
					{
						if (typeof(UInt16).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(byte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}

					if (typeof(byte).Equals(this[0].DataType.LibType))
					{
						if (typeof(byte).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
						if (typeof(string).Equals(this[1].DataType.LibType))
						{
							return this[0].DataType;
						}
					}
					if (typeof(byte).Equals(this[1].DataType.LibType))
					{
						if (typeof(byte).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
						if (typeof(string).Equals(this[0].DataType.LibType))
						{
							return this[1].DataType;
						}
					}
					////////////////////////////////////////////
					if (this[0].DataType != null && this[1].DataType != null)
					{
						if (this[0].DataType.LibType != null && this[1].DataType.LibType != null)
						{
							if (this[0].DataType.LibType == this[1].DataType.LibType)
							{
								_dataType = new RaisDataType();
								_dataType.LibType = this[0].DataType.LibType;
							}
						}
					}
				}
				////////////////////////////////////////////
				if (_dataType == null)
				{
					_dataType = new RaisDataType();
					_dataType.LibType = typeof(double);
				}
				return _dataType;
			}
		}
		[Browsable(false)]
		public override RaisDataType CompileDataType
		{
			get
			{
				return base.CompileDataType;
			}
			set
			{
				base.CompileDataType = value;
				int n = ChildNodeCount;
				if (n > 0)
				{
					RaisDataType rt = base.CompileDataType;
					//determine the number type for the constant children
					if (rt.IsInteger)
					{
						for (int i = 0; i < n; i++)
						{
							if (this[i].IsConstant)
							{
								this[i].CompileDataType = rt;
							}
						}
					}
				}
			}
		}
		[Browsable(false)]
		public override string TraceInfo
		{
			get
			{
				return XmlSerialization.FormatString("{0}: [{1}] [{2}] [{3}]", this.GetType().Name, this[0].TraceInfo, _symbol, this[1].TraceInfo);
			}
		}
		[Browsable(false)]
		protected override void OnCloneDataType(MathNode cloned)
		{
			if (_dataType != null)
			{
				BinOperatorNode m = (BinOperatorNode)cloned;
				m._dataType = _dataType.Clone() as RaisDataType;
			}
		}
		protected override void InitializeChildren()
		{
			ChildNodeCount = 2;
		}
		public override void OnReplaceNode(MathNode replaced)
		{
			if (replaced.ChildNodeCount == 2)
			{
				this[0] = replaced[0];
				this[1] = replaced[1];
			}
			else
			{
				this[0] = replaced;
			}
		}
		public override System.Drawing.SizeF OnCalculateDrawSize(System.Drawing.Graphics g)
		{
			SizeF size1 = this[0].CalculateDrawSize(g);
			SizeF size2 = this[1].CalculateDrawSize(g);
			float h = size1.Height;
			if (h < size2.Height)
				h = size2.Height;
			ftParenthesis = this.TextFontMatchHeight(h);
			if (useSymbol)
				ftSymbol = new Font("Symbol", ftParenthesis.Size, ftParenthesis.Style, ftParenthesis.Unit);
			else
				ftSymbol = ftParenthesis;
			SizeF size3 = g.MeasureString(_symbol, ftSymbol);
			float w = size1.Width + size2.Width + size3.Width;
			if (this.Rank() < this.Parent.Rank())
			{
				SizeF size4 = g.MeasureString("(", ftParenthesis);
				w = w + size4.Width + size4.Width;
			}
			return new SizeF(w, h);
		}

		public override void OnDraw(System.Drawing.Graphics g)
		{
			float enclosureWidth = 0;
			float enclosureHeight = 0;
			float yParenthesis = 0;
			SizeF size1 = this[0].CalculateDrawSize(g);
			SizeF size2 = this[1].CalculateDrawSize(g);
			SizeF size3 = g.MeasureString(_symbol, ftSymbol);
			float h = size1.Height;
			if (h < size2.Height)
				h = size2.Height;
			if (h < size3.Height)
				h = size3.Height;
			bool useParenthesis = (this.Rank() < this.Parent.Rank());
			if (useParenthesis)
			{
				SizeF size4 = g.MeasureString("(", ftParenthesis);
				enclosureWidth = size4.Width;
				enclosureHeight = size4.Height;
			}
			float w = size1.Width + size2.Width + size3.Width + enclosureWidth + enclosureWidth;
			if (IsFocused)
			{
				g.FillRectangle(this.TextBrushBKFocus0, new Rectangle(0, 0, (int)w, (int)h));
			}
			float y = 0;
			if (size1.Height < h)
			{
				y = (h - size1.Height) / (float)2;
			}
			float x = size1.Width + size3.Width + enclosureWidth;
			if (useParenthesis)
			{
				if (enclosureHeight < h)
					yParenthesis = (h - enclosureHeight) / (float)2;
				if (IsFocused)
				{
					g.DrawString("(", ftParenthesis, this.TextBrushFocus, new PointF(0, yParenthesis));
				}
				else
				{
					g.DrawString("(", ftParenthesis, this.TextBrush, new PointF(0, yParenthesis));
				}
			}
			//
			System.Drawing.Drawing2D.GraphicsState gt = g.Save();
			g.TranslateTransform(enclosureWidth, y);
			this[0].Position = new Point(this.Position.X + (int)enclosureWidth, Position.Y + (int)y);
			this[0].Draw(g);
			g.Restore(gt);

			y = 0;
			if (size3.Height < h)
			{
				y = (h - size3.Height) / (float)2;
			}
			//draw operator
			OnDrawOperator(g, size1.Width + enclosureWidth, y, size3.Width, size3.Height, h);
			//
			y = 0;
			if (size2.Height < h)
			{
				y = (h - size2.Height) / (float)2;
			}
			gt = g.Save();
			g.TranslateTransform(x, y);
			this[1].Position = new Point(Position.X + (int)x, (int)y + Position.Y);
			this[1].Draw(g);
			g.Restore(gt);
			//
			if (useParenthesis)
			{
				if (IsFocused)
				{
					g.DrawString(")", ftParenthesis, this.TextBrushFocus, new PointF(x + size2.Width, yParenthesis));
				}
				else
				{
					g.DrawString(")", ftParenthesis, this.TextBrush, new PointF(x + size2.Width, yParenthesis));
				}
			}
		}
		protected virtual void OnDrawOperator(Graphics g, float x, float y, float w, float h1, float h)
		{
			if (IsFocused)
			{
				g.FillRectangle(this.TextBrushBKFocus, new Rectangle((int)x, 0, (int)w, (int)h));
				g.DrawString(_symbol, ftSymbol, this.TextBrushFocus, x, y);
			}
			else
				g.DrawString(_symbol, ftSymbol, this.TextBrush, x, y);
		}
		public override bool Update(string newValue)
		{
			bool bRet = true;
			if (!string.IsNullOrEmpty(newValue))
			{
				MathNode node = null;
				char c = newValue[0];
				if (c == '+')
				{
					if (!(this is PlusNode))
					{
						node = new PlusNode(this.Parent);
					}
				}
				else if (c == '-')
				{
					if (!(this is MinusNode))
					{
						node = new MinusNode(this.Parent);
					}
				}
				else if (c == '/')
				{
					if (!(this is DivNode))
					{
						node = new DivNode(this.Parent);
					}
				}
				else if (c == '*')
				{
					if (!(this is MultiplyNode))
					{
						node = new MultiplyNode(this.Parent);
					}
				}
				if (node != null)
				{
					root.SaveCurrentStateForUndo();
					this[0].Parent = node;
					this[1].Parent = node;
					int n = this.Parent.ChildNodeCount;
					for (int i = 0; i < n; i++)
					{
						if (this.Parent[i] == this)
						{
							this.Parent[i] = node;
							break;
						}
					}
					node[0] = this[0];
					node[1] = this[1];
					root.SetFocus(node);
					root.FireChanged(node);
					bRet = false;
				}
			}
			return bRet;
		}
		public override string ToString()
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1} {2}", this[0].ToString(), _symbol, this[1].ToString());
		}
	}
	[Description("computes the sum of its two operands, in double precisions")]
	[ToolboxBitmapAttribute(typeof(PlusNode), "Resources.PlusNode.bmp")]
	public class PlusNode : BinOperatorNode
	{
		public PlusNode(MathNode parent)
			: base(parent)
		{
			_symbol = "+";
		}
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.Add;
			}
		}
	}
	[Description("computes the product of its operands, in double precisions")]
	[ToolboxBitmapAttribute(typeof(MultiplyNode), "Resources.MultiplyNode.bmp")]
	public class MultiplyNode : BinOperatorNode
	{
		public MultiplyNode(MathNode parent)
			: base(parent)
		{
			_symbol = "*";
		}
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.Multiply;
			}
		}
		public override int Rank() { return 1; }
		protected override void OnDrawOperator(Graphics g, float x, float y, float w, float h1, float h)
		{
			float r = w / (float)3;
			if (r > h)
				r = h;
			float x0 = x;
			if (r < w)
				x0 = x + (w - r) / (float)2;
			float y0 = 0;
			if (r < h)
				y0 = (h - r) / (float)2;
			Rectangle rc = new Rectangle((int)x0, (int)y0, (int)r, (int)r);
			if (IsFocused)
			{
				g.FillRectangle(this.TextBrushBKFocus, new Rectangle((int)x, 0, (int)w, (int)h));
				g.FillEllipse(this.TextBrushFocus, rc);
			}
			else
			{
				g.FillEllipse(this.TextBrush, rc);
			}
		}
	}
	[MathNodeCategoryAttribute(enumOperatorCategory.Integer)]
	[Description("Produces the full product of two 32-bit numbers, and generates a 64-bit number")]
	[ToolboxBitmapAttribute(typeof(MultiplyNodeBig), "Resources.MultiplyNodeBig.bmp")]
	public class MultiplyNodeBig : MultiplyNode
	{
		public MultiplyNodeBig(MathNode parent)
			: base(parent)
		{
		}
		private RaisDataType _dataType;
		public override RaisDataType DataType
		{
			get
			{
				if (_dataType == null)
				{
					_dataType = new RaisDataType();
					_dataType.LibType = typeof(Int64);
				}
				return _dataType;
			}
		}
		public override System.CodeDom.CodeExpression ExportCode(IMethodCompile method)//CodeStatementCollection supprtStatements)
		{
			MathNode.Trace("{0}.ExportCode", this.GetType().Name);
			CodeMethodReferenceExpression mr = new CodeMethodReferenceExpression();
			mr.MethodName = "BigMul";
			mr.TargetObject = new CodeTypeReferenceExpression(typeof(Math));
			int n = ChildNodeCount;
			CodeExpression[] ps = new CodeExpression[n];
			for (int i = 0; i < n; i++)
			{
				ps[i] = this[i].ExportCode(method);
				if (!this[i].DataType.Type.Equals(typeof(int)))
				{
					ps[i] = new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(Convert)), "ToInt32",
						new CodeExpression[] { ps[i] });
				}
			}
			return new CodeMethodInvokeExpression(
				mr,
				ps);
		}
	}
	[Description("subtracts the second operand from the first, in double precisions")]
	[ToolboxBitmapAttribute(typeof(MinusNode), "Resources.MinusNode.bmp")]
	public class MinusNode : BinOperatorNode
	{
		public MinusNode(MathNode parent)
			: base(parent)
		{
			_symbol = "-";
		}
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.Subtract;
			}
		}
	}
	[Description("divides its first operand by its second, in double precisions")]
	[ToolboxBitmapAttribute(typeof(DivNode), "Resources.DivNode.bmp")]
	public class DivNode : BinOperatorNode
	{
		private bool _sameline;
		public DivNode(MathNode parent)
			: base(parent)
		{
			_symbol = "/";
		}
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.Divide;
			}
		}
		public override object CloneExp(MathNode parent)
		{
			DivNode node = (DivNode)base.CloneExp(parent);
			node.SameLine = _sameline;
			return node;
		}

		public bool SameLine
		{
			get
			{
				return _sameline;
			}
			set
			{
				if (_sameline != value)
				{
					_sameline = value;
				}
			}
		}
		protected override void OnSave(XmlNode node)
		{
			XmlAttribute xa = node.OwnerDocument.CreateAttribute("sameline");
			xa.Value = _sameline.ToString();
			node.Attributes.Append(xa);
		}
		protected override void OnLoad(XmlNode node)
		{
			XmlAttribute xa = node.Attributes["sameline"];
			if (xa != null)
			{
				_sameline = bool.Parse(xa.Value);
			}
		}
		private float symbolWidth(float height)
		{
			return (float)(height * Math.Tan(25.0 * Math.PI / 180.0) * 2.0 / 3.0);
		}
		public override int Rank() { if (_sameline) return 1; else return 0; }
		public override System.Drawing.SizeF OnCalculateDrawSize(System.Drawing.Graphics g)
		{
			if (_sameline)
			{
				return base.OnCalculateDrawSize(g);
			}
			else
			{
				SizeF size1 = this[0].CalculateDrawSize(g);
				SizeF size2 = this[1].CalculateDrawSize(g);
				float w = size1.Width;
				if (w < size2.Width)
					w = size2.Width;
				float h = size1.Height + size2.Height + 8;
				if (this.Rank() < this.Parent.Rank())
				{
					ftParenthesis = this.TextFontMatchHeight(h);
					SizeF size4 = g.MeasureString("(", ftParenthesis);
					w = w + size4.Width + size4.Width;
				}
				return new SizeF(w, h);
			}
		}

		public override void OnDraw(System.Drawing.Graphics g)
		{
			if (_sameline)
			{
				base.OnDraw(g);
			}
			else
			{
				float enclosureWidth = 0;
				SizeF size1 = this[0].CalculateDrawSize(g);
				SizeF size2 = this[1].CalculateDrawSize(g);
				bool useParenthesis = (this.Rank() < this.Parent.Rank());
				if (useParenthesis)
				{
					SizeF size4 = g.MeasureString("(", ftParenthesis);
					enclosureWidth = size4.Width;
				}
				//
				float w = size1.Width;
				if (w < size2.Width)
					w = size2.Width;
				float h = size1.Height + size2.Height + 8;
				//
				if (IsFocused)
				{
					g.FillRectangle(this.TextBrushBKFocus0, new Rectangle(0, 0, (int)(w + enclosureWidth + enclosureWidth), (int)h));
				}
				//
				if (useParenthesis)
				{
					if (IsFocused)
					{
						g.DrawString("(", ftParenthesis, this.TextBrushFocus, new PointF(0, 0));
					}
					else
					{
						g.DrawString("(", ftParenthesis, this.TextBrush, new PointF(0, 0));
					}
				}
				//
				float x = 0;
				if (size1.Width < w)
				{
					x = (w - size1.Width) / (float)2;
				}
				System.Drawing.Drawing2D.GraphicsState gt = g.Save();
				g.TranslateTransform(x + enclosureWidth, 0);
				this[0].Position = new Point((int)(x + enclosureWidth) + Position.X, Position.Y);
				this[0].Draw(g);
				g.Restore(gt);

				if (IsFocused)
				{
					g.FillRectangle(this.TextBrushBKFocus, new Rectangle((int)enclosureWidth, (int)size1.Height, (int)(w + enclosureWidth), 6));
					g.DrawLine(this.TextPenFocus, enclosureWidth, size1.Height + 3, w + enclosureWidth, size1.Height + 3);
				}
				else
					g.DrawLine(this.TextPen, enclosureWidth, size1.Height + 3, w + enclosureWidth, size1.Height + 3);
				x = 0;
				if (size2.Width < w)
				{
					x = (w - size2.Width) / (float)2;
				}
				//
				gt = g.Save();
				g.TranslateTransform(x + enclosureWidth, size1.Height + 6);
				this[1].Position = new Point((int)(x + enclosureWidth) + Position.X, (int)(size1.Height + 6) + Position.Y);
				this[1].Draw(g);
				g.Restore(gt);
				//
				if (useParenthesis)
				{
					if (IsFocused)
					{
						g.DrawString(")", ftParenthesis, this.TextBrushFocus, new PointF(x + size2.Width + enclosureWidth, 0));
					}
					else
					{
						g.DrawString(")", ftParenthesis, this.TextBrush, new PointF(x + size2.Width + enclosureWidth, 0));
					}
				}
			}
		}
	}
}
