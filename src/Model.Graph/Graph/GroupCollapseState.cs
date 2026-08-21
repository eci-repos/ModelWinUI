using System;
using System.Collections.Generic;

namespace ModelConsole.Graph
{

   /// <summary>
   /// View-side collapse state for the tagged groups (backlog 039): which
   /// groups are currently collapsed into a single package-style box. The
   /// three states per group compose with the 038 visibility — a group is
   /// <b>hidden</b> when not visible, <b>collapsed</b> when visible and in
   /// this set, and <b>expanded</b> when visible and not in it.
   /// <para>This is view state, not model state — never persisted (saved view
   /// profiles are a future layer). A fresh collapse state has every group
   /// expanded, so it behaves exactly like the pre-039 renderer.</para>
   /// </summary>
   public sealed class GroupCollapseState
   {
      private readonly HashSet<string> _collapsed =
         new HashSet<string>(StringComparer.Ordinal);

      /// <summary>The groups currently collapsed into a box.</summary>
      public IReadOnlyCollection<string> CollapsedGroups => _collapsed;

      /// <summary>
      /// True when every known group is expanded (the untouched / reset
      /// state) — no collapsed box draws.
      /// </summary>
      public bool IsAllExpanded => _collapsed.Count == 0;

      /// <summary>Whether a group's members are collapsed into a box.</summary>
      public bool IsCollapsed(string group)
      {
         return !string.IsNullOrEmpty(group) && _collapsed.Contains(group);
      }

      /// <summary>Collapse (box) or expand (members) a group.</summary>
      public void SetCollapsed(string group, bool collapsed)
      {
         if (string.IsNullOrEmpty(group))
         {
            return;
         }
         if (collapsed)
         {
            _collapsed.Add(group);
         }
         else
         {
            _collapsed.Remove(group);
         }
      }

      /// <summary>Expand every group (the "show all members" reset).</summary>
      public void ExpandAll()
      {
         _collapsed.Clear();
      }
   }

}
