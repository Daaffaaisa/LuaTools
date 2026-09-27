using System.Windows;
using LuaToolsGui.ViewModels;

namespace LuaToolsGui.Views;

public partial class AccountSwitcherWindow : Window
{
    public AccountSwitcherWindow(AccountSwitcherViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
