using System;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string html = @"<a href='enable_javascript.shtml' onMouseDown=""cbox('https://dl.gamecopyworld.com/?c=19330&amp;b=0&amp;a=0&amp;d=2026&amp;f=Crimson.Desert.v1.0-v1.16.Plus.12.Trainer-FLiNG!rar' ); return false;"">";
        var dlMatches = Regex.Matches(html, @"cbox\('([^']+dl\.gamecopyworld\.com[^']+)'\s*\)", RegexOptions.IgnoreCase);
        foreach (Match m in dlMatches)
        {
            string url = m.Groups[1].Value.Replace("&amp;", "&");
            var fileMatch = Regex.Match(url, @"&f=([^!&]+)");
            if (fileMatch.Success)
            {
                Console.WriteLine("Title: " + fileMatch.Groups[1].Value.Replace(".", " "));
            }
        }
    }
}
