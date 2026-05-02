using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.GameServer.Server.Packet.Send.SwitchHand;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.SwitchHand;

[Opcode(CmdIds.SwitchHandResetGameCsReq)]
public class HandlerSwitchHandResetGameCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SwitchHandResetGameCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.GetComponent<SwitchHandComponent>();
        var info = component.UpdateHandInfo(req.ResetHandInfo);

        if (info.Item2 == null)
            await connection.SendPacket(new PacketSwitchHandResetGameScRsp(info.Item1));
        else
            await connection.SendPacket(new PacketSwitchHandResetGameScRsp(info.Item2));
    }
}

