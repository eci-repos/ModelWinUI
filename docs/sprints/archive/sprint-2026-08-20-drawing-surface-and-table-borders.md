# Sprint 2026-08-20 — Drawing-surface background and table borders

> Executed copy of the sprint. Definition: `docs/backlog/archive/041-drawing-surface-background-and-table-borders.md`.

## Dates

- **Start:** 2026-08-20
- **End:** 2026-08-20

## Scope

Backlog items in this sprint:

- [x] `041` — Drawing-surface background color, thicker table borders, hovered-border emphasis

## Execution Log

- 2026-08-20 — **Shared values (the one decision):** `TablePalette` (Model.Palette) gained the border + base metrics both renderers parse — `BorderHex` `#5A5A5A` / `BorderWidth` `1.2f` at rest, `HoveredBorderHex` `#1E90FF` / `HoveredBorderWidth` `2.4f` while hovered, `CanvasBackgroundHex` `#FFFFFF` default surface.
- 2026-08-20 — **Base-color wiring:** the renderer bar gained a **"Base:" `ComboBox`** (White/Ivory/Mint/Light Gray presets + **"Custom…"** opening a `Flyout`-hosted `ColorPicker` live-applied on `ColorChanged` — `ColorPickerFlyout` does not exist in this Windows App SDK version, and the WinAppSDK `ColorChangedEventArgs` exposes `NewColor`/`OldColor`, not `.Color`). `ApplyBackgroundColor` drives both renderers: XAML via `ModelEditorControl.BackgroundColor` → `ModelPanelControl.BackgroundColor` → `DrawingSurface.Background` (the row-1 host grid); Skia via `SkiaPanelControl.BackgroundColor` → a new optional `GlFrame` `backgroundColor` ctor param cleared each paint.
- 2026-08-20 — **XAML table border:** `Table.ApplyBorderAppearance()` strokes the card from the palette — rest neutral 1.2 px, hovered DodgerBlue 2.4 px — mutated live via `Table.Hovered` (no re-render); `ModelPanelControl.OnHoverChanged` sets it on the hovered table and clears on leave/switch/render.
- 2026-08-20 — **Skia table border:** `RectangleHalf.DrawBorder` takes color+width (local paint — no shared-state mutation); `Table.DrawBorders` strokes rest vs hovered from the palette; `ISkiaTableFactory.Create` gained an optional `hovered` param; `SkiaPanelControl` hit-tests the pointer→content table per paint (`HitTestTable`) and threads it through — the next paint picks it up.
- 2026-08-20 — **Tests (+2, 183 total):** `GlFrameTests` — `ClearsToWhiteByDefault` + `ClearsToProvidedBackground` (bitmap-backed surface + `GetPixel`), pinning the shared default + the optional background.

## Results

- **Completed:** `041`
- **Deferred:** — (the backlog now holds `037`–`040`, unscheduled)
- **Notes:** Verified `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **183/183 pass**. Manual verification of both renderers (the "Base:" presets + Custom… picker changing the surface color, the hovered table's DodgerBlue accent border thick/thin on hover, drag / hover-highlight / inspector / pan / zoom / fit unchanged) needs a human run — CLI launch runs on the agent's non-interactive desktop.
