import codecs

with open('src/LuaToolsGui/MainWindow.xaml', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('Title="LuaTools"', 'Title="LuaToolsDev"')

with open('src/LuaToolsGui/MainWindow.xaml', 'w', encoding='utf-8') as f:
    f.write(text)

print("Title patched!")
