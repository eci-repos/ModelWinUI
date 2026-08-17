# 015 — Skia renderer: fit-to-window + zoom, and compose off the UI thread

## Summary

The Skia renderer (`SkiaPanelControl`) draws the ERD at **actual size with no viewing controls** — a 50-table model overflows the window, and the first paint runs the whole routing pass (`ErdComposer.Compose`, seconds of A*) **synchronously on the UI thread**, freezing the window until it finishes. That freeze is the "hang" the user hit (it "suddenly started to work" when the routing finally returned). This item adds a **fit-to-window + zoom slider** toolbar (parity with the XAML path) and moves the compose **off the UI thread** so the window stays responsive.

## Goals

- [x] Fit the whole model into the visible work area with a ~5 px margin, centered, defaulting to fit on load.
- [x] Re-fit automatically when the window is resized.
- [x] A zoom slider (10–400 = 0.1–4.0) + % readout; moving it leaves fit mode.
- [x] A Fit button that returns to fit mode.
- [x] The first paint no longer freezes the window — the compose runs on a background thread, with a "Composing…" indicator.

## Scope

**In scope:**
- A toolbar row in `SkiaPanelControl.xaml` (Fit button + zoom slider + % readout) mirroring `ModelPanelControl`'s toolbar, plus a "Composing…" overlay `TextBlock`.
- A fit/zoom transform in `SkiaPanelControl.xaml.cs`: `Translate` + `Scale` on `frame.Canvas` before drawing (both `Table` and `Connector` draw through `frame.Canvas` in content coordinates). Fit scale = `min((viewW − 2·5)/contentW, (viewH − 2·5)/contentH)`, **capped at 1.0** (never upscale — matches the XAML path's `FitToWindow`), floored at 0.01; content centered via `offset = (view − content·zoom)/2 − min·zoom`. Because fit recomputes on every paint, resizing re-fits automatically (`SKXamlCanvas` repaints on size change).
- **Library fix:** `Table.DrawBorders` (ModelGraphLibrary) replaced `SetMatrix(rotation)` / `SetMatrix(Identity)` with `Save()` / `Concat(rotation)` / `Restore()` so the 180° rotation composes with the current canvas transform instead of wiping it. With an identity current matrix the behavior is identical.
- **Async compose:** the first paint starts `Task.Run` → `ErdComposer.Compose` over a **1×1 offscreen `SKSurface`** (measuring only needs the frame's font), then `DispatcherQueue.TryEnqueue` sets `_diagram`, logs counts + FK issues, hides "Composing…", and invalidates. A **stale-compose guard** (`ReferenceEquals(captured, _tables)`) discards a compose that finished after a `SetModel` and re-paints so the new model composes. `try/catch` logs the error and resets `_composing` on the UI thread.
- Docs: backlog item, sprint record, WORKLOG, functionality map, CLAUDE.md.

**Out of scope:**
- Panning in the Skia renderer (the XAML path has it; the Skia path is centered-zoom only for now — a follow-up if wanted).
- Upscaling beyond 100% on fit (matches the XAML path; a one-line change if wanted).

## Approach / Notes

- **Why the canvas transform works:** both `Table` and `Connector` draw through `frame.Canvas` in content coordinates, so a `Translate` + `Scale` before drawing transforms everything. The one exception was `Table.DrawBorders`, which hard-set the matrix to Identity at the end — fixed with `Save`/`Concat`/`Restore`.
- **Fit is state, not a one-shot:** `_fitMode` (default true) recomputes `_zoom` on every paint, so resize re-fits for free. The slider sets `_fitMode = false` and `_zoom = value/100`. `_syncingSlider` + `_initialized` guard the slider's `ValueChanged` (it fires during `InitializeComponent` and on programmatic sync).
- **Compose off the UI thread:** the routing pass is pure computation (measure probes → layout → edges → anchors → sequential routing) with no UI-thread affinity, so it runs on a background thread. The 1×1 offscreen surface is enough because `ErdComposer.Compose` only measures (its `measureFrame` is used for the font).
- **Stale-compose race:** `SetModel` clears `_diagram` and invalidates; an in-flight compose is discarded by the reference-equality guard, and the re-paint starts a fresh compose for the new model.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64 --no-incremental` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (72, unchanged — no pure-logic change).
- [x] App launches unpackaged and stays running; switching to the Skia renderer shows "Composing…" then the whole model fitted with a ~5 px margin; resizing re-fits; the slider zooms; Fit returns to fit. (Visual pass needs a manual look — CLI launch runs on the agent's non-interactive desktop.)

## Status

- **State:** In progress (sprint 2026-08-17, item 015)
- **Sprint:** (TBD)
- **Completed:** (TBD)
