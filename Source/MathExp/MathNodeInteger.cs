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
using System.CodeDom;
using System.ComponentModel;
using MathExp.RaisTypes;
using VPL;
using System.Drawing.Design;

namespace MathExp
{
	public abstract class MathNodeInteger : MathNode
	{
		public MathNodeInteger(MathNode parent)
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
		[Editor(typeof(UITypeEditorTypeSelector), typeof(UITypeEditor))]
		public virtual RaisDataType VariableType
		{
			get
			{
				return DataType;
			}
			set
			{
				if (value != null && value.IsInteger)
				{
					_dataType = value;
				}
			}
		}
		[Browsable(false)]
		protected override void OnCloneDataType(MathNode cloned)
		{
			if (_dataType != null)
			{
				MathNodeInteger m = (MathNodeInteger)cloned;
				m._dataType = _dataType.Clone() as RaisDataType;
			}
		}
		public override MathNode CreateDefaultNode(int i)
		{
			return new IntegerVariable(this);
		}
	}
	[MathNodeCategory(enumOperatorCategory.Integer)]
	[Description("an integer variable")]
	[ToolboxBitmap(typeof(IntegerVariable), "Resources.IntegerVariable.bmp")]
	public class IntegerVariable : MathNodeVariable
	{
		public IntegerVariable(MathNode parent)
			: base(parent)
		{
			VariableType = new RaisDataType();
			VariableType.LibType = typeof(int);
		}
	}
	public abstract class MathNodeIntegerBin : BinOperatorNode
	{
		public MathNodeIntegerBin(MathNode parent)
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
					_dataType = base.DataType;
					if (_dataType == null)
					{
						_dataType = new RaisDataType();
						_dataType.LibType = typeof(Int64);
					}
					else
					{
						if (_dataType.IsLibType && !_dataType.IsInteger)
						{
							_dataType.LibType = typeof(Int64);
						}
					}
				}
				return _dataType;
			}
		}
		[Editor(typeof(UITypeEditorTypeSelector), typeof(UITypeEditor))]
		public virtual RaisDataType VariableType
		{
			get
			{
				return DataType;
			}
			set
			{
				if (value != null && value.IsInteger)
				{
					_dataType = value;
				}
			}
		}
		[Browsable(false)]
		protected override void OnCloneDataType(MathNode cloned)
		{
			if (_dataType != null)
			{
				MathNodeIntegerBin m = (MathNodeIntegerBin)cloned;
				m._dataType = _dataType.Clone() as RaisDataType;
			}
		}
		public override MathNode CreateDefaultNode(int i)
		{
			return new IntegerVariable(this);
		}
		public override CodeExpression ExportCode(IMethodCompile method)
		{
			MathNode.Trace("{0}.ExportCode", this.GetType().Name);
			CodeExpression e1 = this[0].ExportCode(method);
			CodeExpression e2 = this[1].ExportCode(method);
			return new CodeBinaryOperatorExpression(e1, operaterType, e2);
		}
	}
	[MathNodeCategoryAttribute(enumOperatorCategory.Integer)]
	[Description("computes the remainder of dividing its first operand by its second")]
	[ToolboxBitmapAttribute(typeof(Modulus), "Resources.Modulus.bmp")]
	public class Modulus : MathNodeIntegerBin
	{
		public Modulus(MathNode parent)
			: base(parent)
		{
			_symbol = "%";
		}
		public override int Rank() { return 1; }
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.Modulus;
			}
		}
	}
	[MathNodeCategoryAttribute(enumOperatorCategory.Integer)]
	[Description("Bitwise 'and' operator")]
	[ToolboxBitmapAttribute(typeof(MathNodeBitAnd), "Resources.MathNodeBitAnd.bmp")]
	public class MathNodeBitAnd : MathNodeIntegerBin
	{
		public MathNodeBitAnd(MathNode parent)
			: base(parent)
		{
			_symbol = "&";
		}
		public override int Rank() { return 1; }
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.BitwiseAnd;
			}
		}
	}
	[MathNodeCategoryAttribute(enumOperatorCategory.Integer)]
	[Description("Bitwise 'or' operator")]
	[ToolboxBitmapAttribute(typeof(MathNodeBitOr), "Resources.MathNodeBitOr.bmp")]
	public class MathNodeBitOr : MathNodeIntegerBin
	{
		public MathNodeBitOr(MathNode parent)
			: base(parent)
		{
			_symbol = "|";
		}
		public override int Rank() { return 1; }
		public override CodeBinaryOperatorType operaterType
		{
			get
			{
				return CodeBinaryOperatorType.BitwiseOr;
			}
		}
	}

}
