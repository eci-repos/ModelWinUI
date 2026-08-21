# Sprint 2026-08-21 — Entity details window

> Executed copy of the sprint. Definition: `docs/backlog/archive/042-entity-details-window.md`.

## Dates

- **Start:** 2026-08-21
- **End:** 2026-08-21

## Scope

Backlog items in this sprint:

- [x] `042` — Entity details window: modeless pop-up with view/edit modes

Plus two UX regressions the same request bundled in: the side-panel header banners now use the zoom-toolbar gray (the 035 pastels retire), and the Model Explorer tree stopped showing table names.

## Execution Log

- 2026-08-21 — **Chrome strip unified:** `ControlTheme.xaml`'s `ExplorerHeaderBackgroundBrush`/`InspectorHeaderBackgroundBrush`/`DiagnosticsHeaderBackgroundBrush` all retint to the center zoom-toolbar gray `#F3F3F7`, so every banner in the editor reads as one strip (the lavender/peach/cream 035 pastels retire).
- 2026-08-21 — **Explorer node regression fixed:** 038's `ApplyVisibilityToTree` had replaced each table node's string `Content` with a `TextBlock` (a UIElement) — WinUI 3's TreeView fails to render a UIElement node and falls back to the node type's name ("Microsoft.UI.Xaml.Controls.TreeViewNode"). Nodes whose containers were not yet realized simply showed normal. Fix: keep `Content` a plain string; gray hidden tables via the realized `TreeViewItem` container's `Foreground` (`ContainerFromNode`).
- 2026-08-21 — **View/Edit mode (`EntityInspectorControl`):** the control gained `_editMode`/`IsEditMode` (read-only default) and its header an **Edit/Done `ModeButton`** — the 029 edit surface builds only in edit mode (`BuildEditTable`/`BuildEditConnector`); the read-only readout otherwise (`BuildReadOnlyTable` — description/tags, column list with PK/FK tags, FK list from `column.Constraints`, provenance + metadata; `BuildReadOnlyConnector` — dependency line + cardinality/roles). `ShowModel` hides the toggle.
- 2026-08-21 — **Double-click detection:** `GlContext` raises `ShapeDoubleClicked` on a second quick click (≤500 ms) of the same shape (instead of `ShapeClicked`); `ModelPanelControl` raises `EntityDoubleTapped` with the node's live model object (a table or FK; collapsed group boxes skip — a single click already expands them).
- 2026-08-21 — **`ModelEditorControl` drops the inspector:** the right panel holds only the diagnostics log. The editor exposes `Tables`/`Enumerations`/`CurrentProvenance`/`CurrentMetadata`, relays `EntitySelected`/`EntityDoubleClicked`, and `WireInspector(EntityInspectorControl)` re-wires a hosted inspector's edit/delete/pin events to the model panel exactly as the in-editor inspector did; `ApplyVisibility`/`ApplyCollapse` are public and re-sync the hosted inspector.
- 2026-08-21 — **`EntityDetailsWindow` (app):** a resizable modeless `Window` hosting the inspector (440×640 default), `Attach(editor)` wires it once, `ShowEntity(table|edge)`/`ShowModelInfo()` drive it. `MainWindow` creates it lazily on the first double-click, nulls the reference on `Closed` (a closed WinUI window cannot be re-activated), follows single-click selection while it is open, and adds **File → Model info** (the model-level provenance/metadata readout that used to idle in the inspector panel).
- 2026-08-21 — **Tests:** no pure-logic change (all edits are UI/wiring) — the suite stays green.

## Results

- **Completed:** `042`
- **Deferred:** — (the backlog now holds `040`, unscheduled)
- **Notes:** Verified `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **256/256 pass**. Manual verification — double-click a table/connector opens the window read-only, Edit/Done flips the 029 surface, edits re-render the drawing + both renderers agree, File → Model info, the window follows single-click selection, pan/zoom/drag unaffected — needs a human run; CLI launch runs on the agent's non-interactive desktop.
