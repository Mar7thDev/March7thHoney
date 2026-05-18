namespace March7thHoney.Command;

public static class CommandRoles
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Administrator = "Administrator";
    public const string Developer = "Developer";

    public static Dictionary<string, HashSet<string>> DefaultRoles()
    {
        return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [User] =
            [
                CommandPermissions.Player,
                CommandPermissions.Inventory,
                CommandPermissions.Progress,
                CommandPermissions.Scene
            ],
            [Moderator] =
            [
                CommandPermissions.Player,
                CommandPermissions.Inventory,
                CommandPermissions.Progress,
                CommandPermissions.Scene,
                CommandPermissions.TargetOthers,
                CommandPermissions.Moderate
            ],
            [Administrator] =
            [
                CommandPermissions.AllCommands
            ],
            [Developer] =
            [
                CommandPermissions.All
            ]
        };
    }

    public static string NormalizeRole(string? role, string? defaultRole,
        IReadOnlyDictionary<string, HashSet<string>>? roles)
    {
        var roleName = string.IsNullOrWhiteSpace(role) ? defaultRole : role;
        roleName = string.IsNullOrWhiteSpace(roleName) ? User : roleName.Trim();

        if (roles != null)
            foreach (var configuredRole in roles.Keys)
                if (string.Equals(configuredRole, roleName, StringComparison.OrdinalIgnoreCase))
                    return configuredRole;

        return User;
    }

    public static HashSet<string> GetPermissions(string? role, string? defaultRole,
        IReadOnlyDictionary<string, HashSet<string>>? roles)
    {
        roles ??= DefaultRoles();
        var normalizedRole = NormalizeRole(role, defaultRole, roles);

        if (roles.TryGetValue(normalizedRole, out var permissions))
            return permissions;

        return roles.TryGetValue(User, out var userPermissions) ? userPermissions : [];
    }
}
