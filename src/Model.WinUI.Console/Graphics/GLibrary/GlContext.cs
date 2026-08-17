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
using Windows.System;
using Windows.UI.Core;

using ModelConsole.Model.Diagnostics;
using ModelConsole.Services;

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

      /// <summary>
      /// True once the current press has moved the shape beyond a small
      /// threshold, distinguishing a drag from a click.
      /// </summary>
      private bool _dragMoved;

      /// <summary>
      /// True while a pan gesture is active (drag on empty space, middle-drag,
      /// or space+drag).
      /// </summary>
      private bool _panning;

      /// <summary>
      /// Content-space pointer position at the start of the current pan.
      /// </summary>
      private Point _panStartPoint;

      /// <summary>
      /// Hand cursor shown over empty canvas space (hover); move cursor shown
      /// while panning.
      /// </summary>
      private InputCursor _handCursor;
      private InputCursor _moveCursor;

      /// <summary>
      /// Fired while a pan gesture is active, with the pointer delta (in
      /// content units) from the pan start point. The subscriber
      /// (ModelPanelControl) feeds it to the ScrollViewer's ChangeView so the
      /// drawing follows the pointer at the current zoom.
      /// </summary>
      public event Action<double, double> PanRequested;

      /// <summary>
      /// Fired when a shape is released after being dragged (moved). The
      /// payload is the shape's <see cref="GlObject"/>.
      /// </summary>
      public event Action<GlObject> ShapeReleased;

      /// <summary>
      /// Fired when a shape is clicked (pressed and released without moving).
      /// The payload is the shape's <see cref="GlObject"/>.
      /// </summary>
      public event Action<GlObject> ShapeClicked;

      private Canvas _canvas;
      public Canvas Instance
      {
         get { return _canvas; }
      }

      private DiagnosticsInfo _diagnosticsInfo = new DiagnosticsInfo();

      private readonly ILogService m_Log;

      public GlContext(Canvas canvas, ILogService log)
      {
         _canvas = canvas;
         m_Log = log;
         _canvas.PointerPressed += Canvas_PointerPressed;
         _canvas.PointerReleased += Canvas_PointerRelease;
         _canvas.PointerMoved += Canvas_PointerMoved;
         _canvas.PointerExited += Canvas_PointerExited;
         _canvas.PointerCanceled += Canvas_PointerCanceled;
         _canvas.PointerCaptureLost += Canvas_PointerCaptureLost;
         //_canvas.PointerEntered += Canvas_PointerEntered;

         _diagnosticsInfo.Verbosity = Verbosity.Trace;

         _handCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
         _moveCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
      }

      /// <summary>
      /// Write message to log.
      /// </summary>
      /// <param name="message">message to write</param>
      public void WriteMessage(string message)
      {
         m_Log.WriteMessage(message);
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
      /// Clear all interaction state. Called before a full re-render clears
      /// the canvas, so stale shape references (current/selected shape,
      /// grips, grabbers) do not point at removed elements.
      /// </summary>
      public void Reset()
      {
         _currentShape = null;
         _selectedShape = null;
         _pointerPoint = null;
         _grip = null;
         _handle = new GlHandle();
         _grabber = null;
         _dragMoved = false;
         _panning = false;
         SetCursor(null);
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

         if (Math.Abs(delta.X) > 2 || Math.Abs(delta.Y) > 2)
         {
            _dragMoved = true;
         }

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
      /// True while the space key is held (space+drag pans). Queried from the
      /// current thread's keyboard state so it works regardless of which
      /// element has focus.
      /// </summary>
      private static bool IsSpaceHeld()
      {
         return (InputKeyboardSource.GetKeyStateForCurrentThread(
            VirtualKey.Space) & CoreVirtualKeyStates.Down) ==
            CoreVirtualKeyStates.Down;
      }

      /// <summary>
      /// Swap the canvas cursor. <see cref="UIElement.ProtectedCursor"/> is
      /// protected, so it is reached through the <see cref="GlCanvas"/>
      /// subclass; a plain Canvas (no cursor support) is left untouched.
      /// </summary>
      private void SetCursor(InputCursor cursor)
      {
         if (_canvas is GlCanvas glCanvas)
         {
            glCanvas.Cursor = cursor;
         }
      }

      /// <summary>
      /// Start a pan gesture: capture the pointer and show the grabbing
      /// cursor. The delta from the press point is reported via
      /// <see cref="PanRequested"/> on each move.
      /// </summary>
      private void StartPan(PointerRoutedEventArgs e, PointerPoint pt)
      {
         _panning = true;
         _panStartPoint = pt.Position;
         _canvas.CapturePointer(e.Pointer);
         SetCursor(_moveCursor);
      }

      /// <summary>
      /// End a pan gesture: release the pointer and restore the hover cursor
      /// (hand over empty space, default over a shape).
      /// </summary>
      private void EndPan(PointerRoutedEventArgs e)
      {
         _panning = false;
         _canvas.ReleasePointerCapture(e.Pointer);
         SetCursor((e.OriginalSource as Shape) == null ? _handCursor : null);
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
         _dragMoved = false;

         if (_currentShape != null)
         {
            return;
         }

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         // Canvas-relative (content-space) coordinates. GetCurrentPoint(null)
         // would return window-relative coordinates, which at non-100% zoom
         // make the drag delta move the shape zoom* too far (backlog 013).
         var pt = e.GetCurrentPoint(_canvas);
         var props = pt.Properties;

         // Pan triggers (backlog 011): middle-drag always pans; left-drag on
         // empty canvas space pans; left-drag while space is held pans even
         // over a shape (space+drag convention). Mouse/touchpad only - touch
         // and pen pan natively via the ScrollViewer, so a pan gesture must
         // not capture the pointer away from it.
         var device = e.Pointer.PointerDeviceType;
         bool mouse = device == PointerDeviceType.Mouse ||
                      device == PointerDeviceType.Touchpad;
         bool middlePan = mouse && props.IsMiddleButtonPressed;
         bool spacePan = mouse && IsSpaceHeld() && props.IsLeftButtonPressed;
         bool emptyPan = mouse && props.IsLeftButtonPressed && s == null;

         if (middlePan || spacePan || emptyPan)
         {
            StartPan(e, pt);
            return;
         }

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
         PointerPoint pt = e.GetCurrentPoint(_canvas);
         e.Handled = true;

         if (_panning)
         {
            double dx = pt.Position.X - _panStartPoint.X;
            double dy = pt.Position.Y - _panStartPoint.Y;
            PanRequested?.Invoke(dx, dy);
            return;
         }

         // Hover cursor: hand over empty space, default over shapes.
         SetCursor((e.OriginalSource as Shape) == null ? _handCursor : null);

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

         if (_panning)
         {
            EndPan(e);
            return;
         }

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(_canvas);

         if (s != null)
         {
            s.Opacity = 1;
            s.ReleasePointerCapture(e.Pointer);
         }

         var released = s?.Tag as GlObject;
         bool moved = _dragMoved;

         ReleaseShape(nameof(Canvas_PointerRelease));

         if (released != null)
         {
            if (moved)
            {
               ShapeReleased?.Invoke(released);
            }
            else
            {
               ShapeClicked?.Invoke(released);
            }
         }
      }

      /// <summary>
      /// Pointer Exited
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Canvas_PointerExited(
         object sender, PointerRoutedEventArgs e)
      {
         if (_currentShape != null || _panning)
         {
            return;
         }

         e.Handled = true;

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(_canvas);

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

         if (_panning)
         {
            EndPan(e);
            return;
         }

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(_canvas);

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

         if (_panning)
         {
            EndPan(e);
            return;
         }

         var s = e.OriginalSource as Shape;
         if (s == null)
         {
            s = _currentShape;
         }

         PointerPoint pt = e.GetCurrentPoint(_canvas);

         if (s != null)
         {
            s.ReleasePointerCapture(e.Pointer);
            s.Opacity = 1;
         }

         ReleaseShape(nameof(Canvas_PointerCaptureLost));
      }

   }

}
