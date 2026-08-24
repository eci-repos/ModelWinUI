# 054 — Save Model and Export Diagram Files

## Summary

Add file output options for the current model and diagram: editable Model JSON, full-diagram PNG, and full-diagram PDF. Model JSON preserves the live editable data; PNG/PDF are view/export artifacts generated from the rendered diagram.

## Goals

- [ ] Add **Save As Model JSON...** for the live editable model.
- [ ] Add **Export PNG...** for a full-diagram raster image.
- [ ] Add **Export PDF...** for a full-diagram document.
- [ ] Prefer one shared export composition path for PNG and PDF so the two outputs match.

## Scope

In scope:

- File menu commands for Model JSON, PNG, and PDF output.
- Model JSON serialization using the existing model/data JSON shape so saved files can be reopened and edited.
- Full-diagram PNG export, not current viewport export.
- Full-diagram PDF export, not current viewport export.
- Export sizing based on the composed diagram bounds with reasonable padding and current visual settings where practical.
- Diagnostics/log messages for successful exports and user-visible failures.

Out of scope:

- Persisting app workspace state such as pan, zoom, selected layout, theme, notation, collapsed groups, or dirty state.
- A full **Save** command that overwrites the current file path; start with **Save As** until file path and dirty tracking are designed.
- Exporting only the visible viewport.
- Batch export, print integration, or multi-page PDF pagination.
- Changing the canonical model schema.

## Approach / Notes

- Implement in this order: Model JSON, PNG, PDF.
- For Model JSON, reuse the existing serialization/load model conventions instead of inventing a new `.edam` project format.
- For PNG/PDF, prefer the Skia composition path because it is portable, deterministic, and already intended to be renderer-independent of the live WinUI canvas.
- Use the full diagram bounds from the composed `ErdDiagram` and add stable padding so exports do not clip endpoint markers, labels, selection widths, or future Crow's Foot glyphs.
- Consider whether exports should include transient UI state such as hover/selection emphasis. Default should likely be no hover state, with selected connector styling included only if the current renderer model already treats it as part of the diagram view.
- Keep PlantUML export separate; this item is about saving editable model JSON and exporting rendered visual artifacts.

## Definition of Done

- [ ] **File -> Save As Model JSON...** writes a model file that can be loaded back with the same tables, columns, constraints, tags, metadata, enumerations, and provenance supported by the current model format.
- [ ] **File -> Export PNG...** writes a full-diagram PNG with no clipping and a transparent or documented background behavior.
- [ ] **File -> Export PDF...** writes a full-diagram PDF with matching diagram bounds and readable vector/raster output.
- [ ] Canceled file pickers do not log errors or mutate state.
- [ ] Export failures are reported plainly through diagnostics or UI feedback.
- [ ] Tests cover model save round-trip and the portable export path where practical.
- [ ] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` passes.
- [ ] `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` passes or any failure is documented faithfully.

## Status

- **State:** Planned
- **Sprint:** -
- **Completed:** -
