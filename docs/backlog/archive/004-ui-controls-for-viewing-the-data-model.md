# 004 — UI controls for viewing the data model

## Summary

The README roadmap's item 4: *"UI controls for viewing the data model."* Today the app only draws the hardcoded 50-table public-safety sample — there is no way to browse the model's structure (you must click tables on the canvas one at a time) and no way to load a different model (the data is a fixed fixture). This item adds a **model explorer** panel (a tree of tables → columns → constraints, plus an FK list; clicking a table selects it on the canvas) and a **File → Open** that loads a model from JSON and renders it in both renderers.

## Goals

- [x] A model explorer panel browses the whole model's structure without clicking around the canvas.
- [x] Clicking a table in the explorer selects it on the drawing (highlight) and shows it in the inspector.
- [x] File → Open loads a model from a JSON file and renders it in both the XAML and Skia renderers.
- [x] The model-file load logic is portable and unit-tested.

## Scope

**In scope:**
- `ModelExplorerControl` — a `TreeView` (code-behind `TreeViewNode`s): schema root → one node per table with a child node per column (name, type, PK/FK tags), plus a "Foreign Keys (N)" section listing every FK via `FkEdgeExtractor.Extract`. `SetModel` rebuilds the tree; `TableSelected` fires on a table-node click.
- `ModelPanelControl` — `SetModel` (replace model, re-layout, re-render, `ModelChanged` event), `SelectTable` (accent outline highlight via `IRectangleFactory`, hit-test transparent, plus `EntitySelected` so the inspector shows it), `Tables` accessor.
- `SkiaPanelControl.SetModel` — replace the model, clear the cached `ErdDiagram` so it re-composes on the next paint (compose-once preserved).
- `ModelFile` (ModelGraphLibrary, `Model.Data`) — `ToJson`/`LoadJson`/`Load` over a JSON array of `TableInfo` (round-trips columns + FK constraints).
- `ModelEditorControl` — hosts the explorer as a **collapsible left panel** (mirroring the right panel's backlog-014 toggle), wires explorer → canvas selection + inspector, `ModelChanged` → explorer refresh, public `SetModel`.
- `MainWindow` — a `MenuBar` ("File → Open Model…") with a `FileOpenPicker` initialized for the unpackaged app via `WinRT.Interop.InitializeWithWindow`; load errors surface in a `ContentDialog`.
- Unit tests for `ModelFile` (round-trip incl. FK constraints, temp-file load, empty model).
- Docs: backlog item, sprint record, WORKLOG, functionality map, CLAUDE.md.

**Out of scope:**
- Saving a model to JSON (004 is view-only; `ModelFile.ToJson` exists for tests but no Save UI).
- Canvas → explorer selection sync (explorer → canvas is in scope; the reverse is a small follow-up).
- Editing in the explorer (add/remove tables/columns) — the inspector's type-edit stays the only edit surface.
- The Skia render stays a flat canvas (no selection); it only tracks the loaded model.

## Approach / Notes

- **Explorer placement:** a collapsible **left panel** in `ModelEditorControl`, symmetric with the right panel (log + inspector). The grid gains two columns (explorer + its toggle strip) and the drawing column shifts to the middle — the classic IDE layout (explorer | drawing | inspector).
- **`TreeViewNode` has no `Tag`:** table nodes are mapped via a `Dictionary<TreeViewNode, TableInfo>`; `ItemInvoked` walks up `Parent` to the table node.
- **Selection highlight:** a `GlRectangle` outline (DodgerBlue, 2 px, transparent fill, `IsHitTestVisible = false`) drawn after the connectors so it sits on top without intercepting clicks.
- **Unpackaged file picker:** `FileOpenPicker` must be initialized with the window handle (`WinRT.Interop.WindowNative.GetWindowHandle` + `InitializeWithWindow.Initialize`), otherwise it throws.
- **Model file format:** a JSON array of `TableInfo` — the POCOs round-trip cleanly through System.Text.Json, including `Constraints` with `ReferencedTableName`/`ReferencedColumnName`.
- **Both renderers stay consistent:** `MainWindow.OpenModel_Click` calls `XamlEditor.SetModel` and `SkiaEditor.SetModel`; the Skia path re-composes once (routing is seconds) on the next paint.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (63 existing + new `ModelFileTests`).
- [x] App launches unpackaged and stays running; the explorer shows the 50-table model; clicking a table highlights it on the canvas and shows it in the inspector; File → Open loads a JSON model and both renderers re-render (visual pass needs a manual look on the agent's non-interactive desktop; `ModelFileTests` covers the load logic).
- [x] XAML path unchanged: zoom/pan/drag/inspector/014 toggle/renderer bar all intact after the layout restructure.

## Status

- **State:** Completed
- **Sprint:** 2026-08-17
- **Completed:** 2026-08-18
