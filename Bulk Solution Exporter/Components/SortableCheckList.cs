using System;
using System.Activities.Presentation.Debug;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Microsoft.Crm.Sdk.Messages;


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

		//private List<object> _items = new List<object>();
		private List<SortableCheckItem> _items = new List<SortableCheckItem>();
		private List<ColumnDefinition> _columns = new List<ColumnDefinition>();


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
		private Image _noDataImage = null;
		private int _dynamicColumnSpace = 0;

		private Color _colorOff = Color.FromArgb(150, 150, 150);
		private Color _colorOn = Color.MediumSlateBlue;

		private const string measureText = "QWypg/#_Ág";
		private int? _dragStartIndex = null;
		private int _currentDropIndex = -1;
		private int _hoveringAboveDragBurgerIndex = -1;
		private int _hoveringAboveCheckBoxIndex = -1;


		// ============================================================================
		public SortableCheckList()
		{
			InitializeComponent();
			SuspendLayout();

			SetStyle(
				ControlStyles.Selectable |
				ControlStyles.UserPaint |
				ControlStyles.ResizeRedraw |
				ControlStyles.OptimizedDoubleBuffer,
				true);

			TabStop = true;
			DoubleBuffered = true;
			BackColor = SystemColors.Window;

			_columns.Add(
				new ColumnDefinition
				{
					Header = "Title",
					PropertyName = string.Empty,
					Width = "100px"
				});


			AdjustItemHeight();
			ResumeLayout();
			Invalidate();
		}

		// ##################################################
		// ##################################################

		#region Properties


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<SortableCheckItem> Items
		{
			get
			{
				return _items;
			}
			set
			{
				if (value == null)
				{
					_items = new List<SortableCheckItem>();
				}
				else
				{
					_items = value;
				}

				RecalculateColumnWidths();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<ColumnDefinition> Columns
		{
			get
			{
				return _columns;
			}
			set
			{
				if (value == null)
				{
					_columns = new List<ColumnDefinition>();
				}
				else
				{
					_columns = value;
				}

				RecalculateColumnWidths();
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
					return _items[_selectedIndex].ItemObject;
				}

				return null;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Returns a list of all checked items
		/// </summary>
		public List<SortableCheckItem> CheckedItems
		{
			get
			{
				List<SortableCheckItem> resultList = new List<SortableCheckItem>();

				foreach (var item in _items)
				{
					if (item.IsChecked)
					{
						resultList.Add(item);
					}
				}

				return resultList;
			}
		}



		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Returns a list of all checked items
		/// </summary>
		public List<object> CheckedObject
		{
			get
			{
				List<object> resultList = new List<object>();

				foreach (var item in _items)
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
				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
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

				RecalculateColumnWidths();
				Invalidate();
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public new Image BackgroundImage
		{
			get
			{
				return _noDataImage;
			}
			set
			{
				_noDataImage = value;
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
			var marginTopText = (_itemHeight - _textHeight) / 2f;
			var marginTopBurger = (_itemHeight - _dragBurgerSize) / 2f;
			var marginTopCheckBox = (_itemHeight - _checkBoxSize) / 2f;
			
			// -------------------------------------
			// paint all headers
			var leftMargin = 10 + (_isCheckable ? _checkBoxSize + 10 : 0);
			var spaceAvailableForColumns = GetSpaceForColumns(leftMargin);

			var colPosX = leftMargin;

			g.FillRectangle(
				new SolidBrush(Color.FromArgb(255, 60, 60, 60)),
				new Rectangle(0, 0, this.Width, _itemHeight));

			if (_items.Count > 0)
			{
				foreach (var column in _columns)
				{
					var colWidth = column.GetWithInPixels(_dynamicColumnSpace);

					g.DrawString(
						column.Header,
						new Font(Font, FontStyle.Bold),
						Brushes.White,
						colPosX,
						marginTopText);

					colPosX += (int) colWidth;
				}
			}



			// -------------------------------------
			// paint all items
			int startIndex = _scrollOffset / _itemHeight;
			int endIndex =
				Math.Min(
					_items.Count,
					startIndex + ((Height - _itemHeight) / _itemHeight));


			for (int i = startIndex; i < endIndex; i++)
			{
				// add one item height for the header
				int yPosition = Math.Max(
					_itemHeight,
					((i + 1) * _itemHeight) - _scrollOffset);

				var isChecked = _items[i].IsChecked && _isCheckable;
				var isSelected = i == _selectedIndex;

				var brushRow = isSelected ? new SolidBrush(SystemColors.Highlight) : (isChecked ? brushCheckedRow : Brushes.White);
				var brushText = isSelected ? new SolidBrush(SystemColors.HighlightText) : new SolidBrush(ForeColor);
				var hoveringThisCheckBox = _hoveringAboveCheckBoxIndex == i;

				//var checkBoxBrush = new SolidBrush(
				var checkBoxFillColor =
					isChecked ?
					ColorHelper.MixColors(_colorOn, (isSelected ? 0.2 : 0), Color.White) :
					_colorOff;
				var penCheckBoxFrame =
					isSelected ?
					new Pen(Color.FromArgb(210, Color.Black), hoveringThisCheckBox ? 2f : 1.5f) :
					new Pen(Color.FromArgb(120, 0, 0, 0), isChecked ? (hoveringThisCheckBox ? 2f : 1.5f) : (hoveringThisCheckBox ? 2f : 1f));

				// paint the row
				g.FillRectangle(brushRow, new Rectangle(0, yPosition, this.Width, _itemHeight));
				g.DrawRectangle(Pens.LightGray, new Rectangle(0, yPosition, this.Width, _itemHeight));


				// write the text
				colPosX = leftMargin;

				if (_items.Count > 0)
				{
					foreach (var column in _columns)
					{
						var colWidth = column.GetWithInPixels(_dynamicColumnSpace);

						if (!string.IsNullOrWhiteSpace(column.PropertyName))
						{
							var propertyObject = _items[i].ItemObject.GetType().GetProperty(column.PropertyName)?.GetValue(_items[i].ItemObject, null);

							if (propertyObject is Bitmap)
							{
								var propertyBitmap = propertyObject as Bitmap;
								var imageHeight = Math.Min(_itemHeight - 2, propertyBitmap.Height);
								var ratio = (float)imageHeight / (float)propertyBitmap.Height;
								var imageWidth = propertyBitmap.Width * ratio;
								var marginTop = (_itemHeight - imageHeight) / 2;
								
								g.DrawImage(
									propertyBitmap,
									colPosX,
									yPosition + marginTopText,
									imageWidth,
									imageHeight);
							}
							else
							{
								var propertyString = propertyObject?.ToString();

								g.DrawString(
									propertyString,
									isChecked ? new Font(Font, FontStyle.Bold) : Font,
									brushText,
									new RectangleF(
										colPosX,
										yPosition + marginTopText,
										colWidth,
										_textHeight));
							}
						}
						

						colPosX += (int) colWidth;
					}
				}


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
					var brushBurgerLines =
						new SolidBrush(
							Color.FromArgb(
								_hoveringAboveDragBurgerIndex == i ? 100 : 40,
								Color.Black));

					// the lines
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
				int totalItemsHeight = (_items.Count + 1) * _itemHeight;
				int clientHeight = this.ClientRectangle.Height - 7;

				// Calculate the length and position of the scrollbar
				float scrollBarRatio = (float) clientHeight / (float) totalItemsHeight;

				int scrollBarHeight =
					scrollBarRatio > 1 ?
					clientHeight :
					(int) (clientHeight * scrollBarRatio);

				int scrollBarPos = 3 + (int) ((float) _scrollOffset / totalItemsHeight * clientHeight);
				
				// white background
				DrawRoundedRectangle(
					g,
					new Rectangle(Width - 11, scrollBarPos - 3, 10, scrollBarHeight + 6),
					4f,
					null,
					new SolidBrush(Color.FromArgb(230, BackColor)));

				// actual scrollbar
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
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			_hoveringAboveCheckBoxIndex = -1;
			_hoveringAboveDragBurgerIndex = -1;
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
			RecalculateColumnWidths();
			Invalidate();
		}

		// ============================================================================
		protected override void OnMouseDown(
			MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			HandleMousePointerPosition(e.Location, true);
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
				var draggedMetaItem = _items[_dragStartIndex.Value];

				_items.RemoveAt(_dragStartIndex.Value);
				_items.Insert(_currentDropIndex, draggedItem);

				// Reset drag state
				_dragStartIndex = null;
				_currentDropIndex = -1;

				// update all sorting indexes
				int index = 0;
				foreach (var item in _items)
				{
					item.SortingIndex = index++;
				}

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

			HandleMousePointerPosition(e.Location);

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

		#region Custom Logic


		// ============================================================================
		private void RecalculateColumnWidths()
		{
			var fixedWidthUsed = 0;
			var percentSum = 0;

			foreach (var column in _columns)
			{
				if (column.IsFixedWidth)
				{
					fixedWidthUsed += column.GetWithInPixels();
				}
				else
				{
					percentSum += column.GetWithInPixels(100);
				}
			}

			_dynamicColumnSpace = GetSpaceForColumns() - fixedWidthUsed;
		}


		// ============================================================================
		private int GetSpaceForColumns(
			int leftMargin = -1)
		{
			if (leftMargin == -1)
			{
				leftMargin = 10 + (_isCheckable ? _checkBoxSize + 10 : 0);
			}

			return
				this.Width -
				leftMargin -
				(_isSortable ? (_dragBurgerSize * 2) + 6 : 0) -
				(_showScrollBar ? 8 : 0);
		}


		// ============================================================================
		private void ClampScrollOffset()
		{
			_scrollOffset =
				Math.Max(
					0,
					Math.Min(
						_scrollOffset,
						Math.Max(
							0,
							(Items.Count * _itemHeight) - (Height - _itemHeight))));

			_scrollOffset = RoundUpToNextMultiple(_scrollOffset, _itemHeight);
		}


		// ============================================================================
		int RoundUpToNextMultiple(int number, int stepSize)
		{
			return ((number + stepSize - 1) / stepSize) * stepSize;
		}


		// ============================================================================
		private int GetItemAtPoint(
			Point location)
		{
			// Calculate which item is at the given location
			for (int i = 0; i < _items.Count; i++)
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
					(index * _itemHeight) + _itemHeight - _scrollOffset,
					Width,
					_itemHeight);
		}

		// ============================================================================
		public void SetItemChecked(
			int index,
			bool state)
		{
			if (index >= 0 &&
				index < _items.Count)
			{
				_items[index].IsChecked = state;
			}
		}

		// ============================================================================
		public void CheckAllItems()
		{
			foreach (var item in _items)
			{
				item.IsChecked = true;
			}

			OnItemChecked();
			Invalidate();
		}

		// ============================================================================
		public void UnCheckAllItems()
		{
			foreach (var item in _items)
			{
				item.IsChecked = false;
			}

			OnItemChecked();
			Invalidate();
		}

		// ============================================================================
		public void InvertCheckOfAllItems()
		{
			foreach (var item in _items)
			{
				item.IsChecked = !item.IsChecked;
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
			if (_selectedIndex < _items.Count - 1)
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

			_items[_selectedIndex].Toggle();
			OnItemChecked();
			Invalidate();

		}

		// ============================================================================
		public new void Invalidate()
		{
			CheckIfBackgroundImageNeedsShowing();

			base.Invalidate();
		}

		// ============================================================================
		public void DeselectAll()
		{
			_selectedIndex = -1;
			Invalidate();
		}

		// ============================================================================
		public void SortAlphabetically(
			SortOrder sortOrder = SortOrder.Ascending)
		{

			if (sortOrder == SortOrder.Ascending)
			{
				_items = _items.OrderBy(item => item.Title).ToList();
			}
			else
			{
				_items = _items.OrderByDescending(item => item.Title).ToList();
			}

			// update the sorting indexes as well:
			for (int i = 0; i < _items.Count; i++)
			{
				_items[i].SortingIndex = i;
			}

			Invalidate();
		}

		// ============================================================================
		public void EnsureItemVisible(
			int index)
		{
			if (index < 0 ||
				index > _items.Count)
			{
				return;
			}

			int itemTop = (index * _itemHeight) + _itemHeight;
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
		private void CheckIfBackgroundImageNeedsShowing()
		{
			var noData =
				_items == null ||
				_items.Count == 0;

			var supposedBackground = noData ? _noDataImage : null;

			if (supposedBackground != base.BackgroundImage)
			{
				base.BackgroundImage = supposedBackground;
				Invalidate();
			}
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
		private void HandleMousePointerPosition(
			Point pointerLocation,
			bool isClick = false)
		{
			int startIndex = _scrollOffset / _itemHeight;
			int endIndex = Math.Min(_items.Count, startIndex + ((Height - _itemHeight) / _itemHeight));

			var _hoverCheckBoxIndexOld = _hoveringAboveCheckBoxIndex;
			var _hoverDragBurgerIndexOld = _hoveringAboveDragBurgerIndex;

			_hoveringAboveCheckBoxIndex = -1;
			_hoveringAboveDragBurgerIndex = -1;

			// check check-boxes
			if (_isCheckable)
			{
				for (int i = startIndex; i < endIndex; i++)
				{
					int yPosition = (i * _itemHeight) + _itemHeight - _scrollOffset;

					var checkBoxBoundsCheckBox =
						new Rectangle(10, yPosition, _checkBoxSize, _checkBoxSize);

					if (checkBoxBoundsCheckBox.Contains(pointerLocation))
					{
						if (isClick)
						{
							_items[i].Toggle();
							OnItemChecked();
						}
						else
						{
							_hoveringAboveCheckBoxIndex = i;
							if (_hoveringAboveCheckBoxIndex != _hoverCheckBoxIndexOld)
							{
								Invalidate();
							}
						}

						// Leave - we are done here...
						return;
					}
				}
			}


			if (isClick)
			{
				// deselect all
				_selectedIndex = -1;
			}


			// check drag-burger
			if (_isSortable)
			{
				for (int i = startIndex; i < endIndex; i++)
				{
					int yPosition = (i * _itemHeight) + _itemHeight - _scrollOffset;

					var checkBoxBoundsCheckBox =
						new RectangleF(
							Width - 15f - (_dragBurgerSize * 2f),
							yPosition,
							_dragBurgerSize * 2f,
							_itemHeight);

					if (checkBoxBoundsCheckBox.Contains(pointerLocation))
					{
						if (isClick)
						{
							_dragStartIndex = i;
						}
						else
						{
							_hoveringAboveDragBurgerIndex = i;
							if (_hoveringAboveDragBurgerIndex != _hoverDragBurgerIndexOld)
							{
								Invalidate();
							}
						}

						// Leave - we are done here...
						return;
					}
				}
			}

			if (!isClick)
			{
				if (_hoveringAboveCheckBoxIndex != _hoverCheckBoxIndexOld ||
					_hoveringAboveDragBurgerIndex != _hoverDragBurgerIndexOld)
				{
					Invalidate();
				}

				return;
			}


			// check hole row
			for (int i = startIndex; i < endIndex; i++)
			{
				int yPosition = (i * _itemHeight) + _itemHeight - _scrollOffset;

				var checkBoxBoundsRow =
					new Rectangle(0, yPosition, Width, _itemHeight);

				if (checkBoxBoundsRow.Contains(pointerLocation))
				{
					_selectedIndex = i;
					break;
				}
			}

			OnSelectedIndexChanged();
		}


		#endregion

	}

	#region Supporting Classes

	// ============================================================================
	// ============================================================================
	// ============================================================================
	[Serializable]
	public class SortableCheckItem : IComparable<SortableCheckItem>
	{
		private int _objectHashCode = 0;

		private object _item;
		private bool _isIdentified;
		private bool _isChecked;
		private int _sortingIndex;
		private string _title;
		private Image _icon;




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
			_title = item.ToString();
		}

		// ============================================================================
		public SortableCheckItem(
			object item,
			int sortingIndex,
			Image icon)
		{
			_objectHashCode = item.GetHashCode();
			_item = item;
			_sortingIndex = sortingIndex;
			_isIdentified = true;
			_isChecked = false;
			_title = item.ToString();
			_icon = icon;
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
			set
			{
				_item = value;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Image Icon
		{
			get
			{
				return _icon;
			}
			set
			{
				_icon = value;
			}
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


	// ============================================================================
	// ============================================================================
	// ============================================================================
	[Serializable]
	public class ColumnDefinition
	{
		private string _widthString = "100px";
		private int _widthNumber = 100;
		private string _header = string.Empty;
		private string _propertyName = string.Empty;

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Accepts string definitions of the with in a few formats:
		/// ###% or ###px or ### --> ###px
		/// </summary>
		public string Width
		{
			get
			{
				return _widthString;
			}
			set
			{
				if (TryParseWidthString(
					value, 
					out string widthString, 
					out int widthNumber))
				{
					_widthString = widthString;
					_widthNumber = widthNumber;
				}
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Header
		{
			get
			{
				return _header;
			}
			set
			{
				_header = value;
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string PropertyName
		{
			get
			{
				return _propertyName;
			}
			set
			{
				_propertyName = value;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsFixedWidth
		{
			get
			{
				return !_widthString.EndsWith("%");
			}
		}



		// ============================================================================
		public int GetWithInPixels(
			int clientWith = 0)
		{
			if (_widthString.EndsWith("px"))
			{
				return _widthNumber;
			}

			else if (_widthString.EndsWith("%"))
			{
				return (int)((_widthNumber / 100f) * clientWith);
			}

			return _widthNumber;
		}


		// ============================================================================
		private bool TryParseWidthString(
			string widthString,
			out string resultString,
			out int resultNumber)
		{
			widthString = widthString.Trim().ToLower();
			var workString = widthString;

			if (workString.EndsWith("px"))
			{
				workString = workString.Remove(workString.Length - 2, 2);
			}

			else if (workString.EndsWith("%"))
			{
				workString = workString.Remove(workString.Length - 1, 1);
			}

			var isSuccess = 
				int.TryParse(workString, out int widthNumber);

			if (isSuccess)
			{
				resultString = widthString;
				resultNumber = widthNumber;
			}
			else
			{
				resultString = string.Empty;
				resultNumber = -1;
			}

			return isSuccess;
		}

	}


	#endregion
}
