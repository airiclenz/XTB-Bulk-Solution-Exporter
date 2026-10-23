// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// One solution's entry in the answer of <c>files/suggestFileNames</c>:
	/// the export paths the file-location wizard proposes.
	/// </summary>
	public sealed class SuggestedFileNames
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The unique name the paths were built from, as it was sent.
		/// </summary>
		public string UniqueName
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The proposed managed export path, or <c>null</c> when the pair was
		/// refused for resolving outside the folder (keep the current paths).
		/// </summary>
		public string FileNameManaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The proposed unmanaged export path, or <c>null</c> when the pair was
		/// refused for resolving outside the folder (keep the current paths).
		/// </summary>
		public string FileNameUnmanaged
		{
			get; set;
		}

	}
}
