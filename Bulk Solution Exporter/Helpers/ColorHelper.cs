using System;
using System.Drawing;

// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class ColorHelper
	{

		// ============================================================================
		/// <summary>
		/// Mixes color one and color two with the given bias value. A bias of zero returns color one.
		/// </summary>
		/// <param name="color1"></param>
		/// <param name="bias"></param>
		/// <param name="color2"></param>
		/// <returns></returns>
		public static Color MixColors(
			Color color1, 
			double bias, 
			Color color2)
		{
			// Ensure bias is within the range [0, 1]
			bias = Math.Max(0, Math.Min(1, bias));

			int r = (int) (color1.R * (1 - bias) + color2.R * bias);
			int g = (int) (color1.G * (1 - bias) + color2.G * bias);
			int b = (int) (color1.B * (1 - bias) + color2.B * bias);
			int a = (int) (color1.A * (1 - bias) + color2.A * bias);

			return Color.FromArgb(a, r, g, b);
		}
	}
}
