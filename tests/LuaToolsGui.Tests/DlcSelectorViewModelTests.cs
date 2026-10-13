using System.Collections.Generic;
using System.Linq;
using LuaToolsGui.ViewModels;
using Xunit;

namespace LuaToolsGui.Tests;

public class DlcSelectorViewModelTests
{
    private static List<DlcSelectorItemVm> CreateSampleItems()
    {
        return new List<DlcSelectorItemVm>
        {
            new(101, "Shadow of the Erdtree", "License Only", isSoundtrack: false, hasDepot: false),
            new(102, "Digital Artbook & Original Soundtrack", "Files: 1.2 GB", isSoundtrack: true, hasDepot: true),
            new(103, "Colosseum Pack", "Files: 500 MB", isSoundtrack: false, hasDepot: true)
        };
    }

    [Fact]
    public void InitialSelection_SelectsGameDlc_ExcludesSoundtrack()
    {
        var items = CreateSampleItems();
        var vm = new DlcSelectorViewModel(items, "Elden Ring", 1245620);

        Assert.True(items[0].IsSelected);  // Game DLC
        Assert.False(items[1].IsSelected); // Soundtrack
        Assert.True(items[2].IsSelected);  // Game DLC
        Assert.Equal(2, vm.SelectedCount);
    }

    [Fact]
    public void SelectAll_SelectsAllItems()
    {
        var items = CreateSampleItems();
        var vm = new DlcSelectorViewModel(items, "Elden Ring", 1245620);

        vm.SelectAllCommand.Execute(null);

        Assert.All(items, i => Assert.True(i.IsSelected));
        Assert.Equal(3, vm.SelectedCount);
    }

    [Fact]
    public void DeselectAll_ClearsAllSelection()
    {
        var items = CreateSampleItems();
        var vm = new DlcSelectorViewModel(items, "Elden Ring", 1245620);

        vm.DeselectAllCommand.Execute(null);

        Assert.All(items, i => Assert.False(i.IsSelected));
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public void Filter_MatchesTitleAndId()
    {
        var items = CreateSampleItems();
        var vm = new DlcSelectorViewModel(items, "Elden Ring", 1245620);

        vm.SearchQuery = "Erdtree";
        Assert.Single(vm.FilteredItems);
        Assert.Equal(101, vm.FilteredItems[0].Id);

        vm.SearchQuery = "102";
        Assert.Single(vm.FilteredItems);
        Assert.Equal(102, vm.FilteredItems[0].Id);

        vm.SearchQuery = "";
        Assert.Equal(3, vm.FilteredItems.Count);
    }

    [Fact]
    public void GetSelectedDlcs_ReturnsOnlyCheckedItems()
    {
        var items = CreateSampleItems();
        var vm = new DlcSelectorViewModel(items, "Elden Ring", 1245620);

        items[0].IsSelected = true;
        items[1].IsSelected = false;
        items[2].IsSelected = false;

        var selected = vm.GetSelectedDlcs().ToList();

        Assert.Single(selected);
        Assert.Equal(101, selected[0].DlcId);
        Assert.Equal("Shadow of the Erdtree", selected[0].Name);
    }
}
