using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// A collapsed group's box model (backlog 039): the member tables (the
   /// union of tables whose <b>primary</b> tag is the group) and the box's
   /// external edges — FK edges with exactly one endpoint in the box,
   /// aggregated one connector per external target (a target table, or a
   /// target collapsed group when the target's own primary tag is collapsed).
   /// Internal edges (both endpoints in the box) are hidden when collapsed.
   /// </summary>
   public sealed class GroupBox
   {
      /// <summary>The group (tag) this box renders.</summary>
      public string Group { get; }

      /// <summary>The box's member tables, in model order.</summary>
      public IReadOnlyList<TableInfo> Members { get; }

      /// <summary>
      /// The box's external edges, aggregated per external target in
      /// first-seen order. One connector per target; <see cref="GroupBoxEdge.Count"/>
      /// carries how many FK relationships share it.
      /// </summary>
      public IReadOnlyList<GroupBoxEdge> ExternalEdges { get; }

      public GroupBox(
         string group, IReadOnlyList<TableInfo> members,
         IReadOnlyList<GroupBoxEdge> externalEdges)
      {
         Group = group;
         Members = members;
         ExternalEdges = externalEdges;
      }

      /// <summary>Member table count (rendered on the box).</summary>
      public int MemberCount => Members == null ? 0 : Members.Count;
   }

   /// <summary>
   /// One aggregated external edge of a collapsed group's box (backlog 039):
   /// all FK relationships between the box and a single external target
   /// collapse to one connector. <see cref="Outbound"/> tells the direction
   /// (box → target vs target → box); when several relationships share the
   /// target, <see cref="Count"/> labels the connector.
   /// </summary>
   public sealed class GroupBoxEdge
   {
      /// <summary>The concrete external table this edge leads to.</summary>
      public string TargetTable { get; }

      /// <summary>
      /// The collapsed group the target belongs to, or null when the target
      /// table draws individually (its primary group is expanded). When set,
      /// the connector runs box ↔ that group's box instead of to the table.
      /// </summary>
      public string TargetGroup { get; }

      /// <summary>true = the box points to the target; false = the target
      /// points into the box.</summary>
      public bool Outbound { get; }

      /// <summary>How many FK relationships share this target.</summary>
      public int Count { get; private set; }

      /// <summary>The first underlying FK edge (identity + model object for
      /// hover/click on the aggregated connector).</summary>
      public FkRelation Sample { get; }

      /// <summary>The layout key the connector routes to: the collapsed
      /// target group's box, or the target table.</summary>
      public string TargetKey => TargetGroup ?? TargetTable;

      /// <summary>Display text: the target (group or table) plus the count.</summary>
      public string Label =>
         (Outbound ? "→ " : "← ") + (TargetGroup ?? TargetTable) +
         (Count > 1 ? " ×" + Count : "");

      internal GroupBoxEdge(FkRelation sample, bool outbound,
         string targetGroup, string targetTable)
      {
         Sample = sample;
         Outbound = outbound;
         TargetGroup = targetGroup;
         TargetTable = targetTable;
         Count = 1;
      }

      internal void Add()
      {
         Count++;
      }
   }

   /// <summary>
   /// Pure aggregation of a collapsed group into a box (backlog 039): given
   /// the full table + edge set and the currently collapsed groups, compute
   /// the box's members and its aggregated external edges. Deterministic and
   /// unit-tested; feeds layout + routing exactly like a table's edges.
   /// <para><b>The overlap (entity in N groups) rule — the UML nuance:</b>
   /// an entity renders inside its <b>primary</b> group — its first
   /// non-empty tag — and in the other groups it appears only as an external
   /// connector target on that group's box (a reference stub). So membership
   /// is primary-tag membership, and an edge from group G to a
   /// primary-tagged-H table is an <i>external</i> edge of G whose target
   /// aggregates under H.</para>
   /// </summary>
   public static class GroupBoxAggregation
   {

      /// <summary>
      /// The layout key a collapsed group's box occupies — the name both
      /// renderers place in the layout grid and route connectors to/from. A
      /// collapsed group lays out as one rect: the composer feeds a synthetic
      /// table with this key to the grid engine (which only reads
      /// <c>TableName</c>) and sizes it to the box's measured size. The
      /// reserved <c>group::</c> prefix is the box space; real table names
      /// key by their own name and never collide.
      /// </summary>
      public static string BoxKey(string group)
      {
         return "group::" + group;
      }

      /// <summary>
      /// The group a table belongs to — its first non-empty tag (the UML
      /// owning-package rule). Null for an untagged table. This is the tag
      /// theme's primary-group function (backlog 043); theme-aware callers use
      /// <see cref="GroupingTheme.PrimaryGroupOf"/> instead.
      /// </summary>
      public static string PrimaryTag(TableInfo table)
      {
         return GroupingThemes.Tags.PrimaryGroupOf(table);
      }

      /// <summary>
      /// Build the collapsed-group box for one group: its member tables
      /// (visible tables whose primary group is the group) and its aggregated
      /// external edges (exactly one endpoint in the box, one connector per
      /// target object, counts collapsed). A null/empty <paramref name="tables"/>
      /// or <paramref name="edges"/> yields an empty box.
      /// </summary>
      /// <param name="tables">the full visible table set (the box's members
      /// are drawn from it)</param>
      /// <param name="edges">the visible edge set (both endpoints visible)</param>
      /// <param name="group">the collapsed group</param>
      /// <param name="collapsedGroups">every collapsed group, so targets
      /// whose members are also collapsed merge under their box</param>
      /// <param name="theme">the grouping theme the primary-group membership
      /// derives from (defaults to the tag theme — the pre-043 behavior)</param>
      /// <returns>the box model</returns>
      public static GroupBox Build(
         IReadOnlyList<TableInfo> tables,
         IReadOnlyList<FkRelation> edges,
         string group,
         IReadOnlyCollection<string> collapsedGroups,
         GroupingTheme theme = null)
      {
         theme = theme ?? GroupingThemes.Tags;
         var members = new List<TableInfo>();
         if (tables != null)
         {
            foreach (var t in tables)
            {
               if (t != null && string.Equals(
                  theme.PrimaryGroupOf(t), group, StringComparison.Ordinal))
               {
                  members.Add(t);
               }
            }
         }

         var memberNames = new HashSet<string>(
            members.Select(m => m.TableName), StringComparer.Ordinal);
         var byName = new Dictionary<string, TableInfo>(StringComparer.Ordinal);
         if (tables != null)
         {
            foreach (var t in tables)
            {
               if (t != null && !byName.ContainsKey(t.TableName))
               {
                  byName[t.TableName] = t;
               }
            }
         }

         var collapsed = new HashSet<string>(
            collapsedGroups ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
         var ordered = new List<GroupBoxEdge>();
         var byKey = new Dictionary<string, GroupBoxEdge>(StringComparer.Ordinal);

         if (edges != null)
         {
            foreach (var e in edges)
            {
               if (e == null)
               {
                  continue;
               }
               bool childIn = memberNames.Contains(e.ChildTable);
               bool parentIn = memberNames.Contains(e.ParentTable);
               if (childIn == parentIn)
               {
                  continue; // both inside (internal, hidden) or both outside (another box's concern)
               }

               bool outbound = childIn;
               string targetTable = outbound ? e.ParentTable : e.ChildTable;

               string targetGroup = null;
               if (byName.TryGetValue(targetTable, out TableInfo target))
               {
                  string primary = theme.PrimaryGroupOf(target);
                  if (primary != null && collapsed.Contains(primary))
                  {
                     targetGroup = primary;
                  }
               }

               string key = (outbound ? "O" : "I") + "|" +
                  (targetGroup ?? targetTable);
               if (byKey.TryGetValue(key, out GroupBoxEdge agg))
               {
                  agg.Add();
               }
               else
               {
                  agg = new GroupBoxEdge(e, outbound, targetGroup, targetTable);
                  byKey[key] = agg;
                  ordered.Add(agg);
               }
            }
         }

         return new GroupBox(group, members, ordered);
      }
   }

}
