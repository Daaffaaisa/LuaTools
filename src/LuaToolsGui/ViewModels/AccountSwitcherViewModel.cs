using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace LuaToolsGui.ViewModels;

public class SteamAccount
{
    public string AccountName { get; set; } = "";
    public string PersonaName { get; set; } = "";
    public string DisplayText => $"{PersonaName} ({AccountName})";
}

public partial class AccountSwitcherViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<SteamAccount> _accounts = new();
    [ObservableProperty] private string _statusMessage = "Scanning for Steam accounts on this PC...";
    [ObservableProperty] private bool _hasAccounts = false;

    public AccountSwitcherViewModel()
    {
        LoadAccounts();
    }

    private void LoadAccounts()
    {
        Accounts.Clear();
        try
        {
            string steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(steamPath))
            {
                StatusMessage = "Steam was not found in the Registry.";
                return;
            }

            string vdfPath = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (!File.Exists(vdfPath))
            {
                StatusMessage = "loginusers.vdf was not found! Please log in to Steam manually once.";
                return;
            }

            string content = File.ReadAllText(vdfPath);
            
            // Baca VDF menggunakan Regex. Format VDF Steam sangat terprediksi.
            var accountMatches = Regex.Matches(content, "\"AccountName\"\\s+\"([^\"]+)\"");
            var personaMatches = Regex.Matches(content, "\"PersonaName\"\\s+\"([^\"]+)\"");

            for (int i = 0; i < accountMatches.Count; i++)
            {
                Accounts.Add(new SteamAccount
                {
                    AccountName = accountMatches[i].Groups[1].Value,
                    PersonaName = personaMatches.Count > i ? personaMatches[i].Groups[1].Value : accountMatches[i].Groups[1].Value
                });
            }

            if (Accounts.Count > 0)
            {
                HasAccounts = true;
                StatusMessage = $"Found {Accounts.Count} Steam account(s) ready to use.";
            }
            else
            {
                HasAccounts = false;
                StatusMessage = "No Steam accounts have logged in on this PC yet (or data not saved).";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading accounts: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SwitchAccountAsync(SteamAccount account)
    {
        if (account == null) return;

        try
        {
            StatusMessage = $"Switching to account {account.PersonaName}...";

            // 1. Ganti Registry Steam
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "AutoLoginUser", account.AccountName);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "RememberPassword", 1, RegistryValueKind.DWord);
            
            // 2. Bunuh Steam tanpa belas kasihan
            var steamProcesses = Process.GetProcessesByName("steam");
            foreach (var p in steamProcesses)
            {
                p.Kill();
            }

            StatusMessage = "Waiting for Steam to close completely...";
            await Task.Delay(2000); 

            // 3. Bangunkan Steam lagi
            string steamExe = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamExe", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(steamExe) || !File.Exists(steamExe))
            {
                StatusMessage = "Failed to locate Steam.exe. Please open Steam manually.";
                return;
            }

            Process.Start(steamExe);
            StatusMessage = $"Success! Steam restarted as: {account.PersonaName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to switch account: {ex.Message}";
        }
    }
}
