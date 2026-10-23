import { describe, expect, it } from "vitest";

import { isValidVersionFormat } from "./versionFormat";

describe("isValidVersionFormat", () => {
	it.each(["YYYY.MM.DD.+", "#.+", "#.#.#.+", "+.+", "YYYY.MM"])(
		"isValidVersionFormat / two to four known tokens (%s) / true",
		(format) => {
			expect(isValidVersionFormat(format)).toBe(true);
		},
	);

	it.each(["1", "#.#.#.#.#", "YYYY.X", "", "#", "#..+", "yyyy.mm.dd.+", " #.+", "#.+."])(
		"isValidVersionFormat / wrong part count or unknown token (%j) / false",
		(format) => {
			expect(isValidVersionFormat(format)).toBe(false);
		},
	);
});
