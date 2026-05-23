using March7thHoney.Command;
using March7thHoney.GameServer.Game.Battle;
// TODO 4.3: Challenge.Definitions 暂停
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Proto;
using LineupInfo = March7thHoney.Database.Lineup.LineupInfo;

namespace March7thHoney.GameServer.Plugin.Event;

public static class PluginEvent
{
    public static void InvokeOnPlayerHeartBeat(PlayerInstance player)
    {
        OnPlayerHeartBeat?.Invoke(player);
    }

    public static void InvokeOnPlayerLogin(PlayerInstance player)
    {
        OnPlayerLogin?.Invoke(player);
    }

    public static void InvokeOnPlayerLogout(PlayerInstance player)
    {
        OnPlayerLogout?.Invoke(player);
    }

    public static void InvokeOnPlayerFinishSubMission(PlayerInstance player, int missionId)
    {
        OnPlayerFinishSubMission?.Invoke(player, missionId);
    }

    public static void InvokeOnPlayerFinishMainMission(PlayerInstance player, int missionId)
    {
        OnPlayerFinishMainMission?.Invoke(player, missionId);
    }

    public static void InvokeOnPlayerInteract(PlayerInstance player, EntityProp prop)
    {
        OnPlayerInteract?.Invoke(player, prop);
    }

    public static void InvokeOnPlayerLoadScene(PlayerInstance player, SceneInstance scene)
    {
        OnPlayerLoadScene?.Invoke(player, scene);
    }

    public static void InvokeOnPlayerEnterBattle(PlayerInstance player, BattleInstance battle)
    {
        OnPlayerEnterBattle?.Invoke(player, battle);
    }

    public static void InvokeOnPlayerQuitBattle(PlayerInstance player, PVEBattleResultCsReq result)
    {
        OnPlayerQuitBattle?.Invoke(player, result);
    }

    // TODO 4.3: OnPlayerEnterChallenge / OnPlayerQuitChallenge 依赖 BaseChallengeInstance（已暂停）

    public static void InvokeOnPlayerSyncLineup(PlayerInstance player, LineupInfo? lineup)
    {
        OnPlayerSyncLineup?.Invoke(player, lineup);
    }

    public static void InvokeOnPlayerUseCommand(ICommandSender sender, string command)
    {
        OnPlayerUseCommand?.Invoke(sender, command);
    }

    #region Player

    public delegate void OnPlayerHeartBeatHandler(PlayerInstance player);

    public delegate void OnPlayerLoginHandler(PlayerInstance player);

    public delegate void OnPlayerLogoutHandler(PlayerInstance player);

    public delegate void OnPlayerFinishSubMissionHandler(PlayerInstance player, int missionId);

    public delegate void OnPlayerFinishMainMissionHandler(PlayerInstance player, int missionId);

    public delegate void OnPlayerInteractHandler(PlayerInstance player, EntityProp prop);

    public delegate void OnPlayerLoadSceneHandler(PlayerInstance player, SceneInstance scene);

    public delegate void OnPlayerEnterBattleHandler(PlayerInstance player, BattleInstance battle);

    public delegate void OnPlayerQuitBattleHandler(PlayerInstance player, PVEBattleResultCsReq result);

    // TODO 4.3: OnPlayerEnterChallengeHandler / OnPlayerQuitChallengeHandler 暂停

    public delegate void OnPlayerSyncLineupHandler(PlayerInstance player, LineupInfo? lineup);

    public delegate void OnPlayerUseCommandHandler(ICommandSender sender, string command);

    #endregion

    #region Event

    public static event OnPlayerHeartBeatHandler? OnPlayerHeartBeat;
    public static event OnPlayerLoginHandler? OnPlayerLogin;
    public static event OnPlayerLogoutHandler? OnPlayerLogout;
    public static event OnPlayerFinishSubMissionHandler? OnPlayerFinishSubMission;
    public static event OnPlayerFinishMainMissionHandler? OnPlayerFinishMainMission;
    public static event OnPlayerInteractHandler? OnPlayerInteract;
    public static event OnPlayerLoadSceneHandler? OnPlayerLoadScene;
    public static event OnPlayerEnterBattleHandler? OnPlayerEnterBattle;
    // TODO 4.3: OnPlayerEnterChallenge event 暂停
    public static event OnPlayerQuitBattleHandler? OnPlayerQuitBattle;
    // TODO 4.3: OnPlayerQuitChallenge event 暂停
    public static event OnPlayerSyncLineupHandler? OnPlayerSyncLineup;
    public static event OnPlayerUseCommandHandler? OnPlayerUseCommand;

    #endregion
}
