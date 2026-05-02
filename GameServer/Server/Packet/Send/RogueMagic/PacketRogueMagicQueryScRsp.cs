using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagicQueryScRsp : BasePacket
{
    public PacketRogueMagicQueryScRsp(PlayerInstance player) : base(CmdIds.RogueMagicQueryScRsp)
    {
        var proto = new RogueMagicQueryScRsp
        {
            RogueGetInfo = player.RogueMagicManager!.ToGetInfo()
        };

        SetData(proto);
    }
}