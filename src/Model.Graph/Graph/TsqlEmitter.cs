using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Model.Data;

namespace ModelConsole.Graph
{

   /// <summary>
   /// Contract for a replaceable Transact-SQL DDL exporter (backlog 055).
   /// Implementations are pure, portable, and deterministic: the same tables
   /// always produce the same script. Hosts consume only this interface and
   /// register their own implementation in the container to substitute it.
   /// </summary>
   public interface ITsqlEmitter
   {
      /// <summary>
      /// Emit a runnable SQL Server DDL script (schemas, tables, columns, PK,
      /// FK) from the model, with the issues found while resolving.
      /// </summary>
      /// <param name="tables">tables to export</param>
      /// <param name="options">export options (annotated vs bare)</param>
      /// <returns>the script plus any resolution/type-mapping issues</returns>
      TsqlExportResult EmitCreateScript(
         IReadOnlyList<TableInfo> tables, TsqlExportOptions options = null);
   }

   /// <summary>
   /// Options controlling a T-SQL export. Annotated is the default — the
   /// self-documenting mode that carries the model's design metadata (kind,
   /// tags, descriptions, cardinality, roles) as <c>-- KEY : value</c> comment
   /// cards that bare DDL cannot express.
   /// </summary>
   public sealed class TsqlExportOptions
   {
      /// <summary>
      /// True (default) emits the annotated comment cards before each table
      /// and FK reference; false emits clean DDL only.
      /// </summary>
      public bool Annotated { get; set; } = true;
   }

   /// <summary>
   /// The result of a T-SQL export: the emitted script plus any issues found
   /// while resolving FK references (from <see cref="FkEdgeExtractor"/>) and
   /// mapping column types.
   /// </summary>
   public sealed class TsqlExportResult
   {
      /// <summary>The emitted DDL script.</summary>
      public string Script { get; }

      /// <summary>Resolution / type-mapping issues; empty when clean.</summary>
      public IReadOnlyList<string> Issues { get; }

      /// <summary>TsqlExportResult class initialization.</summary>
      internal TsqlExportResult(string script, IReadOnlyList<string> issues)
      {
         Script = script;
         Issues = issues ?? new List<string>();
      }
   }

   /// <summary>
   /// A pure, deterministic Transact-SQL DDL exporter (backlog 055): emits
   /// <c>CREATE SCHEMA</c>, <c>CREATE TABLE</c> (columns with type/size/
   /// nullability/identity, inline or composite PK), and <c>ALTER TABLE ...
   /// ADD CONSTRAINT ... FOREIGN KEY</c> for every resolved FK — with an
   /// optional annotated mode that emits <c>-- KEY : value</c> comment cards
   /// carrying the model's kind/tags/descriptions/cardinality/roles. The
   /// model stays the source of truth; the script is a derived artifact.
   /// Portable (no WinUI/Skia dependency) and unit-testable.
   /// </summary>
   public sealed class TsqlEmitter : ITsqlEmitter
   {
      /// <summary>The default length applied when a length type has no size.</summary>
      private const int DefaultLength = 256;

      /// <summary>T-SQL character/binary types that take a length argument.</summary>
      private static readonly HashSet<string> LengthTypes =
         new HashSet<string>(StringComparer.Ordinal)
         {
            "VARCHAR", "NVARCHAR", "CHAR", "NCHAR", "BINARY", "VARBINARY"
         };

      /// <summary>Known fixed-width T-SQL scalar types (no length argument).</summary>
      private static readonly HashSet<string> KnownFixedTypes =
         new HashSet<string>(StringComparer.Ordinal)
         {
            "INT", "BIGINT", "SMALLINT", "TINYINT", "BIT",
            "MONEY", "SMALLMONEY", "FLOAT", "REAL",
            "DECIMAL", "NUMERIC",
            "DATE", "DATETIME", "DATETIME2", "DATETIMEOFFSET",
            "SMALLDATETIME", "TIME",
            "TEXT", "NTEXT", "IMAGE",
            "UNIQUEIDENTIFIER", "XML"
         };

