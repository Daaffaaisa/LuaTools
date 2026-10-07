using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async System.Threading.Tasks.Task Main()
    {
        var _httpClient = new HttpClient();
        string gameTitle = "Crimson Desert";
        
        var indexRequest = new HttpRequestMessage(HttpMethod.Get, "https://gamecopyworld.com/games/gcw_index.shtml");
        indexRequest.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        indexRequest.Headers.TryAddWithoutValidation("Referer", "https://gamecopyworld.com/");

        using var indexRes = await _httpClient.SendAsync(indexRequest, HttpCompletionOption.ResponseHeadersRead);
        var indexHtml = await indexRes.Content.ReadAsStringAsync();

        var pageMatch = Regex.Match(indexHtml, $@"<a href=""([^""]+)"">[^<]*{Regex.Escape(gameTitle)}[^<]*</a>", RegexOptions.IgnoreCase);
        if (!pageMatch.Success) 
        {
            Console.WriteLine("PAGE MATCH FAILED!");
            return;
        }

        string gameUrl = "https://gamecopyworld.com/games/" + pageMatch.Groups[1].Value;
        Console.WriteLine("Found URL: " + gameUrl);
        
        var gameReq = new HttpRequestMessage(HttpMethod.Get, gameUrl);
        gameReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        gameReq.Headers.TryAddWithoutValidation("Referer", "https://gamecopyworld.com/games/gcw_index.shtml");

        using var gameRes = await _httpClient.SendAsync(gameReq);
        var gameHtml = await gameRes.Content.ReadAsStringAsync();

        var dlMatches = Regex.Matches(gameHtml, @"cbox\('([^']+dl\.gamecopyworld\.com[^']+)'\)", RegexOptions.IgnoreCase);
        Console.WriteLine("Found DL Mirrors: " + dlMatches.Count);
        
        foreach (Match m in dlMatches)
        {
            var mirrorUrl = m.Groups[1].Value;
            var fileMatch = Regex.Match(mirrorUrl, @"&f=([^!&]+)");
            if (fileMatch.Success)
            {
                var title = Uri.UnescapeDataString(fileMatch.Groups[1].Value).Replace(".", " ");
                Console.WriteLine("Title: " + title);
            }
        }
    }
}
