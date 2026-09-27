import codecs

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('vpk pack -u LuaTools -v', 'vpk pack -u LuaToolsCustom -v')

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text)

print("App ID changed to LuaToolsCustom!")
