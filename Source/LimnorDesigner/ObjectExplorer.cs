/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Xml;
using XmlSerializer;
using System.ComponentModel;
using VSPrj;
using System.Collections.Specialized;
using MathExp;
using Parser;
using LimnorDesigner.MethodBuilder;
using ProgElements;
using XmlUtility;
using VPL;
using System.Collections.ObjectModel;
using LimnorDesigner.Property;
using LimnorDesigner.Action;
using LimnorDesigner.Event;
using System.Linq;
using LimnorDesigner.Interface;
using Limnor.Drawing2D;
using System.Collections;
using TraceLog;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Data;
using LimnorDesigner.ResourcesManager;
using System.Globalization;
using Limnor.WebBuilder;
using Limnor.Windows;
using Limnor.WebServerBuilder;
using WindowsUtility;
using Limnor.Remoting.Host;
using Limnor.PhpComponents;
using LimnorDesigner.Web;
using LimnorVisualProgramming;
using LimnorDesigner.DesignTimeType;
using LimnorKiosk;
#if DOTNET40
using System.Numerics;
#endif
using System.IO;

namespace LimnorDesigner
{
	public interface IEventNode
	{
		IEvent OwnerEvent { get; }
	}
	public interface ICustomPropertyHolder
	{
		List<PropertyClass> GetCustomProperties();
		List<MethodClass> GetCustomMethods();
		List<EventClass> GetCustomEvents();
		UInt32 ClassId { get; }
		UInt32 MemberId { get; }
		IClass Holder { get; }
	}
	public interface IRootNode
	{
		void NotifyChanges();
		MultiPanes ViewersHolder { get; }
		Dictionary<UInt32, IAction> GetActions();
		IAction GetAction(UInt64 id);
		bool IncludeStaticOnly { get; }
		RootComponentData ClassData { get; }
		EventAction GetEventHandler(IEvent ep);
		bool HasEventHandler(IEvent ep);
		bool IsStatic { get; }
	}
	public interface ITreeNodeClass
	{
		void ResetPropertyNode(bool forStatic, bool forCustom);
		void ResetMethodNode(bool forStatic, bool forCustom);
		void ResetEventNode(bool forStatic, bool forCustom);
		IObjectPointer OwnerPointer { get; }
		TreeNodeAction FindActionNode(IAction act);
		UInt32 ClassId { get; }
		TreeNodePME GetMemberNode(bool isStatic);
		void HandleActionModified(UInt32 classId, IAction act, bool isNewAction);
	}
	public interface ITreeNodeBreakPoint
	{
		void ShowBreakPoint(bool pause);
	}
	public interface ITypeNode : ICloneable
	{
		Type OwnerDataType { get; }
		void HandleActionModified(UInt32 classId, IAction act, bool isNewAction);
	}
	public enum EnumActionMethodType { Unknown, Instance, Static }
	public class BoolEventArgs : EventArgs
	{
		public BoolEventArgs()
		{
		}
		public BoolEventArgs(bool result)
		{
			Result = result;
		}
		public bool Result { get; set; }
	}
	public delegate void fnCheckNode(TreeNodeObject node, BoolEventArgs e);
	public class TreeViewObjectExplorer : TreeView
	{
		#region icon constants
		public static int IMG_OK;
		public static int IMG_CANCEL;
		public static int IMG_ARROWLT;
		public static int IMG_DEFICON;
		public static int IMG_VOID;
		public static int IMG_CONSOLE;
		public static int IMG_ATTRIBUTES;
		public static int IMG_PROPERTIES;
		public static int IMG_PROPERTIES_WITHACTIONS;
		public static int IMG_METHODS;
		public static int IMG_METHODS_WITHACTS;
		public static int IMG_EVENTS;
		public static int IMG_EVENT;
		public static int IMG_EVENT_WITHHANDLER;
		public static int IMG_EVENT_WITHHANDLERS;
		public static int IMG_PROPERTY;
		public static int IMG_PROPERTY_READONLY;
		public static int IMG_PROPERTY_WITHACTS;
		public static int IMG_PROPERTY_CLIENT;
		public static int IMG_PROPERTY_SERVER;
		public static int IMG_PROPERTY_CLIENT_ACT;
		public static int IMG_PROPERTY_SERVER_ACT;

		public static int IMG_PROPERTY_OVERRIDE;
		public static int IMG_PROPERTY_OVERRIDE_BASE;
		public static int IMG_PROPERTY_OVERRIDE_PROTECTED;
		public static int IMG_PROPERTY_OVERRIDE_BASE_PROTECTED;
		public static int IMG_PROPERTY_VIRTUAL;
		public static int IMG_PROPERTY_VIRTUAL_PROTECTED;

		public static int IMG_PROPERTY_OVERRIDE_PROTECTED_A;
		public static int IMG_PROPERTY_VIRTUAL_A;
		public static int IMG_PROPERTY_VIRTUAL_PROTECTED_A;
		public static int IMG_PROPERTY_OVERRIDE_BASE_PROTECTED_A;
		public static int IMG_PROPERTY_OVERRIDE_BASE_A;
		public static int IMG_PROPERTY_OVERRIDE_A;

		public static int IMG_METHOD_OVERRIDE;
		public static int IMG_METHOD_OVERRIDE_BASE;
		public static int IMG_METHOD_OVERRIDE_PROTECTED;
		public static int IMG_METHOD_OVERRIDE_BASE_PROTECTED;
		public static int IMG_METHOD_VIRTUAL;
		public static int IMG_METHOD_VIRTUAL_PROTECTED;

		public static int IMG_METHOD_OVERRIDE_A;
		public static int IMG_METHOD_OVERRIDE_BASE_A;
		public static int IMG_METHOD_OVERRIDE_PROTECTED_A;
		public static int IMG_METHOD_OVERRIDE_BASE_PROTECTED_A;
		public static int IMG_METHOD_VIRTUAL_A;
		public static int IMG_METHOD_VIRTUAL_PROTECTED_A;

		public static int IMG_METHOD_WEB;
		public static int IMG_METHOD_WEB_ACT;

		public static int IMG_METHOD;
		public static int IMG_METHOD_WEBCLIENT;
		public static int IMG_METHOD_WEBSERVER;
		public static int IMG_METHOD_WEBCLIENT_ACT;
		public static int IMG_METHOD_WEBSERVER_ACT;
		public static int IMG_METHOD_ACTS;
		public static int IMG_ACTION;
		public static int IMG_ACTION_INVALID;
		public static int IMG_ACTION_UNUSED;
		public static int IMG_ACTIONCOLLECTION;
		public static int IMG_ACTIONGROUP;
		public static int IMG_A_ACTIONGROUP;
		public static int IMG_ACTIONGROUPCOLLECTION;
		public static int IMG_PARAM;
		public static int IMG_OBJECTS;
		public static int IMG_OBJECT;
		public static int IMG_enumValues;
		public static int IMG_enumValue;
		public static int IMG_MessageBox;
		public static int IMG_TypeCollection;
		public static int IMG_BreakPoint;
		public static int IMG_BreakPointPause;
		public static int IMG_Assembly;
		public static int IMG_Type;
		public static int IMG_Interface;
		public static int IMG_c_Interface;
		public static int IMG_Interface2;
		public static int IMG_Interfaces;
		public static int IMG_Constructor;
		public static int IMG_Constructor1;
		public static int IMG_Constructors;
		public static int IMG_Attributes;
		public static int IMG_Attribute;
		public static int IMG_MethodReturn;
		//
		public static int IMG_ActRet;
		//
		//static 
		public static int IMG_S_ATTRIBUTES;
		public static int IMG_S_PROPERTIES;
		public static int IMG_S_PROPERTIES_WITHACTIONS;
		public static int IMG_S_METHODS;
		public static int IMG_S_METHODS_WITHACTS;
		public static int IMG_S_EVENTS;
		public static int IMG_S_EVENT;
		public static int IMG_S_EVENT_WITHHANDLER;
		public static int IMG_S_EVENT_WITHHANDLERS;
		public static int IMG_S_PROPERTY;
		public static int IMG_S_PROPERTY_READONLY;
		public static int IMG_S_PROPERTY_WITHACTS;
		public static int IMG_S_METHOD;
		public static int IMG_S_METHOD_ACTS;
		public static int IMG_S_ACTION;
		public static int IMG_S_ACTIONCOLLECTION;
		public static int IMG_S_ACTIONGROUPCOLLECTION;
		public static int IMG_S_ACTIONGROUP;
		public static int IMG_SA_ACTIONGROUP;
		public static int IMG_S_OBJECTS;
		//
		//customer
		public static int IMG_C_OBJECTS;
		public static int IMG_SC_OBJECTS;
		public static int IMG_C_PROPERTIES;
		public static int IMG_CA_PROPERTIES;
		public static int IMG_SC_PROPERTIES;
		public static int IMG_SCA_PROPERTIES;
		public static int IMG_C_PROPERTY;
		public static int IMG_CA_PROPERTY;
		public static int IMG_SCA_PROPERTY;
		public static int IMG_SCBA_PROPERTY;
		public static int IMG_BC_PROPERTY;
		public static int IMG_BCA_PROPERTY;
		public static int IMG_SBC_PROPERTY;
		public static int IMG_SC_PROPERTY;
		public static int IMG_C_METHODS;
		public static int IMG_CA_METHODS;
		public static int IMG_SC_METHODS;
		public static int IMG_SCA_METHODS;
		public static int IMG_C_METHOD;
		public static int IMG_CO_METHOD;
		public static int IMG_SC_METHOD;
		public static int IMG_C_EVENTS;
		public static int IMG_SC_EVENTS;
		public static int IMG_C_EVENT;
		public static int IMG_SC_EVENT;
		public static int IMG_BC_EVENT;
		public static int IMG_SBC_EVENT;
		public static int IMG_CA_EVENT;
		public static int IMG_SCA_EVENT;
		public static int IMG_BCA_EVENT;
		public static int IMG_SBCA_EVENT;
		//
		public static int IMG_A_TypeCollection;
		public static int IMG_A_ACTIONCOLLECTION;
		public static int IMG_A_Events;
		//
		public static int IMG_A_C_EVENTS;
		public static int IMG_A_S_C_EVENTS;
		public static int IMG_A_S_C_EVENT;
		public static int IMG_A_S_EVENTS;
		//
		public static int IMG_ATTACH_EVENT_ACT;
		public static int IMG_DEATTACH_EVENT_ACT;
		//
		public static int IMG_A_F_EVENT;
		public static int IMG_EVENT_METHOD;
		public static int IMG_EVENT_METHOD_FORALL;
		public static int IMG_EVENT_METHOD_CLIENT;
		public static int IMG_EVENT_METHOD_SERVER;
		public static int IMG_EVENT_METHOD_DOWNLOAD;
		//
		public static int IMG_CONNECTS;
		public static int IMG_CONNECT;
		//
		public static int IMG_LANGUAGES;
		public static int IMG_LANGUAGE;
		public static int IMG_RESMAN;
		public static int IMG_STRINGS;
		public static int IMG_STRING;
		public static int IMG_IMAGES;
		public static int IMG_IMAGE;
		public static int IMG_ICONS;
		public static int IMG_ICON;

		public static int IMG_AUDIOS;
		public static int IMG_AUDIO;
		public static int IMG_FILES;
		public static int IMG_FILE;
		//
		public static int IMG_ARRAY;
		//
		public static int IMG_DEFINST;
		public static int IMG_SERVICES;
		//
		public static int IMG_CLASS;
		//
		static ImageList _imageList;
		static Dictionary<Type, int> _typeImages;
		static Dictionary<Guid, Dictionary<UInt32, int>> _classImages; //project,classId, image index
		//
		public static ImageList ObjectImageList
		{
			get
			{
				if (_imageList == null)
				{
					_imageList = new ImageList();
					IMG_OK = _imageList.Images.Add(Resources.OK, Color.White);
					IMG_CANCEL = _imageList.Images.Add(Resources.cancel, Color.White);
					IMG_ARROWLT = _imageList.Images.Add(Resources.arrorLT, Color.White);
					IMG_DEFICON = _imageList.Images.Add(Resources.defObjectIcon, Color.White);
					IMG_VOID = _imageList.Images.Add(Resources._void, Color.White);
					IMG_CONSOLE = _imageList.Images.Add(Resources.console3, Color.White);
					IMG_ATTRIBUTES = _imageList.Images.Add(Resources.attributes, Color.White);
					IMG_PROPERTIES = _imageList.Images.Add(Resources.properties, Color.White);
					IMG_PROPERTIES_WITHACTIONS = _imageList.Images.Add(Resources.propertiesWithAction, Color.White);
					IMG_METHODS = _imageList.Images.Add(Resources.methods, Color.White);
					IMG_METHODS_WITHACTS = _imageList.Images.Add(Resources.methods_withActs, Color.White);
					IMG_EVENTS = _imageList.Images.Add(Resources.events, Color.White);
					IMG_EVENT = _imageList.Images.Add(Resources._event, Color.White);
					IMG_BC_EVENT = _imageList.Images.Add(Resources.bc_event, Color.White);
					IMG_SBC_EVENT = _imageList.Images.Add(Resources.sbc_event, Color.White);
					//
					IMG_CA_EVENT = _imageList.Images.Add(Resources.ca_event, Color.White);
					IMG_BCA_EVENT = _imageList.Images.Add(Resources.bca_event, Color.White);
					IMG_SCA_EVENT = _imageList.Images.Add(Resources.sca_event, Color.White);
					IMG_SBCA_EVENT = _imageList.Images.Add(Resources.sbca_event, Color.White);
					//
					IMG_ActRet = _imageList.Images.Add(Resources.actRet, Color.White);
					//
					IMG_EVENT_WITHHANDLER = _imageList.Images.Add(Resources.eventHandler, Color.White);
					IMG_EVENT_WITHHANDLERS = _imageList.Images.Add(Resources.eventHandlers, Color.White);
					IMG_PROPERTY = _imageList.Images.Add(Resources.property, Color.White);
					IMG_PROPERTY_CLIENT = _imageList.Images.Add(Resources._custPropClient.ToBitmap(), Color.White);
					IMG_PROPERTY_SERVER = _imageList.Images.Add(Resources._custPropServer.ToBitmap(), Color.White);
					IMG_PROPERTY_CLIENT_ACT = _imageList.Images.Add(Resources._custPropClient_act.ToBitmap(), Color.White);
					IMG_PROPERTY_SERVER_ACT = _imageList.Images.Add(Resources._custPropServer_act.ToBitmap(), Color.White);

					IMG_PROPERTY_OVERRIDE = _imageList.Images.Add(Resources.o_property, Color.White);
					IMG_PROPERTY_OVERRIDE_A = _imageList.Images.Add(Resources.oa_property, Color.White);
					IMG_PROPERTY_OVERRIDE_BASE = _imageList.Images.Add(Resources.ob_property, Color.White);
					IMG_PROPERTY_OVERRIDE_BASE_A = _imageList.Images.Add(Resources.oba_property, Color.White);
					IMG_PROPERTY_OVERRIDE_PROTECTED = _imageList.Images.Add(Resources.op_property, Color.White);
					IMG_PROPERTY_OVERRIDE_PROTECTED_A = _imageList.Images.Add(Resources.opa_property, Color.White);
					IMG_PROPERTY_OVERRIDE_BASE_PROTECTED = _imageList.Images.Add(Resources.obp_property, Color.White);
					IMG_PROPERTY_OVERRIDE_BASE_PROTECTED_A = _imageList.Images.Add(Resources.obpa_property, Color.White);
					IMG_PROPERTY_VIRTUAL = _imageList.Images.Add(Resources.obv_property, Color.White);
					IMG_PROPERTY_VIRTUAL_A = _imageList.Images.Add(Resources.obva_property, Color.White);
					IMG_PROPERTY_VIRTUAL_PROTECTED = _imageList.Images.Add(Resources.obpv_property, Color.White);
					IMG_PROPERTY_VIRTUAL_PROTECTED_A = _imageList.Images.Add(Resources.obpva_property, Color.White);

					IMG_METHOD_OVERRIDE = _imageList.Images.Add(Resources.o_method, Color.White);
					IMG_METHOD_OVERRIDE_A = _imageList.Images.Add(Resources.oa_method, Color.White);
					IMG_METHOD_OVERRIDE_BASE = _imageList.Images.Add(Resources.ob_method, Color.White);
					IMG_METHOD_OVERRIDE_BASE_A = _imageList.Images.Add(Resources.oba_method, Color.White);
					IMG_METHOD_OVERRIDE_PROTECTED = _imageList.Images.Add(Resources.op_method, Color.White);
					IMG_METHOD_OVERRIDE_PROTECTED_A = _imageList.Images.Add(Resources.opa_method, Color.White);
					IMG_METHOD_OVERRIDE_BASE_PROTECTED = _imageList.Images.Add(Resources.obp_method, Color.White);
					IMG_METHOD_OVERRIDE_BASE_PROTECTED_A = _imageList.Images.Add(Resources.obpa_method, Color.White);
					IMG_METHOD_VIRTUAL = _imageList.Images.Add(Resources.obv_method, Color.White);
					IMG_METHOD_VIRTUAL_A = _imageList.Images.Add(Resources.obva_method, Color.White);
					IMG_METHOD_VIRTUAL_PROTECTED = _imageList.Images.Add(Resources.obpv_method, Color.White);
					IMG_METHOD_VIRTUAL_PROTECTED_A = _imageList.Images.Add(Resources.obpva_method, Color.White);
					IMG_METHOD_WEB = _imageList.Images.Add(Resources.webMethod, Color.White);
					IMG_METHOD_WEB_ACT = _imageList.Images.Add(Resources.webMethodAct, Color.White);

					IMG_PROPERTY_READONLY = _imageList.Images.Add(Resources.prop_readonly, Color.White);
					IMG_PROPERTY_WITHACTS = _imageList.Images.Add(Resources.propertyWithAction, Color.White);
					IMG_METHOD = _imageList.Images.Add(Resources.method, Color.White);
					IMG_METHOD_WEBCLIENT = _imageList.Images.Add(Resources._webClientMethod.ToBitmap(), Color.White);
					IMG_METHOD_WEBSERVER = _imageList.Images.Add(Resources._webServerMethod.ToBitmap(), Color.White);
					IMG_METHOD_WEBCLIENT_ACT = _imageList.Images.Add(Resources._webClientMethod_act.ToBitmap(), Color.White);
					IMG_METHOD_WEBSERVER_ACT = _imageList.Images.Add(Resources._webServerMethod_act.ToBitmap(), Color.White);
					IMG_METHOD_ACTS = _imageList.Images.Add(Resources.methodAsHandler, Color.White);
					IMG_ACTIONCOLLECTION = _imageList.Images.Add(Resources.actions, Color.White);
					IMG_ACTION = _imageList.Images.Add(Resources._action.ToBitmap(), Color.White);
					IMG_ACTION_INVALID = _imageList.Images.Add(Resources._action_invalid.ToBitmap(), Color.White);
					IMG_ACTION_UNUSED = _imageList.Images.Add(Resources._action_unused.ToBitmap(), Color.White);
					IMG_ACTIONGROUPCOLLECTION = _imageList.Images.Add(Resources.actiongroups, Color.White);
					IMG_ACTIONGROUP = _imageList.Images.Add(Resources.actiongroup, Color.White);
					IMG_A_ACTIONGROUP = _imageList.Images.Add(Resources.a_actiongroup, Color.White);
					IMG_PARAM = _imageList.Images.Add(Resources.param, Color.White);
					IMG_OBJECTS = _imageList.Images.Add(Resources.objects, Color.White);
					IMG_OBJECT = _imageList.Images.Add(Resources._object, Color.White);
					IMG_enumValues = _imageList.Images.Add(Resources.enumValues, Color.White);
					IMG_enumValue = _imageList.Images.Add(Resources.enumValue, Color.White);
					IMG_MessageBox = _imageList.Images.Add(Resources.msgbox, Color.White);
					IMG_TypeCollection = _imageList.Images.Add(Resources.abd_2, Color.White);
					IMG_BreakPoint = _imageList.Images.Add(Resources.breakpoint, Color.White);
					IMG_BreakPointPause = _imageList.Images.Add(Resources.breakPointPause, Color.White);
					IMG_Assembly = _imageList.Images.Add(Resources.assembly, Color.White);
					IMG_Type = _imageList.Images.Add(Resources._type, Color.White);
					IMG_Interface = _imageList.Images.Add(Resources._interface, Color.White);
					IMG_c_Interface = _imageList.Images.Add(Resources.c_interface, Color.White);
					IMG_Interface2 = _imageList.Images.Add(Resources._interface2, Color.White);
					IMG_Interfaces = _imageList.Images.Add(Resources.interfaces, Color.White);
					IMG_Constructor = _imageList.Images.Add(Resources.constructor, Color.White);
					IMG_Constructors = _imageList.Images.Add(Resources.constructors, Color.White);
					IMG_Constructor1 = _imageList.Images.Add(Resources.constructor1, Color.White);
					IMG_Attributes = _imageList.Images.Add(Resources.attrs, Color.White);
					IMG_Attribute = _imageList.Images.Add(Resources.attr, Color.White);
					IMG_MethodReturn = _imageList.Images.Add(Resources.ret, Color.White);
					//
					IMG_A_TypeCollection = _imageList.Images.Add(Resources.a_types, Color.White);
					IMG_A_ACTIONCOLLECTION = _imageList.Images.Add(Resources.a_actions, Color.White);
					IMG_A_Events = _imageList.Images.Add(Resources.a_events, Color.White);
					//
					IMG_S_ACTION = _imageList.Images.Add(Resources.s_run, Color.White);
					IMG_S_ATTRIBUTES = _imageList.Images.Add(Resources.s_attributes, Color.White);
					IMG_S_PROPERTIES = _imageList.Images.Add(Resources.s_properties, Color.White);
					IMG_S_PROPERTIES_WITHACTIONS = _imageList.Images.Add(Resources.s_propertiesWithAction, Color.White);
					IMG_S_METHODS = _imageList.Images.Add(Resources.s_methods, Color.White);
					IMG_S_METHODS_WITHACTS = _imageList.Images.Add(Resources.s_methods_withActs, Color.White);
					IMG_S_EVENTS = _imageList.Images.Add(Resources.s_events, Color.White);
					IMG_S_EVENT = _imageList.Images.Add(Resources.s_event, Color.White);
					IMG_S_EVENT_WITHHANDLER = _imageList.Images.Add(Resources.s_eventHandler, Color.White);
					IMG_S_EVENT_WITHHANDLERS = _imageList.Images.Add(Resources.s_eventHandlers, Color.White);
					IMG_S_PROPERTY = _imageList.Images.Add(Resources.s_property, Color.White);
					IMG_S_PROPERTY_READONLY = _imageList.Images.Add(Resources.s_prop_readonly, Color.White);
					IMG_S_PROPERTY_WITHACTS = _imageList.Images.Add(Resources.s_propertyWithAction, Color.White);
					IMG_S_METHOD = _imageList.Images.Add(Resources.s_method, Color.White);
					IMG_S_METHOD_ACTS = _imageList.Images.Add(Resources.s_methodAsHandler, Color.White);
					IMG_S_ACTIONCOLLECTION = _imageList.Images.Add(Resources.s_actions, Color.White);
					IMG_S_ACTIONGROUPCOLLECTION = _imageList.Images.Add(Resources.s_actiongroups, Color.White);
					IMG_S_ACTIONGROUP = _imageList.Images.Add(Resources.s_actiongroup, Color.White);
					IMG_SA_ACTIONGROUP = _imageList.Images.Add(Resources.sa_actiongroup, Color.White);
					IMG_S_OBJECTS = _imageList.Images.Add(Resources.s_objects, Color.White);
					//
					IMG_C_OBJECTS = _imageList.Images.Add(Resources.c_objects, Color.White);
					IMG_SC_OBJECTS = _imageList.Images.Add(Resources.sc_objects, Color.White);
					IMG_C_PROPERTIES = _imageList.Images.Add(Resources.c_properties, Color.White);
					IMG_CA_PROPERTIES = _imageList.Images.Add(Resources.ca_properties, Color.White);
					IMG_SC_PROPERTIES = _imageList.Images.Add(Resources.sc_properties, Color.White);
					IMG_SCA_PROPERTIES = _imageList.Images.Add(Resources.sca_properties, Color.White);
					IMG_C_PROPERTY = _imageList.Images.Add(Resources.c_property, Color.White);
					IMG_CA_PROPERTY = _imageList.Images.Add(Resources.ca_property, Color.White);
					IMG_SCA_PROPERTY = _imageList.Images.Add(Resources.sca_property, Color.White);
					IMG_SCBA_PROPERTY = _imageList.Images.Add(Resources.scba_property, Color.White);
					IMG_BC_PROPERTY = _imageList.Images.Add(Resources.cb_property, Color.White);
					IMG_BCA_PROPERTY = _imageList.Images.Add(Resources.cba_property, Color.White);
					IMG_SBC_PROPERTY = _imageList.Images.Add(Resources.scb_property, Color.White);
					IMG_SC_PROPERTY = _imageList.Images.Add(Resources.sc_property, Color.White);
					//
					IMG_C_METHODS = _imageList.Images.Add(Resources.c_methods, Color.White);
					IMG_CA_METHODS = _imageList.Images.Add(Resources.ca_methods, Color.White);
					IMG_SC_METHODS = _imageList.Images.Add(Resources.sc_methods, Color.White);
					IMG_SCA_METHODS = _imageList.Images.Add(Resources.sca_methods, Color.White);
					IMG_C_METHOD = _imageList.Images.Add(Resources.c_method, Color.White);
					IMG_CO_METHOD = _imageList.Images.Add(Resources.co_method, Color.White);
					IMG_SC_METHOD = _imageList.Images.Add(Resources.sc_method, Color.White);
					IMG_C_EVENTS = _imageList.Images.Add(Resources.c_events, Color.White);
					IMG_SC_EVENTS = _imageList.Images.Add(Resources.sc_events, Color.White);
					IMG_C_EVENT = _imageList.Images.Add(Resources.c_event, Color.White);
					IMG_SC_EVENT = _imageList.Images.Add(Resources.sc_event, Color.White);
					//
					IMG_A_C_EVENTS = _imageList.Images.Add(Resources.ac_events, Color.White);
					IMG_A_S_C_EVENTS = _imageList.Images.Add(Resources.asc_events, Color.White);
					IMG_A_S_C_EVENT = _imageList.Images.Add(Resources.asc_event, Color.White);
					IMG_A_S_EVENTS = _imageList.Images.Add(Resources.as_events, Color.White);
					//
					IMG_ATTACH_EVENT_ACT = _imageList.Images.Add(Resources._eventHandler.ToBitmap(), Color.White);
					IMG_DEATTACH_EVENT_ACT = _imageList.Images.Add(Resources._detachEvent.ToBitmap(), Color.White);
					//
					IMG_A_F_EVENT = _imageList.Images.Add(Resources.f_event, Color.White);
					IMG_EVENT_METHOD = _imageList.Images.Add(Resources.handlerMethod, Color.White);
					IMG_EVENT_METHOD_FORALL = _imageList.Images.Add(Resources._methodActionForAll.ToBitmap(), Color.White);
					IMG_EVENT_METHOD_CLIENT = _imageList.Images.Add(Resources.handlerMethod, Color.White);
					IMG_EVENT_METHOD_SERVER = _imageList.Images.Add(Resources._webServerHandler.ToBitmap(), Color.White);
					IMG_EVENT_METHOD_DOWNLOAD = _imageList.Images.Add(Resources._webHandler2.ToBitmap(), Color.White);
					//
					IMG_CONNECTS = _imageList.Images.Add(Resources.connections, Color.White);
					IMG_CONNECT = _imageList.Images.Add(Resources.connect, Color.White);
					//
					IMG_LANGUAGES = _imageList.Images.Add(Resources._regionLanguage.ToBitmap(), Color.White);
					IMG_LANGUAGE = _imageList.Images.Add(Resources._language, Color.White);
					IMG_RESMAN = _imageList.Images.Add(Resources.resx.ToBitmap(), Color.White);
					IMG_STRINGS = _imageList.Images.Add(Resources._strings.ToBitmap(), Color.White);
					IMG_STRING = _imageList.Images.Add(Resources._string.ToBitmap(), Color.White);
					IMG_IMAGES = _imageList.Images.Add(Resources._imgs.ToBitmap(), Color.White);
					IMG_IMAGE = _imageList.Images.Add(Resources._image.ToBitmap(), Color.White);
					IMG_ICONS = _imageList.Images.Add(Resources._icons.ToBitmap(), Color.White);
					IMG_ICON = _imageList.Images.Add(Resources._icon.ToBitmap(), Color.White);

					IMG_AUDIOS = _imageList.Images.Add(Resources.audios.ToBitmap(), Color.White);
					IMG_AUDIO = _imageList.Images.Add(Resources.audio.ToBitmap(), Color.White);
					IMG_FILES = _imageList.Images.Add(Resources._files.ToBitmap(), Color.White);
					IMG_FILE = _imageList.Images.Add(Resources._file.ToBitmap(), Color.White);
					//
					IMG_ARRAY = _imageList.Images.Add(Resources._array.ToBitmap(), Color.White);
					//
					IMG_DEFINST = _imageList.Images.Add(Resources._defInst.ToBitmap(), Color.White);
					IMG_SERVICES = _imageList.Images.Add(Resources.services.ToBitmap(), Color.White);
					//
					IMG_CLASS = _imageList.Images.Add(Resources._class.ToBitmap(), Color.White);
					//
					addLanguageicons();
				}
				return _imageList;
			}
		}
		private static void addLanguageicons()
		{
			ProjectResources.CollectLanguageIcons(_imageList);
		}
		public static Image GetLangaugeBitmapByName(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				ImageList imgs = ObjectImageList;
				if (imgs.Images.ContainsKey(name))
				{
					return imgs.Images[name];
				}
				else
				{
					int pos = name.IndexOf('-');
					if (pos > 0)
					{
						string s = name.Substring(0, pos);
						if (_imageList.Images.ContainsKey(s))
						{
							return _imageList.Images[s];
						}
					}
				}
			}
			return Resources._language;
		}
		public static int GetLangaugeImageByName(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_imageList.Images.ContainsKey(name))
				{
					return _imageList.Images.IndexOfKey(name);
				}
				else
				{
					int pos = name.IndexOf('-');
					if (pos > 0)
					{
						string s = name.Substring(0, pos);
						if (_imageList.Images.ContainsKey(s))
						{
							return _imageList.Images.IndexOfKey(s);
						}
					}
				}
			}
			return IMG_LANGUAGE;
		}
		public static int GetSelectedImageIndex(int imgIdx)
		{
			int ret = -1;
			if (_imageList != null)
			{
				string key = string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"img{0}", imgIdx);
				ret = _imageList.Images.IndexOfKey(key);
				if (ret < 0)
				{
					if (imgIdx >= 0 && imgIdx < _imageList.Images.Count)
					{
						Image img = _imageList.Images[imgIdx];
						Bitmap img2 = new Bitmap(img);
						//
						Graphics g = Graphics.FromImage(img2);
						int x = 0;
						int y = img2.Height - Resources._selected.Height;
						if (y < 0)
						{
							y = 0;
						}
						g.DrawIcon(Resources._selected, x, y);
						_imageList.Images.Add(key, img2);
						ret = _imageList.Images.IndexOfKey(key);
					}
				}
			}
			return ret;
		}
		public static Image GetTypeImage(int index)
		{
			if (index >= 0 && index < ObjectImageList.Images.Count)
			{
				return ObjectImageList.Images[index];
			}
			return null;
		}
		public static Image GetTypeImageFromResources(Type t)
		{
			if (t.IsGenericParameter || t.IsGenericType)
			{
				return Resources._genericParameter.ToBitmap();
			}
			if (t.Equals(typeof(DateTime)))
			{
				return Resources.date;
			}
			if (t.Equals(typeof(string)))
			{
				return Resources.abc;
			}
			if (t.Equals(typeof(bool)))
			{
				return Resources._bool;
			}
			if (t.Equals(typeof(byte)))
			{
				return Resources._byte;
			}
			if (t.Equals(typeof(char)))
			{
				return Resources._char;
			}
			if (t.Equals(typeof(sbyte)))
			{
				return Resources._sbyte;
			}
			if (t.Equals(typeof(ArrayPointer)))
			{
				return Resources.array;
			}
			if (t.Equals(typeof(Color)))
			{
				return Resources._3color;
			}
			if (t.Equals(typeof(StringCollection)))
			{
				return Resources.sc;
			}
			if (t.IsArray)
			{
				return Resources.array;
			}
			if (t.FullName.StartsWith("System.Collections.Generic.List`1[[", StringComparison.InvariantCulture))
			{
				return Resources.list;
			}
			if (t.Equals(typeof(int)) || t.Equals(typeof(Int16)) || t.Equals(typeof(Int32))
				|| t.Equals(typeof(Int64)) || t.Equals(typeof(uint)) || t.Equals(typeof(UInt16))
				|| t.Equals(typeof(UInt32)) || t.Equals(typeof(UInt64)))
			{
				return Resources._int;
			}
			if (t.Equals(typeof(decimal)) || t.Equals(typeof(float)) || t.Equals(typeof(double))
				|| t.Equals(typeof(Single)))
			{
				return Resources._decimal;
			}

			return null;
		}
		public static void SetTypeIcon(LimnorProject project, UInt32 classId, int imgIndex)
		{
			if (_classImages == null)
			{
				_classImages = new Dictionary<Guid, Dictionary<uint, int>>();
			}
			Dictionary<uint, int> imgList;
			if (!_classImages.TryGetValue(project.ProjectGuid, out imgList))
			{
				imgList = new Dictionary<uint, int>();
				_classImages.Add(project.ProjectGuid, imgList);
			}
			if (imgList.ContainsKey(classId))
			{
				imgList[classId] = imgIndex;
			}
			else
			{
				imgList.Add(classId, imgIndex);
			}
		}
		/// <summary>
		/// for a ClassPointer, we do not use Type to save the icon because different
		/// ClassPointers may have same Type
		/// </summary>
		/// <param name="objId"></param>
		/// <returns></returns>
		public static int GetTypeIcon(ClassPointer objId)
		{
			int n;
			LimnorProject project = objId.Project;
			if (_classImages == null)
			{
				_classImages = new Dictionary<Guid, Dictionary<uint, int>>();
			}
			Dictionary<uint, int> imgList;
			if (!_classImages.TryGetValue(project.ProjectGuid, out imgList))
			{
				imgList = new Dictionary<uint, int>();
				_classImages.Add(project.ProjectGuid, imgList);
			}
			if (imgList.TryGetValue(objId.ClassId, out n))
			{
				return n;
			}
			Image img = objId.ImageIcon;
			if (img != null)
			{
				n = ObjectImageList.Images.Add(img, Color.White);
				imgList.Add(objId.ClassId, n);
				return n;
			}

			Type t = VPLUtil.GetObjectType(objId.ObjectType);
			if (t != null)
			{
				return GetTypeIcon(t);
			}
			return IMG_DEFICON;
		}
		public static int GetTypeIcon(LimnorProject project, IClassRef objId)
		{
			int n;
			if (_classImages == null)
			{
				_classImages = new Dictionary<Guid, Dictionary<uint, int>>();
			}
			Dictionary<uint, int> imgList;
			if (!_classImages.TryGetValue(project.ProjectGuid, out imgList))
			{
				imgList = new Dictionary<uint, int>();
				_classImages.Add(project.ProjectGuid, imgList);
			}
			if (imgList.TryGetValue(objId.ClassId, out n))
			{
				return n;
			}
			Image img = objId.ImageIcon;
			if (img != null)
			{
				n = ObjectImageList.Images.Add(img, Color.White);
				imgList.Add(objId.ClassId, n);
				return n;
			}
			return IMG_DEFICON;
		}
		public static int GetTypeIcon(Type t, Image bmp)
		{
			if (_typeImages == null)
			{
				_typeImages = new Dictionary<Type, int>();
			}
			if (_typeImages.ContainsKey(t))
			{
				return _typeImages[t];
			}
			else
			{
				int n = ObjectImageList.Images.Add(bmp, Color.White);
				_typeImages.Add(t, n);
				return n;
			}
		}
		public static int GetTypeIcon(Type t)
		{
			t = VPLUtil.GetObjectType(t);
			if (_typeImages == null)
			{
				_typeImages = new Dictionary<Type, int>();
			}
			if (_typeImages.ContainsKey(t))
			{
				return _typeImages[t];
			}
			else
			{
				if (t.Equals(typeof(MessageBox)))
				{
					return IMG_MessageBox;
				}
				else if (t.Equals(typeof(Console)))
				{
					return IMG_CONSOLE;
				}
				else if (typeof(Attribute).IsAssignableFrom(t))
				{
					return IMG_Attribute;
				}
				else
				{
					Image icon = null;
					if (typeof(SoapHttpClientProtocol).IsAssignableFrom(t))
					{
						icon = Resources.cloud;
					}
					else if (typeof(WebService).IsAssignableFrom(t))
					{
						icon = Resources.earth;
					}
					else
					{
						icon = GetTypeImageFromResources(t);
						if (icon == null)
						{
							icon = DesignUtil.GetTypeIcon(t);
						}
						if (icon == null)
						{
							icon = VPL.VPLUtil.GetTypeIcon(t);
						}
					}
					if (icon != null)
					{
						int n = ObjectImageList.Images.Add(icon, Color.White);
						_typeImages.Add(t, n);
						return n;
					}
				}
				return IMG_DEFICON;
			}
		}
		public static Image GetTypeImage(Type t)
		{
			int n = GetTypeIcon(t);
			return ObjectImageList.Images[n];
		}
		public static Image GetClassImage(ClassPointer pointer)
		{
			int n = GetTypeIcon(pointer);
			return ObjectImageList.Images[n];
		}
		public static int AddBitmap(Image bmp)
		{
			return ObjectImageList.Images.Add(bmp, Color.White);
		}
		#endregion
		#region field members and constructors
		public event EventHandler TypeSelected;
		public event EventHandler NodeSelected;
		public fnCheckNode CheckTarget;
		//
		private bool _selectByEditor;
		private ArrayList m_coll;
		private TreeNode m_firstNode;
		//
		private LimnorProject _project;
		private ClassPointer _rootPointer;
		private bool _forMethodReturn;
		private Label lblInfo;
		private bool _adjusting;
		private EnumObjectSelectType _selectionType = EnumObjectSelectType.All;
		private bool _selectLValue;
		private TreeNodeClassRoot _mainRootNode;
		private fnOnTreeNodeExplorer miLoadNextLevel;
		//
		public TreeViewObjectExplorer()
		{
			m_coll = new ArrayList();
			//
			lblInfo = new Label();
			this.lblInfo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblInfo.ForeColor = System.Drawing.Color.Blue;
			this.lblInfo.Location = new System.Drawing.Point(107, 288);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(164, 29);
			this.lblInfo.TabIndex = 2;
			this.lblInfo.Text = "Loading ......";
			this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			lblInfo.Visible = false;
			this.Controls.Add(lblInfo);
			//
			miLoadNextLevel = new fnOnTreeNodeExplorer(loadNextLevel);
			//
			this.ImageList = ObjectImageList;
			this.HideSelection = false;
			ShowNodeToolTips = true;
		}
		#endregion
		#region Static members
		public static int GetMethodImageIndex(MethodClass m)
		{
			int ImageIndex;
			if (m.Project.IsWebApplication)
			{
				if (m.RunAt == EnumWebRunAt.Client)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEBCLIENT;
				}
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEBSERVER;
				}
			}
			else if (m.IsRemotingServiceMethod)
			{
				ImageIndex = TreeViewObjectExplorer.IMG_SERVICES;
			}
			else
			{
				MethodClassInherited method = m as MethodClassInherited;
				if (method != null)
				{
					if (method.HasBaseImplementation)//base is virtual
					{
						if (method.AccessControl == EnumAccessControl.Public)
						{
							if (method.Implemented)
								ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_BASE;
							else
								ImageIndex = TreeViewObjectExplorer.IMG_METHOD_VIRTUAL;
						}
						else
						{
							if (method.Implemented)
								ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_BASE_PROTECTED;
							else
								ImageIndex = TreeViewObjectExplorer.IMG_METHOD_VIRTUAL_PROTECTED;
						}
					}
					else //base is abstract
					{
						if (method.AccessControl == EnumAccessControl.Public)
						{
							ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE;
						}
						else
						{
							ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_PROTECTED;
						}
					}
				}
				else
				{
					if (m.IsStatic)
					{
						//ImageIndex = TreeViewObjectExplorer.IMG_S_ACTIONGROUP;
						if (m.IsDefined)
						{
							ImageIndex = TreeViewObjectExplorer.IMG_SC_METHOD;
						}
						else
						{
							ImageIndex = TreeViewObjectExplorer.IMG_S_ACTIONGROUP;
						}
					}
					else
					{
						if (m.IsDefined)
						{
							ImageIndex = TreeViewObjectExplorer.IMG_CO_METHOD;
						}
						else
						{
							ImageIndex = TreeViewObjectExplorer.IMG_ACTIONGROUP;
						}
					}
				}
			}
			return ImageIndex;
		}
		public static int GetActMethodImageIndex(MethodClass m)
		{
			int ImageIndex;
			MethodClassInherited method = m as MethodClassInherited;
			if (method != null)
			{
				if (method.HasBaseImplementation)//base is virtual
				{
					if (method.AccessControl == EnumAccessControl.Public)
					{
						if (method.Implemented)
							ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_BASE_A;
						else
							ImageIndex = TreeViewObjectExplorer.IMG_METHOD_VIRTUAL_A;
					}
					else
					{
						if (method.Implemented)
							ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_BASE_PROTECTED_A;
						else
							ImageIndex = TreeViewObjectExplorer.IMG_METHOD_VIRTUAL_PROTECTED_A;
					}
				}
				else //base is abstract
				{
					if (method.AccessControl == EnumAccessControl.Public)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_A;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_METHOD_OVERRIDE_PROTECTED_A;
					}
				}
			}
			else
			{
				if (m.Project.IsWebApplication)
				{
					if (m.RunAt == EnumWebRunAt.Client)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEBCLIENT_ACT;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEBSERVER_ACT;
					}
				}
				else
				{
					if (m.IsStatic)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_SCA_METHODS;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_CA_METHODS;
					}
				}
			}
			return ImageIndex;
		}
		public static int GetPropertyImageIndex(PropertyClass p)
		{
			int ImageIndex;
			PropertyClassInherited propety = p as PropertyClassInherited;
			if (propety != null)
			{
				if (propety.HasBaseImplementation)//base is virtual
				{
					if (propety.AccessControl == EnumAccessControl.Public)
					{
						if (propety.Implemented)
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_BASE;
						else
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_VIRTUAL;
					}
					else
					{
						if (propety.Implemented)
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_BASE_PROTECTED;
						else
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_VIRTUAL_PROTECTED;
					}
				}
				else //base is abstract
				{
					if (propety.AccessControl == EnumAccessControl.Public)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_PROTECTED;
					}
				}
			}
			else
			{
				if (p.RunAt == EnumWebRunAt.Client)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_CLIENT;
				}
				else if (p.RunAt == EnumWebRunAt.Server)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_SERVER;
				}
				else if (p.IsStatic)
				{
					if (p.Implemented)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_SC_PROPERTY;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_SBC_PROPERTY;
					}
				}
				else
				{
					if (p.Implemented)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_C_PROPERTY;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_BC_PROPERTY;
					}
				}
			}
			return ImageIndex;
		}
		public static int GetActPropertyImageIndex(PropertyClass p)
		{
			int ImageIndex;
			PropertyClassInherited propety = p as PropertyClassInherited;
			if (propety != null)
			{
				if (propety.HasBaseImplementation)//base is virtual
				{
					if (propety.AccessControl == EnumAccessControl.Public)
					{
						if (propety.Implemented)
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_BASE_A;
						else
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_VIRTUAL_A;
					}
					else
					{
						if (propety.Implemented)
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_BASE_PROTECTED_A;
						else
							ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_VIRTUAL_PROTECTED_A;
					}
				}
				else //base is abstract
				{
					if (propety.AccessControl == EnumAccessControl.Public)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_A;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_OVERRIDE_PROTECTED_A;
					}
				}
			}
			else
			{
				if (p.RunAt == EnumWebRunAt.Client)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_CLIENT_ACT;
				}
				else if (p.RunAt == EnumWebRunAt.Server)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_SERVER_ACT;
				}
				else if (p.IsStatic)
				{
					if (p.Implemented)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_SCA_PROPERTY;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_SCBA_PROPERTY;
					}
				}
				else
				{
					if (p.Implemented)
					{
						ImageIndex = TreeViewObjectExplorer.IMG_CA_PROPERTY;
					}
					else
					{
						ImageIndex = TreeViewObjectExplorer.IMG_BCA_PROPERTY;
					}
				}
			}
			return ImageIndex;
		}
		public static bool ForProperty(EnumPointerType t)
		{
			return (t != EnumPointerType.Method && t != EnumPointerType.Event && t != EnumPointerType.Field);
		}
		public static bool ForMethod(EnumPointerType t)
		{
			return (t != EnumPointerType.Property && t != EnumPointerType.Event && t != EnumPointerType.Field);
		}
		public static bool ForEvent(EnumPointerType t)
		{
			return (t != EnumPointerType.Method && t != EnumPointerType.Property && t != EnumPointerType.Field);
		}
		/// <summary>
		/// map IObjectPointer implements to tree node types
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static Type TreeNodeType(IObjectIdentity p)
		{
			if (p == null)
				return typeof(TreeNodePrimaryCollection);
			if (p is ClassInstancePointer)
				return typeof(TreeNodeClassComponent);
			if (p is ClassPointer)
				return typeof(TreeNodeClassRoot);
			if (p is MemberComponentIdCustom)
				return typeof(TreeNodeClassComponentCust);
			if (p is MemberComponentId)
				return typeof(TreeNodeClassComponentLib);
			SetterPointer sp = p as SetterPointer;
			if (sp != null)
			{
				if (sp.SetProperty.IsCustomProperty)
					return typeof(TreeNodeCustomPropertyPointer);
				return typeof(TreeNodeProperty);
			}
			if (p is MethodPointer)
				return typeof(TreeNodeMethod);
			if (p is EventPointer)
				return typeof(TreeNodeEvent);
			if (p is MethodClass)
				return typeof(TreeNodeCustomMethod);
			if (p is PropertyPointer)
				return typeof(TreeNodeProperty);
			if (p is PropertyClass)
				return typeof(TreeNodeCustomProperty);
			if (p is EventClass)
				return typeof(TreeNodeCustomEvent);
			if (p is CustomPropertyPointer)
				return typeof(TreeNodeCustomProperty);
			if (p is LocalVariable)
				return typeof(TreeNodeCustomMethod);
			if (p is FieldPointer)
				return typeof(TreeNodeField);
			if (p is CustomEventPointer)
				return typeof(TreeNodeCustomEventPointer);
			throw new DesignerException("object pointer {0} has not been assigned a TreeNode", p.GetType().Name);
		}
		/// <summary>
		/// map IObjectPointer implements to tree node collection types
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static Type TreeNodeCollectionType(IObjectIdentity p)
		{
			SetterPointer sp = p as SetterPointer;
			if (sp != null)
			{
				if (sp.SetProperty.IsCustomProperty)
				{
					return typeof(TreeNodeCustomPropertyPointerCollection);
				}
				else
				{
					return typeof(TreeNodePropertyCollection);
				}
			}
			if (p is MethodPointer)
				return typeof(TreeNodeMethodCollection);
			if (p is EventPointer)
				return typeof(TreeNodeEventCollection);
			if (p is MethodClass)
				return typeof(TreeNodeCustomMethodCollection);
			if (p is PropertyPointer)
				return typeof(TreeNodePropertyCollection);
			if (p is PropertyClass)
				return typeof(TreeNodeCustomPropertyCollection);
			if (p is CustomPropertyPointer)
				return typeof(TreeNodeCustomPropertyCollection);
			if (p is CustomMethodPointer)
				return typeof(TreeNodeCustomMethodPointerCollection);
			if (p is FieldPointer)
				return typeof(TreeNodeFieldCollection);
			if (p is CollectionPointer)
				return typeof(TreeNodeProperty);
			if (p is MethodActionPointer)
				return typeof(TreeNodeActionCollectionForMethod);
			if(p is CustomEventPointer)
				return typeof(TreeNodeCustomEventCollection);
			throw new DesignerException("object pointer {0} has not been assigned a TreeNodeObjectCollection", p.GetType().Name);
		}
		public static TreeNodeObjectCollection GetCollectionNode(TreeNodeObject node, IObjectIdentity p)
		{
			return GetCollectionNode(node, TreeNodeCollectionType(p), p.IsStatic);
		}
		public static TreeNodeObjectCollection GetCollectionNode(TreeNodeObject node, Type nodeType, bool forStatic)
		{
			node.LoadNextLevel();
			for (int i = 0; i < node.Nodes.Count; i++)
			{
				if (nodeType.Equals(node.Nodes[i].GetType()))
				{
					TreeNodeObjectCollection nc = node.Nodes[i] as TreeNodeObjectCollection;
					if (nc.IsStatic == forStatic)
					{
						return nc;
					}
				}
			}
			return null;
		}
		#endregion
		#region properties
		public bool SelectLValue { get { return _selectLValue; } }
		public bool StaticScope { get; set; }
		public string SelectionEventScope { get; set; }
		public DataTypePointer SelectionTypeScope { get; set; }
		public Type SelectionTypeAttribute { get; set; }
		public IMethod ScopeMethod { get; set; }
		public IActionsHolder ActionsHolder { get; set; }
		public UInt32 ScopeMethodId
		{
			get
			{
				MethodClass mc = ScopeMethod as MethodClass;
				if (mc != null)
				{
					return mc.MethodID;
				}
				return 0;
			}
		}
		public bool ForMethodReturn
		{
			get
			{
				return _forMethodReturn;
			}
			set
			{
				_forMethodReturn = value;
			}
		}
		public EnumObjectSelectType SelectionType
		{
			get
			{
				return _selectionType;
			}
			set
			{
				_selectionType = value;
			}
		}
		public TreeNodeClassRoot DesignerRootNode
		{
			get
			{
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassRoot r = Nodes[i] as TreeNodeClassRoot;
					if (r != null)
					{
						if (r.ViewersHolder != null)
						{
							return r;
						}
					}
				}
				return null;
			}
		}
		/// <summary>
		/// debug window is loaded from a thread other than the main thread
		/// </summary>
		public int ThreadId
		{
			get
			{
				FormDebugger f = this.FindForm() as FormDebugger;
				if (f != null)
				{
					return f.MainThreadId;
				}
				return 0;
			}
		}
		public bool ReadOnly { get; set; }
		public LimnorProject Project
		{
			get
			{
				if (_project != null)
					return _project;
				IWithProject p = this.FindForm() as IWithProject;
				if (p != null && p.Project != null)
					return p.Project;
				if (RootClassNode != null && RootClassNode.ClassData != null)
					return RootClassNode.ClassData.Project;
				return null;
			}
		}
		public ClassPointer RootId
		{
			get
			{
				TreeNodeClassRoot r = RootClassNode;
				if (r != null && r.ClassData != null)
				{
					return r.ClassData.RootClassID;
				}
				return _rootPointer;
			}
		}
		public XmlNode RootObjectNode
		{
			get
			{
				TreeNodeClassRoot r = RootClassNode;
				if (r != null && r.ClassData != null)
				{
					return r.ClassData.XmlData;
				}
				r = DesignerRootNode;
				if (r != null && r.ClassData != null)
				{
					return r.ClassData.XmlData;
				}
				return null;
			}
		}
		public ObjectIDmap ObjectIDList
		{
			get
			{
				return RootClassNode.ClassData.ObjectMap;
			}
		}
		public bool IsClosing
		{
			get
			{
				TreeNodeClassRoot r = this.RootClassNode;
				if (r != null)
				{
					if (r.ViewersHolder != null && r.ViewersHolder.Loader != null && r.ViewersHolder.Loader.IsClosing)
						return true;
				}
				return false;
			}
		}
		#endregion
		#region event handlers
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			ClassPointer r = RootId;
			if (r != null)
			{
				ILimnorDesignPane pane = r.Project.GetTypedData<ILimnorDesignPane>(r.ClassId);
				if (pane != null)
				{
					pane.HideToolbox();
				}
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			TreeNodeExplorer nd = GetNodeAt(e.X, e.Y) as TreeNodeExplorer;
			if (nd != null)
			{
				nd.LoadToolTips();
			}
			base.OnMouseMove(e);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			TreeNode nd = GetNodeAt(e.X, e.Y);
			if (nd != null)
			{
				if (SelectedNode != nd)
				{
					if (MultipleSelection)
					{
						SelectedNode = nd;
					}
					else
					{
						base.OnMouseDown(e);
						SelectedNode = nd;
					}
				}
				else
				{
					if (MultipleSelection)
					{
						bool bControl = (ModifierKeys == Keys.Control);
						if (bControl)
						{
							SelectedNode = null;
						}
					}
				}
				if (e.Button == MouseButtons.Right)
				{
					ITreeNodeObject tno = nd as ITreeNodeObject;
					if (tno != null)
					{
						MenuItem[] mi = tno.GetContextMenuItems(ReadOnly || !tno.IsInDesign);
						if (mi != null)
						{
							ContextMenu cm = new ContextMenu();
							cm.MenuItems.AddRange(mi);
							cm.Show(this, new Point(e.X, e.Y));
						}
					}
				}
			}
		}
		protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
		{
			try
			{
				base.OnBeforeExpand(e);
				TreeNodeExplorer tne = e.Node as TreeNodeExplorer;
				if (tne != null)
				{
					ForceLoadNextLevel(tne);
					foreach (TreeNode tn in tne.Nodes)
					{
						TreeNodeObjectCollection tnoc = tn as TreeNodeObjectCollection;
						if (tnoc != null)
						{
							tnoc.AdjustActionIcon();
						}
					}
				}
				else
				{
					TreeNodeClassLoader tcl = e.Node as TreeNodeClassLoader;
					if (tcl != null)
					{
						tcl.LoadClass();
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(this.FindForm(),err);
			}
		}
		protected override void OnBeforeLabelEdit(NodeLabelEditEventArgs e)
		{
			base.OnBeforeLabelEdit(e);
			ITreeNodeLabelEdit ne = e.Node as ITreeNodeLabelEdit;
			if (ne != null)
			{
				ne.OnBeforeLabelEdit(e);
			}
		}
		protected override void OnAfterLabelEdit(NodeLabelEditEventArgs e)
		{
			base.OnAfterLabelEdit(e);
			ITreeNodeLabelEdit ne = e.Node as ITreeNodeLabelEdit;
			if (ne != null)
			{
				ne.OnAfterLabelEdit(e);
			}
		}
		protected override void OnAfterSelect(TreeViewEventArgs e)
		{
			try
			{
				if (!IsClosing && !_adjusting)
				{
					TreeNodeExplorer ner = e.Node as TreeNodeExplorer;
					base.OnAfterSelect(e);
					if (NodeSelected != null)
					{
						NodeSelected(this, new EventArgObjectSelected(e.Node));
					}
					if (TypeSelected != null)
					{
						TreeNodeClassType node = e.Node as TreeNodeClassType;
						if (node != null)
						{
							TypeSelected(this, new EventArgObjectSelected(node.OwnerDataType));
						}
					}
					//select it in PropertyGrid
					if (!_selectByEditor)
					{
						if (ner != null && _selectionType == EnumObjectSelectType.All)
						{
							IRootNode rd = ner.RootClassNode;
							if (rd != null)
							{
								if (rd.ViewersHolder != null && rd.ViewersHolder.Loader != null)
								{
									ner.OnNodeSelection(rd.ViewersHolder.Loader);
								}
							}
						}
						ITreeNodeObjectSelection nos = e.Node as ITreeNodeObjectSelection;
						if (nos != null)
						{
							nos.OnNodeAfterSelection(e);
						}
						onAfterSelectForMultipleSelection(e.Node);
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(this.FindForm(),err);
			}

			ITreeNodeLabelEdit ne = e.Node as ITreeNodeLabelEdit;
			if (ne != null)
			{
				this.LabelEdit = ne.AlloLabelEdit;
			}
			else
			{
				this.LabelEdit = false;
			}
		}
		public void OnHtmlElementSelected(HtmlElement_Base element)
		{
			TreeNodeHtmlElementCollection tnhec = null;
			TreeNodeClassRoot tnRoot = this.RootClassNode;
			if (tnRoot != null)
			{
				for (int i = 0; i < tnRoot.Nodes.Count; i++)
				{
					tnhec = tnRoot.Nodes[i] as TreeNodeHtmlElementCollection;
					if (tnhec != null)
					{
						break;
					}
				}
			}
			if (tnhec != null)
			{
				TreeNodeHtmlElementCurrent tnheCur = null;
				TreeNodeHtmlElement tnhe = null;
				for (int i = 0; i < tnhec.Nodes.Count; i++)
				{
					TreeNodeHtmlElement th = tnhec.Nodes[i] as TreeNodeHtmlElement;
					if (th != null)
					{
						if (tnheCur == null)
						{
							tnheCur = th as TreeNodeHtmlElementCurrent;
						}
						if (th.HtmlElement.ElementGuid == element.ElementGuid)
						{
							tnhe = th;
							if (!(th is TreeNodeHtmlElementCurrent) && tnheCur != null)
							{
								break;
							}
						}
					}
				}
				if (tnheCur != null)
				{
					tnheCur.SwitchHtmlElement(element);
					tnheCur.ShowText();
				}
				if (tnhe != null && element.ElementGuid != Guid.Empty)
				{
					tnhe.ResetObjectPointer(element);
					tnhe.ResetNextLevel(this);
					_selectByEditor = true;
					this.SelectedNode = tnhe;
					_selectByEditor = false;
				}
				else
				{
					this.SelectedNode = tnheCur;
				}
			}
		}
		public void OnHtmlElementIdChanged(HtmlElement_ItemBase element)
		{
			TreeNodeHtmlElementCollection tnhec = null;
			TreeNodeClassRoot tnRoot = this.RootClassNode;
			if (tnRoot != null)
			{
				for (int i = 0; i < tnRoot.Nodes.Count; i++)
				{
					tnhec = tnRoot.Nodes[i] as TreeNodeHtmlElementCollection;
					if (tnhec != null)
					{
						break;
					}
				}
			}
			if (tnhec != null && tnhec.NextLevelLoaded)
			{
				TreeNodeHtmlElement tnhe = null;
				for (int i = 0; i < tnhec.Nodes.Count; i++)
				{
					TreeNodeHtmlElement th = tnhec.Nodes[i] as TreeNodeHtmlElement;
					if (th != null && !(th is TreeNodeHtmlElementCurrent))
					{
						if (th.HtmlElement.ElementGuid == element.ElementGuid)
						{
							tnhe = th;
							break;
						}
					}
				}
				if (tnhe == null)
				{
					tnhe = new TreeNodeHtmlElement(element);
					tnhec.Nodes.Add(tnhe);
				}
				else
				{
					tnhe.ShowText();
				}
			}
		}
		public void OnSelectedHtmlElement(Guid guid, object selector)
		{
			if (selector != this)
			{
				TreeNodeClassRoot tnRoot = this.RootClassNode;
				if (tnRoot != null)
				{
					ClassPointer root = tnRoot.RootObjectId;
					if (root != null)
					{
						HtmlElement_Base heb = root.FindHtmlElementByGuid(guid);
						if (heb != null)
						{
							OnHtmlElementSelected(heb);
						}
					}
				}
			}
		}
		//an html element is used. guid and id should have been created
		public void OnUseHtmlElement(HtmlElement_BodyBase element)
		{
			TreeNodeHtmlElementCollection tnhec = null;
			TreeNodeClassRoot tnRoot = this.RootClassNode;
			if (tnRoot != null)
			{
				for (int i = 0; i < tnRoot.Nodes.Count; i++)
				{
					tnhec = tnRoot.Nodes[i] as TreeNodeHtmlElementCollection;
					if (tnhec != null)
					{
						break;
					}
				}
			}
			if (tnhec != null && tnhec.NextLevelLoaded)
			{
				TreeNodeHtmlElement tnhe = null;
				for (int i = 0; i < tnhec.Nodes.Count; i++)
				{
					TreeNodeHtmlElement th = tnhec.Nodes[i] as TreeNodeHtmlElement;
					if (th != null && !(th is TreeNodeHtmlElementCurrent))
					{
						if (th.HtmlElement.ElementGuid == element.ElementGuid)
						{
							tnhe = th;
							break;
						}
					}
				}
				if (tnhe == null)
				{
					tnhe = new TreeNodeHtmlElement(element);
					tnhec.Nodes.Add(tnhe);
				}
			}
		}
		public void ForceLoadNextLevel(TreeNodeExplorer tne)
		{
			bool invoked = false;
			if (this.Created)
			{
				Form f = this.FindForm();
				if (f != null)
				{
					if (f.InvokeRequired)
					{
						invoked = true;
						this.Invoke(miLoadNextLevel, tne);
					}
				}
			}
			if (!invoked)
			{
				loadNextLevel(tne);
			}
		}
		#endregion
		#region private methods
		private void loadNextLevel(TreeNodeExplorer tne)
		{
			if (!tne.NextLevelLoaded)
			{
				this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
				lblInfo.Location = new System.Drawing.Point(
				(this.Width - lblInfo.Width) / 2, (this.Height - lblInfo.Height) / 2);
				lblInfo.Visible = true;
				lblInfo.Refresh();
				tne.NextLevelLoaded = true;
				try
				{
					List<TreeNodeLoader> loaders = new List<TreeNodeLoader>();
					for (int i = 0; i < tne.Nodes.Count; i++)
					{
						TreeNodeLoader loader = tne.Nodes[i] as TreeNodeLoader;
						if (loader != null)
						{
							loaders.Add(loader);
						}
					}
					foreach (TreeNodeLoader l in loaders)
					{
						TreeNodeObject p = l.Parent as TreeNodeObject;
						l.Remove();
						if (p != null)
						{
							l.LoadNextLevel(this, p);
						}
					}
				}
				catch (Exception err)
				{
					MathNode.Log(this.FindForm(),err);
				}
				lblInfo.Visible = false;
				this.Cursor = System.Windows.Forms.Cursors.Default;
			}
		}
		private void changeActionName(TreeNodeCollection nodes, ActionClass act)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] is TreeNodeAction)
				{
					if (((TreeNodeAction)nodes[i]).Action.ActionId == act.ActionId)
					{
						nodes[i].Text = act.ActionName;
					}
				}
				else
				{
					changeActionName(nodes[i].Nodes, act);
				}
			}
		}
		private void removeActionNode(TreeNodeCollection nodes, UInt32 actionId)
		{
			int i = 0;
			while (i < nodes.Count)
			{
				if (nodes[i] is TreeNodeAction)
				{
					if (((TreeNodeAction)nodes[i]).Action.ActionId == actionId)
					{
						nodes.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
				else
				{
					removeActionNode(nodes[i].Nodes, actionId);
					i++;
				}
			}
		}
		private bool containsType(List<Type> list, Type t)
		{
			foreach (Type p in list)
			{
				if (p.IsInterface)
				{
					Type[] ti = t.GetInterfaces();
					if (ti != null && ti.Length > 0)
					{
						for (int i = 0; i < ti.Length; i++)
						{
							if (p.Equals(ti[i]))
							{
								return true;
							}
						}
					}
				}
				else
				{
					if (p.IsAssignableFrom(t))
					{
						return true;
					}
				}
			}
			return false;
		}
		private TreeNode locateNode(IObjectPointer pointer, List<Type> nodeType, TreeNodeCollection nodes)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				TreeNodeExplorer e = nodes[i] as TreeNodeExplorer;
				if (e != null)
				{
					TreeNodeObject o = e as TreeNodeObject;
					if (o != null)
					{
						if (containsType(nodeType, o.GetType()))
						{
							if (pointer.IsSameObjectRef(o.OwnerPointer))
							{
								return o;
							}
							LocalVariable lv = pointer as LocalVariable;
							if (lv != null)
							{
								if (pointer.Owner.IsSameObjectRef(o.OwnerPointer))
								{
									e.LoadNextLevel();
								}
							}
						}
					}
					if (e.NextLevelLoaded)
					{
						TreeNode tn = locateNode(pointer, nodeType, nodes[i].Nodes);
						if (tn != null)
						{
							return tn;
						}
					}
				}
			}
			return null;
		}
		private TreeNodeObject locateNodeByOwnerType(IObjectPointer pointer, List<Type> nodeType, TreeNodeCollection nodes)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				TreeNodeExplorer e = nodes[i] as TreeNodeExplorer;
				if (e != null)
				{
					TreeNodeObject o = e as TreeNodeObject;
					if (o != null)
					{
						if (o.OwnerPointer != null)
						{
							if (containsType(nodeType, o.OwnerPointer.GetType()))
							{
								if (pointer.IsSameObjectRef(o.OwnerPointer))
								{
									return o;
								}
							}
						}
						else
						{
							i = i + 0;
						}
					}
					if (e.NextLevelLoaded)
					{
						TreeNodeObject tn = locateNodeByOwnerType(pointer, nodeType, nodes[i].Nodes);
						if (tn != null)
						{
							return tn;
						}
					}
				}
			}
			return null;
		}
		private void resetNodes(bool isStatic, Type nodeType, TreeNodeCollection nodes)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				TreeNodeExplorer e = nodes[i] as TreeNodeExplorer;
				if (e != null)
				{
					TreeNodeObject o = e as TreeNodeObject;
					if (o != null)
					{
						if (o.IsStatic == isStatic)
						{
							if (nodeType.Equals(o.GetType()))
							{
								o.ResetNextLevel(this);
							}
						}
					}
					if (e.NextLevelLoaded)
					{
						resetNodes(isStatic, nodeType, e.Nodes);
					}
				}
			}
		}
		private void onObjPropertyChanged(ICustomObject obj)
		{
			TreeNodeClassRoot rootNode = RootClassNode;
			if (rootNode != null)
			{
				bool isStatic = false;
				PropertyClass p = obj as PropertyClass;
				MethodClass m = obj as MethodClass;
				EventClass e = obj as EventClass;
				if (p != null)
				{
					isStatic = p.IsStatic;
				}
				else if (m != null)
				{
					isStatic = m.IsStatic;
				}
				else if (e != null)
				{
					isStatic = e.IsStatic;
				}
				else
				{
					return;
				}
				IList<TreeNodeClassComponentCust> list = rootNode.GetInstancesByClassId(obj.ClassId);
				if (list.Count > 0)
				{
					foreach (TreeNodeClassComponentCust tnc in list)
					{
						MemberComponentIdCustom mc = (MemberComponentIdCustom)(tnc.OwnerPointer);
						TreeNodePME pme = tnc.GetPointerMembersNode(isStatic);
						if (pme != null)
						{
							if (p != null)
							{
								mc.Pointer.ChangeCustomProperty(p);
								TreeNodeCustomPropertyPointerCollection ps = pme.GetCustomPropertyPointersNode();
								if (ps != null)
								{
									TreeNodeCustomPropertyPointer tcp = ps.GetPropertyPointerNode(p.MemberId);
									if (tcp != null)
									{
										tcp.SetPropertyClass(p);
									}
								}
							}
							else if (m != null)
							{
								TreeNodeCustomMethodPointerCollection ms = pme.GetCustomMethodPointersNode();
								if (ms != null)
								{
									TreeNodeCustomMethodPointer tmp = ms.GetMethodPointerNode(m.MemberId);
									if (tmp != null)
									{
										tmp.SetMethodClass(m);
									}
								}
							}
							else if (e != null)
							{
								TreeNodeCustomEventPointerCollection es = pme.GetCustomEventPointersNode();
								if (es != null)
								{
									TreeNodeCustomEventPointer tep = es.GetEventPointerNode(e.MemberId);
									if (tep != null)
									{
										tep.SetEventClass(e);
									}
								}
							}
						}
					}
				}
			}
		}
		private void resetNodes(IObjectPointer pointer, Type nodeType, TreeNodeCollection nodes, bool showIndicator)
		{
			if (pointer == null) return;
			for (int i = 0; i < nodes.Count; i++)
			{
				TreeNodeExplorer e = nodes[i] as TreeNodeExplorer;
				if (e != null)
				{
					TreeNodeObject o = e as TreeNodeObject;
					if (o != null)
					{
						if (nodeType.Equals(o.GetType()))
						{
							if (pointer.IsSameObjectRef(o.OwnerPointer))
							{
								o.ResetNextLevel(this);
								if (showIndicator)
								{
									o.OnShowActionIcon();
								}
							}
						}
					}
					if (e.NextLevelLoaded)
					{
						resetNodes(pointer, nodeType, nodes[i].Nodes, showIndicator);
					}
				}
			}
		}
		private void addTypeNode(Type t, string title, Bitmap img)
		{
			TreeNodeClassType nt = new TreeNodeClassType(this, false, new TypePointer(t), title, img, ScopeMethodId);
			Nodes.Add(nt);
		}
		#endregion
		#region public methods
		public void ResetDefaultInstance(UInt32 classId)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClassRoot tnc = this.Nodes[i] as TreeNodeClassRoot;
				if (tnc != null)
				{
					for (int j = 0; j < tnc.Nodes.Count; j++)
					{
						TreeNodeDefaultInstance tndef = tnc.Nodes[j] as TreeNodeDefaultInstance;
						if (tndef != null)
						{
							MemberComponentIdCustom mc = tndef.OwnerPointer as MemberComponentIdCustom;
							if (mc != null && mc.ClassId == classId)
							{
								tndef.ResetNextLevel(this);
							}
						}
					}
				}
			}
		}
		public void SetSelectLValue()
		{
			_selectLValue = true;
		}
		public void ResetSelectLValue()
		{
			_selectLValue = false;
		}
		public TreeNodeClassComponent CreateComponentTreeNode(IClass holder, object v, UInt32 id, UInt32 scopeMethodId, EnumObjectSelectType target, DataTypePointer scope, string eventName)
		{
			UInt32 mid = 0;
			INonHostedObjectsHolder inh = null;
			string nonHostedName = null;
			MemberComponentId mc = null;

			MemberComponentIdCustomInstance ci = holder as MemberComponentIdCustomInstance;
			if (ci != null)
			{
				holder = (IClass)ci.Pointer.Clone();
				holder.Owner = ci.Owner;
			}
			else
			{
				inh = holder.ObjectInstance as INonHostedObjectsHolder;
				if (inh != null)
				{
					nonHostedName = inh.GetNonHostedKeyName(v);
					mid = holder.MemberId;
				}
				MemberComponentIdCustom c = holder as MemberComponentIdCustom;
				if (c != null)
				{
					holder = c.Pointer;
				}
				else
				{
					MemberComponentId mcHolder = holder as MemberComponentId;
					if (mcHolder != null)
					{
						//holder is a container
						holder = mcHolder.Owner as IClass;
					}
				}
			}
			if (mc == null)
			{
				ClassInstancePointer inst = holder as ClassInstancePointer;
				if (inst != null)
				{
					mc = MemberComponentId.CreateMemberComponentId(inst, v, id);
				}
				else
				{
					ClassPointer cp = holder as ClassPointer;
					if (cp != null)
					{
						if (!string.IsNullOrEmpty(nonHostedName))
							mc = MemberComponentId.CreateMemberComponentId(cp, inh, mid, nonHostedName);
						else
							mc = MemberComponentId.CreateMemberComponentId(cp, v, id, null);
					}
				}
			}
			if (mc != null)
			{
				MemberComponentIdCustom mcc = mc as MemberComponentIdCustom;
				if (mcc != null)
				{
					return new TreeNodeClassComponentCust(this, mcc, scopeMethodId);
				}
				else
				{
					return new TreeNodeClassComponentLib(this, mc, scopeMethodId);
				}
			}
			throw new DesignerException("{0} is not a holder of members", holder);
		}
		public TreeNodeClassRoot CreateClassRoot(bool defineScope, ClassPointer pointer, bool staticScope)
		{
			TreeNodeClassRoot cr;
			if (pointer == null)
			{
				throw new DesignerException("calling CreateClassRoot with null pointer");
			}
			if (pointer.IsInterface)
			{
				cr = new TreeNodeClassInterface(this, true, pointer);
			}
			else
			{
				if (pointer.IsStatic)
				{
					cr = new TreeNodeClassStatic(this, defineScope, pointer);
				}
				else
				{
					cr = new TreeNodeClassNonStatic(this, defineScope, pointer, staticScope);
				}
			}
			cr.StaticScope = staticScope;
			return cr;
		}
		public void SelectActionInList(IAction action)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeAllActionCollection allAct = Nodes[i] as TreeNodeAllActionCollection;
				if (allAct != null)
				{
					allAct.LoadNextLevel();
					if (action == null)
					{
						if (allAct.Nodes.Count > 0)
						{
							for (int j = 0; j < allAct.Nodes.Count; j++)
							{
								TreeNodeAction tna = allAct.Nodes[j] as TreeNodeAction;
								if (tna != null)
								{
									this.SelectedNode = tna;
									break;
								}
							}
						}
					}
					else
					{
						for (int j = 0; j < allAct.Nodes.Count; j++)
						{
							TreeNodeAction tna = allAct.Nodes[j] as TreeNodeAction;
							if (tna != null)
							{
								if (tna.Action.WholeActionId == action.WholeActionId)
								{
									this.SelectedNode = tna;
									break;
								}
							}
						}
					}
					break;
				}
			}
		}
		public void LoadAttributeParameterTypes(EnumWebRunAt runAt, Type initSelection)
		{
			_selectionType = EnumObjectSelectType.Type;
			if (runAt == EnumWebRunAt.Client)
			{
				Dictionary<string, Type> types = WebClientData.GetJavascriptTypes();
				foreach (KeyValuePair<string, Type> kv in types)
				{
					addTypeNode(kv.Value, kv.Key, TypeSelector.GetTypeImageByName(kv.Key));
				}
				addTypeNode(typeof(IWebClientControl), "WebControl", Resources._earth.ToBitmap());
			}
			else if (runAt == EnumWebRunAt.Server)
			{
				Dictionary<string, Type> types = WebPhpData.GetPhpTypes();
				foreach (KeyValuePair<string, Type> kv in types)
				{
					addTypeNode(kv.Value, kv.Key, TypeSelector.GetTypeImageByName(kv.Key));
				}
			}
			else
			{
				addTypeNode(typeof(bool), "Boolean", Resources._bool);
				addTypeNode(typeof(char), "Single letter", Resources._char);
				addTypeNode(typeof(string), "String (one or more letters)", Resources.abc);
				addTypeNode(typeof(sbyte), "Integer(8-bit)", Resources._sbyte);
				addTypeNode(typeof(short), "Integer(16-bit)", Resources._int);
				addTypeNode(typeof(int), "Integer(32-bit)", Resources._int);
				addTypeNode(typeof(long), "Integer(64-bit)", Resources._int);
				addTypeNode(typeof(float), "Single(7 digits decimal)", Resources._decimal);
				addTypeNode(typeof(double), "Double(15 digits decimal)", Resources._decimal);
				addTypeNode(typeof(Type), "Type", Resources._type);
			}
			if (initSelection != null)
			{
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassType tn = Nodes[i] as TreeNodeClassType;
					if (initSelection.Equals(tn.OwnerDataType))
					{
						SelectedNode = tn;
						break;
					}
				}
			}
		}
		public void SetProject(LimnorProject project)
		{
			_project = project;
		}
		public void AddAssemblyNode(string name)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (Nodes[i].Text == name)
				{
					SelectedNode = Nodes[i];
					return;
				}
			}
			TreeNodeAssembly nd = new TreeNodeAssembly(name);
			int n = Nodes.Count;
			if (n > 0)
			{
				TreeNodeDummy d = Nodes[n - 1] as TreeNodeDummy;
				if (d != null)
				{
					n = d.Index;
				}
			}
			Nodes.Insert(n, nd);
			SelectedNode = nd;
		}
		public void AddAssemblyNode(Assembly assembly)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (Nodes[i].Text == assembly.FullName)
				{
					SelectedNode = Nodes[i];
					return;
				}
			}
			TreeNodeAssembly nd = new TreeNodeAssembly(assembly);
			int n = Nodes.Count;
			if (n > 0)
			{
				TreeNodeDummy d = Nodes[n - 1] as TreeNodeDummy;
				if (d != null)
				{
					n = d.Index;
				}
			}
			Nodes.Insert(n, nd);
			SelectedNode = nd;
		}
		public void LoadGac()
		{
			lblInfo.Location = new System.Drawing.Point(
				(this.Width - lblInfo.Width) / 2, (this.Height - lblInfo.Height) / 2);
			lblInfo.Visible = true;
			lblInfo.Refresh();
			List<string> ss = new List<string>();

			AssemblyCacheEnum ace = new AssemblyCacheEnum(null);
			string s;
			s = ace.GetNextAssembly();
			while (!string.IsNullOrEmpty(s))
			{
				ss.Add(s);
				s = ace.GetNextAssembly();
			}
			ss.Sort();
			for (int i = 0; i < ss.Count; i++)
			{
				Nodes.Add(new TreeNodeAssembly(ss[i]));
			}
			lblInfo.Visible = false;
		}
		public void LoadTypeNodes()
		{
			TreeNodeClassRoot r = DesignerRootNode;
			if (r != null)
			{
				r.LoadExternalTypes();
			}
		}
		public Image GetRootObjectIcon()
		{
			TreeNodeClassRoot root = this.RootClassNode;
			if (root != null)
			{
				return root.ObjectIcon;
			}
			return null;
		}
		public void OnIconChanged(UInt32 classId)
		{
			TreeNodeClassRoot root = this.RootClassNode;
			if (root != null)
			{
				root.RefreshIcon(classId);
			}
		}
		public void SetClassRefIcon(UInt32 classId, System.Drawing.Image img)
		{
			TreeNodeClassRoot root = this.RootClassNode;
			if (root != null)
			{
				root.SetClassRefIcon(classId, img);
			}
		}
		public void OnObjectNameChanged(INonHostedObject obj)
		{
			TreeNodeClassRoot rootNode = RootClassNode;
			if (rootNode != null)
			{
				if (obj.ClassId == rootNode.ClassId)
				{
					IObjectPointer p = obj as IObjectPointer;
					if (p != null)
					{
						TreeNodeObject node = LocateNode(p) as TreeNodeObject;
						if (node != null)
						{
							EventClass ec = obj as EventClass;
							if (ec != null)
							{
								TreeNodeCustomEvent tnce = node as TreeNodeCustomEvent;
								if (tnce != null)
								{
									tnce.ResetObjectPointer(ec);
								}
							}
							node.ShowText();
						}
						else
						{
							PropertyClass pc = obj as PropertyClass;
							if (pc != null)
							{
								TreeNodeObject tp = RootClassNode.GetClassRefPropertyNode(pc);
								if (tp != null)
								{
									tp.ShowText();
								}
								else
								{
									TreeNodePME pme = RootClassNode.GetMemberNode(false);
									if (pme != null)
									{
										for (int i = 0; i < pme.Nodes.Count; i++)
										{
											TreeNodeCustomPropertyCollection tncpc = pme.Nodes[i] as TreeNodeCustomPropertyCollection;
											if (tncpc != null)
											{
												if (tncpc.NextLevelLoaded)
												{
													for (int j = 0; j < tncpc.Nodes.Count; j++)
													{
														TreeNodeCustomProperty tncp = tncpc.Nodes[j] as TreeNodeCustomProperty;
														if (tncp != null)
														{
															if (tncp.Property.IsSameObjectRef(pc))
															{
																tncp.ShowText();
																break;
															}
															else
															{
																PropertyOverride po = tncp.Property as PropertyOverride;
																if (po != null)
																{
																	if (po.BaseClassId == pc.ClassId)
																	{
																		if (po.BasePropertyId == pc.MemberId)
																		{
																			tncp.ShowText();
																			break;
																		}
																	}
																}
															}
														}
													}
												}
												break;
											}
										}
									}
								}
							}
						}
					}
					else
					{
						ActionClass act = obj as ActionClass;
						if (act != null)
						{
							OnActionChanged(obj.ClassId, act, false);
							//find event nodes that contains this action
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeClassRoot nodeRoot = Nodes[i] as TreeNodeClassRoot;
								if (nodeRoot != null)
								{
									ClassPointer cp = nodeRoot.OwnerPointer as ClassPointer;
									if (cp.EventHandlers != null)
									{
										foreach (EventAction ea in cp.EventHandlers)
										{
											bool bFound = false;
											if (ea.TaskIDList != null)
											{
												foreach (TaskID tid in ea.TaskIDList)
												{
													if (tid.WholeTaskId == act.WholeActionId)
													{
														bFound = true;
														break;
													}
												}
											}
											if (bFound)
											{
												TreeNodeObject nodeEvent = LocateNode(ea.Event) as TreeNodeObject;
												if (nodeEvent != null)
												{
													if (nodeEvent.NextLevelLoaded)
													{
														for (int k = 0; k < nodeEvent.Nodes.Count; k++)
														{
															TreeNodeAction nodeAct = nodeEvent.Nodes[k] as TreeNodeAction;
															if (nodeAct != null)
															{
																if (nodeAct.Action.ActionId == act.ActionId)
																{
																	nodeAct.ShowText();
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
						}
					}
				}
				else
				{
					ICustomObject ico = obj as ICustomObject;
					if (ico != null)
					{
						onObjPropertyChanged(ico);
					}
				}
			}
		}
		public void OnClassLoadedIntoDesigner(UInt32 classId)
		{
			TreeNodeClassRoot rootNode = this.RootClassNode;
			if (rootNode != null)
			{
				ClassPointer root = rootNode.RootObjectId.Project.GetTypedData<ClassPointer>(classId);
				if (rootNode.ClassId == classId)
				{
					rootNode.ResetPointer(root);
				}
				rootNode.OnClassLoadedIntoDesigner(root);
			}
		}
		public void OnDefinitionChanged(UInt32 classId, object relatedObject, EnumClassChangeType changeMade)
		{
			TreeNodeClassRoot rootNode = this.RootClassNode;
			if (rootNode != null)
			{
				rootNode.OnDefinitionChanged(classId, relatedObject, changeMade);
			}
		}
		public void OnComponentSelected(object obj)
		{

			bool b = _adjusting;
			_adjusting = true;

			TreeNodeClassRoot rootNode = RootClassNode;
			if (rootNode != null)
			{
				ClassProperties cp = obj as ClassProperties;
				if (cp != null)
				{
					obj = cp.Pointer.ObjectInstance;
				}
				if (rootNode.OwnerPointer.ObjectInstance == obj)
				{
					this.SelectedNode = rootNode;
				}
				else if (rootNode.NextLevelLoaded)
				{
					IClass ic = DesignUtil.GetHolder(rootNode.ClassData.ObjectMap, obj);
					if (ic == null)
					{
						//none components can be selected (i.e. ActionClass)
					}
					else
					{
						for (int i = 0; i < rootNode.Nodes.Count; i++)
						{
							TreeNodeClassComponent tn = rootNode.Nodes[i] as TreeNodeClassComponent;
							if (tn != null)
							{
								if (ic.IsSameObjectRef(tn.OwnerPointer))
								{
									if (SelectedNode != tn)
									{
										SelectedNode = tn;
									}
									break;
								}
								else
								{
									TreeNodeClassComponent tc = tn.FindNodeByObject(obj);
									if (tc != null)
									{
										if (SelectedNode != tc)
										{
											SelectedNode = tc;
										}
										break;
									}
								}
							}
						}
					}
				}
			}
			_adjusting = b;
		}
		public void OnComponentAdded(object obj)
		{
			TreeNodeClassRoot rootNode = RootClassNode;
			if (rootNode != null)
			{
				if (rootNode.TreeChild == null)
				{
					rootNode.RootObjectId.ObjectList.TreeRoot.OwnerChanged -= rootNode.OwnerChangedhandler;
					rootNode.RootObjectId.ObjectList.TreeRoot.OwnerChanged += rootNode.OwnerChangedhandler;
					rootNode.TreeChild = rootNode.RootObjectId.ObjectList.TreeRoot;//.GetAllObjectsInTree();
				}
				if (rootNode.NextLevelLoaded)
				{
					UInt32 scopeId = 0;
					MethodClass mc = ScopeMethod as MethodClass;
					if (mc != null)
					{
						scopeId = mc.MethodID;
					}

					Tree t = rootNode.TreeChild.SearchChildByOwner(obj);
					TreeNodeClass ndContainer = null;
					if (t != null && t.Parent != null)
					{
						ndContainer = rootNode.FindComponentNode(t.Parent.Owner);
					}
					if (ndContainer == null)
					{
						ndContainer = rootNode.FindContainerNode(obj);
					}
					if (ndContainer != null)
					{
						if (ndContainer.NextLevelLoaded)
						{
							TreeNodeClassComponent tncc = CreateComponentTreeNode(RootId, obj, ObjectIDList.GetObjectID(obj), scopeId, EnumObjectSelectType.All, null, null);
							tncc.TreeChild = t;
							ndContainer.Nodes.Add(tncc);
						}
					}
				}
			}
		}
		public void LoadReferences(NamespaceList nl)
		{
			Dictionary<string, Assembly> list = new Dictionary<string, Assembly>();
			StringCollection sc = new StringCollection();
			if (this.SelectionType != EnumObjectSelectType.Interface)
			{
				List<Type> tList = _project.GetToolboxItems(sc);
				if (sc.Count > 0)
				{
					MathNode.Log(sc);
				}
				if (tList.Count > 0)
				{
					foreach (Type t in tList)
					{
						if (!list.ContainsKey(t.Assembly.FullName))
						{
							list.Add(t.Assembly.FullName, t.Assembly);
						}
					}
				}
			}
			sc = new StringCollection();
			List<Assembly> nodeList = _project.GetReferences(sc);
			if (sc.Count > 0)
			{
				MathNode.Log(sc);
			}
			if (nodeList != null)
			{
				foreach (Assembly nd in nodeList)
				{
					if (!list.ContainsKey(nd.FullName))
					{
						list.Add(nd.FullName, nd);
					}
				}
			}
			//
			nl.AddAssembly(typeof(object).Assembly);
			nl.AddAssembly(typeof(Form).Assembly);
			nl.AddAssembly(typeof(DrawingItem).Assembly);
			nl.AddAssembly(typeof(IXDesignerViewer).Assembly);
			nl.AddAssembly(typeof(LimnorDatabase.EasyDataSet).Assembly);
			nl.AddAssembly(typeof(DialogPluginManager).Assembly);
			nl.AddAssembly(typeof(WindowsManager).Assembly);
			nl.AddAssembly(typeof(DialogStringCollection).Assembly);
			nl.AddAssembly(typeof(LKiosk).Assembly);
			nl.AddAssembly(typeof(Limnor.CopyProtection.CopyProtector).Assembly);
			nl.AddAssembly(typeof(Image).Assembly);
			nl.AddAssembly(typeof(WebServiceAttribute).Assembly);
			nl.AddAssembly(typeof(DataSet).Assembly);
			nl.AddAssembly(typeof(XmlDocument).Assembly);
			nl.AddAssembly(typeof(System.Net.Mail.SmtpClient).Assembly);
#if DOTNET40
			nl.AddAssembly(typeof(System.Windows.Forms.DataVisualization.Charting.Chart).Assembly);
#endif
			Type tp = XmlUtil.GetKnownType("StringTool");
			if (tp != null)
			{
				nl.AddAssembly(tp.Assembly);
			}
			tp = XmlUtil.GetKnownType("TextBoxNumber");
			if (tp != null)
			{
				nl.AddAssembly(tp.Assembly);
			}
			DesignUtil.LogIdeProfile("Load types in class");
			XmlNode rootXml = this.RootObjectNode;
			if (rootXml != null)
			{
				XmlNode typesNode = rootXml.SelectSingleNode("Types");
				if (typesNode != null)
				{
					XmlNodeList typeNodes = typesNode.SelectNodes("*[@type]");
					if (typeNodes != null && typeNodes.Count > 0)
					{
						foreach (XmlNode nd in typeNodes)
						{
							Type t = XmlUtil.GetLibTypeAttribute(nd);
							if (t != null)
							{
								nl.AddAssembly(t.Assembly);
							}
						}
					}
				}
			}
			DesignUtil.LogIdeProfile("add project references");
			foreach (KeyValuePair<string, Assembly> kv in list)
			{
				nl.AddAssembly(kv.Value);
			}
			DesignUtil.LogIdeProfile("finish adding project references");
		}
		public IList<Type> LoadExternalTypes(LimnorProject project, List<Type> loadedTypes)
		{
			StringCollection sc = new StringCollection();
			List<Type> types = project.GetToolboxItems(sc);
			if (sc.Count > 0)
			{
				MathNode.Log(sc);
			}
			foreach (Type t in types)
			{
				if ((SelectionType != EnumObjectSelectType.Interface || t.IsInterface))
				{
					if (loadedTypes != null && loadedTypes.Contains(t))
					{
						continue;
					}
					TreeNodeClassType nc = new TreeNodeClassType(this, t, ScopeMethodId);
					nc.CanRemove = true;
					nc.SetNodeIcon();
					Nodes.Add(nc);
				}
			}
			return types;
		}
		public TreeNode LocateNode(IObjectIdentity id)
		{
			IObjectPointer pointer = id as IObjectPointer;
			if (pointer != null)
				return LocateNode(pointer);
			IAction act = id as IAction;
			if (act != null)
			{
				TreeNodeObject nodeMethod = FindObjectNode(act.ActionMethod);
				if (nodeMethod != null)
				{
					nodeMethod.LoadNextLevel();
					for (int i = 0; i < nodeMethod.Nodes.Count; i++)
					{
						TreeNodeAction na = nodeMethod.Nodes[i] as TreeNodeAction;
						if (na != null)
						{
							if (na.Action.WholeActionId == act.WholeActionId)
							{
								return na;
							}
						}
					}
				}
			}
			return null;
		}
		public TreeNode LocateNode(IObjectPointer pointer)
		{
			List<Type> tps = new List<Type>(new Type[] { TreeNodeType(pointer) });
			LocalVariable lv = pointer as LocalVariable;
			if (lv != null)
			{
				tps.Add(typeof(TreeNodeLocalVariable));
			}
			return locateNode(pointer, tps, Nodes);
		}
		public void ResetNodes(bool isStatic, Type nodeType)
		{
			resetNodes(isStatic, nodeType, this.Nodes);
		}
		public void ResetNodes(IObjectPointer pointer, bool showIndicator)
		{
			IPropertySetter sp = pointer as IPropertySetter;
			if (sp != null)
			{
				pointer = sp.SetProperty;
			}
			resetNodes(pointer, TreeNodeType(pointer), Nodes, showIndicator);
		}
		public void DeleteComponent(IComponent component)
		{
			RootClassNode.ClassData.DesignerHolder.Loader.DeleteComponent(component);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="act"></param>
		public bool DeleteAction(IAction act)
		{
			ClassPointer root = RootId;
			return root.AskDeleteAction(act, FindForm());
		}
		public void ChangeActionName(ActionClass act)
		{
			changeActionName(this.Nodes, act);
		}
		public void ResetClassNodes()
		{
			RootClassNode.ResetNextLevel(this);
			RootClassNode.LoadNextLevel();
			for (int i = 0; i < RootClassNode.Nodes.Count; i++)
			{
				TreeNodeClassComponent tnc = RootClassNode.Nodes[i] as TreeNodeClassComponent;
				if (tnc != null)
				{
					tnc.ResetNextLevel(this);
				}
			}
			RootClassNode.ClassData.RootClassID.ReloadEventActions(null);
		}
		public TreeNodeClassRoot GetRootClassNode(int classId)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClassRoot r = this.Nodes[i] as TreeNodeClassRoot;
				if (r != null)
				{
					if (((ClassPointer)(r.OwnerPointer)).ClassId == classId)
					{
						return r;
					}
				}
			}
			return null;
		}
		public TreeNodeNamespaceList GetNamespaceListNode()
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeNamespaceList tn = Nodes[i] as TreeNodeNamespaceList;
				if (tn != null)
				{
					return tn;
				}
			}
			return null;
		}
		public TreeNodeNamespace GetNamespaceNode(string namespaceString)
		{
			TreeNodeNamespaceList nsList = GetNamespaceListNode();
			if (nsList != null)
			{
				nsList.LoadNextLevel();
				for (int i = 0; i < nsList.Nodes.Count; i++)
				{
					TreeNodeNamespace ns = nsList.Nodes[i] as TreeNodeNamespace;
					if (ns != null)
					{
						if (ns.Namespace.ToString() == namespaceString)
						{
							return ns;
						}
					}
				}
			}
			return null;
		}
		public TreeNodeClassType GetTypeNode(TypePointer tp)
		{
			TreeNodeNamespace nsNode = GetNamespaceNode(tp.ClassType.Namespace);
			if (nsNode != null)
			{
				nsNode.LoadNextLevel();
				for (int i = 0; i < nsNode.Nodes.Count; i++)
				{
					TreeNodeClassType tyNode = nsNode.Nodes[i] as TreeNodeClassType;
					if (tyNode != null)
					{
						if (tp.IsSameObjectRef(tyNode.OwnerPointer))
						{
							return tyNode;
						}
					}
				}
			}
			return null;
		}
		public TreeNodeClassType GetRootTypeNode(TreeNodeCollection startCollection, DataTypePointer tp)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClassType c = this.Nodes[i] as TreeNodeClassType;
				if (c != null)
				{
					if (tp.IsSameObjectRef(c.OwnerPointer))
					{
						return c;
					}
				}
			}
			return null;
		}
		public ILimnorDesignerLoader DesignerLoader
		{
			get
			{
				TreeNodeClassRoot r = DesignerRootNode;
				if (r != null)
				{
					return r.DesignerLoader;
				}
				return null;
			}
		}
		public TreeNodeClassRoot RootClassNode
		{
			get
			{
				if (_mainRootNode == null)
				{
					TreeNodeClassRoot m;
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						m = this.Nodes[i] as TreeNodeClassRoot;
						if (m != null && m.IsMainRoot)
						{
							_mainRootNode = m;
							break;
						}
					}
				}
				return _mainRootNode;
			}
		}
		public void SetRoortID(ClassPointer rootId)
		{
			_rootPointer = rootId;
		}
		public TreeNodeClassRoot SelectRootClassNode(UInt32 classId)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClassRoot dcr = this.Nodes[i] as TreeNodeClassRoot;
				if (dcr != null)
				{
					if (dcr.ClassData.RootClassID.ClassId == classId)
					{
						return dcr;
					}
				}
			}
			return null;
		}
		private TreeNodeObject findLocalVariableNode(LocalVariable pointer)
		{
			//find the MethodClass node
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeObject nd = Nodes[i] as TreeNodeObject;
				if (nd != null && nd.OwnerPointer != null)
				{
					if (nd.OwnerPointer.IsSameObjectRef(pointer.Owner))
					{
						nd.LoadNextLevel();
						for (int j = 0; j < nd.Nodes.Count; j++)
						{
							TreeNodeObject nd2 = nd.Nodes[j] as TreeNodeObject;
							if (nd2 != null && nd2.OwnerPointer != null)
							{
								if (nd2.OwnerPointer.IsSameObjectRef(pointer))
								{
									this.SelectedNode = nd2;
									return nd2;
								}
							}
						}
						this.SelectedNode = nd;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// locate the treenode corresponding to pointer
		/// </summary>
		/// <param name="pointer"></param>
		public void SetSelection(TreeNodeCollection startCollection, IObjectPointer pointer)
		{
			if (pointer == null)
				return;
			ParameterClass pc0 = pointer as ParameterClass;
			if (pc0 != null)
			{
				for (int i = 0; i < startCollection.Count; i++)
				{
					TreeNodeCustomMethod tno = startCollection[i] as TreeNodeCustomMethod;
					if (tno != null)
					{
						tno.LoadNextLevel();
						for (int j = 0; j < tno.Nodes.Count; j++)
						{
							TreeNodeObject tn = tno.Nodes[j] as TreeNodeObject;
							if (tn != null)
							{
								if (pc0.IsSameObjectRef(tn.OwnerPointer))
								{
									this.SelectedNode = tn;
									break;
								}
							}
						}
						break;
					}
				}
				return;
			}
			DataTypePointer dtp = pointer as DataTypePointer;
			if (dtp != null)
			{
				for (int i = 0; i < startCollection.Count; i++)
				{
					TreeNodeObject tno = startCollection[i] as TreeNodeObject;
					if (tno != null)
					{
						if (dtp.IsSameObjectRef(tno.OwnerPointer))
						{
							this.SelectedNode = tno;
							return;
						}
					}
				}
				return;
			}
			CustomPropertyOverridePointer cpop = pointer as CustomPropertyOverridePointer;
			if (cpop != null)
			{
				if (cpop.UseBaseValue)
				{
					for (int i = 0; i < startCollection.Count; i++)
					{
						TreeNodeCustomMethod tno = startCollection[i] as TreeNodeCustomMethod;
						if (tno != null)
						{
							tno.LoadNextLevel();
							for (int j = 0; j < tno.Nodes.Count; j++)
							{
								TreeNodeObject tn = tno.Nodes[j] as TreeNodeObject;
								if (tn != null)
								{
									if (cpop.IsSameObjectRef(tn.OwnerPointer))
									{
										this.SelectedNode = tn;
										break;
									}
								}
							}
							break;
						}
					}
					return;
				}
			}
			LocalVariable lv = pointer as LocalVariable;
			if (lv != null)
			{
				findLocalVariableNode(lv);
				return;
			}
			////////////////////////////////////////////////////
			IObjectPointer p = pointer;
			Stack<IObjectPointer> pointerStack = new Stack<IObjectPointer>();
			pointerStack.Push(pointer);
			//
			MethodClass customMethod = null;
			while (!(p is LocalVariable) && p.Owner != null)
			{
				if (customMethod == null)
				{
					customMethod = p as MethodClass;
				}
				p = p.Owner;
				pointerStack.Push(p);
			}
			TreeNodeObject node = null;
			p = pointerStack.Pop();
			if (customMethod != null)
			{
				for (int i = 0; i < startCollection.Count; i++)
				{
					TreeNodeObject tno = startCollection[i] as TreeNodeObject;
					if (tno != null)
					{
						MethodClass im = tno.OwnerPointer as MethodClass;
						if (im != null)
						{
							if (im.MemberId == customMethod.MemberId)
							{
								node = tno;
								break;
							}
						}
					}
				}
				if (node != null)
				{
					while (p != null && p != customMethod)
					{
						p = pointerStack.Pop();
					}
				}
			}
			if (node == null)
			{
				ParameterClass pc = p as ParameterClass;
				if (pc != null)
				{
					for (int i = 0; i < startCollection.Count; i++)
					{
						TreeNodeObject tno = startCollection[i] as TreeNodeObject;
						if (tno != null)
						{
							MethodClass im = tno.OwnerPointer as MethodClass;
							if (im != null)
							{
								node = tno;
								tno.LoadNextLevel();
								for (int j = 0; j < tno.Nodes.Count; j++)
								{
									TreeNodeObject tnoP = tno.Nodes[j] as TreeNodeObject;
									if (tnoP != null)
									{
										if (pc.IsSameObjectRef(tnoP.OwnerPointer))
										{
											node = tnoP;
											break;
										}
									}
								}
								break;
							}
						}
					}
				}
				else
				{
					LocalVariable lvar = p as LocalVariable;
					if (lvar != null)
					{
						node = findLocalVariableNode(lvar);
					}
					else
					{
						DataTypePointer tp = p as DataTypePointer;
						if (tp != null)
						{
							if (tp.IsLibType)
							{
								for (int i = 0; i < startCollection.Count; i++)
								{
									TreeNodePrimaryCollection tc = startCollection[i] as TreeNodePrimaryCollection;
									if (tc != null)
									{
										//try primary types first
										ForceLoadNextLevel(tc);
										for (int j = 0; j < tc.Nodes.Count; j++)
										{
											TreeNodeClassType typeNode = tc.Nodes[j] as TreeNodeClassType;
											if (typeNode != null)
											{
												if (p.IsSameObjectRef(typeNode.OwnerPointer))
												{
													node = typeNode;
													break;
												}
											}
										}
									}
									else
									{
										TreeNodeClassType tct = startCollection[i] as TreeNodeClassType;
										if (tct != null)
										{
											if (tct.OwnerPointer.IsSameObjectRef(p))
											{
												node = tct;
												break;
											}
										}
									}
								}
								if (node == null)
								{
									//try Namespaces
									TreeNodeClassType ct = GetTypeNode(tp.LibTypePointer);
									if (ct != null)
									{
										node = ct;
									}
									if (node == null)
									{
										//try TreeNodeClassType nodes directly under startCollection
										ct = GetRootTypeNode(startCollection, tp);
										if (ct != null)
										{
											node = ct;
										}
									}
								}
							}
							else //not a lib type, find its defining class
							{
								for (int i = 0; i < startCollection.Count; i++)
								{
									TreeNodeClassRoot cr = startCollection[i] as TreeNodeClassRoot;
									if (cr != null)
									{
										if (p.IsSameObjectRef(cr.OwnerPointer))
										{
											node = cr;
											break;
										}
									}
								}
							}
						}
						else //not a DatatTypePointer
						{
							ClassPointer cp = p as ClassPointer;
							if (cp != null)
							{
								for (int i = 0; i < startCollection.Count; i++)
								{
									TreeNodeClassRoot tcr = startCollection[i] as TreeNodeClassRoot;
									if (tcr != null)
									{
										if (tcr.OwnerPointer.IsSameObjectRef(p))
										{
											node = tcr;
											break;
										}
									}
								}
							}
							else
							{
								TypePointer tpr = p as TypePointer;
								if (tpr != null && tpr.ClassType != null)
								{
									if (tpr.IsJsType())
									{
										for (int i = 0; i < startCollection.Count; i++)
										{
											TreeNodePrimaryCollection tnpc = startCollection[i] as TreeNodePrimaryCollection;
											if (tnpc != null)
											{
												if (tnpc.WebTypes == EnumWebTypes.JavaScript)
												{
													tnpc.Expand();
													for (int k = 0; k < tnpc.Nodes.Count; k++)
													{
														TreeNodeClassType tnct = tnpc.Nodes[k] as TreeNodeClassType;
														if (tnct != null)
														{
															if (tpr.ClassType.Equals(tnct.OwnerDataType))
															{
																node = tnct;
																break;
															}
														}
													}
													if (node != null)
													{
														break;
													}
												}
											}
										}
									}
									else if (tpr.IsPhpType())
									{
										for (int i = 0; i < startCollection.Count; i++)
										{
											TreeNodePrimaryCollection tnpc = startCollection[i] as TreeNodePrimaryCollection;
											if (tnpc != null)
											{
												if (tnpc.WebTypes == EnumWebTypes.Php)
												{
													tnpc.Expand();
													for (int k = 0; k < tnpc.Nodes.Count; k++)
													{
														TreeNodeClassType tnct = tnpc.Nodes[k] as TreeNodeClassType;
														if (tnct != null)
														{
															if (tpr.ClassType.Equals(tnct.OwnerDataType))
															{
																node = tnct;
																break;
															}
														}
													}
													if (node != null)
													{
														break;
													}
												}
											}
										}
									}
									else
									{
										for (int i = 0; i < startCollection.Count; i++)
										{
											TreeNodePrimaryCollection tc = startCollection[i] as TreeNodePrimaryCollection;
											if (tc != null)
											{
												//try primary types first
												ForceLoadNextLevel(tc);
												for (int j = 0; j < tc.Nodes.Count; j++)
												{
													TreeNodeClassType typeNode = tc.Nodes[j] as TreeNodeClassType;
													if (typeNode != null)
													{
														if (tpr.ClassType.Equals(typeNode.OwnerDataType))
														{
															node = typeNode;
															break;
														}
													}
												}
											}
											else
											{
												TreeNodeClassType tct = startCollection[i] as TreeNodeClassType;
												if (tct != null)
												{
													if (tpr.ClassType.Equals(tct.OwnerDataType))
													{
														node = tct;
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
			}
			if (node == null)
			{
				TreeNodeClassRoot rootClass = RootClassNode;
				if (rootClass != null)
				{
					if (rootClass.OwnerPointer.IsSameObjectRef(p))
					{
						node = rootClass;
					}
					else
					{
						ForceLoadNextLevel(rootClass);
						for (int i = 0; i < rootClass.Nodes.Count; i++)
						{
							TreeNodeObject nd = rootClass.Nodes[i] as TreeNodeObject;
							if (nd != null && nd.OwnerPointer != null)
							{
								if (nd.OwnerPointer.IsSameObjectRef(p))
								{
									node = nd;
									break;
								}
							}
						}
					}
				}
			}
			bool bGo = (pointerStack.Count > 0 && node != null);
			while (bGo)
			{
				p = pointerStack.Pop();
				TreeNodeObject nd = SetSelection(node, p);
				if (nd == null)
					break;
				node = nd;
				if (pointerStack.Count == 0)
					break;
			}
			this.SelectedNode = node;
		}
		/// <summary>
		/// use ClassId to locate Root Class Node
		/// </summary>
		/// <param name="act"></param>
		/// <returns></returns>
		public TreeNodeAction FindActionNode(IAction act)
		{
			if (act == null)
			{
				throw new DesignerException("Calling FindActionNode with null act");
			}
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassRoot tnc = Nodes[i] as TreeNodeClassRoot;
				if (tnc != null)
				{
					if (act.ClassId == tnc.ClassId)
					{
						ITreeNodeClass classNode = tnc.GetClassTreeNode(act.ExecuterClassId);
						if (classNode != null)
						{
							return classNode.FindActionNode(act);
						}
					}
				}
			}
			return null;
		}
		private void searchNodeByType<T>(List<T> list, TreeNodeCollection nodes)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				if (typeof(T).IsAssignableFrom(nodes[i].GetType()))
				{
					object v = nodes[i];
					list.Add((T)v);
				}
				else
				{
					searchNodeByType<T>(list, nodes[i].Nodes);
				}
			}
		}
		public void SearchNodeByType<T>(List<T> list)
		{
			searchNodeByType<T>(list, Nodes);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pointer"></param>
		/// <returns></returns>
		public TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			if (pointer == null)
				return null;
			IAction act = pointer as IAction;
			if (act != null)
			{
				return FindActionNode(act);
			}
			IClass holder = DesignUtil.GetClass(pointer);
			if (holder == null)
				return null;
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClass nodeObj = this.Nodes[i] as TreeNodeClass;
				if (nodeObj != null)
				{
					if (nodeObj.ClassId == holder.ClassId)
					{
						if (nodeObj.MemberId == holder.MemberId)
						{
							return nodeObj.FindObjectNode(pointer);
						}
						else
						{
							nodeObj.LoadNextLevel();
							for (int j = 0; j < nodeObj.Nodes.Count; j++)
							{
								TreeNodeClassComponent nodeCo = nodeObj.Nodes[j] as TreeNodeClassComponent;
								if (nodeCo != null)
								{
									if (nodeCo.MemberId == holder.MemberId)
									{
										return nodeCo.FindObjectNode(pointer);
									}
								}
							}
						}
						break;
					}
				}
			}
			return null;
		}
		public TreeNodeObject SetSelection(TreeNodeObject node, IObjectPointer p)
		{
			TypePointer tp = p as TypePointer;
			if (tp != null)
			{
				if (tp.ClassType != null)
				{
					if (JsTypeAttribute.IsJsType(tp.ClassType))
					{
						TreeNodePrimaryCollection tnpc = null;
						for (int i = 0; i < this.Nodes.Count; i++)
						{
							TreeNodePrimaryCollection tnpc0 = this.Nodes[i] as TreeNodePrimaryCollection;
							if (tnpc0 != null)
							{
								if (tnpc0.RunAt == EnumWebRunAt.Client)
								{
									tnpc = tnpc0;
									break;
								}
							}
						}
						if (tnpc != null)
						{
							tnpc.LoadNextLevel();
							for (int i = 0; i < tnpc.Nodes.Count; i++)
							{
								TreeNodeClassType nt = tnpc.Nodes[i] as TreeNodeClassType;
								if (nt != null)
								{
									if (tp.ClassType.Equals(nt.OwnerDataType))
									{
										return nt;
									}
								}
							}
						}
					}
					else if (PhpTypeAttribute.IsPhpType(tp.ClassType))
					{
						TreeNodePrimaryCollection tnpc = null;
						for (int i = 0; i < this.Nodes.Count; i++)
						{
							TreeNodePrimaryCollection tnpc0 = this.Nodes[i] as TreeNodePrimaryCollection;
							if (tnpc0 != null)
							{
								if (tnpc0.RunAt == EnumWebRunAt.Server)
								{
									tnpc = tnpc0;
									break;
								}
							}
						}
						if (tnpc != null)
						{
							tnpc.LoadNextLevel();
							for (int i = 0; i < tnpc.Nodes.Count; i++)
							{
								TreeNodeClassType nt = tnpc.Nodes[i] as TreeNodeClassType;
								if (nt != null)
								{
									if (tp.ClassType.Equals(nt.OwnerDataType))
									{
										return nt;
									}
								}
							}
						}
					}
					else
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeClassType nt = Nodes[i] as TreeNodeClassType;
							if (nt != null)
							{
								if (tp.ClassType.Equals(nt.OwnerDataType))
								{
									return nt;
								}
							}
						}
					}
				}
			}
			TreeNodeObject nodeRet = null;
			this.ForceLoadNextLevel(node);
			if (p is EventPointer || p is MethodPointer || p is PropertyPointer || p is FieldPointer || p is MethodClass || p is CustomPropertyPointer)
			{
				TreeNodeObject collectionHolder = null;
				TreeNodePME nodeAttribs = node as TreeNodePME;
				if (nodeAttribs == null)
				{
					for (int i = 0; i < node.Nodes.Count; i++)
					{
						nodeAttribs = node.Nodes[i] as TreeNodePME;
						if (nodeAttribs != null)
						{
							break;
						}
					}
				}
				if (nodeAttribs != null)
				{
					this.ForceLoadNextLevel(nodeAttribs);
					collectionHolder = nodeAttribs;
				}
				else
				{
					collectionHolder = node;
				}
				TreeNodeObject nodeCollection = null;
				for (int i = 0; i < collectionHolder.Nodes.Count; i++)
				{
					if (p is EventPointer)
					{
						if (collectionHolder.Nodes[i] is TreeNodeEventCollection)
						{
							nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
							if (nodeCollection.IsStatic == p.IsStatic)
							{
								break;
							}
						}
					}
					else if (p is MethodPointer)
					{
						if (p is SetterPointer)
						{
							if (collectionHolder.Nodes[i] is TreeNodePropertyCollection)
							{
								nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
								if (nodeCollection.IsStatic == p.IsStatic)
								{
									break;
								}
							}
						}
						else
						{
							if (collectionHolder.Nodes[i] is TreeNodeMethodCollection)
							{
								nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
								if (nodeCollection.IsStatic == p.IsStatic)
								{
									break;
								}
							}
						}
					}
					else if (p is PropertyPointer)
					{
						if (collectionHolder.Nodes[i] is TreeNodePropertyCollection)
						{
							nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
							if (nodeCollection.IsStatic == p.IsStatic)
							{
								break;
							}
						}
					}
					else if (p is FieldPointer)
					{
						if (collectionHolder.Nodes[i] is TreeNodeFieldCollection)
						{
							nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
							if (nodeCollection.IsStatic == p.IsStatic)
							{
								break;
							}
						}
					}
					else if (p is MethodClass)
					{
						if (collectionHolder.Nodes[i] is TreeNodeCustomMethodCollection)
						{
							nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
							if (nodeCollection.IsStatic == p.IsStatic)
							{
								break;
							}
						}
					}
					else if (p is CustomPropertyPointer)
					{
						if (collectionHolder.Nodes[i] is TreeNodeCustomPropertyCollection)
						{
							nodeCollection = collectionHolder.Nodes[i] as TreeNodeObject;
							if (nodeCollection.IsStatic == p.IsStatic)
							{
								break;
							}
						}
					}
				}
				if (nodeCollection != null)
				{
					this.ForceLoadNextLevel(nodeCollection);
					for (int k = 0; k < nodeCollection.Nodes.Count; k++)
					{
						TreeNodeObject nd = nodeCollection.Nodes[k] as TreeNodeObject;
						if (p is SetterPointer)
						{
							if (nd.OwnerPointer.IsSameObjectRef(((SetterPointer)p).SetProperty))
							{
								nodeRet = nd;
								break;
							}
						}
						else
						{
							if (nd.OwnerPointer.IsSameObjectRef(p))
							{
								nodeRet = nd;
								break;
							}
						}
					}
				}
			}
			else if (p is EventParamPointer || p is MethodParamPointer || p is CustomMethodParameterPointer)
			{
				for (int k = 0; k < node.Nodes.Count; k++)
				{
					TreeNodeObject nd = node.Nodes[k] as TreeNodeObject;
					if (nd.OwnerPointer.IsSameObjectRef(p))
					{
						nodeRet = nd;
						break;
					}
				}
			}
			else if (p is MemberComponentId)
			{
				for (int k = 0; k < node.Nodes.Count; k++)
				{
					TreeNodeClassComponent nc = node.Nodes[k] as TreeNodeClassComponent;
					if (nc != null)
					{
						if (p.IsSameObjectRef(nc.OwnerIdentity))
						{
							return nc;
						}
					}
				}
			}
			else if (p is LocalVariable)
			{
				for (int k = 0; k < node.Nodes.Count; k++)
				{
					TreeNodeLocalVariable nc = node.Nodes[k] as TreeNodeLocalVariable;
					if (nc != null)
					{
						if (p.IsSameObjectRef(nc.OwnerIdentity))
						{
							return nc;
						}
					}
				}
			}
			else if (p is ClassInstancePointer)
			{
				for (int k = 0; k < node.Nodes.Count; k++)
				{
					TreeNodeClassComponentCust nc = node.Nodes[k] as TreeNodeClassComponentCust;
					if (nc != null)
					{
						if (p.IsSameObjectRef(nc.OwnerIdentity))
						{
							return nc;
						}
					}
				}
			}
			return nodeRet;
		}

		public TreeNodeClassComponent GetObjectNodeByKey(string objectKey)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassComponent dc = Nodes[i] as TreeNodeClassComponent;
				if (dc != null)
				{
					if (dc.OwnerPointer.ObjectKey == objectKey)
					{
						return dc;
					}
					dc = dc.GetObjectNodeByKey(objectKey);
					if (dc != null)
					{
						return dc;
					}
				}
			}
			return null;
		}
		public void NotifyChanges()
		{
			if (!ReadOnly && RootClassNode.ClassData.DesignerHolder != null)
			{
				RootClassNode.ClassData.DesignerHolder.Loader.NotifyChanges();
			}
		}
		public void AddNewHandler(EventAction handler)
		{
			RootClassNode.GetEventHandlers().Add(handler);
			RootId.SaveEventHandler(handler);
			//
			NotifyChanges();
		}
		public void NotifyActionSelection(IAction action)
		{
			TreeNodeClassRoot r = RootClassNode;
			if (r != null)
			{
				if (r.ViewersHolder != null)
				{
					r.ViewersHolder.OnActionSelected(action);
				}
			}
		}
		public void LoadDLL()
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = ".Net DLL|*.Dll";
			dlg.Title = "Select a .Net DLL file";
			if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
			{
				try
				{
					Assembly a = Assembly.LoadFile(dlg.FileName);
					AddAssemblyNode(a);
					Project.AddReference(a.Location);
				}
				catch (Exception er)
				{
					MessageBox.Show(this.FindForm(), er.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#endregion
		#region Notification
		public void OnClosing()
		{
			TreeNodeDocCollection tndc = null;
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				tndc = this.Nodes[i] as TreeNodeDocCollection;
				if (tndc != null)
				{
					break;
				}
			}
			if (tndc != null)
			{
				tndc.CloseAllDocs();
			}
		}
		public void OnAddExternalType(UInt32 classId, Type t)
		{
			TreeNodeClassRoot r = DesignerRootNode;
			if (r != null)
			{
				if (r.ClassId == classId)
				{
					Nodes.Add(new TreeNodeClassType(this, new TypePointer(t), r.ClassData, ScopeMethodId));
				}
			}
		}
		public void OnRemoveExternalType(UInt32 classId, Type t)
		{
			TreeNodeClassRoot r = DesignerRootNode;
			if (r != null)
			{
				if (r.ClassId == classId)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodeClassType nc = Nodes[i] as TreeNodeClassType;
						if (nc != null)
						{
							if (t.Equals(nc.OwnerDataType))
							{
								nc.Remove();
								break;
							}
						}
					}
				}
			}
		}
		public void OnActionDeleted(IAction action)
		{
			removeActionNode(this.Nodes, action.ActionId);
		}
		public void OnDeleteMethod(MethodClass method)
		{
			TreeNodeClassRoot rn = this.RootClassNode;
			if (method.ClassId == this.RootId.ClassId)
			{
				rn.ClassData.RootClassID.ReloadEventActions(null);
				if (method.Holder.IsStatic)
				{
					ResetNodes(method.IsStatic, typeof(TreeNodeActionMixedCollection));
					ResetNodes(method.IsStatic, typeof(TreeNodeMethodMixedCollection));
				}
				else
				{
					ResetNodes(method.IsStatic, typeof(TreeNodeActionCollection));
					ResetNodes(method.IsStatic, typeof(TreeNodeCustomMethodCollection));
				}
				MethodOverride po = method as MethodOverride;
				if (po != null)
				{
					TreeNodeOverridesInterfaces tnoi = rn.GetOverridesInterfacesNode();
					if (tnoi != null)
					{
						tnoi.OnMethodRemoved(po);
					}
				}
			}
			else
			{
				IList<TreeNodeClassComponentCust> list = rn.GetInstancesByClassId(method.ClassId);
				if (list.Count > 0)
				{
					foreach (TreeNodeClassComponentCust tnc in list)
					{
						TreeNodePME pme = tnc.GetPointerMembersNode(method.IsStatic);
						if (pme != null)
						{
							TreeNodeCustomMethodPointerCollection ms = pme.GetCustomMethodPointersNode();
							if (ms != null && ms.NextLevelLoaded)
							{
								TreeNodeCustomMethodPointer tmp = ms.GetMethodPointerNode(method.MemberId);
								if (tmp != null)
								{
									tmp.Remove();
								}
							}
						}
					}
				}
			}
		}
		public void OnDeleteEventMethod(EventHandlerMethod method)
		{
			TreeNodeObject tno = FindObjectNode(method.Event);
			if (tno != null)
			{
				for (int i = 0; i < tno.Nodes.Count; i++)
				{
					TreeNodeEventMethod tnem = tno.Nodes[i] as TreeNodeEventMethod;
					if (tnem != null)
					{
						if (tnem.Method.WholeId == method.WholeId)
						{
							tno.Nodes.Remove(tnem);
							break;
						}
					}
				}
			}
		}
		public void OnPropertySelected(PropertyClass property)
		{
			if (property.ClassId == this.RootId.ClassId)
			{
				TreeNodeClassRoot rn = this.RootClassNode;
				TreeNodeCustomProperty tncp = rn.GetCustomPropertyNode(property);
				if (tncp != null)
				{
					this.SelectedNode = tncp;
				}
			}
		}
		public void OnDeleteProperty(PropertyClass property)
		{
			if (property.ClassId == this.RootId.ClassId)
			{
				TreeNodeClassRoot rn = this.RootClassNode;
				rn.ClassData.RootClassID.ReloadEventActions(null);
				if (property.Holder.IsStatic)
				{
					ResetNodes(property.IsStatic, typeof(TreeNodeActionMixedCollection));
					ResetNodes(property.IsStatic, typeof(TreeNodePropertyMixedCollection));
				}
				else
				{
					ResetNodes(property.IsStatic, typeof(TreeNodeActionCollection));
					ResetNodes(property.IsStatic, typeof(TreeNodeCustomPropertyCollection));
				}
				PropertyOverride po = property as PropertyOverride;
				if (po != null)
				{
					TreeNodeOverridesInterfaces tnoi = rn.GetOverridesInterfacesNode();
					if (tnoi != null)
					{
						tnoi.OnPropertyRemoved(po);
					}
				}
			}
			else
			{
				TreeNodeClassRoot rootNode = RootClassNode;
				if (rootNode != null)
				{
					IList<TreeNodeClassComponentCust> list = rootNode.GetInstancesByClassId(property.ClassId);
					if (list.Count > 0)
					{
						foreach (TreeNodeClassComponentCust tnc in list)
						{
							MemberComponentIdCustom mc = (MemberComponentIdCustom)(tnc.OwnerPointer);
							mc.Pointer.DeleteCustomProperty(property.MemberId);
							TreeNodePME pme = tnc.GetPointerMembersNode(property.IsStatic);
							if (pme != null)
							{
								TreeNodeCustomPropertyPointerCollection pp = pme.GetCustomPropertyPointersNode();
								if (pp != null)
								{
									TreeNodeCustomPropertyPointer cpp = pp.GetPropertyPointerNode(property.MemberId);
									if (cpp != null)
									{
										cpp.Remove();
									}
								}
							}
						}
					}
				}
			}
		}
		public void OnAddProperty(PropertyClass property)
		{
			TreeNodeClassRoot rn = this.RootClassNode;
			if (property.ClassId == this.RootId.ClassId)
			{
				rn.ClassData.RootClassID.ReloadEventActions(null);
				if (property.Holder.IsStatic)
				{
					ResetNodes(property.IsStatic, typeof(TreeNodeActionMixedCollection));
					ResetNodes(property.IsStatic, typeof(TreeNodePropertyMixedCollection));
				}
				else
				{
					ResetNodes(property.IsStatic, typeof(TreeNodeActionCollection));
					ResetNodes(property.IsStatic, typeof(TreeNodeCustomPropertyCollection));
				}
				PropertyOverride po = property as PropertyOverride;
				if (po != null)
				{
					TreeNodeOverridesInterfaces tnoi = rn.GetOverridesInterfacesNode();
					if (tnoi != null)
					{
						tnoi.OnPropertyImplemented(po);
					}
				}
			}
			else
			{
				IList<TreeNodeClassComponentCust> list = RootClassNode.GetInstancesByClassId(property.ClassId);
				if (list.Count > 0)
				{
					foreach (TreeNodeClassComponentCust tnc in list)
					{
						MemberComponentIdCustom mc = (MemberComponentIdCustom)(tnc.OwnerPointer);
						mc.Pointer.AddCustomProperty(property);
						TreeNodePME pme = tnc.GetPointerMembersNode(property.IsStatic);
						if (pme != null)
						{
							TreeNodeCustomPropertyPointerCollection ps = pme.GetCustomPropertyPointersNode();
							if (ps != null && ps.NextLevelLoaded)
							{
								TreeNodeCustomPropertyPointer tcp = ps.GetPropertyPointerNode(property.MemberId);
								if (tcp == null)
								{
									CustomPropertyPointer cpp = new CustomPropertyPointer(property, (IClass)(ps.OwnerPointer.ObjectInstance));
									tcp = new TreeNodeCustomPropertyPointer(property.IsStatic, cpp);
									ps.Nodes.Insert(0, tcp);
								}
							}
						}
					}
				}
			}
		}
		public void OnPropertyChanged(INonHostedObject property, string name)
		{
			if (property.ClassId == this.RootId.ClassId)
			{
				PropertyClass pc = property as PropertyClass;
				if (pc != null)
				{
					if (string.CompareOrdinal(name, "PropertyType") == 0 || string.CompareOrdinal(name, "Name") == 0)
					{
						TreeNodeCustomProperty tp = RootClassNode.GetCustomPropertyNode(pc);
						if (tp != null)
						{
							tp.ShowText();
						}
					}
				}
				else
				{
					EventClass ec = property as EventClass;
					if (ec != null)
					{
						if (string.CompareOrdinal(name, "EventHandlerType") == 0)
						{
							TreeNodeCustomEvent tp = RootClassNode.GetCustomEventNode(ec);
							if (tp != null)
							{
								tp.ShowText();
								tp.ResetNextLevel(this);
							}
						}
					}
				}
			}
			else
			{
				ICustomObject ico = property as ICustomObject;
				if (ico != null)
				{
					onObjPropertyChanged(ico);
				}
			}
		}
		public void OnDeleteEvent(EventClass eventObject)
		{
			if (eventObject.ClassId == this.RootId.ClassId)
			{
				this.RootClassNode.ClassData.RootClassID.ReloadEventActions(null);
				ResetNodes(eventObject.IsStatic, typeof(TreeNodeCustomEventCollection));
			}
			else
			{
				IList<TreeNodeClassComponentCust> list = RootClassNode.GetInstancesByClassId(eventObject.ClassId);
				if (list.Count > 0)
				{
					foreach (TreeNodeClassComponentCust tnc in list)
					{
						TreeNodePME pme = tnc.GetPointerMembersNode(eventObject.IsStatic);
						if (pme != null)
						{
							TreeNodeCustomEventPointerCollection es = pme.GetCustomEventPointersNode();
							if (es != null && es.NextLevelLoaded)
							{
								TreeNodeCustomEventPointer tep = es.GetEventPointerNode(eventObject.MemberId);
								if (tep != null)
								{
									tep.Remove();
								}
							}
						}
					}
				}
			}
		}
		public void OnAddEvent(EventClass eventObject)
		{
			if (RootClassNode != null)
			{
				if (eventObject.ClassId == this.RootId.ClassId)
				{
					this.RootClassNode.ClassData.RootClassID.ReloadEventActions(null);
					if (eventObject.Holder.IsStatic)
					{
						ResetNodes(eventObject.IsStatic, typeof(TreeNodeEventMixedCollection));
					}
					else
					{
						ResetNodes(eventObject.IsStatic, typeof(TreeNodeCustomEventCollection));
					}
				}
				else
				{
					IList<TreeNodeClassComponentCust> list = RootClassNode.GetInstancesByClassId(eventObject.ClassId);
					if (list.Count > 0)
					{
						foreach (TreeNodeClassComponentCust tnc in list)
						{
							TreeNodePME pme = tnc.GetPointerMembersNode(eventObject.IsStatic);
							if (pme != null)
							{
								TreeNodeCustomEventPointerCollection es = pme.GetCustomEventPointersNode();
								if (es != null && es.NextLevelLoaded)
								{
									TreeNodeCustomEventPointer tep = es.GetEventPointerNode(eventObject.MemberId);
									if (tep == null)
									{
										CustomEventPointer ep = new CustomEventPointer(eventObject, es.GetHolder());
										tep = new TreeNodeCustomEventPointer(es.IsStatic, ep);
										es.Nodes.Insert(0, tep);
									}
								}
							}
						}
					}
				}
			}
		}
		public void OnEventSelected(EventClass eventObject)
		{
			if (eventObject.ClassId == this.RootId.ClassId)
			{
				TreeNodeClassRoot rn = this.RootClassNode;
				TreeNodeCustomEvent tncp = rn.GetCustomEventNode(eventObject);
				if (tncp != null)
				{
					this.SelectedNode = tncp;
				}
			}
		}
		public void OnFireEventActionSelected(FireEventMethod method)
		{
			TreeNodeClassRoot rn = this.RootClassNode;
			TreeNodeCustomEvent tncp = rn.GetCustomEventNode(method.Event.Event);
			if (tncp != null)
			{
				for (int i = 0; i < tncp.Nodes.Count; i++)
				{
					TreeNodeAction tna = tncp.Nodes[i] as TreeNodeAction;
					if (tna != null)
					{
						IAction act = tna.Action;
						if (act != null)
						{
							FireEventMethod fe = act.ActionMethod as FireEventMethod;
							if (fe != null)
							{
								if (fe.EventId == method.EventId && fe.MemberId == method.MemberId)
								{
									this.SelectedNode = tna;
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnMethodSelected(MethodClass method)
		{
			if (method.ClassId == this.RootId.ClassId)
			{
				TreeNodeClassRoot rn = this.RootClassNode;
				TreeNodeCustomMethod cm = rn.GetCustomMethdNode(method);
				if (cm != null)
				{
					SelectedNode = cm;
				}
			}
		}
		public void OnMethodChanged(MethodClass method, bool isNewMethod)
		{
			TreeNodeClassRoot rn = this.RootClassNode;
			if (method.ClassId == this.RootId.ClassId)
			{
				if (method.Holder.IsStatic)
				{
					ResetNodes(method.IsStatic, typeof(TreeNodeActionMixedCollection));
					ResetNodes(method.IsStatic, typeof(TreeNodeMethodMixedCollection));
				}
				else
				{
					ResetNodes(method.IsStatic, typeof(TreeNodeActionCollection));
					ResetNodes(method.IsStatic, typeof(TreeNodeCustomMethodCollection));
				}
				MethodOverride po = method as MethodOverride;
				if (po != null)
				{
					TreeNodeOverridesInterfaces tnoi = rn.GetOverridesInterfacesNode();
					if (tnoi != null)
					{
						tnoi.OnMethodImplemented(po);
					}
				}
				EventHandlerMethod ehm = method as EventHandlerMethod;
				if (ehm != null)
				{
					List<TreeNodeEventMethod> list = new List<TreeNodeEventMethod>();
					SearchNodeByType<TreeNodeEventMethod>(list);
					foreach (TreeNodeEventMethod nd in list)
					{
						EventHandlerMethod ehm0 = nd.Method;
						if (ehm0.MemberId == method.MemberId)
						{
							nd.ResetImage();
						}
					}
				}
			}
			else
			{
				IList<TreeNodeClassComponentCust> list = rn.GetInstancesByClassId(method.ClassId);
				if (list.Count > 0)
				{
					foreach (TreeNodeClassComponentCust tnc in list)
					{
						TreeNodePME pme = tnc.GetPointerMembersNode(method.IsStatic);
						if (pme != null)
						{
							TreeNodeCustomMethodPointerCollection ms = pme.GetCustomMethodPointersNode();
							if (ms != null && ms.NextLevelLoaded)
							{
								TreeNodeCustomMethodPointer tmp = ms.GetMethodPointerNode(method.MemberId);
								if (tmp != null)
								{
									tmp.SetMethodClass(method);
								}
								else
								{
									if (isNewMethod)
									{
										IClass holder = (IClass)(ms.OwnerIdentity);
										CustomMethodPointer mp = new CustomMethodPointer(method, holder);
										TreeNodeCustomMethodPointer tna = new TreeNodeCustomMethodPointer(method.IsStatic, mp);
										ms.Nodes.Insert(0, tna);
									}
								}
							}
						}
					}
				}
			}
		}
		public void OnRemoveInterface(InterfacePointer interfacePointer)
		{
			TreeNodeClassRoot rn = this.RootClassNode;
			if (interfacePointer.ClassId == this.RootId.ClassId)
			{
				TreeNodeOverridesInterfaces tnoi = rn.GetOverridesInterfacesNode();
				if (tnoi != null)
				{
					tnoi.ResetNextLevel(this);
				}
			}
		}
		public void OnInterfaceAdded(InterfacePointer interfacePointer)
		{
			TreeNodeClassRoot rn = this.RootClassNode;
			if (interfacePointer.ImplementerClassId == this.RootId.ClassId)
			{
				TreeNodeOverridesInterfaces tnoi = rn.GetOverridesInterfacesNode();
				if (tnoi != null)
				{
					if (tnoi.NextLevelLoaded)
					{
						for (int j = 0; j < tnoi.Nodes.Count; j++)
						{
							TreeNodeInterfaces tnifs = tnoi.Nodes[j] as TreeNodeInterfaces;
							if (tnifs != null)
							{
								tnifs.ResetNextLevel(this);
								break;
							}
						}
					}
				}
			}
		}
		public void OnBaseInterfaceAdded(InterfaceClass owner, InterfacePointer baseInterface)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == owner.ClassId)
				{
					if (r.BaseInterfacesNode.NextLevelLoaded)
					{
						TreeNodeInterfacePointer tnm = new TreeNodeInterfacePointer(baseInterface);
						r.BaseInterfacesNode.Nodes.Add(tnm);
					}
				}
			}
		}
		public void OnInterfaceEventChanged(InterfaceElementEvent eventType)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == eventType.Interface.ClassId)
				{
					if (r.EventsNode.NextLevelLoaded)
					{
						for (int i = 0; i < r.EventsNode.Nodes.Count; i++)
						{
							TreeNodeInterfaceEvent tnm = r.EventsNode.Nodes[i] as TreeNodeInterfaceEvent;
							if (tnm != null)
							{
								if (tnm.Event.EventId == eventType.EventId)
								{
									tnm.Text = eventType.ToString();
									tnm.ResetNextLevel(this);
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnInterfaceEventDeleted(InterfaceElementEvent eventType)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == eventType.Interface.ClassId)
				{
					if (r.EventsNode.NextLevelLoaded)
					{
						for (int i = 0; i < r.EventsNode.Nodes.Count; i++)
						{
							TreeNodeInterfaceEvent tnm = r.EventsNode.Nodes[i] as TreeNodeInterfaceEvent;
							if (tnm != null)
							{
								if (tnm.Event.EventId == eventType.EventId)
								{
									tnm.Remove();
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnInterfaceEventAdded(InterfaceElementEvent eventType)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == eventType.Interface.ClassId)
				{
					if (r.EventsNode.NextLevelLoaded)
					{
						TreeNodeInterfaceEvent tnm = new TreeNodeInterfaceEvent(eventType, new InterfacePointer((ClassPointer)(r.OwnerPointer)));
						r.EventsNode.Nodes.Add(tnm);
					}
				}
			}
		}
		public void OnInterfacePropertyDeleted(InterfaceElementProperty property)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == property.Interface.ClassId)
				{
					if (r.PropertiesNode.NextLevelLoaded)
					{
						for (int i = 0; i < r.PropertiesNode.Nodes.Count; i++)
						{
							TreeNodeInterfaceProperty tnm = r.PropertiesNode.Nodes[i] as TreeNodeInterfaceProperty;
							if (tnm != null)
							{
								if (tnm.Property.PropertyId == property.PropertyId)
								{
									tnm.Remove();
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnInterfacePropertyChanged(InterfaceElementProperty property)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == property.Interface.ClassId)
				{
					if (r.PropertiesNode.NextLevelLoaded)
					{
						for (int i = 0; i < r.PropertiesNode.Nodes.Count; i++)
						{
							TreeNodeInterfaceProperty tnm = r.PropertiesNode.Nodes[i] as TreeNodeInterfaceProperty;
							if (tnm != null)
							{
								if (tnm.Property.PropertyId == property.PropertyId)
								{
									tnm.Text = property.ToString();
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnInterfacePropertyAdded(InterfaceElementProperty property)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == property.Interface.ClassId)
				{
					if (r.PropertiesNode.NextLevelLoaded)
					{
						TreeNodeInterfaceProperty tnm = new TreeNodeInterfaceProperty(property, new InterfacePointer((ClassPointer)(r.OwnerPointer)));
						r.PropertiesNode.Nodes.Add(tnm);
					}
				}
			}
		}
		public void OnInterfaceMethodDeleted(InterfaceElementMethod method)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == method.Interface.ClassId)
				{
					if (r.MethodsNode.NextLevelLoaded)
					{
						for (int i = 0; i < r.MethodsNode.Nodes.Count; i++)
						{
							TreeNodeInterfaceMethod tnm = r.MethodsNode.Nodes[i] as TreeNodeInterfaceMethod;
							if (tnm != null)
							{
								if (tnm.Method.MethodId == method.MethodId)
								{
									tnm.Remove();
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnInterfaceMethodChanged(InterfaceElementMethod method)
		{
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == method.Interface.ClassId)
				{
					if (r.MethodsNode.NextLevelLoaded)
					{
						for (int i = 0; i < r.MethodsNode.Nodes.Count; i++)
						{
							TreeNodeInterfaceMethod tnm = r.MethodsNode.Nodes[i] as TreeNodeInterfaceMethod;
							if (tnm != null)
							{
								if (tnm.Method.MethodId == method.MethodId)
								{
									tnm.Text = method.ToString();
									tnm.ResetNextLevel(this);
									break;
								}
							}
						}
					}
				}
			}
		}
		public void OnInterfaceMethodCreated(InterfaceElementMethod method)
		{
			//TBD: if this viewer is for this method then add new node
			TreeNodeClassInterface r = RootClassNode as TreeNodeClassInterface;
			if (r != null)
			{
				InterfaceClass ifc = r.Interface;
				if (ifc.ClassId == method.Interface.ClassId)
				{
					if (r.MethodsNode.NextLevelLoaded)
					{
						TreeNodeInterfaceMethod tnm = new TreeNodeInterfaceMethod(method, new InterfacePointer((ClassPointer)(r.OwnerPointer)));
						r.MethodsNode.Nodes.Add(tnm);
					}
				}
			}
			//TBD: if this viewer is not for this method then check if this root class implement this interface
		}
		public void OnActionChanged(UInt32 classId, IAction act, bool isNewAction)
		{
			if (act.ScopeMethodId != 0)
			{
				if (ScopeMethodId != act.ScopeMethodId)
				{
					return;
				}
			}
			TreeNodeActionCollection tnac = null;
			for (int i = 0; i < Nodes.Count; i++)
			{
				tnac = Nodes[i] as TreeNodeActionCollection;
				if (tnac != null)
				{
					ClassPointer cp0 = tnac.OwnerIdentity as ClassPointer;
					if (cp0 != null)
					{
						if (cp0.ClassId == classId)
						{
							tnac.ShowActionIcon();
							if (tnac.NextLevelLoaded)
							{
								if (isNewAction)
								{
									TreeNodeAction tna = new TreeNodeAction(act.IsStatic, act);
									tnac.Nodes.Add(tna);
								}
								else
								{
									for (int j = 0; j < tnac.Nodes.Count; j++)
									{
										TreeNodeAction tna = tnac.Nodes[j] as TreeNodeAction;
										if (tna != null)
										{
											if (tna.Action.ActionId == act.ActionId)
											{
												tna.ShowText();//.Text = act.ActionName;
												break;
											}
										}
									}
								}
							}
						}
					}
					break;
				}
			}
			TreeNodeCustomMethod tncm = null;
			MethodClass mc = null;
			IObjectPointer o;
			SetterPointer sp = act.ActionMethod as SetterPointer;
			if (sp != null)
			{
				o = sp.SetProperty;
			}
			else
			{
				o = act.MethodOwner;
			}
			Stack<IObjectPointer> owners = new Stack<IObjectPointer>();
			Stack<IObjectPointer> methodOwners = new Stack<IObjectPointer>();
			while (o != null)
			{
				owners.Push(o);
				mc = o as MethodClass;
				if (tncm == null)
				{
					if (mc != null)
					{
						for (int i = 0; i < this.Nodes.Count; i++)
						{
							TreeNodeCustomMethod nm = Nodes[i] as TreeNodeCustomMethod;
							if (nm != null)
							{
								if (mc.IsSameMethod(nm.Method))
								{
									tncm = nm;
									break;
								}
							}
						}
					}
					if (tncm == null)
					{
						methodOwners.Push(o);
					}
				}
				o = o.Owner;
			}
			TreeNodeAction a = null;
			if (tncm != null)
			{
				//the top owner is a MethodClass
				TreeNodeObject tn = tncm.LocateObjectNode(methodOwners);
				if (tn != null)
				{
					if (tn.NextLevelLoaded)
					{
						a = null;
						for (int i = 0; i < tn.Nodes.Count; i++)
						{
							TreeNodeAction tna = tn.Nodes[i] as TreeNodeAction;
							if (tna != null)
							{
								if (tna.Action.WholeActionId == act.WholeActionId)
								{
									a = tna;
									break;
								}
							}
						}
						if (a == null)
						{
							a = new TreeNodeAction(act.IsStatic, act);
							tn.Nodes.Add(a);
							tn.ShowActionIcon();
						}
						else
						{
							a.ShowText();
						}
					}
				}
			}
			if (owners.Count > 0)
			{
				o = owners.Pop();
			}
			else
			{
				o = null;
			}
			TreeNodeObject actOwnerNode = null;
			ClassPointer cp = o as ClassPointer;
			TypePointer tp = o as TypePointer;
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClassRoot r = this.Nodes[i] as TreeNodeClassRoot;
				if (r != null)
				{
					r.OnActionChanged(classId, act, isNewAction);
				}
				if (actOwnerNode == null)
				{
					if (cp != null)
					{
						if (r != null)
						{
							if (r.RootObjectId.ClassId == cp.ClassId)
							{
								if (owners.Count == 0)
								{
									actOwnerNode = r;
								}
								else
								{
									actOwnerNode = r.LocateObjectNode(owners);
								}
							}
						}
					}
					else
					{
						if (tp != null)
						{
							if (r != null && r.ClassData != null && r.ClassData.RootClassID != null)
							{
								if (r.ClassData.RootClassID.IsSameObjectRef(act.Class))
								{
									if (tp.ClassType.IsAssignableFrom(r.ClassData.RootClassID.BaseClassType))
									{
										if (owners.Count == 0)
										{
											actOwnerNode = r;
										}
										else
										{
											actOwnerNode = r.LocateObjectNode(owners);
										}
									}
								}
							}
						}
					}
				}
			}
			if (actOwnerNode != null)
			{
				if (owners.Count > 0)
				{
					o = owners.Peek();
					TreeNodeClass tnc = actOwnerNode as TreeNodeClass;
					if (tnc != null)
					{
						TreeNodePME tnpme = tnc.GetMemberNode(act.IsStatic);
						if (tnpme != null)
						{
							TreeNodeObjectCollection tnoc = tnpme.GetCollectionNode(o);
							if (tnoc != null)
							{
								tnoc.ShowActionIcon();
							}
						}
					}
				}
				TreeNodeActionCollection tac = actOwnerNode as TreeNodeActionCollection;
				if (tac == null)
				{
					TreeNodePME pme = null;
					for (int i = 0; i < actOwnerNode.Nodes.Count; i++)
					{
						if (act.IsStatic)
						{
							TreeNodePMEPointerStatic pme0 = actOwnerNode.Nodes[i] as TreeNodePMEPointerStatic;
							if (pme0 != null)
							{
								pme = pme0;
								break;
							}
						}
						else
						{
							TreeNodePMEPointer pme1 = actOwnerNode.Nodes[i] as TreeNodePMEPointer;
							if (pme1 != null)
							{
								pme = pme1;
								break;
							}
						}
					}
					if (pme != null)
					{
						pme.Expand();
						actOwnerNode = pme;
						if (act.ActionMethod != null)
						{
							TreeNodeObjectCollection tnoc = pme.GetCollectionNode(act.ActionMethod);
							if (tnoc != null)
							{
								tnoc.ShowActionIcon();
							}
						}
					}
					for (int i = 0; i < actOwnerNode.Nodes.Count; i++)
					{
						tac = actOwnerNode.Nodes[i] as TreeNodeActionCollection;
						if (tac != null)
						{
							actOwnerNode = tac;
							tac.ShowActionIcon();
							tac.Expand();
							break;
						}
					}
				}
				a = null;
				for (int i = 0; i < actOwnerNode.Nodes.Count; i++)
				{
					TreeNodeAction tna = actOwnerNode.Nodes[i] as TreeNodeAction;
					if (tna != null)
					{
						if (tna.Action.WholeActionId == act.WholeActionId)
						{
							a = tna;
							break;
						}
					}
				}
				if (a == null)
				{
					a = new TreeNodeAction(act.IsStatic, act);
					actOwnerNode.Nodes.Add(a);
					actOwnerNode.ShowActionIcon();
				}
				else
				{
					a.ShowText();
				}
			}
			if (act.IsStatic)
			{
				TreeNodeAction tna = null;
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassType tnc = Nodes[i] as TreeNodeClassType;
					if (tnc != null)
					{
						for (int j = 0; j < tnc.Nodes.Count; j++)
						{
							TreeNodeObjectCollection tnoc = tnc.Nodes[j] as TreeNodeObjectCollection;
							if (tnoc != null)
							{
								if (tnoc.NextLevelLoaded)
								{
									for (int k = 0; k < tnoc.Nodes.Count; k++)
									{
										TreeNodeObject tno = tnoc.Nodes[k] as TreeNodeObject;
										if (tno != null)
										{
											tna = tno.FindActionNode(act);
											if (tna != null)
											{
												tna.Text = act.ActionName;
												break;
											}
										}
									}
									if (tna != null)
									{
										break;
									}
								}
							}
						}
						if (tna != null)
						{
							break;
						}
					}
				}
			}
		}
		public void OnRemoveAllEventHandlers(IEvent e)
		{
			List<Type> lst = new List<Type>(new Type[] { typeof(TreeNodeEvent), typeof(TreeNodeCustomEvent), typeof(TreeNodeCustomEventPointer) });
			TreeNodeObject en = locateNode(e, lst, this.Nodes) as TreeNodeObject;
			if (en != null)
			{
				en.ResetNextLevel(this);
			}
		}
		public void OnRemoveEventHandler(EventAction ea, TaskID task)
		{
			List<Type> lst = new List<Type>(new Type[] { typeof(TreeNodeEvent), typeof(TreeNodeCustomEvent), typeof(TreeNodeCustomEventPointer) });
			TreeNodeObject en = locateNode(ea.Event, lst, this.Nodes) as TreeNodeObject;
			if (en != null)
			{
				TreeNodeClassRoot tr = en.TopLevelRootClassNode;
				IList<EventAction> ea0s = tr.GetEventHandlers(ea.Event);
				foreach (EventAction ea0 in ea0s)
				{
					ea0.RemoveAction(task.TaskId);
				}
				en.ResetNextLevel(this);
			}
		}
		/// <summary>
		/// case 1: the TreeNode for the event already exists
		///     find the TreeNode for the event, reset it and show action-icon.
		///     on each level leading to the event node, also show action-icon.
		/// case 2: the event node may not exist at this time if its parent has not expanded. 
		///     this can only happen when using context menu and therefore can only be directly under the holder.
		/// 
		/// </summary>
		/// <param name="ea"></param>
		public void OnAssignAction(EventAction ea)
		{
			IClass holder = ea.Event.Holder;
			if (holder == null)
			{
				return;
			}
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeClassRoot r = this.Nodes[i] as TreeNodeClassRoot;
				if (r != null)
				{
					if (r.ClassId == holder.ClassId)
					{
						for (int j = 0; j < r.Nodes.Count; j++)
						{
							TreeNodeOverridesInterfaces tnOI = r.Nodes[j] as TreeNodeOverridesInterfaces;
							if (tnOI != null)
							{
								//it resets the node
								tnOI.OnActionAssigned(ea);
								break;
							}
						}
						break;
					}
				}
			}

			List<Type> lst = new List<Type>(new Type[] { typeof(IClass) });
			TreeNodeObject en = locateNodeByOwnerType(holder, lst, this.Nodes);
			if (en != null)
			{
				lst = new List<Type>(new Type[] { typeof(TreeNodeEvent), typeof(TreeNodeCustomEvent), typeof(TreeNodeCustomEventPointer) });
				TreeNodeObject en2 = locateNode(ea.Event, lst, en.Nodes) as TreeNodeObject;
				if (en2 != null)
				{
					en2.ResetNextLevel(this);
					en2.ShowActionIcon();
				}
				else
				{
					TreeNodeClass cNode = en as TreeNodeClass;
					if (cNode != null)
					{
						TreeNodeObjectCollection nodeCollection;
						TreeNodePME pmeNode = cNode.GetMemberNode(ea.Event.IsStatic);
						if (pmeNode != null)
						{
							if (ea.Event.IsCustomEvent)
							{
								nodeCollection = pmeNode.GetCustomEventsNode();
							}
							else
							{
								nodeCollection = pmeNode.GetEventCollection();
							}
							if (nodeCollection != null)
							{
								nodeCollection.ShowActionIcon();
							}
						}
					}
				}
			}
		}
		public void OnActionListOrderChanged(object sender, EventAction ea)
		{
			if (sender != this)
			{
				TreeNodeEvent ne = (TreeNodeEvent)LocateNode(ea.Event);
				if (ne != null)
				{
					ne.ResetNextLevel(this);
				}
			}
		}
		public void OnResetDesigner(object obj)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassRoot tnc = Nodes[i] as TreeNodeClassRoot;
				if (tnc != null)
				{
					if (tnc.OwnerPointer.ObjectInstance == obj)
					{
						tnc.ResetNextLevel(this);
						break;
					}
					else
					{
						if (tnc.NextLevelLoaded)
						{
							for (int j = 0; j < tnc.Nodes.Count; j++)
							{
								TreeNodeClassComponent tncc = tnc.Nodes[j] as TreeNodeClassComponent;
								if (tncc != null)
								{
									if (tncc.OwnerPointer.ObjectInstance == obj)
									{
										tncc.ResetNextLevel(this);
										break;
									}
								}
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// compiling will change ClassPointer and object map. after compiling, they are restored
		/// </summary>
		public void OnResetMap()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassRoot tnc = Nodes[i] as TreeNodeClassRoot;
				if (tnc != null)
				{
					//reload event handlers
					tnc.ResetEventHandlers();
				}
			}
		}
		public void OnDatabaseListChanged()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeDatabaseConnectionList tn = Nodes[i] as TreeNodeDatabaseConnectionList;
				if (tn != null)
				{
					tn.ResetNextLevel(this);
					break;
				}
			}
		}
		public void OnDatabaseConnectionNameChanged(Guid connectionid, string newName)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeDatabaseConnectionList tn = Nodes[i] as TreeNodeDatabaseConnectionList;
				if (tn != null)
				{
					for (int j = 0; j < tn.Nodes.Count; j++)
					{
						TreeNodeDatabaseConnection tnd = tn.Nodes[j] as TreeNodeDatabaseConnection;
						if (tnd != null)
						{
							if (connectionid == tnd.Connect.ConnectionGuid)
							{
								tnd.Connect.Name = newName;
								tnd.Text = tnd.Connect.ToString();
								break;
							}
						}
					}
					break;
				}
			}
		}
		public void OnEventListChanged(ICustomEventMethodDescriptor owner, UInt32 objectId)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassRoot tnc = Nodes[i] as TreeNodeClassRoot;
				if (tnc != null)
				{
					for (int j = 0; j < tnc.Nodes.Count; j++)
					{
						TreeNodeClassComponent tncc = tnc.Nodes[j] as TreeNodeClassComponent;
						if (tncc != null)
						{
							MemberComponentId cid = tncc.OwnerPointer as MemberComponentId;
							if (cid.MemberId == objectId)
							{
								for (int k = 0; k < tncc.Nodes.Count; k++)
								{
									TreeNodePMEPointer pmeNode = tncc.Nodes[k] as TreeNodePMEPointer;
									if (pmeNode != null)
									{
										for (int l = 0; l < pmeNode.Nodes.Count; l++)
										{
											TreeNodeEventCollection ecNode = pmeNode.Nodes[l] as TreeNodeEventCollection;
											if (ecNode != null)
											{
												ecNode.ResetNextLevel(this);
												break;
											}
										}
										break;
									}
								}
								break;
							}
						}
					}
					break;
				}
			}
		}
		#endregion
		#region Multiple selection
		public ArrayList SelectedNodes
		{
			get
			{
				return m_coll;
			}
			set
			{
				removePaintFromNodes();
				m_coll.Clear();
				m_coll = value;
				paintSelectedNodes();
			}
		}
		protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
		{
			if (MultipleSelection)
			{
				bool bControl = (ModifierKeys == Keys.Control);
				bool bShift = (ModifierKeys == Keys.Shift);

				// selecting twice the node while pressing CTRL ?
				if (bControl && m_coll.Contains(e.Node))
				{

					// unselect it (let framework know we don't want selection this time)
					e.Cancel = true;

					// update nodes
					removePaintFromNodes();
					m_coll.Remove(e.Node);
					paintSelectedNodes();
				}
				else
				{
					if (!bShift) m_firstNode = e.Node; // store begin of shift sequence
				}
			}
			else
			{
				base.OnBeforeSelect(e);
			}
		}


		void onAfterSelectForMultipleSelection(TreeNode node)// TreeViewEventArgs e)
		{
			if (!MultipleSelection)
				return;
			bool bControl = (ModifierKeys == Keys.Control);
			bool bShift = (ModifierKeys == Keys.Shift);

			if (bControl)
			{
				if (!m_coll.Contains(node)) // new node ?
				{
					m_coll.Add(node);
				}
				else  // not new, remove it from the collection
				{
					removePaintFromNodes();
					m_coll.Remove(node);
				}
				paintSelectedNodes();
			}
			else
			{
				// SHIFT is pressed
				if (bShift)
				{
					Queue myQueue = new Queue();

					TreeNode uppernode = m_firstNode;
					TreeNode bottomnode = node;
					// case 1 : begin and end nodes are parent
					bool bParent = isParent(m_firstNode, node); // is m_firstNode parent (direct or not) of e.Node
					if (!bParent)
					{
						bParent = isParent(bottomnode, uppernode);
						if (bParent) // swap nodes
						{
							TreeNode t = uppernode;
							uppernode = bottomnode;
							bottomnode = t;
						}
					}
					if (bParent)
					{
						TreeNode n = bottomnode;
						while (n != uppernode.Parent)
						{
							if (!m_coll.Contains(n)) // new node ?
								myQueue.Enqueue(n);
							n = n.Parent;
						}
					}
					// case 2 : nor the begin nor the end node are descendant one another
					else
					{
						if ((uppernode.Parent == null && bottomnode.Parent == null) || (uppernode.Parent != null && uppernode.Parent.Nodes.Contains(bottomnode))) // are they siblings ?
						{
							int nIndexUpper = uppernode.Index;
							int nIndexBottom = bottomnode.Index;
							if (nIndexBottom < nIndexUpper) // reversed?
							{
								TreeNode t = uppernode;
								uppernode = bottomnode;
								bottomnode = t;
								nIndexUpper = uppernode.Index;
								nIndexBottom = bottomnode.Index;
							}

							TreeNode n = uppernode;
							while (nIndexUpper <= nIndexBottom)
							{
								if (!m_coll.Contains(n)) // new node ?
									myQueue.Enqueue(n);

								n = n.NextNode;

								nIndexUpper++;
							} // end while

						}
						else
						{
							if (!m_coll.Contains(uppernode)) myQueue.Enqueue(uppernode);
							if (!m_coll.Contains(bottomnode)) myQueue.Enqueue(bottomnode);
						}
					}

					m_coll.AddRange(myQueue);

					paintSelectedNodes();
					m_firstNode = node; // let us chain several SHIFTs if we like it
				} // end if m_bShift
				else
				{
					// in the case of a simple click, just add this item
					if (m_coll != null && m_coll.Count > 0)
					{
						removePaintFromNodes();
						m_coll.Clear();
					}
					m_coll.Add(node);
				}
			}
		}
		public bool MultipleSelection { get; set; }
		private bool isParent(TreeNode parentNode, TreeNode childNode)
		{
			if (parentNode == childNode)
				return true;

			TreeNode n = childNode;
			bool bFound = false;
			while (!bFound && n != null)
			{
				n = n.Parent;
				bFound = (n == parentNode);
			}
			return bFound;
		}

		private void paintSelectedNodes()
		{
			foreach (TreeNode n in m_coll)
			{
				n.BackColor = SystemColors.Highlight;
				n.ForeColor = SystemColors.HighlightText;
			}
		}

		private void removePaintFromNodes()
		{
			if (m_coll.Count == 0) return;

			TreeNode n0 = (TreeNode)m_coll[0];
			if (n0 != null && n0.TreeView != null)
			{
				Color back = n0.TreeView.BackColor;
				Color fore = n0.TreeView.ForeColor;

				foreach (TreeNode n in m_coll)
				{
					n.BackColor = back;
					n.ForeColor = fore;
				}
			}

		}
		#endregion
	}
	public abstract class TreeNodeLoader : TreeNode
	{
		private bool _isStatic;
		public TreeNodeLoader(bool forStatic)
		{
			_isStatic = forStatic;
		}
		public bool ForStatic
		{
			get
			{
				return _isStatic;
			}
		}
		public abstract void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode);
	}
	public abstract class TreeNodeExplorer : TreeNode
	{
		#region fields and constructors
		private bool _isForStatic;
		private bool _nextLevelLoaded;
		private bool _toolTipsLoaded;
		public TreeNodeExplorer()
		{
		}
		public TreeNodeExplorer(bool isForStatic)
		{
			_isForStatic = isForStatic;
		}
		#endregion
		#region public methods
		public Form FindForm()
		{
			if (this.TreeView != null)
			{
				return this.TreeView.FindForm();
			}
			return null;
		}
		public virtual void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(null);
		}
		public ICustomPropertyHolder GetCustomPropertyHolder()
		{
			TreeNode nd = this;
			ICustomPropertyHolder ph = nd as ICustomPropertyHolder;
			while (ph == null && nd != null)
			{
				nd = nd.Parent;
				ph = nd as ICustomPropertyHolder;
			}
			return ph;
		}
		public void LoadToolTips()
		{
			if (!_toolTipsLoaded)
			{
				_toolTipsLoaded = true;
				this.ToolTipText = Tooltips;
			}
		}
		public bool IsInScope(Type type)
		{
			if (this.TargetScope == null)
				return true;
			if (type == null)
				return false;
			return TargetScope.IsAssignableFrom(type);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="type">type of data pointed to</param>
		/// <returns></returns>
		public bool IsInScope(DataTypePointer type)
		{
			if (TargetScope == null)
				return true;
			if (type == null)
				return false;
			return TargetScope.IsAssignableFrom(type);
		}
		/// <summary>
		/// when SelectionTarget is Object/Method, if _includeEvent is not specified then no events will be listed; 
		/// if _includeEvent presents then only the events with their names matching _includeEvent will be listed.
		/// when SelectionTarget is ALL/Event, _includeEvent is ignored.
		/// </summary>
		public bool IncludeEevnt(string eventName)
		{
			EnumObjectSelectType t = SelectionTarget;
			if (t == EnumObjectSelectType.All || t == EnumObjectSelectType.Event || t == EnumObjectSelectType.Action)
				return true;
			string s = this.EventScope;
			if (string.IsNullOrEmpty(s))
				return false;
			return (string.CompareOrdinal(s, eventName) == 0);
		}
		public void LoadNextLevel()
		{
			if (!_nextLevelLoaded)
			{
				TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					tv.ForceLoadNextLevel(this);
				}
			}
		}
		public TreeNodeClassComponent GetObjectNodeByKey(string objectKey)
		{
			this.LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassComponent dc = Nodes[i] as TreeNodeClassComponent;
				if (dc != null)
				{
					if (dc.OwnerPointer.ObjectKey == objectKey)
					{
						return dc;
					}
					dc = dc.GetObjectNodeByKey(objectKey);
					if (dc != null)
					{
						return dc;
					}
				}
			}
			return null;
		}
		#endregion
		#region properties
		public UInt32 ScopeMethodId
		{
			get
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					MethodClass mc = tv.ScopeMethod as MethodClass;
					if (mc != null)
					{
						return mc.MemberId;
					}
				}
				return 0;
			}
		}
		public virtual bool IsStatic
		{
			get
			{
				return _isForStatic;
			}
		}
		public TreeNode TopLevelNode
		{
			get
			{
				if (this.Parent == null)
					return this;
				return ((TreeNodeExplorer)this.Parent).TopLevelNode;
			}
		}
		public virtual IRootNode RootClassNode
		{
			get
			{
				TreeNode node = this;
				IRootNode root = null;
				while (node != null)
				{
					root = node as IRootNode;
					if (root != null)
					{
						break;
					}
					node = node.Parent;
					TreeNodeExplorer tne = node as TreeNodeExplorer;
					if (tne != null)
					{
						return tne.RootClassNode;
					}
				}
				return root;
			}
		}
		public virtual TreeNodeClassRoot TopLevelRootClassNode
		{
			get
			{
				TreeNode node = this;
				TreeNodeClassRoot root = null;
				while (node != null)
				{
					if (node is TreeNodeClassRoot)
					{
						root = (TreeNodeClassRoot)node;
					}
					node = node.Parent;
					TreeNodeExplorer tne = node as TreeNodeExplorer;
					if (tne != null)
					{
						return tne.TopLevelRootClassNode;
					}
				}
				if (root == null)
				{
					if (this.TreeView != null)
					{
						for (int i = 0; i < this.TreeView.Nodes.Count; i++)
						{
							root = this.TreeView.Nodes[i] as TreeNodeClassRoot;
							if (root != null)
							{
								break;
							}
						}
					}
				}
				return root;
			}
		}
		public bool ToolTipsLoaded
		{
			get
			{
				return _toolTipsLoaded;
			}
		}
		public bool NextLevelLoaded
		{
			get
			{
				return _nextLevelLoaded;
			}
			set
			{
				_nextLevelLoaded = value;
			}
		}

		public EnumObjectSelectType SelectionTarget
		{
			get
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					return tv.SelectionType;
				}
				return EnumObjectSelectType.All;
			}
		}
		public DataTypePointer TargetScope
		{
			get
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					return tv.SelectionTypeScope;
				}
				return null;
			}
		}
		public string EventScope
		{
			get
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					return tv.SelectionEventScope;
				}
				return null;
			}
		}
		public virtual string Tooltips
		{
			get
			{
				return "";
			}
		}
		#endregion
	}
	public enum EnumWebTypes { Unknown, JavaScript, Php }
	/// <summary>
	/// for primary types
	/// </summary>
	public class TreeNodePrimaryCollection : TreeNodeObjectCollection
	{
		private EnumWebRunAt _runat = EnumWebRunAt.Inherit;
		private EnumWebTypes _webTypes = EnumWebTypes.Unknown;
		public TreeNodePrimaryCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, null, scopeMethodId)
		{
			if (tv.SelectionTypeAttribute != null)
			{
				if (typeof(PhpTypeAttribute).Equals(tv.SelectionTypeAttribute))
				{
					Text = "Php types";
					_webTypes = EnumWebTypes.Php;
				}
				else if (typeof(JsTypeAttribute).Equals(tv.SelectionTypeAttribute))
				{
					Text = "Web client types";
					_webTypes = EnumWebTypes.JavaScript;
				}
				else
				{
					Text = "Primary types";
				}
			}
			else
			{
				Text = "Primary types";
			}
			Nodes.Add(new CLoader(true));
		}
		public TreeNodePrimaryCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, EnumWebRunAt runAt)
			: this(tv, parentNode, false, 0)
		{
			_runat = runAt;
			if (_runat == EnumWebRunAt.Client)
			{
				Text = "Web Client types";
				_webTypes = EnumWebTypes.JavaScript;
			}
			else if (_runat == EnumWebRunAt.Server)
			{
				Text = "Php types";
				_webTypes = EnumWebTypes.Php;
			}
		}
		protected override bool CheckActions
		{
			get
			{
				return false;
			}
		}
		public EnumWebTypes WebTypes
		{
			get
			{
				return _webTypes;
			}
		}
		public EnumWebRunAt RunAt
		{
			get
			{
				return _runat;
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}
		protected override void ShowIconNoAction()
		{
			ImageIndex = TreeViewObjectExplorer.IMG_TypeCollection;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			ImageIndex = TreeViewObjectExplorer.IMG_A_TypeCollection;
			SelectedImageIndex = ImageIndex;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pointer"></param>
		/// <returns></returns>
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassType tn = Nodes[i] as TreeNodeClassType;
					if (tn != null)
					{
						if (tn.OwnerPointer.IsSameObjectRef(o))
						{
							if (ownerStack.Count == 0)
							{
								return tn;
							}
							else
							{
								return tn.LocateObjectNode(ownerStack);
							}
						}
					}
				}
			}
			return null;
		}
		class CLoader : TreeNodeLoader
		{
			private TreeViewObjectExplorer _tv;
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodePrimaryCollection tnp = (TreeNodePrimaryCollection)parentNode;
				bool isForPhp = false;
				bool isForJs = false;
				if (tnp.RunAt == EnumWebRunAt.Client)
				{
					isForJs = true;
				}
				else if (tnp.RunAt == EnumWebRunAt.Server)
				{
					isForPhp = true;
				}
				else
				{
					if (tv.SelectionTypeAttribute != null)
					{
						isForPhp = typeof(PhpTypeAttribute).Equals(tv.SelectionTypeAttribute);
						isForJs = typeof(JsTypeAttribute).Equals(tv.SelectionTypeAttribute);
					}
				}
				_tv = tv;
				CreateTypeNode(typeof(object), "Object", Resources._object, parentNode);
				if (parentNode.SelectionTarget != EnumObjectSelectType.BaseClass)
				{
					if (tv != null && tv.ForMethodReturn)
					{
						CreateTypeNode(typeof(void), "Void", Resources._void, parentNode);
					}
					CreateTypeNode(typeof(bool), "Boolean", Resources._bool, parentNode);
					if (isForPhp)
					{
						Dictionary<string, Type> types = WebPhpData.GetPhpTypes();
						foreach (KeyValuePair<string, Type> kv in types)
						{
							CreateTypeNode(kv.Value, kv.Key, TypeSelector.GetTypeImageByName(kv.Key), parentNode);
						}
					}
					else if (isForJs)
					{
						Dictionary<string, Type> types = WebClientData.GetJavascriptTypes();
						foreach (KeyValuePair<string, Type> kv in types)
						{
							CreateTypeNode(kv.Value, kv.Key, TypeSelector.GetTypeImageByName(kv.Key), parentNode);
						}
					}
					else
					{
						CreateTypeNode(typeof(char), "Single letter", Resources._char, parentNode);
						CreateTypeNode(typeof(string), "String (one or more letters)", Resources.abc, parentNode);
						CreateTypeNode(typeof(sbyte), "Integer(8-bit)", Resources._sbyte, parentNode);
						CreateTypeNode(typeof(short), "Integer(16-bit)", Resources._int, parentNode);
						CreateTypeNode(typeof(int), "Integer(32-bit)", Resources._int, parentNode);
						CreateTypeNode(typeof(long), "Integer(64-bit)", Resources._int, parentNode);
#if DOTNET40
						CreateTypeNode(typeof(BigInteger), "Integer(any size)", Resources._int, parentNode);
#endif
						CreateTypeNode(typeof(byte), "Byte(usigned 8-bit integer)", Resources._byte, parentNode);
						CreateTypeNode(typeof(ushort), "Usigned Integer(16-bit)", Resources._int, parentNode);
						CreateTypeNode(typeof(uint), "Usigned Integer(32-bit)", Resources._int, parentNode);
						CreateTypeNode(typeof(ulong), "Usigned Integer(64-bit)", Resources._int, parentNode);
						CreateTypeNode(typeof(float), "Single(7 digits decimal)", Resources._decimal, parentNode);
						CreateTypeNode(typeof(double), "Double(15 digits decimal)", Resources._decimal, parentNode);
						CreateTypeNode(typeof(decimal), "Money(28 digits decimal)", Resources._decimal, parentNode);
						CreateTypeNode(typeof(DateTime), "DateTime", Resources.date, parentNode);
						CreateTypeNode(typeof(TimeSpan), "TimeSpan", Resources.date, parentNode);
						CreateTypeNode(typeof(Color), "Color", Resources._3color, parentNode);
						CreateTypeNode(typeof(EventArgs), "Event arguments", Resources.eargv, parentNode);
						CreateTypeNode(new ArrayPointer(new TypePointer(typeof(object))), "Array", Resources._array.ToBitmap(), parentNode);
						CreateTypeNode(typeof(List<object>).GetGenericTypeDefinition(), "List<T>", Resources.list, parentNode);
						CreateTypeNode(new StringCollectionPointer(), "StringCollection", Resources.sc, parentNode);
						CreateTypeNode(typeof(Type), "Type", Resources._type, parentNode);
					}
				}
			}
			void CreateTypeNode(Type t, string title, Bitmap img, TreeNodeObject parentNode)
			{
				if (parentNode.TargetScope == null || parentNode.TargetScope.IsAssignableFrom(t))
				{
					TreeNodeClassType nt = new TreeNodeClassType(_tv, parentNode.IsStatic, new TypePointer(t), title, img, parentNode.ScopeMethodId);
					parentNode.Nodes.Add(nt);
				}
			}
			void CreateTypeNode(DataTypePointer t, string title, Bitmap img, TreeNodeObject parentNode)
			{
				if (parentNode.TargetScope == null || parentNode.TargetScope.IsAssignableFrom(t))
				{
					TreeNodeClassType nt = new TreeNodeClassType(_tv, parentNode.IsStatic, t, title, img, parentNode.ScopeMethodId);
					parentNode.Nodes.Add(nt);
				}
			}
		}
	}
	/// <summary>
	/// holding PropertyDescriptor
	/// </summary>
	public class TreeNodeProperty : TreeNodeObject
	{
		private bool _readOnly;
		public TreeNodeProperty(bool isForStatic, IPropertyEx objectPointer)
			: base(isForStatic, objectPointer)
		{
			if (objectPointer.Owner != null)
			{
				ICustomPropertyNames cpn = objectPointer.Owner.ObjectInstance as ICustomPropertyNames;
				if (cpn != null)
				{
					if (cpn.UseCustomName(objectPointer.Name))
					{
						Text = cpn.GetCustomPropertyName(objectPointer.Name);
					}
				}
			}
			if (VPLUtil.IsItemsHolder(objectPointer.ObjectType))
			{
				ImageIndex = TreeViewObjectExplorer.IMG_ARRAY;
			}
			else
			{
				bool b = objectPointer.IsReadOnly;
				if (!b)
				{
					PropertyPointer pp = objectPointer as PropertyPointer;
					if (pp != null)
					{
						b = pp.IsReadOnlyForProgramming;
					}
				}
				if (b)
				{
					if (isForStatic)
						ImageIndex = TreeViewObjectExplorer.IMG_S_PROPERTY_READONLY;
					else
						ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_READONLY;
					_readOnly = true;
				}
				else
				{
					if (isForStatic)
						ImageIndex = TreeViewObjectExplorer.IMG_S_PROPERTY;
					else
						ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY;
				}
			}
			SelectedImageIndex = ImageIndex;
			//
			//for a component pointer, the programming can be done on the component, no need on the pointer
			if (objectPointer.ObjectType.IsEnum)
			{
				this.Nodes.Add(new TreeNodeEnumValues(objectPointer.ObjectType));
			}
			else
			{
				this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
			}
			this.Nodes.Add(new ActionLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }

		/// <summary>
		/// PropertyPointer and FieldPointer
		/// </summary>
		[Browsable(false)]
		public IPropertyEx Property
		{
			get
			{
				return (IPropertyEx)OwnerPointer;
			}
		}
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			base.OnNodeSelection(loader);
		}
		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			SetterPointer sp = pointer as SetterPointer;
			if (sp != null)
			{
				LoadNextLevel();
				IPropertyEx pp = this.OwnerPointer as IPropertyEx;
				if (pp.IsSameObjectRef(pointer.IdentityOwner))
				{
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodePropertyCollection node = this.Nodes[i] as TreeNodePropertyCollection;
						if (node != null)
						{
							TreeNodeObject ret = node.FindObjectNode(sp.SetProperty);
							if (ret != null)
							{
								return ret;
							}
							break;
						}
					}
				}
			}
			MethodPointer mp = pointer as MethodPointer;
			if (mp != null)
			{
				if (mp.Owner.IsSameObjectRef(this.OwnerPointer))
				{
					LoadNextLevel();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeMethodCollection node = this.Nodes[i] as TreeNodeMethodCollection;
						if (node != null)
						{
							TreeNodeObject ret = node.FindObjectNode(mp);
							if (ret != null)
							{
								return ret;
							}
							break;
						}
					}
				}
			}
			return base.FindObjectNode(pointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loaders = new List<TreeNodeLoader>();
			if (!this.OwnerPointer.ObjectType.IsEnum)
			{
				loaders.Add(new TreeNodeMemberLoader(this.IsStatic));
			}
			loaders.Add(new ActionLoader(IsStatic));
			return loaders;
		}
		public override TreeNodeObject GetActionOwnerNode()
		{
			TreeNodeObjectCollection tnc = this.Parent as TreeNodeObjectCollection;
			if (tnc != null)
			{
				return tnc.Parent as TreeNodeObject;
			}
			return null;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return TreeNodeMemberLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					if (this.Property.IsReadOnly)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.Action)
				{
					if (this.Property.IsReadOnly)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.Method)
				{
					if (!this.Property.IsReadOnly) // .CanWrite)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					)
				{
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Method)
				{
					if (!_readOnly)
					{
						return true;
					}
				}
			}
			return false;
		}
		private void deleteItem(object sender, EventArgs e)
		{
			try
			{
				if (Property.Owner != null)
				{
					IItemsHolder th = Property.Owner.ObjectInstance as IItemsHolder;
					if (th != null)
					{
						TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
						if (tv != null)
						{
							TreeNodeClassRoot ownerNode = tv.DesignerRootNode;
							if (ownerNode != null)
							{
								ClassPointer rootClass = ownerNode.RootObjectId;
								th.RemoveItemByKey(Property.Name);
								IPropertyEx pp = Property.Owner as IPropertyEx;
								if (pp != null)
								{
									rootClass.NotifyChange(th.HolderOwner, pp.Name);
								}
							}
						}
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(this.FindForm(),err);
			}
		}
		private void createClientLoopAction(object sender, EventArgs e)
		{
			createLoopAction(false);
		}
		private void createServerLoopAction(object sender, EventArgs e)
		{
			createLoopAction(true);
		}
		private void createLoopAction(bool runAtServer)
		{
			try
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					TreeNodeClassRoot ownerNode = tv.DesignerRootNode;
					if (ownerNode != null)
					{
						ClassPointer rootClass = ownerNode.RootObjectId;
						Type ta = Property.ObjectType;
						DataTypePointer ti = new DataTypePointer(VPLUtil.GetElementType(ta));
						if (ti.IsGenericParameter)
						{
							IGenericTypePointer igp = null;
							IObjectPointer op = Property;
							while (op != null)
							{
								igp = op as IGenericTypePointer;
								if (igp != null)
									break;
								op = op.Owner;
							}
							if (igp != null)
							{
								DataTypePointer dp = igp.GetConcreteType(ti.BaseClassType);
								if (dp != null)
								{
									ti.SetConcreteType(dp);
								}
							}
						}
						string sk = Property.ObjectKey;
						if (string.IsNullOrEmpty(sk))
						{
							sk = Guid.NewGuid().GetHashCode().ToString("x").ToString(CultureInfo.InvariantCulture);
						}
						CollectionForEachMethodInfo mif = new CollectionForEachMethodInfo(ti, ta, sk);
						//
						SubMethodInfoPointerGlobal mp = new SubMethodInfoPointerGlobal();
						mp.Owner = new CollectionPointer(Property);
						mp.MemberName = mif.Name;
						ParameterInfo[] pifs = mif.GetParameters();
						Type[] ts = new Type[pifs.Length];
						for (int i = 0; i < ts.Length; i++)
						{
							ts[i] = pifs[i].ParameterType;
						}
						mp.ParameterTypes = ts;
						mp.SetMethodInfo(mif);
						mp.SetHolder(rootClass);
						//
						MethodActionForeach method;
						//
						ActionSubMethodGlobal act0 = new ActionSubMethodGlobal(rootClass);
						act0.ActionMethod = mp;
						act0.ActionId = (UInt32)Guid.NewGuid().GetHashCode();

						if (runAtServer)
						{
							method = new MethodActionForeachAtServer(rootClass, act0);
						}
						else
						{
							method = new MethodActionForeachAtClient(rootClass, act0);
						}
						method.MemberId = act0.ActionId;
						method.MethodName = rootClass.CreateNewActionName(string.Format(CultureInfo.InvariantCulture, "ForEachIn{0}", Property.Name));
						act0.ActionName = method.MethodName;
						act0.ActionHolder = method;
						//
						rootClass.SaveAction(act0, ownerNode.ViewersHolder.Loader.Writer);
						//
						if (method.Edit(0, tv.Parent.RectangleToScreen(this.Bounds), ownerNode.ViewersHolder.Loader, tv.FindForm()))
						{
							ownerNode.ViewersHolder.Loader.DesignPane.OnActionChanged(rootClass.ClassId, method, true);
							ownerNode.ViewersHolder.Loader.DesignPane.OnNotifyChanges();
							//
							this.Expand();
							TreeNodeAction na = null;
							for (int i = 0; i < this.Nodes.Count; i++)
							{
								TreeNodeAction na0 = this.Nodes[i] as TreeNodeAction;
								if (na0 != null)
								{
									if (na0.Action.WholeActionId == method.WholeActionId)
									{
										this.ShowActionIcon();
										tv.SelectedNode = na0;
										na0.EnsureVisible();
										na = na0;
										break;
									}
								}
							}
							if (na == null)
							{
								na = new TreeNodeAction(method.IsStatic, method);
								this.Nodes.Add(na);
								this.ShowActionIcon();
								tv.SelectedNode = na;
								na.EnsureVisible();
							}
						}
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(this.FindForm(),err);
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly && !_readOnly)
			{
				if (this.SelectionTarget != EnumObjectSelectType.Method && MainClassPointer != null)
				{
					List<MenuItem> ms = new List<MenuItem>();
					if (!Property.IsReadOnly)
					{
						ms.Add(new MenuItemWithBitmap("Create SetProperty action", OnCreateNewAction, Resources._propAction.ToBitmap()));
					}
					if (VPLUtil.IsItemsHolder(Property.ObjectType))
					{
						bool topScope = true;
						TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
						if (tv != null && tv.ScopeMethod != null)
						{
							topScope = false;
						}
						if (topScope)
						{
							if (Property.RootPointer.IsWebObject)
							{
								PropertyPointer pp = Property as PropertyPointer;
								if (pp != null)
								{
									if (pp.IsWebClientValue())
									{
										ms.Add(new MenuItemWithBitmap("Act on each item at client", createClientLoopAction, Resources.handlerMethod));
									}
									if (pp.IsWebServerValue())
									{
										ms.Add(new MenuItemWithBitmap("Act on each item at server", createServerLoopAction, Resources._webServerHandler.ToBitmap()));
									}
								}
								else
								{
									if (Property.RunAt == EnumWebRunAt.Client)
									{
										ms.Add(new MenuItemWithBitmap("Act on each item at client", createClientLoopAction, Resources.handlerMethod));
									}
									else if (Property.RunAt == EnumWebRunAt.Server)
									{
										ms.Add(new MenuItemWithBitmap("Act on each item at server", createServerLoopAction, Resources._webServerHandler.ToBitmap()));
									}
									else if (Property.RunAt == EnumWebRunAt.Inherit)
									{
										ms.Add(new MenuItemWithBitmap("Act on each item at client", createClientLoopAction, Resources.handlerMethod));
									}
								}
							}
							else
							{
								ms.Add(new MenuItemWithBitmap("Act on each item", createServerLoopAction, Resources._array.ToBitmap()));
							}
						}
					}

					if (Property.Owner != null)// && typeof(SessionVariableCollection).Equals(Property.Owner.ObjectType))
					{
						IItemsHolder th = Property.Owner.ObjectInstance as IItemsHolder;
						if (th != null)
						{
							if (ms.Count > 0)
							{
								ms.Add(new MenuItem("-"));
							}
							ms.Add(new MenuItemWithBitmap("Delete", deleteItem, Resources._cancel.ToBitmap()));
						}
					}
					MenuItem[] mi = new MenuItem[ms.Count];
					ms.CopyTo(mi);
					return mi;
				}
			}
			return null;
		}
		public override IAction CreateNewAction()
		{
			IPropertyEx pp = (IPropertyEx)this.OwnerPointer;
			ActionClass act = new ActionClass(this.MainClassPointer);
			act.ActionMethod = pp.CreateSetterMethodPointer(act);
			act.ActionName = act.ActionMethod.DefaultActionName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return act;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.ImageIndex != TreeViewObjectExplorer.IMG_PROPERTY_WITHACTS)
			{
				this.ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_WITHACTS;
				SelectedImageIndex = this.ImageIndex;
			}
		}
		public override string Tooltips
		{
			get
			{
				PropertyPointer pp = this.OwnerPointer as PropertyPointer;
				if (pp != null)
				{
					return PMEXmlParser.GetPropertyDescription(VPLUtil.GetObjectType(pp.Owner.ObjectType), pp.PropertyInformation);
				}
				return string.Empty;
			}
		}
	}
	public class TreeNodeField : TreeNodeObject
	{
		public TreeNodeField(bool isForStatic, IObjectPointer objectPointer)
			: base(isForStatic, objectPointer)
		{
			FieldPointer pp = objectPointer as FieldPointer;
			FieldInfo pif = pp.Info;
			if (pif.IsInitOnly)
			{
				ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY_READONLY;
			}
			else
			{
				ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY;
			}
			SelectedImageIndex = ImageIndex;
			this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Field; }
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return TreeNodeMemberLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> ls = new List<TreeNodeLoader>();
			ls.Add(new TreeNodeMemberLoader(this.IsStatic));
			return ls;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					);
			}
			return false;
		}
	}
	public class TreeNodePropertyCollection : TreeNodeObjectCollection
	{
		public TreeNodePropertyCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			if (isForStatic)
				Text = "Static properties";
			else
				Text = "Properties inherited";

			Nodes.Add(new PropertyLoader(isForStatic));
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return PropertyLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			object v = VPLUtil.StaticOwnerForObject(OwnerPointer.ObjectInstance);
			IComponent cv = v as IComponent;
			IComponent co = OwnerPointer.ObjectInstance as IComponent;
			if (co != null && co.Site != null)
			{
				cv.Site = co.Site;
			}
			loader.NotifySelection(v);
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_PROPERTIES;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_PROPERTIES;
			SelectedImageIndex = ImageIndex;
		}
		public TreeNodeProperty GetCustomPropertyNode(UInt32 classId, UInt32 memberId)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeProperty tp = Nodes[i] as TreeNodeProperty;
				if (tp != null)
				{
					PropertyPointer pp = tp.OwnerPointer as PropertyPointer;
					if (tp != null)
					{
						PropertyClassDescriptor pcd = pp.Info as PropertyClassDescriptor;
						if (pcd != null)
						{
							if (pcd.ClassId == classId && pcd.MemberId == memberId)
							{
								return tp;
							}
						}
					}
				}
			}
			return null;
		}
		public TreeNodeProperty GetPropertyNodeByName(string name)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeProperty tp = Nodes[i] as TreeNodeProperty;
				if (tp != null)
				{
					PropertyPointer pp = tp.OwnerPointer as PropertyPointer;
					if (tp != null)
					{
						if (pp.Name == name)
						{
							return tp;
						}
					}
				}
			}
			return null;
		}
		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			SetterPointer sp = pointer as SetterPointer;
			if (sp != null)
			{
				LoadNextLevel();
				//PropertyPointer pp = sp.PropertyToSet as PropertyPointer;
				for (int i = 0; i < this.Nodes.Count; i++)
				{
					TreeNodeObject ret = this.Nodes[i] as TreeNodeObject;
					if (ret != null)
					{
						if (sp.SetProperty.IsSameObjectRef(ret.OwnerPointer))
						{
							return ret;
						}
					}
				}
			}
			TreeNodeObject node = base.FindObjectNode(pointer);
			if (node != null)
			{
				return node;
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new PropertyLoader(this.IsStatic));
			return lst;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_PROPERTIES_WITHACTIONS;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool IsPointerNode { get { return false; } }


	}
	/// <summary>
	/// load library properties
	/// </summary>
	class PropertyLoader : TreeNodeLoader
	{
		public PropertyLoader(bool forStatic)
			: base(forStatic)
		{
		}
		public static TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack, TreeNodeObject parentNode)
		{
			if (parentNode.NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				for (int i = 0; i < parentNode.Nodes.Count; i++)
				{
					TreeNodeProperty pn = parentNode.Nodes[i] as TreeNodeProperty;
					if (pn != null)
					{
						if (o.IsSameObjectRef(pn.OwnerPointer))
						{
							if (ownerStack.Count == 0)
							{
								return pn;
							}
							else
							{
								return pn.LocateObjectNode(ownerStack);
							}
						}
					}
				}
			}
			return null;
		}
		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			bool isWebPrj = false;
			bool forPhp = false;
			if (tv.Project != null)
			{
				forPhp = (tv.Project.ProjectType == EnumProjectType.WebAppPhp);
				if ((tv.Project.ProjectType == EnumProjectType.WebAppPhp || tv.Project.ProjectType == EnumProjectType.WebAppAspx))
				{
					isWebPrj = true;
				}
			}
			UInt32 scopeId = parentNode.ScopeMethodId;
			IObjectPointer objRef = parentNode.OwnerPointer;
			TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
			if (topClass == null)
			{
				topClass = tv.RootClassNode;
			}
			Dictionary<UInt32, IAction> actions = null;
			bool isWebPage = false;
			if (topClass != null)
			{
				isWebPage = topClass.RootObjectId.IsWebPage;
				if (!topClass.StaticScope)
				{
					actions = topClass.GetActions();
				}
			}
			else
			{
				if (tv != null)
				{
					if (tv.RootClassNode != null)
					{
						actions = tv.RootClassNode.GetActions();
					}
				}
				if (actions == null)
				{
					TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
					if (rootType != null)
					{
						actions = rootType.GetActions();
					}
				}
			}
			SortedList<string, TreeNode> newNodes = new SortedList<string, TreeNode>();
			EnumReflectionMemberInfoSelectScope scope;
			if (this.ForStatic)
				scope = EnumReflectionMemberInfoSelectScope.StaticOnly;
			else
				scope = EnumReflectionMemberInfoSelectScope.InstanceOnly;
			PropertyDescriptorCollection pifs = null;
			if (isWebPrj)
			{
				PropertyPointer pp = parentNode.OwnerPointer as PropertyPointer;
				if (pp != null && pp.RunAt == EnumWebRunAt.Client)
				{
					IExtendedPropertyOwner epo = objRef.Owner.ObjectInstance as IExtendedPropertyOwner;
					if (epo != null)
					{
						Type t = epo.PropertyCodeType(pp.MemberName);
						if (t != null)
						{
							if (t.Equals(typeof(DateTime)))
							{
								pifs = VPLUtil.GetProperties(typeof(JsDateTime), scope, false, (tv.SelectionType == EnumObjectSelectType.All), true);
							}
							else
							{
								pifs = VPLUtil.GetProperties(t, scope, false, (tv.SelectionType == EnumObjectSelectType.All), true);
							}
						}
					}
				}
			}
			if (pifs == null)
			{
				if (isWebPage && MethodInfoWebClient.IsWebObject(objRef.ObjectInstance))
				{
					pifs = MethodInfoWebClient.GetObjectProperties(scope, objRef.ObjectInstance, (tv.SelectionType == EnumObjectSelectType.All));
				}
				else
				{
					ClassInstancePointer cip = objRef.ObjectInstance as ClassInstancePointer;
					if (cip != null && typeof(WebPage).IsAssignableFrom(cip.ObjectType))
					{
						PropertyDescriptorCollection ps = VPLUtil.GetProperties(cip.ObjectType, scope, false, (tv.SelectionType == EnumObjectSelectType.All), true);
						List<PropertyDescriptor> l = new List<PropertyDescriptor>();
						foreach (PropertyDescriptor p in ps)
						{
							bool include = false;
							if (WebClientMemberAttribute.IsClientProperty(p))
							{
								include = true;
							}
							if (!include)
							{
								if (WebServerMemberAttribute.IsServerProperty(p))
								{
									include = true;
								}
							}
							if (include)
							{
								l.Add(p);
							}
						}
						pifs = new PropertyDescriptorCollection(l.ToArray());
					}
					else if (objRef.ObjectInstance == null)
					{
						ClassPointer cp = objRef as ClassPointer;
						if (cp != null)
						{
							if (typeof(WebPage).IsAssignableFrom(cp.BaseClassType))
							{
								pifs = new PropertyDescriptorCollection(new PropertyDescriptor[] { });
							}
						}
					}
					if (pifs == null)
					{
						bool browsableOnly = true;
						ClassInstancePointer cr0 = objRef.ObjectInstance as ClassInstancePointer;
						if (cr0 != null)
						{
							pifs = VPLUtil.GetProperties(cr0.ObjectType, scope, false, browsableOnly, true);
						}
						else if (objRef.ObjectInstance == null || objRef is TypePointer)
						{
							CustomMethodParameterPointer cmp = objRef as CustomMethodParameterPointer;
							if (cmp != null)
							{
								if (cmp.Parameter != null && cmp.Parameter.ParameterLibType != null)
								{
									TypeX tx = cmp.Parameter.ParameterLibType as TypeX;
									if(tx != null)
									{
										ClassPointer cp = tx.Owner;
										if (cp == null)
										{
											if (tx.ClassId != 0)
											{
												if (tv.Project != null)
												{
													cp = ClassPointer.CreateClassPointer(tx.ClassId, tv.Project);
												}
											}
										}
										if (cp != null)
										{
											XClass<InterfaceClass> xi = cp.ObjectInstance as XClass<InterfaceClass>;
											if (xi != null)
											{
												InterfaceClass ifc = xi.ObjectValue as InterfaceClass;
												if (ifc != null)
												{
													if (ifc.Properties != null)
													{
														newNodes = new SortedList<string, TreeNode>();
														foreach (InterfaceElementProperty iep in ifc.Properties)
														{
															InterfaceElementProperty iep0 = iep.Clone() as InterfaceElementProperty;
															iep0.SetVariablePointer(cmp);
															newNodes.Add(iep.Name,new TreeNodeProperty(iep.IsStatic,iep0));
														}
														parentNode.AddSortedNodes(newNodes);
													}
													return;
												}
											}
										}
									}
								}
							}
							if (pifs == null)
							{
								Type t = DesignUtil.GetTypeParameterValue(objRef);
								if (t != null)
								{
									pifs = VPLUtil.GetProperties(t, scope, false, browsableOnly, true);
								}
							}
							if (pifs == null)
							{
								pifs = VPLUtil.GetProperties(objRef.ObjectType, scope, false, browsableOnly, true);
							}
						}
						else
						{
							IDrawDesignControl dc = objRef.ObjectInstance as IDrawDesignControl;
							if (dc != null)
							{
								pifs = VPLUtil.GetProperties(dc.Item, scope, false, browsableOnly, true);
							}
							else
							{
								pifs = VPLUtil.GetProperties(objRef.ObjectInstance, scope, false, browsableOnly, true);
							}
						}
					}
				}
			}
			if (pifs != null && pifs.Count > 0)
			{
				List<PropertyDescriptor> mifs2 = new List<PropertyDescriptor>();
				for (int k = 0; k < pifs.Count; k++)
				{
					if (NotForProgrammingAttribute.IsNotForProgramming(pifs[k]))
					{
						continue;
					}
					mifs2.Add(pifs[k]);
				}
				if (mifs2.Count < pifs.Count)
				{
					pifs = new PropertyDescriptorCollection(mifs2.ToArray());
				}
				if (!(objRef.ObjectInstance is WebPage) && (forPhp || typeof(LimnorWebApp).IsAssignableFrom(objRef.ObjectType)))
				{
					if (DesignUtil.IsWebServerObject(objRef))
					{
						mifs2 = new List<PropertyDescriptor>();
						for (int k = 0; k < pifs.Count; k++)
						{
							if (VPLUtil.FindAttributeByType(pifs[k].Attributes, typeof(WebClientMemberAttribute)))
							{
								mifs2.Add(pifs[k]);
							}
							else
							{
								if (VPLUtil.FindAttributeByType(pifs[k].Attributes, typeof(WebServerMemberAttribute)))
								{
									mifs2.Add(pifs[k]);
								}
							}
						}
						pifs = new PropertyDescriptorCollection(mifs2.ToArray());
					}
				}
				for (int i = 0; i < pifs.Count; i++)
				{
					if (NotForProgrammingAttribute.IsNotForProgramming(pifs[i]))
					{
						continue;
					}
					if (!isWebPrj)
					{
						if (WebClientOnlyAttribute.IsWebClientOnly(pifs[i]))
						{
							continue;
						}
					}
					if (objRef.RunAt == EnumWebRunAt.Client)
					{
						if (typeof(IWebClientControl).Equals(objRef.ObjectType))
						{
							if (string.CompareOrdinal(pifs[i].Name, "ElementName") == 0)
							{
								continue;
							}
							if (string.CompareOrdinal(pifs[i].Name, "CodeName") == 0)
							{
								continue;
							}
						}
					}
					TreeNodeProperty nodeProperty;
					PropertyPointer pp;
					pp = new PropertyPointer();
					if (ForStatic)
					{
						pp.Owner = new TypePointer(objRef.ObjectType);
					}
					else
					{
						pp.Owner = objRef;
					}
					pp.SetPropertyInfo(pifs[i]);
					if (!newNodes.ContainsKey(pp.Name))
					{
						nodeProperty = new TreeNodeProperty(ForStatic, pp);
						try
						{

							newNodes.Add(pp.Name, nodeProperty);
						}
						catch (Exception err)
						{
							MathNode.Log(tv == null ? TraceLogClass.MainForm : tv.FindForm(), err);
						}

						//load actions
						bool bHasActions = false;
						if (string.CompareOrdinal(pp.Name, "Cursor") == 0)
						{
							bHasActions = false;
						}
						if (actions != null)
						{
							foreach (IAction a in actions.Values)
							{
								if (a != null && a.IsStatic == parentNode.IsStatic)
								{
									if (nodeProperty.IncludeAction(a, tv, scopeId, false))
									{
										bHasActions = true;
										break;
									}
								}
							}
							if (bHasActions)
							{
								nodeProperty.OnShowActionIcon();
							}
						}
					}
				}
			}
			if (objRef.ObjectType != null)
			{
				FieldInfo[] fifs;
				Type t = VPLUtil.GetObjectType(objRef.ObjectType);
				if(scope == EnumReflectionMemberInfoSelectScope.StaticOnly)
					fifs = t.GetFields( BindingFlags.Public|BindingFlags.Static);
				else
					fifs = t.GetFields();
				if (fifs != null && fifs.Length > 0)
				{
					TreeNodeProperty nodeProperty;
					for (int i = 0; i < fifs.Length; i++)
					{
						FieldPointer fp = new FieldPointer(fifs[i], objRef);
						if (!newNodes.ContainsKey(fifs[i].Name))
						{
							nodeProperty = new TreeNodeProperty(ForStatic, fp);
							try
							{
								newNodes.Add(fifs[i].Name, nodeProperty);
							}
							catch (Exception err)
							{
								MathNode.Log(tv == null ? TraceLogClass.MainForm : tv.FindForm(), err);
							}
							//load actions
							bool bHasActions = false;
							if (actions != null)
							{
								foreach (IAction a in actions.Values)
								{
									if (a != null && a.IsStatic == parentNode.IsStatic)
									{
										if (nodeProperty.IncludeAction(a, tv, scopeId, false))
										{
											bHasActions = true;
											break;
										}
									}
								}
								if (bHasActions)
								{
									nodeProperty.OnShowActionIcon();
								}
							}
						}
					}
				}
			}
			parentNode.AddSortedNodes(newNodes);
		}
	}
	public class TreeNodeFieldCollection : TreeNodeObjectCollection
	{
		public TreeNodeFieldCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			if (isForStatic)
				Text = "Static fields";
			else
				Text = "Fields inherited";
			Nodes.Add(new CLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Field; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_OBJECTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_OBJECTS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			ImageIndex = TreeViewObjectExplorer.IMG_OBJECTS;
			SelectedImageIndex = ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader(this.IsStatic));
			return lst;
		}
		public override bool IsPointerNode { get { return false; } }
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeField tn = Nodes[i] as TreeNodeField;
					if (tn != null)
					{
						if (o.IsSameObjectRef(tn.OwnerPointer))
						{
							if (ownerStack.Count == 0)
							{
								return tn;
							}
							else
							{
								return tn.LocateObjectNode(ownerStack);
							}
						}
					}
				}
			}
			return null;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				IObjectPointer objRef = parentNode.OwnerPointer;
				SortedList<string, TreeNode> newNodes = new SortedList<string, TreeNode>();
				EnumReflectionMemberInfoSelectScope scope;
				if (ForStatic)
					scope = EnumReflectionMemberInfoSelectScope.StaticOnly;
				else
					scope = EnumReflectionMemberInfoSelectScope.InstanceOnly;
				FieldInfo[] pifs=null;
				Type t = DesignUtil.GetTypeParameterValue(objRef);
				if (t != null)
				{
					pifs = VPLUtil.GetFields(t, scope, false, true);
				}
				if (pifs == null)
				{
					if (objRef.ObjectInstance == null)
					{
						pifs = VPLUtil.GetFields(objRef.ObjectType, scope, false, true);
					}
					else
					{
						pifs = VPLUtil.GetFields(objRef.ObjectInstance, scope, false, true);
					}
				}
				for (int i = 0; i < pifs.Length; i++)
				{
					if (!pifs[i].IsSpecialName)
					{
						if (parentNode.IsInScope(pifs[i].FieldType))
						{
							FieldPointer pp = new FieldPointer();
							pp.Owner = objRef;
							pp.SetFieldInfo(pifs[i], parentNode.IsStatic);
							newNodes.Add(pp.MemberName, new TreeNodeField(parentNode.IsStatic, pp));
						}
					}
				}
				parentNode.AddSortedNodes(newNodes);
			}
		}
	}
	public class TreeNodeConstant : TreeNodeExplorer
	{
		object _value;
		public TreeNodeConstant(bool isForStatic, object value)
			: base(isForStatic)
		{
			_value = value;
			if (_value == null)
				Text = "{null}";
			else
				Text = _value.ToString();
		}
		public object Value
		{
			get
			{
				return _value;
			}
		}
	}
	public class TreeNodeEnumValue : TreeNodeConstant
	{
		public TreeNodeEnumValue(bool isForStatic, object v)
			: base(isForStatic, v)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_enumValue;
			SelectedImageIndex = ImageIndex;
		}
	}
	public class TreeNodeEnumValues : TreeNodeObject, ITypeNode
	{
		Type _enumType;
		public TreeNodeEnumValues(bool isForStatic, Type enumType)
			: base(isForStatic, new TypePointer(enumType))
		{
			_enumType = enumType;
			Text = enumType.Name;
			this.ImageIndex = TreeViewObjectExplorer.IMG_enumValues;
			this.SelectedImageIndex = ImageIndex;
			Nodes.Add(new CLoader());

		}
		public TreeNodeEnumValues(Type enumType)
			: base(new TypePointer(enumType))
		{
			_enumType = enumType;
			Text = enumType.Name;
			this.ImageIndex = TreeViewObjectExplorer.IMG_enumValues;
			this.SelectedImageIndex = ImageIndex;
			Nodes.Add(new CLoader());
		}
		public override object Clone()
		{
			return new TreeNodeEnumValues(this.IsStatic, _enumType);
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectionType == EnumObjectSelectType.Type || tv.SelectionType == EnumObjectSelectType.InstanceType)
				{
					return true;
				}
			}
			return false;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			return null;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader()
				: base(true)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeEnumValues ne = parentNode as TreeNodeEnumValues;
				Array a = Enum.GetValues(ne.OwnerDataType);
				if (a != null && a.Length > 0)
				{
					for (int i = 0; i < a.Length; i++)
					{
						parentNode.Nodes.Add(new TreeNodeEnumValue(true, a.GetValue(i)));
					}
				}
			}
		}
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Library; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loaders = new List<TreeNodeLoader>();
			loaders.Add(new CLoader());
			return loaders;
		}

		#region ITypeNode Members

		public Type OwnerDataType
		{
			get { return _enumType; }
		}
		public void HandleActionModified(UInt32 classId, IAction act, bool isNewAction)
		{
		}
		#endregion
	}
	public class TreeNodeCustomMethodParameter : TreeNodeObject
	{
		private string _tooltips;
		private ParameterClass _param;
		public TreeNodeCustomMethodParameter(bool isForStatic, ParameterClass pointer)
			: base(isForStatic, pointer)
		{
			_param = pointer;
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			Text = pointer.ExpressionDisplay;
			this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
		}
		public TreeNodeCustomMethodParameter(bool isForStatic, CustomMethodParameterPointer pointer)
			: base(isForStatic, pointer)
		{
			_param = pointer.Parameter;
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			Text = pointer.ExpressionDisplay;
			this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
		}
		public TreeNodeCustomMethodParameter(bool isForStatic, ActionBranchParameterPointer pointer)
			: base(isForStatic, pointer)
		{
			_param = pointer.Parameter;
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			Text = pointer.ExpressionDisplay;
			this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new TreeNodeMemberLoader(this.IsStatic));
			return l;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return TreeNodeMemberLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return false;
		}
		public ParameterClass Parameter
		{
			get
			{
				return _param;
			}
		}
		public override string Tooltips
		{
			get
			{
				return _tooltips;
			}
		}
		public void SetToolTips(string v)
		{
			_tooltips = v;
			ToolTipText = v;
			if (string.IsNullOrEmpty(Parameter.Description))
			{
				Parameter.Description = v;
			}
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					)
				{
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Type)
				{
					return typeof(Type).Equals(this.Parameter.BaseClassType);
				}
			}
			return false;
		}
	}
	public class TreeNodeMethodParameter : TreeNodeObject
	{
		private string _tooltips;
		public TreeNodeMethodParameter(bool isForStatic, MethodParamPointer pointer)
			: base(isForStatic, pointer)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			//do not load next level because they should be loaded within a method editor through parameter object
			//loading them for browsing/checking

			if (pointer.ObjectType.IsEnum)
			{
				this.Nodes.Add(new TreeNodeEnumValues(pointer.ObjectType));
			}
			else
			{
				this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new TreeNodeMemberLoader(this.IsStatic));
			return l;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return TreeNodeMemberLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return false;
		}
		public override string Tooltips
		{
			get
			{
				return _tooltips;
			}
		}
		public void SetToolTips(string v)
		{
			_tooltips = v;
			ToolTipText = v;
		}
	}
	public class TreeNodeMethod : TreeNodeObject
	{
		private Dictionary<string, string> _parameterToolTips;
		public TreeNodeMethod(bool isForStatic, MethodInfoPointer objectPointer)
			: base(isForStatic, objectPointer)
		{
			if (objectPointer.IsWebServerMethod)
			{
				ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEB;
			}
			else
			{
				if (isForStatic)
					ImageIndex = TreeViewObjectExplorer.IMG_S_METHOD;
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_METHOD;
				}
			}
			SelectedImageIndex = ImageIndex;
			this.Nodes.Add(new CLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}

		public MethodPointer MethodRef
		{
			get
			{
				return (MethodPointer)(this.OwnerPointer);
			}
		}
		public MethodInfo MethodInformation
		{
			get
			{
				return ((MethodInfoPointer)(this.OwnerPointer)).MethodInformation;
			}
		}

		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is MethodParamPointer || pointer is IAction);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoader(this.IsStatic));
			return l;
		}
		public Dictionary<string, string> ParameterTooltips
		{
			get
			{
				return _parameterToolTips;
			}
		}
		public override string Tooltips
		{
			get
			{
				string methodDesc;
				string returnDesc;
				MethodInfoPointer methodPointer = (MethodInfoPointer)this.OwnerPointer;
				_parameterToolTips = PMEXmlParser.GetMethodDescription(VPLUtil.GetObjectType(methodPointer.Owner.ObjectType), methodPointer.MethodInformation, out methodDesc, out returnDesc);
				for (int i = 0; i < this.Nodes.Count; i++)
				{
					TreeNodeMethodParameter tmp = this.Nodes[i] as TreeNodeMethodParameter;
					if (tmp != null)
					{
						MethodParamPointer mpp = tmp.OwnerPointer as MethodParamPointer;
						if (_parameterToolTips.ContainsKey(mpp.MemberName))
						{
							tmp.SetToolTips(_parameterToolTips[mpp.MemberName]);
						}
					}
				}
				if (methodPointer.ReturnType.Equals(typeof(void)))
					return string.Format("{0}", methodDesc);
				else
					return string.Format("{0} Return value: {1}", methodDesc, returnDesc);
			}
		}

		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					return false;
				}
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Method
					|| tv.SelectionType == EnumObjectSelectType.Action
					)
				{
					if (!this.IsStatic)
					{
						if (this.OwnerPointer == null)
						{
							return false;
						}
						else
						{
							if (DesignUtil.HasStaticOwner(this.OwnerPointer))
							{
							}
							else if (this.OwnerPointer.RootPointer == null)
							{
								return false;
							}
						}
					}
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Object)
				{
					MethodInfo mif = MethodInformation;
					if (!mif.ReturnType.Equals(typeof(void)))
					{
						ParameterInfo[] ps = mif.GetParameters();
						if (ps == null || ps.Length == 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		public override TreeNodeObject GetActionOwnerNode()
		{
			TreeNodeObjectCollection tnc = this.Parent as TreeNodeObjectCollection;
			if (tnc != null)
			{
				return tnc.Parent as TreeNodeObject;
			}
			return null;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (IsStatic)
				this.ImageIndex = TreeViewObjectExplorer.IMG_S_METHOD_ACTS;
			else
			{
				MethodInfoPointer mif = this.OwnerPointer as MethodInfoPointer;
				if (mif.IsWebServerMethod)
				{
					this.ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEB_ACT;
				}
				else
				{
					this.ImageIndex = TreeViewObjectExplorer.IMG_METHOD_ACTS;
				}
			}
			this.SelectedImageIndex = this.ImageIndex;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (this.SelectionTarget != EnumObjectSelectType.Method && MainClassPointer != null)
				{
					MenuItem[] mi = new MenuItem[1];
					mi[0] = new MenuItemWithBitmap("Create action", OnCreateNewAction, Resources._methodAction.ToBitmap());
					return mi;
				}
			}
			return null;
		}
		/// <summary>
		/// create a new action using this method
		/// </summary>
		/// <returns></returns>
		public override IAction CreateNewAction()
		{
			MethodPointer mp = this.OwnerPointer as MethodPointer;
			if (mp.RootPointer == null)
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					IObjectPointer op = mp;
					while (op.Owner != null)
					{
						op = op.Owner;
					}
					op.Owner = tv.RootId;
				}
			}
			ActionClass act = new ActionClass(this.MainClassPointer);
			MethodPointer mp2 = (MethodPointer)mp.Clone();
			//adjust PropertyClass to CustomPropertyPointer
			IObjectPointer p0 = mp2;
			IObjectPointer p = mp2.Owner;
			while (p != null)
			{
				PropertyClass pc = p as PropertyClass;
				if (pc != null)
				{
					//convert pc to CustomPropertyPointer
					p0.Owner = pc.CreatePointer();
					break;
				}
				p0 = p;
				p = p.Owner;
			}
			MemberComponentIdCustom.AdjustHost(mp2);
			act.ActionMethod = mp2;
			act.ActionName = mp.DefaultActionName;
			return act;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeMethodParameter np = Nodes[i] as TreeNodeMethodParameter;
					if (np != null)
					{
						if (o.IsSameObjectRef(np.OwnerPointer))
						{
							if (ownerStack.Count == 0)
							{
								return np;
							}
							else
							{
								return np.LocateObjectNode(ownerStack);
							}
						}
					}
				}
			}
			return null;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				UInt32 scopeId = parentNode.ScopeMethodId;
				TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
				Dictionary<UInt32, IAction> actions = null;
				if (topClass != null)
				{
					if (!topClass.StaticScope)
					{
						actions = topClass.GetActions();
					}
				}
				else
				{
					if (tv != null)
					{
						ClassPointer cp = tv.RootId;
						if (cp != null)
						{
							actions = cp.GetActions();
						}
					}
				}
				TreeNodeMethod mNode = parentNode as TreeNodeMethod;
				Dictionary<string, string> pts = mNode.ParameterTooltips;
				MethodPointer mmp = parentNode.OwnerPointer as MethodPointer;
				ParameterInfo[] pifs = mmp.Info;
				if (pifs != null && pifs.Length > 0)
				{
					for (int i = 0; i < pifs.Length; i++)
					{
						MethodParamPointer p = new MethodParamPointer();
						p.Owner = mmp;
						if (string.IsNullOrEmpty(pifs[i].Name))
						{
							p.MemberName = "index" + i.ToString();
						}
						else
						{
							p.MemberName = pifs[i].Name;
						}
						TreeNodeMethodParameter tmp = new TreeNodeMethodParameter(false, p);
						if (pts != null && pts.ContainsKey(p.MemberName))
						{
							tmp.SetToolTips(pts[p.MemberName]);
						}
						parentNode.Nodes.Add(tmp);
					}
				}
				if (actions != null)
				{
					foreach (IAction a in actions.Values)
					{
						ActionClass ac = a as ActionClass;
						if (ac != null)
						{
							if (ac.ScopeMethodId != 0)
							{
								if (ac.ScopeMethodId != scopeId)
								{
									continue;
								}
							}
							MethodPointer mp0 = ac.ActionMethod as MethodPointer;
							if (mp0 != null)
							{
								if (mp0.IsSameObjectRef(mmp))
								{
									parentNode.Nodes.Add(new TreeNodeAction(parentNode.IsStatic, a));
								}
							}
						}
					}
				}
			}
		}
	}
	public class TreeNodeMethodCollection : TreeNodeObjectCollection
	{
		public TreeNodeMethodCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			if (isForStatic)
				Text = "Static methods";
			else
				Text = "Methods inherited";
			this.Nodes.Add(new MethodLoader(this.IsStatic));
		}
		protected override bool OnCheckIncludeAction(IAction act)
		{
			SubMethodInfoPointer smip = act.ActionMethod as SubMethodInfoPointer;
			if (smip != null)
			{
				return false;
			}
			return true;
		}

		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return MethodLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public TreeNodeMethod GetMethodNode(IMethod method)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeMethod mNode = Nodes[i] as TreeNodeMethod;
				if (mNode != null)
				{
					IMethod m = mNode.OwnerPointer as IMethod;
					if (m != null)
					{
						if (m.IsSameMethod(method))
						{
							return mNode;
						}
					}
				}
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is MethodPointer || pointer is MethodParamPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new MethodLoader(this.IsStatic));
			return lst;
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_METHODS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_METHODS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_METHODS_WITHACTS;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool IsPointerNode { get { return false; } }
	}
	class MethodLoader : TreeNodeLoader
	{
		public MethodLoader(bool forStatic)
			: base(forStatic)
		{
		}
		public static TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack, TreeNodeObject parentNode)
		{
			if (parentNode.NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				for (int i = 0; i < parentNode.Nodes.Count; i++)
				{
					TreeNodeObject tn = parentNode.Nodes[i] as TreeNodeObject;
					if (tn != null)
					{
						if (o.IsSameObjectRef(tn.OwnerPointer))
						{
							if (ownerStack.Count == 0)
							{
								return tn;
							}
							else
							{
								return tn.LocateObjectNode(ownerStack);
							}
						}
					}
				}
			}
			return null;
		}
		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			bool isWebPrj = false;
			if (tv != null && tv.RootId != null && parentNode != null && parentNode.OwnerPointer != null && parentNode.OwnerPointer.RootPointer != null)
			{
				if (tv.Project != null && (tv.Project.ProjectType == EnumProjectType.WebAppPhp || tv.Project.ProjectType == EnumProjectType.WebAppAspx))
				{
					isWebPrj = true;
					if (!ForStatic && tv.RootId.ClassId != parentNode.OwnerPointer.RootPointer.ClassId)
					{
						return;
					}
				}
			}
			IObjectPointer objRef = parentNode.OwnerPointer;
			TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
			bool webPrj = false;
			bool forPhp = false;
			if (tv.Project != null)
			{
				forPhp = (tv.Project.ProjectType == EnumProjectType.WebAppPhp);
				webPrj = tv.Project.IsWebApplication;
			}
			Dictionary<UInt32, IAction> actions = null;
			if (topClass != null)
			{
				if (!topClass.StaticScope)
				{
					actions = topClass.GetActions();
				}
			}
			else
			{
				TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
				if (rootType != null)
				{
					actions = rootType.GetActions();
				}
			}
			SortedList<string, TreeNode> newNodes = new SortedList<string, TreeNode>();
			EnumReflectionMemberInfoSelectScope scope;
			if (ForStatic)
				scope = EnumReflectionMemberInfoSelectScope.StaticOnly;
			else
				scope = EnumReflectionMemberInfoSelectScope.InstanceOnly;
			bool isWebPage = false;
			bool isRoot = false;
			ClassPointer cp = objRef as ClassPointer;
			if (cp != null)
			{
				isRoot = true;
				if (typeof(WebPage).IsAssignableFrom(cp.BaseClassType))
				{
					isWebPage = true;
				}
			}
			MethodInfo[] mifs = null;
			IExtendedPropertyOwner epo = null;
			if (objRef.Owner != null)
			{
				PropertyPointer pp = objRef as PropertyPointer;
				if (pp != null && pp.RunAt == EnumWebRunAt.Client && isWebPrj)
				{
					epo = objRef.Owner.ObjectInstance as IExtendedPropertyOwner;
					if (epo != null)
					{
						Type t = epo.PropertyCodeType(pp.MemberName);
						if (t != null)
						{
							if (t.Equals(typeof(DateTime)))
							{
								mifs = VPLUtil.GetMethods(typeof(JsDateTime), scope, false, true, isRoot, webPrj);
							}
							else
							{
								mifs = VPLUtil.GetMethods(t, scope, false, true, isRoot, webPrj);
							}
						}
					}
				}
			}
			if (mifs == null)
			{
				Type t = DesignUtil.GetTypeParameterValue(objRef);
				if (t != null)
				{
					mifs = VPLUtil.GetMethods(t, scope, false, true, isRoot, webPrj);
				}
			}
			if (mifs == null)
			{
				IWebClientControl webC = objRef.ObjectInstance as IWebClientControl;
				if ((webC != null || (objRef.ObjectInstance is IWebServerProgrammingSupport)) && isWebPage)
				{
					mifs = MethodInfoWebClient.GetWebMethods(ForStatic, objRef.ObjectInstance);
				}
				else
				{
					CustomMethodParameterPointer cmp = objRef as CustomMethodParameterPointer;
					if (cmp != null)
					{
						if (cmp.Parameter != null && cmp.Parameter.ParameterLibType != null)
						{
							TypeX tx = cmp.Parameter.ParameterLibType as TypeX;
							if (tx != null)
							{
								ClassPointer cpx = tx.Owner;
								if (cpx == null)
								{
									if (tx.ClassId != 0)
									{
										if (tv.Project != null)
										{
											cpx = ClassPointer.CreateClassPointer(tx.ClassId, tv.Project);
										}
									}
								}
								if (cpx != null)
								{
									XClass<InterfaceClass> xi = cpx.ObjectInstance as XClass<InterfaceClass>;
									if (xi != null)
									{
										InterfaceClass ifc = xi.ObjectValue as InterfaceClass;
										if (ifc != null)
										{
											if (ifc.Methods != null)
											{
												InterfacePointer ifp = new InterfacePointer(ifc);
												newNodes = new SortedList<string, TreeNode>();
												foreach (InterfaceElementMethod iep in ifc.Methods)
												{
													InterfaceElementMethod iep0 = iep.Clone() as InterfaceElementMethod;
													iep0.SetVariablePointer(cmp);
													newNodes.Add(iep.Name, new TreeNodeInterfaceMethod(iep0, ifp));
												}
												parentNode.AddSortedNodes(newNodes);
											}
											return;
										}
									}
								}
							}
						}
					}
					Type objType = null;
					ClassInstancePointer cr0 = objRef.ObjectInstance as ClassInstancePointer;
					if (cr0 != null)
					{
						objType = cr0.ObjectType;
						mifs = VPLUtil.GetMethods(cr0.ObjectType, scope, false, true, isRoot, webPrj);
					}
					else if (ForStatic || objRef.ObjectInstance == null)
					{
						RuntimeInstance ri = objRef.ObjectInstance as RuntimeInstance;
						if (ri != null)
						{
							objType = ri.InstanceType;
						}
						else
						{
							objType = objRef.ObjectType;
						}
						if (objType != null && JsTypeAttribute.IsJsType(objType))
						{
							mifs = MethodInfoWebClient.GetWebMethods(ForStatic, objType);
						}
						else
						{
							mifs = VPLUtil.GetMethods(objType, scope, false, true, isRoot, webPrj);
						}
					}
					else if (objRef is TypePointer)
					{
						objType = objRef.ObjectType;
						mifs = VPLUtil.GetMethods(objRef.ObjectType, scope, false, true, isRoot, webPrj);
					}
					else
					{
						objType = objRef.ObjectType;
						if (objType is TypeX)
						{
							mifs = VPLUtil.GetMethods(objType, scope, false, true, isRoot, webPrj);
						}
						else
						{
							IDrawDesignControl dc = objRef.ObjectInstance as IDrawDesignControl;
							if (dc != null)
							{
								mifs = VPLUtil.GetMethods(dc.Item, scope, false, true, isRoot, webPrj);
							}
							else
							{
								ICustomMethodDescriptor icmd = objRef.ObjectInstance as ICustomMethodDescriptor;
								if (icmd != null)
								{
									mifs = icmd.GetMethods(scope);
								}
								else
								{
									if (objRef.ObjectInstance != null)
									{
										objType = objRef.ObjectInstance.GetType();
									}
									mifs = VPLUtil.GetMethods(objRef.ObjectInstance, scope, false, true, isRoot, webPrj);
								}
							}
						}
					}
					bool isWebClient = false;
					if (objType != null)
					{
						object[] avs = objType.GetCustomAttributes(typeof(WebClientClassAttribute), true);
						if (avs != null && avs.Length > 0)
						{
							isWebClient = true;
						}
						else
						{
							if (JsTypeAttribute.IsJsType(objType))
							{
								isWebClient = true;
							}
						}
					}
					if (!isWebClient)
					{
						if (mifs != null && mifs.Length > 0)
						{
							if (isWebPage)
							{
								List<MethodInfo> mifs2 = new List<MethodInfo>();
								for (int k = 0; k < mifs.Length; k++)
								{
									if (objType.IsArray && (string.CompareOrdinal(mifs[k].Name, "Set") == 0 || string.CompareOrdinal(mifs[k].Name, "Get") == 0))
									{
										mifs2.Add(mifs[k]);
									}
									else
									{
										object[] objs = mifs[k].GetCustomAttributes(typeof(WebClientMemberAttribute), true);
										if (objs == null || objs.Length == 0) //why?
										{
											//fix a bug that static methods without WebClientMemberAttribute are displayed
											//not sure why it it is displayed, make a fix limited to bug scope
											if (!ForStatic)
											{
												mifs2.Add(mifs[k]);
											}
										}
										else
										{
											//fix a bug that static methods with WebClientMemberAttribute are not displayed
											//not sure why it is not displayed, make a fix limited to bug scope
											if (ForStatic)
											{
												mifs2.Add(mifs[k]);
											}
											else
											{
												objs = mifs[k].GetCustomAttributes(typeof(WebServerMemberAttribute), true);
												if (objs != null && objs.Length > 0)
												{
													mifs2.Add(mifs[k]);
												}
											}
										}
									}
								}
								mifs = new MethodInfo[mifs2.Count];
								mifs2.CopyTo(mifs, 0);
							}
							else if (forPhp)
							{
								List<MethodInfo> mifs2 = new List<MethodInfo>();
								for (int k = 0; k < mifs.Length; k++)
								{
									if (objType.IsArray && (string.CompareOrdinal(mifs[k].Name, "Set") == 0 || string.CompareOrdinal(mifs[k].Name, "Get") == 0))
									{
										mifs2.Add(mifs[k]);
									}
									else
									{
										object[] objs;
										objs = mifs[k].GetCustomAttributes(typeof(WebClientMemberAttribute), true);
										if (objs != null && objs.Length > 0) //for client event handling
										{
											mifs2.Add(mifs[k]);
										}
										else
										{
											objs = mifs[k].GetCustomAttributes(typeof(WebServerMemberAttribute), true);
											if (objs != null && objs.Length > 0)
											{
												mifs2.Add(mifs[k]);
											}
										}
									}
								}
								mifs = new MethodInfo[mifs2.Count];
								mifs2.CopyTo(mifs, 0);
							}
						}
					}
				}
			}
			if (mifs != null)
			{
				for (int i = 0; i < mifs.Length; i++)
				{
					if (parentNode.SelectionTarget == EnumObjectSelectType.Object)
					{
						if (mifs[i].ReturnType.Equals(typeof(void)))
						{
							continue;
						}
						ParameterInfo[] ps = mifs[i].GetParameters();
						if (ps != null && ps.Length > 0)
						{
							continue;
						}
					}
					if (VPLUtil.IsNotForProgramming(mifs[i]))
					{
						continue;
					}
					if (string.CompareOrdinal(mifs[i].Name, "Show") == 0)
					{
						ParameterInfo[] pms = mifs[i].GetParameters();
						if (pms != null && pms.Length == 1)
						{
							if (!typeof(DrawingPage).Equals(mifs[i].DeclaringType))
							{
								continue;
							}
						}
					}
					MethodInfoPointer mp = new MethodInfoPointer();
					mp.Owner = objRef;
					mp.SetMethodInfo(mifs[i]);
					int c;
					TreeNodeMethod nodeMethod = new TreeNodeMethod(ForStatic, mp);
					string key = mp.GetMethodSignature(out c);
					TreeNode nodeExist;
					if (newNodes.TryGetValue(key, out nodeExist))
					{
						TreeNodeMethod mnd = (TreeNodeMethod)nodeExist;
						if (mnd.MethodInformation.DeclaringType.Equals(mnd.MethodInformation.ReflectedType))
						{
							key = key + " - " + mifs[i].DeclaringType.Name;
						}
						else
						{
							if (mifs[i].DeclaringType.Equals(mifs[i].ReflectedType))
							{
								newNodes[key] = nodeMethod;
								key = key + " - " + mnd.MethodInformation.DeclaringType.Name;
							}
							else
							{
								key = key + " - " + mifs[i].DeclaringType.Name;
							}
						}
					}
					if (!newNodes.ContainsKey(key))
					{
						newNodes.Add(key, nodeMethod);
					}
					//load actions
					if (actions != null)
					{
						bool bHasActions = false;
						foreach (IAction a in actions.Values)
						{
							ActionClass ac = a as ActionClass;
							if (ac != null)
							{
								MethodPointer mp0 = ac.ActionMethod as MethodPointer;
								if (mp0 != null)
								{
									if (mp0.IsSameObjectRef(mp))
									{
										bHasActions = true;
										break;
									}
								}
							}
						}
						if (bHasActions)
						{
							nodeMethod.ShowActionIcon();
						}
					}
				}
				parentNode.AddSortedNodes(newNodes);
			}
		}
	}
	class TreeNodeMemberLoader : TreeNodeLoader
	{
		public TreeNodeMemberLoader(bool forStatic)
			: base(forStatic)
		{
		}

		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			UInt32 scopeId = parentNode.ScopeMethodId;
			IObjectPointer objRef = parentNode.MemberOwner as IObjectPointer;
			Type ti = VPLUtil.GetElementType(objRef.ObjectType);
			if (ti != null)
			{
				TreeNodeClassType tnc = new TreeNodeClassType(tv, ti, 0);
				parentNode.Nodes.Add(tnc);
			}
			parentNode.Nodes.Add(new TreeNodePropertyCollection(tv, parentNode, false, objRef, scopeId));
			parentNode.Nodes.Add(new TreeNodeFieldCollection(tv, parentNode, false, objRef, scopeId));
			parentNode.Nodes.Add(new TreeNodeEventCollection(tv, parentNode, false, objRef, scopeId));
			parentNode.Nodes.Add(new TreeNodeMethodCollection(tv, parentNode, false, objRef, scopeId));
		}
		public static TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack, TreeNodeObject parentNode)
		{
			if (ownerStack.Count > 0 && parentNode.NextLevelLoaded)
			{
				IObjectPointer o = ownerStack.Peek();
				PropertyPointer p = o as PropertyPointer;
				if (p != null)
				{
					for (int i = 0; i < parentNode.Nodes.Count; i++)
					{
						TreeNodePropertyCollection tn = parentNode.Nodes[i] as TreeNodePropertyCollection;
						if (tn != null)
						{
							return tn.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					FieldPointer f = o as FieldPointer;
					if (f != null)
					{
						for (int i = 0; i < parentNode.Nodes.Count; i++)
						{
							TreeNodeFieldCollection tnf = parentNode.Nodes[i] as TreeNodeFieldCollection;
							if (tnf != null)
							{
								return tnf.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						EventPointer e = o as EventPointer;
						if (e != null)
						{
							for (int i = 0; i < parentNode.Nodes.Count; i++)
							{
								TreeNodeEventCollection tne = parentNode.Nodes[i] as TreeNodeEventCollection;
								if (tne != null)
								{
									return tne.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							MethodInfoPointer m = o as MethodInfoPointer;
							if (m != null)
							{
								for (int i = 0; i < parentNode.Nodes.Count; i++)
								{
									TreeNodeMethodCollection tnm = parentNode.Nodes[i] as TreeNodeMethodCollection;
									if (tnm != null)
									{
										return tnm.LocateObjectNode(ownerStack);
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
	}
	public class TreeNodeEventParam : TreeNodeObject
	{
		private string _tooltips;
		public TreeNodeEventParam(bool isForStatic, EventParamPointer objectPointer)
			: base(isForStatic, objectPointer)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			//do not load them here. when to make them available? in method editor?
			//Assigning an action to an event and then editing the action gives the context of the event. Selecting a property should allow selecting an event parameter and its members
			//loading them can serve as a browsing/checking purpose
			if (objectPointer.ObjectType.IsEnum)
			{
				this.Nodes.Add(new TreeNodeEnumValues(objectPointer.ObjectType));
			}
			else
			{
				this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return TreeNodeMemberLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return false;
		}
		public override string Tooltips
		{
			get
			{
				return _tooltips;
			}
		}
		public void SetToolTips(string v)
		{
			_tooltips = v;
			ToolTipText = v;
		}
	}
	public class TreeNodeEvent : TreeNodeObject, IEventNode
	{
		private Dictionary<string, string> _parameterToolTips;
		public TreeNodeEvent(bool isForStatic, EventPointer eventPointer)
			: base(isForStatic, eventPointer)
		{
			//
			if (isForStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_EVENT;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_EVENT;
			SelectedImageIndex = ImageIndex;
			this.Nodes.Add(new CLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		public override IAction CreateNewAction()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.ScopeMethod != null)
				{
					return new AssignHandler(this.OwnerPointer as IEvent);
				}
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is EventPointer || pointer is EventParamPointer || pointer is ClassPointer);
		}
		public IEvent OwnerEvent { get { return (IEvent)(this.OwnerPointer); } }
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoader(this.IsStatic));
			return l;
		}
		public override string Tooltips
		{
			get
			{
				string eventDesc;
				EventPointer epp = this.OwnerPointer as EventPointer;
				_parameterToolTips = PMEXmlParser.GetEventDescription(VPLUtil.GetObjectType(epp.Owner.ObjectType), epp.Info, out eventDesc);
				for (int i = 0; i < this.Nodes.Count; i++)
				{
					TreeNodeEventParam tmp = this.Nodes[i] as TreeNodeEventParam;
					if (tmp != null)
					{
						EventParamPointer mpp = tmp.OwnerPointer as EventParamPointer;
						if (_parameterToolTips.ContainsKey(mpp.MemberName))
						{
							tmp.SetToolTips(_parameterToolTips[mpp.MemberName]);
						}
					}
				}
				return eventDesc;
			}
		}
		public Dictionary<string, string> ParameterTooltips
		{
			get
			{
				return _parameterToolTips;
			}
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Event
					|| tv.SelectionType == EnumObjectSelectType.Object
					|| tv.SelectionType == EnumObjectSelectType.Action
					);
			}
			return false;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			List<MenuItem> mlist = new List<MenuItem>();
			bool isWebPrj = false;
			TreeNodeClassRoot tr = TopLevelRootClassNode;
			if (tr == null)
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					tr = tv.RootClassNode;
				}
			}
			if (tr != null)
			{
				EventPointer objRef = OwnerPointer as EventPointer;
				if (tr.RootObjectId != null && tr.RootObjectId.Project != null)
				{
					isWebPrj = tr.RootObjectId.Project.ProjectType == EnumProjectType.WebAppAspx || tr.RootObjectId.Project.ProjectType == EnumProjectType.WebAppPhp;
				}
				if (!readOnly)
				{
					if (isWebPrj && DesignUtil.IsWebClientObject(objRef.Owner))
					{
						mlist.Add(new MenuItemWithBitmap("Assign action", new EventHandler(TreeNodeEventAppend_Click), Resources._eventHandlers.ToBitmap()));
						mlist.Add(new MenuItemWithBitmap("Add client handler actions", new EventHandler(addClientHandlerMethod_Click), Resources._handlerMethod.ToBitmap()));
						mlist.Add(new MenuItemWithBitmap("Add server handler actions", new EventHandler(addHandlerMethod_Click), Resources._webServerHandler.ToBitmap()));
						mlist.Add(new MenuItemWithBitmap("Add client and download handler actions", new EventHandler(addDownloadHandlerMethod_Click), Resources._webHandler2.ToBitmap()));
					}
					else
					{
						mlist.Add(new MenuItemWithBitmap("Assign action", new EventHandler(TreeNodeEventAppend_Click), Resources._eventHandlers.ToBitmap()));
						mlist.Add(new MenuItemWithBitmap("Add handler method", new EventHandler(addHandlerMethod_Click), Resources._handlerMethod.ToBitmap()));
					}
					mlist.Add(new MenuItemWithBitmap("Create dynamic handler", new EventHandler(dynamicHandler_Click), Resources._eventHandler.ToBitmap()));
				}
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					if (!readOnly)
					{
						mlist.Add(new MenuItem("-"));
					}
					if (!ea.BreakBeforeExecute)
					{
						mlist.Add(new MenuItemWithBitmap("Add breakpoint on entering event", new EventHandler(TreeNodeEventAppend_AddBreakpointOnBefore), Resources._breakpoint.ToBitmap()));
					}
					if (!ea.BreakAfterExecute)
					{
						mlist.Add(new MenuItemWithBitmap("Add breakpoint on leaving event", new EventHandler(TreeNodeEventAppend_AddBreakpointOnAfter), Resources._breakpoint.ToBitmap()));
					}
				}
			}
			MenuItem[] mi = null;
			if (mlist.Count > 0)
			{
				mi = new MenuItem[mlist.Count];
				mlist.CopyTo(mi);
			}
			return mi;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_EVENT_WITHHANDLER;
			this.SelectedImageIndex = this.ImageIndex;
		}
		public ITreeNodeBreakPoint ShowEventBreakPoint(bool forEntering)
		{
			TreeNodeBreakPoint nbp0;
			TreeNodeBreakPoint nbp;
			if (forEntering)
			{
				nbp0 = getBreakPointAtAfter();
				nbp = getBreakPointAtBefore();
			}
			else
			{
				nbp0 = getBreakPointAtBefore();
				nbp = getBreakPointAtAfter();
			}
			if (nbp0 != null)
			{
				nbp0.ShowBreakPoint(false);
			}
			if (nbp != null)
			{
				nbp.ShowBreakPoint(true);
			}
			return nbp;
		}
		public ITreeNodeBreakPoint ShowEventBreakPoint(int actionIndex)
		{
			int n = -1;
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeEventAction tna = Nodes[i] as TreeNodeEventAction;
				if (tna != null)
				{
					n++;
					if (n == actionIndex)
					{
						tna.ShowBreakPoint(true);
						return tna;
					}
				}
			}
			return null;
		}
		TreeNodeBreakPoint getBreakPointAtBefore()
		{
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeBreakPoint nd = Nodes[i] as TreeNodeBreakPoint;
				if (nd != null)
				{
					if (nd.ForBeforeExecution)
					{
						return nd;
					}
				}
			}
			return null;
		}
		TreeNodeBreakPoint getBreakPointAtAfter()
		{
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeBreakPoint nd = Nodes[i] as TreeNodeBreakPoint;
				if (nd != null)
				{
					if (!nd.ForBeforeExecution)
					{
						return nd;
					}
				}
			}
			return null;
		}
		void TreeNodeEventAppend_AddBreakpointOnBefore(object sender, EventArgs e)
		{
			TreeNodeBreakPoint nd = getBreakPointAtBefore();
			if (nd == null)
			{
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				EventPointer objRef = OwnerPointer as EventPointer;
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					ea.BreakBeforeExecute = true;
					nd = new TreeNodeBreakPoint(this.IsStatic, ea, objRef);
					nd.ForBeforeExecution = true;
					int n = 0;
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodeEventParam p = Nodes[i] as TreeNodeEventParam;
						if (p == null)
						{
							break;
						}
						n = i + 1;
					}
					Nodes.Insert(n, nd);
					ea.SaveEventBreakPointsToXml();
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					tv.NotifyChanges();
				}
			}
		}
		void TreeNodeEventAppend_AddBreakpointOnAfter(object sender, EventArgs e)
		{
			TreeNodeBreakPoint nd = getBreakPointAtAfter();
			if (nd == null)
			{
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				EventPointer objRef = OwnerPointer as EventPointer;
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					ea.BreakAfterExecute = true;
					nd = new TreeNodeBreakPoint(this.IsStatic, ea, objRef);
					nd.ForBeforeExecution = false;
					Nodes.Add(nd);
					ea.SaveEventBreakPointsToXml();
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					tv.NotifyChanges();
				}
			}
		}
		void selectLastAction()
		{
			if (this.TreeView != null)
			{
				LoadNextLevel();
				for (int i = this.Nodes.Count - 1; i >= 0; i--)
				{
					TreeNodeEventAction ea = Nodes[i] as TreeNodeEventAction;
					if (ea != null)
					{
						this.TreeView.SelectedNode = Nodes[i];
						break;
					}
				}
			}
		}
		void TreeNodeEventAppend_Click(object sender, EventArgs e)
		{
			EventPointer ep = this.OwnerPointer as EventPointer;
			IRootNode root = this.TopLevelRootClassNode;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (root == null)
				{
					root = tv.RootClassNode;
				}
				if (root != null && root.ViewersHolder != null)
				{
					if (root.ViewersHolder.AssignActions(ep, tv.FindForm()))
					{
						selectLastAction();
					}
				}
			}
		}
		void dynamicHandler_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv.Parent != null)
			{
				IEvent evt = this.OwnerEvent;
				IRootNode r = RootClassNode;
				ClassPointer root = r.ClassData.RootClassID;
				Type t;
				if (root.Project.IsWebApplication)
				{
					t = typeof(WebClientEventHandlerMethodClientActions);
				}
				else
				{
					t = typeof(EventHandlerMethod);
				}
				ActionAttachEvent aae = root.AddDynamicHandler(t, evt, tv.Parent.RectangleToScreen(this.Bounds), tv.FindForm());
				if (aae != null)
				{
					this.ResetNextLevel(tv);
					this.LoadNextLevel();
					this.Expand();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeAction em = this.Nodes[i] as TreeNodeAction;
						if (em != null)
						{
							if (aae.IsSameObjectRef(em.Action))
							{
								tv.SelectedNode = em;
								break;
							}
						}
					}
				}
			}
		}
		void addHandlerMethod_Click(object sender, EventArgs e)
		{
			addEventHandler(typeof(WebClientEventHandlerMethodServerActions));
		}
		void addClientHandlerMethod_Click(object sender, EventArgs e)
		{
			addEventHandler(typeof(WebClientEventHandlerMethodClientActions));
		}
		void addDownloadHandlerMethod_Click(object sender, EventArgs e)
		{
			addEventHandler(typeof(WebClientEventHandlerMethodDownloadActions));
		}
		void addEventHandler(Type editType)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv.Parent != null)
			{
				IEvent evt = this.OwnerEvent;
				IRootNode r = RootClassNode;
				ClassPointer root = r.ClassData.RootClassID;
				EventHandlerMethod ehm = root.AddEventhandlerMethod(editType, evt, 0, tv.Parent.RectangleToScreen(this.Bounds), tv.FindForm());
				if (ehm != null)
				{
					this.ResetNextLevel(tv);
					this.LoadNextLevel();
					this.Expand();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeEventMethod em = this.Nodes[i] as TreeNodeEventMethod;
						if (em != null)
						{
							if (ehm.IsSameMethod(em.Method))
							{
								tv.SelectedNode = em;
								break;
							}
						}
					}
				}
			}
		}

		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeEvent tne = (TreeNodeEvent)parentNode;
				Dictionary<string, string> tps = tne.ParameterTooltips;
				if (tv != null)
				{
					EventPointer objRef = parentNode.OwnerPointer as EventPointer;
					ParameterInfo[] pifs = objRef.Parameters;
					if (pifs != null)
					{
						for (int i = 0; i < pifs.Length; i++)
						{
							if (parentNode.IsInScope(pifs[i].ParameterType))
							{
								EventParamPointer epp = new EventParamPointer();
								epp.Owner = objRef;
								epp.MemberName = pifs[i].Name;
								TreeNodeEventParam tmp = new TreeNodeEventParam(false, epp);
								if (tps != null && tps.ContainsKey(pifs[i].Name))
								{
									tmp.SetToolTips(tps[pifs[i].Name]);
								}
								parentNode.Nodes.Add(tmp);
							}
						}
					}
					TreeNodeClassRoot tr = parentNode.TopLevelRootClassNode;
					if (tr == null)
					{
						tr = tv.RootClassNode;
					}
					if (tr != null)
					{
						ClassPointer root = tr.RootHost;
						IList<EventAction> eas = tr.GetEventHandlers(objRef);
						foreach (EventAction ea in eas)
						{
							TaskIdList idList = ea.TaskIDList;
							if (ea.BreakBeforeExecute)
							{
								TreeNodeBreakPoint nb = new TreeNodeBreakPoint(parentNode.IsStatic, ea, objRef);
								nb.ForBeforeExecution = true;
								parentNode.Nodes.Add(nb);
							}
							for (int i = 0; i < idList.Count; i++)
							{
								TaskID taskId = idList[i];
								HandlerMethodID hmd = taskId as HandlerMethodID;
								if (hmd != null)
								{
									ActionAttachEvent aae = null;
									UInt32 aaId = ea.GetAssignActionId();
									if (aaId != 0)
									{
										aae = root.GetActionInstance(aaId) as ActionAttachEvent;
									}
									if (aae != null && aae.IsStatic == parentNode.IsStatic)
									{
										parentNode.Nodes.Add(new TreeNodeEventAction(parentNode.IsStatic, taskId, aae));
										XmlNodeList deNodes = root.XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
											"{0}/{1}[@attachId='{2}']", XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, aaId));
										foreach (XmlNode deN in deNodes)
										{
											UInt32 deId = XmlUtil.GetAttributeUInt(deN, XmlTags.XMLATT_ActionID);
											IAction deAct = root.GetActionInstance(deId);
											if (deAct != null)
											{
												parentNode.Nodes.Add(new TreeNodeAction(parentNode.IsStatic, deAct));
											}
										}
									}
									else
									{
										parentNode.Nodes.Add(new TreeNodeEventMethod(tv, parentNode.IsStatic, hmd.HandlerMethod));
									}
								}
								else
								{
									IAction a = null;
									if (taskId.ClassId == tr.ClassData.ObjectMap.ClassId)
									{
										a = tr.GetAction(taskId.WholeTaskId);
									}
									else
									{
										ObjectIDmap map = tr.RootObjectId.ObjectList;// ObjectIDmap.GetMap(tv.Project, taskId.ClassId, ClassPointer.OnAfterReadRootComponent);
										XmlNode classNode = map.XmlData;
										XmlNode actNode = classNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"{0}/{1}[@{2}='{3}']",
											XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, taskId.ActionId));
										if (actNode != null)
										{
											XmlObjectReader xr = map.Reader;
											a = (IAction)xr.ReadObject(actNode, null);
										}
									}
									if (a == null)
									{
									}
									else
									{
										parentNode.Nodes.Add(new TreeNodeEventAction(parentNode.IsStatic, taskId, a));
									}
								}
							}
							if (ea.BreakAfterExecute)
							{
								TreeNodeBreakPoint nb = new TreeNodeBreakPoint(parentNode.IsStatic, ea, objRef);
								nb.ForBeforeExecution = false;
								parentNode.Nodes.Add(nb);
							}
							parentNode.OnShowActionIcon();
						}
					}
					IEventInfoTree emi = objRef.Info as IEventInfoTree;
					if (emi != null)
					{
						IEventInfoTree[] subs = emi.GetSubEventInfo();
						if (subs != null && subs.Length > 0)
						{
							for (int i = 0; i < subs.Length; i++)
							{
								EventPointer ep = new EventPointer();
								ep.Owner = objRef.Owner;
								ep.MemberName = subs[i].Name;
								ep.EventId = subs[i].GetEventId();
								EventInfo eSub = subs[i].GetEventInfo();
								ep.SetEventInfo(eSub);
								TreeNodeEvent tneSub = new TreeNodeEvent(false, ep);
								parentNode.Nodes.Add(tneSub);
								if (tr.HasEventHandler(ep))
								{
									tneSub.OnShowActionIcon();
								}
							}
						}
					}
				}
			}
		}
	}
	public class TreeNodeCustomEventPointer : TreeNodeObject, IEventNode
	{
		public TreeNodeCustomEventPointer(bool isForStatic, CustomEventPointer eventObject)
			: base(isForStatic, eventObject)
		{
			if (isForStatic)
			{
				if (eventObject.IsOverride)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_SBC_EVENT;
				}
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_SC_EVENT;
				}
			}
			else
			{
				if (eventObject.IsOverride)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_BC_EVENT;
				}
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_C_EVENT;
				}
			}
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new CLoader(isForStatic));

		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		[Browsable(false)]
		public CustomEventPointer Event
		{
			get
			{
				return (CustomEventPointer)(this.OwnerPointer);
			}
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			CustomEventPointer property = this.Event;
			if (this.IsStatic)
			{
				if (property.IsOverride)
					this.ImageIndex = TreeViewObjectExplorer.IMG_SBCA_EVENT;
				else
					this.ImageIndex = TreeViewObjectExplorer.IMG_SCA_EVENT;
			}
			else
			{
				if (property.IsOverride)
					this.ImageIndex = TreeViewObjectExplorer.IMG_BCA_EVENT;
				else
					this.ImageIndex = TreeViewObjectExplorer.IMG_CA_EVENT;
			}
			this.SelectedImageIndex = this.ImageIndex;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			List<MenuItem> mlist = new List<MenuItem>();
			if (!readOnly)
			{
				IRootNode root = this.TopLevelRootClassNode;
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (root != null && tv != null && tv.RootId != null)
				{
					if (root.ClassData.RootClassID.ClassId == tv.RootId.ClassId)
					{
						mlist.Add(new MenuItemWithBitmap("Assign action", new EventHandler(appendAction_Click), Resources._eventHandlers.ToBitmap()));
						mlist.Add(new MenuItemWithBitmap("Create dynamic handler", new EventHandler(dynamicHandler_Click), Resources._eventHandler.ToBitmap()));
					}
				}
			}
			TreeNodeClassRoot tr = TopLevelRootClassNode;
			EventClass objRef = OwnerPointer as EventClass;
			EventAction ea = tr.GetEventHandler(objRef);
			if (ea != null)
			{
				if (!readOnly)
				{
					mlist.Add(new MenuItem("-"));
				}
				if (!ea.BreakBeforeExecute)
				{
					mlist.Add(new MenuItemWithBitmap("Add breakpoint on entering event", new EventHandler(addBreakpointOnBefore_Click), Resources._breakpoint.ToBitmap()));
				}
				if (!ea.BreakAfterExecute)
				{
					mlist.Add(new MenuItemWithBitmap("Add breakpoint on leaving event", new EventHandler(addBreakpointOnAfter_Click), Resources._breakpoint.ToBitmap()));
				}
			}
			MenuItem[] mi = null;
			if (mlist.Count > 0)
			{
				mi = new MenuItem[mlist.Count];
				mlist.CopyTo(mi);
			}
			return mi;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is CustomEventPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoader(IsStatic));
			return l;
		}
		public void SetEventClass(EventClass e)
		{
			Event.ResetEventClass(e);
			ShowText();
			ResetNextLevel(TreeView as TreeViewObjectExplorer);
		}
		public override void ShowText()
		{
			Text = this.OwnerPointer.DisplayName;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Event
					|| tv.SelectionType == EnumObjectSelectType.Method
					|| tv.SelectionType == EnumObjectSelectType.Action
					);
			}
			return false;
		}
		public override IAction CreateNewAction()
		{
			CustomEventPointer ep = this.OwnerPointer as CustomEventPointer;
			if (ep != null)
			{
				EventClass ec = ep.Event as EventClass;
				if (ec != null)
				{
					IRootNode root = this.TopLevelRootClassNode;
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					if (root != null && root.ViewersHolder != null)
					{
						FireEventMethod fe = new FireEventMethod(new CustomEventPointer(ec, GetHolder()));
						ActionClass act = new ActionClass(this.MainClassPointer);
						act.ActionMethod = fe;
						act.ActionName = fe.DefaultActionName;
						act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
						fe.Action = act;
						fe.ValidateParameterValues(act.ParameterValues);
						return act;
					}
				}
			}
			return null;
		}
		TreeNodeBreakPoint getBreakPointAtBefore()
		{
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeBreakPoint nd = Nodes[i] as TreeNodeBreakPoint;
				if (nd != null)
				{
					if (nd.ForBeforeExecution)
					{
						return nd;
					}
				}
			}
			return null;
		}
		TreeNodeBreakPoint getBreakPointAtAfter()
		{
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeBreakPoint nd = Nodes[i] as TreeNodeBreakPoint;
				if (nd != null)
				{
					if (!nd.ForBeforeExecution)
					{
						return nd;
					}
				}
			}
			return null;
		}
		void addBreakpointOnBefore_Click(object sender, EventArgs e)
		{
			TreeNodeBreakPoint nd = getBreakPointAtBefore();
			if (nd == null)
			{
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				IEvent objRef = OwnerPointer as IEvent;
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					ea.BreakBeforeExecute = true;
					nd = new TreeNodeBreakPoint(this.IsStatic, ea, objRef);
					nd.ForBeforeExecution = true;
					int n = 0;
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodeEventParam p = Nodes[i] as TreeNodeEventParam;
						if (p == null)
						{
							break;
						}
						n = i + 1;
					}
					Nodes.Insert(n, nd);
					ea.SaveEventBreakPointsToXml();
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					tv.NotifyChanges();
				}
			}
		}
		void addBreakpointOnAfter_Click(object sender, EventArgs e)
		{
			TreeNodeBreakPoint nd = getBreakPointAtAfter();
			if (nd == null)
			{
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				IEvent objRef = OwnerPointer as IEvent;
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					ea.BreakAfterExecute = true;
					nd = new TreeNodeBreakPoint(this.IsStatic, ea, objRef);
					nd.ForBeforeExecution = false;
					Nodes.Add(nd);
					ea.SaveEventBreakPointsToXml();
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					tv.NotifyChanges();
				}
			}
		}
		void assignAction_Click(object sender, EventArgs e)
		{
			IEvent ep = this.OwnerPointer as IEvent;
			IRootNode root = this.TopLevelRootClassNode;
			if (root != null)
			{
				if (root.ViewersHolder.AssignActions(ep, TreeView.FindForm()))
				{
					this.LoadNextLevel();
					selectLastAction();
				}
			}
		}
		void selectLastAction()
		{
			if (this.TreeView != null)
			{
				LoadNextLevel();
				for (int i = this.Nodes.Count - 1; i >= 0; i--)
				{
					TreeNodeEventAction ea = Nodes[i] as TreeNodeEventAction;
					if (ea != null)
					{
						this.TreeView.SelectedNode = Nodes[i];
						break;
					}
				}
			}
		}
		void dynamicHandler_Click(object sender, EventArgs e)
		{
		}
		void appendAction_Click(object sender, EventArgs e)
		{
			IEvent ep = this.OwnerPointer as IEvent;
			IRootNode root = this.TopLevelRootClassNode;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (root != null && root.ViewersHolder != null)
			{
				if (root.ViewersHolder.AssignActions(ep, tv.FindForm()))
				{
					root.NotifyChanges();
					selectLastAction();
				}
			}
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
				if (topClass != null)
				{
				}
				else
				{
					TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
				}
				TreeNodeCustomEventPointer mNode = parentNode as TreeNodeCustomEventPointer;
				CustomEventPointer mmp = mNode.Event;
				if (mmp.ParameterCount > 0)
				{
					List<ParameterClass> parameters = mmp.GetParameters(mmp.CreateHandlerMethod(null));
					if (parameters != null)
					{
						foreach (ParameterClass p in parameters)
						{
							TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, p);
							parentNode.Nodes.Add(tmp);
						}
					}
				}
				TreeNodeClassRoot tr = parentNode.TopLevelRootClassNode;
				if (tr != null && tv != null && tv.RootId != null)
				{
					if (tr.ClassId == tv.RootId.ClassId)
					{
						ClassPointer root = tr.RootHost;
						IList<EventAction> eas = tr.GetEventHandlers(mmp);
						foreach (EventAction ea in eas)
						{
							TaskIdList idList = ea.TaskIDList;
							if (ea.BreakBeforeExecute)
							{
								TreeNodeBreakPoint nb = new TreeNodeBreakPoint(parentNode.IsStatic, ea, mmp);
								nb.ForBeforeExecution = true;
								parentNode.Nodes.Add(nb);
							}
							for (int i = 0; i < idList.Count; i++)
							{
								TaskID taskId = idList[i];
								HandlerMethodID hmd = taskId as HandlerMethodID;
								if (hmd != null)
								{
									ActionAttachEvent aae = null;
									UInt32 aaId = ea.GetAssignActionId();
									if (aaId != 0)
									{
										aae = root.GetActionInstance(aaId) as ActionAttachEvent;
									}
									if (aae != null && aae.IsStatic == parentNode.IsStatic)
									{
										parentNode.Nodes.Add(new TreeNodeEventAction(parentNode.IsStatic, taskId, aae));
									}
									else
									{
										parentNode.Nodes.Add(new TreeNodeEventMethod(tv, parentNode.IsStatic, hmd.HandlerMethod));
									}
								}
								else
								{
									IAction a = null;
									if (taskId.ClassId == tr.ClassData.ObjectMap.ClassId)
									{
										a = tr.GetAction(taskId.WholeTaskId);
									}
									else
									{
										XmlNode classNode = null;
										ObjectIDmap map = tr.RootObjectId.ObjectList;// ObjectIDmap.GetMap(tv.Project, taskId.ClassId, ClassPointer.OnAfterReadRootComponent);
										XmlNode actNode = classNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"{0}/{1}[@{2}='{3}']",
											XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, taskId.ActionId));
										if (actNode != null)
										{
											XmlObjectReader xr = map.Reader;
											a = (IAction)xr.ReadObject(actNode, null);
										}
									}
									if (a == null)
									{
									}
									else
									{
										parentNode.Nodes.Add(new TreeNodeEventAction(parentNode.IsStatic, taskId, a));
									}
								}
							}
							if (ea.BreakAfterExecute)
							{
								TreeNodeBreakPoint nb = new TreeNodeBreakPoint(parentNode.IsStatic, ea, mmp);
								nb.ForBeforeExecution = false;
								parentNode.Nodes.Add(nb);
							}
							parentNode.OnShowActionIcon();
						}
					}
				}
			}
		}

		#region IEventNode Members

		public IEvent OwnerEvent
		{
			get
			{
				return (IEvent)(this.OwnerPointer);
			}
		}

		#endregion
	}
	public class TreeNodeCustomEventPointerCollection : TreeNodeObjectCollection
	{
		public TreeNodeCustomEventPointerCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			Text = "Events";

			this.Nodes.Add(new CustomEventPointerLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SC_EVENTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_C_EVENTS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_A_S_C_EVENTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_A_C_EVENTS;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
		public TreeNodeCustomEventPointer GetEventPointerNode(UInt32 id)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomEventPointer tcp = Nodes[i] as TreeNodeCustomEventPointer;
				if (tcp != null)
				{
					if (tcp.Event.MemberId == id)
					{
						return tcp;
					}
				}
			}
			return null;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CustomEventPointerLoader(this.IsStatic));
			return lst;
		}
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			IObjectPointer pointer = this.OwnerPointer;
			if (pointer != null)
			{
				ClassPointer r;
				MemberComponentId mc = pointer as MemberComponentId;
				if (mc != null)
				{
					r = mc.RootHost;
				}
				else
				{
					r = pointer.RootPointer;
				}
				if (r != null)
				{
					EnumObjectDevelopType ot = EnumObjectDevelopType.Custom;
					bool isStatic = parentNode.IsBasePmeStatic;
					foreach (EventAction ea in r.EventHandlers)
					{
						IEvent ep = ea.Event;
						if (ep != null && ep.IsStatic == isStatic && (ot == EnumObjectDevelopType.Both || ep.ObjectDevelopType == ot))
						{
							IObjectPointer id;
							IMemberPointer mp = ep as IMemberPointer;
							if (mp != null)
								id = mp.Holder;
							else
								id = ep.Owner;
							while (id != null)
							{
								if (id.IsSameObjectRef(pointer))
								{
									return true;
								}
								else
								{
									if (id is IClass)
									{
										//return false;
										break;
									}
								}
								mp = id as IMemberPointer;
								if (mp != null)
									id = mp.Holder;
								else
									id = id.Owner;
							}
						}
					}
				}
			}
			return false;
		}
	}
	public class TreeNodeEventCollection : TreeNodeObjectCollection
	{
		public TreeNodeEventCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			if (isForStatic)
				Text = "Static events";
			else
				Text = "Events inherited";

			this.Nodes.Add(new EventsLoader(isForStatic));
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				return EventsLoader.LocateObjectNode(ownerStack, this);
			}
			return null;
		}
		/// <summary>
		/// this node may not have been added to TreeView
		/// </summary>
		/// <param name="scopeMethodId"></param>
		/// <param name="parentNode"></param>
		/// <returns></returns>
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			IObjectPointer pointer = this.OwnerPointer;
			if (pointer != null)
			{
				ClassPointer r = null;
				if (tv != null && tv.RootClassNode != null)
				{
					r = tv.RootClassNode.RootObjectId;
				}
				if (r == null)
				{
					MemberComponentId mc = pointer as MemberComponentId;
					if (mc != null)
					{
						r = mc.RootHost;
					}
					else
					{
						r = pointer.RootPointer;
					}
				}
				if (r == null)
				{
					if (parentNode != null && parentNode.OwnerPointer != null)
					{
						r = parentNode.OwnerPointer.RootPointer;
					}
				}
				if (r != null)
				{
					EnumObjectDevelopType ot;
					if (this.OwnerIdentity is ClassPointer)
					{
						ot = this.ObjectDevelopType;
					}
					else
					{
						ot = DesignUtil.GetBaseObjectDevelopType(this.OwnerIdentity);
					}
					bool isStatic = parentNode.IsBasePmeStatic;
					foreach (EventAction ea in r.EventHandlers)
					{
						IEvent ep = ea.Event;
						if (ep != null && ep.IsStatic == isStatic && (ot == EnumObjectDevelopType.Both || ep.ObjectDevelopType == ot))
						{
							if (ea.TaskIDList != null && ea.TaskIDList.Count > 0)
							{
								if (ep.Owner != null)
								{
									if (ep.Owner.IsSameObjectRef(pointer))
									{
										return true;
									}
								}
								IObjectPointer id;
								IMemberPointer mp = ep as IMemberPointer;
								if (mp != null)
									id = mp.Holder;
								else
									id = ep.Owner;
								while (id != null)
								{
									if (id.IsSameObjectRef(pointer))
									{
										return true;
									}
									else
									{
										if (id is IClass)
										{
											break;
										}
									}
									mp = id as IMemberPointer;
									if (mp != null)
										id = mp.Holder;
									else
										id = id.Owner;
								}
							}
						}
					}
				}
			}
			return false;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Library; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_EVENTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_EVENTS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_A_S_EVENTS;
			else
				this.ImageIndex = TreeViewObjectExplorer.IMG_A_Events;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new EventsLoader(this.IsStatic));
			return lst;
		}
		public override bool IsPointerNode { get { return false; } }
		public TreeNodeEvent GetEventNode(string eventName)
		{
			LoadNextLevel();
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeEvent e = Nodes[i] as TreeNodeEvent;
				if (e != null)
				{
					EventPointer epp = e.OwnerPointer as EventPointer;
					if (epp.MemberName == eventName)
					{
						return e;
					}
				}
			}
			return null;
		}

	}
	class EventsLoader : TreeNodeLoader
	{
		public EventsLoader(bool forStatic)
			: base(forStatic)
		{
		}
		public static TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack, TreeNodeObject parentNode)
		{
			if (parentNode.NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				EventPointer e = o as EventPointer;
				if (e != null)
				{
					for (int i = 0; i < parentNode.Nodes.Count; i++)
					{
						TreeNodeEvent ne = parentNode.Nodes[i] as TreeNodeEvent;
						if (ne != null)
						{
							if (string.CompareOrdinal(e.Name, ne.OwnerEvent.Name) == 0)
							{
								if (ownerStack.Count == 0)
								{
									return ne;
								}
								else
								{
									return ne.LocateObjectNode(ownerStack);
								}
							}
						}
					}
				}
			}
			return null;
		}
		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			bool forPhp = false;
			if (tv.Project != null)
			{
				forPhp = (tv.Project.ProjectType == EnumProjectType.WebAppPhp);
			}
			IObjectPointer objRef = parentNode.OwnerPointer;
			IRootNode root = parentNode.RootClassNode;
			TreeNodeClassRoot tr = parentNode.TopLevelRootClassNode;
			if (root == null)
			{
				if (parentNode != null)
				{
					root = parentNode.RootClassNode;
				}
			}
			if (tr == null)
			{
				if (parentNode != null)
				{
					if (tv != null)
					{
						tr = tv.RootClassNode;
					}
				}
			}
			SortedList<string, TreeNode> newNodes = new SortedList<string, TreeNode>();
			//
			EnumReflectionMemberInfoSelectScope scope;
			if (ForStatic)
				scope = EnumReflectionMemberInfoSelectScope.StaticOnly;
			else
				scope = EnumReflectionMemberInfoSelectScope.InstanceOnly;
			EventInfo[] eifs = null;
			IWebClientControl webc = objRef.ObjectInstance as IWebClientControl;
			if (webc != null)
			{
				eifs = webc.GetWebClientEvents(ForStatic);
			}
			else
			{
				if (objRef.ObjectInstance == null)
				{
					ClassPointer cp = objRef as ClassPointer;
					if (cp != null)
					{
						if (typeof(WebPage).IsAssignableFrom(cp.BaseClassType))
						{
							eifs = new EventInfo[] { };
						}
					}
				}
				if (eifs == null)
				{
					Type t = DesignUtil.GetTypeParameterValue(objRef);
					if (t != null)
					{
						eifs = VPLUtil.GetEvents(t, scope, false, true);
					}
				}
				if (eifs == null)
				{
					ClassInstancePointer cr0 = objRef.ObjectInstance as ClassInstancePointer;
					if (cr0 != null)
					{
						eifs = VPLUtil.GetEvents(cr0.ObjectType, scope, false, true);
					}
					else if (objRef.ObjectInstance == null || objRef.ObjectInstance is ClassInstancePointer)
					{
						eifs = VPLUtil.GetEvents(objRef.ObjectType, scope, false, true);
					}
					else
					{
						IDrawDesignControl dc = objRef.ObjectInstance as IDrawDesignControl;
						if (dc != null)
						{
							eifs = VPLUtil.GetEvents(dc.Item, scope, false, true);
						}
						else
						{
							eifs = VPLUtil.GetEvents(objRef.ObjectInstance, scope, false, true);
						}
					}
				}
			}
			if (eifs != null && eifs.Length > 0)
			{
				if (forPhp)
				{
					List<EventInfo> mifs2 = new List<EventInfo>();
					for (int k = 0; k < eifs.Length; k++)
					{
						object[] objs = eifs[k].GetCustomAttributes(typeof(WebClientMemberAttribute), true);
						if (objs != null && objs.Length > 0)
						{
							mifs2.Add(eifs[k]);
						}
						else
						{
							objs = eifs[k].GetCustomAttributes(typeof(WebServerMemberAttribute), true);
							if (objs != null && objs.Length > 0)
							{
								mifs2.Add(eifs[k]);
							}
						}
					}
					eifs = new EventInfo[mifs2.Count];
					mifs2.CopyTo(eifs, 0);
				}
				if (!tv.Project.IsWebApplication)
				{
					List<EventInfo> mifs2 = new List<EventInfo>();
					for (int k = 0; k < eifs.Length; k++)
					{
						object[] objs = eifs[k].GetCustomAttributes(typeof(WebClientOnlyAttribute), true);
						if (objs == null || objs.Length == 0)
						{
							mifs2.Add(eifs[k]);
						}
					}
					if (mifs2.Count < eifs.Length)
					{
						eifs = new EventInfo[mifs2.Count];
						mifs2.CopyTo(eifs, 0);
					}
				}
				for (int i = 0; i < eifs.Length; i++)
				{
					if (parentNode.IncludeEevnt(eifs[i].Name))
					{
						EventPointer epp = new EventPointer();
						epp.Owner = objRef;
						epp.SetEventInfo(eifs[i]);
						TreeNodeEvent nodeEv = new TreeNodeEvent(ForStatic, epp);
						if (tr != null && tr.HasEventHandler(epp))
						{
							nodeEv.OnShowActionIcon();
						}
						newNodes.Add(epp.MemberName, nodeEv);
					}
				}
				parentNode.AddSortedNodes(newNodes);
			}
		}
	}
	public class TreeNodeEventMethod : TreeNodeObject
	{
		public TreeNodeEventMethod(TreeViewObjectExplorer tv, bool isForStatic, EventHandlerMethod method)
			: base(isForStatic, method)
		{
			if (method.ForAllTypes)
				ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD_FORALL;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD;
			SelectedImageIndex = ImageIndex;
			Nodes.Add(new TreeNodeActionCollectionForMethod(tv, this, isForStatic, method, ScopeMethodId));
		}
		public override void ResetImage()
		{
			if (Method.ForAllTypes)
			{
				ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD_FORALL;
			}
			else
			{
				ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD;
			}
			SelectedImageIndex = ImageIndex;
		}
		[Browsable(false)]
		public bool ShowVariables { get; set; }
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{

			MethodClass mc = Method;
			loader.NotifySelection(mc);
			loader.DesignPane.PaneHolder.OnMethodSelected(mc);
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is IAction || pointer is MethodParamPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			return l;
		}
		public override bool IsTargeted()
		{
			return false;
		}
		public EventHandlerMethod Method
		{
			get
			{
				return (EventHandlerMethod)this.OwnerPointer;
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				IRootNode r = RootClassNode;
				if (r != null)
				{
					ClassPointer root = r.ClassData.RootClassID;
					MenuItem[] mi;
					if (root.IsWebPage)
					{
						mi = new MenuItem[4];
						mi[0] = new MenuItemWithBitmap("Edit client handler actions", Resources._handlerMethod.ToBitmap());
						mi[0].Click += new EventHandler(methodEdit_Click);
						mi[1] = new MenuItemWithBitmap("Edit server handler actions", Resources._webServerHandler.ToBitmap());
						mi[1].Click += new EventHandler(methodDelete_Click);
						mi[2] = new MenuItemWithBitmap("Edit client and download handler actions", Resources._webHandler2.ToBitmap());
						mi[2].Click += new EventHandler(methodEdit_Click);
						mi[3] = new MenuItemWithBitmap("Delete method", Resources._cancel.ToBitmap());
						mi[3].Click += new EventHandler(methodDelete_Click);
					}
					else
					{
						mi = new MenuItem[2];
						mi[0] = new MenuItemWithBitmap("Edit method", Resources._handlerMethod.ToBitmap());
						mi[0].Click += new EventHandler(methodEdit_Click);
						mi[1] = new MenuItemWithBitmap("Delete method", Resources._cancel.ToBitmap());
						mi[1].Click += new EventHandler(methodDelete_Click);
					}
					return mi;
				}
			}
			return null;
		}
		void methodEdit_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				if (Method.Edit(Method.ActionBranchId, tv.Parent.RectangleToScreen(this.Bounds), r.ViewersHolder.Loader, tv.FindForm()))
				{
					this.Text = Method.DisplayName;
				}
			}
		}
		void methodDelete_Click(object sender, EventArgs e)
		{
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				if (MessageBox.Show("Do you want to delete this method?", "Method", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
				{
					r.ClassData.RootClassID.DeleteMethod(Method);
				}
			}
		}
	}
	/// <summary>
	/// show attributes node for an object, which can be a root class or a component hosted by a root class.
	/// it can be for design or for selection when in scope. 
	/// It may be used in the following ways:
	/// 
	/// 1. For non-static class (owner: TreeNodeNonStaticClass)
	///     1.1. non-static PME (for design and selection in the scope) TreeNodeNonStaticPME
	///         Collection Nodes (non-static) for library (base-type) PME
	///             non-static lib PME nodes
	///         Collection Nodes (non-static) for custom developed PME
	///             non-static custom PME nodes
	/// 
	///     1.2. static PME (for design and selection) TreeNonStaticPME
	///         Collection Nodes (static) for library (base-type) PME
	///             static lib PME nodes
	///         Collection Nodes (static) for custom developed PME
	///             static custom PME nodes
	/// 
	/// 2. For static class (owner: TreeNodeStaticClass)
	///     The class is treated as a non-static class with following differences:
	///     a). it does not show static attributes node
	///     b). it shows non-static attributes using icons for static objects
	///     c). it does not show non-static lib PME
	///     d). when compiling, all members are treated static
	///     2.1. mixed PME (for design and selection) TreeNodeMixedPME
	///         Collection Nodes (static) for library (base-type) PME
	///             static lib PME nodes
	///         Collection Nodes (non-static) for custom developed PME (using icons for static objects)
	///             non-static custom PME nodes
	/// </summary>
	public abstract class TreeNodePME : TreeNodeObject
	{
		public TreeNodePME(bool isForStatic, IObjectPointer objectPointer)
			: base(isForStatic, objectPointer)
		{
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		public TreeNodeObjectCollection GetCollectionNode(IObjectIdentity op)
		{
			return TreeViewObjectExplorer.GetCollectionNode(this, op);
		}
		public TreeNodePropertyMixedCollection GetMixedPropertysNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodePropertyMixedCollection node = Nodes[i] as TreeNodePropertyMixedCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeCustomPropertyCollection GetCustomPropertiesNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomPropertyCollection node = Nodes[i] as TreeNodeCustomPropertyCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeCustomPropertyPointerCollection GetCustomPropertyPointersNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomPropertyPointerCollection node = Nodes[i] as TreeNodeCustomPropertyPointerCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeCustomMethodPointerCollection GetCustomMethodPointersNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomMethodPointerCollection node = Nodes[i] as TreeNodeCustomMethodPointerCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeCustomEventPointerCollection GetCustomEventPointersNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomEventPointerCollection node = Nodes[i] as TreeNodeCustomEventPointerCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeCustomMethodCollection GetCustomMethodsNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomMethodCollection node = Nodes[i] as TreeNodeCustomMethodCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeMethodMixedCollection GetMixMethodPointersNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeMethodMixedCollection node = Nodes[i] as TreeNodeMethodMixedCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeObjectCollection GetCustomEventsNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomEventCollection node = Nodes[i] as TreeNodeCustomEventCollection;
				if (node != null)
				{
					return node;
				}
				TreeNodeCustomEventPointerCollection node2 = Nodes[i] as TreeNodeCustomEventPointerCollection;
				if (node2 != null)
				{
					return node2;
				}
			}
			return null;
		}
		public TreeNodeEventCollection GetEventsNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeEventCollection node = Nodes[i] as TreeNodeEventCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public TreeNodeActionCollection GetActionsNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeActionCollection node = Nodes[i] as TreeNodeActionCollection;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		/// <summary>
		/// only a TreeNodeClass (ClassPointer,MemberComponentId, MemberComponentIdCustom) may have PME nodes. 
		/// MemberComponentId - ClassPointer + member id
		/// MemberComponentIdCustom - ClassPointer + ClassInstancePointer
		/// PME node is single leveled.
		/// Under PME-node, the hierarchy of TreeNode is the same as the IObjectIdentity.
		/// </summary>
		/// <param name="pointer"></param>
		/// <returns></returns>
		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			IObjectIdentity p = pointer;
			Stack<IObjectIdentity> pointerStack = new Stack<IObjectIdentity>();
			while (p != null && !(p is IClass))
			{
				pointerStack.Push(p);
				p = p.IdentityOwner;
			}
			if (pointerStack.Count > 0)
			{
				p = pointerStack.Pop();//p is the top level non-IClass 
				SetterPointer sp = p as SetterPointer;
				if (sp != null)
				{
					return FindObjectNode(sp.SetProperty);
				}
				TreeNodeObjectCollection toc = TreeViewObjectExplorer.GetCollectionNode(this, p);
				if (toc != null)
				{
					return toc.FindObjectNode(p, pointerStack);
				}
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
		public override bool IsPointerNode { get { return false; } }
		public TreeNodeEventCollection GetEventCollection()
		{
			LoadNextLevel();
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeEventCollection tna = Nodes[i] as TreeNodeEventCollection;
				if (tna != null)
				{
					return tna;
				}
			}
			return null;
		}
	}
	/// <summary>
	/// 
	/// Collection Nodes (non-static) for library (base-type) PME
	///             non-static lib PME nodes
	/// Collection Nodes (non-static) for custom developed PME
	///             non-static custom PME nodes
	/// </summary>
	public class TreeNodePMENonStatic : TreeNodePME
	{
		public TreeNodePMENonStatic(TreeViewObjectExplorer tv, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(false, objectPointer)
		{
			Text = "Instance Members";
			ImageIndex = TreeViewObjectExplorer.IMG_ATTRIBUTES;
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new TreeNodePropertyCollection(tv, this, false, objectPointer, scopeMethodId));
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action)
			{
				this.Nodes.Add(new TreeNodeMethodCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
			{
				this.Nodes.Add(new TreeNodeEventCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Action || tv.SelectionType == EnumObjectSelectType.Method)
			{
				this.Nodes.Add(new TreeNodeActionCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			this.Nodes.Add(new TreeNodeCustomPropertyCollection(tv, this, false, objectPointer, scopeMethodId));
			this.Nodes.Add(new TreeNodeCustomMethodCollection(tv, this, false, objectPointer, scopeMethodId));
			this.Nodes.Add(new TreeNodeCustomEventCollection(tv, this, false, objectPointer, scopeMethodId));
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				PropertyPointer p = o as PropertyPointer;
				if (p != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePropertyCollection n1 = Nodes[i] as TreeNodePropertyCollection;
						if (n1 != null)
						{
							return n1.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					MethodInfoPointer m = o as MethodInfoPointer;
					if (m != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeMethodCollection n2 = Nodes[i] as TreeNodeMethodCollection;
							if (n2 != null)
							{
								return n2.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						EventPointer e = o as EventPointer;
						if (e != null)
						{
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeEventCollection n3 = Nodes[i] as TreeNodeEventCollection;
								if (n3 != null)
								{
									return n3.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							PropertyClass pc = o as PropertyClass;
							if (pc != null)
							{
								for (int i = 0; i < Nodes.Count; i++)
								{
									TreeNodeCustomPropertyCollection n4 = Nodes[i] as TreeNodeCustomPropertyCollection;
									if (n4 != null)
									{
										return n4.LocateObjectNode(ownerStack);
									}
								}
							}
							else
							{
								MethodClass mc = o as MethodClass;
								if (mc != null)
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodeCustomMethodCollection n5 = Nodes[i] as TreeNodeCustomMethodCollection;
										if (n5 != null)
										{
											return n5.LocateObjectNode(ownerStack);
										}
									}
								}
								else
								{
									EventClass ec = o as EventClass;
									if (ec != null)
									{
										for (int i = 0; i < Nodes.Count; i++)
										{
											TreeNodeCustomEventCollection n6 = Nodes[i] as TreeNodeCustomEventCollection;
											if (n6 != null)
											{
												return n6.LocateObjectNode(ownerStack);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
	}
	public class TreeNodePMEStatic : TreeNodePME
	{
		public TreeNodePMEStatic(TreeViewObjectExplorer tv, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(true, objectPointer)
		{
			Text = "Static Members";
			ImageIndex = TreeViewObjectExplorer.IMG_S_ATTRIBUTES;
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new TreeNodePropertyCollection(tv, this, true, objectPointer, scopeMethodId));
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action)
			{
				this.Nodes.Add(new TreeNodeMethodCollection(tv, this, true, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
			{
				this.Nodes.Add(new TreeNodeEventCollection(tv, this, true, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Action || tv.SelectionType == EnumObjectSelectType.Method)
			{
				this.Nodes.Add(new TreeNodeActionCollection(tv, this, true, objectPointer, scopeMethodId));
			}
			this.Nodes.Add(new TreeNodeCustomPropertyCollection(tv, this, true, objectPointer, scopeMethodId));
			this.Nodes.Add(new TreeNodeCustomMethodCollection(tv, this, true, objectPointer, scopeMethodId));
			this.Nodes.Add(new TreeNodeCustomEventCollection(tv, this, true, objectPointer, scopeMethodId));
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				PropertyPointer p = o as PropertyPointer;
				if (p != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePropertyCollection n1 = Nodes[i] as TreeNodePropertyCollection;
						if (n1 != null)
						{
							return n1.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					MethodInfoPointer m = o as MethodInfoPointer;
					if (m != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeMethodCollection n2 = Nodes[i] as TreeNodeMethodCollection;
							if (n2 != null)
							{
								return n2.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						EventPointer e = o as EventPointer;
						if (e != null)
						{
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeEventCollection n3 = Nodes[i] as TreeNodeEventCollection;
								if (n3 != null)
								{
									return n3.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							PropertyClass pc = o as PropertyClass;
							if (pc != null)
							{
								for (int i = 0; i < Nodes.Count; i++)
								{
									TreeNodeCustomPropertyCollection n4 = Nodes[i] as TreeNodeCustomPropertyCollection;
									if (n4 != null)
									{
										return n4.LocateObjectNode(ownerStack);
									}
								}
							}
							else
							{
								MethodClass mc = o as MethodClass;
								if (mc != null)
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodeCustomMethodCollection n5 = Nodes[i] as TreeNodeCustomMethodCollection;
										if (n5 != null)
										{
											return n5.LocateObjectNode(ownerStack);
										}
									}
								}
								else
								{
									EventClass ec = o as EventClass;
									if (ec != null)
									{
										for (int i = 0; i < Nodes.Count; i++)
										{
											TreeNodeCustomEventCollection n6 = Nodes[i] as TreeNodeCustomEventCollection;
											if (n6 != null)
											{
												return n6.LocateObjectNode(ownerStack);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
	}
	/// <summary>
	///     2.1. mixed PME (for design and selection) TreeNodeMixedPME
	///         Collection Nodes (non-static) for custom developed PME (using icons for static objects)
	///             PME nodes for both non-static custom and non-static library PME
	/// </summary>
	public class TreeNodePMEMixed : TreeNodePME
	{
		public TreeNodePMEMixed(TreeViewObjectExplorer tv, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(true, objectPointer)
		{
			Text = "Static Members";
			ImageIndex = TreeViewObjectExplorer.IMG_S_ATTRIBUTES;
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new TreeNodePropertyMixedCollection(tv, this, objectPointer, scopeMethodId));
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action || (tv.SelectionType == EnumObjectSelectType.Object && tv.SelectionTypeScope != null && tv.SelectionTypeScope.BaseClassType != null && typeof(Delegate).IsAssignableFrom(tv.SelectionTypeScope.BaseClassType)))
			{
				this.Nodes.Add(new TreeNodeMethodMixedCollection(tv, this, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
			{
				this.Nodes.Add(new TreeNodeEventMixedCollection(tv, this, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Action || tv.SelectionType == EnumObjectSelectType.Method)
			{
				this.Nodes.Add(new TreeNodeActionMixedCollection(tv, this, objectPointer, scopeMethodId));
			}
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				PropertyPointer p = o as PropertyPointer;
				if (p != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePropertyCollection n1 = Nodes[i] as TreeNodePropertyCollection;
						if (n1 != null)
						{
							return n1.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					MethodInfoPointer m = o as MethodInfoPointer;
					if (m != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeMethodCollection n2 = Nodes[i] as TreeNodeMethodCollection;
							if (n2 != null)
							{
								return n2.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						EventPointer e = o as EventPointer;
						if (e != null)
						{
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeEventCollection n3 = Nodes[i] as TreeNodeEventCollection;
								if (n3 != null)
								{
									return n3.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							PropertyClass pc = o as PropertyClass;
							if (pc != null)
							{
								for (int i = 0; i < Nodes.Count; i++)
								{
									TreeNodeCustomPropertyCollection n4 = Nodes[i] as TreeNodeCustomPropertyCollection;
									if (n4 != null)
									{
										return n4.LocateObjectNode(ownerStack);
									}
								}
							}
							else
							{
								MethodClass mc = o as MethodClass;
								if (mc != null)
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodeCustomMethodCollection n5 = Nodes[i] as TreeNodeCustomMethodCollection;
										if (n5 != null)
										{
											return n5.LocateObjectNode(ownerStack);
										}
									}
								}
								else
								{
									EventClass ec = o as EventClass;
									if (ec != null)
									{
										for (int i = 0; i < Nodes.Count; i++)
										{
											TreeNodeCustomEventCollection n6 = Nodes[i] as TreeNodeCustomEventCollection;
											if (n6 != null)
											{
												return n6.LocateObjectNode(ownerStack);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
	}
	/// <summary>
	/// used by TreeNodeLocalVariable to show lib PME and custom PME
	/// </summary>
	public class TreeNodePMELocalVariable : TreeNodePME
	{
		public TreeNodePMELocalVariable(TreeViewObjectExplorer tv, LocalVariable objectPointer, UInt32 scopeMethodId)
			: base(false, objectPointer)
		{
			Text = "Instance Members";
			ImageIndex = TreeViewObjectExplorer.IMG_ATTRIBUTES;
			SelectedImageIndex = ImageIndex;
			//
			Nodes.Add(new TreeNodePropertyCollection(tv, this, false, objectPointer, scopeMethodId));
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action)
			{
				this.Nodes.Add(new TreeNodeMethodCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
			{
				this.Nodes.Add(new TreeNodeEventCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Action || tv.SelectionType == EnumObjectSelectType.Method)
			{
				this.Nodes.Add(new TreeNodeActionCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (objectPointer.ObjectInstance is ClassInstancePointer || objectPointer is LocalVariable)
			{
				this.Nodes.Add(new TreeNodeCustomPropertyPointerCollection(tv, this, false, objectPointer, scopeMethodId));
				if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action)
				{
					this.Nodes.Add(new TreeNodeCustomMethodPointerCollection(tv, this, false, objectPointer, scopeMethodId));
				}
				if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
				{
					this.Nodes.Add(new TreeNodeCustomEventPointerCollection(tv, this, false, objectPointer, scopeMethodId));
				}
			}
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				PropertyPointer p = o as PropertyPointer;
				if (p != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePropertyCollection n1 = Nodes[i] as TreeNodePropertyCollection;
						if (n1 != null)
						{
							return n1.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					MethodInfoPointer m = o as MethodInfoPointer;
					if (m != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeMethodCollection n2 = Nodes[i] as TreeNodeMethodCollection;
							if (n2 != null)
							{
								return n2.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						EventPointer e = o as EventPointer;
						if (e != null)
						{
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeEventCollection n3 = Nodes[i] as TreeNodeEventCollection;
								if (n3 != null)
								{
									return n3.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							PropertyClass pc = o as PropertyClass;
							if (pc != null)
							{
								for (int i = 0; i < Nodes.Count; i++)
								{
									TreeNodeCustomPropertyCollection n4 = Nodes[i] as TreeNodeCustomPropertyCollection;
									if (n4 != null)
									{
										return n4.LocateObjectNode(ownerStack);
									}
								}
							}
							else
							{
								MethodClass mc = o as MethodClass;
								if (mc != null)
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodeCustomMethodCollection n5 = Nodes[i] as TreeNodeCustomMethodCollection;
										if (n5 != null)
										{
											return n5.LocateObjectNode(ownerStack);
										}
									}
								}
								else
								{
									EventClass ec = o as EventClass;
									if (ec != null)
									{
										for (int i = 0; i < Nodes.Count; i++)
										{
											TreeNodeCustomEventCollection n6 = Nodes[i] as TreeNodeCustomEventCollection;
											if (n6 != null)
											{
												return n6.LocateObjectNode(ownerStack);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
	}
	/// <summary>
	/// used by TreeNodeClassComponent to show lib PME and custom PME
	/// </summary>
	public class TreeNodePMEPointer : TreeNodePME
	{
		public TreeNodePMEPointer(TreeViewObjectExplorer tv, MemberComponentId objectPointer, UInt32 scopeMethodId)
			: base(false, objectPointer)
		{
			Text = "Instance Members";
			ImageIndex = TreeViewObjectExplorer.IMG_ATTRIBUTES;
			SelectedImageIndex = ImageIndex;
			//
			Nodes.Add(new TreeNodePropertyCollection(tv, this, false, objectPointer, scopeMethodId));
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action || (tv.SelectionType == EnumObjectSelectType.Object && tv.SelectionTypeScope != null && tv.SelectionTypeScope.BaseClassType != null && typeof(Delegate).IsAssignableFrom(tv.SelectionTypeScope.BaseClassType)))
			{
				this.Nodes.Add(new TreeNodeMethodCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
			{
				this.Nodes.Add(new TreeNodeEventCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Action || tv.SelectionType == EnumObjectSelectType.Method)
			{
				this.Nodes.Add(new TreeNodeActionCollection(tv, this, false, objectPointer, scopeMethodId));
			}
			ClassInstancePointer cip = objectPointer.ObjectInstance as ClassInstancePointer;
			if (cip != null)
			{
				this.Nodes.Add(new TreeNodeCustomPropertyPointerCollection(tv, this, false, objectPointer, scopeMethodId));
				if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action || (tv.SelectionType == EnumObjectSelectType.Object && tv.SelectionTypeScope != null && tv.SelectionTypeScope.BaseClassType != null && typeof(Delegate).IsAssignableFrom(tv.SelectionTypeScope.BaseClassType)))
				{
					this.Nodes.Add(new TreeNodeCustomMethodPointerCollection(tv, this, false, objectPointer, scopeMethodId));
				}
				if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope) || ((tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action) && typeof(WebPage).IsAssignableFrom(cip.ObjectType)))
				{
					this.Nodes.Add(new TreeNodeCustomEventPointerCollection(tv, this, false, objectPointer, scopeMethodId));
				}
			}
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				PropertyPointer p = o as PropertyPointer;
				if (p != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePropertyCollection n1 = Nodes[i] as TreeNodePropertyCollection;
						if (n1 != null)
						{
							return n1.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					MethodInfoPointer m = o as MethodInfoPointer;
					if (m != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeMethodCollection n2 = Nodes[i] as TreeNodeMethodCollection;
							if (n2 != null)
							{
								return n2.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						EventPointer e = o as EventPointer;
						if (e != null)
						{
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeEventCollection n3 = Nodes[i] as TreeNodeEventCollection;
								if (n3 != null)
								{
									return n3.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							PropertyClass pc = o as PropertyClass;
							if (pc != null)
							{
								for (int i = 0; i < Nodes.Count; i++)
								{
									TreeNodeCustomPropertyCollection n4 = Nodes[i] as TreeNodeCustomPropertyCollection;
									if (n4 != null)
									{
										return n4.LocateObjectNode(ownerStack);
									}
								}
							}
							else
							{
								MethodClass mc = o as MethodClass;
								if (mc != null)
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodeCustomMethodCollection n5 = Nodes[i] as TreeNodeCustomMethodCollection;
										if (n5 != null)
										{
											return n5.LocateObjectNode(ownerStack);
										}
									}
								}
								else
								{
									EventClass ec = o as EventClass;
									if (ec != null)
									{
										for (int i = 0; i < Nodes.Count; i++)
										{
											TreeNodeCustomEventCollection n6 = Nodes[i] as TreeNodeCustomEventCollection;
											if (n6 != null)
											{
												return n6.LocateObjectNode(ownerStack);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
	}
	public class TreeNodePMEPointerStatic : TreeNodePME
	{
		public TreeNodePMEPointerStatic(TreeViewObjectExplorer tv, MemberComponentId objectPointer, UInt32 scopeMethodId)
			: base(true, objectPointer)
		{
			Text = "Static Members";
			ImageIndex = TreeViewObjectExplorer.IMG_S_ATTRIBUTES;
			SelectedImageIndex = ImageIndex;
			//
			Nodes.Add(new TreeNodePropertyCollection(tv, this, true, objectPointer, scopeMethodId));
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action)
			{
				this.Nodes.Add(new TreeNodeMethodCollection(tv, this, true, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
			{
				this.Nodes.Add(new TreeNodeEventCollection(tv, this, true, objectPointer, scopeMethodId));
			}
			if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Action || tv.SelectionType == EnumObjectSelectType.Method)
			{
				this.Nodes.Add(new TreeNodeActionCollection(tv, this, true, objectPointer, scopeMethodId));
			}
			if (objectPointer.ObjectInstance is ClassInstancePointer)
			{
				this.Nodes.Add(new TreeNodeCustomPropertyPointerCollection(tv, this, true, objectPointer, scopeMethodId));
				if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Method || tv.SelectionType == EnumObjectSelectType.Action)
				{
					this.Nodes.Add(new TreeNodeCustomMethodPointerCollection(tv, this, true, objectPointer, scopeMethodId));
				}
				if (tv.SelectionType == EnumObjectSelectType.All || tv.SelectionType == EnumObjectSelectType.Event || !string.IsNullOrEmpty(tv.SelectionEventScope))
				{
					this.Nodes.Add(new TreeNodeCustomEventPointerCollection(tv, this, true, objectPointer, scopeMethodId));
				}
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
	}

	/// <summary>
	/// Static class 
	/// 1. Do not show static nodes. 
	/// 2. Instance-nodes are displayed with icons for static nodes
	/// 3. Show Custom nodes, displayed with icons for static nodes
	///
	/// Non-static class	
	/// 1. Show instance nodes. Instance-nodes are displayed with icons for instance nodes
	/// 2. Show static nodes
	/// 3. Show Custom nodes, static and instance.
	///
	/// Static class for selection
	/// 1. like "static class"
	/// 2. if it is not the main object then do not show components
	///
	/// Non-static class for selection
	/// 1. like "Non-static class" but not showing instance node
	/// 2. if it is not the main object then do not show components
	/// </summary>
	public abstract class TreeNodeClassRoot : TreeNodeClass, IRootNode
	{
		private MultiPanes _panes;
		private ILimnorDesignerLoader _loader;
		private bool _isMainNode;
		private RootComponentData _data;
		protected ClassInstancePointer[] _instances;
		public TreeNodeClassRoot(TreeViewObjectExplorer tv, bool isMainNode, ClassPointer rd, bool staticScope)//(bool isForStatic, bool isMainNode, XmlNode xml, LimnorProject prj, ObjectIDmap map, IObjectPointer objectPointer, EnumObjectSelectType target, Type scope, string eventName)
			: base(rd.IsStatic, rd)
		{
			StaticScope = staticScope;
			_isMainNode = isMainNode;
			_data = new RootComponentData(isMainNode, rd);
			//
			ImageIndex = TreeViewObjectExplorer.GetTypeIcon(rd);
			SelectedImageIndex = ImageIndex;
			//
			ResetNextLevel(tv);
			//
		}
		public bool StaticScope { get; set; }
		public bool IsWebService
		{
			get
			{
				return _data.RootClassID.IsWebService;
			}
		}
		public override UInt32 ClassId
		{
			get
			{
				return _data.RootClassID.ClassId;
			}
		}
		public override UInt32 MemberId
		{
			get
			{
				return _data.RootClassID.MemberId;
			}
		}
		public override IClass Holder
		{
			get { return _data.RootClassID; }
		}
		public ILimnorDesignerLoader DesignerLoader
		{
			get
			{
				return _loader;
			}
		}
		public void ResetPointer(ClassPointer pointer)
		{
			_data.ResetPointer(pointer);
			this.ResetObjectPointer(pointer);
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			this.Nodes.Clear();
			this.NextLevelLoaded = false;
			UInt32 scopeId = ScopeMethodId;
			if (this.SelectionTarget == EnumObjectSelectType.All)
			{
				Nodes.Add(new TreeNodeAttributeCollection(tv, this, (IAttributeHolder)this.OwnerPointer, scopeId));
			}
		}
		public override void RefreshIcon(UInt32 classId)
		{
			base.RefreshIcon(classId);
			if (NextLevelLoaded)
			{
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassComponentCust c = Nodes[i] as TreeNodeClassComponentCust;
					if (c != null)
					{
						c.RefreshIcon(classId);
					}
				}
			}
		}
		public TreeNodeClass FindContainerNode(object obj)
		{
			ToolStripItem tsi = obj as ToolStripItem;
			if (tsi != null)
			{
				ToolStrip ts = Tree.GetTopMenuOwner(tsi);
				if (ts != null)
				{
					foreach (TreeNode tn in Nodes)
					{
						TreeNodeClassComponent tncc = tn as TreeNodeClassComponent;
						if (tncc != null)
						{
							if (tncc.OwnerPointer.ObjectInstance == ts)
							{
								if (tsi.OwnerItem == null)
								{
									return tncc;
								}
								else
								{
									return tncc.FindNodeByObject(tsi.OwnerItem);
								}
							}
						}
					}
				}
			}
			else
			{
				Control r = this.OwnerPointer.ObjectInstance as Control;
				Control c = obj as Control;
				if (c != null)
				{
					if (c.Parent != null)
					{
						if (c.Parent != r)
						{
							foreach (TreeNode tn in Nodes)
							{
								TreeNodeClassComponent tncc = tn as TreeNodeClassComponent;
								if (tncc != null)
								{
									if (tncc.OwnerPointer.ObjectInstance == c.Parent)
									{
										return tncc;
									}
									else
									{
										TreeNodeClassComponent tc = tncc.FindNodeByObject(c.Parent);
										if (tc != null)
										{
											return tc;
										}
									}
								}
							}
						}
					}
				}
			}
			return this;
		}
		public TypePointerCollection GetTypePointerCollection()
		{
			TypePointerCollection tps = ClassData.ObjectMap.GetTypedData<TypePointerCollection>();
			if (tps == null)
			{
				tps = new TypePointerCollection();
				XmlNodeList nodes = SerializeUtil.GetExternalTypes(ClassData.ObjectMap.XmlData);
				foreach (XmlNode nd in nodes)
				{
					tps.Add(new TypePointer(XmlUtil.GetLibTypeAttribute(nd)));
				}
				ClassData.ObjectMap.SetTypedData<TypePointerCollection>(tps);
			}
			if (ClassData.RootClassID.IsWebPage)
			{
				TypePointer wtp = new TypePointer(typeof(WebMessageBox), ClassData.RootClassID);
				foreach (TypePointer tp in tps)
				{
					if (tp.IsSameObjectRef(wtp))
					{
						wtp = null;
						break;
					}
				}
				if (wtp != null)
				{
					tps.Add(wtp);
				}
			}
			return tps;
		}
		public void OnAddExternalType(UInt32 classId, Type t)
		{
			if (classId == ClassData.RootClassID.ClassId)
			{
				TypePointerCollection tps = GetTypePointerCollection();
				foreach (TypePointer tp in tps)
				{
					if (t.Equals(tp.ClassType))
					{
						return;
					}
				}
				tps.Add(new TypePointer(t));
			}
		}
		public void AddExternalType(Type t)
		{
			//SerializeUtil.AddExternalTypeNode(this.ViewersHolder.Loader.Node, t);
			TypePointerCollection tps = GetTypePointerCollection();
			bool bFound = false;
			foreach (TypePointer tp in tps)
			{
				if (t.Equals(tp.ClassType))
				{
					bFound = true;
					break;
				}
			}
			if (!bFound)
			{
				tps.Add(new TypePointer(t));
			}
			this.ViewersHolder.Loader.NotifyChanges();
			if (ViewersHolder != null)
			{
				ViewersHolder.OnAddExternalType(ViewersHolder.Loader.ClassID, t);
			}
		}
		public static bool TypeNodeExists(TreeViewObjectExplorer tv, Type t)
		{
			for (int i = 0; i < tv.Nodes.Count; i++)
			{
				TreeNodeClassType tnct = tv.Nodes[i] as TreeNodeClassType;
				if (tnct != null)
				{
					if (t.Equals(tnct.OwnerDataType))
					{
						return true;
					}
				}
			}
			return false;
		}
		public List<Type> LoadExternalTypes()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				bool bResManloaded = false;
				if (tv.RootId != null && tv.RootId.Interface == null)
				{
					if (!TypeNodeExists(tv, typeof(ProjectResources)))
					{
						TypePointer tp = new TypePointer(typeof(ProjectResources));
						tp.Owner = this.OwnerPointer;
						TreeNodeClassType tn = new TreeNodeClassType(tv, tp, _data, ScopeMethodId);
						tn.CanRemove = false;
						tv.Nodes.Add(tn);
					}
					bResManloaded = true;
				}
				List<Type> loadedTypes = new List<Type>();
				TypePointerCollection tps = GetTypePointerCollection();
				foreach (TypePointer tp in tps)
				{
					tp.Owner = this.OwnerPointer;
					if (typeof(ProjectResources).Equals(tp.VariableLibType))
					{
						if (bResManloaded)
						{
							continue;
						}
					}
					if (TypeNodeExists(tv, tp.ClassType))
					{
						continue;
					}
					if (loadedTypes.Contains(tp.ClassType))
					{
						continue;
					}
					TreeNodeClassType tn = new TreeNodeClassType(tv, tp, _data, ScopeMethodId);
					tv.Nodes.Add(tn);
					tn.CanRemove = true;
					loadedTypes.Add(tp.ClassType);
				}

				XmlNodeList nds = _data.RootClassID.XmlData.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					"Types/{0}", XmlTags.XML_Item));
				foreach (XmlNode nd in nds)
				{
					Type t = XmlUtil.GetLibTypeAttribute(nd);
					if (t != null && !typeof(ClassPointerX).Equals(t.BaseType))
					{
						bool b = false;
						foreach (Type t0 in loadedTypes)
						{
							if (VPLUtil.IsSameType(t0, t))
							{
								b = true;
								break;
							}
						}
						if (!b)
						{
							if (!TypeNodeExists(tv, t))
							{
								TypePointer tp = new TypePointer(t);
								tp.Owner = this.OwnerPointer;
								TreeNodeClassType tn = new TreeNodeClassType(tv, tp, _data, ScopeMethodId);
								tv.Nodes.Add(tn);
								tn.CanRemove = true;
							}
							loadedTypes.Add(t);
						}
					}
				}
				return loadedTypes;
			}
			return null;
		}
		/// <summary>
		/// action created or modified (name changed).
		/// </summary>
		/// <param name="act"></param>
		/// <param name="isNewAction">false: name changed</param>
		public override void OnActionChanged(UInt32 classId, IAction act, bool isNewAction)
		{
			if (classId == _data.RootClassID.ClassId)
			{
				if (!_data.RootClassID.InBatchSaving)
				{
					_data.RootClassID.ReloadEventActions(act);
				}
				//
				TreeNodeOverridesInterfaces tnOI = null;
				for (int i = 0; i < Nodes.Count; i++)
				{
					tnOI = Nodes[i] as TreeNodeOverridesInterfaces;
					if (tnOI != null)
					{
						tnOI.OnActionChanged(classId, act, isNewAction);
						break;
					}
				}
				//show it in Actions Node
				TreeNodePME pmeNode = this.GetMemberNode(act.IsStatic);
				if (pmeNode != null)
				{
					TreeNodeActionCollection actNodes = pmeNode.GetActionsNode();
					if (actNodes != null)
					{
						//actNodes.ResetNextLevel();
						if (actNodes.NextLevelLoaded)
						{
							TreeNodeAction an = null;
							for (int k = 0; k < actNodes.Nodes.Count; k++)
							{
								TreeNodeAction an0 = actNodes.Nodes[k] as TreeNodeAction;
								if (an0 != null && an0.Action != null)
								{
									if (an0.Action.ActionId == act.ActionId)
									{
										an = an0;
										an.ShowText();
										break;
									}
								}
							}
							if (an == null)
							{
								an = new TreeNodeAction(act.IsStatic, act);
								actNodes.Nodes.Add(an);
							}
						}
						actNodes.OnShowActionIcon();
					}
				}
				if (act.ExecuterClassId != 0)
				{
					ITreeNodeClass classNode = this.GetClassTreeNode(act.ExecuterClassId);
					if (classNode != null)
					{
						classNode.HandleActionModified(classId, act, isNewAction);
					}
				}
				else
				{
					//exeuting a static method from a lib type
					IObjectPointer op = act.ActionMethod as IObjectPointer;
					if (op != null)
					{
						DataTypePointer tp = null;
						IObjectPointer p = op.Owner;
						while (p != null)
						{
							tp = p as DataTypePointer;
							if (tp != null)
							{
								break;
							}
							p = p.Owner;
						}
						if (tp != null)
						{
							TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
							if (tv != null)
							{
								ITypeNode clsNode = null;
								TreeNodeNamespaceList nsList = null;
								for (int i = 0; i < tv.Nodes.Count; i++)
								{
									ITypeNode classNode = tv.Nodes[i] as ITypeNode;
									if (classNode != null)
									{
										if (classNode.OwnerDataType.Equals(tp.BaseClassType))
										{
											clsNode = classNode;
											break;
										}
									}
									else
									{
										if (nsList == null)
										{
											nsList = tv.Nodes[i] as TreeNodeNamespaceList;
										}
									}
								}
								if (clsNode == null)
								{
									if (nsList != null)
									{
										nsList.LoadNextLevel();
										for (int i = 0; i < nsList.Nodes.Count; i++)
										{
											TreeNodeNamespace nsNode = nsList.Nodes[i] as TreeNodeNamespace;
											if (nsNode != null)
											{
												NamespaceClass nsc = nsNode.Namespace;
												if (nsc.Namespace == tp.BaseClassType.Namespace)
												{
													nsNode.LoadNextLevel();
													for (int j = 0; j < nsNode.Nodes.Count; j++)
													{
														ITypeNode classNode = nsNode.Nodes[j] as ITypeNode;
														if (classNode != null)
														{
															if (classNode.OwnerDataType.Equals(tp.BaseClassType))
															{
																clsNode = classNode;
																break;
															}
														}
													}
													break;
												}
											}
										}
									}
								}
								if (clsNode != null)
								{
									clsNode.HandleActionModified(classId, act, isNewAction);
								}
								else
								{
									bool bIncl = true;
									if (act.ScopeMethodId != 0)
									{
										if (act.ScopeMethodId != tv.ScopeMethodId)
										{
											bIncl = false;
										}
									}
									if (bIncl)
									{
										if (act.ActionMethod.ObjectDevelopType == EnumObjectDevelopType.Library && act.ActionMethod.IsStatic)
										{
											//create a new type-node
											if (tv.RootClassNode != null && tv.RootClassNode.ClassData != null && tv.RootClassNode.ClassData.XmlData != null)
											{
												TypePointerCollection tpc = tv.RootId.ObjectList.GetTypedData<TypePointerCollection>();
												if (tpc == null)
												{
													tpc = new TypePointerCollection();
													tv.RootId.ObjectList.SetTypedData<TypePointerCollection>(tpc);
												}
												bool bFound = false;
												foreach (TypePointer t in tpc)
												{
													if (t.IsSameObjectRef(tp.LibTypePointer))
													{
														bFound = true;
														break;
													}
												}
												if (!bFound)
												{
													tpc.Add(tp.LibTypePointer);
												}
												SerializeUtil.AddExternalTypeNode(tv.RootClassNode.ClassData.XmlData, tp.BaseClassType);
												TreeNodeClassType tnct = new TreeNodeClassType(tv, tp.BaseClassType, tv.ScopeMethodId);
												tnct.SetNodeIcon();
												tv.Nodes.Add(tnct);
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
		/// <summary>
		/// do not need to handle Actions node.
		/// </summary>
		/// <param name="act"></param>
		/// <param name="isNewAction"></param>
		public override void HandleActionModified(UInt32 classId, IAction act, bool isNewAction)
		{
			if (act.ExecuterClassId == this.ClassData.RootClassID.ClassId)
			{
				//find class node by member Id
				this.LoadNextLevel();
				ITreeNodeClass tnc = GetComponentTreeNodeByMemberId(act.ExecuterMemberId);
				if (tnc != null)
				{
					//locate members node
					TreeNodePME pmeNode = tnc.GetMemberNode(act.IsStatic);
					if (pmeNode != null)
					{
						int nCount = 0; //only 2 possible: ActionCollection and (Property or Method)
						pmeNode.LoadNextLevel();
						if (isNewAction)
						{
							for (int i = 0; i < pmeNode.Nodes.Count; i++)
							{
								TreeNodeObject objNode = pmeNode.Nodes[i] as TreeNodeObject;
								if (objNode != null)
								{
									//locate the action node
									TreeNodeAction na = objNode.FindActionNode(act);
									if (na != null)
									{
									}
								}
							}
						}
						else
						{
							for (int i = 0; i < pmeNode.Nodes.Count; i++)
							{
								TreeNodeObject objNode = pmeNode.Nodes[i] as TreeNodeObject;
								if (objNode != null)
								{
									//locate the action node
									TreeNodeAction na = objNode.FindActionNode(act);
									if (na != null)
									{
										na.Text = act.ActionName;
										nCount++;
										if (nCount > 1)
										{
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
		public void SetDesigner(ILimnorDesignerLoader loader)
		{
			_loader = loader;
			_panes = _loader.DesignPane.Window as MultiPanes;
		}
		public TreeNodeCustomProperty GetCustomPropertyNode(PropertyClass pc)
		{
			TreeNodePME tna = GetMemberNode(pc.IsStatic);
			if (tna != null)
			{
				TreeNodeCustomPropertyCollection cpc = tna.GetCustomPropertiesNode();
				if (cpc != null)
				{
					return cpc.GetCustomPropertyNode(pc.MemberId);
				}
			}
			return null;
		}
		public TreeNodeCustomMethod GetCustomMethdNode(MethodClass mc)
		{
			TreeNodePME tna = GetMemberNode(mc.IsStatic);
			if (tna != null)
			{
				TreeNodeCustomMethodCollection cpc = tna.GetCustomMethodsNode();
				if (cpc != null)
				{
					return cpc.LocateMethodNode(mc.MemberId);
				}
			}
			return null;
		}
		public TreeNodeCustomEvent GetCustomEventNode(EventClass pc)
		{
			if (pc != null)
			{
				TreeNodePME tna = GetMemberNode(pc.IsStatic);
				if (tna != null)
				{
					TreeNodeCustomEventCollection cpc = tna.GetCustomEventsNode() as TreeNodeCustomEventCollection;
					if (cpc != null)
					{
						return cpc.GetCustomEventNode(pc.MemberId);
					}
				}
			}
			return null;
		}
		public TreeNodeOverridesInterfaces GetOverridesInterfacesNode()
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeOverridesInterfaces nd = Nodes[i] as TreeNodeOverridesInterfaces;
				if (nd != null)
				{
					return nd;
				}
			}
			return null;
		}
		public TreeNodeObject GetClassRefPropertyNode(PropertyClass pc)
		{
			TreeNodeClassComponent dc = GetComponentTreeNode(pc.ClassId) as TreeNodeClassComponent;
			if (dc != null && dc.NextLevelLoaded)
			{
				TreeNodePME ta = dc.GetMemberNode(pc.IsStatic);
				if (ta != null && ta.NextLevelLoaded)
				{
					return ta.FindObjectNode(pc);
				}
			}
			return null;
		}
		public void ReloadClassRefProperties(ClassInstancePointer cr)
		{
			if (_loader != null)
			{
				_loader.ReloadClassRefProperties(cr);
			}
		}
		public override void ResetPropertyNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			TreeNodePME na = GetMemberNode(forStatic);
			if (na != null)
			{
				if (na.NextLevelLoaded)
				{
					for (int i = 0; i < na.Nodes.Count; i++)
					{
						if (forCustom)
						{
							TreeNodeCustomPropertyCollection pc = na.Nodes[i] as TreeNodeCustomPropertyCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
						else
						{
							TreeNodePropertyCollection pc = na.Nodes[i] as TreeNodePropertyCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
					}
				}
			}
		}
		public override void ResetMethodNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			TreeNodePME na = GetMemberNode(forStatic);
			if (na != null)
			{
				if (na.NextLevelLoaded)
				{
					for (int i = 0; i < na.Nodes.Count; i++)
					{
						if (forCustom)
						{
							TreeNodeCustomMethodCollection pc = na.Nodes[i] as TreeNodeCustomMethodCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
						else
						{
							TreeNodeMethodCollection pc = na.Nodes[i] as TreeNodeMethodCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
					}
				}
			}
		}
		public override void ResetEventNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			TreeNodePME na = GetMemberNode(forStatic);
			if (na != null)
			{
				if (na.NextLevelLoaded)
				{
					for (int i = 0; i < na.Nodes.Count; i++)
					{
						if (forCustom)
						{
							TreeNodeCustomEventCollection pc = na.Nodes[i] as TreeNodeCustomEventCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
						else
						{
							TreeNodeEventCollection pc = na.Nodes[i] as TreeNodeEventCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
					}
				}
			}
		}
		public MultiPanes ViewersHolder
		{
			get
			{
				if (_panes != null)
				{
					return _panes;
				}
				if (this.ClassData != null && ClassData.DesignerHolder != null)
				{
					return ClassData.DesignerHolder;
				}
				return null;
			}
		}
		public void NotifyChanges()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null && !tv.ReadOnly)
			{
				if (_loader != null)
				{
					_loader.NotifyChanges();
				}
				else if (this.ClassData != null && ClassData.DesignerHolder != null)
				{
					ClassData.DesignerHolder.Loader.NotifyChanges();
				}
			}
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader(this.IsStatic));
			return lst;
		}
		public RootComponentData ClassData
		{
			get
			{
				return _data;
			}
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				ClassPointer cp = this.OwnerPointer as ClassPointer;
				if (!cp.IsStatic)
				{
					if (
						tv.SelectionType == EnumObjectSelectType.Type
						|| tv.SelectionType == EnumObjectSelectType.BaseClass
						|| tv.SelectionType == EnumObjectSelectType.InstanceType
						|| tv.SelectionType == EnumObjectSelectType.Object
						)
					{
						if (this.TargetScope == null)
						{
							return true;
						}
						else
						{
							if (TargetScope.IsAssignableFrom(new DataTypePointer(cp)))
							{
								return true;
							}
							return false;
						}
					}
					else if (tv.SelectionType == EnumObjectSelectType.Interface)
					{
						return cp.IsInterface;
					}
				}
			}
			return false;
		}
		/// <summary>
		/// the component as the current scope
		/// </summary>
		public bool IsMainRoot
		{
			get
			{
				return _isMainNode;
			}
		}
		/// <summary>
		/// for out of scope components may only display static members
		/// </summary>
		public override bool IncludeStaticOnly
		{
			get
			{
				if (IsMainRoot)
				{
					return false;
				}
				TreeNodeExplorer tr = this.Parent as TreeNodeExplorer;
				if (tr == null)
				{
					return true;
				}
				IRootNode root = tr.RootClassNode;
				return (root == null);
			}
		}
		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			MethodPointer mp = act.ActionMethod as MethodPointer;
			if (mp != null)
			{
				IObjectIdentity o = mp.Owner;
				if (sameLevel)
				{
					return this.OwnerIdentity.IsSameObjectRef(o);
				}
				else
				{
					ClassPointer cp = this.OwnerPointer as ClassPointer;
					while (o != null)
					{
						if (o.IsSameObjectRef(cp))
						{
							return true;
						}
						o = o.IdentityOwner;
					}
				}
			}
			return false;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly && ClassData.DesignerHolder != null)
			{
				List<MenuItem> l = new List<MenuItem>();
				l.Add(new MenuItemWithBitmap("Change Icon", onChangeIcon, Resources._changeIcon.ToBitmap()));
				l.Add(new MenuItemWithBitmap("Add Utility", onAddStaticClass, Resources._assembly.ToBitmap()));
				l.Add(new MenuItemWithBitmap("Use Resources", onMapResources, Resources.resx.ToBitmap()));
				l.Add(new MenuItemWithBitmap("Resource Manager", onEditResources, Resources.resx.ToBitmap()));
				ProjectResources rm = ClassData.Project.GetProjectSingleData<ProjectResources>();
				rm.AddLanguageSelectionMenu(l);
				return l.ToArray();
			}
			return null;
		}
		private void onEditResources(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				LimnorProject prj = tv.RootClassNode.RootObjectId.Project;
				if (DlgResMan.EditResources(tv.FindForm(), prj) == DialogResult.OK)
				{

				}
			}
		}
		private void onMapResources(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				_data.RootClassID.MapResources(tv.FindForm());
			}
		}
		private void onAddStaticClass(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				ILimnorDesignPane pane = tv.RootId.Project.GetTypedData<ILimnorDesignPane>(tv.RootId.ClassId);
				if (pane != null)
				{
					Type t = FormTypeSelection.SelectType(tv.FindForm());
					if (t != null)
					{
						bool bExists = false;
						for (int i = 0; i < this.TreeView.Nodes.Count; i++)
						{
							TreeNodeClassType tnc = this.TreeView.Nodes[i] as TreeNodeClassType;
							if (tnc != null)
							{
								if (t.Equals(tnc.OwnerDataType))
								{
									bExists = true;
									break;
								}
							}
						}
						if (!bExists)
						{
							this.TreeView.Nodes.Add(new TreeNodeClassType(tv, true, t, ScopeMethodId));
							tv.RootId.AddExternalType(t);
							pane.Loader.NotifyChanges();
						}
					}
				}
			}
		}
		private void onChangeIcon(object sender, EventArgs e)
		{
			ClassPointer cp = this.OwnerPointer as ClassPointer;
			Form f = this.TreeView.FindForm();
			OpenFileDialog dlg = new OpenFileDialog();
			if (typeof(LimnorApp).IsAssignableFrom(cp.ObjectType))
			{
				dlg.Title = "Select an icon the application icon";
				dlg.Filter = "Icon|*.ico";
				dlg.DefaultExt = "ico";
			}
			else
			{
				dlg.Title = "Select an icon or a 16x16 bitmap file as the object icon";
				dlg.Filter = "Icon bitmap|*.bmp;*.jpg;*.gif;*.png;*.ico";
				dlg.DefaultExt = "bmp";
			}
			if (dlg.ShowDialog(f) == DialogResult.OK)
			{
				Cursor.Current = Cursors.WaitCursor;
				if (f != null)
				{
					f.Cursor = Cursors.WaitCursor;
				}
				cp.ChangeIconFile(dlg.FileName, ViewersHolder.Loader, f);
				NotifyChanges();
				ViewersHolder.OnIconChanged(this.ClassId);
				Dictionary<UInt32, ILimnorDesignPane> designPanes = cp.Project.GetTypedDataList<ILimnorDesignPane>();
				foreach (ILimnorDesignPane pn in designPanes.Values)
				{
					if (pn.Loader.ClassID != cp.ClassId)
					{
						pn.PaneHolder.OnIconChanged(cp.ClassId);
					}
				}
				if (f != null)
				{
					f.Cursor = Cursors.Default;
				}
				Cursor.Current = Cursors.Default;
			}
		}
		public Dictionary<UInt32, IAction> GetActions()
		{
			if (_data.ActionList == null)
			{
				_data.ReloadActionList();
			}
			return _data.ActionList;
		}
		public IAction GetAction(UInt64 id)
		{
			IAction a;
			UInt32 actId;
			UInt32 classId;
			DesignUtil.ParseDDWord(id, out actId, out classId);
			Dictionary<UInt32, IAction> acts = GetActions();
			if (acts.TryGetValue(actId, out a))
			{
				if (a != null)
				{
					if (a.ClassId == classId)
					{
						return a;
					}
				}
			}
			if (_loader != null)
			{
				ClassPointer cp = _loader.GetRootId();
				if (cp != null)
				{
					return new ActionInvalid(cp, actId);
				}
			}
			return null;
		}
		public List<EventAction> GetEventHandlers()
		{
			if (_data.EventHandlerList == null)
			{
				_data.ReloadActionList();
			}
			return _data.EventHandlerList;
		}
		public void ResetEventHandlers()
		{
			if (_data.RootClassID.ObjectInstance != null)
			{
				_data.RootClassID.ReloadEventActions(null);
			}
		}
		public EventAction GetEventHandler(IEvent ep)
		{
			List<EventAction> handlers = GetEventHandlers();
			foreach (EventAction ea in handlers)
			{
				if (ea.Event.IsSameObjectRef(ep))
				{
					if (ea.GetAssignActionId() == 0)
					{
						return ea;
					}
				}
			}
			return null;
		}
		public IList<EventAction> GetEventHandlers(IEvent ep)
		{
			List<EventAction> ret = new List<EventAction>();
			List<EventAction> handlers = GetEventHandlers();
			foreach (EventAction ea in handlers)
			{
				if (ea.Event.IsSameObjectRef(ep))
				{
					ret.Add(ea);
				}
			}
			return ret;
		}
		public bool HasEventHandler(IEvent ep)
		{
			EventPointer topEv = null;
			List<EventAction> handlers = GetEventHandlers();
			foreach (EventAction ea in handlers)
			{
				if (ea.TaskIDList != null && ea.TaskIDList.Count > 0)
				{
					if (ea.Event.IsSameObjectRef(ep))
					{
						return true;
					}
					else
					{
						EventPointer subEp = ea.Event as EventPointer;
						if (subEp != null)
						{
							IEventInfoTree subTree = subEp.Info as IEventInfoTree;
							if (subTree != null)
							{
								if (topEv == null)
								{
									topEv = ep as EventPointer;
								}
								if (topEv != null)
								{
									IEventInfoTree topTree = topEv.Info as IEventInfoTree;
									if (topTree != null)
									{
										if (topTree.IsChild(subTree))
										{
											return true;
										}
									}
								}
							}
						}
						// eTop = ep.ev
					}
				}
			}
			return false;
		}
		public void SetClassRefIcon(UInt32 classId, System.Drawing.Image img)
		{
			int idx = -1;
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassRoot nd = Nodes[i] as TreeNodeClassRoot;
				if (nd != null)
				{
					MemberComponentId mc = nd.OwnerPointer as MemberComponentId;
					if (mc != null)
					{
						ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
						if (cr != null)
						{
							if (cr.ClassId == classId)
							{
								cr.ImageIcon = img;
								if (idx < 0)
								{
									idx = TreeViewObjectExplorer.AddBitmap(img);
								}
								nd.ImageIndex = idx;
								nd.SelectedImageIndex = idx;
							}
						}
					}
				}
			}
		}
		private EventHandlerOwnerChanged _ownerChangedHandler;
		public EventHandlerOwnerChanged OwnerChangedhandler
		{
			get
			{
				if (_ownerChangedHandler == null)
				{
					_ownerChangedHandler = new EventHandlerOwnerChanged(TreeRoot_OwnerChanged);
				}
				return _ownerChangedHandler;
			}
		}
		/// <summary>
		/// control parent changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TreeRoot_OwnerChanged(object sender, EventArgsOwnerChanged e)
		{
			if (this.NextLevelLoaded && e.NewOwner != e.OldOwner)
			{
				//remove from the old parent
				TreeNodeClass tncc = this.FindComponentNode(e.OldOwner);
				if (tncc != null)
				{
					foreach (TreeNode tn in tncc.Nodes)
					{
						TreeNodeClassComponent t = tn as TreeNodeClassComponent;
						if (t != null)
						{
							if (t.OwnerPointer.ObjectInstance == sender)
							{
								tncc.Nodes.Remove(t);
								break;
							}
						}
					}
				}
				//add to the new parent
				tncc = this.FindComponentNode(e.NewOwner);
				if (tncc != null)
				{
					if (tncc.NextLevelLoaded)
					{
						TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
						UInt32 scopeId = 0;
						if (tv != null)
						{
							scopeId = tv.ScopeMethodId;
							ClassPointer root = (ClassPointer)(OwnerPointer);
							TreeNodeClassComponent t0 = tv.CreateComponentTreeNode(root, sender, root.ObjectList.GetObjectID(sender), scopeId, SelectionTarget, TargetScope, EventScope);
							t0.TreeChild = root.ObjectList.TreeRoot.SearchChildByOwner(sender);
							tncc.Nodes.Add(t0);
						}
					}
				}
			}
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}
			//load components
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassRoot rootTreeNode = parentNode as TreeNodeClassRoot;
				if (rootTreeNode.IsMainRoot || rootTreeNode.IsStatic)
				{
					ObjectIDmap map = rootTreeNode.ClassData.ObjectMap;
					if (map != null)
					{
						UInt32 scopeId = parentNode.ScopeMethodId;
						if (map.Count == 0)
						{
							map.LoadObjects();
							if (map.Reader.HasErrors)
							{
								MathNode.Log(map.Reader.Errors);
								map.Reader.ResetErrors();
							}
						}
						if (rootTreeNode.TreeChild == null)
						{
							rootTreeNode.TreeChild = map.TreeRoot;
						}
						map.TreeRoot.OwnerChanged -= rootTreeNode.OwnerChangedhandler;
						map.TreeRoot.OwnerChanged += rootTreeNode.OwnerChangedhandler;
						SortedList<string, TreeNodeClassComponent> ts = new SortedList<string, TreeNodeClassComponent>();
						foreach (Tree t in rootTreeNode.TreeChild)
						{
							ToolStripMenuItem mi = t.Owner as ToolStripMenuItem;
							if (mi != null)
							{
								if (mi.Site == null)
								{
									continue;
								}
								if (!mi.Site.DesignMode)
								{
									continue;
								}
							}
							TreeNodeClassComponent tncc = tv.CreateComponentTreeNode((ClassPointer)(rootTreeNode.OwnerPointer), t.Owner, map.GetObjectID(t.Owner), scopeId, parentNode.SelectionTarget, parentNode.TargetScope, parentNode.EventScope);
							tncc.TreeChild = t;
							ts.Add(tncc.Text, tncc);
						}
						IEnumerator<KeyValuePair<string, TreeNodeClassComponent>> ie = ts.GetEnumerator();
						while (ie.MoveNext())
						{
							parentNode.Nodes.Add(ie.Current.Value);
						}
					}
				}
			}
		}
		public ITreeNodeClass GetClassTreeNode(UInt32 classId)
		{
			if (classId == this.ClassData.RootClassID.ClassId)
				return this;
			LoadNextLevel();
			ITreeNodeClass r = GetComponentTreeNode(classId);
			if (r != null)
				return r;
			TreeView tv = this.TreeView;
			if (tv != null)
			{
				for (int i = 0; i < tv.Nodes.Count; i++)
				{
					ITreeNodeClass classNode = tv.Nodes[i] as ITreeNodeClass;
					if (classNode != null)
					{
						if (classNode.ClassId == classId)
						{
							return classNode;
						}
					}
				}
			}
			return null;
		}

		public ITreeNodeClass GetComponentTreeNode(UInt32 classId)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassComponent nd = Nodes[i] as TreeNodeClassComponent;
				if (nd != null)
				{
					MemberComponentId mc = nd.OwnerPointer as MemberComponentId;
					if (mc != null)
					{
						if (mc.ClassId == classId)
						{
							return nd;
						}
						else
						{
							ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
							if (cr != null)
							{
								if (cr.ClassId == classId)
								{
									return nd;
								}
							}
						}
					}
				}
				else
				{
					TreeNodeClassType ndc = Nodes[i] as TreeNodeClassType;
					if (ndc != null)
					{
						TypePointer tp = ndc.OwnerPointer as TypePointer;
						if (tp != null)
						{
							if (tp.ClassId == classId)
							{
								return ndc;
							}
						}
					}
				}
			}
			return null;
		}
		public ITreeNodeClass GetComponentTreeNodeByMemberId(UInt32 memberId)
		{
			if (memberId == this.ClassData.RootClassID.MemberId)
				return this;
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeClassComponent nd = Nodes[i] as TreeNodeClassComponent;
				if (nd != null)
				{
					MemberComponentId mc = nd.OwnerPointer as MemberComponentId;
					if (mc != null)
					{
						if (mc.MemberId == memberId)
						{
							return nd;
						}
					}
				}
			}
			return null;
		}
		#region ICustomPropertyHolder Members

		public override List<PropertyClass> GetCustomProperties()
		{
			if (ClassData != null && ClassData.ObjectMap != null)
			{
				ClassPointer rid = null;
				if (ViewersHolder != null && ViewersHolder.Loader != null)
				{
					rid = ViewersHolder.Loader.GetRootId();
				}
				if (rid == null)
				{
					rid = ClassPointer.CreateClassPointer(ClassData.ObjectMap);
				}
				List<PropertyClass> props = new List<PropertyClass>();
				Dictionary<string, PropertyClass> ps = rid.CustomProperties;
				foreach (PropertyClass p in ps.Values)
				{
					props.Add(p);
				}
				return props;
			}
			return null;
		}
		public override List<MethodClass> GetCustomMethods()
		{
			if (ClassData != null && ClassData.ObjectMap != null)
			{
				ClassPointer rid = null;
				if (ViewersHolder != null && ViewersHolder.Loader != null)
				{
					rid = ViewersHolder.Loader.GetRootId();
				}
				if (rid == null)
				{
					rid = ClassPointer.CreateClassPointer(ClassData.ObjectMap);
				}
				List<MethodClass> methods = new List<MethodClass>();
				Dictionary<string, MethodClass> list = rid.CustomMethods;
				foreach (MethodClass m in list.Values)
				{
					methods.Add(m);
				}
				return methods;
			}
			return null;
		}
		public override List<EventClass> GetCustomEvents()
		{
			if (ClassData != null && ClassData.ObjectMap != null)
			{
				ClassPointer rid = null;
				if (ViewersHolder != null && ViewersHolder.Loader != null)
				{
					rid = ViewersHolder.Loader.GetRootId();
				}
				if (rid == null)
				{
					rid = ClassPointer.CreateClassPointer(ClassData.ObjectMap);
				}
				List<EventClass> events = new List<EventClass>();
				Dictionary<string, EventClass> list = rid.CustomEvents;
				foreach (EventClass e in list.Values)
				{
					events.Add(e);
				}
				return events;
			}
			return null;
		}
		#endregion
	}
	/// <summary>
	/// a non-static class
	/// 1.1. non-static PME (for design and selection in the scope) TreeNodeNonStaticPME
	///         Collection Nodes (non-static) for library (base-type) PME
	///             non-static lib PME nodes
	///         Collection Nodes (non-static) for custom developed PME
	///             non-static custom PME nodes
	/// 
	/// 1.2. static PME (for design and selection) TreeNonStaticPME
	///         Collection Nodes (static) for library (base-type) PME
	///             static lib PME nodes
	///         Collection Nodes (static) for custom developed PME
	///             static custom PME nodes
	/// </summary>
	public class TreeNodeClassNonStatic : TreeNodeClassRoot
	{
		public TreeNodeClassNonStatic(TreeViewObjectExplorer tv, bool isMainNode, ClassPointer rd, bool staticScope)
			: base(tv, isMainNode, rd, staticScope)
		{
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(OwnerPointer.ObjectInstance);
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			base.ResetNextLevel(tv);
			UInt32 scopeId = ScopeMethodId;
			if (!StaticScope)
			{
				Nodes.Add(new TreeNodeConstructorCollection(tv, this, this.OwnerPointer, scopeId));
				Nodes.Add(new TreeNodeOverridesInterfaces((ClassPointer)(this.OwnerPointer)));
				if (this.IsMainRoot || SelectionTarget == EnumObjectSelectType.Type || SelectionTarget == EnumObjectSelectType.BaseClass)
				{
					Nodes.Add(new TreeNodePMENonStatic(tv, this.OwnerPointer, scopeId));
				}
			}
			TreeNodePMEStatic pme = new TreeNodePMEStatic(tv, this.OwnerPointer, scopeId);
			Nodes.Add(pme);
#if USEHTMLEDITOR
            WebPage wpage = ClassData.RootClassID.ObjectInstance as WebPage;
            if (wpage != null)
            {
                Nodes.Add(new TreeNodeHtmlElementCollection(tv, this, ClassData.RootClassID, tv.ScopeMethodId));
            }
#endif
			if (!StaticScope)
			{
				List<TreeNodeLoader> loaders = GetLoaderNodes();
				if (loaders != null && loaders.Count > 0)
				{
					foreach (TreeNodeLoader l in loaders)
					{
						this.Nodes.Add(l);
					}
				}
			}
			this.Collapse();
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				MemberComponentId mcid = o as MemberComponentId;
				if (mcid != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodeClassComponent ncc = Nodes[i] as TreeNodeClassComponent;
						if (ncc != null)
						{
							if (mcid.MemberId == ncc.MemberId)
							{
								ownerStack.Pop();
								if (ownerStack.Count == 0)
								{
									return ncc;
								}
								TreeNodeObject nret = ncc.LocateObjectNode(ownerStack);
								if (nret == null)
								{
									return ncc;
								}
								else
								{
									return nret;
								}
							}
							else
							{
								if (ncc.NextLevelLoaded)
								{
									ncc = ncc.FindNodeById(mcid.MemberId);
									if (ncc != null)
									{
										while (ownerStack.Count > 0)
										{
											o = ownerStack.Pop();
											MemberComponentId mcid0 = o as MemberComponentId;
											if (mcid0 == null)
											{
												break;
											}
											if (mcid0.MemberId == mcid.MemberId)
											{
												break;
											}
										}
										if (ownerStack.Count == 0)
										{
											return ncc;
										}
										TreeNodeObject nret = ncc.LocateObjectNode(ownerStack);
										if (nret == null)
										{
											return ncc;
										}
										else
										{
											return nret;
										}
									}
								}
							}
						}
					}
				}
				else
				{
					ConstObjectPointer co = o as ConstObjectPointer;
					if (co != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeAttributeCollection t1 = Nodes[i] as TreeNodeAttributeCollection;
							if (t1 != null)
							{
								return t1.LocateObjectNode(ownerStack);
							}
						}
					}
					else
					{
						ConstructorClass cc = o as ConstructorClass;
						if (cc != null)
						{
							for (int i = 0; i < Nodes.Count; i++)
							{
								TreeNodeConstructorCollection t2 = Nodes[i] as TreeNodeConstructorCollection;
								if (t2 != null)
								{
									return t2.LocateObjectNode(ownerStack);
								}
							}
						}
						else
						{
							if (o is PropertyClassInherited || o is MethodClassInherited || o is InterfacePointer)
							{
								for (int i = 0; i < Nodes.Count; i++)
								{
									TreeNodeOverridesInterfaces t3 = Nodes[i] as TreeNodeOverridesInterfaces;
									if (t3 != null)
									{
										return t3.LocateObjectNode(ownerStack);
									}
								}
							}
							else
							{
								if (o.IsStatic)
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodePMEStatic t4 = Nodes[i] as TreeNodePMEStatic;
										if (t4 != null)
										{
											return t4.LocateObjectNode(ownerStack);
										}
									}
								}
								else
								{
									for (int i = 0; i < Nodes.Count; i++)
									{
										TreeNodePMENonStatic t5 = Nodes[i] as TreeNodePMENonStatic;
										if (t5 != null)
										{
											return t5.LocateObjectNode(ownerStack);
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;// (pointer is MethodClass || pointer is MethodParamPointer);
		}
	}
	/// <summary>
	/// The class is treated as a non-static class with following differences:
	///     a). it does not show static attributes node
	///     b). it shows non-static attributes using icons for static objects
	///     c). it does not show non-static lib PME
	///     d). when compiling, all members are treated static
	///     2.1. mixed PME (for design and selection) TreeNodeMixedPME
	///         Collection Nodes (static) for library (base-type) PME
	///             static lib PME nodes
	///         Collection Nodes (non-static) for custom developed PME (using icons for static objects)
	///             non-static custom PME nodes
	/// </summary>
	public class TreeNodeClassStatic : TreeNodeClassRoot
	{
		public TreeNodeClassStatic(TreeViewObjectExplorer tv, bool isMainNode, ClassPointer rd)
			: base(tv, isMainNode, rd, true)
		{
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			base.ResetNextLevel(tv);
			//TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			Nodes.Add(new TreeNodePMEMixed(tv, this.OwnerPointer, ScopeMethodId));
			List<TreeNodeLoader> loaders = GetLoaderNodes();
			if (loaders != null && loaders.Count > 0)
			{
				foreach (TreeNodeLoader l in loaders)
				{
					this.Nodes.Add(l);
				}
			}
			this.Collapse();
		}
		public override TreeNodePME GetMemberNode(bool forStatic)
		{
			TreeNodePME node;
			for (int i = 0; i < Nodes.Count; i++)
			{
				node = Nodes[i] as TreeNodePME;
				if (node != null)
				{
					return node;
				}
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
	}
	/// <summary>
	/// root class for root/static root and component
	/// </summary>
	public abstract class TreeNodeClass : TreeNodeObject, ICustomPropertyHolder, ITreeNodeClass
	{
		public TreeNodeClass(bool isForStatic, IObjectPointer objectPointer)
			: base(isForStatic, objectPointer)
		{

		}
		public abstract UInt32 ClassId { get; }
		public abstract UInt32 MemberId { get; }
		public abstract IClass Holder { get; }
		public abstract void ResetPropertyNode(bool forStatic, bool forCustom);
		public abstract void ResetMethodNode(bool forStatic, bool forCustom);
		public abstract void ResetEventNode(bool forStatic, bool forCustom);
		public Tree TreeChild { get; set; }
		public Image ObjectIcon
		{
			get
			{
				return TreeViewObjectExplorer.GetTypeImage(this.ImageIndex);
			}
		}
		public override EnumActionMethodType ActionMethodType
		{
			get
			{
				return EnumActionMethodType.Unknown;
			}
		}
		public void OnClassLoadedIntoDesigner(ClassPointer root)
		{
			if (NextLevelLoaded)
			{
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassComponentCust tccc = Nodes[i] as TreeNodeClassComponentCust;
					if (tccc != null)
					{
						if (tccc.DefinitionClassId == root.ClassId)
						{
							tccc.ResetPointer(root);
							//tccc.ResetNextLevel();
							continue;
						}
					}
					TreeNodeClass tnc = Nodes[i] as TreeNodeClass;
					if (tnc != null)
					{
						tnc.OnClassLoadedIntoDesigner(root);
					}
				}
			}
		}
		public void OnDefinitionChanged(UInt32 classId, object relatedObject, EnumClassChangeType changeMade)
		{
			if (NextLevelLoaded)
			{
				TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassComponentCust tccc = Nodes[i] as TreeNodeClassComponentCust;
					if (tccc != null)
					{
						if (tccc.DefinitionClassId == classId)
						{
							tccc.ResetNextLevel(tv);
							continue;
						}
					}
					TreeNodeClass tnc = Nodes[i] as TreeNodeClass;
					if (tnc != null)
					{
						tnc.OnDefinitionChanged(classId, relatedObject, changeMade);
					}
				}
			}
		}
		public IList<TreeNodeClassComponentCust> GetInstancesByClassId(UInt32 classId)
		{
			List<TreeNodeClassComponentCust> list = new List<TreeNodeClassComponentCust>();
			if (NextLevelLoaded)
			{
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassComponentCust tnc = Nodes[i] as TreeNodeClassComponentCust;
					if (tnc != null)
					{
						if (tnc.DefinitionClassId == classId)
						{
							list.Add(tnc);
						}
						IList<TreeNodeClassComponentCust> list1 = tnc.GetInstancesByClassId(classId);
						if (list1.Count > 0)
						{
							list.AddRange(list1);
						}
					}
				}
			}
			return list;
		}
		public TreeNodeClass FindComponentNode(object obj)
		{
			if (obj == this.OwnerPointer.ObjectInstance)
			{
				return this;
			}
			foreach (TreeNode nd in Nodes)
			{
				TreeNodeClassComponent tncc = nd as TreeNodeClassComponent;
				if (tncc != null)
				{
					if (tncc.OwnerPointer.ObjectInstance == obj)
					{
						return tncc;
					}
					else
					{
						TreeNodeClassComponent t0 = tncc.FindNodeByObject(obj);
						if (t0 != null)
						{
							return t0;
						}
					}
				}
			}
			return null;
		}
		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			if (pointer == null)
				return null;
			if (pointer.IsSameObjectRef(this.OwnerIdentity))
			{
				return this;
			}
			IObjectIdentity p = pointer;
			while (p.IdentityOwner != null && !(p.IdentityOwner is IClass))
			{
				p = p.IdentityOwner;
			}
			if (!(p is IClass))
			{
				LoadNextLevel();
				TreeNodePME pmeNode = GetMemberNode(p.IsStatic);
				if (pmeNode != null)
				{
					return pmeNode.FindObjectNode(p);
				}
			}
			return null;
		}
		/// <summary>
		/// use ExecuterClassId to locate this node, within this node find the executer object:
		/// a property for SetterPointer
		/// a method for MethodPointer
		/// </summary>
		/// <param name="act"></param>
		/// <returns></returns>
		public override TreeNodeAction FindActionNode(IAction act)
		{
			this.LoadNextLevel();
			TreeNodePME pmeNode = this.GetMemberNode(act.IsStatic);
			if (pmeNode != null)
			{
				pmeNode.LoadNextLevel();
				for (int i = 0; i < pmeNode.Nodes.Count; i++)
				{
					TreeNodeObject objNode = pmeNode.Nodes[i] as TreeNodeObject;
					if (objNode != null && !(objNode is TreeNodeActionCollection))
					{
						TreeNodeAction na = objNode.FindActionNode(act);
						if (na != null)
						{
							return na;
						}
					}
				}
			}
			return null;
		}
		public virtual void RefreshIcon(UInt32 classId)
		{
			ClassPointer root = this.OwnerPointer as ClassPointer;
			if (root != null)
			{
				if (classId == root.ClassId)
				{
					Bitmap bmp = root.ImageIcon as Bitmap;
					if (bmp != null)
					{
						this.ImageIndex = TreeViewObjectExplorer.AddBitmap(bmp);
						this.SelectedImageIndex = this.ImageIndex;
						TreeViewObjectExplorer.SetTypeIcon(root.Project, root.ClassId, this.ImageIndex);
					}
				}
			}
		}
		public abstract void OnActionChanged(UInt32 classId, IAction act, bool isNewAction);
		public abstract void HandleActionModified(UInt32 classId, IAction act, bool isNewAction);
		public virtual TreeNodePME GetMemberNode(bool forStatic)
		{
			TreeNodePME node;
			for (int i = 0; i < Nodes.Count; i++)
			{
				node = Nodes[i] as TreeNodePME;
				if (node != null)
				{
					if (node.IsStatic == forStatic)
					{
						return node;
					}
				}
			}
			return null;
		}

		#region ICustomPropertyHolder Members

		public abstract List<PropertyClass> GetCustomProperties();
		public abstract List<MethodClass> GetCustomMethods();
		public abstract List<EventClass> GetCustomEvents();
		#endregion
	}
	/// <summary>
	/// It is for showing member components. For design and for selection within the scope.
	/// If the memeber is an instance of a class under development (cannot be a static class) 
	/// then the component is associated with a ClassInstancePointer as its pointer.
	/// When doing selection, it can be created only when its container is defining the scope
	/// 1. Do not show static nodes. 
	/// 2. Instance-nodes are displayed with icons for instance nodes
	/// 3. Show instance custom nodes
	/// </summary>
	public abstract class TreeNodeClassComponent : TreeNodeClass, ITreeNodeClass
	{
		public TreeNodeClassComponent(TreeViewObjectExplorer tv, bool isForStatic, MemberComponentId objectPointer, UInt32 scopeMethodId)
			: base(isForStatic, objectPointer)
		{
			this.Nodes.Add(new TreeNodePMEPointer(tv, objectPointer, scopeMethodId));
			this.Nodes.Add(new TreeNodePMEPointerStatic(tv, objectPointer, scopeMethodId));
			ResetNextLevel(tv);
		}
		public TreeNodeClassComponent FindNodeByObject(object obj)
		{
			if (this.OwnerPointer.ObjectInstance == obj)
			{
				return this;
			}
			if (this.NextLevelLoaded)
			{
				foreach (TreeNode tn in Nodes)
				{
					TreeNodeClassComponent tncc = tn as TreeNodeClassComponent;
					if (tncc != null)
					{
						TreeNodeClassComponent tc = tncc.FindNodeByObject(obj);
						if (tc != null)
						{
							return tc;
						}
					}
				}
			}
			return null;
		}
		public TreeNodeClassComponent FindNodeById(UInt32 id)
		{
			if (this.MemberId == id)
				return this;
			if (NextLevelLoaded)
			{
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassComponent ncc = Nodes[i] as TreeNodeClassComponent;
					if (ncc != null)
					{
						if (ncc.MemberId == id)
						{
							return ncc;
						}
						else
						{
							TreeNodeClassComponent ncc0 = ncc.FindNodeById(id);
							if (ncc0 != null)
							{
								return ncc0;
							}
						}
					}
				}
			}
			return null;
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Peek();
				if (o.IsStatic)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePMEPointerStatic tn = Nodes[i] as TreeNodePMEPointerStatic;
						if (tn != null)
						{
							return tn.LocateObjectNode(ownerStack);
						}
					}
				}
				else
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodePMEPointer tn = Nodes[i] as TreeNodePMEPointer;
						if (tn != null)
						{
							return tn.LocateObjectNode(ownerStack);
						}
					}
				}
			}
			return null;
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			if (OwnerPointer.ObjectInstance != null)
			{
				ClassInstancePointer cr = OwnerPointer as ClassInstancePointer;
				if (cr == null)
				{
					MemberComponentIdCustom mcid = OwnerPointer as MemberComponentIdCustom;
					if (mcid != null)
					{
						cr = mcid.Pointer;
					}
				}
				if (cr != null)
				{
					loader.NotifySelection(cr.ObjectInstance);
				}
				else
				{
					loader.NotifySelection(OwnerPointer.ObjectInstance);
				}
			}
		}
		public override UInt32 ClassId
		{
			get
			{
				MemberComponentId mc = this.OwnerPointer as MemberComponentId;
				return mc.ClassId;
			}
		}
		public override UInt32 MemberId
		{
			get
			{
				MemberComponentId mc = this.OwnerPointer as MemberComponentId;
				return mc.MemberId;
			}
		}
		public override IClass Holder
		{
			get
			{
				MemberComponentId mc = this.OwnerPointer as MemberComponentId;
				return mc.ObjectInstance as IClass;
			}
		}
		private void onToFront(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			Control c = mc.ObjectInstance as Control;
			if (c != null)
			{
				c.BringToFront();
				root.ViewersHolder.Loader.NotifyChanges();
			}
		}
		private void onToBack(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			Control c = mc.ObjectInstance as Control;
			if (c != null)
			{
				c.SendToBack();
				root.ViewersHolder.Loader.NotifyChanges();
			}
		}
		private void onDelete(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			IComponent cr = mc.ObjectInstance as IComponent;
			if (cr != null)
			{
				root.ViewersHolder.Loader.DeleteComponent(cr);
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			List<MenuItem> ml = new List<MenuItem>();
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (mc != null)
			{
				Control c = mc.ObjectInstance as Control;
				if (c != null)
				{
					ml.Add(new MenuItemWithBitmap("Bring to front", onToFront, Resources._bringToFront.ToBitmap()));
					ml.Add(new MenuItemWithBitmap("Send to back", onToBack, Resources._sendToBack.ToBitmap()));
				}
			}
			if (mc == null || !mc.IsClassInstance)
			{
				IRootNode root = this.RootClassNode;
				if (!readOnly && root.ViewersHolder != null)
				{
					if (ml.Count > 0)
					{
						ml.Add(new MenuItem("-"));
					}
					ml.Add(new MenuItemWithBitmap("Delete", onDelete, Resources._cancel.ToBitmap()));
				}
			}
			return ml.ToArray();
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad(this.IsStatic));
			return l;
		}
		public override void ResetPropertyNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (mc != null)
			{
				ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
				if (cr != null)
				{
					TreeNodeClassRoot r = this.TopLevelRootClassNode;
					r.ReloadClassRefProperties(cr);
				}
			}
			TreeNodePME na = GetMemberNode(forStatic);
			if (na != null)
			{
				if (na.NextLevelLoaded)
				{
					for (int i = 0; i < na.Nodes.Count; i++)
					{
						if (forCustom)
						{
							TreeNodeCustomPropertyCollection pc = na.Nodes[i] as TreeNodeCustomPropertyCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
						else
						{
							TreeNodePropertyCollection pc = na.Nodes[i] as TreeNodePropertyCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
					}
				}
			}
		}
		public override void ResetMethodNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (mc != null)
			{
				ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
				if (cr != null)
				{
					TreeNodeClassRoot r = this.TopLevelRootClassNode;
					r.ReloadClassRefProperties(cr);
				}
			}
			TreeNodePME na = GetMemberNode(forStatic);
			if (na != null)
			{
				if (na.NextLevelLoaded)
				{
					for (int i = 0; i < na.Nodes.Count; i++)
					{
						if (forCustom)
						{
							TreeNodeCustomMethodCollection pc = na.Nodes[i] as TreeNodeCustomMethodCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
						else
						{
							TreeNodeMethodCollection pc = na.Nodes[i] as TreeNodeMethodCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
					}
				}
			}
		}
		public override void ResetEventNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			TreeNodePME na = GetMemberNode(forStatic);
			if (na != null)
			{
				if (na.NextLevelLoaded)
				{
					for (int i = 0; i < na.Nodes.Count; i++)
					{
						if (forCustom)
						{
							TreeNodeCustomEventCollection pc = na.Nodes[i] as TreeNodeCustomEventCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
						else
						{
							TreeNodeEventCollection pc = na.Nodes[i] as TreeNodeEventCollection;
							if (pc != null)
							{
								pc.ResetNextLevel(tv);
								break;
							}
						}
					}
				}
			}
		}

		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			if (this.OwnerIdentity != null && pointer != null)
			{
				if (this.OwnerIdentity.IsSameObjectRef(pointer))
				{
					return this;
				}
			}
			bool isStatic = false;
			IObjectPointer op = pointer as IObjectPointer;
			if (op != null)
			{
				isStatic = op.IsStatic;
			}
			TreeNodePME nodeMember = GetMemberNode(isStatic);
			TreeNodeObject node = nodeMember.FindObjectNode(pointer);
			if (node != null)
			{
				return node;
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;// !(pointer is ClassRef || pointer is RootClassId);
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					);
			}
			return false;
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			bool bHasloader = false;
			List<TreeNodeClassComponent> list = new List<TreeNodeClassComponent>();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodePME tna = Nodes[i] as TreeNodePME;
				if (tna != null)
				{
					for (int k = 0; k < tna.Nodes.Count; k++)
					{
						TreeNodeObject tno = tna.Nodes[k] as TreeNodeObject;
						if (tno != null)
						{
							tno.ResetNextLevel(tv);
						}
					}
				}
				else
				{
					TreeNodeClassComponent tncc = Nodes[i] as TreeNodeClassComponent;
					if (tncc != null)
					{
						list.Add(tncc);
					}
					else
					{
						if (!bHasloader)
						{
							if (Nodes[i] is CLoad)
							{
								bHasloader = true;
							}
						}
					}
				}
			}
			foreach (TreeNodeClassComponent tncc in list)
			{
				Nodes.Remove(tncc);
			}
			if (!bHasloader)
			{
				//Nodes.Add(new CLoad(this.IsStatic));
				List<TreeNodeLoader> l = GetLoaderNodes();
				if (l != null)
				{
					foreach (TreeNodeLoader l0 in l)
					{
						Nodes.Add(l0);
					}
				}
			}
			NextLevelLoaded = false;
			this.Collapse();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="act"></param>
		/// <returns></returns>
		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			IObjectIdentity o = act.MethodOwner;//.ActionMethod.Owner;
			MemberComponentId ownerId = this.OwnerPointer as MemberComponentId;
			if (sameLevel)
			{
				if (ownerId.IsSameObjectRef(o))
				{
					return true;
				}
			}
			else
			{
				while (o != null)
				{
					if (o.IsSameObjectRef(ownerId))
					{
						return true;
					}
					o = o.IdentityOwner;
				}
			}
			return false;
		}
		public TreeNodeEvent GetEventNode(string eventName)
		{
			LoadNextLevel();
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodePME tna = Nodes[i] as TreeNodePME;
				if (tna != null)
				{
					TreeNodeEventCollection ec = tna.GetEventCollection();
					if (ec != null)
					{
						return ec.GetEventNode(eventName);
					}
					break;
				}
			}
			return null;
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad(bool forStatic)
				: base(forStatic)
			{
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassComponent tncc = parentNode as TreeNodeClassComponent;
				if (tncc != null)
				{
					if (tncc.TreeChild != null && tncc.TreeChild.Count > 0)
					{
						ClassPointer root = tv.RootId;
						IClassContainer ic = tncc.OwnerPointer as IClassContainer;
						IClass c = tncc.OwnerPointer as IClass;

						UInt32 scopeId = parentNode.ScopeMethodId;
						SortedList<string, TreeNode> lst = new SortedList<string, TreeNode>();
						foreach (Tree t in tncc.TreeChild)
						{
							TreeNodeClassComponent tncc0;
							if (ic != null)
							{
								tncc0 = tv.CreateComponentTreeNode(ic, t.Owner, ic.Definition.ObjectList.GetObjectID(t.Owner), scopeId, parentNode.SelectionTarget, parentNode.TargetScope, parentNode.EventScope);
							}
							else
							{
								tncc0 = tv.CreateComponentTreeNode(c, t.Owner, root.ObjectList.GetObjectID(t.Owner), scopeId, parentNode.SelectionTarget, parentNode.TargetScope, parentNode.EventScope);
							}
							tncc0.TreeChild = t;
							lst.Add(tncc0.Text, tncc0);
						}
						IEnumerator<KeyValuePair<string, TreeNode>> ie = lst.GetEnumerator();
						while (ie.MoveNext())
						{
							parentNode.Nodes.Add(ie.Current.Value);
						}
					}
				}
			}
		}
		#region ICustomPropertyHolder Members

		public override List<PropertyClass> GetCustomProperties()
		{
			List<PropertyClass> props = new List<PropertyClass>();
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (mc != null)
			{
				ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
				if (cr != null)
				{
					List<PropertyClassDescriptor> ps = cr.CustomProperties;
					if (ps != null)
					{
						foreach (PropertyClassDescriptor p in ps)
						{
							props.Add(p.CustomProperty);
						}
					}
				}
			}
			return props;
		}
		public override List<MethodClass> GetCustomMethods()
		{
			List<MethodClass> methods = new List<MethodClass>();
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (mc != null)
			{
				ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
				if (cr != null)
				{
					Dictionary<string, MethodClass> ms = cr.GetCustomMethods();
					foreach (MethodClass m in ms.Values)
					{
						methods.Add(m);
					}
				}
			}
			return methods;
		}
		public override List<EventClass> GetCustomEvents()
		{
			List<EventClass> props = new List<EventClass>();
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (mc != null)
			{
				ClassInstancePointer cr = mc.ObjectInstance as ClassInstancePointer;
				if (cr != null)
				{
					Dictionary<string, EventClass> ps = cr.GetCustomEvents();
					if (ps != null)
					{
						foreach (EventClass e in ps.Values)
						{
							props.Add(e);
						}
					}
				}
			}
			return props;
		}
		#endregion
	}
	public class TreeNodeClassComponentLib : TreeNodeClassComponent
	{
		public TreeNodeClassComponentLib(TreeViewObjectExplorer tv, MemberComponentId objectPointer, UInt32 scopeMethodId)
			: base(tv, false, objectPointer, scopeMethodId)
		{
			this.ImageIndex = TreeViewObjectExplorer.GetTypeIcon(objectPointer.ObjectType);
			this.SelectedImageIndex = this.ImageIndex;
		}
		private void mi_changeBaseType(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			MemberComponentId mcid = this.OwnerPointer as MemberComponentId;
			FormTypeSelection dlg = new FormTypeSelection();
			dlg.Text = "Select new base type";
			dlg.SetSelectionBaseType(mcid.ObjectType);
			if (dlg.ShowDialog(this.TreeView.FindForm()) == DialogResult.OK)
			{
				if (!dlg.SelectedType.Equals(mcid.ObjectType))
				{
					mcid.ObjectType = dlg.SelectedType;
					root.ViewersHolder.Loader.ObjectMap.AddTypeChange(mcid.MemberId, mcid.ObjectType);
					root.ViewersHolder.Loader.NotifyChanges();
				}
			}

		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}
		public override void HandleActionModified(UInt32 classId, IAction act, bool isNewAction)
		{
			OnActionChanged(classId, act, isNewAction);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			MenuItem[] mi = base.GetContextMenuItems(readOnly);
			if (mi != null && mi.Length > 0)
			{
				MenuItem[] mi2 = new MenuItem[mi.Length + 2];
				mi.CopyTo(mi2, 0);
				mi2[mi.Length] = new MenuItem("-");
				mi2[mi.Length + 1] = new MenuItemWithBitmap("Change base type (need reload)", mi_changeBaseType, Resources._baseClass.ToBitmap());
				return mi2;
			}
			return null;
		}
		/// <summary>
		/// actions are local to the root class. Its Actions node only contains
		/// actions for the host and executed by the component: 
		/// act.ClassId=mc.ClassId && act.ExecuterMemberId=mc.MemberId
		/// </summary>
		/// <param name="act"></param>
		/// <param name="isNewAction"></param>
		public override void OnActionChanged(UInt32 classId, IAction act, bool isNewAction)
		{
			MemberComponentId mc = this.OwnerPointer as MemberComponentId;
			if (classId == mc.ClassId && act.ExecuterMemberId == mc.MemberId)
			{
				//show it in P/M
				TreeNodePME pmeNode = this.GetMemberNode(act.IsStatic);
				if (pmeNode != null)
				{
					TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
					//show it in Actions Node
					TreeNodeActionCollection actNodes = pmeNode.GetActionsNode();
					if (actNodes != null)
					{
						actNodes.ResetNextLevel(tv);
						actNodes.OnShowActionIcon();
					}
					//show it in P/M
					IProperty prop = null;
					SetterPointer sp = act.ActionMethod as SetterPointer;
					if (sp != null)
					{
						prop = sp.SetProperty;
					}
					if (prop != null)
					{
						//locate the property node
						TreeNodeObject pNode = pmeNode.FindObjectNode(prop);
						if (pNode != null)
						{
							pNode.OnActionCreated();
						}
					}
					else
					{
						MethodPointer mp = act.ActionMethod as MethodPointer;
						if (mp != null)
						{
							TreeNodeObject mNode = pmeNode.FindObjectNode(mp);
							if (mNode != null)
							{
								mNode.OnActionCreated();
							}
						}
					}
				}
			}
		}
	}
	/// <summary>
	/// custom class instance
	/// </summary>
	public class TreeNodeClassComponentCust : TreeNodeClassComponent
	{
		#region fields and constructors
		public TreeNodeClassComponentCust(TreeViewObjectExplorer tv, MemberComponentIdCustom objectPointer, UInt32 scopeMethodId)
			: base(tv, false, objectPointer, scopeMethodId)
		{
			ClassInstancePointer cr = objectPointer.Pointer;
			if (cr != null)
			{
				if (cr.RootPointer != null && cr.RootPointer.ObjectList != null)
				{
					this.ImageIndex = TreeViewObjectExplorer.GetTypeIcon(cr.Definition);
				}
				else
				{
					Image img = cr.ImageIcon;
					if (img != null)
					{
						this.ImageIndex = TreeViewObjectExplorer.AddBitmap(img);
					}
				}
			}
			this.SelectedImageIndex = this.ImageIndex;
			List<TreeNodeLoader> ls = GetLoaderNodes();
			if (ls != null)
			{
				foreach (TreeNodeLoader l in ls)
				{
					Nodes.Add(l);
				}
			}
		}
		#endregion
		#region Properties
		public UInt32 DefinitionClassId
		{
			get
			{
				MemberComponentIdCustom cc = (MemberComponentIdCustom)(this.OwnerPointer);
				return cc.DefinitionClassId;
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}
		#endregion
		#region Methods
		public void Definition_ComponentsLoaded(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			ResetNextLevel(tv);
		}
		public void ResetPointer(ClassPointer pointer)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			MemberComponentIdCustom mc = (MemberComponentIdCustom)(this.OwnerPointer);
			mc.ReloadInstance();
			ResetNextLevel(tv);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoader(this.IsStatic));
			return l;
		}
		public override void HandleActionModified(UInt32 classId, IAction act, bool isNewAction)
		{
			OnActionChanged(classId, act, isNewAction);
		}
		/// <summary>
		/// actions are local to the root class. Its Actions node only contains
		/// actions for the host and executed by it: act.ClassId=mc.ClassId && act.ExecuterClassId=ip.ClassId
		/// If it is not a class-instance-pointer, act.ClassId=act.ExecuterClassId=mc.ClassId
		/// </summary>
		/// <param name="act"></param>
		/// <param name="isNewAction"></param>
		public override void OnActionChanged(UInt32 classId, IAction act, bool isNewAction)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			MemberComponentIdCustom mc = this.OwnerPointer as MemberComponentIdCustom;
			ClassInstancePointer ip = mc.Pointer;
			if (mc.ClassId == classId && ip.ClassId == act.ExecuterClassId)
			{
				if (!ip.RootPointer.InBatchSaving)
				{
					ip.RootPointer.ReloadEventActions(act);
				}
				TreeNodePME pmeNode = this.GetMemberNode(act.IsStatic);
				if (pmeNode != null)
				{
					//show it in Actions Node
					TreeNodeActionCollection actNodes = pmeNode.GetActionsNode();
					if (actNodes != null)
					{
						actNodes.ResetNextLevel(tv);
						actNodes.OnShowActionIcon();
					}
					//show it in P/M
					TreeNodeAction na = pmeNode.FindActionNode(act);
					if (na != null)
					{
						TreeNodeObject op = na.Parent as TreeNodeObject;
						op.ResetNextLevel(tv);
						op.OnShowActionIcon();
					}
					else
					{
						IObjectIdentity top = null;
						//the parent node of the action has not been loaded
						IObjectIdentity mop = act.ActionMethod as IObjectIdentity;
						if (mop == null)
						{
							throw new DesignerException("{0} is not an IObjectIdentity", act.ActionMethod.GetType());
						}
						else
						{
							top = DesignUtil.GetTopMemberPointer(mop);
							if (top == null)
							{
								throw new DesignerException("top member not found for {0}", act.ActionMethod.GetType());
							}
						}
						if (top != null)
						{
							TreeNodeObjectCollection oc = pmeNode.GetCollectionNode(top);
							if (oc != null)
							{
								oc.ResetNextLevel(tv);
								oc.OnShowActionIcon();
							}
						}
					}
				}
			}
		}
		public TreeNodePME GetPointerMembersNode(bool forStatic)
		{
			if (forStatic)
			{
				TreeNodePMEPointerStatic tnpp = null;
				for (int i = 0; i < Nodes.Count; i++)
				{
					tnpp = Nodes[i] as TreeNodePMEPointerStatic;
					if (tnpp != null)
					{
						break;
					}
				}
				return tnpp;
			}
			else
			{
				TreeNodePMEPointer tnpp = null;
				for (int i = 0; i < Nodes.Count; i++)
				{
					tnpp = Nodes[i] as TreeNodePMEPointer;
					if (tnpp != null)
					{
						break;
					}
				}
				return tnpp;
			}
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			if (NextLevelLoaded)
			{
				base.ResetNextLevel(tv);
				TreeChild = null;
				List<TreeNode> l = new List<TreeNode>();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeClassComponent tncc = Nodes[i] as TreeNodeClassComponent;
					if (tncc != null)
					{
						l.Add(tncc);
					}
				}
				foreach (TreeNode tn in l)
				{
					Nodes.Remove(tn);
				}
				Nodes.Add(new CLoader(this.IsStatic));
			}
		}
		/// <summary>
		/// Monitoring definition class changes
		/// </summary>
		private EventHandlerOwnerChanged _ownerChangedhandler;
		public EventHandlerOwnerChanged OwnerChangedHandler
		{
			get
			{
				if (_ownerChangedhandler == null)
				{
					_ownerChangedhandler = new EventHandlerOwnerChanged(TreeChill_OwnerChanged);
				}
				return _ownerChangedhandler;
			}
		}
		private void TreeChill_OwnerChanged(object sender, EventArgsOwnerChanged e)
		{
			if (this.NextLevelLoaded)
			{
				//remove from the old parent
				TreeNodeClass tncc = this.FindComponentNode(e.OldOwner);
				if (tncc != null)
				{
					foreach (TreeNode tn in tncc.Nodes)
					{
						TreeNodeClassComponent t = tn as TreeNodeClassComponent;
						if (t != null)
						{
							if (t.OwnerPointer.ObjectInstance == sender)
							{
								tncc.Nodes.Remove(t);
								break;
							}
						}
					}
				}
				//add to the new parent
				tncc = this.FindComponentNode(e.NewOwner);
				if (tncc != null)
				{
					if (tncc.NextLevelLoaded)
					{
						TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
						UInt32 scopeId = 0;
						if (tv != null)
						{
							scopeId = tv.ScopeMethodId;
							ClassPointer root = (ClassPointer)(OwnerPointer);
							TreeNodeClassComponent t0 = tv.CreateComponentTreeNode(root, sender, root.ObjectList.GetObjectID(sender), scopeId, SelectionTarget, TargetScope, EventScope);
							t0.TreeChild = root.ObjectList.TreeRoot.SearchChildByOwner(sender);
							tncc.Nodes.Add(t0);
						}
					}
				}
			}
		}
		#endregion
		#region class CLoader
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}
			//load components
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassComponentCust custNode = parentNode as TreeNodeClassComponentCust;
				MemberComponentIdCustom cc = (MemberComponentIdCustom)(parentNode.OwnerPointer);
				ObjectIDmap map = cc.Pointer.Definition.ObjectList;
				if (map != null)
				{
					UInt32 scopeId = parentNode.ScopeMethodId;
					if (map.Count == 0 && map.ClassId != cc.ClassId)
					{
						//load Root Object
						if (map.Reader == null)
						{
							XmlObjectReader reader = new XmlObjectReader(map, ClassPointer.OnAfterReadRootComponent, ClassPointer.OnRootComponentCreated);
							map.SetReader(reader);
						}
						map.LoadObjects();//of);
						if (map.Reader.HasErrors)
						{
							MathNode.Log(map.Reader.Errors);
							map.Reader.ResetErrors();
						}
					}
					map.TreeRoot.OwnerChanged -= custNode.OwnerChangedHandler;
					map.TreeRoot.OwnerChanged += custNode.OwnerChangedHandler;
					custNode.TreeChild = map.TreeRoot;
					SortedList<string, TreeNodeClassComponent> ts = new SortedList<string, TreeNodeClassComponent>();
					foreach (Tree t in custNode.TreeChild)
					{
						TreeNodeClassComponent tncc = tv.CreateComponentTreeNode(cc, t.Owner, map.GetObjectID(t.Owner), scopeId, parentNode.SelectionTarget, parentNode.TargetScope, parentNode.EventScope);
						tncc.TreeChild = t;
						ts.Add(tncc.Text, tncc);
					}
					IEnumerator<KeyValuePair<string, TreeNodeClassComponent>> ie = ts.GetEnumerator();
					while (ie.MoveNext())
					{
						parentNode.Nodes.Add(ie.Current.Value);
					}
				}
			}
		}
		#endregion
	}
	public class TreeNodeDefaultInstance : TreeNodeClassComponentCust
	{
		public TreeNodeDefaultInstance(TreeViewObjectExplorer tv, MemberComponentIdCustom objectPointer, UInt32 scopeMethodId)
			: base(tv, objectPointer, scopeMethodId)
		{
			this.ImageIndex = TreeViewObjectExplorer.IMG_DEFINST;
			this.SelectedImageIndex = this.ImageIndex;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
	}
	/// <summary>
	/// show a design object
	/// </summary>
	public abstract class TreeNodeObject : TreeNodeExplorer, ITreeNodeObject
	{
		#region fields and constructors
		IObjectIdentity _objPointer;
		public TreeNodeObject(bool isForStatic, IObjectIdentity objectPointer)
			: base(isForStatic)
		{
			_objPointer = objectPointer;
			if (_objPointer != null)
			{
				IObjectPointer op = _objPointer as IObjectPointer;
				if (op != null)
				{
					Text = op.DisplayName;
				}
			}
		}
		public TreeNodeObject(IObjectIdentity objectPointer)
		{
			_objPointer = objectPointer;
		}
		#endregion
		#region virtual properties

		[ReadOnly(true)]
		[Browsable(false)]
		public virtual EnumActionMethodType ActionMethodType
		{
			get
			{
				if (this.IsStatic)
				{
					return EnumActionMethodType.Static;
				}
				return EnumActionMethodType.Instance;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public bool DoNotRemove
		{
			get;
			set;
		}
		[Browsable(false)]
		public abstract EnumPointerType NodeType { get; }
		[Browsable(false)]
		public abstract EnumObjectDevelopType ObjectDevelopType { get; }
		public virtual bool IsInDesign
		{
			get
			{
				IRootNode c = this.TopLevelRootClassNode;
				if (c != null && c.ViewersHolder != null)
				{
					return true;
				}
				TreeView v = this.TreeView;
				if (v != null)
				{
					for (int i = 0; i < v.Nodes.Count; i++)
					{
						c = v.Nodes[i] as IRootNode;
						if (c != null && c.ViewersHolder != null)
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		public virtual bool IncludeStaticOnly
		{
			get
			{
				TreeNodeClassRoot root = TopLevelRootClassNode;
				if (root != null)
				{
					if (root.ClassData.RootClassID.IsStatic)
					{
						return false;
					}
				}
				if (this.OwnerPointer is ClassPointer)
				{
					return this.RootClassNode.IncludeStaticOnly;
				}
				return false;
			}
		}
		#endregion
		#region properties
		public bool HasActions { get; set; }
		public ClassPointer MainClassPointer
		{
			get
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					return tv.RootId;
				}
				return null;
			}
		}
		public ClassPointer RootObjectId
		{
			get
			{
				IObjectPointer obj = OwnerPointer;
				if (obj != null)
				{
					return obj.RootPointer;
				}
				return null;
			}
		}
		public ClassPointer RootHost
		{
			get
			{
				IObjectPointer obj = OwnerPointer;
				if (obj != null)
				{
					while (obj.IdentityOwner != null && obj.IdentityOwner is IObjectPointer)
					{
						if (obj == obj.IdentityOwner)
						{
							return obj.RootPointer;
						}
						obj = obj.IdentityOwner as IObjectPointer;
					}
					return obj as ClassPointer;
				}
				return null;
			}
		}
		public IObjectIdentity OwnerIdentity
		{
			get
			{
				return _objPointer;
			}
		}
		public IObjectPointer OwnerPointer
		{
			get
			{
				return _objPointer as IObjectPointer;
			}
		}
		public virtual IObjectIdentity MemberOwner
		{
			get
			{
				return _objPointer;
			}
		}
		#endregion
		#region public methods
		public bool CheckIsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.CheckTarget != null)
				{
					BoolEventArgs e = new BoolEventArgs(false);
					tv.CheckTarget(this, e);
					return e.Result;
				}
				else
				{
					return IsTargeted();
				}
			}
			return false;
		}
		public void ResetObjectPointer(IObjectIdentity pointer)
		{
			_objPointer = pointer;
		}
		public void AddSortedNodes(SortedList<string, TreeNode> newNodes)
		{
			TreeNode[] nodes = new TreeNode[newNodes.Count];
			IEnumerator<KeyValuePair<string, TreeNode>> ie = newNodes.GetEnumerator();
			int k = 0;
			while (ie.MoveNext())
			{
				nodes[k++] = ie.Current.Value;
			}
			Nodes.AddRange(nodes);
		}
		public void ShowActionIcon()
		{
			OnShowActionIcon();
			TreeNodeObject p = this.Parent as TreeNodeObject;
			if (p != null)
			{
				p.ShowActionIcon();
			}
		}
		/// <summary>
		/// it does not expand next levels. it goes through expanded levels to locate the node
		/// </summary>
		/// <param name="ownerStack"></param>
		/// <returns></returns>
		public virtual TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeObject tn = Nodes[i] as TreeNodeObject;
					if (tn != null)
					{
						if (o.IsSameObjectRef(tn.OwnerPointer))
						{
							if (ownerStack.Count == 0)
							{
								return tn;
							}
							else
							{
								return tn.LocateObjectNode(ownerStack);
							}
						}
					}
				}
			}
			return null;
		}
		public virtual TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			if (_objPointer != null && pointer != null)
			{
				if (_objPointer.IsSameObjectRef(pointer))
				{
					return this;
				}
			}
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeObject node = Nodes[i] as TreeNodeObject;
				if (node != null)
				{
					if (pointer.IsSameObjectRef(node.OwnerIdentity))
					{
						return node;
					}
				}
			}
			return null;
		}
		#endregion
		#region Virtual methods
		public abstract bool CanContain(IObjectIdentity objectPointer);
		public virtual void ResetImage()
		{
		}
		public virtual TreeNodeAction FindActionNode(IAction act)
		{
			if (this.ObjectDevelopType != EnumObjectDevelopType.Both && act.ObjectDevelopType != EnumObjectDevelopType.Both && act.ObjectDevelopType != this.ObjectDevelopType)
			{
				return null;
			}
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			UInt32 scopeId = ScopeMethodId;
			if (IncludeAction(act, tv, scopeId, false))
			{
				this.OnShowActionIcon();
				this.LoadNextLevel();
				if (IncludeAction(act, tv, scopeId, true))
				{
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeAction na = Nodes[i] as TreeNodeAction;
						if (na != null)
						{
							if (na.Action.WholeActionId == act.WholeActionId)
							{
								return na;
							}
						}
					}
					TreeNodeAction n = new TreeNodeAction(act.IsStatic, act);
					this.Nodes.Add(n);
					return n;
				}
				else
				{
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeObject na = Nodes[i] as TreeNodeObject;
						if (na != null && !(na is TreeNodeActionCollection))
						{
							TreeNodeAction n = na.FindActionNode(act);
							if (n != null)
							{
								return n;
							}
						}
					}
				}
			}
			return null;
		}
		public virtual void OnActionCreated()
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			ResetNextLevel(tv);
			ShowActionIcon();
		}
		/// <summary>
		/// determine it belongs to customer or library trail
		/// </summary>
		/// <returns></returns>
		public EnumObjectDevelopType GetRootObjectDevelopType()
		{
			EnumObjectDevelopType ret = this.ObjectDevelopType;
			IObjectIdentity oi = this.OwnerIdentity;
			while (oi != null)
			{
				if (oi is IClass)
				{
					break;
				}
				ret = oi.ObjectDevelopType;
				oi = oi.IdentityOwner;
			}
			return ret;
		}
		public bool IsBasePmeStatic
		{
			get
			{
				IClass c = this.OwnerPointer as IClass;
				if (c != null)
				{
					TypePointer tp = c as TypePointer;
					if (tp != null)
					{
						return true;
					}
					DataTypePointer dtp = c as DataTypePointer;
					if (dtp != null)
					{
						if (dtp.IsLibType)
						{
							return true;
						}
					}
					return c.IsStatic;
				}
				TreeNodeObject on = this;
				IObjectPointer o = this.OwnerPointer;
				while (o != null)
				{
					c = o.Owner as IClass;
					if (c != null)
					{
						return o.IsStatic;
					}
					if (on.Parent == null)
					{
						return on.IsStatic;
					}
					on = on.Parent as TreeNodeObject;
					if (on != null)
					{
						o = on.OwnerPointer;
					}
					else
					{
						break;
					}
				}
				return false;
			}
		}
		public IClass GetHolder()
		{
			IClass c = this.OwnerPointer as IClass;
			if (c != null)
				return c;
			TreeNodeObject p = this.Parent as TreeNodeObject;
			while (p != null)
			{
				c = p.OwnerPointer as IClass;
				if (c != null)
					return c;
				p = p.Parent as TreeNodeObject;
			}
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null && tv.RootClassNode != null)
			{
				return tv.RootClassNode.ClassData.RootClassID;
			}
			return null;
		}
		public virtual void ShowText()
		{
			IObjectPointer op = _objPointer as IObjectPointer;
			if (op != null)
			{
				Text = op.DisplayName;
			}
		}
		public virtual MenuItem[] GetContextMenuItems(bool readOnly)
		{
			return null;
		}
		public bool IsCollectionNode
		{
			get
			{
				return (this is TreeNodeObjectCollection);
			}
		}
		public virtual bool IsPointerNode { get { return true; } }
		/// <summary>
		/// P/M/E nodes may have actions associated with them. 
		/// this function determines whether a node has actions associated with it.
		/// the caller should already have determined that the action matches cutomer/library condition for the node
		/// and the static/non-static matching; scope method is also checked by the caller
		/// </summary>
		/// <param name="act"></param>
		/// <param name="sameLevel"></param>
		/// <returns></returns>
		public virtual bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				//it is a method specific action
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}

			IObjectIdentity startPointer = DesignUtil.GetActionOwner(act);
			if (startPointer != null)
			{
				if (sameLevel)
				{
					SubMethodInfoPointer smip = startPointer as SubMethodInfoPointer;
					if (smip != null)
					{
						return smip.ActionOwner.MethodOwner.Owner.IsSameObjectRef(this.MemberOwner);
					}
					else
					{
						return startPointer.IsSameObjectRef(this.MemberOwner);
					}
				}
				else
				{
					while (startPointer != null)
					{
						if (startPointer.IsSameObjectRef(this.MemberOwner))
						{
							return true;
						}
						//move up one level
						ICustomPointer p = startPointer as ICustomPointer;
						if (p != null)
						{
							startPointer = p.Holder;
						}
						else
						{
							startPointer = startPointer.IdentityOwner;
						}
					}
				}
			}
			return false;
		}
		public virtual IAction CreateNewAction()
		{
			return null;
		}

		public abstract List<TreeNodeLoader> GetLoaderNodes();
		public virtual bool IsTargeted()
		{
			return false;
		}
		protected void miResetNextLevels(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				ResetNextLevel(tv);
			}
		}
		public virtual void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			if (NextLevelLoaded)
			{
				List<TreeNodeLoader> loaders = GetLoaderNodes();
				if (loaders != null && loaders.Count > 0)
				{
					this.Nodes.Clear();
					foreach (TreeNodeLoader l in loaders)
					{
						this.Nodes.Add(l);
					}
					this.NextLevelLoaded = false;
					this.Collapse();
				}
			}
		}
		public virtual void OnShowActionIcon()
		{
			HasActions = true;
		}
		#endregion
		#region protected methods
		/// <summary>
		/// context menu for creating action
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void OnCreateNewAction(object sender, EventArgs e)
		{
			CreateAction();
		}
		public virtual TreeNodeObject GetActionOwnerNode()
		{
			return null;
		}
		public IAction CreateAction()
		{
			try
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					TreeNodeClassRoot ownerNode = tv.DesignerRootNode;
					if (ownerNode != null)
					{
						TreeNodeHtmlElement tnh = this.GetActionOwnerNode() as TreeNodeHtmlElement;
						if (tnh != null)
						{
							EnumElementValidation eev = tnh.HtmlElement.Validate(tv.FindForm());
							switch (eev)
							{
								case EnumElementValidation.Fail:
									return null;
								case EnumElementValidation.New: //guid created
									break;
								case EnumElementValidation.Pass: // existing guid
									break;
							}
						}
						IAction act = this.CreateNewAction();
						if (act != null)
						{
							act.ScopeMethod = tv.ScopeMethod;
							act.ActionHolder = tv.ActionsHolder;
							AssignHandler ah = act as AssignHandler;
							if (ah != null)
							{
								return ah;
							}
							if (ownerNode.ClassData.RootClassID.CreateNewAction(act, ownerNode.ViewersHolder.Loader.Writer, tv.ScopeMethod, tv.FindForm()))
							{
								this.Expand();
								TreeNodeAction na = null;
								for (int i = 0; i < this.Nodes.Count; i++)
								{
									TreeNodeAction na0 = this.Nodes[i] as TreeNodeAction;
									if (na0 != null)
									{
										if (na0.Action.WholeActionId == act.WholeActionId)
										{
											this.ShowActionIcon();
											tv.SelectedNode = na0;
											na0.EnsureVisible();
											na = na0;
											break;
										}
									}
								}
								if (na == null)
								{
									na = new TreeNodeAction(act.IsStatic, act);
									this.Nodes.Add(na);
									this.ShowActionIcon();
									tv.SelectedNode = na;
									na.EnsureVisible();
								}
								return act;
							}
						}
					}
				}
			}
			catch (Exception err)
			{
				MathNode.Log(this.FindForm(),err);
			}
			return null;
		}
		#endregion
	}
	public class TreeNodeNullObject : TreeNodeObject
	{
		public TreeNodeNullObject()
			: base(false, new NullObjectPointer())
		{
			Text = "null";
			ImageIndex = TreeViewObjectExplorer.IMG_VOID;
			SelectedImageIndex = ImageIndex;
		}
		public TreeNodeNullObject(ClassPointer root)
			: base(false, new NullObjectPointer(root))
		{
			Text = "null";
			ImageIndex = TreeViewObjectExplorer.IMG_VOID;
			SelectedImageIndex = ImageIndex;
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Library; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}

		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					)
				{
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Method)
				{
					return true;
				}
			}
			return false;
		}
	}
	class TreeNodeAssembly : TreeNodeObject
	{
		private Assembly _a;
		public TreeNodeAssembly(string name)
			: base(false, new AssemblyPointer(name))
		{
			Text = name;
			ImageIndex = TreeViewObjectExplorer.IMG_Assembly;
			SelectedImageIndex = ImageIndex;
			Nodes.Add(new TreeNodeAssemblyLoader());
		}
		public TreeNodeAssembly(Assembly assemnly)
			: this(assemnly.FullName)
		{
			_a = assemnly;
		}
		public Assembly Assembly
		{
			get
			{
				if (_a == null)
				{
					_a = Assembly.Load(Text);
				}
				return _a;
			}
		}
		class TreeNodeAssemblyLoader : TreeNodeLoader
		{
			public TreeNodeAssemblyLoader()
				: base(true)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				SortedList<string, TreeNode> nodes = new SortedList<string, TreeNode>();
				TreeNodeAssembly tna = parentNode as TreeNodeAssembly;
				Assembly a = tna.Assembly;
				Type[] tps = VPLUtil.GetExportedTypes(a);
				for (int i = 0; i < tps.Length; i++)
				{
					if (tps[i].IsPublic)
					{
						TreeNode n;
						if (tps[i].IsEnum)
						{
							n = new TreeNodeEnumValues(tps[i]);
						}
						else
						{
							n = new TreeNodeClassType(tv, tps[i], parentNode.ScopeMethodId);
						}
						TreeNode ndExist;
						if (nodes.TryGetValue(n.Text, out ndExist))
						{
							TreeNodeClassType ndClassExist = ndExist as TreeNodeClassType;
							if (ndClassExist != null)
							{
								DataTypePointer dtpExist = ndClassExist.OwnerPointer as DataTypePointer;
								if (dtpExist != null)
								{
									Type tExist = dtpExist.BaseClassType;
									if (tExist != null)
									{
										if (tExist.Equals(tps[i]))
										{
											continue;
										}
										else
										{
											ndClassExist.Text = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", VPLUtil.GetTypeDisplay(tExist), tExist.AssemblyQualifiedName);
										}
									}
								}
							}
						}
						if (nodes.ContainsKey(n.Text))
						{
							uint mx = 2;
							while (true)
							{
								string nm = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", n.Text, mx.ToString("x", CultureInfo.InvariantCulture));
								if (nodes.ContainsKey(nm))
								{
									mx++;
								}
								else
								{
									nodes.Add(nm, n);
									break;
								}
							}
							n.Text = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", VPLUtil.GetTypeDisplay(tps[i]), tps[i].AssemblyQualifiedName);
						}
						else
						{
							nodes.Add(n.Text, n);
						}
					}
				}
				IEnumerator<KeyValuePair<string, TreeNode>> ie = nodes.GetEnumerator();
				while (ie.MoveNext())
				{
					parentNode.Nodes.Add(ie.Current.Value);
				}
			}
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loaders = new List<TreeNodeLoader>();
			loaders.Add(new TreeNodeAssemblyLoader());
			return loaders;
		}
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Library; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}
	}
	public abstract class TreeNodeObjectCollection : TreeNodeObject
	{
		#region fields and constructors
		protected TreeNodeObject _parentNode;
		private bool _adjustedActIcon;
		private UInt32 _scopeMethodId;
		public TreeNodeObjectCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectIdentity objectPointer, UInt32 scopeMethodId)
			: base(isForStatic, objectPointer)
		{
			_parentNode = parentNode;
			_scopeMethodId = scopeMethodId;
			if (CheckActions && (parentNode == null || parentNode is TreeNodeClassRoot))
			{
				adjustActionIcon(scopeMethodId, tv, parentNode);
			}
			else
			{
				ShowIconNoAction();
			}
		}
		#endregion
		protected virtual bool CheckActions
		{
			get
			{
				return true;
			}
		}
		#region Methods
		protected abstract void ShowIconNoAction();
		private void adjustActionIcon(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			DesignUtil.LogIdeProfile("adjustActionIcon for {0}",this.GetType().Name);
			_adjustedActIcon = true;
			if (!CheckActions)
				return;
			if (HasAction(scopeMethodId, tv, parentNode))
			{
				DesignUtil.LogIdeProfile("show action icon");
				OnShowActionIcon();
			}
			else
			{
				DesignUtil.LogIdeProfile("show no-action icon");
				ShowIconNoAction();
			}
			DesignUtil.LogIdeProfile("finish adjust action icon");
		}
		public void AdjustActionIcon()
		{
			if (!_adjustedActIcon)
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					adjustActionIcon(_scopeMethodId, tv, _parentNode);
				}
			}
		}
		public virtual bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			TreeNodeClassNonStatic rt = parentNode as TreeNodeClassNonStatic;
			if (rt != null)
			{
				if (!rt.IsMainRoot)
				{
					return false;
				}
			}
			//use the root to get all actions
			ClassPointer c;
			if (tv != null && tv.RootClassNode != null)
			{
				c = tv.RootClassNode.ClassData.RootClassID;
			}
			else
			{
				c = this.RootHost;
			}
			if (c != null)
			{
				Dictionary<UInt32, IAction> aList = c.GetActions();
				if (aList != null && aList.Count > 0)
				{
					//it must be for the immediate PME, but at this time the parent can be null
					EnumObjectDevelopType ot;
					if (this.OwnerIdentity is IClass)
					{
						ot = this.ObjectDevelopType;
					}
					else
					{
						ot = DesignUtil.GetBaseObjectDevelopType(this.OwnerIdentity);
					}
					EnumActionMethodType at = parentNode.ActionMethodType; ;
					foreach (IAction a in aList.Values)
					{
						if (a != null && a.IsValid)
						{
							if (at == EnumActionMethodType.Static)
							{
								if (!a.IsStatic)
								{
									continue;
								}
							}
							if (at == EnumActionMethodType.Instance)
							{
								if (a.IsStatic)
								{
									continue;
								}
							}
							if ((ot == EnumObjectDevelopType.Both || ot == a.ObjectDevelopType))
							{
								if (this.IncludeAction(a, tv, scopeMethodId, false)) //the action is for this object and lower member-objects
								{
									if (!this.IncludeAction(a, tv, scopeMethodId, true)) //actions for a collection is for the next level
									{
										return true;
									}
								}
							}
						}
					}
				}
			}
			return false;
		}
		protected virtual bool OnCheckIncludeAction(IAction act)
		{
			return true;
		}
		/// <summary>
		/// P/M/E nodes may have actions associated with them. 
		/// this function determines whether a node has actions associated with it.
		/// the caller should already have determined that the action matches cutomer/library condition for the node
		/// and the static/non-static matching
		/// </summary>
		/// <param name="act"></param>
		/// <param name="sameLevel"></param>
		/// <returns></returns>
		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			if (!OnCheckIncludeAction(act))
			{
				return false;
			}
			IObjectIdentity startPointer = DesignUtil.GetActionOwner(act);
			if (startPointer != null)
			{
				if (sameLevel)
				{
					//collection is not showing the same level actions
				}
				else
				{
					IObjectIdentity lowerLevel = startPointer;
					ICustomPointer cp = lowerLevel as ICustomPointer;
					if (cp != null)
					{
						startPointer = cp.Holder;
					}
					else
					{
						startPointer = startPointer.IdentityOwner;
					}
					while (startPointer != null)
					{
						if (lowerLevel.PointerType == this.NodeType)//same P, M, or E pointer type
						{
							if (startPointer.IsSameObjectRef(this.OwnerIdentity)) //same owner
							{
								return true;
							}
							if (act.IsStatic && startPointer.IdentityOwner == null)
							{
								TypePointer dtp = startPointer as TypePointer;
								if (dtp != null)
								{
									ClassPointer cpx = this.OwnerIdentity as ClassPointer;
									if (cpx != null)
									{
										if (dtp.ClassType != null && dtp.ClassType.Equals(cpx.BaseClassType))
										{
											return true;
										}
									}
								}
							}
						}
						//move up one level
						lowerLevel = startPointer;
						cp = lowerLevel as ICustomPointer;
						if (cp != null)
						{
							startPointer = cp.Holder;
						}
						else
						{
							startPointer = startPointer.IdentityOwner;
						}
					}
				}
			}
			return false;
		}
		public TreeNodeObject FindObjectNode(IObjectIdentity pointer, Stack<IObjectIdentity> pointerStack)
		{
			Type tp = TreeViewObjectExplorer.TreeNodeType(pointer);
			if (tp != null)
			{
				LoadNextLevel();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeObject ct = Nodes[i] as TreeNodeObject;
					if (ct != null && tp.Equals(Nodes[i].GetType()))
					{
						if (pointer.IsSameObjectRef(ct.OwnerIdentity))
						{
							if (pointerStack != null && pointerStack.Count > 0)
							{
								IObjectIdentity p = pointerStack.Pop();
								TreeNodeObjectCollection toc = TreeViewObjectExplorer.GetCollectionNode(ct, p);
								if (toc != null)
								{
									TreeNodeObject tn = toc.FindObjectNode(p, pointerStack);
									if (tn != null)
									{
										return tn;
									}
								}
								return ct;
							}
							else
							{
								return ct;
							}
						}
					}
				}
			}
			return null;
		}
		#endregion
	}
	public class TreeNodeActionCollection : TreeNodeObjectCollection
	{
		public TreeNodeActionCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			Text = "Actions";
			List<TreeNodeLoader> ls = GetLoaderNodes();
			if (ls != null && ls.Count > 0)
			{
				foreach (TreeNodeLoader l in ls)
				{
					this.Nodes.Add(l);
				}
			}
		}
		public override EnumActionMethodType ActionMethodType
		{
			get
			{
				TreeNodePMEPointer pme0 = Parent as TreeNodePMEPointer;
				if (pme0 != null)
				{
					return EnumActionMethodType.Instance;
				}
				TreeNodePMEPointerStatic pme1 = Parent as TreeNodePMEPointerStatic;
				if (pme1 != null)
				{
					return EnumActionMethodType.Static;
				}
				if (_parentNode != null)
				{
					pme0 = _parentNode as TreeNodePMEPointer;
					if (pme0 != null)
					{
						return EnumActionMethodType.Instance;
					}
					pme1 = _parentNode as TreeNodePMEPointerStatic;
					if (pme1 != null)
					{
						return EnumActionMethodType.Static;
					}
				}
				TreeNodeObject tno = Parent as TreeNodeObject;
				if (tno != null)
				{
					return tno.ActionMethodType;
				}
				tno = _parentNode as TreeNodeObject;
				if (tno != null)
				{
					return tno.ActionMethodType;
				}
				return EnumActionMethodType.Unknown;
			}
		}
		public override IRootNode RootClassNode
		{
			get
			{
				IRootNode root = base.RootClassNode;
				if (root == null)
				{
					if (TreeView != null)
					{
						for (int i = 0; i < TreeView.Nodes.Count; i++)
						{
							root = TreeView.Nodes[i] as IRootNode;
							if (root != null)
							{
								break;
							}
						}
					}
				}
				return root;
			}
		}
		public override TreeNodeClassRoot TopLevelRootClassNode
		{
			get
			{
				TreeNodeClassRoot root = base.TopLevelRootClassNode;
				if (root == null)
				{
					if (TreeView != null)
					{
						for (int i = 0; i < TreeView.Nodes.Count; i++)
						{
							root = TreeView.Nodes[i] as TreeNodeClassRoot;
							if (root != null)
							{
								break;
							}
						}
					}
				}
				return root;
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Action; }
		}
		public override bool IncludeAction(IAction a, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			ClassPointer rcp = null;
			if (tv != null)
			{
				rcp = tv.RootId;
			}
			if (rcp == null)
			{
				rcp = this.RootHost;
			}
			if (rcp != null)
			{
				if (a.ClassId != rcp.ClassId)
				{
					return false;
				}
			}
			if (a.ScopeMethodId != 0)
			{
				if (a.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			EnumActionMethodType at = ActionMethodType;
			if (at == EnumActionMethodType.Instance)
			{
				if (a.IsStatic)
				{
					return false;
				}
			}
			if (at == EnumActionMethodType.Static)
			{
				if (!a.IsStatic)
				{
					return false;
				}
			}
			IClass c = this.OwnerPointer as IClass;
			if (c != null)
			{
				ClassPointer cp = this.OwnerPointer as ClassPointer;
				if (cp != null)
				{
					if (rcp != null)
					{
						if (rcp.ClassId == cp.ClassId)
						{
							return true;
						}
					}
					return (cp.ClassId == a.ExecuterClassId);
				}
				else
				{
					MemberComponentIdCustom mc = this.OwnerPointer as MemberComponentIdCustom;
					if (mc != null)
					{
						ClassInstancePointer cip = (ClassInstancePointer)mc.ObjectInstance;
						if (a.ExecuterClassId == cip.ClassId)
						{
							if (a.ExecuterMemberId == cip.MemberId)
							{
								return true;
							}
						}
					}
					else
					{
						if (a.ExecuterClassId == c.ClassId)
						{
							if (a.ExecuterMemberId == c.MemberId)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			ClassPointer r = null;
			if (tv != null)
			{
				r = tv.RootId;
			}
			if (r == null)
			{
				MemberComponentId mc = this.OwnerPointer as MemberComponentId;
				if (mc != null)
				{
					r = mc.RootHost;
				}
				else
				{
					r = RootObjectId;
				}
			}
			if (r != null)
			{
				Dictionary<UInt32, IAction> acts = r.GetActions();
				foreach (IAction a in acts.Values)
				{
					if (a != null)
					{
						if (a.ScopeMethodId != 0)
						{
							if (a.ScopeMethodId != scopeMethodId)
							{
								continue;
							}
						}
						if (IncludeAction(a, tv, scopeMethodId, false))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_ACTIONCOLLECTION;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_ACTIONCOLLECTION;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_A_ACTIONCOLLECTION;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is IAction);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new ActionLoader(this.IsStatic));
			return lst;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				MenuItem[] mi = new MenuItem[4];
				mi[0] = new MenuItemWithBitmap("Create action", CreateAction_Click, Resources._action.ToBitmap());
				mi[1] = new MenuItemWithBitmap("Create data transfer action", dataTransfer_Click, Resources._dataTransfer.ToBitmap());
				mi[2] = new MenuItem("-");
				mi[3] = new MenuItemWithBitmap("Refresh", miResetNextLevels, Resources._refresh.ToBitmap());
				return mi;
			}
			return null;
		}
		private void dataTransfer_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				ClassPointer root = r.ClassData.RootClassID;
				MethodDataTransfer mdt = new MethodDataTransfer(root);
				IAction a = r.ClassData.RootClassID.CreateNewAction(mdt, r.ViewersHolder.Loader.Writer, tv.ScopeMethod, tv.ActionsHolder, tv.FindForm());
				if (a != null)
				{
					tv.ForceLoadNextLevel(this);
					this.Expand();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeAction na = this.Nodes[i] as TreeNodeAction;
						if (na != null)
						{
							if (na.Action.WholeActionId == a.WholeActionId)
							{
								tv.SelectedNode = na;
								na.EnsureVisible();
								break;
							}
						}
					}
				}
			}
		}
		private void CreateAction_Click(object sender, EventArgs e)
		{
			IAction a = null;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				object m = DesignUtil.SelectMethod(r.ViewersHolder.Loader, this.OwnerPointer, tv.FindForm());
				if (m != null)
				{
					a = r.ClassData.RootClassID.CreateNewAction(m, r.ViewersHolder.Loader.Writer, tv.ScopeMethod, tv.ActionsHolder, tv.FindForm());
					if (a != null)
					{
						tv.ForceLoadNextLevel(this);
						this.Expand();
						for (int i = 0; i < this.Nodes.Count; i++)
						{
							TreeNodeAction na = this.Nodes[i] as TreeNodeAction;
							if (na != null)
							{
								if (na.Action.WholeActionId == a.WholeActionId)
								{
									tv.SelectedNode = na;
									na.EnsureVisible();
									break;
								}
							}
						}
					}
				}
			}
		}
	}
	public class TreeNodeActionCollectionForMethod : TreeNodeActionCollection
	{
		private MethodClass _method;
		public TreeNodeActionCollectionForMethod(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, MethodClass objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			_method = objectPointer;
		}
		public MethodClass Method
		{
			get
			{
				if (_method == null)
				{
					_method = (MethodClass)(this.OwnerPointer);
				}
				return _method;
			}
		}
		public override bool IncludeAction(IAction a, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (a.ScopeMethodId == Method.MemberId)
			{
				return true;
			}
			return false;
		}
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			if (Method.ActionList != null && Method.ActionList.Count > 0)
			{
				return true;
			}
			return false;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			return null;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new cloader(Method.IsStatic));
			return lst;
		}
		class cloader : TreeNodeLoader
		{
			public cloader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeActionCollectionForMethod mnode = parentNode as TreeNodeActionCollectionForMethod;
				if (mnode != null)
				{
					MethodClass mc = mnode.Method;
					ClassPointer root = mc.RootPointer;
					root.LoadActions(mc);
					Dictionary<uint, IAction> acts = mc.ActionInstances;
					SortedList<string, TreeNode> sorted = new SortedList<string, TreeNode>();
					foreach (IAction a in acts.Values)
					{
						if (a != null)
						{
							if (a.ScopeMethodId == mc.MethodID)
							{
								a.ScopeMethod = mc;
							}
							TreeNodeAction tna = new TreeNodeAction(ForStatic, a);
							if (sorted.ContainsKey(tna.Text))
							{
								uint mx = 2;
								while (true)
								{
									string nm = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", tna.Text, mx.ToString("x", CultureInfo.InvariantCulture));
									if (sorted.ContainsKey(nm))
									{
										mx++;
									}
									else
									{
										sorted.Add(nm, tna);
										break;
									}
								}
							}
							else
							{
								sorted.Add(tna.Text, tna);
							}
						}
					}
					parentNode.AddSortedNodes(sorted);
				}
			}
		}
	}
	/// <summary>
	/// for static class such as LimnorApp
	/// </summary>
	public class TreeNodeActionMixedCollection : TreeNodeActionCollection
	{
		public TreeNodeActionMixedCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, true, objectPointer, scopeMethodId)
		{
			Text = "Actions";
		}
	}
	/// <summary>
	/// show all actions
	/// </summary>
	public class TreeNodeAllActionCollection : TreeNodeActionCollection
	{
		public TreeNodeAllActionCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, ClassPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, false, objectPointer, scopeMethodId)
		{
		}
		public ClassPointer RootClass
		{
			get
			{
				return (ClassPointer)(this.OwnerPointer);
			}
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader());
			return lst;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader()
				: base(false)
			{
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				bool staticScope = false;
				//TreeViewObjectExplorer tv = parentNode.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					staticScope = tv.StaticScope;
				}
				UInt32 scopeId = parentNode.ScopeMethodId;
				ClassPointer root = (ClassPointer)(parentNode.OwnerPointer);
				IActionsHolder actsHolder = root;
				if (tv.ActionsHolder != null)
				{
					actsHolder = tv.ActionsHolder;
					actsHolder.LoadActionInstances();
				}
				Dictionary<UInt32, IAction> acts = actsHolder.GetVisibleActionInstances();//.ActionInstances;// root.GetActions();
				SortedList<string, TreeNode> sorted = new SortedList<string, TreeNode>();
				foreach (IAction a in acts.Values)
				{
					if (a != null)
					{
						bool bInclude = false;
						if (a.ScopeMethodId != 0)
						{
							if (a.ScopeMethodId != scopeId)
							{
								continue;
							}
							else
							{
								SubMethodInfoPointer smi = a.ActionMethod as SubMethodInfoPointer;
								if (smi != null)
								{
									continue;
								}
								else if (a.ActionMethod != null)
								{
									SetterPointer sp = a.ActionMethod as SetterPointer;
									if (sp != null)
									{
										if (sp.SetProperty != null)
										{
											ParameterClassSubMethod pcs = sp.SetProperty.Owner as ParameterClassSubMethod;
											if (pcs != null)
											{
												continue;
											}
										}
									}
									else
									{
										ActionBranchParameterPointer pcs = a.ActionMethod.Owner as ActionBranchParameterPointer;
										if (pcs != null)
										{
											continue;
										}
									}
								}
								bInclude = true;
							}
						}
						if (!bInclude)
						{
							if (staticScope)
							{
								bInclude = a.IsStaticAction;
							}
							else
							{
								bInclude = true;
							}
						}
						if (bInclude)
						{
							TreeNodeAction tna = new TreeNodeAction(a.IsStatic, a);
							if (sorted.ContainsKey(tna.Text))
							{
								uint mx = 2;
								while (true)
								{
									string nm = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", tna.Text, mx.ToString("x", CultureInfo.InvariantCulture));
									if (sorted.ContainsKey(nm))
									{
										mx++;
									}
									else
									{
										sorted.Add(nm, tna);
										break;
									}
								}
							}
							else
							{
								sorted.Add(tna.Text, tna);
							}
						}
					}
				}
				parentNode.AddSortedNodes(sorted);
				NoAction na = new NoAction();
				TreeNodeAction naNode = new TreeNodeAction(na.IsStatic, na);
				parentNode.Nodes.Add(naNode);
			}
		}
	}
	class ActionLoader : TreeNodeLoader
	{
		public ActionLoader(bool forStatic)
			: base(forStatic)
		{
		}
		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			if (tv != null)
			{
				ClassPointer cp = tv.RootId;
				if (cp == null)
				{
					return;
				}
				UInt32 scopeId = 0;
				MethodClass mc = tv.ScopeMethod as MethodClass;
				if (mc != null)
				{
					scopeId = mc.MethodID;
				}
				Dictionary<UInt32, IAction> acts = cp.GetActions();
				if (acts != null)
				{
					TreeNodeActionCollection acList = parentNode as TreeNodeActionCollection;
					SortedList<string, TreeNode> sorted = new SortedList<string, TreeNode>();
					foreach (IAction a in acts.Values)
					{
						if (a != null)
						{
							bool bInclude = false;
							if (a.ScopeMethodId != 0)
							{
								if (a.ScopeMethodId != scopeId)
								{
									continue;
								}
								else
								{
									bInclude = true;
								}
							}
							if (a.IsValid)
							{
									if (parentNode.ActionMethodType == EnumActionMethodType.Static)
									{
										if (!a.IsStaticAction)
										{
											continue;
										}
									}
									if (acList != null)
									{
										bInclude = acList.IncludeAction(a, tv, scopeId, false);
									}
									else
									{
										bInclude = parentNode.IncludeAction(a, tv, scopeId, true);
									}
							}
							else
							{
								bInclude = (scopeId == 0);
							}
							if (bInclude)
							{
								TreeNodeAction tna = new TreeNodeAction(ForStatic, a);
								if (sorted.ContainsKey(tna.Text))
								{
									uint mx = 2;
									while (true)
									{
										string nm = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", tna.Text, mx.ToString("x", CultureInfo.InvariantCulture));
										if (sorted.ContainsKey(nm))
										{
											mx++;
										}
										else
										{
											sorted.Add(nm, tna);
											break;
										}
									}
								}
								else
								{
									sorted.Add(tna.Text, tna);
								}
							}
						}
					}
					parentNode.AddSortedNodes(sorted);
				}
			}
		}
	}
	class CustomMethodLoader : TreeNodeLoader
	{
		public CustomMethodLoader(bool forStatic)
			: base(forStatic)
		{
		}
		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			if (tv != null)
			{
				UInt32 scopeId = parentNode.ScopeMethodId;
				TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
				Dictionary<UInt32, IAction> actions = null;
				if (topClass != null)
				{
					if (!topClass.StaticScope)
					{
						actions = topClass.GetActions();
					}
				}
				else
				{
					TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
					if (rootType != null)
					{
						actions = rootType.GetActions();
					}
				}
				ClassPointer cp = parentNode.OwnerPointer as ClassPointer;
				if (cp == null)
				{
					throw new DesignerException("{0} is not for ClassPointer", parentNode.GetType().Name);
				}
				SortedList<string, TreeNodeCustomMethod> tcps = new SortedList<string, TreeNodeCustomMethod>();
				SortedList<string, TreeNodeCustomMethod> tcms = new SortedList<string, TreeNodeCustomMethod>();
				Dictionary<string, MethodClass> ms = cp.CustomMethods;
				foreach (MethodClass m in ms.Values)
				{
					if (cp.IsWebPage || cp.IsWebApp)
					{
						if (m is MethodClassInherited)
						{
							continue;
						}
					}
					if (ForStatic == m.IsStatic)
					{
						TreeNodeCustomMethod tna = new TreeNodeCustomMethod(tv, ForStatic, m, tv.RootId, scopeId);
						if (actions != null)
						{
							bool bHasActions = false;
							foreach (IAction a in actions.Values)
							{
								if (a != null)
								{
									if (a.ScopeMethodId != 0)
									{
										if (a.ScopeMethodId != scopeId)
										{
											continue;
										}
									}
									if (m.IsSameObjectRef(a.ActionMethod))
									{
										bHasActions = true;
										break;
									}
								}
							}
							if (bHasActions)
							{
								tna.ShowActionIcon();
							}
						}
						if (m.Implemented)
						{
							tcps.Add(tna.Text, tna);
						}
						else
						{
							tcms.Add(tna.Text, tna);
						}
					}
				}
				IEnumerator<KeyValuePair<string, TreeNodeCustomMethod>> ie = tcps.GetEnumerator();
				while (ie.MoveNext())
				{
					parentNode.Nodes.Add(ie.Current.Value);
				}
				ie = tcms.GetEnumerator();
				while (ie.MoveNext())
				{
					parentNode.Nodes.Add(ie.Current.Value);
				}
			}
		}
	}

	class CustomEventPointerLoader : TreeNodeLoader
	{
		public CustomEventPointerLoader(bool forStatic)
			: base(forStatic)
		{
		}
		public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			ICustomPropertyHolder ph = parentNode.GetCustomPropertyHolder();
			if (ph != null)
			{
				List<EventClass> events = ph.GetCustomEvents();
				if (events != null)
				{
					SortedList<string, TreeNodeCustomEventPointer> es0 = new SortedList<string, TreeNodeCustomEventPointer>();
					TreeNodeClassRoot tr = parentNode.TopLevelRootClassNode;
					foreach (EventClass e in events)
					{
						if (ForStatic == e.IsStatic)
						{
							CustomEventPointer ep = new CustomEventPointer(e, parentNode.GetHolder());
							TreeNodeCustomEventPointer tna = new TreeNodeCustomEventPointer(parentNode.IsStatic, ep);
							es0.Add(tna.Text, tna);
						}
					}
					IEnumerator<KeyValuePair<string, TreeNodeCustomEventPointer>> ie;
					ie = es0.GetEnumerator();
					while (ie.MoveNext())
					{
						TreeNodeCustomEventPointer tna = ie.Current.Value;
						parentNode.Nodes.Add(tna);
						//
						if (tr != null && tr.HasEventHandler(tna.Event))
						{
							tna.OnShowActionIcon();
						}
					}
				}
			}
		}
	}
	public class TreeNodeAction : TreeNodeObject
	{
		#region fields and constructors
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectPointer">method owner</param>
		/// <param name="target"></param>
		/// <param name="scope"></param>
		/// <param name="eventName"></param>
		public TreeNodeAction(bool isForStatic, IAction action)
			: base(isForStatic, action)
		{
			Text = action.ActionName;
			SetActionImage();
			SelectedImageIndex = ImageIndex;
			List<TreeNodeLoader> l = GetLoaderNodes();
			if (l != null && l.Count > 0)
			{
				foreach (TreeNodeLoader tl in l)
				{
					Nodes.Add(tl);
				}
			}
		}
		#endregion
		#region Methods
		protected void SetActionImage()
		{
			IAction action = Action;
			if (action != null && action.IsValid)
			{
				if (action is ActionDetachEvent)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_DEATTACH_EVENT_ACT;
				}
				else if (action is ActionAttachEvent)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_ATTACH_EVENT_ACT;
				}
				else if (action.ActionMethod is FireEventMethod)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_A_F_EVENT;
				}
				else
				{
					if (this.IsStatic)
						ImageIndex = TreeViewObjectExplorer.IMG_S_ACTION;
					else
						ImageIndex = TreeViewObjectExplorer.IMG_ACTION;
					if (action.ScopeMethodId != 0)
					{
						if (action.ScopeMethod != null)
						{
							MethodClass mc = action.ScopeMethod as MethodClass;
							if (mc != null && mc.MethodID == action.ScopeMethodId)
							{
								if (!mc.IsActionUsed(action.ActionId))
								{
									ImageIndex = TreeViewObjectExplorer.IMG_ACTION_UNUSED;
								}
							}
						}
					}
				}
			}
			else
			{
				ImageIndex = TreeViewObjectExplorer.IMG_ACTION_INVALID;
			}
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			TreeViewObjectExplorer mv = this.TreeView as TreeViewObjectExplorer;
			if (mv != null)
			{
				mv.NotifyActionSelection(Action);
			}
		}
		public override void ShowText()
		{
			IAction op = this.OwnerIdentity as IAction;
			if (op != null)
			{
				Text = op.ActionName;
			}
			SetActionImage();
			SelectedImageIndex = ImageIndex;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new TreeNodeActionLoadUsage(Action));
			return l;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return false;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (!tv.StaticScope || tv.StaticScope == Action.IsStatic)
				{
					return (tv.SelectionType == EnumObjectSelectType.All
						|| tv.SelectionType == EnumObjectSelectType.Action
						);
				}
			}
			return false;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			List<MenuItem> mis = new List<MenuItem>();
			if (!readOnly)
			{
				IRootNode classNode = this.RootClassNode;
				if (classNode != null && classNode.ClassData != null && classNode.ClassData.RootClassID != null)
				{
					if (classNode.ClassData.RootClassID.ClassId == Action.ClassId)
					{
						MenuItem mi;
						MethodActionForeach ag = this.OwnerIdentity as MethodActionForeach;
						if (ag == null)
						{
							ActionSubMethodGlobal asg = this.OwnerIdentity as ActionSubMethodGlobal;
							if (asg != null)
							{
								ag = asg.ActionHolder as MethodActionForeach;
							}
						}
						if (ag != null)
						{
							mi = new MenuItemWithBitmap("Specify actions", editLoop_Click, Resources._actLst.ToBitmap());
							mis.Add(mi);
						}
						else
						{
							if (Parent is TreeNodeActionCollection)
							{
								mi = new MenuItemWithBitmap("Copy action", TreeNodeActionCopy_Click, Resources._copy.ToBitmap());
								mis.Add(mi);
							}
						}
						ActionAttachEvent aae = this.Action as ActionAttachEvent;
						if (aae != null && aae.IsAttach)
						{
							if (mis.Count > 0)
							{
								mis.Add(new MenuItem("-"));
							}
							mi = new MenuItemWithBitmap("Create detach action", miDetach, Resources._detachEvent.ToBitmap());
							mis.Add(mi);
						}
						if (!IsHandler)
						{
							if (mis.Count > 0)
							{
								mis.Add(new MenuItem("-"));
							}
							mi = new MenuItemWithBitmap("Delete action", TreeNodeActionDelete_Click, Resources._cancel.ToBitmap());
							mis.Add(mi);
						}
					}
				}
			}
			MenuItem[] ami = null;
			if (mis.Count > 0)
			{
				ami = new MenuItem[mis.Count];
				mis.CopyTo(ami);
			}
			return ami;
		}
		#endregion
		#region Properties
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType
		{
			get
			{
				return Action.ObjectDevelopType;// EnumObjectDevelopType.Both; 
			}
		}
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Action; }
		}
		public virtual bool IsHandler
		{
			get
			{
				return false;
			}
		}
		public IAction Action
		{
			get
			{
				return (IAction)(this.OwnerIdentity);
			}
		}
		#endregion
		#region private methods
		void editLoop_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.TopLevelRootClassNode;
			if (root != null && root.ViewersHolder != null && tv != null)
			{
				MethodActionForeach ag = this.OwnerIdentity as MethodActionForeach;
				if (ag == null)
				{
					ActionSubMethodGlobal asg = this.OwnerIdentity as ActionSubMethodGlobal;
					if (asg != null)
					{
						ag = asg.ActionHolder as MethodActionForeach;
					}
				}
				if (ag != null)
				{
					if (ag.Edit(0, tv.Parent.RectangleToScreen(this.Bounds), root.ViewersHolder.Loader, tv.FindForm()))
					{
					}
				}
			}
		}
		void miDetach(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.TopLevelRootClassNode;
			if (tv != null && root != null && root.ViewersHolder != null)
			{
				ActionAttachEvent aae = this.Action as ActionAttachEvent;
				if (aae != null)
				{
					ActionDetachEvent ade = new ActionDetachEvent(aae.Class);
					ade.AttachedActionId = aae.ActionId;
					ade.SetHandlerOwner(aae.AssignedActions);
					aae.Class.SaveAction(ade, null);
					TreeNodeAction tna = new TreeNodeAction(ade.IsStatic, ade);
					this.Parent.Nodes.Add(tna);
					tv.SelectedNode = tna;
					root.ViewersHolder.Loader.NotifyChanges();
				}
			}
		}
		void TreeNodeActionCopy_Click(object sender, EventArgs e)
		{
			DesignUtil.LogIdeProfile("Copy action from Obj Explorer");
			TreeNodeExplorer p = this.Parent as TreeNodeExplorer;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.TopLevelRootClassNode;
			if (p != null && root != null && root.ViewersHolder != null && tv != null)
			{
				ClassPointer cp = root.ViewersHolder.Loader.GetRootId();
				IAction actNew = Action.CreateNewCopy();
				if (actNew.ActionHolder == null)
				{
					actNew.ActionHolder = tv.ActionsHolder;
				}
				cp.SaveAction(actNew, root.ViewersHolder.Loader.Writer);
				TreeNodeAction tna = new TreeNodeAction(actNew.IsStatic, actNew);
				p.Nodes.Add(tna);
				DesignUtil.LogIdeProfile("Notify designer");
				root.ViewersHolder.Loader.NotifyChanges();
			}
			DesignUtil.LogIdeProfile("End of copy act from obj exp");
		}

		void TreeNodeActionEdit_Click(object sender, EventArgs e)
		{
			TreeNodeExplorer p = this.Parent as TreeNodeExplorer;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.TopLevelRootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				IMethod context = null;
				TreeNodeObject np = this.Parent as TreeNodeObject;
				while (np != null)
				{
					context = np.OwnerPointer as IMethod;
					if (context != null)
					{
						break;
					}
					np = np.Parent as TreeNodeObject;
				}
				IAction act = DesignUtil.EditAction(root.ClassData.Project, Action, root.ViewersHolder.Loader, context, tv.FindForm());
				if (act != null)
				{
					tv.ForceLoadNextLevel(p);
					p.Expand();
					for (int i = 0; i < p.Nodes.Count; i++)
					{
						TreeNodeAction a = p.Nodes[i] as TreeNodeAction;
						if (a != null)
						{
							if (a.Action.WholeActionId == act.WholeActionId)
							{
								tv.SelectedNode = a;
								break;
							}
						}
					}
				}
			}
		}
		void TreeNodeActionDelete_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv.DeleteAction(Action))
			{
			}
		}
		#endregion
		#region class TreeNodeActionLoadUsage
		class TreeNodeActionLoadUsage : TreeNodeLoader
		{
			private IAction _act;
			public TreeNodeActionLoadUsage(IAction a)
				: base(a.IsStatic)
			{
				_act = a;
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				if (_act != null && _act.Class != null)
				{
					List<ObjectTextID> lst = _act.Class.GetActionUsage(_act);
					if (lst != null && lst.Count > 0)
					{
						foreach (ObjectTextID ot in lst)
						{
							parentNode.Nodes.Add(new TreeNodeActionUser(ot));
						}
					}
				}
			}
		}
		#endregion
		#region class TreeNodeActionUser
		class TreeNodeActionUser : TreeNode
		{
			private ObjectTextID _user;
			public TreeNodeActionUser(ObjectTextID user)
			{
				_user = user;
				Text = _user.ToString();
				ImageIndex = TreeViewObjectExplorer.IMG_OBJECT;
				SelectedImageIndex = ImageIndex;
			}
		}
		#endregion
	}
	public class TreeNodeEventAction : TreeNodeAction, ITreeNodeBreakPoint
	{
		const string XMLATTVAL_Breakpoint = "BreakAsEventAction";
		private TaskID _taskId;
		private fnOnBool miShowBreakpoint;
		public TreeNodeEventAction(bool isForStatic, TaskID taskId, IAction action)
			: base(isForStatic, action)
		{
			//
			miShowBreakpoint = new fnOnBool(showBreakpoint);
			//
			_taskId = taskId;
			showIconNotAtBreak();
		}
		public TaskID TaskId
		{
			get
			{
				return _taskId;
			}
		}
		public override bool IsHandler
		{
			get
			{
				return true;
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			MenuItem[] m = base.GetContextMenuItems(readOnly);
			List<MenuItem> mis = new List<MenuItem>();
			if (m != null)
			{
				mis.AddRange(m);
			}
			if (!readOnly)
			{
				if (!(this.Action is ActionAttachEvent))
				{
					MenuItemWithBitmap mi = new MenuItemWithBitmap("Remove from event", Resources._removeFromEvent.ToBitmap());
					mi.Click += new EventHandler(TreeNodeActionDelink_Click);
					mis.Add(mi);

					if (this.PrevNode != null)
					{
						TreeNodeAction na = this.PrevNode as TreeNodeAction;
						if (na != null)
						{
							mi = new MenuItemWithBitmap("Move up", Resources._upIcon.ToBitmap());
							mi.Click += new EventHandler(TreeNodeActionUp_Click);
							mis.Add(mi);
						}
					}
					if (this.NextNode != null)
					{
						TreeNodeAction na = this.NextNode as TreeNodeAction;
						if (na != null)
						{
							mi = new MenuItemWithBitmap("Move down", Resources._downIcon.ToBitmap());
							mi.Click += new EventHandler(TreeNodeActionDown_Click);
							mis.Add(mi);
						}
					}
				}
			}
			if (_taskId.BreakAsEventAction)
			{
				mis.Add(new MenuItemWithBitmap("Remove break point", mnu_removeBreakPoint, Resources._del_breakPoint.ToBitmap()));
			}
			else
			{
				mis.Add(new MenuItemWithBitmap("Set break point", mnu_setBreakPoint, Resources._breakpoint.ToBitmap()));
			}
			m = new MenuItem[mis.Count];
			mis.CopyTo(m);
			return m;
		}
		private void mnu_removeBreakPoint(object sender, EventArgs e)
		{
			_taskId.BreakAsEventAction = false;
			showIconNotAtBreak();
			saveBreakPointToXml();
		}
		private void mnu_setBreakPoint(object sender, EventArgs e)
		{
			_taskId.BreakAsEventAction = true;
			showIconNotAtBreak();
			saveBreakPointToXml();
		}
		private void TreeNodeActionUp_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			TreeNode nd = this;
			int idx = nd.Index;
			TreeNodeObject p = nd.Parent as TreeNodeObject;
			if (idx > 0 && p != null)
			{
				IEventNode ne = this.Parent as IEventNode;
				TreeNodeClassRoot tr = p.TopLevelRootClassNode;
				EventAction ea = tr.GetEventHandler(ne.OwnerEvent);
				ea.MoveActionUp(this.TaskId);
				nd.Remove();
				p.Nodes.Insert(idx - 1, nd);
				tv.SelectedNode = nd;
				tv.NotifyChanges();
				tr.ViewersHolder.OnActionListOrderChanged(this, ea);
			}
		}
		private void TreeNodeActionDown_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			TreeNode nd = this;
			int idx = nd.Index;
			TreeNodeObject p = nd.Parent as TreeNodeObject;
			if (idx < p.Nodes.Count - 1 && p != null)
			{
				IEventNode ne = this.Parent as IEventNode;
				TreeNodeClassRoot tr = p.TopLevelRootClassNode;
				EventAction ea = tr.GetEventHandler(ne.OwnerEvent);
				ea.MoveActionDown(this.TaskId);
				nd.Remove();
				p.Nodes.Insert(idx + 1, nd);
				tv.SelectedNode = nd;
				tv.NotifyChanges();
				tr.ViewersHolder.OnActionListOrderChanged(this, ea);
			}
		}
		private void TreeNodeActionDelink_Click(object sender, EventArgs e)
		{
			IEventNode en = this.Parent as IEventNode;
			if (en == null)
			{
				MathNode.Log(this.FindForm(),new DesignerException("TreeNodeEventAction is not placed under IEventNode. It is under {0}", this.Parent));
			}
			else
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				IEvent epp = en.OwnerEvent;
				TreeNodeClassRoot tr = this.TopLevelRootClassNode;
				EventAction ea = tr.GetEventHandler(epp);
				if (ea == null)
				{
					MathNode.Log(this.FindForm(),new DesignerException("EventHandler not found for event {0}", epp));
				}
				else
				{
					ea.RemoveAction(TaskId.TaskId);
					this.Remove();
					tr.ViewersHolder.OnRemoveEventHandler(ea, TaskId);
					tr.ResetEventHandlers();
					tv.NotifyChanges();
				}
			}
		}
		private void saveBreakPointToXml()
		{
			XmlNode pn = _taskId.DataXmlNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}[@{1}='{2}']", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XMLATTVAL_Breakpoint));
			if (pn == null)
			{
				IEventNode ne = this.Parent as IEventNode;
				if (ne == null)
				{
					throw new DesignerException("TreeNodeEventAction is not placed under IEventNode. It is under [{0}].", this.Parent);
				}
				IEvent epp = ne.OwnerEvent;
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				EventAction ea = tr.GetEventHandler(epp);
				if (ea != null)
				{
					if (ea == null)
					{
						throw new DesignerException("Event handlers not found for event [{0}]", epp);
					}
					pn = ea.XmlNode.OwnerDocument.CreateElement(XmlTags.XML_PROPERTY);
					ea.XmlNode.AppendChild(pn);
					XmlUtil.SetAttribute(pn, XmlTags.XMLATT_NAME, XMLATTVAL_Breakpoint);
				}
			}
			pn.InnerText = _taskId.BreakAsEventAction.ToString();
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			tv.NotifyChanges();
		}
		private void showIconNotAtBreak()
		{
			if (this.ImageIndex != TreeViewObjectExplorer.IMG_ACTION_INVALID)
			{
				if (_taskId.BreakAsEventAction)
				{
					this.ImageIndex = TreeViewObjectExplorer.IMG_BreakPoint;
				}
				else
				{
					SetActionImage();
				}
				this.SelectedImageIndex = this.ImageIndex;
			}
		}
		#region ITreeNodeBreakPoint Members

		public void ShowBreakPoint(bool pause)
		{
			this.TreeView.Invoke(miShowBreakpoint, pause);
		}
		private void showBreakpoint(bool pause)
		{
			if (pause)
			{
				this.ImageIndex = TreeViewObjectExplorer.IMG_BreakPointPause;
				this.SelectedImageIndex = this.ImageIndex;
				this.BackColor = Color.Yellow;
				this.TreeView.SelectedNode = this;
			}
			else
			{
				showIconNotAtBreak();
				this.BackColor = Color.White;
			}
		}

		#endregion
	}
	/// <summary>
	/// a definition of a custom property for a ClassPointer
	/// </summary>
	public class TreeNodeCustomEvent : TreeNodeObject, IEventNode
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectPointer">method owner</param>
		/// <param name="target"></param>
		/// <param name="scope"></param>
		/// <param name="eventName"></param>
		public TreeNodeCustomEvent(bool isForStatic, EventClass eventObject)
			: base(isForStatic, eventObject)
		{
			if (isForStatic)
			{
				if (eventObject.IsOverride)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_SBC_EVENT;
				}
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_SC_EVENT;
				}
			}
			else
			{
				if (eventObject.IsOverride)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_BC_EVENT;
				}
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_C_EVENT;
				}
			}
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new CLoader(isForStatic));
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			EventClass ec = Event;
			loader.NotifySelection(ec);
			loader.DesignPane.PaneHolder.OnEventSelected(ec);
		}
		public IEvent OwnerEvent { get { return (IEvent)(this.OwnerPointer); } }

		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			FireEventMethod fe = act.ActionMethod as FireEventMethod;
			if (fe != null)
			{
				return fe.IsSameEvent(this.Event);
			}
			else
			{
				return base.IncludeAction(act, tv, scopeMethodId, sameLevel);
			}
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			EventClass property = this.OwnerPointer as EventClass;
			if (this.IsStatic)
			{
				if (property.IsOverride)
					this.ImageIndex = TreeViewObjectExplorer.IMG_SBCA_EVENT;
				else
					this.ImageIndex = TreeViewObjectExplorer.IMG_SCA_EVENT;
			}
			else
			{
				if (property.IsOverride)
					this.ImageIndex = TreeViewObjectExplorer.IMG_BCA_EVENT;
				else
					this.ImageIndex = TreeViewObjectExplorer.IMG_CA_EVENT;
			}
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is EventClass);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoader(IsStatic));
			return l;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Event
					|| tv.SelectionType == EnumObjectSelectType.Object
					);
			}
			return false;
		}
		public EventClass Event
		{
			get
			{
				return (EventClass)this.OwnerPointer;
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			List<MenuItem> mlist = new List<MenuItem>();
			if (!readOnly)
			{
				if (this.MainClassPointer != null)
				{
					mlist.Add(new MenuItemWithBitmap("Assign action", new EventHandler(appendAction_Click), Resources._eventHandlers.ToBitmap()));
					mlist.Add(new MenuItemWithBitmap("Create dynamic handler", new EventHandler(dynamicHandler_Click), Resources._eventHandler.ToBitmap()));
				}
			}
			TreeNodeClassRoot tr = TopLevelRootClassNode;
			EventClass objRef = OwnerPointer as EventClass;
			EventAction ea = tr.GetEventHandler(objRef);
			if (ea != null)
			{
				if (!readOnly)
				{
					mlist.Add(new MenuItem("-"));
				}
				if (!ea.BreakBeforeExecute)
				{
					mlist.Add(new MenuItemWithBitmap("Add breakpoint on entering event", new EventHandler(addBreakpointOnBefore_Click), Resources._breakpoint.ToBitmap()));
				}
				if (!ea.BreakAfterExecute)
				{
					mlist.Add(new MenuItemWithBitmap("Add breakpoint on leaving event", new EventHandler(addBreakpointOnAfter_Click), Resources._breakpoint.ToBitmap()));
				}
			}
			if (!readOnly && !objRef.IsOverride)
			{
				mlist.Add(new MenuItem("-"));
				mlist.Add(new MenuItemWithBitmap("Create event-firing action", mi_createFireEventAction, Resources._createEventFireAction.ToBitmap()));
				mlist.Add(new MenuItem("-"));
				mlist.Add(new MenuItemWithBitmap("Delete event", new EventHandler(deleteEvent_Click), Resources._delEvent.ToBitmap()));
			}
			MenuItem[] mi = null;
			if (mlist.Count > 0)
			{
				mi = new MenuItem[mlist.Count];
				mlist.CopyTo(mi);
			}
			return mi;
		}
		TreeNodeBreakPoint getBreakPointAtBefore()
		{
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeBreakPoint nd = Nodes[i] as TreeNodeBreakPoint;
				if (nd != null)
				{
					if (nd.ForBeforeExecution)
					{
						return nd;
					}
				}
			}
			return null;
		}
		TreeNodeBreakPoint getBreakPointAtAfter()
		{
			LoadNextLevel();
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeBreakPoint nd = Nodes[i] as TreeNodeBreakPoint;
				if (nd != null)
				{
					if (!nd.ForBeforeExecution)
					{
						return nd;
					}
				}
			}
			return null;
		}
		public override IAction CreateNewAction()
		{
			EventClass ec = Event;
			FireEventMethod fe = new FireEventMethod(new CustomEventPointer(ec, GetHolder()));
			ActionClass act = new ActionClass(this.MainClassPointer);
			act.ActionMethod = fe;
			act.ActionName = fe.DefaultActionName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			fe.Action = act;
			fe.ValidateParameterValues(act.ParameterValues);
			return act;
		}
		private void mi_createFireEventAction(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				TreeNodeClassRoot ownerNode = tv.DesignerRootNode;
				if (ownerNode != null)
				{
					MethodClass scopeMethod = tv.ScopeMethod as MethodClass;
					ActionClass act = ownerNode.ClassData.RootClassID.CreateFireEventAction(this.Event, ownerNode.ViewersHolder.Loader.Writer, scopeMethod, tv.FindForm());
					if (act != null)
					{
						this.Expand();
						TreeNodeAction na = null;
						for (int i = 0; i < this.Nodes.Count; i++)
						{
							na = this.Nodes[i] as TreeNodeAction;
							if (na != null)
							{
								if (na.Action.WholeActionId == act.WholeActionId)
								{
									this.ShowActionIcon();
									tv.SelectedNode = na;
									na.EnsureVisible();
									break;
								}
							}
						}
						if (na == null)
						{
							na = new TreeNodeAction(act.IsStatic, act);
							this.Nodes.Add(na);
							this.ShowActionIcon();
							tv.SelectedNode = na;
							na.EnsureVisible();
						}
					}
				}
			}
		}
		void addBreakpointOnBefore_Click(object sender, EventArgs e)
		{
			TreeNodeBreakPoint nd = getBreakPointAtBefore();
			if (nd == null)
			{
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				IEvent objRef = OwnerPointer as IEvent;
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					ea.BreakBeforeExecute = true;
					nd = new TreeNodeBreakPoint(this.IsStatic, ea, objRef);
					nd.ForBeforeExecution = true;
					int n = 0;
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodeEventParam p = Nodes[i] as TreeNodeEventParam;
						if (p == null)
						{
							break;
						}
						n = i + 1;
					}
					Nodes.Insert(n, nd);
					ea.SaveEventBreakPointsToXml();
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					tv.NotifyChanges();
				}
			}
		}
		void addBreakpointOnAfter_Click(object sender, EventArgs e)
		{
			TreeNodeBreakPoint nd = getBreakPointAtAfter();
			if (nd == null)
			{
				TreeNodeClassRoot tr = TopLevelRootClassNode;
				IEvent objRef = OwnerPointer as IEvent;
				EventAction ea = tr.GetEventHandler(objRef);
				if (ea != null)
				{
					ea.BreakAfterExecute = true;
					nd = new TreeNodeBreakPoint(this.IsStatic, ea, objRef);
					nd.ForBeforeExecution = false;
					Nodes.Add(nd);
					ea.SaveEventBreakPointsToXml();
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					tv.NotifyChanges();
				}
			}
		}
		void dynamicHandler_Click(object sender, EventArgs e)
		{
		}
		void appendAction_Click(object sender, EventArgs e)
		{
			CustomEventPointer ep = new CustomEventPointer(this.Event, this.GetHolder());
			IRootNode root = this.TopLevelRootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				if (root.ViewersHolder.AssignActions(ep, TreeView.FindForm()))
				{
					this.LoadNextLevel();
					selectLastAction();
				}
			}
		}
		void deleteEvent_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				ClassPointer cp = root.ClassData.RootClassID;
				if (cp.AskDeleteEvent(this.Event, tv.FindForm()))
				{
					tv.SelectedNode = tv.RootClassNode;
				}
			}
		}
		void selectLastAction()
		{
			if (this.TreeView != null)
			{
				LoadNextLevel();
				for (int i = this.Nodes.Count - 1; i >= 0; i--)
				{
					TreeNodeEventAction ea = Nodes[i] as TreeNodeEventAction;
					if (ea != null)
					{
						this.TreeView.SelectedNode = Nodes[i];
						break;
					}
				}
			}
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
				Dictionary<UInt32, IAction> actions = null;
				if (topClass != null)
				{
					if (!topClass.StaticScope)
					{
						actions = topClass.GetActions();
					}
				}
				else
				{
					TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
					if (rootType != null)
					{
						actions = rootType.GetActions();
					}
				}
				TreeNodeCustomEvent mNode = parentNode as TreeNodeCustomEvent;
				EventClass mmp = mNode.Event;
				//show event-firing actions
				foreach (IAction act in actions.Values)
				{
					if (act != null)
					{
						FireEventMethod fe = act.ActionMethod as FireEventMethod;
						if (fe != null)
						{
							if (fe.IsSameEvent(mmp))
							{
								parentNode.Nodes.Add(new TreeNodeAction(parentNode.IsStatic, act));
							}
						}
					}
				}
				//
				//show parameters
				MethodClass mc = mmp.CreateHandlerMethod(null);
				List<ParameterClass> ps = mc.Parameters;
				foreach (ParameterClass p in ps)
				{
					TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, p);
					parentNode.Nodes.Add(tmp);
				}
				TreeNodeClassRoot tr = parentNode.TopLevelRootClassNode;
				if (tr != null)
				{
					ClassPointer root = tr.RootHost;
					IList<EventAction> eas = tr.GetEventHandlers(mmp);
					foreach (EventAction ea in eas)
					{
						TaskIdList idList = ea.TaskIDList;
						if (ea.BreakBeforeExecute)
						{
							TreeNodeBreakPoint nb = new TreeNodeBreakPoint(parentNode.IsStatic, ea, mmp);
							nb.ForBeforeExecution = true;
							parentNode.Nodes.Add(nb);
						}
						for (int i = 0; i < idList.Count; i++)
						{
							TaskID taskId = idList[i];
							HandlerMethodID hmd = taskId as HandlerMethodID;
							if (hmd != null)
							{
								ActionAttachEvent aae = null;
								UInt32 aaId = ea.GetAssignActionId();
								if (aaId != 0)
								{
									aae = root.GetActionInstance(aaId) as ActionAttachEvent;
								}
								if (aae != null && aae.IsStatic == parentNode.IsStatic)
								{
									parentNode.Nodes.Add(new TreeNodeEventAction(parentNode.IsStatic, taskId, aae));
								}
								else
								{
									parentNode.Nodes.Add(new TreeNodeEventMethod(tv, parentNode.IsStatic, hmd.HandlerMethod));
								}
							}
							else
							{
								IAction a = null;
								if (taskId.ClassId == tr.ClassData.ObjectMap.ClassId)
								{
									a = tr.GetAction(taskId.WholeTaskId);
								}
								else
								{
									ObjectIDmap map = tr.RootObjectId.ObjectList;// ObjectIDmap.GetMap(tv.Project, taskId.ClassId, ClassPointer.OnAfterReadRootComponent);
									XmlNode classNode = map.XmlData;
									XmlNode actNode = classNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
										"{0}/{1}[@{2}='{3}']",
										XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, taskId.ActionId));
									if (actNode != null)
									{
										XmlObjectReader xr = map.Reader;
										a = (IAction)xr.ReadObject(actNode, null);
									}
								}
								if (a == null)
								{
								}
								else
								{
									parentNode.Nodes.Add(new TreeNodeEventAction(parentNode.IsStatic, taskId, a));
								}
							}
						}
						if (ea.BreakAfterExecute)
						{
							TreeNodeBreakPoint nb = new TreeNodeBreakPoint(parentNode.IsStatic, ea, mmp);
							nb.ForBeforeExecution = false;
							parentNode.Nodes.Add(nb);
						}
						parentNode.OnShowActionIcon();
					}
				}
			}
		}
	}
	public class TreeNodeCustomEventCollection : TreeNodeObjectCollection
	{
		public TreeNodeCustomEventCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			Text = "Events";
			this.Nodes.Add(new CLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SC_EVENTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_C_EVENTS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_A_S_C_EVENTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_A_C_EVENTS;
			SelectedImageIndex = this.ImageIndex;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader(this.IsStatic));
			return lst;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return false;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					ClassPointer r = tv.RootId;
					if (r != null)
					{
						MenuItem[] mi = new MenuItem[1];
						mi[0] = new MenuItemWithBitmap("Create new event", newEvent_Click, Resources._newEvent.ToBitmap());
						return mi;
					}
				}
			}
			return null;
		}
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			IObjectPointer pointer = this.OwnerPointer;
			if (pointer != null)
			{
				ClassPointer r = pointer.RootPointer;
				if (r != null)
				{
					EnumObjectDevelopType ot;
					if (this.OwnerIdentity is ClassPointer)
					{
						ot = this.ObjectDevelopType;
					}
					else
					{
						ot = DesignUtil.GetBaseObjectDevelopType(this.OwnerIdentity);
					}
					bool isStatic = parentNode.IsBasePmeStatic;
					foreach (EventAction ea in r.EventHandlers)
					{
						IEvent ep = ea.Event;
						if (ep != null && ep.IsStatic == isStatic && (ot == EnumObjectDevelopType.Both || ep.ObjectDevelopType == ot))
						{
							IObjectPointer id;
							IMemberPointer mp = ep as IMemberPointer;
							if (mp != null)
								id = mp.Holder;
							else
								id = ep.Owner;
							while (id != null)
							{
								if (id.IsSameObjectRef(pointer))
								{
									return true;
								}
								else
								{
									if (id is IClass)
									{
										break;
									}
								}
								mp = id as IMemberPointer;
								if (mp != null)
									id = mp.Holder;
								else
									id = id.Owner;
							}
						}
					}
				}
			}
			return false;
		}
		public TreeNodeCustomEvent GetCustomEventNode(UInt32 propertyId)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeCustomEvent cm = this.Nodes[i] as TreeNodeCustomEvent;
				if (cm != null)
				{
					if (cm.Event.MemberId == propertyId)
					{
						return cm;
					}
				}
			}
			return null;
		}
		private void newEvent_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			TreeNodeClassRoot root = this.TopLevelRootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				EventClass ev;
				if (root.ClassData.RootClassID.IsWebPage)
				{
					//create CustomEventHandlerType
					DlgMakeTypeList dlg = new DlgMakeTypeList();
					dlg.LoadData("Select event parameters", "Parameter name", tv.Project, null);
					if (dlg.ShowDialog(tv.FindForm()) == DialogResult.OK)
					{
						CustomEventHandlerType ceht = new CustomEventHandlerType();
						ceht.Parameters = dlg.Results;
						ev = root.ClassData.RootClassID.CreateNewEvent(new DataTypePointer(ceht), root.ViewersHolder.Loader.CreateNewComponentName("Event", null), this.IsStatic);
					}
					else
					{
						return;
					}
				}
				else
				{
					ev = root.ClassData.RootClassID.CreateNewEvent(new DataTypePointer(new TypePointer(typeof(EventHandler))), root.ViewersHolder.Loader.CreateNewComponentName("Event", null), this.IsStatic);
				}
				//
				//locate the new event
				//
				this.ResetNextLevel(tv);
				//
				this.LoadNextLevel();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeCustomEvent cp = Nodes[i] as TreeNodeCustomEvent;
					if (cp != null)
					{
						if (cp.Event.MemberId == ev.MemberId)
						{
							this.TreeView.SelectedNode = cp;
							break;
						}
					}
				}
				//
			}
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassRoot tr = parentNode.TopLevelRootClassNode;
				List<EventClass> props = null;
				ICustomPropertyHolder customHolder = null;
				TreeNode tn = parentNode.Parent;
				while (tn != null)
				{
					customHolder = tn as ICustomPropertyHolder;
					if (customHolder != null)
					{
						break;
					}
					tn = tn.Parent;
				}
				if (customHolder != null)
				{
					props = customHolder.GetCustomEvents();
					if (props != null)
					{
						SortedList<string, TreeNodeCustomEvent> es0 = new SortedList<string, TreeNodeCustomEvent>();
						foreach (EventClass p in props)
						{
							if (ForStatic == p.IsStatic)
							{
								TreeNodeCustomEvent nodeProperty = new TreeNodeCustomEvent(ForStatic, p);
								es0.Add(nodeProperty.Text, nodeProperty);

								if (tr != null && tr.HasEventHandler(p))
								{
									nodeProperty.OnShowActionIcon();
								}
							}
						}
						IEnumerator<KeyValuePair<string, TreeNodeCustomEvent>> ie;
						ie = es0.GetEnumerator();
						while (ie.MoveNext())
						{
							parentNode.Nodes.Add(ie.Current.Value);
						}
					}
				}
			}
		}
	}
	public class TreeNodeEventMixedCollection : TreeNodeCustomEventCollection
	{
		public TreeNodeEventMixedCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, true, objectPointer, scopeMethodId)
		{
			Text = "Events";
			List<TreeNodeLoader> loaders = GetLoaderNodes();
			if (loaders != null && loaders.Count > 0)
			{
				this.Nodes.Clear();
				foreach (TreeNodeLoader l in loaders)
				{
					this.Nodes.Add(l);
				}
				this.NextLevelLoaded = false;
				this.Collapse();
			}
		}
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			ClassPointer r = RootObjectId;
			if (r != null)
			{
				IObjectPointer pointer = this.OwnerPointer;
				foreach (EventAction ea in r.EventHandlers)
				{
					IEvent ep = ea.Event;
					if (ep != null)
					{
						IObjectPointer id = ep.Owner;
						while (id != null)
						{
							if (id.IsSameObjectRef(pointer))
							{
								return true;
							}
							if (id is IClass)
							{
								break;
							}
							id = id.Owner;
						}
					}
				}
			}
			return false;
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_EVENTS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_EVENTS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_A_Events;
			SelectedImageIndex = this.ImageIndex;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			//the base version loads the custom events
			List<TreeNodeLoader> loaders = base.GetLoaderNodes();
			//this version loads the base-type events which are from a template static class (i.e. WinApp)
			loaders.Add(new EventsLoader(true));
			return loaders;
		}
	}
	public class TreeNodeCustomMethodPointerCollection : TreeNodeObjectCollection
	{
		public TreeNodeCustomMethodPointerCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			Text = "Methods";
			this.Nodes.Add(new CustomMethodPointerLoader(isForStatic));
		}
		protected override bool OnCheckIncludeAction(IAction act)
		{
			SubMethodInfoPointer smip = act.ActionMethod as SubMethodInfoPointer;
			if (smip != null)
			{
				return false;
			}
			return true;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SC_METHODS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_C_METHODS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_METHODS_WITHACTS;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;// (pointer is MethodClass || pointer is MethodParamPointer);
		}
		public TreeNodeCustomMethodPointer GetMethodPointerNode(UInt32 id)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomMethodPointer tcp = Nodes[i] as TreeNodeCustomMethodPointer;
				if (tcp != null)
				{
					if (tcp.Method.MemberId == id)
					{
						return tcp;
					}
				}
			}
			return null;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CustomMethodPointerLoader(this.IsStatic));
			return lst;
		}
		class CustomMethodPointerLoader : TreeNodeLoader
		{
			public CustomMethodPointerLoader(bool forStatic)
				: base(forStatic)
			{
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				ICustomPropertyHolder ph = parentNode.GetCustomPropertyHolder();
				if (ph != null)
				{
					bool isWebPage = false;
					TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
					Dictionary<UInt32, IAction> actions = null;
					if (topClass != null)
					{
						if (typeof(WebPage).IsAssignableFrom(topClass.ClassData.RootClassID.BaseClassType))
						{
							isWebPage = true;
						}
						if (!topClass.StaticScope)
						{
							actions = topClass.GetActions();
						}
					}
					else
					{
						TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
						if (rootType != null)
						{
							actions = rootType.GetActions();
						}
					}
					SortedList<string, TreeNodeCustomMethodPointer> list = new SortedList<string, TreeNodeCustomMethodPointer>();
					IClass holder = (IClass)(parentNode.OwnerIdentity);
					List<MethodClass> methods = ph.GetCustomMethods();
					foreach (MethodClass m in methods)
					{
						if (isWebPage && m.RunAt == EnumWebRunAt.Server)
						{
							continue;
						}
						if (!m.IsOverride)
						{
							if (ForStatic == m.IsStatic)
							{
								CustomMethodPointer mp = new CustomMethodPointer(m, holder);
								TreeNodeCustomMethodPointer tna = new TreeNodeCustomMethodPointer(ForStatic, mp);
								if (actions != null)
								{
									bool bHasActions = false;
									foreach (IAction a in actions.Values)
									{
										if (a != null)
										{
											if (m.IsSameObjectRef(a.ActionMethod))
											{
												bHasActions = true;
												break;
											}
										}
									}
									if (bHasActions)
									{
										tna.ShowActionIcon();
									}
								}
								list.Add(tna.Text, tna);

							}
						}
					}
					IEnumerator<KeyValuePair<string, TreeNodeCustomMethodPointer>> ie = list.GetEnumerator();
					while (ie.MoveNext())
					{
						parentNode.Nodes.Add(ie.Current.Value);
					}
				}
			}
		}
	}
	public class TreeNodeCustomMethodCollection : TreeNodeObjectCollection
	{
		public TreeNodeCustomMethodCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			Text = "Methods";
			this.Nodes.Add(new CustomMethodLoader(isForStatic));
		}
		protected override bool OnCheckIncludeAction(IAction act)
		{
			SubMethodInfoPointer smip = act.ActionMethod as SubMethodInfoPointer;
			if (smip != null)
			{
				return false;
			}
			return true;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SC_METHODS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_C_METHODS;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SCA_METHODS;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_CA_METHODS;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is MethodClass || pointer is MethodParamPointer);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				bool isWebService = false;
				bool isWebPage = false;
				bool isRemotingHost = false;
				ClassPointer root = this.OwnerPointer as ClassPointer;
				if (root != null)
				{
					if (root.IsWebService)
					{
						isWebService = true;
					}
					else if (root.IsWebPage)
					{
						isWebPage = true;
					}
					else
					{
						if (root.ObjectInstance is RemotingHost)
						{
							isRemotingHost = true;
						}
					}
				}
				List<MenuItem> mi = new List<MenuItem>();
				if (isWebService)
				{
					mi.Add(new MenuItemWithBitmap("Create local method", newMethod_Click, Resources._newMethod.ToBitmap()));
					mi.Add(new MenuItemWithBitmap("Create web method", newWebServiceMethod_Click, Resources._newWebMethod.ToBitmap()));
				}
				else if (isWebPage)
				{
					mi.Add(new MenuItemWithBitmap("Create web client method", newClientMethod_Click, Resources._webClientMethodNew.ToBitmap()));
					mi.Add(new MenuItemWithBitmap("Create web server method", newMethod_Click, Resources._webServerMethodNew.ToBitmap()));
				}
				else if (isRemotingHost)
				{
					mi.Add(new MenuItemWithBitmap("Create local method", newMethod_Click, Resources._newMethod.ToBitmap()));
					mi.Add(new MenuItemWithBitmap("Create service method", newRemotingServiceMethod_Click, Resources._newWebMethod.ToBitmap()));
				}
				else
				{
					mi.Add(new MenuItemWithBitmap("Create new method", newMethod_Click, Resources._newMethod.ToBitmap()));
				}
				return mi.ToArray();
			}
			return null;
		}
		public TreeNodeCustomMethod LocateMethodNode(UInt32 methodId)
		{
			TreeNodeCustomMethod cmRet = null;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			tv.ForceLoadNextLevel(this);
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeCustomMethod cm = this.Nodes[i] as TreeNodeCustomMethod;
				if (cm != null)
				{
					if (cm.Method.MemberId == methodId)
					{
						tv.SelectedNode = cm;
						cmRet = cm;
						break;
					}
				}
			}
			return cmRet;
		}
		private void newRemotingServiceMethod_Click(object sender, EventArgs e)
		{
			createMethod(EnumMethodStyle.RemotingService, EnumMethodWebUsage.Server);
		}
		private void newWebServiceMethod_Click(object sender, EventArgs e)
		{
			createMethod(EnumMethodStyle.WebService, EnumMethodWebUsage.Server);
		}
		private void newMethod_Click(object sender, EventArgs e)
		{
			createMethod(EnumMethodStyle.Normal, EnumMethodWebUsage.Server);
		}
		private void newClientMethod_Click(object sender, EventArgs e)
		{
			createMethod(EnumMethodStyle.Normal, EnumMethodWebUsage.Client);
		}
		private void createMethod(EnumMethodStyle style, EnumMethodWebUsage webUsage)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				MethodClass method = root.ClassData.RootClassID.CreateNewMethod(IsStatic, style, webUsage, tv.Parent.RectangleToScreen(this.Bounds), tv.FindForm());
				if (method != null)
				{
					TreeNodeCustomMethod cm = LocateMethodNode(method.MemberId);
					if (style == EnumMethodStyle.WebService)
					{
						cm.ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEB;
						cm.SelectedImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEB;
					}
					else if (style == EnumMethodStyle.RemotingService)
					{
						cm.ImageIndex = TreeViewObjectExplorer.IMG_SERVICES;
						cm.SelectedImageIndex = TreeViewObjectExplorer.IMG_SERVICES;
					}
				}
			}
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CustomMethodLoader(this.IsStatic));
			return lst;
		}
	}
	/// <summary>
	/// for showing methods for a static class
	/// </summary>
	public class TreeNodeMethodMixedCollection : TreeNodeCustomMethodCollection
	{
		public TreeNodeMethodMixedCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, true, objectPointer, scopeMethodId)
		{
			Text = "Methods";
			ImageIndex = TreeViewObjectExplorer.IMG_SC_METHODS;//.IMG_S_ACTIONGROUPCOLLECTION;
			SelectedImageIndex = ImageIndex;
			List<TreeNodeLoader> loaders = GetLoaderNodes();
			if (loaders != null && loaders.Count > 0)
			{
				this.Nodes.Clear();
				foreach (TreeNodeLoader l in loaders)
				{
					this.Nodes.Add(l);
				}
				this.NextLevelLoaded = false;
				this.Collapse();
			}
		}
		protected override bool OnCheckIncludeAction(IAction act)
		{
			SubMethodInfoPointer smip = act.ActionMethod as SubMethodInfoPointer;
			if (smip != null)
			{
				return false;
			}
			return true;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			//the base version loads the custom methods
			List<TreeNodeLoader> loaders = base.GetLoaderNodes();
			bool useMethodLoader = true;
			ClassPointer root = this.OwnerPointer as ClassPointer;
			if (root != null)
			{
				if (root.IsWebApp)
				{
					useMethodLoader = false;
				}
			}
			if (useMethodLoader)
			{
				//this version loads the base-type methods which are from a template static class (i.e. WinApp)
				loaders.Add(new MethodLoader(true));
			}
			return loaders;
		}
	}
	public class TreeNodeCustomMethodPointer : TreeNodeObject
	{
		public TreeNodeCustomMethodPointer(bool isForStatic, CustomMethodPointer methodPointer)
			: base(isForStatic, methodPointer)
		{
			Text = methodPointer.DisplayName;//.ToString();
			if (isForStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_ACTIONGROUP;
			else
			{
				if (methodPointer.IsWebMethod)
				{
					ImageIndex = TreeViewObjectExplorer.IMG_METHOD_WEB;
				}
				else
				{
					ImageIndex = TreeViewObjectExplorer.IMG_ACTIONGROUP;
				}
			}
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new CLoader(isForStatic));

		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					return false;
				}
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Action
					|| tv.SelectionType == EnumObjectSelectType.Method
					);
			}
			return false;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		[Browsable(false)]
		public CustomMethodPointer Method
		{
			get
			{
				return (CustomMethodPointer)(this.OwnerIdentity);
			}
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (IsStatic)
				this.ImageIndex = TreeViewObjectExplorer.IMG_SA_ACTIONGROUP;
			else
				this.ImageIndex = TreeViewObjectExplorer.IMG_A_ACTIONGROUP;
			this.SelectedImageIndex = this.ImageIndex;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (this.MainClassPointer != null)
				{
					MenuItem[] mi = new MenuItem[1];
					mi[0] = new MenuItemWithBitmap("Create action", OnCreateNewAction, Resources._customMethodAction.ToBitmap());
					return mi;
				}
			}
			return null;
		}
		public override IAction CreateNewAction()
		{
			CustomMethodPointer mp = Method;
			ActionClass act = new ActionClass(this.MainClassPointer);
			act.ActionMethod = (CustomMethodPointer)mp.Clone();
			act.ActionName = mp.DefaultActionName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return act;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader(this.IsStatic));
			return lst;
		}
		public void SetMethodClass(MethodClass m)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			Method.SetMethod(m);
			ShowText();
			ResetNextLevel(tv);
		}
		public override void ShowText()
		{
			Text = Method.DisplayName;
		}

		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
				Dictionary<UInt32, IAction> actions = null;
				if (topClass != null)
				{
					if (!topClass.StaticScope)
					{
						actions = topClass.GetActions();
					}
				}
				else
				{
					TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
					if (rootType != null)
					{
						actions = rootType.GetActions();
					}
				}
				TreeNodeCustomMethodPointer mNode = parentNode as TreeNodeCustomMethodPointer;
				CustomMethodPointer mmp = mNode.Method;
				if (mmp.ParameterCount > 0)
				{
					List<ParameterClass> parameters = mmp.Parameters;
					if (parameters != null)
					{
						foreach (ParameterClass p in parameters)
						{
							TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, p);
							parentNode.Nodes.Add(tmp);
						}
					}
				}
				if (actions != null)
				{
					foreach (IAction a in actions.Values)
					{
						if (a != null && a.ActionMethod.IsSameMethod(mmp))
						{
							parentNode.Nodes.Add(new TreeNodeAction(ForStatic, a));
						}
					}
				}
			}
		}
	}
	public class TreeNodeCustomMethod : TreeNodeObject
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectPointer">method owner</param>
		/// <param name="target"></param>
		/// <param name="scope"></param>
		/// <param name="eventName"></param>
		public TreeNodeCustomMethod(TreeViewObjectExplorer tv, bool isForStatic, MethodClass method, ClassPointer root, UInt32 scopeMethodId)
			: base(isForStatic, method)
		{
			ImageIndex = TreeViewObjectExplorer.GetMethodImageIndex(method);
			SelectedImageIndex = ImageIndex;
			if (this.SelectionTarget == EnumObjectSelectType.All)
			{
				if (method.Implemented)
				{
					Nodes.Add(new TreeNodeAttributeCollection(tv, this, (IAttributeHolder)this.OwnerPointer, scopeMethodId));
				}
			}
			Nodes.Add(new TreeNodeActionCollectionForMethod(tv, this, isForStatic, method, scopeMethodId));
			this.Nodes.Add(new CLoader(isForStatic));
		}
		public override void ResetImage()
		{
			ImageIndex = TreeViewObjectExplorer.GetMethodImageIndex(Method);
			SelectedImageIndex = ImageIndex;
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			MethodClass mc = Method;
			loader.NotifySelection(mc);
			loader.DesignPane.PaneHolder.OnMethodSelected(mc);
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			this.Nodes.Clear();
			if (this.SelectionTarget == EnumObjectSelectType.All)
			{
				Nodes.Add(new TreeNodeAttributeCollection(tv, this, (IAttributeHolder)this.OwnerPointer, ScopeMethodId));
			}
			this.Nodes.Add(new CLoader(this.IsStatic));
			this.NextLevelLoaded = false;
			this.Collapse();
		}
		[Browsable(false)]
		public bool ShowVariables { get; set; }
		[Browsable(false)]
		public bool ForScopeMethod { get; set; }
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			ImageIndex = TreeViewObjectExplorer.GetActMethodImageIndex(Method);
			this.SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is IAction || pointer is MethodParamPointer);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoader(this.IsStatic));
			return l;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					return false;
				}
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Action
					|| tv.SelectionType == EnumObjectSelectType.Method
					)
				{
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Object)
				{
					if (tv.SelectionTypeScope != null && tv.SelectionTypeScope.BaseClassType != null)
					{
						if (typeof(Delegate).IsAssignableFrom(tv.SelectionTypeScope.BaseClassType))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		public MethodClass Method
		{
			get
			{
				return (MethodClass)this.OwnerPointer;
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				List<MenuItem> mis = new List<MenuItem>();
				if (!ForScopeMethod)
				{
					if (MainClassPointer != null && this.SelectionTarget != EnumObjectSelectType.Method)
					{
						mis.Add(new MenuItemWithBitmap("Create action", OnCreateNewAction, Resources._customMethodAction.ToBitmap()));
					}
					MenuItem mnuEdit = new MenuItemWithBitmap("Edit method", methodEdit_Click, Resources._method.ToBitmap());
					mis.Add(mnuEdit);
					mis.Add(new MenuItem("-"));

					if (Method.IsOverride)
					{
						if (Method.Implemented)
						{
							mis.Add(new MenuItemWithBitmap("Remove Override", mnu_remove, Resources._cancel.ToBitmap()));
						}
						else
						{
							mis.Add(new MenuItemWithBitmap("Override", mnu_implement, Resources._overrideMethod.ToBitmap()));
							mnuEdit.Enabled = false;
						}
					}
					else
					{
						mis.Add(new MenuItemWithBitmap("Delete method", methodDelete_Click, Resources._cancel.ToBitmap()));
						ClassPointer cp = Method.Owner as ClassPointer;
						if (cp != null && cp.IsWebService)
						{
							mis.Add(new MenuItem("-"));
							if (Method.IsWebMethod)
							{
								mis.Add(new MenuItemWithBitmap("Make it a local method", methodMakeLocal_Click, Resources._method.ToBitmap()));
							}
							else
							{
								mis.Add(new MenuItemWithBitmap("Make it a web method", methodMakeWeb_Click, Resources._webMethod.ToBitmap()));
							}
						}
					}
					if (mis.Count > 0)
					{
						mis.Add(new MenuItem("-"));
						mis.Add(new MenuItemWithBitmap("Make a new copy", methodMakeCopy_Click, Resources._copy.ToBitmap()));
					}
				}
				return mis.ToArray();
			}
			return null;
		}
		void mnu_remove(object sender, EventArgs e)
		{
			MethodOverride po = Method as MethodOverride;
			if (po == null)
			{
				throw new DesignerException("Overriden method is not found. [{0}]", Method);
			}
			if (MessageBox.Show("Do you want to remove the override of this method?", "Method", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
			{
				TreeNode pn = this.Parent;
				ClassPointer r = po.Holder as ClassPointer;
				r.RemoveOverrideMethod(po);
				pn.Expand();
				for (int i = 0; i < pn.Nodes.Count; i++)
				{
					TreeNodeCustomMethod tnm = pn.Nodes[i] as TreeNodeCustomMethod;
					if (tnm != null)
					{
						if (tnm.Method.MemberId == po.MemberId)
						{
							tnm.TreeView.SelectedNode = tnm;
							break;
						}
					}
				}
			}
		}
		void mnu_implement(object sender, EventArgs e)
		{
			MethodClassInherited pc = Method as MethodClassInherited;
			if (pc != null)
			{
				TreeNode pn = this.Parent;
				ClassPointer r = pc.Holder as ClassPointer;
				MethodOverride mo = r.ImplementOverrideMethod(pc);
				pn.Expand();
				for (int i = 0; i < pn.Nodes.Count; i++)
				{
					TreeNodeCustomMethod tnm = pn.Nodes[i] as TreeNodeCustomMethod;
					if (tnm != null)
					{
						if (tnm.Method.MemberId == mo.MemberId)
						{
							tnm.TreeView.SelectedNode = tnm;
							break;
						}
					}
				}
			}
		}
		public override IAction CreateNewAction()
		{
			MethodClass mc = Method;
			ActionClass act = new ActionClass(this.MainClassPointer);
			act.ActionMethod = mc.CreatePointer(GetHolder());
			act.ActionName = act.ActionMethod.DefaultActionName;// mc.ReferenceName;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return act;
		}
		void methodEdit_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				TreeNodeCustomMethodCollection p = this.Parent as TreeNodeCustomMethodCollection;
				if (Method.Edit(0, tv.Parent.RectangleToScreen(this.Bounds), r.ViewersHolder.Loader, tv.FindForm()))
				{
					if (p != null)
					{
						tv.ForceLoadNextLevel(p);
						p.Expand();
						p.LocateMethodNode(this.Method.MemberId);
					}
				}
			}
		}
		void methodDelete_Click(object sender, EventArgs e)
		{
			IRootNode r = this.RootClassNode;
			Form f = null;
			if (this.TreeView != null)
			{
				f = this.TreeView.FindForm();
			}
			if (r != null && r.ViewersHolder != null)
			{
				ClassPointer root = r.ClassData.RootClassID;
				root.AskDeleteMethod(Method, f);
			}
		}
		void methodMakeCopy_Click(object sender, EventArgs e)
		{
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				MethodClass newMethod = r.ClassData.RootClassID.MakeMethodCopy(Method, TreeView.FindForm());
				if (newMethod != null)
				{
					TreeNodeCustomMethodCollection mc = this.Parent as TreeNodeCustomMethodCollection;
					if (mc != null)
					{
						TreeNodeCustomMethod tm = mc.LocateMethodNode(newMethod.MemberId);
						if (tm != null)
						{
							int n = TreeViewObjectExplorer.GetMethodImageIndex(newMethod);
							tm.ImageIndex = n;
							tm.SelectedImageIndex = n;
						}
					}
				}
			}
		}
		void methodMakeLocal_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				Method.RemoveWebAttribute();
				ImageIndex = TreeViewObjectExplorer.GetMethodImageIndex(Method);
				SelectedImageIndex = ImageIndex;
				ResetNextLevel(tv);
				r.ViewersHolder.Loader.NotifyChanges();
			}
		}
		void methodMakeWeb_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				Method.AddWebAttribute();
				ImageIndex = TreeViewObjectExplorer.GetMethodImageIndex(Method);
				SelectedImageIndex = ImageIndex;
				ResetNextLevel(tv);
				r.ViewersHolder.Loader.NotifyChanges();
			}
		}
		public override TreeNodeObject LocateObjectNode(Stack<IObjectPointer> ownerStack)
		{
			if (NextLevelLoaded && ownerStack.Count > 0)
			{
				IObjectPointer o = ownerStack.Pop();
				ParameterClass p = o as ParameterClass;
				if (p != null)
				{
					for (int i = 0; i < Nodes.Count; i++)
					{
						TreeNodeCustomMethodParameter np = Nodes[i] as TreeNodeCustomMethodParameter;
						if (np != null)
						{
							if (np.Parameter.ParameterID == p.ParameterID)
							{
								if (ownerStack.Count == 0)
								{
									return np;
								}
								else
								{
									return np.LocateObjectNode(ownerStack);
								}
							}
						}
					}
				}
				else
				{
					LocalVariable lv = o as LocalVariable;
					if (lv == null)
					{
						ComponentIconLocal ci = o as ComponentIconLocal;
						if (ci != null)
						{
							lv = (LocalVariable)ci.ClassPointer;
						}
					}
					if (lv != null)
					{
						for (int i = 0; i < Nodes.Count; i++)
						{
							TreeNodeLocalVariable np2 = Nodes[i] as TreeNodeLocalVariable;
							if (np2 != null)
							{
								if (np2.OwnerVariable.MemberId == lv.MemberId)
								{
									if (ownerStack.Count == 0)
									{
										return np2;
									}
									else
									{
										return np2.LocateObjectNode(ownerStack);
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}
			static void addNodeToSortedList(SortedList<string, TreeNode> sortedNodes, TreeNode node)
			{
				int n = 0;
				string key = node.Text;
				while (sortedNodes.ContainsKey(key))
				{
					n++;
					key = string.Format(CultureInfo.InvariantCulture, "{0}{1}", node.Text, n);
				}
				sortedNodes.Add(key, node);
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
				Dictionary<UInt32, IAction> actions = null;
				if (topClass != null)
				{
					if (!topClass.StaticScope)
					{
						actions = topClass.GetActions();
					}
				}
				else
				{
					TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
					if (rootType != null)
					{
						actions = rootType.GetActions();
					}
				}
				TreeNodeCustomMethod mNode = parentNode as TreeNodeCustomMethod;
				MethodClass mmp = mNode.Method;
				if (mmp.ParameterCount > 0)
				{
					List<ParameterClass> parameters = mmp.Parameters;
					foreach (ParameterClass p in parameters)
					{
						if (parentNode.SelectionTarget == EnumObjectSelectType.All)
						{
							TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, p);
							parentNode.Nodes.Add(tmp);
						}
						else
						{
							CustomMethodParameterPointer cmp = new CustomMethodParameterPointer(p);
							TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, cmp);
							parentNode.Nodes.Add(tmp);
						}
					}
				}
				if (mNode.ShowVariables)
				{
					List<ComponentIcon> iconList;
					if (mmp.CurrentSubEditor != null)
					{
						iconList = mmp.CurrentSubEditor.IconList;
					}
					else
					{
						iconList = mmp.ComponentIconList;
					}
					SortedList<string, TreeNode> sortedNodes = new SortedList<string, TreeNode>();
					foreach (ComponentIcon ci in iconList)
					{
						ComponentIconLocal cil = ci as ComponentIconLocal;
						if (cil != null)
						{
							LocalVariable lvp = (LocalVariable)cil.ClassPointer;
							lvp.Owner = mmp;
							cil.SetOwnerMethod(mmp);
							TreeNodeLocalVariable tlv = new TreeNodeLocalVariable(tv, lvp);
							addNodeToSortedList(sortedNodes, tlv);
						}
					}
					IMethod0[] subms = mmp.SubMethod.ToArray();
					if (subms != null && subms.Length > 0)
					{
						for (int k = 0; k < subms.Length; k++)
						{
							if (subms[k] == null)
							{
								throw new DesignerException("Submethod [{0}] for method [{1}] is null", k, mmp.MethodName);
							}
							IList<IParameter> ps = subms[k].MethodParameterTypes;
							if (ps != null && ps.Count > 0)
							{
								for (int i = 0; i < ps.Count; i++)
								{
									if (ps[i] == null)
									{
										throw new DesignerException("Parameter [{0}] for submethod [{1}] is null", i, subms[k].MethodName);
									}
									ActionBranchParameter ap = ps[i] as ActionBranchParameter;
									if (ap != null)
									{
										ActionBranchParameterPointer cmp = ap.CreatePointer();
										TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, cmp);
										addNodeToSortedList(sortedNodes, tmp);
									}
									else
									{
										ParameterClass p = ps[i] as ParameterClass;
										if (p != null)
										{
											CustomMethodParameterPointer cmp = new CustomMethodParameterPointer(p);
											TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, cmp);
											addNodeToSortedList(sortedNodes, tmp);
										}
										else
										{
											SubMethodInfoPointer sp = subms[k] as SubMethodInfoPointer;
											if (sp != null)
											{
												ParameterClassSubMethod p0 = sp.GetParameterById(ps[i].ParameterID);
												ActionBranchParameterPointer ap0 = p0.CreatePointer();
												TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, ap0);
												addNodeToSortedList(sortedNodes, tmp);
											}
											else
											{
												throw new DesignerException("Unrecognized IParameter type {0}", ps[i].GetType().AssemblyQualifiedName);
											}
										}
									}
								}
							}
						}
					}
					if (sortedNodes.Count > 0)
					{
						IEnumerator<KeyValuePair<string, TreeNode>> ie = sortedNodes.GetEnumerator();
						while (ie.MoveNext())
						{
							parentNode.Nodes.Add(ie.Current.Value);
						}
					}
				}
				if (actions != null)
				{
					UInt32 scopeId = parentNode.ScopeMethodId;
					foreach (IAction a in actions.Values)
					{
						if (a != null)
						{
							if (a.ScopeMethodId != 0)
							{
								if (a.ScopeMethodId != scopeId)
								{
									continue;
								}
							}
							if (a.ActionMethod != null)
							{
								if (a.ActionMethod.IsSameMethod(mmp))
								{
									parentNode.Nodes.Add(new TreeNodeAction(ForStatic, a));
								}
							}
						}
					}
				}
			}
		}
	}
	public class TreeNodeClassType : TreeNodeObject, IRootNode, ITreeNodeClass, ITypeNode
	{
		private RootComponentData _data;
		public TreeNodeClassType(TreeViewObjectExplorer tv, TypePointer objectPointer, RootComponentData rootComponent, UInt32 scopeMethodId)
			: base(true, new DataTypePointer(objectPointer))
		{
			bool isForStatic = true;
			_data = rootComponent;
			ImageIndex = TreeViewObjectExplorer.GetTypeIcon((Type)objectPointer.ClassType);
			SelectedImageIndex = ImageIndex;
			init(tv, isForStatic, scopeMethodId);
		}
		public TreeNodeClassType(TreeViewObjectExplorer tv, bool isForStatic, TypePointer objectPointer, string title, Bitmap bmp, UInt32 scopeMethodId)
			: base(isForStatic, new DataTypePointer(objectPointer))
		{
			Text = title;
			ImageIndex = TreeViewObjectExplorer.GetTypeIcon(objectPointer.ClassType, bmp);
			SelectedImageIndex = ImageIndex;
			init(tv, isForStatic, scopeMethodId);
		}
		public TreeNodeClassType(TreeViewObjectExplorer tv, bool isForStatic, DataTypePointer objectPointer, string title, Bitmap bmp, UInt32 scopeMethodId)
			: base(isForStatic, objectPointer)
		{
			Text = title;
			ImageIndex = TreeViewObjectExplorer.GetTypeIcon(objectPointer.BaseClassType, bmp);
			SelectedImageIndex = ImageIndex;
			init(tv, isForStatic, scopeMethodId);
		}
		public TreeNodeClassType(TreeViewObjectExplorer tv, Type t, UInt32 scopeMethodId)
			: base(new DataTypePointer(new TypePointer(t)))
		{
			Type td = typeof(Delegate);
			if (td.IsAssignableFrom(t))
			{
				MethodInfo mi = t.GetMethod("Invoke");
				if (t != null)
				{
					int c;
					Text = MethodPointer.GetMethodSignature(mi, t.Name, true, out c);
				}
			}
			else
			{
				Text = VPLUtil.GetTypeDisplay(t);
			}
			setTypeIcon(t);
			SelectedImageIndex = ImageIndex;
			init(tv, false, scopeMethodId);
		}
		public TreeNodeClassType(TreeViewObjectExplorer tv, bool isForStatic, Type t, UInt32 scopeMethodId)
			: base(isForStatic, new DataTypePointer(new TypePointer(t)))
		{
			Type td = typeof(Delegate);
			if (td.IsAssignableFrom(t))
			{
				MethodInfo mi = t.GetMethod("Invoke");
				if (mi != null)
				{
					int c;
					Text = MethodPointer.GetMethodSignature(mi, t.Name, true, out c);
				}
				else
				{
					Text = t.Name;
				}
			}
			else
			{
				Text = t.Name;
			}
			setTypeIcon(t);
			SelectedImageIndex = ImageIndex;
			init(tv, isForStatic, scopeMethodId);
		}
		private void setTypeIcon(Type t)
		{
			if (t.IsInterface)
				ImageIndex = TreeViewObjectExplorer.IMG_Interface;
			else if (t.IsEnum)
				ImageIndex = TreeViewObjectExplorer.IMG_enumValues;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_Type;
		}
		private void init(TreeViewObjectExplorer tv, bool isForStatic, UInt32 scopeMethodId)
		{
			if (OwnerDataType.IsGenericParameter || OwnerDataType.IsGenericType)
			{
			}
			else
			{
				EnumObjectSelectType target = this.SelectionTarget;
				DataTypePointer scope = this.TargetScope;
				string eventName = this.EventScope;
				if (isForStatic)
				{
					if (target == EnumObjectSelectType.Type || target == EnumObjectSelectType.BaseClass || target == EnumObjectSelectType.Interface)
					{
						//show non-static members for illustrating purpose
						this.Nodes.Add(new TreeNodePropertyCollection(tv, this, false, this.OwnerPointer, scopeMethodId));
						this.Nodes.Add(new TreeNodeFieldCollection(tv, this, false, this.OwnerPointer, scopeMethodId));
						this.Nodes.Add(new TreeNodeMethodCollection(tv, this, false, this.OwnerPointer, scopeMethodId));
						this.Nodes.Add(new TreeNodeEventCollection(tv, this, false, this.OwnerPointer, scopeMethodId));
					}
				}
				this.Nodes.Add(new TreeNodePropertyCollection(tv, this, isForStatic, this.OwnerPointer, scopeMethodId));
				this.Nodes.Add(new TreeNodeFieldCollection(tv, this, isForStatic, this.OwnerPointer, scopeMethodId));
				this.Nodes.Add(new TreeNodeMethodCollection(tv, this, isForStatic, this.OwnerPointer, scopeMethodId));
				this.Nodes.Add(new TreeNodeEventCollection(tv, this, isForStatic, this.OwnerPointer, scopeMethodId));
				if (!isForStatic)
				{
					this.Nodes.Add(new TreeNodePropertyCollection(tv, this, true, this.OwnerPointer, scopeMethodId));
					this.Nodes.Add(new TreeNodeFieldCollection(tv, this, true, this.OwnerPointer, scopeMethodId));
					this.Nodes.Add(new TreeNodeMethodCollection(tv, this, true, this.OwnerPointer, scopeMethodId));
					this.Nodes.Add(new TreeNodeEventCollection(tv, this, true, this.OwnerPointer, scopeMethodId));
				}
			}
		}
		private void editResources(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.RootClassNode != null)
				{
					LimnorProject prj = tv.RootClassNode.RootObjectId.Project;
					if (DlgResMan.EditResources(tv.FindForm(), prj) == DialogResult.OK)
					{

					}
				}
			}
		}
		private void removeThisExternType(object sender, EventArgs e)
		{
			if (_data != null && _data.DesignerHolder != null)
			{
				if (HasAction())
				{
					MessageBox.Show(this.TreeView.FindForm(), "Cannot remove this type. There are actions using it.", "Type reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					XmlNodeList nds = _data.DesignerHolder.Loader.Node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
					"Types/{0}", XmlTags.XML_Item));
					foreach (XmlNode nd in nds)
					{
						Type t = XmlUtil.GetLibTypeAttribute(nd);
						if (t != null)
						{
							if (t.Equals(OwnerDataType))
							{
								string nm = XmlUtil.GetLibTypeAttributeString(nd);
								XmlNodeList nds0;
								nds0 = _data.DesignerHolder.Loader.Node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
									"Types/{0}[@ownerTypeName='{1}']", XmlTags.XML_Item, nm));
								if (nds0 != null && nds0.Count > 0)
								{
									MessageBox.Show(this.TreeView.FindForm(), "Cannot remove this type. It is being used.", "Type reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
									return;
								}
								nds0 = _data.DesignerHolder.Loader.Node.SelectNodes(string.Format(CultureInfo.InvariantCulture,
									"//*[@{0}='{1}']", XmlTags.XMLATT_type, nm));
								foreach (XmlNode nx in nds0)
								{
									if (string.CompareOrdinal(nx.Name, "Item") == 0 && nx.ParentNode != null && string.CompareOrdinal(nx.ParentNode.Name, "Types") == 0)
									{
									}
									else
									{
										MessageBox.Show(this.TreeView.FindForm(), "Cannot remove this type. It is being used.", "Type reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
										return;
									}
								}
								nd.ParentNode.RemoveChild(nd);
								break;
							}
						}
					}
					this.Remove();
					SerializeUtil.RemoveExternalTypeNode(_data.DesignerHolder.Loader.Node, OwnerDataType);
					TypePointerCollection tps = _data.ObjectMap.GetTypedData<TypePointerCollection>();
					if (tps != null)
					{
						foreach (TypePointer tp in tps)
						{
							if (OwnerDataType.Equals(tp.ClassType))
							{
								tps.Remove(tp);
								break;
							}
						}
					}
					_data.DesignerHolder.OnRemoveExternalType(_data.RootClassID.ClassId, OwnerDataType);
					_data.DesignerHolder.Loader.NotifyChanges();
				}
			}
		}
		public void SetNodeIcon()
		{
			ImageIndex = TreeViewObjectExplorer.GetTypeIcon(OwnerDataType);
			SelectedImageIndex = ImageIndex;
		}
		public override bool IsStatic { get { return true; } }
		public bool CanRemove { get; set; }
		public UInt32 ClassId
		{
			get
			{
				return ((DataTypePointer)this.OwnerPointer).ClassId;
			}
		}
		public TreeNodePME GetMemberNode(bool isStatic)
		{
			return null;
		}
		public override TreeNodeAction FindActionNode(IAction act)
		{
			if (act.IsStatic)
			{
				TreeNodeAction tna = null;
				for (int j = 0; j < Nodes.Count; j++)
				{
					TreeNodeObjectCollection tnoc = Nodes[j] as TreeNodeObjectCollection;
					if (tnoc != null)
					{
						if (tnoc.NextLevelLoaded)
						{
							for (int k = 0; k < tnoc.Nodes.Count; k++)
							{
								TreeNodeObject tno = tnoc.Nodes[k] as TreeNodeObject;
								if (tno != null)
								{
									tna = tno.FindActionNode(act);
									if (tna != null)
									{
										tna.Text = act.ActionName;
										break;
									}
								}
							}
							if (tna != null)
							{
								break;
							}
						}
					}
				}
				return tna;
			}
			return null;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (CanRemove)
			{
				if (_data != null && _data.DesignerHolder != null)
				{
					MenuItem[] mi = new MenuItem[1];
					mi[0] = new MenuItemWithBitmap("Remove", removeThisExternType, Resources._cancel.ToBitmap());
					return mi;
				}
			}
			else
			{
				if (typeof(ProjectResources).Equals(OwnerDataType))
				{
					List<MenuItem> l = new List<MenuItem>();
					l.Add(new MenuItemWithBitmap("Resource Manager", editResources, Resources.resx.ToBitmap()));
					TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
					if (tv != null)
					{
						if (tv.Project != null)
						{
							ProjectResources rm = tv.Project.GetProjectSingleData<ProjectResources>();
							rm.AddLanguageSelectionMenu(l);
						}
					}
					return l.ToArray();
				}
			}
			return null;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Class; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return !(pointer is ClassPointer);
		}

		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			DataTypePointer tp = this.OwnerPointer as DataTypePointer;
			if (sameLevel)
			{
				return tp.IsSameObjectRef(act.MethodOwner);
			}
			else
			{
				IObjectIdentity o = act.MethodOwner as IObjectIdentity;
				while (o != null)
				{
					if (o.IsSameObjectRef(tp))
					{
						return true;
					}
					o = o.IdentityOwner;
				}
			}
			return false;
		}
		public override bool IsTargeted()
		{
			DataTypePointer tp = this.OwnerPointer as DataTypePointer;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					return false;
				}
				if (tv.SelectionType == EnumObjectSelectType.Object)
				{
					if (this.TargetScope != null)
					{
						return TargetScope.IsAssignableFrom(tp.ObjectType);
					}
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.BaseClass)
				{
					if (tp.IsLibType)
					{
						//cannot use a sealed class for base class
						if (tp.ObjectType.IsSealed)
						{
							return false;
						}
						//interface will be handled differently
						if (tp.ObjectType.IsInterface)
						{
							return false;
						}
					}
					else
					{
						if (tp.ClassTypePointer.IsStatic)
						{
							return false;
						}
					}
					return true;
				}
				else if (tv.SelectionType == EnumObjectSelectType.Type)
				{
					if (this.TargetScope != null)
					{
						return TargetScope.IsAssignableFrom(tp.ObjectType);
					}
					return true;
				}
				else if (tv.SelectionType == EnumObjectSelectType.InstanceType)
				{
					if (this.TargetScope == null || TargetScope.IsAssignableFrom(tp.ObjectType))
					{
						if (tp.IsLibType)
						{
						}
						else
						{
							if (tp.ClassTypePointer.IsStatic)
							{
								return false;
							}
						}
						return true;
					}
					return false;
				}
				if (tv.SelectionType == EnumObjectSelectType.Interface)
				{
					return tp.IsInterface;
				}
				return (tv.SelectionType == EnumObjectSelectType.All);
			}
			return false;
		}
		public bool HasAction()
		{
			if (_data != null)
			{
				if (_data.ActionList == null)
				{
					_data.ReloadActionList();
				}
				Type t = this.OwnerDataType;
				foreach (IAction a in _data.ActionList.Values)
				{
					DataTypePointer dtp = null;
					IObjectPointer op = a.MethodOwner;
					while (op != null)
					{
						dtp = op as DataTypePointer;
						if (dtp != null)
							break;
						op = op.Owner;
					}
					if (dtp != null)
					{
						if (dtp.BaseClassType.Equals(t))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		//public override 
		#region IRootNode Members

		public Dictionary<UInt32, IAction> GetActions()
		{
			Dictionary<UInt32, IAction> acts = new Dictionary<UInt32, IAction>();
			if (_data != null)
			{
				if (_data.ActionList == null)
				{
					_data.ReloadActionList();
				}
				TypePointer tp = this.OwnerPointer as TypePointer;
				if (tp != null)
				{
					foreach (IAction a in _data.ActionList.Values)
					{
						if (tp.IsSameObjectRef(a.MethodOwner))
						{
							acts.Add(a.ActionId, a);
						}
					}
				}
				else
				{
					DataTypePointer dp = this.OwnerPointer as DataTypePointer;
					if (dp != null)
					{
						foreach (IAction a in _data.ActionList.Values)
						{
							if (dp.IsSameObjectRef(a.MethodOwner))
							{
								acts.Add(a.ActionId, a);
							}
						}
					}
				}
			}
			return acts;
		}
		public IAction GetAction(UInt64 id)
		{
			throw new NotImplementedException();
		}
		public EventAction GetEventHandler(IEvent ep)
		{
			throw new NotImplementedException();
		}
		public bool HasEventHandler(IEvent ep)
		{
			throw new NotImplementedException();
		}
		public void OnAddVariable(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}
		public override bool IncludeStaticOnly
		{
			get
			{
				return true;
			}
		}
		public RootComponentData ClassData
		{
			get
			{
				return _data;
			}
		}
		public void NotifyChanges()
		{
		}
		public MultiPanes ViewersHolder
		{
			get
			{
				if (_data != null)
				{
					return _data.DesignerHolder;
				}
				return null;
			}
		}
		#endregion
		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			IObjectIdentity p = pointer;
			Stack<IObjectIdentity> pointerStack = new Stack<IObjectIdentity>();
			while (p != null && !(p is IClass))
			{
				pointerStack.Push(p);
				p = p.IdentityOwner;
			}
			if (pointerStack.Count > 0)
			{
				p = pointerStack.Pop();//p is the top level non-IClass 
				IProperty prop = null;
				SetterPointer sp = p as SetterPointer;
				if (sp != null)
				{
					prop = sp.SetProperty;
				}
				if (prop != null)
				{
					TreeNodeObjectCollection c = TreeViewObjectExplorer.GetCollectionNode(this, prop);
					return c.FindObjectNode(prop);
				}
				TreeNodeObjectCollection toc = TreeViewObjectExplorer.GetCollectionNode(this, p);
				if (toc != null)
				{
					return toc.FindObjectNode(p, pointerStack);
				}
			}
			return null;
		}
		public void ResetPropertyNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (forCustom)
				{
				}
				else
				{
					TreeNodePropertyCollection pc = Nodes[i] as TreeNodePropertyCollection;
					if (pc != null)
					{
						pc.ResetNextLevel(tv);
						break;
					}
				}
			}
		}
		public void ResetMethodNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (forCustom)
				{
				}
				else
				{
					TreeNodeMethodCollection pc = Nodes[i] as TreeNodeMethodCollection;
					if (pc != null)
					{
						pc.ResetNextLevel(tv);
						break;
					}
				}
			}
		}
		public void ResetEventNode(bool forStatic, bool forCustom)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			for (int i = 0; i < Nodes.Count; i++)
			{
				if (forCustom)
				{
				}
				else
				{
					TreeNodeEventCollection pc = Nodes[i] as TreeNodeEventCollection;
					if (pc != null)
					{
						pc.ResetNextLevel(tv);
						break;
					}
				}
			}
		}


		#region ITypeNode Members

		public Type OwnerDataType
		{
			get
			{
				return ((DataTypePointer)this.OwnerPointer).BaseClassType;
			}
		}
		public void HandleActionModified(UInt32 classId, IAction act, bool isNewAction)
		{
			TreeViewObjectExplorer tv = TreeView as TreeViewObjectExplorer;
			Type tp0 = this.OwnerDataType;
			IObjectPointer op = act.ActionMethod as IObjectPointer;
			if (op != null)
			{
				DataTypePointer tp = null;
				IObjectPointer p = op.Owner;
				while (p != null)
				{
					tp = p as DataTypePointer;
					if (tp != null)
					{
						break;
					}
					p = p.Owner;
				}
				if (tp != null)
				{
					if (tp0.Equals(tp.BaseClassType))
					{
						//show it in P/M
						IProperty prop = null;
						SetterPointer sp = act.ActionMethod as SetterPointer;
						if (sp != null)
						{
							prop = sp.SetProperty;
						}
						if (prop != null)
						{
							//locate the property node
							TreeNodeObject tno = FindObjectNode(prop);
							if (tno != null)
							{
								tno.ResetNextLevel(tv);
								tno.ShowActionIcon();
							}
						}
						else
						{
							//locate the method node
							MethodPointer mp = act.ActionMethod as MethodPointer;
							if (mp != null)
							{
								TreeNodeObject tno = FindObjectNode(mp);
								if (tno != null)
								{
									tno.ResetNextLevel(tv);
									tno.ShowActionIcon();
								}
							}
						}
					}
				}
			}
		}
		public override object Clone()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			TreeNodeClassType obj = new TreeNodeClassType(tv, this.IsStatic, this.OwnerDataType, this.ScopeMethodId);
			obj._data = _data;
			return obj;
		}
		#endregion

		public override string Tooltips
		{
			get
			{
				DataTypePointer pp = (DataTypePointer)this.OwnerPointer;
				return PMEXmlParser.GetTypeDescription(pp.BaseClassType);
			}
		}
	}
	public class TreeNodeInterfaceBaseCollection : TreeNodeObject
	{
		public TreeNodeInterfaceBaseCollection(InterfacePointer interfaceType)
			: base(false, interfaceType)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_Interfaces;
			SelectedImageIndex = ImageIndex;
			Text = "Base Interfaces";
			Nodes.Add(new CLoad());
		}
		public InterfacePointer InterfaceType
		{
			get
			{
				return (InterfacePointer)(this.OwnerPointer);
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (!InterfaceType.IsLibType)
				{
					MenuItem[] menus = new MenuItem[1];
					menus[0] = new MenuItemWithBitmap("Add base interface", add_Click, Resources._interfaceIcon.ToBitmap());
					return menus;
				}
			}
			return null;
		}

		void add_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				ClassPointer cp = InterfaceType.VariableCustomType;
				DataTypePointer dp = DesignUtil.SelectDataType(cp.Project, tv.RootId, null, EnumObjectSelectType.Interface, null, null, null, tv.FindForm());
				if (dp != null)
				{
					InterfacePointer ip;
					ClassPointer cp2 = dp.VariableCustomType;
					if (cp2 != null)
					{
						if (cp2.IsSameObjectRef(cp))
						{
							return;
						}
						ip = new InterfacePointer(cp2);
					}
					else
					{
						ip = new InterfacePointer(new TypePointer(dp.VariableLibType));
					}
					cp.Interface.AddBaseInterface(ip, tv.RootClassNode.ViewersHolder.Loader);
				}
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfaceBaseCollection ibc = parentNode as TreeNodeInterfaceBaseCollection;
				List<InterfacePointer> bases = ibc.InterfaceType.BaseInterfaces;
				if (bases != null && bases.Count > 0)
				{
					foreach (InterfacePointer ip in bases)
					{
						TreeNodeInterfacePointer node = new TreeNodeInterfacePointer(ip);
						parentNode.Nodes.Add(node);
					}
				}
			}
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get
			{
				DataTypePointer dp = InterfaceType;
				if (dp.IsLibType)
					return EnumObjectDevelopType.Library;
				return EnumObjectDevelopType.Custom;
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}
	public class TreeNodeClassInterface : TreeNodeClassRoot
	{
		private TreeNodeInterfacePropertyCollection _properties;
		private TreeNodeInterfaceEventCollection _events;
		private TreeNodeInterfaceMethodCollection _methods;
		private TreeNodeInterfaceBaseCollection _bases;
		public TreeNodeClassInterface(TreeViewObjectExplorer tv, bool isMain, ClassPointer rd)
			: base(tv, isMain, rd, false)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_Interface2;
			SelectedImageIndex = ImageIndex;
			Text = rd.Name;
		}
		public override void ResetNextLevel(TreeViewObjectExplorer tv)
		{
			base.ResetNextLevel(tv);
			ClassPointer rd = this.OwnerPointer as ClassPointer;
			_methods = new TreeNodeInterfaceMethodCollection(new InterfacePointer(rd));
			_bases = new TreeNodeInterfaceBaseCollection(new InterfacePointer(rd));
			_properties = new TreeNodeInterfacePropertyCollection(new InterfacePointer(rd));
			_events = new TreeNodeInterfaceEventCollection(new InterfacePointer(rd));
			Nodes.Add(_bases);
			Nodes.Add(_properties);
			Nodes.Add(_methods);
			Nodes.Add(_events);
		}
		public TreeNodeInterfaceMethodCollection MethodsNode
		{
			get
			{
				return _methods;
			}
		}
		public TreeNodeInterfacePropertyCollection PropertiesNode
		{
			get
			{
				return _properties;
			}
		}
		public TreeNodeInterfaceEventCollection EventsNode
		{
			get
			{
				return _events;
			}
		}
		public TreeNodeInterfaceBaseCollection BaseInterfacesNode
		{
			get
			{
				return _bases;
			}
		}
		public ClassPointer InterfaceHolder
		{
			get
			{
				return this.OwnerPointer as ClassPointer;
			}
		}
		public InterfaceClass Interface
		{
			get
			{
				return VPLUtil.GetObject(InterfaceHolder.ObjectInstance) as InterfaceClass;
			}
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Custom; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}
	}
	public class TreeNodeInterfaceMethodCollection : TreeNodeInterfaceMember
	{
		public TreeNodeInterfaceMethodCollection(InterfacePointer objectPointer)
			: base(objectPointer)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_METHODS;
			SelectedImageIndex = ImageIndex;
			Text = "Methods";
			this.Nodes.Add(new CLoad());
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (!this.Interface.IsLibType)
				{
					MenuItem[] menus = new MenuItem[1];
					menus[0] = new MenuItemWithBitmap("Add method", addMethod, Resources._methodIcon.ToBitmap());
					return menus;
				}
			}
			return null;
		}
		private void addMethod(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				InterfaceClass ifc = this.Interface.ClassTypePointer.Interface;
				InterfaceElementMethod m0 = ifc.AddNewMethod(root.ViewersHolder.Loader);
				if (this.TreeView != null)
				{
					//
					this.LoadNextLevel();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeInterfaceMethod tnm = Nodes[i] as TreeNodeInterfaceMethod;
						if (tnm != null)
						{
							if (tnm.Method.MethodId == m0.MethodId)
							{
								this.TreeView.SelectedNode = tnm;
								break;
							}
						}
					}
				}
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfaceMethodCollection imc = parentNode as TreeNodeInterfaceMethodCollection;
				InterfacePointer ifc = imc.Interface;
				if (ifc.Methods != null)
				{
					foreach (InterfaceElementMethod m in ifc.Methods)
					{
						parentNode.Nodes.Add(new TreeNodeInterfaceMethod(m, ifc));
					}
				}
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}

	//
	public class TreeNodeInterfaceEvent : TreeNodeInterfaceMember
	{
		private InterfaceElementEvent _event;
		public TreeNodeInterfaceEvent(InterfaceElementEvent eventType, InterfacePointer root)
			: base(root)
		{
			_event = eventType;
			ImageIndex = TreeViewObjectExplorer.IMG_EVENT;
			SelectedImageIndex = ImageIndex;
			Text = eventType.ToString();
			this.Nodes.Add(new CLoad());
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(_event);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				MenuItem[] menus = new MenuItem[1];
				menus[0] = new MenuItemWithBitmap("Remove", remove, Resources._cancel.ToBitmap());
				return menus;
			}
			return null;
		}
		void remove(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				if (MessageBox.Show(this.TreeView.FindForm(), "Do you want to remove this event from the interface?", "Interface method", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					_event.Interface.ClassTypePointer.Interface.DeleteEvent(root.ViewersHolder.Loader, _event.EventId);
				}
			}
		}
		public InterfaceElementEvent Event
		{
			get
			{
				return _event;
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfaceEvent ifm = parentNode as TreeNodeInterfaceEvent;
				InterfaceElementEvent e = ifm.Event;
				if (e.Parameters != null)
				{
					foreach (NamedDataType dp in e.Parameters)
					{
						parentNode.Nodes.Add(new TreeNodeInterfaceEventParameter(e, dp, ifm.Interface));
					}
				}
			}
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}
	public class TreeNodeInterfaceEventParameter : TreeNodeInterfaceMember
	{
		public TreeNodeInterfaceEventParameter(InterfaceElementEvent eventType, NamedDataType p, InterfacePointer root)
			: base(root)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			Text = p.ToString();
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
	}
	public class TreeNodeInterfaceEventCollection : TreeNodeInterfaceMember
	{
		public TreeNodeInterfaceEventCollection(InterfacePointer objectPointer)
			: base(objectPointer)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_EVENTS;
			SelectedImageIndex = ImageIndex;
			Text = "Events";
			this.Nodes.Add(new CLoad());
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Event; }
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (!this.Interface.IsLibType)
				{
					MenuItem[] menus = new MenuItem[1];
					menus[0] = new MenuItemWithBitmap("Add event", addEvent, Resources._event1.ToBitmap());
					return menus;
				}
			}
			return null;
		}
		private void addEvent(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				InterfaceClass ifc = this.Interface.ClassTypePointer.Interface;
				InterfaceElementEvent m0 = ifc.AddNewEvent(root.ViewersHolder.Loader);
				if (this.TreeView != null)
				{
					//
					this.LoadNextLevel();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeInterfaceEvent tnm = Nodes[i] as TreeNodeInterfaceEvent;
						if (tnm != null)
						{
							if (tnm.Event.EventId == m0.EventId)
							{
								this.TreeView.SelectedNode = tnm;
								break;
							}
						}
					}
				}
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfaceEventCollection imc = parentNode as TreeNodeInterfaceEventCollection;
				InterfacePointer ifc = imc.Interface;
				if (ifc.Events != null)
				{
					foreach (InterfaceElementEvent m in ifc.Events)
					{
						parentNode.Nodes.Add(new TreeNodeInterfaceEvent(m, ifc));
					}
				}
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}
	//

	//
	public class TreeNodeInterfaceProperty : TreeNodeInterfaceMember
	{
		private InterfaceElementProperty _property;
		public TreeNodeInterfaceProperty(InterfaceElementProperty property, InterfacePointer root)
			: base(root)
		{
			_property = property;
			ImageIndex = TreeViewObjectExplorer.IMG_PROPERTY;
			SelectedImageIndex = ImageIndex;
			Text = _property.ToString();
			this.Nodes.Add(new CLoad());
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(_property);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				MenuItem[] menus = new MenuItem[1];
				menus[0] = new MenuItemWithBitmap("Remove", remove, Resources._cancel.ToBitmap());
				return menus;
			}
			return null;
		}
		void remove(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				if (MessageBox.Show(this.TreeView.FindForm(), "Do you want to remove this property from the interface?", "Interface property", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					_property.Interface.ClassTypePointer.Interface.DeleteProperty(root.ViewersHolder.Loader, _property.PropertyId);
				}
			}
		}
		public InterfaceElementProperty Property
		{
			get
			{
				return _property;
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfaceProperty ifm = parentNode as TreeNodeInterfaceProperty;
				InterfaceElementProperty property = ifm.Property;
				ClassPointer cp = property.VariableCustomType;
				if (cp != null)
				{
				}
				else
				{
					Type t = property.VariableLibType;
					if (t != null)
					{
						UInt32 scopeId = parentNode.ScopeMethodId;
						parentNode.Nodes.Add(new TreeNodePropertyCollection(tv, parentNode, false, property, scopeId));
						parentNode.Nodes.Add(new TreeNodeMethodCollection(tv, parentNode, false, property, scopeId));
						parentNode.Nodes.Add(new TreeNodeEventCollection(tv, parentNode, false, property, scopeId));
					}
				}
			}
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}
	public class TreeNodeInterfacePropertyCollection : TreeNodeInterfaceMember
	{
		public TreeNodeInterfacePropertyCollection(InterfacePointer objectPointer)
			: base(objectPointer)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_PROPERTIES;
			SelectedImageIndex = ImageIndex;
			Text = "Properties";
			this.Nodes.Add(new CLoad());
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (!this.Interface.IsLibType)
				{
					MenuItem[] menus = new MenuItem[1];
					menus[0] = new MenuItemWithBitmap("Add property", addProperty, Resources._prop.ToBitmap());
					return menus;
				}
			}
			return null;
		}
		private void addProperty(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				InterfaceClass ifc = this.Interface.ClassTypePointer.Interface;
				InterfaceElementProperty p0 = ifc.AddNewProperty(root.ViewersHolder.Loader);
				if (this.TreeView != null)
				{
					//
					this.LoadNextLevel();
					for (int i = 0; i < this.Nodes.Count; i++)
					{
						TreeNodeInterfaceProperty tnm = Nodes[i] as TreeNodeInterfaceProperty;
						if (tnm != null)
						{
							if (tnm.Property.PropertyId == p0.PropertyId)
							{
								this.TreeView.SelectedNode = tnm;
								break;
							}
						}
					}
				}
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfacePropertyCollection imc = parentNode as TreeNodeInterfacePropertyCollection;
				InterfacePointer ifc = imc.Interface;
				List<InterfaceElementProperty> ls = ifc.GetProperties();
				if (ls != null)
				{
					foreach (InterfaceElementProperty m in ls)
					{
						parentNode.Nodes.Add(new TreeNodeInterfaceProperty(m, ifc));
					}
				}
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}
	//

	public class TreeNodeInterfacePointer : TreeNodeObject
	{
		public TreeNodeInterfacePointer(InterfacePointer interfaceType)
			: base(false, interfaceType)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_Interface2;
			SelectedImageIndex = ImageIndex;
			Text = interfaceType.Name;
			Nodes.Add(new CLoad());
		}
		public InterfacePointer Interface
		{
			get
			{
				return (InterfacePointer)(this.OwnerPointer);
			}
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Interface; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get
			{
				if (Interface.IsLibType)
					return EnumObjectDevelopType.Library;
				return EnumObjectDevelopType.Custom;
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfacePointer ibc = parentNode as TreeNodeInterfacePointer;
				List<InterfacePointer> bases = ibc.Interface.BaseInterfaces;
				if (bases != null && bases.Count > 0)
				{
					TreeNodeInterfaceBaseCollection bc = new TreeNodeInterfaceBaseCollection(ibc.Interface);
					parentNode.Nodes.Add(bc);
				}
				if (ibc.Interface.Methods != null && ibc.Interface.Methods.Count > 0)
				{
					TreeNodeInterfaceMethodCollection mc = new TreeNodeInterfaceMethodCollection(ibc.Interface);
					parentNode.Nodes.Add(mc);
				}
				List<InterfaceElementProperty> ls = ibc.Interface.GetProperties();
				if (ls != null && ls.Count > 0)
				{
					TreeNodeInterfacePropertyCollection mc = new TreeNodeInterfacePropertyCollection(ibc.Interface);
					parentNode.Nodes.Add(mc);
				}
				if (ibc.Interface.Events != null && ibc.Interface.Events.Count > 0)
				{
					TreeNodeInterfaceEventCollection mc = new TreeNodeInterfaceEventCollection(ibc.Interface);
					parentNode.Nodes.Add(mc);
				}
			}
		}
	}
	public abstract class TreeNodeInterfaceMember : TreeNodeObject
	{
		public TreeNodeInterfaceMember(InterfacePointer root)
			: base(false, root)
		{
		}
		public ClassPointer InterfaceHolder
		{
			get
			{
				return Interface.ClassTypePointer;
			}
		}
		public InterfacePointer Interface
		{
			get
			{
				return this.OwnerPointer as InterfacePointer;
			}
		}
		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Custom; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}
	}
	public class TreeNodeInterfaceMethod : TreeNodeInterfaceMember
	{
		private InterfaceElementMethod _method;
		public TreeNodeInterfaceMethod(InterfaceElementMethod method, InterfacePointer root)
			: base(root)
		{
			_method = method;
			ImageIndex = TreeViewObjectExplorer.IMG_ACTIONGROUP;
			SelectedImageIndex = ImageIndex;
			Text = _method.ToString();

			this.Nodes.Add(new CLoad());
		}
		public override IAction CreateNewAction()
		{
			ActionClass act = new ActionClass(this.MainClassPointer);
			InterfaceElementMethod mp2 = (InterfaceElementMethod)_method.Clone();
			act.ActionMethod = mp2.CreatePointer(act);
			act.ActionName = mp2.DefaultActionName;
			return act;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					return false;
				}
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Method
					|| tv.SelectionType == EnumObjectSelectType.Action
					)
				{
					if (!this.IsStatic)
					{
						if (this.OwnerPointer == null)
						{
							return false;
						}
						else
						{
							if (DesignUtil.HasStaticOwner(this.OwnerPointer))
							{
							}
							else if (this.OwnerPointer.RootPointer == null)
							{
								if (!(this.OwnerPointer is InterfacePointer))
								{
									return false;
								}
							}
						}
					}
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Object)
				{
					if (_method.ReturnType != null && _method.ReturnType.BaseClassType.Equals(typeof(void)))
					{
						if (_method.ParameterCount == 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(_method);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				MenuItem[] menus = new MenuItem[3];
				menus[0] = new MenuItemWithBitmap("Add parameter", addParameter, Resources._param.ToBitmap());
				menus[1] = new MenuItem("-");
				menus[2] = new MenuItemWithBitmap("Remove", remove, Resources._cancel.ToBitmap());
				return menus;
			}
			return null;
		}
		void addParameter(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				string baseName = "Parameter";
				int n = 1;
				string name = baseName + n.ToString();
				InterfaceElementMethod method = Method;
				if (method.Parameters != null)
				{
					bool exists = true;
					while (exists)
					{
						exists = false;
						foreach (NamedDataType p in method.Parameters)
						{
							if (p.Name == name)
							{
								exists = true;
								n++;
								name = baseName + n.ToString();
								break;
							}
						}
					}
				}
				else
				{
					method.Parameters = new List<InterfaceElementMethodParameter>();
				}
				InterfaceElementMethodParameter mp = new InterfaceElementMethodParameter(typeof(string), name, method);
				method.Parameters.Add(mp);
				this.InterfaceHolder.Interface.OnMethodChanged(root.ViewersHolder.Loader, method);
			}
		}
		void remove(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				if (MessageBox.Show(this.TreeView.FindForm(), "Do you want to remove this method from the interface?", "Interface method", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					_method.Interface.ClassTypePointer.Interface.DeleteMethod(root.ViewersHolder.Loader, _method.MethodId);
				}
			}
		}
		public InterfaceElementMethod Method
		{
			get
			{
				return _method;
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeInterfaceMethod ifm = parentNode as TreeNodeInterfaceMethod;
				InterfaceElementMethod method = ifm.Method;
				parentNode.Nodes.Add(new TreeNodeAttributeCollection(tv, parentNode, (IAttributeHolder)method, 0));
				parentNode.Nodes.Add(new TreeNodeInterfaceMethodReturn(method, method.ReturnType, ifm.Interface));
				if (method.Parameters != null)
				{
					foreach (InterfaceElementMethodParameter dp in method.Parameters)
					{
						parentNode.Nodes.Add(new TreeNodeInterfaceMethodParameter(method, dp, ifm.Interface));
					}
				}
			}
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new CLoad());
			return l;
		}
	}
	public class TreeNodeInterfaceMethodParameter : TreeNodeInterfaceMember
	{
		private InterfaceElementMethod _method;
		private InterfaceElementMethodParameter _param;
		public TreeNodeInterfaceMethodParameter(InterfaceElementMethod method, InterfaceElementMethodParameter p, InterfacePointer root)
			: base(root)
		{
			_method = method;
			_param = p;
			ImageIndex = TreeViewObjectExplorer.IMG_PARAM;
			SelectedImageIndex = ImageIndex;
			Text = p.ToString();
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(_param);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				MenuItem[] menus = new MenuItem[1];
				menus[0] = new MenuItemWithBitmap("Remove", remove, Resources._cancel.ToBitmap());
				return menus;
			}
			return null;
		}
		void remove(object sender, EventArgs e)
		{
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				if (MessageBox.Show(this.TreeView.FindForm(), "Do you want to remove this parameter?", "Interface method", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					if (_method.Parameters != null)
					{
						for (int i = 0; i < _method.Parameters.Count; i++)
						{
							if (_method.Parameters[i].Name == _param.Name)
							{
								_method.Parameters.Remove(_method.Parameters[i]);
								_method.Interface.ClassTypePointer.Interface.OnMethodChanged(root.ViewersHolder.Loader, _method);
								break;
							}
						}
					}
				}
			}
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
	}
	public class TreeNodeInterfaceMethodReturn : TreeNodeInterfaceMember
	{
		public TreeNodeInterfaceMethodReturn(InterfaceElementMethod method, DataTypePointer p, InterfacePointer root)
			: base(root)
		{
			if (p == null)
			{
				p = new DataTypePointer(new TypePointer(typeof(void)));
				p.Name = "return";
			}
			ImageIndex = TreeViewObjectExplorer.IMG_MethodReturn;
			SelectedImageIndex = ImageIndex;
			Text = p.ToString();
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
	}
	/// <summary>
	/// break point
	/// </summary>
	public class TreeNodeBreakPoint : TreeNodeObject, ITreeNodeBreakPoint
	{
		private IBreakPointOwner _owner;
		private bool _forEnterringEvent;
		private fnOnBool miShowBreakpoint;
		public TreeNodeBreakPoint(bool isForStatic, IBreakPointOwner breakPointOwner, IObjectPointer objectPointer)
			: base(isForStatic, objectPointer)
		{
			//
			miShowBreakpoint = new fnOnBool(showBreakpoint);
			//
			Text = "Break Point";
			this.ImageIndex = TreeViewObjectExplorer.IMG_BreakPoint;
			this.SelectedImageIndex = this.ImageIndex;
			_owner = breakPointOwner;
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		/// <summary>
		/// it does not contain child nodes
		/// </summary>
		/// <param name="pointer"></param>
		/// <returns></returns>
		public override bool CanContain(IObjectIdentity pointer)
		{
			return false;
		}
		public IBreakPointOwner BreakPointOwner
		{
			get
			{
				return _owner;
			}
		}
		public bool ForBeforeExecution
		{
			get
			{
				return _forEnterringEvent;
			}
			set
			{
				_forEnterringEvent = value;
				if (_forEnterringEvent)
				{
					Text = "Break Point at entering event";
				}
				else
				{
					Text = "Break Point at leaving event";
				}
			}
		}
		public void ShowBreakPoint(bool pause)
		{
			this.TreeView.Invoke(miShowBreakpoint, pause);
		}
		private void showBreakpoint(bool pause)
		{
			if (pause)
			{
				this.ImageIndex = TreeViewObjectExplorer.IMG_BreakPointPause;
				this.BackColor = Color.Yellow;
				this.TreeView.SelectedNode = this;
			}
			else
			{
				this.ImageIndex = TreeViewObjectExplorer.IMG_BreakPoint;
				this.BackColor = Color.White;
			}
			this.SelectedImageIndex = this.ImageIndex;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			MenuItem[] mis = new MenuItem[1];
			mis[0] = new MenuItemWithBitmap("Remove", new EventHandler(menu_remove), Resources._cancel.ToBitmap());
			return mis;
		}
		private void menu_remove(object sender, EventArgs e)
		{
			EventAction ea = _owner as EventAction;
			if (ea != null)
			{
				if (ForBeforeExecution)
				{
					ea.BreakBeforeExecute = false;
				}
				else
				{
					ea.BreakAfterExecute = false;
				}
				ea.SaveEventBreakPointsToXml();
			}
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			this.Remove();
			if (tv != null)
			{
				tv.NotifyChanges();
			}
		}
	}
	public class TreeNodeNamespaceList : TreeNodeObject
	{
		public TreeNodeNamespaceList(NamespaceList nl)
			: base(true, nl)
		{
			Text = "Namespaces";
			ImageIndex = TreeViewObjectExplorer.IMG_OBJECTS;
			SelectedImageIndex = ImageIndex;
			Nodes.Add(new CLoader());
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		public NamespaceList Namespaces
		{
			get
			{
				return (NamespaceList)(this.OwnerIdentity);
			}
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeNamespaceList tn = (TreeNodeNamespaceList)parentNode;
				NamespaceList nl = tn.Namespaces;
				tv.LoadReferences(nl);
				IOrderedEnumerable<NamespaceClass> en = nl.GetElementsSorted();
				foreach (NamespaceClass e in en)
				{
					parentNode.Nodes.Add(new TreeNodeNamespace(e));
				}
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return true;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loaders = new List<TreeNodeLoader>();
			loaders.Add(new CLoader());
			return loaders;
		}
	}
	public class TreeNodeNamespace : TreeNodeObject
	{
		public TreeNodeNamespace(NamespaceClass nc)
			: base(true, nc)
		{
			Text = nc.ToString();
			ImageIndex = TreeViewObjectExplorer.IMG_OBJECT;
			SelectedImageIndex = ImageIndex;
			this.Nodes.Add(new CLoader());
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}
		public NamespaceClass Namespace
		{
			get
			{
				return (NamespaceClass)(this.OwnerIdentity);
			}
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader()
				: base(true)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				Type tc = typeof(IDrawDesignControl);
				
				TreeNodeNamespace tn = (TreeNodeNamespace)parentNode;
				NamespaceClass nc = tn.Namespace;
				IOrderedEnumerable<Type> types = nc.GetTypesSorted();
				foreach (Type t in types)
				{
					if (tv.SelectionTypeAttribute != null)
					{
						if (tv.SelectionTypeAttribute.Equals(typeof(PhpTypeAttribute)))
						{
							if (!PhpTypeAttribute.IsPhpType(t))
							{
								continue;
							}
						}
						else if (tv.SelectionTypeAttribute.Equals(typeof(JsTypeAttribute)))
						{
							if (!JsTypeAttribute.IsJsType(t))
							{
								continue;
							}
						}
					}
					if (tc.IsAssignableFrom(t))
					{
					}
					else
					{
						bool bInclude = (parentNode.TargetScope == null || parentNode.TargetScope.IsAssignableFrom(t));
						if (parentNode.SelectionTarget == EnumObjectSelectType.BaseClass)
						{
							if (t.IsInterface)
							{
								bInclude = false;
							}
							else if (t.IsSealed)
							{
								bInclude = false;
							}
						}
						else if (parentNode.SelectionTarget == EnumObjectSelectType.Type)
						{
							if (t.IsAbstract && t.IsSealed)
							{
								bInclude = false;
							}
						}
						else if (parentNode.SelectionTarget == EnumObjectSelectType.Interface)
						{
							bInclude = t.IsInterface;
						}
						if (bInclude)
						{
							if (t.IsEnum)
							{
								parentNode.Nodes.Add(new TreeNodeEnumValues(parentNode.IsStatic, t));
							}
							else
							{
								parentNode.Nodes.Add(new TreeNodeClassType(tv, parentNode.IsStatic, t, tv.ScopeMethodId));
							}
						}
					}
				}
			}
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return true;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loaders = new List<TreeNodeLoader>();
			loaders.Add(new CLoader());
			return loaders;
		}
		public override string Tooltips
		{
			get
			{
				NamespaceClass pp = (NamespaceClass)this.OwnerIdentity;
				ReadOnlyCollection<string> lst = pp.AssemblyNames;
				foreach (string s in lst)
				{
					string desc = PMEXmlParser.GetNamespaceDescription(s, pp.Namespace);
					if (!string.IsNullOrEmpty(desc))
						return desc;
				}
				return "";
			}
		}
	}
	#region Mixed Properties
	public class TreeNodePropertyMixedCollection : TreeNodeCustomPropertyCollection
	{
		public TreeNodePropertyMixedCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, true, objectPointer, scopeMethodId)
		{
			if (!HasActions)
			{
				ImageIndex = TreeViewObjectExplorer.IMG_SC_PROPERTIES;
				SelectedImageIndex = ImageIndex;
			}
			List<TreeNodeLoader> loaders = GetLoaderNodes();
			if (loaders != null && loaders.Count > 0)
			{
				this.Nodes.Clear();
				foreach (TreeNodeLoader l in loaders)
				{
					this.Nodes.Add(l);
				}
				this.NextLevelLoaded = false;
				this.Collapse();
			}
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loaders = base.GetLoaderNodes();
			loaders.Add(new PropertyLoader(false));
			return loaders;
		}
	}
	#endregion
	#region Custom Properties
	public class TreeNodeCustomPropertyPointerCollection : TreeNodeObjectCollection
	{
		public TreeNodeCustomPropertyPointerCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scopeMethodId)
		{
			Text = "Properties";
			this.Nodes.Add(new CLoader(this.IsStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, UInt32 scopeMethodId, bool sameLevel)
		{
			if (act.ScopeMethodId != 0)
			{
				if (act.ScopeMethodId != scopeMethodId)
				{
					return false;
				}
			}
			IObjectIdentity startPointer = null;
			IPropertySetter ps = act.ActionMethod as IPropertySetter;
			if (ps != null)
			{
				startPointer = ps.SetProperty;
			}
			else
			{
				return false;
			}
			if (startPointer != null)
			{
				if (sameLevel)
				{
					//collection is not showing the same level actions
				}
				else
				{
					IObjectIdentity lowerLevel = startPointer;
					ICustomPointer cp = lowerLevel as ICustomPointer;
					if (cp != null)
					{
						ClassInstancePointer inst = startPointer.IdentityOwner as ClassInstancePointer;
						if (inst != null)
						{
							startPointer = inst;
						}
						else
						{
							startPointer = cp.Holder;
						}
					}
					else
					{
						startPointer = startPointer.IdentityOwner;
					}
					while (startPointer != null)
					{
						if (lowerLevel.PointerType == this.NodeType)//same P, M, or E pointer type
						{
							if (startPointer.IsSameObjectRef(this.OwnerIdentity)) //same owner
							{
								return true;
							}
						}
						//move up one level
						lowerLevel = startPointer;
						cp = lowerLevel as ICustomPointer;
						if (cp != null)
						{
							startPointer = cp.Holder;
						}
						else
						{
							startPointer = startPointer.IdentityOwner;
						}
					}
				}
			}
			return false;
		}
		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SC_PROPERTIES;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_C_PROPERTIES;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SCA_PROPERTIES;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_CA_PROPERTIES;
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
		public TreeNodeCustomPropertyPointer GetPropertyPointerNode(UInt32 id)
		{
			for (int i = 0; i < Nodes.Count; i++)
			{
				TreeNodeCustomPropertyPointer tcp = Nodes[i] as TreeNodeCustomPropertyPointer;
				if (tcp != null)
				{
					if (tcp.PropertyPointer.MemberId == id)
					{
						return tcp;
					}
				}
			}
			return null;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader(IsStatic));
			return lst;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				List<PropertyClass> props = null;
				ICustomPropertyHolder customHolder = parentNode.GetCustomPropertyHolder();
				if (customHolder != null)
				{
					bool isWebPage = false;
					TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
					Dictionary<UInt32, IAction> actions = null;
					if (topClass != null)
					{
						if (typeof(WebPage).IsAssignableFrom(topClass.ClassData.RootClassID.BaseClassType))
						{
							isWebPage = true;
						}
						if (!topClass.StaticScope)
						{
							actions = topClass.GetActions();
						}
					}
					else
					{
						TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
						if (rootType != null)
						{
							actions = rootType.GetActions();
						}
					}
					props = customHolder.GetCustomProperties();
					if (props != null)
					{
						SortedList<string, TreeNodeCustomPropertyPointer> list = new SortedList<string, TreeNodeCustomPropertyPointer>();
						IClass holder = customHolder.Holder;
						foreach (PropertyClass p in props)
						{
							if (isWebPage && p.RunAt == EnumWebRunAt.Server)
							{
								continue;
							}
							if (ForStatic == p.IsStatic)
							{
								CustomPropertyPointer cpp = new CustomPropertyPointer(p, holder);
								TreeNodeCustomPropertyPointer nodeProperty = new TreeNodeCustomPropertyPointer(ForStatic, cpp);
								if (!p.IsOverride)
								{
									list.Add(nodeProperty.Text, nodeProperty);

									if (p.CanWrite)
									{
										//load actions
										bool bHasActions = false;
										if (actions != null)
										{
											foreach (IAction a in actions.Values)
											{
												if (a != null && a.IsStatic == parentNode.IsStatic || a.ObjectDevelopType != EnumObjectDevelopType.Custom)
												{
													SetterPointer sp = a.ActionMethod as SetterPointer;
													if (sp != null)
													{
														ICustomObject pp = sp.SetProperty as ICustomObject;
														if (pp != null && pp.WholeId == p.WholeId)
														{
															bHasActions = true;
															break;
														}
													}
													else
													{
														IObjectIdentity oi = a.ActionMethod.IdentityOwner;
														while (oi != null)
														{
															IProperty prop = oi as IProperty;
															if (prop != null)
															{
																if (prop.IsCustomProperty)
																{
																	if (prop.IsSameObjectRef(p))
																	{
																		bHasActions = true;
																		break;
																	}
																}
															}
															oi = oi.IdentityOwner;
														}
													}
												}
											}
											if (bHasActions)
											{
												nodeProperty.OnShowActionIcon();
											}
										}
									}
								}
							}
						}
						IEnumerator<KeyValuePair<string, TreeNodeCustomPropertyPointer>> ie = list.GetEnumerator();
						while (ie.MoveNext())
						{
							parentNode.Nodes.Add(ie.Current.Value);
						}
					}
				}
			}
		}
	}
	/// <summary>
	/// show custom property definitions
	/// </summary>
	public class TreeNodeCustomPropertyCollection : TreeNodeObjectCollection
	{
		public TreeNodeCustomPropertyCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, bool isForStatic, IObjectPointer objectPointer, UInt32 scoeMethodId)
			: base(tv, parentNode, isForStatic, objectPointer, scoeMethodId)
		{
			Text = "Properties";
			this.Nodes.Add(new CLoader(isForStatic));
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}

		protected override void ShowIconNoAction()
		{
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SC_PROPERTIES;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_C_PROPERTIES;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SCA_PROPERTIES;
			else
				this.ImageIndex = TreeViewObjectExplorer.IMG_CA_PROPERTIES;
			SelectedImageIndex = this.ImageIndex;
		}
		public override TreeNodeObject FindObjectNode(IObjectIdentity pointer)
		{
			SetterPointer sp = pointer as SetterPointer;
			if (sp != null)
			{
				LoadNextLevel();
				for (int i = 0; i < this.Nodes.Count; i++)
				{
					TreeNodeObject ret = this.Nodes[i] as TreeNodeObject;
					if (ret != null)
					{
						if (sp.SetProperty.IsSameObjectRef(ret.OwnerPointer))
						{
							return ret;
						}
					}
				}
			}
			TreeNodeObject node = base.FindObjectNode(pointer);
			if (node != null)
			{
				return node;
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is PropertyClass);
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
				if (tv != null)
				{
					ClassPointer r = tv.RootId;
					if (r != null)
					{
						List<MenuItem> ms = new List<MenuItem>();
						if (r.IsWebPage || r.IsWebApp)
						{
							ms.Add(new MenuItemWithBitmap("Create new web client property", newWebClientProperty_Click, Resources._custPropClientNew.ToBitmap()));
							ms.Add(new MenuItemWithBitmap("Create new web server property", newWebServerProperty_Click, Resources._custPropServerNew.ToBitmap()));
						}
						else
						{
							ms.Add(new MenuItemWithBitmap("Create new property", newProperty_Click, Resources._newCustProp.ToBitmap()));
						}
						MenuItem[] mi = new MenuItem[ms.Count];
						ms.CopyTo(mi, 0);
						return mi;
					}
				}
			}
			return null;
		}
		public TreeNodeCustomProperty GetCustomPropertyNode(UInt32 propertyId)
		{
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeCustomProperty cm = this.Nodes[i] as TreeNodeCustomProperty;
				if (cm != null)
				{
					if (cm.Property.MemberId == propertyId)
					{
						return cm;
					}
				}
			}
			return null;
		}
		public void LocatePropertyNode(int propertyId)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			tv.ForceLoadNextLevel(this);
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeCustomProperty cm = this.Nodes[i] as TreeNodeCustomProperty;
				if (cm != null)
				{
					if (cm.Property.MemberId == propertyId)
					{
						tv.SelectedNode = cm;
						break;
					}
				}
			}
		}
		private void newWebClientProperty_Click(object sender, EventArgs e)
		{
			createProperty(EnumWebRunAt.Client);
		}
		private void newWebServerProperty_Click(object sender, EventArgs e)
		{
			createProperty(EnumWebRunAt.Server);
		}
		private void newProperty_Click(object sender, EventArgs e)
		{
			createProperty(EnumWebRunAt.Inherit);
		}
		private void createProperty(EnumWebRunAt runAt)
		{
			TreeNodeClassRoot root = this.TopLevelRootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				ClassPointer rootClass = root.ClassData.RootClassID;
				PropertyClass prop = rootClass.CreateNewProperty(
					new DataTypePointer(new TypePointer(typeof(string))),
					false,
					root.ViewersHolder.Loader.CreateNewComponentName("Property", null),
					false,
					true,
					true,
					this.IsStatic, runAt);
				//
				this.LoadNextLevel();
				for (int i = 0; i < Nodes.Count; i++)
				{
					TreeNodeCustomProperty cp = Nodes[i] as TreeNodeCustomProperty;
					if (cp != null)
					{
						if (cp.Property.MemberId == prop.MemberId)
						{
							this.TreeView.SelectedNode = cp;
							break;
						}
					}
				}
			}
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader(this.IsStatic));
			return lst;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader(bool forStatic)
				: base(forStatic)
			{
			}
			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				UInt32 scopeId = parentNode.ScopeMethodId;
				List<PropertyClass> props = null;
				ICustomPropertyHolder customHolder = null;
				TreeNode tn = parentNode.Parent;
				while (tn != null)
				{
					customHolder = tn as ICustomPropertyHolder;
					if (customHolder != null)
					{
						break;
					}
					tn = tn.Parent;
				}
				if (customHolder != null)
				{
					TreeNodeClassRoot topClass = parentNode.TopLevelRootClassNode;
					ClassPointer root = topClass.RootObjectId;
					Dictionary<UInt32, IAction> actions = null;
					if (topClass != null)
					{
						if (!topClass.StaticScope)
						{
							actions = topClass.GetActions();
						}
					}
					else
					{
						TreeNodeClassType rootType = parentNode.TopLevelNode as TreeNodeClassType;
						if (rootType != null)
						{
							actions = rootType.GetActions();
						}
					}
					SortedList<string, TreeNodeCustomProperty> tcms = new SortedList<string, TreeNodeCustomProperty>();
					SortedList<string, TreeNodeCustomProperty> tcps = new SortedList<string, TreeNodeCustomProperty>();
					props = customHolder.GetCustomProperties();
					if (props != null)
					{
						foreach (PropertyClass p in props)
						{
							if (root != null)
							{
								if (root.IsWebPage || root.IsWebApp)
								{
									if (p is PropertyClassInherited)
									{
										continue;
									}
								}
							}
							if (ForStatic == p.IsStatic)
							{
								TreeNodeCustomProperty nodeProperty = new TreeNodeCustomProperty(tv, ForStatic, p, scopeId);
								if (p.Implemented)
								{
									tcms.Add(nodeProperty.Text, nodeProperty);
								}
								else
								{
									tcps.Add(nodeProperty.Text, nodeProperty);
								}
							}
						}
						IEnumerator<KeyValuePair<string, TreeNodeCustomProperty>> ie;
						ie = tcms.GetEnumerator();
						while (ie.MoveNext())
						{
							TreeNodeCustomProperty nodeProperty = ie.Current.Value;
							parentNode.Nodes.Add(nodeProperty);
							if (actions != null)
							{
								foreach (IAction a in actions.Values)
								{
									if (a != null && nodeProperty.IncludeAction(a, tv, scopeId, false))
									{
										nodeProperty.OnShowActionIcon();
										break;
									}
								}
							}
						}
						//
						ie = tcps.GetEnumerator();
						while (ie.MoveNext())
						{
							TreeNodeCustomProperty nodeProperty = ie.Current.Value;
							parentNode.Nodes.Add(nodeProperty);
							if (actions != null)
							{
								foreach (IAction a in actions.Values)
								{
									if (a != null && nodeProperty.IncludeAction(a, tv, scopeId, false))
									{
										nodeProperty.OnShowActionIcon();
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
	/// <summary>
	/// a pointer to a custom property for an instance of a ClassPointer
	/// </summary>
	public class TreeNodeCustomPropertyPointer : TreeNodeObject
	{
		public TreeNodeCustomPropertyPointer(bool isForStatic, CustomPropertyPointer property)
			: base(isForStatic, property)
		{
			ImageIndex = TreeViewObjectExplorer.GetPropertyImageIndex(property.Property);
			SelectedImageIndex = ImageIndex;
			//
			this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
			this.Nodes.Add(new ActionLoader(isForStatic));
			//
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					if (!this.PropertyPointer.Property.CanWrite)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.Action)
				{
					if (this.PropertyPointer.Property.CanWrite)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.Method)
				{
					if (this.PropertyPointer.Property.CanWrite)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				return (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					);
			}
			return false;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			if (this.IsStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_SCA_PROPERTY;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_CA_PROPERTY;
			SelectedImageIndex = ImageIndex;
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				if (MainClassPointer != null)
				{
					MenuItem[] mi;
					mi = new MenuItem[1];
					mi[0] = new MenuItemWithBitmap("Create set property action", OnCreateNewAction, Resources._custPropAction.ToBitmap());
					return mi;
				}
			}
			return null;
		}
		public override IAction CreateNewAction()
		{
			CustomPropertyPointer cpp = PropertyPointer;
			ActionClass act = new ActionClass(MainClassPointer);
			act.ActionMethod = cpp.CreateSetterMethodPointer(act);
			act.ActionName = act.ActionMethod.DefaultActionName;// cpp.Holder.CodeName + ".Set" + cpp.Name;
			act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			return act;
		}
		[Browsable(false)]
		public CustomPropertyPointer PropertyPointer
		{
			get
			{
				return (CustomPropertyPointer)this.OwnerPointer;
			}
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new TreeNodeMemberLoader(this.IsStatic));
			lst.Add(new ActionLoader(this.IsStatic));
			return lst;
		}
		public override void ShowText()
		{
			Text = this.OwnerPointer.DisplayName;
		}
		public void SetPropertyClass(PropertyClass p)
		{
			PropertyPointer.SetProperty(p);
			ShowText();
		}
	}
	/// <summary>
	/// a definition of a custom property for a ClassPointer
	/// </summary>
	public class TreeNodeCustomProperty : TreeNodeObject
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectPointer">method owner</param>
		/// <param name="target"></param>
		/// <param name="scope"></param>
		/// <param name="eventName"></param>
		public TreeNodeCustomProperty(TreeViewObjectExplorer tv, bool isForStatic, PropertyClass property, UInt32 scopeMethodId)
			: base(isForStatic, property)
		{
			Text = property.ToString();
			ImageIndex = TreeViewObjectExplorer.GetPropertyImageIndex(property);
			SelectedImageIndex = ImageIndex;
			//
			if (this.SelectionTarget == EnumObjectSelectType.All)
			{
				Nodes.Add(new TreeNodeAttributeCollection(tv, this, (IAttributeHolder)this.OwnerPointer, scopeMethodId));
			}
			this.Nodes.Add(new TreeNodeMemberLoader(isForStatic));
			this.Nodes.Add(new ActionLoader(isForStatic));
			//

		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(OwnerPointer);
			loader.DesignPane.PaneHolder.OnPropertySelected(Property);
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Custom; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}
		public override void ShowText()
		{
			Text = this.OwnerPointer.ToString();
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			PropertyClass property = this.OwnerPointer as PropertyClass;
			ImageIndex = TreeViewObjectExplorer.GetActPropertyImageIndex(property);
			SelectedImageIndex = this.ImageIndex;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return (pointer is PropertyClass);
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> l = new List<TreeNodeLoader>();
			l.Add(new TreeNodeMemberLoader(IsStatic));
			l.Add(new ActionLoader(IsStatic));
			return l;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectLValue)
				{
					if (!this.Property.CanWrite)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.Action)
				{
					if (this.Property.CanWrite)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					)
				{
					return true;
				}
				if (tv.SelectionType == EnumObjectSelectType.Method)
				{
					PropertyClass pc = this.OwnerPointer as PropertyClass;
					if (pc != null && pc.CanWrite)
					{
						return true;
					}
				}
			}
			return false;
		}
		public PropertyClass Property
		{
			get
			{
				return (PropertyClass)this.OwnerPointer;
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				PropertyClass pc = Property;
				List<MenuItem> list = new List<MenuItem>();
				MenuItem mi;
				if (MainClassPointer != null && this.SelectionTarget != EnumObjectSelectType.Method)
				{
					mi = new MenuItemWithBitmap("Create set property action", OnCreateNewAction, Resources._custPropAction.ToBitmap());
					list.Add(mi);
				}
				if (!DoNotRemove)
				{
					list.Add(new MenuItem("-"));
					if (pc.IsOverride)
					{
						if (pc.Implemented)
						{
							mi = new MenuItemWithBitmap("Remove Override", mnu_remove, Resources._cancel.ToBitmap());
							if (!pc.HasBaseImplementation)
							{
								mi.Enabled = false;
							}
						}
						else
						{
							mi = new MenuItemWithBitmap("Override", mnu_implement, Resources._overrideProperty.ToBitmap());
						}
					}
					else
					{
						mi = new MenuItemWithBitmap("Delete property", deleteProperty_Click, Resources._cancel.ToBitmap());
					}
					list.Add(mi);
				}
				if (list.Count > 0)
				{
					MenuItem[] menus = new MenuItem[list.Count];
					list.CopyTo(menus);
					return menus;
				}
			}
			return null;
		}
		public override IAction CreateNewAction()
		{
			return MainClassPointer.CreateSetPropertyAction(Property);
		}
		void mnu_remove(object sender, EventArgs e)
		{
			PropertyOverride po = Property as PropertyOverride;
			if (po == null)
			{
				throw new DesignerException("Overriden property is not found. [{0}]", Property);
			}
			if (MessageBox.Show("Do you want to remove the override of this property?", "Property", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
			{
				TreeNode pn = this.Parent;
				ClassPointer r = po.Holder as ClassPointer;
				r.DeleteProperty(po);
				pn.Expand();
				for (int i = 0; i < pn.Nodes.Count; i++)
				{
					TreeNodeCustomProperty tnc = pn.Nodes[i] as TreeNodeCustomProperty;
					if (tnc != null)
					{
						if (tnc.Property.MemberId == po.MemberId)
						{
							pn.TreeView.SelectedNode = tnc;
							tnc.EnsureVisible();
							break;
						}
					}
				}
			}
		}
		void mnu_implement(object sender, EventArgs e)
		{
			PropertyClassInherited pc = Property as PropertyClassInherited;
			if (pc != null)
			{
				TreeNode pn = this.Parent;
				ClassPointer r = pc.Holder as ClassPointer;
				PropertyOverride po = r.CreateOverrideProperty(pc);
				pn.Expand();
				for (int i = 0; i < pn.Nodes.Count; i++)
				{
					TreeNodeCustomProperty tnc = pn.Nodes[i] as TreeNodeCustomProperty;
					if (tnc != null)
					{
						if (tnc.Property.MemberId == po.MemberId)
						{
							pn.TreeView.SelectedNode = tnc;
							tnc.EnsureVisible();
							break;
						}
					}
				}
			}
		}
		void deleteProperty_Click(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode root = this.RootClassNode;
			if (root != null && root.ViewersHolder != null)
			{
				//check property usage
				List<ObjectTextID> usages = DesignUtil.GetPropertyUsage(tv.Project, this.Property);
				List<ObjectTextID> usages2 = root.ClassData.RootClassID.GetPropertyUsages(Property);
				usages.AddRange(usages2);
				if (usages.Count > 0)
				{
					dlgObjectUsage dlg = new dlgObjectUsage();
					dlg.LoadData("Cannot delete this property. It is being used by the following objects", string.Format("Property - {0}", this.Property.Name), usages);
					dlg.ShowDialog(tv.FindForm());
				}
				else
				{
					if (MessageBox.Show("Do you want to delete this property?", "Property", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
					{
						root.ClassData.RootClassID.DeleteProperty(this.Property);
					}
				}
			}
		}
	}
	#endregion
	public class TreeNodeLocalVariable : TreeNodeObject, ICustomPropertyHolder
	{
		#region fields and constructors
		public TreeNodeLocalVariable(TreeViewObjectExplorer tv, LocalVariable var)
			: base(false, var)
		{
			int n = TreeViewObjectExplorer.GetTypeIcon(var.ObjectType); // TreeViewObjectExplorer.AddBitmap(var.IconImage);
			ImageIndex = n;
			SelectedImageIndex = n;
			MethodClass mc = var.Owner as MethodClass;
			this.Nodes.Add(new TreeNodePMELocalVariable(tv, var, mc.MethodID));
		}
		#endregion
		#region Properties
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Custom; }
		}
		public LocalVariable OwnerVariable
		{
			get
			{
				return (LocalVariable)(this.OwnerPointer);
			}
		}
		#endregion
		#region Methods
		public override IAction CreateNewAction()
		{
			LocalVariable lv = OwnerVariable;
			MethodClass mc = lv.Owner as MethodClass;
			if (mc != null && mc.CurrentEditor != null)
			{
				ActionAssignInstance act = mc.CurrentEditor.CreateSetValueAction(lv);
				return act;
			}
			return null;
		}
		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		public override bool IsTargeted()
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			if (tv != null)
			{
				if (tv.SelectionType == EnumObjectSelectType.All
					|| tv.SelectionType == EnumObjectSelectType.Object
					|| tv.SelectionType == EnumObjectSelectType.Action
					)
				{
					return true;
				}
			}
			return false;
		}
		#endregion

		#region ICustomPropertyHolder Members

		public List<PropertyClass> GetCustomProperties()
		{
			List<PropertyClass> ps = new List<PropertyClass>();
			LocalVariable var = OwnerVariable;
			ClassPointer pointer = var.VariableCustomType;
			if (pointer != null)
			{
				Dictionary<string, PropertyClass> cps = pointer.CustomProperties;
				if (cps != null)
				{
					foreach (PropertyClass p in cps.Values)
					{
						ps.Add(p);
					}
				}
			}
			return ps;
		}

		public List<MethodClass> GetCustomMethods()
		{
			List<MethodClass> ms = new List<MethodClass>();
			ClassPointer pointer = OwnerVariable.VariableCustomType;
			if (pointer != null)
			{
				Dictionary<string, MethodClass> cms = pointer.CustomMethods;
				if (cms != null)
				{
					foreach (MethodClass m in cms.Values)
					{
						ms.Add(m);
					}
				}
			}
			return ms;
		}

		public List<EventClass> GetCustomEvents()
		{
			List<EventClass> es = new List<EventClass>();
			ClassPointer pointer = OwnerVariable.VariableCustomType;
			if (pointer != null)
			{
				Dictionary<string, EventClass> ces = pointer.CustomEvents;
				if (ces != null)
				{
					foreach (EventClass e in ces.Values)
					{
						es.Add(e);
					}
				}
			}
			return es;
		}

		public uint ClassId
		{
			get { return OwnerVariable.ClassId; }
		}

		public uint MemberId
		{
			get { return OwnerVariable.MemberId; }
		}
		public IClass Holder
		{
			get
			{
				return OwnerVariable;
			}
		}
		#endregion
	}
	public class TreeNodeConstructorCollection : TreeNodeObjectCollection
	{
		public TreeNodeConstructorCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, IObjectPointer objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, false, objectPointer, scopeMethodId)
		{
			Text = "Constructors";
			this.Nodes.Add(new CLoader());
		}
		[Browsable(false)]
		public override EnumObjectDevelopType ObjectDevelopType { get { return EnumObjectDevelopType.Both; } }
		[Browsable(false)]
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}
		protected override bool OnCheckIncludeAction(IAction act)
		{
			ConstructorPointer cp = act.ActionMethod as ConstructorPointer;
			if (cp != null)
			{
				return true;
			}
			return false;
		}
		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader());
			return lst;
		}
		public override bool CanContain(IObjectIdentity pointer)
		{
			return true;
		}
		protected override void ShowIconNoAction()
		{
			ImageIndex = TreeViewObjectExplorer.IMG_Constructors;
			SelectedImageIndex = ImageIndex;
		}
		public override void OnShowActionIcon()
		{
			base.OnShowActionIcon();
			this.ImageIndex = TreeViewObjectExplorer.IMG_Constructors;
			SelectedImageIndex = this.ImageIndex;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				ClassPointer cp = parentNode.OwnerPointer as ClassPointer;
				SortedList<string, ConstructorClass> sortedBaseList = cp.GetBaseConstructorsSortedByParameterCount();
				if (sortedBaseList != null)
				{
					SortedList<int, TreeNodeBaseConstructor> nodeList = new SortedList<int, TreeNodeBaseConstructor>();
					List<ConstructorClass> list = cp.GetConstructors();
					for (int i = sortedBaseList.Count - 1; i >= 0; i--)
					{
						ConstructorClass baseConstructor = sortedBaseList.ElementAt<KeyValuePair<string, ConstructorClass>>(i).Value;
						TreeNodeBaseConstructor node = new TreeNodeBaseConstructor(false, baseConstructor);
						nodeList.Add(i, node);
						List<ConstructorClass> list2 = new List<ConstructorClass>();
						foreach (ConstructorClass x in list)
						{
							if (x.IsDerivedFrom(baseConstructor))
							{
								TreeNodeConstructor tnc = new TreeNodeConstructor(tv, false, x);
								node.Nodes.Add(tnc);
							}
							else
							{
								list2.Add(x);
							}
						}
						list = list2;
					}
					for (int i = 0; i < nodeList.Count; i++)
					{
						parentNode.Nodes.Add(nodeList[i]);
					}
				}
			}
		}
	}
	/// <summary>
	/// objectPointer is one of the base constructors.
	/// if the base is a lib type then the base constructors are the lib constructors
	/// </summary>
	public class TreeNodeBaseConstructor : TreeNodeObject
	{
		private Dictionary<string, string> _parameterToolTips;
		public TreeNodeBaseConstructor(bool isForStatic, ConstructorClass objectPointer)
			: base(isForStatic, objectPointer)
		{
			if (isForStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_METHOD;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_Constructor1;
			SelectedImageIndex = ImageIndex;
			for (int i = 0; i < objectPointer.ParameterCount; i++)
			{
				TreeNodeCustomMethodParameter cmp = new TreeNodeCustomMethodParameter(false, objectPointer.Parameters[i]);
				Nodes.Add(cmp);
			}
		}
		public ConstructorClass Constructor
		{
			get
			{
				return (ConstructorClass)this.OwnerPointer;
			}
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Method; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Both; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return true;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return new List<TreeNodeLoader>();
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (readOnly)
			{
				return null;
			}
			else
			{
				MenuItem[] mi = new MenuItem[1];
				mi[0] = new MenuItemWithBitmap("Create new constructor", newConstructor, Resources._newConstructor.ToBitmap());
				return mi;
			}
		}
		protected void CreateParameterToolTips()
		{
			ConstructorClass methodPointer = Constructor;
			_parameterToolTips = methodPointer.GetParameterTooltips();
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				TreeNodeMethodParameter tmp = this.Nodes[i] as TreeNodeMethodParameter;
				if (tmp != null)
				{
					MethodParamPointer mpp = tmp.OwnerPointer as MethodParamPointer;
					if (_parameterToolTips.ContainsKey(mpp.MemberName))
					{
						tmp.SetToolTips(_parameterToolTips[mpp.MemberName]);
					}
				}
				else
				{
					TreeNodeCustomMethodParameter cmp = this.Nodes[i] as TreeNodeCustomMethodParameter;
					if (cmp != null)
					{
						if (_parameterToolTips.ContainsKey(cmp.Parameter.Name))
						{
							cmp.SetToolTips(_parameterToolTips[cmp.Parameter.Name]);
						}
					}
				}
			}
		}
		public Dictionary<string, string> ParameterTooltips
		{
			get
			{
				return _parameterToolTips;
			}
		}
		public override string Tooltips
		{
			get
			{
				CreateParameterToolTips();
				return string.Format("A constructor is a method to create a new instance of [{0}]", Constructor.CodeName);
			}
		}
		private void newConstructor(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				ClassPointer root = this.RootObjectId;
				ConstructorClass cc = (ConstructorClass)this.OwnerPointer;
				ConstructorClass c2 = new ConstructorClass(root);
				c2.Owner = root;
				if (cc.ParameterCount > 0)
				{
					List<ParameterClass> ps = new List<ParameterClass>();
					for (int i = 0; i < cc.ParameterCount; i++)
					{
						ParameterClass p = (ParameterClass)cc.Parameters[i].Clone();
						p.Owner = c2;
						ps.Add(p);
					}
					c2.Parameters = ps;
					c2.FixedParameters = ps.Count;
				}
				c2.MemberId = 0;//indicate a new method
				if (c2.Edit(0, tv.Parent.RectangleToScreen(this.Bounds), r.ViewersHolder.Loader, tv.FindForm()))
				{
					TreeNodeConstructor tnc = new TreeNodeConstructor(tv, c2.IsStatic, c2);
					this.Nodes.Add(tnc);
				}
			}
		}
	}
	public class TreeNodeConstructor : TreeNodeBaseConstructor
	{
		public TreeNodeConstructor(TreeViewObjectExplorer tv, bool isForStatic, ConstructorClass objectPointer)
			: base(isForStatic, objectPointer)
		{
			if (isForStatic)
				ImageIndex = TreeViewObjectExplorer.IMG_S_METHOD;
			else
				ImageIndex = TreeViewObjectExplorer.IMG_Constructor;
			SelectedImageIndex = ImageIndex;
			Nodes.Add(new TreeNodeActionCollectionForMethod(tv, this, isForStatic, objectPointer, objectPointer.MemberId));
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (readOnly)
			{
				return null;
			}
			else
			{
				MenuItem[] mi = new MenuItem[2];
				mi[0] = new MenuItemWithBitmap("Edit", edit, Resources._constructor1.ToBitmap());
				mi[1] = new MenuItemWithBitmap("Remove", remove, Resources._cancel.ToBitmap());
				return mi;
			}
		}
		private void remove(object sender, EventArgs e)
		{
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				if (MessageBox.Show(this.TreeView.FindForm(), "Do you want to remove this constructor?", "Constructor", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					ConstructorClass constructor = this.Constructor;
					r.ClassData.RootClassID.DeleteMethod(constructor);
						this.Remove();
				}
			}
		}
		private void edit(object sender, EventArgs e)
		{
			ConstructorClass constructor = this.Constructor;
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			IRootNode r = this.RootClassNode;
			if (r != null && r.ViewersHolder != null)
			{
				if (constructor.Edit(0, tv.Parent.RectangleToScreen(this.Bounds), r.ViewersHolder.Loader, tv.FindForm()))
				{
					this.Text = constructor.DisplayName;
					//reload children
					Nodes.Clear();
					for (int i = 0; i < constructor.ParameterCount; i++)
					{
						TreeNodeCustomMethodParameter cmp = new TreeNodeCustomMethodParameter(false, constructor.Parameters[i]);
						Nodes.Add(cmp);
					}
					CreateParameterToolTips();
				}
			}
		}
	}
	public class TreeNodeAttributeCollection : TreeNodeObjectCollection
	{
		public TreeNodeAttributeCollection(TreeViewObjectExplorer tv, TreeNodeObject parentNode, IAttributeHolder objectPointer, UInt32 scopeMethodId)
			: base(tv, parentNode, false, objectPointer, scopeMethodId)
		{
			Text = "Attributes";
			ImageIndex = TreeViewObjectExplorer.IMG_Attributes;
			SelectedImageIndex = ImageIndex;
			this.Nodes.Add(new CLoad());
		}
		protected override bool CheckActions
		{
			get
			{
				return false;
			}
		}
		public IAttributeHolder AttributeHolder
		{
			get
			{
				return (IAttributeHolder)OwnerIdentity;
			}
		}
		class CLoad : TreeNodeLoader
		{
			public CLoad()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				TreeNodeAttributeCollection ac = parentNode as TreeNodeAttributeCollection;
				IAttributeHolder ah = ac.AttributeHolder;
				List<ConstObjectPointer> list = ah.GetCustomAttributeList();
				if (list != null && list.Count > 0)
				{
					foreach (ConstObjectPointer cop in list)
					{
						TreeNodeConstObject nc = new TreeNodeConstObject(cop);
						ac.Nodes.Add(nc);
					}
				}
			}
		}
		private void addAttribute(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			DataTypePointer tp = DesignUtil.SelectDataType(tv.Project, tv.RootId, null, EnumObjectSelectType.InstanceType, null, new DataTypePointer(new TypePointer(typeof(Attribute))), null, tv.FindForm());
			if (tp != null)
			{
				IAttributeHolder ah = this.OwnerIdentity as IAttributeHolder;
				List<ConstObjectPointer> list = ah.GetCustomAttributeList();
				if (list != null && list.Count > 0)
				{
					foreach (ConstObjectPointer op in list)
					{
						if (tp.BaseClassType.IsAssignableFrom(op.BaseClassType))
						{
							MessageBox.Show(tv.FindForm(), "The attribute already applied", "Add attribute", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							return;
						}
					}
				}
				FormActionParameters dlg = new FormActionParameters();
				ConstObjectPointer cop = new ConstObjectPointer(tp);
				cop.Name = "Attribute";
				if (dlg.LoadObject(cop))
				{
					dlg.DisableBack();
					if (dlg.ShowDialog(tv.FindForm()) == DialogResult.OK)
					{
						ah.AddAttribute(cop);
						this.LoadNextLevel();
						TreeNodeConstObject nc = null;
						for (int i = 0; i < this.Nodes.Count; i++)
						{
							TreeNodeConstObject nc0 = this.Nodes[i] as TreeNodeConstObject;
							if (nc0 != null)
							{
								if (cop.IsSameObjectRef(nc0.OwnerPointer))
								{
									nc = nc0;
									break;
								}
							}
						}
						if (nc == null)
						{
							nc = new TreeNodeConstObject(cop);
							this.Nodes.Add(nc);
						}
						tv.SelectedNode = nc;
						tv.NotifyChanges();
						if (typeof(WebMethodAttribute).IsAssignableFrom(cop.BaseClassType))
						{
							TreeNodeObject tno = this.Parent as TreeNodeObject;
							while (tno != null)
							{
								TreeNodeCustomMethod tcm = tno as TreeNodeCustomMethod;
								if (tcm != null)
								{
									tcm.ResetImage();
									break;
								}
								tno = tno.Parent as TreeNodeObject;
							}
						}
					}
				}
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				MenuItem[] mi;
				mi = new MenuItem[1];
				mi[0] = new MenuItemWithBitmap("Add attribute", addAttribute, Resources._newAttribute.ToBitmap());
				return mi;
			}
			return null;
		}
		protected override void ShowIconNoAction()
		{

		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Attribute; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Both; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return true;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> loader = new List<TreeNodeLoader>();
			loader.Add(new CLoad());
			return loader;
		}
	}
	/// <summary>
	/// it is used for Attributes
	/// </summary>
	public class TreeNodeConstObject : TreeNodeObject
	{
		public TreeNodeConstObject(ConstObjectPointer obj)
			: base(false, obj)
		{
			Text = obj.ToString();
			ImageIndex = TreeViewObjectExplorer.GetTypeIcon(obj.BaseClassType);
			SelectedImageIndex = ImageIndex;
		}
		public override void OnNodeSelection(ILimnorDesignerLoader loader)
		{
			loader.NotifySelection(Value.NamedValues);
		}
		public ConstObjectPointer Value
		{
			get
			{
				return (ConstObjectPointer)(OwnerPointer);
			}
		}
		private void edit(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			ConstObjectPointer cop = Value;
			FormActionParameters dlg = new FormActionParameters();
			if (dlg.LoadObject(cop))
			{
				dlg.DisableBack();
				if (dlg.ShowDialog(tv.FindForm()) == DialogResult.OK)
				{
					this.Text = cop.ToString();
					TreeNodeObject pn = this.Parent as TreeNodeObject;
					if (pn != null)
					{
						IAttributeHolder hc = pn.OwnerIdentity as IAttributeHolder;
						if (hc != null)
						{
							hc.AddAttribute(cop);
							tv.NotifyChanges();
						}
					}
				}
			}
		}
		private void remove(object sender, EventArgs e)
		{
			TreeViewObjectExplorer tv = this.TreeView as TreeViewObjectExplorer;
			ConstObjectPointer cop = Value;
			if (MessageBox.Show(tv.FindForm(), "Do you want to remove this attribute?", "Attribute", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				TreeNodeObject pn = this.Parent as TreeNodeObject;
				if (pn != null)
				{
					IAttributeHolder hc = pn.OwnerIdentity as IAttributeHolder;
					if (hc != null)
					{
						hc.RemoveAttribute(cop);
						if (typeof(WebMethodAttribute).IsAssignableFrom(cop.BaseClassType))
						{
							TreeNodeObject tno = pn;
							while (tno != null)
							{
								TreeNodeCustomMethod tcm = tno as TreeNodeCustomMethod;
								if (tcm != null)
								{
									tcm.ResetImage();
									break;
								}
								tno = tno.Parent as TreeNodeObject;
							}
						}
						this.Remove();
						tv.NotifyChanges();
					}
				}
			}
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			if (!readOnly)
			{
				List<MenuItem> mi = new List<MenuItem>();
				mi.Add(new MenuItemWithBitmap("Edit", edit, Resources._obj.ToBitmap()));
				bool bCanRemove = true;
				if (typeof(WebServiceAttribute).IsAssignableFrom(Value.BaseClassType))
				{
					TreeNodeObject parentNode = this.Parent as TreeNodeObject;
					if (parentNode != null)
					{
						ClassPointer root = parentNode.OwnerPointer as ClassPointer;
						if (root != null && root.IsWebService)
						{
							bCanRemove = false;
						}
					}
				}
				if (bCanRemove)
				{
					mi.Add(new MenuItemWithBitmap("Remove", remove, Resources._cancel.ToBitmap()));
				}
				return mi.ToArray();
			}
			return null;
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Attribute; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Both; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
	}
	public class TreeNodeActionInput : TreeNodeObject
	{
		public TreeNodeActionInput()
			: base(new ActionInput())
		{
			Text = "Action Input";
			ImageIndex = TreeViewObjectExplorer.IMG_ActRet;
			SelectedImageIndex = ImageIndex;
		}

		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Property; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Custom; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}
		public override bool IsTargeted()
		{
			return true;
		}
	}
	public class TreeNodeAddEventHandler : TreeNodeObject
	{
		#region fields and constructors
		public TreeNodeAddEventHandler(IEvent eventToHandle)
			: base(eventToHandle)
		{
			Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
				NameDisplayForm, eventToHandle.DisplayName);
			ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD;
			SelectedImageIndex = ImageIndex;
			List<ParameterClass> parameters = eventToHandle.GetParameters(eventToHandle.CreateHandlerMethod(null));
			if (parameters != null && parameters.Count > 0)
			{
				foreach (ParameterClass p in parameters)
				{
					CustomMethodParameterPointer cmp = new CustomMethodParameterPointer(p);
					TreeNodeCustomMethodParameter tmp = new TreeNodeCustomMethodParameter(false, cmp);
					Nodes.Add(tmp);
				}
			}
		}
		#endregion
		#region protected properties
		protected virtual string NameDisplayForm
		{
			get
			{
				return "New event handler method for {0}";
			}
		}
		#endregion
		#region overrides
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Both; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			return null;
		}

		public override bool IsTargeted()
		{
			return true;
		}
		#endregion
	}
	public abstract class TreeNodeAddWebClientEventHandler : TreeNodeAddEventHandler
	{
		public TreeNodeAddWebClientEventHandler(IEvent eventToHandle)
			: base(eventToHandle)
		{
		}
		public override bool IsTargeted()
		{
			if (MethodEditContext.UseServerExecuterOnly)
				return false;
			return true;
		}
		public abstract Type ActionType { get; }
	}
	public class TreeNodeAddWebClientEventHandlerClientActs : TreeNodeAddWebClientEventHandler
	{
		#region fields and constructors
		public TreeNodeAddWebClientEventHandlerClientActs(IEvent eventToHandle)
			: base(eventToHandle)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD_CLIENT;
			SelectedImageIndex = ImageIndex;
		}
		#endregion
		#region protected properties
		protected override string NameDisplayForm
		{
			get
			{
				return "New client handler actions for {0}";
			}
		}
		#endregion
		public override Type ActionType { get { return typeof(WebClientEventHandlerMethodClientActions); } }
	}
	public class TreeNodeAddWebClientEventHandlerServerActs : TreeNodeAddWebClientEventHandler
	{
		#region fields and constructors
		public TreeNodeAddWebClientEventHandlerServerActs(IEvent eventToHandle)
			: base(eventToHandle)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD_SERVER;
			SelectedImageIndex = ImageIndex;
		}
		#endregion
		#region protected properties
		protected override string NameDisplayForm
		{
			get
			{
				return "New server handler actions for {0}";
			}
		}
		#endregion
		public override bool IsTargeted()
		{
			return true;
		}
		public override Type ActionType { get { return typeof(WebClientEventHandlerMethodServerActions); } }
	}
	public class TreeNodeAddWebClientEventHandlerDownloadActs : TreeNodeAddWebClientEventHandler
	{
		#region fields and constructors
		public TreeNodeAddWebClientEventHandlerDownloadActs(IEvent eventToHandle)
			: base(eventToHandle)
		{
			ImageIndex = TreeViewObjectExplorer.IMG_EVENT_METHOD_DOWNLOAD;
			SelectedImageIndex = ImageIndex;
		}
		#endregion
		#region protected properties
		protected override string NameDisplayForm
		{
			get
			{
				return "New client and download handler actions for {0}";
			}
		}
		#endregion
		public override Type ActionType { get { return typeof(WebClientEventHandlerMethodDownloadActions); } }
	}
	/// <summary>
	/// put it at the end of a TreeView to make other nodes visible
	/// </summary>
	public class TreeNodeDummy : TreeNode
	{
		public TreeNodeDummy()
		{
			Text = "";
			ImageIndex = -1;
			SelectedImageIndex = -1;
		}
	}
	/// <summary>
	/// document list
	/// </summary>
	public class TreeNodeDocCollection : TreeNodeObjectCollection
	{
		private Dictionary<string, FormRTF> _forms;
		public TreeNodeDocCollection(TreeViewObjectExplorer tv, ClassPointer objectPointer)
			:base( tv,  null,  true,  objectPointer,  0)
		{
			Text = "Documents";
			ImageIndex = TreeViewObjectExplorer.IMG_FILES;
			SelectedImageIndex = ImageIndex;
			this.Nodes.Add(new CLoader());
			_forms = new Dictionary<string, FormRTF>();
		}
		public void CloseAllDocs()
		{
			if (_forms.Count > 0)
			{
				FormRTF[] fs = new FormRTF[_forms.Count];
				int i = 0;
				foreach (KeyValuePair<string, FormRTF> kv in _forms)
				{
					fs[i++] = kv.Value;
				}
				for (i = 0; i < fs.Length; i++)
				{
					fs[i].Close();
				}
			}
		}
		public void OpenDocument(string file)
		{
			string key = file.ToLowerInvariant();
			FormRTF f;
			if (_forms.TryGetValue(key, out f))
			{
				f.BringToFront();
			}
			else
			{
				f = new FormRTF();
				f.FormClosing += f_FormClosing;
				string path = getFullPath(file);
				f.LoadData(key, path);
				f.Show(this.TreeView.FindForm());
				_forms.Add(key, f);
			}
		}

		void f_FormClosing(object sender, FormClosingEventArgs e)
		{
			FormRTF f = sender as FormRTF;
			if (f != null)
			{
				if (_forms.ContainsKey(f.Key))
				{
					f.AskSave();
					_forms.Remove(f.Key);
				}
			}
		}
		public bool DeleteDocument(string file)
		{
			if (MessageBox.Show(this.TreeView.FindForm(), string.Format(CultureInfo.InvariantCulture, "Do you want to remove the following document?\r\n{0}", file), "Remove document", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				try
				{
					string key = file.ToLowerInvariant();
					FormRTF f;
					if (_forms.TryGetValue(key, out f))
					{
						f.Close();
					}
					ClassPointer cp = this.OwnerPointer as ClassPointer;
					cp.DeleteDocumentFile(file);
					string path = getFullPath(file);
					if (File.Exists(path))
					{
						if (MessageBox.Show(this.TreeView.FindForm(), string.Format(CultureInfo.InvariantCulture, "The document is removed from the class. Do you also want to delete the following file from the computer?\r\n{0}", path), "Remove document", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						{
							File.Delete(path);
						}
					}
					return true;
				}
				catch (Exception e)
				{
					MessageBox.Show(this.TreeView.FindForm(), e.Message, "Remove document", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			return false;
		}
		public string RenameDocument(string file)
		{
			try
			{
				DlgNewName dlg = new DlgNewName();
				dlg.LoadData(checkNewName);
				if (dlg.ShowDialog(this.TreeView.FindForm()) == DialogResult.OK)
				{
					string path = getFullPath(file);
					string pathNew = getFullPath(dlg.NewName);
					if (File.Exists(path))
					{
						File.Move(path, pathNew);
					}
					ClassPointer cp = this.OwnerPointer as ClassPointer;
					cp.RenameDocumentFile(file, dlg.NewName);
					string key = file.ToLowerInvariant();
					FormRTF f;
					if (_forms.TryGetValue(key, out f))
					{
						f.SetFile(getFullPath(dlg.NewName));
					}
					return dlg.NewName;
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(this.TreeView.FindForm(), e.Message, "Rename document", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			return null;
		}
		private string getFullPath(string file)
		{
			ClassPointer cp = this.OwnerPointer as ClassPointer;
			return string.Format(CultureInfo.InvariantCulture, "{0}.rtf", Path.Combine(cp.Project.ProjectFolder, file));
		}
		private string checkNewName(string newName)
		{
			if (string.IsNullOrEmpty(newName))
				return "Name is empty";
			if (newName.Contains('.') || newName.Contains('?') || newName.Contains('/')
				|| newName.Contains('\\') || newName.Contains(':') || newName.Contains('*'))
			{
				return "The name should not contain following characters: /?.:*\\";
			}
			string path = getFullPath(newName);
			if (File.Exists(path))
			{
				return string.Format(CultureInfo.InvariantCulture, "File exists: [{0}]", path);
			}
			return null;
		}
		private void mi_new(object sender, EventArgs e)
		{
			DlgNewName dlg = new DlgNewName();
			dlg.LoadData(checkNewName);
			if (dlg.ShowDialog(this.TreeView.FindForm()) == DialogResult.OK)
			{
				ClassPointer cp = this.OwnerPointer as ClassPointer;
				cp.AddDocumentFile(dlg.NewName);
				TreeNodeClassDocument tc = new TreeNodeClassDocument(dlg.NewName);
				this.Nodes.Add(tc);
				this.TreeView.SelectedNode = tc;
				OpenDocument(dlg.NewName);
			}
		}
		protected override void ShowIconNoAction()
		{
			
		}
		public override bool HasAction(UInt32 scopeMethodId, TreeViewObjectExplorer tv, TreeNodeObject parentNode)
		{
			return false;
		}
		public override bool IncludeAction(IAction act, TreeViewObjectExplorer tv, uint scopeMethodId, bool sameLevel)
		{
			return false;
		}
		public override TreeNodeAction FindActionNode(IAction act)
		{
			return null;
		}
		protected override bool OnCheckIncludeAction(IAction act)
		{
			return false;
		}
		public override void OnShowActionIcon()
		{
			
		}
		public override MenuItem[] GetContextMenuItems(bool readOnly)
		{
			MenuItem[] mis = new MenuItem[1];
			mis[0] = new MenuItemWithBitmap("New Document", mi_new, Resources._new);
			return mis;
		}
		public override EnumPointerType NodeType
		{
			get { return EnumPointerType.Unknown; }
		}

		public override EnumObjectDevelopType ObjectDevelopType
		{
			get { return EnumObjectDevelopType.Custom; }
		}

		public override bool CanContain(IObjectIdentity objectPointer)
		{
			return false;
		}

		public override List<TreeNodeLoader> GetLoaderNodes()
		{
			List<TreeNodeLoader> lst = new List<TreeNodeLoader>();
			lst.Add(new CLoader());
			return lst;
		}
		class CLoader : TreeNodeLoader
		{
			public CLoader()
				: base(false)
			{
			}

			public override void LoadNextLevel(TreeViewObjectExplorer tv, TreeNodeObject parentNode)
			{
				ClassPointer cp = parentNode.OwnerPointer as ClassPointer;
				StringCollection fileList = cp.GetDocumentFiles();
				foreach (string file in fileList)
				{
					TreeNodeClassDocument dn = new TreeNodeClassDocument(file);
					parentNode.Nodes.Add(dn);
				}
			}
		}
	}
	class TreeNodeClassDocument : TreeNode, ITreeNodeObject
	{
		private string _file;
		public TreeNodeClassDocument(string file)
		{
			_file = file;
			this.Text = file;
			this.ImageIndex = TreeViewObjectExplorer.IMG_FILE;
			this.SelectedImageIndex = this.ImageIndex;
		}

		public MenuItem[] GetContextMenuItems(bool bReadOnly)
		{
			MenuItem[] mis = new MenuItem[3];
			mis[0] = new MenuItemWithBitmap("Open", mi_open, Resources._action.ToBitmap());
			mis[1] = new MenuItemWithBitmap("Rename", mi_rename, Resources._rename.ToBitmap());
			mis[2] = new MenuItemWithBitmap("Delete", mi_delete, Resources._cancel.ToBitmap());
			return mis;
		}
		private void mi_open(object sender, EventArgs e)
		{
			TreeNodeDocCollection tnc = this.Parent as TreeNodeDocCollection;
			if (tnc != null)
			{
				tnc.OpenDocument(_file);
			}
		}
		private void mi_delete(object sender, EventArgs e)
		{
			TreeNodeDocCollection tnc = this.Parent as TreeNodeDocCollection;
			if (tnc != null)
			{
				if (tnc.DeleteDocument(_file))
				{
					tnc.Nodes.Remove(this);
				}
			}
		}
		private void mi_rename(object sender, EventArgs e)
		{
			TreeNodeDocCollection tnc = this.Parent as TreeNodeDocCollection;
			if (tnc != null)
			{
				string newName = tnc.RenameDocument(_file);
				if (!string.IsNullOrEmpty(newName))
				{
					_file = newName;
					this.Text = _file;
				}
			}
		}
		public bool IsInDesign
		{
			get { return true; }
		}
	}
}
