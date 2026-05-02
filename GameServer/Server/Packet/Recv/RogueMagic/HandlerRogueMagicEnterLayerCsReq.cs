using CastoriceHoney.Server.Enums.RogueMagic;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueMagic;

[Opcode(CmdIds.RogueMagicEnterLayerCsReq)]
public class HandlerRogueMagicEnterLayerCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueMagicEnterLayerCsReq.Parser.ParseFrom(data);
        var inst = connection.Player!.RogueMagicManager?.RogueMagicInstance;

        if (inst == null)
        {
            await connection.SendPacket(new PacketRogueMagicEnterLayerScRsp(Retcode.RetRogueStatusNotMatch, null));
            return;
        }

        await inst.EnterNextLayer((int)req.CurLevelIndex, (RogueMagicRoomTypeEnum)req.NextRoomType);
        await connection.SendPacket(new PacketRogueMagicEnterLayerScRsp(Retcode.RetSucc, inst));
    }
}