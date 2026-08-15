using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConsole.Skia.GLibrary
{

    public class GlPalette
    {

    }

    public class GlPastelPalette : GlPalette
    {
        public const string DARK_GREEN = "#97C1A9";
        public const string GREEN = "#B7CFB7";
        public const string LIGHT_GREEN = "#CCE2CB";
        public const string LIGHT_GRAY = "#EAEAEA";

        public const string LIGHT_BLUE = "#C7DBDA";

        public static SKColor LightGreen = SKColor.Parse(LIGHT_GREEN);
    }

}
