using March7thHoney.GameServer.Game.RogueMagic.Adventure;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

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