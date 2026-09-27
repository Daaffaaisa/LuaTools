using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuaToolsGui.ViewModels;

public partial class SuspendedProcessItem : ObservableObject
{
    public int ProcessId { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(ButtonIcon))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private bool _isSuspended;

    public string StatusColor => IsSuspended ? "#ef4444" : "#16a34a"; // Red if suspended, Green if running
    public string ButtonIcon => IsSuspended ? "Play24" : "Pause24";
    public string ButtonText => IsSuspended ? "RESUME" : "FREEZE";
}

public partial class GameSuspenderViewModel : ObservableObject
{
    // Sihir Windows OS tingkat rendah (NtDll)
    [DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtResumeProcess(IntPtr processHandle);

    [ObservableProperty] private ObservableCollection<SuspendedProcessItem> _processes = new();
    [ObservableProperty] private string _statusMessage = "";

    public GameSuspenderViewModel()
    {
        RefreshProcesses();
    }

    [RelayCommand]
    private void RefreshProcesses()
    {
        Processes.Clear();
        
        var allProcs = Process.GetProcesses()
            .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
            .OrderBy(p => p.ProcessName);

        int currentPid = Process.GetCurrentProcess().Id;

        // Daftar hitam aplikasi yang PASTI bukan game
        string[] blacklist = { "chrome", "msedge", "firefox", "discord", "spotify", "steam", "steamwebhelper", "epicgameslauncher", "devenv", "code", "explorer", "applicationframehost", "textinputhost", "cmd", "powershell", "taskmgr", "luatools", "cefclient", "obs32", "obs64" };

        foreach (var p in allProcs)
        {
            if (p.Id == currentPid) continue;
            
            string procName = p.ProcessName.ToLower();
            if (blacklist.Contains(procName)) continue;

            bool isGame = false;

            try
            {
                string path = p.MainModule?.FileName?.ToLower() ?? "";
                
                // Ciri 1: Berada di folder launcher game resmi
                if (path.Contains("steamapps") || 
                    path.Contains("epic games") || 
                    path.Contains("xboxgames") || 
                    path.Contains("ea games") ||
                    path.Contains("gog galaxy"))
                {
                    isGame = true;
                }
                // Ciri 2: Game bajakan / standalone biasanya ukurannya besar di RAM (di atas 300MB)
                // dan bukan merupakan aplikasi sistem Windows (C:\Windows)
                else if (!path.Contains("c:\\windows") && p.WorkingSet64 > 300_000_000)
                {
                    isGame = true;
                }
            }
            catch 
            { 
                // Jika Access Denied (biasanya aplikasi sistem Windows), skip saja.
                continue; 
            }

            if (isGame)
            {
                Processes.Add(new SuspendedProcessItem
                {
                    ProcessId = p.Id,
                    Name = p.ProcessName,
                    Title = p.MainWindowTitle,
                    IsSuspended = false 
                });
            }
        }
        StatusMessage = $"🔍 Menemukan {Processes.Count} game yang sedang aktif.";
    }

    [RelayCommand]
    private void ToggleSuspend(SuspendedProcessItem item)
    {
        if (item == null) return;

        try
        {
            var p = Process.GetProcessById(item.ProcessId);
            
            if (item.IsSuspended)
            {
                NtResumeProcess(p.Handle);
                item.IsSuspended = false;
                StatusMessage = $"▶️ [{item.Name}] berhasil dibangunkan! Game berjalan normal.";
            }
            else
            {
                NtSuspendProcess(p.Handle);
                item.IsSuspended = true;
                StatusMessage = $"⏸️ [{item.Name}] DIBEKUKAN! Pemakaian CPU langsung drop ke 0%.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠️ Gagal memanipulasi {item.Name}. Pastikan run as Admin jika game diproteksi. Error: {ex.Message}";
        }
    }
}
