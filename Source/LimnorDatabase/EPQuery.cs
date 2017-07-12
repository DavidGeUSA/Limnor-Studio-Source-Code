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
using System.IO;
using System.Xml;
using System.Globalization;
using MathExp;
using System.CodeDom;
using Limnor.Net;

namespace LimnorDatabase
{
	public enum enmQueryType { SELECT_QUERY, FROM_QUERY, WHERE_QUERY, GROUPBY_QUERY, ORDERBY_QUERY }
	public enum enmTableRelation { INNER, LEFT, RIGHT, NONE }
	/*
	 SELECT ...
 FROM ...
 WHERE ...
 GROUP BY ...
 HAVING ...
 ORDER BY ...
	 */
	/// <summary>
	/// SQL Query
	/// </summary>
	[global::System.Serializable()]
	[global::System.ComponentModel.ToolboxItem(true)]
	[global::System.ComponentModel.Design.HelpKeywordAttribute("vs.data.DataSet")]
	[Description("It defines database query to provide data to a DataSet. It provides a Query Builder to let you build SQL query.")]
	[ToolboxBitmapAttribute(typeof(EasyQuery), "Resources.qry.bmp")]
	public class EasyQuery : ICloneable, IComponent, IDatabaseAccess, IReport32Usage, IDynamicMethodParameters, ISourceValueEnumProvider, ICustomMethodCompiler
	{
		#region private fields
		private EnumDataSourceType _sourceType;
		//=========================================
		private ConnectionItem connect = null;
		private string _defConnectionString;
		private Type _defConnectionType;
		//=========================================
		private DataTableNewRowEventHandler _miNewRow;
		private DataRowChangeEventHandler _miRowDeleted;
		private DataRowChangeEventHandler _miRowDeleting;
		//=========================================
		private bool _isQuerying;
		private BindingContext _context;
		private BindingManagerBase _bindingMan;
		private DataSet _data;
		private DbDataAdapter da = null;
		private DbCommand cmdSelection = null;
		private DbCommand cmdTimestamp = null;
		private DbCommand cmdUpdate = null;
		private DbCommand cmdDeletion = null;
		private DbCommand cmdInsert = null;
		private string[] insertFields = null;
		private ConnectionItem _timestampConnection;
		//
		private bool _hasCalculatedFields;
		private bool _forReadOnly;
		private bool _readOnlyResult;
		private BLOBTable blobTable = null;
		private TableAliasList _tableDefs;
		//
		private string _selectClause;
		private SQLStatement _sql;
		private string _rawSql;
		private FieldList _fields;
		private FieldList rowIDFields = null;
		private ParameterList _parameters = null;
		private ParameterList _designtimeParameters = null;
		private int _currentRowIndex;
		private string _lastError;
		private bool _loaded;
		private bool _reloadAfterUpdate = true;
		//
		private string _tableName;
		private bool _changeDataSet;
		private bool _designMode;
		private StringCollection _messages;
		private long _identity;
		//
		/// <summary>
		/// original form without mapping parameters.
		/// parameters by position, names are mapped to Parameters 
		/// for EnumParameterStyle.QuestionMark
		/// </summary>
		private FieldList _paramMap = null;
		#endregion