      /// <summary>
      /// Emit the DDL script for the given tables. Output is deterministic:
      /// schemas by name, tables by schema then name, columns by ordinal, FKs
      /// by child table/column/parent table/column. FK references resolve
      /// null <c>ReferencedColumnName</c> to the parent PK via
      /// <see cref="FkEdgeExtractor"/>; unresolved FKs are reported and
      /// skipped. Unknown column types pass through verbatim with a
      /// diagnostic — never a crash.
      /// </summary>
      public TsqlExportResult EmitCreateScript(
         IReadOnlyList<TableInfo> tables, TsqlExportOptions options = null)
      {
         tables = tables ?? new List<TableInfo>();
         var opts = options ?? new TsqlExportOptions();
         var issues = new List<string>();
         var unknownTypes = new HashSet<string>(StringComparer.Ordinal);

         var (edges, fkIssues) = FkEdgeExtractor.Extract(tables);
         issues.AddRange(fkIssues);

         var byName = tables
            .Where(t => t != null && t.TableName != null)
            .ToDictionary(t => t.TableName, t => t, StringComparer.Ordinal);

         var sb = new StringBuilder();
         var schemas = tables.Where(t => t != null)
            .Select(t => t.SchemaName)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

         foreach (var schema in schemas)
         {
            sb.Append("CREATE SCHEMA [").Append(Quote(schema)).AppendLine("];");
         }
         if (schemas.Count > 0)
         {
            sb.AppendLine();
         }

         foreach (var table in tables.Where(t => t != null)
            .OrderBy(t => t.SchemaName ?? "", StringComparer.Ordinal)
            .ThenBy(t => t.TableName ?? "", StringComparer.Ordinal))
         {
            if (opts.Annotated)
            {
               AppendTableHeader(sb, table, edges);
            }
            EmitCreateTable(sb, table, unknownTypes);
            sb.AppendLine();
         }

         var fkNames = new HashSet<string>(StringComparer.Ordinal);
         foreach (var edge in edges
            .OrderBy(e => e.ChildTable, StringComparer.Ordinal)
            .ThenBy(e => e.ChildColumn, StringComparer.Ordinal)
            .ThenBy(e => e.ParentTable, StringComparer.Ordinal)
            .ThenBy(e => e.ParentColumn, StringComparer.Ordinal))
         {
            if (!byName.TryGetValue(edge.ChildTable, out TableInfo child) ||
                !byName.TryGetValue(edge.ParentTable, out TableInfo parent))
            {
               continue;
            }

            if (opts.Annotated)
            {
               AppendFkHeader(sb, edge);
            }
            string fkName = UniqueFkName(fkNames, edge.ChildTable, edge.ChildColumn);
            sb.Append("ALTER TABLE ")
               .Append(QualifiedName(child.SchemaName, child.TableName))
               .Append(" WITH CHECK ADD CONSTRAINT [")
               .Append(Quote(fkName))
               .Append("] FOREIGN KEY ([")
               .Append(Quote(edge.ChildColumn))
               .Append("]) REFERENCES ")
               .Append(QualifiedName(parent.SchemaName, parent.TableName))
               .Append(" ([")
               .Append(Quote(edge.ParentColumn))
               .Append("]);")
               .AppendLine();
            if (opts.Annotated)
            {
               sb.AppendLine();
            }
         }

         foreach (var type in unknownTypes.OrderBy(t => t, StringComparer.Ordinal))
         {
            issues.Add("unknown column type '" + type + "' passed through verbatim.");
         }

         return new TsqlExportResult(sb.ToString().TrimEnd() + Environment.NewLine, issues);
      }

      // ------------------------------------------------------------------

      private static void EmitCreateTable(
         StringBuilder sb, TableInfo table, ISet<string> unknownTypes)
      {
         var columns = (table.Columns ?? new List<ColumnInfo>())
            .Where(c => c != null)
            .OrderBy(c => c.OrdinalPosition)
            .ThenBy(c => c.ColumnName ?? "", StringComparer.Ordinal)
            .ToList();
         var keys = columns.Where(c => c.IsKey).ToList();

         sb.Append("CREATE TABLE ")
            .Append(QualifiedName(table.SchemaName, table.TableName))
            .AppendLine(" (");
         sb.Append("   ").AppendLine(ColumnDefinition(columns[0], keys.Count == 1, unknownTypes));
         for (int i = 1; i < columns.Count; i++)
         {
            sb.Append("  ,")
               .AppendLine(ColumnDefinition(columns[i], keys.Count == 1, unknownTypes));
         }
         if (keys.Count > 1)
         {
            sb.Append("  ,CONSTRAINT [")
               .Append(Quote("PK_" + table.TableName))
               .Append("] PRIMARY KEY (")
               .Append(string.Join(", ",
                  keys.Select(k => "[" + Quote(k.ColumnName) + "]")))
               .AppendLine(")");
         }
         sb.AppendLine(");");
      }

      private static string ColumnDefinition(
         ColumnInfo column, bool singleKey, ISet<string> unknownTypes)
      {
         var sb = new StringBuilder();
         sb.Append('[').Append(Quote(column.ColumnName)).Append("] ")
            .Append(MapType(column, unknownTypes));

         if (column.IsIdentity)
         {
            sb.Append(" IDENTITY(1,1)");
         }

         bool notNull = !column.IsNullable || column.IsKey;
         sb.Append(notNull ? " NOT NULL" : " NULL");

         if (singleKey && column.IsKey)
         {
            sb.Append(" PRIMARY KEY");
         }
         return sb.ToString();
      }

