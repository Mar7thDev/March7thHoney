using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Pet;

public class PacketRecallPetScRsp : BasePacket
{
    public PacketRecallPetScRsp() : base(CmdIds.RecallPetScRsp)
    {
        // 官服收回后的回包带一个空的 cur_pet。
        var proto = new RecallPetScRsp
        {
            PLKBLNMOOPH = new EHDNPMEKPCA()
        };

        SetData(proto);
    }
}
