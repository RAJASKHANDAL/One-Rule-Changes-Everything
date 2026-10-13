using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Generates chunky, flat-shaded low-poly meshes (rocks, pillars, plateaus, crystals, grass) procedurally.</summary>
public static class MeshFactory
{
    class MB
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<int> T = new List<int>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 center)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f - center) < 0f) { var t = b; b = c; c = t; }
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c);
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 center)
        {
            Tri(a, b, c, center);
            Tri(a, c, d, center);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(V);
            m.SetTriangles(T, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Scales a mesh so its bounds are 1x1x1 with X/Z centred and the base sitting at y = 0.</summary>
    static Mesh Normalize(Mesh m)
    {
        var v = m.vertices;
        var b = m.bounds;
        for (int i = 0; i < v.Length; i++)
            v[i] = new Vector3((v[i].x - b.center.x) / b.size.x, (v[i].y - b.min.y) / b.size.y, (v[i].z - b.center.z) / b.size.z);
        m.vertices = v;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ boulders

    public static Mesh Rock(string name, int seed, float jitter = 0.22f)
    {
        var rng = new System.Random(seed);
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var verts = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        int[] faces =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };
        var tris = new List<int[]>();
        var mids = new Dictionary<long, int>();
        int Mid(int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (mids.TryGetValue(key, out int idx)) return idx;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            return mids[key] = verts.Count - 1;
        }
        for (int i = 0; i < faces.Length; i += 3)
        {
            int a = faces[i], b = faces[i + 1], c = faces[i + 2];
            int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
            tris.Add(new[] { a, ab, ca }); tris.Add(new[] { b, bc, ab }); tris.Add(new[] { c, ca, bc }); tris.Add(new[] { ab, bc, ca });
        }
        for (int i = 0; i < verts.Count; i++)
        {
            float k = 1f + ((float)rng.NextDouble() - 0.5f) * 2f * jitter;
            var p = verts[i] * k;
            p.y = Mathf.Max(p.y, -0.55f); // flat-ish base
            verts[i] = p;
        }
        var mb = new MB();
        foreach (var tri in tris) mb.Tri(verts[tri[0]], verts[tri[1]], verts[tri[2]], Vector3.zero);
        return Normalize(mb.ToMesh(name));
    }

    // ------------------------------------------------------------------ pillars (tall rock columns)

    public static Mesh Pillar(string name, int seed, int sides, float topRatio)
    {
        var rng = new System.Random(seed);
        float[] heights = { 0f, 0.5f, 1f };
        float[] radii = { 1f, Mathf.Lerp(1f, topRatio, 0.55f), topRatio };
        var rings = new Vector3[3][];
        for (int r = 0; r < 3; r++)
        {
            rings[r] = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float ang = (i + ((float)rng.NextDouble() - 0.5f) * 0.4f) / sides * Mathf.PI * 2f;
                float rad = radii[r] * (0.82f + (float)rng.NextDouble() * 0.36f);
                float y = heights[r] + (r > 0 ? ((float)rng.NextDouble() - 0.5f) * 0.12f : 0f);
                rings[r][i] = new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad);
            }
        }
        var mb = new MB();
        var center = new Vector3(0f, 0.5f, 0f);
        for (int r = 0; r < 2; r++)
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                mb.Quad(rings[r][i], rings[r][j], rings[r + 1][j], rings[r + 1][i], center);
            }
        var top = new Vector3(0.05f, 1.12f, -0.04f);
        for (int i = 0; i < sides; i++) mb.Tri(rings[2][i], rings[2][(i + 1) % sides], top, center);
        for (int i = 0; i < sides; i++) mb.Tri(rings[0][i], rings[0][(i + 1) % sides], new Vector3(0f, -0.05f, 0f), center);
        return Normalize(mb.ToMesh(name));
    }

    // ------------------------------------------------------------------ plateau (flat top at exactly `height`)

    public static Mesh Plateau(string name, int seed, float radius, float height)
    {
        var rng = new System.Random(seed);
        const int sides = 8;
        var bottom = new Vector3[sides];
        var top = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float ang = i / (float)sides * Mathf.PI * 2f + Mathf.PI / sides;
            float jb = 1.12f + ((float)rng.NextDouble() - 0.5f) * 0.12f;
            bottom[i] = new Vector3(Mathf.Cos(ang) * radius * jb, 0f, Mathf.Sin(ang) * radius * jb);
            top[i] = new Vector3(Mathf.Cos(ang) * radius, height, Mathf.Sin(ang) * radius);
        }
        var mb = new MB();
        var center = new Vector3(0f, height * 0.5f, 0f);
        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            mb.Quad(bottom[i], bottom[j], top[j], top[i], center);
            mb.Tri(top[i], top[j], new Vector3(0f, height, 0f), center);
            mb.Tri(bottom[i], bottom[j], Vector3.zero, center);
        }
        return mb.ToMesh(name);
    }

    // ------------------------------------------------------------------ crystals

    static void AddCrystal(MB mb, Matrix4x4 m, float radius, float height, System.Random rng)
    {
        const int sides = 6;
        var ring0 = new Vector3[sides];
        var ring1 = new Vector3[sides];
        float twist = (float)rng.NextDouble() * 1f;
        for (int i = 0; i < sides; i++)
        {
            float ang = (i + twist) / sides * Mathf.PI * 2f;
            ring0[i] = m.MultiplyPoint3x4(new Vector3(Mathf.Cos(ang) * radius, -0.15f, Mathf.Sin(ang) * radius));
            ring1[i] = m.MultiplyPoint3x4(new Vector3(Mathf.Cos(ang) * radius * 0.92f, height * 0.72f, Mathf.Sin(ang) * radius * 0.92f));
        }
        var apex = m.MultiplyPoint3x4(new Vector3(radius * 0.1f, height, 0f));
        var center = m.MultiplyPoint3x4(new Vector3(0f, height * 0.4f, 0f));
        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            mb.Quad(ring0[i], ring0[j], ring1[j], ring1[i], center);
            mb.Tri(ring1[i], ring1[j], apex, center);
        }
    }

    public static Mesh CrystalCluster(string name, int seed, int count, float height, float spread)
    {
        var rng = new System.Random(seed);
        var mb = new MB();
        for (int i = 0; i < count; i++)
        {
            float ang = i / (float)count * Mathf.PI * 2f + (float)rng.NextDouble() * 0.6f;
            float dist = i == 0 ? 0f : spread * (0.6f + (float)rng.NextDouble() * 0.5f);
            var pos = new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            float tilt = i == 0 ? 4f : 14f + (float)rng.NextDouble() * 16f;
            var rot = Quaternion.Euler(Mathf.Sin(ang) * tilt, (float)rng.NextDouble() * 360f, -Mathf.Cos(ang) * tilt);
            float h = height * (i == 0 ? 1f : 0.45f + (float)rng.NextDouble() * 0.35f);
            float r = height * (i == 0 ? 0.17f : 0.1f + (float)rng.NextDouble() * 0.05f);
            AddCrystal(mb, Matrix4x4.TRS(pos, rot, Vector3.one), r, h, rng);
        }
        return mb.ToMesh(name);
    }

    // ------------------------------------------------------------------ grass

    public static Mesh Grass(string name, int seed, int blades)
    {
        var rng = new System.Random(seed);
        var mb = new MB();
        for (int i = 0; i < blades; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = Mathf.Sqrt((float)rng.NextDouble()) * 0.55f;
            var basePos = new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            float h = 0.45f + (float)rng.NextDouble() * 0.5f;
            var lean = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (0.08f + (float)rng.NextDouble() * 0.18f);
            var apex = basePos + lean + Vector3.up * h;
            float w = 0.07f + (float)rng.NextDouble() * 0.03f;
            float a0 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var p0 = basePos + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * w;
            var p1 = basePos + new Vector3(Mathf.Cos(a0 + 2.094f), 0f, Mathf.Sin(a0 + 2.094f)) * w;
            var p2 = basePos + new Vector3(Mathf.Cos(a0 + 4.189f), 0f, Mathf.Sin(a0 + 4.189f)) * w;
            var c = basePos + Vector3.up * (h * 0.3f);
            mb.Tri(p0, p1, apex, c); mb.Tri(p1, p2, apex, c); mb.Tri(p2, p0, apex, c);
        }
        return mb.ToMesh(name);
    }
}
