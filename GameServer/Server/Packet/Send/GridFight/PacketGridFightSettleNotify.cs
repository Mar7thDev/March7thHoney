using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

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