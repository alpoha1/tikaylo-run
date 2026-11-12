using System.Collections.Generic;
using UnityEngine;

namespace TilkayoRun
{
    /// <summary>
    /// Отдельная "студия" далеко от трассы: рендерит скин в RenderTexture для живого предпросмотра
    /// и в маленькие текстуры-иконки (кэшируются).
    /// </summary>
    public class SkinPreview : MonoBehaviour
    {
        public const int IconSize = 160;

        Camera cam;
        SkinVisual visual;
        RenderTexture liveRT;
        readonly Dictionary<int, Texture2D> icons = new Dictionary<int, Texture2D>();
        bool live;
        float spin;

        public RenderTexture LiveTexture { get { return liveRT; } }

        public static SkinPreview Create()
        {
            var go = new GameObject("SkinPreviewStudio");
            go.transform.position = new Vector3(2000f, -500f, 2000f);
            var sp = go.AddComponent<SkinPreview>();
            sp.Init();
            return sp;
        }

        void Init()
        {
            visual = SkinVisual.Create(transform, true);
            visual.root.localPosition = new Vector3(-0.42f, 0.06f, 0f);

            // каменная площадка
            var b = new MeshBuilder(5);
            b.Frustum(new Vector3(0, -0.2f, 0), 1.25f, 1.15f, 0.25f, 16, new Color(0.35f, 0.33f, 0.36f), new Color(0.52f, 0.5f, 0.54f), 0, 0, true);
            var pf = new GameObject("Platform");
            pf.transform.SetParent(transform, false);
            pf.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("platform");
            pf.AddComponent<MeshRenderer>().sharedMaterial = Fx.Glow;

            var cg = new GameObject("Cam");
            cg.transform.SetParent(transform, false);
            cam = cg.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.2f, 1f);
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 30f;
            cam.allowMSAA = false;
            cam.transform.localPosition = new Vector3(0f, 1.15f, -5.0f);
            cam.transform.LookAt(transform.position + new Vector3(0, 1.0f, 0));

            liveRT = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            liveRT.name = "SkinPreviewRT";
        }

        static Color Background(Rarity r)
        {
            Color c = RarityInfo.Tint(r);
            return Color.Lerp(new Color(0.08f, 0.07f, 0.14f), c, 0.32f);
        }

        public void ShowLive(SkinDef s)
        {
            visual.SetSkin(s);
            visual.SetAuraActive(true);
            cam.backgroundColor = Background(s.rarity);
            live = true;
        }

        public void StopLive()
        {
            live = false;
            visual.SetAuraActive(false);
        }

        void LateUpdate()
        {
            if (!live) return;
            spin += Time.unscaledDeltaTime;
            visual.root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(spin * 1.8f) * 3f);
            float s = 1f + Mathf.Sin(spin * 3f) * 0.02f;
            visual.root.localScale = new Vector3(1f / s, s, 1f);
            visual.Tick(Time.unscaledDeltaTime, 0f);
            cam.targetTexture = liveRT;
            cam.Render();
        }

        public Texture2D GetIcon(SkinDef s)
        {
            Texture2D t;
            if (icons.TryGetValue(s.id, out t) && t != null) return t;

            bool wasLive = live;
            live = false;
            visual.SetSkin(s);
            visual.SetAuraActive(false);
            visual.FreezeRainbow(140f);
            visual.root.localRotation = Quaternion.identity;
            visual.root.localScale = Vector3.one;
            cam.backgroundColor = Background(s.rarity);

            var rt = RenderTexture.GetTemporary(IconSize, IconSize, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            t = new Texture2D(IconSize, IconSize, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            t.Apply(false);
            t.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            icons[s.id] = t;

            if (wasLive) { live = true; }
            return t;
        }
    }
}
