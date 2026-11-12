using System.Collections;
using UnityEngine;

namespace TilkayoRun
{
    public enum ObsKind { Log = 0, Rock = 1, Branch = 2, Wall = 3, Gap = 4 }

    /// <summary>Все процедурные меши выбранной темы: сегменты леса, препятствия, монета, небо.</summary>
    public class WorldMeshes
    {
        public const int SceneryVariants = 5;
        public Mesh[] scenery = new Mesh[SceneryVariants];
        public Mesh sky;
        public Mesh[][] obstacles = new Mesh[5][];
        public Mesh coin;
        public bool ready;

        static Color M(Color c, float f) { return MeshBuilder.Mul(c, f); }

        public void DestroyAll()
        {
            ready = false;
            for (int i = 0; i < scenery.Length; i++) if (scenery[i] != null) Object.Destroy(scenery[i]);
            if (sky != null) Object.Destroy(sky);
            if (coin != null) Object.Destroy(coin);
            for (int k = 0; k < obstacles.Length; k++)
                if (obstacles[k] != null)
                    for (int i = 0; i < obstacles[k].Length; i++) if (obstacles[k][i] != null) Object.Destroy(obstacles[k][i]);
        }

        /// <summary>Строит всё по одному мешу за кадр, чтобы не подвешивать телефон.</summary>
        public IEnumerator Build(ThemeDef th, float density)
        {
            ready = false;
            for (int i = 0; i < SceneryVariants; i++)
            {
                scenery[i] = Scenery(th, 100 + i * 31, density);
                yield return null;
            }
            sky = Sky(th);
            coin = Coin();
            yield return null;
            obstacles[(int)ObsKind.Log] = new[] { Log(th, 1), Log(th, 2) };
            obstacles[(int)ObsKind.Rock] = new[] { RockLow(th, 1), RockLow(th, 2) };
            yield return null;
            obstacles[(int)ObsKind.Branch] = new[] { Branch(th, 1), Branch(th, 2) };
            obstacles[(int)ObsKind.Wall] = new[] { Boulder(th), BrokenTrunk(th), BigTree(th) };
            obstacles[(int)ObsKind.Gap] = new[] { Gap(th, 1), Gap(th, 2) };
            yield return null;
            ready = true;
        }

        public Mesh GetObstacle(ObsKind k, int variant)
        {
            var arr = obstacles[(int)k];
            return arr[Mathf.Abs(variant) % arr.Length];
        }

        // ------------------------------------------------------------------ деревья
        static Color LeafPick(MeshBuilder b, ThemeDef th)
        {
            float r = b.Rand();
            if (r < 0.18f) return th.leafC;
            return r < 0.6f ? th.leafA : th.leafB;
        }

        static void Pine(MeshBuilder b, Vector3 p, float sc, ThemeDef th, Color leaf)
        {
            b.Frustum(p, 0.24f * sc, 0.16f * sc, 1.5f * sc, 6, M(th.trunk, 0.8f), th.trunk, 0f, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float y = (0.9f + i * 1.05f) * sc;
                float r = (1.6f - i * 0.42f) * sc;
                b.Frustum(p + new Vector3(0, y, 0), r, 0.02f, 1.75f * sc, 8, M(leaf, 0.78f), M(leaf, 1.12f), 0.5f + i * 0.15f, 1f);
            }
        }

        static void RoundTree(MeshBuilder b, Vector3 p, float sc, ThemeDef th, Color leaf)
        {
            b.Frustum(p, 0.3f * sc, 0.2f * sc, 2.3f * sc, 6, M(th.trunk, 0.8f), th.trunk, 0f, 0.4f);
            b.Blob(p + new Vector3(0, 3.0f * sc, 0), new Vector3(1.6f, 1.3f, 1.6f) * sc, 0.18f, leaf, 0.6f, 1f);
            b.Blob(p + new Vector3(0.95f * sc, 2.5f * sc, 0.3f * sc), new Vector3(1.0f, 0.85f, 1.0f) * sc, 0.2f, M(leaf, 0.92f), 0.6f, 1f);
            b.Blob(p + new Vector3(-0.9f * sc, 2.6f * sc, -0.2f * sc), new Vector3(1.0f, 0.85f, 1.0f) * sc, 0.2f, M(leaf, 1.05f), 0.6f, 1f);
        }

