using March7thHoney.GameServer.Server.Packet.Send.Gacha;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Gacha;

[Opcode(CmdIds.SetGachaDecideItemCsReq)]
public class HandlerSetGachaDecideItemCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetGachaDecideItemCsReq.Parser.ParseFrom(data);
        var manager = connection.Player!.GachaManager!;
        var retcode = manager.SetCharacterEventNonFeaturedPool((int)req.GachaId, (int)req.MBOEFLAHLEM,
            req.DGOMHDMJHEK);
        var savedOrder = retcode == Retcode.RetSucc
            ? manager.GetCharacterEventNonFeaturedPool().Select(id => (uint)id).ToList()
            : req.DGOMHDMJHEK.ToList();

        await connection.SendPacket(new PacketSetGachaDecideItemScRsp(req.GachaId, req.MBOEFLAHLEM, savedOrder,
            retcode));
    }
}

