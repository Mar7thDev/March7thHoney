using CastoriceHoney.Server.GameServer.Game.Rogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketGetRogueTalentInfoScRsp : BasePacket
{
    public PacketGetRogueTalentInfoScRsp() : base(CmdIds.GetRogueTalentInfoScRsp)
    {
        var proto = new GetRogueTalentInfoScRsp
        {
            TalentInfoList = RogueManager.ToTalentProto()
        };

        SetData(proto);
    }
}