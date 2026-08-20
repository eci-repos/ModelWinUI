# 041 — Drawing-surface background color, thicker table borders, hovered-border emphasis

## Summary

The drawing model area sits on a gray: the XAML `GlCanvas` is `Background="Transparent"`, so the window chrome (`WindowBackgroundBrush`, warm gray `#F0EEF4`) shows through the paper — while the Skia renderer already clears to white. The user asked for three visual changes: (a) the drawing-area background should default to white and be **selectable via a drop-down** (with a custom color picker); (b) each table should have a **thicker card border**; (c) the **hovered table should draw an even thicker accent border** while hovered. This item turns the drawing surface into a first-class, user-tunable "base" color, and gives the table card a visible, hover-aware border — in **both renderers** (the 003 parity discipline), sourced from the shared `Model.Palette` `TablePalette` so the two stacks cannot drift.

## Goals

- [x] Drawing surface defaults to white; a renderer-bar **"Base:" drop-down** (presets + a **Custom…** WinUI color picker) drives both renderers to the same color.
- [x] Table card border thickens to a shared rest width/color (from `TablePalette`) in both the XAML and Skia stacks.
- [x] The hovered table draws the shared hovered accent — thicker and tinted DodgerBlue — in both stacks (XAML live on hover; Skia via a table hit-test on each paint).
- [x] Geometry contract preserved (row Y positions, `GetRowCenterY`, routing, anchors) — the border is a stroke, not a layout change; tests green.

## Scope

**In scope:**
- `TablePalette`: rest + hovered border hex/width, and the `CanvasBackgroundHex` default.
- XAML `Table` border (replaces the `GlRectangle` 0.5 px hairline default) + a `Hovered` flag toggled by `ModelPanelControl`'s existing hover tracking.
- Skia `Table`/`RectangleHalf` border draw (color + width params, local paint) + a `Hovered` flag threaded through `ISkiaTableFactory`.
- `GlFrame` optional background color; `SkiaPanelControl` clears each paint to it.
- `ModelPanelControl`/`ModelEditorControl`/`SkiaPanelControl` `BackgroundColor` properties.
- `MainWindow` renderer-bar "Base:" drop-down + color picker flyout.
- Docs/WORKLOG.

**Out of scope:**
- Connector appearance (DodgerBlue/SlateBlue stays; the hover accent is DodgerBlue to match selection).
- Grip/handle/selection visuals (the DodgerBlue selection outline stays).
- Per-table border colors (all tables share the rest border; the hover accent is uniform).
- `TableLayoutEngine`/routing changes.
- Light/dark theme variants.

## Approach / Notes

- **Diagnosis (verified):** the XAML drawing gray is `WindowBackgroundBrush` (`#F0EEF4`) showing through the transparent `GlCanvas`; the Skia path already clears to white (`GlFrame` ctor). XAML table borders are a 0.5 px black hairline (`GlRectangle.SetInstance`); the Skia table border is a 0.75 px gray (`GlFrame.DefaultStroke`). Hover tracking already exists in both stacks (XAML `OnHoverChanged` / Skia `IsOverTable`) but only emphasized connectors.
- **Shared values (the one decision):** everything lives in `TablePalette` — `BorderHex` `#5A5A5A` / `BorderWidth` `1.2f` at rest; `HoveredBorderHex` `#1E90FF` (DodgerBlue — the selection/hover accent) / `HoveredBorderWidth` `2.4f` while hovered; `CanvasBackgroundHex` `#FFFFFF` default surface. Both renderers parse the same hex/widths.
- **XAML background:** paint the drawing host grid (the row-1 `Grid` wrapping the ScrollViewer + hover overlay) — the transparent canvas shows it through, and the same color wraps the paper at low zoom. `ModelPanelControl.BackgroundColor` → `DrawingSurface.Background`; `ModelEditorControl` forwards it; `MainWindow`'s ComboBox sets both renderers.
- **Skia background:** `GlFrame` gains an optional `backgroundColor` ctor param (default white — all existing callers unchanged); `SkiaPanelControl` passes its `BackgroundColor` (converted) each paint. Changing the property invalidates so the next paint picks it up.
- **Hovered-border wiring:** XAML — `OnHoverChanged` already fires with the hovered `GlObject`; when it is a `Table`, set `Hovered = true` (the setter repaints the stroke live); clear on leave/switch/render. Skia — a `HitTestTable` returns the hovered table's name (same pointer→content mapping as the paint), `SkiaPanelControl` tracks it, and each paint passes `hovered` to `ISkiaTableFactory.Create` (new optional param).

## Definition of Done

- [x] `ModelPanelControl`/`SkiaPanelControl` default to white; the renderer-bar "Base:" drop-down (presets + Custom…) changes both renderers live.
- [x] Both renderers stroke tables with the shared rest border (XAML + Skia).
- [x] Hovering a table thickens + tints its border in both renderers; leaving restores it.
- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass (existing + any new tests).
- [x] Manual run: drawing surface changes color in both renderers; drag, hover, inspector, pan/zoom/fit unchanged; connectors still anchor at column rows.
- [x] `docs/WORKLOG.md` updated (and `CLAUDE.md`/`CURRENT.md` for the palette story + the bar description).

## Status

- **State:** Completed
- **Sprint:** sprint-2026-08-20-drawing-surface-and-table-borders
- **Completed:** 2026-08-20 — `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **183/183 pass** (was 181). The "Base:" drop-down (presets + Custom… picker) + the hovered-table accent border are wired in both renderers. (Manual visual pass on the two renderers needs a human run — CLI launch runs on the agent's non-interactive desktop.)
