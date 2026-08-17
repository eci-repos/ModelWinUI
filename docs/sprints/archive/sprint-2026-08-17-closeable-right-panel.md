# Sprint 2026-08-17 — Closeable right panel

> Executed copy of the sprint. Backlog item: `docs/backlog/014-closeable-right-panel.md`.

## Dates

- **Start:** 2026-08-17
- **End:** 2026-08-17

## Scope

Backlog items in this sprint (reference by number):

- [x] `014` — Right panel (log + inspector) can be closed: one-click collapse/expand toggle, drawing reflows into freed space, zoom/pan preserved

## Execution Log

- 2026-08-17 — Sprint defined from backlog item `014`. Approach: a dedicated toggle strip column between the drawing and the right panel — the strip lives in its **own column** so the button stays reachable while the panel is collapsed (a header row inside the panel would collapse away with it). Clicking flips `RightPanel.Visibility` (`Visible` / `Collapsed`); the star-sized drawing column reflows automatically (no manual resize math); the chevron and tooltip flip between collapse/expand. No `ChangeView` / `FitToWindow` is triggered, so zoom and pan are preserved. `DiagnosticsLogControl` is still declared before `ModelPanelControl`, so log-subscription ordering is intact.
- 2026-08-17 — Verified: full solution `-c Debug -p:Platform=x64` → **0 errors, 0 warnings**; **54/54 tests pass**; app launches unpackaged and stays running (toggle click needs a manual pass — a CLI launch runs on the agent's non-interactive desktop).
- 2026-08-17 — Backlog item archived: `docs/backlog/archive/014-closeable-right-panel.md`.

## Results

- **Completed:** `014`
- **Deferred:** resizable splitter, persisted collapsed state across sessions, independent log/inspector toggles — all out of scope per the item.
- **Notes:** The previous sprint (2026-08-16, items 008–012) was promoted to `docs/sprints/archive/sprint-2026-08-16-connector-routing.md`; backlog items 011/012/013 were archived in the same housekeeping pass.
