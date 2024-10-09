using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ScintillaNET.Style;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Components
{

    // ============================================================================
    // ============================================================================
    // ============================================================================
    public partial class FlipSwitch : UserControl
    {

        private bool _isChecked;
        private string _title;
        private int _switchWidth;
        private int _switchHeight;
        private int _marginTextLeft;
        private Color _colorOn;
        private Color _colorOff;
        private bool _isEnabled;


        // ============================================================================
        public FlipSwitch()
        {
            InitializeComponent();

            this.SuspendLayout();

            this.DoubleBuffered = true;

            //this.Size = new Size(50, 25);
            this._isChecked = false;
            this._isEnabled = true;
            this._switchWidth = 40;
            this._switchHeight = 20;
            this._marginTextLeft = 7;
            this._colorOn = Color.FromArgb(0, 30, 150);
            this._colorOff = Color.FromArgb(150, 150, 150);
            this.Title = "Flip Switch";


            this.Click += FlipSwitch_Click;

            this.ResumeLayout(false);
        }


        // ##################################################
        // ##################################################

        #region Events

        public event EventHandler Toggled;


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
            get { return _isChecked; }
            set
            {
                if (value !=  _isChecked)
                {
                    _isChecked = value;
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
				return !_isChecked;
			}
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		[Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Size Size
        {
            get { return new Size(this.Width, this.Height); }
            set
            {
                this.Height = value.Height;
                this.Width = value.Width;
                Invalidate();
            }
        }

        // ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new bool AutoSize
        {
            get { return true; }
            set
            {
                // nothing
            }
        }

        // ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public new bool Enabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                Invalidate();
            }
        }


        // ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public string Title
        {
            get { return _title; }
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
            get { return _switchWidth; }
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
            get { return _switchHeight; }
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
            get { return _colorOn; }
            set
            {
                _colorOn = value;

                Invalidate();
            }
        }

        // ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public Color ColorOff
        {
            get { return _colorOff; }
            set
            {
                _colorOff = value;

                Invalidate();
            }
        }

        #endregion

        // ##################################################
        // ##################################################

        #region Custom Logic




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
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            var alpha = _isEnabled ? 255 : 60;

            var brushOn = new SolidBrush(Color.FromArgb(alpha, _colorOn.R, _colorOn.G, _colorOn.B));
            var brushOff = new SolidBrush(Color.FromArgb(alpha, _colorOff.R, _colorOff.G, _colorOff.B));
            var brushKnob = new SolidBrush(Color.FromArgb(_isEnabled ? 255 : 150, 255, 255, 255));
            var penFrame = new Pen(Color.FromArgb(_isEnabled ? 80 : 40, 0, 0, 0), _isEnabled ? 1.5f : 1f);

            // Draw background with rounded corners

            float radius = (_switchHeight - 1) / 2f;
            float topMargin = (this.Height - _switchHeight) / 2f;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(0, topMargin, radius * 2, radius * 2, 90, 180);
                path.AddArc(_switchWidth - radius * 2, topMargin, radius * 2, radius * 2, 270, 180);
                path.AddLine(_switchWidth - radius, topMargin + _switchHeight - 1, radius, topMargin + _switchHeight - 1);
                path.CloseFigure();
                g.FillPath(IsOn ? brushOn : brushOff, path);
                g.DrawPath(penFrame, path);
            }

            // Draw toggle circle
            int circleDiameter = _switchHeight - 5;
            int circleX = IsOn ? _switchWidth - circleDiameter - 2 : 2;
            g.FillEllipse(brushKnob, circleX, topMargin + 2, circleDiameter, circleDiameter);

            // Draw title text
            using (Brush textBrush = new SolidBrush(this.ForeColor))
            {
                g.DrawString(_title, this.Font, textBrush, _switchWidth + _marginTextLeft, (this.Height - this.Font.Height - 1) / 2);
            }
        }

        // ============================================================================
        private void AdjustWidth()
        {
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(_title, this.Font);
                this.Width = _switchWidth + (int)textSize.Width + _marginTextLeft + 5;
            }
        }

        // ============================================================================
        private void AdjustHeight()
        {
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(_title, this.Font);
                var textHeight = (int)textSize.Height;

                this.Height = _switchHeight > textHeight ? _switchHeight + 2 : textHeight + 2;
            }
        }

        #endregion
    }
}
