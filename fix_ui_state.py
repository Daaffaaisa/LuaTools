import codecs

# 1. Update ModFolderSelectorWindow.xaml.cs
with open('src/LuaToolsGui/Views/ModFolderSelectorWindow.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target_window = '''    public ModFolderSelectorWindow(string gameDomain, string modId, List<string> availablePaths, bool isSetupMode = false)
    {
        InitializeComponent();
        
        if (isSetupMode)
        {'''

replacement_window = '''    private string? _initialDefaultPath;

    public ModFolderSelectorWindow(string gameDomain, string modId, List<string> availablePaths, bool isSetupMode = false, string? currentDefaultPath = null)
    {
        InitializeComponent();
        _initialDefaultPath = currentDefaultPath;
        
        if (isSetupMode)
        {'''

text = text.replace(target_window, replacement_window)

target_refresh = '''    private void RefreshList()
    {
        PathsListBox.ItemsSource = null;
        PathsListBox.ItemsSource = _availablePaths;
        
        if (_availablePaths.Count > 0)
        {
            PathsListBox.SelectedIndex = 0;
        }
    }'''

replacement_refresh = '''    private void RefreshList()
    {
        PathsListBox.ItemsSource = null;
        PathsListBox.ItemsSource = _availablePaths;
        
        if (_availablePaths.Count > 0)
        {
            if (!string.IsNullOrEmpty(_initialDefaultPath) && _availablePaths.Contains(_initialDefaultPath))
            {
                PathsListBox.SelectedItem = _initialDefaultPath;
                RememberCheckBox.IsChecked = true;
            }
            else
            {
                PathsListBox.SelectedIndex = 0;
            }
        }
    }'''

text = text.replace(target_refresh, replacement_refresh)

with open('src/LuaToolsGui/Views/ModFolderSelectorWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)


# 2. Update App.xaml.cs
with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text2 = f.read()

target_app = '''            var availablePaths = settings.ModDirectories[guessDomain];

            var dialog = new Views.ModFolderSelectorWindow(guessDomain, "", availablePaths, isSetupMode: true)
            {'''

replacement_app = '''            var availablePaths = settings.ModDirectories[guessDomain];
            settings.DefaultModDirectories.TryGetValue(guessDomain, out string? defaultPath);

            var dialog = new Views.ModFolderSelectorWindow(guessDomain, "", availablePaths, isSetupMode: true, currentDefaultPath: defaultPath)
            {'''

text2 = text2.replace(target_app, replacement_app)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text2)

print("UI state patched!")
