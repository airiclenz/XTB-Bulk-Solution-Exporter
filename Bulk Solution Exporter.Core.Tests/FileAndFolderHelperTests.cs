using System;
using System.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers <see cref="FileAndFolderHelper.ToSafeFileNamePart"/> and
	/// <see cref="FileAndFolderHelper.IsValidSolutionFilePath"/>, which keep
	/// server-controlled solution names from escaping the export folder, and
	/// the repository-root walk of
	/// <see cref="FileAndFolderHelper.FindRepositoryRootPath"/>.
	/// </summary>
	[TestClass]
	public class FileAndFolderHelperTests
	{
		private const string GitFolderName = ".git";

		private const string SolutionFileName = "Managed.Contoso.zip";

		// ============================================================================
		[TestMethod]
		[DataRow(@"..\..\Windows\win.ini")]
		[DataRow("C:evil")]
		[DataRow("a/b")]
		[DataRow("x:y")]
		public void ToSafeFileNamePart_HostileName_ContainsNoPathElements(
			string hostileName)
		{
			// Act
			var safeName = FileAndFolderHelper.ToSafeFileNamePart(hostileName);

			// Assert
			Assert.IsFalse(safeName.Contains(@"\"), "backslash in " + safeName);
			Assert.IsFalse(safeName.Contains("/"), "slash in " + safeName);
			Assert.IsFalse(safeName.Contains(".."), "parent reference in " + safeName);
			Assert.IsFalse(safeName.Contains(":"), "drive separator in " + safeName);
			Assert.AreEqual(-1, safeName.IndexOfAny(Path.GetInvalidFileNameChars()));
		}


		// ============================================================================
		[TestMethod]
		public void ToSafeFileNamePart_LeadingDot_IsTrimmed()
		{
			// Act
			var safeName = FileAndFolderHelper.ToSafeFileNamePart(".x");

			// Assert
			Assert.IsFalse(safeName.StartsWith("."));
		}


		// ============================================================================
		[TestMethod]
		public void ToSafeFileNamePart_LegitimateName_IsUnchanged()
		{
			// Act
			var safeName = FileAndFolderHelper.ToSafeFileNamePart("Contoso_Core");

			// Assert
			Assert.AreEqual("Contoso_Core", safeName);
		}


		// ============================================================================
		[TestMethod]
		[DataRow("")]
		[DataRow("..")]
		[DataRow(". .")]
		public void ToSafeFileNamePart_NothingUsable_ReturnsFallback(
			string text)
		{
			// Act
			var safeName = FileAndFolderHelper.ToSafeFileNamePart(text);

			// Assert
			Assert.AreEqual(FileAndFolderHelper.FallbackFileNamePart, safeName);
		}


		// ============================================================================
		[TestMethod]
		[DataRow("name.zip")]
		[DataRow(@"sub\name.zip")]
		[DataRow(@"C:\exports\a|b.zip")]
		[DataRow(@"C:\exports\")]
		public void IsValidSolutionFilePath_InvalidPath_ReturnsFalse(
			string path)
		{
			// Act
			var isValid = FileAndFolderHelper.IsValidSolutionFilePath(path);

			// Assert
			Assert.IsFalse(isValid);
		}


		// ============================================================================
		[TestMethod]
		public void IsValidSolutionFilePath_AbsoluteZipPath_ReturnsTrue()
		{
			// Act
			var isValid =
				FileAndFolderHelper.IsValidSolutionFilePath(
					Path.Combine(Path.GetTempPath(), SolutionFileName));

			// Assert
			Assert.IsTrue(isValid);
		}


		// ============================================================================
		[TestMethod]
		public void FindRepositoryRootPath_GitFolderTwoLevelsUp_ReturnsThatFolder()
		{
			string rootPath = CreateTemporaryDirectory();

			try
			{
				// Arrange
				Directory.CreateDirectory(Path.Combine(rootPath, GitFolderName));
				string solutionFolderPath = Path.Combine(rootPath, "solutions", "managed");
				Directory.CreateDirectory(solutionFolderPath);
				string filePath = Path.Combine(solutionFolderPath, SolutionFileName);

				// Act
				string repositoryRootPath =
					FileAndFolderHelper.FindRepositoryRootPath(
						filePath,
						out _);

				// Assert
				Assert.AreEqual(rootPath, repositoryRootPath);
			}
			finally
			{
				Directory.Delete(rootPath, true);
			}
		}


		// ============================================================================
		[TestMethod]
		public void FindRepositoryRootPath_NoRepository_ReturnsEmpty()
		{
			string rootPath = CreateTemporaryDirectory();

			try
			{
				// Arrange
				if (HasGitFolderAbove(rootPath))
				{
					Assert.Inconclusive("The temporary folder lies inside a git repository.");
				}

				string filePath = Path.Combine(rootPath, SolutionFileName);

				// Act
				string repositoryRootPath =
					FileAndFolderHelper.FindRepositoryRootPath(
						filePath,
						out _);

				// Assert
				Assert.AreEqual(string.Empty, repositoryRootPath);
			}
			finally
			{
				Directory.Delete(rootPath, true);
			}
		}


		// ============================================================================
		private static string CreateTemporaryDirectory()
		{
			string directoryPath = Path.Combine(
				Path.GetTempPath(),
				"FileAndFolderHelperTests " + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(directoryPath);
			return directoryPath;
		}


		// ============================================================================
		/// <summary>
		/// Tells whether the folder or one of its ancestors holds a .git
		/// folder, which would make a "no repository" expectation untrue on
		/// this machine.
		/// </summary>
		private static bool HasGitFolderAbove(
			string directoryPath)
		{
			for (var directory = new DirectoryInfo(directoryPath);
				directory != null;
				directory = directory.Parent)
			{
				if (Directory.Exists(Path.Combine(directory.FullName, GitFolderName)))
				{
					return true;
				}
			}

			return false;
		}

	}
}
