using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LuaToolsGui.ViewModels;

public partial class DlcSelectorItemVm : ObservableObject
{
    public long Id { get; }
    public string Title { get; }
    public string Meta { get; }
    public bool IsSoundtrack { get; }
    public bool HasDepot { get; }

    [ObservableProperty]
    private bool _isSelected;

    public Action? OnSelectionChanged { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        OnSelectionChanged?.Invoke();
    }

    public DlcSelectorItemVm(long id, string title, string meta, bool isSoundtrack, bool hasDepot)
    {
        Id = id;
        Title = title;
        Meta = meta;
        IsSoundtrack = isSoundtrack;
        HasDepot = hasDepot;
        _isSelected = !isSoundtrack; // Default: uncheck soundtracks, check game content
    }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        string q = query.Trim();
        return Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || Id.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
            || Meta.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}

public partial class DlcSelectorViewModel : ObservableObject
{
    public string GameName { get; }
    public long AppId { get; }
    public IReadOnlyList<DlcSelectorItemVm> AllItems { get; }
    public ObservableCollection<DlcSelectorItemVm> FilteredItems { get; } = [];

    [ObservableProperty]
    private string _searchQuery = "";

    public int SelectedCount => AllItems.Count(i => i.IsSelected);
    public int TotalCount => AllItems.Count;
    public bool HasSelection => SelectedCount > 0;

    public event Action<bool>? RequestClose;

    public DlcSelectorViewModel(IEnumerable<DlcSelectorItemVm> items, string gameName, long appId)
    {
        GameName = gameName;
        AppId = appId;
        AllItems = items.ToList();

        foreach (var item in AllItems)
        {
            item.OnSelectionChanged = UpdateCounts;
        }

        ApplyFilter();
    }

    private void UpdateCounts()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        foreach (var item in AllItems)
        {
            if (item.Matches(SearchQuery))
                FilteredItems.Add(item);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in FilteredItems)
            item.IsSelected = true;
        UpdateCounts();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in AllItems)
            item.IsSelected = false;
        UpdateCounts();
    }

    [RelayCommand]
    private void Apply()
    {
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    public IEnumerable<(long DlcId, string Name)> GetSelectedDlcs()
    {
        return AllItems.Where(i => i.IsSelected).Select(i => (i.Id, i.Title));
    }
}
