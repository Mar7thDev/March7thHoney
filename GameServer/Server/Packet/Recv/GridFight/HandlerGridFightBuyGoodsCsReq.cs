using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightBuyGoodsCsReq)]
public class HandlerGridFightBuyGoodsCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightBuyGoodsCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        uint roleId = 0, roleUniqueId = 0, pos = 14;
        int goldDelta = 0;
        if (inst != null && req.BuyGoodsIndexList.Count > 0)
        {
            var idx = (int)req.BuyGoodsIndexList[0];
            var (ok, uid, rid, addedPos) = inst.TryBuyGoods(idx);
            if (ok)
            {
                roleId = rid;
                roleUniqueId = uid;
                pos = addedPos == 0 ? 14u : addedPos;
                goldDelta = -1;
            }
        }

        var rsp = new BasePacket((ushort)CmdIds.CEFIMADBIBH);
        rsp.SetData(new CEFIMADBIBH());
        await connection.SendPacket(rsp);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.BuyGoods, (roleId, roleUniqueId, pos, goldDelta)));
    }
}
