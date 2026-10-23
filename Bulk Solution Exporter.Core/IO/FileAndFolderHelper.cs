using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.IO
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class FileAndFolderHelper
	{
		// Name used when sanitizing leaves nothing of the original text.
		public const string FallbackFileNamePart = "Solution";

		private const char InvalidCharReplacement = '_';
		private const string ParentDirectoryToken = "..";

		// Characters Windows refuses in a file name. They are stripped on
		// every platform, so a name sanitized on macOS or Linux is still safe
		// on Windows and never carries a drive separator or a backslash.
		private const string WindowsInvalidFileNameChars = "\"<>|:*?\\/";

		// Every character below the space is a control character, which
		// Windows also refuses in a file name.
		private const char FirstNonControlChar = ' ';

		// The platform's invalid file-name characters plus the Windows set.
		private static readonly char[] InvalidFileNameChars = BuildInvalidFileNameChars();


		// ============================================================================
		public static string FindRepositoryRootPath(
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

				// An empty path has no parent; GetParent would throw on it.
				if (string.IsNullOrEmpty(currentRootPath))
				{
					break;
				}

				// GetParent understands the separators of every platform and
				// returns null above the root, so the walk always ends.
				var parentDirectory = Directory.GetParent(currentRootPath);

				if (parentDirectory == null)
				{
					break;
				}

				currentRootPath = parentDirectory.FullName;
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
		/// single file-name segment: invalid file-name characters (those of the
		/// current platform plus every character Windows refuses) become '_',
		/// every ".." is removed, and leading dots plus trailing dots/spaces are
		/// trimmed. The result never contains a path separator, a drive prefix
		/// or a parent-directory reference.
		/// </summary>
		/// <returns>
		/// The sanitized segment, or <see cref="FallbackFileNamePart"/> when
		/// nothing usable remains.
		/// </returns>
		public static string ToSafeFileNamePart(
			string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return FallbackFileNamePart;
			}

			var builder = new StringBuilder(text.Length);

			foreach (var character in text)
			{
				builder.Append(
					Array.IndexOf(InvalidFileNameChars, character) >= 0 ?
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
		public static bool IsValidSolutionFilePath(
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


		// ============================================================================
		/// <summary>
		/// Joins the current platform's invalid file-name characters with the
		/// Windows set and the control characters, without duplicates.
		/// </summary>
		private static char[] BuildInvalidFileNameChars()
		{
			var controlChars =
				Enumerable
					.Range(0, FirstNonControlChar)
					.Select(code => (char)code);

			return
				Path.GetInvalidFileNameChars()
					.Concat(WindowsInvalidFileNameChars)
					.Concat(controlChars)
					.Distinct()
					.ToArray();
		}
	}
}
