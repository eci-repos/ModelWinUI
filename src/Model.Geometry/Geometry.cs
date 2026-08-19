using System;

namespace ModelConsole.Geometry
{

   /// <summary>
   /// Portable 2D point. Kept free of Windows.Foundation so the graph
   /// modules stay usable from plain net10.0 (unit tests) and the
   /// WinUI/WASM renderers alike.
   /// </summary>
   public readonly struct Point2 : IEquatable<Point2>
   {
      public double X { get; }
      public double Y { get; }

      public Point2(double x, double y)
      {
         X = x;
         Y = y;
      }

      public bool Equals(Point2 other)
      {
         return X == other.X && Y == other.Y;
      }

      public override bool Equals(object obj)
      {
         return obj is Point2 p && Equals(p);
      }

      public override int GetHashCode()
      {
         return (X, Y).GetHashCode();
      }

      public override string ToString()
      {
         return "(" + X + ", " + Y + ")";
      }
   }

   /// <summary>
   /// Portable axis-aligned rectangle (X/Y is the top-left corner).
   /// </summary>
   public readonly struct Rect2 : IEquatable<Rect2>
   {
      public double X { get; }
      public double Y { get; }
      public double Width { get; }
      public double Height { get; }

      public double Left { get { return X; } }
      public double Top { get { return Y; } }
      public double Right { get { return X + Width; } }
      public double Bottom { get { return Y + Height; } }

      public Point2 Center { get { return new Point2(X + Width / 2.0, Y + Height / 2.0); } }

      public Rect2(double x, double y, double width, double height)
      {
         X = x;
         Y = y;
         Width = width;
         Height = height;
      }

      /// <summary>
      /// True when the point is on or inside the rectangle (closed test).
      /// </summary>
      public bool Contains(Point2 p)
      {
         return p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;
      }

      /// <summary>
      /// True when the point is strictly inside the rectangle (open test).
      /// </summary>
      public bool ContainsStrict(Point2 p)
      {
         return p.X > Left && p.X < Right && p.Y > Top && p.Y < Bottom;
      }

      /// <summary>
      /// True when this rectangle shares any interior area with the other.
      /// </summary>
      public bool Intersects(Rect2 other)
      {
         return Left < other.Right && Right > other.Left &&
            Top < other.Bottom && Bottom > other.Top;
      }

      /// <summary>
      /// Grow the rectangle by the given amount on every side.
      /// </summary>
      public Rect2 Inflate(double amount)
      {
         return new Rect2(X - amount, Y - amount, Width + amount * 2.0, Height + amount * 2.0);
      }

      /// <summary>
      /// True when the open segment a-b (endpoints excluded) passes through
      /// the strict interior of the rectangle. A segment that only touches a
      /// boundary edge - including one that starts exactly on the edge - does
      /// not count unless it then travels inside.
      /// </summary>
      public static bool SegmentCrossesInterior(Point2 a, Point2 b, Rect2 rect)
      {
         double dx = b.X - a.X;
         double dy = b.Y - a.Y;

         // t values (as open intervals) where the point is strictly inside
         // the rectangle on each axis, then intersect with (0, 1).
         if (!GetStrictInterval(a.X, dx, rect.Left, rect.Right, out double loX, out double hiX))
         {
            return false;
         }
         if (!GetStrictInterval(a.Y, dy, rect.Top, rect.Bottom, out double loY, out double hiY))
         {
            return false;
         }

         double lo = Math.Max(loX, loY);
         double hi = Math.Min(hiX, hiY);
         if (lo >= hi)
         {
            return false;
         }

         double oLo = Math.Max(lo, 0.0);
         double oHi = Math.Min(hi, 1.0);
         return oLo < oHi;
      }

      /// <summary>
      /// Interval of t where p + t*d is strictly inside (min, max). For a
      /// zero velocity (segment parallel to this axis) the interval is the
      /// whole real line when p is inside, empty otherwise.
      /// </summary>
      private static bool GetStrictInterval(
         double p, double d, double min, double max,
         out double lo, out double hi)
      {
         if (d == 0.0)
         {
            if (p > min && p < max)
            {
               lo = double.NegativeInfinity;
               hi = double.PositiveInfinity;
               return true;
            }
            lo = double.PositiveInfinity;
            hi = double.NegativeInfinity;
            return false;
         }

         double t1 = (min - p) / d;
         double t2 = (max - p) / d;
         lo = Math.Min(t1, t2);
         hi = Math.Max(t1, t2);
         return lo < hi;
      }

      public bool Equals(Rect2 other)
      {
         return X == other.X && Y == other.Y &&
            Width == other.Width && Height == other.Height;
      }

      public override bool Equals(object obj)
      {
         return obj is Rect2 r && Equals(r);
      }

      public override int GetHashCode()
      {
         return (X, Y, Width, Height).GetHashCode();
      }

      public override string ToString()
      {
         return "(" + X + ", " + Y + ", " + Width + "x" + Height + ")";
      }
   }

}
