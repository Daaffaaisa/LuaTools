using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuaToolsGui.Services;

namespace LuaToolsGui.ViewModels;

public partial class LeftoverItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true; // Checked by default
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public string SizeString { get; set; } = "";
    public string Description { get; set; } = "";
    public long SizeBytes { get; set; }
}

public partial class LeftoverCleanerViewModel : ObservableObject
{
    private readonly LuaTileViewModel _game;
    private readonly AppOverview? _overview;
    
    [ObservableProperty] private ObservableCollection<LeftoverItemViewModel> _leftovers = new();
    [ObservableProperty] private string _statusMessage = "Mencari sisa file/mod/save...";
    [ObservableProperty] private string _gameName = "";
    [ObservableProperty] private bool _isScanning = false;

    public Action? CloseWindow { get; set; }

    public LeftoverCleanerViewModel(LuaTileViewModel game, AppOverview? overview)
    {
        _game = game;
        _overview = overview;
        GameName = game.Name;
        ScanLeftoversAsync();
    }

    private async void ScanLeftoversAsync()
    {
        IsScanning = true;
        Leftovers.Clear();

        await Task.Run(() =>
        {
            try
            {
                // Normalisasi nama game (hapus tanda baca dan SPASI agar lebih kebal typo)
                string rawGame = _game.Name ?? "";
                string cleanGame = System.Text.RegularExpressions.Regex.Replace(rawGame, "[^a-zA-Z0-9]", "").ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(cleanGame) || cleanGame.Length < 3) return;

                // Cek studio / publisher (untuk game seperti Pearl Abyss -> Crimson Desert)
                string[] studios = _overview != null 
                    ? _overview.Developers.Concat(_overview.Publishers).Distinct().ToArray() 
                    : Array.Empty<string>();

                // 1. Cek folder instalasi Steam
                string steamRoot = @"C:\Program Files (x86)\Steam";
                string steamApps = Path.Combine(steamRoot, "steamapps");
                string steamCommon = Path.Combine(steamApps, "common");
                
                bool isInstalledInSteam = false;
                if (_game.AppId > 0)
                {
                    string manifest = Path.Combine(steamApps, $"appmanifest_{_game.AppId}.acf");
                    if (File.Exists(manifest)) isInstalledInSteam = true;
                }

                if (!isInstalledInSteam && Directory.Exists(steamCommon))
                {
                    var dirs = Directory.GetDirectories(steamCommon);
                    foreach (var d in dirs)
                    {
                        string folderName = Path.GetFileName(d);
                        string cleanFolder = System.Text.RegularExpressions.Regex.Replace(folderName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
                        
                        if (cleanFolder.Length < 3) continue;

                        if (cleanFolder == cleanGame || cleanGame.StartsWith(cleanFolder) || cleanFolder.StartsWith(cleanGame))
                        {
                            AddLeftoverItem(d, "Sisa Instalasi Game / Mod (Aman dihapus karena game sudah Uninstalled)");
                        }
                    }
                }

                // 2. Cek Steam UserData (Saves Cloud)
                string steamUserData = Path.Combine(steamRoot, "userdata");
                if (Directory.Exists(steamUserData) && _game.AppId > 0)
                {
                    string appIdStr = _game.AppId.ToString();
                    var userDirs = Directory.GetDirectories(steamUserData);
                    foreach (var userDir in userDirs)
                    {
                        string targetSave = Path.Combine(userDir, appIdStr);
                        if (Directory.Exists(targetSave))
                        {
                            AddLeftoverItem(targetSave, "Steam Cloud Data (Config/Saves)");
                        }
                    }
                }

                // Bikin Singkatan (Acronym), misal "Crimson Desert" -> "cd", "Grand Theft Auto V" -> "gtav"
                string acronym = "";
                var words = rawGame.Split(new char[] { ' ', ':', '-', '.', '_' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words)
                {
                    if (char.IsLetterOrDigit(w[0])) acronym += char.ToLowerInvariant(w[0]);
                }

                // 3. Cek AppData Local, Roaming, LocalLow, dan Saved Games
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] appDatas = {
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Path.Combine(userProfile, "AppData", "LocalLow"),
                    Path.Combine(userProfile, "Saved Games")
                };
                
                foreach (var appData in appDatas)
                {
                    if (Directory.Exists(appData))
                    {
                        var dirs = Directory.GetDirectories(appData);
                        foreach (var d in dirs)
                        {
                            string folderName = Path.GetFileName(d);
                            string cleanFolder = System.Text.RegularExpressions.Regex.Replace(folderName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
                            
                            // Cek nama game langsung (Level 0) atau singkatan (Acronym)
                            bool matchLevel0 = (cleanFolder.Length > 2 && (cleanFolder == cleanGame || cleanGame.StartsWith(cleanFolder) || cleanFolder.StartsWith(cleanGame))) ||
                                               (acronym.Length >= 2 && cleanFolder.Length >= 2 && (cleanFolder == acronym || acronym.StartsWith(cleanFolder) || cleanFolder.StartsWith(acronym)));

                            if (matchLevel0)
                            {
                                AddLeftoverItem(d, "AppData/Saved Games");
                                continue; // Skip sub-directories if parent matches
                            }

                            // Cek 1-level lebih dalam (Level 1)
                            try
                            {
                                var subDirs = Directory.GetDirectories(d);
                                foreach (var sub in subDirs)
                                {
                                    string subName = Path.GetFileName(sub);
                                    string cleanSub = System.Text.RegularExpressions.Regex.Replace(subName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
                                    
                                    bool matchLevel1 = (cleanSub.Length > 2 && (cleanSub == cleanGame || cleanGame.StartsWith(cleanSub) || cleanSub.StartsWith(cleanGame))) ||
                                                       (acronym.Length >= 2 && cleanSub.Length >= 2 && (cleanSub == acronym || acronym.StartsWith(cleanSub) || cleanSub.StartsWith(acronym)));

                                    if (matchLevel1)
                                    {
                                        AddLeftoverItem(sub, $"AppData/Saved Games (via {folderName})");
                                    }
                                }
                            }
                            catch { } // Abaikan error Access Denied
                        }
                    }
                }

                // 4. Cek My Documents
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Directory.Exists(docs))
                {
                    string docsGame = Path.Combine(docs, rawGame);
                    if (Directory.Exists(docsGame)) AddLeftoverItem(docsGame, "My Documents (Saves)");
                    
                    // Terkadang developer menghilangkan titik dua/spasi di My Documents
                    string docsClean = Path.Combine(docs, rawGame.Replace(":", "").Replace("'", ""));
                    if (docsClean != docsGame && Directory.Exists(docsClean)) AddLeftoverItem(docsClean, "My Documents (Saves)");
                    
                    string myGames = Path.Combine(docs, "My Games", rawGame);
                    if (Directory.Exists(myGames)) AddLeftoverItem(myGames, "My Games (Saves)");
                    
                    // Cek 1-level lebih dalam di Documents (Level 1)
                    try
                    {
                        var dirs = Directory.GetDirectories(docs);
                        foreach (var d in dirs)
                        {
                            string folderName = Path.GetFileName(d);
                            string cleanFolder = System.Text.RegularExpressions.Regex.Replace(folderName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
                            
                            bool matchLevel0 = (cleanFolder.Length > 2 && (cleanFolder == cleanGame || cleanGame.StartsWith(cleanFolder) || cleanFolder.StartsWith(cleanGame))) ||
                                               (acronym.Length >= 2 && cleanFolder.Length >= 2 && (cleanFolder == acronym || acronym.StartsWith(cleanFolder) || cleanFolder.StartsWith(acronym)));

                            // Jika match langsung
                            if (matchLevel0)
                            {
                                AddLeftoverItem(d, "My Documents");
                                continue;
                            }

                            // Masuk 1 level
                            var subDirs = Directory.GetDirectories(d);
                            foreach(var sub in subDirs)
                            {
                                string cleanSub = System.Text.RegularExpressions.Regex.Replace(Path.GetFileName(sub), "[^a-zA-Z0-9]", "").ToLowerInvariant();
                                
                                bool matchLevel1 = (cleanSub.Length > 2 && (cleanSub == cleanGame || cleanGame.StartsWith(cleanSub) || cleanSub.StartsWith(cleanGame))) ||
                                                   (acronym.Length >= 2 && cleanSub.Length >= 2 && (cleanSub == acronym || acronym.StartsWith(cleanSub) || cleanSub.StartsWith(acronym)));
                                                   
                                if (matchLevel1)
                                {
                                    AddLeftoverItem(sub, $"My Documents (via {folderName})");
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        });

        IsScanning = false;
        
        if (Leftovers.Count == 0)
            StatusMessage = "Wah! Komputermu sudah bersih. Tidak ada file sampah yang tersisa.";
        else
            StatusMessage = $"Menemukan {Leftovers.Count} lokasi sampah. Pilih yang ingin dihapus!";
    }

    private void AddLeftoverItem(string folderPath, string desc)
    {
        long size = GetDirectorySize(new DirectoryInfo(folderPath));
        if (size == 0) return; // Abaikan folder kosong

        Application.Current.Dispatcher.Invoke(() =>
        {
            // Jangan add duplicate
            if (Leftovers.Any(x => x.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase))) return;

            Leftovers.Add(new LeftoverItemViewModel
            {
                Path = folderPath,
                Name = Path.GetFileName(folderPath),
                Description = desc,
                SizeBytes = size,
                SizeString = FormatSize(size)
            });
        });
    }

    private long GetDirectorySize(DirectoryInfo d)
    {
        long size = 0;
        try
        {
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis) size += fi.Length;
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis) size += GetDirectorySize(di);
        }
        catch { }
        return size;
    }

    private string FormatSize(long bytes)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB" };
        if (bytes == 0) return "0 B";
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return $"{num} {suf[place]}";
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var toDelete = Leftovers.Where(x => x.IsSelected).ToList();
        if (toDelete.Count == 0) return;

        StatusMessage = "Sedang menghapus...";
        IsScanning = true;

        await Task.Run(() =>
        {
            foreach (var item in toDelete)
            {
                try
                {
                    if (Directory.Exists(item.Path))
                        Directory.Delete(item.Path, true);
                }
                catch { }
            }
        });

        IsScanning = false;
        StatusMessage = $"Berhasil menghapus {toDelete.Count} sampah!";
        
        // Remove from UI
        foreach (var item in toDelete) Leftovers.Remove(item);
        
        if (Leftovers.Count == 0)
            CloseWindow?.Invoke();
    }
}
