using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.GameServer.Server.Packet.Send.RogueCommon;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueCommon;

[Opcode(CmdIds.StopRogueAdventureRoomCsReq)]
public class HandlerStopRogueAdventureRoomCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StopRogueAdventureRoomCsReq.Parser.ParseFrom(data);
        var instance = connection.Player?.RogueManager?.GetRogueInstance();

        if (instance == null)
        {
            await connection.SendPacket(new PacketStopRogueAdventureRoomScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        if (instance is not RogueMagicInstance magic)
        {
            await connection.SendPacket(new PacketStopRogueAdventureRoomScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        var inst = magic.CurLevel?.CurRoom?.AdventureInstance;
        if (inst == null)
        {
            await connection.SendPacket(new PacketStopRogueAdventureRoomScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        inst.Status = RogueAdventureRoomStatus.Stopped;
        await magic.HandleStopWolfGunAdventure(req.HitTargetIndexList.Select(x => (int)x).ToList(), inst);
        await connection.SendPacket(new PacketStopRogueAdventureRoomScRsp(inst));
    }
}