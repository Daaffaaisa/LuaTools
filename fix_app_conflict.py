import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''<<<<<<< ours
        // Point the shared HTTP handler at the DNS setting before anything makes a request. It calls
        // this per connection rather than reading it now, so the order is not load-bearing ?" but doing
        // it first means the very first call of the session already honours the user's choice.
        var dnsSettings = _host.Services.GetRequiredService<SettingsService>();
        AppHttp.ModeProvider = () => dnsSettings.DnsMode;
=======
        // Kloningan Shadow Clone masuk ke sini (MENCEGAH Host.StartAsync JALAN)
        if (Program.SamWorkerAppId.HasValue)
        {
            var vm = _host.Services.GetRequiredService<ViewModels.AchievementsViewModel>();
            var dialog = new Views.AchievementsWindow(vm, Program.SamWorkerAppId.Value, Program.SamWorkerGameName ?? "Game");
            
            // Jadikan ini window utama, dan bunuh diri saat ditutup
            MainWindow = dialog;
            dialog.Closed += (s, ev) => Environment.Exit(0);
            
            // Paksa jendela muncul paling depan
            dialog.Loaded += (s, ev) => 
            {
                dialog.Activate();
                dialog.Topmost = true;
                dialog.Topmost = false;
                dialog.Focus();
            };

            dialog.Show();
            return; // Skip sisa inisialisasi aplikasi normal (Termasuk UAC dari Background Service)
        }

        await _host.StartAsync();
>>>>>>> theirs'''

replacement = '''        // Point the shared HTTP handler at the DNS setting before anything makes a request. It calls
        // this per connection rather than reading it now, so the order is not load-bearing - but doing
        // it first means the very first call of the session already honours the user's choice.
        var dnsSettings = _host.Services.GetRequiredService<SettingsService>();
        AppHttp.ModeProvider = () => dnsSettings.DnsMode;

        // Kloningan Shadow Clone masuk ke sini (MENCEGAH Host.StartAsync JALAN)
        if (Program.SamWorkerAppId.HasValue)
        {
            var vm = _host.Services.GetRequiredService<ViewModels.AchievementsViewModel>();
            var dialog = new Views.AchievementsWindow(vm, Program.SamWorkerAppId.Value, Program.SamWorkerGameName ?? "Game");
            
            // Jadikan ini window utama, dan bunuh diri saat ditutup
            MainWindow = dialog;
            dialog.Closed += (s, ev) => Environment.Exit(0);
            
            // Paksa jendela muncul paling depan
            dialog.Loaded += (s, ev) => 
            {
                dialog.Activate();
                dialog.Topmost = true;
                dialog.Topmost = false;
                dialog.Focus();
            };

            dialog.Show();
            return; // Skip sisa inisialisasi aplikasi normal (Termasuk UAC dari Background Service)
        }

        await _host.StartAsync();'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Conflict Resolved!")
