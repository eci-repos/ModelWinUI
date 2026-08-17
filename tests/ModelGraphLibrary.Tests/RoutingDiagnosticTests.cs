using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;
using ModelConsole.ModelData;

using SkiaSharp;

using Xunit;
using Xunit.Abstractions;

namespace ModelConsole.Tests
{

   /// <summary>
   /// TEMPORARY diagnostic - renders the full 50-table routing to a PNG so the
   /// routing quality can be inspected visually. Not a real test.
   /// </summary>
   public class RoutingDiagnosticTests
   {
      private readonly ITestOutputHelper _output;

      public RoutingDiagnosticTests(ITestOutputHelper output)
      {
         _output = output;
      }

      [Fact]
      public void RenderRoutingToPng()
      {
         var tables = PublicSafetySchema.Tables;

         // Replicate the app's Table measurement (approx: 8.5px/char at 16px).
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

         const double SlotPadding = 80, Gutter = 80;
         var layout = TableLayoutEngine.Layout(tables, new GridLayoutOptions
         {
            Columns = 7,
            SlotWidth = maxW + SlotPadding,
            SlotHeight = maxH + SlotPadding,
            Gutter = Gutter
         });

         var rects = new Dictionary<string, Rect2>();
         foreach (var t in tables)
         {
            var slot = layout[t.TableName];
            rects[t.TableName] = new Rect2(
               slot.X, slot.Y, sizes[t.TableName].W, sizes[t.TableName].H);
         }

         double maxX = rects.Values.Max(r => r.Right);
         double maxY = rects.Values.Max(r => r.Bottom);
         var bounds = new Rect2(0, 0, maxX + 80, maxY + 80);

         var options = new RouterOptions
         {
            GridSize = 16,
            ObstacleMargin = 14,
            StubLength = 20
         };

         var (edges, issues) = FkEdgeExtractor.Extract(tables);

         // Replicate anchor resolution + fan-out (row Y = table top + banner
         // + corner + padding + row index * rowHeight + rowHeight/2).
         var startGroups = edges
            .GroupBy(e => e.ChildTable + "::" + e.ChildColumn)
            .ToDictionary(g => g.Key, g => g.ToList());
         var endGroups = edges
            .GroupBy(e => e.ParentTable + "::" + e.ParentColumn)
            .ToDictionary(g => g.Key, g => g.ToList());

         var anchorEdges = new List<(Point2 Start, Point2 End, FkRelation Edge)>();
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

            anchorEdges.Add((start, end, edge));
         }

         var obstacles = rects.Values.ToList();
         var routes = SequentialRouter.RouteAll(
            anchorEdges.Select(a => (a.Start, a.End)).ToList(),
            obstacles, bounds, options);

         // Count crossings.
         int crossings = 0;
         var crossingEdges = new List<string>();
         for (int i = 0; i < routes.Count; i++)
         {
            var pts = routes[i];
            for (int s = 0; s < pts.Count - 1; s++)
            {
               foreach (var o in obstacles)
               {
                  if (Rect2.SegmentCrossesInterior(pts[s], pts[s + 1], o))
                  {
                     crossings++;
                     crossingEdges.Add(anchorEdges[i].Edge.ChildTable + "->" +
                        anchorEdges[i].Edge.ParentTable);
                     break;
                  }
               }
            }
         }

         _output.WriteLine("tables=" + tables.Length + " edges=" + edges.Count);
         _output.WriteLine("maxW=" + maxW + " maxH=" + maxH);
         _output.WriteLine("crossings=" + crossings);
         _output.WriteLine("crossing edges: " + string.Join(", ", crossingEdges.Distinct()));

         // Render to PNG.
         RenderPng(rects, anchorEdges, routes, bounds);
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
         // banner(40) + corner(10) + padding/2(2) + idx*28 + 14
         return r.Y + 40 + 10 + 2 + idx * 28 + 14;
      }

      private static void RenderPng(
         IReadOnlyDictionary<string, Rect2> rects,
         List<(Point2 Start, Point2 End, FkRelation Edge)> anchorEdges,
         IReadOnlyList<IReadOnlyList<Point2>> routes,
         Rect2 bounds)
      {
         int w = (int)Math.Ceiling(bounds.Width) + 40;
         int h = (int)Math.Ceiling(bounds.Height) + 40;
         using var bitmap = new SKBitmap(w, h);
         using var canvas = new SKCanvas(bitmap);
         canvas.Clear(SKColors.White);

         var tablePaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Fill };
         var strokePaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
         var routePaint = new SKPaint { Color = SKColors.DodgerBlue, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
         var anchorPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };

         foreach (var r in rects.Values)
         {
            canvas.DrawRect(new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom), tablePaint);
            canvas.DrawRect(new SKRect((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom), strokePaint);
         }

         foreach (var pts in routes)
         {
            using var builder = new SKPathBuilder();
            builder.MoveTo(new SKPoint((float)pts[0].X, (float)pts[0].Y));
            for (int i = 1; i < pts.Count; i++)
            {
               builder.LineTo(new SKPoint((float)pts[i].X, (float)pts[i].Y));
            }
            using var path = builder.Detach();
            canvas.DrawPath(path, routePaint);
         }

         foreach (var a in anchorEdges)
         {
            canvas.DrawCircle((float)a.Start.X, (float)a.Start.Y, 3, anchorPaint);
            canvas.DrawCircle((float)a.End.X, (float)a.End.Y, 3, anchorPaint);
         }

         string dir = Path.Combine(Path.GetTempPath(), "model-console-diag");
         Directory.CreateDirectory(dir);
         string file = Path.Combine(dir, "routing.png");
         using (var image = SKImage.FromBitmap(bitmap))
         using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
         using (var fs = File.OpenWrite(file))
         {
            data.SaveTo(fs);
         }
      }

   }

}
