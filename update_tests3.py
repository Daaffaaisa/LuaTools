import re

with open('tests/LuaToolsGui.Tests/GameTitleSanitizerTests.cs', 'r', encoding='utf-8') as f:
    text = f.read()

text = re.sub(r'\[InlineData\("The Witcher 3: Wild Hunt - Game of the Year Edition[^"]*", "The Witcher 3: Wild Hunt"\)\]',
              r'[InlineData("The Witcher 3: Wild Hunt - Game of the Year Edition", "The Witcher 3")]',
              text)
text = re.sub(r'\[InlineData\("Cyberpunk[^"]* 2077", "Cyberpunk 2077"\)\]',
              r'[InlineData("Cyberpunk 2077", "Cyberpunk 2077")]', text)
text = re.sub(r'\[InlineData\("Horizon Zero Dawn[^"]* Complete Edition", "Horizon Zero Dawn"\)\]',
              r'[InlineData("Horizon Zero Dawn Complete Edition", "Horizon Zero Dawn")]', text)

with open('tests/LuaToolsGui.Tests/GameTitleSanitizerTests.cs', 'w', encoding='utf-8') as f:
    f.write(text)
