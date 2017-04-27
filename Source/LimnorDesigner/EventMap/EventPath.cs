/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LimnorDesigner;
using System.Xml;
using LimnorDesigner.MethodBuilder;
using VSPrj;
using MathExp;
using LimnorDesigner.Event;
using LimnorDesigner.Property;
using VPL;
using XmlUtility;
using LimnorDesigner.Action;
using LimnorDesigner.Web;
using System.IO;
using TraceLog;
using Limnor.WebBuilder;
using Limnor.PhpComponents;
using LimnorDatabase;
using WindowsUtility;

namespace LimnorDesigner.EventMap
{
	/// <summary>
	/// design viewer showing event paths
	/// </summary>
	public partial class EventPath : Diagram, IXDesignerViewer, IIconHolder, IWithProject
	{
		#region fields and constructors
		public const string XML_EventPath = "EventPath";
		private MultiPanes _holder;
		private Cursor _cursor;
		private ILimnorDesigner _loader;
		private EventPathData _data;
		private EventPathHolder _eventPathHolder;
		private EventIcon _selectedEventIcon;
		public EventPath()
		{
		}
		public EventPath(ILimnorDesigner designerLoader, EventPathHolder pathHolder)
		{
			Init(designerLoader, pathHolder);
		}
		public void Init(ILimnorDesigner designerLoader, EventPathHolder pathHolder)
		{
			_loader = designerLoader;
			_eventPathHolder = pathHolder;
			InitializeComponent();
			string f = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "panAll.cur");
			if (File.Exists(f))
			{
				try
				{
					_cursor = new Cursor(f);
				}
				catch
				{
					_cursor = Cursors.PanNW;
				}
			}
			this.Cursor = _cursor;
		}
		#endregion

