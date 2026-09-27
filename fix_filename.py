import codecs
with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''                var (cdnLink, fileName) = await nexusService.GetDownloadLinkAsync(nxm.Game, nxm.ModId, nxm.FileId, nxm.QueryParams);
                
                if (cdnLink != null)
                {
                    toastService.Show("Mendownload Mod...", $"File: {fileName ?? "mod.zip"}\nSilakan tunggu di latar belakang.");
                    
                    // Actually download the file
                    string? savedPath = await nexusService.DownloadModFileAsync(cdnLink, fileName ?? $"mod_{nxm.ModId}_{nxm.FileId}.zip");'''

replacement = '''                var (cdnLink, _) = await nexusService.GetDownloadLinkAsync(nxm.Game, nxm.ModId, nxm.FileId, nxm.QueryParams);
                
                if (cdnLink != null)
                {
                    // Ekstrak nama asli file dari URL CDN (contoh: https://.../ModName-123.zip?md5=...)
                    string realFileName = System.IO.Path.GetFileName(new Uri(cdnLink).LocalPath);
                    if (string.IsNullOrWhiteSpace(realFileName)) realFileName = $"mod_{nxm.ModId}_{nxm.FileId}.zip";
                    
                    toastService.Show("Mendownload Mod...", $"File: {realFileName}\nSilakan tunggu di latar belakang.");
                    
                    // Actually download the file
                    string? savedPath = await nexusService.DownloadModFileAsync(cdnLink, realFileName);'''

text = text.replace(target, replacement)
with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)
