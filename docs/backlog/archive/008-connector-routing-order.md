# 008 — Connector routing: logical order, port-based anchors, endpoint markers

## Summary

The 50-table schema renders, but the ~74 FK connectors are unreadable: lines cross each other with no logical order. Each edge is routed **independently** (no edge knows about the others), every edge leaves the child's **right** edge and enters the parent's **left** edge regardless of relative position, and multiple FKs sharing one column overlap into a blob. This sprint makes connectors readable: sequential routing (already-drawn edges become obstacles), per-edge anchor-side selection, port fan-out, ~20 px stubs, and a visible circle at each endpoint.

## Goals

- [ ] Connectors no longer cross each other — each new edge routes around the edges already drawn (sequential routing).
- [ ] Anchor side chosen from relative position (nearest side facing the target) — a straight line when the two tables are side-by-side.
- [ ] Multiple FKs on the same column fan out with small offsets instead of overlapping.
- [ ] Stub length ~20 px (depart straight out from the exact column, then turn).
- [ ] Visible circle marker at each connector start/end anchor.

## Scope

**In scope:**
- Sequential routing: route edges in a deterministic order; feed each routed polyline back in as an obstacle (inflated) so later edges avoid crossing it. Lives in the app's `ModelPanelControl` draw loop or as a new pure helper in `ModelConsole.Graph` (prefer the latter — unit-testable).
- Per-edge anchor-side selection: replace the hardcoded right→left anchors (`ModelPanelControl.xaml.cs:140-143`) with a side chosen from the child/parent relative position.
- Port fan-out: group edges by source column; offset the anchor points by a few pixels so shared-column edges separate.
- `StubLength` → 20 (app router options; keep the router default configurable).
- Endpoint markers: a small circle (ellipse) at each anchor — new `GlEllipse` primitive (or equivalent) in the XAML `Graphics` stack, drawn at the routed path's first/last point.
- Unit tests for the new routing behavior (sequential ordering, anchor-side choice, fan-out) in `tests/ModelGraphLibrary.Tests`.
- Update `docs/WORKLOG.md`, `docs/codebase-functionality-map.md`, `CLAUDE.md` as needed.

**Out of scope:**
- Corner rounding on routed connectors (still deferred — rounding can re-intersect obstacles in tight gaps).
- Dragging / re-layout of tables (`GetRowCenterY` is static-layout correct).
- Edge bundling / curved (non-orthogonal) connectors.
- Wiring `SkiaPanelControl` into `MainWindow`.

## Approach / Notes

- **The crossing problem is edge-ordering, not obstacle avoidance.** `OrthogonalRouter` already does A* grid pathfinding with obstacle inflation, outward stubs, and collinear simplification — the shape the user wants (depart from the exact column, ~20 px out, turn, align, bend in) is already produced. The chaos comes from each of the 74 edges being routed with no knowledge of the others.
- **Sequential routing:** deterministic edge order (e.g. by child table, then column index), and for each edge add the previous edges' polylines to the obstacle list (inflated by a small margin) before routing. This is what gives "logical order."
- **Anchor side:** currently `start` is always the child's right edge and `end` always the parent's left edge. Choose the departure side from the relative position (e.g. parent to the left ⇒ leave the child's left edge; parent above ⇒ leave the top edge). The direct-path preference in the router then yields the straight side-by-side case for free.
- **Port fan-out:** when several FKs share a column, offset each anchor along the column by a few pixels so the stubs separate before turning.
- **Endpoint circles:** no `GlEllipse` exists yet — add a small ellipse primitive (mirrors `GlRectangle`) and draw one at each anchor. Keep it simple (no grips/handles for now).
- **Router options:** app currently uses `GridSize=16, ObstacleMargin=14, StubLength=32`. Change `StubLength` to 20; consider whether `ObstacleMargin` needs to shrink so sequential edges can pass through tight gaps.
- **Pure vs app code:** keep as much as possible in `ModelConsole.Graph` (pure, deterministic, unit-testable) — e.g. a `SequentialRouter` or an ordering/fan-out helper — and keep `ModelPanelControl` thin.

## Definition of Done

- [ ] `dotnet build src/ModelGraphLibrary/ModelGraphLibrary.csproj -c Debug` → 0 errors.
- [ ] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all green (existing 32 + new tests).
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [ ] App launches unpackaged; connectors no longer cross tables **or each other**; side-by-side tables get a straight line; shared-column FKs fan out; a visible circle marks each endpoint.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/CURRENT.md` (2026-08-16 connector routing)
- **Completed:** 2026-08-16
