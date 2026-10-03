using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Pet;

public class PacketSetBlindBoxPetLineupScRsp : BasePacket
{
    public PacketSetBlindBoxPetLineupScRsp(CMBGLKEMOGF lineup) : base(CmdIds.SetBlindBoxPetLineupScRsp)
    {
        var proto = new SetBlindBoxPetLineupScRsp
        {
            LCFKGCJCHBO = lineup
        };

        SetData(proto);
    }

    public PacketSetBlindBoxPetLineupScRsp(Retcode retcode) : base(CmdIds.SetBlindBoxPetLineupScRsp)
    {
        SetData(new SetBlindBoxPetLineupScRsp
        {
            Retcode = (uint)retcode
        });
    }
}
