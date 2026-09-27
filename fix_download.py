import codecs
import re

# 1. Update NexusModsService.cs
with open('src/LuaToolsGui/Services/NexusModsService.cs', 'r', encoding='utf-8') as f:
    nexus_text = f.read()

nexus_target = '''    public async Task<string?> DownloadModFileAsync(string cdnUrl, string fileName)
    {
        try
        {
            // Ensure filename is safe and has a value
            string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "mod_download.zip" : string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            string destinationPath = Path.Combine(ModsCacheDirectory, safeFileName);

            using var response = await _http.GetAsync(cdnUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();'''

nexus_replacement = '''    public async Task<string?> DownloadModFileAsync(string cdnUrl)
    {
        try
        {
            using var response = await _http.GetAsync(cdnUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Extract from Content-Disposition header
            string? headerFileName = response.Content.Headers.ContentDisposition?.FileNameStar 
                                  ?? response.Content.Headers.ContentDisposition?.FileName;
            
            if (!string.IsNullOrEmpty(headerFileName))
                headerFileName = headerFileName.Trim('"', '\'');
            
            // Fallback to URL path or default
            if (string.IsNullOrWhiteSpace(headerFileName) || !headerFileName.Contains('.'))
            {
                headerFileName = Path.GetFileName(new Uri(cdnUrl).LocalPath);
                if (string.IsNullOrWhiteSpace(headerFileName) || !headerFileName.Contains('.'))
                    headerFileName = "mod_download.zip";
            }

            string safeFileName = string.Join("_", headerFileName.Split(Path.GetInvalidFileNameChars()));
            string destinationPath = Path.Combine(ModsCacheDirectory, safeFileName);'''

nexus_text = nexus_text.replace(nexus_target, nexus_replacement)
with open('src/LuaToolsGui/Services/NexusModsService.cs', 'w', encoding='utf-8') as f:
    f.write(nexus_text)


# 2. Update App.xaml.cs
with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    app_text = f.read()

app_target = '''                if (cdnLink != null)
                {
                    // Ekstrak nama asli file dari URL CDN (contoh: https://.../ModName-123.zip?md5=...)
                    string realFileName = System.IO.Path.GetFileName(new Uri(cdnLink).LocalPath);
                    if (string.IsNullOrWhiteSpace(realFileName)) realFileName = $"mod_{nxm.ModId}_{nxm.FileId}.zip";
                    
                    toastService.Show("Mendownload Mod...", $"File: {realFileName}\\nSilakan tunggu di latar belakang.");
                    
                    // Actually download the file
                    string? savedPath = await nexusService.DownloadModFileAsync(cdnLink, realFileName);'''

app_replacement = '''                if (cdnLink != null)
                {
                    toastService.Show("Mendownload Mod...", $"Mod ID {nxm.ModId}\\nSilakan tunggu di latar belakang.");
                    
                    // Actually download the file
                    string? savedPath = await nexusService.DownloadModFileAsync(cdnLink);'''

app_text = app_text.replace(app_target, app_replacement)
with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(app_text)

print("Patch applied")
