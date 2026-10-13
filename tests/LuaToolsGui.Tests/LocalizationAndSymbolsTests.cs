using LuaToolsGui.Models;
using LuaToolsGui.ViewModels;
using Xunit;

namespace LuaToolsGui.Tests;

public class LocalizationAndSymbolsTests
{
    [Fact]
    public void UniversalGameCard_HasEnglishTitleAndLabel()
    {
        var game = new DenuvoGameListing
        {
            AppId = "UNIVERSAL",
            Name = "Search Other Games (GCW Universal)",
            FixCount = 0
        };
        var vm = new FixGameCardVm(game);

        Assert.Equal("Search Other Games (GCW Universal)", vm.Name);
        Assert.Equal("Community Search", vm.FixCountLabel);
        Assert.DoesNotContain("Cari", vm.Name);
    }

    [Fact]
    public void DepotRow_ManifestLabel_DoesNotContainCorruptedReplacementChar()
    {
        var row = new DepotRow(
            Id: 100,
            Title: "Test Depot",
            Meta: "100",
            IsDlc: false,
            IsShared: false,
            SteamDbUrl: "https://steamdb.info",
            ManifestId: null,
            CommentedManifestId: "1234567890",
            PublicManifestId: "1234567890",
            IsInLua: true,
            IsEnabled: false,
            CanToggle: true,
            IsBaseApp: false);

        Assert.NotNull(row.ManifestLabel);
        Assert.DoesNotContain("\ufffd", row.ManifestLabel);
        Assert.DoesNotContain("?", row.ManifestLabel);
        Assert.Contains("·", row.ManifestLabel);
    }
}
