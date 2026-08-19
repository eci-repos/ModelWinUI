using System.Collections.Generic;

using ModelConsole.Geometry;
using ModelConsole.Skia.GLibrary;
using ModelConsole.Skia.Primitives;

namespace ModelConsole.Skia.Services
{
   /// <summary>
   /// Creates and draws <see cref="Connector"/> primitives on the Skia
   /// graphics stack.
   /// </summary>
   public class SkiaConnectorFactory : ISkiaConnectorFactory
   {
      /// <summary>
      /// Create and draw a connector from a routed polyline.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="points">polyline to draw</param>
      /// <returns>the created Connector instance is returned</returns>
      public Connector Create(GlFrame frame, IReadOnlyList<Point2> points)
      {
         return Connector.Draw(frame, points);
      }
   }
}
