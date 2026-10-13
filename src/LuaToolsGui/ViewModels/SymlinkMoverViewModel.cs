using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace LuaToolsGui.ViewModels;

public partial class SymlinkMoverViewModel : ObservableObject
{
    private readonly LuaTileViewModel _game;
    
    public string GameName { get; }
    public string CurrentPath { get; }

    [ObservableProperty] private string _destinationPath = "";
    [ObservableProperty] private string _statusMessage = "Select a new disk location to move this game.";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotMoving))]
    private bool _isMoving = false;
    
    public bool IsNotMoving => !IsMoving;

    [ObservableProperty] private double _progressPercentage = 0;
    [ObservableProperty] private bool _canMove = false;
    [ObservableProperty] private string _totalSizeString = "";
    
    private long _totalSizeBytes = 0;

    public Action? CloseWindow { get; set; }

    public SymlinkMoverViewModel(LuaTileViewModel game, string installDir)
    {
        _game = game;
        GameName = game.Name ?? "Unknown Game";
        CurrentPath = installDir;
        
        CalculateSizeAsync();
    }

    private async void CalculateSizeAsync()
    {
        StatusMessage = "Calculating game size...";
        await Task.Run(() =>
        {
            _totalSizeBytes = GetDirectorySize(new DirectoryInfo(CurrentPath));
        });
        TotalSizeString = FormatSize(_totalSizeBytes);
        StatusMessage = $"Game size: {TotalSizeString}. Select new disk location.";
    }

    [RelayCommand]
    private void BrowseDestination()
    {
        // Using System.Windows.Forms.FolderBrowserDialog natively via WPF is tricky without referencing WinForms,
        // so we use a simple fallback or the native OpenFolderDialog if available (WPF .NET 8 doesn't have it natively unless using Ookii or WPFFolderBrowser)
        // Let's use OpenFolderDialog which was added in .NET 8 / Win10! Wait, OpenFolderDialog is .NET 8 WPF!
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select New Destination Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            DestinationPath = dialog.FolderName;
            
            // Check drive space
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(DestinationPath)!);
                if (drive.AvailableFreeSpace < _totalSizeBytes)
                {
                    StatusMessage = $"Not enough space on {drive.Name}. Free: {FormatSize(drive.AvailableFreeSpace)}";
                    CanMove = false;
                }
                else if (DestinationPath.StartsWith(CurrentPath, StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "New location cannot be inside the current folder!";
                    CanMove = false;
                }
                else if (CurrentPath.StartsWith(DestinationPath, StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "Current location cannot be inside the new folder!";
                    CanMove = false;
                }
                else
                {
                    StatusMessage = $"Sufficient disk space ({FormatSize(drive.AvailableFreeSpace)}). Ready to move!";
                    CanMove = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Invalid folder.";
                CanMove = false;
            }
        }
    }

    [RelayCommand]
    private async Task StartMoveAsync()
    {
        if (!CanMove || string.IsNullOrWhiteSpace(DestinationPath)) return;

        IsMoving = true;
        CanMove = false;

        string targetDir = Path.Combine(DestinationPath, Path.GetFileName(CurrentPath));
        
        await Task.Run(() =>
        {
            try
            {
                // 1. Pindahkan folder menggunakan metode rekursif dengan progress
                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Moving files...");
                MoveDirectoryWithProgress(CurrentPath, targetDir);

                // 2. Hapus direktori lama sepenuhnya (karena Directory.Move gagal beda volume)
                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Cleaning up old directory...");
                if (Directory.Exists(CurrentPath))
                {
                    Directory.Delete(CurrentPath, true);
                }

                // 3. Buat Symlink (Junction)
                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Creating Symlink (Junction)...");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c mklink /J \"{CurrentPath}\" \"{targetDir}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Application.Current.Dispatcher.Invoke(() => StatusMessage = "Failed to create Symlink! But files were moved successfully.");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Done! Game successfully moved and Symlink created.");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => StatusMessage = $"Failed: {ex.Message}");
            }
        });

        // Tunggu bentar lalu tutup
        if (StatusMessage.StartsWith("Done!"))
        {
            await Task.Delay(2000);
            CloseWindow?.Invoke();
        }
        
        IsMoving = false;
    }

    private void MoveDirectoryWithProgress(string source, string target)
    {
        Directory.CreateDirectory(target);
        string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        
        long copied = 0;
        
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, target));
        }
        
        foreach (string file in files)
        {
            string dest = file.Replace(source, target);
            FileInfo fi = new FileInfo(file);
            long fileLen = fi.Length;
            
            // Move file
            File.Move(file, dest, overwrite: true);
            
            copied += fileLen;
            if (_totalSizeBytes > 0)
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    ProgressPercentage = ((double)copied / _totalSizeBytes) * 100;
                    StatusMessage = $"Moving: {FormatSize(copied)} / {TotalSizeString}";
                });
            }
        }
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
}
