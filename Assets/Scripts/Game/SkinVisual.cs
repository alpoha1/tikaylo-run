using UnityEngine;

namespace TilkayoRun
{
    /// <summary>
    /// 2D-спрайт Тилкайо в 3D-мире: квадрат с шейдером перекраски, контур-свечение,
    /// 3D-аксессуар и аура частиц. Используется и игроком, и окном предпросмотра.
    /// </summary>
    public class SkinVisual : MonoBehaviour
    {
        public const float Size = 1.7f;

        public Transform root;                 // его двигает/масштабирует PlayerController
        Transform glowQuad;
        Material spriteMat, glowMat;
        MeshFilter accFilter;
        MeshRenderer accRenderer;
        ParticleSystem aura;
        SkinDef skin;
        float rainbowT;
        float lastScroll = -999f;

        static Mesh quadMesh;
        static Texture2D spriteTex;

        public SkinDef Skin { get { return skin; } }

        public static SkinVisual Create(Transform parent, bool withAura)
        {
            var go = new GameObject("SkinVisual");
            go.transform.SetParent(parent, false);
            var sv = go.AddComponent<SkinVisual>();
            sv.Build(withAura);
            return sv;
        }

        static Texture2D LoadTexture()
        {
            if (spriteTex != null) return spriteTex;
            spriteTex = Resources.Load<Texture2D>("tilkayo");
            if (spriteTex == null)
            {
                // запасной вариант: рыжий круг с ушками, если PNG вдруг пропал
                int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x - n * 0.5f) / (n * 0.42f), dy = (y - n * 0.4f) / (n * 0.34f);
                        float d = dx * dx + dy * dy;
                        float ear = Mathf.Max(
                            1f - ((x - n * 0.32f) * (x - n * 0.32f) / 140f + (y - n * 0.85f) * (y - n * 0.85f) / 700f),
                            1f - ((x - n * 0.68f) * (x - n * 0.68f) / 140f + (y - n * 0.85f) * (y - n * 0.85f) / 700f));
                        t.SetPixel(x, y, (d < 1f || ear > 0f) ? new Color(0.85f, 0.55f, 0.2f, 1f) : new Color(0, 0, 0, 0));
                    }
                t.Apply();
                spriteTex = t;
            }
            return spriteTex;
        }

        static Mesh QuadMesh()
        {
            if (quadMesh != null) return quadMesh;
            float h = Size * 0.5f;
            quadMesh = new Mesh { name = "SpriteQuad" };
            quadMesh.vertices = new[]
            {
                new Vector3(-h, 0, 0), new Vector3(h, 0, 0), new Vector3(h, Size, 0), new Vector3(-h, Size, 0)
            };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quadMesh.RecalculateNormals();
            quadMesh.RecalculateBounds();
            return quadMesh;
        }

        void Build(bool withAura)
        {
            root = new GameObject("Root").transform;
            root.SetParent(transform, false);

            var tex = LoadTexture();

            // контур-свечение: тот же спрайт, залитый цветом, чуть крупнее и сзади
            var g = new GameObject("Glow");
            g.transform.SetParent(root, false);
            glowQuad = g.transform;
            g.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var gr = g.AddComponent<MeshRenderer>();
            glowMat = new Material(Fx.SpriteBase);
            glowMat.mainTexture = tex;
            glowMat.SetFloat("_Solid", 1f);
            gr.sharedMaterial = glowMat;
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gr.receiveShadows = false;
            float k = 1.16f;
            glowQuad.localScale = new Vector3(k, k, 1f);
            glowQuad.localPosition = new Vector3(0, -(k - 1f) * Size * 0.5f, 0.02f);

            var q = new GameObject("Sprite");
            q.transform.SetParent(root, false);
            q.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var sr = q.AddComponent<MeshRenderer>();
            spriteMat = new Material(Fx.SpriteBase);
            spriteMat.mainTexture = tex;
            sr.sharedMaterial = spriteMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;

            var a = new GameObject("Accessory");
            a.transform.SetParent(root, false);
            accFilter = a.AddComponent<MeshFilter>();
            accRenderer = a.AddComponent<MeshRenderer>();
            accRenderer.sharedMaterial = Fx.Glow;
            accRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            accRenderer.receiveShadows = false;

            if (withAura)
            {
                aura = Fx.MakeSystem("Aura", root, Fx.GlowParticle, 80, false);
                aura.transform.localPosition = new Vector3(0, 0.7f, 0);
            }
        }

        public void SetSkin(SkinDef s)
        {
            skin = s;
            spriteMat.SetFloat("_Hue", s.hue);
            spriteMat.SetFloat("_Sat", s.sat);
            spriteMat.SetFloat("_Bright", s.bright);
            spriteMat.SetColor("_Tint", s.tint);
            spriteMat.SetFloat("_Alpha", s.alpha);
            rainbowT = 0f;

            bool hasOutline = s.outline.a > 0.01f;
            glowQuad.gameObject.SetActive(hasOutline);
            if (hasOutline)
            {
                Color oc = s.outline;
                glowMat.SetColor("_SolidColor", new Color(oc.r, oc.g, oc.b, 1f));
                glowMat.SetColor("_Tint", new Color(1, 1, 1, 1f));
                glowMat.SetFloat("_Alpha", Mathf.Clamp01(oc.a) * s.alpha);
            }

            var mesh = Accessories.Get(s.accessory, s.accColor);
            accFilter.sharedMesh = mesh;
            accRenderer.enabled = mesh != null;

            if (aura != null) Fx.ConfigureAura(aura, s);
            lastScroll = -999f;
        }

        /// <summary>Вызывать каждый кадр: радужный оттенок и "убегание" частиц назад.</summary>
        public void Tick(float dt, float worldSpeed)
        {
            if (skin == null) return;
            if (skin.rainbow)
            {
                rainbowT += dt * 120f;
                spriteMat.SetFloat("_Hue", rainbowT);
                if (glowQuad.gameObject.activeSelf)
                {
                    Color c = Color.HSVToRGB(Mathf.Repeat(rainbowT / 360f + 0.5f, 1f), 0.7f, 1f);
                    glowMat.SetColor("_SolidColor", c);
                }
            }
            if (aura != null && Mathf.Abs(worldSpeed - lastScroll) > 0.4f)
            {
                lastScroll = worldSpeed;
                Fx.ScrollBack(aura, worldSpeed * 0.9f);
            }
        }

        /// <summary>Остановить rainbow на фиксированном оттенке (для иконок).</summary>
        public void FreezeRainbow(float hue)
        {
            if (skin != null && skin.rainbow) spriteMat.SetFloat("_Hue", hue);
        }

        public void SetAuraActive(bool on)
        {
            if (aura == null) return;
            var em = aura.emission;
            if (!on) { aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            else if (skin != null && skin.aura != AuraKind.None) { aura.Play(); }
        }
    }
}
