/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Object Builder Utility
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace VPL
{
	public interface IBeforeListItemSerialize
	{
		/// <summary>
		/// determine whether to save an item of a property. the property is a list or collection
		/// </summary>
		/// <param name="node"></param>
		/// <param name="propertyName"></param>
		/// <param name="item">item of the property</param>
		/// <returns>true:save; false:not save</returns>
		bool OnBeforeItemSerialize(XmlNode node, string propertyName, object item);
	}
	public interface IWebClientControlBase
	{
	}
	public interface IControlDeserialize
	{
		/// <summary>
		/// use this event to cache properties which could be altered during adding the control to Controls
		/// </summary>
		void OnDeserialized();
		/// <summary>
		/// use this event to restore cached properties after adding the control to Controls
		/// </summary>
		void OnAddedToControls();
	}
}
