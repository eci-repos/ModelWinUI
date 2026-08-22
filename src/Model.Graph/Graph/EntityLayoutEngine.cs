using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Geometry;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Options controlling deterministic entity layouts.
   /// </summary>
   public sealed class EntityLayoutOptions
   {
      /// <summary>Number of columns used by grid-like projections.</summary>
      public int Columns { get; set; } = 1;

      /// <summary>Width of every layout slot.</summary>
      public double SlotWidth { get; set; } = 300;

      /// <summary>Height of every layout slot.</summary>
      public double SlotHeight { get; set; } = 200;

      /// <summary>Horizontal and vertical spacing between slots.</summary>
      public double Gutter { get; set; } = 60;

      /// <summary>
      /// Whether the grid projection should use the connectivity-aware order.
      /// False preserves the historical row-major order exactly.
      /// </summary>
      public bool UseConnectivityOrdering { get; set; }

      /// <summary>
      /// Whether serpentine rows sweep from bottom to top instead of top to
      /// bottom.
      /// </summary>
      public bool SerpentineBottomUp { get; set; }
   }

   /// <summary>
   /// Name-driven entity-layout registry, matching the grouping-theme pattern.
   /// </summary>
   public sealed class EntityLayout
   {
      public const string GridName = "Grid";
      public const string SerpentineName = "Serpentine";
      public const string CircleName = "Circle";
      public const string CrossName = "Cross";

      public static readonly EntityLayout Grid =
         new EntityLayout(GridName, LayoutKind.Grid);
      public static readonly EntityLayout Serpentine =
         new EntityLayout(SerpentineName, LayoutKind.Serpentine);
      public static readonly EntityLayout Circle =
         new EntityLayout(CircleName, LayoutKind.Circle);
      public static readonly EntityLayout Cross =
         new EntityLayout(CrossName, LayoutKind.Cross);

      private readonly LayoutKind _kind;

      private EntityLayout(string name, LayoutKind kind)
      {
         Name = name;
         _kind = kind;
      }

      /// <summary>The display and shared-state name for this layout.</summary>
      public string Name { get; }

      /// <summary>All built-in layout names, in UI order.</summary>
      public static IReadOnlyList<string> Names { get; } =
         new[] { GridName, SerpentineName, CircleName, CrossName };

      /// <summary>
      /// Resolve a shared layout name. Unknown names fall back to Grid.
      /// </summary>
      public static EntityLayout FromName(string name)
      {
         if (string.Equals(name, SerpentineName, StringComparison.Ordinal))
         {
            return Serpentine;
         }
         if (string.Equals(name, CircleName, StringComparison.Ordinal))
         {
            return Circle;
         }
         if (string.Equals(name, CrossName, StringComparison.Ordinal))
         {
            return Cross;
         }
         return Grid;
      }

      internal LayoutKind Kind
      {
         get { return _kind; }
      }
   }

   internal enum LayoutKind
   {
      Grid,
      Serpentine,
      Circle,
      Cross
   }

   /// <summary>
   /// Places entity rectangles in deterministic layouts. Grid preserves the
   /// caller order by default; alternate layouts first cluster connected
   /// entities so FK endpoints tend to sit nearer each other before routing.
   /// </summary>
   public static class EntityLayoutEngine
   {
      /// <summary>
      /// Lay entities out by name into non-overlapping slots.
      /// </summary>
      /// <param name="entities">entities to place</param>
      /// <param name="edges">FK edges between entities, used for ordering</param>
      /// <param name="options">layout options</param>
      /// <param name="layout">layout kind; null resolves to Grid</param>
      /// <returns>an entity-name to rect mapping</returns>
      public static IReadOnlyDictionary<string, Rect2> Layout(
         IReadOnlyList<TableInfo> entities, IReadOnlyList<FkRelation> edges,
         EntityLayoutOptions options, EntityLayout layout = null)
      {
         var result = new Dictionary<string, Rect2>();
         if (entities == null || options == null)
         {
            return result;
         }

         var entityList = DistinctNamedEntities(entities);
         if (entityList.Count == 0)
         {
            return result;
         }

         var selected = layout ?? EntityLayout.Grid;
         bool orderByConnectivity =
            selected.Kind != LayoutKind.Grid || options.UseConnectivityOrdering;
         var inputOrder = entityList.Select(t => t.TableName).ToList();
         var ordered = orderByConnectivity
            ? OrderEntities(entityList, edges)
            : inputOrder;
         if (orderByConnectivity)
         {
            ordered = OptimizeProjectedOrder(
               inputOrder, ordered, edges, options, selected.Kind);
         }

         switch (selected.Kind)
         {
            case LayoutKind.Serpentine:
               return Serpentine(ordered, options);
            case LayoutKind.Circle:
               return Circle(ordered, options);
            case LayoutKind.Cross:
               return Cross(ordered, options);
            default:
               return Grid(ordered, options);
         }
      }

      /// <summary>
      /// Return the deterministic connectivity-aware entity order used by
      /// non-grid projections and opt-in grid ordering.
      /// </summary>
      public static IReadOnlyList<string> OrderEntities(
         IReadOnlyList<TableInfo> entities, IReadOnlyList<FkRelation> edges)
      {
         var entityList = DistinctNamedEntities(entities);
         var names = entityList.Select(t => t.TableName).ToList();
         if (names.Count == 0)
         {
            return names;
         }

         var nameSet = new HashSet<string>(names, StringComparer.Ordinal);
         var adjacency = BuildAdjacency(names, edges);
         var parent = names.ToDictionary(n => n, n => n, StringComparer.Ordinal);

         foreach (var edge in UsefulEdges(edges, nameSet))
         {
            Union(parent, edge.ChildTable, edge.ParentTable);
         }

         var components = names
            .GroupBy(n => Find(parent, n), StringComparer.Ordinal)
            .Select(g => g.ToList())
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.Sum(n => adjacency[n].Count))
            .ThenBy(g => g.Min(StringComparer.Ordinal), StringComparer.Ordinal)
            .ToList();

         var ordered = new List<string>();
         foreach (var component in components)
         {
            ordered.AddRange(OrderComponent(component, adjacency));
         }
         return ordered;
      }

      private static IReadOnlyList<TableInfo> DistinctNamedEntities(
         IReadOnlyList<TableInfo> entities)
      {
         var seen = new HashSet<string>(StringComparer.Ordinal);
         var list = new List<TableInfo>();
         foreach (var entity in entities)
         {
            if (entity == null || string.IsNullOrEmpty(entity.TableName))
            {
               continue;
            }
            if (seen.Add(entity.TableName))
            {
               list.Add(entity);
            }
         }
         return list;
      }

      private static Dictionary<string, HashSet<string>> BuildAdjacency(
         IReadOnlyList<string> names, IReadOnlyList<FkRelation> edges)
      {
         var adjacency = names.ToDictionary(
            n => n, n => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
         var nameSet = new HashSet<string>(names, StringComparer.Ordinal);
         foreach (var edge in UsefulEdges(edges, nameSet))
         {
            adjacency[edge.ChildTable].Add(edge.ParentTable);
            adjacency[edge.ParentTable].Add(edge.ChildTable);
         }
         return adjacency;
      }

      private static IEnumerable<FkRelation> UsefulEdges(
         IReadOnlyList<FkRelation> edges, HashSet<string> names)
      {
         if (edges == null)
         {
            yield break;
         }
         foreach (var edge in edges)
         {
            if (edge == null ||
                string.Equals(edge.ChildTable, edge.ParentTable,
                   StringComparison.Ordinal))
            {
               continue;
            }
            if (names.Contains(edge.ChildTable) && names.Contains(edge.ParentTable))
            {
               yield return edge;
            }
         }
      }

      private static IReadOnlyList<string> OrderComponent(
         IReadOnlyList<string> component,
         Dictionary<string, HashSet<string>> adjacency)
      {
         var remaining = new HashSet<string>(component, StringComparer.Ordinal);
         var visited = new HashSet<string>(StringComparer.Ordinal);
         var ordered = new List<string>();

         while (remaining.Count > 0)
         {
            string current = remaining
               .OrderByDescending(n => adjacency[n].Count)
               .ThenBy(n => n, StringComparer.Ordinal)
               .First();

            while (current != null)
            {
               remaining.Remove(current);
               visited.Add(current);
               ordered.Add(current);

               current = remaining
                  .OrderByDescending(n => adjacency[n].Count(v => visited.Contains(v)))
                  .ThenByDescending(n => adjacency[n].Count)
                  .ThenBy(n => n, StringComparer.Ordinal)
                  .FirstOrDefault(n => adjacency[n].Any(v => visited.Contains(v)));
            }
         }

         return ordered;
      }

      private static IReadOnlyList<string> RotateAtWeakestCut(
         IReadOnlyList<string> ordered, IReadOnlyList<FkRelation> edges)
      {
         if (ordered.Count < 3 || edges == null || edges.Count == 0)
         {
            return ordered;
         }

         var index = new Dictionary<string, int>(StringComparer.Ordinal);
         for (int i = 0; i < ordered.Count; i++)
         {
            index[ordered[i]] = i;
         }

         int bestGap = ordered.Count - 1;
         int bestScore = int.MaxValue;
         for (int gap = 0; gap < ordered.Count; gap++)
         {
            int score = 0;
            foreach (var edge in edges)
            {
               if (edge == null ||
                   !index.TryGetValue(edge.ChildTable, out int a) ||
                   !index.TryGetValue(edge.ParentTable, out int b))
               {
                  continue;
               }
               bool leftA = a <= gap;
               bool leftB = b <= gap;
               if (leftA != leftB)
               {
                  score++;
               }
            }
            if (score < bestScore ||
                (score == bestScore &&
                 string.CompareOrdinal(ordered[(gap + 1) % ordered.Count],
                    ordered[(bestGap + 1) % ordered.Count]) < 0))
            {
               bestScore = score;
               bestGap = gap;
            }
         }

         var rotated = new List<string>();
         for (int i = 1; i <= ordered.Count; i++)
         {
            rotated.Add(ordered[(bestGap + i) % ordered.Count]);
         }
         return rotated;
      }

      private static IReadOnlyList<string> OptimizeProjectedOrder(
         IReadOnlyList<string> inputOrder, IReadOnlyList<string> graphOrder,
         IReadOnlyList<FkRelation> edges, EntityLayoutOptions options,
         LayoutKind kind)
      {
         var candidates = new List<List<string>>
         {
            inputOrder.ToList(),
            graphOrder.ToList()
         };
         candidates.Add(BarycentricOrder(inputOrder, edges));

         var best = candidates
            .OrderBy(c => ProjectedSpan(c, edges, options, kind))
            .ThenBy(c => string.Join("|", c), StringComparer.Ordinal)
            .First();

         if (best.Count <= 150)
         {
            return ImproveByPairSwaps(best, edges, options, kind);
         }
         return ImproveByAdjacentSwaps(best, edges, options, kind);
      }

      private static List<string> BarycentricOrder(
         IReadOnlyList<string> inputOrder, IReadOnlyList<FkRelation> edges)
      {
         var index = new Dictionary<string, int>(StringComparer.Ordinal);
         for (int i = 0; i < inputOrder.Count; i++)
         {
            index[inputOrder[i]] = i;
         }
         var neighbours = inputOrder.ToDictionary(
            n => n, n => new List<int>(), StringComparer.Ordinal);
         var names = new HashSet<string>(inputOrder, StringComparer.Ordinal);
         foreach (var edge in UsefulEdges(edges, names))
         {
            neighbours[edge.ChildTable].Add(index[edge.ParentTable]);
            neighbours[edge.ParentTable].Add(index[edge.ChildTable]);
         }
         return inputOrder
            .OrderBy(n => neighbours[n].Count == 0
               ? index[n]
               : neighbours[n].Average())
            .ThenBy(n => index[n])
            .ToList();
      }

      private static IReadOnlyList<string> ImproveByPairSwaps(
         List<string> order, IReadOnlyList<FkRelation> edges,
         EntityLayoutOptions options, LayoutKind kind)
      {
         double best = ProjectedSpan(order, edges, options, kind);
         bool improved = true;
         int pass = 0;
         while (improved && pass < 4)
         {
            improved = false;
            pass++;
            for (int i = 0; i < order.Count - 1; i++)
            {
               for (int j = i + 1; j < order.Count; j++)
               {
                  Swap(order, i, j);
                  double span = ProjectedSpan(order, edges, options, kind);
                  if (span + 0.0001 < best)
                  {
                     best = span;
                     improved = true;
                  }
                  else
                  {
                     Swap(order, i, j);
                  }
               }
            }
         }
         return order;
      }

      private static IReadOnlyList<string> ImproveByAdjacentSwaps(
         List<string> order, IReadOnlyList<FkRelation> edges,
         EntityLayoutOptions options, LayoutKind kind)
      {
         double best = ProjectedSpan(order, edges, options, kind);
         bool improved = true;
         int pass = 0;
         while (improved && pass < 6)
         {
            improved = false;
            pass++;
            for (int i = 0; i < order.Count - 1; i++)
            {
               Swap(order, i, i + 1);
               double span = ProjectedSpan(order, edges, options, kind);
               if (span + 0.0001 < best)
               {
                  best = span;
                  improved = true;
               }
               else
               {
                  Swap(order, i, i + 1);
               }
            }
         }
         return order;
      }

      private static double ProjectedSpan(
         IReadOnlyList<string> order, IReadOnlyList<FkRelation> edges,
         EntityLayoutOptions options, LayoutKind kind)
      {
         var positions = Project(order, options, kind);
         var names = new HashSet<string>(order, StringComparer.Ordinal);
         double total = 0;
         foreach (var edge in UsefulEdges(edges, names))
         {
            var child = positions[edge.ChildTable];
            var parent = positions[edge.ParentTable];
            total += Math.Abs(child.X - parent.X) + Math.Abs(child.Y - parent.Y);
         }
         return total;
      }

      private static IReadOnlyDictionary<string, Point2> Project(
         IReadOnlyList<string> names, EntityLayoutOptions options, LayoutKind kind)
      {
         var rects = kind switch
         {
            LayoutKind.Serpentine => Serpentine(names, options),
            LayoutKind.Circle => Circle(names, options),
            LayoutKind.Cross => Cross(names, options),
            _ => Grid(names, options)
         };
         return rects.ToDictionary(
            kv => kv.Key, kv => kv.Value.Center, StringComparer.Ordinal);
      }

      private static void Swap(List<string> order, int left, int right)
      {
         string temp = order[left];
         order[left] = order[right];
         order[right] = temp;
      }

      private static IReadOnlyDictionary<string, Rect2> Grid(
         IReadOnlyList<string> names, EntityLayoutOptions options)
      {
         var result = new Dictionary<string, Rect2>();
         int columns = Math.Max(1, options.Columns);
         double pitchX = options.SlotWidth + options.Gutter;
         double pitchY = options.SlotHeight + options.Gutter;

         for (int i = 0; i < names.Count; i++)
         {
            int col = i % columns;
            int row = i / columns;
            result[names[i]] = new Rect2(
               col * pitchX, row * pitchY,
               options.SlotWidth, options.SlotHeight);
         }
         return result;
      }

      private static IReadOnlyDictionary<string, Rect2> Serpentine(
         IReadOnlyList<string> names, EntityLayoutOptions options)
      {
         var result = new Dictionary<string, Rect2>();
         int columns = Math.Max(1, options.Columns);
         int rows = (int)Math.Ceiling(names.Count / (double)columns);
         double pitchX = options.SlotWidth + options.Gutter;
         double pitchY = options.SlotHeight + options.Gutter;

         for (int i = 0; i < names.Count; i++)
         {
            int sourceRow = i / columns;
            int col = i % columns;
            if (sourceRow % 2 == 1)
            {
               col = columns - 1 - col;
            }

            int row = options.SerpentineBottomUp
               ? rows - 1 - sourceRow
               : sourceRow;
            result[names[i]] = new Rect2(
               col * pitchX, row * pitchY,
               options.SlotWidth, options.SlotHeight);
         }
         return result;
      }

      private static IReadOnlyDictionary<string, Rect2> Circle(
         IReadOnlyList<string> names, EntityLayoutOptions options)
      {
         var result = new Dictionary<string, Rect2>();
         if (names.Count == 1)
         {
            result[names[0]] = new Rect2(0, 0, options.SlotWidth, options.SlotHeight);
            return result;
         }

         var slots = FilledCircleSlots(names.Count, options);
         for (int i = 0; i < names.Count; i++)
         {
            result[names[i]] = slots[i];
         }
         return result;
      }

      private static IReadOnlyList<Rect2> FilledCircleSlots(
         int count, EntityLayoutOptions options)
      {
         double pitchX = options.SlotWidth + options.Gutter;
         double pitchY = options.SlotHeight + options.Gutter;
         double radiusUnit = Math.Max(pitchX, pitchY);
         var cells = new List<(int Col, int Row, double Distance, double Angle)>();

         int shell = 0;
         while (cells.Count < count)
         {
            shell++;
            cells.Clear();
            double radius = shell * radiusUnit;
            for (int row = -shell; row <= shell; row++)
            {
               for (int col = -shell; col <= shell; col++)
               {
                  double x = col * pitchX;
                  double y = row * pitchY;
                  double distance = Math.Sqrt(x * x + y * y);
                  if (distance <= radius + 0.0001)
                  {
                     double angle = Math.Atan2(y, x);
                     cells.Add((col, row, distance, angle));
                  }
               }
            }
         }

         var ordered = cells
            .OrderBy(c => c.Distance)
            .ThenBy(c => c.Angle)
            .ThenBy(c => c.Row)
            .ThenBy(c => c.Col)
            .Take(count)
            .Select(c => new Rect2(
               c.Col * pitchX - options.SlotWidth / 2,
               c.Row * pitchY - options.SlotHeight / 2,
               options.SlotWidth,
               options.SlotHeight))
            .ToList();

         double minX = ordered.Min(r => r.X);
         double minY = ordered.Min(r => r.Y);
         for (int i = 0; i < ordered.Count; i++)
         {
            var r = ordered[i];
            ordered[i] = new Rect2(
               r.X - minX, r.Y - minY, r.Width, r.Height);
         }
         return ordered;
      }

      private static IReadOnlyDictionary<string, Rect2> Cross(
         IReadOnlyList<string> names, EntityLayoutOptions options)
      {
         var result = new Dictionary<string, Rect2>();
         if (names.Count == 1)
         {
            result[names[0]] = new Rect2(0, 0, options.SlotWidth, options.SlotHeight);
            return result;
         }

         var slots = FilledCrossSlots(names.Count, options);
         for (int i = 0; i < names.Count; i++)
         {
            result[names[i]] = slots[i];
         }
         return result;
      }

      private static IReadOnlyList<Rect2> FilledCrossSlots(
         int count, EntityLayoutOptions options)
      {
         double pitchX = options.SlotWidth + options.Gutter;
         double pitchY = options.SlotHeight + options.Gutter;
         var cells = new List<(int Col, int Row, int Ring, int BarDepth, double Angle)>();

         int extent = 0;
         int halfThickness = 1;
         while (cells.Count < count)
         {
            extent++;
            if (extent > 2 && CountCrossCells(extent, halfThickness) < count)
            {
               halfThickness++;
            }

            cells.Clear();
            for (int row = -extent; row <= extent; row++)
            {
               for (int col = -extent; col <= extent; col++)
               {
                  if (Math.Abs(col) > halfThickness &&
                      Math.Abs(row) > halfThickness)
                  {
                     continue;
                  }

                  int ring = Math.Max(Math.Abs(col), Math.Abs(row));
                  int barDepth = Math.Min(Math.Abs(col), Math.Abs(row));
                  double angle = Math.Atan2(row, col);
                  cells.Add((col, row, ring, barDepth, angle));
               }
            }
         }

         var ordered = cells
            .OrderBy(c => c.Ring)
            .ThenBy(c => c.BarDepth)
            .ThenBy(c => c.Angle)
            .ThenBy(c => c.Row)
            .ThenBy(c => c.Col)
            .Take(count)
            .Select(c => new Rect2(
               c.Col * pitchX - options.SlotWidth / 2,
               c.Row * pitchY - options.SlotHeight / 2,
               options.SlotWidth,
               options.SlotHeight))
            .ToList();

         double minX = ordered.Min(r => r.X);
         double minY = ordered.Min(r => r.Y);
         for (int i = 0; i < ordered.Count; i++)
         {
            var r = ordered[i];
            ordered[i] = new Rect2(
               r.X - minX, r.Y - minY, r.Width, r.Height);
         }
         return ordered;
      }

      private static int CountCrossCells(int extent, int halfThickness)
      {
         int width = 2 * extent + 1;
         int thickness = Math.Min(width, 2 * halfThickness + 1);
         int barArea = thickness * width;
         return barArea + barArea - thickness * thickness;
      }

      private static string Find(Dictionary<string, string> parent, string name)
      {
         if (parent[name] != name)
         {
            parent[name] = Find(parent, parent[name]);
         }
         return parent[name];
      }

      private static void Union(
         Dictionary<string, string> parent, string left, string right)
      {
         string leftRoot = Find(parent, left);
         string rightRoot = Find(parent, right);
         if (leftRoot == rightRoot)
         {
            return;
         }
         if (string.CompareOrdinal(leftRoot, rightRoot) < 0)
         {
            parent[rightRoot] = leftRoot;
         }
         else
         {
            parent[leftRoot] = rightRoot;
         }
      }
   }

}
