using System.Linq;

using ModelConsole.Geometry;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// Unit tests for draw-only rounded connector path commands.
   /// </summary>
   public class RoundedPolylineTests
   {

      [Fact]
      public void StraightPolylineStaysStraight()
      {
         var commands = RoundedPolyline.Build(new[]
         {
            new Point2(0, 10),
            new Point2(100, 10)
         }, 8);

         Assert.Equal(new[]
         {
            RoundedPathCommandType.MoveTo,
            RoundedPathCommandType.LineTo
         }, commands.Select(c => c.Type).ToArray());
         Assert.Equal(new Point2(100, 10), commands[1].Point);
      }

      [Fact]
      public void OrthogonalBendEmitsQuadraticCorner()
      {
         var commands = RoundedPolyline.Build(new[]
         {
            new Point2(0, 0),
            new Point2(100, 0),
            new Point2(100, 100)
         }, 8);

         Assert.Equal(new[]
         {
            RoundedPathCommandType.MoveTo,
            RoundedPathCommandType.LineTo,
            RoundedPathCommandType.QuadraticTo,
            RoundedPathCommandType.LineTo
         }, commands.Select(c => c.Type).ToArray());
         Assert.Equal(new Point2(92, 0), commands[1].Point);
         Assert.Equal(new Point2(100, 0), commands[2].ControlPoint);
         Assert.Equal(new Point2(100, 8), commands[2].Point);
         Assert.Equal(new Point2(100, 100), commands[3].Point);
      }

      [Fact]
      public void RadiusClampsToShortSegments()
      {
         var commands = RoundedPolyline.Build(new[]
         {
            new Point2(0, 0),
            new Point2(10, 0),
            new Point2(10, 10)
         }, 8);

         Assert.Equal(new Point2(5, 0), commands[1].Point);
         Assert.Equal(new Point2(10, 5), commands[2].Point);
      }

      [Fact]
      public void SourcePointsAreNotChanged()
      {
         var points = new[]
         {
            new Point2(0, 0),
            new Point2(100, 0),
            new Point2(100, 100)
         };

         _ = RoundedPolyline.Build(points, 8);

         Assert.Equal(new Point2(0, 0), points[0]);
         Assert.Equal(new Point2(100, 0), points[1]);
         Assert.Equal(new Point2(100, 100), points[2]);
      }

   }

}
