import codecs

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''                Directory.CreateDirectory(targetDir);

                // Ekstrak Zip-nya (Timpa file kalau sudah ada)
                ZipFile.ExtractToDirectory(zipFilePath, targetDir, overwriteFiles: true);

                return (true, null, targetDir);'''

replacement = '''                // --- SMART ZIP ANALYZER (Overlap Detection) ---
                // Cek apakah zip memuat folder yang overlap dengan targetDir
                // Misal Target: .../Game/Content/Paks/~mods
                // Zip berisi: Game/Content/Paks/~mods/mod.pak
                // Kita akan memundurkan target extraction ke root game agar tidak double-nesting!
                
                string finalExtractDir = targetDir;
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
                catch { /* Abaikan jika error baca zip, lanjut ekstrak normal */ }

                Directory.CreateDirectory(finalExtractDir);

                // Ekstrak Zip-nya (Timpa file kalau sudah ada)
                ZipFile.ExtractToDirectory(zipFilePath, finalExtractDir, overwriteFiles: true);

                return (true, null, finalExtractDir);'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/Services/ModExtractionService.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("ModExtractionService overlap detection added!")
