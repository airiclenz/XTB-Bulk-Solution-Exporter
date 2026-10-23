using System;
using System.Threading.Tasks;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.BulkSolutionExporter.PptbWorker
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// The worker's entry point. Power Platform ToolBox starts the worker and
	/// speaks JSON-RPC (<c>jsonrpc-stdio-v1</c>) with it on stdin and stdout;
	/// the worker runs until the host closes the connection.
	/// </summary>
	public static class Program
	{

		private const string ReadyMessage = "Bulk Solution Exporter worker ready; stdout is reserved for JSON-RPC.";


		// ============================================================================
		/// <summary>
		/// Serves JSON-RPC on stdin/stdout until the host disconnects.
		/// Diagnostics go to stderr only.
		/// </summary>
		public static async Task Main()
		{
			var standardOutput = Console.OpenStandardOutput();
			var standardInput = Console.OpenStandardInput();

			// A stray Console.WriteLine would corrupt the protocol stream, so the
			// console writer is pointed at stderr once the raw stdout is taken.
			Console.SetOut(Console.Error);

			using (var rpc = WorkerMethods.Attach(standardOutput, standardInput))
			{
				Console.Error.WriteLine(ReadyMessage);
				await rpc.Completion;
			}
		}

	}
}
