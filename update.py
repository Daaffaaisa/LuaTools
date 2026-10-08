import re

with open('src/LuaToolsGui/ViewModels/FixesViewModel.cs', 'r', encoding='latin-1') as f:
    text = f.read()

target = r"""        SelectedGame = game;
        _ = game.EnsureCoverAsync(covers); // ensure the flyout header image is cached too
        GcwSearchTitle = LuaToolsGui.Services.GameTitleSanitizer.Sanitize(game.Name);
        GcwFixes.Clear();
        IsGcwManualSearchVisible = false;"""

replacement = r"""        SelectedGame = game;
        _ = game.EnsureCoverAsync(covers); // ensure the flyout header image is cached too
        
        if (game.AppId == "UNIVERSAL")
        {
            GcwSearchTitle = "";
            IsGcwManualSearchVisible = true;
        }
        else
        {
            GcwSearchTitle = LuaToolsGui.Services.GameTitleSanitizer.Sanitize(game.Name);
            IsGcwManualSearchVisible = false;
        }
        
        GcwFixes.Clear();"""

if target in text:
    print("Found! Replacing...")
    text = text.replace(target, replacement)
    with open('src/LuaToolsGui/ViewModels/FixesViewModel.cs', 'w', encoding='latin-1') as f:
        f.write(text)
else:
    print("Not found! Here is the context around it:")
    match = re.search(r'SelectedGame = game;.{0,300}', text, re.DOTALL)
    if match: print(match.group(0))
