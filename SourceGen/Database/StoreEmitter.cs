using System.Collections.Generic;
using System.Linq;

namespace March7thHoney.SourceGen.Database;

/// <summary>
///     Writes <c>DbStores.g.cs</c>. For each entity: a row class holding what the database stores (scalars
///     as they are, everything else as bytes), and a store with the table DDL and the load, insert, update
///     and delete commands, written against <c>DbCommand</c> and <c>DbDataReader</c> directly so nothing is
///     resolved by reflection at run time. Then the <c>EntityStores</c> map <c>DatabaseHelper</c> dispatches
///     through, and the registration of the MemoryPack formatters the BLOB columns need.
/// </summary>
internal static class StoreEmitter
{
    /// <summary>Every entity inherits this column from <c>BaseDatabaseDataHelper</c>; rows are keyed by it.</summary>
    private const string Key = "Uid";

    private const string EntityBase = "global::March7thHoney.Database.BaseDatabaseDataHelper";

    public static string Emit(IReadOnlyList<Entity> entities)
    {
        var code = new CodeWriter();
        // The mapping reads and writes every persisted column, including one the entity marks [Obsolete]
        // for callers in source (CS0618).
        code.Line("#pragma warning disable CS8618, CS8601, CS8603, CS8604, CS0618");
        code.Line("using System;");
        code.Line("using System.Collections.Generic;");
        code.Line("using System.Data.Common;");
        code.Line("using System.Globalization;");
        if (entities.Any(e => e.Columns.Any(c => c.Kind == ColumnKind.Protobuf))) code.Line("using Google.Protobuf;");
        code.Line("using MemoryPack;");
        code.Line();
        code.Line("namespace March7thHoney.Database.Generated;");
        code.Line();

        foreach (var entity in entities)
        {
            EmitRow(code, entity);
            code.Line();
            EmitStore(code, entity);
            code.Line();
        }

        EmitRegistry(code, entities);
        code.Line();
        EmitFormatterRegistrar(code, entities);
        return code.ToString();
    }

    private static void EmitRow(CodeWriter code, Entity entity)
    {
        code.Line($"internal sealed class {entity.ClassName}Row");
        using (code.Block())
        {
            foreach (var column in entity.Columns)
                code.Line($"public {RowType(column)} {column.Name} {{ get; set; }}");
        }
    }

