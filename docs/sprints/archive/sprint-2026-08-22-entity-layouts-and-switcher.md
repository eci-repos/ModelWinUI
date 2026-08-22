# Sprint 2026-08-22 — Entity layouts and switcher

## Dates

- **Start:** 2026-08-22
- **End:** 2026-08-22

## Scope

Backlog items in this sprint:

- [x] `044` — Connectivity-aware entity layout engine
- [x] `045` — Entity layout switcher in the app

## Execution Log

- 2026-08-22 — Started sprint from backlog items `044` and `045`.
- 2026-08-22 — Replaced the row-major-only `TableLayoutEngine` with `EntityLayout` / `EntityLayoutEngine` and wired the XAML + Skia renderers to a shared layout name.

## Results

- **Completed:** `044`, `045`
- **Deferred:** Serpentine column/up-down knobs were not surfaced yet; the default options are used from both renderer paths.
- **Notes:** Grid remains the no-regression default. Connectivity-aware ordering and alternate shapes are view-side only and are not persisted.
