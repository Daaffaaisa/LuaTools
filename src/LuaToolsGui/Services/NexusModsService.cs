using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LuaToolsGui.Services;

public class NexusDownloadLink
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("short_name")] public string? ShortName { get; set; }
    [JsonPropertyName("URI")] public string? Uri { get; set; }
}

public class NexusModsService
{
    private readonly HttpClient _http;
    private readonly SettingsService _settings;
    
    public string ModsCacheDirectory { get; }

    public NexusModsService(SettingsService settings)
    {
        _settings = settings;
        _http = new HttpClient { BaseAddress = new Uri("https://api.nexusmods.com/") };
        // We MUST pretend to be a real app
        _http.DefaultRequestHeaders.Add("Application-Name", "LuaTools");
        _http.DefaultRequestHeaders.Add("Application-Version", "1.3.1");
        
        ModsCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LuaToolsGui", "ModsCache");
        Directory.CreateDirectory(ModsCacheDirectory);
    }

    /// <summary>
    /// Fetches the direct CDN download link and filename from Nexus API.
    /// </summary>
    public async Task<(string? Url, string? FileName)> GetDownloadLinkAsync(string game, string modId, string fileId, string queryParams)
    {
        string? apiKey = _settings.NexusApiKey;
        if (string.IsNullOrEmpty(apiKey))
            return (null, null); // Missing API Key

        // queryParams is expected to be "?key=...&expires=..."
        string endpoint = $"v1/games/{game}/mods/{modId}/files/{fileId}/download_link.json{queryParams}";

        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("apikey", apiKey);

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (null, null);

            var links = await response.Content.ReadFromJsonAsync<List<NexusDownloadLink>>();
            var bestLink = links?.FirstOrDefault();
            
            return (bestLink?.Uri, bestLink?.ShortName);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Downloads the mod from the CDN URL to the ModsCacheDirectory.
    /// </summary>
    public async Task<string?> DownloadModFileAsync(string cdnUrl)
    {
        try
        {
            using var response = await _http.GetAsync(cdnUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Extract from Content-Disposition header
            string? headerFileName = response.Content.Headers.ContentDisposition?.FileNameStar 
                                  ?? response.Content.Headers.ContentDisposition?.FileName;
            
            if (!string.IsNullOrEmpty(headerFileName))
                headerFileName = headerFileName.Trim('"');
            
            // Fallback to URL path or default
            if (string.IsNullOrWhiteSpace(headerFileName) || !headerFileName.Contains('.'))
            {
                headerFileName = Path.GetFileName(new Uri(cdnUrl).LocalPath);
                if (string.IsNullOrWhiteSpace(headerFileName) || !headerFileName.Contains('.'))
                    headerFileName = "mod_download.zip";
            }

            string safeFileName = string.Join("_", headerFileName.Split(Path.GetInvalidFileNameChars()));
            string destinationPath = Path.Combine(ModsCacheDirectory, safeFileName);

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            await stream.CopyToAsync(fileStream);

            return destinationPath;
        }
        catch
        {
            return null;
        }
    }
}
