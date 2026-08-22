# 047 — Filled cross entity layout

## Summary

Correct the Cross entity layout from a center-plus-linear-arms projection to a filled cross-shaped region. The intended shape is two filled rectangles crossing each other, with entities packed throughout the cross area rather than aligned on four thin arms.

## Goals

- [x] Replace the current Cross arm-line placement with a filled cross-region packing algorithm.
- [x] Preserve the existing `EntityLayout` name (`Cross`) and shared layout-name plumbing from `045`.
- [x] Keep `Grid` unchanged as the no-regression default.
- [x] Pack entities into the union of a horizontal rectangle and a vertical rectangle.
- [x] Use connectivity information to bias central/high-degree entities into the intersection and related entities into nearby cells.
- [x] Support collapsed group boxes as single layout entities using the existing `group::...` layout keys.
- [x] Add tests that prove Cross is filled, deterministic, non-overlapping, and not a four-line arm layout.

## Scope

In scope:
- Changes inside the pure `EntityLayoutEngine` Cross projection and any helper types it needs.
- Unit tests in `EntityLayoutEngineTests` for filled-cross shape behavior.
- Documentation/worklog updates.

Out of scope:
- UI changes; the existing `Layout: Cross` selector should keep working.
- Persisting layout state to model files.
- Force-directed/spring refinement.
- Router, anchor, or connector-style changes.
- Renaming data model or renderer primitives from `Table*` to `Entity*`.

## Approach / Notes

- The current implementation is wrong for the intended semantics because it places the first entity at the center and then distributes the rest along four one-cell-wide arms. Cross should instead fill the area of two overlapping rectangles.
- A deterministic implementation can generate candidate grid cells in a cross mask: `abs(col) <= verticalHalfWidth` OR `abs(row) <= horizontalHalfHeight`, expanding arm lengths and bar thickness until all entities fit.
- The intersection should receive the highest-priority/highest-degree entities first. Remaining entities should occupy nearby available cells, preferably preserving connectivity neighborhoods and avoiding unnecessary connector span.
- The output contract remains `IReadOnlyDictionary<string, Rect2>` keyed by table names and collapsed-box keys.
- Tests should reject the linear-arm behavior by requiring occupied cells that are inside the horizontal/vertical bars but not on the center row or center column.

## Definition of Done

- [x] `EntityLayout.Cross` packs entities inside a filled cross-shaped footprint rather than four one-cell-wide arms.
- [x] Cross layout is deterministic and non-overlapping for PublicSafety and synthetic models.
- [x] Collapsed groups still lay out as one rect in Cross.
- [x] Tests assert filled-region behavior, not only non-overlap.
- [x] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` passes.
- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` passes with 0 errors / 0 warnings.
- [x] `docs/WORKLOG.md` updated.

## Status

- **State:** Completed
- **Sprint:** 2026-08-22 — Filled cross entity layout
- **Completed:** 2026-08-22
