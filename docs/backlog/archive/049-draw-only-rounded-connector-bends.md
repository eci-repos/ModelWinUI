# 049 — Draw-Only Rounded Connector Bends

## Summary

FK connector turns should render with a small rounded radius so relationship
paths are easier to scan. Rounding must be visual only: the router, hit testing,
labels, and obstacle calculations continue to use the original orthogonal
polyline.

## Goals

- [x] Render routed connector bends with a small capped radius in the XAML
      graphics stack.
- [x] Render the same rounded-bend appearance in the Skia stack.
- [x] Keep endpoint anchors exact and preserve endpoint marker placement.
- [x] Leave the stored route points unchanged for routing, connector-wall
      obstacles, hover hit testing, and UML label placement.

## Scope

In scope: routed connector rendering in `Model.Graphics.WinUI` and
`Model.Skia`, plus focused tests or helper-level coverage where practical.

Out of scope: changing route geometry, changing connector routing behavior,
changing endpoint perpendicularity, changing connector labels, and changing
model persistence.

## Approach / Notes

- Implement rounding as path construction, not router mutation. For each
  interior bend, shorten the incoming and outgoing straight segments by
  `radius = min(configuredRadius, previousSegmentLength / 2,
  nextSegmentLength / 2)`, draw a line to the first tangent point, then draw a
  quadratic curve through the original corner to the second tangent point.
- XAML routed mode currently builds only `LineSegment`s in
  `GlOrthoPath.DrawRouted`; add a rounded routed-path helper that emits
  `QuadraticBezierSegment`s for eligible bends.
- Skia routed connectors currently use `SKPathBuilder.LineTo`; add equivalent
  `QuadTo` construction in `ModelConsole.Skia.Primitives.Connector`.
- Use a conservative default radius, likely 6-8 px. If adjacent segments are
  short, the radius clamps down automatically; zero/near-zero segments should
  fall back to straight lines.
- Leave `RouteHitTest.Nearest`, `OrthogonalRouter.PolylineLength`, connector
  wall generation, and UML midpoint labels on the original point list.

## Definition of Done

- [x] XAML routed connectors draw rounded bends without changing `Points`.
- [x] Skia routed connectors draw matching rounded bends without changing
      `Points`.
- [x] Straight connectors remain visually straight.
- [x] Very short connector segments clamp the radius instead of overshooting
      or folding back.
- [x] Endpoint circles and UML labels remain positioned from the original
      route points.
- [x] Existing geometry, Skia, and graph tests still pass; solution build
      succeeds.

## Status

- **State:** Completed
- **Sprint:** 049 Draw-Only Rounded Connector Bends
- **Completed:** 2026-08-22
