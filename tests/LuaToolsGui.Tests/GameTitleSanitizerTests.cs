using Xunit;
using LuaToolsGui.Services;

namespace LuaToolsGui.Tests;

public class GameTitleSanitizerTests
{
    [Theory]
    [InlineData("Cyberpunk™ 2077", "Cyberpunk 2077")]
    [InlineData("The Witcher 3: Wild Hunt - Game of the Year Edition", "The Witcher 3")]
    [InlineData("Resident Evil 4 (2023)", "Resident Evil 4")]
    [InlineData("Horizon Zero Dawn™ Complete Edition", "Horizon Zero Dawn")]
    public void Sanitize_RemovesTrademarksAndEditions(string input, string expected)
    {
        Assert.Equal(expected, GameTitleSanitizer.Sanitize(input));
    }
}
