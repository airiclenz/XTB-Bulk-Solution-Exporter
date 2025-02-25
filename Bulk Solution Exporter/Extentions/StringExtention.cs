using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.Extentions
{

    // ============================================================================
    // ============================================================================
    // ============================================================================
    public static class StringExtention
    {

		// ============================================================================
		/// <summary>
		/// Checks if a string value has content or is empty or null.
		/// </summary>
		/// <param name="inputString"></param>
		/// <returns>Return true if the string is null, empty or contains only white-spaces.</returns>
		public static bool IsEmpty(
			this string inputString)
		{
			return string.IsNullOrWhiteSpace(inputString);
		}

		// ============================================================================
		public static bool HasValue(
			this string inputString)
		{
			return !string.IsNullOrWhiteSpace(inputString);
		}


		// ============================================================================
		public static string ForceToLength(
            this string inputString,
            int length,
            char spacer = ' ')
        {
            if (inputString == null)
            {
                return null;
            }

            var resultString = inputString;
            var diff = length - inputString.Length;

            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    resultString += spacer;
                }
            }

            if (diff < 0)
            {
                resultString = inputString.Substring(0, length);
            }

            return resultString;
        }


    }
}