        static Mesh Scenery(ThemeDef th, int seed, float dens)
        {
            var b = new MeshBuilder(seed);
            float L = GameConfig.SegmentLength;
            float lw = GameConfig.LaneWidth;

            // раствор между плитами
            b.Box(new Vector3(0, -0.32f, L * 0.5f), new Vector3(lw * 3f + 0.5f, 0.2f, L + 0.1f), th.mortar);

            // плиты тропы
            int rows = 10;
            float rl = L / rows;
            for (int r = 0; r < rows; r++)
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    float w = lw - 0.14f - b.Rand(0f, 0.08f);
                    float len = rl - 0.14f - b.Rand(0f, 0.06f);
                    Color c = Color.Lerp(th.roadA, th.roadB, b.Rand());
                    b.Box(new Vector3(lane * lw, -0.13f, r * rl + rl * 0.5f), new Vector3(w, 0.26f, len), c, 0, 0, M(c, 1.1f));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                // земля
                b.Box(new Vector3(side * (3.6f + 30f), -0.3f, L * 0.5f), new Vector3(60f, 0.5f, L + 0.1f), th.ground, 0, 0, th.groundAlt);
                for (int i = 0; i < 9; i++)
                {
                    float x = side * b.Rand(4.2f, 45f), z = b.Rand(0f, L);
                    float sx = b.Rand(2f, 6f), sz = b.Rand(2f, 7f);
                    b.Quad(new Vector3(x - sx, -0.045f, z - sz), new Vector3(x + sx, -0.045f, z - sz),
                        new Vector3(x + sx, -0.045f, z + sz), new Vector3(x - sx, -0.045f, z + sz), th.groundAlt);
                }

                // камни у кромки тропы
                int curbs = Mathf.Max(4, Mathf.RoundToInt(12 * dens));
                for (int i = 0; i < curbs; i++)
                {
                    float x = side * b.Rand(3.85f, 4.5f), z = b.Rand(0f, L);
                    float r = b.Rand(0.25f, 0.5f);
                    b.Blob(new Vector3(x, 0.05f, z), new Vector3(r, r * 0.7f, r * 1.2f), 0.22f, th.rock, 0, 0, 1.1f, 0.7f);
                }

                // кусты
                int bushes = Mathf.RoundToInt(9 * dens);
                for (int i = 0; i < bushes; i++)
                {
                    float x = side * b.Rand(4.6f, 11f), z = b.Rand(0f, L);
                    float r = b.Rand(0.45f, 0.95f);
                    b.Blob(new Vector3(x, r * 0.55f, z), new Vector3(r * 1.2f, r * 0.8f, r * 1.2f), 0.22f, LeafPick(b, th), 0.2f, 0.7f);
                }

                // цветы/грибы
                int flowers = Mathf.RoundToInt(10 * dens);
                for (int i = 0; i < flowers; i++)
                {
                    float x = side * b.Rand(4.3f, 14f), z = b.Rand(0f, L);
                    b.Frustum(new Vector3(x, 0, z), 0.025f, 0.02f, 0.22f, 3, th.leafA, th.leafB, 0.2f, 0.5f);
                    b.Blob(new Vector3(x, 0.26f, z), new Vector3(0.09f, 0.07f, 0.09f), 0.1f, th.accent, 0.6f, 0.8f, 1.2f, 1f);
                }

                // трава
                int grass = Mathf.RoundToInt(18 * dens);
                for (int i = 0; i < grass; i++)
                {
                    float x = side * b.Rand(4.2f, 12f), z = b.Rand(0f, L);
                    b.Frustum(new Vector3(x, 0, z), 0.12f, 0.01f, b.Rand(0.35f, 0.7f), 4, M(th.leafA, 0.8f), th.leafB, 0.3f, 0.9f);
                }

                // деревья
                int trees = Mathf.RoundToInt(15 * dens);
                for (int i = 0; i < trees; i++)
                {
                    float x = side * (5.4f + Mathf.Pow(b.Rand(), 1.6f) * 30f);
                    float z = b.Rand(0f, L);
                    float sc = b.Rand(0.85f, 1.7f) * (1f + (Mathf.Abs(x) - 5f) / 60f);
                    bool pine = th.treeStyle == 0 || (th.treeStyle == 2 && b.Rand() < 0.5f);
                    Color leaf = LeafPick(b, th);
                    if (pine) Pine(b, new Vector3(x, 0, z), sc, th, leaf);
                    else RoundTree(b, new Vector3(x, 0, z), sc, th, leaf);
                }

                // валуны
                int rocks = Mathf.RoundToInt(5 * dens);
                for (int i = 0; i < rocks; i++)
                {
                    float x = side * b.Rand(7f, 26f), z = b.Rand(0f, L);
                    float r = b.Rand(0.6f, 1.5f);
                    b.Blob(new Vector3(x, r * 0.45f, z), new Vector3(r, r * 0.75f, r * 1.1f), 0.25f, th.rock, 0, 0, 1.1f, 0.7f);
                }

                // дальние холмы
                for (int i = 0; i < 2; i++)
                {
                    float x = side * b.Rand(40f, 58f), z = b.Rand(0f, L);
                    float r = b.Rand(10f, 16f);
                    b.Blob(new Vector3(x, -1f, z), new Vector3(r, r * 0.5f, r), 0.1f, M(th.ground, 0.85f), 0, 0, 1.15f, 0.8f);
                }
            }
            return b.ToMesh("scenery_" + seed);
        }

