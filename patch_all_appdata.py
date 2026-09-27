import os

dirs_to_scan = ['src/LuaToolsGui/Services', 'src/LuaToolsGui/ViewModels', 'src/LuaToolsGui/Models']

for root, dirs, files in os.walk('src/LuaToolsGui'):
    for file in files:
        if file.endswith('.cs'):
            filepath = os.path.join(root, file)
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Only replace string literals
            new_content = content.replace('"LuaToolsGui"', '"LuaToolsGuiDev"')
            # Be careful with LuaTools, as it might match other things, but usually it's just the folder name.
            # Only one place had "LuaTools" (ModRegistryService) which we already patched in patch_isolation.py.
            
            if new_content != content:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Patched {filepath}")

print("All AppData paths patched!")
