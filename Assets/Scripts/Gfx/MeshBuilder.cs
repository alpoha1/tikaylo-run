using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TilkayoRun
{
    /// <summary>
    /// Процедурный low-poly: плоское затенение, цвет в вершинах, uv.x = вес покачивания (для шейдера мира).
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color32> cols = new List<Color32>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();
        Matrix4x4 m = Matrix4x4.identity;
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        readonly System.Random rnd;
        public float Shade = 0.06f;

        public MeshBuilder(int seed = 1) { rnd = new System.Random(seed); }

        public float Rand() { return (float)rnd.NextDouble(); }
        public float Rand(float a, float b) { return a + (b - a) * (float)rnd.NextDouble(); }
        public int RandInt(int n) { return rnd.Next(n); }

        public static Color Mul(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, 1f); }

        // ---------- трансформации ----------
        public void Push(Matrix4x4 mm) { stack.Push(m); m = m * mm; }
        public void PushTRS(Vector3 pos, Vector3 euler, Vector3 scale)
        {
            Push(Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale));
        }
        public void Pop() { m = stack.Pop(); }

        // ---------- примитивы нижнего уровня ----------
        Color32 Shaded(Color c, float s)
        {
            return new Color(Mathf.Clamp01(c.r * s), Mathf.Clamp01(c.g * s), Mathf.Clamp01(c.b * s), 1f);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc, float wa, float wb, float wc)
        {
            Vector3 pa = m.MultiplyPoint3x4(a), pb = m.MultiplyPoint3x4(b), pc = m.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(pb - pa, pc - pa);
            if (n.sqrMagnitude < 1e-12f) return;
            n.Normalize();
            float s = 1f + (Rand() * 2f - 1f) * Shade;
            int i = verts.Count;
            verts.Add(pa); verts.Add(pb); verts.Add(pc);
            normals.Add(n); normals.Add(n); normals.Add(n);
            cols.Add(Shaded(ca, s)); cols.Add(Shaded(cb, s)); cols.Add(Shaded(cc, s));
            uvs.Add(new Vector2(wa, 0)); uvs.Add(new Vector2(wb, 0)); uvs.Add(new Vector2(wc, 0));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd,
            float wa = 0, float wb = 0, float wc = 0, float wd = 0)
        {
            Vector3 pa = m.MultiplyPoint3x4(a), pb = m.MultiplyPoint3x4(b), pc = m.MultiplyPoint3x4(c), pd = m.MultiplyPoint3x4(d);
            Vector3 n = Vector3.Cross(pb - pa, pc - pa);
            if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(pc - pa, pd - pa);
            if (n.sqrMagnitude < 1e-12f) return;
            n.Normalize();
            float s = 1f + (Rand() * 2f - 1f) * Shade;
            int i = verts.Count;
            verts.Add(pa); verts.Add(pb); verts.Add(pc); verts.Add(pd);
            for (int k = 0; k < 4; k++) normals.Add(n);
            cols.Add(Shaded(ca, s)); cols.Add(Shaded(cb, s)); cols.Add(Shaded(cc, s)); cols.Add(Shaded(cd, s));
            uvs.Add(new Vector2(wa, 0)); uvs.Add(new Vector2(wb, 0)); uvs.Add(new Vector2(wc, 0)); uvs.Add(new Vector2(wd, 0));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col, float w = 0)
        {
            Quad(a, b, c, d, col, col, col, col, w, w, w, w);
        }

        // ---------- фигуры ----------
        public void Box(Vector3 c, Vector3 size, Color col, float swB = 0f, float swT = 0f, Color? topColor = null)
        {
            Vector3 h = size * 0.5f;
            Color ct = topColor.HasValue ? topColor.Value : col;
            Vector3 b0 = c + new Vector3(-h.x, -h.y, -h.z), b1 = c + new Vector3(h.x, -h.y, -h.z);
            Vector3 b2 = c + new Vector3(h.x, -h.y, h.z), b3 = c + new Vector3(-h.x, -h.y, h.z);
            Vector3 t0 = c + new Vector3(-h.x, h.y, -h.z), t1 = c + new Vector3(h.x, h.y, -h.z);
            Vector3 t2 = c + new Vector3(h.x, h.y, h.z), t3 = c + new Vector3(-h.x, h.y, h.z);
            Quad(b0, b1, t1, t0, col, col, col, col, swB, swB, swT, swT);
            Quad(b1, b2, t2, t1, col, col, col, col, swB, swB, swT, swT);
            Quad(b2, b3, t3, t2, col, col, col, col, swB, swB, swT, swT);
            Quad(b3, b0, t0, t3, col, col, col, col, swB, swB, swT, swT);
            Quad(t0, t1, t2, t3, ct, ct, ct, ct, swT, swT, swT, swT);
            Quad(b0, b3, b2, b1, col, col, col, col, swB, swB, swB, swB);
        }

        /// <summary>Усечённый конус/цилиндр вдоль +Y.</summary>
        public void Frustum(Vector3 bc, float rb, float rt, float h, int sides, Color cb, Color ct,
            float swB = 0f, float swT = 0f, bool capTop = false, bool capBottom = false, Color? capColor = null)
        {
            Color cap = capColor.HasValue ? capColor.Value : ct;
            Vector3 tc = bc + new Vector3(0, h, 0);
            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides, a1 = Mathf.PI * 2f * (i + 1) / sides;
                Vector3 b0 = bc + new Vector3(Mathf.Cos(a0) * rb, 0, Mathf.Sin(a0) * rb);
                Vector3 b1 = bc + new Vector3(Mathf.Cos(a1) * rb, 0, Mathf.Sin(a1) * rb);
                Vector3 t0 = tc + new Vector3(Mathf.Cos(a0) * rt, 0, Mathf.Sin(a0) * rt);
                Vector3 t1 = tc + new Vector3(Mathf.Cos(a1) * rt, 0, Mathf.Sin(a1) * rt);
                if (rt < 0.002f) Tri(b0, b1, tc, cb, cb, ct, swB, swB, swT);
                else Quad(b0, b1, t1, t0, cb, cb, ct, ct, swB, swB, swT, swT);
                if (capTop && rt >= 0.002f) Tri(tc, t0, t1, cap, cap, cap, swT, swT, swT);
                if (capBottom) Tri(bc, b1, b0, cap, cap, cap, swB, swB, swB);
            }
        }

        static readonly Vector3[] ico = BuildIco();
        static readonly int[] icoF =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        static Vector3[] BuildIco()
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Length; i++) v[i].Normalize();
            return v;
        }

        /// <summary>Неровный шар (камень, куст, крона).</summary>
        public void Blob(Vector3 center, Vector3 radii, float jitter, Color col, float swBase = 0f, float swTop = 0f,
            float brightTop = 1.12f, float brightBottom = 0.78f)
        {
            var p = new Vector3[ico.Length];
            for (int i = 0; i < ico.Length; i++)
            {
                float j = 1f + (Rand() * 2f - 1f) * jitter;
                p[i] = center + new Vector3(ico[i].x * radii.x, ico[i].y * radii.y, ico[i].z * radii.z) * j;
            }
            for (int f = 0; f < icoF.Length; f += 3)
            {
                int a = icoF[f], b = icoF[f + 1], c = icoF[f + 2];
                float ya = (ico[a].y + 1f) * 0.5f, yb = (ico[b].y + 1f) * 0.5f, yc = (ico[c].y + 1f) * 0.5f;
                Tri(p[a], p[b], p[c],
                    Mul(col, Mathf.Lerp(brightBottom, brightTop, ya)),
                    Mul(col, Mathf.Lerp(brightBottom, brightTop, yb)),
                    Mul(col, Mathf.Lerp(brightBottom, brightTop, yc)),
                    Mathf.Lerp(swBase, swTop, ya), Mathf.Lerp(swBase, swTop, yb), Mathf.Lerp(swBase, swTop, yc));
            }
        }

        /// <summary>Плоский диск, повёрнутый к камере (-Z), для солнца/звёзд.</summary>
        public void Disc(Vector3 center, float radius, int sides, Color col)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides, a1 = Mathf.PI * 2f * (i + 1) / sides;
                Tri(center, center + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0),
                    center + new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0), col, col, col, 0, 0, 0);
            }
        }

        public int VertexCount { get { return verts.Count; } }

        public Mesh ToMesh(string name, bool markStatic = true)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            // чтобы камера не отсекала сдвинутые шейдером вершины
            var bnd = mesh.bounds;
            bnd.Expand(new Vector3(4f, 4f, 4f));
            mesh.bounds = bnd;
            if (markStatic) mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
