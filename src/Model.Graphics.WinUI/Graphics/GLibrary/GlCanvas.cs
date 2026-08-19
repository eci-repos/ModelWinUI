using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace ModelConsole.Graphics.GLibrary
{

   /// <summary>
   /// A Canvas that exposes the protected <see cref="UIElement.ProtectedCursor"/>
   /// as a public property, so the graphics context can swap the cursor (hand
   /// over empty space, grabbing while panning) without reflection.
   /// </summary>
   public class GlCanvas : Canvas
   {
      public InputCursor Cursor
      {
         get { return ProtectedCursor; }
         set { ProtectedCursor = value; }
      }
   }

}
