# 033 — Connector routing takes optimal paths: crossing is a cost, not a ban

## Summary

Some connector routes are wildly non-optimal: two nearby tables get a path that runs all the way to the extremes of the drawing and back, "adding lines that could be omitted." The cause is the **greedy-sequential wall cascade** — each routed connector becomes a thin "wall" that later connectors refuse to cross (a hard ban), so the first edges claim the short corridors and a late-routed short edge must detour thousands of pixels around the accumulated walls. Crossing another *connector* should be a **cost** (pay it only when avoiding it is absurdly expensive), while crossing a *table* stays forbidden. Measured on the 50-table public-safety model: 2 of 74 routes exceed 2× their anchor distance, worst case `Employee → Person` at **9.6×** (8252 px vs 861 px straight, routed out to the routing-region bounds `x=3624 / y=−24`). The same edge routed without the earlier walls is the optimal 1212 px.

## Goals

- [x] No connector route on the 50-table public-safety model exceeds ~2× its anchor-to-anchor distance (today: 2 routes do, worst 9.6×) — done: max 1.98×, `Employee → Person` 9.6× → 1.4× (1212 px).
- [x] The "no connector crosses a table interior" invariant (backlog 012) is untouched — only **connector-cross-connector** becomes a cost, and only when it buys a meaningfully shorter route.
- [x] The existing no-crossing look is preserved where it is cheap: routes still avoid other connectors unless avoiding them means a large detour.

## Scope

**In scope:**
- `src/Model.Geometry/OrthogonalRouter.cs` + `src/Model.Geometry/SequentialRouter.cs` — the routing policy change (both the full re-route in `SequentialRouter.RouteAll` and the drag re-route in `ModelPanelControl` that calls `OrthogonalRouter.Route` directly).
- A deterministic tolerance knob (e.g. on `RouterOptions`): the wall-ignoring route is taken only when `len(withWalls) > factor × len(withoutWalls)` (default ≈ 1.5).
- New pure regression tests (in `tests/ModelConsole.Tests`) covering the measured worst cases + the invariant.

**Out of scope:**
- Visual polish beyond the path geometry (corner rounding, arrowheads).
- Auto-arrange/re-layout after drags (separate known gap).
- Changing the A* grid/heuristic or obstacle inflation values.

## Approach / Notes

- **Root cause:** `SequentialRouter.RouteAll` feeds each routed polyline back as thin obstacles ("walls", `AddSegmentObstacles`, 4 px half-thickness). `OrthogonalRouter.Route` treats crossing a wall as forbidden — it only retries *without* walls when the grid is **unreachable** (`AStar` null). A walled grid is almost always still reachable (just with a huge detour), so the ban silently produces the 8252-px perimeter route. Reordering edges (nearest-first) does **not** fix it (measured: 4 edges > 2× instead of 2 — it just moves the cascade); the fix must make crossing cheap when the detour is absurd.
- **Fix (two-pass "route both, keep the shorter within a tolerance"):**
  ```csharp
  // In OrthogonalRouter (or as a small helper used by both call sites):
  var withWalls   = Route(start, end, obstacles, bounds, options, thin);
  var withoutWalls = Route(start, end, obstacles, bounds, options, null);
  var useWithout  = Length(withoutWalls) * factor < Length(withWalls);
  return useWithout ? withoutWalls : withWalls;
  ```
  `SequentialRouter.RouteAll` calls this per edge and feeds the **chosen** route's segments back as walls (self-consistent, deterministic). `ModelPanelControl`'s drag re-route path gets the same helper. Both branches never cross table interiors — the 012 invariant tests (`NoCrossingInvariantTests`) stay green, since they only assert no *table* crossing.
- **Measured on the public-safety model** (diagnostic harness: `tests/ModelConsole.Tests/RouteDiagnosticTests.cs`, a passing throwaway that composes `PublicSafetySchema` and compares routed length to anchor distance):
  - current: 2 edges > 2×, total 145,808 px;
  - nearest-first order: 4 edges > 2×, total 142,123 — **rejected**;
  - hybrid (keep shorter, tolerance 1): **0 edges > 2×**, total 134,407 px (−8%) — the chosen direction. With a tolerance factor ≈ 1.5 the count stays 0 and crossings become rarer.
- Keep the deterministic edge order (`FkEdgeExtractor`) and the existing A* unreachable-fallback (crossing walls) intact.

## Definition of Done

- [x] A regression test asserts **0 routes > 2×** their anchor straight-line distance on the 50-table public-safety model — `OptimalRoutingTests.PublicSafetyRoutesStayNearOptimal` (via `ErdComposer.Compose`, the real pipeline).
- [x] `Employee → Person` (the 9.6× case) routes at the optimal 1212 px.
- [x] Existing `NoCrossingInvariantTests` (no segment crosses a *table* interior, incl. the tight-layout adversarial case) pass unchanged — and `CrossingRouteNeverCrossesATable` pins the invariant for the crossing route itself.
- [x] No new crossings appear where the wall-avoiding route was already within the tolerance — `CheapDetourKeepsTheWallAvoidingRoute` (tolerance 10 keeps the crossing-free route) + the default 1.5 knob.
- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors, 0 warnings**; `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` → **180/180 pass**.
- [x] WORKLOG entry + promote this item to `docs/backlog/archive/` on completion.

## Status

- **State:** Completed
- **Sprint:** 2026-08-19
- **Completed:** 2026-08-19
