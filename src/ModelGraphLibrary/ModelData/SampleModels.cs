using System;
using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.ModelData
{

   /// <summary>
   /// A shipped sample model: a display name, a one-line description, the
   /// JSON file it ships as, and the code fixture it is generated from.
   /// </summary>
   public sealed class SampleModel
   {
      /// <summary>Display name shown in the File → Open Sample menu.</summary>
      public string Name;

      /// <summary>One-line description of the sample.</summary>
      public string Description;

      /// <summary>Name of the shipped JSON file (under the Samples folder).</summary>
      public string FileName;

      /// <summary>The code fixture the JSON is generated from.</summary>
      public IReadOnlyList<TableInfo> Tables;
   }

   /// <summary>
   /// The shipped sample models (backlog 005). Single source of truth for
   /// the File → Open Sample menu (app) and the sample-model tests. The JSON
   /// files under <c>ModelGraphLibrary/Samples/</c> are generated from these
   /// fixtures via <see cref="ModelFile.ToJson"/> and kept in sync by
   /// <c>SampleModelTests.ShippedJsonMatchesFixture</c>.
   /// </summary>
   public static class SampleModels
   {
      /// <summary>
      /// The shipped samples, in menu order.
      /// </summary>
      public static IReadOnlyList<SampleModel> All { get; } = new[]
      {
         new SampleModel
         {
            Name = "Public Safety",
            Description = "50-table criminal-justice schema (74 FKs)",
            FileName = "PublicSafety.json",
            Tables = PublicSafetySchema.Tables
         },
         new SampleModel
         {
            Name = "Library",
            Description = "20-table library / books schema (30 FKs)",
            FileName = "Library.json",
            Tables = LibrarySchema.Tables
         }
      };
   }

}