        // ------------------------------------------------------------------ препятствия
        static Mesh Log(ThemeDef th, int v)
        {
            var b = new MeshBuilder(300 + v);
            float r = v == 1 ? 0.44f : 0.4f;
            Color bark = M(th.trunk, 1.1f);
            Color cut = Color.Lerp(th.trunk, new Color(0.95f, 0.8f, 0.55f), 0.65f);
            b.PushTRS(new Vector3(0, r, 0), new Vector3(0, 0, 90), Vector3.one);
            b.Frustum(new Vector3(0, -1f, 0), r, r, 2f, 9, M(bark, 0.85f), bark, 0, 0, true, true, cut);
            b.Pop();
            // сучки
            b.PushTRS(new Vector3(0.4f, r * 1.7f, 0.05f), new Vector3(0, 0, -30), Vector3.one);
            b.Frustum(Vector3.zero, 0.09f, 0.03f, 0.3f, 5, bark, bark);
            b.Pop();
            b.PushTRS(new Vector3(-0.5f, r * 1.6f, -0.1f), new Vector3(0, 0, 35), Vector3.one);
            b.Frustum(Vector3.zero, 0.08f, 0.03f, 0.26f, 5, bark, bark);
            b.Pop();
            // мох
            b.Blob(new Vector3(b.Rand(-0.5f, 0.5f), r * 1.9f, 0), new Vector3(0.35f, 0.1f, 0.3f), 0.2f, th.leafB, 0, 0.3f);
            return b.ToMesh("log" + v);
        }

        static Mesh RockLow(ThemeDef th, int v)
        {
            var b = new MeshBuilder(400 + v);
            b.Blob(new Vector3(0, 0.4f, 0), new Vector3(0.95f, 0.58f, 0.72f), 0.2f, th.rock, 0, 0, 1.15f, 0.7f);
            b.Blob(new Vector3(v == 1 ? 0.75f : -0.75f, 0.24f, 0.15f), new Vector3(0.5f, 0.34f, 0.46f), 0.2f, M(th.rock, 0.9f), 0, 0, 1.15f, 0.7f);
            b.Blob(new Vector3(v == 1 ? -0.65f : 0.6f, 0.2f, -0.2f), new Vector3(0.4f, 0.28f, 0.38f), 0.2f, M(th.rock, 1.05f), 0, 0, 1.15f, 0.7f);
            b.Blob(new Vector3(0.1f, 0.88f, 0), new Vector3(0.45f, 0.14f, 0.38f), 0.2f, th.leafB, 0, 0.2f);
            return b.ToMesh("rock" + v);
        }

