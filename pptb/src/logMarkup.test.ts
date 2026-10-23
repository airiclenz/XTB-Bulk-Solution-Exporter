import { describe, expect, it } from "vitest";

import { MAX_LOG_LINE_LENGTH, MAX_NESTING_DEPTH, parseLogMarkup, TRUNCATION_MARKER, type LogSpan } from "./logMarkup";

const PLAIN: Omit<LogSpan, "text"> = {
	isBold: false,
	isItalic: false,
	isCode: false,
	color: null,
	headingLevel: null,
};

/** The spans of a single-line markup. */
function parseLine(markup: string): readonly LogSpan[] {
	const lines = parseLogMarkup(markup);
	expect(lines).toHaveLength(1);
	return lines[0];
}

/** The text a line renders, its spans joined. */
function readText(spans: readonly LogSpan[]): string {
	return spans.map((span) => span.text).join("");
}

describe("parseLogMarkup", () => {
	describe("headings", () => {
		it("parseLogMarkup / normal heading / renders bold heading text", () => {
			const spans = parseLine("##### Done.");

			expect(spans).toEqual([{ ...PLAIN, text: "Done.", isBold: true, headingLevel: 5 }]);
		});

		it.each(["#NoSpace", "####### seven", "a # b"])("parseLogMarkup / %s / is no heading", (markup) => {
			const spans = parseLine(markup);

			expect(spans).toEqual([{ ...PLAIN, text: markup }]);
		});

		it.each(["# ", "# **x**"])(
			"parseLogMarkup / %s repeated just under the length limit / renders without crash",
			(repeatedText) => {
				const repeatCount = Math.floor((MAX_LOG_LINE_LENGTH - 1) / repeatedText.length);

				const spans = parseLine(repeatedText.repeat(repeatCount));

				expect(readText(spans).length).toBeGreaterThan(0);
			},
		);
	});

	describe("nesting cap", () => {
		it("parseLogMarkup / headings nested past the cap / keeps the rest as literal text", () => {
			const spans = parseLine(`${"# ".repeat(MAX_NESTING_DEPTH + 2)}x`);

			expect(spans).toEqual([{ ...PLAIN, text: "# # x", isBold: true, headingLevel: 1 }]);
		});

		it("parseLogMarkup / colour inside a heading / keeps both styles", () => {
			const spans = parseLine("##### <color=#227700>Done.</color>");

			expect(spans).toEqual([{ ...PLAIN, text: "Done.", isBold: true, headingLevel: 5, color: "#227700" }]);
		});
	});

	describe("formats", () => {
		it("parseLogMarkup / bold, italic and code / styles each part", () => {
			const spans = parseLine("**b** *i* `c`");

			expect(spans).toEqual([
				{ ...PLAIN, text: "b", isBold: true },
				{ ...PLAIN, text: " " },
				{ ...PLAIN, text: "i", isItalic: true },
				{ ...PLAIN, text: " " },
				{ ...PLAIN, text: "c", isCode: true },
			]);
		});

		it("parseLogMarkup / marker without end / keeps the marker as text", () => {
			const spans = parseLine("2 * 3");

			expect(readText(spans)).toBe("2 * 3");
		});

		it("parseLogMarkup / several lines / renders each line apart", () => {
			const lines = parseLogMarkup("first\r\n**second**\nthird   ");

			expect(lines.map(readText)).toEqual(["first", "second", "third"]);
		});
	});

	describe("colour", () => {
		it("parseLogMarkup / colour token / colours its content", () => {
			const spans = parseLine("<color=#770000>Failed</color> after");

			expect(spans).toEqual([
				{ ...PLAIN, text: "Failed", color: "#770000" },
				{ ...PLAIN, text: " after" },
			]);
		});

		it("parseLogMarkup / lower-case hex colour / reports it upper-case", () => {
			const spans = parseLine("<color=#ccccce>x</color>");

			expect(spans[0].color).toBe("#CCCCCE");
		});

		it.each(["<color=red>x</color>", "<color=#12345G>x</color>", "<color=#1234567>x</color>", "<color=#123>x</color>"])(
			"parseLogMarkup / %s / is plain text",
			(markup) => {
				const spans = parseLine(markup);

				expect(spans.every((span) => span.color === null) && readText(spans) === markup).toBe(true);
			},
		);

		it("parseLogMarkup / colour token without end tag / is plain text", () => {
			const spans = parseLine("<color=#770000>open");

			expect(spans.every((span) => span.color === null) && readText(spans) === "<color=#770000>open").toBe(true);
		});
	});

	describe("hostile text", () => {
		it("parseLogMarkup / script element / is one plain text span", () => {
			const markup = `<script>alert("x")</script><img src=x onerror="alert(1)">`;

			const spans = parseLine(markup);

			expect(spans).toEqual([{ ...PLAIN, text: markup }]);
		});
	});

	describe("truncation", () => {
		it("parseLogMarkup / huge line / ends with the truncation marker", () => {
			const spans = parseLine("x".repeat(MAX_LOG_LINE_LENGTH * 200));

			expect(readText(spans).endsWith(TRUNCATION_MARKER)).toBe(true);
		});

		it("parseLogMarkup / over-limit coloured line / hides the colour token", () => {
			const spans = parseLine(`<color=#770000>${"x".repeat(MAX_LOG_LINE_LENGTH * 2)}</color>`);

			expect(readText(spans)).not.toContain("<color=");
		});

		it("parseLogMarkup / line at the limit / is kept in full", () => {
			const line = "x".repeat(MAX_LOG_LINE_LENGTH);

			const spans = parseLine(line);

			expect(readText(spans)).toBe(line);
		});
	});
});
