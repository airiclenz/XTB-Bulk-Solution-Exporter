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
		public static bool IsEmpty(
			this string inputString)
		{
			return string.IsNullOrWhiteSpace(inputString);
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
