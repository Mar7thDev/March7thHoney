using March7thHoney.Database;
using March7thHoney.Database.TrainParty;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.TrainParty;

public class TrainPartyManager(PlayerInstance player) : BasePlayerManager(player)
{
    public TrainData Data { get; } =
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<TrainData>(player.Uid);

    public void EnsureDefaults()
    {
        // no-op
    }

    public TrainAreaInfo? SetDynamicId(int areaId, int slotId, int dynamicId)
    {
        return null;
    }

    public TrainPartyData ToProto()
    {
        return new TrainPartyData
        {
            RecordId = 5,
            GGHOAIDMOMC = BuildGamePlayData(),
            TrainPartyInfo = ToPartyInfo(),
            PassengerInfo = ToPassenger(),
            UnlockAreaNum = 6
        };
    }

    public TrainPartyInfo ToPartyInfo()
    {
        var info = new TrainPartyInfo
        {
            EKLIAOBCHPI = true,
            HIHKGPJCDPK = 30,
            NMBFMHLNPLM = 100,
            OBAMHCIFFOC = 30,
            CurFund = 308874,
            FCLJEACKGLE = new KJGMAEGNBKL()
        };

        info.FCLJEACKGLE.IDPHCKABBJM.AddRange(new[]
        {
            new IOBBILGOFCL { Id = 1409, Slot = 1600301, Type = 1 },
            new IOBBILGOFCL { Id = 1415, Slot = 1600302, Type = 1 },
            new IOBBILGOFCL { Id = 1005, Slot = 1600303, Type = 4 },
            new IOBBILGOFCL { Id = 5113, Slot = 1600304, Type = 4 },
            new IOBBILGOFCL { Id = 23052, Slot = 1600305, Type = 3 },
            new IOBBILGOFCL { Id = 23042, Slot = 1600306, Type = 3 }
        });

        info.FCLJEACKGLE.JDBOIHJHEHP.AddRange(new[]
        {
            new AreaDynamicInfo { DiceSlotId = 1600101, DiyDynamicId = 295608 },
            new AreaDynamicInfo { DiceSlotId = 1600102, DiyDynamicId = 295609 },
            new AreaDynamicInfo { DiceSlotId = 1600103, DiyDynamicId = 294003 },
            new AreaDynamicInfo { DiceSlotId = 1600104, DiyDynamicId = 295604 },
            new AreaDynamicInfo { DiceSlotId = 1600105, DiyDynamicId = 295606 },
            new AreaDynamicInfo { DiceSlotId = 1600201, DiyDynamicId = 295121 }
        });

        info.FCLJEACKGLE.NMLANPJGJFA.AddRange(new[]
        {
            new LMHFOFIEIMN { DiceSlotId = 1600106, OOPOADNHABK = 295523, NJMONGKLJJC = 1780264800 },
            new LMHFOFIEIMN { DiceSlotId = 1600107, OOPOADNHABK = 295527, NJMONGKLJJC = 1783893600 },
            new LMHFOFIEIMN { DiceSlotId = 1600108, OOPOADNHABK = 295531, NJMONGKLJJC = 1787522400 }
        });

        foreach (var (propId, time) in Props)
            info.GBDEGDGGKOL.Add(new CCAIPEBCIMH { PropId = (uint)propId, Time = time });

        info.MMFEPEBGAFC.AddRange(new[]
        {
            new CBGAODCKAGB { OOPOADNHABK = 295523, NJMONGKLJJC = 1780264800 },
            new CBGAODCKAGB { OOPOADNHABK = 295527, NJMONGKLJJC = 1783893600 },
            new CBGAODCKAGB { OOPOADNHABK = 295531, NJMONGKLJJC = 1787522400 }
        });

        info.AreaList.AddRange(BuildAreas());
        return info;
    }

