# 052 - Connector Nudge Diagnostics and Safe Straightening

## Summary

Some FK connector paths still show small visual nudges near a table edge even after terminal micro-offset cleanup. This work should capture the actual rendered route geometry, classify whether each nudge is removable or intentionally produced by routing constraints, and then straighten only the safe removable cases.

## Goals

- [x] Add diagnostics that can capture route identity, anchors, anchor sides, route points, nearby obstacles, and fan-out/wall context for suspicious near-straight connector nudges.
- [x] Identify the remaining nudge patterns from final rendered route points instead of guessing from synthetic route cases only.
- [x] Classify each captured case as removable grid/stitch artifact, endpoint-alignment requirement, obstacle avoidance, connector-wall avoidance, or endpoint-side preservation.
- [x] Add focused regression tests from captured point-list patterns before changing cleanup behavior.
- [x] Implement targeted straightening rules for cases proven safe, preserving exact endpoints and perpendicular table entry/exit.

## Scope

In scope:

- Portable router diagnostics and optional debug logging/export for selected or suspicious FK connector routes.
- Tests and cleanup changes in the geometry routing layer.
- Renderer wiring only if needed to identify selected/visible connectors and surface the diagnostic output.
- A final result note that states which observed cases were fixed and which were intentionally retained.

Out of scope:

- Replacing the orthogonal grid/router design with a new routing algorithm.
- Removing fan-out behavior that keeps multiple relationships distinguishable.
- Disabling table, connector, or collapsed-group obstacle avoidance.
- Changing draw-only rounded bend rendering or connector hit testing unless diagnostics prove it is directly involved.
- Persisting diagnostic output as model data.

## Approach / Notes

- Start from the current route pipeline:
  - `src/Model.Geometry/OrthogonalRouter.cs`
  - `src/Model.Geometry/ConnectorRouteRequest.cs`
  - `src/Model.Geometry/SequentialRouter.cs`
  - `src/Model.Controls.WinUI/Controls/ModelPanelControl.xaml.cs`
  - `src/Model.Skia/Skia/Primitives/ErdComposer.cs`
  - `tests/ModelConsole.Tests/OrthogonalRouterTests.cs`
- Capture the final route points after all cleanup passes, not only the raw pathfinder output.
- Flag suspicious candidates where a long straight route has a short lateral shift close to the start or end anchor.
- Keep side-aware guarantees from backlog `048` and the terminal normalization safety rules from backlog `050`.
- Treat a nudge as removable only when the replacement path is axis-aligned, obstacle-safe, preserves the requested endpoint side, and does not collapse distinct fan-out lanes.
- Be explicit when a remaining nudge is an honest tradeoff of the current router rather than a bug that can be removed safely.

## Definition of Done

- [x] The app or tests can produce enough route diagnostics to inspect the remaining visible near-straight nudges.
- [x] Regression tests cover at least one captured removable case and at least one intentionally retained case.
- [x] Safe removable nudges are straightened without changing exact connector endpoints.
- [x] Perpendicular start/end behavior remains covered by tests.
- [x] Obstacle and fan-out preservation remain covered by tests.
- [x] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` passes.
- [x] `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` passes or any failure is documented exactly.
- [x] `docs/WORKLOG.md`, `docs/sprints/CURRENT.md`, and backlog archive state are updated according to `AGENTS.md`.

## Status

- **State:** Archived
- **Sprint:** 052 Connector Nudge Diagnostics and Safe Straightening
- **Completed:** 2026-08-22
