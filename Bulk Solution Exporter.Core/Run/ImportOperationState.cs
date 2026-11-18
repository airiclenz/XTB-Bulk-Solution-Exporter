// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Where a server-side import operation stands, as
	/// <see cref="IDataverseGateway.GetImportOperationState"/> read it.
	/// </summary>
	public sealed class ImportOperationState
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Whether the operation still runs or how it ended.
		/// </summary>
		public ImportOperationStatus Status
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The server's text about the operation (its friendly message and its
		/// message, one per line); empty or <c>null</c> when the server gave none.
		/// Untrusted text from Dataverse.
		/// </summary>
		public string Message
		{
			get; set;
		}

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The states of a server-side import operation. Every state but
	/// <see cref="Running"/> is final.
	/// </summary>
	public enum ImportOperationStatus
	{
		/// <summary>The operation has not reached a final state yet.</summary>
		Running,

		/// <summary>The import finished successfully.</summary>
		Succeeded,

		/// <summary>The import failed.</summary>
		Failed,

		/// <summary>The operation was canceled.</summary>
		Canceled
	}
}
