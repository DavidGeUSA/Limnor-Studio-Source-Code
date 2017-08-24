/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Object Builder Utility
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace VPL
{
	public interface IClassId
	{
		UInt32 ClassId { get; }
		object ObjectInstance { get; }
		bool IsWebPage { get; }
	}
	/// <summary>
	/// implemented by ClassPointer
	/// </summary>
	public interface IClassPointer : IClassId
	{
		object GetOwnerMethod(UInt32 methodId);
		IGuidIdentified GetElementByGuid(Guid g);
		void SaveHtmlElements(IXmlCodeWriter xw);
	}
	public interface IOwnedObject
	{
		IClassPointer Owner { get; set; }
	}
	public interface IDevClass : IClassId
	{
		void NotifyChange(object obj, string member);
		void NotifyBeforeChange(object obj, string member);
	}
	public interface IDevClassReferencer
	{
		void SetDevClass(IDevClass c);
		IDevClass GetDevClass();
	}
	public interface IDevClassReferencerHolder
	{
		IDevClassReferencer DevClass { get; }
	}
	public interface IGuidIdentified
	{
		Guid ElementGuid { get; }
	}
	public interface IComponentID
	{
		UInt32 ComponentId { get; }
		string ComponentName { get; }
	}
	public interface INonHostedObjectsHolder
	{
		string GetNonHostedCodeName(object v);
		string GetNonHostedKeyName(object v);
		object GetNonHostedObjectByKeyName(string keyName);
	}
}
