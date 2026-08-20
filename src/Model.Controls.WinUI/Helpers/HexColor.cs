using System;
using Windows.UI;

namespace ModelConsole.Controls.Helpers
{

   /// <summary>
   /// Parse a #RRGGBB hex string (e.g. from the shared
   /// <see cref="ModelConsole.Palette.TablePalette"/>) into a
   /// <see cref="Windows.UI.Color"/>. The controls' drawing-surface defaults
   /// and the renderer-bar presets all parse the same way, so the helper
   /// lives here once (backlog 041).
   /// </summary>
   public static class HexColor
   {

      /// <summary>
      /// Parse a #RRGGBB hex string into an opaque <see cref="Color"/>.
      /// The leading '#' is optional.
      /// </summary>
      /// <param name="hex">hex color string, e.g. "#FFFFFF"</param>
      /// <returns>the parsed color</returns>
      public static Color FromHex(string hex)
      {
         hex = hex.TrimStart('#');
         return Color.FromArgb(255,
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
      }

   }

}
