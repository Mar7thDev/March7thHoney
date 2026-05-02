using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Activity;

public class PacketGetTrialActivityDataScRsp : BasePacket
{
    public PacketGetTrialActivityDataScRsp(PlayerInstance player) : base(CmdIds.GetTrialActivityDataScRsp)
    {
        var proto = new GetTrialActivityDataScRsp();
        proto.TrialActivityInfoList.Add(player.ActivityManager!.Data.TrialActivityData.ToProto());
        SetData(proto);
    }
}