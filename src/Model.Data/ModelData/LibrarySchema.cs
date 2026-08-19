using System;
using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.ModelData
{

   /// <summary>
   /// Sample library / books schema used to exercise the renderer and the
   /// routing tests. Exactly 20 tables and 30 FK edges. A different domain
   /// from <see cref="PublicSafetySchema"/> so the shipped samples show the
   /// tool's breadth. Domain areas: Reference data, Geography, Catalog
   /// (books &amp; authors), Circulation (patrons, loans, holds, fines),
   /// Operations (branches &amp; staff).
   /// </summary>
   public static class LibrarySchema
   {

      private const string SCHEMA = "Library";

      /// <summary>
      /// The full schema, in a deterministic order (grouped by domain area).
      /// </summary>
      public static TableInfo[] Tables { get; } = BuildTables();

      private static TableInfo[] BuildTables()
      {
         var tables = new List<TableInfo>();

         // -- Reference data (7) ------------------------------------------
         tables.Add(T("RefBookStatus",
            C("BookStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefLoanStatus",
            C("LoanStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefPatronType",
            C("PatronTypeCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefFineType",
            C("FineTypeCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefGenre",
            C("GenreCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefLanguage",
            C("LanguageCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefBranchType",
            C("BranchTypeCode", 20, key: true),
            C("Description", 60)));

         // -- Geography (1) ------------------------------------------------
         tables.Add(T("Address",
            C("AddressID", 20, key: true),
            C("Street1", 80),
            C("Street2", 80),
            C("City", 60),
            C("StateCode", 20),
            C("PostalCode", 20),
            C("CountryCode", 20)));

         // -- Catalog (5) --------------------------------------------------
         tables.Add(T("Publisher",
            C("PublisherID", 20, key: true),
            C("PublisherName", 80),
            C("AddressID", 20, fk: "Address"),
            C("Phone", 40)));

         tables.Add(T("Author",
            C("AuthorID", 20, key: true),
            C("NameGiven", 40),
            C("NameSurname", 40),
            C("BirthDate", type: "DATETIMEOFFSET"),
            C("NationalityCode", 20)));

         tables.Add(T("Book",
            C("BookID", 20, key: true),
            C("Title", 128),
            C("Subtitle", 128),
            C("ISBN", 20),
            C("PublisherID", 20, fk: "Publisher"),
            C("GenreCode", 20, fk: "RefGenre"),
            C("LanguageCode", 20, fk: "RefLanguage"),
            C("PublicationYear", 20),
            C("PageCount", 20),
            C("BookStatusCode", 20, fk: "RefBookStatus")));

         tables.Add(T("BookAuthor",
            C("BookAuthorID", 20, key: true),
            C("BookID", 20, fk: "Book"),
            C("AuthorID", 20, fk: "Author"),
            C("AuthorOrder", 20)));

         tables.Add(T("BookCopy",
            C("CopyID", 20, key: true),
            C("BookID", 20, fk: "Book"),
            C("BranchID", 20, fk: "LibraryBranch"),
            C("AcquisitionDate", type: "DATETIMEOFFSET"),
            C("CopyStatusCode", 20, fk: "RefBookStatus"),
            C("Price", 20)));

         // -- Circulation (5) ----------------------------------------------
         tables.Add(T("Patron",
            C("PatronID", 20, key: true),
            C("NameGiven", 40),
            C("NameSurname", 40),
            C("PatronTypeCode", 20, fk: "RefPatronType"),
            C("AddressID", 20, fk: "Address"),
            C("Email", 80),
            C("Phone", 40),
            C("MembershipDate", type: "DATETIMEOFFSET")));

         tables.Add(T("Loan",
            C("LoanID", 20, key: true),
            C("CopyID", 20, fk: "BookCopy"),
            C("PatronID", 20, fk: "Patron"),
            C("BranchID", 20, fk: "LibraryBranch"),
            C("LoanDate", type: "DATETIMEOFFSET"),
            C("DueDate", type: "DATETIMEOFFSET"),
            C("ReturnDate", type: "DATETIMEOFFSET"),
            C("LoanStatusCode", 20, fk: "RefLoanStatus")));

         tables.Add(T("Hold",
            C("HoldID", 20, key: true),
            C("BookID", 20, fk: "Book"),
            C("PatronID", 20, fk: "Patron"),
            C("BranchID", 20, fk: "LibraryBranch"),
            C("HoldDate", type: "DATETIMEOFFSET"),
            C("ExpiryDate", type: "DATETIMEOFFSET"),
            C("HoldStatusCode", 20, fk: "RefBookStatus")));

         tables.Add(T("Fine",
            C("FineID", 20, key: true),
            C("LoanID", 20, fk: "Loan"),
            C("PatronID", 20, fk: "Patron"),
            C("FineTypeCode", 20, fk: "RefFineType"),
            C("Amount", 20),
            C("AssessedDate", type: "DATETIMEOFFSET"),
            C("PaidDate", type: "DATETIMEOFFSET")));

         tables.Add(T("Reservation",
            C("ReservationID", 20, key: true),
            C("BookID", 20, fk: "Book"),
            C("PatronID", 20, fk: "Patron"),
            C("BranchID", 20, fk: "LibraryBranch"),
            C("ReservationDate", type: "DATETIMEOFFSET"),
            C("PickupDate", type: "DATETIMEOFFSET"),
            C("ReservationStatusCode", 20, fk: "RefBookStatus")));

         // -- Operations (2) ----------------------------------------------
         tables.Add(T("LibraryBranch",
            C("BranchID", 20, key: true),
            C("BranchName", 80),
            C("AddressID", 20, fk: "Address"),
            C("BranchTypeCode", 20, fk: "RefBranchType"),
            C("Phone", 40)));

         tables.Add(T("Staff",
            C("StaffID", 20, key: true),
            C("BranchID", 20, fk: "LibraryBranch"),
            C("NameGiven", 40),
            C("NameSurname", 40),
            C("RoleCode", 20),
            C("HireDate", type: "DATETIMEOFFSET")));

         return tables.ToArray();
      }

      #region -- Builder helpers

      private sealed class Col
      {
         public string Name;
         public int Size = 20;
         public bool Key;
         public string RefTable;
         public string RefColumn;
         public string Type;
      }

      private static Col C(
         string name, int size = 20, bool key = false,
         string fk = null, string fkColumn = null, string type = null)
      {
         return new Col
         {
            Name = name,
            Size = size,
            Key = key,
            RefTable = fk,
            RefColumn = fkColumn,
            Type = type
         };
      }

      private static TableInfo T(string tableName, params Col[] cols)
      {
         var columns = new ColumnList();
         foreach (var col in cols)
         {
            ColumnInfo column = columns.Add(
               SCHEMA, col.Name, col.Size, col.Key, col.Type);

            if (col.Key)
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = SCHEMA,
                  TableName = tableName,
                  ColumnName = col.Name,
                  Type = DataInfo.PRIMARY_KEY
               });
            }

            if (!String.IsNullOrWhiteSpace(col.RefTable))
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = SCHEMA,
                  TableName = tableName,
                  ColumnName = col.Name,
                  Type = DataInfo.FOREIGN_KEY,
                  ReferencedTableName = col.RefTable,
                  ReferencedColumnName = col.RefColumn
               });
            }
         }

         return new TableInfo
         {
            SchemaName = SCHEMA,
            TableName = tableName,
            Columns = columns
         };
      }

      #endregion

   }

}
