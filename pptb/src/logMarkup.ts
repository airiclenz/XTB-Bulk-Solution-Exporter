/**
 * A pure parser of the run log's mini-markup (the core `RunLog` contract)
 * into styled text spans, mirroring the XrmToolBox plugin's `MarkdownParser`:
 * - `# Heading` (one to six `#` followed by a space or the line end; the rest
 *   of the line is the heading, bold)
 * - `**bold**`, `*italic*` and `` `code` ``
 * - `<color=#RRGGBB>colored</color>`; any other colour form stays plain text
 *
 * Lines longer than `MAX_LOG_LINE_LENGTH` are cut and end with
 * `TRUNCATION_MARKER`; headings and markers nested deeper than
 * `MAX_NESTING_DEPTH` stay plain text. The spans carry text only: a caller
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

/** A colour token; `lastIndex` sets where a search starts. */
const COLOR_TOKEN_PATTERN = /<color=#([0-9A-Fa-f]{6})>/g;

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

/** Cuts an over-long line and closes a colour token the cut left open. */
function truncateLine(line: string): string {
	if (line.length <= MAX_LOG_LINE_LENGTH) {
		return line;
	}

	const keptText = line.slice(0, MAX_LOG_LINE_LENGTH);
	const closedText = hasUnclosedColorToken(keptText) ? keptText + COLOR_END_TAG : keptText;

	return closedText + TRUNCATION_MARKER;
}

/** Whether a colour token follows the last end tag; tokens pair with the next end tag. */
function hasUnclosedColorToken(text: string): boolean {
	const lastEndTagIndex = text.lastIndexOf(COLOR_END_TAG);
	const searchStart = lastEndTagIndex < 0 ? 0 : lastEndTagIndex + COLOR_END_TAG.length;

	return findColorToken(text, searchStart) !== null;
}

/** The first colour token at or after a position, or `null`. */
function findColorToken(text: string, position: number): RegExpExecArray | null {
	COLOR_TOKEN_PATTERN.lastIndex = position;
	return COLOR_TOKEN_PATTERN.exec(text);
}

/**
 * Appends the spans of a text with its headings and markers applied. Every
 * heading or marker parses its content one level deeper; at
 * `MAX_NESTING_DEPTH` the text is appended literally.
 */
function parseText(text: string, style: SpanStyle, depth: number, spans: LogSpan[]): void {
	if (depth >= MAX_NESTING_DEPTH) {
		appendSpan(text, style, spans);
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
			appendSpan(text.slice(position), style, spans);
			return;
		}

		appendSpan(text.slice(position, next.position), style, spans);
		position = next.marker.isColorToken
			? parseColorToken(text, next.position, style, depth, spans)
			: parseTextMarker(text, next.position, next.marker, style, depth, spans);
	}
}

/**
 * A heading starting at a position: one to six `#` followed by a space or
 * the end. Its content starts after one space; it runs to the line end.
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

/** The nearest marker at or after a position; a colour token is looked for first. */
function findNextMarker(text: string, position: number): { position: number; marker: FormatMarker } | null {
	let nearest: { position: number; marker: FormatMarker } | null = null;
	const colorToken = findColorToken(text, position);

	if (colorToken !== null) {
		nearest = { position: colorToken.index, marker: COLOR_MARKER };
	}

	for (const marker of TEXT_MARKERS) {
		const markerPosition = text.indexOf(marker.start, position);

		if (markerPosition !== -1 && (nearest === null || markerPosition < nearest.position)) {
			nearest = { position: markerPosition, marker };
		}
	}

	return nearest;
}

/**
 * Parses the colour token at a position; one without an end tag is plain
 * text. Returns the position after what was consumed.
 */
function parseColorToken(text: string, position: number, style: SpanStyle, depth: number, spans: LogSpan[]): number {
	const token = findColorToken(text, position);

	if (token !== null && token.index === position) {
		const contentStart = position + token[0].length;
		const endTagIndex = text.indexOf(COLOR_END_TAG, contentStart);

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

/** The index of the end marker that is not the tail of a longer marker's start, or -1. */
function findMatchingEnd(text: string, endMarker: string): number {
	let position = 0;

	while (position < text.length) {
		const endIndex = text.indexOf(endMarker, position);

		if (endIndex === -1) {
			return -1;
		}

		const isPartOfLongerMarker = ALL_MARKERS.some((marker) => {
			const checkIndex = endIndex + endMarker.length - marker.start.length;
			return marker.start.length > endMarker.length && checkIndex >= 0 && text.startsWith(marker.start, checkIndex);
		});

		if (!isPartOfLongerMarker) {
			return endIndex;
		}

		position = endIndex + 1;
	}

	return -1;
}

/** Appends a span unless its text is empty. */
function appendSpan(text: string, style: SpanStyle, spans: LogSpan[]): void {
	if (text.length > 0) {
		spans.push({ text, ...style });
	}
}