		#region Methods
		private Dictionary<EasyGrid, List<ComponentIconEvent>> _egCols;
		public bool CacheGridColumns(EasyGrid eg)
		{
			if (_egCols == null)
			{
				_egCols = new Dictionary<EasyGrid, List<ComponentIconEvent>>();
			}
			List<ComponentIconEvent> lst = new List<ComponentIconEvent>();
			if (_egCols.ContainsKey(eg))
			{
				_egCols[eg] = lst;
			}
			else
			{
				_egCols.Add(eg, lst);
			}
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				foreach (ComponentIconEvent cie in icons)
				{
					if (cie.ClassPointer != null)
					{
						DataGridViewColumn dgc = cie.ClassPointer.ObjectInstance as DataGridViewColumn;
						if (dgc != null && dgc.DataGridView == eg)
						{
							lst.Add(cie);
						}
					}
				}
			}
			return lst.Count > 0;
		}
		public bool RestoreGridColumns(EasyGrid eg)
		{
			if (_egCols != null)
			{
				if (_egCols.ContainsKey(eg))
				{
					List<ComponentIconEvent> lst = _egCols[eg];
					if (lst.Count > 0)
					{
						List<ComponentIconEvent> icons = _data.ComponentIconList;
						if (icons != null && icons.Count > 0)
						{
							foreach (ComponentIconEvent cie in icons)
							{
								if (cie.ClassPointer != null)
								{
									DataGridViewColumn dgc = cie.ClassPointer.ObjectInstance as DataGridViewColumn;
									if (dgc != null && dgc.DataGridView == eg)
									{
										foreach (ComponentIconEvent cie0 in lst)
										{
											DataGridViewColumn dgc0 = cie0.ClassPointer.ObjectInstance as DataGridViewColumn;
											if (string.Compare(dgc.DataPropertyName, dgc0.DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
											{
												cie.Location = cie0.Location;
												break;
											}
										}
									}
								}
							}
						}
					}
					_data.ShowLinks();
					this.Refresh();
					return true;
				}
			}
			return false;
		}
		protected override void OnCreateContextMenuForLinkLine(LinkLineNode lineNode, ContextMenu mnu, Point pt)
		{
			EventPortOut po = lineNode.Start as EventPortOut;
			if (po != null)
			{
				EventPortIn pi = lineNode.End as EventPortIn;
				if (pi != null)
				{
					MenuItemWithBitmap mi = new MenuItemWithBitmap("Remove", mi_removeOutPortActionsfromEvent, Resources.cancel);
					mi.Tag = po;
					mnu.MenuItems.Add("-");
					mnu.MenuItems.Add(mi);
				}
			}
		}
		public void OnCreateContextMenuForRootComponent(ContextMenu mnu, Point pt)
		{
			MenuItem mi;
			ClassPointer root = _loader.GetRootId();
			if (root.IsWebService)
			{
				mi = new MenuItemWithBitmap("Create local method", mi_newMethod, Resources._newMethod.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
				mi = new MenuItemWithBitmap("Create web method", mi_newWebMethod, Resources._newWebMethod.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
			}
			else
			{
				if (root.IsWebPage || root.IsWebApp)
				{
					mi = new MenuItemWithBitmap("Create web client method", mi_newClientMethod, Resources._webClientMethodNew.ToBitmap());
					mi.Tag = pt;
					mnu.MenuItems.Add(mi);
					mi = new MenuItemWithBitmap("Create web server method", mi_newMethod, Resources._webServerMethodNew.ToBitmap());
					mi.Tag = pt;
					mnu.MenuItems.Add(mi);
				}
				else if (root.IsRemotingHost)
				{
					mi = new MenuItemWithBitmap("Create local method", mi_newClientMethod, Resources._newMethod.ToBitmap());
					mi.Tag = pt;
					mnu.MenuItems.Add(mi);
					mi = new MenuItemWithBitmap("Create service method", mi_newServiceMethod, Resources._newService.ToBitmap());
					mi.Tag = pt;
					mnu.MenuItems.Add(mi);
				}
				else
				{
					mi = new MenuItemWithBitmap("Create method", mi_newMethod, Resources._newMethod.ToBitmap());
					mi.Tag = pt;
					mnu.MenuItems.Add(mi);
				}
			}
			//
			if (root.IsWebApp || root.IsWebPage)
			{
				mi = new MenuItemWithBitmap("Create new web client property", mi_newPropertyWebClient, Resources._custPropClientNew.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
				mi = new MenuItemWithBitmap("Create new web server property", mi_newPropertyWebServer, Resources._custPropServerNew.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
			}
			else
			{
				mi = new MenuItemWithBitmap("Create property", mi_newProperty, Resources._newCustProp.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
			}
			if (root.IsWebPage)
			{
				mi = new MenuItemWithBitmap("Create web client event", mi_newWebClientEvent, Resources._newEvent.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
			}
			else
			{
				mi = new MenuItemWithBitmap("Create event", mi_newEvent, Resources._newEvent.ToBitmap());
				mi.Tag = pt;
				mnu.MenuItems.Add(mi);
			}
		}
		protected override void OnCreateContextMenu(ContextMenu mnu, Point pt)
		{
			base.OnCreateContextMenu(mnu, pt);
			if (mnu.MenuItems.Count > 0)
			{
				mnu.MenuItems.Add("-");
			}
			MenuItem mi;
			OnCreateContextMenuForRootComponent(mnu, pt);
			mnu.MenuItems.Add("-");
			mi = new MenuItemWithBitmap("Move unlinked icons below here", mi_moveUnlinkedIcons, Resources._moveUnlinked.ToBitmap());
			mi.Tag = pt;
			mnu.MenuItems.Add(mi);
			mi = new MenuItemWithBitmap("Move an icon to here", mi_moveIcon, Resources._moveIcon.ToBitmap());
			mi.Tag = pt;
			mnu.MenuItems.Add(mi);
		}
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground(e);
			foreach (Control c in Controls)
			{
				ComponentIconEvent cie = c as ComponentIconEvent;
				if (cie != null)
				{
					cie.ShowEventOwnerLinks(e);
				}
			}
			isFrameOn = false;
		}
		protected override void OnLineSelectionChanged()
		{
			if (_selectedEventIcon != null)
			{
				_selectedEventIcon.IsSelected = false;
				_selectedEventIcon.Invalidate();
				_selectedEventIcon = null;
			}
			_eventPathHolder.OnLineSelectionChanged(this.SelectedLinkLine.StartNode as EventPortOut);

		}
		protected override void OnLineNodeAdded(LinkLineNode newNode)
		{
			NotifyChanges();
			newNode.Move += new EventHandler(ln_Move);
		}
		public void ClearEventIconSelection()
		{
			_selectedEventIcon = null;
		}
		public override void OnActiveDrawingMouseUp(ActiveDrawing a)
		{
			NotifyCurrentChanges();
		}
		public override void OnLineNodeDeleted(LinkLineNode previousNode, LinkLineNode nodeRemoved, LinkLineNode nextNode)
		{
			NotifyChanges();
		}
		public bool AssignActions(IEvent e)
		{
			return _holder.AssignActions(e, FindForm());
		}
		public void RemoveEventhandlers(IEvent e)
		{
			_holder.RemoveEventHandlers(e);
		}
		public void NotifyChanges()
		{
			if (_holder != null && _holder.Loaded && !_holder.IsReSizing)
			{
				ILimnorLoader l = (ILimnorLoader)_loader;
				XmlNode node = l.Node.SelectSingleNode(XML_EventPath);
				if (node == null)
				{
					node = l.Node.OwnerDocument.CreateElement(XML_EventPath);
					l.Node.AppendChild(node);
				}
				l.Writer.WriteObjectToNode(node, _data);
				l.NotifyChanges();
			}
		}
		public void NotifyCurrentChanges()
		{
			if (this.MouseDownControl == null)
			{
				NotifyChanges();
			}
		}
		public void SetupLineNodeMonitor()
		{
			foreach (Control c in Controls)
			{
				LinkLineNode ln = c as LinkLineNode;
				if (ln != null)
				{
					ln.Move += new EventHandler(ln_Move);
				}
			}
		}


		public ComponentIconEvent GetSelectedComponentIcon()
		{
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				foreach (ComponentIconEvent cie in icons)
				{
					if (cie.IsSelected)
					{
						return cie;
					}
				}
			}
			return null;
		}
		public ComponentIconEvent GetRootComponentIcon()
		{
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				foreach (ComponentIconEvent cie in icons)
				{
					if (cie.IsRootClass)
					{
						return cie;
					}
				}
			}
			return null;
		}
		public void RefreshEventHandlers(EventAction ea)
		{
			ClassPointer root = _loader.GetRootId();
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				foreach (ComponentIconEvent cie in icons)
				{
					bool b = false;
					if (cie.IsForComponent)
					{
						List<EventIcon> eis = cie.EventIcons;
						if (eis != null && eis.Count > 0)
						{
							foreach (EventIcon ei in eis)
							{
								if (ei.Event.IsSameObjectRef(ea.Event))
								{
									List<EventPortOut> ports = ei.SourcePorts;
									if (ports != null && ports.Count > 0)
									{
										foreach (EventPortOut po in ports)
										{
											po.RefreshActionlist(ea, root);
										}
									}
									b = true;
									break;
								}
							}
						}
					}
					if (b)
					{
						break;
					}
				}
			}
		}
		public void SelectEventIcon(EventIcon ei)
		{
			_selectedEventIcon = ei;
			foreach (Control c in Controls)
			{
				EventIcon e = c as EventIcon;
				if (e != null)
				{
					if (e == ei)
					{
						if (!e.IsSelected)
						{
							e.IsSelected = true;
						}
					}
					else
					{
						if (e.IsSelected)
						{
							e.IsSelected = false;
						}
					}
				}
				else
				{
					ComponentIcon ci = c as ComponentIcon;
					if (ci != null)
					{
						ci.IsSelected = false;
					}
				}
			}
			if (_selectedEventIcon != null)
			{
				this.ClearLineSelection();
				_eventPathHolder.OnEventIconSelection(_selectedEventIcon);
			}
		}
		public void RefreshKeepScroll()
		{
			ActiveDrawing.RefreshControl(this.Parent);
			isFrameOn = false;
		}
		private bool isDrag = false;
		private Rectangle theRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));
		private Point startPoint = Point.Empty;
		private bool isFrameOn = false;
		private bool isFrameMoving = false;
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (!isFrameMoving)
			{
				if (e.Button == MouseButtons.Left)
				{
					if (isFrameOn)
					{
						ControlPaint.DrawReversibleFrame(theRectangle,
							this.BackColor, FrameStyle.Dashed);
						isFrameOn = false;
					}
					isDrag = true;
					startPoint = this.PointToScreen(new Point(e.X, e.Y));
				}
				base.OnMouseDown(e);
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (isFrameOn)
			{
				ControlPaint.DrawReversibleFrame(theRectangle,
					this.BackColor, FrameStyle.Dashed);
				isFrameOn = false;
			}
			if (!isFrameMoving)
			{
				if (e.Button == System.Windows.Forms.MouseButtons.Left)
				{
					if (isDrag)
					{
						// Calculate the endpoint and dimensions for the new  
						// rectangle, again using the PointToScreen method.
						Point endPoint = this.PointToScreen(new Point(e.X, e.Y));
						int width = endPoint.X - startPoint.X;
						int height = endPoint.Y - startPoint.Y;
						theRectangle = new Rectangle(startPoint.X, startPoint.Y, width, height);
						// Draw the new rectangle by calling DrawReversibleFrame 
						// again.  
						ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
						isFrameOn = true;
					}
				}
				else
				{
					base.OnMouseMove(e);
				}
			}
		}
		private LabelBox[] _dragBoxes = null;
		private List<ComponentIconEvent> _selectedIcons = null;
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (isFrameMoving)
			{
				if (_dragBoxes != null)
				{
					for (int i = 0; i < _dragBoxes.Length; i++)
					{
						_dragBoxes[i].Visible = false;
					}
				}
				isFrameMoving = false;
			}
			else
			{
				if (isDrag)
				{
					if (isFrameOn)
					{
						ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
						isFrameOn = false;
						//collect selected elements
						if (theRectangle.Height < 0)
						{
							if (theRectangle.Width < 0)
							{
								int x = theRectangle.X + theRectangle.Width;
								int y = theRectangle.Y + theRectangle.Height;
								theRectangle = new Rectangle(x, y, -theRectangle.Width, -theRectangle.Height);
							}
							else
							{
								theRectangle = new Rectangle(theRectangle.X, theRectangle.Y + theRectangle.Height, theRectangle.Width, -theRectangle.Height);
							}
						}
						else
						{
							if (theRectangle.Width < 0)
							{
								theRectangle = new Rectangle(theRectangle.X + theRectangle.Width, theRectangle.Y, -theRectangle.Width, theRectangle.Height);
							}
						}
						Point p = this.PointToClient(theRectangle.Location);
						Rectangle rt = this.RectangleToClient(theRectangle);
						_selectedIcons = new List<ComponentIconEvent>();
						foreach (Control c in this.Controls)
						{
							if (rt.Contains(c.Location))
							{
								ComponentIconEvent ad = c as ComponentIconEvent;
								if (ad != null)
								{
									_selectedIcons.Add(ad);
								}
							}
						}
						if (_selectedIcons.Count > 0)
						{
							if (_dragBoxes == null)
							{
								_dragBoxes = new LabelBox[4];
								for (int i = 0; i < _dragBoxes.Length; i++)
								{
									_dragBoxes[i] = new LabelBox(this);
									this.Controls.Add(_dragBoxes[i]);
								}
							}
							_dragBoxes[0].Location = p;
							_dragBoxes[1].Location = new Point(p.X + theRectangle.Width, p.Y);
							_dragBoxes[2].Location = new Point(p.X + theRectangle.Width, p.Y + theRectangle.Height);
							_dragBoxes[3].Location = new Point(p.X, p.Y + theRectangle.Height);
							for (int i = 0; i < _dragBoxes.Length; i++)
							{
								_dragBoxes[i].Visible = true;
							}
							isFrameMoving = true;
						}
					}
				}
			}
			base.OnMouseUp(e);
			if (isDrag)
			{
				isDrag = false;
			}
		}
		public void OnDraggedBox(int dx, int dy)
		{
			if (_selectedIcons != null)
			{
				foreach (ActiveDrawing ad in _selectedIcons)
				{
					ad.Location = new Point(ad.Location.X + dx, ad.Location.Y + dy);
				}
			}
			RefreshKeepScroll();
		}
		public void OnDragBox(int dx, int dy)
		{
			if (_dragBoxes != null)
			{
				for (int i = 0; i < _dragBoxes.Length; i++)
				{
					_dragBoxes[i].Location = new Point(_dragBoxes[i].Location.X + dx, _dragBoxes[i].Location.Y + dy);
				}
			}
		}
		#endregion

		#region private methods

		private void mi_removeOutPortActionsfromEvent(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				EventPortOut po = mi.Tag as EventPortOut;
				if (po != null)
				{
					List<TaskID> acts = po.Actions;
					if (acts != null)
					{
						bool bDel = true;
						if (acts.Count > 0)
						{
							HandlerMethodID hid = acts[0] as HandlerMethodID;
							if (hid != null)
							{
								if (MessageBox.Show(this.FindForm(), "Do you want to delete the event handler method?", "Delete Event Handler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
								{
									bDel = false;
								}
							}
						}
						if (bDel)
						{
							_holder.RemoveEventHandlers(po.Event, acts);
						}
					}
				}
			}
		}
		private void mi_newServiceMethod(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createMethod(EnumMethodStyle.RemotingService, EnumMethodWebUsage.Server, p);
		}
		private void mi_newWebMethod(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createMethod(EnumMethodStyle.WebService, EnumMethodWebUsage.Server, p);
		}
		private void mi_newMethod(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createMethod(EnumMethodStyle.Normal, EnumMethodWebUsage.Server, p);
		}
		private void mi_newClientMethod(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createMethod(EnumMethodStyle.Normal, EnumMethodWebUsage.Client, p);
		}
		private MethodClass createMethod(EnumMethodStyle style, EnumMethodWebUsage webUsage, Point p)
		{
			ClassPointer root = _loader.GetRootId();
			Rectangle rc = this.RectangleToScreen(new Rectangle(p, new Size(30, 30)));
			bool isStatic = _loader.GetRootId().IsStatic;
			MethodClass method = root.CreateNewMethod(isStatic, style, webUsage, rc, this.FindForm());
			if (method != null)
			{
				foreach (Control c in Controls)
				{
					ComponentIconMethod cim = c as ComponentIconMethod;
					if (cim != null)
					{
						if (cim.MethodId == method.MethodID)
						{
							cim.Location = p;
							break;
						}
					}
				}
			}
			return method;
		}
		private void mi_newPropertyWebClient(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createProperty(p, EnumWebRunAt.Client);
		}
		private void mi_newPropertyWebServer(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createProperty(p, EnumWebRunAt.Server);
		}
		private void mi_newProperty(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			createProperty(p, EnumWebRunAt.Inherit);
		}
		private void createProperty(Point p, EnumWebRunAt runAt)
		{
			ClassPointer root = _loader.GetRootId();
			DataTypePointer dtp;
			if (root.Project.IsWebApplication)
			{
				if (runAt == EnumWebRunAt.Client)
				{
					dtp = new DataTypePointer(new TypePointer(typeof(JsString)));
				}
				else
				{
					if (runAt == EnumWebRunAt.Server)
					{
						if (root.Project.ProjectType == EnumProjectType.WebAppPhp)
						{
							dtp = new DataTypePointer(new TypePointer(typeof(PhpString)));
						}
						else
						{
							dtp = new DataTypePointer(new TypePointer(typeof(string)));
						}
					}
					else
					{
						dtp = new DataTypePointer(new TypePointer(typeof(string)));
					}
				}
			}
			else
			{
				dtp = new DataTypePointer(new TypePointer(typeof(string)));
			}
			PropertyClass prop = root.CreateNewProperty(
					dtp,
					false,
					_holder.Loader.CreateNewComponentName("Property", null),
					false,
					true,
					true,
					false, runAt);
			if (prop != null)
			{
				ComponentIconProperty ic = getPropertyComponentIcon(prop.MemberId);
				if (ic != null)
				{
					ic.Location = p;
					ic.SaveLocation();
					SetIconSelection(ic);
					Panes.OnAddProperty(prop);
					_holder.Loader.NotifySelection(prop);
				}
			}
			//
		}
		private void mi_newWebClientEvent(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			ClassPointer root = _loader.GetRootId();
			ILimnorDesignerLoader loader = _loader as ILimnorDesignerLoader;
			DlgMakeTypeList dlg = new DlgMakeTypeList();
			dlg.LoadData("Select event parameters", "Parameter name", root.Project, null);
			if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
			{
				CustomEventHandlerType ceht = new CustomEventHandlerType();
				ceht.Parameters = dlg.Results;
				EventClass ec = root.CreateNewEvent(new DataTypePointer(ceht), loader.CreateNewComponentName("Event", null), false);
				ComponentIconEvent ci = _data.GetRootComponentIcon();
				if (ci != null)
				{
					EventIcon ei = ci.GetEventIcon(ec);
					if (ei != null)
					{
						ei.Location = p;
						ei.SaveLocation();
					}
				}
			}
		}
		private void mi_newEvent(object sender, EventArgs e)
		{
			MenuItem mi = (MenuItem)sender;
			Point p = (Point)(mi.Tag);
			ClassPointer root = _loader.GetRootId();
			EventClass ec = root.CreateNewEvent(new DataTypePointer(new TypePointer(typeof(EventHandler))), _holder.Loader.CreateNewComponentName("Event", null), false);
			ComponentIconEvent ci = _data.GetRootComponentIcon();
			if (ci != null)
			{
				EventIcon ei = ci.GetEventIcon(ec);
				if (ei != null)
				{
					ei.Location = p;
					ei.SaveLocation();
				}
			}
		}
		private void mi_moveUnlinkedIcons(object sender, EventArgs e)
		{
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				MenuItem mi = (MenuItem)sender;
				Point p = (Point)(mi.Tag);
				int x0 = 30;
				int y0 = p.Y;
				int x = x0;
				int y = y0;
				int dx = 20;
				int dy = 20;
				foreach (ComponentIconEvent cie in icons)
				{
					if (cie.IsForComponent)
					{
						if (!cie.IsLinked)
						{
							cie.Location = new Point(x, y);
							x += dx;
							if (x >= this.ClientSize.Width - 30)
							{
								x = x0;
								y += dy;
							}
						}
					}
				}
			}
		}
		private void mi_moveIcon(object sender, EventArgs e)
		{
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				MenuItem mi = (MenuItem)sender;
				Point p = (Point)(mi.Tag);
				DialogSelectComponentIcon dlg = new DialogSelectComponentIcon();
				dlg.LoadData(icons);
				if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
				{
					if (dlg.SelectedIcon.Parent == null)
					{
						this.Controls.Add(dlg.SelectedIcon);
					}
					dlg.SelectedIcon.Location = p;
					RefreshKeepScroll();
				}
			}
		}
		private ComponentIconProperty getPropertyComponentIcon(UInt32 propertyId)
		{
			foreach (Control c0 in Controls)
			{
				ComponentIconProperty ic = c0 as ComponentIconProperty;
				if (ic != null)
				{
					if (ic.PropertyId == propertyId)
					{
						return ic;
					}
				}
			}
			return null;
		}
		public ComponentIconMethod getMethodComponentIcon(UInt32 methodId)
		{
			foreach (Control c0 in Controls)
			{
				ComponentIconMethod ic = c0 as ComponentIconMethod;
				if (ic != null)
				{
					if (ic.MethodId == methodId)
					{
						return ic;
					}
				}
			}
			return null;
		}
		void ln_Move(object sender, EventArgs e)
		{
			LinkLineNode ln = sender as LinkLineNode;
			if (ln != null)
			{
				ln.SaveLocation();
			}
			NotifyCurrentChanges();
		}
		#endregion

		#region Properties
		protected override bool AllowLineDisconnect { get { return false; } }
		protected override bool AllowLineSelection
		{
			get
			{
				return true;
			}
		}
		public EventIcon SelectedEvent
		{
			get
			{
				return _selectedEventIcon;
			}
		}
		public bool Loaded
		{
			get
			{
				return (_holder != null && _holder.Loaded);
			}
		}
		public MultiPanes Panes
		{
			get
			{
				return _holder;
			}
		}
		public ClassPointer RootPointer
		{
			get
			{
				if (_loader != null)
					return _loader.GetRootId();
				return null;
			}
		}
		public ILimnorDesigner Loader
		{
			get
			{
				return _loader;
			}
		}
		public UInt32 ClassId
		{
			get
			{
				if (_holder != null)
				{
					return _holder.Loader.ClassID;
				}
				if (_loader != null)
				{
					return _loader.GetRootId().ClassId;
				}
				return 0;
			}
		}
		public XmlNode XmlData
		{
			get
			{
				ILimnorLoader l = _loader as ILimnorLoader;
				if (l != null)
				{
					XmlNode node = l.Node.SelectSingleNode(XML_EventPath);
					if (node == null)
					{
						if (_holder != null && _holder.Loaded)
						{
							node = l.Node.OwnerDocument.CreateElement(XML_EventPath);
							l.Node.AppendChild(node);
						}
					}
					return node;
				}
				return null;
			}
		}
		protected override IUndoHost UndoHost
		{
			get { return null; }
		}
		#endregion

		#region IXDesignerViewer Members
		public void OnClosing()
		{
		}
		public void OnRemoveAllEventHandlers(IEvent e)
		{
			if (_selectedEventIcon != null)
			{
				if (e.IsSameObjectRef(_selectedEventIcon.Event))
				{
					_selectedEventIcon = null;
				}
			}
			bool b = false;
			List<ComponentIconEvent> cs = _data.ComponentIconList;
			foreach (ComponentIconEvent cie in cs)
			{
				if (cie.IsForComponent)
				{
					List<EventIcon> eis = cie.EventIcons;
					if (eis != null && eis.Count > 0)
					{
						foreach (EventIcon ei in eis)
						{
							if (e.IsSameObjectRef(ei.Event))
							{
								eis.Remove(ei);
								List<Control> ctrls = ei.GetRelatedControls();
								foreach (Control c in ctrls)
								{
									Controls.Remove(c);
								}
								b = true;
								break;
							}
						}
						if (b)
						{
							break;
						}
					}
				}
			}
			if (b)
			{
				NotifyChanges();
				RefreshKeepScroll();
			}
		}
		public void SetDesigner(MultiPanes mp)
		{
			_holder = mp;
		}
		public void SetDesignerEx(MultiPanes mp, EventPathHolder pathHolder)
		{
			_holder = mp;
			Init(mp.Loader, pathHolder);
		}

		public Control GetDesignSurfaceUI()
		{
			return this;
		}

		public void OnDataLoaded()
		{
			ILimnorLoader l = (ILimnorLoader)_loader;
			_data = new EventPathData();
			XmlNode node = l.Node.SelectSingleNode(XML_EventPath);
			if (node != null)
			{
				l.Reader.ResetErrors();
				l.Reader.ReadObjectFromXmlNode(node, _data, _data.GetType(), null);
				if (l.Reader.HasErrors)
				{
					MathNode.Log(l.Reader.Errors);
				}
			}
			_data.OnLoadData(this);
		}
		public void OnUIConfigLoaded()
		{
		}
		public EnumMaxButtonLocation MaxButtonLocation
		{
			get { return EnumMaxButtonLocation.Right; }
		}
		public void OnClassLoadedIntoDesigner(UInt32 classId)
		{
		}
		public void OnDefinitionChanged(UInt32 classId, object relatedObject, EnumClassChangeType changeMade)
		{
		}
		public void OnComponentAdded(object obj)
		{
			IClass c = DesignUtil.CreateObjectPointer(_loader.ObjectMap, obj);
			if (c != null)
			{
				bool bFound = false;
				foreach (Control c0 in Controls)
				{
					ComponentIconEvent ic = c0 as ComponentIconEvent;
					if (ic != null && ic.IsForComponent)
					{
						if (ic.DefinitionClassId == this.ClassId && ic.MemberId == c.MemberId)
						{
							bFound = true;
							break;
						}
					}
				}
				if (!bFound)
				{
					ComponentIconEvent cip = new ComponentIconEvent();
					cip.Init(_loader, c);
					double x, y;
					ComponentIconEvent.CreateRandomPoint(80, out x, out y);
					cip.Location = new Point(80 + (int)x, 80 + (int)y);
					Controls.Add(cip);
					cip.BringToFront();
					cip.RefreshLabelPosition();
					//validate the input/output ports of the icon
					cip.Initialize(this._data);
					_data.ComponentIconList.Add(cip);
					//
					//save it
					NotifyChanges();
				}
			}
		}

		public void OnComponentRename(object obj, string newName)
		{
			HtmlElement_ItemBase hei = obj as HtmlElement_ItemBase;
			if (hei != null)
			{
				int n = 0;
				foreach (Control c0 in Controls)
				{
					ComponentIconHtmlElement cihe = c0 as ComponentIconHtmlElement;
					if (cihe != null)
					{
						if (cihe.HtmlElement.ElementGuid == hei.ElementGuid)
						{
							cihe.SetLabelText(newName);
							n++;
							if (n > 1)
							{
								break;
							}
						}
					}
				}
			}
			else
			{
				UInt32 memberId = _loader.ObjectMap.GetObjectID(obj);
				if (memberId != 0)
				{
					foreach (Control c0 in Controls)
					{
						ComponentIconEvent ic = c0 as ComponentIconEvent;
						if (ic != null && ic.IsForComponent)
						{
							if (ic.MemberId == memberId)
							{
								ic.SetLabelText(newName);
							}
						}
					}
				}
			}
		}

		public void OnComponentRemoved(object obj)
		{
			UInt32 memberId = _loader.ObjectMap.GetObjectID(obj);
			if (memberId != 0)
			{
				foreach (Control c0 in Controls)
				{
					ComponentIconEvent ic = c0 as ComponentIconEvent;
					if (ic != null && ic.IsForComponent)
					{
						if (ic.MemberId == memberId)
						{
							ic.RemoveIcon();
							if (_data.ComponentIconList.Contains(ic))
							{
								_data.ComponentIconList.Remove(ic);
							}
							NotifyChanges();
							RefreshKeepScroll();
							break;
						}
					}
				}
			}
		}

		public void OnComponentSelected(object obj)
		{
			List<ComponentIconEvent> list = _data.ComponentIconList;
			if (list != null)
			{
				bool found = false;
				HtmlElement_Base he = obj as HtmlElement_Base;
				if (he == null)
				{
					obj = VPLUtil.GetObject(obj);
					foreach (ComponentIconEvent cie in list)
					{
						if (cie.IsForComponent)
						{
							object v = cie.ClassPointer.ObjectInstance;
							ClassInstancePointer cip = cie.ClassPointer.ObjectInstance as ClassInstancePointer;
							if (cip != null)
							{
								v = VPLUtil.GetObject(cip.ObjectInstance);
							}
							if (v == obj)
							{
								SetIconSelection(cie);
								found = true;
								break;
							}
						}
					}
					if (!found)
					{
					}
				}
				else
				{
					ComponentIconHtmlElement heIcon = null;
					ComponentIconHtmlElementCurrent currentHtmlIcon = null;
					foreach (ComponentIconEvent cie in list)
					{
						if (cie.IsForComponent)
						{
							currentHtmlIcon = cie as ComponentIconHtmlElementCurrent;
							if (currentHtmlIcon != null)
							{
								currentHtmlIcon.OnSelectHtmlElement(he);
								if (he.ElementGuid == Guid.Empty || heIcon != null)
								{
									break;
								}
							}
							else
							{
								if (he.ElementGuid != Guid.Empty)
								{
									ComponentIconHtmlElement he0 = cie as ComponentIconHtmlElement;
									if (he0 != null)
									{
										if (he0.HtmlElement.ElementGuid == he.ElementGuid)
										{
											heIcon = he0;
											if (currentHtmlIcon != null)
											{
												break;
											}
										}
									}
								}
							}
						}
					}
					if (heIcon != null)
					{
						SetIconSelection(heIcon);
					}
					else if (currentHtmlIcon != null)
					{
						SetIconSelection(currentHtmlIcon);
					}
				}
			}
		}

		public void OnActionSelected(IAction action)
		{
		}

		public void OnAssignAction(EventAction ea)
		{
			ClassPointer root = _loader.GetRootId();
			ComponentIconEvent eventIcon = null;
			List<EventPortOut> newOutPorts = null;
			PortCollection pc = new PortCollection();
			//
			Dictionary<string, List<EventAction>> eaGroup = EventPathData.CreateHandlerGroup(root.EventHandlers);
			//add missing event handler method icons
			foreach (TaskID tid in ea.TaskIDList)
			{
				if (tid.Action == null)
				{
					tid.LoadActionInstance(root);
				}
				HandlerMethodID hid = tid as HandlerMethodID;
				if (hid != null)
				{
					bool bFound = false;
					foreach (ComponentIconEvent cie in _data.ComponentIconList)
					{
						ComponentIconMethod cim = cie as ComponentIconMethod;
						if (cim != null)
						{
							if (cim.MethodId == hid.ActionId)
							{
								bFound = true;
								break;
							}
						}
					}
					if (!bFound)
					{
						ComponentIconEventhandle cip = new ComponentIconEventhandle();
						cip.Init(Loader, hid.HandlerMethod.RootPointer);
						cip.Method = hid.HandlerMethod;
						cip.MethodId = hid.HandlerMethod.MethodID;
						double x, y;
						ComponentIconEvent.CreateRandomPoint(80, out x, out y);
						cip.Location = new Point(80 + (int)x, 80 + (int)y);
						Controls.Add(cip);
						cip.BringToFront();
						cip.RefreshLabelPosition();
						//validate the input/output ports of the icon
						cip.Initialize(this._data);
						pc.AddRange(cip.DestinationPorts.ToArray());
						_data.ComponentIconList.Add(cip);
					}
				}
			}
			//find event firer and action executer
			if (!(ea.Event.Holder is LocalVariable))
			{
				IClass c = EventPathData.GetEventFirerRef(ea.Event.Holder);
				foreach (ComponentIconEvent ic in _data.ComponentIconList)
				{
					if (eventIcon == null)
					{
						if (ic.IsForComponent && ic.IsPortOwner)
						{
							if (ic.DefinitionClassId == c.DefinitionClassId && ic.ClassPointer.MemberId == c.MemberId)
							{
								eventIcon = ic;
								//find/add EventIcon
								List<EventAction> eas;
								if (eaGroup.TryGetValue(ea.Event.ObjectKey, out eas))
								{
									EventIcon ei = ic.ValidateEventFirer(eas, _data);
									if (!Controls.Contains(ei))
									{
										//ei is newly created
										Controls.Add(ei);
										ei.AdjustPosition();
									}
									newOutPorts = ei.Initialize();
									foreach (EventPortOut po in newOutPorts)
									{
										pc.Add(po);
									}
								}
								else
								{
									throw new DesignerException("Event handler for event [{0}] does not in class [{1}]", ea.Event, root);
								}
							}
						}
					}
					EventPortIn newInports0 = ic.ValidateActionExecuter(ea, _loader.GetRootId());
					if (newInports0 != null)
					{
						pc.Add(newInports0);
					}
				}
			}
			EventPathData.MakePortLinks(pc);
			Controls.AddRange(pc.GetAllControls(false).ToArray());
			foreach (LinkLineNodePort p in pc)
			{
				p.RestoreLocation();
				p.SetLoaded();
			}
			//link ports
			if (newOutPorts != null)
			{
				EventPortOut.LinkPorts(newOutPorts, _data.ComponentIconList);
				pc.MakeLinks(this.FindForm());
				EventPathData.SetDynamicEventHandlerLineColor(pc);
				pc.CreateLinkLines();
				RefreshKeepScroll();
			}
			NotifyChanges();
		}

		public void OnRemoveEventHandler(EventAction ea, TaskID task)
		{
			//find the event firer
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null)
			{
				IClass c = EventPathData.GetEventFirerRef(ea.Event.Holder);
				foreach (ComponentIconEvent ic in icons)
				{
					if (ic.IsForComponent) //only components fire events
					{
						if (ic.DefinitionClassId == c.DefinitionClassId && ic.ClassPointer.MemberId == c.MemberId)
						{
							//ic is the event firer
							List<EventIcon> es = ic.EventIcons;
							if (es != null && es.Count > 0)
							{
								foreach (EventIcon ei in es)
								{
									//find the event
									if (ei.Event.IsSameObjectRef(ea.Event))
									{
										List<EventPortOut> ps = ei.SourcePorts;
										foreach (EventPortOut po in ps)
										{
											//find the out port
											if (po.CanActionBeLinked(task))
											{
												po.RemoveAction(task);
												if (po.ActionCount == 0)
												{
													PortCollection pc = new PortCollection();
													pc.Add(po);
													pc.Add((LinkLineNodePort)(po.End));
													//
													//find the action executer
													foreach (Control c1 in Controls)
													{
														ComponentIconEvent ic1 = c1 as ComponentIconEvent;
														if (ic1 != null)
														{
															List<EventPortIn> ins = ic1.DestinationPorts;
															if (ins != null && ins.Count > 0)
															{
																bool b = false;
																foreach (EventPortIn pi in ins)
																{
																	if (pi == po.End)
																	{
																		ins.Remove(pi);
																		b = true;
																		break;
																	}
																}
																if (b)
																{
																	break;
																}
															}
														}
													}
													List<Control> cs = pc.GetAllControls(false);
													if (ps.Count == 1)
													{
														cs.Add(ei);
														cs.Add(ei.Label);
														es.Remove(ei);
													}
													ps.Remove(po);
													foreach (Control ct in cs)
													{
														Controls.Remove(ct);
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
			}
			NotifyChanges();
			RefreshKeepScroll();
		}
		/// <summary>
		/// not needed, handled by EventPathHolder
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ea"></param>
		public void OnActionListOrderChanged(object sender, EventAction ea)
		{

		}

		public void OnActionChanged(uint classId, IAction act, bool isNewAction)
		{
			if (act.ClassId == this.ClassId)
			{
				FireEventMethod fe = act.ActionMethod as FireEventMethod;
				if (fe != null)
				{
					ComponentIconEvent cie = GetRootComponentIcon();
					if (cie != null)
					{
						EventIcon ei = cie.CheckCreateEventIcon(fe.Event.Event);
						List<EventPortIn> ports = ei.DestinationPorts;
						EventPortInFireEvent epf = null;
						if (ports.Count > 0)
						{
							foreach (EventPortIn epi in ports)
							{
								EventPortInFireEvent epif = epi as EventPortInFireEvent;
								if (epif != null)
								{
									if (epif.FireEventMethodId == fe.MemberId)
									{
										epf = epif;
										break;
									}
								}
							}
						}
						if (epf == null)
						{
							epf = new EventPortInFireEvent(ei);
							epf.FireEventMethod = fe;
							Controls.Add(epf);
							ei.AddInPort(epf);

						}
						ComponentIconFireEvent cife = null;
						foreach (Control c in Controls)
						{
							ComponentIconFireEvent c0 = c as ComponentIconFireEvent;
							if (c0 != null)
							{
								if (c0.EventId == fe.EventId && c0.FirerId == fe.MemberId)
								{
									cife = c0;
									break;
								}
							}
						}
						if (cife == null)
						{
							cife = new ComponentIconFireEvent();
							cife.ClassPointer = cie.ClassPointer;
							cife.Firer = fe;
							cife.SetDesigner(_loader);
							double x, y;
							ComponentIconEvent.CreateRandomPoint(80, out x, out y);
							cife.Location = new Point(80 + (int)x, 80 + (int)y);
							Controls.Add(cife);
							Controls.Add(cife.FirerPort);
							_data.ComponentIconList.Add(cife);
						}
						epf.LinkedPortID = cife.FirerPort.PortID;
						epf.LinkedPortInstanceID = cife.FirerPort.PortInstanceID;
						cife.FirerPort.LinkedPortID = epf.PortID;
						cife.FirerPort.LinkedPortInstanceID = epf.PortInstanceID;
						PortCollection pc = new PortCollection();
						pc.Add(epf);
						pc.Add(cife.FirerPort);

						pc.MakeLinks(TraceLogClass.MainForm);
						pc.CreateLinkLines();
						cife.FirerPort.SetLoaded();
						epf.SetLoaded();
						RefreshKeepScroll();
						NotifyChanges();
					}
				}
				else
				{
					ClassPointer cp = RootPointer.GetExternalExecuterClass(act);
					if (cp != null)
					{
						ComponentIconClass cic = null;
						foreach (Control c in Controls)
						{
							ComponentIconClass c0 = c as ComponentIconClass;
							if (c0 != null)
							{
								if (c0.ClassId == cp.ClassId)
								{
									cic = c0;
									break;
								}
							}
						}
						if (cic == null)
						{
							cic = new ComponentIconClass();
							cic.Init(_loader, cp);
							double x, y;
							ComponentIconEvent.CreateRandomPoint(Math.Min(this.Width, this.Height) / 3, out x, out y);
							cic.Location = new Point(this.Width / 3 + (int)x, this.Height / 3 + (int)y);
							Controls.Add(cic);
							cic.Visible = true;
							_data.ComponentIconList.Add(cic);
						}
					}
					else if(act != null && act.ActionMethod != null)
					{
						DataTypePointer tp = act.ActionMethod.Owner as DataTypePointer;
						if (tp != null)
						{
							ComponentIconClassType cict = null;
							foreach (Control c in Controls)
							{
								ComponentIconClassType ct0 = c as ComponentIconClassType;
								if (ct0 != null)
								{
									if (ct0.ClassType.Equals(tp.BaseClassType))
									{
										cict = ct0;
										break;
									}
								}
							}
							if (cict == null)
							{
								cict = new ComponentIconClassType();
								cict.Init(_loader, tp);
								double x, y;
								ComponentIconEvent.CreateRandomPoint(Math.Min(this.Width, this.Height) / 3, out x, out y);
								cict.Location = new Point(this.Width / 3 + (int)x, this.Height / 3 + (int)y);
								Controls.Add(cict);
								cict.Visible = true;
								_data.ComponentIconList.Add(cict);
							}
						}
					}
				}
			}
		}
		public void OnResetDesigner(object obj)
		{
		}
		public void OnHtmlElementSelected(HtmlElement_Base element)
		{
			//if (element is HtmlElement_BodyBase)
			//{
			OnComponentSelected(element);
			//}
		}
		public void OnSelectedHtmlElement(Guid guid, object selector)
		{
			if (selector != this)
			{
				if (guid == Guid.Empty)
				{
					foreach (Control c in this.Controls)
					{
						ComponentIconHtmlElementCurrent ce = c as ComponentIconHtmlElementCurrent;
						if (ce != null)
						{
							SetIconSelection(ce);
							break;
						}
					}
				}
				else
				{
					ClassPointer root = RootPointer;
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
			bool found = false;
			foreach (Control c in this.Controls)
			{
				ComponentIconHtmlElement ce = c as ComponentIconHtmlElement;
				if (ce != null && ce.IsPortOwner)
				{
					if (ce.HtmlElement.ElementGuid == element.ElementGuid)
					{
						found = true;
						break;
					}
				}
			}
			if (!found)
			{
				ComponentIconHtmlElement ce = new ComponentIconHtmlElement();
				ce.Init(_loader, element);
				ce.Location = new Point(30, 30);
				ce.Initialize(_data);
				PortCollection pc = new PortCollection();
				pc.AddRange(ce.GetAllPorts());
				this.Controls.Add(ce);
				List<Control> cs = pc.GetAllControls(false);
				this.Controls.AddRange(cs.ToArray());
				_data.ComponentIconList.Add(ce);
			}
		}
		public void OnMethodChanged(MethodClass method, bool isNewMethod)
		{
			if (method.ClassId != this.ClassId)
				return;
			if (isNewMethod)
			{
				ComponentIconMethod cip = new ComponentIconMethod();
				cip.Init(Loader, method.RootPointer);
				cip.Method = method;
				cip.MethodId = method.MethodID;
				double x, y;
				ComponentIconEvent.CreateRandomPoint(80, out x, out y);
				cip.Location = new Point(80 + (int)x, 80 + (int)y);
				Controls.Add(cip);
				cip.BringToFront();
				cip.RefreshLabelPosition();
				//validate the input/output ports of the icon
				cip.Initialize(this._data);
				_data.ComponentIconList.Add(cip);
				NotifyChanges();
			}
			else
			{
				foreach (Control c in Controls)
				{
					ComponentIconMethod cip = c as ComponentIconMethod;
					if (cip != null)
					{
						if (cip.MethodId == method.MethodID)
						{
							cip.SetLabelText(method.MethodName);
							cip.RefreshIcon();
							break;
						}
					}
				}
			}

		}
		public void OnMethodSelected(MethodClass method)
		{
			ComponentIconMethod cip = getMethodComponentIcon(method.MethodID);
			if (cip != null)
			{
				SetIconSelection(cip);
			}
		}
		public void OnActionDeleted(IAction action)
		{
			ClassPointer root = _loader.GetRootId();
			List<ComponentIconEvent> icons = _data.ComponentIconList;
			if (icons != null && icons.Count > 0)
			{
				ComponentIconFireEvent firer = null;
				FireEventMethod fe = action.ActionMethod as FireEventMethod;
				foreach (ComponentIconEvent cie in icons)
				{
					if (fe != null)
					{
						if (firer == null)
						{
							ComponentIconFireEvent f0 = cie as ComponentIconFireEvent;
							if (f0 != null)
							{
								if (f0.EventId == fe.EventId && f0.FirerId == fe.MemberId)
								{
									firer = f0;
								}
							}
						}
					}
					List<EventIcon> events = cie.EventIcons;
					if (events != null && events.Count > 0)
					{
						foreach (EventIcon ei in events)
						{
							List<EventPortOut> ports = ei.SourcePorts;
							if (ports != null && ports.Count > 0)
							{
								foreach (EventPortOut po in ports)
								{
									List<TaskID> tids = po.Actions;
									if (tids != null && tids.Count > 0)
									{
										List<TaskID> dels = new List<TaskID>();
										foreach (TaskID tid in tids)
										{
											if (!tid.IsEmbedded)
											{
												if (tid.ActionId == action.ActionId && tid.ClassId == root.ClassId)
												{
													dels.Add(tid);
												}
											}
										}
										if (dels.Count > 0)
										{
											foreach (TaskID tid in dels)
											{
												tids.Remove(tid);
											}
										}
									}
								}
							}
						}
					}
				}
				if (firer != null)
				{
					icons.Remove(firer);
					firer.RemoveIcon();
				}
				NotifyChanges();
				RefreshKeepScroll();
			}
		}
		public void OnDeleteMethod(MethodClass method)
		{
			foreach (Control c0 in Controls)
			{
				ComponentIconMethod ic = c0 as ComponentIconMethod;
				if (ic != null)
				{
					if (ic.DefinitionClassId == method.ClassId && ic.MethodId == method.MethodID)
					{
						ic.RemoveIcon();
						if (_data.ComponentIconList.Contains(ic))
						{
							_data.ComponentIconList.Remove(ic);
						}
						NotifyChanges();
						RefreshKeepScroll();
						break;
					}
				}
			}
		}

		public void OnDeleteEventMethod(EventHandlerMethod method)
		{
			foreach (Control c0 in Controls)
			{
				ComponentIconEventhandle ic = c0 as ComponentIconEventhandle;
				if (ic != null)
				{
					if (ic.DefinitionClassId == method.ClassId && ic.MethodId == method.MethodID)
					{
						ic.RemoveIcon();
						if (_data.ComponentIconList.Contains(ic))
						{
							_data.ComponentIconList.Remove(ic);
						}
						NotifyChanges();
						RefreshKeepScroll();
						break;
					}
				}
			}
		}

		public void OnDeleteProperty(PropertyClass property)
		{
			if (property.ClassId == ClassId)
			{
				ComponentIconProperty ic = getPropertyComponentIcon(property.MemberId);
				if (ic != null)
				{
					ic.RemoveIcon();
					if (_data.ComponentIconList.Contains(ic))
					{
						_data.ComponentIconList.Remove(ic);
					}
					NotifyChanges();
					RefreshKeepScroll();
				}
			}
		}

		public void OnAddProperty(PropertyClass property)
		{
			if (property.ClassId == ClassId)
			{
				ComponentIconProperty cip = getPropertyComponentIcon(property.MemberId);
				if (cip == null)
				{
					ClassPointer root = _loader.GetRootId();
					cip = new ComponentIconProperty();
					cip.Init(Loader, root);
					cip.Property = property;
					cip.PropertyId = property.MemberId;
					double x, y;
					ComponentIconEvent.CreateRandomPoint(80, out x, out y);
					cip.Location = new Point(80 + (int)x, 80 + (int)y);
					Controls.Add(cip);
					cip.BringToFront();
					cip.RefreshLabelPosition();
					//validate the input/output ports of the icon
					cip.Initialize(this._data);
					_data.ComponentIconList.Add(cip);
					NotifyChanges();
				}
			}
		}
		public void OnPropertySelected(PropertyClass property)
		{
			ComponentIconProperty cip = getPropertyComponentIcon(property.MemberId);
			if (cip != null)
			{
				SetIconSelection(cip);
			}
		}
		public void OnPropertyChanged(INonHostedObject property, string name)
		{
		}

		public void OnDeleteEvent(EventClass eventObject)
		{
			if (eventObject.ClassId == this.ClassId)
			{
				ComponentIconEvent ci = _data.GetRootComponentIcon();
				if (ci != null)
				{
					EventIcon ei = ci.GetEventIcon(eventObject);
					if (ei != null)
					{
						List<Control> cs = ei.GetRelatedControls();
						foreach (Control c in cs)
						{
							Control p = c.Parent;
							if (p != null)
							{
								p.Controls.Remove(c);
							}
						}
						ci.EventIcons.Remove(ei);
					}
				}
				//remove event node
				XmlNode node = XmlData;
				if (node != null)
				{
					//EventPath/Property/Item/Events/Item/Event[eventId={id}]
					XmlNode evNode = node.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{0}/{1}/{2}/{1}/{3}[@{4}='{5}']",
						XmlTags.XML_PROPERTY, XmlTags.XML_Item, ComponentIconEvent.XML_EVENTS, EventIcon.XML_Event, XmlTags.XMLATT_EventId, eventObject.MemberId));
					if (evNode != null)
					{
						XmlNode itemNode = evNode.ParentNode;
						XmlNode esNode = itemNode.ParentNode;
						esNode.RemoveChild(itemNode);
						NotifyChanges();
					}
				}

			}
		}

		public void OnAddEvent(EventClass eventObject)
		{
			if (eventObject.ClassId == this.ClassId)
			{
				ComponentIconEvent ci = _data.GetRootComponentIcon();
				if (ci != null)
				{
					ci.CheckCreateEventIcon(eventObject);
					NotifyChanges();
				}
			}
		}
		public void OnEventSelected(EventClass eventObject)
		{
			ComponentIconEvent cie = GetRootComponentIcon();
			if (cie != null)
			{
				EventIcon ei = cie.GetEventIcon(eventObject);
				if (ei != null)
				{
					SelectEventIcon(ei);
				}
			}
		}
		public void OnFireEventActionSelected(FireEventMethod method)
		{
			foreach (Control c in Controls)
			{
				ComponentIconFireEvent cife = c as ComponentIconFireEvent;
				if (cife != null)
				{
					if (cife.EventId == method.EventId && cife.FirerId == method.MemberId)
					{
						SetIconSelection(cife);
						break;
					}
				}
			}
		}
		public void OnObjectNameChanged(INonHostedObject obj)
		{
			if (obj.ClassId == this.ClassId)
			{
				MethodClass mc = obj as MethodClass;
				if (mc != null)
				{
					foreach (Control c in Controls)
					{
						ComponentIconMethod cip = c as ComponentIconMethod;
						if (cip != null)
						{
							if (cip.MethodId == mc.MethodID)
							{
								cip.SetLabelText(mc.MethodName);
								cip.AdjustLabelPosition();
								NotifyChanges();
								break;
							}
						}
					}
				}
				else
				{
					PropertyClass pc = obj as PropertyClass;
					if (pc != null)
					{
						foreach (Control c in Controls)
						{
							ComponentIconProperty cip = c as ComponentIconProperty;
							if (cip != null)
							{
								if (cip.PropertyId == pc.MemberId)
								{
									cip.SetLabelText(pc.Name);
									cip.AdjustLabelPosition();
									NotifyChanges();
									break;
								}
							}
						}
					}
					else
					{
						EventClass ec = obj as EventClass;
						if (ec != null)
						{
							ComponentIconEvent ci = _data.GetRootComponentIcon();
							if (ci != null)
							{
								EventIcon ei = ci.GetEventIcon(ec);
								if (ei != null)
								{
									CustomEventPointer cep = ei.Event as CustomEventPointer;
									if (cep != null)
									{
										cep.ResetEventClass(ec);
									}
									ei.RefreshLabelText();
								}
							}
						}
					}
				}
			}
		}

		public void OnIconChanged(uint classId)
		{
			foreach (ComponentIconEvent cie in _data.ComponentIconList)
			{
				if (cie.IsForComponent)
				{
					if (cie.ClassId == classId)
					{
						if (classId == _loader.ObjectMap.ClassId)
						{
							if (cie.MemberId == _loader.ObjectMap.MemberId)
							{
								cie.RefreshIcon();
							}
						}
						else
						{
							cie.RefreshIcon();
						}
					}
					else
					{
						MemberComponentIdCustom mcc = cie.ClassPointer as MemberComponentIdCustom;
						if (mcc != null)
						{
							ClassInstancePointer cp = mcc.Pointer as ClassInstancePointer;
							if (cp != null)
							{
								if (cp.Definition != null && cp.Definition.ClassId == classId)
								{
									cie.RefreshIcon();
								}
							}
						}
					}
				}
			}
		}

		public void OnAddExternalType(uint classId, Type t)
		{
		}

		public void OnRemoveExternalType(uint classId, Type t)
		{
		}

		public void SetClassRefIcon(uint classId, Image img)
		{
		}

		public void OnResetMap()
		{
		}

		public void OnDatabaseListChanged()
		{
		}
		public void OnDatabaseConnectionNameChanged(Guid connectionid, string newName)
		{

		}
		public void OnInterfaceAdded(LimnorDesigner.Interface.InterfacePointer interfacePointer)
		{
		}

		public void OnRemoveInterface(LimnorDesigner.Interface.InterfacePointer interfacePointer)
		{
		}

		public void OnInterfaceMethodDeleted(LimnorDesigner.Interface.InterfaceElementMethod method)
		{
		}

		public void OnInterfaceMethodChanged(LimnorDesigner.Interface.InterfaceElementMethod method)
		{
		}

		public void OnInterfaceEventDeleted(LimnorDesigner.Interface.InterfaceElementEvent eventType)
		{
		}

		public void OnInterfaceEventAdded(LimnorDesigner.Interface.InterfaceElementEvent eventType)
		{
		}

		public void OnInterfaceEventChanged(LimnorDesigner.Interface.InterfaceElementEvent eventType)
		{
		}

		public void OnInterfacePropertyDeleted(LimnorDesigner.Interface.InterfaceElementProperty property)
		{
		}

		public void OnInterfacePropertyAdded(LimnorDesigner.Interface.InterfaceElementProperty property)
		{
		}

		public void OnInterfacePropertyChanged(LimnorDesigner.Interface.InterfaceElementProperty property)
		{
		}

		public void OnInterfaceMethodCreated(LimnorDesigner.Interface.InterfaceElementMethod method)
		{
		}

		public void OnBaseInterfaceAdded(LimnorDesigner.Interface.InterfaceClass owner, LimnorDesigner.Interface.InterfacePointer baseInterface)
		{
		}

		public void OnEventListChanged(VPL.ICustomEventMethodDescriptor owner, uint objectId)
		{
		}

		#endregion

		#region IIconHolder Members

		public void ClearIconSelection()
		{
			for (int i = 0; i < Controls.Count; i++)
			{
				ComponentIcon ci = Controls[i] as ComponentIcon;
				if (ci != null)
				{
					if (ci.IsSelected)
					{
						ci.IsSelected = false;
						ci.Invalidate();
					}
				}
			}
		}
		/// <summary>
		/// icon selected by mouse down
		/// </summary>
		/// <param name="icon"></param>
		public void SetIconSelection(ComponentIcon icon)
		{
			for (int i = 0; i < Controls.Count; i++)
			{
				ComponentIcon ci = Controls[i] as ComponentIcon;
				if (ci != null)
				{
					if (ci == icon)
					{
						if (!ci.IsSelected)
						{
							ci.IsSelected = true;
						}
					}
					else
					{
						if (ci.IsSelected)
						{
							ci.IsSelected = false;
						}
					}
				}
				else
				{
					EventIcon ei = Controls[i] as EventIcon;
					if (ei != null)
					{
						ei.IsSelected = false;
					}
				}
			}
			_selectedEventIcon = null;
			if (icon != null)
			{
				this.ClearLineSelection();
				_eventPathHolder.OnComponentIconSelection(icon);
			}
		}

		#endregion

		#region IWithProject Members

		public LimnorProject Project
		{
			get
			{
				if (_loader != null)
					return _loader.Project;
				return null;
			}
		}

		#endregion

	}

	class LabelBox : Label
	{
		private EventPath _parent;
		public LabelBox(EventPath parent)
		{
			_parent = parent;
			this.BackColor = Color.Red;
			this.Cursor = Cursors.SizeAll;
			this.Size = new Size(8, 8);
		}
		private int _x0 = 0, _y0 = 0, dx = 0, dy = 0;
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			_x0 = e.X;
			_y0 = e.Y;
			dx = 0;
			dy = 0;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				int dx0 = e.X - _x0;
				int dy0 = e.Y - _y0;
				if (dx0 != 0 || dy0 != 0)
				{
					int x = this.Location.X + dx0;
					int y = this.Location.Y + dy0;
					if (x >= 0 && y >= 0 && x < _parent.Width && y < _parent.Height)
					{
						dx += dx0;
						dy += dy0;
						_parent.OnDragBox(dx0, dy0);
					}
				}
			}
			base.OnMouseMove(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			_parent.OnDraggedBox(dx, dy);
			base.OnMouseUp(e);
		}
	}
}
