using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

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