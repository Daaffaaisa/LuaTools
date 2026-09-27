import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

logic_target = '''                        Dispatcher.Invoke(() =>
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
                        });'''

logic_replacement = '''                        if (!success && errorMsg == "NEEDS_SETUP")
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

text = text.replace(logic_target, logic_replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs patch applied")
