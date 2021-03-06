﻿/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Object Builder Utility
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace VPL
{
	public interface ITypedNamedValuesHolder
	{
		StringCollection GetValueNames();
		bool CreateTypedNamedValue(string name, Type type);
		bool RenameTypedNamedValue(string oldName, string name, Type type);
		bool DeleteTypedNamedValue(string name);
		TypedNamedValue GetTypedNamedValueByName(string name);
	}
}
