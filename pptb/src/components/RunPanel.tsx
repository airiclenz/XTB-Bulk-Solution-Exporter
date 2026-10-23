/**
 * The run panel: the Execute button, the run's progress and its log.
 *
 * Execute is enabled under the XrmToolBox plugin's `SetExportButtonState`
 * rules: at least one solution checked, at least one action on, a commit
 * message of at least `MIN_COMMIT_MESSAGE_LENGTH` characters when committing
 * and a valid version format when updating versions. When an export misses
 * a configured file the user confirms first, as in the plugin.
 *
 * A run sends `run/start` with a snapshot of the checked solutions (their
 * configurations copied) and the settings at the click. While it runs the
 * button is the Abort button (`run/abort` after a confirmation; the run stops
 * between steps), `onRunningChange` lets the caller lock the rest of the UI,
 * and `toolboxAPI.preventClose` keeps the tool from closing unwarned. After it
 * the log ends with "Done." or "Aborted by user.", the export switches turn
 * off when `autoDisableExportButtons` is set and the run succeeded, and
 * `onRunEnded` is called.
 *
 * The log renders the core run log's markup (see `logMarkup`) as text nodes
 * only; the markup's colours map to theme tokens so they stay readable in
 * the dark theme.
 */

import {
	Button,
	Dialog,
	DialogActions,
	DialogBody,
	DialogContent,
	DialogSurface,
	DialogTitle,
	makeStyles,
	Spinner,
	Text,
	tokens,
} from "@fluentui/react-components";
import { memo, useEffect, useRef, useState, type CSSProperties } from "react";

import { parseLogMarkup, type LogSpan } from "../logMarkup";
import type { SettingsStore } from "../settingsStore";
import type { RunProgress, RunRequest, RunSolution, ToolSettings } from "../types";
import { isValidVersionFormat } from "../versionFormat";
import type { RunEnvironments, WorkerClient } from "../workerClient";
import { MIN_COMMIT_MESSAGE_LENGTH } from "./RunOptions";

/** The worker calls the run panel makes. */
export type RunWorker = Pick<WorkerClient, "startRun" | "abort" | "onLog" | "onProgress">;

/** The properties of `RunPanel`. */
export interface RunPanelProps {
	/** The worker; Execute is disabled until it is connected. */
	worker: RunWorker | undefined;
	store: SettingsStore;
	environments: RunEnvironments;
	/** The checked solutions with their configurations, in run order. */
	checkedSolutions: readonly RunSolution[];
	/** Called with true when a run starts and with false when it ended. */
	onRunningChange: (isRunning: boolean) => void;
	/** Called after a run ended, failed or was aborted. */
	onRunEnded: () => void;
}

const RunPhase = {
	Idle: "idle",
	Running: "running",
	Aborting: "aborting",
} as const;

type RunPhase = (typeof RunPhase)[keyof typeof RunPhase];

/** A question the panel waits on: the files an export misses, or an abort. */
type PendingConfirmation = { kind: "missingFiles"; files: readonly string[] } | { kind: "abort" };

const COLOR_ERROR = "<color=#770000>";
const COLOR_GREEN = "<color=#227700>";
const COLOR_ORANGE = "<color=#CC6600>";
const COLOR_END_TAG = "</color>";
const DONE_LINE = `${COLOR_GREEN}##### Done.${COLOR_END_TAG}`;
const ABORTED_LINE = `${COLOR_ORANGE}##### Aborted by user.${COLOR_END_TAG}`;
const ABORTING_MESSAGE = "Aborting... Waiting for the current step to finish.";
const MAX_HEADING_LEVEL = 6;
const HEADING_SIZE_STEP_IN_PIXELS = 2;

/** The run log's `Color*` colours (core `RunLog`) as theme tokens; other colours show as given. */
const LOG_COLOR_TOKENS: Readonly<Record<string, string>> = {
	"#770000": tokens.colorPaletteRedForeground1,
	"#000077": tokens.colorPaletteBlueForeground2,
	"#777700": tokens.colorPaletteGoldForeground2,
	"#DDDDDD": tokens.colorNeutralStroke1,
	"#227700": tokens.colorPaletteGreenForeground1,
	"#CC6600": tokens.colorPaletteDarkOrangeForeground1,
	"#337799": tokens.colorPaletteSteelForeground2,
	"#CCCCCE": tokens.colorNeutralForegroundDisabled,
};

