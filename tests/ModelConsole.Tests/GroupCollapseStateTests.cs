using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Backlog 039 — the view-side collapse state: which groups are currently
   /// collapsed into a package-style box. A fresh state has everything
   /// expanded, so the pre-039 renderer behavior is the default.
   /// </summary>
   public class GroupCollapseStateTests
   {

      [Fact]
      public void FreshStateHasEveryGroupExpanded()
      {
         var state = new GroupCollapseState();

         Assert.True(state.IsAllExpanded);
         Assert.False(state.IsCollapsed("Core"));
         Assert.False(state.IsCollapsed("Audit"));
         Assert.Empty(state.CollapsedGroups);
      }

      [Fact]
      public void SetCollapsedMarksTheGroup()
      {
         var state = new GroupCollapseState();

         state.SetCollapsed("Core", true);

         Assert.True(state.IsCollapsed("Core"));
         Assert.False(state.IsCollapsed("Audit"));
         Assert.Contains("Core", state.CollapsedGroups);
         Assert.False(state.IsAllExpanded);
      }

      [Fact]
      public void ExpandingRestoresTheGroup()
      {
         var state = new GroupCollapseState();
         state.SetCollapsed("Core", true);

         state.SetCollapsed("Core", false);

         Assert.False(state.IsCollapsed("Core"));
         Assert.True(state.IsAllExpanded);
      }

      [Fact]
      public void ExpandAllResetsEveryGroup()
      {
         var state = new GroupCollapseState();
         state.SetCollapsed("Core", true);
         state.SetCollapsed("Audit", true);

         state.ExpandAll();

         Assert.True(state.IsAllExpanded);
         Assert.Empty(state.CollapsedGroups);
      }

      [Fact]
      public void NullOrBlankGroupIsIgnored()
      {
         var state = new GroupCollapseState();

         state.SetCollapsed(null, true);
         state.SetCollapsed("", true);

         Assert.True(state.IsAllExpanded);
      }

      [Fact]
      public void CollapseIsPerGroup()
      {
         var state = new GroupCollapseState();

         state.SetCollapsed("Core", true);

         // A second group stays independent; expanding Core does not touch it.
         state.SetCollapsed("Audit", true);
         state.SetCollapsed("Core", false);
         Assert.False(state.IsCollapsed("Core"));
         Assert.True(state.IsCollapsed("Audit"));
      }
   }

}
