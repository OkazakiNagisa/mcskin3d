/*
 * Renderers - A collection of useful renderers that are used to owner draw a cell in an ObjectListView
 *
 * Author: Phillip Piper
 * Date: 27/09/2008 9:15 AM
 *
 * Change log:
 * 2010-08-24   JPP  - CheckBoxRenderer handles hot boxes and correctly vertically centers the box.
 * 2010-06-23   JPP  - Major rework of HighlightTextRenderer. Now uses TextMatchFilter directly.
 *                     Draw highlighting underneath text to improve legibility. Works with new
 *                     TextMatchFilter capabilities.
 * v2.4
 * 2009-10-30   JPP  - Plugged possible resource leak by using using() with CreateGraphics()
 * v2.3
 * 2009-09-28   JPP  - Added DescribedTaskRenderer
 * 2009-09-01   JPP  - Correctly handle an ImageRenderer's handling of an aspect that holds
 *                     the image to be displayed at Byte[].
 * 2009-08-29   JPP  - Fixed bug where some of a cell's background was not erased. 
 * 2009-08-15   JPP  - Correctly MeasureText() using the appropriate graphic context
 *                   - Handle translucent selection setting
 * v2.2.1
 * 2009-07-24   JPP  - Try to honour CanWrap setting when GDI rendering text.
 * 2009-07-11   JPP  - Correctly calculate edit rectangle for subitems of a tree view
 *                     (previously subitems were indented in the same way as the primary column)
 * v2.2
 * 2009-06-06   JPP  - Tweaked text rendering so that column 0 isn't ellipsed unnecessarily.
 * 2009-05-05   JPP  - Added Unfocused foreground and background colors 
 *                     (thanks to Christophe Hosten)
 * 2009-04-21   JPP  - Fixed off-by-1 error when calculating text widths. This caused
 *                     middle and right aligned columns to always wrap one character
 *                     when printed using ListViewPrinter (SF#2776634).
 * 2009-04-11   JPP  - Correctly renderer checkboxes when RowHeight is non-standard
 * 2009-04-06   JPP  - Allow for item indent when calculating edit rectangle
 * v2.1
 * 2009-02-24   JPP  - Work properly with ListViewPrinter again
 * 2009-01-26   JPP  - AUSTRALIA DAY (why aren't I on holidays!)
 *                   - Major overhaul of renderers. Now uses IRenderer interface.
 *                   - ImagesRenderer and FlagsRenderer<T> are now defunct.
 *                     The names are retained for backward compatibility.
 * 2009-01-23   JPP  - Align bitmap AND text according to column alignment (previously
 *                     only text was aligned and bitmap was always to the left).
 * 2009-01-21   JPP  - Changed to use TextRenderer rather than native GDI routines.
 * 2009-01-20   JPP  - Draw images directly from image list if possible. 30% faster!
 *                   - Tweaked some spacings to look more like native ListView
 *                   - Text highlight for non FullRowSelect is now the right color
 *                     when the control doesn't have focus.
 *                   - Commented out experimental animations. Still needs work.
 * 2009-01-19   JPP  - Changed to draw text using GDI routines. Looks more like
 *                     native control this way. Set UseGdiTextRendering to false to 
 *                     revert to previous behavior.
 * 2009-01-15   JPP  - Draw background correctly when control is disabled
 *                   - Render checkboxes using CheckBoxRenderer
 * v2.0.1
 * 2008-12-29   JPP  - Render text correctly when HideSelection is true.
 * 2008-12-26   JPP  - BaseRenderer now works correctly in all Views
 * 2008-12-23   JPP  - Fixed two small bugs in BarRenderer
 * v2.0
 * 2008-10-26   JPP  - Don't owner draw when in Design mode
 * 2008-09-27   JPP  - Separated from ObjectListView.cs
 * 
 * Copyright (C) 2006-2010 Phillip Piper
 * 
 * TO DO:
 * - Hit detection on renderers doesn't change the controls standard selection behavior
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip_piper@bigfoot.com.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Timer = System.Threading.Timer;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// Renderers are the mechanism used for owner drawing cells. As such, they can also handle
	/// hit detection and positioning of cell editing rectangles.
	/// </summary>
	public interface IRenderer
	{
		/// <summary>
		/// Render the whole item within an ObjectListView. This is only used in non-Details views.
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="g">A Graphics for rendering</param>
		/// <param name="itemBounds">The bounds of the item</param>
		/// <param name="rowObject">The model object to be drawn</param>
		/// <returns>Return true to indicate that the event was handled and no further processing is needed.</returns>
		bool RenderItem(DrawListViewItemEventArgs e, Graphics g, Rectangle itemBounds, Object rowObject);

		/// <summary>
		/// Render one cell within an ObjectListView when it is in Details mode.
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="g">A Graphics for rendering</param>
		/// <param name="cellBounds">The bounds of the cell</param>
		/// <param name="rowObject">The model object to be drawn</param>
		/// <returns>Return true to indicate that the event was handled and no further processing is needed.</returns>
		bool RenderSubItem(DrawListViewSubItemEventArgs e, Graphics g, Rectangle cellBounds, Object rowObject);

		/// <summary>
		/// What is under the given point?
		/// </summary>
		/// <param name="hti"></param>
		/// <param name="x">x co-ordinate</param>
		/// <param name="y">y co-ordinate</param>
		/// <remarks>This method should only alter HitTestLocation and/or UserData.</remarks>
		void HitTest(OlvListViewHitTestInfo hti, int x, int y);

		/// <summary>
		/// When the value in the given cell is to be edited, where should the edit rectangle be placed?
		/// </summary>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <returns></returns>
		Rectangle GetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex);
	}

	/// <summary>
	/// An AbstractRenderer is a do-nothing implementation of the IRenderer interface.
	/// </summary>
	[Browsable(true),
	 ToolboxItem(false)]
	public class AbstractRenderer : Component, IRenderer
	{
		#region IRenderer Members

		/// <summary>
		/// Render the whole item within an ObjectListView. This is only used in non-Details views.
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="g">A Graphics for rendering</param>
		/// <param name="itemBounds">The bounds of the item</param>
		/// <param name="rowObject">The model object to be drawn</param>
		/// <returns>Return true to indicate that the event was handled and no further processing is needed.</returns>
		public virtual bool RenderItem(DrawListViewItemEventArgs e, Graphics g, Rectangle itemBounds, object rowObject)
		{
			return true;
		}

		/// <summary>
		/// Render one cell within an ObjectListView when it is in Details mode.
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="g">A Graphics for rendering</param>
		/// <param name="cellBounds">The bounds of the cell</param>
		/// <param name="rowObject">The model object to be drawn</param>
		/// <returns>Return true to indicate that the event was handled and no further processing is needed.</returns>
		public virtual bool RenderSubItem(DrawListViewSubItemEventArgs e, Graphics g, Rectangle cellBounds, object rowObject)
		{
			return false;
		}

		/// <summary>
		/// What is under the given point?
		/// </summary>
		/// <param name="hti"></param>
		/// <param name="x">x co-ordinate</param>
		/// <param name="y">y co-ordinate</param>
		/// <remarks>This method should only alter HitTestLocation and/or UserData.</remarks>
		public virtual void HitTest(OlvListViewHitTestInfo hti, int x, int y)
		{
		}

		/// <summary>
		/// When the value in the given cell is to be edited, where should the edit rectangle be placed?
		/// </summary>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <returns></returns>
		public virtual Rectangle GetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex)
		{
			return cellBounds;
		}

		#endregion
	}

	/// <summary>
	/// This class provides compatibility for v1 RendererDelegates
	/// </summary>
	[ToolboxItem(false)]
	internal class Version1Renderer : AbstractRenderer
	{
		/// <summary>
		/// The renderer delegate that this renderer wraps
		/// </summary>
		public RenderDelegate RenderDelegate;

		public Version1Renderer(RenderDelegate renderDelegate)
		{
			RenderDelegate = renderDelegate;
		}

		public override bool RenderSubItem(DrawListViewSubItemEventArgs e, Graphics g, Rectangle cellBounds, object rowObject)
		{
			if (RenderDelegate == null)
				return base.RenderSubItem(e, g, cellBounds, rowObject);
			else
				return RenderDelegate(e, g, cellBounds, rowObject);
		}
	}

	/// <summary>
	/// A BaseRenderer provides useful base level functionality for any custom renderer.
	/// </summary>
	/// <remarks>
	/// <para>Subclasses will normally override the Render or OptionalRender method, and use the other
	/// methods as helper functions.</para>
	/// </remarks>
	[Browsable(true),
	 ToolboxItem(true)]
	public class BaseRenderer : AbstractRenderer
	{
		#region Configuration Properties

		private bool canWrap;
		private int spacing = 1;
		private bool useGdiTextRendering = true;

		/// <summary>
		/// Can the renderer wrap lines that do not fit completely within the cell?
		/// </summary>
		/// <remarks>Wrapping text doesn't work with the GDI renderer.</remarks>
		[Category("Appearance"),
		 Description("Can the renderer wrap text that does not fit completely within the cell"),
		 DefaultValue(false)]
		public bool CanWrap
		{
			get { return canWrap; }
			set
			{
				canWrap = value;
				if (canWrap)
					UseGdiTextRendering = false;
			}
		}

		/// <summary>
		/// Gets or sets the image list from which keyed images will be fetched
		/// </summary>
		[Category("Appearance"), Description("The image list from which keyed images will be fetched for drawing."),
		 DefaultValue(null)]
		public ImageList ImageList { get; set; }

		/// <summary>
		/// When rendering multiple images, how many pixels should be between each image?
		/// </summary>
		[Category("Appearance"),
		 Description("When rendering multiple images, how many pixels should be between each image?"),
		 DefaultValue(1)]
		public int Spacing
		{
			get { return spacing; }
			set { spacing = value; }
		}

		/// <summary>
		/// Should text be rendered using GDI routines? This makes the text look more
		/// like a native List view control.
		/// </summary>
		[Category("Appearance"),
		 Description("Should text be rendered using GDI routines?"),
		 DefaultValue(true)]
		public bool UseGdiTextRendering
		{
			get
			{
				if (IsPrinting)
					return false; // Can't use GDI routines on a GDI+ printer context
				else
					return useGdiTextRendering;
			}
			set { useGdiTextRendering = value; }
		}

		#endregion

		#region State Properties

		private Object aspect;
		private OLVColumn column;
		private Font font;
		private OLVListSubItem listSubItem;
		private Object rowObject;
		private Brush textBrush;

		/// <summary>
		/// Get or set the aspect of the model object that this renderer should draw
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Object Aspect
		{
			get
			{
				if (aspect == null)
					aspect = column.GetValue(rowObject);
				return aspect;
			}
			set { aspect = value; }
		}

		/// <summary>
		/// What are the bounds of the cell that is being drawn?
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Rectangle Bounds { get; set; }

		/// <summary>
		/// Get or set the OLVColumn that this renderer will draw
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OLVColumn Column
		{
			get { return column; }
			set { column = value; }
		}

		/// <summary>
		/// Get/set the event that caused this renderer to be called
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DrawListViewItemEventArgs DrawItemEvent { get; set; }

		/// <summary>
		/// Get/set the event that caused this renderer to be called
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DrawListViewSubItemEventArgs Event { get; set; }

		/// <summary>
		/// Return the font to be used for text in this cell
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Font Font
		{
			get
			{
				if (font != null || ListItem == null)
					return font;

				if (SubItem == null || ListItem.UseItemStyleForSubItems)
					return ListItem.Font;
				else
					return SubItem.Font;
			}
			set { font = value; }
		}

		/// <summary>
		/// Gets the image list from which keyed images will be fetched
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ImageList ImageListOrDefault
		{
			get { return ImageList ?? ListView.BaseSmallImageList; }
		}

		/// <summary>
		/// Should this renderer fill in the background before drawing?
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsDrawBackground
		{
			get { return !IsPrinting; }
		}

		/// <summary>
		/// Cache whether or not our item is selected
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsItemSelected { get; set; }

		/// <summary>
		/// Is this renderer being used on a printer context?
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPrinting { get; set; }

		/// <summary>
		/// Get or set the listitem that this renderer will be drawing
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OLVListItem ListItem { get; set; }

		/// <summary>
		/// Get/set the listview for which the drawing is to be done
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ObjectListView ListView { get; set; }

		/// <summary>
		/// Get the specialized OLVSubItem that this renderer is drawing
		/// </summary>
		/// <remarks>This returns null for column 0.</remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OLVListSubItem OLVSubItem
		{
			get { return listSubItem; }
		}

		/// <summary>
		/// Get or set the model object that this renderer should draw
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Object RowObject
		{
			get { return rowObject; }
			set { rowObject = value; }
		}

		/// <summary>
		/// Get or set the list subitem that this renderer will be drawing
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OLVListSubItem SubItem
		{
			get { return listSubItem; }
			set { listSubItem = value; }
		}

		/// <summary>
		/// The brush that will be used to paint the text
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Brush TextBrush
		{
			get
			{
				if (textBrush == null)
					return new SolidBrush(GetForegroundColor());
				else
					return textBrush;
			}
			set { textBrush = value; }
		}

		private void ClearState()
		{
			Event = null;
			DrawItemEvent = null;
			Aspect = null;
			Font = null;
			TextBrush = null;
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Align the second rectangle with the first rectangle,
		/// according to the alignment of the column
		/// </summary>
		/// <param name="outer">The cell's bounds</param>
		/// <param name="inner">The rectangle to be aligned within the bounds</param>
		/// <returns>An aligned rectangle</returns>
		protected virtual Rectangle AlignRectangle(Rectangle outer, Rectangle inner)
		{
			var r = new Rectangle(outer.Location, inner.Size);

			// Centre horizontally depending on the column alignment
			if (inner.Width < outer.Width)
			{
				switch (Column.TextAlign)
				{
					case HorizontalAlignment.Left:
						r.X = outer.Left;
						break;
					case HorizontalAlignment.Center:
						r.X = outer.Left + ((outer.Width - inner.Width) / 2);
						break;
					case HorizontalAlignment.Right:
						r.X = outer.Right - inner.Width - 1;
						break;
				}
			}
			// Centre vertically too
			if (inner.Height < outer.Height)
				r.Y = outer.Top + ((outer.Height - inner.Height) / 2);

			return r;
		}

		/// <summary>
		/// Calculate the space that our rendering will occupy and then align that space
		/// with the given rectangle, according to the Column alignment
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		protected virtual Rectangle CalculateAlignedRectangle(Graphics g, Rectangle r)
		{
			if (Column.TextAlign == HorizontalAlignment.Left)
				return r;

			int width = CalculateCheckBoxWidth(g);
			width += CalculateImageWidth(g, GetImageSelector());
			width += CalculateTextWidth(g, GetText());

			// If the combined width is greater than the whole cell, 
			// we just use the cell itself
			if (width >= r.Width)
				return r;

			return AlignRectangle(r, new Rectangle(0, 0, width, r.Height));
		}

		/// <summary>
		/// How much space will the check box for this cell occupy?
		/// </summary>
		/// <remarks>Only column 0 can have check boxes. Sub item checkboxes are
		/// treated as images</remarks>
		/// <param name="g"></param>
		/// <returns></returns>
		protected virtual int CalculateCheckBoxWidth(Graphics g)
		{
			if (ListView.CheckBoxes && Column.Index == 0)
				return CheckBoxRenderer.GetGlyphSize(g, CheckBoxState.UncheckedNormal).Width + 6;
			else
				return 0;
		}

		/// <summary>
		/// How much horizontal space will the image of this cell occupy?
		/// </summary>
		/// <param name="g"></param>
		/// <param name="imageSelector"></param>
		/// <returns></returns>
		protected virtual int CalculateImageWidth(Graphics g, object imageSelector)
		{
			if (imageSelector == null || imageSelector == DBNull.Value)
				return 0;

			// Draw from the image list (most common case)
			ImageList il = ImageListOrDefault;
			if (il != null)
			{
				int selectorAsInt = -1;

				if (imageSelector is Int32)
					selectorAsInt = (Int32) imageSelector;
				else
				{
					var selectorAsString = imageSelector as String;
					if (selectorAsString != null)
						selectorAsInt = il.Images.IndexOfKey(selectorAsString);
				}
				if (selectorAsInt >= 0)
					return il.ImageSize.Width;
			}

			// Is the selector actually an image?
			var image = imageSelector as Image;
			if (image != null)
				return image.Width;

			return 0;
		}

		/// <summary>
		/// How much horizontal space will the text of this cell occupy?
		/// </summary>
		/// <param name="g"></param>
		/// <param name="txt"></param>
		/// <returns></returns>
		protected virtual int CalculateTextWidth(Graphics g, string txt)
		{
			if (String.IsNullOrEmpty(txt))
				return 0;

			if (UseGdiTextRendering)
			{
				var proposedSize = new Size(int.MaxValue, int.MaxValue);
				return
					TextRenderer.MeasureText(g, txt, Font, proposedSize, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix).Width;
			}
			else
			{
				using (var fmt = new StringFormat())
				{
					fmt.Trimming = StringTrimming.EllipsisCharacter;
					return 1 + (int) g.MeasureString(txt, Font, int.MaxValue, fmt).Width;
				}
			}
		}

		/// <summary>
		/// Return the Color that is the background color for this item's cell
		/// </summary>
		/// <returns>The background color of the subitem</returns>
		protected virtual Color GetBackgroundColor()
		{
			if (!ListView.Enabled)
				return SystemColors.Control;
			if (IsItemSelected && !ListView.UseTranslucentSelection && ListView.FullRowSelect)
			{
				if (ListView.Focused)
					return ListView.HighlightBackgroundColorOrDefault;
				else if (!ListView.HideSelection)
					return ListView.UnfocusedHighlightBackgroundColorOrDefault;
			}
			if (SubItem == null || ListItem.UseItemStyleForSubItems)
				return ListItem.BackColor;
			else
				return SubItem.BackColor;
		}

		/// <summary>
		/// Return the color to be used for text in this cell
		/// </summary>
		/// <returns>The text color of the subitem</returns>
		protected virtual Color GetForegroundColor()
		{
			if (IsItemSelected && !ListView.UseTranslucentSelection &&
			    (Column.Index == 0 || ListView.FullRowSelect))
			{
				if (ListView.Focused)
					return ListView.HighlightForegroundColorOrDefault;
				else if (!ListView.HideSelection)
					return ListView.UnfocusedHighlightForegroundColorOrDefault;
			}
			if (SubItem == null || ListItem.UseItemStyleForSubItems)
				return ListItem.ForeColor;
			else
				return SubItem.ForeColor;
		}

		/// <summary>
		/// Return the image that should be drawn against this subitem
		/// </summary>
		/// <returns>An Image or null if no image should be drawn.</returns>
		protected virtual Image GetImage()
		{
			return GetImage(GetImageSelector());
		}

		/// <summary>
		/// Return the actual image that should be drawn when keyed by the given image selector.
		/// An image selector can be: <list type="bullet">
		/// <item><description>an int, giving the index into the image list</description></item>
		/// <item><description>a string, giving the image key into the image list</description></item>
		/// <item><description>an Image, being the image itself</description></item>
		/// </list>
		/// </summary>
		/// <param name="imageSelector">The value that indicates the image to be used</param>
		/// <returns>An Image or null</returns>
		protected virtual Image GetImage(Object imageSelector)
		{
			if (imageSelector == null || imageSelector == DBNull.Value)
				return null;

			ImageList il = ImageListOrDefault;
			if (il != null)
			{
				if (imageSelector is Int32)
				{
					var index = (Int32) imageSelector;
					if (index < 0 || index >= il.Images.Count)
						return null;
					else
						return il.Images[index];
				}

				var str = imageSelector as String;
				if (str != null)
				{
					if (il.Images.ContainsKey(str))
						return il.Images[str];
					else
						return null;
				}
			}

			return imageSelector as Image;
		}

		/// <summary>
		/// </summary>
		protected virtual Object GetImageSelector()
		{
			if (Column.Index == 0)
				return ListItem.ImageSelector;
			else
				return OLVSubItem.ImageSelector;
		}

		/// <summary>
		/// Return the string that should be drawn within this
		/// </summary>
		/// <returns></returns>
		protected virtual string GetText()
		{
			if (SubItem == null)
				return ListItem.Text;
			else
				return SubItem.Text;
		}

		/// <summary>
		/// Return the Color that is the background color for this item's text
		/// </summary>
		/// <returns>The background color of the subitem's text</returns>
		protected virtual Color GetTextBackgroundColor()
		{
			//TODO: Refactor with GetBackgroundColor() - they are almost identical
			if (IsItemSelected && !ListView.UseTranslucentSelection
			    && (Column.Index == 0 || ListView.FullRowSelect))
			{
				if (ListView.Focused)
					return ListView.HighlightBackgroundColorOrDefault;
				else if (!ListView.HideSelection)
					return ListView.UnfocusedHighlightBackgroundColorOrDefault;
			}

			if (SubItem == null || ListItem.UseItemStyleForSubItems)
				return ListItem.BackColor;
			else
				return SubItem.BackColor;
		}

		#endregion

		#region IRenderer members

		/// <summary>
		/// Render the whole item in a non-details view.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="g"></param>
		/// <param name="itemBounds"></param>
		/// <param name="rowObject"></param>
		/// <returns></returns>
		public override bool RenderItem(DrawListViewItemEventArgs e, Graphics g, Rectangle itemBounds, object rowObject)
		{
			ClearState();

			DrawItemEvent = e;
			ListItem = (OLVListItem) e.Item;
			SubItem = null;
			ListView = (ObjectListView) ListItem.ListView;
			Column = ListView.GetColumn(0);
			RowObject = rowObject;
			Bounds = itemBounds;
			IsItemSelected = ListItem.Selected;

			return OptionalRender(g, itemBounds);
		}

		/// <summary>
		/// Render one cell
		/// </summary>
		/// <param name="e"></param>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <param name="rowObject"></param>
		/// <returns></returns>
		public override bool RenderSubItem(DrawListViewSubItemEventArgs e, Graphics g, Rectangle cellBounds, object rowObject)
		{
			ClearState();

			Event = e;
			ListItem = (OLVListItem) e.Item;
			SubItem = (OLVListSubItem) e.SubItem;
			ListView = (ObjectListView) ListItem.ListView;
			Column = (OLVColumn) e.Header;
			RowObject = rowObject;
			Bounds = cellBounds;
			IsItemSelected = ListItem.Selected;

			return OptionalRender(g, cellBounds);
		}

		/// <summary>
		/// Calculate which part of this cell was hit
		/// </summary>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public override void HitTest(OlvListViewHitTestInfo hti, int x, int y)
		{
			ClearState();

			ListView = hti.ListView;
			ListItem = hti.Item;
			SubItem = hti.SubItem;
			Column = hti.Column;
			RowObject = hti.RowObject;
			IsItemSelected = ListItem.Selected;
			if (SubItem == null)
				Bounds = ListItem.Bounds;
			else
				Bounds = ListItem.GetSubItemBounds(Column.Index);
			//this.Bounds = this.ListView.CalculateCellBounds(this.ListItem, this.Column.Index);

			using (Graphics g = ListView.CreateGraphics()) HandleHitTest(g, hti, x, y);
		}

		/// <summary>
		/// Calculate the edit rectangle
		/// </summary>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <returns></returns>
		public override Rectangle GetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex)
		{
			ClearState();

			ListView = (ObjectListView) item.ListView;
			ListItem = item;
			SubItem = item.GetSubItem(subItemIndex);
			Column = ListView.GetColumn(subItemIndex);
			RowObject = item.RowObject;
			IsItemSelected = ListItem.Selected;
			Bounds = cellBounds;

			return HandleGetEditRectangle(g, cellBounds, item, subItemIndex);
		}

		#endregion

		#region IRenderer implementation

		// Subclasses will probably want to override these methods rather than the IRenderer
		// interface methods.

		/// <summary>
		/// Draw our data into the given rectangle using the given graphics context.
		/// </summary>
		/// <remarks>
		/// <para>Subclasses should override this method.</para></remarks>
		/// <param name="g">The graphics context that should be used for drawing</param>
		/// <param name="r">The bounds of the subitem cell</param>
		/// <returns>Returns whether the renderering has already taken place.
		/// If this returns false, the default processing will take over.
		/// </returns>
		public virtual bool OptionalRender(Graphics g, Rectangle r)
		{
			if (ListView.View == View.Details)
			{
				Render(g, r);
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Draw our data into the given rectangle using the given graphics context.
		/// </summary>
		/// <remarks>
		/// <para>Subclasses should override this method if they never want
		/// to fall back on the default processing</para></remarks>
		/// <param name="g">The graphics context that should be used for drawing</param>
		/// <param name="r">The bounds of the subitem cell</param>
		public virtual void Render(Graphics g, Rectangle r)
		{
			StandardRender(g, r);
		}

		/// <summary>
		/// Do the actual work of hit testing. Subclasses should override this rather than HitTest()
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected virtual void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
		{
			Rectangle r = CalculateAlignedRectangle(g, Bounds);
			StandardHitTest(g, hti, r, x, y);
		}

		/// <summary>
		/// Handle a HitTest request after all state information has been initialized
		/// </summary>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <returns></returns>
		protected virtual Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item,
		                                                   int subItemIndex)
		{
			// MAINTAINER NOTE: This type testing is wrong (design-wise). The base class should return cell bounds,
			// and a more specialized class should return StandardGetEditRectangle(). But BaseRenderer is used directly
			// to draw most normal cells, as well as being directly subclassed for user implemented renderers. And this
			// method needs to return different bounds in each of those cases. We should have a StandardRenderer and make
			// BaseRenderer into an ABC -- but that would break too much existing code. And so we have this hack :(

			// If we are a standard renderer, return the position of the text, otherwise, use the whole cell.
			if (GetType() == typeof (BaseRenderer))
				return StandardGetEditRectangle(g, cellBounds);
			else
				return cellBounds;
		}

		#endregion

		#region Standard IRenderer implementations

		/// <summary>
		/// Draw the standard "[checkbox] [image] [text]" cell after the state properties have been initialized.
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		protected void StandardRender(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);

			// Adjust the first columns rectangle to match the padding used by the native mode of the ListView
			if (Column.Index == 0)
			{
				r.X += 3;
				r.Width -= 1;
			}
			DrawAlignedImageAndText(g, r);
		}

		/// <summary>
		/// Perform normal hit testing relative to the given bounds
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hti"></param>
		/// <param name="bounds"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected void StandardHitTest(Graphics g, OlvListViewHitTestInfo hti, Rectangle bounds, int x, int y)
		{
			Rectangle r = bounds;

			// Did they hit a check box?
			int width = CalculateCheckBoxWidth(g);
			Rectangle r2 = r;
			r2.Width = width;
			if (r2.Contains(x, y))
			{
				hti.HitTestLocation = HitTestLocation.CheckBox;
				return;
			}

			// Did they hit the image? If they hit the image of a 
			// non-primary column that has a checkbox, it counts as a 
			// checkbox hit
			r.X += width;
			r.Width -= width;
			width = CalculateImageWidth(g, GetImageSelector());
			r2 = r;
			r2.Width = width;
			if (r2.Contains(x, y))
			{
				if (Column.Index > 0 && Column.CheckBoxes)
					hti.HitTestLocation = HitTestLocation.CheckBox;
				else
					hti.HitTestLocation = HitTestLocation.Image;
				return;
			}

			// Did they hit the text?
			r.X += width;
			r.Width -= width;
			width = CalculateTextWidth(g, GetText());
			r2 = r;
			r2.Width = width;
			if (r2.Contains(x, y))
			{
				hti.HitTestLocation = HitTestLocation.Text;
				return;
			}

			hti.HitTestLocation = HitTestLocation.InCell;
		}

		/// <summary>
		/// This method calculates the bounds of the text within a standard layout
		/// (i.e. optional checkbox, optional image, text)
		/// </summary>
		/// <remarks>This method only works correctly if the state of the renderer
		/// has been fully initialized (see BaseRenderer.GetEditRectangle)</remarks>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <returns></returns>
		protected Rectangle StandardGetEditRectangle(Graphics g, Rectangle cellBounds)
		{
			Rectangle r = CalculateAlignedRectangle(g, cellBounds);

			int width = CalculateCheckBoxWidth(g);
			width += CalculateImageWidth(g, GetImageSelector());

			// Indent the primary column by the required amount
			if (Column.Index == 0 && ListItem.IndentCount > 0)
			{
				int indentWidth = ListView.SmallImageSize.Width;
				width += (indentWidth * ListItem.IndentCount);
			}

			// If there wasn't either a check box or an image, just use the whole cell
			if (width == 0)
				return cellBounds;

			// Take the check box and the image out of the rectangle, but ensure that
			// there is minimum width to the editor
			r.X += width;
			r.Width = Math.Max(r.Width - width, 40);

			return r;
		}

		#endregion

		#region Drawing routines

		/// <summary>
		/// Gets the StringFormat needed when drawing text using GDI+
		/// </summary>
		protected virtual StringFormat StringFormatForGdiPlus
		{
			get
			{
				var fmt = new StringFormat();
				fmt.LineAlignment = StringAlignment.Center;
				fmt.Trimming = StringTrimming.EllipsisCharacter;
				fmt.Alignment = Column.TextStringAlign;
				if (!CanWrap)
					fmt.FormatFlags = StringFormatFlags.NoWrap;
				return fmt;
			}
		}

		/// <summary>
		/// Draw the given image aligned horizontally within the column.
		/// </summary>
		/// <remarks>
		/// Over tall images are scaled to fit. Over-wide images are
		/// truncated. This is by design!
		/// </remarks>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		/// <param name="image">The image to be drawn</param>
		protected virtual void DrawAlignedImage(Graphics g, Rectangle r, Image image)
		{
			if (image == null)
				return;

			// By default, the image goes in the top left of the rectangle
			var imageBounds = new Rectangle(r.Location, image.Size);

			// If the image is too tall to be drawn in the space provided, proportionally scale it down.
			// Too wide images are not scaled.
			if (image.Height > r.Height)
			{
				float scaleRatio = r.Height / (float) image.Height;
				imageBounds.Width = (int) (image.Width * scaleRatio);
				imageBounds.Height = r.Height - 1;
			}

			// Align and draw our (possibly scaled) image
			g.DrawImage(image, AlignRectangle(r, imageBounds));
		}

		/// <summary>
		/// Draw our subitems image and text
		/// </summary>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		protected virtual void DrawAlignedImageAndText(Graphics g, Rectangle r)
		{
			DrawImageAndText(g, CalculateAlignedRectangle(g, r));
		}

		/// <summary>
		/// Fill in the background of this cell
		/// </summary>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		protected virtual void DrawBackground(Graphics g, Rectangle r)
		{
			if (!IsDrawBackground)
				return;

			Color backgroundColor = GetBackgroundColor();

			using (Brush brush = new SolidBrush(backgroundColor))
				g.FillRectangle(brush, r.X - 1, r.Y, r.Width + 2, r.Height + 1);
		}

		/// <summary>
		/// Draw the check box of this row
		/// </summary>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		protected virtual int DrawCheckBox(Graphics g, Rectangle r)
		{
			int imageIndex = ListItem.StateImageIndex;

			if (IsPrinting)
			{
				if (ListView.StateImageList == null || imageIndex < 0)
					return 0;
				else
					return DrawImage(g, r, ListView.StateImageList.Images[imageIndex]) + 4;
			}

			CheckBoxState boxState = CheckBoxState.UncheckedNormal;
			int switchValue = (imageIndex << 4); // + (this.IsItemHot ? 1 : 0);
			switch (switchValue)
			{
				case 0x00:
					boxState = CheckBoxState.UncheckedNormal;
					break;
				case 0x01:
					boxState = CheckBoxState.UncheckedHot;
					break;
				case 0x10:
					boxState = CheckBoxState.CheckedNormal;
					break;
				case 0x11:
					boxState = CheckBoxState.CheckedHot;
					break;
				case 0x20:
					boxState = CheckBoxState.MixedNormal;
					break;
				case 0x21:
					boxState = CheckBoxState.MixedHot;
					break;
			}

			// The odd constants are to match checkbox placement in native mode (on XP at least)
			CheckBoxRenderer.DrawCheckBox(g, new Point(r.X + 3, r.Y + (r.Height / 2) - 6), boxState);
			return CheckBoxRenderer.GetGlyphSize(g, boxState).Width + 6;
		}

		/// <summary>
		/// Draw the given text and optional image in the "normal" fashion
		/// </summary>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		/// <param name="imageSelector">The optional image to be drawn</param>
		protected virtual int DrawImage(Graphics g, Rectangle r, Object imageSelector)
		{
			if (imageSelector == null || imageSelector == DBNull.Value)
				return 0;

			// Draw from the image list (most common case)
			ImageList il = ListView.BaseSmallImageList;
			if (il != null)
			{
				int selectorAsInt = -1;

				if (imageSelector is Int32)
					selectorAsInt = (Int32) imageSelector;
				else
				{
					var selectorAsString = imageSelector as String;
					if (selectorAsString != null)
						selectorAsInt = il.Images.IndexOfKey(selectorAsString);
				}
				if (selectorAsInt >= 0)
				{
					if (IsPrinting)
					{
						// For some reason, printing from an image list doesn't work onto a printer context
						// So get the image from the list and fall through to the "print an image" case
						imageSelector = il.Images[selectorAsInt];
					}
					else
					{
						// If we are not printing, it's probable that the given Graphics object is double buffered using a BufferedGraphics object.
						// But the ImageList.Draw method doesn't honor the Translation matrix that's probably in effect on the buffered
						// graphics. So we have to calculate our drawing rectangle, relative to the cells natural boundaries.
						// This effectively simulates the Translation matrix.
						var r2 = new Rectangle(r.X - Bounds.X, r.Y - Bounds.Y, r.Width, r.Height);
						il.Draw(g, r2.Location, selectorAsInt);

						// Use this call instead of the above if you want to images to appear blended when selected
						//NativeMethods.DrawImageList(g, il, selectorAsInt, r2.X, r2.Y, this.IsItemSelected);
						return il.ImageSize.Width;
					}
				}
			}

			// Is the selector actually an image?
			var image = imageSelector as Image;
			if (image != null)
			{
				int top = r.Y;
				if (image.Size.Height < r.Height)
					top += ((r.Height - image.Size.Height) / 2);

				g.DrawImageUnscaled(image, r.X, top);
				return image.Width;
			}

			return 0;
		}

		/// <summary>
		/// Draw our subitems image and text
		/// </summary>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		protected virtual void DrawImageAndText(Graphics g, Rectangle r)
		{
			int offset = 0;
			if (ListView.CheckBoxes && Column.Index == 0)
			{
				offset = DrawCheckBox(g, r);
				r.X += offset;
				r.Width -= offset;
			}

			offset = DrawImage(g, r, GetImageSelector());
			r.X += offset;
			r.Width -= offset;

			DrawText(g, r, GetText());
		}

		/// <summary>
		/// Draw the given collection of image selectors
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="imageSelectors"></param>
		protected virtual int DrawImages(Graphics g, Rectangle r, ICollection imageSelectors)
		{
			// Collect the non-null images
			var images = new List<Image>();
			foreach (Object selector in imageSelectors)
			{
				Image image = GetImage(selector);
				if (image != null)
					images.Add(image);
			}

			// Figure out how much space they will occupy
			int width = 0;
			int height = 0;
			foreach (Image image in images)
			{
				width += (image.Width + Spacing);
				height = Math.Max(height, image.Height);
			}

			// Align the collection of images within the cell
			Rectangle r2 = AlignRectangle(r, new Rectangle(0, 0, width, height));

			// Finally, draw all the images in their correct location
			Point pt = r2.Location;
			foreach (Image image in images)
			{
				g.DrawImage(image, pt);
				pt.X += (image.Width + Spacing);
			}

			// Return the width that the images occupy
			return width;
		}

		/// <summary>
		/// Draw the given text and optional image in the "normal" fashion
		/// </summary>
		/// <param name="g">Graphics context to use for drawing</param>
		/// <param name="r">Bounds of the cell</param>
		/// <param name="txt">The string to be drawn</param>
		protected virtual void DrawText(Graphics g, Rectangle r, String txt)
		{
			if (String.IsNullOrEmpty(txt))
				return;

			if (UseGdiTextRendering)
				DrawTextGdi(g, r, txt);
			else
				DrawTextGdiPlus(g, r, txt);
		}

		/// <summary>
		/// Print the given text in the given rectangle using only GDI routines
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="txt"></param>
		/// <remarks>
		/// The native list control uses GDI routines to do its drawing, so using them
		/// here makes the owner drawn mode looks more natural.
		/// <para>This method doesn't honour the CanWrap setting on the renderer. All
		/// text is single line</para>
		/// </remarks>
		protected virtual void DrawTextGdi(Graphics g, Rectangle r, String txt)
		{
			Color backColor = Color.Transparent;
			if (IsDrawBackground && IsItemSelected && Column.Index == 0 && !ListView.FullRowSelect)
				backColor = GetTextBackgroundColor();

			TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
			                        TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsTranslateTransform;

			// BUG: Setting or not setting SingleLine doesn't make any difference -- it is always single line.
			if (!CanWrap)
				flags |= TextFormatFlags.SingleLine;
			TextRenderer.DrawText(g, txt, Font, r, GetForegroundColor(), backColor, flags);
		}

		/// <summary>
		/// Print the given text in the given rectangle using normal GDI+ .NET methods
		/// </summary>
		/// <remarks>Printing to a printer dc has to be done using this method.</remarks>
		protected virtual void DrawTextGdiPlus(Graphics g, Rectangle r, String txt)
		{
			using (StringFormat fmt = StringFormatForGdiPlus)
			{
				// Draw the background of the text as selected, if it's the primary column
				// and it's selected and it's not in FullRowSelect mode.
				Font f = Font;
				if (IsDrawBackground && IsItemSelected && Column.Index == 0 && !ListView.FullRowSelect)
				{
					SizeF size = g.MeasureString(txt, f, r.Width, fmt);
					Rectangle r2 = r;
					r2.Width = (int) size.Width + 1;
					using (Brush brush = new SolidBrush(ListView.HighlightBackgroundColorOrDefault)) g.FillRectangle(brush, r2);
				}
				RectangleF rf = r;
				g.DrawString(txt, f, TextBrush, rf, fmt);
			}

			// We should put a focus rectange around the column 0 text if it's selected --
			// but we don't because:
			// - I really dislike this UI convention
			// - we are using buffered graphics, so the DrawFocusRecatangle method of the event doesn't work

			//if (this.Column.Index == 0) {
			//    Size size = TextRenderer.MeasureText(this.SubItem.Text, this.ListView.ListFont);
			//    if (r.Width > size.Width)
			//        r.Width = size.Width;
			//    this.Event.DrawFocusRectangle(r);
			//}
		}

		#endregion
	}


	/// <summary>
	/// This renderer highlights substrings that match a given text filter. 
	/// </summary>
	public class HighlightTextRenderer : BaseRenderer
	{
		#region Life and death

		/// <summary>
		/// Create a HighlightTextRenderer
		/// </summary>
		public HighlightTextRenderer()
		{
			FramePen = Pens.DarkGreen;
			FillBrush = Brushes.Yellow;
		}

		/// <summary>
		/// Create a HighlightTextRenderer
		/// </summary>
		/// <param name="filter"></param>
		public HighlightTextRenderer(TextMatchFilter filter)
			: this()
		{
			Filter = filter;
		}

		/// <summary>
		/// Create a HighlightTextRenderer
		/// </summary>
		/// <param name="text"></param>
		[Obsolete("Use HighlightTextRenderer(TextMatchFilter) instead", true)]
		public HighlightTextRenderer(string text)
		{
		}

		#endregion

		#region Configuration properties

		private float cornerRoundness = 3.0f;
		private bool useRoundedRectangle = true;

		/// <summary>
		/// Gets or set how rounded will be the corners of the text match frame
		/// </summary>
		[Category("Appearance"),
		 DefaultValue(3.0f),
		 Description("How rounded will be the corners of the text match frame?")]
		public float CornerRoundness
		{
			get { return cornerRoundness; }
			set { cornerRoundness = value; }
		}

		/// <summary>
		/// Gets or set the brush will be used to paint behind the matched substrings.
		/// Set this to null to not fill the frame.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Brush FillBrush { get; set; }

		/// <summary>
		/// Gets or sets the filter that is filtering the ObjectListView and for
		/// which this renderer should highlight text
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextMatchFilter Filter { get; set; }

		/// <summary>
		/// Gets or set the pen will be used to frame the matched substrings.
		/// Set this to null to not draw a frame.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Pen FramePen { get; set; }

		/// <summary>
		/// Gets or sets whether the frame around a text match will have rounded corners
		/// </summary>
		[Category("Appearance"),
		 DefaultValue(true),
		 Description("Will the frame around a text match will have rounded corners?")]
		public bool UseRoundedRectangle
		{
			get { return useRoundedRectangle; }
			set { useRoundedRectangle = value; }
		}

		#endregion

		#region Compatibility properties

		/// <summary>
		/// Gets or set the text that will be highlighted
		/// </summary>
		[Obsolete("Set the Filter directly rather than just the text", true)]
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string TextToHighlight
		{
			get { return String.Empty; }
			set { }
		}

		/// <summary>
		/// Gets or sets the manner in which substring will be compared.
		/// </summary>
		/// <remarks>
		/// Use this to control if substring matches are case sensitive or insensitive.</remarks>
		[Obsolete("Set the Filter directly rather than just this setting", true)]
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public StringComparison StringComparison
		{
			get { return StringComparison.CurrentCultureIgnoreCase; }
			set { }
		}

		#endregion

		#region Rendering

		// This class has two implement two highlighting schemes: one for GDI, another for GDI+.
		// Naturally, GDI+ makes the task easier, but we have to provide something for GDI
		// since that it is what is normally used.

		/// <summary>
		/// Draw text using GDI
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="txt"></param>
		protected override void DrawTextGdi(Graphics g, Rectangle r, string txt)
		{
			if (ShouldDrawHighlighting)
				DrawGdiTextHighlighting(g, r, txt);

			base.DrawTextGdi(g, r, txt);
		}

		/// <summary>
		/// Draw the highlighted text using GDI
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="txt"></param>
		protected virtual void DrawGdiTextHighlighting(Graphics g, Rectangle r, string txt)
		{
			TextFormatFlags flags = TextFormatFlags.NoPrefix |
			                        TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsTranslateTransform;

			// TextRenderer puts horizontal padding around the strings, so we need to take
			// that into account when measuring strings
			int paddingAdjustment = 6;

			// Cache the font
			Font f = Font;

			foreach (CharacterRange range in Filter.FindAllMatchedRanges(txt))
			{
				// Measure the text that comes before our substring
				Size precedingTextSize = Size.Empty;
				if (range.First > 0)
				{
					string precedingText = txt.Substring(0, range.First);
					precedingTextSize = TextRenderer.MeasureText(g, precedingText, f, r.Size, flags);
					precedingTextSize.Width -= paddingAdjustment;
				}

				// Measure the length of our substring (may be different each time due to case differences)
				string highlightText = txt.Substring(range.First, range.Length);
				Size textToHighlightSize = TextRenderer.MeasureText(g, highlightText, f, r.Size, flags);
				textToHighlightSize.Width -= paddingAdjustment;

				// Draw a filled frame around our substring
				DrawSubstringFrame(g, r.X + precedingTextSize.Width + 1, r.Top,
				                   textToHighlightSize.Width, r.Height - 2);
			}
		}

		/// <summary>
		/// Draw an indication around the given frame that shows a text match
		/// </summary>
		/// <param name="g"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		protected virtual void DrawSubstringFrame(Graphics g, float x, float y, float width, float height)
		{
			if (UseRoundedRectangle)
			{
				GraphicsPath path = GetRoundedRect(x, y, width, height, 3.0f);
				if (FillBrush != null)
					g.FillPath(FillBrush, path);
				if (FramePen != null)
					g.DrawPath(FramePen, path);
			}
			else
			{
				if (FillBrush != null)
					g.FillRectangle(FillBrush, x, y, width, height);
				if (FramePen != null)
					g.DrawRectangle(FramePen, x, y, width, height);
			}
		}

		/// <summary>
		/// Draw the text using GDI+
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="txt"></param>
		protected override void DrawTextGdiPlus(Graphics g, Rectangle r, string txt)
		{
			if (ShouldDrawHighlighting)
				DrawGdiPlusTextHighlighting(g, r, txt);

			base.DrawTextGdiPlus(g, r, txt);
		}

		/// <summary>
		/// Draw the highlighted text using GDI+
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="txt"></param>
		protected virtual void DrawGdiPlusTextHighlighting(Graphics g, Rectangle r, string txt)
		{
			// Find the substrings we want to highlight
			var ranges = new List<CharacterRange>(Filter.FindAllMatchedRanges(txt));

			if (ranges.Count == 0)
				return;

			using (StringFormat fmt = StringFormatForGdiPlus)
			{
				RectangleF rf = r;
				fmt.SetMeasurableCharacterRanges(ranges.ToArray());
				Region[] stringRegions = g.MeasureCharacterRanges(txt, Font, rf, fmt);

				foreach (Region region in stringRegions)
				{
					RectangleF bounds = region.GetBounds(g);
					DrawSubstringFrame(g, bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height);
				}
			}
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Gets whether the renderer should actually draw highlighting
		/// </summary>
		protected bool ShouldDrawHighlighting
		{
			get { return Column.Searchable && Filter != null && Filter.HasComponents; }
		}

		/// <summary>
		/// Return a GraphicPath that is a round cornered rectangle
		/// </summary>
		/// <returns>A round cornered rectagle path</returns>
		/// <remarks>If I could rely on people using C# 3.0+, this should be
		/// an extension method of GraphicsPath.</remarks>        
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="diameter"></param>
		protected GraphicsPath GetRoundedRect(float x, float y, float width, float height, float diameter)
		{
			return GetRoundedRect(new RectangleF(x, y, width, height), diameter);
		}

		/// <summary>
		/// Return a GraphicPath that is a round cornered rectangle
		/// </summary>
		/// <param name="rect">The rectangle</param>
		/// <param name="diameter">The diameter of the corners</param>
		/// <returns>A round cornered rectagle path</returns>
		/// <remarks>If I could rely on people using C# 3.0+, this should be
		/// an extension method of GraphicsPath.</remarks>
		protected GraphicsPath GetRoundedRect(RectangleF rect, float diameter)
		{
			var path = new GraphicsPath();

			if (diameter > 0)
			{
				var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
				path.AddArc(arc, 180, 90);
				arc.X = rect.Right - diameter;
				path.AddArc(arc, 270, 90);
				arc.Y = rect.Bottom - diameter;
				path.AddArc(arc, 0, 90);
				arc.X = rect.Left;
				path.AddArc(arc, 90, 90);
				path.CloseFigure();
			}
			else path.AddRectangle(rect);

			return path;
		}

		#endregion
	}

	/// <summary>
	/// This class maps a data value to an image that should be drawn for that value.
	/// </summary>
	/// <remarks><para>It is useful for drawing data that is represented as an enum or boolean.</para></remarks>
	public class MappedImageRenderer : BaseRenderer
	{
		/// <summary>
		/// Make a new empty renderer
		/// </summary>
		public MappedImageRenderer()
		{
			map = new Hashtable();
		}

		/// <summary>
		/// Make a new renderer that will show the given image when the given key is the aspect value
		/// </summary>
		/// <param name="key">The data value to be matched</param>
		/// <param name="image">The image to be shown when the key is matched</param>
		public MappedImageRenderer(Object key, Object image)
			: this()
		{
			Add(key, image);
		}

		/// <summary>
		/// Make a new renderer that will show the given images when it receives the given keys
		/// </summary>
		/// <param name="key1"></param>
		/// <param name="image1"></param>
		/// <param name="key2"></param>
		/// <param name="image2"></param>
		public MappedImageRenderer(Object key1, Object image1, Object key2, Object image2)
			: this()
		{
			Add(key1, image1);
			Add(key2, image2);
		}

		/// <summary>
		/// Build a renderer from the given array of keys and their matching images
		/// </summary>
		/// <param name="keysAndImages">An array of key/image pairs</param>
		public MappedImageRenderer(Object[] keysAndImages)
			: this()
		{
			if ((keysAndImages.GetLength(0) % 2) != 0)
				throw new ArgumentException("Array must have key/image pairs");

			for (int i = 0; i < keysAndImages.GetLength(0); i += 2)
				Add(keysAndImages[i], keysAndImages[i + 1]);
		}

		/// <summary>
		/// Return a renderer that draw boolean values using the given images
		/// </summary>
		/// <param name="trueImage">Draw this when our data value is true</param>
		/// <param name="falseImage">Draw this when our data value is false</param>
		/// <returns>A Renderer</returns>
		public static MappedImageRenderer Boolean(Object trueImage, Object falseImage)
		{
			return new MappedImageRenderer(true, trueImage, false, falseImage);
		}

		/// <summary>
		/// Return a renderer that draw tristate boolean values using the given images
		/// </summary>
		/// <param name="trueImage">Draw this when our data value is true</param>
		/// <param name="falseImage">Draw this when our data value is false</param>
		/// <param name="nullImage">Draw this when our data value is null</param>
		/// <returns>A Renderer</returns>
		public static MappedImageRenderer TriState(Object trueImage, Object falseImage, Object nullImage)
		{
			return new MappedImageRenderer(new[] {true, trueImage, false, falseImage, null, nullImage});
		}

		/// <summary>
		/// Register the image that should be drawn when our Aspect has the data value.
		/// </summary>
		/// <param name="value">Value that the Aspect must match</param>
		/// <param name="image">An ImageSelector -- an int, string or image</param>
		public void Add(Object value, Object image)
		{
			if (value == null)
				nullImage = image;
			else
				map[value] = image;
		}

		/// <summary>
		/// Render our value
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);

			var aspectAsCollection = Aspect as ICollection;
			if (aspectAsCollection == null)
				RenderOne(g, r, Aspect);
			else
				RenderCollection(g, r, aspectAsCollection);
		}

		/// <summary>
		/// Draw a collection of images
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="imageSelectors"></param>
		protected void RenderCollection(Graphics g, Rectangle r, ICollection imageSelectors)
		{
			var images = new ArrayList();
			Image image = null;
			foreach (Object selector in imageSelectors)
			{
				if (selector == null)
					image = GetImage(nullImage);
				else if (map.ContainsKey(selector))
					image = GetImage(map[selector]);
				else
					image = null;

				if (image != null)
					images.Add(image);
			}

			DrawImages(g, r, images);
		}

		/// <summary>
		/// Draw one image
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="selector"></param>
		protected void RenderOne(Graphics g, Rectangle r, Object selector)
		{
			Image image = null;
			if (selector == null)
				image = GetImage(nullImage);
			else if (map.ContainsKey(selector))
				image = GetImage(map[selector]);

			if (image != null)
				DrawAlignedImage(g, r, image);
		}

		#region Private variables

		private readonly Hashtable map; // Track the association between values and images
		private Object nullImage; // image to be drawn for null values (since null can't be a key)

		#endregion
	}

	/// <summary>
	/// This renderer draws just a checkbox to match the check state of our model object.
	/// </summary>
	public class CheckStateRenderer : BaseRenderer
	{
		/// <summary>
		/// Draw our cell
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);
			CheckState state = Column.GetCheckState(RowObject);
			if (IsPrinting)
			{
				// Renderers don't work onto printer DCs, so we have to draw the image ourselves
				string key = ObjectListView.CHECKED_KEY;
				if (state == CheckState.Unchecked)
					key = ObjectListView.UNCHECKED_KEY;
				if (state == CheckState.Indeterminate)
					key = ObjectListView.INDETERMINATE_KEY;
				DrawAlignedImage(g, r, ListView.SmallImageList.Images[key]);
			}
			else
			{
				r = CalculateCheckBoxBounds(g, r);
				CheckBoxRenderer.DrawCheckBox(g, r.Location, GetCheckBoxState(state));
			}
		}

		/// <summary>
		/// Calculate the renderer checkboxstate we need to correctly draw the given state
		/// </summary>
		/// <param name="checkState"></param>
		/// <returns></returns>
		protected virtual CheckBoxState GetCheckBoxState(CheckState checkState)
		{
			// Should the checkbox be drawn as disabled?
			bool isDisabled =
				ListView.RenderNonEditableCheckboxesAsDisabled &&
				(ListView.CellEditActivation == ObjectListView.CellEditActivateMode.None ||
				 !Column.IsEditable);

			if (isDisabled)
			{
				switch (checkState)
				{
					case CheckState.Checked:
						return CheckBoxState.CheckedDisabled;
					case CheckState.Unchecked:
						return CheckBoxState.UncheckedDisabled;
					default:
						return CheckBoxState.MixedDisabled;
				}
			}

			// Is the cursor currently over this checkbox?
			bool isHot =
				ListView != null &&
				ListItem != null &&
				ListView.HotRowIndex == ListItem.Index &&
				ListView.HotColumnIndex == Column.Index &&
				ListView.HotCellHitLocation == HitTestLocation.CheckBox;

			if (isHot)
			{
				switch (checkState)
				{
					case CheckState.Checked:
						return CheckBoxState.CheckedHot;
					case CheckState.Unchecked:
						return CheckBoxState.UncheckedHot;
					default:
						return CheckBoxState.MixedHot;
				}
			}

			// Not hot and not disabled -- just draw it normally
			switch (checkState)
			{
				case CheckState.Checked:
					return CheckBoxState.CheckedNormal;
				case CheckState.Unchecked:
					return CheckBoxState.UncheckedNormal;
				default:
					return CheckBoxState.MixedNormal;
			}
		}

		/// <summary>
		/// Handle the GetEditRectangle request
		/// </summary>
		/// <param name="g"></param>
		/// <param name="cellBounds"></param>
		/// <param name="item"></param>
		/// <param name="subItemIndex"></param>
		/// <returns></returns>
		protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item,
		                                                    int subItemIndex)
		{
			return cellBounds;
		}

		/// <summary>
		/// Handle the HitTest request
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
		{
			Rectangle r = CalculateCheckBoxBounds(g, Bounds);
			if (r.Contains(x, y))
				hti.HitTestLocation = HitTestLocation.CheckBox;
		}

		private Rectangle CalculateCheckBoxBounds(Graphics g, Rectangle cellBounds)
		{
			Size checkBoxSize = CheckBoxRenderer.GetGlyphSize(g, CheckBoxState.CheckedNormal);
			return AlignRectangle(cellBounds,
			                      new Rectangle(0, 0, checkBoxSize.Width, checkBoxSize.Height));
		}
	}

	/// <summary>
	/// Render an image that comes from our data source.
	/// </summary>
	/// <remarks>The image can be sourced from:
	/// <list type="bullet">
	/// <item><description>a byte-array (normally when the image to be shown is
	/// stored as a value in a database)</description></item>
	/// <item><description>an int, which is treated as an index into the image list</description></item>
	/// <item><description>a string, which is treated first as a file name, and failing that as an index into the image list</description></item>
	/// <item><description>an ICollection of ints or strings, which will be drawn as consecutive images</description></item>
	/// </list>
	/// <para>If an image is an animated GIF, it's state is stored in the SubItem object.</para>
	/// <para>By default, the image renderer does not render animations (it begins life with animations paused).
	/// To enable animations, you must call Unpause().</para>
	/// <para>In the current implementation (2009-09), each column showing animated gifs must have a 
	/// different instance of ImageRenderer assigned to it. You cannot share the same instance of
	/// an image renderer between two animated gif columns. If you do, only the last column will be
	/// animated.</para>
	/// </remarks>
	public class ImageRenderer : BaseRenderer
	{
		/// <summary>
		/// Make an empty image renderer
		/// </summary>
		public ImageRenderer()
		{
			tickler = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
			stopwatch = new Stopwatch();
		}

		/// <summary>
		/// Make an empty image renderer that begins life ready for animations
		/// </summary>
		public ImageRenderer(bool startAnimations)
			: this()
		{
			Paused = !startAnimations;
		}

		#region Properties

		private bool isPaused = true;

		/// <summary>
		/// Should the animations in this renderer be paused?
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Paused
		{
			get { return isPaused; }
			set
			{
				if (isPaused != value)
				{
					isPaused = value;
					if (isPaused)
					{
						tickler.Change(Timeout.Infinite, Timeout.Infinite);
						stopwatch.Stop();
					}
					else
					{
						tickler.Change(1, Timeout.Infinite);
						stopwatch.Start();
					}
				}
			}
		}

		#endregion

		#region Commands

		/// <summary>
		/// Pause any animations
		/// </summary>
		public void Pause()
		{
			Paused = true;
		}

		/// <summary>
		/// Unpause any animations
		/// </summary>
		public void Unpause()
		{
			Paused = false;
		}

		#endregion

		#region Drawing

		/// <summary>
		/// Draw our image
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);

			if (Aspect == null || Aspect == DBNull.Value)
				return;

			if (Aspect is Byte[]) DrawAlignedImage(g, r, GetImageFromAspect());
			else
			{
				var imageSelectors = Aspect as ICollection;
				if (imageSelectors == null)
					DrawAlignedImage(g, r, GetImageFromAspect());
				else
					DrawImages(g, r, imageSelectors);
			}
		}

		/// <summary>
		/// Translate our Aspect into an image.
		/// </summary>
		/// <remarks>The strategy is:<list type="bullet">
		/// <item><description>If its a byte array, we treat it as an in-memory image</description></item>
		/// <item><description>If it's an int, we use that as an index into our image list</description></item>
		/// <item><description>If it's a string, we try to load a file by that name. If we can't, 
		/// we use the string as an index into our image list.</description></item>
		///</list></remarks>
		/// <returns>An image</returns>
		protected Image GetImageFromAspect()
		{
			// If we've already figured out the image, don't do it again
			if (OLVSubItem != null && OLVSubItem.ImageSelector is Image)
			{
				if (OLVSubItem.AnimationState == null)
					return (Image) OLVSubItem.ImageSelector;
				else
					return OLVSubItem.AnimationState.image;
			}

			// Try to convert our Aspect into an Image
			// If its a byte array, we treat it as an in-memory image
			// If it's an int, we use that as an index into our image list
			// If it's a string, we try to find a file by that name.
			//    If we can't, we use the string as an index into our image list.
			Image image = null;
			if (Aspect is Byte[])
			{
				using (var stream = new MemoryStream((Byte[]) Aspect))
				{
					try
					{
						image = Image.FromStream(stream);
					}
					catch (ArgumentException)
					{
						// ignore
					}
				}
			}
			else if (Aspect is Int32) image = GetImage(Aspect);
			else
			{
				var str = Aspect as String;
				if (!String.IsNullOrEmpty(str))
				{
					try
					{
						image = Image.FromFile(str);
					}
					catch (FileNotFoundException)
					{
						image = GetImage(Aspect);
					}
					catch (OutOfMemoryException)
					{
						image = GetImage(Aspect);
					}
				}
			}

			// If this image is an animation, initialize the animation process
			if (OLVSubItem != null && AnimationState.IsAnimation(image)) OLVSubItem.AnimationState = new AnimationState(image);

			// Cache the image so we don't repeat this dreary process
			if (OLVSubItem != null)
				OLVSubItem.ImageSelector = image;

			return image;
		}

		#endregion

		#region Events

		/// <summary>
		/// This is the method that is invoked by the timer. It basically switches control to the listview thread.
		/// </summary>
		/// <param name="state">not used</param>
		public void OnTimer(Object state)
		{
			if (ListView == null || Paused)
				tickler.Change(1000, Timeout.Infinite);
			else
			{
				if (ListView.InvokeRequired)
					ListView.Invoke((MethodInvoker) delegate { OnTimer(state); });
				else
					OnTimerInThread();
			}
		}

		/// <summary>
		/// This is the OnTimer callback, but invoked in the same thread as the creator of the ListView.
		/// This method can use all of ListViews methods without creating a CrossThread exception.
		/// </summary>
		protected void OnTimerInThread()
		{
			// MAINTAINER NOTE: This method must renew the tickler. If it doesn't the animations will stop.

			// If this listview has been destroyed, we can't do anything, so we return without
			// renewing the tickler, effectively killing all animations on this renderer
			if (ListView.IsDisposed)
				return;

			// If we're not in Detail view or our column has been removed from the list,
			// we can't do anything at the moment, but we still renew the tickler because the view may change later.
			if (ListView.View != View.Details || Column.Index < 0)
			{
				tickler.Change(1000, Timeout.Infinite);
				return;
			}

			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
			int subItemIndex = Column.Index;
			long nextCheckAt = elapsedMilliseconds + 1000; // wait at most one second before checking again
			var updateRect = new Rectangle(); // what part of the view must be updated to draw the changed gifs?

			// Run through all the subitems in the view for our column, and for each one that
			// has an animation attached to it, see if the frame needs updating.

			for (int i = 0; i < ListView.GetItemCount(); i++)
			{
				OLVListItem lvi = ListView.GetItem(i);

				// Get the animation state from the subitem. If there isn't an animation state, skip this row.
				OLVListSubItem lvsi = lvi.GetSubItem(subItemIndex);
				AnimationState state = lvsi.AnimationState;
				if (state == null || !state.IsValid)
					continue;

				// Has this frame of the animation expired?
				if (elapsedMilliseconds >= state.currentFrameExpiresAt)
				{
					state.AdvanceFrame(elapsedMilliseconds);

					// Track the area of the view that needs to be redrawn to show the changed images
					if (updateRect.IsEmpty)
						updateRect = lvsi.Bounds;
					else
						updateRect = Rectangle.Union(updateRect, lvsi.Bounds);
				}

				// Remember the minimum time at which a frame is next due to change
				nextCheckAt = Math.Min(nextCheckAt, state.currentFrameExpiresAt);
			}

			// Update the part of the listview where frames have changed
			if (!updateRect.IsEmpty)
				ListView.Invalidate(updateRect);

			// Renew the tickler in time for the next frame change
			tickler.Change(nextCheckAt - elapsedMilliseconds, Timeout.Infinite);
		}

		#endregion

		#region Nested type: AnimationState

		/// <summary>
		/// Instances of this class kept track of the animation state of a single image.
		/// </summary>
		internal class AnimationState
		{
			private const int PropertyTagTypeShort = 3;
			private const int PropertyTagTypeLong = 4;
			private const int PropertyTagFrameDelay = 0x5100;
			private const int PropertyTagLoopCount = 0x5101;

			internal int currentFrame;
			internal long currentFrameExpiresAt;
			internal int frameCount;
			internal Image image;
			internal List<int> imageDuration;

			/// <summary>
			/// Create an AnimationState in a quiet state
			/// </summary>
			public AnimationState()
			{
				imageDuration = new List<int>();
			}

			/// <summary>
			/// Create an animation state for the given image, which may or may not
			/// be an animation
			/// </summary>
			/// <param name="image">The image to be rendered</param>
			public AnimationState(Image image)
				: this()
			{
				if (!IsAnimation(image))
					return;

				// How many frames in the animation?
				this.image = image;
				frameCount = this.image.GetFrameCount(FrameDimension.Time);

				// Find the delay between each frame.
				// The delays are stored an array of 4-byte ints. Each int is the
				// number of 1/100th of a second that should elapsed before the frame expires
				foreach (PropertyItem pi in this.image.PropertyItems)
				{
					if (pi.Id == PropertyTagFrameDelay)
					{
						for (int i = 0; i < pi.Len; i += 4)
						{
							//TODO: There must be a better way to convert 4-bytes to an int
							int delay = (pi.Value[i + 3] << 24) + (pi.Value[i + 2] << 16) + (pi.Value[i + 1] << 8) + pi.Value[i];
							imageDuration.Add(delay * 10); // store delays as milliseconds
						}
						break;
					}
				}

				// There should be as many frame durations as frames
				Debug.Assert(imageDuration.Count == frameCount, "There should be as many frame durations as there are frames.");
			}

			/// <summary>
			/// Does this state represent a valid animation
			/// </summary>
			public bool IsValid
			{
				get { return (image != null && frameCount > 0); }
			}

			/// <summary>
			/// Is the given image an animation
			/// </summary>
			/// <param name="image">The image to be tested</param>
			/// <returns>Is the image an animation?</returns>
			public static bool IsAnimation(Image image)
			{
				if (image == null)
					return false;
				else
					return (new List<Guid>(image.FrameDimensionsList)).Contains(FrameDimension.Time.Guid);
			}

			/// <summary>
			/// Advance our images current frame and calculate when it will expire
			/// </summary>
			public void AdvanceFrame(long millisecondsNow)
			{
				currentFrame = (currentFrame + 1) % frameCount;
				currentFrameExpiresAt = millisecondsNow + imageDuration[currentFrame];
				image.SelectActiveFrame(FrameDimension.Time, currentFrame);
			}
		}

		#endregion

		#region Private variables

		private readonly Stopwatch stopwatch; // clock used to time the animation frame changes
		private readonly Timer tickler; // timer used to tickle the animations

		#endregion
	}

	/// <summary>
	/// Render our Aspect as a progress bar
	/// </summary>
	public class BarRenderer : BaseRenderer
	{
		#region Constructors

		/// <summary>
		/// Make a BarRenderer
		/// </summary>
		public BarRenderer()
		{
		}

		/// <summary>
		/// Make a BarRenderer for the given range of data values
		/// </summary>
		public BarRenderer(int minimum, int maximum)
			: this()
		{
			MinimumValue = minimum;
			MaximumValue = maximum;
		}

		/// <summary>
		/// Make a BarRenderer using a custom bar scheme
		/// </summary>
		public BarRenderer(Pen pen, Brush brush)
			: this()
		{
			Pen = pen;
			Brush = brush;
			UseStandardBar = false;
		}

		/// <summary>
		/// Make a BarRenderer using a custom bar scheme
		/// </summary>
		public BarRenderer(int minimum, int maximum, Pen pen, Brush brush)
			: this(minimum, maximum)
		{
			Pen = pen;
			Brush = brush;
			UseStandardBar = false;
		}

		/// <summary>
		/// Make a BarRenderer that uses a horizontal gradient
		/// </summary>
		public BarRenderer(Pen pen, Color start, Color end)
			: this()
		{
			Pen = pen;
			SetGradient(start, end);
		}

		/// <summary>
		/// Make a BarRenderer that uses a horizontal gradient
		/// </summary>
		public BarRenderer(int minimum, int maximum, Pen pen, Color start, Color end)
			: this(minimum, maximum)
		{
			Pen = pen;
			SetGradient(start, end);
		}

		#endregion

		#region Configuration Properties

		private Color backgroundColor = Color.AliceBlue;
		private Color endColor = Color.DarkBlue;
		private Color fillColor = Color.BlueViolet;
		private Color frameColor = Color.Black;
		private float frameWidth = 1.0f;
		private int maximumHeight = 16;
		private double maximumValue = 100.0;
		private int maximumWidth = 100;
		private int padding = 2;
		private Color startColor = Color.CornflowerBlue;
		private bool useStandardBar = true;

		/// <summary>
		/// Should this bar be drawn in the system style?
		/// </summary>
		[Category("ObjectListView"),
		 Description("Should this bar be drawn in the system style?"),
		 DefaultValue(true)]
		public bool UseStandardBar
		{
			get { return useStandardBar; }
			set { useStandardBar = value; }
		}

		/// <summary>
		/// How many pixels in from our cell border will this bar be drawn
		/// </summary>
		[Category("ObjectListView"),
		 Description("How many pixels in from our cell border will this bar be drawn"),
		 DefaultValue(2)]
		public int Padding
		{
			get { return padding; }
			set { padding = value; }
		}

		/// <summary>
		/// What color will be used to fill the interior of the control before the 
		/// progress bar is drawn?
		/// </summary>
		[Category("ObjectListView"),
		 Description("The color of the interior of the bar"),
		 DefaultValue(typeof (Color), "AliceBlue")]
		public Color BackgroundColor
		{
			get { return backgroundColor; }
			set { backgroundColor = value; }
		}

		/// <summary>
		/// What color should the frame of the progress bar be?
		/// </summary>
		[Category("ObjectListView"),
		 Description("What color should the frame of the progress bar be"),
		 DefaultValue(typeof (Color), "Black")]
		public Color FrameColor
		{
			get { return frameColor; }
			set { frameColor = value; }
		}

		/// <summary>
		/// How many pixels wide should the frame of the progress bar be?
		/// </summary>
		[Category("ObjectListView"),
		 Description("How many pixels wide should the frame of the progress bar be"),
		 DefaultValue(1.0f)]
		public float FrameWidth
		{
			get { return frameWidth; }
			set { frameWidth = value; }
		}

		/// <summary>
		/// What color should the 'filled in' part of the progress bar be?
		/// </summary>
		/// <remarks>This is only used if GradientStartColor is Color.Empty</remarks>
		[Category("ObjectListView"),
		 Description("What color should the 'filled in' part of the progress bar be"),
		 DefaultValue(typeof (Color), "BlueViolet")]
		public Color FillColor
		{
			get { return fillColor; }
			set { fillColor = value; }
		}

		/// <summary>
		/// Use a gradient to fill the progress bar starting with this color
		/// </summary>
		[Category("ObjectListView"),
		 Description("Use a gradient to fill the progress bar starting with this color"),
		 DefaultValue(typeof (Color), "CornflowerBlue")]
		public Color GradientStartColor
		{
			get { return startColor; }
			set { startColor = value; }
		}

		/// <summary>
		/// Use a gradient to fill the progress bar ending with this color
		/// </summary>
		[Category("ObjectListView"),
		 Description("Use a gradient to fill the progress bar ending with this color"),
		 DefaultValue(typeof (Color), "DarkBlue")]
		public Color GradientEndColor
		{
			get { return endColor; }
			set { endColor = value; }
		}

		/// <summary>
		/// Regardless of how wide the column become the progress bar will never be wider than this
		/// </summary>
		[Category("Behavior"),
		 Description("The progress bar will never be wider than this"),
		 DefaultValue(100)]
		public int MaximumWidth
		{
			get { return maximumWidth; }
			set { maximumWidth = value; }
		}

		/// <summary>
		/// Regardless of how high the cell is  the progress bar will never be taller than this
		/// </summary>
		[Category("Behavior"),
		 Description("The progress bar will never be taller than this"),
		 DefaultValue(16)]
		public int MaximumHeight
		{
			get { return maximumHeight; }
			set { maximumHeight = value; }
		}

		/// <summary>
		/// The minimum data value expected. Values less than this will given an empty bar
		/// </summary>
		[Category("Behavior"), Description("The minimum data value expected. Values less than this will given an empty bar"),
		 DefaultValue(0.0)]
		public double MinimumValue { get; set; }

		/// <summary>
		/// The maximum value for the range. Values greater than this will give a full bar
		/// </summary>
		[Category("Behavior"),
		 Description("The maximum value for the range. Values greater than this will give a full bar"),
		 DefaultValue(100.0)]
		public double MaximumValue
		{
			get { return maximumValue; }
			set { maximumValue = value; }
		}

		#endregion

		#region Public Properties (non-IDE)

		private Brush backgroundBrush;
		private Brush brush;
		private Pen pen;

		/// <summary>
		/// The Pen that will draw the frame surrounding this bar
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Pen Pen
		{
			get
			{
				if (pen == null && !FrameColor.IsEmpty)
					return new Pen(FrameColor, FrameWidth);
				else
					return pen;
			}
			set { pen = value; }
		}

		/// <summary>
		/// The brush that will be used to fill the bar
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Brush Brush
		{
			get
			{
				if (brush == null && !FillColor.IsEmpty)
					return new SolidBrush(FillColor);
				else
					return brush;
			}
			set { brush = value; }
		}

		/// <summary>
		/// The brush that will be used to fill the background of the bar
		/// </summary>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Brush BackgroundBrush
		{
			get
			{
				if (backgroundBrush == null && !BackgroundColor.IsEmpty)
					return new SolidBrush(BackgroundColor);
				else
					return backgroundBrush;
			}
			set { backgroundBrush = value; }
		}

		#endregion

		/// <summary>
		/// Draw this progress bar using a gradient
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public void SetGradient(Color start, Color end)
		{
			GradientStartColor = start;
			GradientEndColor = end;
		}

		/// <summary>
		/// Draw our aspect
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);

			Rectangle frameRect = Rectangle.Inflate(r, 0 - Padding, 0 - Padding);
			frameRect.Width = Math.Min(frameRect.Width, MaximumWidth);
			frameRect.Height = Math.Min(frameRect.Height, MaximumHeight);
			frameRect = AlignRectangle(r, frameRect);

			// Convert our aspect to a numeric value
			var convertable = Aspect as IConvertible;
			if (convertable == null)
				return;
			double aspectValue = convertable.ToDouble(NumberFormatInfo.InvariantInfo);

			Rectangle fillRect = Rectangle.Inflate(frameRect, -1, -1);
			if (aspectValue <= MinimumValue)
				fillRect.Width = 0;
			else if (aspectValue < MaximumValue)
				fillRect.Width = (int) (fillRect.Width * (aspectValue - MinimumValue) / MaximumValue);

			// MS-themed progress bars don't work when printing
			if (UseStandardBar && ProgressBarRenderer.IsSupported && !IsPrinting)
			{
				ProgressBarRenderer.DrawHorizontalBar(g, frameRect);
				ProgressBarRenderer.DrawHorizontalChunks(g, fillRect);
			}
			else
			{
				g.FillRectangle(BackgroundBrush, frameRect);
				if (fillRect.Width > 0)
				{
					// FillRectangle fills inside the given rectangle, so expand it a little
					fillRect.Width++;
					fillRect.Height++;
					if (GradientStartColor == Color.Empty)
						g.FillRectangle(Brush, fillRect);
					else
					{
						using (
							var gradient = new LinearGradientBrush(frameRect, GradientStartColor, GradientEndColor,
							                                       LinearGradientMode.Horizontal)) g.FillRectangle(gradient, fillRect);
					}
				}
				g.DrawRectangle(Pen, frameRect);
			}
		}
	}

	/// <summary>
	/// An ImagesRenderer draws zero or more images depending on the data returned by its Aspect.
	/// </summary>
	/// <remarks><para>This renderer's Aspect must return a ICollection of ints, strings or Images,
	/// each of which will be drawn horizontally one after the other.</para>
	/// <para>As of v2.1, this functionality has been absorbed into ImageRenderer and this is now an
	/// empty shell, solely for backwards compatibility.</para>
	/// </remarks>
	[ToolboxItem(false)]
	public class ImagesRenderer : ImageRenderer
	{
	}

	/// <summary>
	/// A MultiImageRenderer draws the same image a number of times based on our data value
	/// </summary>
	/// <remarks><para>The stars in the Rating column of iTunes is a good example of this type of renderer.</para></remarks>
	public class MultiImageRenderer : BaseRenderer
	{
		/// <summary>
		/// Make a quiet rendererer
		/// </summary>
		public MultiImageRenderer()
		{
		}

		/// <summary>
		/// Make an image renderer that will draw the indicated image, at most maxImages times.
		/// </summary>
		/// <param name="imageSelector"></param>
		/// <param name="maxImages"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		public MultiImageRenderer(Object imageSelector, int maxImages, int minValue, int maxValue)
			: this()
		{
			ImageSelector = imageSelector;
			MaxNumberImages = maxImages;
			MinimumValue = minValue;
			MaximumValue = maxValue;
		}

		#region Configuration Properties

		private Object imageSelector;
		private int maxNumberImages = 10;
		private int maximumValue = 100;

		/// <summary>
		/// The index of the image that should be drawn
		/// </summary>
		[Category("Behavior"),
		 Description("The index of the image that should be drawn"),
		 DefaultValue(-1)]
		public int ImageIndex
		{
			get
			{
				if (imageSelector is Int32)
					return (Int32) imageSelector;
				else
					return -1;
			}
			set { imageSelector = value; }
		}

		/// <summary>
		/// The name of the image that should be drawn
		/// </summary>
		[Category("Behavior"),
		 Description("The index of the image that should be drawn"),
		 DefaultValue(null)]
		public string ImageName
		{
			get { return imageSelector as String; }
			set { imageSelector = value; }
		}

		/// <summary>
		/// The image selector that will give the image to be drawn
		/// </summary>
		/// <remarks>Like all image selectors, this can be an int, string or Image</remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Object ImageSelector
		{
			get { return imageSelector; }
			set { imageSelector = value; }
		}

		/// <summary>
		/// What is the maximum number of images that this renderer should draw?
		/// </summary>
		[Category("Behavior"),
		 Description("The maximum number of images that this renderer should draw"),
		 DefaultValue(10)]
		public int MaxNumberImages
		{
			get { return maxNumberImages; }
			set { maxNumberImages = value; }
		}

		/// <summary>
		/// Values less than or equal to this will have 0 images drawn
		/// </summary>
		[Category("Behavior"), Description("Values less than or equal to this will have 0 images drawn"), DefaultValue(0)]
		public int MinimumValue { get; set; }

		/// <summary>
		/// Values greater than or equal to this will have MaxNumberImages images drawn
		/// </summary>
		[Category("Behavior"),
		 Description("Values greater than or equal to this will have MaxNumberImages images drawn"),
		 DefaultValue(100)]
		public int MaximumValue
		{
			get { return maximumValue; }
			set { maximumValue = value; }
		}

		#endregion

		/// <summary>
		/// Draw our data value
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);

			Image image = GetImage(ImageSelector);
			if (image == null)
				return;

			// Convert our aspect to a numeric value
			var convertable = Aspect as IConvertible;
			if (convertable == null)
				return;
			double aspectValue = convertable.ToDouble(NumberFormatInfo.InvariantInfo);

			// Calculate how many images we need to draw to represent our aspect value
			int numberOfImages;
			if (aspectValue <= MinimumValue)
				numberOfImages = 0;
			else if (aspectValue < MaximumValue)
				numberOfImages = 1 + (int) (MaxNumberImages * (aspectValue - MinimumValue) / MaximumValue);
			else
				numberOfImages = MaxNumberImages;

			// If we need to shrink the image, what will its on-screen dimensions be?
			int imageScaledWidth = image.Width;
			int imageScaledHeight = image.Height;
			if (r.Height < image.Height)
			{
				imageScaledWidth = (int) (image.Width * (float) r.Height / image.Height);
				imageScaledHeight = r.Height;
			}
			// Calculate where the images should be drawn
			Rectangle imageBounds = r;
			imageBounds.Width = (MaxNumberImages * (imageScaledWidth + Spacing)) - Spacing;
			imageBounds.Height = imageScaledHeight;
			imageBounds = AlignRectangle(r, imageBounds);

			// Finally, draw the images
			for (int i = 0; i < numberOfImages; i++)
			{
				g.DrawImage(image, imageBounds.X, imageBounds.Y, imageScaledWidth, imageScaledHeight);
				imageBounds.X += (imageScaledWidth + Spacing);
			}
		}
	}


	/// <summary>
	/// A class to render a value that contains a bitwise-OR'ed collection of values.
	/// </summary>
	public class FlagRenderer : BaseRenderer
	{
		private readonly Dictionary<Int32, Object> imageMap = new Dictionary<Int32, object>();
		private readonly List<Int32> keysInOrder = new List<Int32>();

		/// <summary>
		/// Register the given image to the given value
		/// </summary>
		/// <param name="key">When this flag is present...</param>
		/// <param name="imageSelector">...draw this image</param>
		public void Add(Object key, Object imageSelector)
		{
			Int32 k2 = ((IConvertible) key).ToInt32(NumberFormatInfo.InvariantInfo);

			imageMap[k2] = imageSelector;
			keysInOrder.Remove(k2);
			keysInOrder.Add(k2);
		}

		/// <summary>
		/// Draw the flags
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);

			var convertable = Aspect as IConvertible;
			if (convertable == null)
				return;

			Int32 v2 = convertable.ToInt32(NumberFormatInfo.InvariantInfo);

			Point pt = r.Location;
			foreach (Int32 key in keysInOrder)
			{
				if ((v2 & key) == key)
				{
					Image image = GetImage(imageMap[key]);
					if (image != null)
					{
						g.DrawImage(image, pt);
						pt.X += (image.Width + Spacing);
					}
				}
			}
		}

		/// <summary>
		/// Do the actual work of hit testing. Subclasses should override this rather than HitTest()
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
		{
			var convertable = Aspect as IConvertible;
			if (convertable == null)
				return;

			Int32 v2 = convertable.ToInt32(NumberFormatInfo.InvariantInfo);

			Point pt = Bounds.Location;
			foreach (Int32 key in keysInOrder)
			{
				if ((v2 & key) == key)
				{
					Image image = GetImage(imageMap[key]);
					if (image != null)
					{
						var imageRect = new Rectangle(pt, image.Size);
						if (imageRect.Contains(x, y))
						{
							hti.UserData = key;
							return;
						}
						pt.X += (image.Width + Spacing);
					}
				}
			}
		}
	}

	/// <summary>
	/// This renderer draws an image, a single line title, and then multi-line descrition
	/// under the title.
	/// </summary>
	/// <remarks>
	/// <para>This class works best with FullRowSelect = true.</para>
	/// <para>It's not designed to work with cell editing -- it will work but will look odd.</para>
	/// <para>
	/// This class is experimental. It may not work properly and may disappear from
	/// future versions.
	/// </para>
	/// </remarks>
	public class DescribedTaskRenderer : BaseRenderer
	{
		#region Configuration properties

		private Size cellPadding = new Size(2, 2);
		private Color descriptionColor = Color.DimGray;
		private int imageTextSpace = 4;

		/// <summary>
		/// Gets or set the font that will be used to draw the title of the task
		/// </summary>
		/// <remarks>If this is null, the ListView's font will be used</remarks>
		[Category("ObjectListView"), Description("The font that will be used to draw the title of the task"),
		 DefaultValue(null)]
		public Font TitleFont { get; set; }

		/// <summary>
		/// Return a font that has been set for the title or a reasonable default
		/// </summary>
		[Browsable(false)]
		public Font TitleFontOrDefault
		{
			get { return TitleFont ?? ListView.Font; }
		}

		/// <summary>
		/// Gets or set the color of the title of the task
		/// </summary>
		/// <remarks>This color is used when the task is not selected or when the listview
		/// has a translucent selection mechanism.</remarks>
		[Category("ObjectListView"), Description("The color of the title"), DefaultValue(typeof (Color), "")]
		public Color TitleColor { get; set; }

		/// <summary>
		/// Return the color of the title of the task or a reasonable default
		/// </summary>
		[Browsable(false)]
		public Color TitleColorOrDefault
		{
			get
			{
				if (IsItemSelected || TitleColor.IsEmpty)
					return GetForegroundColor();
				else
					return TitleColor;
			}
		}

		/// <summary>
		/// Gets or set the font that will be used to draw the description of the task
		/// </summary>
		/// <remarks>If this is null, the ListView's font will be used</remarks>
		[Category("ObjectListView"), Description("The font that will be used to draw the description of the task"),
		 DefaultValue(null)]
		public Font DescriptionFont { get; set; }

		/// <summary>
		/// Return a font that has been set for the title or a reasonable default
		/// </summary>
		[Browsable(false)]
		public Font DescriptionFontOrDefault
		{
			get { return DescriptionFont ?? ListView.Font; }
		}

		/// <summary>
		/// Gets or set the color of the description of the task
		/// </summary>
		/// <remarks>This color is used when the task is not selected or when the listview
		/// has a translucent selection mechanism.</remarks>
		[Category("ObjectListView"),
		 Description("The color of the description"),
		 DefaultValue(typeof (Color), "DimGray")]
		public Color DescriptionColor
		{
			get { return descriptionColor; }
			set { descriptionColor = value; }
		}

		/// <summary>
		/// Return the color of the description of the task or a reasonable default
		/// </summary>
		[Browsable(false)]
		public Color DescriptionColorOrDefault
		{
			get
			{
				if (DescriptionColor.IsEmpty || (IsItemSelected && !ListView.UseTranslucentSelection))
					return GetForegroundColor();
				else
					return DescriptionColor;
			}
		}

		/// <summary>
		/// Gets or sets the number of pixels that renderer will leave empty around the edge of the cell
		/// </summary>
		[Category("ObjectListView"),
		 Description("The number of pixels that renderer will leave empty around the edge of the cell"),
		 DefaultValue(typeof (Size), "2,2")]
		public Size CellPadding
		{
			get { return cellPadding; }
			set { cellPadding = value; }
		}

		/// <summary>
		/// Gets or sets the number of pixels that will be left between the image and the text
		/// </summary>
		[Category("ObjectListView"),
		 Description("The number of pixels that that will be left between the image and the text"),
		 DefaultValue(4)]
		public int ImageTextSpace
		{
			get { return imageTextSpace; }
			set { imageTextSpace = value; }
		}

		/// <summary>
		/// Gets or sets the name of the aspect of the model object that contains the task description
		/// </summary>
		[Category("ObjectListView"),
		 Description("The name of the aspect of the model object that contains the task description"), DefaultValue(null)]
		public string DescriptionAspectName { get; set; }

		#endregion

		#region Calculating

		private Munger descriptionGetter;

		/// <summary>
		/// Fetch the description from the model class
		/// </summary>
		/// <returns></returns>
		protected virtual string GetDescription()
		{
			if (String.IsNullOrEmpty(DescriptionAspectName))
				return String.Empty;

			if (descriptionGetter == null)
				descriptionGetter = new Munger(DescriptionAspectName);

			return descriptionGetter.GetValue(RowObject) as String;
		}

		#endregion

		#region Rendering

		/// <summary>
		/// Draw our item
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(Graphics g, Rectangle r)
		{
			DrawBackground(g, r);
			DrawDescribedTask(g, r, Aspect as String, GetDescription(), GetImage());
		}

		/// <summary>
		/// Draw the task
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <param name="title"></param>
		/// <param name="description"></param>
		/// <param name="image"></param>
		protected virtual void DrawDescribedTask(Graphics g, Rectangle r, string title, string description, Image image)
		{
			Rectangle cellBounds = r;
			cellBounds.Inflate(-CellPadding.Width, -CellPadding.Height);
			Rectangle textBounds = cellBounds;

			if (image != null)
			{
				g.DrawImage(image, cellBounds.Location);
				int gapToText = image.Width + ImageTextSpace;
				textBounds.X += gapToText;
				textBounds.Width -= gapToText;
			}

			// Color the background if the row is selected and we're not using a translucent selection
			if (IsItemSelected && !ListView.UseTranslucentSelection)
				using (var b = new SolidBrush(GetTextBackgroundColor())) g.FillRectangle(b, textBounds);

			// Draw the title
			if (!String.IsNullOrEmpty(title))
			{
				using (var fmt = new StringFormat(StringFormatFlags.NoWrap))
				{
					fmt.Trimming = StringTrimming.EllipsisCharacter;
					fmt.Alignment = StringAlignment.Near;
					fmt.LineAlignment = StringAlignment.Near;
					Font f = TitleFontOrDefault;
					using (var b = new SolidBrush(TitleColorOrDefault)) g.DrawString(title, f, b, textBounds, fmt);

					// How tall was the title?
					SizeF size = g.MeasureString(title, f, textBounds.Width, fmt);
					textBounds.Y += (int) size.Height;
					textBounds.Height -= (int) size.Height;
				}
			}

			// Draw the description
			if (!String.IsNullOrEmpty(description))
			{
				using (var fmt2 = new StringFormat())
				{
					fmt2.Trimming = StringTrimming.EllipsisCharacter;
					using (var b = new SolidBrush(DescriptionColorOrDefault))
						g.DrawString(description, DescriptionFontOrDefault, b, textBounds, fmt2);
				}
			}
		}

		#endregion

		#region Hit Testing

		/// <summary>
		/// Handle the HitTest request
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
		{
			if (Bounds.Contains(x, y))
				hti.HitTestLocation = HitTestLocation.Text;
		}

		#endregion
	}
}