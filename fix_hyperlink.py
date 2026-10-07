import codecs

# 1. Patch XAML
with open('src/LuaToolsGui/Views/SettingsView.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

xaml = xaml.replace(
    '<Hyperlink NavigateUri="https://next.nexusmods.com/settings/api-keys" Foreground="#a78bfa">Nexus Mods Settings</Hyperlink>',
    '<Hyperlink NavigateUri="https://next.nexusmods.com/settings/api-keys" RequestNavigate="Hyperlink_RequestNavigate" Foreground="#a78bfa">Nexus Mods Settings</Hyperlink>'
)

with open('src/LuaToolsGui/Views/SettingsView.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)

# 2. Patch Code-Behind
with open('src/LuaToolsGui/Views/SettingsView.xaml.cs', 'r', encoding='utf-8') as f:
    cs = f.read()

target = '''    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _vm = viewModel;
        Loaded += (_, _) => _vm.OnViewLoaded();
    }'''

replacement = '''    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _vm = viewModel;
        Loaded += (_, _) => _vm.OnViewLoaded();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }'''

cs = cs.replace(target, replacement)

with open('src/LuaToolsGui/Views/SettingsView.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs)

print("Hyperlink crash fixed!")
