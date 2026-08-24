# 053 — Crow's Foot Cardinality Notation

## Summary

Add Crow's Foot as an ERD cardinality notation option so FK relationships can show optionality and multiplicity visually at connector endpoints. This should be a notation/rendering profile over the existing `ConstraintInfo.MinCardinality` / `MaxCardinality` data, not a new canonical model shape.

## Goals

- [x] Add a Crow's Foot ERD notation mode alongside the existing ERD/UML rendering choices.
- [x] Render endpoint glyphs for zero/one/many cardinality in both the XAML and Skia connector renderers.
- [x] Keep the existing FK route geometry, hit testing, labels, selection, and model serialization unchanged.
- [x] Cover cardinality-to-glyph mapping with focused portable tests and renderer-level parity checks where practical.

## Scope

In scope:

- A notation/profile type that can distinguish simple ERD connectors from Crow's Foot ERD connectors without changing the canonical data model.
- Mapping FK cardinality from `ConstraintInfo` to endpoint markers:
  - zero: optional circle
  - one: required bar
  - many: crow's-foot prongs
  - common combinations such as `0..1`, `1..1`, `0..*`, and `1..*`.
- Drawing glyphs at connector endpoints in both graphics stacks, derived from the final routed segment direction so markers align to table edges.
- UI wiring for choosing the ERD cardinality style in the renderer bar, relayed to both renderers.

Out of scope:

- Persisting the user's notation preference in model JSON.
- Introducing UML-only multiplicity structures or alternate relationship model types.
- IDEF1X or other stricter modeling notations.
- Changing routing, obstacle avoidance, table anchoring, connector label placement, or FK extraction semantics.

## Approach / Notes

- Treat Crow's Foot like the rounded-bend work: draw-only decoration on top of the existing orthogonal connector route.
- Keep `ConstraintInfo.MinCardinality` / `MaxCardinality` as the single source of truth. If cardinality is missing, fall back to the current simple FK connector appearance.
- Prefer a portable mapping helper in `Model.Graph`, for example a small endpoint marker/profile object that both XAML and Skia can consume.
- The parent/one side and child/many side need careful interpretation: existing data currently stores cardinality on the dependency/child FK constraint, so document and test which endpoint each marker belongs to.
- Reuse existing endpoint-side information from `ConnectorRouteRequest` / routed segments to orient glyphs; do not infer orientation from table positions alone.
- Keep UML mode separate: UML multiplicity labels already exist and should not be replaced by Crow's Foot glyphs.

## Definition of Done

- [x] The app exposes a Crow's Foot ERD notation option and keeps the current simple ERD behavior as the default unless intentionally changed.
- [x] XAML connectors render Crow's Foot endpoint markers correctly for `0..1`, `1..1`, `0..*`, and `1..*`.
- [x] Skia connectors render equivalent endpoint markers for the same mappings.
- [x] Existing connector selection/highlight styling remains legible with markers.
- [x] Tests cover the cardinality-to-marker mapping and at least one renderer-visible output path.
- [x] `dotnet test tests/ModelConsole.Tests/ModelConsole.Tests.csproj -c Debug` passes.
- [x] `dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64` passes or any failure is documented faithfully.

## Status

- **State:** Completed
- **Sprint:** sprint-2026-08-23-crows-foot-cardinality-notation
- **Completed:** 2026-08-23
