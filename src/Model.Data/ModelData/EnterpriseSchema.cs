using System;
using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.ModelData
{

   /// <summary>
   /// Sample multi-schema enterprise schema used to exercise the grouping
   /// themes (backlog 043). Exactly 27 tables and 31 FK edges across three
   /// schemas — <c>Sales</c> (core domain), <c>Inventory</c>, and
   /// <c>Finance</c> — with cross-schema FKs (Inventory and Finance reference
   /// Sales), so the schema theme groups the model with zero authoring and
   /// the package overview shows inter-schema dependency edges. A different
   /// domain from <see cref="PublicSafetySchema"/> and
   /// <see cref="LibrarySchema"/>; the shipped samples show the tool's
   /// breadth. No tags (the tag theme shows "Groups (0)"), and some
   /// <c>Ref*</c> reference-code tables so the kind theme splits entity vs
   /// reference-code. Table names are globally unique (the FK extractor
   /// requires it).
   /// </summary>
   public static class EnterpriseSchema
   {

      /// <summary>
      /// The full schema, in a deterministic order (grouped by schema).
      /// </summary>
      public static TableInfo[] Tables { get; } = BuildTables();

      private static TableInfo[] BuildTables()
      {
         var tables = new List<TableInfo>();

         // -- Sales (12): the core domain --------------------------------
         tables.Add(T("Sales", "RefOrderStatus",
            C("OrderStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Sales", "RefPaymentMethod",
            C("PaymentMethodCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Sales", "RefShipMethod",
            C("ShipMethodCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Sales", "RefProductStatus",
            C("ProductStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Sales", "ProductCategory",
            C("ProductCategoryID", 20, key: true),
            C("CategoryName", 80),
            C("ParentCategoryID", 20, fk: "ProductCategory")));

         tables.Add(T("Sales", "Product",
            C("ProductID", 20, key: true),
            C("ProductName", 80),
            C("ProductCategoryID", 20, fk: "ProductCategory"),
            C("ProductStatusCode", 20, fk: "RefProductStatus"),
            C("UnitPrice", 20)));

         tables.Add(T("Sales", "Customer",
            C("CustomerID", 20, key: true),
            C("NameGiven", 40),
            C("NameSurname", 40),
            C("Email", 80),
            C("Phone", 40)));

         tables.Add(T("Sales", "Salesperson",
            C("SalespersonID", 20, key: true),
            C("NameGiven", 40),
            C("NameSurname", 40),
            C("TerritoryCode", 20)));

         tables.Add(T("Sales", "SalesOrder",
            C("SalesOrderID", 20, key: true),
            C("CustomerID", 20, fk: "Customer"),
            C("SalespersonID", 20, fk: "Salesperson"),
            C("OrderDate", type: "DATETIMEOFFSET"),
            C("OrderStatusCode", 20, fk: "RefOrderStatus"),
            C("PaymentMethodCode", 20, fk: "RefPaymentMethod"),
            C("ShipMethodCode", 20, fk: "RefShipMethod")));

         tables.Add(T("Sales", "SalesOrderLine",
            C("SalesOrderLineID", 20, key: true),
            C("SalesOrderID", 20, fk: "SalesOrder"),
            C("ProductID", 20, fk: "Product"),
            C("Quantity", 20),
            C("UnitPrice", 20),
            C("ProductStatusCode", 20, fk: "RefProductStatus")));

         tables.Add(T("Sales", "Payment",
            C("PaymentID", 20, key: true),
            C("SalesOrderID", 20, fk: "SalesOrder"),
            C("PaymentMethodCode", 20, fk: "RefPaymentMethod"),
            C("Amount", 20),
            C("PaymentDate", type: "DATETIMEOFFSET")));

         tables.Add(T("Sales", "Shipment",
            C("ShipmentID", 20, key: true),
            C("SalesOrderID", 20, fk: "SalesOrder"),
            C("ShipMethodCode", 20, fk: "RefShipMethod"),
            C("ShipDate", type: "DATETIMEOFFSET"),
            C("TrackingNumber", 40)));

         // -- Inventory (8): references Sales ----------------------------
         tables.Add(T("Inventory", "RefStockStatus",
            C("StockStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Inventory", "RefMovementType",
            C("MovementTypeCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Inventory", "RefPurchaseStatus",
            C("PurchaseStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Inventory", "Warehouse",
            C("WarehouseID", 20, key: true),
            C("WarehouseName", 80),
            C("AddressLine", 80),
            C("City", 60)));

         tables.Add(T("Inventory", "Supplier",
            C("SupplierID", 20, key: true),
            C("SupplierName", 80),
            C("ContactEmail", 80),
            C("Phone", 40)));

         tables.Add(T("Inventory", "PurchaseOrder",
            C("PurchaseOrderID", 20, key: true),
            C("SupplierID", 20, fk: "Supplier"),
            C("WarehouseID", 20, fk: "Warehouse"),
            C("OrderDate", type: "DATETIMEOFFSET"),
            C("PurchaseStatusCode", 20, fk: "RefPurchaseStatus")));

         tables.Add(T("Inventory", "StockItem",
            C("StockItemID", 20, key: true),
            C("ProductID", 20, fk: "Product"), // cross-schema → Sales
            C("WarehouseID", 20, fk: "Warehouse"),
            C("QuantityOnHand", 20),
            C("StockStatusCode", 20, fk: "RefStockStatus")));

         tables.Add(T("Inventory", "StockMovement",
            C("StockMovementID", 20, key: true),
            C("StockItemID", 20, fk: "StockItem"),
            C("WarehouseID", 20, fk: "Warehouse"),
            C("MovementTypeCode", 20, fk: "RefMovementType"),
            C("Quantity", 20),
            C("MovementDate", type: "DATETIMEOFFSET")));

         // -- Finance (7): references Sales ------------------------------
         tables.Add(T("Finance", "RefInvoiceStatus",
            C("InvoiceStatusCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Finance", "RefTaxCode",
            C("TaxCode", 20, key: true),
            C("Description", 60),
            C("Rate", 20)));

         tables.Add(T("Finance", "RefAccountType",
            C("AccountTypeCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("Finance", "Account",
            C("AccountID", 20, key: true),
            C("AccountName", 80),
            C("AccountTypeCode", 20, fk: "RefAccountType"),
            C("OpeningBalance", 20)));

         tables.Add(T("Finance", "Invoice",
            C("InvoiceID", 20, key: true),
            C("CustomerID", 20, fk: "Customer"), // cross-schema → Sales
            C("InvoiceDate", type: "DATETIMEOFFSET"),
            C("InvoiceStatusCode", 20, fk: "RefInvoiceStatus")));

         tables.Add(T("Finance", "InvoiceLine",
            C("InvoiceLineID", 20, key: true),
            C("InvoiceID", 20, fk: "Invoice"),
            C("ProductID", 20, fk: "Product"), // cross-schema → Sales
            C("Quantity", 20),
            C("UnitPrice", 20),
            C("TaxCode", 20, fk: "RefTaxCode")));

         tables.Add(T("Finance", "LedgerEntry",
            C("LedgerEntryID", 20, key: true),
            C("AccountID", 20, fk: "Account"),
            C("InvoiceID", 20, fk: "Invoice"),
            C("Amount", 20),
            C("EntryDate", type: "DATETIMEOFFSET")));

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

      private static TableInfo T(string schema, string tableName, params Col[] cols)
      {
         var columns = new ColumnList();
         foreach (var col in cols)
         {
            ColumnInfo column = columns.Add(
               schema, col.Name, col.Size, col.Key, col.Type);

            if (col.Key)
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = schema,
                  TableName = tableName,
                  ColumnName = col.Name,
                  Type = DataInfo.PRIMARY_KEY
               });
            }

            if (!String.IsNullOrWhiteSpace(col.RefTable))
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = schema,
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
            SchemaName = schema,
            TableName = tableName,
            Columns = columns
         };
      }

      #endregion

   }

}
