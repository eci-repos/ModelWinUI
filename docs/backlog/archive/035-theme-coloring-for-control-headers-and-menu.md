# 035 — Themed pastel coloring for control headers and the menu bar

## Summary

Every control in the solution reads as flat white-on-white: the header strips are hardcoded `#fbfbfb` (near-white) — `ModelExplorerControl`, `EntityInspectorControl`, `DiagnosticsLogControl`, and the left/right collapse strips in `ModelEditorControl` — the zoom toolbars of `ModelPanelControl`/`SkiaPanelControl` have no background at all, and the top `MenuBar` (plus the renderer bar beneath it) is default colorless. This item gives **every control header and the app's top chrome a distinct pastel tint** so panels read apart at a glance, and centralizes all of it in a **theme resource dictionary** so the palette is one file to change. Constraint: the tints must not collide with the model's own colors (entity-table headers `#DCE9F7` light blue, reference-code headers `#E2EFDA` light green) or wash out the drawing area.

## Goals

- [ ] A theme `ResourceDictionary` (the app's first custom theme) defining named brushes for each control header, the panel toggle strips, the two zoom toolbars, the menu bar, and the renderer bar.
- [ ] All control headers/strips reference theme brushes — the hardcoded `#fbfbfb`, `LightGray`, and `#EDEDED` values leave `Controls/*.xaml`.
- [ ] `MainWindow`'s `MenuBar` and renderer bar are themed from the same dictionary.
- [ ] Palette is pastel-light, distinguishable per panel, and does not conflict with the ERD table colors.

## Scope

**In scope:**
- The theme dictionary + brush-key sweep across the six controls and `MainWindow.xaml`.
- Per-panel distinct pastel tints (proposal below), centralized for one-file retuning.
- App chrome: `MenuBar` + the renderer bar (they share the colorless problem; strike if you want them left alone).

**Out of scope:**
- Re-skinning menu **flyouts** (`MenuFlyoutItem`/submenu) — only the `MenuBar` strip itself is themed.
- Light/dark theme variants — the dictionary is structured so a future `ThemeDictionaries` split is trivial, but only a single light palette is delivered.
- Changing the ERD table banner colors (`Table.cs` `EntityHeaderColor`/`ReferenceHeaderColor`) — the constraint, not the target.
- Any behavioral change; the Skia/XAML drawing surfaces are untouched.

## Approach / Notes

**Dependency on 034** (planned, not executed): 034 moves these controls into `Model.Controls.WinUI`. **Execute this item after 034** so the theme dictionary ships inside the library (`src/Model.Controls.WinUI/Themes/`) and the colored headers land in their permanent home in one move. If done before 034, the dictionary starts in the app (`Themes/` in `Model.WinUI.Console`) and must be added to 034's file-move list — note that in the 034 item when scheduling. The brush keys and control edits are identical either way; only the dictionary's home differs.

- **Theme file:** `Themes/ControlTheme.xaml` — a plain `ResourceDictionary` of named `Color`/`SolidColorBrush` keys, merged into the host's `App.xaml` (`ResourceDictionary.MergedDictionaries`, after `XamlControlsResources`). Controls reference the keys with **`{ThemeResource …}`** — the codebase's established idiom (`DividerStrokeColorDefaultBrush`) — so a host can override any key in its own merged dictionary, and a future light/dark split drops into `ThemeDictionaries` without touching a single control. This is the "changed as needed" contract: the palette is one dictionary, and any host can shadow it.

- **Brush keys** (names are the theme surface — finalize at execution):
  - `ExplorerHeaderBackgroundBrush` — Model Explorer header
  - `InspectorHeaderBackgroundBrush` — Entity Inspector header
  - `DiagnosticsHeaderBackgroundBrush` — Diagnostics Log header
  - `EditorToggleStripBackgroundBrush` — left/right collapse strips (`ModelEditorControl`)
  - `ModelToolbarBackgroundBrush` — Model Panel zoom toolbar
  - `SkiaToolbarBackgroundBrush` — Skia Panel zoom toolbar
  - `MenuBarBackgroundBrush` (+ `MenuBarForegroundBrush`) — top menu
  - `RendererBarBackgroundBrush` — renderer bar in `MainWindow`
  - `ControlHeaderForegroundBrush` — header text (dark, keeps current look)
  - `ControlBorderBrush` — the panel `#EDEDED`/`LightGray` borders (DiagnosticsLog/EntityInspector/ModelExplorer roots, the toggle strips, the ListView border)

- **Proposed palette** (placeholder values — tune at execution; the point is the constraint, not the hex):
  | Area | Proposed pastel | Note |
  |---|---|---|
  | Model Explorer | lavender `#ECE7F6` | distinct from model blue/green |
  | Entity Inspector | peach/apricot `#FBE9E0` | warm, opposite the drawing |
  | Diagnostics Log | warm cream `#FDF4DC` | third distinct hue |
  | Editor toggle strips | soft violet-gray `#F1EFF7` | neutral, thin strips |
  | Model + Skia toolbars | soft neutral gray `#F3F3F7` | keep the drawing area calm |
  | Menu bar / renderer bar | soft warm gray `#F0EEF4` | app chrome, quiet |
  | Header text | dark `#1F1F1F` | unchanged contrast |
  | Borders | soft `#D9D9E3` | replaces `LightGray`/`#EDEDED` |
  - **Constraint check:** none of the above sits in the `#DCE9F7` (entity blue) or `#E2EFDA` (reference green) family, so the ERD tables and the panel headers stay visually separable.

- **Edits:**
  - `ModelExplorerControl.xaml` — header `Border` `Background="#fbfbfb"` → `{ThemeResource ExplorerHeaderBackgroundBrush}`; root `BorderBrush="#EDEDED"` → `{ThemeResource ControlBorderBrush}`.
  - `EntityInspectorControl.xaml` — same pattern with `InspectorHeaderBackgroundBrush`.
  - `DiagnosticsLogControl.xaml` — header `StackPanel` → `DiagnosticsHeaderBackgroundBrush`; root + `ListView` borders → `ControlBorderBrush`.
  - `ModelEditorControl.xaml` — the two `Background="#fbfbfb"` toggle strips → `EditorToggleStripBackgroundBrush` (borders → `ControlBorderBrush`).
  - `ModelPanelControl.xaml` / `SkiaPanelControl.xaml` — zoom-toolbar `Border` gains `Background="{ThemeResource ModelToolbarBackgroundBrush}"` / `SkiaToolbarBackgroundBrush` (border already theme-based).
  - `MainWindow.xaml` — `MenuBar` gains `Background`/`Foreground` from the theme keys (explicit attribute references, simplest and themeable; an implicit `Style TargetType="MenuBar"` in the dictionary is the alternative); renderer `StackPanel` gains `RendererBarBackgroundBrush`.
- **Verify no regressions:** the toggle-strip and toolbar backgrounds are hit-test-relevant only in that they're siblings of interactive content — keep them non-interactive (they already are) and keep `Background="Transparent"` where the canvas needs hit-testing (`GlCanvas`, `SkiaCanvasView`).

## Definition of Done

- [ ] One theme `ResourceDictionary` with the named pastel brushes; merged by the host `App.xaml`.
- [ ] No hardcoded `#fbfbfb`, `LightGray`, or `#EDEDED` remains in `Controls/*.xaml` (all on theme brushes).
- [ ] `MenuBar` + renderer bar themed; menu flyout untouched.
- [ ] Palette verified against the model colors: no header tint reads as `#DCE9F7`/`#E2EFDA`; ERD tables remain distinguishable on the canvas.
- [ ] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings (pre-existing `NETSDK1198` allowed); `dotnet test tests/ModelConsole.Tests` → all pass (no logic change).
- [ ] Manual run: colored headers in both renderers, toggle strips, menu bar; pan/zoom/fit/drag/hover unchanged (human visual pass).
- [ ] `docs/WORKLOG.md` updated (and `CLAUDE.md` if the theme ships in the library per the 034 ordering).

## Status

- **State:** Completed
- **Sprint:** sprint-2026-08-20-controls-library-and-theme
- **Completed:** 2026-08-20
