using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Tutorial;

public class PacketGetTutorialScRsp : BasePacket
{
    public PacketGetTutorialScRsp(PlayerInstance player) : base(CmdIds.GetTutorialScRsp)
    {
        var proto = new GetTutorialScRsp();

        foreach (var item in player.TutorialData!.Tutorials)
            proto.TutorialList.Add(new Proto.Tutorial
            {
                Id = (uint)item.Key,
                Status = item.Value
            });
        SetData(proto);
    }
}