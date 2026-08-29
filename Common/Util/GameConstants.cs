namespace March7thHoney.Util;

public static class GameConstants
{
    // 版本号规则：x.y.5 等价于客户端 x.(y+1).0 —— 末位 .5 就是下一个版本的测试版。
    // 所以 4.4.5 = 客户端 4.5.0，4.5.5 = 客户端 4.6.0。中间号（4.4.51/52/53 之类）互不兼容，直接跳过，
    // 因此版本号相近并不代表协议可以互换：换版本必须同时换 Proto/Generated 与 KcpSharp/CmdIds.cs。
    // 当前服务端面向客户端 4.5.0，协议取自 Proto/4.5v3 dump。
    public const string GAME_VERSION = "4.4.5";
    public const string AvatarDbVersion = "20250430";
    public const int GameVersionInt = 4450;
    public const int MAX_STAMINA = 300;
    public const int MAX_STAMINA_RESERVE = 2400;
    public const int STAMINA_RECOVERY_TIME = 360; // 6 minutes
    public const int STAMINA_RESERVE_RECOVERY_TIME = 1080; // 18 minutes
    public const int INVENTORY_MAX_EQUIPMENT = 1500;
    public const int INVENTORY_MAX_RELIC = 3000;
    public const int INVENTORY_MAX_MATERIAL = 2000;
    public const int MAX_LINEUP_COUNT = 9;
    public const int LAST_TRAIN_WORLD_ID = 501;
    public const int AMBUSH_BUFF_ID = 1000102;
    public const int CHALLENGE_ENTRANCE = 100000103;
    public const int CHALLENGE_PEAK_ENTRANCE = 100000352;
    public const int CHALLENGE_STORY_ENTRANCE = 102020107;
    public const int CHALLENGE_BOSS_ENTRANCE = 1030402;
    public const int CURRENT_ROGUE_TOURN_SEASON = 2;

    public const uint CHALLENGE_PEAK_BRONZE_FRAME_ID = 226001;
    public const uint CHALLENGE_PEAK_SILVER_FRAME_ID = 226002;
    public const uint CHALLENGE_PEAK_GOLD_FRAME_ID = 226003;
    public const uint CHALLENGE_PEAK_ULTRA_FRAME_ID = 226004;

    public const uint CHALLENGE_PEAK_CUR_GROUP_ID = 1;
    public static Dictionary<uint, List<uint>> CHALLENGE_PEAK_TARGET_ENTRY_ID = new()
    {
        {1, [3013501, 8]}
    };

    public static readonly List<int> UpgradeWorldLevel = [20, 30, 40, 50, 60, 65];
    public static readonly List<int> AllowedChessRogueEntranceId = [8020701, 8020901, 8020401, 8020201];
}
