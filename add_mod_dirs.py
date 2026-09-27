import codecs

with open('src/LuaToolsGui/Services/SettingsService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Add to AppSettings
app_settings_target = '''    public string? NexusApiKey { get; set; }'''
app_settings_replacement = '''    public string? NexusApiKey { get; set; }
    
    // Maps a Nexus game domain (e.g. "stardewvalley") to its local extraction directory
    public System.Collections.Generic.Dictionary<string, string> ModDirectories { get; set; } = new();'''

text = text.replace(app_settings_target, app_settings_replacement)

# Add to SettingsService
service_target = '''    public string? NexusApiKey'''
service_replacement = '''    public System.Collections.Generic.Dictionary<string, string> ModDirectories => _settings.ModDirectories;
    
    public void SaveModDirectory(string gameDomain, string path)
    {
        _settings.ModDirectories[gameDomain] = path;
        Save();
    }

    public string? NexusApiKey'''

text = text.replace(service_target, service_replacement)

with open('src/LuaToolsGui/Services/SettingsService.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("SettingsService updated")
