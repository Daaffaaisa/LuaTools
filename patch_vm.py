import codecs

with open('src/LuaToolsGui/ViewModels/ManageViewModel.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''    /// <summary>Action to open the Storage Mover window for a specific game.</summary>
    public Action<LuaTileViewModel>? OpenStorageMover { get; set; }'''

replacement = '''    /// <summary>Action to open the Storage Mover window for a specific game.</summary>
    public Action<LuaTileViewModel>? OpenStorageMover { get; set; }
    
    /// <summary>Action to open the Mod Folder Settings window for a specific game.</summary>
    public Action<LuaTileViewModel>? OpenModSettings { get; set; }'''

text = text.replace(target, replacement)

cmd_target = '''    [RelayCommand]
    private void MoveStorage(LuaTileViewModel tile)
    {
        if (tile != null)
            OpenStorageMover?.Invoke(tile);
    }'''

cmd_replacement = '''    [RelayCommand]
    private void MoveStorage(LuaTileViewModel tile)
    {
        if (tile != null)
            OpenStorageMover?.Invoke(tile);
    }
    
    [RelayCommand]
    private void ManageMods(LuaTileViewModel tile)
    {
        if (tile != null)
            OpenModSettings?.Invoke(tile);
    }'''

text = text.replace(cmd_target, cmd_replacement)

with open('src/LuaToolsGui/ViewModels/ManageViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("ManageViewModel patched")
