import codecs

with open('src/LuaToolsGui/AppConfig.cs', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('"https://github.com/Daaffaaisa/Lua",', '"https://github.com/Daaffaaisa/LuaTools",')

with open('src/LuaToolsGui/AppConfig.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Repo fixed!")
