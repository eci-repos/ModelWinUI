# 050 — Connector Terminal Micro-Offset Straightening

## Summary

Some FK connector paths still show a tiny lateral displacement immediately
before landing on a table edge. These final approach artifacts should be
detected and straightened when doing so preserves the side-aware perpendicular
endpoint contract and does not cross table interiors or erase intentional
fan-out.

## Goals

- [x] Detect small terminal jogs near the start or end anchor where the route
      shifts a few pixels before landing on the table edge.
- [x] Replace those jogs with a cleaner straight terminal approach when the
      shortcut keeps the endpoint exact, preserves perpendicular entry/exit,
      and remains obstacle-safe.
- [x] Keep intentional shared-column fan-out intact; do not collapse distinct
      connector anchors into a single overlapping port.
- [x] Apply the cleanup to both side-aware full routing and drag re-routing,
      so XAML and Skia renderers receive the same improved route points.

## Scope

In scope: portable routing normalization in `Model.Geometry`, tests that
reproduce terminal micro-offset cases, and any required wiring in the existing
side-aware routing path.

Out of scope: draw-only rounded bend rendering, layout engine changes,
connector colors/styles, and model persistence.

## Approach / Notes

- This is a narrower follow-up to backlog `048`. The `048` cleanup catches
  general tiny doglegs, but it deliberately protects endpoint stubs. The
  remaining visual issue appears to be a terminal pattern close to the anchor:
  the route is technically perpendicular at the final segment, but the approach
  rail has a tiny lateral offset before the final landing.
- Add a terminal cleanup pass after general normalization. It should examine
  the first and last few points of a side-aware route and ask whether a direct
  replacement segment can connect the nearby route rail to the endpoint/stub
  without changing the endpoint side direction.
- Candidate replacement must be conservative:
  - exact start/end anchor points stay unchanged;
  - first and last non-zero segments still match the explicit `AnchorSide`;
  - no replacement segment crosses a table interior;
  - fan-out offsets along the table border are respected, especially for
    multiple FKs sharing the same table column;
  - if a nearby obstacle or already-routed connector made the offset necessary,
    keep the existing route.
- Relevant code:
  `src/Model.Geometry/OrthogonalRouter.cs`,
  `src/Model.Geometry/ConnectorRouteRequest.cs`,
  `src/Model.Geometry/SequentialRouter.cs`,
  `src/Model.Controls.WinUI/Controls/ModelPanelControl.xaml.cs`,
  `src/Model.Skia/Skia/Primitives/ErdComposer.cs`.

## Definition of Done

- [x] Unit tests reproduce a terminal micro-offset at the start anchor and
      verify it is straightened.
- [x] Unit tests reproduce a terminal micro-offset at the end anchor and verify
      it is straightened.
- [x] Unit tests prove the cleanup is skipped when the straight replacement
      would cross an obstacle.
- [x] Unit tests prove intentional fan-out anchors remain distinct.
- [x] Existing routing, no-table-crossing, rounded-polyline, Skia, graph, and
      layout tests still pass.
- [x] Solution build succeeds with 0 errors and no new warnings.

## Status

- **State:** Completed
- **Sprint:** 050 Connector Terminal Micro-Offset Straightening
- **Completed:** 2026-08-22
