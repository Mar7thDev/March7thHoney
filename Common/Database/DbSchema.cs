using System.Data.Common;
using March7thHoney.Internationalization;

namespace March7thHoney.Database;

/// <summary>Schema changes the generated stores apply when they open their table.</summary>
internal static class DbSchema
{
    /// <summary>Adds the columns an entity declares that a table created by an older build does not have.</summary>
    public static void EnsureColumns(DbConnection connection, string table, (string Name, string Sql)[] columns)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "select * from \"" + table + "\" limit 0";
            using var reader = command.ExecuteReader();
            for (var i = 0; i < reader.FieldCount; i++) existing.Add(reader.GetName(i));
        }

        foreach (var (name, sql) in columns)
        {
            if (existing.Contains(name)) continue;
            using var alter = connection.CreateCommand();
            alter.CommandText = "alter table \"" + table + "\" add column \"" + name + "\" " + sql;
            alter.ExecuteNonQuery();
            DatabaseHelper.logger.Info(I18NManager.Translate("Server.Log.DatabaseColumnAdded", table, name));
        }
    }
}
