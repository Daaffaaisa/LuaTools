import codecs

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''services.AddSingleton<Services.ModExtractionService>();'''
replacement = '''services.AddSingleton<Services.ModExtractionService>();
                services.AddSingleton<Services.ModRegistryService>();'''
text = text.replace(target, replacement)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("DI Patched!")
