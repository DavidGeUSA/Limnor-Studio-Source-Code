﻿/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Web Project Support
 * License: GNU General Public License v3.0
 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using System.Drawing;
using VPL;
using System.Collections.Specialized;
using LFilePath;
using System.IO;

namespace Limnor.WebBuilder
{
	class MarkerCompiler : IObjectCompiler, IJavascriptEventHolder
	{
		public MarkerCompiler()
		{
		}

		#region IObjectCompiler Members

		public void CreateActionJavaScript(string codeName, string methodName, StringCollection sb, StringCollection parameters, string returnReceiver)
		{
			if (string.CompareOrdinal(methodName, "ShowInformationWindow") == 0)
			{
				sb.Add("limnorgooglemaps.showInfoWindow(");
				sb.Add(codeName);
				sb.Add(",");
				sb.Add(parameters[0]);
				sb.Add(",");
				sb.Add(parameters[1]);
				sb.Add(");\r\n");
			}
			else if (string.CompareOrdinal(methodName, "Show") == 0)
			{
				sb.Add(codeName);
				sb.Add(".setMap(");
				sb.Add(codeName);
				sb.Add(".gmap.map);\r\n");
			}
			else if (string.CompareOrdinal(methodName, "Hide") == 0)
			{
				sb.Add(codeName);
				sb.Add(".setMap(null);\r\n");
			}
		}

		#endregion

		#region IJavascriptEventHolder Members

		public void AttachJsEvent(string codeName, string eventName, string handlerName, StringCollection jsCode)
		{
			if (string.CompareOrdinal(eventName, "onclick") == 0)
			{
				jsCode.Add("limnorgooglemaps.setMarkerEvent(");
				jsCode.Add(codeName);
				jsCode.Add(",'click',");
				jsCode.Add(handlerName);
				jsCode.Add(");\r\n");
			}
		}

		#endregion
	}

	[WebClientMember]
	[ObjectCompiler(typeof(MarkerCompiler))]
	[UseParentObject]
	public class GoogleMapsMarker : IJavaScriptEventOwner, ISupportWebClientMethods, IValueUIEditorOwner, IJavascriptVariable
	{
		#region fields and constructors
		private HtmlGoogleMap _owner;
		private string _name;
		private List<WebResourceFile> _resourceFiles;
		public GoogleMapsMarker(HtmlGoogleMap owner)
		{
			_owner = owner;
			Location = new GoogleMapsLatLng();
			markerSize = Size.Empty;
		}
		#endregion
		#region Events
		[Description("Occurs when this marker is clicked on")]
		[WebClientMember]
		public event GoogleMapsMarkerEvent onclick { add { } remove { } }
		#endregion
		#region Properties
		[NotForProgramming]
		public GoogleMapsLatLng Location { get; set; }

		[WebClientMember]
		public float latitude { get { return Location.lat(); } }

		[WebClientMember]
		public float longitude { get { return Location.lng(); } }

		[WebClientMember]
		[ReadOnlyInProgramming]
		public string title { get; set; }

