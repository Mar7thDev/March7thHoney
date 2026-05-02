using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketOpenChestScNotify : BasePacket
{
    public PacketOpenChestScNotify(int chestId) : base(CmdIds.OpenChestScNotify)
    {
        var proto = new OpenChestScNotify
        {
            ChestId = (uint)chestId
        };

        SetData(proto);
    }
}