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
			string startPath,
			out string message)
		{
			message = "starting path / filename-path: " + startPath + Environment.NewLine;

			startPath = Path.GetDirectoryName(startPath);
			var currentRootPath = startPath;

			message += "pure cleaned path:             " + currentRootPath + Environment.NewLine;


			while (true)
			{
				string[] folders;
				message += "--------------------------------------------------------------------" + Environment.NewLine;
				message += "current path:                  " + currentRootPath + Environment.NewLine;

				// Load all subfolders 
				if (IsValidPath(currentRootPath) &&
					Directory.Exists(currentRootPath))
				{
					folders = Directory.GetDirectories(currentRootPath);
					message += "found sub directories:         " + folders.Length + Environment.NewLine;
				}
				else
				{
					folders = null;
					message += "found sub directories:         null" + Environment.NewLine;
				}

				// could we retrieve the contained directories?
				if (folders != null)
				{
					foreach (var folderPath in folders)
					{
						message += "scanning subdirectory:         " + folderPath + Environment.NewLine;

						var folderName = Path.GetFileName(folderPath);

						if (folderName.ToLower() == ".git")
						{
							message += ".git found!";
							return currentRootPath;
						}
					}
				}

				message += "moving one level up if possible..." + Environment.NewLine;

				// move one folder up
				var oneLevelUp =
					Path.GetFullPath(
						Path.Combine(
							currentRootPath, @"..\"));

				if (oneLevelUp != currentRootPath)
				{
					currentRootPath = oneLevelUp;
				}
				else
				{
					break;
				}
			}

			message += "no git root was found!";
			return string.Empty;
		}


		// ============================================================================
		internal static bool IsValidPath(
			string path)
		{
			try
			{
				return
					!string.IsNullOrWhiteSpace(path)
					&& Path.IsPathRooted(path) // Ensures the path has a root (e.g., "C:\")
					&& path.IndexOfAny(Path.GetInvalidPathChars()) == -1; // Checks for invalid characters
			}
			catch
			{
				return false; // Catch any exceptions for invalid paths
			}
		}
	}
}
