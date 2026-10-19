using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuaToolsGui.Services;

namespace LuaToolsGui.ViewModels;

public partial class SteamLibraryItemVm : ObservableObject
{
    public long AppId { get; }
    [ObservableProperty] private string _name = "";
    public string InstallDir { get; }
    public string StPlugInDir { get; }

    public string LuaPath => Path.Combine(StPlugInDir, $"{AppId}.lua");

    private int _resolving;
    private bool _nameIsPlaceholder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVanilla))]
    [NotifyPropertyChangedFor(nameof(StatusBadge))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeBackground))]
    private bool _isInstalled = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVanilla))]
    [NotifyPropertyChangedFor(nameof(StatusBadge))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeBackground))]
    private bool _isDlcInjected;

    public bool IsVanilla => IsInstalled && !IsDlcInjected;

    public string StatusBadge => !IsInstalled ? "Not Installed" : IsDlcInjected ? "DLCs Injected" : "Original";
    public string StatusBadgeBackground => !IsInstalled ? "#475569" : IsDlcInjected ? "#8b5cf6" : "#10b981"; // Slate if Not Installed, Purple if DLCs injected, Emerald if Original

    [ObservableProperty] private ImageSource? _cover;
    [ObservableProperty] private string? _coverPath;
    [ObservableProperty] private bool _isLoadingCover;
    [ObservableProperty] private string _formattedSize = "";
    public long SizeOnDisk { get; init; }

    public SteamLibraryItemVm(long appId, string name, string installDir, string stPlugInDir, long sizeOnDisk = 0, bool isInstalled = true)
    {
        AppId = appId;
        _name = name;
        _nameIsPlaceholder = string.IsNullOrWhiteSpace(name) || name == appId.ToString();
        InstallDir = installDir;
        StPlugInDir = stPlugInDir;
        SizeOnDisk = sizeOnDisk;
        IsInstalled = isInstalled;
        
        RefreshStatus();
    }

    public void SetName(string newName)
    {
        _nameIsPlaceholder = false;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) Name = newName;
        else dispatcher.Invoke(() => Name = newName);
    }

    public async Task EnsureResolvedAsync(SteamAppInfoCache? appInfo, CoverCache? covers, SteamAppListCache? appList)
    {
        if (Cover is not null && !_nameIsPlaceholder) return;
        if (System.Threading.Interlocked.Exchange(ref _resolving, 1) == 1) return;
        try
        {
            // 1. If name is placeholder, try fast lookup from appList or appInfo
            if (_nameIsPlaceholder)
            {
                string? fastName = appList?.GetName(AppId) ?? appInfo?.GetCached(AppId)?.Name;
                if (!string.IsNullOrWhiteSpace(fastName))
                {
                    SetName(fastName);
                }
            }

            if (appInfo == null || covers == null) return;

            // 2. Resolve cover file (and backfill name if still placeholder)
            string? local = await LuaTileViewModel.ResolveCoverFileAsync(AppId, appInfo, covers,
                name => { if (_nameIsPlaceholder) SetName(name); });

            if (_nameIsPlaceholder)
            {
                var info = appInfo.GetCached(AppId) ?? await appInfo.ResolveAsync(AppId);
                if (!string.IsNullOrWhiteSpace(info?.Name)) SetName(info!.Name);
            }

            if (local is null) return;
            var image = await System.Threading.Tasks.Task.Run(() => LuaTileViewModel.LoadFrozen(local));
            if (image is not null)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher is null || dispatcher.CheckAccess()) Cover = image;
                else dispatcher.Invoke(() => Cover = image);
            }
        }
        catch { }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _resolving, 0);
        }
    }

    [RelayCommand]
    private void InstallGame() => SteamService.OpenUrl($"steam://install/{AppId}");

    [RelayCommand]
    private void PlayGame() => SteamService.OpenUrl($"steam://run/{AppId}");

    public void RefreshStatus()
    {
        IsDlcInjected = !string.IsNullOrEmpty(StPlugInDir) && File.Exists(LuaPath);
    }

    public void CreateOrUpdateLua(IEnumerable<(long DlcId, string? Name)> dlcs)
    {
        if (string.IsNullOrEmpty(StPlugInDir)) return;

        Directory.CreateDirectory(StPlugInDir);

        string existingText = File.Exists(LuaPath) ? File.ReadAllText(LuaPath) : $"addappid({AppId}) -- {Name} (Official Base Game)\n";
        string updated = LuaEditor.AppendDlcEntries(existingText, dlcs);

        File.WriteAllText(LuaPath, updated);
        RefreshStatus();
    }

    public void RemoveLua()
    {
        if (File.Exists(LuaPath))
        {
            File.Delete(LuaPath);
        }
        RefreshStatus();
    }
}
