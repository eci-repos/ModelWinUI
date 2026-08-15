using System.Collections.Generic;

namespace ModelConsole.Graphics.GLibrary
{
   /// <summary>
   /// Model collection holding the drawable objects of the XAML graphics
   /// stack.
   /// </summary>
   public interface IGlModel
   {
      /// <summary>
      /// Items in the model.
      /// </summary>
      List<GlObject> Items { get; }

      /// <summary>
      /// Add an object to the model.
      /// </summary>
      /// <param name="instance">object to add</param>
      /// <returns>the added object is returned, else null if not added</returns>
      GlObject Add(GlObject instance);
   }
}
