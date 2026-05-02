using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueMagic;

[Opcode(CmdIds.RogueMagicStartCsReq)]
public class HandlerRogueMagicStartCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueMagicStartCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        var areaId = (int)req.AreaId;
        var styleType = (int)req.SelectStyleType;
        var difficulty = req.StartDifficultyIdList.Select(x => (int)x).ToList();
        var avatars = req.BaseAvatarIdList.Select(x => (int)x).ToList();

        var rsp = await player.RogueMagicManager!.StartRogueMagic(avatars, areaId, styleType, difficulty);
        await connection.SendPacket(new PacketRogueMagicStartScRsp(rsp.Item1, rsp.Item2));
    }
}