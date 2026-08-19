# Sprint 2026-08-19 — Pointer-hover metadata readout

> Executed copy of the sprint. Definition: `docs/backlog/archive/027-hover-metadata-readout.md`.

## Dates

- **Start:** 2026-08-19
- **End:** 2026-08-19

## Scope

Backlog items in this sprint:

- [x] `027` — Pointer-hover metadata readout (part 1 of the object-oriented drawing series)

## Execution Log

- 2026-08-19 — Sprint defined from backlog item 027 (object-oriented drawing series, part 1). The drawing answers "what is this?" without a click: hovering an FK connector shows its cardinality/roles, hovering a table shows description/column count/PK/FK count/provenance — always read from the **live** model objects.
- 2026-08-19 — Portable hover-summary provider `ModelConsole.Graph.HoverSummary` (ModelGraphLibrary, pure net10.0) beside `ReadoutFormatter`: `ForTable` (schema::table header, description, `N columns`, `PK: n, FK: n`, provenance), `ForConnector` (`child.col → parent.col` + cardinality/roles from the edge's constraint), `For` dispatch. The XAML tooltip only renders these lines, so hover, inspector, and connector details share one source of text (the 022 discipline).
- 2026-08-19 — `GlContext` hover tracking: `_hoveredObject` + `HoverChanged(GlObject, Point)` event raised on every hover move (content-space position), cleared on press, pointer-exit, pointer-moved-over-empty-space, and `Reset()`. `ResolveHoverObject` resolves a hover-drawn grabber handle back to the `GlOrthoPath` it grabs. Hover is gated behind the existing pan/drag early returns, so it never interferes.
- 2026-08-19 — `ModelPanelControl` tooltip host: a hit-test-transparent overlay `Canvas` (sibling of the ScrollViewer in the row-1 grid, so it positions in viewport px) holding a `Border`/`StackPanel`. A 400 ms `DispatcherQueueTimer` delay triggers the readout; the tooltip follows the pointer, closes on drag/pan/zoom/re-render/press, and is strictly read-only. The timer field is `Microsoft.UI.Dispatching.DispatcherQueueTimer` (the `UIElement.DispatcherQueue` queue) — fully qualified because the file also imports the WinRT `Windows.System` timer.
- 2026-08-19 — Added 7 tests (`HoverSummaryTests`): table readout (full + minimal + no-columns), connector readout (with/without constraint), `For` dispatch, null → empty. Library + app build **0 errors, 0 warnings** (a clean `obj`/`bin` was required — the WinUI XAML compiler intermittently fails `DiagnosticsLogControl.xaml` with `WMC1509: No LocalAssembly`; a clean rebuild clears it); full suite **139/139 pass** (was 132).

## Results

- **Completed:** `027`
- **Deferred:** none — `028` (graph node objects) is the next item in the object-oriented drawing series.
- **Notes:** The hover readout is strictly read-only — editing stays on the inspector (backlog 029). `HoverSummary` is portable and the Skia/WASM sibling can reuse it for its own hover. Visual verification (tooltip appearance/positioning over tables and connectors, closure on drag/pan/zoom) needs a manual pass — CLI launch runs on the agent's non-interactive desktop.
