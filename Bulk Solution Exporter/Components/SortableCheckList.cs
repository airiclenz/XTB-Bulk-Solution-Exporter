﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Plugin.Helpers;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public partial class SortableCheckList : Control
	{

		private List<object> _items = new List<object>();
		private List<SortableCheckItem> _metaDataPerItem = new List<SortableCheckItem>();

		private int _itemHeight = 20;
		private float _textHeight;
		private float _borderThickness = 1f;
		private Color _borderColor = SystemColors.ControlDark;
		private int _scrollOffset = 0;
		private int _checkBoxSize = 18;
		private int _checkBoxRadius = 5;
		private int _checkBoxMargin = 3;
		private int _selectedIndex = -1;
		private int _dragBurgerSize = 14;
		private float _dragBurgerLineThickness = 1.5f;
		private bool _showScrollBar = true;
		private bool _isSortable = true;
		private bool _isCheckable = true;



		private Color _colorOff = Color.FromArgb(150, 150, 150);
		private Color _colorOn = Color.MediumSlateBlue;

		private const string measureText = "QWypg/#_Ág";
		private int? _dragStartIndex = null;
		private int _currentDropIndex = -1;



		// ============================================================================
		public SortableCheckList()
		{
			InitializeComponent();
			SuspendLayout();

			SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
			//SetStyle(ControlStyles.Selectable, true);
			TabStop = true;
			DoubleBuffered = true;
			BackColor = SystemColors.Window;


			AdjustItemHeight();
			ResumeLayout();
			Invalidate();
		}


		// ##################################################
		// ##################################################

		#region Events

		/// <summary>
		/// Triggers when a new row was selected or all have been de-selected.
		/// </summary>
		public event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Triggers whenever an item is checked or unchecked.
		/// </summary>
		public event EventHandler ItemChecked;

		/// <summary>
		/// Triggers if the order of the items has be changed.
		/// </summary>
		public event EventHandler ItemOrderChanged;



		// ============================================================================
		protected override void OnPaint(
			PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			Brush brushCheckedRow =
				new SolidBrush(
					Color.FromArgb(30, ColorHelper.MixColors(_colorOn, 0.6, Color.Black)));

			var checkerRadius = _checkBoxRadius * 0.5f;


			// do some cleanup if needed
			if (_items.Count != _metaDataPerItem.Count)
			{
				SynchronizeItemsMetaData();
			}


			// -------------------------------------
			// paint all items
			int startIndex = _scrollOffset / _itemHeight;
			int endIndex = Math.Min(_items.Count, startIndex + (Height / _itemHeight));
			var marginTopText = (_itemHeight - _textHeight) / 2f;
			var marginTopBurger = (_itemHeight - _dragBurgerSize) / 2f;
			var marginTopCheckBox = (_itemHeight - _checkBoxSize) / 2f;

			for (int i = startIndex; i < endIndex; i++)
			{
				int yPosition = (i * _itemHeight) - _scrollOffset;
				var isChecked = _metaDataPerItem[i].IsChecked && _isCheckable;
				var isSelected = i == _selectedIndex;

				var brushRow = isSelected ? new SolidBrush(SystemColors.Highlight) : (isChecked ? brushCheckedRow : Brushes.White);
				var brushText = isSelected ? new SolidBrush(SystemColors.HighlightText) : new SolidBrush(ForeColor);

				//var checkBoxBrush = new SolidBrush(
				var checkBoxFillColor =
					isChecked ?
					ColorHelper.MixColors(_colorOn, (isSelected ? 0.2 : 0), Color.White) :
					_colorOff;
				var penCheckBoxFrame =
					isSelected ?
					new Pen(Color.FromArgb(210, Color.Black), 1.5f) :
					new Pen(Color.FromArgb(120, 0, 0, 0), isChecked ? 1.5f : 1f);

				// paint the row
				g.FillRectangle(brushRow, new Rectangle(0, yPosition, this.Width, _itemHeight));
				g.DrawRectangle(Pens.LightGray, new Rectangle(0, yPosition, this.Width, _itemHeight));

				// write the text
				g.DrawString(
					_metaDataPerItem[i].Title,
					isChecked ? new Font(Font, FontStyle.Bold) : Font,
					brushText,
					new RectangleF(
						10 + (_isCheckable ? _checkBoxSize + 10 : 0), 
						yPosition + marginTopText,
						Width -  (_isCheckable ? _checkBoxSize + 10 : 10) - (_isSortable ? (_dragBurgerSize * 2) + 15 : 15) - (_showScrollBar ? 15f : 8f),
						_textHeight));
				

				// Draw the checkbox background
				if (_isCheckable)
				{
					LinearGradientBrush lgb = new LinearGradientBrush(
						new Point(10, yPosition + (int) marginTopCheckBox),
						new Point(10 + _checkBoxSize, yPosition + (int) marginTopCheckBox + _checkBoxSize),
						ColorHelper.MixColors(checkBoxFillColor, 0.11, Color.Black),
						Color.FromArgb(255, checkBoxFillColor));

					DrawRoundedRectangle(
						g,
						new Rectangle(10, yPosition + (int) marginTopCheckBox, _checkBoxSize, _checkBoxSize),
						_checkBoxRadius,
						penCheckBoxFrame,
						lgb);

					// Draw the ckecker if this item is checked
					if (isChecked)
					{
						DrawRoundedRectangle(
							g,
							new Rectangle(
								10 + _checkBoxMargin,
								yPosition + (int) marginTopCheckBox + _checkBoxMargin,
								_checkBoxSize - (2 * _checkBoxMargin),
								_checkBoxSize - (2 * _checkBoxMargin)),
							checkerRadius,
							null,
							Brushes.White);
					}
				}

				// Draw drag-burger
				if (_isSortable)
				{
					var brushBurgerLines = new SolidBrush(Color.FromArgb(40, Color.Black));

					g.FillRectangle(
						brushBurgerLines,
						new RectangleF(
							Width - (_showScrollBar ? 15f : 8f) - (_dragBurgerSize * 2f),
							yPosition + marginTopBurger,
							_dragBurgerSize * 2f,
							_dragBurgerLineThickness));

					g.FillRectangle(
						brushBurgerLines,
						new RectangleF(
							Width - (_showScrollBar ? 15f : 8f) - (_dragBurgerSize * 2f),
							yPosition + (_itemHeight / 2f) - (_dragBurgerLineThickness / 2f),
							_dragBurgerSize * 2f,
							_dragBurgerLineThickness));

					g.FillRectangle(
						brushBurgerLines,
						new RectangleF(
							Width - (_showScrollBar ? 15f : 8f) - (_dragBurgerSize * 2f),
							yPosition + marginTopBurger + _dragBurgerSize - _dragBurgerLineThickness,
							_dragBurgerSize * 2f,
							_dragBurgerLineThickness));


					// paint a drag-n-drop indicator
					if (i == _currentDropIndex)
					{
						var brushFill = new SolidBrush(Color.FromArgb(50, Color.OrangeRed));
						g.FillRectangle(brushFill, 0, yPosition + 1, Width, _itemHeight - 2);
					}
				}
			}

			// paint the scroll bar
			if (_showScrollBar)
			{
				int totalItemsHeight = _metaDataPerItem.Count * _itemHeight;
				int clientHeight = this.ClientRectangle.Height - 6;

				// Calculate the length and position of the scrollbar
				float scrollBarRatio = (float) clientHeight / totalItemsHeight;
				int scrollBarHeight =
					scrollBarRatio > 1 ?
					clientHeight - 1 :
					(int) (clientHeight * scrollBarRatio) - 1;

				int scrollBarPos = (int) ((float) _scrollOffset / totalItemsHeight * clientHeight) + 3;

				DrawRoundedRectangle(
					g,
					new Rectangle(Width - 11, scrollBarPos - 3, 10, scrollBarHeight + 6),
					4f,
					null,
					new SolidBrush(BackColor));

				DrawRoundedRectangle(
					g,
					new Rectangle(Width - 8, scrollBarPos, 4, scrollBarHeight),
					4f,
					null,
					new SolidBrush(Color.FromArgb(100, Color.Gray)));
			}


			// -------------------------------------
			// paint the border
			var borderPen = new Pen(_borderColor, _borderThickness);
			g.DrawRectangle(borderPen, 0, 0, Width - _borderThickness, Height - _borderThickness);

		}


		// ============================================================================
		protected virtual void OnSelectedIndexChanged()
		{
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
		}

		// ============================================================================
		protected virtual void OnItemChecked()
		{
			ItemChecked?.Invoke(this, ItemCheckEventArgs.Empty);
		}

		// ============================================================================
		protected virtual void OnItemOrderChanged()
		{
			ItemOrderChanged?.Invoke(this, ItemCheckEventArgs.Empty);
		}


		// ============================================================================
		protected override void OnMouseWheel(
			MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			_scrollOffset -= e.Delta / 120 * _itemHeight;
			ClampScrollOffset();

			Invalidate();
		}

		// ============================================================================
		protected override void OnResize(
			EventArgs e)
		{
			base.OnResize(e);
			ClampScrollOffset();
			EnsureItemVisible(_selectedIndex);
			Invalidate();
		}

		// ============================================================================
		protected override void OnMouseDown(
			MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			HandleItemClick(e.Location);
			Invalidate();
		}

		// ============================================================================
		protected override void OnMouseUp(
			MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (_isSortable &&
				_dragStartIndex.HasValue &&
				_currentDropIndex != -1 &&
				_dragStartIndex.Value != _currentDropIndex)
			{
				// Reorder the list
				var draggedItem = _items[_dragStartIndex.Value];
				var draggedMetaItem = _metaDataPerItem[_dragStartIndex.Value];

				_items.RemoveAt(_dragStartIndex.Value);
				_metaDataPerItem.RemoveAt(_dragStartIndex.Value);

				_items.Insert(_currentDropIndex, draggedItem);
				_metaDataPerItem.Insert(_currentDropIndex, draggedMetaItem);

				// Reset drag state
				_dragStartIndex = null;
				_currentDropIndex = -1;

				// Trigger events
				OnItemOrderChanged();
				OnSelectedIndexChanged();
			}
			else
			{
				_dragStartIndex = null;
				_currentDropIndex = -1;
			}

			Invalidate();
		}

		// ============================================================================
		protected override void OnMouseMove(
			MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (_isSortable &&
				_dragStartIndex.HasValue)
			{
				int newIndex = GetItemAtPoint(e.Location);
				if (newIndex != -1 && newIndex != _currentDropIndex)
				{
					_currentDropIndex = newIndex;
					Invalidate();
				}
			}
		}


		// ============================================================================
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Up:
					SelectPreviousItem();
					return true; // Key press handled
				case Keys.Down:
					SelectNextItem();
					return true; // Key press handled
				case Keys.Space:
					if (_isCheckable)
					{
						ToggleSelectedItem();
					}
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}


		#endregion

		// ##################################################
		// ##################################################

		#region Properties


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<object> Items
		{
			get
			{
				VerifyItems();
				return _items;
			}
			set
			{
				_items = value;
				SynchronizeItemsMetaData();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Return the index of the currently selected row / item; If no row / item is selected, the return value is -1
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				return _selectedIndex;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Return the currently selected row / item; If no row / item is selected, the return value is null
		/// </summary>
		public object SelectedItem
		{
			get
			{
				if (_selectedIndex != -1)
				{
					return _metaDataPerItem[_selectedIndex].ItemObject;
				}

				return null;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Returns a list of all checked items
		/// </summary>
		public List<object> CheckedItems
		{
			get
			{
				List<object> resultList = new List<object>();

				foreach (var item in _metaDataPerItem)
				{
					if (item.IsChecked)
					{
						resultList.Add(item.ItemObject);
					}
				}

				return resultList;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int ItemHeigth
		{
			get
			{
				return _itemHeight;
			}
			set
			{
				_itemHeight = value;
				AdjustItemHeight();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int CheckBoxSize
		{
			get
			{
				return _checkBoxSize;
			}
			set
			{
				_checkBoxSize = value;

				if (_checkBoxSize < 10)
				{
					_checkBoxSize = 10;
				}

				AdjustItemHeight();
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int CheckBoxRadius
		{
			get
			{
				return _checkBoxRadius;
			}
			set
			{
				_checkBoxRadius = value;

				if (_checkBoxRadius > _checkBoxSize)
				{
					_checkBoxRadius = _checkBoxSize;
				}

				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int CheckBoxMargin
		{
			get
			{
				return _checkBoxMargin;
			}
			set
			{
				_checkBoxMargin = value;

				if (_checkBoxMargin < 1)
				{
					_checkBoxMargin = 1;
				}

				if (_checkBoxMargin > (_checkBoxSize / 2f) - 1)
				{
					_checkBoxMargin = (int) (_checkBoxSize / 2f) - 1;
				}

				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public override Font Font
		{
			get => base.Font;
			set
			{
				base.Font = value;
				AdjustItemHeight();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color BorderColor
		{
			get
			{
				return _borderColor;
			}
			set
			{
				_borderColor = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public float BorderThickness
		{
			get
			{
				return _borderThickness;
			}
			set
			{
				_borderThickness = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color ColorChecked
		{
			get
			{
				return _colorOn;
			}
			set
			{
				_colorOn = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color ColorUnchecked
		{
			get
			{
				return _colorOff;
			}
			set
			{
				_colorOff = value;
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int DragBurgerSize
		{
			get
			{
				return _dragBurgerSize;
			}
			set
			{
				_dragBurgerSize = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public float DragBurgerLineThickness
		{
			get
			{
				return _dragBurgerLineThickness;
			}
			set
			{
				_dragBurgerLineThickness = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool ShowScrollBar
		{
			get
			{
				return _showScrollBar;
			}
			set
			{
				_showScrollBar = value;
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsCheckable
		{
			get
			{
				return _isCheckable;
			}
			set
			{
				_isCheckable = value;
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsSortable
		{
			get
			{
				return _isSortable;
			}
			set
			{
				_isSortable = value;
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				base.Text = value;
			}
		}







		#endregion

		// ##################################################
		// ##################################################

		#region Custom Logic


		// ============================================================================
		private void VerifyItems()
		{
			SynchronizeItemsMetaData();
		}

		// ============================================================================
		private void ClampScrollOffset()
		{
			_scrollOffset = Math.Max(0, Math.Min(_scrollOffset, Math.Max(0, (Items.Count * _itemHeight) - Height)));
		}

		// ============================================================================
		private int GetItemAtPoint(
			Point location)
		{
			// Calculate which item is at the given location
			for (int i = 0; i < _metaDataPerItem.Count; i++)
			{
				if (GetItemBounds(i).Contains(location))
				{
					return i;
				}
			}
			return -1;
		}

		// ============================================================================
		private Rectangle GetItemBounds(
			int index)
		{
			return
				new Rectangle(
					0,
					(index * _itemHeight) + _scrollOffset,
					Width,
					_itemHeight);
		}


		// ============================================================================
		public void SetItemChecked(
			int index,
			bool state)
		{
			if (index >= 0 &&
				index < _metaDataPerItem.Count)
			{
				_metaDataPerItem[index].IsChecked = state;
			}
		}

		// ============================================================================
		public void CheckAllItems()
		{
			foreach (var item in _metaDataPerItem)
			{
				item.IsChecked = true;
			}

			OnItemChecked();
			Invalidate();
		}


		// ============================================================================
		public void UnCheckAllItems()
		{
			foreach (var item in _metaDataPerItem)
			{
				item.IsChecked = false;
			}

			OnItemChecked();
			Invalidate();
		}



		// ============================================================================
		private void SelectPreviousItem()
		{
			if (_selectedIndex > 0)
			{
				_selectedIndex--;
				EnsureItemVisible(_selectedIndex);
				OnSelectedIndexChanged();
				Invalidate();
			}
		}

		// ============================================================================
		private void SelectNextItem()
		{
			if (_selectedIndex < _metaDataPerItem.Count - 1)
			{
				_selectedIndex++;
				EnsureItemVisible(_selectedIndex);
				OnSelectedIndexChanged();
				Invalidate();
			}
		}


		// ============================================================================
		private void ToggleSelectedItem()
		{
			if (_selectedIndex == -1)
			{
				return;
			}

			_metaDataPerItem[_selectedIndex].Toggle();
			OnItemChecked();
			Invalidate();

		}



		// ============================================================================
		public new void Invalidate()
		{
			base.Invalidate();

			if (_items.Count != _metaDataPerItem.Count)
			{
				SynchronizeItemsMetaData();
			}
		}


		// ============================================================================
		public void DeselectAll()
		{
			_selectedIndex = -1;
			Invalidate();
		}

		// ============================================================================
		public void EnsureItemVisible(
			int index)
		{
			if (index < 0 ||
				index > _metaDataPerItem.Count)
			{
				return;
			}

			int itemTop = index * _itemHeight;
			int itemBottom = itemTop + _itemHeight;

			// Adjust scrolling to ensure the item is fully visible
			if (itemTop < _scrollOffset)
			{
				_scrollOffset = itemTop;
			}
			else if (itemBottom > _scrollOffset + Height)
			{
				_scrollOffset = itemBottom - Height + _itemHeight;
			}

			Invalidate();
		}


		// ============================================================================
		private void SynchronizeItemsMetaData()
		{
			// unidentify all meta data rows
			foreach (var metaData in _metaDataPerItem)
			{
				metaData.UnIdentify();
			}

			var position = -1;

			// link items to metadata
			foreach (var item in _items)
			{
				var hashCode = item.GetHashCode();
				var found = false;
				position++;

				foreach (var metaData in _metaDataPerItem)
				{
					if (metaData.IsIdentified)
					{
						continue;
					}

					if (hashCode == metaData.ItemHashCode)
					{
						metaData.IsIdentified = true;
						found = true;
						break;
					}
				}

				if (!found)
				{
					var newMetaData =
						new SortableCheckItem(
							item,
							position);

					_metaDataPerItem.Insert(position, newMetaData);
				}
			}

			// remove unidentified metadata rows
			for (int i = 0; i < _metaDataPerItem.Count; i++)
			{
				if (!_metaDataPerItem[i].IsIdentified)
				{
					_metaDataPerItem.RemoveAt(i);
				}
			}

			//_itemMetaDatas.Sort();
			Console.WriteLine("Exit");
		}


		// ============================================================================
		private void DrawRoundedRectangle(
			Graphics g,
			Rectangle bounds,
			float cornerRadius,
			Pen drawPen,
			Brush fillBrush)
		{
			using (GraphicsPath path = new GraphicsPath())
			{
				path.AddArc(bounds.Left, bounds.Top, cornerRadius, cornerRadius, 180, 90);
				path.AddArc(bounds.Right - cornerRadius, bounds.Top, cornerRadius, cornerRadius, 270, 90);
				path.AddArc(bounds.Right - cornerRadius, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
				path.AddArc(bounds.Left, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
				path.CloseFigure();

				if (fillBrush != null)
				{
					g.FillPath(fillBrush, path);
				}

				if (drawPen != null)
				{
					g.DrawPath(drawPen, path);
				}
			}
		}


		// ============================================================================
		private void AdjustItemHeight()
		{
			using (Graphics g = CreateGraphics())
			{
				SizeF textSize = g.MeasureString(measureText, Font);
				_textHeight = textSize.Height;
				_itemHeight = _textHeight > _itemHeight ? (int) _textHeight : _itemHeight;
				_itemHeight = _checkBoxSize + 2 > _itemHeight ? _checkBoxSize + 2 : _itemHeight;
			}
		}


		// ============================================================================
		private void HandleItemClick(Point clickLocation)
		{
			int startIndex = _scrollOffset / _itemHeight;
			int endIndex = Math.Min(_metaDataPerItem.Count, startIndex + (Height / _itemHeight));


			// check check-boxes for clicks
			if (_isCheckable)
			{
				for (int i = startIndex; i < endIndex; i++)
				{
					int yPosition = (i * _itemHeight) - _scrollOffset;

					var checkBoxBoundsCheckBox =
						new Rectangle(10, yPosition, _checkBoxSize, _checkBoxSize);

					if (checkBoxBoundsCheckBox.Contains(clickLocation))
					{
						_metaDataPerItem[i].Toggle();

						OnItemChecked();

						// Leave - we are done here...
						return;
					}
				}
			}

			// deselect all
			_selectedIndex = -1;


			// check drag-burger for click
			if (_isSortable)
			{
				for (int i = startIndex; i < endIndex; i++)
				{
					int yPosition = (i * _itemHeight) - _scrollOffset;

					var checkBoxBoundsCheckBox =
						new RectangleF(
							Width - 15f - (_dragBurgerSize * 2f),
							yPosition + ((_itemHeight - _dragBurgerSize) / 2f),
							_dragBurgerSize * 2f,
							_dragBurgerSize);

					if (checkBoxBoundsCheckBox.Contains(clickLocation))
					{
						_dragStartIndex = i;

						// Leave - we are done here...
						return;
					}
				}
			}


			// check hole row for clicks
			for (int i = startIndex; i < endIndex; i++)
			{
				int yPosition = (i * _itemHeight) - _scrollOffset;

				var checkBoxBoundsRow =
					new Rectangle(0, yPosition, Width, _itemHeight);

				if (checkBoxBoundsRow.Contains(clickLocation))
				{
					_selectedIndex = i;
					break;
				}
			}

			OnSelectedIndexChanged();
		}


		#endregion



	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class SortableCheckItem : IComparable<SortableCheckItem>
	{
		private int _objectHashCode = 0;

		private object _item;
		private bool _isIdentified;
		private bool _isChecked;
		//private bool _isSelected;
		private int _sortingIndex;
		private string _title;

		// ============================================================================
		public SortableCheckItem(
			object item,
			int sortingIndex)
		{
			_objectHashCode = item.GetHashCode();
			_item = item;
			_sortingIndex = sortingIndex;
			_isIdentified = true;
			_isChecked = false;
			//_isSelected = false;
			_title = item.ToString();
		}

		// ============================================================================
		public int CompareTo(SortableCheckItem other)
		{
			return this.SortingIndex.CompareTo(other.SortingIndex);
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public object ItemObject
		{
			get
			{
				return _item;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int ItemHashCode
		{
			get
			{
				return _objectHashCode;
			}
			// set { _objectHashCode = value; }
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsChecked
		{
			get
			{
				return _isChecked;
			}
			set
			{
				_isChecked = value;
			}
		}

		/*
		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsSelected
		{
			get { return _isSelected; }
			set { _isSelected = value; }
		}
		*/

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsIdentified
		{
			get
			{
				return _isIdentified;
			}
			set
			{
				_isIdentified = value;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int SortingIndex
		{
			get
			{
				return _sortingIndex;
			}
			set
			{
				_sortingIndex = value;
			}
		}

		// ============================================================================
		public void UnIdentify()
		{
			_isIdentified = false;
		}

		// ============================================================================
		public bool Toggle()
		{
			_isChecked = !_isChecked;
			return _isChecked;
		}

	}
}
