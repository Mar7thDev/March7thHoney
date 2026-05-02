using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournTitanUpdateTitanBlessProgressScNotify : BasePacket
{
    public PacketRogueTournTitanUpdateTitanBlessProgressScNotify(RogueTournInstance inst) : base(
        CmdIds.RogueTournTitanUpdateTitanBlessProgressScNotify)
    {
        var proto = new RogueTournTitanUpdateTitanBlessProgressScNotify
        {
            TitanBlessProgress = (uint)inst.TitanProgress
        };

        SetData(proto);
    }
}