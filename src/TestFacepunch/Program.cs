using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        string yaml = await client.GetStringAsync("https://raw.githubusercontent.com/mtkennerly/ludusavi-manifest/master/data/manifest.yaml");
        
        string[] lines = yaml.Split('\n');
        
        long appId = 413150;
        int targetIdLine = -1;
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].Contains("id: " + appId) || lines[i].Contains("- " + appId)) {
                targetIdLine = i; break;
            }
        }
        if (targetIdLine != -1) {
            int gameStartLine = -1;
            for (int i = targetIdLine; i >= 0; i--) {
                if (lines[i].Length > 0 && !lines[i].StartsWith(" ") && lines[i].TrimEnd().EndsWith(":")) {
                    gameStartLine = i; break;
                }
            }
            
            if (gameStartLine != -1) {
                Console.WriteLine("GAME NAME: " + lines[gameStartLine]);
                bool inFiles = false;
                string foundPath = null;
                for (int i = gameStartLine + 1; i < lines.Length; i++) {
                    if (lines[i].Length > 0 && !lines[i].StartsWith(" ") && lines[i].TrimEnd().EndsWith(":")) break;
                    
                    if (lines[i].StartsWith("  files:")) inFiles = true;
                    else if (inFiles && lines[i].StartsWith("  ") && !lines[i].StartsWith("    ")) inFiles = false; // exited files block
                    else if (inFiles) {
                        if (lines[i].Trim().StartsWith("\"<win")) {
                            foundPath = lines[i].Split('"')[1];
                            break;
                        }
                    }
                }
                Console.WriteLine("FOUND PATH: " + foundPath);
            }
        }
    }
}
