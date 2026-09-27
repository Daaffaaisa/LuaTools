using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LuaToolsGui.Models;

namespace LuaToolsGui.Services;

public class ModRegistryService
{
    private readonly string _registryFilePath;
    
    // GameDomain -> List of Mods
    private Dictionary<string, List<InstalledMod>> _registry = new();

    public ModRegistryService()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LuaTools");
        Directory.CreateDirectory(appData);
        _registryFilePath = Path.Combine(appData, "mods_registry.json");
        Load();
    }

    private void Load()
    {
        if (File.Exists(_registryFilePath))
        {
            try
            {
                string json = File.ReadAllText(_registryFilePath);
                _registry = JsonSerializer.Deserialize<Dictionary<string, List<InstalledMod>>>(json) ?? new();
            }
            catch { _registry = new(); }
        }
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(_registry, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_registryFilePath, json);
        }
        catch { }
    }

    public List<InstalledMod> GetModsForGame(string gameDomain)
    {
        if (_registry.TryGetValue(gameDomain, out var mods))
            return mods;
        return new List<InstalledMod>();
    }

    public void RegisterMod(InstalledMod mod)
    {
        if (!_registry.ContainsKey(mod.GameDomain))
            _registry[mod.GameDomain] = new List<InstalledMod>();
        
        _registry[mod.GameDomain].Add(mod);
        Save();
    }

    public void RemoveMod(string gameDomain, string modId)
    {
        if (_registry.TryGetValue(gameDomain, out var mods))
        {
            var mod = mods.FirstOrDefault(m => m.Id == modId);
            if (mod != null)
            {
                // Delete physical files
                foreach (var file in mod.InstalledFiles)
                {
                    string target = mod.IsEnabled ? file : file + ".disabled";
                    if (File.Exists(target))
                    {
                        try { File.Delete(target); } catch { }
                    }
                }
                mods.Remove(mod);
                Save();
            }
        }
    }

    public bool ToggleMod(string gameDomain, string modId, bool enable)
    {
        if (_registry.TryGetValue(gameDomain, out var mods))
        {
            var mod = mods.FirstOrDefault(m => m.Id == modId);
            if (mod != null && mod.IsEnabled != enable)
            {
                bool allSuccess = true;
                foreach (var file in mod.InstalledFiles)
                {
                    try
                    {
                        string currentPath = enable ? file + ".disabled" : file;
                        string newPath = enable ? file : file + ".disabled";
                        
                        if (File.Exists(currentPath))
                        {
                            File.Move(currentPath, newPath);
                        }
                    }
                    catch 
                    {
                        allSuccess = false;
                    }
                }
                
                mod.IsEnabled = enable;
                Save();
                return allSuccess;
            }
        }
        return false;
    }
}
