using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketQueryProductInfoScRsp : BasePacket
{
    public PacketQueryProductInfoScRsp() : base(CmdIds.QueryProductInfoScRsp)
    {
        var proto = new QueryProductInfoScRsp
        {
            BAMOOGPNJEH = 5,
            IFKKKLCFOBK = 5,
            MonthCardOutDateTime = 1787774400,
            ProductList =
            {
                new Product
                {
                    JCJGHCOEOOJ = 50,
                    BeginTime = 978278400,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftAppleGiftCard,
                    PriceTier = "Tier_2",
                    ProductId = "rpgchnios050giftcard"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftMonthCard,
                    PriceTier = "Tier_5",
                    ProductId = "rpgchnmonthcardtier5"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 1,
                    BeginTime = 1743969600,
                    EndTime = 1747724399,
                    GiftType = ProductGiftType.ProductGiftSingle6,
                    PriceTier = "Tier_1",
                    ProductId = "rpgchnyeartier1"
                },
                new Product
                {
                    BeginTime = 978278400,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftBp68Upgrade128,
                    PriceTier = "Tier_12",
                    ProductId = "rpgchnbattlepassupgradetier12"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    DoubleReward = true,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftCoin,
                    PriceTier = "Tier_1",
                    ProductId = "rpgchncoin60tier1"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftCoin,
                    PriceTier = "Tier_60",
                    ProductId = "rpgchncoin6480tier60"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    DoubleReward = true,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftCoin,
                    PriceTier = "Tier_5",
                    ProductId = "rpgchncoin300tier5"
                },
                new Product
                {
                    BeginTime = 978278400,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftBp128,
                    PriceTier = "Tier_20",
                    ProductId = "rpgchnbattlepassdeluxetier20"
                },
                new Product
                {
                    BeginTime = 978278400,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftBp68,
                    PriceTier = "Tier_10",
                    ProductId = "rpgchnbattlepassnormaltier10"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftCoin,
                    PriceTier = "Tier_50",
                    ProductId = "rpgchncoin3280tier50"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    DoubleReward = true,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftCoin,
                    PriceTier = "Tier_15",
                    ProductId = "rpgchncoin980tier15"
                },
                new Product
                {
                    BeginTime = 1577808000,
                    DoubleReward = true,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.ProductGiftCoin,
                    PriceTier = "Tier_30",
                    ProductId = "rpgchncoin1980tier30"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 1,
                    BeginTime = 1751227200,
                    EndTime = 1754981999,
                    GiftType = ProductGiftType.ProductGiftSingle6,
                    PriceTier = "Tier_1",
                    ProductId = "rpgchnyear2tier1"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 1,
                    MEMNCJLKAEE = 2,
                    BeginTime = 1770850800,
                    EndTime = 1774335600,
                    GiftType = ProductGiftType.ProductGiftSingle6,
                    PriceTier = "Tier_1",
                    ProductId = "rpgchnyear3tier1"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 1,
                    MEMNCJLKAEE = 1,
                    BeginTime = 1776675600,
                    EndTime = 1780257599,
                    GiftType = (ProductGiftType)25,
                    PriceTier = "Alternate_Tier_A",
                    ProductId = "rpgchnyear4tiercny100"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 1,
                    MEMNCJLKAEE = 3,
                    BeginTime = 1776675600,
                    EndTime = 1780257599,
                    GiftType = ProductGiftType.ProductGiftDailyLogin30,
                    PriceTier = "Tier_5",
                    ProductId = "rpgchnyear3tier5"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 2,
                    MEMNCJLKAEE = 1,
                    BeginTime = 1770850800,
                    EndTime = 1774335600,
                    GiftType = ProductGiftType.ProductGiftTicket10,
                    PriceTier = "Tier_15",
                    ProductId = "rpgchnticket3tier15"
                },
                new Product
                {
                    EEFHEBKHKAB = 2,
                    JCJGHCOEOOJ = 2,
                    BeginTime = 1751227200,
                    EndTime = 1754981999,
                    GiftType = ProductGiftType.ProductGiftTicket10,
                    PriceTier = "Tier_15",
                    ProductId = "rpgchntickettier15"
                },
                new Product
                {
                    EEFHEBKHKAB = 1,
                    JCJGHCOEOOJ = 1,
                    MEMNCJLKAEE = 1,
                    BeginTime = 1776810600,
                    EndTime = 4070880000,
                    GiftType = ProductGiftType.EjebhiikajfDbiadngnfmd,
                    PriceTier = "Tier_4",
                    ProductId = "rpgchnskintier4"
                }
            }
        };

        SetData(proto);
    }
}
