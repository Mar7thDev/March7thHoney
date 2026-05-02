using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketEnhanceRogueBuffScRsp : BasePacket
{
    public PacketEnhanceRogueBuffScRsp(uint buffId) : base(CmdIds.EnhanceRogueBuffScRsp)
    {
        var proto = new EnhanceRogueBuffScRsp
        {
            RogueBuff = new RogueBuff
            {
                BuffId = buffId,
                Level = 2
            },
            IsSucc = true
        };

        SetData(proto);
    }
}