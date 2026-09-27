import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Fix List Mutation in HandleProtocolUrl (Download Phase)
target_1 = '''                                var availablePaths = settings.ModDirectories.ContainsKey(nxm.Game) 
                                    ? settings.ModDirectories[nxm.Game] 
                                    : new System.Collections.Generic.List<string>();

                                var dialog = new Views.ModFolderSelectorWindow(nxm.Game, nxm.ModId, availablePaths);'''

replacement_1 = '''                                if (!settings.ModDirectories.ContainsKey(nxm.Game))
                                {
                                    settings.ModDirectories[nxm.Game] = new System.Collections.Generic.List<string>();
                                }
                                var availablePaths = settings.ModDirectories[nxm.Game];

                                var dialog = new Views.ModFolderSelectorWindow(nxm.Game, nxm.ModId, availablePaths);'''
if target_1 in text:
    text = text.replace(target_1, replacement_1)

# 2. Fix List Mutation in ManageViewModel (Setup Phase)
target_2 = '''            var availablePaths = settings.ModDirectories.ContainsKey(guessDomain) 
                ? settings.ModDirectories[guessDomain] 
                : new System.Collections.Generic.List<string>();

            var dialog = new Views.ModFolderSelectorWindow(guessDomain, "", availablePaths, isSetupMode: true)'''

replacement_2 = '''            if (!settings.ModDirectories.ContainsKey(guessDomain))
            {
                settings.ModDirectories[guessDomain] = new System.Collections.Generic.List<string>();
            }
            var availablePaths = settings.ModDirectories[guessDomain];

            var dialog = new Views.ModFolderSelectorWindow(guessDomain, "", availablePaths, isSetupMode: true)'''
if target_2 in text:
    text = text.replace(target_2, replacement_2)

# 3. Replace MessageBox with Toast for final extraction result
target_3 = '''                        Dispatcher.Invoke(() =>
                        {
                            if (success)
                            {
                                System.Windows.MessageBox.Show(
                                    $\"?? MOD BERHASIL DIPASANG! ??\\n\\n\" +
                                    $\"File Mod telah sukses diekstrak.\\n\\n\" +
                                    $\"Lokasi:\\n{extractPath}\\n\\n\" +
                                    $\"Fase 4 (Smart Extractor) Selesai!\",
                                    \"LuaTools Mod Manager\", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Information);
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(
                                    $\"? GAGAL EKSTRAK MOD ?\\n\\n\" +
                                    $\"Alasan: {errorMsg}\\n\\n\" +
                                    $\"File zip mentahnya masih aman di:\\n{savedPath}\",
                                    \"LuaTools Mod Manager\", 
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Error);
                            }
                        });'''

replacement_3 = '''                        Dispatcher.Invoke(() =>
                        {
                            if (success)
                            {
                                toastService.Show("?? Mod Dipasang!", $"Mod ID {nxm.ModId} sukses diekstrak ke:\n{extractPath}");
                            }
                            else
                            {
                                toastService.Show("? Gagal Ekstrak", $"{errorMsg}", true);
                            }
                        });'''
if target_3 in text:
    text = text.replace(target_3, replacement_3)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs fully patched for List Mutation and Toasts")
