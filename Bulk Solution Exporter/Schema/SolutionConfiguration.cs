using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Schema
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
