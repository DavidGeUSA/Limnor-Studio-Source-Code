/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using MathExp;
using System.Drawing;
using System.Windows.Forms;
using XmlSerializer;
using System.Xml;
using XmlUtility;
using LimnorDesigner.MenuUtil;
using TraceLog;
using System.ComponentModel;
using VPL;
using System.Collections.Specialized;
using System.Reflection;
using VSPrj;
using LimnorDesigner.Property;
using ProgElements;
using LimnorDesigner.Action;
using LimnorDesigner.EventMap;
using LimnorDesigner.Event;
using System.Globalization;
using WindowsUtility;

namespace LimnorDesigner.MethodBuilder
{
	public class UseLabelAttribute : Attribute
	{
		private bool _useLable;
		public UseLabelAttribute()
		{
		}
		public UseLabelAttribute(bool useLabel)
		{
			_useLable = useLabel;
		}
		public bool UseLabel
		{
			get
			{
				return _useLable;
			}
		}
	}
	/// <summary>
	/// use an icon to represent a programming entity
	/// </summary>
	public abstract class ComponentIcon : ActiveDrawing, IXmlNodeSerializable, IControlWithID, ICustomTypeDescriptor, IWithProject, ISerializerProcessor
	{
		#region fields and constructors
		private Color C_SelectedByMouse = Color.Yellow;
		private Color C_Selected = Color.Cyan;
		private IClass _componentPointer; //the object pointer this icon represents
		private Image _img;
		private UInt32 _classId;
		private UInt32 _memberId;
		private bool _selected;

		private ILimnorDesigner _designer;
		private DrawingLabel _label;
		private StringCollection propertyNames; //properties to be displayed

