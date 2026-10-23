/**
 * The source environment's solutions as a grid: a check box per solution,
 * its friendly and/or logical name, its version, its version state in the
 * single target (only when exactly one target is connected), its export file
 * state and buttons that move it up or down.
 *
 * The grid reads the solutions, the name settings and each solution's
 * configuration from the `SettingsStore` and writes the check state and the
 * order back to it, so both persist. The order is the configurations'
 * `sortingIndex`, which is also the run's processing order. A click on a name
 * or version header sorts by that column and rewrites the order to match, as
 * the XrmToolBox plugin's list does; moving a solution clears the column sort.
 *
 * The filter matches friendly and logical names, case-insensitively; checking
 * or unchecking all only affects the solutions the filter shows. At least one
 * of the two name columns is always shown: turning one off turns the other on.
 */

import {
	Button,
	createTableColumn,
	DataGrid,
	DataGridBody,
	DataGridCell,
	DataGridHeader,
	DataGridHeaderCell,
	DataGridRow,
	Input,
	makeStyles,
	Switch,
	Text,
	tokens,
	type DataGridProps,
	type TableColumnDefinition,
} from "@fluentui/react-components";
import {
	ArrowDownRegular,
	ArrowSyncCircleFilled,
	ArrowUpRegular,
	CheckmarkCircleFilled,
	DismissCircleRegular,
	SearchRegular,
	WarningFilled,
} from "@fluentui/react-icons";
import { useEffect, useReducer, useState } from "react";

import type { SettingsStore } from "../settingsStore";
import type { SolutionFileStatus, SolutionInfo, ToolSettings } from "../types";

/** The state of a solution's version in the single target environment. */
export const TargetVersionState = {
	Equal: "equal",
	Different: "different",
	Missing: "missing",
} as const;

export type TargetVersionState = (typeof TargetVersionState)[keyof typeof TargetVersionState];

/** The properties of `SolutionGrid`. */
export interface SolutionGridProps {
	store: SettingsStore;
	/** The source environment's URL, the first part of each configuration identifier. */
	environmentUrl: string;
	/** The single target's solutions; `null` unless exactly one target is connected. */
	targetSolutions: readonly SolutionInfo[] | null;
	/** The `files/status` answers, matched to the solutions by unique name. */
	fileStatuses: readonly SolutionFileStatus[];
}

/** One row of the grid. */
interface SolutionRow {
	solution: Readonly<SolutionInfo>;
	targetState: TargetVersionState | null;
	fileStatus: SolutionFileStatus | undefined;
}

type SortState = NonNullable<DataGridProps["sortState"]>;
type NameSetting = "showFriendlySolutionNames" | "showLogicalSolutionNames";

const ColumnId = {
	FriendlyName: "friendlyName",
	LogicalName: "logicalName",
	Version: "version",
	Target: "target",
	Files: "files",
	Order: "order",
} as const;

const UNSORTED: SortState = { sortColumn: undefined, sortDirection: "ascending" };
const MOVE_UP = -1;
const MOVE_DOWN = 1;
const VERSION_SEPARATOR = ".";
const FILE_CONFIGURED_MANAGED = "M";
const FILE_CONFIGURED_UNMANAGED = "U";
const FILE_NOT_CONFIGURED = "-";

const TARGET_STATE_LABELS: Record<TargetVersionState, string> = {
	[TargetVersionState.Equal]: "The target has the same version.",
	[TargetVersionState.Different]: "The target has a different version.",
	[TargetVersionState.Missing]: "The solution is not installed in the target.",
};

const useStyles = makeStyles({
	root: {
		display: "flex",
		flexDirection: "column",
		rowGap: tokens.spacingVerticalS,
	},
	toolbar: {
		display: "flex",
		flexWrap: "wrap",
		alignItems: "center",
		columnGap: tokens.spacingHorizontalL,
	},
	equal: { color: tokens.colorPaletteGreenForeground1 },
	different: { color: tokens.colorPaletteMarigoldForeground1 },
	missing: { color: tokens.colorNeutralForeground3 },
	outdated: { color: tokens.colorPaletteMarigoldForeground1 },
	fileState: { fontFamily: tokens.fontFamilyMonospace },
});

/** The comparers of the sortable columns, by column id. */
const SORT_COMPARERS = {
	[ColumnId.FriendlyName]: (first: SolutionInfo, second: SolutionInfo) =>
		compareText(first.friendlyName, second.friendlyName),
	[ColumnId.LogicalName]: (first: SolutionInfo, second: SolutionInfo) =>
		compareText(first.uniqueName, second.uniqueName),
	[ColumnId.Version]: (first: SolutionInfo, second: SolutionInfo) => compareVersions(first.version, second.version),
};

