using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// A grouping theme (backlog 043): a named, pure mapping from a table to
   /// its group(s). Themes make grouping a first-class, view-time capability —
   /// groups can be <b>derived automatically</b> (schema, table kind, FK
   /// connectivity) as well as authored (tags). The theme is immutable, so it
   /// is safe to capture on the Skia compose thread.
   /// <para>Two membership notions, mirroring the 038/039 machinery:</para>
   /// <list type="bullet">
   /// <item><b>Primary group</b> — the table's <i>owning</i> group (the UML
   /// owning-package rule): collapse/box membership and the explorer's group
   /// counts use it. Null = the table belongs to no group.</item>
   /// <item><b>All groups</b> — every group the table belongs to: the
   /// visibility composition rule (<see cref="EntityVisibility.IsVisible"/>)
   /// uses it, so hiding a group hides all its members.</item>
   /// </list>
   /// For the derived themes (schema/kind/connectivity) the two coincide; for
   /// tags a table can carry several tags, of which the first is primary.
   /// </summary>
   public sealed class GroupingTheme
   {
      private readonly Func<TableInfo, string> _primaryGroup;
      private readonly Func<TableInfo, IEnumerable<string>> _allGroups;

      /// <summary>The theme's display name (the explorer's "Group by:" value).</summary>
      public string Name { get; }

      /// <summary>
      /// Create a theme from two pure functions.
      /// </summary>
      /// <param name="name">display name</param>
      /// <param name="primaryGroupOf">table → its owning group (null = none)</param>
      /// <param name="groupsOf">table → every group it belongs to</param>
      public GroupingTheme(
         string name,
         Func<TableInfo, string> primaryGroupOf,
         Func<TableInfo, IEnumerable<string>> groupsOf)
      {
         Name = name;
         _primaryGroup = primaryGroupOf;
         _allGroups = groupsOf;
      }

      /// <summary>
      /// The table's owning group (the UML owning-package rule), or null when
      /// it belongs to no group. Collapse/box membership and group counts use
      /// this.
      /// </summary>
      public string PrimaryGroupOf(TableInfo table)
      {
         return _primaryGroup(table);
      }

      /// <summary>
      /// Every group the table belongs to (the visibility membership test).
      /// </summary>
      public IEnumerable<string> GroupsOf(TableInfo table)
      {
         return _allGroups(table) ?? Enumerable.Empty<string>();
      }

      /// <summary>
      /// The theme's group universe over a table set: the ordered-distinct
      /// group names (Ordinal, deterministic — the repo bans nondeterministic
      /// output). Empty/whitespace group names are dropped.
      /// </summary>
      public IEnumerable<string> Groups(IEnumerable<TableInfo> tables)
      {
         var set = new HashSet<string>(StringComparer.Ordinal);
         if (tables != null)
         {
            foreach (var t in tables)
            {
               if (t == null)
               {
                  continue;
               }
               foreach (var g in GroupsOf(t))
               {
                  if (!string.IsNullOrEmpty(g))
                  {
                     set.Add(g);
                  }
               }
            }
         }
         return set.OrderBy(g => g, StringComparer.Ordinal);
      }
   }

   /// <summary>
   /// The built-in grouping themes (backlog 043): tags (the authored theme,
   /// backlog 037), schema, table kind, and FK connectivity (auto-derived).
   /// The theme is <b>model-dependent</b> — connectivity needs the FK graph —
   /// so shared state is the theme <b>name</b> (string) and the concrete
   /// <see cref="GroupingTheme"/> is derived from <c>tables + name</c> at each
   /// use site via <see cref="FromName"/>. This keeps a model change (File →
   /// Open) from ever leaving a stale connectivity theme behind.
   /// </summary>
   public static class GroupingThemes
   {
      /// <summary>The authored theme: a group = a <see cref="TableInfo.Tags"/> value.</summary>
      public const string TagsName = "Tags";

      /// <summary>The zero-authoring structural theme: a group = a schema.</summary>
      public const string SchemaName = "Schema";

      /// <summary>The coarse always-available theme: entity vs reference-code.</summary>
      public const string KindName = "Kind";

      /// <summary>The no-metadata discovery theme: FK-connected components.</summary>
      public const string ConnectivityName = "Connectivity";

      /// <summary>
      /// The authored theme (backlog 037): a group = a tag. Primary group =
      /// the first non-whitespace tag (the 039 owning-package rule); all
      /// groups = the raw tag list (byte-identical to the pre-043 visibility
      /// behavior).
      /// </summary>
      public static GroupingTheme Tags { get; } = new GroupingTheme(
         TagsName,
         table =>
         {
            if (table == null || table.Tags == null)
            {
               return null;
            }
            foreach (var tag in table.Tags)
            {
               if (!string.IsNullOrWhiteSpace(tag))
               {
                  return tag;
               }
            }
            return null;
         },
         table => table == null ? Enumerable.Empty<string>() : table.Tags);

      /// <summary>
      /// The schema theme: a group = the table's schema. Zero authoring for
      /// multi-schema models — the containerized form (backlog 023) already
      /// populates <see cref="TableInfo.SchemaName"/> per table. A table with
      /// no schema is ungrouped.
      /// </summary>
      public static GroupingTheme Schema { get; } = new GroupingTheme(
         SchemaName,
         table => table == null ? null : table.SchemaName,
         table => table == null || string.IsNullOrEmpty(table.SchemaName)
            ? Enumerable.Empty<string>()
            : new[] { table.SchemaName });

      /// <summary>
      /// The kind theme: a group = the table's <see cref="TableKind"/> (entity
      /// vs reference-code) via <see cref="TableKindClassifier.Classify"/>.
      /// Coarse, but always available and a useful first split on any model.
      /// </summary>
      public static GroupingTheme Kind { get; } = new GroupingTheme(
         KindName,
         table => table == null ? null : TableKindClassifier.Classify(table).ToString(),
         table => table == null
            ? Enumerable.Empty<string>()
            : new[] { TableKindClassifier.Classify(table).ToString() });

      /// <summary>
      /// The connectivity theme: weakly-connected components of the FK graph
      /// (union-find over <see cref="FkEdgeExtractor"/>'s deterministic edge
      /// order). Components with ≥ 2 tables become groups — the auto-suggested
      /// functional clusters for a single-schema, untagged model; singletons
      /// stay ungrouped (always visible per the composition rule). A
      /// component's group name is its <b>lexicographically-smallest table
      /// name</b> — deterministic, order-independent, unique, and a meaningful
      /// anchor (not the union-find root, which depends on union order).
      /// </summary>
      public static GroupingTheme Connectivity(IReadOnlyList<TableInfo> tables)
      {
         var edges = FkEdgeExtractor.Extract(tables).Edges;

         var parent = new Dictionary<string, string>(StringComparer.Ordinal);
         if (tables != null)
         {
            foreach (var t in tables)
            {
               if (t != null)
               {
                  parent[t.TableName] = t.TableName;
               }
            }
         }
         foreach (var e in edges)
         {
            if (e == null)
            {
               continue;
            }
            Union(parent, e.ChildTable, e.ParentTable);
         }

         var components = new Dictionary<string, List<string>>(StringComparer.Ordinal);
         if (tables != null)
         {
            foreach (var t in tables)
            {
               if (t == null)
               {
                  continue;
               }
               var root = Find(parent, t.TableName);
               if (!components.TryGetValue(root, out var list))
               {
                  list = new List<string>();
                  components[root] = list;
               }
               list.Add(t.TableName);
            }
         }

         var groupByTable = new Dictionary<string, string>(StringComparer.Ordinal);
         foreach (var kv in components)
         {
            if (kv.Value.Count < 2)
            {
               continue; // singletons stay ungrouped
            }
            var name = kv.Value.OrderBy(n => n, StringComparer.Ordinal).First();
            foreach (var t in kv.Value)
            {
               groupByTable[t] = name;
            }
         }

         return new GroupingTheme(
            ConnectivityName,
            table => table != null && groupByTable.TryGetValue(table.TableName, out var g)
               ? g : null,
            table => table != null && groupByTable.TryGetValue(table.TableName, out var g)
               ? new[] { g } : Enumerable.Empty<string>());
      }

      /// <summary>
      /// Resolve a theme by name over a table set. "Connectivity" derives the
      /// theme from the tables (it needs the FK graph); the others are
      /// model-independent singletons. Unknown names fall back to
      /// <see cref="Tags"/> (never throws).
      /// </summary>
      public static GroupingTheme FromName(string name, IReadOnlyList<TableInfo> tables)
      {
         switch (name)
         {
            case SchemaName:
               return Schema;
            case KindName:
               return Kind;
            case ConnectivityName:
               return Connectivity(tables);
            default:
               return Tags;
         }
      }

      private static string Find(Dictionary<string, string> parent, string x)
      {
         if (!parent.TryGetValue(x, out var root) || string.Equals(root, x, StringComparison.Ordinal))
         {
            return x;
         }
         root = Find(parent, root);
         parent[x] = root; // path compression
         return root;
      }

      private static void Union(Dictionary<string, string> parent, string a, string b)
      {
         var ra = Find(parent, a);
         var rb = Find(parent, b);
         if (!string.Equals(ra, rb, StringComparison.Ordinal))
         {
            parent[ra] = rb;
         }
      }
   }

}
