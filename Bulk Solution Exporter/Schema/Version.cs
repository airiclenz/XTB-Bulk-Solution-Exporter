using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class Version
	{
		private int[] _version = { -1, -1, -1, -1 };
		private int[] _versionDigits = { 1, 1, 1, 1 };

		private string _versionString;


		// ============================================================================
		public Version(
			string version)
		{
			_versionString = version;

			var parts = version.Split('.');

			for (int i = 0; i < parts.Length; i++)
			{
				if (int.TryParse(parts[i], out int number))
				{
					_version[i] = number;
				}
			}
		}


		// ============================================================================
		public bool IncreaseVersionNumber(
			string format = "YYYY.MM.DD.+")
		{
			format = format.ToUpper().Trim();


			if (!ValidateFormat(format))
			{
				return false;
			}

			var formatParts =
				format.Split('.');

			for (int i = 0; i < 4; i++)
			{
				if (i > formatParts.Length)
				{
					_version[i] = -1;
				}

				var partString = formatParts[i];
				var dateNow = DateTime.Now;

				// TOKEN - INTERPRETATION

				// # ==> nothing - keep the number;
				if (partString == "#")
				{
					// ...                    
				}

				// + ==> increase the number by 1
				if (partString == "+")
				{
					_version[i]++;
				}

				// YYYY ==> current year
				if (partString == "YYYY")
				{
					_version[i] = dateNow.Year;
					_versionDigits[i] = 4;
				}

				// MM ==> current month
				if (partString == "MM")
				{
					_version[i] = dateNow.Month;
					_versionDigits[i] = 2;
				}

				// DD ==> current day
				if (partString == "DD")
				{
					_version[i] = dateNow.Day;
					_versionDigits[i] = 2;
				}




			}

			UpdateVersionString();
			return true;
		}



		// ============================================================================
		// Overriding the ToString method
		public override string ToString()
		{
			return _versionString;
		}

		// ============================================================================
		private void UpdateVersionString()
		{

			_versionString = string.Empty;

			for (int i = 0; i < 4; i++)
			{
				_versionString +=
					(i > 0 ? "." : "") +
					(_version[i] == -1 ? "" : _version[i].ToString("D" + _versionDigits[i]));
			}
		}



		// ============================================================================
		public static bool ValidateFormat(
			string format)
		{
			var formatParts =
				format.Split('.');

			if (formatParts.Length > 4 ||
				formatParts.Length < 2)
			{
				return false;
			}

			foreach (var part in formatParts)
			{
				bool found = false;

				foreach (var token in VersionTokens.AllowedTokens)
				{
					if (part == token)
					{
						found = true;
						continue;
					}
				}

				if (!found)
				{
					return false;
				}
			}

			return true;
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	class VersionTokens
	{

		public static readonly string[] AllowedTokens =
		{
			NumberIncreased,
			NumberUnchanged,
			Year,
			Month,
			Day
		};

		public const string NumberIncreased = "+";
		public const string NumberUnchanged = "#";
		public const string Year = "YYYY";
		public const string Month = "MM";
		public const string Day = "DD";

	}
}
