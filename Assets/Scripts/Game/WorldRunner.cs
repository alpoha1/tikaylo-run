using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilkayoRun
{
    public enum Hit { None, Crash, Fall }

    /// <summary>
    /// Бесконечная дорога: игрок стоит на месте, мир едет навстрёк (нет проблем с точностью float).
    /// Сегменты, препятствия и монеты берутся из пулов и переиспользуются.
    /// </summary>
    public class WorldRunner : MonoBehaviour
    {
        class Seg { public Transform t; public MeshFilter mf; }

        class Obs
        {
            public Transform t; public MeshFilter mf; public GameObject go;
            public ObsKind kind; public float halfW, halfD, bottom, top;
        }

        class CoinObj { public Transform t; public GameObject go; public float phase; public float y; }

        public WorldMeshes meshes = new WorldMeshes();
        public Action<Vector3> onCoin;
        public bool Busy { get; private set; }

        readonly List<Seg> segs = new List<Seg>();
        readonly List<Obs> obsPool = new List<Obs>();
        readonly List<Obs> obsActive = new List<Obs>();
        readonly List<CoinObj> coinPool = new List<CoinObj>();
        readonly List<CoinObj> coinActive = new List<CoinObj>();
        Transform skyRoot;
        MeshFilter skyFilter;
        float segOffset, spawnZ;
        System.Random rng = new System.Random();
        float baseSpeed = GameConfig.StartSpeed, maxSpeed = GameConfig.MaxSpeed;
        int lastPattern;

        public void Init()
        {
            for (int i = 0; i < GameConfig.SegmentCount; i++)
            {
                var go = new GameObject("Seg" + i);
                go.transform.SetParent(transform, false);
                var s = new Seg { t = go.transform, mf = go.AddComponent<MeshFilter>() };
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Fx.World;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                segs.Add(s);
            }
            for (int i = 0; i < 36; i++) obsPool.Add(MakeObs(i));
            for (int i = 0; i < 90; i++) coinPool.Add(MakeCoin(i));

            var sk = new GameObject("Sky");
            skyRoot = sk.transform;
            skyFilter = sk.AddComponent<MeshFilter>();
            var smr = sk.AddComponent<MeshRenderer>();
            smr.sharedMaterial = Fx.Sky;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.receiveShadows = false;
        }

        public Transform SkyTransform { get { return skyRoot; } }

        Obs MakeObs(int i)
        {
            var go = new GameObject("Obs" + i);
            go.transform.SetParent(transform, false);
            var o = new Obs { go = go, t = go.transform, mf = go.AddComponent<MeshFilter>() };
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Fx.World;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.SetActive(false);
            return o;
        }

        CoinObj MakeCoin(int i)
        {
            var go = new GameObject("Coin" + i);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Fx.Glow;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.SetActive(false);
            return new CoinObj { go = go, t = go.transform, phase = i * 0.37f };
        }

        // ------------------------------------------------------------ тема
        public IEnumerator BuildTheme(ThemeDef th, int quality)
        {
            Busy = true;
            meshes.DestroyAll();
            float dens = quality == 0 ? 0.55f : (quality == 1 ? 0.8f : 1f);
            yield return StartCoroutine(meshes.Build(th, dens));
            skyFilter.sharedMesh = meshes.sky;
            for (int i = 0; i < segs.Count; i++) segs[i].mf.sharedMesh = meshes.scenery[i % WorldMeshes.SceneryVariants];
            var coinMesh = meshes.coin;
            for (int i = 0; i < coinPool.Count; i++) coinPool[i].go.GetComponent<MeshFilter>().sharedMesh = coinMesh;
            Fx.SetThemeGlobals(th);
            Busy = false;
        }

        public void SetDifficulty(float startSpeed, float maxSpd) { baseSpeed = startSpeed; maxSpeed = maxSpd; }

        // ------------------------------------------------------------ запуск
        public void ResetWorld(bool withGrace)
        {
            for (int i = obsActive.Count - 1; i >= 0; i--) ReleaseObs(i);
            for (int i = coinActive.Count - 1; i >= 0; i--) ReleaseCoin(i);
            segOffset = -20f;
            spawnZ = withGrace ? 75f : 9999f;
            lastPattern = 0;
            LayoutSegments();
        }

        void LayoutSegments()
        {
            for (int i = 0; i < segs.Count; i++)
                segs[i].t.position = new Vector3(0, 0, segOffset + i * GameConfig.SegmentLength);
        }

        // ------------------------------------------------------------ тик
        public void Tick(float dt, float speed, bool spawn)
        {
            if (!meshes.ready) return;
            float dz = speed * dt;

            segOffset -= dz;
            while (segOffset < -(GameConfig.SegmentLength + GameConfig.SegmentBack))
            {
                segOffset += GameConfig.SegmentLength;
                var first = segs[0];
                segs.RemoveAt(0);
                segs.Add(first);
                first.mf.sharedMesh = meshes.scenery[rng.Next(WorldMeshes.SceneryVariants)];
            }
            LayoutSegments();

            for (int i = obsActive.Count - 1; i >= 0; i--)
            {
                var o = obsActive[i];
                var p = o.t.position; p.z -= dz; o.t.position = p;
                if (p.z < -14f) ReleaseObs(i);
            }

            float time = Time.time;
            for (int i = coinActive.Count - 1; i >= 0; i--)
            {
                var c = coinActive[i];
                var p = c.t.position; p.z -= dz;
                p.y = c.y + Mathf.Sin(time * 3f + c.phase) * 0.06f;
                c.t.position = p;
                c.t.rotation = Quaternion.Euler(0f, time * 220f + c.phase * 57f, 0f);
                if (p.z < -14f) ReleaseCoin(i);
            }

            if (spawn)
            {
                spawnZ -= dz;
                float diff = Mathf.Clamp01((speed - baseSpeed) / Mathf.Max(1f, maxSpeed - baseSpeed));
                int guard = 0;
                while (spawnZ < GameConfig.SpawnAhead && guard++ < 4)
                {
                    float timeGap = Mathf.Lerp(1.9f, 1.05f, diff) + Rand(0f, 0.5f * (1f - diff));
                    float spacing = Mathf.Max(12f, timeGap * speed);
                    SpawnRow(spawnZ, spacing, diff);
                    spawnZ += spacing;
                }
            }
        }

        float Rand(float a, float b) { return a + (b - a) * (float)rng.NextDouble(); }

        // ------------------------------------------------------------ пулы
        Obs GetObs()
        {
            for (int i = 0; i < obsPool.Count; i++)
                if (!obsPool[i].go.activeSelf) return obsPool[i];
            return null;
        }

        void ReleaseObs(int activeIdx)
        {
            var o = obsActive[activeIdx];
            o.go.SetActive(false);
            obsActive.RemoveAt(activeIdx);
        }

        void ReleaseCoin(int activeIdx)
        {
            var c = coinActive[activeIdx];
            c.go.SetActive(false);
            coinActive.RemoveAt(activeIdx);
        }

        void PlaceObs(ObsKind kind, int lane, float z)
        {
            var o = GetObs();
            if (o == null) return;
            o.kind = kind;
            o.halfW = 0.85f;
            int variant = rng.Next(3);
            switch (kind)
            {
                case ObsKind.Log: o.halfD = 0.55f; o.bottom = 0f; o.top = 0.85f; break;
                case ObsKind.Rock: o.halfD = 0.75f; o.bottom = 0f; o.top = 0.95f; break;
                case ObsKind.Branch: o.halfD = 0.45f; o.bottom = 1.05f; o.top = 3.4f; break;
                case ObsKind.Wall: o.halfD = 0.85f; o.bottom = 0f; o.top = 3.5f; break;
                default: o.halfD = 1.5f; o.bottom = 0f; o.top = 0f; break;
            }
            o.mf.sharedMesh = meshes.GetObstacle(kind, variant);
            o.t.position = new Vector3(lane * GameConfig.LaneWidth, 0f, z);
            o.t.rotation = Quaternion.identity;
            o.go.SetActive(true);
            obsActive.Add(o);
        }

        void PlaceCoin(int lane, float z, float y)
        {
            CoinObj c = null;
            for (int i = 0; i < coinPool.Count; i++) if (!coinPool[i].go.activeSelf) { c = coinPool[i]; break; }
            if (c == null) return;
            c.y = y;
            c.t.position = new Vector3(lane * GameConfig.LaneWidth, y, z);
            c.go.SetActive(true);
            coinActive.Add(c);
        }

        // ------------------------------------------------------------ генерация рядов (честная)
        static readonly ObsKind[] Passable = { ObsKind.Log, ObsKind.Rock, ObsKind.Branch, ObsKind.Gap };

        ObsKind RandomKind(bool allowWall, float diff)
        {
            float r = (float)rng.NextDouble();
            if (allowWall && r < 0.18f) return ObsKind.Wall;
            r = (float)rng.NextDouble();
            if (r < 0.28f) return ObsKind.Log;
            if (r < 0.52f) return ObsKind.Rock;
            if (r < 0.78f) return ObsKind.Branch;
            return ObsKind.Gap;
        }

        void SpawnRow(float z, float spacing, float diff)
        {
            var kinds = new int[3] { -1, -1, -1 };
            float r = (float)rng.NextDouble();
            float p1 = 0.42f - 0.15f * diff;
            float p2 = p1 + 0.33f;
            int pattern;
            if (r < p1) pattern = 1;
            else if (r < p2) pattern = 2;
            else pattern = 3;
            if (pattern == 3 && diff < 0.15f) pattern = 2;

            int free = -1;
            if (pattern == 1)
            {
                kinds[rng.Next(3)] = (int)RandomKind(true, diff);
            }
            else if (pattern == 2)
            {
                free = rng.Next(3);
                for (int l = 0; l < 3; l++) if (l != free) kinds[l] = (int)RandomKind(true, diff);
            }
            else
            {
                bool same = rng.NextDouble() < 0.25;
                ObsKind k0 = Passable[rng.Next(Passable.Length)];
                for (int l = 0; l < 3; l++) kinds[l] = (int)(same ? k0 : Passable[rng.Next(Passable.Length)]);
            }

            // обязательно хотя бы одна полоса без стены
            int walls = 0;
            for (int l = 0; l < 3; l++) if (kinds[l] == (int)ObsKind.Wall) walls++;
            if (walls >= 3) kinds[rng.Next(3)] = (int)ObsKind.Log;

            for (int l = 0; l < 3; l++)
                if (kinds[l] >= 0) PlaceObs((ObsKind)kinds[l], l - 1, z);

            // монеты: между рядами по свободной полосе + дуги над препятствиями
            int coinLane = -1;
            if (free >= 0) coinLane = free;
            else
            {
                for (int l = 0; l < 3; l++) if (kinds[l] < 0) { coinLane = l; break; }
            }
            if (coinLane < 0)
            {
                for (int l = 0; l < 3; l++) if (kinds[l] != (int)ObsKind.Wall) { coinLane = l; break; }
            }

            if (rng.NextDouble() < 0.75 && spacing > 14f && coinLane >= 0)
            {
                float z0 = z + 4f, z1 = z + spacing - 4f;
                for (float cz = z0; cz <= z1; cz += 1.7f) PlaceCoin(coinLane - 1, cz, 0.75f);
            }

            for (int l = 0; l < 3; l++)
            {
                if (kinds[l] < 0) continue;
                var k = (ObsKind)kinds[l];
                if ((k == ObsKind.Log || k == ObsKind.Rock || k == ObsKind.Gap) && rng.NextDouble() < 0.5)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        float t = i / 2f;
                        PlaceCoin(l - 1, z + i * 1.7f, 0.7f + (1f - t * t) * 1.2f);
                    }
                }
                else if (k == ObsKind.Branch && rng.NextDouble() < 0.5)
                {
                    for (int i = -1; i <= 1; i++) PlaceCoin(l - 1, z + i * 1.5f, 0.45f);
                }
            }
        }

        // ------------------------------------------------------------ столкновения и монеты
        public Hit CheckHit(PlayerController p)
        {
            for (int i = 0; i < obsActive.Count; i++)
            {
                var o = obsActive[i];
                var pos = o.t.position;
                float dz = Mathf.Abs(pos.z);
                if (dz > o.halfD + 0.4f) continue;
                float dx = Mathf.Abs(pos.x - p.X);
                if (dx > o.halfW + p.HalfW) continue;

                if (o.kind == ObsKind.Gap)
                {
                    if (dz < o.halfD - 0.3f && p.Y < 0.12f) return Hit.Fall;
                    continue;
                }
                float pBottom = p.Y;
                float pTop = p.Y + (p.Sliding ? 0.55f : 1.25f);
                if (pBottom < o.top - 0.05f && pTop > o.bottom + 0.05f) return Hit.Crash;
            }
            return Hit.None;
        }

        public int CollectCoins(PlayerController p)
        {
            int n = 0;
            for (int i = coinActive.Count - 1; i >= 0; i--)
            {
                var c = coinActive[i];
                var pos = c.t.position;
                if (Mathf.Abs(pos.z) > 0.8f) continue;
                if (Mathf.Abs(pos.x - p.X) > 0.8f) continue;
                float mid = p.Y + (p.Sliding ? 0.3f : 0.65f);
                if (Mathf.Abs(pos.y - mid) > 0.95f) continue;
                if (onCoin != null) onCoin(pos);
                ReleaseCoin(i);
                n++;
            }
            return n;
        }
    }
}
