using Com.AiricLenz.XTB.Plugin.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin.Tests
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	/// <summary>
	/// Proves the test project builds against the plugin and can see its
	/// internal types.
	/// </summary>
	[TestClass]
	public class SmokeTests
	{

		private const string PluginAssemblyName =
			"Com.AiricLenz.XTB.Plugin.BulkSolutionExporter";


		// ============================================================================
		[TestMethod]
		public void PluginAssembly_Load_HasPluginAssemblyName()
		{
			// Arrange
			var pluginType = typeof(BulkSolutionExporter_Plugin);

			// Act
			var assemblyName = pluginType.Assembly.GetName().Name;

			// Assert
			Assert.AreEqual(PluginAssemblyName, assemblyName);
		}


		// ============================================================================
		[TestMethod]
		public void InternalType_Reference_ResolvesToPluginAssembly()
		{
			// Arrange
			var internalType = typeof(ColorHelper);

			// Act
			var assemblyName = internalType.Assembly.GetName().Name;

			// Assert
			Assert.AreEqual(PluginAssemblyName, assemblyName);
		}

	}
}
