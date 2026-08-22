using System;
using System.Collections.Generic;

namespace ModelConsole.Geometry
{

   /// <summary>
   /// Command kinds emitted by <see cref="RoundedPolyline"/>.
   /// </summary>
   public enum RoundedPathCommandType
   {
      MoveTo,
      LineTo,
      QuadraticTo
   }

   /// <summary>
   /// A framework-neutral drawing command for a visually rounded polyline.
   /// The command stream is a draw-only projection; it does not replace the
   /// original route points used for routing, labels, and hit testing.
   /// </summary>
   public readonly struct RoundedPathCommand
   {
      public RoundedPathCommandType Type { get; }
      public Point2 Point { get; }
      public Point2 ControlPoint { get; }

      private RoundedPathCommand(
         RoundedPathCommandType type, Point2 point, Point2 controlPoint)
      {
         Type = type;
         Point = point;
         ControlPoint = controlPoint;
      }

      public static RoundedPathCommand MoveTo(Point2 point)
      {
         return new RoundedPathCommand(
            RoundedPathCommandType.MoveTo, point, point);
      }

      public static RoundedPathCommand LineTo(Point2 point)
      {
         return new RoundedPathCommand(
            RoundedPathCommandType.LineTo, point, point);
      }

      public static RoundedPathCommand QuadraticTo(Point2 control, Point2 point)
      {
         return new RoundedPathCommand(
            RoundedPathCommandType.QuadraticTo, point, control);
      }
   }

   /// <summary>
   /// Converts an orthogonal polyline into a draw-only command stream with
   /// rounded bends. The input points are never changed.
   /// </summary>
   public static class RoundedPolyline
   {
      /// <summary>
      /// Build move/line/quadratic commands for the given route points.
      /// </summary>
      public static IReadOnlyList<RoundedPathCommand> Build(
         IReadOnlyList<Point2> points, double radius)
      {
         var commands = new List<RoundedPathCommand>();
         if (points == null || points.Count == 0)
         {
            return commands;
         }

         commands.Add(RoundedPathCommand.MoveTo(points[0]));
         if (points.Count == 1)
         {
            return commands;
         }

         Point2 current = points[0];
         for (int i = 1; i < points.Count - 1; i++)
         {
            Point2 previous = points[i - 1];
            Point2 corner = points[i];
            Point2 next = points[i + 1];

            if (CanRound(previous, corner, next, radius, out double r))
            {
               Point2 tangentIn = StepFrom(corner, previous, r);
               Point2 tangentOut = StepFrom(corner, next, r);
               if (!SamePoint(current, tangentIn))
               {
                  commands.Add(RoundedPathCommand.LineTo(tangentIn));
               }
               commands.Add(RoundedPathCommand.QuadraticTo(corner, tangentOut));
               current = tangentOut;
            }
            else if (!SamePoint(current, corner))
            {
               commands.Add(RoundedPathCommand.LineTo(corner));
               current = corner;
            }
         }

         Point2 last = points[points.Count - 1];
         if (!SamePoint(current, last))
         {
            commands.Add(RoundedPathCommand.LineTo(last));
         }
         return commands;
      }

      private static bool CanRound(
         Point2 previous, Point2 corner, Point2 next, double radius,
         out double resolvedRadius)
      {
         resolvedRadius = 0;
         if (radius <= 0 ||
             !IsAxisAligned(previous, corner) ||
             !IsAxisAligned(corner, next) ||
             IsCollinear(previous, corner, next))
         {
            return false;
         }

         double prevLength = Distance(previous, corner);
         double nextLength = Distance(corner, next);
         if (prevLength < 0.001 || nextLength < 0.001)
         {
            return false;
         }

         resolvedRadius = Math.Min(radius, Math.Min(prevLength / 2.0, nextLength / 2.0));
         return resolvedRadius >= 0.001;
      }

      private static Point2 StepFrom(Point2 origin, Point2 target, double distance)
      {
         double dx = target.X - origin.X;
         double dy = target.Y - origin.Y;
         if (Math.Abs(dx) >= Math.Abs(dy) && Math.Abs(dx) >= 0.001)
         {
            return new Point2(origin.X + Math.Sign(dx) * distance, origin.Y);
         }
         return new Point2(origin.X, origin.Y + Math.Sign(dy) * distance);
      }

      private static bool IsAxisAligned(Point2 a, Point2 b)
      {
         return SameX(a, b) || SameY(a, b);
      }

      private static bool IsCollinear(Point2 a, Point2 b, Point2 c)
      {
         return (SameX(a, b) && SameX(b, c)) ||
            (SameY(a, b) && SameY(b, c));
      }

      private static double Distance(Point2 a, Point2 b)
      {
         return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
      }

      private static bool SamePoint(Point2 a, Point2 b)
      {
         return SameX(a, b) && SameY(a, b);
      }

      private static bool SameX(Point2 a, Point2 b)
      {
         return Math.Abs(a.X - b.X) < 0.001;
      }

      private static bool SameY(Point2 a, Point2 b)
      {
         return Math.Abs(a.Y - b.Y) < 0.001;
      }
   }

}
