using System;
using System.Collections.Generic;
using UnityEngine;

namespace TilkayoRun
{
    public enum Accessory
    {
        None, Glasses, Sunglasses, PartyHat, Bow, TopHat, WizardHat, Horns,
        Crown, Halo, Headphones, CowboyHat, PirateHat, Headband
    }

    public enum AuraKind { None, Sparkle, Fire, Snow, Ghost, Neon, Gold, Rainbow }

    /// <summary>Скин = перекраска одного спрайта + 3D-аксессуар + аура частиц.</summary>
    public class SkinDef
    {
        public int id;
        public string name;
        public Rarity rarity;
        public float hue;                 // поворот оттенка, градусы
        public float sat = 1f;            // насыщенность
        public float bright = 1f;        // яркость
        public Color tint = Color.white;  // множитель цвета
        public float alpha = 1f;
        public bool rainbow;              // оттенок плавно меняется
        public Color outline = new Color(0, 0, 0, 0);   // контур-свечение (alpha 0 = нет)
        public Accessory accessory;
        public Color accColor = Color.white;
        public AuraKind aura;
        public Color auraColor = Color.white;
    }

    public class CaseDef
    {
        public string name;
        public string subtitle;
        public int price;
        public float[] chance;     // проценты для [Обычный, Необычный, Редкий, Легендарный], сумма = 100
        public Color color;
    }

    public static class SkinDatabase
    {
        public static readonly List<SkinDef> All = new List<SkinDef>();
        public static readonly CaseDef[] Cases;

        static Color C(string hex)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.white;
            return c;
        }

        static SkinDef Add(string name, Rarity r, float hue, float sat, float bright, string tint,
            Accessory acc = Accessory.None, string accColor = "#ffffff", AuraKind aura = AuraKind.None, string auraColor = "#ffffff")
        {
            var s = new SkinDef
            {
                id = All.Count, name = name, rarity = r, hue = hue, sat = sat, bright = bright,
                tint = C(tint), accessory = acc, accColor = C(accColor), aura = aura, auraColor = C(auraColor)
            };
            All.Add(s);
            return s;
        }

