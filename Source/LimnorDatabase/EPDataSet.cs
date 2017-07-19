/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Database support
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.ComponentModel;
using System.Drawing.Design;
using System.Data;
using VPL;
using System.Data.OracleClient;
using System.Xml.Serialization;
using System.Collections.Generic;
using TraceLog;
using System.Reflection;
using System.Text;
using System.Xml;
using Limnor.WebServerBuilder;
using System.Globalization;
using System.Net;
using Limnor.WebBuilder;
using WebServerProcessor;
using MathExp;
using System.CodeDom;
using System.Web;
using ProgElements;

namespace LimnorDatabase
{
	/// <summary>
	/// As a master:
	///     
	/// </summary>
	[Designer(typeof(EasyDataSetDesigner))]
	[global::System.ComponentModel.ToolboxItem(true)]
	[global::System.ComponentModel.Design.HelpKeywordAttribute("vs.data.DataSet")]
	[Description("It combines EasyQuery and DataSet into one component. It provides a Query Builder to let you build SQL query. It can also be used to provide master-records in a master-detail relationship.")]
	[ToolboxBitmap(typeof(EasyDataSet), "Resources.easyDataset.bmp")]
	public class EasyDataSet : DataSet, ICloneable, IComponent, IDatabaseAccess, IReport32Usage, IMasterSource, IBeforeListItemSerialize, ISourceValueEnumProvider, IDynamicMethodParameters, ISqlUser, IWebServerProgrammingSupport, ISupportWebClientMethods, IWebServerComponentCreator, IDataTableUpdator, IDataSetSource, ICustomMethodCompiler, IResetOnComponentChange, IDevClassReferencer, ICategoryDataSetSource, IWebClientSupport, IMethodParameterAttributesProvider, ICustomTypeDescriptor, IWebClientInitializer, IEnumerable, IWebServerCompilerHolder, IFieldsHolder, IWebClientPropertyValueHolder, IPhpServerObject
	{
		#region fields and constructors
		private IMasterSource _master;
		private string _masterFunction;
		private Dictionary<string, string> _keyfieldcode;
		private IWebServerCompiler _webServerCompiler;
		private EasyQuery _qry;
		private IDevClass _holder;
		private object _locker = new object();
		private BindingSource _bindingSource;
		private BindingManagerBase _bindingManager;
		private string _tableName;
		static private StringCollection _subClassReadOnlyProperties;
		static private StringCollection _subClassExcludeProperties;
		static private Dictionary<string, EasyQuery> _sqlQueries;
		private JsonWebServerProcessor _webPage;
		private DbParameterListExt _dynamicParameters;
		private StringCollection _initCoe;
		private static void staticInit()
		{
			if (_subClassExcludeProperties == null)
			{
				_subClassExcludeProperties = new StringCollection();
				_subClassExcludeProperties.Add("Field_FieldText");
				_subClassExcludeProperties.Add("Field_IsIdentity");
				_subClassExcludeProperties.Add("Field_OleDbType");
				_subClassExcludeProperties.Add("Field_FromTableName");
				_subClassExcludeProperties.Add("Field_ReadOnly");
				_subClassExcludeProperties.Add("Field_DataSize");
				_subClassExcludeProperties.Add("Field_Indexed");
				_subClassExcludeProperties.Add("Field_Name");
				_subClassExcludeProperties.Add("Field_FieldCaption");
				//
				_subClassExcludeProperties.Add("Param_OleDbType");
				_subClassExcludeProperties.Add("Param_Name");
				_subClassExcludeProperties.Add("SqlParameternames");
				_subClassExcludeProperties.Add("Tables");
				_subClassExcludeProperties.Add("SqlQuery");
			}
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
				_subClassReadOnlyProperties.Add("Distinct");
				_subClassReadOnlyProperties.Add("UpdatableTableName");
				_subClassReadOnlyProperties.Add("DataPreparer");
				_subClassReadOnlyProperties.Add("TableName");
				_subClassReadOnlyProperties.Add("Parameters");
			}
		}
		static EasyDataSet()
		{
			staticInit();
		}
		public EasyDataSet()
		{
			staticInit();
			QueryOnStart = true;
			AutoConvertUTCforWeb = false;
			DataSetName = "Data_" + Guid.NewGuid().ToString("D");
			_qry = new EasyQuery();
			_qry.DataStorage = this;
			_qry.LastErrorMessageChanged += new EventHandler(_qry_LastErrorMessageChanged);
			_qry.DataFilled += new EventHandler(_qry_DataFilled);
			_qry.SqlChanged += new EventHandler(_qry_SqlChanged);
			_qry.BeforePrepareCommands += new EventHandler(_qry_BeforePrepareCommands);
			_qry.BeforeQuery += new EventHandler(_qry_BeforeQuery);
			_qry.AfterQuery += new EventHandler(_qry_AfterQuery);
			_qry.BeforeDeleteRecordHandler = _qry_BeforeDeleteRecord;
			_qry.AfterDeleteRecordHandler = _qry_afterDeleteRecord;
		}
		void _qry_BeforeDeleteRecord(object sender, EventArgs e)
		{
			if (IsEmptyChanged != null)
			{
				IsEmptyChanged(this, new EventArgsDataFill(_qry.CommitedRowCount));
			}
		}
		void _qry_afterDeleteRecord(object sender, EventArgs e)
		{
			if (IsEmptyChanged != null)
			{
				IsEmptyChanged(this, new EventArgsDataFill(_qry.CommitedRowCount));
			}
		}
		void _qry_AfterQuery(object sender, EventArgs e)
		{
			if (AfterQuery != null)
			{
				AfterQuery(this, e);
			}
		}

		void _qry_BeforeQuery(object sender, EventArgs e)
		{
			if (BeforeQuery != null)
			{
				BeforeQuery(this, e);
			}
		}

		void _qry_BeforePrepareCommands(object sender, EventArgs e)
		{
			if (BeforePrepareCommands != null)
			{
				BeforePrepareCommands(this, e);
			}
		}

		void _qry_SqlChanged(object sender, EventArgs e)
		{
			if (SqlChanged != null)
			{
				SqlChanged(this, e);
			}
		}

		void _qry_DataFilled(object sender, EventArgs e)
		{
			if (DataFilled != null)
			{
				DataTable tbl = Tables[TableName];
				int nRowCount = 0;
				if (tbl != null)
				{
					nRowCount = tbl.Rows.Count;
				}
				EventArgsDataFill e0 = new EventArgsDataFill(nRowCount);
				DataFilled(this, e0);
			}
		}

		void _qry_LastErrorMessageChanged(object sender, EventArgs e)
		{
			if (LastErrorMessageChanged != null)
			{
				LastErrorMessageChanged(this, e);
			}
		}
		private void setupBindingManager()
		{
			if (_bindingSource != null)
			{
				_bindingManager = _bindingSource.CurrencyManager;
				_qry.SetBindingContext(_bindingManager);
			}
		}

		private void setupBindingSource()
		{
			if (_bindingSource == null)
			{
				DataTable tbl = Tables[TableName];
				if (tbl != null)
				{
					_bindingSource = new BindingSource(this, TableName);
					_bindingSource.PositionChanged += new EventHandler(_bindingSource_PositionChanged);
					setupBindingManager();
				}
			}
		}

