using System;
using System.Windows;
using LuaToolsGui.Views;

namespace TestXaml
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                var win = new ModDashboardWindow("test", null, null, null, null);
                Console.WriteLine("Created OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Crash: {ex}");
            }
        }
    }
}
