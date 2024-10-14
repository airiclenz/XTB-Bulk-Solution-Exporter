

// ============================================================================
// ============================================================================
// ============================================================================
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;

namespace Com.AiricLenz.XTB.Components
{

    // ============================================================================
    // ============================================================================
    // ============================================================================
    partial class FlipSwitch
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FlipSwitch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "FlipSwitch";
            this.ResumeLayout(false);

        }


		// ============================================================================
		protected override void OnPaint(
			PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			SizeF textSize = g.MeasureString(_title, Font);

			var alpha = _isEnabled ? 255 : 60;

			var fillColor =	_isOn && _isEnabled ? _colorOn : _colorOff;
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
					Color.FromArgb(alpha, (int) (fillColor.R * 0.9f), (int) (fillColor.G * 0.9f), (int) (fillColor.B * 0.9f)),
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

		#endregion
	}
}
