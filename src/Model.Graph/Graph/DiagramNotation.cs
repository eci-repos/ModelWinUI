namespace ModelConsole.Graph
{

   /// <summary>
   /// The notation used to present the same canonical model. The model stays
   /// ERD-native; UML is a derived view/export (backlog 040).
   /// </summary>
   public enum DiagramNotation
   {
      /// <summary>Entity-relationship notation, the default app view.</summary>
      Erd,

      /// <summary>
      /// Entity-relationship notation with Crow's Foot endpoint cardinality
      /// markers derived from FK cardinality.
      /// </summary>
      ErdCrowFoot,

      /// <summary>UML class/package notation over the same model.</summary>
      Uml
   }

}
