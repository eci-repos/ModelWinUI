using System;
using System.Collections.Generic;
using System.Linq;

using Model.Data;
using Model.Interpretation;

using Xunit;

namespace ModelConsole.Tests
{

   /// <summary>
   /// The schema-driven interpretation core (backlog 019): interpreting an
   /// arbitrary JSON document through a mapping spec to the canonical model.
   /// </summary>
   public class SchemaInterpreterTests
   {

      // ---- Profile: the existing ModelFile array format -------------------

      [Fact]
      public void ArrayProfileReproducesModelFileLoad()
      {
         var fixture = BuildArrayFixture();
         var json = ModelFile.ToJson(fixture);

         var interpretation = SchemaInterpreter.Interpret(json, BuiltInProfiles.Array);
         var loaded = ModelFile.LoadJson(json);

         Assert.Empty(interpretation.Issues);
         Assert.Equal(loaded.Count, interpretation.Tables.Count);
         foreach (var expected in loaded)
         {
            var actual = interpretation.Tables.First(t => t.TableName == expected.TableName);
            AssertModelEquivalent(expected, actual);
         }
      }

      // -------- Profile: the grouped $.entities shape (object keyed) ------

      [Fact]
      public void GroupedProfileObjectKeyedEntities()
      {
         const string json = """
            {
               "entities": {
                  "Author": {
                     "Elements": [
                        { "name": "Id", "type": "int" },
                        { "name": "Name", "type": "string" }
                     ]
                  },
                  "Book": {
                     "Elements": [
                        { "name": "Id", "type": "int", "primaryKey": true },
                        { "name": "Title", "type": "string" },
                        { "name": "AuthorId", "type": "int", "Depends On": "Author" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);
         Assert.Equal(2, result.Tables.Count);

         // Author.Id is keyed by convention (identity name "Id").
         var author = result.Tables.First(t => t.TableName == "Author");
         Assert.True(author.Columns.Single(c => c.ColumnName == "Id").IsKey);
         Assert.Equal("INT", author.Columns.Single(c => c.ColumnName == "Id").Type);

         var book = result.Tables.First(t => t.TableName == "Book");
         Assert.True(book.Columns.Single(c => c.ColumnName == "Id").IsKey);
         Assert.Equal("VARCHAR", book.Columns.Single(c => c.ColumnName == "Title").Type);

         var fk = book.Columns.Single(c => c.ColumnName == "AuthorId")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("Author", fk.ReferencedTableName);
         Assert.Null(fk.ReferencedColumnName); // resolve to the parent's identity
      }

      // -------- Containerized grouped form (backlog 023) --------------------

      [Fact]
      public void GroupedContainerizedSchemasInterpretToTables()
      {
         const string json = """
            {
               "repository": "Clinic",
               "schemas": {
                  "clinic": {
                     "entities": {
                        "Patient": {
                           "Elements": [
                              { "name": "id", "type": "int" },
                              { "name": "name", "type": "string" }
                           ]
                        },
                        "Visit": {
                           "Elements": [
                              { "name": "id", "type": "int", "primaryKey": true },
                              { "name": "patient", "type": "int", "Depends On": "Patient" }
                           ]
                        }
                     }
                  },
                  "audit": {
                     "entities": {
                        "AuditLog": {
                           "Elements": [ { "name": "id", "type": "int", "primaryKey": true } ]
                        }
                     }
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);
         Assert.NotNull(result.Catalog);
         Assert.Equal("Clinic", result.Catalog.CatalogName);

         Assert.Equal(3, result.Tables.Count);
         Assert.All(result.Tables.Where(t => t.TableName is "Patient" or "Visit"),
            t => Assert.Equal("clinic", t.SchemaName));
         Assert.Equal("audit", result.Tables.Single(t => t.TableName == "AuditLog").SchemaName);

         var visit = result.Tables.Single(t => t.TableName == "Visit");
         Assert.True(visit.Columns.Single(c => c.ColumnName == "patient").IsForeignKey);
      }

      [Fact]
      public void ArrayContainerizedFormInterpretsToTables()
      {
         const string json = """
            {
               "dataSource": "Northwind",
               "schemas": [
                  {
                     "name": "dbo",
                     "tables": [
                        {
                           "TableName": "Parent",
                           "Columns": [ { "ColumnName": "ID", "Type": "int", "IsKey": true, "Constraints": [ { "Type": "PK" } ] } ]
                        },
                        {
                           "TableName": "Child",
                           "Columns": [ { "ColumnName": "ParentID", "Type": "int", "IsForeignKey": true, "Constraints": [ { "Type": "FK", "ReferencedTableName": "Parent" } ] } ]
                        }
                     ]
                  }
               ]
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Array);

         Assert.Empty(result.Issues);
         Assert.NotNull(result.Catalog);
         Assert.Equal("Northwind", result.Catalog.CatalogName);

         Assert.Equal(2, result.Tables.Count);
         Assert.All(result.Tables, t => Assert.Equal("dbo", t.SchemaName));

         var child = result.Tables.Single(t => t.TableName == "Child");
         Assert.True(child.Columns.Single(c => c.ColumnName == "ParentID").IsForeignKey);
      }

      // -------- Profile: the grouped $.entities shape (array + bare elements)

      [Fact]
      public void GroupedProfileArrayEntitiesAndBareElements()
      {
         const string json = """
            {
               "entities": [
                  { "name": "Author", "Elements": [ "id", "name" ] },
                  {
                     "name": "Book",
                     "Elements": [
                        { "name": "id", "type": "int", "primaryKey": true },
                        { "name": "Title", "type": "string" },
                        { "name": "AuthorId", "type": "int", "Depends On": "Author" }
                     ]
                  }
               ]
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);

         // A bare string element is its own name; "id" keys by convention.
         var author = result.Tables.First(t => t.TableName == "Author");
         Assert.Equal(2, author.Columns.Count);
         Assert.True(author.Columns.Single(c => c.ColumnName == "id").IsKey);
         Assert.Equal("VARCHAR", author.Columns.Single(c => c.ColumnName == "name").Type);

         var book = result.Tables.First(t => t.TableName == "Book");
         Assert.Equal("INT", book.Columns.Single(c => c.ColumnName == "id").Type);
         Assert.True(book.Columns.Single(c => c.ColumnName == "AuthorId").IsForeignKey);
      }

      // -------- Term-synonym vocabulary (Entity / Elements / "Depends On") --

      [Fact]
      public void SynonymVocabularyMapsToCanonicalModel()
      {
         const string specJson = """
            {
               "specVersion": 1,
               "entities": { "path": "$.models", "kind": "array", "nameField": "title", "elementsField": "Elements" },
               "elements": {
                  "nameField": "elementName",
                  "typeField": "elementType",
                  "keyField": "isKey",
                  "refField": "Depends On",
                  "refColumnField": "DependsOnColumn"
               },
               "conventions": { "identityName": null, "referenceSuffix": null, "referenceByValue": false }
            }
            """;

         const string json = """
            {
               "models": [
                  {
                     "title": "Vendor",
                     "Elements": [
                        { "elementName": "vendorCode", "elementType": "string", "isKey": true }
                     ]
                  },
                  {
                     "title": "Product",
                     "Elements": [
                        { "elementName": "productCode", "elementType": "string", "isKey": true },
                        { "elementName": "vendor", "elementType": "string", "Depends On": "Vendor", "DependsOnColumn": "vendorCode" }
                     ]
                  }
               ]
            }
            """;

         var spec = MappingSpec.Parse(specJson);
         var result = SchemaInterpreter.Interpret(json, spec);

         Assert.Empty(result.Issues);

         var vendor = result.Tables.First(t => t.TableName == "Vendor");
         Assert.True(vendor.Columns.Single(c => c.ColumnName == "vendorCode").IsKey);

         var product = result.Tables.First(t => t.TableName == "Product");
         var dependency = product.Columns.Single(c => c.ColumnName == "vendor")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("Vendor", dependency.ReferencedTableName);
         Assert.Equal("vendorCode", dependency.ReferencedColumnName);
      }

      // -------- 024: descriptions on entities and elements -----------------

      [Fact]
      public void EntityAndElementDescriptionsAreCaptured()
      {
         const string json = """
            {
               "entities": {
                  "Patient": {
                     "description": "A person receiving care at the clinic.",
                     "Elements": [
                        { "name": "id", "type": "int", "primaryKey": true },
                        { "name": "name", "type": "string", "description": "Full legal name." }
                     ]
                  },
                  "Provider": {
                     "Elements": [ { "name": "id", "type": "int", "primaryKey": true } ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);
         var patient = result.Tables.First(t => t.TableName == "Patient");
         Assert.Equal("A person receiving care at the clinic.", patient.Description);
         Assert.Equal(
            "Full legal name.",
            patient.Columns.Single(c => c.ColumnName == "name").Description);
         Assert.Null(patient.Columns.Single(c => c.ColumnName == "id").Description);

         // An entity without a description stays silent — never invented.
         Assert.Null(result.Tables.First(t => t.TableName == "Provider").Description);
      }

      [Fact]
      public void ArrayContainerCapturesDescriptionsFromPascalCaseFields()
      {
         const string json = """
            {
               "dataSource": "Clinic",
               "schemas": [
                  {
                     "name": "clinic",
                     "tables": [
                        {
                           "TableName": "Patient",
                           "Description": "A person receiving care at the clinic.",
                           "Columns": [
                              { "ColumnName": "ID", "Type": "int", "IsKey": true,
                                "Constraints": [ { "Type": "PK" } ] },
                              { "ColumnName": "name", "Type": "string",
                                "Description": "Full legal name." }
                           ]
                        }
                     ]
                  }
               ]
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Array);

         Assert.Empty(result.Issues);
         var patient = result.Tables.Single();
         Assert.Equal("A person receiving care at the clinic.", patient.Description);
         Assert.Equal(
            "Full legal name.",
            patient.Columns.Single(c => c.ColumnName == "name").Description);
      }

      // -------- 026: per-node provenance (entity → element) ----------------

      [Fact]
      public void EntityAndElementProvenanceIsCaptured()
      {
         const string json = """
            {
               "entities": {
                  "Patient": {
                     "provenance": { "source": "patient-registry.json", "version": "1.1" },
                     "Elements": [
                        { "name": "id", "type": "int", "primaryKey": true },
                        { "name": "name", "type": "string",
                          "provenance": { "source": "demographics-transform.json", "version": "2.0" } }
                     ]
                  },
                  "Provider": {
                     "Elements": [ { "name": "id", "type": "int", "primaryKey": true } ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);
         var patient = result.Tables.First(t => t.TableName == "Patient");
         Assert.NotNull(patient.Provenance);
         Assert.Equal("patient-registry.json", patient.Provenance.Source);
         Assert.Equal("1.1", patient.Provenance.Version);

         var name = patient.Columns.Single(c => c.ColumnName == "name");
         Assert.NotNull(name.Provenance);
         Assert.Equal("demographics-transform.json", name.Provenance.Source);
         Assert.Equal("2.0", name.Provenance.Version);
         Assert.Null(patient.Columns.Single(c => c.ColumnName == "id").Provenance);

         // An entity without provenance stays silent — never invented.
         Assert.Null(result.Tables.First(t => t.TableName == "Provider").Provenance);
      }

      [Fact]
      public void MalformedNodeProvenanceIsAnIssueNotASilentDrop()
      {
         const string json = """
            {
               "entities": {
                  "Order": {
                     "provenance": "not-an-object",
                     "Elements": [
                        { "name": "id", "type": "int", "primaryKey": true,
                          "provenance": "also-bad" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         var order = result.Tables.Single();
         Assert.Null(order.Provenance);
         Assert.Null(order.Columns.Single().Provenance);
         Assert.Contains(result.Issues, issue => issue.Contains("provenance is not an object"));
      }

      // -------- R7: declared beats inferred --------------------------------

      [Fact]
      public void DeclaredReferenceBeatsInferredByValue()
      {
         const string specJson = """
            {
               "specVersion": 1,
               "entities": { "path": "$.entities" },
               "elements": { "refField": "ref" },
               "conventions": { "identityName": null, "referenceSuffix": null, "referenceByValue": true }
            }
            """;

         const string json = """
            {
               "entities": {
                  "Customer": { "elements": [ { "name": "id", "primaryKey": true } ] },
                  "Supplier": { "elements": [ { "name": "id", "primaryKey": true } ] },
                  "Order": {
                     "elements": [
                        { "name": "id", "primaryKey": true },
                        { "name": "customer", "ref": "Customer", "value": "Supplier" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, MappingSpec.Parse(specJson));

         // The declared "ref" wins; the by-value read is suppressed (R7).
         Assert.Empty(result.Issues);
         var order = result.Tables.First(t => t.TableName == "Order");
         var dependency = order.Columns.Single(c => c.ColumnName == "customer")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("Customer", dependency.ReferencedTableName);
      }

      // -------- R8: ambiguity is an issue, never a silent guess ------------

      [Fact]
      public void AmbiguousValueYieldsIssueNotSilentGuess()
      {
         const string specJson = """
            {
               "specVersion": 1,
               "entities": { "path": "$.entities" },
               "conventions": { "identityName": null, "referenceSuffix": null, "referenceByValue": true }
            }
            """;

         const string json = """
            {
               "entities": {
                  "Shipper": { "elements": [ { "name": "id", "primaryKey": true } ] },
                  "Shipment": {
                     "elements": [
                        { "name": "id", "primaryKey": true },
                        { "name": "carrier", "value": "Shipper" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, MappingSpec.Parse(specJson));

         // Resolved (R4) but reported (R8) — the read is visible, not silent.
         var shipment = result.Tables.First(t => t.TableName == "Shipment");
         var dependency = shipment.Columns.Single(c => c.ColumnName == "carrier")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal("Shipper", dependency.ReferencedTableName);
         Assert.Contains(result.Issues, issue => issue.Contains("value 'Shipper'"));
         Assert.Contains(result.Issues, issue => issue.Contains("disambiguate"));
      }

      // -------- R5: cardinality/optionality and roles ----------------------

      [Fact]
      public void CardinalityAndRolesAreCapturedOnTheDependency()
      {
         const string json = """
            {
               "entities": {
                  "Author": { "Elements": [ { "name": "id", "primaryKey": true } ] },
                  "Book": {
                     "Elements": [
                        { "name": "id", "primaryKey": true },
                        { "name": "authorId", "Depends On": "Author",
                           "cardinality": "1:N", "childRole": "writes", "parentRole": "writtenBy" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);
         var book = result.Tables.First(t => t.TableName == "Book");
         var fk = book.Columns.Single(c => c.ColumnName == "authorId")
            .Constraints.Single(c => c.IsForeignKey);
         Assert.Equal(1, fk.MinCardinality);
         Assert.Null(fk.MaxCardinality);
         Assert.Equal("writes", fk.ChildRole);
         Assert.Equal("writtenBy", fk.ParentRole);
      }

      // -------- R6: metadata / provenance / enumerations -------------------

      [Fact]
      public void EnumerationsProvenanceAndMetadataAreCaptured()
      {
         // A spec declares where the extras live. The built-in grouped profile
         // declares the same root paths; a document without a section stays
         // quiet (missing optional sections are silent, not issues).
         const string specJson = """
            {
               "specVersion": 1,
               "entities": { "path": "$.entities" },
               "elements": { "enumField": "enum" },
               "enumerationsPath": "$.enumerations",
               "provenancePath": "$.provenance",
               "metadataPath": "$.metadata"
            }
            """;

         const string json = """
            {
               "provenance": { "source": "orders.json", "version": "1.2", "loadedAt": "2026-08-18", "notes": "fixture" },
               "metadata": { "domain": "orders", "owner": "billing" },
               "enumerations": {
                  "OrderStatus": [ "PENDING", { "code": "SHIPPED", "label": "Shipped" } ]
               },
               "entities": {
                  "Order": {
                     "metadata": { "audit": "true" },
                     "elements": [
                        { "name": "id", "primaryKey": true },
                        { "name": "status", "type": "string", "enum": "OrderStatus" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, MappingSpec.Parse(specJson));

         Assert.Empty(result.Issues);
         Assert.NotNull(result.Provenance);
         Assert.Equal("orders.json", result.Provenance.Source);
         Assert.Equal("1.2", result.Provenance.Version);
         Assert.Equal("billing", result.Metadata["owner"]);

         Assert.True(result.Enumerations.ContainsKey("OrderStatus"));
         var status = result.Enumerations["OrderStatus"];
         Assert.Equal(2, status.Values.Count);
         Assert.Equal("PENDING", status.Values[0].Code);
         Assert.Equal("SHIPPED", status.Values[1].Code);
         Assert.Equal("Shipped", status.Values[1].Label);

         var order = result.Tables.Single();
         Assert.Equal("OrderStatus", order.Columns.Single(c => c.ColumnName == "status").EnumerationName);
         Assert.Equal("true", order.Metadata["audit"]);
      }

      // -------- Data-driven type map (gear 4) ------------------------------

      [Fact]
      public void TypeMapIsAppliedFromData()
      {
         const string specJson = """
            {
               "specVersion": 1,
               "entities": { "path": "$.entities" },
               "typeMap": { "string": "TEXT", "int": "NUMERIC" },
               "conventions": { "identityName": null, "referenceSuffix": null }
            }
            """;

         const string json = """
            {
               "entities": {
                  "Item": {
                     "elements": [
                        { "name": "sku", "type": "string", "primaryKey": true },
                        { "name": "count", "type": "int" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, MappingSpec.Parse(specJson));

         Assert.Empty(result.Issues);
         var item = result.Tables.Single();
         Assert.Equal("TEXT", item.Columns.Single(c => c.ColumnName == "sku").Type);
         Assert.Equal("NUMERIC", item.Columns.Single(c => c.ColumnName == "count").Type);
      }

      // -------- Extension bag (gear 3: preserve unmodeled data) ------------

      [Fact]
      public void NamedExtensionFieldsArePreservedVerbatim()
      {
         const string specJson = """
            {
               "specVersion": 1,
               "entities": { "path": "$.entities", "extensionFields": [ "domainArea" ] },
               "elements": { "extensionFields": ["sourceSystem"] },
               "conventions": { "identityName": null, "referenceSuffix": null }
            }
            """;

         const string json = """
            {
               "entities": {
                  "Case": {
                     "domainArea": "courts",
                     "elements": [
                        { "name": "caseId", "primaryKey": true, "sourceSystem": "legacy" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, MappingSpec.Parse(specJson));

         Assert.Empty(result.Issues);
         var table = result.Tables.Single();
         Assert.Equal("courts", table.Extensions["domainArea"]);
         Assert.Equal("legacy", table.Columns.Single().Extensions["sourceSystem"]);
      }

      // -------- Tags (backlog 037) -----------------------------------------

      [Fact]
      public void GroupedProfileCapturesTags()
      {
         const string json = """
            {
               "entities": {
                  "Incident": {
                     "tags": [ "Core", "uml", "audit" ],
                     "Elements": [
                        { "name": "Id", "type": "int" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Issues);
         Assert.Equal(new[] { "Core", "uml", "audit" }, result.Tables.Single().Tags);
      }

      [Fact]
      public void ArrayProfileCapturesTagsFromPascalCaseField()
      {
         const string json = """
            [
               {
                  "TableName": "Incident",
                  "Tags": [ "Core", "uml" ],
                  "Columns": [
                     { "ColumnName": "Id", "Type": "int", "IsKey": true }
                  ]
               }
            ]
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Array);

         Assert.Empty(result.Issues);
         Assert.Equal(new[] { "Core", "uml" }, result.Tables.Single().Tags);
      }

      [Fact]
      public void MalformedTagsFieldIsAnIssueNotASilentDrop()
      {
         const string json = """
            {
               "entities": {
                  "Incident": {
                     "tags": "not-an-array",
                     "Elements": [ { "name": "Id", "type": "int" } ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Contains(result.Issues, i => i.Contains("tags is not an array"));
         Assert.Null(result.Tables.Single().Tags);
      }

      [Fact]
      public void NonStringTagItemIsAnIssue()
      {
         const string json = """
            {
               "entities": {
                  "Incident": {
                     "tags": [ "Core", 42 ],
                     "Elements": [ { "name": "Id", "type": "int" } ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Contains(result.Issues, i => i.Contains("a tag is not a string"));
         // The valid string tag is still captured.
         Assert.Equal(new[] { "Core" }, result.Tables.Single().Tags);
      }

      // -------- Spec reader ------------------------------------------------

      [Fact]
      public void UnknownSpecVersionIsReadTolerantly()
      {
         const string specJson = """
            {
               "specVersion": 99,
               "name": "future",
               "entities": { "path": "$.entities" }
            }
            """;

         const string json = """
            { "entities": { "A": { "elements": [ { "name": "id", "primaryKey": true } ] } } }
            """;

         var result = SchemaInterpreter.Interpret(json, MappingSpec.Parse(specJson));

         Assert.Single(result.Tables);
         Assert.Contains(result.Issues, issue => issue.Contains("newer"));
      }

      [Fact]
      public void UnknownReferenceIsDroppedWithAnIssue()
      {
         const string json = """
            {
               "entities": {
                  "Ghost": {
                     "Elements": [
                        { "name": "id", "primaryKey": true },
                        { "name": "parent", "Depends On": "NoSuchEntity" }
                     ]
                  }
               }
            }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         var ghost = result.Tables.Single();
         var parent = ghost.Columns.Single(c => c.ColumnName == "parent");
         Assert.False(parent.IsForeignKey);
         Assert.DoesNotContain(parent.Constraints, c => c.IsForeignKey);
         Assert.Contains(result.Issues, issue => issue.Contains("NoSuchEntity"));
      }

      [Fact]
      public void InvalidJsonYieldsAnIssueNotAThrow()
      {
         var result = SchemaInterpreter.Interpret("not json {{{", BuiltInProfiles.Array);

         Assert.Empty(result.Tables);
         Assert.Contains(result.Issues, issue => issue.Contains("not valid JSON"));
      }

      [Fact]
      public void MissingContainerYieldsAnIssueNotAThrow()
      {
         const string json = """
            { "something": { "else": 1 } }
            """;

         var result = SchemaInterpreter.Interpret(json, BuiltInProfiles.Grouped);

         Assert.Empty(result.Tables);
         Assert.Contains(result.Issues, issue => issue.Contains("entities container"));
      }

      // -------- Spec reader ------------------------------------------------

      [Fact]
      public void SpecParsesFromJson()
      {
         const string specJson = """
            {
               "specVersion": 1,
               "name": "third-party",
               "entities": { "path": "$.entities", "kind": "object", "nameField": "title" },
               "elements": { "nameField": "elementName", "refField": "Depends On" },
               "provenancePath": "$.provenance"
            }
            """;

         var spec = MappingSpec.Parse(specJson);

         Assert.Equal(1, spec.SpecVersion);
         Assert.Equal("third-party", spec.Name);
         Assert.Equal("$.entities", spec.Entities.Path);
         Assert.Equal("elementName", spec.Elements.NameField);
         Assert.Equal("Depends On", spec.Elements.RefField);
         Assert.Equal("$.provenance", spec.ProvenancePath);
      }

      [Fact]
      public void ProfilesResolveByName()
      {
         Assert.Same(BuiltInProfiles.Array, BuiltInProfiles.FromName("array"));
         Assert.Same(BuiltInProfiles.Grouped, BuiltInProfiles.FromName("grouped"));
         Assert.Null(BuiltInProfiles.FromName("nope"));
      }

      // -------- helpers -----------------------------------------------------

      private static IReadOnlyList<TableInfo> BuildArrayFixture()
      {
         return new[]
         {
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Parent",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "ID",
                     Type = "int",
                     IsKey = true,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  },
                  new ColumnInfo { ColumnName = "Name", Type = "nvarchar", Size = 100 }
               }
            },
            new TableInfo
            {
               SchemaName = "dbo",
               TableName = "Child",
               Columns = new ColumnList
               {
                  new ColumnInfo
                  {
                     ColumnName = "ID",
                     Type = "int",
                     IsKey = true,
                     Constraints = { new ConstraintInfo { Type = DataInfo.PRIMARY_KEY } }
                  },
                  new ColumnInfo
                  {
                     ColumnName = "ParentID",
                     Type = "int",
                     IsForeignKey = true,
                     Constraints =
                     {
                        new ConstraintInfo
                        {
                           Type = DataInfo.FOREIGN_KEY,
                           ReferencedTableName = "Parent",
                           ReferencedColumnName = "ID"
                        }
                     }
                  }
               }
            }
         };
      }

      /// <summary>
      /// Compare the renderer-relevant projection of a table: name, schema,
      /// column names/types/keyness, and FK constraints. The interpreter does
      /// not carry every array-format cosmetic field (ordinal, size, identity),
      /// so the comparison is over what FkEdgeExtractor and the renderers use.
      /// </summary>
      private static void AssertModelEquivalent(TableInfo expected, TableInfo actual)
      {
         Assert.Equal(expected.TableName, actual.TableName);
         Assert.Equal(expected.SchemaName, actual.SchemaName);
         Assert.Equal(expected.Columns.Count, actual.Columns.Count);

         foreach (var expectedColumn in expected.Columns)
         {
            var actualColumn = actual.Columns.First(c => c.ColumnName == expectedColumn.ColumnName);
            Assert.Equal(expectedColumn.Type, actualColumn.Type);
            Assert.Equal(expectedColumn.IsKey, actualColumn.IsKey);
            Assert.Equal(expectedColumn.IsForeignKey, actualColumn.IsForeignKey);

            var expectedFks = expectedColumn.Constraints.Where(c => c.IsForeignKey).ToList();
            var actualFks = actualColumn.Constraints.Where(c => c.IsForeignKey).ToList();
            Assert.Equal(expectedFks.Count, actualFks.Count);
            foreach (var expectedFk in expectedFks)
            {
               var actualFk = actualFks.First(f => f.ReferencedTableName == expectedFk.ReferencedTableName);
               Assert.Equal(expectedFk.ReferencedColumnName, actualFk.ReferencedColumnName);
            }
         }
      }

   }

}
