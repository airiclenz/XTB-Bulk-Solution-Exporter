using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Com.AiricLenz.BulkSolutionExporter.Core.IO;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;
using StreamJsonRpc;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The JSON-RPC methods the tool's UI calls outside a run: the git branch
	/// of the export folder, the export files' status, the solution CSV and
	/// the file-location wizard's names. Same wire format as
	/// <see cref="WorkerMethods"/>.
	/// <para>
	/// Every path is checked before use: file paths must be absolute, end in
	/// a valid file name and hold no ".." segment; folder paths must be
	/// absolute and hold no ".." segment. A refused input, a missing
	/// repository or a failed git command is returned as the request's error.
	/// </para>
	/// </summary>
	public sealed class WorkspaceMethods
	{

		private const string ParentDirectorySegment = "..";
		private const string InvalidFilePathMessage = "The file path must be an absolute path to a file, without '..' segments.";
		private const string InvalidFolderPathMessage = "The folder path must be an absolute path, without '..' segments.";
		private const string NullEntryMessage = "The list must not hold empty entries.";
		private const string NoRepositoryMessage = "The file is not inside a git repository.";
		private const string UnknownBranchMessage = "The branch is not a local branch of the repository.";
		private const string RunActiveMessage = "The branch cannot be switched while a run is in progress.";

		private static readonly char[] PathSeparators = { '/', '\\' };

		private readonly Func<bool> _isRunActive;


		// ============================================================================
		/// <summary>
		/// Creates the methods for a connection.
		/// </summary>
		/// <param name="isRunActive">Tells whether a run is active; a branch switch is refused while it is.</param>
		/// <exception cref="ArgumentNullException"><paramref name="isRunActive"/> is null.</exception>
		public WorkspaceMethods(
			Func<bool> isRunActive)
		{
			_isRunActive = isRunActive ?? throw new ArgumentNullException(nameof(isRunActive));
		}


		// ============================================================================
		/// <summary>
		/// Lists the local branches of the git repository that holds a file,
		/// found by walking up from the file's folder.
		/// </summary>
		/// <param name="filePath">An export file path inside the repository; the file need not exist.</param>
		/// <returns>The repository's branches and its checked-out branch; an
		/// empty answer (no root, no branches) when the file is in no repository.</returns>
		/// <exception cref="ArgumentException">The file path is refused.</exception>
		[JsonRpcMethod("git/branches")]
		public GitBranches GetBranches(
			string filePath)
		{
			RequireSafeFilePath(filePath, nameof(filePath));

			var repositoryRoot = FindRepositoryRoot(filePath);

			if (repositoryRoot == null)
			{
				return new GitBranches();
			}

			return ReadBranches(new GitHelper(repositoryRoot));
		}


		// ============================================================================
		/// <summary>
		/// Checks out another local branch in the git repository that holds a file.
		/// </summary>
		/// <param name="filePath">An export file path inside the repository; the file need not exist.</param>
		/// <param name="branchName">One of the branches <c>git/branches</c> lists.</param>
		/// <returns>The repository's branches after the switch.</returns>
		/// <exception cref="ArgumentException">The file path is refused, or the branch is not a local branch.</exception>
		/// <exception cref="InvalidOperationException">A run is active, the file is
		/// in no repository, or git failed (the message carries git's error).</exception>
		[JsonRpcMethod("git/switchBranch")]
		public GitBranches SwitchBranch(
			string filePath,
			string branchName)
		{
			RequireSafeFilePath(filePath, nameof(filePath));

			if (_isRunActive())
			{
				throw new InvalidOperationException(RunActiveMessage);
			}

			var repositoryRoot =
				FindRepositoryRoot(filePath) ??
				throw new InvalidOperationException(NoRepositoryMessage);

			var gitHelper = new GitHelper(repositoryRoot);

			if (!gitHelper.GetAllBranches().Contains(branchName, StringComparer.Ordinal))
			{
				throw new ArgumentException(UnknownBranchMessage, nameof(branchName));
			}

			if (!gitHelper.SwitchToBranch(branchName, out var errorMessage))
			{
				throw new InvalidOperationException(errorMessage);
			}

			return ReadBranches(gitHelper);
		}


		// ============================================================================
		/// <summary>
		/// Determines each solution's export file state, in the given order:
		/// configured, present, the version <c>exported_versions.json</c>
		/// records and whether the file is outdated (see <see cref="SolutionFileStatus.Evaluate"/>).
		/// </summary>
		/// <param name="solutions">The solutions with their current version and file paths.</param>
		/// <param name="shouldSaveVersionJson">The "save version JSON" setting; without it no file is outdated.</param>
		/// <exception cref="ArgumentNullException"><paramref name="solutions"/> is null.</exception>
		/// <exception cref="ArgumentException">An entry, its solution or its configuration is null.</exception>
		[JsonRpcMethod("files/status")]
		public List<SolutionFileStatus> GetFileStatus(
			RunSolution[] solutions,
			bool shouldSaveVersionJson)
		{
			if (solutions == null)
			{
				throw new ArgumentNullException(nameof(solutions));
			}

			if (solutions.Any(entry => entry?.Solution == null || entry.Configuration == null))
			{
				throw new ArgumentException(NullEntryMessage, nameof(solutions));
			}

			return solutions
				.Select(entry => SolutionFileStatus.Evaluate(
					entry.Solution,
					entry.Configuration,
					shouldSaveVersionJson))
				.ToList();
		}


		// ============================================================================
		/// <summary>
		/// Writes the solution information CSV (<c>Unique Name, Solution Guid,
		/// Version</c>) for the solutions, in the given order; an existing
		/// file is overwritten.
		/// </summary>
		/// <param name="filePath">The CSV file to write; its folder must exist.</param>
		/// <param name="solutions">The solutions to list.</param>
		/// <exception cref="ArgumentNullException"><paramref name="solutions"/> is null.</exception>
		/// <exception cref="ArgumentException">The file path is refused, or an entry is null.</exception>
		/// <exception cref="IOException">The file cannot be written.</exception>
		/// <exception cref="UnauthorizedAccessException">Writing the file is not allowed.</exception>
		[JsonRpcMethod("files/exportCsv")]
		public void ExportCsv(
			string filePath,
			SolutionInfo[] solutions)
		{
			RequireSafeFilePath(filePath, nameof(filePath));

			if (solutions == null)
			{
				throw new ArgumentNullException(nameof(solutions));
			}

			if (solutions.Contains(null))
			{
				throw new ArgumentException(NullEntryMessage, nameof(solutions));
			}

			SolutionCsv.Write(filePath, solutions);
		}


		// ============================================================================
		/// <summary>
		/// Proposes the file-location wizard's export paths: per solution, the
		/// folder joined with <see cref="FileAndFolderHelper.GetDefaultSolutionFileName"/>
		/// for both exports, in the given order. The names are sanitized, so a
		/// crafted unique name cannot leave the folder; a pair that would still
		/// resolve outside it is answered with null paths.
		/// </summary>
		/// <param name="folderPath">The folder the files go to.</param>
		/// <param name="uniqueNames">The solutions' unique names.</param>
		/// <exception cref="ArgumentNullException"><paramref name="uniqueNames"/> is null.</exception>
		/// <exception cref="ArgumentException">The folder path is refused.</exception>
		[JsonRpcMethod("files/suggestFileNames")]
		public List<SuggestedFileNames> SuggestFileNames(
			string folderPath,
			string[] uniqueNames)
		{
			if (!FileAndFolderHelper.IsValidPath(folderPath) ||
				HasParentSegment(folderPath))
			{
				throw new ArgumentException(InvalidFolderPathMessage, nameof(folderPath));
			}

			if (uniqueNames == null)
			{
				throw new ArgumentNullException(nameof(uniqueNames));
			}

			return uniqueNames
				.Select(uniqueName => SuggestFileNamesFor(folderPath, uniqueName))
				.ToList();
		}


		// ============================================================================
		/// <summary>
		/// Builds one solution's proposed export paths; both are null when
		/// either would resolve outside the folder.
		/// </summary>
		private static SuggestedFileNames SuggestFileNamesFor(
			string folderPath,
			string uniqueName)
		{
			var fileNameManaged =
				Path.Combine(
					folderPath,
					FileAndFolderHelper.GetDefaultSolutionFileName(uniqueName, true));

			var fileNameUnmanaged =
				Path.Combine(
					folderPath,
					FileAndFolderHelper.GetDefaultSolutionFileName(uniqueName, false));

			var isInsideFolder =
				FileAndFolderHelper.IsInsideFolder(fileNameManaged, folderPath) &&
				FileAndFolderHelper.IsInsideFolder(fileNameUnmanaged, folderPath);

			return new SuggestedFileNames
			{
				UniqueName = uniqueName,
				FileNameManaged = isInsideFolder ? fileNameManaged : null,
				FileNameUnmanaged = isInsideFolder ? fileNameUnmanaged : null
			};
		}


		// ============================================================================
		/// <summary>
		/// Reads the branch list and the checked-out branch of a repository.
		/// </summary>
		private static GitBranches ReadBranches(
			GitHelper gitHelper)
		{
			return new GitBranches
			{
				RepositoryRoot = gitHelper.GitRootDirectory,
				ActiveBranch = gitHelper.GetActiveBranch(),
				Branches = gitHelper.GetAllBranches()
			};
		}


		// ============================================================================
		/// <summary>
		/// Finds the root of the git repository that holds a file.
		/// </summary>
		/// <returns>The repository root, or <c>null</c> when there is none.</returns>
		private static string FindRepositoryRoot(
			string filePath)
		{
			var repositoryRoot =
				FileAndFolderHelper.FindRepositoryRootPath(
					filePath,
					out _);

			return
				string.IsNullOrEmpty(repositoryRoot) ?
				null :
				repositoryRoot;
		}


		// ============================================================================
		/// <summary>
		/// Refuses a file path that is not absolute, does not end in a valid
		/// file name or holds a ".." segment.
		/// </summary>
		/// <exception cref="ArgumentException">The path is refused.</exception>
		private static void RequireSafeFilePath(
			string filePath,
			string parameterName)
		{
			if (!FileAndFolderHelper.IsValidSolutionFilePath(filePath) ||
				HasParentSegment(filePath))
			{
				throw new ArgumentException(InvalidFilePathMessage, parameterName);
			}
		}


		// ============================================================================
		/// <summary>
		/// Whether any segment of the path is "..", on either separator.
		/// </summary>
		private static bool HasParentSegment(
			string path)
		{
			return path
				.Split(PathSeparators)
				.Contains(ParentDirectorySegment, StringComparer.Ordinal);
		}

	}
}
