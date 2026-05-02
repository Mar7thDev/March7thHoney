using CastoriceHoney.Server.GameServer.Server.Packet.Send.ContentPackage;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.ContentPackage;

[Opcode(CmdIds.ContentPackageGetDataCsReq)]
public class HandlerContentPackageGetDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = ContentPackageGetDataCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(
            new PacketContentPackageGetDataScRsp()); // cause crash (not only SR but also ur PC(or other program) 
    }
}