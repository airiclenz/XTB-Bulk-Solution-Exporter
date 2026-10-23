/**
 * The tool's connection to its .NET worker (`engine` in `pptb.config.json`):
 * typed calls for the run and the workspace methods, subscriptions to the
 * run's log, progress and processed solutions, and the Dataverse bridge that
 * answers the worker's `dataverse/*` reverse calls.
 *
 * The bridge's connection slots are those of the active run: `startRun` maps
 * worker target 0 to the source's slot and 1 to n to the targets' slots; a
 * reverse call outside a run rejects. The run's configuration and version
 * changes go to the `SettingsStore`. The worker stops when the page hides
 * (the tool closes) or on `stop()`; pending settings are flushed then too.
 */

import { createDataverseBridge, DataverseMethod, type ReverseCallHandler } from "./dataverseBridge";
import type { SettingsStore } from "./settingsStore";
import type {
	GitBranches,
	RunProgress,
	RunReport,
	RunRequest,
	RunSolution,
	SolutionConfiguration,
	SolutionFileStatus,
	SolutionInfo,
	SuggestedFileNames,
} from "./types";

/** The worker's id in `pptb.config.json`. */
export const WORKER_ID = "engine";

/** The worker's methods, by name. */
export const WorkerMethod = {
	StartRun: "run/start",
	AbortRun: "run/abort",
	GitBranches: "git/branches",
	SwitchBranch: "git/switchBranch",
	FileStatus: "files/status",
	ExportCsv: "files/exportCsv",
	SuggestFileNames: "files/suggestFileNames",
} as const;

/** The worker's run notifications, by name. */
export const WorkerNotification = {
	Log: "run/log",
	Progress: "run/progress",
	SolutionVersionChanged: "run/solutionVersionChanged",
	ConfigurationChanged: "run/configurationChanged",
	SolutionProcessed: "run/solutionProcessed",
} as const;

/** The page event on which the worker stops. */
const UNLOAD_EVENT = "pagehide";

const RUN_ALREADY_ACTIVE_MESSAGE = "A run is already in progress.";

/** One environment of a run: its display name and its PPTB connection slot. */
export interface RunEnvironment {
	name: string;
	/** The zero-based PPTB connection slot. */
	connectionSlot: number;
}

/** The environments of a run: the source and the targets in import order. */
export interface RunEnvironments {
	source: RunEnvironment;
	targets: readonly RunEnvironment[];
}

