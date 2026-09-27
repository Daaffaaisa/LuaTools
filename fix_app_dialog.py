import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''                        var extractor = _host.Services.GetRequiredService<Services.ModExtractionService>();
                        var (success, errorMsg, extractPath) = await extractor.ExtractModAsync(savedPath, nxm.Game);
                        
                        if (!success && errorMsg == "NEEDS_SETUP")
                        {
                            Dispatcher.Invoke(async () =>
                            {
                                System.Windows.MessageBox.Show(
                                    $"Game ini belum disetel!\\n\\n" +
                                    $"Karena ini pertama kalinya kamu install mod untuk game '{nxm.Game}', tolong pilih di mana letak folder 'Mods' atau 'Data' untuk game tersebut.\\n\\n" +
                                    $"Setelah dipilih, LuaTools akan mengingatnya selamanya!",
                                    "Setup Folder Mod Baru", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Information);

                                var dialog = new Microsoft.Win32.OpenFolderDialog
                                {
                                    Title = $"Pilih folder target Mod untuk game {nxm.Game}"
                                };

                                if (dialog.ShowDialog() == true)
                                {
                                    var settings = _host.Services.GetRequiredService<Services.SettingsService>();
                                    settings.SaveModDirectory(nxm.Game, dialog.FolderName);
                                    
                                    toastService.Show("Setup Selesai", "Folder disimpan! Mengekstrak mod sekarang...");
                                    
                                    // Ulangi ekstrak
                                    var (retrySuccess, retryError, retryPath) = await extractor.ExtractModAsync(savedPath, nxm.Game);
                                    
                                    if (retrySuccess)
                                    {
                                        System.Windows.MessageBox.Show(
                                            $"?? MOD BERHASIL DIPASANG! ??\\n\\n" +
                                            $"Lokasi Mod:\\n{retryPath}",
                                            "LuaTools Mod Manager", 
                                            System.Windows.MessageBoxButton.OK, 
                                            System.Windows.MessageBoxImage.Information);
                                    }
                                }
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (success)
                                {
                                    System.Windows.MessageBox.Show(
                                        $"?? MOD BERHASIL DIPASANG! ??\\n\\n" +
                                        $"File Mod telah di-download dan sukses diekstrak.\\n\\n" +
                                        $"Lokasi Mod:\\n{extractPath}\\n\\n" +
                                        $"Fase 4 (Smart Extractor) Selesai! Kamu sudah bisa memainkan gamenya dengan mod ini!",
                                        "LuaTools Mod Manager", 
                                        System.Windows.MessageBoxButton.OK, 
                                        System.Windows.MessageBoxImage.Information);
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show(
                                        $"? GAGAL EKSTRAK MOD ?\\n\\n" +
                                        $"Mod berhasil didownload, tapi gagal dipasang.\\n\\n" +
                                        $"Alasan: {errorMsg}\\n\\n" +
                                        $"File zip mentahnya masih aman di:\\n{savedPath}",
                                        "LuaTools Mod Manager", 
                                        System.Windows.MessageBoxButton.OK, 
                                        System.Windows.MessageBoxImage.Error);
                                }
                            });
                        }'''

replacement = '''                        var extractor = _host.Services.GetRequiredService<Services.ModExtractionService>();
                        var settings = _host.Services.GetRequiredService<Services.SettingsService>();
                        
                        string? targetDirOverride = null;
                        bool needsPrompt = true;

                        if (settings.DefaultModDirectories.TryGetValue(nxm.Game, out string? defaultPath))
                        {
                            targetDirOverride = defaultPath;
                            needsPrompt = false;
                        }

                        if (needsPrompt)
                        {
                            bool userCanceled = false;
                            Dispatcher.Invoke(() =>
                            {
                                var availablePaths = settings.ModDirectories.ContainsKey(nxm.Game) 
                                    ? settings.ModDirectories[nxm.Game] 
                                    : new System.Collections.Generic.List<string>();

                                var dialog = new Views.ModFolderSelectorWindow(nxm.Game, nxm.ModId, availablePaths);
                                if (dialog.ShowDialog() == true)
                                {
                                    targetDirOverride = dialog.SelectedPath;
                                    settings.SaveModDirectory(nxm.Game, targetDirOverride, dialog.RememberChoice);
                                }
                                else
                                {
                                    userCanceled = true;
                                }
                            });

                            if (userCanceled)
                            {
                                toastService.Show("Batal", "Pemasangan mod dibatalkan oleh pengguna.");
                                return;
                            }
                        }

                        toastService.Show("Mengekstrak Mod...", $"Sedang memasang mod ke {System.IO.Path.GetFileName(targetDirOverride ?? "folder game")}...");
                        var (success, errorMsg, extractPath) = await extractor.ExtractModAsync(savedPath, nxm.Game, targetDirOverride);
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (success)
                            {
                                System.Windows.MessageBox.Show(
                                    $"?? MOD BERHASIL DIPASANG! ??\\n\\n" +
                                    $"File Mod telah sukses diekstrak.\\n\\n" +
                                    $"Lokasi:\\n{extractPath}\\n\\n" +
                                    $"Fase 4 (Smart Extractor) Selesai!",
                                    "LuaTools Mod Manager", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Information);
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    $"? GAGAL EKSTRAK MOD ?\\n\\n" +
                                    $"Alasan: {errorMsg}\\n\\n" +
                                    $"File zip mentahnya masih aman di:\\n{savedPath}",
                                    "LuaTools Mod Manager", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Error);
                            }
                        });'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs updated for ModFolderSelectorWindow")
