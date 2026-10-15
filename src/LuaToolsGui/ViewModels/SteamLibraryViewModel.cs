using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuaToolsGui.Models;
using LuaToolsGui.Services;
using LuaToolsGui.Views;

namespace LuaToolsGui.ViewModels;

public enum SteamLibraryFilter
{
    All,
    Injected,
    Vanilla
}

public partial class SteamLibraryViewModel : ObservableObject
{
    private readonly SteamLibraryService? _library;
    private readonly SteamService? _steam;
    private readonly SteamDepotInfo? _depotInfo;
    private readonly SteamAppListCache? _appList;
    private readonly SteamAppInfoCache? _appInfo;
    private readonly CoverCache? _covers;
    private readonly ToastService? _toast;
    private readonly SteamlessService? _steamless;

    private List<SteamLibraryItemVm> _allGames = [];
    private CancellationTokenSource? _coverCts;

    [ObservableProperty]
    private ObservableCollection<SteamLibraryItemVm> _filteredGames = [];

    [ObservableProperty]
    private SteamLibraryItemVm? _selectedGame;

    [ObservableProperty]
    private bool _isFlyoutOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private SteamLibraryFilter _currentFilter = SteamLibraryFilter.All;

    [ObservableProperty]
    private int _totalGamesCount;

    [ObservableProperty]
    private int _injectedCount;

    [ObservableProperty]
    private int _vanillaCount;

    public bool IsFilterAll => CurrentFilter == SteamLibraryFilter.All;
    public bool IsFilterInjected => CurrentFilter == SteamLibraryFilter.Injected;
    public bool IsFilterVanilla => CurrentFilter == SteamLibraryFilter.Vanilla;

    // Action delegates wired in App.xaml.cs
    public Action<long, string>? OpenLaunchOptions { get; set; }
    public Action<long, string>? OpenAchievements { get; set; }
    public Action<SteamLibraryItemVm>? OpenModSettings { get; set; }
    public Action<SteamLibraryItemVm>? OpenStorageMover { get; set; }
    public Action<SteamLibraryItemVm>? OpenLeftoverCleaner { get; set; }
    public Action<long>? NavigateToBuilds { get; set; }

    /// <summary>Parameterless constructor for testing / design-time</summary>
    public SteamLibraryViewModel()
    {
    }

    public SteamLibraryViewModel(
        SteamLibraryService library,
        SteamService steam,
        SteamDepotInfo depotInfo,
        SteamAppListCache appList,
        SteamAppInfoCache appInfo,
        CoverCache covers,
        ToastService toast,
        SteamlessService steamless)
    {
        _library = library;
        _steam = steam;
        _depotInfo = depotInfo;
        _appList = appList;
        _appInfo = appInfo;
        _covers = covers;
        _toast = toast;
        _steamless = steamless;
    }

