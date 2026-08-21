# 042 — Entity details window: modeless pop-up with view/edit modes

## Summary

Replace the right-panel **Inspection** panel with a **modeless details window**: double-clicking a table (or FK connector) opens a resizable pop-up window where the entity's details are comfortably shown — read-only by default, with the editing capabilities revealed only when an **Edit** option is pressed. The Inspection panel is removed from the right column (which then holds only the diagnostics log). **User decision (2026-08-20):** the pop-up is a **modeless secondary `Window`**, not a modal `ContentDialog` — the user can keep it open and reference the drawing while editing.

## Goals

- [ ] Double-click a table (and an FK connector, for parity) opens a **modeless details window**; single-click remains select-only (the existing DodgerBlue highlight).
- [ ] The window shows the entity's details **read-only by default**; an **Edit** button reveals the 029 edit surface (gated by the 028 `NodeVerbs`), a **Done** button returns to read-only.
- [ ] The **Inspection panel is removed** from `ModelEditorControl`'s right column; the right panel keeps only the diagnostics log (and the 014 collapse toggle).
- [ ] The 037 tags editor and 038 Show/Hide pins relocate into the details window's edit mode; the model-level readout (`ShowModel` — provenance/metadata) moves to a "Model info" menu item rather than being dropped.
- [ ] Both renderers unaffected; the window's edit events route back to the editor's `ApplyVisibility`/`ApplyCollapse` paths exactly as the inspector's do today.

## Scope

**In scope:**
- `EntityInspectorControl` gains a **View/Edit mode** (read-only default; edit surface shown on demand) — the control is reused as the window's content, not rewritten.
- A new **`EntityDetailsWindow`** (`Window` subclass) in `Model.Controls.WinUI` hosting the inspector control — resizable, modeless.
- **Double-click detection** in the `Gl*` stack (`GlContext` raises a double-click signal, or the canvas handles `DoubleTapped` and resolves the object); single-click stays select-only.
- Host wiring: `MainWindow` opens the window on double-click and routes the window's edit events back to the editor; `ModelEditorControl` drops the inspector from the right column.
- Docs/WORKLOG.

**Out of scope:**
- The 029 edit surface itself (unchanged — it moves, not reworks).
- The 038 visibility / 039 collapse layers (unchanged; the explorer keeps its group toggles).
- The diagnostics log (stays in the right panel).
- Multiple-window lifecycle beyond the one details window (no MDI, no docking).

## Approach / Notes

- **Reuse, don't rebuild:** `EntityInspectorControl` already has the readout (columns, PK/FK tags, description, provenance, metadata, FK list) and the full 029 edit surface gated by `NodeVerbs`, plus the events that drive the canvas (`EntityRenamed`, `ColumnRenamed`, `StructureChanged`, `VisibilityPinChanged`…). The smallest change is a **View/Edit mode** on that control (a `Mode` property or a toggle that shows/hides the edit controls) and a thin `Window` wrapper. Only the *panel hosting* in the right column is removed.
- **Modeless window (user decision):** a separate resizable `Window` (WinUI 3 supports multiple windows, unpackaged included) so the user can reference the drawing while editing. A modal `ContentDialog` was considered and rejected — it blocks the canvas.
- **Double-click vs. drag:** `GlContext` already separates drag from click via the 2 px movement threshold; double-click is a separate gesture and does not conflict. Detection lives in the `Gl*` stack so both renderers' hit-testing stays consistent.
- **Connectors:** double-click an FK connector opens the same window in connector mode (cardinality/roles readout + the 029 FK editors), preserving today's inspector behavior — otherwise there is no way to see dependency details after the panel is gone. The 027 hover tooltip covers quick lookups.
- **Relocations:** the 037 tags editor and 038 Show/Hide pins move into the window's edit mode; the model-level readout (`ShowModel`) moves to a "Model info" menu item (the diagnostics log already reports load info).
- **Reusability:** `EntityDetailsWindow` lives in `Model.Controls.WinUI` (like the other six controls) so the library stays host-agnostic; the host wires the window's edit events back to the editor's `ApplyVisibility`/`ApplyCollapse` paths, same as today.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [x] Double-clicking a table opens the modeless details window (read-only); **Edit** reveals the NodeVerbs-gated controls; **Done** returns to read-only; edits re-render the canvas as today.
- [x] Double-clicking an FK connector opens the same window in connector mode.
- [x] The Inspection panel is gone from the right column; the diagnostics log remains; single-click still selects.
- [x] Manual run: open details, edit a table/column/FK from the window while the drawing stays visible, verify both renderers agree; pan/zoom/drag unaffected.
- [x] `docs/WORKLOG.md` updated (and `CLAUDE.md`: the details window replaces the inspector panel; the View/Edit mode).

## Status

- **State:** Complete
- **Sprint:** 2026-08-21 (entity details window)
- **Completed:** 2026-08-21
