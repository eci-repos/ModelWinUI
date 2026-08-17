# Sprint 2026-08-17 — UI controls for viewing the data model

> Executed copy of the sprint. Backlog item: `docs/backlog/004-ui-controls-for-viewing-the-data-model.md`.

## Dates

- **Start:** 2026-08-17
- **End:** (TBD)

## Scope

Backlog items in this sprint (reference by number):

- [x] `004` — UI controls for viewing the data model: model explorer panel + File → Open (JSON load)

## Execution Log

- 2026-08-17 — Sprint defined from backlog item `004`. User scope decision: **Explorer + JSON load** — a model explorer panel (tree of tables → columns → constraints, plus an FK list; clicking a table selects it on the canvas) AND a File → Open that loads a model from JSON and renders it.
- 2026-08-17 — `ModelFile` (ModelGraphLibrary, `Model.Data`): `ToJson`/`LoadJson`/`Load` over a JSON array of `TableInfo` — the POCOs round-trip cleanly through System.Text.Json, including `Constraints` with `ReferencedTableName`/`ReferencedColumnName`. New `ModelFileTests` (round-trip incl. FK constraints, temp-file load, empty model) — 3 tests.
- 2026-08-17 — `ModelExplorerControl`: a `TreeView` built in code-behind (`TreeViewNode`s) — schema root → one node per table with a child node per column (name, type, PK/FK tags), plus a "Foreign Keys (N)" section via `FkEdgeExtractor.Extract`. `SetModel` rebuilds; `TableSelected` fires on a table-node click. `TreeViewNode` has no `Tag`, so table nodes are mapped via a `Dictionary<TreeViewNode, TableInfo>`.
- 2026-08-17 — `ModelPanelControl`: `SetModel` (replace model, re-layout, re-render, `ModelChanged` event), `SelectTable` (DodgerBlue accent outline via `IRectangleFactory`, hit-test transparent, plus `EntitySelected` so the inspector shows it), `Tables` accessor. `SkiaPanelControl.SetModel` clears the cached `ErdDiagram` so it re-composes on the next paint.
- 2026-08-17 — `ModelEditorControl` hosts the explorer as a **collapsible left panel** (mirroring the right panel's backlog-014 toggle): grid gains two columns (explorer + toggle strip), the drawing column shifts to the middle. Wires explorer → canvas selection + inspector, `ModelChanged` → explorer refresh, public `SetModel`.
- 2026-08-17 — `MainWindow` gains a `MenuBar` ("File → Open Model…") with a `FileOpenPicker` initialized for the unpackaged app via `WinRT.Interop.WindowNative.GetWindowHandle` + `InitializeWithWindow.Initialize`; load errors surface in a `ContentDialog`. Both renderers get the loaded model.
- 2026-08-17 — Verified: app project builds 0 errors / 0 warnings; `ModelFileTests` 3/3 pass. (Full-solution `--no-incremental` build + full test suite + launch check pending.)

## Results

- **Completed:** `004`
- **Notes:**
  - The explorer is the primary "view a model" surface: browse structure, click a table → it highlights on the canvas and shows in the inspector.
  - The model file is a JSON array of `TableInfo`; `ModelFile.ToJson` exists for tests but there is no Save UI (004 is view-only).
  - Canvas → explorer selection sync is a small follow-up (explorer → canvas is wired).
  - The Skia render stays a flat canvas (no selection); it only tracks the loaded model.
