using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Pet;

public class PacketSummonPetScRsp : BasePacket
{
    public PacketSummonPetScRsp(int curPetId, uint newPetId) : base(CmdIds.SummonPetScRsp)
    {
        var proto = new SummonPetScRsp
        {
            CurPetId = (uint)curPetId,
            SelectPetId = newPetId
        };

        SetData(proto);
    }
}