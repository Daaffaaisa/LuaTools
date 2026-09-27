import codecs

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'r', encoding='utf-8') as f:
    cs = f.read()

# Add Models
target_ns = '''namespace LuaToolsGui.Views;'''
replacement_ns = '''namespace LuaToolsGui.Views;

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
'''
cs = cs.replace(target_ns, replacement_ns)

# Update ObservableCollection type
cs = cs.replace('private readonly ObservableCollection<InstalledMod> _mods;', 'private readonly ObservableCollection<ModDisplayItem> _mods;')
cs = cs.replace('_mods = new ObservableCollection<InstalledMod>();', '_mods = new ObservableCollection<ModDisplayItem>();')

# Add _dirColors field
cs = cs.replace('private bool _isInitializing = true;', 'private bool _isInitializing = true;\n    private readonly List<DirectoryColorInfo> _dirColors = new();')

# Update LoadPaths
target_loadpaths = '''        var availablePaths = _settings.ModDirectories[_gameDomain];
        FoldersCombo.ItemsSource = null;
        FoldersCombo.ItemsSource = availablePaths;
        
        _settings.DefaultModDirectories.TryGetValue(_gameDomain, out string? defaultPath);
        
        if (availablePaths.Count > 0)
        {
            if (!string.IsNullOrEmpty(defaultPath) && availablePaths.Contains(defaultPath))
            {
                FoldersCombo.SelectedItem = defaultPath;
                DefaultCheckBox.IsChecked = true;
            }
            else
            {
                FoldersCombo.SelectedIndex = 0;
            }
            
            if (FoldersCombo.SelectedItem is string selectedPath)
            {
                ScanForUntrackedMods(selectedPath);
            }
        }'''

replacement_loadpaths = '''        var availablePaths = _settings.ModDirectories[_gameDomain];
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
        }'''
cs = cs.replace(target_loadpaths, replacement_loadpaths)

# Update LoadMods
target_loadmods = '''        _mods.Clear();
        var mods = _registry.GetModsForGame(_gameDomain).OrderByDescending(m => m.InstalledAt).ToList();
        foreach (var mod in mods)
        {
            _mods.Add(mod);
        }'''
replacement_loadmods = '''        _mods.Clear();
        var mods = _registry.GetModsForGame(_gameDomain).OrderByDescending(m => m.InstalledAt).ToList();
        foreach (var mod in mods)
        {
            string color = "#888888";
            string firstFile = mod.InstalledFiles.FirstOrDefault() ?? "";
            var match = _dirColors.FirstOrDefault(d => firstFile.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));
            if (match != null) color = match.ColorHex;
            
            _mods.Add(new ModDisplayItem { Mod = mod, ColorHex = color });
        }'''
cs = cs.replace(target_loadmods, replacement_loadmods)

# Update combos logic
cs = cs.replace('if (FoldersCombo.SelectedItem is string selectedPath)', 'if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)\n        {\n            string selectedPath = selectedDir.Path;')

# Fix the curly brace closure for the replacement above
# Wait, I should manually patch UpdateDefaultSetting, FoldersCombo_SelectionChanged and AddFolder_Click to use DirectoryColorInfo safely.
# Let's do it precisely:

target_updatedefault = '''    private void UpdateDefaultSetting()
    {
        if (FoldersCombo.SelectedItem is string selectedDir)
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
    }'''
# The previous string replacement messed up if (FoldersCombo.SelectedItem is string selectedPath) so let's rewrite the functions entirely.

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs)
