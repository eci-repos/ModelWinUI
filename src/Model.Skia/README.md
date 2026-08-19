# Model.Console.Skia

Portable SkiaSharp drawing stack. `GlFrame` wraps the `SKCanvas` and manages
the coordinate system; the generic primitives (`Table`, `Connector`,
`RectangleHalf`) stroke routed geometry; `ErdComposer.Compose` is the reusable
composition API — "define and draw an ERD by writing code" — running
layout → edge extraction → anchoring → routing to produce an `ErdDiagram`
(`Layout` / `Edges` / `Routes` / `Issues`). `ISkiaTableFactory` /
`ISkiaConnectorFactory` are the DI-wired factory contracts. This is the stack
intended for the Uno/WebAssembly sibling; keep it free of WinUI dependencies.

**Dependencies:** `SkiaSharp`, `Model.Graph`, `Model.Data`, `Model.Geometry`.

**Usage**

```csharp
using var surface = SKSurface.Create(...);
var frame = new GlFrame(surface);
Connector.Draw(frame, points);           // strokes a routed polyline
```
