using System.Collections.Generic;

using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.GLibrary.GlOrtho;
using ModelConsole.Graph;

namespace ModelConsole.Services
{
   /// <summary>
   /// Creates orthogonal connector paths between shapes on the XAML graphics
   /// stack. Replaces direct calls to the static <c>GlOrthoPath.Draw</c>.
   /// </summary>
   public interface IConnectorFactory
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
      GlOrthoPath Create(GlContext context,
         double x1, double y1, double x2, double y2,
         GlSide side = GlSide.Right);

      /// <summary>
      /// Create and draw an orthogonal connector from a pre-computed absolute
      /// polyline (obstacle-avoiding route).
      /// </summary>
      /// <param name="context">drawing context</param>
      /// <param name="points">absolute path points (at least two)</param>
      /// <returns>the created connector instance is returned</returns>
      GlOrthoPath CreateRouted(GlContext context, IReadOnlyList<Point2> points);
   }
}
