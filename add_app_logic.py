import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''        manage.OpenStorageMover = (tile) => Dispatcher.Invoke(() =>
        {'''

replacement = '''        manage.OpenModSettings = (tile) => Dispatcher.Invoke(() =>
        {
            var settings = _host.Services.GetRequiredService<Services.SettingsService>();
            var toastService = _host.Services.GetRequiredService<Services.ToastService>();
            
            // Coba tebak nama domain game dari nama game di Steam (karena SteamApp nggak nyimpen Nexus ID)
            // Misal "Stardew Valley" -> "stardewvalley"
            string guessDomain = tile.Name.Replace(" ", "").Replace(":", "").Replace("-", "").Replace("'", "").ToLower();
            
            var availablePaths = settings.ModDirectories.ContainsKey(guessDomain) 
                ? settings.ModDirectories[guessDomain] 
                : new System.Collections.Generic.List<string>();

            var dialog = new Views.ModFolderSelectorWindow(guessDomain, "", availablePaths, isSetupMode: true)
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
            }
        });

        manage.OpenStorageMover = (tile) => Dispatcher.Invoke(() =>
        {'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs logic added")
