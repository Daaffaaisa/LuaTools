using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuaToolsGui.ViewModels;

public partial class SettingsViewModel
{
    [ObservableProperty] private string _nexusKeyInput = "";
    [ObservableProperty] private bool _nexusIsKeyConfigured;
    [ObservableProperty] private string? _nexusKeyStatus;

    [RelayCommand]
    private void SaveNexusKey()
    {
        string key = NexusKeyInput.Trim();
        if (string.IsNullOrEmpty(key))
        {
            NexusKeyStatus = "API Key cannot be empty.";
            return;
        }

        _settings.NexusApiKey = key;
        NexusIsKeyConfigured = true;
        NexusKeyInput = "";
        NexusKeyStatus = "API Key successfully saved.";
    }

    [RelayCommand]
    private void ClearNexusKey()
    {
        _settings.NexusApiKey = null;
        NexusIsKeyConfigured = false;
        NexusKeyInput = "";
        NexusKeyStatus = null;
    }
}
