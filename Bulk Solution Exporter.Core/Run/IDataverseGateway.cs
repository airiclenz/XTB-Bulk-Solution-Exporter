using System;
using System.Collections.Generic;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The one way <see cref="RunEngine"/> reaches a Dataverse environment.
	/// Each host adapts its own connection to it (XrmToolBox: the organization
	/// service; Power Platform ToolBox: calls back into the UI's Dataverse API).
	/// Every call blocks until the request finished. A failed request throws:
	/// the host's fault turned into <see cref="DataverseOperationException"/>
	/// with the server's message unchanged, so retries can classify it. The
	/// engine calls a gateway from one thread at a time.
	/// </summary>
	public interface IDataverseGateway
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The environment's display name, shown in log and progress lines.
		/// </summary>
		string ConnectionName
		{
			get;
		}


		// ============================================================================
		/// <summary>
		/// Publishes all customizations of the environment.
		/// </summary>
		void PublishAll();


		// ============================================================================
		/// <summary>
		/// Writes a new version number to a solution record.
		/// </summary>
		/// <param name="solutionId">The solution record id.</param>
		/// <param name="version">The new version, in its dotted form.</param>
		void UpdateSolutionVersion(
			Guid solutionId,
			string version);


		// ============================================================================
		/// <summary>
		/// Exports a solution.
		/// </summary>
		/// <param name="uniqueName">The solution's unique name.</param>
		/// <param name="isManaged">True for a managed export, false for an unmanaged one.</param>
		/// <returns>The content of the solution zip file; never <c>null</c>.</returns>
		byte[] ExportSolution(
			string uniqueName,
			bool isManaged);


		// ============================================================================
		/// <summary>
		/// Returns whether the environment has a solution with this unique name.
		/// </summary>
		bool SolutionExists(
			string uniqueName);


		// ============================================================================
		/// <summary>
		/// Imports a solution file.
		/// </summary>
		/// <param name="solutionFile">The content of the solution zip file.</param>
		/// <param name="options">How the file is imported.</param>
		void ImportSolution(
			byte[] solutionFile,
			ImportOptions options);


		// ============================================================================
		/// <summary>
		/// Starts a solution import as one server-side async operation and
		/// returns as soon as the server accepted it: an upgrade
		/// (<see cref="ImportOptions.IsUpgrade"/>) as <c>StageAndUpgradeAsync</c>,
		/// any other import as <c>ImportSolutionAsync</c>. The caller follows the
		/// operation with <see cref="GetImportOperationState"/> until it ends.
		/// </summary>
		/// <param name="solutionFile">The content of the solution zip file.</param>
		/// <param name="options">How the file is imported.</param>
		/// <returns>The id of the server's async operation.</returns>
		Guid StartImport(
			byte[] solutionFile,
			ImportOptions options);


		// ============================================================================
		/// <summary>
		/// Reads where an import operation that <see cref="StartImport"/> began
		/// stands. A failed or canceled import is answered as that state, not
		/// thrown; only a failed read throws.
		/// </summary>
		/// <param name="asyncOperationId">The id <see cref="StartImport"/> returned.</param>
		ImportOperationState GetImportOperationState(
			Guid asyncOperationId);


		// ============================================================================
		/// <summary>
		/// Applies a pending upgrade: deletes the original solution and promotes
		/// its holding solution (the solution's "_Upgrade" twin) in its place.
		/// </summary>
		void DeleteAndPromote(
			string uniqueName);


		// ============================================================================
		/// <summary>
		/// Reads the solutions of the environment.
		/// </summary>
		/// <param name="includeManaged">False returns the unmanaged solutions only.</param>
		IReadOnlyList<SolutionInfo> RetrieveSolutions(
			bool includeManaged);

	}



	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// How <see cref="IDataverseGateway.ImportSolution"/> and
	/// <see cref="IDataverseGateway.StartImport"/> import a solution file.
	/// </summary>
	public sealed class ImportOptions
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True when the file is a managed solution (converts unmanaged
		/// components of the same name to managed).
		/// </summary>
		public bool IsManaged
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True to import the file as a holding solution, the first half of an
		/// upgrade that <see cref="IDataverseGateway.DeleteAndPromote"/> completes.
		/// </summary>
		public bool IsHolding
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// For <see cref="IDataverseGateway.StartImport"/>: true to upgrade the
		/// installed solution in one operation (<c>StageAndUpgradeAsync</c>),
		/// false for a plain import (<c>ImportSolutionAsync</c>).
		/// </summary>
		public bool IsUpgrade
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True to activate the solution's processes and flows after the import.
		/// </summary>
		public bool ShouldPublishWorkflows
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True to overwrite unmanaged customizations of the solution's components.
		/// </summary>
		public bool ShouldOverwriteUnmanagedCustomizations
		{
			get; set;
		}

	}
}
