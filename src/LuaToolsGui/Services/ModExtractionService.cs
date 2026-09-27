using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace LuaToolsGui.Services;

public class ModGameRule
{
    public uint AppId { get; set; }
    public string RelativeTargetFolder { get; set; } = string.Empty;
}

public class ModExtractionService
{
    private readonly SteamLibraryService _steamLibrary;

    // Kamus Aturan Game (Bisa ditambah ke depannya lewat file JSON, sekarang di-hardcode untuk MVP)
    private readonly Dictionary<string, ModGameRule> _rules = new(StringComparer.OrdinalIgnoreCase)
    {
        { "stardewvalley", new ModGameRule { AppId = 413150, RelativeTargetFolder = "Mods" } },
        { "skyrimspecialedition", new ModGameRule { AppId = 489830, RelativeTargetFolder = "Data" } },
        { "cyberpunk2077", new ModGameRule { AppId = 1091500, RelativeTargetFolder = "" } },
        { "palworld", new ModGameRule { AppId = 1623730, RelativeTargetFolder = @"Pal\Content\Paks\~mods" } },
        { "fallout4", new ModGameRule { AppId = 377160, RelativeTargetFolder = "Data" } }
    };

    private readonly SettingsService _settings;

    public ModExtractionService(SteamLibraryService steamLibrary, SettingsService settings)
    {
        _steamLibrary = steamLibrary;
        _settings = settings;
    }

    /// <summary>
    /// Mengekstrak file ZIP mod ke folder tujuan yang benar sesuai aturan game.
    /// Returns (Success, ErrorMessage, TargetFolder)
    /// </summary>
    public async Task<(bool Success, string? Error, string? ExtractPath, System.Collections.Generic.List<string>? ExtractedFiles)> ExtractModAsync(string zipFilePath, string gameDomain, string? targetDirOverride = null)
    {
        return await Task.Run<(bool Success, string? Error, string? ExtractPath, System.Collections.Generic.List<string>? ExtractedFiles)>(() =>
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
                        return (false, $"Game belum di-install atau tidak ditemukan oleh Steam (AppID: {rule.AppId}).", null, null);
                    }
                    targetDir = Path.Combine(installDir, rule.RelativeTargetFolder);
                }
                else
                {
                    return (false, "NEEDS_SETUP", null, null);
                }
                
                // --- SMART ZIP ANALYZER (Overlap Detection) ---
                // Cek apakah zip memuat folder yang overlap dengan targetDir
                // Misal Target: .../Game/Content/Paks/~mods
                // Zip berisi: Game/Content/Paks/~mods/mod.pak
                // Kita akan memundurkan target extraction ke root game agar tidak double-nesting!
                
                string finalExtractDir = targetDir;
                try
                {
                    using var archive = ZipFile.OpenRead(zipFilePath);
                    string[] targetParts = targetDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    bool overlapFound = false;

                    // Periksa setiap file/folder di dalam zip sampai kita menemukan overlap
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.Contains('/')) continue;
                        
                        var zipParts = entry.FullName.Split('/');
                        string topZipFolder = zipParts[0];

                        // Cari dari kanan ke kiri pada target directory
                        for (int i = targetParts.Length - 1; i >= 0; i--)
                        {
                            if (string.Equals(targetParts[i], topZipFolder, StringComparison.OrdinalIgnoreCase))
                            {
                                bool isFullMatch = true;
                                int zipIndex = 0;
                                
                                for (int j = i; j < targetParts.Length; j++)
                                {
                                    if (zipIndex >= zipParts.Length || 
                                        !string.Equals(targetParts[j], zipParts[zipIndex], StringComparison.OrdinalIgnoreCase))
                                    {
                                        isFullMatch = false;
                                        break;
                                    }
                                    zipIndex++;
                                }

                                if (isFullMatch)
                                {
                                    // Overlap terdeteksi! Mundurkan targetDir.
                                    var newParts = new string[i];
                                    Array.Copy(targetParts, newParts, i);
                                    
                                    string newTarget = string.Join(Path.DirectorySeparatorChar.ToString(), newParts);
                                    if (newParts.Length == 1 && newParts[0].EndsWith(":"))
                                    {
                                        newTarget += Path.DirectorySeparatorChar;
                                    }
                                    
                                    finalExtractDir = newTarget;
                                    overlapFound = true;
                                    break;
                                }
                            }
                        }
                        if (overlapFound) break;
                    }
                }
                catch { /* Abaikan jika error baca zip, lanjut ekstrak normal */ }

                Directory.CreateDirectory(finalExtractDir);

                var extractedFiles = new System.Collections.Generic.List<string>();
                
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Skip directories
                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\")) continue;
                        
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

                return (true, null, finalExtractDir, extractedFiles);
            }
            catch (Exception ex)
            {
                return (false, $"Gagal mengekstrak: {ex.Message}", null, null);
            }
        });
    }
}
