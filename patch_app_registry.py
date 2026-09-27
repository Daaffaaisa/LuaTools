import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Add DI Registration
target_di = '''        services.AddSingleton<Services.SteamLibraryService>();
        services.AddSingleton<Services.NexusModsService>();
        services.AddSingleton<Services.ModExtractionService>();'''

replacement_di = '''        services.AddSingleton<Services.SteamLibraryService>();
        services.AddSingleton<Services.NexusModsService>();
        services.AddSingleton<Services.ModExtractionService>();
        services.AddSingleton<Services.ModRegistryService>();'''
text = text.replace(target_di, replacement_di)

# Update Extractor Call
target_call = '''                        toastService.Show("Mengekstrak Mod...", $"Sedang memasang mod...");
                        var (success, errorMsg, extractPath) = await extractor.ExtractModAsync(savedPath, nxm.Game, targetDirOverride);
                        
                        Dispatcher.Invoke(() =>
                        {'''

replacement_call = '''                        toastService.Show("Mengekstrak Mod...", $"Sedang memasang mod...");
                        var (success, errorMsg, extractPath, extractedFiles) = await extractor.ExtractModAsync(savedPath, nxm.Game, targetDirOverride);
                        
                        if (success && extractedFiles != null)
                        {
                            var registry = _host.Services.GetRequiredService<Services.ModRegistryService>();
                            registry.RegisterMod(new Models.InstalledMod
                            {
                                Name = System.IO.Path.GetFileNameWithoutExtension(savedPath),
                                GameDomain = nxm.Game,
                                Source = "Nexus",
                                NexusModId = nxm.ModId,
                                InstalledFiles = extractedFiles
                            });
                        }
                        
                        Dispatcher.Invoke(() =>
                        {'''
text = text.replace(target_call, replacement_call)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs DI and Extraction updated!")
