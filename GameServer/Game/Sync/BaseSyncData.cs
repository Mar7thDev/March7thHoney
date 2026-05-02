using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Sync;

public abstract class BaseSyncData
{
    public abstract void SyncData(in PlayerSyncScNotify notify);
}