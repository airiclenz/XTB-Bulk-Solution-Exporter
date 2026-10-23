/**
 * The tool's main view: the run options above the solution grid, in the
 * ToolBox's light or dark theme.
 *
 * On start it loads the settings, reads the PPTB connections (slot 0 is the
 * source, every further assigned slot a target, in slot order), loads the
 * source's unmanaged solutions through the Dataverse bridge and, when exactly
 * one target is connected, that target's solutions for the version state
 * column. It then connects the worker, which reports each solution's export
 * file state (`files/status`) whenever the solutions, their file names or the
 * version-file setting change.
 */

import {
	FluentProvider,
	makeStyles,
	MessageBar,
	MessageBarBody,
	Spinner,
	tokens,
	webDarkTheme,
	webLightTheme,
	type Theme,
} from "@fluentui/react-components";
import { useEffect, useReducer, useState } from "react";

import { RunOptions } from "./components/RunOptions";
import { SolutionGrid } from "./components/SolutionGrid";
import { createDataverseBridge, DataverseMethod, type SolutionRecord } from "./dataverseBridge";
import { SettingsStore } from "./settingsStore";
import type { RunSolution, SolutionFileStatus, SolutionInfo } from "./types";
import { WorkerClient, type RunEnvironments } from "./workerClient";

const DARK_THEME_NAME = "dark";
const SETTINGS_UPDATED_EVENT = "settings:updated";
const SOURCE_SLOT = 0;
const SOURCE_TARGET = 0;
const SINGLE_TARGET = 1;
const NO_SOURCE_MESSAGE = "Connect a source environment to the tool.";

/** What the view works on once loaded. */
interface Workspace {
	store: SettingsStore;
	environments: RunEnvironments;
	/** The source environment's URL, the first part of each configuration identifier. */
	environmentUrl: string;
	/** The single target's solutions; `null` unless exactly one target is connected. */
	targetSolutions: readonly SolutionInfo[] | null;
}

const useStyles = makeStyles({
	root: {
		display: "flex",
		flexDirection: "column",
		rowGap: tokens.spacingVerticalL,
		minHeight: "100vh",
		padding: tokens.spacingHorizontalL,
		boxSizing: "border-box",
	},
});

/** The main view; see the module comment. */
export function App() {
	const styles = useStyles();
	const theme = useToolboxTheme();
	const [workspace, setWorkspace] = useState<Workspace>();
	const [worker, setWorker] = useState<WorkerClient>();
	const [fileStatuses, setFileStatuses] = useState<readonly SolutionFileStatus[]>([]);
	const [errorMessage, setErrorMessage] = useState<string>();
	const [, render] = useReducer((revision: number) => revision + 1, 0);

	useEffect(() => {
		let isCurrent = true;

		loadWorkspace()
			.then((loaded) => isCurrent && setWorkspace(loaded))
			.catch((error: unknown) => isCurrent && setErrorMessage(describeError(error)));

		return () => {
			isCurrent = false;
		};
	}, []);

	useEffect(() => workspace?.store.subscribe(render), [workspace]);
	useWorker(workspace?.store, setWorker, setErrorMessage);

	const runSolutions = workspace === undefined ? [] : createRunSolutions(workspace);
	const shouldSaveVersionJson = workspace?.store.getSettings().saveVersionJson ?? false;
	const fileStatusKey = createFileStatusKey(runSolutions, shouldSaveVersionJson);

	useEffect(() => {
		let isCurrent = true;

		worker
			?.getFileStatus(runSolutions, shouldSaveVersionJson)
			.then((statuses) => isCurrent && setFileStatuses(statuses))
			.catch((error: unknown) => isCurrent && setErrorMessage(describeError(error)));

		return () => {
			isCurrent = false;
		};
		// The key holds everything the answer depends on; the arrays are new on each render.
	}, [worker, fileStatusKey]);

	return (
		<FluentProvider theme={theme} className={styles.root}>
			{errorMessage === undefined ? null : (
				<MessageBar intent="error">
					<MessageBarBody>{errorMessage}</MessageBarBody>
				</MessageBar>
			)}
			{workspace === undefined ? (
				errorMessage === undefined && <Spinner label="Loading solutions..." />
			) : (
				<>
					<RunOptions
						settings={workspace.store.getSettings()}
						onChange={(changes) => workspace.store.updateSettings(changes)}
						hasTargets={workspace.environments.targets.length > 0}
					/>
					<SolutionGrid
						store={workspace.store}
						environmentUrl={workspace.environmentUrl}
						targetSolutions={workspace.targetSolutions}
						fileStatuses={fileStatuses}
					/>
				</>
			)}
		</FluentProvider>
	);
}

