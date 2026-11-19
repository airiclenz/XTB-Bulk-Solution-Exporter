/**
 * A pure parser of the run log's mini-markup (the core `RunLog` contract)
 * into styled text spans, mirroring the XrmToolBox plugin's `MarkdownParser`:
 * - `# Heading` (one to six `#` followed by a space or the line end; the rest
 *   of the line is the heading, bold)
 * - `**bold**`, `*italic*` and `` `code` ``
 * - `<color=#RRGGBB>colored</color>`; any other colour form stays plain text
 * - `\x`, a backslash before a backslash, `#`, `*`, backtick or `<`, renders
 *   that character literally: it never acts as markup (`escape` writes it, as
 *   the core `LogMarkup.Escape`); any other backslash is literal itself
 *
 * Lines longer than `MAX_LOG_LINE_LENGTH` are cut and end with
 * `TRUNCATION_MARKER`; the cut never splits a colour token, end tag or escape,
 * and a colour token it leaves open is closed (the core `LogMarkup.TruncateLine`
 * rules). Headings and markers nested deeper than `MAX_NESTING_DEPTH` stay
 * plain text. The spans carry text only: a caller
 * renders them as text nodes, so markup-like or hostile text (`<script>`) is
 * shown, never interpreted.
 */

/** The longest line parsed in full, as the plugin's `MarkdownParser.MaxLogLineLength`. */
export const MAX_LOG_LINE_LENGTH = 10000;

/** Appended to a line cut at `MAX_LOG_LINE_LENGTH`. */
export const TRUNCATION_MARKER = " … [truncated]";

/** The deepest nesting of headings and markers parsed; deeper text stays literal. */
export const MAX_NESTING_DEPTH = 10;

/** One piece of a log line with its style. */
export interface LogSpan {
	text: string;
	isBold: boolean;
	isItalic: boolean;
	isCode: boolean;
	/** The colour token's colour as upper-case `#RRGGBB`, or `null` outside a colour token. */
	color: string | null;
	/** The level (1 to 6) of the innermost heading the span is in, or `null` outside a heading. */
	headingLevel: number | null;
}

/** One rendered line: its spans in order; an empty line has none. */
export type LogLine = readonly LogSpan[];

/** The style a span inherits from the markers around it. */
type SpanStyle = Omit<LogSpan, "text">;

/** A format marker: its start and end text and the style it adds. */
interface FormatMarker {
	start: string;
	end: string;
	isColorToken: boolean;
	apply: (style: SpanStyle) => SpanStyle;
}

const MAX_HEADING_LEVEL = 6;
const HEADING_CHARACTER = "#";
const HEADING_SEPARATOR = " ";
const COLOR_END_TAG = "</color>";
const LINE_SEPARATOR = /\r\n|\n/;
const TAG_START = "<";
const ESCAPE_CHARACTER = "\\";

/** Every character an `ESCAPE_CHARACTER` before it makes literal. */
const ESCAPABLE_CHARACTERS = "\\#*`<";

/** The length of an escape: the `ESCAPE_CHARACTER` plus the character it escapes. */
const ESCAPE_LENGTH = 2;

/** A colour token starting exactly at `lastIndex`. */
const COLOR_TOKEN_PATTERN = /<color=#([0-9A-Fa-f]{6})>/y;

/** What a truncation scan steps over in one go: an escape, a colour token, an end tag or one character. */
type MarkupElementKind = "escape" | "colorToken" | "endTag" | "character";

const PLAIN_STYLE: SpanStyle = {
	isBold: false,
	isItalic: false,
	isCode: false,
	color: null,
	headingLevel: null,
};

const COLOR_MARKER: FormatMarker = {
	start: "<color=",
	end: COLOR_END_TAG,
	isColorToken: true,
	apply: (style) => style,
};

/** The markers in the plugin's search order: `**` wins over `*` at the same position. */
const TEXT_MARKERS: readonly FormatMarker[] = [
	{ start: "**", end: "**", isColorToken: false, apply: (style) => ({ ...style, isBold: true }) },
	{ start: "*", end: "*", isColorToken: false, apply: (style) => ({ ...style, isItalic: true }) },
	{ start: "`", end: "`", isColorToken: false, apply: (style) => ({ ...style, isCode: true }) },
];

const ALL_MARKERS: readonly FormatMarker[] = [...TEXT_MARKERS, COLOR_MARKER];

/** Parses log markup, one or more lines, into its rendered lines. */
export function parseLogMarkup(markup: string): LogLine[] {
	return markup.split(LINE_SEPARATOR).map((line) => {
		const spans: LogSpan[] = [];
		parseText(truncateLine(line.trimEnd()), PLAIN_STYLE, 0, spans);
		return spans;
	});
}

/**
 * Escapes a text so every character of it renders literally: each backslash,
 * `#`, `*`, backtick and `<` gets a backslash before it; every other
 * character is kept. Mirrors the core `LogMarkup.Escape`.
 *
 * @returns An empty string for `null` or `undefined`.
 */
