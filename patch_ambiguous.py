import codecs

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target1 = '''MessageBoxButton.YesNo,'''
replacement1 = '''System.Windows.MessageBoxButton.YesNo,'''
text = text.replace(target1, replacement1)

target2 = '''if (result == MessageBoxResult.Yes)'''
replacement2 = '''if (result == System.Windows.MessageBoxResult.Yes)'''
text = text.replace(target2, replacement2)

target3 = '''MessageBoxButton.OK,'''
replacement3 = '''System.Windows.MessageBoxButton.OK,'''
text = text.replace(target3, replacement3)

with open('src/LuaToolsGui/Views/ModDashboardWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Ambiguous references patched!")
