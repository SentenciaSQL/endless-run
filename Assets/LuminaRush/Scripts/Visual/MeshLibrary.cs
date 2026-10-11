using System.Collections.Generic;
using UnityEngine;

namespace LuminaRush
{
    public static class MeshLibrary
    {
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        static Material shared;

        public static Material SharedMaterial()
        {
            if (shared != null)
                return shared;
            shared = Resources.Load<Material>("VertexLit");
            if (shared != null)
                return shared;
            Shader shader = Shader.Find("LuminaRush/VertexLit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            shared = new Material(shader);
            shared.color = Color.white;
            return shared;
        }

        public static Mesh Box()
        {
            return Get("box", BuildBox);
        }

        public static Mesh Wedge()
        {
            return Get("wedge", BuildWedge);
        }

        public static Mesh Sphere()
        {
            return Get("sphere", () => BuildSphere(6, 5));
        }

        public static Mesh Coin()
        {
            return Get("coin", BuildCoin);
        }

        public static Mesh Cone()
        {
            return Get("cone", BuildCone);
        }

        public static Mesh Cylinder()
        {
            return Get("cylinder", BuildCylinder);
        }

        public static Mesh Crystal()
        {
            return Get("crystal", BuildCrystal);
        }

        public static Mesh Ground()
        {
            return Get("ground", BuildGround);
        }

        public static Mesh Cloud()
        {
            return Get("cloud", BuildCloud);
        }

        public static Mesh Rock()
        {
            return Get("rock", BuildRock);
        }

        public static GameObject Create(string name, Mesh mesh, Material material, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go;
        }

        public static void Tint(Renderer renderer, Color color, MaterialPropertyBlock block)
        {
            if (renderer == null)
                return;
            renderer.GetPropertyBlock(block);
            block.SetColor("_Tint", color);
            renderer.SetPropertyBlock(block);
        }

        static Mesh Get(string key, System.Func<Mesh> build)
        {
            Mesh mesh;
            if (Cache.TryGetValue(key, out mesh) && mesh != null)
                return mesh;
            mesh = build();
            mesh.name = "Lumina_" + key;
            Cache[key] = mesh;
            return mesh;
        }

        static Mesh BuildBox()
        {
            var vertices = new Vector3[24];
            var normals = new Vector3[24];
            var colors = new Color[24];
            var triangles = new int[36];
            Vector3[] faces =
            {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
            };
            int v = 0;
            int t = 0;
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = faces[f];
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 w = Vector3.Cross(n, u);
                Vector3 c = n * 0.5f;
                vertices[v] = c + (-u - w) * 0.5f;
                vertices[v + 1] = c + (u - w) * 0.5f;
                vertices[v + 2] = c + (u + w) * 0.5f;
                vertices[v + 3] = c + (-u + w) * 0.5f;
                float shade = 0.82f + (f % 3) * 0.06f;
                Color col = new Color(shade, shade, shade, 1f);
                for (int i = 0; i < 4; i++)
                {
                    normals[v + i] = n;
                    colors[v + i] = col;
                }
                triangles[t++] = v;
                triangles[t++] = v + 2;
                triangles[t++] = v + 1;
                triangles[t++] = v;
                triangles[t++] = v + 3;
                triangles[t++] = v + 2;
                v += 4;
            }
            return Finish(vertices, normals, colors, triangles);
        }