    public void SetTestGames(IEnumerable<SteamLibraryItemVm> games)
    {
        _allGames = games.ToList();
        UpdateCounts();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnCurrentFilterChanged(SteamLibraryFilter value)
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterInjected));
        OnPropertyChanged(nameof(IsFilterVanilla));
        ApplyFilter();
    }

    [RelayCommand]
    private void SetFilter(string filterName)
    {
        CurrentFilter = filterName switch
        {
            "Injected" => SteamLibraryFilter.Injected,
            "Vanilla" => SteamLibraryFilter.Vanilla,
            _ => SteamLibraryFilter.All
        };
    }

    public void ApplyFilter()
    {
        string q = SearchText.Trim();
        var query = _allGames.AsEnumerable();

        if (CurrentFilter == SteamLibraryFilter.Injected)
            query = query.Where(g => g.IsDlcInjected);
        else if (CurrentFilter == SteamLibraryFilter.Vanilla)
            query = query.Where(g => g.IsVanilla);

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(g =>
                g.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                g.AppId.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        FilteredGames = new ObservableCollection<SteamLibraryItemVm>(query.OrderBy(g => g.Name));
    }

    private void UpdateCounts()
    {
        TotalGamesCount = _allGames.Count;
        InjectedCount = _allGames.Count(g => g.IsDlcInjected);
        VanillaCount = _allGames.Count(g => g.IsVanilla);
    }

    [RelayCommand]
    public async Task ScanLibraryAsync()
    {
        if (_library == null || _steam == null) return;

        IsLoading = true;
        try
        {
            if (_appList != null)
            {
                await _appList.EnsureLoadedAsync();
            }

            string stPlugIn = _steam.StPlugInDir;

            var games = await Task.Run(() =>
            {
                var list = new List<SteamLibraryItemVm>();
                foreach (var installed in _library.EnumerateInstalled())
                {
                    string name = installed.Name;
                    if (_appList != null && (_appList.GetName(installed.AppId) is { } realName))
                    {
                        name = realName;
                    }

                    long size = 0;
                    try
                    {
                        if (Directory.Exists(installed.InstallDir))
                        {
                            var di = new DirectoryInfo(installed.InstallDir);
                            size = di.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(f => f.Length);
                        }
                    }
                    catch { }

                    var item = new SteamLibraryItemVm(installed.AppId, name, installed.InstallDir, stPlugIn, size)
                    {
                        FormattedSize = size > 0 ? FormatSize(size) : ""
                    };
                    list.Add(item);
                }
                return list;
            });

            _allGames = games;
            UpdateCounts();
            ApplyFilter();

            StartCoverPrefetch();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartCoverPrefetch()
    {
        if (_covers == null || _appInfo == null) return;

        _coverCts?.Cancel();
        _coverCts = new CancellationTokenSource();
        var ct = _coverCts.Token;

        _ = Task.Run(async () =>
        {
            foreach (var game in _allGames)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    string? path = await LuaTileViewModel.ResolveCoverFileAsync(game.AppId, _appInfo, _covers);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var img = await Task.Run(() => LuaTileViewModel.LoadFrozen(path));
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            game.CoverPath = path;
                            if (img != null) game.Cover = img;
                        });
                    }
                }
                catch { }
            }
        }, ct);
    }

    [RelayCommand]
    private void SelectGame(SteamLibraryItemVm? game)
    {
        SelectedGame = game;
        IsFlyoutOpen = game != null;
    }

    [RelayCommand]
    private void CloseFlyout()
    {
        IsFlyoutOpen = false;
        SelectedGame = null;
    }

    [RelayCommand]
    private async Task UnlockAllDlcsAsync(SteamLibraryItemVm? item)
    {
        if (item == null || _depotInfo == null) return;

        _toast?.Show("Steam Library", $"Fetching DLCs for {item.Name}...");

        try
        {
            var info = await _depotInfo.GetAsync(item.AppId);
            if (info == null)
            {
                _toast?.Show("DLC Fetch Failed", "Could not fetch DLC data from Steam/SteamDB.", error: true);
                return;
            }

            var dlcMap = new Dictionary<long, string?>();

            // Collect all DLC IDs from depots and DlcIds
            foreach (var d in info.Depots.Where(x => x.DlcAppId != null))
            {
                long dlcId = d.DlcAppId!.Value;
                string? name = _appList?.GetName(dlcId) ?? _appInfo?.GetCached(dlcId)?.Name;
                dlcMap[dlcId] = name;
            }

            foreach (long dlcId in info.DlcIds)
            {
                if (!dlcMap.ContainsKey(dlcId))
                {
                    string? name = _appList?.GetName(dlcId) ?? _appInfo?.GetCached(dlcId)?.Name;
                    dlcMap[dlcId] = name;
                }
            }

            // Exclude soundtracks & bonuses
            var dlcsToAdd = dlcMap
                .Where(kv =>
                {
                    string title = kv.Value ?? "";
                    return !DlcClassifier.IsSoundtrack(title) && !DlcClassifier.IsBonus(title);
                })
                .Select(kv => (DlcId: kv.Key, Name: kv.Value))
                .ToList();

            if (dlcsToAdd.Count == 0)
            {
                _toast?.Show(item.Name, "No game DLCs found for this title.");
                return;
            }

            item.CreateOrUpdateLua(dlcsToAdd);
            UpdateCounts();
            ApplyFilter();

            _toast?.Show(item.Name, $"{dlcsToAdd.Count} DLC(s) unlocked!");
            PromptRestartSteam();
        }
        catch (Exception ex)
        {
            _toast?.Show("Error", "Failed to unlock DLCs: " + ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task CustomizeDlcsAsync(SteamLibraryItemVm? item)
    {
        if (item == null || _depotInfo == null) return;

        _toast?.Show("Steam Library", $"Loading DLCs for {item.Name}...");

        try
        {
            var info = await _depotInfo.GetAsync(item.AppId);
            if (info == null)
            {
                _toast?.Show("DLC Fetch Failed", "Could not fetch DLC data.", error: true);
                return;
            }

            var dlcMap = new Dictionary<long, (string? Name, long Size, bool HasDepot)>();

            foreach (var d in info.Depots.Where(x => x.DlcAppId != null))
            {
                long dlcId = d.DlcAppId!.Value;
                string? name = _appList?.GetName(dlcId) ?? _appInfo?.GetCached(dlcId)?.Name;
                dlcMap[dlcId] = (name, d.Size, d.Size > 0);
            }

            foreach (long dlcId in info.DlcIds)
            {
                if (!dlcMap.ContainsKey(dlcId))
                {
                    string? name = _appList?.GetName(dlcId) ?? _appInfo?.GetCached(dlcId)?.Name;
                    dlcMap[dlcId] = (name, 0, false);
                }
            }

            if (dlcMap.Count == 0)
            {
                _toast?.Show(item.Name, "No DLCs available for this game.");
                return;
            }

            var selectorItems = dlcMap.Select(kv =>
            {
                long dlcId = kv.Key;
                var (name, size, hasDepot) = kv.Value;
                string title = name ?? $"DLC {dlcId}";
                string meta = size > 0 ? $"Files: {FormatSize(size)}" : "License Only";
                bool isSoundtrack = DlcClassifier.IsSoundtrack(title);
                bool isBonus = DlcClassifier.IsBonus(title);

                return new DlcSelectorItemVm(dlcId, title, meta, isSoundtrack, hasDepot, isBonus);
            }).ToList();

            var vm = new DlcSelectorViewModel(selectorItems, item.Name, item.AppId);
            var win = new DlcSelectorWindow(vm)
            {
                Owner = Application.Current?.MainWindow
            };

            if (win.ShowDialog() == true)
            {
                var selected = vm.GetSelectedDlcs().Select(x => (x.DlcId, (string?)x.Name)).ToList();
                if (selected.Count > 0)
                {
                    item.CreateOrUpdateLua(selected);
                    UpdateCounts();
                    ApplyFilter();

                    _toast?.Show(item.Name, $"{selected.Count} DLC(s) applied!");
                    PromptRestartSteam();
                }
            }
        }
        catch (Exception ex)
        {
            _toast?.Show("Error", "Failed to open DLC selector: " + ex.Message, error: true);
        }
    }

    [RelayCommand]
    private void RemoveDlcs(SteamLibraryItemVm? item)
    {
        if (item == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to remove the injected DLCs for '{item.Name}'?\n\nThis will only delete the stplug-in Lua file. Your official Steam game files will remain completely untouched.",
            "Remove Injected DLCs",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            item.RemoveLua();
            UpdateCounts();
            ApplyFilter();

            _toast?.Show(item.Name, "Injected DLCs removed. Game is now Vanilla.");
            PromptRestartSteam();
        }
    }

    private void PromptRestartSteam()
    {
        var result = MessageBox.Show(
            "DLC configuration has been updated. Steam needs to be restarted for the changes to take effect.\n\nRestart Steam now?",
            "Restart Steam",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            RestartSteam();
        }
    }

    private void RestartSteam()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("steam"))
            {
                try { p.Kill(); } catch { }
            }

            Task.Delay(1500).ContinueWith(_ =>
            {
                string? steamPath = _steam?.EffectivePath;
                if (!string.IsNullOrEmpty(steamPath))
                {
                    string exe = Path.Combine(steamPath, "steam.exe");
                    if (File.Exists(exe)) Process.Start(exe);
                }
            });
        }
        catch { }
    }

    // --- Action Commands matching ManageViewModel ---

    [RelayCommand]
    private void ManageAchievements(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        OpenAchievements?.Invoke(item.AppId, item.Name);
    }

    [RelayCommand]
    private void EditLaunchOptions(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        OpenLaunchOptions?.Invoke(item.AppId, item.Name);
    }

    [RelayCommand]
    private void ManageMods(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        OpenModSettings?.Invoke(item);
    }

    [RelayCommand]
    private void MoveStorage(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        OpenStorageMover?.Invoke(item);
    }

    [RelayCommand]
    private void CleanLeftovers(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        OpenLeftoverCleaner?.Invoke(item);
    }

    [RelayCommand]
    private async Task RemoveDrm(SteamLibraryItemVm? item)
    {
        if (item == null || _steamless == null) return;

        var confirm = MessageBox.Show(
            "Steamless will unpack and remove SteamStub DRM from this game's executable.\n\nProceed?",
            "Remove DRM",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK) return;

        try
        {
            var prog = new Progress<double?>();
            var result = await _steamless.PatchGameAsync(item.AppId, prog);
            if (result.Failed)
            {
                _toast?.Show("Remove DRM", "Failed to unpack executable: " + result.Error, error: true);
            }
            else
            {
                _toast?.Show("Remove DRM", $"Successfully patched {result.Patched} executable(s)!");
            }
        }
        catch (Exception ex)
        {
            _toast?.Show("Remove DRM", "Error: " + ex.Message, error: true);
        }
    }

    [RelayCommand]
    private static void OpenStorePage(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        SteamService.OpenUrl($"steam://store/{item.AppId}");
    }

    [RelayCommand]
    private static void OpenInSteam(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        SteamService.OpenUrl($"steam://nav/games/details/{item.AppId}");
    }

    [RelayCommand]
    private static void RevealFolder(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        if (Directory.Exists(item.InstallDir))
        {
            SteamService.RevealInExplorer(item.InstallDir);
        }
    }

    [RelayCommand]
    private void CopyAppId(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        if (!SteamService.CopyToClipboard(item.AppId.ToString()))
        {
            _toast?.Show("Copy AppID", "Clipboard was busy, could not copy.", error: true);
        }
    }

    [RelayCommand]
    private void NavigateBuilds(SteamLibraryItemVm? item)
    {
        if (item == null) return;
        NavigateToBuilds?.Invoke(item.AppId);
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1024 * 1024 * 1024 => $"{(double)bytes / (1024 * 1024 * 1024):0.##} GB",
            >= 1024 * 1024 => $"{(double)bytes / (1024 * 1024):0.##} MB",
            >= 1024 => $"{(double)bytes / 1024:0.##} KB",
            _ => $"{bytes} B"
        };
    }
}
