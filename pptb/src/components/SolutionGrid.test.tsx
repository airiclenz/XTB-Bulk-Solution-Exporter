// @vitest-environment jsdom

import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import {
	createSolutionIdentifier,
	DEFAULT_SETTINGS,
	SETTINGS_KEY,
	SettingsStore,
	SOLUTION_CONFIGURATIONS_KEY,
} from "../settingsStore";
import type { SolutionConfiguration, SolutionFileStatus, SolutionInfo } from "../types";
import { SolutionGrid } from "./SolutionGrid";

const ENVIRONMENT_URL = "https://contoso.crm4.dynamics.com";

/** Loaded in this order; the versions sort differently as text and as numbers. */
const SOLUTIONS: readonly SolutionInfo[] = [
	createSolution("00000000-0000-4000-8000-000000000003", "gamma_ui", "Gamma", "1.5.0.0"),
	createSolution("00000000-0000-4000-8000-000000000001", "alpha_core", "Alpha", "10.0.0.0"),
	createSolution("00000000-0000-4000-8000-000000000002", "beta_tools", "Beta", "2.0.0.0"),
];

interface SettingsApiFake {
	stored: Map<string, unknown>;
	api: ToolBoxAPI.SettingsAPI;
}

/** Creates an in-memory stand-in of `toolboxAPI.settings`. */
function createSettingsApi(initial: Record<string, unknown> = {}): SettingsApiFake {
	const stored = new Map<string, unknown>(Object.entries(initial));
	const api: ToolBoxAPI.SettingsAPI = {
		getAll: async () => Object.fromEntries(stored),
		get: async (key) => stored.get(key),
		set: vi.fn<ToolBoxAPI.SettingsAPI["set"]>(async (key, value) => {
			stored.set(key, structuredClone(value));
		}),
		setAll: async () => undefined,
	};

	return { stored, api };
}

/** Creates a solution of the source list. */
function createSolution(solutionId: string, uniqueName: string, friendlyName: string, version: string): SolutionInfo {
	return { solutionId, uniqueName, friendlyName, version, isManaged: false, description: null };
}

/** Loads a store from the fake and lists `SOLUTIONS` in it. */
async function loadStore(settingsApi: SettingsApiFake): Promise<SettingsStore> {
	const store = await SettingsStore.load(settingsApi.api);
	store.setSolutions(SOLUTIONS);
	return store;
}

/** Renders the grid in a Fluent theme. */
function renderGrid(
	store: SettingsStore,
	targetSolutions: readonly SolutionInfo[] | null = null,
	fileStatuses: readonly SolutionFileStatus[] = [],
): void {
	render(
		<FluentProvider theme={webLightTheme}>
			<SolutionGrid
				store={store}
				environmentUrl={ENVIRONMENT_URL}
				targetSolutions={targetSolutions}
				fileStatuses={fileStatuses}
			/>
		</FluentProvider>,
	);
}

/** The friendly names of the shown rows, top to bottom. */
function readShownNames(): string[] {
	return screen
		.getAllByRole("row")
		.slice(1)
		.map((row) => SOLUTIONS.find((solution) => row.textContent?.includes(solution.friendlyName ?? ""))?.friendlyName)
		.filter((name): name is string => name !== undefined);
}

/** The stored configuration of a unique name after the store saved. */
async function readStoredConfiguration(
	store: SettingsStore,
	settingsApi: SettingsApiFake,
	uniqueName: string,
): Promise<SolutionConfiguration | undefined> {
	await act(() => store.flush());
	const configurations = settingsApi.stored.get(SOLUTION_CONFIGURATIONS_KEY) as Record<string, SolutionConfiguration>;
	return configurations?.[createSolutionIdentifier(ENVIRONMENT_URL, uniqueName)];
}

afterEach(() => {
	cleanup();
	vi.clearAllMocks();
});

