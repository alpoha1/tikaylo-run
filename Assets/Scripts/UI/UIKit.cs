using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TilkayoRun
{
    /// <summary>Небольшой набор помощников для построения uGUI из кода (без префабов).</summary>
    public static class UIKit
    {
        public static Font Font;
        public static Sprite Round, Circle;

        public static readonly Color Orange = new Color32(255, 168, 38, 255);
        public static readonly Color Green = new Color32(64, 190, 90, 255);
        public static readonly Color Blue = new Color32(70, 120, 220, 255);
        public static readonly Color Red = new Color32(220, 70, 70, 255);
        public static readonly Color Panel = new Color32(28, 24, 44, 235);
        public static readonly Color PanelLight = new Color32(52, 46, 78, 255);
        public static readonly Color Gold = new Color32(255, 205, 60, 255);

        public static void Init()
        {
            // Если положишь свой шрифт в Assets/Resources/Fonts/main.ttf — он будет использован.
            Font = Resources.Load<Font>("Fonts/main");
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Font == null) Font = Font.CreateDynamicFontFromOSFont(new[] { "Roboto", "Arial", "Noto Sans" }, 32);
            Round = MakeRound(64, 22);
            Circle = MakeRound(64, 32);
        }

        static Sprite MakeRound(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float d = outside + inside - radius;
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
                }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        // ------------------------------------------------------------ раскладка
        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Привязка к точке anchor; pivot = pivot; pos — смещение от якоря.</summary>
        public static void At(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static void At(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            At(rt, anchor, anchor, pos, size);
        }

        public static RectTransform Box(Transform parent, string name, Color color, bool round = true)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (round) { img.sprite = Round; img.type = Image.Type.Sliced; }
            return rt;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold, bool outline = false)
        {
            var rt = NewRect("Text", parent);
            Fill(rt);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0, 0, 0, 0.7f);
                o.effectDistance = new Vector2(3, -3);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string label, Vector2 size, Color color, UnityAction onClick, int fontSize = 36)
        {
            var rt = NewRect(label, parent);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Round; img.type = Image.Type.Sliced; img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.35f);
            sh.effectDistance = new Vector2(0, -6);
            rt.gameObject.AddComponent<PressScale>();
            btn.onClick.AddListener(() => { if (AudioManager.I != null) AudioManager.I.Play(Sfx.Click); });
            if (onClick != null) btn.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(label))
            {
                var t = Label(rt, label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 12;
                t.resizeTextMaxSize = fontSize;
                var trt = t.rectTransform;
                trt.offsetMin = new Vector2(10, 4); trt.offsetMax = new Vector2(-10, -4);
            }
            return btn;
        }

        public static Image CoinIcon(Transform parent, float size)
        {
            var rt = NewRect("CoinIcon", parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Circle; img.color = new Color32(255, 196, 30, 255);
            var inner = NewRect("Inner", rt);
            inner.anchorMin = new Vector2(0.2f, 0.2f); inner.anchorMax = new Vector2(0.8f, 0.8f);
            inner.offsetMin = inner.offsetMax = Vector2.zero;
            var ii = inner.gameObject.AddComponent<Image>();
            ii.sprite = Circle; ii.color = new Color32(255, 150, 20, 255);
            ii.raycastTarget = false; img.raycastTarget = false;
            return img;
        }

        public static Slider MakeSlider(Transform parent, float value, UnityAction<float> onChange, Vector2 size)
        {
            var root = NewRect("Slider", parent);
            root.sizeDelta = size;
            var bg = NewRect("Background", root);
            bg.anchorMin = new Vector2(0, 0.3f); bg.anchorMax = new Vector2(1, 0.7f);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            var bgi = bg.gameObject.AddComponent<Image>();
            bgi.sprite = Round; bgi.type = Image.Type.Sliced; bgi.color = new Color32(20, 18, 32, 255);

            var fillArea = NewRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0, 0.3f); fillArea.anchorMax = new Vector2(1, 0.7f);
            fillArea.offsetMin = new Vector2(size.y * 0.5f, 0); fillArea.offsetMax = new Vector2(-size.y * 0.5f, 0);
            var fill = NewRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(-size.y * 0.5f, 0); fill.offsetMax = new Vector2(size.y * 0.5f, 0);
            var fi = fill.gameObject.AddComponent<Image>();
            fi.sprite = Round; fi.type = Image.Type.Sliced; fi.color = Orange;

            var slideArea = NewRect("Handle Slide Area", root);
            slideArea.anchorMin = Vector2.zero; slideArea.anchorMax = Vector2.one;
            slideArea.offsetMin = new Vector2(size.y * 0.5f, 0); slideArea.offsetMax = new Vector2(-size.y * 0.5f, 0);
            var handle = NewRect("Handle", slideArea);
            handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(0, 1);
            handle.sizeDelta = new Vector2(size.y, 0);
            var hi = handle.gameObject.AddComponent<Image>();
            hi.sprite = Circle; hi.color = Color.white;

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = hi;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f; s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
        }

        public static ScrollRect MakeScroll(Transform parent, out RectTransform content)
        {
            var rt = NewRect("Scroll", parent);
            var sr = rt.gameObject.AddComponent<ScrollRect>();
            var vp = NewRect("Viewport", rt);
            Fill(vp);
            var vi = vp.gameObject.AddComponent<Image>();
            vi.color = new Color(0, 0, 0, 0.01f);
            vp.gameObject.AddComponent<RectMask2D>();
            content = NewRect("Content", vp);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 100);
            sr.viewport = vp; sr.content = content;
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.inertia = true; sr.decelerationRate = 0.135f; sr.scrollSensitivity = 40f;
            return sr;
        }
    }

    /// <summary>Лёгкая анимация нажатия кнопки.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one;
        bool pressed;
        float cur = 1f;

        void Awake() { baseScale = transform.localScale; }
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnPointerExit(PointerEventData e) { pressed = false; }

        void OnDisable() { pressed = false; cur = 1f; transform.localScale = baseScale; }

        void Update()
        {
            float target = pressed ? 0.94f : 1f;
            if (Mathf.Abs(cur - target) < 0.001f) return;
            cur = Mathf.MoveTowards(cur, target, Time.unscaledDeltaTime * 3f);
            transform.localScale = baseScale * cur;
        }
    }
}