/** The connection to the worker; see the module comment. */
export class WorkerClient {
	readonly #session: ToolBoxAPI.WorkerSession;
	readonly #store: SettingsStore;
	readonly #logListeners = new Set<(line: string) => void>();
	readonly #progressListeners = new Set<(progress: RunProgress) => void>();
	readonly #solutionProcessedListeners = new Set<(solutionId: string) => void>();
	readonly #exitListeners = new Set<(snapshot: ToolBoxAPI.WorkerSnapshot) => void>();
	readonly #unloadHandler = (): void => {
		void Promise.allSettled([this.#store.flush(), this.stop()]).then(logRejections);
	};
	#bridge: Record<string, ReverseCallHandler> = createDataverseBridge([]);
	#isRunActive = false;

	private constructor(session: ToolBoxAPI.WorkerSession, store: SettingsStore) {
		this.#session = session;
		this.#store = store;
	}

	/**
	 * Starts the worker, registers the Dataverse bridge and the run
	 * notifications, and resolves once the worker is ready. A worker that
	 * fails to get ready is stopped and the failure rethrown.
	 */
	static async connect(store: SettingsStore): Promise<WorkerClient> {
		let client: WorkerClient | undefined;

		// The session exists before the client does; a call that arrives in
		// between finds no run, so a reverse call rejects and a notification is dropped.
		const withClient = <TResult>(use: (connected: WorkerClient) => TResult, fallback: () => TResult): TResult =>
			client === undefined ? fallback() : use(client);
		const rejectEarlyCall = (): Promise<unknown> =>
			Promise.reject(new Error("The worker called before its connection was set up."));
		const ignoreEarlyNotification = (): void => undefined;

		const requests: Record<string, ReverseCallHandler> = {};

		for (const method of Object.values(DataverseMethod)) {
			requests[method] = (params) =>
				withClient((connected) => connected.#answerReverseCall(method, params), rejectEarlyCall);
		}

		const session = await window.toolboxAPI.workers.connect(WORKER_ID, {
			requests,
			notifications: {
				[WorkerNotification.Log]: (params) =>
					withClient((connected) => connected.#receiveLog(params), ignoreEarlyNotification),
				[WorkerNotification.Progress]: (params) =>
					withClient((connected) => connected.#receiveProgress(params), ignoreEarlyNotification),
				[WorkerNotification.SolutionVersionChanged]: (params) =>
					withClient((connected) => connected.#receiveSolutionVersion(params), ignoreEarlyNotification),
				[WorkerNotification.ConfigurationChanged]: (params) =>
					withClient((connected) => connected.#receiveConfiguration(params), ignoreEarlyNotification),
				[WorkerNotification.SolutionProcessed]: (params) =>
					withClient((connected) => connected.#receiveSolutionProcessed(params), ignoreEarlyNotification),
			},
			onExit: (snapshot) => withClient((connected) => connected.#receiveExit(snapshot), ignoreEarlyNotification),
		});

		client = new WorkerClient(session, store);

		try {
			await session.ready;
		} catch (error: unknown) {
			await session.dispose().catch((disposeError: unknown) => {
				console.error("Stopping the worker that failed to start failed.", disposeError);
			});
			throw error;
		}

		window.addEventListener(UNLOAD_EVENT, client.#unloadHandler);
		return client;
	}

	/**
	 * Runs the request; resolves when the run ended or was aborted. One run at
	 * a time: a second call while a run is active rejects.
	 */
	async startRun(request: RunRequest, environments: RunEnvironments): Promise<RunReport> {
		if (this.#isRunActive) {
			throw new Error(RUN_ALREADY_ACTIVE_MESSAGE);
		}

		this.#isRunActive = true;
		this.#bridge = createDataverseBridge([
			environments.source.connectionSlot,
			...environments.targets.map((target) => target.connectionSlot),
		]);

		try {
			return await this.#session.request<RunReport>(WorkerMethod.StartRun, {
				request,
				sourceName: environments.source.name,
				targetNames: environments.targets.map((target) => target.name),
			});
		} finally {
			this.#bridge = createDataverseBridge([]);
			this.#isRunActive = false;
		}
	}

	/**
	 * Requests an abort of the active run; it stops between steps.
	 *
	 * @returns True when a run was active.
	 */
	abort(): Promise<boolean> {
		return this.#session.request<boolean>(WorkerMethod.AbortRun);
	}

	/** Lists the branches of the git repository that holds a file. */
	getBranches(filePath: string): Promise<GitBranches> {
		return this.#session.request<GitBranches>(WorkerMethod.GitBranches, { filePath });
	}

	/** Checks out a branch of the repository that holds a file; refused during a run. */
	switchBranch(filePath: string, branchName: string): Promise<GitBranches> {
		return this.#session.request<GitBranches>(WorkerMethod.SwitchBranch, { filePath, branchName });
	}

	/** Determines each solution's export file state, in the given order. */
	getFileStatus(solutions: readonly RunSolution[], shouldSaveVersionJson: boolean): Promise<SolutionFileStatus[]> {
		return this.#session.request<SolutionFileStatus[]>(WorkerMethod.FileStatus, {
			solutions,
			shouldSaveVersionJson,
		});
	}

	/** Writes the solution information CSV; an existing file is overwritten. */
	async exportCsv(filePath: string, solutions: readonly SolutionInfo[]): Promise<void> {
		await this.#session.request(WorkerMethod.ExportCsv, { filePath, solutions });
	}

	/** Proposes export paths in a folder for each unique name, in the given order. */
	suggestFileNames(folderPath: string, uniqueNames: readonly string[]): Promise<SuggestedFileNames[]> {
		return this.#session.request<SuggestedFileNames[]>(WorkerMethod.SuggestFileNames, {
			folderPath,
			uniqueNames,
		});
	}

	/**
	 * Registers a listener for the run's log lines (markup lines of the core run log).
	 *
	 * @returns A function that removes the listener.
	 */
	onLog(listener: (line: string) => void): () => void {
		return subscribe(this.#logListeners, listener);
	}

	/**
	 * Registers a listener for the run's progress.
	 *
	 * @returns A function that removes the listener.
	 */
	onProgress(listener: (progress: RunProgress) => void): () => void {
		return subscribe(this.#progressListeners, listener);
	}

	/**
	 * Registers a listener for each solution the run finished with.
	 *
	 * @returns A function that removes the listener.
	 */
	onSolutionProcessed(listener: (solutionId: string) => void): () => void {
		return subscribe(this.#solutionProcessedListeners, listener);
	}

	/**
	 * Registers a listener for the worker's exit (stopped or crashed).
	 *
	 * @returns A function that removes the listener.
	 */
	onExit(listener: (snapshot: ToolBoxAPI.WorkerSnapshot) => void): () => void {
		return subscribe(this.#exitListeners, listener);
	}

	/** Stops the worker; an active run ends with it. */
	async stop(): Promise<void> {
		window.removeEventListener(UNLOAD_EVENT, this.#unloadHandler);
		await this.#session.stop();
	}

	/** Answers one reverse call with the active run's bridge. */
	#answerReverseCall(method: string, params: unknown): Promise<unknown> {
		const handler = this.#bridge[method];

		if (handler === undefined) {
			return Promise.reject(new Error(`The tool does not answer ${method}.`));
		}

		return handler(params);
	}

	/** Handles `run/log`. */
	#receiveLog(params: unknown): void {
		const line = readField(params, "line");

		if (typeof line === "string") {
			this.#logListeners.forEach((listener) => listener(line));
		}
	}

	/** Handles `run/progress`. */
	#receiveProgress(params: unknown): void {
		const message = readField(params, "message");
		const detail = readField(params, "detail");
		const isNewStep = readField(params, "isNewStep");

		if (typeof message !== "string" || typeof isNewStep !== "boolean") {
			return;
		}

		const progress: RunProgress = { message, detail: typeof detail === "string" ? detail : null, isNewStep };
		this.#progressListeners.forEach((listener) => listener(progress));
	}

	/** Handles `run/solutionVersionChanged`: the store takes the new version. */
	#receiveSolutionVersion(params: unknown): void {
		const solutionId = readField(params, "solutionId");
		const version = readField(params, "version");

		if (typeof solutionId === "string" && typeof version === "string") {
			this.#store.applySolutionVersion(solutionId, version);
		}
	}

	/** Handles `run/configurationChanged`: the store merges the run's durations. */
	#receiveConfiguration(params: unknown): void {
		const configuration = readField(params, "configuration");

		if (isRunConfiguration(configuration)) {
			this.#store.applyRunConfiguration(configuration);
		}
	}

	/** Handles `run/solutionProcessed`. */
	#receiveSolutionProcessed(params: unknown): void {
		const solutionId = readField(params, "solutionId");

		if (typeof solutionId === "string") {
			this.#solutionProcessedListeners.forEach((listener) => listener(solutionId));
		}
	}

