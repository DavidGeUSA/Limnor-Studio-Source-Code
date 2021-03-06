/*
 
 * Author:	Bob Limnor (info@limnor.com)
 * Project: Limnor Studio
 * Item:	Visual Expression Display and Edit
 * License: GNU General Public License v3.0
 
 */
/*
 * All rights reserved by Longflow Enterprises Ltd
 * 
 * Formula Editor
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace MathExp
{
	class GradientColorTable : ProfessionalColorTable
	{
		public override Color ButtonCheckedGradientBegin
		{
			get { return Color.FromArgb(0xe1, 230, 0xe8); }
		}

		public override Color ButtonCheckedGradientEnd
		{
			get
			{
				return Color.FromArgb(0xe1, 230, 0xe8);
			}
		}

		public override Color ButtonCheckedGradientMiddle
		{
			get
			{
				return Color.FromArgb(0xe1, 230, 0xe8);
			}
		}

		public override Color ButtonCheckedHighlight
		{
			get
			{
				return base.ButtonCheckedHighlight;
			}
		}

		public override Color ButtonCheckedHighlightBorder
		{
			get
			{
				return base.ButtonCheckedHighlightBorder;
			}
		}

		public override Color ButtonPressedBorder
		{
			get
			{
				return base.ButtonPressedBorder;
			}
		}

		public override Color ButtonPressedGradientBegin
		{
			get
			{
				return Color.FromArgb(0x98, 0xb5, 0xe2);
			}
		}

		public override Color ButtonPressedGradientEnd
		{
			get
			{
				return Color.FromArgb(0x98, 0xb5, 0xe2);
			}
		}
		public override Color ButtonPressedGradientMiddle
		{
			get
			{
				return Color.FromArgb(0x98, 0xb5, 0xe2);
			}
		}

		public override Color ButtonSelectedGradientBegin
		{
			get
			{
				return Color.FromArgb(0xc1, 210, 0xee);
			}
		}

		public override Color ButtonSelectedGradientEnd
		{
			get
			{
				return Color.FromArgb(0xc1, 210, 0xee);
			}
		}

		public override Color ButtonSelectedGradientMiddle
		{
			get
			{
				return Color.FromArgb(0xc1, 210, 0xee);
			}
		}

		public override Color CheckBackground
		{
			get
			{
				return Color.FromArgb(0xe1, 230, 0xe8);
			}
		}

		public override Color CheckPressedBackground
		{
			get
			{
				return base.CheckPressedBackground;
			}
		}

		public override Color ButtonPressedHighlight
		{
			get
			{
				return base.ButtonPressedHighlight;
			}
		}

		public override Color ButtonPressedHighlightBorder
		{
			get
			{
				return base.ButtonPressedHighlightBorder;
			}
		}

		public override Color ButtonSelectedBorder
		{
			get
			{
				return base.ButtonSelectedBorder;
			}
		}

		public override Color ButtonSelectedHighlight
		{
			get
			{
				return base.ButtonSelectedHighlight;
			}
		}

		public override Color ButtonSelectedHighlightBorder
		{
			get
			{
				return base.ButtonSelectedHighlightBorder;
			}
		}

		public override Color CheckSelectedBackground
		{
			get
			{
				return base.CheckSelectedBackground;
			}
		}

		public override Color GripDark
		{
			get
			{
				return base.GripDark;
			}
		}

		public override Color GripLight
		{
			get
			{
				return Color.FromArgb(0xff, 0xff, 0xff);
			}
		}

		public override Color ImageMarginGradientBegin
		{
			get
			{
				return Color.FromArgb(0xfe, 0xfe, 0xfb);
			}
		}

		public override Color ImageMarginGradientEnd
		{
			get
			{
				return Color.FromArgb(0xbd, 0xbd, 0xa3);
			}
		}

		public override Color ImageMarginGradientMiddle
		{
			get
			{
				return Color.FromArgb(0xec, 0xe7, 0xe0);
			}
		}

		public override Color ImageMarginRevealedGradientBegin
		{
			get
			{
				return Color.FromArgb(0xf7, 0xf6, 0xef);
			}
		}

		public override Color ImageMarginRevealedGradientEnd
		{
			get
			{
				return Color.FromArgb(230, 0xe3, 210);
			}
		}

		public override Color ImageMarginRevealedGradientMiddle
		{
			get
			{
				return Color.FromArgb(0xf2, 240, 0xe4);
			}
		}

		public override Color MenuBorder
		{
			get
			{
				return Color.FromArgb(0x8a, 0x86, 0x7a);
			}
		}

		public override Color MenuItemBorder
		{
			get
			{
				return Color.FromArgb(0x31, 0x6a, 0xc5);
			}
		}

		public override Color MenuItemPressedGradientBegin
		{
			get
			{
				return Color.FromArgb(0xfc, 0xfc, 0xf9);
			}
		}

		public override Color MenuItemPressedGradientEnd
		{
			get
			{
				return Color.FromArgb(0xf6, 0xf4, 0xec);
			}
		}

		public override Color MenuItemPressedGradientMiddle
		{
			get
			{
				return base.MenuItemPressedGradientMiddle;
			}
		}

		public override Color MenuItemSelected
		{
			get
			{
				return Color.FromArgb(0xc1, 210, 0xee);
			}
		}

		public override Color MenuItemSelectedGradientBegin
		{
			get
			{
				return base.MenuItemSelectedGradientBegin;
			}
		}

		public override Color MenuItemSelectedGradientEnd
		{
			get
			{
				return base.MenuItemSelectedGradientEnd;
			}
		}

		public override Color MenuStripGradientBegin
		{
			get
			{
				return Color.FromArgb(0xe5, 0xe5, 0xd7);
			}
		}

		public override Color MenuStripGradientEnd
		{
			get
			{
				return Color.FromArgb(0xf4, 0xf2, 0xe8);
			}
		}

		public override Color OverflowButtonGradientBegin
		{
			get
			{
				return Color.FromArgb(0xf3, 0xf2, 240);
			}
		}

		public override Color OverflowButtonGradientEnd
		{
			get
			{
				return Color.FromArgb(0x92, 0x92, 0x76);
			}
		}

		public override Color OverflowButtonGradientMiddle
		{
			get
			{
				return Color.FromArgb(0xe2, 0xe1, 0xdb);
			}
		}

		public override Color RaftingContainerGradientBegin
		{
			get
			{
				return base.RaftingContainerGradientBegin;
			}
		}

		public override Color RaftingContainerGradientEnd
		{
			get
			{
				return base.RaftingContainerGradientEnd;
			}
		}

		public override Color SeparatorDark
		{
			get
			{
				return Color.FromArgb(0xc5, 0xc2, 0xb8);
			}
		}

		public override Color SeparatorLight
		{
			get
			{
				return Color.FromArgb(0xff, 0xff, 0xff);
			}
		}

		public override Color StatusStripGradientBegin
		{
			get
			{
				return base.StatusStripGradientBegin;
			}
		}

		public override Color StatusStripGradientEnd
		{
			get
			{
				return base.StatusStripGradientEnd;
			}
		}

		public override Color ToolStripBorder
		{
			get
			{
				return Color.FromArgb(0xa3, 0xa3, 0x7c);
			}
		}


		public override Color ToolStripContentPanelGradientBegin
		{
			get
			{
				return Color.FromArgb(250, 249, 245);
			}
		}

		public override Color ToolStripContentPanelGradientEnd
		{
			get
			{
				return Color.FromArgb(192, 192, 168);
			}
		}

		public override Color ToolStripDropDownBackground
		{
			get
			{
				return Color.FromArgb(0xfc, 0xfc, 0xf9);
			}
		}

		public override Color ToolStripGradientBegin
		{
			get
			{
				return Color.FromArgb(250, 249, 245);
			}
		}

		public override Color ToolStripGradientEnd
		{
			get
			{
				return Color.FromArgb(192, 192, 168);
			}
		}

		public override Color ToolStripGradientMiddle
		{
			get
			{
				return Color.FromArgb(235, 231, 224);
			}
		}

		public override Color ToolStripPanelGradientBegin
		{
			get
			{
				return base.ToolStripPanelGradientBegin;
			}
		}

		public override Color ToolStripPanelGradientEnd
		{
			get
			{
				return base.ToolStripPanelGradientEnd;
			}
		}
	}
}
