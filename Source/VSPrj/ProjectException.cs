﻿/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Limnor Studio Project
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace VSPrj
{
	public class ProjectException : Exception
	{
		public ProjectException(string msg, params object[] values)
			: base(string.Format(msg, values))
		{
		}
	}
}
