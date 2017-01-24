/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Object Builder Utility
 * License: GNU General Public License v3.0
 
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.Reflection;
using System.ComponentModel; 
using System.ComponentModel.Design.Serialization;
using System.Threading;
using System.Reflection.Emit;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Design;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Drawing.Imaging;
using WindowsUtility;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace VPL
{
	public delegate CodeExpression fnGetTypeConversion(Type targetType, CodeExpression data, Type dataType, CodeStatementCollection statements);
	public delegate Type fnGetClassTypeFromDynamicType(Type dynamicType);
	public delegate void fnLogMessage(string message, params object[] values);
	public delegate IComponentID fnGetComponentID(IComponent caller, UInt32 cid);
	public delegate IList<IComponentID> fnGetComponentIDList(IComponent caller);
	public delegate string fnGetMessage();
	public delegate void fnProjectOperator(Guid prjId, object data);
	public enum EnumXType { None, Class, FormControl }
	public enum EnumSessionDataStorage
	{
		Cookies = 0,
		HTML5Storage = 1
	}
	/// <summary>
	/// generic utilities
	/// </summary>
	public sealed class VPLUtil
	{
		#region fields and constructors
		public const string ASSEMBLYNAME_PRE = "LimnorStudioDynamic";
		public static bool IsCompiling;
		public static bool IsCopyProtectorClient; //VerifyLicense is called in the program
		public static bool IsCompilingToDebug;
		public static IErrorLog ErrorLogger;
		public static ILimnorStudioProject CurrentProject;
		public static fnGetClassTypeFromDynamicType GetClassTypeFromDynamicType;
		public static fnLogMessage DelegateLogIdeProfiling;
		public static Type VariableMapTargetType;
		private static List<string> _imageExtList;
		static VPLUtil()
		{
			_imageExtList = new List<string>();
			_imageExtList.Add(".bmp");
			_imageExtList.Add(".png");
			_imageExtList.Add(".jpg");
			_imageExtList.Add(".tiff");
			_imageExtList.Add(".gif");
		}
		#endregion
		#region IDE
		public const int IDEEDITION = 5;
		public static EnumRunContext CurrentRunContext = EnumRunContext.Server;
		public static void LogIdeProfiling(string message, params object[] values)
		{
			if (DelegateLogIdeProfiling != null)
			{
				DelegateLogIdeProfiling(message, values);
			}
		}
		public static fnGetComponentID delegateGetComponentID;
		public static fnGetComponentIDList delegateGetComponentList;
		#endregion
		#region Image Filename Utilities
		public static IList<string> ImageExtentionList
		{
			get
			{
				return _imageExtList;
			}
		}
		public static bool IsImageFile(string file)
		{
			string ext = System.IO.Path.GetExtension(file);
			foreach (string s in _imageExtList)
			{
				if (string.Compare(s, ext, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return true;
				}
			}
			return false;
		}
		#endregion
		#region Extern DLL
		[DllImport("Gdi32")]
		static extern bool DeleteObject(IntPtr hObj);
		#endregion
		#region Default values
		public static bool IsDefaultValue(object v)
		{
			if (v == null)
				return true;

			Type t = v.GetType();
			if (t.IsEnum)
			{
				Array a = Enum.GetValues(t);
				if (a.Length > 0)
				{
					return (a.GetValue(0) == v);
				}
				return false;
			}
			TypeCode tc = Type.GetTypeCode(t);
			switch (tc)
			{
				case TypeCode.Boolean:
					return (bool)v == default(bool);
				case TypeCode.Char:
					return (char)v == default(char);
				case TypeCode.DateTime:
					return (DateTime)v == default(DateTime);
				case TypeCode.DBNull:
					return v == System.DBNull.Value;
				case TypeCode.Empty:
					return false;
				case TypeCode.Object:
					if (t.IsClass)
						return false;
					else if (t.IsArray)
						return false;
					else
					{
						//a struct
						int n = 0;
						PropertyInfo[] fifs = t.GetProperties();
						if (fifs.Length > 0)
						{
							for (int i = 0; i < fifs.Length; i++)
							{
								if (fifs[i].CanWrite && !fifs[i].IsSpecialName)
								{
									object v0 = fifs[i].GetValue(v, null);
									if (!IsDefaultValue(v0))
									{
										return false;
									}
									n++;
								}
							}
						}
						return (n > 0);
					}
				case TypeCode.String:
					return false;
				case TypeCode.Byte:
					return ((byte)v == default(byte));
				case TypeCode.Decimal:
					return ((decimal)v == default(decimal));
				case TypeCode.Double:
					return ((double)v == default(double));
				case TypeCode.Int16:
					return ((Int16)v == default(Int16));
				case TypeCode.Int32:
					return ((Int32)v == default(Int32));
				case TypeCode.Int64:
					return ((Int64)v == default(Int64));
				case TypeCode.SByte:
					return ((sbyte)v == default(sbyte));
				case TypeCode.Single:
					return ((Single)v == default(Single));
				case TypeCode.UInt16:
					return ((UInt16)v == default(UInt16));
				case TypeCode.UInt32:
					return ((UInt32)v == default(UInt32));
				case TypeCode.UInt64:
					return ((UInt64)v == default(UInt64));
				default:
					return false;
			}
		}
		public static object TryCreateInstance(Type t)
		{
			object v = GetDefaultValue(t);
			if (v == null)
			{
				ConstructorInfo[] cifs = t.GetConstructors();
				if (cifs != null && cifs.Length > 0)
				{
					ConstructorInfo c = cifs[0];
					ParameterInfo[] pifs = c.GetParameters();
					if (pifs.Length > 0)
					{
						int n = pifs.Length;
						for (int i = 1; i < cifs.Length; i++)
						{
							pifs = cifs[i].GetParameters();
							if (n > pifs.Length)
							{
								c = cifs[i];
								n = pifs.Length;
								if (n == 0)
								{
									break;
								}
							}
						}
					}
					object[] ps = new object[pifs.Length];
					for (int i = 0; i < pifs.Length; i++)
					{
						ps[i] = TryCreateInstance(pifs[i].ParameterType);
					}
					v = c.Invoke(ps);
				}
			}
			return v;
		}
		public static object CreatedDefaultValue(Type t)
		{
			if (t.GetConstructor(Type.EmptyTypes) != null)
			{
				return Activator.CreateInstance(t);
			}
			ConstructorInfo[] cifs = t.GetConstructors();
			if (cifs != null && cifs.Length > 0)
			{
				ConstructorInfo c = null;
				int n = int.MaxValue;
				bool isPrimitive = false;
				bool isValue = false;
				for (int i = 0; i < cifs.Length; i++)
				{
					ParameterInfo[] pifs = cifs[i].GetParameters();
					bool ip = true;
					bool iv = true;
					int n0;
					if (pifs != null)
					{
						n0 = pifs.Length;
						for (int k = 0; k < n0; k++)
						{
							if (!pifs[k].ParameterType.IsPrimitive)
							{
								ip = false;
							}
							if (!pifs[k].ParameterType.IsValueType)
							{
								iv = false;
							}
						}
					}
					else
					{
						n0 = 0;
					}
					if (!isPrimitive)
					{
						if (ip)
						{
							c = cifs[i];
							n = n0;
							isPrimitive = true;
							isValue = true;
						}
						else
						{
							if (!isValue)
							{
								if (iv)
								{
									isValue = true;
									n = n0;
									c = cifs[i];
								}
								else
								{
									if (n0 < n)
									{
										n = n0;
										c = cifs[i];
									}
								}
							}
							else
							{
								if (iv)
								{
									if (n0 < n)
									{
										n = n0;
										c = cifs[i];
									}
								}
							}
						}
					}
					else
					{
						//isPrimitive
						if (ip)
						{
							if (n0 < n)
							{
								n = n0;
								c = cifs[i];
							}
						}
					}
				}

				ParameterInfo[] ps = c.GetParameters();
				object[] vs = new object[ps.Length];
				for (int i = 0; i < ps.Length; i++)
				{
					vs[i] = GetDefaultValue(ps[i].ParameterType);
				}
				return Activator.CreateInstance(t, vs);
			}
			return null;
		}
		public static object GetDefaultValue(Type t)
		{
			if (t == null)
				return null;
			if (t.Equals(typeof(void)))
				return null;
			if (t.IsInterface)
				return null;
			if (t.IsAbstract)
				return null;
			if (t.IsEnum)
			{
				Array a = Enum.GetValues(t);
				if (a.Length > 0)
				{
					return a.GetValue(0);
				}
				return 0;
			}
			if (t.IsArray)
			{
				int rank = t.GetArrayRank();
				Type te = t.GetElementType();
				if (te.IsGenericParameter || te.IsGenericType || te.IsGenericTypeDefinition)
				{
					return null;
				}
				int[] sizes = new int[rank];
				for (int i = 0; i < sizes.Length; i++)
				{
					sizes[i] = 0;
				}
				Array a = Array.CreateInstance(te, sizes);
				return a;
			}
			if (t.Equals(typeof(IntPtr)))
			{
				return IntPtr.Zero;
			}
			if (t.Equals(typeof(UIntPtr)))
			{
				return UIntPtr.Zero;
			}
			TypeCode tc = Type.GetTypeCode(t);
			switch (tc)
			{
				case TypeCode.Boolean:
					return default(bool);
				case TypeCode.Char:
					return default(char);
				case TypeCode.DateTime:
					return default(DateTime);
				case TypeCode.DBNull:
					return System.DBNull.Value;
				case TypeCode.Empty:
					return null;
				case TypeCode.Object:
					if (t.IsValueType)
					{
						return CreatedDefaultValue(t);
					}
					else if (t.IsClass)
						return null;
					else if (t.IsArray)
						return null;
					else
					{
						if (!t.ContainsGenericParameters)
						{
							return CreatedDefaultValue(t);
						}
						return null;
					}
				case TypeCode.String:
					return default(string);
				case TypeCode.Byte:
					return default(byte);
				case TypeCode.Decimal:
					return default(decimal);
				case TypeCode.Double:
					return default(double);
				case TypeCode.Int16:
					return default(Int16);
				case TypeCode.Int32:
					return default(Int32);
				case TypeCode.Int64:
					return default(Int64);
				case TypeCode.SByte:
					return default(sbyte);
				case TypeCode.Single:
					return default(Single);
				case TypeCode.UInt16:
					return default(UInt16);
				case TypeCode.UInt32:
					return default(UInt32);
				case TypeCode.UInt64:
					return default(UInt64);
				default:
					return Convert.ChangeType(0, tc);
			}
		}
		private static Random _rand;
		public static object CreateRandomValue(Type t)
		{
			if (t == null)
				return null;
			if (t.Equals(typeof(void)))
				return null;
			if (t.IsInterface)
				return null;
			if (t.IsAbstract)
				return null;
			if (t.IsEnum)
			{
				Array a = Enum.GetValues(t);
				if (a.Length > 0)
				{
					if (_rand == null) _rand = new Random();
					int idx = _rand.Next(0, a.Length);
					return a.GetValue(idx);
				}
				return 0;
			}
			if (t.IsArray)
			{
				return null;
			}
			if (_rand == null) _rand = new Random();
			Random r = _rand;
			int n;
			TypeCode tc = Type.GetTypeCode(t);
			switch (tc)
			{
				case TypeCode.Boolean:
					n = r.Next(0, 2);
					return n == 0;
				case TypeCode.Char:
					n = r.Next(33, 126);
					return (char)n;
				case TypeCode.DateTime:
					return new DateTime(r.Next(1990, 2014), r.Next(1, 13), r.Next(1, 29), r.Next(0, 24), r.Next(0, 60), r.Next(0, 60));
				case TypeCode.DBNull:
					return System.DBNull.Value;
				case TypeCode.Empty:
					return null;
				case TypeCode.Object:
					if (t.IsValueType)
					{
						return CreatedDefaultValue(t);
					}
					else if (t.IsClass)
						return null;
					else if (t.IsArray)
						return null;
					else
					{
						if (!t.ContainsGenericParameters)
						{
							return CreatedDefaultValue(t);
						}
						return null;
					}
				case TypeCode.String:
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < 8; i++)
					{
						sb.Append((char)(r.Next(33, 126)));
					}
					return sb.ToString();
				case TypeCode.Byte:
					return (byte)r.Next(0, 126);
				case TypeCode.Decimal:
					return (decimal)r.Next(0, 65536);
				case TypeCode.Double:
					return (double)r.Next(0, 65536);
				case TypeCode.Int16:
					return (Int16)r.Next(Int16.MinValue, Int16.MaxValue);
				case TypeCode.Int32:
					return (Int32)r.Next(Int32.MinValue, Int32.MaxValue);
				case TypeCode.Int64:
					return (Int64)r.Next(Int32.MinValue, Int32.MaxValue);
				case TypeCode.SByte:
					return (sbyte)r.Next(-126, 126);
				case TypeCode.Single:
					return (Single)r.Next(0, 65536);
				case TypeCode.UInt16:
					return (UInt16)r.Next(Int16.MinValue, Int16.MaxValue);
				case TypeCode.UInt32:
					return (UInt32)r.Next(Int32.MinValue, Int32.MaxValue);
				case TypeCode.UInt64:
					return (UInt64)r.Next(Int32.MinValue, Int32.MaxValue);
				default:
					return null;
			}
		}
		#endregion
		#region icon
		public static ImageFormat GetImageFormat(EnumImageFormat format)
		{
			switch (format)
			{
				case EnumImageFormat.Bmp:
					return ImageFormat.Bmp;
				case EnumImageFormat.Emf:
					return ImageFormat.Emf;
				case EnumImageFormat.Exif:
					return ImageFormat.Exif;
				case EnumImageFormat.Gif:
					return ImageFormat.Gif;

				case EnumImageFormat.Icon:
					return ImageFormat.Icon;
				case EnumImageFormat.Jpeg:
					return ImageFormat.Jpeg;
				case EnumImageFormat.MemoryBmp:
					return ImageFormat.MemoryBmp;
				case EnumImageFormat.Png:
					return ImageFormat.Png;

				case EnumImageFormat.Tiff:
					return ImageFormat.Tiff;
				case EnumImageFormat.Wmf:
					return ImageFormat.Wmf;
			}
			return ImageFormat.Bmp;
		}
		public static Image GetTypeIcon(Type type)
		{
			if (type == null)
				return null;
			ToolboxBitmapAttribute tba;
			Type t = GetObjectType(type);
			Image img = GetActiveXIcon(t);
			if (img != null)
			{
				return img;
			}
			while (t != null && !t.Equals(typeof(object)))
			{
				object[] obs = t.GetCustomAttributes(false);
				if (obs != null && obs.Length > 0)
				{
					for (int i = 0; i < obs.Length; i++)
					{
						tba = obs[i] as ToolboxBitmapAttribute;
						if (tba != null)
						{
							img = tba.GetImage(t);
							if (img != null)
							{
								return img;
							}
						}
					}
				}
				t = t.BaseType;
			}
			t = GetObjectType(type);
			while (t != null && !t.Assembly.GlobalAssemblyCache)
			{
				t = t.BaseType;
			}
			if (t != null)
			{
				tba = TypeDescriptor.GetAttributes(t)[typeof(System.Drawing.ToolboxBitmapAttribute)] as System.Drawing.ToolboxBitmapAttribute;
				if (tba != null)
				{
					return tba.GetImage(t);
				}
			}
			return null;
		}
		static public Image GetActiveXIcon(Type tp)
		{
			if (tp == null)
				return null;
			Image img = null;
			object[] vs = tp.GetCustomAttributes(typeof(System.Windows.Forms.AxHost.ClsidAttribute), true);
			if (vs != null && vs.Length > 0)
			{
				for (int i = 0; i < vs.Length; i++)
				{
					System.Windows.Forms.AxHost.ClsidAttribute clsid = (System.Windows.Forms.AxHost.ClsidAttribute)vs[i];
					if (!string.IsNullOrEmpty(clsid.Value))
					{
						Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(
							string.Format(System.Globalization.CultureInfo.InvariantCulture,
							"CLSID\\{0}\\ToolboxBitmap32",
							clsid.Value));
						string s = "";
						if (reg != null)
						{
							object v = reg.GetValue(null);
							if (v != null)
							{
								s = v.ToString();
							}
							reg.Close();
						}
						if (s.Length > 0)
						{
							int n = s.LastIndexOf(',');
							if (n > 0)
							{
								try
								{
									string sModule = s.Substring(0, n);
									string sRes = s.Substring(n + 1);
									sModule = sModule.Trim();
									sRes = sRes.Trim();
									if (sRes.Length > 0)
									{
										int nRes = Convert.ToInt32(sRes);
										if (System.IO.File.Exists(sModule))
										{
											IntPtr hBitmap = WinUtil.LoadRes_Bitmap(sModule, nRes);
											if (hBitmap != IntPtr.Zero)
											{
												img = System.Drawing.Image.FromHbitmap(hBitmap);
												DeleteObject(hBitmap);
											}
										}
									}
								}
								catch
								{
								}
							}
						}
						if (img != null)
						{
							break;
						}
					}
				}
			}
			return img;
		}
		#endregion
		#region Designer Control wrapper
		static AppDomain currentDomain;
		static AssemblyName assemblyName;
		static AssemblyBuilder assemblyBuilder;
		static ModuleBuilder moduleBuilder;
		static string _assemblyName;
		private static TypeBuilder createTypeBuilder(Type t)
		{
			if (currentDomain == null)
			{
				currentDomain = Thread.GetDomain();
			}
			if (assemblyName == null)
			{
				_assemblyName = VPLUtil.CreateUniqueName("X");
				assemblyName = new AssemblyName();
				assemblyName.Name = _assemblyName;
			}
			if (assemblyBuilder == null)
			{
				assemblyBuilder = currentDomain.DefineDynamicAssembly(
				assemblyName,
				AssemblyBuilderAccess.Run);
			}
			if (moduleBuilder == null)
			{
				moduleBuilder = assemblyBuilder.DefineDynamicModule("XModule", true);
			}
			TypeBuilder typeBuilder = moduleBuilder.DefineType(t.Name,
								TypeAttributes.Public | TypeAttributes.BeforeFieldInit,
								t);
			//
			Type[] constructorParameters = new Type[] { typeof(string), typeof(string) };
			ConstructorInfo constructorInfo = typeof(DesignerAttribute).GetConstructor(constructorParameters);
			CustomAttributeBuilder custAttributeBuilder = new CustomAttributeBuilder(
				constructorInfo,
				new object[] { 
                    "System.Windows.Forms.Design.UserControlDocumentDesigner, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 
                    "System.ComponentModel.Design.IRootDesigner, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" 
                });
			typeBuilder.SetCustomAttribute(custAttributeBuilder);

			//
			constructorParameters = new Type[] { typeof(Type) };
			constructorInfo = typeof(XDesignerAttribute).GetConstructor(constructorParameters);
			custAttributeBuilder = new CustomAttributeBuilder(
				constructorInfo,
				new object[] { t });
			typeBuilder.SetCustomAttribute(custAttributeBuilder);
			//
			return typeBuilder;
		}
		private static Type createDesignerType(Type baseType)
		{
			TypeBuilder typeBuilder = createTypeBuilder(baseType);
			//
			Type type = typeBuilder.CreateType();
			//
			return type;
		}
		static Dictionary<Type, Type> _designerTypes;
		static public Type GetDesignerType(Type baseType)
		{
			if (_designerTypes == null)
			{
				_designerTypes = new Dictionary<Type, Type>();
			}
			Type designer;
			if (!_designerTypes.TryGetValue(baseType, out designer))
			{
				designer = createDesignerType(baseType);
				_designerTypes.Add(baseType, designer);
			}
			return designer;
		}
		static public bool IsDesignerWrapper(Type t)
		{
			if (t.Assembly.GetName().Name == _assemblyName)
			{
				return true;
			}
			return false;
		}
		#endregion
		#region XClass Factory
		private static Type _xclassType;
		private static Dictionary<Type, object> _staticOwner;
		public static int StaticOwnerCount
		{
			get
			{
				if (_staticOwner == null)
					return 0;
				return _staticOwner.Count;
			}
		}
		public static Dictionary<Type, object> StaticOwners
		{
			get
			{
				return _staticOwner;
			}
		}
		public static Dictionary<Type, object>.Enumerator GetStaticTypeEnumerator()
		{
			if (_staticOwner != null)
			{
				return _staticOwner.GetEnumerator();
			}
			return new Dictionary<Type, object>.Enumerator();
		}
		public static XClass<T> StaticOwner<T>()
		{
			if (_staticOwner == null)
			{
				_staticOwner = new Dictionary<Type, object>();
			}
			//
			object obj;
			XClass<T> o;
			if (!_staticOwner.TryGetValue(typeof(T), out obj))
			{
				o = new XClass<T>();
				o.MakeStatic();
				_staticOwner.Add(typeof(T), o);
			}
			else
			{
				o = (XClass<T>)obj;
			}
			return o;
		}
		public static object StaticOwnerForType(Type type)
		{
			if (_staticOwner == null)
			{
				_staticOwner = new Dictionary<Type, object>();
			}
			object obj;

			if (!_staticOwner.TryGetValue(type, out obj))
			{
				Type t = GetXClassType(type);
				obj = Activator.CreateInstance(t);
				((ICustomEventMethodType)obj).MakeStatic();
				_staticOwner.Add(type, obj);
			}
			return obj;
		}
		public static object StaticOwnerForObject(object v)
		{
			return StaticOwnerForType(GetObjectType(v));
		}
		public static string GetXClassTypeString(Type t)
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "VPL.XClass`1[[{0}]], VPL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", t.AssemblyQualifiedName);
		}
		public static Type GetXClassType(Type t)
		{
			if (!typeof(Form).IsAssignableFrom(t) && typeof(Control).IsAssignableFrom(t))
			{
				Type ret = typeof(XControl<>);
				return ret.MakeGenericType(t);
			}
			else
			{
				Type ret = typeof(XClass<>);
				return ret.MakeGenericType(t);
			}
		}
		public static Type GetXControlType(Type t)
		{
			Type ret = typeof(XControl<>);
			return ret.MakeGenericType(t);
		}
		public static string GetXControlTypeString(Type t)
		{
			Type ret = typeof(XControl<>);
			return ret.MakeGenericType(t).AssemblyQualifiedName;
		}
		#endregion
		#region Type Info
		public static bool HasAttribute(PropertyDescriptor p, Type attType)
		{
			if (attType != null)
			{
				if (p.Attributes != null && p.Attributes.Count > 0)
				{
					foreach (Attribute a in p.Attributes)
					{
						if (a != null && attType.Equals(a.GetType()))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		private static ResolveEventHandler reh;
		public static void SetupExternalDllResolve(string folder)
		{
			_currentDllFolder = folder;
			if (reh == null)
			{
				reh = new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			}
			AppDomain.CurrentDomain.AssemblyResolve += reh;
		}
		public static void RemoveExternalDllResolve()
		{
			if (reh != null)
			{
				AppDomain.CurrentDomain.AssemblyResolve -= reh;
			}
		}
		/// <summary>
		/// create an object by trying parameterless constructor first.
		/// if there is not a parameterless constructor then use provided parameters to find a matching constructor
		/// </summary>
		/// <param name="t">object type</param>
		/// <param name="values">optional constructor parameters</param>
		/// <returns>object created</returns>
		public static object CreateObject(Type t, params object[] values)
		{
			object obj = null;
			ConstructorInfo[] cifs = t.GetConstructors();
			if (cifs != null && cifs.Length > 0)
			{
				for (int i = 0; i < cifs.Length; i++)
				{
					ParameterInfo[] pifs = cifs[i].GetParameters();
					if (pifs == null || pifs.Length == 0)
					{
						if (t.Assembly.GlobalAssemblyCache)
						{
							obj = Activator.CreateInstance(t);
						}
						else
						{
							SetupExternalDllResolve(Path.GetDirectoryName(t.Assembly.Location));
							try
							{
								obj = Activator.CreateInstance(t);
							}
							finally
							{
								RemoveExternalDllResolve();
							}
						}
						break;
					}
				}
				if (obj == null && values != null && values.Length > 0)
				{
					for (int i = 0; i < cifs.Length; i++)
					{
						ParameterInfo[] pifs = cifs[i].GetParameters();
						if (pifs != null && pifs.Length == values.Length)
						{
							bool b = true;
							for (int j = 0; j < pifs.Length; j++)
							{
								if (values[j] != null)
								{
									if (!pifs[j].ParameterType.IsAssignableFrom(values[j].GetType()))
									{
										b = false;
										break;
									}
								}
							}
							if (b)
							{
								if (t.Assembly.GlobalAssemblyCache)
								{
									obj = Activator.CreateInstance(t, values);
								}
								else
								{
									SetupExternalDllResolve(Path.GetDirectoryName(t.Assembly.Location));
									try
									{
										obj = Activator.CreateInstance(t, values);
									}
									finally
									{
										RemoveExternalDllResolve();
									}
								}
								break;
							}
						}
					}
				}
			}
			if (obj == null)
			{
				obj = Activator.CreateInstance(t);
			}
			return obj;
		}
		public static string GetTypeCSharpName(Type t)
		{
			List<Type> nests = new List<Type>();
			Dictionary<string, Type> arguments = new Dictionary<string, Type>();
			nests.Add(t);
			Type t0 = t;
			while (t0.IsNested)
			{
				Type[] args = t0.GetGenericArguments();
				if (args != null)
				{
					Type[] ps = t0.GetGenericTypeDefinition().GetGenericArguments();
					for (int i = 0; i < args.Length; i++)
					{
						if (!arguments.ContainsKey(ps[i].Name))
						{
							arguments.Add(ps[i].Name, args[i]);
						}
					}
				}
				t0 = t0.DeclaringType;
				nests.Insert(0, t0);
			}
			StringBuilder sb = new StringBuilder();
			t0 = nests[0];
			if (t0.IsGenericType)
			{
				string s = t0.FullName;
				int pos = s.IndexOf("`", StringComparison.Ordinal);
				if (pos > 0)
				{
					s = s.Substring(0, pos);
				}
				sb.Append(s);
				sb.Append("<");
				Type[] argsT = t0.GetGenericArguments();
				if (argsT != null)
				{
					for (int j = 0; j < argsT.Length; j++)
					{
						if (j > 0)
						{
							sb.Append(",");
						}
						Type t2;
						if (arguments.TryGetValue(argsT[j].Name, out t2))
						{
							if (string.IsNullOrEmpty(t2.FullName))
							{
								sb.Append(t2.Name);
							}
							else
							{
								sb.Append(t2.FullName);
							}
						}
					}
				}
				sb.Append(">");
			}
			else
			{
				if (string.IsNullOrEmpty(t0.FullName))
				{
					sb.Append(t0.Name);
				}
				else
				{
					sb.Append(t0.FullName);
				}
			}
			for (int i = 1; i < nests.Count; i++)
			{
				sb.Append(".");
				sb.Append(nests[i].Name);
			}
			return sb.ToString();
		}
		private static Dictionary<Guid, Dictionary<UInt32, Type>> _classTypes;
		public static Type GetClassType(UInt32 classId, Guid projectGuid)
		{
			if (_classTypes != null)
			{
				Dictionary<UInt32, Type> types;
				if (_classTypes.TryGetValue(projectGuid, out types))
				{
					Type t;
					if (types.TryGetValue(classId, out t))
					{
						return t;
					}
				}
			}
			return null;
		}
		public static void SetClassType(UInt32 classId, Guid projectGuid, Type t)
		{
			if (_classTypes == null)
			{
				_classTypes = new Dictionary<Guid, Dictionary<uint, Type>>();
			}
			Dictionary<UInt32, Type> types;
			if (!_classTypes.TryGetValue(projectGuid, out types))
			{
				types = new Dictionary<uint, Type>();
				_classTypes.Add(projectGuid, types);
			}
			if (types.ContainsKey(classId))
			{
				types[classId] = t;
			}
			else
			{
				types.Add(classId, t);
			}
		}
		public static bool IsDynamicAssembly(Assembly a)
		{
			if (a == null)
				return false;
			AssemblyName an = a.GetName();
			return an.Name != null && an.Name.StartsWith(ASSEMBLYNAME_PRE, StringComparison.Ordinal);
		}
		public static void RemoveDynamicAssemblies()
		{
			_classTypes = null;
		}
		public static Type[] GetExportedTypes(Assembly a)
		{
			if (IsDynamicAssembly(a))
			{
				if (_classTypes != null)
				{
					AssemblyName aName = a.GetName();
					foreach (KeyValuePair<Guid, Dictionary<UInt32, Type>> kv in _classTypes)
					{
						foreach (KeyValuePair<UInt32, Type> kv2 in kv.Value)
						{
							AssemblyName an2 = kv2.Value.Assembly.GetName();
							if (string.CompareOrdinal(an2.Name, aName.Name) == 0)
							{
								return new Type[] { kv2.Value };
							}
						}
					}
				}
				return Type.EmptyTypes;
			}
			bool loadAsExternalDll = false;
			if (!a.GlobalAssemblyCache)
			{
				string file = null;
				try
				{
					file = a.Location;
				}
				catch
				{
					file = null;
				}
				if (!string.IsNullOrEmpty(file))
				{
					if (string.Compare(Path.GetDirectoryName(Application.ExecutablePath), Path.GetDirectoryName(file), StringComparison.OrdinalIgnoreCase) != 0)
					{
						loadAsExternalDll = true;
					}
				}
			}
			if (loadAsExternalDll)
			{
				return GetExternalDllTypes(a);
			}
			else
			{
				return a.GetExportedTypes();
			}
		}
		public static bool IsSameType(Type t1, Type t2)
		{
			if (t1.Equals(t2))
			{
				return true;
			}
			if (string.CompareOrdinal(t1.Name, t2.Name) != 0)
			{
				return false;
			}
			if (string.CompareOrdinal(t1.FullName, t2.FullName) != 0)
			{
				return false;
			}
			if (string.CompareOrdinal(t1.AssemblyQualifiedName, t2.AssemblyQualifiedName) != 0)
			{
				return false;
			}
			return true;
		}
		private static string _currentDllFolder;
		private static Type[] GetExternalDllTypes(Assembly a)
		{
			_currentDllFolder = Path.GetDirectoryName(a.Location);
			ResolveEventHandler reh = new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			AppDomain.CurrentDomain.AssemblyResolve += reh;
			try
			{
				return a.GetExportedTypes();
			}
			catch
			{
				throw;
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= reh;
			}
		}
		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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
					if (File.Exists(s))
					{
						return Assembly.LoadFrom(s);
					}
				}
			}
			return null;
		}
		static public Type GetCoClassType(Type comType)
		{
			if (comType != null)
			{
				object[] vs = comType.GetCustomAttributes(typeof(CoClassAttribute), true);
				if (vs != null && vs.Length > 0)
				{
					CoClassAttribute a = (CoClassAttribute)vs[0];
					return a.CoClass;
				}
			}
			return null;
		}
		public static string GetTypeDisplay(Type t)
		{
			if (t == null)
				return string.Empty;
			if (t.IsGenericType)
			{
				string s = t.Name;
				int pos = s.IndexOf("`", StringComparison.Ordinal);
				if (pos >= 0)
				{
					s = s.Substring(0, pos);
				}
				StringBuilder sb = new StringBuilder(s);
				sb.Append("<");
				Type[] ts = t.GetGenericArguments();
				if (ts != null && ts.Length > 0)
				{
					sb.Append(ts[0].Name);
					for (int i = 1; i < ts.Length; i++)
					{
						sb.Append(",");
						sb.Append(ts[i].Name);
					}
				}
				sb.Append(">");
				return sb.ToString();
			}
			else
			{
				return t.Name;
			}
		}
		public static string GetMethodSignature(MethodBase mif)
		{
			StringBuilder sb;
			ParameterInfo[] pifs = mif.GetParameters();
			string name = mif.Name;
			if (mif.ContainsGenericParameters)
			{
				Type[] tcs = mif.GetGenericArguments();
				sb = new StringBuilder(name);
				sb.Append("<");
				if (tcs.Length > 0)
				{
					sb.Append(GetTypeDisplay(tcs[0]));
					for (int i = 1; i < tcs.Length; i++)
					{
						sb.Append(",");
						sb.Append(GetTypeDisplay(tcs[i]));
					}
				}
				sb.Append(">");
				name = sb.ToString();
			}
			sb = new StringBuilder(name);
			sb.Append("(");
			if (pifs != null && pifs.Length > 0)
			{
				sb.Append(GetTypeDisplay(pifs[0].ParameterType));
				sb.Append(" ");
				sb.Append(pifs[0].Name);
				for (int i = 1; i < pifs.Length; i++)
				{
					sb.Append(", ");
					sb.Append(GetTypeDisplay(pifs[i].ParameterType));
					sb.Append(" ");
					sb.Append(pifs[i].Name);
				}
			}
			sb.Append(")");
			MethodInfo mi = mif as MethodInfo;
			if (mi != null)
			{
				if (mi.ReturnType != null && !typeof(void).Equals(mi.ReturnType))
				{
					sb.Append(GetTypeDisplay(mi.ReturnType));
				}
			}
			return sb.ToString();
		}
		/// <summary>
		/// .Net reflection is missing a method to get overloaded method differs in return type
		/// </summary>
		/// <param name="t"></param>
		/// <param name="method"></param>
		/// <param name="parameters"></param>
		/// <param name="returnType"></param>
		/// <returns></returns>
		static public MethodInfo GetMethod(Type t, string method, Type[] parameters, Type returnType)
		{
			if (t == null || string.IsNullOrEmpty(method))
				return null;
			try
			{

				bool bPOK = true;
				if (parameters == null || parameters.Length == 0)
				{
					parameters = Type.EmptyTypes;
				}
				else
				{
					for (int i = 0; i < parameters.Length; i++)
					{
						if (parameters[i] == null)
						{
							bPOK = false;
							throw new VPLException("parameter {0} is null for method {1} of type {2}", i, method, t);
						}
					}
				}

				MethodInfo mi = null;
				if (bPOK)
				{
					if (IsDynamicAssembly(t.Assembly))
					{
						if (GetClassTypeFromDynamicType != null)
						{
							t = GetClassTypeFromDynamicType(t);
						}
					}
					mi = t.GetMethod(method, parameters);
					if (mi == null)
					{
						mi = t.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
					}
					if (mi == null)
					{
						if (t.IsInterface)
						{
							Type[] ts = t.GetInterfaces();
							if (ts != null && ts.Length > 0)
							{
								for (int i = 0; i < ts.Length; i++)
								{
									mi = GetMethod(ts[i], method, parameters, returnType);
									if (mi != null)
									{
										break;
									}
								}
							}
						}
					}
					if (mi == null)
					{
						MethodInfo[] mifs = t.GetMethods();
						if (mifs != null && mifs.Length > 0)
						{
							int nCount = 0;
							int np0 = 0;
							if (parameters != null)
							{
								np0 = parameters.Length;
							}
							for (int i = 0; i < mifs.Length; i++)
							{
								if (string.CompareOrdinal(mifs[i].Name, method) == 0)
								{
									ParameterInfo[] pifs = mifs[i].GetParameters();
									int np1 = 0;
									if (pifs != null)
									{
										np1 = pifs.Length;
									}
									if (np0 == 0 && np0 == np1)
									{
										mi = mifs[i];
										break;
									}
									int n2 = 0;
									for (int k = 0; k < Math.Min(np0, np1); k++)
									{
										if (pifs[k].ParameterType.Equals(parameters[k]))
										{
											n2++;
										}
										else
										{
											break;
										}
									}
									if (mi == null || n2 > nCount)
									{
										mi = mifs[i];
										if (n2 > nCount)
										{
											nCount = n2;
										}
									}
								}
							}
						}
					}
				}
				if (mi == null)
				{
					Type t0 = GetCoClassType(t);
					if (t0 != null)
					{
						mi = t0.GetMethod(method, parameters);
						if (mi == null)
						{
							mi = t0.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
						}
					}
				}
				return mi;
			}
			catch (AmbiguousMatchException)
			{
				int n = 0;
				MethodInfo mif = null;
				Type ret = typeof(void);
				if (returnType != null)
				{
					ret = returnType;
				}
				if (parameters != null)
				{
					n = parameters.Length;
				}
				MethodInfo[] mis = t.GetMethods();
				if (mis != null && mis.Length > 0)
				{
					for (int i = 0; i < mis.Length; i++)
					{
						if (string.CompareOrdinal(method, mis[i].Name) == 0)
						{
							ParameterInfo[] pis = mis[i].GetParameters();
							int n2 = 0;
							if (pis != null)
							{
								n2 = pis.Length;
							}
							if (n == n2)
							{
								if (n > 0)
								{
									bool bOK = true;
									for (int j = 0; j < n; j++)
									{
										if (!pis[j].ParameterType.Equals(parameters[j]))
										{
											bOK = false;
											break;
										}
									}
									if (!bOK)
									{
										continue;
									}
								}
								mif = mis[i];
								if (mis[i].ReturnType == null)
								{
									if (ret.Equals(typeof(void)))
									{
										return mis[i];
									}
								}
								else
								{
									if (ret.Equals(mis[i].ReturnType))
									{
										return mis[i];
									}
								}
							}
						}
					}
				}
				return mif;
			}
		}
		static public Type GetElementType(Type listType)
		{
			if (listType.IsArray)
			{
				return listType.GetElementType();
			}
			else
			{
				object[] vs = listType.GetCustomAttributes(typeof(JsTypeAttribute), true);
				if (vs != null && vs.Length > 0)
				{
					JsTypeAttribute jta = vs[0] as JsTypeAttribute;
					if (jta != null && jta.IsArray)
					{
						return typeof(IJavascriptType);
					}
				}
				vs = listType.GetCustomAttributes(typeof(PhpTypeAttribute), true);
				if (vs != null && vs.Length > 0)
				{
					PhpTypeAttribute pta = vs[0] as PhpTypeAttribute;
					if (pta != null && pta.IsArray)
					{
						return typeof(object);
					}
				}
				PropertyInfo[] pifs = listType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

				if (pifs != null && pifs.Length > 0)
				{
					for (int i = 0; i < pifs.Length; i++)
					{
						PropertyInfo pif = pifs[i];
						if (pif != null && string.CompareOrdinal(pif.Name, "Item") == 0)
						{
							MethodInfo mif = pif.GetGetMethod(true);
							ParameterInfo[] mpifs = mif.GetParameters();
							if (mpifs != null && mpifs.Length == 1)
							{
								ParameterInfo mpif = mpifs[0];
								if (mpif.ParameterType.Equals(typeof(int)))
								{
									return pif.PropertyType;
								}
							}
						}
					}
				}
			}
			return null;
		}
		static public Type XClassType
		{
			get
			{
				if (_xclassType == null)
				{
					_xclassType = typeof(XClass<string>).GetGenericTypeDefinition();
				}
				return _xclassType;
			}
		}
		static private Type _xcontrolType;
		static public Type XControlType
		{
			get
			{
				if (_xcontrolType == null)
				{
					_xcontrolType = typeof(XControl<Control>).GetGenericTypeDefinition();
				}
				return _xcontrolType;
			}
		}
		static public Type GetObjectType(Type v)
		{
			if (v == null)
			{
				return typeof(object);
			}
			if (v.IsGenericType)
			{
				Type t = v.GetGenericTypeDefinition();
				if (XClassType.Equals(t))
				{
					Type[] ts = v.GetGenericArguments();
					return ts[0];
				}
				else if (XControlType.Equals(t))
				{
					Type[] ts = v.GetGenericArguments();
					return ts[0];
				}
			}
			return v;
		}

		static public Type GetInternalType(Type v)
		{
			if (v.IsGenericType)
			{
				Type t = v.GetGenericTypeDefinition();
				if (XClassType.Equals(t))
				{
					Type[] ts = v.GetGenericArguments();
					return ts[0];
				}
			}
			Type tx = TypeMappingAttribute.GetMappedType(v);
			if (tx != null)
			{
				return tx;
			}
			return v;
		}
		static public EnumXType IsXType(Type v)
		{
			if (v.IsGenericType)
			{
				Type t = v.GetGenericTypeDefinition();
				if (XClassType.Equals(t))
				{
					return EnumXType.Class;
				}
				if (XControlType.Equals(t))
				{
					return EnumXType.FormControl;
				}
			}
			return EnumXType.None;
		}
		static public Type GetObjectType(object v)
		{
			if (v == null)
				return typeof(object);
			ICustomEventMethodType ent = v as ICustomEventMethodType;
			if (ent != null)
			{
				return ent.ValueType;
			}
			else
			{
				Type t = v as Type;
				if (t != null)
					return t;
				return v.GetType();
			}
		}
		static public object GetObject(object v)
		{
			if (v == null)
				return null;
			ICustomEventMethodType ent = v as ICustomEventMethodType;
			if (ent != null)
			{
				//an XClass<T> or XControl<T>
				return ent.ObjectValue;
			}
			else
			{
				return v;
			}
		}
		static public bool IsAbstract(PropertyInfo pif)
		{
			if (pif.CanRead)
			{
				MethodInfo mif = pif.GetGetMethod(true);
				if ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
				{
					return true;
				}
			}
			else if (pif.CanWrite)
			{
				MethodInfo mif = pif.GetSetMethod(true);
				if ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
				{
					return true;
				}
			}
			return false;
		}
		static public bool IsAbstract(MethodInfo mif)
		{
			return ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract);
		}
		static public bool IsAbstract(EventInfo eif)
		{
			MethodInfo mif = eif.GetRaiseMethod();
			return ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract);
		}
		static public object GetPropertyDefaultValue(PropertyInfo pif)
		{
			object[] vs = pif.GetCustomAttributes(typeof(DefaultValueAttribute), true);
			if (vs != null && vs.Length > 0)
			{
				DefaultValueAttribute defv = (DefaultValueAttribute)vs[0];
				return defv.Value;
			}
			return GetDefaultValue(pif.PropertyType);
		}
		static public object GetFieldDefaultValue(FieldInfo pif)
		{
			object[] vs = pif.GetCustomAttributes(typeof(DefaultValueAttribute), true);
			if (vs != null && vs.Length > 0)
			{
				DefaultValueAttribute defv = (DefaultValueAttribute)vs[0];
				return defv.Value;
			}
			return GetDefaultValue(pif.FieldType);
		}
		static public bool IsNumber(Type t)
		{
			TypeCode tc = Type.GetTypeCode(t);
			switch (tc)
			{
				case TypeCode.Byte: return true;
				case TypeCode.Decimal: return true;
				case TypeCode.Double: return true;
				case TypeCode.Int16: return true;
				case TypeCode.Int32: return true;
				case TypeCode.Int64: return true;
				case TypeCode.SByte: return true;
				case TypeCode.Single: return true;
				case TypeCode.UInt16: return true;
				case TypeCode.UInt32: return true;
				case TypeCode.UInt64: return true;
			}
			return false;
		}
		static public bool IsValueEqual(object v1, object v2)
		{
			if (v1 == null && v2 == null)
			{
				return true;
			}
			if (v1 == v2)
			{
				return true;
			}
			if (v1 == null && v2 != null)
			{
				return false;
			}
			if (v1 != null && v2 == null)
			{
				return false;
			}
			Type t1 = v1.GetType();
			Type t2 = v2.GetType();
			if (IsNumber(t1) && IsNumber(t2))
			{
				double d1 = Convert.ToDouble(v1);
				double d2 = Convert.ToDouble(v2);
				return (d1 == d2);
			}
			if (t1.Equals(t2))
			{
				if (t1.Equals(typeof(string)))
				{
					string s1 = (string)v1;
					string s2 = (string)v2;
					return (string.CompareOrdinal(s1, s2) == 0);
				}
				if (t1.Equals(typeof(bool)))
				{
					bool b1 = (bool)v1;
					bool b2 = (bool)v2;
					return (b1 == b2);
				}
				if (t1.Equals(typeof(DateTime)))
				{
					DateTime dt1 = (DateTime)v1;
					DateTime dt2 = (DateTime)v2;
					return (dt1 == dt2);
				}
				if (t1.Equals(typeof(char)))
				{
					char c1 = (char)v1;
					char c2 = (char)v2;
					return (c1 == c2);
				}
				string sv1 = v1.ToString();
				string sv2 = v2.ToString();
				return (string.CompareOrdinal(sv1, sv2) == 0);
			}
			return false;
		}
		static public bool IsFinal(PropertyInfo pif)
		{
			if (pif == null)
			{
				throw new VPLException("pif is null calling IsFinal");
			}
			if (pif.CanRead)
			{
				MethodInfo mif = pif.GetGetMethod(true);
				if ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
				{
					return false;
				}
				if ((mif.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual)
				{
					return false;
				}
			}
			if (pif.CanWrite)
			{
				MethodInfo mif = pif.GetSetMethod(true);
				if ((mif.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
				{
					return false;
				}
				if ((mif.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual)
				{
					return false;
				}
			}
			return true;
		}
		static public bool IsBrowsable(MemberInfo p)
		{
			object[] attrs = p.GetCustomAttributes(typeof(BrowsableAttribute), true);
			if (attrs != null && attrs.Length > 0)
			{
				BrowsableAttribute a = attrs[0] as BrowsableAttribute;
				return a.Browsable;
			}
			return true;
		}
		static public bool IsBrowsable(PropertyDescriptor p)
		{
			if (p.Attributes != null && p.Attributes.Count > 0)
			{
				foreach (Attribute attr in p.Attributes)
				{
					BrowsableAttribute a = attr as BrowsableAttribute;
					if (a != null)
					{
						return a.Browsable;
					}
				}
			}
			return true;
		}
		static public bool IsNotForProgramming(MemberInfo p)
		{
			object[] attrs = p.GetCustomAttributes(typeof(NotForProgrammingAttribute), true);
			if (attrs != null && attrs.Length > 0)
			{
				return true;
			}
			return false;
		}

		static public bool UseTypeEditor(Type t)
		{
			string sUIname = typeof(UITypeEditor).AssemblyQualifiedName;
			object[] objs = t.GetCustomAttributes(true);
			if (objs != null && objs.Length > 0)
			{
				for (int i = 0; i < objs.Length; i++)
				{
					if (objs[i] is UITypeEditor)
					{
						return true;
					}
					else
					{
						System.ComponentModel.EditorAttribute ea = objs[i] as System.ComponentModel.EditorAttribute;
						if (ea != null)
						{
							if (ea.EditorBaseTypeName == sUIname)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		static public void CopyProperties(object source, object target)
		{
			if (source != null && target != null)
			{
				Type typeTarget = target.GetType();
				PropertyDescriptorCollection psSource = TypeDescriptor.GetProperties(source);
				foreach (PropertyDescriptor pSrc in psSource)
				{
					if (!pSrc.IsReadOnly && pSrc.SerializationVisibility != DesignerSerializationVisibility.Hidden)
					{
						if (pSrc.PropertyType.IsArray)
						{
						}
						else if (pSrc.PropertyType.IsSpecialName)
						{
						}
						else if (typeof(IList).IsAssignableFrom(pSrc.PropertyType))
						{
						}
						else
						{
							PropertyInfo pif = typeTarget.GetProperty(pSrc.Name);
							if (pif != null && pif.CanWrite && !pif.IsSpecialName)
							{
								object src = pSrc.GetValue(source);
								object tgt = pif.GetValue(target, null);
								if (src != tgt)
								{
									pif.SetValue(target, src, null);
								}
							}
						}
					}
				}
			}
		}
		public static void FindNonGacReferenceLocations(Dictionary<string, Assembly> sc, Assembly a)
		{
			if (!a.GlobalAssemblyCache)
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
							Assembly a0 = Assembly.Load(names[i]);
							FindNonGacReferenceLocations(sc, a0);
						}
					}
				}
			}
		}
		public static bool NeedConstructor(Type t)
		{
			if (t.Equals(typeof(string)))
			{
				return false;
			}
			ConstructorInfo[] cifs = t.GetConstructors();
			if (cifs != null && cifs.Length > 0)
			{
				if (cifs.Length == 1)
				{
					ParameterInfo[] pifs = cifs[0].GetParameters();
					if (pifs == null || pifs.Length == 0)
					{
						return false;
					}
				}
			}
			else
			{
				return false;
			}
			return true;
		}
		#endregion
		#region GetMethods
		public static ParameterInfo[] FindParameters(Type t, string methodname)
		{
			MethodInfo[] mifs = t.GetMethods();
			if (mifs != null && mifs.Length > 0)
			{
				ParameterInfo[] pifs = null;
				for (int i = 0; i < mifs.Length; i++)
				{
					if (string.CompareOrdinal(methodname, mifs[i].Name) == 0)
					{
						if (pifs == null)
						{
							pifs = mifs[i].GetParameters();
						}
						else
						{
							ParameterInfo[] ps = mifs[i].GetParameters();
							if (ps != null && ps.Length > pifs.Length)
							{
								pifs = ps;
							}
						}
					}
				}
				return pifs;
			}
			return null;
		}
		public static MethodInfo[] GetMethods(MethodInfo[] eArray, bool includeSpecialName, bool browsableOnly, bool webOnly)
		{
			if (eArray != null)
			{
				List<MethodInfo> es = new List<MethodInfo>();
				for (int i = 0; i < eArray.Length; i++)
				{
					if (includeSpecialName || !eArray[i].IsSpecialName)
					{
						if (!browsableOnly || IsBrowsable(eArray[i]))
						{
							if (VPLUtil.IsNotForProgramming(eArray[i]))
							{
								continue;
							}
							if (!webOnly)
							{
								object[] vs = eArray[i].GetCustomAttributes(typeof(WebClientOnlyAttribute), true);
								if (vs != null && vs.Length > 0)
								{
									continue;
								}
							}
							es.Add(eArray[i]);
						}
					}
				}
				if (es.Count < eArray.Length)
				{
					eArray = new MethodInfo[es.Count];
					es.CopyTo(eArray);
				}
			}
			return eArray;
		}
		public static MethodInfo[] GetMethods(object obj, EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly, bool includeNonPublic, bool webOnly)
		{
			ICustomEventMethodDescriptor cem = obj as ICustomEventMethodDescriptor;
			if (cem != null)
			{
				return cem.GetMethods();
			}
			MethodInfo[] eArray;
			ICustomEventMethodType ent = obj as ICustomEventMethodType;
			if (ent != null)
			{
				eArray = ent.GetMethods(scope, includeSpecialName, browsableOnly, webOnly);
			}
			else
			{
				Type t = obj as Type;
				if (t == null)
				{
					t = obj.GetType();
				}
				t = VPLUtil.GetObjectType(t);
				List<MethodInfo> mlst = new List<MethodInfo>();
				BindingFlags flags;
				if (scope == EnumReflectionMemberInfoSelectScope.Both)
				{
					if (includeNonPublic)
					{
						flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
					}
					else
					{
						flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
					}
				}
				else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
				{
					if (includeNonPublic)
					{
						flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
					}
					else
					{
						flags = BindingFlags.Public | BindingFlags.Instance;
					}
				}
				else
				{
					if (includeNonPublic)
					{
						flags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
					}
					else
					{
						flags = (BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
					}
				}
				GetAllMethods(t, flags, mlst);
				eArray = mlst.ToArray();
				eArray = GetMethods(eArray, includeSpecialName, browsableOnly, webOnly);
				//
				PropertyInfo[] pifs;
				if (scope == EnumReflectionMemberInfoSelectScope.Both)
				{
					flags = (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
				}
				else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
				{
					flags = (BindingFlags.Public | BindingFlags.Instance);
				}
				else
				{
					flags = (BindingFlags.Public | BindingFlags.Static);
				}
				List<PropertyInfo> plst = new List<PropertyInfo>();
				GetAllProperties(t, flags, plst);
				pifs = plst.ToArray();
				List<MethodInfo> mis = new List<MethodInfo>();
				for (int i = 0; i < pifs.Length; i++)
				{
					ParameterInfo[] ps = pifs[i].GetIndexParameters();
					if (ps.Length > 0)
					{
						if (pifs[i].CanRead)
						{
							MethodInfo mi = pifs[i].GetGetMethod(true);
							mis.Add(mi);
						}
						if (pifs[i].CanWrite)
						{
							MethodInfo mi = pifs[i].GetSetMethod(true);
							mis.Add(mi);
						}
					}
				}
				if (mis.Count > 0)
				{
					MethodInfo[] a = new MethodInfo[mis.Count + eArray.Length];
					eArray.CopyTo(a, 0);
					mis.CopyTo(a, eArray.Length);
					eArray = a;
				}
			}
			return eArray;
		}
		public static MethodInfo[] GetIndexers(Type type)
		{
			List<MethodInfo> list = new List<MethodInfo>();
			PropertyInfo[] pifs = type.GetProperties();
			if (pifs != null && pifs.Length > 0)
			{
				for (int i = 0; i < pifs.Length; i++)
				{
					ParameterInfo[] ps = pifs[i].GetIndexParameters();
					if (ps != null && ps.Length > 0)
					{
						list.Add(new MethodInfoIndexer(pifs[i], ps));
					}
				}
			}
			return list.ToArray();
		}
		#endregion
		#region GetEvents
		public static EventInfo[] GetEvents(Type t)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					EventInfo[] events = t.GetEvents(BindingFlags.Public);
					return events;
				}
				return new EventInfo[] { };
			}
			else
			{
				List<EventInfo> es = new List<EventInfo>();
				EventInfo[] events = t.GetEvents();
				if (events != null)
				{
					es.AddRange(events);
				}
				if (t.IsInterface)
				{
					Type[] ts = t.GetInterfaces();
					if (ts != null && ts.Length > 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							events = GetEvents(ts[i]);
							if (events != null)
							{
								es.AddRange(events);
							}
						}
					}
				}
				return es.ToArray();
			}
		}
		public static EventInfo[] GetEvents(EventInfo[] eArray, bool includeSpecialName, bool browsableOnly)
		{
			if (eArray != null)
			{
				List<EventInfo> es = new List<EventInfo>();
				for (int i = 0; i < eArray.Length; i++)
				{
					if (includeSpecialName || !eArray[i].IsSpecialName)
					{
						if (!browsableOnly || IsBrowsable(eArray[i]))
						{
							es.Add(eArray[i]);
						}
					}
				}
				if (es.Count < eArray.Length)
				{
					eArray = new EventInfo[es.Count];
					es.CopyTo(eArray);
				}
			}
			return eArray;
		}
		public static EventInfo[] GetEvents(object obj, EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly)
		{
			ICustomEventMethodDescriptor cem = obj as ICustomEventMethodDescriptor;
			if (cem != null)
			{
				return cem.GetEvents();
			}
			EventInfo[] eArray;
			ICustomEventMethodType ent = obj as ICustomEventMethodType;
			if (ent != null)
			{
				eArray = ent.GetEvents(scope, includeSpecialName, browsableOnly);
			}
			else
			{
				Type t = obj as Type;
				if (t == null)
				{
					t = obj.GetType();
				}
				t = VPLUtil.GetObjectType(t);
				Type t0 = GetCoClassType(t);
				if (t0 != null)
				{
					t = t0;
				}
				if (IsDynamicAssembly(t.Assembly))
				{
					if (GetClassTypeFromDynamicType != null)
					{
						t = GetClassTypeFromDynamicType(t);
						EventInfo[] es = t.GetEvents();
						return es;
					}
				}
				if (scope == EnumReflectionMemberInfoSelectScope.Both)
				{
					eArray = t.GetEvents(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				}
				else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
				{
					eArray = t.GetEvents(BindingFlags.Public | BindingFlags.Instance);
				}
				else
				{
					eArray = t.GetEvents(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				}
				eArray = GetEvents(eArray, includeSpecialName, browsableOnly);
			}
			return eArray;
		}

		#endregion
		#region GetFields
		public static FieldInfo[] GetFields(FieldInfo[] eArray, bool includeSpecialName, bool browsableOnly)
		{
			if (eArray != null)
			{
				List<FieldInfo> es = new List<FieldInfo>();
				for (int i = 0; i < eArray.Length; i++)
				{
					if (includeSpecialName || !eArray[i].IsSpecialName)
					{
						if (!browsableOnly || IsBrowsable(eArray[i]))
						{
							es.Add(eArray[i]);
						}
					}
				}
				if (es.Count < eArray.Length)
				{
					eArray = new FieldInfo[es.Count];
					es.CopyTo(eArray);
				}
			}
			return eArray;
		}
		public static FieldInfo[] GetFields(object obj, EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly)
		{
			FieldInfo[] eArray;
			ICustomEventMethodType ent = obj as ICustomEventMethodType;
			if (ent != null)
			{
				eArray = ent.GetFields(scope, includeSpecialName, browsableOnly);
			}
			else
			{
				Type t = obj as Type;
				if (t == null)
				{
					t = obj.GetType();
				}
				if (scope == EnumReflectionMemberInfoSelectScope.Both)
				{
					eArray = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
				}
				else if (scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
				{
					eArray = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
				}
				else
				{
					eArray = t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				}
				eArray = GetFields(eArray, includeSpecialName, browsableOnly);
			}
			return eArray;
		}

		#endregion
		#region GetProperties
		public static bool FindAttributeByType(AttributeCollection Attributes, Type attType)
		{
			foreach (Attribute a in Attributes)
			{
				if (a != null && attType.IsAssignableFrom(a.GetType()))
				{
					return true;
				}
			}
			return false;
		}
		public static void FixPropertyValues(object v)
		{
			System.IO.FileSystemWatcher fw = v as System.IO.FileSystemWatcher;
			if (fw != null)
			{
				fw.EnableRaisingEvents = false; //fix the default value bug
			}
			else
			{
				System.Timers.Timer timer = v as System.Timers.Timer;
				if (timer != null)
				{
					timer.Enabled = false; //fix the default value bug
				}
			}
		}
		public static bool TryGetDefaultValue(PropertyDescriptor p, out object defValue)
		{
			defValue = null;
			if (p.Attributes != null)
			{
				foreach (Attribute a in p.Attributes)
				{
					IgnoreDefaultValueAttribute idv = a as IgnoreDefaultValueAttribute;
					if (idv != null)
					{
						return false;
					}
				}
				foreach (Attribute a in p.Attributes)
				{
					DefaultValueAttribute dva = a as DefaultValueAttribute;
					if (dva != null)
					{
						defValue = dva.Value;
						return true;
					}
				}
			}
			return false;
		}
		public static PropertyInfo GetPropertyInfo(Type t, string name)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				Type t0 = GetClassTypeFromDynamicType(t);
				return t0.GetProperty(name);
			}
			try
			{
				PropertyInfo pif = t.GetProperty(name);
				if (pif != null)
				{
					return pif;
				}
				if (t.IsInterface)
				{
					Type[] ts = t.GetInterfaces();
					if (ts != null && ts.Length > 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							pif = GetPropertyInfo(ts[i], name);
							if (pif != null)
							{
								return pif;
							}
						}
					}
				}
			}
			catch
			{
				PropertyInfo[] ps = t.GetProperties();
				if (ps != null && ps.Length > 0)
				{
					for (int i = 0; i < ps.Length; i++)
					{
						if (string.CompareOrdinal(ps[i].Name, name) == 0)
						{
							return ps[i];
						}
					}
				}
				Type[] ts = t.GetInterfaces();
				if (ts != null && ts.Length > 0)
				{
					for (int i = 0; i < ts.Length; i++)
					{
						PropertyInfo pif = GetPropertyInfo(ts[i], name);
						if (pif != null)
						{
							return pif;
						}
					}
				}
			}
			return null;
		}
		public static PropertyDescriptor GetProperty(object obj, string propertyName)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(string.Format("Calling VPLUtil.GetProperty with null obj, propertyName={0}", propertyName));
			}
			Type tp = obj.GetType();
			if (IsXType(tp) != EnumXType.None)
			{
				tp = GetObjectType(tp);
				return GetProperty(tp, propertyName);
			}
			ICustomEventMethodType ent = obj as ICustomEventMethodType;
			if (ent != null)
			{
				return ent.GetProperty(propertyName);
			}
			else
			{
				bool isType;
				Type t = obj as Type;
				if (t == null)
				{
					t = obj.GetType();
					isType = false;
				}
				else
				{
					isType = true;
				}
				if (isType)
				{
					Type t0 = VPLUtil.GetCoClassType(t);
					if (t0 != null) t = t0;
					PropertyInfo pif = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
					if (pif != null)
					{
						ParameterInfo[] ps = pif.GetIndexParameters();
						if (ps == null || ps.Length == 0)
						{
							TypePropertyDescriptor prop = new TypePropertyDescriptor(t, pif.Name, null, pif);//.PropertyType, null);
							return prop;
						}
					}
				}
				else
				{
					PropertyDescriptorCollection eArray = TypeDescriptor.GetProperties(obj, new Attribute[] { DesignOnlyAttribute.No });
					foreach (PropertyDescriptor p in eArray)
					{
						if (p.Name == propertyName)
						{
							return p;
						}
					}
				}
			}
			return null;
		}
		public static MethodInfo[] GetMethods(Type t)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					MethodInfo[] methods = t.GetMethods(BindingFlags.Public);
					return methods;
				}
				return new MethodInfo[] { };
			}
			else
			{
				List<MethodInfo> ms = new List<MethodInfo>();
				MethodInfo[] methods = t.GetMethods();
				if (methods != null)
				{
					ms.AddRange(methods);
				}
				if (t.IsInterface)
				{
					Type[] ts = t.GetInterfaces();
					if (ts != null && ts.Length > 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							methods = GetMethods(ts[i]);
							if (methods != null)
							{
								ms.AddRange(methods);
							}
						}
					}
				}
				return ms.ToArray();
			}
		}
		public static void GetAllMethods(Type t, BindingFlags flag, List<MethodInfo> ms)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					MethodInfo[] methods = t.GetMethods(flag);
					if (methods != null)
					{
						ms.AddRange(methods);
					}
				}
			}
			else
			{
				MethodInfo[] methods = t.GetMethods(flag);
				for (int i = 0; i < methods.Length; i++)
				{
					if (!methods[i].IsSpecialName)
					{
						ms.Add(methods[i]);
					}
				}
				if (t.IsInterface)
				{
					Type[] ts = t.GetInterfaces();
					if (ts != null && ts.Length > 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							GetAllMethods(ts[i], flag, ms);
						}
					}
				}
			}
		}
		public static void GetAllProperties(Type t, BindingFlags flag, List<PropertyInfo> ps)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					PropertyInfo[] props = t.GetProperties(flag);
					ps.AddRange(props);
				}
			}
			else
			{
				ps.AddRange(t.GetProperties(flag));
				if (t.IsInterface)
				{
					Type[] ts = t.GetInterfaces();
					if (ts != null && ts.Length > 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							GetAllProperties(ts[i], flag, ps);
						}
					}
				}
			}
		}
		public static EventInfo[] GetAllEvents(Type t)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					EventInfo[] es = t.GetEvents();
					return es;
				}
				return new EventInfo[] { };
			}
			else
			{
				return t.GetEvents();
			}
		}
		public static EventInfo GetEventInfo(Type t, string name)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					return t.GetEvent(name);
				}
			}
			else
			{
				EventInfo eif = t.GetEvent(name);
				if (eif == null)
				{
					eif = t.GetEvent(name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);
					if (eif == null)
					{
						eif = t.GetEvent(name, BindingFlags.FlattenHierarchy | BindingFlags.Static);
					}
				}
				return eif;
			}
			return null;
		}
		public static PropertyInfo[] GetProperties(Type t)
		{
			if (IsDynamicAssembly(t.Assembly))
			{
				if (GetClassTypeFromDynamicType != null)
				{
					t = GetClassTypeFromDynamicType(t);
					PropertyInfo[] props = t.GetProperties(BindingFlags.Public);
					return props;
				}
				return new PropertyInfo[] { };
			}
			else
			{
				List<PropertyInfo> ps = new List<PropertyInfo>();
				PropertyInfo[] props = t.GetProperties();
				if (props != null)
				{
					ps.AddRange(props);
				}
				if (t.IsInterface)
				{
					Type[] ts = t.GetInterfaces();
					if (ts != null && ts.Length > 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							props = GetProperties(ts[i]);
							if (props != null)
							{
								ps.AddRange(props);
							}
						}
					}
				}
				return ps.ToArray();
			}
		}
		public static PropertyDescriptorCollection GetProperties(object obj, EnumReflectionMemberInfoSelectScope scope, bool includeSpecialName, bool browsableOnly, bool includeAbstract)
		{
			if (obj == null)
			{
				throw new VPLException("Calling GetProperties with a null object");
			}
			RuntimeInstance lti = obj as RuntimeInstance;
			if (lti != null)
			{
				return GetProperties(lti.InstanceType, scope, includeSpecialName, browsableOnly, includeAbstract);
			}
			Type tp = obj.GetType();
			if (IsXType(tp) != EnumXType.None)
			{
				tp = GetObjectType(tp);
				return GetProperties(tp, scope, includeSpecialName, browsableOnly, includeAbstract);
			}
			PropertyDescriptorCollection eArray;
			ICustomEventMethodType ent = obj as ICustomEventMethodType;
			if (ent != null)
			{
				eArray = ent.GetProperties(scope, includeSpecialName, browsableOnly, includeAbstract);
			}
			else
			{
				bool isType;
				Type t = obj as Type;
				if (t == null)
				{
					t = obj.GetType();
					isType = false;
				}
				else
				{
					isType = true;
				}
				PropertyInfo[] pifs;
				if (scope == EnumReflectionMemberInfoSelectScope.Both || scope == EnumReflectionMemberInfoSelectScope.InstanceOnly)
				{
					if (isType)
					{
						t = GetObjectType(t);
						Type t0 = VPLUtil.GetCoClassType(t);
						if (t0 != null)
						{
							t = t0;
						}
						eArray = new PropertyDescriptorCollection(null);
						List<PropertyInfo> plist = new List<PropertyInfo>();
						GetAllProperties(t, BindingFlags.Public | BindingFlags.Instance, plist);
						pifs = plist.ToArray();
						if (pifs != null)
						{
							for (int i = 0; i < pifs.Length; i++)
							{
								if (includeAbstract || !IsAbstract(pifs[i]))
								{
									ParameterInfo[] ps = pifs[i].GetIndexParameters();
									if (ps == null || ps.Length == 0)
									{
										object[] vs = pifs[i].GetCustomAttributes(true);
										List<Attribute> attrs = new List<Attribute>();
										if (vs != null && vs.Length > 0)
										{
											for (int k = 0; k < vs.Length; k++)
											{
												Attribute a = vs[k] as Attribute;
												if (a != null)
												{
													attrs.Add(a);
												}
											}
										}
										TypePropertyDescriptor prop = new TypePropertyDescriptor(t, pifs[i].Name, attrs.ToArray(), pifs[i]);//.PropertyType, null);
										eArray.Add(prop);
									}
								}
							}
						}
						if (scope == EnumReflectionMemberInfoSelectScope.Both)
						{
							pifs = t.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
							if (pifs != null)
							{
								for (int i = 0; i < pifs.Length; i++)
								{
									if (includeAbstract || !IsAbstract(pifs[i]))
									{
										StaticPropertyDescriptor2 prop = new StaticPropertyDescriptor2(t, pifs[i].Name, null, pifs[i]);
										eArray.Add(prop);
									}
								}
							}
						}
					}
					else
					{
						Attribute[] attrs;
						if (!(obj is Form))
						{
							//if (browsableOnly)
							//{
							//	attrs = new Attribute[] { DesignOnlyAttribute.No, new BrowsableAttribute(true) };
							//}
							//else
							//{
								attrs = new Attribute[] { DesignOnlyAttribute.No };
							//}
							eArray = TypeDescriptor.GetProperties(obj, attrs);
							if (!browsableOnly)
							{
								attrs = new Attribute[] { DesignOnlyAttribute.No };
								PropertyDescriptorCollection ea0 = TypeDescriptor.GetProperties(obj, attrs);
								if (ea0.Count > 0)
								{
									Dictionary<string, PropertyDescriptor> ps0 = new Dictionary<string, PropertyDescriptor>();
									foreach (PropertyDescriptor p in eArray)
									{
										ps0.Add(p.Name, p);
									}
									foreach (PropertyDescriptor p in ea0)
									{
										if (!ps0.ContainsKey(p.Name))
										{
											ps0.Add(p.Name, p);
										}
									}
									PropertyDescriptor[] ea = new PropertyDescriptor[ps0.Count];
									ps0.Values.CopyTo(ea, 0);
									eArray = new PropertyDescriptorCollection(ea);
								}
							}
						}
						else
						{
							//BrowsableAttribute attribute is not the same as "not for programming"
							//if (browsableOnly)
							//{
							//	attrs = new Attribute[] { DesignOnlyAttribute.No, new BrowsableAttribute(true) };
							//}
							//else
							//{
								attrs = new Attribute[] { DesignOnlyAttribute.No };
							//}
							eArray = TypeDescriptor.GetProperties(obj, attrs);
						}
					}
				}
				else
				{
					eArray = new PropertyDescriptorCollection(null);
					pifs = t.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
					if (pifs != null && pifs.Length > 0)
					{
						Dictionary<string, PropertyDescriptor> ps0 = new Dictionary<string, PropertyDescriptor>();
						foreach (PropertyDescriptor p in eArray)
						{
							ps0.Add(p.Name, p);
						}
						for (int i = 0; i < pifs.Length; i++)
						{
							if (includeAbstract || !IsAbstract(pifs[i]))
							{
								if (!ps0.ContainsKey(pifs[i].Name))
								{
									Attribute[] attrs;
									object[] vs = pifs[i].GetCustomAttributes(true);
									if (vs == null)
									{
										attrs = new Attribute[] { };
									}
									else
									{
										attrs = new Attribute[vs.Length];
										for (int k = 0; k < attrs.Length; k++)
										{
											attrs[k] = (Attribute)vs[k];
										}
									}
									StaticPropertyDescriptor2 prop = new StaticPropertyDescriptor2(t, pifs[i].Name, attrs, pifs[i]);
									ps0.Add(pifs[i].Name, prop);
								}
							}
						}
						PropertyDescriptor[] ea = new PropertyDescriptor[ps0.Count];
						ps0.Values.CopyTo(ea, 0);
						eArray = new PropertyDescriptorCollection(ea);
					}
				}
			}
			if (eArray != null)
			{
				List<PropertyDescriptor> pp = new List<PropertyDescriptor>();
				foreach (PropertyDescriptor p in eArray)
				{
					if (!NotForProgrammingAttribute.IsNotForProgramming(p))
					{
						pp.Add(p);
					}
				}
				if (pp.Count != eArray.Count)
				{
					PropertyDescriptor[] ea = new PropertyDescriptor[pp.Count];
					pp.CopyTo(ea, 0);
					eArray = new PropertyDescriptorCollection(ea);
				}
			}
			return eArray;
		}
		/// <summary>
		/// it must be called just before setting it to the PropertyGrid
		/// </summary>
		/// <param name="c"></param>
		public static void SetPropertiesReadOnly(object c)
		{
			PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(c);
			foreach (PropertyDescriptor pd in pdc)
			{
				FieldInfo[] fifs = pd.Attributes.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
				for (int k = 0; k < fifs.Length; k++)
				{
					if (string.CompareOrdinal(fifs[k].Name, "_attributes") == 0)
					{
						int n = -1;
						Attribute[] attrs = fifs[k].GetValue(pd.Attributes) as Attribute[];
						if (attrs != null && attrs.Length > 0)
						{
							for (int j = 0; j < attrs.Length; j++)
							{
								ReadOnlyAttribute ea = attrs[j] as ReadOnlyAttribute;
								if (ea != null)
								{
									if (!ea.IsReadOnly)
									{
										attrs[j] = new ReadOnlyAttribute(true);
									}
									n = j;
									break;
								}
							}
						}
						if (n < 0)
						{
							Attribute[] attrs0;
							if (attrs == null)
							{
								attrs0 = new Attribute[1];
							}
							else
							{
								attrs0 = new Attribute[1 + attrs.Length];
								attrs.CopyTo(attrs0, 0);
							}
							attrs0[attrs.Length] = new ReadOnlyAttribute(true);
							fifs[k].SetValue(pd.Attributes, attrs0);
						}
						break;
					}
				}
			}
		}
		public static bool AdjustImageEditor(AttributeCollection ats)
		{
			bool adjusted = false;
			FieldInfo[] fifs = ats.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			for (int i = 0; i < fifs.Length; i++)
			{
				if (string.CompareOrdinal(fifs[i].Name, "_attributes") == 0)
				{
					Attribute[] attrs = fifs[i].GetValue(ats) as Attribute[];
					if (attrs != null)
					{
						for (int j = 0; j < attrs.Length; j++)
						{
							EditorAttribute ea = attrs[j] as EditorAttribute;
							if (ea != null)
							{
								if (!ea.EditorTypeName.StartsWith("VPL.", StringComparison.Ordinal))
								{
									attrs[j] = new EditorAttribute(typeof(TypeEditorImage), typeof(UITypeEditor));
								}
								adjusted = true;
								break;
							}
						}
					}
					break;
				}
			}
			return adjusted;
		}
		/// <summary>
		/// change the image property editor
		/// </summary>
		/// <param name="c"></param>
		public static bool AdjustImagePropertyAttribute(object c)
		{
			bool adjusted = false;
			PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(c, new Attribute[] { new BrowsableAttribute(true) });
			foreach (PropertyDescriptor pd in pdc)
			{
				if (typeof(Image).IsAssignableFrom(pd.PropertyType))
				{
					if (AdjustImageEditor(pd.Attributes))
					{
						adjusted = true;
					}
				}
				else if (pd.PropertyType.Equals(typeof(StringCollection)))
				{
					FieldInfo[] fifs = pd.Attributes.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
					for (int i = 0; i < fifs.Length; i++)
					{
						if (string.CompareOrdinal(fifs[i].Name, "_attributes") == 0)
						{
							bool found = false;
							Attribute[] attrs = fifs[i].GetValue(pd.Attributes) as Attribute[];
							if (attrs != null)
							{
								for (int j = 0; j < attrs.Length; j++)
								{
									EditorAttribute ea = attrs[j] as EditorAttribute;
									if (ea != null)
									{
										found = true;
										break;
									}
								}
							}
							if (!found)
							{
								Attribute[] a;
								if (attrs != null)
								{
									a = new Attribute[attrs.Length + 1];
									attrs.CopyTo(a, 0);
								}
								else
								{
									a = new Attribute[1];
								}
								Type t = Type.GetType("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
								EditorAttribute ea = new EditorAttribute(t, typeof(UITypeEditor));
								a[a.Length - 1] = ea;
								fifs[i].SetValue(pd.Attributes, a);
								adjusted = true;
							}
							break;
						}
					}
				}
			}
			return adjusted;
		}
		public static void AdjustImageEditorOnPropertyGrid(GridItem item)
		{
			if (item != null && item.GridItems != null && item.GridItems.Count > 0)
			{
				foreach (GridItem gi in item.GridItems)
				{
					if (gi.PropertyDescriptor != null)
					{
						if (typeof(Image).IsAssignableFrom(gi.PropertyDescriptor.PropertyType))
						{
							AdjustImageEditor(gi.PropertyDescriptor.Attributes);
						}
					}
					AdjustImageEditorOnPropertyGrid(gi);
				}
			}
		}
		#endregion
		#region misc
		public static EnumSessionDataStorage SessionDataStorage;
		public static bool Shutingdown;
		public static bool IsValidVariableName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return false;
			if (!Char.IsLetter(name, 0))
				return false;
			for (int i = 0; i < name.Length; i++)
			{
				char ch = name[i];
				if (ch != '_')
				{
					UnicodeCategory uc = Char.GetUnicodeCategory(ch);
					switch (uc)
					{
						case UnicodeCategory.UppercaseLetter:
						case UnicodeCategory.LowercaseLetter:
						case UnicodeCategory.TitlecaseLetter:
						case UnicodeCategory.DecimalDigitNumber:
							break;
						default:
							return false;
					}
				}
			}
			return true;
		}
		public static string IncreaseVersion(string ver, bool parts3)
		{
			if (string.IsNullOrEmpty(ver))
			{
				if (parts3)
					ver = "1.0.0";
				else
					ver = "1.0.0.0";
			}
			else
			{
				string[] ss = ver.Split('.');
				int verMajor = 1;
				int verMin = 0;
				int revMajor = 0;
				int revMin = 0;
				if (ss.Length > 0)
				{
					if (!int.TryParse(ss[0], out verMajor))
					{
						verMajor = 1;
					}
					else
					{
						if (verMajor < 0)
							verMajor = 0;
					}
					if (ss.Length > 1)
					{
						if (!int.TryParse(ss[1], out verMin))
						{
							verMin = 0;
						}
						else
						{
							if (verMin < 0)
								verMin = 0;
						}
						if (ss.Length > 2)
						{
							if (!int.TryParse(ss[2], out revMajor))
							{
								revMajor = 0;
							}
							else
							{
								if (revMajor < 0)
									revMajor = 0;
							}
							if (ss.Length > 3)
							{
								if (!int.TryParse(ss[3], out revMin))
								{
									revMin = 0;
								}
								else
								{
									if (revMin < 0)
										revMin = 0;
								}
							}
						}
					}
				}
				if (parts3 || revMin > 998)
				{
					revMin = 0;
					if (revMajor > 998)
					{
						revMajor = 0;
						if (verMin > 998)
						{
							verMin = 0;
							verMajor++;
						}
						else
						{
							verMin++;
						}
					}
					else
					{
						revMajor++;
					}
				}
				else
				{
					revMin++;
				}
				if (parts3)
					ver = string.Format(CultureInfo.InvariantCulture,
						"{0}.{1}.{2}", verMajor, verMin, revMajor);
				else
					ver = string.Format(CultureInfo.InvariantCulture,
					"{0}.{1}.{2}.{3}", verMajor, verMin, revMajor, revMin);
			}
			return ver;
		}
		public static bool IsInDesignMode(IComponent ic)
		{
			if (ic == null)
				return false;
			if (ic.Site != null)
			{
				return ic.Site.DesignMode;
			}
			string exe = Path.GetFileName(Application.ExecutablePath);
			if (string.Compare(exe, "LimnorMain.exe", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			if (string.Compare(exe, "LimnorStudio.exe", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			if (string.Compare(exe, "LimnorStudio64.exe", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			return false;
		}
		public static StringCollection JsFiles;
		public static void AddJsFile(string js)
		{
			if (JsFiles == null)
			{
				JsFiles = new StringCollection();
			}
			foreach (string s in JsFiles)
			{
				if (string.Compare(s, js, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return;
				}
			}
			JsFiles.Add(js);
		}
		static public bool CompilerContext_PHP;
		static public bool CompilerContext_JS;
		static public bool CompilerContext_ASPX;
		static public string NameToCodeName(string name)
		{
			return name.Replace("<", "_").Replace(">", "_").Replace("`", "_").Replace(",", "_");
		}
		static public string GuidToString(Guid g)
		{
			return g.ToString("N", CultureInfo.InvariantCulture);
		}
		static public string DateTimeToString(DateTime dt)
		{
			return dt.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);
		}
		static public string FormString(string format, params object[] values)
		{
			if (values != null && values.Length > 0)
			{
				return string.Format(CultureInfo.InvariantCulture, format, values);
			}
			return format;
		}
		static public bool IsItemsHolder(Type t)
		{
			if (t == null)
				return false;
			if (t.IsArray)
				return true;
			return GetElementType(t) != null;
		}
		static public string GetObjectDisplayString(object v)
		{
			if (v == null)
			{
				return "null";
			}
			Type t = v as Type;
			if (t != null)
			{
				return string.Format(CultureInfo.InvariantCulture, "Type:{0}", t.FullName);
			}
			Type t0 = GetObjectType(v);
			IComponent ic = v as IComponent;
			if (ic != null && ic.Site != null)
			{
				if (!string.IsNullOrEmpty(ic.Site.Name))
				{
					return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", t0.Name, ic.Site.Name);
				}
			}
			object o = GetObject(v);
			return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", t0.Name, o);
		}
		static public bool GetBrowseableProperties(Attribute[] attributes)
		{
			if (attributes != null && attributes.Length > 0)
			{
				for (int i = 0; i < attributes.Length; i++)
				{
					BrowsableAttribute ba = attributes[i] as BrowsableAttribute;
					if (ba != null)
					{
						return ba.Browsable;
					}
				}
			}
			return false;
		}
		static public bool GetBrowseableProperties(AttributeCollection attributes)
		{
			if (attributes != null && attributes.Count > 0)
			{
				for (int i = 0; i < attributes.Count; i++)
				{
					BrowsableAttribute ba = attributes[i] as BrowsableAttribute;
					if (ba != null)
					{
						return ba.Browsable;
					}
				}
			}
			return false;
		}
		public static string GetFontString(Font font)
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(Font));
			return converter.ConvertToInvariantString(font);
		}
		public static Font GetFont(string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(Font));
				return (Font)converter.ConvertFromInvariantString(value);
			}
			return new Font("Times New Roman", 12);
		}
		public static string GetColorHexString(Color color)
		{
			return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
		}
		public static string GetColorHexString2(Color color)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
		}
		public static string GetColorString(Color c)
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(Color));
			return converter.ConvertToInvariantString(c);
		}
		public static Color GetColor(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return System.Drawing.Color.Black;
			}
			else
			{
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(Color));
				return (Color)converter.ConvertFromInvariantString(value);
			}
		}
		/// <summary>
		/// convert v to a return value of targetType; assignable is true if the return value is of targetType
		/// </summary>
		/// <param name="v"></param>
		/// <param name="targetType"></param>
		/// <param name="assignable"></param>
		/// <returns></returns>
		public static object ConvertObject(object v, Type targetType, out bool assignable)
		{
			assignable = true;
			if (v == null || v == DBNull.Value)
			{
				return GetDefaultValue(targetType);
			}
			Type tSrc = v.GetType();
			if (targetType.IsAssignableFrom(tSrc))
			{
				return v;
			}
			if (targetType.Equals(typeof(IntPtr)))
			{
				if (IntPtr.Size == 4)
				{
					bool a;
					object v0 = ConvertObject(v, typeof(int), out a);
					if (a)
					{
						IntPtr r = new IntPtr((int)v0);
						return r;
					}
				}
				else
				{
					bool a;
					object v0 = ConvertObject(v, typeof(long), out a);
					if (a)
					{
						IntPtr r = new IntPtr((long)v0);
						return r;
					}
				}
			}
			if (targetType.Equals(typeof(UIntPtr)))
			{
				if (IntPtr.Size == 4)
				{
					bool a;
					object v0 = ConvertObject(v, typeof(uint), out a);
					if (a)
					{
						UIntPtr r = new UIntPtr((uint)v0);
						return r;
					}
				}
				else
				{
					bool a;
					object v0 = ConvertObject(v, typeof(ulong), out a);
					if (a)
					{
						UIntPtr r = new UIntPtr((ulong)v0);
						return r;
					}
				}
			}
			if (targetType.IsEnum)
			{
				if (tSrc.Equals(typeof(string)))
				{
					return Enum.Parse(targetType, (string)v);
				}
			}
			if (targetType.Equals(typeof(string)))
			{
				if (tSrc.IsArray)
				{
					StringBuilder sb = new StringBuilder("[");
					IEnumerable ie = v as IEnumerable;
					if (ie != null)
					{
						bool bfirst = true;
						IEnumerator ier = ie.GetEnumerator();
						while (ier.MoveNext())
						{
							if (bfirst)
							{
								bfirst = false;
							}
							else
							{
								sb.Append(",");
							}
							bool b;
							object v0 = ConvertObject(ier.Current, typeof(string), out b);
							if (b)
							{
								sb.Append((string)v0);
							}
							else
							{
								sb.Append("\"\"");
							}
						}
					}
					sb.Append("]");
					return sb.ToString();
				}
			}
			TypeConverter cnt = TypeDescriptor.GetConverter(v);
			if (cnt.CanConvertTo(targetType))
			{
				return cnt.ConvertTo(v, targetType);
			}
			cnt = TypeDescriptor.GetConverter(targetType);
			if (tSrc.Equals(typeof(string)))
			{
				if (string.IsNullOrEmpty((string)v))
				{
					return GetDefaultValue(targetType);
				}
				if (cnt.CanConvertFrom(typeof(string)))
				{
					string s = (string)v;
					return cnt.ConvertFromInvariantString(s);
				}
			}
			if (cnt.CanConvertFrom(tSrc))
			{
				return cnt.ConvertFrom(v);
			}
			assignable = false;
			return v;
		}
		public static string ObjectToString(object v)
		{
			if (v == null || v == DBNull.Value)
			{
				return string.Empty;
			}
			return v.ToString();
		}
		public static bool ObjectToBool(object v)
		{
			if (v == null || v == DBNull.Value)
			{
				return false;
			}
			string s = v as string;
			if (!string.IsNullOrEmpty(s))
			{
				if (string.Compare(s, "on", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return true;
				}
				if (string.Compare(s, "yes", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return true;
				}
				if (string.Compare(s, "true", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return true;
				}
				return false;
			}
			TypeCode tc = Type.GetTypeCode(v.GetType());
			switch (tc)
			{
				case TypeCode.Byte: return ((byte)v) != 0;
				case TypeCode.Decimal: return ((decimal)v) != 0;
				case TypeCode.Double: return ((double)v) != 0;
				case TypeCode.Int16: return ((Int16)v) != 0;
				case TypeCode.Int32: return ((Int32)v) != 0;
				case TypeCode.Int64: return ((Int64)v) != 0;
				case TypeCode.SByte: return ((sbyte)v) != 0;
				case TypeCode.Single: return ((float)v) != 0;
				case TypeCode.UInt16: return (UInt16)v != 0;
				case TypeCode.UInt32: return (UInt32)v != 0;
				case TypeCode.UInt64: return (UInt64)v != 0;
			}
			return Convert.ToBoolean(v, System.Globalization.CultureInfo.InvariantCulture);
		}
		public static int ObjectToInt(object v)
		{
			if (v == null || v == DBNull.Value)
			{
				return 0;
			}
			return Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture);
		}
		public static double ObjectToDouble(object v)
		{
			if (v == null || v == DBNull.Value)
			{
				return 0.0;
			}
			return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
		}
		public static string FormCodeNameFromname(string name)
		{
			return Regex.Replace(name, @"[\W]", "_");
		}
		public static string CreateUniqueName(string preFix)
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"{0}{1}", preFix, Guid.NewGuid().ToString().Replace("-", ""));
		}

		static public string PopPath(ref string s)
		{
			if (s == null)
				return "";
			if (s.Length == 0)
				return "";
			int n = s.IndexOf('/');
			string sr;
			if (n < 0)
			{
				sr = s;
				s = "";
				return sr;
			}
			if (n == 0)
			{
				s = s.Substring(1);
				return "";
			}
			sr = s.Substring(0, n);
			if (n == s.Length - 1)
				s = "";
			else
				s = s.Substring(n + 1);
			return sr;
		}
		static public string FormExceptionText(Exception e)
		{
			StringBuilder sb = new StringBuilder(e.Message);
			if (e.StackTrace != null)
			{
				sb.Append("\r\nStackt trace:\r\n");
				sb.Append(e.StackTrace);
			}
			while (true)
			{
				e = e.InnerException;
				if (e == null)
					break;
				sb.Append("\r\nInner exception:\r\n");
				sb.Append(e.Message);
				if (e.StackTrace != null)
				{
					sb.Append("\r\nStackt trace:\r\n");
					sb.Append(e.StackTrace);
				}
			}
			return sb.ToString();
		}
		public static System.Globalization.CultureInfo GetCultureInfo(EnumCommonCulture info)
		{
			switch (info)
			{
				case EnumCommonCulture.CurrentCulture:
					return System.Globalization.CultureInfo.CurrentCulture;
				case EnumCommonCulture.CurrentUICulture:
					return System.Globalization.CultureInfo.CurrentUICulture;
				case EnumCommonCulture.InvariantCulture:
					return System.Globalization.CultureInfo.InvariantCulture;
				case EnumCommonCulture.InvariantUICulture:
					return System.Globalization.CultureInfo.InstalledUICulture;
			}
			return System.Globalization.CultureInfo.InvariantCulture;
		}
		public static CodeExpression GetCultureInfoCode(EnumCommonCulture info)
		{
			CodePropertyReferenceExpression c = new CodePropertyReferenceExpression();
			c.TargetObject = new CodeTypeReferenceExpression(typeof(System.Globalization.CultureInfo));
			switch (info)
			{
				case EnumCommonCulture.CurrentCulture:
					c.PropertyName = "CurrentCulture";
					break;
				case EnumCommonCulture.CurrentUICulture:
					c.PropertyName = "CurrentUICulture";
					break;
				case EnumCommonCulture.InvariantCulture:
					c.PropertyName = "InvariantCulture";
					break;
				case EnumCommonCulture.InvariantUICulture:
					c.PropertyName = "InstalledUICulture";
					break;
				default:
					c.PropertyName = "InvariantCulture";
					break;
			}
			return c;
		}
		public static CodeExpression GetStringComparisonCode(StringComparison compare)
		{
			CodePropertyReferenceExpression c = new CodePropertyReferenceExpression();
			c.TargetObject = new CodeTypeReferenceExpression(typeof(StringComparison));
			c.PropertyName = compare.ToString();
			return c;
		}
		public static void SelectSiblingPropertyGridItemByName(PropertyGrid g, string name)
		{
			GridItem gi = g.SelectedGridItem;
			if (gi != null && gi.Parent != null)
			{
				gi = gi.Parent;
				foreach (GridItem ci in gi.GridItems)
				{
					if (string.CompareOrdinal(name, ci.PropertyDescriptor.Name) == 0)
					{
						g.SelectedGridItem = ci;
						break;
					}
				}
			}
		}
		public static int GetImageIndex(string filePath, ImageList imageList)
		{
			if (imageList != null && !string.IsNullOrEmpty(filePath))
			{
				if (File.Exists(filePath))
				{
					string key = filePath.ToLowerInvariant();
					int idx = -1;
					if (imageList.Images.ContainsKey(key))
					{
						idx = imageList.Images.IndexOfKey(key);
					}
					if (idx < 0)
					{
						try
						{
							Image img = Image.FromFile(filePath);
							if (img != null)
							{
								imageList.Images.Add(key, img);
								idx = imageList.Images.IndexOfKey(key);
							}
						}
						catch
						{
						}
					}
					return idx;
				}
			}
			return -1;
		}
		public static FileStream GetFileStream(out long n)
		{
			byte[] bs = Convert.FromBase64String("Q29weVByb3RlY3Rpb25DbGllbnQuZGxs");
			char[] cs = new char[bs.Length];
			for (int i = 0; i < bs.Length; i++)
			{
				cs[i] = (char)bs[i];
			}
			string s = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, new string(cs));
			if (File.Exists(s))
			{
				FileInfo fi = new FileInfo(s);
				n = fi.Length;
				return new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, false);
			}
			else
			{
				n = 0;
			}
			return null;
		}
		public static FileStream GetFileStream2(out long n)
		{
			n = 0;
			byte[] bs = Convert.FromBase64String("TGltbm9yLkNvcHlQcm90ZWN0aW9uLkNvcHlQcm90ZWN0b3IsIENvcHlQcm90ZWN0aW9uQ2xpZW50LCBWZXJzaW9uPTEuMi4yLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49Y2EyMjBmZjA4NTcxNjQ3NA==");
			char[] cs = new char[bs.Length];
			for (int i = 0; i < bs.Length; i++)
			{
				cs[i] = (char)bs[i];
			}
			string s = new string(cs);
			Type t = Type.GetType(s);
			if (t != null)
			{
				s = t.Assembly.Location;
				if (string.IsNullOrEmpty(s))
				{
					n = 1;
				}
				else
				{
					FileInfo fi = new FileInfo(s);
					n = fi.Length;
					return new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, false);
				}
			}
			return null;
		}
		public static string GrantFolderFullPermission(string path)
		{
			string ret = string.Empty;
			try
			{
				DirectorySecurity sec = Directory.GetAccessControl(path);
				SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
				Directory.SetAccessControl(path, sec);
			}
			catch (Exception err)
			{
				ret = err.Message;
			}
			return ret;
		}
		public static string GrantFileFullPermission(string path)
		{
			string ret = string.Empty;
			try
			{
				DirectoryInfo dInfo = new DirectoryInfo(path);
				DirectorySecurity sec = dInfo.GetAccessControl();
				SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
				Directory.SetAccessControl(path, sec);
			}
			catch (Exception err)
			{
				ret = err.Message;
			}
			return ret;
		}
		#endregion
		#region static typed storage
		private static Dictionary<Type, object> _typedData;
		public static void SetTypedData<T>(T v)
		{
			if (_typedData == null)
			{
				_typedData = new Dictionary<Type, object>();
			}
			if (_typedData.ContainsKey(typeof(T)))
			{
				_typedData[typeof(T)] = v;
			}
			else
			{
				_typedData.Add(typeof(T), v);
			}
		}
		public static T GetTypedData<T>()
		{
			if (_typedData != null)
			{
				object v;
				if (_typedData.TryGetValue(typeof(T), out v))
				{
					return (T)v;
				}
			}
			return default(T);
		}
		public static void RemoveTypedData<T>()
		{
			if (_typedData != null)
			{
				if (_typedData.ContainsKey(typeof(T)))
				{
					_typedData.Remove(typeof(T));
				}
			}
		}
		public static bool HasTypedData<T>()
		{
			if (_typedData != null)
			{
				if (_typedData.ContainsKey(typeof(T)))
				{
					return true;
				}
			}
			return false;
		}
		#endregion
		#region Services
		public static Type PropertyValueLinkEditor;
		public static Type PropertyValueLinkType;
		public static fnCollectLanguageIcons CollectLanguageIcons;
		public static fnGetLanguageImageByName GetLanguageImageByName;
		public static fnProjectOperator RemoveDialogCaches;
		public const string SERVICE_ComponentSelector = "SERVICE_EasyDataSetSelector";
		private static Dictionary<string, object> _servicesByName;
		public static void SetServiceByName(string name, object value)
		{
			if (_servicesByName == null)
			{
				_servicesByName = new Dictionary<string, object>();
			}
			if (_servicesByName.ContainsKey(name))
			{
				_servicesByName[name] = value;
			}
			else
			{
				_servicesByName.Add(name, value);
			}
		}
		public static object GetServiceByName(string name)
		{
			if (_servicesByName != null)
			{
				object v;
				if (_servicesByName.TryGetValue(name, out v))
				{
					return v;
				}
			}
			return null;
		}
		#endregion
		#region Compile utility
		public static Dictionary<string, string> ClassCompileData;
		/// <summary>
		/// ce is of primary type
		/// caller determines that ce is not a type of t and call this function
		/// </summary>
		/// <param name="t">destination type</param>
		/// <param name="ce">source exp</param>
		/// <returns>exp of type t</returns>
		public static CodeExpression ConvertByType(Type t, CodeExpression ce)
		{
			CodeExpression cm;
			bool needCast = false;
			if (t.Equals(typeof(IntPtr)) || t.Equals(typeof(UIntPtr)))
			{
				needCast = true;
				if (IntPtr.Size == 4)
					cm = ConvertByType(typeof(Int32), ce);
				else
					cm = ConvertByType(typeof(Int64), ce);
			}
			else
			{
				string method;
				TypeCode tc = Type.GetTypeCode(t);
				switch (tc)
				{
					case TypeCode.Boolean:
						method = "ToBoolean"; needCast = !t.Equals(typeof(bool));
						break;
					case TypeCode.Byte:
						method = "ToByte"; needCast = !t.Equals(typeof(byte)); break;
					case TypeCode.Char:
						method = "ToChar"; break;
					case TypeCode.DateTime:
						method = "ToDateTime"; break;
					case TypeCode.Decimal:
						method = "ToDecimal"; break;
					case TypeCode.Double:
						method = "ToDouble"; break;
					case TypeCode.Int16:
						method = "ToInt16"; needCast = !t.Equals(typeof(Int16)); break;
					case TypeCode.Int32:
						method = "ToInt32"; needCast = !t.Equals(typeof(Int32)); break;
					case TypeCode.Int64:
						method = "ToInt64"; needCast = !t.Equals(typeof(Int64)); break;
					case TypeCode.SByte:
						method = "ToSByte"; needCast = !t.Equals(typeof(sbyte)); break;
					case TypeCode.Single:
						method = "ToSingle"; break;
					case TypeCode.String:
						return new CodeMethodInvokeExpression(
							ce, "ToString", new CodeExpression[] { });
					case TypeCode.UInt16:
						method = "ToUInt16"; needCast = !t.Equals(typeof(UInt16)); break;
					case TypeCode.UInt32:
						method = "ToUInt32"; needCast = !t.Equals(typeof(UInt32)); break;
					case TypeCode.UInt64:
						method = "ToUInt64"; needCast = !t.Equals(typeof(UInt64)); break;
					default:
						method = "ToDouble"; break;
				}
				cm = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression(typeof(Convert)), method, new CodeExpression[] { ce });
			}
			if (needCast)
			{
				return new CodeCastExpression(t, cm);
			}
			return cm;
		}

		public static CodeExpression GetCoreExpressionFromCast(CodeExpression exp)
		{
			CodeCastExpression cast = exp as CodeCastExpression;
			if (cast == null)
			{
				return exp;
			}
			else
			{
				CodeCastExpression c = cast.Expression as CodeCastExpression;
				if (c != null)
				{
					return GetCoreExpressionFromCast(c);
				}
				return cast.Expression;
			}
		}
		public static string JavascriptConvertElementValue(string val)
		{
			if (!string.IsNullOrEmpty(val))
			{
				if (val.EndsWith(".style.top", StringComparison.Ordinal))
				{
					return string.Format(CultureInfo.InvariantCulture, "{0}offsetTop", val.Substring(0, val.Length - 9));
				}
				else if (val.EndsWith(".style.left", StringComparison.Ordinal))
				{
					return string.Format(CultureInfo.InvariantCulture, "{0}offsetLeft", val.Substring(0, val.Length - 10));
				}
				if (val.EndsWith(".style.height", StringComparison.Ordinal))
				{
					return string.Format(CultureInfo.InvariantCulture, "{0}offsetHeight", val.Substring(0, val.Length - 12));
				}
				if (val.EndsWith(".style.width", StringComparison.Ordinal))
				{
					return string.Format(CultureInfo.InvariantCulture, "{0}offsetWidth", val.Substring(0, val.Length - 11));
				}
			}
			return val;
		}
		#endregion
	}
	/// <summary>
	/// generic event handler
	/// </summary>
	public class DotNetUtilEvent
	{
		static Dictionary<Type, Dictionary<object, Dictionary<string, MethodInfo>>> delegates = new Dictionary<Type, Dictionary<object, Dictionary<string, MethodInfo>>>();
		protected DotNetUtilEvent()
		{
		}
		private static void addReferencedAssemblies(CompilerParameters cp, Assembly a)
		{
			cp.ReferencedAssemblies.Add(a.Location);
			AssemblyName[] ans = a.GetReferencedAssemblies();
			if (ans != null && ans.Length > 0)
			{
				for (int i = 0; i < ans.Length; i++)
				{
					Assembly a0 = Assembly.Load(ans[i]);
					cp.ReferencedAssemblies.Add(a0.Location);
				}
			}
		}
		/// <summary>
		/// create event handler
		/// </summary>
		/// <param name="delegateType">event handler type</param>
		/// <param name="instance">owner of event handler method</param>
		/// <param name="methodName">name of the event handler method</param>
		/// <returns>event handler</returns>
		public static object CreateDelegate(Type delegateType, object instance, string methodName)
		{
			Dictionary<object, Dictionary<string, MethodInfo>> v1 = null;
			if (delegates.ContainsKey(delegateType))
			{
				v1 = delegates[delegateType];
			}
			else
			{
				v1 = new Dictionary<object, Dictionary<string, MethodInfo>>();
				delegates.Add(delegateType, v1);
			}
			Dictionary<string, MethodInfo> v2;
			if (v1.ContainsKey(instance))
			{
				v2 = v1[instance];
			}
			else
			{
				v2 = new Dictionary<string, MethodInfo>();
				v1.Add(instance, v2);
			}
			MethodInfo mi = null;
			if (v2.ContainsKey(methodName))
			{
				mi = v2[methodName];
			}
			else
			{
				string name = "XT" + delegates.Count.ToString();
				string method = "create";
				string paraName = "owner";
				string Namespace = "XT";
				CodeCompileUnit code = new CodeCompileUnit();
				CodeNamespace ns = new CodeNamespace(Namespace);
				ns.Imports.Add(new CodeNamespaceImport("System"));
				code.Namespaces.Add(ns);
				CodeTypeDeclaration td = new CodeTypeDeclaration(name);
				td.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				ns.Types.Add(td);
				CodeMemberMethod mm = new CodeMemberMethod();
				mm.Name = method;
				mm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				mm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), paraName));
				mm.ReturnType = new CodeTypeReference(delegateType);
				td.Members.Add(mm);
				CodeMethodReturnStatement mr = new CodeMethodReturnStatement(
					new CodeObjectCreateExpression(
					delegateType,
					new CodeExpression[]{ new CodeMethodReferenceExpression(
							new CodeCastExpression(instance.GetType(), new CodeArgumentReferenceExpression(paraName)),
							methodName) }
						)
				);
				mm.Statements.Add(mr);
				CompilerParameters cp = new CompilerParameters();
				cp.GenerateExecutable = false;
				cp.GenerateInMemory = true;
				cp.IncludeDebugInformation = false;
				addReferencedAssemblies(cp, instance.GetType().Assembly);
				CSharpCodeProvider ccp = new CSharpCodeProvider();
				CompilerResults cr = ccp.CompileAssemblyFromDom(cp, new CodeCompileUnit[] { code });
				if (cr.Errors.HasErrors)
				{
					System.Text.StringBuilder sb = new StringBuilder("Compiler error.\r\n");
					for (int i = 0; i < cr.Errors.Count; i++)
					{
						sb.Append(cr.Errors[i].ErrorText);
						sb.Append("\r\n");
					}
					throw new VPLException(sb.ToString());
				}
				Type[] tps = cr.CompiledAssembly.GetExportedTypes();
				Type p = null;
				for (int i = 0; i < tps.Length; i++)
				{
					if (tps[i].Name == name)
					{
						p = tps[i];
						mi = p.GetMethod(method, BindingFlags.Static | BindingFlags.Public);
						v2.Add(methodName, mi);
						break;
					}
				}
			}
			if (mi == null)
			{
				throw new VPLException("Error creating event handler.");
			}
			return mi.Invoke(null, new object[] { instance });
		}
		public Type FindComType(object comObject, Type[] comTypes)
		{
			IntPtr iunkwn = Marshal.GetIUnknownForObject(comObject);
			for (int i = 0; i < comTypes.Length; i++)
			{
				if (!comTypes[i].IsInterface)
					continue;
				Guid iid = comTypes[i].GUID;
				if (iid == Guid.Empty)
					continue;
				IntPtr ipointer = IntPtr.Zero;
				Marshal.QueryInterface(iunkwn, ref iid, out ipointer);
				if (ipointer != IntPtr.Zero)
				{
					Marshal.Release(ipointer);
					Marshal.Release(iunkwn);
					return comTypes[i];
				}
			}
			Marshal.Release(iunkwn);
			return null;
		}
	}
}
