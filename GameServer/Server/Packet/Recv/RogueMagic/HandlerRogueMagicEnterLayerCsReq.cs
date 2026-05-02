using March7thHoney.Enums.RogueMagic;
using March7thHoney.GameServer.Server.Packet.Send.RogueMagic;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueMagic;

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