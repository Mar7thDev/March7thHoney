using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Raid;

public class PacketDelSaveRaidScNotify : BasePacket
{
    public PacketDelSaveRaidScNotify(int raidId, int worldLevel) : base(CmdIds.DelSaveRaidScNotify)
    {
        var proto = new DelSaveRaidScNotify
        {
            RaidId = (uint)raidId,
            WorldLevel = (uint)worldLevel
        };

        SetData(proto);
    }
}