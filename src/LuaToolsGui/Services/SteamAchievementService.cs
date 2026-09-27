using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Steamworks.Data;

namespace LuaToolsGui.Services;

public class SteamAchievementService
{
    /// <summary>
    /// Terhubung ke Steam API
    /// </summary>
    
    public bool ConnectToGame(uint appId, out string errorMessage)
    {
        errorMessage = "";
        try
        {
            // jika sebelumnya masih ada koneksi yang nyangkut, maka disconnect dulu
            if (SteamClient.IsValid) SteamClient.Shutdown();

            // Setup facepunch to explicitly use the given appid
            System.Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());

            // Inisialisasi koneksi klien Steam
            SteamClient.Init(appId);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Mengambil semua daftar achievement dari game yang sedang aktif
    /// </summary>
    public IEnumerable<Achievement> GetAllAchievements()
    {
        if (!SteamClient.IsValid) return Enumerable.Empty<Achievement>();

        // Memaksa Steam menarik status terbaru dari server
        SteamUserStats.RequestCurrentStats();

        return SteamUserStats.Achievements;
    }

    /// <summary>
    /// Membuka (unlock) achievement berdasarkan identifiernya
    /// </summary>
    public void UnlockAchievement(string identifier)
    {
        if (!SteamClient.IsValid) return;

        foreach (var ach in SteamUserStats.Achievements.Where(x => x.Identifier == identifier))
        {
            ach.Trigger();
        }
    }

    /// <summary>
    /// Mengunci kembali achievement
    /// </summary>
    public void LockAchievement(String identifier)
    {
        if (!SteamClient.IsValid) return;

        foreach (var ach in SteamUserStats.Achievements.Where(x => x.Identifier == identifier))
        {
            ach.Clear();
        }
    }

    /// <summary>
    /// Menyimpan dan menyinkronkan perubahan ke server pusat Steam
    /// </summary>
    public void SaveChanges()
    {
        if (!SteamClient.IsValid) return;
        SteamUserStats.StoreStats();
    }

    /// <summary>
    /// Memutuskan koneksi dari Steam API
    /// </summary>
    public void Disconnect()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.Shutdown();
        }
        System.Environment.SetEnvironmentVariable("SteamAppId", null);
    }
}