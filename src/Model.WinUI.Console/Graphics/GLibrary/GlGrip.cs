using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.PointOfService;
using Windows.Foundation;

using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace ModelConsole.Graphics.GLibrary
{

   /// <summary>
   /// Grip allow to select end-points or grip-handle and drag/move points or
   /// part of the shape around such as making the shape bigger or smaller
   /// usually with one end-point of line as an anchor that will not move.
   /// Grips are represented as squares.
   /// </summary>
   public class GlGrip : GlGrabberBase<Rectangle>, IGlGrabber
   {

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="position">position</param>
      public override void DeltaMove(Point? position = null)
      {
         if (_objectShape != null)
         {
            GlObject shape = _objectShape.Tag as GlObject;
            if (shape != null)
            {
               shape.Reshape(this);
            }
         }
      }

      /// <summary>
      /// Move Object to a relative position using given point.
      /// </summary>
      /// <param name="point">X, Y to move</param>
      private void MovePoint(Point? point)
      {
         if (point.HasValue)
         {
            X += point.Value.X;
            Y += point.Value.Y;
         }

         var d = DEFAULT_HANDLE_LENGTH;
         double dt = d / 2.0;
         Canvas.SetLeft(NativeInstance, X - dt);
         Canvas.SetTop(NativeInstance, Y - dt);
      }

      /// <summary>
      /// Move Object to a relative position using given delta values.
      /// </summary>
      /// <param name="point">X, Y to move</param>
      public static void Move(List<GlGrip> nodes, Point? point)
      {
         foreach (var n in nodes)
         {
            n.MovePoint(point);
         }
      }

   }

}