export function escape(text: string | null | undefined): string {
	return Array.from(text ?? "", (character) =>
		isEscapable(character) ? ESCAPE_CHARACTER + character : character,
	).join("");
}

/**
 * Cuts a line longer than `MAX_LOG_LINE_LENGTH` and appends
 * `TRUNCATION_MARKER`, as the core `LogMarkup.TruncateLine`. The cut moves
 * back before a colour token, end tag or escape it would split, so the kept
 * text never ends on a partial tag or on a backslash escaping the cut-off
 * character. A colour token left open is closed before the marker; the cut
 * then also moves back before a trailing literal backslash, which would
 * otherwise escape the appended end tag. An escaped token or end tag is
 * plain text, never a tag.
 */
function truncateLine(line: string): string {
	if (line.length <= MAX_LOG_LINE_LENGTH) {
		return line;
	}

	let cutIndex = MAX_LOG_LINE_LENGTH;
	let isColorTokenOpen = false;
	let lastElementIndex = -1;
	let index = 0;

	while (index < cutIndex) {
		const element = readMarkupElement(line, index);

		if (index + element.length > cutIndex) {
			cutIndex = index;
			break;
		}

		if (element.kind === "colorToken") {
			isColorTokenOpen = true;
		} else if (element.kind === "endTag") {
			isColorTokenOpen = false;
		}

		lastElementIndex = index;
		index += element.length;
	}

	if (isColorTokenOpen && isLoneEscapeCharacter(line, lastElementIndex, cutIndex)) {
		cutIndex = lastElementIndex;
	}

	const keptText = line.slice(0, cutIndex);
	const closedText = isColorTokenOpen ? keptText + COLOR_END_TAG : keptText;

	return closedText + TRUNCATION_MARKER;
}

/** The markup element starting at a position outside any escape. */
function readMarkupElement(text: string, index: number): { kind: MarkupElementKind; length: number } {
	if (isEscape(text, index)) {
		return { kind: "escape", length: ESCAPE_LENGTH };
	}

	if (text[index] === TAG_START) {
		const colorToken = matchColorToken(text, index);

		if (colorToken !== null) {
			return { kind: "colorToken", length: colorToken[0].length };
		}

		if (text.startsWith(COLOR_END_TAG, index)) {
			return { kind: "endTag", length: COLOR_END_TAG.length };
		}
	}

	return { kind: "character", length: 1 };
}

/** Whether the element from `elementIndex` to `elementEnd` is a lone backslash that escapes nothing. */
function isLoneEscapeCharacter(text: string, elementIndex: number, elementEnd: number): boolean {
	return elementIndex >= 0 && elementEnd - elementIndex === 1 && text[elementIndex] === ESCAPE_CHARACTER;
}

/** Whether the character at a position is a backslash before an escapable character; the two render as that character. */
function isEscape(text: string, index: number): boolean {
	return text[index] === ESCAPE_CHARACTER && index + 1 < text.length && isEscapable(text[index + 1]);
}

/** Whether an `ESCAPE_CHARACTER` before the character makes it literal. */
function isEscapable(character: string): boolean {
	return ESCAPABLE_CHARACTERS.includes(character);
}

/** Replaces each escape in a text that starts outside any escape with the character it escapes. */
function removeEscapes(text: string): string {
	if (!text.includes(ESCAPE_CHARACTER)) {
		return text;
	}

	let unescapedText = "";
	let index = 0;

	while (index < text.length) {
		const characterIndex = isEscape(text, index) ? index + 1 : index;
		unescapedText += text[characterIndex];
		index = characterIndex + 1;
	}

	return unescapedText;
}

/** The colour token starting exactly at a position, or `null`. */
function matchColorToken(text: string, position: number): RegExpExecArray | null {
	COLOR_TOKEN_PATTERN.lastIndex = position;
	return COLOR_TOKEN_PATTERN.exec(text);
}

/** The first `value` at or after a position outside any escape that is not inside an escape, or -1. */
function indexOfUnescaped(text: string, value: string, position: number): number {
	let index = position;

	while (index < text.length) {
		if (isEscape(text, index)) {
			index += ESCAPE_LENGTH;
			continue;
		}

		if (text.startsWith(value, index)) {
			return index;
		}

		index++;
	}

	return -1;
}

/**
 * Appends the spans of a text with its headings and markers applied. Every
 * heading or marker parses its content one level deeper; at
 * `MAX_NESTING_DEPTH` the text is appended with only its escapes resolved.
 * Positions inside an escape are never markup, and every content range
 * parsed starts and ends outside one.
 */
