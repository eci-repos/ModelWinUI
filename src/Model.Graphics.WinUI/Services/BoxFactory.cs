using System.Collections.Generic;

using Model.Data;
using ModelConsole.Graphics.GLibrary;
using ModelConsole.Graphics.Primitives;

namespace ModelConsole.Graphics.Services
{
   /// <summary>
   /// Creates and draws <see cref="GroupBox"/> primitives on the XAML graphics
   /// stack (backlog 039).
   /// </summary>
   public class BoxFactory : IBoxFactory
   {
      /// <summary>
      /// Create and draw a collapsed group's box primitive.
      /// </summary>
      /// <param name="frame">drawing context</param>
      /// <param name="x">x lower-left</param>
      /// <param name="y">y lower-left</param>
      /// <param name="group">the collapsed group's name</param>
      /// <param name="members">the box's member tables (live model objects)</param>
      /// <returns>the created GroupBox instance is returned</returns>
      public GroupBox Create(GlContext frame, float x, float y,
         string group, IReadOnlyList<TableInfo> members)
      {
         GroupBox g = new GroupBox(frame, x, y, group, members);
         g.DrawBox(frame);
         return g;
      }
   }
}
