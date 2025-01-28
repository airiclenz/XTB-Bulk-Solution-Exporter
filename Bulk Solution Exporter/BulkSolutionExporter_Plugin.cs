using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	// ============================================================================
	// Do not forget to update version number and author (company attribute) in AssemblyInfo.cs class
	// To generate Base64 string for Images below, you can use https://www.base64-image.de/
	[Export(typeof(IXrmToolBoxPlugin)),
		ExportMetadata("Name", "Bulk Solution Exporter"),
		ExportMetadata("Description", "This tool helps to bulk export solution files as managed and/or unmanaged and save them as specific files including version number updates."),
		// Please specify the base64 content of a 32x32 pixels image
		ExportMetadata(
			"SmallImageBase64",
			"iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAFw2lUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4KPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iWE1QIENvcmUgNS41LjAiPgogPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4KICA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIgogICAgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIgogICAgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIgogICAgeG1sbnM6cGhvdG9zaG9wPSJodHRwOi8vbnMuYWRvYmUuY29tL3Bob3Rvc2hvcC8xLjAvIgogICAgeG1sbnM6ZXhpZj0iaHR0cDovL25zLmFkb2JlLmNvbS9leGlmLzEuMC8iCiAgICB4bWxuczp0aWZmPSJodHRwOi8vbnMuYWRvYmUuY29tL3RpZmYvMS4wLyIKICAgIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIgogICAgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIKICAgeG1wOkNyZWF0ZURhdGU9IjIwMjQtMTAtMDdUMTI6MDM6MDkrMDIwMCIKICAgeG1wOk1vZGlmeURhdGU9IjIwMjQtMTAtMDdUMTI6MTA6NTErMDI6MDAiCiAgIHhtcDpNZXRhZGF0YURhdGU9IjIwMjQtMTAtMDdUMTI6MTA6NTErMDI6MDAiCiAgIHBob3Rvc2hvcDpEYXRlQ3JlYXRlZD0iMjAyNC0xMC0wN1QxMjowMzowOSswMjAwIgogICBwaG90b3Nob3A6Q29sb3JNb2RlPSIzIgogICBwaG90b3Nob3A6SUNDUHJvZmlsZT0ic1JHQiBJRUM2MTk2Ni0yLjEi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"),
		// Please specify the base64 content of a 80x80 pixels image
		ExportMetadata(
			"BigImageBase64",
			"iVBORw0KGgoAAAANSUhEUgAAAFAAAABQCAYAAACOEfKtAAAFw2lUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4KPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iWE1QIENvcmUgNS41LjAiPgogPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4KICA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIgogICAgeG1sbnM6ZGM9Imh0dHA6Ly9wdXJsLm9yZy9kYy9lbGVtZW50cy8xLjEvIgogICAgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIgogICAgeG1sbnM6cGhvdG9zaG9wPSJodHRwOi8vbnMuYWRvYmUuY29tL3Bob3Rvc2hvcC8xLjAvIgogICAgeG1sbnM6ZXhpZj0iaHR0cDovL25zLmFkb2JlLmNvbS9leGlmLzEuMC8iCiAgICB4bWxuczp0aWZmPSJodHRwOi8vbnMuYWRvYmUuY29tL3RpZmYvMS4wLyIKICAgIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIgogICAgeG1sbnM6c3RFdnQ9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZUV2ZW50IyIKICAgeG1wOkNyZWF0ZURhdGU9IjIwMjQtMTAtMDdUMTI6MDM6MDkrMDIwMCIKICAgeG1wOk1vZGlmeURhdGU9IjIwMjQtMTAtMDdUMTI6MDQ6NDErMDI6MDAiCiAgIHhtcDpNZXRhZGF0YURhdGU9IjIwMjQtMTAtMDdUMTI6MDQ6NDErMDI6MDAiCiAgIHBob3Rvc2hvcDpEYXRlQ3JlYXRlZD0iMjAyNC0xMC0wN1QxMjowMzowOSswMjAwIgogICBwaG90b3Nob3A6Q29sb3JNb2RlPSIzIgogICBwaG90b3Nob3A6SUNDUHJvZmlsZT0ic1JHQiBJRUM2MTk2Ni0yLjEi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"),
		ExportMetadata("BackgroundColor", "Lavender"),
		ExportMetadata("PrimaryFontColor", "Black"),
		ExportMetadata("SecondaryFontColor", "Gray")]
	public class BulkSolutionExporter_Plugin : PluginBase
	{

		public const string JsonSettingsFileName = "exported_versions.json";


		// ============================================================================
		public override IXrmToolBoxPluginControl GetControl()
		{
			return new BulkSolutionExporter_PluginControl();
		}


		// ============================================================================
		/// <summary>
		/// Constructor 
		/// </summary>
		public BulkSolutionExporter_Plugin()
		{
			// If you have external assemblies that you need to load, uncomment the following to 
			// hook into the event that will fire when an Assembly fails to resolve
			// AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);
		}


		// ============================================================================
		/// <summary>
		/// Event fired by CLR when an assembly reference fails to load
		/// Assumes that related assemblies will be loaded from a subfolder named the same as the Plugin
		/// For example, a folder named Sample.XrmToolBox.MyPlugin 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
		{
			Assembly loadAssembly = null;
			Assembly currAssembly = Assembly.GetExecutingAssembly();

			// base name of the assembly that failed to resolve
			var argName = args.Name.Substring(0, args.Name.IndexOf(","));

			// check to see if the failing assembly is one that we reference.
			List<AssemblyName> refAssemblies = currAssembly.GetReferencedAssemblies().ToList();
			var refAssembly = refAssemblies.Where(a => a.Name == argName).FirstOrDefault();

			// if the current unresolved assembly is referenced by our plugin, attempt to load
			if (refAssembly != null)
			{
				// load from the path to this plugin assembly, not host executable
				string dir = Path.GetDirectoryName(currAssembly.Location).ToLower();
				string folder = Path.GetFileNameWithoutExtension(currAssembly.Location);
				dir = Path.Combine(dir, folder);

				var assmbPath = Path.Combine(dir, $"{argName}.dll");

				if (File.Exists(assmbPath))
				{
					loadAssembly = Assembly.LoadFrom(assmbPath);
				}
				else
				{
					throw new FileNotFoundException($"Unable to locate dependency: {assmbPath}");
				}
			}

			return loadAssembly;
		}
	}
}