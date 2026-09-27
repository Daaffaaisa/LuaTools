import codecs

with open('src/LuaToolsGui/LuaToolsGui.csproj', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('<AssemblyName>LuaTools</AssemblyName>', '<AssemblyName>LuaToolsDev</AssemblyName>')

with open('src/LuaToolsGui/LuaToolsGui.csproj', 'w', encoding='utf-8') as f:
    f.write(text)

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text2 = f.read()

text2 = text2.replace('vpk pack -u LuaTools -v 99.0.${{ github.run_number }} -p publish_output -o release_output', 'vpk pack -u LuaToolsDev -v 99.0.${{ github.run_number }} -p publish_output -o release_output --mainExe LuaToolsDev.exe')

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text2)

print("Fixes reapplied!")
