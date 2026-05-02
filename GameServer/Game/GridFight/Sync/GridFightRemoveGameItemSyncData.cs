using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightRemoveGameItemSyncData(GridFightSrc src, List<GridFightEquipmentItemPb> equipment, List<GridFightConsumableUpdateInfo> consumables, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public List<GridFightEquipmentItemPb> Equipment { get; } = equipment;
    public List<GridFightConsumableUpdateInfo> Consumables { get; } = consumables;

    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            RemoveGameItemInfo = new GridFightGameItemSyncInfo
            {
                GridFightEquipmentList = { Equipment.Select(x => x.ToProto()) },
                UpdateGridFightConsumableList = { Consumables }
            }
        };
    }
}