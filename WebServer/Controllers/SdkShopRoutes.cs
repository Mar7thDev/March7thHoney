using System.Globalization;
using System.Text;
using System.Text.Json;
using March7thHoney.Configuration;
using March7thHoney.Data;
using March7thHoney.Data.Custom;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Inventory;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

// Emulates the SDK shop endpoints so a recharge click grants the tier instead of opening a payment page.
public static class SdkShopRoutes
{
    private const int OneiricShardItemId = 3; // Mcoin
    private const int PayAbortedRetcode = -1;
    private static readonly TimeSpan GrantDedupeWindow = TimeSpan.FromSeconds(5);

    private static readonly Logger Logger = new("SdkShop");
    private static readonly Lock GrantLock = new();
    private static readonly Dictionary<string, long> LastGrantAt = [];

    private static ConfigContainer Config => ConfigManager.Config;

    private static Task Json(HttpContext ctx, string content)
    {
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(content);
    }

    // Paths taken from the client's own SDK url table (area=os, env=2); the launcher proxy forwards them here.
    public static void MapSdkShopRoutes(this IEndpointRouteBuilder app)
    {
        foreach (var prefix in (string[]) ["/", "/hkrpg_global/", "/{gameKey}/"])
        {
            app.MapMethods($"{prefix}combo/granter/login/beforeVerify", ["GET", "POST"], BeforeVerify);
            app.MapMethods($"{prefix}mdk/shopwindow/shopwindow/getCurrencyAndCountryByIp", ["GET", "POST"],
                CurrencyAndCountry);
            app.MapMethods($"{prefix}mdk/shopwindow/shopwindow/listPriceTier", ["GET", "POST"], ListPriceTier);
            app.MapMethods($"{prefix}mdk/shopwindow/shopwindow/listPriceTierV2", ["GET", "POST"], ListPriceTier);
            app.MapMethods($"{prefix}mdk/shopwindow/shopwindow/listGoods", ["GET", "POST"], ListGoods);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/queryGoods", ["GET", "POST"], ListGoods);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/listPayPlat", ["GET", "POST"], ListPayPlat);
            app.MapMethods($"{prefix}mdk/tally/tally/listPayPlat", ["GET", "POST"], ListPayPlat);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/detectPay", ["GET", "POST"], EmptyOk);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/firstPayment", ["GET", "POST"], EmptyOk);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/preOrderValidation", ["GET", "POST"], EmptyOk);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/createOrder", ["GET", "POST"], CreateOrder);
            app.MapMethods($"{prefix}combo/cashier/cashier/createOrder", ["GET", "POST"], CreateOrder);
            app.MapMethods($"{prefix}mdk/luckycat/luckycat/checkOrder", ["GET", "POST"], CheckOrder);
        }

        app.MapMethods("/mm-backpack/v1/listGoods", ["GET", "POST"], ListGoods);
    }

    private static async Task BeforeVerify(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"beforeVerify. {request.Describe()}");
        await Json(ctx, "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}");
    }

    private static async Task CurrencyAndCountry(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"getCurrencyAndCountryByIp. {request.Describe()}");

        var config = GameData.QueryProductInfoConfig;
        await Json(ctx,
            $"{{\"retcode\":0,\"message\":\"OK\",\"data\":{{\"currency\":\"{config.Currency}\"," +
            $"\"country\":\"{config.CountryCode}\"}}}}");
    }

    private static async Task ListPriceTier(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"listPriceTier. {request.Describe()}");

        var config = GameData.QueryProductInfoConfig;
        var rows = new StringBuilder();
        foreach (var tier in config.TierPriceList)
        {
            if (rows.Length > 0) rows.Append(',');
            rows.Append(BuildTierJson(config, tier));
        }

        await Json(ctx,
            $"{{\"retcode\":0,\"message\":\"OK\",\"data\":{{\"suggest_currency\":\"\",\"tiers\":[{rows}]," +
            $"\"price_tier_version\":\"{PriceTierVersion(config)}\"}}}}");
    }

    private static async Task ListGoods(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"listGoods. {request.Describe()}");
        await Json(ctx, "{\"retcode\":0,\"message\":\"OK\",\"data\":{\"goods_list\":[]}}");
    }

    private static async Task ListPayPlat(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"listPayPlat. {request.Describe()}");
        await Json(ctx, "{\"retcode\":0,\"message\":\"OK\",\"data\":{\"pay_plat\":[]}}");
    }

    private static async Task EmptyOk(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"sdk pay endpoint. {request.Describe()}");
        await Json(ctx, "{\"retcode\":0,\"message\":\"OK\",\"data\":{}}");
    }

    private static async Task CheckOrder(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"checkOrder. {request.Describe()}");
        await Json(ctx, PayAborted());
    }

    private static async Task CreateOrder(HttpContext ctx)
    {
        var request = await SdkRequest.Read(ctx);
        Logger.Info($"createOrder. {request.Describe()}");

        if (!Config.ServerOption.EnableFakeRecharge)
        {
            Logger.Info("createOrder ignored, fake recharge is disabled");
            await Json(ctx, PayAborted());
            return;
        }

        var product = ResolveProduct(request);
        if (product == null)
        {
            Logger.Warn($"createOrder did not match any recharge tier. {request.Describe()}");
            await Json(ctx, PayAborted());
            return;
        }

        if (product.GiftType != (int)ProductGiftType.ProductGiftCoin)
        {
            Logger.Warn($"createOrder for unsupported gift type {product.GiftType}, product={product.ProductID}");
            await Json(ctx, PayAborted());
            return;
        }

        var player = ResolvePlayer(request);
        if (player?.InventoryManager == null)
        {
            Logger.Warn($"createOrder has no online player. {request.Describe()}");
            await Json(ctx, PayAborted());
            return;
        }

        if (!TryMarkGranted(player.Uid, product.ProductID))
        {
            Logger.Info($"createOrder deduplicated. uid={player.Uid} product={product.ProductID}");
            await Json(ctx, PayAborted());
            return;
        }

        await GrantRecharge(player, product);
        await Json(ctx, PayAborted());
    }

    private static async Task GrantRecharge(PlayerInstance player, RechargeConfigExcel product)
    {
        List<ItemData> rewards = [new() { ItemId = OneiricShardItemId, Count = product.FirstCharge }];
        if (product.NormalCharge > 0)
            rewards.Add(new ItemData { ItemId = OneiricShardItemId, Count = product.NormalCharge });

        var total = rewards.Sum(x => x.Count);
        await player.InventoryManager!.AddItem(OneiricShardItemId, total, notify: false, sync: false);
        await player.SendPacket(new PacketPlayerSyncScNotify(player.ToProto()));
        await player.SendPacket(new PacketRechargeSuccNotify(product.ProductID, rewards,
            MonthCardService.GetMonthCardOutDateTime(player.Data)));

        Logger.Info($"Fake recharge granted. uid={player.Uid} product={product.ProductID} amount={total}");
    }

    private static RechargeConfigExcel? ResolveProduct(SdkRequest request)
    {
        var productId = request.Find("product_id", "productId", "goods_id", "goodsId", "product");
        if (!string.IsNullOrEmpty(productId) &&
            GameData.RechargeConfigData.TryGetValue(productId, out var byProduct))
            return byProduct;

        var tierId = request.Find("price_tier", "priceTier", "tier", "tier_id", "tierId");
        if (string.IsNullOrEmpty(tierId)) return null;

        return GameData.QueryProductInfoConfig.ProductList
            .Where(x => string.Equals(x.PriceTier, tierId, StringComparison.OrdinalIgnoreCase))
            .Select(x => GameData.RechargeConfigData.GetValueOrDefault(x.ProductId ?? ""))
            .FirstOrDefault(x => x != null && x.GiftType == (int)ProductGiftType.ProductGiftCoin);
    }

    private static PlayerInstance? ResolvePlayer(SdkRequest request)
    {
        var rawUid = request.Find("open_id", "openId", "account_id", "accountId", "uid", "account_uid", "role_id");
        if (int.TryParse(rawUid, out var uid))
        {
            var connection = Listener.GetActiveConnection(uid);
            if (connection?.Player != null) return connection.Player;
        }

        var online = March7thHoneyListener.GetSnapshot()
            .OfType<Connection>()
            .Where(x => x.Player != null && x.State == SessionStateEnum.ACTIVE)
            .ToList();

        if (online.Count != 1) return null;

        Logger.Warn($"Pay request carried no usable uid, falling back to the only online player {online[0].Player!.Uid}");
        return online[0].Player;
    }

    private static bool TryMarkGranted(int uid, string productId)
    {
        var now = Extensions.GetUnixMs();
        var key = $"{uid}:{productId}";
        lock (GrantLock)
        {
            if (LastGrantAt.TryGetValue(key, out var last) && now - last < (long)GrantDedupeWindow.TotalMilliseconds)
                return false;

            LastGrantAt[key] = now;
            return true;
        }
    }

    // Shape recovered from the SDK's own cached price-tier payload; price is in minor units.
    private static string BuildTierJson(QueryProductInfoConfig config, QueryProductTierPrice tier)
    {
        var minorUnits = ((long)Math.Round(tier.Price * 100)).ToString(CultureInfo.InvariantCulture);
        return $"{{\"tier_id\":\"{tier.TierId}\",\"t_price\":[{{\"enable\":1," +
               $"\"country\":\"{config.CountryCode}\",\"currency\":\"{config.Currency}\"," +
               $"\"price\":\"{minorUnits}\",\"symbol\":\"{config.CurrencySymbol}\"," +
               "\"amount_display\":\"\"}]}";
    }

    // Stable across restarts so the SDK only refetches when the configured prices actually change.
    private static string PriceTierVersion(QueryProductInfoConfig config)
    {
        var version = 17L;
        foreach (var tier in config.TierPriceList)
        {
            foreach (var c in tier.TierId) version = (version * 31 + c) % 1_000_000_007L;
            version = (version * 31 + (long)Math.Round(tier.Price * 100)) % 1_000_000_007L;
        }

        return version.ToString(CultureInfo.InvariantCulture);
    }

    // The client surfaces this message in a toast, so keep it empty and log the reason instead.
    private static string PayAborted()
    {
        return $"{{\"retcode\":{PayAbortedRetcode},\"message\":\"\",\"data\":null}}";
    }
}

