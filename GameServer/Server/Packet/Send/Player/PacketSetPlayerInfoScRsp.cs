using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketSetPlayerInfoScRsp : BasePacket
{
    public PacketSetPlayerInfoScRsp(PlayerInstance player, bool IsModify) : base(CmdIds.SetPlayerInfoScRsp)
    {
        var proto = new SetPlayerInfoScRsp
        {
            CurAvatarPath = (MultiPathAvatarType)player.Data.CurBasicType,
            IsModify = IsModify
        };

        SetData(proto);
    }
}