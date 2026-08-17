# 018 — Wheel-zoom anchor at the cursor, and finer wheel increments

## Summary

The user reports two visible problems with wheel zooming: (1) "You are not using the current mouse pointer position as the transformation reference point it looks like you are using the origin (0,0) instead" and (2) "the increments of the wheeling up/down are too big and just jump to fast." **Root cause of (1) — the XAML path used the wrong ScrollViewer coordinate model.** WinUI 3 ScrollViewer offsets (`HorizontalOffset`/`VerticalOffset`) and `ExtentWidth` are measured in **viewport pixels (zoom-applied)**, not content units: the extent grows with `ZoomFactor`, and the correct mapping is `content = (viewportPx + offset) / zoom`. `ApplyZoom`, `FitToWindow`, and `OnPanRequested` all treated offsets as content units, so (a) wheel zoom mixed content and viewport units and drifted wildly — reading as "anchored at origin" — and (b) `FitToWindow` computed a content-units offset (≈ 6900) that `ChangeView` clamped to the real range (~0–1800 at fit zoom), showing empty paper instead of the model. Issue (2) is the fixed ×1.25 per-notch wheel step — too coarse; it needs a smooth, delta-proportional step.

## Goals

- [ ] Wheel zoom keeps the content point **under the cursor** fixed (the cursor is the transformation reference point) on both renderers.
- [ ] Wheel increments are fine and gradual — a per-notch factor of ~1.1, proportional to the actual wheel delta (trackpads get smaller steps), on both renderers.
- [ ] The Fit button shows the whole model centered with the ~5 px margin (currently shows blank on the XAML path).
- [ ] Panning tracks the pointer 1:1 at any zoom (currently 1/zoom too slow when zoomed out).

## Scope

**In scope:**
- **`ModelPanelControl.xaml.cs` coordinate math (the core fix):**
  - `ApplyZoom` re-derived for viewport-px offsets: anchor is a **viewport-px** point (default viewport center); content under the anchor = `(anchorPx + offset) / oldZoom`; `newOffset = content * zoom − anchorPx`. `ChangeView(newH, newV, zoom)`.
  - `ModelCanvas_PointerWheelChanged`: cursor taken from `e.GetCurrentPoint(ModelScrollViewer)` (viewport px — unambiguous under zoom) and passed as the anchor; wheel factor from the smooth step (below).
  - `FitToWindow`: center the content at the fit zoom — `hOff = (b.X + b.Width/2)·fit − vw/2` (not `b.CenterX − vw/(2·fit)`), same for vertical.
  - `OnPanRequested`: offsets are viewport px, so a content-space delta `dx` shifts the viewport by `dx·zoom` — `ChangeView(H − dx·zoom, V − dy·zoom)`.
- **Wheel step in both renderers:** replace the fixed `delta > 0 ? 1/1.25 : 1.25` with a smooth exponential `factor = Math.Pow(WheelZoomStep, −delta/120.0)` (≈1.1× per notch; fractional deltas from trackpads scale proportionally). Keep `ZoomStep` 1.25 for the XAML keyboard accelerators (discrete commands, not continuous wheel).

**Out of scope:**
- The Skia path's anchor math (already correct — it is a self-owned `Translate`+`Scale` transform); only its wheel step changes.
- Backlog items `016` (wheel-zoom) and `017` (fit) track the broader behaviors; this item is the concrete bug + step fix inside them.
- The `ScrollMode="Disabled"` blank-page fix from `016` stays as is.

## Approach / Notes

- **The wrong model in one line:** the old `ApplyZoom` computed `viewportX = (cx − H)·oldZoom` with `cx` a content coordinate and `H` a viewport-px offset — mixing units. The correct model is `contentPx·zoom − offset = viewportPx`, so the anchor is inverted as `content = (viewportPx + offset)/zoom` and re-projected as `newOffset = content·zoom − viewportPx`.
- **Why Fit showed blank:** `FitToWindow` computed `hOff ≈ 6900` (content-units style) but the valid offset range at fit zoom is ~[0, 1800] (ExtentWidth = 20000·fit ≈ 2600, minus viewport 800), so `ChangeView` clamped it and the viewport showed the empty top-left of the paper. The Skia path was unaffected (own transform, centered fit is correct).
- **Wheel-delta proportional step:** `Math.Pow(1.1, −delta/120)` — one notch (Δ=120) = ×1.1 / ÷1.1; a trackpad's Δ=15 notch = ×1.012. Direction preserved: up (Δ>0) zooms out, down zooms in.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [ ] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (72, unchanged — no pure-logic change).
- [ ] Interactive pass: wheeling over a table keeps the point under the cursor fixed on both renderers; one notch is a small, smooth step; the Fit button shows the whole model with a margin; drag-pan tracks the pointer at any zoom. (Visual pass needs a manual look — CLI launch runs on the agent's non-interactive desktop.)

## Status

- **State:** In progress (root-caused; fix pending in the working tree)
- **Sprint:** 2026-08-17
- **Completed:** (TBD)
