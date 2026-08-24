using Model.Data;
using ModelConsole.Graph;

using Xunit;

namespace ModelConsole.Tests
{

   public class CrowFootNotationTests
   {
      [Theory]
      [InlineData(0, 1, true, true, false)]
      [InlineData(1, 1, false, true, false)]
      [InlineData(0, null, true, false, true)]
      [InlineData(1, null, false, true, true)]
      [InlineData(1, 5, false, true, true)]
      public void FromBoundsMapsCommonCrowFootCombinations(
         int? min, int? max, bool optional, bool one, bool many)
      {
         var marker = CrowFootNotation.FromBounds(min, max);

         Assert.Equal(optional, marker.Optional);
         Assert.Equal(one, marker.One);
         Assert.Equal(many, marker.Many);
      }

      [Fact]
      public void ForEdgeUsesChildCardinalityAndRequiredParent()
      {
         var edge = new FkRelation(
            "Order", "CustomerId", "Customer", "Id",
            new ConstraintInfo
            {
               Type = DataInfo.FOREIGN_KEY,
               MinCardinality = 0,
               MaxCardinality = null
            });

         var markers = CrowFootNotation.ForEdge(edge);

         Assert.True(markers.ChildMarker.Optional);
         Assert.False(markers.ChildMarker.One);
         Assert.True(markers.ChildMarker.Many);
         Assert.False(markers.ParentMarker.Optional);
         Assert.True(markers.ParentMarker.One);
         Assert.False(markers.ParentMarker.Many);
      }

      [Fact]
      public void ForEdgeWithoutCardinalityFallsBackToSimpleConnector()
      {
         var edge = new FkRelation(
            "Order", "CustomerId", "Customer", "Id",
            new ConstraintInfo { Type = DataInfo.FOREIGN_KEY });

         Assert.True(CrowFootNotation.ForEdge(edge).IsNone);
         Assert.True(CrowFootNotation.ForEdge(null).IsNone);
      }
   }

}