		void _bindingSource_PositionChanged(object sender, EventArgs e)
		{
			DataRowView drv = _bindingSource.Current as DataRowView;
			if (drv != null && drv.Row != null)
			{
				if (drv.Row.RowState != DataRowState.Added && drv.Row.RowState != DataRowState.Detached && drv.Row.RowState != DataRowState.Deleted)
				{
					if (LeaveAddingRecord != null)
					{
						LeaveAddingRecord(this, EventArgs.Empty);
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
			else
			{
				if (LeaveAddingRecord != null)
				{
					LeaveAddingRecord(this, EventArgs.Empty);
				}
			}
			if (CurrentRowIndexChanged != null)
			{
				CurrentRowIndexChanged(this, EventArgs.Empty);
			}
		}
		private string getFirtsOrderField()
		{
			if (!string.IsNullOrEmpty(this.OrderBy))
			{
				string s = this.OrderBy;
				string f = FieldsParser.PopValue(ref s, ",", this.NameDelimitBegin, this.NameDelimitEnd);
				if (f.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase))
				{
					f = f.Substring(0, f.Length - 5).Trim();
				}
				return f;
			}
			return string.Empty;
		}
		private string getFirtsOrderFieldForBatchFetch()
		{
			if (this.DataBatching != null && this.DataBatching.DataBatchingAllowed())
			{
				return getFirtsOrderField();
			}
			return null;
		}
		private int getBatchTimeDelay()
		{
			if (this.DataBatching != null)
			{
				return DataBatching.BatchDelay;
			}
			return 0;
		}
		#endregion

		#region Events
		[Description("Occurs when the LastError property is changed")]
		public event EventHandler LastErrorMessageChanged;
		[Description("Occurs when data are retrieved from the database")]
		public event EventHandler DataFilled;
		[Description("Occurs when setting the SQL property succeeded")]
		public event EventHandler SqlChanged;
		[Description("This event occurs before creating database accessing commands for loading the data. SetCredential, SetConnectionString, or SelectConnection can be executed at this moment to give desired database accessing parameters.")]
		public event EventHandler BeforePrepareCommands;
		[Description("This event occurs after the database accessing commands are created and before querying the database to get the data.")]
		public event EventHandler BeforeQuery;
		[Description("This event occurs after querying the database to get the data.")]
		public event EventHandler AfterQuery;
		[Description("This event occurs before executing Update action.")]
		public event EventHandler BeforeUpdate;
		[Description("This event occurs after executing Update action.")]
		public event EventHandler AfterUpdate;

		[Description("Occurs when a CreateNewRecord action succeeds. The property RowIndex indicates the new row.")]
		public event EventHandler RowAdded;
		[Description("Occurs when a CancelEditCurrentRecord action succeeds.")]
		public event EventHandler CanceledEditCurrentRecord;

		[Description("Occurs before a DeleteCurrentRecord action is executed.")]
		public event EventHandlerWithCancel DeletingCurrentRecord;
		[Description("Occurs when a DeleteCurrentRecord action succeeds.")]
		public event EventHandler DeletedCurrentRecord;
		
		[Browsable(false)]
		[Description("This event occurs when the row count changed from 0 to non-0 or from non-0 to 0.")]
		public event EventHandler IsEmptyChanged;


		#endregion

		#region Web Events
		//
		[WebServerMember]
		[Description("This event is for web applications. This event occurs after fetching data from the database for each row feteched. This event only occurs if property FireEventOnFetchData is true.")]
		public event fnHandleDataFetch DataFetched;
		//
		[WebClientEventByServerObject]
		[WebClientMember]
		[Description("Occurs when another row becomes the current row")]
		public event EventHandler CurrentRowIndexChanged;
		//
		[WebClientOnly]
		[WebClientEventByServerObject]
		[Description("Occurs when the query finishes and the data are downloaded from web server and populated on the data-bound controls on web page.")]
		[WebClientMember]
		public event SimpleCall DataArrived { add { } remove { } }

		[WebClientOnly]
		[WebClientEventByServerObject]
		[Description("Occurs when an Update action failed. Page property ServerError represents error message.")]
		[WebClientMember]
		public event SimpleCall DataUpdateFailed { add { } remove { } }
		//
		[WebClientOnly]
		[WebClientEventByServerObject]
		[Description("Occurs when an Update action finishes on the web server and the program control goes back to the web page.")]
		[WebClientMember]
		public event fnHandleUpdate DataUpdated { add { } remove { } }
		//
		[WebClientOnly]
		[WebClientEventByServerObject]
		[Description("Occurs when a value of a cell is modified. Use properties RowIndexLastModifiedCell and ColumnIndexLastModifiedCell to identify the modified cell.")]
		[WebClientMember]
		public event SimpleCall DataEdited { add { } remove { } }
		#endregion

		#region Fields Serialization
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_Name
		{
			get
			{
				return _qry.Field_Name;
			}
			set
			{
				_qry.Field_Name = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_ReadOnly
		{
			get
			{
				return _qry.Field_ReadOnly;
			}
			set
			{
				_qry.Field_ReadOnly = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public OleDbType[] Field_OleDbType
		{
			get
			{
				return _qry.Field_OleDbType;
			}
			set
			{
				_qry.Field_OleDbType = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_IsIdentity
		{
			get
			{
				return _qry.Field_IsIdentity;
			}
			set
			{
				_qry.Field_IsIdentity = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool[] Field_Indexed
		{
			get
			{
				return _qry.Field_Indexed;
			}
			set
			{
				_qry.Field_Indexed = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public int[] Field_DataSize
		{
			get
			{
				return _qry.Field_DataSize;
			}
			set
			{
				_qry.Field_DataSize = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_FromTableName
		{
			get
			{
				return _qry.Field_FromTableName;
			}
			set
			{
				_qry.Field_FromTableName = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_FieldText
		{
			get
			{
				return _qry.Field_FieldText;
			}
			set
			{
				_qry.Field_FieldText = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Field_FieldCaption
		{
			get
			{
				return _qry.Field_FieldCaption;
			}
			set
			{
				_qry.Field_FieldCaption = value;
			}
		}
		#endregion

		#region non-Browsable Properties
		[Browsable(false)]
		[NotForProgramming]
		[DefaultValue(false)]
		public bool IsCommand
		{
			get
			{
				if (_qry != null)
					return _qry.IsCommand;
				return false;
			}
			set
			{
				if (_qry != null)
					_qry.IsCommand = value;
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public string TableVariableName
		{
			get
			{
				return string.Format(CultureInfo.InvariantCulture, "tbl{0}", TableName.GetHashCode().ToString("x", CultureInfo.InvariantCulture));
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		[XmlIgnore]
		public new DataRelationCollection Relations
		{
			get
			{
				return base.Relations;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public string SqlQuery
		{
			get
			{
				return _qry.SqlQuery;
			}
			set
			{
				_qry.SetDesignMode((Site != null && Site.DesignMode));
				_qry.SqlQuery = value;
			}
		}
		[Browsable(false)]
		public Guid ConnectionID
		{
			get
			{
				return _qry.ConnectionID;
			}
			set
			{
				_qry.ConnectionID = value;
			}
		}
		/// <summary>
		/// for setting default connection
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public string DefaultConnectionString
		{
			get
			{
				return _qry.DefaultConnectionString;
			}
			set
			{
				_qry.DefaultConnectionString = value;
			}
		}
		/// <summary>
		/// for setting default connection
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public Type DefaultConnectionType
		{
			get
			{
				return _qry.DefaultConnectionType;
			}
			set
			{
				_qry.DefaultConnectionType = value;
			}
		}
		/// <summary>
		/// If the query has more than one table, this value is "".
		/// </summary>
		[Browsable(false)]
		public string UpdatableTableName
		{
			get
			{
				return _qry.UpdatableTableName;
			}
			set
			{
				_qry.UpdatableTableName = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Param_Name
		{
			get
			{
				return _qry.Param_Name;
			}
			set
			{
				_qry.Param_Name = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public OleDbType[] Param_OleDbType
		{
			get
			{
				return _qry.Param_OleDbType;
			}
			set
			{
				_qry.Param_OleDbType = value;
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string[] Param_Value
		{
			get
			{
				return _qry.Param_Value;
			}
			set
			{
				_qry.Param_Value = value;
			}
		}
		#endregion

		#region Properties
		[Description("If this property is true then date-time values are in UTC and automatically converted to local time of web page; Update action will convert date-time values to UTC to be saved in the database.")]
		[DefaultValue(false)]
		[WebClientMember]
		[WebClientOnly]
		public bool AutoConvertUTCforWeb
		{
			get;
			set;
		}

		[DefaultValue(false)]
		[Description("Gets and sets a Boolean indicating whether the data should not be downloaded to web page. If you just want to use the data on the web server via handling event DataFetched and setting FireEventOnFetchData to True then you may set this property to True to avoid wasting network traffic for downloading the data to web page.")]
		public bool DonotDownloadDataToWebPage { get; set; }

		[ReadOnlyInProgramming]
		[DefaultValue(false)]
		[WebServerMember]
		[Description("Gets and sets a Boolean indicating whether DataFetched events should be fired for each row of data fetched.")]
		public bool FireEventOnFetchData { get; set; }

		[WebClientOnly]
		[Category("Database")]
		[Description("This is the component holding the master records. For details on using master-detail link, see http://www.limnor.com/support/webDatabaseProgramming.pdf")]
		public IMasterSource Master
		{
			get
			{
				return _master;
			}
			set
			{
				if (this != value && _master != value)
				{
					_master = value;
				}
			}
		}
		[WebClientOnly]
		[Category("Database")]
		[Description("Key fields of the master/details data tables for setting the master-details relationship. Key field names should be the same in both master and detail tables.")]
		[Editor(typeof(TypeEditorSelectFieldNames), typeof(UITypeEditor))]
		public string[] MasterKeyColumns
		{
			get;
			set;
		}

		[Editor(typeof(TypeEditorDbParameters), typeof(UITypeEditor))]
		[Category("Database")]
		[Description("These parameters are defined for QueryWithWhereDynamic actions. ")]
		public DbParameterListExt DynamicParameters
		{
			get
			{
				if (_dynamicParameters == null)
				{
					ParameterList ps = new ParameterList();
					_dynamicParameters = new DbParameterListExt(ps);
				}
				return _dynamicParameters;
			}
			set
			{
				_dynamicParameters = value;
			}
		}
		[Category("Database")]
		[Description("The data binding source to provide data held in this DataSet to other controls")]
		public BindingSource DataBindingSource
		{
			get
			{
				setupBindingSource();
				return _bindingSource;
			}
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
				if (string.IsNullOrEmpty(_tableName))
				{
					_tableName = QueryDef.TableName;
				}
				return _tableName;
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					_tableName = value;
					QueryDef.TableName = value;
				}
			}
		}
		[Category("Database")]
		[Description("Accessing this property will test connect to the database. If the connection is made then this property is True; otherwise it is False.")]
		public bool IsConnectionReady
		{
			get
			{
				return _qry.IsConnectionReady;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[Description("Gets a Boolean indicating whether a Search action is executing.")]
		[ReadOnly(true)]
		[XmlIgnore]
		public bool IsSearching
		{
			get
			{
				return _qry.IsSearching;
			}
			set
			{
				_qry.IsSearching = value;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[ReadOnly(true)]
		[XmlIgnore]
		[Description("When retrieving values by Fields property, this property indicates the row number. 0 indicates the first row; 1 indicates the second row, and so on.")]
		public int RowIndex
		{
			get
			{
				return _qry.RowIndex;
			}
			set
			{
				_qry.RowIndex = value;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[Description("Gets an integer which is the last auto number by calling CreateNewRecord")]
		public int LastInsertID
		{
			get
			{
				return 0;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[Description("The number of records currently hold in the DataTable")]
		public int RowCount
		{
			get
			{
				return _qry.RowCount;
			}
		}
		[Category("Database")]
		[Description("The number of columns in the DataTable")]
		public int ColumnCount
		{
			get
			{
				if (_qry.DataTable == null)
					return 0;
				return _qry.DataTable.Columns.Count;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[Description("Gets an integer which is a row index of the last modified row")]
		public int RowIndexLastModifiedCell
		{
			get
			{
				return 0;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[Description("Gets an integer which is a column index of the last modified row")]
		public int ColumnIndexLastModifiedCell
		{
			get
			{
				return 0;
			}
		}
		[Category("Database")]
		[Description("The number of records currently hold in the DataTable")]
		public bool HasData
		{
			get
			{
				return _qry.HasData;
			}
		}
		[Category("Database")]
		[Description("The DataTable for holding the records for this instance")]
		public DataTable DataTable
		{
			get
			{
				return _qry.DataTable;
			}
		}
		[Browsable(false)]
		[Description("The rows holding the data used by this control")]
		public DataRowCollection DataRows
		{
			get
			{
				if (_qry != null && _qry.DataTable != null)
				{
					return _qry.DataTable.Rows;
				}
				return null;
			}
		}

		public object[][] DataArray
		{
			get
			{
				DataRowCollection rs = DataRows;
				if (rs == null)
				{
					return null;
				}
				else
				{
					object[][] data = new object[this.RowCount][];
					for (int i = 0; i < this.RowCount; i++)
					{
						data[i] = new object[this.ColumnCount];
						for (int j = 0; j < this.ColumnCount; j++)
						{
							data[i][j] = rs[i].ItemArray[j];
						}
					}
					return data;
				}
			}
		}
		[Category("Database")]
		[Description("Description for this query")]
		public string Description
		{
			get { return _qry.Description; }
			set { _qry.Description = value; }
		}

		[DefaultValue(null)]
		[NotForProgramming]
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
		[DefaultValue(null)]
		[NotForProgramming]
		public string[] DataPreparerParameternames
		{
			get
			{
				return QueryDef.DataPreparerParameternames;
			}
			set
			{
				QueryDef.DataPreparerParameternames = value;
			}
		}
		[DefaultValue(null)]
		[NotForProgramming]
		public string[] SqlParameternames
		{
			get
			{
				return QueryDef.SqlParameternames;
			}
			set
			{
				QueryDef.SqlParameternames = value;
			}
		}

		[DefaultValue(false)]
		[Category("Query")]
		[Description("Indicates whether duplicated records are allowed")]
		public bool Distinct
		{
			get { return _qry.Distinct; }
			set { _qry.Distinct = value; }
		}
		[DefaultValue(0)]
		[Category("Query")]
		[Description("Specifies that the query result contain a specific number of rows or a percentage of rows of the query result. Following keyword TOP, you can specify 1 to 32,767 rows, or, if you include the PERCENT option, you can specify 0.01 to 99.99 percent. ")]
		public int Top
		{
			get { return _qry.Top; }
			set { _qry.Top = value; }
		}
		[DefaultValue(false)]
		[Category("Query")]
		[Description("Specifies that the query result contain a specific number of rows or a percentage of rows of the query result. Following keyword TOP, you can specify 1 to 32,767 rows, or, if you include the PERCENT option, you can specify 0.01 to 99.99 percent. ")]
		public bool Percent
		{
			get { return _qry.Percent; }
			set { _qry.Percent = value; }
		}
		[DefaultValue(false)]
		[Category("Query")]
		[Description("Specifies that additional rows be returned from the base result set with the same value in the ORDER BY columns appearing as the last of the TOP n (PERCENT) rows. TOP…WITH TIES can be specified only in SELECT statements, and only if an ORDER BY clause is specified.")]
		public bool WithTies
		{
			get { return _qry.WithTies; }
			set { _qry.WithTies = value; }
		}
		[WebClientMember]
		[Category("Database")]
		[XmlIgnore]
		[ReadOnly(true)]
		[Editor(typeof(HideUITypeEditor), typeof(UITypeEditor))]
		[Description("Fields in this data table. It represents the current row at runtime.")]
		public FieldList Fields
		{
			get
			{
				return _qry.Fields;
			}
			set
			{
				_qry.Fields = value;
			}
		}
		public int FieldCount
		{
			get
			{
				return _qry.Fields.Count;
			}
		}
		[WebClientMember]
		[WebClientOnly]
		[Description("It lists fields as data-binding sources. Create an action to set a property to a field to establish data-binding at runtime")]
		public NameList BindingFields
		{
			get
			{
				FieldList fl = _qry.Fields;
				string[] ss = new string[fl.Count];
				for (int i = 0; i < ss.Length; i++)
				{
					ss[i] = fl[i].Name;
				}
				return new NameList(ss);
			}
		}
		[Category("Database")]
		[Description("Parameters for filtering the data.")]
		public DbParameterList Parameters
		{
			get
			{
				return new DbParameterList(_qry.Parameters);
			}
		}
		[Category("Database")]
		[Description("Indicate whether the data is changeable")]
		public bool ReadOnly
		{
			get
			{
				return _qry.ReadOnly;
			}
		}
		[Category("Database")]
		[DefaultValue(false)]
		[Description("Indicate whether the changing of data is desired")]
		public bool ForReadOnly
		{
			get
			{
				return _qry.ForReadOnly;
			}
			set
			{
				_qry.ForReadOnly = value;
			}
		}
		[Category("Query")]
		[Description("'FROM' clause of the SELECT statement")]
		public string From
		{
			get { return _qry.From; }
			set { _qry.From = value; }
		}

		[Category("Query")]
		[Description("'LIMIT' clause of the SELECT statement")]
		public string Limit
		{
			get { return _qry.Limit; }
			set { _qry.Limit = value; }
		}

		[Category("Query")]
		[Description("'WHERE' clause of the SELECT statement")]
		public string Where
		{
			get { return _qry.Where; }
			set { _qry.Where = value; }
		}
		[Category("Query")]
		[Description("'GROUP BY' clause of the SELECT statement")]
		public string GroupBy
		{
			get { return _qry.GroupBy; }
			set { _qry.GroupBy = value; }
		}
		[Category("Query")]
		[Description("'HAVING' clause of the SELECT statement")]
		public string Having
		{
			get { return _qry.Having; }
			set { _qry.Having = value; }
		}
		[Category("Query")]
		[Description("'ORDER BY' clause of the SELECT statement")]
		public string OrderBy
		{
			get { return _qry.OrderBy; }
			set { _qry.OrderBy = value; }
		}
		[Category("Database")]
		[Description("Error message from the last failed operation. For web applications, the property name is ErrorMessage so that the property naming is consistent with DatabaseExecuter.")]
		public string LastError
		{
			get
			{
				return _qry.LastError;
			}
		}
		[WebClientMember]
		[Category("Database")]
		[Description("Gets the error message for the last Execute action if it failed. It is the same as LastError. It is for web applications.")]
		public string ErrorMessage
		{
			get
			{
				return LastError;
			}
		}
		[Category("Database")]
		[DefaultValue(true)]
		[Description("Controls whether error message is displayed when an error occurs. Error messages are logged in a text file. The file path is indicated by LogFile property.")]
		public bool ShowErrorMessage
		{
			get
			{
				return _qry.ShowErrorMessage;
			}
			set
			{
				_qry.ShowErrorMessage = value;
			}
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Get the number of modified records on a web page")]
		public int ModifiedRowCount
		{
			get
			{
				return 0;
			}
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Get the number of deleted records on a web page")]
		public int DeletedRowCount
		{
			get
			{
				return 0;
			}
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Get the number of new records on a web page")]
		public int NewRowCount
		{
			get
			{
				return 0;
			}
		}
		private FieldsPropertyCollection _fieldsForImage;
		private bool[] _isFieldImage;
		[Editor(typeof(TypeEditorFieldProperties), typeof(UITypeEditor))]
		[WebClientOnly]
		[Description("This property is for web project use. This is an array of Boolean values. It has the same number of elements as the number of the columns. Each array element indicates whether the corresponding column is an image field. It is for html table data display at the time of data-binding. For a blob field, setting it to true for the field will make the data to be treated as binary image data; setting it to false will make the data to be treated as large text. For a text field, setting it to true will make the data to be treated as an image url.")]
		public FieldsPropertyCollection IsFieldForImage
		{
			get
			{
				if (_fieldsForImage == null)
				{
					_fieldsForImage = new FieldsPropertyCollection(this);
				}
				return _fieldsForImage;
			}
		}
		[WebClientMember]
		[Description("Gets a Boolean indicating that the data querying has finished.")]
		public bool IsDataReady
		{
			get
			{
				return IsQueryOK;
			}
		}

		private DataBatch _batching;
		[NotForProgramming]
		[DefaultValue(0)]
		[WebClientOnly]
		[Description("For details on enabling data streaming see http://www.limnor.com/support/webDatabaseProgramming.pdf. If data batching is enabled then the records should be fetched from web server batch by batch, the BatchSize value indicates roughly how many records will be fetched each time. OrderBy must be used to enable batch-fetching. The first field of OrderBy should be included in the field list of the SELECT clause. The first field of OrderBy should not have too many duplications in records.")]
		public DataBatch DataBatching
		{
			get
			{
				if (_batching == null)
					_batching = new DataBatch(this);
				return _batching;
			}
			set
			{
				if (value != null)
				{
					_batching = value;
					_batching.SetOwner(this);
					//if (string.IsNullOrEmpty(_batching.KeyField))
					//{
					//    _batching.KeyField = getFirtsOrderField();
					//}
				}
			}
		}

		[Browsable(false)]
		public bool[] IsFieldImage
		{
			get
			{
				if (_isFieldImage == null)
				{
					_isFieldImage = new bool[this.FieldCount];
				}
				else
				{
					if (_isFieldImage.Length != this.FieldCount)
					{
						bool[] a = new bool[this.FieldCount];
						for (int i = 0; i < _isFieldImage.Length && i < this.FieldCount; i++)
						{
							a[i] = _isFieldImage[i];
						}
						_isFieldImage = a;
					}
				}
				return _isFieldImage;
			}
			set
			{
				_isFieldImage = value;
			}
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
		public void SetSqlContext(string name)
		{

		}
		[NotForProgramming]
		[Browsable(false)]
		public IList<Guid> DatabaseConnectionsUsed
		{
			get
			{
				if (_qry != null)
				{
					return _qry.DatabaseConnectionsUsed;
				}
				return new List<Guid>();
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public IList<Type> DatabaseConnectionTypesUsed
		{
			get
			{
				if (_qry != null)
				{
					return _qry.DatabaseConnectionTypesUsed;
				}
				return new List<Type>();
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
		[NotForProgramming]
		[Browsable(false)]
		public EasyQuery QueryDef
		{
			get
			{
				return _qry;
			}
		}
		[Browsable(false)]
		[DefaultValue("")]
		public string CommandText
		{
			get
			{
				if (_qry != null)
					return _qry.CommandText;
				return "";
			}
			set
			{
				if (_qry != null)
					_qry.CommandText = value;
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
				QueryDef.SQL = value;
			}
		}
		[Category("Database")]
		[XmlIgnore]
		[ReadOnly(true)]
		[Editor(typeof(TypeEditorSelectConnection), typeof(UITypeEditor))]
		[Description("Connection to the database")]
		public ConnectionItem DatabaseConnection
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
		[XmlIgnore]
		[ParenthesizePropertyName(true)]
		[Description("The name of the component")]
		public string Name
		{
			get
			{
				if (Site != null && !string.IsNullOrEmpty(Site.Name))
				{
					return Site.Name;
				}
				return QueryDef.Name;
			}
			set
			{
				if (Site != null && !string.IsNullOrEmpty(Site.Name))
				{
					Site.Name = value;
				}
				QueryDef.SetName(value);
			}
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Change sorting of the data rows using specified field as key.")]
		public void SortOnColumn(string fieldName, bool sortingAscend, bool ignoreCase)
		{
		}
		[WebServerMember]
		[Description("Compose the query commands and query the database for data")]
		public bool Query()
		{
			_qry.LogMessage("Calling Query from EasyDataSet");
			if (_qry.DataStorage != null && _qry.DataStorage.Tables.Count > 0)
			{
				_qry.RemoveTable(TableName);
			}
			_qry.ResetCanChangeDataSet(false);
			bool bOK = _qry.Query();
			setupBindingSource();
			if (_webPage != null)
			{
				_webPage.SetServerComponentName(TableName);
			}
			if (bOK)
			{
				if (DataFetched != null)
				{
					for (int i = 0; i < _qry.DataTable.Rows.Count; i++)
					{
						DataFetched(i, new WebDataRow(_qry.DataTable.Rows[i].ItemArray));
					}
				}
			}
			return bOK;
		}
		public void CreateRandomNewRecord()
		{
			if (_qry != null)
			{
				_qry.LogMessage("Calling CreateRandomNewRecord from EasyDataSet");
				if (_qry.DataStorage != null && _qry.DataStorage.Tables.Count > 0)
				{
					_qry.RemoveTable(TableName);
				}
				_qry.ResetCanChangeDataSet(false);
				_qry.CreateRandomNewRecord();
				setupBindingSource();
			}
		}
		[WebServerMember]
		[Description("Create a new record. On finishing ths method the new record is already saved to the database so that relational links can be made to the new record.")]
		public bool CreateNewRecord(params object[] values)
		{
			if (_qry != null)
			{
				_qry.LogMessage("Calling CreateNewRecord from EasyDataSet");
				if (_qry.DataStorage != null && _qry.DataStorage.Tables.Count > 0)
				{
					_qry.RemoveTable(TableName);
				}
				_qry.ResetCanChangeDataSet(false);
				_qry.CreateNewRecord(values);
				setupBindingSource();
				if (RowAdded != null)
				{
					RowAdded(this, EventArgs.Empty);
				}
				return true;
			}
			return false;
		}
		[RefreshProperties(RefreshProperties.All)]
		[WebServerMember]
		[Description("Execute database query with WHERE clause and parameter values defined in the DynamicParameters property.")]
		public bool QueryWithWhereDynamic(string where, params object[] valuesDynamic)
		{
			if (_qry != null)
			{
				_qry.LogMessage("Calling  QueryWithWhereDynamic from EasyDataSet");
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
					int pvndynamic = 0;
					if (_dynamicParameters != null)
					{
						pvndynamic = _dynamicParameters.Count;
					}
					if (valuesDynamic != null)
						pvn = valuesDynamic.Length;
					_qry.LogMessage("Parameter count:{0}", pn);
					_qry.LogMessage("Dynamic Parameter value count:{0}", pvn);
					_qry.LogMessage("Dynamic Parameter definition count:{0}", pvndynamic);
					object[] values = new object[pn];
					if (pn > 0)
					{
						if (pvn != pvndynamic)
						{
							throw new ExceptionLimnorDatabase("Counts for dynamic parameter values and definitions do not match. Definition count:{0}; value count:{1}", pvndynamic, pvn);
						}
						StringBuilder sbErr = new StringBuilder();
						for (int i = 0; i < pn; i++)
						{
							bool found = false;
							for (int k = 0; k < pvndynamic; k++)
							{
								if (string.Compare(_dynamicParameters[k].Name, pl[i].Name, StringComparison.OrdinalIgnoreCase) == 0)
								{
									values[i] = valuesDynamic[k];
									found = true;
									break;
								}
							}
							if (!found)
							{
								sbErr.Append("parameter [");
								sbErr.Append(pl[i].Name);
								sbErr.Append("] in WHERE is not defined");
							}
						}
						if (sbErr.Length > 0)
						{
							throw new ExceptionLimnorDatabase(sbErr.ToString());
						}
						pvn = pn;
					}
					if (values != null && values.Length > 0 && pl != null)
					{
						int n = Math.Min(pn, pvn);
						for (int i = 0; i < n; i++)
						{
							if (values[i] is DateTime)
							{
								if (!EPField.IsDatetime(pl[i].OleDbType))
								{
									pl[i].OleDbType = OleDbType.DBTimeStamp;
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
		[RefreshProperties(RefreshProperties.All)]
		[WebServerMember]
		[Description("Execute database query with a SQL statement and parameter values. The parameters are included in parameter sql with leading @ symbol.")]
		public bool QueryWithSQL(string sql, params object[] values)
		{
			this.QueryDef.LogMessage("Calling  QueryWithSQL from EasyDataSet");
			SQLStatement sqls = new SQLStatement(sql);
			this.QueryDef.UnsetLoaded();
			this.SQL = sqls;
			this.QueryDef.SetLoaded();
			return this.QueryWithParameterValues(values);
		}
		[RefreshProperties(RefreshProperties.All)]
		[WebServerMember]
		[Description("Execute database query with WHERE clause and parameter values. The parameters can be in the new WHERE clause.")]
		public bool QueryWithWhere(string where, params object[] values)
		{
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
									pl[i].OleDbType = OleDbType.DBTimeStamp;
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
		[WebServerMember]
		[Description("Execute database query with the parameter values. The parameters must be defined in the DynamicParameters property at design time.")]
		public bool QueryWithParameterValues(params object[] values)
		{
			if (_qry != null)
			{
				_qry.LogMessage("Calling  QueryWithParameterValues from EasyDataSet");
				QueryDef.PrepareQueryIfChanged();
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
								pl[i].OleDbType = OleDbType.DBTimeStamp;
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
			return false;
		}
		[Description("Query the database for data. It may be called after setting parameter values. ")]
		public void FetchData()
		{
			if (_qry != null)
			{
				_qry.LogMessage("Calling FetchData from EasyDataSet");
			}
			Query();
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CopyFrom(EasyQuery query)
		{
			if (_qry != null && query != null)
			{
				_qry.CopyFrom(query);
			}
		}
		#endregion

		#region ICloneable Members
		[Browsable(false)]
		[NotForProgramming]
		public new object Clone()
		{
			EasyDataSet ds = Activator.CreateInstance(this.GetType()) as EasyDataSet;
			if (_qry != null)
			{
				ds._qry = (EasyQuery)_qry.Clone();
			}
			return ds;
		}

		#endregion

		#region Web Client Methods
		[WebClientOnly]
		[Description("Save all records to HTML5 storage. These records can be loaded by calling LoadFromHTML5Storage. Use dataName to uniquely identify the data in HTML5 storage. Use only alphanumeric letters in dataName")]
		[WebClientMember]
		public void SaveToHTML5Storage(string dataName)
		{
		}
		[WebClientOnly]
		[Description("Load records from HTML5 storage. These records were saved by calling SaveToHTML5Storage. Use the same dataName when SaveToHTML5Storage was called.")]
		[WebClientMember]
		public void LoadFromHTML5Storage(string dataName)
		{
		}
		[Description("Remove all records from HTML5 storage. dataName must be the value used when calling SaveToHTML5Storage to save records to HTML5 storage.")]
		[WebClientMember]
		[WebClientOnly]
		public void RemoveFromHTML5Storage(string dataName)
		{
		}
		[WebClientOnly]
		[Description("Apply an empty dataset to the web page. Web server and database are not connected and used.")]
		[WebClientMember]
		public void PseudoQuery()
		{
		}
		[WebClientOnly]
		[Description("Switch web client event handler at runtime")]
		[WebClientMember]
		public void SwitchEventHandler(string eventName, VplMethodPointer handler)
		{
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("This object may have a master if properties Master and MasterKeyColumns are set properly. This method re-fetches rows from the database for the current master record.")]
		public void RefetchRowsForCurrentMaster()
		{
		}
		[WebClientMember]
		[Description("Set field value for the current record")]
		public void SetFieldValue(string fieldName, object value)
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			_qry.SetFieldValue(fieldName, value);
		}

		[WebClientMember]
		[Description("Set field value for the record specified by rowNumber")]
		public void SetFieldValueEx(int rowNumber, string fieldName, object value)
		{
			_qry.SetFieldValueEx(rowNumber, fieldName, value);
		}

		[WebClientMember]
		[Description("Return true if the field value for the current record is null")]
		public bool IsFieldValueNull(string fieldName)
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			return _qry.IsFieldValueNull(fieldName);
		}
		[WebClientMember]
		[Description("Return true if the field value for the current record is null or an empty string")]
		public bool IsFieldValueNullOrEmpty(string fieldName)
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			return _qry.IsFieldValueNullOrEmpty(fieldName);
		}
		[WebClientMember]
		[Description("Return true if the field value for the current record is not null")]
		public bool IsFieldValueNotNull(string fieldName)
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			return _qry.IsFieldValueNotNull(fieldName);
		}
		[WebClientMember]
		[Description("Return true if the field value for the current record is not null and not an empty string")]
		public bool IsFieldValueNotNullOrEmpty(string fieldName)
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			return _qry.IsFieldValueNotNullOrEmpty(fieldName);
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Return true if the field value for the current record was changed by a previous editing but the changed value has not been saved to the database.")]
		public bool IsFieldValueChanged(string fieldName)
		{
			return false;
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Return true if the field value for the specified record was changed by a previous editing but the changed value has not been saved to the database.")]
		public bool IsFieldValueChangedEx(string fieldName, int rowIndex)
		{
			return false;
		}
		[WebClientMember]
		[Description("Set the next record of the first table in DataStorage as the current record")]
		public bool MoveNext()
		{
			return _qry.MoveNext();
		}
		[WebClientMember]
		[Description("Set the previous record of the first table in DataStorage as the current record")]
		public bool MovePrevious()
		{
			return _qry.MovePrevious();
		}
		[WebClientMember]
		[Description("Set the last record of the first table in DataStorage as the current record")]
		public bool MoveLast()
		{
			return _qry.MoveLast();
		}
		[WebClientMember]
		[Description("Set the first record of the first table in DataStorage as the current record")]
		public bool MoveFirst()
		{
			return _qry.MoveFirst();
		}
		[WebClientMember]
		[Description("Set the record, specified by rowIndex, of the first table in DataStorage as the current record")]
		public bool MoveToRow(int rowIndex)
		{
			if (rowIndex >= 0 && rowIndex < _qry.RowCount)
			{
				_qry.RowIndex = rowIndex;
				return true;
			}
			return false;
		}
		[WebClientMember]
		[Description("Go through each row and test the condition. The first row making the condition match becomes the current row.")]
		public bool Search(bool condition)
		{
			return false;
		}
		[WebClientOnly]
		[WebClientMember]
		[Description("Restore original data. All data modifications are discarded.")]
		public void CancelEdit()
		{

		}
		[WebClientMember]
		[Description("Add an empty new record. The record has not been saved to the database. An Update action should be used to save the record to the database.")]
		public void AddNewRecord()
		{
			_qry.AddNewRecord();
		}
		[WebClientMember]
		[Description("Delete the current record")]
		public bool DeleteCurrentRecord()
		{
			if (DeletingCurrentRecord != null)
			{
				CancelEventArgs e = new CancelEventArgs(false);
				DeletingCurrentRecord(this, e);
				if (e.Cancel)
				{
					return false;
				}
			}
			if (_qry.DeleteCurrentRecord())
			{
				if (DeletedCurrentRecord != null)
				{
					DeletedCurrentRecord(this, EventArgs.Empty);
				}
				return true;
			}
			return false;
		}
		[WebServerMember]
		[Description("Save changed data back to the database")]
		public bool Update()
		{
			if (BeforeUpdate != null)
			{
				BeforeUpdate(this, new EventArgsDataFill(_qry.RowCount));
			}
			if (_qry.Update())
			{
				if (AfterUpdate != null)
				{
					AfterUpdate(this, new EventArgsDataFill(_qry.RowCount));
				}
				if (_webPage != null)
				{
					_webPage.SetServerComponentName(TableName);
				}
				return true;
			}
			return false;
		}
		[WebClientMember]
		[Description("Calculate summary of the column specified by fieldName.")]
		public double GetColumnSum(string fieldName)
		{
			return _qry.GetColumnSum(fieldName);
		}
		[WebClientMember]
		[Description("Get value of the column specified by fieldName.")]
		public object GetFieldValue(string fieldName)
		{
			return _qry.GetColumnValue(fieldName);
		}
		[WebClientMember]
		[Description("Get value of the column specified by fieldName from the record specified by rowNumber.")]
		public object GetFieldValueEx(int rowNumber, string fieldName)
		{
			return _qry.GetColumnValueEx(rowNumber, fieldName);
		}
		[WebClientMember]
		[Description("Clears the DataSet of any data by removing all rows in all tables.")]
		public new void Clear()
		{
			base.Clear();
		}

		#endregion

		#region Static Methods
		[Description("Set the full file path for the log file for database activities. ")]
		public static void SetLogFilePath(string logFileName)
		{
			EasyQuery.SetLogFilePath(logFileName);
		}
		#endregion

		#region Internal Methods
		[NotForProgramming]
		[Browsable(false)]
		public void SetKeyFieldCode(Dictionary<string, string> fc)
		{
			_keyfieldcode = fc;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void SetMasterFunction(string name)
		{
			_masterFunction = name;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CreateDataTable()
		{
			if (_qry != null)
			{
				FieldList fl = _qry.Fields;
				if (fl != null && fl.Count > 0)
				{
					DataTable tbl = this.DataTable;
					if (tbl == null)
					{
						tbl = new DataTable(this.TableName);
						this.Tables.Add(tbl);
						for (int i = 0; i < fl.Count; i++)
						{
							tbl.Columns.Add(fl[i].CreateDataColumn());
						}
					}
				}
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public void OutputProcessMessages(HttpResponse response)
		{
			StringCollection sc = ProcessMessages;
			foreach (string s in sc)
			{
				if (!string.IsNullOrEmpty(s))
				{
					response.Write(s);
					response.Write("<br>");
				}
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string GetConnectionCodeName()
		{
			return ServerCodeutility.GetPhpMySqlConnectionName(this.ConnectionID);
		}
		#endregion

		#region Desktop methods
		public void SetRandomValuesToFields()
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			_qry.SetRandomValuesToFields();
		}
		public void SetRandomFieldValue(string fieldName)
		{
			if (_bindingManager != null)
			{
				_bindingManager.EndCurrentEdit();
			}
			_qry.SetRandomFieldValue(fieldName);
		}
		[Description("Create a data binding object. It is to be added to DataBindings property via DataBindings.Add(obj) method, where obj is created by this method.")]
		public Binding CreateDataBinding(string bindToPropertyName, string sourceFieldName)
		{
			return new Binding(bindToPropertyName, DataBindingSource, sourceFieldName, true);
		}
		[Browsable(false)]
		public DataRow CurrentDataRow
		{
			get
			{
				return _qry.CurrentDataRow;
			}
		}
		[Description("Get information about this query")]
		public string GetQueryInfo()
		{
			return _qry.GetQueryInfo();
		}
		[Description("Begin editing the current record")]
		public void BeginEditCurrentRecord()
		{
			_qry.BeginEditCurrentRecord();
		}
		[Description("Cancel editing the current record")]
		public void CancelEditCurrentRecord()
		{
			if (_qry.CancelEditCurrentRecord())
			{
				if (CanceledEditCurrentRecord != null)
				{
					CanceledEditCurrentRecord(this, EventArgs.Empty);
				}
			}
		}
		[Description("End editing the current record")]
		public void EndEditCurrentRecord()
		{
			_qry.EndEditCurrentRecord();
		}
		[Description("Set credential for accessing the database. It should be called at the event of BeforePrepareCommands and before event BeforeQuery.")]
		public void SetCredential(string user, string userPassword, string databasePassword)
		{
			_qry.SetCredential(user, userPassword, databasePassword);
		}
		[Description("Set connection string for accessing the database. For using the new connection it should be called at the event of BeforePrepareCommands and before event BeforeQuery.")]
		public void SetConnectionString(string connectionString)
		{
			_qry.SetConnectionString(connectionString);
		}
		[Description("Use a dialogue box to select database connection type and other parameters.")]
		public void SelectConnection(Form dialogParent)
		{
			_qry.SelectConnection(dialogParent);
		}
		[Description("Use a dialogue box to make database query.")]
		public void SelectQuery(Form dialogParent)
		{
			_qry.SelectQuery(dialogParent);
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
		/// <summary>
		/// 
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		[Description("Set query parameter value. ")]
		public void SetParameterValue(string parameterName, object value)
		{
			_qry.SetParameterValue(parameterName, value);
		}
		#endregion

		#region Overrides
		public override string ToString()
		{
			if (Site != null)
			{
				if (_qry != null)
				{
					return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", Site.Name, _qry.ToString());
				}
				else
				{
					return Site.Name;
				}
			}
			if (_qry != null)
			{
				return _qry.ToString();
			}
			return string.Empty;
		}
		#endregion

		#region IReport32Usage Members
		[NotForProgramming]
		[Browsable(false)]
		public string Report32Usage()
		{
			if (_qry != null)
			{
				return _qry.Report32Usage();
			}
			return string.Empty;
		}

		#endregion

		#region IMasterSource Members
		[Browsable(false)]
		public bool IsQuerying
		{
			get
			{
				if (_qry != null)
				{
					return _qry.IsQuerying;
				}
				return false;
			}
		}
		[Browsable(false)]
		public bool IsQueryOK
		{
			get
			{
				if (_qry != null)
				{
					return _qry.bQueryOK;
				}
				return false;
			}
		}
		[Browsable(false)]
		public object MasterSource
		{
			get
			{
				if (DataBindingSource == null)
					return this;
				return _bindingSource;
			}
		}
		[Browsable(false)]
		public DataSet DataStorage
		{
			get
			{
				if (_qry != null)
				{
					return _qry.DataStorage;
				}
				return this;
			}
		}
		[Browsable(false)]
		public string[] GetFieldNames()
		{
			FieldList flds = Fields;
			string[] fs = new string[flds.Count];
			for (int i = 0; i < fs.Length; i++)
			{
				fs[i] = flds[i].Name;
			}
			return fs;
		}
		[Browsable(false)]
		public string GetFieldNameByIndex(int i)
		{
			FieldList flds = Fields;
			if (i >= 0 && i < flds.Count)
			{
				return flds[i].Name;
			}
			return string.Empty;
		}
		[Browsable(false)]
		public DataColumn[] GetColumnsByNames(string[] names)
		{
			if (names == null)
			{
				return new DataColumn[] { };
			}
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
			else
			{
				return new DataColumn[] { };
			}
		}
		[Browsable(false)]
		public event EventHandler EnterAddingRecord;
		[Browsable(false)]
		public event EventHandler LeaveAddingRecord;
		#endregion

		#region IBeforeListItemSerialize Members
		[NotForProgramming]
		[Browsable(false)]
		public bool OnBeforeItemSerialize(XmlNode node, string propertyName, object item)
		{
			if (string.Compare(propertyName, "Tables", StringComparison.Ordinal) == 0)
			{
				DataTable tbl = item as DataTable;
				if (tbl != null)
				{
					if (string.Compare(tbl.TableName, TableName, StringComparison.Ordinal) != 0)
					{
						return false;
					}
				}
			}
			return true;
		}

		#endregion

		#region ISourceValueEnumProvider Members
		[NotForProgramming]
		[Browsable(false)]
		public object[] GetValueEnum(string section, string item)
		{
			if (string.CompareOrdinal(section, "SetParameterValue") == 0 && string.CompareOrdinal(item, "parameterName") == 0)
			{
				ParameterList pl = _qry.Parameters;
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
				if (string.CompareOrdinal(section, "SetFieldValue") == 0
					|| string.CompareOrdinal(section, "SetFieldValueEx") == 0
					|| string.CompareOrdinal(section, "GetFieldValue") == 0
					|| string.CompareOrdinal(section, "GetFieldValueEx") == 0
					|| string.CompareOrdinal(section, "GetColumnSum") == 0
					|| string.CompareOrdinal(section, "IsFieldValueNotNull") == 0
					|| string.CompareOrdinal(section, "IsFieldValueNull") == 0
					|| string.CompareOrdinal(section, "IsFieldValueNotNullOrEmpty") == 0
					|| string.CompareOrdinal(section, "IsFieldValueNullOrEmpty") == 0
					|| string.CompareOrdinal(section, "SortOnColumn") == 0
					|| string.CompareOrdinal(section, "IsFieldValueChanged") == 0
					|| string.CompareOrdinal(section, "IsFieldValueChangedEx") == 0
					)
				{
					FieldList fs = Fields;
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
		private string getSqlDesc()
		{
			return string.Format(CultureInfo.InvariantCulture, "SQL statement for executing a query. Original query:{0}", this.SqlQuery);
		}
		[NotForProgramming]
		[Browsable(false)]
		public ParameterInfo[] GetDynamicMethodParameters(string methodName, object av)
		{
			lock (_locker)
			{
				int nStep = 0;
				try
				{
					if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
					{
						ParameterList pl = QueryDef.Parameters;
						if (pl != null && pl.Count > 0)
						{
							nStep = 1;
							ParameterInfo[] ps = new ParameterInfo[pl.Count];
							for (int i = 0; i < pl.Count; i++)
							{
								EPField f = pl[i];
								nStep = 2;
								ps[i] = new SimpleParameterInfo(f.Name, methodName, EPField.ToSystemType(f.OleDbType), string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", f.Name));
								nStep = 3;
							}
							nStep = 4;
							return ps;
						}
						nStep = 5;
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
					else if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
					{
						nStep = 31;
						return QueryDef.GetDynamicMethodParameters(methodName, av);
					}
					else if (string.CompareOrdinal(methodName, "QueryWithWhereDynamic") == 0)
					{
						nStep = 32;
						int np = 0;
						if (_dynamicParameters != null)
						{
							np = _dynamicParameters.Count;
						}
						ParameterInfo[] ps = new ParameterInfo[np + 1];
						ps[0] = new SimpleParameterInfo("where", methodName, typeof(string), getWhereDesc());
						nStep = 33;
						for (int i = 0; i < np; i++)
						{
							nStep = 34;
							DbCommandParam f = _dynamicParameters[i];
							nStep = 35;
							ps[i + 1] = new SimpleParameterInfo(f.Name, methodName, EPField.ToSystemType(f.Type), string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", f.Name));
							nStep = 36;
						}
						nStep = 37;
						return ps;
					}
					else if (string.CompareOrdinal(methodName, "QueryWithSQL") == 0)
					{
						string sSql = null;
						nStep = 38;
						IActionContextExt ace = av as IActionContextExt;
						if (ace != null)
						{
							nStep = 39;
							object pv = ace.GetParameterValue("sql");
							nStep = 40;
							IParameterValue ipv = pv as IParameterValue;
							if (ipv != null)
							{
								nStep = 41;
								sSql = ipv.GetConstValue() as string;
							}
							else
							{
								nStep = 42;
								sSql = pv as string;
							}
						}
						else
						{
							nStep = 43;
						}
						if (!string.IsNullOrEmpty(sSql))
						{
							int np = 0;
							ParameterList pl;
							if (string.Compare(sSql, this.QueryDef.SqlQuery, StringComparison.OrdinalIgnoreCase) == 0)
							{
								pl = this.QueryDef.Parameters;
							}
							else
							{
								EasyQuery qry = null;
								if (_sqlQueries != null)
								{
									if (!_sqlQueries.TryGetValue(sSql, out qry))
									{
										qry = null;
									}
								}
								if (qry == null)
								{
									qry = new EasyQuery();
									qry.DatabaseConnection = this.DatabaseConnection;
									nStep = 44;
									qry.ResetQuery(sSql);
									nStep = 45;
									if (_sqlQueries == null)
									{
										_sqlQueries = new Dictionary<string, EasyQuery>();
									}
									_sqlQueries.Add(sSql, qry);
								}
								pl = qry.Parameters;
							}
							nStep = 46;
							if (pl != null && pl.Count > 0)
							{
								nStep = 47;
								np = pl.Count;
								nStep = 48;
							}
							else
							{
								nStep = 49;
							}
							ParameterInfo[] ps = new ParameterInfo[np + 1];
							nStep = 50;
							ps[0] = new SimpleParameterInfo("sql", methodName, typeof(string), getSqlDesc());
							nStep = 51;
							if (pl != null && pl.Count > 0)
							{
								nStep = 52;
								for (int i = 0; i < pl.Count; i++)
								{
									nStep = 53;
									EPField f = pl[i];
									nStep = 54;
									ps[i + 1] = new SimpleParameterInfo(f.Name, methodName, EPField.ToSystemType(f.OleDbType), string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", f.Name));
									nStep = 55;
								}
								nStep = 56;
							}
							else
							{
								nStep = 57;
							}
							nStep = 58;
							return ps;
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
		[NotForProgramming]
		[Browsable(false)]
		public object InvokeWithDynamicMethodParameters(string methodName, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
			{
				QueryWithParameterValues(parameters);
			}
			else if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
			{
				CreateNewRecord(parameters);
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
			else if (string.CompareOrdinal(methodName, "QueryWithSQL") == 0)
			{
				if (parameters != null && parameters.Length > 0)
				{
					string sSql = parameters[0] as string;
					if (!string.IsNullOrEmpty(sSql))
					{
						object[] vs = new object[parameters.Length - 1];
						for (int i = 1; i < parameters.Length; i++)
						{
							vs[i - 1] = parameters[i];
						}
						this.QueryWithSQL(sSql, vs);
					}
				}
			}
			return null;
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool IsUsingDynamicMethodParameters(string methodName)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
				return true;
			if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
				return true;
			if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0)
				return true;
			if (string.CompareOrdinal(methodName, "QueryWithWhereDynamic") == 0)
				return true;
			if (string.CompareOrdinal(methodName, "QueryWithSQL") == 0)
				return true;
			return false;
		}
		[Browsable(false)]
		[NotForProgramming]
		public Dictionary<string, string> GetParameterDescriptions(string methodName)
		{
			if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0 || string.CompareOrdinal(methodName, "QueryWithWhereDynamic") == 0)
			{
				Dictionary<string, string> descs = new Dictionary<string, string>();
				descs.Add("where", getWhereDesc());
				return descs;
			}
			else if (string.CompareOrdinal(methodName, "QueryWithSQL") == 0)
			{
				Dictionary<string, string> descs = new Dictionary<string, string>();
				descs.Add("sql", getSqlDesc());
				return descs;
			}
			return null;
		}
		#endregion

		#region IWebServerProgrammingSupport Members
		/// <summary>
		/// if it is true then the component instance declaration will be removed from the page
		/// </summary>
		/// <returns></returns>
		[Browsable(false)]
		[NotForProgramming]
		public bool DoNotCreate()
		{
			return true;
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool IsWebServerProgrammingSupported(EnumWebServerProcessor webServerProcessor)
		{
			return true;
		}
		private void addPhpReturn(StringCollection code, string returnReceiver)
		{
			code.Add("$msql->errorMessage=$GLOBALS[\"debugError\"].$msql->errorMessage;\r\n");
			code.Add(string.Format(CultureInfo.InvariantCulture,
					"$this->{0}->ErrorMessage=$msql->errorMessage;\r\n", this.Site.Name));
			if (!string.IsNullOrEmpty(returnReceiver))
			{
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}=(strlen($msql->errorMessage)==0);\r\n", returnReceiver));
			}
		}
		private void phpQueryWithParameterValues(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			string orderFld1 = getFirtsOrderFieldForBatchFetch();
			if (string.IsNullOrEmpty(orderFld1))
			{
				//parameters should have the same number of elements as this.Parameters
				//this.Parameters are unique
				code.Add("$msql = new JsonSourceMySql();\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->SetCredential($this->{0});\r\n", connName));
				code.Add("$msql->SetDebug($this->DEBUG);\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
				//generate $dpps and $dpsql ================================
				if (!string.IsNullOrEmpty(this.DataPreparer))
				{
					this.DataPreparer = this.DataPreparer.Trim();
				}
				string dataPreparer = this.DataPreparer;
				if (!string.IsNullOrEmpty(dataPreparer))
				{
					//assume data preparer will not use duplicated parameter names because it is a procedure call
					if (this.Parameters != null)
					{
						for (int i = 0; i < this.Parameters.Count; i++)
						{
							dataPreparer = dataPreparer.Replace(this.Parameters[i].Name, "?");
						}
					}
					code.Add(string.Format(CultureInfo.InvariantCulture, "$dpsql = \"{0} \";\r\n", dataPreparer));
					code.Add("$dpps = array();\r\n");
					if (this.Parameters != null && this.DataPreparerParameternames != null && this.DataPreparerParameternames.Length > 0)
					{
						for (int i = 0; i < this.DataPreparerParameternames.Length; i++)
						{
							int k = this.Parameters.GetIndex(this.DataPreparerParameternames[i]);
							if (k < 0)
							{
								throw new ExceptionLimnorDatabase("Data preparer Parameter [{0}] not found", this.DataPreparerParameternames[i]);
							}
							DbCommandParam dp = this.Parameters[k];
							code.Add("$dpp = new SqlClientParameter();\r\n");
							code.Add(string.Format(CultureInfo.InvariantCulture, "$dpp->name = '{0}';\r\n", dp.Name));
							code.Add(string.Format(CultureInfo.InvariantCulture, "$dpp->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
							code.Add(string.Format(CultureInfo.InvariantCulture, "$dpp->value = {0};\r\n", parameters[k]));
							code.Add("$dpps[] = $dpp;\r\n//\r\n");
						}
					}
				}
				//prepare query sql, generate $sql, $tbl, $ps ======================================================
				//data preparer's parameters may be used for sql parameters
				string sql = this.SqlQuery;
				if (!string.IsNullOrEmpty(this.DataPreparer))
				{
					sql = sql.Substring(this.DataPreparer.Length).Trim();
				}
				//sqlParams contains parameter names orderred by the appearance in sql, may duplicate
				string[] sqlParams = EasyQuery.GetParameterNames(sql, _qry.NameDelimiterBegin, _qry.NameDelimiterEnd);
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				if (sql.StartsWith("'", StringComparison.Ordinal) && sql.EndsWith("'", StringComparison.Ordinal))
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "$sql = {0};\r\n", sql));
				}
				else
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "$sql = \"{0} \";\r\n", sql));
				}
				if (this.DonotDownloadDataToWebPage)
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
				}
				else
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
				}
				code.Add("$ps = array();\r\n");
				if (this.Parameters != null)
				{
					for (int i = 0; i < sqlParams.Length; i++)//index into non-unique sequence list
					{
						int k = this.Parameters.GetIndex(sqlParams[i]);//index into the parameters, unique parameters
						if (k < 0)
						{
							throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", sqlParams[i]);
						}
						DbCommandParam dp = this.Parameters[k];
						code.Add("$p = new SqlClientParameter();\r\n");
						code.Add(string.Format(CultureInfo.InvariantCulture, "$p->name = '{0}';\r\n", dp.Name));
						code.Add(string.Format(CultureInfo.InvariantCulture, "$p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
						code.Add(string.Format(CultureInfo.InvariantCulture, "$p->value = {0};\r\n", parameters[k]));
						code.Add("$ps[] = $p;\r\n//\r\n");
					}
				}
				if (string.IsNullOrEmpty(this.DataPreparer))
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
				}
				else
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->QueryWithPreparer($dpsql,$dpps,${0},$sql,$ps);\r\n", this.TableVariableName));
				}
				addPhpReturn(code, returnReceiver);
			}
			else
			{
				//batched query
				bool isDesc = this.DataBatching.IsOrderDesc;
				string batchFuncName = string.Format(CultureInfo.InvariantCulture, "batch{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				StringCollection funcCode = new StringCollection();
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "function {0}($orderKeyValue)\r\n{{\r\n", batchFuncName));
				//
				if (this.DonotDownloadDataToWebPage)
				{
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
				}
				else
				{
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
				}
				funcCode.Add("\t$msql = new JsonSourceMySql();\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$msql->SetCredential($this->{0});\r\n", connName));
				funcCode.Add("\t$msql->SetDebug($this->DEBUG);\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
				//
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->DownloadAllUploads('{0}');\r\n", this.TableName));
				//
				funcCode.Add("\tif($orderKeyValue == null)\r\n\t{\r\n");
				//first-batch
				//key query
				StringBuilder sb = new StringBuilder();
				if (isDesc)
				{
					sb.Append("SELECT Min(f) FROM (SELECT (");
				}
				else
				{
					sb.Append("SELECT Max(f) FROM (SELECT (");
				}
				sb.Append(orderFld1);
				sb.Append(") as f FROM ");
				sb.Append(this.From);
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append(" WHERE ");
					sb.Append(this.Where);
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				sb.Append(" LIMIT ");
				sb.Append(this.DataBatching.BatchSize);
				sb.Append(") p");
				string sql = sb.ToString();
				//find out parameter sequence
				string[] sqlParams = EasyQuery.GetParameterNames(sql, _qry.NameDelimiterBegin, _qry.NameDelimiterEnd);
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0}\";\r\n", sql));
				//key parameters
				funcCode.Add("\t\t$paramsKey = array();\r\n");
				int pcount = 0;
				if (this.Parameters != null)
				{
					for (int i = 0; i < sqlParams.Length; i++)//index into non-unique sequence list
					{
						int k = this.Parameters.GetIndex(sqlParams[i]);//index into the parameters, unique parameters
						if (k < 0)
						{
							throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", sqlParams[i]);
						}
						DbCommandParam dp = this.Parameters[k];
						funcCode.Add("$p = new SqlClientParameter();\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$p->name = '{0}';\r\n", dp.Name));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$p->value = {0};\r\n", parameters[k]));
						funcCode.Add("$paramsKey[] = $p;\r\n//\r\n");
						pcount++;
					}
				}
				//data query
				sb = new StringBuilder();
				sb.Append(this.QueryDef.SELECT);
				sb.Append(" FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append("(");
					sb.Append(this.Where);
					sb.Append(") AND ");
				}
				sb.Append("(");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") >= ?");
				}
				else
				{
					sb.Append(") <= ?");
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				sql = sb.ToString();
				sqlParams = EasyQuery.GetParameterNames(sql, _qry.NameDelimiterBegin, _qry.NameDelimiterEnd);
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} \";\r\n", sql));
				//data parameters
				funcCode.Add("\t\t$paramsData = array();\r\n");
				if (this.Parameters != null)
				{
					for (int i = 0; i < sqlParams.Length; i++)//index into non-unique sequence list
					{
						int k = this.Parameters.GetIndex(sqlParams[i]);//index into the parameters, unique parameters
						if (k < 0)
						{
							throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", sqlParams[i]);
						}
						DbCommandParam dp = this.Parameters[k];
						funcCode.Add("$p = new SqlClientParameter();\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$p->name = '{0}';\r\n", dp.Name));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$p->value = {0};\r\n", parameters[k]));
						funcCode.Add("$paramsData[] = $p;\r\n//\r\n");
						pcount++;
					}
				}
				//next key parameter
				funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->name = 'p{0}';\r\n", pcount));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				//get data
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
				addPhpReturn(funcCode, returnReceiver);
				//
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',rand());\r\n", this.TableName));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchIsFirst_{0}',1);\r\n", this.TableName));

				//finish first-batch
				funcCode.Add("\t}\r\n\telse\r\n\t{\r\n");
				//non-first batch ==================================================================
				//key query
				sb = new StringBuilder();
				if (isDesc)
				{
					sb.Append("SELECT Min(f) FROM (SELECT (");
				}
				else
				{
					sb.Append("SELECT Max(f) FROM (SELECT (");
				}
				sb.Append(orderFld1);
				sb.Append(") as f FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append("(");
					sb.Append(this.Where);
					sb.Append(") AND ");
				}
				sb.Append("(");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") < ?");
				}
				else
				{
					sb.Append(") > ?");
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				sb.Append(" LIMIT ");
				sb.Append(this.DataBatching.BatchSize);
				sb.Append(") p");
				sql = sb.ToString();
				sqlParams = EasyQuery.GetParameterNames(sql, _qry.NameDelimiterBegin, _qry.NameDelimiterEnd);
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0}\";\r\n", sql));
				//key parameters
				funcCode.Add("\t\t$paramsKey = array();\r\n");
				pcount = 0;
				if (this.Parameters != null)
				{
					for (int i = 0; i < sqlParams.Length; i++)//index into non-unique sequence list
					{
						int k = this.Parameters.GetIndex(sqlParams[i]);//index into the parameters, unique parameters
						if (k < 0)
						{
							throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", sqlParams[i]);
						}
						DbCommandParam dp = this.Parameters[k];
						funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->name = '{0}';\r\n", dp.Name));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->value = {0};\r\n", parameters[k]));
						funcCode.Add("\t\t$paramsKey[] = $p;\r\n//\r\n");
						pcount++;
					}
				}
				funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->name = 'p{0}';\r\n", pcount));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
				funcCode.Add("\t\t$paramsKey[]=$p;\r\n");
				//data query
				sb = new StringBuilder();
				sb.Append(this.QueryDef.SELECT);
				sb.Append(" FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append("(");
					sb.Append(this.Where);
					sb.Append(") AND ");
				}
				sb.Append("(");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") < ?");
				}
				else
				{
					sb.Append(") > ?");
				}
				sb.Append(" AND (");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") >= ?");
				}
				else
				{
					sb.Append(") <= ?");
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				sql = sb.ToString();
				sqlParams = EasyQuery.GetParameterNames(sql, _qry.NameDelimiterBegin, _qry.NameDelimiterEnd);
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} \";\r\n", sql));
				//data parameters
				funcCode.Add("\t\t$paramsData = array();\r\n");
				//
				pcount = 0;
				if (this.Parameters != null)
				{
					for (int i = 0; i < sqlParams.Length; i++)//index into non-unique sequence list
					{
						int k = this.Parameters.GetIndex(sqlParams[i]);//index into the parameters, unique parameters
						if (k < 0)
						{
							throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", sqlParams[i]);
						}
						DbCommandParam dp = this.Parameters[k];
						funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->name = '{0}';\r\n", dp.Name));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->value = {0};\r\n", parameters[k]));
						funcCode.Add("\t\t$paramsData[] = $p;\r\n//\r\n");
						pcount++;
					}
				}
				//
				funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->name = 'p{0}';\r\n", pcount));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
				funcCode.Add("\t\t$paramsData[]=$p;\r\n");
				//
				//next key parameter
				pcount++;
				funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->name = 'p{0}';\r\n", pcount));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				//get data
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
				addPhpReturn(funcCode, returnReceiver);
				//
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',$this->jsonFromClient->values->batchStreamId);\r\n", this.TableName));
				//finish non-first
				funcCode.Add("\t}\r\n");
				//streaming status
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchKey_{0}',$maxV);\r\n", this.TableName));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchFunction_{0}','{1}');\r\n", this.TableName, batchFuncName));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchObjName_{0}','{1}');\r\n", this.TableName, this.Site.Name));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->SetServerComponentName('{0}');\r\n", this.Site.Name));
				//finish function
				funcCode.Add("}\r\n");
				_webServerCompiler.AppendServerPagePhpCode(funcCode);
				//
				code.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}(null);\r\n", batchFuncName));
				//
				StringCollection execCode = new StringCollection();
				execCode.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}($value);\r\n", batchFuncName));
				_webServerCompiler.AppendServerExecutePhpCode(execCode);
			}
		}
		/// <summary>
		/// parameters[0] = WHERE
		/// parameters[1, 2, ...] = values for _dynamicParameters
		/// 
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="code"></param>
		/// <param name="parameters"></param>
		/// <param name="returnReceiver"></param>
		/// <param name="connName"></param>
		private void phpQueryWithWhereDynamic(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			string where = parameters[0];
			//parameters should have the same number of elements as this._dynamicParameters
			int pnCount = 0;
			if (_dynamicParameters != null)
			{
				pnCount = _dynamicParameters.Count;
			}
			if (parameters.Count - 1 != pnCount)
			{
				throw new ExceptionLimnorDatabase("Error compiling QueryWithWhereDynamic. Counts of parameter values and parameter definitions do not match. Value count:{0}. Definition count:{1}", parameters.Count - 1, pnCount);
			}
			try
			{
				QueryDef.PrepareQuery();
				string orderFld1 = getFirtsOrderFieldForBatchFetch();
				if (string.IsNullOrEmpty(orderFld1))
				{
					code.Add("$allParams = array();\r\n");
					//
					StringCollection ps = new StringCollection();
					for (int i = 1; i < parameters.Count; i++)
					{
						code.Add("$param = new SqlClientParameter();\r\n");
						code.Add("$param->name = '");
						code.Add(_dynamicParameters[i - 1].Name);
						code.Add("';\r\n");
						code.Add("$param->type = '");
						code.Add(ValueConvertor.OleDbTypeToPhpMySqlType(_dynamicParameters[i - 1].Type));
						code.Add("';\r\n");
						code.Add("$param->value=");
						code.Add(parameters[i]);
						code.Add(";\r\n");
						code.Add("$allParams[] = $param;\r\n");
						code.Add("if($this->DEBUG)\r\n");
						code.Add("{\r\n");
						code.Add("  echo 'param"); code.Add((i - 1).ToString(CultureInfo.InvariantCulture)); code.Add(":'.");
						code.Add("$param->name.',type:'.$param->type.',value:'.$param->value");
						code.Add(".'<br>';\r\n");
						code.Add("}\r\n");
					}
					code.Add("$where=");
					if (string.IsNullOrEmpty(where))
					{
						code.Add("'';\r\n");
					}
					else
					{
						if (where.StartsWith("'", StringComparison.Ordinal))
						{
							code.Add("'");
							code.Add(where.Replace("'", "\\'"));
							code.Add("';\r\n");
						}
						else
						{
							code.Add("trim(");
							code.Add(where);
							code.Add(");\r\n");
						}
					}
					code.Add("if($this->DEBUG)\r\n");
					code.Add("{\r\n");
					code.Add("  echo 'before process<br>';\r\n");
					code.Add("  echo 'where:'.$where.'<br>';\r\n");
					code.Add("  echo 'where.length:'.strlen($where).'<br>';\r\n");
					code.Add("  echo 'defined parameters:'.count($allParams).'<br>';\r\n");
					code.Add("}\r\n");
					//
					code.Add("$paramParser = new SqlScriptProcess();\r\n");
					code.Add("$ps = $paramParser->createSqlParameters($where,$allParams);\r\n");
					code.Add("$where = $paramParser->script;\r\n");
					code.Add("if($this->DEBUG)\r\n");
					code.Add("{\r\n");
					code.Add("  echo 'after process<br>';\r\n");
					code.Add("  echo 'where:'.$where.'<br>';\r\n");
					code.Add("  echo 'where.length:'.strlen($where).'<br>';\r\n");
					code.Add("  echo 'used parameters:'. count($ps). '<br>';\r\n");
					code.Add("}\r\n");
					//
					//this.Parameters are unique
					code.Add("$msql = new JsonSourceMySql();\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->SetCredential($this->{0});\r\n", connName));
					code.Add("$msql->SetDebug($this->DEBUG);\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
					//prepare query sql, generate $sql, $tbl, $ps ======================================================
					string sql;
					StringBuilder sbSql = new StringBuilder(QueryDef.SELECT);
					sbSql.Append(" FROM ");
					sbSql.Append(QueryDef.From);
					//
					StringBuilder sbSql2 = new StringBuilder();
					if (!string.IsNullOrEmpty(QueryDef.GroupBy))
					{
						sbSql2.Append(" GROUP BY ");
						sbSql2.Append(QueryDef.GroupBy);
					}
					if (!string.IsNullOrEmpty(QueryDef.Having))
					{
						sbSql2.Append(" HAVING ");
						sbSql2.Append(QueryDef.Having);
					}
					if (!string.IsNullOrEmpty(QueryDef.OrderBy))
					{
						sbSql2.Append(" ORDER BY ");
						sbSql2.Append(QueryDef.OrderBy);
					}
					string s = sbSql2.ToString();
					if (!string.IsNullOrEmpty(where))
					{
						code.Add("if (strlen($where) > 1)\r\n");
						code.Add("{\r\n");
						if (string.IsNullOrEmpty(s))
						{
							sql = string.Format(CultureInfo.InvariantCulture, "'{0} WHERE '. $where", sbSql.ToString().Replace("'", "\\'"));
						}
						else
						{
							sql = string.Format(CultureInfo.InvariantCulture, "'{0} WHERE '. $where. '{1}'", sbSql.ToString().Replace("'", "\\'"), s.Replace("'", "\\'"));
						}
						code.Add(string.Format(CultureInfo.InvariantCulture, "\t$sql = {0};\r\n", sql));
						code.Add("}\r\nelse\r\n{\r\n");
						if (string.IsNullOrEmpty(s))
						{
							sql = string.Format(CultureInfo.InvariantCulture, "'{0}'", sbSql.ToString().Replace("'", "\\'"));
						}
						else
						{
							sql = string.Format(CultureInfo.InvariantCulture, "'{0} {1}'", sbSql.ToString().Replace("'", "\\'"), s.Replace("'", "\\'"));
						}
						code.Add(string.Format(CultureInfo.InvariantCulture, "\t$sql = {0};\r\n", sql));
						code.Add("}\r\n");
					}
					else
					{
						if (string.IsNullOrEmpty(s))
						{
							sql = string.Format(CultureInfo.InvariantCulture, "'{0}'", sbSql.ToString().Replace("'", "\\'"));
						}
						else
						{
							sql = string.Format(CultureInfo.InvariantCulture, "'{0} {1}'", sbSql.ToString().Replace("'", "\\'"), s.Replace("'", "\\'"));
						}
						code.Add(string.Format(CultureInfo.InvariantCulture, "$sql = {0};\r\n", sql));
					}
					if (this.DonotDownloadDataToWebPage)
					{
						code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
					}
					else
					{
						code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
					}
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
					addPhpReturn(code, returnReceiver);
				}
				else
				{
					//data streaming ================================================================================
					bool isDesc = this.DataBatching.IsOrderDesc;
					string batchFuncName = string.Format(CultureInfo.InvariantCulture, "batch{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					StringCollection funcCode = new StringCollection();
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "function {0}($orderKeyValue)\r\n{{\r\n", batchFuncName));
					//
					if (this.DonotDownloadDataToWebPage)
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
					}
					else
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
					}
					funcCode.Add("\t$msql = new JsonSourceMySql();\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$msql->SetCredential($this->{0});\r\n", connName));
					funcCode.Add("\t$msql->SetDebug($this->DEBUG);\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->DownloadAllUploads('{0}');\r\n", this.TableName));
					//
					funcCode.Add("\tif($orderKeyValue == null)\r\n\t{\r\n");
					//first-batch =====================================================================
					//parese where clause and get parameters
					funcCode.Add("\t\t$allParams = array();\r\n");
					//
					StringCollection ps = new StringCollection();
					for (int i = 1; i < parameters.Count; i++)
					{
						funcCode.Add("\t\t$param = new SqlClientParameter();\r\n");
						funcCode.Add("\t\t$param->name = '");
						funcCode.Add(_dynamicParameters[i - 1].Name);
						funcCode.Add("';\r\n");
						funcCode.Add("\t\t$param->type = '");
						funcCode.Add(ValueConvertor.OleDbTypeToPhpMySqlType(_dynamicParameters[i - 1].Type));
						funcCode.Add("';\r\n");
						funcCode.Add("\t\t$param->value=");
						funcCode.Add(parameters[i]);
						funcCode.Add(";\r\n");
						funcCode.Add("\t\t$allParams[] = $param;\r\n");
						funcCode.Add("\t\tif($this->DEBUG)\r\n");
						funcCode.Add("\t\t{\r\n");
						funcCode.Add("\t\t\techo 'param");
						funcCode.Add((i - 1).ToString(CultureInfo.InvariantCulture));
						funcCode.Add(":'.");
						funcCode.Add("$param->name.',type:'.$param->type.',value:'.$param->value");
						funcCode.Add(".'<br>';\r\n");
						funcCode.Add("\t\t}\r\n");
					}
					funcCode.Add("\t\t$where=");
					if (string.IsNullOrEmpty(where))
					{
						funcCode.Add("'';\r\n");
					}
					else
					{
						if (where.StartsWith("'", StringComparison.Ordinal))
						{
							funcCode.Add("'");
							funcCode.Add(where.Replace("'", "\\'"));
							funcCode.Add("';\r\n");
						}
						else
						{
							funcCode.Add("trim(");
							funcCode.Add(where);
							funcCode.Add(");\r\n");
						}
					}
					funcCode.Add("\t\tif($this->DEBUG)\r\n");
					funcCode.Add("\t\t{\r\n");
					funcCode.Add("\t\t\techo 'before process<br>';\r\n");
					funcCode.Add("\t\t\techo 'where:'.$where.'<br>';\r\n");
					funcCode.Add("\t\t\techo 'where.length:'.strlen($where).'<br>';\r\n");
					funcCode.Add("\t\t\techo 'defined parameters:'.count($allParams).'<br>';\r\n");
					funcCode.Add("\t\t}\r\n");
					//
					funcCode.Add("\t\t$paramParser = new SqlScriptProcess();\r\n");
					funcCode.Add("\t\t$ps = $paramParser->createSqlParameters($where,$allParams);\r\n");
					funcCode.Add("\t\t$where = $paramParser->script;\r\n");
					funcCode.Add("\t\tif($this->DEBUG)\r\n");
					funcCode.Add("\t\t{\r\n");
					funcCode.Add("\t\t\techo 'after process<br>';\r\n");
					funcCode.Add("\t\t\techo 'where:'.$where.'<br>';\r\n");
					funcCode.Add("\t\t\techo 'where.length:'.strlen($where).'<br>';\r\n");
					funcCode.Add("\t\t\techo 'used parameters:'. count($ps). '<br>';\r\n");
					funcCode.Add("\t\t}\r\n");
					//pass where and parameters to client for next batch
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchWhere_{0}',$where);\r\n", this.TableName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchWhereParams_{0}',$ps);\r\n", this.TableName));
					//
					//key query
					StringBuilder sb = new StringBuilder();
					StringBuilder sb2 = new StringBuilder();
					if (isDesc)
					{
						sb.Append("SELECT Min(f) FROM (SELECT (");
					}
					else
					{
						sb.Append("SELECT Max(f) FROM (SELECT (");
					}
					sb.Append(orderFld1);
					sb.Append(") as f FROM ");
					sb.Append(this.From);
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb2.Append(" GROUP BY ");
						sb2.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb2.Append(" HAVING ");
						sb2.Append(this.Having);
					}
					sb2.Append(" ORDER BY ");
					sb2.Append(this.OrderBy);
					sb2.Append(" LIMIT ");
					sb2.Append(this.DataBatching.BatchSize);
					sb2.Append(") p");
					if (string.IsNullOrEmpty(where))
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					else
					{
						funcCode.Add("\t\tif(strlen($where) == 0)\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlKey = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
						funcCode.Add("\t\telse\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlKey = \"{0} WHERE $where {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					//data query
					sb = new StringBuilder();
					sb.Append(this.QueryDef.SELECT);
					sb.Append(" FROM ");
					sb.Append(this.From);
					sb.Append(" WHERE ");
					sb2 = new StringBuilder();
					sb2.Append("(");
					sb2.Append(orderFld1);
					if (isDesc)
					{
						sb2.Append(") >= ?");
					}
					else
					{
						sb2.Append(") <= ?");
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb2.Append(" GROUP BY ");
						sb2.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb2.Append(" HAVING ");
						sb2.Append(this.Having);
					}
					sb2.Append(" ORDER BY ");
					sb2.Append(this.OrderBy);
					if (string.IsNullOrEmpty(where))
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					else
					{
						funcCode.Add("\t\tif(strlen($where) == 0)\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlData = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
						funcCode.Add("\t\telse\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlData = \"{0} ($where) AND {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					//next key parameter
					funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$nextKeyParam->name = 'p'.count($ps);\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					//get data
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$ps,$sqlData,$ps,$nextKeyParam);\r\n", this.TableVariableName));
					addPhpReturn(funcCode, returnReceiver);
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',rand());\r\n", this.TableName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchIsFirst_{0}',1);\r\n", this.TableName));
					//
					//finish first-batch
					funcCode.Add("\t}\r\n\telse\r\n\t{\r\n");
					//non-first batch =========================================================================================
					//key query
					funcCode.Add("\t\t$where=$this->jsonFromClient->values->batchWhere;\r\n");
					funcCode.Add("\t\t$ps=$this->jsonFromClient->values->batchparameters;\r\n");
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchWhere_{0}',$where);\r\n", this.TableName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchWhereParams_{0}',$ps);\r\n", this.TableName));
					//
					funcCode.Add("\t\t$paramsKey=$ps;\r\n");
					funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$p->name = 'p'.count($ps);\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
					funcCode.Add("\t\t$paramsKey[]=$p;\r\n");
					sb = new StringBuilder();
					if (isDesc)
					{
						sb.Append("SELECT Min(f) FROM (SELECT (");
					}
					else
					{
						sb.Append("SELECT Max(f) FROM (SELECT (");
					}
					sb.Append(orderFld1);
					sb.Append(") as f FROM ");
					sb.Append(this.From);
					sb.Append(" WHERE ");
					sb2 = new StringBuilder();
					sb2.Append("(");
					sb2.Append(orderFld1);
					if (isDesc)
					{
						sb2.Append(") < ?");
					}
					else
					{
						sb2.Append(") > ?");
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb2.Append(" GROUP BY ");
						sb2.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb2.Append(" HAVING ");
						sb2.Append(this.Having);
					}
					sb2.Append(" ORDER BY ");
					sb2.Append(this.OrderBy);
					sb2.Append(" LIMIT ");
					sb2.Append(this.DataBatching.BatchSize);
					sb2.Append(") p");
					if (string.IsNullOrEmpty(where))
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					else
					{
						funcCode.Add("\t\tif(strlen($where)==0)\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlKey = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
						funcCode.Add("\t\telse\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlKey = \"{0} ($where) AND {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					//data query
					sb = new StringBuilder();
					sb.Append(this.QueryDef.SELECT);
					sb.Append(" FROM ");
					sb.Append(this.From);
					sb.Append(" WHERE ");
					sb2 = new StringBuilder();
					sb2.Append("(");
					sb2.Append(orderFld1);
					if (isDesc)
					{
						sb2.Append(") < ?");
					}
					else
					{
						sb2.Append(") > ?");
					}
					sb2.Append(" AND (");
					sb2.Append(orderFld1);
					if (isDesc)
					{
						sb2.Append(") >= ?");
					}
					else
					{
						sb2.Append(") <= ?");
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb2.Append(" GROUP BY ");
						sb2.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb2.Append(" HAVING ");
						sb2.Append(this.Having);
					}
					sb2.Append(" ORDER BY ");
					sb2.Append(this.OrderBy);
					if (string.IsNullOrEmpty(where))
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					else
					{
						funcCode.Add("\t\tif(strlen($where)==0)\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlData = \"{0} {1}\";\r\n", sb.ToString(), sb2.ToString()));
						funcCode.Add("\t\telse\r\n");
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t\t$sqlData = \"{0} ($where) AND {1}\";\r\n", sb.ToString(), sb2.ToString()));
					}
					//data parameters
					funcCode.Add("\t\t$paramsData = $ps;\r\n");
					//
					funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$p->name = 'p'.count($ps);\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
					funcCode.Add("\t\t$paramsData[]=$p;\r\n");
					//
					//next key parameter
					//pcount++;
					funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$nextKeyParam->name = 'p.'.count($paramsData);\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					//get data
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
					addPhpReturn(funcCode, returnReceiver);
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',$this->jsonFromClient->values->batchStreamId);\r\n", this.TableName));

					//finish non-first
					funcCode.Add("\t}\r\n");
					//after getting data
					//pass back parameters
					if (parameters != null && parameters.Count > 0)
					{
						foreach (string pv in parameters)
						{
							if (!string.IsNullOrEmpty(pv))
							{
								if (pv.StartsWith("$this->jsonFromClient->values->", StringComparison.Ordinal))
								{
									string sn = pv.Substring("$this->jsonFromClient->values->".Length);
									funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchParameter_{0}_{1}',{2});\r\n", sn, this.TableName, pv));
								}
							}
						}
					}
					//streaming status
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchKey_{0}',$maxV);\r\n", this.TableName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchFunction_{0}','{1}');\r\n", this.TableName, batchFuncName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchObjName_{0}','{1}');\r\n", this.TableName, this.Site.Name));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->SetServerComponentName('{0}');\r\n", this.Site.Name));
					//finish function
					funcCode.Add("}\r\n");
					_webServerCompiler.AppendServerPagePhpCode(funcCode);
					//
					code.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}(null);\r\n", batchFuncName));
					//
					StringCollection execCode = new StringCollection();
					execCode.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}($value);\r\n", batchFuncName));
					_webServerCompiler.AppendServerExecutePhpCode(execCode);
				}
			}
			catch
			{
				throw;
			}
		}
		private void phpQueryWithWhere(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			string wNew = parameters[0];
			if (!string.IsNullOrEmpty(wNew))
			{
				if (wNew.StartsWith("'", StringComparison.Ordinal))
				{
					wNew = wNew.Substring(1);
					if (wNew.EndsWith("'", StringComparison.Ordinal))
					{
						if (wNew.Length == 1)
						{
							wNew = string.Empty;
						}
						else
						{
							wNew = wNew.Substring(0, wNew.Length - 1);
						}
					}
				}
				if (wNew.StartsWith("\"", StringComparison.Ordinal))
				{
					wNew = wNew.Substring(1);
					if (wNew.EndsWith("\"", StringComparison.Ordinal))
					{
						if (wNew.Length == 1)
						{
							wNew = string.Empty;
						}
						else
						{
							wNew = wNew.Substring(0, wNew.Length - 1);
						}
					}
				}
			}
			StringCollection ps = new StringCollection();
			for (int i = 1; i < parameters.Count; i++)
			{
				ps.Add(parameters[i]);
			}
			string w = QueryDef.Where;
			try
			{
				if (!string.IsNullOrEmpty(wNew) && !wNew.StartsWith("$this->", StringComparison.Ordinal))
				{
					QueryDef.Where = wNew;
				}
				QueryDef.PrepareQuery();
				phpQueryWithParameterValues(methodName, code, ps, returnReceiver, connName);
			}
			catch
			{
				throw;
			}
			finally
			{
				QueryDef.Where = w;
				QueryDef.PrepareQuery();
			}
		}
		private void phpQueryWithSQL(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			bool isExpress = false;
			string sSql = parameters[0];
			if (!string.IsNullOrEmpty(sSql))
			{
				if (sSql.StartsWith("'", StringComparison.Ordinal))
				{
					if (sSql.IndexOf("'.", 1) > 0)
					{
						isExpress = true;
					}
					else
					{
						sSql = sSql.Substring(1);
						if (sSql.EndsWith("'", StringComparison.Ordinal))
						{
							if (sSql.Length == 1)
							{
								sSql = string.Empty;
							}
							else
							{
								sSql = sSql.Substring(0, sSql.Length - 1);
							}
						}
					}
				}
				if (sSql.StartsWith("\"", StringComparison.Ordinal))
				{
					sSql = sSql.Substring(1);
					if (sSql.EndsWith("\"", StringComparison.Ordinal))
					{
						if (sSql.Length == 1)
						{
							sSql = string.Empty;
						}
						else
						{
							sSql = sSql.Substring(0, sSql.Length - 1);
						}
					}
				}
			}
			StringCollection ps = new StringCollection();
			for (int i = 1; i < parameters.Count; i++)
			{
				ps.Add(parameters[i]);
			}
			string s = QueryDef.SqlQuery;
			try
			{
				if (isExpress)
				{
					QueryDef.SetRawSQL(sSql);
				}
				else if (!string.IsNullOrEmpty(sSql) && !sSql.StartsWith("$this->", StringComparison.Ordinal))
				{
					QueryDef.ResetQuery(sSql);
				}
				else
				{
					QueryDef.PrepareQuery();
				}
				phpQueryWithParameterValues(methodName, code, ps, returnReceiver, connName);
			}
			catch
			{
				throw;
			}
			finally
			{
				QueryDef.ResetQuery(s);
			}
		}
		private void phpQueryWithWhereOld(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			string where = parameters[0];
			string wNew = where;
			if (!string.IsNullOrEmpty(wNew))
			{
				if (wNew.StartsWith("'", StringComparison.Ordinal))
				{
					wNew = wNew.Substring(1);
					if (wNew.EndsWith("'", StringComparison.Ordinal))
					{
						if (wNew.Length == 1)
						{
							wNew = string.Empty;
						}
						else
						{
							wNew = wNew.Substring(0, wNew.Length - 1);
						}
					}
				}
				if (wNew.StartsWith("\"", StringComparison.Ordinal))
				{
					wNew = wNew.Substring(1);
					if (wNew.EndsWith("\"", StringComparison.Ordinal))
					{
						if (wNew.Length == 1)
						{
							wNew = string.Empty;
						}
						else
						{
							wNew = wNew.Substring(0, wNew.Length - 1);
						}
					}
				}
			}
			StringCollection ps = new StringCollection();
			for (int i = 1; i < parameters.Count; i++)
			{
				ps.Add(parameters[i]);
			}
			parameters = ps;
			string w = QueryDef.Where;
			try
			{
				if (!string.IsNullOrEmpty(wNew) && !wNew.StartsWith("$this->", StringComparison.Ordinal))
				{
					QueryDef.Where = wNew;
				}
				QueryDef.PrepareQuery();
				string orderFld1 = getFirtsOrderFieldForBatchFetch();
				if (string.IsNullOrEmpty(orderFld1))
				{
					//parameters should have the same number of elements as this.Parameters
					//this.Parameters are unique
					code.Add("$msql = new JsonSourceMySql();\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->SetCredential($this->{0});\r\n", connName));
					code.Add("$msql->SetDebug($this->DEBUG);\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
					//generate $dpps and $dpsql ================================
					if (!string.IsNullOrEmpty(this.DataPreparer))
					{
						this.DataPreparer = this.DataPreparer.Trim();
					}
					string dataPreparer = this.DataPreparer;
					if (!string.IsNullOrEmpty(dataPreparer))
					{
						//assume data preparer will not use duplicated parameter names because it is a procedure call
						if (this.Parameters != null)
						{
							for (int i = 0; i < this.Parameters.Count; i++)
							{
								dataPreparer = dataPreparer.Replace(this.Parameters[i].Name, "?");
							}
						}
						code.Add(string.Format(CultureInfo.InvariantCulture, "$dpsql = \"{0} \";\r\n", dataPreparer));
						code.Add("$dpps = array();\r\n");
						if (this.Parameters != null && this.DataPreparerParameternames != null && this.DataPreparerParameternames.Length > 0)
						{
							for (int i = 0; i < this.DataPreparerParameternames.Length; i++)
							{
								int k = this.Parameters.GetIndex(this.DataPreparerParameternames[i]);
								if (k < 0)
								{
									throw new ExceptionLimnorDatabase("Data preparer Parameter [{0}] not found", this.DataPreparerParameternames[i]);
								}
								DbCommandParam dp = this.Parameters[k];
								code.Add("$dpp = new SqlClientParameter();\r\n");
								code.Add(string.Format(CultureInfo.InvariantCulture, "$dpp->name = '{0}';\r\n", dp.Name));
								code.Add(string.Format(CultureInfo.InvariantCulture, "$dpp->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
								code.Add(string.Format(CultureInfo.InvariantCulture, "$dpp->value = {0};\r\n", parameters[k]));
								code.Add("$dpps[] = $dpp;\r\n//\r\n");
							}
						}
					}
					string sql = this.SqlQuery;
					if (!string.IsNullOrEmpty(this.DataPreparer))
					{
						sql = sql.Substring(this.DataPreparer.Length).Trim();
					}
					//sqlParams contains parameter names orderred by the appearance in sql, may duplicate
					string[] sqlParams = EasyQuery.GetParameterNames(sql, _qry.NameDelimiterBegin, _qry.NameDelimiterEnd);
					if (this.Parameters != null)
					{
						for (int i = 0; i < this.Parameters.Count; i++)
						{
							sql = sql.Replace(this.Parameters[i].Name, "?");
						}
					}
					code.Add(string.Format(CultureInfo.InvariantCulture, "$sql = \"{0} \";\r\n", sql));
					if (this.DonotDownloadDataToWebPage)
					{
						code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
					}
					else
					{
						code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
					}
					code.Add("$ps = array();\r\n");
					if (this.Parameters != null)
					{
						for (int i = 0; i < sqlParams.Length; i++)
						{
							int k = this.Parameters.GetIndex(sqlParams[i]);
							if (k < 0)
							{
								throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", sqlParams[i]);
							}
							DbCommandParam dp = this.Parameters[k];
							code.Add("$p = new SqlClientParameter();\r\n");
							code.Add(string.Format(CultureInfo.InvariantCulture, "$p->name = '{0}';\r\n", dp.Name));
							code.Add(string.Format(CultureInfo.InvariantCulture, "$p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
							code.Add(string.Format(CultureInfo.InvariantCulture, "$p->value = {0};\r\n", parameters[k]));
							code.Add("$ps[] = $p;\r\n//\r\n");
						}
					}
					if (string.IsNullOrEmpty(this.DataPreparer))
					{
						code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
					}
					else
					{
						code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->QueryWithPreparer($dpsql,$dpps,${0},$sql,$ps);\r\n", this.TableVariableName));
					}
					addPhpReturn(code, returnReceiver);
				}
				else
				{
					//data streaming
					bool isDesc = this.DataBatching.IsOrderDesc;
					string batchFuncName = string.Format(CultureInfo.InvariantCulture, "batch{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					StringCollection funcCode = new StringCollection();
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "function {0}($orderKeyValue)\r\n{{\r\n", batchFuncName));
					//
					if (this.DonotDownloadDataToWebPage)
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
					}
					else
					{
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
					}
					funcCode.Add("\t$msql = new JsonSourceMySql();\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$msql->SetCredential($this->{0});\r\n", connName));
					funcCode.Add("\t$msql->SetDebug($this->DEBUG);\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->DownloadAllUploads('{0}');\r\n", this.TableName));
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$where = {0};\r\n", wNew));
					funcCode.Add("\tif($orderKeyValue == null)\r\n\t{\r\n");
					//first batch
					//key query
					StringBuilder sb = new StringBuilder();
					if (isDesc)
					{
						sb.Append("SELECT Min(f) FROM (SELECT (");
					}
					else
					{
						sb.Append("SELECT Max(f) FROM (SELECT (");
					}
					sb.Append(orderFld1);
					sb.Append(") as f FROM ");
					sb.Append(this.From);
					if (!string.IsNullOrEmpty(this.Where))
					{
						//sb.Append(" WHERE $where");
						sb.Append(" WHERE ");
						sb.Append(this.Where);
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb.Append(" GROUP BY ");
						sb.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb.Append(" HAVING ");
						sb.Append(this.Having);
					}
					sb.Append(" ORDER BY ");
					sb.Append(this.OrderBy);
					sb.Append(" LIMIT ");
					sb.Append(this.DataBatching.BatchSize);
					sb.Append(") p");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0}\";\r\n", sb.ToString()));
					//key parameters
					funcCode.Add("\t\t$paramsKey = array();\r\n");
					//data query
					sb = new StringBuilder();
					sb.Append(this.QueryDef.SELECT);
					sb.Append(" FROM ");
					sb.Append(this.From);
					sb.Append(" WHERE ");
					if (!string.IsNullOrEmpty(this.Where))
					{
						//sb.Append("($where) AND ");
						sb.Append("(");
						sb.Append(this.Where);
						sb.Append(") AND ");
					}
					sb.Append("(");
					sb.Append(orderFld1);
					if (isDesc)
					{
						sb.Append(") >= ?");
					}
					else
					{
						sb.Append(") <= ?");
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb.Append(" GROUP BY ");
						sb.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb.Append(" HAVING ");
						sb.Append(this.Having);
					}
					sb.Append(" ORDER BY ");
					sb.Append(this.OrderBy);
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} \";\r\n", sb.ToString()));
					//data key
					funcCode.Add("\t\t$paramsData = array();\r\n");
					//next key parameter
					funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$nextKeyParam->name = 'p0';\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					//get data
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
					addPhpReturn(funcCode, returnReceiver);
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',rand());\r\n", this.TableName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchIsFirst_{0}',1);\r\n", this.TableName));
					//
					funcCode.Add("\t}\r\n\telse\r\n\t{\r\n");
					//non-first batch ======================================================================
					//key query
					sb = new StringBuilder();
					if (isDesc)
					{
						sb.Append("SELECT Min(f) FROM (SELECT (");
					}
					else
					{
						sb.Append("SELECT Max(f) FROM (SELECT (");
					}
					sb.Append(orderFld1);
					sb.Append(") as f FROM ");
					sb.Append(this.From);
					sb.Append(" WHERE ");
					if (!string.IsNullOrEmpty(this.Where))
					{
						sb.Append("(");
						sb.Append(this.Where);
						sb.Append(") AND ");
					}
					sb.Append("(");
					sb.Append(orderFld1);
					if (isDesc)
					{
						sb.Append(") < ?");
					}
					else
					{
						sb.Append(") > ?");
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb.Append(" GROUP BY ");
						sb.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb.Append(" HAVING ");
						sb.Append(this.Having);
					}
					sb.Append(" ORDER BY ");
					sb.Append(this.OrderBy);
					sb.Append(" LIMIT ");
					sb.Append(this.DataBatching.BatchSize);
					sb.Append(") p");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0}\";\r\n", sb.ToString()));
					//key parameters
					funcCode.Add("\t\t$paramsKey = array();\r\n");
					funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$p->name = 'p0';\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
					funcCode.Add("\t\t$paramsKey[]=$p;\r\n");
					//data query
					sb = new StringBuilder();
					sb.Append(this.QueryDef.SELECT);
					sb.Append(" FROM ");
					sb.Append(this.From);
					sb.Append(" WHERE ");
					if (!string.IsNullOrEmpty(wNew))
					{
						sb.Append("($where) AND ");
					}
					sb.Append("(");
					sb.Append(orderFld1);
					if (isDesc)
					{
						sb.Append(") < ?");
					}
					else
					{
						sb.Append(") > ?");
					}
					sb.Append(" AND (");
					sb.Append(orderFld1);
					if (isDesc)
					{
						sb.Append(") >= ?");
					}
					else
					{
						sb.Append(") <= ?");
					}
					if (!string.IsNullOrEmpty(this.GroupBy))
					{
						sb.Append(" GROUP BY ");
						sb.Append(this.GroupBy);
					}
					if (!string.IsNullOrEmpty(this.Having))
					{
						sb.Append(" HAVING ");
						sb.Append(this.Having);
					}
					sb.Append(" ORDER BY ");
					sb.Append(this.OrderBy);
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} \";\r\n", sb.ToString()));
					//data parameters
					funcCode.Add("\t\t$paramsData = array();\r\n");
					funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$p->name = 'p0';\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
					funcCode.Add("\t\t$paramsData[]=$p;\r\n");
					//
					//next key parameter
					funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
					funcCode.Add("\t\t$nextKeyParam->name = 'p1';\r\n");
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
					//get data
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
					addPhpReturn(funcCode, returnReceiver);
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',$this->jsonFromClient->values->batchStreamId);\r\n", this.TableName));
					//
					funcCode.Add("\t}\r\n");
					//
					if (where.StartsWith("$this->jsonFromClient->values->", StringComparison.Ordinal))
					{
						string sn = where.Substring("$this->jsonFromClient->values->".Length);
						funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchParameter_{0}_{1}',{2});\r\n", sn, this.TableName, where));
					}
					//
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchKey_{0}',$maxV);\r\n", this.TableName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchFunction_{0}','{1}');\r\n", this.TableName, batchFuncName));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchObjName_{0}','{1}');\r\n", this.TableName, this.Site.Name));
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->SetServerComponentName('{0}');\r\n", this.Site.Name));
					//
					funcCode.Add("}\r\n");
					_webServerCompiler.AppendServerPagePhpCode(funcCode);
					//
					code.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}(null);\r\n", batchFuncName));
					//
					StringCollection execCode = new StringCollection();
					execCode.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}($value);\r\n", batchFuncName));
					_webServerCompiler.AppendServerExecutePhpCode(execCode);
				}
			}
			catch
			{
				throw;
			}
			finally
			{
				QueryDef.Where = w;
				QueryDef.PrepareQuery();
			}
		}
		private void phpQuery(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			string orderFld1 = getFirtsOrderFieldForBatchFetch();
			if (string.IsNullOrEmpty(orderFld1))
			{
				string sql = this.SqlQuery;
				if (!string.IsNullOrEmpty(this.DataPreparer))
				{
					this.DataPreparer = this.DataPreparer.Trim();
				}
				if (!string.IsNullOrEmpty(this.DataPreparer))
				{
					sql = sql.Substring(this.DataPreparer.Length).Trim();
				}
				//original unbatched query
				code.Add(string.Format(CultureInfo.InvariantCulture, "$sql = \"{0} \";\r\n", sql));
				if (this.DonotDownloadDataToWebPage)
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
				}
				else
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
				}
				code.Add("$ps = array();\r\n");
				code.Add("$msql = new JsonSourceMySql();\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->SetCredential($this->{0});\r\n", connName));
				code.Add("$msql->SetDebug($this->DEBUG);\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
				//
				//generate $dpps and $dpsql ================================
				string dataPreparer = this.DataPreparer;
				if (!string.IsNullOrEmpty(dataPreparer))
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "$dpsql = \"{0} \";\r\n", dataPreparer));
					code.Add("$dpps = array();\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->QueryWithPreparer($dpsql,$dpps,${0},$sql,$ps);\r\n", this.TableVariableName));
				}
				else
				{
					code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
				}
				addPhpReturn(code, returnReceiver);
			}
			else
			{
				//batched query
				bool isDesc = this.DataBatching.IsOrderDesc;
				string batchFuncName = string.Format(CultureInfo.InvariantCulture, "batch{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				StringCollection funcCode = new StringCollection();
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "function {0}($orderKeyValue)\r\n{{\r\n", batchFuncName));
				//
				if (this.DonotDownloadDataToWebPage)
				{
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = new JsonDataTable(); ${1}->TableName = '{0}';\r\n", this.TableName, this.TableVariableName));
				}
				else
				{
					funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
				}
				funcCode.Add("\t$msql = new JsonSourceMySql();\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$msql->SetCredential($this->{0});\r\n", connName));
				funcCode.Add("\t$msql->SetDebug($this->DEBUG);\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->dataFetcher = $this->{0};\r\n", this.Site.Name));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$msql->FireEventOnFetchData = $this->{0}->FireEventOnFetchData;\r\n", this.Site.Name));
				//
				funcCode.Add("\tif($orderKeyValue == null)\r\n\t{\r\n");
				//first batch
				//key query
				StringBuilder sb = new StringBuilder();
				if (isDesc)
				{
					sb.Append("SELECT Min(f) FROM (SELECT (");
				}
				else
				{
					sb.Append("SELECT Max(f) FROM (SELECT (");
				}
				sb.Append(orderFld1);
				sb.Append(") as f FROM ");
				sb.Append(this.From);
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append(" WHERE ");
					sb.Append(this.Where);
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				sb.Append(" LIMIT ");
				sb.Append(this.DataBatching.BatchSize);
				sb.Append(") p");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0}\";\r\n", sb.ToString()));
				//key parameters
				funcCode.Add("\t\t$paramsKey = array();\r\n");
				//data query
				sb = new StringBuilder();
				sb.Append(this.QueryDef.SELECT);
				sb.Append(" FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append("(");
					sb.Append(this.Where);
					sb.Append(") AND ");
				}
				sb.Append("(");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") >= ?");
				}
				else
				{
					sb.Append(") <= ?");
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} \";\r\n", sb.ToString()));
				//data key
				funcCode.Add("\t\t$paramsData = array();\r\n");
				//next key parameter
				funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
				funcCode.Add("\t\t$nextKeyParam->name = 'p0';\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				//get data
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
				addPhpReturn(funcCode, returnReceiver);
				//
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',rand());\r\n", this.TableName));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchIsFirst_{0}',1);\r\n", this.TableName));
				//
				funcCode.Add("\t}\r\n\telse\r\n\t{\r\n");
				//non-first batch
				//key query
				sb = new StringBuilder();
				if (isDesc)
				{
					sb.Append("SELECT Min(f) FROM (SELECT (");
				}
				else
				{
					sb.Append("SELECT Max(f) FROM (SELECT (");
				}
				sb.Append(orderFld1);
				sb.Append(") as f FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append("(");
					sb.Append(this.Where);
					sb.Append(") AND ");
				}
				sb.Append("(");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") < ?");
				}
				else
				{
					sb.Append(") > ?");
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				sb.Append(" LIMIT ");
				sb.Append(this.DataBatching.BatchSize);
				sb.Append(") p");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlKey = \"{0}\";\r\n", sb.ToString()));
				//key parameters
				funcCode.Add("\t\t$paramsKey = array();\r\n");
				funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
				funcCode.Add("\t\t$p->name = 'p0';\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
				funcCode.Add("\t\t$paramsKey[]=$p;\r\n");
				//data query
				sb = new StringBuilder();
				sb.Append(this.QueryDef.SELECT);
				sb.Append(" FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				if (!string.IsNullOrEmpty(this.Where))
				{
					sb.Append("(");
					sb.Append(this.Where);
					sb.Append(") AND ");
				}
				sb.Append("(");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") < ?");
				}
				else
				{
					sb.Append(") > ?");
				}
				sb.Append(" AND (");
				sb.Append(orderFld1);
				if (isDesc)
				{
					sb.Append(") >= ?");
				}
				else
				{
					sb.Append(") <= ?");
				}
				if (!string.IsNullOrEmpty(this.GroupBy))
				{
					sb.Append(" GROUP BY ");
					sb.Append(this.GroupBy);
				}
				if (!string.IsNullOrEmpty(this.Having))
				{
					sb.Append(" HAVING ");
					sb.Append(this.Having);
				}
				sb.Append(" ORDER BY ");
				sb.Append(this.OrderBy);
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$sqlData = \"{0} \";\r\n", sb.ToString()));
				//data parameters
				funcCode.Add("\t\t$paramsData = array();\r\n");
				funcCode.Add("\t\t$p = new SqlClientParameter();\r\n");
				funcCode.Add("\t\t$p->name = 'p0';\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				funcCode.Add("\t\t$p->value = $orderKeyValue;\r\n");
				funcCode.Add("\t\t$paramsData[]=$p;\r\n");
				//
				//next key parameter
				funcCode.Add("\t\t$nextKeyParam = new SqlClientParameter();\r\n");
				funcCode.Add("\t\t$nextKeyParam->name = 'p1';\r\n");
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$nextKeyParam->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(this.DataBatching.KeyFieldType)));
				//get data
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$maxV=$msql->GetDataByStreaming(${0},$sqlKey,$paramsKey,$sqlData,$paramsData,$nextKeyParam);\r\n", this.TableVariableName));
				addPhpReturn(funcCode, returnReceiver);
				//
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t\t$this->AddDownloadValue('batchSreamID_{0}',$this->jsonFromClient->values->batchStreamId);\r\n", this.TableName));
				//
				funcCode.Add("\t}\r\n");
				//
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchKey_{0}',$maxV);\r\n", this.TableName));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchFunction_{0}','{1}');\r\n", this.TableName, batchFuncName));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "\t$this->AddDownloadValue('batchObjName_{0}','{1}');\r\n", this.TableName, this.Site.Name));
				funcCode.Add(string.Format(CultureInfo.InvariantCulture, "$this->SetServerComponentName('{0}');\r\n", this.Site.Name));
				//
				funcCode.Add("}\r\n");
				_webServerCompiler.AppendServerPagePhpCode(funcCode);
				//
				code.Add(string.Format(CultureInfo.InvariantCulture, "$this->{0}(null);\r\n", batchFuncName));
				//
				StringCollection execCode = new StringCollection();
				execCode.Add(string.Format(CultureInfo.InvariantCulture, "if($method == '{0}') $this->{0}($value);\r\n", batchFuncName));
				_webServerCompiler.AppendServerExecutePhpCode(execCode);
			}
		}
		private void phpLoadFromCategoryQuery(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			//parameters[0] determines loading root or loading sub
			if (parameters != null && parameters.Count > 0)
			{
				string w = this.Where;
				code.Add(string.Format(CultureInfo.InvariantCulture, "if({0} == null) {{\r\n", parameters[0]));
				code.Add("if($this->DEBUG) {\r\n echo \"Load root categories for treeview <br>\";\r\n}\r\n");
				if (string.IsNullOrEmpty(w))
					this.Where = string.Format(CultureInfo.InvariantCulture, "  {0}{1}{2} is null", this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd);
				else
					this.Where = string.Format(CultureInfo.InvariantCulture, "  ({0}) AND ({1}{2}{3} is null)", w, this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd);
				string sql = this.SqlQuery;
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				code.Add(string.Format(CultureInfo.InvariantCulture, "  $sql = \"{0} \";\r\n", sql));
				code.Add(string.Format(CultureInfo.InvariantCulture, "  ${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
				code.Add("  $ps = array();\r\n");
				if (this.Parameters != null)
				{
					_qry.ValidateSqlParameters();
					for (int i = 0; i < this.SqlParameternames.Length; i++)
					{
						int k = this.Parameters.GetIndex(this.SqlParameternames[i]);
						if (k >= 0)
						{
							DbCommandParam dp = this.Parameters[k];
							code.Add("  $p = new SqlClientParameter();\r\n");
							code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->name = '{0}';\r\n", dp.Name));
							code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
							code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->value = {0};\r\n", parameters[k + 1]));
							code.Add("  $ps[] = $p;\r\n//\r\n");
						}
					}
				}
				code.Add("  $msql = new JsonSourceMySql();\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "  $msql->SetCredential($this->{0});\r\n", connName));
				code.Add("  $msql->SetDebug($this->DEBUG);\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "  $msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
				//
				code.Add("}\r\nelse {\r\n");
				//
				code.Add("if($this->DEBUG) {\r\n echo \"Load sub-categories for treeview using key:\". " + parameters[0] + ".\" <br>\";\r\n}\r\n");
				//
				string pn = string.Format(CultureInfo.InvariantCulture, "@p{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				if (string.IsNullOrEmpty(w))
					this.Where = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2} = {3}", this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd, pn);
				else
					this.Where = string.Format(CultureInfo.InvariantCulture, "({0}) AND ({1}{2}{3} = {4})", w, this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd, pn);
				sql = this.SqlQuery;
				if (this.Parameters != null)
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						sql = sql.Replace(this.Parameters[i].Name, "?");
					}
				}
				code.Add(string.Format(CultureInfo.InvariantCulture, "  $sql = \"{0} \";\r\n", sql));
				code.Add(string.Format(CultureInfo.InvariantCulture, "  ${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
				code.Add("  $ps = array();\r\n");
				if (this.Parameters != null)
				{
					//bool found = false;
					_qry.ValidateSqlParameters();
					for (int i = 0; i < this.SqlParameternames.Length; i++)
					{
						code.Add("  $p = new SqlClientParameter();\r\n");
						if (string.CompareOrdinal(this.SqlParameternames[i], pn) == 0)
						{
							code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->name = '{0}';\r\n", pn));
							if (PrimaryKey == null)
								PrimaryKey = new NameTypePair("P", typeof(string));
							else if (PrimaryKey.Type == null)
								PrimaryKey.Type = typeof(string);
							code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(EPField.ToOleDBType(PrimaryKey.Type))));
							code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->value = {0};\r\n", parameters[0]));
							code.Add("  $ps[] = $p;\r\n//\r\n");
							continue;
						}
						int k = this.Parameters.GetIndex(this.SqlParameternames[i]);
						if (k < 0)
						{
							throw new ExceptionLimnorDatabase("Query Parameter [{0}] not found", this.SqlParameternames[i]);
						}
						DbCommandParam dp = this.Parameters[k];
						code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->name = '{0}';\r\n", dp.Name));
						code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->type = '{0}';\r\n", ValueConvertor.OleDbTypeToPhpMySqlType(dp.Type)));
						//parentid inserted at 0, pushed this parameter 1 down
						code.Add(string.Format(CultureInfo.InvariantCulture, "  $p->value = {0};\r\n", parameters[k + 1]));
						code.Add("  $ps[] = $p;\r\n//\r\n");
					}
				}
				code.Add("  $msql = new JsonSourceMySql();\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "  $msql->SetCredential($this->{0});\r\n", connName));
				code.Add("  $msql->SetDebug($this->DEBUG);\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "  $msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
				//
				code.Add("}\r\n");
				this.Where = w;
			}
		}
		private void phpCreateNewRecord(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			string sql = QueryDef.GetCreateNewRecordSQL();
			if (!string.IsNullOrEmpty(sql))
			{
				code.Add("$msql = new JsonSourceMySql();\r\n");
				code.Add(string.Format(CultureInfo.InvariantCulture, "$msql->SetCredential($this->{0});\r\n", connName));
				code.Add("$msql->SetDebug($this->DEBUG);\r\n");
				string qrySql = QueryDef.GetCreateNewRecordQuerySQL();
				code.Add(string.Format(CultureInfo.InvariantCulture, "$sql = \"{0} \";\r\n", sql));
				code.Add("$ps = array();\r\n");
				DbParameterCollection dbps = _qry.GetCreateNewRecordParameters();
				if (dbps != null && dbps.Count > 0)
				{
					if (parameters == null || parameters.Count < dbps.Count)
					{
						throw new ExceptionLimnorDatabase("Parameter count mismatch. Parameter values:{0}; parameters in command: {1}", parameters == null ? 0 : parameters.Count, dbps.Count);
					}
					for (int i = 0; i < dbps.Count; i++)
					{
						DbParameter dp = dbps[i];
						code.Add("$p = new SqlClientParameter();\r\n");
						code.Add(string.Format(CultureInfo.InvariantCulture, "$p->name = '{0}';\r\n", dp.ParameterName));
						code.Add(string.Format(CultureInfo.InvariantCulture, "$p->type = '{0}';\r\n", ValueConvertor.DbTypeToPhpMySqlType(dp.DbType)));
						if ((string.IsNullOrEmpty(parameters[i]) || string.CompareOrdinal(parameters[i], "''") == 0) && ValueConvertor.IsDateTime(dp.DbType))
						{
							code.Add("$p->value = null;\r\n");
						}
						else
						{
							if ((dp.DbType == DbType.DateTime || dp.DbType == DbType.Date || dp.DbType == DbType.DateTime2) && (string.IsNullOrEmpty(parameters[i]) || string.CompareOrdinal(parameters[i], "\"\"") == 0))
							{
								code.Add("$p->value = '0000-00-00 00:00:00';\r\n");
							}
							else
							{
								code.Add(string.Format(CultureInfo.InvariantCulture, "$p->value = {0};\r\n", parameters[i]));
							}
						}
						code.Add("$ps[] = $p;\r\n//\r\n");
					}
				}
				if (_qry.UseIdentity())
				{
					code.Add("$autoNumber = $msql->CreateNewRecord($sql,$ps, true);\r\n");
					addPhpReturn(code, returnReceiver);
					code.Add("if($autoNumber !== false)\r\n{\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "\t$sql = \"{0} \";\r\n", qrySql));
					code.Add(string.Format(CultureInfo.InvariantCulture, "\t${1} = $this->AddDataTable('{0}');\r\n", this.TableName, this.TableVariableName));
					code.Add("\t$ps = array();\r\n");
					code.Add("\t$p = new SqlClientParameter();\r\n");
					code.Add("\t$p->name = 'anum';\r\n");
					code.Add("\t$p->type = 'i';\r\n");
					code.Add("\t$p->value = $autoNumber;\r\n");
					code.Add("\t$ps[] = $p;\r\n//\r\n");
					code.Add(string.Format(CultureInfo.InvariantCulture, "\t$msql->GetData(${0},$sql,$ps);\r\n", this.TableVariableName));
					code.Add("\t$script=\"_setObjectProperty('");
					code.Add(this.TableName);
					code.Add("','autoNumber',\".$autoNumber.\");\";\r\n");
					code.Add("\t$streaming = array();$streaming[] = '");
					code.Add(this.TableName);
					code.Add("';\r\n");
					code.Add("\t$this->AddDownloadValue('isdatastreaming', $streaming);\r\n");
					code.Add("\t$this->AddDownloadValue('addednewrecord', $streaming);\r\n");
					code.Add("\t$this->AddClientScript($script);\r\n");
					code.Add("}\r\n");
				}
				else
				{
					code.Add("$msql->CreateNewRecord($sql,$ps, true, '');\r\n");
					addPhpReturn(code, returnReceiver);
				}
				code.Add(string.Format(CultureInfo.InvariantCulture, "$this->SetServerComponentName('{0}');\r\n", this.Site.Name));
			}
		}
		private void phpUpdate(string methodName, StringCollection code, StringCollection parameters, string returnReceiver, string connName)
		{
			if (!string.IsNullOrEmpty(returnReceiver))
			{
				code.Add(returnReceiver);
				code.Add("=");
			}
			code.Add("$this->UpdateTable($this->");
			code.Add(this.Site.Name);
			code.Add(");\r\n");
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CreateActionPhpScript(string objectName, string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			string eventName = null;
			this.QueryDef.PrepareQuery();
			string connName = GetConnectionCodeName();
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
			{
				phpQueryWithParameterValues(methodName, code, parameters, returnReceiver, connName);
				eventName = "DataArrived";
			}
			else if (string.CompareOrdinal(methodName, "QueryWithWhereDynamic") == 0)
			{
				phpQueryWithWhereDynamic(methodName, code, parameters, returnReceiver, connName);
				eventName = "DataArrived";
			}
			else if (string.CompareOrdinal(methodName, "QueryWithWhere") == 0)
			{
				phpQueryWithWhere(methodName, code, parameters, returnReceiver, connName);
				eventName = "DataArrived";
			}
			else if (string.CompareOrdinal(methodName, "QueryWithSQL") == 0)
			{
				phpQueryWithSQL(methodName, code, parameters, returnReceiver, connName);
				eventName = "DataArrived";
			}
			else if (string.CompareOrdinal(methodName, "Query") == 0 || string.CompareOrdinal(methodName, "FetchData") == 0)
			{
				phpQuery(methodName, code, parameters, returnReceiver, connName);
				eventName = "DataArrived";
			}
			else if (string.CompareOrdinal(methodName, "LoadFromCategoryQuery") == 0)
			{
				phpLoadFromCategoryQuery(methodName, code, parameters, returnReceiver, connName);
			}
			else if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
			{
				phpCreateNewRecord(methodName, code, parameters, returnReceiver, connName);
			}
			else if (string.CompareOrdinal(methodName, "Update") == 0)
			{
				phpUpdate(methodName, code, parameters, returnReceiver, connName);
			}
			if (!string.IsNullOrEmpty(eventName))
			{
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public Dictionary<string, string> GetPhpFilenames()
		{
			Dictionary<string, string> files = new Dictionary<string, string>();
			return files;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void OnRequestStart(System.Web.UI.Page webPage)
		{
			_webPage = webPage as JsonWebServerProcessor;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CreateOnRequestStartPhp(StringCollection code)
		{
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CreateOnRequestClientDataPhp(StringCollection code)
		{
		}
		[NotForProgramming]
		[Browsable(false)]
		public void CreateOnRequestFinishPhp(StringCollection code)
		{
		}
		/// <summary>
		/// true:component name needs to be passed into PHP constructor 
		/// </summary>
		[NotForProgramming]
		public bool NeedObjectName { get { return false; } }


		[NotForProgramming]
		[Browsable(false)]
		public virtual bool OnBeforeSetSQL()
		{
			return true;
		}
		#endregion

		#region ISupportWebClientMethods Members
		[Browsable(false)]
		[NotForProgramming]
		public void CreateActionJavaScript(string codeName, string methodName, StringCollection sb, StringCollection parameters, string returnReceiver)
		{
			string ret = string.IsNullOrEmpty(returnReceiver) ? string.Empty : string.Format(CultureInfo.InvariantCulture, "{0}=", returnReceiver);
			if (string.CompareOrdinal(methodName, "AddNewRecord") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.addRow('{1}');\r\n", ret, this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "DeleteCurrentRecord") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.deleteCurrentRow('{1}');\r\n", ret, this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "Update") == 0)
			{
				string tblName = this.QueryDef.TableToUpdate;
				if (tblName != null)
				{
					tblName = tblName.Trim();
				}
				try
				{
					if (string.IsNullOrEmpty(tblName))
					{
						throw new ExceptionLimnorDatabase("Error using EasyDataSet {0}. Field list in SQL property does not include an autonumber or fields for a unique index. It cannot be used to modify the database. You need to add an autonumber field or fields for a unique index.", this.Site.Name);
					}
				}
				finally
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"JsonDataBinding.putData('{0}');\r\n", this.TableName));
				}
			}
			else if (string.CompareOrdinal(methodName, "MoveNext") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.dataMoveNext('{1}');\r\n", ret, this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "MovePrevious") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.dataMovePrevious('{1}');\r\n", ret, this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "MoveFirst") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.dataMoveFirst('{1}');\r\n", ret, this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "MoveLast") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.dataMoveLast('{1}');\r\n", ret, this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "MoveToRow") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						if (string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.dataMoveToRecord('{0}', {1});\r\n", this.TableName, parameters[0]));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{0} = JsonDataBinding.dataMoveToRecord('{1}', {2});\r\n", returnReceiver, this.TableName, parameters[0]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "GetModifiedRowCount") == 0)
			{
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0} = JsonDataBinding.getModifiedRowCount('{1}');\r\n", returnReceiver, this.TableName));
				}
			}
			else if (string.CompareOrdinal(methodName, "GetDeletedRowCount") == 0)
			{
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0} = JsonDataBinding.getDeletedRowCount('{1}');\r\n", returnReceiver, this.TableName));
				}
			}
			else if (string.CompareOrdinal(methodName, "GetNewRowCount") == 0)
			{
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0} = JsonDataBinding.getNewRowCount('{1}');\r\n", returnReceiver, this.TableName));
				}
			}
			else if (string.CompareOrdinal(methodName, "SetFieldValue") == 0)
			{
				if (parameters != null && parameters.Count > 1)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"JsonDataBinding.setColumnValue('{0}',{1},{2});\r\n", this.TableName, parameters[0], parameters[1]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "SetFieldValueEx") == 0)
			{
				if (parameters != null && parameters.Count > 2)
				{
					if (!string.IsNullOrEmpty(parameters[1]))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"JsonDataBinding.setColumnValue('{0}',{1},{2},{3});\r\n", this.TableName, parameters[1], parameters[2], parameters[0]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "GetFieldValue") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]) && !string.IsNullOrEmpty(returnReceiver))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"{0}=JsonDataBinding.getColumnValue('{1}',{2});\r\n", returnReceiver, this.TableName, parameters[0]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "GetFieldValueEx") == 0)
			{
				if (parameters != null && parameters.Count > 1)
				{
					if (!string.IsNullOrEmpty(parameters[1]) && !string.IsNullOrEmpty(returnReceiver))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"{0}=JsonDataBinding.getColumnValue('{1}',{2}, {3});\r\n", returnReceiver, this.TableName, parameters[1], parameters[0]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "GetColumnSum") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{0}=JsonDataBinding.columnSum('{1}',{2});\r\n", returnReceiver, this.TableName, parameters[0]));
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNull") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						if (!string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{2} = JsonDataBinding.isColumnValueNull('{0}',{1});\r\n", this.TableName, parameters[0], returnReceiver));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNull('{0}',{1});\r\n", this.TableName, parameters[0]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNullOrEmpty") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						if (!string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{2} = JsonDataBinding.isColumnValueNullOrEmpty('{0}',{1});\r\n", this.TableName, parameters[0], returnReceiver));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNullOrEmpty('{0}',{1});\r\n", this.TableName, parameters[0]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNotNull") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						if (!string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{2} = JsonDataBinding.isColumnValueNotNull('{0}',{1});\r\n", this.TableName, parameters[0], returnReceiver));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNotNull('{0}',{1});\r\n", this.TableName, parameters[0]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNotNullOrEmpty") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						if (!string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{2} = JsonDataBinding.isColumnValueNotNullOrEmpty('{0}',{1});\r\n", this.TableName, parameters[0], returnReceiver));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNotNullOrEmpty('{0}',{1});\r\n", this.TableName, parameters[0]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueChanged") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						if (!string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{2} = JsonDataBinding.isColumnValueChanged('{0}',{1});\r\n", this.TableName, parameters[0], returnReceiver));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueChanged('{0}',{1});\r\n", this.TableName, parameters[0]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueChangedEx") == 0)
			{
				if (parameters != null && parameters.Count > 1)
				{
					if (!string.IsNullOrEmpty(parameters[0]) && !string.IsNullOrEmpty(parameters[1]))
					{
						if (!string.IsNullOrEmpty(returnReceiver))
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"{3} = JsonDataBinding.isColumnValueChanged('{0}',{1},{2});\r\n", this.TableName, parameters[0], parameters[1], returnReceiver));
						}
						else
						{
							sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueChanged('{0}',{1}, {2});\r\n", this.TableName, parameters[0], parameters[1]));
						}
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "Search") == 0)
			{
				_qry.CreateActionJavaScript(methodName, sb, parameters, returnReceiver);
			}
			else if (string.CompareOrdinal(methodName, "CancelEdit") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.cancelDataEdit('{0}');\r\n", this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "RefetchRowsForCurrentMaster") == 0)
			{
				if (Master != null && MasterKeyColumns != null && MasterKeyColumns.Length > 0)
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.refetchDetailRows('{0}','{1}');\r\n", Master.TableName, this.TableName));
				}
			}
			else if (string.CompareOrdinal(methodName, "Clear") == 0)
			{
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.clearTableRows('{0}');\r\n", this.TableName));
			}
			else if (string.CompareOrdinal(methodName, "SwitchEventHandler") == 0)
			{
				string fn = WebPageCompilerUtility.StripSingleQuotes(parameters[1]);
				if (!string.IsNullOrEmpty(fn))
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"JsonDataBinding.switchExtendedEvent({0},'{1}',{2});\r\n", parameters[0], this.TableName, fn));
				}
			}
			else if (string.CompareOrdinal(methodName, "SortOnColumn") == 0)
			{
				if (parameters != null && parameters.Count > 1)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"JsonDataBinding.sortOnColumn('{0}',{1},{2},{3});\r\n", this.TableName, parameters[0], parameters[1], parameters[2]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "RemoveFromHTML5Storage") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"JsonDataBinding.removeEdsFromOffline({0});\r\n", parameters[0]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "LoadFromHTML5Storage") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"JsonDataBinding.loadEdsFromOffline('{0}',{1});\r\n", this.TableName, parameters[0]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "SaveToHTML5Storage") == 0)
			{
				if (parameters != null && parameters.Count > 0)
				{
					if (!string.IsNullOrEmpty(parameters[0]))
					{
						sb.Add(string.Format(CultureInfo.InvariantCulture,
							"JsonDataBinding.saveEdsToOffline('{0}',{1});\r\n", this.TableName, parameters[0]));
					}
				}
			}
			else if (string.CompareOrdinal(methodName, "PseudoQuery") == 0)
			{
				sb.Add("JsonDataBinding.values={};\r\n");
				string dataName = string.Format(CultureInfo.InvariantCulture,
					"data{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"var {0}={{}};\r\n", dataName));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.Tables=[];\r\n", dataName));
				string tblName = string.Format(CultureInfo.InvariantCulture,
					"tbl{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"var {0}={{}};\r\n", tblName));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.Tables.push({1});\r\n", dataName, tblName));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.TableName='{1}';\r\n", tblName, this.TableName));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.Rows=[];\r\n", tblName));
				sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.Columns=[];\r\n", tblName));
				FieldList fl = this.Fields;
				for (int i = 0; i < fl.Count; i++)
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.Columns.push({{}});\r\n", tblName));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.Columns[{1}].Name='{2}';\r\n", tblName, i, fl[i].Name));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.Columns[{1}].Type={2};\r\n", tblName, i, fl[i].GetMySqlPhpType()));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.Columns[{1}].ReadOnly={2};\r\n", tblName, i, (fl[i].ReadOnly ? "true" : "false")));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.Columns[{1}].isAutoNumber={2};\r\n", tblName, i, (fl[i].IsIdentity ? "true" : "false")));
				}
				//
				sb.Add(string.Format(CultureInfo.InvariantCulture,
						"JsonDataBinding.applyData({0});\r\n", dataName));
			}
		}

		#endregion

		#region IWebServerComponentCreator Members
		[NotForProgramming]
		[Browsable(false)]
		public void CreateServerComponentPhp(StringCollection objectDecl, StringCollection initCode, ServerScriptHolder scripts)
		{
			string decName = string.Format(CultureInfo.InvariantCulture,
				"private ${0};\r\n", this.Site.Name);
			if (!objectDecl.Contains(decName))
			{
				objectDecl.Add(decName);
			}
			string tblName = this.QueryDef.TableToUpdate;
			if (tblName != null)
			{
				tblName = tblName.Trim();
			}
			initCode.Add(string.Format(CultureInfo.InvariantCulture,
				"$this->{0} = new DataTableUpdator('{1}','{2}','{3}','{4}',$this->{5});\r\n", this.Site.Name, tblName, this.TableName, this.QueryDef.NameDelimiterBegin.Replace("'", "\\'"), this.QueryDef.NameDelimiterEnd.Replace("'", "\\'"), GetConnectionCodeName()));
			initCode.Add(string.Format(CultureInfo.InvariantCulture,
				"$this->{0}->FireEventOnFetchData = {1};\r\n", this.Site.Name, this.FireEventOnFetchData));
			if (_initCoe != null && _initCoe.Count > 0)
			{
				foreach (string s in _initCoe)
				{
					initCode.Add(s);
				}
			}
			scripts.OnRequestPutData.Add(string.Format(CultureInfo.InvariantCulture,
				"if($value == '{0}') $this->UpdateData($this->{1});", this.TableName, this.Site.Name));
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool RemoveFromComponentInitializer(string propertyName)
		{
			return false;
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool ExcludePropertyForPhp(string name)
		{
			return false;
		}
		#endregion

		#region IDataTableUpdator Members
		[NotForProgramming]
		[Browsable(false)]
		public string SourceTableName
		{
			get { return this.UpdatableTableName; }
		}
		[NotForProgramming]
		[Browsable(false)]
		public DbCommand CreateCommand()
		{
			return this.QueryDef.DatabaseConnection.ConnectionObject.CreateCommand();
		}
		[NotForProgramming]
		[Browsable(false)]
		public string NameDelimitBegin
		{
			get { return this.QueryDef.NameDelimiterBegin; }
		}
		[NotForProgramming]
		[Browsable(false)]
		public string NameDelimitEnd
		{
			get { return this.QueryDef.NameDelimiterEnd; }
		}
		[NotForProgramming]
		[Browsable(false)]
		public DbParameter AddParameter(DbCommand cmd, JsonDataColumn c, int columnIndex)
		{
			DbParameter p = cmd.CreateParameter();
			p.ParameterName = string.Format(CultureInfo.InvariantCulture, "@c{0}{1}", c.Name, columnIndex);
			p.DbType = (DbType)Enum.Parse(typeof(DbType), c.Type);
			cmd.Parameters.Add(p);
			return p;
		}
		[NotForProgramming]
		[Browsable(false)]
		public DbParameter AddFilterParameter(DbCommand cmd, JsonDataColumn c, int columnIndex)
		{
			DbParameter p = cmd.CreateParameter();
			p.ParameterName = string.Format(CultureInfo.InvariantCulture, "@f{0}{1}", c.Name, columnIndex);
			p.DbType = (DbType)Enum.Parse(typeof(DbType), c.Type);
			cmd.Parameters.Add(p);
			return p;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void Open()
		{
			this.QueryDef.DatabaseConnection.ConnectionObject.Open();
		}
		[NotForProgramming]
		[Browsable(false)]
		public void Close()
		{
			this.QueryDef.DatabaseConnection.ConnectionObject.Close();
		}
		[NotForProgramming]
		[Browsable(false)]
		public void SetSuccessMessage()
		{
			this.ProcessMessages.Clear();
			this.ProcessMessages.Add("Data updated");
		}

		public void SetErrorMessage(string message)
		{
			this.QueryDef.SetLastErrorMessage(message);
		}
		#endregion

		#region IDataSetSource Members

		[Browsable(false)]
		public StringCollection ProcessMessages
		{
			get
			{
				if (_qry == null)
				{
					StringCollection err = new StringCollection();
					err.Add("Query object not created");
					return err;
				}
				return _qry.ProcessMessages;
			}
		}
		[Browsable(false)]
		public string Messages
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				StringCollection s = ProcessMessages;
				for (int i = 0; i < s.Count; i++)
				{
					sb.Append(s[i]);
					sb.Append("<br>");
				}
				return sb.ToString();
			}
		}
		[Browsable(false)]
		public string[] GetReadOnlyFields()
		{
			List<string> l = new List<string>();
			if (Field_ReadOnly != null && Field_Name != null && Field_Name.Length == Field_ReadOnly.Length)
			{
				for (int i = 0; i < Field_ReadOnly.Length; i++)
				{
					if (Field_ReadOnly[i])
					{
						l.Add(Field_Name[i]);
					}
				}
			}
			else
			{
				if (Fields != null)
				{
					for (int i = 0; i < Fields.Count; i++)
					{
						if (Fields[i].ReadOnly)
						{
							l.Add(Fields[i].Name);
						}
					}
				}
			}
			return l.ToArray();
		}
		#endregion

		#region ICustomMethodCompiler Members
		[NotForProgramming]
		[Browsable(false)]
		public CodeExpression CompileMethod(string methodName, string varName, IMethodCompile methodToCompile, CodeStatementCollection statements, CodeExpressionCollection parameters)
		{
			if (VPLUtil.CompilerContext_ASPX)
			{
				if (string.CompareOrdinal(methodName, "Update") == 0)
				{
					//code to be generated: this.UpdateData(this.{EasyDataSet Name}, this.{EasyDataSet name}.TableName)
					CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression();
					cmie.Method = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "UpdateData");
					CodeFieldReferenceExpression eds = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), this.Name);
					cmie.Parameters.Add(eds);
					cmie.Parameters.Add(new CodePropertyReferenceExpression(eds, "TableName"));
					return cmie;
				}
			}
			return _qry.CompileMethod(methodName, varName, methodToCompile, statements, parameters);
		}

		#endregion

		#region IResetOnComponentChange Members
		[NotForProgramming]
		[Browsable(false)]
		public bool OnResetDesigner(string memberName)
		{
			if (string.IsNullOrEmpty(memberName) || string.CompareOrdinal(memberName, "SqlQuery") == 0)
			{
				return true;
			}
			return false;
		}

		#endregion

		#region IDevClassReferencer Members
		[NotForProgramming]
		[Browsable(false)]
		public void SetDevClass(IDevClass c)
		{
			_holder = c;
		}
		[NotForProgramming]
		[Browsable(false)]
		public IDevClass GetDevClass()
		{
			return _holder;
		}
		#endregion

		#region ICategoryDataSetSource Members
		[DefaultValue(null)]
		[WebServerMember]
		[NotForProgramming]
		[Browsable(false)]
		public NameTypePair PrimaryKey
		{
			get;
			set;
		}
		[DefaultValue(null)]
		[WebServerMember]
		[NotForProgramming]
		[Browsable(false)]
		public NameTypePair ForeignKey
		{
			get;
			set;
		}
		[NotForProgramming]
		[Browsable(false)]
		public NameTypePair[] GetParameters()
		{
			DbParameterList pl = this.Parameters;
			if (pl != null)
			{
				NameTypePair[] ret = new NameTypePair[pl.Count];
				for (int i = 0; i < pl.Count; i++)
				{
					ret[i] = new NameTypePair(pl[i].Name, EPField.ToSystemType(pl[i].Type));
				}
				return ret;
			}
			return new NameTypePair[] { };
		}
		[NotForProgramming]
		[Browsable(false)]
		public NameTypePair[] GetFields()
		{
			FieldList fd = Fields;
			if (fd != null)
			{
				NameTypePair[] ret = new NameTypePair[fd.Count];
				for (int i = 0; i < fd.Count; i++)
				{
					ret[i] = new NameTypePair(fd[i].Name, EPField.ToSystemType(fd[i].OleDbType));
				}
				return ret;
			}
			return new NameTypePair[] { };
		}
		[NotForProgramming]
		[Browsable(false)]
		public bool LoadFromCategoryQuery(params object[] values)
		{
			if (values != null && values.Length > 0)
			{
				object[] vs = new object[values.Length - 1];
				for (int i = 1; i < values.Length; i++)
				{
					vs[i - 1] = values[i];
				}
				if (values[0] == null)
				{
					return QueryRootCategories(vs);
				}
				else
				{
					return QuerySubCategories(values[0], vs);
				}
			}
			return false;
		}
		[WebServerMember]
		[NotForProgramming]
		[Browsable(false)]
		public bool QueryRootCategories(params object[] values)
		{
			string w = this.Where;
			if (string.IsNullOrEmpty(w))
				this.Where = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2} is null", this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd);
			else
				this.Where = string.Format(CultureInfo.InvariantCulture, "({0}) AND ({1}{2}{3} is null)", w, this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd);
			bool ret = false;
			try
			{
				ret = QueryWithParameterValues(values);
			}
			finally
			{
				this.Where = w;
			}
			return ret;
		}
		[WebServerMember]
		[NotForProgramming]
		[Browsable(false)]
		public bool QuerySubCategories(object foreignKeyValue, params object[] values)
		{
			string pn = string.Format(CultureInfo.InvariantCulture, "@p{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
			string w = this.Where;
			if (string.IsNullOrEmpty(w))
				this.Where = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2} = {4}", this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd, pn);
			else
				this.Where = string.Format(CultureInfo.InvariantCulture, "({0}) AND ({1}{2}{3} = {4})", w, this.DatabaseConnection.ConnectionObject.NameDelimiterBegin, this.ForeignKey, this.DatabaseConnection.ConnectionObject.NameDelimiterEnd, pn);
			bool ret = false;
			try
			{
				_qry.LogMessage("Parameter count before:{0}", _qry.Parameters.Count);
				_qry.PrepareQuery();
				_qry.LogMessage("Parameter count after:{0}", _qry.Parameters.Count);
				int n = 0;
				if (values != null)
				{
					n = values.Length;
				}
				object[] vs = new object[n + 1];
				bool found = false;
				DbParameterList pl = this.Parameters;
				for (int i = 0; i < pl.Count; i++)
				{
					if (!found)
					{
						if (string.CompareOrdinal(pl[i].Name, pn) == 0)
						{
							vs[i] = foreignKeyValue;
							found = true;
						}
						else
						{
							vs[i] = values[i];
						}
					}
					else
					{
						vs[i + 1] = values[i];
					}
				}
				ret = QueryWithParameterValues(vs);
			}
			finally
			{
				this.Where = w;
			}
			return ret;
		}

		#endregion

		#region IWebClientSupport Members
		[Browsable(false)]
		[NotForProgramming]
		public string GetJavaScriptWebMethodReferenceCode(string ownerCodeName, string methodName, StringCollection code, StringCollection parameters)
		{
			if (string.CompareOrdinal(methodName, "GetModifiedRowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getModifiedRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "GetDeletedRowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getDeletedRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "GetNewRowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getNewRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "GetColumnSum") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.columnSum('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "GetFieldValue") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.getColumnValue('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "GetFieldValueEx") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.getColumnValue('{0}',{1}, {2})", this.TableName, parameters[1], parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNull") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNull('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNullOrEmpty") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNullOrEmpty('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNotNull") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNotNull('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueNotNullOrEmpty") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueNotNullOrEmpty('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueChanged") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueChanged('{0}',{1})", this.TableName, parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "IsFieldValueChangedEx") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
								"JsonDataBinding.isColumnValueChanged('{0}',{1}, {2})", this.TableName, parameters[0], parameters[1]);
			}
			else if (string.CompareOrdinal(methodName, "MoveNext") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.dataMoveNext('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "MovePrevious") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.dataMovePrevious('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "MoveFirst") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.dataMoveFirst('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "MoveLast") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.dataMoveLast('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(methodName, "Search") == 0)
			{
				return _qry.GetJavaScriptWebMethodReferenceCode(this.Site.Name, methodName, code, parameters);
			}
			return null;
		}
		[NotForProgramming]
		[Browsable(false)]
		public string GetJavaScriptWebPropertyReferenceCode(StringCollection method, string propertyName, string[] parameters)
		{
			if (string.CompareOrdinal(propertyName, "ModifiedRowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getModifiedRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "DeletedRowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getDeletedRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "NewRowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getNewRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "RowCount") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getActiveRowCount('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "RowIndexLastModifiedCell") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getTableAttribute('{0}', 'LastEditRow')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "ColumnIndexLastModifiedCell") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getTableAttribute('{0}', 'LastEditColumn')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "IsDataReady") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getTableAttribute('{0}', 'IsDataReady')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "IsSearching") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getTableAttribute('{0}', 'IsSearching')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "Fields") == 0)
			{
				return this.TableName;
			}//
			else if (string.CompareOrdinal(propertyName, "LastInsertID") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getObjectProperty('{0}', 'autoNumber')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "RowIndex") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.getCurrentRowIndex('{0}')", this.TableName);
			}
			else if (string.CompareOrdinal(propertyName, "ErrorMessage") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.getTableAttribute('{0}', 'ErrorMessage')", this.TableName);
			}
			return null;
		}
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
			if (string.CompareOrdinal(methodName, "QueryWithSQL") == 0)
			{
				Dictionary<string, Attribute[]> pas = new Dictionary<string, Attribute[]>();
				pas.Add("sql", new Attribute[] { new RefreshPropertiesAttribute(RefreshProperties.All) });
				return pas;
			}
			return null;
		}

		#endregion

		#region ICustomTypeDescriptor Members
		[NotForProgramming]
		[Browsable(false)]
		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}
		[NotForProgramming]
		[Browsable(false)]
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			bool isWeb = true;
			if (_holder != null)
			{
				isWeb = _holder.IsWebPage;
			}
			List<PropertyDescriptor> lst = new List<PropertyDescriptor>();
			PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(this, attributes, true);
			if (typeof(EasyDataSet).Equals(this.GetType()))
			{
				bool nameUsed = false;
				foreach (PropertyDescriptor p in ps)
				{
					if (string.CompareOrdinal("Name", p.Name) == 0)
					{
						if (!p.IsReadOnly)
						{
							if (!nameUsed)
							{
								nameUsed = true;
								lst.Add(p);
							}
						}
					}
					else
					{
						if (!isWeb)
						{
							if (p.Attributes[typeof(WebClientOnlyAttribute)] != null)
							{
								continue;
							}
						}
						lst.Add(p);
					}
				}
			}
			else
			{
				staticInit();
				bool forBrowsing = VPLUtil.GetBrowseableProperties(attributes);
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
						if (string.CompareOrdinal("Name", p.Name) == 0)
						{
							if (!p.IsReadOnly)
							{
								lst.Add(p);
							}
						}
						else
						{
							if (!isWeb)
							{
								if (p.Attributes[typeof(WebClientOnlyAttribute)] != null)
								{
									continue;
								}
							}
							lst.Add(p);
						}
					}
				}
			}
			return new PropertyDescriptorCollection(lst.ToArray());
		}
		[NotForProgramming]
		[Browsable(false)]
		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}
		[NotForProgramming]
		[Browsable(false)]
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion

		#region IWebClientInitializer Members
		[NotForProgramming]
		[Browsable(false)]
		public void OnWebPageLoaded(StringCollection sc)
		{
			bool[] bs = this.IsFieldImage;
			if (bs != null && bs.Length > 0)
			{
				bool hasSetting = false;
				for (int i = 0; i < bs.Length; i++)
				{
					if (bs[i])
					{
						hasSetting = true;
						break;
					}
				}
				if (hasSetting)
				{
					StringBuilder sb = new StringBuilder("[");
					sb.Append(bs[0] ? "1" : "0");
					for (int i = 1; i < bs.Length; i++)
					{
						sb.Append(",");
						sb.Append(bs[i] ? "1" : "0");
					}
					sb.Append("]");
					sc.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.setObjectProperty('{0}','IsFieldImage',{1});\r\n", this.TableName, sb.ToString()));
				}
			}
			if (!this.ReadOnly)
			{
				bool[] brd = this.Field_ReadOnly;
				string[] snm = this.Field_Name;
				if (brd != null && brd.Length > 0 && snm != null && snm.Length == brd.Length)
				{
					bool bfirst = true;
					StringBuilder sb = new StringBuilder("[");
					for (int i = 0; i < brd.Length; i++)
					{
						if (brd[i] && snm[i] != null)
						{
							if (bfirst)
							{
								bfirst = false;
							}
							else
							{
								sb.Append(",");
							}
							sb.Append(string.Format(CultureInfo.InvariantCulture, "'{0}'", snm[i].ToLowerInvariant()));
						}
					}
					sb.Append("]");
					sc.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.setObjectProperty('{0}','readonlyfields',{1});\r\n", this.TableName, sb.ToString()));
				}
			}
			if (this.AutoConvertUTCforWeb)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.setTableAttribute('{0}','AutoConvertUTCforWeb',true);\r\n", this.TableName));
			}
			if (this.DataBatching != null && this.DataBatching.DataBatchingAllowed())
			{
				StringBuilder dbsc = new StringBuilder();
				dbsc.Append("JsonDataBinding.setTableAttribute('");
				dbsc.Append(this.TableName);
				dbsc.Append("','batchStatus',{batchSize:");
				dbsc.Append(this.DataBatching.BatchSize);
				int nDelay = this.getBatchTimeDelay();
				if (nDelay > 0)
				{
					dbsc.Append(",batchDelay:");
					dbsc.Append(nDelay);
				}
				dbsc.Append("});\r\n");
				sc.Add(dbsc.ToString());
			}
			if (Master != null && MasterKeyColumns != null && MasterKeyColumns.Length > 0)
			{
				string vname = string.Format(CultureInfo.InvariantCulture, "rel{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				sc.Add(string.Format(CultureInfo.InvariantCulture, "var {0} = {{}};\r\n", vname));
				sc.Add(string.Format(CultureInfo.InvariantCulture, "{0}.detailTableName = '{1}';\r\n", vname, this.TableName));
				sc.Add(string.Format(CultureInfo.InvariantCulture, "{0}.masterTableName  = '{1}';\r\n", vname, this.Master.TableName));
				sc.Add(string.Format(CultureInfo.InvariantCulture, "{0}.masterMethod = '{1}';\r\n", vname, _masterFunction));
				StringBuilder sbf = new StringBuilder("[");
				for (int i = 0; i < this.MasterKeyColumns.Length; i++)
				{
					if (i > 0)
					{
						sbf.Append(",");
					}
					sbf.Append("{name:'");
					sbf.Append(this.MasterKeyColumns[i]);
					sbf.Append("',code:'");
					sbf.Append(_keyfieldcode[this.MasterKeyColumns[i]]);
					sbf.Append("'}");
				}
				sbf.Append("]");
				sc.Add(string.Format(CultureInfo.InvariantCulture, "{0}.fields  = {1};\r\n", vname, sbf.ToString()));
				sc.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.addTableLink('{0}',{1});\r\n", this.Master.TableName, vname));
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public void OnWebPageLoadedAfterEventHandlerCreations(StringCollection sc)
		{

		}

		#endregion

		#region IEnumerable Members
		class DataEnumerator : IEnumerator
		{
			private EasyDataSet _ds;
			public DataEnumerator(EasyDataSet ds)
			{
				_ds = ds;
			}
			#region IEnumerator Members
			private bool _moved;
			public object Current
			{
				get
				{
					if (_moved)
						return _ds.CurrentDataRow;
					return null;
				}
			}
			public bool MoveNext()
			{
				if (_moved)
					return _ds.MoveNext();
				_moved = true;
				return _ds.MoveFirst();
			}
			public void Reset()
			{
				_moved = false;
			}
			#endregion
		}
		[NotForProgramming]
		[Browsable(false)]
		public IEnumerator GetEnumerator()
		{
			return new DataEnumerator(this);
		}

		#endregion

		#region IWebServerCompilerHolder Members
		[NotForProgramming]
		[Browsable(false)]
		public void SetWebServerCompiler(IWebServerCompiler compiler)
		{
			_webServerCompiler = compiler;
		}
		[NotForProgramming]
		[Browsable(false)]
		public void OnCreatedWebServerEventHandler(IWebServerCompiler compiler, string eventName, string handlerName)
		{
			_initCoe = new StringCollection();
			_initCoe.Add(string.Format(CultureInfo.InvariantCulture,
				"$this->{0}->owner = $this;\r\n", this.Site.Name));
			_initCoe.Add(string.Format(CultureInfo.InvariantCulture,
				"$this->{0}->funcName='{1}';\r\n", this.Site.Name, handlerName));
		}
		#endregion

		#region IWebClientPropertyValueHolder Members
		public string GetJavaScriptCode(string propertyName)
		{
			if (string.CompareOrdinal(propertyName, "DataSource") == 0)
			{
				return this.TableName;
			}
			else if (string.CompareOrdinal(propertyName, "ErrorMessage") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.getTableAttribute('{0}', 'ErrorMessage')", this.TableName);
			}
			return null;
		}
		#endregion

		#region IPhpServerObject Members
		public string GetPhpMethodRef(string objectName, string methodname, StringCollection methodCode, string[] parameters)
		{
			if (string.CompareOrdinal(methodname, "Update") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture, "$this->UpdateTable($this->{0})", objectName);
			}
			return null;
		}
		#endregion
	}
	public delegate void fnHandleUpdate(EasyDataSet sender, bool createNewRecord);
	public delegate void fnHandleDataFetch(int rowNumber, WebDataRow row);
	public class WebDataRow : JsonDataRow, ICustomTypeDescriptor, IPhpCodeType
	{
		#region fields and constructors
		public WebDataRow()
			: base(new object[] { })
		{
		}
		public WebDataRow(object[] r)
			: base(r)
		{
		}
		#endregion
		#region Methods
		[WebServerMember]
		public void setColumnValue(int i, object v)
		{
			ItemArray[i] = v;
		}
		[WebServerMember]
		public object getColumnValue(int i)
		{
			return ItemArray[i];
		}
		[WebServerMember]
		public int getColumnCount()
		{
			return Count;
		}
		#endregion
		#region ICustomTypeDescriptor Members

		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}

		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}

		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			Attribute[] attrs = new Attribute[1];
			int n = this.Count;
			attrs[0] = new WebClientMemberAttribute();
			PropertyDescriptor[] ps = new PropertyDescriptor[n + 1];
			ps[0] = new PropertyDescriptorReadOnlyValue("Count", n, this.GetType(), attrs);
			for (int i = 0; i < n; i++)
			{
				ps[i + 1] = new PropertyDescriptorReadOnlyValue(string.Format(CultureInfo.InvariantCulture, "Column{0}", i), ItemArray[i], this.GetType(), attrs);
			}
			return new PropertyDescriptorCollection(ps);
		}

		public PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[] { });
		}

		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}

		#endregion
		#region IPhpCodeType Members

		public void CreateActionPhpScript(string objectName, string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			if (string.CompareOrdinal(methodName, "setColumnValue") == 0)
			{
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}->ItemArray[{1}] = {2};\r\n", objectName, parameters[0], parameters[1]));
			}
		}

		#endregion
	}
}
