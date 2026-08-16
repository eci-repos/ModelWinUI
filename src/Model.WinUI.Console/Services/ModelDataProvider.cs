using System.Collections.Generic;

using Model.Data;
using Model.Test;
using ModelConsole.ModelData;

namespace ModelConsole.Services
{
   /// <summary>
   /// Sample data provider backed by the static fixtures in
   /// <see cref="Data_Table_Entity"/> and <see cref="PublicSafetySchema"/>.
   /// This is the DI surface over the fixtures; the static classes themselves
   /// are left untouched.
   /// </summary>
   public class ModelDataProvider : IModelDataProvider
   {
      /// <summary>
      /// Sample Person table.
      /// </summary>
      public TableInfo GetPersonTable()
      {
         return Data_Table_Entity.GetPersonTable();
      }

      /// <summary>
      /// Sample Person Name table.
      /// </summary>
      public TableInfo GetPersonNameTable()
      {
         return Data_Table_Entity.GetPersonNameTable();
      }

      /// <summary>
      /// The 50-table public-safety / criminal-justice sample schema.
      /// </summary>
      public IReadOnlyList<TableInfo> GetPublicSafetyTables()
      {
         return PublicSafetySchema.Tables;
      }
   }
}
