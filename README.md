# Bulk Solution Exporter
![XrmToolBox](https://img.shields.io/badge/XrmToolBox-Plugin-blue?logo=powerapps)
![Dataverse](https://img.shields.io/badge/Target-Dataverse%20%7C%20Dynamics%20365-764ABC)
![License](https://img.shields.io/github/license/airiclenz/XTB-Bulk-Solution-Exporter)


<img src="https://raw.githubusercontent.com/airiclenz/XTB-Bulk-Solution-Exporter/refs/heads/master/Images/bulk%20solution%20exporter.svg" alt="Logo" width="110"/>

Export smarter, **not harder**. <br><br>
Bulk Solution Exporter automates the export *and* import of multiple Power Platform / Dataverse / Dynamics 365 solutions in one click - complete with version bumping, Git commits, and clean output folders. Perfect for CI/CD pipelines, configuration backups, or any team that juggles more than a handful of solutions.

---

## ✨ Features

| Capability | Description |
|------------|-------------|
| **Batch export / import** | Select dozens of solutions and run a single operation—managed or unmanaged. |
| **Automated versioning** | Apply custom version patterns and auto-increment rules to stay release-ready. |
| **Git integration** | Commit exported ZIPs directly to your repo for easy traceability. |
| **Custom output paths** | Define destination folders per solution, project, or environment. |
| **Reusable history** | Rerun previous exports with identical settings for consistent builds. |
| **Check solution status in target** | Comnpares solution version numberas in source and target and displays status icons. |

---

## 🚀 Quick Start

1. **Install** via XrmToolBox > *Plugins Store* > **Bulk Solution Exporter**.
2. Connect to your Dataverse environment.
3. Pick the solutions, choose *Managed* or *Unmanaged*, tweak versioning if needed.
4. Hit **Execute** and enjoy the coffee break you just earned.

> **Tip:** Turn on **Create Git Commit** to commit the exported files automatically.
> The plugin creates one commit per run, using the commit message you enter.
---

## 🧰 Power Platform ToolBox

Bulk Solution Exporter also runs in [Power Platform ToolBox](https://www.powerplatformtoolbox.com) (PPTB), with the same features as the XrmToolBox plugin.

1. **Install** the [.NET 8 runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer). The tool's engine is a .NET worker that PPTB downloads from nuget.org on first use and runs with your local `dotnet`; it does not start without it.
2. **Install** the tool in PPTB from the tool marketplace: **Bulk Solution Exporter** (npm package `@airiclenz/pptb-bulk-solution-exporter`).
3. **Connect** when you open the tool. PPTB lets you assign up to ten connections:
   - connection **0** is the **source** environment: its unmanaged solutions are listed, versioned and exported;
   - connections **1..n** are the **targets**: the exported solutions are imported into them, in that order. With no target the tool only exports.
4. Pick the solutions, set the options and hit **Execute**, as in XrmToolBox.

Settings and per-solution file locations are stored by PPTB per tool; they are not shared with the XrmToolBox plugin.

### Building the packages

The tool is two packages: the .NET worker (NuGet) and the UI (npm). Each has one command, run with the .NET 8 SDK and Node.js 20+:

```sh
sh pptb/scripts/pack-worker.sh     # -> artifacts/worker/AiricLenz.BulkSolutionExporter.PptbWorker.<version>.nupkg
cd pptb && npm ci && npm pack      # -> pptb/airiclenz-pptb-bulk-solution-exporter-<version>.tgz
```

Both check that `workers.engine.packageVersion` in `pptb/pptb.config.json` equals `<Version>` in the worker `.csproj` (`node pptb/scripts/check-worker-version.mjs`); `npm pack` also builds `dist/` first.

---

## 🤝 Contributing

PRs and feature requests are welcome!
1. Fork the repo
2. Create your branch (`git checkout -b feature/amazing-thing`)
3. Commit and push (`git push origin feature/amazing-thing`)
4. Open a Pull Request

---

## 📄 License

Distributed under the GNU GENERAL PUBLIC LICENSE Version 3. See `LICENSE` for details.

---

> **Maintainer:** [Airic Lenz](https://github.com/airiclenz) – *Solution Architect & hobby synth nerd*  🎛️ <br>
> *Because exporting and importing 42 solutions one-by-one should never be a thing.*

