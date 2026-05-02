using CastoriceHoney.Server.Enums.Fight;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.MultiPlayer.MarbleGame.Sync;

public abstract class MarbleGameBaseSyncData(MarbleNetWorkMsgEnum type)
{
    public MarbleNetWorkMsgEnum MessageType { get; set; } = type;
    public abstract FightGameInfo ToProto();
}