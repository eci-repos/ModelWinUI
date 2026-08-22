using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;
using ModelConsole.Graph;
using ModelConsole.Skia.GLibrary;

// The collapsed-group box model lives in Model.Graph (GroupBox + GroupBoxEdge);
// this namespace's own GroupBox is the drawable primitive. Alias the model so
// the diagram's Boxes dictionary holds the metadata the renderer reads
// (Group, MemberCount) while the measure probe uses the primitive.
using GroupBoxModel = ModelConsole.Graph.GroupBox;

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

      /// <summary>
      /// Presentation notation used when measuring table probes. UML rows can
      /// be wider than ERD rows, so the layout must measure the chosen view.
      /// </summary>
      public DiagramNotation Notation { get; set; } = DiagramNotation.Erd;

      /// <summary>
      /// Name of the entity layout projection to use. Grid is the historical
      /// default and preserves the incoming row-major order.
      /// </summary>
      public string LayoutName { get; set; } = EntityLayout.GridName;
   }

   /// <summary>
   /// The result of composing an ERD: the measured table layout, the FK
   /// edges, and the routed connector polylines — plus, when groups are
   /// collapsed (backlog 039), the collapsed group boxes and their external
   /// connectors. Pure data — it can be cached and replayed across paints
   /// without re-running the router.
   /// </summary>
   public sealed class ErdDiagram
   {
      /// <summary>
      /// Measured rects keyed by table name <b>and</b> by collapsed-box key
      /// (<see cref="GroupBoxAggregation.BoxKey"/>) — position = layout slot,
      /// size = the table's measured size or the box's measured size. Boxes
      /// live in the same map as tables so they join fit bounds, hover, and
      /// the router's obstacle set unchanged.
      /// </summary>
      public IReadOnlyDictionary<string, Rect2> Layout { get; }

      /// <summary>
      /// FK edges extracted from the tables, in deterministic order — the
      /// visible edges whose endpoints are both outside every collapsed box
      /// (an edge touching a box is represented by the box's aggregated
      /// external edge instead). One <see cref="Routes"/> polyline per edge.
      /// </summary>
      public List<FkRelation> Edges { get; }

      /// <summary>
      /// Every routed connector polyline: the table FK routes first, then
      /// the collapsed boxes' external connectors (<see cref="BoxEdges"/> is
      /// parallel to that box portion). Drawn with the same Connector
      /// primitive, so hover + emphasis apply to box connectors too.
      /// </summary>
      public IReadOnlyList<IReadOnlyList<Point2>> Routes { get; }

      /// <summary>
      /// The collapsed-group boxes (backlog 039) keyed by their layout key.
      /// Each box's rect is in <see cref="Layout"/> under the same key.
      /// </summary>
      public IReadOnlyDictionary<string, GroupBoxModel> Boxes { get; }

      /// <summary>
      /// The boxes' aggregated external edges, in routing order — one
      /// <see cref="GroupBoxEdge"/> per connector, parallel to the box
      /// portion of <see cref="Routes"/>.
      /// </summary>
      public IReadOnlyList<GroupBoxEdge> BoxEdges { get; }

      /// <summary>FK resolution issues found while extracting the edges.</summary>
      public List<string> Issues { get; }

      /// <summary>
      /// ErdDiagram class initialization.
      /// </summary>
      /// <param name="layout">measured table + box layout</param>
      /// <param name="edges">routed FK edges</param>
      /// <param name="routes">routed polylines, one per edge + box edge</param>
      /// <param name="boxes">collapsed boxes keyed by layout key</param>
      /// <param name="boxEdges">the boxes' external edges, parallel to the box routes</param>
      /// <param name="issues">FK resolution issues</param>
      internal ErdDiagram(IReadOnlyDictionary<string, Rect2> layout,
         List<FkRelation> edges, IReadOnlyList<IReadOnlyList<Point2>> routes,
         IReadOnlyDictionary<string, GroupBoxModel> boxes,
         IReadOnlyList<GroupBoxEdge> boxEdges, List<string> issues)
      {
         Layout = layout;
         Edges = edges;
         Routes = routes;
         Boxes = boxes;
         BoxEdges = boxEdges;
         Issues = issues;
      }
   }

   /// <summary>
   /// Compose a full ERD from tables by writing code: measure each table,
   /// lay them out in a grid, extract the FK edges, resolve and fan out the
   /// anchors, and route every connector around the tables. When groups are
   /// collapsed (backlog 039) the visible tables of a collapsed group become
   /// one box rect in the grid, its FK edges aggregate to one connector per
   /// external target, and the box↔box / box↔table connectors are routed
   /// around the boxes too — so no connector crosses a collapsed box (the
   /// 012 invariant extended). This is the reusable, WinUI-free "define and
   /// draw an ERD" API on the Skia stack; the result is pure data that a
   /// renderer replays on every paint.
   /// </summary>
   public static class ErdComposer
   {

      /// <summary>
      /// Compose the ERD for the given tables. The
      /// <paramref name="measureFrame"/> is used only to measure the
      /// tables + boxes (its font) — nothing is drawn here.
      /// </summary>
      /// <param name="tables">tables to compose</param>
      /// <param name="measureFrame">frame used to measure table sizes</param>
      /// <param name="options">composition options</param>
      /// <param name="visibility">view-side visibility (backlog 038); the
      /// layout runs over the visible subset and only edges with both
      /// endpoints visible are routed. Null draws everything (pre-038).</param>
      /// <param name="collapse">view-side collapse state (backlog 039); a
      /// visible collapsed group draws as one box. Null keeps every group
      /// expanded (pre-039).</param>
      /// <param name="theme">the grouping theme the collapsed boxes' primary
      /// membership derives from (backlog 043; defaults to the tag theme —
      /// the pre-043 behavior)</param>
      /// <returns>the composed diagram (layout + edges + routes + boxes)</returns>
      public static ErdDiagram Compose(
         IReadOnlyList<TableInfo> tables, GlFrame measureFrame, ErdOptions options,
         EntityVisibility visibility = null, GroupCollapseState collapse = null,
         GroupingTheme theme = null)
      {
         var opts = options ?? new ErdOptions();
         var empty = new Dictionary<string, Rect2>();
         var noEdges = new List<FkRelation>();
         var noRoutes = new List<IReadOnlyList<Point2>>();
         var noIssues = new List<string>();
         var noBoxes = new Dictionary<string, GroupBoxModel>();
         var noBoxEdges = new List<GroupBoxEdge>();

         if (tables == null || tables.Count == 0)
         {
            return new ErdDiagram(empty, noEdges, noRoutes, noBoxes, noBoxEdges, noIssues);
         }

         // Backlog 038: extract the FULL edge set first so unresolved-FK
         // diagnostics (R8) still surface for edges whose endpoints are
         // hidden, then project to the visible subset — the same
         // projection the XAML path consumes (parity).
         var (edges, issues) = FkEdgeExtractor.Extract(tables);
         var (visibleTables, visibleEdges) =
            ModelProjection.Project(tables, edges, visibility);

         // Backlog 039: build the collapsed boxes over the visible set. A box
         // draws only when it has visible members (a group hidden by 038
         // collapses to nothing); the aggregation dedupes its external edges
         // to one connector per target — a target table, or a target
         // collapsed group's box.
         var collapsedSet = collapse == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(collapse.CollapsedGroups, StringComparer.Ordinal);
         var boxes = new List<(string Key, GroupBoxModel Box)>();
         if (collapsedSet.Count > 0)
         {
            foreach (var group in collapse.CollapsedGroups)
            {
               var model = GroupBoxAggregation.Build(
                  visibleTables, visibleEdges, group, collapsedSet, theme);
               if (model.MemberCount == 0)
               {
                  continue; // no visible members — nothing to collapse
               }
               boxes.Add((GroupBoxAggregation.BoxKey(group), model));
            }
         }

         // Every table inside a collapsed box: its table-level edges are
         // hidden (internal) or represented by the box's external edges.
         var boxed = new HashSet<string>(StringComparer.Ordinal);
         foreach (var (_, box) in boxes)
         {
            foreach (var m in box.Members)
            {
               boxed.Add(m.TableName);
            }
         }

         // 1. Measure — one probe per laid-out table (every visible table
         // except a collapsed box's members, which the box replaces) and one
         // probe per box (never drawn). The probes hold SKFonts, so dispose
         // them once the sizes are read.
         var probes = new Dictionary<string, Table>();
         double maxWidth = 0, maxHeight = 0;
         foreach (var t in visibleTables)
         {
            if (boxed.Contains(t.TableName))
            {
               continue;
            }
            var probe = new Table(measureFrame, 0, 0, opts.BannerHeight, t,
               opts.Notation);
            probes[t.TableName] = probe;
            maxWidth = Math.Max(maxWidth, probe.ComputedWidth);
            maxHeight = Math.Max(maxHeight, probe.ComputedHeight);
         }
         var boxSizes = new Dictionary<string, (float W, float H)>();
         foreach (var (key, box) in boxes)
         {
            var probe = new GroupBox(measureFrame, 0, 0, box.Group, box.MemberCount);
            boxSizes[key] = (probe.ComputedWidth, probe.ComputedHeight);
            maxWidth = Math.Max(maxWidth, probe.ComputedWidth);
            maxHeight = Math.Max(maxHeight, probe.ComputedHeight);
         }

         try
         {
            // 2. Layout — entity layout sized for the widest/tallest table or
            // box so nothing overlaps its neighbour. A collapsed group's
            // members are REPLACED by its box (one rect for the group — the
            // 2000-table win); a synthetic table per box feeds the layout
            // engine unchanged (it reads only TableName), so a boxed member
            // neither draws nor blocks the router.
            var layoutTables = new List<TableInfo>();
            foreach (var t in visibleTables)
            {
               if (!boxed.Contains(t.TableName))
               {
                  layoutTables.Add(t);
               }
            }
            foreach (var (key, _) in boxes)
            {
               layoutTables.Add(new TableInfo { TableName = key });
            }
            var layoutEdges = BuildLayoutEdges(visibleEdges, boxes, boxed);
            var layout = EntityLayoutEngine.Layout(
               layoutTables, layoutEdges, new EntityLayoutOptions
            {
               Columns = opts.Columns,
               SlotWidth = maxWidth + opts.SlotPadding,
               SlotHeight = maxHeight + opts.SlotPadding,
               Gutter = opts.Gutter
            }, EntityLayout.FromName(opts.LayoutName));

            // Measured rects at their slot positions (the router avoids
            // these; the renderer draws tables and boxes onto them).
            var rects = new Dictionary<string, Rect2>();
            foreach (var t in visibleTables)
            {
               if (boxed.Contains(t.TableName))
               {
                  continue;
               }
               var slot = layout[t.TableName];
               rects[t.TableName] = new Rect2(
                  slot.X, slot.Y,
                  probes[t.TableName].ComputedWidth,
                  probes[t.TableName].ComputedHeight);
            }
            var boxRects = new Dictionary<string, Rect2>();
            foreach (var (key, _) in boxes)
            {
               var slot = layout[key];
               var r = new Rect2(slot.X, slot.Y, boxSizes[key].W, boxSizes[key].H);
               rects[key] = r;
               boxRects[key] = r;
            }

            // 3. Edges — the visible FK subset whose endpoints are both
            // outside every box; edges touching a box route as the box's
            // aggregated external edges below instead.
            var tableEdges = new List<FkRelation>();
            foreach (var e in visibleEdges)
            {
               if (boxed.Contains(e.ChildTable) || boxed.Contains(e.ParentTable))
               {
                  continue;
               }
               tableEdges.Add(e);
            }

            // 4. Anchors — table FKs as before (fanned out per shared
            // column), then the boxes' external edges: one anchor pair per
            // external target, the box side facing the target resolved from
            // the relative position of the two rects (the box anchors at its
            // vertical midpoint — it has no column rows).
            var startGroups = tableEdges
               .GroupBy(e => e.ChildTable + "::" + e.ChildColumn)
               .ToDictionary(g => g.Key, g => g.ToList());
            var endGroups = tableEdges
               .GroupBy(e => e.ParentTable + "::" + e.ParentColumn)
               .ToDictionary(g => g.Key, g => g.ToList());

            var tableAnchors = new List<(ConnectorRouteRequest Route, FkRelation Edge)>();
            foreach (var edge in tableEdges)
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

               tableAnchors.Add((
                  new ConnectorRouteRequest(start, childSide, end, parentSide),
                  edge));
            }

            var boxAnchors = new List<(ConnectorRouteRequest Route, GroupBoxEdge Edge)>();
            var boxEdges = new List<GroupBoxEdge>();
            foreach (var (key, box) in boxes)
            {
               var boxRect = boxRects[key];
               foreach (var be in box.ExternalEdges)
               {
                  string targetKey = be.TargetGroup != null
                     ? GroupBoxAggregation.BoxKey(be.TargetGroup)
                     : be.TargetTable;
                  if (!rects.TryGetValue(targetKey, out var targetRect))
                  {
                     continue; // target box collapsed to nothing (all hidden)
                  }
                  var (start, end, startSide, endSide) = ConnectorAnchors.Resolve(
                     boxRect, targetRect, boxRect.Center.Y, targetRect.Center.Y);
                  boxAnchors.Add((
                     new ConnectorRouteRequest(start, startSide, end, endSide),
                     be));
                  boxEdges.Add(be);
               }
            }

            // 5. Routes — table FKs first, then the boxes' external
            // connectors, sequentially so no connector crosses another — nor
            // any box, which is in the obstacle set (the 012 invariant now
            // covers collapsed boxes).
            double minX = rects.Values.Min(r => r.X);
            double minY = rects.Values.Min(r => r.Y);
            double maxX = rects.Values.Max(r => r.Right);
            double maxY = rects.Values.Max(r => r.Bottom);
            var bounds = new Rect2(
               minX - opts.ExtentMargin, minY - opts.ExtentMargin,
               (maxX - minX) + 2 * opts.ExtentMargin,
               (maxY - minY) + 2 * opts.ExtentMargin);

            var allAnchors = new List<ConnectorRouteRequest>();
            foreach (var a in tableAnchors)
            {
               allAnchors.Add(a.Route);
            }
            foreach (var a in boxAnchors)
            {
               allAnchors.Add(a.Route);
            }

            var routes = SequentialRouter.RouteAll(
               allAnchors, rects.Values.ToList(), bounds, opts.RouterOptions);

            var boxesDict = boxes.ToDictionary(b => b.Key, b => b.Box);
            return new ErdDiagram(
               rects, tableEdges, routes, boxesDict, boxEdges, issues);
         }
         finally
         {
            foreach (var probe in probes.Values)
            {
               probe.Dispose();
            }
         }
      }

      private static IReadOnlyList<FkRelation> BuildLayoutEdges(
         IReadOnlyList<FkRelation> visibleEdges,
         IReadOnlyList<(string Key, GroupBoxModel Box)> boxes,
         HashSet<string> boxed)
      {
         var layoutEdges = new List<FkRelation>();
         foreach (var edge in visibleEdges)
         {
            if (!boxed.Contains(edge.ChildTable) && !boxed.Contains(edge.ParentTable))
            {
               layoutEdges.Add(edge);
            }
         }
         foreach (var (key, box) in boxes)
         {
            foreach (var edge in box.ExternalEdges)
            {
               string target = edge.TargetKey;
               if (edge.Outbound)
               {
                  layoutEdges.Add(new FkRelation(
                     key, "", target, "", edge.Sample.Constraint));
               }
               else
               {
                  layoutEdges.Add(new FkRelation(
                     target, "", key, "", edge.Sample.Constraint));
               }
            }
         }
         return layoutEdges;
      }

   }

}
