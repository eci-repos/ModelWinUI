# Model.Console.Controls.WinUI

Reusable WinUI 3 (Windows App SDK) ERD editor controls. Drop an
`ModelEditorControl` into any host window and it renders an editable,
routable ERD of the sample public-safety schema immediately: the XAML
drawing path (`ModelPanelControl` over `Model.Graphics.WinUI`'s `Gl*`
stack) and the Skia renderer (`SkiaPanelControl`) with the slim renderer
bar, model explorer, diagnostics log, and entity inspector that the EDAM
Studio app composes. Ships the app-owned plumbing the controls resolve from
DI — `IModelDataProvider`/`ModelDataProvider`, the `DiagnosticsLogViewModel`,
its `ObservableObject` base, and the `DataElementName` constants — plus the
`Themes/ControlTheme.xaml` palette the control headers reference.

WinUI-bound — consumers must reference `Microsoft.WindowsAppSDK`.

**Dependencies:** `Model.Graphics.WinUI`, `Model.Skia`, `Model.Graph`,
`Model.Data`, `Model.Geometry`, `Model.Diagnostics`.

**The DI contract.** The controls are XAML-instantiated, so they resolve
their services through `Ioc.Default` (CommunityToolkit) at construction. A
host must register the same set the app registers before creating the
controls:

```csharp
services.AddSingleton<ILogService, LogService>();
services.AddSingleton<IModelDataProvider, ModelDataProvider>();
services.AddSingleton<ITableFactory, TableFactory>();
services.AddSingleton<IConnectorFactory, ConnectorFactory>();
services.AddSingleton<IRectangleFactory, RectangleFactory>();
services.AddSingleton<IBoxFactory, BoxFactory>();
services.AddSingleton<ISkiaTableFactory, SkiaTableFactory>();
services.AddSingleton<ISkiaConnectorFactory, SkiaConnectorFactory>();
services.AddSingleton<ISkiaBoxFactory, SkiaBoxFactory>();
services.AddTransient<IGlModel, GlModel>();
services.AddSingleton<DiagnosticsLogViewModel>();
Ioc.Default.ConfigureServices(services);
```

**Usage**

```csharp
// In any host window's XAML
// xmlns:ct="using:ModelConsole.Controls"
<ct:ModelEditorControl x:Name="Editor" />
<ct:SkiaPanelControl   x:Name="Skia" Visibility="Collapsed" />

// Replace the default sample model after load:
//   Editor.SetModel(tables, enumerations, provenance, metadata);
//   Skia.SetModel(tables);
```
