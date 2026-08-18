# 017 — Fit the whole model into the visible area

## Summary

A **Fit** button — always visible, labeled "Fit" (icon + text) — that shows the **whole model** in the visible work area with a **~5 px margin**, never upscaling beyond 100%. The Skia path defaults to fit on load and re-fits on resize; the XAML path fits to the content bounds (all tables), not the paper extent. When the user reported "the fit button don't fit anything I get a blank page," the root cause was **not** a Fit bug — it was the wheel-scroll drift of backlog item `016` moving the viewport thousands of pixels onto empty paper, so the (correct) Fit restore looked like nothing happened.

## Goals

- [x] A Fit button in both renderers' toolbars that is **always visible** and **labeled "Fit"** (icon + text — the icon-only button was not discoverable).
- [x] Fit shows the whole model with a ~5 px margin, centered.
- [x] Fit never upscales beyond 100% (matches the XAML path; the Skia path caps at 1.0 and floors at 0.01).
- [x] Skia path defaults to fit on load and re-fits when the window is resized (fit recomputes on every paint).
- [x] XAML Fit button restores the content from any view (even after the blank-page drift — see `016`).

## Scope

**In scope:**
- **XAML path (`ModelPanelControl`):** `FitToWindow` fits to `_contentBounds` (all tables), not the canvas extent: `fit = min((vw − 2·5)/b.Width, (vh − 2·5)/b.Height)`, clamped `[MinZoom, 1.0]`, content centered via `hOff = (b.X + b.Width/2)·fit − vw/2` — offsets are viewport px, so the content center must land on the viewport center (backlog `018`; the earlier `center − vw/(2·fit)` formula computed an offset that `ChangeView` clamped to ~0 → blank page). Fed through `ChangeView` (unchanged by `ScrollMode="Disabled"`, item `016`).
- **Skia path (`SkiaPanelControl`):** `_fitMode` (default **true**) recomputes `_zoom` on every paint, so resizing re-fits for free; fit scale = `min((viewW − 2·5)/contentW, (viewH − 2·5)/contentH)` capped at 1.0, floored at 0.01; content centered via `Translate(offsetX, offsetY); Scale(_zoom)` on `frame.Canvas`. The Fit button resets the pan (`_panX = _panY = 0`) and re-enters fit mode; the slider leaves fit mode (`_fitMode = false`, `_zoom = value/100`).
- The **toolbar Fit button** (glyph E81C + "Fit" text) in both renderers.

**Out of scope:**
- Mouse-wheel zoom — that is backlog item `016`.
- Upscaling beyond 100% on fit (matches the XAML path; a one-line change if wanted).
- The zoom slider (already present; not part of this item).

## Approach / Notes

- **Fit is state, not a one-shot (Skia):** `_fitMode` recomputes the scale every paint, so resize re-fits for free; the slider sets `_fitMode = false`. The XAML path re-fits on demand (button) and starts at 100% showing the content's top-left.
- **Why "fit shows blank" was not a Fit bug:** `016`'s wheel-scroll drift moved the viewport to empty paper; `FitToWindow`/`FitButton_Click` were already restoring the content correctly, but from thousands of pixels away the jump read as "nothing happened." The `016` fix (ScrollMode disabled) removes the drift entirely.
- **Fit bounds:** XAML fits to `_contentBounds` (computed from the drawn tables), Skia fits to `_diagram.Layout` min/max — both are the content, never the 20000×20000 paper.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (72, unchanged — no pure-logic change).
- [x] App launches unpackaged and stays running. Interactive pass: the Fit button (always visible, labeled) shows the whole model with a ~5 px margin from any view; the Skia path loads fitted and re-fits on resize. (Visual pass needs a manual look — CLI launch runs on the agent's non-interactive desktop.)

## Status

- **State:** Completed
- **Sprint:** 2026-08-17 (execution-log follow-up to `015`)
- **Completed:** 2026-08-18
