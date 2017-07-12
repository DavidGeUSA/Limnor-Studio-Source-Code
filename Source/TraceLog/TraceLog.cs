/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Trace and log
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Reflection;

namespace TraceLog
{
	public class TraceLogClass : ITraceLog
	{
		#region fields and constructors
		private string logFile;
		private string errFile;
		private string _regName;
		private string _defaultLogfilename;
		private string _defaultErrorfilename;
		private Object thisLock = new Object();
		private static ITraceLog _tracelog;
		private static bool _showMessage = true;
		private static bool _cleared;
		private bool _writeTime = true;
		public TraceLogClass()
			: this("LogFile", "LimnorStudio.log")
		{
		}
		public TraceLogClass(string regName, string defaultFile)
		{
			_regName = regName;
			if (!string.IsNullOrEmpty(defaultFile) && defaultFile.IndexOf(':') > 0)
			{
				logFile = defaultFile;
			}
			else
			{
				try
				{
					Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Longflow\\LimnorStudio");
					if (key != null)
					{
						object v = key.GetValue(regName);
						if (v != null)
							logFile = v.ToString();
						key.Close();
					}
				}
				catch
				{
				}
			}
			if (string.IsNullOrEmpty(logFile))
			{
				string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Limnor Studio");
				if (!System.IO.Directory.Exists(dir))
				{
					System.IO.Directory.CreateDirectory(dir);
				}
				if (string.IsNullOrEmpty(defaultFile))
				{
					logFile = System.IO.Path.Combine(dir, "LimnorStudio.log");
				}
				else
				{
					logFile = System.IO.Path.Combine(dir, defaultFile);
				}
			}
			errFile = string.Format(CultureInfo.InvariantCulture, "{0}.err", logFile);
			_defaultLogfilename = logFile;
			_defaultErrorfilename = errFile;
		}
		#endregion
		#region private methods
		private void log(bool err, string message)
		{
			lock (thisLock)
			{
				StreamWriter sw;
				if (err)
					sw = new StreamWriter(errFile, true);
				else
					sw = new StreamWriter(logFile, true);
				sw.WriteLine();
				if (_writeTime)
				{
					sw.WriteLine(System.DateTime.Now.ToString("u") + "========");
				}
				sw.Write(message);
				if (_writeTime)
				{
					sw.WriteLine("========");
				}
				sw.Flush();
				sw.Close();
			}
		}
		#endregion
		public static ITraceLog TraceLog
		{
			get
			{
				if (_tracelog == null)
				{
					_tracelog = new TraceLogClass();
				}
				return _tracelog;
			}
			set
			{
				_tracelog = value;
			}
		}
		public static Form MainForm;
		public static Form GetForm(IServiceProvider provider)
		{
			if (provider != null)
			{
				IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (edSvc != null)
				{
					Type t = edSvc.GetType();
					PropertyInfo pif0 = t.GetProperty("OwnerGrid");
					if (pif0 != null)
					{
						Control g = pif0.GetValue(edSvc, null) as Control;
						if (g != null)
						{
							return g.FindForm();
						}
					}
				}
			}
			return null;
		}
		#region ITraceLog Members
		public bool ShowMessageBox
		{
			get
			{
				return _showMessage;
			}
			set
			{
				_showMessage = value;
			}
		}
		public bool LogFileExist
		{
			get
			{
				if (string.IsNullOrEmpty(logFile))
				{
					return false;
				}
				return System.IO.File.Exists(logFile);
			}
		}
		public bool ErrorFileExist
		{
			get
			{
				if (string.IsNullOrEmpty(errFile))
				{
					return false;
				}
				return System.IO.File.Exists(errFile);
			}
		}
		public string ErrorFile
		{
			get
			{
				return _defaultErrorfilename;
			}
		}
		public void EnableWriteTime(bool enable)
		{
			_writeTime = enable;
		}
		public string ExceptionMessage(Exception e)
		{
			return ExceptionMessage0(e);
		}
		public static string ExceptionMessage0(Exception e)
		{
			System.Text.StringBuilder sb;
			if (e == null)
				sb = new StringBuilder("Unknown exception");
			else
			{
				sb = new StringBuilder(e.GetType().AssemblyQualifiedName);
				sb.Append("\r\n");
				while (e != null)
				{
					if (string.IsNullOrEmpty(e.Message))
						sb.Append("Message not available");
					else
						sb.Append(e.Message);
					sb.Append("\r\n");
					if (string.IsNullOrEmpty(e.StackTrace))
						sb.Append("Stack trace not available");
					else
					{
						sb.Append("StackTrace:");
						sb.Append(e.StackTrace);
					}
					e = e.InnerException;
					if (e != null)
					{
						sb.Append("\r\nInner Exception:\r\n");
					}
				}
			}
			return sb.ToString();
		}
		public string Name
		{
			get
			{
				return _regName;
			}
		}
		public string LogFile
		{
			get
			{
				return _defaultLogfilename;
			}
		}
		public void SwitchLogFiles(string filename)
		{
			string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Limnor Studio");
			if (!System.IO.Directory.Exists(dir))
			{
				System.IO.Directory.CreateDirectory(dir);
			}
			logFile = System.IO.Path.Combine(dir, filename);
			errFile = string.Format(CultureInfo.InvariantCulture, "{0}.err", logFile);
		}
		public void RestoreLogFiles()
		{
			logFile = _defaultLogfilename;
			errFile = _defaultErrorfilename;
		}
		public void Log(Exception e)
		{
			string msg = ExceptionMessage(e);
			Log(msg);
			LogError(msg);
		}
		public void Log(Exception e, string message, params object[] values)
		{
			StringCollection ms = new StringCollection();
			if (!string.IsNullOrEmpty(message))
			{
				if (values != null && values.Length > 0)
				{
					ms.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, message, values));
				}
				else
				{
					ms.Add(message);
				}
			}
			if (e != null)
			{
				ms.Add(ExceptionMessage(e));
			}
			Log(ms);
		}
		public void Log(StringCollection messages)
		{
			lock (thisLock)
			{
				StreamWriter sw = new StreamWriter(logFile, true);
				sw.WriteLine();
				sw.WriteLine(System.DateTime.Now.ToString("u") + "========");
				if (messages != null && messages.Count > 0)
				{
					foreach (string s in messages)
					{
						sw.WriteLine(s);
					}
				}
				sw.WriteLine("========");
				sw.Flush();
				sw.Close();
				if (_showMessage)
				{
					dlgMessage dlg = new dlgMessage();
					dlg.SetMessage(logFile, "There are errors. See the end of the log file for details. You may send the log file to support@limnor.com");
					dlg.TopMost = true;
					dlg.ShowDialog();
					_showMessage = dlg.bShowMessage;
				}
			}
		}
		public string Log(Form caller, Exception e, ref bool display)
		{
			string s = ExceptionMessage(e);
			if (display && _showMessage)
			{
				Log(s);
				dlgMessage dlg = new dlgMessage();
				dlg.SetMessage(logFile, s);
				dlg.ShowDialog(caller);
				display = dlg.bShowMessage;
			}
			else
			{
				Log(s);
			}
			LogError(s);
			return s;
		}
		public void Log(string message, ref bool display)
		{
			if (display && _showMessage)
			{
				Log(message);
				dlgMessage dlg = new dlgMessage();
				dlg.SetMessage(logFile, message);
				dlg.ShowDialog();
				display = dlg.bShowMessage;
			}
			else
			{
				Log(message);
			}
		}
		public void Log(string message)
		{
			log(false, message);
		}
		public void LogMessage(string message, params object[] values)
		{
			if (values == null || values.Length == 0)
			{
				log(false, message);
			}
			else
			{
				log(false, string.Format(System.Globalization.CultureInfo.InvariantCulture, message, values));
			}
		}
		public void LogError(string message)
		{
			log(true, message);
		}
		public void Trace(string message)
		{
			lock (thisLock)
			{
				StreamWriter sw = new StreamWriter(logFile, true);
				sw.WriteLine();
				if (_traceIndent > 0)
				{
					sw.Write(new string('\t', _traceIndent));
				}
				sw.Write(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.ffff", CultureInfo.InvariantCulture));
				sw.Write(" ");
				sw.Write(message);
				sw.Flush();
				sw.Close();
			}
		}
		public void Trace(string message, params object[] values)
		{
			lock (thisLock)
			{
				StreamWriter sw = new StreamWriter(logFile, true);
				sw.WriteLine();
				if (_traceIndent > 0)
				{
					sw.Write(new string('\t', _traceIndent));
				}
				string s;
				try
				{
					if (values != null && values.Length > 0)
					{
						s = string.Format(message, values);
					}
					else
					{
						s = message;
					}
				}
				catch (Exception er)
				{
					s = "Error formating message " + message + "\r\n" + ExceptionMessage0(er);
				}
				sw.Write(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.ffff", CultureInfo.InvariantCulture));
				sw.Write(" ");
				sw.Write(s);
				//sw.WriteLine("");
				sw.Flush();
				sw.Close();
			}
		}
		int _traceIndent;
		public int GetIndent()
		{
			return _traceIndent;
		}
		public void IndentIncrement()
		{
			_traceIndent++;
		}
		public void IndentDecrement()
		{
			_traceIndent--;
			if (_traceIndent < 0)
				_traceIndent = 0;
		}
		public void ResetIndent()
		{
			_traceIndent = 0;
		}
		public string GetLogContents()
		{
			lock (thisLock)
			{
				StreamReader sr = new StreamReader(logFile);
				string s = sr.ReadToEnd();
				sr.Close();
				return s;
			}
		}
		public void TrimOneTime()
		{
			if (!_cleared)
			{
				_cleared = true;
				try
				{
					if (System.IO.File.Exists(logFile))
					{
						System.IO.File.Delete(logFile);
					}
				}
				catch
				{
				}
				try
				{
					if (System.IO.File.Exists(errFile))
					{
						System.IO.File.Delete(errFile);
					}
				}
				catch
				{
				}
			}
		}
		public void ClearLogContents()
		{
			lock (thisLock)
			{
				StreamWriter sw = new StreamWriter(logFile, false);
				sw.Write("");
				sw.Flush();
				sw.Close();
				//
				sw = new StreamWriter(errFile, false);
				sw.Write("");
				sw.Flush();
				sw.Close();
			}
		}
		public void ViewLog()
		{
			Process p = new Process();
			p.StartInfo.FileName = "Notepad.exe";
			p.StartInfo.Arguments = logFile;
			p.Start();
		}
		public void ViewError()
		{
			Process p = new Process();
			p.StartInfo.FileName = "Notepad.exe";
			p.StartInfo.Arguments = errFile;
			p.Start();
		}
		#endregion
	}
}
