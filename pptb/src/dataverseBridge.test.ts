import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { createDataverseBridge, DataverseMethod, type ReverseCallHandler } from "./dataverseBridge";

const SOURCE_SLOT = 2;
const FIRST_TARGET_SLOT = 0;
const SECOND_TARGET_SLOT = 5;
const CONNECTION_SLOTS = [SOURCE_SLOT, FIRST_TARGET_SLOT, SECOND_TARGET_SLOT];
const IMPORT_ALREADY_RUNNING_TEXT =
	"Cannot start another [Import] because there is a previous [Import] running at this moment";
const SOLUTION_ID = "6f1c3d2a-0000-4000-8000-000000000001";
const SOLUTION_FILE_BASE64 = "UEsDBBQAAAAIAA==";

interface DataverseMock {
	execute: ReturnType<typeof vi.fn<DataverseAPI.API["execute"]>>;
	update: ReturnType<typeof vi.fn<DataverseAPI.API["update"]>>;
	queryData: ReturnType<typeof vi.fn<DataverseAPI.API["queryData"]>>;
}

let dataverse: DataverseMock;
let bridge: Record<string, ReverseCallHandler>;

/** Sends one reverse call to the bridge, as the worker session would. */
function call(method: string, params: Record<string, unknown>): Promise<unknown> {
	const handler = bridge[method];

	if (handler === undefined) {
		throw new Error(`No handler for ${method}`);
	}

	return handler(params);
}

beforeEach(() => {
	dataverse = {
		execute: vi.fn<DataverseAPI.API["execute"]>().mockResolvedValue({}),
		update: vi.fn<DataverseAPI.API["update"]>().mockResolvedValue(undefined),
		queryData: vi.fn<DataverseAPI.API["queryData"]>().mockResolvedValue({ value: [] }),
	};
	vi.stubGlobal("window", { dataverseAPI: dataverse });
	bridge = createDataverseBridge(CONNECTION_SLOTS);
});

afterEach(() => {
	vi.unstubAllGlobals();
	vi.clearAllMocks();
});

