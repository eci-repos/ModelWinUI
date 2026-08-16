# Sprint 2026-08-15 — Unit tests & FK routing

> Executed copy of the sprint. Backlog item: `docs/backlog/archive/007-unit-tests-and-fk-routing.md`.

## Dates

- **Start:** 2026-08-15
- **End:** 2026-08-15

## Scope

- [x] `007` — Unit tests + A* FK routing + 50-table public-safety schema

## Execution Log

- 2026-08-15 — Sprint defined from backlog item `007`. Routing approach confirmed with user: **A* grid pathfinding** (pure, deterministic, unit-testable); `ModelPanelControl` replaces the 2-table sample with the full 50-table schema + FK connectors.
- 2026-08-15 — Extended `ConstraintInfo` with nullable `ReferencedTableName` / `ReferencedColumnName` (backward compatible; null column ⇒ parent PK default).
- 2026-08-15 — Added pure `ModelConsole.Graph` modules to ModelGraphLibrary: `Geometry` (`Point2`/`Rect2` + strict-interior segment test), `FkRelation` + `FkEdgeExtractor` (74 edges, deterministic, issue reporting), `TableLayoutEngine` (row-major grid), `OrthogonalRouter` (A* + obstacle inflation + cell-snapped stubs + collinear simplification + Z-path fallback).
- 2026-08-15 — Authored `PublicSafetySchema` fixture — exactly 50 tables, 74 FK edges across 8 domain areas; SentenceCondition→Sentence deliberately omits `ReferencedColumnName` to exercise the PK-default rule.
- 2026-08-15 — Created xUnit test project `tests/ModelGraphLibrary.Tests` (net10.0, refs library; Microsoft.NET.Test.Sdk 17.11.1, xunit 2.9.2, xunit.runner.visualstudio 2.8.2), added to `ModelWinUI.sln`; 32 tests across 4 files.
- 2026-08-15 — App integration: `GlOrthoPath.DrawRouted` (absolute-point `PathGeometry`), `IConnectorFactory.CreateRouted`, `Table.ComputedWidth/Height` + `GetRowCenterY`, `IModelDataProvider.GetPublicSafetyTables`, `ModelPanelControl` rewrite with the Canvas wrapped in a ScrollViewer.
- 2026-08-15 — Fixed build error CS0246 (`IReadOnlyList<>`) — added `using System.Collections.Generic;` to `IConnectorFactory.cs` / `ConnectorFactory.cs`.
- 2026-08-15 — Verified: library builds 0/0; **32/32 tests pass**; full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; app launches unpackaged, window "EDAM Studio" responding, 50 tables + ~74 routed FK connectors render without crashing. (Screenshot declined.)

## Results

- **Completed:** `007`
- **Deferred:** none
- **Notes:**
  - Router options used by the app: `GridSize=16, ObstacleMargin=14, StubLength=32`.
  - Corner rounding on routed connectors deferred (rounding can re-intersect obstacles in tight gaps) — clean follow-up.
  - `GetRowCenterY` is static-layout correct; dragging tables later is out of scope.
  - Legacy `GetPersonTable` / `GetPersonNameTable` fixtures kept intact (still used by `SkiaPanelControl`).
