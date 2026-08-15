# Sprint 2026-08-15 — .NET 10 Migration

> Executed copy of the sprint. Backlog item: `docs/backlog/archive/001-migrate-to-latest-net.md`.

## Dates

- **Start:** 2026-08-15
- **End:** 2026-08-15

## Scope

- [x] `001` — Migrate solution from .NET 6 to .NET 10 (latest LTS)

## Execution Log

- 2026-08-15 — Sprint defined from backlog item `001`. Target confirmed: .NET 10 (LTS, supported until Nov 2028).
- 2026-08-15 — Updated `TargetFramework` to `net10.0-windows10.0.19041.0`.
- 2026-08-15 — Updated RuntimeIdentifiers `win10-*` → `win-*` (the `win10-` prefix was removed in .NET 8+; NETSDK1083 otherwise).
- 2026-08-15 — Updated packages: Microsoft.WindowsAppSDK 1.3.230502000 → **2.4.0**, SkiaSharp.Views.WinUI 2.88.3 → **4.151.1**, CommunityToolkit.Mvvm 8.2.1 → **8.4.2**, Microsoft.Windows.SDK.BuildTools 10.0.22621.755 → **10.0.28000.2526**.
- 2026-08-15 — Fixed SkiaSharp 4.x breaking changes in the `Skia` stack (text rendering moved from `SKPaint` to `SKFont`): `GlFrame.DefaultFont` is now an `SKFont` with a new `DefaultTextPaint`; `GlText.DrawText`, `Table.SetFont`, `Table.AddColumns` (MeasureText), and the `DrawText` calls updated.
- 2026-08-15 — Fixed `CS8981` (`mvvm` alias → `Mvvm`) introduced by .NET 9+ analyzer.
- 2026-08-15 — Added `<WindowsPackageType>None</WindowsPackageType>` so the app runs unpackaged (the "ModelWinUI (Unpackaged)" launch profile / direct exe launch); without it the WinAppSDK auto-initializer threw `COMException 0x80040154 Class not registered`.
- 2026-08-15 — Build verified: `dotnet build ./ModelWinUI.csproj -c Debug -p:Platform=x64` → **0 errors**, `NETSDK1138` gone.
- 2026-08-15 — App run verified: `ModelWinUI.exe` launches unpackaged, window "EDAM Studio" is created and responds; the sample-model drawing runs in `ModelPanelControl`'s constructor without crashing. (Rendered output not visually inspected — screenshot declined.)

## Results

- **Completed:** `001`
- **Deferred:** none
- **Notes:**
  - Remaining build warning: `NETSDK1198` — csproj references `PublishProfile=win10-$(Platform).pubxml` but no `.pubxml` files exist. Pre-existing; candidates for a future backlog item.
  - The XAML errors seen mid-migration ("Unknown type 'ModelPanelControl'", "Cannot resolve DataType diag:IMessageLogEntry") were cascades from the C# compile failure and resolved once it was fixed.
