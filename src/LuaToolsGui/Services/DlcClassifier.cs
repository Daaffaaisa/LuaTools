using System;
using System.Text.RegularExpressions;

namespace LuaToolsGui.Services;

/// <summary>
/// Helper to classify Steam DLC entries into Soundtracks, Artbooks/Bonuses, or Standard Game DLCs.
/// </summary>
public static partial class DlcClassifier
{
    // Matches standalone words: soundtrack, ost, score, music, bgm (e.g. "Game - OST", "(OST)", "Original Soundtrack")
    [GeneratedRegex(@"\b(soundtrack|ost|score|music|bgm)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SoundtrackRegex();

    // Matches artbooks, wallpapers, avatars, digital art, extras, and bonus app packages
    [GeneratedRegex(@"\b(artbook|art\s*book|wallpaper|wallpapers|avatar|avatars|digital\s*art)\b|\b(bonus\s*(content|app|application|pack)|extras?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex BonusRegex();

    public static bool IsSoundtrack(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        return SoundtrackRegex().IsMatch(title);
    }

    public static bool IsBonus(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        return !IsSoundtrack(title) && BonusRegex().IsMatch(title);
    }
}
