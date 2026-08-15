using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;
using Microsoft.UI.Input;
using Windows.Foundation;
using System.Runtime.CompilerServices;

using ModelConsole.Controls;
using ModelConsole.Model.Diagnostics;
using SkiaSharp;

namespace ModelConsole.Graphics.GLibrary
{

   /// <summary>
   /// Model Graphics Context
   /// </summary>
   public class GlContext : IDiagnosticWritter
   {
      public static double DefaultRoundCorderRadious = 10;
      public static double DefaultTextPanelPadding = 4;

      private Shape _currentShape = null;
      private Shape _selectedShape = null;
      private PointerPoint _pointerPoint = null;

      /// <summary>
      /// Grip implements the Shape resizing on predefine grip-nodes.
      /// </summary>
      private GlGrip _grip = null;

      /// <summary>
      /// Handle implements the relocation of a Shape by moving it around.
      /// </summary>
      private GlHandle _handle = new GlHandle();

      /// <summary>
      /// Grabber is the current Grip or Handle being used.
      /// </summary>
      private IGlGrabber _grabber = null;

      private Canvas _canvas;
      public Canvas Instance
      {
         get { return _canvas; }
      }

      private DiagnosticsInfo _diagnosticsInfo = new DiagnosticsInfo();

      public IDiagnosticWritter Writer = null;

      public GlContext(Canvas canvas)
      {
         _canvas = canvas;
         _canvas.PointerPressed += Canvas_PointerPressed;
         _canvas.PointerReleased += Canvas_PointerRelease;
         _canvas.PointerMoved += Canvas_PointerMoved;
         _canvas.PointerExited += Canvas_PointerExited;
         _canvas.PointerCanceled += Canvas_PointerCanceled;
         _canvas.PointerCaptureLost += Canvas_PointerCaptureLost;
         //_canvas.PointerEntered += Canvas_PointerEntered;

         _diagnosticsInfo.Verbosity = Verbosity.Trace;
      }

      /// <summary>
      /// Write message to log.
      /// </summary>
      /// <param name="message">message to write</param>
      public void WriteMessage(string message)
      {
         MessageLogEntry entry = new MessageLogEntry();
         entry.Message = message;
         ResultLog.DefaultLog.Write(entry);
      }

      /// <summary>
      /// Reset Grabber.
      /// </summary>
      public void ResetGrabber()
      {
         _handle.Selected = false;
         _grabber = null;
      }

      /// <summary>
      /// Set Pointer Handler that could be a handle (to move the object around)
      /// or a Grip (to stretch the object Shape).
      /// </summary>
      /// <param name="point"></param>
      public void SetPointerHandle(Shape item, PointerPoint point = null)
      {
         if (_currentShape != null)
         {
            return;
         }

         if (point == null || item == null)
         {
            ResetGrabber();
         }
         else if (_grabber == null)
         {
            IGlGrip grip = item.Tag as IGlGrip;

            var node = grip.GetGripNode(point.Position);
            if (node != null && _grabber != node)
            {
               if (_handle != null)
               {
                  _handle.Selected = false;
                  _handle.Tag = null;
               }

               _grip = node;
               _grip.Selected = true;

               _grabber = node;
               _grabber.Tag = item;
            }
            else if (_grabber == null)
            {
               if (_grip != null)
               {
                  _grip = null;
               }

               _handle.Selected = true;
               _grabber = _handle;
               _grabber.Draw(this, point.Position.X, point.Position.Y);
            }

            _grabber.Tag = item;
         }
      }

      /// <summary>
      /// Graber and Shape Delta Move.
      /// </summary>
      /// <param name="shape"></param>
      /// <param name="point"></param>
      private void DeltaMove(Shape shape, PointerPoint point, 
         GlPointerEvent pointerEvent = GlPointerEvent.None)
      {
         Point delta = new Point();
         delta.X = point.Position.X - _pointerPoint.Position.X;
         delta.Y = point.Position.Y - _pointerPoint.Position.Y;

         var o = shape.Tag as GlObject;
         o.DeltaMove(delta);

         if (_grabber != null)
         {
            _grabber.Draw(this, point.Position.X, point.Position.Y);
         }

         _pointerPoint = point;
      }

      private Shape GetShape(Shape grabber)
      {
         var o = grabber.Tag as IGlGrabber;
         return o != null ? o.Shape : null;
      }

      /// <summary>
      /// When user press the pointer over a graphic instance this function gets
      /// called and e.OriginalSource will identify the object.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerPressed(
         object sender, PointerRoutedEventArgs e)
      {
         e.Handled = true;

