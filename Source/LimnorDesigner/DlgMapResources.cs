/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LimnorDesigner.Property;

namespace LimnorDesigner.ResourcesManager
{
	public partial class DlgMapResources : Form
	{
		private ClassPointer _pointer;
		private DataTable _table;
		private Button bt;
		private List<IProperty> _deletedImages;
		public DlgMapResources()
		{
			InitializeComponent();
			_deletedImages = new List<IProperty>();
			bt = new Button();
			bt.BackColor = System.Drawing.Color.FromArgb(255, 236, 233, 216);
			bt.Text = "...";
			bt.Parent = dataGridView1;
			bt.Visible = false;
			bt.Click += new EventHandler(bt_Click);
		}

		void bt_Click(object sender, EventArgs e)
		{
			if (dataGridView1.CurrentCell != null)
			{
				int r = dataGridView1.CurrentCell.RowIndex;
				DlgSelectResourceName dlg = new DlgSelectResourceName();
				dlg.LoadData(_pointer.Project.GetProjectSingleData<ProjectResources>());
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					_table.Rows[r][1] = dlg.SelectedResource;
					dataGridView1.Refresh();
				}
			}
		}
		public void LoadData(ClassPointer pointer)
		{
			_pointer = pointer;
			ProjectResources rm = _pointer.Project.GetProjectSingleData<ProjectResources>();
			Dictionary<IProperty, UInt32> map = _pointer.ResourceMap;
			_table = new DataTable();
			_table.Columns.Add("Property", typeof(object));
			_table.Columns.Add("Resource name", typeof(object));
			Dictionary<IProperty, UInt32>.Enumerator en = map.GetEnumerator();
			while (en.MoveNext())
			{
				ResourcePointer rp = rm.GetResourcePointerById(en.Current.Value);
				if (rp != null)
				{
					_table.Rows.Add(en.Current.Key, rp);
				}
			}
			dataGridView1.DataSource = _table;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			Dictionary<IProperty, UInt32> map = new Dictionary<IProperty, uint>();
			for (int i = 0; i < _table.Rows.Count; i++)
			{
				if (_table.Rows[i][0] != null && _table.Rows[i][0] != DBNull.Value && _table.Rows[i][1] != null && _table.Rows[i][1] != DBNull.Value)
				{
					IProperty p = (IProperty)(_table.Rows[i][0]);
					ResourcePointer rp = (ResourcePointer)(_table.Rows[i][1]);
					map.Add(p, rp.MemberId);
				}
			}
			_pointer.SetResourceMap(map);
			if (_deletedImages.Count > 0)
			{
				foreach (IProperty p in _deletedImages)
				{
					try
					{
						p.SetValue(null);
					}
					catch
					{
					}
				}
			}
			this.DialogResult = DialogResult.OK;
		}

		private void buttonInsert_Click(object sender, EventArgs e)
		{
			FrmObjectExplorer dlg = DesignUtil.GetPropertySelector(null, null, new DataTypePointer(new TypePointer(typeof(IProperty))));
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				IProperty p = dlg.SelectedObject as IProperty;
				if (p != null)
				{
					bool bFound = false;
					for (int i = 0; i < _table.Rows.Count; i++)
					{
						IProperty p0 = _table.Rows[i][0] as IProperty;
						if (p0 != null)
						{
							if (p0.IsSameObjectRef(p))
							{
								dataGridView1.Rows[i].Selected = true;
								bFound = true;
								break;
							}
						}
					}
					if (!bFound)
					{
						_table.Rows.Add(p, null);
						dataGridView1.Refresh();
					}
				}
			}
		}

		private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
		{

		}
		private void showButton(int nCurrentRowIndex)
		{
			int nCurrentCellColumn = 1;
			System.Drawing.Rectangle rc = dataGridView1.GetCellDisplayRectangle(nCurrentCellColumn, nCurrentRowIndex, true);
			bt.SetBounds(rc.Left + rc.Width - 20, rc.Top, 20, rc.Height);
			bt.Visible = true;
			bt.BringToFront();
		}
		private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0)
			{
				showButton(e.RowIndex);
			}
		}

		private void buttonDel_Click(object sender, EventArgs e)
		{
			if (dataGridView1.CurrentCell != null)
			{
				if (dataGridView1.CurrentCell.RowIndex >= 0)
				{
					try
					{
						IProperty p = _table.Rows[dataGridView1.CurrentCell.RowIndex][0] as IProperty;
						if (p != null)
						{
							if (p.ObjectType != null)
							{
								if (typeof(Image).Equals(p.ObjectType))
								{
									if (!_deletedImages.Contains(p))
									{
										_deletedImages.Add(p);
									}
								}
							}
						}
					}
					catch
					{
					}
					_table.Rows.RemoveAt(dataGridView1.CurrentCell.RowIndex);
					dataGridView1.Refresh();
				}
			}
		}

		private void DlgMapResources_Resize(object sender, EventArgs e)
		{
			if (dataGridView1.CurrentCell != null)
			{
				if (dataGridView1.CurrentCell.RowIndex >= 0)
				{
					showButton(dataGridView1.CurrentCell.RowIndex);
				}
			}
		}
	}
}
