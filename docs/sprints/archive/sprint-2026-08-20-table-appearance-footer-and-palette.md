# Sprint 2026-08-20 — Table appearance: footer band, kind-tinted body, unified palette

> Executed copy of the sprint. Definition: `docs/backlog/archive/036-table-appearance-footer-and-palette.md`.

## Dates

- **Start:** 2026-08-20
- **End:** 2026-08-20

## Scope

Backlog items in this sprint:

- [x] `036` — Table appearance: footer band, kind-tinted body, unified palette

## Execution Log

- 2026-08-20 — **New library `Model.Palette`** (the backlog's recommended palette home): a portable `net10.0` / 0-package library (namespace `ModelConsole.Palette`, → Model.Data for `TableKind`) holding `TablePalette` — `BannerHex(kind)` (entity `#DCE9F7` / reference `#E2EFDA`, unchanged), `FooterHex(kind)` (slightly deeper tones `#C7D9F3` / `#D3E7C4` so the card closes), `StripeHex(kind)` (`#F7FAFD` / `#F6FAF3` alternating row) + `PlainRowHex` white, and `FooterHeight` (the one shared F = 20). Wired into the sln (16 config mappings), `Model.Skia`, `Model.Graphics.WinUI`, the tests, and the app (the app now references all eight directly). Packs clean as `Model.Console.Palette.0.1.0.nupkg`.
- 2026-08-20 — **XAML `Table.cs`:** the dead +40 tail becomes the shared footer budget (`ComputedHeight = totalHeight + TablePalette.FooterHeight`, tables shrink 20 px); a new hit-test-transparent `_footerBorder` closes the card (kind-tinted fill, bottom corners rounded to mirror the banner, LightGray hairline top edge, moved in `DeltaMove`); body stripes switch WhiteSmoke/White → kind-tinted stripe/plain; header parses `TablePalette.BannerHex`.
- 2026-08-20 — **Skia `Table.cs`/`RectangleHalf`:** `RectangleHalf.DrawBottom` existed but was never wired in — `DrawBorders` now calls it for the footer, colored by kind from the shared palette; `RectangleHalf.Draw` gained an explicit fill color and correctly squares the bottom band's row-facing edge (`oy = y + h - r`); the measured height grows by the shared F so both renderers close identically; row stripes switch from the twin `#efefef` frame paints to a per-kind stripe/white pair; a hairline separates the footer from the last row. `GlPastelPalette.LightGreen` re-points at `TablePalette.ReferenceBannerHex` (the old `#CCE2CB` const retired).
- 2026-08-20 — **Geometry contract preserved:** row Y positions unchanged (top-down from the banner); only the bottom budget changed `ComputedHeight`/`_panel.height`. Matched-column `GetRowCenterY` anchors don't move; the unknown-column fallback tracks the new height self-consistently. New test pins the footer math.
- 2026-08-20 — **Tests (+1, 181 total):** `SkiaTableTests.ComputedHeightIncludesSharedFooterBudget` — from the last column row center to the table bottom sits half a row + the corner radius + the shared `FooterHeight`.

## Results

- **Completed:** `036`
- **Deferred:** — (the backlog now holds `037`–`040`, unscheduled)
- **Notes:** Verified `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **181/181 pass**; `dotnet pack src/Model.Palette` → clean. Manual verification of both renderers (card-style tables, drag / hover-highlight / inspector / pan / zoom / fit, no connector crossing a table) needs a human pass — CLI launch runs on the agent's non-interactive desktop.
