import codecs

# 1. Update App.xaml.cs to register ModExtractionService
with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    app_text = f.read()

reg_target = '''                services.AddSingleton<SettingsService>();
                services.AddSingleton<NexusModsService>();
                services.AddSingleton<CacheService>();'''

reg_replacement = '''                services.AddSingleton<SettingsService>();
                services.AddSingleton<NexusModsService>();
                services.AddSingleton<Services.ModExtractionService>();
                services.AddSingleton<CacheService>();'''

app_text = app_text.replace(reg_target, reg_replacement)

# 2. Update HandleProtocolUrl to call ModExtractionService
logic_target = '''                    // Actually download the file
                    string? savedPath = await nexusService.DownloadModFileAsync(cdnLink);
                    
                    if (savedPath != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show(
                                $\"? DOWNLOAD SELESAI! ?\\n\\n\" +
                                $\"File Mod telah berhasil disedot dari Nexus Mods!\\n\\n\" +
                                $\"Tersimpan di:\\n{savedPath}\\n\\n\" +
                                $\"Fase 3 Sukses! Selanjutnya kita tinggal build Smart Extractor ke dalam folder game.\",
                                \"LuaTools Mod Manager (Phase 3)\", 
                                System.Windows.MessageBoxButton.OK, 
                                System.Windows.MessageBoxImage.Information);
                        });
                    }'''

logic_replacement = '''                    // Actually download the file
                    string? savedPath = await nexusService.DownloadModFileAsync(cdnLink);
                    
                    if (savedPath != null)
                    {
                        toastService.Show(\"Mengekstrak Mod...\", $\"Sedang memasang mod ke folder game...\");
                        
                        var extractor = _host.Services.GetRequiredService<Services.ModExtractionService>();
                        var (success, errorMsg, extractPath) = await extractor.ExtractModAsync(savedPath, nxm.Game);
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (success)
                            {
                                System.Windows.MessageBox.Show(
                                    $\"?? MOD BERHASIL DIPASANG! ??\\n\\n\" +
                                    $\"File Mod telah di-download dan sukses diekstrak.\\n\\n\" +
                                    $\"Lokasi Mod:\\n{extractPath}\\n\\n\" +
                                    $\"Fase 4 (Smart Extractor) Selesai! Kamu sudah bisa memainkan gamenya dengan mod ini!\",
                                    \"LuaTools Mod Manager\", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Information);
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    $\"? GAGAL EKSTRAK MOD ?\\n\\n\" +
                                    $\"Mod berhasil didownload, tapi gagal dipasang.\\n\\n\" +
                                    $\"Alasan: {errorMsg}\\n\\n\" +
                                    $\"File zip mentahnya masih aman di:\\n{savedPath}\",
                                    \"LuaTools Mod Manager\", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Error);
                            }
                        });
                    }'''

app_text = app_text.replace(logic_target, logic_replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(app_text)

print("Extractor patch applied")
