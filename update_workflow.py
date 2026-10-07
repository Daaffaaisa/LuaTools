import codecs

with open('.github/workflows/auto-pabrik.yml', 'r', encoding='utf-8') as f:
    text = f.read()

target_on = '''on:
  schedule:
    - cron: '0 0 * * *'
  workflow_dispatch:'''

replacement_on = '''on:
  push:
    branches: [ "main" ]
  schedule:
    - cron: '0 0 * * *'
  workflow_dispatch:'''

text = text.replace(target_on, replacement_on)

target_if = '''        if [ "$BEFORE" != "$AFTER" ] || [ "${{ github.event_name }}" == "workflow_dispatch" ]; then'''
replacement_if = '''        if [ "$BEFORE" != "$AFTER" ] || [ "${{ github.event_name }}" == "workflow_dispatch" ] || [ "${{ github.event_name }}" == "push" ]; then'''

text = text.replace(target_if, replacement_if)

with open('.github/workflows/auto-pabrik.yml', 'w', encoding='utf-8') as f:
    f.write(text)

print("Workflow updated for push trigger!")
