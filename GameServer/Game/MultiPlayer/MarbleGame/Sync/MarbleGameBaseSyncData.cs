using March7thHoney.Enums.Fight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.MultiPlayer.MarbleGame.Sync;

public abstract class MarbleGameBaseSyncData(MarbleNetWorkMsgEnum type)
{
    public MarbleNetWorkMsgEnum MessageType { get; set; } = type;
    public abstract FightGameInfo ToProto();
}