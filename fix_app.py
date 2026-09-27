import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Fix in HandleProtocolUrl (Download phase)
target_1 = '''                                var availablePaths = settings.ModDirectories.ContainsKey(nxm.Game) 
                                    ? settings.ModDirectories[nxm.Game] 
                                    : new System.Collections.Generic.List<string>();

                                var dialog = new Views.ModFolderSelectorWindow(nxm.Game, nxm.ModId, availablePaths);
                                if (dialog.ShowDialog() == true)
                                {
                                    targetDirOverride = dialog.SelectedPath;
                                    settings.SaveModDirectory(nxm.Game, targetDirOverride, dialog.RememberChoice);
                                }'''

replacement_1 = '''                                if (!settings.ModDirectories.ContainsKey(nxm.Game))
                                {
                                    settings.ModDirectories[nxm.Game] = new System.Collections.Generic.List<string>();
                                }
                                var availablePaths = settings.ModDirectories[nxm.Game];

                                var dialog = new Views.ModFolderSelectorWindow(nxm.Game, nxm.ModId, availablePaths);
                                if (dialog.ShowDialog() == true)
                                {
                                    targetDirOverride = dialog.SelectedPath;
                                    settings.SaveModDirectory(nxm.Game, targetDirOverride, dialog.RememberChoice);
                                }'''
text = text.replace(target_1, replacement_1)

# Fix in ManageViewModel (Setup phase)
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

text = text.replace(target_2, replacement_2)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("App.xaml.cs list mutation fixed!")
