using CastoriceHoney.Server.GameServer.Game.RogueMagic;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueCommon;

[Opcode(CmdIds.PrepareRogueAdventureRoomCsReq)]
public class HandlerPrepareRogueAdventureRoomCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var instance = connection.Player?.RogueManager?.GetRogueInstance();

        if (instance == null)
        {
            await connection.SendPacket(new PacketPrepareRogueAdventureRoomScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        if (instance is not RogueMagicInstance magic)
        {
            await connection.SendPacket(new PacketPrepareRogueAdventureRoomScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        var inst = magic.CurLevel?.CurRoom?.AdventureInstance;
        if (inst == null)
        {
            await connection.SendPacket(new PacketPrepareRogueAdventureRoomScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        inst.Prepare();
        await connection.SendPacket(new PacketPrepareRogueAdventureRoomScRsp(inst));
    }
}