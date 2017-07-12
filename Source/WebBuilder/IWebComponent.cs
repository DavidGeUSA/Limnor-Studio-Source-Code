/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Web Project Support
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.ComponentModel;
using System.Collections.Specialized;
using VPL;
using System.Windows.Forms;
using System.Drawing;

namespace Limnor.WebBuilder
{
	public interface IWebClientElementGetter
	{
		void SetElementGetter(string getter);
	}
	/// <summary>
	/// implemented by server components
	/// </summary>
	public interface IWebClientSupport
	{
		string GetJavaScriptWebMethodReferenceCode(string ownerCodeName, string methodName, StringCollection code, StringCollection parameters);
		string GetJavaScriptWebPropertyReferenceCode(StringCollection method, string propertyName, string[] parameters);
	}
	public interface IWebResourceFileUser
	{
		IList<WebResourceFile> GetResourceFiles();
		bool IsParameterFilePath(string parameterName);
		string CreateWebFileAddress(string localFilePath, string parameterName);
	}
	public interface IWebClientComponent : IWebResourceFileUser, IWebClientSupport
	{
		/// <summary>
		/// only support isStatic is true
		/// </summary>
		/// <param name="isStatic"></param>
		/// <returns></returns>
		MethodInfo[] GetWebClientMethods(bool isStatic);
		void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver);
		string GetJavaScriptReferenceCode(StringCollection method, string attributeName, string[] parameters);
		string MapJavaScriptCodeName(string name);
		string MapJavaScriptVallue(string name, string value);
		void SetCodeName(string vname);
		//
		WebClientValueCollection CustomValues { get; }
		//runtime support
		[WebClientMember]
		string id { get; }
		[WebClientMember]
		string tagName { get; }
		[WebClientMember]
		string tag { get; set; }
		[WebClientMember]
		int Opacity { get; set; }
		[WebClientMember]
		int zOrder { get; set; }
		[WebClientMember]
		int clientWidth { get; }
		[WebClientMember]
		int clientHeight { get; }
		[WebClientMember]
		string innerHTML { get; set; }
		[WebClientMember]
		int offsetHeight { get; }
		[WebClientMember]
		int offsetWidth { get; }
		[WebClientMember]
		int offsetLeft { get; }
		[WebClientMember]
		int offsetTop { get; }
		[WebClientMember]
		int scrollHeight { get; }
		[WebClientMember]
		int scrollLeft { get; }
		[WebClientMember]
		int scrollTop { get; }
		[WebClientMember]
		int scrollWidth { get; }
		[WebClientMember]
		bool Visible { get; set; }
		[WebClientMember]
		Color BackColor { get; set; }
		[WebClientMember]
		Color ForeColor { get; set; }
		[WebClientMember]
		void SetOrCreateNamedValue(string name, string value);
		[WebClientMember]
		string GetNamedValue(string name);
		[WebClientMember]
		IWebClientComponent[] getElementsByTagName(string tagName);
		[WebClientMember]
		IWebClientComponent[] getDirectChildElementsByTagName(string tagName);
	}
	public interface ICustomSize
	{
	}
	public interface IWebBox
	{
		WebElementBox Box { get; set; }
	}
	public interface IWebClientAlternative
	{
		string RuntimeID { get; }
	}
	public interface IWebClientControl : IWebClientComponent, IXmlNodeHolder, IWebClientControlBase
	{
		EventInfo[] GetWebClientEvents(bool isStatic);
		PropertyDescriptorCollection GetWebClientProperties(bool isStatic);
		void CreateHtmlContent(XmlNode node, EnumWebElementPositionType positionType, int groupId);
		[WebClientMember]
		void SwitchEventHandler(string eventName, VplMethodPointer handler);
		[WebClientMember]
		void Print();
		[WebClientMember]
		void setStyle(string styleName, string styleValue);
		string ElementName { get; }
		string CodeName { get; }
		[WebClientMember]
		string className { get; set; }
		[WebClientMember]
		EnumWebCursor cursor { get; set; }
		[WebClientMember]
		EnumTextAlign textAlign { get; set; }
		Dictionary<string, string> HtmlParts { get; }
		[WebClientMember]
		AnchorStyles PositionAnchor { get; set; }
		[WebClientMember]
		ContentAlignment PositionAlignment { get; set; }
		SizeType WidthType { get; set; }
		SizeType HeightType { get; set; }
		uint WidthInPercent { get; set; }
		uint HeightInPercent { get; set; }
		//
		[WebClientMember]
		event WebControlMouseEventHandler onclick;
		[WebClientMember]
		event WebControlMouseEventHandler ondblclick;
		//
		[WebClientMember]
		event WebControlMouseEventHandler onmousedown;
		[WebClientMember]
		event WebControlMouseEventHandler onmouseup;
		[WebClientMember]
		event WebControlMouseEventHandler onmouseover;
		[WebClientMember]
		event WebControlMouseEventHandler onmousemove;
		[WebClientMember]
		event WebControlMouseEventHandler onmouseout;
		//
		[WebClientMember]
		event SimpleCall onAdjustAnchorAlign;
	}
	public interface IWebClientControlCustomEvents : IWebClientControl
	{
	}
	public interface IWebClientPropertySetter
	{
		/// <summary>
		/// if it return true then the compiler does not add property setting code. 
		/// OnSetProperty can be used to add custom code for setting the property
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		bool UseCustomSetter(string propertyName);
		/// <summary>
		/// after setting a property, Javascript code may be added to make the new property value take effects
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <param name="sc"></param>
		void OnSetProperty(string propertyName, string value, StringCollection sc);
		/// <summary>
		/// if the value is a file name then add the file to the web resources
		/// and return the relative path and filename.
		/// returns null if a conversion is not needed.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		string ConvertSetPropertyActionValue(string propertyName, string value);
	}
	public interface IWebClientPropertyCustomSetter
	{
		/// <summary>
		/// change normal "property"="value" to other js code
		/// </summary>
		/// <param name="ownerCode"></param>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <param name="sc"></param>
		/// <returns>true:custom script is generated, normal compiling should bail out</returns>
		bool CreateSetPropertyJavaScript(string ownerCode, string propertyName, string value, StringCollection sc);
	}
	public interface IUseJavascriptFiles
	{
		IList<string> GetJavascriptFiles();
	}
	public interface IUseCssFiles
	{
		IList<string> GetCssFiles();
	}
	public interface IUseDatetimePicker
	{
		bool UseDatetimePicker { get; }
	}
	public interface IInnerHtmlEdit
	{
		EditContents HtmlContents { get; }
	}
	public interface IHtmlElement
	{
		void OnSetProperty(string propName);
	}
	public interface IWebClientPropertyConverter
	{
		string GetJavaScriptPropertyCode(string propertyName, string subPropertyName);
	}
}
