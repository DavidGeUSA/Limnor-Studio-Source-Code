/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	XML Utility
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Xml;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.ComponentModel.Design.Serialization;
using System.Collections.Specialized;
using System.Collections;
using System.Globalization;
using VPL;
using System.Windows.Forms;
using System.Drawing;
using System.Data;

namespace XmlUtility
{
	public class ObjectXmlReader : IXmlCodeReader
	{
		private static readonly Attribute[] propertyAttributes = new Attribute[] 
        {
			DesignOnlyAttribute.No
		};
		//
		private Stack _objectStack;
		private Stack<List<IPostOwnersSerialize>> _postOwnersDeserializers;
		private Dictionary<string, XmlDocument> _auxDocuments;
		private Dictionary<object, Dictionary<PropertyDescriptor, List<BindingLoader>>> _bindings;
		private Dictionary<object, Dictionary<PropertyDescriptor, string>> _references;
		private Dictionary<PropertyDescriptor, object> _propertiesNotSet;
		private bool _lightRead;
		private StringCollection errors;
		public ObjectXmlReader()
		{
		}
		public Stack ObjectStack { get { return _objectStack; } }
		public Dictionary<object, Dictionary<PropertyDescriptor, List<BindingLoader>>> Bindings
		{
			get
			{
				return _bindings;
			}
		}
		public Dictionary<object, Dictionary<PropertyDescriptor, string>> References
		{
			get
			{
				return _references;
			}
		}
		public void PushPostOwnersDeserializers(List<IPostOwnersSerialize> objs)
		{
			if (_postOwnersDeserializers == null)
			{
				_postOwnersDeserializers = new Stack<List<IPostOwnersSerialize>>();
			}
			_postOwnersDeserializers.Push(objs);
		}
		public IPostOwnersSerialize[] PopPostSerializers()
		{
			if (_postOwnersDeserializers != null && _postOwnersDeserializers.Count > 0)
			{
				List<IPostOwnersSerialize> objs = _postOwnersDeserializers.Pop();
				if (objs != null)
				{
					return objs.ToArray();
				}
			}
			return null;
		}
		public void AddPostOwnersDeserializers(IPostOwnersSerialize obj)
		{
			if (_postOwnersDeserializers != null && _postOwnersDeserializers.Count > 0)
			{
				List<IPostOwnersSerialize> objs = _postOwnersDeserializers.Peek();
				if (objs != null)
				{
					objs.Add(obj);
				}
			}
		}
		public UInt32 ClassId { get { return 0; } }
		public string ProjectFolder { get { return string.Empty; } }
		private bool _forCompile;
		public bool IsForCompile { get { return _forCompile; } set { _forCompile = value; } }
		private void push(object obj)
		{
			if (_objectStack == null)
			{
				_objectStack = new Stack();
			}
			_objectStack.Push(obj);
		}
		private XmlDocument getAuxDocument(XmlNode node)
		{
			string filename = null;
			XmlAttribute xa = node.OwnerDocument.DocumentElement.Attributes[XmlTags.XMLATT_filename];
			if (xa != null)
			{
				string sFile = xa.Value;
				if (System.IO.File.Exists(sFile))
				{
					filename = sFile + ".aux";
				}
			}
			if (!string.IsNullOrEmpty(filename))
			{
				XmlDocument doc;
				filename = filename.ToLowerInvariant();
				if (_auxDocuments != null)
				{
					if (_auxDocuments.TryGetValue(filename, out doc))
						return doc;
				}
				if (System.IO.File.Exists(filename))
				{
					doc = new XmlDocument();
					doc.Load(filename);
					if (_auxDocuments == null)
					{
						_auxDocuments = new Dictionary<string, XmlDocument>();
					}
					_auxDocuments.Add(filename, doc);
					return doc;
				}
			}
			return null;
		}
		private void logTrace(string message)
		{
		}
		private void logTrace(string message, params object[] values)
		{
		}
		private void logTraceIncIndent()
		{
		}
		private void logTraceDecIndent()
		{
		}
		public void addErrStr2(string s, params object[] values)
		{
			if (values == null || values.Length == 0)
			{
				addErrStr(s);
			}
			else
			{
				addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture, s, values));
			}
		}
		private void addErrStr(string s)
		{
			if (errors == null)
				errors = new StringCollection();
			errors.Add(s);
		}
		public StringCollection Errors
		{
			get
			{
				return errors;
			}
		}
		public bool HasErrors
		{
			get
			{
				return (errors != null && errors.Count > 0);
			}
		}
		public void ResetErrors()
		{
			errors = null;
		}
		public void SetLightRead(bool lightRead)
		{
			_lightRead = lightRead;
		}
		public bool GetAttributeBoolDefFalse(XmlNode node, string name)
		{
			return XmlUtil.GetAttributeBoolDefFalse(node, name);
		}
		public void ReadStaticProperties(XmlNode node)
		{
			XmlNodeList nodeList = node.SelectNodes(string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"{0}/{1}", XmlTags.XML_StaticList, XmlTags.XML_StaticValue));
			foreach (XmlNode nd in nodeList)
			{
				string acid;
				Type t = XmlUtil.GetLibTypeAttribute(nd, out acid);
				ICustomEventMethodType icme = VPLUtil.StaticOwnerForType(t) as ICustomEventMethodType;
				PropertyDescriptorCollection props = icme.GetProperties(EnumReflectionMemberInfoSelectScope.StaticOnly, false, true, false);
				ReadProperties(nd, icme, props, XmlTags.XML_PROPERTY);
			}
		}
		public void ReloadObjectFromXmlNode<T>(XmlNode node, T obj)
		{
			ReadObjectFromXmlNode(node, obj, typeof(T), null);
		}
		public void ReadObjectFromXmlNode(XmlNode node, object obj, Type type, object parentObject)
		{
			IXmlNodeHolder xmlHolder = obj as IXmlNodeHolder;
			if (xmlHolder != null)
			{
				xmlHolder.DataXmlNode = node;
			}
			ISerializeNotify notify = obj as ISerializeNotify;
			if (notify != null)
			{
				notify.ReadingProperties = true;
			}
			IBeforeXmlNodeSerialize before = obj as IBeforeXmlNodeSerialize;
			if (before != null)
			{
				before.OnReadFromXmlNode(this, node);
			}
			ICustomSerialization customerSaver = obj as ICustomSerialization;
			if (customerSaver != null)
			{
				logTrace("ICustomSerialization");
				customerSaver.OnReadFromXmlNode(this, node);
			}
			else
			{
				ICustomContentSerialization ccs = obj as ICustomContentSerialization;
				if (ccs != null)
				{
					logTrace("ICustomContentSerialization");
					XmlNode nodeContent = node.SelectSingleNode(XmlTags.XML_Content);
					if (nodeContent != null)
					{
						ReadCustomValue(nodeContent, ccs);
					}
					else
					{
						logTrace("No content");
					}
				}
				else
				{
					logTrace("Properties");
					ReadProperties(node, obj);
					if (type.IsArray)
					{
						XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
						if (nodes.Count > 0)
						{
							logTrace("Array");
							ReadArray(nodes, (Array)obj, parentObject);
						}
					}
					else if (typeof(IList).IsAssignableFrom(type))
					{
						XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
						if (nodes.Count > 0)
						{
							IList list = obj as IList;
							logTrace("IList");
							ReadList(nodes, list, parentObject);
						}
					}
				}
			}
			if (notify != null)
			{
				notify.ReadingProperties = false;
			}
			try
			{
				ISerializationProcessor spr = obj as ISerializationProcessor;
				if (spr != null)
				{
					spr.OnDeserialization(node);
				}
			}
			catch (Exception err)
			{
				addErrStr(SerializerException.FormExceptionText(err));
			}
		}
		public object ReadLightObject(XmlNode node, object parentObject)
		{
			bool lightRead = _lightRead;
			_lightRead = true;
			object v = ReadObject(node, parentObject);
			_lightRead = lightRead;
			return v;
		}
		public object ReadObject(XmlNode node, object parentObject)
		{
			return ReadObject(node, parentObject, null);
		}
		public T ReadObject<T>(XmlNode node)
		{
			return (T)ReadObject(node, null, typeof(T));
		}
		public T ReadObject<T>(XmlNode node, object parentObject)
		{
			object v = ReadObject(node, parentObject, typeof(T));
			if (v != null)
			{
				Type t = v.GetType();
				if (!typeof(T).IsAssignableFrom(t))
				{
					throw new XmlSerializerException("Cannot cast {0} to {1}", t.FullName, typeof(T).FullName);
				}
			}
			return (T)v;
		}
		public object ReadObject(XmlNode node, object parentObject, Type type)
		{
			if (node == null)
			{
				addErrStr("XmlNode is null when calling ReadObject");
				return null;
			}
			else
			{
				string acid;
				object obj = null;
				Type t0 = XmlUtil.GetLibTypeAttribute(node, out acid);
				if (t0 != null)
				{
					if (type == null)
					{
						type = t0;
					}
					else
					{
						if (!t0.IsAssignableFrom(type))
						{
							if (!type.IsAssignableFrom(t0))
							{
								addErrStr2("Cannot load type [{0}] from [{1}]. Type mismatch: {2} and {3}", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetPath(node), type.FullName, t0.FullName);
								return null;
							}
							else
							{
								type = t0;
							}
						}
					}
				}
				if (type == null)
				{
					addErrStr2("ReadObject: cannot load type [{0}] from [{1}]", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetPath(node));
				}
				else
				{
					if (type.Equals(typeof(Binding)))
					{
						return null;
					}
					if (XmlUtil.IsValueType(type))
					{
						TypeConverter converter = TypeDescriptor.GetConverter(type);
						obj = converter.ConvertFromInvariantString(node.InnerText);
						logTrace("ReadObject: value is a value [{0}]", node.InnerText);
					}
					else
					{
						obj = createInstance(node, parentObject, type);
						if (obj != null)
						{
							push(obj);
							try
							{
								ReadObjectFromXmlNode(node, obj, type, parentObject);
							}
							catch
							{
								throw;
							}
							finally
							{
								_objectStack.Pop();
							}
						}
					}
				}
				return obj;
			}
		}
		public object ReadControl(XmlNode node, IList list, object parentObject)
		{
			if (node == null)
			{
				addErrStr("XmlNode is null when calling ReadObject");
				return null;
			}
			else
			{
				string acid;
				object obj = null;
				Type type = XmlUtil.GetLibTypeAttribute(node, out acid);
				if (type == null)
				{
					addErrStr2("ReadControl: cannot load type [{0}] from [{1}]", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetPath(node));
				}
				else
				{
					if (XmlUtil.IsValueType(type))
					{
						TypeConverter converter = TypeDescriptor.GetConverter(type);
						obj = converter.ConvertFromInvariantString(node.InnerText);
						logTrace("ReadControl: value is a value [{0}]", node.InnerText);
					}
					else
					{
						obj = createInstance(node, parentObject, type);
						if (obj != null)
						{
							push(obj);
							try
							{
								list.Add(obj);
								ReadObjectFromXmlNode(node, obj, type, parentObject);
							}
							catch
							{
								throw;
							}
							finally
							{
								_objectStack.Pop();
							}
						}
					}
				}
				return obj;
			}
		}
		public void ReadProperties(XmlNode node, object obj)
		{
			_propertiesNotSet = null;
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj, propertyAttributes);
			ReadProperties(node, obj, properties, XmlTags.XML_PROPERTY);
			DataTable tbl = obj as DataTable;
			if (tbl != null)
			{
				if (_propertiesNotSet != null)
				{
					foreach (KeyValuePair<PropertyDescriptor, object> kv in _propertiesNotSet)
					{
						if (string.Compare(kv.Key.Name, "PrimaryKey", StringComparison.Ordinal) == 0)
						{
							DataColumn[] dcc = kv.Value as DataColumn[];
							List<DataColumn> ldc = new List<DataColumn>();
							foreach (DataColumn dc in dcc)
							{
								bool b = false;
								for (int i = 0; i < tbl.Columns.Count; i++)
								{
									if (string.Compare(tbl.Columns[i].ColumnName, dc.ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
									{
										ldc.Add(tbl.Columns[i]);
										b = true;
										break;
									}
								}
								if (!b)
								{
									tbl.Columns.Add(dc);
									ldc.Add(dc);
								}
							}
							kv.Key.SetValue(tbl, ldc.ToArray());
						}
					}
				}
			}
		}
		public void ReadProperties(XmlNode node, object obj, PropertyDescriptorCollection properties, string elementName)
		{
			if (properties.Count > 0)
			{
				bool isCustomProperty = (properties[0] is IIdentityByInteger);
				logTraceIncIndent();
				XmlNodeList propNodeList = node.SelectNodes(elementName);
				SortedList<UInt32, PropertyReadOrder> sortedProperties = new SortedList<UInt32, PropertyReadOrder>();
				UInt32 idx = 0;
				foreach (XmlNode propNode in propNodeList)
				{
					PropertyDescriptor prop = null;
					if (isCustomProperty)
					{
						UInt64 id = XmlUtil.GetAttributeUInt64(propNode, XmlTags.XMLATT_PropID);
						foreach (PropertyDescriptor p in properties)
						{
							IIdentityByInteger iid = p as IIdentityByInteger;
							if (iid.WholeId == id)
							{
								prop = p;
								break;
							}
						}
					}
					else
					{
						prop = properties.Find(XmlUtil.GetNameAttribute(propNode), false);
					}
					if (prop != null)
					{
						PropertyReadOrder pro = new PropertyReadOrder(prop, propNode, idx++, (UInt32)properties.Count);
						sortedProperties.Add(pro.ReadOrder, pro);
					}
				}
				Form frm = obj as Form;
				if (frm != null)
				{
					frm.SuspendLayout();
				}
				IEnumerator<KeyValuePair<UInt32, PropertyReadOrder>> ie = sortedProperties.GetEnumerator();
				while (ie.MoveNext())
				{
					ReadProperty(ie.Current.Value.Property, ie.Current.Value.Node, obj, elementName);
				}
				if (frm != null)
				{
					frm.ResumeLayout(false);
				}
				logTraceDecIndent();
			}
		}
		public void ReadProperty(PropertyDescriptor prop, XmlNode node, object value, string elementName)
		{
			try
			{
				if (_lightRead)
				{
					if (XmlUtil.NotForLightRead(prop.Attributes))
					{
						return;
					}
				}
				logTrace("Property {0} {1}", prop.Name, prop.PropertyType);
				DesignerSerializationVisibilityAttribute visibility = (DesignerSerializationVisibilityAttribute)prop.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
				switch (visibility.Visibility)
				{
					case DesignerSerializationVisibility.Visible:
						logTrace("Visible");
						if (prop.PropertyType.IsArray)
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							if (nodes.Count > 0)
							{
								logTrace("Array item count {0}", nodes.Count);
								Array propValue = (Array)Activator.CreateInstance(prop.PropertyType, nodes.Count);
								ReadArray(nodes, propValue, value);
								DataTable tbl = value as DataTable;
								if (tbl != null)
								{
									if (string.Compare(prop.Name, "PrimaryKey", StringComparison.Ordinal) == 0)
									{
										if (_propertiesNotSet == null)
										{
											_propertiesNotSet = new Dictionary<PropertyDescriptor, object>();
										}
										_propertiesNotSet.Add(prop, propValue);
										return;
									}
								}
								prop.SetValue(value, propValue);
							}
						}
						else if (typeof(IList).IsAssignableFrom(prop.PropertyType))
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							if (nodes.Count > 0)
							{
								logTrace("IList item count {0}", nodes.Count);
								IList list = null;
								object propValue = prop.GetValue(value);
								if (propValue != null)
								{
									list = (IList)propValue;
								}
								else
								{
									if (prop.IsReadOnly)
									{
										addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"IList property {0} is null and read-only as {1}", prop.Name, prop.PropertyType));
									}
									else
									{
										logTrace("creating IList object instance of type {0}", prop.PropertyType);
										list = (IList)Activator.CreateInstance(prop.PropertyType);
										prop.SetValue(value, list);
										logTrace("created IList object instance");
									}
								}
								if (list != null)
								{
									if (value is Control && string.Compare(prop.Name, "Controls", StringComparison.Ordinal) == 0)
									{
										ReadControls(nodes, list, value);
									}
									else
									{
										ReadList(nodes, list, value);//, (IList)propValue);
									}
								}
							}
						}
						else if (!prop.IsReadOnly)
						{
							object obj;
							logTrace("reading value of type {0} ...", prop.PropertyType);
							TypeConverter tct = null;
							for (int i = 0; i < prop.Attributes.Count; i++)
							{
								TypeConverterAttribute tcta = prop.Attributes[i] as TypeConverterAttribute;
								if (tcta != null)
								{
									Type tc = Type.GetType(tcta.ConverterTypeName);
									ConstructorInfo cif = tc.GetConstructor(new Type[] { });
									if (cif != null)
									{
										tct = Activator.CreateInstance(tc) as TypeConverter;
										if (tct != null)
										{
											break;
										}
									}
								}
							}
							if (tct == null)
							{
								tct = TypeDescriptor.GetConverter(prop.PropertyType);
							}
							if (ReadValue(node, tct, prop, value, out obj))
							{
								prop.SetValue(value, obj);
							}
						}
						break;
					case DesignerSerializationVisibility.Content:
						logTrace("Content");
						if (typeof(IList).IsAssignableFrom(prop.PropertyType))
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							logTrace("IList item count:{0}", nodes.Count);
							if (nodes.Count > 0)
							{
								IList list = null;
								object propValue = prop.GetValue(value);
								if (propValue != null)
								{
									list = (IList)propValue;
								}
								else
								{
									if (prop.IsReadOnly)
									{
										addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"IList property {0} is null and read-only as {1}", prop.Name, prop.PropertyType));
									}
									else
									{
										try
										{
											list = (IList)Activator.CreateInstance(prop.PropertyType);
											logTrace("created IList instance");
										}
										catch (Exception e)
										{
											addErrStr(string.Format(System.Globalization.CultureInfo.InvariantCulture,
											"IList property {0}({1}) is null and failed to create it. {2}", prop.Name, prop.PropertyType, e.Message));
										}
									}
								}
								if (list != null)
								{
									if (value is Control && string.Compare(prop.Name, "Controls", StringComparison.Ordinal) == 0)
									{
										ReadControls(nodes, list, value);
									}
									else
									{
										ReadList(nodes, list, value);
									}
								}
							}
						}
						else if (typeof(ICollection).IsAssignableFrom(prop.PropertyType))
						{
							XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
							logTrace("ICollection item count:{0}", nodes.Count);
							if (nodes.Count > 0)
							{
								if (typeof(ControlBindingsCollection).Equals(prop.PropertyType))
								{
									foreach (XmlNode nd in nodes)
									{
										string member = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_member);
										string propName = XmlUtil.GetAttribute(nd, XmlTags.XMLATT_PropID);
										XmlNode nr = nd.SelectSingleNode(XmlTags.XML_Reference);
										string dataSourceName = null;
										if (nr != null)
										{
											dataSourceName = XmlUtil.GetNameAttribute(nr);
										}
										if (_bindings == null)
										{
											_bindings = new Dictionary<object, Dictionary<PropertyDescriptor, List<BindingLoader>>>();
										}
										if (!string.IsNullOrEmpty(dataSourceName) && !string.IsNullOrEmpty(member) && !string.IsNullOrEmpty(propName))
										{
											Dictionary<PropertyDescriptor, List<BindingLoader>> plist;
											if (!_bindings.TryGetValue(value, out plist))
											{
												plist = new Dictionary<PropertyDescriptor, List<BindingLoader>>();
												_bindings.Add(value, plist);
											}
											List<BindingLoader> list;
											if (!plist.TryGetValue(prop, out list))
											{
												list = new List<BindingLoader>();
												plist.Add(prop, list);
											}
											list.Add(new BindingLoader(member, propName, dataSourceName));
										}
									}
								}
								else
								{
									Dictionary<Type, MethodInfo> mifList = new Dictionary<Type, MethodInfo>();
									MethodInfo[] mifs = prop.PropertyType.GetMethods();
									for (int i = 0; i < mifs.Length; i++)
									{
										if (string.Compare(mifs[i].Name, "Add", StringComparison.Ordinal) == 0)
										{
											ParameterInfo[] pifs = mifs[i].GetParameters();
											if (pifs.Length == 1)
											{
												mifList.Add(pifs[0].ParameterType, mifs[i]);
											}
										}
									}
									object propValue = prop.GetValue(value);
									if (propValue != null && mifList.Count > 0)
									{
										ReadCollection(nodes, propValue, mifList, value);
									}
									else
									{
										ArrayList al = new ArrayList();
										int nCount = ReadCollection(nodes, al, value);
										if (nCount > 0)
										{
											ICollection list = (ICollection)Activator.CreateInstance(prop.PropertyType, al.ToArray());
											prop.SetValue(value, list);
										}
									}
								}
							}
						}
						else
						{
							logTrace("read properties");
							object propValue = prop.GetValue(value);
							ReadProperties(node, propValue);
						}
						break;
					default:
						//normally we should not reach here because we only save for the above two cases
						logTrace("not read for {0}", visibility.Visibility);
						break;
				}
			}
			catch (Exception err)
			{
				addErrStr2("Error reading property {0}.{1}. {2}\r\nXml path:{3}", value, prop.Name, SerializerException.FormExceptionText(err), XmlUtil.GetPath(node));
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="node">the Contents node</param>
		/// <param name="ccs"></param>
		private void ReadCustomValue(XmlNode node, ICustomContentSerialization ccs)
		{
			XmlNodeList nodes = node.SelectNodes(XmlTags.XML_Item);
			if (nodes.Count > 0)
			{
				Dictionary<string, object> vs = new Dictionary<string, object>();
				foreach (XmlNode nd in nodes)
				{
					string key = XmlUtil.GetNameAttribute(nd);
					object v = ReadObject(nd, ccs);
					vs.Add(key, v);
				}
				ccs.CustomContents = vs;
			}
		}
		public string GetName(XmlNode node)
		{
			return XmlUtil.GetNameAttribute(node);
		}
		public byte[] ReadBinary(XmlNode binNode)
		{
			byte[] data = null;
			string binId = XmlUtil.GetAttribute(binNode, XmlTags.XMLATT_ResId);
			if (!string.IsNullOrEmpty(binId))
			{
				XmlDocument docAux = getAuxDocument(binNode);
				if (docAux != null)
				{
					binId = binId.ToLowerInvariant();
					XmlNode resNode = docAux.SelectSingleNode(string.Format(System.Globalization.CultureInfo.InvariantCulture,
						"//{0}/{1}[@{2}='{3}']", XmlTags.XML_BINS, XmlTags.XML_Binary, XmlTags.XMLATT_ResId, binId));
					if (resNode != null)
					{
						data = Convert.FromBase64String(resNode.InnerText);
					}
				}
			}
			if (data == null)
			{
				data = Convert.FromBase64String(binNode.InnerText);
			}
			return data;
		}
		public T ReadValue<T>(XmlNode node, object parentObject)
		{
			object v;
			if (typeof(Type).Equals(typeof(T)))
			{
				XmlNode nodeLib = node.SelectSingleNode(XmlTags.XML_LIBTYPE);
				if (nodeLib != null)
				{
					string acid;
					v = XmlUtil.GetLibTypeAttribute(nodeLib, out acid);
					if (v != null)
					{
						return (T)v;
					}
				}
			}
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
			if (ReadValue(node, converter, null, parentObject, out v))
			{
				return (T)v;
			}
			return default(T);
		}
		public object ReadValue(XmlNode node)
		{
			return ReadValue(node, null);
		}
		public object ReadValue(XmlNode node, object parentObject)
		{
			if (XmlUtil.GetAttributeBoolDefFalse(node, XmlTags.XMLATT_IsNull))
			{
				return null;
			}
			string acid;
			Type t = XmlUtil.GetLibTypeAttribute(node, out acid);
			if (t == null)
			{
				addErrStr2("Cannot load type [{0}] from [{1}]. *", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetAttribute(node, XmlTags.XMLATT_filename));
				return null;
			}
			else
			{
				return ReadValue(node, parentObject, t);
			}
		}
		public object ReadValue(XmlNode node, object parentObject, Type t)
		{
			if (t == null)
			{
				string acid;
				t = XmlUtil.GetLibTypeAttribute(node, out acid);
			}
			if (t == null)
			{
				addErrStr2("Cannot load type [{0}] from [{1}] .2", XmlUtil.GetLibTypeAttributeString(node), XmlUtil.GetAttribute(node, XmlTags.XMLATT_filename));
			}
			else
			{
				object v;
				if (typeof(Type).Equals(t))
				{
					XmlNode nodeLib = node.SelectSingleNode(XmlTags.XML_LIBTYPE);
					if (nodeLib != null)
					{
						string acid;
						v = XmlUtil.GetLibTypeAttribute(nodeLib, out acid);
						if (v != null)
						{
							return v;
						}
					}
				}
				TypeConverter converter = TypeDescriptor.GetConverter(t);
				if (ReadValue(node, converter, null, parentObject, out v))
				{
					return v;
				}
				return VPLUtil.GetDefaultValue(t);
			}
			return null;
		}
		public bool ReadValueFromChildNode<T>(XmlNode node, string nodeName, out T v)
		{
			XmlNode nd = node.SelectSingleNode(nodeName);
			if (nd != null)
			{
				v = ReadValue<T>(nd, null);
				return true;
			}
			v = default(T);
			return false;
		}
		private bool parseString(string txt, TypeConverter converter, PropertyDescriptor prop, object parentObject, out object value)
		{
			IFromString fs = parentObject as IFromString;
			if (fs != null && prop != null && string.CompareOrdinal(prop.Name, "ConstantValue") == 0)
			{
				value = fs.FromString(txt);
				return true;
			}
			if (XmlUtil.GetConversionSupported(converter, typeof(string)))
			{
				GenericConverterContext gcc = null;
				if (prop != null)
				{
					object v = prop.GetValue(parentObject);
					fs = v as IFromString;
					if (fs != null)
					{
						value = fs.FromString(txt);
						return true;
					}
					gcc = new GenericConverterContext(prop, parentObject);
				}
				if (gcc != null)
					value = converter.ConvertFromString(gcc, CultureInfo.InvariantCulture, txt);
				else
					value = converter.ConvertFromInvariantString(txt);
				return true;
			}
			else
			{
				value = converter.ConvertFromInvariantString(txt);
				return true;
			}
		}
		/// Generic function to read an object value.  Returns true if the read
		/// succeeded.
		private bool ReadValue(XmlNode node, TypeConverter converter, PropertyDescriptor prop, object parentObject, out object value)
		{
			try
			{
				foreach (XmlNode child in node.ChildNodes)
				{
					if (child.Name.Equals(XmlTags.XML_LIBTYPE))
					{
						string acid;
						value = XmlUtil.GetLibTypeAttribute(child, out acid);
						if (value == null)
						{
							addErrStr2("Cannot load type [{0}] from [{1}] .3", XmlUtil.GetLibTypeAttributeString(child), XmlUtil.GetAttribute(child, XmlTags.XMLATT_filename));
						}
						return true;
					}
					if (child.Name.Equals(XmlTags.XML_Binary))
					{
						byte[] data = ReadBinary(child);
						if (data == null || data.Length == 0)
						{
							value = null;
							return true;
						}
						// Binary blob.  Now, check to see if the type converter
						// can convert it.  If not, use serialization.
						//
						if (XmlUtil.GetConversionSupported(converter, typeof(byte[])))
						{
							value = converter.ConvertFrom(null, CultureInfo.InvariantCulture, data);
							return true;
						}
						else
						{
							BinaryFormatter formatter = new BinaryFormatter();
							MemoryStream stream = new MemoryStream(data);
							value = formatter.Deserialize(stream);
							return true;
						}
					}
					else if (child.Name.Equals(XmlTags.XML_ObjProperty))
					{
						value = ReadObject(child, parentObject);
						return (value != null);
					}
					else if (child.Name.Equals(XmlTags.XML_InstanceDescriptor))
					{
						value = ReadInstanceDescriptor(child);
						return (value != null);
					}
					else if (child.Name.Equals(XmlTags.XML_Reference))
					{
						string sref = XmlUtil.GetNameAttribute(child);
						if (_references == null)
						{
							_references = new Dictionary<object, Dictionary<PropertyDescriptor, string>>();
						}
						Dictionary<PropertyDescriptor, string> propValue;
						if (!_references.TryGetValue(parentObject, out propValue))
						{
							propValue = new Dictionary<PropertyDescriptor, string>();
							_references.Add(parentObject, propValue);
						}
						propValue.Add(prop, sref);
						value = null;
						return false; //do not set the value
					}
					else if (child.Name.Equals(XmlTags.XML_Data))
					{
						value = createInstance(node, parentObject, null);
						if (value != null)
						{
							push(value);
						}
						try
						{
							ICustomSerialization cs = value as ICustomSerialization;
							if (cs != null)
							{
								cs.OnReadFromXmlNode(this, child);
							}
							else
							{
								IXmlNodeSerializable xs = value as IXmlNodeSerializable;
								xs.OnReadFromXmlNode(this, child);
							}
						}
						catch
						{
							throw;
						}
						finally
						{
							_objectStack.Pop();
						}
						return true;
					}
					else if (child.Name.Equals(XmlTags.XML_Content))
					{
						value = createInstance(node, parentObject, null);
						if (value != null)
						{
							ICustomContentSerialization ccs = value as ICustomContentSerialization;
							if (ccs != null)
							{
								ReadCustomValue(child, ccs);
							}
							return true;
						}
					}
					else if (child.NodeType == XmlNodeType.Text)
					{
						try
						{
							return parseString(node.InnerText, converter, prop, parentObject, out value);
						}
						catch (Exception erv)
						{
							addErrStr(string.Format("Read value from element type {0} failed. {1}. \r\nXmlPath:{2}", child.Name, SerializerException.FormExceptionText(erv), XmlUtil.GetPath(node)));
							value = null;
							return false;
						}
					}
					else if (child.NodeType == XmlNodeType.CDATA)
					{
						XmlCDataSection cd = child as XmlCDataSection;
						try
						{
							return parseString(cd.Value, converter, prop, parentObject, out value);
						}
						catch (Exception erv)
						{
							addErrStr(string.Format("Read value from element type {0} failed. {1}. \r\nXmlPath:{2}", child.Name, SerializerException.FormExceptionText(erv), XmlUtil.GetPath(node)));
							value = null;
							return false;
						}
					}
					else
					{
						addErrStr(string.Format("Unexpected element type {0}", child.Name));
						value = null;
						return false;
					}
				}

				// If we get here, it is because there were no nodes.  No nodes and no inner
				// text is how we signify null.
				//
				value = null;
				return true;
			}
			catch (Exception ex)
			{
				addErrStr(SerializerException.FormExceptionText(ex));
				value = null;
				return false;
			}
		}
		private object ReadInstanceDescriptor(XmlNode node)
		{
			// First, need to deserialize the member
			//
			string memberAttr = XmlUtil.GetAttribute(node, XmlTags.XMLATT_member);
			if (string.IsNullOrEmpty(memberAttr))
			{
				addErrStr("No member attribute on instance descriptor");
				return null;
			}
			else
			{
				byte[] data = Convert.FromBase64String(memberAttr);
				BinaryFormatter formatter = new BinaryFormatter();
				MemoryStream stream = new MemoryStream(data);
				MemberInfo mi = (MemberInfo)formatter.Deserialize(stream);
				object[] args = null;

				// Check to see if this member needs arguments.  If so, gather
				// them from the XML.
				if (mi is MethodBase)
				{
					ParameterInfo[] paramInfos = ((MethodBase)mi).GetParameters();

					args = new object[paramInfos.Length];

					int idx = 0;

					foreach (XmlNode child in node.ChildNodes)
					{
						if (child.Name.Equals(XmlTags.XML_Argument))// "Argument"))
						{
							object value;
							if (!ReadValue(child, TypeDescriptor.GetConverter(paramInfos[idx].ParameterType), null, null, out value))
							{
								return null;
							}

							args[idx++] = value;
						}
					}

					if (idx != paramInfos.Length)
					{
						addErrStr(string.Format("Member {0} requires {1} arguments, not {2}.", mi.Name, args.Length, idx));
						return null;
					}
				}

				InstanceDescriptor id = new InstanceDescriptor(mi, args);
				object instance = id.Invoke();

				// Ok, we have our object.  Now, check to see if there are any properties, and if there are, 
				// set them.
				//
				ReadProperties(node, instance);
				//
				return instance;
			}
		}
		private void ReadList(XmlNodeList nodes, IList list, object parentObject)
		{
			logTraceIncIndent();
			list.Clear();
			foreach (XmlNode nd in nodes)
			{
				object obj;
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					string acid;
					obj = XmlUtil.GetLibTypeAttribute(nd, out acid);
				}
				else
				{
					obj = ReadObject(nd, parentObject);
				}
				if (obj != null)
				{
					list.Add(obj);
				}
			}
			logTraceDecIndent();
		}
		private void ReadControls(XmlNodeList nodes, IList list, object parentObject)
		{
			logTraceIncIndent();
			foreach (XmlNode nd in nodes)
			{
				ReadControl(nd, list, parentObject);
			}
			logTraceDecIndent();
		}
		private void ReadCollection(XmlNodeList nodes, object list, Dictionary<Type, MethodInfo> mifList, object parentObject)
		{
			logTraceIncIndent();
			foreach (XmlNode nd in nodes)
			{
				object obj;
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					string acid;
					obj = XmlUtil.GetLibTypeAttribute(nd, out acid);
				}
				else
				{
					obj = ReadObject(nd, parentObject);
				}
				if (obj != null)
				{
					MethodInfo add;
					if (mifList.TryGetValue(obj.GetType(), out add))
					{
						add.Invoke(list, new object[] { obj });
					}
					else
					{
						addErrStr2("Method {0}.Add({1}) not found", list.GetType(), obj.GetType());
					}
				}
			}
			logTraceDecIndent();
		}
		private int ReadCollection(XmlNodeList nodes, ArrayList list, object parentObject)
		{
			int nCount = 0;
			logTraceIncIndent();
			foreach (XmlNode nd in nodes)
			{
				object obj;
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					string acid;
					obj = XmlUtil.GetLibTypeAttribute(nd, out acid);
				}
				else
				{
					obj = ReadObject(nd, parentObject);
				}
				if (obj != null)
				{
					list.Add(obj);
					nCount++;
				}
			}
			logTraceDecIndent();
			return nCount;
		}
		public void ReadArray(XmlNodeList nodes, Array list, object parentObject)
		{
			logTraceIncIndent();
			for (int i = 0; i < nodes.Count; i++)
			{
				string acid;
				XmlNode nd = nodes[i];
				if (XmlUtil.GetAttributeBoolDefFalse(nd, XmlTags.XMLATT_IsType))
				{
					list.SetValue(XmlUtil.GetLibTypeAttribute(nd, out acid), i);
				}
				else
				{
					if (!typeof(void).Equals(XmlUtil.GetLibTypeAttribute(nd, out acid)))
					{
						object obj = ReadObject(nd, parentObject);
						if (obj != null)
						{
							list.SetValue(obj, i);
						}
					}
				}
			}
			logTraceDecIndent();
		}
		private object createInstance(XmlNode node, object parentObject, Type t)
		{
			if (t == null)
			{
				string acid;
				t = XmlUtil.GetLibTypeAttribute(node, out acid);
				if (t == null)
				{
					addErrStr("Could not create instance. Type is null");
					return null;
				}
			}
			try
			{
				object v = null;
				try
				{
					if (parentObject != null)
					{
						ConstructorInfo cif = t.GetConstructor(new Type[] { parentObject.GetType() });
						if (cif != null)
						{
							v = Activator.CreateInstance(t, parentObject);
						}
					}
					if (v == null)
					{
						if (canCreateInstance(t))
						{
							v = Activator.CreateInstance(t);
						}
						else
						{
							ConstructorInfo[] cifs = t.GetConstructors();
							if (cifs != null && cifs.Length > 0)
							{
								for (int i = 0; i < cifs.Length; i++)
								{
									bool pOK = true;
									ParameterInfo[] pifs = cifs[i].GetParameters();
									object[] ps = new object[pifs.Length];
									for (int k = 0; k < pifs.Length; k++)
									{
										XmlNode nd = node.SelectSingleNode(string.Format(CultureInfo.InvariantCulture, "Property[@name='{0}']", pifs[k].Name));
										if (nd != null)
										{
											ps[k] = ReadValue(nd, null, pifs[k].ParameterType);
										}
										else
										{
											pOK = false;
											break;
										}
									}
									if (pOK)
									{
										v = Activator.CreateInstance(t, ps);
									}
								}
							}
#if DEBUG
							if (v == null)
							{
								addErrStr(string.Format("Could not create instance {0}(). Parameterless constructor not found. \r\nXml path:{1}", t, XmlUtil.GetPath(node)));
							}
#endif
							
						}
					}
				}
				catch (Exception err)
				{
					addErrStr(string.Format("Could not create instance {0}(). {1}. \r\nXml path:{2}", t, SerializerException.FormExceptionText(err), XmlUtil.GetPath(node)));
					v = null;
				}
				if (v != null)
				{
					IXmlNodeHolder xmlHolder = v as IXmlNodeHolder;
					if (xmlHolder != null)
					{
						xmlHolder.DataXmlNode = node;
					}
				}
				return v;
			}
			catch (Exception e)
			{
				addErrStr2("cannot create instance for {0}. {1}. \r\nXml path:{2}", t.FullName, SerializerException.FormExceptionText(e), XmlUtil.GetPath(node));
			}
			return null;
		}
		private bool canCreateInstance(Type t)
		{
			if (t.IsValueType)
				return true;
			if (t.IsSerializable)
				return true;
			if (t.Equals(typeof(string)))
				return true;
			ConstructorInfo cif = t.GetConstructor(Type.EmptyTypes);
			if (cif != null)
			{
				return true;
			}
			return false;
		}
	}
}
