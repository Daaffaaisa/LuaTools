import codecs

with open('src/LuaToolsGui/ViewModels/MainViewModel.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''        int plus = ver.IndexOf('+');
        return plus >= 0 ? ver[..plus] : ver;
    }'''

replacement = '''        int plus = ver.IndexOf('+');
        string baseVer = plus >= 0 ? ver[..plus] : ver;
        return baseVer + " (Dev Version)";
    }'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/ViewModels/MainViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("VersionLabel Patched!")
