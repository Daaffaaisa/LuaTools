using System.Windows;

namespace LuaToolsGui.Views;

public partial class SymlinkMoverWindow
{
    public SymlinkMoverWindow(ViewModels.SymlinkMoverViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseWindow = Close;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
