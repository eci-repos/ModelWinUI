using System;
using System.Globalization;

namespace ModelConsole.Palette
{

   /// <summary>
   /// Current-session visual style for a focused FK connector. Rest-state
   /// connectors keep their renderer defaults; this value is applied only to
   /// selected or hover-highlighted paths.
   /// </summary>
   public sealed class ConnectorStyle
   {
      public const string DefaultSelectedHex = "#6A5ACD";
      public const double DefaultSelectedWidth = 3.5;
      public const double MinSelectedWidth = 1.0;
      public const double MaxSelectedWidth = 8.0;

      /// <summary>The default selected/highlighted connector style.</summary>
      public static ConnectorStyle Default
      {
         get { return new ConnectorStyle(DefaultSelectedHex, DefaultSelectedWidth); }
      }

      /// <summary>Selected/highlighted connector line color as #RRGGBB.</summary>
      public string SelectedHex { get; }

      /// <summary>Selected/highlighted connector line width in pixels.</summary>
      public double SelectedWidth { get; }

      public ConnectorStyle(string selectedHex, double selectedWidth)
      {
         SelectedHex = NormalizeHex(selectedHex);
         SelectedWidth = ClampWidth(selectedWidth);
      }

      public ConnectorStyle WithSelectedHex(string selectedHex)
      {
         return new ConnectorStyle(selectedHex, SelectedWidth);
      }

      public ConnectorStyle WithSelectedWidth(double selectedWidth)
      {
         return new ConnectorStyle(SelectedHex, selectedWidth);
      }

      public static double ClampWidth(double width)
      {
         if (double.IsNaN(width) || double.IsInfinity(width))
         {
            return DefaultSelectedWidth;
         }
         return Math.Max(MinSelectedWidth, Math.Min(MaxSelectedWidth, width));
      }

      private static string NormalizeHex(string hex)
      {
         if (string.IsNullOrWhiteSpace(hex))
         {
            return DefaultSelectedHex;
         }

         string value = hex.Trim();
         if (value.StartsWith("#", StringComparison.Ordinal))
         {
            value = value.Substring(1);
         }

         if (value.Length != 6)
         {
            return DefaultSelectedHex;
         }

         for (int i = 0; i < value.Length; i++)
         {
            if (!Uri.IsHexDigit(value[i]))
            {
               return DefaultSelectedHex;
            }
         }

         return "#" + value.ToUpperInvariant();
      }

      public override string ToString()
      {
         return SelectedHex + " / " +
            SelectedWidth.ToString("0.#", CultureInfo.InvariantCulture) + " px";
      }
   }

}
