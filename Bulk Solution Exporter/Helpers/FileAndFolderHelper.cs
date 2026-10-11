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
		// Name used when sanitizing leaves nothing of the original text.
		internal const string FallbackFileNamePart = "Solution";

		private const char InvalidCharReplacement = '_';
		private const string ParentDirectoryToken = "..";


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


		// ============================================================================
		/// <summary>
		/// Turns untrusted text (e.g. a Dataverse solution unique name) into a
		/// single file-name segment: invalid file-name characters become '_',
		/// every ".." is removed, and leading dots plus trailing dots/spaces are
		/// trimmed. The result never contains a path separator, a drive prefix
		/// or a parent-directory reference.
		/// </summary>
		/// <returns>
		/// The sanitized segment, or <see cref="FallbackFileNamePart"/> when
		/// nothing usable remains.
		/// </returns>
		internal static string ToSafeFileNamePart(
			string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return FallbackFileNamePart;
			}

			var invalidChars = Path.GetInvalidFileNameChars();
			var builder = new StringBuilder(text.Length);

			foreach (var character in text)
			{
				builder.Append(
					Array.IndexOf(invalidChars, character) >= 0 ?
					InvalidCharReplacement :
					character);
			}

			var safeText = builder.ToString();

			while (safeText.Contains(ParentDirectoryToken))
			{
				safeText = safeText.Replace(ParentDirectoryToken, string.Empty);
			}

			safeText = safeText.TrimStart('.').TrimEnd('.', ' ').Trim();

			return
				safeText.Length == 0 ?
				FallbackFileNamePart :
				safeText;
		}


		// ============================================================================
		/// <summary>
		/// Checks that a solution file path is rooted and ends in a non-empty,
		/// valid file name. Never throws: any path the framework rejects is
		/// reported as invalid.
		/// </summary>
		internal static bool IsValidSolutionFilePath(
			string path)
		{
			try
			{
				if (!IsValidPath(path))
				{
					return false;
				}

				var fileName = Path.GetFileName(path);

				return
					!string.IsNullOrWhiteSpace(fileName)
					&& fileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
			}
			catch
			{
				return false; // Catch any exceptions for invalid paths
			}
		}
	}
}
