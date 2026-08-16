namespace ModelConsole.Graph
{

   /// <summary>
   /// A resolved foreign-key edge between a child column and the parent
   /// column it references.
   /// </summary>
   public sealed record FkRelation(
      string ChildTable,
      string ChildColumn,
      string ParentTable,
      string ParentColumn);

}