    public static TrainPartyPassengerInfo ToPassenger()
    {
        var info = new TrainPartyPassengerInfo();
        info.DACEDNGJLBM.Add(1010);
        info.PassengerInfoList.AddRange(new[]
        {
            new TrainPartyPassenger { PassengerId = 1001, RecordId = 4, DACEDNGJLBM = { 103u, 101u, 102u } },
            new TrainPartyPassenger { PassengerId = 1002, RecordId = 4, DACEDNGJLBM = { 202u, 201u, 203u } },
            new TrainPartyPassenger { PassengerId = 1003, RecordId = 3, DACEDNGJLBM = { 302u, 303u, 301u } },
            new TrainPartyPassenger { PassengerId = 1004, RecordId = 3, DACEDNGJLBM = { 503u, 502u } },
            new TrainPartyPassenger { PassengerId = 1005, RecordId = 4, DACEDNGJLBM = { 401u, 402u, 403u } },
            new TrainPartyPassenger { PassengerId = 1006, RecordId = 2, DACEDNGJLBM = { 601u } },
            new TrainPartyPassenger { PassengerId = 1007, RecordId = 2, DACEDNGJLBM = { 701u } },
            new TrainPartyPassenger { PassengerId = 1008, RecordId = 2, DACEDNGJLBM = { 801u } },
            new TrainPartyPassenger { PassengerId = 1009, RecordId = 1, DACEDNGJLBM = { 901u } }
        });
        return info;
    }

    public uint GetWorldId() => 501;
    public ulong GetRefreshTime() => 1780264800;

    public KJGMAEGNBKL SetSelfDisplay(KJGMAEGNBKL? selfDisplay)
    {
        return ToPartyInfo().FCLJEACKGLE;
    }

    private static FKPMOKOJNHP BuildGamePlayData()
    {
        var data = new FKPMOKOJNHP
        {
            MAONLHBKOFK = 5,
            ADGDMDNMCIK = new LNLMPKALPEF
            {
                AOLHMEFDAHE = 2,
                BHFDLCPEPHM = 2,
                FHKEMCLFBON = new GDKPBDAAKBH
                {
                    DGGAIEANNBJ = 5,
                    DHFGLLAIFHP = 6,
                    FELAADBOAKD = 4,
                    MPJPBLLCMHK = 8001
                }
            },
            MBHAHNEJGAC = new AJNICOHFJGC
            {
                IJIMFPEKPOK = 100,
                IMKOJKJAHMM = new KBNIJFEMBPJ()
            },
            NJFAMMMDDIK = new MEGIHBEMOAB
            {
                AFOLIFFCDHJ = 1001,
                FNKKIDJPKIH = 28,
                CurIndex = 1
            }
        };

        data.ADGDMDNMCIK.JBOCOPNEJLB.AddRange(new[]
        {
            new ANBOFKHHDNE { CJOPNFDBJHD = 6101, JBPCICCFPGE = 2, UniqueId = 2 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 6102, JBPCICCFPGE = 3, UniqueId = 3 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 6103, JBPCICCFPGE = 4, UniqueId = 4 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 6104, JBPCICCFPGE = 5, UniqueId = 5 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 2001, JBPCICCFPGE = 6, UniqueId = 6 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 8001, JBPCICCFPGE = 7, UniqueId = 7 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 4001, JBPCICCFPGE = 8, UniqueId = 8 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 6105, JBPCICCFPGE = 9, UniqueId = 9 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 1001, JBPCICCFPGE = 10, UniqueId = 10 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 6101, JBPCICCFPGE = 11, UniqueId = 11 },
            new ANBOFKHHDNE { CJOPNFDBJHD = 2001, JBPCICCFPGE = 12, UniqueId = 12 }
        });

        data.MBHAHNEJGAC.IMKOJKJAHMM.IMKOJKJAHMM.AddRange(new[]
        {
            new DKBJKODADGM { CardId = 107, UniqueId = 1 },
            new DKBJKODADGM { CardId = 321, CurIndex = 2, UniqueId = 3 },
            new DKBJKODADGM { CardId = 207, CurIndex = 4, UniqueId = 4 },
            new DKBJKODADGM { CardId = 206, CurIndex = 5, UniqueId = 5 }
        });

        data.NJFAMMMDDIK.DMMNCNGGPHL.AddRange(new[]
        {
            MakePassenger(1001, 350, 35),
            MakePassenger(1006, 300, 30),
            MakePassenger(1007, 300, 30),
            MakePassenger(1008, 300, 30),
            MakePassenger(1009, 300, 30)
        });
        data.NJFAMMMDDIK.IBEBJMMCIDH.Add(105);
        data.NJFAMMMDDIK.PHGLFHDDACM.AddRange(new[]
        {
            new JAFPMLLOGDI { SkillId = 214, SkillLevel = 1, MOOOPAMBOFK = 1 },
            new JAFPMLLOGDI { SkillId = 208, SkillLevel = 1, MOOOPAMBOFK = 1 }
        });

        return data;
    }

