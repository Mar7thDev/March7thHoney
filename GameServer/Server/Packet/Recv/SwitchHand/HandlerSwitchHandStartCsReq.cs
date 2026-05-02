using CastoriceHoney.Server.GameServer.Game.Player.Components;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.SwitchHand;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.SwitchHand;

[Opcode(CmdIds.SwitchHandStartCsReq)]
public class HandlerSwitchHandStartCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SwitchHandStartCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.GetComponent<SwitchHandComponent>();
        component.RunningHandConfigId = (int)req.ConfigId;

        await connection.SendPacket(new PacketSwitchHandStartScRsp(req.ConfigId));
    }
}