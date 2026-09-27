import codecs

with open('src/LuaToolsGui/LuaToolsGui.csproj', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('<Version>1.1.3</Version>', '<Version>1.3.2</Version>')
text = text.replace('<Version>1.3.1</Version>', '<Version>1.3.2</Version>')

with open('src/LuaToolsGui/LuaToolsGui.csproj', 'w', encoding='utf-8') as f:
    f.write(text)

print("Version bumped to 1.3.2!")
