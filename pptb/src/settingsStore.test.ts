import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
	createSolutionIdentifier,
	DEFAULT_SETTINGS,
	SAVE_DELAY_IN_MILLISECONDS,
	SETTINGS_KEY,
	SettingsStore,
	SOLUTION_CONFIGURATIONS_KEY,
} from "./settingsStore";
import type { SolutionConfiguration, SolutionInfo } from "./types";

const ENVIRONMENT_URL = "https://contoso.crm4.dynamics.com";
const MIXED_CASE_ENVIRONMENT_URL = "https://Contoso.CRM4.dynamics.com";
const OTHER_ENVIRONMENT_URL = "https://fabrikam.crm.dynamics.com";
const UNIQUE_NAME = "ContosoCore";
const SOLUTION_ID = "6F1C3D2A-0000-4000-8000-000000000001";
const NEW_VERSION = "2026.10.23.1";
const XTB_ONLY_SETTING_NAME = /sorting|splitContainer|toolTips|lastUsedOrganizationWebappUrl|connectionTimeout/i;

interface SettingsApiMock {
	stored: Map<string, unknown>;
	api: ToolBoxAPI.SettingsAPI;
	set: ReturnType<typeof vi.fn<ToolBoxAPI.SettingsAPI["set"]>>;
}

/** Creates an in-memory stand-in of `toolboxAPI.settings`. */
function createSettingsApi(initial: Record<string, unknown> = {}): SettingsApiMock {
	const stored = new Map<string, unknown>(Object.entries(initial));
	const set = vi.fn<ToolBoxAPI.SettingsAPI["set"]>(async (key, value) => {
		stored.set(key, structuredClone(value));
	});
	const api: ToolBoxAPI.SettingsAPI = {
		getAll: async () => Object.fromEntries(stored),
		get: async (key) => stored.get(key),
		set,
		setAll: async () => undefined,
	};

	return { stored, api, set };
}

/** Creates a configuration with the given durations. */
function createRunConfiguration(identifier: string, seconds: number): SolutionConfiguration {
	return {
		solutionIndentifier: identifier,
		fileNameManaged: "run-managed.zip",
		fileNameUnmanaged: "run-unmanaged.zip",
		checked: false,
		sortingIndex: 9,
		lastExportDurationUnamangedInSeconds: seconds,
		lastExportDurationManagedInSeconds: seconds + 1,
		lastImportDurationManagedInSeconds: seconds + 2,
		lastImportDurationUnmanagedInSeconds: seconds + 3,
	};
}

/** Creates a solution of the source list. */
function createSolution(solutionId: string, version: string): SolutionInfo {
	return {
		solutionId,
		uniqueName: UNIQUE_NAME,
		friendlyName: "Contoso Core",
		version,
		isManaged: false,
		description: null,
	};
}

beforeEach(() => {
	vi.useFakeTimers();
});

afterEach(() => {
	vi.useRealTimers();
	vi.clearAllMocks();
});

describe("createSolutionIdentifier", () => {
	it("createSolutionIdentifier / mixed-case URL / lower-cases the URL and keeps the unique name", () => {
		expect(createSolutionIdentifier(MIXED_CASE_ENVIRONMENT_URL, UNIQUE_NAME)).toBe(
			`${ENVIRONMENT_URL}.${UNIQUE_NAME}`,
		);
	});
});

