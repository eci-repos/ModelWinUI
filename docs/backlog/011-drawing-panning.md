# 011 — Drawing panning: drag-to-pan the canvas

## Summary

The drawing can be zoomed (009) but not panned with the mouse — the ScrollViewer's scrollbars and touch/pen work, but a diagramming tool needs drag-to-pan. This item adds mouse-driven panning of the drawing so the user can navigate the 50-table schema without hunting for scrollbars.

## Goals

- [x] Drag on **empty canvas space** pans the view (left-drag on the background).
- [x] Middle-mouse drag pans (common diagramming convention).
- [x] Space+drag pans (keyboard-modifier convention).
- [x] Panning preserves the current zoom level.
- [x] Table-drag (move, from 010) still works — press on a table moves it, press on empty space pans.
- [x] Cursor feedback (hand / grabbing cursor while panning).

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

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [x] App launches unpackaged; left-drag on empty canvas pans, middle-drag pans, space+drag pans.
- [x] Panning preserves the current zoom level (zoom does not jump or reset).
- [x] Dragging a table still moves it (010 behavior intact); connectors still follow.
- [x] Cursor shows hand/grabbing during pan.

## Implementation (2026-08-16)

- **Pan gesture in `GlContext`** (`src/Model.WinUI.Console/Graphics/GLibrary/GlContext.cs`): a press that hits no shape starts a pan; middle-mouse drag pans regardless of what's under the pointer; left-drag while **space** is held pans even over a shape. Space state is queried via `InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Space)` (the WinUI 3 replacement for `CoreWindow.GetKeyState` — no focus tracking needed). **Mouse only** (`e.Pointer.PointerDeviceType == PointerDeviceType.Mouse`) so touch/pen keep panning natively via the ScrollViewer.
- **Pan plumbing:** `GlContext` captures the pointer, tracks the delta from the pan start point in Canvas-local (content) space, and raises a new `PanRequested(dx, dy)` event. `ModelPanelControl` subscribes and calls `ModelScrollViewer.ChangeView(HorizontalOffset - dx, VerticalOffset - dy, (float)ZoomFactor, true)`. Because the delta is in content units, `offset - delta` is 1:1 with the pointer at any zoom — no `1/ZoomFactor` scaling needed (that note in Approach applies to window-space deltas; `GetCurrentPoint(_canvas)` is content-space).
- **Cursor feedback:** `UIElement.ProtectedCursor` is protected, so a new `GlCanvas : Canvas` subclass exposes it as a public `Cursor` property (the officially recommended pattern). `GlContext` swaps to a hand cursor over empty space and a `SizeAll` move cursor while panning — `InputSystemCursorShape.Grabbing` does not exist in this SDK version. The drawing canvas in `ModelPanelControl.xaml` is now a `GlCanvas`.
- **Table-drag (010) intact:** a press that hits a shape (table, connector, endpoint circle) keeps the existing drag/select behavior; only a press on empty space (or middle/space+drag) starts a pan.
- **Hand-tool toggle button** (optional per Scope) was **not** added — the three pan triggers cover the need; noted as a possible follow-up.

## Status

- **State:** Complete
- **Sprint:** 2026-08-16
- **Completed:** 2026-08-16
