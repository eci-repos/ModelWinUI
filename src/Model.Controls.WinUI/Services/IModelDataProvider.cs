using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.Controls.Services
{
   /// <summary>
   /// Provides the sample model data used to exercise the graphics stacks.
   /// </summary>
   public interface IModelDataProvider
   {
      /// <summary>
      /// The 50-table public-safety / criminal-justice sample schema.
      /// </summary>
      IReadOnlyList<TableInfo> GetPublicSafetyTables();
   }
}
