using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

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

		private List<SortableCheckItem> _items = new List<SortableCheckItem>();
		private int _itemHeight = 20;
		private float _textHeight;
		private float _borderThickness = 1f;
		private Color _borderColor = SystemColors.ControlDark;
		private int _scrollOffset = 0;

		private const string measureText = "XQgjÄÜ#";


		// ============================================================================
		public SortableCheckList()
		{
			InitializeComponent();
			SuspendLayout();

			DoubleBuffered = true;

			BackColor = SystemColors.Window;

			AdjustItemHeight();
			ResumeLayout();
			Invalidate();
		}


		// ##################################################
		// ##################################################

		#region Events


		// ============================================================================
		protected override void OnMouseWheel(
			MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			_scrollOffset -= e.Delta / 120 * _itemHeight; // Adjust scroll speed
			_scrollOffset = Math.Max(0, Math.Min(_scrollOffset, Math.Max(0, (Items.Count * _itemHeight) - Height)));

			Invalidate();
		}

		// ============================================================================
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Invalidate(); 
		}


		#endregion

		// ##################################################
		// ##################################################

		#region Properties


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public List<object> Items
		{
			get { return SortableCheckItem.GetObjectList(_items); } 
			set 
			{
				_items.Clear();

				for (int i = 0; i < value.Count; i++)
				{
					_items.Add(
						new SortableCheckItem(
							value[i],
							i));
				}

				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int ItemHeigth
		{
			get { return _itemHeight;}
			set 
			{ 
				_itemHeight = value;
				AdjustItemHeight();

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
		public int ScrollOffset
		{
			get	{ return _scrollOffset;	}
			set
			{
				_scrollOffset = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color BorderColor
		{
			get { return _borderColor; }
			set
			{
				_borderColor = value;
				Invalidate();
			}
		}

		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public float BorderThickness
		{
			get { return _borderThickness; }
			set
			{
				_borderThickness = value;
				Invalidate();
			}
		}



		#endregion

		// ##################################################
		// ##################################################

		#region Custom Logic

		// ============================================================================
		protected override void OnPaint(
			PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			var penColor = (Enabled ? ForeColor : SystemColors.ControlDark);
			Brush textBrush = new SolidBrush(penColor);
					


			// -------------------------------------
			// paint all items
			int startIndex = ScrollOffset / _itemHeight;
			int endIndex = Math.Min(_items.Count, startIndex + (Height / _itemHeight));
			var marginTop = (_itemHeight - _textHeight) / 2f;

			for (int i = startIndex; i < endIndex; i++)
			{
				int yPosition = (i * _itemHeight) - ScrollOffset;

				g.DrawRectangle(Pens.LightGray, new Rectangle(0, yPosition, this.Width, _itemHeight));

				g.DrawString(_items[i].Object.ToString(), Font, textBrush, new PointF(30, yPosition + marginTop));

			}


			// paint the border
			var borderPen = new Pen(_borderColor, _borderThickness);
			g.DrawRectangle(borderPen, 0, 0, Width - _borderThickness, Height - _borderThickness);


		}


		// ============================================================================
		private void AdjustItemHeight()
		{
			using (Graphics g = CreateGraphics())
			{
				SizeF textSize = g.MeasureString(measureText, Font);
				_textHeight = textSize.Height;
				_itemHeight = _textHeight > _itemHeight ? (int)_textHeight : _itemHeight;
			}
		}


		#endregion



	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class SortableCheckItem
	{
		private bool _isChecked = false;
		private object _object = null;
		private int _sortingIndex = 0;


		// ============================================================================
		public SortableCheckItem(
			object Object,
			int sortingIndex)
		{
			_object = Object;
			_sortingIndex = sortingIndex;
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool Checked
		{
			get { return _isChecked; }
			set
			{
				_isChecked = value;
			}
		}


		//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public object Object
		{
			get
			{
				return _object;
			}
			set
			{
				_object = value;
			}
		}


		// ============================================================================
		public static List<object> GetObjectList(
			List<SortableCheckItem> list)
		{
			var resultList = new List<object>();

			foreach (var item in list)
			{
				resultList.Add(item.Object);
			}

			return resultList;
		}



	}
}
