using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournGetPermanentTalentInfoScRsp : BasePacket
{
    public PacketRogueTournGetPermanentTalentInfoScRsp(PlayerInstance player) : base(
        CmdIds.RogueTournGetPermanentTalentInfoScRsp)
    {
        var proto = new RogueTournGetPermanentTalentInfoScRsp
        {
            PermanentInfo = player.RogueTournManager!.ToPermanentTalentProto()
        };

        SetData(proto);
    }
}