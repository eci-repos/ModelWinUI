# 046 — Filled circle entity layout

## Summary

Correct the Circle entity layout from a ring/circumference projection to a filled circular-region packing. The intent is a gathered cluster of entities inside a roughly circular footprint, with connected or central entities nearer the middle and related entities near each other.

## Goals

- [x] Replace the current Circle ring placement with a filled-circle packing algorithm.
- [x] Preserve the existing `EntityLayout` name (`Circle`) and shared layout-name plumbing from `045`.
- [x] Keep `Grid` unchanged as the no-regression default.
- [x] Place entities in non-overlapping slots within a disk-like footprint, not just on the perimeter.
- [x] Use connectivity information to bias central/high-degree entities toward the center and keep related entities close.
- [x] Support collapsed group boxes as single layout entities using the existing `group::...` layout keys.
- [x] Add tests that prove Circle is filled, deterministic, non-overlapping, and not a ring-only layout.

## Scope

In scope:
- Changes inside the pure `EntityLayoutEngine` Circle projection and any helper types it needs.
- Unit tests in `EntityLayoutEngineTests` for filled-circle shape behavior.
- Documentation/worklog updates.

Out of scope:
- UI changes; the existing `Layout: Circle` selector should keep working.
- Persisting layout state to model files.
- Force-directed/spring refinement.
- Router, anchor, or connector-style changes.
- Renaming data model or renderer primitives from `Table*` to `Entity*`.

## Approach / Notes

- The current implementation is wrong for the intended semantics because it maps a linear order onto a ring. Circle should instead pack slots whose centers fall inside, or as close as practical to, a circular area.
- A deterministic implementation can generate candidate grid cells around a center, keep cells whose centers fit inside a radius, sort cells by radius then angle or by a connectivity-aware priority, and expand the radius until all entities fit.
- High-degree or central entities should be assigned first to cells nearest the center; neighbors should prefer nearby available cells. This can reuse the connectivity-aware ordering from `EntityLayoutEngine`.
- The output contract remains `IReadOnlyDictionary<string, Rect2>` keyed by table names and collapsed-box keys.
- Tests should reject the ring-only behavior by requiring at least one non-center entity to occupy an interior radius band for a sufficiently large model.

## Definition of Done

- [x] `EntityLayout.Circle` packs entities inside a filled disk-like footprint rather than around a circumference.
- [x] Circle layout is deterministic and non-overlapping for PublicSafety and synthetic models.
- [x] Collapsed groups still lay out as one rect in Circle.
- [x] Tests assert filled-region behavior, not only non-overlap.
- [x] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` passes.
- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` passes with 0 errors / 0 warnings.
- [x] `docs/WORKLOG.md` updated.

## Status

- **State:** Completed
- **Sprint:** 2026-08-22 — Filled circle entity layout
- **Completed:** 2026-08-22
