using CastoriceHoney.Server.GameServer.Game.RogueMagic.Adventure;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

public class PacketStopRogueAdventureRoomScRsp : BasePacket
{
    public PacketStopRogueAdventureRoomScRsp(RogueMagicAdventureInstance instance) : base(
        CmdIds.StopRogueAdventureRoomScRsp)
    {
        var proto = new StopRogueAdventureRoomScRsp
        {
            AdventureRoomInfo = instance.ToProto()
        };

        SetData(proto);
    }

    public PacketStopRogueAdventureRoomScRsp(Retcode ret) : base(
        CmdIds.StopRogueAdventureRoomScRsp)
    {
        var proto = new StopRogueAdventureRoomScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}