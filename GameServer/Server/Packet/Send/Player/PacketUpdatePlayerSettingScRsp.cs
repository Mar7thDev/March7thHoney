using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketUpdatePlayerSettingScRsp : BasePacket
{
    public PacketUpdatePlayerSettingScRsp(UpdatePlayerSetting setting) : base(CmdIds.UpdatePlayerSettingScRsp)
    {
        var proto = new UpdatePlayerSettingScRsp
        {
            PlayerSetting = setting
        };

        SetData(proto);
    }
}