      private static string MapType(ColumnInfo column, ISet<string> unknownTypes)
      {
         string raw = string.IsNullOrWhiteSpace(column.Type)
            ? DataInfo.VARCHAR : column.Type.Trim();
         string type = raw.ToUpperInvariant();
         int size = column.Size;

         if (LengthTypes.Contains(type))
         {
            return type + "(" + (size > 0 ? size : DefaultLength) + ")";
         }

         if (KnownFixedTypes.Contains(type))
         {
            return type;
         }

         // Unknown type — pass through verbatim and note it once per distinct
         // type so the diagnostics channel sees it without crashing.
         unknownTypes.Add(raw);
         return raw;
      }

      private static void AppendTableHeader(
         StringBuilder sb, TableInfo table, IReadOnlyList<FkRelation> edges)
      {
         string kind = TableKindClassifier.Classify(table) == TableKind.ReferenceCode
            ? "reference" : "entity";
         var fkCount = edges.Count(e => e.ChildTable == table.TableName);

         sb.Append("-- ============================================================").AppendLine();
         sb.Append("-- TABLE : ").AppendLine(QualifiedName(table.SchemaName, table.TableName));
         sb.Append("-- KIND  : ").AppendLine(kind);
         if (table.Tags != null && table.Tags.Count > 0)
         {
            sb.Append("-- TAGS  : ").AppendLine(string.Join(", ", table.Tags));
         }
         if (!string.IsNullOrEmpty(table.Description))
         {
            sb.Append("-- DESC  : ").AppendLine(CommentValue(table.Description));
         }
         sb.Append("-- FKs   : ").Append(fkCount).AppendLine();
         sb.Append("-- ============================================================").AppendLine();
      }

      private static void AppendFkHeader(StringBuilder sb, FkRelation edge)
      {
         sb.Append("-- FK : [").Append(Quote(edge.ChildTable))
            .Append("].[").Append(Quote(edge.ChildColumn))
            .Append("] -> [").Append(Quote(edge.ParentTable))
            .Append("].[").Append(Quote(edge.ParentColumn)).AppendLine("]");

         var constraint = edge.Constraint;
         string cardinality = FormatCardinality(constraint);
         if (!string.IsNullOrEmpty(cardinality))
         {
            sb.Append("--     cardinality : ").AppendLine(cardinality);
         }
         if (!string.IsNullOrEmpty(constraint?.ChildRole))
         {
            sb.Append("--     child role  : ").AppendLine(CommentValue(constraint.ChildRole));
         }
         if (!string.IsNullOrEmpty(constraint?.ParentRole))
         {
            sb.Append("--     parent role : ").AppendLine(CommentValue(constraint.ParentRole));
         }
      }

      /// <summary>
      /// Format the FK cardinality as "child : parent" (e.g. "0..* : 1") from
      /// the constraint's Min/MaxCardinality; null when the constraint has no
      /// cardinality data.
      /// </summary>
      private static string FormatCardinality(ConstraintInfo constraint)
      {
         if (constraint == null ||
             (constraint.MinCardinality == null && constraint.MaxCardinality == null))
         {
            return null;
         }
         string child = UmlProfile.Multiplicity(
            constraint.MinCardinality, constraint.MaxCardinality);
         return (string.IsNullOrEmpty(child) ? "?" : child) + " : 1";
      }

      /// <summary>Sanitize a value for a single-line comment (no newlines).</summary>
      private static string CommentValue(string value)
      {
         return (value ?? "").Replace("\r", " ").Replace("\n", " ");
      }

      /// <summary>Escape a bracket-quoted T-SQL identifier's inner brackets.</summary>
      private static string Quote(string name)
      {
         return (name ?? "").Replace("]", "]]");
      }

      /// <summary>
      /// A bracket-quoted table name: <c>[schema].[name]</c> when a schema is
      /// present, otherwise just <c>[name]</c>.
      /// </summary>
      private static string QualifiedName(string schema, string name)
      {
         return string.IsNullOrEmpty(schema)
            ? "[" + Quote(name) + "]"
            : "[" + Quote(schema) + "].[" + Quote(name) + "]";
      }

      private static string UniqueFkName(
         ISet<string> used, string childTable, string childColumn)
      {
         string baseName = "FK_" + Clean(childTable) + "_" + Clean(childColumn);
         string name = baseName;
         int i = 2;
         while (!used.Add(name))
         {
            name = baseName + "_" + i++;
         }
         return name;
      }

      private static string Clean(string value)
      {
         var sb = new StringBuilder();
         foreach (char ch in value ?? "")
         {
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
         }
         return sb.Length == 0 ? "x" : sb.ToString();
      }

   }

}
