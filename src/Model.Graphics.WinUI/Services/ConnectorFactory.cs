using System.Collections.Generic;

using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.GLibrary.GlOrtho;
using ModelConsole.Geometry;

namespace ModelConsole.Graphics.Services
{
   /// <summary>
   /// Creates orthogonal connector paths between shapes on the XAML graphics
   /// stack.
   /// </summary>
   public class ConnectorFactory : IConnectorFactory
   {
      /// <summary>
      /// Create and draw an orthogonal connector.
      /// </summary>
      /// <param name="context">drawing context</param>
      /// <param name="x1">start x</param>
      /// <param name="y1">start y</param>
      /// <param name="x2">end x</param>
      /// <param name="y2">end y</param>
      /// <param name="side">direction the path steers (default: right)</param>
      /// <returns>the created connector instance is returned</returns>
      public GlOrthoPath Create(GlContext context,
         double x1, double y1, double x2, double y2,
         GlSide side = GlSide.Right)
      {
         return GlOrthoPath.Draw(context, x1, y1, x2, y2, side);
      }

      /// <summary>
      /// Create and draw an orthogonal connector from a pre-computed absolute
      /// polyline (obstacle-avoiding route).
      /// </summary>
      /// <param name="context">drawing context</param>
      /// <param name="points">absolute path points (at least two)</param>
      /// <returns>the created connector instance is returned</returns>
      public GlOrthoPath CreateRouted(GlContext context, IReadOnlyList<Point2> points)
      {
         return GlOrthoPath.DrawRouted(context, points);
      }
   }
}