/** Follows the ToolBox's theme: read on start and again after each settings change. */
function useToolboxTheme(): Theme {
	const [theme, setTheme] = useState<Theme>(webLightTheme);

	useEffect(() => {
		const readTheme = (): void => {
			window.toolboxAPI.utils
				.getCurrentTheme()
				.then((name) => setTheme(name === DARK_THEME_NAME ? webDarkTheme : webLightTheme))
				.catch((error: unknown) => console.error("Reading the ToolBox theme failed.", error));
		};
		const handleEvent = (_event: unknown, payload: ToolBoxAPI.ToolBoxEventPayload): void => {
			if (payload.event === SETTINGS_UPDATED_EVENT) {
				readTheme();
			}
		};

		readTheme();
		window.toolboxAPI.events.on(handleEvent);
		return () => window.toolboxAPI.events.off(handleEvent);
	}, []);

	return theme;
}

/** Connects the worker once the store is loaded and stops it when the view goes. */
function useWorker(
	store: SettingsStore | undefined,
	setWorker: (worker: WorkerClient | undefined) => void,
	setErrorMessage: (message: string) => void,
): void {
	useEffect(() => {
		if (store === undefined) {
			return undefined;
		}

		let connected: WorkerClient | undefined;
		let isCurrent = true;

		WorkerClient.connect(store)
			.then((client) => {
				// The view went while the worker started: nothing will use it.
				if (!isCurrent) {
					return client.stop();
				}

				connected = client;
				setWorker(client);
				return undefined;
			})
			.catch((error: unknown) => isCurrent && setErrorMessage(describeError(error)));

		return () => {
			isCurrent = false;
			setWorker(undefined);
			connected?.stop().catch((error: unknown) => console.error("Stopping the worker failed.", error));
		};
	}, [store, setWorker, setErrorMessage]);
}

/** Loads the settings, the connections and the solutions; see the module comment. */
async function loadWorkspace(): Promise<Workspace> {
	const [store, connections] = await Promise.all([
		SettingsStore.load(),
		window.toolboxAPI.connections.getConnections(),
	]);
	const source = connections[SOURCE_SLOT];

	if (source === null || source === undefined) {
		throw new Error(NO_SOURCE_MESSAGE);
	}

	const targets = connections.flatMap((connection, slot) =>
		slot === SOURCE_SLOT || connection === null ? [] : [{ name: connection.name, connectionSlot: slot }],
	);
	const environments: RunEnvironments = { source: { name: source.name, connectionSlot: SOURCE_SLOT }, targets };
	const retrieveSolutions = createDataverseBridge([SOURCE_SLOT, ...targets.map((target) => target.connectionSlot)])[
		DataverseMethod.RetrieveSolutions
	];
	const [sourceSolutions, targetSolutions] = await Promise.all([
		retrieveSolutions({ target: SOURCE_TARGET, includeManaged: false }) as Promise<SolutionRecord[]>,
		targets.length === 1
			? (retrieveSolutions({ target: SINGLE_TARGET, includeManaged: true }) as Promise<SolutionRecord[]>)
			: Promise.resolve(null),
	]);

	store.setSolutions(
		[...sourceSolutions].sort((first, second) =>
			(first.friendlyName ?? "").localeCompare(second.friendlyName ?? "", undefined, { sensitivity: "base" }),
		),
	);

	return { store, environments, environmentUrl: source.url, targetSolutions };
}

/** Each source solution with its configuration, as `files/status` takes them. */
function createRunSolutions({ store, environmentUrl }: Workspace): RunSolution[] {
	return store.getSolutions().map((solution) => ({
		solution,
		configuration: store.getSolutionConfiguration(environmentUrl, solution.uniqueName ?? "") ?? null,
	}));
}

/** A text that changes exactly when the `files/status` answer may change. */
function createFileStatusKey(runSolutions: readonly RunSolution[], shouldSaveVersionJson: boolean): string {
	return JSON.stringify([
		shouldSaveVersionJson,
		runSolutions.map(({ solution, configuration }) => [
			solution.uniqueName,
			solution.version,
			configuration?.fileNameManaged ?? null,
			configuration?.fileNameUnmanaged ?? null,
		]),
	]);
}

/** The message text of an error of any kind. */
function describeError(error: unknown): string {
	return error instanceof Error ? error.message : String(error);
}
