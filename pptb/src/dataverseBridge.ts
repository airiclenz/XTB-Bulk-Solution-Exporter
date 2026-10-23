/**
 * The tool's side of the worker's Dataverse gateway: answers each
 * `dataverse/*` reverse call the .NET worker sends (`RpcDataverseGateway`)
 * through `window.dataverseAPI`.
 *
 * Every call carries `target`, the worker's environment index: 0 is the
 * source, 1 to n the targets in the order the UI passed them to `run/start`.
 * The bridge maps it to a PPTB connection slot through the `connectionSlots`
 * it was created with. Solution files cross the connection as base64 strings.
 *
 * A rejected `dataverseAPI` call rejects the reverse call with the original
 * message text unchanged: the worker's retry logic classifies faults by that
 * text (for example "Cannot start another [Import]" or "_Upgrade already
 * exists"). Malformed parameters or an unknown target reject with a message of
 * their own.
 */

/** The reverse-call methods the worker sends, by name. */
export const DataverseMethod = {
	PublishAll: "dataverse/publishAll",
	UpdateSolutionVersion: "dataverse/updateSolutionVersion",
	ExportSolution: "dataverse/exportSolution",
	SolutionExists: "dataverse/solutionExists",
	ImportSolution: "dataverse/importSolution",
	DeleteAndPromote: "dataverse/deleteAndPromote",
	RetrieveSolutions: "dataverse/retrieveSolutions",
} as const;

/** One reverse-call handler, as `toolboxAPI.workers.connect` takes it in `requests`. */
export type ReverseCallHandler = (params: unknown) => Promise<unknown>;

/**
 * One solution as `dataverse/retrieveSolutions` answers it: the worker reads
 * it as its core `SolutionInfo` (camelCase names, version as a dotted string).
 */
export interface SolutionRecord {
	solutionId: string;
	uniqueName: string | null;
	friendlyName: string | null;
	version: string | null;
	isManaged: boolean;
	description: string | null;
}

type CallParameters = Record<string, unknown>;

const SOLUTION_ENTITY_NAME = "solution";
const SOLUTION_ENTITY_SET = "solutions";
const SOLUTION_COLUMNS = "solutionid,uniquename,friendlyname,version,ismanaged,description";
const EXPORT_FILE_PROPERTY = "ExportSolutionFile";
const ACTION_OPERATION = "action";
const PUBLISH_ALL_OPERATION = "PublishAllXml";
const EXPORT_SOLUTION_OPERATION = "ExportSolution";
const IMPORT_SOLUTION_OPERATION = "ImportSolution";
const DELETE_AND_PROMOTE_OPERATION = "DeleteAndPromote";
const ODATA_QUOTE = "'";
const ODATA_ESCAPED_QUOTE = "''";
const INVALID_PARAMETERS_MESSAGE = "The reverse call's parameters must be an object.";

/**
 * Creates the handlers of every `dataverse/*` reverse call, keyed by method
 * name, ready to pass as `requests` to `toolboxAPI.workers.connect`.
 *
 * @param connectionSlots - The PPTB connection slot (zero-based) of each
 *   worker target: index 0 the source, 1 to n the targets in `run/start` order.
 */
