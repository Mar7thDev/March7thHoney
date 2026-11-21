using System.Data.Common;

namespace March7thHoney.Database;

/// <summary>
///     The database operations of one entity type. The source generator writes a store for every entity and
///     lists them in <c>EntityStores.Map</c>; <see cref="DatabaseHelper" /> reaches a table only through
///     this record, so it needs no reflection.
/// </summary>
/// <param name="IsPlayerData">False for an entity marked <see cref="NotPlayerDataAttribute" />.</param>
public sealed record EntityStore(
    Type Type,
    string Table,
    Action<DbConnection> EnsureTable,
    Func<DbConnection, List<BaseDatabaseDataHelper>> LoadAll,
    Action<DbConnection, BaseDatabaseDataHelper> Insert,
    Action<DbConnection, BaseDatabaseDataHelper> Update,
    Action<DbConnection, int> Delete,
    bool IsPlayerData);
