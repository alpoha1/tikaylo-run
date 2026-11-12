using UnityEngine;

namespace TilkayoRun
{
    /// <summary>Все игровые константы в одном месте.</summary>
    public static class GameConfig
    {
        public const string Version = "1.0.0";
        public const string SaveKey = "tilkayo_run_save_v1";

        // Дорога
        public const float LaneWidth = 2.2f;
        public const float SegmentLength = 40f;
        public const int SegmentCount = 7;
        public const float SegmentBack = 25f;
        public const float SpawnAhead = 135f;

        // Скорость
        public const float StartSpeed = 14f;
        public const float MaxSpeed = 34f;
        public const float SpeedGain = 0.20f;     // м/с прироста скорости за секунду
        public const float MenuSpeed = 5f;

        // Физика персонажа
        public const float JumpVelocity = 11.5f;
        public const float Gravity = 32f;
        public const float FastFallVelocity = -30f;
        public const float SlideTime = 0.65f;
        public const float LaneChangeSpeed = 20f;

        // Очки
        public const int CoinScore = 5;
    }

    public enum Rarity { Common = 0, Uncommon = 1, Rare = 2, Legendary = 3 }

    public static class RarityInfo
    {
        public static string Name(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return "Обычный";
                case Rarity.Uncommon: return "Необычный";
                case Rarity.Rare: return "Редкий";
                default: return "Легендарный";
            }
        }

        public static Color Tint(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return new Color32(170, 178, 190, 255);
                case Rarity.Uncommon: return new Color32(96, 214, 110, 255);
                case Rarity.Rare: return new Color32(76, 160, 255, 255);
                default: return new Color32(255, 176, 32, 255);
            }
        }

        /// <summary>Сколько монет возвращается, если скин из кейса уже куплен.</summary>
        public static int DuplicateRefund(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return 60;
                case Rarity.Uncommon: return 120;
                case Rarity.Rare: return 300;
                default: return 800;
            }
        }
    }

    /// <summary>Описание уровня (темы леса).</summary>
    public class ThemeDef
    {
        public string name;
        public int unlockDistance;      // лучший пройденный путь (м), нужный для открытия
        public Color skyTop, skyMid, skyBottom;
        public Color ground, groundAlt, roadA, roadB, mortar;
        public Color trunk, leafA, leafB, leafC, rock, accent, pit;
        public Color sun, ambient, sunLight, dust;
        public Vector3 sunDir = new Vector3(-0.35f, 0.85f, -0.4f);
        public bool stars;
        public float fogStart = 40f, fogEnd = 150f;
        public int treeStyle;           // 0 ели, 1 лиственные, 2 смешанный
        public float speedBonus;        // добавка к стартовой скорости
    }

    public static class Themes
    {
        static Color H(string hex)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            return c;
        }

        public static readonly ThemeDef[] All = new ThemeDef[]
        {
            new ThemeDef
            {
                name = "Закатный лес", unlockDistance = 0,
                skyTop = H("#3b2a6b"), skyMid = H("#d0587a"), skyBottom = H("#ff9d5c"),
                ground = H("#27503b"), groundAlt = H("#2f6a48"), roadA = H("#8d8478"), roadB = H("#a39a8c"), mortar = H("#3a342e"),
                trunk = H("#5a3d2b"), leafA = H("#2f7a4a"), leafB = H("#3f9a52"), leafC = H("#e0a43a"),
                rock = H("#7a736d"), accent = H("#ffd166"), pit = H("#120d14"),
                sun = H("#ffd98a"), ambient = new Color(0.52f, 0.44f, 0.5f, 1), sunLight = new Color(0.62f, 0.52f, 0.4f, 1),
                dust = H("#ffd36b"), treeStyle = 2, speedBonus = 0f
            },
            new ThemeDef
            {
                name = "Ночной лес", unlockDistance = 600,
                skyTop = H("#060a22"), skyMid = H("#14214f"), skyBottom = H("#24376e"),
                ground = H("#10261f"), groundAlt = H("#17352b"), roadA = H("#4a5163"), roadB = H("#5b6378"), mortar = H("#151a26"),
                trunk = H("#2c2230"), leafA = H("#1d5a55"), leafB = H("#2b7a6e"), leafC = H("#4056b8"),
                rock = H("#3b4254"), accent = H("#9dffb0"), pit = H("#05060c"),
                sun = H("#e6efff"), ambient = new Color(0.4f, 0.44f, 0.62f, 1), sunLight = new Color(0.34f, 0.42f, 0.62f, 1),
                dust = H("#baff7a"), stars = true, treeStyle = 0, speedBonus = 1.5f, fogStart = 35f, fogEnd = 140f
            },
            new ThemeDef
            {
                name = "Туманный рассвет", unlockDistance = 1500,
                skyTop = H("#7b6fb3"), skyMid = H("#e7a6c2"), skyBottom = H("#f6d2c8"),
                ground = H("#4a7a68"), groundAlt = H("#5a9078"), roadA = H("#9a9a9e"), roadB = H("#b0b0b4"), mortar = H("#4a4a52"),
                trunk = H("#6b5649"), leafA = H("#5c9a7e"), leafB = H("#7bb592"), leafC = H("#f0a6c0"),
                rock = H("#8a8a92"), accent = H("#fff3b0"), pit = H("#1a1624"),
                sun = H("#fff0c4"), ambient = new Color(0.6f, 0.56f, 0.62f, 1), sunLight = new Color(0.5f, 0.45f, 0.45f, 1),
                dust = H("#ffffff"), treeStyle = 1, speedBonus = 3f, fogStart = 18f, fogEnd = 115f
            },
            new ThemeDef
            {
                name = "Осенняя чаща", unlockDistance = 3000,
                skyTop = H("#4a2b3d"), skyMid = H("#c4623f"), skyBottom = H("#f0a057"),
                ground = H("#5a4320"), groundAlt = H("#6e5128"), roadA = H("#8a7458"), roadB = H("#a08a68"), mortar = H("#3a2c1e"),
                trunk = H("#4a2f20"), leafA = H("#c9461f"), leafB = H("#e8892b"), leafC = H("#d9b23a"),
                rock = H("#7a6a58"), accent = H("#ff9f40"), pit = H("#140d0a"),
                sun = H("#ffcf85"), ambient = new Color(0.55f, 0.45f, 0.42f, 1), sunLight = new Color(0.65f, 0.5f, 0.35f, 1),
                dust = H("#ffa24a"), treeStyle = 1, speedBonus = 4.5f
            }
        };
    }
}
