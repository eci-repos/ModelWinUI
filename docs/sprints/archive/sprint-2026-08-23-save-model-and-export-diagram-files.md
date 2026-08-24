# Sprint - Current

## Dates

- **Start:** 2026-08-23
- **End:** 2026-08-23

## Scope

Backlog items in this sprint:

- `054-save-model-and-export-diagram-files.md` — Save As Model JSON, Export PNG, Export PDF.

## Execution Log

- **Milestone 1 — Save As Model JSON:** added `File → Save As Model JSON…` writing the live editable model in the container JSON shape `File → Open Model…` reads; canceled pickers are silent no-ops, write failures surface in a dialog, success logs through diagnostics. Added `FullPublicSafetyFixtureRoundTrips` (the real 50-table/74-FK shipped model survives a `ToJson → LoadJson` pass).
- **Milestone 2 — Export PNG:** added the portable `ErdExporter` in `Model.Skia` (compose via `ErdComposer`, render at full size = content bounds + stable padding, no hover/selection emphasis, honoring notation/layout/visibility/collapse/theme/background). `GlFrame` gained an `SKCanvas` constructor overload so the same draw code targets a raster surface and a PDF page. Added `File → Export PNG…` + `ErdExporterTests` (non-blank, sized to bounds + padding, empty for empty model).
- **Milestone 3 — Export PDF:** added `File → Export PDF…` writing a single-page PDF through the same shared composition path (page sized to the same bounds + padding, so PNG and PDF match). Added PDF tests (readable `%PDF` header, `MediaBox` matches diagram size).

## Results

- **Completed:** 054 — Save As Model JSON, Export PNG, Export PDF.
- **Deferred:** workspace-state persistence, dirty tracking, a full Save command, viewport-only export, batch/print/multi-page pagination, and schema changes (all explicitly out of scope in 054).
- **Notes:** `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` → **328/328 pass** (was 322; +6 new tests). `dotnet build ModelWinUI.sln -c Debug -p:Platform=x64` → **0 errors / 0 warnings**. Manual pass (open a sample, Save As Model JSON and reopen, Export PNG/PDF and open the files) needs a human run.