        static SkinDatabase()
        {
            // ---------- Обычные ----------
            Add("Классический Тилкайо", Rarity.Common, 0, 1f, 1f, "#ffffff");
            Add("Рыжий закат", Rarity.Common, -6, 1.45f, 1.05f, "#fff0dd");
            Add("Лесной мох", Rarity.Common, 75, 1.1f, 0.95f, "#ffffff");
            Add("Снежок", Rarity.Common, 0, 0.12f, 1.5f, "#f4f8ff");
            Add("Уголёк", Rarity.Common, 0, 0.25f, 0.5f, "#ffffff");
            Add("Лазурный", Rarity.Common, 175, 1.2f, 1.05f, "#ffffff");
            Add("Малина", Rarity.Common, -55, 1.3f, 1f, "#ffffff");
            Add("Песчаный", Rarity.Common, 5, 0.55f, 1.3f, "#fff4d6");

            // ---------- Необычные ----------
            Add("Очкарик", Rarity.Uncommon, 0, 1f, 1f, "#ffffff", Accessory.Glasses, "#2b2b33");
            Add("Именинник", Rarity.Uncommon, 28, 1.2f, 1.05f, "#ffffff", Accessory.PartyHat, "#ff4fa0");
            Add("Модник", Rarity.Uncommon, -22, 1.2f, 1f, "#ffffff", Accessory.Bow, "#e0243c");
            Add("Меломан", Rarity.Uncommon, 120, 1.1f, 1f, "#ffffff", Accessory.Headphones, "#ff7a1a");
            Add("Мятный франт", Rarity.Uncommon, 100, 0.9f, 1.25f, "#d8fff0", Accessory.Sunglasses, "#101018");
            Add("Фиолетовый сон", Rarity.Uncommon, -95, 1.2f, 0.95f, "#ffffff", Accessory.Glasses, "#f2d34a");
            Add("Ковбой", Rarity.Uncommon, 0, 1.1f, 1f, "#ffffff", Accessory.CowboyHat, "#8a5a2b");
            Add("Клубничный", Rarity.Uncommon, -38, 1.35f, 1.1f, "#ffffff", Accessory.Bow, "#7bd96a");

            // ---------- Редкие ----------
            Add("Джентльмен", Rarity.Rare, 0, 0.8f, 1f, "#ffffff", Accessory.TopHat, "#1a1a22", AuraKind.None);
            Add("Маг Леса", Rarity.Rare, -100, 1.2f, 1f, "#ffffff", Accessory.WizardHat, "#5b3fd0", AuraKind.Sparkle, "#c89bff");
            var dev = Add("Дьяволёнок", Rarity.Rare, -28, 1.6f, 1f, "#ffb0a0", Accessory.Horns, "#7a0f18", AuraKind.Fire, "#ff7a2a");
            dev.outline = C("#ff3a1a80");
            var ice = Add("Ледяной", Rarity.Rare, 180, 0.7f, 1.35f, "#dff6ff", Accessory.None, "#ffffff", AuraKind.Snow, "#d8f4ff");
            ice.outline = C("#6fe0ff70");
            Add("Пират", Rarity.Rare, 0, 0.8f, 0.9f, "#ffffff", Accessory.PirateHat, "#16161c", AuraKind.None);
            Add("Ниндзя", Rarity.Rare, 0, 0.3f, 0.42f, "#ffffff", Accessory.Headband, "#e02030", AuraKind.None);
            var fly = Add("Светлячок", Rarity.Rare, 62, 1.2f, 1.25f, "#fff7b0", Accessory.None, "#ffffff", AuraKind.Sparkle, "#f4ff7a");
            fly.outline = C("#f7ff5a70");

            // ---------- Легендарные ----------
            var king = Add("Король Леса", Rarity.Legendary, 22, 1.7f, 1.2f, "#ffe9a0", Accessory.Crown, "#ffcc1a", AuraKind.Gold, "#ffd84a");
            king.outline = C("#ffc21a90");
            var ang = Add("Ангел", Rarity.Legendary, 0, 0.15f, 1.55f, "#fffaf0", Accessory.Halo, "#fff2a0", AuraKind.Sparkle, "#fff7c8");
            ang.outline = C("#fff6b090");
            var gh = Add("Призрак", Rarity.Legendary, 180, 0.3f, 1.4f, "#bfeeff", Accessory.None, "#ffffff", AuraKind.Ghost, "#a8f0ff");
            gh.alpha = 0.62f; gh.outline = C("#7be8ff80");
            var rb = Add("Радуга", Rarity.Legendary, 0, 1.6f, 1.2f, "#ffffff", Accessory.PartyHat, "#ffffff", AuraKind.Rainbow, "#ffffff");
            rb.rainbow = true; rb.outline = C("#ffffff60");
            var lava = Add("Лавовый", Rarity.Legendary, -20, 2f, 1.3f, "#ff7a2a", Accessory.Horns, "#2a1208", AuraKind.Fire, "#ff9a20");
            lava.outline = C("#ff5a10aa");
            var neon = Add("Неон", Rarity.Legendary, 0, 0.2f, 0.35f, "#a040ff", Accessory.Sunglasses, "#ff2fd0", AuraKind.Neon, "#ff3fe0");
            neon.outline = C("#d53cffcc");
            var gold = Add("Золотой идол", Rarity.Legendary, 25, 2.3f, 1.55f, "#ffd966", Accessory.Halo, "#ffb81a", AuraKind.Gold, "#ffe27a");
            gold.outline = C("#ffd24acc");

            Cases = new[]
            {
                new CaseDef { name = "Лесной кейс", subtitle = "Для начала пути", price = 300,
                    chance = new[] { 60f, 25f, 12f, 3f }, color = C("#3f9a52") },
                new CaseDef { name = "Лунный кейс", subtitle = "Больше редких находок", price = 900,
                    chance = new[] { 30f, 40f, 22f, 8f }, color = C("#4056b8") },
                new CaseDef { name = "Королевский кейс", subtitle = "Без обычных скинов", price = 2500,
                    chance = new[] { 0f, 25f, 50f, 25f }, color = C("#c9861a") },
            };
        }

        public static SkinDef Get(int id)
        {
            if (id < 0 || id >= All.Count) return All[0];
            return All[id];
        }

        public static List<SkinDef> ByRarity(Rarity r)
        {
            var list = new List<SkinDef>();
            for (int i = 0; i < All.Count; i++) if (All[i].rarity == r) list.Add(All[i]);
            return list;
        }

        public static Rarity RollRarity(CaseDef c, System.Random rng)
        {
            double roll = rng.NextDouble() * 100.0;
            double acc = 0;
            Rarity last = Rarity.Common;
            for (int i = 0; i < 4; i++)
            {
                if (c.chance[i] <= 0f) continue;
                last = (Rarity)i;
                acc += c.chance[i];
                if (roll < acc) return last;
            }
            return last;
        }

        public static SkinDef RollSkin(CaseDef c, System.Random rng)
        {
            var pool = ByRarity(RollRarity(c, rng));
            return pool[rng.Next(pool.Count)];
        }
    }
}
