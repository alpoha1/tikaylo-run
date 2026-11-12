using System.Collections.Generic;
using UnityEngine;

namespace TilkayoRun
{
    /// <summary>
    /// 3D-аксессуары для спрайта Тилкайо. Координаты — в локальном пространстве квадрата спрайта
    /// (низ по центру, размер SkinVisual.Size). Опорные точки подобраны под вырезанный спрайт.
    /// </summary>
    public static class Accessories
    {
        // опорные точки на морде (в метрах спрайта при Size = 1.7)
        static readonly Vector3 Head = new Vector3(0.36f, 1.33f, -0.10f);
        static readonly Vector3 Eyes = new Vector3(0.465f, 0.915f, -0.07f);
        const float EyeHalf = 0.165f;
        static readonly Vector3 Neck = new Vector3(0.48f, 0.55f, -0.07f);
        const float HeadTilt = -7f;

        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static Mesh Get(Accessory a, Color c)
        {
            if (a == Accessory.None) return null;
            string key = a + "_" + ColorUtility.ToHtmlStringRGB(c);
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = Build(a, c);
            cache[key] = m;
            return m;
        }

        static void Ring(MeshBuilder b, Vector3 center, float r, int n, float thick, Color col)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 2f * i / n;
                b.PushTRS(center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), new Vector3(0, 0, a * Mathf.Rad2Deg), Vector3.one);
                b.Box(Vector3.zero, new Vector3(thick, thick * 2.2f, thick), col);
                b.Pop();
            }
        }

        static Mesh Build(Accessory a, Color c)
        {
            var b = new MeshBuilder(17 + (int)a);
            b.Shade = 0.03f;
            Color dark = MeshBuilder.Mul(c, 0.6f);
            Color light = MeshBuilder.Mul(c, 1.25f);
            switch (a)
            {
                case Accessory.Glasses:
                    Ring(b, Eyes + new Vector3(-EyeHalf, 0, 0), 0.115f, 12, 0.03f, c);
                    Ring(b, Eyes + new Vector3(EyeHalf, 0.0f, 0), 0.115f, 12, 0.03f, c);
                    b.Box(Eyes + new Vector3(0, 0.03f, 0), new Vector3(0.1f, 0.03f, 0.03f), c);
                    b.Box(Eyes + new Vector3(-EyeHalf - 0.13f, 0.02f, 0.05f), new Vector3(0.12f, 0.025f, 0.025f), c);
                    break;

                case Accessory.Sunglasses:
                    b.PushTRS(Eyes + new Vector3(-EyeHalf, 0, 0), new Vector3(90, 0, 0), new Vector3(1.0f, 1f, 0.85f));
                    b.Frustum(Vector3.zero, 0.125f, 0.125f, 0.03f, 10, c, c, 0, 0, true, true, MeshBuilder.Mul(c, 1.0f));
                    b.Pop();
                    b.PushTRS(Eyes + new Vector3(EyeHalf, 0, 0), new Vector3(90, 0, 0), new Vector3(1.0f, 1f, 0.85f));
                    b.Frustum(Vector3.zero, 0.125f, 0.125f, 0.03f, 10, c, c, 0, 0, true, true, MeshBuilder.Mul(c, 1.0f));
                    b.Pop();
                    b.Box(Eyes + new Vector3(0, 0.04f, 0), new Vector3(0.12f, 0.03f, 0.03f), dark);
                    b.Box(Eyes + new Vector3(-EyeHalf - 0.13f, 0.03f, 0.05f), new Vector3(0.12f, 0.025f, 0.025f), dark);
                    b.Box(Eyes + new Vector3(-EyeHalf - 0.05f, 0.05f, -0.03f), new Vector3(0.06f, 0.015f, 0.01f), new Color(0.8f, 0.9f, 1f));
                    break;

                case Accessory.PartyHat:
                    b.PushTRS(Head + new Vector3(0, -0.03f, 0), new Vector3(0, 0, HeadTilt), Vector3.one);
                    b.Frustum(Vector3.zero, 0.25f, 0.012f, 0.62f, 10, c, light);
                    b.Frustum(new Vector3(0, 0.18f, 0), 0.17f, 0.15f, 0.04f, 10, Color.white, Color.white);
                    b.Frustum(new Vector3(0, 0.34f, 0), 0.115f, 0.1f, 0.04f, 10, Color.white, Color.white);
                    b.Blob(new Vector3(0, 0.64f, 0), new Vector3(0.07f, 0.07f, 0.07f), 0.1f, Color.white);
                    b.Pop();
                    break;

                case Accessory.Bow:
                    b.PushTRS(Neck, Vector3.zero, Vector3.one);
                    b.Tri(new Vector3(0, 0, 0), new Vector3(-0.2f, 0.11f, 0), new Vector3(-0.2f, -0.11f, 0), c, light, light, 0, 0, 0);
                    b.Tri(new Vector3(0, 0, 0), new Vector3(0.2f, -0.11f, 0), new Vector3(0.2f, 0.11f, 0), c, light, light, 0, 0, 0);
                    b.Box(Vector3.zero, new Vector3(0.09f, 0.1f, 0.05f), dark);
                    b.Pop();
                    break;

                case Accessory.TopHat:
                    b.PushTRS(Head + new Vector3(0, -0.02f, 0), new Vector3(0, 0, HeadTilt), Vector3.one);
                    b.Frustum(Vector3.zero, 0.4f, 0.4f, 0.05f, 12, c, c, 0, 0, true, true);
                    b.Frustum(new Vector3(0, 0.05f, 0), 0.25f, 0.23f, 0.5f, 12, c, MeshBuilder.Mul(c, 1.35f), 0, 0, true);
                    b.Frustum(new Vector3(0, 0.05f, 0), 0.255f, 0.252f, 0.1f, 12, new Color(0.85f, 0.15f, 0.2f), new Color(0.85f, 0.15f, 0.2f));
                    b.Pop();
                    break;

                case Accessory.WizardHat:
                    b.PushTRS(Head + new Vector3(0, -0.03f, 0), new Vector3(0, 0, HeadTilt), Vector3.one);
                    b.Frustum(Vector3.zero, 0.5f, 0.5f, 0.04f, 14, dark, dark, 0, 0, true, true);
                    b.Frustum(new Vector3(0, 0.04f, 0), 0.32f, 0.12f, 0.5f, 12, c, c);
                    b.PushTRS(new Vector3(0, 0.54f, 0), new Vector3(0, 0, -22), Vector3.one);
                    b.Frustum(Vector3.zero, 0.12f, 0.01f, 0.42f, 10, c, light);
                    b.Pop();
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * 1.6f;
                        b.Blob(new Vector3(Mathf.Cos(ang) * 0.2f, 0.18f + i * 0.09f, -0.2f), new Vector3(0.035f, 0.035f, 0.02f), 0.2f, new Color(1f, 0.9f, 0.3f), 0, 0, 1.4f, 1f);
                    }
                    b.Pop();
                    break;

                case Accessory.Horns:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 root = s < 0 ? new Vector3(0.20f, 1.30f, -0.06f) : new Vector3(0.54f, 1.28f, -0.06f);
                        b.PushTRS(root, new Vector3(0, 0, -s * 24f), Vector3.one);
                        b.Frustum(Vector3.zero, 0.075f, 0.03f, 0.22f, 7, c, light);
                        b.PushTRS(new Vector3(0, 0.2f, 0), new Vector3(0, 0, -s * 22f), Vector3.one);
                        b.Frustum(Vector3.zero, 0.03f, 0.004f, 0.16f, 7, light, light);
                        b.Pop();
                        b.Pop();
                    }
                    break;

                case Accessory.Crown:
                    b.PushTRS(Head + new Vector3(0, 0.0f, 0), new Vector3(0, 0, HeadTilt), Vector3.one);
                    b.Frustum(Vector3.zero, 0.27f, 0.25f, 0.15f, 10, c, light, 0, 0, true);
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = Mathf.PI * (0.15f + 0.7f * i / 4f);
                        Vector3 p = new Vector3(Mathf.Cos(ang) * -0.23f + 0f, 0.14f, -0.0f);
                        p = new Vector3(Mathf.Lerp(-0.24f, 0.24f, i / 4f), 0.14f, -0.02f);
                        b.Frustum(p, 0.07f, 0.015f, 0.2f, 5, c, light);
                        Color jewel = (i % 2 == 0) ? new Color(0.9f, 0.1f, 0.2f) : new Color(0.2f, 0.5f, 1f);
                        b.Blob(p + new Vector3(0, 0.22f, 0), new Vector3(0.04f, 0.04f, 0.04f), 0.1f, jewel, 0, 0, 1.4f, 1f);
                    }
                    b.Pop();
                    break;

                case Accessory.Halo:
                    b.PushTRS(Head + new Vector3(0, 0.42f, 0.02f), new Vector3(0, 0, HeadTilt), new Vector3(1f, 0.32f, 1f));
                    Ring(b, Vector3.zero, 0.3f, 18, 0.05f, c);
                    b.Pop();
                    break;

                case Accessory.Headphones:
                    {
                        Vector3 ctr = new Vector3(0.41f, 0.86f, -0.08f);
                        for (int i = 0; i <= 10; i++)
                        {
                            float ang = Mathf.PI * i / 10f;
                            Vector3 p = ctr + new Vector3(Mathf.Cos(ang) * 0.33f, Mathf.Sin(ang) * 0.33f, 0);
                            b.PushTRS(p, new Vector3(0, 0, ang * Mathf.Rad2Deg), Vector3.one);
                            b.Box(Vector3.zero, new Vector3(0.04f, 0.09f, 0.05f), new Color(0.12f, 0.12f, 0.16f));
                            b.Pop();
                        }
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 p = ctr + new Vector3(s * 0.33f, -0.02f, 0f);
                            b.PushTRS(p, new Vector3(90, 0, 0), Vector3.one);
                            b.Frustum(Vector3.zero, 0.12f, 0.12f, 0.1f, 10, c, light, 0, 0, true, true, light);
                            b.Pop();
                        }
                    }
                    break;

                case Accessory.CowboyHat:
                    b.PushTRS(Head + new Vector3(0.0f, -0.03f, 0), new Vector3(0, 0, HeadTilt), new Vector3(1.15f, 1f, 1f));
                    b.Frustum(Vector3.zero, 0.5f, 0.42f, 0.06f, 14, c, dark, 0, 0, true, true);
                    b.Frustum(new Vector3(0, 0.06f, 0), 0.23f, 0.25f, 0.3f, 12, c, light, 0, 0, true);
                    b.Frustum(new Vector3(0, 0.06f, 0), 0.232f, 0.234f, 0.07f, 12, dark, dark);
                    b.Pop();
                    break;

                case Accessory.PirateHat:
                    b.PushTRS(Head + new Vector3(0.0f, 0.0f, 0), new Vector3(0, 0, HeadTilt), Vector3.one);
                    b.Frustum(new Vector3(-0.52f, 0.0f, 0), 0.2f, 0.01f, 0.28f, 4, c, c);
                    b.Box(new Vector3(0, 0.08f, 0), new Vector3(0.9f, 0.16f, 0.2f), c, 0, 0, MeshBuilder.Mul(c, 1.4f));
                    b.Box(new Vector3(0, 0.22f, 0), new Vector3(0.55f, 0.14f, 0.18f), c, 0, 0, MeshBuilder.Mul(c, 1.4f));
                    b.Disc(new Vector3(0, 0.1f, -0.11f), 0.055f, 8, Color.white);
                    b.Box(new Vector3(0, 0.03f, -0.11f), new Vector3(0.1f, 0.015f, 0.01f), Color.white);
                    b.Pop();
                    b.Disc(Eyes + new Vector3(-EyeHalf, -0.0f, -0.02f), 0.085f, 10, new Color(0.05f, 0.05f, 0.07f));
                    b.Quad(Eyes + new Vector3(-EyeHalf - 0.09f, 0.05f, -0.02f), Eyes + new Vector3(0.1f, 0.19f, -0.02f),
                        Eyes + new Vector3(0.1f, 0.16f, -0.02f), Eyes + new Vector3(-EyeHalf - 0.09f, 0.02f, -0.02f), new Color(0.05f, 0.05f, 0.07f));
                    break;

                case Accessory.Headband:
                    b.PushTRS(Head + new Vector3(0.0f, -0.2f, 0.0f), new Vector3(0, 0, HeadTilt), Vector3.one);
                    b.Box(Vector3.zero, new Vector3(0.74f, 0.075f, 0.04f), c, 0, 0, light);
                    b.Box(new Vector3(0.0f, 0.0f, -0.022f), new Vector3(0.2f, 0.06f, 0.01f), Color.white);
                    b.Pop();
                    b.PushTRS(Head + new Vector3(0.38f, -0.23f, 0.0f), new Vector3(0, 0, 20), Vector3.one);
                    b.Quad(new Vector3(0, 0, 0), new Vector3(0.22f, 0.03f, 0), new Vector3(0.22f, -0.04f, 0), new Vector3(0, -0.05f, 0), c);
                    b.Pop();
                    b.PushTRS(Head + new Vector3(0.38f, -0.23f, 0.0f), new Vector3(0, 0, -15), Vector3.one);
                    b.Quad(new Vector3(0, 0, 0), new Vector3(0.2f, -0.03f, 0), new Vector3(0.2f, -0.1f, 0), new Vector3(0, -0.05f, 0), dark);
                    b.Pop();
                    break;
            }
            var mesh = b.ToMesh("acc_" + a, true);
            return mesh;
        }
    }
}
