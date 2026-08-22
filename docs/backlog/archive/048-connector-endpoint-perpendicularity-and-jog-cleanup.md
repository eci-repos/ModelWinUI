# 048 — Connector Endpoint Perpendicularity and Jog Cleanup

## Summary

FK connector routes should leave and enter table borders cleanly: the first
segment must be perpendicular to the child table edge, and the final segment
must be perpendicular to the parent table edge. Routes should also collapse
tiny visual jogs that are artifacts of grid snapping when a straight segment is
clear.

## Goals

- [x] Preserve each connector's resolved anchor side through the routing
      request instead of relying only on start/end points.
- [x] Guarantee a short perpendicular departure stub from the child table edge
      and a short perpendicular arrival stub into the parent table edge.
- [x] Keep shared-column fan-out behavior while ensuring the fanned anchor
      still departs perpendicular to the table border.
- [x] Add route normalization that removes duplicate points, near-collinear
      points, and tiny doglegs when the simplified segment does not cross a
      table interior.

## Scope

In scope: portable geometry changes in `Model.Geometry`, route request plumbing
from the XAML and Skia composition paths, and pure tests for the routing
contract.

Out of scope: rounded bend drawing, connector color/style changes, layout
engine changes, and persistence/model-format changes.

## Approach / Notes

- Current anchor resolution in `ConnectorAnchors.Resolve` returns
  `AnchorSide`, but `SequentialRouter.RouteAll` only receives `(Start, End)`.
  Introduce a small route request type, or overloads, that carry
  `StartSide`/`EndSide` into `OrthogonalRouter`.
- Build mandatory endpoint stubs from the explicit sides:
  right = `(+1, 0)`, left = `(-1, 0)`, bottom = `(0, +1)`, top = `(0, -1)`.
  Route between the two stub points, then stitch back to the exact anchors.
- The direct HV/VH fast path should still be allowed only when it respects the
  endpoint-side invariant. A perfectly horizontal right-to-left connector can
  remain straight; a tangent edge-hugging connector should not.
- Route cleanup must be obstacle-aware. Only remove a tiny dogleg when replacing
  it with the straight segment keeps the existing no-table-interior-crossing
  invariant.
- Relevant code:
  `src/Model.Geometry/ConnectorAnchors.cs`,
  `src/Model.Geometry/OrthogonalRouter.cs`,
  `src/Model.Geometry/SequentialRouter.cs`,
  `src/Model.Controls.WinUI/Controls/ModelPanelControl.xaml.cs`,
  `src/Model.Skia/Skia/Primitives/ErdComposer.cs`.

## Definition of Done

- [x] Unit tests prove the first and last route segments are perpendicular to
      the explicit anchor sides for left/right/top/bottom cases.
- [x] Unit tests prove fanned-out anchors still leave/enter perpendicular to
      their table borders.
- [x] Unit tests cover at least one tiny dogleg that is safely collapsed and
      at least one that is retained because collapsing it would cross an
      obstacle.
- [x] Existing no-table-crossing, sequential-routing, optimal-routing, and
      layout tests still pass.
- [x] Both XAML and Skia composition paths use the same side-aware routing
      contract.

## Status

- **State:** Completed
- **Sprint:** 048 Connector Endpoint Perpendicularity and Jog Cleanup
- **Completed:** 2026-08-22
