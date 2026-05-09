using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.TrainCakeCatch;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.TrainCakeCatch;

public class TrainCakeCatchManager(PlayerInstance player) : BasePlayerManager(player)
{
    public TrainCakeCatchData Data { get; } =
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<TrainCakeCatchData>(player.Uid);

    private static int GetConst(string name, int fallback)
    {
        return GameData.CakeCatchConstValueData.TryGetValue(name, out var entry)
               && entry.Value.IntValue is { } v
            ? v
            : fallback;
    }

    private static long WeekEndUnixSec()
    {
        var now = DateTimeOffset.Now;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        var weekEnd = now.Date.AddDays(daysUntilSunday).AddDays(1).AddSeconds(-1);
        return new DateTimeOffset(weekEnd, now.Offset).ToUnixTimeSeconds();
    }

    private static long NextDayUnixSec()
    {
        var now = DateTimeOffset.Now;
        var nextDay = now.Date.AddDays(1);
        return new DateTimeOffset(nextDay, now.Offset).ToUnixTimeSeconds();
    }

    public TrainCakeCatchGetDataScRsp ToGetDataProto()
    {
        var maxQuantity = GetConst("EveryCat_MaxQuantity", 13);
        var dailySearchMax = GetConst("CatCatch_Week_Max", 9);

        var rsp = new TrainCakeCatchGetDataScRsp
        {
            DHLGCEGONIB = WeekEndUnixSec(),
            PEOECDDANNP = BuildDiyProto(),
            FMECAHALPKG = new MKHIPFHEKCK
            {
                MFBDMLPDEOE = (uint)Player.Uid
            },
            PEPMFGLDGOH = new FAOPPAFAJLO(),
            HJOFGOINOAK = new LLKEMMMEPOB
            {
                EHLIFHPILPG = (uint)dailySearchMax,
                FIHMHJHGHOG = NextDayUnixSec()
            }
        };

        rsp.PerformanceIdList.AddRange(Data.PerformanceIds.Select(x => (uint)x));

        rsp.AFEJACMLOLH.AddRange(Data.CatTreeSlots.Select(x => new NPLEDHFMFDM
        {
            HCJPDNDOHAM = (uint)x.CreatureId,
            Slot = (uint)x.Slot
        }));

        foreach (var entry in GameData.CakeConfigData)
        {
            rsp.BLEDIOOJPNL.Add(new PKILNDNCAMI
            {
                HCJPDNDOHAM = (uint)entry.Key,
                Count = (uint)maxQuantity
            });
        }

        return rsp;
    }

    public FOFNOBHLKGO BuildDiyProto()
    {
        var diy = new FOFNOBHLKGO
        {
            MIPPCFJPJDD = (HEAJBHNMJGO)Data.DiyTheme
        };

        diy.OJNJEDBBAJJ.AddRange(Data.DiceSlots.Select(x =>
        {
            var msg = new BIAIKHBFALH
            {
                DiceSlotId = (uint)x.DiceSlotId,
                HGJHIJOGGIN = (uint)x.Index
            };
            msg.GBGDLNNOBID.AddRange(x.Values.Select(v => (uint)v));
            return msg;
        }));

        diy.LKPGMBDBJFA.AddRange(Data.StagePlacements.Select(x => new GFAHEGCPIEB
        {
            HCJPDNDOHAM = (uint)x.CreatureId,
            Slot = (uint)x.Slot
        }));

        return diy;
    }

    public FOFNOBHLKGO ApplyDiy(FOFNOBHLKGO req)
    {
        Data.DiyTheme = (int)req.MIPPCFJPJDD;

        Data.DiceSlots = req.OJNJEDBBAJJ.Select(x => new DiyDiceSlotInfo
        {
            DiceSlotId = (int)x.DiceSlotId,
            Index = (int)x.HGJHIJOGGIN,
            Values = x.GBGDLNNOBID.Select(v => (int)v).ToList()
        }).ToList();

        Data.StagePlacements = req.LKPGMBDBJFA.Select(x => new DiyStagePlacement
        {
            CreatureId = (int)x.HCJPDNDOHAM,
            Slot = (int)x.Slot
        }).ToList();

        DatabaseHelper.ToSaveUidList.Add(Player.Uid);
        return BuildDiyProto();
    }

    public void SetCatTreeSlots(IEnumerable<(int CreatureId, int Slot)> placements)
    {
        Data.CatTreeSlots = placements.Select(p => new CakeTreeSlotInfo
        {
            CreatureId = p.CreatureId,
            Slot = p.Slot
        }).ToList();
        DatabaseHelper.ToSaveUidList.Add(Player.Uid);
    }
}
