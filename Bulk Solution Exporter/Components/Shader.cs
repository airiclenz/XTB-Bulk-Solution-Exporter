using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Components
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public partial class Shader : Control
	{

		private int _opacity = 50;
		private Color _color;

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int Opacity
		{
			get
			{
				return _opacity;
			}
			set
			{
				if (value >= 0 && value <= 100)
				{
					_opacity = value;
					Invalidate();
				}
			}
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public Color Color
		{
			get
			{
				return _color;
			}
			set
			{
				_color = value;
				Invalidate();
			}

		}



		// ============================================================================
		public Shader()
		{
			InitializeComponent();

			SuspendLayout();

			Opacity = 50;
			Color = SystemColors.Control;

			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			ResumeLayout(false);

		}

		// ============================================================================
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			int opacityValue = (int) (_opacity * 2.55f);
			var newColor = Color.FromArgb(opacityValue, _color);

			BackColor = newColor;

			using (SolidBrush brush = new SolidBrush(newColor))
			{
				e.Graphics.FillRectangle(brush, ClientRectangle);
			}
		}


	}
}
