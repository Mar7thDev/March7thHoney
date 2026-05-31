using System.Collections.Concurrent;
using March7thHoney.Configuration;
using March7thHoney.Database.Account;
using March7thHoney.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace March7thHoney.WebServer.Handler;

public static class AuthRateLimiter
{
    private sealed class Window
    {
        public long Start;
        public int Count;
    }

    private static readonly ConcurrentDictionary<string, Window> Windows = new();
    private static readonly ConcurrentDictionary<string, long> BlockedUntil = new();
    private static long _lastSweep;

    public static bool TryReject(HttpContext context, out JsonResult result)
    {
        result = default!;
        var option = ConfigManager.Config.ServerOption.Auth.AntiAbuse;
        if (!option.RateLimitEnabled)
            return false;

        var key = AccountBanHelper.CreateIpIdentityKey(context.Connection.RemoteIpAddress?.ToString());
        if (string.IsNullOrEmpty(key))
            return false;

        var now = Extensions.GetUnixSec();
        Sweep(now, option);

        if (BlockedUntil.TryGetValue(key, out var until))
        {
            if (now < until)
            {
                result = BlockedResult(until - now);
                return true;
            }

            BlockedUntil.TryRemove(key, out _);
        }

        var window = Windows.GetOrAdd(key, _ => new Window { Start = now, Count = 0 });
        bool tripped;
        lock (window)
        {
            if (now - window.Start >= option.RateLimitWindowSeconds)
            {
                window.Start = now;
                window.Count = 1;
            }
            else
            {
                window.Count++;
            }

            tripped = window.Count > option.RateLimitMaxRequests;
        }

        if (!tripped)
            return false;

        BlockedUntil[key] = now + option.RateLimitBlockSeconds;
        Windows.TryRemove(key, out _);
        result = BlockedResult(option.RateLimitBlockSeconds);
        return true;
    }

    private static JsonResult BlockedResult(long remainingSeconds)
    {
        var message = $"Too many requests. Try again in {AccountBanHelper.FormatDuration(remainingSeconds)}.";
        return new JsonResult(new { retcode = -203, message, data = (object?)null });
    }

    private static void Sweep(long now, AntiAbuseOption option)
    {
        if (now - _lastSweep < option.RateLimitWindowSeconds)
            return;
        _lastSweep = now;

        foreach (var (key, until) in BlockedUntil)
            if (now >= until)
                BlockedUntil.TryRemove(key, out _);

        foreach (var (key, window) in Windows)
            if (now - window.Start >= option.RateLimitWindowSeconds)
                Windows.TryRemove(key, out _);
    }
}
