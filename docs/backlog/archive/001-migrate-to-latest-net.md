# 001 — Migrate solution from .NET 6 to .NET 10 (latest LTS)

## Summary

The project targets `net6.0-windows10.0.19041.0`, which is out of support (EOL since Nov 2024) and emits `NETSDK1138` on every build. Migrate the solution to **.NET 10** — the latest LTS release (released Nov 2025, supported until Nov 2028).

## Goals

- [ ] Target the latest supported .NET LTS (`.NET 10`, `net10.0-windows...`).
- [ ] Update NuGet packages to versions compatible with .NET 10 (Windows App SDK, SkiaSharp, CommunityToolkit.Mvvm).
- [ ] Build succeeds with no `NETSDK1138` warning.
- [ ] App launches and still draws the sample model (unpackaged run).

## Scope

**In scope:**
- `TargetFramework` change in `src/Model.WinUI.Console/ModelWinUI.csproj`.
- Package version updates required for .NET 10 compatibility.
- Build + run verification.

**Out of scope:**
- The Uno/WebAssembly sibling (not in this repo).
- Any feature work or graphics changes.
- Migrating the `Skia` stack separately — it moves with the same project.

## Approach / Notes

- Change `TargetFramework` to `net10.0-windows10.0.19041.0` (verify the correct Windows TFM for .NET 10; the min platform version `10.0.17763.0` can stay).
- Current packages (as of migration start):
  - `Microsoft.WindowsAppSDK` 1.3.230502000 — likely needs a major bump; verify the version compatible with .NET 10.
  - `SkiaSharp.Views.WinUI` 2.88.3 — verify compatibility.
  - `CommunityToolkit.Mvvm` 8.2.1 — verify compatibility.
- Build with `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` (a platform is required; AnyCPU fails for this packaged WinUI app).
- Run via the **ModelWinUI (Unpackaged)** launch profile.
- Watch for breaking changes in Windows App SDK between 1.3 and the target version (XAML/`Window` API changes).

## Definition of Done

- [x] `dotnet build ... -p:Platform=x64` succeeds with **0 errors** and no `NETSDK1138`.
- [x] App launches unpackaged and renders the sample tables + connectors (window "EDAM Studio" created and responding; drawing runs without crashing).
- [x] `docs/WORKLOG.md` updated; sprint record promoted to `docs/sprints/archive/`.

## Status

- **State:** Completed / Archived
- **Sprint:** `docs/sprints/archive/sprint-2026-08-15-net10-migration.md`
- **Completed:** 2026-08-15
