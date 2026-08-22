using System.Collections.Generic;

namespace ModelConsole.Geometry
{

   /// <summary>Which end of a connector route contains a near-straight nudge.</summary>
   public enum RouteNudgeTerminal
   {
      Start,
      End
   }

   /// <summary>Why a suspicious connector nudge can or cannot be straightened.</summary>
   public enum RouteNudgeDisposition
   {
      Removable,
      RequiredByEndpointAlignment,
      BlockedByObstacleOrConnector
   }

   /// <summary>
   /// Diagnostic record for a small terminal nudge in a final routed
   /// connector polyline. These records are produced from the post-cleanup
   /// route points so visual issues can be tied back to the exact geometry
   /// the renderers draw.
   /// </summary>
   public sealed class RouteNudgeDiagnostic
   {
      public RouteNudgeDiagnostic(
         RouteNudgeTerminal terminal,
         RouteNudgeDisposition disposition,
         double offset,
         IReadOnlyList<Point2> routePoints,
         IReadOnlyList<Point2> candidatePoints,
         string reason)
      {
         Terminal = terminal;
         Disposition = disposition;
         Offset = offset;
         RoutePoints = routePoints ?? new Point2[0];
         CandidatePoints = candidatePoints ?? new Point2[0];
         Reason = reason ?? "";
      }

      /// <summary>The route end where the nudge was found.</summary>
      public RouteNudgeTerminal Terminal { get; }

      /// <summary>Whether the nudge is removable or must be retained.</summary>
      public RouteNudgeDisposition Disposition { get; }

      /// <summary>Magnitude of the small lateral offset in pixels.</summary>
      public double Offset { get; }

      /// <summary>The final route points that were inspected.</summary>
      public IReadOnlyList<Point2> RoutePoints { get; }

      /// <summary>
      /// Candidate straightened route points when a safe replacement exists.
      /// Empty when the nudge is intentionally retained.
      /// </summary>
      public IReadOnlyList<Point2> CandidatePoints { get; }

      /// <summary>Human-readable classification note for diagnostic output.</summary>
      public string Reason { get; }
   }

}
