/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Project Serialization in XML
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace XmlSerializer
{
	/// <summary>
	/// build object hierarchy.
	/// Control, MenuStrip/ToolStripMenuItem
	/// </summary>
	public class Tree : List<Tree>
	{
		private object _owner;
		private Tree _parent;
		public Tree(Tree parent, object owner)
		{
			_parent = parent;
			if (parent != null)
			{
				TreeRoot r = parent.Root;
				ObjectIDmap map = r.Map;
				IClassRef cr = map.GetClassRefByObject(owner);
				if (cr != null)
				{
					owner = cr;
				}
			}
			_owner = owner;
		}
		public object Owner
		{
			get
			{
				return _owner;
			}
		}
		public Tree Parent
		{
			get
			{
				return _parent;
			}
		}
		public TreeRoot Root
		{
			get
			{
				if (_parent == null)
				{
					return this as TreeRoot;
				}
				return _parent.Root;
			}
		}
		/// <summary>
		/// to be called from root
		/// </summary>
		/// <param name="c"></param>
		public Tree AddChild(object c)
		{
			ObjectIDmap map = Root.Map;
			IClassRef cr = map.GetClassRefByObject(c);
			if (cr != null)
			{
				c = cr;
			}
			Tree t0 = SearchChildByOwner(c);
			if (t0 != null)
			{
				return t0;
			}
			ToolStripItem mi = c as ToolStripItem;
			if (mi != null)
			{
				ToolStrip ts = GetTopMenuOwner(mi);
				if (ts == this.Owner)
				{
					if (mi.OwnerItem == null)
					{
						this.Add(new Tree(this, mi));
						return this;
					}
					else
					{
						Tree t = this.SearchChildByOwner(mi.OwnerItem);
						if (t != null)
						{
							t.Add(new Tree(t, mi));
							return t;
						}
						else
						{
							this.Add(new Tree(this, mi));
							return this;
						}
					}
				}
				else
				{
					foreach (Tree t in this)
					{
						if (ts == t.Owner)
						{
							return t.AddChild(mi);
						}
					}
					this.Add(new Tree(this, mi));
					return this;
				}
			}
			else
			{
				DataGridViewColumn dc = c as DataGridViewColumn;
				if (dc != null)
				{
					Tree tp = this.SearchChildByOwner(dc.DataGridView);
					if (tp == null)
					{
						tp = this;
					}
					tp.Add(new Tree(tp, c));
					return tp;
				}
				SplitterPanel stp = c as SplitterPanel;
				if (stp != null)
				{
					Tree tp = this.SearchChildByOwner(stp.Parent);
					if (tp == null)
					{
						tp = this;
					}
					tp.Add(new Tree(tp, c));
					return tp;
				}
				else
				{
					Control ctl = c as Control;
					if (ctl != null)
					{
						TreeRoot tr = this as TreeRoot;
						if (tr != null)
						{
							ctl.ParentChanged -= tr.ParentChangehandler;
							ctl.ParentChanged += tr.ParentChangehandler;
						}
					}
					Tree tParent = this;

					Control r = this.Owner as Control;

					if (ctl != null)
					{
						if (ctl.Parent != null)
						{
							if (ctl.Parent != r)
							{
								Tree t = this.SearchChildByOwner(ctl.Parent);
								if (t != null)
								{
									tParent = t;
								}
							}
						}
					}
					Tree tx = new Tree(tParent, c);
					tParent.Add(tx);
					SplitContainer sc = c as SplitContainer;
					if (sc != null)
					{
						tx.Add(new Tree(tx, sc.Panel1));
						tx.Add(new Tree(tx, sc.Panel2));
					}
					return tParent;
				}
			}
		}
		public static ToolStrip GetTopMenuOwner(ToolStripItem mi)
		{
			ToolStripItem o = mi;
			if (o != null)
			{
				while (o.OwnerItem != null)
				{
					o = o.OwnerItem;
				}
				return o.Owner;
			}
			return null;
		}
		public static TreeRoot PopulateChildren(ObjectIDmap map, object root, IList<object> list)
		{
			TreeRoot top = new TreeRoot(map, root);
			Control rc = root as Control;
			List<Control> ctrls = new List<Control>();
			List<ToolStripItem> menuItems = new List<ToolStripItem>();
			List<SplitterPanel> splitterpanels = new List<SplitterPanel>();
			List<DataGridViewColumn> dcolumns = new List<DataGridViewColumn>();
			foreach (object v in list)
			{
				if (v != root)
				{
					ToolStripItem mi = v as ToolStripItem;
					if (mi != null)
					{
						menuItems.Add(mi);
						continue;
					}
					DataGridViewColumn dc = v as DataGridViewColumn;
					if (dc != null)
					{
						dcolumns.Add(dc);
						continue;
					}
					SplitterPanel sp = v as SplitterPanel;
					if (sp != null)
					{
						splitterpanels.Add(sp);
						continue;
					}
					Control c = v as Control;
					if (c != null)
					{
						c.ParentChanged += new EventHandler(top.OnControlParentChanged);

						if (c.Parent != null && c.Parent != rc)
						{
							ctrls.Add(c);
							continue;
						}
					}
					//
					Tree t0 = new Tree(top, v);
					top.Add(t0);
					SplitContainer sc = v as SplitContainer;
					if (sc != null)
					{
						t0.Add(new Tree(t0, sc.Panel1));
						t0.Add(new Tree(t0, sc.Panel2));
					}
				}
			}

			while (ctrls.Count > 0)
			{
				List<Control> ctrls0 = new List<Control>();
				foreach (Control c in ctrls)
				{
					Tree t = top.SearchChildByOwner(c.Parent);
					if (t != null)
					{
						Tree t0 = new Tree(t, c);
						t.Add(t0);
						SplitContainer sc = c as SplitContainer;
						if (sc != null)
						{
							t0.Add(new Tree(t0, sc.Panel1));
							t0.Add(new Tree(t0, sc.Panel2));
						}
					}
					else
					{
						ctrls0.Add(c);
					}
				}
				if (ctrls.Count == ctrls0.Count)
				{
					foreach (Control c in ctrls0)
					{
						Tree t0 = new Tree(top, c);
						top.Add(t0);
						SplitContainer sc = c as SplitContainer;
						if (sc != null)
						{
							t0.Add(new Tree(t0, sc.Panel1));
							t0.Add(new Tree(t0, sc.Panel2));
						}
					}
					break;
				}
				ctrls = ctrls0;
			}

			if (splitterpanels.Count > 0)
			{
				foreach (SplitterPanel sp in splitterpanels)
				{
					Tree t = top.SearchChildByOwner(sp.Parent);
					if (t != null)
					{
						t.Add(new Tree(t, sp));
					}
				}
			}
			while (menuItems.Count > 0)
			{
				List<ToolStripItem> menuItems0 = new List<ToolStripItem>();
				foreach (ToolStripItem mi in menuItems)
				{
					ToolStrip ts = GetTopMenuOwner(mi);
					if (ts == null)
					{
						top.Add(new Tree(top, mi));
					}
					else
					{
						Tree t = top.GetChildByOwner(ts);
						if (t == null)
						{
							top.Add(new Tree(top, mi));
						}
						else
						{
							if (mi.OwnerItem == null)
							{
								t.Add(new Tree(t, mi));
							}
							else
							{
								t = top.SearchChildByOwner(mi.OwnerItem);
								if (t != null)
								{
									t.Add(new Tree(t, mi));
								}
								else
								{
									menuItems0.Add(mi);
								}
							}
						}
					}
				}
				if (menuItems.Count == menuItems0.Count)
				{
					foreach (ToolStripItem mi in menuItems0)
					{
						top.Add(new Tree(top, mi));
					}
					break;
				}
				menuItems = menuItems0;
			}
			if (dcolumns.Count > 0)
			{
				foreach (DataGridViewColumn dc in dcolumns)
				{
					Tree t = top.GetChildByOwner(dc.DataGridView);
					if (t == null)
					{
						top.Add(new Tree(top, dc));
					}
					else
					{
						t.Add(new Tree(t, dc));
					}
				}
			}
			return top;
		}

		public Tree GetChildByOwner(object owner)
		{
			ObjectIDmap map = Root.Map;
			foreach (Tree t in this)
			{
				if (map.IsSameInstance(t.Owner, owner))
				{
					return t;
				}
				else
				{
					Tree t0 = t.GetChildByOwner(owner);
					if (t0 != null)
					{
						return t0;
					}
				}
			}
			return null;
		}
		public Tree SearchChildByOwner(object owner)
		{
			ObjectIDmap map = Root.Map;
			foreach (Tree t in this)
			{
				if (map.IsSameInstance(t.Owner, owner))
				{
					return t;
				}
				else
				{
					Tree t0 = t.SearchChildByOwner(owner);
					if (t0 != null)
					{
						return t0;
					}
				}
			}
			return null;
		}
		public virtual Tree SearchParentByOwner(object owner)
		{
			ObjectIDmap map = Root.Map;
			foreach (Tree t in this)
			{
				if (map.IsSameInstance(t.Owner, owner))
				{
					return this;
				}
				else
				{
					Tree t0 = t.SearchParentByOwner(owner);
					if (t0 != null)
					{
						return t0;
					}
				}
			}
			return null;
		}
	}
	public class EventArgsOwnerChanged : EventArgs
	{
		private object _oldOwner;
		private object _newOwner;
		public EventArgsOwnerChanged(object oldOwner, object newOwner)
		{
			_oldOwner = oldOwner;
			_newOwner = newOwner;
		}
		public object OldOwner
		{
			get
			{
				return _oldOwner;
			}
		}
		public object NewOwner
		{
			get
			{
				return _newOwner;
			}
		}
	}
	public delegate void EventHandlerOwnerChanged(object sender, EventArgsOwnerChanged e);
	public class TreeRoot : Tree
	{
		public event EventHandlerOwnerChanged OwnerChanged;
		private EventHandler _parentChangeHandler;
		private ObjectIDmap _map;
		public TreeRoot(ObjectIDmap map, object owner)
			: base(null, owner)
		{
			_map = map;
			_parentChangeHandler = new EventHandler(OnControlParentChanged);
		}
		public EventHandler ParentChangehandler
		{
			get
			{
				return _parentChangeHandler;
			}
		}
		public ObjectIDmap Map
		{
			get
			{
				return _map;
			}
		}
		public Tree RemoveTree(object owner)
		{
			Tree t = this.SearchParentByOwner(owner);
			if (t != null)
			{
				foreach (Tree t0 in t)
				{
					if (_map.IsSameInstance(t0.Owner, owner))
					{
						t.Remove(t0);
						break;
					}
				}
			}
			return t;
		}
		public override Tree SearchParentByOwner(object owner)
		{
			if (_map.IsSameInstance(owner, Owner))
			{
				return null;
			}
			return base.SearchParentByOwner(owner);
		}
		public void OnControlParentChanged(object sender, EventArgs e)
		{
			if (OwnerChanged != null && _map != null)
			{
				Control c = sender as Control;
				if (c != null && c.Parent != null)
				{
					//Tree oldOwner = RemoveTree(sender);
					//Tree newOwner = AddChild(c);
					//OwnerChanged(sender, new EventArgsOwnerChanged(oldOwner.Owner, newOwner.Owner));
					Tree oldOwner = SearchParentByOwner(c);
					if (oldOwner != null)
					{
						Tree newOwner = SearchChildByOwner(c.Parent);
						if (newOwner != null)
						{
							if (newOwner != oldOwner)
							{
								Tree t0 = null;
								foreach (Tree t in oldOwner)
								{
									if (_map.IsSameInstance(t.Owner, sender))
									{
										t0 = t;
										break;
									}
								}
								if (t0 != null)
								{
									oldOwner.Remove(t0);
									newOwner.Add(t0);
									OwnerChanged(sender, new EventArgsOwnerChanged(oldOwner.Owner, newOwner.Owner));
								}
							}
						}
					}
				}
			}
		}
		public void OnAddedObject(object v)
		{
			object parent = null;
			ToolStripItem mi = v as ToolStripItem;
			if (mi != null)
			{
				parent = mi.OwnerItem;
			}
			else
			{
				DataGridViewColumn dgc = v as DataGridViewColumn;
				if (dgc != null)
				{
					parent = dgc.DataGridView;
				}
				else
				{
					Control ctrl = v as Control;
					if (ctrl != null)
					{
						parent = ctrl.Parent;
					}
				}
			}
			if (parent != null)
			{
				Tree t = SearchChildByOwner(parent);
				if (t != null)
				{
					if (t.GetChildByOwner(v) == null)
					{
						t.Add(new Tree(t, v));
					}
				}
			}
		}
	}
}
