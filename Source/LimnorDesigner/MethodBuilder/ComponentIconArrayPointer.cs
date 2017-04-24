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
using System.ComponentModel;
using LimnorDesigner.MenuUtil;
using LimnorDesigner.Action;

namespace LimnorDesigner.MethodBuilder
{
	/// <summary>
	/// represent an array icon
	/// </summary>
	public class ComponentIconArrayPointer : ComponentIconLocal
	{
		public ComponentIconArrayPointer()
			: base()
		{
		}
		public ComponentIconArrayPointer(MethodClass method)
			: base(method)
		{
		}
		public ComponentIconArrayPointer(ActionBranch action)
			: this(action.Method)
		{
		}
		public ComponentIconArrayPointer(ILimnorDesigner designer, ArrayVariable pointer, MethodClass method)
			: base(designer, pointer, method)
		{
		}
		public int GetArrayDimension()
		{
			ArrayVariable v = Variable;
			if (v != null)
			{
				ArrayPointer ap = v.ValueType as ArrayPointer;
				if (ap != null)
				{
					int[] dims = ap.Dimnesions;
					if (dims != null && dims.Length == 1)
					{
						return dims[0];
					}
				}
			}
			return 0;
		}
		public void SetArrayDimension(int n)
		{
			ArrayVariable v = Variable;
			if (v != null)
			{
				ArrayPointer ap = v.ValueType as ArrayPointer;
				if (ap != null)
				{
					int[] dims = ap.Dimnesions;
					if (dims != null && dims.Length == 1)
					{
						dims[0] = n;
					}
				}
			}
		}
		protected override LimnorContextMenuCollection GetMenuData()
		{
			IClassWrapper a = Variable;
			if (a == null)
			{
				throw new DesignerException("Calling GetMenuData without an array variable");
			}
			return LimnorContextMenuCollection.GetMenuCollection(a);
		}
		protected override IList<PropertyDescriptor> OnGetProperties(Attribute[] attrs)
		{
			ArrayVariable v = Variable;
			if (v != null)
			{
				ArrayPointer ap = v.ValueType as ArrayPointer;
				if (ap != null)
				{
					List<PropertyDescriptor> ps = new List<PropertyDescriptor>();
					ps.Add(new PropertyDescriptorArrayDim(this, "ArrayLength", attrs));
					return ps;
				}
			}
			return null;
		}
		public ArrayVariable Variable
		{
			get
			{
				return this.ClassPointer as ArrayVariable;
			}
		}
	}
	class PropertyDescriptorArrayDim : PropertyDescriptor
	{
		private ComponentIconArrayPointer _owner;
		public PropertyDescriptorArrayDim(ComponentIconArrayPointer owner, string name, Attribute[] attrs)
			: base(name, attrs)
		{
			_owner = owner;
		}
		public override bool CanResetValue(object component)
		{
			return false;
		}

		public override Type ComponentType
		{
			get { return typeof(ComponentIconArrayPointer); }
		}

		public override object GetValue(object component)
		{
			return _owner.GetArrayDimension();
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override Type PropertyType
		{
			get { return typeof(int); }
		}

		public override void ResetValue(object component)
		{
		}

		public override void SetValue(object component, object value)
		{
			int n = Convert.ToInt32(value);
			_owner.SetArrayDimension(n);
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}

	}
}