    private static DOBIOOHLGAA MakePassenger(uint id, uint hp, uint atk)
    {
        return new DOBIOOHLGAA
        {
            PassengerId = id,
            IANGBIBHJDF = 100,
            DGFGNMJALGJ = new DKBNGDHCNCH
            {
                KKIOIFLJEEL = hp,
                PNOCKMEMHLK = atk
            }
        };
    }

    private static readonly (int propId, long time)[] Props =
    [
        (291006, 1776823406), (295008, 1778283303), (293002, 1776823406), (292004, 1776823406),
        (294005, 1776823406), (295603, 1776823406), (295125, 1776823685), (295608, 1776823406),
        (291002, 1776823406), (295611, 1777136151), (291005, 1776823406), (293004, 1776823406),
        (291003, 1776823406), (295609, 1776823406), (294006, 1776823406), (295604, 1776823406),
        (292005, 1776823406), (295602, 1777908001), (294004, 1776823406), (293005, 1776823406),
        (295607, 1776823406), (291001, 1776823406), (295606, 1776823406), (293007, 1776823406),
        (295122, 1776823406), (295605, 1776823406), (294001, 1776823406), (293003, 1776823406),
        (293006, 1776823406), (295121, 1776823406), (292003, 1776823406), (295011, 1776823406),
        (295123, 1776823406), (293008, 1776823406), (291007, 1776823406), (295124, 1776823406),
        (293001, 1776823406), (292001, 1776823406), (294002, 1776823406), (292006, 1776823406),
        (292002, 1776823406), (294003, 1776823406), (295601, 1776959127), (291004, 1776823406),
        (295610, 1776823406)
    ];

    private static IEnumerable<TrainPartyArea> BuildAreas()
    {
        return
        [
            BuildArea(11, [10001,10003,11001,11002,11003,11004,11005,11006,11007,11008,11009,11011], [101010,101011,101012,101013,101014,101015,101016,101017,101018], [(1100101,291002),(1100201,294001),(1100901,292004)]),
            BuildArea(12, [10007,10009,10010,12001,12002,12003,12004,12005,12006,12007,12008,12009,12011,12012,12013], [201010,201011,201013,201014,201016,201017,201021,201022], [(1200101,292001),(1200201,294006),(1200401,292002),(1200501,294002),(1200701,293007),(1200703,295011),(1201201,291006)]),
            BuildArea(13, [10002,10014,13001,13002,13003,13004,13005,13007], [301011,301012,301013,301014,301015], [(1300201,291001),(1300301,293001),(1300302,293008),(1300401,291007),(1300402,294004)]),
            BuildArea(14, [10004,10008,14001,14002,14003,14004,14005,14006,14007,14008,14009,14010,14011], [401010,401011,401012,401015,401016,401018,401020], [(1400501,292005),(1400502,292006)]),
            BuildArea(15, [10006,10011,15001,15002,15005,15006,15007,15008], [501010,501011,501013,501014,501015,501016,501017], [(1500101,294005),(1500601,292003),(1500602,291005),(1500701,293002)]),
            BuildArea(16, [10005,10012,10013,16001,16002,16003], [601010,601012,601013,601014], [])
        ];
    }

    private static TrainPartyArea BuildArea(int areaId, int[] stepIds, int[] staticProps, (int slotId, int dynamicId)[] dynamic)
    {
        var area = new TrainPartyArea
        {
            AreaId = (uint)areaId,
            Progress = 100,
            AreaStepInfo = new AreaStepInfo()
        };
        area.StepIdList.AddRange(stepIds.Select(x => (uint)x));
        area.VerifyStepIdList.AddRange(staticProps.Select(x => (uint)x));
        area.StaticPropIdList.AddRange(staticProps.Select(x => (uint)x));
        area.DynamicInfo.AddRange(dynamic.Select(x => new AreaDynamicInfo
        {
            DiceSlotId = (uint)x.slotId,
            DiyDynamicId = (uint)x.dynamicId
        }));
        return area;
    }
}
