/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Database support
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using VPL;
using XmlUtility;

namespace LimnorDatabase
{
	[ToolboxBitmapAttribute(typeof(DataGridViewEx), "Resources.dgvEx.bmp")]
	public class DataGridViewEx : DataGridView, IDataGrid, IDevClassReferencer, IPostDeserializeProcess, IPostXmlNodeSerialize
	{
		public DataGridViewEx()
		{
		}
#if Limnor56
		[Editor(typeof(CustomColumnCollectionEditor), typeof(UITypeEditor))]
		[Category("Database")]
		public new DataGridViewColumnCollection Columns
		{
			get
			{
				return base.Columns;
			}
		}
#endif
		#region private methods
		private EasyDataSet getEasyDataSet()
		{
			EasyDataSet eds = this.DataSource as EasyDataSet;
			if (eds != null)
				return eds;
			BindingSource bs = this.DataSource as BindingSource;
			if (bs != null)
			{
				return bs.DataSource as EasyDataSet;
			}
			return null;
		}
		private FieldList getFields()
		{
			FieldList flds = new FieldList();
			EasyDataSet eds = getEasyDataSet();
			if (eds != null)
			{
				flds = eds.Fields;
			}
			return flds;
		}
		#endregion

		#region Columns preserve
		[NotForProgramming]
		[Browsable(false)]
		public void PreserveColumns()
		{
			_cols = new List<DataGridViewColumn>();
			for (int i = 0; i < this.Columns.Count; i++)
			{
				DataGridViewColumn c = new DataGridViewColumn();
				_cols.Add(c);
				c.DataPropertyName = this.Columns[i].DataPropertyName;
				c.HeaderText = this.Columns[i].HeaderText;
				c.AutoSizeMode = this.Columns[i].AutoSizeMode;
				c.DefaultCellStyle = this.Columns[i].DefaultCellStyle;
				c.DefaultHeaderCellType = this.Columns[i].DefaultHeaderCellType;
				c.DividerWidth = this.Columns[i].DividerWidth;
				c.FillWeight = this.Columns[i].FillWeight;
				c.Frozen = this.Columns[i].Frozen;
				c.MinimumWidth = this.Columns[i].MinimumWidth;
				c.Name = this.Columns[i].Name;
				c.ReadOnly = this.Columns[i].ReadOnly;
				if (this.Columns[i].Resizable != DataGridViewTriState.NotSet)
				{
					c.Resizable = this.Columns[i].Resizable;
				}
				c.SortMode = this.Columns[i].SortMode;
				c.Tag = this.Columns[i].Tag;
				c.ToolTipText = this.Columns[i].ToolTipText;
				if (this.Columns[i].ValueType == null)
				{
					c.ValueType = this.Columns[i].ValueType;
				}
				c.Visible = this.Columns[i].Visible;
				c.Width = this.Columns[i].Width;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public void RestoreColumns()
		{
			OnDeserialize(null);
		}
		#endregion
		#region IDataGrid members
		[NotForProgramming]
		[Browsable(false)]
		public void OnChangeColumns(DataGridViewColumnCollection cs)
		{
			if (cs != null)
			{
				EasyDataSet eds = getEasyDataSet();
				if (eds != null)
				{
					FieldList flds0 = eds.Fields;
					FieldList flds = new FieldList();
					for (int c = 0; c < cs.Count; c++)
					{
						EPField f = flds0[cs[c].DataPropertyName];
						if (f == null)
						{
							f = new EPField();
							f.Name = cs[c].DataPropertyName;
						}
						f.FieldCaption = cs[c].HeaderText;
						f.OleDbType = EPField.ToOleDBType(cs[c].ValueType);
						flds.Add(f);
					}
					eds.Fields = flds;
				}
			}
		}

		[Browsable(false)]
		[ReadOnly(true)]
		[NotForProgramming]
		public bool DisableColumnChangeNotification { get; set; }
		#endregion
		#region IDevClassReferencer Members
		private IDevClass _holder;
		public void SetDevClass(IDevClass c)
		{
			_holder = c;
		}
		public IDevClass GetDevClass()
		{
			return _holder;
		}
		#endregion
		#region IPostDeserializeProcess Members
		public void OnDeserialize(object context)
		{
			if (Site == null || !Site.DesignMode)
			{
				return;
			}
			FieldList flds = null;
			EasyDataSet eds = getEasyDataSet();
			if (eds != null)
			{
				flds = eds.Fields;
			}
			for (int i = 0; i < this.Columns.Count; i++)
			{
				bool set = false;
				if (_cols != null && _cols.Count > 0)
				{
					for (int j = 0; j < _cols.Count; j++)
					{
						if (string.Compare(this.Columns[i].DataPropertyName, _cols[j].DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							set = true;
							this.Columns[i].HeaderText = _cols[j].HeaderText;
							this.Columns[i].AutoSizeMode = _cols[j].AutoSizeMode;
							this.Columns[i].DefaultCellStyle = _cols[j].DefaultCellStyle;
							this.Columns[i].DefaultHeaderCellType = _cols[j].DefaultHeaderCellType;
							this.Columns[i].DividerWidth = _cols[j].DividerWidth;
							this.Columns[i].FillWeight = _cols[j].FillWeight;
							this.Columns[i].Frozen = _cols[j].Frozen;
							this.Columns[i].MinimumWidth = _cols[j].MinimumWidth;
							this.Columns[i].Name = _cols[j].Name;
							this.Columns[i].ReadOnly = _cols[j].ReadOnly;
							if (_cols[j].Resizable != DataGridViewTriState.NotSet)
							{
								this.Columns[i].Resizable = _cols[j].Resizable;
							}
							this.Columns[i].SortMode = _cols[j].SortMode;
							this.Columns[i].Tag = _cols[j].Tag;
							this.Columns[i].ToolTipText = _cols[j].ToolTipText;
							if (_cols[j].ValueType == null)
							{
								this.Columns[i].ValueType = _cols[j].ValueType;
							}
							this.Columns[i].Visible = _cols[j].Visible;
							this.Columns[i].Width = _cols[j].Width;
							break;
						}
					}
				}
				if (!set)
				{
					EPField f = flds[this.Columns[i].DataPropertyName];
					if (f != null && !string.IsNullOrEmpty(f.FieldCaption) && string.CompareOrdinal(f.FieldCaption, this.Columns[i].HeaderText) != 0)
					{
						this.Columns[i].HeaderText = f.FieldCaption;
					}
				}
			}
		}
		#endregion
		#region IPostXmlNodeSerialize Members
		private List<DataGridViewColumn> _cols;
		const string XML_Columns = "Columns";
		[Browsable(false)]
		[NotForProgramming]
		public void OnReadFromXmlNode(IXmlCodeReader serializer, XmlNode node)
		{
			XmlNode nd = node.SelectSingleNode(XML_Columns);
			if (nd != null)
			{
				_cols = new List<DataGridViewColumn>();
				XmlNodeList nds = nd.SelectNodes(XmlTags.XML_Item);
				foreach (XmlNode iNode in nds)
				{
					DataGridViewColumn c = new DataGridViewColumn();
					c.SortMode = DataGridViewColumnSortMode.Automatic;
					_cols.Add(c);
					serializer.ReadObjectFromXmlNode(iNode, c, _cols.GetType(), _cols);
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void OnWriteToXmlNode(IXmlCodeWriter serializer, XmlNode node)
		{
			if (this.Columns != null && this.Columns.Count > 0)
			{
				XmlNode nd = XmlUtil.CreateSingleNewElement(node, XML_Columns);
				serializer.WriteObjectToNode(nd, this.Columns);
			}
		}

		#endregion
	}
}
