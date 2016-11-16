/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Programming Language Implement
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using XmlSerializer;
using System.Xml;
using System.Reflection;
using System.ComponentModel;
using VSPrj;
using MathExp;
using LimnorDesigner.MethodBuilder;
using ProgElements;
using System.Drawing;
using XmlUtility;
using System.Collections.Specialized;
using LimnorDesigner.Property;
using LimnorDesigner.Action;
using LimnorDesigner.MenuUtil;
using VPL;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using LimnorDesigner.Event;
using TraceLog;
using System.Collections;
using Limnor.WebBuilder;
using System.Globalization;
using Limnor.WebServerBuilder;
using Limnor.PhpComponents;
using LimnorDesigner.Web;
using LimnorDesigner.DesignTimeType;
using Microsoft.Win32;
using LimnorDatabase;

namespace LimnorDesigner
{
	public delegate LimnorProject fnGetLimnorProject();
	/// <summary>
	/// utility functions
	/// </summary>
	public sealed class DesignUtil
	{
		#region Designer Loader
		public delegate void fnWriteToOutputWindow(string message, params object[] values);
		static public fnGetLoaderByClassId GetLoaderByClassId;
		public static fnWriteToOutputWindow WriteToOutputWindowDelegate;
		public static fnGetLimnorProject GetActiveLimnorProject;

		#endregion

		#region Output window
		private static bool _enableIdeProfiling;
		private static DateTime _lastIdeTime = DateTime.MinValue;
		public static void SetEnableIdeProfiling(bool enable)
		{
			_enableIdeProfiling = enable;
		}
		public static void LogIdeProfile(string message, params object[] values)
		{
			if (_enableIdeProfiling)
			{
				DateTime nw = DateTime.Now;
				TimeSpan ts;
				if (_lastIdeTime > DateTime.MinValue)
				{
					ts = nw.Subtract(_lastIdeTime);
				}
				else
				{
					ts = new TimeSpan(0);
				}
				_lastIdeTime = nw;
				if (values != null && values.Length > 0)
				{
					message = string.Format(CultureInfo.InvariantCulture, message, values);
				}
				if (ts.TotalSeconds > 1)
				{
					MathNode.Trace("The last operation took {0} seconds ************************************", ts.TotalSeconds);
				}
				MathNode.Trace("IDE time:{0} (ms:{1}) - {2}", nw.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),ts.TotalMilliseconds, message);
			}
		}
		public static void WriteToOutputWindowAndLog(Exception err, string message, params object[] values)
		{
			if (string.IsNullOrEmpty(message))
			{
				WriteToOutputWindowAndLog(DesignerException.FormExceptionText(err));
			}
			else
			{
				WriteToOutputWindowAndLog(DesignerException.FormExceptionText(err) + "\r\n" + message, values);
			}
		}
		public static void WriteToOutputWindowAndLog(string message, params object[] values)
		{
			if (WriteToOutputWindowDelegate != null)
			{
				WriteToOutputWindowDelegate(message, values);
				WriteToOutputWindowDelegate("\r\n");
				bool b = TraceLogClass.TraceLog.ShowMessageBox;
				TraceLogClass.TraceLog.ShowMessageBox = false;
				TraceLogClass.TraceLog.Log(null, message, values);
				FormWarning.ShowMessage(message, values);
				TraceLogClass.TraceLog.ShowMessageBox = b;
			}
			else
			{
				MathNode.Log(null, message, values);
			}
		}
		public static void WriteToOutputWindow(string message, params object[] values)
		{
			if (WriteToOutputWindowDelegate != null)
			{
				WriteToOutputWindowDelegate(message, values);
				WriteToOutputWindowDelegate("\r\n");
			}
		}
		#endregion

		#region Usages checking

