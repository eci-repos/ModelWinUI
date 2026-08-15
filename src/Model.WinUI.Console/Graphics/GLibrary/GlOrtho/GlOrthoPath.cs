using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using Model.Data;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Windows.Devices.Bluetooth.Advertisement;
using Microsoft.UI;
using Microsoft.UI.Input;
using System.Reflection;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace ModelConsole.Graphics.GLibrary.GlOrtho
{

   /// <summary>
   /// Helper class to draw orthogonal lines with rounded edges.
   /// </summary>
   public class GlOrthoPath : GlObject, IGlGrip
   {
      protected Path _path = new Path();
      protected GlOrthoPathShape _orthoShape;
      protected double MiddleLength = -1;

      public double X { get; set; }
      public double Y { get; set; }
      public double Z { get; set; } = 0;

      protected List<GlGrip> _gripNodes = new List<GlGrip>();
      protected GlSide _side = GlSide.Right;

      private bool _Selected;
      public override bool Selected
      {
         get { return _Selected; }
         set
         {
            SetSelected(value);
         }
      }

      public Path Path
      {
         get { return _path; }
      }

      public Shape Shape
      {
         get { return _path; }
      }

      public GlOrthoPath() : base(null)
      {
         m_Instance = _path;
         _path.Stroke = new SolidColorBrush(Colors.Black);
         _path.StrokeThickness = 1;
      }

      #region -- 4.00 - Shape Selected

      /// <summary>
      /// Set Shape Selected or No...
      /// </summary>
      /// <param name="selected">true to mark it a selected</param>
      private void SetSelected(bool selected)
      {
         _Selected = selected;
         foreach(GlGrip g in _gripNodes)
         {
            g.SetPosition(g.X, g.Y);
            g.Selected = selected;
         }
      }

      #endregion
      #region -- 4.00 - Shape and Grip Positioning

      /// <summary>
      /// Get the supported Grip nodes.
      /// </summary>
      /// <returns>a list of nodes are returned</returns>
      public List<GlGrip> GetGripNodes()
      {
         return _gripNodes;
      }

      /// <summary>
      /// See if point is on top of a listed Grip and if so return it.
      /// </summary>
      /// <param name="point">point to test</param>
      /// <returns>returns the selected grip else null</returns>
      public GlGrip GetGripNode(Point point)
      {
         foreach(GlGrip grip in _gripNodes)
         {
            if (grip.PointInShape(point))
            {
               return grip;
            }
         }
         return null;
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="delta">DX and DY distance to move</param>
      public override void DeltaMove(Point? delta = null)
      {
         // move object
         if (delta.HasValue)
         {
            X += delta.Value.X;
            Y += delta.Value.Y;

            // move the grips to this relative (delta) position.
            GlGrip.Move(_gripNodes, delta);
         }

         Canvas.SetLeft(_path, X);
         Canvas.SetTop(_path, Y);
      }

      /// <summary>
      /// Move path to another position.
      /// </summary>
      /// <param name="point"></param>
      public override void Move(Point? point = null)
      {
         if (point.HasValue)
         {
            X = point.Value.X;
            Y = point.Value.Y;
            DeltaMove(null);
         }
      }

      /// <summary>
      /// Given a Grip node set its middle length (distance from the origin).
      /// </summary>
      /// <param name="grip">grip node to consider</param>
      private void SetMiddleLength(GlGrip grip, GlSide side)
      {
         GlGrip l = _gripNodes[0];
         GlGrip r = _gripNodes[1];

         double x1, y1, x2, y2, ml;

         // get bounding box
         if (l.X > r.X)
         {
            x1 = r.X;
            x2 = l.X;
         }
         else
         {
            x1 = l.X;
            x2 = r.X;
         }

         if (l.Y > r.Y)
         {
            y1 = r.Y;
            y2 = l.Y;
         }
         else
         {
            y1 = l.Y;
            y2 = r.Y;
         }

         // if point is not inside the bounding box then return
         if (grip.X < x1 || grip.X > x2)
         {
            return;
         }
         if (grip.Y < y1 || grip.Y > y2)
         {
            return;
         }

         // calculate middle length
         if (side == GlSide.Right)
         {
            ml = grip.X - x1;
         }
         else
         {
            ml = grip.Y - y1;
         }

         MiddleLength = ml;
      }

      /// <summary>
      /// Reshape the Shape while managing the Grip that allow to do so.
      /// </summary>
      /// <param name="node">Node use to hold the Grip on a known Shape 
      /// Gripable points.
      /// </param>
      public override void Reshape(object node)
      {
         if (node is GlGrip)
         {
            GlGrip grip = (GlGrip)node, sgrip = null;
            if (_gripNodes != null)
            {
               GlGrip anchor = null;
               if (grip.Snapped(_gripNodes[0]))
               {
                  anchor = _gripNodes[1];
                  sgrip = _gripNodes[0];
                  GetPath(sgrip.X, sgrip.Y, anchor.X, anchor.Y, _side);
               }
               else if (grip.Snapped(_gripNodes[1]))
               {
                  anchor = _gripNodes[0];
                  sgrip = _gripNodes[1];
                  GetPath(anchor.X, anchor.Y, sgrip.X, sgrip.Y, _side);
               }
               else if (grip.Snapped(_gripNodes[2]))
               {
                  SetMiddleLength(grip, _side);
                  GetPath(_gripNodes[0].X, _gripNodes[0].Y, 
                     _gripNodes[1].X, _gripNodes[1].Y, _side);
               }
               else
               {
                  return;
               }

               Draw(Context, true);
            }
         }
      }

      /// <summary>
      /// Manage pointer event.
      /// </summary>
      /// <param name="poinerEvent"></param>
      public override void PointerEvent(
         GlPointerEvent poinerEvent, PointerPoint point = null)
      {
         Context.SetPointerHandle(_path, point);
      }

      #endregion
      #region -- 4.00 - Shape - Path Definition and Drawing Support

      /// <summary>
      /// Get path for orthogonal rounded edges lines from a point to another.
      /// Generally this will be the starting point for drawing orthogonal 
      /// rounded edges lines that connects two shapes.  The line will start or
      /// ends from the bounding box of 2 shapes (such as a rectangle) and 
      /// therefore the side of each source and target is calculated to
      /// figure out the best way to draw the ortho-lines.
      /// </summary>
      /// <param name="x1">start x</param>
      /// <param name="y1">start y</param>
      /// <param name="x2">end x</param>
      /// <param name="y2">end y</param>
      /// <param name="side">in what direction the path will steer?
      /// (default: right)</param>
      public GlOrthoPathShape GetPath(
         double x1, double y1, double x2, double y2, GlSide side = GlSide.Right)
      {
         // prepare the path
         GlDirection direction;
         GlOrthoPathBuilder path = new GlOrthoPathBuilder();
         if (side == GlSide.Left || side == GlSide.Right)
         {
            // always keep x1 lower than x2
            if (x2 < x1)
            {
               GlObject.Swap(ref x1, ref y1, ref x2, ref y2);
            }

            // always move to the right since x1 is always lower then x2
            side = GlSide.Right;
            direction = y1 < y2 ? GlDirection.Up : GlDirection.Down;
         }
         else
         {
            // always keep y1 lower than y2
            if (y2 < y1)
            {
               GlObject.Swap(ref x1, ref y1, ref x2, ref y2);
            }
            side = GlSide.Bottom;
            direction = x1 < x2 ? GlDirection.Right : GlDirection.Left;
         }
         _side = side;

         // set the original path position
         X = x1;
         Y = y1;

         // move x1,y1 and x2, y2 to the origin
         x1 = 0;
         y1 = 0;
         x2 = x2 - X;
         y2 = y2 - Y;

         // define first grip point
         if (_gripNodes.Count == 0)
         {
            GlGrip fgrip = new GlGrip();
            fgrip.X = x1;
            fgrip.Y = y1;
            fgrip.Create(Context);
            fgrip.Tag = _path;
            _gripNodes.Add(fgrip);
         }
         else
         {
            _gripNodes[0].X = x1;
            _gripNodes[0].Y = y1;
         }

         // what is the distance to the inflection point to steer direction?
         path.MiddleLength = MiddleLength <= 0 ?
            ((side == GlSide.Right ? x2 - x1 : y2 - y1) / 2.0) :
            MiddleLength;

         // add line from source shape into the path
         path.AddLine(x1, y1, side, direction);

         // in what direction the path will go?
         double x3, y3;
         if (direction == GlDirection.Up)
         {
            x3 = path.NextPoint.X;
            y3 = y2;
         }
         else if (direction == GlDirection.Down)
         {
            x3 = path.NextPoint.X;
            y3 = y2;
         }
         else
         {
            y3 = path.NextPoint.Y;
            double al = Math.Abs(path.NextPoint.X - x2);
            x3 = x2;
         }

         // add next lines
         path.AddLine(x3, y3, direction);
         path.AddLine(x2, y2);

         // define last grip point
         if (_gripNodes.Count == 1)
         {
            GlGrip lgrip = new GlGrip();
            lgrip.X = x2;
            lgrip.Y = y2;
            lgrip.Create(Context);
            lgrip.Tag = _path;
            _gripNodes.Add(lgrip);
         }
         else
         {
            _gripNodes[1].X = x2;
            _gripNodes[1].Y = y2;
         }

         // define the middle grip
         if (_gripNodes.Count == 2)
         {
            GlGrip lgrip = new GlGrip();
            lgrip.X = x1 + (side == GlSide.Right ?
               path.MiddleLength : (x2 - x1) / 2.0);
            lgrip.Y = y1 + (side == GlSide.Bottom ?
               path.MiddleLength : (y2 - y1) / 2.0);
            lgrip.Create(Context);
            lgrip.Tag = _path;
            _gripNodes.Add(lgrip);
         }
         else
         {
            _gripNodes[2].X = x1 + (side == GlSide.Right ?
               path.MiddleLength : (x2 - x1) / 2.0); ;
            _gripNodes[2].Y = y1 + (side == GlSide.Bottom ?
               path.MiddleLength : (y2 - y1) / 2.0); ;
         }

         // setup the current shape
         _orthoShape = path.Shape;
         return path.Shape;
      }

      /// <summary>
      /// Draw Path and add it to the Canvas.
      /// </summary>
      /// <param name="context">the drawing context</param>
      public void Draw(GlContext context, bool reshaping = false)
      {
         Context = context;

         var geometry = new PathGeometry();
         var figure = new PathFigure();
         Point lastPoint = new Point();

         foreach (var i in _orthoShape.Items)
         {
            if (i.Type == GlOrthoPathItemType.Line)
            {
               var lgeo = new LineSegment();
               lgeo.Point = i.Point2;
               if (figure.Segments.Count == 0)
               {
                  figure.StartPoint = i.Point1;
                  figure.Segments.Add(lgeo);
               }
               else
               {
                  figure.Segments.Add(lgeo);
                  lastPoint = i.Point2;
               }
            }
            else
            {
               var bgeo = new BezierSegment();
               bgeo.Point1 = i.Point1;
               bgeo.Point2 = i.Point2;
               bgeo.Point3 = i.Point3;
               figure.Segments.Add(bgeo);
            }
         }

         geometry.Figures.Add(figure);
         _path.Data = geometry;
         _path.Tag = this;

         // move the path to the original given position
         DeltaMove();

         Point point = new Point(X, Y);
         GlGrip.Move(_gripNodes, point);

         if (!reshaping)
         {
            context.Instance.Children.Add(_path);
         }
      }

      /// <summary>
      /// Draw orthogonal lines.
      /// </summary>
      /// <param name="context">drawing canvas and context</param>
      /// <param name="x1">start x</param>
      /// <param name="y1">start y</param>
      /// <param name="x2">end x</param>
      /// <param name="y2">end y</param>
      /// <returns>orthogonal lines instance is returned</returns>
      public static GlOrthoPath Draw(GlContext context,
         double x1, double y1, double x2, double y2, GlSide side = GlSide.Right)
      {
         GlOrthoPath shape = new GlOrthoPath();
         shape.Context = context;
         shape.GetPath(x1, y1, x2, y2, side);
         shape.Draw(context);

         return shape;
      }

      #endregion

   }

}