        static Mesh Branch(ThemeDef th, int v)
        {
            var b = new MeshBuilder(500 + v);
            Color bark = M(th.trunk, 1.05f);
            for (int s = -1; s <= 1; s += 2)
            {
                b.Frustum(new Vector3(s * 1.0f, 0, 0), 0.3f, 0.2f, 3.1f, 7, M(bark, 0.8f), bark, 0, 0.3f);
                b.Blob(new Vector3(s * 1.0f, 3.2f, 0), new Vector3(0.7f, 0.5f, 0.7f), 0.2f, v == 1 ? th.leafA : th.leafB, 0.3f, 0.7f);
            }
            b.PushTRS(new Vector3(0, 1.5f, 0), new Vector3(0, 0, 90), Vector3.one);
            Color cut = Color.Lerp(th.trunk, new Color(0.95f, 0.8f, 0.55f), 0.6f);
            b.Frustum(new Vector3(0, -1.25f, 0), 0.42f, 0.38f, 2.5f, 9, M(bark, 0.9f), bark, 0, 0, true, true, cut);
            b.Pop();
            b.Blob(new Vector3(0.1f, 2.0f, 0), new Vector3(0.95f, 0.35f, 0.5f), 0.22f, th.leafC, 0.3f, 0.7f);
            b.Blob(new Vector3(-0.5f, 2.05f, 0), new Vector3(0.6f, 0.3f, 0.45f), 0.22f, th.leafA, 0.3f, 0.7f);
            // предупреждающие светящиеся полоски на балке
            b.Box(new Vector3(-0.55f, 1.5f, -0.43f), new Vector3(0.18f, 0.5f, 0.02f), th.accent);
            b.Box(new Vector3(0.55f, 1.5f, -0.43f), new Vector3(0.18f, 0.5f, 0.02f), th.accent);
            return b.ToMesh("branch" + v);
        }

        static Mesh Boulder(ThemeDef th)
        {
            var b = new MeshBuilder(600);
            b.Blob(new Vector3(0, 1.4f, 0), new Vector3(1.0f, 1.55f, 0.95f), 0.16f, th.rock, 0, 0, 1.2f, 0.65f);
            b.Blob(new Vector3(0.45f, 0.6f, -0.2f), new Vector3(0.65f, 0.6f, 0.6f), 0.2f, M(th.rock, 0.92f), 0, 0, 1.2f, 0.65f);
            b.Blob(new Vector3(-0.45f, 2.55f, 0), new Vector3(0.5f, 0.2f, 0.45f), 0.2f, th.leafB, 0, 0.2f);
            b.Box(new Vector3(0, 1.0f, -0.88f), new Vector3(0.5f, 0.9f, 0.02f), th.accent);
            return b.ToMesh("boulder");
        }

