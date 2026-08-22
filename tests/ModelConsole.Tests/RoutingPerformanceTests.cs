using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using Xunit;
using Xunit.Abstractions;

namespace ModelConsole.Tests
{

   /// <summary>
   /// TEMPORARY diagnostic - measures the routing cost of the full 50-table
   /// pipeline for backlog 013 (drag hang). Compares the old behavior (full
   /// re-route of all 74 edges) with the fix (re-route only the moved table's
   /// edges, reusing stored routes as thin obstacles). Prints timings; no
   /// assertions on wall-clock time (would be flaky).
   /// </summary>
   public class RoutingPerformanceTests
   {
      private readonly ITestOutputHelper _output;

      public RoutingPerformanceTests(ITestOutputHelper output)
      {
         _output = output;
      }

      [Fact]
      public void MeasurePartialRerouteVsFullReroute()
      {
         var tables = PublicSafetySchema.Tables;
         var (sizes, maxW, maxH) = MeasureTables(tables);

         var layout = EntityLayoutEngine.Layout(tables, Array.Empty<FkRelation>(),
            new EntityLayoutOptions
         {
            Columns = 7,
            SlotWidth = maxW + 80,
            SlotHeight = maxH + 80,
            Gutter = 80
         });

         var rects = new Dictionary<string, Rect2>();
         foreach (var t in tables)
         {
            var slot = layout[t.TableName];
            rects[t.TableName] = new Rect2(
               slot.X, slot.Y, sizes[t.TableName].W, sizes[t.TableName].H);
         }

         var (edges, _) = FkEdgeExtractor.Extract(tables);
         var options = new RouterOptions
         {
            GridSize = 16,
            ObstacleMargin = 14,
            StubLength = 20
         };

         _output.WriteLine("tables=" + tables.Length + " edges=" + edges.Count);
         _output.WriteLine("baseline canvas=" + Bounds(rects));

         // Full re-route (old behavior on every drag release).
         var full = RouteAllAndTime(rects, tables, edges, options, "full-reroute");

         // Partial re-route (the fix): route all once, then re-route only the
         // moved table's edges against the stored routes as thin obstacles.
         var moved = tables[0].TableName;
         var partial = PartialRerouteAndTime(
            rects, tables, edges, options, moved, "partial-reroute");

         // Partial re-route with the table dragged far (grown canvas) - the
         // node-budget safety net keeps this bounded.
         var farRects = new Dictionary<string, Rect2>(rects);
         farRects[moved] = new Rect2(5000, 5000, sizes[moved].W, sizes[moved].H);
         _output.WriteLine("far-drag canvas=" + Bounds(farRects));
         var partialFar = PartialRerouteAndTime(
            farRects, tables, edges, options, moved, "partial-reroute@5000");

         _output.WriteLine("--- summary ---");
         _output.WriteLine("full reroute     total=" + full.TotalMs + "ms  maxEdge=" + full.MaxEdgeMs + "ms");
         _output.WriteLine("partial reroute  total=" + partial.TotalMs + "ms  maxEdge=" + partial.MaxEdgeMs + "ms");
         _output.WriteLine("partial far@5000 total=" + partialFar.TotalMs + "ms  maxEdge=" + partialFar.MaxEdgeMs + "ms");
      }

      private (double TotalMs, double MaxEdgeMs) RouteAllAndTime(
         IReadOnlyDictionary<string, Rect2> rects,
         IReadOnlyList<TableInfo> tables,
         IReadOnlyList<FkRelation> edges,
         RouterOptions options,
         string label)
      {
         var bounds = Bounds(rects);
         var obstacles = rects.Values.ToList();
         var anchorEdges = BuildAnchors(rects, tables, edges);

         var sw = Stopwatch.StartNew();
         double maxEdge = 0;
         var thin = new List<Rect2>();
         foreach (var a in anchorEdges)
         {
            var esw = Stopwatch.StartNew();
            OrthogonalRouter.Route(a.Start, a.End, obstacles, bounds, options, thin);
            esw.Stop();
            maxEdge = Math.Max(maxEdge, esw.Elapsed.TotalMilliseconds);
            AddSegmentObstacles(thin, a.Start, a.End, 4);
         }
         sw.Stop();

         _output.WriteLine(label + ": total=" + sw.Elapsed.TotalMilliseconds.ToString("F1") +
            "ms  maxEdge=" + maxEdge.ToString("F1") + "ms");
         return (sw.Elapsed.TotalMilliseconds, maxEdge);
      }

