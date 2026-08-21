using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// The canonical node kinds (backlog 028): the object-oriented drawing's
   /// vocabulary, mapping 1:1 to the canonical model — Entity = Table,
   /// Element = Column, Dependency = FK, Group = Schema (the 023 container
   /// concept).
   /// </summary>
   public enum GraphNodeKind
   {
      Entity,
      Element,
      Dependency,
      Group
   }

   /// <summary>
   /// The portable node surface every drawable exposes (backlog 028): a
   /// typed identity (kind + name), the live canonical object it renders,
   /// a hover-summary provider, and the edit verbs it supports. Hover,
   /// click-select, and the inspector all read the same node — never a
   /// frozen projection (the 022 discipline).
   /// </summary>
   public interface IGraphNode
   {
      GraphNodeKind Kind { get; }
      string Name { get; }
      object Model { get; }
      IReadOnlyList<string> Summary();
      NodeVerbs Verbs { get; }
   }

   /// <summary>
   /// The edit verbs a node kind supports. Backlog 028 defines the surface
   /// (what can be edited); backlog 029 wires the operations. An immutable
   /// descriptor — a node declares its edit surface, never how it edits.
   /// </summary>
   public sealed class NodeVerbs
   {
      public bool CanRename { get; }
      public bool CanAddColumn { get; }
      public bool CanRemoveColumn { get; }
      public bool CanEditType { get; }
      public bool CanEditKey { get; }
      public bool CanEditDescription { get; }
      public bool CanEditTags { get; }
      public bool CanEditMetadata { get; }
      public bool CanAddForeignKey { get; }
      public bool CanEditTarget { get; }
      public bool CanEditCardinality { get; }
      public bool CanEditRoles { get; }
      public bool CanDelete { get; }

      /// <summary>
      /// Whether the node can be hidden/shown on the drawing (backlog 038) —
      /// a per-entity pin that overrides its group's visibility.
      /// </summary>
      public bool CanToggleVisibility { get; }

      private NodeVerbs(
         bool canRename = false, bool canAddColumn = false,
         bool canRemoveColumn = false, bool canEditType = false,
         bool canEditKey = false, bool canEditDescription = false,
         bool canEditTags = false, bool canEditMetadata = false,
         bool canAddForeignKey = false,
         bool canEditTarget = false, bool canEditCardinality = false,
         bool canEditRoles = false, bool canDelete = false,
         bool canToggleVisibility = false)
      {
         CanRename = canRename;
         CanAddColumn = canAddColumn;
         CanRemoveColumn = canRemoveColumn;
         CanEditType = canEditType;
         CanEditKey = canEditKey;
         CanEditDescription = canEditDescription;
         CanEditTags = canEditTags;
         CanEditMetadata = canEditMetadata;
         CanAddForeignKey = canAddForeignKey;
         CanEditTarget = canEditTarget;
         CanEditCardinality = canEditCardinality;
         CanEditRoles = canEditRoles;
         CanDelete = canDelete;
         CanToggleVisibility = canToggleVisibility;
      }

      /// <summary>An entity: rename, add/remove columns, edit key/description/tags, hide/show, delete.</summary>
      public static NodeVerbs Entity { get; } = new NodeVerbs(
         canRename: true, canAddColumn: true, canRemoveColumn: true,
         canEditKey: true, canEditDescription: true, canEditTags: true,
         canEditMetadata: true, canDelete: true,
         canToggleVisibility: true);

      /// <summary>An element (column): rename, edit type/key/description, add an FK.</summary>
      public static NodeVerbs Element { get; } = new NodeVerbs(
         canRename: true, canEditType: true, canEditKey: true,
         canEditDescription: true, canEditMetadata: true,
         canAddForeignKey: true);

      /// <summary>A dependency (FK): edit target/cardinality/roles, delete.</summary>
      public static NodeVerbs Dependency { get; } = new NodeVerbs(
         canEditTarget: true, canEditCardinality: true,
         canEditRoles: true, canDelete: true);

      /// <summary>A group (schema): no edit surface yet.</summary>
      public static NodeVerbs Group { get; } = new NodeVerbs();
   }

   /// <summary>
   /// Base of the concrete node types (backlog 028).
   /// </summary>
   public abstract class GraphNode : IGraphNode
   {
      public abstract GraphNodeKind Kind { get; }
      public abstract string Name { get; }
      public abstract object Model { get; }
      public abstract IReadOnlyList<string> Summary();
      public abstract NodeVerbs Verbs { get; }
   }

   /// <summary>
   /// The node a table renders: a typed identity over the live
   /// <see cref="TableInfo"/> (backlog 028).
   /// </summary>
   public sealed class EntityNode : GraphNode
   {
      public EntityNode(TableInfo table) { Table = table; }

      /// <summary>The live canonical object this node renders.</summary>
      public TableInfo Table { get; }

      public override GraphNodeKind Kind => GraphNodeKind.Entity;
      public override string Name => Table.SchemaName + "::" + Table.TableName;
      public override object Model => Table;
      public override IReadOnlyList<string> Summary() => HoverSummary.ForTable(Table);
      public override NodeVerbs Verbs => NodeVerbs.Entity;
   }

   /// <summary>
   /// The node a column renders: a typed identity over the live
   /// <see cref="ColumnInfo"/>, with its owning table for context
   /// (backlog 028).
   /// </summary>
   public sealed class ElementNode : GraphNode
   {
      public ElementNode(TableInfo table, ColumnInfo column)
      {
         Table = table;
         Column = column;
      }

      /// <summary>The owning table (context for the column's identity).</summary>
      public TableInfo Table { get; }

      /// <summary>The live canonical object this node renders.</summary>
      public ColumnInfo Column { get; }

      public override GraphNodeKind Kind => GraphNodeKind.Element;
      public override string Name => Table.TableName + "." + Column.ColumnName;
      public override object Model => Column;
      public override IReadOnlyList<string> Summary() =>
         HoverSummary.ForColumn(Column, Table.TableName);
      public override NodeVerbs Verbs => NodeVerbs.Element;
   }

   /// <summary>
   /// The node a connector renders: a typed identity over the live
   /// <see cref="FkRelation"/> edge (whose <see cref="Constraint"/> is the
   /// canonical dependency object) (backlog 028).
   /// </summary>
   public sealed class DependencyNode : GraphNode
   {
      public DependencyNode(FkRelation edge) { Edge = edge; }

      /// <summary>The live edge this node renders.</summary>
      public FkRelation Edge { get; }

      /// <summary>The canonical dependency object behind the edge, when the
      /// edge was extracted from one.</summary>
      public ConstraintInfo Constraint => Edge.Constraint;

      public override GraphNodeKind Kind => GraphNodeKind.Dependency;
      public override string Name => Edge.ChildTable + "." + Edge.ChildColumn +
         "  →  " + Edge.ParentTable + "." + Edge.ParentColumn;
      public override object Model => Edge;
      public override IReadOnlyList<string> Summary() => HoverSummary.ForConnector(Edge);
      public override NodeVerbs Verbs => NodeVerbs.Dependency;
   }

   /// <summary>
   /// The node a schema group renders: a typed identity over a schema name
   /// and its live tables (the 023 container concept as a first-class
   /// object) (backlog 028).
   /// </summary>
   public sealed class GroupNode : GraphNode
   {
      public GroupNode(string schemaName, IReadOnlyList<TableInfo> tables)
      {
         SchemaName = schemaName;
         Tables = tables;
      }

      /// <summary>The group's identity (the schema name).</summary>
      public string SchemaName { get; }

      /// <summary>The live tables in the group.</summary>
      public IReadOnlyList<TableInfo> Tables { get; }

      public override GraphNodeKind Kind => GraphNodeKind.Group;
      public override string Name => SchemaName;
      public override object Model => Tables;
      public override IReadOnlyList<string> Summary() =>
         HoverSummary.ForGroup(SchemaName, Tables);
      public override NodeVerbs Verbs => NodeVerbs.Group;
   }

   /// <summary>
   /// Factory for the portable node types (backlog 028). Each creator
   /// returns null for a null input — the host treats null as "nothing
   /// hovered / nothing clicked".
   /// </summary>
   public static class GraphNodes
   {
      public static IGraphNode Entity(TableInfo table)
      {
         return table == null ? null : new EntityNode(table);
      }

      public static IGraphNode Element(TableInfo table, ColumnInfo column)
      {
         return table == null || column == null
            ? null : new ElementNode(table, column);
      }

      public static IGraphNode Dependency(FkRelation edge)
      {
         return edge == null ? null : new DependencyNode(edge);
      }

      public static IGraphNode Group(
         string schemaName, IReadOnlyList<TableInfo> tables)
      {
         return tables == null ? null : new GroupNode(schemaName, tables);
      }
   }

}
