using System.Collections.Generic;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests.Fakes
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// An in-memory git client for <see cref="RunEngine"/> tests. Pass
	/// <see cref="Create"/> as the engine's client factory: it records the
	/// repository root the engine derived, and every git call is recorded in
	/// <see cref="Calls"/>. Every call succeeds unless
	/// <see cref="FailureMessage"/> is set.
	/// </summary>
	public sealed class FakeGitClient : IGitClient
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The repository root of the last <see cref="Create"/> call; null when
		/// the engine never created a client.
		/// </summary>
		public string RepositoryRootPath
		{
			get; private set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Every git call in order, as "Method:argument" (e.g. "AddFile:/tmp/a.zip").
		/// </summary>
		public List<string> Calls
		{
			get;
		} = new List<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// When set, every git call fails and reports this text as its error
		/// message; null (the default) lets every call succeed.
		/// </summary>
		public string FailureMessage
		{
			get; set;
		}


		// ============================================================================
		/// <summary>
		/// The engine's client factory: records the repository root and returns this client.
		/// </summary>
		public IGitClient Create(
			string repositoryRootPath)
		{
			RepositoryRootPath = repositoryRootPath;
			return this;
		}


		// ============================================================================
		public bool AddFile(
			string absolutePath,
			out string errorMessage)
		{
			Calls.Add("AddFile:" + absolutePath);
			return IsSuccessful(out errorMessage);
		}


		// ============================================================================
		public bool Commit(
			string message,
			out string errorMessage)
		{
			Calls.Add("Commit:" + message);
			return IsSuccessful(out errorMessage);
		}


		// ============================================================================
		public bool Push(
			out string errorMessage)
		{
			Calls.Add("Push");
			return IsSuccessful(out errorMessage);
		}


		// ============================================================================
		/// <summary>
		/// The outcome of a git call: false with <see cref="FailureMessage"/> as
		/// the error message when it is set, otherwise true with an empty one.
		/// </summary>
		private bool IsSuccessful(
			out string errorMessage)
		{
			errorMessage = FailureMessage ?? string.Empty;
			return FailureMessage == null;
		}

	}
}
