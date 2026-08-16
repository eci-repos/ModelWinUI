# 007 — Unit tests + A* FK routing + 50-table public-safety schema

## Summary

EDAM Studio's main purpose is: given a database schema, render the tables on a canvas. FK connectors were drawn at hardcoded coordinates with a fixed 3-segment orthogonal path and **zero obstacle avoidance** (they could cross over tables), `ConstraintInfo` could not express which table a FK references, and the repo had **no unit tests**. This sprint formalizes testing and makes the app actually render a realistic schema: **50 tables** in the public-safety / criminal-justice domain, with every parent–child FK drawn as a connector that routes around tables.

## Goals

- [ ] First unit-test project (xUnit) over the portable ModelGraphLibrary.
- [ ] FK connectors route around tables — A* grid pathfinding, pure, deterministic, unit-testable.
- [ ] 50-table public-safety / criminal-justice sample schema + all FK connectors rendered in the app.

## Scope

**In scope:**
- `ConstraintInfo` FK parent references (`ReferencedTableName` / `ReferencedColumnName`, nullable).
- Pure `ModelConsole.Graph` modules in ModelGraphLibrary: `Geometry`, `FkRelation` + `FkEdgeExtractor`, `TableLayoutEngine`, `OrthogonalRouter`.
- `PublicSafetySchema` fixture (50 tables, 74 FK edges) in `ModelConsole.ModelData`.
- `tests/ModelGraphLibrary.Tests` — xUnit test project (net10.0), added to `ModelWinUI.sln`.
- App integration: `GlOrthoPath.DrawRouted`, `IConnectorFactory.CreateRouted`, `Table.ComputedWidth/Height` + `GetRowCenterY`, `IModelDataProvider.GetPublicSafetyTables`, `ModelPanelControl` rewrite (measure → layout → draw → route) with the Canvas wrapped in a ScrollViewer.

**Out of scope:**
- Corner rounding on routed connectors (rounding can re-intersect obstacles in tight gaps) — clean follow-up.
- Dragging/re-layout of tables (`GetRowCenterY` is static-layout correct).
- Namespace reorganization of ModelGraphLibrary.
- Wiring `SkiaPanelControl` into `MainWindow`.

## Approach / Notes

- **Routing:** A* grid pathfinding over obstacle-inflated grid cells; Manhattan heuristic, 4-directional neighbors, deterministic tie-breaking; direct-path (HV/VH) preference; outward stubs snapped to grid-cell centers so stitching stays axis-aligned; collinear simplification; unreachable → orthogonal Z-path fallback.
- **Anchor rule:** connectors attach at the **FK column row center** on the child's right edge and the referenced column row center on the parent's left edge.
- **ModelGraphLibrary stays plain `net10.0`** — no Windows.Foundation anywhere in the new code (the unit-test target / Uno sibling).
- **`ActualWidth` pitfall:** `GlRectangle.Width/Height` return 0 pre-layout — the app uses `Table.ComputedWidth/ComputedHeight`.
- **No new DI registrations:** Graph modules are pure static library calls; existing singletons (`IConnectorFactory`, `IModelDataProvider`) gained members only.
- App router options: `GridSize=16, ObstacleMargin=14, StubLength=32`.

## Definition of Done

- [ ] `dotnet build src/ModelGraphLibrary/ModelGraphLibrary.csproj -c Debug` → 0 errors.
- [ ] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all green (32 tests).
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors.
- [ ] App launches unpackaged; window "EDAM Studio" responding; 50 tables + ~74 FK connectors render without crashing.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/archive/sprint-2026-08-15-tests-and-fk-routing.md`
- **Completed:** 2026-08-15
