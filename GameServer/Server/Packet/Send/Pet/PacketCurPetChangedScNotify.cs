using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Pet;

public class PacketCurPetChangedScNotify : BasePacket
{
    public PacketCurPetChangedScNotify(uint newPetId) : base(CmdIds.CurPetChangedScNotify)
    {
        var proto = new CurPetChangedScNotify
        {
            CurPetId = newPetId
        };

        SetData(proto);
    }
}