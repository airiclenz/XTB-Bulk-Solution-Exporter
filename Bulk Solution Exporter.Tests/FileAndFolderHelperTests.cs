using System.IO;
using Com.AiricLenz.XTB.Plugin.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Covers <see cref="FileAndFolderHelper.ToSafeFileNamePart"/> and
	/// <see cref="FileAndFolderHelper.IsValidSolutionFilePath"/>, which keep
	/// server-controlled solution names from escaping the export folder.
	/// </summary>
	[TestClass]
	public class FileAndFolderHelperTests
	{

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
					@"C:\exports\Managed.Contoso.zip");

			// Assert
			Assert.IsTrue(isValid);
		}

	}
}
