/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Database support
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using System.IO;
using VPL;
using Limnor.Net;

namespace LimnorDatabase
{
	public class CsvText
	{
		private string _error;
		private FieldList fields;
		private enumSourceTextDelimiter _delimiter= enumSourceTextDelimiter.Comma;
		public CsvText()
		{
		}
		public bool HasHeader { get; set; }
		public string ErrorMessage { get { return _error; } }
		private string popField(ref string sIn)
		{
			if (sIn == null)
				return "";
			if (sIn.Length == 0)
				return "";
			int startIndex = 0;
			int qi2 = -1;
			int qi = sIn.IndexOf('"');
			if (qi >= 0)
			{
				qi2 = sIn.IndexOf('"', qi + 1);
			}
			int nTab = 0;
			while (true)
			{
				if (_delimiter == enumSourceTextDelimiter.TAB)
					nTab = sIn.IndexOf("\t", startIndex, StringComparison.Ordinal);
				else
					nTab = sIn.IndexOf(",", startIndex, StringComparison.Ordinal);
				if (qi < 0 || qi2 < 0 || nTab <= 0)
				{
					break;
				}
				if (nTab > qi && nTab < qi2)
				{
					startIndex = qi2 + 1;
				}
				else
				{
					break;
				}
			}
			if (nTab == 0)
			{
				sIn = sIn.Substring(1);
				return "";
			}
			string sRet;
			if (nTab > 0)
			{
				sRet = sIn.Substring(0, nTab);
				if (nTab == sIn.Length - 1)
				{
					sIn = "";
				}
				else
					sIn = sIn.Substring(nTab + 1);
			}
			else
			{
				sRet = sIn;
				sIn = "";
			}
			if (!string.IsNullOrEmpty(sRet) && sRet.Length > 1)
			{
				if (sRet.StartsWith("\"", StringComparison.Ordinal) && sRet.EndsWith("\"", StringComparison.Ordinal))
				{
					sRet = sRet.Substring(1, sRet.Length - 2);
				}
			}
			return DataTransferConvert.Decode(_delimiter, sRet);
		}
		private void parseFieldHeaders(string sFirstLine)
		{
			string s;
			EPField fld;
			int i = 0;
			fields = new FieldList();
			while (true)
			{
				s = popField(ref sFirstLine);
				if (s.Length == 0)
					break;
				if (this.HasHeader)
				{
					fld = new EPField(i, s);
					fields.AddField(fld);
				}
				else
				{
					fld = new EPField(i, "Column" + i.ToString());
					fld.Value = s;
					fields.AddField(fld);
				}
				i++;
			}
		}
		private void parseFields(string sLine)
		{
			string s;
			for (int i = 0; i < fields.Count; i++)
			{
				s = popField(ref sLine);
				if (s.Length == 0)
					break;
				fields[i].Value = s;
			}
		}
		public DataTable ImportFromCsvFile(string filename)
		{
			DataTable tbl = null;
			_error = string.Empty;
			if (System.IO.File.Exists(filename))
			{
				string s = null;
				int i;
				DataRow rw;
				StreamReader sr = null;
				try
				{
					sr = new StreamReader(filename);
					if (!sr.EndOfStream)
					{
						tbl = new System.Data.DataTable("TextSource");
						while (!sr.EndOfStream)
						{
							s = sr.ReadLine();
							if (!string.IsNullOrEmpty(s))
							{
								break;
							}
						}
						if (!string.IsNullOrEmpty(s))
						{
							parseFieldHeaders(s);
							for (i = 0; i < fields.Count; i++)
							{
								tbl.Columns.Add(fields[i].Name, typeof(string));
							}
							if (!this.HasHeader)
							{
								rw = tbl.NewRow();
								rw.BeginEdit();
								for (i = 0; i < fields.Count; i++)
								{
									rw[i] = fields[i].Value;
								}
								rw.EndEdit();
								tbl.Rows.Add(rw);
							}
							while (!sr.EndOfStream)
							{
								//read in one line
								s = sr.ReadLine();
								if (!string.IsNullOrEmpty(s))
								{
									parseFields(s);
									rw = tbl.NewRow();
									rw.BeginEdit();
									for (i = 0; i < fields.Count; i++)
									{
										rw[i] = fields[i].Value;
									}
									rw.EndEdit();
									tbl.Rows.Add(rw);
								}
							}
						}
					}
				}
				catch (Exception err)
				{
					_error = string.Format(CultureInfo.InvariantCulture, "Error importing {0}. {1}. {2}", filename, err.Message, err.StackTrace);
				}
				finally
				{
					if (sr != null)
					{
						try
						{
							sr.Close();
						}
						catch
						{
						}
					}
				}
			}
			else
			{
				_error = string.Format(CultureInfo.InvariantCulture, "File not found:{0}", filename);
			}
			return tbl;
		}
		public bool ExportToCsvFile(string filename, DataTable tblSrc, bool append, EnumCharEncode encode)
		{
			_error = null;
			System.IO.StreamWriter sw = null;
			if (tblSrc == null)
				_error = "Source data is null";
			else if (tblSrc.Columns.Count == 0)
				_error = "Source data is empty";
			else
			{
				try
				{
					bool bWriteHeader = HasHeader;
					if (bWriteHeader && append)
					{
						if (System.IO.File.Exists(filename))
						{
							System.IO.FileInfo fi = new System.IO.FileInfo(filename);
							if (fi.Length > 0)
							{
								bWriteHeader = false;
							}
						}
					}
					System.Text.StringBuilder sb;
					//
					sw = new System.IO.StreamWriter(filename, append, EncodeUtility.GetEncoding(encode), 2048);
					if (bWriteHeader && !append)
					{
						sb = new System.Text.StringBuilder();
						sb.Append(DataTransferConvert.Encode(_delimiter, tblSrc.Columns[0].ColumnName));
						for (int i = 1; i < tblSrc.Columns.Count; i++)
						{
							sb.Append(DataTransferConvert.Delimiter(_delimiter));
							sb.Append(DataTransferConvert.Encode(_delimiter, tblSrc.Columns[i].ColumnName));
						}
						sw.WriteLine(sb.ToString());
					}
					for (int r = 0; r < tblSrc.Rows.Count; r++)
					{
						sb = new System.Text.StringBuilder();
						object v = tblSrc.Rows[r][0];
						if (v == DBNull.Value || v == null)
						{
							v = VPLUtil.GetDefaultValue(tblSrc.Columns[0].DataType);
						}
						sb.Append(DataTransferConvert.Encode(_delimiter, StringUtility.ToString(v)));
						for (int i = 1; i < tblSrc.Columns.Count; i++)
						{
							sb.Append(DataTransferConvert.Delimiter(_delimiter));
							v = tblSrc.Rows[r][i];
							if (v == DBNull.Value || v == null)
							{
								v = VPLUtil.GetDefaultValue(tblSrc.Columns[i].DataType);
							}
							sb.Append(DataTransferConvert.Encode(_delimiter, StringUtility.ToString(v)));
						}
						sw.WriteLine(sb.ToString());
					}
				}
				catch (Exception er)
				{
					_error = ExceptionLimnorDatabase.FormExceptionText(er, "Error saving data to file {0}", filename);
				}
				finally
				{
					if (sw != null)
					{
						sw.Close();
					}
				}
			}
			return string.IsNullOrEmpty(_error);
		}
	}
	public class DataTransferConvert
	{
		const string ENC_COMMA = "&!2C";
		const string ENC_TAB = "&!09";
		const string ENC_LF = "&!0A";
		const string ENC_CR = "&!0D";
		private DataTransferConvert()
		{
		}
		public static string Delimiter(enumSourceTextDelimiter delim)
		{
			switch (delim)
			{
				case enumSourceTextDelimiter.Comma:
					return ",";
				case enumSourceTextDelimiter.TAB:
					return "\t";
			}
			return "";
		}
		public static string Encode(enumSourceTextDelimiter delim, string data)
		{
			if (data == null)
				return "";
			data = data.Replace("\r", ENC_CR);
			data = data.Replace("\n", ENC_LF);
			switch (delim)
			{
				case enumSourceTextDelimiter.Comma:
					data = data.Replace(",", ENC_COMMA);
					break;
				case enumSourceTextDelimiter.TAB:
					data = data.Replace("\t", ENC_TAB);
					break;
			}
			return data;
		}
		public static string Decode(enumSourceTextDelimiter delim, string data)
		{
			if (data == null)
				return "";
			data = data.Replace(ENC_CR, "\r");
			data = data.Replace(ENC_LF, "\n");
			switch (delim)
			{
				case enumSourceTextDelimiter.Comma:
					data = data.Replace(ENC_COMMA, ",");
					break;
				case enumSourceTextDelimiter.TAB:
					data = data.Replace(ENC_TAB, "\t");
					break;
			}
			return data;
		}
	}
	public enum enumSourceTextDelimiter { TAB, Comma }
}
