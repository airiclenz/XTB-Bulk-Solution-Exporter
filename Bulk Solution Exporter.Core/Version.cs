using System;



// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// A solution version of two to four numeric parts that can be advanced
	/// by a format of <see cref="VersionTokens"/>.
	/// </summary>
	public class Version
	{
		// Marks a version slot that holds no part (absent or unparsed).
		private const int UnsetPart = -1;

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
					//_versionDigits[i] = 2;
				}
			}

			//UpdateVersionString();
		}


		// ============================================================================
		/// <summary>
		/// Advances the version by <paramref name="format"/>: one part per
		/// format part, each part kept, increased or set from the date.
		/// </summary>
		/// <param name="format">Two to four <see cref="VersionTokens"/> joined by dots.</param>
		/// <param name="today">The date the YYYY, MM and DD tokens read; <see cref="DateTime.Now"/> when null.</param>
		/// <returns>False, with the version unchanged, when the format is invalid.</returns>
		public bool IncreaseVersionNumber(
			string format = "YYYY.MM.DD.+",
			DateTime? today = null)
		{
			format = format.ToUpper().Trim();
			var dateNow = today ?? DateTime.Now;


			if (!ValidateFormat(format))
			{
				return false;
			}

			var formatParts =
				format.Split('.');

			for (int i = 0; i < 4; i++)
			{
				// The result has exactly as many parts as the format.
				if (i >= formatParts.Length)
				{
					_version[i] = UnsetPart;
					continue;
				}

				// A part the source version lacks reads as 0.
				if (_version[i] == UnsetPart)
				{
					_version[i] = 0;
				}

				var partString = formatParts[i];

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
				if (_version[i] == UnsetPart)
				{
					break;
				}

				var versionPartLen =
					 _version[i].ToString().Length;

				var digits =
					Math.Max(versionPartLen, _versionDigits[i]);
				
				_versionString +=
					(i > 0 ? "." : "") +
					_version[i].ToString("D" + digits);
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
	/// <summary>
	/// The tokens a version format is built from.
	/// </summary>
	public class VersionTokens
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
