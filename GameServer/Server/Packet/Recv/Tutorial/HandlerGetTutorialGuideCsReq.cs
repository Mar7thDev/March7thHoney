using CastoriceHoney.Server.GameServer.Server.Packet.Send.Tutorial;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.GetTutorialGuideCsReq)]
public class HandlerGetTutorialGuideCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        if (ConfigManager.Config.ServerOption.EnableMission) // If missions are enabled
            await connection.SendPacket(new PacketGetTutorialGuideScRsp(connection.Player!)); // some bug
    }
}
