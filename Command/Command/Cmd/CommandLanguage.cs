using March7thHoney.Database;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("language", "Game.Command.Language.Desc", "Game.Command.Language.Usage", ["lang"],
    permission: CommandPermissions.Public)]
public class CommandLanguage : ICommand
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "EN", ["en-us"] = "EN", ["english"] = "EN",
        ["chs"] = "CHS", ["zh"] = "CHS", ["zh-cn"] = "CHS", ["zh-hans"] = "CHS", ["cn"] = "CHS",
        ["chinese"] = "CHS",
        ["cht"] = "CHT", ["zh-tw"] = "CHT", ["zh-hant"] = "CHT", ["tc"] = "CHT", ["traditional"] = "CHT"
    };

    [CommandDefault]
    public async ValueTask Language(CommandArg arg)
    {
        // The pin belongs to the player typing the command; the server console has no language of its own.
        var player = arg.Target?.Player;
        if (arg.Sender.IsConsole || player == null || player.Uid != arg.Sender.GetSender())
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Language.PlayerOnly"));
            return;
        }

        var available = GetAvailableLanguages();
        var availableText = string.Join(", ", available);
        if (arg.Args.Count == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Language.Current", player.EffectiveLanguage,
                I18NManager.Translate(player.Data.LanguagePin != null
                    ? "Game.Command.Language.SourcePinned"
                    : "Game.Command.Language.SourceClient"), availableText));
            return;
        }

        var choice = arg.Args[0];
        if (choice.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Language.Available", availableText));
            return;
        }

        if (choice.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            player.Data.LanguagePin = null;
            DatabaseHelper.MarkDirty(player.Uid);
            await arg.SendMsg(I18NManager.TranslateAsCertainLang(player.EffectiveLanguage,
                "Game.Command.Language.Cleared", player.EffectiveLanguage));
            return;
        }

        var code = ResolveLanguage(choice, available);
        if (code == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Language.NotFound", choice, availableText));
            return;
        }

        player.Data.LanguagePin = code;
        DatabaseHelper.MarkDirty(player.Uid);
        // Answer in the language just chosen, not the one this command started in.
        await arg.SendMsg(I18NManager.TranslateAsCertainLang(code, "Game.Command.Language.Set", code));
    }

    /// <summary>Maps an alias or file code (case-insensitive) to an installed language file code, or null.</summary>
    public static string? ResolveLanguage(string input, IReadOnlyCollection<string> available)
    {
        var wanted = Aliases.GetValueOrDefault(input.Trim()) ?? input.Trim();
        return available.FirstOrDefault(code => code.Equals(wanted, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Language codes that have a file in the configured language directory.</summary>
    public static List<string> GetAvailableLanguages()
    {
        try
        {
            var directory = ConfigManager.Config.Path.LanguagePath;
            if (!Directory.Exists(directory)) return [];
            return Directory.EnumerateFiles(directory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>()
                .Where(code => code.Length > 0)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
