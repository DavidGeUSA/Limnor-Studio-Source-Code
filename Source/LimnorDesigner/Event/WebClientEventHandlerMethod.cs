/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using LimnorDesigner.Action;
using System.Collections.Specialized;
using LimnorDesigner.MethodBuilder;
using VPL;

namespace LimnorDesigner.Event
{
	/// <summary>
	/// handler method for events of web client components
	/// </summary>
	public abstract class WebClientEventHandlerMethod : EventHandlerMethod
	{
		#region fields and constructors
		public WebClientEventHandlerMethod(ClassPointer owner)
			: base(owner)
		{
		}
		public WebClientEventHandlerMethod(ClassPointer owner, IEvent eventOwner)
			: base(owner, eventOwner)
		{
		}
		public WebClientEventHandlerMethod(HandlerMethodID taskId)
			: base(taskId)
		{
		}
		#endregion
		#region Properties
		[ReadOnly(true)]
		[Browsable(false)]
		public override EnumMethodWebUsage WebUsage
		{
			get
			{
				return EnumMethodWebUsage.Client;
			}
			set
			{
			}
		}
		public override EnumWebRunAt RunAt
		{
			get
			{
				return EnumWebRunAt.Client;
			}
		}
		[Browsable(false)]
		public override bool NoReturn
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		public override ParameterClass ReturnValue
		{
			get
			{
				if (this.Event != null && string.CompareOrdinal("onbeforeunload", this.Event.Name) == 0)
				{
					if (!typeof(string).Equals(base.ReturnValue.BaseClassType))
					{
						base.ReturnValue.SetDataType(typeof(string));
					}
				}
				else if (!typeof(bool).Equals(base.ReturnValue.BaseClassType))
				{
					base.ReturnValue.SetDataType(typeof(bool));
				}
				return base.ReturnValue;
			}
			set
			{
				if (value != null)
				{
					if (this.Event != null && string.CompareOrdinal("onbeforeunload", this.Event.Name) == 0)
					{
						if (!typeof(string).Equals(value.BaseClassType))
						{
							value.SetDataType(typeof(string));
						}
					}
					else if (!typeof(bool).Equals(value.BaseClassType))
					{
						value.SetDataType(typeof(bool));
					}
					base.ReturnValue = value;
				}
			}
		}
		#endregion
		protected override void createJavascriptFunction(StringCollection jsCode, StringCollection methodcode, JsMethodCompiler jmc, bool bRet)
		{
		}
	}
	/// <summary>
	/// client executers not allowing server properties
	/// </summary>
	public class WebClientEventHandlerMethodClientActions : WebClientEventHandlerMethod
	{
		#region fields and constructors
		public WebClientEventHandlerMethodClientActions(ClassPointer owner)
			: base(owner)
		{
		}
		public WebClientEventHandlerMethodClientActions(ClassPointer owner, IEvent eventOwner)
			: base(owner, eventOwner)
		{
		}
		public WebClientEventHandlerMethodClientActions(HandlerMethodID taskId)
			: base(taskId)
		{
		}
		#endregion
		#region Properties
		[Browsable(false)]
		public override bool UseClientPropertyOnly { get { return true; } }
		#endregion
	}
	/// <summary>
	/// server executers
	/// </summary>
	public class WebClientEventHandlerMethodServerActions : WebClientEventHandlerMethod
	{
		#region fields and constructors
		public WebClientEventHandlerMethodServerActions(ClassPointer owner)
			: base(owner)
		{
		}
		public WebClientEventHandlerMethodServerActions(ClassPointer owner, IEvent eventOwner)
			: base(owner, eventOwner)
		{
		}
		public WebClientEventHandlerMethodServerActions(HandlerMethodID taskId)
			: base(taskId)
		{
		}
		#endregion
		#region Properties
		[ReadOnly(true)]
		[Browsable(false)]
		public override EnumMethodWebUsage WebUsage
		{
			get
			{
				return EnumMethodWebUsage.Server;
			}
			set
			{
			}
		}
		public override EnumWebRunAt RunAt
		{
			get
			{
				return EnumWebRunAt.Server;
			}
		}
		[Browsable(false)]
		public override ParameterClass ReturnValue
		{
			get
			{
				ParameterClass r = new ParameterClass(new TypePointer(typeof(void)), this);
				r.Name = "value";
				return r;
			}
			set
			{
			}
		}
		#endregion
	}
	/// <summary>
	/// client executers allowing server values
	/// </summary>
	public class WebClientEventHandlerMethodDownloadActions : WebClientEventHandlerMethod
	{
		#region fields and constructors
		public WebClientEventHandlerMethodDownloadActions(ClassPointer owner)
			: base(owner)
		{
		}
		public WebClientEventHandlerMethodDownloadActions(ClassPointer owner, IEvent eventOwner)
			: base(owner, eventOwner)
		{
		}
		public WebClientEventHandlerMethodDownloadActions(HandlerMethodID taskId)
			: base(taskId)
		{
		}
		#endregion
		#region Properties

		#endregion
	}
}
