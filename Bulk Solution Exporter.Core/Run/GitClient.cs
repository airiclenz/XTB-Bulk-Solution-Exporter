using Com.AiricLenz.BulkSolutionExporter.Core.IO;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The production <see cref="IGitClient"/>: runs the git executable in a
	/// repository through <see cref="GitHelper"/>, which never builds a command
	/// line by hand and passes the commit message on standard input.
	/// </summary>
	public sealed class GitClient : IGitClient
	{

		private readonly GitHelper _gitHelper;


		// ============================================================================
		/// <summary>
		/// Creates a client for the repository rooted at the given folder.
		/// </summary>
		/// <param name="repositoryRootPath">The folder that holds the repository's ".git" folder.</param>
		public GitClient(
			string repositoryRootPath)
		{
			_gitHelper = new GitHelper(repositoryRootPath);
		}


		// ============================================================================
		/// <inheritdoc/>
		public bool AddFile(
			string absolutePath,
			out string errorMessage)
		{
			return
				_gitHelper.AddFile(
					absolutePath,
					out errorMessage);
		}


		// ============================================================================
		/// <inheritdoc/>
		public bool Commit(
			string message,
			out string errorMessage)
		{
			return
				_gitHelper.Commit(
					message,
					out errorMessage);
		}


		// ============================================================================
		/// <inheritdoc/>
		public bool Push(
			out string errorMessage)
		{
			return _gitHelper.Push(out errorMessage);
		}

	}
}
