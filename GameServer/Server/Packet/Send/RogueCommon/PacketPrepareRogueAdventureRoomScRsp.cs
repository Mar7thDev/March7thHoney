using CastoriceHoney.Server.GameServer.Game.RogueMagic.Adventure;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

public class PacketPrepareRogueAdventureRoomScRsp : BasePacket
{
    public PacketPrepareRogueAdventureRoomScRsp(RogueMagicAdventureInstance instance) : base(
        CmdIds.PrepareRogueAdventureRoomScRsp)
    {
        var proto = new PrepareRogueAdventureRoomScRsp
        {
            AdventureRoomInfo = instance.ToProto()
        };

        SetData(proto);
    }


    public PacketPrepareRogueAdventureRoomScRsp(Retcode retcode) : base(CmdIds.PrepareRogueAdventureRoomScRsp)
    {
        var proto = new PrepareRogueAdventureRoomScRsp
        {
            Retcode = (uint)retcode
        };

        SetData(proto);
    }
}