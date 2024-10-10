using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Helpers
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	internal class FileAndFolderHelper
	{
		// ============================================================================
		internal static string FindRepositoryRootPath(
			string startPath)
		{
			startPath = Path.GetDirectoryName(startPath);
			var currentRootPath = startPath;

			bool found = false;

			while (!found)
			{
				var folders = Directory.GetDirectories(currentRootPath);

				foreach (var folderPath in folders)
				{
					var folderName = Path.GetFileName(folderPath);

					if (folderName.ToLower() == ".git")
					{
						found = true;
						return currentRootPath;
					}
				}

				// move one folder up
				var oneLevelUp = Path.GetFullPath(Path.Combine(currentRootPath, @"..\"));

				if (oneLevelUp != currentRootPath)
				{
					currentRootPath = oneLevelUp;
				}
				else
				{
					break;
				}
			}

			return string.Empty;
		}


	}
}
