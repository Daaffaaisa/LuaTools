using System.Text.RegularExpressions;

namespace LuaToolsGui.Services;

public static class GameTitleSanitizer
{
    public static string Sanitize(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return title;

        string result = title.Replace("®", "").Replace("™", "").Replace("©", "");
        result = Regex.Replace(result, @" - Game of the Year Edition.*$", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @" Complete Edition.*$", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s*\(\d{4}\)$", "");

        // Aggressive GCW sanitization: GCW's search is wildcard based.
        // If a title has a colon or a dash, taking just the first part usually yields the best search results.
        var colonIndex = result.IndexOf(':');
        if (colonIndex > 0) result = result.Substring(0, colonIndex);

        var dashIndex = result.IndexOf('-');
        if (dashIndex > 0) result = result.Substring(0, dashIndex);

        return result.Trim();
    }
}
