---
title: PPTB retrieveSolutions ignores later result pages
status: open
type: bug
priority: 2
labels: [pptb, pptb-port-followup]
created: 2026-10-23T16:25:48Z
---

The PPTB `dataverseBridge` `retrieveSolutions` handler (`pptb/src/dataverseBridge.ts:141`) reads only the first `queryData` page (the Web API default of up to 5000 rows) and does not follow `@odata.nextLink`. An environment with more visible solutions than one page silently loses the rest from the solution grid.

## Acceptance

- `retrieveSolutions` pages through every visible solution with keyset paging on `uniquename` (`$orderby=uniquename`, `$top=5000`, the next page filtered on `uniquename gt '<last>'`) until a page comes back short, and returns every row; a test covers a two-page answer. Keyset paging replaces following `@odata.nextLink` (decision of 2026-11-18, robustness and log escaping plan).
