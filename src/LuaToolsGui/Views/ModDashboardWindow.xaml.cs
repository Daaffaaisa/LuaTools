using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using LuaToolsGui.Models;
using LuaToolsGui.Services;
using Wpf.Ui.Controls;

namespace LuaToolsGui.Views;

public class DirectoryColorInfo
{
    public string Path { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
}

public class ModDisplayItem : System.ComponentModel.INotifyPropertyChanged
{
    public InstalledMod Mod { get; set; } = null!;
    public string ColorHex { get; set; } = string.Empty;
    
    public string Id => Mod.Id;
    public string Name => Mod.Name;
    public string Source => Mod.Source;
    public DateTime InstalledAt => Mod.InstalledAt;
    public bool IsEnabled 
    { 
        get => Mod.IsEnabled; 
        set 
        { 
            if (Mod.IsEnabled != value) 
            { 
                Mod.IsEnabled = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
            } 
        } 
    }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}


public partial class ModDashboardWindow : FluentWindow
{
    private readonly string _gameDomain;
    private readonly SettingsService _settings;
    private readonly ModRegistryService _registry;
    private readonly ModExtractionService _extractor;
    private readonly ToastService _toast;
    
    private readonly ObservableCollection<ModDisplayItem> _mods;
    private bool _isInitializing = true;
    private readonly List<DirectoryColorInfo> _dirColors = new();

    public ModDashboardWindow(
        string gameDomain, 
        SettingsService settings, 
        ModRegistryService registry, 
        ModExtractionService extractor,
        ToastService toast)
    {
        InitializeComponent();
        
        _gameDomain = gameDomain;
        _settings = settings;
        _registry = registry;
        _extractor = extractor;
        _toast = toast;
        
        GameTitleText.Text = $"Mod Dashboard: {_gameDomain}";
        
        _mods = new ObservableCollection<ModDisplayItem>();
        ModsList.ItemsSource = _mods;
        
        LoadPaths();
        LoadMods();
        
        _isInitializing = false;
    }

    private void LoadPaths()
    {
        if (!_settings.ModDirectories.ContainsKey(_gameDomain))
        {
            _settings.ModDirectories[_gameDomain] = new List<string>();
        }
        
        var availablePaths = _settings.ModDirectories[_gameDomain];
        _dirColors.Clear();
        string[] palette = { "#60a5fa", "#4ade80", "#fb923c", "#c084fc", "#f472b6", "#facc15", "#38bdf8", "#f87171" };
        for (int i = 0; i < availablePaths.Count; i++)
        {
            _dirColors.Add(new DirectoryColorInfo { Path = availablePaths[i], ColorHex = palette[i % palette.Length] });
        }
        
        FoldersCombo.ItemsSource = null;
        FoldersCombo.ItemsSource = _dirColors;
        
        _settings.DefaultModDirectories.TryGetValue(_gameDomain, out string? defaultPath);
        
        if (_dirColors.Count > 0)
        {
            var match = _dirColors.FirstOrDefault(d => d.Path.Equals(defaultPath, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                FoldersCombo.SelectedItem = match;
                DefaultCheckBox.IsChecked = true;
            }
            else
            {
                FoldersCombo.SelectedIndex = 0;
            }
            
            if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)
            {
                ScanForUntrackedMods(selectedDir.Path);
            }
        }
    }

    private void LoadMods()
    {
        _mods.Clear();
        var mods = _registry.GetModsForGame(_gameDomain).OrderByDescending(m => m.InstalledAt).ToList();
        foreach (var mod in mods)
        {
            string color = "#888888";
            string firstFile = mod.InstalledFiles.FirstOrDefault() ?? "";
            var match = _dirColors.FirstOrDefault(d => firstFile.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));
            if (match != null) color = match.ColorHex;
            
            _mods.Add(new ModDisplayItem { Mod = mod, ColorHex = color });
        }
        
        EmptyModsText.Visibility = _mods.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void FoldersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateDefaultSetting();
        
