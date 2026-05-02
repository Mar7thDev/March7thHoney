using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.GameServer.Server.Packet.Send.SwitchHand;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.SwitchHand;

[Opcode(CmdIds.SwitchHandCoinUpdateCsReq)]
public class HandlerSwitchHandCoinUpdateCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SwitchHandCoinUpdateCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.GetComponent<SwitchHandComponent>();
        var info = component.GetHandInfo(component.RunningHandConfigId);
        if (info.Item2 == null)
        {
            await connection.SendPacket(new PacketSwitchHandCoinUpdateScRsp(info.Item1));
        }
        else
        {
            info.Item2.CoinNum = (int)req.CKNFABPOMBL;
            await connection.SendPacket(new PacketSwitchHandCoinUpdateScRsp(req.CKNFABPOMBL));
        }
    }
}