    private static void EmitStore(CodeWriter code, Entity entity)
    {
        var row = $"{entity.ClassName}Row";
        var table = Identifier(entity.Table);
        var columns = entity.Columns;
        var columnList = string.Join(", ", columns.Select(c => Identifier(c.Name)));

        code.Line($"internal static class {entity.ClassName}Store");
        using (code.Block())
        {
            code.Line($"public const string Table = \"{entity.Table}\";");
            code.Line();

            // A table created by an older build gets the columns added since.
            var definitions = string.Join(", ",
                columns.Select(c => $"{Identifier(c.Name)} {c.SqlType}{(c.Name == Key ? " PRIMARY KEY" : "")}"));
            code.Line("public static void EnsureTable(DbConnection c)");
            using (code.Block())
            {
                code.Line("using (var cmd = c.CreateCommand())");
                using (code.Block())
                {
                    code.Line($"cmd.CommandText = \"create table if not exists {table} ({definitions})\";");
                    code.Line("cmd.ExecuteNonQuery();");
                }

                code.Line();
                code.Line("DbSchema.EnsureColumns(c, Table, new[]");
                using (code.Block("});"))
                {
                    foreach (var column in columns)
                        code.Line($"(\"{column.Name}\", \"{column.SqlType}\"),");
                }
            }

            code.Line();
            code.Line($"public static List<{entity.TypeName}> LoadAll(DbConnection c)");
            using (code.Block())
            {
                code.Line($"var list = new List<{entity.TypeName}>();");
                code.Line("using var cmd = c.CreateCommand();");
                code.Line($"cmd.CommandText = \"select {columnList} from {table}\";");
                code.Line("using var rd = cmd.ExecuteReader();");
                code.Line("while (rd.Read())");
                using (code.Block())
                {
                    code.Line($"var r = new {row}");
                    using (code.Block("};"))
                    {
                        for (var ordinal = 0; ordinal < columns.Count; ordinal++)
                            code.Line($"{columns[ordinal].Name} = {ReadFromReader(columns[ordinal], ordinal)},");
                    }

                    code.Line("list.Add(ToEntity(r));");
                }

                code.Line("return list;");
            }

            code.Line();
            code.Line($"private static void Bind(DbCommand cmd, {row} row)");
            using (code.Block())
            {
                code.Line("DbParameter p;");
                foreach (var column in columns)
                {
                    code.Line("p = cmd.CreateParameter();");
                    code.Line($"p.ParameterName = \"@{column.Name}\";");
                    code.Line($"p.Value = {ParameterValue(column)};");
                    code.Line("cmd.Parameters.Add(p);");
                }
            }

            code.Line();
            var parameters = string.Join(", ", columns.Select(c => "@" + c.Name));
            code.Line($"public static void Insert(DbConnection c, {entity.TypeName} e)");
            using (code.Block())
            {
                code.Line("var row = ToRow(e);");
                code.Line("using var cmd = c.CreateCommand();");
                code.Line($"cmd.CommandText = \"insert into {table} ({columnList}) values ({parameters})\";");
                code.Line("Bind(cmd, row);");
                code.Line("cmd.ExecuteNonQuery();");
            }

            code.Line();
            var assignments = string.Join(", ",
                columns.Where(c => c.Name != Key).Select(c => $"{Identifier(c.Name)}=@{c.Name}"));
            code.Line($"public static void Update(DbConnection c, {entity.TypeName} e)");
            using (code.Block())
            {
                code.Line("var row = ToRow(e);");
                code.Line("using var cmd = c.CreateCommand();");
                code.Line($"cmd.CommandText = \"update {table} set {assignments} where {Identifier(Key)}=@{Key}\";");
                code.Line("Bind(cmd, row);");
                code.Line("cmd.ExecuteNonQuery();");
            }

            code.Line();
            code.Line("public static void Delete(DbConnection c, int uid)");
            using (code.Block())
            {
                code.Line("using var cmd = c.CreateCommand();");
                code.Line($"cmd.CommandText = \"delete from {table} where {Identifier(Key)}=@{Key}\";");
                code.Line("var p = cmd.CreateParameter();");
                code.Line($"p.ParameterName = \"@{Key}\";");
                code.Line("p.Value = uid;");
                code.Line("cmd.Parameters.Add(p);");
                code.Line("cmd.ExecuteNonQuery();");
            }

            code.Line();
            code.Line($"private static {entity.TypeName} ToEntity({row} r) => new {entity.TypeName}");
            using (code.Block("};"))
            {
                foreach (var column in columns)
                    code.Line($"{column.Name} = {FromRow(column)},");
            }

            code.Line();
            code.Line($"private static {row} ToRow({entity.TypeName} e) => new {row}");
            using (code.Block("};"))
            {
                foreach (var column in columns)
                    code.Line($"{column.Name} = {ToRow(column)},");
            }
        }
    }

    private static void EmitRegistry(CodeWriter code, IReadOnlyList<Entity> entities)
    {
        code.Line("public static class EntityStores");
        using (code.Block())
        {
            code.Line("public static readonly global::System.Collections.Generic.Dictionary<global::System.Type, EntityStore> Map = new()");
            using (code.Block("};"))
            {
                foreach (var entity in entities)
                {
                    var store = $"{entity.ClassName}Store";
                    code.Line($"[typeof({entity.TypeName})] = new EntityStore(");
                    using (code.Indent())
                    {
                        code.Line($"typeof({entity.TypeName}), {store}.Table, {store}.EnsureTable,");
                        code.Line($"c => {{ var r = new List<{EntityBase}>(); foreach (var x in {store}.LoadAll(c)) r.Add(x); return r; }},");
                        code.Line($"(c, e) => {store}.Insert(c, ({entity.TypeName})e),");
                        code.Line($"(c, e) => {store}.Update(c, ({entity.TypeName})e),");
                        code.Line($"{store}.Delete),");
                    }
                }
            }
        }
    }

