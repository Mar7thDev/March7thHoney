using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketGetRogueBuffEnhanceInfoScRsp : BasePacket
{
    public PacketGetRogueBuffEnhanceInfoScRsp(PlayerInstance player) : base(CmdIds.GetRogueBuffEnhanceInfoScRsp)
    {
        var proto = new GetRogueBuffEnhanceInfoScRsp();
        if (player.RogueManager!.GetRogueInstance() == null)
        {
            proto.Retcode = 1;
            SetData(proto);
            return;
        }

        proto.BuffEnhanceInfo = player.RogueManager.GetRogueInstance()!.ToEnhanceInfo();

        SetData(proto);
    }
}