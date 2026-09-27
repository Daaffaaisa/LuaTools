import codecs

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''                string finalExtractDir = targetDir;
                try
                {
                    using var archive = ZipFile.OpenRead(zipFilePath);
                    // Cari entri pertama yang punya folder
                    var firstEntry = System.Linq.Enumerable.FirstOrDefault(archive.Entries, e => e.FullName.Contains('/'));
                    if (firstEntry != null)
                    {
                        var zipParts = firstEntry.FullName.Split('/');
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
                                        isFullMatch = false;
                                        break;
                                    }
                                    zipIndex++;
                                }

                                if (isFullMatch)
                                {
                                    // Overlap terdeteksi! Mundurkan targetDir.
                                    var newParts = new string[i];
                                    Array.Copy(targetParts, newParts, i);
                                    
                                    // Handle drive letter root (e.g. "C:") missing the slash if we just string.Join
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
                }
                catch { /* Abaikan jika error baca zip, lanjut ekstrak normal */ }'''

replacement = '''                string finalExtractDir = targetDir;
                try
                {
                    using var archive = ZipFile.OpenRead(zipFilePath);
                    string[] targetParts = targetDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    bool overlapFound = false;

                    // Periksa setiap file/folder di dalam zip sampai kita menemukan overlap
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.Contains('/')) continue;
                        
                        var zipParts = entry.FullName.Split('/');
                        string topZipFolder = zipParts[0];

                        // Cari dari kanan ke kiri pada target directory
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
                                        isFullMatch = false;
                                        break;
                                    }
                                    zipIndex++;
                                }

                                if (isFullMatch)
                                {
                                    // Overlap terdeteksi! Mundurkan targetDir.
                                    var newParts = new string[i];
                                    Array.Copy(targetParts, newParts, i);
                                    
                                    string newTarget = string.Join(Path.DirectorySeparatorChar.ToString(), newParts);
                                    if (newParts.Length == 1 && newParts[0].EndsWith(":"))
                                    {
                                        newTarget += Path.DirectorySeparatorChar;
                                    }
                                    
                                    finalExtractDir = newTarget;
                                    overlapFound = true;
                                    break;
                                }
                            }
                        }
                        if (overlapFound) break;
                    }
                }
                catch { /* Abaikan jika error baca zip, lanjut ekstrak normal */ }'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("ModExtractionService overlap logic refined!")
