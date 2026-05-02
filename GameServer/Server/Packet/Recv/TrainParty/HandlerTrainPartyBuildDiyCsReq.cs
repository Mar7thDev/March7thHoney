using CastoriceHoney.Server.GameServer.Server.Packet.Send.TrainParty;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.TrainParty;

[Opcode(CmdIds.TrainPartyBuildDiyCsReq)]
public class HandlerTrainPartyBuildDiyCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = TrainPartyBuildDiyCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        var area = player.TrainPartyManager!.SetDynamicId((int)req.AreaId, (int)req.DiceSlotId, (int)req.DiyDynamicId);
        await player.SendPacket(new PacketTrainPartyBuildDiyScRsp(area));
    }
}