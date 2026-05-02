using March7thHoney.Enums.Fight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.MultiPlayer.MarbleGame.Sync;

public class MarblePerformanceSyncData(MarbleNetWorkMsgEnum type) : MarbleGameBaseSyncData(type)
{
    public override FightGameInfo ToProto()
    {
        return new FightGameInfo
        {
            GameMessageType = (uint)MessageType,
            MarbleGameSyncInfo = new MarbleGameSyncInfo
            {
                MarbleSyncType = MarbleSyncType.Performance
            }
        };
    }
}