		private string _uuid;
		[Description("Gets a string uniquely identifying a marker. If a marker has a place object then the place has the same uuid of the marker.")]
		[Browsable(false)]
		[WebClientMember]
		[ReadOnlyInProgramming]
		public string uuid
		{
			get
			{
				if (string.IsNullOrEmpty(_uuid))
				{
					_uuid = string.Format(CultureInfo.InvariantCulture, "marker_{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				}
				return _uuid;
			}
			set
			{
				_uuid = value;
			}
		}

		[FilePath("Image files|*.png;*.gif;*.jpg;*.bmp", "Select icon image file for marker")]
		[Editor(typeof(PropEditorFilePath), typeof(UITypeEditor))]
		public string iconUrl { get; set; }

		[Description("Gets and sets the size of the marker")]
		public Size markerSize
		{
			get;
			set;
		}
		[Description("Gets and sets marker color if iconUrl is not used.")]
		public Color markerColor
		{
			get;
			set;
		}
		[WebClientMember]
		[ReadOnlyInProgramming]
		public string name
		{
			get
			{
				if (string.IsNullOrEmpty(_name))
				{
					_name = string.Format(CultureInfo.InvariantCulture, "marker{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				}
				return _name;
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					if (!_owner.IsMarkerNameInUse(value))
					{
						_name = value;
					}
				}
			}
		}

		[Description("Gets a place object if this marker represents a place. Such a marker is among markers generated by a place search action.")]
		[WebClientMember]
		public GoogleMapsPlace place
		{
			get { return null; }
		}
		#endregion
		#region Methods
		public override string ToString()
		{
			return name;
		}
		public string GetVarName()
		{
			return string.Format(CultureInfo.InvariantCulture, "limnorgooglemaps.getMarker('{0}','{1}')", _owner.VarName, this.uuid);
		}
		[WebClientMember]
		public void ShowInformationWindow(string infoHtml, int maximumWidth)
		{
		}
		[WebClientMember]
		public void Show()
		{
		}
		[WebClientMember]
		public void Hide()
		{
		}

		[Browsable(false)]
		[NotForProgramming]
		public IList<WebResourceFile> GetWebResourceFiles()
		{
			return _resourceFiles;
		}
		[Browsable(false)]
		[NotForProgramming]
		public void OnWebPageLoaded(StringCollection sc)
		{
			sc.Add("limnorgooglemaps.addMarker('");
			sc.Add(_owner.VarName);
			sc.Add("',");
			sc.Add(this.Location.lat().ToString(CultureInfo.InvariantCulture));
			sc.Add(",");
			sc.Add(this.Location.lng().ToString(CultureInfo.InvariantCulture));
			sc.Add(",");
			if (string.IsNullOrEmpty(this.name))
			{
				sc.Add("null,");
			}
			else
			{
				sc.Add("'");
				sc.Add(this.name.Replace("'", "\\'"));
				sc.Add("',");
			}
			if (string.IsNullOrEmpty(this.title))
			{
				sc.Add("null,");
			}
			else
			{
				sc.Add("'");
				sc.Add(this.title.Replace("'", "\\'"));
				sc.Add("',");
			}
			sc.Add("'");
			sc.Add(this.uuid);
			sc.Add("',");
			if (string.IsNullOrEmpty(this.iconUrl))
			{
				sc.Add("null");
			}
			else
			{
				string iconAddr = this.iconUrl;
				try
				{
					if (File.Exists(this.iconUrl))
					{
						if (_resourceFiles == null)
						{
							_resourceFiles = new List<WebResourceFile>();
						}
						bool b;
						WebResourceFile wrf = new WebResourceFile(iconUrl, WebResourceFile.WEBFOLDER_Images, out b);
						_resourceFiles.Add(wrf);
						iconAddr = wrf.WebAddress;
					}
				}
				catch
				{
				}
				sc.Add("'");
				sc.Add(iconAddr);
				sc.Add("'");
			}
			if (this.markerSize != Size.Empty && this.markerSize.Width > 0 && this.markerSize.Height > 0)
			{
				sc.Add(",");
				sc.Add(this.markerSize.Width.ToString(CultureInfo.InvariantCulture));
				sc.Add(",");
				sc.Add(this.markerSize.Height.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				sc.Add(",32,32");
			}
			sc.Add(",");
			if (this.markerColor == Color.Empty)
			{
				sc.Add("'FFFF00'");
			}
			else
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture, "'{0}'", VPLUtil.GetColorHexString2(this.markerColor)));
			}
			sc.Add(");\r\n");
		}
		[Browsable(false)]
		[NotForProgramming]
		public void OnWebPageLoadedAfterEventHandlerCreations(StringCollection jsCode)
		{
			if (_eventHandlers != null)
			{
				foreach (KeyValuePair<string, string> kv in _eventHandlers)
				{
					AttachJsEvent(null, kv.Key, kv.Value, jsCode);
				}
			}
		}
		#endregion
		#region IJavaScriptEventOwner Members
		private Dictionary<string, string> _eventHandlersDynamic;
		private Dictionary<string, string> _eventHandlers;
		[Browsable(false)]
		[NotForProgramming]
		public void LinkJsEvent(string codeName, string eventName, string handlerName, StringCollection jsCode, bool isDynamic)
		{
			if (isDynamic)
			{
				if (_eventHandlersDynamic == null)
				{
					_eventHandlersDynamic = new Dictionary<string, string>();
				}
				_eventHandlersDynamic.Add(eventName, handlerName);
			}
			else
			{
				if (_eventHandlers == null)
				{
					_eventHandlers = new Dictionary<string, string>();
				}
				_eventHandlers.Add(eventName, handlerName);
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void AttachJsEvent(string codeName, string eventName, string handlerName, StringCollection jsCode)
		{
			if (_compiler == null)
			{
				_compiler = new MarkerCompiler();
			}
			string cn;
			if (string.IsNullOrEmpty(codeName))
			{
				cn = GetVarName();
			}
			else
			{
				cn = codeName;
			}
			_compiler.AttachJsEvent(cn, eventName, handlerName, jsCode);

		}
		#endregion

		#region ISupportWebClientMethods Members
		private MarkerCompiler _compiler;
		[Browsable(false)]
		[NotForProgramming]
		public void CreateActionJavaScript(string codeName, string methodName, StringCollection sb, StringCollection parameters, string returnReceiver)
		{
			string cn;
			if (string.IsNullOrEmpty(codeName))
			{
				cn = this.GetVarName();
			}
			else
			{
				if (string.CompareOrdinal(codeName, uuid) == 0)
				{
					cn = this.GetVarName();
				}
				else
				{
					cn = codeName;
				}
			}
			if (_compiler == null)
			{
				_compiler = new MarkerCompiler();
			}
			_compiler.CreateActionJavaScript(cn, methodName, sb, parameters, returnReceiver);
		}

		#endregion

		#region IValueUIEditorOwner Members
		[Browsable(false)]
		[NotForProgramming]
		public EditorAttribute GetValueUIEditor(string valueName)
		{
			if (string.CompareOrdinal(valueName, "infoHtml") == 0)
			{
				return new EditorAttribute(typeof(TypeEditorHtmlContents), typeof(UITypeEditor));
			}
			return null;
		}

		#endregion
	}
	public class GoogleMapsMarkerCollection : List<GoogleMapsMarker>, ICustomTypeDescriptor
	{
		private HtmlGoogleMap _owner;
		public GoogleMapsMarkerCollection(HtmlGoogleMap owner)
		{
			_owner = owner;
		}
		public HtmlGoogleMap Owner
		{
			get
			{
				return _owner;
			}
		}
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "Count:{0}", this.Count);
		}
		#region ICustomTypeDescriptor Members
		[Browsable(false)]
		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}
		[Browsable(false)]
		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}
		[Browsable(false)]
		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}
		[Browsable(false)]
		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}
		[Browsable(false)]
		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}
		[Browsable(false)]
		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}
		[Browsable(false)]
		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}
		[Browsable(false)]
		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}
		[Browsable(false)]
		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}
		[Browsable(false)]
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			List<PropertyDescriptor> lst = new List<PropertyDescriptor>();
			lst.Add(new PropertyDescriptorNewMapMarker(this));
			foreach (GoogleMapsMarker mm in this)
			{
				lst.Add(new PropertyDescriptorMapMarker(mm));
			}
			return new PropertyDescriptorCollection(lst.ToArray());
		}
		[Browsable(false)]
		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}
		[Browsable(false)]
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion

		#region class PropertyDescriptorMapMarker:PropertyDescriptor
		class PropertyDescriptorMapMarker : PropertyDescriptor
		{
			private GoogleMapsMarker _marker;
			public PropertyDescriptorMapMarker(GoogleMapsMarker owner)
				: base(owner.uuid,
				new Attribute[]{
                    new TypeConverterAttribute(typeof(ExpandableObjectConverter)),
                    new EditorAttribute(typeof(TypeEditorDeleteMapMarker), typeof(UITypeEditor)), 
                    new RefreshPropertiesAttribute(RefreshProperties.All)
                })
			{
				_marker = owner;
			}

			public override bool CanResetValue(object component)
			{
				return false;
			}

			public override Type ComponentType
			{
				get { return typeof(GoogleMapsMarkerCollection); }
			}

			public override object GetValue(object component)
			{
				return _marker;
			}

			public override bool IsReadOnly
			{
				get { return true; }
			}

			public override Type PropertyType
			{
				get { return typeof(GoogleMapsMarker); }
			}

			public override void ResetValue(object component)
			{

			}

			public override void SetValue(object component, object value)
			{

			}

			public override bool ShouldSerializeValue(object component)
			{
				return false;
			}
		}
		#endregion
	}
	#region class PropertyDescriptorNewMapMarker : PropertyDescriptor
	class PropertyDescriptorNewMapMarker : PropertyDescriptor
	{
		private GoogleMapsMarkerCollection _owner;
		public PropertyDescriptorNewMapMarker(GoogleMapsMarkerCollection owner)
			: base("New Marker", new Attribute[] { 
                new EditorAttribute(typeof(TypeEditorNewMapMarker), typeof(UITypeEditor)), 
                new RefreshPropertiesAttribute(RefreshProperties.All) })
		{
			_owner = owner;
		}

		public override bool CanResetValue(object component)
		{
			return false;
		}

		public override Type ComponentType
		{
			get { return typeof(GoogleMapsMarkerCollection); }
		}

		public override object GetValue(object component)
		{
			return "Create a new map marker";
		}

		public override bool IsReadOnly
		{
			get { return true; }
		}

		public override Type PropertyType
		{
			get { return typeof(string); }
		}

		public override void ResetValue(object component)
		{

		}

		public override void SetValue(object component, object value)
		{

		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}
	#endregion
	class TypeEditorNewMapMarker : UITypeEditor
	{
		public TypeEditorNewMapMarker()
		{
		}
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.Modal;
		}
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			if (context != null && context.Instance != null && provider != null)
			{
				IWindowsFormsEditorService service = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (service != null)
				{
					GoogleMapsMarkerCollection mc = context.Instance as GoogleMapsMarkerCollection;
					if (mc != null)
					{
						mc.Add(new GoogleMapsMarker(mc.Owner));
						value = null;
					}
					else
					{
						HtmlGoogleMap map = context.Instance as HtmlGoogleMap;
						if (map != null)
						{
							map.GoogleMapsMarkers.Add(new GoogleMapsMarker(map));
						}
					}
				}
			}
			return value;
		}

	}
	class TypeEditorDeleteMapMarker : UITypeEditor
	{
		public TypeEditorDeleteMapMarker()
		{
		}
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			if (context != null && context.Instance != null && provider != null)
			{
				IWindowsFormsEditorService service = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (service != null)
				{
					GoogleMapsMarkerCollection mc = context.Instance as GoogleMapsMarkerCollection;
					if (mc != null)
					{
						GoogleMapsMarker marker = value as GoogleMapsMarker;
						if (marker != null)
						{
							service.CloseDropDown();
							CommandSelectControl isc = new CommandSelectControl(service);
							service.DropDownControl(isc);
							if (isc.SelectedCommand != null)
							{
								if (isc.SelectedCommand.CommandId == EnumCommand.Delete)
								{
									mc.Remove(marker);
								}
								else if (isc.SelectedCommand.CommandId == EnumCommand.New)
								{
									mc.Add(new GoogleMapsMarker(mc.Owner));
								}
								value = marker;
							}
						}
					}
				}
			}
			return value;
		}

	}
	enum EnumCommand { Delete, New };
	class Command
	{
		public Command(Bitmap img, string name, EnumCommand cmd)
		{
			Image = img;
			Name = name;
			CommandId = cmd;
		}
		public EnumCommand CommandId { get; set; }
		public Bitmap Image { get; set; }
		public string Name { get; set; }
	}
	class CommandSelectControl : UserControl
	{
		private ImageListControl _list;
		public IWindowsFormsEditorService EdSvc;
		public Command SelectedCommand;
		public CommandSelectControl(IWindowsFormsEditorService edSvc)
		{
			EdSvc = edSvc;
			_list = new ImageListControl();
			_list.Dock = DockStyle.Fill;
			_list.Items.Add(new Command(Resource1._cancel.ToBitmap(), "Delete Map Marker", EnumCommand.Delete));
			_list.Items.Add(new Command(Resource1._newIcon.ToBitmap(), "New Map Marker", EnumCommand.New));
			this.Controls.Add(_list);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		}


		class ImageListControl : ListBox
		{
			Pen pen;
			public ImageListControl()
			{
				pen = new Pen(Brushes.Blue, 3);
				this.DrawMode = DrawMode.OwnerDrawFixed;
				this.ItemHeight = 48;
			}
			protected override void OnDrawItem(DrawItemEventArgs e)
			{
				e.DrawBackground();
				if (e.Index >= 0)
				{
					Command iid = this.Items[e.Index] as Command;
					e.Graphics.DrawImage(iid.Image, e.Bounds);
					e.Graphics.DrawString(iid.Name, this.Font, Brushes.Black, (float)iid.Image.Width, (float)2);
					if ((e.State & DrawItemState.Selected) != 0)
					{
						e.DrawFocusRectangle();
					}
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				this.SelectedIndex = this.IndexFromPoint(e.X, e.Y);
			}
			protected override void OnMouseDown(MouseEventArgs e)
			{
				this.SelectedIndex = this.IndexFromPoint(e.X, e.Y);
				if (this.SelectedIndex >= 0)
					((CommandSelectControl)Parent).SelectedCommand = this.Items[this.SelectedIndex] as Command;
				else
					((CommandSelectControl)Parent).SelectedCommand = null;
				((CommandSelectControl)Parent).EdSvc.CloseDropDown();
			}
		}
	}
	class TypeEditorMapMarker : UITypeEditor
	{
		public TypeEditorMapMarker()
		{
		}
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.Modal;
		}
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			if (context != null && context.Instance != null && provider != null)
			{
				IWindowsFormsEditorService service = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (service != null)
				{
					HtmlGoogleMap map = context.Instance as HtmlGoogleMap;
					if (map != null)
					{
						DialogMapMarkers dlg = new DialogMapMarkers();
						dlg.LoadData(map);
						if (service.ShowDialog(dlg) == DialogResult.OK)
						{
							value = dlg.Result;
						}
					}
				}
			}
			return value;
		}

	}

	public delegate void GoogleMapsMarkerEvent(GoogleMapsMarker sender);
}
