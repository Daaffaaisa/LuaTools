import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''            var availablePaths = settings.ModDirectories[guessDomain];
            settings.DefaultModDirectories.TryGetValue(guessDomain, out string? defaultPath);

            var dialog = new Views.ModFolderSelectorWindow(guessDomain, "", availablePaths, isSetupMode: true, currentDefaultPath: defaultPath)
            {
                Owner = window
            };

            if (dialog.ShowDialog() == true)
            {
                settings.SaveModDirectory(guessDomain, dialog.SelectedPath, dialog.RememberChoice);
                
                // Jika user batal centang "Selalu gunakan", kita hapus dari DefaultModDirectories
                if (!dialog.RememberChoice && settings.DefaultModDirectories.ContainsKey(guessDomain))
                {
                    settings.DefaultModDirectories.Remove(guessDomain);
                    // Force save
                    settings.SaveModDirectory(guessDomain, dialog.SelectedPath, false);
                }
                
                toastService.Show("Setup Disimpan", $"Pengaturan folder mod untuk {tile.Name} berhasil disimpan.");
            }'''

replacement = '''            var registry = _host.Services.GetRequiredService<Services.ModRegistryService>();
            var extractor = _host.Services.GetRequiredService<Services.ModExtractionService>();
            
            var dialog = new Views.ModDashboardWindow(guessDomain, settings, registry, extractor, toastService)
            {
                Owner = window
            };
            
            dialog.ShowDialog();'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs patched to launch ModDashboardWindow")
