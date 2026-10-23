/**
 * Checks that the worker version the tool asks for is the version the worker
 * package is built with: `workers.engine.packageVersion` in `pptb.config.json`
 * must equal `<Version>` in the worker .csproj. PPTB installs exactly that
 * NuGet version, so a mismatch ships a tool that cannot start its worker.
 *
 * Usage: node scripts/check-worker-version.mjs [pptb.config.json] [worker .csproj]
 * Both paths default to this repository's files. Exits 0 when the versions
 * match, 1 with a message on stderr when they differ or cannot be read.
 */

import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { argv, exit, stderr, stdout } from "node:process";
import { fileURLToPath } from "node:url";

const SCRIPT_FOLDER = dirname(fileURLToPath(import.meta.url));
const DEFAULT_CONFIG_PATH = resolve(SCRIPT_FOLDER, "..", "pptb.config.json");
const DEFAULT_PROJECT_PATH = resolve(
	SCRIPT_FOLDER,
	"..",
	"..",
	"Bulk Solution Exporter.PptbWorker",
	"Bulk Solution Exporter.PptbWorker.csproj",
);
const WORKER_ID = "engine";
const FIRST_ARGUMENT_INDEX = 2;
const EXIT_CODE_FAILURE = 1;
const PROJECT_VERSION_PATTERN = /<Version>\s*([^<\s]+)\s*<\/Version>/g;

// ============================================================================
/** Reads `workers.engine.packageVersion` from a pptb.config.json file. */
function readConfigVersion(configPath) {
	const config = JSON.parse(readFileSync(configPath, "utf8"));
	const version = config?.workers?.[WORKER_ID]?.packageVersion;

	if (typeof version !== "string" || version.length === 0) {
		throw new Error(`${configPath} has no workers.${WORKER_ID}.packageVersion.`);
	}

	return version;
}

// ============================================================================
/** Reads the single `<Version>` element of a .csproj file. */
function readProjectVersion(projectPath) {
	const project = readFileSync(projectPath, "utf8");
	const versions = [...project.matchAll(PROJECT_VERSION_PATTERN)].map((match) => match[1]);

	if (versions.length !== 1) {
		throw new Error(`${projectPath} must hold exactly one <Version> element; found ${versions.length}.`);
	}

	return versions[0];
}

// ============================================================================
/** Compares the two versions and returns the process exit code. */
function checkWorkerVersion(configPath, projectPath) {
	const configVersion = readConfigVersion(configPath);
	const projectVersion = readProjectVersion(projectPath);

	if (configVersion !== projectVersion) {
		stderr.write(
			`Worker version mismatch: ${configPath} asks for ${configVersion}, ` +
				`${projectPath} builds ${projectVersion}.\n`,
		);
		return EXIT_CODE_FAILURE;
	}

	stdout.write(`Worker version ${projectVersion} matches.\n`);
	return 0;
}

const [configPath = DEFAULT_CONFIG_PATH, projectPath = DEFAULT_PROJECT_PATH] = argv.slice(FIRST_ARGUMENT_INDEX);

try {
	exit(checkWorkerVersion(resolve(configPath), resolve(projectPath)));
} catch (error) {
	stderr.write(`Worker version check failed: ${error instanceof Error ? error.message : String(error)}\n`);
	exit(EXIT_CODE_FAILURE);
}