describe("createDataverseBridge", () => {
	it("createDataverseBridge / any bridge / answers every worker reverse call", () => {
		expect(Object.keys(bridge).sort()).toEqual(Object.values(DataverseMethod).sort());
	});

	describe("publishAll", () => {
		it("publishAll / target 1 / runs PublishAllXml on the first target's slot", async () => {
			await expect(call(DataverseMethod.PublishAll, { target: 1 })).resolves.toBeNull();

			expect(dataverse.execute).toHaveBeenCalledWith(
				{ operationName: "PublishAllXml", operationType: "action", parameters: undefined },
				FIRST_TARGET_SLOT,
			);
		});
	});

	describe("updateSolutionVersion", () => {
		it("updateSolutionVersion / source / updates the solution record's version on the source slot", async () => {
			await call(DataverseMethod.UpdateSolutionVersion, { target: 0, solutionId: SOLUTION_ID, version: "1.2.3.4" });

			expect(dataverse.update).toHaveBeenCalledWith("solution", SOLUTION_ID, { version: "1.2.3.4" }, SOURCE_SLOT);
		});
	});

	describe("exportSolution", () => {
		it("exportSolution / managed / runs ExportSolution and answers the base64 file", async () => {
			dataverse.execute.mockResolvedValue({ ExportSolutionFile: SOLUTION_FILE_BASE64 });

			const answer = await call(DataverseMethod.ExportSolution, {
				target: 0,
				uniqueName: "Contoso",
				isManaged: true,
			});

			expect(answer).toBe(SOLUTION_FILE_BASE64);
			expect(dataverse.execute).toHaveBeenCalledWith(
				{
					operationName: "ExportSolution",
					operationType: "action",
					parameters: { SolutionName: "Contoso", Managed: true },
				},
				SOURCE_SLOT,
			);
		});

		it("exportSolution / response without a file / answers null", async () => {
			await expect(
				call(DataverseMethod.ExportSolution, { target: 0, uniqueName: "Contoso", isManaged: false }),
			).resolves.toBeNull();
		});
	});

	describe("solutionExists", () => {
		it("solutionExists / one matching row / queries solutions on the target's slot and answers true", async () => {
			dataverse.queryData.mockResolvedValue({ value: [{ solutionid: SOLUTION_ID }] });

			await expect(call(DataverseMethod.SolutionExists, { target: 2, uniqueName: "Contoso" })).resolves.toBe(true);

			expect(dataverse.queryData).toHaveBeenCalledWith(
				"solutions?$select=solutionid&$filter=uniquename eq 'Contoso'&$top=1",
				SECOND_TARGET_SLOT,
			);
		});

		it("solutionExists / name with quote and ampersand / escapes the OData literal", async () => {
			await expect(call(DataverseMethod.SolutionExists, { target: 1, uniqueName: "a'b&c" })).resolves.toBe(false);

			expect(dataverse.queryData).toHaveBeenCalledWith(
				"solutions?$select=solutionid&$filter=uniquename eq 'a''b%26c'&$top=1",
				FIRST_TARGET_SLOT,
			);
		});
	});

	describe("importSolution", () => {
		it("importSolution / holding import / runs ImportSolution with HoldingSolution true", async () => {
			await call(DataverseMethod.ImportSolution, {
				target: 1,
				solutionFile: SOLUTION_FILE_BASE64,
				isManaged: true,
				isHolding: true,
				shouldPublishWorkflows: true,
				shouldOverwriteUnmanagedCustomizations: false,
			});

			expect(dataverse.execute).toHaveBeenCalledWith(
				{
					operationName: "ImportSolution",
					operationType: "action",
					parameters: {
						CustomizationFile: SOLUTION_FILE_BASE64,
						PublishWorkflows: true,
						OverwriteUnmanagedCustomizations: false,
						ConvertToManaged: true,
						HoldingSolution: true,
						ImportJobId: expect.any(String),
					},
				},
				FIRST_TARGET_SLOT,
			);
		});

		it("importSolution / plain import / sends HoldingSolution false", async () => {
			await call(DataverseMethod.ImportSolution, {
				target: 2,
				solutionFile: SOLUTION_FILE_BASE64,
				isManaged: false,
				isHolding: false,
				shouldPublishWorkflows: false,
				shouldOverwriteUnmanagedCustomizations: true,
			});

			const [request, slot] = dataverse.execute.mock.calls[0] ?? [];
			expect(request?.parameters?.HoldingSolution).toBe(false);
			expect(slot).toBe(SECOND_TARGET_SLOT);
		});

		it("importSolution / execute rejects / surfaces the original message text unchanged", async () => {
			dataverse.execute.mockRejectedValue(new Error(IMPORT_ALREADY_RUNNING_TEXT));

			await expect(
				call(DataverseMethod.ImportSolution, {
					target: 1,
					solutionFile: SOLUTION_FILE_BASE64,
					isManaged: true,
					isHolding: false,
					shouldPublishWorkflows: true,
					shouldOverwriteUnmanagedCustomizations: false,
				}),
			).rejects.toThrow(new Error(IMPORT_ALREADY_RUNNING_TEXT));
		});
	});

	describe("deleteAndPromote", () => {
		it("deleteAndPromote / target 2 / runs DeleteAndPromote on the second target's slot", async () => {
			await call(DataverseMethod.DeleteAndPromote, { target: 2, uniqueName: "Contoso" });

			expect(dataverse.execute).toHaveBeenCalledWith(
				{ operationName: "DeleteAndPromote", operationType: "action", parameters: { UniqueName: "Contoso" } },
				SECOND_TARGET_SLOT,
			);
		});

		it("deleteAndPromote / execute rejects with a non-Error / rejects with an Error of the same text", async () => {
			dataverse.execute.mockRejectedValue("_Upgrade already exists");

			await expect(call(DataverseMethod.DeleteAndPromote, { target: 1, uniqueName: "Contoso" })).rejects.toThrow(
				new Error("_Upgrade already exists"),
			);
		});
	});

	describe("retrieveSolutions", () => {
		it("retrieveSolutions / unmanaged only / filters visible unmanaged solutions and maps the rows", async () => {
			dataverse.queryData.mockResolvedValue({
				value: [
					{
						solutionid: SOLUTION_ID,
						uniquename: "Contoso",
						friendlyname: "Contoso Core",
						version: "1.0.0.7",
						ismanaged: false,
						description: null,
					},
				],
			});

			const answer = await call(DataverseMethod.RetrieveSolutions, { target: 0, includeManaged: false });

			expect(dataverse.queryData).toHaveBeenCalledWith(
				"solutions?$select=solutionid,uniquename,friendlyname,version,ismanaged,description" +
					"&$filter=isvisible eq true and ismanaged eq false",
				SOURCE_SLOT,
			);
			expect(answer).toEqual([
				{
					solutionId: SOLUTION_ID,
					uniqueName: "Contoso",
					friendlyName: "Contoso Core",
					version: "1.0.0.7",
					isManaged: false,
					description: null,
				},
			]);
		});

		it("retrieveSolutions / managed included / drops the managed filter", async () => {
			await call(DataverseMethod.RetrieveSolutions, { target: 0, includeManaged: true });

			expect(dataverse.queryData).toHaveBeenCalledWith(
				"solutions?$select=solutionid,uniquename,friendlyname,version,ismanaged,description" +
					"&$filter=isvisible eq true",
				SOURCE_SLOT,
			);
		});
	});

	describe("parameter checks", () => {
		it("any method / unknown target / rejects without calling Dataverse", async () => {
			await expect(call(DataverseMethod.PublishAll, { target: 3 })).rejects.toThrow("unknown connection target");

			expect(dataverse.execute).not.toHaveBeenCalled();
		});

		it("any method / missing parameter / rejects naming the parameter", async () => {
			await expect(call(DataverseMethod.DeleteAndPromote, { target: 0 })).rejects.toThrow('"uniqueName"');
		});
	});
});
