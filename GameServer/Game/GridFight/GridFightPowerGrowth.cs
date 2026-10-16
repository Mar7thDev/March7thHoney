namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    public const uint PowerGrowthPortalId = 136;
    // Temporary, user-approved rule: the S1 wiki does not publish the random pool or magnitudes.
    // One 5% bonus per new node: attack, HP, damage, on-field power or off-field power (permille).
    private static readonly uint[] PowerGrowthProperties = [1001, 1009, 1014, 1027, 1030];
    private readonly List<uint> _powerGrowthRoleUpdates = [];

    private void GrowPowerPortalRoles()
    {
        foreach (var role in Roles.Where(role => role.HasPowerGrowth))
        {
            var property = PowerGrowthProperties[_catalog.NextRandom(0, PowerGrowthProperties.Length)];
            role.PermanentPropertyBonuses[property] = role.PermanentPropertyBonuses.GetValueOrDefault(property) + 50;
            _powerGrowthRoleUpdates.Add(role.UniqueId);
        }
    }

    public IReadOnlyList<uint> TakePowerGrowthRoleUpdates()
    {
        var result = _powerGrowthRoleUpdates.ToList();
        _powerGrowthRoleUpdates.Clear();
        return result;
    }

    public void ApplyPermanentRolePropertyScales(IDictionary<uint, uint> properties, GridFightRoleState role)
    {
        foreach (var (property, bonus) in role.PermanentPropertyBonuses)
            properties[property] = (properties.TryGetValue(property, out var current) ? current : 0) + bonus;
    }
}
