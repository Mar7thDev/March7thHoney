using March7thHoney.Database.Avatar;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.GridFight.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Battle;

public class PacketPVEBattleResultScRsp : BasePacket
{
    public PacketPVEBattleResultScRsp() : base(CmdIds.PVEBattleResultScRsp)
    {
        var proto = new PVEBattleResultScRsp
        {
            Retcode = 1
        };

        SetData(proto);
    }

    public PacketPVEBattleResultScRsp(PVEBattleResultCsReq req, PlayerInstance player, BattleInstance battle,
        ItemList dropItemList) : base(
        CmdIds.PVEBattleResultScRsp)
    {
        var proto = new PVEBattleResultScRsp
        {
            DropData = dropItemList,
            StageId = req.StageId,
            BattleId = req.BattleId,
            EndStatus = req.EndStatus,
            CheckIdentical = true,
            ItemListUnk1 = new ItemList(),
            ItemListUnk2 = new ItemList(),
            MultipleDropData = new ItemList(),
            EventId = (uint)battle.EventId
        };

        if (battle.GridFightContext != null)
        {
            var collection = new PlayerDataCollection(player.Data, player.InventoryManager!.Data, battle.Lineup);
            foreach (var avatar in GridFightBattleProtoBuilder.BuildBattleAvatars(battle, battle.GridFightContext))
                proto.BattleAvatarList.Add(GridFightBattleProtoBuilder.BuildBattleAvatarProto(avatar, collection));
        }
        else
        {
            // Wire parity: the official result ALWAYS echoes the fight roster; the client's mission battle-end
            // chain settles off it (a bare result left the prologue looping the same battle).
            try
            {
                proto.BattleAvatarList.AddRange(battle.BuildResultBattleAvatars());
            }
            catch
            {
                // A roster that cannot be rebuilt (stage row gone) must not swallow the result itself.
            }
        }

        SetData(proto);
    }
}