         if (_currentShape != null)
         {
            return;
         }

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         var pt = e.GetCurrentPoint(null);

         if (s != null)
         {
            if (_grabber == null)
            {
               var g = s.Tag as IGlGrip;
               if (g != null)
               {
                  s = g.Shape;
               }
            }

            s.CapturePointer(e.Pointer);
            SetShapeVisibility(false, _selectedShape);

            if (_currentShape != s)
            {
               _currentShape = s;
               _currentShape.Opacity = 1;

               SetShapeVisibility(true, _currentShape);
               _selectedShape = GetShape(s);
            }
            _pointerPoint = pt;

            s.Opacity = .5;

            DeltaMove(s, pt, GlPointerEvent.Enter);
         }
      }

      /// <summary>
      /// When user move the pointer over a graphic instance this function gets
      /// called and e.OriginalSource will identify the object.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      //private void Canvas_PointerEntered(
      //   object sender, PointerRoutedEventArgs e)
      //{
      //   e.Handled = true;
      //}

      /// <summary>
      /// On Pointer Move if there is an object already clicked then move the
      /// object to a new position based on the moved distance.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerMoved(
         object sender, PointerRoutedEventArgs e)
      {
         PointerPoint pt = e.GetCurrentPoint(null);
         e.Handled = true;

         if (_currentShape != null)
         {
            DeltaMove(_currentShape, pt);
            return;
         }

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         if (s != null)
         {
            var o = s.Tag as GlObject;
            if (o != null)
            {
               o.PointerEvent(GlPointerEvent.Enter, pt);

               var tag = _currentShape == null ? 
                  null : _currentShape.Tag as GlObject;
               if (tag == null)
               {
                  return;
               }

               DeltaMove(_currentShape, pt);

               _currentShape = s;

               return;
            }
         }
      }

      /// <summary>
      /// Set Shape visibility.
      /// </summary>
      /// <param name="visible">true if vissible</param>
      private void SetShapeVisibility(bool visible, Shape shape)
      {
         if (shape != null)
         {
            var o = shape.Tag as IGlGrabber;
            if (o != null)
            {
               if (visible)
               {
                  o.Selected = true;
                  shape.Opacity = 0.5;
               }
               else
               {
                  o.Selected = false;
                  shape.Opacity = 1.0;
               }
               var s = o.Tag as Shape;
               if (s != null)
               {
                  var shapeObj = s.Tag as GlObject;
                  if (shapeObj != null)
                  {
                     shapeObj.Selected = visible;
                  }
                  //s.Selected = visible;
               }
            }
            else
            {
               var g = shape.Tag as IGlGrip;
               if (g != null)
               {
                  g.Selected = visible;
               }
            }
         }
      }

      /// <summary>
      /// Release Shape
      /// </summary>
      private void ReleaseShape(string locationText)
      {
         //WriteMessage(locationText);
         _currentShape = null;
         SetPointerHandle(null);
      }

      /// <summary>
      /// Pointer Release
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerRelease(
         object sender, PointerRoutedEventArgs e)
      {
         e.Handled = true;

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(null);

         if (s != null)
         {
            s.Opacity = 1;
            s.ReleasePointerCapture(e.Pointer);
         }

         ReleaseShape(nameof(Canvas_PointerRelease));
      }

      /// <summary>
      /// Pointer Exited
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerExited(
         object sender, PointerRoutedEventArgs e)
      {
         if (_currentShape != null)
         {
            return;
         }

         e.Handled = true;

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(null);

         if (s != null)
         {
            s.Opacity = 1;
         }

         ReleaseShape(nameof(Canvas_PointerExited));
      }

      /// <summary>
      /// Pointer Canceled
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerCanceled(
         object sender, PointerRoutedEventArgs e)
      {
         e.Handled = true;

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(null);

         if (s != null)
         {
            s.ReleasePointerCapture(e.Pointer);
            s.Opacity = 1;
         }

         ReleaseShape(nameof(Canvas_PointerCanceled));
      }

      /// <summary>
      /// Pointer Capture Lost
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerCaptureLost(
         object sender, PointerRoutedEventArgs e)
      {
         e.Handled = true;

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(null);

         if (s != null)
         {
            s.ReleasePointerCapture(e.Pointer);
            s.Opacity = 1;
         }

         ReleaseShape(nameof(Canvas_PointerCaptureLost));
      }

   }

}
