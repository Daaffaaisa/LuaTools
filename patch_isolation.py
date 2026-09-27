import codecs
import re

# 1. Patch Mutex in Program.cs
with open('src/LuaToolsGui/Program.cs', 'r', encoding='utf-8') as f:
    prog = f.read()

prog = prog.replace('"LuaToolsGui.SingleInstance"', '"LuaToolsGuiDev.SingleInstance"')
with open('src/LuaToolsGui/Program.cs', 'w', encoding='utf-8') as f:
    f.write(prog)

# 2. Patch AppData paths
def patch_appdata(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        text = f.read()
    
    # Simple replaces for specific known strings
    text = text.replace('"LuaToolsGui"', '"LuaToolsGuiDev"')
    text = text.replace('"LuaTools"', '"LuaToolsDev"')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(text)

files_to_patch = [
    'src/LuaToolsGui/Services/ModRegistryService.cs',
    'src/LuaToolsGui/Services/CefInjectorService.cs',
    'src/LuaToolsGui/Services/LuaVault.cs',
    'src/LuaToolsGui/Program.cs'
]

for fp in files_to_patch:
    try:
        patch_appdata(fp)
    except:
        pass

print("Isolation patched!")
