/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Designer Host
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
//using XLoader2;
using VPL;
using VOB;
using LimnorDesigner.MenuUtil;
using LimnorDesigner;
using System.Collections.Generic;

namespace XHost
{
	/// <summary>
	/// Inherits from DesignSurface and hosts the RootComponent and 
	/// all other designers. It also uses loaders (BasicDesignerLoader
	/// or CodeDomDesignerLoader) when required. It also provides various
	/// services to the designers. Adds MenuCommandService which is used
	/// for Cut, Copy, Paste, etc.
	/// </summary>
	public class HostSurface3 : DesignSurface
	{
		private LimnorXmlDesignerLoader2 _loader;
		private ISelectionService _selectionService;
		private Dictionary<UInt32, LimnorContextMenuCollection> _menuData;
		public HostSurface3()
			: base()
		{
			addMenuServices();
		}
		public HostSurface3(IServiceProvider parentProvider)
			: base(parentProvider)
		{
			addMenuServices();
		}
		private void addMenuServices()
		{
			_selectionService = (ISelectionService)(this.ServiceContainer.GetService(typeof(ISelectionService)));
			//
		}
		#region Component changes
		void componentChangeService_ComponentRename(object sender, ComponentRenameEventArgs e)
		{
		}

		void componentChangeService_ComponentRemoving(object sender, ComponentEventArgs e)
		{
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
			if (host != null)
			{
				VOB.OutputWindow O = (VOB.OutputWindow)host.GetService(typeof(VOB.OutputWindow));
				if (O != null)
				{
					O.AppendMessage("ComponentRemoving " + e.Component.Site.Name);
				}
			}
		}

		void componentChangeService_ComponentRemoved(object sender, ComponentEventArgs e)
		{
		}

		void componentChangeService_ComponentChanging(object sender, ComponentChangingEventArgs e)
		{
		}

		void componentChangeService_ComponentChanged(object sender, ComponentChangedEventArgs e)
		{
		}

		void componentChangeService_ComponentAdding(object sender, ComponentEventArgs e)
		{
		}

		void componentChangeService_ComponentAdded(object sender, ComponentEventArgs e)
		{
			if (!(e.Component is NewComponentPrompt))
			{
				IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
				if (host != null)
				{
					VOB.OutputWindow O = (VOB.OutputWindow)host.GetService(typeof(VOB.OutputWindow));
					if (O != null)
					{
						O.AppendMessage("ComponentAdded " + e.Component.Site.Name);
					}
				}
			}
		}
		#endregion
		internal void Initialize()
		{
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));

			if (host == null)
				return;
			//
			try
			{
				// Set SelectionService - SelectionChanged event handler
				_selectionService = (ISelectionService)(this.ServiceContainer.GetService(typeof(ISelectionService)));
				_selectionService.SelectionChanged += new EventHandler(selectionService_SelectionChanged);
				//
				UndoEngine undoEngine = (UndoEngine)host.GetService(typeof(UndoEngine));
				if (undoEngine != null)
				{
					undoEngine.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		public LimnorXmlDesignerLoader2 Loader
		{
			get
			{
				return _loader;
			}
			set
			{
				_loader = value;
			}
		}
		public bool SelectComponent(object v)
		{
			if (_selectionService != null && v != null)
			{
				_selectionService.SetSelectedComponents(new object[] { v });
				PropertyGrid propertyGrid = (PropertyGrid)this.GetService(typeof(PropertyGrid));
				propertyGrid.SelectedObject = v;
				return (_selectionService.PrimarySelection != null);
			}
			return false;
		}
		public void OnSelectionChange()
		{
			if (_selectionService != null)
			{
				ICollection selectedComponents = _selectionService.GetSelectedComponents();
				PropertyGrid propertyGrid = (PropertyGrid)this.GetService(typeof(PropertyGrid));
				object[] comps = new object[selectedComponents.Count];
				selectedComponents.CopyTo(comps, 0);
				propertyGrid.SelectedObjects = comps;
				//
				if (_selectionService.PrimarySelection != null)
				{
				}
			}
		}
		/// <summary>
		/// When the selection changes this sets the PropertyGrid's selected component 
		/// </summary>
		private void selectionService_SelectionChanged(object sender, EventArgs e)
		{
			OnSelectionChange();
		}

		public void AddService(Type type, object serviceInstance)
		{
			this.ServiceContainer.AddService(type, serviceInstance);
		}
		public LimnorContextMenuCollection GetObjectMenuData(object obj)
		{
			LimnorXmlDesignerLoader2 l = this.Loader as LimnorXmlDesignerLoader2;
			UInt32 id = l.ObjectMap.GetObjectID(obj);
			if (_menuData == null)
			{
				_menuData = new Dictionary<UInt32, LimnorContextMenuCollection>();
			}
			LimnorContextMenuCollection data;
			if (!_menuData.TryGetValue(id, out data))
			{
				if (obj == l.RootObject)
				{
					data = new LimnorContextMenuCollection(l.GetRootId());
				}
				else
				{
					MemberComponentId mc = MemberComponentId.CreateMemberComponentId(l.GetRootId(), obj, id, null);
					data = new LimnorContextMenuCollection(mc);
				}
				_menuData.Add(id, data);
			}
			return data;
		}
		public LimnorContextMenuCollection GetMenuData(IClass owner)
		{
			if (_menuData == null)
			{
				_menuData = new Dictionary<UInt32, LimnorContextMenuCollection>();
			}
			LimnorContextMenuCollection data;
			if (!_menuData.TryGetValue(owner.MemberId, out data))
			{
				data = new LimnorContextMenuCollection(owner);
				_menuData.Add(owner.MemberId, data);
			}
			return data;
		}
	}// class
}// namespace
