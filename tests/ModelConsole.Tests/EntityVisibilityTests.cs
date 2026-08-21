using System.Collections.Generic;

using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 038 — the visibility composition rule: draw entity E iff NOT
   /// pinned-hide AND (pinned-show OR belongs to ≥ 1 visible group OR belongs
   /// to no group). This is the single deterministic rule both renderers
   /// consume, so these tests pin the exact boundaries (pins beat groups, an
   /// ungrouped table is always visible, focus narrows to a chosen set).
   /// </summary>
   public class EntityVisibilityTests
   {

      private static TableInfo Table(string name, params string[] tags)
      {
         return new TableInfo
         {
            TableName = name,
            Tags = tags != null && tags.Length > 0 ? new List<string>(tags) : null
         };
      }

      [Fact]
      public void FreshVisibilityShowsEverything()
      {
         var visibility = EntityVisibility.Create(new[]
         {
            Table("Orders", "Core"),
            Table("LineItems", "Core", "Audit"),
            Table("Lookup")   // ungrouped
         });

         Assert.True(visibility.IsVisible(Table("Orders", "Core")));
         Assert.True(visibility.IsVisible(Table("LineItems", "Core", "Audit")));
         Assert.True(visibility.IsVisible(Table("Lookup")));
         Assert.True(visibility.IsShowAll);
      }

      [Fact]
      public void HideGroupHidesItsMembersOnly()
      {
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Core", false);

         Assert.False(visibility.IsVisible(Table("Orders", "Core")));
         Assert.True(visibility.IsVisible(Table("AuditLog", "Audit")));
         Assert.True(visibility.IsVisible(Table("Lookup"))); // ungrouped
         // A table in a visible group AND a hidden group still draws (≥1).
         Assert.True(visibility.IsVisible(Table("LineItems", "Core", "Audit")));
      }

      [Fact]
      public void ShowGroupRestoresItsMembers()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         visibility.SetGroupVisible("Core", false);
         Assert.False(visibility.IsVisible(Table("Orders", "Core")));

         visibility.SetGroupVisible("Core", true);
         Assert.True(visibility.IsVisible(Table("Orders", "Core")));
         Assert.True(visibility.IsShowAll);
      }

      [Fact]
      public void PinShowBeatsGroupHide()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         visibility.SetGroupVisible("Core", false);
         visibility.PinShow("Orders");

         Assert.True(visibility.IsVisible(Table("Orders", "Core")));
      }

      [Fact]
      public void PinHideBeatsGroupShow()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         visibility.PinHide("Orders");

         Assert.False(visibility.IsVisible(Table("Orders", "Core")));
      }

      [Fact]
      public void PinHideHidesAnUngroupedTable()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         Assert.True(visibility.IsVisible(Table("Lookup")));

         visibility.PinHide("Lookup");
         Assert.False(visibility.IsVisible(Table("Lookup")));
      }

      [Fact]
      public void ClearPinRestoresGroupRule()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         visibility.SetGroupVisible("Core", false);
         visibility.PinShow("Orders");
         Assert.True(visibility.IsVisible(Table("Orders", "Core")));

         visibility.ClearPin("Orders");
         Assert.False(visibility.IsVisible(Table("Orders", "Core")));
      }

      [Fact]
      public void PinStateReflectsThePin()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         Assert.Null(visibility.PinState("Orders"));

         visibility.PinShow("Orders");
         Assert.True(visibility.PinState("Orders"));
         visibility.PinHide("Orders");
         Assert.False(visibility.PinState("Orders"));
         visibility.ClearPin("Orders");
         Assert.Null(visibility.PinState("Orders"));
      }

      [Fact]
      public void FocusShowsOnlyTheSelectedGroupsPlusUngrouped()
      {
         var visibility = new EntityVisibility(new[] { "Core", "Audit", "Hr" });
         visibility.SetFocus(new[] { "Hr" });

         Assert.False(visibility.IsVisible(Table("Orders", "Core")));
         Assert.True(visibility.IsVisible(Table("Employee", "Hr")));
         Assert.True(visibility.IsVisible(Table("Lookup"))); // ungrouped
         Assert.False(visibility.IsShowAll);
      }

      [Fact]
      public void ShowAllRestoresEverything()
      {
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         visibility.SetGroupVisible("Core", false);
         visibility.SetGroupVisible("Audit", false);
         Assert.False(visibility.IsVisible(Table("Orders", "Core")));

         visibility.ShowAll();

         Assert.True(visibility.IsVisible(Table("Orders", "Core")));
         Assert.True(visibility.IsVisible(Table("AuditLog", "Audit")));
         Assert.True(visibility.IsShowAll);
      }

      [Fact]
      public void PinsSurviveShowAll()
      {
         var visibility = new EntityVisibility(new[] { "Core" });
         visibility.PinHide("Orders");
         visibility.ShowAll();

         Assert.False(visibility.IsVisible(Table("Orders", "Core")));
      }

      [Fact]
      public void IsGroupVisibleReflectsToggles()
      {
         var visibility = new EntityVisibility(new[] { "Core", "Audit" });
         Assert.True(visibility.IsGroupVisible("Core"));
         Assert.True(visibility.IsGroupVisible("Audit"));

         visibility.SetGroupVisible("Audit", false);
         Assert.True(visibility.IsGroupVisible("Core"));
         Assert.False(visibility.IsGroupVisible("Audit"));
      }

      [Fact]
      public void CreateDerivesGroupsFromTags()
      {
         var visibility = EntityVisibility.Create(new[]
         {
            Table("Orders", "Core"),
            Table("AuditLog", "Audit"),
            Table("Lookup")
         });

         Assert.Equal(2, visibility.Groups.Count);
         Assert.Contains("Core", visibility.Groups);
         Assert.Contains("Audit", visibility.Groups);
      }

      [Fact]
      public void NullTableIsNotVisible()
      {
         var visibility = EntityVisibility.Create(new[] { Table("Orders", "Core") });
         Assert.False(visibility.IsVisible(null));
      }

      [Fact]
      public void EmptyGroupsConstructionShowsUngroupedTables()
      {
         var visibility = new EntityVisibility(null);
         Assert.True(visibility.IsVisible(Table("Lookup")));
         Assert.True(visibility.IsShowAll);
      }

      // ------------------------------------------------------------------
      // Backlog 043 — the theme-aware visibility: the group universe and the
      // membership test derive from the active grouping theme, so hiding a
      // schema/kind/connectivity group hides its members exactly as hiding a
      // tag group does. The tag theme is the default and behaves identically
      // to the pre-043 code (the tests above pin that).
      // ------------------------------------------------------------------

      private static TableInfo SchemaTable(string name, string schema = null)
      {
         return new TableInfo { TableName = name, SchemaName = schema };
      }

      [Fact]
      public void CreateWithSchemaThemeDerivesTheUniverseFromSchemas()
      {
         var visibility = EntityVisibility.Create(
            new[]
            {
               SchemaTable("Orders", "Sales"),
               SchemaTable("StockItem", "Inventory"),
               SchemaTable("Lookup") // no schema — ungrouped
            },
            GroupingThemes.Schema);

         Assert.Equal(2, visibility.Groups.Count);
         Assert.Contains("Sales", visibility.Groups);
         Assert.Contains("Inventory", visibility.Groups);
         Assert.True(visibility.IsShowAll);
      }

      [Fact]
      public void HideSchemaGroupHidesItsMembers()
      {
         var visibility = EntityVisibility.Create(
            new[]
            {
               SchemaTable("Orders", "Sales"),
               SchemaTable("StockItem", "Inventory"),
               SchemaTable("Lookup")
            },
            GroupingThemes.Schema);
         visibility.SetGroupVisible("Sales", false);

         Assert.False(visibility.IsVisible(SchemaTable("Orders", "Sales")));
         Assert.True(visibility.IsVisible(SchemaTable("StockItem", "Inventory")));
         Assert.True(visibility.IsVisible(SchemaTable("Lookup"))); // ungrouped
      }

      [Fact]
      public void TagThemeIsTheDefaultAndMatchesPre043Behavior()
      {
         // Create(tables) with no theme must behave exactly like the tag
         // theme — the pre-043 default.
         var tables = new[]
         {
            Table("Orders", "Core"),
            Table("Lookup")
         };
         var defaultVisibility = EntityVisibility.Create(tables);
         var tagVisibility = EntityVisibility.Create(tables, GroupingThemes.Tags);

         Assert.Equal(defaultVisibility.Groups, tagVisibility.Groups);
         Assert.Equal(
            defaultVisibility.IsVisible(Table("Orders", "Core")),
            tagVisibility.IsVisible(Table("Orders", "Core")));
      }
   }

}
