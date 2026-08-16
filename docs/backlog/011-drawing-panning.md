# 011 — Drawing panning: drag-to-pan the canvas

## Summary

The drawing can be zoomed (009) but not panned with the mouse — the ScrollViewer's scrollbars and touch/pen work, but a diagramming tool needs drag-to-pan. This item adds mouse-driven panning of the drawing so the user can navigate the 50-table schema without hunting for scrollbars.

## Goals

- [ ] Drag on **empty canvas space** pans the view (left-drag on the background).
- [ ] Middle-mouse drag pans (common diagramming convention).
- [ ] Space+drag pans (keyboard-modifier convention).
- [ ] Panning preserves the current zoom level.
- [ ] Table-drag (move, from 010) still works — press on a table moves it, press on empty space pans.
- [ ] Cursor feedback (hand / grabbing cursor while panning).

## Scope

**In scope:**
- Pan via the existing ScrollViewer's `ChangeView(horizontalOffset, verticalOffset, zoomFactor)` — pass the current `ZoomFactor` so panning never resets zoom.
- Disambiguation in the pointer pipeline: a press that hits a shape keeps the existing drag/select behavior (010); a press that hits nothing starts a pan gesture.
- Middle-mouse drag and space+drag as additional pan triggers.
- Cursor feedback (`InputSystemCursor` hand / grabbing) while panning.
- Optional: a hand-tool toggle button in the zoom toolbar row for discoverability.
- Update `docs/WORKLOG.md`, `docs/codebase-functionality-map.md`, `CLAUDE.md` as needed.

**Out of scope:**
- Panning in the Skia stack (`SkiaPanelControl` is unwired).
- Auto-pan / edge-scroll while dragging a table near the viewport edge.
- Persisting the pan position across app sessions.
- Minimap / overview navigation.

## Approach / Notes

- **The ScrollViewer already pans via scrollbars and touch/pen** (`ModelPanelControl.xaml`); what's missing is mouse-drag panning. `ScrollViewer.PanMode` defaults to `Disabled` for mouse — and setting it to `Enabled` would make left-drag anywhere pan, **conflicting with table-drag (010)**. So implement panning in the pointer handlers instead.
- **Hook point:** `GlContext` already owns all pointer handling (press/move/release/capture) on the Canvas. A press that hits no shape currently does nothing — that's where a pan gesture starts. Track the pointer delta and feed it to `ChangeView` (offset += delta), preserving `ZoomFactor`.
- **Coordinate space:** pointer deltas from `GetCurrentPoint(null)` are in Canvas-local (content) space; at non-100% zoom the offset delta must be scaled by `1 / ZoomFactor` to move the view by the visual drag distance.
- **Middle-mouse:** detect the middle button in `PointerPressed` (`GetCurrentPoint(...).Properties.IsMiddleButtonPressed`). **Space+drag:** track the space key state (or a `KeyboardAccelerator`/`KeyDown` flag) and treat left-drag as pan while held.
- **Cursor:** swap to a hand cursor on hover over empty space and a grabbing cursor during a pan; restore the default over shapes.
- **Hand tool (optional):** a toggle in the zoom toolbar row (fit / slider / % box) that flips left-drag into pan mode even over shapes — the standard diagramming "hand" tool. If added, keep it visually consistent with the existing toolbar.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [ ] App launches unpackaged; left-drag on empty canvas pans, middle-drag pans, space+drag pans.
- [ ] Panning preserves the current zoom level (zoom does not jump or reset).
- [ ] Dragging a table still moves it (010 behavior intact); connectors still follow.
- [ ] Cursor shows hand/grabbing during pan.

## Status

- **State:** Planned
- **Sprint:** (TBD)
- **Completed:** (date, once moved to `archive/`)
