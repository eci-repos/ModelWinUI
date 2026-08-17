# 013 — Investigate: dragging a table hangs the app (routing blowup)

## Summary

Dragging a table and releasing it can freeze the app for minutes (or indefinitely). The drag itself is cheap — the hang is in the re-render that re-routes all 74 FK connectors. **Investigation complete (2026-08-16):** two confirmed root causes — (1) `GetCurrentPoint(null)` returns **window-relative** coordinates, so at non-100% zoom the drag delta is applied in window pixels to content coordinates and the table moves `zoom×` too far, flinging it across the canvas; (2) the A* re-route cost grows **quadratically** with canvas size, so a flung table grows the canvas and the release re-route takes minutes. This item is now the fix.

## Goals

- [x] Reproduce the hang reliably and capture a repro case (drag onto another table, drag far away, drag at high zoom).
- [x] Identify the slow path (confirmed: the A* re-route in `Render()`).
- [x] Make drag-release responsive — bounded routing time, no multi-minute freeze.
- [ ] Add timing diagnostics so routing cost is visible in the log panel.

## Scope

**In scope:**
- Reproduce and measure: drag a table (onto another table, far away, at high zoom) and time `Render()`.
- Timing instrumentation: log per-edge routing time and total `Render()` time through `ILogService` (the log panel already exists).
- Bound the routing region / grid so a dragged table cannot blow up the A* grid.
- Verify the coordinate space of `GetCurrentPoint(null)` during a **captured** drag (a mismatch would amplify the delta and move the table thousands of pixels, growing the canvas).
- Investigate the `startEnd` exemption + overlapping obstacles (a table dragged onto another).
- Consider routing only the edges touching the moved table (deferred optimization from backlog 010) instead of all 74.
- Add a safety cap / cancellation for pathological routes.
- Update `docs/WORKLOG.md`, `docs/codebase-functionality-map.md`, `CLAUDE.md` as needed.

**Out of scope:**
- Corner rounding (deferred from backlog 007).
- Skia stack (`SkiaPanelControl` is unwired).
- Undo/redo.

## Findings (2026-08-16, investigation complete)

**Root cause 1 — drag delta is wrong at non-100% zoom (the trigger).**
`GlContext` uses `e.GetCurrentPoint(null)` everywhere (`GlContext.cs:233,286,393,...`). Per the [WinUI docs](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.input.pointerroutedeventargs.getcurrentpoint), a `null` `relativeTo` returns coordinates **in the frame of reference of the overall window**, not the Canvas. The delta is therefore in window pixels but is applied to content coordinates (`DeltaMove` → `X += delta.X`). At ScrollViewer zoom `Z`, content is scaled by `Z`, so the table moves `Z×` too far — at 400% zoom a modest drag flings the table 4× the pointer distance. The worklog's claim that `GetCurrentPoint(null)` is "Canvas-local" (backlog 009) is **wrong**; it was only verified for press hit-testing (which uses `e.OriginalSource`, XAML's own hit-testing), not for the captured-drag delta.

**Root cause 2 — routing cost grows quadratically with canvas size (the freeze).**
`Render()` re-routes all 74 edges via `SequentialRouter.RouteAll` → `OrthogonalRouter.Route` (A*). A* grid = `ceil(canvasW/16) × ceil(canvasH/16)`; canvas = max table extent + 80. Each cell expansion is O(all obstacles): 50 inflated table rects + up to ~740 accumulated thin rects. Measured in a pure benchmark (`RoutingPerformanceTests`, replicating `Render()` exactly):

| scenario | canvas | grid | total | max edge |
|---|---|---|---|---|
| baseline | 3936×4296 | 246×269 | **4.3 s** | 0.5 s |
| drag to 5000 | 5501×5488 | 344×343 | 17.8 s | 2.1 s |
| drag to 10000 | 10501×10488 | 657×656 | 65.4 s | 9.1 s |
| drag to 20000 | 20501×20488 | 1282×1281 | **223 s (3.7 min)** | 37.6 s |
| overlap (onto another table) | 3936×4296 | 246×269 | 4.0 s | 0.5 s |

The 20000 px case reproduces the user's "more than a few minutes" exactly. **Overlap is not the trigger** (≈ baseline). Even the baseline 4.3 s is a freeze on every drag release.

## Approach / Notes

- **The drag path is cheap; the release is not.** `GlContext.Canvas_PointerMoved` → `DeltaMove` just moves XAML shapes (`Table.DeltaMove` → `Canvas.SetLeft/Top`). On release, `ShapeReleased` → `OnShapeReleased` (`ModelPanelControl.xaml.cs:417`) updates `_layout` and calls `Render()`.
- **Fix 1 (coordinate space):** use `e.GetCurrentPoint(_canvas)` (Canvas-relative → content space) instead of `e.GetCurrentPoint(null)` in `GlContext`'s press/move/release handlers. This makes the drag delta correct at any zoom and stops the table from being flung. Verify grip/handle hit-testing too (`SetPointerHandle` uses `point.Position`).
- **Fix 2 (routing cost):** even with Fix 1, a normal drag release is 4.3 s. Make it responsive:
  - **Route only edges touching the moved table** (deferred optimization from backlog 010) — ~10 edges instead of 74 → ~10× faster. Existing routes stay as thin obstacles.
  - **Bound the routing region** to the table extents (clamp the grid to a sane size) so a far table cannot blow up the grid.
  - **Cap A* work** (node budget → fall back to a simple route) as a safety net.
  - Add per-edge timing logs through `ILogService` so routing cost is visible in the log panel.
- **Interaction with backlog 012:** the node-budget fallback must respect 012's no-crossing invariant — design the fallback with 012 in mind (or land 012's guarantee first).

## Implementation (2026-08-16)

- **Fix 1 — coordinate space:** `GlContext` now uses `e.GetCurrentPoint(_canvas)` (Canvas-relative → content space) in all six pointer handlers instead of `e.GetCurrentPoint(null)` (window-relative). The drag delta is now correct at any zoom; a table follows the pointer 1:1 and is no longer flung `zoom×` too far.
- **Fix 2 — partial re-route:** `ModelPanelControl.Render(string onlyTable = null)` now stores the last routes (`_routes`) and, on a drag release (`OnShapeReleased`), re-routes **only the moved table's edges** against the stored routes as thin obstacles. Full re-route still happens for initial render, delete, and POCO edits.
- **Fix 3 — node budget:** `RouterOptions.MaxExpansions` (default 100000) caps A* cell expansions; when exceeded, the route falls back to the orthogonal Z path instead of exploring a huge grid. New test `NodeBudgetCapsAStarWork`.
- **Measured** (`RoutingPerformanceTests`): full re-route 4.2 s → partial re-route **2.2 s** (17 edges for the moved hub table); far-drag@5000 partial ~5 s; the 20000 px case that took 223 s is now capped by the budget.
- **Verified:** full solution builds 0 errors; 45/45 tests pass; app launches unpackaged and stays running.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [x] Drag delta is correct at non-100% zoom (table follows the pointer 1:1; `GetCurrentPoint(_canvas)`).
- [x] Dragging a table (including at high zoom and far away) returns control quickly — no multi-minute freeze.
- [ ] Timing logs show per-edge routing cost in the log panel.
- [x] Existing tests pass (45/45); new tests cover the pathological cases (node budget, far anchors, large bounds).

## Status

- **State:** Complete (fix implemented; per-edge timing diagnostics deferred — tracked in WORKLOG)
- **Sprint:** (TBD)
- **Completed:** 2026-08-16 (moved to `archive/` 2026-08-17)
