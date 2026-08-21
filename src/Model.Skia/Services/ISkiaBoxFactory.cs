using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Skia.Services
{
   /// <summary>
   /// Creates and draws <see cref="GroupBox"/> primitives on the Skia
   /// graphics stack (backlog 039) — the factory contract the controls
   /// library resolves, mirroring <see cref="ISkiaTableFactory"/> so the
   /// Skia stack stays WinUI-free while still DI-wired.
   /// </summary>
   public interface ISkiaBoxFactory
   {
      /// <summary>
      /// Create and draw a collapsed group's box.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="x">x lower-left</param>
      /// <param name="y">y lower-left</param>
      /// <param name="group">the collapsed group's name</param>
      /// <param name="memberCount">the box's member table count</param>
      /// <param name="hovered">when true, the border draws the hovered accent
      /// (backlog 041); false (default) draws the rest-state border</param>
      /// <returns>the created GroupBox instance is returned</returns>
      GroupBox Create(GlFrame frame, float x, float y,
         string group, int memberCount, bool hovered = false);
   }
}
