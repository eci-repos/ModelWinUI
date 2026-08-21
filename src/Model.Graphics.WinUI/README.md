# Model.Console.Graphics.WinUI

Reusable WinUI 3 (Windows App SDK) XAML drawing stack. `GlContext` wraps a
`Canvas` and owns all pointer handling (press/move/release/capture), selection,
grabbers, hover tracking, and the drag/click/pan events; `GlObject` is the base
for every drawable shape object (`GlRectangle`, `GlOrthoPath`, `GlEllipse`,
`GlTextBox`, ...) with `DeltaMove`/`Move`/`PointerEvent`/`Reshape`/`Selected`
and a `Node` surface over the live model object; `GlOrtho/GlOrthoPath` draws
orthogonal rounded-edge connector lines with reshape grips; `Graphics/Primitives`
renders an ERD table (`Table`, `TableRowPanel`) from a `TableInfo` and a
collapsed group's UML package box (`GroupBox`, name + `<<package>>` + count,
per-group tint). Ships the DI-wired XAML factory contracts (`ITableFactory` /
`IConnectorFactory` / `IRectangleFactory` / `IBoxFactory`), mirroring Model.Skia's
`ISkiaTableFactory`. WinUI-bound — consumers must reference `Microsoft.WindowsAppSDK`.

**Dependencies:** `Model.Data`, `Model.Graph`, `Model.Geometry`, `Model.Diagnostics`.

**Usage**

```csharp
// Host a GlContext over a Canvas and draw a table (DI resolves the factories)
var context = new GlContext(canvas, logService);
context.Reset();
var table = GlObjects.Table(...);   // shape-level object over a TableInfo
```
