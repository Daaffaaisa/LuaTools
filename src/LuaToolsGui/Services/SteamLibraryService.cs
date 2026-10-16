using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LuaToolsGui.Services;

/// <summary>
/// Resolves where a Steam game is installed on disk by walking Valve's KeyValues files:
/// registry Steam root → steamapps\libraryfolders.vdf (every library, possibly across drives) →
/// per-library steamapps\appmanifest_&lt;appid&gt;.acf (the game's installdir) → common\&lt;installdir&gt;.
/// Best-effort: returns null if Steam/the game can't be located. Used to apply Denuvo "fix" zips.
/// </summary>
public partial class SteamLibraryService(SteamService steam)
{
    // "path"        "D:\\SteamLibrary"     → the library root (libraryfolders.vdf)
    // "installdir"  "Elden Ring"           → folder under steamapps\common (appmanifest_*.acf)
    // Values are quoted; backslashes are escaped (\\). One key per line.
    [GeneratedRegex(@"""path""\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex PathRegex();

    [GeneratedRegex(@"""installdir""\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex InstallDirRegex();

    [GeneratedRegex(@"""appid""\s*""(\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AppIdRegex();

    [GeneratedRegex(@"""name""\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^""(\d{17})""", RegexOptions.Multiline)]
    private static partial Regex SteamId64Regex();

    /// <summary>An installed game, as its appmanifest describes it.</summary>
    public record InstalledGame(long AppId, string Name, string InstallDir);

    /// <summary>An owned game from the active user's Steam library.</summary>
    public record OwnedGame(long AppId, string Name, bool IsInstalled, string? InstallDir);

    /// <summary>
    /// Full path to the game's install folder (…\steamapps\common\&lt;installdir&gt;) if it exists on
    /// disk, else null (game not installed / Steam not found / unreadable).
    /// </summary>
    public string? GetInstallDir(long appId)
    {
        try
        {
            string? steamRoot = steam.EffectivePath;
            if (steamRoot is null) return null;

            foreach (string library in GetLibraryRoots(steamRoot))
            {
                string acf = Path.Combine(library, "steamapps", $"appmanifest_{appId}.acf");
                if (!File.Exists(acf)) continue;

                var m = InstallDirRegex().Match(File.ReadAllText(acf));
                if (!m.Success) continue;

                string installDir = Unescape(m.Groups[1].Value);
                string full = Path.Combine(library, "steamapps", "common", installDir);
                if (Directory.Exists(full)) return full;
            }
        }
        catch { /* unreadable VDF/ACF or odd path. Treat as not found */ }
        return null;
    }

    /// <summary>
    /// Every installed game across every library, from the appmanifests. Lazy, and skips anything whose
    /// folder isn't actually on disk.
    /// </summary>
    /// <remarks>
    /// Enumerating the .acf files is the cheap part (~39 ms across three drives here); what costs is
    /// whatever the caller then does per game. Yields rather than returning a list so a caller looking
    /// for one thing can stop early.
    /// </remarks>
    public IEnumerable<InstalledGame> EnumerateInstalled()
    {
        string? steamRoot = steam.EffectivePath;
        if (steamRoot is null) yield break;

        foreach (string library in GetLibraryRoots(steamRoot))
        {
            string steamapps = Path.Combine(library, "steamapps");
            string[] acfs;
            try
            {
                if (!Directory.Exists(steamapps)) continue;
                acfs = Directory.GetFiles(steamapps, "appmanifest_*.acf");
            }
            catch { continue; }

            foreach (string acf in acfs)
            {
                InstalledGame? game = null;
                try
                {
                    string text = File.ReadAllText(acf);

                    var idm = AppIdRegex().Match(text);
                    var dirm = InstallDirRegex().Match(text);
                    if (!idm.Success || !dirm.Success) continue;
                    if (!long.TryParse(idm.Groups[1].Value, out long appId)) continue;

                    string full = Path.Combine(steamapps, "common", Unescape(dirm.Groups[1].Value));
                    if (!Directory.Exists(full)) continue;

                    var nm = NameRegex().Match(text);
                    game = new InstalledGame(appId, nm.Success ? nm.Groups[1].Value : appId.ToString(), full);
                }
                catch { /* unreadable or malformed acf: skip this one, not the whole library */ }

                if (game is not null) yield return game;
            }
        }
    }

    /// <summary>Every Steam library root (the main install plus any added libraries).</summary>
    private static IEnumerable<string> GetLibraryRoots(string steamRoot)
    {
        // The main install is always a library.
        yield return steamRoot;

        string vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf)) yield break;

        string text;
        try { text = File.ReadAllText(vdf); } catch { yield break; }

        foreach (Match m in PathRegex().Matches(text))
        {
            string path = Unescape(m.Groups[1].Value);
            // The main root often appears here too; harmless duplicate (we just probe each).
            if (!string.Equals(path, steamRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(path))
                yield return path;
        }
    }

    /// <summary>
    /// Gets the 32-bit AccountID of the currently active/logged-in Steam user.
    /// Walks config\loginusers.vdf, or probes userdata directories as fallback.
    /// </summary>
    public uint? GetActiveAccountId(string? steamRoot = null)
    {
        steamRoot ??= steam.EffectivePath;
        if (steamRoot is null) return null;

        string loginUsersPath = Path.Combine(steamRoot, "config", "loginusers.vdf");
        if (File.Exists(loginUsersPath))
        {
            try
            {
                string text = File.ReadAllText(loginUsersPath);

                // Priority 1: user block with "mostrecent" "1"
                var mostRecent = Regex.Match(text, @"(?si)""(\d{17})""\s*\{[^}]*?""mostrecent""\s*""1""");
                if (mostRecent.Success && ulong.TryParse(mostRecent.Groups[1].Value, out ulong sid1))
                {
                    return (uint)(sid1 & 0xFFFFFFFFUL);
                }

                // Priority 2: user block with "AutoLogin" "1"
                var autoLogin = Regex.Match(text, @"(?si)""(\d{17})""\s*\{[^}]*?""AutoLogin""\s*""1""");
                if (autoLogin.Success && ulong.TryParse(autoLogin.Groups[1].Value, out ulong sid2))
                {
                    return (uint)(sid2 & 0xFFFFFFFFUL);
                }

                // Priority 3: first 17-digit SteamID64 in loginusers.vdf
                var first = SteamId64Regex().Match(text);
                if (first.Success && ulong.TryParse(first.Groups[1].Value, out ulong sid3))
                {
                    return (uint)(sid3 & 0xFFFFFFFFUL);
                }
            }
            catch { }
        }

        // Fallback: look at folders in steamRoot\userdata\
        string userdataDir = Path.Combine(steamRoot, "userdata");
        if (Directory.Exists(userdataDir))
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(userdataDir))
                {
                    string folderName = Path.GetFileName(dir);
                    if (uint.TryParse(folderName, out uint id) && id > 0)
                    {
                        if (Directory.Exists(Path.Combine(dir, "config", "librarycache")) ||
                            File.Exists(Path.Combine(dir, "config", "localconfig.vdf")))
                        {
                            return id;
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Enumerates all game AppIDs owned by the user via local Steam library cache & config files.
    /// </summary>
    public HashSet<long> EnumerateOwnedAppIds(string? steamRoot = null, uint? accountId = null)
    {
        var result = new HashSet<long>();
        steamRoot ??= steam.EffectivePath;
        if (steamRoot is null) return result;

        accountId ??= GetActiveAccountId(steamRoot);

        // 1. Scan userdata\<accountId>\config\librarycache\*.json
        if (accountId.HasValue)
        {
            string cacheDir = Path.Combine(steamRoot, "userdata", accountId.Value.ToString(), "config", "librarycache");
            if (Directory.Exists(cacheDir))
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(cacheDir, "*.json"))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(file);
                        if (long.TryParse(baseName, out long appId) && appId > 0)
                        {
                            result.Add(appId);
                        }
                    }
                }
                catch { }
            }

            // 2. Scan userdata\<accountId>\config\localconfig.vdf
            string localConfig = Path.Combine(steamRoot, "userdata", accountId.Value.ToString(), "config", "localconfig.vdf");
            if (File.Exists(localConfig))
            {
                try
                {
                    string text = File.ReadAllText(localConfig);
                    var matches = Regex.Matches(text, @"""(\d+)""\s*\{");
                    foreach (Match m in matches)
                    {
                        if (long.TryParse(m.Groups[1].Value, out long id) && id > 10)
                        {
                            result.Add(id);
                        }
                    }
                }
                catch { }
            }
        }

        // 3. Include any games currently installed in Steam
        foreach (var inst in EnumerateInstalled())
        {
            result.Add(inst.AppId);
        }

        return result;
    }

    /// <summary>
    /// Enumerates all original owned games for the user, matching with their installed state.
    /// </summary>
    public IEnumerable<OwnedGame> EnumerateOwnedGames()
    {
        string? steamRoot = steam.EffectivePath;
        if (steamRoot is null) yield break;

        var installedMap = new Dictionary<long, InstalledGame>();
        foreach (var inst in EnumerateInstalled())
        {
            installedMap[inst.AppId] = inst;
        }

        var ownedIds = EnumerateOwnedAppIds(steamRoot);

        foreach (var kvp in installedMap)
        {
            ownedIds.Add(kvp.Key);
        }

        foreach (long appId in ownedIds)
        {
            if (installedMap.TryGetValue(appId, out var inst))
            {
                yield return new OwnedGame(appId, inst.Name, true, inst.InstallDir);
            }
            else
            {
                yield return new OwnedGame(appId, appId.ToString(), false, null);
            }
        }
    }

    /// <summary>VDF strings escape backslashes as "\\"; collapse to a real path.</summary>
    private static string Unescape(string s) => s.Replace(@"\\", @"\");
}

