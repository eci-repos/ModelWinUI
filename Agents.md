# Agents.md

Mandates and instructions for agents working in this repository. **Read this file and `docs/WORKLOG.md` before starting any work.**

## Purpose

This document is the instruction set for new agents. It defines the non-negotiable mandates, the documentation workflow, and the project context needed to work productively.

---

## Mandates

1. **Documentation is part of the work.** Every piece of work is planned in `docs/backlog/`, executed against a sprint record in `docs/sprints/`, and recorded in `docs/WORKLOG.md`. No work is "done" until the worklog is updated.
2. **Read before you act.** Start every session by reading `Agents.md` and `docs/WORKLOG.md`. Check the backlog for planned work before inventing new tasks.
3. **Instructions always go in `Agents.md`.** This file is the canonical instruction set for agents. When you receive a standing instruction (from the user or from another agent), record it here so it governs future work. Do not keep instructions only in conversation or in scattered files.
4. **One file per backlog item.** Planned work is a single file in `docs/backlog/` named `NNN-short-description.md`, created from `docs/backlog/_TEMPLATE.md`.
5. **Promote, don't delete.** Completed backlog items move to `docs/backlog/archive/`. Archived items are a permanent record — never delete them.
6. **One current sprint.** There is exactly one current sprint: `docs/sprints/CURRENT.md`. It holds the sprint in execution (or the most recently promoted sprint until the next starts). When a sprint is promoted, move `CURRENT.md` to `docs/sprints/archive/sprint-YYYY-MM-DD-<name>.md` and start the next sprint in `CURRENT.md`. Never create a second current-sprint file.
7. **Hand off explicitly.** When you stop, `docs/WORKLOG.md` must state what was done, what is pending, and anything the next agent needs to know.
8. **Report faithfully.** If a build fails, a test fails, or a step was skipped, say so plainly with the output. Do not claim work is complete when it is not.

---

## Documentation workflow

```
Plan    → docs/backlog/NNN-short-description.md   (from _TEMPLATE.md)
Execute → docs/sprints/CURRENT.md                 (the single current sprint)
Record  → docs/WORKLOG.md                          (done + pending, every session)
Promote → docs/backlog/archive/                    (completed backlog items)
          docs/sprints/archive/                    (promoted sprints, dated name)
```

See `docs/README.md` for the full workflow description.

---

## Project context

**EDAM Studio** — a data-model (ERD) visualization tool. This repo is the WinUI 3 desktop app (`ModelWinUI`), a fast-prototyping sibling of a planned Uno Platform WebAssembly app. Graphics code is written to be portable; the SkiaSharp-based stack is the one intended to move to the WebAssembly sibling unchanged.

### Build & run

```powershell
# Build (a platform is required — AnyCPU fails for this packaged WinUI app)
dotnet build src/Model.WinUI.Console/ModelWinUI.csproj -c Debug -p:Platform=x64

# Run from Visual Studio via the "ModelWinUI (Unpackaged)" launch profile
```

The project targets `net10.0-windows10.0.19041.0` (Windows App SDK 2.4.0, SkiaSharp 4.151.1) and runs unpackaged (`WindowsPackageType=None`). There are **no test projects**. Known harmless build warning: `NETSDK1198` (missing `.pubxml` publish profile).

### Architecture at a glance

- **UI:** `App → MainWindow → ModelEditorControl → {ModelPanelControl, DiagnosticsLogControl}`.
- **Two parallel graphics stacks** (do not confuse them — they share class names):
  - `ModelConsole.Graphics.GLibrary` — active, XAML `Shape`-based rendering on a `Canvas`; `GlContext` owns pointer handling; `GlGrip`/`GlHandle` implement the grabber interaction model.
  - `ModelConsole.Skia.GLibrary` — experimental, SkiaSharp rendering; intended for the WebAssembly sibling; keep it free of WinUI-specific dependencies.
- **Data model:** `Model/Data` (`TableInfo`, `ColumnInfo`, `ColumnList`, `ConstraintInfo`, `CatalogInfo`) with JSON round-tripping; sample fixtures in `Model/ModelData/Data_Table_Entity.cs`.
- **Diagnostics:** `ResultLog.DefaultLog` → static `LogMessageHandler` event → `DiagnosticsLogViewModel` → log panel.
- **MVVM:** `CommunityToolkit.Mvvm`; `Model/Helpers/ObservableObject`; `DiagnosticsLogViewModel` is the only view model so far.

See `CLAUDE.md` for the full architecture and conventions.

---

## Conventions

- Namespaces are `ModelConsole.*` (root namespace `ModelWinUI` only for `App`/`MainWindow`).
- Graphics classes use the `Gl` prefix (`GlContext`, `GlRectangle`, `GlOrthoPath`).
- Author's code style: 3-space indentation in graphics/data code, XML doc comments on members, `m_` prefix on private fields.
- When adding a graphics primitive, consider whether it needs a counterpart in both the `Graphics` and `Skia` stacks.

---

## Handoff protocol

When finishing a session or handing off to another agent:

1. Update `docs/WORKLOG.md` — append to **Done**, refresh **Pending**, add **Handoff notes**.
2. Move any completed backlog items to `docs/backlog/archive/`.
3. If a sprint ran, promote it: move `docs/sprints/CURRENT.md` to `docs/sprints/archive/sprint-YYYY-MM-DD-<name>.md` and start the next sprint in `CURRENT.md`.
4. Leave the repo in a buildable state; if it is not, say so in the worklog.
