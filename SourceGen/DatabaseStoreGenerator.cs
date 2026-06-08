using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace March7thHoney.SourceGen;

/// <summary>
///     Emits, for every entity (subclass of BaseDatabaseDataHelper), a Dapper.AOT-backed store: a row record
///     (scalar columns + byte[] BLOBs for complex columns), CRUD + DDL, and conversions that MemoryPack-serialize
///     complex POCO columns / proto-serialize IMessage columns. Also emits a Type->store registry so DatabaseHelper
///     dispatches with zero reflection. Replaces SqlSugar.
/// </summary>
[Generator]
public sealed class DatabaseStoreGenerator : IIncrementalGenerator
{
    private const string BaseType = "March7thHoney.Database.BaseDatabaseDataHelper";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entities = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax cls && cls.BaseList != null &&
                                    !cls.Modifiers.Any(m => m.ValueText == "abstract"),
                static (ctx, _) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) is not INamedTypeSymbol sym) return null;
                    return DerivesFromBase(sym) ? Extract(sym) : null;
                })
            .Where(static x => x != null)
            .Select(static (x, _) => x!)
            .Collect();

        context.RegisterSourceOutput(entities, static (spc, items) =>
        {
            if (items.IsDefaultOrEmpty) return;
            spc.AddSource("DbStores.g.cs", Emit(items));
        });
    }

    private static bool DerivesFromBase(INamedTypeSymbol sym)
    {
        for (var t = sym.BaseType; t != null; t = t.BaseType)
            if (t.ToDisplayString() == BaseType)
                return true;
        return false;
    }

    private static Entity Extract(INamedTypeSymbol sym)
    {
        var fqn = sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var table = sym.Name;
        foreach (var attr in sym.GetAttributes())
            if (attr.AttributeClass?.Name == "DbTableAttribute" && attr.ConstructorArguments.Length == 1)
                table = attr.ConstructorArguments[0].Value as string ?? table;

        var cols = new List<Column>();
        // Collect properties from the type and its bases (to include Uid), declaration order, base-first.
        var chain = new List<INamedTypeSymbol>();
        for (var t = sym; t != null && t.ToDisplayString() != "object"; t = t.BaseType) chain.Insert(0, t);
        foreach (var t in chain)
        foreach (var member in t.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsStatic || member.IsIndexer || member.GetMethod == null || member.SetMethod == null) continue;
            if (member.GetAttributes().Any(a => a.AttributeClass?.Name == "DbIgnoreAttribute")) continue;
            if (cols.Any(c => c.Name == member.Name)) continue;
            cols.Add(Classify(member));
        }

        return new Entity(fqn, sym.Name, table, cols);
    }

    private static Column Classify(IPropertySymbol p)
    {
        var type = p.Type;
        var nullable = type.NullableAnnotation == NullableAnnotation.Annotated ||
                       (type is INamedTypeSymbol nn && nn.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
        var underlying = type is INamedTypeSymbol n && n.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? n.TypeArguments[0]
            : type;

        var fqType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingFq = underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var kind = ColumnKind.Complex;
        var sql = "BLOB";
        if (underlying is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
        {
            kind = ColumnKind.Scalar;
            sql = "BLOB";
        }
        else if (underlying.TypeKind == TypeKind.Enum)
        {
            kind = ColumnKind.Scalar;
            sql = "INTEGER";
        }
        else
        {
            switch (underlying.SpecialType)
            {
                case SpecialType.System_String:
                    kind = ColumnKind.Scalar;
                    sql = "TEXT";
                    break;
                case SpecialType.System_Boolean:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    kind = ColumnKind.Scalar;
                    sql = "INTEGER";
                    break;
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    kind = ColumnKind.Scalar;
                    sql = "REAL";
                    break;
                default:
                    kind = underlying.AllInterfaces.Any(i => i.ToDisplayString() == "Google.Protobuf.IMessage")
                        ? ColumnKind.Proto
                        : ColumnKind.Complex;
                    sql = "BLOB";
                    break;
            }
        }

        return new Column(p.Name, fqType, underlyingFq, kind, sql, nullable);
    }

    private static string Emit(IEnumerable<Entity> entities)
    {
        var list = entities.OrderBy(e => e.ClassName).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        // CS0618: generated mapping intentionally reads/writes every persisted column,
        // including ones the entity marks [Obsolete] for source callers.
        sb.AppendLine("#pragma warning disable CS8618, CS8601, CS8603, CS8604, CS0618");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data.Common;");
        sb.AppendLine("using Dapper;");
        sb.AppendLine("using MemoryPack;");
        sb.AppendLine();
        sb.AppendLine("namespace March7thHoney.Database.Generated;");
        sb.AppendLine();

        foreach (var e in list) EmitStore(sb, e);

        // Registry
        sb.AppendLine(
            "public sealed record EntityStore(global::System.Type Type, string Table, " +
            "global::System.Action<DbConnection> EnsureTable, " +
            "global::System.Func<DbConnection, List<global::March7thHoney.Database.BaseDatabaseDataHelper>> LoadAll, " +
            "global::System.Action<DbConnection, global::March7thHoney.Database.BaseDatabaseDataHelper> Insert, " +
            "global::System.Action<DbConnection, global::March7thHoney.Database.BaseDatabaseDataHelper> Update, " +
            "global::System.Action<DbConnection, int> Delete);");
        sb.AppendLine();
        sb.AppendLine("public static class EntityStores");
        sb.AppendLine("{");
        sb.AppendLine(
            "    public static readonly global::System.Collections.Generic.Dictionary<global::System.Type, EntityStore> Map = new()");
        sb.AppendLine("    {");
        foreach (var e in list)
        {
            sb.AppendLine($"        [typeof({e.Fqn})] = new EntityStore(");
            sb.AppendLine($"            typeof({e.Fqn}), {e.ClassName}Store.Table, {e.ClassName}Store.EnsureTable,");
            sb.AppendLine(
                $"            c => {{ var r = new List<global::March7thHoney.Database.BaseDatabaseDataHelper>(); foreach (var x in {e.ClassName}Store.LoadAll(c)) r.Add(x); return r; }},");
            sb.AppendLine($"            (c, e) => {e.ClassName}Store.Insert(c, ({e.Fqn})e),");
            sb.AppendLine($"            (c, e) => {e.ClassName}Store.Update(c, ({e.Fqn})e),");
            sb.AppendLine($"            {e.ClassName}Store.Delete),");
        }

        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void EmitStore(StringBuilder sb, Entity e)
    {
        var cols = e.Columns;
        var colNames = cols.Select(c => $"\\\"{c.Name}\\\"").ToList();

        // Row record
        sb.AppendLine($"internal sealed class {e.ClassName}Row");
        sb.AppendLine("{");
        foreach (var c in cols)
            sb.AppendLine($"    public {RowType(c)} {c.Name} {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine();

        // Store
        sb.AppendLine("[global::Dapper.DapperAot]");
        sb.AppendLine($"internal static class {e.ClassName}Store");
        sb.AppendLine("{");
        sb.AppendLine($"    public const string Table = \"{e.Table}\";");

        var ddlCols = string.Join(", ",
            cols.Select(c => $"\\\"{c.Name}\\\" {c.SqlType}{(c.Name == "Uid" ? " PRIMARY KEY" : "")}"));
        sb.AppendLine(
            $"    public static void EnsureTable(DbConnection c) => c.Execute(\"create table if not exists \\\"{e.Table}\\\" ({ddlCols})\");");

        var selectCols = string.Join(", ", colNames);
        sb.AppendLine(
            $"    public static List<{e.Fqn}> LoadAll(DbConnection c)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var list = new List<{e.Fqn}>();");
        sb.AppendLine(
            $"        foreach (var r in c.Query<{e.ClassName}Row>(\"select {selectCols} from \\\"{e.Table}\\\"\")) list.Add(ToEntity(r));");
        sb.AppendLine("        return list;");
        sb.AppendLine("    }");

        var insertCols = string.Join(", ", colNames);
        var insertVals = string.Join(", ", cols.Select(c => "@" + c.Name));
        sb.AppendLine(
            $"    public static void Insert(DbConnection c, {e.Fqn} e) => c.Execute(\"insert into \\\"{e.Table}\\\" ({insertCols}) values ({insertVals})\", ToRow(e));");

        var setClause = string.Join(", ", cols.Where(c => c.Name != "Uid").Select(c => $"\\\"{c.Name}\\\"=@{c.Name}"));
        sb.AppendLine(
            $"    public static void Update(DbConnection c, {e.Fqn} e) => c.Execute(\"update \\\"{e.Table}\\\" set {setClause} where \\\"Uid\\\"=@Uid\", ToRow(e));");

        sb.AppendLine(
            $"    public static void Delete(DbConnection c, int uid) => c.Execute(\"delete from \\\"{e.Table}\\\" where \\\"Uid\\\"=@Uid\", new {{ Uid = uid }});");

        // ToEntity
        sb.AppendLine($"    private static {e.Fqn} ToEntity({e.ClassName}Row r) => new {e.Fqn}");
        sb.AppendLine("    {");
        foreach (var c in cols)
            sb.AppendLine($"        {c.Name} = {ReadExpr(c)},");
        sb.AppendLine("    };");

        // ToRow
        sb.AppendLine($"    private static {e.ClassName}Row ToRow({e.Fqn} e) => new {e.ClassName}Row");
        sb.AppendLine("    {");
        foreach (var c in cols)
            sb.AppendLine($"        {c.Name} = {WriteExpr(c)},");
        sb.AppendLine("    };");

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static string RowType(Column c)
    {
        return c.Kind == ColumnKind.Scalar ? c.FqType : "byte[]?";
    }

    private static string ReadExpr(Column c)
    {
        switch (c.Kind)
        {
            case ColumnKind.Scalar:
                return $"r.{c.Name}";
            case ColumnKind.Proto:
                return c.Nullable
                    ? $"r.{c.Name} is {{ }} b{c.Name} ? {c.UnderlyingFq}.Parser.ParseFrom(b{c.Name}) : null"
                    : $"r.{c.Name} is {{ }} b{c.Name} ? {c.UnderlyingFq}.Parser.ParseFrom(b{c.Name}) : new {c.UnderlyingFq}()";
            default: // Complex (MemoryPack)
                return c.Nullable
                    ? $"r.{c.Name} is {{ }} b{c.Name} ? MemoryPackSerializer.Deserialize<{c.UnderlyingFq}>(b{c.Name}) : null"
                    : $"r.{c.Name} is {{ }} b{c.Name} ? (MemoryPackSerializer.Deserialize<{c.UnderlyingFq}>(b{c.Name}) ?? new {c.UnderlyingFq}()) : new {c.UnderlyingFq}()";
        }
    }

    private static string WriteExpr(Column c)
    {
        switch (c.Kind)
        {
            case ColumnKind.Scalar:
                return $"e.{c.Name}";
            case ColumnKind.Proto:
                return $"e.{c.Name}?.ToByteArray()";
            default:
                return c.Nullable
                    ? $"e.{c.Name} is null ? null : MemoryPackSerializer.Serialize(e.{c.Name})"
                    : $"MemoryPackSerializer.Serialize(e.{c.Name})";
        }
    }

    private enum ColumnKind
    {
        Scalar,
        Complex,
        Proto
    }

    private sealed class Column(string name, string fqType, string underlyingFq, ColumnKind kind, string sqlType,
        bool nullable)
    {
        public string Name { get; } = name;
        public string FqType { get; } = fqType;
        public string UnderlyingFq { get; } = underlyingFq;
        public ColumnKind Kind { get; } = kind;
        public string SqlType { get; } = sqlType;
        public bool Nullable { get; } = nullable;
    }

    private sealed class Entity(string fqn, string className, string table, List<Column> columns)
    {
        public string Fqn { get; } = fqn;
        public string ClassName { get; } = className;
        public string Table { get; } = table;
        public List<Column> Columns { get; } = columns;
    }
}
