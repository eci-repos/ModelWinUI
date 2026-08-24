using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Draw-only Crow's Foot endpoint marker semantics for an FK connector.
   /// The canonical model remains the existing ERD data; this adapter reads
   /// <see cref="ConstraintInfo.MinCardinality"/> /
   /// <see cref="ConstraintInfo.MaxCardinality"/> at render time.
   /// </summary>
   public static class CrowFootNotation
   {
      /// <summary>
      /// Marker set for the connector's child/start and parent/end endpoints.
      /// </summary>
      public readonly struct ConnectorMarkers
      {
         public ConnectorMarkers(
            CardinalityMarker childMarker, CardinalityMarker parentMarker)
         {
            ChildMarker = childMarker;
            ParentMarker = parentMarker;
         }

         /// <summary>Marker drawn at the child/start endpoint.</summary>
         public CardinalityMarker ChildMarker { get; }

         /// <summary>Marker drawn at the parent/end endpoint.</summary>
         public CardinalityMarker ParentMarker { get; }

         /// <summary>True when no Crow's Foot marker should be drawn.</summary>
         public bool IsNone
         {
            get { return ChildMarker.IsNone && ParentMarker.IsNone; }
         }
      }

      /// <summary>
      /// Endpoint marker parts. Combinations map to the usual symbols:
      /// optional circle, required bar, and many-prong crow foot.
      /// </summary>
      public readonly struct CardinalityMarker
      {
         public CardinalityMarker(bool optional, bool one, bool many)
         {
            Optional = optional;
            One = one;
            Many = many;
         }

         public bool Optional { get; }

         public bool One { get; }

         public bool Many { get; }

         public bool IsNone
         {
            get { return !Optional && !One && !Many; }
         }

         public static CardinalityMarker None
         {
            get { return new CardinalityMarker(false, false, false); }
         }

         public static CardinalityMarker RequiredOne
         {
            get { return new CardinalityMarker(false, true, false); }
         }
      }

      /// <summary>
      /// Return endpoint markers for a resolved FK edge. Missing cardinality
      /// falls back to the simple ERD connector style.
      /// </summary>
      public static ConnectorMarkers ForEdge(FkRelation edge)
      {
         ConstraintInfo constraint = edge?.Constraint;
         if (constraint == null ||
             (constraint.MinCardinality == null &&
              constraint.MaxCardinality == null))
         {
            return new ConnectorMarkers(
               CardinalityMarker.None, CardinalityMarker.None);
         }

         return new ConnectorMarkers(
            FromBounds(constraint.MinCardinality, constraint.MaxCardinality),
            CardinalityMarker.RequiredOne);
      }

      /// <summary>
      /// Map model cardinality bounds to one endpoint's Crow's Foot marker.
      /// </summary>
      public static CardinalityMarker FromBounds(int? min, int? max)
      {
         if (min == null && max == null)
         {
            return CardinalityMarker.None;
         }

         int lower = min ?? 0;
         bool optional = lower == 0;
         bool many = max == null || max.Value > 1;
         bool one = lower >= 1 || (!many && max.GetValueOrDefault(1) >= 1);

         return new CardinalityMarker(optional, one, many);
      }
   }

}
