/**
 * Typed mirrors of the .NET worker's wire format (camelCase property names,
 * versions as dotted strings, ids as GUID strings) and the tool's own
 * persisted settings.
 */

import type { SolutionRecord } from "./dataverseBridge";

/** One solution of the source environment, as the worker reads its core `SolutionInfo`. */
export type SolutionInfo = SolutionRecord;

/**
 * The tool's per-solution configuration, the core `SolutionConfiguration`.
 * The property names keep the C# spelling, typos included, because the
 * worker reads and writes them by name.
 */
export interface SolutionConfiguration {
	/** Lower-cased environment URL + `.` + unique name; see `createSolutionIdentifier`. */
	solutionIndentifier: string;
	fileNameManaged: string | null;
	fileNameUnmanaged: string | null;
	checked: boolean;
	sortingIndex: number;
	lastExportDurationUnamangedInSeconds: number;
	lastExportDurationManagedInSeconds: number;
	lastImportDurationManagedInSeconds: number;
	lastImportDurationUnmanagedInSeconds: number;
}

/** One solution of a run: its source data and a copy of its configuration. */
export interface RunSolution {
	solution: SolutionInfo;
	/** `null` when the solution has no configuration (it then has no file names). */
	configuration: SolutionConfiguration | null;
}

/** Everything one run reads, the core `RunRequest`. */
export interface RunRequest {
	shouldPublishSource: boolean;
	shouldUpdateVersion: boolean;
	shouldExportManaged: boolean;
	shouldExportUnmanaged: boolean;
	shouldCommitToGit: boolean;
	shouldPushCommit: boolean;
	shouldImportManaged: boolean;
	shouldImportUnmanaged: boolean;
	shouldUpgrade: boolean;
	shouldOverwriteCustomizations: boolean;
	shouldEnableAutomation: boolean;
	shouldPublishTargets: boolean;
	shouldSaveVersionJson: boolean;
	shouldContinueOnError: boolean;
	versionFormat: string;
	commitMessage: string;
	retryCount: number;
	retryDelayInSeconds: number;
	/** The checked solutions, in processing order. */
	solutions: RunSolution[];
}

/** One export file or one import attempt of a run, the worker's `StepReport`. */
export interface StepReport {
	/** True for an import into a target, false for an export from the source. */
	isImport: boolean;
	solutionId: string;
	/** The display name of the environment the step ran against. */
	connectionName: string;
	isManaged: boolean;
	isSuccessful: boolean;
	durationInSeconds: number;
}

/** The answer of `run/start`, the worker's `RunReport` (a core `RunResult`). */
export interface RunReport {
	/** True when an abort was requested during the run. */
	isAborted: boolean;
	durationInSeconds: number;
	/** One entry per export file and per import attempt, in run order. */
	steps: StepReport[];
}

/** The parameters of the worker's `run/progress` notification. */
export interface RunProgress {
	message: string;
	detail: string | null;
	/** True when a new step starts, false for an update of the current one. */
	isNewStep: boolean;
}

/** The answer of `git/branches` and `git/switchBranch`. */
export interface GitBranches {
	/** The repository's root folder, or `null` when the file is in no repository. */
	repositoryRoot: string | null;
	/** The checked-out branch; empty when there is none. */
	activeBranch: string;
	/** The local branch names in git's order. */
	branches: string[];
}

/** The state of one export file of a solution. */
export interface ExportFileStatus {
	isConfigured: boolean;
	isPresent: boolean;
	/** The version the version file records, or `null` when it records none. */
	exportedVersion: string | null;
	isOutdated: boolean;
}

/** One answer entry of `files/status`. */
export interface SolutionFileStatus {
	uniqueName: string;
	managed: ExportFileStatus;
	unmanaged: ExportFileStatus;
	isOutdated: boolean;
}

/** One answer entry of `files/suggestFileNames`; both paths are `null` when refused. */
export interface SuggestedFileNames {
	uniqueName: string;
	fileNameManaged: string | null;
	fileNameUnmanaged: string | null;
}

/**
 * The tool's global settings: the XrmToolBox plugin's `Settings` fields
 * without the XTB-only ones (sorting, splitter position, tooltips, last used
 * organization URL, connection timeout). Per-solution configurations are
 * kept apart, as `SolutionConfiguration`s.
 */
export interface ToolSettings {
	publishAllPreExport: boolean;
	publishAllPostImport: boolean;
	updateVersion: boolean;
	exportManaged: boolean;
	exportUnmanaged: boolean;
	gitCommit: boolean;
	pushCommit: boolean;
	importManaged: boolean;
	importUnmanaged: boolean;
	upgrade: boolean;
	commitMessage: string;
	versionFormat: string;
	enableAutomation: boolean;
	overwriteCustomizations: boolean;
	saveVersionJson: boolean;
	showFriendlySolutionNames: boolean;
	showLogicalSolutionNames: boolean;
	retryCount: number;
	retryDelayInSeconds: number;
	continueOnError: boolean;
	autoDisableExportButtons: boolean;
}
