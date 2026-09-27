import codecs

with open('src/LuaToolsGui/Views/ModFolderSelectorWindow.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''    public ModFolderSelectorWindow(string gameDomain, string modId, List<string> availablePaths)
    {
        InitializeComponent();
        
        SubtitleText.Text = $"Game: {gameDomain}  |  Mod ID: {modId}";
        _availablePaths = availablePaths ?? new List<string>();
        
        RefreshList();
    }'''

replacement = '''    public ModFolderSelectorWindow(string gameDomain, string modId, List<string> availablePaths, bool isSetupMode = false)
    {
        InitializeComponent();
        
        if (isSetupMode)
        {
            Title = "Pengaturan Mod Folder";
            SubtitleText.Text = $"Game: {gameDomain} (Manual Setup)";
            ExtractButton.Content = "Simpan Pengaturan";
            ExtractButton.Icon = new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Controls.SymbolRegular.Save24);
        }
        else
        {
            SubtitleText.Text = $"Game: {gameDomain}  |  Mod ID: {modId}";
        }
        
        _availablePaths = availablePaths ?? new List<string>();
        RefreshList();
    }'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/Views/ModFolderSelectorWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("ModFolderSelectorWindow patched")
