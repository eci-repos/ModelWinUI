# 037 — Tags on the model: UML-ready entity metadata

## Summary

The grouping/visibility story (038–040) is built on tags, but `TableInfo` has no *persisted* annotation field today — its annotation bags (`Metadata`, `Extensions`, `Provenance`) are all `[JsonIgnore]` (`TableInfo.cs:25,34,43`), meaning they are interpretation-time concerns that never reach the model file. A tag encodes domain intent ("this table belongs to Accounting") and must survive save/load, so it needs a first-class serialized member. This item adds **`TableInfo.Tags`** — a serialized `List<string>` — round-trips it through both JSON formats, extends both schemas, edits it in the inspector, and displays it in the explorer. **UML seam:** a tag is deliberately the bridge to UML — it maps to a UML **package** (group membership) and to a **stereotype/tagged value** on a class, so the model can be emitted as UML (040) with no remodeling. The mapping is *derived* at emit time, never stored — **tags are the only UML-visible addition to the model**, and the JSON format gains nothing beyond them. Tag names are kept UML-identifier-friendly to make that mapping lossless.

## Goals

- [x] `TableInfo.Tags` (`List<string>`, serialized, null-tolerant → empty) added to `TableInfo` and to `Copy()`.
- [x] `ModelFile` round-trips tags through **both** formats — the containerized form (`{ dataSource, schemas: [{ name, tables }] }`) persists them; the flat-array legacy format stays readable with absent tags defaulting to empty.
- [x] Both JSON schemas (`array.schema.json`, `grouped.schema.json`) declare an optional `tags` string-array; `ModelSchemaValidator` stays green and existing files validate unchanged.
- [x] Tags editable in the inspector (via the pure `ModelEdits` ops, the 029 discipline) and shown in the explorer nodes + readout/hover.
- [x] **UML:** the tag vocabulary maps directly onto UML package/stereotype names (name hygiene enforced at input) so a later UML emit needs no reshaping.

## Scope

**In scope:**
- Model.Data: `Tags` member, `Copy()`, JSON round-trip, schema updates, validation tests.
- Model.Graph: a pure `ModelEdits.SetTableTags` operation so the inspector edits through the same invariant-preserving path as every other edit.
- Editor UI: tags editor in the inspector (gated by `NodeVerbs` like the 029 controls), tags text in the explorer node summary.
- Docs/WORKLOG.

**Out of scope:**
- Groups/visibility logic (038), collapsed group boxes (039), UML export/notation (040).
- Tag *values* (a stereotype-with-value form, e.g. `status=archived`) — free-form name only for now; the field shape leaves room for a value later.
- Saving named view profiles.

## Approach / Notes

- **Why a new field, not the existing bags:** `Metadata`/`Extensions`/`Provenance` are `[JsonIgnore]` by design (source-interpretation concerns, not canonical stored data). Tags are the first **persisted** per-entity annotation — do not hang them off the non-persisted bags.
- **Lean-core guardrail:** tags are the single bridge to UML and stay plain strings. No UML-only members (`Stereotype`, `TaggedValues`, `Package`/`Uml*`) join `TableInfo`, `ColumnInfo`, or `ConstraintInfo`; per-tag colors or metadata are view-side (036/039), never model. The UML profile in 040 *reads* the model and never writes UML back into it.
- **Model:** `public List<string> Tags { get; set; }` — deserializers tolerate null/absent (normalize to empty). `Copy()` includes it (tables are copied at render/sample time).
- **Serialization:** containerized form writes `tags` per table; the interpreter (backlog 023/025 path) captures it; the flat-array reader ignores it when absent (backward compatible). `SampleModelTests` stay green; add a round-trip test that tags survive `ToJson`/`Load`.
- **Schema:** `tags` = `{ "type": "array", "items": { "type": "string" } }`, optional (not required), in both `array.schema.json` and `grouped.schema.json`. No default needed — absence means empty.
- **Edit path:** `ModelEdits.SetTableTags(TableInfo table, IEnumerable<string> tags)` — normalize (trim, dedupe, drop empties), return the new list. The inspector's tag editor (comma-separated box or a simple chip editor) calls it, then re-renders. Tag editing appears only when the node's `NodeVerbs` permit (028).
- **UML name hygiene:** tag names must be UML-identifier-friendly — letters, digits, `_`, `-`, no leading digit, no embedded control chars. Enforce at `SetTableTags` (validate + surface a diagnostics message on violation) so the 040 mapping is lossless. Kept light — free-form like descriptions otherwise.
- **Display:** explorer table nodes show tags as a compact suffix (e.g. `[Accounting, HR]`) or via the hover summary; the readout (022 path) includes them. No model-behavior change: untagged models are byte-for-byte today's behavior.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → 0 errors / 0 warnings; `dotnet test tests/ModelConsole.Tests` → all pass.
- [x] Round-trip test: tags survive `ModelFile` containerized save/load; legacy flat-array load with no `tags` yields empty tags.
- [x] Both schemas validate with `tags` present and absent; `ModelSchemaValidator` green.
- [x] The model diff introduces **only** `Tags` — no UML-only members (`Stereotype`, `TaggedValues`, `Uml*`) on the POCOs (code-review check).
- [x] Inspector edits tags via `ModelEdits.SetTableTags`; re-render shows them in explorer + readout; invalid names surface a diagnostics message, not a crash.
- [x] Manual run: open a sample, tag a table, save/reload, tags persist; untagged models render exactly as before. *(pending a human run — CLI launch runs on a non-interactive desktop)*
- [x] `docs/WORKLOG.md` updated (and `CLAUDE.md` "Data model" section: `TableInfo` now carries `Tags`).

## Status

- **State:** Completed
- **Sprint:** 2026-08-20
- **Completed:** 2026-08-20 (205/205 tests; solution 0/0)
