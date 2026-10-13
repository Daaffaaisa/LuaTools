using LuaToolsGui.Services;
using Xunit;

namespace LuaToolsGui.Tests;

public class DlcClassifierTests
{
    [Theory]
    [InlineData("SILENT HILL 2 - Original Soundtrack", true)]
    [InlineData("Cyberpunk 2077: Original Score", true)]
    [InlineData("Elden Ring OST", true)]
    [InlineData("Game (OST)", true)]
    [InlineData("Game - OST", true)]
    [InlineData("Persona 3 Reload: Velvet BGM Set", true)]
    [InlineData("Bonus Music Collection", true)]
    [InlineData("Digital Artbook & Original Soundtrack", true)]
    [InlineData("Costume Pack", false)] // "ost" inside "Costume" must NOT match!
    [InlineData("Hostage Scenario DLC", false)] // "ost" inside "Hostage" must NOT match!
    [InlineData("Shadow of the Erdtree", false)]
    [InlineData("Blood and Wine", false)]
    public void IsSoundtrack_ClassifiesAccurately(string title, bool expected)
    {
        Assert.Equal(expected, DlcClassifier.IsSoundtrack(title));
    }

    [Theory]
    [InlineData("SILENT HILL 2 - Artbook", true)]
    [InlineData("Digital Artbook", true)]
    [InlineData("Adventure Guide Art Book", true)]
    [InlineData("Desktop Wallpaper Pack", true)]
    [InlineData("Avatars Pack", true)]
    [InlineData("Digital Art & Comic", true)]
    [InlineData("Deluxe Extras", true)]
    [InlineData("Bonus Content Pack", true)]
    [InlineData("Pre-Order Bonus", false)] // In-game pre-order entitlement should be considered Game DLC
    [InlineData("Colosseum Pack", false)]
    [InlineData("Shadow of the Erdtree", false)]
    public void IsBonus_ClassifiesAccurately(string title, bool expected)
    {
        Assert.Equal(expected, DlcClassifier.IsBonus(title));
    }
}
