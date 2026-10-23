/**
 * The tool's settings, kept in memory and persisted through
 * `toolboxAPI.settings`: the global `ToolSettings` under one key and every
 * per-solution `SolutionConfiguration` under another, keyed by its
 * identifier (lower-cased environment URL + `.` + unique name).
 *
 * Changes apply in memory at once and reach PPTB debounced: one save per
 * burst of changes, `SAVE_DELAY_IN_MILLISECONDS` after the last one, or at
 * once on `flush()`. A failed save keeps the change pending for the next one
 * and is reported to `onSaveError`.
 *
 * The store also holds the source environment's solution list for the
 * session (never persisted), so the run's version changes reach the screen.
 * Every getter returns a frozen object that is replaced, never changed, so
 * subscribers can compare snapshots by reference.
 */

import type { SolutionConfiguration, SolutionInfo, ToolSettings } from "./types";

/** The wait after the last change before the store saves, in milliseconds. */
export const SAVE_DELAY_IN_MILLISECONDS = 500;

/** The `toolboxAPI.settings` key of the global settings. */
export const SETTINGS_KEY = "settings";

/** The `toolboxAPI.settings` key of the per-solution configurations. */
export const SOLUTION_CONFIGURATIONS_KEY = "solutionConfigurations";

/** The defaults of the XrmToolBox plugin's `new Settings()`, its constructor's values. */
export const DEFAULT_SETTINGS: Readonly<ToolSettings> = Object.freeze({
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

const IDENTIFIER_SEPARATOR = ".";

/** The options of `SettingsStore.load`. */
export interface SettingsStoreOptions {
	/** Receives the error of a failed save; defaults to `console.error`. */
	onSaveError?: (error: unknown) => void;
}

/**
 * Builds a solution's configuration identifier: the lower-cased environment
 * URL, a dot and the unique name.
 */
export function createSolutionIdentifier(environmentUrl: string, uniqueName: string): string {
	return `${environmentUrl.toLowerCase()}${IDENTIFIER_SEPARATOR}${uniqueName}`;
}

/** The settings of the tool; see the module comment. */
export class SettingsStore {
	readonly #settingsApi: ToolBoxAPI.SettingsAPI;
	readonly #onSaveError: (error: unknown) => void;
	readonly #listeners = new Set<() => void>();
	#settings: Readonly<ToolSettings>;
	#configurations: ReadonlyMap<string, Readonly<SolutionConfiguration>>;
	#solutions: readonly Readonly<SolutionInfo>[] = Object.freeze([]);
	#isSettingsDirty = false;
	#isConfigurationsDirty = false;
	#saveTimer: ReturnType<typeof setTimeout> | undefined;

	private constructor(
		settingsApi: ToolBoxAPI.SettingsAPI,
		settings: Readonly<ToolSettings>,
		configurations: ReadonlyMap<string, Readonly<SolutionConfiguration>>,
		options: SettingsStoreOptions,
	) {
		this.#settingsApi = settingsApi;
		this.#settings = settings;
		this.#configurations = configurations;
		this.#onSaveError = options.onSaveError ?? ((error) => console.error("Saving the settings failed.", error));
	}

	/**
	 * Reads the stored settings and configurations. Missing or mistyped
	 * stored values fall back to `DEFAULT_SETTINGS`; malformed configuration
	 * entries are dropped.
	 *
	 * @param settingsApi - The PPTB settings API; defaults to `window.toolboxAPI.settings`.
	 */
	static async load(
		settingsApi: ToolBoxAPI.SettingsAPI = window.toolboxAPI.settings,
		options: SettingsStoreOptions = {},
	): Promise<SettingsStore> {
		const [storedSettings, storedConfigurations] = await Promise.all([
			settingsApi.get(SETTINGS_KEY),
			settingsApi.get(SOLUTION_CONFIGURATIONS_KEY),
		]);

		return new SettingsStore(
			settingsApi,
			readSettings(storedSettings),
			readConfigurations(storedConfigurations),
			options,
		);
	}

	/** The global settings. */
	getSettings(): Readonly<ToolSettings> {
		return this.#settings;
	}

	/** Changes some global settings and schedules a save. */
	updateSettings(changes: Partial<ToolSettings>): void {
		this.#settings = Object.freeze({ ...this.#settings, ...changes });
		this.#isSettingsDirty = true;
		this.#changed();
	}

	/** A solution's configuration, or `undefined` when it has none. */
	getSolutionConfiguration(
		environmentUrl: string,
		uniqueName: string,
	): Readonly<SolutionConfiguration> | undefined {
		return this.#configurations.get(createSolutionIdentifier(environmentUrl, uniqueName));
	}

	/** A solution's configuration; a new, empty one is added (and saved) when it has none. */
	getOrCreateSolutionConfiguration(environmentUrl: string, uniqueName: string): Readonly<SolutionConfiguration> {
		const identifier = createSolutionIdentifier(environmentUrl, uniqueName);
		const existing = this.#configurations.get(identifier);

		if (existing !== undefined) {
			return existing;
		}

		const created = createConfiguration(identifier);
		this.#setConfiguration(created);
		return created;
	}

	/** Every configuration of one environment. */
	getSolutionConfigurations(environmentUrl: string): Readonly<SolutionConfiguration>[] {
		const prefix = createSolutionIdentifier(environmentUrl, "");

		return [...this.#configurations.values()].filter((configuration) =>
			configuration.solutionIndentifier.startsWith(prefix),
		);
	}

	/** Stores a configuration under its `solutionIndentifier` and schedules a save. */
	updateSolutionConfiguration(configuration: SolutionConfiguration): void {
		this.#setConfiguration(Object.freeze({ ...configuration }));
	}

	/** Removes a configuration by its identifier; nothing happens when it does not exist. */
	removeSolutionConfiguration(solutionIdentifier: string): void {
		if (!this.#configurations.has(solutionIdentifier)) {
			return;
		}

		const configurations = new Map(this.#configurations);
		configurations.delete(solutionIdentifier);
		this.#configurations = configurations;
		this.#isConfigurationsDirty = true;
		this.#changed();
	}

	/**
	 * Merges a run's configuration change (the worker's
	 * `run/configurationChanged`): only the last export and import durations
	 * are taken, since file names and check state may have changed during the
	 * run. A configuration that does not exist yet is created.
	 */
	applyRunConfiguration(runConfiguration: SolutionConfiguration): void {
		const identifier = runConfiguration.solutionIndentifier;
		const current = this.#configurations.get(identifier) ?? createConfiguration(identifier);

		this.#setConfiguration(
			Object.freeze({
				...current,
				lastExportDurationManagedInSeconds: runConfiguration.lastExportDurationManagedInSeconds,
				lastExportDurationUnamangedInSeconds: runConfiguration.lastExportDurationUnamangedInSeconds,
				lastImportDurationManagedInSeconds: runConfiguration.lastImportDurationManagedInSeconds,
				lastImportDurationUnmanagedInSeconds: runConfiguration.lastImportDurationUnmanagedInSeconds,
			}),
		);
	}

	/** The source environment's solutions, as last set. */
	getSolutions(): readonly Readonly<SolutionInfo>[] {
		return this.#solutions;
	}

	/** Replaces the source environment's solutions (not persisted). */
	setSolutions(solutions: readonly SolutionInfo[]): void {
		this.#solutions = Object.freeze(solutions.map((solution) => Object.freeze({ ...solution })));
		this.#changed();
	}

	/**
	 * Applies a run's version change (the worker's
	 * `run/solutionVersionChanged`) to the listed solution; an unlisted
	 * solution is ignored.
	 */
	applySolutionVersion(solutionId: string, version: string): void {
		const solutionKey = solutionId.toLowerCase();

		if (!this.#solutions.some((solution) => solution.solutionId.toLowerCase() === solutionKey)) {
			return;
		}

		this.#solutions = Object.freeze(
			this.#solutions.map((solution) =>
				solution.solutionId.toLowerCase() === solutionKey ? Object.freeze({ ...solution, version }) : solution,
			),
		);
		this.#changed();
	}

	/**
	 * Registers a listener called after every change.
	 *
	 * @returns A function that removes the listener.
	 */
	subscribe(listener: () => void): () => void {
		this.#listeners.add(listener);
		return () => this.#listeners.delete(listener);
	}

	/**
	 * Saves the pending changes now. A failed save keeps them pending and
	 * rejects with the settings API's error.
	 */
	async flush(): Promise<void> {
		clearTimeout(this.#saveTimer);
		this.#saveTimer = undefined;

		const saves: Promise<void>[] = [];

		if (this.#isSettingsDirty) {
			this.#isSettingsDirty = false;
			saves.push(this.#save(SETTINGS_KEY, this.#settings, () => (this.#isSettingsDirty = true)));
		}

		if (this.#isConfigurationsDirty) {
			this.#isConfigurationsDirty = false;
			saves.push(
				this.#save(SOLUTION_CONFIGURATIONS_KEY, Object.fromEntries(this.#configurations), () => {
					this.#isConfigurationsDirty = true;
				}),
			);
		}

		await Promise.all(saves);
	}

	/** Writes one key; on failure marks it dirty again through `markDirty` and rethrows. */
	async #save(key: string, value: unknown, markDirty: () => void): Promise<void> {
		try {
			await this.#settingsApi.set(key, value);
		} catch (error: unknown) {
			markDirty();
			throw error;
		}
	}

	/** Stores one configuration under its identifier and schedules a save. */
	#setConfiguration(configuration: Readonly<SolutionConfiguration>): void {
		this.#configurations = new Map(this.#configurations).set(configuration.solutionIndentifier, configuration);
		this.#isConfigurationsDirty = true;
		this.#changed();
	}

	/** Schedules the debounced save when something is pending and notifies the listeners. */
	#changed(): void {
		if (this.#isSettingsDirty || this.#isConfigurationsDirty) {
			clearTimeout(this.#saveTimer);
			this.#saveTimer = setTimeout(() => {
				this.flush().catch(this.#onSaveError);
			}, SAVE_DELAY_IN_MILLISECONDS);
		}

		this.#listeners.forEach((listener) => listener());
	}
}

