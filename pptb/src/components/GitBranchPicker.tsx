/**
 * The git branch picker, as the XrmToolBox plugin's branch box: it lists the
 * local branches of the repository that holds an export file
 * (`git/branches`) with the checked-out one selected, and checks out the
 * branch the user picks (`git/switchBranch`). A refused switch is reported
 * and the list re-read. Without a file or outside a repository nothing is
 * shown.
 */

import { Dropdown, Field, Option } from "@fluentui/react-components";
import { useEffect, useState } from "react";

import type { GitBranches } from "../types";
import type { WorkerClient } from "../workerClient";

/** The worker calls the picker makes. */
export type BranchWorker = Pick<WorkerClient, "getBranches" | "switchBranch">;

/** The properties of `GitBranchPicker`. */
export interface GitBranchPickerProps {
	worker: BranchWorker | undefined;
	/** An export file whose repository the picker shows; `null` when no file is configured. */
	filePath: string | null;
	/** Whether the picker is disabled; the plugin enables it only while committing is on. */
	isDisabled: boolean;
}

/** The branch picker; see the module comment. */
export function GitBranchPicker({ worker, filePath, isDisabled }: GitBranchPickerProps) {
	const [branches, setBranches] = useState<GitBranches | null>(null);
	const [isSwitching, setIsSwitching] = useState(false);

	useEffect(() => {
		let isCurrent = true;

		if (worker === undefined || filePath === null) {
			setBranches(null);
			return undefined;
		}

		// readBranches never rejects: a failure is logged and reads as no repository.
		void readBranches(worker, filePath).then((read) => isCurrent && setBranches(read));

		return () => {
			isCurrent = false;
		};
	}, [worker, filePath]);

	if (worker === undefined || filePath === null || branches === null || branches.repositoryRoot === null) {
		return null;
	}

	const switchBranch = async (branchName: string): Promise<void> => {
		setIsSwitching(true);

		try {
			setBranches(await worker.switchBranch(filePath, branchName));
		} catch (error: unknown) {
			console.error("Switching the git branch failed.", error);
			await window.toolboxAPI.utils
				.showNotification({
					title: "Git branch",
					body: `Switching to ${branchName} failed: ${error instanceof Error ? error.message : String(error)}`,
					type: "error",
				})
				.catch((notifyError: unknown) => console.error("Showing a notification failed.", notifyError));
			setBranches(await readBranches(worker, filePath));
		} finally {
			setIsSwitching(false);
		}
	};

	return (
		<Field label="Git branch" orientation="horizontal">
			<Dropdown
				aria-label="Git branch"
				value={branches.activeBranch}
				selectedOptions={[branches.activeBranch]}
				disabled={isDisabled || isSwitching}
				onOptionSelect={(_event, data) => {
					if (data.optionValue !== undefined && data.optionValue !== branches.activeBranch) {
						void switchBranch(data.optionValue);
					}
				}}
			>
				{branches.branches.map((branch) => (
					<Option key={branch} value={branch}>
						{branch}
					</Option>
				))}
			</Dropdown>
		</Field>
	);
}

/** The repository's branches, or `null` when they cannot be read (the failure is logged). */
async function readBranches(worker: BranchWorker, filePath: string): Promise<GitBranches | null> {
	try {
		return await worker.getBranches(filePath);
	} catch (error: unknown) {
		console.error("Reading the git branches failed.", error);
		return null;
	}
}
