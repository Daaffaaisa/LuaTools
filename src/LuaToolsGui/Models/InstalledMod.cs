using System;
using System.Collections.Generic;

namespace LuaToolsGui.Models;

public class InstalledMod
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string GameDomain { get; set; } = string.Empty;
    
    // "Nexus" or "Manual"
    public string Source { get; set; } = "Nexus";
    public string NexusModId { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; } = DateTime.Now;
    
    // Absolute paths to all files that were extracted
    public List<string> InstalledFiles { get; set; } = new();
}
