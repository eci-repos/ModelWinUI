using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModelConsole.Palette;

namespace ModelConsole.Skia.GLibrary
{

    public class GlPalette
    {

    }

    public class GlPastelPalette : GlPalette
    {
        public const string DARK_GREEN = "#97C1A9";
        public const string GREEN = "#B7CFB7";
        public const string LIGHT_GRAY = "#EAEAEA";

        public const string LIGHT_BLUE = "#C7DBDA";

        // Connector colors — DodgerBlue, matching the app's XAML connectors
        // (endpoint markers are DodgerBlue there; the routed lines render in
        // DodgerBlue in the routing diagnostics too).
        public const string CONNECTOR_BLUE = "#1E90FF";

        // Hovered-connector line — SlateBlue, the analogous (violet) neighbor
        // of DodgerBlue on the color wheel. A hovered connector is drawn in
        // this hue so it pops out of the DodgerBlue rest-state connectors
        // while its endpoint markers stay DodgerBlue (the emphasized line is
        // the same #6A5ACD SlateBlue the XAML path applies).
        public const string CONNECTOR_HOVER_BLUE = "#6A5ACD";

        // Table-banner green, now sourced from the shared table palette
        // (backlog 036) — the old #CCE2CB hex retired with the Skia table
        // renderer's move to kind-based banner colors.
        public static SKColor LightGreen = SKColor.Parse(
            TablePalette.ReferenceBannerHex);

        public static SKColor ConnectorStroke = SKColor.Parse(CONNECTOR_BLUE);
        public static SKColor ConnectorFill = SKColor.Parse(CONNECTOR_BLUE);
        public static SKColor ConnectorHoverStroke = SKColor.Parse(CONNECTOR_HOVER_BLUE);
    }

}
