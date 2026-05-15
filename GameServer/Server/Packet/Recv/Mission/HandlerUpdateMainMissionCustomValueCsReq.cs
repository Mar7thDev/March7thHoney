using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.UpdateMainMissionCustomValueCsReq)]
public class HandlerUpdateMainMissionCustomValueCsReq : Handler
{
    public override Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = UpdateMainMissionCustomValueCsReq.Parser.ParseFrom(data);
        connection.Player!.MissionManager!.SetMainMissionCustomValue((int)req.MainMissionId, req.DLCLDIAOIMK,
            req.Value);
        return Task.CompletedTask;
    }
}
