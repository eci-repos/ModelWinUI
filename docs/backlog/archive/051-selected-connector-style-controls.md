# 051 — Selected Connector Style Controls

## Summary

The selected or highlighted FK relationship path currently uses a fixed blue
style. Users should be able to choose the selected connector line color and
line width so relationship focus can match their visual preference or diagram
background.

## Goals

- [x] Add UI controls for the selected connector path color.
- [x] Add UI controls for the selected connector path stroke width.
- [x] Apply the selected connector style consistently in the XAML renderer.
- [x] Apply the selected connector style consistently in the Skia renderer.
- [x] Keep normal/rest connector style unchanged unless the connector is
      selected or highlighted.

## Scope

In scope: app-level UI controls, shared selected-connector style state,
renderer wiring for XAML and Skia, and focused tests where the style is held in
portable/shared code.

Out of scope: persistence unless the existing app settings pattern already has
a natural place for it, changing table selection styling, changing connector
routing, and changing normal connector colors.

## Approach / Notes

- Clarify whether "selected" maps to the existing connector hover/emphasis
  path, click-selected connector state, or both. If both exist, use one shared
  selected/highlight style so the user sees the same color/width when a
  relationship is focused.
- The current blue-like selected/highlight color appears in renderer-specific
  connector styling:
  `src/Model.Skia/Skia/Primitives/Connector.cs` and routed XAML connector
  styling through `src/Model.Controls.WinUI/Controls/ModelPanelControl.xaml.cs`
  / `src/Model.Graphics.WinUI/Graphics/GLibrary/GlOrtho/GlOrthoPath.cs`.
- Prefer a small shared style/options object or palette setting that both
  renderers consume instead of duplicating constants.
- UI should use a color picker or existing "Custom..." color-picker pattern
  from the drawing-surface background work, plus a bounded numeric control or
  slider for stroke width.
- Width should be clamped to a sane range, for example 1-8 px, so a selected
  connector remains readable without overwhelming the diagram.

## Definition of Done

- [x] User can choose any selected connector color from the app UI.
- [x] User can choose selected connector line width from the app UI.
- [x] XAML selected/highlighted connector paths use the chosen color and width.
- [x] Skia selected/highlighted connector paths use the chosen color and width.
- [x] Normal connector paths keep their existing default appearance.
- [x] The chosen style survives renderer switching during the current session.
- [x] Existing tests pass and solution build succeeds.

## Status

- **State:** Archived
- **Sprint:** 051 Selected Connector Style Controls
- **Completed:** 2026-08-22
