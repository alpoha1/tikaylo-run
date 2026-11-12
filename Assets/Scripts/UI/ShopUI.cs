using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TilkayoRun
{
    /// <summary>Экран «Скины и кейсы»: сетка скинов с предпросмотром, магазин кейсов, анимация открытия.</summary>
    public class ShopUI
    {
        class Card
        {
            public int id;
            public Image bg;
            public GameObject lockObj, equippedObj;
            public Outline outline;
        }

        UIManager ui;
        GameController gc;
        RectTransform root, skinsPage, casesPage, overlay;
        Text coinsLabel;
        Button tabSkins, tabCases;
        RawImage previewImg;
        Text pvName, pvInfo;
        Button pvAction;
        Text pvActionText;
        readonly List<Card> cards = new List<Card>();
        int previewId;
        bool busy;
        Coroutine prewarm;

        // оверлей открытия кейса
        RectTransform strip, window, resultPanel, caseTitleRt;
        Image flash;
        RawImage resIcon;
        Text resName, resRarity, resNote;
        Button resEquip;
        Text caseTitle;
        SkinDef lastWin;
        readonly List<RectTransform> confetti = new List<RectTransform>();

        public void Build(Transform parent, UIManager uiManager, GameController controller)
        {
            ui = uiManager; gc = controller;
            root = UIKit.Box(parent, "Shop", new Color32(20, 16, 36, 252), false);
            UIKit.Fill(root);

            var back = UIKit.MakeButton(root, "< НАЗАД", new Vector2(210, 84), UIKit.Orange, () => Close(), 32);
            UIKit.At((RectTransform)back.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(210, 84));

            var cp = UIKit.Box(root, "CoinPill", new Color(0, 0, 0, 0.5f));
            UIKit.At(cp, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -22), new Vector2(280, 70));
            var ci = UIKit.CoinIcon(cp, 44);
            UIKit.At(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 44));
            coinsLabel = UIKit.Label(cp, "0", 34, UIKit.Gold, TextAnchor.MiddleRight);
            coinsLabel.rectTransform.offsetMin = new Vector2(70, 0); coinsLabel.rectTransform.offsetMax = new Vector2(-20, 0);

            tabSkins = UIKit.MakeButton(root, "СКИНЫ", new Vector2(330, 80), UIKit.Green, () => ShowTab(true), 34);
            UIKit.At((RectTransform)tabSkins.transform, new Vector2(0.5f, 1), new Vector2(-175, -150), new Vector2(330, 80));
            tabCases = UIKit.MakeButton(root, "КЕЙСЫ", new Vector2(330, 80), UIKit.PanelLight, () => ShowTab(false), 34);
            UIKit.At((RectTransform)tabCases.transform, new Vector2(0.5f, 1), new Vector2(175, -150), new Vector2(330, 80));

            BuildSkinsPage();
            BuildCasesPage();
            BuildOverlay();
            root.gameObject.SetActive(false);
        }

        // ============================================================ скины
        void BuildSkinsPage()
        {
            skinsPage = UIKit.NewRect("SkinsPage", root);
            UIKit.Fill(skinsPage);

            var pvBox = UIKit.Box(skinsPage, "PreviewBox", new Color(0, 0, 0, 0.35f));
            UIKit.At(pvBox, new Vector2(0.5f, 1), new Vector2(0, -205), new Vector2(340, 340));
            var pvRt = UIKit.NewRect("Preview", pvBox);
            UIKit.Fill(pvRt);
            pvRt.offsetMin = new Vector2(6, 6); pvRt.offsetMax = new Vector2(-6, -6);
            previewImg = pvRt.gameObject.AddComponent<RawImage>();
            previewImg.raycastTarget = false;

            pvName = UIKit.Label(skinsPage, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(pvName.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -574), new Vector2(680, 56));
            pvInfo = UIKit.Label(skinsPage, "", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.At(pvInfo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -626), new Vector2(680, 44));

            pvAction = UIKit.MakeButton(skinsPage, "ВЫБРАТЬ", new Vector2(520, 84), UIKit.Green, () => EquipPreview(), 34);
            UIKit.At((RectTransform)pvAction.transform, new Vector2(0.5f, 1), new Vector2(0, -706), new Vector2(520, 84));
            pvActionText = pvAction.GetComponentInChildren<Text>();

            RectTransform content;
            var sr = UIKit.MakeScroll(skinsPage, out content);
            var srt = (RectTransform)sr.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(16, 10); srt.offsetMax = new Vector2(-16, -760);

            const int cols = 3;
            float width = 688f, gap = 12f;
            float cw = (width - gap * (cols - 1)) / cols;
            float ch = 262f;
            int total = SkinDatabase.All.Count;
            int rows = (total + cols - 1) / cols;
            content.sizeDelta = new Vector2(0, rows * (ch + gap) + 10);

            for (int i = 0; i < total; i++)
            {
                var s = SkinDatabase.All[i];
                int col = i % cols, row = i / cols;
                var card = new Card { id = s.id };
                var rt = UIKit.Box(content, "Skin" + s.id, UIKit.PanelLight);
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(col * (cw + gap), -row * (ch + gap));
                rt.sizeDelta = new Vector2(cw, ch);
                card.bg = rt.GetComponent<Image>();
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = card.bg;
                int id = s.id;
                btn.onClick.AddListener(() => { AudioManager.I.Play(Sfx.Click); SelectPreview(id); });
                card.outline = rt.gameObject.AddComponent<Outline>();
                card.outline.effectColor = Color.white; card.outline.effectDistance = new Vector2(4, -4);
                card.outline.enabled = false;

                var icon = UIKit.NewRect("Icon", rt);
                UIKit.At(icon, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(cw - 16, cw - 16));
                var ri = icon.gameObject.AddComponent<RawImage>();
                ri.raycastTarget = false;
                ri.texture = null;
                ri.color = new Color(1, 1, 1, 0);
                IconSlots[s.id] = ri;

                var nm = UIKit.Label(rt, s.name, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = 22;
                UIKit.At(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(cw - 10, 44));

                var strip = UIKit.NewRect("Rarity", rt);
                strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(1, 0);
                strip.offsetMin = new Vector2(14, 5); strip.offsetMax = new Vector2(-14, 11);
                var si = strip.gameObject.AddComponent<Image>();
                si.color = RarityInfo.Tint(s.rarity); si.raycastTarget = false;

                var lk = UIKit.Box(rt, "Lock", new Color(0, 0, 0, 0.55f));
                UIKit.At(lk, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(cw - 16, cw - 16));
                lk.GetComponent<Image>().raycastTarget = false;
                var lt = UIKit.Label(lk, "НЕ КУПЛЕН", 24, new Color(1, 1, 1, 0.85f));
                card.lockObj = lk.gameObject;

                var eq = UIKit.Box(rt, "Equipped", UIKit.Green);
                UIKit.At(eq, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -14), new Vector2(104, 34));
                eq.GetComponent<Image>().raycastTarget = false;
                UIKit.Label(eq, "ВЫБРАН", 20, Color.white);
                card.equippedObj = eq.gameObject;

                cards.Add(card);
            }
        }

        readonly Dictionary<int, RawImage> IconSlots = new Dictionary<int, RawImage>();

        IEnumerator PrewarmIcons()
        {
            for (int i = 0; i < SkinDatabase.All.Count; i++)
            {
                var s = SkinDatabase.All[i];
                var tex = gc.preview.GetIcon(s);
                RawImage ri;
                if (IconSlots.TryGetValue(s.id, out ri)) { ri.texture = tex; ri.color = Color.white; }
                if (i % 3 == 2) yield return null;
            }
            // GetIcon мог поменять скин в студии — вернуть выбранный предпросмотр
            gc.preview.ShowLive(SkinDatabase.Get(previewId));
            prewarm = null;
        }

        void SelectPreview(int id)
        {
            previewId = id;
            var s = SkinDatabase.Get(id);
            gc.preview.ShowLive(s);
            previewImg.texture = gc.preview.LiveTexture;
            bool owned = SaveSystem.Owns(id);
            bool equipped = SaveSystem.Data.selectedSkin == id;
            pvName.text = s.name;
            Color rc = RarityInfo.Tint(s.rarity);
            string hex = ColorUtility.ToHtmlStringRGB(rc);
            pvInfo.text = "<color=#" + hex + ">" + RarityInfo.Name(s.rarity) + "</color>  ·  " + (owned ? (equipped ? "выбран" : "куплен") : "не куплен");
            var img = pvAction.GetComponent<Image>();
            if (owned && !equipped) { pvActionText.text = "ВЫБРАТЬ"; pvAction.interactable = true; img.color = UIKit.Green; }
            else if (owned) { pvActionText.text = "ВЫБРАН"; pvAction.interactable = false; img.color = UIKit.PanelLight; }
            else { pvActionText.text = "ДОСТАЁТСЯ ИЗ КЕЙСОВ"; pvAction.interactable = false; img.color = UIKit.PanelLight; }
            RefreshCards();
        }

        void RefreshCards()
        {
            int sel = SaveSystem.Data.selectedSkin;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                bool owned = SaveSystem.Owns(c.id);
                c.lockObj.SetActive(!owned);
                c.equippedObj.SetActive(c.id == sel);
                c.outline.enabled = c.id == previewId;
                c.bg.color = c.id == previewId ? new Color(0.33f, 0.29f, 0.49f, 1f) : UIKit.PanelLight;
            }
        }

        void EquipPreview()
        {
            if (!SaveSystem.Owns(previewId)) return;
            gc.EquipSkin(previewId);
            SelectPreview(previewId);
        }

        // ============================================================ кейсы
        void BuildCasesPage()
        {
            casesPage = UIKit.NewRect("CasesPage", root);
            UIKit.Fill(casesPage);
            RectTransform content;
            var sr = UIKit.MakeScroll(casesPage, out content);
            var srt = (RectTransform)sr.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(16, 10); srt.offsetMax = new Vector2(-16, -215);

            float ch = 380f, gap = 20f;
            content.sizeDelta = new Vector2(0, SkinDatabase.Cases.Length * (ch + gap) + 10);
            for (int i = 0; i < SkinDatabase.Cases.Length; i++)
            {
                var cd = SkinDatabase.Cases[i];
                int idx = i;
                var card = UIKit.Box(content, "Case" + i, UIManager.Dark(cd.color, 0.55f));
                card.anchorMin = new Vector2(0, 1); card.anchorMax = new Vector2(1, 1); card.pivot = new Vector2(0.5f, 1);
                card.anchoredPosition = new Vector2(0, -i * (ch + gap));
                card.sizeDelta = new Vector2(0, ch);
                var band = UIKit.Box(card, "Band", cd.color);
                band.anchorMin = new Vector2(0, 1); band.anchorMax = new Vector2(1, 1); band.pivot = new Vector2(0.5f, 1);
                band.anchoredPosition = Vector2.zero; band.sizeDelta = new Vector2(0, 8);

                var nm = UIKit.Label(card, cd.name, 40, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
                UIKit.At(nm.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(340, 60));
                var sub = UIKit.Label(card, cd.subtitle, 24, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft, FontStyle.Normal);
                UIKit.At(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -84), new Vector2(340, 40));

                var price = UIKit.Box(card, "Price", new Color(0, 0, 0, 0.4f));
                UIKit.At(price, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -140), new Vector2(240, 64));
                var pic = UIKit.CoinIcon(price, 40);
                UIKit.At(pic.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(40, 40));
                var pt = UIKit.Label(price, cd.price.ToString(), 36, UIKit.Gold, TextAnchor.MiddleRight);
                pt.rectTransform.offsetMin = new Vector2(60, 0); pt.rectTransform.offsetMax = new Vector2(-16, 0);

                var open = UIKit.MakeButton(card, "ОТКРЫТЬ", new Vector2(300, 90), UIKit.Green, () => OpenCase(idx), 38);
                UIKit.At((RectTransform)open.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 24), new Vector2(300, 90));

                var head = UIKit.Label(card, "ШАНСЫ", 24, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.At(head.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -28), new Vector2(270, 36));
                for (int r = 0; r < 4; r++)
                {
                    var rar = (Rarity)r;
                    float ch2 = cd.chance[r];
                    string line = RarityInfo.Name(rar) + "  " + FormatPct(ch2);
                    var l = UIKit.Label(card, line, 28, ch2 > 0 ? RarityInfo.Tint(rar) : new Color(1, 1, 1, 0.35f), TextAnchor.MiddleLeft, FontStyle.Bold);
                    l.horizontalOverflow = HorizontalWrapMode.Overflow;
                    UIKit.At(l.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -74 - r * 52), new Vector2(270, 46));
                }
            }
        }

        static string FormatPct(float v) { return (v % 1f == 0f ? ((int)v).ToString() : v.ToString("0.#")) + "%"; }

        void ShowTab(bool skins)
        {
            skinsPage.gameObject.SetActive(skins);
            casesPage.gameObject.SetActive(!skins);
            tabSkins.GetComponent<Image>().color = skins ? UIKit.Green : UIKit.PanelLight;
            tabCases.GetComponent<Image>().color = skins ? UIKit.PanelLight : UIKit.Green;
            if (!skins) gc.preview.StopLive();
            else SelectPreview(previewId);
        }

        // ============================================================ открытие / закрытие
        public void Open()
        {
            root.gameObject.SetActive(true);
            overlay.gameObject.SetActive(false);
            previewId = SaveSystem.Data.selectedSkin;
            RefreshCoins();
            ShowTab(true);
            if (prewarm != null) ui.StopCoroutine(prewarm);
            prewarm = ui.StartCoroutine(PrewarmIcons());
        }

        void Close()
        {
            if (busy) return;
            gc.CloseShop();
        }

        public void Hide()
        {
            if (root == null) return;
            root.gameObject.SetActive(false);
            if (gc != null && gc.preview != null) gc.preview.StopLive();
        }

        void RefreshCoins() { coinsLabel.text = SaveSystem.Data.coins.ToString(); }

        // ============================================================ оверлей кейса
        void BuildOverlay()
        {
            overlay = UIKit.Box(root, "CaseOverlay", new Color(0.03f, 0.02f, 0.08f, 0.96f), false);
            UIKit.Fill(overlay);

            caseTitle = UIKit.Label(overlay, "", 56, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(caseTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(680, 90));

            var wbox = UIKit.Box(overlay, "Window", new Color(0, 0, 0, 0.55f));
            UIKit.At(wbox, new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(700, 270));
            var wmask = UIKit.NewRect("Mask", wbox);
            UIKit.Fill(wmask);
            wmask.offsetMin = new Vector2(8, 8); wmask.offsetMax = new Vector2(-8, -8);
            wmask.gameObject.AddComponent<RectMask2D>();
            window = wmask;
            strip = UIKit.NewRect("Strip", wmask);
            strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(0, 1); strip.pivot = new Vector2(0, 0.5f);
            strip.sizeDelta = new Vector2(100, 0); strip.anchoredPosition = Vector2.zero;

            var marker = UIKit.NewRect("Marker", wbox);
            marker.anchorMin = new Vector2(0.5f, 0); marker.anchorMax = new Vector2(0.5f, 1);
            marker.sizeDelta = new Vector2(6, 20);
            var mi = marker.gameObject.AddComponent<Image>();
            mi.color = UIKit.Gold; mi.raycastTarget = false;
            var tri = UIKit.NewRect("Tri", wbox);
            UIKit.At(tri, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(34, 34));
            tri.localRotation = Quaternion.Euler(0, 0, 45);
            var ti = tri.gameObject.AddComponent<Image>();
            ti.color = UIKit.Gold; ti.raycastTarget = false;

            // результат
            resultPanel = UIKit.NewRect("Result", overlay);
            UIKit.Fill(resultPanel);
            var rbg = UIKit.Box(resultPanel, "Bg", new Color(0.03f, 0.02f, 0.08f, 0.9f), false);
            UIKit.Fill(rbg);
            var ric = UIKit.Box(resultPanel, "IconBox", new Color(1, 1, 1, 0.08f));
            UIKit.At(ric, new Vector2(0.5f, 1), new Vector2(0, -440), new Vector2(420, 420));
            var ir = UIKit.NewRect("Icon", ric);
            UIKit.Fill(ir); ir.offsetMin = new Vector2(10, 10); ir.offsetMax = new Vector2(-10, -10);
            resIcon = ir.gameObject.AddComponent<RawImage>();
            resName = UIKit.Label(resultPanel, "", 52, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(resName.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -700), new Vector2(680, 80));
            resRarity = UIKit.Label(resultPanel, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(resRarity.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -775), new Vector2(680, 60));
            resNote = UIKit.Label(resultPanel, "", 34, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.At(resNote.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -850), new Vector2(680, 60));
            resEquip = UIKit.MakeButton(resultPanel, "НАДЕТЬ", new Vector2(420, 100), UIKit.Green, () => { if (lastWin != null) gc.EquipSkin(lastWin.id); CloseResult(); }, 40);
            UIKit.At((RectTransform)resEquip.transform, new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(420, 100));
            var take = UIKit.MakeButton(resultPanel, "ЗАБРАТЬ", new Vector2(420, 100), UIKit.Orange, () => CloseResult(), 40);
            UIKit.At((RectTransform)take.transform, new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(420, 100));

            var fl = UIKit.NewRect("Flash", overlay);
            UIKit.Fill(fl);
            flash = fl.gameObject.AddComponent<Image>();
            flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
            overlay.gameObject.SetActive(false);
        }

        void CloseResult()
        {
            overlay.gameObject.SetActive(false);
            for (int i = 0; i < confetti.Count; i++) if (confetti[i] != null) UnityEngine.Object.Destroy(confetti[i].gameObject);
            confetti.Clear();
            busy = false;
            RefreshCoins();
            if (lastWin != null) previewId = lastWin.id;
            ShowTab(true);
        }

        void OpenCase(int idx)
        {
            if (busy) return;
            var def = SkinDatabase.Cases[idx];
            if (SaveSystem.Data.coins < def.price)
            {
                ui.Toast("Не хватает монет: нужно " + def.price);
                return;
            }
            ui.StartCoroutine(CaseRoutine(def));
        }

        IEnumerator CaseRoutine(CaseDef def)
        {
            busy = true;
            SaveSystem.AddCoins(-def.price);
            var rng = new System.Random(Guid.NewGuid().GetHashCode());
            var win = SkinDatabase.RollSkin(def, rng);
            bool dup = SaveSystem.Owns(win.id);
            int refund = 0;
            if (dup) { refund = RarityInfo.DuplicateRefund(win.rarity); SaveSystem.AddCoins(refund); }
            else SaveSystem.Data.ownedSkins.Add(win.id);
            SaveSystem.Commit();
            RefreshCoins();
            lastWin = win;
            gc.preview.StopLive();

            overlay.gameObject.SetActive(true);
            resultPanel.gameObject.SetActive(false);
            flash.color = new Color(1, 1, 1, 0);
            caseTitle.text = def.name;

            for (int i = strip.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(strip.GetChild(i).gameObject);

            const int N = 46, winIdx = 38;
            const float cardW = 190f, step = 200f;
            RectTransform winCard = null;
            for (int i = 0; i < N; i++)
            {
                var s = i == winIdx ? win : SkinDatabase.RollSkin(def, rng);
                Color rc = RarityInfo.Tint(s.rarity);
                var c = UIKit.Box(strip, "C" + i, Color.Lerp(new Color(0.1f, 0.09f, 0.18f), rc, 0.35f));
                c.anchorMin = new Vector2(0, 0.5f); c.anchorMax = new Vector2(0, 0.5f); c.pivot = new Vector2(0, 0.5f);
                c.anchoredPosition = new Vector2(i * step + 5f, 0); c.sizeDelta = new Vector2(cardW, 240f);
                var ic = UIKit.NewRect("Icon", c);
                UIKit.At(ic, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(170, 170));
                var ri = ic.gameObject.AddComponent<RawImage>();
                ri.texture = gc.preview.GetIcon(s); ri.raycastTarget = false;
                var nm = UIKit.Label(c, s.name, 20, Color.white);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = 20;
                UIKit.At(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(cardW - 8, 44));
                var bar = UIKit.NewRect("Bar", c);
                bar.anchorMin = new Vector2(0, 0); bar.anchorMax = new Vector2(1, 0);
                bar.offsetMin = new Vector2(12, 6); bar.offsetMax = new Vector2(-12, 12);
                var bi = bar.gameObject.AddComponent<Image>(); bi.color = rc; bi.raycastTarget = false;
                if (i == winIdx) winCard = c;
            }
            strip.sizeDelta = new Vector2(N * step + 10f, 0);

            yield return null;
            float windowW = window.rect.width;
            float target = -(winIdx * step + 5f + cardW * 0.5f - windowW * 0.5f + UnityEngine.Random.Range(-55f, 55f));
            strip.anchoredPosition = new Vector2(0, 0);
            float dur = 5.2f, t = 0f;
            int lastIdx = -1;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = 1f - Mathf.Pow(1f - k, 4f);
                float x = Mathf.Lerp(0f, target, e);
                strip.anchoredPosition = new Vector2(x, 0);
                int idxUnder = Mathf.FloorToInt((-x + windowW * 0.5f) / step);
                if (idxUnder != lastIdx)
                {
                    lastIdx = idxUnder;
                    AudioManager.I.Play(Sfx.Tick, 0.9f + 0.2f * k, 0.8f);
                }
                yield return null;
            }
            strip.anchoredPosition = new Vector2(target, 0);

            // вспышка и результат
            AudioManager.I.Play(Sfx.CaseOpen);
            if (win.rarity >= Rarity.Rare) AudioManager.I.Play(Sfx.NewRecord, 1f, 0.7f);
            if (win.rarity == Rarity.Legendary) AudioManager.Vibrate();
            if (winCard != null) winCard.localScale = new Vector3(1.1f, 1.1f, 1f);
            float ft = 0f;
            while (ft < 0.5f)
            {
                ft += Time.unscaledDeltaTime;
                flash.color = new Color(1, 1, 1, Mathf.Sin(Mathf.Clamp01(ft / 0.5f) * Mathf.PI) * 0.85f);
                yield return null;
            }
            flash.color = new Color(1, 1, 1, 0);

            resIcon.texture = gc.preview.GetIcon(win);
            resName.text = win.name;
            Color rarityColor = RarityInfo.Tint(win.rarity);
            resRarity.text = RarityInfo.Name(win.rarity);
            resRarity.color = rarityColor;
            if (dup) { resNote.text = "Уже есть! Возврат: +" + refund + " монет"; resEquip.gameObject.SetActive(false); }
            else { resNote.text = "НОВЫЙ СКИН!"; resEquip.gameObject.SetActive(true); }
            resultPanel.gameObject.SetActive(true);
            RefreshCoins();

            // конфетти
            var rt0 = resultPanel;
            var vel = new List<Vector2>();
            int cnt = 40;
            for (int i = 0; i < cnt; i++)
            {
                var p = UIKit.NewRect("Conf", resultPanel);
                p.anchorMin = p.anchorMax = new Vector2(0.5f, 1); p.pivot = new Vector2(0.5f, 0.5f);
                p.anchoredPosition = new Vector2(0, -440);
                p.sizeDelta = new Vector2(UnityEngine.Random.Range(10f, 22f), UnityEngine.Random.Range(10f, 22f));
                var pi = p.gameObject.AddComponent<Image>();
                pi.color = Color.Lerp(rarityColor, Color.white, UnityEngine.Random.value * 0.6f); pi.raycastTarget = false;
                confetti.Add(p);
                vel.Add(new Vector2(UnityEngine.Random.Range(-700f, 700f), UnityEngine.Random.Range(300f, 1100f)));
            }
            float life = 0f;
            while (life < 1.6f && resultPanel.gameObject.activeInHierarchy)
            {
                float dt = Time.unscaledDeltaTime;
                life += dt;
                for (int i = 0; i < confetti.Count; i++)
                {
                    if (confetti[i] == null) continue;
                    var v = vel[i];
                    v.y -= 2200f * dt;
                    vel[i] = v;
                    confetti[i].anchoredPosition += v * dt;
                    confetti[i].localRotation = Quaternion.Euler(0, 0, life * 360f * (i % 2 == 0 ? 1 : -1));
                }
                yield return null;
            }
            for (int i = 0; i < confetti.Count; i++) if (confetti[i] != null) UnityEngine.Object.Destroy(confetti[i].gameObject);
            confetti.Clear();
        }
    }
}
