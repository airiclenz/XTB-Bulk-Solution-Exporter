using System.Collections.Generic;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The answer of <c>git/branches</c> and <c>git/switchBranch</c>: a
	/// repository's local branches and the checked-out one.
	/// </summary>
	public sealed class GitBranches
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The repository's root folder, or <c>null</c> when the file is in no repository.
		/// </summary>
		public string RepositoryRoot
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The checked-out branch; empty when there is none (no repository,
		/// a detached HEAD or a failed git command).
		/// </summary>
		public string ActiveBranch
		{
			get; set;
		} = string.Empty;


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The local branch names in git's order; empty when git listed none.
		/// </summary>
		public List<string> Branches
		{
			get; set;
		} = new List<string>();

	}
}