export function createDataverseBridge(connectionSlots: readonly number[]): Record<string, ReverseCallHandler> {
	const readSlot = (parameters: CallParameters): number => resolveConnectionSlot(parameters, connectionSlots);

	return {
		[DataverseMethod.PublishAll]: async (params) => {
			const parameters = readParameters(params);
			await executeAction(PUBLISH_ALL_OPERATION, undefined, readSlot(parameters));
			return null;
		},

		[DataverseMethod.UpdateSolutionVersion]: async (params) => {
			const parameters = readParameters(params);
			const solutionId = readString(parameters, "solutionId");
			const version = readString(parameters, "version");
			const slot = readSlot(parameters);
			await callDataverse(() => window.dataverseAPI.update(SOLUTION_ENTITY_NAME, solutionId, { version }, slot));
			return null;
		},

		[DataverseMethod.ExportSolution]: async (params) => {
			const parameters = readParameters(params);
			const response = await executeAction(
				EXPORT_SOLUTION_OPERATION,
				{
					SolutionName: readString(parameters, "uniqueName"),
					Managed: readBoolean(parameters, "isManaged"),
				},
				readSlot(parameters),
			);
			const solutionFile = response[EXPORT_FILE_PROPERTY];
			return typeof solutionFile === "string" ? solutionFile : null;
		},

		[DataverseMethod.SolutionExists]: async (params) => {
			const parameters = readParameters(params);
			const uniqueName = toODataLiteral(readString(parameters, "uniqueName"));
			const query = `${SOLUTION_ENTITY_SET}?$select=solutionid&$filter=uniquename eq ${uniqueName}&$top=1`;
			const slot = readSlot(parameters);
			const result = await callDataverse(() => window.dataverseAPI.queryData(query, slot));
			return result.value.length > 0;
		},

		[DataverseMethod.ImportSolution]: async (params) => {
			const parameters = readParameters(params);
			await executeAction(
				IMPORT_SOLUTION_OPERATION,
				{
					CustomizationFile: readString(parameters, "solutionFile"),
					PublishWorkflows: readBoolean(parameters, "shouldPublishWorkflows"),
					OverwriteUnmanagedCustomizations: readBoolean(parameters, "shouldOverwriteUnmanagedCustomizations"),
					ConvertToManaged: readBoolean(parameters, "isManaged"),
					HoldingSolution: readBoolean(parameters, "isHolding"),
					ImportJobId: crypto.randomUUID(),
				},
				readSlot(parameters),
			);
			return null;
		},

		[DataverseMethod.DeleteAndPromote]: async (params) => {
			const parameters = readParameters(params);
			await executeAction(
				DELETE_AND_PROMOTE_OPERATION,
				{ UniqueName: readString(parameters, "uniqueName") },
				readSlot(parameters),
			);
			return null;
		},

		[DataverseMethod.RetrieveSolutions]: async (params) => {
			const parameters = readParameters(params);
			const managedFilter = readBoolean(parameters, "includeManaged") ? "" : " and ismanaged eq false";
			const query = `${SOLUTION_ENTITY_SET}?$select=${SOLUTION_COLUMNS}&$filter=isvisible eq true${managedFilter}`;
			const slot = readSlot(parameters);
			const result = await callDataverse(() => window.dataverseAPI.queryData(query, slot));
			return result.value.map(toSolutionRecord);
		},
	};
}

/** Runs one unbound Dataverse action on a connection slot. */
function executeAction(
	operationName: string,
	parameters: Record<string, unknown> | undefined,
	slot: number,
): Promise<Record<string, unknown>> {
	return callDataverse(() =>
		window.dataverseAPI.execute({ operationName, operationType: ACTION_OPERATION, parameters }, slot),
	);
}

/**
 * Awaits one `dataverseAPI` call. A rejection keeps its message text: an
 * `Error` passes through untouched, anything else becomes an `Error` whose
 * message is its text, so the JSON-RPC layer can carry it to the worker.
 */
async function callDataverse<TResult>(operation: () => Promise<TResult>): Promise<TResult> {
	try {
		return await operation();
	} catch (reason: unknown) {
		throw reason instanceof Error ? reason : new Error(String(reason));
	}
}

/** Reads the reverse call's named parameters. */
function readParameters(params: unknown): CallParameters {
	if (typeof params !== "object" || params === null || Array.isArray(params)) {
		throw new Error(INVALID_PARAMETERS_MESSAGE);
	}

	return params as CallParameters;
}

/** Maps the call's `target` to its PPTB connection slot. */
function resolveConnectionSlot(parameters: CallParameters, connectionSlots: readonly number[]): number {
	const target = parameters.target;
	const slot = Number.isInteger(target) ? connectionSlots[target as number] : undefined;

	if (slot === undefined) {
		throw new Error(`The reverse call names an unknown connection target: ${String(target)}.`);
	}

	return slot;
}

/** Reads a required string parameter. */
function readString(parameters: CallParameters, name: string): string {
	const value = parameters[name];

	if (typeof value !== "string") {
		throw new Error(`The reverse call's parameter "${name}" must be a string.`);
	}

	return value;
}

/** Reads a required boolean parameter. */
function readBoolean(parameters: CallParameters, name: string): boolean {
	const value = parameters[name];

	if (typeof value !== "boolean") {
		throw new Error(`The reverse call's parameter "${name}" must be a boolean.`);
	}

	return value;
}

/**
 * Turns untrusted text into a quoted OData string literal that is safe inside
 * a query string: quotes are doubled, URL-reserved characters are escaped.
 */
function toODataLiteral(text: string): string {
	const escapedText = encodeURIComponent(text.replaceAll(ODATA_QUOTE, ODATA_ESCAPED_QUOTE));
	return `${ODATA_QUOTE}${escapedText}${ODATA_QUOTE}`;
}

/** Converts one `solutions` row of the Web API into the worker's shape. */
function toSolutionRecord(row: Record<string, unknown>): SolutionRecord {
	return {
		solutionId: String(row.solutionid),
		uniqueName: readOptionalString(row.uniquename),
		friendlyName: readOptionalString(row.friendlyname),
		version: readOptionalString(row.version),
		isManaged: row.ismanaged === true,
		description: readOptionalString(row.description),
	};
}

/** Returns the value when it is a string, otherwise `null`. */
function readOptionalString(value: unknown): string | null {
	return typeof value === "string" ? value : null;
}
