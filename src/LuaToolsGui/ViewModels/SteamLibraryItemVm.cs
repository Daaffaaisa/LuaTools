using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private bool _isDlcInjected;

    public bool IsVanilla => !IsDlcInjected;

    public string StatusBadge => IsDlcInjected ? "DLCs Injected" : "Vanilla";
    public string StatusBadgeBackground => IsDlcInjected ? "#8b5cf6" : "#3b82f6"; // Purple if DLCs injected, Blue if Vanilla

    [ObservableProperty] private ImageSource? _cover;
    [ObservableProperty] private string? _coverPath;
    [ObservableProperty] private bool _isLoadingCover;
    [ObservableProperty] private string _formattedSize = "";
    public long SizeOnDisk { get; init; }

    public SteamLibraryItemVm(long appId, string name, string installDir, string stPlugInDir, long sizeOnDisk = 0)
    {
        AppId = appId;
        Name = name;
        InstallDir = installDir;
        StPlugInDir = stPlugInDir;
        SizeOnDisk = sizeOnDisk;
        
        RefreshStatus();
    }

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