      private (double TotalMs, double MaxEdgeMs) PartialRerouteAndTime(
         IReadOnlyDictionary<string, Rect2> rects,
         IReadOnlyList<TableInfo> tables,
         IReadOnlyList<FkRelation> edges,
         RouterOptions options,
         string movedTable,
         string label)
      {
         var bounds = Bounds(rects);
         var obstacles = rects.Values.ToList();
         var anchorEdges = BuildAnchors(rects, tables, edges);

         // Route all edges once (the stored routes).
         var stored = new List<(FkRelation Edge, IReadOnlyList<Point2> Points)>();
         var thin = new List<Rect2>();
         foreach (var a in anchorEdges)
         {
            var pts = OrthogonalRouter.Route(a.Start, a.End, obstacles, bounds, options, thin);
            stored.Add((a.Edge, pts));
            AddSegmentObstacles(thin, pts, 4);
         }

         // Re-route only the moved table's edges against the stored routes.
         var toRoute = anchorEdges
            .Where(a => a.Edge.ChildTable == movedTable || a.Edge.ParentTable == movedTable)
            .ToList();
         var thin2 = new List<Rect2>();
         foreach (var (edge, pts) in stored)
         {
            if (edge.ChildTable == movedTable || edge.ParentTable == movedTable)
            {
               continue;
            }
            AddSegmentObstacles(thin2, pts, 4);
         }

         var sw = Stopwatch.StartNew();
         double maxEdge = 0;
         foreach (var a in toRoute)
         {
            var esw = Stopwatch.StartNew();
            OrthogonalRouter.Route(a.Start, a.End, obstacles, bounds, options, thin2);
            esw.Stop();
            maxEdge = Math.Max(maxEdge, esw.Elapsed.TotalMilliseconds);
            AddSegmentObstacles(thin2, a.Start, a.End, 4);
         }
         sw.Stop();

         _output.WriteLine(label + ": total=" + sw.Elapsed.TotalMilliseconds.ToString("F1") +
            "ms  maxEdge=" + maxEdge.ToString("F1") + "ms  edges=" + toRoute.Count);
         return (sw.Elapsed.TotalMilliseconds, maxEdge);
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

      private static Rect2 Bounds(IReadOnlyDictionary<string, Rect2> rects)
      {
         double maxX = rects.Values.Max(r => r.Right);
         double maxY = rects.Values.Max(r => r.Bottom);
         return new Rect2(0, 0, maxX + 80, maxY + 80);
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

      private static (Dictionary<string, (double W, double H)> Sizes, double MaxW, double MaxH) MeasureTables(
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

      private static void AddSegmentObstacles(
         List<Rect2> thin, IReadOnlyList<Point2> pts, double margin)
      {
         for (int i = 0; i < pts.Count - 1; i++)
         {
            AddSegmentObstacles(thin, pts[i], pts[i + 1], margin);
         }
      }

      private static void AddSegmentObstacles(
         List<Rect2> thin, Point2 a, Point2 b, double margin)
      {
         if (a.Y == b.Y)
         {
            double x1 = Math.Min(a.X, b.X);
            double x2 = Math.Max(a.X, b.X);
            thin.Add(new Rect2(x1 - margin, a.Y - margin, (x2 - x1) + 2 * margin, 2 * margin));
         }
         else
         {
            double y1 = Math.Min(a.Y, b.Y);
            double y2 = Math.Max(a.Y, b.Y);
            thin.Add(new Rect2(a.X - margin, y1 - margin, 2 * margin, (y2 - y1) + 2 * margin));
         }
      }

   }

}
