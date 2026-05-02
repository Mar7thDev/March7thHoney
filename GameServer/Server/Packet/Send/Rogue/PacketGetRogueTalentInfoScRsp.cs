using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

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