using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

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