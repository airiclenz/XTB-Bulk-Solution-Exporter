/**
 * The version format rules of the core `Version.ValidateFormat`: a format is
 * two to four dot-separated parts, each exactly one of the `VersionToken`s.
 * The worker refuses a run whose format breaks them, so the UI checks the
 * same rules before it starts one.
 */

/** The tokens a version format is built from, the core `VersionTokens`. */
export const VersionToken = {
	NumberIncreased: "+",
	NumberUnchanged: "#",
	Year: "YYYY",
	Month: "MM",
	Day: "DD",
} as const;

/** The fewest parts a version format may have. */
export const MIN_FORMAT_PARTS = 2;

/** The most parts a version format may have. */
export const MAX_FORMAT_PARTS = 4;

const FORMAT_SEPARATOR = ".";
const ALLOWED_TOKENS: readonly string[] = Object.values(VersionToken);

/** Whether a version format follows the core's rules; see the module comment. */
export function isValidVersionFormat(format: string): boolean {
	const parts = format.split(FORMAT_SEPARATOR);

	return (
		parts.length >= MIN_FORMAT_PARTS &&
		parts.length <= MAX_FORMAT_PARTS &&
		parts.every((part) => ALLOWED_TOKENS.includes(part))
	);
}
