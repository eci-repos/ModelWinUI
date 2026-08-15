using Model.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Test
{

   public class Data_Table_Entity
   {
      public const string ENTITY = "Entity";
      public const string PERSON = "Person";
      public const string PERSON_NAME = "PersonName";

      /// <summary>
      /// Sample Person Table.
      /// </summary>
      /// <returns>instance ot TableInfo is returned</returns>
      public static TableInfo GetPersonTable()
      {
         string schemaName = ENTITY;

         ColumnList columns = new ColumnList();

         var p = columns.Add(schemaName, "PersonID", 20);
         p.IsKey = true;

         columns.Add(schemaName, "SexCode", 20);
         columns.Add(schemaName, "EthnicityCode", 20);
         columns.Add(schemaName, "RaceCode", 20);
         columns.Add(schemaName, "BirthDate", type: "DATETIMEOFFSET");
         columns.Add(schemaName, "BirthLocationID", 20);
         columns.Add(schemaName, "DeathDate", type: "DATETIMEOFFSET");
         columns.Add(schemaName, "NationalityCode", 20);
         columns.Add(schemaName, "IdentificationNumber", 40);
         columns.Add(schemaName, "IdentificationNumberStateCode", 20);
         columns.Add(schemaName, "DriverLicenseNumber", 40);
         columns.Add(schemaName, "DriverLicenseStateCode", 20);
         columns.Add(schemaName, "PassportNumber", 20);
         columns.Add(schemaName, "SocialSecurityID", 20);

         TableInfo table1 = new TableInfo();
         table1.Columns = columns;
         table1.SchemaName = ENTITY;
         table1.TableName = PERSON;

         return table1;
      }

      /// <summary>
      /// Test JSON file read and write to a file.
      /// </summary>
      /// <param name="table">table information</param>
      public static void TestTableJsonFile(TableInfo table)
      {
         // write JSON TableInfo to a file
         string path = "c:/temp/jsonSample.json";
         table.ToJsonFile(path);

         // read JSON TableInfo from a file
         var table_object = TableInfo.FromJsonFile(path);
      }

      /// <summary>
      /// Sample Person Name Table.
      /// </summary>
      /// <returns>instance ot TableInfo is returned</returns>
      public static TableInfo GetPersonNameTable()
      {
         string schemaName = ENTITY;

         ColumnList columns = new ColumnList();

         var n = columns.Add(schemaName, "PersonNameID", 20);
         n.IsKey = true;
         n.Constraints.Add(new ConstraintInfo()
         {
            SchemaName = schemaName,
            Type = DataInfo.PRIMARY_KEY
         });

         var fkColumnName = "PersonID";
         var p = columns.Add(schemaName, fkColumnName, 20);
         p.IsForeignKey = true;
         n.Constraints.Add(new ConstraintInfo()
         {
            SchemaName = schemaName,
            TableName = PERSON_NAME,
            ColumnName = fkColumnName,
            Type = DataInfo.FOREIGN_KEY
         });

         columns.Add(schemaName, "NameTypeID", 20);
         columns.Add(schemaName, "NameGiven", 40);
         columns.Add(schemaName, "NameMiddle", 40);
         columns.Add(schemaName, "NameSurname", 40);
         columns.Add(schemaName, "NamePrefix", 20);
         columns.Add(schemaName, "NameSuffix", 20);
         columns.Add(schemaName, "NameFull", 128);

         TableInfo table1 = new TableInfo();
         table1.Columns = columns;
         table1.SchemaName = ENTITY;
         table1.TableName = PERSON_NAME;

         return table1;
      }

   }

}
