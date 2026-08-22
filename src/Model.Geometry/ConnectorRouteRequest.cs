namespace ModelConsole.Geometry
{

   /// <summary>
   /// A connector route request that preserves the table edge each endpoint
   /// belongs to. The router uses the sides to keep the first and final
   /// segments perpendicular to the table borders.
   /// </summary>
   public readonly struct ConnectorRouteRequest
   {
      /// <summary>Exact child-side anchor point.</summary>
      public Point2 Start { get; }

      /// <summary>Table side the route leaves from.</summary>
      public AnchorSide StartSide { get; }

      /// <summary>Exact parent-side anchor point.</summary>
      public Point2 End { get; }

      /// <summary>Table side the route enters at.</summary>
      public AnchorSide EndSide { get; }

      /// <summary>
      /// Create a side-aware connector route request.
      /// </summary>
      public ConnectorRouteRequest(
         Point2 start, AnchorSide startSide, Point2 end, AnchorSide endSide)
      {
         Start = start;
         StartSide = startSide;
         End = end;
         EndSide = endSide;
      }
   }

}
