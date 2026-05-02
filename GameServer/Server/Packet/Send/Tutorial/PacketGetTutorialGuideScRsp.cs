using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Tutorial;

public class PacketGetTutorialGuideScRsp : BasePacket
{
    public PacketGetTutorialGuideScRsp(PlayerInstance player) : base(CmdIds.GetTutorialGuideScRsp)
    {
        var proto = new GetTutorialGuideScRsp();
        foreach (var data in player.TutorialGuideData?.Tutorials ?? [])
            proto.TutorialGuideList.Add(new TutorialGuide
            {
                Id = (uint)data.Key,
                Status = data.Value
            });

        SetData(proto);
    }
}