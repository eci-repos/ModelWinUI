using Model.Data;

namespace ModelConsole.Palette
{

   /// <summary>
   /// The one source of drawing-surface appearance (backlogs 036 + 041): the
   /// banner, the footer band, the body-row stripes, the table card border
   /// (rest + hovered), and the default drawing-surface background. Both ERD
   /// renderers consume these hex strings — the XAML stack parses them to a
   /// <c>Windows.UI.Color</c>, the Skia stack to an <c>SKColor</c> — so the
   /// two palettes cannot drift (before this library they were hardcoded
   /// independently in each renderer).
   /// </summary>
   public static class TablePalette
   {

      // Banner band (the pastel header) by table kind — the established
      // entity blue / reference-green family, unchanged from the previous
      // XAML header colors.
      public const string EntityBannerHex = "#DCE9F7";
      public const string ReferenceBannerHex = "#E2EFDA";

      // Footer band: a slightly deeper tone from the same family, so the
      // card reads banner → columns → footer and the bottom closes instead
      // of trailing off.
      public const string EntityFooterHex = "#C7D9F3";
      public const string ReferenceFooterHex = "#D3E7C4";

      // Body-row stripes: the alternating (non-white) row carries a whisper
      // of the banner hue so the kind reads from the body too; the plain row
      // stays white.
      public const string EntityStripeHex = "#F7FAFD";
      public const string ReferenceStripeHex = "#F6FAF3";
      public const string PlainRowHex = "#FFFFFF";

      /// <summary>
      /// Shared footer band height, in pixels — one F for both renderers so
      /// the XAML and Skia tables close identically.
      /// </summary>
      public const float FooterHeight = 20;

      // Table card border (backlog 041): a soft neutral line at rest; the
      // hovered table draws the DodgerBlue accent, thicker, so it reads at a
      // glance under the pointer (the same accent the selection outline and
      // connector emphasis use). One width/color pair for both renderers so
      // the XAML and Skia tables border identically.
      public const string BorderHex = "#5A5A5A";
      public const string HoveredBorderHex = "#1E90FF";
      public const float BorderWidth = 1.2f;
      public const float HoveredBorderWidth = 2.4f;

      /// <summary>
      /// Default drawing-surface (canvas) background, in hex. The renderer-bar
      /// drop-down (backlog 041) starts here and can override it at runtime;
      /// both renderers default to the same surface.
      /// </summary>
      public const string CanvasBackgroundHex = "#FFFFFF";

      /// <summary>Banner color hex for a table kind.</summary>
      /// <param name="kind">table kind</param>
      /// <returns>hex string, without the '#'</returns>
      public static string BannerHex(TableKind kind)
      {
         return kind == TableKind.ReferenceCode
            ? ReferenceBannerHex : EntityBannerHex;
      }

      /// <summary>Footer band color hex for a table kind.</summary>
      /// <param name="kind">table kind</param>
      /// <returns>hex string, without the '#'</returns>
      public static string FooterHex(TableKind kind)
      {
         return kind == TableKind.ReferenceCode
            ? ReferenceFooterHex : EntityFooterHex;
      }

      /// <summary>The tinted (alternating, non-white) row stripe for a kind.</summary>
      /// <param name="kind">table kind</param>
      /// <returns>hex string, without the '#'</returns>
      public static string StripeHex(TableKind kind)
      {
         return kind == TableKind.ReferenceCode
            ? ReferenceStripeHex : EntityStripeHex;
      }
   }

}