        if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)
        {
            ScanForUntrackedMods(selectedDir.Path);
        }
    }

    private void ScanForUntrackedMods(string targetDir)
    {
        if (!System.IO.Directory.Exists(targetDir)) return;
        
        var existingMods = _registry.GetModsForGame(_gameDomain);
        var trackedFiles = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var mod in existingMods)
        {
            foreach (var f in mod.InstalledFiles)
            {
                trackedFiles.Add(f);
                trackedFiles.Add(f + ".disabled");
            }
        }
        
        bool foundNew = false;
        
        // Scan loose files
        try
        {
            foreach (var file in System.IO.Directory.GetFiles(targetDir))
            {
                if (trackedFiles.Contains(file)) continue;
                
                bool isDisabled = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string basePath = isDisabled ? file.Substring(0, file.Length - 9) : file;
                string name = System.IO.Path.GetFileNameWithoutExtension(basePath);
                
                // Ignore common junk files
                if (name.ToLower() == "desktop" || file.EndsWith(".ini")) continue;
                
                var newMod = new InstalledMod
                {
                    Name = name + " (Legacy)",
                    GameDomain = _gameDomain,
                    Source = "Auto-Imported",
                    IsEnabled = !isDisabled,
                    InstalledFiles = new System.Collections.Generic.List<string> { basePath }
                };
                _registry.RegisterMod(newMod);
                foundNew = true;
            }
            
            // Scan directories (for mod structures like Stardew Valley)
            foreach (var dir in System.IO.Directory.GetDirectories(targetDir))
            {
                if (trackedFiles.Contains(dir)) continue;
                
                bool isDisabled = dir.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string basePath = isDisabled ? dir.Substring(0, dir.Length - 9) : dir;
                string name = System.IO.Path.GetFileName(basePath);
                
                if (name.StartsWith(".luatools")) continue;
                
                var newMod = new InstalledMod
                {
                    Name = name + " (Legacy)",
                    GameDomain = _gameDomain,
                    Source = "Auto-Imported",
                    IsEnabled = !isDisabled,
                    InstalledFiles = new System.Collections.Generic.List<string> { basePath }
                };
                _registry.RegisterMod(newMod);
                foundNew = true;
            }
        }
        catch { /* ignore permission issues */ }
        
        if (foundNew)
        {
            LoadMods();
            _toast.Show("Mod Lama Terdeteksi", "Beberapa mod lama berhasil di-import otomatis.");
        }
    }

    private void DefaultCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateDefaultSetting();
    }
    
    private void UpdateDefaultSetting()
    {
        if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)
        {
            string selectedPath = selectedDir.Path;
            bool isDefault = DefaultCheckBox.IsChecked == true;
            _settings.SaveModDirectory(_gameDomain, selectedPath, isDefault);
            
            if (!isDefault && _settings.DefaultModDirectories.ContainsKey(_gameDomain))
            {
                _settings.DefaultModDirectories.Remove(_gameDomain);
                _settings.SaveModDirectory(_gameDomain, selectedPath, false); // force save
            }
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        using var fbd = new FolderBrowserDialog { Description = "Pilih folder target instalasi mod" };
        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string newPath = fbd.SelectedPath;
            var paths = _settings.ModDirectories[_gameDomain];
            
            if (!paths.Contains(newPath))
            {
                paths.Add(newPath);
                // force refresh
                _isInitializing = true;
                LoadPaths();
                _isInitializing = false;
            }
            FoldersCombo.SelectedItem = _dirColors.FirstOrDefault(d => d.Path == newPath);
        }
    }

    private void Mod_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        
        if (sender is ToggleSwitch toggle && toggle.Tag is string modId)
        {
            bool enable = toggle.IsChecked == true;
            bool success = _registry.ToggleMod(_gameDomain, modId, enable);
            
            if (!success)
            {
                _toast.Show("Gagal Toggle Mod", "Sebagian file gagal diganti namanya. Pastikan game ditutup.", true);
                // Revert toggle visually without triggering event
                _isInitializing = true;
                toggle.IsChecked = !enable;
                _isInitializing = false;
            }
        }
    }

    private void DeleteMod_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button btn && btn.Tag is string modId)
        {
            var result = System.Windows.MessageBox.Show(
                "Yakin ingin menghapus mod ini beserta semua file-nya?", 
                "Hapus Mod", 
                System.Windows.MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
                
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _registry.RemoveMod(_gameDomain, modId);
                LoadMods();
                _toast.Show("Mod Dihapus", "File mod berhasil dihapus secara permanen.");
            }
        }
    }

    private void DropZone_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            DropZone.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255));
    }

    private void DropZone_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        DropZone.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(17, 255, 255, 255));
    }

    private async void DropZone_Drop(object sender, System.Windows.DragEventArgs e)
    {
        DropZone.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(17, 255, 255, 255));
        
        if (FoldersCombo.SelectedItem is not DirectoryColorInfo selectedDirInfo)
        {
            _toast.Show("Error", "Silakan pilih atau tambah direktori target terlebih dahulu.", true);
            return;
        }
        string targetDir = selectedDirInfo.Path;

        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            string zipFile = files.FirstOrDefault(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".rar", StringComparison.OrdinalIgnoreCase));
            
            if (string.IsNullOrEmpty(zipFile))
            {
                _toast.Show("Error", "Hanya file .zip atau .rar yang didukung saat ini.", true);
                return;
            }

            LoadingText.Text = "Mengekstrak Mod Manual...";
            LoadingOverlay.Visibility = Visibility.Visible;

            var (success, errorMsg, extractPath, extractedFiles) = await _extractor.ExtractModAsync(zipFile, _gameDomain, targetDir);

            if (success && extractedFiles != null && extractedFiles.Count > 0)
            {
                _registry.RegisterMod(new InstalledMod
                {
                    Name = Path.GetFileNameWithoutExtension(zipFile),
                    GameDomain = _gameDomain,
                    Source = "Manual Install",
                    InstalledFiles = extractedFiles
                });
                
                LoadMods();
                _toast.Show("Mod Dipasang!", $"Mod manual berhasil diekstrak.");
            }
            else
            {
                System.Windows.MessageBox.Show(
                    $"? GAGAL EKSTRAK MOD ?\n\nAlasan: {errorMsg}", 
                    "Error Instalasi", 
                    System.Windows.MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
