using System.Collections.Concurrent;
using System.Text.Json;
using March7thHoney.Util;

namespace March7thHoney.Internationalization;

/// <summary>
///     Loads translations from external flat key→value JSON files (Config/Languages/{lang}.json) into a
///     dictionary and resolves keys by direct lookup. No reflection, no Type.GetType/Activator — NativeAOT safe.
/// </summary>
public static class I18NManager
{
    public static Logger Logger = new("I18nManager");

    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache = new();
    private static Dictionary<string, string> _current = new();

    public static void LoadLanguage()
    {
        var lang = ConfigManager.Config.ServerOption.Language;
        _current = LoadOrGet(lang);
        if (_current.Count == 0)
            Logger.Warn($"Configured language '{lang}' could not be loaded (and EN fallback is missing).");
        Logger.Info(Translate("Server.ServerInfo.LoadedItem", Translate("Word.Language")));
    }

    public static string Translate(string key, params string[] args)
    {
        return Format(_current.GetValueOrDefault(key) ?? key, args);
    }

    public static string TranslateAsCertainLang(string langStr, string key, params string[] args)
    {
        return Format(LoadOrGet(langStr).GetValueOrDefault(key) ?? key, args);
    }

    private static Dictionary<string, string> LoadOrGet(string lang)
    {
        return Cache.GetOrAdd(lang, static l =>
            Load(l) ?? (l == "EN" ? [] : LoadOrGet("EN")));
    }

    private static Dictionary<string, string>? Load(string lang)
    {
        var path = Path.Combine(ConfigManager.Config.Path.LanguagePath, lang + ".json");
        // Missing file is expected for client-only languages (e.g. handbook generation across locales);
        // callers silently fall back to EN. Only surfaced if the configured server language is missing.
        if (!File.Exists(path)) return null;

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(path),
                I18nJsonContext.Default.DictionaryStringString) ?? [];
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to load language file: {path}", e);
            return null;
        }
    }

    private static string Format(string result, string[] args)
    {
        var index = 0;
        return args.Aggregate(result, (current, arg) => current.Replace("{" + index++ + "}", arg));
    }
}
