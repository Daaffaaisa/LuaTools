using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using LuaToolsGui.Models;

namespace LuaToolsGui.Services;

public class GcwScraperService
{
    private readonly HttpClient _http;

    public GcwScraperService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GcwFixItem>> SearchFixesAsync(string gameTitle)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://gamecopyworld.com/games/gcw_index.shtml?search=" + Uri.EscapeDataString(gameTitle));
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Referer", "https://gamecopyworld.com/");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var results = new List<GcwFixItem>();

        var titleMatch = Regex.Match(html, @"<td class=""item-title""><a href=""([^""]+)"">([^<]+)</a></td>");
        var dateMatch = Regex.Match(html, @"<td class=""item-date"">([^<]+)</td>");
        var sizeMatch = Regex.Match(html, @"<td class=""item-size"">([^<]+)</td>");

        if (titleMatch.Success && dateMatch.Success && sizeMatch.Success)
        {
            var url = titleMatch.Groups[1].Value;
            if (url.StartsWith("/")) url = "https://gamecopyworld.com" + url;
            
            results.Add(new GcwFixItem
            {
                Title = titleMatch.Groups[2].Value,
                DateLabel = dateMatch.Groups[1].Value,
                Size = sizeMatch.Groups[1].Value,
                MirrorPageUrl = url
            });
        }

        return results;
    }

    public async Task<string> GetDirectDownloadUrlAsync(string mirrorPageUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, mirrorPageUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Referer", "https://gamecopyworld.com/");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(html, @"href=""([^""]+\.(?:rar|zip|7z))""", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new Exception("Direct download link not found on the mirror page.");
    }
}