		public const int ICONSIZE = 20;
		public ComponentIcon()
		{
			this.Size = new Size(ICONSIZE, ICONSIZE);
			this.Cursor = System.Windows.Forms.Cursors.Hand;

			if (UseLabel)
			{
				_label = new DrawingLabel(this);
				_label.RelativePosition.IsXto0 = true;
				_label.Cursor = Cursors.Hand;
			}
			_img = Resources._void;
			propertyNames = new StringCollection();
			propertyNames.Add("Name");
		}
		protected virtual List<Control> GetRelatedControls()
		{
			List<Control> lst = new List<Control>();
			lst.Add(this);
			lst.Add(_label);
			return lst;
		}
		protected virtual void OnInit(ILimnorDesigner designer, IClass pointer)
		{
		}
		public void Init(ILimnorDesigner designer, IClass pointer)
		{
			_designer = designer;
			_componentPointer = pointer;
			_classId = _componentPointer.ClassId;
			_memberId = _componentPointer.MemberId;
			OnSetImage();
			if (UseLabel && (IsForComponent || IsForMethodParameter))
			{
				RefreshLabelText();
			}
			OnInit(designer, pointer);
		}
		#endregion
		#region event handlers
		protected virtual void OnCreateContextMenu(ContextMenu mnu, Point location)
		{
		}
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (UseLabel)
			{
				if (this.Parent != null)
				{
					if (_label.Parent != this.Parent)
					{
						this.Parent.Controls.Add(_label);
					}
				}
				else
				{
					if (_label.Parent != null)
					{
						_label.Parent.Controls.Remove(_label);
					}
				}
			}
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (IsSelectedByMouseMove)
			{
				e.Graphics.FillRectangle(Brushes.Yellow, 0, 0, _img.Width + 4, _img.Height + 4);
			}
			else if (IsSelected)
			{
				e.Graphics.FillRectangle(Brushes.Cyan, 0, 0, _img.Width + 4, _img.Height + 4);
			}
			e.Graphics.DrawImage(_img, 2, 2);
		}
		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			refreshLabel();
		}
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			refreshLabel();
		}
		public override void OnRelativeDrawingMouseEnter(RelativeDrawing relDraw)
		{
			refreshLabel();
		}
		public override void OnRelativeDrawingMouseLeave(RelativeDrawing relDraw)
		{
			refreshLabel();
		}
		private RelativeDrawing _mouseDownSender;
		public override void OnRelativeDrawingMouseDown(RelativeDrawing relDraw, MouseEventArgs e)
		{
			if (!(relDraw is EventIcon))
			{
				_mouseDownSender = relDraw;
				try
				{
					OnMouseDown(e);
				}
				finally
				{
					_mouseDownSender = null;
				}
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			//
			IIconHolder mv = IconHolder;
			if (mv != null)
			{
				mv.ClearIconSelection();
				mv.SetIconSelection(this);

			}
			this.IsSelected = true;
			this.Invalidate();
			//

			if (e.Button == MouseButtons.Right)
			{
				ContextMenu mnu = new ContextMenu();

				//
				OnCreateContextMenu(mnu, e.Location);
				//
				if (mnu.MenuItems.Count > 0)
				{
					if (_mouseDownSender != null)
					{
						mnu.Show(_mouseDownSender, e.Location);
					}
					else
					{
						mnu.Show(this, e.Location);
					}
				}
			}
		}
		private void refreshLabel()
		{
			if (_label != null && _label.Parent != null)
			{
				_label.IsSelected = IsSelectedByMouseMove;
				if (IsSelectedByMouseMove)
				{
					_label.BackColor = C_SelectedByMouse;
				}
				else if (IsSelected)
				{
					_label.BackColor = C_Selected;
				}
				else
				{
					_label.BackColor = _label.Parent.BackColor;
				}
				_label.Refresh();
			}
			this.Refresh();
		}
		#endregion
		#region Properties
		/// <summary>
		/// for root class and its components
		/// </summary>
		[Browsable(false)]
		public virtual bool IsForComponent
		{
			get
			{
				return true;
			}
		}
		[Browsable(false)]
		public virtual bool IsForMethodParameter
		{
			get
			{
				return false;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		public virtual bool ReadOnly { get; set; }
		public virtual bool NameReadOnly
		{
			get
			{
				return true;
			}
		}
		/// <summary>
		/// indicate selection flag
		/// </summary>
		public bool IsSelected
		{
			get
			{
				return _selected;
			}
			set
			{
				_selected = value;
				refreshLabel();
				this.Invalidate();
				this.Refresh();
			}
		}
		protected virtual bool UseLabel
		{
			get
			{
				object[] vs = this.GetType().GetCustomAttributes(typeof(UseLabelAttribute), false);
				if (vs != null && vs.Length > 0)
				{
					UseLabelAttribute u = (UseLabelAttribute)vs[0];
					return u.UseLabel;
				}
				return true;
			}
		}
		public bool IsSelectedByMouseMove
		{
			get
			{
				if (this.IsMouseIn)
					return true;
				if (_label != null && _label.IsMouseIn)
					return true;
				return false;
			}
		}
		public ILimnorDesignPane DesignerPane
		{
			get
			{
				return _designer.Project.GetTypedData<ILimnorDesignPane>(_designer.ObjectMap.ClassId);
			}
		}
		public ILimnorDesigner Designer
		{
			get
			{
				if (_designer == null)
				{
					MethodDesignerHolder h = null;
					Control p = this.Parent;
					while (p != null)
					{
						h = p as MethodDesignerHolder;
						if (h != null)
						{
							break;
						}
						p = p.Parent;
					}
					if (h != null)
					{
						_designer = h.Loader;
					}
				}
				return _designer;
			}
			set
			{
				_designer = value;
			}
		}
		public DrawingLabel Label
		{
			get
			{
				return _label;
			}
		}
		public IIconHolder IconHolder
		{
			get
			{
				IIconHolder mh = this.Parent as IIconHolder;
				if (mh == null)
				{
					throw new DesignerException("The component parent is not an IIconHolder");
				}
				return mh;
			}
		}
		public UInt32 ClassId
		{
			get
			{
				if (_classId == 0)
				{
					if (_componentPointer != null)
					{
						_classId = _componentPointer.ClassId;
					}
				}
				return _classId;
			}
		}
		public UInt32 DefinitionClassId
		{
			get
			{
				MemberComponentIdCustom mcc = _componentPointer as MemberComponentIdCustom;
				if (mcc != null)
				{
					if (mcc.VariableCustomType != null)
					{
						return mcc.VariableCustomType.ClassId;
					}
				}
				return ClassId;
			}
		}
		public UInt32 MemberId
		{
			get
			{
				if (_memberId == 0)
				{
					if (_componentPointer != null)
					{
						return _componentPointer.MemberId;
					}
					else
					{
						_memberId = (UInt32)(Guid.NewGuid().GetHashCode());
					}
				}
				return _memberId;
			}
		}

		public UInt64 WholeId
		{
			get
			{
				return DesignUtil.MakeDDWord(MemberId, ClassId);
			}
		}
		public IClass ClassPointer
		{
			get
			{
				return _componentPointer;
			}
			set
			{
				_componentPointer = value;
				OnSetImage();
				if (_componentPointer != null)
				{
					RefreshLabelText();
				}
			}
		}
		public Image IconImage
		{
			get
			{
				if (_img == null)
				{
					OnSetImage();
				}
				return _img;
			}
		}
		#endregion
		#region Methods
		protected abstract void OnEstablishObjectOwnership(MethodClass owner);
		public void EstablishObjectOwnership(MethodClass owner)
		{
			OnEstablishObjectOwnership(owner);
		}
		public override string ToString()
		{
			if (_label != null)
			{
				return _label.Text;
			}
			return this.GetType().Name;
		}
		public void BringToFront2()
		{
			if (_label != null)
			{
				_label.BringToFront();
			}
			this.BringToFront();
		}
		public void RefreshIcon()
		{
			OnSetImage();
			this.Invalidate();
		}
		public void ResetLabelPosition()
		{
			if (_label != null)
			{
				_label.RelativePosition.IsXto0 = true;
				_label.RelativePosition.IsYto0 = false;
				_label.RelativePosition.Location = new Point(-this.Width / 2, 2);
				_label.AdjustPosition();
			}
		}
		public void SetDesigner(ILimnorDesigner designer)
		{
			Designer = designer;
		}
		protected Size GetLabelSize()
		{
			if (_label != null)
			{
				return _label.Size;
			}
			return Size.Empty;
		}
		protected Point GetLabelLocation()
		{
			if (_label != null)
			{
				return _label.Location;
			}
			return Point.Empty;
		}
		protected void ShowLabel(bool show)
		{
			if (_label != null)
			{
				_label.Visible = show;
			}
		}
		protected void SetIconImage(Image img)
		{
			if (img != null)
			{
				_img = img;
			}
		}
		public virtual void SetLabelText(string name)
		{
			_overrideLabelText = name;
			RefreshLabelText();
		}
		public void AdjustLabelPosition()
		{
			if (_label != null)
			{
				if (_label.Top > this.Top + this.Height)
				{
					_label.Left = this.Left;
				}
				else if (_label.Top < this.Top - _label.Height)
				{
					_label.Left = this.Left;
				}
				else if (_label.Left > this.Left + this.Width)
				{
					_label.Left = this.Left + this.Width;
				}
				else
				{
					_label.Left = this.Left - _label.Width;
				}
				_label.SaveLocation();
				_label.SaveRelativePosition();
			}
		}
		public void RefreshLabelPosition()
		{
			if (_label != null)
			{
				_label.AdjustPosition();
			}
		}
		private string _overrideLabelText;
		public void RefreshLabelText()
		{
			if (_label != null)
			{
				if (string.IsNullOrEmpty(_overrideLabelText))
				{
					_label.Text = _componentPointer.ExpressionDisplay;
				}
				else
				{
					_label.Text = _overrideLabelText;
				}
				_label.Refresh();
			}
		}
		public void RemoveLabel()
		{
			if (_label != null)
			{
				Control p = _label.Parent;
				if (p != null)
				{
					p.Controls.Remove(_label);
				}
			}
		}

		#endregion
		#region IXmlNodeSerializable Members
		const string XML_RelativePosition = "RelativePosition";
		const string XML_Location = "Location";
		public override void OnWriteToXmlNode(IXmlCodeWriter writer, XmlNode node)
		{
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ClassID, ClassId);
			XmlUtil.SetAttribute(node, XmlTags.XMLATT_ComponentID, MemberId);
			XmlNode pn = node.OwnerDocument.CreateElement(XML_Location);
			node.AppendChild(pn);
			writer.WriteValue(pn, this.Location, null);
			//
			if (_label != null)
			{
				XmlSerialization.WriteValueToChildNode(node, XML_RelativePosition, _label.RelativePosition.Location);
				XmlNode nd = node.SelectSingleNode(XML_RelativePosition);
				XmlSerialization.SetAttribute(nd, "xTo0", _label.RelativePosition.IsXto0);
				XmlSerialization.SetAttribute(nd, "yTo0", _label.RelativePosition.IsYto0);
			}
		}

		public override void OnReadFromXmlNode(IXmlCodeReader reader, XmlNode node)
		{
			_classId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
			_memberId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID);
			XmlNode pn = node.SelectSingleNode(XML_Location);
			if (pn != null)
			{
				this.Location = reader.ReadValue<Point>(pn, this);
			}
			if (UseLabel)
			{
				object v;
				if (XmlSerialization.ReadValueFromChildNode(node, XML_RelativePosition, out v))
				{
					if (_label == null)
					{
						_label = new DrawingLabel(this);
					}
					_label.RelativePosition.Location = (Point)v;
					XmlNode nd = node.SelectSingleNode(XML_RelativePosition);
					_label.RelativePosition.IsXto0 = XmlSerialization.GetAttributeBool(nd, "xTo0", true);
					_label.RelativePosition.IsYto0 = XmlSerialization.GetAttributeBool(nd, "yTo0", true);
				}
			}
		}

		#endregion
		#region IControlWithID Members

		public uint ControlID
		{
			get { return MemberId; }
		}

		#endregion
		#region ICloneable Members
		public override object Clone()
		{
			ComponentIcon obj = (ComponentIcon)base.Clone();
			obj._classId = _classId;
			obj._img = _img;
			obj._memberId = _memberId;
			obj._componentPointer = _componentPointer;
			obj._designer = _designer;

			obj._messageReturn = _messageReturn;
			if (_label != null)
			{
				obj._label = (DrawingLabel)_label.Clone();
				obj._label.SetOwner(obj);
			}
			return obj;
		}
		#endregion
		#region ICustomTypeDescriptor Members

		public virtual AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(_componentPointer, true);
		}

		public virtual string GetClassName()
		{
			return TypeDescriptor.GetClassName(_componentPointer, true);
		}

		public virtual string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(_componentPointer, true);
		}

		public virtual TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(_componentPointer, true);
		}

		public virtual EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(_componentPointer, true);
		}

		public virtual PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(_componentPointer, true);
		}

		public virtual object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(_componentPointer, editorBaseType, true);
		}

		public virtual EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(_componentPointer, attributes, true);
		}

		public virtual EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(_componentPointer, true);
		}
		protected virtual void OnSetImage()
		{
			if (_componentPointer != null)
			{
				if (_componentPointer.ImageIcon != null)
				{
					_img = _componentPointer.ImageIcon;
				}
			}
		}
		public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection baseProps;
			ParameterClass pc = _componentPointer as ParameterClass;
			if (pc != null)
			{
				baseProps = pc.GetProperties(attributes);
			}
			else
			{
				baseProps = TypeDescriptor.GetProperties(_componentPointer, attributes, true);
				if (ReadOnly)
				{
					List<PropertyDescriptor> ps = new List<PropertyDescriptor>();
					foreach (PropertyDescriptor p in baseProps)
					{
						List<Attribute> aa = new List<Attribute>();
						if (p.Attributes != null)
						{
							foreach (Attribute a in p.Attributes)
							{
								if (!(a is EditorAttribute))
								{
									aa.Add(a);
								}
							}
						}
						object v = p.GetValue(_componentPointer);
						string s;
						if (v == null)
							s = "";
						else
							s = v.ToString();
						PropertyDescriptorForDisplay r = new PropertyDescriptorForDisplay(_componentPointer.GetType(), p.Name, s, aa.ToArray());
						ps.Add(r);
					}
					baseProps = new PropertyDescriptorCollection(ps.ToArray());
				}
			}
			IList<PropertyDescriptor> lst = OnGetProperties(attributes);
			if (lst != null && lst.Count > 0)
			{
				PropertyDescriptor[] pa = new PropertyDescriptor[baseProps.Count + lst.Count];
				baseProps.CopyTo(pa, 0);
				lst.CopyTo(pa, baseProps.Count);
				baseProps = new PropertyDescriptorCollection(pa);
			}
			return baseProps;
		}
		protected virtual IList<PropertyDescriptor> OnGetProperties(Attribute[] attrs)
		{
			return null;
		}
		public virtual PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}

		public virtual object GetPropertyOwner(PropertyDescriptor pd)
		{
			return _componentPointer;

		}

		#endregion
		#region IWithProject Members

		public virtual LimnorProject Project
		{
			get
			{
				if (this.Parent != null)
				{
					IWithProject p = this.Parent as IWithProject;
					if (p != null)
					{
						return p.Project;
					}
					else
					{
						MathNode.Log(TraceLogClass.MainForm, new DesignerException("{0} does not implement IWithProject.", this.Parent.GetType()));
					}
				}
				return null;
			}
		}

		#endregion
		#region ISerializerProcessor Members

		public abstract void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer);

		#endregion
	}
	/// <summary>
	/// in a method editor, represent an object:
	/// 1. the root class
	/// 2. a component hosted by the root class
	/// 4. a local variable
	/// </summary>
	[UseParentObject]
	public abstract class ComponentIconForMethod : ComponentIcon, IScopeMethodHolder
	{
		#region fields and constructors
		private MethodClass _methodClass;
		private LimnorContextMenuCollection _menuData;
		public ComponentIconForMethod()
		{
		}
		public ComponentIconForMethod(MethodClass method)
			: base()
		{
			_methodClass = method;
		}
		public ComponentIconForMethod(ActionBranch branch)
			: base()
		{
			_methodClass = branch.Method;
		}
		public ComponentIconForMethod(ILimnorDesigner designer, IClass pointer, MethodClass method)
			: this(method)
		{
			if (designer == null)
			{
				throw new DesignerException("designer is null for ComponentIcon");
			}
			if (pointer == null)
			{
				throw new DesignerException("pointer is null for ComponentIcon");
			}
			Init(designer, pointer);
		}
		#endregion
		#region ICloneable Members
		public override object Clone()
		{
			ComponentIconForMethod obj = (ComponentIconForMethod)base.Clone();
			obj._methodClass = _methodClass;
			obj._menuData = _menuData;
			return obj;
		}
		#endregion
		#region Properties
		private MethodDesignerHolder _mh;
		[Browsable(false)]
		public MethodDesignerHolder MethodViewer
		{
			get
			{
				if (_mh == null)
				{
					Control p = this.Parent;
					MethodDesignerHolder mv = p as MethodDesignerHolder;
					while (mv == null && p != null)
					{
						p = p.Parent;
						mv = p as MethodDesignerHolder;
					}
					_mh = mv;
				}
				return _mh;
			}
		}
		[Browsable(false)]
		public MethodClass Method
		{
			get
			{
				return _methodClass;
			}
		}
		#endregion
		#region Methods
		public void SetOwnerMethod(MethodClass m)
		{
			_methodClass = m;
		}
		public abstract PropertyPointer CreatePropertyPointer(string propertyName);
		protected abstract LimnorContextMenuCollection GetMenuData();
		protected abstract IAction OnCreateAction(MenuItemDataMethod data, ILimnorDesignPane designPane);
		protected abstract ActionClass OnCreateSetPropertyAction(MenuItemDataProperty data);
		protected override void OnCreateContextMenu(ContextMenu mnu, Point location)
		{
			if (_menuData == null)
			{
				_menuData = GetMenuData();
			}
			if (_menuData != null)
			{
				MenuItem mi;
				MenuItem m0;
				MenuItem m1;
				//
				#region Create Action
				mi = new MenuItemWithBitmap("Create Action", Resources._newMethodAction.ToBitmap());

				List<MenuItemDataMethod> methods = _menuData.PrimaryMethods;
				foreach (MenuItemDataMethod kv in methods)
				{
					m0 = new MenuItemWithBitmap(kv.Key, Resources._methodAction.ToBitmap());
					m0.Click += new EventHandler(miAction_Click);
					kv.Location = location;
					m0.Tag = kv;
					mi.MenuItems.Add(m0);
				}
				m1 = new MenuItemWithBitmap("More methods", Resources._methods.ToBitmap());
				mi.MenuItems.Add(m1);
				methods = _menuData.SecondaryMethods;
				foreach (MenuItemDataMethod kv in methods)
				{
					m0 = new MenuItemWithBitmap(kv.Key, Resources._methodAction.ToBitmap());
					m0.Click += new EventHandler(miAction_Click);
					kv.Location = location;
					m0.Tag = kv;
					m1.MenuItems.Add(m0);
				}
				m0 = new MenuItemWithBitmap("*All methods* =>", Resources._dialog.ToBitmap());
				MenuItemDataMethodSelector miAll = new MenuItemDataMethodSelector(m0.Text, _menuData);
				miAll.Location = location;
				m0.Tag = miAll;
				m1.MenuItems.Add(m0);
				m0.Click += new EventHandler(miSetMethods_Click);
				//
				mnu.MenuItems.Add(mi);
				#endregion
				//
				#region Create Set Property Action
				mi = new MenuItemWithBitmap("Create Set Property Action", Resources._newPropAction.ToBitmap());
				List<MenuItemDataProperty> properties = _menuData.PrimaryProperties;
				foreach (MenuItemDataProperty kv in properties)
				{
					m0 = new MenuItemWithBitmap(kv.Key, Resources._propAction.ToBitmap());
					m0.Click += new EventHandler(miSetProperty_Click);
					kv.Location = location;
					m0.Tag = kv;
					mi.MenuItems.Add(m0);
				}
				m1 = new MenuItemWithBitmap("More properties", Resources._properties.ToBitmap());
				mi.MenuItems.Add(m1);
				properties = _menuData.SecondaryProperties;
				foreach (MenuItemDataProperty kv in properties)
				{
					m0 = new MenuItemWithBitmap(kv.Key, Resources._propAction.ToBitmap());
					m0.Click += new EventHandler(miSetProperty_Click);
					kv.Location = location;
					m0.Tag = kv;
					m1.MenuItems.Add(m0);
				}
				m0 = new MenuItemWithBitmap("*All properties* =>", Resources._dialog.ToBitmap());
				m1.MenuItems.Add(m0);
				MenuItemDataPropertySelector pAll = new MenuItemDataPropertySelector(m0.Text, _menuData);
				pAll.Location = location;
				m0.Tag = pAll;
				m0.Click += new EventHandler(miSetProperties_Click);
				//
				mnu.MenuItems.Add(mi);
				#endregion
				//
				#region Assign Actions
				//hooking events is not done inside a method
				mi = new MenuItemWithBitmap("Assign Action", Resources._event1.ToBitmap());
				List<MenuItemDataEvent> events = _menuData.PrimaryEvents;
				foreach (MenuItemDataEvent kv in events)
				{
					m0 = new MenuItemWithBitmap(kv.Key, Resources._event1.ToBitmap());
					m0.Click += new EventHandler(miAssignEvent_Click);
					m0.Tag = kv;
					mi.MenuItems.Add(m0);
				}
				m1 = new MenuItem("More events");
				mi.MenuItems.Add(m1);
				events = _menuData.SecondaryEvents;
				foreach (MenuItemDataEvent kv in events)
				{
					m0 = new MenuItemWithBitmap(kv.Key, Resources._event1.ToBitmap());
					m0.Click += new EventHandler(miAssignEvent_Click);
					m0.Tag = kv;
					m1.MenuItems.Add(m0);
				}
				m0 = new MenuItem("*All events* =>");
				m1.MenuItems.Add(m0);
				m0.Click += new EventHandler(miSetEvents_Click);
				mnu.MenuItems.Add(mi);
				#endregion
			}
		}
		#endregion
		#region private methods
		private void miAction_Click(object sender, EventArgs e)
		{
			MenuItemDataMethod data = (MenuItemDataMethod)(((MenuItem)sender).Tag);
			createNewAction(data);
		}
		private void miSetMethods_Click(object sender, EventArgs e)
		{
			miAction_Click(sender, e);
			_menuData = GetMenuData();
		}
		private void createNewAction(MenuItemDataMethod data)
		{
			ILimnorDesignPane dp = Designer.Project.GetTypedData<ILimnorDesignPane>(Designer.ObjectMap.ClassId);
			IAction act = OnCreateAction(data, dp);
			if (act != null)
			{
				if (!(this.ClassPointer is LocalVariable))
				{
					if (MethodEditContext.IsWebPage)
					{
						if (!MethodEditContext.CheckAction(act, this.FindForm()))
						{
							return;
						}
					}
				}
				MethodDiagramViewer mv = MethodViewer.GetCurrentViewer();
				act.ScopeMethod = _methodClass;
				act.ActionHolder = MethodViewer.ActionsHolder;
				double x0, y0;
				ComponentIconEvent.CreateRandomPoint((double)((mv.Width - 20) / 2), out x0, out y0);
				if (x0 < 0) x0 = 10;
				if (y0 < 0) y0 = 10;
				ActionViewer av = MethodViewer.AddNewAction(act, new Point((mv.Width - 20) / 2 + (int)x0, (mv.Height - 20) / 2 + (int)y0));
				if (av.Parent == null)
				{
#if DEBUG
					MessageBox.Show("Adding action viewer failed (1)");
#endif
					mv.Controls.Add(av);
				}
				else
				{
				}
			}
		}
		private void miSetProperty_Click(object sender, EventArgs e)
		{
			MenuItemDataProperty data = (MenuItemDataProperty)(((MenuItem)sender).Tag);
			MethodDiagramViewer mv = MethodViewer.GetCurrentViewer();
			ActionClass act = OnCreateSetPropertyAction(data);
			if (act != null)
			{
				if (!(this.ClassPointer is LocalVariable))
				{
					if (MethodEditContext.IsWebPage)
					{
						if (!MethodEditContext.CheckAction(act, this.FindForm()))
						{
							return;
						}
					}
				}
				act.SetScopeMethod(_methodClass);
				int x = (mv.Width / 4 - 20) / 2;
				int y = (mv.Height / 4 - 20) / 2;
				if (x < 30)
					x = 30;
				if (y < 30)
					y = 30;
				mv.AddNewAction(act, new Point(x, y));
			}
		}
		private void miSetProperties_Click(object sender, EventArgs e)
		{
			miSetProperty_Click(sender, e);
			_menuData = GetMenuData();
		}
		private void miAssignEvent_Click(object sender, EventArgs e)
		{
			MenuItemDataEvent data = (MenuItemDataEvent)(((MenuItem)sender).Tag);
			MethodDiagramViewer mv = MethodViewer.GetCurrentViewer();
			IEvent ep = data.CreateEventPointer(data.Owner);
			if (ep != null)
			{
				assignAction(ep);
			}
		}
		private void miSetEvents_Click(object sender, EventArgs e)
		{
			MethodDiagramViewer mv = MethodViewer.GetCurrentViewer();
			ILimnorDesignerLoader loader = mv.Loader;
			DlgSelectEvent dlg = new DlgSelectEvent();
			dlg.LoadData(_menuData);
			DialogResult ret = dlg.ShowDialog(this.FindForm());
			if (dlg.FrequentlyUsedMethodsChanged)
			{
				_menuData.RemoveMenuCollection();
				loader.DesignPane.ResetContextMenu();
			}
			if (ret == DialogResult.OK)
			{
				IEvent ei = dlg.ReturnEventInfo;
				if (ei != null)
				{
					EventPointer ep = ei as EventPointer;
					if (ep == null)
					{
						EventClass ec = ei as EventClass;
						if (ec != null)
						{
							EventClass ec2 = (EventClass)ec.Clone();
							ec2.SetHolder(this.ClassPointer);
							CustomEventPointer cep = new CustomEventPointer(ec2, this.ClassPointer);
							ei = cep;
						}
					}
					assignAction(ei);
				}
			}
		}
		private void assignAction(IEvent ep)
		{
			if (ep != null)
			{
				MethodDiagramViewer mv = MethodViewer.GetCurrentViewer();
				mv.AddAssignActionsAction(ep);
			}
		}
		#endregion
		#region IScopeMethodHolder Members

		public MethodClass GetScopeMethod()
		{
			return _methodClass;
		}

		#endregion
	}
	public class ComponentIconPublic : ComponentIconForMethod, IDelayedInitialize
	{
		#region fields and constructors
		public ComponentIconPublic()
			: base()
		{
		}
		public ComponentIconPublic(MethodClass method)
			: base(method)
		{
		}
		public ComponentIconPublic(ActionBranch branch)
			: base(branch)
		{
		}
		public ComponentIconPublic(ILimnorDesigner designer, IClass pointer, MethodClass method)
			: base(designer, pointer, method)
		{
		}
		#endregion
		#region Methods
		protected override LimnorContextMenuCollection GetMenuData()
		{
			return LimnorContextMenuCollection.GetMenuCollection(ClassPointer);
		}
		protected override IAction OnCreateAction(MenuItemDataMethod data, ILimnorDesignPane designPane)
		{
			return data.CreateMethodAction(designPane, ClassPointer, MethodViewer.Method, MethodViewer.ActionsHolder);
		}
		protected override ActionClass OnCreateSetPropertyAction(MenuItemDataProperty data)
		{
			return data.CreateSetPropertyAction(DesignerPane, ClassPointer, MethodViewer.Method, MethodViewer.ActionsHolder);
		}
		public override PropertyPointer CreatePropertyPointer(string propertyName)
		{
			ILimnorDesignPane dp = DesignerPane;
			return dp.Loader.CreatePropertyPointer(dp.Loader.ObjectMap.GetObjectByID(MemberId), propertyName);
		}
		protected override void OnCreateContextMenu(ContextMenu mnu, Point location)
		{
			base.OnCreateContextMenu(mnu, location);
			if (!(this.ClassPointer is ClassPointer))
			{
				MenuItem mi;
				//
				mi = new MenuItemWithBitmap("Create Set Value Action", Resources._setVar.ToBitmap());
				mi.Click += new EventHandler(miNewInstance_Click);
				mnu.MenuItems.Add(mi);
			}
		}
		protected override void OnEstablishObjectOwnership(MethodClass owner)
		{
		}
		#endregion
		#region private methods
		void miNewInstance_Click(object sender, EventArgs e)
		{
			MethodDiagramViewer mv = MethodViewer.GetCurrentViewer();
			if (mv != null)
			{
				MemberComponentId lv = this.ClassPointer as MemberComponentId;
				ActionAssignComponent act = new ActionAssignComponent(MethodViewer.Method.RootPointer);
				act.ActionOwner = lv;
				act.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
				act.ActionName = MethodViewer.RootClass.CreateNewActionName("Create" + lv.ObjectType.Name);
				Point p = mv.PointToClient(System.Windows.Forms.Cursor.Position);
				if (p.X < 0)
					p.X = 10;
				if (p.Y < 0)
					p.Y = 10;
				act.ValidateParameterValues();
				mv.AddNewAction(act, p);
			}
		}
		#endregion
		#region ICloneable Members
		#endregion
		#region ISerializerProcessor Members

		public override void OnPostSerialize(ObjectIDmap objMap, XmlNode objectNode, bool saved, object serializer)
		{
			if (saved)
			{
			}
			else
			{
				if (MemberId == objMap.MemberId)
				{
					ClassPointer = objMap.GetTypedData<ClassPointer>();
				}
				else
				{
					ClassInstancePointer cr = DesignUtil.GetClassRef(WholeId, objMap);
					if (cr != null)
					{
						ClassPointer = cr;
					}
					else
					{
						object v = objMap.GetObjectByID(MemberId);
						if (v == null)
						{
							if (objMap.Count == 0)
							{
							}
							else
							{
								if (MemberId == 3667767822)
								{
									//it is the HtmlElement_body
								}
								else
								{
									//this time the object may not be available.
								}
							}
						}
						else
						{
							MemberComponentId mc = MemberComponentId.CreateMemberComponentId(objMap.GetTypedData<ClassPointer>(), v, MemberId, null);
							ClassPointer = mc;
						}
					}
				}
			}
		}

		#endregion
		#region ICustomTypeDescriptor Members
		private ClassPointerDisplay _rootDisplay;
		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			ClassPointer cp = this.ClassPointer as ClassPointer;
			if (cp != null)
			{
				if (_rootDisplay == null)
				{
					_rootDisplay = new ClassPointerDisplay(cp);
				}
				return TypeDescriptor.GetProperties(_rootDisplay, attributes);
			}
			return base.GetProperties(attributes);
		}
		public override object GetPropertyOwner(PropertyDescriptor pd)
		{
			ClassPointer cp = this.ClassPointer as ClassPointer;
			if (cp != null)
			{
				if (_rootDisplay == null)
				{
					_rootDisplay = new ClassPointerDisplay(cp);
				}
				return _rootDisplay;
			}
			return base.GetPropertyOwner(pd);
		}
		#endregion
		#region IDelayedInitialize Members

		public void OnDelayedPostSerialize(ObjectIDmap objMap, XmlNode objectNode, XmlObjectReader reader)
		{
			OnPostSerialize(objMap, objectNode, false, reader);
		}

		public void SetReader(ObjectIDmap objMap, XmlNode objectNode, XmlObjectReader reader)
		{

		}

		#endregion
	}
	/// <summary>
	/// to display ClassPointer in PropertyGrid within method editor
	/// </summary>
	class ClassPointerDisplay
	{
		#region fields and constructors
		private ClassPointer _pointer;
		public ClassPointerDisplay(ClassPointer pointer)
		{
			_pointer = pointer;
		}
		#endregion
		#region Properties
		[Description("Class name")]
		[ParenthesizePropertyName(true)]
		public string ClassName
		{
			get
			{
				return _pointer.Name;
			}
		}
		[Description("The class this class derived from")]
		public string BaseClass
		{
			get
			{
				if (_pointer.BaseClassPointer != null)
				{
					return _pointer.BaseClassPointer.DisplayName;
				}
				return _pointer.BaseClassType.Name;
			}
		}
		#endregion
	}
}
