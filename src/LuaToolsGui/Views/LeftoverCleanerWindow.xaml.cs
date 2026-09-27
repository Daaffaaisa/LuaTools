using System.Windows;
using LuaToolsGui.ViewModels;

namespace LuaToolsGui.Views;

public partial class LeftoverCleanerWindow : Window
{
    public LeftoverCleanerWindow(LeftoverCleanerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseWindow = Close;
    }
}
