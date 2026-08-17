# Sprint 2026-08-17 — Non-trivial sample models

> Executed copy of the sprint. Backlog item: `docs/backlog/005-non-trivial-sample-models.md`.

## Dates

- **Start:** 2026-08-17
- **End:** (TBD)

## Scope

Backlog items in this sprint (reference by number):

- [x] `005` — Non-trivial sample models: ship sample models showing the tool's capabilities
- [x] `015` — Skia renderer: fit-to-window + zoom, and compose off the UI thread (follow-up added to this sprint)
- [x] `016` — Mouse-wheel zoom around the pointer (follow-up added to this sprint)
- [x] `017` — Fit the whole model into the visible area (follow-up added to this sprint)
- [x] `018` — Wheel-zoom anchor at the cursor, and finer wheel increments (follow-up added to this sprint)

## Execution Log

- 2026-08-17 — Sprint defined from backlog item `005`. User scope decision: **Ship JSON + Open Sample menu** — export the existing 50-table PublicSafety schema to a shipped JSON file, author one new non-trivial sample in a different domain (a ~20-table library schema), ship both as app content, and add a File → Open Sample submenu.
- 2026-08-17 — `LibrarySchema` (ModelGraphLibrary, `ModelConsole.ModelData`): a 20-table / 30-FK library & books schema mirroring the `PublicSafetySchema` builder pattern — 7 `Ref*` reference tables (code key + `Description`) + 13 entity tables (Address, Publisher, Author, LibraryBranch, Book, BookAuthor, BookCopy, Patron, Loan, Hold, Fine, Staff, Reservation). All FK `ReferencedColumnName`s null → parent-PK default; four FKs to `RefBookStatus` exercise `ConnectorAnchors.FanOut`.
- 2026-08-17 — `SampleModels` registry (`ModelConsole.ModelData`): `SampleModel` (Name / Description / FileName / Tables) + `SampleModels.All` listing Public Safety + Library — the single source of truth for the menu and the tests.
- 2026-08-17 — Shipped JSON files: `ModelGraphLibrary/Samples/PublicSafety.json` + `Library.json`, generated from the fixtures via `ModelFile.ToJson` (a one-off generator test wrote them, then was deleted). Both the app and the test project include them as content (`Link="Samples\…"`, `CopyToOutputDirectory="PreserveNewest"`).
- 2026-08-17 — `MainWindow` gains **File → Open Sample**: a `MenuFlyoutSubItem` after "Open Model…" (with a separator); items built in code-behind from `SampleModels.All` (each `Tag` = file name); clicking loads `AppContext.BaseDirectory/Samples/<file>` via `ModelFile.Load` and feeds both renderers. The shared "load → both renderers" logic is extracted into a `LoadModel` helper used by both `OpenModel_Click` and `OpenSample_Click`; load errors surface in a shared `ShowLoadErrorAsync` dialog.
- 2026-08-17 — `SampleModelTests` (6): shipped samples load + are valid (non-empty, every table has a PK, `FkEdgeExtractor.Extract` reports no issues), shipped JSON matches the fixture (sync guard, line endings normalized), PublicSafety is 50 tables / 74 FKs, Library is ≥ 15 tables / ≥ 15 FKs.
- 2026-08-17 — Verified: app project builds 0 errors / 0 warnings; `SampleModelTests` 6/6 pass; the shipped JSON files land in the app output `Samples/`. (Full-solution `--no-incremental` build + full test suite + launch check pending.)
- 2026-08-17 — Sprint extended with backlog item `015` (Skia renderer fit-to-window + zoom, and compose off the UI thread). User scope decision: **Fit button + zoom slider** — a small toolbar like the XAML path's, defaulting to fit-to-window with a ~5 px margin, recomputed on resize.
- 2026-08-17 — `SkiaPanelControl.xaml` gains a zoom toolbar row (Fit button E81C + zoom slider 10–400 + % readout) mirroring `ModelPanelControl`'s, plus a "Composing…" overlay `TextBlock`.
- 2026-08-17 — `SkiaPanelControl.xaml.cs` gains fit/zoom state (`_fitMode` default true, `_zoom`, `_syncingSlider`/`_initialized` guards): on paint, content bounds come from `_diagram.Layout`; fit scale = `min((viewW−10)/contentW, (viewH−10)/contentH)` capped at 1.0 (never upscale, matches the XAML path), floored at 0.01; `Translate`+`Scale` on `frame.Canvas` centers the content. Fit recomputes on every paint → resize re-fits automatically. Fit button → `_fitMode = true`; slider → `_fitMode = false`, `_zoom = value/100`.
- 2026-08-17 — **Library fix:** `Table.DrawBorders` (ModelGraphLibrary) now uses `Save`/`Concat`/`Restore` around the 180° rotation instead of `SetMatrix(rotation)`/`SetMatrix(Identity)` — the old code wiped any canvas transform (e.g. the fit/zoom transform) for the rest of the draw. Identical behavior with an identity current matrix.
- 2026-08-17 — **Compose off the UI thread:** the first paint starts `Task.Run` → `ErdComposer.Compose` over a 1×1 offscreen `SKSurface` (measuring only needs the font), then `DispatcherQueue.TryEnqueue` sets `_diagram`, logs counts + FK issues, hides "Composing…", invalidates. Stale-compose guard (`ReferenceEquals(captured, _tables)`) discards a compose that finished after a `SetModel` and re-paints; `try/catch` logs the error and resets `_composing` on the UI thread. This fixes the first-paint freeze the user hit.
- 2026-08-17 — Verified: full-solution `--no-incremental` build → **0 errors, 0 warnings**; **72/72 tests pass** (unchanged — no pure-logic change). (Launch + visual pass pending — CLI launch runs on the agent's non-interactive desktop.)
- 2026-08-17 — **Follow-up to 015 (user request):** the mouse wheel must zoom instead of scroll, and the fit button must show the whole model with a margin. **XAML path:** `PointerWheelChanged` handled on the `GlCanvas` (`e.Handled = true` stops it bubbling to the ScrollViewer's scroll handler); `ApplyZoom` generalized to take a content-space anchor (the cursor) so the point under the cursor stays fixed; `FitToWindow` now fits with a 5 px margin. **Skia path:** the wheel zooms around the cursor via a `_panX`/`_panY` surface-px offset (cursor DIPs converted via `_dpiScale`); the Fit button resets the pan and re-enters fit mode; the slider preserves the pan.
- 2026-08-17 — Verified (wheel-zoom follow-up): full-solution `--no-incremental` build → **0 errors, 0 warnings**; **72/72 tests pass** (unchanged — no pure-logic change); app launches unpackaged and stays running. (Interactive wheel/fit behavior needs a manual pass.)
- 2026-08-17 — **Wheel-zoom follow-up 2 (user feedback):** the Fit button is now **labeled "Fit"** (icon + text) in both renderers' toolbars (the icon-only button was not discoverable); the XAML ScrollViewer's scroll bars are **Hidden** (panning/zoom still work via `ChangeView` — the bars were confusing); the **wheel direction is flipped** in both renderers — **wheel up zooms out, wheel down zooms in**, still around the cursor (`factor = delta > 0 ? 1.0 / ZoomStep : ZoomStep`).
- 2026-08-17 — Verified (wheel-zoom follow-up 2): full-solution `--no-incremental` build → **0 errors, 0 warnings**; app launches unpackaged and stays running. (Interactive wheel/fit behavior needs a manual pass.)
- 2026-08-17 — **Blank-page bug fixed (user report):** "The fit button don't fit anything I get a blank page… when I wheel down or up the page turns blank." **Root cause:** the WinUI 3 `ScrollViewer` scrolls on the wheel **even when the canvas marks `PointerWheelChanged` as `e.Handled = true`** (known issue microsoft/microsoft-ui-xaml#2947) — at low zoom the wheel scroll amount (48 px ÷ zoom) is huge in content units, so the viewport drifts to empty paper → blank page. **Fix:** `HorizontalScrollMode="Disabled"` + `VerticalScrollMode="Disabled"` on `ModelScrollViewer` — `ScrollableWidth = ExtentWidth − ViewportWidth` is independent of `ScrollMode`, so `ChangeView` (zoom/fit/pan) still works while user-initiated wheel scrolling is disabled.
- 2026-08-17 — Verified (blank-page fix): app project builds **0 errors, 0 warnings**; **72/72 tests pass** (unchanged — no pure-logic change); app launches unpackaged and stays running. (Interactive wheel/fit behavior needs a manual pass.)
- 2026-08-17 — The wheel-zoom and fit work is split into two backlog items — `016` (mouse-wheel zoom around the pointer, incl. the blank-page fix) and `017` (fit the whole model into the visible area) — so each visible-drawing concern is tracked separately.
- 2026-08-17 — **User report → backlog `018`:** "You are not using the current mouse pointer position as the transformation reference point it looks like you are using the origin (0,0) instead. also the increments of the wheeling up/down are too big and just jump to fast." **Root cause:** the XAML path's zoom/fit/pan math assumed ScrollViewer offsets were **content units**; they are actually **viewport px (zoom-applied)** — `ExtentWidth` grows with `ZoomFactor`, and the correct mapping is `content = (viewportPx + offset)/zoom`. Under the wrong model, `ApplyZoom` mixed units and drifted (reading as origin-anchored), `FitToWindow` computed an offset (~6900) that `ChangeView` clamped to ~[0, 1800] at fit zoom → blank page, and pan was 1/zoom too slow when zoomed out.
- 2026-08-17 — **Fix (`018`):** `ApplyZoom` re-derived for viewport-px offsets — anchor is a viewport-px point, `newOffset = content·zoom − anchorPx`; the wheel anchor comes from `GetCurrentPoint(ModelScrollViewer)` (viewport px, unambiguous under zoom); `FitToWindow` centers via `(center)·fit − vw/2`; `OnPanRequested` multiplies the content delta by zoom. **Both renderers** get a smooth, delta-proportional wheel step — `factor = Math.Pow(1.1, −delta/120)` (≈1.1× per notch, trackpad-friendly) — replacing the fixed ×1.25 notch.
- 2026-08-17 — Verified (`018`): full-solution `--no-incremental` build → **0 errors, 0 warnings**; **72/72 tests pass** (unchanged — no pure-logic change); app launches unpackaged and stays running. (Interactive wheel/fit/pan behavior needs a manual pass.)

## Results

- **Completed:** `005`, `015`
- **Notes:**
  - The samples are **generated from code fixtures**, not hand-maintained JSON — `SampleModelTests.ShippedJsonMatchesFixture` keeps the checked-in files in sync with the fixtures.
  - Adding a sample to `SampleModels.All` automatically adds it to the File → Open Sample menu (the menu is built from the registry).
  - The roadmap is now complete: all four roadmap items (base library, UI controls, sample models, assess next steps) are done.
