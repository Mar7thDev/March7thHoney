using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.EraFlipper;

public class PacketChangeEraFlipperDataScRsp : BasePacket
{
    public PacketChangeEraFlipperDataScRsp(ChangeEraFlipperDataCsReq req) : base(CmdIds.ChangeEraFlipperDataScRsp)
    {
        var proto = new ChangeEraFlipperDataScRsp
        {
            Data = req.Data
        };

        SetData(proto);
    }

    public PacketChangeEraFlipperDataScRsp(Retcode code) : base(CmdIds.ChangeEraFlipperDataScRsp)
    {
        var proto = new ChangeEraFlipperDataScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}