        static Mesh BrokenTrunk(ThemeDef th)
        {
            var b = new MeshBuilder(700);
            Color bark = M(th.trunk, 1.0f);
            Color cut = Color.Lerp(th.trunk, new Color(0.95f, 0.8f, 0.55f), 0.6f);
            b.Frustum(Vector3.zero, 0.85f, 0.6f, 3.0f, 8, M(bark, 0.75f), bark, 0, 0.1f, true, false, cut);
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.PI * 2f * i / 6f;
                b.Frustum(new Vector3(Mathf.Cos(a) * 0.35f, 3.0f, Mathf.Sin(a) * 0.35f), 0.15f, 0.01f, b.Rand(0.3f, 0.75f), 4, cut, M(cut, 0.9f), 0.1f, 0.3f);
            }
            for (int i = 0; i < 3; i++)
            {
                b.PushTRS(new Vector3(Mathf.Cos(i * 2.1f) * 0.65f, 0, Mathf.Sin(i * 2.1f) * 0.65f), new Vector3(0, 0, i % 2 == 0 ? 55 : -55), Vector3.one);
                b.Frustum(Vector3.zero, 0.2f, 0.03f, 0.9f, 5, bark, bark);
                b.Pop();
            }
            b.Blob(new Vector3(0.3f, 1.6f, -0.55f), new Vector3(0.3f, 0.6f, 0.15f), 0.2f, th.leafB, 0, 0.2f);
            b.Box(new Vector3(0, 1.3f, -0.7f), new Vector3(0.5f, 0.7f, 0.02f), th.accent);
            return b.ToMesh("broken_trunk");
        }

        static Mesh BigTree(ThemeDef th)
        {
            var b = new MeshBuilder(800);
            Color bark = M(th.trunk, 1.0f);
            b.Frustum(Vector3.zero, 0.8f, 0.55f, 3.4f, 8, M(bark, 0.75f), bark, 0, 0.2f);
            for (int i = 0; i < 3; i++)
            {
                b.PushTRS(new Vector3(Mathf.Cos(i * 2.1f + 0.5f) * 0.7f, 0, Mathf.Sin(i * 2.1f + 0.5f) * 0.7f), new Vector3(0, 0, i % 2 == 0 ? 50 : -50), Vector3.one);
                b.Frustum(Vector3.zero, 0.22f, 0.03f, 0.9f, 5, bark, bark);
                b.Pop();
            }
            b.Blob(new Vector3(0, 5.0f, 0), new Vector3(1.9f, 1.5f, 1.9f), 0.18f, th.leafA, 0.5f, 1f);
            b.Blob(new Vector3(1.0f, 4.5f, 0.2f), new Vector3(1.2f, 1.0f, 1.2f), 0.2f, th.leafB, 0.5f, 1f);
            b.Blob(new Vector3(-1.0f, 4.6f, -0.2f), new Vector3(1.2f, 1.0f, 1.2f), 0.2f, th.leafC, 0.5f, 1f);
            b.Box(new Vector3(0, 1.3f, -0.8f), new Vector3(0.5f, 0.7f, 0.02f), th.accent);
            return b.ToMesh("big_tree");
        }

        static Mesh Gap(ThemeDef th, int v)
        {
            var b = new MeshBuilder(900 + v);
            float w = 2.0f, len = 3.0f;
            Color edge = M(th.pit, 2.2f);
            // тёмный провал чуть выше плит, чтобы закрыть растворные швы
            b.Quad(new Vector3(-w / 2, 0.012f, -len / 2), new Vector3(w / 2, 0.012f, -len / 2),
                new Vector3(w / 2, 0.012f, len / 2), new Vector3(-w / 2, 0.012f, len / 2),
                edge, edge, edge, edge);
            b.Quad(new Vector3(-w / 2 + 0.2f, 0.016f, -len / 2 + 0.25f), new Vector3(w / 2 - 0.2f, 0.016f, -len / 2 + 0.25f),
                new Vector3(w / 2 - 0.2f, 0.016f, len / 2 - 0.25f), new Vector3(-w / 2 + 0.2f, 0.016f, len / 2 - 0.25f), th.pit);
            // мерцающая "трещина" для читаемости на тёмных тропах
            Color glow = M(th.accent, 0.45f);
            b.Quad(new Vector3(-0.5f, 0.02f, -0.06f), new Vector3(0.5f, 0.02f, -0.06f), new Vector3(0.5f, 0.02f, 0.06f), new Vector3(-0.5f, 0.02f, 0.06f), glow);
            // сколы камней по краям
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.8f + i * 0.53f + b.Rand(-0.1f, 0.1f);
                    float r = b.Rand(0.13f, 0.22f);
                    b.Blob(new Vector3(x, 0.07f, s * (len / 2 + 0.02f)), new Vector3(r, r * 0.6f, r), 0.25f, th.roadA, 0, 0, 1.1f, 0.7f);
                }
            }
            for (int s = -1; s <= 1; s += 2)
            {
                b.Blob(new Vector3(s * (w / 2 + 0.05f), 0.06f, b.Rand(-0.8f, 0.8f)), new Vector3(0.14f, 0.1f, 0.2f), 0.25f, th.roadB, 0, 0, 1.1f, 0.7f);
            }
            return b.ToMesh("gap" + v);
        }

        // ------------------------------------------------------------------ монета
        static Mesh Coin()
        {
            var b = new MeshBuilder(1000);
            b.Shade = 0.03f;
            Color gold = new Color(1f, 0.78f, 0.12f);
            Color rim = new Color(1f, 0.93f, 0.55f);
            Color inner = new Color(1f, 0.62f, 0.08f);
            b.PushTRS(Vector3.zero, new Vector3(90, 0, 0), Vector3.one);
            b.Frustum(new Vector3(0, -0.05f, 0), 0.34f, 0.34f, 0.1f, 14, gold, gold, 0, 0, true, true, rim);
            b.Frustum(new Vector3(0, 0.05f, 0), 0.2f, 0.2f, 0.02f, 12, inner, inner, 0, 0, true, false, inner);
            b.Frustum(new Vector3(0, -0.07f, 0), 0.2f, 0.2f, 0.02f, 12, inner, inner, 0, 0, false, true, inner);
            b.Pop();
            return b.ToMesh("coin");
        }

        // ------------------------------------------------------------------ небо
        static Mesh Sky(ThemeDef th)
        {
            var b = new MeshBuilder(1100);
            b.Shade = 0f;
            float W = 700f;
            Color bot = th.skyBottom, mid = th.skyMid, top = th.skyTop;
            float y0 = -80f, y1 = 6f, y2 = 45f, y3 = 150f, y4 = 520f;
            b.Quad(new Vector3(-W, y0, 0), new Vector3(W, y0, 0), new Vector3(W, y1, 0), new Vector3(-W, y1, 0), bot, bot, bot, bot);
            b.Quad(new Vector3(-W, y1, 0), new Vector3(W, y1, 0), new Vector3(W, y2, 0), new Vector3(-W, y2, 0), bot, bot, mid, mid);
            b.Quad(new Vector3(-W, y2, 0), new Vector3(W, y2, 0), new Vector3(W, y3, 0), new Vector3(-W, y3, 0), mid, mid, top, top);
            b.Quad(new Vector3(-W, y3, 0), new Vector3(W, y3, 0), new Vector3(W, y4, 0), new Vector3(-W, y4, 0), top, top, top, top);

            // солнце / луна с мягким ореолом
            Vector3 sp = new Vector3(-70f, 52f, -1f);
            b.Disc(sp, 62f, 28, Color.Lerp(mid, th.sun, 0.18f));
            b.Disc(sp + new Vector3(0, 0, -0.5f), 42f, 28, Color.Lerp(mid, th.sun, 0.4f));
            b.Disc(sp + new Vector3(0, 0, -1f), 26f, 28, th.sun);

            if (th.stars)
            {
                for (int i = 0; i < 90; i++)
                {
                    float x = b.Rand(-300f, 300f), y = b.Rand(40f, 260f);
                    float r = b.Rand(0.5f, 1.4f);
                    b.Disc(new Vector3(x, y, -1.5f), r, 5, Color.Lerp(Color.white, th.skyTop, b.Rand(0f, 0.4f)));
                }
            }

            // горы-силуэты двумя слоями
            for (int layer = 0; layer < 2; layer++)
            {
                float z = -3f - layer * 3f;
                Color mc = Color.Lerp(bot, th.ground, 0.45f + layer * 0.3f);
                mc = M(mc, 0.95f - layer * 0.12f);
                float xPrev = -W;
                float hPrev = 6f + b.Rand(0f, 30f);
                for (float x = -W + 45f; x <= W + 45f; x += 45f)
                {
                    float h = 6f + b.Rand(0f, 34f) + layer * 4f;
                    b.Quad(new Vector3(xPrev, -20f, z), new Vector3(x, -20f, z), new Vector3(x, h, z), new Vector3(xPrev, hPrev, z), mc);
                    xPrev = x; hPrev = h;
                }
            }
            return b.ToMesh("sky");
        }
    }
}
