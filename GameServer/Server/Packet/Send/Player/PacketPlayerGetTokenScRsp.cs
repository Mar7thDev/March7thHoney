using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;
using CastoriceHoney.Server.Util.Security;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketPlayerGetTokenScRsp : BasePacket
{
    public PacketPlayerGetTokenScRsp(Connection connection) : base(CmdIds.PlayerGetTokenScRsp)
    {
        var rsp = new PlayerGetTokenScRsp
        {
            BlackInfo = new BlackInfo(),
            Uid = (uint)(connection.Player?.Uid ?? 0)
        };

        if (ConfigManager.Config.GameServer.UsePacketEncryption)
        {
            var tempRandom = new MT19937((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
            rsp.SecretKeySeed = connection.ClientSecretKeySeed = tempRandom.NextUInt64();
        }

        SetData(rsp);
    }

    public PacketPlayerGetTokenScRsp(uint uid, Retcode ret, BlackInfo? black = null, string msg = "") : base(
        CmdIds.PlayerGetTokenScRsp)
    {
        var rsp = new PlayerGetTokenScRsp
        {
            Retcode = (uint)ret,
            BlackInfo = black ?? new BlackInfo(),
            Uid = uid
        };

        SetData(rsp);
    }
}

