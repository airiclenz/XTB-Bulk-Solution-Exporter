import react from "@vitejs/plugin-react";
import type { Plugin } from "vite";
import { defineConfig } from "vitest/config";

const MODULE_TYPE_ATTRIBUTE = /\s*type="module"/g;
const CROSSORIGIN_ATTRIBUTE = /\s*crossorigin/g;
const SPACE_BEFORE_TAG_END = /\s+>/g;
const SCRIPT_WITH_SOURCE = /(<script[^>]*src="[^"]*"[^>]*><\/script>)/g;
const BODY_END_TAG = "</body>";

/**
 * Rewrites the built index.html for Power Platform ToolBox, which loads the
 * tool from file:// into an iframe: drops `type="module"` and `crossorigin`
 * (the bundle is an IIFE) and moves the scripts to the end of the body, so
 * the DOM exists when the IIFE runs.
 */
function fixHtmlForPptb(): Plugin {
	return {
		name: "fix-html-for-pptb",
		enforce: "post",
		transformIndexHtml(html) {
			const scripts: string[] = [];
			const withoutScripts = html
				.replace(MODULE_TYPE_ATTRIBUTE, "")
				.replace(CROSSORIGIN_ATTRIBUTE, "")
				.replace(SPACE_BEFORE_TAG_END, ">")
				.replace(SCRIPT_WITH_SOURCE, (script) => {
					scripts.push(script);
					return "";
				});

			if (scripts.length === 0) {
				return withoutScripts;
			}

			return withoutScripts.replace(BODY_END_TAG, `${scripts.join("\n")}\n${BODY_END_TAG}`);
		},
	};
}

export default defineConfig({
	plugins: [react(), fixHtmlForPptb()],
	base: "./",
	build: {
		outDir: "dist",
		assetsDir: "assets",
		rollupOptions: {
			output: {
				// One IIFE file: ES modules fail to load from file:// inside the PPTB iframe.
				format: "iife",
				inlineDynamicImports: true,
			},
		},
	},
	test: {
		environment: "node",
		include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
	},
});
