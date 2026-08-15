using Model.Data;

namespace ModelConsole.Services
{
   /// <summary>
   /// Provides the sample model data used to exercise the graphics stacks.
   /// </summary>
   public interface IModelDataProvider
   {
      /// <summary>
      /// Sample Person table.
      /// </summary>
      TableInfo GetPersonTable();

      /// <summary>
      /// Sample Person Name table.
      /// </summary>
      TableInfo GetPersonNameTable();
   }
}
