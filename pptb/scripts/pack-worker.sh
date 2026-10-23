#!/bin/sh
# Packs the Power Platform ToolBox worker as a .NET tool NuGet package into
# artifacts/worker/ at the repository root, after checking that its version
# equals workers.engine.packageVersion in pptb/pptb.config.json.
# Needs the .NET 8 SDK and Node.js on PATH. Publish this package to nuget.org
# before publishing the npm package that asks for it.

set -eu

SCRIPT_FOLDER=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH= cd -- "$SCRIPT_FOLDER/../.." && pwd)
WORKER_PROJECT="$REPOSITORY_ROOT/Bulk Solution Exporter.PptbWorker/Bulk Solution Exporter.PptbWorker.csproj"
OUTPUT_FOLDER="$REPOSITORY_ROOT/artifacts/worker"

node "$SCRIPT_FOLDER/check-worker-version.mjs" \
	"$REPOSITORY_ROOT/pptb/pptb.config.json" \
	"$WORKER_PROJECT"

dotnet pack "$WORKER_PROJECT" \
	--configuration Release \
	--output "$OUTPUT_FOLDER"

echo "Worker package written to $OUTPUT_FOLDER"
