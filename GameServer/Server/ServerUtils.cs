using March7thHoney.GameServer.Game.Mission;

namespace March7thHoney.GameServer.Server;

public static class ServerUtils
{
    public static void InitializeHandlers()
    {
        // Mission finish-action / finish-type handlers, populated by the source-generated
        // GeneratedMissionHandlers (MissionHandlerRegistryGenerator) — no reflection (NativeAOT safe).
        GeneratedMissionHandlers.Register();
    }
}
