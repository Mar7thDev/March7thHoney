using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Pet;

public class PacketPetDataChangeScNotify : BasePacket
{
    public PacketPetDataChangeScNotify(PetDataChangeScNotify notify) : base(CmdIds.PetDataChangeScNotify)
    {
        SetData(notify);
    }

    /// <summary>编队变化：change_list 里一条 blind_box_lineup（DFNCMMIMKHG）。</summary>
    public PacketPetDataChangeScNotify(CMBGLKEMOGF lineup) : base(CmdIds.PetDataChangeScNotify)
    {
        var proto = new PetDataChangeScNotify();
        proto.DGNGMJMPFGK.Add(new CBHJJCBDJHA { DFNCMMIMKHG = lineup });
        SetData(proto);
    }
}
