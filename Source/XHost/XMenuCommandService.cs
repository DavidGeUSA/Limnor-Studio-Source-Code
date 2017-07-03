/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Designer Host
 * License: GNU General Public License v3.0
 
 */
//------------------------------------------------------------------------------
/// <copyright from='2006' to='2020' company='Longflow Enterprises Ltd'>
///    Copyright (c) Longflow Enterprises Ltd. All Rights Reserved.
///
/// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.Design;
using VOB;
using System.ComponentModel.Design.Serialization;
using LimnorDesigner.MenuUtil;
using LimnorWebBrowser;
using Limnor.Drawing2D;
using System.Collections.Generic;
using LimnorDesigner;
using VPL;
using Limnor.WebBuilder;
using System.Globalization;
using WindowsUtility;

namespace XHost
{
	/// The IMenuCommandService keeps track of MenuCommands and Designer Verbs
	/// that are available at any single moment. It can invoke these commands
	/// and also handles the displaying of ContextMenus for designers that support them.
	public class XMenuCommandService : MenuCommandService
	{
		private Hashtable commands; // added MenuCommands
		private Hashtable verbsFromMenuItems; // DesignerVerb values mapped to MenuItem keys
		private Hashtable menuItemsFromVerbs; // MenuItem values mapped to DesignerVerb keys
		private ArrayList globalVerbs; // verbs currently available to all designers
		private ContextMenu cm;
		public XMenuCommandService(IServiceProvider serviceProvider)
			: base(serviceProvider)
		{
			MenuCommand undoCommand = new MenuCommand(new EventHandler(ExecuteUndo), StandardCommands.Undo);
			undoCommand.Enabled = false;
			base.AddCommand(undoCommand);

			MenuCommand redoCommand = new MenuCommand(new EventHandler(ExecuteRedo), StandardCommands.Redo);
			redoCommand.Enabled = false;
			base.AddCommand(redoCommand);

			MenuCommand cutCommand = new MenuCommand(new EventHandler(ExecuteCut), StandardCommands.Cut);
			base.AddCommand(cutCommand);

			MenuCommand delCommand = new MenuCommand(new EventHandler(ExecuteDel), StandardCommands.Delete);
			base.AddCommand(delCommand);

			MenuCommand copyCommand = new MenuCommand(new EventHandler(ExecuteCopy), StandardCommands.Copy);
			base.AddCommand(copyCommand);

			MenuCommand pasteCommand = new MenuCommand(new EventHandler(ExecutePaste), StandardCommands.Paste);
			base.AddCommand(pasteCommand);
		}
		void enableUndoMenu()
		{
			MenuCommand undoCommand = this.FindCommand(StandardCommands.Undo);
			undoCommand.Enabled = true;
		}
		#region Menu Commands
		void ExecuteUndo(object sender, EventArgs e)
		{
			UndoEngineImpl undoEngine = GetService(typeof(UndoEngine)) as UndoEngineImpl;
			if (undoEngine != null)
				undoEngine.DoUndo();
		}
		void ExecuteRedo(object sender, EventArgs e)
		{
			UndoEngineImpl undoEngine = GetService(typeof(UndoEngine)) as UndoEngineImpl;
			if (undoEngine != null)
				undoEngine.DoRedo();
		}
		void ExecuteCut(object sender, EventArgs e)
		{
			IDesignerHost host = (IDesignerHost)(this.GetService(typeof(IDesignerHost)));
			if (host != null)
			{
				ISelectionService ss = (ISelectionService)host.GetService(typeof(ISelectionService));
				if (ss != null)
				{
					ICollection cc = ss.GetSelectedComponents();
					if (cc != null)
					{

						using (DesignerTransaction trans = host.CreateTransaction("Cut " + cc.Count + " component(s)"))
						{
							ExecuteCopy(sender, e);
							ExecuteDel(sender, e);
							trans.Commit();
							enableUndoMenu();
						}

					}
				}
			}
		}
		void ExecuteCopy(object sender, EventArgs e)
		{
			ISelectionService ss = (ISelectionService)this.GetService(typeof(ISelectionService));
			if (ss != null)
			{
				if (ss.PrimarySelection != null)
				{
					IDesignerSerializationService dss = (IDesignerSerializationService)this.GetService(typeof(IDesignerSerializationService));
					if (dss != null)
					{
						object v = dss.Serialize(ss.GetSelectedComponents());
						System.Windows.Forms.Clipboard.SetDataObject(new DataObject("DesignObject", v), true);
					}
				}
			}
		}
		void ExecutePaste(object sender, EventArgs e)
		{
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
			if (host != null)
			{
				IDesignerSerializationService dss = (IDesignerSerializationService)this.GetService(typeof(IDesignerSerializationService));
				if (dss != null)
				{
					object v = System.Windows.Forms.Clipboard.GetData("DesignObject");
					if (v != null)
					{
						ICollection cc = dss.Deserialize(v);
						if (cc != null)
						{
							using (DesignerTransaction trans = host.CreateTransaction("Paste"))
							{
								foreach (object x in cc)
								{
									if (x is IComponent)
									{
										host.Container.Add((IComponent)x);
									}
								}
								ISelectionService selSvc = (ISelectionService)GetService(typeof(ISelectionService));
								if (selSvc != null)
								{
									selSvc.SetSelectedComponents(cc, SelectionTypes.Replace);
								}
								trans.Commit();
								enableUndoMenu();
							}
						}
					}
				}
			}
		}
		void ExecuteDel(object sender, EventArgs e)
		{
			if (VobState.GetMainPropertyGridFocus != null)
			{
				if (VobState.GetMainPropertyGridFocus())
				{
					if (VobState.SendDeleteToMainPropertyGridTextEditor != null)
					{
						VobState.SendDeleteToMainPropertyGridTextEditor(this, EventArgs.Empty);
					}
					return;
				}
			}
			ISelectionService ss = (ISelectionService)this.GetService(typeof(ISelectionService));
			if (ss != null)
			{
				ICollection cc = ss.GetSelectedComponents();
				if (cc != null)
				{
					IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
					if (host != null)
					{
						using (DesignerTransaction trans = host.CreateTransaction("Delete " + cc.Count + " component(s)"))
						{
							foreach (object v in cc)
							{
								if (v is System.ComponentModel.IComponent)
								{
									if (v != host.RootComponent)
									{
										host.DestroyComponent((System.ComponentModel.IComponent)v);
									}
								}
							}
							trans.Commit();
							enableUndoMenu();
						}
					}
				}
			}
		}
		#endregion
		/// The IMenuCommandService deals with two kinds of verbs: 1) local verbs specific
		/// to each designer (i.e. Add/Remove Tab on a TabControl) which are added
		/// and removed on-demand, each time a designer is right-clicked, 2) the rarer
		/// global verbs, which once added are available to all designers,
		/// until removed. This method (not a standard part of IMenuCommandService) is used
		/// to add a local verb. If the verb is already in our global list, we don't add it 
		/// again. It is called through IMenuCommandService.ShowContextMenu().
		public void AddLocalVerb(DesignerVerb verb)
		{
			if ((globalVerbs == null) || (!globalVerbs.Contains(verb)))
			{
				if (cm == null)
				{
					cm = new ContextMenu();
					verbsFromMenuItems = new Hashtable();
					menuItemsFromVerbs = new Hashtable();
				}

				// Verbs and MenuItems are dually mapped to each other, so that we can
				// check for association given either half of the pair. All of our MenuItems
				// use the same event handler, but we can check the event sender to see
				// what verb to invoke. MenuItems like to only be assigned to one Menu in their
				// lifetime, so we have to create a single ContextMenu and use that thereafter.
				// If we were to instead create a ContextMenu every time we need to show one,
				// the MenuItems' click events might not work properly.
				//
				MenuItem menuItem = new MenuItem(verb.Text);
				menuItem.Click += new EventHandler(menuItem_Click);
				verbsFromMenuItems.Add(menuItem, verb);
				menuItemsFromVerbs.Add(verb, menuItem);
				cm.MenuItems.Add(menuItem);
			}
		}

