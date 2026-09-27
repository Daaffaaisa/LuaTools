import codecs

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''        - name: 2. Smart Auto-Merge Update Dev Resmi
      run: |'''

replacement = '''    - name: 2. Smart Auto-Merge Update Dev Resmi
      run: |'''

text = text.replace(target, replacement)

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text)

print("Indentation Fixed!")
