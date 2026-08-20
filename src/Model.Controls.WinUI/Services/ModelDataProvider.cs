using System.Collections.Generic;

using Model.Data;
using ModelConsole.ModelData;

namespace ModelConsole.Controls.Services
{
   /// <summary>
   /// Sample data provider backed by the <see cref="PublicSafetySchema"/>
   /// fixture. This is the DI surface over the fixture; the static class
   /// itself is left untouched.
   /// </summary>
   public class ModelDataProvider : IModelDataProvider
   {
      /// <summary>
      /// The 50-table public-safety / criminal-justice sample schema.
      /// </summary>
      public IReadOnlyList<TableInfo> GetPublicSafetyTables()
      {
         return PublicSafetySchema.Tables;
      }
   }
}
