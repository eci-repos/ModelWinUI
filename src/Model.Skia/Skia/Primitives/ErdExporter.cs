using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;

using SkiaSharp;

namespace ModelConsole.Skia.Primitives
{

   /// <summary>
   /// Options controlling a full-diagram export (backlog 054). Defaults mirror
   /// the app's live Skia render path (<c>SkiaPanelControl</c>), so an export
   /// matches what the renderer draws.
   /// </summary>
   public sealed class ErdExportOptions
   {
      /// <summary>Height of the table banner, in pixels.</summary>
      public float BannerHeight { get; set; } = 40;

      /// <summary>Presentation notation used when measuring + drawing.</summary>
      public DiagramNotation Notation { get; set; } = DiagramNotation.Erd;

      /// <summary>Name of the entity layout projection to use.</summary>
      public string LayoutName { get; set; } = EntityLayout.GridName;

      /// <summary>View-side visibility (backlog 038); null draws everything.</summary>
      public EntityVisibility Visibility { get; set; }

      /// <summary>View-side collapse state (backlog 039); null keeps all expanded.</summary>
      public GroupCollapseState Collapse { get; set; }

      /// <summary>The grouping theme (backlog 043); null = the tag theme.</summary>
      public GroupingTheme Theme { get; set; }

      /// <summary>The drawing-surface background color.</summary>
      public SKColor BackgroundColor { get; set; } = SKColors.White;

      /// <summary>
      /// Stable padding (px) added around the composed content so endpoint
      /// markers, labels, and selection widths never clip at the export edge.
      /// </summary>
      public double Padding { get; set; } = 40;
   }

   /// <summary>
   /// The portable full-diagram export path (backlog 054): compose an ERD via
   /// <see cref="ErdComposer"/> and render it at full size onto a raster
   /// surface (PNG) or a PDF page. This is the shared composition path both
   /// exports use, so the PNG and PDF outputs match. Pure Skia — no WinUI
   /// dependency — deterministic and unit-testable.
   /// </summary>
   public static class ErdExporter
   {

      /// <summary>
      /// Compose the diagram for export. A 1×1 offscreen surface supplies the
      /// measure frame's font; nothing is drawn here.
      /// </summary>
      public static ErdDiagram Compose(
         IReadOnlyList<TableInfo> tables, ErdExportOptions options = null)
      {
         var opts = options ?? new ErdExportOptions();
         using var surface = SKSurface.Create(new SKImageInfo(1, 1));
         var frame = new GlFrame(surface);
         return ErdComposer.Compose(tables, frame, new ErdOptions
         {
            BannerHeight = opts.BannerHeight,
            Notation = opts.Notation,
            LayoutName = opts.LayoutName
         }, opts.Visibility, opts.Collapse, opts.Theme);
      }

      /// <summary>
      /// The full-diagram pixel size: the composed content bounds plus the
      /// export padding on every side. Returns (0, 0) for an empty diagram.
      /// </summary>
      public static (int Width, int Height) GetSize(
         ErdDiagram diagram, double padding = 40)
      {
         if (diagram == null || diagram.Layout.Count == 0)
         {
            return (0, 0);
         }
         double minX = diagram.Layout.Values.Min(r => r.X);
         double minY = diagram.Layout.Values.Min(r => r.Y);
         double maxX = diagram.Layout.Values.Max(r => r.Right);
         double maxY = diagram.Layout.Values.Max(r => r.Bottom);
         return (
            (int)Math.Ceiling(maxX - minX + 2 * padding),
            (int)Math.Ceiling(maxY - minY + 2 * padding));
      }

      /// <summary>
      /// Draw the full diagram onto an existing canvas at 1:1 scale, translated
      /// so the content origin lands at the padding (no fit/zoom — the export
      /// is the whole diagram). Tables, collapsed boxes, and connectors are
      /// drawn in the current notation; no hover/selection emphasis.
      /// </summary>
      public static void Draw(SKCanvas canvas, ErdDiagram diagram,
         IReadOnlyList<TableInfo> tables, ErdExportOptions options = null)
      {
         var opts = options ?? new ErdExportOptions();
         if (diagram == null || diagram.Layout.Count == 0)
         {
            return;
         }

         var frame = new GlFrame(canvas, opts.BackgroundColor);
         var tablesByName = tables.ToDictionary(t => t.TableName, t => t);

         double minX = diagram.Layout.Values.Min(r => r.X);
         double minY = diagram.Layout.Values.Min(r => r.Y);
         frame.Canvas.Translate(
            (float)(opts.Padding - minX), (float)(opts.Padding - minY));

         foreach (var kv in diagram.Layout)
         {
            if (diagram.Boxes.TryGetValue(kv.Key, out var box))
            {
               GroupBox.Draw(frame, (float)kv.Value.X, (float)kv.Value.Y,
                  box.Group, box.MemberCount);
            }
            else
            {
               using var table = new Table(frame, (float)kv.Value.X,
                  (float)kv.Value.Y, opts.BannerHeight,
                  tablesByName[kv.Key], opts.Notation);
               table.DrawTable();
            }
         }

         for (int i = 0; i < diagram.Routes.Count; i++)
         {
            DrawConnector(frame, diagram, i, opts);
         }
      }

