/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	XML Utility
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Xml;

namespace XmlUtility
{
	/// <summary>
	/// to determine which properties to be read first. small order read first
	/// </summary>
	public class PropertyReadOrderAttribute : Attribute
	{
		private bool _exclude;
		private UInt32 _order;
		/// <summary>
		/// reading is in ascendent order. properties without this property will always be read first
		/// </summary>
		/// <param name="order">the value must be unique; the reading-order is the order plus property index plus property count, if this attribute presents; otherwise the order is the property index.</param>
		public PropertyReadOrderAttribute(UInt32 order)
		{
			_order = order;
		}
		public PropertyReadOrderAttribute(UInt32 order, bool exclude)
		{
			_order = order;
			_exclude = exclude;
		}
		public bool Exclude
		{
			get
			{
				return _exclude;
			}
		}
		public UInt32 ReadOrder
		{
			get
			{
				return _order;
			}
		}
	}
	public class PropertyReadOrder
	{
		private bool _exclude;
		private UInt32 _order;
		private PropertyDescriptor _property;
		private XmlNode _node;
		public PropertyReadOrder(PropertyDescriptor property, XmlNode node, UInt32 idx, UInt32 count)
		{
			_property = property;
			_node = node;
			_order = idx;
			if (property.Attributes != null)
			{
				foreach (Attribute a in property.Attributes)
				{
					PropertyReadOrderAttribute pr = a as PropertyReadOrderAttribute;
					if (pr != null)
					{
						_order = pr.ReadOrder + count;
						_exclude = pr.Exclude;
						break;
					}
				}
			}
		}
		public void SetOrder(UInt32 order)
		{
			_order = order;
		}
		public UInt32 ReadOrder
		{
			get
			{
				return _order;
			}
		}
		public bool Exclude
		{
			get
			{
				return _exclude;
			}
		}
		public PropertyDescriptor Property
		{
			get
			{
				return _property;
			}
		}
		public XmlNode Node
		{
			get
			{
				return _node;
			}
		}
	}
}