/** Whether a column id names a sortable column. */
function isSortableColumn(columnId: unknown): columnId is keyof typeof SORT_COMPARERS {
	return typeof columnId === "string" && Object.hasOwn(SORT_COMPARERS, columnId);
}

/** The solution grid; see the module comment. */
export function SolutionGrid({ store, environmentUrl, targetSolutions, fileStatuses }: SolutionGridProps) {
	const styles = useStyles();
	const [, render] = useReducer((revision: number) => revision + 1, 0);
	const [filterText, setFilterText] = useState("");
	const [sortState, setSortState] = useState<SortState>(UNSORTED);

	useEffect(() => store.subscribe(render), [store]);

	const settings = store.getSettings();
	const showsFriendlyNames = settings.showFriendlySolutionNames || !settings.showLogicalSolutionNames;
	const showsLogicalNames = settings.showLogicalSolutionNames;
	const readSortingIndex = (solution: SolutionInfo): number =>
		store.getSolutionConfiguration(environmentUrl, solution.uniqueName ?? "")?.sortingIndex ?? 0;
	const orderedSolutions = orderBySortingIndex(store.getSolutions(), readSortingIndex);
	const visibleSolutions = orderedSolutions.filter((solution) => matchesFilter(solution, filterText));
	const rows = visibleSolutions.map((solution) => createRow(solution, targetSolutions, fileStatuses));
	const checkedIds = new Set(
		visibleSolutions
			.filter((solution) => store.getSolutionConfiguration(environmentUrl, solution.uniqueName ?? "")?.checked)
			.map((solution) => solution.solutionId),
	);

	/** Rewrites every solution's sorting index to its position in `solutions`. */
	const writeOrder = (solutions: readonly SolutionInfo[]): void => {
		solutions.forEach((solution, index) => {
			const configuration = store.getOrCreateSolutionConfiguration(environmentUrl, solution.uniqueName ?? "");

			if (configuration.sortingIndex !== index) {
				store.updateSolutionConfiguration({ ...configuration, sortingIndex: index });
			}
		});
	};

	/** Swaps a solution with its visible neighbour above or below. */
	const moveSolution = (solutionId: string, direction: number): void => {
		const visibleIndex = visibleSolutions.findIndex((solution) => solution.solutionId === solutionId);
		const neighbour = visibleSolutions[visibleIndex + direction];

		if (visibleIndex < 0 || neighbour === undefined) {
			return;
		}

		const reordered = [...orderedSolutions];
		const position = reordered.indexOf(visibleSolutions[visibleIndex]);
		const neighbourPosition = reordered.indexOf(neighbour);
		[reordered[position], reordered[neighbourPosition]] = [reordered[neighbourPosition], reordered[position]];
		setSortState(UNSORTED);
		writeOrder(reordered);
	};

	const handleSortChange: DataGridProps["onSortChange"] = (_event, nextSortState) => {
		const { sortColumn, sortDirection } = nextSortState;

		setSortState(nextSortState);

		if (isSortableColumn(sortColumn)) {
			const compare = SORT_COMPARERS[sortColumn];
			const sign = sortDirection === "ascending" ? 1 : -1;
			writeOrder([...orderedSolutions].sort((first, second) => sign * compare(first, second)));
		}
	};

	const handleSelectionChange: DataGridProps["onSelectionChange"] = (_event, data) => {
		for (const solution of visibleSolutions) {
			setChecked(store, environmentUrl, solution, data.selectedItems.has(solution.solutionId));
		}
	};

	const changeNameSetting = (setting: NameSetting, isShown: boolean): void => {
		store.updateSettings(toggleNameColumn(setting, isShown));
	};

	const columns = createColumns({
		showsFriendlyNames,
		showsLogicalNames,
		showsTargetState: targetSolutions !== null,
		styles,
		visibleCount: visibleSolutions.length,
		indexOf: (row) => visibleSolutions.indexOf(row.solution),
		moveSolution,
	});

	return (
		<div className={styles.root}>
			<div className={styles.toolbar}>
				<Input
					aria-label="Filter solutions"
					placeholder="Filter"
					contentBefore={<SearchRegular />}
					value={filterText}
					onChange={(_event, data) => setFilterText(data.value)}
				/>
				<Switch
					label="Friendly names"
					checked={showsFriendlyNames}
					onChange={(_event, data) => changeNameSetting("showFriendlySolutionNames", data.checked)}
				/>
				<Switch
					label="Logical names"
					checked={showsLogicalNames}
					onChange={(_event, data) => changeNameSetting("showLogicalSolutionNames", data.checked)}
				/>
			</div>

			<DataGrid
				items={rows}
				columns={columns}
				getRowId={(row: SolutionRow) => row.solution.solutionId}
				sortable
				sortState={sortState}
				onSortChange={handleSortChange}
				selectionMode="multiselect"
				selectedItems={checkedIds}
				onSelectionChange={handleSelectionChange}
				aria-label="Solutions"
			>
				<DataGridHeader>
					<DataGridRow selectionCell={{ checkboxIndicator: { "aria-label": "Check all shown solutions" } }}>
						{({ renderHeaderCell }) => <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>}
					</DataGridRow>
				</DataGridHeader>
				<DataGridBody<SolutionRow>>
					{({ item, rowId }) => (
						<DataGridRow<SolutionRow>
							key={rowId}
							selectionCell={{ checkboxIndicator: { "aria-label": `Check ${displayName(item.solution)}` } }}
						>
							{({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
						</DataGridRow>
					)}
				</DataGridBody>
			</DataGrid>
		</div>
	);
}

/** What `createColumns` needs to build the grid's columns. */
interface ColumnOptions {
	showsFriendlyNames: boolean;
	showsLogicalNames: boolean;
	showsTargetState: boolean;
	styles: ReturnType<typeof useStyles>;
	visibleCount: number;
	indexOf: (row: SolutionRow) => number;
	moveSolution: (solutionId: string, direction: number) => void;
}

/** Builds the grid's columns; hidden columns are left out. */
function createColumns(options: ColumnOptions): TableColumnDefinition<SolutionRow>[] {
	const { styles } = options;
	const columns: TableColumnDefinition<SolutionRow>[] = [];

	if (options.showsFriendlyNames) {
		columns.push(
			createTableColumn<SolutionRow>({
				columnId: ColumnId.FriendlyName,
				compare: (first, second) => SORT_COMPARERS.friendlyName(first.solution, second.solution),
				renderHeaderCell: () => "Solution Name",
				renderCell: (row) => row.solution.friendlyName,
			}),
		);
	}

	if (options.showsLogicalNames) {
		columns.push(
			createTableColumn<SolutionRow>({
				columnId: ColumnId.LogicalName,
				compare: (first, second) => SORT_COMPARERS.logicalName(first.solution, second.solution),
				renderHeaderCell: () => "Logical Name",
				renderCell: (row) => row.solution.uniqueName,
			}),
		);
	}

	columns.push(
		createTableColumn<SolutionRow>({
			columnId: ColumnId.Version,
			compare: (first, second) => SORT_COMPARERS.version(first.solution, second.solution),
			renderHeaderCell: () => "Version",
			renderCell: (row) => row.solution.version,
		}),
	);

	if (options.showsTargetState) {
		columns.push(
			createTableColumn<SolutionRow>({
				columnId: ColumnId.Target,
				renderHeaderCell: () => "Target",
				renderCell: (row) => (row.targetState === null ? null : renderTargetState(row.targetState, styles)),
			}),
		);
	}

	columns.push(
		createTableColumn<SolutionRow>({
			columnId: ColumnId.Files,
			renderHeaderCell: () => "Files",
			renderCell: (row) => (row.fileStatus === undefined ? null : renderFileStatus(row.fileStatus, styles)),
		}),
		createTableColumn<SolutionRow>({
			columnId: ColumnId.Order,
			renderHeaderCell: () => "Order",
			renderCell: (row) => renderMoveButtons(row, options),
		}),
	);

	return columns;
}

/** The target version state icon, labelled for screen readers and as a tooltip. */
function renderTargetState(state: TargetVersionState, styles: ReturnType<typeof useStyles>) {
	const label = TARGET_STATE_LABELS[state];
	const icons = {
		[TargetVersionState.Equal]: <CheckmarkCircleFilled className={styles.equal} />,
		[TargetVersionState.Different]: <ArrowSyncCircleFilled className={styles.different} />,
		[TargetVersionState.Missing]: <DismissCircleRegular className={styles.missing} />,
	};

	return (
		<span role="img" aria-label={label} title={label}>
			{icons[state]}
		</span>
	);
}

/** Which export files are configured (M managed, U unmanaged) and a warning when outdated. */
function renderFileStatus(status: SolutionFileStatus, styles: ReturnType<typeof useStyles>) {
	const managed = status.managed.isConfigured ? FILE_CONFIGURED_MANAGED : FILE_NOT_CONFIGURED;
	const unmanaged = status.unmanaged.isConfigured ? FILE_CONFIGURED_UNMANAGED : FILE_NOT_CONFIGURED;
	const label =
		`Managed file ${status.managed.isConfigured ? "set" : "not set"}, ` +
		`unmanaged file ${status.unmanaged.isConfigured ? "set" : "not set"}` +
		(status.isOutdated ? ", the source version differs from the last export." : ".");

	return (
		<span role="img" aria-label={label} title={label}>
			<Text className={styles.fileState}>{`${managed}${unmanaged}`}</Text>
			{status.isOutdated ? <WarningFilled className={styles.outdated} /> : null}
		</span>
	);
}

/** The buttons that move a row up or down among the shown rows. */
function renderMoveButtons(row: SolutionRow, options: ColumnOptions) {
	const index = options.indexOf(row);
	const name = displayName(row.solution);

	// The buttons stop the click, which would otherwise toggle the row's check box.
	return (
		<>
			<Button
				appearance="subtle"
				size="small"
				icon={<ArrowUpRegular />}
				aria-label={`Move ${name} up`}
				disabled={index <= 0}
				onClick={(event) => {
					event.stopPropagation();
					options.moveSolution(row.solution.solutionId, MOVE_UP);
				}}
			/>
			<Button
				appearance="subtle"
				size="small"
				icon={<ArrowDownRegular />}
				aria-label={`Move ${name} down`}
				disabled={index >= options.visibleCount - 1}
				onClick={(event) => {
					event.stopPropagation();
					options.moveSolution(row.solution.solutionId, MOVE_DOWN);
				}}
			/>
		</>
	);
}

/** Builds a row: the solution with its target version state and file status. */
function createRow(
	solution: Readonly<SolutionInfo>,
	targetSolutions: readonly SolutionInfo[] | null,
	fileStatuses: readonly SolutionFileStatus[],
): SolutionRow {
	return {
		solution,
		targetState: targetSolutions === null ? null : findTargetState(solution, targetSolutions),
		fileStatus: fileStatuses.find((status) => status.uniqueName === solution.uniqueName),
	};
}

/** Compares the solution's version with the target's solution of the same unique name. */
function findTargetState(solution: SolutionInfo, targetSolutions: readonly SolutionInfo[]): TargetVersionState {
	const targetSolution = targetSolutions.find((target) => target.uniqueName === solution.uniqueName);

	if (targetSolution === undefined) {
		return TargetVersionState.Missing;
	}

	return targetSolution.version === solution.version ? TargetVersionState.Equal : TargetVersionState.Different;
}

/** Sorts by sorting index; solutions with equal indexes keep their order. */
function orderBySortingIndex(
	solutions: readonly Readonly<SolutionInfo>[],
	readSortingIndex: (solution: SolutionInfo) => number,
): Readonly<SolutionInfo>[] {
	return [...solutions].sort((first, second) => readSortingIndex(first) - readSortingIndex(second));
}

/** Whether the friendly or logical name contains the filter text, ignoring case. */
function matchesFilter(solution: SolutionInfo, filterText: string): boolean {
	const filter = filterText.trim().toLowerCase();

	return (
		filter.length === 0 ||
		(solution.friendlyName ?? "").toLowerCase().includes(filter) ||
		(solution.uniqueName ?? "").toLowerCase().includes(filter)
	);
}

/** Stores a solution's check state; an unchanged state writes nothing. */
function setChecked(store: SettingsStore, environmentUrl: string, solution: SolutionInfo, isChecked: boolean): void {
	const uniqueName = solution.uniqueName ?? "";

	if ((store.getSolutionConfiguration(environmentUrl, uniqueName)?.checked ?? false) === isChecked) {
		return;
	}

	const configuration = store.getOrCreateSolutionConfiguration(environmentUrl, uniqueName);
	store.updateSolutionConfiguration({ ...configuration, checked: isChecked });
}

/**
 * The settings change of a name switch: turning one name column off turns
 * the other on, as the XrmToolBox plugin's settings form does.
 */
function toggleNameColumn(setting: NameSetting, isShown: boolean): Partial<ToolSettings> {
	if (setting === "showFriendlySolutionNames") {
		return isShown
			? { showFriendlySolutionNames: true }
			: { showFriendlySolutionNames: false, showLogicalSolutionNames: true };
	}

	return isShown
		? { showLogicalSolutionNames: true }
		: { showLogicalSolutionNames: false, showFriendlySolutionNames: true };
}

/** The name a label shows: the friendly name, else the unique name. */
function displayName(solution: SolutionInfo): string {
	return solution.friendlyName ?? solution.uniqueName ?? solution.solutionId;
}

/** Compares two names alphabetically, ignoring case; a missing name sorts first. */
function compareText(first: string | null, second: string | null): number {
	return (first ?? "").localeCompare(second ?? "", undefined, { sensitivity: "base" });
}

/** Compares two dotted versions part by part, numerically; a missing part counts as 0. */
function compareVersions(first: string | null, second: string | null): number {
	const firstParts = (first ?? "").split(VERSION_SEPARATOR).map(Number);
	const secondParts = (second ?? "").split(VERSION_SEPARATOR).map(Number);
	const length = Math.max(firstParts.length, secondParts.length);

	for (let index = 0; index < length; index++) {
		const difference = (firstParts[index] || 0) - (secondParts[index] || 0);

		if (difference !== 0) {
			return difference;
		}
	}

	return 0;
}
