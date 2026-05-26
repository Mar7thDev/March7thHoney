using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("ChallengeMazeTierceConfig.json")]
public class ChallengeMazeTierceConfigExcel : ExcelResource
{
    public int ID { get; set; }
    public int PreChallengeMazeID { get; set; }
    public int MapEntranceID { get; set; }
    public int MazeGroupID { get; set; }

    public List<int> ConfigList { get; set; } = [];
    public List<int> NpcMonsterIDList { get; set; } = [];
    public List<int> EventIDList { get; set; } = [];
    public List<string> DamageType { get; set; } = [];

    [JsonProperty("GNOOAGPBNLD")]
    public int ChallengeCountDown { get; set; }

    public List<int> ChallengeTargetID { get; set; } = [];
    public int ChallengeTierceTargetID { get; set; }
    public int RewardID { get; set; }

    [JsonIgnore]
    public Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> ChallengeMonsters { get; } = new();

    public override int GetId() => ID;

    public override void Loaded()
    {
        for (var i = 0; i < ConfigList.Count; i++)
        {
            if (ConfigList[i] == 0) break;
            if (i >= NpcMonsterIDList.Count || i >= EventIDList.Count) break;

            var monster = new ChallengeConfigExcel.ChallengeMonsterInfo(ConfigList[i], NpcMonsterIDList[i], EventIDList[i]);
            ChallengeMonsters.TryAdd(MazeGroupID, []);
            ChallengeMonsters[MazeGroupID].Add(monster);
        }

        GameData.ChallengeMazeTierceConfigData.TryAdd(ID, this);
    }
}
