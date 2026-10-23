using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.Core.Schema
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public class SolutionConfiguration
	{


		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string SolutionIndentifier
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string FileNameManaged
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public string FileNameUnmanaged
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public bool Checked
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int SortingIndex
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int LastExportDurationUnamangedInSeconds
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int LastExportDurationManagedInSeconds
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int LastImportDurationManagedInSeconds
		{
			get; set;
		}

		// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
		public int LastImportDurationUnmanagedInSeconds
		{
			get; set;
		}


		// ============================================================================
		/// <summary>
		/// Creates an empty configuration, for deserializers that need a
		/// parameterless constructor.
		/// </summary>
		public SolutionConfiguration()
		{
		}


		// ============================================================================
		public SolutionConfiguration(
			string solutionIdentifier)
		{
			SolutionIndentifier = solutionIdentifier;
		}


		// ============================================================================
		public static SolutionConfiguration GetConfigFromJson(
			string jsonString)
		{
			return
				JsonConvert.DeserializeObject<SolutionConfiguration>(
					jsonString);
		}


		// ============================================================================
		public string GetJson()
		{
			return JsonConvert.SerializeObject(this);
		}




	}
}