		/// Remove a local verb, but only if it isn't in our global list.
		/// It is also called through IMenuCommandService.ShowContextMenu().
		public void RemoveLocalVerb(DesignerVerb verb)
		{
			if ((globalVerbs == null) || (!globalVerbs.Contains(verb)))
			{
				if (cm != null)
				{
					// Remove the verb and its mapped MenuItem from our tables and menu.
					MenuItem key = menuItemsFromVerbs[verb] as MenuItem;
					verbsFromMenuItems.Remove(key);
					menuItemsFromVerbs.Remove(verb);
					cm.MenuItems.Remove(key);
				}
			}
		}

		#region Implementation of IMenuCommandService

		/// If a designer supports a MenuCommand, it will add it here.
		public override void AddCommand(System.ComponentModel.Design.MenuCommand command)
		{
			if (commands == null)
			{
				commands = new Hashtable();
			}

			// Only add a command if we haven't already.
			if (FindCommand(command.CommandID) == null)
			{
				if (!commands.ContainsKey(command.CommandID))
				{
					commands.Add(command.CommandID, command);
				}
			}
		}

		/// This is only called by external callers if someone wants to
		/// remove a verb that is available for all designers. We keep track of
		/// such verbs to make sure that they are not re-added or removed
		/// when we frequently manipulate our local verbs.
		public override void RemoveVerb(System.ComponentModel.Design.DesignerVerb verb)
		{
			if (globalVerbs != null)
			{
				globalVerbs.Remove(verb);

				if (cm != null)
				{
					// Remove the verb and its mapped MenuItem from our tables and menu.
					MenuItem key = menuItemsFromVerbs[verb] as MenuItem;
					verbsFromMenuItems.Remove(key);
					menuItemsFromVerbs.Remove(verb);
					cm.MenuItems.Remove(key);
				}
			}
		}

