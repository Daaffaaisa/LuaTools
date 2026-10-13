using System.Windows;
using LuaToolsGui.ViewModels;

namespace LuaToolsGui.Views;

public partial class DlcSelectorWindow : Wpf.Ui.Controls.FluentWindow
{
    public DlcSelectorViewModel ViewModel { get; }

    public DlcSelectorWindow(DlcSelectorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        viewModel.RequestClose += success =>
        {
            DialogResult = success;
            Close();
        };

        if (Application.Current?.MainWindow is { } owner && owner != this && owner.IsVisible)
        {
            Owner = owner;
        }
    }
}
