using Model.Data;
using Model.Test;

namespace ModelConsole.Services
{
   /// <summary>
   /// Sample data provider backed by the static fixtures in
   /// <see cref="Data_Table_Entity"/>. This is the DI surface over the
   /// fixtures; the static class itself is left untouched.
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
   }
}
