using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Pet;

public class PacketSummonPetScRsp : BasePacket
{
    public PacketSummonPetScRsp(EHDNPMEKPCA curPet) : base(CmdIds.SummonPetScRsp)
    {
        var proto = new SummonPetScRsp
        {
            PLKBLNMOOPH = curPet
        };

        SetData(proto);
    }
}
