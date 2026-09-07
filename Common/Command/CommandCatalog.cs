namespace March7thHoney.Command;

/// <summary>
///     Read-only description of a registered command, exposed to the admin console so it can render a
///     command reference without depending on the Command project (which lives above WebServer).
/// </summary>
public sealed record CommandCatalogEntry(
    string Name,
    string Description,
    string Usage,
    string Permission,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Subcommands);
