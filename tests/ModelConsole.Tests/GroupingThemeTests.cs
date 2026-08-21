using System.Collections.Generic;
using System.Linq;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 043 — the grouping-theme abstraction: a named, pure mapping
   /// from a table to its group(s). Tags is the authored theme (a group = a
   /// tag); Schema, Kind, and Connectivity are derived automatically. These
   /// tests pin the group assignment, the deterministic universe ordering,
   /// the primary-vs-all membership split, and the connectivity theme's
   /// component naming.
   /// </summary>
   public class GroupingThemeTests
   {

      private static TableInfo Table(string name, string schema = null, params string[] tags)
      {
         return new TableInfo
         {
            TableName = name,
            SchemaName = schema,
            Tags = tags != null && tags.Length > 0 ? new List<string>(tags) : null
         };
      }

      [Fact]
      public void TagsThemePrimaryIsTheFirstNonBlankTag()
      {
         var theme = GroupingThemes.Tags;

         Assert.Equal("Core", theme.PrimaryGroupOf(Table("Orders", null, "Core", "Audit")));
         Assert.Equal("Core", theme.PrimaryGroupOf(Table("Blank", null, "  ", "Core")));
         Assert.Null(theme.PrimaryGroupOf(Table("Untagged")));
         Assert.Null(theme.PrimaryGroupOf(null));
      }

      [Fact]
      public void TagsThemeGroupsOfReturnsRawTags()
      {
         var theme = GroupingThemes.Tags;

         Assert.Equal(new[] { "Core", "Audit" },
            theme.GroupsOf(Table("Orders", null, "Core", "Audit")));
         Assert.Empty(theme.GroupsOf(Table("Untagged")));
      }

      [Fact]
      public void SchemaThemeGroupsBySchema()
      {
         var theme = GroupingThemes.Schema;

         Assert.Equal("Sales", theme.PrimaryGroupOf(Table("Orders", "Sales")));
         Assert.Equal(new[] { "Sales" }, theme.GroupsOf(Table("Orders", "Sales")));
         Assert.Null(theme.PrimaryGroupOf(Table("Untagged")));
         Assert.Empty(theme.GroupsOf(Table("Untagged")));
      }

      [Fact]
      public void KindThemeSplitsEntityAndReferenceCode()
      {
         var theme = GroupingThemes.Kind;

         // Ref* prefix → ReferenceCode; a plain table → Entity.
         Assert.Equal("ReferenceCode", theme.PrimaryGroupOf(Table("RefStatus")));
         Assert.Equal("Entity", theme.PrimaryGroupOf(Table("Orders")));
         Assert.Equal(new[] { "ReferenceCode" }, theme.GroupsOf(Table("RefStatus")));
      }

      [Fact]
      public void GroupsIsOrderedDistinctAndDropsBlanks()
      {
         var theme = GroupingThemes.Schema;

         var groups = theme.Groups(new[]
         {
            Table("A", "Zeta"),
            Table("B", "Alpha"),
            Table("C", "Zeta"),
            Table("D", "Alpha"),
            Table("E") // no schema — no group
         }).ToList();

         Assert.Equal(new[] { "Alpha", "Zeta" }, groups);
      }

      [Fact]
      public void ConnectivityGroupsConnectedComponents()
      {
         // A→B→C form one component (named by the smallest table, A); D is a
         // singleton and stays ungrouped.
         var tables = new[]
         {
            TableWithFk("A", "B"),
            TableWithFk("B", "C"),
            TableWithFk("C", null),
            TableWithFk("D", null)
         };
         var theme = GroupingThemes.Connectivity(tables);

         Assert.Equal("A", theme.PrimaryGroupOf(tables[0]));
         Assert.Equal("A", theme.PrimaryGroupOf(tables[1]));
         Assert.Equal("A", theme.PrimaryGroupOf(tables[2]));
         Assert.Null(theme.PrimaryGroupOf(tables[3])); // singleton → no group
         Assert.Equal(new[] { "A" }, theme.Groups(tables));
      }

      [Fact]
      public void ConnectivityComponentNameIsTheSmallestTableName()
      {
         // The component's group name is the lexicographically-smallest table
         // name, not the union-find root (which depends on union order).
         var tables = new[]
         {
            TableWithFk("Zulu", "Alpha"),
            TableWithFk("Alpha", "Mike")
         };
         var theme = GroupingThemes.Connectivity(tables);

         Assert.Equal("Alpha", theme.PrimaryGroupOf(tables[0]));
         Assert.Equal("Alpha", theme.PrimaryGroupOf(tables[1]));
      }

      [Fact]
      public void ConnectivityIsDeterministicAcrossEdgeOrder()
      {
         // Same component, edges listed in the opposite order — the group
         // names must not change (the repo bans nondeterministic output).
         var forward = new[]
         {
            TableWithFk("A", "B"),
            TableWithFk("B", "C")
         };
         var reversed = new[]
         {
            TableWithFk("B", "C"),
            TableWithFk("A", "B")
         };

         Assert.Equal(
            GroupingThemes.Connectivity(forward).Groups(forward),
            GroupingThemes.Connectivity(reversed).Groups(reversed));
      }

      [Fact]
      public void FromNameRoundTripsTheBuiltInThemes()
      {
         var tables = new[] { Table("A", "Sales") };

         Assert.Same(GroupingThemes.Tags,
            GroupingThemes.FromName(GroupingThemes.TagsName, tables));
         Assert.Same(GroupingThemes.Schema,
            GroupingThemes.FromName(GroupingThemes.SchemaName, tables));
         Assert.Same(GroupingThemes.Kind,
            GroupingThemes.FromName(GroupingThemes.KindName, tables));
         Assert.Equal(GroupingThemes.ConnectivityName,
            GroupingThemes.FromName(GroupingThemes.ConnectivityName, tables).Name);
         // Unknown names fall back to the tag theme (never throws).
         Assert.Same(GroupingThemes.Tags,
            GroupingThemes.FromName("Bogus", tables));
      }

      // ------------------------------------------------------------------

      private static TableInfo TableWithFk(string name, string parent)
      {
         var table = new TableInfo
         {
            TableName = name,
            Columns = new List<ColumnInfo>
            {
               new ColumnInfo
               {
                  ColumnName = "Id",
                  Type = DataInfo.VARCHAR,
                  IsKey = true,
                  Constraints = new List<ConstraintInfo>
                  {
                     new ConstraintInfo { Type = DataInfo.PRIMARY_KEY }
                  }
               }
            }
         };
         if (parent != null)
         {
            table.Columns.Add(new ColumnInfo
            {
               ColumnName = parent + "Id",
               Type = DataInfo.VARCHAR,
               IsForeignKey = true,
               Constraints = new List<ConstraintInfo>
               {
                  new ConstraintInfo
                  {
                     Type = DataInfo.FOREIGN_KEY,
                     ReferencedTableName = parent,
                     ReferencedColumnName = "Id"
                  }
               }
            });
         }
         return table;
      }
   }

}
