import codecs

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Change return type of ExtractModAsync
target_sig = '''    public async Task<(bool Success, string? Error, string? ExtractPath)> ExtractModAsync(string zipFilePath, string gameDomain, string? targetDirOverride = null)
    {
        return await Task.Run<(bool Success, string? Error, string? ExtractPath)>(() =>'''

replacement_sig = '''    public async Task<(bool Success, string? Error, string? ExtractPath, System.Collections.Generic.List<string>? ExtractedFiles)> ExtractModAsync(string zipFilePath, string gameDomain, string? targetDirOverride = null)
    {
        return await Task.Run<(bool Success, string? Error, string? ExtractPath, System.Collections.Generic.List<string>? ExtractedFiles)>(() =>'''

text = text.replace(target_sig, replacement_sig)


# Change return values in error cases
target_err1 = '''return (false, $"Game belum di-install atau tidak ditemukan oleh Steam (AppID: {rule.AppId}).", null);'''
replacement_err1 = '''return (false, $"Game belum di-install atau tidak ditemukan oleh Steam (AppID: {rule.AppId}).", null, null);'''
text = text.replace(target_err1, replacement_err1)

target_err2 = '''return (false, "NEEDS_SETUP", null);'''
replacement_err2 = '''return (false, "NEEDS_SETUP", null, null);'''
text = text.replace(target_err2, replacement_err2)

target_err3 = '''return (false, $"Gagal mengekstrak: {ex.Message}", null);'''
replacement_err3 = '''return (false, $"Gagal mengekstrak: {ex.Message}", null, null);'''
text = text.replace(target_err3, replacement_err3)


# Track files during extraction
target_extract = '''                Directory.CreateDirectory(finalExtractDir);

                // Ekstrak Zip-nya (Timpa file kalau sudah ada)
                ZipFile.ExtractToDirectory(zipFilePath, finalExtractDir, overwriteFiles: true);

                return (true, null, finalExtractDir);'''

replacement_extract = '''                Directory.CreateDirectory(finalExtractDir);

                var extractedFiles = new System.Collections.Generic.List<string>();
                
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Skip directories
                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\\\")) continue;
                        
                        string destinationPath = Path.GetFullPath(Path.Combine(finalExtractDir, entry.FullName));
                        
                        // Security check to prevent Zip Slip vulnerability
                        if (destinationPath.StartsWith(finalExtractDir, StringComparison.OrdinalIgnoreCase))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            entry.ExtractToFile(destinationPath, overwrite: true);
                            extractedFiles.Add(destinationPath);
                        }
                    }
                }

                return (true, null, finalExtractDir, extractedFiles);'''

text = text.replace(target_extract, replacement_extract)

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("ModExtractionService modified to return ExtractedFiles")
