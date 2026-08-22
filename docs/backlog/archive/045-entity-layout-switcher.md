# 045 — Entity layout switcher in the app (both renderers)

## Summary

Expose the `EntityLayout` engine from 044 to the user: a **layout selector** in the app that re-lays-out and re-renders **both renderers identically** — mirroring the 043 theme wiring (`ApplyTheme`/`SetTheme` + the shared-name relay). The layout name flows as a string (like the theme name), nothing is persisted, and the default stays **grid** so the app opens looking exactly as it does today. This is the wiring half of the layout work; the pure engine and shapes are 044.

## Goals

- [x] **`SetLayout(string)` / `CurrentLayout` on `ModelPanelControl`** — re-initializes the layout + re-renders (the `SetTheme` shape, 043).
- [x] **`SetLayout(string)` on `SkiaPanelControl`** — swaps the shared layout name, clears the cached `ErdDiagram`, re-composes once off-thread on the next paint (the 043 stale-compose guard, extended to compare the layout name).
- [x] **`ModelEditorControl.ApplyLayout`** raising **`LayoutChanged`** → `MainWindow` relays to `SkiaEditor.SetLayout` — the 038/039/043 parity pattern, so both renderers draw the identical placement.
- [x] **A UI affordance** — a "Layout ▸/▾" collapsible section or a combo in the Model Explorer header (the 043 "Groups" button precedent: hidden by default, shown on request), listing Grid / Serpentine / Circle / Cross.
- [ ] **Option plumbing (where shippable)** — the serpentine up/down + column knobs from 044 surface as a minimal control (e.g. a "Columns:" number box, an "Up/Down" toggle); keep defaults sane; other shapes' options can wait. Deferred: selector ships with default shared options.
- [x] **Determinism + parity check** — switching layouts never changes model state; both renderers pixel-identical after the switch.
- [x] Tests/build/WORKLOG: pure layout behavior is unit-tested; the WinUI relay is build-covered and noted for manual visual parity.

## Scope

**In scope:**
- The layout name as a shared string on both renderer paths + the editor relay.
- The explorer layout selector UI + the (minimal) serpentine options.
- Docs/WORKLOG.

**Out of scope:**
- New layout kinds or ordering improvements — that is 044.
- Persisting the chosen layout to the model file or app settings.
- Anchor-side tuning per layout (future item).
- Any rename beyond what 044 already scoped.

## Approach / Notes

- **Copy the 043 theme wiring exactly.** The shared name lives on `ModelPanelControl` (`CurrentLayout`); `ApplyLayout` re-inits layout + re-renders the XAML path, re-syncs nothing in the explorer (layout has no explorer state), raises `LayoutChanged`; `MainWindow` relays to `SkiaPanelControl.SetLayout`; the Skia path clears its cached `ErdDiagram` and re-composes once (guard compares the layout name). `LayoutRequested` from the selector flows through `ModelEditorControl` — same shape as `ThemeRequested`/`CollapseAllRequested` (043).
- **Default = Grid** until the new shapes prove out on the samples; the sample models are the manual test bed (PublicSafety: dense fan-out; Enterprise: multi-schema components — good for the circle/cross reads).
- **Parity is by construction** (both paths consume the shared name + the same `EntityLayoutEngine`), so the DoD is one manual pass: switch every layout, confirm the XAML and Skia panels agree.
- **Tests:** the relay logic lives in the app controls (WinUI, not unit-tested today) — cover `EntityLayout.FromName` round-trip in the pure lib (already 044) and assert `LayoutChanged` wiring by hand; the routing/shape correctness is 044's test burden.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [x] The layout selector lists Grid / Serpentine / Circle / Cross; picking one re-lays-out and re-renders both renderers identically; default is Grid = today's look.
- [ ] Serpentine options (columns, up/down) work where surfaced; model state is unchanged by switching. Deferred: no option controls are surfaced yet.
- [x] Switching layout is safe with groups collapsed/visible in any combination.
- [x] `docs/WORKLOG.md` updated.

## Status

- **State:** Completed
- **Sprint:** 2026-08-22 — Entity layouts and switcher
- **Completed:** 2026-08-22
