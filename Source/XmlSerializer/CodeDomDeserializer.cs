/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Project Serialization in XML
 * License: GNU General Public License v3.0
 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using XmlUtility;
using System.Reflection;
using System.Xml;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using VPL;
using System.Collections;

namespace XmlSerializer
{
	class CodeStatementCollectionDeserializer
	{
		private CodeStatementCollection _statements;
		private ObjectIDmap _objmap;
		private object _root;
		private object _obj;
		private string _objName;

		Dictionary<string, object> _variables;
		public CodeStatementCollectionDeserializer(CodeStatementCollection statements, ObjectIDmap map, XmlNode rootNode, object root, object obj)
		{
			_objmap = map;
			_statements = statements;
			_root = root;
			_obj = obj;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="isNew">True:created by DOM; False:in obj map</param>
		/// <returns></returns>
		private object getComponentByName(string name, out bool isNew)
		{
			if (string.CompareOrdinal(name, _objName) == 0)
			{
				isNew = true;
				return _obj;
			}
			object v = null;
			if (_variables != null)
			{
				if (_variables.ContainsKey(name))
				{
					v = _variables[name];
				}
			}
			if (v == null)
			{
				v = _objmap.GetObjectByName(name);
				isNew = false;
				if (_variables != null && _variables.ContainsKey(name))
				{
					_variables[name] = v;
				}
			}
			else
			{
				isNew = true;
			}
			return v;
		}
		public static Type GetTypeFromTypeReference(CodeTypeReference code)
		{
			if (code == null)
			{
				return null;
			}

			if (code.Options == CodeTypeReferenceOptions.GenericTypeParameter)
			{
				//tb<tas[0],tas[1],...,tas[n-1]>
				throw new XmlSerializerException("Generic type is not supported. {0}", code.BaseType);
			}
			else
			{
				if (code.ArrayRank > 0)
				{
					Type ta = GetTypeFromTypeReference(code.ArrayElementType);
					if (ta != null)
					{
						//ta[a1,a2,...,an-1] -- rank; 
						int[] r = new int[code.ArrayRank];
						for (int i = 0; i < code.ArrayRank; i++)
						{
							r[i] = 0;
						}
						Array a = Array.CreateInstance(ta, r);
						return a.GetType();
					}
					else
					{
						throw new XmlSerializerException("Invalid ArrayElementType:{0}", code.ArrayElementType);
					}
				}
				else
				{
					Type tb = null;
					if (code.UserData.Contains(XmlTags.XML_TYPE))
					{
						tb = code.UserData[XmlTags.XML_TYPE] as Type;
					}
					if (tb == null)
					{
						tb = Type.GetType(code.BaseType);
					}
					if (tb == null)
					{
						throw new XmlSerializerException("Could not load type [{0}]", code.BaseType);
					}
					return tb;
				}
			}
		}
		public object ExecuteMethod(CodeMethodInvokeExpression code)
		{
			if (code == null)
			{
				throw new XmlSerializerException("Error executing CodeStatementCollectionDeserializer.ExecuteMethod(code). code is null");
			}
			if (code.Method == null)
			{
				throw new XmlSerializerException("Error executing CodeStatementCollectionDeserializer.ExecuteMethod(code). code.Method is null");
			}
			if (code.Method.TargetObject == null)
			{
				throw new XmlSerializerException("Error executing CodeStatementCollectionDeserializer.ExecuteMethod(code). code.Method.TargetObject is null");
			}
			bool isNew, isControl;
			object owner = GetObject(code.Method.TargetObject, out isNew, out isControl);
			if (owner == null)
			{
				throw new XmlSerializerException(string.Format(CultureInfo.InvariantCulture, "Error executing CodeStatementCollectionDeserializer.ExecuteMethod(code). Failed to resolve TargetObject. Method:{0}. TargetObject:{1}", code.Method.MethodName, code.Method.TargetObject));
			}
			else
			{
				if (!isNew)
				{
					if (string.CompareOrdinal(code.Method.MethodName, "Add") == 0)
					{
						TableLayoutColumnStyleCollection tlc = owner as TableLayoutColumnStyleCollection;
						if (tlc != null)
						{
							return null;
						}
						TableLayoutRowStyleCollection tlr = owner as TableLayoutRowStyleCollection;
						if (tlr != null)
						{
							return null;
						}
						Type t = owner.GetType();
						if (string.CompareOrdinal(t.Name, "DesignerControlCollection") == 0)
						{
							return null;
						}
					}
				}
				Type[] ps = null;
				object[] vs = null;
				if (code.Parameters != null && code.Parameters.Count > 0)
				{
					bool b, c;
					ps = new Type[code.Parameters.Count];
					vs = new object[code.Parameters.Count];
					for (int i = 0; i < ps.Length; i++)
					{
						vs[i] = GetObject(code.Parameters[i], out b, out c);
						if (vs[i] != null)
						{
							ps[i] = vs[i].GetType();
						}
					}
				}
				MethodInfo mif;
				try
				{
					Type t = owner as Type;
					if (t == null)
					{
						t = owner.GetType();
					}
					if (ps != null)
					{
						mif = t.GetMethod(code.Method.MethodName, ps);
					}
					else
					{
						mif = t.GetMethod(code.Method.MethodName, Type.EmptyTypes);
					}
					if (mif == null)
					{
						MethodInfo[] mifs = t.GetMethods(BindingFlags.Static | BindingFlags.Instance);
						if (mifs != null && mifs.Length > 0)
						{
							for (int i = 0; i < mifs.Length; i++)
							{
								if (string.CompareOrdinal(mifs[i].Name, code.Method.MethodName) == 0)
								{
									ParameterInfo[] pifs = mifs[i].GetParameters();
									if (ps == null || ps.Length == 0)
									{
										if (pifs == null || pifs.Length == 0)
										{
											mif = mifs[i];
											break;
										}
									}
									else
									{
										if (pifs != null && pifs.Length == ps.Length)
										{
											bool match = true;
											for (int k = 0; k < ps.Length; k++)
											{
												if (!pifs[k].ParameterType.Equals(ps[k]))
												{
													match = false;
													break;
												}
											}
											if (match)
											{
												mif = mifs[i];
												break;
											}
										}
									}
								}
							}
						}
						if (ps != null)
						{
							mif = owner.GetType().GetMethod(code.Method.MethodName, ps);
						}
						else
						{
							mif = owner.GetType().GetMethod(code.Method.MethodName, Type.EmptyTypes);
						}
					}
					if (mif == null)
					{
						StringBuilder errSb = new StringBuilder("Error executing method: method information not found. ");
						errSb.Append("Method name:");
						errSb.Append(code.Method.MethodName);
						errSb.Append(". Parameter count:");
						int pn = ps == null ? 0 : ps.Length;
						errSb.Append(pn);
						errSb.Append(". ");
						for (int i = 0; i < pn; i++)
						{
							if (i > 0)
							{
								errSb.Append(",");
							}
							errSb.Append("Parameter ");
							errSb.Append(i);
							errSb.Append(" - type:");
							errSb.Append(ps[i] == null ? "null" : ps[i].Name);
							errSb.Append(" value:");
							errSb.Append(vs[i]);
						}
						throw new XmlSerializerException(errSb.ToString());
					}
					else
					{
						if (mif.IsStatic)
						{
							return mif.Invoke(null, vs);
						}
						if (string.CompareOrdinal(mif.Name, "AddRange") == 0)
						{
							//prevent duplicated adding of items
							DataGridViewColumnCollection dgvcc = owner as DataGridViewColumnCollection;
							if (dgvcc != null)
							{
								if (vs == null || vs.Length != 1)
									return null;
								object[] vs1 = vs[0] as object[];
								if (vs1 == null)
									return null;
								for (int i = 0; i < vs1.Length; i++)
								{
									if (vs1[i] == null)
									{
										return null;
									}
								}
								if (dgvcc.Count == vs1.Length)
								{
									return null;
								}
								dgvcc.Clear();
							}
							else
							{
								if (vs != null && vs.Length > 0)
								{
									IList lst = owner as IList;
									if (lst != null)
									{
										lst.Clear();
									}
								}
							}
						}
						object vret = mif.Invoke(owner, vs);
						return vret;
					}
				}
				catch (Exception e)
				{
					string msg;
					if (ps == null)
					{
						msg = string.Empty;
					}
					else
					{
						if (ps.Length == 0)
						{
							msg = string.Empty;
						}
						else
						{
							StringBuilder sb = new StringBuilder(ps[0] == null ? "null" : ps[0].FullName);
							for (int i = 1; i < ps.Length; i++)
							{
								sb.Append(",");
								if (ps[i] == null)
								{
									sb.Append("null");
								}
								else
								{
									sb.Append(ps[i].FullName);
								}
							}
							msg = sb.ToString();
						}
					}
					throw new XmlSerializerException(string.Format(CultureInfo.InvariantCulture, "Error executing {0}.{1}({2})", owner.GetType().Name, code.Method.MethodName, msg), e);
				}
			}
		}
		private FieldInfo getObjectByName(string name)
		{
			return _root.GetType().GetField(name);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="code"></param>
		/// <param name="isNew">True:created by DOM; False:in obj map</param>
		/// <param name="isControl"></param>
		/// <returns></returns>
		public object GetObject(CodeExpression code, out bool isNew, out bool isControl)
		{
			isNew = true;
			isControl = false;
			CodePrimitiveExpression ce = code as CodePrimitiveExpression;
			if (ce != null)
			{
				return ce.Value;
			}
			CodeThisReferenceExpression v0 = code as CodeThisReferenceExpression;
			if (v0 != null)
			{
				isNew = false;
				isControl = (_root is Control);
				return _root;
			}
			CodeObjectCreateExpression oc = code as CodeObjectCreateExpression;
			if (oc != null)
			{
				Type t = GetTypeFromTypeReference(oc.CreateType);
				object[] ps = null;
				if (oc.Parameters != null)
				{
					bool b, c;
					ps = new object[oc.Parameters.Count];
					for (int i = 0; i < oc.Parameters.Count; i++)
					{
						ps[i] = GetObject(oc.Parameters[i], out b, out c);
					}
				}
				isNew = true;
				isControl = typeof(Control).IsAssignableFrom(t);
				return Activator.CreateInstance(t, ps);
			}
			CodeVariableReferenceExpression v1 = code as CodeVariableReferenceExpression;
			if (v1 != null)
			{
				object o = getComponentByName(v1.VariableName, out isNew);
				isControl = (o is Control);
				return o;
			}
			CodeFieldReferenceExpression v2 = code as CodeFieldReferenceExpression;
			if (v2 != null)
			{
				object v = GetObject(v2.TargetObject, out isNew, out isControl);
				Type vt = v as Type;
				if (vt != null)
				{
					if (vt.IsEnum)
					{
						return Enum.Parse(vt, v2.FieldName);
					}
					else
					{
						FieldInfo f = vt.GetField(v2.FieldName);
						if (f == null)
						{
							throw new XmlSerializerException("Field {0}.{1} not found", vt, v2.FieldName);
						}
						return f.GetValue(null);
					}
				}
				else
				{
					vt = v.GetType();
					FieldInfo f = vt.GetField(v2.FieldName);
					if (f == null)
					{
						throw new XmlSerializerException("Field {0}.{1} not found", vt, v2.FieldName);
					}
					return f.GetValue(v);
				}
			}
			CodePropertyReferenceExpression v3 = code as CodePropertyReferenceExpression;
			if (v3 != null)
			{
				object v = GetObject(v3.TargetObject, out isNew, out isControl);
				if (v == null)
				{
					throw new XmlSerializerException("Property owner not found for [{0}]", v3.PropertyName);
				}
				else
				{
					Type t = v as Type;
					if (t != null)
					{
						if (t.IsEnum)
						{
							return Enum.Parse(t, v3.PropertyName);
						}
						PropertyInfo pif = t.GetProperty(v3.PropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
						if (pif == null)
						{
							throw new XmlSerializerException("Static property [{0}] for type [{1}] not found", v3.PropertyName, t.AssemblyQualifiedName);
						}
						return pif.GetValue(null, new object[] { });
					}
					PropertyDescriptorCollection ps = TypeDescriptor.GetProperties(v);
					if (ps == null)
					{
						throw new XmlSerializerException("Property [{0}] not found for [{1}]", v3.PropertyName, VPLUtil.GetObjectDisplayString(v));
					}
					PropertyDescriptor p = ps[v3.PropertyName];
					if (p == null)
					{
						throw new XmlSerializerException("Property [{0}].[{1}] not found", VPLUtil.GetObjectDisplayString(v), v3.PropertyName);
					}
					return p.GetValue(v);
				}
			}
			CodeMethodInvokeExpression mi = code as CodeMethodInvokeExpression;
			if (mi != null)
			{
				return ExecuteMethod(mi);
			}
			CodeArrayCreateExpression ac = code as CodeArrayCreateExpression;
			if (ac != null)
			{
				Type ti = GetTypeFromTypeReference(ac.CreateType);
				bool bRemoveNull = (typeof(ToolStripItem).Equals(ti));
				object[] objs = null;
				isNew = true;
				if (ac.Initializers != null)
				{
					bool b, c;
					objs = new object[ac.Initializers.Count];
					for (int i = 0; i < ac.Initializers.Count; i++)
					{
						objs[i] = GetObject(ac.Initializers[i], out b, out c);
					}
					if (bRemoveNull)
					{
						List<object> oa = new List<object>();
						for (int i = 0; i < objs.Length; i++)
						{
							if (objs[i] != null)
							{
								oa.Add(objs[i]);
							}
						}
						if (oa.Count < objs.Length)
						{
							objs = new object[oa.Count];
							oa.CopyTo(objs);
						}
					}
				}
				else
				{
					objs = new object[] { };
				}

				Array a = Array.CreateInstance(ti, objs.Length);
				objs.CopyTo(a, 0);
				return a;
			}
			CodeTypeReferenceExpression tr = code as CodeTypeReferenceExpression;
			if (tr != null)
			{
				return GetTypeFromTypeReference(tr.Type);
			}
			CodeCastExpression cast = code as CodeCastExpression;
			if (cast != null)
			{
				Type t = GetTypeFromTypeReference(cast.TargetType);
				object v = GetObject(cast.Expression, out isNew, out isControl);
				bool b;
				object vt = VPLUtil.ConvertObject(v, t, out b);
				if (!b)
				{
				}
				return vt;
			}
			CodeBinaryOperatorExpression bop = code as CodeBinaryOperatorExpression;
			if (bop != null)
			{
				bool bl, br, cl, cr;
				object left = GetObject(bop.Left, out bl, out cl);
				object right = GetObject(bop.Right, out br, out cr);
				switch (bop.Operator)
				{
					case CodeBinaryOperatorType.Add:
						if (left is string || right is string)
						{
							return string.Format(CultureInfo.InvariantCulture, "{0}{1}", left, right);
						}
						else if (left == null)
						{
							return right;
						}
						else if (right == null)
						{
							return left;
						}
						else
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Decimal || tcRight == TypeCode.Decimal)
							{
								return Convert.ToDecimal(left) + Convert.ToDecimal(right);
							}
							if (tcLeft == TypeCode.Double || tcRight == TypeCode.Double)
							{
								return Convert.ToDouble(left) + Convert.ToDouble(right);
							}
							else if (tcLeft == TypeCode.Single || tcRight == TypeCode.Single)
							{
								return Convert.ToSingle(left) + Convert.ToSingle(right);
							}
							else if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) + Convert.ToInt64(right);
							}
							else if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) + Convert.ToUInt64(right);
							}
							else if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) + Convert.ToInt32(right);
							}
							else if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) + Convert.ToUInt32(right);
							}
							else if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) + Convert.ToInt16(right);
							}
							else if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) + Convert.ToUInt16(right);
							}
							else if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) + Convert.ToSByte(right);
							}
							else if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) + Convert.ToByte(right);
							}
							else if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) + Convert.ToChar(right);
							}
							else if (tcLeft == TypeCode.Boolean || tcRight == TypeCode.Boolean)
							{
								return Convert.ToBoolean(left) || Convert.ToBoolean(right);
							}
							return null;
						}
					case CodeBinaryOperatorType.Subtract:
						if (left is string || right is string)
						{
							return string.Format(CultureInfo.InvariantCulture, "{0}-{1}", left, right);
						}
						else if (left == null)
						{
							if (right == null)
								return null;
							switch (Type.GetTypeCode(right.GetType()))
							{
								case TypeCode.Decimal:
									return ((decimal)(-1)) * Convert.ToDecimal(right);
								case TypeCode.Double:
									return ((double)(-1)) * Convert.ToDouble(right);
								case TypeCode.Int16:
									return ((Int16)(-1)) * Convert.ToInt16(right);
								case TypeCode.Int32:
									return ((Int32)(-1)) * Convert.ToInt32(right);
								case TypeCode.Int64:
									return ((Int64)(-1)) * Convert.ToInt64(right);
								case TypeCode.SByte:
									return ((sbyte)(-1)) * Convert.ToSByte(right);
								case TypeCode.Single:
									return ((Single)(-1)) * Convert.ToSingle(right);
								case TypeCode.UInt16:
									return ((Int16)(-1)) * Convert.ToUInt16(right);
								case TypeCode.UInt32:
									return ((Int32)(-1)) * Convert.ToUInt32(right);
							}
							return right;
						}
						else if (right == null)
						{
							return left;
						}
						else
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Decimal || tcRight == TypeCode.Decimal)
							{
								return Convert.ToDecimal(left) - Convert.ToDecimal(right);
							}
							else if (tcLeft == TypeCode.Double || tcRight == TypeCode.Double)
							{
								return Convert.ToDouble(left) - Convert.ToDouble(right);
							}
							else if (tcLeft == TypeCode.Single || tcRight == TypeCode.Single)
							{
								return Convert.ToSingle(left) - Convert.ToSingle(right);
							}
							else if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) - Convert.ToInt64(right);
							}
							else if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) - Convert.ToUInt64(right);
							}
							else if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) - Convert.ToInt32(right);
							}
							else if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) - Convert.ToUInt32(right);
							}
							else if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) - Convert.ToInt16(right);
							}
							else if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) - Convert.ToUInt16(right);
							}
							else if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) - Convert.ToSByte(right);
							}
							else if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) - Convert.ToByte(right);
							}
							else if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) - Convert.ToChar(right);
							}
							else if (tcLeft == TypeCode.Boolean || tcRight == TypeCode.Boolean)
							{
								return Convert.ToBoolean(left) || Convert.ToBoolean(right);
							}
							return null;
						}
					case CodeBinaryOperatorType.Multiply:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Decimal || tcRight == TypeCode.Decimal)
							{
								return Convert.ToDecimal(left) * Convert.ToDecimal(right);
							}
							else if (tcLeft == TypeCode.Double || tcRight == TypeCode.Double)
							{
								return Convert.ToDouble(left) * Convert.ToDouble(right);
							}
							else if (tcLeft == TypeCode.Single || tcRight == TypeCode.Single)
							{
								return Convert.ToSingle(left) * Convert.ToSingle(right);
							}
							else if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) * Convert.ToInt64(right);
							}
							else if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) * Convert.ToUInt64(right);
							}
							else if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) * Convert.ToInt32(right);
							}
							else if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) * Convert.ToUInt32(right);
							}
							else if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) * Convert.ToInt16(right);
							}
							else if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) * Convert.ToUInt16(right);
							}
							else if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) * Convert.ToSByte(right);
							}
							else if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) * Convert.ToByte(right);
							}
							else if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) * Convert.ToChar(right);
							}
							else if (tcLeft == TypeCode.Boolean || tcRight == TypeCode.Boolean)
							{
								return Convert.ToBoolean(left) && Convert.ToBoolean(right);
							}
						}
						return 0;
					case CodeBinaryOperatorType.Divide:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Decimal || tcRight == TypeCode.Decimal)
							{
								return Convert.ToDecimal(left) / Convert.ToDecimal(right);
							}
							else if (tcLeft == TypeCode.Double || tcRight == TypeCode.Double)
							{
								return Convert.ToDouble(left) / Convert.ToDouble(right);
							}
							else if (tcLeft == TypeCode.Single || tcRight == TypeCode.Single)
							{
								return Convert.ToSingle(left) / Convert.ToSingle(right);
							}
							else if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) / Convert.ToInt64(right);
							}
							else if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) / Convert.ToUInt64(right);
							}
							else if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) / Convert.ToInt32(right);
							}
							else if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) / Convert.ToUInt32(right);
							}
							else if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) / Convert.ToInt16(right);
							}
							else if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) / Convert.ToUInt16(right);
							}
							else if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) / Convert.ToSByte(right);
							}
							else if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) / Convert.ToByte(right);
							}
							else if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) / Convert.ToChar(right);
							}
						}
						return 0;
					case CodeBinaryOperatorType.BitwiseAnd:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) & Convert.ToInt64(right);
							}
							if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) & Convert.ToUInt64(right);
							}
							if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) & Convert.ToInt32(right);
							}
							if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) & Convert.ToUInt32(right);
							}
							if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) & Convert.ToInt16(right);
							}
							if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) & Convert.ToUInt16(right);
							}
							if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) & Convert.ToSByte(right);
							}
							if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) & Convert.ToByte(right);
							}
							if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) & Convert.ToChar(right);
							}
						}
						return 0;
					case CodeBinaryOperatorType.BitwiseOr:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) | Convert.ToInt64(right);
							}
							if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) | Convert.ToUInt64(right);
							}
							if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) | Convert.ToInt32(right);
							}
							if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) | Convert.ToUInt32(right);
							}
							if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) | Convert.ToInt16(right);
							}
							if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) | Convert.ToUInt16(right);
							}
							if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) | Convert.ToSByte(right);
							}
							if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) | Convert.ToByte(right);
							}
							if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) | Convert.ToChar(right);
							}
						}
						return 0;
					case CodeBinaryOperatorType.BooleanAnd:
						return Convert.ToBoolean(left) && Convert.ToBoolean(right);
					case CodeBinaryOperatorType.BooleanOr:
						return Convert.ToBoolean(left) || Convert.ToBoolean(right);
					case CodeBinaryOperatorType.GreaterThan:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) > 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) > Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) > Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.GreaterThanOrEqual:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) >= 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) >= Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) >= Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.IdentityEquality:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) == 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) == Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) == Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.IdentityInequality:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) != 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) != Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) != Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.LessThan:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) < 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) < Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) < Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.LessThanOrEqual:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) <= 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) <= Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) < Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.ValueEquality:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.String || tcRight == TypeCode.String)
							{
								return string.CompareOrdinal(Convert.ToString(left), Convert.ToString(right)) == 0;
							}
							if (tcLeft == TypeCode.DateTime || tcRight == TypeCode.DateTime)
							{
								return Convert.ToDateTime(left) == Convert.ToDateTime(right);
							}
							return Convert.ToDouble(left) == Convert.ToDouble(right);
						}
						return false;
					case CodeBinaryOperatorType.Modulus:
						if (left != null && right != null)
						{
							TypeCode tcLeft = Type.GetTypeCode(left.GetType());
							TypeCode tcRight = Type.GetTypeCode(right.GetType());
							if (tcLeft == TypeCode.Int64 || tcRight == TypeCode.Int64)
							{
								return Convert.ToInt64(left) % Convert.ToInt64(right);
							}
							if (tcLeft == TypeCode.UInt64 || tcRight == TypeCode.UInt64)
							{
								return Convert.ToUInt64(left) % Convert.ToUInt64(right);
							}
							if (tcLeft == TypeCode.Int32 || tcRight == TypeCode.Int32)
							{
								return Convert.ToInt32(left) % Convert.ToInt32(right);
							}
							if (tcLeft == TypeCode.UInt32 || tcRight == TypeCode.UInt32)
							{
								return Convert.ToUInt32(left) % Convert.ToUInt32(right);
							}
							if (tcLeft == TypeCode.Int16 || tcRight == TypeCode.Int16)
							{
								return Convert.ToInt16(left) % Convert.ToInt16(right);
							}
							if (tcLeft == TypeCode.UInt16 || tcRight == TypeCode.UInt16)
							{
								return Convert.ToUInt16(left) % Convert.ToUInt16(right);
							}
							if (tcLeft == TypeCode.SByte || tcRight == TypeCode.SByte)
							{
								return Convert.ToSByte(left) % Convert.ToSByte(right);
							}
							if (tcLeft == TypeCode.Byte || tcRight == TypeCode.Byte)
							{
								return Convert.ToByte(left) % Convert.ToByte(right);
							}
							if (tcLeft == TypeCode.Char || tcRight == TypeCode.Char)
							{
								return Convert.ToChar(left) % Convert.ToChar(right);
							}
						}
						return 0;
					default:
						return null;
				}
			}
			throw new XmlSerializerException("Unhandled object reference {0}. Please contact Limnor Studio team to add support for it.", code.GetType());
		}
		public void D_CodeAssignStatement(CodeAssignStatement code)
		{
			bool isNew, isControl;
			CodeVariableReferenceExpression v1 = code.Left as CodeVariableReferenceExpression;
			if (v1 != null)
			{
				FieldInfo v = getObjectByName(v1.VariableName);
				if (v == null)
				{
					return;
				}
				else
				{
					object v0 = GetObject(code.Right, out isNew, out isControl);
					if (isNew)
					{
					}
					v.SetValue(_root, v0);
					//}
					return;
				}
			}
			CodeFieldReferenceExpression v2 = code.Left as CodeFieldReferenceExpression;
			if (v2 != null)
			{
				object v = GetObject(v2.TargetObject, out isNew, out isControl);
				if (isNew)
				{
				}
				FieldInfo f = v.GetType().GetField(v2.FieldName);
				object v0 = GetObject(code.Right, out isNew, out isControl);
				if (isNew)
				{
				}
				f.SetValue(v, v0);
				return;
			}
			CodePropertyReferenceExpression v3 = code.Left as CodePropertyReferenceExpression;
			if (v3 != null)
			{
				object v = GetObject(v3.TargetObject, out isNew, out isControl);
				if (isNew)
				{
				}
				if (v == null)
				{
				}
				else
				{
					PropertyInfo f = v.GetType().GetProperty(v3.PropertyName);
					object v0 = GetObject(code.Right, out isNew, out isControl);
					if (isNew)
					{
					}
					f.SetValue(v, v0, null);
				}
				return;
			}
			throw new XmlSerializerException("Unhandled assignment.Left [{0}]", code.Left.GetType());
		}
		public void D_CodeAttachEventStatement(CodeAttachEventStatement code)
		{
			bool isNew, isControl;
			object v = GetObject(code.Event.TargetObject, out isNew, out isControl);
			if (isNew)
			{
			}
			EventInfo e = v.GetType().GetEvent(code.Event.EventName);
			Delegate dg = (Delegate)GetObject(code.Listener, out isNew, out isControl);
			if (isNew)
			{
			}
			e.AddEventHandler(v, dg);
		}
		public void D_CodeCommentStatement(CodeCommentStatement code)
		{
		}
		public void D_CodeConditionStatement(CodeConditionStatement code)
		{
			bool isNew, isControl;
			bool b = (bool)GetObject(code.Condition, out isNew, out isControl);
			if (isNew)
			{
			}
			if (b)
			{
				for (int i = 0; i < code.TrueStatements.Count; i++)
				{
					ProcessStatement(code.TrueStatements[i]);
				}
			}
			else
			{
				for (int i = 0; i < code.FalseStatements.Count; i++)
				{
					ProcessStatement(code.FalseStatements[i]);
				}
			}
		}
		public void D_CodeExpressionStatement(CodeExpressionStatement code)
		{
			CodeMethodInvokeExpression cm = code.Expression as CodeMethodInvokeExpression;
			if (cm != null)
			{
				ExecuteMethod(cm);
				return;
			}
			throw new XmlSerializerException("Unsupported code expression statement. [{0}]", code.Expression.GetType());
		}
		public void D_CodeGotoStatement(CodeGotoStatement code)
		{
			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeIterationStatement(CodeIterationStatement code)
		{
			bool isNew, isControl;
			ProcessStatement(code.InitStatement);
			while (true)
			{
				bool b = (bool)GetObject(code.TestExpression, out isNew, out isControl);
				if (!b)
				{
					break;
				}
				if (!isNew)
				{
					break;
				}
				for (int i = 0; i < code.Statements.Count; i++)
				{
					ProcessStatement(code.Statements[i]);
				}
				ProcessStatement(code.IncrementStatement);
			}

		}
		public void D_CodeLabeledStatement(CodeLabeledStatement code)
		{

			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeMethodReturnStatement(CodeMethodReturnStatement code)
		{
			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeRemoveEventStatement(CodeRemoveEventStatement code)
		{
			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeSnippetStatement(CodeSnippetStatement code)
		{
			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeThrowExceptionStatement(CodeThrowExceptionStatement code)
		{
			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeTryCatchFinallyStatement(CodeTryCatchFinallyStatement code)
		{
			throw new NotImplementedException(code.GetType().Name);
		}
		public void D_CodeVariableDeclarationStatement(CodeVariableDeclarationStatement code)
		{
			if (_variables == null)
			{
				_variables = new Dictionary<string, object>();
			}
			if (_variables.ContainsKey(code.Name) && _variables[code.Name] != null)
			{
				return;
			}
			object v = null; //it is a variable declaration, value can be null
			if (code.InitExpression != null)
			{
				bool isNew, isControl;
				v = GetObject(code.InitExpression, out isNew, out isControl);
				if (isNew)
				{
					if (_variables.ContainsKey(code.Name))
					{
						_variables[code.Name] = v;
					}
					else
					{
						_variables.Add(code.Name, v);
					}
				}
			}
			else
			{
				if (!_variables.ContainsKey(code.Name))
				{
					_variables.Add(code.Name, v);
				}
			}
		}
		public void ProcessStatement(CodeStatement code)
		{
			CodeCommentStatement c3 = code as CodeCommentStatement;
			if (c3 != null)
			{
				D_CodeCommentStatement(c3);
				return;
			}
			CodeAssignStatement c = code as CodeAssignStatement;
			if (c != null)
			{
				D_CodeAssignStatement(c);
				return;
			}
			CodeAttachEventStatement c2 = code as CodeAttachEventStatement;
			if (c2 != null)
			{
				D_CodeAttachEventStatement(c2);
				return;
			}
			CodeConditionStatement c4 = code as CodeConditionStatement;
			if (c4 != null)
			{
				D_CodeConditionStatement(c4);
				return;
			}
			CodeExpressionStatement c5 = code as CodeExpressionStatement;
			if (c5 != null)
			{
				D_CodeExpressionStatement(c5);
				return;
			}
			CodeVariableDeclarationStatement c14 = code as CodeVariableDeclarationStatement;
			if (c14 != null)
			{
				D_CodeVariableDeclarationStatement(c14);
				return;
			}
			CodeGotoStatement c6 = code as CodeGotoStatement;
			if (c6 != null)
			{
				D_CodeGotoStatement(c6);
				return;
			}
			CodeIterationStatement c7 = code as CodeIterationStatement;
			if (c7 != null)
			{
				D_CodeIterationStatement(c7);
				return;
			}
			CodeLabeledStatement c8 = code as CodeLabeledStatement;
			if (c8 != null)
			{
				D_CodeLabeledStatement(c8);
				return;
			}
			CodeMethodReturnStatement c9 = code as CodeMethodReturnStatement;
			if (c9 != null)
			{
				D_CodeMethodReturnStatement(c9);
				return;
			}
			CodeRemoveEventStatement c10 = code as CodeRemoveEventStatement;
			if (c10 != null)
			{
				D_CodeRemoveEventStatement(c10);
				return;
			}
			CodeSnippetStatement c11 = code as CodeSnippetStatement;
			if (c11 != null)
			{
				D_CodeSnippetStatement(c11);
				return;
			}
			CodeThrowExceptionStatement c12 = code as CodeThrowExceptionStatement;
			if (c12 != null)
			{
				D_CodeThrowExceptionStatement(c12);
				return;
			}
			CodeTryCatchFinallyStatement c13 = code as CodeTryCatchFinallyStatement;
			if (c13 != null)
			{
				D_CodeTryCatchFinallyStatement(c13);
				return;
			}
			throw new XmlSerializerException("Unhandled statement: {0}", code.GetType());
		}

		public void Deserialize()
		{
			//the first statement declares the component
			if (_statements == null)
			{
				throw new XmlSerializerException("no statements ");
			}
			if (_statements.Count < 1)
			{
				return;
			}
			CodeVariableDeclarationStatement vd = _statements[0] as CodeVariableDeclarationStatement;
			if (vd == null)
			{
				throw new XmlSerializerException("Invalid first statement");
			}
			_variables = new Dictionary<string, object>();
			_variables.Add(vd.Name, _obj);
			_objName = vd.Name;
			int nStart = 1;
			if (_statements.Count > 1)
			{
				CodeAssignStatement cas = _statements[1] as CodeAssignStatement;
				if (cas != null)
				{
					CodeVariableReferenceExpression cv = cas.Left as CodeVariableReferenceExpression;
					if (cv != null)
					{
						if (string.CompareOrdinal(cv.VariableName, _objName) == 0)
						{
							nStart++;
						}
					}
				}
			}
			for (int i = nStart; i < _statements.Count; i++)
			{
				CodeStatement code = _statements[i];
				try
				{
					ProcessStatement(code);
				}
				catch (Exception err)
				{
					this._objmap.Reader.addErrStr2(SerializerException.FormExceptionText(err));
				}
			}
		}
	}
}