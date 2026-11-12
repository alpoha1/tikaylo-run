using System;
using System.Collections.Generic;
using UnityEngine;

namespace TilkayoRun
{
    [Serializable]
    public class SaveData
    {
        public int bestScore;
        public int bestDistance;
        public int coins;
        public List<int> ownedSkins = new List<int> { 0 };
        public int selectedSkin;
        public bool sfxOn = true;
        public bool musicOn = true;
        public float sfxVolume = 0.8f;
        public float musicVolume = 0.6f;
        public bool vibration = true;
        public int quality = -1;          // -1 = ещё не выбрано (авто)
        public int level;                 // выбранный уровень
        public int unlockedLevel;         // максимальный открытый уровень
        public int totalRuns;
        public int totalCoins;
    }

    /// <summary>Локальное сохранение (PlayerPrefs, JSON). Никакой сети.</summary>
    public static class SaveSystem
    {
        static SaveData data;
        public static event Action OnChanged;

        public static SaveData Data
        {
            get { if (data == null) Load(); return data; }
        }

        public static void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(GameConfig.SaveKey, "");
                data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save load failed: " + e.Message);
                data = null;
            }
            if (data == null) data = new SaveData();
            Sanitize();
        }

        static void Sanitize()
        {
            if (data.ownedSkins == null) data.ownedSkins = new List<int>();
            if (!data.ownedSkins.Contains(0)) data.ownedSkins.Add(0);
            if (!data.ownedSkins.Contains(data.selectedSkin)) data.selectedSkin = 0;
            data.coins = Mathf.Max(0, data.coins);
            data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
            data.musicVolume = Mathf.Clamp01(data.musicVolume);
            data.level = Mathf.Clamp(data.level, 0, Themes.All.Length - 1);
            RecomputeUnlocked();
            if (data.level > data.unlockedLevel) data.level = data.unlockedLevel;
        }

        public static void RecomputeUnlocked()
        {
            int u = 0;
            for (int i = 0; i < Themes.All.Length; i++)
                if (data.bestDistance >= Themes.All[i].unlockDistance) u = i;
            data.unlockedLevel = Mathf.Max(data.unlockedLevel, u);
        }

        public static void Commit()
        {
            try
            {
                PlayerPrefs.SetString(GameConfig.SaveKey, JsonUtility.ToJson(Data));
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
            if (OnChanged != null) OnChanged();
        }

        public static bool Owns(int skinId) { return Data.ownedSkins.Contains(skinId); }

        public static void AddCoins(int n)
        {
            Data.coins = Mathf.Max(0, Data.coins + n);
            if (n > 0) Data.totalCoins += n;
        }
    }
}
