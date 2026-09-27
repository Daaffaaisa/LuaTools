import codecs

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''    - name: 2. Gabungkan dengan Update Dev Resmi
      continue-on-error: true
      run: |
        git config user.name "Robot Pabrik"
        git config user.email "robot@pabrik.com"
        git remote add upstream https://github.com/madoiscool/LuaTools.git
        git fetch upstream
        git merge upstream/main --allow-unrelated-histories -m "Auto-Merge Update Resmi"
        
    - name: 3. Setup Mesin .NET 8'''

replacement = '''    # Langkah Merge Sementara dimatikan dulu untuk rilis perdana
    - name: 3. Setup Mesin .NET 8'''

text = text.replace(target, replacement)

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text)

print("YML Patched!")
