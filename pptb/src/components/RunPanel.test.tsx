// @vitest-environment jsdom

import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { act, cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { createSolutionIdentifier, SettingsStore } from "../settingsStore";
import type { RunReport, RunSolution, ToolSettings } from "../types";
import type { RunEnvironments } from "../workerClient";
import { RunPanel, type RunWorker } from "./RunPanel";

const ENVIRONMENT_URL = "https://contoso.crm4.dynamics.com";
const ENVIRONMENTS: RunEnvironments = { source: { name: "Contoso Dev", connectionSlot: 0 }, targets: [] };
const VALID_COMMIT_MESSAGE = "Release";
const FINISHED_REPORT: RunReport = { isAborted: false, durationInSeconds: 1, steps: [] };

/** One checked solution with both export files configured. */
const CHECKED_SOLUTION: RunSolution = {
	solution: {
		solutionId: "00000000-0000-4000-8000-000000000001",
		uniqueName: "alpha_core",
		friendlyName: "Alpha",
		version: "1.0.0.0",
		isManaged: false,
		description: null,
	},
	configuration: {
		solutionIndentifier: createSolutionIdentifier(ENVIRONMENT_URL, "alpha_core"),
		fileNameManaged: "C:/exports/alpha_core_managed.zip",
		fileNameUnmanaged: "C:/exports/alpha_core.zip",
		checked: true,
		sortingIndex: 0,
		lastExportDurationUnamangedInSeconds: 0,
		lastExportDurationManagedInSeconds: 0,
		lastImportDurationManagedInSeconds: 0,
		lastImportDurationUnmanagedInSeconds: 0,
	},
};

interface WorkerFake {
	worker: RunWorker;
	startRun: ReturnType<typeof vi.fn<RunWorker["startRun"]>>;
	abort: ReturnType<typeof vi.fn<RunWorker["abort"]>>;
	/** Sends a `run/log` line to the panel. */
	emitLog: (line: string) => void;
	/** Ends the pending run with a report. */
	finishRun: (report: RunReport) => void;
}

interface ToolboxFake {
	preventClose: ReturnType<typeof vi.fn<() => Promise<boolean>>>;
	releasePreventClose: ReturnType<typeof vi.fn<() => Promise<boolean>>>;
}

/** Creates a worker stand-in whose run stays pending until `finishRun`. */
function createWorker(): WorkerFake {
	const logListeners = new Set<(line: string) => void>();
	let resolveRun: ((report: RunReport) => void) | undefined;
	const startRun = vi.fn<RunWorker["startRun"]>(
		() =>
			new Promise<RunReport>((resolve) => {
				resolveRun = resolve;
			}),
	);
	const abort = vi.fn<RunWorker["abort"]>(async () => true);
	const worker: RunWorker = {
		startRun,
		abort,
		onLog: (listener) => {
			logListeners.add(listener);
			return () => logListeners.delete(listener);
		},
		onProgress: () => () => undefined,
	};

	return {
		worker,
		startRun,
		abort,
		emitLog: (line) => logListeners.forEach((listener) => listener(line)),
		finishRun: (report) => resolveRun?.(report),
	};
}

/** Loads a store from an in-memory settings API with some settings changed. */
async function loadStore(changes: Partial<ToolSettings>): Promise<SettingsStore> {
	const stored = new Map<string, unknown>();
	const store = await SettingsStore.load({
		getAll: async () => Object.fromEntries(stored),
		get: async (key) => stored.get(key),
		set: async (key, value) => {
			stored.set(key, value);
		},
		setAll: async () => undefined,
	});

	store.updateSettings(changes);
	return store;
}

/** Renders the panel in a Fluent theme. */
function renderPanel(
	worker: RunWorker,
	store: SettingsStore,
	onRunEnded: () => void = () => undefined,
	checkedSolutions: readonly RunSolution[] = [CHECKED_SOLUTION],
): void {
	render(
		<FluentProvider theme={webLightTheme}>
			<RunPanel
				worker={worker}
				store={store}
				environments={ENVIRONMENTS}
				checkedSolutions={checkedSolutions}
				onRunningChange={() => undefined}
				onRunEnded={onRunEnded}
			/>
		</FluentProvider>,
	);
}

let toolbox: ToolboxFake;

beforeEach(() => {
	toolbox = {
		preventClose: vi.fn(async () => true),
		releasePreventClose: vi.fn(async () => true),
	};
	vi.stubGlobal("toolboxAPI", toolbox);
});

afterEach(() => {
	cleanup();
	vi.unstubAllGlobals();
	vi.clearAllMocks();
});

describe("RunPanel", () => {
	describe("execute state", () => {
		it.each([
			["abcd", true],
			["abcde", false],
		])("execute / commit on with message %j / disabled is %s", async (commitMessage, isDisabled) => {
			const store = await loadStore({ exportManaged: true, gitCommit: true, commitMessage });

			renderPanel(createWorker().worker, store);

			expect((screen.getByRole("button", { name: "Execute" }) as HTMLButtonElement).disabled).toBe(isDisabled);
		});

		it("execute / no solution checked / is disabled", async () => {
			const store = await loadStore({ exportManaged: true });

			renderPanel(createWorker().worker, store, () => undefined, []);

			expect((screen.getByRole("button", { name: "Execute" }) as HTMLButtonElement).disabled).toBe(true);
		});

		it("execute / update version with an invalid format / is disabled", async () => {
			const store = await loadStore({ updateVersion: true, versionFormat: "YYYY.X" });

			renderPanel(createWorker().worker, store);

			expect((screen.getByRole("button", { name: "Execute" }) as HTMLButtonElement).disabled).toBe(true);
		});
	});

	describe("run", () => {
		it("execute / click / starts the run with the checked snapshot", async () => {
			const fake = createWorker();
			const store = await loadStore({ exportManaged: true, gitCommit: true, commitMessage: VALID_COMMIT_MESSAGE });
			renderPanel(fake.worker, store);

			fireEvent.click(screen.getByRole("button", { name: "Execute" }));

			await waitFor(() => expect(fake.startRun).toHaveBeenCalledOnce());
			expect(fake.startRun.mock.calls[0][0]).toMatchObject({
				shouldExportManaged: true,
				shouldCommitToGit: true,
				commitMessage: VALID_COMMIT_MESSAGE,
				solutions: [CHECKED_SOLUTION],
			});
		});

		it("execute / run in progress / holds the close prevention until the run ends", async () => {
			const fake = createWorker();
			const onRunEnded = vi.fn();
			const store = await loadStore({ exportManaged: true });
			renderPanel(fake.worker, store, onRunEnded);

			fireEvent.click(screen.getByRole("button", { name: "Execute" }));
			await waitFor(() => expect(fake.startRun).toHaveBeenCalledOnce());
			const wasReleasedDuringRun = toolbox.releasePreventClose.mock.calls.length > 0;
			await act(async () => fake.finishRun(FINISHED_REPORT));
			await waitFor(() => expect(onRunEnded).toHaveBeenCalledOnce());

			expect([
				toolbox.preventClose.mock.calls.length,
				wasReleasedDuringRun,
				toolbox.releasePreventClose.mock.calls.length,
			]).toEqual([1, false, 1]);
		});

		it("execute / successful run with auto-disable on / turns the export switches off", async () => {
			const fake = createWorker();
			const onRunEnded = vi.fn();
			const store = await loadStore({ exportManaged: true, updateVersion: true, autoDisableExportButtons: true });
			renderPanel(fake.worker, store, onRunEnded);

			fireEvent.click(screen.getByRole("button", { name: "Execute" }));
			await waitFor(() => expect(fake.startRun).toHaveBeenCalledOnce());
			await act(async () => fake.finishRun(FINISHED_REPORT));
			await waitFor(() => expect(onRunEnded).toHaveBeenCalledOnce());

			expect(store.getSettings()).toMatchObject({ exportManaged: false, updateVersion: false });
		});

		it("abort / confirmed during a run / requests the abort", async () => {
			const fake = createWorker();
			const store = await loadStore({ exportManaged: true });
			renderPanel(fake.worker, store);
			fireEvent.click(screen.getByRole("button", { name: "Execute" }));
			await waitFor(() => expect(fake.startRun).toHaveBeenCalledOnce());

			fireEvent.click(screen.getByRole("button", { name: "Abort" }));
			fireEvent.click(within(await screen.findByRole("dialog")).getByRole("button", { name: "Abort" }));

			await waitFor(() => expect(fake.abort).toHaveBeenCalledOnce());
		});

		it("execute / export file missing / asks before running", async () => {
			const fake = createWorker();
			const store = await loadStore({ exportManaged: true });
			const withoutFiles: RunSolution = {
				...CHECKED_SOLUTION,
				configuration: { ...CHECKED_SOLUTION.configuration!, fileNameManaged: null },
			};
			renderPanel(fake.worker, store, () => undefined, [withoutFiles]);

			fireEvent.click(screen.getByRole("button", { name: "Execute" }));

			expect(within(await screen.findByRole("dialog")).getByText(`"Alpha" (Managed)`)).toBeTruthy();
			expect(fake.startRun).not.toHaveBeenCalled();
		});
	});

	describe("log", () => {
		it("log / hostile markup line / renders it as text without elements", async () => {
			const fake = createWorker();
			const store = await loadStore({});
			const hostileLine = `<script>alert("x")</script><img src=x onerror="alert(1)">`;
			renderPanel(fake.worker, store);

			act(() => fake.emitLog(hostileLine));

			const log = screen.getByRole("log");
			expect([log.querySelector("script, img"), log.textContent]).toEqual([null, hostileLine]);
		});

		it("log / coloured heading / renders the text bold and coloured", async () => {
			const fake = createWorker();
			const store = await loadStore({});
			renderPanel(fake.worker, store);

			act(() => fake.emitLog("##### <color=#227700>Done.</color>"));

			const span = within(screen.getByRole("log")).getByText("Done.");
			expect([span.style.fontWeight !== "", span.style.color !== ""]).toEqual([true, true]);
		});
	});
});
