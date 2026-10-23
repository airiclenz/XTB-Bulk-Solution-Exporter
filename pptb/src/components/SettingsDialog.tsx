/**
 * The settings dialog behind a "Settings" button, as the XrmToolBox plugin's
 * `SettingsForm`: saving a versions JSON file beside the exports, continuing
 * after a failed solution, turning the export switches off after a
 * successful run, and the retry count and retry delay. Every change goes
 * straight to `onChange`; the caller stores it.
 *
 * The two numbers accept digits only, at most `MAX_NUMBER_DIGITS` of them;
 * an empty box is 0, as in the plugin.
 */

import {
	Button,
	Dialog,
	DialogActions,
	DialogBody,
	DialogContent,
	DialogSurface,
	DialogTitle,
	DialogTrigger,
	Field,
	Input,
	makeStyles,
	Switch,
	tokens,
} from "@fluentui/react-components";

import type { ToolSettings } from "../types";

/** The most digits the retry count and the retry delay take, as in the plugin. */
const MAX_NUMBER_DIGITS = 3;

/** The properties of `SettingsDialog`. */
export interface SettingsDialogProps {
	settings: Readonly<ToolSettings>;
	/** Receives each change of a setting. */
	onChange: (changes: Partial<ToolSettings>) => void;
}

const NON_DIGITS = /[^0-9]/g;
const DECIMAL_RADIX = 10;

const useStyles = makeStyles({
	content: {
		display: "flex",
		flexDirection: "column",
		rowGap: tokens.spacingVerticalM,
	},
});

/** The settings dialog; see the module comment. */
export function SettingsDialog({ settings, onChange }: SettingsDialogProps) {
	const styles = useStyles();

	return (
		<Dialog>
			<DialogTrigger disableButtonEnhancement>
				<Button>Settings</Button>
			</DialogTrigger>
			<DialogSurface>
				<DialogBody>
					<DialogTitle>Settings</DialogTitle>
					<DialogContent className={styles.content}>
						<Switch
							label="Save a versions JSON file beside the exports"
							checked={settings.saveVersionJson}
							onChange={(_event, data) => onChange({ saveVersionJson: data.checked })}
						/>
						<Switch
							label="Continue with the next solution after an error"
							checked={settings.continueOnError}
							onChange={(_event, data) => onChange({ continueOnError: data.checked })}
						/>
						<Switch
							label="Turn off the version and export switches after a successful run"
							checked={settings.autoDisableExportButtons}
							onChange={(_event, data) => onChange({ autoDisableExportButtons: data.checked })}
						/>
						<Field label="Retries of a failed step">
							<Input
								inputMode="numeric"
								value={String(settings.retryCount)}
								onChange={(_event, data) => onChange({ retryCount: readWholeNumber(data.value) })}
							/>
						</Field>
						<Field label="Delay between retries (seconds)">
							<Input
								inputMode="numeric"
								value={String(settings.retryDelayInSeconds)}
								onChange={(_event, data) => onChange({ retryDelayInSeconds: readWholeNumber(data.value) })}
							/>
						</Field>
					</DialogContent>
					<DialogActions>
						<DialogTrigger disableButtonEnhancement>
							<Button appearance="primary">OK</Button>
						</DialogTrigger>
					</DialogActions>
				</DialogBody>
			</DialogSurface>
		</Dialog>
	);
}

/** The whole number a box shows: its digits, the first `MAX_NUMBER_DIGITS` of them; none is 0. */
function readWholeNumber(text: string): number {
	const digits = text.replace(NON_DIGITS, "").slice(0, MAX_NUMBER_DIGITS);
	return digits.length === 0 ? 0 : Number.parseInt(digits, DECIMAL_RADIX);
}
