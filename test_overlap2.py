import os

targetDir = r'C:\Games\FANTASY LIFE i\Game\Content\Paks\~mods'
zipEntries = [
    'Screenshots/preview.jpg',
    'Game/Content/Paks/~mods/mod.pak'
]

finalExtractDir = targetDir

targetParts = targetDir.replace('/', '\\').split('\\')
overlap_found = False

for entry in zipEntries:
    if '/' not in entry:
        continue
        
    zipParts = entry.split('/')
    topZipFolder = zipParts[0]
    
    for i in range(len(targetParts) - 1, -1, -1):
        if targetParts[i].lower() == topZipFolder.lower():
            isFullMatch = True
            zipIndex = 0
            
            for j in range(i, len(targetParts)):
                if zipIndex >= len(zipParts) or targetParts[j].lower() != zipParts[zipIndex].lower():
                    isFullMatch = False
                    break
                zipIndex += 1
                
            if isFullMatch:
                overlap_found = True
                newParts = targetParts[:i]
                newTarget = '\\'.join(newParts)
                if len(newParts) == 1 and newParts[0].endswith(':'):
                    newTarget += '\\'
                finalExtractDir = newTarget
                break
                
    if overlap_found:
        break

print(f"Final: {finalExtractDir}")
