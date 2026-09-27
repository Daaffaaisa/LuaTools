import codecs

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target_combo = '''    private void FoldersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateDefaultSetting();
    }'''

replacement_combo = '''    private void FoldersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateDefaultSetting();
        
        if (FoldersCombo.SelectedItem is string selectedPath)
        {
            ScanForUntrackedMods(selectedPath);
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
    }'''

text = text.replace(target_combo, replacement_combo)


target_init_scan = '''        if (availablePaths.Count > 0)
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
        }
    }'''

replacement_init_scan = '''        if (availablePaths.Count > 0)
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
        }
    }'''

text = text.replace(target_init_scan, replacement_init_scan)

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Auto-Scan injected!")