		#region constructors
		public EasyQuery()
		{
			_miNewRow = new DataTableNewRowEventHandler(EasyQuery_TableNewRow);
			_miRowDeleting = new DataRowChangeEventHandler(EasyQuery_RowDeleted);
			_miRowDeleted = new DataRowChangeEventHandler(EasyQuery_RowDeleting);
			QueryOnStart = true;
			IsCommand = false;
			CommandText = "";
			ShowErrorMessage = true;
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

		[Browsable(false)]
		public event EventHandlerNewRowID CreatedNewRowID;
		#endregion

		#region internal members
		internal EventHandler EmptyRecordHandler;
		internal EventHandler BeforeDeleteRecordHandler;
		internal EventHandler AfterDeleteRecordHandler;
		internal void SetReloadAfterUpdate(bool reload)
		{
			_reloadAfterUpdate = reload;
		}
		//design time usages by query builder===
		internal bool bQueryOK = false;
		[ReadOnly(true)]
		[Browsable(false)]
		internal TableAliasList T
		{
			get
			{
				if (_tableDefs == null)
				{
					_tableDefs = new TableAliasList();
					FieldList fl = Fields;
					foreach (EPField f in fl)
					{
						if (!string.IsNullOrEmpty(f.FromTableName))
						{
							if (!_tableDefs.IsTableIncluded(f.FromTableName))
							{
								TableAlias t = new TableAlias(f.FromTableName, "");
								_tableDefs.AddTable(t);
							}
						}
					}

				}
				return _tableDefs;
			}
			set
			{
				_tableDefs = value;
			}
		}

		private BindingManagerBase getCurrencyManager()
		{
			if (_bindingMan != null)
			{
				return _bindingMan;
			}
			BindingManagerBase cm = null;
			if (_context != null)
			{
				if (_context.Contains(_data, TableName))
				{
					cm = _context[_data, TableName];
				}
			}
			return cm;
		}
		private string formErrorMessage()
		{
			StringBuilder sb = new StringBuilder();
			if (da != null)
			{
				if (da.SelectCommand != null)
				{
					sb.Append("SQL:");
					sb.Append(da.SelectCommand.CommandText);
					sb.Append("\r\n");
					sb.Append("Parameters:");
					sb.Append(da.SelectCommand.Parameters.Count);
					for (int i = 0; i < da.SelectCommand.Parameters.Count; i++)
					{
						sb.Append("\r\nParameter ");
						sb.Append(i);
						sb.Append(":");
						sb.Append(da.SelectCommand.Parameters[i].ParameterName);
						sb.Append(", ");
						sb.Append(da.SelectCommand.Parameters[i].DbType);
						sb.Append(", value:");
						sb.Append(da.SelectCommand.Parameters[i].Value);
					}
				}
			}
			return sb.ToString();
		}
		internal void SetDesignMode(bool isDesignMode)
		{
			_designMode = isDesignMode;
		}
		internal void SetDataSet(DataSet ds)
		{
			_data = ds;
		}
		[Browsable(false)]
		public void RemoveTable(string tableName)
		{
			if (_data == null)
			{
				LogMessage("{0} - EasyQuery.RemoveTable({1}). Ignored because dataset does not exist", TableName, tableName);
			}
			else
			{
				DataTable tbl = _data.Tables[tableName];
				if (tbl == null)
				{
					LogMessage("{0} - EasyQuery.RemoveTable({1}). Ignored because {1} does not exist", TableName, tableName);
				}
				else
				{
					LogMessage("{0} - EasyQuery.RemoveTable({1})\r\n\tRelations:{2}\r\n\tTables:{3}", TableName, tableName, _data.Relations.Count, _data.Tables.Count);
					_data.Relations.Clear();
					//remove foreign keys
					for (int i = 0; i < _data.Tables.Count; i++)
					{
						LogMessage("\t{0}.Constraints:{1},ChildRelations:{2},ParentRelations:{3}", _data.Tables[i].TableName, _data.Tables[i].Constraints.Count, _data.Tables[i].ChildRelations.Count, _data.Tables[i].ParentRelations.Count);
						List<Constraint> cs = new List<Constraint>();
						for (int j = 0; j < _data.Tables[i].Constraints.Count; j++)
						{
							LogMessage("\t\t{0}:{1}", _data.Tables[i].Constraints[j].ConstraintName, _data.Tables[i].Constraints[j].GetType().FullName);
							System.Data.ForeignKeyConstraint fk = _data.Tables[i].Constraints[j] as System.Data.ForeignKeyConstraint;
							if (fk != null)
							{
								cs.Add(fk);
							}
						}
						if (cs.Count > 0)
						{
							foreach (Constraint c in cs)
							{
								_data.Tables[i].Constraints.Remove(c);
							}
						}
					}
					for (int i = 0; i < _data.Tables.Count; i++)
					{
						_data.Tables[i].ChildRelations.Clear();
					}
					for (int i = 0; i < _data.Tables.Count; i++)
					{
						_data.Tables[i].ParentRelations.Clear();
					}
					tbl.Constraints.Clear();
					_data.Relations.Clear();
					tbl.PrimaryKey = null;
					tbl.Rows.Clear();
					try
					{
						tbl.Columns.Clear();
					}
					catch
					{
					}
				}
			}
		}
		#endregion

		#region Fields Serialization
		private void setFieldCount(int len, bool trim)
		{
			if (_fields == null)
			{
				_fields = new FieldList();
			}
			int n = _fields.Count;
			for (int i = n; i < len; i++)
			{
				_fields.Add(new EPField());
			}
			if (trim)
			{
				while (_fields.Count > len)
				{
					_fields.RemoveAt(_fields.Count - 1);
				}
			}
		}
		[Browsable(false)]
		public string[] Field_Name
		{
			get
			{
				string[] ss = new string[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].Name;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, true);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].Name = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public bool[] Field_ReadOnly
		{
			get
			{
				bool[] ss = new bool[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].ReadOnly;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].ReadOnly = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public OleDbType[] Field_OleDbType
		{
			get
			{
				OleDbType[] ss = new OleDbType[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].OleDbType;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].OleDbType = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public bool[] Field_IsIdentity
		{
			get
			{
				bool[] ss = new bool[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].IsIdentity;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].IsIdentity = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public bool[] Field_Indexed
		{
			get
			{
				bool[] ss = new bool[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].Indexed;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].Indexed = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public int[] Field_DataSize
		{
			get
			{
				int[] ss = new int[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].DataSize;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].DataSize = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public string[] Field_FromTableName
		{
			get
			{
				string[] ss = new string[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].FromTableName;
				}
				return ss;
			}
			set
			{
				if (value != null)
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].FromTableName = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public string[] Field_FieldText
		{
			get
			{
				string[] ss = new string[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].FieldText;
				}
				return ss;
			}
			set
			{
				if (value != null)
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].FieldText = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public string[] Field_Expression
		{
			get
			{
				string[] ss = new string[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].FieldExpression;
					if (ss[i] == null)
						ss[i] = string.Empty;
				}
				return ss;
			}
			set
			{
				if (value != null)
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].FieldExpression = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public string[] Field_FieldCaption
		{
			get
			{
				string[] ss = new string[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].FieldCaption;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].FieldCaption = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public bool[] Field_IsCalculated
		{
			get
			{
				bool[] ss = new bool[Fields.Count];
				for (int i = 0; i < _fields.Count; i++)
				{
					ss[i] = _fields[i].IsCalculated;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_fields = null;
				}
				else
				{
					setFieldCount(value.Length, false);
					for (int i = 0; i < value.Length; i++)
					{
						_fields[i].IsCalculated = value[i];
					}
				}
			}
		}
		#endregion

		#region Parameters Serialization
		private void setParameterCount(int len)
		{
			if (_parameters == null)
			{
				_parameters = new ParameterList();
			}
			int n = _parameters.Count;
			for (int i = n; i < len; i++)
			{
				_parameters.Add(new EPField());
			}
			while (_parameters.Count > len)
			{
				_parameters.RemoveAt(_parameters.Count - 1);
			}
			_designtimeParameters = _parameters.Clone() as ParameterList;
		}
		[Browsable(false)]
		public string[] Param_Name
		{
			get
			{
				string[] ss = new string[Parameters.Count];
				for (int i = 0; i < _parameters.Count; i++)
				{
					ss[i] = _parameters[i].Name;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_parameters = null;
				}
				else
				{
					setParameterCount(value.Length);
					for (int i = 0; i < value.Length; i++)
					{
						_parameters[i].Name = value[i];
						_designtimeParameters[i].Name = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public OleDbType[] Param_OleDbType
		{
			get
			{
				OleDbType[] ss = new OleDbType[Parameters.Count];
				for (int i = 0; i < _parameters.Count; i++)
				{
					ss[i] = _parameters[i].OleDbType;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_parameters = null;
				}
				else
				{
					setParameterCount(value.Length);
					for (int i = 0; i < value.Length; i++)
					{
						_parameters[i].OleDbType = value[i];
						_designtimeParameters[i].OleDbType = value[i];
					}
				}
			}
		}
		[Browsable(false)]
		public int[] Param_DataSize
		{
			get
			{
				int[] ss = new int[Parameters.Count];
				for (int i = 0; i < _parameters.Count; i++)
				{
					ss[i] = _parameters[i].DataSize;
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_parameters = null;
				}
				else
				{
					setParameterCount(value.Length);
					for (int i = 0; i < value.Length; i++)
					{
						_parameters[i].DataSize = value[i];
						_designtimeParameters[i].DataSize = value[i];
					}
				}
			}
		}

		[Browsable(false)]
		public string[] Param_Value
		{
			get
			{
				string[] ss = new string[Parameters.Count];
				for (int i = 0; i < _parameters.Count; i++)
				{
					if (_parameters[i].Value != null && _parameters[i].Value != DBNull.Value)
					{
						TypeConverter tc = TypeDescriptor.GetConverter(_parameters[i].Value);
						ss[i] = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}|{1}", _parameters[i].Value.GetType().AssemblyQualifiedName, tc.ConvertToInvariantString(_parameters[i].Value));
					}
					else
					{
						ss[i] = string.Empty;
					}
				}
				return ss;
			}
			set
			{
				if (value == null)
				{
					_parameters = null;
				}
				else
				{
					setParameterCount(value.Length);
					for (int i = 0; i < value.Length; i++)
					{
						if (string.IsNullOrEmpty(value[i]))
						{
							_parameters[i].SetValue(null);
							_designtimeParameters[i].SetValue(null);
						}
						else
						{
							string[] ss = value[i].Split('|');
							if (ss.Length == 2)
							{
								Type t = Type.GetType(ss[0]);
								if (t != null)
								{
									TypeConverter tc = TypeDescriptor.GetConverter(t);
									if (tc != null)
									{
										_parameters[i].SetValue(tc.ConvertFromInvariantString(ss[1]));
										_designtimeParameters[i].SetValue(tc.ConvertFromInvariantString(ss[1]));
									}
								}
							}
							else
							{
								_parameters[i].SetValue(null);
								_designtimeParameters[i].SetValue(null);
							}
						}
					}
				}
			}
		}

		#endregion

		#region Properties
		[Browsable(false)]
		[DefaultValue(false)]
		public bool IsCommand
		{
			get;
			set;
		}
		[Browsable(false)]
		[DefaultValue("")]
		public string CommandText
		{
			get
			{
				if (_sql != null)
				{
					return _sql.GetSQL();
				}
				return string.Empty;
			}
			set
			{
				if (_sql == null)
				{
					_sql = new SQLStatement(value);
				}
				else
				{
					_sql.SetSQL(value);
				}
			}
		}
		[Browsable(false)]
		public StringCollection ProcessMessages
		{
			get
			{
				if (_messages == null)
				{
					_messages = new StringCollection();
				}
				return _messages;
			}
		}
		[Browsable(false)]
		public bool IsQuerying
		{
			get
			{
				return _isQuerying;
			}
		}
		[Browsable(false)]
		public string TableName
		{
			get
			{
				if (string.IsNullOrEmpty(_tableName))
				{
					_tableName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Table{0}", (UInt32)(Guid.NewGuid().GetHashCode()));
					EasyDataSet eds = _data as EasyDataSet;
					if (eds != null)
					{
						eds.TableName = _tableName;
					}
				}
				return _tableName;
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					_tableName = value;
				}
			}
		}
		/// <summary>
		/// for saving reference
		/// </summary>
		[IgnoreXmlIgnore]
		[XmlIgnore]
		[Browsable(false)]
		public IComponent DataStorageRef
		{
			get
			{
				if (_data != null && _data.Site != null && _data.Site.DesignMode)
				{
					return _data;
				}
				return null; //do not save an instance
			}
			set
			{
				DataSet ds = value as DataSet;
				if (ds != null)
				{
					_data = ds;
				}
			}
		}
		[Browsable(false)] //not allow
		[Category("Database")]
		[XmlIgnore]
		[ComponentReferenceSelectorType(typeof(DataSet))]
		[Editor(typeof(ComponentReferenceSelector), typeof(UITypeEditor))]
		[Description("This is a DataSet to store the query results")]
		public DataSet DataStorage
		{
			get
			{
				return _data;
			}
			set
			{
				if (value == null)
				{
					_data = null;
				}
				else
				{
					DataSet ds = value;
					if (ds.Site != null && ds.Site.DesignMode)
					{
						if (_data != null && _data.Tables.Count > 0)
						{
							for (int i = 0; i < _data.Tables.Count; i++)
							{
								DataTable tbl = ds.Tables[_data.Tables[i].TableName];
								if (tbl == null)
								{
									tbl = ds.Tables.Add(_data.Tables[i].TableName);
									for (int k = 0; k < _data.Tables[i].Columns.Count; k++)
									{
										tbl.Columns.Add(_data.Tables[i].Columns[k].ColumnName, _data.Tables[i].Columns[k].DataType, _data.Tables[i].Columns[k].Expression);
									}
									for (int k = 0; k < _data.Tables[i].Rows.Count; k++)
									{
										tbl.Rows.Add(_data.Tables[i].Rows[k].ItemArray);
									}
								}
							}
						}
					}
					_data = ds;
				}
			}
		}
		[Browsable(false)]
		[ReadOnly(true)]
		[XmlIgnore]
		public bool IsSearching
		{
			get;
			set;
		}
		[Browsable(false)]
		[Category("Database")]
		[ReadOnly(true)]
		[XmlIgnore]
		[Description("When retrieving values by Fields property, this property indicates the row number. 0 indicates the first row; 1 indicates the second row, and so on.")]
		public int RowIndex
		{
			get
			{
				BindingManagerBase cm = getCurrencyManager();
				if (cm != null)
				{
					return cm.Position;
				}
				return -1;
			}
			set
			{
				BindingManagerBase cm = getCurrencyManager();
				if (cm != null)
				{
					cm.Position = value;
				}
			}
		}
		[Browsable(false)]
		[Category("Database")]
		[Description("The number of records currently hold in the DataTable")]
		public int RowCount
		{
			get
			{
				if (_data != null && _data.Tables.Count > 0)
				{
					DataTable tbl = _data.Tables[TableName];
					if (tbl != null)
					{
						return tbl.Rows.Count;
					}
				}
				return 0;
			}
		}
		[Browsable(false)]
		[Category("Database")]
		[Description("Gets a value indicating whether RowCount is greater than 0")]
		public bool HasData
		{
			get
			{
				if (_isQuerying)
				{
					return false;
				}
				return (RowCount > 0);
			}
		}
		[Browsable(false)]
		[Category("Database")]
		[Description("The DataTable for holding the records for this instance")]
		public DataTable DataTable
		{
			get
			{
				if (_data != null)
				{
					return _data.Tables[TableName];
				}
				return null;
			}
		}
		[Browsable(false)]
		[Category("Database")]
		[Description("The number of records currently held in memory, which are also in the database")]
		public int CommitedRowCount
		{
			get
			{
				DataTable tbl = CurrentDataTable;
				if (tbl != null)
				{
					int n = 0;
					for (int i = 0; i < tbl.Rows.Count; i++)
					{
						if (tbl.Rows[i].RowState == DataRowState.Modified || tbl.Rows[i].RowState == DataRowState.Unchanged)
						{
							n++;
						}
					}
					return n;
				}
				return 0;
			}
		}
		[Category("Database")]
		[Description("Description for this query")]
		public string Description { get; set; }

		[DefaultValue(null)]
		[NotForProgramming]
		[Browsable(false)]
		[Category("Database")]
		[Description("gets and sets sql scripts used before SELECT")]
		public string DataPreparer { get; set; }

		[DefaultValue(null)]
		[NotForProgramming]
		[Browsable(false)]
		public string[] DataPreparerParameternames { get; set; }

		[DefaultValue(null)]
		[NotForProgramming]
		[Browsable(false)]
		public string[] SqlParameternames { get; set; }

		[Browsable(false)]
		[Description("Indicates whether duplicated records are allowed")]
		public bool Distinct { get; set; } //include DISTINCT

		[Browsable(false)]
		[Description("Specifies that the query result contain a specific number of rows or a percentage of rows of the query result. Following keyword TOP, you can specify 1 to 32,767 rows, or, if you include the PERCENT option, you can specify 0.01 to 99.99 percent. ")]
		public int Top { get; set; } // include TOP <Top>

		[Browsable(false)]
		[Description("Specifies that the query result contain a specific number of rows or a percentage of rows of the query result. Following keyword TOP, you can specify 1 to 32,767 rows, or, if you include the PERCENT option, you can specify 0.01 to 99.99 percent. ")]
		public bool Percent { get; set; }

		[Browsable(false)]
		[Description("Specifies that additional rows be returned from the base result set with the same value in the ORDER BY columns appearing as the last of the TOP n (PERCENT) rows. TOP…WITH TIES can be specified only in SELECT statements, and only if an ORDER BY clause is specified.")]
		public bool WithTies { get; set; }

		[Browsable(false)]
		[Category("Database")]
		[Description("The table that can be updated")]
		public string TableToUpdate
		{
			get
			{
				return UpdatableTableName;
			}
		}

		[Browsable(false)]
		[ReadOnly(true)]
		[XmlIgnore]
		public bool IsFilterInsertedByQueryBuilder
		{
			get;
			set;
		}
		//=========================================

		[Browsable(false)]
		[Category("Database")]
		[XmlIgnore]
		[ReadOnly(true)]
		[Editor(typeof(HideUITypeEditor), typeof(UITypeEditor))]
		[Description("Fields in this data table. It represents the current row at runtime.")]
		public FieldList Fields
		{
			get
			{
				if (_fields == null)
				{
					_fields = new FieldList();
				}
				DataTable tbl = CurrentDataTable;
				if (tbl != null)
				{
					int n = RowIndex;
					if (n >= 0)
					{
						if (tbl.Rows.Count > n)
						{
							SetFieldValues(tbl.Rows[n]);
							_currentRowIndex = n;
						}
					}
				}
				return _fields;
			}
			set
			{
				_fields = value;
			}
		}
		[Browsable(false)]
		public DataRow CurrentDataRow
		{
			get
			{
				DataTable tbl = CurrentDataTable;
				if (tbl != null)
				{
					int n = RowIndex;
					if (n >= 0 && _currentRowIndex != n)
					{
						if (tbl.Rows.Count > n)
						{
							return tbl.Rows[n];
						}
					}
				}
				return null;
			}
		}
		[Browsable(false)]
		[Category("Database")]
		[ReadOnly(true)]
		[Editor(typeof(HideUITypeEditor), typeof(UITypeEditor))]
		[Description("Parameters for filtering the data.")]
		public ParameterList Parameters
		{
			get
			{
				if (_parameters == null)
				{
					_parameters = new ParameterList { };
				}
				if (_parameters != null)
				{
					foreach (EPField f in _parameters)
					{
						f.ReadOnly = false;
					}
				}
				return _parameters;
			}
			set
			{
				_parameters = value;
				if (_parameters != null)
				{
					foreach (EPField f in _parameters)
					{
						f.ReadOnly = false;
					}
				}
			}
		} //Named parameters. Names are unique
		[Browsable(false)]
		[Category("Database")]
		[Description("Indicate whether the data is changeable")]
		public bool ReadOnly
		{
			get
			{
				return _readOnlyResult;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[DefaultValue(false)]
		[Description("Indicate whether the changing of data is desired")]
		public bool ForReadOnly
		{
			get
			{
				return _forReadOnly;
			}
			set
			{
				_forReadOnly = value;
				if (value)
				{
					_readOnlyResult = true;
				}
			}
		}

		[Browsable(false)]
		[Description("Gets and sets a SELECT statement for getting the last autonumber. it is used by CreateNewRecord action.")]
		public string CommandToGetAutoNumber { get; set; }

		[Browsable(false)]
		public long LastIdentity
		{
			get
			{
				return _identity;
			}
		}

		private bool _queryChanged;

		[Browsable(false)]
		public string SELECT
		{
			get
			{
				return _selectClause;
			}
		}

		private string _from;
		[Browsable(false)]
		[Description("'FROM' clause of the SELECT statement")]
		public string From { get { return _from; } set { _from = value; _queryChanged = true; } }

		private string _where;
		[Browsable(false)]
		[Description("'WHERE' clause of the SELECT statement")]
		public string Where
		{
			get
			{
				return _where;
			}
			set
			{
				if (value != null)
				{
					if (value.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
					{
						_where = value.Substring(6).Trim();
					}
					else
					{
						_where = value;
					}
				}
				else
				{
					_where = value;
				}
				_queryChanged = true;
			}
		}

		private string _groupBy;
		[Browsable(false)]
		[Description("'GROUP BY' clause of the SELECT statement")]
		public string GroupBy { get { return _groupBy; } set { _groupBy = value; _queryChanged = true; } }

		private string _having;
		[Browsable(false)]
		[Description("'HAVING' clause of the SELECT statement")]
		public string Having { get { return _having; } set { _having = value; _queryChanged = true; } }

		private string _orderBy;
		[Browsable(false)]
		[Description("'ORDER BY' clause of the SELECT statement")]
		public string OrderBy { get { return _orderBy; } set { _orderBy = value; _queryChanged = true; } }

		[Browsable(false)]
		[Description("'LIMIT' clause of the SELECT statement")]
		public string Limit { get; set; }

		[ReadOnly(true)]
		[Browsable(false)]
		[Description("'TOP or LIMIT' clause of the SELECT statement for sampling data")]
		public int SampleTopRec { get; set; }

		[ReadOnly(true)]
		[Browsable(false)]
		[Description("Use SampleTopRec in SELECT statement for sampling data")]
		public bool UseSampleTopRec { get; set; }

		[Browsable(false)]
		[Category("Database")]
		[Description("Error message from the last failed operation")]
		public string LastError
		{
			get
			{
				return _lastError;
			}
		}
		[Category("Database")]
		[ReadOnly(true)]
		[TypeConverter(typeof(TypeConverterSQLString))]
		[XmlIgnore]
		[Description("SQL statement for querying database")]
		[Editor(typeof(UIQueryEditor), typeof(UITypeEditor))]
		public SQLStatement SQL
		{
			get
			{
				ParameterList ps = this.Parameters.Clone() as ParameterList;
				MakeSelectionQuery(ps);
				return _sql;
			}
			set
			{
				LogMessage("{0} - Setting EasyQuery.SQL starts...\r\n\tvalue:{1}\r\n\tdesign mode:{2}\r\n\r\n", TableName, value, _designMode);
				_sql = value;
				if (!_designMode && connect != null && connect.ConnectionObject.IsConnectionReady)
				{
					if (value != null)
					{
						string sSQL = value.GetSQL();
						if (!string.IsNullOrEmpty(sSQL))
						{
							if (parsesql(sSQL))
							{
								if (_loaded)
								{
									LogMessage("{0} - Set EasyQuery.SQL: Execute Query.", TableName);
									Query();
									if (SqlChanged != null)
									{
										SqlChanged(this, EventArgs.Empty);
									}
								}
							}
						}
					}
				}
				LogMessage("{0} - Setting EasyQuery.SQL ends---", TableName);
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
				if (connect == null)
				{
					connect = new ConnectionItem();
				}
				return connect;
			}
			set
			{
				connect = value;
			}
		}
		[Browsable(false)]
		[Category("Database")]
		[Description("Connection string used for connecting to the database")]
		public string ConnectionString
		{
			get
			{
				if (connect != null)
				{
					return connect.ConnectionStringDisplay;
				}
				return "";
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Accessing this property will test connect to the database. If the connection is made then this property is True; otherwise it is False.")]
		public bool IsConnectionReady
		{
			get
			{
				if (connect != null)
				{
					return connect.ConnectionObject.IsConnectionReady;
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Indicates the database connection is through an OleDbConnection for OleDb drives such as Microsoft Access, Excel, etc.")]
		public bool IsOleDb
		{
			get
			{
				if (connect != null)
				{
					return (connect.ConnectionObject.TheConnection is OleDbConnection);
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Indicates the database connection is through an OdbcConnection for ODBC drives such as MySQL, PostgreSQL, etc.")]
		public bool IsOdbc
		{
			get
			{
				if (connect != null)
				{
					return (connect.ConnectionObject.TheConnection is OdbcConnection);
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Indicates the database connection is through a SqlConnection for Microsoft SQL Server")]
		public bool IsSQLSever
		{
			get
			{
				if (connect != null)
				{
					return (connect.ConnectionObject.TheConnection is SqlConnection);
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Indicates the database connection is through a MySqlConnection for MySql Server")]
		public bool IsMySql
		{
			get
			{
				if (connect != null)
				{
					return (connect.ConnectionObject.IsMySql);
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Indicates the database connection is through an OracleConnection for Oracle database")]
		public bool IsOracle
		{
			get
			{
				if (connect != null)
				{
#if NET35
					return (connect.ConnectionObject.TheConnection is OracleConnection);
#else
					return connect.ConnectionObject.TheConnection.GetType().Name.IndexOf("OracleConnection") >= 0;
#endif
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("Indicates the database connection is through a Jet database driver")]
		public bool IsJet
		{
			get
			{
				if (connect != null)
				{
					return (connect.ConnectionObject.IsJet);
				}
				return false;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("SQL statement for updating a row in the database")]
		public string UpdateSql
		{
			get
			{
				if (da != null && da.UpdateCommand != null)
				{
					return da.UpdateCommand.CommandText;
				}
				return "";
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("SQL statement for deleting a row from the database")]
		public string DeleteSql
		{
			get
			{
				if (da != null && da.DeleteCommand != null)
				{
					return da.DeleteCommand.CommandText;
				}
				return "";
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[Description("SQL statement for inserting a new row to the database")]
		public string InsertSql
		{
			get
			{
				if (da != null && da.InsertCommand != null)
				{
					return da.InsertCommand.CommandText;
				}
				return "";
			}
		}

		[Browsable(false)]
		[Description("Indicates the file path for logging errors")]
		public static string LogFile
		{
			get
			{
				return _logfilepath;
			}
		}

		[Browsable(false)]
		[Category("Database")]
		[DefaultValue(true)]
		[Description("Indicates whether to make database query when this object is created.")]
		public bool QueryOnStart
		{
			get;
			set;
		}

		[Browsable(false)]
		[Category("Database")]
		[DefaultValue(true)]
		[Description("Controls whether error message is displayed when an error occurs. Error messages are logged in a text file. The file path is indicated by LogFile property.")]
		public bool ShowErrorMessage
		{
			get;
			set;
		}
		#endregion

		#region Methods
		public void SetLastErrorMessage(string message)
		{
			_lastError = message;
		}
		[Browsable(false)]
		[NotForProgramming]
		public void SetName(string name)
		{
			if (Site != null)
			{
				Site.Name = name;
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public bool HasLeftJoin()
		{
			if (!string.IsNullOrEmpty(_from))
			{
				int pos = _from.IndexOf(" LEFT ", StringComparison.OrdinalIgnoreCase);
				if (pos >= 0)
				{
					string s = _from.Substring(pos + 6).TrimStart(' ');
					if (s.StartsWith("Join ", StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
			return false;
		}
		[Browsable(false)]
		[NotForProgramming]
		public EPField GetMappedParameter(int i)
		{
			return _paramMap[i];
		}
		[Browsable(false)]
		[NotForProgramming]
		public void ResetQuery(string sSQL)
		{
			parsesql(sSQL);
			PrepareQuery();
		}
		[Browsable(false)]
		[NotForProgramming]
		public void PrepareQuery()
		{
			if (initializeFields())
			{
				if (initializeCommands())
				{
					_queryChanged = false;
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void PrepareQueryIfChanged()
		{
			if (_queryChanged)
			{
				if (initializeFields())
				{
					if (initializeCommands())
					{
						_queryChanged = false;
						RestoreParameters();
					}
				}
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void RestoreParameters()
		{
			if (_designtimeParameters != null && _designtimeParameters.Count > 0 && _parameters != null && _parameters.Count > 0)
			{
				for (int i = 0; i < _parameters.Count; i++)
				{
					EPField f = _designtimeParameters[_parameters[i].Name];
					if (f != null)
					{
						_parameters[i].OleDbType = f.OleDbType;
						_parameters[i].DataSize = f.DataSize;
					}
				}
			}
		}
		[Description("Create a data binding object. It is to be added to DataBindings property via DataBindings.Add(obj) method, where obj is created by this method.")]
		public Binding CreateDataBinding(string bindToPropertyName, string sourceFieldName)
		{
			return new Binding(bindToPropertyName, _data, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", TableName, sourceFieldName));
		}
		[Description("Set the next record of the first table in DataStorage as the current record")]
		public bool MoveNext()
		{
			BindingManagerBase cm = getCurrencyManager();
			if (cm != null)
			{
				if (cm.Position < this.RowCount - 1)
				{
					cm.Position += 1;
					return true;
				}
			}
			return false;
		}
		[Description("Set the previous record of the first table in DataStorage as the current record")]
		public bool MovePrevious()
		{
			BindingManagerBase cm = getCurrencyManager();
			if (cm != null)
			{
				if (cm.Position > 0)
				{
					cm.Position -= 1;
					return true;
				}
			}
			return false;
		}
		[Description("Set the last record of the first table in DataStorage as the current record")]
		public bool MoveLast()
		{
			DataTable tbl = CurrentDataTable;
			if (tbl != null)
			{
				BindingManagerBase cm = getCurrencyManager();
				if (cm != null)
				{
					if (tbl.Rows.Count > 0)
					{
						cm.Position = tbl.Rows.Count - 1;
						return true;
					}
				}
			}
			return false;
		}
		[Description("Set the first record of the first table in DataStorage as the current record")]
		public bool MoveFirst()
		{
			if (HasData)
			{
				BindingManagerBase cm = getCurrencyManager();
				if (cm != null)
				{
					cm.Position = 0;
					return true;
				}
			}
			return false;
		}
		[Description("Get information about this query")]
		public string GetQueryInfo()
		{
			StringBuilder sb = new StringBuilder();
			if (connect == null)
			{
				sb.Append("No connection\r\n");
			}
			else
			{
				sb.Append("connection:");
				sb.Append(connect.ConnectionStringDisplay);
				sb.Append("\r\n");
			}
			if (_sql == null)
			{
				sb.Append("_sql is null\r\n");
			}
			else
			{
				sb.Append("_sql:");
				sb.Append(_sql.ToString());
				sb.Append("\r\n");
			}
			if (cmdSelection == null)
			{
				sb.Append("cmdSelection is null\r\n");
			}
			else
			{
				sb.Append("cmdSelection:");
				sb.Append(cmdSelection.CommandText);
				sb.Append("\r\nParameters:");
				sb.Append(cmdSelection.Parameters.Count.ToString());
				sb.Append("\r\n");
				for (int i = 0; i < cmdSelection.Parameters.Count; i++)
				{
					sb.Append("parameter ");
					sb.Append(i.ToString());
					sb.Append(":");
					sb.Append(cmdSelection.Parameters[i].ParameterName);
					sb.Append(",");
					sb.Append(cmdSelection.Parameters[i].DbType.ToString());
					sb.Append("\r\n");
				}
			}
			if (cmdUpdate == null)
			{
				sb.Append("cmdUpdate is null\r\n");
			}
			else
			{
				sb.Append("cmdUpdate:");
				sb.Append(cmdUpdate.CommandText);
				sb.Append("\r\nParameters:");
				sb.Append(cmdUpdate.Parameters.Count.ToString());
				sb.Append("\r\n");
				for (int i = 0; i < cmdUpdate.Parameters.Count; i++)
				{
					sb.Append("parameter ");
					sb.Append(i.ToString());
					sb.Append(":");
					sb.Append(cmdUpdate.Parameters[i].ParameterName);
					sb.Append(",");
					sb.Append(cmdUpdate.Parameters[i].DbType.ToString());
					sb.Append("\r\n");
				}
			}
			if (cmdDeletion == null)
			{
				sb.Append("cmdDeletion is null\r\n");
			}
			else
			{
				sb.Append("cmdDeletion:");
				sb.Append(cmdDeletion.CommandText);
				sb.Append("\r\nParameters:");
				sb.Append(cmdDeletion.Parameters.Count.ToString());
				sb.Append("\r\n");
				for (int i = 0; i < cmdDeletion.Parameters.Count; i++)
				{
					sb.Append("parameter ");
					sb.Append(i.ToString());
					sb.Append(":");
					sb.Append(cmdDeletion.Parameters[i].ParameterName);
					sb.Append(",");
					sb.Append(cmdDeletion.Parameters[i].DbType.ToString());
					sb.Append("\r\n");
				}
			}
			if (cmdInsert == null)
			{
				sb.Append("cmdInsert is null\r\n");
			}
			else
			{
				sb.Append("cmdInsert:");
				sb.Append(cmdInsert.CommandText);
				sb.Append("\r\nParameters:");
				sb.Append(cmdInsert.Parameters.Count.ToString());
				sb.Append("\r\n");
				for (int i = 0; i < cmdInsert.Parameters.Count; i++)
				{
					sb.Append("parameter ");
					sb.Append(i.ToString());
					sb.Append(":");
					sb.Append(cmdInsert.Parameters[i].ParameterName);
					sb.Append(",");
					sb.Append(cmdInsert.Parameters[i].DbType.ToString());
					sb.Append("\r\n");
				}
			}
			return sb.ToString();
		}
		[Description("Add a new empty record")]
		public void AddNewRecord()
		{
			if (_data != null && _data.Tables.Count > 0)
			{
				if (!this.ReadOnly)
				{
					BindingManagerBase cm = getCurrencyManager();
					if (cm != null)
					{
						cm.AddNew();
					}
				}
			}
		}
		[Description("Delete the current record")]
		public bool DeleteCurrentRecord()
		{
			if (_data != null && _data.Tables.Count > 0)
			{
				if (!this.ReadOnly)
				{
					BindingManagerBase cm = getCurrencyManager();
					if (cm != null && cm.Position >= 0)
					{
						cm.RemoveAt(cm.Position);
						return true;
					}
				}
			}
			return false;
		}
		[Description("Begin editing the current record")]
		public void BeginEditCurrentRecord()
		{
			if (!this.ReadOnly)
			{
				DataTable tbl = CurrentDataTable;
				if (tbl != null)
				{
					BindingManagerBase cm = getCurrencyManager();
					if (cm != null)
					{
						if (cm.Position >= 0 && cm.Position < tbl.Rows.Count)
						{
							tbl.Rows[cm.Position].BeginEdit();
						}
					}
				}
			}
		}
		[Description("Cancel editing the current record")]
		public bool CancelEditCurrentRecord()
		{
			if (_data != null && _data.Tables.Count > 0)
			{
				if (!this.ReadOnly)
				{
					BindingManagerBase cm = getCurrencyManager();
					if (cm != null)
					{
						cm.CancelCurrentEdit();
						return true;
					}
				}
			}
			return false;
		}
		[Description("End editing the current record")]
		public void EndEditCurrentRecord()
		{
			if (_data != null && _data.Tables.Count > 0)
			{
				if (!this.ReadOnly)
				{
					BindingManagerBase cm = getCurrencyManager();
					if (cm != null)
					{
						cm.EndCurrentEdit();
					}
				}
			}
		}
		[Description("Save changed data back to the database")]
		public bool Update()
		{
			_lastError = string.Empty;
			if (_forReadOnly || _readOnlyResult)
			{
				_lastError = string.Format(CultureInfo.InvariantCulture, "Error calling EasyQuery.Update. The table {0} is read-only.", TableName);
				FormLog.LogMessage(ShowErrorMessage, "{0} - Error calling EasyQuery.Update. The data is read-only", TableName);
			}
			else
			{
				if (_sourceType == EnumDataSourceType.Array)
				{
					DataTable tbl = this.DataTable;
					Array[] data = new Array[tbl.Columns.Count];
					for (int i = 0; i < tbl.Rows.Count; i++)
					{
						Array a = data[i];
						for (int c = 0; c < data.Length; c++)
						{
							a.SetValue(tbl.Rows[i].ItemArray[c], c);
						}
					}
					_sourceData = data;
					return true;
				}
				DatabaseConnection.Log("Update {0}", TableName);
				if (_data == null)
				{
					DatabaseConnection.Log("no data");
				}
				else
				{
					try
					{
						if (BeforeUpdate != null)
						{
							DataTable tbl = Tables[TableName];
							int nRowCount = 0;
							if (tbl != null)
							{
								nRowCount = tbl.Rows.Count;
							}
							EventArgsDataFill e0 = new EventArgsDataFill(nRowCount);
							BeforeUpdate(this, e0);
						}
						_updating = true;
						EndEditCurrentRecord();
						DataSet dsChanges = _data.GetChanges();
						if (dsChanges == null)
						{
							DatabaseConnection.Log("no changes");
						}
						else
						{
							if (DatabaseConnection.LoggingEnabled)
							{
								if (dsChanges.Tables[TableName] != null)
								{
									DatabaseConnection.Log("Rows:{0}", dsChanges.Tables[TableName].Rows.Count);
									for (int r = 0; r < dsChanges.Tables[TableName].Rows.Count; r++)
									{
										if (dsChanges.Tables[TableName].Rows[r] != null && dsChanges.Tables[TableName].Rows[r].ItemArray != null)
										{
											StringBuilder sb = new StringBuilder("row");
											sb.Append(r.ToString());
											sb.Append(":");
											for (int c = 0; c < dsChanges.Tables[TableName].Rows[r].ItemArray.Length; c++)
											{
												if (dsChanges.Tables[TableName].Rows[r].ItemArray[c] == null)
													sb.Append("null");
												else if (dsChanges.Tables[TableName].Rows[r].ItemArray[c] == DBNull.Value)
													sb.Append("DBNull");
												else
												{
													if (typeof(byte[]).Equals(dsChanges.Tables[TableName].Columns[c].DataType))
													{
														sb.Append("byte[]");
													}
													else
													{
														sb.Append(dsChanges.Tables[TableName].Rows[r].ItemArray[c]);
													}
												}
												sb.Append(",");
											}
											connect.Log(sb.ToString());
										}
									}
								}
								else
								{
									DatabaseConnection.Log("Table {0} not found in changes", TableName);
								}
							}
							bool bAdded = false;
							da.Update(dsChanges, TableName);
							_data.Merge(dsChanges);
							_data.AcceptChanges();
							if (_reloadAfterUpdate)
							{
								DatabaseConnection.Log("Reload after update");
								if (IsSQLSever)
								{
									DataTable tbl = CurrentDataTable;
									if (tbl != null)
									{
										tbl.Rows.Clear();
									}
									da.Fill(_data, TableName);
									checkEmptyRecords();
								}
								else
								{
									EPField f = Fields.GetIdentityField();
									if (f != null)
									{
										if (_newRows != null)
										{
											DataTable tbl = CurrentDataTable;
											if (tbl != null)
											{
												foreach (KeyValuePair<int, DataRow> kv in _newRows)
												{
													if (kv.Value.RowState != DataRowState.Detached)
													{
														tbl.Rows.Remove(kv.Value);
													}
												}
											}
											_newRows = null;
											bAdded = true;
										}
									}
									if (_hasCalculatedFields || bAdded)
									{
										for (int i = 0; i < DataTable.Columns.Count; i++)
										{
											DataTable.Columns[i].Expression = null;
										}
										DataTable.Rows.Clear();
										da.Fill(_data, TableName);
										checkEmptyRecords();
										FieldList fl = Fields;
										for (int i = 0; i < DataTable.Columns.Count; i++)
										{
											if (!string.IsNullOrEmpty(fl[i].FieldExpression))
											{
												DataTable.Columns[i].Expression = fl[i].FieldExpression;
											}
										}
									}
								}
							}
							else
							{
							}
							if (AfterQuery != null)
							{
								DataTable tbl = Tables[TableName];
								int nRowCount = 0;
								if (tbl != null)
								{
									nRowCount = tbl.Rows.Count;
								}
								EventArgsDataFill e0 = new EventArgsDataFill(nRowCount);
								AfterQuery(this, e0);
							}
							if (AfterUpdate != null)
							{
								DataTable tbl = Tables[TableName];
								int nRowCount = 0;
								if (tbl != null)
								{
									nRowCount = tbl.Rows.Count;
								}
								EventArgsDataFill e0 = new EventArgsDataFill(nRowCount);
								AfterUpdate(this, e0);
							}
						}
						return true;
					}
					catch (Exception err)
					{
						_lastError = string.Format(CultureInfo.InvariantCulture, "Error updating [{0}]. {1}", UpdatableTableName, err.Message);
						FormLog.NotifyException(ShowErrorMessage, err, "Error Updating the database");
					}
					finally
					{
						_updating = false;
					}
				}
			}
			return false;
		}
		[Description("Compose the query commands and query the database for data")]
		public bool Query()
		{
			_isQuerying = true;
			bQueryOK = false;
			_loaded = true;//this is the last initialization call
			LogMessage("{0} - Calling Query()", TableName);
			try
			{
				if (initializeFields())
				{
					if (initializeCommands())
					{
						bQueryOK = true;
						_sourceType = EnumDataSourceType.Database;
						RemoveTable(TableName);
						FetchData();
						_isQuerying = false;
						return true;
					}
					else
					{
						LogMessage("{0} - initializeCommands failed", TableName);
					}
				}
				else
				{
					LogMessage("{0} - initializeFields failed", TableName);
				}
			}
			finally
			{
				_isQuerying = false;
			}
			return false;
		}
		[Browsable(false)]
		[NotForProgramming]
		public DbParameterCollection GetCreateNewRecordParameters()
		{
			if (cmdInsert == null)
			{
				if (initializeFields())
				{
					initializeCommands();
				}
			}
			if (cmdInsert != null)
			{
				return cmdInsert.Parameters;
			}
			return null;
		}
		[Browsable(false)]
		[NotForProgramming]
		public bool UseIdentity()
		{
			if (cmdInsert == null)
			{
				if (initializeFields())
				{
					initializeCommands();
				}
			}
			return !(rowIDFields == null || rowIDFields.Count == 0);
		}
		[Browsable(false)]
		[NotForProgramming]
		public string GetCreateNewRecordQuerySQL()
		{
			this.PrepareQuery();
			if (rowIDFields != null && rowIDFields.Count > 0)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(this.SELECT);
				sb.Append(" FROM ");
				sb.Append(this.From);
				sb.Append(" WHERE ");
				sb.Append(string.Format(CultureInfo.InvariantCulture,
							"{0}{1}{2} = ?", this.NameDelimiterBegin, rowIDFields[0].Name, NameDelimiterEnd));
				return sb.ToString();
			}
			return string.Empty;
		}
		[Browsable(false)]
		[NotForProgramming]
		public string GetCreateNewRecordSQL()
		{
			if (cmdInsert == null)
			{
				if (initializeFields())
				{
					initializeCommands();
				}
			}
			if (cmdInsert != null)
			{
				string sql = cmdInsert.CommandText;
				if (!string.IsNullOrEmpty(sql))
				{
					foreach (DbParameter p in cmdInsert.Parameters)
					{
						sql = sql.Replace(p.ParameterName, "?");
					}
				}
				return sql;
			}
			return string.Empty;
		}
		public void CreateRandomNewRecord()
		{
			if (cmdInsert == null)
			{
				if (initializeFields())
				{
					initializeCommands();
				}
			}
			if (cmdInsert != null)
			{
				object[] vs = new object[cmdInsert.Parameters.Count];
				for (int i = 0; i < cmdInsert.Parameters.Count; i++)
				{
					vs[i] = VPLUtil.CreateRandomValue(EPField.ToSystemType(cmdInsert.Parameters[i].DbType));
				}
				CreateNewRecord(vs);
			}
		}
		[Description("Create a new record. The query must include the identity field for the table. On finishing ths method the new record is already saved to the database so that relational links can be made to the new record.")]
		public void CreateNewRecord(params object[] values)
		{
			if (cmdInsert == null)
			{
				if (initializeFields())
				{
					initializeCommands();
				}
			}
			if (cmdInsert != null)
			{
				if (rowIDFields == null || rowIDFields.Count == 0)
				{
					throw new ExceptionLimnorDatabase("Cannot execute CreateNewRecord because identity field is not included");
				}
				DbCommand cmd = cmdInsert.Connection.CreateCommand();
				cmd.CommandText = cmdInsert.CommandText;

				if (values != null && values.Length > 0)
				{
					for (int i = 0; i < cmdInsert.Parameters.Count && i < values.Length; i++)
					{
						DbParameter p = cmd.CreateParameter();
						p.DbType = cmdInsert.Parameters[i].DbType;
						p.ParameterName = cmdInsert.Parameters[i].ParameterName;
						if (values[i] == null || values[i] == DBNull.Value)
						{
							Type t = EPField.ToSystemType(p.DbType);
							if (t.Equals(typeof(string)))
							{
								p.Value = string.Empty;
							}
							else
							{
								p.Value = VPLUtil.GetDefaultValue(t);
							}
						}
						else
						{
							if (IsJet && values[i] is DateTime)
							{
								DateTime dt = (DateTime)values[i];
								p.Value = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
							}
							else
							{
								Type pType = EPField.ToSystemType(p.DbType);
								Type vType = values[i].GetType();
								if (pType.IsAssignableFrom(vType))
								{
									p.Value = values[i];
								}
								else
								{
									TypeConverter tc = TypeDescriptor.GetConverter(values[i]);
									if (tc != null && tc.CanConvertTo(pType))
									{
										p.Value = tc.ConvertTo(values[i], pType);
									}
									else
									{
										FormLog.ShowMessage("Cannot convert value of type {0} to parameter {1} of type {2} [{3}]", vType.AssemblyQualifiedName, p.ParameterName, p.DbType, pType.AssemblyQualifiedName);
										p.Value = values[i];
									}
								}
							}
						}
						cmd.Parameters.Add(p);
					}
				}
				bool bClosed = (cmd.Connection.State == ConnectionState.Closed);
				try
				{
					if (bClosed)
					{
						cmd.Connection.Open();
					}
					cmd.ExecuteNonQuery();
					EPField idField = this.Fields.GetIdentityField();
					if (idField != null)
					{
						if (this.IsSQLSever)
						{
							cmd.CommandText = "SELECT SCOPE_IDENTITY();";
						}
						else if (this.IsJet)
						{
							cmd.CommandText = "SELECT @@IDENTITY;";
						}
						else if (this.IsMySql)
						{
							cmd.CommandText = "SELECT LAST_INSERT_ID();";
						}
						else
						{
							if (string.IsNullOrEmpty(CommandToGetAutoNumber))
							{
								cmd.CommandText = string.Format(CultureInfo.InvariantCulture,
									"SELECT Max({0}) FROM {1}{2}{3}", rowIDFields[0].Name, NameDelimiterBegin, this.TableToUpdate, NameDelimiterEnd);
							}
							else
							{
								cmd.CommandText = CommandToGetAutoNumber;
							}
						}
						DbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
						if (dr.Read())
						{
							if (dr[0] == DBNull.Value)
								_identity = 0;
							else
								_identity = Convert.ToInt64(dr[0]);
							dr.Close();
						}
						else
						{
							dr.Close();
							throw new ExceptionLimnorDatabase("Identity not found.");
						}
					}
					Query();
					if (idField != null && _identity != 0)
					{
						DataTable tbl = CurrentDataTable;
						if (tbl != null)
						{
							int n = -1;
							for (int i = 0; i < tbl.Columns.Count; i++)
							{
								if (string.Compare(idField.Name, tbl.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
								{
									n = i;
									break;
								}
							}
							if (n >= 0)
							{
								BindingManagerBase cm = getCurrencyManager();
								if (cm != null)
								{
									for (int i = 0; i < tbl.Rows.Count; i++)
									{
										if (tbl.Rows[i].RowState != DataRowState.Deleted)
										{
											object v = tbl.Rows[i][n];
											if (v != null && v != System.DBNull.Value)
											{
												if (_identity == Convert.ToInt64(v))
												{
													cm.Position = i;
													break;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				catch (Exception err)
				{
					FormLog.LogMessage(ShowErrorMessage, FormLog.FormExceptionString(err));
				}
				finally
				{
					if (bClosed)
					{
						cmd.Connection.Close();
					}
				}
			}
		}
		[Description("Execute database query with the parameter values")]
		public void QueryWithParameterValues(params object[] values)
		{
			if (values != null && values.Length > 0)
			{
				if (_queryChanged)
				{
					if (initializeFields())
					{
						initializeCommands();
					}
				}
				ParameterList pl = Parameters;
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
							if (this.IsJet)
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
		[Description("Get value of the column specified by fieldName from the current record.")]
		public object GetColumnValue(string fieldName)
		{
			if (Fields != null)
			{
				EPField f = this.Fields[fieldName];
				if (f != null)
				{
					return f.Value;
				}
			}
			return null;
		}
		[Description("Get value of the column specified by fieldName from the record specified by rowNumber.")]
		public object GetColumnValueEx(int rowNumber, string fieldName)
		{
			if (DataTable != null)
			{
				if (DataTable.Rows.Count > rowNumber)
				{
					return DataTable.Rows[rowNumber][fieldName];
				}
			}
			return null;
		}
		[Description("Calculate summary of the column specified by fieldName.")]
		public double GetColumnSum(string fieldName)
		{
			double ret = 0;
			DataTable tbl = CurrentDataTable;
			if (tbl != null)
			{
				int n = -1;
				for (int i = 0; i < tbl.Columns.Count; i++)
				{
					if (string.Compare(fieldName, tbl.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
					{
						n = i;
						break;
					}
				}
				if (n >= 0)
				{
					if (EPField.IsNumber(tbl.Columns[n].DataType))
					{
						for (int i = 0; i < tbl.Rows.Count; i++)
						{
							if (tbl.Rows[i].RowState != DataRowState.Deleted)
							{
								object v = tbl.Rows[i][n];
								if (v != null && v != System.DBNull.Value)
								{
									ret += Convert.ToDouble(v);
								}
							}
						}
					}
				}
			}
			return ret;
		}
		[Description("Set credential for accessing the database.")]
		public void SetCredential(string user, string userPassword, string databasePassword)
		{
			if (connect != null)
			{
				connect.ConnectionObject.SetCredential(user, userPassword, databasePassword);
			}
		}
		[Description("Launch a dialogue box to set credential for accessing the database.")]
		public bool SetCredentialByUI(Form caller)
		{
			if (connect != null)
			{
				if (connect.SetCredentialByUI(caller))
				{
					Query();
					return true;
				}
			}
			return false;
		}
		[Description("Set connection string for accessing the database. It should be called at the event of BeforePrepareCommands and before event BeforeQuery.")]
		public void SetConnectionString(string connectionString)
		{
			if (connect == null)
			{
				connect = new ConnectionItem();
			}
			connect.ConnectionString = connectionString;
		}
		[Description("Use a dialogue box to select database connection type and other parameters.")]
		public void SelectConnection(Form dialogParent)
		{
			Connection cnn;
			if (connect != null)
			{
				cnn = (Connection)connect.ConnectionObject.Clone();
			}
			else
			{
				cnn = new Connection();
			}
			DlgConnection dlg = cnn.CreateDataDialog();
			if (dlg.ShowDialog(dialogParent) == DialogResult.OK)
			{
				connect = new ConnectionItem(cnn);
			}
		}
		[Description("Use a dialogue box to make database query.")]
		public void SelectQuery(Form dialogParent)
		{
			if (connect == null)
			{
				connect = new ConnectionItem();
			}
			if (!connect.ConnectionObject.TestConnection(dialogParent, false))
			{
				SelectConnection(dialogParent);
				if (!connect.ConnectionObject.TestConnection(dialogParent, false))
				{
					return;
				}
			}
			QueryParser qryP = new QueryParser();
			if (qryP.BuildQuery(this, dialogParent))
			{
				CopyFrom(qryP.query);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="parameterName"></param>
		/// <param name="value"></param>
		[Description("Set query parameter value. ")]
		public void SetParameterValue(string parameterName, object value)
		{
			if (_queryChanged)
			{
				if (initializeFields())
				{
					initializeCommands();
				}
			}
			EPField p = Parameters[parameterName];
			if (p != null)
			{
				if (value == null || value == DBNull.Value)
				{
					p.SetValue(DBNull.Value);
				}
				else
				{
					Type t = value.GetType();
					p.OleDbType = EPField.ToOleDBType(t);
					LogMessage("Set parameter type of {0} to {1}. IsOleDb:{2}, value:{3}, value type:{4}", p.Name, p.OleDbType, IsOleDb, value, t.FullName);
					if (IsOleDb)
					{
						if (value != null)
						{
							if (typeof(DateTime).Equals(t))
							{
								DateTime dt = (DateTime)value;
								dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
								p.SetValue(dt);
								return;
							}
						}
					}
					p.SetValue(value);
				}
			}
		}
		[Description("Query the database for data. It may be called after setting parameter values. ")]
		public void FetchData()
		{
			LogMessage("{0} - FetchData. QueryOK:{1}", TableName, bQueryOK);
			if (bQueryOK)
			{
				try
				{
					if (da != null)
					{
						if (BeforeQuery != null)
						{
							BeforeQuery(this, EventArgs.Empty);
						}
						if (_paramMap != null && _paramMap.Count > 0 && _paramMap.Count == cmdSelection.Parameters.Count)
						{
							for (int i = 0; i < _paramMap.Count; i++)
							{
								LogMessage("Parameter {0}: name:{1}, type:{2}, value:{3}, value type:{4}, command parameter type:{5}", i, _paramMap[i].Name, _paramMap[i].OleDbType, _paramMap[i].Value, (_paramMap[i].Value == null ? "null" : _paramMap[i].Value.GetType().Name), cmdSelection.Parameters[i].DbType);
								if (_paramMap[i].Value == null)
								{
									cmdSelection.Parameters[i].Value = EPField.DefaultFieldValue(_paramMap[i].OleDbType);
								}
								else
								{
									if (_paramMap[i].Value is string)
									{
										string s = (string)(_paramMap[i].Value);
										if (string.IsNullOrEmpty(s))
										{
											cmdSelection.Parameters[i].Value = EPField.DefaultFieldValue(_paramMap[i].OleDbType);
										}
										else
										{
											cmdSelection.Parameters[i].Value = _paramMap[i].Value;
										}
									}
									else
									{
										cmdSelection.Parameters[i].Value = _paramMap[i].Value;
									}
								}
							}
						}
						else
						{
							for (int i = 0; i < cmdSelection.Parameters.Count; i++)
							{
								string pn = cmdSelection.Parameters[i].ParameterName;
								if (pn.StartsWith("?", StringComparison.Ordinal))
								{
									pn = string.Format(CultureInfo.InvariantCulture, "@{0}", pn.Substring(1));
								}
								EPField p = _parameters[pn];
								if (p != null)
								{
									if (p.Value == null)
									{
										cmdSelection.Parameters[i].Value = EPField.DefaultFieldValue(p.OleDbType);
									}
									else
									{
										if (p.Value is string)
										{
											string s = (string)(p.Value);
											if (string.IsNullOrEmpty(s))
											{
												cmdSelection.Parameters[i].Value = EPField.DefaultFieldValue(p.OleDbType);
											}
											else
											{
												cmdSelection.Parameters[i].Value = p.Value;
											}
										}
										else
										{
											cmdSelection.Parameters[i].Value = p.Value;
										}
									}
								}
							}
						}
						if (connect.LoggingEnabled)
						{
							connect.Log("Fetch data for {0} using query {1}. Parameters:{2}", TableName, cmdSelection.CommandText, cmdSelection.Parameters.Count);
							if (cmdSelection.Parameters.Count > 0)
							{
								for (int i = 0; i < cmdSelection.Parameters.Count; i++)
								{
									connect.Log("param{0}:{1},{2},{3}, Value:{4}", i, cmdSelection.Parameters[i].ParameterName, cmdSelection.Parameters[i].DbType, cmdSelection.Parameters[i].Size, cmdSelection.Parameters[i].Value);
								}
							}
						}
						else
						{
							LogMessage("Fetch data for {0} using query {1}. Parameters:{2}", TableName, cmdSelection.CommandText, cmdSelection.Parameters.Count);
							if (cmdSelection.Parameters.Count > 0)
							{
								for (int i = 0; i < cmdSelection.Parameters.Count; i++)
								{
									LogMessage("param{0}:{1},{2},{3}, Value:{4}", i, cmdSelection.Parameters[i].ParameterName, cmdSelection.Parameters[i].DbType, cmdSelection.Parameters[i].Size, cmdSelection.Parameters[i].Value);
								}
							}
						}
						bool bOpen = cmdSelection.Connection.State != ConnectionState.Closed;
						if (bOpen)
						{
							cmdSelection.Connection.Close();
						}

						//find reference
						bool bCreate = (_data == null);
						if (bCreate)
						{
							_data = new DataSet(Guid.NewGuid().ToString());
						}
						_changeDataSet = true;
						da.Fill(_data, TableName);
						//
						LogMessage("{0} - EasyQuery.FetchData: filled data", TableName);
						//
						checkEmptyRecords();
						//adjust primary key
						DataTable tbl = CurrentDataTable;
						FieldList fl = Fields;
						if (tbl != null)
						{
							if (_readOnlyResult)
							{
								for (int i = 0; i < tbl.Columns.Count; i++)
								{
									tbl.Columns[i].ReadOnly = true;
								}
							}
							else
							{
								bool hasIdentityField = false;
								for (int i = 0; i < fl.Count; i++)
								{
									if (fl[i].IsIdentity)
									{
										fl[i].Indexed = true;
										hasIdentityField = true;
										tbl.Columns[i].ReadOnly = true;
										if (!_readOnlyResult)
										{
											if (typeof(UInt16).Equals(tbl.Columns[i].DataType))
											{
												tbl.Columns[i].DataType = typeof(Int16);
											}
											else if (typeof(UInt32).Equals(tbl.Columns[i].DataType))
											{
												tbl.Columns[i].DataType = typeof(Int32);
											}
											else if (typeof(UInt64).Equals(tbl.Columns[i].DataType))
											{
												tbl.Columns[i].DataType = typeof(Int64);
											}
											else if (!typeof(Int16).Equals(tbl.Columns[i].DataType)
												&& !typeof(Int32).Equals(tbl.Columns[i].DataType)
												&& !typeof(Int64).Equals(tbl.Columns[i].DataType))
											{
												tbl.Columns[i].DataType = typeof(Int32);
											}
											tbl.Columns[i].AutoIncrement = true;
											tbl.Columns[i].AutoIncrementSeed = short.MinValue;
											if (tbl.Columns[i].AutoIncrementStep <= 0)
											{
												tbl.Columns[i].AutoIncrementStep = 1;
											}
										}
										break;
									}
								}
								if (hasIdentityField)
								{
									for (int i = 0; i < fl.Count; i++)
									{
										if (!fl[i].IsIdentity)
										{
											fl[i].Indexed = false;
										}
									}
								}
								if (tbl.PrimaryKey == null || tbl.PrimaryKey.Length == 0)
								{
									List<DataColumn> clst = new List<DataColumn>();
									for (int i = 0; i < tbl.Columns.Count; i++)
									{
										EPField fld = fl[tbl.Columns[i].ColumnName];
										if (fld != null)
										{
											if (string.CompareOrdinal(fld.FromTableName, this.UpdatableTableName) == 0)
											{
												if (fld.Indexed)
												{
													clst.Add(tbl.Columns[i]);
												}
												else
												{
													tbl.Columns[i].ReadOnly = this.ReadOnly;
												}
											}
											else
											{
												tbl.Columns[i].ReadOnly = true;
											}
										}
									}
									if (!_readOnlyResult && clst.Count > 0)
									{
										try
										{
											tbl.PrimaryKey = clst.ToArray();
										}
										catch
										{
										}
									}
								}
								tbl.TableNewRow -= _miNewRow;
								tbl.RowDeleted -= _miRowDeleting;
								tbl.RowDeleting -= _miRowDeleted;
								//
								tbl.TableNewRow += _miNewRow;
								tbl.RowDeleted += _miRowDeleting;
								tbl.RowDeleting += _miRowDeleted;
							}
						}
						for (int i = 0; i < fl.Count; i++)
						{
							if (!string.IsNullOrEmpty(fl[i].FieldExpression))
							{
								try
								{
									tbl.Columns[i].Expression = fl[i].FieldExpression;
								}
								catch (Exception err)
								{
									MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "Error setting field expression [{0}] to field [{1}]. {2}", fl[i].FieldExpression, fl[i].Name, err.Message));
								}
							}
						}
						if (DataFilled != null)
						{
							DataFilled(this, EventArgs.Empty);
						}
						_currentRowIndex = -1;
					}
					else
					{
						LogMessage("{0} - Error calling FetchData: data adapter is null", TableName);
					}
					if (AfterQuery != null)
					{
						AfterQuery(this, EventArgs.Empty);
					}
				}
				catch (Exception err)
				{
					_lastError = ExceptionLimnorDatabase.FormExceptionText(err, "{0} - FetchData failed. Command:{1}", TableName, formErrorMessage());
					FormLog.NotifyException(ShowErrorMessage, err, _lastError);
					bQueryOK = false;
					if (LastErrorMessageChanged != null)
					{
						LastErrorMessageChanged(this, EventArgs.Empty);
					}
				}
			}
		}
		[Description("Return true if the field value for the current record is not null")]
		public bool IsFieldValueNotNull(string fieldName)
		{
			EPField fld = null;
			if (_fields != null)
			{
				fld = _fields[fieldName];
				if (fld != null)
				{
					return fld.IsNotNull;
				}
			}
			return false;
		}
		[Description("Return true if the field value for the current record is not null and not an empty string")]
		public bool IsFieldValueNotNullOrEmpty(string fieldName)
		{
			EPField fld = null;
			if (_fields != null)
			{
				fld = _fields[fieldName];
				if (fld != null)
				{
					return fld.IsNotNullOrEmpty;
				}
			}
			return false;
		}
		[Description("Return true if the field value for the current record is null")]
		public bool IsFieldValueNull(string fieldName)
		{
			EPField fld = null;
			if (_fields != null)
			{
				fld = _fields[fieldName];
				if (fld != null)
				{
					return fld.IsNull;
				}
			}
			return true;
		}
		[Description("Return true if the field value for the current record is null or an empty string")]
		public bool IsFieldValueNullOrEmpty(string fieldName)
		{
			EPField fld = null;
			if (_fields != null)
			{
				fld = _fields[fieldName];
				if (fld != null)
				{
					return fld.IsNullOrEmpty;
				}
			}
			return true;
		}
		[Description("Set field value for the record specified by rowNumber")]
		public void SetFieldValueEx(int rowNumber, string fieldName, object value)
		{
			if (_isQuerying)
				return;
			if (DataTable != null)
			{
				if (DataTable.Rows.Count > rowNumber)
				{
					Type fType = typeof(object);
					int n = -1;
					for (int i = 0; i < DataTable.Columns.Count; i++)
					{
						if (string.Compare(fieldName, DataTable.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							fType = DataTable.Columns[i].DataType;
							if (typeof(DateTime).Equals(DataTable.Columns[i].DataType))
							{
								DateTime dt = Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture);
								value = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
							}
							n = i;
							break;
						}
					}
					if (n >= 0 && n < DataTable.Rows[rowNumber].ItemArray.Length)
					{
						DataTable.Rows[rowNumber][n] = value;
					}
				}
			}
		}
		public void SetRandomValuesToFields()
		{
			if (_isQuerying)
				return;
			if (_fields != null)
			{
				for (int i = 0; i < _fields.Count; i++)
				{
					if (!_fields[i].ReadOnly)
					{
						SetRandomFieldValue(_fields[i].Name);
					}
				}
			}
		}
		public void SetRandomFieldValue(string fieldName)
		{
			if (_isQuerying)
				return;
			EPField fld = null;
			if (_fields != null)
			{
				fld = _fields[fieldName];
				if (fld != null)
				{
					object v = VPLUtil.CreateRandomValue(fld.SystemType);
					SetFieldValue(fieldName, v);
				}
			}
		}
		[Description("Set field value for the current record")]
		public void SetFieldValue(string fieldName, object value)
		{
			if (_isQuerying)
				return;
			EPField fld = null;
			if (_fields != null)
			{
				fld = _fields[fieldName];
				if (fld != null)
				{
					fld.SetValue(value);
				}
			}
			BindingManagerBase cm = getCurrencyManager();
			if (cm != null)
			{
				DataRowView drv = cm.Current as DataRowView;
				if (drv != null && drv.Row != null && drv.Row.Table != null && drv.Row.ItemArray != null)
				{
					Type fType = typeof(object);
					int n = -1;
					for (int i = 0; i < drv.Row.Table.Columns.Count; i++)
					{
						if (string.Compare(fieldName, drv.Row.Table.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							fType = drv.Row.Table.Columns[i].DataType;
							if (typeof(DateTime).Equals(drv.Row.Table.Columns[i].DataType))
							{
								DateTime dt = Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture);
								value = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
							}
							n = i;
							break;
						}
					}
					if (n >= 0 && n < drv.Row.ItemArray.Length)
					{
						bool b = drv.IsEdit;
						if (!b)
						{
							drv.BeginEdit();
						}
						if (drv.Row[n] != value)
						{
							try
							{
								if (fld != null)
								{
									drv.Row[n] = ValueConvertor.ConvertByOleDbType(value, fld.OleDbType);
								}
								else
								{
									bool bc;
									drv.Row[n] = VPLUtil.ConvertObject(value, fType, out bc);
									if (!bc)
									{
										DatabaseConnection.Log("Cannot convert value of type {0} to field {1} of type {2} [{3}]", value.GetType(), fieldName, fType, value);
									}
								}
							}
							catch
							{
							}
						}
						if (!b)
						{
							drv.EndEdit();
						}
					}
				}
			}
		}
		[Description("Go through each row and test the condition. The first row making the condition match becomes the current row.")]
		public bool Search(bool condition)
		{
			return false;
		}
		Dictionary<int, DataRow> _newRows;
		void EasyQuery_TableNewRow(object sender, DataTableNewRowEventArgs e)
		{
			EPField f = Fields.GetIdentityField();
			if (f != null)
			{
				int id = VPLUtil.ObjectToInt(e.Row[f.Name]);
				if (_newRows == null)
				{
					_newRows = new Dictionary<int, DataRow>();
				}
				if (_newRows.ContainsKey(id))
				{
					_newRows[id] = e.Row;
				}
				else
				{
					_newRows.Add(id, e.Row);
				}
				if (CreatedNewRowID != null)
				{
					CreatedNewRowID(this, new EventArgsNewRowID(id));
				}
			}
		}
		bool _updating;
		void checkEmptyRecords()
		{
			DataTable tbl = CurrentDataTable;
			if (tbl != null)
			{
				if (EmptyRecordHandler != null)
				{
					bool bEmpty = true;
					for (int i = 0; i < tbl.Rows.Count; i++)
					{
						if (tbl.Rows[i].RowState != DataRowState.Deleted)
						{
							bEmpty = false;
							break;
						}
					}
					if (bEmpty)
					{
						EmptyRecordHandler(this, EventArgs.Empty);
					}
				}
			}
		}
		void EasyQuery_RowDeleting(object sender, DataRowChangeEventArgs e)
		{
			if (!_updating)
			{
				if (BeforeDeleteRecordHandler != null)
				{
					BeforeDeleteRecordHandler(sender, e);
				}
			}
		}
		void EasyQuery_RowDeleted(object sender, DataRowChangeEventArgs e)
		{
			if (!_updating)
			{
				//checkEmptyRecords();
				if (AfterDeleteRecordHandler != null)
				{
					AfterDeleteRecordHandler(this, e);
				}
			}
		}
		#endregion

		#region Logging support
		private static string _logfilepath;
		private static TraceLogClass log;
		[Description("Set the full file path for the log file. ")]
		public static void SetLogFilePath(string logFileName)
		{
			_logfilepath = logFileName;
			log = new TraceLogClass("Database", _logfilepath);
			log.EnableWriteTime(true);
		}
		[Description("log message into the log file")]
		public void LogMessage(string message, params object[] values)
		{
			if (log != null)
			{
				log.LogMessage(message, values);
			}
			if (ProcessMessages.Count > 1000)
			{
				ProcessMessages.Clear();
			}
			if (values != null && values.Length > 0)
			{
				ProcessMessages.Add(string.Format(CultureInfo.InvariantCulture, message, values));
			}
			else
			{
				ProcessMessages.Add(message);
			}
		}
		public static void LogMessage2(string message, params object[] values)
		{
			if (log != null)
			{
				log.LogMessage(message, values);
			}
		}
		[Description("log message into the log file")]
		public static void LogMessage(Exception e, string message, params object[] values)
		{
			if (log != null)
			{
				if (e != null)
				{
					StringBuilder sb = new StringBuilder(ExceptionLimnorDatabase.FormExceptionText(e));
					sb.AppendFormat(message, values);
					log.LogMessage(sb.ToString());
				}
				else
				{
					log.LogMessage(message, values);
				}
			}
			else
			{
				if (e != null)
				{
					throw new ExceptionLimnorDatabase(e, message, values);
				}
			}
		}
		#endregion

		#region backwards compatibility support (EPTable)
		[Browsable(false)]
		public void SetFieldValues(DataRow dw)
		{
			if (rowIDFields != null)
			{
				bool bOK = (dw != null && (dw.RowState != DataRowState.Deleted));
				if (bOK)
				{
					for (int i = 0; i < rowIDFields.Count; i++)
					{
						if (!string.IsNullOrEmpty(rowIDFields[i].Name))
						{
							rowIDFields[i].SetValue(dw[rowIDFields[i].Name]);
						}
					}
				}
				else
				{
					for (int i = 0; i < rowIDFields.Count; i++)
					{
						rowIDFields[i].SetValue(null);
					}
				}
			}
			if (_fields != null)
			{
				int i;
				if (dw == null)
				{
					for (i = 0; i < _fields.Count; i++)
					{
						_fields[i].SetValue(null);
					}
				}
				else if (dw.RowState != DataRowState.Deleted && dw.ItemArray != null)
				{
					for (i = 0; i < _fields.Count && i < dw.ItemArray.Length; i++)
					{
						_fields[i].SetValue(dw[i]);
					}
				}
			}
		}

		#endregion

		#region Non-Browsable properties
		[Browsable(false)]
		public bool QueryReady
		{
			get
			{
				return bQueryOK;
			}
		}
		[Browsable(false)]
		public DataTableCollection Tables
		{
			get
			{
				if (_data != null)
				{
					return _data.Tables;
				}
				return null;
			}
		}
		[Browsable(false)]
		public int TableCount
		{
			get
			{
				if (_data != null)
				{
					return _data.Tables.Count;
				}
				return 0;
			}
		}
		[Browsable(false)]
		public string SqlQuery
		{
			get
			{
				if (!string.IsNullOrEmpty(_rawSql))
				{
					return _rawSql;
				}
				return SQL.ToString();
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					SQL = new SQLStatement(value);
				}
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
				if (connect != null && connect.ConnectionObject != null)
				{
					if (!string.IsNullOrEmpty(connect.ConnectionObject.ConnectionString))
					{
						return connect.ConnectionObject.ConnectionString;
					}
				}
				return _defConnectionString;
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					string s = value.Trim();
					if (s.Length > 0)
					{
						_defConnectionString = value;
						setDefaultConnection();
					}
				}
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
				if (connect != null && connect.ConnectionObject != null)
				{
					if (connect.ConnectionObject.DatabaseType != null)
					{
						return connect.ConnectionObject.DatabaseType;
					}
				}
				return _defConnectionType;
			}
			set
			{
				if (value != null)
				{
					_defConnectionType = value;
					setDefaultConnection();
				}
			}
		}
		/// <summary>
		/// If the query has more than one table, this value is "".
		/// </summary>
		[Browsable(false)]
		public string UpdatableTableName { get; set; }

		[Browsable(false)]
		public IDbDataAdapter Adapter
		{
			get
			{
				return da;
			}
		}
		[Browsable(false)]
		internal DataTable CurrentDataTable
		{
			get
			{
				if (_data != null)
				{
					return _data.Tables[TableName];
				}
				return null;
			}
		}
		[Browsable(false)]
		public string DatabaseDisplayText
		{
			get
			{
				if (connect != null)
				{
					return connect.ConnectionObject.DatabaseDisplayText;
				}
				return string.Empty;
			}
		}

		[Browsable(false)]
		public string ConnectionName
		{
			get
			{
				if (connect != null)
				{
					return connect.Name;
				}
				return "";
			}
		}

		[Browsable(false)]
		public Guid ConnectionID
		{
			get
			{
				if (connect != null)
					return connect.ConnectionObject.ConnectionGuid;
				return Guid.Empty;
			}
			set
			{
				if (value != Guid.Empty)
				{
					connect = ConnectionItem.LoadConnection(value, true, false);
					setDefaultConnection();
				}
			}
		}
		[Browsable(false)]
		public string NameDelimiterBegin
		{
			get
			{
				if (connect != null)
					return DatabaseEditUtil.SepBegin(connect.NameDelimiterStyle);
				return "";
			}
		}
		[Browsable(false)]
		public string NameDelimiterEnd
		{
			get
			{
				if (connect != null)
					return DatabaseEditUtil.SepEnd(connect.NameDelimiterStyle);
				return "";
			}
		}
		[Browsable(false)]
		public EnumParameterStyle ParameterStyle
		{
			get
			{
				if (connect != null)
					return connect.ParameterStyle;
				return EnumParameterStyle.QuestionMark;
			}
		}
		#endregion

		#region static non-browsable methods
		public static string CodeBaseFolder
		{
			get
			{
				const string f0 = "file://";
				const string f1 = "file:\\";
				string sCfg = Path.GetDirectoryName(typeof(EasyQuery).Assembly.CodeBase);
				if (sCfg.StartsWith(f0, StringComparison.OrdinalIgnoreCase))
				{
					sCfg = sCfg.Substring(f0.Length);
				}
				else if (sCfg.StartsWith(f1, StringComparison.OrdinalIgnoreCase))
				{
					sCfg = sCfg.Substring(f1.Length);
				}
				return sCfg;
			}
		}
		private static EnumAppLocation _appLocation = EnumAppLocation.Unknown;
		[Browsable(false)]
		public static EnumAppLocation ApplicationLocation
		{
			get
			{
				if (_appLocation == EnumAppLocation.Unknown)
				{
					_appLocation = EnumAppLocation.Both;
					string sCfg = Path.Combine(CodeBaseFolder, "databaseSettings.xml");
					if (System.IO.File.Exists(sCfg))
					{
						XmlDocument doc = new XmlDocument();
						doc.Load(sCfg);
						XmlNode node = doc.SelectSingleNode("Root/Configuration");
						if (node != null)
						{
							XmlAttribute xa = node.Attributes["appLocation"];
							if (xa != null)
							{
								string sLoc = xa.Value;
								if (!string.IsNullOrEmpty(sLoc))
								{
									try
									{
										_appLocation = (EnumAppLocation)Enum.Parse(typeof(EnumAppLocation), sLoc, true);
									}
									catch
									{
										_appLocation = EnumAppLocation.Both;
									}
								}
							}
						}
					}
				}
				return _appLocation;
			}
		}
		[Browsable(false)]
		public static EasyQuery Edit(EasyQuery query, Form dialogParent)
		{
			if (string.IsNullOrEmpty(query.ConnectionString))
			{
				query.SelectConnection(dialogParent);
			}
			if (!string.IsNullOrEmpty(query.ConnectionString))
			{
				QueryParser qryP = new QueryParser();
				if (qryP.BuildQuery(query, dialogParent))
				{
					return qryP.query;
				}
			}
			return null;
		}
		[Browsable(false)]
		public static int GetControlWidth(UserControl control, string inputString, Font inputFont)
		{
			// Create a Graphics object.
			Graphics g = control.CreateGraphics();

			// Get the Size needed to accommodate the formatted Text.
			int width = g.MeasureString(inputString, inputFont).ToSize().Width;

			// Clean up the Graphics object.
			g.Dispose();

			return width;
		}
		#endregion

		#region ICloneable Members
		public void CopyFrom(EasyQuery query)
		{
			Description = query.Description;
			connect = query.connect;
			UpdatableTableName = query.UpdatableTableName;
			Fields = query.Fields;
			Distinct = query.Distinct;
			DataPreparer = query.DataPreparer;
			DataPreparerParameternames = query.DataPreparerParameternames;
			SqlParameternames = query.SqlParameternames;
			Top = query.Top;

			Percent = query.Percent;
			WithTies = query.WithTies;
			Parameters = query.Parameters;
			From = query.From;
			Where = query.Where;
			GroupBy = query.GroupBy;
			OrderBy = query.OrderBy;

			_sql = query._sql;
			IsCommand = query.IsCommand;
			_paramMap = query._paramMap;
			T = query.T;
			Having = query.Having;
			Limit = query.Limit;
			SampleTopRec = query.SampleTopRec;
		}
		public virtual object Clone()
		{
			EasyQuery obj = (EasyQuery)Activator.CreateInstance(this.GetType());
			obj.Description = Description;
			obj.IsCommand = this.IsCommand;
			if (connect != null)
			{
				obj.DatabaseConnection = (ConnectionItem)connect.Clone();
			}
			obj.UpdatableTableName = UpdatableTableName;
			if (Fields != null)
				obj.Fields = (FieldList)Fields.Clone();
			obj.Distinct = Distinct;
			obj.Top = Top;
			obj.DataPreparer = DataPreparer;
			if (DataPreparerParameternames == null)
			{
				obj.DataPreparerParameternames = null;
			}
			else
			{
				obj.DataPreparerParameternames = new string[DataPreparerParameternames.Length];
				for (int i = 0; i < DataPreparerParameternames.Length; i++)
				{
					obj.DataPreparerParameternames[i] = DataPreparerParameternames[i];
				}
			}
			if (SqlParameternames == null)
			{
				obj.SqlParameternames = null;
			}
			else
			{
				obj.SqlParameternames = new string[SqlParameternames.Length];
				for (int i = 0; i < SqlParameternames.Length; i++)
				{
					obj.SqlParameternames[i] = SqlParameternames[i];
				}
			}
			obj.SampleTopRec = SampleTopRec;
			obj.Limit = Limit;
			obj.Percent = Percent;
			obj.WithTies = WithTies;
			if (Parameters != null)
				obj.Parameters = (ParameterList)Parameters.Clone();
			obj.From = From;
			obj.Where = Where;
			obj.GroupBy = GroupBy;
			obj.OrderBy = OrderBy;
			obj.IsFilterInsertedByQueryBuilder = IsFilterInsertedByQueryBuilder;
			if (_sql != null)
			{
				obj._sql = new SQLStatement(_sql.ToString());
			}
			if (_paramMap != null)
			{
				obj._paramMap = _paramMap;
			}
			if (T != null)
				obj.T = (TableAliasList)T.Clone();
			obj.Having = Having;
			obj.bQueryOK = bQueryOK;
			return obj;
		}
		#endregion

		#region Overrides
		public override string ToString()
		{
			if (string.IsNullOrEmpty(Description))
			{
				if (_sql != null)
				{
					return _sql.ToString();
				}
			}
			return Description;
		}
		#endregion

		#region None-browsable methods
		[Browsable(false)]
		[NotForProgramming]
		public static string[] GetParameterNames(string sql, string Sep1, string Sep2)
		{
			List<string> sc = new List<string>();
			string ParseError;

			sql = sql.Replace('\r', ' ');
			sql = sql.Replace('\n', ' ');
			sql = sql.Replace('\t', ' ');
			sql = sql.Trim();
			int n = SQLParser.FindParameterIndex(sql, 0, Sep1, Sep2, out ParseError);
			if (!string.IsNullOrEmpty(ParseError))
			{
				throw new ExceptionLimnorDatabase("Error validating sql parameters for {0}. {1}", sql, ParseError);
			}
			while (n >= 0)
			{
				int i = SQLParser.FindNameEnd(sql, n + 2);
				string sName = sql.Substring(n, i - n);
				sc.Add(sName);
				n = SQLParser.FindParameterIndex(sql, i + 1, Sep1, Sep2, out ParseError);
				if (!string.IsNullOrEmpty(ParseError))
				{
					throw new ExceptionLimnorDatabase("Error validating sql parameters for {0}. {1}", sql, ParseError);
				}
			}
			return sc.ToArray();
		}
		[Browsable(false)]
		[NotForProgramming]
		public void ValidateSqlParameters()
		{
			if (this.Parameters != null)
			{
				bool misMatching = false;
				if (this.SqlParameternames == null)
				{
					misMatching = true;
				}
				else if (this.SqlParameternames.Length < this.Parameters.Count)
				{
					misMatching = true;
				}
				else
				{
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						bool found = false;
						for (int k = 0; k < this.SqlParameternames.Length; k++)
						{
							if (string.Compare(this.SqlParameternames[k], this.Parameters[i].Name, StringComparison.OrdinalIgnoreCase) == 0)
							{
								found = true;
								break;
							}
						}
						if (!found)
						{
							misMatching = true;
							break;
						}
					}
				}
				if (misMatching)
				{
					string Sep1 = this.NameDelimiterBegin, Sep2 = this.NameDelimiterEnd;
					string sql = SQL.GetSQL();
					this.SqlParameternames = GetParameterNames(sql, Sep1, Sep2);
					if (this.SqlParameternames.Length < this.Parameters.Count)
					{
						StringBuilder sb = new StringBuilder();
						sb.Append("Existing parameters:");
						for (int i = 0; i < this.Parameters.Count; i++)
						{
							sb.Append(this.Parameters[i].Name);
							sb.Append(",");
						}
						sb.Append("  Validated parameters:");
						for (int i = 0; i < this.SqlParameternames.Length; i++)
						{
							sb.Append(this.SqlParameternames[i]);
							sb.Append(",");
						}
						throw new ExceptionLimnorDatabase("Error validating sql parameters for {0}. {1}", sql, sb.ToString());
					}
				}
			}
		}
		/// <summary>
		/// for design time databinding; called by EasyGrid
		/// </summary>
		[Browsable(false)]
		public void CreateMainTable()
		{
			if (Tables[TableName] == null)
			{
				DataTable tbl = new DataTable(TableName);
				FieldList fl = Fields;
				for (int i = 0; i < fl.Count; i++)
				{
					DataColumn c = new DataColumn(fl[i].PureName, EPField.ToSystemType(fl[i].OleDbType));
					tbl.Columns.Add(c);
				}
				Tables.Add(tbl);
			}
		}
		[Browsable(false)]
		public BLOBRow GetBlobRow(DataRow dw)
		{
			if (blobTable != null)
			{
				return blobTable.GetRow(dw);
			}
			return null;
		}
		[Browsable(false)]
		public int GetBlobFieldIndex(string fld)
		{
			if (blobTable != null)
			{
				for (int i = 0; i < blobTable.fields.Count; i++)
				{
					if (blobTable.fields[i].Name == fld)
						return i;
				}
			}
			return -1;
		}
		[Browsable(false)]
		public void SaveBlob(int index, EPBLOB blob)
		{
			if (blobTable != null && rowIDFields != null)
			{
				blobTable.ExecuteCommand(rowIDFields, index, blob);
			}
		}
		[Browsable(false)]
		public FieldList GetUniqueKeyFields()
		{
			if (rowIDFields == null)
			{
				rowIDFields = new FieldList();
				if (!string.IsNullOrEmpty(this.UpdatableTableName))
				{
					FieldList fl = Fields;
					for (int i = 0; i < fl.Count; i++)
					{
						if (fl[i].IsIdentity && (string.Compare(fl[i].FromTableName, this.UpdatableTableName, StringComparison.OrdinalIgnoreCase) == 0))
						{
							rowIDFields.AddField(fl[i]);
							break;
						}
					}
					if (rowIDFields.Count == 0)
					{
						for (int i = 0; i < fl.Count; i++)
						{
							if (fl[i].Indexed)
								rowIDFields.AddField(fl[i]);
						}
					}
				}
			}
			return rowIDFields;
		}
		[Browsable(false)]
		public bool HasRowID()
		{
			if (Fields != null)
			{
				return Fields.HasRowID();
			}
			return false;
		}
		[Browsable(false)]
		public void ReloadConnection()
		{
			if (connect != null)
			{
				if (!connect.ConnectionObject.IsConnectionReady)
				{
					if (!string.IsNullOrEmpty(connect.Filename))
					{
						Guid g = new Guid(connect.Filename);
						connect = ConnectionItem.LoadConnection(g, true, false);
					}
				}
			}
			setDefaultConnection();
		}
		[Browsable(false)]
		public void ResetCanChangeDataSet(bool canChange)
		{
			_changeDataSet = canChange;
		}
		[Browsable(false)]
		public void SetRawSQL(string s)
		{
			_rawSql = s;
		}
		[Browsable(false)]
		public void SetSQL(SQLStatement q)
		{
			_rawSql = null;
			_sql = q;
		}
		[Browsable(false)]
		public virtual bool OnBeforeSetSQL()
		{
			return true;
		}
		/// <summary>
		/// replace all parameters with ? and collect parameter names in paramMap
		/// </summary>
		/// <param name="langID"></param>
		/// <param name="pTable"></param>
		/// <returns></returns>
		[Browsable(false)]
		public string MakeSelectionQuery(ParameterList psOld)
		{
			QueryParser._UseLowerCase = this.DatabaseConnection.ConnectionObject.LowerCaseSqlKeywords;
			EnumTopRecStyle topStyle = this.DatabaseConnection.ConnectionObject.TopRecStyle;
			if (psOld == null)
			{
				psOld = this.Parameters.Clone() as ParameterList;
			}
			_paramMap = new FieldList();
			string sel;
			if (this.IsCommand && _sql != null)
			{
				sel = makeParamMapSQL(_sql.GetSQL(), psOld);
				return sel;
			}
			else
			{
				//get the part before "FROM"
				sel = makeSelectClause();
				_selectClause = sel;
				if (!string.IsNullOrEmpty(DataPreparer))
				{
					sel = string.Format(CultureInfo.InvariantCulture, "{0} {1}", DataPreparer, sel);
				}
				StringBuilder sSQL;
				//when this function returns, SQL and sSQL may not be the same
				//SQL keeps parameter names, sSQL replaces parameter names with ?
				_sql = new SQLStatement(sel);
				if (string.IsNullOrEmpty(sel))
				{
					sSQL = new StringBuilder();
				}
				else
				{
					sel = makeParamMapSQL(sel, psOld);
					sSQL = new StringBuilder(sel);
					sSQL.Append(QueryParser.SQL_From());
					string sFrom = SQLParser.MapParameters(From, ParameterStyle, this.connect.ConnectionObject.NameDelimiterBegin,
						this.connect.ConnectionObject.NameDelimiterEnd, Parameters, _paramMap, psOld);
					sSQL.Append(sFrom);
					//
					_sql.StartBuilder();
					//
					_sql.Append(QueryParser.SQL_From());
					_sql.Append(From);
					//sWhere is for linking to the parent table
					//for MS SQL, sWhere = Where
					string sWhere = makeParamMap(psOld);
					if (!string.IsNullOrEmpty(Where))
					{
						if (!string.IsNullOrEmpty(sWhere))
						{
							if (sWhere.IndexOf(" UNION ", StringComparison.OrdinalIgnoreCase) < 0)
							{
								sSQL.Append(QueryParser.SQL_Where());
								sSQL.Append("(");
								sSQL.Append(sWhere);
								sSQL.Append(")");
							}
							else
							{
								sSQL.Append(QueryParser.SQL_Where());
								sSQL.Append(sWhere);
							}
						}
						else
						{
							sSQL.Append(QueryParser.SQL_Where());
							sSQL.Append(Where);
						}
						//using parameter name, for displaying
						_sql.Append(QueryParser.SQL_Where());
						_sql.Append(Where);
					}
					else
					{
						//sWhere is for linking to parent table
						if (!string.IsNullOrEmpty(sWhere))
						{
							sSQL.Append(QueryParser.SQL_Where());
							sSQL.Append(sWhere);
						}
					}
					if (!string.IsNullOrEmpty(GroupBy))
					{
						sSQL.Append(QueryParser.SQL_GroupBy());
						sSQL.Append(GroupBy);
						_sql.Append(QueryParser.SQL_GroupBy());
						_sql.Append(GroupBy);
					}
					if (!string.IsNullOrEmpty(Having))
					{
						string sHAVING = makeHavingParamMap(psOld);
						sSQL.Append(QueryParser.SQL_Having());
						sSQL.Append(sHAVING);
						_sql.Append(QueryParser.SQL_Having());
						_sql.Append(sHAVING);
					}
					if (!string.IsNullOrEmpty(OrderBy))
					{
						sSQL.Append(QueryParser.SQL_OrderBy());
						sSQL.Append(OrderBy);
						_sql.Append(QueryParser.SQL_OrderBy());
						_sql.Append(OrderBy);
					}
					if (UseSampleTopRec)
					{
						if (topStyle == EnumTopRecStyle.Limit)
						{
							int nTop = 0;
							if (SampleTopRec > 0)
							{
								nTop = SampleTopRec;
							}
							else
							{
								nTop = Top;
							}
							if (nTop > 0)
							{
								sSQL.Append(QueryParser.SQL_Top_MySql());
								sSQL.Append(nTop.ToString(CultureInfo.InvariantCulture));
								_sql.Append(QueryParser.SQL_Top_MySql());
								_sql.Append(nTop.ToString(CultureInfo.InvariantCulture));
							}
						}
					}
					else
					{
						if (!string.IsNullOrEmpty(Limit))
						{
							sSQL.Append(QueryParser.SQL_Top_MySql());
							sSQL.Append(Limit);
							_sql.Append(QueryParser.SQL_Top_MySql());
							_sql.Append(Limit);
						}
					}
					_sql.FinishBuilder();
				}
				return sSQL.ToString();
			}
		}
		[Browsable(false)]
		public bool QueryMade()
		{
			if (Fields != null)
			{
				if (Fields.Count > 0)
				{
					if (!string.IsNullOrEmpty(From))
						return true;
				}
			}
			return false;
		}
		[Browsable(false)]
		public bool DeleteField(int i)
		{
			if (Fields != null)
			{
				return Fields.DeleteField(i);
			}
			return false;
		}
		[Browsable(false)]
		public void SwapFields(int i, int j)
		{
			if (Fields != null)
			{
				Fields.SwapFields(i, j);
			}
		}
		[Browsable(false)]
		public DbCommand MakeCommand(ParameterList psOld)
		{
			bool bReady = false;
			_lastError = string.Empty;
			string cnnStr = string.Empty;
			DbConnection cnn;
			if (connect != null)
			{
				cnnStr = connect.ConnectionStringPlain;
				if (string.IsNullOrEmpty(cnnStr))
				{
					ConnectionItem ci = ConnectionItem.LoadConnection(connect.ConnectionGuid, false, false);
					if (ci != null)
					{
						cnnStr = ci.ConnectionStringPlain;
						if (!string.IsNullOrEmpty(cnnStr))
						{
							connect = ci;
						}
					}
				}
				if (!string.IsNullOrEmpty(cnnStr))
				{
					bReady = connect.ConnectionObject.IsConnectionReady;
					if (!bReady)
					{
						_lastError = connect.ConnectionObject.LastError;
						connect.ReloadFromCache();
						cnnStr = connect.ConnectionStringPlain;
						if (!string.IsNullOrEmpty(cnnStr))
						{
							bReady = connect.ConnectionObject.IsConnectionReady;
							_lastError = connect.ConnectionObject.LastError;
						}
					}
				}
			}
			if (connect == null || !bReady || connect.ConnectionObject.TheConnection == null)
			{
				if (ApplicationLocation == EnumAppLocation.Server)
				{
					if (connect == null)
					{
						throw new ExceptionLimnorDatabase("Connection configuration not found. Make sure the database configuration file is included in the same folder with Limnordatabase.DLL");
					}
					else
					{
						if (string.IsNullOrEmpty(connect.FullFilePath))
						{
							throw new ExceptionLimnorDatabase("Connection configuration not found. Error:{0}. If a 32-bit database driver is used then compile your program to x86. Make sure the database configuration file [{1}] is included in the same folder with Limnordatabase.DLL", _lastError, connect.FullFilePath);
						}
						else
						{
							throw new ExceptionLimnorDatabase("Connection configuration not ready. Error:{0}. If a 32-bit database driver is used then compile your program to x86. Make sure the database configuration file [{1}\\{2}{3}] is included in the same folder with Limnordatabase.DLL", _lastError, EasyQuery.CodeBaseFolder, connect.Filename, ConnectionItem.EXT);
						}
					}
				}
				else
				{
					string exe = System.IO.Path.GetFileName(Application.ExecutablePath);
					if (string.Compare(exe, "LimnorMain.exe", StringComparison.OrdinalIgnoreCase) != 0)
					{
						if (string.Compare(exe, "LimnorStudioMono.exe", StringComparison.OrdinalIgnoreCase) != 0)
						{
							//create a connection record
							DlgConnectionManager dlg = new DlgConnectionManager();
							if (connect == null)
							{
								connect = new ConnectionItem();
							}
							connect.Name = exe;
							dlg.AddConnection(connect);
							if (dlg.ShowDialog() == DialogResult.OK)
							{
								connect = dlg.SelectedConnection;
								if (connect.ConnectionObject.IsConnectionReady)
								{
									ConnectionItem.AddDefaultConnection(connect.ConnectionObject);
								}
							}
						}
					}
				}
			}
			if (connect != null)
			{
				if (connect.ConnectionObject.TheConnection != null)
				{
					EnumParameterStyle pstyle = ParameterStyle;
					cnn = connect.ConnectionObject.GetConnection();
					DbCommand cmd = cnn.CreateCommand();
					cmd.CommandText = MakeSelectionQuery(psOld);
					if (pstyle == EnumParameterStyle.QuestionMark)
					{
						if (_paramMap != null)
						{
							if (_paramMap.Count > 0)
							{
								for (int i = 0; i < _paramMap.Count; i++)
								{
									DbParameter pam = cmd.CreateParameter();
									pam.ParameterName = "@P" + i.ToString();
									pam.DbType = ValueConvertor.OleDbTypeToDbType(_paramMap[i].OleDbType);
									pam.Size = _paramMap[i].DataSize;
									if (_paramMap[i].Value == null)
										pam.Value = EPField.DefaultFieldValue(_paramMap[i].OleDbType);
									else
										pam.Value = _paramMap[i].Value;
									cmd.Parameters.Add(pam);
								}
							}
						}
					}
					else
					{
						ParameterList ps = Parameters;
						if (ps != null && ps.Count > 0)
						{
							for (int i = 0; i < ps.Count; i++)
							{
								DbParameter pam = cmd.CreateParameter();
								pam.ParameterName = ParameterList.GetParameterName(pstyle, ps[i].Name);
								pam.DbType = ValueConvertor.OleDbTypeToDbType(ps[i].OleDbType);
								pam.Size = ps[i].DataSize;
								if (ps[i].Value == null)
									pam.Value = EPField.DefaultFieldValue(ps[i].OleDbType);
								else
									pam.Value = ps[i].Value;
								cmd.Parameters.Add(pam);
							}
						}
					}
					return cmd;
				}
				else
				{
				}
			}
			return null;
		}
		[Browsable(false)]
		public void ClearQuery()
		{
			Fields = new FieldList();
			Top = 0;
			SampleTopRec = 0;
			Distinct = false;
			Percent = false;
			WithTies = false;
			Parameters = null;
			Limit = string.Empty;
			From = string.Empty;
			Where = string.Empty;
			GroupBy = string.Empty;
			OrderBy = string.Empty;
			UpdatableTableName = string.Empty;
			_paramMap = null;
			_sql = new SQLStatement(string.Empty);
			T = null;
			rowIDFields = null;
			_data = null;
		}
		[Browsable(false)]
		public void AddField(EPField fld)
		{
			if (Fields == null)
			{
				Fields = new FieldList();
				Fields.AddField(fld);
			}
			else
			{
				int z = 0;
				string s = fld.Name;
				EPField f = Fields[s];
				while (f != null)
				{
					z++;
					s = fld.Name + z.ToString();
					f = Fields[s];
				}
				if (z > 0)
					fld.Name += z.ToString();
				Fields.AddField(fld);
			}
		}

		#endregion

		#region internal methods
		internal void SetLoaded()
		{
			_loaded = true;
		}
		internal void UnsetLoaded()
		{
			_loaded = false;
		}
		internal dlgPropFields GetFieldsDialog()
		{
			if (Fields.Count > 0)
			{
				dlgPropFields dlg = new dlgPropFields();
				dlg.LoadData(this);
				return dlg;
			}
			return null;
		}
		#endregion

		#region private methods
		private bool parsesql(string sSQL)
		{
			QueryParser qp = new QueryParser();
			qp.LoadData(this);
			EasyQuery qry = null;
			string sMsg;
			try
			{
				qry = qp.ParseQuery((Form)null, sSQL, out sMsg);
				if (qry != null)
				{
					this.CopyFrom(qry);
					ParameterList ps = this.Parameters.Clone() as ParameterList;
					qry.MakeSelectionQuery(ps);
					_sql = qry._sql;
					_rawSql = null;
				}
			}
			catch (Exception err)
			{
				qry = null;
				sMsg = TraceLogClass.ExceptionMessage0(err);
			}
			if (!string.IsNullOrEmpty(sMsg))
			{
				_lastError = sMsg;
				LogMessage("{0} - Error setting SQL. {1}", TableName, sMsg);
				if (LastErrorMessageChanged != null)
				{
					LastErrorMessageChanged(this, EventArgs.Empty);
				}
			}
			return (qry != null);
		}
		private bool initializeFields()
		{
			FieldList fl = Fields;
			if (fl.Count > 0)
			{
				LogMessage("{0} - initializeFields: field count={1}", TableName, fl.Count);
				for (int i = 0; i < fl.Count; i++)
				{
					fl[i].SetOwner(this);
					if (!_hasCalculatedFields)
					{
						_hasCalculatedFields = fl[i].IsCalculated;
					}
				}
				fl.AdjustEditor();
				createBlobFields();
				LogMessage("{0} - initializeFields: finished", TableName);
				return true;
			}
			else
			{
				_lastError = "field count is 0";
				LogMessage("{0} - initializeFields: field count=0", TableName);
			}
			return false;
		}
		private bool initializeCommands()
		{
			LogMessage("{0} - initializeCommands starts", TableName);
			if (!string.IsNullOrEmpty(this.Where))
			{
				string wr = this.Where.Trim();
				if (wr.Length == 0)
				{
					_where = string.Empty;
				}
			}
			if (BeforePrepareCommands != null)
			{
				BeforePrepareCommands(this, EventArgs.Empty);
			}
			QueryParser._UseLowerCase = this.DatabaseConnection.ConnectionObject.LowerCaseSqlKeywords;
			StringBuilder sQry;
			string Sep1 = DatabaseConnection.ConnectionObject.NameDelimiterBegin;
			string Sep2 = DatabaseConnection.ConnectionObject.NameDelimiterEnd;
			int i;
			if (this.IsCommand)
			{
				_forReadOnly = true;
			}
			_readOnlyResult = _forReadOnly;
			if (!_forReadOnly)
			{
				if (string.IsNullOrEmpty(UpdatableTableName))
				{
					FieldList fl = Fields;
					for (i = 0; i < fl.Count; i++)
					{
						if (fl[i].IsIdentity)
						{
							UpdatableTableName = fl[i].FromTableName;
							break;
						}
					}
					if (string.IsNullOrEmpty(UpdatableTableName))
					{
						for (i = 0; i < fl.Count; i++)
						{
							if (fl[i].Indexed)
							{
								UpdatableTableName = fl[i].FromTableName;
								break;
							}
						}
					}
					if (string.IsNullOrEmpty(UpdatableTableName))
					{
						_readOnlyResult = true;
					}
				}
			}
			StringCollection scInsertFields = new StringCollection();
			//
			rowIDFields = null;
			FieldList slID = GetUniqueKeyFields();
			if (slID.Count == 0)
			{
				_readOnlyResult = true;
			}
			ParameterList psOld = this.Parameters;
			this.Parameters = new ParameterList();
			cmdSelection = MakeCommand(psOld);
			foreach (EPField f in this.Parameters)
			{
				EPField fo = psOld[f.Name];
				if (fo != null)
				{
					if (fo.OleDbType != OleDbType.Error && fo.OleDbType != OleDbType.Empty)
					{
						f.OleDbType = fo.OleDbType;
						if (cmdSelection != null)
						{
							for (int j = 0; j < cmdSelection.Parameters.Count; j++)
							{
								if (string.CompareOrdinal(f.Name, cmdSelection.Parameters[j].ParameterName) == 0)
								{
									cmdSelection.Parameters[j].DbType = f.GetDbType();
									break;
								}
							}
						}
					}
					f.Value = fo.Value;
				}
			}
			if (cmdSelection != null)
			{
				LogMessage("{0} - Select command:{1}", TableName, cmdSelection.CommandText);
				da = DataAdapterFinder.CreateDataAdapter(cmdSelection);
				if (!string.IsNullOrEmpty(UpdatableTableName))
				{
					da.TableMappings.Add(UpdatableTableName, TableName);
				}
				//
				bool bUpdate = false;
				if (!_readOnlyResult)
				{
					bUpdate = !string.IsNullOrEmpty(cmdSelection.CommandText);
				}
				if (bUpdate)
				{
					StringBuilder sWhere = new StringBuilder();
					bool gotWhere = false;
					//update query
					StringBuilder sFldList = new StringBuilder();
					bool gotUpdateField = false;
					//insert query
					bool gotInsertField = false;
					StringBuilder sInsertNames = new StringBuilder();
					StringBuilder sInsertValues = new StringBuilder();
					//
					bool bUse;
					string sTimeStamp = "";
					int nTSfield = -1;
					bool isSqlServer = IsSQLSever;
					EnumParameterStyle pstyle = ParameterStyle;
					bool isOdbc = IsOdbc;
					FieldList fl = Fields;
					for (i = 0; i < fl.Count; i++)
					{
						if (sTimeStamp.Length == 0)
						{
							if (isOdbc)
							{
								if (fl[i].GetOdbcDbType() == System.Data.Odbc.OdbcType.Timestamp)
								{
									sTimeStamp = string.Format(CultureInfo.InvariantCulture, "{4} {0}{1}{2} {5} {0}{3}{2}=?", Sep1, UpdatableTableName, Sep2, fl[i].Name, QueryParser.SQL_Update(), QueryParser.SQL_Set());
									nTSfield = i;
								}
							}
							else if (isSqlServer)
							{
								if (fl[i].GetSqlDbType() == SqlDbType.Timestamp)
								{
									sTimeStamp = string.Format(CultureInfo.InvariantCulture, "{5} {0}{1}{2} {6} {0}{3}{2}={4}", Sep1, UpdatableTableName, Sep2, fl[i].Name, fl[i].GetParameterName(pstyle), QueryParser.SQL_Update(), QueryParser.SQL_Set());
									nTSfield = i;
								}
							}
						}
						//insert query
						if (!fl[i].IsIdentity && !fl[i].ReadOnly)
						{
							if (gotInsertField)
							{
								sInsertNames.Append(",");
								sInsertValues.Append(",");
							}
							else
							{
								gotInsertField = true;
							}
							sInsertNames.Append(Sep1);
							sInsertNames.Append(fl[i].Name);
							sInsertNames.Append(Sep2);
							if (pstyle == EnumParameterStyle.QuestionMark)
							{
								sInsertValues.Append("?");
							}
							else
							{
								sInsertValues.Append(fl[i].GetParameterName(pstyle));
							}
						}
						//update query
						bUse = false;
						if (!fl[i].IsIdentity && !fl[i].ReadOnly)
						{
							bUse = true;
							if (isSqlServer)
							{
								if (fl[i].GetSqlDbType() == SqlDbType.Timestamp)
									bUse = false;
							}
							else if (isOdbc)
							{
								if (fl[i].GetOdbcDbType() == System.Data.Odbc.OdbcType.Timestamp)
									bUse = false;
							}
						}
						if (bUse)
						{
							if (gotUpdateField)
							{
								sFldList.Append(",");
							}
							else
							{
								gotUpdateField = true;
							}
							sFldList.Append(Sep1);
							sFldList.Append(fl[i].Name);
							sFldList.Append(Sep2);
							sFldList.Append("=");
							if (pstyle == EnumParameterStyle.QuestionMark)
							{
								sFldList.Append("?");
							}
							else
							{
								sFldList.Append(fl[i].GetParameterName(pstyle));
							}
						}
					}
					sQry = new StringBuilder(QueryParser.SQL_Update());
					sQry.Append(Sep1);
					sQry.Append(UpdatableTableName);
					sQry.Append(Sep2);
					sQry.Append(QueryParser.SQL_Set());
					sQry.Append(sFldList.ToString());
					sQry.Append(QueryParser.SQL_Where());
					//make where
					for (i = 0; i < slID.Count; i++)
					{
						if (gotWhere)
						{
							sWhere.Append(QueryParser.SQL_And());
						}
						sWhere.Append(Sep1);
						sWhere.Append(slID[i]);
						sWhere.Append(Sep2);
						sWhere.Append("=");
						if (pstyle == EnumParameterStyle.QuestionMark)
						{
							sWhere.Append("?");
						}
						else
						{
							sWhere.Append(slID[i].GetParameterName(pstyle));
						}
					}
					sQry.Append(sWhere.ToString());
					if (sTimeStamp.Length > 0)
					{
						sTimeStamp += QueryParser.SQL_Where() + sWhere;
						_timestampConnection = (ConnectionItem)DatabaseConnection.Clone();
						cmdTimestamp = _timestampConnection.ConnectionObject.TheConnection.CreateCommand();
						cmdTimestamp.CommandText = sTimeStamp;
						DbParameter pam = cmdTimestamp.CreateParameter();
						if (pstyle == EnumParameterStyle.QuestionMark)
						{
							pam.ParameterName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "@{0}", fl[nTSfield].Name);
						}
						else
						{
							pam.ParameterName = fl[nTSfield].GetParameterName(pstyle);
						}
						pam.DbType = DbType.DateTime;
						pam.Size = 8;
						cmdTimestamp.Parameters.Add(pam);
						for (i = 0; i < slID.Count; i++)
						{
							pam = cmdTimestamp.CreateParameter();
							pam.ParameterName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "@{0}", slID[i].Name);
							pam.DbType = ValueConvertor.OleDbTypeToDbType(slID[i].OleDbType);
							pam.Size = slID[i].DataSize;
							cmdTimestamp.Parameters.Add(pam);
						}
					}
					string sInsertQry = string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"{5} {6} {0}{1}{2} ({3}) {7} ({4})",
						Sep1, UpdatableTableName, Sep2, sInsertNames.ToString(), sInsertValues.ToString(), QueryParser.SQL_Insert(), QueryParser.SQL_Into(), QueryParser.SQL_Values());
					insertFields = sInsertNames.ToString().Split(',');
					if (Sep1.Length > 0 && Sep2.Length > 0)
					{
						for (int k = 0; k < insertFields.Length; k++)
						{
							if (insertFields[k].Length > (Sep1.Length + Sep2.Length))
							{
								if (insertFields[k].StartsWith(Sep1, StringComparison.OrdinalIgnoreCase) && insertFields[k].EndsWith(Sep2, StringComparison.OrdinalIgnoreCase))
								{
									insertFields[k] = insertFields[k].Substring(Sep1.Length);
									insertFields[k] = insertFields[k].Substring(0, insertFields[k].Length - Sep2.Length);
								}
							}
						}
					}
					if (gotUpdateField)
					{
						//update command
						cmdUpdate = DatabaseConnection.ConnectionObject.TheConnection.CreateCommand();
						cmdUpdate.UpdatedRowSource = UpdateRowSource.Both;
						cmdUpdate.CommandText = sQry.ToString();
						for (i = 0; i < fl.Count; i++)
						{
							bUse = (!fl[i].IsIdentity && !fl[i].ReadOnly);
							if (bUse)
							{
								if (isSqlServer)
								{
									bUse = (fl[i].GetSqlDbType() != SqlDbType.Timestamp);
								}
								else if (isOdbc)
								{
									bUse = (fl[i].GetOdbcDbType() != System.Data.Odbc.OdbcType.Timestamp);
								}
							}
							if (bUse)
							{
								DbParameter pam = cmdUpdate.CreateParameter();
								if (pstyle == EnumParameterStyle.QuestionMark)
								{
									pam.ParameterName = fl[i].Name;
								}
								else
								{
									pam.ParameterName = fl[i].GetParameterName(pstyle);
								}
								pam.DbType = ValueConvertor.OleDbTypeToDbType(fl[i].OleDbType);
								pam.Size = fl[i].DataSize;
								pam.SourceColumn = fl[i].Name;
								cmdUpdate.Parameters.Add(pam);
							}
						}
						for (i = 0; i < slID.Count; i++)
						{
							DbParameter pam = cmdUpdate.CreateParameter();
							if (pstyle == EnumParameterStyle.QuestionMark)
							{
								pam.ParameterName = slID[i].Name;
							}
							else
							{
								pam.ParameterName = slID[i].GetParameterName(pstyle);
							}
							bool bFound = false;
							for (int j = 0; j < cmdUpdate.Parameters.Count; j++)
							{
								if (string.Compare(pam.ParameterName, cmdUpdate.Parameters[j].ParameterName, StringComparison.OrdinalIgnoreCase) == 0)
								{
									bFound = true;
									break;
								}
							}
							if (!bFound)
							{
								pam.DbType = ValueConvertor.OleDbTypeToDbType(slID[i].OleDbType);
								pam.Size = slID[i].DataSize;
								pam.SourceColumn = slID[i].Name;
								pam.Direction = ParameterDirection.Input;
								pam.SourceColumnNullMapping = false;
								pam.SourceVersion = DataRowVersion.Original;
								cmdUpdate.Parameters.Add(pam);
							}
						}
						da.UpdateCommand = cmdUpdate;
					}
					//delete command ===========================================================
					sQry = new StringBuilder(QueryParser.SQL_Delete());// "DELETE FROM ");
					sQry.Append(QueryParser.SQL_From());
					sQry.Append(Sep1);
					sQry.Append(UpdatableTableName);
					sQry.Append(Sep2);
					sQry.Append(QueryParser.SQL_Where());
					sQry.Append(sWhere.ToString());
					cmdDeletion = DatabaseConnection.ConnectionObject.TheConnection.CreateCommand();
					cmdDeletion.CommandText = sQry.ToString();
					for (i = 0; i < slID.Count; i++)
					{
						DbParameter pam = cmdDeletion.CreateParameter();
						if (pstyle == EnumParameterStyle.QuestionMark)
						{
							pam.ParameterName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Original_{0}", slID[i].Name);
						}
						else
						{
							pam.ParameterName = slID[i].GetParameterName(pstyle);
						}
						pam.DbType = ValueConvertor.OleDbTypeToDbType(slID[i].OleDbType);
						pam.Size = slID[i].DataSize;
						pam.SourceColumn = slID[i].Name;
						pam.SourceColumnNullMapping = false;
						pam.SourceVersion = DataRowVersion.Original;
						pam.Direction = ParameterDirection.Input;
						cmdDeletion.Parameters.Add(pam);
					}
					da.DeleteCommand = cmdDeletion;
					//===============================================================================
					if (gotInsertField)
					{
						cmdInsert = DatabaseConnection.ConnectionObject.TheConnection.CreateCommand();
						cmdInsert.CommandText = sInsertQry;
						for (i = 0; i < fl.Count; i++)
						{
							if (!fl[i].IsIdentity && !fl[i].ReadOnly)
							{
								DbParameter pam = cmdInsert.CreateParameter();
								if (pstyle == EnumParameterStyle.QuestionMark)
								{
									pam.ParameterName = fl[i].Name;
								}
								else
								{
									pam.ParameterName = fl[i].GetParameterName(pstyle);
								}
								pam.DbType = ValueConvertor.OleDbTypeToDbType(fl[i].OleDbType);
								pam.Size = fl[i].DataSize;
								pam.SourceColumn = fl[i].Name;
								cmdInsert.Parameters.Add(pam);
							}
						}
						cmdInsert.UpdatedRowSource = UpdateRowSource.Both;
						da.InsertCommand = cmdInsert;
					}
				}
				if (blobTable != null && slID != null)
				{
					if (slID.Count > 0)
					{
						blobTable.MakeCommand(UpdatableTableName, slID, DatabaseConnection.ConnectionObject);
					}
				}
				_queryChanged = false;
				LogMessage("{0} - initializeCommands: finished", TableName);
				return true;
			}
			else
			{
				LogMessage("{0} - initializeCommands: MakeCommand failed", TableName);
			}
			return false;
		}
		private void setDefaultConnection()
		{
			if (_defConnectionType != null && !string.IsNullOrEmpty(_defConnectionString))
			{
				if (connect != null)
				{
					Guid id;
					if (!string.IsNullOrEmpty(connect.Filename))
					{
						id = new Guid(connect.Filename);
					}
					else
					{
						id = connect.ConnectionObject.ConnectionGuid;
					}
					Connection _def = new Connection();
					_def.ConnectionGuid = id;
					_def.DatabaseType = _defConnectionType;
					_def.ConnectionString = _defConnectionString;
					ConnectionItem.AddDefaultConnection(_def);
					if (connect.ConnectionObject.DatabaseType == null)
					{
						connect.ConnectionObject.DatabaseType = _defConnectionType;
					}
					if (string.IsNullOrEmpty(connect.ConnectionObject.ConnectionString))
					{
						connect.ConnectionObject.ConnectionString = _defConnectionString;
					}
				}
			}
		}
		private void createBlobFields()
		{
			blobTable = null;
			if (HasRowID())
			{
				blobTable = new BLOBTable();
				FieldList fl = Fields;
				for (int i = 0; i < fl.Count; i++)
				{
					//separate memo fields
					if (fl[i].OleDbType == System.Data.OleDb.OleDbType.LongVarBinary)
					{
						blobTable.fields.AddField(fl[i]);
					}
				}
			}
		}
		private string makeSelectClause()
		{
			QueryParser._UseLowerCase = this.connect.ConnectionObject.LowerCaseSqlKeywords;
			EnumTopRecStyle topStyle = this.connect.ConnectionObject.TopRecStyle;
			StringBuilder sSQL = new StringBuilder();
			FieldList fl = Fields;
			if (fl != null)
			{
				int nCount = fl.Count;
				if (nCount > 0)
				{
					sSQL.Append(QueryParser.SQL_Select());
					if (Distinct)
						sSQL.Append(QueryParser.SQL_Distinct());
					if (topStyle == EnumTopRecStyle.TopN)
					{
						if (Top > 0 || SampleTopRec > 0)
						{
							int nTop = 0;
							if (UseSampleTopRec)
							{
								if (SampleTopRec > 0)
								{
									nTop = SampleTopRec;
								}
								else
								{
									nTop = Top;
								}
							}
							else
							{
								nTop = Top;
							}
							if (nTop > 0)
							{
								sSQL.Append(QueryParser.SQL_Top_MS());
								sSQL.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}", nTop);
								sSQL.Append(" ");
								if (Percent)
									sSQL.Append(QueryParser.SQL_Percent());
							}
						}
					}
					if (WithTies)
						sSQL.Append(QueryParser.SQL_WithTies());
					EnumQulifierDelimiter useNameSep = EnumQulifierDelimiter.Microsoft;
					if (connect != null)
					{
						useNameSep = connect.NameDelimiterStyle;
					}
					string sText = fl[0].GetFieldTextInSelect(useNameSep);
					sSQL.Append(sText);
					for (int i = 1; i < nCount; i++)
					{
						sText = fl[i].GetFieldTextInSelect(useNameSep);
						sSQL.Append(",");
						sSQL.Append(sText);
					}
				}
			}
			return sSQL.ToString();
		}


		private string makeParamMapSQL(string sSQL, ParameterList psOld)
		{
			if (this.connect != null && this.connect.ConnectionObject != null)
			{
				return SQLParser.MapParameters(sSQL, connect.ParameterStyle, this.connect.ConnectionObject.NameDelimiterBegin,
					this.connect.ConnectionObject.NameDelimiterEnd, Parameters, _paramMap, psOld);
			}
			return string.Empty;
		}

		private string makeHavingParamMap(ParameterList psOld)
		{
			if (Having == null)
				Having = "";
			string HAVING = SQLParser.MapParameters(Having, connect.ParameterStyle, this.connect.ConnectionObject.NameDelimiterBegin,
				this.connect.ConnectionObject.NameDelimiterEnd, Parameters, _paramMap, psOld);
			return HAVING;
		}
		/// <summary>
		/// 1. find parameter names in where-clause and fill them in paramMap.
		/// 2. make new parameters based on linking with the parent table, pTable
		/// </summary>
		/// <param name="pTable"></param>
		/// <returns></returns>
		private string makeParamMap(ParameterList psOld)
		{
			string WHERE;
			if (!string.IsNullOrEmpty(Where))
			{
				WHERE = SQLParser.MapParameters(Where, ParameterStyle, this.connect.ConnectionObject.NameDelimiterBegin,
					this.connect.ConnectionObject.NameDelimiterEnd, Parameters, _paramMap, psOld);
			}
			else
			{
				WHERE = string.Empty;
			}
			return WHERE;
		}
		#endregion

		#region IComponent Members

		public event EventHandler Disposed;
		private ISite _site;
		[XmlIgnore]
		[Browsable(false)]
		[ReadOnly(true)]
		public ISite Site
		{
			get
			{
				return _site;
			}
			set
			{
				_site = value;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (Disposed != null)
			{
				Disposed(this, EventArgs.Empty);
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
		public void CreateDataTable()
		{
			FieldList fl = Fields;
			if (fl != null && fl.Count > 0)
			{
				if (_data == null)
				{
					_data = new DataSet();
				}
				DataTable tbl = this.DataTable;
				if (tbl == null)
				{
					tbl = new DataTable(this.TableName);
					_data.Tables.Add(tbl);
					for (int i = 0; i < fl.Count; i++)
					{
						tbl.Columns.Add(fl[i].CreateDataColumn());
					}
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
				List<Guid> lst = new List<Guid>();
				if (connect != null)
				{
					if (connect.ConnectionGuid != Guid.Empty)
					{
						lst.Add(connect.ConnectionGuid);
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
				List<Type> lst = new List<Type>();
				if (connect != null)
				{
					if (connect.ConnectionObject.DatabaseType != null)
					{
						lst.Add(connect.ConnectionObject.DatabaseType);
					}
				}
				return lst;
			}
		}
		[Browsable(false)]
		public EasyQuery QueryDef
		{
			get
			{
				return this;
			}
		}

		[Browsable(false)]
		[Description("The name of the component")]
		public string Name
		{
			get
			{
				if (Site != null)
				{
					return Site.Name;
				}
				return string.Empty;
			}
		}
		[Browsable(false)]
		public void SetBindingContext(BindingContext context)
		{
			_context = context;
		}
		[Browsable(false)]
		public void SetBindingContext(BindingManagerBase bindingManager)
		{
			_bindingMan = bindingManager;
			if (_bindingMan != null)
			{
				_bindingMan.PositionChanged += new EventHandler(bm_PositionChanged);
			}
		}
		void bm_PositionChanged(object sender, EventArgs e)
		{
			if (_bindingMan.Position >= 0)
			{

			}
		}
		#endregion

		#region IReport32Usage Members
		[Browsable(false)]
		public string Report32Usage()
		{
			if (connect != null)
			{
				if (connect.ConnectionObject != null)
				{
					if (connect.ConnectionObject.IsJet)
					{
						return Resource1.UseJet;
					}
				}
			}
			return string.Empty;
		}

		#endregion

		#region IDynamicMethodParameters Members
		[Browsable(false)]
		public ParameterInfo[] GetDynamicMethodParameters(string methodName, object av)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
			{
				ParameterList pl = Parameters;
				if (pl != null && pl.Count > 0)
				{
					ParameterInfo[] ps = new ParameterInfo[pl.Count];
					for (int i = 0; i < pl.Count; i++)
					{
						EPField f = pl[i];
						ps[i] = new SimpleParameterInfo(f.Name, methodName, EPField.ToSystemType(f.OleDbType), string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", f.Name));
					}
					return ps;
				}
				return new ParameterInfo[] { };
			}
			else if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
			{
				if (cmdInsert == null)
				{
					if (initializeFields())
					{
						initializeCommands();
					}
				}
				if (cmdInsert != null)
				{
					ParameterInfo[] ps = new ParameterInfo[cmdInsert.Parameters.Count];
					if (insertFields != null && insertFields.Length == ps.Length)
					{
						for (int i = 0; i < cmdInsert.Parameters.Count; i++)
						{
							ps[i] = new SimpleParameterInfo(insertFields[i],
								methodName,
								EPField.ToSystemType(cmdInsert.Parameters[i].DbType),
								string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", insertFields[i]));
						}
					}
					else
					{
						for (int i = 0; i < cmdInsert.Parameters.Count; i++)
						{
							ps[i] = new SimpleParameterInfo(cmdInsert.Parameters[i].ParameterName,
								methodName,
								EPField.ToSystemType(cmdInsert.Parameters[i].DbType),
								string.Format(System.Globalization.CultureInfo.InvariantCulture, "parameter {0}", cmdInsert.Parameters[i].ParameterName));
						}
					}
					return ps;
				}
				return new ParameterInfo[] { };
			}
			return null;
		}
		[Browsable(false)]
		public object InvokeWithDynamicMethodParameters(string methodName, BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
			{
				QueryWithParameterValues(parameters);
			}
			else if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
			{
				CreateNewRecord(parameters);
			}
			return null;
		}
		[Browsable(false)]
		public bool IsUsingDynamicMethodParameters(string methodName)
		{
			if (string.CompareOrdinal(methodName, "QueryWithParameterValues") == 0)
				return true;
			if (string.CompareOrdinal(methodName, "CreateNewRecord") == 0)
				return true;
			return false;
		}
		[NotForProgramming]
		[Browsable(false)]
		public Dictionary<string, string> GetParameterDescriptions(string methodName)
		{
			return null;
		}
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
				if (string.CompareOrdinal(section, "SetFieldValue") == 0 || string.CompareOrdinal(section, "GetColumnSum") == 0)
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

		#region ICustomMethodCompiler Members
		[Browsable(false)]
		public CodeExpression CompileMethod(string methodName, string varName, IMethodCompile methodToCompile, CodeStatementCollection statements, CodeExpressionCollection parameters)
		{
			if (string.CompareOrdinal(methodName, "Search") == 0)
			{
				/* code generated:
				ExpressionValue condition = null; //action parameter
				CodeExpression ceCondition = condition.ExportCode(methodToCompile);
				IsSearching = true;
				int r0 = RowIndex;
				bool b = MoveFirst();
				while (b)
				{
					if (ceCondition)
					{
						break;
					}
					b = MoveNext();
				}
				if(!b)
				{
				   if(r0 >=0 )
				   {
						RowIndex = r0;
				   }
				}
				IsSearching = false;
				return b;
				*/
				//set flag
				CodeAssignStatement casX = new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(varName), "IsSearching"), new CodePrimitiveExpression(true));
				statements.Add(casX);
				//the first parameter is the condition
				CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(varName), "MoveFirst");
				string varBool = string.Format(CultureInfo.InvariantCulture, "b{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				string r0 = string.Format(CultureInfo.InvariantCulture, "r{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				statements.Add(new CodeVariableDeclarationStatement(typeof(int), r0, new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(varName), "RowIndex")));
				CodeVariableDeclarationStatement cvds = new CodeVariableDeclarationStatement(typeof(bool), varBool, cmie);
				statements.Add(cvds);
				statements.Add(new CodeSnippetStatement(string.Format(CultureInfo.InvariantCulture, "while ({0}) {{", varBool)));
				CodeConditionStatement ccs = new CodeConditionStatement();
				ccs.Condition = parameters[0];
				ccs.TrueStatements.Add(new CodeSnippetStatement("break;"));
				ccs.FalseStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(varBool), new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(varName), "MoveNext")));
				statements.Add(ccs);
				statements.Add(new CodeSnippetStatement("}"));
				CodeConditionStatement cc2 = new CodeConditionStatement();
				cc2.Condition = new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(varBool), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(false)),
					 CodeBinaryOperatorType.BooleanAnd,
					 new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(r0), CodeBinaryOperatorType.GreaterThanOrEqual, new CodePrimitiveExpression(0)));
				cc2.TrueStatements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(varName), "RowIndex"), new CodeVariableReferenceExpression(r0)));
				statements.Add(cc2);
				//reset flag
				casX = new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(varName), "IsSearching"), new CodePrimitiveExpression(false));
				statements.Add(casX);
				return new CodeVariableReferenceExpression(varBool);
			}
			return null;
		}

		[Browsable(false)]
		public string GetJavaScriptWebMethodReferenceCode(string ownerCodeName, string methodName, StringCollection sb, StringCollection parameters)
		{
			if (string.CompareOrdinal(methodName, "Search") == 0)
			{
				if (VPLUtil.ClassCompileData == null)
				{
					VPLUtil.ClassCompileData = new Dictionary<string, string>();
				}
				string funName;
				{
					funName = string.Format(CultureInfo.InvariantCulture,
						"search{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					sb.Add(string.Format(CultureInfo.InvariantCulture, "limnorPage.{0}=function() {{\r\n", funName));
					//
					//parameters[0] is the condition expression
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"if(JsonDataBinding.getRowCount('{0}') > 0) {{\r\n", this.TableName));
					//set flag
					sb.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.setTableAttribute('{0}','IsSearching', true);\r\n", this.TableName));
					string r0 = string.Format(CultureInfo.InvariantCulture, "r{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					string varBool = string.Format(CultureInfo.InvariantCulture, "b{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"\tvar {0} = JsonDataBinding.getCurrentRowIndex('{1}');\r\n", r0, this.TableName));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"\tvar {0} = JsonDataBinding.dataMoveFirst('{1}');\r\n", varBool, this.TableName));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"\twhile({0}) {{\r\n", varBool));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"\t\tif({0}) break;\r\n", parameters[0]));
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"\t\t{0} = JsonDataBinding.dataMoveNext('{1}');\r\n", varBool, this.TableName));
					sb.Add("\t}\r\n");
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"\tif(!{0}) JsonDataBinding.dataMoveToRecord('{1}',{2});\r\n", varBool, this.TableName, r0));
					//set flag
					sb.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.setTableAttribute('{0}','IsSearching', false);\r\n", this.TableName));
					sb.Add(string.Format(CultureInfo.InvariantCulture, "\treturn {0};\r\n", varBool));
					sb.Add("}\r\n");
					sb.Add("else return false;\r\n");
					sb.Add("}\r\n");
				}
				return string.Format(CultureInfo.InvariantCulture, "limnorPage.{0}()", funName);
			}
			return null;
		}
		[Browsable(false)]
		public void CreateActionJavaScript(string methodName, StringCollection sb, StringCollection parameters, string returnReceiver)
		{
			if (string.CompareOrdinal(methodName, "Search") == 0)
			{
				string funcCall = GetJavaScriptWebMethodReferenceCode(null, methodName, sb, parameters);
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}={1};\r\n", returnReceiver, funcCall));
				}
				else
				{
					sb.Add(string.Format(CultureInfo.InvariantCulture,
					"{1};\r\n", returnReceiver, funcCall));
				}
			}
		}
		#endregion

		#region Array Import and Export
		private Array[] _sourceData;
		private void prepareDataImport()
		{
			_sourceType = EnumDataSourceType.Array;
			if (_data == null)
			{
				_data = new DataSet();
			}
			if (_data.Tables.Count == 0)
			{
				_data.Tables.Add(TableName);
			}
			else
			{
				_tableName = _data.Tables[0].TableName;
				if (string.IsNullOrEmpty(_tableName))
				{
					_tableName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Table{0}", (UInt32)(Guid.NewGuid().GetHashCode()));
					_data.Tables[0].TableName = _tableName;
				}
			}
		}
		private void applyArrayData()
		{
			prepareDataImport();
			DataTable tbl = this.DataTable;
			tbl.Rows.Clear();
			if (_sourceData != null)
			{
				int rc = 0;
				for (int c = 0; c < _sourceData.Length; c++)
				{
					Array a = _sourceData[c];
					if (a.Length > rc) rc = a.Length;
					if (tbl.Columns.Count <= c)
					{
						int n = c + 1;
						string colName = string.Format(CultureInfo.InvariantCulture, "Column{0}", n);
						while (true)
						{
							bool bfound = false;
							for (int k = 0; k < tbl.Columns.Count; k++)
							{
								if (string.Compare(tbl.Columns[k].ColumnName, colName, StringComparison.OrdinalIgnoreCase) == 0)
								{
									bfound = true;
									break;
								}
							}
							if (bfound)
							{
								n++;
								colName = string.Format(CultureInfo.InvariantCulture, "Column{1}", n);
							}
							else
							{
								break;
							}
						}
						tbl.Columns.Add(colName, typeof(string));
					}
				}
				for (int i = 0; i < rc; i++)
				{
					object[] vs = new object[tbl.Columns.Count];
					for (int c = 0; c < vs.Length; c++)
					{
						vs[c] = null;
						if (c < _sourceData.Length)
						{
							Array a = _sourceData[c];
							if (a != null)
							{
								if (i < a.Length)
								{
									vs[c] = a.GetValue(i);
								}
							}
						}
					}
					tbl.Rows.Add(vs);
				}
			}
		}
		public EnumDataSourceType BindSourceType
		{
			get
			{
				if (_sourceType == EnumDataSourceType.None)
				{
					if (this.DatabaseConnection.IsValid)
					{
						return EnumDataSourceType.Database;
					}
				}
				return _sourceType;
			}
		}
		public void ClearData()
		{
			if (_data != null)
			{
				if (_data.Tables.Count > 0)
				{
					for (int i = 0; i < _data.Tables.Count; i++)
					{
						_data.Tables[i].Rows.Clear();
					}
				}
			}
			_sourceData = null;
		}
		public Array[] ExportToColumnArrays()
		{
			DataTable tbl = this.DataTable;
			if (tbl != null)
			{
				_sourceData = new Array[tbl.Columns.Count];
				for (int c = 0; c < tbl.Columns.Count; c++)
				{
					_sourceData[c] = new object[tbl.Rows.Count];
				}
				for (int i = 0; i < tbl.Rows.Count; i++)
				{
					for (int c = 0; c < tbl.Columns.Count; c++)
					{
						_sourceData[c].SetValue(tbl.Rows[i][c], i);
					}
				}
			}
			return _sourceData;
		}
		public void ImportFromColumnArrays(Array[] data)
		{
			if (data != null && data.Length > 0)
			{
				prepareDataImport();
				_sourceData = data;
				applyArrayData();
			}
		}
		public Array[] ExportToRowArrays()
		{
			DataTable tbl = this.DataTable;
			if (tbl != null)
			{
				Array[] rs = new Array[tbl.Rows.Count];
				for (int i = 0; i < tbl.Rows.Count; i++)
				{
					rs[i] = tbl.Rows[i].ItemArray;
				}
				return rs;
			}
			return null;
		}
		private void createColumns(DataTable tbl, int colCount)
		{
			while (tbl.Columns.Count < colCount)
			{
				int n = tbl.Columns.Count;
				string colName = string.Format(CultureInfo.InvariantCulture, "Column{0}", n);
				while (true)
				{
					bool found = false;
					for (int k = 0; k < tbl.Columns.Count; k++)
					{
						if (string.Compare(tbl.Columns[k].ColumnName, colName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							found = true;
							break;
						}
					}
					if (found)
					{
						n++;
						colName = string.Format(CultureInfo.InvariantCulture, "COlumn{0}", n);
					}
					else
					{
						break;
					}
				}
				tbl.Columns.Add(colName, typeof(string));
			}
		}
		public void ImportFromRowArrays(Array[] data, bool append)
		{
			if (data != null && data.Length > 0)
			{
				prepareDataImport();
				DataTable tbl = this.DataTable;
				if (!append)
				{
					tbl.Rows.Clear();
				}
				for (int i = 0; i < data.Length; i++)
				{
					createColumns(tbl, data[i].Length);
					object[] vs = new object[tbl.Columns.Count];
					data[i].CopyTo(vs, 0);
					tbl.Rows.Add(vs);
				}
			}
		}
		public Array ExportColumnData(int columnIndex)
		{
			DataTable tbl = this.DataTable;
			if (tbl != null && columnIndex >= 0 && columnIndex < tbl.Columns.Count)
			{
				object[] vs = new object[tbl.Rows.Count];
				for (int i = 0; i < tbl.Rows.Count; i++)
				{
					vs[i] = tbl.Rows[i].ItemArray[columnIndex];
				}
				return vs;
			}
			return null;
		}
		public void ImportColumnData(int columnIndex, Array data, bool applyData)
		{
			prepareDataImport();
			if (data != null && columnIndex >= 0)
			{
				if (_sourceData == null)
				{
					_sourceData = new Array[columnIndex + 1];
				}
				else if (columnIndex >= _sourceData.Length)
				{
					Array[] a = new Array[columnIndex + 1];
					_sourceData.CopyTo(a, 0);
					_sourceData = a;
				}
				_sourceData[columnIndex] = data;
				if (applyData)
				{
					applyArrayData();
				}
			}
		}
		public Array ExportRow(int rowIndex)
		{
			DataTable tbl = this.DataTable;
			if (tbl != null && rowIndex >= 0 && rowIndex < tbl.Rows.Count)
			{
				return tbl.Rows[rowIndex].ItemArray;
			}
			return null;
		}
		public void ImportRow(Array data, int rowIndex)
		{
			if (data != null)
			{
				DataTable tbl = this.DataTable;
				if (tbl != null)
				{
					createColumns(tbl, data.Length);
					if (rowIndex >= 0 && rowIndex < tbl.Rows.Count)
					{
						tbl.Rows[rowIndex].BeginEdit();
						for (int c = 0; c < data.Length; c++)
						{
							tbl.Rows[rowIndex][c] = data.GetValue(c);
						}
						tbl.Rows[rowIndex].AcceptChanges();
						tbl.Rows[rowIndex].EndEdit();
					}
					else if (rowIndex == -1)
					{
						object[] vs = new object[tbl.Columns.Count];
						for (int c = 0; c < data.Length; c++)
						{
							vs[c] = data.GetValue(c);
						}
						tbl.Rows.Add(vs);
					}
				}
			}
		}
		public object GetCellData(int rowIndex, int columnIndex)
		{
			DataTable tbl = this.DataTable;
			if (tbl != null)
			{
				if (rowIndex >= 0 && rowIndex < tbl.Rows.Count)
				{
					if (columnIndex >= 0 && columnIndex < tbl.Columns.Count)
					{
						return tbl.Rows[rowIndex].ItemArray[columnIndex];
					}
				}
			}
			return null;
		}
		public void SetCellData(int rowIndex, int columnIndex, object data)
		{
			DataTable tbl = this.DataTable;
			if (tbl != null)
			{
				if (rowIndex >= 0 && rowIndex < tbl.Rows.Count)
				{
					if (columnIndex >= 0 && columnIndex < tbl.Columns.Count)
					{
						tbl.Rows[rowIndex].BeginEdit();
						tbl.Rows[rowIndex][columnIndex] = data;
						tbl.Rows[rowIndex].AcceptChanges();
						tbl.Rows[rowIndex].EndEdit();
					}
				}
			}
		}
		public void ImportFromCsvFile(string filename, bool includeHeader, bool append)
		{
			prepareDataImport();
			CsvText csv = new CsvText();
			csv.HasHeader = includeHeader;
			DataTable tbl = csv.ImportFromCsvFile(filename);
			if (tbl == null)
			{
				LogMessage(csv.ErrorMessage);
			}
			else
			{
				DataTable curTbl = this.DataTable;
				if (includeHeader && curTbl.Columns.Count > 0)
				{
					int[] colIdxes = new int[tbl.Columns.Count];
					for (int c = 0; c < tbl.Columns.Count; c++)
					{
						colIdxes[c] = -1;
						for (int k = 0; k < curTbl.Columns.Count; k++)
						{
							if (string.Compare(curTbl.Columns[k].ColumnName, tbl.Columns[c].ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
							{
								colIdxes[c] = k;
								break;
							}
						}
						if (colIdxes[c] == -1)
						{
							curTbl.Columns.Add(tbl.Columns[c].ColumnName, tbl.Columns[c].DataType);
							colIdxes[c] = curTbl.Columns.Count - 1;
						}
					}
					if (!append)
					{
						curTbl.Rows.Clear();
					}
					for (int i = 0; i < tbl.Rows.Count; i++)
					{
						object[] vs = new object[curTbl.Columns.Count];
						for (int c = 0; c < tbl.Columns.Count; c++)
						{
							vs[colIdxes[c]] = tbl.Rows[i].ItemArray[c];
						}
						curTbl.Rows.Add(vs);
					}
				}
				else
				{
					while (curTbl.Columns.Count < tbl.Columns.Count)
					{
						curTbl.Columns.Add(tbl.Columns[curTbl.Columns.Count].ColumnName, tbl.Columns[curTbl.Columns.Count].DataType);
					}
					if (!append)
					{
						curTbl.Rows.Clear();
					}
					for (int i = 0; i < tbl.Rows.Count; i++)
					{
						object[] vs = new object[curTbl.Columns.Count];
						tbl.Rows[i].ItemArray.CopyTo(vs, 0);
						curTbl.Rows.Add(vs);
					}
				}
			}
		}
		public void ExportToCsvFile(string filename, bool includeHeader, bool append, EnumCharEncode encode)
		{
			CsvText csv = new CsvText();
			csv.HasHeader = includeHeader;
			bool b = csv.ExportToCsvFile(filename, this.DataTable, append, encode);
			if (!b)
			{
				LogMessage(csv.ErrorMessage);
			}
		}
		#endregion
	}
	public class EventArgsNewRowID : EventArgs
	{
		private int _rowId;
		public EventArgsNewRowID(int rowId)
		{
			_rowId = rowId;
		}
		public int RowId { get { return _rowId; } }
	}
	public delegate void EventHandlerNewRowID(object sender, EventArgsNewRowID e);
}
