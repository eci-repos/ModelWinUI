using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// A resolved foreign-key edge between a child column and the parent
   /// column it references. Carries the source <see cref="ConstraintInfo"/>
   /// (when the edge was extracted from one) so the readout can surface the
   /// dependency's cardinality and role names (backlog 022).
   /// </summary>
   public sealed record FkRelation(
      string ChildTable,
      string ChildColumn,
      string ParentTable,
      string ParentColumn,
      ConstraintInfo Constraint = null);

}
