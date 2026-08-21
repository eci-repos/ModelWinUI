using System;

namespace ModelConsole.Palette
{

   /// <summary>
   /// The per-group pastel tints for the collapsed group boxes (backlog 039) —
   /// the single source both renderers parse (XAML → Windows.UI.Color,
   /// Skia → SKColor), so the two stacks color a group's box identically.
   /// The tints are chosen to stay out of the model's own color families
   /// (entity blue <c>#DCE9F7</c> / reference green <c>#E2EFDA</c>, backlog
   /// 035), and a group maps to one of them stably by name hash, so the
   /// same group gets the same pastel in both renderers on every layout.
   /// </summary>
   public static class GroupPalette
   {

      /// <summary>
      /// The pastel box family, distinct from the entity/reference banner
      /// hexes — lavender, blush, butter, sky, rose, sage.
      /// </summary>
      private static readonly string[] BoxHexes =
      {
         "#EFE3F5",  // lavender
         "#FBE6E0",  // blush
         "#FBF3DC",  // butter
         "#E3F0F7",  // sky
         "#F5E3E8",  // rose
         "#E4F2E3",  // sage
      };

      /// <summary>Name-compartment height of a collapsed group box.</summary>
      public const float HeaderHeight = 24;

      /// <summary>Body compartment height (the «package» + count lines).</summary>
      public const float BodyHeight = 44;

      /// <summary>Minimum box width so a short name still reads as a card.</summary>
      public const float MinWidth = 150;

      /// <summary>Horizontal padding inside the box around its text.</summary>
      public const float TextPadding = 16;

      /// <summary>The name-compartment tint hex for a group (stable by name).</summary>
      /// <param name="group">the group (tag) name</param>
      /// <returns>hex string, without the '#'</returns>
      public static string BoxHex(string group)
      {
         int index = 0;
         if (!string.IsNullOrEmpty(group))
         {
            // A stable FNV-style hash so the same name always picks the same
            // tint, and adjacent names scatter across the family.
            unchecked
            {
               int hash = 17;
               foreach (char c in group)
               {
                  hash = (hash * 31) + c;
               }
               index = Math.Abs(hash) % BoxHexes.Length;
            }
         }
         return BoxHexes[index];
      }
   }

}
