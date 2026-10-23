import { FluentProvider, Text, webLightTheme } from "@fluentui/react-components";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

const ROOT_ELEMENT_ID = "root";

const rootElement = document.getElementById(ROOT_ELEMENT_ID);

if (rootElement === null) {
	throw new Error(`The page has no #${ROOT_ELEMENT_ID} element.`);
}

createRoot(rootElement).render(
	<StrictMode>
		<FluentProvider theme={webLightTheme}>
			<Text>Bulk Solution Exporter</Text>
		</FluentProvider>
	</StrictMode>,
);
