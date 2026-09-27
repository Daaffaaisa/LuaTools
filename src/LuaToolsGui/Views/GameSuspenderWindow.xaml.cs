using System.Windows;
using LuaToolsGui.ViewModels;

namespace LuaToolsGui.Views;

public partial class GameSuspenderWindow : Window
{
    public GameSuspenderWindow(GameSuspenderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