        static Mesh BuildWedge()
        {
            var vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 1f, 0.5f), new Vector3(-0.5f, 1f, 0.5f)
            };
            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5
            };
            return FinishWithComputedNormals(vertices, triangles, new Color(0.92f, 0.92f, 0.92f, 1f));
        }

        static Mesh BuildSphere(int lon, int lat)
        {
            int vertCount = (lat + 1) * (lon + 1);
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var colors = new Color[vertCount];
            int index = 0;
            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                float phi = v * Mathf.PI;
                for (int x = 0; x <= lon; x++)
                {
                    float u = x / (float)lon;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    vertices[index] = n * 0.5f;
                    normals[index] = n;
                    float shade = 0.78f + 0.22f * n.y;
                    colors[index] = new Color(shade, shade, shade, 1f);
                    index++;
                }
            }
            var triangles = new int[lat * lon * 6];
            int t = 0;
            for (int y = 0; y < lat; y++)
            {
                for (int x = 0; x < lon; x++)
                {
                    int a = y * (lon + 1) + x;
                    int b = a + lon + 1;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                }
            }
            return Finish(vertices, normals, colors, triangles);
        }

        static Mesh BuildCoin()
        {
            const int sides = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float half = 0.12f;
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? half : -half;
                int center = vertices.Count;
                vertices.Add(new Vector3(0f, y, 0f));
                int ring = vertices.Count;
                for (int i = 0; i < sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(a) * 0.5f, y, Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < sides; i++)
                {
                    int n = ring + i;
                    int m = ring + (i + 1) % sides;
                    if (cap == 0)
                    {
                        triangles.Add(center); triangles.Add(n); triangles.Add(m);
                    }
                    else
                    {
                        triangles.Add(center); triangles.Add(m); triangles.Add(n);
                    }
                }
            }
            int sideStart = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                vertices.Add(p + Vector3.up * half);
                vertices.Add(p - Vector3.up * half);
            }
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                int a = sideStart + i * 2;
                int b = a + 1;
                int c = sideStart + n * 2;
                int d = c + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
            return FinishWithComputedNormals(vertices.ToArray(), triangles.ToArray(), new Color(1f, 0.92f, 0.55f, 1f));
        }

        static Mesh BuildCone()
        {
            const int sides = 7;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int apex = 0;
            vertices.Add(new Vector3(0f, 0.5f, 0f));
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f));
            }
            int center = vertices.Count;
            vertices.Add(new Vector3(0f, -0.5f, 0f));
            for (int i = 0; i < sides; i++)
            {
                int n = 1 + i;
                int m = 1 + (i + 1) % sides;
                triangles.Add(apex); triangles.Add(n); triangles.Add(m);
                triangles.Add(center); triangles.Add(m); triangles.Add(n);
            }
            return FinishWithComputedNormals(vertices.ToArray(), triangles.ToArray(), new Color(0.9f, 0.9f, 0.9f, 1f));
        }

        static Mesh BuildCylinder()
        {
            const int sides = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? 0.5f : -0.5f;
                int center = vertices.Count;
                vertices.Add(new Vector3(0f, y, 0f));
                int ring = vertices.Count;
                for (int i = 0; i < sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(a) * 0.5f, y, Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < sides; i++)
                {
                    int n = ring + i;
                    int m = ring + (i + 1) % sides;
                    if (cap == 0)
                    {
                        triangles.Add(center); triangles.Add(n); triangles.Add(m);
                    }
                    else
                    {
                        triangles.Add(center); triangles.Add(m); triangles.Add(n);
                    }
                }
            }
            int side = vertices.Count;
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                vertices.Add(p + Vector3.up * 0.5f);
                vertices.Add(p - Vector3.up * 0.5f);
            }
            for (int i = 0; i < sides; i++)
            {
                int a = side + i * 2;
                int c = a + 2;
                triangles.Add(a); triangles.Add(c); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(c); triangles.Add(c + 1);
            }
            return FinishWithComputedNormals(vertices.ToArray(), triangles.ToArray(), new Color(0.88f, 0.88f, 0.88f, 1f));
        }

        static Mesh BuildCrystal()
        {
            var vertices = new[]
            {
                new Vector3(0f, 0.55f, 0f),
                new Vector3(0.28f, 0.05f, 0f),
                new Vector3(0f, 0.05f, 0.28f),
                new Vector3(-0.28f, 0.05f, 0f),
                new Vector3(0f, 0.05f, -0.28f),
                new Vector3(0f, -0.55f, 0f)
            };
            var triangles = new[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1,
                5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4
            };
            return FinishWithComputedNormals(vertices, triangles, new Color(0.85f, 0.95f, 1f, 1f));
        }

        static Mesh BuildGround()
        {
            const float width = 14f;
            const float length = 40f;
            const float lane = 2.4f;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            float[] edges = { -width * 0.5f, -lane * 1.5f, -lane * 0.5f, lane * 0.5f, lane * 1.5f, width * 0.5f };
            Color shoulder = new Color(0.72f, 0.72f, 0.72f, 1f);
            Color laneColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            Color mark = new Color(1f, 1f, 1f, 1f);
            for (int i = 0; i < edges.Length - 1; i++)
            {
                bool isLane = i >= 1 && i <= 3;
                Color col = isLane ? (i == 2 ? mark : laneColor) : shoulder;
                AddQuad(vertices, colors, triangles,
                    new Vector3(edges[i], 0f, 0f),
                    new Vector3(edges[i + 1], 0f, 0f),
                    new Vector3(edges[i + 1], 0f, length),
                    new Vector3(edges[i], 0f, length),
                    col);
            }
            AddQuad(vertices, colors, triangles,
                new Vector3(-width * 0.5f, -0.35f, 0f),
                new Vector3(width * 0.5f, -0.35f, 0f),
                new Vector3(width * 0.5f, 0f, 0f),
                new Vector3(-width * 0.5f, 0f, 0f),
                shoulder * 0.8f);
            AddQuad(vertices, colors, triangles,
                new Vector3(-width * 0.5f, 0f, length),
                new Vector3(width * 0.5f, 0f, length),
                new Vector3(width * 0.5f, -0.35f, length),
                new Vector3(-width * 0.5f, -0.35f, length),
                shoulder * 0.8f);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;
            return Finish(vertices.ToArray(), normals, colors.ToArray(), triangles.ToArray());
        }

        static Mesh BuildCloud()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AppendBox(vertices, triangles, new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0.55f, 0.8f));
            AppendBox(vertices, triangles, new Vector3(-0.7f, 0.15f, 0.1f), new Vector3(0.9f, 0.5f, 0.7f));
            AppendBox(vertices, triangles, new Vector3(0.75f, 0.12f, -0.05f), new Vector3(0.8f, 0.42f, 0.6f));
            return FinishWithComputedNormals(vertices.ToArray(), triangles.ToArray(), Color.white);
        }

        static Mesh BuildRock()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AppendBox(vertices, triangles, Vector3.zero, new Vector3(1.1f, 0.7f, 0.9f));
            AppendBox(vertices, triangles, new Vector3(0.35f, 0.25f, 0.15f), new Vector3(0.55f, 0.45f, 0.5f));
            AppendBox(vertices, triangles, new Vector3(-0.3f, 0.1f, -0.2f), new Vector3(0.4f, 0.35f, 0.4f));
            return FinishWithComputedNormals(vertices.ToArray(), triangles.ToArray(), new Color(0.8f, 0.8f, 0.8f, 1f));
        }

        static void AppendBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] p =
            {
                center + new Vector3(-h.x, -h.y, -h.z),
                center + new Vector3(h.x, -h.y, -h.z),
                center + new Vector3(h.x, -h.y, h.z),
                center + new Vector3(-h.x, -h.y, h.z),
                center + new Vector3(-h.x, h.y, -h.z),
                center + new Vector3(h.x, h.y, -h.z),
                center + new Vector3(h.x, h.y, h.z),
                center + new Vector3(-h.x, h.y, h.z)
            };
            int[] faces =
            {
                0, 1, 2, 0, 2, 3,
                4, 7, 6, 4, 6, 5,
                0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3,
                3, 7, 4, 3, 4, 0
            };
            int start = vertices.Count;
            vertices.AddRange(p);
            for (int i = 0; i < faces.Length; i++)
                triangles.Add(start + faces[i]);
        }

        static void AddQuad(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            colors.Add(color); colors.Add(color); colors.Add(color); colors.Add(color);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
        }

        static Mesh Finish(Vector3[] vertices, Vector3[] normals, Color[] colors, int[] triangles)
        {
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh FinishWithComputedNormals(Vector3[] vertices, int[] triangles, Color color)
        {
            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = color;
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
