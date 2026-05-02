using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

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