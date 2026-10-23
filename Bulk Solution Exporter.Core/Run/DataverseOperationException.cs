using System;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// A Dataverse request failed. A host's Dataverse gateway throws it in place
	/// of its own fault type and keeps the server's message text unchanged, which
	/// is what <see cref="RetryRunner"/> classifies retries and failures by.
	/// The message is server text: untrusted, so log it but never execute or
	/// interpret it beyond that classification.
	/// </summary>
	public sealed class DataverseOperationException : Exception
	{

		// ============================================================================
		/// <summary>
		/// Creates the exception from the server's fault message.
		/// </summary>
		/// <param name="message">The server's fault message, unchanged.</param>
		public DataverseOperationException(
			string message)
			: base(message)
		{
		}


		// ============================================================================
		/// <summary>
		/// Creates the exception from the server's fault message and the host
		/// exception that carried it.
		/// </summary>
		/// <param name="message">The server's fault message, unchanged.</param>
		/// <param name="innerException">The host's original exception (a FaultException or a rejected bridge call).</param>
		public DataverseOperationException(
			string message,
			Exception innerException)
			: base(message, innerException)
		{
		}

	}
}
