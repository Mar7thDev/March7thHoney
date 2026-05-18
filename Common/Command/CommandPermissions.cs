namespace March7thHoney.Command;

public static class CommandPermissions
{
    public const string Public = "";
    public const string All = "*";
    public const string AllCommands = "command.*";
    public const string LegacyManage = "egglink.manage";
    public const string Manage = "command.manage";
    public const string Moderate = "command.moderate";
    public const string Mail = "command.mail";
    public const string Server = "command.server";
    public const string TargetOthers = "command.others";
    public const string Player = "command.player";
    public const string Inventory = "command.inventory";
    public const string Progress = "command.progress";
    public const string Scene = "command.scene";
    public const string Debug = "command.debug";

    private static readonly char[] Separators = [',', ';', '\r', '\n', ' ', '\t'];
    private static readonly string[] LegacyManagePermissions = [Manage, Moderate, Mail, Server];

    public static bool Allows(string? grantedPermissions, string? requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission)) return true;

        foreach (var grantedPermission in Parse(grantedPermissions))
        {
            if (grantedPermission == All) return true;
            if (string.Equals(grantedPermission, LegacyManage, StringComparison.OrdinalIgnoreCase) &&
                LegacyManagePermissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase))
                return true;
            if (IsScopedWildcard(grantedPermission, requiredPermission)) return true;
            if (string.Equals(grantedPermission, requiredPermission, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    public static string Normalize(string? permissions)
    {
        return string.Join(",", Parse(permissions)
            .SelectMany(ExpandLegacyPermission)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static string Normalize(IEnumerable<string>? permissions)
    {
        return string.Join(",", (permissions ?? [])
            .SelectMany(Parse)
            .SelectMany(ExpandLegacyPermission)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> Parse(string? permissions)
    {
        return (permissions ?? string.Empty)
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(permission => !string.IsNullOrWhiteSpace(permission));
    }

    public static IEnumerable<string> KnownPermissions()
    {
        return
        [
            All,
            AllCommands,
            Player,
            Inventory,
            Progress,
            Scene,
            Debug,
            TargetOthers,
            Moderate,
            Mail,
            Server,
            Manage
        ];
    }

    private static IEnumerable<string> ExpandLegacyPermission(string permission)
    {
        if (string.Equals(permission, LegacyManage, StringComparison.OrdinalIgnoreCase))
            return LegacyManagePermissions;

        return [permission];
    }

    private static bool IsScopedWildcard(string grantedPermission, string requiredPermission)
    {
        if (!grantedPermission.EndsWith(".*", StringComparison.Ordinal)) return false;

        var prefix = grantedPermission[..^1];
        return requiredPermission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