/** The colour of `code` outside a colour token, the plugin's DarkRed. */
const CODE_COLOR_TOKEN = tokens.colorPaletteDarkRedForeground2;

const useStyles = makeStyles({
	root: {
		display: "flex",
		flexDirection: "column",
		rowGap: tokens.spacingVerticalS,
	},
	toolbar: {
		display: "flex",
		alignItems: "center",
		columnGap: tokens.spacingHorizontalM,
	},
	log: {
		minHeight: "160px",
		maxHeight: "40vh",
		overflowY: "auto",
		padding: tokens.spacingHorizontalS,
		border: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke1}`,
		borderRadius: tokens.borderRadiusMedium,
		backgroundColor: tokens.colorNeutralBackground1,
		color: tokens.colorNeutralForeground1,
		whiteSpace: "pre-wrap",
		overflowWrap: "anywhere",
	},
	logLine: {
		minHeight: "1em",
	},
});

/** The run panel; see the module comment. */
export function RunPanel({
	worker,
	store,
	environments,
	checkedSolutions,
	onRunningChange,
	onRunEnded,
}: RunPanelProps) {
	const styles = useStyles();
	const [phase, setPhase] = useState<RunPhase>(RunPhase.Idle);
	const [progress, setProgress] = useState<RunProgress | null>(null);
	const [logLines, setLogLines] = useState<readonly string[]>([]);
	const [confirmation, setConfirmation] = useState<PendingConfirmation | null>(null);
	const phaseRef = useRef<RunPhase>(RunPhase.Idle);

	const changePhase = (next: RunPhase): void => {
		phaseRef.current = next;
		setPhase(next);
	};
	const appendLog = (line: string): void => setLogLines((lines) => [...lines, line]);

	useEffect(() => worker?.onLog(appendLog), [worker]);
	useEffect(
		() =>
			worker?.onProgress((next) => {
				// While aborting, the running step must not replace the aborting message.
				if (phaseRef.current === RunPhase.Running) {
					setProgress(next);
				}
			}),
		[worker],
	);

	const settings = store.getSettings();
	const isExecuteEnabled = worker !== undefined && canExecute(settings, checkedSolutions.length);

	const runNow = async (): Promise<void> => {
		if (worker === undefined) {
			return;
		}

		setLogLines([]);
		setProgress(null);
		changePhase(RunPhase.Running);
		onRunningChange(true);

		const request = createRunRequest(store.getSettings(), checkedSolutions);
		const report = await executeRun(worker, store, request, environments, appendLog);
		// A run that failed has no report; an abort requested before it failed still counts.
		const isAborted = report?.isAborted ?? phaseRef.current === RunPhase.Aborting;

		appendLog(isAborted ? ABORTED_LINE : DONE_LINE);

		if (report !== null && !isAborted && store.getSettings().autoDisableExportButtons) {
			store.updateSettings({ updateVersion: false, exportManaged: false, exportUnmanaged: false });
		}

		setProgress(null);
		changePhase(RunPhase.Idle);
		onRunningChange(false);
		onRunEnded();
	};

	const handleExecuteClick = (): void => {
		if (phase === RunPhase.Running) {
			setConfirmation({ kind: "abort" });
			return;
		}

		const missingFiles = findMissingFiles(settings, checkedSolutions);

		if (missingFiles.length > 0) {
			setConfirmation({ kind: "missingFiles", files: missingFiles });
			return;
		}

		void runNow();
	};

	const handleConfirm = (): void => {
		const confirmed = confirmation;
		setConfirmation(null);

		if (confirmed?.kind === "missingFiles") {
			void runNow();
			return;
		}

		// The run may have ended while the question was open.
		if (worker === undefined || phaseRef.current !== RunPhase.Running) {
			return;
		}

		changePhase(RunPhase.Aborting);
		worker.abort().catch((error: unknown) => appendLog(formatError("Requesting the abort failed", error)));
	};

	const progressText =
		phase === RunPhase.Aborting
			? ABORTING_MESSAGE
			: [progress?.message, progress?.detail].filter((part) => part !== undefined && part !== null).join(" - ");

	return (
		<div className={styles.root}>
			<div className={styles.toolbar}>
				<Button
					appearance="primary"
					disabled={phase === RunPhase.Aborting || (phase === RunPhase.Idle && !isExecuteEnabled)}
					onClick={handleExecuteClick}
				>
					{EXECUTE_BUTTON_LABELS[phase]}
				</Button>
				{phase === RunPhase.Idle ? null : <Spinner size="tiny" />}
				{phase === RunPhase.Idle ? null : <Text>{progressText}</Text>}
			</div>
			<LogView lines={logLines} />
			<ConfirmDialog confirmation={confirmation} onConfirm={handleConfirm} onCancel={() => setConfirmation(null)} />
		</div>
	);
}

const EXECUTE_BUTTON_LABELS: Record<RunPhase, string> = {
	[RunPhase.Idle]: "Execute",
	[RunPhase.Running]: "Abort",
	[RunPhase.Aborting]: "Aborting...",
};

/**
 * Runs the request while the tool is kept from closing.
 *
 * @returns The run's report, or `null` when the run failed (the failure is logged).
 */
async function executeRun(
	worker: RunWorker,
	store: SettingsStore,
	request: RunRequest,
	environments: RunEnvironments,
	appendLog: (line: string) => void,
): Promise<{ isAborted: boolean } | null> {
	// The run reads its own snapshot; saving first keeps the check state if the tool is closed anyway.
	await store.flush().catch((error: unknown) => console.error("Saving the settings before the run failed.", error));
	await window.toolboxAPI
		.preventClose()
		.catch((error: unknown) => console.error("Keeping the tool from closing failed.", error));

	try {
		return await worker.startRun(request, environments);
	} catch (error: unknown) {
		appendLog(formatError("The run failed", error));
		return null;
	} finally {
		await window.toolboxAPI
			.releasePreventClose()
			.catch((error: unknown) => console.error("Releasing the close prevention failed.", error));
	}
}

/** Whether Execute may start a run: `SetExportButtonState`'s rules. */
function canExecute(settings: Readonly<ToolSettings>, checkedCount: number): boolean {
	const hasAction =
		settings.exportManaged ||
		settings.exportUnmanaged ||
		settings.updateVersion ||
		settings.importManaged ||
		settings.importUnmanaged;
	const isCommitMessageTooShort = settings.gitCommit && settings.commitMessage.length < MIN_COMMIT_MESSAGE_LENGTH;
	const isVersionFormatInvalid = settings.updateVersion && !isValidVersionFormat(settings.versionFormat);

	return checkedCount > 0 && hasAction && !isCommitMessageTooShort && !isVersionFormatInvalid;
}

/** The run request of the settings and the checked solutions; each configuration is a copy. */
function createRunRequest(settings: Readonly<ToolSettings>, solutions: readonly RunSolution[]): RunRequest {
	return {
		shouldPublishSource: settings.publishAllPreExport,
		shouldUpdateVersion: settings.updateVersion,
		shouldExportManaged: settings.exportManaged,
		shouldExportUnmanaged: settings.exportUnmanaged,
		shouldCommitToGit: settings.gitCommit,
		shouldPushCommit: settings.pushCommit,
		shouldImportManaged: settings.importManaged,
		shouldImportUnmanaged: settings.importUnmanaged,
		shouldUpgrade: settings.upgrade,
		shouldOverwriteCustomizations: settings.overwriteCustomizations,
		shouldEnableAutomation: settings.enableAutomation,
		shouldPublishTargets: settings.publishAllPostImport,
		shouldSaveVersionJson: settings.saveVersionJson,
		shouldContinueOnError: settings.continueOnError,
		versionFormat: settings.versionFormat,
		commitMessage: settings.commitMessage,
		retryCount: settings.retryCount,
		retryDelayInSeconds: settings.retryDelayInSeconds,
		solutions: solutions.map(({ solution, configuration }) => ({
			solution: { ...solution },
			configuration: configuration === null ? null : { ...configuration },
		})),
	};
}

/** The export files the checked solutions miss, as "Name (Managed)" entries. */
function findMissingFiles(settings: Readonly<ToolSettings>, solutions: readonly RunSolution[]): string[] {
	return solutions.flatMap(({ solution, configuration }) => {
		const name = solution.friendlyName ?? solution.uniqueName ?? solution.solutionId;
		const missing: string[] = [];

		if (settings.exportManaged && !configuration?.fileNameManaged) {
			missing.push(`"${name}" (Managed)`);
		}

		if (settings.exportUnmanaged && !configuration?.fileNameUnmanaged) {
			missing.push(`"${name}" (Unmanaged)`);
		}

		return missing;
	});
}

/** An error log line. */
function formatError(context: string, error: unknown): string {
	const message = error instanceof Error ? error.message : String(error);
	return `${COLOR_ERROR}${context}: ${message}${COLOR_END_TAG}`;
}

/** The properties of `LogView`. */
interface LogViewProps {
	lines: readonly string[];
}

/** The run log, scrolled to its newest line. */
function LogView({ lines }: LogViewProps) {
	const styles = useStyles();
	const containerRef = useRef<HTMLDivElement>(null);

	useEffect(() => {
		const container = containerRef.current;

		if (container !== null) {
			container.scrollTop = container.scrollHeight;
		}
	}, [lines]);

	return (
		<div ref={containerRef} className={styles.log} role="log" aria-label="Run log">
			{lines.map((line, index) => (
				// The log only grows, or is emptied at a run's start, so an index identifies a line.
				<LogEntry key={index} markup={line} />
			))}
		</div>
	);
}

/** One log line, parsed once; it may render as several lines. */
const LogEntry = memo(function LogEntry({ markup }: { markup: string }) {
	const styles = useStyles();

	return (
		<>
			{parseLogMarkup(markup).map((spans, lineIndex) => (
				<div key={lineIndex} className={styles.logLine}>
					{spans.map((span, spanIndex) => (
						<span key={spanIndex} style={createSpanStyle(span)}>
							{span.text}
						</span>
					))}
				</div>
			))}
		</>
	);
});

/** The inline style of a span: weight, slant, heading size and a theme colour. */
function createSpanStyle(span: LogSpan): CSSProperties {
	const codeColor = span.isCode ? CODE_COLOR_TOKEN : undefined;
	const color = span.color === null ? codeColor : (LOG_COLOR_TOKENS[span.color] ?? span.color);
	const headingStep =
		span.headingLevel === null ? 0 : (MAX_HEADING_LEVEL - span.headingLevel) * HEADING_SIZE_STEP_IN_PIXELS;

	return {
		fontWeight: span.isBold ? tokens.fontWeightSemibold : undefined,
		fontStyle: span.isItalic ? "italic" : undefined,
		fontSize: headingStep === 0 ? undefined : `calc(1em + ${headingStep}px)`,
		color,
	};
}

/** The properties of `ConfirmDialog`. */
interface ConfirmDialogProps {
	confirmation: PendingConfirmation | null;
	onConfirm: () => void;
	onCancel: () => void;
}

/** Asks to run despite missing files, or to abort the run. */
function ConfirmDialog({ confirmation, onConfirm, onCancel }: ConfirmDialogProps) {
	const isAbort = confirmation?.kind === "abort";

	return (
		<Dialog open={confirmation !== null} onOpenChange={(_event, data) => !data.open && onCancel()}>
			<DialogSurface>
				<DialogBody>
					<DialogTitle>{isAbort ? "Abort Execution" : "Problems were found"}</DialogTitle>
					<DialogContent>
						{confirmation?.kind === "missingFiles" ? (
							<>
								<Text block>These files were not defined:</Text>
								{confirmation.files.map((file, index) => (
									// Two solutions may share a friendly name, so the position is the key.
									<Text block key={index}>
										{file}
									</Text>
								))}
								<Text block>Do you want to continue anyway?</Text>
							</>
						) : (
							<Text>Abort after the current step finishes?</Text>
						)}
					</DialogContent>
					<DialogActions>
						<Button appearance="primary" onClick={onConfirm}>
							{isAbort ? "Abort" : "Continue"}
						</Button>
						<Button onClick={onCancel}>{isAbort ? "Keep running" : "Cancel"}</Button>
					</DialogActions>
				</DialogBody>
			</DialogSurface>
		</Dialog>
	);
}
