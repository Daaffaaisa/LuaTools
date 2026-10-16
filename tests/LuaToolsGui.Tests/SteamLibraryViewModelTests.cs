using System.Collections.Generic;
using System.IO;
using System.Linq;
using LuaToolsGui.Services;
using LuaToolsGui.ViewModels;
using Xunit;

namespace LuaToolsGui.Tests;

public class SteamLibraryViewModelTests
{
    [Fact]
    public void SteamLibraryItemVm_DetectsInjectedStatusCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "SteamLibTest_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        string stPlugIn = Path.Combine(tempDir, "config", "stplug-in");
        Directory.CreateDirectory(stPlugIn);

        try
        {
            // Game 1 has a lua file in stplug-in
            File.WriteAllText(Path.Combine(stPlugIn, "1091500.lua"), "addappid(1091500)\naddappid(2138330)");

            // Game 2 does not have a lua file
            var item1 = new SteamLibraryItemVm(
                1091500,
                "Cyberpunk 2077",
                @"D:\SteamLibrary\steamapps\common\Cyberpunk 2077",
                stPlugIn);

            var item2 = new SteamLibraryItemVm(
                1245620,
                "Elden Ring",
                @"D:\SteamLibrary\steamapps\common\ELDEN RING",
                stPlugIn);

            Assert.True(item1.IsDlcInjected);
            Assert.False(item1.IsVanilla);

            Assert.False(item2.IsDlcInjected);
            Assert.True(item2.IsVanilla);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void SteamLibraryItemVm_GenerateDlcLua_CreatesValidLuaFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "SteamLibTest_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        string stPlugIn = Path.Combine(tempDir, "config", "stplug-in");
        Directory.CreateDirectory(stPlugIn);

        try
        {
            var item = new SteamLibraryItemVm(
                1245620,
                "Elden Ring",
                @"D:\SteamLibrary\steamapps\common\ELDEN RING",
                stPlugIn);

            var dlcs = new List<(long DlcId, string? Name)>
            {
                (2778580, "Shadow of the Erdtree"),
                (2778590, "Digital Artbook & Original Soundtrack")
            };

            item.CreateOrUpdateLua(dlcs);

            string luaPath = Path.Combine(stPlugIn, "1245620.lua");
            Assert.True(File.Exists(luaPath));

            string content = File.ReadAllText(luaPath);
            Assert.Contains("addappid(1245620)", content);
            Assert.Contains("addappid(2778580)", content);
            Assert.Contains("Shadow of the Erdtree", content);
            Assert.Contains("addappid(2778590)", content);

            Assert.True(item.IsDlcInjected);
            Assert.False(item.IsVanilla);

            // Now test remove
            item.RemoveLua();
            Assert.False(File.Exists(luaPath));
            Assert.False(item.IsDlcInjected);
            Assert.True(item.IsVanilla);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void SteamLibraryViewModel_Filtering_AppliesSearchAndStatusFilter()
    {
        var items = new List<SteamLibraryItemVm>
        {
            new(1091500, "Cyberpunk 2077", @"C:\Games\Cyberpunk", "") { IsDlcInjected = true },
            new(1245620, "Elden Ring", @"C:\Games\Elden Ring", "") { IsDlcInjected = false },
            new(570, "Dota 2", @"C:\Games\Dota 2", "") { IsDlcInjected = true }
        };

        var vm = new SteamLibraryViewModel();
        vm.SetTestGames(items);

        // Default: All 3 games
        Assert.Equal(3, vm.FilteredGames.Count);

        // Search: "Elden"
        vm.SearchText = "Elden";
        Assert.Single(vm.FilteredGames);
        Assert.Equal(1245620, vm.FilteredGames[0].AppId);

        // Clear search, filter to Injected only
        vm.SearchText = "";
        vm.CurrentFilter = SteamLibraryFilter.Injected;
        Assert.Equal(2, vm.FilteredGames.Count);
        Assert.All(vm.FilteredGames, g => Assert.True(g.IsDlcInjected));

        // Filter to Vanilla only
        vm.CurrentFilter = SteamLibraryFilter.Vanilla;
        Assert.Single(vm.FilteredGames);
        Assert.Equal(1245620, vm.FilteredGames[0].AppId);
    }

    [Fact]
    public void SteamLibraryViewModel_Filtering_InstalledAndNotInstalledFilters()
    {
        var items = new List<SteamLibraryItemVm>
        {
            new(1091500, "Cyberpunk 2077", @"C:\Games\Cyberpunk", "", isInstalled: true) { IsDlcInjected = true },
            new(1245620, "Elden Ring", @"C:\Games\Elden Ring", "", isInstalled: true) { IsDlcInjected = false },
            new(570, "Dota 2", "", "", isInstalled: false)
        };

        var vm = new SteamLibraryViewModel();
        vm.SetTestGames(items);

        Assert.Equal(3, vm.TotalGamesCount);
        Assert.Equal(2, vm.InstalledCount);
        Assert.Equal(1, vm.NotInstalledCount);
        Assert.Equal(1, vm.InjectedCount);
        Assert.Equal(1, vm.VanillaCount);

        // Filter Installed
        vm.SetFilterCommand.Execute("Installed");
        Assert.Equal(SteamLibraryFilter.Installed, vm.CurrentFilter);
        Assert.True(vm.IsFilterInstalled);
        Assert.Equal(2, vm.FilteredGames.Count);
        Assert.All(vm.FilteredGames, g => Assert.True(g.IsInstalled));

        // Filter Not Installed
        vm.SetFilterCommand.Execute("NotInstalled");
        Assert.Equal(SteamLibraryFilter.NotInstalled, vm.CurrentFilter);
        Assert.True(vm.IsFilterNotInstalled);
        Assert.Single(vm.FilteredGames);
        Assert.Equal(570, vm.FilteredGames[0].AppId);
        Assert.False(vm.FilteredGames[0].IsInstalled);
    }

    [Fact]
    public void LuaTileViewModel_IsOriginal_DefaultsToFalseAndCanBeSet()
    {
        var tile = new LuaTileViewModel(1091500, @"C:\Games\Cyberpunk\1091500.lua", System.DateTime.UtcNow, "Cyberpunk 2077", false);
        Assert.False(tile.IsOriginal);

        tile.IsOriginal = true;
        Assert.True(tile.IsOriginal);
    }

    [Fact]
    public void SteamLibraryViewModel_FlyoutSelection_OpensAndClosesCorrectly()
    {
        var vm = new SteamLibraryViewModel();
        var item = new SteamLibraryItemVm(1091500, "Cyberpunk 2077", @"C:\Games\Cyberpunk", "");

        Assert.False(vm.IsFlyoutOpen);
        Assert.Null(vm.SelectedGame);

        vm.SelectGameCommand.Execute(item);
        Assert.True(vm.IsFlyoutOpen);
        Assert.Equal(item, vm.SelectedGame);

        vm.CloseFlyoutCommand.Execute(null);
        Assert.False(vm.IsFlyoutOpen);
        Assert.Null(vm.SelectedGame);
    }

    [Fact]
    public void SteamLibraryViewModel_ActionDelegates_TriggerExpectedCallbacks()
    {
        var vm = new SteamLibraryViewModel();
        var item = new SteamLibraryItemVm(1245620, "Elden Ring", @"D:\Steam\Elden Ring", "");

        long launchAppId = 0;
        string launchName = "";
        vm.OpenLaunchOptions = (id, name) => { launchAppId = id; launchName = name; };

        long achAppId = 0;
        string achName = "";
        vm.OpenAchievements = (id, name) => { achAppId = id; achName = name; };

        SteamLibraryItemVm? modItem = null;
        vm.OpenModSettings = i => modItem = i;

        SteamLibraryItemVm? storageItem = null;
        vm.OpenStorageMover = i => storageItem = i;

        SteamLibraryItemVm? cleanerItem = null;
        vm.OpenLeftoverCleaner = i => cleanerItem = i;

        long buildsAppId = 0;
        vm.NavigateToBuilds = id => buildsAppId = id;

        // Execute commands
        vm.EditLaunchOptionsCommand.Execute(item);
        Assert.Equal(1245620, launchAppId);
        Assert.Equal("Elden Ring", launchName);

        vm.ManageAchievementsCommand.Execute(item);
        Assert.Equal(1245620, achAppId);
        Assert.Equal("Elden Ring", achName);

        vm.ManageModsCommand.Execute(item);
        Assert.Equal(item, modItem);

        vm.MoveStorageCommand.Execute(item);
        Assert.Equal(item, storageItem);

        vm.CleanLeftoversCommand.Execute(item);
        Assert.Equal(item, cleanerItem);

        vm.NavigateBuildsCommand.Execute(item);
        Assert.Equal(1245620, buildsAppId);
    }

    [Fact]
    public void SteamLibraryService_EnumerateOwnedGames_DetectsOwnedGames()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "SteamOwnedTest_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create config/loginusers.vdf
            string configDir = Path.Combine(tempDir, "config");
            Directory.CreateDirectory(configDir);
            File.WriteAllText(Path.Combine(configDir, "loginusers.vdf"), 
                "\"users\" { \"76561198875534981\" { \"AccountName\" \"user1\" \"mostrecent\" \"1\" } }");

            // Account ID: 915269253
            string libCacheDir = Path.Combine(tempDir, "userdata", "915269253", "config", "librarycache");
            Directory.CreateDirectory(libCacheDir);
            File.WriteAllText(Path.Combine(libCacheDir, "1085660.json"), "{}");
            File.WriteAllText(Path.Combine(libCacheDir, "1245620.json"), "{}");

            // Localconfig.vdf
            string localConfig = Path.Combine(tempDir, "userdata", "915269253", "config", "localconfig.vdf");
            File.WriteAllText(localConfig, "\"UserLocalConfigStore\" { \"Software\" { \"Valve\" { \"Steam\" { \"apps\" { \"570\" { \"playtime\" \"100\" } } } } } }");

            var settings = new SettingsService();
            settings.SteamPathOverride = tempDir;
            var steam = new SteamService(settings);
            var service = new SteamLibraryService(steam);

            uint? accId = service.GetActiveAccountId();
            Assert.Equal(915269253u, accId);

            var ownedAppIds = service.EnumerateOwnedAppIds();
            Assert.Contains(1085660, ownedAppIds);
            Assert.Contains(1245620, ownedAppIds);
            Assert.Contains(570, ownedAppIds);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void SteamLibraryItemVm_StatusBadge_ReflectsInstalledAndDlcStatus()
    {
        var itemInstalledVanilla = new SteamLibraryItemVm(1245620, "Elden Ring", @"C:\Games\Elden Ring", "", isInstalled: true) { IsDlcInjected = false };
        Assert.True(itemInstalledVanilla.IsInstalled);
        Assert.Equal("Original", itemInstalledVanilla.StatusBadge);
        Assert.Equal("#10b981", itemInstalledVanilla.StatusBadgeBackground);

        var itemInstalledInjected = new SteamLibraryItemVm(1091500, "Cyberpunk 2077", @"C:\Games\Cyberpunk", "", isInstalled: true) { IsDlcInjected = true };
        Assert.True(itemInstalledInjected.IsInstalled);
        Assert.Equal("DLCs Injected", itemInstalledInjected.StatusBadge);
        Assert.Equal("#8b5cf6", itemInstalledInjected.StatusBadgeBackground);

        var itemNotInstalled = new SteamLibraryItemVm(570, "Dota 2", "", "", isInstalled: false);
        Assert.False(itemNotInstalled.IsInstalled);
        Assert.Equal("Not Installed", itemNotInstalled.StatusBadge);
        Assert.Equal("#475569", itemNotInstalled.StatusBadgeBackground);
    }
}



