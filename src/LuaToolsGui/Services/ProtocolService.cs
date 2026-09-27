using System.IO;
using Microsoft.Win32;
using System;

namespace LuaToolsGui.Services;

public static class ProtocolService
{
    private const string ProtocolName = "luatools";
    private const string NxmProtocol = "nxm";

    private static readonly string PendingFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LuaToolsGuiDev", "protocol_url.tmp");

    public static void Register()
    {
        try
        {
            string exePath = Environment.ProcessPath ?? "";
            
            // Register luatools://
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProtocolName}\shell\open\command");
            key.SetValue("", $"\"{exePath}\" \"%1\"");
            using var protoKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProtocolName}");
            protoKey.SetValue("", "URL:LuaTools Protocol");
            protoKey.SetValue("URL Protocol", "");

            // Register nxm:// (Nexus Mods)
            using var nxmKeyCommand = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{NxmProtocol}\shell\open\command");
            nxmKeyCommand.SetValue("", $"\"{exePath}\" \"%1\"");
            using var nxmKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{NxmProtocol}");
            nxmKey.SetValue("", "URL:Nexus Mods Protocol");
            nxmKey.SetValue("URL Protocol", "");
        }
        catch { }
    }

    public static (string? Game, string? ModId, string? FileId, string? QueryParams) ParseNxm(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return (null, null, null, null);
        Uri uri;
        try { uri = new Uri(url); }
        catch { return (null, null, null, null); }

        if (!uri.Scheme.Equals(NxmProtocol, StringComparison.OrdinalIgnoreCase))
            return (null, null, null, null);

        // nxm://<game>/mods/<mod_id>/files/<file_id>?key=...
        string game = uri.Authority; // <game>
        
        string[] segments = uri.AbsolutePath.TrimStart('/').Split('/');
        string? modId = segments.Length > 1 && segments[0].Equals("mods", StringComparison.OrdinalIgnoreCase) ? segments[1] : null;
        string? fileId = segments.Length > 3 && segments[2].Equals("files", StringComparison.OrdinalIgnoreCase) ? segments[3] : null;
        
        string query = uri.Query; // ?key=...&expires=...
        
        return (game, modId, fileId, query);
    }

    public static (string? Action, long? AppId, bool Silent) Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return (null, null, false);
        Uri uri;
        try { uri = new Uri(url); }
        catch { return (null, null, false); }

        if (!uri.Scheme.Equals(ProtocolName, StringComparison.OrdinalIgnoreCase))
            return (null, null, false);

        string action = uri.Authority.ToLowerInvariant();
        if (action is not ("game" or "install" or "manage" or "fix"))
            return (null, null, false);

        string id = uri.AbsolutePath.TrimStart('/');

        // luatools://install/silent/<appid> → run the install headless (tray only + a balloon when done).
        bool silent = false;
        if (action == "install" && id.StartsWith("silent/", StringComparison.OrdinalIgnoreCase))
        {
            silent = true;
            id = id["silent/".Length..];
        }

        if (!long.TryParse(id, out long appId)) return (null, null, false);

        return (action, appId, silent);
    }

    public static void WritePending(string url)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PendingFile)!);
            File.WriteAllText(PendingFile, url);
        }
        catch { }
    }

    public static string? TryReadPending()
    {
        try
        {
            if (File.Exists(PendingFile))
            {
                string url = File.ReadAllText(PendingFile).Trim();
                File.Delete(PendingFile);
                return string.IsNullOrEmpty(url) ? null : url;
            }
        }
        catch { }
        return null;
    }
}
