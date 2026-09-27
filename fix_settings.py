import codecs

with open('src/LuaToolsGui/Services/SettingsService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''    public string? HubcapApiKey { get; set; }
    public string? NexusApiKey { get; set; }
    
    // Maps a Nexus game domain (e.g. "stardewvalley") to its local extraction directory
    public System.Collections.Generic.Dictionary<string, string> ModDirectories { get; set; } = new();'''

replacement = '''    public string? HubcapApiKey { get; set; }
    public string? NexusApiKey { get; set; }
    
    // Maps a Nexus game domain (e.g. "stardewvalley") to a list of known mod directories (e.g. UE4SS vs ~mods)
    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> ModDirectories { get; set; } = new();
    
    // Maps a Nexus game domain to its chosen default directory (bypasses prompt)
    public System.Collections.Generic.Dictionary<string, string> DefaultModDirectories { get; set; } = new();'''

text = text.replace(target, replacement)

method_target = '''    public System.Collections.Generic.Dictionary<string, string> ModDirectories => _settings.ModDirectories;
    
    public void SaveModDirectory(string gameDomain, string path)
    {
        _settings.ModDirectories[gameDomain] = path;
        Save();
    }'''

method_replacement = '''    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> ModDirectories => _settings.ModDirectories;
    public System.Collections.Generic.Dictionary<string, string> DefaultModDirectories => _settings.DefaultModDirectories;
    
    public void SaveModDirectory(string gameDomain, string path, bool isDefault)
    {
        if (!_settings.ModDirectories.ContainsKey(gameDomain))
        {
            _settings.ModDirectories[gameDomain] = new System.Collections.Generic.List<string>();
        }
        
        if (!_settings.ModDirectories[gameDomain].Contains(path))
        {
            _settings.ModDirectories[gameDomain].Add(path);
        }
        
        if (isDefault)
        {
            _settings.DefaultModDirectories[gameDomain] = path;
        }
        Save();
    }'''

text = text.replace(method_target, method_replacement)

with open('src/LuaToolsGui/Services/SettingsService.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("SettingsService updated for Multi-Path")
