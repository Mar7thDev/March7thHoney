using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Battle;

public class PacketGetFarmStageGachaInfoScRsp : BasePacket
{
    public PacketGetFarmStageGachaInfoScRsp(GetFarmStageGachaInfoCsReq req) : base(CmdIds.GetFarmStageGachaInfoScRsp)
    {
        var proto = new GetFarmStageGachaInfoScRsp();

        foreach (var item in req.FarmStageGachaIdList)
            proto.FarmStageGachaInfoList.Add(new FarmStageGachaInfo
            {
                GachaId = item,
                BeginTime = 0,
                EndTime = Extensions.GetUnixSec() + 3600
            });

        SetData(proto);
    }
}