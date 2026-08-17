# 005 — Non-trivial sample models

## Summary

The README roadmap's item 3: *"Prepare non-trivial sample models that will be shipped with different versions to show the tool capabilities."* Today the app can load a model from JSON (File → Open, backlog 004) but ships **no sample models** — the only model is the hardcoded 50-table `PublicSafetySchema` fixture in code. This item ships real sample model files with the app and makes them discoverable via a **File → Open Sample** menu.

## Goals

- [x] Ship the existing 50-table public-safety schema as a JSON model file.
- [x] Author a second non-trivial sample in a different domain (a ~20-table library/books schema) to show the tool's breadth.
- [x] Ship both samples as app content (copied to the app output) and expose them through a File → Open Sample submenu.
- [x] The shipped files are validated by tests and kept in sync with the code fixtures they are generated from.

## Scope

**In scope:**
- `LibrarySchema` (ModelGraphLibrary, `ModelConsole.ModelData`) — a new 20-table / 30-FK library & books schema mirroring the `PublicSafetySchema` builder pattern (`T()`/`C()` helpers, `SCHEMA` const). A mix of entity tables and `Ref*` reference-code tables (so the pastel header colors show), with several FKs to the same table (`RefBookStatus`) to exercise `ConnectorAnchors.FanOut`. All FK `ReferencedColumnName`s are null → the parent-PK default rule.
- `SampleModels` (ModelGraphLibrary, `ModelConsole.ModelData`) — a registry of shipped samples (`SampleModel`: Name / Description / FileName / Tables). Single source of truth for the menu and the tests.
- Shipped JSON files — `ModelGraphLibrary/Samples/PublicSafety.json` + `Library.json`, **generated from the fixtures** via `ModelFile.ToJson` and checked in as real artifacts. Both the app and the test project include them as content (`Link="Samples\…"`, `CopyToOutputDirectory="PreserveNewest"`).
- `MainWindow` — a **File → Open Sample** submenu (after "Open Model…", with a separator); items built in code-behind from `SampleModels.All`; clicking one loads `AppContext.BaseDirectory/Samples/<file>` via `ModelFile.Load` and feeds both renderers. The shared "load → both renderers" logic is extracted into a `LoadModel` helper used by both `OpenModel_Click` and `OpenSample_Click`.
- Unit tests — `SampleModelTests` (6): shipped samples load + are valid (non-empty, every table has a PK, `FkEdgeExtractor.Extract` reports no issues), shipped JSON matches the fixture (sync guard), PublicSafety is 50 tables / 74 FKs, Library is ≥ 15 tables / ≥ 15 FKs.
- Docs: backlog item, sprint record, WORKLOG, functionality map, CLAUDE.md.

**Out of scope:**
- Saving a model to JSON (005 ships samples; a Save UI is a separate future item).
- Hand-maintained JSON — the samples are generated from code fixtures and kept in sync by a test.
- Sample thumbnails/preview in the menu.

## Approach / Notes

- **Samples are generated, not hand-written:** a one-off generator test wrote the JSON from the fixtures via `ModelFile.ToJson` (then was deleted). `SampleModelTests.ShippedJsonMatchesFixture` compares the checked-in file (line endings normalized) against `ModelFile.ToJson(fixture)`, so the files can never drift from the code.
- **Where the files live:** `ModelGraphLibrary/Samples/` — the library owns the fixtures, so the shipped artifacts sit next to them. The app links them into its output `Samples/` folder; the test project links them into the test output so tests validate the exact shipped bytes.
- **Menu built from the registry:** `MainWindow`'s ctor populates the Open Sample submenu from `SampleModels.All` (each item's `Tag` = the file name), so adding a sample to the registry automatically adds it to the menu.
- **Library schema design:** 7 `Ref*` reference tables (code key + `Description`) + 13 entity tables (Address, Publisher, Author, LibraryBranch, Book, BookAuthor, BookCopy, Patron, Loan, Hold, Fine, Staff, Reservation). 30 FK edges; Book→RefBookStatus, BookCopy→RefBookStatus, Hold→RefBookStatus, Reservation→RefBookStatus all fan out to the same parent.

## Definition of Done

- [x] `dotnet build ModelWinUI.sln -p:Platform=x64` → **0 errors, 0 warnings**.
- [x] `dotnet test tests/ModelGraphLibrary.Tests/ModelGraphLibrary.Tests.csproj -c Debug` → all pass (66 existing + new `SampleModelTests`).
- [x] App launches unpackaged and stays running; the shipped JSON files exist in the app output `Samples/`; File → Open Sample lists both samples and opening one renders it in both renderers (visual pass needs a manual look on the agent's non-interactive desktop; `SampleModelTests` covers the load logic).
- [x] XAML path unchanged: zoom/pan/drag/inspector/014 toggle/renderer bar all intact.

## Status

- **State:** In progress (sprint 2026-08-17, item 005)
- **Sprint:** (TBD)
- **Completed:** (TBD)
