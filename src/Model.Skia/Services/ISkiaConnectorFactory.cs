using System.Collections.Generic;

using ModelConsole.Geometry;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Skia.Services
{
   /// <summary>
   /// Creates and draws <see cref="Connector"/> primitives on the Skia
   /// graphics stack. The Skia <c>Connector</c> is a different type from the
   /// XAML <c>GlOrthoPath</c>, so it gets its own factory; this keeps the
   /// Skia stack free of WinUI dependencies while still being DI-wired.
   /// </summary>
   public interface ISkiaConnectorFactory
   {
      /// <summary>
      /// Create and draw a connector from a routed polyline.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="points">polyline to draw</param>
      /// <returns>the created Connector instance is returned</returns>
      Connector Create(GlFrame frame, IReadOnlyList<Point2> points);
   }
}
