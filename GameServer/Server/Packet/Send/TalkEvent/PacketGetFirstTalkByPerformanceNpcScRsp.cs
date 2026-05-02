using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.TalkEvent;

public class PacketGetFirstTalkByPerformanceNpcScRsp : BasePacket
{
    public PacketGetFirstTalkByPerformanceNpcScRsp(GetFirstTalkByPerformanceNpcCsReq req) : base(
        CmdIds.GetFirstTalkByPerformanceNpcScRsp)
    {
        var rsp = new GetFirstTalkByPerformanceNpcScRsp();

        foreach (var id in req.PerformanceIdList)
            rsp.NpcMeetStatusList.Add(new NpcMeetByPerformanceStatus
            {
                PerformanceId = id
            });

        SetData(rsp);
    }
}