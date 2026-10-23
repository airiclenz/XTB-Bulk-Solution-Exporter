using System;
using System.Collections.Generic;
using System.Text;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.BulkSolutionExporter.Core.Schema;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Tests.Fakes
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// An in-memory Dataverse environment for <see cref="RunEngine"/> tests.
	/// Records every call in <see cref="Calls"/> and throws a
	/// <see cref="DataverseOperationException"/> from a call the test armed.
	/// </summary>
	public sealed class FakeDataverseGateway : IDataverseGateway
	{

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The prefix of every exported file's content, followed by the unique name.
		/// </summary>
		public const string ExportContentPrefix = "export:";

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The message of every armed failure.
		/// </summary>
		public const string FailureMessage = "Fake gateway failure";


		// ============================================================================
		public FakeDataverseGateway(
			string connectionName = "Source")
		{
			ConnectionName = connectionName;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string ConnectionName
		{
			get;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Every call in order, as "Method:argument" (e.g. "ExportSolution:A:managed").
		/// </summary>
		public List<string> Calls
		{
			get;
		} = new List<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The solutions <see cref="RetrieveSolutions"/> and <see cref="SolutionExists"/> read.
		/// </summary>
		public List<SolutionInfo> Solutions
		{
			get;
		} = new List<SolutionInfo>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// True makes <see cref="PublishAll"/> throw.
		/// </summary>
		public bool ShouldFailPublishAll
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// <see cref="UpdateSolutionVersion"/> throws for this solution id.
		/// </summary>
		public Guid? FailVersionUpdateFor
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// <see cref="ExportSolution"/> throws for this unique name.
		/// </summary>
		public string FailExportFor
		{
			get; set;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The options of every <see cref="ImportSolution"/> call, in order.
		/// </summary>
		public List<ImportOptions> ImportedOptions
		{
			get;
		} = new List<ImportOptions>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Faults thrown by the next <see cref="ImportSolution"/> calls, one per
		/// call, as a <see cref="DataverseOperationException"/> with that message;
		/// an empty queue lets the import succeed.
		/// </summary>
		public Queue<string> ImportFaultMessages
		{
			get;
		} = new Queue<string>();


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// Runs after each export, with the unique name and the managed flag;
		/// a test requests its abort here.
		/// </summary>
		public Action<string, bool> AfterExport
		{
			get; set;
		}


		// ============================================================================
		public void PublishAll()
		{
			Calls.Add("PublishAll");

			if (ShouldFailPublishAll)
			{
				throw new DataverseOperationException(FailureMessage);
			}
		}


		// ============================================================================
		public void UpdateSolutionVersion(
			Guid solutionId,
			string version)
		{
			Calls.Add("UpdateSolutionVersion:" + solutionId + ":" + version);

			if (FailVersionUpdateFor == solutionId)
			{
				throw new DataverseOperationException(FailureMessage);
			}
		}


		// ============================================================================
		public byte[] ExportSolution(
			string uniqueName,
			bool isManaged)
		{
			Calls.Add("ExportSolution:" + uniqueName + ":" + (isManaged ? "managed" : "unmanaged"));

			if (FailExportFor == uniqueName)
			{
				throw new DataverseOperationException(FailureMessage);
			}

			AfterExport?.Invoke(uniqueName, isManaged);

			return Encoding.UTF8.GetBytes(ExportContentPrefix + uniqueName);
		}


		// ============================================================================
		public bool SolutionExists(
			string uniqueName)
		{
			Calls.Add("SolutionExists:" + uniqueName);

			return Solutions.Exists(solution => solution.UniqueName == uniqueName);
		}


		// ============================================================================
		public void ImportSolution(
			byte[] solutionFile,
			ImportOptions options)
		{
			Calls.Add("ImportSolution:" + Encoding.UTF8.GetString(solutionFile));
			ImportedOptions.Add(options);

			if (ImportFaultMessages.Count > 0)
			{
				throw new DataverseOperationException(ImportFaultMessages.Dequeue());
			}
		}


		// ============================================================================
		public void DeleteAndPromote(
			string uniqueName)
		{
			Calls.Add("DeleteAndPromote:" + uniqueName);
		}


		// ============================================================================
		public IReadOnlyList<SolutionInfo> RetrieveSolutions(
			bool includeManaged)
		{
			Calls.Add("RetrieveSolutions:" + includeManaged);

			return
				Solutions.FindAll(
					solution => includeManaged || !solution.IsManaged);
		}

	}
}
