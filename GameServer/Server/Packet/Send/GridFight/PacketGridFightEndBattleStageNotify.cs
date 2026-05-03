using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightEndBattleStageNotify : BasePacket
{
    public PacketGridFightEndBattleStageNotify() : base(CmdIds.GridFightEndBattleStageNotify)
    {
        SetData(new GridFightEndBattleStageNotify());
    }
}
