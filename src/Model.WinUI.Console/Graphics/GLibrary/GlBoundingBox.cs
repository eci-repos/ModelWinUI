using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;

namespace ModelConsole.Graphics.GLibrary
{

   public class GlBoundingBox
   {
      public double X1 { get; set; }
      public double Y1 { get; set; }
      public double X2 { get; set; }
      public double Y2 { get; set; }

      public GlBoundingBox(double x1, double y1, double x2, double y2)
      {
         X1 = x1;
         Y1 = y1;
         X2 = x2;
         Y2 = y2;
      }

      public bool PointInShape(Point point)
      {
         return point.X >= X1 && point.X <= X2 && 
                point.Y >= Y1 && point.Y <= Y2;
      }
   }

}
