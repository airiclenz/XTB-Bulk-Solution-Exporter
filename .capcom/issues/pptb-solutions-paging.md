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

- `retrieveSolutions` follows `@odata.nextLink` until the last page and returns every row; a test covers a two-page answer.
