using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuaToolsGui.Services;
using Steamworks;
using Steamworks.Data;

namespace LuaToolsGui.ViewModels;

public partial class AchievementItemViewModel : ObservableObject
{
    public string Identifier { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool OriginalState { get; set; }

    [ObservableProperty]
    private bool _isUnlocked;

    // Properti baru untuk menampung gambar ikon
    [ObservableProperty]
    private ImageSource? _icon;
}

public partial class AchievementsViewModel : ObservableObject
{
    private readonly SteamAchievementService _achievementService;

    [ObservableProperty] private string _gameTitle = "Loading...";
    [ObservableProperty] private int _totalAchievements;
    [ObservableProperty] private int _unlockedAchievements;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private bool _isIdling;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _idleTimeText = "Session Time: 00:00:00";

    private System.Windows.Threading.DispatcherTimer? _idleTimer;
    private DateTime _idleStartTime;

    public System.ComponentModel.ICollectionView AchievementsView { get; }

    public ObservableCollection<AchievementItemViewModel> Achievements { get; } = new();

    public AchievementsViewModel(SteamAchievementService achievementService)
    {
        _achievementService = achievementService;

        // Pasang Lensa ke list Achievements asli
        AchievementsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Achievements);
        AchievementsView.Filter = FilterAchievement;
    }

    partial void OnSearchTextChanged(string value)
    {
        AchievementsView.Refresh();
    }

    // Aturan filternya, apakah text pencarian ada di Nama atau Deskripsi?
    private bool FilterAchievement(object obj)
    {
        if (obj is not AchievementItemViewModel item) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        return item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || item.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnIsIdlingChanged(bool value)
    {
        if (value)
        {
            _idleStartTime = DateTime.Now;

            // Bikin alarm yang berbungi tiap 1 detik
            _idleTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _idleTimer.Tick += (s, e) =>
            {
                var durasi = DateTime.Now - _idleStartTime;
                // Format "D2" artinya selalu 2 digit angka
                IdleTimeText = $"Session Time: {durasi.Hours:D2}:{durasi.Minutes:D2}:{durasi.Seconds:D2}";
            };

            _idleTimer.Start();
        }
        else
        {
            _idleTimer?.Stop();
            IdleTimeText = "Session Time: 00:00:00";
        }
    }

    public void LoadGame(uint appId, string gameName)
    {
        GameTitle = $"Memanajemen Achievement: {gameName}";
        Achievements.Clear();

        if (_achievementService.ConnectToGame(appId, out string errorMsg))
        {
            var achs = _achievementService.GetAllAchievements().ToList();
            
            // Hitung total dan progress saat ini
            TotalAchievements = achs.Count;
            UnlockedAchievements = achs.Count(a => a.State);
            UpdateProgressText();

            foreach (var ach in achs)
            {
                var item = new AchievementItemViewModel
                {
                    Identifier = ach.Identifier,
                    Name = ach.Name,
                    Description = ach.Description,
                    IsUnlocked = ach.State,
                    OriginalState = ach.State
                };
                
                // Download ikonnya di background (paralel) agar aplikasi tidak nge-lag
                _ = LoadIconAsync(item, ach);

                Achievements.Add(item);
            }
        }
        else
        {
            GameTitle = $"Gagal terhubung: {errorMsg}";
        }
    }

    private async Task LoadIconAsync(AchievementItemViewModel item, Achievement ach)
    {
        var img = await ach.GetIconAsync();
        if (img.HasValue)
        {
            var data = img.Value.Data;
            int width = (int)img.Value.Width;
            int height = (int)img.Value.Height;

            // Trik: Tukar warna merah (R) dan biru (B) (Steam RGBA -> WPF BGRA)
            for (int i = 0; i < data.Length; i += 4)
            {
                byte r = data[i];
                data[i] = data[i + 2];
                data[i + 2] = r;
            }

            // Kirim gambar kembali ke UI Thread untuk ditampilkan
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, width * 4);
                item.Icon = bitmap;
            });
        }
    }

    public void UpdateProgressText()
    {
        if (TotalAchievements == 0) return;
        double pct = ((double)UnlockedAchievements / TotalAchievements) * 100;
        ProgressText = $"{UnlockedAchievements} / {TotalAchievements} ({pct:0.0}%)";
    }

    [RelayCommand]
    private void SaveChanges()
    {
        foreach (var ach in Achievements)
        {
            if (ach.IsUnlocked && !ach.OriginalState)
            {
                // Kalau baru dicentang, kita BUKA di steam
                _achievementService.UnlockAchievement(ach.Identifier);
                ach.OriginalState = true;
            }
            else if (!ach.IsUnlocked && ach.OriginalState)
            {
                // Kalau centangnya dihapus, kita cabut dari steam
                _achievementService.LockAchievement(ach.Identifier);
                ach.OriginalState = false;
            }
        }

        // Perbarui angka progres bar supaya otomatis ngikutin jumlah centang
        UnlockedAchievements = Achievements.Count(a => a.IsUnlocked);
        UpdateProgressText();

        _achievementService.SaveChanges();
    }

    [RelayCommand]
    private void UnlockAll()
    {
        // centang semua kotak dalam sedetik
        foreach (var ach in Achievements) ach.IsUnlocked = true;
    }

    [RelayCommand]
    private void LockAll()
    {
        // Buang semua centang dalam sedetik
        foreach (var ach in Achievements) ach.IsUnlocked = false;
    }

    [RelayCommand]
    private void ToggleIdle()
    {
        // Membalikkan status (kalau true jadi false, kalau false jadi true)
        IsIdling = !IsIdling;
    }

    public void Close()
    {
        _achievementService.Disconnect();
    }
}