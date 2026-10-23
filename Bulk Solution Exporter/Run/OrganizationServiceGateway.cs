using System;
using System.Collections.Generic;
using System.ServiceModel;
using Com.AiricLenz.BulkSolutionExporter.Core.Run;
using Com.AiricLenz.XTB.Plugin.Schema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionInfo = Com.AiricLenz.BulkSolutionExporter.Core.Schema.SolutionInfo;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Run
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The XrmToolBox adapter of <see cref="IDataverseGateway"/>: runs each
	/// call as an organization service request on the calling thread. A
	/// <see cref="FaultException"/> of the service is rethrown as
	/// <see cref="DataverseOperationException"/> with the server's message
	/// unchanged; any other exception passes through as it is.
	/// </summary>
	internal sealed class OrganizationServiceGateway : IDataverseGateway
	{

		private const string SolutionEntityName = "solution";
		private const string SolutionIdAttributeName = "solutionid";
		private const string UniqueNameAttributeName = "uniquename";
		private const string FriendlyNameAttributeName = "friendlyname";
		private const string VersionAttributeName = "version";
		private const string IsManagedAttributeName = "ismanaged";
		private const string IsVisibleAttributeName = "isvisible";
		private const string DescriptionAttributeName = "description";
		private const string MissingExportFileMessage = "The export returned no solution file.";

		private readonly IOrganizationService _service;


		// ============================================================================
		/// <summary>
		/// Creates the gateway of one connection.
		/// </summary>
		/// <param name="service">The connection's organization service.</param>
		/// <param name="connectionName">The connection's display name.</param>
		/// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
		public OrganizationServiceGateway(
			IOrganizationService service,
			string connectionName)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			ConnectionName = connectionName ?? string.Empty;
		}


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		/// <summary>
		/// The connection's display name.
		/// </summary>
		public string ConnectionName
		{
			get;
		}


		// ============================================================================
		/// <summary>
		/// Publishes all customizations (<see cref="PublishAllXmlRequest"/>).
		/// </summary>
		public void PublishAll()
		{
			Execute(new PublishAllXmlRequest());
		}


		// ============================================================================
		/// <summary>
		/// Writes the version attribute of the solution record.
		/// </summary>
		public void UpdateSolutionVersion(
			Guid solutionId,
			string version)
		{
			var solutionToBeUpdated =
				new Entity(
					SolutionEntityName,
					solutionId);

			solutionToBeUpdated.Attributes.Add(
				VersionAttributeName,
				version);

			Translate(() => _service.Update(solutionToBeUpdated));
		}


		// ============================================================================
		/// <summary>
		/// Exports the solution (<see cref="ExportSolutionRequest"/>).
		/// </summary>
		/// <exception cref="InvalidOperationException">The response carries no file.</exception>
		public byte[] ExportSolution(
			string uniqueName,
			bool isManaged)
		{
			var exportRequest = new ExportSolutionRequest
			{
				SolutionName = uniqueName,
				Managed = isManaged
			};

			var exportResponse =
				(ExportSolutionResponse) Execute(exportRequest);

			return
				exportResponse.ExportSolutionFile ??
				throw new InvalidOperationException(MissingExportFileMessage);
		}


		// ============================================================================
		/// <summary>
		/// Queries the solution table for a record with this unique name.
		/// </summary>
		public bool SolutionExists(
			string uniqueName)
		{
			var query = new QueryExpression(SolutionEntityName)
			{
				ColumnSet = new ColumnSet(UniqueNameAttributeName, VersionAttributeName),
				Criteria = new FilterExpression(LogicalOperator.And)
			};

			query.Criteria.AddCondition(
				UniqueNameAttributeName,
				ConditionOperator.Equal,
				uniqueName);

			EntityCollection result = null;
			Translate(() => result = _service.RetrieveMultiple(query));

			return result.Entities.Count > 0;
		}


		// ============================================================================
		/// <summary>
		/// Imports the solution file (<see cref="ImportSolutionRequest"/>).
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
		public void ImportSolution(
			byte[] solutionFile,
			ImportOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var importRequest = new ImportSolutionRequest()
			{
				CustomizationFile = solutionFile,
				PublishWorkflows = options.ShouldPublishWorkflows,
				OverwriteUnmanagedCustomizations = options.ShouldOverwriteUnmanagedCustomizations,
				ConvertToManaged = options.IsManaged,
				HoldingSolution = options.IsHolding,
			};

			Execute(importRequest);
		}


		// ============================================================================
		/// <summary>
		/// Applies the pending upgrade (<see cref="DeleteAndPromoteRequest"/>).
		/// </summary>
		public void DeleteAndPromote(
			string uniqueName)
		{
			var applyUpgradeRequest = new DeleteAndPromoteRequest
			{
				UniqueName = uniqueName,
			};

			Execute(applyUpgradeRequest);
		}


		// ============================================================================
		/// <summary>
		/// Reads the visible solutions of the environment.
		/// </summary>
		public IReadOnlyList<SolutionInfo> RetrieveSolutions(
			bool includeManaged)
		{
			var query = new QueryExpression(SolutionEntityName);

			if (!includeManaged)
			{
				query.Criteria.AddCondition(
					IsManagedAttributeName,
					ConditionOperator.Equal,
					false);
			}

			query.Criteria.AddCondition(
				IsVisibleAttributeName,
				ConditionOperator.Equal,
				true);

			query.ColumnSet.AddColumns(
				FriendlyNameAttributeName,
				IsManagedAttributeName,
				SolutionIdAttributeName,
				UniqueNameAttributeName,
				DescriptionAttributeName,
				VersionAttributeName);

			EntityCollection records = null;
			Translate(() => records = _service.RetrieveMultiple(query));

			var solutions = new List<SolutionInfo>();

			foreach (var record in records.Entities)
			{
				var solution =
					Solution.ConvertFrom(
						record,
						Guid.Empty);

				if (solution != null)
				{
					solutions.Add(solution.Info);
				}
			}

			return solutions;
		}


		// ============================================================================
		private OrganizationResponse Execute(
			OrganizationRequest request)
		{
			OrganizationResponse response = null;
			Translate(() => response = _service.Execute(request));

			return response;
		}


		// ============================================================================
		/// <summary>
		/// Runs the service call and turns a service fault into
		/// <see cref="DataverseOperationException"/>, keeping the server's message.
		/// </summary>
		private static void Translate(
			Action serviceCall)
		{
			try
			{
				serviceCall();
			}
			catch (FaultException ex)
			{
				throw new DataverseOperationException(ex.Message, ex);
			}
		}

	}
}
