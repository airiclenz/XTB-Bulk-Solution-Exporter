/**
 * The run-option switches above the solution grid: what a run does at the
 * source, on export, in git and at the targets, plus the version format and
 * the commit message. Every change goes straight to `onChange`; the caller
 * stores it. The version format and the commit message show the XrmToolBox
 * plugin's validation (`SetExportButtonState`) while their switch is on.
 */

import { Field, Input, makeStyles, Switch, Text, tokens } from "@fluentui/react-components";

import type { ToolSettings } from "../types";
import { isValidVersionFormat, MAX_FORMAT_PARTS, MIN_FORMAT_PARTS, VersionToken } from "../versionFormat";

/** The shortest commit message a run accepts, as in the XrmToolBox plugin. */
export const MIN_COMMIT_MESSAGE_LENGTH = 5;

/** The names of the boolean settings, the ones a switch shows. */
type SwitchSetting = {
	[Name in keyof ToolSettings]: ToolSettings[Name] extends boolean ? Name : never;
}[keyof ToolSettings];

/** The properties of `RunOptions`. */
export interface RunOptionsProps {
	settings: Readonly<ToolSettings>;
	/** Receives each change of a setting. */
	onChange: (changes: Partial<ToolSettings>) => void;
	/** Whether at least one target environment is connected; imports need one. */
	hasTargets: boolean;
}

const VERSION_FORMAT_HINT =
	`${MIN_FORMAT_PARTS} to ${MAX_FORMAT_PARTS} parts joined by dots, each one of ` +
	Object.values(VersionToken).join(" ");
const COMMIT_MESSAGE_HINT = `At least ${MIN_COMMIT_MESSAGE_LENGTH} characters.`;

const useStyles = makeStyles({
	root: {
		display: "flex",
		flexWrap: "wrap",
		columnGap: tokens.spacingHorizontalXXL,
		rowGap: tokens.spacingVerticalM,
	},
	group: {
		display: "flex",
		flexDirection: "column",
		minWidth: "220px",
	},
	heading: {
		fontWeight: tokens.fontWeightSemibold,
	},
});

/** The run-option switches; see the module comment. */
export function RunOptions({ settings, onChange, hasTargets }: RunOptionsProps) {
	const styles = useStyles();
	const isImporting = settings.importManaged || settings.importUnmanaged;
	const isVersionFormatInvalid = settings.updateVersion && !isValidVersionFormat(settings.versionFormat);
	const isCommitMessageTooShort = settings.gitCommit && settings.commitMessage.length < MIN_COMMIT_MESSAGE_LENGTH;

	const renderSwitch = (setting: SwitchSetting, label: string, isDisabled = false) => (
		<Switch
			label={label}
			checked={settings[setting]}
			disabled={isDisabled}
			onChange={(_event, data) => onChange({ [setting]: data.checked })}
		/>
	);

	return (
		<div className={styles.root}>
			<div className={styles.group} role="group" aria-label="Source">
				<Text className={styles.heading}>Source</Text>
				{renderSwitch("publishAllPreExport", "Publish all before export")}
				{renderSwitch("updateVersion", "Update version")}
				<Field
					label="Version format"
					validationState={isVersionFormatInvalid ? "error" : "none"}
					validationMessage={isVersionFormatInvalid ? VERSION_FORMAT_HINT : undefined}
				>
					<Input
						value={settings.versionFormat}
						onChange={(_event, data) => onChange({ versionFormat: data.value })}
					/>
				</Field>
			</div>

			<div className={styles.group} role="group" aria-label="Export">
				<Text className={styles.heading}>Export</Text>
				{renderSwitch("exportManaged", "Export managed")}
				{renderSwitch("exportUnmanaged", "Export unmanaged")}
			</div>

			<div className={styles.group} role="group" aria-label="Git">
				<Text className={styles.heading}>Git</Text>
				{renderSwitch("gitCommit", "Commit")}
				{renderSwitch("pushCommit", "Push", !settings.gitCommit)}
				<Field
					label="Commit message"
					validationState={isCommitMessageTooShort ? "error" : "none"}
					validationMessage={isCommitMessageTooShort ? COMMIT_MESSAGE_HINT : undefined}
				>
					<Input
						value={settings.commitMessage}
						onChange={(_event, data) => onChange({ commitMessage: data.value })}
					/>
				</Field>
			</div>

			<div className={styles.group} role="group" aria-label="Import">
				<Text className={styles.heading}>Import</Text>
				{renderSwitch("importManaged", "Import managed", !hasTargets)}
				{renderSwitch("importUnmanaged", "Import unmanaged", !hasTargets)}
				{renderSwitch("upgrade", "Upgrade", !isImporting)}
				{renderSwitch("enableAutomation", "Enable automation", !isImporting)}
				{renderSwitch("overwriteCustomizations", "Overwrite customizations", !isImporting)}
				{renderSwitch("publishAllPostImport", "Publish all after import", !isImporting)}
			</div>
		</div>
	);
}
