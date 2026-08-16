# 009 — Zoom & fit: scale slider, % entry, fit-to-window

## Summary

The drawing canvas is fixed-size: the user cannot scale the 50-table schema to see it as a whole or zoom into detail. This sprint adds zoom/scale controls — a scale slider, a % entry box, and a fit-to-window button — built on the ScrollViewer's native zoom so pinch and Ctrl+wheel work for free.

## Goals

- [ ] Scale slider (e.g. 10%–400%, snap points) that zooms the drawing.
- [ ] % entry box (validated, clamped to min/max) that zooms the drawing.
- [ ] Fit-to-window button (icon) that scales the whole drawing to fit the viewport, centered.
- [ ] Zoom around the cursor (Ctrl+wheel / pinch zooms toward the mouse).
- [ ] Pointer hit-testing (grips/handles) still correct at non-100% zoom.

## Scope

**In scope:**
- Zoom mechanism via the existing ScrollViewer's `ZoomFactor` / `MinZoomFactor` / `MaxZoomFactor` + `ChangeView(zoomFactor, ...)` — no hand-rolled `ScaleTransform`.
- A zoom toolbar (slider + % textbox + fit button) in `ModelEditorControl` (or `ModelPanelControl`), wired to the ScrollViewer.
- Fit-to-window: `min(viewportW/extentW, viewportH/extentH)`, capped at 100% so a small drawing doesn't blow up, then centered.
- Zoom-around-cursor for Ctrl+wheel / pinch (pass a zoom-center point to `ChangeView`).
- Keyboard shortcuts: Ctrl+0 (100%), Ctrl+1 (fit), Ctrl+Plus / Ctrl+Minus.
- Persist the zoom level across redraws (the draw loop rebuilds the canvas; keep the level in a field and re-apply).
- Status-bar zoom readout synced to the slider.
- Verify/fix `GlContext` pointer hit-testing and grip/handle math at non-100% zoom.
- Update `docs/WORKLOG.md`, `docs/codebase-functionality-map.md`, `CLAUDE.md` as needed.

**Out of scope:**
- Zoom-to-table (double-click a table to zoom to it) — clean follow-up.
- Panning improvements beyond what the ScrollViewer already provides.
- Zoom in the Skia stack (`SkiaPanelControl` is unwired; Skia zoom would be a canvas matrix transform — different mechanism).
- Persisting zoom across app sessions.

## Approach / Notes

- **Use the ScrollViewer's native zoom.** The Canvas is already wrapped in a ScrollViewer (`ModelPanelControl`). WinUI's ScrollViewer exposes `ZoomFactor`, `MinZoomFactor`, `MaxZoomFactor`, and `ChangeView(horizontalOffset, verticalOffset, zoomFactor, ...)` — the slider/textbox/fit button all reduce to `ChangeView`. Pinch and Ctrl+wheel zoom come along for free.
- **Fit math:** `fit = min(viewportW / extentW, viewportH / extentH)`, clamped to `[MinZoomFactor, 1.0]` (cap at 100%), then `ChangeView` with the offsets that center the content.
- **Zoom around cursor:** `ChangeView` accepts a zoom-center point; compute it from the pointer position so Ctrl+wheel zooms toward the mouse rather than the corner.
- **Pointer hit-testing gotcha:** with ScrollViewer zoom, pointer coordinates land in content space, but `GlContext`'s hit-testing and the grip/handle math assume a 1:1 mapping. Verify that path before wiring the slider, or the grabbers will misbehave at non-100% zoom.
- **Slider snap points:** 25/50/75/100/150/200/400 — keeps the slider meaningful across a wide range.
- **% textbox:** validate + clamp to `[MinZoomFactor*100, MaxZoomFactor*100]`; commit on Enter / focus loss.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [ ] App launches unpackaged; slider, % box, and fit button all zoom the drawing; Ctrl+wheel zooms toward the cursor; Ctrl+0/1/Plus/Minus work.
- [ ] Fit button shows the whole drawing, centered, capped at 100%.
- [ ] Grips/handles still select and drag correctly at non-100% zoom.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/CURRENT.md` (2026-08-16 connector routing)
- **Completed:** 2026-08-16
