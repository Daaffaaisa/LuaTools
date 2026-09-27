import os

targetDir = r'C:\Games\FANTASY LIFE i\Game\Content\Paks\~mods'
zipEntries = ['Game/Content/Paks/~mods/mod.pak']

finalExtractDir = targetDir
firstEntryFullName = next((e for e in zipEntries if '/' in e), None)

if firstEntryFullName:
    zipParts = firstEntryFullName.split('/')
    topZipFolder = zipParts[0]

    targetParts = targetDir.replace('/', '\\').split('\\')
    
    for i in range(len(targetParts) - 1, -1, -1):
        if targetParts[i].lower() == topZipFolder.lower():
            isFullMatch = True
            zipIndex = 0
            
            for j in range(i, len(targetParts)):
                if zipIndex >= len(zipParts) or targetParts[j].lower() != zipParts[zipIndex].lower():
                    print(f"Mismatch at target '{targetParts[j]}' vs zip '{zipParts[zipIndex] if zipIndex < len(zipParts) else 'OUT_OF_BOUNDS'}'")
                    isFullMatch = False
                    break
                zipIndex += 1
                
            if isFullMatch:
                print("Full match found!")
                newParts = targetParts[:i]
                newTarget = '\\'.join(newParts)
                if len(newParts) == 1 and newParts[0].endswith(':'):
                    newTarget += '\\'
                finalExtractDir = newTarget
                break

print(f"Final: {finalExtractDir}")
