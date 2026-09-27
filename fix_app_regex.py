import codecs
import re

with open('src/LuaToolsGui/App.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Remove conflict markers
text = re.sub(r'^<<<<<<< ours\n', '', text, flags=re.MULTILINE)
text = re.sub(r'^=======\n', '', text, flags=re.MULTILINE)
text = re.sub(r'^>>>>>>> theirs\n', '', text, flags=re.MULTILINE)

with open('src/LuaToolsGui/App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Regex clean done!")