describe("SettingsStore", () => {
	describe("load", () => {
		it("load / nothing stored / uses the defaults of new Settings()", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);

			expect(store.getSettings()).toEqual({
				publishAllPreExport: false,
				publishAllPostImport: false,
				updateVersion: false,
				exportManaged: false,
				exportUnmanaged: false,
				gitCommit: false,
				pushCommit: false,
				importManaged: false,
				importUnmanaged: false,
				upgrade: true,
				commitMessage: "XTB Commit - YYYY.MM.DD",
				versionFormat: "YYYY.MM.DD.+",
				enableAutomation: true,
				overwriteCustomizations: false,
				saveVersionJson: false,
				showFriendlySolutionNames: true,
				showLogicalSolutionNames: false,
				retryCount: 3,
				retryDelayInSeconds: 10,
				continueOnError: false,
				autoDisableExportButtons: false,
			});
		});

		it("load / settings without XTB-only fields / leaves sorting, splitter, tooltips, URL and timeout out", () => {
			const settingNames = Object.keys(DEFAULT_SETTINGS);

			expect(settingNames.filter((name) => XTB_ONLY_SETTING_NAME.test(name))).toEqual([]);
		});

		it("load / partly stored or mistyped settings / keeps the valid values and defaults the rest", async () => {
			const settingsApi = createSettingsApi({
				[SETTINGS_KEY]: { retryCount: 7, upgrade: "yes", unknownField: true },
			});

			const settings = (await SettingsStore.load(settingsApi.api)).getSettings();

			expect(settings).toEqual({ ...DEFAULT_SETTINGS, retryCount: 7 });
		});

		it("load / stored configurations / reads them by URL and drops malformed entries", async () => {
			const identifier = createSolutionIdentifier(ENVIRONMENT_URL, UNIQUE_NAME);
			const settingsApi = createSettingsApi({
				[SOLUTION_CONFIGURATIONS_KEY]: {
					[identifier]: { solutionIndentifier: identifier, fileNameManaged: "c:/out/m.zip", checked: true },
					broken: { fileNameManaged: "no identifier" },
				},
			});

			const store = await SettingsStore.load(settingsApi.api);

			expect(store.getSolutionConfiguration(MIXED_CASE_ENVIRONMENT_URL, UNIQUE_NAME)).toMatchObject({
				solutionIndentifier: identifier,
				fileNameManaged: "c:/out/m.zip",
				fileNameUnmanaged: null,
				checked: true,
				sortingIndex: 0,
			});
			expect(store.getSolutionConfigurations(ENVIRONMENT_URL)).toHaveLength(1);
		});
	});

	describe("solution configurations", () => {
		it("getSolutionConfiguration / URL in another case / finds the same configuration", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			const created = store.getOrCreateSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME);

			const found = store.getSolutionConfiguration(MIXED_CASE_ENVIRONMENT_URL, UNIQUE_NAME);

			expect(found).toBe(created);
		});

		it("getSolutionConfiguration / unique name in another case / finds nothing", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			store.getOrCreateSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME);

			expect(store.getSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME.toLowerCase())).toBeUndefined();
		});

		it("getSolutionConfigurations / two environments / returns only the asked environment's", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			store.getOrCreateSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME);
			store.getOrCreateSolutionConfiguration(OTHER_ENVIRONMENT_URL, UNIQUE_NAME);

			const configurations = store.getSolutionConfigurations(MIXED_CASE_ENVIRONMENT_URL);

			expect(configurations.map((configuration) => configuration.solutionIndentifier)).toEqual([
				createSolutionIdentifier(ENVIRONMENT_URL, UNIQUE_NAME),
			]);
		});

		it("removeSolutionConfiguration / existing configuration / removes it", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			const created = store.getOrCreateSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME);

			store.removeSolutionConfiguration(created.solutionIndentifier);

			expect(store.getSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME)).toBeUndefined();
		});

		it("applyRunConfiguration / existing configuration / takes only the durations", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			const identifier = createSolutionIdentifier(ENVIRONMENT_URL, UNIQUE_NAME);
			store.updateSolutionConfiguration({
				...createRunConfiguration(identifier, 0),
				fileNameManaged: "user-managed.zip",
				checked: true,
				sortingIndex: 2,
			});

			store.applyRunConfiguration(createRunConfiguration(identifier, 40));

			expect(store.getSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME)).toMatchObject({
				fileNameManaged: "user-managed.zip",
				checked: true,
				sortingIndex: 2,
				lastExportDurationUnamangedInSeconds: 40,
				lastExportDurationManagedInSeconds: 41,
				lastImportDurationManagedInSeconds: 42,
				lastImportDurationUnmanagedInSeconds: 43,
			});
		});

		it("applyRunConfiguration / unknown configuration / creates it with the durations only", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			const identifier = createSolutionIdentifier(ENVIRONMENT_URL, UNIQUE_NAME);

			store.applyRunConfiguration(createRunConfiguration(identifier, 5));

			expect(store.getSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME)).toMatchObject({
				fileNameManaged: null,
				checked: false,
				lastExportDurationUnamangedInSeconds: 5,
			});
		});
	});

	describe("saving", () => {
		it("updateSettings / burst of changes / saves once after the delay", async () => {
			const settingsApi = createSettingsApi();
			const store = await SettingsStore.load(settingsApi.api);

			store.updateSettings({ retryCount: 4 });
			store.updateSettings({ gitCommit: true });
			await vi.advanceTimersByTimeAsync(SAVE_DELAY_IN_MILLISECONDS - 1);
			expect(settingsApi.set).not.toHaveBeenCalled();
			await vi.advanceTimersByTimeAsync(1);

			expect(settingsApi.set).toHaveBeenCalledTimes(1);
			expect(settingsApi.stored.get(SETTINGS_KEY)).toEqual({ ...DEFAULT_SETTINGS, retryCount: 4, gitCommit: true });
		});

		it("flush / changed configuration / saves it keyed by its identifier and reloads it", async () => {
			const settingsApi = createSettingsApi();
			const store = await SettingsStore.load(settingsApi.api);
			store.getOrCreateSolutionConfiguration(MIXED_CASE_ENVIRONMENT_URL, UNIQUE_NAME);

			await store.flush();
			const reloaded = await SettingsStore.load(settingsApi.api);

			expect(Object.keys(settingsApi.stored.get(SOLUTION_CONFIGURATIONS_KEY) as object)).toEqual([
				`${ENVIRONMENT_URL}.${UNIQUE_NAME}`,
			]);
			expect(reloaded.getSolutionConfiguration(ENVIRONMENT_URL, UNIQUE_NAME)).toBeDefined();
		});

		it("flush / failed save / reports the error and keeps the change pending", async () => {
			const settingsApi = createSettingsApi();
			const onSaveError = vi.fn();
			const saveFailure = new Error("disk full");
			settingsApi.set.mockRejectedValueOnce(saveFailure);
			const store = await SettingsStore.load(settingsApi.api, { onSaveError });

			store.updateSettings({ retryCount: 5 });
			await vi.advanceTimersByTimeAsync(SAVE_DELAY_IN_MILLISECONDS);
			await store.flush();

			expect(onSaveError).toHaveBeenCalledWith(saveFailure);
			expect(settingsApi.stored.get(SETTINGS_KEY)).toMatchObject({ retryCount: 5 });
		});
	});

	describe("solutions", () => {
		it("applySolutionVersion / listed solution, other GUID case / replaces its version and notifies", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			store.setSolutions([createSolution(SOLUTION_ID, "1.0.0.0")]);
			const listener = vi.fn();
			store.subscribe(listener);

			store.applySolutionVersion(SOLUTION_ID.toLowerCase(), NEW_VERSION);

			expect(store.getSolutions()[0]?.version).toBe(NEW_VERSION);
			expect(listener).toHaveBeenCalledTimes(1);
		});

		it("applySolutionVersion / unlisted solution / changes nothing", async () => {
			const store = await SettingsStore.load(createSettingsApi().api);
			store.setSolutions([createSolution(SOLUTION_ID, "1.0.0.0")]);
			const before = store.getSolutions();

			store.applySolutionVersion("00000000-0000-0000-0000-000000000000", NEW_VERSION);

			expect(store.getSolutions()).toBe(before);
		});
	});
});
