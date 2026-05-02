using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.GameServer.Server.Packet.Send.SwitchHand;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.SwitchHand;

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