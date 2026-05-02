using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightSettleNotify : BasePacket
{
    public PacketGridFightSettleNotify(GridFightInstance inst) : base(CmdIds.GridFightSettleNotify)
    {
        var divisionId = GameData.GridFightDivisionInfoData.Where(x => x.Value.SeasonID == GridFightManager.CurSeasonId)
            .Select(x => x.Key).Max();

        var proto = new GridFightSettleNotify
        {
            CurDivisionId = divisionId,
            PrevDivisionId = divisionId,
            TournFinishInfo = inst.ToFinishInfo()
        };

        SetData(proto);
    }
}