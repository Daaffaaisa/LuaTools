import codecs

with open('src/LuaToolsGui/Views/ManageView.xaml', 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Add to Context Menu
cm_target = '''                                <MenuItem
                                    Command="{Binding PlacementTarget.Tag.MoveStorageCommand, RelativeSource={RelativeSource AncestorType=ContextMenu}}"
                                    CommandParameter="{Binding}"
                                    Header="Move Storage (Symlink)"
                                    Icon="{ui:SymbolIcon FolderArrowRight24}" />'''

cm_replacement = '''                                <MenuItem
                                    Command="{Binding PlacementTarget.Tag.MoveStorageCommand, RelativeSource={RelativeSource AncestorType=ContextMenu}}"
                                    CommandParameter="{Binding}"
                                    Header="Move Storage (Symlink)"
                                    Icon="{ui:SymbolIcon FolderArrowRight24}" />
                                <MenuItem
                                    Command="{Binding PlacementTarget.Tag.ManageModsCommand, RelativeSource={RelativeSource AncestorType=ContextMenu}}"
                                    CommandParameter="{Binding}"
                                    Header="Manage Mod Folders"
                                    Icon="{ui:SymbolIcon Settings24}" />'''

text = text.replace(cm_target, cm_replacement)

# 2. Add to Detail Panel
panel_target = '''                        <ui:Button
                            Margin="0,8,0,0"
                            HorizontalAlignment="Stretch"
                            Command="{Binding DataContext.MoveStorageCommand, ElementName=Root}"
                            CommandParameter="{Binding}"
                            Content="Move Storage (Symlink)"
                            Icon="{ui:SymbolIcon FolderArrowRight24}" />'''

panel_replacement = '''                        <ui:Button
                            Margin="0,8,0,0"
                            HorizontalAlignment="Stretch"
                            Command="{Binding DataContext.MoveStorageCommand, ElementName=Root}"
                            CommandParameter="{Binding}"
                            Content="Move Storage (Symlink)"
                            Icon="{ui:SymbolIcon FolderArrowRight24}" />
                        <ui:Button
                            Margin="0,8,0,0"
                            HorizontalAlignment="Stretch"
                            Command="{Binding DataContext.ManageModsCommand, ElementName=Root}"
                            CommandParameter="{Binding}"
                            Content="Manage Mod Folders"
                            Icon="{ui:SymbolIcon Settings24}" />'''

text = text.replace(panel_target, panel_replacement)

with open('src/LuaToolsGui/Views/ManageView.xaml', 'w', encoding='utf-8') as f:
    f.write(text)

print("ManageView.xaml patched")
