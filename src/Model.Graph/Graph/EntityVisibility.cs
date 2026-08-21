using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// View-side visibility state for an ERD (backlog 038): which groups
   /// (table tags) draw, plus per-table pins that override the group rules.
   /// The composition rule (<see cref="IsVisible"/>) is the single
   /// deterministic source both renderers agree on:
   /// <para>draw entity E iff NOT pinned-hide AND (pinned-show OR belongs
   /// to ≥ 1 visible group OR belongs to no group).</para>
   /// This is view state, not model state — it is never persisted (saved
   /// view profiles are a future layer). The model's tag universe is
   /// captured at construction and every group starts visible, so a fresh
   /// visibility behaves exactly like the pre-038 renderer (everything
   /// draws).
   /// </summary>
   public sealed class EntityVisibility
   {
      private readonly HashSet<string> _groups;
      private readonly Dictionary<string, bool> _pins = new Dictionary<string, bool>();
      private readonly GroupingTheme _theme;
      private HashSet<string> _visibleGroups;

      /// <summary>
      /// Create the visibility for a model's group universe.
      /// </summary>
      /// <param name="groups">every group the model mentions</param>
      /// <param name="theme">the grouping theme the groups come from (defaults
      /// to the tag theme, backlog 037)</param>
      public EntityVisibility(IEnumerable<string> groups, GroupingTheme theme = null)
      {
         _groups = new HashSet<string>(
            groups ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
         _visibleGroups = new HashSet<string>(_groups, StringComparer.Ordinal);
         _theme = theme ?? GroupingThemes.Tags;
      }

      /// <summary>
      /// Build a show-everything visibility from a model (the default; a
      /// fresh model starts with all groups visible and no pins).
      /// </summary>
      /// <param name="tables">the model's tables</param>
      /// <param name="theme">the grouping theme the group universe derives
      /// from (defaults to the tag theme — the pre-043 behavior)</param>
      public static EntityVisibility Create(
         IReadOnlyList<TableInfo> tables, GroupingTheme theme = null)
      {
         theme = theme ?? GroupingThemes.Tags;
         return new EntityVisibility(theme.Groups(tables), theme);
      }

      /// <summary>
      /// The tag universe — every group the model mentions. The explorer's
      /// Groups section lists this.
      /// </summary>
      public IReadOnlyCollection<string> Groups => _groups;

      /// <summary>
      /// True when every known group is visible (the untouched / "Show all"
      /// state). Pins do not affect this — a pinned-hide table is still
      /// hidden even when its group is visible.
      /// </summary>
      public bool IsShowAll => _visibleGroups.Count == _groups.Count;

      /// <summary>
      /// Whether a group's members currently draw.
      /// </summary>
      public bool IsGroupVisible(string group)
      {
         return _visibleGroups.Contains(group);
      }

      /// <summary>
      /// The composition rule (backlog 038): draw entity E iff NOT pinned-hide
      /// AND (pinned-show OR belongs to ≥ 1 visible group OR belongs to no
      /// group). A table in no group is always visible unless pinned-hide.
      /// Membership is theme-aware (backlog 043): the theme's
      /// <see cref="GroupingTheme.GroupsOf"/> replaces the direct tag read, so
      /// hiding a schema/kind/connectivity group hides its members exactly as
      /// hiding a tag group does.
      /// </summary>
      public bool IsVisible(TableInfo table)
      {
         if (table == null)
         {
            return false;
         }
         if (_pins.TryGetValue(table.TableName, out bool pinned))
         {
            // pinned=true → pinned-show; pinned=false → pinned-hide.
            return pinned;
         }
         var groups = _theme.GroupsOf(table);
         if (!groups.Any())
         {
            return true; // belongs to no group
         }
         foreach (var group in groups)
         {
            if (_visibleGroups.Contains(group))
            {
               return true; // belongs to ≥ 1 visible group
            }
         }
         return false;
      }

      /// <summary>Pin a table visible — its group's state no longer hides it.</summary>
      public void PinShow(string tableName)
      {
         _pins[tableName] = true;
      }

      /// <summary>Pin a table hidden — its group's state no longer shows it.</summary>
      public void PinHide(string tableName)
      {
         _pins[tableName] = false;
      }

      /// <summary>Drop a pin; the table returns to its group's visibility.</summary>
      public void ClearPin(string tableName)
      {
         _pins.Remove(tableName);
      }

      /// <summary>
      /// The table's pin state: true = pinned-show, false = pinned-hide,
      /// null = unpinned (group rules apply).
      /// </summary>
      public bool? PinState(string tableName)
      {
         return _pins.TryGetValue(tableName, out bool v) ? (bool?)v : null;
      }

      /// <summary>Show or hide a group's members.</summary>
      public void SetGroupVisible(string group, bool visible)
      {
         if (visible)
         {
            _visibleGroups.Add(group);
         }
         else
         {
            _visibleGroups.Remove(group);
         }
      }

      /// <summary>
      /// Focus mode: only these groups' members draw (plus ungrouped and
      /// pinned-show entities). The selected set replaces the current one.
      /// </summary>
      public void SetFocus(IEnumerable<string> groups)
      {
         _visibleGroups = new HashSet<string>(
            groups ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
      }

      /// <summary>Reset to show-everything: every group visible, pins kept.</summary>
      public void ShowAll()
      {
         _visibleGroups = new HashSet<string>(_groups, StringComparer.Ordinal);
      }
   }

}