	/** Handles the worker's exit. */
	#receiveExit(snapshot: ToolBoxAPI.WorkerSnapshot): void {
		window.removeEventListener(UNLOAD_EVENT, this.#unloadHandler);
		this.#exitListeners.forEach((listener) => listener(snapshot));
	}
}

/** Adds a listener to a set and returns its removal. */
function subscribe<TListener>(listeners: Set<TListener>, listener: TListener): () => void {
	listeners.add(listener);
	return () => listeners.delete(listener);
}

/** Reads one named parameter of a notification; `undefined` when the parameters are no object. */
function readField(params: unknown, name: string): unknown {
	return typeof params === "object" && params !== null ? (params as Record<string, unknown>)[name] : undefined;
}

/** Whether a notification's configuration carries what the store merges. */
function isRunConfiguration(value: unknown): value is SolutionConfiguration {
	return (
		typeof readField(value, "solutionIndentifier") === "string" &&
		typeof readField(value, "lastExportDurationManagedInSeconds") === "number" &&
		typeof readField(value, "lastExportDurationUnamangedInSeconds") === "number" &&
		typeof readField(value, "lastImportDurationManagedInSeconds") === "number" &&
		typeof readField(value, "lastImportDurationUnmanagedInSeconds") === "number"
	);
}

/** Logs the rejected outcomes of the unload's stop and flush. */
function logRejections(outcomes: PromiseSettledResult<void>[]): void {
	outcomes
		.filter((outcome): outcome is PromiseRejectedResult => outcome.status === "rejected")
		.forEach((outcome) => console.error("Closing the tool's worker or settings failed.", outcome.reason));
}
