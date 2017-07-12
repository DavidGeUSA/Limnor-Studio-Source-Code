/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Project Serialization in XML
 * License: GNU General Public License v3.0
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using VPL;
using System.Xml;
using VSPrj;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Reflection;

namespace XmlSerializer
{
	/// <summary>
	/// implementation of IObjectFactory
	/// </summary>
	public class ComponentFactory : IObjectFactory
	{
		#region fields and constructors
		IDesignerLoaderHost _loaderHost;
		IContainer _container;
		public ComponentFactory()
		{
		}
		#endregion
		#region Properties
		public IDesignerLoaderHost DesignerLoaderHost
		{
			get
			{
				return _loaderHost;
			}
			set
			{
				_loaderHost = value;
			}
		}
		#endregion
		#region IObjectFactory Members

		public virtual IComponent CreateInstance(Type type, string name)
		{
			IComponent c = null;
			try
			{
				VPLUtil.SetupExternalDllResolve(Path.GetDirectoryName(type.Assembly.Location));
			}
			catch
			{
			}
			try
			{
				if (_loaderHost == null)
				{
					c = (IComponent)Activator.CreateInstance(type);
				}
				else
				{
					c = _loaderHost.CreateComponent(type, name);
				}
				if (c != null)
				{
					VPLUtil.FixPropertyValues(c);
				}
				else
				{
					MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "Cannot create design time instance for type [{0}], name:[{1}]", type.AssemblyQualifiedName, name), "Load design object");
				}
			}
			catch (Exception err)
			{
				MessageBox.Show(string.Format(CultureInfo.InvariantCulture, "Cannot create design time instance for type [{0}], name:[{1}]. Error:{2}", type.AssemblyQualifiedName, name, err.Message), "Load design object");
				throw;
			}
			finally
			{
				VPLUtil.RemoveExternalDllResolve();
			}
			return c;
		}

		public IContainer ComponentContainer
		{
			get
			{
				return _container;
			}
			set
			{
				_container = value;
			}
		}

		#endregion
	}
}
