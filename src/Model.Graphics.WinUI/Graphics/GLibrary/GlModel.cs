using System.Collections.Generic;

namespace ModelConsole.Graphics.GLibrary
{

   /// <summary>
   /// Model collection holding the drawable objects of the XAML graphics
   /// stack.
   /// </summary>
   public class GlModel : IGlModel
   {
      public List<GlObject> Items { get; set; } = new List<GlObject>();

      /// <summary>
      /// Add an object to the model.
      /// </summary>
      /// <param name="instance">object to add</param>
      /// <returns>the added object is returned, else null if not added</returns>
      public GlObject Add(GlObject instance)
      {
         if (instance == null)
         {
            return null;
         }

         Items.Add(instance);
         return instance;
      }
   }

}
