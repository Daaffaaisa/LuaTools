using System.Collections.Generic;
using System.Windows;
using LuaToolsGui.Services;
using Wpf.Ui.Controls;
using Microsoft.Win32;

namespace LuaToolsGui.Views;

public partial class ModFolderSelectorWindow : FluentWindow
{
    public string SelectedPath { get; private set; } = string.Empty;
    public bool RememberChoice { get; private set; } = false;

    private readonly List<string> _availablePaths;

    private string? _initialDefaultPath;

    public ModFolderSelectorWindow(string gameDomain, string modId, List<string> availablePaths, bool isSetupMode = false, string? currentDefaultPath = null)
    {
        InitializeComponent();
        _initialDefaultPath = currentDefaultPath;
        
        if (isSetupMode)
        {
            Title = "Pengaturan Mod Folder";
            SubtitleText.Text = $"Game: {gameDomain} (Manual Setup)";
            ExtractButton.Content = "Simpan Pengaturan";
            ExtractButton.Icon = new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Controls.SymbolRegular.Save24);
        }
        else
        {
            SubtitleText.Text = $"Game: {gameDomain}  |  Mod ID: {modId}";
        }
        
        _availablePaths = availablePaths ?? new List<string>();
        RefreshList();
    }

    private void RefreshList()
    {
        PathsListBox.ItemsSource = null;
        PathsListBox.ItemsSource = _availablePaths;
        
        if (_availablePaths.Count > 0)
        {
            if (!string.IsNullOrEmpty(_initialDefaultPath) && _availablePaths.Contains(_initialDefaultPath))
            {
                PathsListBox.SelectedItem = _initialDefaultPath;
                RememberCheckBox.IsChecked = true;
            }
            else
            {
                PathsListBox.SelectedIndex = 0;
            }
        }
    }

    private void PathsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ExtractButton.IsEnabled = PathsListBox.SelectedItem != null;
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Pilih folder instalasi mod (misal: Mods atau Data)"
        };

        if (dialog.ShowDialog() == true)
        {
            string newPath = dialog.FolderName;
            if (!_availablePaths.Contains(newPath))
            {
                _availablePaths.Add(newPath);
            }
            RefreshList();
            PathsListBox.SelectedItem = newPath;
        }
    }

    private void Extract_Click(object sender, RoutedEventArgs e)
    {
        if (PathsListBox.SelectedItem is string path)
        {
            SelectedPath = path;
            RememberChoice = RememberCheckBox.IsChecked == true;
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
