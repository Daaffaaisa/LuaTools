using System.Windows;
using LuaToolsGui.ViewModels;

namespace LuaToolsGui.Views;

public partial class AchievementsWindow : Window
{
    public AchievementsWindow(AchievementsViewModel viewModel, uint appId, string gameName)
    {
        InitializeComponent();

        // Memasukkan data ViewModel ke Window ini
        DataContext = viewModel;
        viewModel.LoadGame(appId, gameName);
    }

    // Memastikan koneksi Steam diputus saat penggunakan menekan tombol close
    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is AchievementsViewModel vm)
        {
            vm.Close();
        }
    }
}