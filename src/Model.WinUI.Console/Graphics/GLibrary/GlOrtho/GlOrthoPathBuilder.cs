using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;

namespace ModelConsole.Graphics.GLibrary.GlOrtho
{

    /// <summary>
    /// Helper to define orthogonal line path with rounded-edges by traversing
    /// through it.
    /// </summary>
    public class GlOrthoPathBuilder
    {
        protected double _edgeRadius = 10;

        private Point _nextPoint;
        public GlOrthoPathShape Shape { get; set; } =
           new GlOrthoPathShape();

        public Point NextPoint
        {
            get { return _nextPoint; }
        }

        public double MiddleLength { get; set; } = 30;

        /// <summary>
        /// Draw first segment of the connector starting at the source x1,y1 
        /// point.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="side"></param>
        /// <param name="direction"></param>
        public void AddLine(
           double x1, double y1, GlSide side, GlDirection direction)
        {
            double l = MiddleLength - _edgeRadius;
            double x2 = x1, y2 = y1, x = x1, y = y1;

            // draw starting line from existing shape
            switch (side)
            {
                case GlSide.Right:
                    x2 = x1 + l;
                    x = x1 + MiddleLength;
                    break;
                case GlSide.Bottom:
                    y2 = y1 + l;
                    y = y1 + MiddleLength;
                    break;
            }

            // perpendicular direction
            l = _edgeRadius;
            double x3 = x2, y3 = y2;
            switch (direction)
            {
                case GlDirection.Left:
                    x3 = x2 - l;
                    y3 = y2 + l;
                    break;
                case GlDirection.Right:
                    x3 = x2 + l;
                    y3 = y2 + l;
                    break;
                case GlDirection.Up:
                    x3 = x2 + l;
                    y3 = y2 + l;
                    break;
                case GlDirection.Down:
                    x3 = x2 + l;
                    y3 = y2 - l;
                    break;
            }

            // draw line
            var p1 = new Point(x1, y1);
            var p2 = new Point(x2, y2);
            var s1 = new GlOrthoPathItem(p1, p2);
            Shape.Items.Add(s1);

            // draw arc
            var p3 = new Point(x, y);
            var p4 = new Point(x3, y3);
            var s2 = new GlOrthoPathItem(p2, p3, p4);
            Shape.Items.Add(s2);

            _nextPoint = new Point(x3, y3);
        }

        /// <summary>
        /// Draw 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="direction"></param>
        public void AddLine(double x1, double y1, GlDirection direction)
        {
            // perpendicular direction
            double l = _edgeRadius, x = x1, y = y1;
            bool horizontal = true;
            double x3 = x1, y3 = y1;
            switch (direction)
            {
                case GlDirection.Left:
                    horizontal = false;
                    x3 = x1 + l;
                    y3 = y1;
                    x = x1;
                    break;
                case GlDirection.Right:
                    horizontal = false;
                    x3 = x1 - l;
                    y3 = y1;
                    break;
                case GlDirection.Up:
                    x3 = x1;
                    y3 = y1 - l;
                    y = y1;
                    break;
                case GlDirection.Down:
                    x3 = x1;
                    y3 = y1 + l;
                    break;
            }

            // draw a line
            var p1 = _nextPoint;
            var p2 = new Point(x3, y3);
            var s2 = new GlOrthoPathItem(p1, p2);
            Shape.Items.Add(s2);

            // draw an arc
            var p3 = new Point(x, y);
            var p4 = horizontal ? new Point(x + l, y1) : new Point(x1, y + l);
            var s1 = new GlOrthoPathItem(p2, p3, p4);
            Shape.Items.Add(s1);

            _nextPoint = p3;
        }

        public void AddLine(double x, double y)
        {
            // draw a line
            var p1 = _nextPoint;
            var p2 = new Point(x, y);
            var s1 = new GlOrthoPathItem(p1, p2);
            Shape.Items.Add(s1);
        }

    }

}
