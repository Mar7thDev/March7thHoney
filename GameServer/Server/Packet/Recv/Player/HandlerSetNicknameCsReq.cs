using CastoriceHoney.Server.GameServer.Server.Packet.Send.PlayerSync;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.SetNicknameCsReq)]
public class HandlerSetNicknameCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;
        var req = SetNicknameCsReq.Parser.ParseFrom(data);
        if (req == null) return;
        player.Data.Name = req.Nickname;

        await connection.SendPacket(CmdIds.SetNicknameScRsp);
        await connection.SendPacket(new PacketPlayerSyncScNotify(player.ToProto()));
    }
}