		public static List<ObjectTextID> GetPropertyUsage(LimnorProject project, PropertyClass property)
		{
			StringCollection NAMES = new StringCollection();
			NAMES.Add("Action");
			List<ObjectTextID> list = new List<ObjectTextID>();
			//find all actions that use this property
			List<XmlNode> componentNodes = GetAllComponentXmlNode(project);
			foreach (XmlNode node in componentNodes)
			{
				XmlNodeList nodes = node.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"//*[@{0}='{1}']",
					XmlTags.XMLATT_PropId, property.MemberId));
				foreach (XmlNode nd in nodes)
				{
					string objType;
					string name;
					string cname = XmlUtil.GetNameAttribute(nd.OwnerDocument.DocumentElement);
					XmlNode nodeUsage = nd;
					while (nodeUsage != null && !NAMES.Contains(nodeUsage.Name))
					{
						nodeUsage = nodeUsage.ParentNode;
					}
					if (nodeUsage != null)
					{
						name = XmlUtil.GetNameAttribute(nodeUsage);
						objType = nodeUsage.Name;
						//ignore it if it is inside the getter/setter
						UInt32 actId = XmlUtil.GetAttributeUInt(nodeUsage, XmlTags.XMLATT_ActionID);
						if (property.Getter != null && property.Getter.ActionList != null)
						{
							if (property.Getter.ActionList.ContainsActionId(actId))
							{
								continue;
							}
						}
						if (property.Setter != null && property.Setter.ActionList != null)
						{
							if (property.Setter.ActionList.ContainsActionId(actId))
							{
								continue;
							}
						}
					}
					else
					{
						name = XmlUtil.GetNameAttribute(nd);
						if (string.CompareOrdinal(nd.Name, XmlTags.XML_Item) == 0)
						{
							Type t = XmlUtil.GetLibTypeAttribute(nd);
							if (t != null)
							{
								if (typeof(ComponentIcon).IsAssignableFrom(t))
								{
									continue;
								}
							}
						}
						objType = nd.Name;
					}
					if (string.IsNullOrEmpty(name))
					{
						name = "?";
					}
					list.Add(new ObjectTextID(cname, objType, name));
				}
			}
			return list;
		}
		public static List<ObjectTextID> GetClassUsage(LimnorProject project, UInt32 classId)
		{
			List<ObjectTextID> list = new List<ObjectTextID>();
			List<XmlNode> nodes = GetAllComponentXmlNode(project);
			for (int i = 0; i < nodes.Count; i++)
			{
				UInt32 cid = XmlUtil.GetAttributeUInt(nodes[i], XmlTags.XMLATT_ClassID);
				if (cid != classId)
				{
					XmlNodeList refNodes = nodes[i].SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"//*[@{0}='{1}']", XmlTags.XMLATT_ClassID, classId));
					foreach (XmlNode nd in refNodes)
					{
						if (string.Compare(XmlTags.XML_ClassRef, nd.Name, StringComparison.OrdinalIgnoreCase) == 0)
						{
							list.Add(new ObjectTextID(XmlUtil.GetNameAttribute(nodes[i]), "Instance", XmlUtil.GetNameAttribute(nd)));
						}
						else
						{
							//identify the owner
							string ownerClass = "Unknown";
							string ownerName = "Unknown";
							XmlNode ndOwner = null;
							XmlNode np = nd.ParentNode;
							while (np != null && np.ParentNode != null && ndOwner == null)
							{
								if (string.Compare(np.ParentNode.Name, XmlTags.XML_METHODS, StringComparison.OrdinalIgnoreCase) == 0)
								{
									ndOwner = np;
									ownerClass = "Method";
									ownerName = XmlUtil.GetNameAttribute(ndOwner);
								}
								else if (string.Compare(np.ParentNode.Name, XmlTags.XML_PROPERTYLIST, StringComparison.OrdinalIgnoreCase) == 0)
								{
									ndOwner = np;
									ownerClass = "Property";
									XmlNode ndPropName = np.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
										"{0}[@{1}='Name']", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME));
									if (ndPropName != null)
									{
										ownerName = ndPropName.InnerText;
									}
								}
								else if (string.Compare(np.ParentNode.Name, XmlTags.XML_ACTIONS, StringComparison.OrdinalIgnoreCase) == 0)
								{
									ndOwner = np;
									ownerClass = "Action";
									ownerName = XmlUtil.GetNameAttribute(ndOwner);
								}
								else
								{
									np = np.ParentNode;
								}
							}
							if (ndOwner == null)
							{
								list.Add(new ObjectTextID(ownerClass, "Instance", ownerName));
							}
							else
							{
								list.Add(new ObjectTextID(ownerClass, "Instance", ownerName));
							}
						}
					}
				}
			}
			return list;
		}
		public static bool QueryDeleteItemFile(LimnorProject project, string file)
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(file);
				UInt32 classId = XmlUtil.GetAttributeUInt(doc.DocumentElement, XmlTags.XMLATT_ClassID);
				if (classId == 0)
				{
					classId = LimnorProject.GetUnsavedClassId(file);
				}
				if (classId == 0)
				{
					throw new DesignerException("Class id not found in {0}", file);
				}
				string name = XmlUtil.GetNameAttribute(doc.DocumentElement);
				List<ObjectTextID> list = GetClassUsage(project, classId);
				if (list.Count == 0)
				{
					return true;
				}
				else
				{
					dlgObjectUsage dlg = new dlgObjectUsage();
					dlg.LoadData("Cannot delete this class. It is being used by the following objects", string.Format("Class - {0}", name), list);
					dlg.ShowDialog();
				}
			}
			catch (Exception err)
			{
				WriteToOutputWindowAndLog(err, "Error calling QueryDeleteItemFile for {0}", file);
			}
			return false;
		}
		#endregion
		private static bool _enableHtmlEditor;
		public static bool HtmlEditorEnabled
		{
			get
			{
				return _enableHtmlEditor;
			}
		}
		public static void EnableHtmlEditor()
		{
			_enableHtmlEditor = true;
		}

		private static void ListFiles(string folder, StringCollection allFiles)
		{
			// Here we crate array of all dirs
			System.IO.DirectoryInfo[] dirs = new System.IO.DirectoryInfo(folder).GetDirectories();
			// Here we crate array of all files
			System.IO.FileInfo[] files = new System.IO.DirectoryInfo(folder).GetFiles();
			// Here we list the files
			foreach (System.IO.FileInfo file in files)
			{
				// here we out filename without root path
				allFiles.Add(file.FullName);
			}
			// here we search files in subfolders (using recursive method)
			foreach (System.IO.DirectoryInfo dir in dirs)
			{
				ListFiles(dir.FullName, allFiles);
			}
		}

		public static void DeleteInternetExplorerCache(string[] targets)
		{
			if (targets == null || targets.Length == 0)
				return;
			string path = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
			bool bOK = true;
			StringBuilder sbErr = new StringBuilder();
			FormProgress.ShowProgress("Clearing cache, please wait...");
			try
			{
				StringCollection files = new StringCollection();
				ListFiles(path, files);
				for (int i = 0; i < files.Count; i++)
				{
					string f = Path.GetFileName(files[i]);
					for (int j = 0; j < targets.Length; j++)
					{
						if (f.StartsWith(targets[j], StringComparison.OrdinalIgnoreCase))
						{
							try
							{
								File.Delete(files[i]);
							}
							catch
							{
								string ext = Path.GetExtension(files[i]);
								if (!string.IsNullOrEmpty(ext))
								{
									if (string.CompareOrdinal(ext.ToLowerInvariant(), ".html") == 0)
									{
										bOK = false;
										sbErr.Append(files[i]);
										sbErr.Append("\r\n");
									}
								}
							}
							break;
						}
					}
				}
			}
			catch(Exception err)
			{
				DesignUtil.WriteToOutputWindowAndLog("Cannot remove web page cache. {0}", err.Message);
			}
			finally
			{
				if (!bOK)
				{
					MessageBox.Show(FormProgress.GetCurrentForm(), string.Format(CultureInfo.InvariantCulture, "Failed deleting web page caches.\r\n{0}\r\n If the web pages are not show properly in designers then you may restart Limnor Studio.", sbErr.ToString()), "Loading web page", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				FormProgress.HideProgress();
			}
		}
		public static Type GetTypeParameterValue(IObjectPointer objRef)
		{
			if (objRef != null && objRef.ObjectType != null && objRef.ObjectType.IsGenericParameter)
			{
				Type t = null;
				ActionBranchParameterPointer abpp = objRef as ActionBranchParameterPointer;
				if (abpp != null)
				{
					CollectionPointer cp = abpp.Owner as CollectionPointer;
					if (cp != null)
					{
						LocalVariable lv = cp.Owner as LocalVariable;
						if (lv != null)
						{
							DataTypePointer dtp = lv.ClassType;
							if (dtp != null)
							{
								if (dtp.TypeParameters != null)
								{
									if (objRef.ObjectType.GenericParameterPosition >= 0 && objRef.ObjectType.GenericParameterPosition < dtp.TypeParameters.Length)
									{
										t = dtp.TypeParameters[objRef.ObjectType.GenericParameterPosition].VariableLibType;
									}
								}
							}
						}
					}
				}
				return t;
			}
			return null;
		}
		public static bool CanBeWebType(Type t)
		{
			if (typeof(string).Equals(t))
				return true;
			if (typeof(IntPtr).Equals(t))
				return false;
			if (typeof(UIntPtr).Equals(t))
				return false;
			if (t.IsPrimitive)
				return true;
			if (typeof(DateTime).Equals(t))
				return true;
			if (t.GetInterface("IJavascriptType") != null)
				return true;
			if (t.GetInterface("IPhpType") != null)
				return true;
			return false;
		}
		public static bool CanBeWebProperty(PropertyDescriptor p)
		{
			if (CanBeWebType(p.PropertyType))
				return true;
			if (VPLUtil.FindAttributeByType(p.Attributes, typeof(WebClientMemberAttribute)))
			{
				return true;
			}
			else
			{
				if (VPLUtil.FindAttributeByType(p.Attributes, typeof(WebServerMemberAttribute)))
				{
					return true;
				}
			}
			return false;
		}
		public static bool BatchBuildEnabled()
		{
			try
			{
				string name;
				if (IntPtr.Size == 4)
				{
					name = @"Software\Wow6432Node\Longflow Enterprises";
				}
				else
				{
					name = @"SOFTWARE\Longflow Enterprises";
				}
				RegistryKey key = Registry.LocalMachine.OpenSubKey(name);
				if (key != null)
				{
					object v = key.GetValue("BatchBuild");
					if (v != null)
					{
						int n = Convert.ToInt32(v, CultureInfo.InvariantCulture);
						if (n != 0)
						{
							return true;
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}
		public static bool IsLocalVarPointer(IObjectPointer op)
		{
			LocalVariable lv = op as LocalVariable;
			if (lv != null)
				return true;
			lv = op.ObjectInstance as LocalVariable;
			if (typeof(LocalVariable).IsAssignableFrom(op.ObjectType))
			{
				return true;
			}
			return false;
		}
		public static IComponent SwitchClassRef(ClassInstancePointer obj, ILimnorDesignerLoader loader, Form caller)
		{
			if (obj != null)
			{
				loader.DeleteComponent(obj);
			}
			if (ToolboxItemXType.SelectedToolboxClassId == 0)
			{
				MessageBox.Show(caller, "Cannot create component. Component ID not set for Toolbox item", "Component creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				ClassPointer definitionClass = ClassPointer.CreateClassPointer(ToolboxItemXType.SelectedToolboxClassId, loader.Project);
				Type baseType = definitionClass.BaseClassType;
				if (baseType != null)
				{
					Type[] tsArgs = null;
					if (baseType.ContainsGenericParameters)
					{
						tsArgs = GetTypeParameters(baseType, loader.Project, caller);
						if (tsArgs == null)
						{
							return null;
						}
					}
					string name;
					name = loader.CreateNewComponentName(definitionClass.Name, null);
					bool isXType = false;
					if (baseType.IsAbstract)
					{
						isXType = true;
					}
					else
					{
						if (typeof(Form).IsAssignableFrom(baseType))
						{
							isXType = true;
						}
						else
						{
							ConstructorInfo cif = baseType.GetConstructor(Type.EmptyTypes);
							if (cif == null)
							{
								isXType = true;
							}
							else
							{
								if (baseType.GetInterface("IComponent") == null)
								{
									isXType = true;
								}
							}
						}
					}
					Type componentType;
					if (isXType)
					{
						componentType = VPLUtil.GetXClassType(baseType);
					}
					else
					{
						componentType = baseType;
					}
					IComponent c = loader.CreateComponent(componentType, name);
					IXType ix = c as IXType;
					if (ix != null)
					{
						ix.SetTypeParameters(tsArgs);
					}
					return c;
				}
			}
			return null;
		}
		public static Type[] GetTypeParameters(Type t, LimnorProject project, Form caller)
		{
			t = VPLUtil.GetObjectType(t);
			Type[] tsArgs = null;
			Type[] ts = t.GetGenericArguments();
			if (ts != null && ts.Length > 0)
			{
				DlgSelectTypeParameters dlgP = new DlgSelectTypeParameters();
				dlgP.LoadData(t, project);
				if (dlgP.ShowDialog(caller) == DialogResult.OK)
				{
					if (dlgP._holder.Results != null)
					{
						tsArgs = new Type[dlgP._holder.Results.Length];
						for (int i = 0; i < dlgP._holder.Results.Length; i++)
						{
							tsArgs[i] = dlgP._holder.Results[i].BaseClassType;
						}
					}
				}
			}
			else
			{
				tsArgs = Type.EmptyTypes;
			}
			return tsArgs;
		}
		public static IComponent SwitchXType(ToolboxItemXType obj, ILimnorDesignerLoader loader, Form caller)
		{
			if (obj != null)
			{
				try
				{
					loader.DeleteComponent(obj);
				}
				catch
				{
					MessageBox.Show(caller, "Error removing component wrapper ToolboxItemXType. You may close the form and re-open it, Limnor Studio will do clean up later.", "Component creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			if (string.IsNullOrEmpty(ToolboxItemXType.SelectedToolboxTypeKey))
			{
				MessageBox.Show(caller, "Cannot create component. Component type not set for Toolbox item", "Component creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				Type t = ToolboxItemXType.GetCurrentToolboxType();
				if (t == null)
				{
					MessageBox.Show(caller, string.Format(CultureInfo.InvariantCulture, "Cannot create component. Component type not found for [{0}]", ToolboxItemXType.SelectedToolboxTypeKey), "Component creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					Type tCom = t;
					Type[] tsArgs = null;
					if (t.ContainsGenericParameters)
					{
						tsArgs = GetTypeParameters(t, loader.Project, caller);
						if (tsArgs == null)
						{
							return null;
						}
						tCom = t.MakeGenericType(tsArgs);
					}
					Type xt = VPLUtil.GetXClassType(tCom);
					string name = loader.CreateNewComponentName(VPLUtil.FormCodeNameFromname(VPLUtil.GetTypeDisplay(tCom)), null);
					IComponent c = loader.CreateComponent(xt, name);
					IXType ix = (IXType)c;
					ix.SetTypeParameters(tsArgs);
					return c;
				}
			}
			return null;
		}
		public static string LoadToolBoxItemsToProject(LimnorProject project, string[] types)
		{
			string sRet = string.Empty;
			if (types != null && types.Length > 0)
			{
				for (int i = 0; i < types.Length; i++)
				{
					int pos = types[i].IndexOf(";", StringComparison.Ordinal);
					if (pos > 0)
					{
						string dllFile = types[i].Substring(0, pos);
						if (File.Exists(dllFile))
						{
							string sType = types[i].Substring(pos + 1);
							VPLUtil.SetupExternalDllResolve(Path.GetDirectoryName(dllFile));
							try
							{
								Type tp = Type.GetType(sType);
								if (tp != null)
								{
									ConstructorInfo[] cifs = tp.GetConstructors();
									if (cifs == null || cifs.Length == 0)
									{
										throw new DesignerException("Cannot add [{0}] to the toolbox. It does not expose a public constructor.", tp.FullName);
									}
									else
									{
										project.AddToolboxItem(tp);
									}
								}
								else
								{
									sRet = string.Format(System.Globalization.CultureInfo.InvariantCulture,
										"Cannot load {0}", types[i]);
								}
							}
							catch (Exception err)
							{
								sRet = string.Format(System.Globalization.CultureInfo.InvariantCulture,
										"Cannot load {0}. {1}", types[i], err.Message);
							}
							finally
							{
								VPLUtil.RemoveExternalDllResolve();
							}
						}
						else
						{
							sRet = string.Format(System.Globalization.CultureInfo.InvariantCulture,
							  "Invalid type: {0}. File not found:{1}", types[i], dllFile);
						}
					}
					else
					{
						sRet = string.Format(System.Globalization.CultureInfo.InvariantCulture,
								"Invalid type: {0}", types[i]);
					}
				}
			}
			return sRet;
		}
		public static bool IsComComponentOwned(IObjectPointer pointer)
		{
			while (pointer != null)
			{
				if (pointer.ObjectType != null)
				{
					if (VPLUtil.GetCoClassType(pointer.ObjectType) != null)
					{
						return true;
					}
				}
				pointer = pointer.Owner;
			}
			return false;
		}
		public static bool IsAssignableFrom(Type target, Type fromType)
		{
			if (target.IsAssignableFrom(fromType))
			{
				return true;
			}
			object[] vs = fromType.GetCustomAttributes(typeof(TypeMappingAttribute), true);
			if (vs != null && vs.Length > 0)
			{
				TypeMappingAttribute tm = vs[0] as TypeMappingAttribute;
				if (target.IsAssignableFrom(tm.MappedType))
				{
					return true;
				}
			}
			if (typeof(string).Equals(target))
			{
				if (typeof(JsString).IsAssignableFrom(fromType))
				{
					return true;
				}
				if (typeof(PhpString).IsAssignableFrom(fromType))
				{
					return true;
				}
			}
			return false;
		}
		public static bool IsSessionVariable(IObjectPointer op)
		{
			PropertyPointer pp = op as PropertyPointer;
			if (pp != null)
			{
				return pp.IsSessionvariable;
			}
			return false;
		}
		public static IObjectIdentity GetActionOwner(IAction act)
		{
			IObjectIdentity startPointer = null;
			MethodActionForeach ma = act as MethodActionForeach;
			if (ma != null)
			{
				startPointer = ma.ActionOwner;
			}
			else
			{
				IPropertySetter ps = act.ActionMethod as IPropertySetter;
				if (ps != null)
				{
					startPointer = ps.SetProperty;
				}
				else
				{
					startPointer = act.ActionMethod;
				}
			}
			return startPointer;
		}
		public static object GetObjectInstance(object v)
		{
			ClassInstancePointer cp = v as ClassInstancePointer;
			if (cp != null)
			{
				return cp.ObjectInstance;
			}
			return v;
		}
		public static bool IsInstanceEqual(object v1, object v2)
		{
			if (v1 == v2)
			{
				return true;
			}
			if (GetObjectInstance(v1) == GetObjectInstance(v2))
			{
				return true;
			}
			return false;
		}

		public static EnumObjectDevelopType GetBaseObjectDevelopType(IObjectIdentity id)
		{
			IObjectIdentity o = id as IObjectIdentity;
			if (o != null)
			{
				IClass c = o as IClass;
				if (c != null)
				{
					return c.ObjectDevelopType;
				}
				List<IObjectIdentity> list = new List<IObjectIdentity>();
				list.Add(o);
				while (o != null)
				{
					c = o.IdentityOwner as IClass;
					if (c != null)
					{
						return o.ObjectDevelopType;
					}
					if (o.IdentityOwner == null)
					{
						return o.ObjectDevelopType;
					}
					o = o.IdentityOwner;
					if (list.Contains(o))
					{
						throw new DesignerException("GetBaseObjectDevelopType: cyclic owner reference detected for id [{0}], owner:[{1}][{2}]", id.ToString(), o.ToString(), o.GetType().FullName);
					}
				}
			}
			return EnumObjectDevelopType.Both;
		}
		public static IObjectIdentity GetBasePmePointer(IObjectIdentity id)
		{
			if (id == null)
			{
				throw new DesignerException("Calling GetBasePmePointer with null id");
			}
			IObjectIdentity o = id as IObjectIdentity;
			if (o != null)
			{
				IClass c = o as IClass;
				if (c != null)
				{
					throw new DesignerException("Calling GetBasePmePointer with an IClass [{0}]", c.GetType());
				}
				List<IObjectIdentity> list = new List<IObjectIdentity>();
				list.Add(o);
				while (o != null)
				{
					c = o.IdentityOwner as IClass;
					if (c != null)
					{
						return o;
					}
					o = o.IdentityOwner;
					if (list.Contains(o))
					{
						throw new DesignerException("Calling GetBasePmePointer: cyclic owner reference detected for id [{0}], owner:[{1}][{2}]", id.ToString(), o.ToString(), o.GetType().FullName);
					}
				}
			}
			throw new DesignerException("Calling GetBasePmePointer: IClass not found for [{0}]", id.GetType());
		}
		public static IClass GetClass(IObjectIdentity id)
		{
			while (id != null)
			{
				IClass co = id as IClass;
				if (co != null)
					return co;
				id = id.IdentityOwner;
			}
			return null;
		}
		public static UInt32 GetClassId(IObjectIdentity id)
		{
			while (id != null)
			{
				ICustomObject co = id as ICustomObject;
				if (co != null)
					return co.ClassId;
				id = id.IdentityOwner;
			}
			return 0;
		}
		public static void GetAllMethods(Type t, List<MethodInfo> methods)
		{
			methods.AddRange(t.GetMethods());
			if (t.IsInterface)
			{
				Type[] ts = t.GetInterfaces();
				if (ts != null && ts.Length > 0)
				{
					for (int i = 0; i < ts.Length; i++)
					{
						GetAllMethods(ts[i], methods);
					}
				}
			}
		}
		public static SortedDictionary<string, MethodInfo> FindAllMethods(Type type, bool webServerOnly)
		{
			MethodInfo[] allMethods;
			if ((typeof(IWebClientControl).Equals(type) || typeof(IWebClientComponent).Equals(type)))
			{
				allMethods = typeof(IWebClientComponent).GetMethods();
				List<MethodInfo> list = new List<MethodInfo>();
				for (int i = 0; i < allMethods.Length; i++)
				{
					if (allMethods[i].IsSpecialName)
						continue;
					if (!WebClientMemberAttribute.IsClientMethod(allMethods[i]))
					{
						continue;
					}
					list.Add(allMethods[i]);
				}
				allMethods = list.ToArray();
			}
			else
			{
				if (VPLUtil.IsDynamicAssembly(type.Assembly))
				{
					Type tc = ClassPointerX.GetClassTypeFromDynamicType(type);
					allMethods = tc.GetMethods();
				}
				else
				{
					if (type.IsInterface)
					{
						List<MethodInfo> ms = new List<MethodInfo>();
						GetAllMethods(type, ms);
						allMethods = ms.ToArray();
					}
					else
					{
						allMethods = type.GetMethods();
					}
				}
				if (allMethods != null && allMethods.Length > 0)
				{
					bool isPhp = PhpTypeAttribute.IsPhpType(type);
					bool isJs = (JsTypeAttribute.IsJsType(type) || (typeof(HtmlElement_Base).IsAssignableFrom(type)));
					List<MethodInfo> list = new List<MethodInfo>();
					for (int i = 0; i < allMethods.Length; i++)
					{
						if (isPhp)
						{
							if (!WebServerMemberAttribute.IsServerMethod(allMethods[i]))
							{
								continue;
							}
						}
						if (isJs)
						{
							if (!WebClientMemberAttribute.IsClientMethod(allMethods[i]))
							{
								continue;
							}
						}
						if (webServerOnly)
						{
							object[] vs = allMethods[i].GetCustomAttributes(typeof(WebServerMemberAttribute), true);
							if (vs == null || vs.Length == 0)
							{
								continue;
							}
						}
						bool bInclude = true;
						object[] aa = allMethods[i].GetCustomAttributes(typeof(BrowsableAttribute), false);
						if (aa != null && aa.Length > 0)
						{
							BrowsableAttribute ba = aa[0] as BrowsableAttribute;
							if (ba != null)
							{
								bInclude = ba.Browsable;
							}
						}
						if (bInclude)
						{
							aa = allMethods[i].GetCustomAttributes(typeof(NotForProgrammingAttribute), false);
							if (aa != null && aa.Length > 0)
							{
								bInclude = false;
							}
						}
						if (bInclude)
						{
							list.Add(allMethods[i]);
						}
					}

					if (list.Count < allMethods.Length)
					{
						allMethods = list.ToArray();
					}

				}
			}
			return SortMethods(allMethods, type);
		}
		public static SortedDictionary<string, MethodInfo> SortMethods(MethodInfo[] allMethods, Type type)
		{
			SortedDictionary<string, MethodInfo> methods = new SortedDictionary<string, MethodInfo>();
			if (allMethods != null && allMethods.Length > 0)
			{
				for (int i = 0; i < allMethods.Length; i++)
				{
					if (!allMethods[i].IsSpecialName)
					{
						string key = MethodPointer.GetMethodSignature(allMethods[i]);
						MethodInfo mi;
						if (methods.TryGetValue(key, out mi))
						{
							if (mi.DeclaringType.Equals(type))
							{
								key = key + "|" + allMethods[i].DeclaringType.FullName;
								methods.Add(key, allMethods[i]);
							}
							else
							{
								if (allMethods[i].DeclaringType.Equals(type))
								{
									methods[key] = allMethods[i];
									key = key + "|" + mi.DeclaringType.FullName;
									methods.Add(key, mi);
								}
								else
								{
									//both are not declared by the current type
									key = key + "|" + allMethods[i].DeclaringType.FullName;
									methods.Add(key, allMethods[i]);
								}
							}
						}
						else
						{
							methods.Add(key, allMethods[i]);
						}
					}
				}
			}
			return methods;
		}
		public static SortedDictionary<string, EventInfo> FindAllEvents(Type type, bool webServerOnly)
		{
			EventInfo[] allEvents = VPLUtil.GetAllEvents(type);
			if (webServerOnly)
			{
				List<EventInfo> l = new List<EventInfo>();
				if (allEvents != null && allEvents.Length > 0)
				{
					for (int i = 0; i < allEvents.Length; i++)
					{
						object[] vs = allEvents[i].GetCustomAttributes(typeof(WebServerMemberAttribute), true);
						if (vs == null || vs.Length == 0)
						{
							continue;
						}
					}
				}
				allEvents = new EventInfo[l.Count];
				l.CopyTo(allEvents, 0);
			}
			return SortEvents(allEvents, type);
		}
		public static SortedDictionary<string, EventInfo> SortEvents(EventInfo[] allEvents, Type type)
		{
			SortedDictionary<string, EventInfo> events = new SortedDictionary<string, EventInfo>();
			if (allEvents != null && allEvents.Length > 0)
			{
				for (int i = 0; i < allEvents.Length; i++)
				{
					if (!allEvents[i].IsSpecialName)
					{
						EventInfo ei;
						if (events.TryGetValue(allEvents[i].Name, out ei))
						{
							if (ei.DeclaringType.Equals(type))
							{
								events.Add(allEvents[i].Name + "|" + allEvents[i].DeclaringType.FullName, allEvents[i]);
							}
							else
							{
								if (allEvents[i].DeclaringType.Equals(type))
								{
									events[allEvents[i].Name] = allEvents[i];
									events.Add(ei.Name + "|" + ei.DeclaringType.FullName, ei);
								}
								else
								{
									events.Add(allEvents[i].Name + "|" + allEvents[i].DeclaringType.FullName, allEvents[i]);
								}
							}
						}
						else
						{
							events.Add(allEvents[i].Name, allEvents[i]);
						}
					}
				}
			}
			return events;
		}
		public static SortedDictionary<string, PropertyInfo> FindAllProperties(Type type)
		{
			PropertyInfo[] allProperties = type.GetProperties();
			SortedDictionary<string, PropertyInfo> properties = new SortedDictionary<string, PropertyInfo>();
			if (allProperties != null && allProperties.Length > 0)
			{
				for (int i = 0; i < allProperties.Length; i++)
				{
					if (!allProperties[i].IsSpecialName)
					{
						PropertyInfo ei;
						if (properties.TryGetValue(allProperties[i].Name, out ei))
						{
							if (ei.DeclaringType.Equals(type))
							{
								properties.Add(allProperties[i].Name + "|" + allProperties[i].DeclaringType.FullName, allProperties[i]);
							}
							else
							{
								if (allProperties[i].DeclaringType.Equals(type))
								{
									properties[allProperties[i].Name] = allProperties[i];
									properties.Add(ei.Name + "|" + ei.DeclaringType.FullName, ei);
								}
								else
								{
									properties.Add(allProperties[i].Name + "|" + allProperties[i].DeclaringType.FullName, allProperties[i]);
								}
							}
						}
						else
						{
							properties.Add(allProperties[i].Name, allProperties[i]);
						}
					}
				}
			}
			return properties;
		}
		/// <summary>
		/// for compiling purpose,
		/// find all the assemblies this type is using,
		/// include those in GAC,
		/// </summary>
		/// <param name="sc"></param>
		public static void FindReferenceLocations(Dictionary<string, Assembly> sc, Type t)
		{
			FindReferenceLocations(sc, t.Assembly);
		}
		static string _currentDllFolder;
		public static void FindReferenceLocations(Dictionary<string, Assembly> sc, Assembly a)
		{
			string s = a.Location.ToLowerInvariant();
			if (!sc.ContainsKey(s))
			{
				sc.Add(s, a);
				AssemblyName[] names = a.GetReferencedAssemblies();
				if (names != null)
				{
					for (int i = 0; i < names.Length; i++)
					{
						if (!_sysNames.Contains(names[i].Name))
						{
							try
							{
								Assembly a0 = Assembly.Load(names[i]);
								FindReferenceLocations(sc, a0);
							}
							catch (Exception e)
							{
								_currentDllFolder = Path.GetDirectoryName(a.Location);
								ResolveEventHandler mi = new ResolveEventHandler(CurrentDomain_AssemblyResolve);
								AppDomain.CurrentDomain.AssemblyResolve += mi;
								try
								{
									Assembly a0 = Assembly.Load(names[i]);
									FindReferenceLocations(sc, a0);
								}
								catch (Exception e2)
								{
									throw new DesignerException(e2, "Error resolving type [{0}]. Original error: [{1}]", names[i], DesignerException.FormExceptionText(e));
								}
								finally
								{
									AppDomain.CurrentDomain.AssemblyResolve -= mi;
								}
							}
						}
					}
				}
			}
		}

		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			if (!string.IsNullOrEmpty(_currentDllFolder) && !string.IsNullOrEmpty(args.Name))
			{
				int pos = args.Name.IndexOf(",", StringComparison.Ordinal);
				string s;
				if (pos >= 0)
				{
					s = args.Name.Substring(0, pos);
				}
				else
				{
					s = args.Name;
				}
				if (!string.IsNullOrEmpty(s))
				{
					s = Path.Combine(_currentDllFolder, string.Format(CultureInfo.InvariantCulture, "{0}.dll", s));
					return Assembly.LoadFrom(s);
				}
			}
			throw new SerializerException("Cannot resolve assembly [{0}] in folder [{1}]", args.Name, _currentDllFolder);
		}
		/// <summary>
		/// for the purpose of copying/distributing assemblies
		/// </summary>
		/// <param name="files"></param>
		/// <param name="t"></param>
		public static void GetNonGacAssemblyFiles(StringCollection files, Type t)
		{
			GetNonGacAssemblyFiles(files, t.Assembly);
		}
		public static void GetNonGacAssemblyFiles(StringCollection files, Assembly a)
		{
			string s = a.Location.ToLowerInvariant();
			if (!files.Contains(s))
			{
				files.Add(s);
				string cfg = string.Format(CultureInfo.InvariantCulture, "{0}.config", s);
				if (File.Exists(cfg))
				{
					files.Add(cfg);
				}
				AssemblyName[] names = a.GetReferencedAssemblies();
				if (names != null)
				{
					for (int i = 0; i < names.Length; i++)
					{
						Assembly a0;
						VPLUtil.SetupExternalDllResolve(Path.GetDirectoryName(a.Location));
						try
						{
							a0 = Assembly.Load(names[i]);
						}
						catch
						{
							throw;
						}
						finally
						{
							VPLUtil.RemoveExternalDllResolve();
						}
						if (!a0.GlobalAssemblyCache)
						{
							GetNonGacAssemblyFiles(files, a0);
						}
					}
				}
			}
		}
		public static bool CopyAssembly(string f, string outputFolder, StringCollection errors)
		{
			bool bOK = true;
			string tfPdb = null;
			string fn = System.IO.Path.GetFileNameWithoutExtension(f);
			string pdb = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(f), fn + ".pdb");
			string tf = System.IO.Path.Combine(outputFolder, System.IO.Path.GetFileName(f));
			try
			{
				System.IO.File.Copy(f, tf, true);
				if (System.IO.File.Exists(pdb))
				{
					tfPdb = System.IO.Path.Combine(outputFolder, fn + ".pdb");
					System.IO.File.Copy(pdb, tfPdb, true);
				}
			}
			catch (Exception ef)
			{
				string msg = DesignerException.FormExceptionText(ef);
				if (string.IsNullOrEmpty(tfPdb))
				{
					errors.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Error copying {0} to {1}. {2}", f, tf, msg));
				}
				else
				{
					errors.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Error copying {0} to {1}. {2}", pdb, tfPdb, msg));
				}
				bOK = false;
			}
			return bOK;
		}
		public static bool CopyAssembly(StringCollection afiles, Type t, string outputFolder, StringCollection errors)
		{
			bool bOK = true;
			GetNonGacAssemblyFiles(afiles, t);
			foreach (string f in afiles)
			{
				if (!CopyAssembly(f, outputFolder, errors))
				{
					bOK = false;
				}
			}
			return bOK;
		}
		public static bool CopyAssembly(StringCollection afiles, Assembly a, string outputFolder, StringCollection errors)
		{
			bool bOK = true;
			if (!a.GlobalAssemblyCache)
			{
				GetNonGacAssemblyFiles(afiles, a);
				foreach (string f in afiles)
				{
					if (!CopyAssembly(f, outputFolder, errors))
					{
						bOK = false;
					}
				}
			}
			return bOK;
		}
		public const string COMPONENT_FILE_EXT = "limnor";
		static public string CreateUniqueName(string baseName)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}{1}", baseName, Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
		}
		static public UInt64 MakeDDWord(UInt32 LoWord, UInt32 HiWord)
		{
			return ((((UInt64)LoWord & 0x00000000ffffffff)) | (((UInt64)(HiWord & 0x00000000ffffffff) << 32)));
		}
		static public void ParseDDWord(UInt64 ddword, out UInt32 LoWord, out UInt32 HiWord)
		{
			LoWord = (UInt32)(ddword & 0x00000000ffffffff);
			HiWord = (UInt32)((ddword & 0xffffffff00000000) >> 32);
		}

		static public IObjectPointer GetRootPointer(IObjectPointer p)
		{
			IObjectPointer pr = p;
			while (pr.Owner != null)
			{
				pr = pr.Owner;
			}
			return pr;
		}
		static public Type GetObjectType(XmlNode objectNode, IObjectPointer p, XmlObjectReader xr)
		{
			if (p.ObjectType != null)
			{
				return p.ObjectType;
			}
			ActionBranchParameterPointer abp = p as ActionBranchParameterPointer;
			if (abp != null)
			{
				if (abp.ParameterId != 0)
				{
					XmlNodeList nds = objectNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
						"//Name[@ID='{0}']",
						abp.ParameterId));
					foreach (XmlNode nd in nds)
					{
						XmlNode np = nd.ParentNode.SelectSingleNode("Type");
						if (np != null)
						{
							object v = xr.ReadObject(np, null);
							TypePointer tp = v as TypePointer;
							if (tp != null)
							{
								abp.SetParameterType(tp.ClassType);
								break;
							}
						}
					}
				}
			}
			else if (p.Owner != null)
			{
				GetObjectType(objectNode, p.Owner, xr);
			}
			return p.ObjectType;
		}
		static public IObjectIdentity GetTopMemberPointer(IObjectIdentity p)
		{
			if (p == null || p is IClass)
				return null;
			IObjectIdentity pr = p;
			while (pr.IdentityOwner != null)
			{
				if (pr.IdentityOwner is IClass)
					break;
				pr = pr.IdentityOwner;
			}
			return pr;
		}

		static public ulong GetRootComponentId(XmlNode rootNode)
		{
			return MakeDDWord(XmlUtil.GetAttributeUInt(rootNode, XmlTags.XMLATT_ComponentID), XmlUtil.GetAttributeUInt(rootNode, XmlTags.XMLATT_ClassID));
		}
		static public string GetNameAttribute(XmlNode node)
		{
			return XmlUtil.GetNameAttribute(node);
		}
		static public ClassPointer LoadComponentClass(LimnorProject project, XmlNode rootNode)
		{
			UInt32 classId = XmlUtil.GetAttributeUInt(rootNode, XmlTags.XMLATT_ClassID);
			return ClassPointer.CreateClassPointer(classId, project);
		}
		static public void RemoveComponentById(XmlNode rootNode, uint id)
		{
			XmlNode node = rootNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"//*[@{0}='{1}']", XmlTags.XMLATT_ComponentID, id));
			if (node != null)
			{
				XmlNode p = node.ParentNode;
				p.RemoveChild(node);
			}
		}

		static public UInt32 GetClassId(XmlNode node)
		{
			UInt32 classId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
			while (classId == 0)
			{
				node = node.ParentNode;
				if (node == null)
				{
					break;
				}
				classId = XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ClassID);
			}
			return classId;
		}
		static public List<WcfServiceProxy> GetWcfProxyList(LimnorProject project)
		{
			List<WcfServiceProxy> proxyWcfList = project.GetTypedProjectData<List<WcfServiceProxy>>();
			if (proxyWcfList == null)
			{
				string appFolder = Path.GetDirectoryName(Application.ExecutablePath);
				VSPrj.PropertyBag sc = project.GetWcfServiceFiles();
				proxyWcfList = new List<WcfServiceProxy>();
				foreach (Dictionary<string, string> file in sc)
				{
					string fn = file[XmlTags.XMLATT_filename];
					if (string.IsNullOrEmpty(fn))
					{
						MessageBox.Show("DLL file is missing in WCF proxy setting in the project file");
					}
					else
					{
						string fp = Path.Combine(appFolder, fn);
						bool bFound = File.Exists(fp);
						if (!bFound)
						{
							string f0 = Path.Combine(project.ProjectFolder, fn);
							if (!File.Exists(f0))
							{
								MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "DLL file recorded in WCF proxy setting in the project file does not exist. File path: [{0}]", f0));
							}
							else
							{
								File.Copy(f0, fp);
								bFound = true;
							}
						}
						if (bFound)
						{
							Assembly a = Assembly.LoadFile(fp);
							WcfServiceProxy proxy = new WcfServiceProxy(file[XmlTags.XMLATT_filename], a, file[XmlTags.XMLATT_url], file[XmlTags.XMLATT_config]);
							proxyWcfList.Add(proxy);
						}
					}
				}
				project.SetTypedProjectData<List<WcfServiceProxy>>(proxyWcfList);
			}
			return proxyWcfList;
		}
		static public IAction OnCreateAction(object c, IMethodPointer methodToExecute, IMethod scopeMethod, IActionsHolder actsHolder, MultiPanes viewersHolder, XmlNode rootNode)
		{
			IClass holder = c as IClass;
			if (holder == null)
			{
				holder = GetHolder(viewersHolder.Loader.ObjectMap, c);
				if (holder == null)
				{
					throw new DesignerException("Cannot find action executer [{0}]", c);
				}
			}
			IActionMethodPointer m = methodToExecute.Clone() as IActionMethodPointer;
			if (m == null)
			{
				throw new DesignerException("method {0} is not an IActionMethodPointer", methodToExecute.GetType());
			}
			IMemberPointer mp = m as IMemberPointer;
			if (mp == null)
			{
				throw new DesignerException("method {0} is not an IMemberPointer", methodToExecute.GetType());
			}
			else
			{
				mp.SetHolder(holder);
			}

			Type t = m.MethodPointed.ActionType;
			if (t == null)
			{
				t = typeof(ActionClass);
			}

			IAction act = (IAction)Activator.CreateInstance(t, viewersHolder.Loader.GetRootId());
			act.ActionMethod = m;
			act.ActionHolder = actsHolder;
			StringCollection sc = new StringCollection();
			MethodClass mc = scopeMethod as MethodClass;
			if (mc != null)
			{
				MethodClass.GetActionNamesInEditors(sc, mc.MemberId);
			}
			else
			{
				viewersHolder.Loader.GetRootId().GetActionNames(sc);
			}
			act.ActionName = viewersHolder.Loader.CreateMethodName(act.ActionMethod.DefaultActionName, sc);
			if (viewersHolder.Loader.GetRootId().CreateNewAction(act, viewersHolder.Loader.Writer, scopeMethod, viewersHolder.FindForm()))
			{
				return act;
			}
			return null;
		}
		static public void SetActionName(XmlNode rootNode, string actionName, UInt32 actid)
		{
			XmlNode actNode = rootNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"//{0}/{1}[@{2}='{3}']",
				XmlTags.XML_ACTIONS, XmlTags.XML_ACTION, XmlTags.XMLATT_ActionID, actid));
			if (actNode != null)
			{
				XmlUtil.SetAttribute(actNode, XmlTags.XMLATT_NAME, actionName);
			}
		}
		static public bool IsSameValueType(Type t1, Type t2)
		{
			if (t1 == null || typeof(void).Equals(t1))
			{
				return (t2 == null || typeof(void).Equals(t2));
			}
			return t1.Equals(t2);
		}
		static public bool IsSameValueTypes(Type[] t1, Type[] t2)
		{
			if (t1 == null || Type.EmptyTypes.Equals(t1) || t1.Length == 0)
			{
				return (t2 == null || Type.EmptyTypes.Equals(t2) || t2.Length == 0);
			}
			if (t1.Length == t2.Length)
			{
				for (int i = 0; i < t1.Length; i++)
				{
					if (!IsSameValueType(t1[i], t2[i]))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		static public IMethodPointer SelectMethod(ILimnorDesignerLoader loader, IAction action, IMethod context, Form caller)
		{
			MethodClass mc = context as MethodClass;
			FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, mc, EnumObjectSelectType.Method, null, null);
			dlg.SetSelection(action);
			if (dlg.ShowDialog(caller) == DialogResult.OK)
			{
				if (dlg.SelectedMethod != null)
				{
					return dlg.SelectedMethod.CreateMethodPointer(action);
				}
				IPropertyEx pp = dlg.SelectedObject as IPropertyEx;
				if (pp != null)
				{
					return pp.CreateSetterMethodPointer(action);
				}
				IMethodPointer mp = dlg.SelectedObject as IMethodPointer;
				if (mp != null)
				{
					return mp;
				}
				IMethod m = dlg.SelectedObject as IMethod;
				if (m != null)
				{
					return m.CreateMethodPointer(action); ;
				}
			}
			return null;
		}
		static public object SelectMethod(IMethod method, Form caller)
		{
			ILimnorDesignerLoader loader = LimnorProject.ActiveDesignerLoader as ILimnorDesignerLoader;
			if (loader != null)
			{
				LimnorProject project = LimnorProject.ActiveProject;
				return EditMethod(project, method, loader, caller);
			}
			return null;
		}
		static public FrmObjectExplorer CreateSelectMethodDialog(MethodClass scopeMethod, IMethod method)
		{
			ILimnorDesignerLoader loader = LimnorProject.ActiveDesignerLoader as ILimnorDesignerLoader;
			if (loader != null)
			{
				FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, scopeMethod, EnumObjectSelectType.Method, null, null);
				dlg.SetSelection(method);
				return dlg;
			}
			return null;
		}
		static public object EditMethod(LimnorProject project, IMethod method, ILimnorDesignerLoader loader, Form caller)
		{
			FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, null, EnumObjectSelectType.Method, null, null);
			dlg.SetSelection(method);
			if (dlg.ShowDialog(caller) == DialogResult.OK)
			{
				PropertyClass cp = dlg.SelectedObject as PropertyClass;
				if (cp != null)
				{
					return cp;
				}
				else
				{
					PropertyPointer p = dlg.SelectedObject as PropertyPointer;
					if (p != null)
					{
						return p;
					}
					else
					{
						IMethod m = dlg.SelectedObject as IMethod;
						return m;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// select a method to create an action, edit the action
		/// </summary>
		/// <param name="project"></param>
		/// <param name="action"></param>
		/// <param name="loader"></param>
		/// <param name="caller"></param>
		/// <returns></returns>
		static public IAction EditAction(LimnorProject project, IAction action, ILimnorDesignerLoader loader, IMethod context, Form caller)
		{
			ClassPointer rootId = loader.GetRootId();
			ObjectIDmap map = loader.ObjectMap;
			XmlNode node = loader.Node;
			IAction act;
			if (action != null)
			{
				act = (IAction)action.Clone();
			}
			else
			{
				act = new ActionClass(loader.GetRootId());
			}
			while (true)
			{
				IActionMethodPointer mp = SelectMethod(loader, act, context, caller) as IActionMethodPointer;
				if (mp == null)
					break;
				else
				{
					if (!(act.ActionMethod != null && act.ActionMethod.IsSameMethod(mp)))
					{
						ActionClass a = new ActionClass(act.Class);
						a.ActionId = act.ActionId;
						a.ActionName = act.ActionName;
						a.ActionMethod = mp;
						a.Description = act.Description;
						act = a;
					}
					//edit the action
					FormActionParameters dlgData = new FormActionParameters();
					dlgData.SetScopeMethod(context);
					dlgData.LoadAction(act, node);
					DialogResult ret = dlgData.ShowDialog(caller);
					if (ret == DialogResult.OK)
					{
						rootId.SaveAction(act, loader.Writer);
						ILimnorDesignPane pane = map.Project.GetTypedData<ILimnorDesignPane>(map.ClassId);//, typeof(ILimnorDesignPane));
						pane.OnActionChanged(rootId.ClassId, act, false);
						pane.OnNotifyChanges();
						return act;
					}
					else if (ret != DialogResult.Retry)
					{
						return null;
					}
				}
			}
			return null;
		}
		static public IClass GetMemberOwner(IObjectPointer p)
		{
			IClass ic = p as IClass;
			if (ic != null)
			{
				return ic;
			}
			IObjectPointer po = p;
			while (po != null)
			{
				ic = po as IClass;
				if (ic != null)
				{
					return ic;
				}
				po = po.Owner;
			}
			return null;
		}
		static public ISourceValuePointer GetSourceValue(IObjectPointer p)
		{
			if (p != null)
			{
				ISourceValuePointer sp = p as ISourceValuePointer;
				while (sp == null && p.Owner != null)
				{
					p = p.Owner;
					sp = p as ISourceValuePointer;
				}
				return sp;
			}
			return null;
		}
		static public IClass GetMemberOwner(ISourceValuePointer p)
		{
			IClass ic = p as IClass;
			if (ic != null)
			{
				return ic;
			}
			IObjectPointer po = p.ValueOwner as IObjectPointer;
			while (po != null)
			{
				ic = po as IClass;
				if (ic != null)
				{
					return ic;
				}
				po = po.Owner;
			}
			return null;
		}
		static public bool IsWebClientObject(IClass obj)
		{
			if (obj == null)
				return false;
			if (JsTypeAttribute.IsJsType(obj.ObjectType))
			{
				return true;
			}
			if (PhpTypeAttribute.IsPhpType(obj.ObjectType))
			{
				return false;
			}
			if ((obj.ObjectType.GetInterface("IWebClientControl") != null))
			{
				return true;
			}
			object[] vs = obj.ObjectType.GetCustomAttributes(typeof(WebClientClassAttribute), true);
			if (vs != null && vs.Length > 0)
			{
				return true;
			}
			return false;
		}
		static public bool CanBeWebClientObject(IObjectPointer obj)
		{
			if (obj == null)
				return false;
			if (JsTypeAttribute.IsJsType(obj.ObjectType))
			{
				return true;
			}
			if (PhpTypeAttribute.IsPhpType(obj.ObjectType))
			{
				return false;
			}
			if (obj is ActionBranchParameterPointer)
				return true;
			return IsWebClientObject(obj);
		}
		static public bool IsDataFieldPointer(IObjectPointer obj)
		{
			PropertyPointer pp = obj.Owner as PropertyPointer;
			if (pp != null && pp.Owner != null)
			{
				if (typeof(EasyDataSet).IsAssignableFrom(pp.Owner.ObjectType))
				{
					if (string.CompareOrdinal("Fields", pp.Name) == 0)
					{
						return true;
					}
				}
			}
			return false;
		}
		static public bool CanBeWebServerObject(IObjectPointer obj)
		{
			if (obj == null)
				return false;
			PropertyPointer pp = obj as PropertyPointer;
			if (pp != null && pp.Owner != null)
			{
				Type ot = pp.Owner.ObjectType;
				if (ot != null)
				{
					if (WebServerMemberAttribute.IsWebServerMember(ot.GetCustomAttributes(true)))
					{
						return true;
					}
				}
			}
			if (JsTypeAttribute.IsJsType(obj.ObjectType))
			{
				return false;
			}
			if (PhpTypeAttribute.IsPhpType(obj.ObjectType))
			{
				return true;
			}
			if (obj is ActionBranchParameterPointer)
				return true;
			return !IsWebClientObject(obj);
		}
		static public bool IsWebClientObject(IObjectPointer obj)
		{
			if (obj != null)
			{
				if (JsTypeAttribute.IsJsType(obj.ObjectType))
				{
					return true;
				}
				if (PhpTypeAttribute.IsPhpType(obj.ObjectType))
				{
					return false;
				}
				if (obj.RunAt == EnumWebRunAt.Server)
				{
					return false;
				}
				LocalVariable loc = obj as LocalVariable;
				if (loc != null)
				{
					if (VPLUtil.CompilerContext_PHP)
					{
						return false;
					}
					return true;
				}
				TypePointer tp = obj as TypePointer;
				if (tp != null)
				{
					if (tp.ClassType != null)
					{
						if (tp.ClassType.GetInterface("IWebClientComponent") != null)
						{
							return true;
						}
						if (typeof(HtmlElement_Base).IsAssignableFrom(tp.ClassType))
						{
							return true;
						}
						if (typeof(HtmlElement_document).IsAssignableFrom(tp.ClassType))
						{
							return true;
						}
						object[] vs = tp.ClassType.GetCustomAttributes(typeof(WebClientClassAttribute), true);
						if (vs != null && vs.Length > 0 && vs[0] != null)
						{
							return true;
						}
					}
					return false;
				}
				ClassPointer root = obj.RootPointer;
				if (root != null)
				{
					if (root.RunAt == EnumWebRunAt.Server)
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		static public bool IsWebServerObject(IObjectPointer obj)
		{
			if (obj == null)
				return false;
			PropertyPointer pp = obj as PropertyPointer;
			if (pp != null && pp.Owner != null)
			{
				Type ot = pp.Owner.ObjectType;
				if (ot != null)
				{
					if (WebServerMemberAttribute.IsWebServerMember(ot.GetCustomAttributes(true)))
					{
						return true;
					}
				}
			}
			if (JsTypeAttribute.IsJsType(obj.ObjectType))
			{
				return false;
			}
			if (PhpTypeAttribute.IsPhpType(obj.ObjectType))
			{
				return true;
			}
			if (obj != null)
			{
				if (obj.RunAt == EnumWebRunAt.Client)
				{
					return false;
				}
				LocalVariable loc = obj as LocalVariable;
				if (loc != null)
				{
					if (VPLUtil.CompilerContext_JS)
					{
						return false;
					}
					return true;
				}
				TypePointer tp = obj as TypePointer;
				if (tp != null)
				{
					if (tp.ClassType != null)
					{
						if (tp.ClassType.GetInterface("IWebClientComponent") != null)
						{
							return false;
						}
						if (typeof(HtmlElement_Base).IsAssignableFrom(tp.ClassType))
						{
							return false;
						}
						if (typeof(HtmlElement_document).IsAssignableFrom(tp.ClassType))
						{
							return false;
						}
						object[] vs = tp.ClassType.GetCustomAttributes(typeof(WebClientClassAttribute), true);
						if (vs != null && vs.Length > 0 && vs[0] != null)
						{
							return false;
						}
					}
					return true;
				}
				return true;
			}
			return true;
		}
		/// <summary>
		/// used by PropEditorPropertyPointer
		/// </summary>
		/// <param name="property">initial property selection</param>
		/// <param name="methodScope"></param>
		/// <param name="typeScope"></param>
		/// <returns>dialog box</returns>
		static public FrmObjectExplorer GetPropertySelector(IObjectPointer property, IMethod methodScope, DataTypePointer typeScope)
		{
			ILimnorDesignerLoader loader = LimnorProject.ActiveDesignerLoader as ILimnorDesignerLoader;
			if (loader != null)
			{
				MethodClass mc = methodScope as MethodClass;
				FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, mc, EnumObjectSelectType.Object, typeScope, null);
				dlg.AddNullPointerNode(loader.GetRootId());
				dlg.SetSelection(property);
				return dlg;
			}
			return null;
		}
		static public bool IsFormSubmission(IAction act)
		{
			if (act != null && act.ActionMethod != null && act.ActionMethod.Owner != null)
			{
				IFormSubmitter fs = act.ActionMethod.Owner.ObjectInstance as IFormSubmitter;
				if (fs != null)
				{
					if (fs.IsSubmissionMethod(act.ActionMethod.MethodName))
					{
						return true;
					}
				}
			}
			return false;
		}
		static public FrmObjectExplorer CreateSelectActionDialog(MethodClass method, IAction action, IEvent eventToHandle, bool multipleSelection)
		{
			IMethod scopeMethod = method;
			ClassPointer root = method.RootPointer;
			if (root != null && root.Project != null)
			{
				ILimnorDesignPane dp = root.Project.GetTypedData<ILimnorDesignPane>(root.ClassId);
				if (dp != null)
				{
					ILimnorDesignerLoader loader = dp.Loader;
					if (loader != null)
					{
						IActionsHolder actsHolder = method.ActionsHolder;
						string eventScope = null;
						FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, method, EnumObjectSelectType.Action, null, eventScope);
						dlg.Text = "Select an action";
						dlg.SetScopeMethod(scopeMethod, actsHolder);
						if (scopeMethod != null)
						{
							dlg.LoadMethodParameters(scopeMethod);
						}
						dlg.SelectActionCollectionNode(loader.ClassID, false);
						dlg.SetMultipleSelection(multipleSelection);
						dlg.SetSelection(action);

						if (eventToHandle != null)
						{
							dlg.AddEventHandlerNode(loader.GetRootId().IsWebPage, eventToHandle);
						}
						return dlg;
					}
				}
			}
			return null;
		}
		static public List<IAction> SelectAction(ILimnorDesignerLoader loader, IAction action, IEvent eventToHandle, bool multipleSelection, IMethod scopeMethod, IActionsHolder actsHolder, Form caller)
		{
			FormProgress.ShowProgress("Loading action selection dialogue. Please wait ...");
			DesignUtil.LogIdeProfile("Loading action selection dialogue.");
			try
			{
				string eventScope = null;
				MethodClass mc = scopeMethod as MethodClass;
				FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, mc, EnumObjectSelectType.Action, null, eventScope);
				dlg.Text = "Select an action";
				dlg.SetScopeMethod(scopeMethod, actsHolder);
				dlg.SetMultipleSelection(multipleSelection);
				dlg.SetSelection(action);

				if (eventToHandle != null)
				{
					dlg.AddEventHandlerNode(loader.GetRootId().IsWebPage, eventToHandle);
				}
				DesignUtil.LogIdeProfile("Showing dialogue and wait for user selection");
				FormProgress.HideProgress();
				if (dlg.ShowDialog(caller) == DialogResult.OK)
				{
					DesignUtil.LogIdeProfile("User made action selection");
					List<IAction> list = new List<IAction>();
					if (scopeMethod != null)
					{
						AssignHandler ah = dlg.SelectedAction as AssignHandler;
						if (ah != null)
						{
							list.Add(ah);
							return list;
						}
					}
					if (multipleSelection)
					{
						ArrayList nodes = dlg.GetSelectedNodes();
						if (nodes != null && nodes.Count > 0)
						{
							for (int i = 0; i < nodes.Count; i++)
							{
								TreeNodeAction ta = nodes[i] as TreeNodeAction;
								if (ta != null)
								{
									if (scopeMethod != null)
									{
										if (IsFormSubmission(ta.Action))
										{
											MessageBox.Show(caller, "Submission action cannot be used inside a method", "Select action", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
											continue;
										}
									}
									list.Add(ta.Action);
								}
							}
						}
					}
					else
					{
						bool ok = true;
						if (scopeMethod != null)
						{
							if (IsFormSubmission(dlg.SelectedAction))
							{
								MessageBox.Show(caller, "Submission action cannot be used inside a method", "Select action", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
								ok = false;
							}
						}
						if (ok)
						{
							list.Add(dlg.SelectedAction);
						}
					}
					return list;
				}
				else
				{
					DesignUtil.LogIdeProfile("User canceled action selection");
				}
				return null;
			}
			catch
			{
				throw;
			}
			finally
			{
				FormProgress.HideProgress();
			}
		}
		static public object SelectMethod(ILimnorDesignerLoader loader, IObjectPointer startSelect, Form caller)
		{
			string eventScope = null;
			FrmObjectExplorer dlg = FrmObjectExplorer.LoadRootObject(loader, null, EnumObjectSelectType.Method, null, eventScope);
			dlg.SetSelection(startSelect);
			if (dlg.ShowDialog(caller) == DialogResult.OK)
			{
				if (dlg.SelectedMethod != null)
				{
					return dlg.SelectedMethod;
				}
				IPropertyEx pp = dlg.SelectedObject as IPropertyEx;
				if (pp != null)
				{
					return pp;
				}
				PropertyClass pc = dlg.SelectedObject as PropertyClass;
				if (pc != null)
				{
					return pc;
				}
				CustomMethodPointer cmp = dlg.SelectedObject as CustomMethodPointer;
				if (cmp != null)
				{
					return cmp;
				}
				return dlg.SelectedObject;
			}
			return null;
		}
		/// <summary>
		/// select lib type or root component for non-hosted usages such as method parameters and return values
		/// </summary>
		/// <param name="project"></param>
		/// <param name="initDataType"></param>
		/// <param name="caller"></param>
		/// <returns></returns>
		static public DataTypePointer SelectDataType(LimnorProject project, ClassPointer root, MethodClass scopeMethod, EnumObjectSelectType selectType, DataTypePointer initDataType, DataTypePointer scope, Type typeAttribute, Form caller)
		{
			FrmObjectExplorer dlg = new FrmObjectExplorer();
			dlg.LoadProject(project, scopeMethod, selectType, false, scope, typeAttribute);
			dlg.SetSelection(initDataType);
			dlg.SetRootPointer(root);
			if (dlg.ShowDialog(caller) == DialogResult.OK)
			{
				if (dlg.SelectedDataType == null)
				{
					MessageBox.Show(caller, "The selected object has not been converted to a data type", "Select data type", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					if (typeof(List<object>).Equals(dlg.SelectedDataType.BaseClassType))
					{
						DlgSelectTypeParameters dlgP = new DlgSelectTypeParameters();
						Type t = typeof(List<object>).GetGenericTypeDefinition();
						dlgP.LoadData(t, project);
						if (dlgP.ShowDialog(caller) == DialogResult.OK)
						{
							dlg.SelectedDataType.SetDataType(t);
							dlg.SelectedDataType.TypeParameters = dlgP._holder.Results;
						}
						else
						{
							return null;
						}
					}
					else if (dlg.SelectedDataType.ContainsGenericParameters())
					{
						if (dlg.SelectedDataType.TypeParameters != null && dlg.SelectedDataType.TypeParameters.Length > 0)
						{
						}
						else
						{
							DlgSelectTypeParameters dlgP = new DlgSelectTypeParameters();
							dlgP.LoadData(dlg.SelectedDataType.BaseClassType, project);
							if (dlgP.ShowDialog(caller) == DialogResult.OK)
							{
								dlg.SelectedDataType.TypeParameters = dlgP._holder.Results;
							}
							else
							{
								return null;
							}
						}
					}
				}
				return dlg.SelectedDataType;
			}
			return null;
		}
		/// <summary>
		/// select lib type or root component for non-hosted usages such as method parameters and return values
		/// </summary>
		/// <param name="project"></param>
		/// <param name="initDataType"></param>
		/// <returns></returns>
		static public FrmObjectExplorer GetDataTypeSelectionDialogue(LimnorProject project, MethodClass scopeMethod, DataTypePointer initDataType, bool forMethodReturn, DataTypePointer scope, Type typeAttribute)
		{
			FrmObjectExplorer dlg = new FrmObjectExplorer();
			dlg.LoadProject(project, scopeMethod, EnumObjectSelectType.Type, forMethodReturn, scope, typeAttribute);
			dlg.SetSelection(initDataType);
			return dlg;
		}
		static public List<ClassPointer> GetAllComponentClassRef(LimnorProject project)
		{
			List<ClassPointer> classes = new List<ClassPointer>();
			if (project == null)
			{
				if (LimnorProject.ActiveProject != null)
					project = LimnorProject.ActiveProject;
				else if (LimnorProject.ActiveDesignerLoader != null)
					project = LimnorProject.ActiveDesignerLoader.ActiveProject;
				if (project == null)
				{
					throw new DesignerException("Active project not found");
				}
			}
			string[] files = project.GetComponentFiles();
			for (int i = 0; i < files.Length; i++)
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(files[i]);
				UInt32 classId = XmlUtil.GetAttributeUInt(doc.DocumentElement, XmlTags.XMLATT_ClassID);
				if (classId == 0)
				{
					classId = LimnorProject.GetUnsavedClassId(files[i]);
				}
				if (classId != 0)
				{
					ClassPointer cp = ClassPointer.CreateClassPointer(classId, project);
					classes.Add(cp);
				}
			}
			return classes;
		}
		static public List<XmlNode> GetAllComponentXmlNode(LimnorProject project)
		{
			List<XmlNode> nodes = new List<XmlNode>();
			string[] files = project.GetComponentFiles();
			for (int i = 0; i < files.Length; i++)
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(files[i]);
				UInt32 classId = XmlUtil.GetAttributeUInt(doc.DocumentElement, XmlTags.XMLATT_ClassID);
				if (classId == 0)
				{
					classId = LimnorProject.GetUnsavedClassId(files[i]);
				}
				if (classId == 0)
				{
					throw new DesignerException("Class id not found in {0}", files[i]);
				}
				ILimnorDesignPane pane = project.GetTypedData<ILimnorDesignPane>(classId);
				if (pane != null)
				{
					nodes.Add(pane.RootXmlNode);
				}
				else
				{
					nodes.Add(doc.DocumentElement);
				}
			}
			return nodes;
		}

		/// <summary>
		/// the action classes only contains ID and name
		/// </summary>
		/// <param name="rootNode"></param>
		/// <returns></returns>
		static public List<NameID> GetActionNames(XmlNode rootNode)
		{
			List<NameID> acts = new List<NameID>();
			XmlNodeList actionNodes = rootNode.SelectNodes(string.Format(CultureInfo.InvariantCulture,
				"//{0}/{1}", XmlTags.XML_ACTIONS, XmlTags.XML_ACTION));
			if (actionNodes.Count > 0)
			{
				foreach (XmlNode nd in actionNodes)
				{
					NameID a = new NameID();
					a.Id = XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_ActionID);
					a.Name = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_NAME);
					acts.Add(a);
				}
			}
			return acts;
		}
		static public XmlNodeList GetMethodNodes(XmlNode rootNode)
		{
			return rootNode.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}", XmlTags.XML_METHODS, XmlTags.XML_METHOD));
		}
		static public XmlNode GetMethodVariableNode(XmlNode rootNode, UInt32 memberId)
		{
			return rootNode.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				 "//{0}[@{1}='ComponentIconList']/{2}[@{3}={4}]/{5}", XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_Item, XmlTags.XMLATT_ComponentID, memberId, XmlTags.XML_Var));

		}
		static public Dictionary<string, UInt32> GetMethodNames(XmlNode rootNode)
		{
			Dictionary<string, UInt32> acts = new Dictionary<string, UInt32>();
			XmlNode ndMethodList = rootNode.SelectSingleNode(XmlTags.XML_METHODS);
			if (ndMethodList != null)
			{
				XmlNodeList methodNodes = ndMethodList.SelectNodes(XmlTags.XML_METHOD);
				if (methodNodes.Count > 0)
				{
					foreach (XmlNode nd in methodNodes)
					{
						acts.Add(XmlUtil.GetAttribute(nd, XmlTags.XMLATT_NAME), XmlUtil.GetAttributeUInt(nd, XmlTags.XMLATT_MethodID));
					}
				}
			}
			Type t = XmlUtil.GetLibTypeAttribute(rootNode);
			MethodInfo[] mifs = t.GetMethods();
			for (int i = 0; i < mifs.Length; i++)
			{
				if (!acts.ContainsKey(mifs[i].Name))
				{
					acts.Add(mifs[i].Name, 0);
				}
			}
			return acts;
		}

		static public void SaveCustomProperty(XmlNode rootNode, XmlObjectWriter writer, PropertyClass property)
		{
			XmlNode propNode = SerializeUtil.GetCustomPropertyNode(rootNode, property.MemberId);
			writer.WriteObjectToNode(propNode, property);
		}

		public static void RemoveActionFromHandlers(List<EventAction> handlers, TaskID task)
		{
			foreach (EventAction ea in handlers)
			{
				ea.RemoveAction(task.TaskId);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="node">
		/// the node contains Actions/Action and Handlers; its class Id must be the same as that of task, but it is not checked.
		/// 
		/// </param>
		/// <param name="task">action to be removed from Actions</param>
		public static void RemoveActionFromXmlNode(XmlNode node, UInt32 classId, UInt32 actionId)
		{
			string tag1, tag2;
			tag1 = XmlTags.XML_ACTIONS;
			tag2 = XmlTags.XML_ACTION;
			//remove from Actions/Action
			XmlNode nodeAct = node.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}[@{2}='{3}']", tag1, tag2, XmlTags.XMLATT_ActionID, actionId));
			if (nodeAct != null)
			{
				XmlNode p = nodeAct.ParentNode;
				p.RemoveChild(nodeAct);
			}
			//remove from HandlerList/Handler/{TaskIDList Property}/Item/{ActionId Property}
			XmlNodeList nodeList = node.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}/{2}[@name='TaskIDList']/Item/{2}[@name='ActionId']", XmlTags.XML_HANDLERLISTS, XmlTags.XML_HANDLER, XmlTags.XML_PROPERTY
					));
			List<XmlNode> delete = new List<XmlNode>();
			foreach (XmlNode nd in nodeList)
			{
				if (Convert.ToUInt32(nd.InnerText) == actionId)
				{
					XmlNode p = nd.ParentNode; //Item node
					XmlNode g = p.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					 "{0}[@name='ClassId']", XmlTags.XML_PROPERTY));
					if (Convert.ToUInt32(g.InnerText) == classId)
					{
						delete.Add(nd);
					}
				}
			}
			foreach (XmlNode nd in delete)
			{
				XmlNode item = nd.ParentNode;
				XmlNode TaskIDList = item.ParentNode;
				TaskIDList.RemoveChild(item);
				if (TaskIDList.ChildNodes.Count == 0)
				{
					XmlNode handler = TaskIDList.ParentNode;
					XmlNode handlerList = handler.ParentNode;
					handlerList.RemoveChild(handler);
				}
			}
		}
		/// <summary>
		/// set action id and action name for a new action.
		/// the new action has not been saved to xml yet
		/// </summary>
		/// <param name="rootNode"></param>
		/// <param name="action"></param>
		static public void InitializeNewAction(XmlNode rootNode, IAction action)
		{
			action.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
			string actNameBase = action.ActionName;
			List<NameID> acts = GetActionNames(rootNode);
			while (true)
			{
				bool found = false;
				foreach (NameID act in acts)
				{
					if (act.Id == action.ActionId)
					{
						found = true;
						action.ActionId = (UInt32)(Guid.NewGuid().GetHashCode());
						break;
					}
				}
				if (!found)
					break;
			}
			int n = 1;
			while (true)
			{
				bool found = false;
				foreach (NameID act in acts)
				{
					if (act.Name == action.ActionName)
					{
						found = true;
						n++;
						action.ActionName = actNameBase + n.ToString();
						break;
					}
				}
				if (!found)
					break;
			}
		}
		public static bool IsActionNameInUse(XmlNode rootNode, string newName, UInt32 actionId)
		{
			List<NameID> acts = GetActionNames(rootNode);
			foreach (NameID act in acts)
			{
				if (act.Id != actionId)
				{
					if (act.Name == newName)
					{
						return true;
					}
				}
			}
			return false;
		}
		public static IMethodPointer EditFrequentlyUsedMethodList(LimnorProject project, XmlNode node, LimnorContextMenuCollection type, ILimnorDesignPane designPane, Form caller)
		{
			DlgSelectMethod dlg = new DlgSelectMethod();
			dlg.SetProject(project);
			dlg.LoadData(type);
			DialogResult ret = dlg.ShowDialog(caller);
			if (dlg.FrequentlyUsedMethodsChanged)
			{
				type.RemoveMenuCollection();
				designPane.ResetContextMenu();
			}
			if (ret == DialogResult.OK)
			{
				MethodClass mc = dlg.ReturnMethodInfo as MethodClass;
				if (mc != null)
				{
					return mc.CreatePointer(type.Owner);
				}
				return dlg.ReturnMethodInfo.CreateMethodPointer(null);
			}
			return null;
		}
		public static IProperty EditFrequentlyUsedPropertyList(LimnorProject project, XmlNode node, LimnorContextMenuCollection type, ILimnorDesignPane designPane, Form caller)
		{
			DlgSelectProperty dlg = new DlgSelectProperty();
			dlg.SetProject(project);
			dlg.LoadData(type);
			DialogResult ret = dlg.ShowDialog(caller);
			if (dlg.FrequentlyUsedMethodsChanged)
			{
				type.RemoveMenuCollection();
				designPane.ResetContextMenu();
			}
			if (ret == DialogResult.OK)
			{
				return dlg.ReturnPropertyInfo;
			}
			return null;
		}
		public static ActionClass CreateSetPropertyAction(IPropertyEx pp, ILimnorDesignPane designPane, IMethod scopeMethod, IActionsHolder actsHolder, Form caller)
		{
			ActionClass act = new ActionClass(designPane.Loader.GetRootId());
			act.ActionMethod = pp.CreateSetterMethodPointer(act);
			act.ActionName = string.Format(CultureInfo.InvariantCulture, "{0}.Set{1}", pp.Holder.ExpressionDisplay, pp.Name);
			act.ActionHolder = actsHolder;
			if (designPane.Loader.GetRootId().CreateNewAction(act, designPane.Loader.Writer, scopeMethod, caller))
			{
				return act;
			}
			return null;
		}
		public static bool IsStatic(PropertyInfo p)
		{
			if (p.CanRead)
			{
				MethodInfo mif = p.GetGetMethod(true);
				return mif.IsStatic;
			}
			else if (p.CanWrite)
			{
				MethodInfo mif = p.GetSetMethod(true);
				return mif.IsStatic;
			}
			return false;
		}
		public static bool IsStatic(EventInfo e)
		{
			EventInfoX x = e as EventInfoX;
			if (e != null)
			{
				return x.IsStatic;
			}
			EventInfoInterface eii = e as EventInfoInterface;
			if (eii != null)
			{
				return eii.IsStatic;
			}
			MethodInfo mif = e.GetAddMethod(true);
			if (mif != null)
			{
				return mif.IsStatic;
			}
			if (e.EventHandlerType != null)
			{
				mif = e.EventHandlerType.GetMethod("Invoke");
				if (mif != null)
				{
					return mif.IsStatic;
				}
			}
			mif = e.GetAddMethod();
			if (mif != null)
			{
				return mif.IsStatic;
			}
			return false;
		}
		public static bool HasStaticOwner(IObjectPointer op)
		{
			while (op != null)
			{
				if (op.IsStatic)
					return true;
				op = op.Owner;
			}
			return false;
		}
		public static bool IsApp(Type t)
		{
			if (t == null)
			{
				throw new DesignerException("Calling IsApp with null type");
			}
			object[] attrs = t.GetCustomAttributes(typeof(ProjectMainComponentAttribute), true);
			if (attrs != null && attrs.Length > 0)
			{
				ProjectMainComponentAttribute a = (ProjectMainComponentAttribute)attrs[0];
				return LimnorProject.IsApp(a.ProjectType);
			}
			return false;
		}
		public static string GetObjectNameById(UInt32 id, XmlNode node)
		{
			if (node == null)
			{
				throw new DesignerException("XmlNode is null when calling GetObjectNameById for member id {0}", id);
			}
			//try root
			if (XmlUtil.GetAttributeUInt(node, XmlTags.XMLATT_ComponentID) == id)
			{
				return XmlUtil.GetAttribute(node, XmlTags.XMLATT_NAME);
			}
			//try it as a Control
			XmlNode objNode = node.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"{0}[@{1}='Controls']/{2}[@{3}='{4}']",
							XmlTags.XML_PROPERTY, XmlTags.XMLATT_NAME, XmlTags.XML_Item, XmlTags.XMLATT_ComponentID, id));
			if (objNode != null)
			{
				return XmlUtil.GetAttribute(objNode, XmlTags.XMLATT_NAME);
			}
			//try it as a non-Control
			objNode = node.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"{0}[@{1}='{2}']",
							XmlTags.XML_Object, XmlTags.XMLATT_ComponentID, id));
			if (objNode != null)
			{
				return XmlUtil.GetAttribute(objNode, XmlTags.XMLATT_NAME);
			}
			//
			return "?";
		}

		public static ClassInstancePointer GetClassRefByInstance(ObjectIDmap map, object obj)
		{
			foreach (KeyValuePair<object, uint> kv in map)
			{
				if (kv.Key is ClassInstancePointer)
				{
					ClassInstancePointer cr = (ClassInstancePointer)kv.Key;
					if (cr.ObjectInstance == obj)
					{
						return cr;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// generate an IObjectPointer instance pointing to obj
		/// </summary>
		/// <param name="map"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static IClass CreateObjectPointer(ObjectIDmap map, object obj)
		{
			IClass o = obj as ClassInstancePointer;
			if (o == null)
			{
				UInt32 memberId = map.GetObjectID(obj);
				if (memberId == 0)
				{
					o = GetClassRefByInstance(map, obj);
				}
				else
				{
					ClassPointer rt = ClassPointer.CreateClassPointer(map);
					if (memberId == map.MemberId)
					{
						o = rt;
					}
					else
					{
						o = MemberComponentId.CreateMemberComponentId(rt, obj, memberId);
					}
				}
			}
			return o;
		}
		/// <summary>
		/// get an IClass mapping to the object
		/// </summary>
		/// <param name="map"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static IClass GetHolder(ObjectIDmap map, object obj)
		{
			MemberComponentIdCustom mc = obj as MemberComponentIdCustom;
			if (mc != null)
			{
				return mc.Pointer;
			}
			ClassProperties cp = obj as ClassProperties;
			if (cp != null)
			{
				return (IClass)cp.Pointer;
			}
			IClass ic = obj as IClass;
			if (ic != null)
			{
				return ic;
			}
			UInt32 memberId = map.GetObjectID(obj);
			if (memberId == 0)
			{
				return GetClassRefByInstance(map, obj);
			}
			else
			{
				ClassPointer rt = ClassPointer.CreateClassPointer(map);
				if (memberId == map.MemberId)
				{
					return rt;
				}
				else
				{
					MemberComponentId m = MemberComponentId.CreateMemberComponentId(rt, obj, memberId);
					MemberComponentIdCustom c = m as MemberComponentIdCustom;
					if (c != null)
					{
						return c.Pointer;
					}
					return m;
				}
			}
		}
		public static ClassInstancePointer GetClassRef(UInt64 wholeId, ObjectIDmap map)
		{
			foreach (KeyValuePair<object, uint> kv in map)
			{
				ClassInstancePointer cr = kv.Key as ClassInstancePointer;
				if (cr != null)
				{
					if (cr.WholeId == wholeId)
					{
						return cr;
					}
				}
			}
			List<ObjectIDmap> maps = map.ChildMaps;
			if (maps != null)
			{
				foreach (ObjectIDmap m in maps)
				{
					ClassInstancePointer cr = GetClassRef(wholeId, m);
					if (cr != null)
					{
						return cr;
					}
				}
			}
			return null;
		}
		public static UInt32 GetClassId(IObjectPointer p)
		{
			ClassPointer r = p as ClassPointer;
			while (r == null && p != null)
			{
				p = p.Owner;
				r = p as ClassPointer;
			}
			if (r != null)
			{
				return r.ClassId;
			}
			return 0;
		}
		public static bool HasFormControlParent(IObjectPointer op)
		{
			if (op == null)
			{
				return false;
			}
			if (typeof(Control).IsAssignableFrom(op.ObjectType))
			{
				return true;
			}
			else
			{
				DataTypePointer dt = op as DataTypePointer;
				if (dt != null)
				{
					return false;
				}
				PropertyPointer pp = op as PropertyPointer;
				if (pp != null)
				{
					if (pp.Owner != null)
					{
						if (typeof(Control).IsAssignableFrom(pp.Owner.ObjectType))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		public static void RemoveEventHandler(EventPointer ep, TaskID act, ClassPointer root)
		{
			XmlObjectReader xr = root.ObjectList.Reader;
			XmlNodeList nodes = root.XmlData.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}/{1}",
				XmlTags.XML_HANDLERLISTS, XmlTags.XML_HANDLER));
			XmlNode taskNode = null;
			EventAction ea0 = null;
			foreach (XmlNode nd in nodes)
			{
				EventAction ea = (EventAction)xr.ReadObject(nd, null);
				ea.TaskIDList.RemoveInvalidTasks();
				if (ea.Event.IsSameObjectRef(ep))
				{
					if (ea.TaskIDList != null)
					{
						foreach (TaskID td in ea.TaskIDList)
						{
							if (td.WholeTaskId == act.WholeTaskId)
							{
								ea.RemoveAction(act.TaskId);
								ea0 = ea;
								taskNode = nd;
								break;
							}
						}
						if (taskNode != null)
						{
							break;
						}
					}
				}
			}
			if (taskNode == null)
			{
				throw new DesignerException("event handler {0} not found for event {1}", act, ep);
			}
			XmlNode p = taskNode.ParentNode;
			p.RemoveChild(taskNode);
			ILimnorDesignPane pane = root.ObjectList.Project.GetTypedData<ILimnorDesignPane>(root.ClassId);
			pane.OnRemoveEventHandler(ea0, act);
			pane.OnNotifyChanges();
		}
		public static string TypeIconFilePath
		{
			get
			{
				string dir = GetApplicationDataFolder();
				return System.IO.Path.Combine(dir, "TypeIcon.xml");
			}
		}
		public static string GetLauncherConfigFile()
		{
			return System.IO.Path.Combine(GetApplicationDataFolder(), "launcher.cfg.xml");
		}
		public static XmlDocument GetTypeIconDocument()
		{
			XmlDocument doc = new XmlDocument();
			string file = TypeIconFilePath;
			if (System.IO.File.Exists(file))
			{
				doc.Load(file);
			}
			return doc;
		}
		public static Image GetTypeIcon(Type t)
		{
			Image img = null;
			XmlDocument doc = GetTypeIconDocument();
			XmlNode root = doc.DocumentElement;
			if (root != null)
			{
				XmlNode iconNode = root.SelectSingleNode(
					string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}[@{1}='{2}']", XmlTags.XML_Item, XmlTags.XMLATT_NAME, t.FullName));
				if (iconNode != null)
				{
					byte[] bs = Convert.FromBase64String(iconNode.InnerText);
					BinaryFormatter formatter = new BinaryFormatter();
					MemoryStream stream = new MemoryStream(bs);
					img = (Image)formatter.Deserialize(stream);
				}
			}
			return img;
		}
		/// <summary>
		/// folder for saving application configurations
		/// </summary>
		/// <returns></returns>
		public static string GetApplicationDataFolder()
		{
			string sDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Limnor Studio");
			if (!System.IO.Directory.Exists(sDir))
			{
				System.IO.Directory.CreateDirectory(sDir);
			}
			return sDir;
		}
		public static Image ChangeTypeIcon(Type t, Image currentIcon, Form caller)
		{
			const int IconSize = 16;
			string file = null;
			Image img = null;
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Title = "Select an icon image for " + t.Name;
			dlg.Filter = "Image|*.bmp;*.jpg;*.ico;*.gif;*.tiff";
			try
			{
				if (dlg.ShowDialog(caller) == DialogResult.OK)
				{
					file = dlg.FileName;
					img = Image.FromFile(file, true);
					if (img.Width > IconSize || img.Height > IconSize)
					{
						img = null;
						MessageBox.Show(caller, string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"The image is too big. Use an image that is less than or equal to {0} pixels on each side.", IconSize),
							"Change type icon", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					else
					{
						XmlDocument doc = GetTypeIconDocument();
						XmlNode root = doc.DocumentElement;
						if (root == null)
						{
							root = doc.CreateElement("TypeIcon");
							doc.AppendChild(root);
						}
						XmlNode iconNode = root.SelectSingleNode(
							string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"{0}[@{1}='{2}']", XmlTags.XML_Item, XmlTags.XMLATT_NAME, t.FullName));
						if (iconNode == null)
						{
							iconNode = doc.CreateElement(XmlTags.XML_Item);
							root.AppendChild(iconNode);
							XmlUtil.SetAttribute(iconNode, XmlTags.XMLATT_NAME, t.FullName);
						}
						BinaryFormatter formatter = new BinaryFormatter();
						MemoryStream stream = new MemoryStream();

						formatter.Serialize(stream, img);
						stream.Close();
						byte[] bs = stream.ToArray();
						iconNode.InnerText = Convert.ToBase64String(bs);
						//
						doc.Save(TypeIconFilePath);
					}
				}
			}
			catch (Exception e)
			{
				if (string.IsNullOrEmpty(file))
				{
					MessageBox.Show(caller, string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"Error loading {0}. {1}", file, e.Message), "Change type icon", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					MessageBox.Show(caller, e.Message, "Change type icon", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				img = null;
			}
			return img;
		}
		private DesignUtil()
		{
		}
		static StringCollection _sysNames;
		static DesignUtil()
		{
			_sysNames = new StringCollection();
			_sysNames.Add("mscorlib");
			_sysNames.Add("System");
			_sysNames.Add("System.Xml");
		}
	}
}
