// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The git operations of a <see cref="RunEngine"/> run's git phase, on one
	/// repository. <see cref="GitClient"/> is the production client. A failed
	/// operation returns false with git's error text; none of them throws for
	/// a git failure.
	/// </summary>
	public interface IGitClient
	{

		// ============================================================================
		/// <summary>
		/// Stages one file.
		/// </summary>
		/// <param name="absolutePath">The full path of a file inside the repository.</param>
		/// <param name="errorMessage">Git's error text; empty on success.</param>
		/// <returns>True when the file was staged.</returns>
		bool AddFile(
			string absolutePath,
			out string errorMessage);


		// ============================================================================
		/// <summary>
		/// Commits the staged changes.
		/// </summary>
		/// <param name="message">The commit message; never <c>null</c>.</param>
		/// <param name="errorMessage">Git's error text; empty on success.</param>
		/// <returns>True when the commit was created.</returns>
		bool Commit(
			string message,
			out string errorMessage);


		// ============================================================================
		/// <summary>
		/// Pushes the checked-out branch to its upstream.
		/// </summary>
		/// <param name="errorMessage">Git's error text; empty on success.</param>
		/// <returns>True when the push succeeded.</returns>
		bool Push(
			out string errorMessage);

	}
}
