using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;

namespace ModelConsole.Graphics.GLibrary.GlOrtho
{

    public class GlOrthoPathItem
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public Point Point3 { get; set; }
        public GlOrthoPathItemType Type { get; set; }

        public GlOrthoPathItem(
           Point p1, Point p2)
        {
            Point1 = p1;
            Point2 = p2;
            Type = GlOrthoPathItemType.Line;
        }

        /// <summary>
        /// Arc information that will be based on a Bezier curve that expect
        /// 3 points.
        /// </summary>
        /// <param name="p1">first point</param>
        /// <param name="p2">middle point</param>
        /// <param name="p3">last point</param>
        public GlOrthoPathItem(
           Point p1, Point p2, Point p3)
        {
            Point1 = p1;
            Point2 = p2;
            Point3 = p3;
            Type = GlOrthoPathItemType.Arc;
        }
    }

}
