# Sprint 2026-08-17 — Non-trivial sample models

> Executed copy of the sprint. Backlog item: `docs/backlog/005-non-trivial-sample-models.md`.

## Dates

- **Start:** 2026-08-17
- **End:** (TBD)

## Scope

Backlog items in this sprint (reference by number):

- [x] `005` — Non-trivial sample models: ship sample models showing the tool's capabilities

## Execution Log

- 2026-08-17 — Sprint defined from backlog item `005`. User scope decision: **Ship JSON + Open Sample menu** — export the existing 50-table PublicSafety schema to a shipped JSON file, author one new non-trivial sample in a different domain (a ~20-table library schema), ship both as app content, and add a File → Open Sample submenu.
- 2026-08-17 — `LibrarySchema` (ModelGraphLibrary, `ModelConsole.ModelData`): a 20-table / 30-FK library & books schema mirroring the `PublicSafetySchema` builder pattern — 7 `Ref*` reference tables (code key + `Description`) + 13 entity tables (Address, Publisher, Author, LibraryBranch, Book, BookAuthor, BookCopy, Patron, Loan, Hold, Fine, Staff, Reservation). All FK `ReferencedColumnName`s null → parent-PK default; four FKs to `RefBookStatus` exercise `ConnectorAnchors.FanOut`.
- 2026-08-17 — `SampleModels` registry (`ModelConsole.ModelData`): `SampleModel` (Name / Description / FileName / Tables) + `SampleModels.All` listing Public Safety + Library — the single source of truth for the menu and the tests.
- 2026-08-17 — Shipped JSON files: `ModelGraphLibrary/Samples/PublicSafety.json` + `Library.json`, generated from the fixtures via `ModelFile.ToJson` (a one-off generator test wrote them, then was deleted). Both the app and the test project include them as content (`Link="Samples\…"`, `CopyToOutputDirectory="PreserveNewest"`).
- 2026-08-17 — `MainWindow` gains **File → Open Sample**: a `MenuFlyoutSubItem` after "Open Model…" (with a separator); items built in code-behind from `SampleModels.All` (each `Tag` = file name); clicking loads `AppContext.BaseDirectory/Samples/<file>` via `ModelFile.Load` and feeds both renderers. The shared "load → both renderers" logic is extracted into a `LoadModel` helper used by both `OpenModel_Click` and `OpenSample_Click`; load errors surface in a shared `ShowLoadErrorAsync` dialog.
- 2026-08-17 — `SampleModelTests` (6): shipped samples load + are valid (non-empty, every table has a PK, `FkEdgeExtractor.Extract` reports no issues), shipped JSON matches the fixture (sync guard, line endings normalized), PublicSafety is 50 tables / 74 FKs, Library is ≥ 15 tables / ≥ 15 FKs.
- 2026-08-17 — Verified: app project builds 0 errors / 0 warnings; `SampleModelTests` 6/6 pass; the shipped JSON files land in the app output `Samples/`. (Full-solution `--no-incremental` build + full test suite + launch check pending.)

## Results

- **Completed:** `005`
- **Notes:**
  - The samples are **generated from code fixtures**, not hand-maintained JSON — `SampleModelTests.ShippedJsonMatchesFixture` keeps the checked-in files in sync with the fixtures.
  - Adding a sample to `SampleModels.All` automatically adds it to the File → Open Sample menu (the menu is built from the registry).
  - The roadmap is now complete: all four roadmap items (base library, UI controls, sample models, assess next steps) are done.
