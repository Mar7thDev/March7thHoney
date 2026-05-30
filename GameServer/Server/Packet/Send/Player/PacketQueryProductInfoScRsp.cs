using March7thHoney.Data;
using March7thHoney.Data.Custom;
using March7thHoney.Database.Player;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketQueryProductInfoScRsp : BasePacket
{
    public PacketQueryProductInfoScRsp(PlayerData? playerData) : base(CmdIds.QueryProductInfoScRsp)
    {
        // 4.3: QueryProductInfoScRsp 混淆名变更 BAMOOGPNJEH->BFFHEKCGLMA, IFKKKLCFOBK->BEBFNDIGKFK(tag10 不变)
        var proto = new QueryProductInfoScRsp
        {
            BFFHEKCGLMA = 5,
            BEBFNDIGKFK = 5,
            MonthCardOutDateTime = playerData == null ? 0 : (ulong)MonthCardService.GetMonthCardOutDateTime(playerData)
        };

        foreach (var item in BuildProducts(GameData.QueryProductInfoConfig, ConfigManager.Config.ServerOption.EnableMonthCard))
            proto.ProductList.Add(item);

        SetData(proto);
    }

    private static List<Product> BuildProducts(QueryProductInfoConfig config, bool enableMonthCard)
    {
        List<Product> products = [];
        foreach (var item in config.ProductList)
        {
            var giftType = ParseGiftType(item.GiftType);
            if (!enableMonthCard && giftType == ProductGiftType.ProductGiftMonthCard) continue;

            // 4.3: Product 三个 uint 字段混淆名变更，proto 与 .proto 定义均无可读名，按 tag 升序尽力对应。
            //   配置中仅 JCJGHCOEOOJ 有非零值(奖励数量)，故映射到 CLDELMPNAPB；另两者恒为 0。
            //   (config 字段名保持不变以匹配 JSON key)
            products.Add(new Product
            {
                CLDELMPNAPB = item.JCJGHCOEOOJ,
                EMIHEDNKBCA = item.MEMNCJLKAEE,
                ENCDABOOIKJ = item.EEFHEBKHKAB,
                BeginTime = item.BeginTime,
                EndTime = item.EndTime,
                GiftType = giftType,
                PriceTier = item.PriceTier ?? "",
                ProductId = item.ProductId ?? "",
                DoubleReward = item.DoubleReward
            });
        }

        return products;
    }

    private static ProductGiftType ParseGiftType(object raw)
    {
        if (raw is null) return ProductGiftType.ProductGiftNone;

        if (raw is long l) return (ProductGiftType)l;
        if (raw is int i) return (ProductGiftType)i;
        if (raw is JValue jv)
        {
            if (jv.Type == JTokenType.Integer) return (ProductGiftType)jv.Value<long>();
            var strValue = jv.Value<string>();
            if (!string.IsNullOrWhiteSpace(strValue)) return ParseGiftTypeFromString(strValue);
            return ProductGiftType.ProductGiftNone;
        }

        return ParseGiftTypeFromString(raw.ToString() ?? "");
    }

    private static ProductGiftType ParseGiftTypeFromString(string value)
    {
        if (long.TryParse(value, out var number)) return (ProductGiftType)number;
        if (Enum.TryParse<ProductGiftType>(value, true, out var byName)) return byName;

        foreach (var field in typeof(ProductGiftType).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<Google.Protobuf.Reflection.OriginalNameAttribute>();
            if (attr != null && string.Equals(attr.Name, value, StringComparison.OrdinalIgnoreCase))
                return (ProductGiftType)field.GetValue(null)!;
        }

        return ProductGiftType.ProductGiftNone;
    }
}
