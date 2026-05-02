using CastoriceHoney.Server.GameServer.Game.RogueTourn;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

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