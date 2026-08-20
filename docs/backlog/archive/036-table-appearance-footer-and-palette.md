# 036 — Table appearance: footer band, kind-tinted body, unified palette

## Summary

The ERD tables render as white rectangles with a thin pastel banner — flat and unfinished at the bottom. In the XAML path a dead **+40 px band** (`Table.ComputedHeight`) sits empty below the last column row, and in the Skia path there is no bottom band at all. Three pre-existing parity gaps compound the look: the Skia banner is always light-green regardless of table kind (XAML colors by kind), the Skia row stripes paint both stripes the same `#efefef` (XAML alternates WhiteSmoke/White), and the two palettes are hardcoded independently (`Table.cs` statics vs `GlPastelPalette.cs`). This item turns the empty bottom into a **designed footer strip** that closes each table as a card (banner → columns → footer), tints the body rows toward the table kind, and **unifies the table palette** into one source both renderers consume — extending the 035 theming idea to the drawing surfaces.

## Goals

- [ ] Footer strip replacing the empty +40 (XAML) and closing the Skia table — rounded to match the banner, colored from the kind family; tables read as finished cards.
- [ ] Kind-tinted body rows (both renderers) so entity vs reference reads from the body, not just the banner.
- [ ] One shared table palette source consumed by both stacks; the two hardcoded palettes retire.
- [ ] Geometry contract preserved: row Y positions unchanged, `ComputedHeight`/`ComputedWidth` updated self-consistently, `GetRowCenterY` (matched + fallback) still correct, routing/anchors re-verified, tests green.

## Scope

**In scope:**
- XAML `Table.cs`: footer band, kind-tinted striping, palette values from the shared source.
- Skia `Table.cs`/`RectangleHalf`: enable/implement the bottom band (`DrawBottom` already exists, commented out), banner colored by kind, real stripe alternation, shared palette.
- The shared palette home (design decision below).
- Optional stretch: footer metadata text ("N columns") if width-guarded and rendered in both stacks.
- Docs/WORKLOG.

**Out of scope:**
- Connector appearance (DodgerBlue/SlateBlue stays).
- Grip/handle/selection visuals.
- `TableLayoutEngine` changes (row placement stays top-down from the banner).
- Light/dark theme variants.
- 035's control-header theming (separate item — only the shared palette overlaps).

## Approach / Notes

- **Diagnosis (verified):**
  - XAML: `ComputedHeight = banner + cornerRadius×2 + rowHeights + 40` (`Table.cs:229`); the +40 renders as WhiteSmoke below the last row. Rows are positioned top-down (`y += row.Height`), independent of the tail.
  - Skia: `_panel.height` ends at rows + corner radius — no tail. Banner always `GlPastelPalette.LightGreen` (`#CCE2CB`, `RectangleHalf.cs:20`), not kind-based. Row stripes are both `#efefef` (`GlFrame.cs` `DefaultLightStroke`/`DefaultLightFill`), so Skia shows no alternation.
  - Tests are height-agnostic: `SkiaTableTests` asserts `ComputedHeight > 0` and relative `GetRowCenterY` positions; the fallback asserts `ComputedHeight / 2`. All stay self-consistent under a footer-budget change. `ErdComposerTests` only checks the diagram is non-empty/colored.

- **Safe geometry approach:** keep row Y positions identical (top-down from the banner); only the footer budget changes `ComputedHeight`. Matched-column anchors move not at all; the `GetRowCenterY` fallback midpoint tracks `ComputedHeight` self-consistently. Nothing anchors to the bottom strip (connectors attach at column rows, `GetRowCenterY`). Re-run the geometry/routing tests + a manual pass after any height change.

- **Footer design:** a band of height **F (propose 20 px)** — today XAML is +40 and Skia +0; pick **one F for both** for visual parity. Rounded on the bottom corners to mirror the banner's top rounding, filled from the same kind family (the banner pastel or a slightly deeper tone), separated from the last row by a hairline. The optional "N columns" text is left-aligned and clipped when too wide for narrow tables.

- **Kind-tinted body:** replace fixed WhiteSmoke/White (XAML) and `#efefef`/`#efefef` (Skia) with a per-kind even/odd pair — e.g. entity `#F7FAFD`/`#FFFFFF`, reference `#F6FAF3`/`#FFFFFF` — whispers of the banner hue so the body echoes the kind without going full-color. Keep `TableKindClassifier` as the source of the kind.

- **Unified palette — where it lives (the one decision in this item):**
  - **Recommended:** a tiny portable **`Model.Palette`** library (`net10.0`, 0 packages, namespace `ModelConsole.Palette`) holding the hex constants for banner-by-kind, footer, and row stripes — consumed by Model.Skia (`SKColor.Parse`) and Model.Graphics.WinUI (`Color.FromArgb`). One source of truth, consistent with the 030–034 layering; both stacks already reference the portable layers and neither references the other (no cycle). 035's theme dictionary can later source its table-relevant defaults from here.
  - **Lighter alternative:** place the constants in `Model.Data` (both stacks already reference it) — one fewer project, but color-in-the-metadata-layer is conceptually odd.
  - Confirm the home before starting; the rest of the item is the same either way.

- **Skia `DrawBottom`:** `RectangleHalf.DrawBottom` is already written but commented out in `Table.DrawBorders` (`Table.cs:255` — `//spHalfRec.DrawBottom(...)`). The bottom band was anticipated; implement/enable it as the Skia footer rather than drawing a new shape. Flip the banner from the fixed `GlPastelPalette.LightGreen` to the kind-based color from the shared palette, and make the two row paints distinct.

- **Parity discipline:** every change lands in both renderers in the same commit (the 003 discipline); verify visually on both the XAML model and the Skia render. When the footer budget changes a table's height, both diagrams re-layout independently and routing re-runs — confirm no connector crosses a table (the 012 invariant).

- **Relationship to 035:** 035 themes control headers/menu; this item extends the theme idea to the drawing surfaces and consolidates the two hardcoded palettes. If both are scheduled, align this item's palette-home decision with 035's so there is one palette story end to end.

## Definition of Done

- [ ] XAML tables show a footer band closing the card (banner → columns → footer), rounded to match; no dead WhiteSmoke tail.
- [ ] Skia tables show the same footer, color the banner by table kind, and alternate two distinct row stripes.
- [ ] One shared palette source; `Table.cs` statics and `GlPastelPalette` constants derive from it (no duplicated hex).
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass (existing + any new footer assertions).
- [ ] Manual run: both renderers show the card-style tables; drag, hover-highlight, inspector, pan/zoom/fit unchanged; FK connectors still anchor to column rows and never cross a table (012).
- [ ] `docs/WORKLOG.md` updated (and `CLAUDE.md` if a new `Model.Palette` library joins the collection).

## Status

- **State:** Completed
- **Sprint:** sprint-2026-08-20-table-appearance-footer-and-palette
- **Completed:** 2026-08-20 — footer band both renderers (shared F = 20, from the new `Model.Palette` `TablePalette`), kind-tinted banners + body stripes, one shared palette source; build 0/0, tests 181/181.
