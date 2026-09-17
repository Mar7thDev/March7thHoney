using March7thHoney.Data;
using March7thHoney.Enums.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    private const uint DiceFilledEquipmentSource = 100;
    private const uint CasualDiceEquipmentId = 35030404;
    private const uint PrivilegeDiceEquipmentId = 35040404;
    private readonly Dictionary<uint, uint[]> _diceBindings = [];
    private readonly List<GridFightEquipmentState> _removedDiceEquipment = [];
    private readonly List<GridFightEquipmentState> _addedDiceEquipment = [];
    private readonly HashSet<GridFightRoleState> _updatedDiceRoles = [];

    public static bool IsDiceEquipment(uint equipmentId) =>
        equipmentId is CasualDiceEquipmentId or PrivilegeDiceEquipmentId;

    public static bool IsDiceFilledEquipment(GridFightEquipmentState equipment) =>
        equipment.Source == DiceFilledEquipmentSource;

    private void DestroyDiceFilledOnRole(GridFightRoleState role)
    {
        foreach (var equipment in GetEquippedEquipment(role).Where(IsDiceFilledEquipment))
        {
            role.EquippedEquipmentUniqueIds.Remove(equipment.UniqueId);
            Equipments.Remove(equipment);
            // Items created and removed before publication need no wire update.
            if (!_addedDiceEquipment.Remove(equipment)) _removedDiceEquipment.Add(equipment);
            _updatedDiceRoles.Add(role);
        }
    }

    private uint[] RollDiceBinding(GridFightEquipmentState dice)
    {
        var category = GameData.GridFightEquipmentData.GetValueOrDefault(dice.EquipmentId)?.EquipCategory;
        if (category is null or GridFightEquipCategoryEnum.None) return [];
        var pool = GameData.GridFightEquipmentData.Values
            .Where(row => row.EquipCategory == category && !IsDiceEquipment(row.ID) &&
                          row.EquipType != GridFightEquipmentTypeEnum.Implants &&
                          row.DressRule != GridFightEquipDressTypeEnum.DressRuleAllSlotEmpty)
            .Select(row => row.ID).Order().ToList();
        return pool.Count == 0 ? [] : [_catalog.RollOne(pool), _catalog.RollOne(pool)];
    }

    private void ApplyDiceEquipmentToRole(GridFightRoleState role)
    {
        var dice = GetEquippedEquipment(role).FirstOrDefault(item => IsDiceEquipment(item.EquipmentId));
        if (dice == null) return;
        DestroyDiceFilledOnRole(role);
        if (!_diceBindings.TryGetValue(dice.UniqueId, out var binding))
            _diceBindings[dice.UniqueId] = binding = RollDiceBinding(dice);
        foreach (var equipmentId in binding)
        {
            var equipment = CreateEquipment(equipmentId, DiceFilledEquipmentSource);
            equipment.IsLocked = true;
            role.EquippedEquipmentUniqueIds.Add(equipment.UniqueId);
            _addedDiceEquipment.Add(equipment);
        }
        _updatedDiceRoles.Add(role);
        RefreshEquipmentTrackCompletionForRole(role);
    }

    public void ApplyDiceEquipmentRefills()
    {
        var diceItems = Equipments.Where(item => IsDiceEquipment(item.EquipmentId)).ToList();
        var liveIds = diceItems.Select(item => item.UniqueId).ToHashSet();
        foreach (var id in _diceBindings.Keys.Where(id => !liveIds.Contains(id)).ToList())
            _diceBindings.Remove(id);
        foreach (var dice in diceItems) _diceBindings[dice.UniqueId] = RollDiceBinding(dice);
        foreach (var role in Roles) ApplyDiceEquipmentToRole(role);
    }

    public GridFightEquipDressResult TakeDiceEquipmentChanges()
    {
        var result = new GridFightEquipDressResult(true, _removedDiceEquipment.ToArray(),
            _addedDiceEquipment.ToArray(), _updatedDiceRoles.Where(Roles.Contains).ToArray(), false, false);
        _removedDiceEquipment.Clear();
        _addedDiceEquipment.Clear();
        _updatedDiceRoles.Clear();
        return result;
    }
}

public sealed partial class GridFightManager
{
    private void AppendDiceEquipmentRefreshSync(GridFightSyncUpdateResultScNotify notify)
    {
        if (Session == null) return;
        var changes = Session.TakeDiceEquipmentChanges();
        if (changes.RemovedEquipments.Count == 0 && changes.AddedEquipments.Count == 0 &&
            changes.UpdatedRoles.Count == 0) return;
        var result = new GridFightSyncResultData { UpdateSource = GridFightUpdateSrcType.KgridFightSrcDressEquip };
        AppendEquipmentChanges(result, changes.RemovedEquipments, changes.AddedEquipments);
        foreach (var role in changes.UpdatedRoles)
            result.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRole(role) });
        notify.ResultList.Add(result);
    }
}
