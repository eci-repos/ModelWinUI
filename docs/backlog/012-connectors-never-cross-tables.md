# 012 — Connector paths must never cross tables (route through the gaps)

## Summary

The orthogonal router already treats tables as obstacles, but the "no line on top of a table" guarantee is incomplete: the Z-path fallback and the anchor-stitching segments are never checked against table interiors, so some connectors in the 50-table schema can still be drawn over tables. This item makes "no connector segment crosses a table interior" a hard invariant — routing through the space between tables.

## Goals

- [ ] No connector segment crosses the interior of any table (hard invariant, verified after routing).
- [ ] The Z-path fallback routes around tables instead of straight through them (currently unchecked).
- [ ] Anchor-stitching segments (anchor→stub, stub→first grid point, last grid point→endStub) avoid tables.
- [ ] Routes keep using the gaps between tables (A* already does this — preserve it).
- [ ] A post-route verification/repair step: if a route crosses a table, re-route or adjust rather than ship the bad line.

## Scope

**In scope:**
- Post-route validation of every returned polyline against table interiors, in `OrthogonalRouter.Route` (and therefore `SequentialRouter.RouteAll`).
- Fix the Z-path fallback so it avoids tables (route around, or fall back to a safe corridor) instead of returning an unchecked Z.
- Validate/nudge the anchor-stitching segments so a route never starts inside a blocked cell or crosses a neighbor table on its way out.
- Keep the direct-path optimization (straight lines for side-by-side tables) but confirm it still satisfies the no-crossing invariant.
- New unit tests: no routed segment crosses any table rect, across the 50-table `PublicSafetySchema` fixture and adversarial layouts (tables packed tight, anchors adjacent to a neighbor table, unreachable-grid cases that hit the fallback).
- Update `docs/WORKLOG.md`, `docs/codebase-functionality-map.md`, `CLAUDE.md` as needed.

**Out of scope:**
- Corner rounding (deferred from backlog 007 — rounding can re-intersect obstacles in tight gaps; this item is about the routing guarantee, rounding stays deferred).
- Skia stack (`SkiaPanelControl` is unwired).
- Visual polish (line thickness, colors, arrowheads).

## Approach / Notes

- **Concrete gaps in `OrthogonalRouter.Route`** (`src/ModelGraphLibrary/Graph/OrthogonalRouter.cs`):
  1. **Step 3 — Z-path fallback:** when A* returns null (grid unreachable), the fallback `start → startStub → (endStub.X, startStub.Y) → endStub → end` is returned **without any obstacle check** — it can draw straight through tables.
  2. **Step 4 — stitching segments:** the final polyline `start → startStub → gridPath → endStub → end` is never validated against obstacles. The start cell is exempted from blocking (`startEnd` in `AStar`), so the anchor→stub segment can cross a neighboring table.
  3. **Step 1 — direct path:** the HV/VH check uses the **un-inflated** obstacles, so a direct line can run flush against a table edge with zero clearance. This is intentional (keeps straight lines for side-by-side tables) and does not cross interiors, but the invariant should be stated and tested.
- **Verification/repair pattern:** after building the polyline, run a `RouteIsClear(points, rawObstacles)` check (reuse `Rect2.SegmentCrossesInterior`). If it fails, re-route with a fallback strategy — e.g. skip the direct-path shortcut and force A*, or route to a safe intermediate point — rather than returning the bad line.
- **Fallback corridor:** the Z fallback should pick a path through the gaps between tables (e.g. route to a point in the nearest open corridor, then to the target) instead of a straight Z.
- **Clearance:** A* paths already get `ObstacleMargin` clearance via inflation; the stitching segments need the same treatment so the guarantee is "no crossing" *and* "no hugging".
- **Test fixture:** the 50-table `PublicSafetySchema` (`ModelGraphLibrary/ModelData/`) is the natural regression corpus — assert every routed segment avoids every table rect.

## Definition of Done

- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [ ] New unit tests assert no connector crosses any table interior for the 50-table schema and tight/adversarial layouts; existing 43 tests still pass.
- [ ] App launches unpackaged; the rendered 50-table schema shows no connector overlapping a table.
- [ ] The Z-path fallback and anchor-stitching paths are covered by tests (not just the happy path).

## Status

- **State:** Planned
- **Sprint:** (TBD)
- **Completed:** (date, once moved to `archive/`)
