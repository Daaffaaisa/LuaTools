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
    [ObservableProperty] private string _statusMessage = "Mencari akun Steam di komputer ini...";
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
                StatusMessage = "⚠️ Steam tidak ditemukan di Registry!";
                return;
            }

            string vdfPath = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (!File.Exists(vdfPath))
            {
                StatusMessage = "⚠️ File loginusers.vdf tidak ditemukan! Coba login ke Steam secara manual sekali.";
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
                StatusMessage = $"✅ Menemukan {Accounts.Count} akun Steam siap dipakai.";
            }
            else
            {
                HasAccounts = false;
                StatusMessage = "⚠️ Belum ada akun yang pernah login di PC ini (atau data belum tersimpan).";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error membaca akun: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SwitchAccountAsync(SteamAccount account)
    {
        if (account == null) return;

        try
        {
            StatusMessage = $"Mengganti ke akun {account.PersonaName}...";

            // 1. Ganti Registry Steam
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "AutoLoginUser", account.AccountName);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "RememberPassword", 1, RegistryValueKind.DWord);
            
            // 2. Bunuh Steam tanpa belas kasihan
            var steamProcesses = Process.GetProcessesByName("steam");
            foreach (var p in steamProcesses)
            {
                p.Kill();
            }

            StatusMessage = "Menunggu Steam mati sepenuhnya...";
            await Task.Delay(2000); 

            // 3. Bangunkan Steam lagi
            string steamExe = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamExe", "")?.ToString() ?? "";
            if (string.IsNullOrEmpty(steamExe) || !File.Exists(steamExe))
            {
                StatusMessage = "Gagal menemukan Steam.exe. Silakan buka Steam manual.";
                return;
            }

            Process.Start(steamExe);
            StatusMessage = $"🚀 Berhasil! Steam dinyalakan kembali sebagai: {account.PersonaName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Gagal mengganti akun: {ex.Message}";
        }
    }
}
