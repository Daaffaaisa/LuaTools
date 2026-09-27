using System;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        string targetDir = @"C:\Games\FANTASY LIFE i\Game\Content\Paks\~mods";
        string[] zipEntries = new[] { "Game/Content/Paks/~mods/mod.pak" };

        string finalExtractDir = targetDir;

        // Cari entri pertama yang punya folder
        var firstEntryFullName = zipEntries.FirstOrDefault(e => e.Contains('/'));
        if (firstEntryFullName != null)
        {
            var zipParts = firstEntryFullName.Split('/');
            string topZipFolder = zipParts[0];

            string[] targetParts = targetDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Cari dari kanan ke kiri
            for (int i = targetParts.Length - 1; i >= 0; i--)
            {
                if (string.Equals(targetParts[i], topZipFolder, StringComparison.OrdinalIgnoreCase))
                {
                    bool isFullMatch = true;
                    int zipIndex = 0;
                    
                    for (int j = i; j < targetParts.Length; j++)
                    {
                        if (zipIndex >= zipParts.Length || 
                            !string.Equals(targetParts[j], zipParts[zipIndex], StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Mismatch at target '{targetParts[j]}' vs zip '{zipParts[zipIndex]}'");
                            isFullMatch = false;
                            break;
                        }
                        zipIndex++;
                    }

                    if (isFullMatch)
                    {
                        Console.WriteLine("Full match found!");
                        var newParts = new string[i];
                        Array.Copy(targetParts, newParts, i);
                        
                        string newTarget = string.Join(Path.DirectorySeparatorChar.ToString(), newParts);
                        if (newParts.Length == 1 && newParts[0].EndsWith(":"))
                        {
                            newTarget += Path.DirectorySeparatorChar;
                        }
                        
                        finalExtractDir = newTarget;
                        break;
                    }
                }
            }
        }
        Console.WriteLine($"Final: {finalExtractDir}");
    }
}