function parseText(text: string, style: SpanStyle, depth: number, spans: LogSpan[]): void {
	if (depth >= MAX_NESTING_DEPTH) {
		appendSpan(removeEscapes(text), style, spans);
		return;
	}

	let position = 0;

	while (position < text.length) {
		const heading = readHeading(text, position);

		if (heading !== null) {
			const headingStyle = { ...style, isBold: true, headingLevel: heading.level };
			parseText(text.slice(heading.contentStart), headingStyle, depth + 1, spans);
			return;
		}

		const next = findNextMarker(text, position);

		if (next === null) {
			appendSpan(removeEscapes(text.slice(position)), style, spans);
			return;
		}

		appendSpan(removeEscapes(text.slice(position, next.position)), style, spans);
		position = next.marker.isColorToken
			? parseColorToken(text, next.position, style, depth, spans)
			: parseTextMarker(text, next.position, next.marker, style, depth, spans);
	}
}

/**
 * A heading starting at a position: one to six `#` followed by a space or
 * the end. Its content starts after one space; it runs to the line end. The
 * position is outside any escape, so an escaped `\#` is no heading.
 */
function readHeading(text: string, position: number): { level: number; contentStart: number } | null {
	let scan = position;

	while (scan < text.length && scan - position < MAX_HEADING_LEVEL && text[scan] === HEADING_CHARACTER) {
		scan++;
	}

	const level = scan - position;
	const isTerminated = scan >= text.length || text[scan] === HEADING_SEPARATOR;

	if (level === 0 || !isTerminated) {
		return null;
	}

	return { level, contentStart: scan < text.length ? scan + HEADING_SEPARATOR.length : scan };
}

/** The nearest marker at or after a position outside any escape that is not inside an escape. */
function findNextMarker(text: string, position: number): { position: number; marker: FormatMarker } | null {
	let index = position;

	while (index < text.length) {
		if (isEscape(text, index)) {
			index += ESCAPE_LENGTH;
			continue;
		}

		const marker = findMarkerAt(text, index);

		if (marker !== null) {
			return { position: index, marker };
		}

		index++;
	}

	return null;
}

/** The colour token or text marker starting at a position, text markers in `TEXT_MARKERS` order, or `null`. */
function findMarkerAt(text: string, index: number): FormatMarker | null {
	if (matchColorToken(text, index) !== null) {
		return COLOR_MARKER;
	}

	return TEXT_MARKERS.find((marker) => text.startsWith(marker.start, index)) ?? null;
}

/**
 * Parses the colour token at a position; one without an end tag is plain
 * text. Returns the position after what was consumed.
 */
function parseColorToken(text: string, position: number, style: SpanStyle, depth: number, spans: LogSpan[]): number {
	const token = matchColorToken(text, position);

	if (token !== null) {
		const contentStart = position + token[0].length;
		const endTagIndex = indexOfUnescaped(text, COLOR_END_TAG, contentStart);

		if (endTagIndex !== -1) {
			const color = `#${token[1].toUpperCase()}`;
			parseText(text.slice(contentStart, endTagIndex), { ...style, color }, depth + 1, spans);
			return endTagIndex + COLOR_END_TAG.length;
		}
	}

	appendSpan(COLOR_MARKER.start, style, spans);
	return position + COLOR_MARKER.start.length;
}

/**
 * Parses the bold, italic or code marker at a position; one without a
 * matching end is plain text. Returns the position after what was consumed.
 */
function parseTextMarker(
	text: string,
	position: number,
	marker: FormatMarker,
	style: SpanStyle,
	depth: number,
	spans: LogSpan[],
): number {
	const contentStart = position + marker.start.length;
	const remainingText = text.slice(contentStart);
	const endIndex = findMatchingEnd(remainingText, marker.end);

	if (endIndex === -1) {
		appendSpan(marker.start, style, spans);
		return contentStart;
	}

	parseText(remainingText.slice(0, endIndex), marker.apply(style), depth + 1, spans);
	return contentStart + endIndex + marker.end.length;
}

/**
 * The index of the first end marker that is not inside an escape and not the
 * tail of a longer marker's start, or -1. A longer start marker ending right
 * after an escape would cover the escaped character, so there is none.
 */
function findMatchingEnd(text: string, endMarker: string): number {
	let isAfterEscape = false;
	let index = 0;

	while (index < text.length) {
		if (isEscape(text, index)) {
			isAfterEscape = true;
			index += ESCAPE_LENGTH;
			continue;
		}

		if (text.startsWith(endMarker, index) && (isAfterEscape || !endsLongerStartMarker(text, index, endMarker))) {
			return index;
		}

		isAfterEscape = false;
		index++;
	}

	return -1;
}

/** Whether the end marker at a position is the tail of a longer marker's start (the second `*` of `**`). */
function endsLongerStartMarker(text: string, endIndex: number, endMarker: string): boolean {
	return ALL_MARKERS.some((marker) => {
		const checkIndex = endIndex + endMarker.length - marker.start.length;
		return marker.start.length > endMarker.length && checkIndex >= 0 && text.startsWith(marker.start, checkIndex);
	});
}

/** Appends a span unless its text is empty. */
function appendSpan(text: string, style: SpanStyle, spans: LogSpan[]): void {
	if (text.length > 0) {
		spans.push({ text, ...style });
	}
}