// Reads SDK request parameters regardless of whether they arrive as query, form or JSON body.
internal sealed class SdkRequest
{
    private static readonly string[] SecretKeys = ["combo_token", "token", "auth_key", "sign", "combo_id"];

    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public string Path { get; private set; } = "";

    public static async Task<SdkRequest> Read(HttpContext ctx)
    {
        var request = new SdkRequest { Path = ctx.Request.Path.Value ?? "" };

        foreach (var pair in ctx.Request.Query)
            request._values[pair.Key] = pair.Value.ToString();

        if (ctx.Request.HasFormContentType)
        {
            var form = await ctx.Request.ReadFormAsync();
            foreach (var pair in form)
                request._values[pair.Key] = pair.Value.ToString();
            return request;
        }

        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
        request.ReadJsonBody(await reader.ReadToEndAsync());
        return request;
    }

    public string? Find(params string[] keys)
    {
        foreach (var key in keys)
            if (_values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;

        return null;
    }

    public string Describe()
    {
        var fields = _values.Select(x => $"{x.Key}={(IsSecret(x.Key) ? "<redacted>" : x.Value)}");
        return $"path={Path} {string.Join(' ', fields)}";
    }

    private static bool IsSecret(string key)
    {
        return SecretKeys.Any(x => key.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private void ReadJsonBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object) Flatten(document.RootElement);
        }
        catch (JsonException)
        {
            _values["_raw_body"] = body.Length > 512 ? body[..512] : body;
        }
    }

    private void Flatten(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Flatten(property.Value);
                    break;
                case JsonValueKind.Array:
                    break;
                default:
                    _values.TryAdd(property.Name, property.Value.ToString());
                    break;
            }
    }
}
