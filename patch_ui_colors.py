import codecs

# 1. Update XAML
with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

target_combo = '''<ComboBox x:Name="FoldersCombo" SelectionChanged="FoldersCombo_SelectionChanged" Foreground="White" Background="#22FFFFFF"/>'''
replacement_combo = '''<ComboBox x:Name="FoldersCombo" SelectionChanged="FoldersCombo_SelectionChanged" Foreground="White" Background="#22FFFFFF" SelectedValuePath="Path">
                            <ComboBox.ItemTemplate>
                                <DataTemplate>
                                    <StackPanel Orientation="Horizontal">
                                        <Border Width="4" Height="14" CornerRadius="2" Background="{Binding ColorHex}" Margin="0,0,8,0"/>
                                        <TextBlock Text="{Binding Path}" VerticalAlignment="Center"/>
                                    </StackPanel>
                                </DataTemplate>
                            </ComboBox.ItemTemplate>
                        </ComboBox>'''
xaml = xaml.replace(target_combo, replacement_combo)

target_list = '''<Grid Margin="0,5">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    
                                    <StackPanel VerticalAlignment="Center">'''
replacement_list = '''<Grid Margin="0,5">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    
                                    <Border Grid.Column="0" Width="4" CornerRadius="2" Background="{Binding ColorHex}" Margin="0,2,12,2" VerticalAlignment="Stretch"/>
                                    
                                    <StackPanel Grid.Column="1" VerticalAlignment="Center">'''
xaml = xaml.replace(target_list, replacement_list)

target_toggle = '''<ui:ToggleSwitch Grid.Column="1"'''
replacement_toggle = '''<ui:ToggleSwitch Grid.Column="2"'''
xaml = xaml.replace(target_toggle, replacement_toggle)

target_delete = '''<ui:Button Grid.Column="2" Icon="{ui:SymbolIcon Delete24}"'''
replacement_delete = '''<ui:Button Grid.Column="3" Icon="{ui:SymbolIcon Delete24}"'''
xaml = xaml.replace(target_delete, replacement_delete)

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)

print("XAML updated for colors.")
