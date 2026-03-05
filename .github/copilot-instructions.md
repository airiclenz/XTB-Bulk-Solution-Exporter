# Plugin Development Instructions

This document outlines the standards, structure, and best practices for developing Dataverse server-side plugins in this codebase. Follow these instructions strictly when creating or modifying plugin projects.

## 1. Project Structure & Setup

### Folder & Project Naming
- **Scenario**: One plugin project per Entity/Table.
- **Folder Name**: `Plugin - {Entity Name}` (e.g., `Plugin - Shared Product Detail`).
- **Project Name**: Matches the folder name (e.g., `Plugin - Shared Product Detail.csproj`).
- **Assembly Name**: `BO.D365.Dataverse.Plugin.{EntityName}` (e.g., `BO.D365.Dataverse.Plugin.SharedProductDetail`).
- **Namespace**: `BO.D365.Dataverse.Plugin`.
- **VS Project Template**: "Class Library (.NET Framework)" with .NET version 4.7.1.

### Dependencies
Every plugin project must reference:
1.  **NuGet Packages**:
    -   `Microsoft.CrmSdk.CoreAssemblies`
    -   `spkl` (for deployment attributes)
    -   `Microsoft.Bcl.AsyncInterfaces`

2.  **Shared Projects (Local)**:
    -   `..\..\Dataverse Toolbox\GeneralToolbox\General Toolbox.projitems`
    -   `..\..\Dataverse Toolbox\PluginToolbox\Plugin Toolbox.projitems`
    -   `..\..\Shared Code\Shared Code.projitems`
3.  **Signing**:
    -   Sign the assembly using `key.snk` (copy from an existing project).

### Configuration Files
-   **spkl.json**: Place in the project root.
    ```json
    {
      "plugins": [
        {
          "profile": "default,debug",
          "assemblypath": "bin\\Debug",
          "solution": "BOPlugins"
        }
      ]
    }
    ```

## 2. Plugin Class Structure

Refactor the main plugin class into four distinct regions. The class must inherit from `PluginBase`.
The main plugin class file should be named `Plugin{EntityName}.cs` (e.g., `PluginSharedProductDetail.cs`).

```csharp
using BO.D365.Implementation.Shared.Schema;
using Com.Columbus.D365.Dataverse.PluginToolbox;
using Com.Columbus.D365.Dataverse.Toolbox;
using Microsoft.Xrm.Sdk;
using System;

namespace BO.D365.Dataverse.Plugin
{
    #region SPKL
    // Define all steps here using CrmPluginRegistration attributes (adjust accordingly, e.g. add Pre- or Post image)
    [CrmPluginRegistration(
        MessageNameEnum.Create,
        {EntityName}.LogicalName,
        StageEnum.PostOperation,
        ExecutionModeEnum.Synchronous,
        "",
        "BO.D365.Dataverse.Plugin.{EntityName} - Create PostSync",
        1,
        IsolationModeEnum.Sandbox
    )]
    #endregion

    // ============================================================================
    // ============================================================================
    public class Plugin{EntityName} : PluginBase
    {
        #region Base
        // Override Entry Points (OnCreatePostOperation, OnUpdatePreOperation, etc.)
        #endregion

        #region Condition Properties
        // properties to validate execution context (Target vs PreImage data)
        #endregion

        #region Custom Logic
        // Private methods implementing the actual business logic
        #endregion
    }
}
```

### Region Details

#### 1. SPKL (Registration)
-   Define triggers using `[CrmPluginRegistration]`.

# 1. Coding Standards

### Formatting & Style
-   **Headers**: Precede every function with an 80-character divider:
    `// ============================================================================`
-   **Regions**: Strictly use the generic regions (`Base`, `Condition Properties`, `Custom Logic`).
-   **Braces**: Always use `{ }` for control blocks (`if`, `foreach`), even one-liners.
-   **Chop Down**: For complex method calls (like constructors or `CrmPluginRegistration`), put each argument on a new line.

### Naming
-   **Variables**: Descriptive and full words (e.g., `retrievedAccount` not `acc`).
-   **Methods**: PascalCase, verb-noun phrases (e.g., `CalculateTotalAmount`).

### Quality
-   **Single Responsibility**: Keep methods small. One method = one logical action.
-   **Early Exit**: Use guard clauses to flatten `if` nesting.
-   **Logging**: Use the inherited `Log()` method for tracing.
