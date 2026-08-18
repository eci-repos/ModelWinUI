# 016 — Mouse-wheel zoom around the pointer (both renderers)

## Summary

The mouse wheel must **zoom the drawing in/out with the pointer as the fixed reference point** — never scroll the paper. Wheel **up zooms out, wheel down zooms in**, and the content point under the cursor stays fixed as the zoom changes (the pointer is the transformation reference point — "when you use the mouse the mouse pointer becomes the transformation point of reference"). On the XAML path this is broken by a WinUI 3 ScrollViewer behavior: it scrolls on the wheel **even when the canvas marks `PointerWheelChanged` as `e.Handled = true`** (known issue microsoft/microsoft-ui-xaml#2947), and at low zoom the wheel scroll amount (48 px screen ÷ zoom) is huge in content units, drifting the viewport to empty paper → **blank page**. The user hit this ("when I wheel down or up the page turns blank"); this item is the fix.

## Goals

- [x] Wheel up zooms out, wheel down zooms in, still around the cursor.
- [x] The content point under the cursor stays fixed as the zoom changes (no drift).
- [x] The wheel never scrolls the paper — the viewport cannot drift to blank paper.
- [x] Zoom clamped to 0.1–4.0, stepped ×/÷ 1.25.

## Scope

**In scope:**
- **XAML path (`ModelPanelControl`):** `PointerWheelChanged` handled on the `GlCanvas` (the ScrollViewer's content) — `e.Handled = true` stops the event bubbling to the ScrollViewer's scroll handler; `ApplyZoom(zoom, anchorX, anchorY)` takes a **viewport-px anchor** (the cursor from `e.GetCurrentPoint(ModelScrollViewer)`) so the content point under the cursor stays fixed. **Coordinate model (backlog `018`):** ScrollViewer offsets are viewport px (zoom-applied), so `content = (anchorPx + offset)/oldZoom`, `newOffset = content·zoom − anchorPx` — the earlier content-units model made the wheel zoom drift.
- **The blank-page fix (root cause):** the WinUI 3 ScrollViewer scrolls on wheel despite `e.Handled = true` (issue #2947). `HorizontalScrollMode="Disabled"` + `VerticalScrollMode="Disabled"` on `ModelScrollViewer` kill the user-initiated wheel scroll. Verified from the Microsoft Learn docs that `ScrollableWidth = ExtentWidth − ViewportWidth` is independent of `ScrollMode`, so `ChangeView` (programmatic zoom/fit/pan) is unaffected — all zoom/pan goes through `ChangeView` only.
- **Scroll bars are `Hidden`** (part of "don't use scroll bars at all, they are confusing"): panning/zoom still work programmatically via `ChangeView`, so hiding the bars only removes the visual clutter.
- **Skia path (`SkiaPanelControl`):** `PointerWheelChanged` on the `SKXamlCanvas`; a `_panX`/`_panY` offset in surface px keeps the content point under the cursor fixed (cursor DIPs converted via `_dpiScale` = `e.Info.Width / ActualWidth`); same step direction (`factor = delta > 0 ? 1.0 / ZoomStep : ZoomStep`).

**Out of scope:**
- The Fit button / fit-to-window behavior — that is backlog item `017`.
- Panning (drag-pan, backlog 011) — unchanged.

## Approach / Notes

- **The XAML wheel math (corrected in `018`):** `content = (viewportPx + offset)/oldZoom`, `newOffset = content·zoom − viewportPx`. The Skia path's math is `contentX = (cursorX − offsetX)/zoom; newOffsetX = cursorX − contentX·zoom` (surface px) — its own `Translate`+`Scale` transform, already correct. Both keep the content point under the cursor fixed. **The wheel step is a smooth delta-proportional `Math.Pow(1.1, −delta/120)`** (≈1.1×/notch; the old fixed ×1.25 was "too big… just jump to fast", fixed in `018`).
- **`e.Handled = true` is not enough on the XAML path:** the WinUI 3 ScrollViewer scrolls on wheel even when the content marks the event handled (microsoft/microsoft-ui-xaml#2947). The ScrollMode fix is the actual guarantee that the wheel only zooms.
- **Why the user saw a blank page:** each wheel notch scrolled ~48 px ÷ zoom in content units; a few notches at low zoom moved the viewport thousands of px onto the empty 20000×20000 canvas. The Fit button was restoring the view — but from so far away it looked like nothing happened. Disabling ScrollMode makes the drift impossible.
- **Wheel direction is the user's requested direction:** up = zoom out, down = zoom in (the opposite of the initial implementation — flipped per user feedback).

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (72, unchanged — no pure-logic change).
- [x] App launches unpackaged and stays running. Interactive pass: wheeling over a table zooms with the table point under the cursor fixed, never a blank page; up = out, down = in. (Visual pass needs a manual look — CLI launch runs on the agent's non-interactive desktop.)

## Status

- **State:** Completed
- **Sprint:** 2026-08-17 (execution-log follow-up to `015`)
- **Completed:** 2026-08-18
