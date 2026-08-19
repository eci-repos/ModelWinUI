using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;

namespace ModelConsole.Skia.Primitives
{

    /// <summary>
    /// Options controlling how an ERD is composed. Defaults mirror the app's
    /// XAML path (<c>ModelPanelControl</c>).
    /// </summary>
    public sealed class ErdOptions
    {
        /// <summary>Height of the table banner, in pixels.</summary>
        public float BannerHeight { get; set; } = 40;

        /// <summary>Number of columns in the table grid (row-major fill).</summary>
        public int Columns { get; set; } = 7;

        /// <summary>Extra space added to every grid slot so a table fits its
        /// measured size plus breathing room.</summary>
        public double SlotPadding { get; set; } = 80;

        /// <summary>Spacing between grid cells.</summary>
        public double Gutter { get; set; } = 80;

        /// <summary>Margin around the content that the router region extends
        /// to (keeps the A* grid tight around the drawing).</summary>
        public double ExtentMargin { get; set; } = 80;

        /// <summary>Options passed to the orthogonal router.</summary>
        public RouterOptions RouterOptions { get; set; } =
            new RouterOptions { GridSize = 16, ObstacleMargin = 14, StubLength = 20 };
    }

    /// <summary>
    /// The result of composing an ERD: the measured table layout, the FK
    /// edges, and the routed connector polylines. Pure data — it can be
    /// cached and replayed across paints without re-running the router.
    /// </summary>
    public sealed class ErdDiagram
    {
        /// <summary>
        /// Measured table rects keyed by table name (position = layout slot,
        /// size = the table's measured size).
        /// </summary>
        public IReadOnlyDictionary<string, Rect2> Layout { get; }

        /// <summary>
        /// FK edges extracted from the tables, in deterministic order.
        /// </summary>
        public List<FkRelation> Edges { get; }

        /// <summary>
        /// One routed polyline per edge, in the same order as
        /// <see cref="Edges"/>.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Point2>> Routes { get; }

        /// <summary>FK resolution issues found while extracting the edges.</summary>
        public List<string> Issues { get; }

        /// <summary>
        /// ErdDiagram class initialization.
        /// </summary>
        /// <param name="layout">measured table layout</param>
        /// <param name="edges">FK edges</param>
        /// <param name="routes">routed polylines, one per edge</param>
        /// <param name="issues">FK resolution issues</param>
        internal ErdDiagram(IReadOnlyDictionary<string, Rect2> layout,
            List<FkRelation> edges, IReadOnlyList<IReadOnlyList<Point2>> routes,
            List<string> issues)
        {
            Layout = layout;
            Edges = edges;
            Routes = routes;
            Issues = issues;
        }
    }

    /// <summary>
    /// Compose a full ERD from tables by writing code: measure each table,
    /// lay them out in a grid, extract the FK edges, resolve and fan out the
    /// anchors, and route every connector around the tables. This is the
    /// reusable, WinUI-free "define and draw an ERD" API on the Skia stack;
    /// the result is pure data that a renderer replays on every paint.
    /// </summary>
    public static class ErdComposer
    {

