# Current Sprint

**No sprint is currently in execution.**

The last sprint (2026-08-20, controls library and the pastel theme — backlog items `035` + `034`) was promoted to `docs/sprints/archive/sprint-2026-08-20-controls-library-and-theme.md`. `035` themed every control header, the toggle strips, the zoom toolbars, and the `MenuBar` from one pastel `ResourceDictionary` (`ControlTheme.xaml`, `{ThemeResource}` keys, palette kept out of the model's `#DCE9F7`/`#E2EFDA` families); `034` extracted the six ERD controls into the reusable `Model.Controls.WinUI` library — the theme dictionary now ships there (`ms-appx:///Model.Controls.WinUI/Themes/ControlTheme.xaml`), and the app no longer declares any `ModelConsole.*` namespace. Seven libraries in the collection; app + all six reference the seventh.

**The backlog holds `036`–`040`** (table-appearance footer + palette, tags, visibility projection, collapsed group boxes, UML notation/export), unscheduled. Next work: whichever the user picks — e.g. `036` (table footer + palette polish) is the smallest.
