using CastoriceHoney.Server.Enums.Fight;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.MultiPlayer.MarbleGame.Sync;

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