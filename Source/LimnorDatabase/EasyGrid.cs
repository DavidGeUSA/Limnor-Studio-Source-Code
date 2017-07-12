/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Database support
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing.Design;
using System.Xml.Serialization;
using System.Drawing;
using System.Data.OleDb;
using VPL;
using System.Reflection;
using MathExp;
using System.CodeDom;
using System.Collections;
using System.Globalization;
using System.Collections.Specialized;
using Limnor.Net;
using ProgElements;
//http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=91605
namespace LimnorDatabase
{
	[DesignTimeColumnsHolder]
	[ToolboxBitmapAttribute(typeof(EasyGrid), "Resources.datagrid.bmp")]
	[Description("This control combines EasyQuery, DataSet and DataGridView into one component")]
	public class EasyGrid : DataGridView, IDatabaseAccess, IFieldListHolder, IReport32Usage, IMasterSource, ISourceValueEnumProvider, IDynamicMethodParameters, ICustomMethodCompiler, ICollection, IListSource, ITypedList, IBindingList, IPostDeserializeProcess, ICustomDataSource, IBindingContextHolder, IResetOnComponentChange, IDevClassReferencer, ICustomTypeDescriptor, INewObjectInit, IFieldsHolder, IMethodParameterAttributesProvider, IDataGrid, IControlDeserialize
	{
		#region fields and constructors
		private EasyQuery _query;
		private FieldList _cachedFields;
		private bool _isQuerying;
		private IDevClass _holder;
		private object _lock = new object();
		private ComboBox[] cbx = null; //look ups including Yes/No
		private Button[] bts = null; //datetime
		private Button btSave;
		private Button btNew;
		private bool _keepColumnSettings;
		private bool _allowAddNewrow = true;
		private BindingSource _bindingSource;
		private bool _bUpdating;
		private EventHandler _handleBeforeSQLChange;
		private EventHandler _handleAfterSQLChange;
		//
		DataGridViewCellStyle[] cellStyles = null;
		string[] names = null;
		//
		static private StringCollection _subClassReadOnlyProperties;
		static private StringCollection _subClassExcludeProperties;
		//
		static private Dictionary<DataGridView, List<DataGridViewColumn>> _oldCls;
		static private char _decimalSymbol;
		//
		private int _currentIdentity;
		private int _bindingIdentity;
		private int _leavingIdentity;
		//
		private List<int> _newIdentities;
		private List<DataGridViewColumn> _columnsCache;
		//
		private static void staticInit()
		{
			_decimalSymbol = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
			if (_subClassReadOnlyProperties == null)
			{
				_subClassReadOnlyProperties = new StringCollection();
				_subClassReadOnlyProperties.Add("Having");
				_subClassReadOnlyProperties.Add("GroupBy");
				_subClassReadOnlyProperties.Add("Where");
				_subClassReadOnlyProperties.Add("OrderBy");
				_subClassReadOnlyProperties.Add("Limit");
				_subClassReadOnlyProperties.Add("From");
				_subClassReadOnlyProperties.Add("ForReadOnly");
				_subClassReadOnlyProperties.Add("Fields");
				_subClassReadOnlyProperties.Add("WithTies");
				_subClassReadOnlyProperties.Add("Percent");
				_subClassReadOnlyProperties.Add("Top");
				_subClassReadOnlyProperties.Add("SqlTop");
				_subClassReadOnlyProperties.Add("Distinct");
				_subClassReadOnlyProperties.Add("UpdatableTableName");
				_subClassReadOnlyProperties.Add("DataPreparer");
				_subClassReadOnlyProperties.Add("TableName");
				_subClassReadOnlyProperties.Add("TableToUpdate");
				_subClassReadOnlyProperties.Add("Parameters");
			}
			if (_subClassExcludeProperties == null)
			{
				_subClassExcludeProperties = new StringCollection();
				//
				_subClassExcludeProperties.Add("Field_FieldText");
				_subClassExcludeProperties.Add("Field_IsIdentity");
				_subClassExcludeProperties.Add("Field_OleDbType");
				_subClassExcludeProperties.Add("Field_FromTableName");
				_subClassExcludeProperties.Add("Field_ReadOnly");
				_subClassExcludeProperties.Add("Field_DataSize");
				_subClassExcludeProperties.Add("Field_Indexed");
				_subClassExcludeProperties.Add("Field_Name");
				_subClassExcludeProperties.Add("Field_FieldCaption");
				_subClassExcludeProperties.Add("Field_Iscalculated");
				_subClassExcludeProperties.Add("Field_Editor");
				//
				//_subClassExcludeProperties.Add("Param_Value");
				_subClassExcludeProperties.Add("Param_OleDbType");
				_subClassExcludeProperties.Add("Param_Name");
				_subClassExcludeProperties.Add("SqlParameternames");
				_subClassExcludeProperties.Add("Tables");
				_subClassExcludeProperties.Add("SqlQuery");
			}
		}
		static EasyGrid()
		{
			staticInit();
		}
		public static void OnAddCell(DataGridViewColumn cell)
		{
			if (cell != null && cell.DataGridView != null && _oldCls != null)
			{
				List<DataGridViewColumn> cls;
				if (_oldCls.TryGetValue(cell.DataGridView, out cls))
				{
					if (cls != null && cls.Count > 0)
					{
						foreach (DataGridViewColumn c in cls)
						{
							if (c != null)
							{
								if (string.Compare(c.DataPropertyName, cell.DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
								{
									try
									{
										if (cell.Site != null)
										{
											cell.Site.Name = c.Name;
										}
										cell.Name = c.Name;
										cell.Width = c.Width;
										cell.Visible = c.Visible;
										cell.SortMode = c.SortMode;
										if (c.ReadOnly) cell.ReadOnly = c.ReadOnly;
										cell.HeaderText = c.HeaderText;
										cell.AutoSizeMode = c.AutoSizeMode;
										cell.Frozen = c.Frozen;
										cell.FillWeight = c.FillWeight;
										cell.DividerWidth = c.DividerWidth;
										cell.DefaultHeaderCellType = c.DefaultHeaderCellType;
										cell.DefaultCellStyle = c.DefaultCellStyle;
									}
									catch
									{
									}
									break;
								}
							}
						}
					}
				}
			}
		}
		public EasyGrid()
		{
			QueryOnStart = true;
			KeepCurrentRowOnSorting = false;
			btSave = new System.Windows.Forms.Button();
			btSave.Parent = this;
			btSave.Width = 20;
			btSave.Height = 20;
			btSave.Top = 0;
			btSave.Image = Resource1.save;
			btSave.Left = this.Width - btSave.Width;
			btSave.Anchor = AnchorStyles.Right | AnchorStyles.Top;
			btSave.BackColor = System.Drawing.Color.FromArgb(255, 236, 233, 216);
			btSave.Click += new System.EventHandler(onSaveButtonClick);
			btSave.Visible = false;
			//
			btNew = new System.Windows.Forms.Button();
			btNew.Parent = this;
			btNew.Width = 20;
			btNew.Height = 20;
			btNew.Top = 0;
			btNew.Image = Resource1._newItem;
			btNew.Left = this.Width - btSave.Width - btNew.Width;
			btNew.Anchor = AnchorStyles.Right | AnchorStyles.Top;
			btNew.BackColor = System.Drawing.Color.FromArgb(255, 236, 233, 216);
			btNew.Visible = false;
			btNew.Click += new System.EventHandler(onNewButtonClick);
			//
		}
		private bool _calculating;
		void EasyGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			if (!_calculating)
			{
				_calculating = true;
				try
				{
					if (CalculateCellValues != null)
					{
						CalculateCellValues(this, EventArgs.Empty);
						this.Refresh();
					}
				}
				catch
				{
				}
				_calculating = false;
			}
		}
		#endregion
		#region Events
		[Description("Occurs when a DragDrop event occurs on a cell")]
		public event EventHandlerGridViewDragDrop DropOnCell;

		[Description("Occurs when another row becomes the current")]
		public event EventHandler CurrentRowIndexChanged;

		[Description("Occurs when the Query method fails. The ErrorMessage property gives information about why the execution failed.")]
		public event EventHandler ExecutionError = null;

		[Description("Occurs when the user select an item from a dropdown list for editing a field.")]
		public event EventHandler Lookup;

		[Description("Occurs when data are retrieved from the database")]
		public event EventHandler DataFilled;

		[Description("Occurs when the user clicks on the new row and a new temporary auto-number is generated if an auto-number field is included.")]
		public event EventHandlerNewRowID CreatedNewRowID;
		#endregion
		#region Properties
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
		[Description("Error message from the last failed database operation")]
		public string LastError
		{
			get
			{
				return QueryDef.LastError;
			}
		}
		[Description("Gets and sets a Boolean indicating whether current row should be kept selected on sorting. It only works when an auto-number is included.")]
		[DefaultValue(false)]
		public bool KeepCurrentRowOnSorting { get; set; }

		private bool _hidesavebutton = false;
		[Description("Gets and sets a Boolean indicating whether to show a 'save' button at the top-right corner if the control is not read-only.")]
		[DefaultValue(false)]
		public bool HideSaveButton
		{
			get
			{
				return _hidesavebutton;
			}
			set
			{
				_hidesavebutton = value;
				if (btSave != null)
				{
					btSave.Visible = !_hidesavebutton;
				}
			}
		}
		[Category("Database")]
		[DefaultValue(true)]
		[Description("Indicates whether to make database query when this object is created.")]
		public bool QueryOnStart
		{
			get;
			set;
		}
		[Category("Database")]
		[Description("Name of the DataTable holding the data for this instance")]
		public string DataName
		{
			get
			{
				return TableName;
			}
		}
		[IgnoreDefaultValue]
		[Browsable(false)]
		public string TableName
		{
			get
			{
				return QueryDef.TableName;
			}
			set
			{
				QueryDef.TableName = value;
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		[NotForProgramming]
		public bool DisableColumnChangeNotification { get; set; }

		[Category("Database")]
		[XmlIgnore]
		[Description("Gets and sets an integer indicating the current row. When retrieving values by Fields property, this property indicates the row number. 0 indicates the first row; 1 indicates the second row, and so on.")]
		public int RowIndex
		{
			get
			{
				return QueryDef.RowIndex;
			}
			set
			{
				if (value >= 0)
				{
					if (QueryDef.DataTable != null)
					{
						if (QueryDef.DataTable.Rows.Count > value)
						{
							QueryDef.RowIndex = value;
						}
					}
				}
			}
		}
		[Category("Database")]
		[Description("Accessing this property will test connect to the database. If the connection is made then this property is True; otherwise it is False.")]
		public bool IsConnectionReady
		{
			get
			{
				return QueryDef.IsConnectionReady;
			}
		}
		[Browsable(false)]
		[Description("The database query used by this control.")]
		public EasyQuery QueryDef
		{
			get
			{
				if (_query == null)
				{
					_query = new EasyQuery();
					_query.DataStorage = new DataSet(Guid.NewGuid().ToString());
					_query.EmptyRecordHandler = OnEmptyRecord;
					_query.BeforeDeleteRecordHandler = onBeforeDeleteRecord;
					_query.AfterDeleteRecordHandler = onAfterDeleteRecord;
					_query.SetDesignMode(Site != null && Site.DesignMode);
					_query.LastErrorMessageChanged += new EventHandler(_query_LastErrorMessageChanged);
					_query.CreatedNewRowID += _query_CreatedNewRowID;
					OnQueryObjectCreated();
				}
				return _query;
			}
		}

		void _query_CreatedNewRowID(object sender, EventArgsNewRowID e)
		{
			_currentIdentity = e.RowId;
			if (_newIdentities == null)
			{
				_newIdentities = new List<int>();
			}
			if (!_newIdentities.Contains(_currentIdentity))
			{
				_newIdentities.Add(_currentIdentity);
			}
			if (CreatedNewRowID != null)
			{
				CreatedNewRowID(this, e);
			}
		}
		private bool isNewIdentity(int id)
		{
			if (id != 0 && _newIdentities != null)
			{
				return _newIdentities.Contains(id);
			}
			return false;
		}
		protected virtual void OnQueryObjectCreated()
		{
		}
		void _query_LastErrorMessageChanged(object sender, EventArgs e)
		{
			if (ExecutionError != null)
			{
				ExecutionError(sender, e);
			}
		}
		[Browsable(false)]
		[Description("The DataSet holding the data used by this control")]
		public DataSet DataStorage
		{
			get
			{
				if (_query != null)
				{
					return _query.DataStorage;
				}
				return null;
			}
		}
		[Browsable(false)]
		[Description("The DataTable holding the data used by this control")]
		public DataTable DataTable
		{
			get
			{
				if (_query != null)
				{
					return _query.DataTable;
				}
				return null;
			}
		}
		[Browsable(false)]
		[Description("The rows holding the data used by this control")]
		public DataRowCollection DataRows
		{
			get
			{
				if (_query != null && _query.DataTable != null)
				{
					return _query.DataTable.Rows;
				}
				return null;
			}
		}
		[Browsable(false)]
		[Description("The number of data rows holding the data used by this control")]
		public int DataRowCount
		{
			get
			{
				if (_query != null && _query.DataTable != null && _query.DataTable.Rows != null)
				{
					return _query.DataTable.Rows.Count;
				}
				return 0;
			}
		}
		public object[][] DataArray
		{
			get
			{
				lock (_lock)
				{
					DataRowCollection rs = DataRows;
					if (rs == null)
					{
						object[][] data = new object[0][];
						return data;
					}
					else
					{
						int rCount = rs.Count;
						int cCount = 0;
						object[][] data = new object[rCount][];
						for (int i = 0; i < rCount; i++)
						{
							cCount = rs[i].ItemArray.Length;
							data[i] = new object[cCount];
							for (int j = 0; j < cCount; j++)
							{
								data[i][j] = rs[i].ItemArray[j];
							}
						}
						return data;
					}
				}
			}
		}
		[Browsable(false)]
		internal DataTable CurrentDataTable
		{
			get
			{
				if (_query != null)
				{
					return _query.CurrentDataTable;
				}
				return null;
			}
		}
		[Category("Database")]
		[ReadOnly(true)]
		[Editor(typeof(HideUITypeEditor), typeof(UITypeEditor))]
		[Description("Fields in this data table. It represents the current row at runtime.")]
		public FieldList Fields
		{
			get
			{
				FieldList fs = QueryDef.Fields;
				for (int i = 0; i < fs.Count; i++)
				{
					fs[i].editor = null;
				}
				if (_editorList != null)
				{
					foreach (DataEditor de in _editorList)
					{
						EPField f = fs[de.ValueField];
						if (f != null)
						{
							f.editor = de;
						}
					}
				}
				_cachedFields = fs;
				DataRow dr = currentDataRow;
				if (dr != null && dr.ItemArray != null && fs.Count == dr.ItemArray.Length)
				{
					if (dr.RowState != DataRowState.Deleted && dr.RowState != DataRowState.Detached)
					{
						for (int i = 0; i < fs.Count; i++)
						{
							fs[i].SetValue(dr[i]);
						}
					}
				}
				return fs;
			}
			set
			{
				QueryDef.Fields = value;
				FieldList fs = QueryDef.Fields;
				for (int i = 0; i < fs.Count; i++)
				{
					fs[i].editor = null;
					fs[i].calculator = null;
				}
				if (_editorList != null)
				{
					foreach (DataEditor de in _editorList)
					{
						EPField f = fs[de.ValueField];
						if (f != null)
						{
							f.editor = de;
						}
					}
				}
				_cachedFields = fs;
			}
		}
		[Category("Database")]
		[Description("Parameters for filtering the data.")]
		public DbParameterList Parameters
		{
			get
			{
				return new DbParameterList(QueryDef.Parameters);
			}
		}
		[Browsable(false)]
		public virtual Guid ConnectionID
		{
			get
			{
				if (QueryDef != null)
					return QueryDef.ConnectionID;
				return Guid.Empty;
			}
			set
			{
				if (QueryDef != null)
				{
					QueryDef.ConnectionID = value;
				}
			}
		}

		[XmlIgnore]
		[ReadOnly(true)]
		[Browsable(false)]
		public string SqlQuery
		{
			get
			{
				return QueryDef.SqlQuery;
			}
			set
			{
				QueryDef.SetDesignMode((Site != null && Site.DesignMode));
				QueryDef.SqlQuery = value;
			}
		}
		[Category("Database")]
		[Description("Gets a value indicating whether RowCount is greater than 0")]
		public virtual bool HasData
		{
			get
			{
				return QueryDef.HasData;
			}
		}
		[Category("Database")]
		[Description("Gets a value indicating whether current row is valid to be used. ")]
		public bool HasValidCurrentRow
		{
			get
			{
				if (HasData)
				{
					DataRow dr = currentDataRow;
					if (dr != null)
					{
						if (dr.RowState == DataRowState.Deleted || dr.RowState == DataRowState.Detached)
						{
							return false;
						}
						return true;
					}
				}
				return false;
			}
		}
		[Category("Query")]
		[ReadOnly(true)]
		[TypeConverter(typeof(TypeConverterSQLString))]
		[XmlIgnore]
		[Description("SQL statement for querying database")]
		[Editor(typeof(UIQueryEditor), typeof(UITypeEditor))]
		public SQLStatement SQL
		{
			get
			{
				return QueryDef.SQL;
			}
			set
			{
				EasyQuery.LogMessage2("{0} - Setting {1}.SQL Starts...\r\n\tvalue:{2}\r\n\tdesign mode:{3}\r\n\r\n", TableName, this.GetType().FullName, value, DesignMode);
				List<DataGridViewColumn> cls = null;
				cls = new List<DataGridViewColumn>();
				for (int i = 0; i < Columns.Count; i++)
				{
					cls.Add(Columns[i]);
				}
				if (_oldCls == null)
				{
					_oldCls = new Dictionary<DataGridView, List<DataGridViewColumn>>();
				}
				if (_oldCls.ContainsKey(this))
				{
					_oldCls[this] = cls;
				}
				else
				{
					_oldCls.Add(this, cls);
				}
				IVplDesignerService ivs = VPLUtil.GetServiceByName("EasyGridCols") as IVplDesignerService;
				if (ivs != null)
				{
					ivs.OnRequestService(this, "BeforeSetSQL");
				}
				DataSource = null;
				if (_handleBeforeSQLChange != null)
				{
					_handleBeforeSQLChange(this, EventArgs.Empty);
				}
				if (DesignMode)
				{
					QueryDef.RemoveTable(TableName);
					OnRemoveTable();
					AutoGenerateColumns = true;
				}
				if (value != null)
				{
					if (!(DesignMode))
					{
						if (!_keepColumnSettings)
						{
							Columns.Clear();
							AutoGenerateColumns = true;
						}
					}
				}
				Refresh();
				clearEditorControls();
				QueryDef.ResetCanChangeDataSet(false);
				QueryDef.SQL = value;
				//
				if (DesignMode)
				{
					//QueryDef.SQL will not do query at design time. do it now
					QueryDef.Query();
				}
				//
				OnBindDataSource();
				//
				if (cls != null && cls.Count > 0)
				{
					//restore column attributes
					for (int i = 0; i < Columns.Count; i++)
					{
						for (int j = 0; j < cls.Count; j++)
						{
							if (string.Compare(cls[j].DataPropertyName, Columns[i].DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
							{
								if (Columns[i].Site != null)
								{
									Columns[i].Site.Name = cls[j].Name;
								}
								Columns[i].Name = cls[j].Name;
								Columns[i].Width = cls[j].Width;
								Columns[i].Visible = cls[j].Visible;
								Columns[i].SortMode = cls[j].SortMode;
								if (cls[j].ReadOnly)
								{
									Columns[i].ReadOnly = cls[j].ReadOnly;
								}
								Columns[i].HeaderText = cls[j].HeaderText;
								Columns[i].Frozen = cls[j].Frozen;
								Columns[i].FillWeight = cls[j].FillWeight;
								Columns[i].DividerWidth = cls[j].DividerWidth;
								Columns[i].DefaultHeaderCellType = cls[j].DefaultHeaderCellType;
								Columns[i].DefaultCellStyle = cls[j].DefaultCellStyle;
								Columns[i].AutoSizeMode = cls[j].AutoSizeMode;
								DataGridViewTextBoxColumn dvct = Columns[i] as DataGridViewTextBoxColumn;
								if (dvct != null)
								{
									DataGridViewTextBoxColumn dvct0 = cls[j] as DataGridViewTextBoxColumn;
									if (dvct0 != null)
									{
										dvct.MaxInputLength = dvct0.MaxInputLength;
									}
								}
								break;
							}
						}
					}
				}
				//
				createEditorControls();
				//
				if (_handleAfterSQLChange != null)
				{
					_handleAfterSQLChange(this, EventArgs.Empty);
				}
				//
				if (ivs != null)
				{
					ivs.OnRequestService(this, "AfterSetSQL");
				}
				//
				EasyQuery.LogMessage2("{0} - Setting {1}.SQL ends---", TableName, this.GetType().FullName);
			}
		}
		[DefaultValue(false)]
		[Category("Query")]
		[Description("Indicates whether duplicated records are allowed")]
		public bool Distinct
		{
			get
			{
				return QueryDef.Distinct;
			}
			set
			{
				QueryDef.Distinct = value;
			}
		}
		[Category("Query")]
		[Description("Specifies that the query result contain a specific number of rows or a percentage of rows of the query result. Following keyword TOP, you can specify 1 to 32,767 rows, or, if you include the PERCENT option, you can specify 0.01 to 99.99 percent. ")]
		public int SqlTop
		{
			get
			{
				return QueryDef.Top;
			}
			set
			{
				QueryDef.Top = value;
			}
		} // include TOP <Top>
		[DefaultValue(false)]
		[Category("Query")]
		[Description("Specifies that the query result contain a specific number of rows or a percentage of rows of the query result. Following keyword TOP, you can specify 1 to 32,767 rows, or, if you include the PERCENT option, you can specify 0.01 to 99.99 percent. ")]
		public bool Percent
		{
			get
			{
				return QueryDef.Percent;
			}
			set
			{
				QueryDef.Percent = value;
			}
		}
		[DefaultValue(false)]
		[Category("Query")]
		[Description("Specifies that additional rows be returned from the base result set with the same value in the ORDER BY columns appearing as the last of the TOP n (PERCENT) rows. TOP…WITH TIES can be specified only in SELECT statements, and only if an ORDER BY clause is specified.")]
		public bool WithTies
		{
			get
			{
				return QueryDef.WithTies;
			}
			set
			{
				QueryDef.WithTies = value;
			}
		}
		[Browsable(false)]
		[Description("The table that can be updated")]
		public string TableToUpdate
		{
			get
			{
				return QueryDef.TableToUpdate;
			}
			set
			{
				QueryDef.UpdatableTableName = value;
			}
		}
		[Category("Database")]
		[DefaultValue(false)]
		[Description("Indicate whether the changing of data is desired")]
		public bool ForReadOnly
		{
			get
			{
				return QueryDef.ForReadOnly;
			}
			set
			{
				if (QueryDef.ForReadOnly != value)
				{
					QueryDef.ForReadOnly = value;
					if (!value)
					{
						if (Site != null && Site.DesignMode)
						{
							if (QueryReady)
							{
								SQL = QueryDef.SQL;
							}
						}
					}
				}
			}
		}
		[Category("Database")]
		[DefaultValue(false)]
		[Description("True: the changes will be automatically saved back to the database when the form is closed; False: the changes will be saved to the database only by UpdateData action")]
		public bool AutoSave
		{
			get;
			set;

		}

		[Category("Database")]
		[Description("gets and sets sql scripts used before SELECT, separated by a semicolon")]
		public string DataPreparer
		{
			get
			{
				return QueryDef.DataPreparer;
			}
			set
			{
				QueryDef.DataPreparer = value;
			}
		}

		[Category("Query")]
		[Description("'FROM' clause of the SELECT statement")]
		public string From
		{
			get
			{
				return QueryDef.From;
			}
			set
			{
				QueryDef.From = value;
			}
		}

		[Category("Query")]
		[Description("'LIMIT' clause of the SELECT statement")]
		public string Limit
		{
			get { return QueryDef.Limit; }
			set { QueryDef.Limit = value; }
		}

		[Category("Query")]
		[Description("'WHERE' clause of the SELECT statement")]
		public string Where
		{
			get
			{
				return QueryDef.Where;
			}
			set
			{
				QueryDef.Where = value;
			}
		}
		[Category("Query")]
		[Description("'GROUP BY' clause of the SELECT statement")]
		public string GroupBy
		{
			get
			{
				return QueryDef.GroupBy;
			}
			set
			{
				QueryDef.GroupBy = value;
			}
		}
		[Category("Query")]
		[Description("'HAVING' clause of the SELECT statement")]
		public string Having
		{
			get
			{
				return QueryDef.Having;
			}
			set
			{
				QueryDef.Having = value;
			}
		}

		[Category("Query")]
		[Description("'ORDER BY' clause of the SELECT statement")]
		public string OrderBy
		{
			get
			{
				return QueryDef.OrderBy;
			}
			set
			{
				QueryDef.OrderBy = value;
			}
		}
		/// <summary>
		/// for setting default connection
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public virtual string DefaultConnectionString
		{
			get
			{
				return QueryDef.DefaultConnectionString;
			}
			set
			{
				QueryDef.DefaultConnectionString = value;
			}
		}
		/// <summary>
		/// for setting default connection
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public virtual Type DefaultConnectionType
		{
			get
			{
				return QueryDef.DefaultConnectionType;
			}
			set
			{
				QueryDef.DefaultConnectionType = value;
			}
		}
		[Description("Indicates the file path for logging errors")]
		public static string LogFile
		{
			get
			{
				return EasyQuery.LogFile;
			}
		}
		[DefaultValue(true)]
		[Description("Controls whether error message is displayed when an error occurs. Error messages are logged in a text file. The file path is indicated by LogFile property.")]
		public bool ShowErrorMessage
		{
			get
			{
				return QueryDef.ShowErrorMessage;
			}
			set
			{
				QueryDef.ShowErrorMessage = value; ;
			}
		}
		#endregion
		#region Fields Serialization
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_Name
		{
			get
			{
				return QueryDef.Field_Name;
			}
			set
			{
				QueryDef.Field_Name = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_ReadOnly
		{
			get
			{
				return QueryDef.Field_ReadOnly;
			}
			set
			{
				QueryDef.Field_ReadOnly = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public OleDbType[] Field_OleDbType
		{
			get
			{
				return QueryDef.Field_OleDbType;
			}
			set
			{
				QueryDef.Field_OleDbType = value;
				_allowAddNewrow = this.AllowUserToAddRows;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_IsIdentity
		{
			get
			{
				return QueryDef.Field_IsIdentity;
			}
			set
			{
				QueryDef.Field_IsIdentity = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_Indexed
		{
			get
			{
				return QueryDef.Field_Indexed;
			}
			set
			{
				QueryDef.Field_Indexed = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public int[] Field_DataSize
		{
			get
			{
				return QueryDef.Field_DataSize;
			}
			set
			{
				QueryDef.Field_DataSize = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_FromTableName
		{
			get
			{
				return QueryDef.Field_FromTableName;
			}
			set
			{
				QueryDef.Field_FromTableName = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_FieldText
		{
			get
			{
				return QueryDef.Field_FieldText;
			}
			set
			{
				QueryDef.Field_FieldText = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_Expression
		{
			get
			{
				return QueryDef.Field_Expression;
			}
			set
			{
				QueryDef.Field_Expression = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_Iscalculated
		{
			get
			{
				return QueryDef.Field_IsCalculated;
			}
			set
			{
				QueryDef.Field_IsCalculated = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_Editor
		{
			get
			{
				string[] rets = null;
				if (_editorList != null)
				{
					rets = new string[_editorList.Count];
					for (int i = 0; i < _editorList.Count; i++)
					{
						rets[i] = _editorList[i].ConvertToString();
					}
				}
				return rets;
			}
			set
			{
				FieldList fs = Fields;
				_editorList = new FieldEditorList(this);
				if (value != null)
				{
					for (int i = 0; i < value.Length; i++)
					{
						DataEditor de = DataEditor.ConvertFromString(value[i]);
						if (de != null && !string.IsNullOrEmpty(de.ValueField))
						{
							de.OnLoad();
							_editorList.AddEditor(de);
							EPField f = fs[de.ValueField];
							if (f != null)
							{
								f.editor = de;
							}
						}
					}
				}
			}
		}
		private FieldEditorList _editorList;
		[Category("Database")]
		[Description("Field editors for editing cells at runtime by the user.")]
		[Editor(typeof(TypeEditorHide), typeof(UITypeEditor))]
		public FieldEditorList FieldEditors
		{
			get
			{
				if (_editorList == null)
				{
					_editorList = new FieldEditorList(this);
				}
				return _editorList;
			}
		}

		private FieldExpressionList _expList;
		[Category("Database")]
		[Description("Field expression for calculating cell values at runtime by the user.")]
		[Editor(typeof(TypeEditorHide), typeof(UITypeEditor))]
		public FieldExpressionList FieldExpressions
		{
			get
			{
				if (_expList == null)
				{
					_expList = new FieldExpressionList(this);
				}
				return _expList;
			}
		}
		[Browsable(false)]
		public string[] Field_FieldCaption
		{
			get
			{
				return QueryDef.Field_FieldCaption;
			}
			set
			{
				QueryDef.Field_FieldCaption = value;
			}
		}
		#endregion
		#region Parameters Serialization
		[NotForProgramming]
		[Browsable(false)]
		public string[] Param_Name
		{
			get
			{
				return QueryDef.Param_Name;
			}
			set
			{
				QueryDef.Param_Name = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public OleDbType[] Param_OleDbType
		{
			get
			{
				return QueryDef.Param_OleDbType;
			}
			set
			{
				QueryDef.Param_OleDbType = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Param_Value
		{
			get
			{
				return QueryDef.Param_Value;
			}
			set
			{
				QueryDef.Param_Value = value;
			}
		}

		#endregion
		#region protected overrides
		protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
		{
			base.OnEditingControlShowing(e);
			if (e != null && e.Control != null)
			{
				e.Control.KeyPress -= tb_KeyPress;
				if (this.CurrentCell != null)
				{
					OleDbType[] ts = Field_OleDbType;
					if (ts != null && this.CurrentCell.ColumnIndex >= 0 && this.CurrentCell.ColumnIndex < ts.Length)
					{
						if (EPField.IsNumber(ts[this.CurrentCell.ColumnIndex]))
						{
							TextBox tb = e.Control as TextBox;
							if (tb != null)
							{
								tb.KeyPress += tb_KeyPress;
							}
						}
					}
				}
			}
		}

		void tb_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
			{
				if (e.KeyChar == _decimalSymbol)
				{
					OleDbType[] ts = Field_OleDbType;
					if (ts != null && this.CurrentCell.ColumnIndex >= 0 && this.CurrentCell.ColumnIndex < ts.Length)
					{
						if (EPField.IsInteger(ts[this.CurrentCell.ColumnIndex]))
						{
							e.Handled = true;
						}
					}
				}
				else
				{
					e.Handled = true;
				}
			}

		}
		protected override void OnUserAddedRow(DataGridViewRowEventArgs e)
		{
			base.OnUserAddedRow(e);
		}
		protected override void OnCellLeave(DataGridViewCellEventArgs e)
		{
			base.OnCellLeave(e);
			if (isNewIdentity(_bindingIdentity))
			{
				_leavingIdentity = _bindingIdentity;
			}
			else
			{
				_bindingIdentity = 0;
			}
		}
		private void saveIdentity()
		{
			if (this.CurrentRow != null)
			{
				bool[] bs = Field_IsIdentity;
				if (bs != null)
				{
					for (int i = 0; i < bs.Length; i++)
					{
						if (bs[i])
						{
							_currentIdentity = Convert.ToInt32(this.CurrentRow.Cells[i].Value);
							break;
						}
					}
				}
			}
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (KeepCurrentRowOnSorting)
			{
				if (_bindingIdentity == _leavingIdentity && isNewIdentity(_leavingIdentity))
				{
					restoreCurrentRowSelection();
					_leavingIdentity = 0;
					_bindingIdentity = 0;
				}
				else
				{
					saveIdentity();
				}
			}
		}
		protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
		{
			base.OnCellMouseUp(e);
			if (KeepCurrentRowOnSorting)
			{
				if (e.RowIndex >= 0)
				{
					if (_bindingIdentity == _leavingIdentity && isNewIdentity(_leavingIdentity))
					{
						restoreCurrentRowSelection();
						_leavingIdentity = 0;
						_bindingIdentity = 0;
					}
					else
					{
						if (this.CurrentRow != null)
						{
							bool[] bs = Field_IsIdentity;
							if (bs != null)
							{
								for (int i = 0; i < bs.Length; i++)
								{
									if (bs[i])
									{
										_currentIdentity = Convert.ToInt32(this.CurrentRow.Cells[i].Value);
										break;
									}
								}
							}
						}
					}
				}
			}
		}
		private void restoreCurrentRowSelection()
		{
			if (KeepCurrentRowOnSorting && _currentIdentity != 0)
			{
				int n = -1;
				bool[] bs = Field_IsIdentity;
				if (bs != null)
				{
					for (int i = 0; i < bs.Length; i++)
					{
						if (bs[i])
						{
							n = i;
							break;
						}
					}
				}
				if (n >= 0 && n < this.Columns.Count)
				{
					for (int i = 0; i < this.Rows.Count; i++)
					{
						if (this.Rows[i].Cells[n].Value != DBNull.Value)
						{
							if (Convert.ToInt32(this.Rows[i].Cells[n].Value) == _currentIdentity)
							{
								if (this.RowIndex != i)
								{
									this.RowIndex = i;
									this.FirstDisplayedScrollingRowIndex = i;
								}
								break;
							}
						}
					}
				}
			}
		}
		protected override void OnSorted(EventArgs e)
		{
			restoreCurrentRowSelection();
			base.OnSorted(e);
		}
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			ShowNewButton(this.Enabled);
			btSave.Enabled = (this.Enabled && !ReadOnly && !ForReadOnly);
		}
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);

			Form f = this.FindForm();
			if (f != null && !f.IsDisposed && !f.Disposing)
			{
				f.FormClosing += new FormClosingEventHandler(f_FormClosing);
			}
		}

		void f_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (AutoSave)
			{
				UpdateData();
			}
		}
		private int getColumnIndexByTitle(string title)
		{
			for (int i = 0; i < this.Columns.Count; i++)
			{
				if (string.Compare(this.Columns[i].DataPropertyName, title, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return i;
				}
			}
			return -1;
		}
		[Browsable(false)]
		protected void processOnCellEnter()
		{
			if (!_bUpdating && !IsQuerying && this.Rows.Count > 0)//HasData)
			{
				try
				{
					FieldList fields = GetCachedFields();
					if (fields != null)
					{
						if (fields.Count > 0)
						{
							for (int i = 0; i < fields.Count; i++)
							{
								if (cbx != null && i < cbx.Length)
									if (cbx[i] != null)
										cbx[i].Visible = false;
								if (bts != null && i < bts.Length)
									if (bts[i] != null)
										bts[i].Visible = false;
							}
						}
					}
					if (currentCellInSynch() && !ReadOnly)
					{
						int nCurrentCellColumn = getCurrentFieldIndex();
						if (nCurrentCellColumn >= 0)
						{
							string fldName = getCurrentFieldName();
							int nCurrentRowIndex = CurrentCell.RowIndex;
							if (cbx != null && nCurrentCellColumn < cbx.Length)
							{
								if (cbx[nCurrentCellColumn] != null)
								{
									object v0 = CurrentCell.Value;
									System.Drawing.Rectangle rc = this.GetCellDisplayRectangle(CurrentCell.ColumnIndex, nCurrentRowIndex, true);
									cbx[nCurrentCellColumn].SetBounds(rc.Left, rc.Top, rc.Width, rc.Height);
									ComboLook cbxLK = cbx[nCurrentCellColumn] as ComboLook;
									if (cbxLK != null)
									{
										cbxLK.SelectedIndex = -1;
										DataRowView rv;
										for (int i = 0; i < cbxLK.Items.Count; i++)
										{
											object v;
											rv = cbxLK.Items[i] as DataRowView;
											if (rv != null)
											{
												v = rv[fldName];
											}
											else
											{
												v = cbxLK.Items[i];
											}
											if (LogicExp.Compare(v, enumLogicType2.Equal, v0))
											{
												cbxLK.bNoEvent = true;
												cbxLK.SelectedIndex = i;
												cbxLK.bNoEvent = false;
												break;
											}
										}
										if (cbxLK.SelectedIndex < 0)
										{
											cbxLK.SetSelectedIndex(v0);
										}
										//
									}
									else
									{
										cbx[nCurrentCellColumn].SelectedIndex = -1;
										cbx[nCurrentCellColumn].Text = ValueConvertor.ToString(v0);
									}
									cbx[nCurrentCellColumn].Visible = true;
									cbx[nCurrentCellColumn].BringToFront();
								}
							}
							if (columnEditable(nCurrentCellColumn))
							{
								if (bts != null && nCurrentCellColumn < bts.Length)
								{
									if (bts[nCurrentCellColumn] != null)
									{
										System.Drawing.Rectangle rc = this.GetCellDisplayRectangle(CurrentCell.ColumnIndex, nCurrentRowIndex, true);
										bts[nCurrentCellColumn].SetBounds(rc.Left + rc.Width - 20, rc.Top, 20, rc.Height);
										bts[nCurrentCellColumn].Visible = true;
										bts[nCurrentCellColumn].BringToFront();
										if (fields[nCurrentCellColumn].editor != null)
										{
											fields[nCurrentCellColumn].editor.currentValue = ValueConvertor.ToString(CurrentCell.Value);
										}
									}
								}
							}
						}
					}
					else
					{
					}
				}
				catch (Exception er)
				{
					FormLog.NotifyException(ShowErrorMessage, er, "Error showing editor");
				}
			}
		}
		protected override void OnCellEnter(DataGridViewCellEventArgs e)
		{
			if (!_bUpdating)
			{
				processOnCellEnter();
				base.OnCellEnter(e);
			}
		}
		protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
		{
			if (!_bUpdating)
			{
				if (e.RowIndex >= 0)
				{
					DataRow dr = null;
					DataRowView drv = Rows[e.RowIndex].DataBoundItem as DataRowView;
					if (drv != null)
					{
						if (drv.IsNew)
						{
							dr = drv.Row;
							if (dr.RowState == DataRowState.Detached)
							{
								DataTable tbl = CurrentDataTable;
								if (tbl != null)
								{
									tbl.Rows.Add(dr);
								}
							}
						}
					}
				}
				EasyGrid_CellValueChanged(this, e);
				base.OnCellValueChanged(e);
			}
		}
		protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValuePushed(e);
		}
		protected override void OnRowEnter(DataGridViewCellEventArgs e)
		{
			if (!_bUpdating)
			{
				base.OnRowEnter(e);
				if (e.RowIndex >= 0)
				{
					DataRow dr = null;
					DataRowView drv = Rows[e.RowIndex].DataBoundItem as DataRowView;
					if (drv != null)
					{
						dr = drv.Row;
					}
					if (Rows[e.RowIndex].IsNewRow)
					{
						if (EnterAddingRecord != null)
						{
							EnterAddingRecord(this, EventArgs.Empty);
						}
					}
					else
					{
						if (dr != null)
						{
							if (dr.RowState == DataRowState.Added)
							{
								if (EnterAddingRecord != null)
								{
									EnterAddingRecord(this, EventArgs.Empty);
								}
							}
							else
							{
								if (LeaveAddingRecord != null)
								{
									LeaveAddingRecord(this, EventArgs.Empty);
								}
							}
						}
						else
						{
							if (EnterAddingRecord != null)
							{
								EnterAddingRecord(this, EventArgs.Empty);
							}
						}
					}
				}
			}
		}
		private int _rowIndex = -1;
		protected override void OnCurrentCellChanged(EventArgs e)
		{
			base.OnCurrentCellChanged(e);
			if (this.CurrentCell == null)
			{
				_rowIndex = -1;
			}
			else
			{
				if (this.CurrentCell.RowIndex != _rowIndex)
				{
					_rowIndex = this.CurrentCell.RowIndex;
					if (CurrentRowIndexChanged != null)
					{
						CurrentRowIndexChanged(this, EventArgs.Empty);
					}
				}
			}
		}
		#endregion
		#region Methods
		[Description("Set the full file path for the log file for database activities. ")]
		public static void SetLogFilePath(string logFileName)
		{
			EasyQuery.SetLogFilePath(logFileName);
		}
		[Description("End editing the current record")]
		public void EndEditCurrentRecord()
		{
			if (IsQuerying)
				return;
			if (BindingContext != null && _bindingSource != null)
			{
				BindingManagerBase bm = BindingContext[_bindingSource];
				if (bm != null)
				{
					bm.EndCurrentEdit();
				}
			}
			QueryDef.EndEditCurrentRecord();
		}
		[Description("Cancel editing the current record")]
		public void CancelEditCurrentRecord()
		{
			if (IsQuerying)
				return;
			BindingManagerBase bm = null;
			if (BindingContext != null && _bindingSource != null)
			{
				bm = BindingContext[_bindingSource];
				if (bm != null)
				{
					bm.CancelCurrentEdit();
				}
			}
			QueryDef.CancelEditCurrentRecord();
			DataTable t = QueryDef.DataTable;
			if (t != null)
			{
				if (bm != null && bm.Position >= 0 && bm.Position < t.Rows.Count)
				{
					t.Rows[bm.Position].RejectChanges();
				}
			}
		}
		[Description("Discard all changes")]
		public new bool CancelEdit()
		{
			base.CancelEdit();
			this.RefreshEdit();
			DataTable t = QueryDef.DataTable;
			if (t != null)
			{
				t.RejectChanges();
			}
			return true;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void OnChangeColumns(DataGridViewColumnCollection cs)
		{
			if (cs != null)
			{
				FieldList flds0 = QueryDef.Fields;
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
				QueryDef.Fields = flds;
			}
		}
		[Description("Print the data grid, use a dialogue box to specify print parameters.")]
		public void Print()
		{
			PrintDGV.Print_DataGridView(this);
		}
		[Description("Print the data grid using specified print parameters.")]
		public void Print(string title, bool fitToPageWidth, bool selectedRows, bool preview, bool landscape, int pageWidth, int pageHeight, bool selectPrinter)
		{
			PrintDGV.Print_DataGridView(this, title, fitToPageWidth, selectedRows, preview, landscape, pageWidth, pageHeight, selectPrinter);
		}
		[Description("Save changed data to the database")]
		public new void Update()
		{
			UpdateData();
		}
		[Description("Causes the control to redraw the invalidated regions within its client area. Executes any pending requests for painting.")]
		public void UpdateScreen()
		{
			base.Update();
		}
		[Description("Use a dialogue box to make database query.")]
		public void SelectQuery(Form dialogParent)
		{
			QueryDef.SelectQuery(dialogParent);
		}
		[Description("Set connection string for accessing the database.")]
		public void SetConnectionString(string connectionString)
		{
			QueryDef.SetConnectionString(connectionString);
		}
		[Description("Create a data binding object. It is to be added to DataBindings property via DataBindings.Add(obj) method, where obj is created by this method.")]
		public Binding CreateDataBinding(string bindToPropertyName, string sourceFieldName)
		{
			return new Binding(bindToPropertyName, BindSource, sourceFieldName, true);
		}
		[Description("Calculate summary of the column specified by fieldName.")]
		public virtual double GetColumnSum(string fieldName)
		{
			double ret = 0;
			int n = -1;
			for (int i = 0; i < this.Columns.Count; i++)
			{
				if (string.Compare(fieldName, this.Columns[i].DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
				{
					n = i;
					break;
				}
			}
			if (n >= 0)
			{
				if (EPField.IsNumber(this.Columns[n].ValueType))
				{
					for (int i = 0; i < this.Rows.Count; i++)
					{
						object v = this.Rows[i].Cells[n].Value;
						if (v != null && v != System.DBNull.Value)
						{
							ret += Convert.ToDouble(v);
						}
					}
				}
			}
			return ret;
		}
		[Description("Set field value for the current record")]
		public void SetFieldValue(string fieldName, object value)
		{
			if (IsQuerying)
				return;
			if (BindingContext != null && _bindingSource != null)
			{
				BindingManagerBase bm = BindingContext[_bindingSource];
				if (bm != null)
				{
					bm.EndCurrentEdit();
				}
			}
			QueryDef.SetFieldValue(fieldName, value);
			if (this.CurrentCell != null)
			{
				DataGridViewRow dvr = this.Rows[this.CurrentCell.RowIndex];
				if (dvr != null)
				{
					int n = -1;
					for (int i = 0; i < this.Columns.Count; i++)
					{
						if (string.Compare(this.Columns[i].DataPropertyName, fieldName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							if (!this.Columns[i].ReadOnly)
							{
								n = i;
							}
							break;
						}
					}
					if (n >= 0)
					{
						DataGridViewCell dvc = dvr.Cells[n];
						if (dvc != null)
						{
							dvc.Value = value.ToString();
						}
					}
				}
			}
		}
		[Description("Set field value for the record specified by rowNumber")]
		public void SetFieldValueEx(int rowNumber, string fieldName, object value)
		{
			if (IsQuerying)
				return;
			if (BindingContext != null && _bindingSource != null)
			{
				BindingManagerBase bm = BindingContext[_bindingSource];
				if (bm != null)
				{
					bm.EndCurrentEdit();
				}
			}
			QueryDef.SetFieldValueEx(rowNumber, fieldName, value);
			DataGridViewRow dvr = this.Rows[rowNumber];
			if (dvr != null)
			{
				int n = -1;
				for (int i = 0; i < this.Columns.Count; i++)
				{
					if (string.Compare(this.Columns[i].DataPropertyName, fieldName, StringComparison.OrdinalIgnoreCase) == 0)
					{
						if (!this.Columns[i].ReadOnly)
						{
							n = i;
						}
						break;
					}
				}
				if (n >= 0)
				{
					DataGridViewCell dvc = dvr.Cells[n];
					if (dvc != null)
					{
						dvc.Value = value.ToString();
					}
				}
			}
		}
		[Description("Get row modification status for the specified row")]
		public DataRowState GetRowState(int rowIndex)
		{
			if (rowIndex >= 0 && rowIndex < this.DataRows.Count)
			{
				DataRow dr = this.DataRows[rowIndex];
				if (dr != null)
				{
					return dr.RowState;
				}
			}
			return DataRowState.Unchanged;
		}
		[Description("Get row modification status for the current row")]
		public DataRowState GetCurrentRowState()
		{
			return GetRowState(this.RowIndex);
		}
		[Description("Save changed data to the database")]
		public bool UpdateData()
		{
			try
			{
				if (IsCurrentCellDirty)
				{
					CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
				if (BeforeUpdate != null)
				{
					BeforeUpdate(this, new EventArgsDataFill(QueryDef.RowCount));
				}
				if (cbx != null)
				{
					for (int i = 0; i < cbx.Length; i++)
					{
						if (cbx[i] != null)
						{
							cbx[i].Visible = false;
						}
					}
				}
				if (bts != null)
				{
					for (int i = 0; i < bts.Length; i++)
					{
						if (bts[i] != null)
						{
							bts[i].Visible = false;
						}
					}
				}
				_bUpdating = true;
				if (QueryDef.Update())
				{
					if (QueryDef.RowCount == 0)
					{
						if (this.AllowUserToAddRows)
						{
							this.AllowUserToAddRows = false;
						}
						if (_allowAddNewrow)
						{
							if (!this.ReadOnly && !this.ForReadOnly)
							{
								btNew.Visible = CanShowNewButton();
								btNew.Enabled = true;
								btNew.BringToFront();
							}
						}
					}
					else
					{
						if (_allowAddNewrow)
						{
							this.AllowUserToAddRows = true;
							btNew.Visible = false;
						}
					}
					_bUpdating = false;
					if (LeaveAddingRecord != null)
					{
						LeaveAddingRecord(this, EventArgs.Empty);
					}
					if (AfterUpdate != null)
					{
						AfterUpdate(this, new EventArgsDataFill(QueryDef.RowCount));
					}
					_newIdentities = null;
					return true;
				}
				else
				{
					if (UpdateFail != null)
					{
						UpdateFail(this, EventArgs.Empty);
					}
					RequeryAfterError();
				}
			}
			catch (System.Data.DBConcurrencyException)
			{
				RequeryAfterError();
			}
			catch (Exception er)
			{
				FormLog.NotifyException(this.ShowErrorMessage, er, "Error updaing database. {0}", QueryDef.GetQueryInfo());
				RequeryAfterError();
			}
			finally
			{
				_bUpdating = false;
				this.AllowUserToAddRows = _allowAddNewrow;
			}
			return false;
		}
		[Description("Set query parameter value. ")]
		public void SetParameterValue(string parameterName, object value)
		{
			QueryDef.SetParameterValue(parameterName, value);
		}
		[Browsable(false)]
		public BindingSource BindSource
		{
			get
			{
				if (_bindingSource == null)
				{
					_bindingSource = new BindingSource();

				}
				return _bindingSource;
			}
		}
		public FieldList GetCachedFields()
		{
			if (_cachedFields == null)
			{
				_cachedFields = Fields;
			}
			return _cachedFields;
		}
		[Description("Query the database to get data")]
		public virtual bool Query()
		{
			bool bOK = false;
			_isQuerying = true;
			_cachedFields = null;
			try
			{
				EasyQuery.LogMessage2("{0} - {1}.Query() Starts...", TableName, this.GetType().FullName);
				if (Columns != null && Columns.Count > 0)
				{
					cellStyles = new DataGridViewCellStyle[Columns.Count];
					names = new string[Columns.Count];
					for (int i = 0; i < Columns.Count; i++)
					{
						cellStyles[i] = (DataGridViewCellStyle)Columns[i].DefaultCellStyle.Clone();
						names[i] = Columns[i].DataPropertyName;
					}
				}
				else
				{
					this.AutoGenerateColumns = true;
				}
				_rowIndex = -1;
				//
				List<DataGridViewColumn> cls = new List<DataGridViewColumn>();
				for (int i = 0; i < Columns.Count; i++)
				{
					cls.Add(Columns[i]);
				}
				if (_bindingSource != null)
				{
					_bindingSource.Sort = string.Empty;
				}
				clearEditorControls();
				QueryDef.ResetCanChangeDataSet(false);
				//
				if (this.BindSourceType == EnumDataSourceType.Array)
				{
					bOK = true;
				}
				else
				{
					_newIdentities = new List<int>();
					_currentIdentity = 0;
					bOK = QueryDef.Query();
					if (bOK)
					{
						FieldList fl = QueryDef.Fields;
						List<DataGridViewColumn> cls0 = new List<DataGridViewColumn>();
						for (int i = 0; i < Columns.Count; i++)
						{
							EPField f = fl[Columns[i].DataPropertyName];
							if (f == null)
							{
								cls0.Add(Columns[i]);
							}
						}
						foreach (DataGridViewColumn c in cls0)
						{
							Columns.Remove(c);
						}
					}
				}
				//
				//
				OnBindDataSource();
				//
				createEditorControls();
				//
				if (cls.Count > 0)
				{
					//restore column attributes
					for (int i = 0; i < Columns.Count; i++)
					{
						for (int j = 0; j < cls.Count; j++)
						{
							if (string.Compare(cls[j].DataPropertyName, Columns[i].DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
							{
								if (Columns[i].Site != null)
								{
									Columns[i].Site.Name = cls[j].Name;
								}
								Columns[i].Name = cls[j].Name;
								Columns[i].Width = cls[j].Width;
								Columns[i].Visible = cls[j].Visible;
								Columns[i].SortMode = cls[j].SortMode;
								Columns[i].AutoSizeMode = cls[j].AutoSizeMode;
								if (cls[j].ReadOnly)
								{
									Columns[i].ReadOnly = cls[j].ReadOnly;
								}
								Columns[i].HeaderText = cls[j].HeaderText;
								Columns[i].Frozen = cls[j].Frozen;
								Columns[i].FillWeight = cls[j].FillWeight;
								Columns[i].DividerWidth = cls[j].DividerWidth;
								//Columns[i].DisplayIndex = cls[j].DisplayIndex;
								Columns[i].DefaultHeaderCellType = cls[j].DefaultHeaderCellType;
								Columns[i].DefaultCellStyle = cls[j].DefaultCellStyle;

								break;
							}
						}
					}
				}
				saveIdentity();
			}
			catch (Exception err)
			{
				MessageBox.Show(this.FindForm(), LimnorDatabase.ExceptionLimnorDatabase.FormExceptionText(err), "Query", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				_isQuerying = false;
				//
				EasyQuery.LogMessage2("{0} - {1}.Query() Ends---", TableName, this.GetType().FullName);
			}
			return bOK;
		}
		[RefreshProperties(RefreshProperties.All)]
		[Description("Execute database query with WHERE clause and parameter values. The parameters can be in the new WHERE clause.")]
		public bool QueryWithWhere(string where, params object[] values)
		{
			EasyQuery _qry = QueryDef;
			if (_qry != null)
			{
				_qry.LogMessage("Calling  QueryWithWhere from EasyDataSet");
				string w = QueryDef.Where;
				try
				{
					QueryDef.Where = where;
					QueryDef.PrepareQuery();
					ParameterList pl = QueryDef.Parameters;
					int pn;
					if (pl != null)
						pn = pl.Count;
					else
						pn = 0;
					int pvn = 0;
					if (values != null)
						pvn = values.Length;
					_qry.LogMessage("Parameter count:{0}", pn);
					_qry.LogMessage("Parameter value count:{0}", pvn);
					if (values != null && values.Length > 0 && pl != null)
					{
						int n = Math.Min(pn, pvn);
						for (int i = 0; i < n; i++)
						{
							if (values[i] is DateTime)
							{
								if (!EPField.IsDatetime(pl[i].OleDbType))
								{
									pl[i].OleDbType = OleDbType.Date;
								}
								if (_qry.IsJet)
								{
									DateTime dt = (DateTime)values[i];
									pl[i].SetValue(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second));
								}
								else
								{
									pl[i].SetValue(values[i]);
								}
							}
							else
							{
								if (values[i] == null || values[i] == DBNull.Value)
								{
									pl[i].OleDbType = OleDbType.VarWChar;
									pl[i].SetValue(null);
								}
								else
								{
									pl[i].OleDbType = EPField.ToOleDBType(values[i].GetType());
									pl[i].SetValue(values[i]);
								}
							}
							_qry.LogMessage("value:{0}", values[i]);
						}
					}
					return Query();
				}
				catch
				{
					throw;
				}
				finally
				{
					_qry.Where = w;
				}
			}
			return false;
		}

		[Description("Execute database query with the parameter values")]
		public void QueryWithParameterValues(params object[] values)
		{
			if (values != null && values.Length > 0)
			{
				QueryDef.PrepareQueryIfChanged();
				ParameterList pl = QueryDef.Parameters;
				if (pl != null)
				{
					int n = Math.Min(pl.Count, values.Length);
					for (int i = 0; i < n; i++)
					{
						if (values[i] is DateTime)
						{
							if (!EPField.IsDatetime(pl[i].OleDbType))
							{
								pl[i].OleDbType = OleDbType.DBTimeStamp;
							}
							if (QueryDef.IsJet)
							{
								DateTime dt = (DateTime)values[i];
								pl[i].SetValue(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second));
							}
							else
							{
								pl[i].SetValue(values[i]);
							}
						}
						else
						{
							if (values[i] == null || values[i] == DBNull.Value)
							{
								pl[i].OleDbType = OleDbType.VarWChar;
								pl[i].SetValue(null);
							}
							else
							{
								pl[i].OleDbType = EPField.ToOleDBType(values[i].GetType());
								pl[i].SetValue(values[i]);
							}
						}
					}
				}
			}
			Query();
		}
		[Description("Setting SQL property usually will remove all column settings. Calling this method with True for keepColumnSettings prevents column settings being removed when SQL property is changed.")]
		public void SetKeepColumnSettings(bool keepColumnSettings)
		{
			_keepColumnSettings = keepColumnSettings;
		}
		[Description("Set credential for accessing the database.")]
		public void SetCredential(string user, string userPassword, string databasePassword)
		{
			QueryDef.SetCredential(user, userPassword, databasePassword);
			Query();
		}
		[Description("Launch a dialogue box to set credential for accessing the database.")]
		public bool SetCredentialByUI(Form caller)
		{
			if (QueryDef.DatabaseConnection.SetCredentialByUI(caller))
			{
				Query();
				return true;
			}
			return false;
		}
		[Description("Gets a Boolean indicating whether a Search action is executing.")]
		[ReadOnly(true)]
		[XmlIgnore]
		public bool IsSearching
		{
			get
			{
				return QueryDef.IsSearching;
			}
			set
			{
				QueryDef.IsSearching = value;
			}
		}
		[Description("Set the next record of the first table in DataStorage as the current record")]
		public bool MoveNext()
		{
			return QueryDef.MoveNext();
		}

		[Description("Set the previous record of the first table in DataStorage as the current record")]
		public bool MovePrevious()
		{
			return QueryDef.MovePrevious();
		}

		[Description("Set the last record of the first table in DataStorage as the current record")]
		public bool MoveLast()
		{
			return QueryDef.MoveLast();
		}

		[Description("Set the first record of the first table in DataStorage as the current record")]
		public bool MoveFirst()
		{
			return QueryDef.MoveFirst();
		}
		[Description("Go through each row and test the condition. The first row making the condition match becomes the current row.")]
		public bool Search(bool condition)
		{
			return false;
		}
		[Description("Delete the current record")]
		public void DeleteCurrentRecord()
		{
			DataGridViewRow r = this.CurrentRow;
			if (r != null)
			{
				DataGridViewRowCancelEventArgs e = new DataGridViewRowCancelEventArgs(r);
				OnUserDeletingRow(e);
				if (e.Cancel)
				{
				}
				else
				{
					QueryDef.DeleteCurrentRecord();
					DataGridViewRowEventArgs e2 = new DataGridViewRowEventArgs(r);
					OnUserDeletedRow(e2);
				}
			}
		}
		protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
		{
			base.OnDataBindingComplete(e);
			QueryDef.SetBindingContext(this.BindingContext);
			_bindingIdentity = _currentIdentity;
			_leavingIdentity = 0;
			restoreCurrentRowSelection();
		}
		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			base.OnDragDrop(drgevent);
			Point cursorLocation = this.PointToClient(new Point(drgevent.X, drgevent.Y));
			DataGridView.HitTestInfo hittest = this.HitTest(cursorLocation.X, cursorLocation.Y);
			if (hittest.Type == DataGridViewHitTestType.Cell)
			{
				OnDragDropCell(hittest, drgevent);
			}
		}
		protected virtual void OnDragDropCell(DataGridView.HitTestInfo hitTest, DragEventArgs dragEvent)
		{
			if (DropOnCell != null)
			{
				DropOnCell(this, new EventArgsDropOnCell(hitTest, dragEvent));
			}
		}
		[Browsable(false)]
		public void CopyFrom(EasyQuery query)
		{
			QueryDef.CopyFrom(query);
		}
		#endregion
		#region private methods
		private void RequeryAfterError()
		{
			if (this.CurrentCell != null)
			{
				int n = this.CurrentCell.RowIndex;
				Query();
				if (QueryDef.DataStorage != null && QueryDef.DataStorage.Tables.Count > 0)
				{
					if (n >= 0 && n < QueryDef.DataStorage.Tables[0].Rows.Count)
					{
						this.SetSelectedRowCore(n, true);
					}
				}
			}
		}
		private void onSaveButtonClick(object sender, System.EventArgs e)
		{
			UpdateData();
		}
		private void onNewButtonClick(object sender, System.EventArgs e)
		{
			try
			{
				BindSource.AddNew();
				try
				{
					BindSource.EndEdit();
				}
				catch (NoNullAllowedException)
				{
				}
				if (EnterAddingRecord != null)
				{
					EnterAddingRecord(this, EventArgs.Empty);
				}
				if (_allowAddNewrow)
				{
					this.AllowUserToAddRows = true;
				}
			}
			catch (Exception err)
			{
				MessageBox.Show(this.FindForm(), ExceptionLimnorDatabase.FormExceptionText(err), "EasyGrid", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		protected void OnEmptyRecord(object sender, System.EventArgs e)
		{
			if (Site == null || !Site.DesignMode)
			{
				if (this.AllowUserToAddRows || _allowAddNewrow)
				{
					_allowAddNewrow = true;
					this.AllowUserToAddRows = false;
					if (!ReadOnly && !this.ForReadOnly)
					{
						btNew.Visible = CanShowNewButton();
						btNew.BringToFront();
					}
				}
				if (IsEmptyChanged != null)
				{
					IsEmptyChanged(this, new EventArgsDataFill(QueryDef.CommitedRowCount));
				}
			}
		}
		private void onBeforeDeleteRecord(object sender, System.EventArgs e)
		{
			OnEmptyRecord(sender, e);
		}
		private void onAfterDeleteRecord(object sender, System.EventArgs e)
		{
			if (!ReadOnly)
			{
				if (QueryDef.CommitedRowCount > 0)
				{
					if (!this.AllowUserToAddRows)
					{
						if (_allowAddNewrow)
						{
							this.AllowUserToAddRows = true;
						}
					}
				}
			}
			if (IsEmptyChanged != null)
			{
				IsEmptyChanged(this, new EventArgsDataFill(QueryDef.CommitedRowCount));
			}
		}
		private bool columnEditable(int i)
		{
			FieldList fields = this.GetCachedFields();
			if (fields != null)
			{
				if (i >= 0 && i < fields.Count)
				{
					EPField fld = fields[i];
					if (fld != null)
					{
						return !fld.ReadOnly;
					}
				}
			}
			return false;
		}
		[Browsable(false)]
		protected DataRow currentDataRow
		{
			get
			{
				try
				{
					if (_bindingSource != null)
					{
						DataRowView drv = _bindingSource.Current as DataRowView;
						if (drv != null)
						{
							return drv.Row;
						}
					}
					if (_query != null)
					{
						if (_query.DataStorage != null && _query.DataStorage.Tables.Count > 0)
						{
							Form frm = this.FindForm();
							if (frm != null)
							{
								BindingManagerBase bmb = frm.BindingContext[this.DataSource, this.DataMember];
								if (bmb != null)
								{
									DataRowView drv = bmb.Current as DataRowView;
									if (drv != null)
									{
										return drv.Row;
									}
								}
							}
						}
					}
				}
				catch
				{
				}
				return null;
			}
		}
		private bool currentCellInSynch()
		{
			return (CurrentCell != null && CurrentCell.ColumnIndex >= 0 && CurrentCell.RowIndex >= 0);
		}
		private void onPickValueByButton(object Sender, object Value)
		{
			if (currentCellInSynch())
			{
				try
				{
					int nColumnIndex = getCurrentFieldIndex();
					if (nColumnIndex >= 0)
					{
						System.Data.DataRow dw = currentDataRow;
						if (dw != null)
						{
							dw.BeginEdit();
							dw[nColumnIndex] = Value;
							dw.EndEdit();
						}
					}
				}
				catch (Exception er)
				{
					FormLog.NotifyException(ShowErrorMessage, er, "Error picking value by button");
				}
			}
		}
		protected void ShowNewButton(bool visible)
		{
			if (CanShowNewButton())
			{
				btNew.Visible = visible;
			}
			else
			{
				btNew.Visible = false;
			}
		}
		protected virtual void onLookup()
		{
			if (Lookup != null)
			{
				Lookup(this, EventArgs.Empty);
			}
		}
		private string getCurrentFieldName()
		{
			if (CurrentCell.OwningColumn != null)
			{
				string fldName = CurrentCell.OwningColumn.DataPropertyName;
				if (string.IsNullOrEmpty(fldName))
				{
					fldName = CurrentCell.OwningColumn.Name;
				}
				return fldName;
			}
			return string.Empty;
		}
		private int getCurrentFieldIndex()
		{
			int nCurrentCellColumn = -1;
			FieldList fields = GetCachedFields();
			if (fields != null && fields.Count > 0)
			{
				if (CurrentCell.OwningColumn != null)
				{
					string fldName = getCurrentFieldName();
					if (!string.IsNullOrEmpty(fldName))
					{
						for (int i = 0; i < fields.Count; i++)
						{
							if (string.Compare(fields[i].Name, fldName, StringComparison.OrdinalIgnoreCase) == 0)
							{
								nCurrentCellColumn = i;
								break;
							}
						}
					}
				}
			}
			return nCurrentCellColumn;
		}
		private void onLookupSelected(object sender, System.EventArgs e)
		{
			ComboLook cb = (ComboLook)sender;
			if (cb.bNoEvent)
				return;
			int n = cb.SelectedIndex;
			if (n >= 0)
			{
				if (currentCellInSynch())
				{
					int nColumnIndex = getCurrentFieldIndex();
					FieldList fields = GetCachedFields();
					object Value = cb.GetLookupData();
					bool bMatch = false;
					if (cbx != null && nColumnIndex >= 0 && nColumnIndex < cbx.Length)
					{
						bMatch = (cbx[nColumnIndex] == sender);
					}
					try
					{
						System.Data.DataRow dw = currentDataRow;
						if (bMatch && dw != null && fields[nColumnIndex].editor != null)
						{
							DataBind databind = null;
							DataEditorLookupDB lk = fields[nColumnIndex].editor as DataEditorLookupDB;
							if (lk != null)
							{
								databind = lk.valuesMaps;
							}
							DataRow rv = Value as DataRow;
							if (databind != null && rv != null) //database lookup
							{
								if (databind.AdditionalJoins != null && databind.AdditionalJoins.StringMaps != null)
								{
									for (int i = 0; i < databind.AdditionalJoins.StringMaps.Length; i++)
									{
										if (!string.IsNullOrEmpty(databind.AdditionalJoins.StringMaps[i].Source))
										{
											EPField f = fields[databind.AdditionalJoins.StringMaps[i].Target];
											f.Value = rv[databind.AdditionalJoins.StringMaps[i].Source];
											if (!f.ReadOnly)
											{
												dw[databind.AdditionalJoins.StringMaps[i].Target] = f.Value;
											}
											else
											{
												int c = getColumnIndexByTitle(databind.AdditionalJoins.StringMaps[i].Target);
												if (c >= 0)
												{
													this.QueryDef.DataTable.Columns[c].ReadOnly = false;
													this.Rows[CurrentCell.RowIndex].Cells[c].ReadOnly = false;
													this.Rows[CurrentCell.RowIndex].Cells[c].Value = f.Value;
													this.Rows[CurrentCell.RowIndex].Cells[c].ReadOnly = true;
													this.QueryDef.DataTable.Columns[c].ReadOnly = true;
												}
											}
										}
									}
								}
								onLookup();
							}
							else
							{
								if (rv != null)
								{
									Value = rv[1]; //database lookup uses the second column as the value
								}
								bool bEQ = false;
								int nPos = cb.GetUpdateFieldIndex();
								if (nPos < 0 || nPos >= Columns.Count)
								{
									nPos = nColumnIndex;
								}
								if (Value == null || Value == System.DBNull.Value)
								{
									if (dw[nPos] == null || dw[nPos] == System.DBNull.Value)
									{
										bEQ = true;
									}
								}
								else if (Value.Equals(dw[nPos]))
									bEQ = true;
								if (!bEQ)
								{
									if (!fields[nColumnIndex].ReadOnly)
									{
										dw.BeginEdit();
										bool bt;
										dw[nPos] = VPLUtil.ConvertObject(Value, EPField.ToSystemType(fields[nColumnIndex].OleDbType), out bt);
										if (!bt)
										{
											DatabaseConnection.Log("Lookup: cannot convert value of type {0} to field {1} of type {2}. Value:{3}", Value.GetType(), fields[nColumnIndex].Name, fields[nColumnIndex].OleDbType, Value);
										}
										dw.EndEdit();
									}
									dw[nColumnIndex] = ValueConvertor.ToObject(Value, QueryDef.DataStorage.Tables[0].Columns[nColumnIndex].DataType);
								}
								if (!bEQ)
								{
									onLookup();
								}
							}
						}
					}
					catch (Exception er)
					{
						FormLog.NotifyException(ShowErrorMessage, er, "Error looking up for value");
					}
				}
			}
		}
		private void clearEditorControls()
		{
			btSave.Visible = false;
			int i;
			if (cbx != null)
			{
				for (i = 0; i < cbx.Length; i++)
				{
					try
					{
						this.Controls.Remove(cbx[i]);
					}
					catch
					{
					}
					cbx[i] = null;
				}
				cbx = null;
			}
			if (bts != null)
			{
				for (i = 0; i < bts.Length; i++)
				{
					try
					{
						this.Controls.Remove(bts[i]);
					}
					catch
					{
					}
					bts[i] = null;
				}
				bts = null;
			}
		}
		private void createEditorControls()
		{
			clearEditorControls();
			if (!ReadOnly)
			{
				if (!QueryDef.ReadOnly)
				{
					btSave.Visible = !HideSaveButton;
					btSave.Location = new Point(this.Width - btSave.Width, 0);
					btSave.BringToFront();
				}
			}
			int i;

			FieldList fields = Fields;
			int n = fields.Count;
			cbx = new ComboBox[n];
			bts = new Button[n];
			if (ReadOnly || n == 0)
			{
				return;
			}

			for (i = 0; i < fields.Count; i++)
			{
				if (fields[i].IsIdentity)
				{
					for (int j = 0; j < this.Columns.Count; j++)
					{
						if (string.Compare(fields[i].Name, this.Columns[j].DataPropertyName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							this.Columns[j].ReadOnly = true;
							break;
						}
					}
				}
				if (fields[i].editor != null)
				{
					DataEditorButton bn = fields[i].editor as DataEditorButton;
					if (bn != null)
					{
						bts[i] = bn.MakeButton(this.FindForm());
						if (bts[i] != null)
						{
							bts[i].Parent = this;
							bts[i].Visible = false;
							fields[i].editor.OnPickValue += new fnOnPickValue(onPickValueByButton);
						}
					}
					else
					{
						DataEditorLookupYesNo LK = fields[i].editor as DataEditorLookupYesNo;
						if (LK != null)
						{
							cbx[i] = LK.MakeComboBox();
							if (cbx[i] != null)
							{
								cbx[i].Parent = this;
								cbx[i].Visible = false;
								cbx[i].SelectedIndexChanged += new System.EventHandler(onLookupSelected);
							}
						}
					}
				}
			}
		}
		#endregion
		#region Hidden members
		[ReadOnly(true)]
		[Browsable(false)]
		protected bool AllowAddNewrow { get { return _allowAddNewrow; } set { _allowAddNewrow = value; } }
		[Browsable(false)]
		public bool QueryReady
		{
			get
			{
				if (_query != null)
				{
					return _query.QueryReady;
				}
				return false;
			}
		}
		[Browsable(false)]
		public virtual bool IsQuerying
		{
			get
			{
				if (_query != null)
				{
					return _query.IsQuerying;
				}
				return _isQuerying;
			}
		}
		[Browsable(false)]
		protected virtual void OnRemoveTable()
		{

		}
		[XmlIgnore]
		[Browsable(false)]
		[ReadOnly(true)]
		public new object DataSource
		{
			get
			{
				return base.DataSource;
			}
			set
			{
				base.DataSource = value;
			}
		}
		[XmlIgnore]
		[Browsable(false)]
		[ReadOnly(true)]
		public new string DataMember
		{
			get
			{
				return base.DataMember;
			}
			set
			{
				base.DataMember = value;
			}
		}
		[Browsable(false)]
		public DataColumn GetColumnByName(string name)
		{
			if (QueryDef.DataStorage != null && QueryDef.DataStorage.Tables.Count > 0)
			{
				DataTable tbl = QueryDef.DataStorage.Tables[TableName];
				if (tbl != null)
				{
					return tbl.Columns[name];
				}
			}
			return null;
		}
		[Browsable(false)]
		public void SetHandlerForSQLChange(EventHandler beforeSqlChangeHandle, EventHandler afterSqlChangeHandle)
		{
			_handleBeforeSQLChange = beforeSqlChangeHandle;
			_handleAfterSQLChange = afterSqlChangeHandle;
		}
		protected void OnFillData()
		{
			if (DataFilled != null)
			{
				int nRowCount = this.Rows.Count;
				EventArgsDataFill e0 = new EventArgsDataFill(nRowCount);
				DataFilled(this, e0);
			}
		}
		[Browsable(false)]
		public virtual bool OnBeforeSetSQL()
		{
			return true;
		}
		[Browsable(false)]
		public virtual bool CanShowNewButton()
		{
			return true;
		}
		protected virtual void OnBindDataSource()
		{
			try
			{
				EasyQuery.LogMessage2("{0} - EasyGrid.OnBindDataSource starts...", TableName);
				if (QueryDef.DataStorage != null && QueryDef.DataStorage.Tables.Count > 0)
				{
					DataTable tbl = QueryDef.DataStorage.Tables[TableName];
					if (tbl != null)
					{
						if (Site != null && Site.DesignMode)
						{
							this.AutoGenerateColumns = true;
						}
						else
						{
							if (this.Columns == null || this.Columns.Count == 0)
							{
								this.AutoGenerateColumns = true;
							}
						}
						BindSource.DataSource = QueryDef.DataStorage;
						BindSource.DataMember = TableName;
						_bindingSource.AllowNew = !ReadOnly;
						_bindingSource.Sort = string.Empty;
						base.DataSource = _bindingSource;
						if (BindingContext != null)
						{
							BindingManagerBase bm = BindingContext[_bindingSource];
							if (bm != null)
							{
								QueryDef.SetBindingContext(bm);
							}
						}
						OnFillData();
					}
				}
			}
			catch (Exception e)
			{
				FormLog.NotifyException(ShowErrorMessage, e, "{0} - EasyGrid.OnBindDataSource", TableName);
			}
			EasyQuery.LogMessage2("{0} - EasyGrid.OnBindDataSource ends---", TableName);
		}
		protected override void OnBindingContextChanged(EventArgs e)
		{
			EasyQuery.LogMessage2("{0} - {1}.OnBindingContextChanged", TableName, this.GetType().FullName);
			base.OnBindingContextChanged(e);
			if (BindingContext != null)
			{
				BindingManagerBase bm = BindingContext[BindSource];
				if (bm != null)
				{
					QueryDef.SetBindingContext(bm);
				}
			}
		}
		#endregion
		#region IReport32Usage Members
		[Browsable(false)]
		public string Report32Usage()
		{
			if (_query != null)
			{
				return _query.Report32Usage();
			}
			return string.Empty;
		}

		#endregion
		#region IMasterSource Members

		[Browsable(false)]
		public object MasterSource
		{
			get { return BindSource; }
		}
		[Browsable(false)]
		public DataColumn[] GetColumnsByNames(string[] names)
		{
			if (names != null)
			{
				DataTable tbl = QueryDef.DataStorage.Tables[TableName];
				if (tbl != null)
				{
					DataColumn[] cols = new DataColumn[names.Length];
					for (int i = 0; i < names.Length; i++)
					{
						cols[i] = tbl.Columns[names[i]];
					}
					return cols;
				}
			}
			return new DataColumn[] { };
		}
		[Browsable(false)]
		public string[] GetFieldNames()
		{
			FieldList flds = GetCachedFields();
			string[] fs = new string[flds.Count];
			for (int i = 0; i < fs.Length; i++)
			{
				fs[i] = flds[i].Name;
			}
			return fs;
		}
		[Browsable(false)]
		public event EventHandler EnterAddingRecord;
		[Browsable(false)]
		public event EventHandler LeaveAddingRecord;

		[Description("This event occurs at the time when the field calculations are to be executed.")]
		public event EventHandler CalculateCellValues;

		[Description("This event occurs before executing Update action.")]
		public event EventHandler BeforeUpdate;
		[Description("This event occurs after executing Update action.")]
		public event EventHandler AfterUpdate;
		[Description("This event occurs on error executing Update action. LastError property is an error message for the failure.")]
		public event EventHandler UpdateFail;
		[Browsable(false)]
		[Description("This event occurs when the row count changed from 0 to non-0 or from non-0 to 0.")]
		public event EventHandler IsEmptyChanged;
		#endregion
		#region ISourceValueEnumProvider Members
		[Browsable(false)]
		public object[] GetValueEnum(string section, string item)
		{
			if (string.CompareOrdinal(section, "SetParameterValue") == 0 && string.CompareOrdinal(item, "parameterName") == 0)
			{
				ParameterList pl = QueryDef.Parameters;
				if (pl != null)
				{
					object[] vs = new object[pl.Count];
					for (int i = 0; i < pl.Count; i++)
					{
						vs[i] = pl[i].Name;
					}
					return vs;
				}
			}
			else if (string.CompareOrdinal(item, "fieldName") == 0)
			{
				if (string.CompareOrdinal(section, "SetFieldValue") == 0 || string.CompareOrdinal(section, "GetColumnSum") == 0 || string.CompareOrdinal(section, "SetFieldValueEx") == 0 || string.CompareOrdinal(section, "SetFieldValueByRow") == 0)
				{
					FieldList fs = GetCachedFields();
					if (fs != null && fs.Count > 0)
					{
						object[] vs = new object[fs.Count];
						for (int i = 0; i < fs.Count; i++)
						{
							vs[i] = fs[i].Name;
						}
						return vs;
					}
				}
			}
			return null;
		}

		#endregion
		#region IDynamicMethodParameters Members
		private string getWhereDesc()
		{
			return string.Format(CultureInfo.InvariantCulture, "WHERE clause of the database query. Original query:{0}", this.SqlQuery);
		}
		[Browsable(false)]
		public System.Reflection.ParameterInfo[] GetDynamicMethodParameters(string methodName, object av)
		{
			lock (_lock)
			{
				int nStep = 0;
				try
				{
					if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
					{
						ParameterList pl = QueryDef.Parameters;
						nStep = 1;
						if (pl != null && pl.Count > 0)
						{
							ParameterInfo[] ps = new ParameterInfo[pl.Count];
							for (int i = 0; i < pl.Count; i++)
							{
								EPField f = pl[i];
								ps[i] = new SimpleParameterInfo(f.Name, methodName, EPField.ToSystemType(f.OleDbType), string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", f.Name));
							}
							nStep = 2;
							return ps;
						}
						return new ParameterInfo[] { };
					}
					else if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0)
					{

						string where = null;
						nStep = 6;
						IActionContextExt ace = av as IActionContextExt;
						if (ace != null)
						{
							nStep = 7;
							object pv = ace.GetParameterValue("where");
							nStep = 8;
							IParameterValue ipv = pv as IParameterValue;
							if (ipv != null)
							{
								nStep = 9;
								where = ipv.GetConstValue() as string;
							}
							else
							{
								nStep = 10;
								where = pv as string;
							}
						}
						else
						{
							nStep = 11;
						}
						bool changed = false;
						DbParameterList curPs = this.Parameters;
						DbCommandParam[] oldPs = new DbCommandParam[curPs.Count];
						for (int i = 0; i < oldPs.Length; i++)
						{
							oldPs[i] = curPs[i];
						}
						string w = QueryDef.Where;
						if (string.Compare(w, where, StringComparison.OrdinalIgnoreCase) != 0)
						{
							nStep = 12;
							changed = true;
							QueryDef.Where = where;
							nStep = 13;
							QueryDef.PrepareQuery();
							nStep = 14;
						}
						try
						{
							int np = 0;
							ParameterList pl = QueryDef.Parameters;
							nStep = 15;
							if (pl != null && pl.Count > 0)
							{
								nStep = 16;
								np = pl.Count;
								nStep = 17;
							}
							else
							{
								nStep = 18;
							}
							ParameterInfo[] ps = new ParameterInfo[np + 1];
							nStep = 19;
							ps[0] = new SimpleParameterInfo("where", methodName, typeof(string), getWhereDesc());
							nStep = 20;
							if (pl != null && pl.Count > 0)
							{
								nStep = 21;
								for (int i = 0; i < pl.Count; i++)
								{
									nStep = 22;
									EPField f = pl[i];
									nStep = 23;
									ps[i + 1] = new SimpleParameterInfo(f.Name, methodName, EPField.ToSystemType(f.OleDbType), string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", f.Name));
									nStep = 24;
								}
								nStep = 25;
							}
							else
							{
								nStep = 26;
							}
							nStep = 27;
							return ps;
						}
						catch
						{
							throw;
						}
						finally
						{
							if (changed)
							{
								nStep = 28;
								QueryDef.Where = w;
								nStep = 29;
								QueryDef.PrepareQuery();
								if (oldPs.Length > 0)
								{
									ParameterList qps = QueryDef.Parameters;
									for (int i = 0; i < qps.Count; i++)
									{
										for (int j = 0; j < oldPs.Length; j++)
										{
											if (string.CompareOrdinal(qps[i].Name, oldPs[j].Name) == 0)
											{
												qps[i].OleDbType = oldPs[j].Field.OleDbType;
												qps[i].DataSize = oldPs[j].Field.DataSize;
												break;
											}
										}
									}
								}
								nStep = 30;
							}
						}
					}
				}
				catch (Exception errUnknown)
				{
					throw new ExceptionLimnorDatabase(errUnknown, "method name:{0}, step:{1}, component name:{2}", methodName, nStep, this.Name);
				}
			}
			return null;
		}
		[Browsable(false)]
		public object InvokeWithDynamicMethodParameters(string methodName, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
			{
				QueryWithParameterValues(parameters);
			}
			else if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0 || string.CompareOrdinal(methodName, "QueryWithWhereDynamic") == 0)
			{
				if (parameters != null && parameters.Length > 0)
				{
					string w = parameters[0] as string;
					if (!string.IsNullOrEmpty(w))
					{
						object[] vs = new object[parameters.Length - 1];
						for (int i = 1; i < parameters.Length; i++)
						{
							vs[i - 1] = parameters[i];
						}
						QueryWithWhere(w, vs);
					}
				}
			}
			return null;
		}
		[Browsable(false)]
		public bool IsUsingDynamicMethodParameters(string methodName)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0) return true;
			if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0)
				return true;
			return false;
		}
		[NotForProgramming]
		[Browsable(false)]
		public Dictionary<string, string> GetParameterDescriptions(string methodName)
		{
			if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0 || string.CompareOrdinal(methodName, "QueryWithWhereDynamic") == 0)
			{
				Dictionary<string, string> descs = new Dictionary<string, string>();
				descs.Add("where", getWhereDesc());
				return descs;
			}
			return null;
		}
		#endregion
		#region IDatabaseAccess Members
		[NotForProgramming]
		[Browsable(false)]
		public bool NeedDesignTimeSQL
		{
			get { return false; }
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CreateDataTable()
		{
			FieldList fs = Fields;
			if (fs != null && fs.Count > 0)
			{
				DataTable tbl = this.DataTable;
				if (tbl == null)
				{
					tbl = new DataTable(this.TableName);
					QueryDef.DataStorage.Tables.Add(tbl);
					for (int i = 0; i < fs.Count; i++)
					{
						tbl.Columns.Add(fs[i].CreateDataColumn());
					}
					this.DataSource = tbl;
				}
			}
		}
		[Browsable(false)]
		public void SetSqlContext(string name)
		{

		}
		[Browsable(false)]
		public IList<Guid> DatabaseConnectionsUsed
		{
			get
			{
				List<Guid> lst = new List<Guid>(QueryDef.DatabaseConnectionsUsed);
				if (_editorList != null)
				{
					foreach (DataEditor de in _editorList)
					{
						DataEditorLookupDB dedb = de as DataEditorLookupDB;
						if (dedb != null)
						{
							IList<Guid> l = dedb.DatabaseConnectionsUsed;
							foreach (Guid g in l)
							{
								if (!lst.Contains(g))
								{
									lst.Add(g);
								}
							}
						}
					}
				}
				return lst;
			}
		}
		[Browsable(false)]
		public IList<Type> DatabaseConnectionTypesUsed
		{
			get
			{
				List<Type> lst = new List<Type>(QueryDef.DatabaseConnectionTypesUsed);
				if (_editorList != null)
				{
					foreach (DataEditor de in _editorList)
					{
						DataEditorLookupDB dedb = de as DataEditorLookupDB;
						if (dedb != null)
						{
							IList<Type> l = dedb.DatabaseConnectionTypesUsed;
							foreach (Type g in l)
							{
								if (!lst.Contains(g))
								{
									lst.Add(g);
								}
							}
						}
					}
				}
				return lst;
			}
		}
		[Category("Database")]
		[ReadOnly(true)]
		[Editor(typeof(TypeEditorSelectConnection), typeof(UITypeEditor))]
		[Description("Connection to the database")]
		public virtual ConnectionItem DatabaseConnection
		{
			get
			{
				return QueryDef.DatabaseConnection;
			}
			set
			{
				QueryDef.DatabaseConnection = value;
			}
		}
		#endregion
		#region ICustomMethodCompiler Members
		[Browsable(false)]
		public CodeExpression CompileMethod(string methodName, string varName, IMethodCompile methodToCompile, CodeStatementCollection statements, CodeExpressionCollection parameters)
		{
			return QueryDef.CompileMethod(methodName, varName, methodToCompile, statements, parameters);
		}

		#endregion
		#region ICollection Members

		public void CopyTo(Array array, int index)
		{
			this.BindSource.CopyTo(array, index);
		}

		public int Count
		{
			get { return this.BindSource.Count; }
		}

		public bool IsSynchronized
		{
			get { return this.BindSource.IsSynchronized; }
		}

		public object SyncRoot
		{
			get { return this.BindSource.SyncRoot; }
		}

		#endregion
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return this.BindSource.GetEnumerator();
		}

		#endregion
		#region IListSource Members

		public bool ContainsListCollection
		{
			get
			{
				return true;
			}
		}

		public IList GetList()
		{
			return BindSource;
		}

		#endregion
		#region ITypedList Members

		public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
		{
			return this.BindSource.GetItemProperties(listAccessors);
		}

		public string GetListName(PropertyDescriptor[] listAccessors)
		{
			return this.BindSource.GetListName(listAccessors);
		}

		#endregion
		#region IBindingList Members

		public void AddIndex(PropertyDescriptor property)
		{
			((IBindingList)BindSource).AddIndex(property);
		}

		public object AddNew()
		{
			try
			{
				object v = ((IBindingList)BindSource).AddNew();
				DataGridViewRowsAddedEventArgs e = new DataGridViewRowsAddedEventArgs(this.Rows.Count - 1, 1);
				this.OnRowsAdded(e);
				DataGridViewRowEventArgs e2 = new DataGridViewRowEventArgs(this.Rows[this.Rows.Count - 1]);
				this.OnUserAddedRow(e2);
				return v;
			}
			catch (Exception er)
			{
				FormLog.NotifyException(ShowErrorMessage, er, "Error adding record");
				return null;
			}
		}

		public bool AllowEdit
		{
			get { return !this.ReadOnly; }
		}

		public bool AllowNew
		{
			get { return this.AllowUserToAddRows && _allowAddNewrow; }
		}

		public bool AllowRemove
		{
			get { return !this.ReadOnly; }
		}
		private ListSortDirection _sortDir;
		private PropertyDescriptor _sortProperty;
		public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
		{
			_sortDir = direction;
			_sortProperty = property;
		}

		public int Find(PropertyDescriptor property, object key)
		{
			return 0;
		}

		public bool IsSorted
		{
			get { return false; }
		}
		[Browsable(false)]
		public event ListChangedEventHandler ListChanged;

		public void RemoveIndex(PropertyDescriptor property)
		{

		}

		public void RemoveSort()
		{
			BindSource.RemoveSort();
			if (ListChanged != null)
			{
			}
		}

		public ListSortDirection SortDirection
		{
			get { return _sortDir; }
		}

		public PropertyDescriptor SortProperty
		{
			get { return _sortProperty; }
		}

		public bool SupportsChangeNotification
		{
			get { return BindSource.SupportsChangeNotification; }
		}

		public bool SupportsSearching
		{
			get { return BindSource.SupportsSearching; }
		}

		public bool SupportsSorting
		{
			get { return BindSource.SupportsSorting; }
		}

		#endregion
		#region IList Members

		public int Add(object value)
		{
			return this.BindSource.Add(value);
		}

		public void Clear()
		{
			this.BindSource.Clear();
		}

		public bool Contains(object value)
		{
			return this.BindSource.Contains(value);
		}

		public int IndexOf(object value)
		{
			return this.BindSource.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			this.BindSource.Insert(index, value);
		}

		public bool IsFixedSize
		{
			get { return this.BindSource.IsFixedSize; }
		}

		public bool IsReadOnly
		{
			get { return this.ReadOnly; }
		}

		public void Remove(object value)
		{
			this.BindSource.Remove(value);
		}

		public void RemoveAt(int index)
		{
			this.BindSource.RemoveAt(index);
		}

		public object this[int index]
		{
			get
			{
				return this.BindSource[index];
			}
			set
			{
				this.BindSource[index] = value;
			}
		}

		#endregion
		#region IPostDeserializeProcess Members
		private void createtable()
		{
			DataTable tbl = QueryDef.DataStorage.Tables[TableName];
			if (tbl == null)
			{
				QueryDef.CreateMainTable();
				tbl = QueryDef.DataStorage.Tables[TableName];
				if (tbl.Columns.Count == 0)
				{
					for (int i = 0; i < this.Columns.Count; i++)
					{
						if (string.IsNullOrEmpty(this.Columns[i].Name))
						{
							this.Columns[i].Name = string.Format(CultureInfo.InvariantCulture, "col{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						}
						if (this.Columns[i].ValueType == null)
						{
							this.Columns[i].ValueType = typeof(string);
						}
						if (this.Columns[i] is DataGridViewCheckBoxColumn)
						{
							this.Columns[i].ValueType = typeof(bool);
						}
						string name = this.Columns[i].DataPropertyName;
						if (string.IsNullOrEmpty(name))
						{
							name = this.Columns[i].Name;
							if (string.IsNullOrEmpty(name))
							{
								name = this.Columns[i].HeaderText;
							}
						}
						tbl.Columns.Add(name, this.Columns[i].ValueType);
					}
				}
			}
		}
		//create dataset so that databinding can be done
		public void OnDeserialize(object context)
		{
			if (Site == null || !Site.DesignMode)
			{
				return;
			}
			createtable();

			BindSource.DataSource = QueryDef.DataStorage;
			BindSource.DataMember = TableName;
			base.DataSource = _bindingSource;
			if (BindingContext != null)
			{
				BindingManagerBase bm = BindingContext[_bindingSource];
				if (bm != null)
				{
					QueryDef.SetBindingContext(bm);
				}
			}
			for (int i = 0; i < this.Columns.Count; i++)
			{
				if (string.IsNullOrEmpty(this.Columns[i].Name))
				{
					this.Columns[i].Name = string.Format(CultureInfo.InvariantCulture, "col{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				}
				if (this.Columns[i].Site != null && this.Columns[i].Site.DesignMode)
				{
					if (string.IsNullOrEmpty(this.Columns[i].Site.Name))
					{
						try
						{
							this.Columns[i].Site.Name = this.Columns[i].Name;
						}
						catch
						{
							this.Columns[i].Site.Name = string.Format(CultureInfo.InvariantCulture, "col{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						}
					}
				}
			}
		}

		#endregion
		#region ICustomDataSource Members

		public string DataBindingPropertyName
		{
			get { return "BindSource"; }
		}

		#endregion
		#region IResetOnComponentChange Members

		public bool OnResetDesigner(string memberName)
		{
			if (string.IsNullOrEmpty(memberName) || string.CompareOrdinal(memberName, "SqlQuery") == 0 || string.CompareOrdinal(memberName, "SQL") == 0)
			{
				return true;
			}
			return false;
		}

		#endregion
		#region IDevClassReferencer Members

		public void SetDevClass(IDevClass c)
		{
			_holder = c;
		}
		public IDevClass GetDevClass()
		{
			return _holder;
		}
		#endregion
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
			PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(this, attributes, true);
			if (typeof(EasyGrid).Equals(this.GetType()) || typeof(EasyGridDetail).Equals(this.GetType()))
			{
				return ps;
			}
			bool forBrowsing = VPLUtil.GetBrowseableProperties(attributes);
			List<PropertyDescriptor> lst = new List<PropertyDescriptor>();
			foreach (PropertyDescriptor p in ps)
			{
				if (string.CompareOrdinal(p.Name, "SQL") == 0)
				{
					lst.Add(new PropertyDescriptorForDisplay(this.GetType(), p.Name, this.SQL.ToString(), new Attribute[] { new CategoryAttribute("Query") }));
				}
				else if (_subClassExcludeProperties.Contains(p.Name))
				{
				}
				else if (_subClassReadOnlyProperties.Contains(p.Name))
				{
					if (forBrowsing)
					{
						lst.Add(new ReadOnlyPropertyDesc(p));
					}
				}
				else
				{
					lst.Add(p);
				}
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
		#region INewObjectInit Members
		[Browsable(false)]
		[NotForProgramming]
		public void OnNewInstanceCreated()
		{
			Type t = this.GetType();
			if (!typeof(EasyGrid).Equals(t) && !typeof(EasyGridDetail).Equals(t))
			{
				CreateDataTable();
			}
		}

		#endregion
		#region Array Import and Export
		[Description("Indicates data source type.")]
		public EnumDataSourceType BindSourceType
		{
			get
			{
				if (_query != null)
				{
					return _query.BindSourceType;
				}
				return EnumDataSourceType.None;
			}
		}
		[Description("Remove all data from the data table and the array.")]
		public void ClearData()
		{
			if (_query != null)
			{
				_query.ClearData();
			}
		}
		[Description("Returns data in an array of arrays. Each array is a column of data.")]
		public Array[] ExportToColumnArrays()
		{
			if (_query != null)
			{
				return _query.ExportToColumnArrays();
			}
			return new Array[] { };
		}
		[Description("Import data from an array of arrays. Each array is a column of data. Original data will be erased.")]
		public void ImportFromColumnArrays(Array[] data)
		{
			createtable();
			QueryDef.ImportFromColumnArrays(data);
			this.Query();
		}
		[Description("Returns data in an array of arrays. Each array is a row of data.")]
		public Array[] ExportToRowArrays()
		{
			if (_query != null)
			{
				return _query.ExportToRowArrays();
			}
			return new Array[] { };
		}
		[Description("Import data from an array of arrays. Each array is a row of data. Original data will be erased if append is false.")]
		public void ImportFromRowArrays(Array[] data, bool append)
		{
			createtable();
			QueryDef.ImportFromRowArrays(data, append);
			this.Query();
		}
		[Description("Returns an array containing a column of data. Parameter columnIndex specifies the column.")]
		public Array ExportColumnData(int columnIndex)
		{
			if (_query != null)
			{
				return _query.ExportColumnData(columnIndex);
			}
			return null;
		}
		[Description("Set data of the specified column. The original data in the column will be overwritten. The data are saved in an internal memory. If applyData is true then all data in the memory will be passed to the data table and displayed on the screen.")]
		public void ImportColumnData(int columnIndex, Array data, bool applyData)
		{
			createtable();
			QueryDef.ImportColumnData(columnIndex, data, applyData);
			if (applyData)
			{
				this.Query();
			}
		}
		[Description("Returns an array containing a row of data. Parameter rowIndex specifies the row.")]
		public Array ExportRow(int rowIndex)
		{
			if (_query != null)
			{
				return _query.ExportRow(rowIndex);
			}
			return null;
		}
		[Description("Import one row of data. Parameter rowIndex specifies the row. If rowIndex is -1 then a new row will be appended.")]
		public void ImportRow(Array data, int rowIndex)
		{
			createtable();
			QueryDef.ImportRow(data, rowIndex);
			this.Query();
		}
		[Description("Get data specified by rowIndex and columnIndex")]
		public object GetCellData(int rowIndex, int columnIndex)
		{
			if (_query != null)
			{
				return _query.GetCellData(rowIndex, columnIndex);
			}
			return null;
		}
		[Description("Get data on the current row and the column specified by columnIndex")]
		public object GetCellDataCurrentRow(int columnIndex)
		{
			if (_query != null)
			{
				return _query.GetCellData(_rowIndex, columnIndex);
			}
			return null;
		}
		[Description("Set data specified by rowIndex and columnIndex")]
		public void SetCellData(int rowIndex, int columnIndex, object data)
		{
			QueryDef.SetCellData(rowIndex, columnIndex, data);
		}
		[Description("Import data from a CSV file. If includeHeader is true then the first line of the file contains column names. If append is false then the original data will be erased.")]
		public void ImportFromCsvFile(string filename, bool includeHeader, bool append)
		{
			createtable();
			QueryDef.ImportFromCsvFile(filename, includeHeader, append);
			this.Query();
		}
		[Description("Write data to a CSV file. If append is true then the data will be appended to the file. If append is false then the data will overwrite existing file. If includeHeader is true the append is false then column names will be written as the first line of the file.")]
		public void ExportToCsvFile(string filename, bool includeHeader, bool append, EnumCharEncode encode)
		{
			if (_query != null)
			{
				_query.ExportToCsvFile(filename, includeHeader, append, encode);
			}
		}
		#endregion
		#region IFieldsHolder Members

		#endregion
		#region IMethodParameterAttributesProvider Members
		[NotForProgramming]
		[Browsable(false)]
		public Dictionary<string, Attribute[]> GetParameterAttributes(string methodName)
		{
			if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0)
			{
				Dictionary<string, Attribute[]> pas = new Dictionary<string, Attribute[]>();
				pas.Add("where", new Attribute[] { new RefreshPropertiesAttribute(RefreshProperties.All) });
				return pas;
			}
			return null;
		}

		#endregion
		#region IControlDeserialize Members
		[Browsable(false)]
		[NotForProgramming]
		public void OnDeserialized()
		{
			_columnsCache = new List<DataGridViewColumn>();
			foreach (DataGridViewColumn c in this.Columns)
			{
				_columnsCache.Add(c);
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void OnAddedToControls()
		{
			if (this.Columns.Count == 0)
			{
				if (_columnsCache != null && _columnsCache.Count > 0)
				{
					for (int i = 0; i < _columnsCache.Count; i++)
					{
						this.Columns.Add(_columnsCache[i]);
					}
				}
			}
		}
		#endregion
	}
	public enum EnumDataSourceType { None = 0, Database = 1, Array }
	public class EventArgsDropOnCell : EventArgs
	{
		private DataGridView.HitTestInfo _hittest;
		private DragEventArgs _eventArgs;
		public EventArgsDropOnCell(DataGridView.HitTestInfo hitTest, DragEventArgs eventArgs)
		{
			_hittest = hitTest;
			_eventArgs = eventArgs;
		}
		[Description("Gets dropped data as a string")]
		public string DataString
		{
			get
			{
				return _eventArgs.Data.GetData(typeof(string)) as string;
			}
		}
		[Description("Gets the index of the column that contains the coordinates described by the current DataGridView.HitTestInfo")]
		public int ColumnIndex { get { return _hittest.ColumnIndex; } }
		[Description("Gets the x-coordinate of the beginning of the column that contains the coordinates described by the current DataGridView.HitTestInfo")]
		public int ColumnX { get { return _hittest.ColumnX; } }
		[Description("Gets the index of the row that contains the coordinates described by the current DataGridView.HitTestInfo")]
		public int RowIndex { get { return _hittest.RowIndex; } }
		[Description("Gets the y-coordinate of the top of the row that contains the coordinates described by the current DataGridView.HitTestInfo")]
		public int RowY { get { return _hittest.RowY; } }
		[Description("Gets the DataGridViewHitTestType that indicates which part of the DataGridView the coordinates described by the current DataGridView.HitTestInfo belong to")]
		public DataGridViewHitTestType Type { get { return _hittest.Type; } }
		[Description("Gets which drag-and-drop operations are allowed by the originator (or source) of the drag event.")]
		public DragDropEffects AllowedEffect { get { return _eventArgs.AllowedEffect; } }
		[Description("Gets the IDataObject that contains the data associated with this event.")]
		public IDataObject Data { get { return _eventArgs.Data; } }
		[Description("Gets the target drop effect in a drag-and-drop operation.")]
		public DragDropEffects Effect { get { return _eventArgs.Effect; } }
		[Description("Gets the current state of the SHIFT, CTRL, and ALT keys, as well as the state of the mouse buttons.")]
		public int KeyState { get { return _eventArgs.KeyState; } }
		[Description("Gets the x-coordinate of the mouse pointer, in screen coordinates.")]
		public int X { get { return _eventArgs.X; } }
		[Description("Gets the y-coordinate of the mouse pointer, in screen coordinates.")]
		public int Y { get { return _eventArgs.Y; } }
	}
	public delegate void EventHandlerGridViewDragDrop(object sender, EventArgsDropOnCell dropInfo);
}