    /// <summary>The registrations run when the module loads, before anything is serialized.</summary>
    private static void EmitFormatterRegistrar(CodeWriter code, IReadOnlyList<Entity> entities)
    {
        code.Line("internal static class DbCollectionFormatterRegistrar");
        using (code.Block())
        {
            code.Line("private static bool _done;");
            code.Line();
            code.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            code.Line("internal static void Register()");
            using (code.Block())
            {
                code.Line("if (_done) return;");
                code.Line("_done = true;");
                foreach (var formatter in entities.SelectMany(e => e.FormatterRegistrations).Distinct()
                             .OrderBy(f => f, System.StringComparer.Ordinal))
                    code.Line($"global::MemoryPack.MemoryPackFormatterProvider.Register({formatter});");
            }
        }
    }

    /// <summary>A quoted SQL identifier, escaped for the C# string literal the statement is written in.</summary>
    private static string Identifier(string name) => $"\\\"{name}\\\"";

    private static string RowType(Column column) => column.Kind == ColumnKind.Scalar ? column.TypeName : "byte[]?";

    /// <summary>
    ///     Reads the column at <paramref name="ordinal" /> into its row property. Scalars go through
    ///     <c>Convert</c> because the providers box differently: SQLite returns long, double or string,
    ///     MySQL the declared type.
    /// </summary>
    private static string ReadFromReader(Column column, int ordinal)
    {
        var isNull = $"rd.IsDBNull({ordinal})";
        var value = $"rd.GetValue({ordinal})";
        const string culture = "CultureInfo.InvariantCulture";

        switch (column.Scalar)
        {
            case ScalarType.None or ScalarType.Bytes:
                return $"{isNull} ? null : (byte[]){value}";
            case ScalarType.String:
                return $"{isNull} ? null : Convert.ToString({value}, {culture})";
        }

        var converted = column.Scalar == ScalarType.Enum
            ? $"({column.ValueTypeName})Convert.ToInt64({value}, {culture})"
            : $"Convert.To{column.Scalar}({value}, {culture})";
        var missing = column.Nullable ? $"({column.TypeName})null" : $"default({column.TypeName})";
        return $"{isNull} ? {missing} : {converted}";
    }

    /// <summary>The value bound to the column's parameter: an enum as its number, null as <c>DBNull</c>.</summary>
    private static string ParameterValue(Column column)
    {
        if (column.Scalar != ScalarType.Enum) return $"((object?)row.{column.Name}) ?? DBNull.Value";
        return column.Nullable
            ? $"row.{column.Name}.HasValue ? (object)Convert.ToInt64(row.{column.Name}.Value) : (object)DBNull.Value"
            : $"(object)Convert.ToInt64(row.{column.Name})";
    }

    /// <summary>Turns a row property into the entity property; a missing BLOB becomes null or an empty value.</summary>
    private static string FromRow(Column column)
    {
        var bytes = $"b{column.Name}";
        var present = $"r.{column.Name} is {{ }} {bytes}";
        var empty = column.Nullable ? "null" : $"new {column.ValueTypeName}()";
        switch (column.Kind)
        {
            case ColumnKind.Protobuf:
                return $"{present} ? {column.ValueTypeName}.Parser.ParseFrom({bytes}) : {empty}";
            case ColumnKind.MemoryPack:
                var deserialized = $"MemoryPackSerializer.Deserialize<{column.ValueTypeName}>({bytes})";
                return column.Nullable
                    ? $"{present} ? {deserialized} : null"
                    : $"{present} ? ({deserialized} ?? {empty}) : {empty}";
            default:
                return $"r.{column.Name}";
        }
    }

    private static string ToRow(Column column) => column.Kind switch
    {
        ColumnKind.Protobuf => $"e.{column.Name}?.ToByteArray()",
        ColumnKind.MemoryPack when column.Nullable => $"e.{column.Name} is null ? null : MemoryPackSerializer.Serialize(e.{column.Name})",
        ColumnKind.MemoryPack => $"MemoryPackSerializer.Serialize(e.{column.Name})",
        _ => $"e.{column.Name}",
    };
}