      /// <summary>
      /// Render the full diagram to a PNG byte array.
      /// </summary>
      public static byte[] ToPng(
         IReadOnlyList<TableInfo> tables, ErdExportOptions options = null)
      {
         var opts = options ?? new ErdExportOptions();
         var diagram = Compose(tables, opts);
         var (w, h) = GetSize(diagram, opts.Padding);
         if (w <= 0 || h <= 0)
         {
            return new byte[0];
         }

         using var bitmap = new SKBitmap(w, h);
         using (var surface = SKSurface.Create(
            new SKImageInfo(w, h), bitmap.GetPixels(), bitmap.RowBytes))
         {
            Draw(surface.Canvas, diagram, tables, opts);
         }
         using var image = SKImage.FromBitmap(bitmap);
         using var data = image.Encode(SKEncodedImageFormat.Png, 100);
         return data.ToArray();
      }

      /// <summary>
      /// Render the full diagram to a single-page PDF written to
      /// <paramref name="stream"/>. The page is sized to the diagram bounds +
      /// padding, so the PDF matches the PNG.
      /// </summary>
      public static void WritePdf(Stream stream,
         IReadOnlyList<TableInfo> tables, ErdExportOptions options = null)
      {
         var opts = options ?? new ErdExportOptions();
         var diagram = Compose(tables, opts);
         var (w, h) = GetSize(diagram, opts.Padding);
         if (w <= 0 || h <= 0)
         {
            return;
         }

         using var document = SKDocument.CreatePdf(stream);
         using (var pdfCanvas = document.BeginPage(w, h))
         {
            Draw(pdfCanvas, diagram, tables, opts);
         }
         document.Close();
      }

      private static void DrawConnector(GlFrame frame, ErdDiagram diagram,
         int index, ErdExportOptions opts)
      {
         if (opts.Notation == DiagramNotation.Uml)
         {
            new Connector(diagram.Routes[index])
            {
               ShowEndpointMarkers = false
            }.Draw(frame);
            DrawUmlConnectorLabel(frame, diagram, index);
            return;
         }

         if (opts.Notation == DiagramNotation.ErdCrowFoot)
         {
            var markers = CrowFootMarkers(diagram, index);
            new Connector(diagram.Routes[index])
            {
               StartMarker = markers.ChildMarker,
               EndMarker = markers.ParentMarker
            }.Draw(frame);
            return;
         }

         Connector.Draw(frame, diagram.Routes[index]);
      }

      private static CrowFootNotation.ConnectorMarkers CrowFootMarkers(
         ErdDiagram diagram, int index)
      {
         if (index >= 0 && index < diagram.Edges.Count)
         {
            return CrowFootNotation.ForEdge(diagram.Edges[index]);
         }
         return new CrowFootNotation.ConnectorMarkers(
            CrowFootNotation.CardinalityMarker.None,
            CrowFootNotation.CardinalityMarker.None);
      }

      private static void DrawUmlConnectorLabel(GlFrame frame,
         ErdDiagram diagram, int index)
      {
         string label = "";
         if (index < diagram.Edges.Count)
         {
            label = UmlProfile.AssociationLabel(diagram.Edges[index]);
         }
         else
         {
            int boxIndex = index - diagram.Edges.Count;
            if (boxIndex >= 0 && boxIndex < diagram.BoxEdges.Count)
            {
               label = diagram.BoxEdges[boxIndex].Label;
            }
         }
         if (string.IsNullOrEmpty(label))
         {
            return;
         }

         Point2 p = Midpoint(diagram.Routes[index]);
         using var paint = new SKPaint
         {
            IsAntialias = true,
            Color = SKColors.Black
         };
         using var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 11);
         frame.Canvas.DrawText(label, (float)p.X + 4, (float)p.Y - 4,
            SKTextAlign.Left, font, paint);
      }

      private static Point2 Midpoint(IReadOnlyList<Point2> pts)
      {
         if (pts.Count == 1)
         {
            return pts[0];
         }

         double total = OrthogonalRouter.PolylineLength(pts);
         double target = total / 2.0;
         double walked = 0;
         for (int i = 0; i < pts.Count - 1; i++)
         {
            Point2 a = pts[i];
            Point2 b = pts[i + 1];
            double length = Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
            if (walked + length >= target)
            {
               double remaining = target - walked;
               double dx = Math.Sign(b.X - a.X) *
                  Math.Min(Math.Abs(b.X - a.X), remaining);
               double dy = Math.Sign(b.Y - a.Y) *
                  Math.Min(Math.Abs(b.Y - a.Y), remaining);
               return new Point2(a.X + dx, a.Y + dy);
            }
            walked += length;
         }
         return pts[pts.Count - 1];
      }

   }

}
