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
using System.ComponentModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Xml;
using XmlUtility;
using System.IO;
using LFilePath;
using System.Drawing.Design;
using VPL;
using Limnor.WebServerBuilder;
using LimnorDesigner;
using LimnorDatabase;
using VSPrj;
using System.Xml.Serialization;
using System.ComponentModel.Design.Serialization;
using LimnorDesigner.Web;
using MathExp;
using LimnorDesigner.Event;
using System.Data;
#if USECEF
using CefSharp.WinForms;
using CefSharp;
using CefSharp.LimnorStudio;
#endif

namespace Limnor.WebBuilder
{
	public delegate void fnWebPageCall(WebChildPage sender);
	/// <summary>
	/// root object. web page designer, toggle with WebHost
	/// </summary>
	[UseDefaultInstance(WebPage.DEFAULTWEBPAGE)]
	public class WebPage : Form, IWebClientControl, ICustomTypeDescriptor, IDynamicMethodParameters, IValueUIEditorOwner, IWithProject2, IWebClientInitializer, IWebPage, ISourceValueEnumProvider, IDevClassReferencer, ISerializeNotify
	{
		#region fields and constructors
		public const string DEFAULTWEBPAGE = "PageInstance";
		private static StringCollection _propertyNames;
		private List<WebResourceFile> _resourceFiles;
		private XmlNode _scriptNode;
		private LimnorProject _prj;
		private ClassPointer _class;
		private Size _pageSize;
		private bool _useSendKeys;
		private System.ComponentModel.IContainer components = null;
		private IWebHost _htmlEditor; //it is created by a designer of this form
		private bool _htmlChanged;
		private screen _screen;
		private System.Windows.Forms.Timer _timer;
		private bool _closing;
		static WebPage()
		{
			_propertyNames = new StringCollection();
			_propertyNames.Add("Name");
			_propertyNames.Add("Controls");
			_propertyNames.Add("BackgroundImageFile");
			_propertyNames.Add("BackgroundImageTile");
			_propertyNames.Add("ServerError");
			_propertyNames.Add("Title");
			_propertyNames.Add("Description");
			_propertyNames.Add("Keywords");
			_propertyNames.Add("LoginPage");
			_propertyNames.Add("UserLevel");
			_propertyNames.Add("LoginPageId");
			_propertyNames.Add("CurrentUserAlias");
			_propertyNames.Add("CurrentUserLevel");
			_propertyNames.Add("CurrentUserID");
			_propertyNames.Add("UserLoggedOn");
			_propertyNames.Add("StatucBarVisible");
			_propertyNames.Add("SelectedText");
			_propertyNames.Add("ShowAjaxCallWaitingImage");
			_propertyNames.Add("ShowAjaxCallWaitingLabel");
			//
			_propertyNames.Add("dialogResult");
			_propertyNames.Add("CloseDialogPrompt");
			_propertyNames.Add("CancelDialogPrompt");
			_propertyNames.Add("confirmResult");
			_propertyNames.Add("MaximumSize");
			_propertyNames.Add("VisitorScreen");
			//
			_propertyNames.Add("AspxPhysicalFolder");
			_propertyNames.Add("PhpPhysicalFolder");
			_propertyNames.Add("isDialog");
			_propertyNames.Add("IPAddress");
			_propertyNames.Add("IPAddress2");
			_propertyNames.Add("CssStyles");
			//
			_propertyNames.Add("EnableBrowserPageCache");
		}
		private LabelPos _lblSize;
		//
		public WebPage()
		{
			this.components = new System.ComponentModel.Container();
			UserLevel = -1;
			this.BackColor = Color.White;
			this.FormBorderStyle = FormBorderStyle.None;
			this.Size = Screen.PrimaryScreen.Bounds.Size;
			this.BackgroundImageLayout = ImageLayout.Tile;
			PositionAnchor = AnchorStyles.None;
			this.AutoScroll = true;
			this.EnableBrowserPageCache = false;
			_lblSize = new LabelPos();
			_lblSize.Text = string.Empty;
			_lblSize.AutoSize = true;
			this.Controls.Add(_lblSize);
			_lblSize.Visible = true;
			MaximumSize = Size;
			CssStyles = null;

			_timer = new Timer();
			_timer.Interval = 300;
			_timer.Enabled = false;
			_timer.Tick += new EventHandler(_timer_Tick);
		}
		void _timer_Tick(object sender, EventArgs e)
		{
			_timer.Enabled = false;

			if (_closing) return;
			if (_htmlEditor != null && !string.IsNullOrEmpty(_htmlEditor.HtmlUrl))
			{
				if (!_bkImgFileUsed)
				{
					_bkImgFileUsed = true;
					if (!string.IsNullOrEmpty(_bkImgFile))
					{
						if (File.Exists(_bkImgFile))
						{
							showDeprecateMsg();
						}
					}
				}
				if (_htmlEditor.IsSaving)
				{
					_timer.Enabled = true;
					return;
				}
				//this part moved to onPageStarted
				FormWeb fw = new FormWeb();
				fw.Show();
				//load web page and capture web page as image and display the image as a background image of this form
				fw.ShowWeb(_htmlEditor.HtmlUrl, this);
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public string GetBkTile()
		{
			return _bkTile ? "repeat" : "none";
		}
		[Browsable(false)]
		[NotForProgramming]
		public string GetBkUrl()
		{
			if (!string.IsNullOrEmpty(_bkImgFile))
			{
				if (File.Exists(_bkImgFile))
				{
					if (_prj != null)
					{
						try
						{
							string tgt = Path.Combine(_prj.WebPhysicalFolder(this.FindForm()), "images");
							if (!Directory.Exists(tgt))
							{
								Directory.CreateDirectory(tgt);
							}
							tgt = Path.Combine(tgt, Path.GetFileName(_bkImgFile));
							if (!File.Exists(tgt))
							{
								File.Copy(_bkImgFile, tgt);
							}
							string bkUrl = string.Format(CultureInfo.InvariantCulture, "url('images/{0}')", Path.GetFileName(_bkImgFile));
							return bkUrl;
						}
						catch (Exception err)
						{
							MessageBox.Show(this, err.Message, "Adjust Web Display", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
				}
			}
			return null;
		}
		#endregion

		#region Properties
		private void adjustLargePageSize()
		{
			if (_pageSize.Width > Screen.PrimaryScreen.Bounds.Size.Width || _pageSize.Height > Screen.PrimaryScreen.Bounds.Size.Height)
			{
				_lblSize.Location = new Point(Math.Max(Screen.PrimaryScreen.Bounds.Size.Width, _pageSize.Width), Math.Max(Screen.PrimaryScreen.Bounds.Size.Height, _pageSize.Height));
			}
		}
		[DefaultValue(null)]
		[Description("Gets and sets text of cascade styles to be included in this page. If you want to include CSS files then add a JavascriptFiles component and set its CssFiles property.")]
		public string CssStyles { get; set; }
		public bool EditorStarted
		{
			get
			{
				if (_htmlEditor != null)
				{
					return _htmlEditor.EditorStarted;
				}
				return false;
			}
		}
		public override Size MaximumSize
		{
			get
			{
				return _pageSize;
			}
			set
			{
				_pageSize = value;
				if (_pageSize == Size.Empty)
				{
					_pageSize = Screen.PrimaryScreen.Bounds.Size;
				}
				else
				{
					if (_pageSize.Height < Screen.PrimaryScreen.Bounds.Size.Height || _pageSize.Width < Screen.PrimaryScreen.Bounds.Size.Width)
					{
						_pageSize = Screen.PrimaryScreen.Bounds.Size;
					}
				}
				adjustLargePageSize();
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public bool CanBuild
		{
			get
			{
				if (HtmlChanged)
				{
					UpdateHtmlFile();
					if (_htmlEditor != null && (_htmlEditor.IsSaving || _htmlEditor.HtmlChanged))
					{
						return false;
					}
				}
				return true;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public bool HtmlChanged
		{
			get
			{
				if (_htmlChanged)
					return true;
				if (_htmlEditor != null)
				{
					if (_htmlEditor.checkHtmlChange())
					{
						_htmlChanged = true;
					}
				}
				return _htmlChanged;
			}
		}
		[ReadOnly(true)]
		[Browsable(false)]
		public bool HtmlChangedNoCheck
		{
			get
			{
				if (_htmlEditor != null)
				{
					return _htmlEditor.HtmlChanged;
				}
				return false;
			}
		}
		#endregion

		#region Static utility methods
		public static string WebNameToPageFilename(string webName)
		{
			string url = webName;
			if (string.IsNullOrEmpty(url))
			{
				MessageBox.Show("Child page not specified for a child page related action, such as CloseChildPage, HideChildPage, ShowChildWindow and ShowChildDialog");
			}
			else
			{
				if (url.StartsWith("'", StringComparison.Ordinal) && !url.StartsWith("''.", StringComparison.Ordinal))
				{
					url = url.Substring(1);
					if (url.EndsWith("'", StringComparison.Ordinal))
					{
						url = url.Substring(0, url.Length - 1);
					}
					if (!url.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
					{
						url = string.Format(CultureInfo.InvariantCulture, "{0}.html", url);
					}
					url = string.Format(CultureInfo.InvariantCulture, "'{0}'", url);
				}
			}
			return url;
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
			return WebClientValueCollection.GetWebClientProperties(this, _propertyNames, attributes);
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

		#region Web properties
		[DefaultValue(false)]
		[Description("Gets and sets a Boolean indicating whether web browsers may cache this page. A browser uses page cache to load a web page from the local computer the second time and thus avoid network traffic. But the visitor may see old page when a page is modified on the web server, due to page cache.")]
		public bool EnableBrowserPageCache
		{
			get;
			set;
		}
		//
		private SizeType _widthSizeType = SizeType.Absolute;
		[Browsable(false)]
		[Category("Layout")]
		[DefaultValue(SizeType.Absolute)]
		[Description("Gets and sets size type for width. Check out its effects by showing the page in a browser.")]
		public SizeType WidthType
		{
			get
			{
				return _widthSizeType;
			}
			set
			{
				_widthSizeType = value;
			}
		}
		private uint _width = 100;
		[Browsable(false)]
		[Category("Layout")]
		[DefaultValue(100)]
		[Description("Gets and sets the width of this layout as a percentage of parent width. This value is used when WidthType is Percent.")]
		public uint WidthInPercent
		{
			get
			{
				return _width;
			}
			set
			{
				if (value > 0 && value <= 100)
				{
					_width = value;
				}
			}
		}

		private SizeType _heightSizeType = SizeType.Absolute;
		[Browsable(false)]
		[Category("Layout")]
		[DefaultValue(SizeType.Absolute)]
		[Description("Gets and sets size type for height. Check out its effects by showing the page in a browser.")]
		public SizeType HeightType
		{
			get
			{
				return _heightSizeType;
			}
			set
			{
				_heightSizeType = value;
			}
		}
		private uint _height = 100;
		[Browsable(false)]
		[Category("Layout")]
		[DefaultValue(100)]
		[Description("Gets and sets the height of this layout as a percentage of parent height. It is used when HeightType is Percent.")]
		public uint HeightInPercent
		{
			get
			{
				return _height;
			}
			set
			{
				if (value > 0 && value <= 100)
				{
					_height = value;
				}
			}
		}
		//
		[WebClientMember]
		[Description("It contains basic information about the screen of your visitor.")]
		public screen VisitorScreen
		{
			get
			{
				if (_screen == null)
				{
					_screen = new screen();
				}
				return _screen;
			}
		}
		[Description("Gets a Boolean indicating the last result of a WebMessageBox.confirm action.")]
		[WebClientMember]
		public bool confirmResult
		{
			get
			{
				return false;
			}
			set
			{
			}
		}
		private bool _bkImgFileUsed;
		private string _bkImgFile;
		private void showDeprecateMsg()
		{
			MessageBox.Show(this, "Properties BackgroundImageFile and BackgroundImageTile are deprecated. You may set body element's Background properties in Visual HTML Editor. Click <...> to open Visual HTML Editor", "Web Page", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		[Category("Appearance")]
		[Description("Gets and sets background image")]
		public string BackgroundImageFile
		{
			get
			{
				return _bkImgFile;
			}
			set
			{
				if (this.ReadingProperties)
				{
					if (string.IsNullOrEmpty(value))
					{
						_bkImgFile = value;
					}
					else
					{
						if (File.Exists(value))
						{
							try
							{
								_bkImgFile = value;
							}
							catch (Exception err)
							{
								MessageBox.Show(err.Message, "Set background image");
							}
						}
					}
				}
				else
				{
					if (string.IsNullOrEmpty(value))
					{
						_bkImgFile = value;
					}
					else
					{
						showDeprecateMsg();
					}
				}
			}
		}
		private bool _bkTile = true;
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("Gets and sets a Boolean indicating whether the background image display will be repeated.")]
		public bool BackgroundImageTile
		{
			get
			{
				return _bkTile;
			}
			set
			{
				if (this.ReadingProperties)
				{
					_bkTile = value;
					if (_bkTile)
					{
						BackgroundImageLayout = ImageLayout.Tile;
					}
					else
					{
						BackgroundImageLayout = ImageLayout.None;
					}
				}
				else
				{
					showDeprecateMsg();
				}
			}
		}


		private string _title;
		[Category("Head")]
		[DesignerOnly]
		[Description("Page title")]
		public string Title
		{
			get { return _title; }
			set { _title = value; }
		}
		private string _desc;
		[Category("Head")]
		[DesignerOnly]
		[Description("Page description")]
		public string Description
		{
			get { return _desc; }
			set
			{
				_desc = value;
			}
		}
		private string _keyw;
		[Category("Head")]
		[DesignerOnly]
		[Description("Page keywords")]
		public string Keywords
		{
			get { return _keyw; }
			set { _keyw = value; }
		}
		[WebClientMember]
		[Description("Gets a Boolean indicating whether the page is loaded as a dialogue box.")]
		public bool isDialog { get { return false; } }

		[Category("Login Manager")]
		[Description("Gets and sets user level requirement for this page. -1 means not using it. To use it, set it to 0 or a larger positive number. Smaller user level is supposed to have more permissions than larger user level number. It is only used when LoginPage is used and UserAccountLevelFieldName property is set for the Web Login Manager. Suppose this property is set to 1 and a logged on user has a level of 2 then this page will not open.")]
		[DefaultValue(-1)]
		public int UserLevel
		{
			get;
			set;
		}

		[Category("Login Manager")]
		[WebClientMember]
		[Description("Gets the alias of the current logged on user.")]
		public string CurrentUserAlias { get { return string.Empty; } }

		[Category("Login Manager")]
		[WebClientMember]
		[Description("Gets the ID of the current logged on user.")]
		public int CurrentUserID { get { return 0; } }

		[Category("Login Manager")]
		[WebClientMember]
		[Description("Gets the user level of the current logged on user. 0 indicates full permissions. Larger positive number represents less permissions. -1 indicates there is not a user logged on.")]
		public int CurrentUserLevel { get { return 0; } }

		[Category("Login Manager")]
		[WebClientMember]
		[Description("Gets a Boolean indicating whether a user has logged on.")]
		public bool UserLoggedOn { get { return false; } }

		private ComponentID _loginPage;
		[Category("Login Manager")]
		[XmlIgnore]
		[Editor(typeof(TypeSelectorWebPage), typeof(UITypeEditor))]
		[DesignerOnly]
		[Description("Gets and sets a string indicating the login web page. If this property is not empty then this page is restricted to authenticated users only.")]
		public ComponentID LoginPage
		{
			get
			{
				if (_loginPage == null)
				{
					if (_loginId != 0 && Project != null)
					{
						_loginPage = Project.GetComponentByID(_loginId);
					}
				}
				return _loginPage;
			}
			set
			{
				if (value != null)
				{
					if (value.ComponentId != 0)
					{
						if (string.Compare(this.Name, value.ComponentName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							MessageBox.Show(this.FindForm(), "A web page cannot be protected by itself", "Login Page", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
						else
						{
							_loginId = value.ComponentId;
							_loginPage = value;
						}
					}
					else
					{
						_loginId = 0;
						_loginPage = null;
					}
				}
			}
		}

		[Browsable(false)]
		public string LoginPageName
		{
			get
			{
				ComponentID cid = LoginPage;
				if (cid != null)
				{
					return cid.ComponentName;
				}
				return string.Empty;
			}
		}
		[Browsable(false)]
		public string LoginPageFileName
		{
			get
			{
				ComponentID cid = LoginPage;
				if (cid != null)
				{
					return cid.ComponentFile;
				}
				return string.Empty;
			}
		}
		private UInt32 _loginId;
		[DesignerOnly]
		[Browsable(false)]
		public UInt32 LoginPageId
		{
			get { return _loginId; }
			set
			{
				_loginId = value;
				if (_loginId != 0 && Project != null)
				{
					_loginPage = Project.GetComponentByID(_loginId);
				}
			}
		}
		[WebClientMember]
		[Description("Gets the text selected by the web page visitor")]
		public string SelectedText { get { return string.Empty; } }

		[ComponentReferenceSelectorType(typeof(HtmlLabel))]
		[Editor(typeof(ComponentReferenceSelector), typeof(UITypeEditor))]
		[WebClientMember]
		[Description("Gets and sets a label on the web page. When an Ajax call is initiated this label is displayed. When the Ajax call finishes this label is hidden.")]
		public IComponent ShowAjaxCallWaitingLabel
		{
			get;
			set;
		}
		[ComponentReferenceSelectorType(typeof(HtmlImage))]
		[Editor(typeof(ComponentReferenceSelector), typeof(UITypeEditor))]
		[WebClientMember]
		[Description("Gets and sets an image on the web page. When an Ajax call is initiated this image is displayed. When the Ajax call finishes this image is hidden.")]
		public IComponent ShowAjaxCallWaitingImage
		{
			get;
			set;
		}

		[WebClientMember]
		[Description("Gets a string indicating the result of a dialogue box. This page may use ShowChildPage to display a dialogue box. If the dialogue box is canceled then this property is 'cancel'; otherwise this property is 'ok'.")]
		public string dialogResult { get { return "ok"; } }

		[DefaultValue("")]
		[WebClientMember]
		[Description("Gets and sets a prompt asking whether the user wants to close this window. This prompt is displayed when this page is being closed. This prompt is used only if this page is opened as a dialogue box, that is, it is opened via a ShowChildDialog action of another page, the parant page. The parent page's dialogResult property will be 'ok' if the user confirms the prompt.")]
		public string CloseDialogPrompt { get; set; }

		[DefaultValue("")]
		[WebClientMember]
		[Description("Gets and sets a prompt asking whether the user wants to cancel a dialogue box or close this window. This prompt is displayed when this page is being closed. This prompt is used only if this page is opened as a child page, that is, it is opened via a ShowChildWindow or ShowChildDialog action of another page, the parent page. The parent page's dialogResult property will be 'cancel' if the user confirms the prompt.")]
		public string CancelDialogPrompt { get; set; }

		[WebClientMember]
		public EnumWebCursor cursor { get; set; }

		[WebClientOnly]
		[WebClientMember]
		[Description("Gets a string showing error message from the last server operation, for example, an Update action of an EasyDataSet.")]
		public string ServerError { get { return string.Empty; } }
		#endregion

		#region Web methods
		[Description("Switch web client event handler at runtime")]
		[WebClientMember]
		public void SwitchEventHandler(string eventName, VplMethodPointer handler)
		{
		}

		[Description("Change element style at runtime")]
		[WebClientMember]
		public void setStyle(string styleName, string styleValue) { }
		private string _vaname;
		[NotForProgramming]
		[Browsable(false)]
		public void SetCodeName(string vname)
		{
			_vaname = vname;
		}
		[Description("Create a new web page and show it in the current web browser window")]
		[WebClientMember]
		public static void CreateWebPage()
		{
		}
		[Description("Create a new web page and show it in a new web browser window")]
		[WebClientMember]
		public static void CreateWebPageInNewWindow()
		{

		}
		[Description("Create a new web page and show it in a popup web browser window")]
		[WebClientMember]
		public static void CreateWebPageInPopupWindow(bool status, bool toolbar, bool locationBar, bool menubar, bool directoriesButtons, bool resizable, bool scrollbars, int height, int width)
		{
		}
		[Description("It is used to reload the current document.")]
		[WebClientMember]
		public void reload()
		{
		}
		[Description("Display the previous web page in the current web browser window")]
		[WebClientMember]
		public void GoBack()
		{
		}
		[Description("Display the next web page in the current web browser window")]
		[WebClientMember]
		public void GoForward()
		{
		}
		[Description("Display the specified web page in the current web browser window. Use {page url}?{name}={value}&{name}={value}&... to pass parameters to the page")]
		[WebClientMember]
		public void GotoWebPage(string pageAddress)
		{
		}
		[Description("Send characters specified by parameter keys to the text box or text area currently having input focus.")]
		[WebClientMember]
		public void sendKeys(string keys)
		{
		}
		[Description("Make the next text box or text area to have input focus, in the order of TabIndex")]
		[WebClientMember]
		public void selectNextInput()
		{
		}
		private void addOpenSpecsInt(StringCollection code, StringCollection specs, string name, string val, bool allowZero)
		{
			if (!string.IsNullOrEmpty(val))
			{
				int n = 0;
				try
				{
					if (int.TryParse(val, out n))
					{
						if (!allowZero)
						{
							if (n == 0)
							{
								return;
							}
						}
						specs.Add(string.Format(CultureInfo.InvariantCulture, "'{0}={1}'", name, n));
					}
					else
					{
						string v = string.Format(CultureInfo.InvariantCulture, "v{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						code.Add(string.Format(CultureInfo.InvariantCulture,
							"var {0}={1};\r\n", v, val));
						specs.Add(string.Format(CultureInfo.InvariantCulture, "'{0}=' + {1}", name, v));
					}
				}
				catch
				{
				}
			}
		}
		private void addOpenSpecs(StringCollection code, StringCollection specs, string name, string val)
		{
			if (!string.IsNullOrEmpty(val) && string.CompareOrdinal(val, "''") != 0)
			{
				if (string.CompareOrdinal(val, "false") == 0)
				{
					specs.Add(string.Format(CultureInfo.InvariantCulture, "'{0}=0'", name));
				}
				else if (string.CompareOrdinal(val, "true") == 0)
				{
					specs.Add(string.Format(CultureInfo.InvariantCulture, "'{0}=1'", name));
				}
				else
				{
					string v = string.Format(CultureInfo.InvariantCulture, "v{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"var {0}={1};\r\n", v, val));
					specs.Add(string.Format(CultureInfo.InvariantCulture, "'{0}=' + {1}", name, v));
				}
			}
		}

		/// <summary>
		/// not used anymore
		/// </summary>
		/// <param name="pageAddress"></param>
		/// <param name="windowName"></param>
		[NotForProgramming]
		[Browsable(false)]
		public void GotoWebPageInNewWindow(string pageAddress, string windowName)
		{
		}
		/// <summary>
		/// obsolete
		/// </summary>
		/// <param name="pageAddress"></param>
		/// <param name="windowName"></param>
		/// <param name="showStatusBar"></param>
		/// <param name="showToolbar"></param>
		/// <param name="showAddressBar"></param>
		/// <param name="showMenubar"></param>
		/// <param name="showScrollbar"></param>
		/// <param name="showTitlebar"></param>
		/// <param name="resizable"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="replaceHistoryEntry"></param>
		/// <param name="singleInstanceOnly"></param>
		[NotForProgramming]
		[Browsable(false)]
		[WebClientMember]
		public void ShowPopupWindow(string pageAddress, string windowName,
			bool showStatusBar //2
			, bool showToolbar //3
			, bool showAddressBar //4
			, bool showMenubar //5
			, bool showScrollbar //6
			, bool showTitlebar //7
			, bool resizable //8
			, int left, int top, int width, int height
			, bool replaceHistoryEntry //13
			, bool singleInstanceOnly //14
			)
		{
		}
		[Description("Display the specified web page in another web browser window. Use {page url}?{name}={value}&{name}={value}&... to pass parameters to the page")]
		[WebClientMember]
		public void ShowPopupWindow(string pageAddress, string windowName,
			bool showStatusBar //2
			, bool showToolbar //3
			, bool showAddressBar //4
			, bool showMenubar //5
			, bool showScrollbar //6
			, bool showTitlebar //7
			, bool resizable //8
			, int left, int top, int width, int height
			, bool replaceHistoryEntry //13
			)
		{
		}
		[Description("Closes the current window.")]
		[WebClientMember]
		public void close()
		{
		}
		[Description("Log off")]
		[WebClientMember]
		public void LogOff()
		{
		}
		[Description("Set status bar message")]
		[WebClientMember]
		public void SetStatus(string message)
		{
		}
		[Description("Print this page")]
		[WebClientMember]
		public void Print()
		{
		}
		[Description("Display a message in a popup window if ClientDebugLevel property of the Web Application is greater than 0. If ClientDebugLevel is 0 or less than 0 then this method does nothing.")]
		[WebClientMember]
		public void ShowDebugMessage(string message)
		{
		}
		[StandaloneOnlyAction]
		[Description("Display a web page as a dialogue box and enable communications between this page and the web page shown in the dialogue box. When the dialogue box is closed, the property dialogResult will be 'ok' or 'cancel', depending on how the user closes the dialogue box. The access of the current web page by the users is blocked until the dialogue box is closed. The actions following the action executing this method are also suspended until the dialogue box is closed.")]
		[WebClientMember]
		public void ShowChildDialog(
			ComponentID webpage, //0
			bool center, //1
			int top, //2
			int left, //3
			int width, //4
			int height, //5
			bool resizable, //6
			EnumBorderStyle borderStyle, //7
			EnumBorderWidthStyle borderWidthStyle, //8
			int borderWidth, //9
			Color borderColor, //10
			string iconPath, //11
			string title, //12
			EnumHideDialogButtons hideCloseButtons, //13
			string pageParameters, //14
			string closeIconPath, //15
			string okIconPath, //16
			EnumOverflow overflow, //17
			bool hideTitle, //18
			Color titleBackColor, //19
			Color titleColor //20
			)
		{
		}
		[Description("Display a web page as a child window and enable communications between this page and the child web page. It does not block the access to this page. It does not block the execution of the other actions. ")]
		[WebClientMember]
		public void ShowChildWindow(
			ComponentID webpage, //0
			bool center, //1
			int top, //2
			int left, //3
			int width, //4
			int height, //5
			bool resizable, //6
			EnumBorderStyle borderStyle, //7
			EnumBorderWidthStyle borderWidthStyle, //8
			int borderWidth, //9
			Color borderColor, //10
			string iconPath, //11
			string title, //12
			EnumHideDialogButtons hideCloseButtons, //13
			string pageParameters, //14
			string closeIconPath, //15
			string okIconPath, //16
			EnumOverflow overflow, //17
			bool hideTitle, //18
			Color titleBackColor, //19
			Color titleColor //20
			)
		{
		}
		[Description("It hides the specified child page which was opened via a ShowChildWindow action. The child page is kept in the memory. A subsequent ShowChildWindow action will make the child page visible again.")]
		[WebClientMember]
		public void HideChildPage(ComponentID webpage)
		{
		}
		[Description("It closes the specified child page which was opened via a ShowChildWindow action, removing the child page from the memory. A subsequent ShowChildWindow action will create a new child page.")]
		[WebClientMember]
		public void CloseChildPage(ComponentID webpage)
		{
		}
		[Description("It hides the current page which was opened via a ShowChildWindow or a ShowChildDialog action by another page (parent page). This page is kept in the memory. A subsequent ShowChildWindow action by its parent page will make this page visible again.")]
		[WebClientMember]
		public void HidePage()
		{
		}
		[Description("It closes the current page which was opened via a ShowChildWindow or a ShowChildDialog action by another page (parent page), removing this page from the memory. A subsequent ShowChildWindow action by the parent page will create a new page. The dialogResult is set to 'cancel'.")]
		[WebClientMember]
		public void ClosePage()
		{
		}
		[Description("It closes the current page which was opened via a ShowChildDialog action by another page (parent page), removing this page from the memory. A subsequent ShowChildWindow action by the parent page will create a new page. The dialogResult is set to 'ok'.")]
		[WebClientMember]
		public void ConfirmDialog()
		{
		}
		[Description("It moves the browser window to the specified location")]
		[WebClientMember]
		public void moveTo(int x, int y)
		{
		}
		[Description("It resizes the browser window to the specified size")]
		[WebClientMember]
		public void resizeTo(int width, int height)
		{
		}
		[Description("It maximize the browser window to available screen size")]
		[WebClientMember]
		public void maximizeToAvailableScreen()
		{
		}
		[Description("If this page is used for the src of an IFRAME then this method returns a property of the parent window; the name of the property is specified by valueName parameter. This method is only valid after the event onparentload has occurred.")]
		[WebClientMember]
		public object getParentVale(string valueName)
		{
			return null;
		}
		[Description("If this page is used for the src of an IFRAME then this method sets a property of the parent window; the name of the property is specified by valueName parameter. This method is only valid after the event onparentload has occurred.")]
		[WebClientMember]
		public void setParentVale(string valueName, object value)
		{
		}
		[Description("If this page is used for the src of an IFRAME then this method returns the URL of the parent window, including the page parameters. This method is only valid after the event onparentload has occurred.")]
		[WebClientMember]
		public JsString getParentUrl()
		{
			return new JsString();
		}
		[Description("Gets a string value specified by valueName which is a resource name. The value returned is for the current culture.")]
		[WebClientMember]
		public JsString GetValueInCurrentCulture(string valueName)
		{
			return new JsString();
		}
		#endregion

		#region Web events
		[Description("Occurs when Anchor or Alignment adjustment.")]
		[WebClientMember]
		public event SimpleCall onAdjustAnchorAlign { add { } remove { } }

		[Description("Occurs when an Ajax call takes longer than a time specified by AjaxTimeout property of WebApp object.")]
		[WebClientMember]
		public event SimpleCall onAjaxTimeout { add { } remove { } }

		[Description("Occurs when a PHP fatal error happens on web server. The error message is stored in property ServerError.")]
		[WebClientMember]
		public event SimpleCall onPhpFatalError { add { } remove { } }

		[Description("Occurs when the web page is loaded but not initialized. All programming entities may not be valid. If this event is handled by returning a False value then the page will stop initializing. Parameter pageUrl is the URL for loading this page. Parameter parentUrl is the URL of the parent page if this page is loaded via an IFrame.")]
		[WebClientMember]
		public event fnInitPage onBeforeInitPage { add { } remove { } }

		[Description("Occurs when the web page finishes loading")]
		[WebClientMember]
		public event SimpleCall onload { add { } remove { } }

		[Description("Occurs when the parent window finishes loading if this page is used for the src of an IFRAME. onload event occurs before this event.")]
		[WebClientMember]
		public event SimpleCall onparentload { add { } remove { } }

		[Description("Occurs when a child web page finishes loading. The child web page is loaded by a ShowChildWindow or a ShowChildDialog action.")]
		[WebClientMember]
		public event fnWebPageCall onChildWindowReady { add { } remove { } }

		[Description("Occurs when the window is going to be closed. It occurs only if the page is opened via ShowChildPage by another page.")]
		[WebClientMember]
		public event SimpleCall onClosingWindow { add { } remove { } }

		[Description("Occurs when the user navigates away from the page (by clicking on a link, submitting a form, closing the browser window, etc.)")]
		[WebClientMember]
		public event SimpleCall onunload { add { } remove { } }

		[Description("Occurs prior to a document being unloaded")]
		[WebClientMember]
		public event SimpleStringCall onbeforeunload { add { } remove { } }

		[WebClientEventByServerObject]
		[Description("Occurs on returning from a web server access")]
		[WebClientMember]
		public event SimpleCall onwebserverreturn { add { } remove { } }

		[WebClientMember]
		public event WebControlMouseEventHandler onclick { add { } remove { } }
		[WebClientMember]
		public event WebControlMouseEventHandler ondblclick { add { } remove { } }
		[WebClientMember]
		public event WebControlMouseEventHandler onmousedown { add { } remove { } }
		[WebClientMember]
		public event WebControlMouseEventHandler onmousemove { add { } remove { } }
		[WebClientMember]
		public event WebControlMouseEventHandler onmouseover { add { } remove { } }
		[WebClientMember]
		public event WebControlMouseEventHandler onmouseout { add { } remove { } }
		[WebClientMember]
		public event WebControlMouseEventHandler onmouseup { add { } remove { } }
		[WebClientMember]
		public event WebControlSimpleEventHandler onkeydown { add { } remove { } }
		[WebClientMember]
		public event WebControlSimpleEventHandler onkeypress { add { } remove { } }
		[WebClientMember]
		public event WebControlSimpleEventHandler onkeyup { add { } remove { } }
		#endregion

		#region IWebClientControl Members
		[DefaultValue(EnumTextAlign.left)]
		[WebClientMember]
		public EnumTextAlign textAlign { get; set; }

		[Browsable(false)]
		[NotForProgramming]
		[DefaultValue(0)]
		[WebClientMember]
		public int zOrder { get; set; }

		[Category("Layout")]
		[DefaultValue(AnchorStyles.None)]
		[Description("Gets and sets anchor style. PositionAlignment is ignored if PositionAnchor involves right and bottom.")]
		public AnchorStyles PositionAnchor
		{
			get;
			set;
		}
		[Category("Layout")]
		[DefaultValue(ContentAlignment.TopLeft)]
		[Description("Gets and sets position alignment. PositionAlignment is ignored if PositionAnchor involves right and bottom.")]
		public ContentAlignment PositionAlignment
		{
			get;
			set;
		}
		private XmlNode _dataNode;
		[Browsable(false)]
		public XmlNode DataXmlNode { get { return _dataNode; } set { _dataNode = value; } }
		[Browsable(false)]
		public XmlNode SctiptNode { get { return _scriptNode; } }

		private int _opacity = 100;
		[DefaultValue(100)]
		[Description("Gets and sets the opacity of the control. 0 is transparent. 100 is full opacity")]
		public new int Opacity
		{
			get
			{
				if (_opacity < 0 || _opacity > 100)
				{
					_opacity = 100;
				}
				return _opacity;
			}
			set
			{
				if (value >= 0 && value <= 100)
				{
					_opacity = value;
				}
			}
		}

		[Browsable(false)]
		public string CodeName
		{
			get
			{
				string s = Name;
				if (!string.IsNullOrEmpty(s))
				{
					int n = s.IndexOf('.');
					if (n > 0)
					{
						return s.Substring(0, n);
					}
				}
				return s;
			}
		}
		[Browsable(false)]
		public bool WebContentLoaded
		{
			get
			{
				return true;
			}
		}

		[Browsable(false)]
		public Dictionary<string, string> HtmlParts
		{
			get { return null; }
		}

		[Browsable(false)]
		public string ElementName { get { return "body"; } }

		[Browsable(false)]
		public string MapJavaScriptCodeName(string name)
		{
			if (string.CompareOrdinal(name, "confirmResult") == 0)
			{
				return "JsonDataBinding.confirmResult";
			}
			if (string.CompareOrdinal(name, "dialogResult") == 0)
			{
				return "document.dialogResult";
			}
			if (string.CompareOrdinal(name, "CloseDialogPrompt") == 0)
			{
				return "document.childManager.CloseDialogPrompt";
			}
			if (string.CompareOrdinal(name, "CancelDialogPrompt") == 0)
			{
				return "document.childManager.CancelDialogPrompt";
			}
			if (string.CompareOrdinal(name, "VisitorScreen") == 0)
			{
				return "window.screen";
			}
			string s = WebPageCompilerUtility.MapJavaScriptCodeName(name);
			if (s != null)
			{
				return s;
			}
			return name;
		}

		[Browsable(false)]
		public IList<WebResourceFile> GetResourceFiles()
		{
			return _resourceFiles;
		}
		[Browsable(false)]
		public void CreateHtmlContent(XmlNode node, EnumWebElementPositionType positionType, int groupId)
		{
			bool b;
			string[] jsFiles = new string[5];
			jsFiles[0] = "ok.png";
			jsFiles[1] = "cancel.png";
			jsFiles[2] = "dlg.png";
			jsFiles[3] = "resizer.gif";
			jsFiles[4] = "downArrow.png";
			_resourceFiles = new List<WebResourceFile>();
			for (int i = 0; i < jsFiles.Length; i++)
			{
				string btimg = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), jsFiles[i]);
				if (File.Exists(btimg))
				{
					_resourceFiles.Add(new WebResourceFile(btimg, WebResourceFile.WEBFOLDER_Javascript, out b));
				}
			}
			XmlDocument doc = node.OwnerDocument;
			XmlNode xHead = doc.CreateElement(XhtmlTags.XH_head);
			node.AppendChild(xHead);
			//
			XmlNode data;
			if (!string.IsNullOrEmpty(Title))
			{
				data = doc.CreateElement(XhtmlTags.XH_title);
				data.InnerText = Title;
				xHead.AppendChild(data);
			}
			//
			data = doc.CreateElement(XhtmlTags.XH_meta);
			xHead.AppendChild(data);
			XmlUtil.SetAttribute(data, "name", "generator");
			XmlUtil.SetAttribute(data, "content", "Limnor Studio (http://www.limnor.com)");
			//
			data = doc.CreateElement(XhtmlTags.XH_meta);
			xHead.AppendChild(data);
			XmlUtil.SetAttribute(data, "http-equiv", "Content-Type");
			XmlUtil.SetAttribute(data, "content", "text/html; charset=UTF-8");
			//
			data = doc.CreateElement(XhtmlTags.XH_meta);
			xHead.AppendChild(data);
			XmlUtil.SetAttribute(data, "http-equiv", "Content-Script-Type");
			XmlUtil.SetAttribute(data, "content", "text/javascript");
			//
			if (!string.IsNullOrEmpty(Description))
			{
				data = doc.CreateElement(XhtmlTags.XH_meta);
				xHead.AppendChild(data);
				XmlUtil.SetAttribute(data, "name", "description");
				XmlUtil.SetAttribute(data, "content", Description);
			}
			if (!string.IsNullOrEmpty(Keywords))
			{
				data = doc.CreateElement(XhtmlTags.XH_meta);
				xHead.AppendChild(data);
				XmlUtil.SetAttribute(data, "name", "keywords");
				XmlUtil.SetAttribute(data, "content", Keywords);
			}
			//
			StringBuilder sb;
			//===body style========================================
			data = doc.CreateElement(XhtmlTags.XH_style);
			XmlUtil.SetAttribute(data, "type", "text/css");
			//
			//
			bool bodyStyle = false;
			sb = new StringBuilder(XhtmlTags.XH_body);
			sb.Append(" {");
			bool b0;
			WebResourceFile wf;
			if (!string.IsNullOrEmpty(_bkImgFile))
			{
				if (File.Exists(_bkImgFile))
				{
					wf = new WebResourceFile(_bkImgFile, WebResourceFile.WEBFOLDER_Images, out b0);
					_resourceFiles.Add(wf);
					if (b0)
					{
						_bkImgFile = wf.ResourceFile;
					}
					sb.Append("background-image:url(");
					sb.Append(WebResourceFile.WEBFOLDER_Images);
					sb.Append("/");
					sb.Append(Path.GetFileName(_bkImgFile));
					sb.Append(");");
					if (!BackgroundImageTile)
					{
						sb.Append("background-repeat: no-repeat;");
					}
					bodyStyle = true;
				}
			}
			sb.Append("}");
			if (!bodyStyle)
			{
				sb = new StringBuilder();
			}
			if (!string.IsNullOrEmpty(this.CssStyles))
			{
				if (bodyStyle)
				{
					sb.Append("\r\n");
				}
				sb.Append(this.CssStyles);
				bodyStyle = true;
			}
			if (bodyStyle)
			{
				data.InnerText = sb.ToString();
				xHead.AppendChild(data);
			}
			//===end of body style
			//
			XmlNode xBody = doc.CreateElement(XhtmlTags.XH_body);
			node.AppendChild(xBody);
			//
			sb = new StringBuilder();
			WebPageCompilerUtility.CreateWebElementCursor(cursor, sb, false);
			if (textAlign != EnumTextAlign.left)
			{
				sb.Append(string.Format(CultureInfo.InvariantCulture, "text-align:{0};", textAlign));
			}
			if (sb.Length > 0)
			{
				XmlUtil.SetAttribute(xBody, "style", sb.ToString());
			}
			//
			//<SCRIPT LANGUAGE="JavaScript" SRC="libjs/json2.js"></SCRIPT>
			XmlNode limnorNode = doc.CreateElement(XhtmlTags.XH_script);
			xHead.AppendChild(limnorNode);
			XmlUtil.SetAttribute(limnorNode, XmlTags.XMLATT_type, "text/javascript");
			XmlUtil.SetAttribute(limnorNode, "src", "libjs/json2.js"); //for release json.js includes jsonDataBind.js
			((XmlElement)limnorNode).IsEmpty = false;
			//
#if DEBUG
			//<SCRIPT LANGUAGE="JavaScript" SRC="libjs/jsonDataBind.js"></SCRIPT>
			limnorNode = doc.CreateElement(XhtmlTags.XH_script);
			xHead.AppendChild(limnorNode);
			XmlUtil.SetAttribute(limnorNode, XmlTags.XMLATT_type, "text/javascript");
			XmlUtil.SetAttribute(limnorNode, "src", "libjs/jsonDataBind.js");
			((XmlElement)limnorNode).IsEmpty = false;
#endif
			//
			limnorNode = doc.CreateElement(XhtmlTags.XH_script);
			xHead.AppendChild(limnorNode);
			XmlUtil.SetAttribute(limnorNode, XmlTags.XMLATT_type, "text/javascript");
			XmlUtil.SetAttribute(limnorNode, "src", "libjs/modal.js");
			((XmlElement)limnorNode).IsEmpty = false;
			//
			limnorNode = doc.CreateElement(XhtmlTags.XH_script);
			xHead.AppendChild(limnorNode);
			XmlUtil.SetAttribute(limnorNode, XmlTags.XMLATT_type, "text/javascript");
			XmlUtil.SetNameAttribute(limnorNode, "webapp");
			((XmlElement)limnorNode).IsEmpty = false;
			//
			StringCollection files = new StringCollection();
			foreach (IComponent ic in this.Container.Components)
			{
				IUseJavascriptFiles uj = ic as IUseJavascriptFiles;
				if (uj != null)
				{
					IList<string> l = uj.GetJavascriptFiles();
					if (l != null)
					{
						foreach (string f in l)
						{
							bool found = false;
							foreach (string f0 in files)
							{
								if (string.Compare(f0, f, StringComparison.OrdinalIgnoreCase) == 0)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								files.Add(f);
							}
						}
					}
				}
			}
			foreach (object obj in _class.UsedHtmlElements)
			{
				IUseJavascriptFiles uj = obj as IUseJavascriptFiles;
				if (uj != null)
				{
					IList<string> l = uj.GetJavascriptFiles();
					if (l != null)
					{
						foreach (string f in l)
						{
							bool found = false;
							foreach (string f0 in files)
							{
								if (string.Compare(f0, f, StringComparison.OrdinalIgnoreCase) == 0)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								files.Add(f);
							}
						}
					}
				}
			}
			foreach (string f in files)
			{
				XmlNode jsNode = doc.CreateElement(XhtmlTags.XH_script);
				xHead.AppendChild(jsNode);
				XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/javascript");
				XmlUtil.SetAttribute(jsNode, "src", string.Format(CultureInfo.InvariantCulture, "libjs/{0}", f));
				((XmlElement)jsNode).IsEmpty = false;
			}
			files = new StringCollection();
			foreach (IComponent ic in this.Container.Components)
			{
				IUseCssFiles uj = ic as IUseCssFiles;
				if (uj != null)
				{
					IList<string> l = uj.GetCssFiles();
					if (l != null)
					{
						foreach (string f in l)
						{
							bool found = false;
							foreach (string f0 in files)
							{
								if (string.Compare(f0, f, StringComparison.OrdinalIgnoreCase) == 0)
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								files.Add(f);
							}
						}
					}
				}
			}
			foreach (string f in files)
			{
				XmlNode jsNode = doc.CreateElement(XhtmlTags.XH_link);
				xHead.AppendChild(jsNode);
				XmlUtil.SetAttribute(jsNode, XmlTags.XMLATT_type, "text/css");
				XmlUtil.SetAttribute(jsNode, "rel", "stylesheet");
				XmlUtil.SetAttribute(jsNode, "href", string.Format(CultureInfo.InvariantCulture, "css/{0}", f));
			}
			//
			_scriptNode = doc.CreateElement(XhtmlTags.XH_script);
			xHead.AppendChild(_scriptNode);
			//
			XmlUtil.SetAttribute(_scriptNode, XmlTags.XMLATT_type, "text/javascript");
		}
		[Browsable(false)]
		public PropertyDescriptorCollection GetWebClientProperties(bool isStatic)
		{
			if (isStatic)
			{
				return new PropertyDescriptorCollection(new PropertyDescriptor[] { });
			}
			else
			{
				List<PropertyDescriptor> lst = new List<PropertyDescriptor>();
				PropertyDescriptorCollection ps = GetProperties(new Attribute[] { });
				foreach (PropertyDescriptor p in ps)
				{
					if (p.Attributes != null)
					{
						bool bDesignOnly = false;
						foreach (Attribute a in p.Attributes)
						{
							DesignerOnlyAttribute da = a as DesignerOnlyAttribute;
							if (da != null)
							{
								bDesignOnly = true;
								break;
							}
						}
						if (bDesignOnly)
						{
							continue;
						}
					}
					lst.Add(p);
				}
				return new PropertyDescriptorCollection(lst.ToArray());
			}
		}
		[Browsable(false)]
		public MethodInfo[] GetWebClientMethods(bool isStatic)
		{
			List<MethodInfo> lst = new List<MethodInfo>();
			BindingFlags flags;
			if (isStatic)
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static;
			}
			else
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
			}
			MethodInfo[] ret = this.GetType().GetMethods(flags);
			if (ret != null && ret.Length > 0)
			{
				for (int i = 0; i < ret.Length; i++)
				{
					if (!ret[i].IsSpecialName)
					{
						object[] objs = ret[i].GetCustomAttributes(typeof(WebClientMemberAttribute), true);
						if (objs != null && objs.Length > 0)
						{
							lst.Add(ret[i]);
						}
					}
				}
			}
			ret = lst.ToArray();
			return ret;
		}
		[Browsable(false)]
		public EventInfo[] GetWebClientEvents(bool isStatic)
		{
			List<EventInfo> lst = new List<EventInfo>();
			BindingFlags flags;
			if (isStatic)
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static;
			}
			else
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
			}
			EventInfo[] ret = this.GetType().GetEvents(flags);
			if (ret != null && ret.Length > 0)
			{
				for (int i = 0; i < ret.Length; i++)
				{
					if (!ret[i].IsSpecialName)
					{
						object[] objs = ret[i].GetCustomAttributes(typeof(WebClientMemberAttribute), true);
						if (objs != null && objs.Length > 0)
						{
							lst.Add(ret[i]);
						}
					}
				}
			}
			ret = lst.ToArray();
			return ret;
		}
		private string _lastChildCode;
		[Browsable(false)]
		[NotForProgramming]
		public string GetLastChildWindowCode()
		{
			return _lastChildCode;
		}
		private void processFileParam(StringCollection code, string v, string name, string val)
		{
			if (!string.IsNullOrEmpty(val))
			{
				string img = val;
				bool isConst = false;
				if (img.StartsWith("'", StringComparison.Ordinal))
				{
					isConst = true;
					img = img.Substring(1);
					if (img.EndsWith("'", StringComparison.Ordinal))
					{
						img = img.Substring(0, img.Length - 1);
					}
				}
				if (isConst)
				{
					if (File.Exists(img))
					{
						bool b0;
						code.Add(Indentation.GetIndent());
						_resourceFiles.Add(new WebResourceFile(img, WebResourceFile.WEBFOLDER_Images, out b0));
						code.Add(string.Format(CultureInfo.InvariantCulture,
								  "{0}.{1}='{2}/{3}';\r\n", v, name, WebResourceFile.WEBFOLDER_Images, Path.GetFileName(img)));
					}
				}
				else
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.{1}={2};\r\n", v, name, val));
				}
			}
		}
		[NotForProgramming]
		[Browsable(false)]
		public string CreateDialogParameters(StringCollection code, StringCollection parameters, bool isDialog)
		{
			string v = string.Format(CultureInfo.InvariantCulture, "v{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
			_lastChildCode = v;
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
				"var {0} = {{}}; {0}.isDialog = {1};\r\n", v, isDialog ? "true" : "false"));
			string url = WebNameToPageFilename(parameters[0]);
			code.Add(Indentation.GetIndent());
			if (string.IsNullOrEmpty(parameters[14]) || string.CompareOrdinal("''", parameters[14]) == 0 || string.CompareOrdinal("\"\"", parameters[14]) == 0)
			{
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.url={1};\r\n", v, url));
			}
			else
			{
				if (url.StartsWith("'", StringComparison.Ordinal) && !url.StartsWith("''.", StringComparison.Ordinal))
				{
					url = url.Substring(1);
					if (url.EndsWith("'", StringComparison.Ordinal))
					{
						url = url.Substring(0, url.Length - 1);
					}
					url = string.Format(CultureInfo.InvariantCulture,
					"'{0}?'+({1})", url, parameters[14]);
				}
				else
				{
					url = string.Format(CultureInfo.InvariantCulture,
					"{0}+'?'+({1})", url, parameters[14]);
				}
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.url={1};\r\n", v, url));
			}
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
								"{0}.center={1};\r\n", v, parameters[1]));
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
								"{0}.top={1};\r\n", v, parameters[2]));
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
				"{0}.left={1};\r\n", v, parameters[3]));
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
				"{0}.width={1};\r\n", v, parameters[4]));
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
				"{0}.height={1};\r\n", v, parameters[5]));
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
				"{0}.resizable={1};\r\n", v, parameters[6]));
			if (!string.IsNullOrEmpty(parameters[7]))
			{
				EnumBorderStyle borderStyle = (EnumBorderStyle)Enum.Parse(typeof(EnumBorderStyle), WebPageCompilerUtility.StripSingleQuotes(parameters[7]));
				if (borderStyle == EnumBorderStyle.none || string.CompareOrdinal(parameters[9], "0") == 0)
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.border='0px';\r\n", v));
				}
				else
				{
					StringBuilder sbb = new StringBuilder();
					sbb.Append(parameters[9]);
					sbb.Append("px ");
					if (!string.IsNullOrEmpty(parameters[7]))
					{
						sbb.Append(parameters[7].Replace("'", ""));
					}
					if (string.Compare(parameters[10], "null", StringComparison.OrdinalIgnoreCase) != 0)
					{
						string sc = parameters[10];
						if (sc.StartsWith("'", StringComparison.Ordinal))
						{
							sc = sc.Substring(1);
							if (sc.EndsWith("'", StringComparison.Ordinal))
							{
								sc = sc.Substring(0, sc.Length - 1);
							}
						}
						sbb.Append(" ");
						sbb.Append(sc);
					}
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.border='{1}';\r\n", v, sbb.ToString()));
					if (!string.IsNullOrEmpty(parameters[8]))
					{
						EnumBorderWidthStyle bwStyle = (EnumBorderWidthStyle)Enum.Parse(typeof(EnumBorderWidthStyle), WebPageCompilerUtility.StripSingleQuotes(parameters[8]));
						if (bwStyle != EnumBorderWidthStyle.inherit)
						{
							code.Add(Indentation.GetIndent());
							if (bwStyle == EnumBorderWidthStyle.useNumber)
							{
								code.Add(string.Format(CultureInfo.InvariantCulture,
								"{0}.borderWidth='{1}px';\r\n", v, parameters[9]));
							}
							else
							{
								code.Add(string.Format(CultureInfo.InvariantCulture,
								"{0}.borderWidth={1};\r\n", v, parameters[8]));
							}
						}
					}
				}
			}
			processFileParam(code, v, "icon", parameters[11]);
			if (!string.IsNullOrEmpty(parameters[12]))
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.title={1};\r\n", v, parameters[12]));
			}
			code.Add(Indentation.GetIndent());
			code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.hideCloseButtons={1};\r\n", v, parameters[13]));
			//
			if (parameters.Count > 20)
			{
				processFileParam(code, v, "iconClose", parameters[15]);
				processFileParam(code, v, "iconOK", parameters[16]);
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}.overflow={1};\r\n", v, parameters[17]));
				if (string.CompareOrdinal(parameters[18], "false") != 0)
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.hideTitle={1};\r\n", v, parameters[18]));
				}
				if (string.CompareOrdinal(parameters[19], "null") != 0)
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.titleBkColor={1};\r\n", v, parameters[19]));
				}
				if (string.CompareOrdinal(parameters[20], "null") != 0)
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.titleColor={1};\r\n", v, parameters[20]));
				}
			}
			if (parameters.Count > 21)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}.pageId={1};\r\n", v, parameters[21]));
			}
			return _lastChildCode;
		}
		[Browsable(false)]
		public static bool CreateActionJavaScriptStatic(string codeName, string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			if (string.CompareOrdinal(methodName, "CreateWebPage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined') JsonDataBinding.pageMoveout = true;\r\n");
				code.Add(Indentation.GetIndent());
				string act = string.Format(CultureInfo.InvariantCulture,
					"window.location.href = '{0}.html';\r\n", codeName);
				code.Add(act);
				return true;
			}
			else if (string.CompareOrdinal(methodName, "CreateWebPageInNewWindow") == 0)
			{
				code.Add(Indentation.GetIndent());
				string act = string.Format(CultureInfo.InvariantCulture,
					"window.open('{0}.html','{0}');\r\n", codeName);
				code.Add(act);
				return true;
			}
			else if (string.CompareOrdinal(methodName, "CreateWebPageInPopupWindow") == 0)
			{
				string[] pnames = { "status", "toolbar", "location", "menubar", "directories", "resizable", "scrollbars", "height", "width" };
				StringBuilder sb = new StringBuilder();
				if (parameters != null && parameters.Count > 0)
				{
					bool first = true;
					for (int i = 0; i < parameters.Count && i < pnames.Length; i++)
					{
						if (!string.IsNullOrEmpty(parameters[i]))
						{
							if (first)
							{
								first = false;
							}
							else
							{
								sb.Append(",");
							}
							sb.Append(pnames[i]);
							sb.Append("=");
							sb.Append(parameters[i].Replace("'", "").ToLowerInvariant());
						}
					}
				}
				string act;
				if (string.IsNullOrEmpty(returnReceiver))
				{
					act = string.Format(CultureInfo.InvariantCulture,
						 "window.open('{0}.html','{0}','{1}');\r\n", codeName, sb.ToString());
				}
				else
				{
					act = string.Format(CultureInfo.InvariantCulture,
						 "{2} = window.open('{0}.html','{0}','{1}');\r\n", codeName, sb.ToString(), returnReceiver);
				}
				code.Add(Indentation.GetIndent());
				code.Add(act);
				return true;
			}
			return false;
		}
		[Browsable(false)]
		public void CreateActionJavaScript(string methodName, StringCollection code, StringCollection parameters, string returnReceiver)
		{
			if (string.CompareOrdinal(methodName, "ShowDebugMessage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.ShowDebugInfoLine({0});\r\n", parameters[0]));
			}
			else if (string.CompareOrdinal(methodName, "Print") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("window.print();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "selectNextInput") == 0)
			{
				code.Add(Indentation.GetIndent());
				_useSendKeys = true;
				code.Add("JsonDataBinding.selectNextInput();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "sendKeys") == 0)
			{
				code.Add(Indentation.GetIndent());
				_useSendKeys = true;
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.sendKeys({0});\r\n", parameters[0]));
			}
			else if (string.CompareOrdinal(methodName, "reload") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined' && JsonDataBinding.IsFireFox()) setTimeout('window.location.reload(true);', 0); else window.location.reload(true);\r\n");
			}
			else if (string.CompareOrdinal(methodName, "close") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined') JsonDataBinding.pageMoveout = true;\r\n");
				code.Add(Indentation.GetIndent());
				code.Add("window.close();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "CreateWebPage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined') JsonDataBinding.pageMoveout = true;\r\n");
				code.Add(Indentation.GetIndent());
				string act = string.Format(CultureInfo.InvariantCulture,
					"window.location.href = '{0}.html';\r\n", this.CodeName);
				code.Add(act);
			}
			else if (string.CompareOrdinal(methodName, "CreateWebPageInNewWindow") == 0)
			{
				code.Add(Indentation.GetIndent());
				string act = string.Format(CultureInfo.InvariantCulture,
					"window.open('{0}.html','{0}');\r\n", this.CodeName);
				code.Add(act);
			}
			else if (string.CompareOrdinal(methodName, "CreateWebPageInPopupWindow") == 0)
			{
				string[] pnames = { "status", "toolbar", "location", "menubar", "directories", "resizable", "scrollbars", "height", "width" };
				StringBuilder sb = new StringBuilder();
				if (parameters != null && parameters.Count > 0)
				{
					bool first = true;
					for (int i = 0; i < parameters.Count && i < pnames.Length; i++)
					{
						if (!string.IsNullOrEmpty(parameters[i]))
						{
							if (first)
							{
								first = false;
							}
							else
							{
								sb.Append(",");
							}
							sb.Append(pnames[i]);
							sb.Append("=");
							sb.Append(parameters[i].Replace("'", "").ToLowerInvariant());
						}
					}
				}
				string act;
				if (string.IsNullOrEmpty(returnReceiver))
				{
					act = string.Format(CultureInfo.InvariantCulture,
						 "window.open('{0}.html','{0}','{1}');\r\n", this.CodeName, sb.ToString());
				}
				else
				{
					act = string.Format(CultureInfo.InvariantCulture,
						 "{2} = window.open('{0}.html','{0}','{1}');\r\n", this.CodeName, sb.ToString(), returnReceiver);
				}
				code.Add(Indentation.GetIndent());
				code.Add(act);
			}
			else if (string.CompareOrdinal(methodName, "SetStatus") == 0)
			{
				string act = string.Format(CultureInfo.InvariantCulture,
					"window.status = {0};\r\n", parameters[0]);
				code.Add(Indentation.GetIndent());
				code.Add(act);
			}
			else if (string.CompareOrdinal(methodName, "GoBack") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined') JsonDataBinding.pageMoveout = true;\r\n");
				code.Add(Indentation.GetIndent());
				code.Add("history.back();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "GoForward") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined') JsonDataBinding.pageMoveout = true;\r\n");
				code.Add(Indentation.GetIndent());
				code.Add("history.forward();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "GotoWebPage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"if(typeof JsonDataBinding != 'undefined') JsonDataBinding.gotoWebPage({0});\r\n", parameters[0]));
				code.Add(Indentation.GetIndent());
				string act = string.Format(CultureInfo.InvariantCulture,
					"else window.location.href = {0};\r\n", parameters[0]);
				code.Add(act);
			}
			else if (string.CompareOrdinal(methodName, "ShowPopupWindow") == 0)
			{
				string w = GetJavaScriptWebMethodReferenceCode(CodeName, methodName, code, parameters);
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"{0}={1};\r\n", returnReceiver, w));
				}
			}
			else if (string.CompareOrdinal(methodName, "LogOff") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("JsonDataBinding.LogOff();");
			}
			else if (string.CompareOrdinal(methodName, "ShowChildDialog") == 0)
			{
				string ind = Indentation.GetIndent();
				string v = CreateDialogParameters(code, parameters, true);
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.showChild({1});\r\n", ind, v));
			}
			else if (string.CompareOrdinal(methodName, "ShowChildWindow") == 0)
			{
				string ind = Indentation.GetIndent();
				string v = CreateDialogParameters(code, parameters, false);
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"{0}JsonDataBinding.showChild({1});\r\n", ind, v));
			}
			else if (string.CompareOrdinal(methodName, "HideChildPage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.hideChild({0});\r\n", WebNameToPageFilename(parameters[0])));
			}
			else if (string.CompareOrdinal(methodName, "CloseChildPage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture,
					"JsonDataBinding.closeChild({0});\r\n", WebNameToPageFilename(parameters[0])));
			}
			else if (string.CompareOrdinal(methodName, "HidePage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("JsonDataBinding.hidePage();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "ClosePage") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("if(typeof JsonDataBinding != 'undefined') JsonDataBinding.pageMoveout = true;\r\n");
				code.Add(Indentation.GetIndent());
				code.Add("JsonDataBinding.closePage();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "ConfirmDialog") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("JsonDataBinding.confirmDialog();\r\n");
			}
			else if (string.CompareOrdinal(methodName, "moveTo") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture, "window.moveTo({0},{1});\r\n", WebPageCompilerUtility.GetMethodParameterValueInt(parameters[0], code), WebPageCompilerUtility.GetMethodParameterValueInt(parameters[1], code)));
			}
			else if (string.CompareOrdinal(methodName, "resizeTo") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture, "window.resizeTo({0},{1});\r\n", WebPageCompilerUtility.GetMethodParameterValueInt(parameters[0], code), WebPageCompilerUtility.GetMethodParameterValueInt(parameters[1], code)));
			}
			else if (string.CompareOrdinal(methodName, "maximizeToAvailableScreen") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add("window.moveTo(0,0);\r\n");
				code.Add(Indentation.GetIndent());
				code.Add("window.resizeTo(window.screen.availWidth,window.screen.availHeight);\r\n");
			}
			else if (string.CompareOrdinal(methodName, "getParentVale") == 0)
			{
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture, "{0}=JsonDataBinding.getParentWindowValue({1});\r\n", returnReceiver, parameters[0]));
				}
			}
			else if (string.CompareOrdinal(methodName, "getParentUrl") == 0)
			{
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture, "{0}=JsonDataBinding.getParentWindowUrl();\r\n", returnReceiver));
				}
			}
			else if (string.CompareOrdinal(methodName, "GetValueInCurrentCulture") == 0)
			{
				if (!string.IsNullOrEmpty(returnReceiver))
				{
					code.Add(Indentation.GetIndent());
					code.Add(string.Format(CultureInfo.InvariantCulture, "{0}=JsonDataBinding.GetValueInCurrentCulture({0});\r\n", returnReceiver, parameters[0]));
				}
			}
			else if (string.CompareOrdinal(methodName, "setParentVale") == 0)
			{
				code.Add(Indentation.GetIndent());
				code.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.setParentWindowValue({0},{1});\r\n", parameters[0], parameters[1]));
			}
			else //
			{
				string element = "document";
				if (string.CompareOrdinal("getDirectChildElementsByTagName", methodName) == 0)
				{
					element = "document.body";
				}
				WebPageCompilerUtility.CreateActionJavaScript(element, methodName, code, parameters, returnReceiver);
			}
		}
		[Browsable(false)]
		public string GetJavaScriptReferenceCode(StringCollection method, string attributeName, string[] parameters)
		{
			if (string.CompareOrdinal(attributeName, "CurrentUserAlias") == 0)
			{
				return "JsonDataBinding.GetCurrentUserAlias()";
			}
			else if (string.CompareOrdinal(attributeName, "CurrentUserID") == 0)
			{
				return "JsonDataBinding.GetCurrentUserID()";
			}
			else if (string.CompareOrdinal(attributeName, "CurrentUserLevel") == 0)
			{
				return "JsonDataBinding.GetCurrentUserLevel()";
			}
			else if (string.CompareOrdinal(attributeName, "UserLoggedOn") == 0)
			{
				return "JsonDataBinding.UserLoggedOn()";
			}
			else if (string.CompareOrdinal(attributeName, "UserLevel") == 0)
			{
				return "JsonDataBinding.TargetUserLevel";
			}
			else if (string.CompareOrdinal(attributeName, "SelectedText") == 0)
			{
				return "JsonDataBinding.GetSelectedText()";
			}
			else if (string.CompareOrdinal(attributeName, "ServerError") == 0)
			{
				return "JsonDataBinding.values.ServerError";
			}
			else if (string.CompareOrdinal(attributeName, "dialogResult") == 0)
			{
				return "document.dialogResult";
			}
			else if (string.CompareOrdinal(attributeName, "isDialog") == 0)
			{
				return "document.isDialog";
			}
			else if (string.CompareOrdinal(attributeName, "CloseDialogPrompt") == 0)
			{
				return "document.childManager.CloseDialogPrompt";
			}
			else if (string.CompareOrdinal(attributeName, "CancelDialogPrompt") == 0)
			{
				return "document.childManager.CancelDialogPrompt";
			}
			if (string.CompareOrdinal(attributeName, "confirmResult") == 0)
			{
				return "JsonDataBinding.confirmResult";
			}
			if (string.CompareOrdinal(attributeName, "cursor") == 0)
			{
				return "document.body.style.cursor";
			}
			if (string.CompareOrdinal(attributeName, "VisitorScreen") == 0)
			{
				return "window.screen";
			}
			StringCollection ps = new StringCollection();
			if (parameters != null)
			{
				ps.AddRange(parameters);
			}
			string s = GetJavaScriptWebMethodReferenceCode(WebPageCompilerUtility.JsCodeRef(CodeName), attributeName, method, ps);
			if (!string.IsNullOrEmpty(s))
			{
				return s;
			}
			return null;
		}
		[Browsable(false)]
		public virtual string MapJavaScriptVallue(string name, string value)
		{
			string s = WebPageCompilerUtility.MapJavaScriptVallue(name, value, _resourceFiles);
			if (s != null)
			{
				return s;
			}
			return value;
		}
		#endregion

		#region IWebClientControl Properties
		[Browsable(false)]
		[Description("class names for the element")]
		[WebClientMember]
		public string className { get; set; }

		[Description("id of the html element")]
		[Browsable(false)]
		[WebClientMember]
		public string id { get { return string.Empty; } }

		[Description("tag name of the html element")]
		[Browsable(false)]
		[WebClientMember]
		public string tagName { get { return ElementName; } }

		[Description("Returns the viewable width of the content on a page (not including borders, margins, or scrollbars)")]
		[Browsable(false)]
		[WebClientMember]
		public int clientWidth { get { return 0; } }

		[Description("Returns the viewable height of the content on a page (not including borders, margins, or scrollbars)")]
		[Browsable(false)]
		[WebClientMember]
		public int clientHeight { get { return 0; } }

		[XmlIgnore]
		[Description("Sets or returns the HTML contents (+text) of an element")]
		[Browsable(false)]
		[WebClientMember]
		public string innerHTML { get; set; }

		[Description("Returns the height of an element, including borders and padding if any, but not margins")]
		[Browsable(false)]
		[WebClientMember]
		public int offsetHeight { get { return 0; } }

		[Description("Returns the width of an element, including borders and padding if any, but not margins")]
		[Browsable(false)]
		[WebClientMember]
		public int offsetWidth { get { return 0; } }

		[Description("Returns the horizontal offset position of the current element relative to its offset container")]
		[Browsable(false)]
		[WebClientMember]
		public int offsetLeft { get { return 0; } }

		[Description("Returns the vertical offset position of the current element relative to its offset container")]
		[Browsable(false)]
		[WebClientMember]
		public int offsetTop { get { return 0; } }

		[Description("Returns the entire height of an element (including areas hidden with scrollbars)")]
		[Browsable(false)]
		[WebClientMember]
		public int scrollHeight { get { return 0; } }

		[Description("Returns the distance between the actual left edge of an element and its left edge currently in view")]
		[Browsable(false)]
		[WebClientMember]
		public int scrollLeft { get { return 0; } }

		[Description("Returns the distance between the actual top edge of an element and its top edge currently in view")]
		[Browsable(false)]
		[WebClientMember]
		public int scrollTop { get { return 0; } }

		[Description("Returns the entire width of an element (including areas hidden with scrollbars)")]
		[Browsable(false)]
		[WebClientMember]
		public int scrollWidth { get { return 0; } }
		#endregion

		#region server processing
		virtual protected void OnRequestGetData(string value) { }
		virtual protected void OnRequestPutData(string value) { }
		virtual protected void OnRequestExecution(string method, string value) { }
		virtual protected void OnRequestFinish() { }
		//
		protected void AddApplyDataBindMethod(string dataName) { }
		[WebServerMember]
		protected void AddClientScript(string script) { }
		[WebServerMember]
		protected void AddDownloadValue(string name, string value) { }
		protected void AddDataTable(string name) { }

		[WebServerMember]
		public string AspxPhysicalFolder
		{
			get
			{
				return null;
			}
		}
		[WebServerMember]
		public string PhpPhysicalFolder
		{
			get
			{
				return null;
			}
		}
		[Description("Gets the IP address from which the user is viewing the current page. ")]
		[WebServerMember]
		public string IPAddress
		{
			get
			{
				return null;
			}
		}
		[Description("Gets the IP address from which the user is viewing the current page. It can be more accurate than IPAddress but it can be more easily spoofed and thus less secure than IPAddress.")]
		[WebServerMember]
		public string IPAddress2
		{
			get
			{
				return null;
			}
		}
		#endregion

		#region IDynamicMethodParameters Members
		[NotForProgramming]
		[Browsable(false)]
		public Dictionary<string, string> GetParameterDescriptions(string methodName)
		{
			if (string.CompareOrdinal(methodName, "FetchData") == 0)
			{
				Dictionary<string, string> descs = new Dictionary<string, string>();
				descs.Add("dataName", "Select a DataTable to provide data for data binding");
				return descs;
			}
			return null;
		}
		[Browsable(false)]
		public ParameterInfo[] GetDynamicMethodParameters(string methodName, object attrs)
		{
			if (string.CompareOrdinal(methodName, "FetchData") == 0)
			{
				SimpleParameterInfo dataName = new SimpleParameterInfo("dataName", "FetchData", typeof(ComponentPointer), "Select a DataTable to provide data for data binding");
				return new ParameterInfo[] { dataName };
			}
			else
			{
				return VPLUtil.FindParameters(this.GetType(), methodName);
			}
		}
		[Browsable(false)]
		public object InvokeWithDynamicMethodParameters(string methodName, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return null;
		}
		[Browsable(false)]
		public bool IsUsingDynamicMethodParameters(string methodName)
		{
			return (string.CompareOrdinal(methodName, "FetchData") == 0);
		}

		#endregion

		#region IValueUIEditorOwner Members

		public EditorAttribute GetValueUIEditor(string valueName)
		{
			if (string.CompareOrdinal(valueName, "dataName") == 0)
			{
				Type t = VPLUtil.GetServiceByName(VPLUtil.SERVICE_ComponentSelector) as Type;
				return new EditorAttribute(t, typeof(UITypeEditor));
			}
			if (string.CompareOrdinal(valueName, "webpage") == 0 || string.CompareOrdinal(valueName, "pageAddress") == 0)
			{
				return new EditorAttribute(typeof(TypeSelectorWebPage), typeof(UITypeEditor));
			}
			if (string.CompareOrdinal(valueName, "iconPath") == 0 || string.CompareOrdinal(valueName, "closeIconPath") == 0 || string.CompareOrdinal(valueName, "okIconPath") == 0)
			{
				return new EditorAttribute(typeof(PropEditorFilePath), typeof(UITypeEditor));
			}
			if (string.CompareOrdinal(valueName, "windowName") == 0)
			{
				return new EditorAttribute(typeof(TypeEditorValueEnum), typeof(UITypeEditor));
			}
			return null;
		}

		#endregion

		#region IWithProject2 Members
		[Browsable(false)]
		public LimnorProject Project
		{
			get { return _prj; }
		}
		[Browsable(false)]
		public void SetProject(LimnorProject project)
		{
			_prj = project;
		}

		#endregion

		#region IWebClientInitializer Members

		public void OnWebPageLoaded(StringCollection sc)
		{
			if (ShowAjaxCallWaitingImage != null && ShowAjaxCallWaitingImage.Site != null)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					"\r\ndocument.getElementById('{0}').style.display='none';",
					ShowAjaxCallWaitingImage.Site.Name));
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					"\r\nJsonDataBinding.ShowAjaxCallWaitingImage=document.getElementById('{0}');",
					ShowAjaxCallWaitingImage.Site.Name));

			}
			if (ShowAjaxCallWaitingLabel != null && ShowAjaxCallWaitingLabel.Site != null)
			{
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					"\r\ndocument.getElementById('{0}').style.display='none';",
					ShowAjaxCallWaitingLabel.Site.Name));
				sc.Add(string.Format(CultureInfo.InvariantCulture,
					"\r\nJsonDataBinding.ShowAjaxCallWaitingLabel=document.getElementById('{0}');",
					ShowAjaxCallWaitingLabel.Site.Name));
			}
		}
		public void OnWebPageLoadedAfterEventHandlerCreations(StringCollection sc)
		{
			if (_useSendKeys)
			{
				sc.Add("\r\nJsonDataBinding.initSendKeys();");
			}
		}
		#endregion

		#region Methods
		[Browsable(false)]
		[NotForProgramming]
		public void OnDoCopy()
		{
			if (_htmlEditor != null)
			{
				_htmlEditor.DoCopy();
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public void OnDoPaste()
		{
			if (_htmlEditor != null)
			{
				_htmlEditor.DoPaste();
			}
		}
		[Browsable(false)]
		[NotForProgramming]
		public IComponent GetComponentByName(string name)
		{
			return _class.GetComponentByName(name);
		}
		public void RemoveDesignHtmlFiles()
		{
			if (_htmlEditor != null)
			{
				string f = _htmlEditor.HtmlFile;
				if (File.Exists(f))
				{
					try
					{
						File.Delete(f);
					}
					catch
					{
					}
				}
				f = Path.Combine(Path.GetDirectoryName(_htmlEditor.HtmlFile), string.Format(CultureInfo.InvariantCulture, "{0}.css", Path.GetFileNameWithoutExtension(_htmlEditor.HtmlFile)));
				if (File.Exists(f))
				{
					try
					{
						File.Delete(f);
					}
					catch
					{
					}
				}
			}
		}
		public void OnBeforeIDErun()
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				_htmlEditor.ApplyTextBoxChanges();
			}
		}
		public void SaveHtmlFile()
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				if (_htmlEditor.checkHtmlChange())
				{
					_htmlChanged = true;
					try
					{
						_htmlEditor.Save();
					}
					catch
					{
					}
				}
				if (_htmlChanged)
				{
					_htmlEditor.CopyToProjectFolder();
					_htmlChanged = false;
				}
			}
		}
		public bool UpdateHtmlFile()
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				string htmlFile = _htmlEditor.HtmlFile;
				if (_htmlEditor.checkHtmlChange())
				{
					_htmlChanged = true;
					try
					{
						//validate html elements=======================================================
						IList<HtmlElement_BodyBase> elements = _class.UsedHtmlElements;
						if (elements != null && elements.Count > 0)
						{
							List<HtmlElement_ItemBase> invalids = new List<HtmlElement_ItemBase>();
							for (int i = 0; i < elements.Count; i++)
							{
								HtmlElement_ItemBase hei = elements[i] as HtmlElement_ItemBase;
								if (hei != null)
								{
									if (hei.ElementGuid == Guid.Empty)
									{
										invalids.Add(hei);
									}
									else
									{
										string id = getIdByGuid(hei.ElementGuidString);
										if (string.IsNullOrEmpty(id))
										{
											invalids.Add(hei);
										}
										else
										{
											if (string.CompareOrdinal(id, hei.id) != 0)
											{
												hei.SetId(id);
												_class.SaveHtmlElement(hei);
											}
										}
									}
								}
							}
							if (invalids.Count > 0)
							{
								foreach (HtmlElement_ItemBase hei in invalids)
								{
									_class.RemoveHtmlElement(hei);
								}
							}
						}
						_class.RemoveInvalidHtmlComponentIcons();
						_class.RemoveInvalidHtmlEventHandlers();
					}
					catch (Exception err)
					{
						MathNode.Log(this.FindForm(), err);
					}
					try
					{
						_htmlEditor.Save();
					}
					catch (Exception err)
					{
						MathNode.Log(this.FindForm(), err);
					}
				}
			}
			return _htmlChanged;
		}
		public void SelectHtmlElementByGuid(Guid guid)
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				_htmlEditor.SelectHtmlElementByGuid(guid);
			}
		}
		public IList<HtmlElement_map> GetMaps()
		{
			List<HtmlElement_map> maps = new List<HtmlElement_map>();
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				string s = _htmlEditor.GetMaps();
				if (!string.IsNullOrEmpty(s))
				{
					string[] ss = s.Split('|');
					for (int i = 0; i < ss.Length; i++)
					{
						HtmlElement_map map = _class.ParseHtmlElement(ss[i]) as HtmlElement_map;
						if (map != null)
						{
							maps.Add(map);
						}
					}
				}
			}
			return maps;
		}
		public List<HtmlElement_area> GetAreas(string mapId)
		{
			List<HtmlElement_area> maps = new List<HtmlElement_area>();
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				string s = _htmlEditor.GetAreas(mapId);
				if (!string.IsNullOrEmpty(s))
				{
					string[] ss = s.Split('|');
					for (int i = 0; i < ss.Length; i++)
					{
						HtmlElement_area map = _class.ParseHtmlElement(ss[i]) as HtmlElement_area;
						if (map != null)
						{
							maps.Add(map);
						}
					}
				}
			}
			return maps;
		}
		public HtmlElement_map CreateNewMap()
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				Guid g = Guid.NewGuid();
				string s = _htmlEditor.CreateNewMap(HtmlElement_Base.GetGuidString(g));
				if (!string.IsNullOrEmpty(s))
				{
					HtmlElement_map map = HtmlElement_Base.CreateHtmlElement(_class, s) as HtmlElement_map;
					if (map != null)
					{
						_class.OnUseHtmlElement(map);
						return map;
					}
				}
			}
			return null;
		}
		public void UpdateMapAreas(HtmlElement_map map)
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				StringBuilder sb = new StringBuilder();
				List<HtmlElement_area> areas = map.Areas;
				for (int i = 0; i < areas.Count; i++)
				{
					if (i > 0)
					{
						sb.Append("|");
					}
					sb.Append(areas[i].id);
					sb.Append(";");
					sb.Append(areas[i].ElementGuidString);
					sb.Append(";");
					sb.Append(areas[i].shape.ToString());
					sb.Append(";");
					if (areas[i].coords != null && areas[i].coords.Length > 0)
					{
						sb.Append(areas[i].coords[0].ToString(CultureInfo.InvariantCulture));
						for (int k = 1; k < areas[i].coords.Length; k++)
						{
							sb.Append(",");
							sb.Append(areas[i].coords[k].ToString(CultureInfo.InvariantCulture));
						}
					}
				}
				_htmlEditor.UpdateMapAreas(map.id, sb.ToString());
			}
		}
		public void SetUseMap(string imgId, string mapId)
		{
			if (_htmlEditor != null && _htmlEditor.EditorStarted)
			{
				_htmlEditor.SetUseMap(imgId, mapId);
			}
		}
		public void OnSelectHtmlElement(string selectedElement)
		{
			if (_class != null)
			{
				_class.OnSelectHtmlElement(selectedElement);
			}
		}
		public void OnRightClickElement(string selectedElement, int x, int y)
		{
			if (_class != null)
			{
				_class.OnRightClickElement(selectedElement, x, y);
			}
		}
		public void OnEditorStarted()
		{
		}
		public void OnLoadEditorFailed()
		{
		}
		public void OnUpdated()
		{
			if (_htmlEditor != null)
			{
				_htmlEditor.ResetSavingFlag();
			}
		}
		public void OnAddFileupload(string id)
		{
			if (_class != null)
			{
				_class.OnSelectHtmlElement(string.Format(CultureInfo.InvariantCulture, "fileupload,,,{0},", id));
				HtmlElement_fileupload hf = _class.CurrentHtmlElement as HtmlElement_fileupload;
				if (hf != null)
				{
					if (hf.ElementGuid == Guid.Empty)
					{
						_class.OnUseHtmlElement(hf);
					}
				}
			}
		}
		public void onPageStarted()
		{
		}
		public void OnElementIdChanged(Guid g, string id)
		{
			if (_class != null)
			{
				_class.OnElementIdChanged(g, id);
			}
		}
		public static string GetFilePattern(string filetypes)
		{
			//the logic is in filebrowser.php
			if (!string.IsNullOrEmpty(filetypes))
			{
				if (string.Compare(filetypes, ".html", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return "Web Pages|*.html;*.htm";
				}
				if (string.Compare(filetypes, ".image", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return "Image files|*.jpg;*.png;*.gif;*.bmp";
				}
				if (string.Compare(filetypes, ".href", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return "Web Pages|*.html;*.htm|Image files|*.jpg;*.png;*.gif;*.bmp|CSS files|*.css|JavaScript|*.js|Media files|*.mp3;*.mp4;*.swf";
				}
				string[] ss = filetypes.Split(';');
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < ss.Length; i++)
				{
					if (!string.IsNullOrEmpty(ss[i]))
					{
						sb.Append(string.Format(CultureInfo.InvariantCulture, "*.{0};", ss[i]));
					}
				}
				return string.Format(CultureInfo.InvariantCulture, "Files|{0}", sb.ToString());
			}
			return "Files|*.*";
		}
		public string OnSelectFile(string title, string filetypes, string subFolder, string subName, int msize, bool disableUpload)
		{
			DialogHtmlFile dlg = new DialogHtmlFile();
			dlg.LoadData(_htmlEditor.PhysicalFolder, null, title, GetFilePattern(filetypes));
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				return dlg.WebFile;
			}
			return string.Empty;
		}
		public void RefreshHtmlElementSelection()
		{
			if (_class != null)
			{
				_class.RefreshHtmlElementSelection();
			}
		}
		public void OnClosing()
		{
			UpdateHtmlFile();
		}
		public void RemovePageCache()
		{
			if (_htmlEditor != null)
			{
				try
				{
					List<string> targets = new List<string>();
					targets.Add(Path.GetFileNameWithoutExtension(_htmlEditor.HtmlFile));
					targets.Add("ideloader");
					targets.Add("LimnorStudio");
					targets.Add("htmlEditorClient");
					DesignUtil.DeleteInternetExplorerCache(targets.ToArray());
				}
				catch (Exception err)
				{
					DesignUtil.WriteToOutputWindowAndLog("Cannot remove web page cache. {0}", err.Message);
				}
			}
		}
		public void RefreshWebDisplay()
		{
			if (_htmlEditor != null)
			{
				RemovePageCache();
				_timer.Enabled = true;
			}
		}
		public void SetHtmlEditor(IWebHost editor)
		{
			_htmlEditor = editor;
		}
		public IWebHost GetHtmlEditor()
		{
			return _htmlEditor;
		}
		public void SetWebProperty(string name, string value)
		{
			if (_htmlEditor != null)
			{
				_htmlEditor.SetWebProperty(name, value);
			}
		}
		public string AppendArchiveFile(string name, string archiveFilePath)
		{
			if (_htmlEditor != null)
			{
				return _htmlEditor.AppendArchiveFile(name, archiveFilePath);
			}
			return string.Empty;
		}
		public string CreateOrGetIdForCurrentElement(string newGuid)
		{
			if (_htmlEditor != null)
			{
				return _htmlEditor.CreateOrGetIdForCurrentElement(newGuid);
			}
			return string.Empty;
		}
		public bool SetGuidById(string id, string guid)
		{
			if (_htmlEditor != null)
			{
				return _htmlEditor.SetGuidById(id, guid);
			}
			return false;
		}
		public Dictionary<string, string> getIdList()
		{
			if (_htmlEditor != null)
			{
				return _htmlEditor.getIdList();
			}
			return new Dictionary<string, string>();
		}
		public string getIdByGuid(string guid)
		{
			if (_htmlEditor != null)
			{
				return _htmlEditor.getIdByGuid(guid);
			}
			return string.Empty;
		}
		public string getTagNameById(string id)
		{
			if (_htmlEditor != null)
			{
				return _htmlEditor.getTagNameById(id);
			}
			return string.Empty;
		}
		public void AddWebControl(IDesignerLoaderHost host)
		{
		}
		public void SetClosing()
		{
			_closing = true;
		}
		public string MapEventOwnerName(string eventName)
		{
			if (string.CompareOrdinal(eventName, "onClosingWindow") == 0)
			{
				return "document.childManager";
			}
			if (string.CompareOrdinal(eventName, "onChildWindowReady") == 0)
			{
				return "document.childManager";
			}
			if (string.CompareOrdinal(eventName, "onunload") == 0)
			{
				return "window";
			}
			if (string.CompareOrdinal(eventName, "onbeforeunload") == 0)
			{
				return "window";
			}
			return "document";
		}
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			foreach (Control c in this.Controls)
			{
				IOwnerDrawControl od = c as IOwnerDrawControl;
				if (od != null)
				{
					od.DrawControl(e.Graphics);
				}
			}
		}
		protected override void OnDragOver(DragEventArgs drgevent)
		{
			drgevent.Effect = DragDropEffects.None;
		}
		[Browsable(false)]
		[NotForProgramming]
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		#region IWebPage Members
		[Browsable(false)]
		[NotForProgramming]
		public IComponent AddComponent(Type t)
		{
			return Activator.CreateInstance(t, components) as IComponent;
		}

		#endregion

		#region IWebClientSupport Members

		public string GetJavaScriptWebMethodReferenceCode(string ownerCodeName, string methodName, StringCollection code, StringCollection parameters)
		{
			if (string.CompareOrdinal(methodName, "ShowPopupWindow") == 0)
			{
				string act;
				bool singleInstance;
				string w = string.Format(CultureInfo.InvariantCulture, "w{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				code.Add(string.Format(CultureInfo.InvariantCulture, "var {0};\r\n", w));
				if (string.IsNullOrEmpty(parameters[1]))
				{
					singleInstance = true;
					code.Add(string.Format(CultureInfo.InvariantCulture, "{0} = JsonDataBinding.getWindowByPageFilename({1});\r\n", w, parameters[0]));
				}
				else
				{
					if (string.CompareOrdinal(parameters[1], "_blank") == 0)
					{
						singleInstance = false;
					}
					else
					{
						string vCs = string.Format(CultureInfo.InvariantCulture, "c{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
						code.Add(string.Format(CultureInfo.InvariantCulture, "var {0}={1};\r\n", vCs, parameters[1]));
						code.Add(string.Format(CultureInfo.InvariantCulture, "if({0} != '_blank') {1} = JsonDataBinding.getWindowByPageFilename({2});\r\n", vCs, w, parameters[0]));
						singleInstance = true;
					}
				}
				StringCollection specs = new StringCollection();
				addOpenSpecs(code, specs, "status", parameters[2]);
				addOpenSpecs(code, specs, "toolbar", parameters[3]);
				addOpenSpecs(code, specs, "location", parameters[4]);
				addOpenSpecs(code, specs, "menubar", parameters[5]);
				addOpenSpecs(code, specs, "scrollbars", parameters[6]);
				addOpenSpecs(code, specs, "titlebar", parameters[7]);
				addOpenSpecs(code, specs, "resizable", parameters[8]);
				//
				addOpenSpecsInt(code, specs, "left", parameters[9], true);
				addOpenSpecsInt(code, specs, "top", parameters[10], true);
				addOpenSpecsInt(code, specs, "width", parameters[11], false);
				addOpenSpecsInt(code, specs, "height", parameters[12], false);
				//
				string vspecs = string.Format(CultureInfo.InvariantCulture, "v{0}", Guid.NewGuid().GetHashCode().ToString("x", CultureInfo.InvariantCulture));
				StringBuilder sSpecs = new StringBuilder();
				if (specs.Count > 0)
				{
					sSpecs.Append(specs[0]);
					for (int i = 1; i < specs.Count; i++)
					{
						sSpecs.Append("+ ',' +");
						sSpecs.Append(specs[i]);
					}
				}
				else
				{
					sSpecs.Append("''");
				}
				if (singleInstance)
				{
					code.Add(string.Format(CultureInfo.InvariantCulture,
						"if(!{0}) {{\r\n", w));
				}
				code.Add(string.Format(CultureInfo.InvariantCulture, "var {0}={1};\r\n", vspecs, sSpecs.ToString()));

				act = string.Format(CultureInfo.InvariantCulture,
						"{4} = window.open({0},{1}, {2}, {3});\r\n", parameters[0], parameters[1], vspecs, parameters[13], w);
				code.Add(act);
				code.Add(string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.addWindow({0});\r\n", w));
				if (singleInstance)
				{
					code.Add("}\r\n");
				}
				code.Add(string.Format(CultureInfo.InvariantCulture,
					   "{0}.focus();\r\n", w));
				return w;
			}
			else if (string.CompareOrdinal(methodName, "getParentVale") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.getParentWindowValue({0})", parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "GetValueInCurrentCulture") == 0)
			{
				return string.Format(CultureInfo.InvariantCulture, "JsonDataBinding.GetValueInCurrentCulture({0})", parameters[0]);
			}
			else if (string.CompareOrdinal(methodName, "getParentUrl") == 0)
			{
				return "JsonDataBinding.getParentWindowUrl()";
			}
			return WebPageCompilerUtility.GetJavaScriptWebMethodReferenceCode(ownerCodeName, methodName, code, parameters);
		}

		public string GetJavaScriptWebPropertyReferenceCode(StringCollection method, string propertyName, string[] parameters)
		{
			return GetJavaScriptReferenceCode(method, propertyName, parameters);
		}

		#endregion

		#region ISourceValueEnumProvider Members

		public object[] GetValueEnum(string section, string item)
		{
			if (string.CompareOrdinal(section, "ShowPopupWindow") == 0)
			{
				if (string.CompareOrdinal(item, "windowName") == 0)
				{
					object[] vs = new object[4];
					vs[0] = "_blank";
					vs[1] = "_parent";
					vs[2] = "_self";
					vs[3] = "_top";
					return vs;
				}
			}
			return null;
		}

		#endregion

		#region LabelPos
		class LabelPos : Label, ISkipWrite
		{
			public LabelPos()
			{
			}

			#region ISkipWrite Members

			public bool SkipSerialize
			{
				get { return true; }
			}

			#endregion
		}
		#endregion

		#region IDevClassReferencer Members

		public void SetDevClass(IDevClass c)
		{
			_class = (ClassPointer)c;
		}

		public IDevClass GetDevClass()
		{
			return _class;
		}

		#endregion

		#region IWebClientComponent Members
		public bool IsParameterFilePath(string parameterName)
		{
			if (string.CompareOrdinal(parameterName, "BackgroundImageFile") == 0)
			{
				return true;
			}
			return false;
		}
		public string CreateWebFileAddress(string localFilePath, string parameterName)
		{
			if (string.CompareOrdinal(parameterName, "BackgroundImageFile") == 0)
			{
				if (_resourceFiles == null)
				{
					_resourceFiles = new List<WebResourceFile>();
				}
				bool b;
				WebResourceFile wf = new WebResourceFile(localFilePath, WebResourceFile.WEBFOLDER_Images, out b);
				_resourceFiles.Add(wf);
				return wf.WebAddress;
			}
			return null;
		}
		private WebClientValueCollection _customValues;
		[TypeEditorDescription("web page")]
		[WebClientMember]
		[RefreshProperties(RefreshProperties.All)]
		[EditorAttribute(typeof(TypeEditorWebClientValue), typeof(UITypeEditor))]
		[Description("A custom value is associated with an Html element. It provides a storage to hold data for the element.")]
		public WebClientValueCollection CustomValues
		{
			get
			{
				if (_customValues == null)
				{
					_customValues = new WebClientValueCollection(this);
				}
				return _customValues;
			}
		}
		[Bindable(true)]
		[WebClientMember]
		[Description("Gets and sets data associated with the element")]
		public string tag
		{
			get;
			set;
		}
		[Description("Associate a named data with the element")]
		[WebClientMember]
		public void SetOrCreateNamedValue(string name, string value)
		{

		}
		[Description("Gets a named data associated with the element")]
		[WebClientMember]
		public string GetNamedValue(string name)
		{
			return string.Empty;
		}
		[Description("Gets all child elements of the specific tag name")]
		[WebClientMember]
		public IWebClientComponent[] getElementsByTagName(string tagName)
		{
			return null;
		}
		[Description("Gets all immediate child elements of the specific tag name")]
		[WebClientMember]
		public IWebClientComponent[] getDirectChildElementsByTagName(string tagName)
		{
			return null;
		}
		#endregion

		#region ISerializeNotify
		[NotForProgramming]
		[XmlIgnore]
		[ReadOnly(true)]
		[Browsable(false)]
		public bool ReadingProperties
		{
			get;
			set;
		}
		#endregion
	}
	public delegate bool fnInitPage(string pageUrl, string parentUrl);
	public enum EnumHideDialogButtons
	{
		ShowOKCancel = 0,
		HideOKCancel = 1,
		HideOK = 2,
		HideCancel = 3
	}
}
