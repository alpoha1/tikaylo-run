using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TilkayoRun
{
    /// <summary>Главное меню, HUD, пауза, итоги забега, настройки. Всё строится из кода.</summary>
    public class UIManager : MonoBehaviour
    {
        GameController gc;
        Canvas canvas;
        RectTransform safe;
        Rect lastSafe;

        RectTransform menu, hud, pause, over, settings, loading;
        Text menuBest, menuCoins, menuLevel, menuLevelSub, menuVersion;
        Text hudDist, hudCoins, countdown, toast, overTitle, overNew, overScore, overDist, overCoins, overBest;
        int lastDist = -1, lastCoins = -1;
        Coroutine toastRoutine, pulseRoutine;
        public ShopUI shop;

        // настройки
        Button qLow, qMid, qHigh;
        Button[] qButtons;

        public static Color Dark(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, 1f); }

        public void Build(GameController controller)
        {
            gc = controller;

            var cgo = new GameObject("UI");
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            cgo.AddComponent<GraphicRaycaster>();

            safe = UIKit.NewRect("SafeArea", cgo.transform);
            UIKit.Fill(safe);

            BuildMenu();
            BuildHud();
            BuildPause();
            BuildOver();
            BuildSettings();

            shop = new ShopUI();
            shop.Build(safe, this, gc);

            // тост и загрузка поверх всего
            var trt = UIKit.NewRect("Toast", safe);
            UIKit.At(trt, new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(640, 80));
            var tb = trt.gameObject.AddComponent<Image>();
            tb.sprite = UIKit.Round; tb.type = Image.Type.Sliced; tb.color = new Color(0, 0, 0, 0.8f); tb.raycastTarget = false;
            toast = UIKit.Label(trt, "", 30, Color.white);
            trt.gameObject.SetActive(false);

            loading = UIKit.Box(safe, "Loading", new Color32(16, 12, 30, 255), false);
            UIKit.Fill(loading);
            var lt = UIKit.Label(loading, "ТИЛКАЙО <color=#FFC53D>RUN</color>", 96, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(lt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(680, 160));
            var lt2 = UIKit.Label(loading, "Загрузка леса…", 38, new Color(1, 1, 1, 0.8f));
            UIKit.At(lt2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(680, 70));

            HideAll();
        }

        void Update()
        {
            // безопасная зона (вырезы экрана)
            if (Time.frameCount % 30 == 0)
            {
                Rect sa = Screen.safeArea;
                if (sa != lastSafe && Screen.width > 0)
                {
                    lastSafe = sa;
                    safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
                    safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
                    safe.offsetMin = safe.offsetMax = Vector2.zero;
                }
            }
        }

        void HideAll()
        {
            menu.gameObject.SetActive(false); hud.gameObject.SetActive(false); pause.gameObject.SetActive(false);
            over.gameObject.SetActive(false); settings.gameObject.SetActive(false);
            if (shop != null) shop.Hide();
        }

        // ============================================================ меню
        void BuildMenu()
        {
            menu = UIKit.NewRect("Menu", safe); UIKit.Fill(menu);

            var title = UIKit.Label(menu, "ТИЛКАЙО <color=#FFC53D>RUN</color>", 110, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 40; title.resizeTextMaxSize = 110;
            UIKit.At(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(690, 160));
            var sub = UIKit.Label(menu, "Беги по лесной тропе, собирай монеты!", 28, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter, FontStyle.Normal, true);
            UIKit.At(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -305), new Vector2(690, 50));

            // рекорд
            var rec = UIKit.Box(menu, "RecordPill", new Color(0, 0, 0, 0.5f));
            UIKit.At(rec, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(330, 70));
            menuBest = UIKit.Label(rec, "РЕКОРД: 0", 30, Color.white);

            // монеты
            var cp = UIKit.Box(menu, "CoinPill", new Color(0, 0, 0, 0.5f));
            UIKit.At(cp, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(270, 70));
            var ci = UIKit.CoinIcon(cp, 44);
            UIKit.At(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 44));
            menuCoins = UIKit.Label(cp, "0", 34, UIKit.Gold, TextAnchor.MiddleRight);
            menuCoins.rectTransform.offsetMin = new Vector2(70, 0); menuCoins.rectTransform.offsetMax = new Vector2(-20, 0);

            // кнопки
            var play = UIKit.MakeButton(menu, "НАЧАТЬ ЗАБЕГ", new Vector2(600, 160), UIKit.Green, () => gc.StartRun(), 54);
            UIKit.At((RectTransform)play.transform, new Vector2(0.5f, 0), new Vector2(0, 520), new Vector2(600, 160));

            // выбор уровня
            var lv = UIKit.Box(menu, "LevelBox", new Color(0, 0, 0, 0.5f));
            UIKit.At(lv, new Vector2(0.5f, 0), new Vector2(0, 372), new Vector2(600, 96));
            var prev = UIKit.MakeButton(lv, "<", new Vector2(80, 80), UIKit.Blue, () => gc.ChangeLevel(-1), 44);
            UIKit.At((RectTransform)prev.transform, new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(80, 80));
            var next = UIKit.MakeButton(lv, ">", new Vector2(80, 80), UIKit.Blue, () => gc.ChangeLevel(1), 44);
            UIKit.At((RectTransform)next.transform, new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(80, 80));
            menuLevel = UIKit.Label(lv, "Уровень 1: Закатный лес", 30, Color.white);
            menuLevel.rectTransform.offsetMin = new Vector2(96, 28); menuLevel.rectTransform.offsetMax = new Vector2(-96, -4);
            menuLevelSub = UIKit.Label(lv, "", 22, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal);
            menuLevelSub.rectTransform.offsetMin = new Vector2(96, 2); menuLevelSub.rectTransform.offsetMax = new Vector2(-96, -62);

            var skins = UIKit.MakeButton(menu, "СКИНЫ И КЕЙСЫ", new Vector2(600, 110), UIKit.Orange, () => gc.OpenShop(), 40);
            UIKit.At((RectTransform)skins.transform, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(600, 110));
            var set = UIKit.MakeButton(menu, "НАСТРОЙКИ", new Vector2(600, 100), UIKit.Blue, () => ShowSettings(true), 38);
            UIKit.At((RectTransform)set.transform, new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(600, 100));

            menuVersion = UIKit.Label(menu, "v" + GameConfig.Version, 24, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal, true);
            UIKit.At(menuVersion.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(400, 40));
        }

        public void RefreshMenu()
        {
            var d = SaveSystem.Data;
            menuBest.text = "РЕКОРД: " + d.bestScore;
            menuCoins.text = d.coins.ToString();
            var th = Themes.All[d.level];
            menuLevel.text = "Уровень " + (d.level + 1) + ": " + th.name;
            int next = d.level + 1;
            if (next < Themes.All.Length && next > d.unlockedLevel)
                menuLevelSub.text = "Следующий откроется на " + Themes.All[next].unlockDistance + " м (лучший: " + d.bestDistance + " м)";
            else menuLevelSub.text = d.level == Themes.All.Length - 1 ? "Все уровни открыты" : "Следующий уровень открыт";
        }

        public void ShowMenu()
        {
            HideAll();
            RefreshMenu();
            menu.gameObject.SetActive(true);
        }

        // ============================================================ HUD
        void BuildHud()
        {
            hud = UIKit.NewRect("HUD", safe); UIKit.Fill(hud);
            hudDist = UIKit.Label(hud, "0 м", 62, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            UIKit.At(hudDist.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(330, 90));

            var cp = UIKit.Box(hud, "CoinPill", new Color(0, 0, 0, 0.45f));
            UIKit.At(cp, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-130, -30), new Vector2(230, 70));
            var ci = UIKit.CoinIcon(cp, 42);
            UIKit.At(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(42, 42));
            hudCoins = UIKit.Label(cp, "0", 36, UIKit.Gold, TextAnchor.MiddleRight);
            hudCoins.rectTransform.offsetMin = new Vector2(60, 0); hudCoins.rectTransform.offsetMax = new Vector2(-18, 0);

            var pb = UIKit.MakeButton(hud, "II", new Vector2(88, 88), new Color(0.15f, 0.12f, 0.25f, 0.9f), () => gc.Pause(), 44);
            UIKit.At((RectTransform)pb.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -21), new Vector2(88, 88));

            countdown = UIKit.Label(hud, "", 220, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(countdown.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(500, 300));
        }

        public void ShowHud()
        {
            HideAll();
            lastDist = -1; lastCoins = -1;
            UpdateHud(0, 0);
            countdown.text = "";
            hud.gameObject.SetActive(true);
        }

        public void UpdateHud(int dist, int coins)
        {
            if (dist != lastDist) { lastDist = dist; hudDist.text = dist + " м"; }
            if (coins != lastCoins) { lastCoins = coins; hudCoins.text = coins.ToString(); }
        }

        public void SetCountdown(string s) { countdown.text = s; }

        // ============================================================ пауза
        void BuildPause()
        {
            pause = UIKit.Box(safe, "Pause", new Color(0, 0, 0, 0.72f), false);
            UIKit.Fill(pause);
            var card = UIKit.Box(pause, "Card", UIKit.Panel);
            UIKit.At(card, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(600, 620));
            var t = UIKit.Label(card, "ПАУЗА", 76, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(560, 110));
            var a = UIKit.MakeButton(card, "ПРОДОЛЖИТЬ", new Vector2(480, 110), UIKit.Green, () => gc.Resume(), 40);
            UIKit.At((RectTransform)a.transform, new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(480, 110));
            var b = UIKit.MakeButton(card, "ЗАНОВО", new Vector2(480, 100), UIKit.Orange, () => gc.Restart(), 38);
            UIKit.At((RectTransform)b.transform, new Vector2(0.5f, 1), new Vector2(0, -370), new Vector2(480, 100));
            var c = UIKit.MakeButton(card, "В МЕНЮ", new Vector2(480, 100), UIKit.Blue, () => gc.GoMenu(), 38);
            UIKit.At((RectTransform)c.transform, new Vector2(0.5f, 1), new Vector2(0, -495), new Vector2(480, 100));
        }

        public void ShowPause(bool on) { pause.gameObject.SetActive(on); }

        // ============================================================ итоги забега
        void BuildOver()
        {
            over = UIKit.Box(safe, "GameOver", new Color(0, 0, 0, 0.75f), false);
            UIKit.Fill(over);
            var card = UIKit.Box(over, "Card", UIKit.Panel);
            UIKit.At(card, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(630, 860));
            overTitle = UIKit.Label(card, "ЗАБЕГ ОКОНЧЕН", 56, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(overTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(590, 90));
            overNew = UIKit.Label(card, "Новый рекорд!", 50, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(overNew.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(590, 80));

            var sl = UIKit.Label(card, "РЕЗУЛЬТАТ", 30, new Color(1, 1, 1, 0.7f));
            UIKit.At(sl.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -225), new Vector2(500, 40));
            overScore = UIKit.Label(card, "0", 120, UIKit.Orange, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(overScore.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(560, 150));

            overDist = Row(card, "Расстояние", -440);
            overCoins = Row(card, "Собрано монет", -505);
            overBest = Row(card, "Лучший рекорд", -570);

            var again = UIKit.MakeButton(card, "ИГРАТЬ СНОВА", new Vector2(520, 110), UIKit.Green, () => gc.Restart(), 42);
            UIKit.At((RectTransform)again.transform, new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(520, 110));
            var menuBtn = UIKit.MakeButton(card, "В МЕНЮ", new Vector2(520, 100), UIKit.Blue, () => gc.GoMenu(), 38);
            UIKit.At((RectTransform)menuBtn.transform, new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(520, 100));
        }

        Text Row(Transform p, string label, float y)
        {
            var l = UIKit.Label(p, label, 32, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleLeft, FontStyle.Normal);
            UIKit.At(l.rectTransform, new Vector2(0.5f, 1), new Vector2(-30, y), new Vector2(420, 54));
            var v = UIKit.Label(p, "0", 38, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.At(v.rectTransform, new Vector2(0.5f, 1), new Vector2(200, y), new Vector2(210, 54));
            return v;
        }

        public void ShowGameOver(int score, int dist, int coins, int best, bool isNew)
        {
            HideAll();
            overScore.text = score.ToString();
            overDist.text = dist + " м";
            overCoins.text = coins.ToString();
            overBest.text = best.ToString();
            overNew.gameObject.SetActive(isNew);
            overTitle.text = "ЗАБЕГ ОКОНЧЕН";
            over.gameObject.SetActive(true);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            if (isNew) pulseRoutine = StartCoroutine(Pulse(overNew.rectTransform));
        }

        IEnumerator Pulse(RectTransform rt)
        {
            float t = 0;
            while (rt != null && rt.gameObject.activeInHierarchy)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin(t * 7f) * 0.08f;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        // ============================================================ настройки
        void BuildSettings()
        {
            settings = UIKit.Box(safe, "Settings", new Color32(20, 16, 36, 250), false);
            UIKit.Fill(settings);
            var t = UIKit.Label(settings, "НАСТРОЙКИ", 70, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(660, 110));

            float y = -220;
            SwitchRow(y, "Звуки", () => SaveSystem.Data.sfxOn, v => { SaveSystem.Data.sfxOn = v; }); y -= 100;
            SliderRow(y, "Громкость эффектов", SaveSystem.Data.sfxVolume, v => { SaveSystem.Data.sfxVolume = v; }, true); y -= 130;
            SwitchRow(y, "Музыка", () => SaveSystem.Data.musicOn, v => { SaveSystem.Data.musicOn = v; gc.ApplyAudio(); }); y -= 100;
            SliderRow(y, "Громкость музыки", SaveSystem.Data.musicVolume, v => { SaveSystem.Data.musicVolume = v; gc.ApplyAudio(); }, false); y -= 130;
            SwitchRow(y, "Вибрация", () => SaveSystem.Data.vibration, v => { SaveSystem.Data.vibration = v; if (v) AudioManager.Vibrate(); }); y -= 110;

            var ql = UIKit.Label(settings, "Качество графики", 32, Color.white, TextAnchor.MiddleLeft);
            UIKit.At(ql.rectTransform, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(640, 50));
            y -= 80;
            qButtons = new Button[3];
            string[] names = { "НИЗКОЕ", "СРЕДНЕЕ", "ВЫСОКОЕ" };
            for (int i = 0; i < 3; i++)
            {
                int q = i;
                var b = UIKit.MakeButton(settings, names[i], new Vector2(205, 84), UIKit.Blue, () => { gc.SetQuality(q); RefreshQuality(); }, 28);
                UIKit.At((RectTransform)b.transform, new Vector2(0.5f, 1), new Vector2(-215 + i * 215, y), new Vector2(205, 84));
                qButtons[i] = b;
            }
            y -= 100;

            var info = UIKit.Label(settings, "Версия " + GameConfig.Version + "\nБез рекламы · без интернета · открытый код (MIT)", 26,
                new Color(1, 1, 1, 0.65f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.At(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(660, 90));

            var back = UIKit.MakeButton(settings, "НАЗАД", new Vector2(420, 100), UIKit.Orange, () => ShowSettings(false), 42);
            UIKit.At((RectTransform)back.transform, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(420, 100));
        }

        void SwitchRow(float y, string label, System.Func<bool> get, System.Action<bool> set)
        {
            var l = UIKit.Label(settings, label, 34, Color.white, TextAnchor.MiddleLeft);
            UIKit.At(l.rectTransform, new Vector2(0.5f, 1), new Vector2(-110, y), new Vector2(420, 70));
            Button b = null;
            UnityEngine.Events.UnityAction apply = null;
            b = UIKit.MakeButton(settings, get() ? "ВКЛ" : "ВЫКЛ", new Vector2(190, 76), get() ? UIKit.Green : UIKit.Red, null, 32);
            UIKit.At((RectTransform)b.transform, new Vector2(0.5f, 1), new Vector2(240, y), new Vector2(190, 76));
            var txt = b.GetComponentInChildren<Text>();
            var img = b.GetComponent<Image>();
            apply = () =>
            {
                bool nv = !get();
                set(nv);
                txt.text = nv ? "ВКЛ" : "ВЫКЛ";
                img.color = nv ? UIKit.Green : UIKit.Red;
                SaveSystem.Commit();
            };
            b.onClick.AddListener(apply);
        }

        void SliderRow(float y, string label, float value, System.Action<float> set, bool sfxTick)
        {
            var l = UIKit.Label(settings, label, 30, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleLeft, FontStyle.Normal);
            UIKit.At(l.rectTransform, new Vector2(0.5f, 1), new Vector2(0, y + 10), new Vector2(640, 44));
            var s = UIKit.MakeSlider(settings, value, null, new Vector2(600, 56));
            UIKit.At((RectTransform)s.transform, new Vector2(0.5f, 1), new Vector2(0, y - 40), new Vector2(600, 56));
            float lastTick = 0;
            s.onValueChanged.AddListener(v =>
            {
                set(v);
                if (sfxTick && Time.unscaledTime - lastTick > 0.12f) { lastTick = Time.unscaledTime; AudioManager.I.Play(Sfx.Coin); }
            });
            var up = s.gameObject.AddComponent<SliderCommit>();
        }

        void RefreshQuality()
        {
            int q = SaveSystem.Data.quality;
            for (int i = 0; i < 3; i++)
                qButtons[i].GetComponent<Image>().color = (i == q) ? UIKit.Green : UIKit.PanelLight;
        }

        public void ShowSettings(bool on)
        {
            if (on)
            {
                RefreshQuality();
                settings.gameObject.SetActive(true);
            }
            else
            {
                SaveSystem.Commit();
                settings.gameObject.SetActive(false);
                RefreshMenu();
            }
        }

        // ============================================================ прочее
        public void ShowShop(bool on)
        {
            if (on) { HideAll(); shop.Open(); }
            else { shop.Hide(); ShowMenu(); }
        }

        public void ShowLoading(bool on) { loading.gameObject.SetActive(on); }

        public void Toast(string msg)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(msg));
        }

        IEnumerator ToastRoutine(string msg)
        {
            var rt = toast.transform.parent;
            toast.text = msg;
            rt.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(1.8f);
            rt.gameObject.SetActive(false);
        }
    }

    /// <summary>Сохраняет настройки, когда игрок отпускает ползунок.</summary>
    public class SliderCommit : MonoBehaviour, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData e) { SaveSystem.Commit(); }
    }
}
