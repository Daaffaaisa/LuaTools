import codecs

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'r', encoding='utf-8') as f:
    cs = f.read()

# Fix SelectionChanged
target1 = '''    private void FoldersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateDefaultSetting();
        
        if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)
        {
            string selectedPath = selectedDir.Path;
        {
            ScanForUntrackedMods(selectedPath);
        }
    }'''

replacement1 = '''    private void FoldersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateDefaultSetting();
        
        if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)
        {
            ScanForUntrackedMods(selectedDir.Path);
        }
    }'''
cs = cs.replace(target1, replacement1)

# Fix UpdateDefaultSetting
target2 = '''    private void UpdateDefaultSetting()
    {
        if (FoldersCombo.SelectedItem is DirectoryColorInfo selectedDir)
        {
            string selectedPath = selectedDir.Path;
        {
            bool isDefault = DefaultCheckBox.IsChecked == true;
            _settings.SaveModDirectory(_gameDomain, selectedPath, isDefault);
            
            if (!isDefault && _settings.DefaultModDirectories.ContainsKey(_gameDomain))
            {
                _settings.DefaultModDirectories.Remove(_gameDomain);
                _settings.SaveModDirectory(_gameDomain, selectedPath, false); // force save
            }
        }
    }'''

replacement2 = '''    private void UpdateDefaultSetting()
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
    }'''
cs = cs.replace(target2, replacement2)

# Fix AddFolder_Click
target3 = '''            FoldersCombo.SelectedItem = newPath;'''
replacement3 = '''            FoldersCombo.SelectedItem = _dirColors.FirstOrDefault(d => d.Path == newPath);'''
cs = cs.replace(target3, replacement3)

# Fix DropZone_Drop
target4 = '''        if (FoldersCombo.SelectedItem is not string targetDir)'''
replacement4 = '''        if (FoldersCombo.SelectedItem is not DirectoryColorInfo selectedDirInfo)
        {
            _toast.Show("Error", "Silakan pilih atau tambah direktori target terlebih dahulu.", true);
            return;
        }
        string targetDir = selectedDirInfo.Path;
'''
cs = cs.replace(target4, replacement4)

# Remove duplicates of Error Toast if it generated a syntax issue
cs = cs.replace('''        {
            _toast.Show("Error", "Silakan pilih atau tambah direktori target terlebih dahulu.", true);
            return;
        }
        string targetDir = selectedDirInfo.Path;

        {
            _toast.Show("Error", "Silakan pilih atau tambah direktori target terlebih dahulu.", true);
            return;
        }''', '''        {
            _toast.Show("Error", "Silakan pilih atau tambah direktori target terlebih dahulu.", true);
            return;
        }
        string targetDir = selectedDirInfo.Path;''')


with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs)
