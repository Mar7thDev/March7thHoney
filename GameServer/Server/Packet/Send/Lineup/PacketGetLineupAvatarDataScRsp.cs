using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lineup;

public class PacketGetLineupAvatarDataScRsp : BasePacket
{
    public PacketGetLineupAvatarDataScRsp(PlayerInstance player) : base(CmdIds.GetLineupAvatarDataScRsp)
    {
        var rsp = new GetLineupAvatarDataScRsp();

        player.AvatarManager?.AvatarData?.FormalAvatars?.ForEach(avatar =>
        {
            var data = new LineupAvatarData
            {
                Id = (uint)avatar.AvatarId,
                Hp = (uint)avatar.CurrentHp,
                AvatarType = AvatarType.AvatarFormalType
            };
            rsp.AvatarDataList.Add(data);
        });

        SetData(rsp);
    }
}