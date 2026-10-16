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
    public string Name { get; }
    public string InstallDir { get; }
    public string StPlugInDir { get; }

    public string LuaPath => Path.Combine(StPlugInDir, $"{AppId}.lua");

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
        Name = name;
        InstallDir = installDir;
        StPlugInDir = stPlugInDir;
        SizeOnDisk = sizeOnDisk;
        IsInstalled = isInstalled;
        
        RefreshStatus();
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
