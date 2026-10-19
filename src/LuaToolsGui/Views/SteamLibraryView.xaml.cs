using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LuaToolsGui.ViewModels;

namespace LuaToolsGui.Views;

public partial class SteamLibraryView : UserControl
{
    private readonly SteamLibraryViewModel _viewModel;

    public SteamLibraryView(SteamLibraryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;

        Loaded += async (_, _) =>
        {
            if (_viewModel.FilteredGames.Count == 0 && !_viewModel.IsLoading)
            {
                await _viewModel.ScanLibraryAsync();
            }
        };
    }

    private void Tile_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SteamLibraryItemVm tile) _viewModel.ResolveTile(tile);
    }

    private void Tile_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SteamLibraryItemVm tile }) _viewModel.ResolveTile(tile);
    }

    private void Tile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SteamLibraryItemVm game })
        {
            _viewModel.SelectGameCommand.Execute(game);
        }
    }

    private void Scrim_Click(object sender, MouseButtonEventArgs e)
    {
        _viewModel.CloseFlyoutCommand.Execute(null);
    }
}
