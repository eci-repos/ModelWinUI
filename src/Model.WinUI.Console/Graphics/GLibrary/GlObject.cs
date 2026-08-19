using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Input;
using Windows.Foundation;

using ModelConsole.Graph;

namespace ModelConsole.Graphics.GLibrary
{

   public abstract class GlObject : GlObjectInfo
   {

      protected object m_Instance { get; set; } = null;
      public object Instance
      {
         get { return m_Instance; }
         set { m_Instance = value; }
      }

      public GlContext Context { get; set; } = null;

      /// <summary>
      /// Arbitrary payload attached to a drawable (e.g. the <c>FkRelation</c>
      /// edge a connector renders) so a click can resolve the underlying
      /// model entity.
      /// </summary>
      public object Data { get; set; } = null;

      /// <summary>
      /// The typed graph node this drawable exposes (backlog 028): identity,
      /// the live canonical model object it renders, and its hover summary.
      /// Null for drawables with no model payload (grips, handles, highlights).
      /// The host consumes <c>obj.Node</c> — it never type-checks raw objects.
      /// </summary>
      public virtual IGraphNode Node => null;

      public GlObject(object instance) : base()
      {
         m_Instance = instance;
      }

      public abstract void DeltaMove(Point? deltaPoint = null);
      public abstract void Move(Point? point = null);
      public abstract void PointerEvent(
         GlPointerEvent pointerEvent, PointerPoint point = null);
      public abstract void Reshape(object node);
      public abstract bool Selected { get; set; }

      /// <summary>
      /// Swap cordinates.
      /// </summary>
      /// <param name="x1">start x</param>
      /// <param name="y1">start y</param>
      /// <param name="x2">end x</param>
      /// <param name="y2">end y</param>
      public static void Swap(
         ref double x1, ref double y1, ref double x2, ref double y2)
      {
         double xt = x2;
         x2 = x1;
         x1 = xt;

         xt = y2;
         y2 = y1;
         y1 = xt;
      }

   }

}
