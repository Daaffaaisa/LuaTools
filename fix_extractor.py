import codecs

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''    public async Task<(bool Success, string? Error, string? ExtractPath)> ExtractModAsync(string zipFilePath, string gameDomain)
    {
        return await Task.Run<(bool Success, string? Error, string? ExtractPath)>(() =>
        {
            try
            {
                string targetDir;

                // 1. Cek apakah user pernah set folder manual
                if (_settings.ModDirectories.TryGetValue(gameDomain, out string? customPath) && Directory.Exists(customPath))
                {
                    targetDir = customPath;
                }
                // 2. Cek kamus internal (Fallback)
                else if (_rules.TryGetValue(gameDomain, out var rule))
                {
                    string? installDir = _steamLibrary.GetInstallDir(rule.AppId);
                    if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                    {
                        return (false, $"Game belum di-install atau tidak ditemukan oleh Steam (AppID: {rule.AppId}).", null);
                    }
                    targetDir = Path.Combine(installDir, rule.RelativeTargetFolder);
                    Directory.CreateDirectory(targetDir);
                }
                else
                {
                    // Kasih kode rahasia "NEEDS_SETUP" ke App.xaml.cs biar UI nampilin File Picker
                    return (false, "NEEDS_SETUP", null);
                }'''

replacement = '''    public async Task<(bool Success, string? Error, string? ExtractPath)> ExtractModAsync(string zipFilePath, string gameDomain, string? targetDirOverride = null)
    {
        return await Task.Run<(bool Success, string? Error, string? ExtractPath)>(() =>
        {
            try
            {
                string targetDir;

                if (!string.IsNullOrEmpty(targetDirOverride))
                {
                    targetDir = targetDirOverride;
                }
                else if (_rules.TryGetValue(gameDomain, out var rule))
                {
                    string? installDir = _steamLibrary.GetInstallDir(rule.AppId);
                    if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                    {
                        return (false, $"Game belum di-install atau tidak ditemukan oleh Steam (AppID: {rule.AppId}).", null);
                    }
                    targetDir = Path.Combine(installDir, rule.RelativeTargetFolder);
                }
                else
                {
                    return (false, "NEEDS_SETUP", null);
                }
                
                Directory.CreateDirectory(targetDir);'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("ModExtractionService updated")
