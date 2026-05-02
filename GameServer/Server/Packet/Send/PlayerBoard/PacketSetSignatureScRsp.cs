using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.PlayerBoard;

public class PacketSetSignatureScRsp : BasePacket
{
    public PacketSetSignatureScRsp(string signature) : base(CmdIds.SetSignatureScRsp)
    {
        var proto = new SetSignatureScRsp
        {
            Signature = signature
        };

        SetData(proto);
    }
}