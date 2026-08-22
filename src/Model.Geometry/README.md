# Model.Console.Geometry

Portable 2D geometry for connector drawing — no `Windows.Foundation`, no
SkiaSharp, no UI framework. `Point2` / `Rect2` are plain structs with
strict-interior segment and rectangle tests; `OrthogonalRouter` is an A*
grid pathfinder that routes orthogonal polylines around rectangular
obstacles; `ConnectorAnchors` picks departure sides and fans shared-column
anchors; `ConnectorRouteRequest` carries those sides through routing so
connectors leave/enter table borders perpendicularly; `SequentialRouter.RouteAll`
routes many edges one at a time, feeding each route back in as a thin obstacle;
`RoundedPolyline` emits draw-only rounded-bend commands; `RouteHitTest.Nearest`
finds the polyline closest to a pointer for hover hit-testing.

**Dependencies:** none.

**Usage**

```csharp
var bounds = new Rect2(0, 0, 400, 400);
var route = OrthogonalRouter.Route(
   new Point2(0, 100), new Point2(300, 100),
   obstacles, bounds, Options);   // IReadOnlyList<Point2>
var index = RouteHitTest.Nearest(routes, pointer, maxDistance);
```
