using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


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
            get { return _isOn && _isEnabled; }
            set
            {
                if (value !=  _isOn)
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
			get { return _textOnLeftSide; }
			set
			{
				_textOnLeftSide = value;
				Invalidate();
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int MarginText
		{
			get { return _marginText; }
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
            get { return new Size(this.Width, this.Height); }
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

				/*
				if (_isEnabled == false)
				{
					_isOn = false;
				}
				*/

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
        private void AdjustWidth()
        {
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(_title, this.Font);
                Width = _switchWidth + (int)textSize.Width + _marginText + 5;
            }
        }

        // ============================================================================
        private void AdjustHeight()
        {
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(_title, this.Font);
                var textHeight = (int)textSize.Height;

                Height = _switchHeight > textHeight ? _switchHeight + 2 : textHeight + 2;
            }
        }

		

		#endregion
	}
}
