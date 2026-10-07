using System.Text.RegularExpressions;

namespace LuaToolsGui.Services;

public static class GameTitleSanitizer
{
    public static string Sanitize(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return title;

        // Remove trademark/copyright symbols
        string result = title.Replace("®", "").Replace("™", "").Replace("©", "");

        // Remove common editions and suffixes
        result = Regex.Replace(result, @" - Game of the Year Edition.*$", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @" Complete Edition.*$", "", RegexOptions.IgnoreCase);
        
        // Remove trailing dates like (2023)
        result = Regex.Replace(result, @"\s*\(\d{4}\)$", "");

        return result.Trim();
    }
}
