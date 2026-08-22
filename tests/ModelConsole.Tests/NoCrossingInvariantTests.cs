using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 012 - the hard invariant that no connector segment crosses a
   /// table interior, verified across the 50-table schema and adversarial
   /// layouts.
   /// </summary>
   public class NoCrossingInvariantTests
   {
      private static readonly RouterOptions Options = new()
      {
         GridSize = 16,
         ObstacleMargin = 14,
         StubLength = 20
      };

      [Fact]
      public void PublicSafetySchemaHasNoTableCrossings()
      {
         var (routes, anchorEdges, rects) = RouteSchema(80, 80);
         AssertNoCrossings(routes, anchorEdges, rects);
      }

      [Fact]
      public void TightLayoutHasNoTableCrossings()
      {
         // Pack the tables into tight slots so the gaps between them are
         // small and the anchors sit close to neighbour tables.
         var (routes, anchorEdges, rects) = RouteSchema(20, 20);
         AssertNoCrossings(routes, anchorEdges, rects);
      }

      [Fact]
      public void ThinObstacleBarrierRetriesWithoutThin()
      {
         // PersonAlias->RefId routed first creates a thin barrier (vertical at
         // x=581, horizontal at y=619) that, combined with the tables, makes
         // the grid unreachable for PersonName->Person. The router must retry
         // without the thin obstacles so the route still avoids tables.
         var obstacles = new List<Rect2>
         {
            new Rect2(0, 0, 421, 408),        // Person
            new Rect2(581, 0, 344.5, 240),    // PersonAlias
            new Rect2(1162, 0, 302, 352),     // PersonName
            new Rect2(0, 568, 285, 156)       // RefIdentifierType
         };
         var bounds = new Rect2(0, 0, 2000, 2000);

         var first = OrthogonalRouter.Route(
            new Point2(581, 122), new Point2(285, 619), obstacles, bounds, Options);
         var thin = new List<Rect2>();
         AddSegmentObstacles(thin, first, 4);

         var second = OrthogonalRouter.Route(
            new Point2(1162, 94), new Point2(421, 33), obstacles, bounds, Options, thin);

         AssertNoCrossing(second, obstacles);
      }

      [Fact]
      public void DirectPathDoesNotCrossTableInterior()
      {
         // Side-by-side tables: the direct HV path runs along the gap and must
         // not enter either table's interior (open-interval crossing test).
         var child = new Rect2(0, 40, 100, 200);
         var parent = new Rect2(300, 40, 100, 200);
         var pts = OrthogonalRouter.Route(
            new Point2(100, 140), new Point2(300, 140),
            new[] { child, parent }, new Rect2(0, 0, 400, 300), Options);

         AssertNoCrossing(pts, new[] { child, parent });
      }

      private static (IReadOnlyList<IReadOnlyList<Point2>> Routes,
         List<(Point2 Start, Point2 End, FkRelation Edge)> AnchorEdges,
         IReadOnlyDictionary<string, Rect2> Rects) RouteSchema(
         double slotPadding, double gutter)
      {
         var tables = PublicSafetySchema.Tables;
         var sizes = MeasureTables(tables);

         var layout = EntityLayoutEngine.Layout(tables, Array.Empty<FkRelation>(),
            new EntityLayoutOptions
         {
            Columns = 7,
            SlotWidth = sizes.MaxW + slotPadding,
            SlotHeight = sizes.MaxH + slotPadding,
            Gutter = gutter
         });

         var rects = new Dictionary<string, Rect2>();
         foreach (var t in tables)
         {
            var slot = layout[t.TableName];
            rects[t.TableName] = new Rect2(
               slot.X, slot.Y, sizes.Sizes[t.TableName].W, sizes.Sizes[t.TableName].H);
         }

         var bounds = new Rect2(0, 0,
            rects.Values.Max(r => r.Right) + 80,
            rects.Values.Max(r => r.Bottom) + 80);

         var (edges, _) = FkEdgeExtractor.Extract(tables);
         var anchorEdges = BuildAnchors(rects, tables, edges);
         var obstacles = rects.Values.ToList();

         var routes = SequentialRouter.RouteAll(
            anchorEdges.Select(a => (a.Start, a.End)).ToList(),
            obstacles, bounds, Options);

         return (routes, anchorEdges, rects);
      }

      private static void AssertNoCrossings(
         IReadOnlyList<IReadOnlyList<Point2>> routes,
         List<(Point2 Start, Point2 End, FkRelation Edge)> anchorEdges,
         IReadOnlyDictionary<string, Rect2> rects)
      {
         var obstacles = rects.Values.ToList();
         for (int i = 0; i < routes.Count; i++)
         {
            AssertNoCrossing(routes[i], obstacles,
               anchorEdges[i].Edge.ChildTable + "->" + anchorEdges[i].Edge.ParentTable);
         }
      }

      private static void AssertNoCrossing(
         IReadOnlyList<Point2> pts, IReadOnlyList<Rect2> obstacles,
         string label = null)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            foreach (var o in obstacles)
            {
               Assert.False(
                  Rect2.SegmentCrossesInterior(pts[i], pts[i + 1], o),
                  (label != null ? label + ": " : "") +
                  "segment " + pts[i] + " -> " + pts[i + 1] + " crosses " + o);
            }
         }
      }

      private static void AddSegmentObstacles(
         List<Rect2> thin, IReadOnlyList<Point2> pts, double margin)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            Point2 a = pts[i];
            Point2 b = pts[i + 1];
            if (a.Y == b.Y)
            {
               double x1 = Math.Min(a.X, b.X);
               double x2 = Math.Max(a.X, b.X);
               thin.Add(new Rect2(
                  x1 - margin, a.Y - margin,
                  (x2 - x1) + 2 * margin, 2 * margin));
            }
            else
            {
               double y1 = Math.Min(a.Y, b.Y);
               double y2 = Math.Max(a.Y, b.Y);
               thin.Add(new Rect2(
                  a.X - margin, y1 - margin,
                  2 * margin, (y2 - y1) + 2 * margin));
            }
         }
      }

      private static (Dictionary<string, (double W, double H)> Sizes,
         double MaxW, double MaxH) MeasureTables(
         IReadOnlyList<TableInfo> tables)
      {
         var sizes = new Dictionary<string, (double W, double H)>();
         double maxW = 0, maxH = 0;
         foreach (var t in tables)
         {
            double maxName = 0, maxType = 0;
            foreach (var c in t.Columns)
            {
               maxName = Math.Max(maxName, c.ColumnName.Length * 8.5);
               string dt = c.Type + (c.Size > 0 ? "(" + c.Size + ")" : "");
               maxType = Math.Max(maxType, dt.Length * 8.5);
            }
            double w = 66 + (maxName + 10) + maxType + 22;
            double h = 100 + 28 * t.Columns.Count;
            sizes[t.TableName] = (w, h);
            maxW = Math.Max(maxW, w);
            maxH = Math.Max(maxH, h);
         }
         return (sizes, maxW, maxH);
      }

      private static List<(Point2 Start, Point2 End, FkRelation Edge)> BuildAnchors(
         IReadOnlyDictionary<string, Rect2> rects,
         IReadOnlyList<TableInfo> tables,
         IReadOnlyList<FkRelation> edges)
      {
         var startGroups = edges
            .GroupBy(e => e.ChildTable + "::" + e.ChildColumn)
            .ToDictionary(g => g.Key, g => g.ToList());
         var endGroups = edges
            .GroupBy(e => e.ParentTable + "::" + e.ParentColumn)
            .ToDictionary(g => g.Key, g => g.ToList());

         var result = new List<(Point2 Start, Point2 End, FkRelation Edge)>();
         foreach (var edge in edges)
         {
            var child = rects[edge.ChildTable];
            var parent = rects[edge.ParentTable];
            double childRowY = RowCenterY(edge.ChildTable, edge.ChildColumn, rects, tables);
            double parentRowY = RowCenterY(edge.ParentTable, edge.ParentColumn, rects, tables);

            var (start, end, childSide, parentSide) = ConnectorAnchors.Resolve(
               child, parent, childRowY, parentRowY);

            var sg = startGroups[edge.ChildTable + "::" + edge.ChildColumn];
            var eg = endGroups[edge.ParentTable + "::" + edge.ParentColumn];
            start = ConnectorAnchors.FanOut(start, childSide, sg.IndexOf(edge), sg.Count, 6);
            end = ConnectorAnchors.FanOut(end, parentSide, eg.IndexOf(edge), eg.Count, 6);

            result.Add((start, end, edge));
         }
         return result;
      }

      private static double RowCenterY(
         string tableName, string columnName,
         IReadOnlyDictionary<string, Rect2> rects, IReadOnlyList<TableInfo> tables)
      {
         var t = tables.First(x => x.TableName == tableName);
         int idx = 0;
         for (int i = 0; i < t.Columns.Count; i++)
         {
            if (t.Columns[i].ColumnName == columnName)
            {
               idx = i;
               break;
            }
         }
         var r = rects[tableName];
         return r.Y + 40 + 10 + 2 + idx * 28 + 14;
      }

   }

}
