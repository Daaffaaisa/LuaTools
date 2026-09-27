import codecs

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text = f.read()

target = 'vpk pack -u LuaToolsDev -v 99.0.${{ github.run_number }} -p publish_output -o release_output'
replacement = 'vpk pack -u LuaToolsDev -v 99.0.${{ github.run_number }} -p publish_output -o release_output --mainExe LuaTools.exe'

text = text.replace(target, replacement)

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text)

print("mainExe argument added!")
