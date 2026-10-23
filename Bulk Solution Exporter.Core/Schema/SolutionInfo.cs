using System;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The host-neutral data of one Dataverse solution, as read from the
	/// solution table. Holds no UI state.
	/// </summary>
	public class SolutionInfo
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution record id.
		/// </summary>
		public Guid SolutionId
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution's unique (logical) name.
		/// </summary>
		public string UniqueName
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution's display name.
		/// </summary>
		public string FriendlyName
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution version, or <c>null</c> when the record carries none.
		/// </summary>
		public Version Version
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Whether the solution is installed as a managed solution.
		/// </summary>
		public bool IsManaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solution description, or <c>null</c> when the record carries none.
		/// </summary>
		public string Description
		{
			get; set;
		}

	}
}
