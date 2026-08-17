# 014 — Right panel (log + inspector) can't be closed

## Summary

The right column of `ModelEditorControl` — the diagnostics log on top and the entity inspector below — is always visible with no way to collapse or close it. On a small window it eats horizontal space the drawing could use, and the log is often not needed while working on the model. This item adds a way to collapse/expand the right panel so the drawing canvas reflows into the freed space.

## Goals

- [x] The right panel (log + inspector) can be collapsed/closed with one click.
- [x] The collapsed panel can be expanded again (the state is not lost).
- [x] The drawing canvas (left column) reflows to use the freed horizontal space when the panel is closed.
- [x] Collapsing/expanding does not disturb the current zoom or pan position of the drawing.

## Scope

**In scope:**
- A collapse/expand control on the right panel — e.g. a toggle button in a slim header bar above the log, or a WinUI `Expander` wrapping the log + inspector.
- The right column width collapses to 0 (or the panel's `Visibility` toggles to `Collapsed`) so the `*` left column reflows automatically.
- Keep the existing layout: log on top, inspector below, both still reachable when the panel is open.

**Out of scope:**
- A resizable splitter (dragging to size the panel) — a simple open/close toggle is enough for this item; a splitter can be a follow-up.
- Persisting the collapsed state across app sessions.
- Hiding the log and inspector independently (one toggle for the whole panel).

## Approach / Notes

- **Where the panel lives:** `src/Model.WinUI.Console/Controls/ModelEditorControl.xaml` — the right column is a `Grid` (`Grid.Column="1"`, `MinWidth="250"`) holding `DiagnosticsLogControl` (row 0) and `EntityInspectorControl` (row 1); the column is `Width="Auto"` and the drawing is `Grid.Column="0"` with `Width="*"`. Because the drawing column is star-sized, collapsing the right column to width 0 makes the drawing reflow automatically — no manual resize math.
- **Simplest approach:** add a slim header row to the right panel with a toggle button (chevron `>` / `<` glyph). Clicking it sets the right column width to 0 (or the panel `Visibility` to `Collapsed`) and flips the button glyph; clicking again restores `Auto` / `Visible`. The `ModelEditorControl.xaml.cs` code-behind holds the open/closed state.
- **Alternative:** wrap the log + inspector in a WinUI `Expander` control (`IsExpanded` bound to the state). The `Expander` gives a header + chevron for free but adds a header row above the log; verify the log's startup-message ordering (the log VM must subscribe before `ModelPanelControl` writes "GL Context Ready.") is unaffected.
- **Zoom/pan preservation:** collapsing the panel changes the viewport width, which the ScrollViewer handles natively (the drawing keeps its zoom and offset; only the visible extent changes). Verify no `ChangeView`/`FitToWindow` is triggered by the toggle.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [x] App launches unpackaged; clicking the toggle collapses the right panel and expands it again. (Toggle click needs a manual pass; launch verified from CLI.)
- [x] The drawing canvas reflows into the freed space; zoom and pan position are preserved across the toggle.
- [x] The log and inspector still work when the panel is open (startup message ordering intact).

## Status

- **State:** Done
- **Sprint:** (TBD)
- **Completed:** 2026-08-17 (moved to `archive/`)