/** Creates an empty configuration, the core `SolutionConfiguration`'s defaults. */
function createConfiguration(solutionIdentifier: string): Readonly<SolutionConfiguration> {
	return Object.freeze({
		solutionIndentifier: solutionIdentifier,
		fileNameManaged: null,
		fileNameUnmanaged: null,
		checked: false,
		sortingIndex: 0,
		lastExportDurationUnamangedInSeconds: 0,
		lastExportDurationManagedInSeconds: 0,
		lastImportDurationManagedInSeconds: 0,
		lastImportDurationUnmanagedInSeconds: 0,
	});
}

/** Reads stored global settings: every field of the stored value's matching type, else its default. */
function readSettings(stored: unknown): Readonly<ToolSettings> {
	const settings: Record<string, unknown> = { ...DEFAULT_SETTINGS };

	if (isRecord(stored)) {
		for (const [name, defaultValue] of Object.entries(DEFAULT_SETTINGS)) {
			if (typeof stored[name] === typeof defaultValue) {
				settings[name] = stored[name];
			}
		}
	}

	return Object.freeze(settings as unknown as ToolSettings);
}

/** Reads stored configurations; an entry without a string identifier is dropped. */
function readConfigurations(stored: unknown): ReadonlyMap<string, Readonly<SolutionConfiguration>> {
	const configurations = new Map<string, Readonly<SolutionConfiguration>>();

	if (!isRecord(stored)) {
		return configurations;
	}

	for (const entry of Object.values(stored)) {
		const configuration = readConfiguration(entry);

		if (configuration !== undefined) {
			configurations.set(configuration.solutionIndentifier, configuration);
		}
	}

	return configurations;
}

/** Reads one stored configuration: every field of its matching type, else the empty configuration's. */
function readConfiguration(stored: unknown): Readonly<SolutionConfiguration> | undefined {
	if (!isRecord(stored) || typeof stored.solutionIndentifier !== "string") {
		return undefined;
	}

	const defaults = createConfiguration(stored.solutionIndentifier);
	const configuration: Record<string, unknown> = { ...defaults };

	for (const [name, defaultValue] of Object.entries(defaults)) {
		const value = stored[name];
		const isFileName = defaultValue === null;

		if (isFileName ? typeof value === "string" : typeof value === typeof defaultValue) {
			configuration[name] = value;
		}
	}

	return Object.freeze(configuration as unknown as SolutionConfiguration);
}

/** Whether a value is a plain object (not `null`, not an array). */
function isRecord(value: unknown): value is Record<string, unknown> {
	return typeof value === "object" && value !== null && !Array.isArray(value);
}