describe("SolutionGrid", () => {
	describe("filter", () => {
		it("filter / friendly name text in another case / shows only the matching solution", async () => {
			const store = await loadStore(createSettingsApi());
			renderGrid(store);

			fireEvent.change(screen.getByRole("textbox", { name: "Filter solutions" }), { target: { value: "bEtA" } });

			expect(readShownNames()).toEqual(["Beta"]);
		});

		it("filter / logical name text while logical names are hidden / still matches", async () => {
			const store = await loadStore(createSettingsApi());
			renderGrid(store);

			fireEvent.change(screen.getByRole("textbox", { name: "Filter solutions" }), {
				target: { value: "gamma_" },
			});

			expect(readShownNames()).toEqual(["Gamma"]);
		});
	});

	describe("sorting", () => {
		it("sorting / no stored order / keeps the loaded order", async () => {
			const store = await loadStore(createSettingsApi());

			renderGrid(store);

			expect(readShownNames()).toEqual(["Gamma", "Alpha", "Beta"]);
		});

		it("sorting / name header clicked twice / sorts ascending then descending", async () => {
			const store = await loadStore(createSettingsApi());
			renderGrid(store);

			fireEvent.click(screen.getByText("Solution Name"));
			const ascending = readShownNames();
			fireEvent.click(screen.getByText("Solution Name"));

			expect([ascending, readShownNames()]).toEqual([
				["Alpha", "Beta", "Gamma"],
				["Gamma", "Beta", "Alpha"],
			]);
		});

		it("sorting / version header clicked / sorts versions numerically", async () => {
			const store = await loadStore(createSettingsApi());
			renderGrid(store);

			fireEvent.click(screen.getByText("Version"));

			expect(readShownNames()).toEqual(["Gamma", "Beta", "Alpha"]);
		});

		it("sorting / column sorted / persists the new order as sorting indexes", async () => {
			const settingsApi = createSettingsApi();
			const store = await loadStore(settingsApi);
			renderGrid(store);

			fireEvent.click(screen.getByText("Solution Name"));

			const indexes = await Promise.all(
				["alpha_core", "beta_tools", "gamma_ui"].map(async (uniqueName) =>
					(await readStoredConfiguration(store, settingsApi, uniqueName))?.sortingIndex,
				),
			);
			expect(indexes).toEqual([0, 1, 2]);
		});

		it("sorting / stored sorting indexes / shows the stored order", async () => {
			const settingsApi = createSettingsApi({
				[SOLUTION_CONFIGURATIONS_KEY]: Object.fromEntries(
					[
						["beta_tools", 0],
						["gamma_ui", 1],
						["alpha_core", 2],
					].map(([uniqueName, sortingIndex]) => {
						const identifier = createSolutionIdentifier(ENVIRONMENT_URL, String(uniqueName));
						return [identifier, { solutionIndentifier: identifier, sortingIndex }];
					}),
				),
			});
			const store = await loadStore(settingsApi);

			renderGrid(store);

			expect(readShownNames()).toEqual(["Beta", "Gamma", "Alpha"]);
		});

		it("sorting / solution moved down / swaps it with the next one and persists the order", async () => {
			const settingsApi = createSettingsApi();
			const store = await loadStore(settingsApi);
			renderGrid(store);

			fireEvent.click(screen.getByRole("button", { name: "Move Gamma down" }));

			expect(readShownNames()).toEqual(["Alpha", "Gamma", "Beta"]);
			expect((await readStoredConfiguration(store, settingsApi, "gamma_ui"))?.sortingIndex).toBe(1);
		});
	});

	describe("check state", () => {
		it("check state / solution checked / persists checked in its configuration", async () => {
			const settingsApi = createSettingsApi();
			const store = await loadStore(settingsApi);
			renderGrid(store);

			fireEvent.click(screen.getByRole("checkbox", { name: "Check Beta" }));

			expect((await readStoredConfiguration(store, settingsApi, "beta_tools"))?.checked).toBe(true);
		});

		it("check state / stored as checked / shows the solution checked after a reload", async () => {
			const settingsApi = createSettingsApi();
			const firstStore = await loadStore(settingsApi);
			renderGrid(firstStore);
			fireEvent.click(screen.getByRole("checkbox", { name: "Check Beta" }));
			await act(() => firstStore.flush());
			cleanup();

			renderGrid(await loadStore(settingsApi));

			expect(
				SOLUTIONS.map(
					(solution) =>
						(screen.getByRole("checkbox", { name: `Check ${solution.friendlyName}` }) as HTMLInputElement)
							.checked,
				),
			).toEqual([false, false, true]);
		});

		it("check state / check all while filtered / checks only the shown solutions", async () => {
			const settingsApi = createSettingsApi();
			const store = await loadStore(settingsApi);
			renderGrid(store);
			fireEvent.change(screen.getByRole("textbox", { name: "Filter solutions" }), { target: { value: "alpha" } });

			fireEvent.click(screen.getByRole("checkbox", { name: "Check all shown solutions" }));

			expect([
				(await readStoredConfiguration(store, settingsApi, "alpha_core"))?.checked,
				(await readStoredConfiguration(store, settingsApi, "beta_tools"))?.checked ?? false,
			]).toEqual([true, false]);
		});
	});

	describe("name columns", () => {
		it("name columns / defaults / show the friendly name column only", async () => {
			const store = await loadStore(createSettingsApi());

			renderGrid(store);

			expect([screen.queryByText("Solution Name") !== null, screen.queryByText("Logical Name") !== null]).toEqual(
				[true, false],
			);
		});

		it("name columns / friendly names turned off / turns logical names on", async () => {
			const store = await loadStore(createSettingsApi());
			renderGrid(store);

			fireEvent.click(screen.getByRole("switch", { name: "Friendly names" }));

			expect([
				screen.queryByText("Solution Name") !== null,
				screen.queryByText("Logical Name") !== null,
				store.getSettings().showLogicalSolutionNames,
			]).toEqual([false, true, true]);
		});

		it("name columns / logical names turned off / turns friendly names back on", async () => {
			const store = await loadStore(createSettingsApi());
			renderGrid(store);
			fireEvent.click(screen.getByRole("switch", { name: "Friendly names" }));

			fireEvent.click(screen.getByRole("switch", { name: "Logical names" }));

			expect([
				screen.queryByText("Solution Name") !== null,
				screen.queryByText("Logical Name") !== null,
				store.getSettings().showFriendlySolutionNames,
			]).toEqual([true, false, true]);
		});

		it("name columns / both stored as off / still shows the friendly name column", async () => {
			const settingsApi = createSettingsApi({
				[SETTINGS_KEY]: {
					...DEFAULT_SETTINGS,
					showFriendlySolutionNames: false,
					showLogicalSolutionNames: false,
				},
			});
			const store = await loadStore(settingsApi);

			renderGrid(store);

			expect(screen.queryByText("Solution Name")).not.toBeNull();
		});
	});

	describe("status icons", () => {
		it("status icons / one target / shows equal, different and missing versions", async () => {
			const store = await loadStore(createSettingsApi());
			const targetSolutions = [
				createSolution("10000000-0000-4000-8000-000000000001", "alpha_core", "Alpha", "10.0.0.0"),
				createSolution("10000000-0000-4000-8000-000000000002", "beta_tools", "Beta", "1.0.0.0"),
			];

			renderGrid(store, targetSolutions);

			expect(
				["Alpha", "Beta", "Gamma"].map((name) =>
					screen
						.getAllByRole("row")
						.find((row) => row.textContent?.includes(name))
						?.querySelector("[role=img]")
						?.getAttribute("aria-label"),
				),
			).toEqual([
				"The target has the same version.",
				"The target has a different version.",
				"The solution is not installed in the target.",
			]);
		});

		it("status icons / no single target / hides the target column", async () => {
			const store = await loadStore(createSettingsApi());

			renderGrid(store, null);

			expect(screen.queryByText("Target")).toBeNull();
		});

		it("status icons / file status / shows the configured files and the outdated warning", async () => {
			const store = await loadStore(createSettingsApi());
			const fileState = { isPresent: true, exportedVersion: "1.0.0.0", isOutdated: true };
			const fileStatuses: SolutionFileStatus[] = [
				{
					uniqueName: "alpha_core",
					managed: { ...fileState, isConfigured: true },
					unmanaged: { ...fileState, isConfigured: false },
					isOutdated: true,
				},
			];

			renderGrid(store, null, fileStatuses);

			expect(
				screen.getByRole("img", {
					name: "Managed file set, unmanaged file not set, the source version differs from the last export.",
				}).textContent,
			).toBe("M-");
		});
	});
});
