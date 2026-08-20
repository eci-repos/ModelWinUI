using Microsoft.UI.Input;
using SkiaSharp.Views.Windows;

namespace ModelConsole.Controls
{

   /// <summary>
   /// An <see cref="SKXamlCanvas"/> that exposes the protected
   /// <see cref="UIElement.ProtectedCursor"/> as a public property, so the
   /// Skia panel can swap the cursor (hand over empty space, move while
   /// panning) without reflection — the Skia-path counterpart of
   /// <see cref="ModelConsole.Graphics.GLibrary.GlCanvas"/>.
   /// </summary>
   public class SkiaCanvasView : SKXamlCanvas
   {
      public InputCursor Cursor
      {
         get { return ProtectedCursor; }
         set { ProtectedCursor = value; }
      }
   }

}