        /// <summary>
        /// Compose the ERD for the given tables. The
        /// <paramref name="measureFrame"/> is used only to measure the
        /// tables (its font) — nothing is drawn here.
        /// </summary>
        /// <param name="tables">tables to compose</param>
        /// <param name="measureFrame">frame used to measure table sizes</param>
        /// <param name="options">composition options</param>
        /// <returns>the composed diagram (layout + edges + routes)</returns>
        public static ErdDiagram Compose(
            IReadOnlyList<TableInfo> tables, GlFrame measureFrame, ErdOptions options)
        {
            var opts = options ?? new ErdOptions();
            var empty = new Dictionary<string, Rect2>();
            var noEdges = new List<FkRelation>();
            var noRoutes = new List<IReadOnlyList<Point2>>();
            var noIssues = new List<string>();

            if (tables == null || tables.Count == 0)
            {
                return new ErdDiagram(empty, noEdges, noRoutes, noIssues);
            }

            // 1. Measure — one probe per table (never drawn). The probe holds
            // an SKFont, so dispose it once the sizes are read.
            var probes = new Dictionary<string, Table>();
            double maxWidth = 0, maxHeight = 0;
            foreach (var t in tables)
            {
                var probe = new Table(measureFrame, 0, 0, opts.BannerHeight, t);
                probes[t.TableName] = probe;
                maxWidth = Math.Max(maxWidth, probe.ComputedWidth);
                maxHeight = Math.Max(maxHeight, probe.ComputedHeight);
            }

            try
            {
                // 2. Layout — row-major grid sized for the widest/tallest
                // table so no table overlaps its neighbour.
                var layout = TableLayoutEngine.Layout(tables, new GridLayoutOptions
                {
                    Columns = opts.Columns,
                    SlotWidth = maxWidth + opts.SlotPadding,
                    SlotHeight = maxHeight + opts.SlotPadding,
                    Gutter = opts.Gutter
                });

                // Measured table rects at their slot positions (the router
                // avoids these; the renderer draws tables onto them).
                var rects = new Dictionary<string, Rect2>();
                foreach (var t in tables)
                {
                    var slot = layout[t.TableName];
                    rects[t.TableName] = new Rect2(
                        slot.X, slot.Y,
                        probes[t.TableName].ComputedWidth,
                        probes[t.TableName].ComputedHeight);
                }

                // 3. Edges.
                var (edges, issues) = FkEdgeExtractor.Extract(tables);

                // 4. Anchors — departure side from the relative position of
                // the two tables, fanned out per shared column (same grouping
                // as the app's XAML path).
                var startGroups = edges
                    .GroupBy(e => e.ChildTable + "::" + e.ChildColumn)
                    .ToDictionary(g => g.Key, g => g.ToList());
                var endGroups = edges
                    .GroupBy(e => e.ParentTable + "::" + e.ParentColumn)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var anchorEdges = new List<(Point2 Start, Point2 End, FkRelation Edge)>();
                foreach (var edge in edges)
                {
                    if (!rects.TryGetValue(edge.ChildTable, out var child) ||
                        !rects.TryGetValue(edge.ParentTable, out var parent))
                    {
                        continue;
                    }

                    // The probe sits at y = 0, so its row centers are
                    // table-relative; adding the slot's Y makes them absolute.
                    double childRowY = probes[edge.ChildTable].GetRowCenterY(
                        edge.ChildColumn) + child.Y;
                    double parentRowY = probes[edge.ParentTable].GetRowCenterY(
                        edge.ParentColumn) + parent.Y;

                    var (start, end, childSide, parentSide) = ConnectorAnchors.Resolve(
                        child, parent, childRowY, parentRowY);

                    var startGroup = startGroups[edge.ChildTable + "::" + edge.ChildColumn];
                    var endGroup = endGroups[edge.ParentTable + "::" + edge.ParentColumn];
                    start = ConnectorAnchors.FanOut(
                        start, childSide, startGroup.IndexOf(edge), startGroup.Count, 6);
                    end = ConnectorAnchors.FanOut(
                        end, parentSide, endGroup.IndexOf(edge), endGroup.Count, 6);

                    anchorEdges.Add((start, end, edge));
                }

                // 5. Routes — sequentially so no connector crosses another.
                double minX = rects.Values.Min(r => r.X);
                double minY = rects.Values.Min(r => r.Y);
                double maxX = rects.Values.Max(r => r.Right);
                double maxY = rects.Values.Max(r => r.Bottom);
                var bounds = new Rect2(
                    minX - opts.ExtentMargin, minY - opts.ExtentMargin,
                    (maxX - minX) + 2 * opts.ExtentMargin,
                    (maxY - minY) + 2 * opts.ExtentMargin);

                var routes = SequentialRouter.RouteAll(
                    anchorEdges.Select(a => (a.Start, a.End)).ToList(),
                    rects.Values.ToList(), bounds, opts.RouterOptions);

                return new ErdDiagram(rects, edges, routes, issues);
            }
            finally
            {
                foreach (var probe in probes.Values)
                {
                    probe.Dispose();
                }
            }
        }

    }

}
