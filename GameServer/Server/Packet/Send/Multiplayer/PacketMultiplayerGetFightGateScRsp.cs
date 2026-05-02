using CastoriceHoney.Server.GameServer.Game.MultiPlayer;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Multiplayer;

public class PacketMultiplayerGetFightGateScRsp : BasePacket
{
    public PacketMultiplayerGetFightGateScRsp(Retcode code) : base(CmdIds.MultiplayerGetFightGateScRsp)
    {
        var proto = new MultiplayerGetFightGateScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }

    public PacketMultiplayerGetFightGateScRsp(BaseMultiPlayerGameRoomInstance room) : base(
        CmdIds.MultiplayerGetFightGateScRsp)
    {
        var proto = new MultiplayerGetFightGateScRsp
        {
            GateRoomId = (ulong)room.RoomId,
            Ip = ConfigManager.Config.GameServer.PublicAddress,
            Port = ConfigManager.Config.GameServer.Port
        };

        SetData(proto);
    }
}