import codecs

with open('src/LuaToolsGui/AppConfig.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '''    public static readonly string[] GithubReleasesRepos =
    [
        "https://github.com/madoiscool/LuaTools",   // primary
        "https://github.com/mendy-tools/LuaTools",  // backup. Create this repo + re-upload the Velopack
                                                    // assets ONLY if the primary goes down (404s harmlessly
                                                    // until then; UpdateService just falls through past it).
    ];'''

replacement = '''    public static readonly string[] GithubReleasesRepos =
    [
        "https://github.com/USERNAME_KAMU/LuaTools_Custom", // ganti dengan URL Repo kamu bosku!
    ];'''

text = text.replace(target, replacement)

with open('src/LuaToolsGui/AppConfig.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("URL Patched!")
