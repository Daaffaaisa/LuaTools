using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;

class Program
{
    static async System.Threading.Tasks.Task Main()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.Add("Referer", "https://gamecopyworld.com/");

        string[] testGames = { "LEGO Batman", "Stellar Blade", "Crimson Desert", "Call of Duty" };
        
        foreach (var title in testGames)
        {
            string indexFile = "gcw_index.shtml";
            char firstChar = char.ToUpperInvariant(title.Trim().FirstOrDefault());
            if (firstChar >= 'F' && firstChar <= 'M') indexFile = "gcw_index_2.shtml";
            else if (firstChar >= 'N' && firstChar <= 'S') indexFile = "gcw_index_3.shtml";
            else if (firstChar >= 'T' && firstChar <= 'Z') indexFile = "gcw_index_4.shtml";

            var res = await client.GetAsync($"https://gamecopyworld.com/games/{indexFile}");
            var indexHtml = await res.Content.ReadAsStringAsync();

            var match = Regex.Match(indexHtml, $@"<a href=""([^""]+)"">[^<]*{Regex.Escape(title)}[^<]*</a>", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Console.WriteLine($"[PASS] {title} -> {match.Groups[1].Value} (in {indexFile})");
            }
            else
            {
                Console.WriteLine($"[FAIL] {title} (in {indexFile})");
            }
        }
    }
}
