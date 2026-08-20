# Model.Console.Palette

The single source of drawing-surface colors and metrics (backlogs 036 + 041).
The two ERD renderers consumed their own hardcoded palettes before this
library — the XAML `Table` carried its banner colors as statics, the Skia
stack its own `GlPastelPalette`. Now both parse the same hex strings here, so
entity vs reference-code tables read identically across the XAML model and
the Skia render.

**What it holds**

- `TablePalette.BannerHex(kind)` — the pastel banner (entity blue / reference green).
- `TablePalette.FooterHex(kind)` — a slightly deeper tone from the same family for the closing footer band.
- `TablePalette.StripeHex(kind)` — the alternating body-row stripe; the plain row stays white.
- `TablePalette.FooterHeight` — the one footer budget (in pixels) both renderers use.
- `TablePalette.BorderHex`/`BorderWidth` + `HoveredBorderHex`/`HoveredBorderWidth` (backlog 041) — the table card border at rest and when hovered (the DodgerBlue accent, thicker).
- `TablePalette.CanvasBackgroundHex` — the default drawing-surface background both renderers start from (the renderer-bar drop-down can override it at runtime).

The hex strings are the point of truth. XAML converts with `Color.FromArgb`,
Skia with `SKColor.Parse`.

The hex strings are the point of truth. XAML converts with `Color.FromArgb`,
Skia with `SKColor.Parse`.

**Dependencies:** `Model.Data` (for `TableKind`).

**Usage**

```csharp
// XAML
var banner = FromHex(TablePalette.BannerHex(kind));   // → Windows.UI.Color
// Skia
var footer = SKColor.Parse(TablePalette.FooterHex(kind));
```
