using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUseOrbNotify : BasePacket
{
    public PacketGridFightUseOrbNotify(uint uniqueId, List<GridFightDropItemInfo> drops) : base(CmdIds.GridFightUseOrbNotify)
    {
        var proto = new GridFightUseOrbNotify
        {
            DropItemList = new GridFightDropInfo
            {
                DropItemList = { drops }
            },
            OrbUniqueId = uniqueId
        };

        SetData(proto);
    }
}