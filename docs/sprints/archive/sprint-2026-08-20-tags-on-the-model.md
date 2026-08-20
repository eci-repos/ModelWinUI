# Sprint 2026-08-20 — Tags on the model: UML-ready entity metadata

> Executed copy of the sprint. Definition: `docs/backlog/archive/037-tags-on-the-model.md`.

## Dates

- **Start:** 2026-08-20
- **End:** 2026-08-20

## Scope

Backlog items in this sprint:

- [x] `037` — Tags on the model: UML-ready entity metadata

## Execution Log

- 2026-08-20 — **The model member:** `TableInfo.Tags` (`List<string>`) — the first **persisted** per-entity annotation, unlike the `[JsonIgnore]` bags (`Metadata`/`Provenance`/`Extensions`). `[JsonIgnore(Condition = WhenWritingNull)]` so a table without tags writes **no** `Tags` member at all — the schemas declare the array without a null case, and older model files stay byte-identical. `Copy()` includes it.
- 2026-08-20 — **Round-trip:** `ModelFile.ToJson`/`LoadJson` carry `Tags` automatically through the containerized form (per-table, via `SerializeToNode`) and the flat-array form; `TableInfo.ToJson`/`FromJsonFile` too. The interpreter gained `EntityContainerSpec.TagsField` (null ⇒ off), set to `"Tags"` (array profile) and `"tags"` (grouped profile); `SchemaInterpreter.BuildTable` captures the array after the description. Both schemas (`array.schema.json`, `grouped.schema.json`) declare the optional `tags` string-array.
- 2026-08-20 — **The edit path (029 discipline):** pure `ModelEdits.SetTableTags(table, tags, out rejected)` — trims, drops blanks, dedupes case-insensitively (first occurrence, order preserved), enforces **UML-identifier hygiene** (`IsValidTagName`: letters/digits/`_`/`-`, no leading digit) and returns the applied list with rejected names separately. `NodeVerbs.CanEditTags` added to the Entity preset, gating the inspector's tags editor.
- 2026-08-20 — **The UI:** the inspector gained a "Tags (comma-separated)" editor (Enter/LostFocus commit, like the description editor) that rewrites the box to the normalized applied list and surfaces rejected names on the **diagnostics log** (`ILogService`) — never a crash. The explorer table node shows a `[tag, tag]` suffix; `HoverSummary.ForTable` adds a `Tags: …` line so hover, node summary, and inspector never drift.
- 2026-08-20 — **Tests (+22, 205 total):** `ModelEditsTests` (normalize/validate/reject/null), `ModelFileTests` (container round-trip + absent stays absent), `SchemaInterpreterTests` (grouped + array capture, malformed tags is an issue not a silent drop, non-string item is an issue), `SchemaValidationTests` (both schemas accept tags / reject a non-string tag), `GraphNodeTests` (`CanEditTags` true for Entity, false for Element/Dependency/Group), `HoverSummaryTests` (tags line after description; omitted when absent).

## Results

- **Completed:** `037`
- **Deferred:** — (the backlog now holds `038`–`040`, unscheduled)
- **Notes:** Verified `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors / 0 warnings**; `dotnet test tests/ModelConsole.Tests` → **205/205 pass**. Manual verification (tag a table in the inspector, rejected names on the log, save/reload persistence, untagged models byte-identical) needs a human run — CLI launch runs on the agent's non-interactive desktop.
