using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LuaToolsGui.Models;

namespace LuaToolsGui.Services;

public class GcwScraperService
{
    private readonly HttpClient _httpClient;

    public GcwScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GcwFixItem>> SearchFixesAsync(string gameTitle)
    {
        // 1. Fetch GCW index
        var indexRequest = new HttpRequestMessage(HttpMethod.Get, "https://gamecopyworld.com/games/gcw_index.shtml");
        indexRequest.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        indexRequest.Headers.TryAddWithoutValidation("Referer", "https://gamecopyworld.com/");

        using var indexRes = await _httpClient.SendAsync(indexRequest, HttpCompletionOption.ResponseHeadersRead);
        indexRes.EnsureSuccessStatusCode();
        var indexHtml = await indexRes.Content.ReadAsStringAsync();

        // 2. Find game page link
        var pageMatch = Regex.Match(indexHtml, $@"<a href=""([^""]+)"">[^<]*{Regex.Escape(gameTitle)}[^<]*</a>", RegexOptions.IgnoreCase);
        if (!pageMatch.Success) return new List<GcwFixItem>();

        string gameUrl = "https://gamecopyworld.com/games/" + pageMatch.Groups[1].Value;
        
        // 3. Fetch game page
        var gameReq = new HttpRequestMessage(HttpMethod.Get, gameUrl);
        gameReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        gameReq.Headers.TryAddWithoutValidation("Referer", "https://gamecopyworld.com/games/gcw_index.shtml");

        using var gameRes = await _httpClient.SendAsync(gameReq);
        gameRes.EnsureSuccessStatusCode();
        var gameHtml = await gameRes.Content.ReadAsStringAsync();

        var results = new List<GcwFixItem>();
        // cbox('https://dl.gamecopyworld.com/?c=19330&d=2026&f=Crimson.Desert.v1.0.Trainer-FLiNG!rar')
        var dlMatches = Regex.Matches(gameHtml, @"cbox\('([^']+dl\.gamecopyworld\.com[^']+)'\)", RegexOptions.IgnoreCase);

        foreach (Match m in dlMatches)
        {
            var mirrorUrl = m.Groups[1].Value;
            var fileMatch = Regex.Match(mirrorUrl, @"&f=([^!&]+)");
            if (fileMatch.Success)
            {
                var title = Uri.UnescapeDataString(fileMatch.Groups[1].Value).Replace(".", " ");
                if (!results.Any(x => x.Title == title))
                {
                    results.Add(new GcwFixItem {
                        Title = title,
                        DateLabel = "Unknown",
                        Size = "Unknown",
                        MirrorPageUrl = mirrorUrl
                    });
                }
            }
        }

        return results;
    }

    public async Task<string> GetDirectDownloadUrlAsync(string mirrorPageUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, mirrorPageUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        request.Headers.TryAddWithoutValidation("Referer", mirrorPageUrl);
        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        var html = await response.Content.ReadAsStringAsync();
        
        // <a href="https://g1.gamecopyworld.com/?y=..." rel="nofollow">MIRROR #01</a>
        var match = Regex.Match(html, @"href=""(https?://g\d+\.gamecopyworld\.com[^""]+)""", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        throw new Exception("Direct download link not found on the mirror page.");
    }
}
