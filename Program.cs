using System;
using System.Net.Http;
using System.Text.RegularExpressions;

class Program
{
    static async System.Threading.Tasks.Task Main()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.Add("Referer", "https://gamecopyworld.com/");
        var res = await client.GetAsync("https://gamecopyworld.com/games/gcw_index.shtml");
        var indexHtml = await res.Content.ReadAsStringAsync();

        string[] testGames = { "LEGO Batman", "Stellar Blade", "Crimson Desert", "Call of Duty" };
        
        foreach (var title in testGames)
        {
            var match = Regex.Match(indexHtml, $@"<a href=""([^""]+)"">[^<]*{Regex.Escape(title)}[^<]*</a>", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Console.WriteLine($"[PASS] {title} -> {match.Groups[1].Value}");
            }
            else
            {
                Console.WriteLine($"[FAIL] {title}");
                // Let's see what IS in the HTML for this game
                var snippets = Regex.Matches(indexHtml, $@".{{0,30}}{Regex.Escape(title)}.{{0,30}}", RegexOptions.IgnoreCase);
                foreach (Match snippet in snippets)
                {
                    Console.WriteLine($"       Found Snippet: {snippet.Value.Replace("\n", " ")}");
                }
            }
        }
    }
}
