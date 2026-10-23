import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { DataverseMethod } from "./dataverseBridge";
import { createSolutionIdentifier, SETTINGS_KEY, SettingsStore } from "./settingsStore";
import type { RunReport, RunRequest, SolutionConfiguration } from "./types";
import { WORKER_ID, WorkerClient, WorkerMethod, WorkerNotification, type RunEnvironments } from "./workerClient";

const ENVIRONMENT_URL = "https://contoso.crm4.dynamics.com";
const UNIQUE_NAME = "ContosoCore";
const SOLUTION_ID = "6f1c3d2a-0000-4000-8000-000000000001";
const SOURCE_SLOT = 3;
const TARGET_SLOT = 1;
const RUN_ENVIRONMENTS: RunEnvironments = {
	source: { name: "Contoso Dev", connectionSlot: SOURCE_SLOT },
	targets: [{ name: "Contoso Test", connectionSlot: TARGET_SLOT }],
};
const RUN_REPORT: RunReport = { isAborted: false, durationInSeconds: 12, steps: [] };

type ConnectOptions = ToolBoxAPI.WorkerSessionOptions;
type RequestHandler = NonNullable<ConnectOptions["requests"]>[string];

interface SessionMock {
	session: ToolBoxAPI.WorkerSession;
	request: ReturnType<typeof vi.fn<ToolBoxAPI.WorkerSession["request"]>>;
	stop: ReturnType<typeof vi.fn<ToolBoxAPI.WorkerSession["stop"]>>;
	dispose: ReturnType<typeof vi.fn<ToolBoxAPI.WorkerSession["dispose"]>>;
}

let sessionMock: SessionMock;
let connectOptions: ConnectOptions;
let unloadHandlers: Map<string, () => void>;
let execute: ReturnType<typeof vi.fn<DataverseAPI.API["execute"]>>;
let settingsSet: ReturnType<typeof vi.fn<ToolBoxAPI.SettingsAPI["set"]>>;
let store: SettingsStore;

/** Creates a worker session stand-in whose `ready` settles as given. */
function createSession(ready: Promise<void>): SessionMock {
	const request = vi.fn<ToolBoxAPI.WorkerSession["request"]>().mockResolvedValue(undefined);
	const stop = vi.fn<ToolBoxAPI.WorkerSession["stop"]>().mockResolvedValue(undefined);
	const dispose = vi.fn<ToolBoxAPI.WorkerSession["dispose"]>().mockResolvedValue(undefined);
	const session: ToolBoxAPI.WorkerSession = {
		ready,
		request: request as ToolBoxAPI.WorkerSession["request"],
		notify: async () => undefined,
		cancel: () => undefined,
		stop,
		dispose,
	};

	return { session, request, stop, dispose };
}

/** Sends one worker notification to the client, as the session would. */
function notify(method: string, params: unknown): void {
	const handler = connectOptions.notifications?.[method];

	if (handler === undefined) {
		throw new Error(`No notification handler for ${method}`);
	}

	handler(params);
}

/** Sends one worker reverse call to the client, as the session would. */
function reverseCall(method: string, params: unknown): Promise<unknown> {
	const handler: RequestHandler | undefined = connectOptions.requests?.[method];

	if (handler === undefined) {
		throw new Error(`No request handler for ${method}`);
	}

	const context: ToolBoxAPI.WorkerRequestContext = {
		isCancellationRequested: false,
		onCancellationRequested: () => ({ dispose: () => undefined }),
	};

	return Promise.resolve(handler(params, context));
}

/** Creates a run request with no solutions. */
function createRunRequest(): RunRequest {
	return {
		shouldPublishSource: false,
		shouldUpdateVersion: false,
		shouldExportManaged: true,
		shouldExportUnmanaged: false,
		shouldCommitToGit: false,
		shouldPushCommit: false,
		shouldImportManaged: true,
		shouldImportUnmanaged: false,
		shouldUpgrade: true,
		shouldOverwriteCustomizations: false,
		shouldEnableAutomation: true,
		shouldPublishTargets: false,
		shouldSaveVersionJson: false,
		shouldContinueOnError: false,
		versionFormat: "YYYY.MM.DD.+",
		commitMessage: "commit",
		retryCount: 3,
		retryDelayInSeconds: 10,
		solutions: [],
	};
}