		/// If a command is no longer viable for a designer, it is removed.
		public override void RemoveCommand(System.ComponentModel.Design.MenuCommand command)
		{
			if (commands != null && command != null)
			{
				// Hashtable already has nothing happen if the command isn't there.
				commands.Remove(command.CommandID);
			}
		}

		/// We only invoke commands that have been added.
		public override bool GlobalInvoke(System.ComponentModel.Design.CommandID commandID)
		{
			MenuCommand command = FindCommand(commandID);
			if (command != null)
			{
				command.Invoke();
				return true;
			}
			return false;
		}

		/// This is called whenever the user right-clicks on a designer. It removes any local verbs
		/// added by a previous, different selection and adds the local verbs for the current (primary)
		/// selection. Then it displays the ContextMenu.
		public override void ShowContextMenu(CommandID menuID, int x, int y)
		{

			ISelectionService ss = this.GetService(typeof(ISelectionService)) as ISelectionService;
			if (ss == null || ss.PrimarySelection == null)
			{
				return;
			}
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
			//
			if (host != null)
			{
				HostSurface surface = host.GetService(typeof(DesignSurface)) as HostSurface;
				if (surface != null)
				{
					ContextMenu menu = new ContextMenu();
					if (host.RootComponent != ss.PrimarySelection && ss.PrimarySelection is Control)
					{
						MenuItem mi = new MenuItem("Bring to front", menu_bringToFront);
						mi.Tag = ss.PrimarySelection;
						menu.MenuItems.Add(mi);
						//
						mi = new MenuItem("Send to back", menu_sendToBack);
						mi.Tag = ss.PrimarySelection;
						menu.MenuItems.Add(mi);
						//
						menu.MenuItems.Add("-");
					}
					if (host.RootComponent != ss.PrimarySelection)
					{
						MenuItem mi = new MenuItem("Copy", menu_cmd);
						mi.Tag = StandardCommands.Copy;
						menu.MenuItems.Add(mi);
						//
						mi = new MenuItem("Cut", menu_cmd);
						mi.Tag = StandardCommands.Cut;
						menu.MenuItems.Add(mi);
						//
						mi = new MenuItem("Delete", menu_cmd);
						mi.Tag = StandardCommands.Delete;
						menu.MenuItems.Add(mi);
						//
						if (ss.SelectionCount == 1)
						{
							MenuItem mi2 = new MenuItemWithBitmap("Add to toolbar", addToToolbar, Resource1._toolbar.ToBitmap());
							IXType ix = ss.PrimarySelection as IXType;
							if (ix != null)
							{
								mi2.Tag = ix.ValueType;
							}
							else
							{
								mi2.Tag = ss.PrimarySelection.GetType();
							}
							menu.MenuItems.Add(mi2);
						}
					}
					else
					{
						MenuItem mi = new MenuItem("Undo", menu_cmd);
						mi.Tag = StandardCommands.Undo;
						menu.MenuItems.Add(mi);
						//
						mi = new MenuItem("Redo", menu_cmd);
						mi.Tag = StandardCommands.Redo;
						menu.MenuItems.Add(mi);
						//
						mi = new MenuItem("Paste", menu_cmd);
						mi.Tag = StandardCommands.Paste;
						menu.MenuItems.Add(mi);
					}
					//
					DrawingPage dp = ss.PrimarySelection as DrawingPage;
					if (dp != null)
					{
						MenuItem mi2 = new MenuItem("-");
						menu.MenuItems.Add(mi2);
						//
						mi2 = new MenuItemWithBitmap("Drawing Board", showDrawingBoard, Resource1._paint.ToBitmap());
						mi2.Tag = dp;
						menu.MenuItems.Add(mi2);
					}
					else
					{
						IDrawDesignControl drc = ss.PrimarySelection as IDrawDesignControl;
						if (drc != null)
						{
							MenuItem midrc = new MenuItemWithBitmap("Copy to Clipboard as bitmap image", mi_copyDrawing, Resource1._copy.ToBitmap());
							midrc.Tag = drc;
							menu.MenuItems.Add(midrc);
							midrc = new MenuItemWithBitmap("Save to XML file", mi_savedrawing, Resource1._savefile.ToBitmap());
							midrc.Tag = drc;
							menu.MenuItems.Add(midrc);
							midrc = new MenuItemWithBitmap("Load from XML file", mi_loadDrawing, Resource1._loadfile.ToBitmap());
							midrc.Tag = drc;
							menu.MenuItems.Add(midrc);
						}
					}
					MenuItem mi20 = new MenuItem("-");
					menu.MenuItems.Add(mi20);
					MenuItem miAddComponent = new MenuItemWithBitmap("Add component", mi_addComponent, Resource1._newIcon.ToBitmap());
					menu.MenuItems.Add(miAddComponent);
					//
					LimnorContextMenuCollection mdata = surface.GetObjectMenuData(ss.PrimarySelection);
					if (mdata != null)
					{
						if (menu.MenuItems.Count > 0)
						{
							menu.MenuItems.Add("-");
						}
						mdata.CreateContextMenu(menu, new Point(0, 0), surface.Loader.ViewerHolder);
					}

					if (menu.MenuItems.Count > 0)
					{
						Control ps = surface.View as Control;
						if (ps != null)
						{
							LimnorXmlDesignerLoader2.MenuPoint = new Point(x, y);
							Point s = ps.PointToClient(LimnorXmlDesignerLoader2.MenuPoint);
							menu.Show(ps, s);
						}
						else
						{
							ps = ss.PrimarySelection as Control;
							if (ps != null)
							{
								LimnorXmlDesignerLoader2.MenuPoint = new Point(x, y);
								Point s = ps.PointToScreen(new Point(0, 0));
								menu.Show(ps, new Point(x - s.X, y - s.Y));
							}
						}
					}
				}
			}
		}
		private void addToToolbar(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				Type t = mi.Tag as Type;
				if (t != null)
				{
					sendVOBnotice(enumVobNotice.AddFrequentType, t);
				}
			}
		}
		private void sendVOBnotice(enumVobNotice notice, object data)
		{
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
			if (host != null)
			{
				InterfaceVOB vob = (InterfaceVOB)host.GetService(typeof(InterfaceVOB));
				if (vob != null)
				{
					vob.SendNotice(notice, data);
				}
			}
		}
		private void mi_loadDrawing(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				IDrawDesignControl drc = mi.Tag as IDrawDesignControl;
				if (drc != null && drc.Item != null)
				{
					IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
					if (host != null)
					{
						HostSurface surface = host.GetService(typeof(DesignSurface)) as HostSurface;
						if (surface != null)
						{
							ClassPointer root = surface.Loader.GetRootId();
							if (root != null)
							{
								ILimnorDesignerLoader ldp = root.GetDesignerLoader();
								if (ldp != null)
								{
									IDesignPane idp = ldp.DesignPane as IDesignPane;
									OpenFileDialog dlg = new OpenFileDialog();
									dlg.CheckFileExists = true;
									dlg.DefaultExt = ".xml";
									dlg.Filter = "XML files|*.xml";
									dlg.Title = "Select XML file for loading drawing";
									if (dlg.ShowDialog(drc.Item.Page) == DialogResult.OK)
									{
										drc.Item.LoadDrawingsFromFile(dlg.FileName, idp, drc);
									}
								}
							}
						}
					}
				}
			}
		}
		private void mi_savedrawing(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				IDrawDesignControl drc = mi.Tag as IDrawDesignControl;
				if (drc != null && drc.Item != null)
				{
					SaveFileDialog dlg = new SaveFileDialog();
					dlg.DefaultExt = ".xml";
					dlg.Filter = "XML files|*.xml";
					dlg.Title = "Specify XML file name for saving the drawing";
					if (dlg.ShowDialog(drc.Item.Page) == DialogResult.OK)
					{
						if (drc.Item.SaveToXmlFile(dlg.FileName))
						{
							MessageBox.Show(drc.Item.Page, string.Format(CultureInfo.InvariantCulture, "Drawing saved to {0}", dlg.FileName), "Save Drawing", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					}
				}
			}
		}
		private void mi_copyDrawing(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				IDrawDesignControl drc = mi.Tag as IDrawDesignControl;
				if (drc != null && drc.Item != null)
				{
					drc.Item.CopyToClipboardAsBitmapImage(0, 0);
				}
			}
		}

		private void editWebPage(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				IWebPage page = mi.Tag as IWebPage;
				if (page != null)
				{
					IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
					if (host != null)
					{
						HostSurface surface = host.GetService(typeof(DesignSurface)) as HostSurface;
						if (surface != null)
						{
							ClassPointer root = surface.Loader.GetRootId();
							root.EditWebPage();
						}
					}
				}
			}
		}
		private void mi_addComponent(object sender, EventArgs e)
		{
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
			if (host != null)
			{
				HostSurface surface = host.GetService(typeof(DesignSurface)) as HostSurface;
				if (surface != null)
				{
					ClassPointer root = surface.Loader.GetRootId();
					if (root != null)
					{
						root.AddComponent(null);
					}
				}
			}
		}
		private void showDrawingBoard(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				DrawingPage page = mi.Tag as DrawingPage;
				if (page != null)
				{
					IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
					if (host != null)
					{
						HostSurface surface = host.GetService(typeof(DesignSurface)) as HostSurface;
						if (surface != null)
						{
							ClassPointer root = surface.Loader.GetRootId();
							DrawingLayerCollection layers = page.ShowDrawingsEditor();
							if (layers != null)
							{
								//layers0 is the existing layer collections
								DrawingLayerCollection layers0 = page.DrawingLayers;
								//for layers0 if a drawing object's guid does not exist in layers then
								//the object has been deleted
								List<DrawingItem> deleted = new List<DrawingItem>();
								foreach (DrawingLayer l0 in layers0)
								{
									DrawingItem d = null;
									for (int i = 0; i < l0.Count; i++)
									{
										DrawingItem d0 = l0[i];
										d = layers.GetDrawingItemById(d0.DrawingId);
										if (d != null)
										{
											d0.Copy(d);//not deleted
											//move and resize the IDrawDesignControl
											foreach (Control c in page.Controls)
											{
												IDrawDesignControl dc0 = c as IDrawDesignControl;
												if (dc0 != null)
												{
													if (dc0.Item == d0)
													{
														c.Location = d0.Location;
														c.Size = d0.Bounds.Size;
														break;
													}
												}
											}
										}
										else
										{
											deleted.Add(d0); //deleted
										}
									}
								}
								if (deleted.Count > 0)
								{
									//check to see if each deleted item has usages
									List<DrawingItem> ls = new List<DrawingItem>();
									foreach (DrawingItem d0 in deleted)
									{
										for (int i = 0; i < page.Controls.Count; i++)
										{
											IDrawDesignControl dc = page.Controls[i] as IDrawDesignControl;
											if (dc != null)
											{
												if (dc.DrawingId == d0.DrawingId)
												{
													uint id = root.ObjectList.GetObjectID(dc);
													if (id == 0)
													{
														DesignUtil.WriteToOutputWindow("id not found on deleting component {0}", dc.Name);
													}
													else
													{
														List<ObjectTextID> list = root.GetComponentUsage(id);
														if (list.Count > 0)
														{
															dlgObjectUsage dlg = new dlgObjectUsage();
															dlg.LoadData("Cannot delete this component. It is being used by the following objects", string.Format("Component - {0}", dc.Name), list);
															dlg.ShowDialog();
															ls.Add(d0);
														}
													}
													break;
												}
											}
										}
									}
									if (ls.Count > 0)
									{
										foreach (DrawingItem d0 in ls)
										{
											deleted.Remove(d0);
										}
									}
									foreach (DrawingItem d0 in deleted)
									{
										for (int i = 0; i < page.Controls.Count; i++)
										{
											IDrawDesignControl dc = page.Controls[i] as IDrawDesignControl;
											if (dc != null)
											{
												if (dc.DrawingId == d0.DrawingId)
												{
													surface.Loader.DeleteComponent(page.Controls[i]);
													break;
												}
											}
										}
									}
								}
								//for a drawing object in layers if its guid does not exist in layers0 then 
								//it is a new object
								//reset zorder according to the new layers
								List<Control> ctrls = new List<Control>();
								foreach (DrawingLayer l in layers)
								{
									int zOrder = 0;
									foreach (DrawingItem d in l)
									{
										DrawingItem d0 = layers0.GetDrawingItemById(d.DrawingId);
										if (d0 == null)
										{
											//add a new drawing
											string name = d.Name + Guid.NewGuid().GetHashCode().ToString("x");
											Type designerType = TypeMappingAttribute.GetMappedType(d.GetType());
											if (designerType == null)
											{
												throw new DesignerException("Drawing type {0} does not have a designer", d.GetType());
											}
											Control dc = (Control)surface.Loader.CreateComponent(designerType, name);
											IDrawDesignControl ddc = (IDrawDesignControl)dc;
											ddc.Item = d;
											dc.Location = d.Location;
											dc.Size = d.Bounds.Size;
											ctrls.Add(dc);
											ddc.ZOrder = zOrder;
										}
										else
										{
											for (int k = 0; k < page.Controls.Count; k++)
											{
												IDrawDesignControl dc = page.Controls[k] as IDrawDesignControl;
												if (dc != null)
												{
													if (dc.Item == d0)
													{
														dc.ZOrder = zOrder;
														break;
													}
												}
											}
										}
										zOrder += 10;
									}
								}
								if (ctrls.Count > 0)
								{
									page.Controls.AddRange(ctrls.ToArray());
								}
								page.LoadData(layers, false);
								page.Refresh();
								surface.SetModified();
								surface.Loader.NotifyChanges();
							}
						}
					}
				}
			}
		}
		private void menu_bringToFront(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				IDrawDesignControl ddc = mi.Tag as IDrawDesignControl;
				if (ddc != null)
				{
					IDrawingHolder h = ddc.Item.Holder;
					if (h != null)
					{
						h.BringObjectToFront(ddc.Item);
					}
				}
				Control c = mi.Tag as Control;
				if (c != null)
				{
					c.BringToFront();
					makeControlChanged(c);
				}
			}
		}
		private void menu_sendToBack(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				IDrawDesignControl ddc = mi.Tag as IDrawDesignControl;
				if (ddc != null)
				{
					IDrawingHolder h = ddc.Item.Holder;
					if (h != null)
					{
						h.SendObjectToBack(ddc.Item);
					}
				}
				Control c = mi.Tag as Control;
				if (c != null)
				{
					c.SendToBack();
					makeControlChanged(c);
				}
			}
		}
		private int getControlCount(Control c)
		{
			int n = c.Controls.Count;
			for (int i = 0; i < c.Controls.Count; i++)
			{
				n += getControlCount(c.Controls[i]);
			}
			return n;
		}
		private void adjustWebControlZOrders(Control c, int n)
		{
			for (int i = 0; i < c.Controls.Count; i++)
			{
				IWebClientControl wc = c.Controls[i] as IWebClientControl;
				if (wc != null)
				{
					int zi = n - c.Controls.GetChildIndex(c.Controls[i], false);
					wc.zOrder = zi;
				}
			}
			n -= c.Controls.Count;
			for (int i = 0; i < c.Controls.Count; i++)
			{
				adjustWebControlZOrders(c.Controls[i], n);
			}
		}
		private void makeControlChanged(Control c)
		{
			Control pc = c.Parent;
			if (pc != null)
			{
				Control p0 = pc;
				while (!(p0 is Form) && p0.Parent != null)
				{
					p0 = p0.Parent;
				}
				int nm = getControlCount(p0) + 10;
				adjustWebControlZOrders(p0, nm);
			}
			int n = c.Left;
			PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(c, true);
			foreach (PropertyDescriptor p in ps)
			{
				if (string.CompareOrdinal("Left", p.Name) == 0)
				{
					p.SetValue(c, n + 1);
					p.SetValue(c, n);
					break;
				}
			}
		}
		private void menu_cmd(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi != null)
			{
				CommandID cid = mi.Tag as CommandID;
				if (cid != null)
				{
					this.GlobalInvoke(cid);
				}
			}
		}
		/// This is only called by external callers if someone wants to
		/// remove a verb that is available for all designers. We keep track of
		/// such verbs to make sure that they are not re-added or removed
		/// when we frequently manipulate our local verbs.
		public override void AddVerb(System.ComponentModel.Design.DesignerVerb verb)
		{
			if (globalVerbs == null)
			{
				globalVerbs = new ArrayList();
			}

			globalVerbs.Add(verb);

			if (cm == null)
			{
				cm = new ContextMenu();
				verbsFromMenuItems = new Hashtable();
				menuItemsFromVerbs = new Hashtable();
			}

			// Verbs and MenuItems are dually mapped to each other, so that we can
			// check for association given either half of the pair. All of our MenuItems
			// use the same event handler, but we can check the event sender to see
			// what verb to invoke. We have to have a stable ContextMenu, and we need
			// to add the MenuItems to it now. If we try to add the MenuItems on-demand
			// right before we show a ContextMenu, the clicks events won't work.
			//
			MenuItem menuItem = new MenuItem(verb.Text);
			menuItem.Click += new EventHandler(menuItem_Click);
			verbsFromMenuItems.Add(menuItem, verb);
			menuItemsFromVerbs.Add(verb, menuItem);
			cm.MenuItems.Add(menuItem);
		}

		/// This is frequently called to get the current list of available verbs. We have
		/// to be careful in what verbs we return, however, because the information contained
		/// in menuItemsFromVerbs and verbsFromMenuItems will not be current if there has been
		/// no right-click since the last selection change. Thus we return the global verbs
		/// plus any additional local verbs for the current selection.
		public override System.ComponentModel.Design.DesignerVerbCollection Verbs
		{
			get
			{
				ArrayList currentVerbs;
				if (globalVerbs != null)
				{
					currentVerbs = new ArrayList(globalVerbs);
				}
				else
				{
					currentVerbs = new ArrayList();
				}
				IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
				if (host != null)
				{
					ISelectionService ss = this.GetService(typeof(ISelectionService)) as ISelectionService;
					if (ss != null && ss.PrimarySelection is IComponent)
					{
						IDesigner dr = host.GetDesigner(ss.PrimarySelection as IComponent);
						if (dr != null && dr.Verbs != null)
						{
							foreach (DesignerVerb verb in dr.Verbs)
							{
								if (!currentVerbs.Contains(verb))
								{
									currentVerbs.Add(verb);
								}
							}
						}
					}
				}
				if (currentVerbs.Count > 0)
				{
					DesignerVerb[] ret = new DesignerVerb[currentVerbs.Count];
					currentVerbs.CopyTo(ret);
					return new DesignerVerbCollection(ret);
				}
				else
				{
					return new DesignerVerbCollection();
				}
			}
		}

		#endregion
		/// All of our MenuItems' click events are handled by this method. It checks the
		/// mapping to see which verb is associated with the MenuItem that sent the event
		/// and then invokes that verb.
		private void menuItem_Click(object sender, EventArgs e)
		{
			MenuItem key = sender as MenuItem;
			DesignerVerb v = verbsFromMenuItems[key] as DesignerVerb;
			this.GlobalInvoke(v.CommandID);
		}
	}
}
