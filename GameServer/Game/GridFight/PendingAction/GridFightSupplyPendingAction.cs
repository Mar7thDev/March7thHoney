using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Enums.GridFight;
using CastoriceHoney.Server.GameServer.Game.GridFight.Sync;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.PendingAction;

public class GridFightSupplyPendingAction : BaseGridFightPendingAction
{
    public uint MaxRerollNum { get; set; } = 1;
    public uint CurRerollNum { get; set; }
    public List<GridFightGameSupplyRoleInfo> RoleList { get; set; } = [];

    public GridFightSupplyPendingAction(GridFightInstance inst) : base(inst)
    {
        for (var i = 0; i < 5; i++)
        {
            RoleList.Add(new GridFightGameSupplyRoleInfo(GameData.GridFightRoleBasicInfoData.Keys.ToList().RandomElement()));
        }
    }

    public async ValueTask Reroll()
    {
        if (MaxRerollNum <= CurRerollNum)
            return;

        CurRerollNum++;

        RoleList.Clear();
        for (var i = 0; i < 5; i++)
        {
            RoleList.Add(new GridFightGameSupplyRoleInfo(GameData.GridFightRoleBasicInfoData.Keys.ToList().RandomElement()));
        }

        // sync
        await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            new GridFightPendingActionSyncData(GridFightSrc.KGridFightSrcNone, this)));
    }

    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            QueuePosition = QueuePosition,
            SupplyAction = new GridFightSupplyActionInfo
            {
                MaxRerollCount = MaxRerollNum,
                CurRollCount = CurRerollNum,
                MaxSelectCount = 1,
                JLHIKCHIEDJ = 2,
                SupplyRoleInfoList = { RoleList.Select(x => x.ToProto()) }
            }
        };
    }
}

public class GridFightGameSupplyRoleInfo(uint roleId)
{
    public uint RoleId { get; set; } = roleId;

    public uint EquipmentId { get; set; } = GameData.GridFightEquipmentData.Values
        .Where(x => x.EquipCategory == GridFightEquipCategoryEnum.Craftable).ToList().RandomElement().ID;

    public GridFightSupplyRoleInfo ToProto()
    {
        return new GridFightSupplyRoleInfo
        {
            RoleBasicId = RoleId,
            GridFightItemList = { EquipmentId }
        };
    }
}