/** Stubs `window` with the PPTB APIs the client and the bridge use. */
function stubWindow(session: SessionMock): void {
	vi.stubGlobal("window", {
		toolboxAPI: {
			workers: {
				connect: vi.fn(async (_workerId: string, options: ConnectOptions) => {
					connectOptions = options;
					return session.session;
				}),
			},
		},
		dataverseAPI: { execute },
		addEventListener: (type: string, handler: () => void) => unloadHandlers.set(type, handler),
		removeEventListener: (type: string) => unloadHandlers.delete(type),
	});
}

beforeEach(async () => {
	unloadHandlers = new Map();
	execute = vi.fn<DataverseAPI.API["execute"]>().mockResolvedValue({});
	settingsSet = vi.fn<ToolBoxAPI.SettingsAPI["set"]>().mockResolvedValue(undefined);
	store = await SettingsStore.load({
		getAll: async () => ({}),
		get: async () => undefined,
		set: settingsSet,
		setAll: async () => undefined,
	});
	sessionMock = createSession(Promise.resolve());
	stubWindow(sessionMock);
});

afterEach(() => {
	vi.unstubAllGlobals();
	vi.clearAllMocks();
});

describe("WorkerClient", () => {
	describe("connect", () => {
		it("connect / ready worker / connects to the engine worker", async () => {
			await WorkerClient.connect(store);

			expect(window.toolboxAPI.workers.connect).toHaveBeenCalledWith(WORKER_ID, expect.any(Object));
		});

		it("connect / any worker / registers every bridge method by name", async () => {
			await WorkerClient.connect(store);

			expect(Object.keys(connectOptions.requests ?? {}).sort()).toEqual(Object.values(DataverseMethod).sort());
		});

		it("connect / worker fails to get ready / stops it and rejects with the failure", async () => {
			const failure = new Error("dotnet missing");
			sessionMock = createSession(Promise.reject(failure));
			stubWindow(sessionMock);

			await expect(WorkerClient.connect(store)).rejects.toBe(failure);

			expect(sessionMock.dispose).toHaveBeenCalledTimes(1);
		});
	});

	describe("notifications", () => {
		it("configurationChanged / run durations / reach the store", async () => {
			await WorkerClient.connect(store);
			const identifier = createSolutionIdentifier(ENVIRONMENT_URL, UNIQUE_NAME);
			const configuration: SolutionConfiguration = {
				solutionIndentifier: identifier,
				fileNameManaged: null,
				fileNameUnmanaged: null,
				checked: true,
				sortingIndex: 0,
				lastExportDurationUnamangedInSeconds: 11,
				lastExportDurationManagedInSeconds: 12,
				lastImportDurationManagedInSeconds: 13,
				lastImportDurationUnmanagedInSeconds: 14,
			};

			notify(WorkerNotification.ConfigurationChanged, { configuration });

			expect(store.getSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME)).toMatchObject({
				lastExportDurationUnamangedInSeconds: 11,
				lastImportDurationUnmanagedInSeconds: 14,
			});
		});

		it("solutionVersionChanged / listed solution / reaches the store", async () => {
			await WorkerClient.connect(store);
			store.setSolutions([
				{
					solutionId: SOLUTION_ID,
					uniqueName: UNIQUE_NAME,
					friendlyName: null,
					version: "1.0.0.0",
					isManaged: false,
					description: null,
				},
			]);

			notify(WorkerNotification.SolutionVersionChanged, { solutionId: SOLUTION_ID, version: "1.0.0.1" });

			expect(store.getSolutions()[0]?.version).toBe("1.0.0.1");
		});

		it("log and progress / subscribed listeners / receive the lines and steps", async () => {
			const client = await WorkerClient.connect(store);
			const lines: string[] = [];
			const onProgress = vi.fn();
			client.onLog((line) => lines.push(line));
			client.onProgress(onProgress);

			notify(WorkerNotification.Log, { line: "**Export** done" });
			notify(WorkerNotification.Progress, { message: "Exporting", detail: null, isNewStep: true });
			notify(WorkerNotification.Log, { line: 42 });

			expect(lines).toEqual(["**Export** done"]);
			expect(onProgress).toHaveBeenCalledWith({ message: "Exporting", detail: null, isNewStep: true });
		});

		it("onLog / removed listener / receives nothing", async () => {
			const client = await WorkerClient.connect(store);
			const onLog = vi.fn();
			const removeListener = client.onLog(onLog);

			removeListener();
			notify(WorkerNotification.Log, { line: "ignored" });

			expect(onLog).not.toHaveBeenCalled();
		});
	});

	describe("startRun", () => {
		it("startRun / any request / sends run/start with the request and environment names", async () => {
			const client = await WorkerClient.connect(store);
			const request = createRunRequest();
			sessionMock.request.mockResolvedValueOnce(RUN_REPORT);

			await expect(client.startRun(request, RUN_ENVIRONMENTS)).resolves.toBe(RUN_REPORT);

			expect(sessionMock.request).toHaveBeenCalledWith(WorkerMethod.StartRun, {
				request,
				sourceName: "Contoso Dev",
				targetNames: ["Contoso Test"],
			});
		});

		it("startRun / reverse call during the run / answers on the target's connection slot", async () => {
			const client = await WorkerClient.connect(store);
			sessionMock.request.mockImplementationOnce(async () => {
				await reverseCall(DataverseMethod.PublishAll, { target: 1 });
				return RUN_REPORT;
			});

			await client.startRun(createRunRequest(), RUN_ENVIRONMENTS);

			expect(execute).toHaveBeenCalledWith(expect.objectContaining({ operationName: "PublishAllXml" }), TARGET_SLOT);
		});

		it("startRun / reverse call after the run / rejects without calling Dataverse", async () => {
			const client = await WorkerClient.connect(store);
			sessionMock.request.mockResolvedValueOnce(RUN_REPORT);
			await client.startRun(createRunRequest(), RUN_ENVIRONMENTS);

			await expect(reverseCall(DataverseMethod.PublishAll, { target: 0 })).rejects.toThrow(/unknown connection target/);

			expect(execute).not.toHaveBeenCalled();
		});

		it("startRun / run already active / rejects the second run", async () => {
			const client = await WorkerClient.connect(store);
			sessionMock.request.mockReturnValueOnce(new Promise(() => undefined));
			void client.startRun(createRunRequest(), RUN_ENVIRONMENTS);

			await expect(client.startRun(createRunRequest(), RUN_ENVIRONMENTS)).rejects.toThrow(/already in progress/);
		});
	});

	describe("workspace calls", () => {
		it("abort / any time / sends run/abort", async () => {
			const client = await WorkerClient.connect(store);
			sessionMock.request.mockResolvedValueOnce(true);

			await expect(client.abort()).resolves.toBe(true);

			expect(sessionMock.request).toHaveBeenCalledWith(WorkerMethod.AbortRun);
		});

		it("switchBranch / file and branch / sends them as named parameters", async () => {
			const client = await WorkerClient.connect(store);

			await client.switchBranch("c:/repo/out/m.zip", "release");

			expect(sessionMock.request).toHaveBeenCalledWith(WorkerMethod.SwitchBranch, {
				filePath: "c:/repo/out/m.zip",
				branchName: "release",
			});
		});

		it("suggestFileNames / folder and names / sends them as named parameters", async () => {
			const client = await WorkerClient.connect(store);

			await client.suggestFileNames("c:/repo/out", [UNIQUE_NAME]);

			expect(sessionMock.request).toHaveBeenCalledWith(WorkerMethod.SuggestFileNames, {
				folderPath: "c:/repo/out",
				uniqueNames: [UNIQUE_NAME],
			});
		});
	});

	describe("unload", () => {
		it("pagehide / pending settings / stops the worker and flushes the settings", async () => {
			await WorkerClient.connect(store);
			store.updateSettings({ retryCount: 6 });

			unloadHandlers.get("pagehide")?.();
			await vi.waitFor(() => expect(sessionMock.stop).toHaveBeenCalledTimes(1));

			expect(settingsSet).toHaveBeenCalledWith(SETTINGS_KEY, expect.objectContaining({ retryCount: 6 }));
		});

		it("stop / connected client / removes the unload handler", async () => {
			const client = await WorkerClient.connect(store);

			await client.stop();

			expect(unloadHandlers.has("pagehide")).toBe(false);
		});
	});
});
