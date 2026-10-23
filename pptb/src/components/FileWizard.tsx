/**
 * The file-location wizard, as the XrmToolBox plugin's: the user picks a
 * folder and every checked solution's managed and unmanaged export paths
 * are set to the default file names in it (`files/suggestFileNames`). A
 * solution whose default names the worker refuses (they would resolve
 * outside the folder) keeps its paths and is named in a warning. The button
 * is disabled while no solution is checked or the worker is not connected.
 */

import { Button } from "@fluentui/react-components";
import { useState } from "react";

import type { SettingsStore } from "../settingsStore";
import type { SolutionInfo } from "../types";
import type { WorkerClient } from "../workerClient";

/** The worker call the wizard makes. */
export type FileNameWorker = Pick<WorkerClient, "suggestFileNames">;

/** The properties of `FileWizard`. */
export interface FileWizardProps {
	worker: FileNameWorker | undefined;
	store: SettingsStore;
	/** The source environment's URL, the first part of each configuration identifier. */
	environmentUrl: string;
	/** The solutions whose paths the wizard sets. */
	checkedSolutions: readonly SolutionInfo[];
}

const NOTIFICATION_TITLE = "File locations";
const DONE_MESSAGE = "The file locations have been updated.";

/** The wizard's button; see the module comment. */
export function FileWizard({ worker, store, environmentUrl, checkedSolutions }: FileWizardProps) {
	const [isBusy, setIsBusy] = useState(false);

	const handleClick = async (): Promise<void> => {
		if (worker === undefined) {
			return;
		}

		setIsBusy(true);

		try {
			const folderPath = await window.toolboxAPI.fileSystem.selectPath({
				type: "folder",
				title: "Folder of the export files",
			});

			if (folderPath !== null) {
				const refusedNames = await setFileLocations(worker, store, environmentUrl, checkedSolutions, folderPath);
				await notifyResult(refusedNames);
			}
		} catch (error: unknown) {
			console.error("Setting the file locations failed.", error);
			await notify("error", `Setting the file locations failed: ${describeError(error)}`);
		} finally {
			setIsBusy(false);
		}
	};

	return (
		<Button
			disabled={worker === undefined || checkedSolutions.length === 0 || isBusy}
			onClick={() => void handleClick()}
		>
			Set file locations...
		</Button>
	);
}

/**
 * Stores the suggested paths in each solution's configuration.
 *
 * @returns The unique names whose suggestion the worker refused.
 */
async function setFileLocations(
	worker: FileNameWorker,
	store: SettingsStore,
	environmentUrl: string,
	solutions: readonly SolutionInfo[],
	folderPath: string,
): Promise<string[]> {
	const uniqueNames = solutions.map((solution) => solution.uniqueName ?? "");
	const suggestions = await worker.suggestFileNames(folderPath, uniqueNames);
	const refusedNames: string[] = [];

	for (const { uniqueName, fileNameManaged, fileNameUnmanaged } of suggestions) {
		if (fileNameManaged === null || fileNameUnmanaged === null) {
			refusedNames.push(uniqueName);
			continue;
		}

		const configuration = store.getOrCreateSolutionConfiguration(environmentUrl, uniqueName);
		store.updateSolutionConfiguration({ ...configuration, fileNameManaged, fileNameUnmanaged });
	}

	return refusedNames;
}

/** Tells the user the wizard finished, naming the solutions it skipped. */
function notifyResult(refusedNames: readonly string[]): Promise<void> {
	if (refusedNames.length === 0) {
		return notify("success", DONE_MESSAGE);
	}

	return notify(
		"warning",
		`The default file names of these solutions resolve outside the selected folder; ` +
			`their file locations were not changed: ${refusedNames.join(", ")}`,
	);
}

/** Shows a notification; a failure to show it is only logged. */
async function notify(type: ToolBoxAPI.NotificationOptions["type"], body: string): Promise<void> {
	await window.toolboxAPI.utils
		.showNotification({ title: NOTIFICATION_TITLE, body, type })
		.catch((error: unknown) => console.error("Showing a notification failed.", error));
}

/** The message text of an error of any kind. */
function describeError(error: unknown): string {
	return error instanceof Error ? error.message : String(error);
}
