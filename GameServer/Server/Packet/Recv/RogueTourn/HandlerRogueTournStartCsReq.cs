using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.RogueTournStartCsReq)]
public class HandlerRogueTournStartCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueTournStartCsReq.Parser.ParseFrom(data);

        var avatars = req.BaseAvatarIdList.Select(x => (int)x);
        var area = (int)req.AreaId;

        var player = connection.Player!;
        var rsp = await player.RogueTournManager!.StartRogueTourn(avatars.ToList(), area);
        await connection.SendPacket(new PacketRogueTournStartScRsp(rsp.Item1, rsp.Item2));
    }
}