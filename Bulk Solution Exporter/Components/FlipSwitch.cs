using System;
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
	public partial class FlipSwitch : Control
	{

		private bool _isOn;
		private bool _isLocked;
		private string _title;
		private int _switchWidth;
		private int _switchHeight;
		private int _marginText;
		private Color _colorOn;
		private Color _colorOff;
		private bool _isEnabled;
		private bool _textOnLeftSide;


		// ============================================================================
		public FlipSwitch()
		{
			InitializeComponent();

			SuspendLayout();

			DoubleBuffered = true;

			//this.Size = new Size(50, 25);
			_isOn = false;
			_isEnabled = true;
			_textOnLeftSide = false;

			_switchWidth = 40;
			_switchHeight = 20;
			_marginText = 7;
			_colorOn = Color.FromArgb(0, 30, 150);
			_colorOff = Color.FromArgb(150, 150, 150);
			Title = "Flip Switch";


			Click += FlipSwitch_Click;

			ResumeLayout(false);
		}


		// ##################################################
		// ##################################################

		#region Events

		public event EventHandler Toggled;


		// ============================================================================
		protected override void OnPaint(
			PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			SizeF textSize = g.MeasureString(_title, Font);

			var alpha = _isEnabled ? 255 : 60;

			var fillColor = _isOn && _isEnabled ? _colorOn : _colorOff;
			var brushFill = new SolidBrush(Color.FromArgb(alpha, fillColor.R, fillColor.G, fillColor.B));
			var penFrame = new Pen(Color.FromArgb(_isEnabled ? 80 : 40, 0, 0, 0), _isEnabled ? 1.5f : 1f);
			var brushKnob = new SolidBrush(Color.FromArgb(_isEnabled ? 255 : 100, 255, 255, 255));

			var _xOffset = _textOnLeftSide ? (int) textSize.Width + _marginText + 4 : 0;

			// Draw background with rounded corners
			float radius = (_switchHeight - 1) / 2f;
			float topMargin = (this.Height - _switchHeight) / 2f;
			using (GraphicsPath path = new GraphicsPath())
			{
				path.AddArc(_xOffset, topMargin, radius * 2, radius * 2, 90, 180);
				path.AddArc(_xOffset + _switchWidth - radius * 2, topMargin, radius * 2, radius * 2, 270, 180);
				path.AddLine(_xOffset + _switchWidth - radius, topMargin + _switchHeight - 1, _xOffset + radius, topMargin + _switchHeight - 1);
				path.CloseFigure();

				LinearGradientBrush lgb = new LinearGradientBrush(
					new Point(0, 0),
					new Point(0, _switchHeight),
					Color.FromArgb(alpha, ColorHelper.MixColors(fillColor, 0.11, Color.Black)),
					Color.FromArgb(alpha, fillColor)
				);

				g.FillPath(lgb, path);
				g.DrawPath(penFrame, path);
			}

			// Draw toggle circle
			int circleDiameter = _switchHeight - 5;
			int circleX = IsOn && _isEnabled ? _xOffset + _switchWidth - circleDiameter - 2 : _xOffset + 2;
			g.FillEllipse(brushKnob, circleX, topMargin + 2, circleDiameter, circleDiameter);


			// Draw title text
			var penColor = (Enabled ? ForeColor : SystemColors.ControlDark);
			Brush textBrush = new SolidBrush(penColor);

			if (_textOnLeftSide)
			{
				g.DrawString(_title, Font, textBrush, 0, (Height - Font.Height - 1) / 2);
			}
			else
			{
				g.DrawString(_title, Font, textBrush, _switchWidth + _marginText, (Height - Font.Height - 1) / 2);
			}
		}



		// ============================================================================
		private void FlipSwitch_Click(object sender, EventArgs e)
		{
			if (Enabled)
			{
				IsOn = !IsOn;
				this.Invalidate();
			}
		}


		// ============================================================================
		protected virtual void OnToggled()
		{
			if (Toggled != null)
				Toggled(this, EventArgs.Empty);
		}

		#endregion

		// ##################################################
		// ##################################################

		#region Properties

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsOn
		{
			get
			{
				return _isOn && _isEnabled;
			}
			set
			{
				if (value != _isOn &&
					!_isLocked)
				{
					_isOn = value;
					Invalidate();
					OnToggled();
				}
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsOff
		{
			get
			{
				return !IsOn;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool TextOnLeftSide
		{
			get
			{
				return _textOnLeftSide;
			}
			set
			{
				_textOnLeftSide = value;
				Invalidate();
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int MarginText
		{
			get
			{
				return _marginText;
			}
			set
			{
				_marginText = value;
				Invalidate();
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new Size Size
		{
			get
			{
				return new Size(this.Width, this.Height);
			}
			set
			{
				Height = value.Height;
				this.Width = value.Width;
				Invalidate();
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new bool AutoSize
		{
			get
			{
				return true;
			}
			set
			{
				// nothing
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public new bool Enabled
		{
			get
			{
				return _isEnabled;
			}
			set
			{
				_isEnabled = value;
				Invalidate();
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;

				AdjustWidth();
				AdjustHeight();
				Invalidate();

			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int SwitchWidth
		{
			get
			{
				return _switchWidth;
			}
			set
			{
				_switchWidth = value;

				AdjustWidth();
				Invalidate();

			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int SwitchHeight
		{
			get
			{
				return _switchHeight;
			}
			set
			{
				_switchHeight = value;

				AdjustHeight();
				Invalidate();

			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color ColorOn
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

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color ColorOff
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

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool IsLocked
		{
			get
			{
				return _isLocked;
			}
			set
			{
				_isLocked = value;
			}
		}


		#endregion

		// ##################################################
		// ##################################################

		#region Custom Logic


		// ============================================================================
		private void AdjustWidth()
		{
			using (Graphics g = this.CreateGraphics())
			{
				SizeF textSize = g.MeasureString(_title, this.Font);
				Width = _switchWidth + (int) textSize.Width + _marginText + 5;
			}
		}

		// ============================================================================
		private void AdjustHeight()
		{
			using (Graphics g = this.CreateGraphics())
			{
				SizeF textSize = g.MeasureString(_title, this.Font);
				var textHeight = (int) textSize.Height;

				Height = _switchHeight > textHeight ? _switchHeight + 2 : textHeight + 2;
			}
		}



		#endregion
	}
}
