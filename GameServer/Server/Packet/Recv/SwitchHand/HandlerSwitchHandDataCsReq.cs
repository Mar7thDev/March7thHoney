using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.GameServer.Server.Packet.Send.SwitchHand;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.SwitchHand;

[Opcode(CmdIds.SwitchHandDataCsReq)]
public class HandlerSwitchHandDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SwitchHandDataCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.GetComponent<SwitchHandComponent>();
        if (req.ConfigId != 0)
        {
            var info = component.GetHandInfo((int)req.ConfigId);
            if (info.Item2 == null)
                await connection.SendPacket(new PacketSwitchHandDataScRsp(info.Item1));
            else
                await connection.SendPacket(new PacketSwitchHandDataScRsp(info.Item2));
        }
        else
        {
            var infos = component.GetHandInfos();
            await connection.SendPacket(new PacketSwitchHandDataScRsp(infos));
        }
    }
}