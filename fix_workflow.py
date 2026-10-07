import codecs

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('uses: softprops/action-gh-release@v1', 'uses: softprops/action-gh-release@v2')

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text)

print("Workflow updated.")
