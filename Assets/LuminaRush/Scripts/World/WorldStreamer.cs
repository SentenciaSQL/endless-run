using System.Collections.Generic;
using UnityEngine;

namespace LuminaRush
{
    public sealed class WorldStreamer : MonoBehaviour
    {
        struct Chunk
        {
            public GameObject Root;
            public MeshRenderer Ground;
            public float Origin;
            public readonly List<Coin> Coins;
            public readonly List<Hazard> Hazards;
            public readonly List<PowerOrb> Powers;
            public readonly List<GameObject> Props;

            public Chunk(GameObject root, MeshRenderer ground)
            {
                Root = root;
                Ground = ground;
                Origin = 0f;
                Coins = new List<Coin>(16);
                Hazards = new List<Hazard>(8);
                Powers = new List<PowerOrb>(2);
                Props = new List<GameObject>(6);
            }
        }

        static readonly string[] Easy =
        {
            "c.c|...|c.c|...",
            "..c|.c.|c..|.c.",
            "c..|...|..c|c.c",
            "...|c.c|...|.c."
        };

        static readonly string[] Mid =
        {
            "..c|.b.|c.c|...",
            "c..|h..|..c|.c.",
            ".c.|..b|c..|h..",
            "c.c|b..|..c|.m.",
            "...|x..|c.c|.b."
        };

        static readonly string[] Hard =
        {
            "c.c|x.b|..c|.h.",
            ".c.|b.x|c..|h..",
            "c..|.x.|b.h|..c",
            "..c|h.b|x..|c.c",
            ".b.|c.s|x.h|.c.",
            "x.c|h.b|.p.|c.h",
            "c.x|b.h|..m|.b."
        };

        Chunk[] chunks;
        GameObject[] clouds;
        Pool coins;
        Pool hazards;
        Pool powers;
        Pool props;
        Material material;
        MaterialPropertyBlock block;
        float farthest;
        int chunkSerial;
        int runTier;
        BiomeLook painted;

        public void Build(Material source, Transform parent)
        {
            material = source;
            block = new MaterialPropertyBlock();
            coins = new Pool(parent, CreateCoin);
            hazards = new Pool(parent, CreateHazard);
            powers = new Pool(parent, CreatePower);
            props = new Pool(parent, CreateProp);
            chunks = new Chunk[Tune.ChunkCount];
            for (int i = 0; i < chunks.Length; i++)
            {
                var root = new GameObject("Chunk" + i);
                root.transform.SetParent(parent, false);
                GameObject ground = MeshLibrary.Create("Ground", MeshLibrary.Ground(), material, root.transform);
                chunks[i] = new Chunk(root, ground.GetComponent<MeshRenderer>());
            }
            clouds = new GameObject[7];
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i] = MeshLibrary.Create("Cloud", MeshLibrary.Cloud(), material, parent);
                clouds[i].transform.localScale = Vector3.one * Random.Range(1.4f, 2.6f);
            }
        }

        public void ResetRun(BiomeLook biome)
        {
            chunkSerial = 0;
            farthest = 0f;
            runTier = 0;
            painted = biome;
            for (int i = 0; i < chunks.Length; i++)
                Place(i, i * Tune.SegmentLength, true);
            LayoutClouds(0f);
        }

        public void Tick(float playerZ, float dt, int tier, BiomeLook biome, PlayerController player)
        {
            runTier = tier;
            painted = biome;
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].Origin + Tune.SegmentLength < playerZ - 18f)
                    Place(i, farthest, false);
                PaintGround(chunks[i].Ground, biome);
                TickContents(chunks[i], dt, player);
            }
            DriftClouds(playerZ, dt, biome);
        }

        public void ClearAhead(float playerZ, float distance)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                List<Hazard> list = chunks[i].Hazards;
                for (int h = 0; h < list.Count; h++)
                {
                    Hazard hazard = list[h];
                    if (hazard != null && hazard.transform.position.z < playerZ + distance)
                        hazard.DisableHit();
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (chunks == null)
                return;
            for (int i = 0; i < chunks.Length; i++)
                chunks[i].Root.SetActive(visible);
            for (int i = 0; i < clouds.Length; i++)
                clouds[i].SetActive(visible);
        }

        void Place(int index, float origin, bool opening)
        {
            Release(chunks[index]);
            chunks[index].Origin = origin;
            chunks[index].Root.transform.position = new Vector3(0f, 0f, origin);
            farthest = Mathf.Max(farthest, origin + Tune.SegmentLength);
            string pattern = ChoosePattern(opening);
            SpawnPattern(chunks[index], pattern);
            SpawnProps(chunks[index]);
            chunkSerial++;
        }

        string ChoosePattern(bool opening)
        {
            if (opening && chunkSerial < 2)
                return Easy[chunkSerial % Easy.Length];
            int tier = Mathf.Max(runTier, chunkSerial < 4 ? 0 : chunkSerial < 10 ? 1 : 2);
            if (tier <= 0)
                return Easy[Random.Range(0, Easy.Length)];
            if (tier == 1)
                return Random.value < 0.45f ? Easy[Random.Range(0, Easy.Length)] : Mid[Random.Range(0, Mid.Length)];
            float roll = Random.value;
            if (roll < 0.2f)
                return Mid[Random.Range(0, Mid.Length)];
            return Hard[Random.Range(0, Hard.Length)];
        }

        void SpawnPattern(Chunk chunk, string pattern)
        {
            string[] rows = pattern.Split('|');
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                if (row.Length < 3)
                    continue;
                char left = row[0];
                char center = row[1];
                char right = row[2];
                if (left == 'x' && center == 'x' && right == 'x')
                    center = '.';
                float z = 8f + r * 7.5f;
                SpawnCell(chunk, -1, left, z);
                SpawnCell(chunk, 0, center, z);
                SpawnCell(chunk, 1, right, z);
            }
        }

        void SpawnCell(Chunk chunk, int lane, char cell, float localZ)
        {
            float x = lane * Tune.LaneWidth;
            float z = chunk.Origin + localZ;
            switch (cell)
            {
                case 'c':
                    SpawnCoin(chunk, x, z);
                    break;
                case 'b':
                    SpawnHazard(chunk, x, z, new Vector3(1.25f, 0.85f, 0.7f), new Vector3(0f, 0.425f, 0f), Catalog.Hex("E07A5F"));
                    break;
                case 'h':
                    SpawnHazard(chunk, x, z, new Vector3(1.45f, 0.5f, 0.45f), new Vector3(0f, 1.78f, 0f), Catalog.Hex("3D405B"));
                    break;
                case 'x':
                    SpawnHazard(chunk, x, z, new Vector3(1.25f, 2.15f, 0.8f), new Vector3(0f, 1.05f, 0f), Catalog.Hex("6D597A"));
                    break;
                case 's':
                    SpawnPower(chunk, x, z, PowerKind.Shield);
                    break;
                case 'm':
                    SpawnPower(chunk, x, z, PowerKind.Magnet);
                    break;
                case 'p':
                    SpawnPower(chunk, x, z, PowerKind.Boost);
                    break;
            }
        }

        void SpawnCoin(Chunk chunk, float x, float z)
        {
            GameObject go = coins.Get();
            go.transform.SetParent(chunk.Root.transform, true);
            go.transform.position = new Vector3(x, 1.05f, z);
            go.transform.localScale = Vector3.one * 0.7f;
            Coin coin = go.GetComponent<Coin>();
            coin.Prepare(1.05f);
            Tint(go, Catalog.Hex("F5C542"));
            chunk.Coins.Add(coin);
        }

        void SpawnHazard(Chunk chunk, float x, float z, Vector3 scale, Vector3 center, Color color)
        {
            GameObject go = hazards.Get();
            go.transform.SetParent(chunk.Root.transform, true);
            go.transform.position = new Vector3(x, 0f, z);
            go.transform.localScale = Vector3.one;
            Transform model = go.transform.GetChild(0);
            model.localScale = scale;
            model.localPosition = center;
            BoxCollider box = go.GetComponent<BoxCollider>();
            box.size = scale;
            box.center = center;
            Hazard hazard = go.GetComponent<Hazard>();
            hazard.Prepare();
            Tint(model.gameObject, color);
            chunk.Hazards.Add(hazard);
        }

        void SpawnPower(Chunk chunk, float x, float z, PowerKind kind)
        {
            GameObject go = powers.Get();
            go.transform.SetParent(chunk.Root.transform, true);
            go.transform.position = new Vector3(x, 1.05f, z);
            go.transform.localScale = Vector3.one * 0.55f;
            PowerOrb orb = go.GetComponent<PowerOrb>();
            orb.Prepare(kind);
            Color color = kind == PowerKind.Shield ? Catalog.Hex("7AD1FF") : kind == PowerKind.Magnet ? Catalog.Hex("F5C542") : Catalog.Hex("FF6FAE");
            Tint(go, color);
            chunk.Powers.Add(orb);
        }

        void SpawnProps(Chunk chunk)
        {
            int count = Random.Range(3, 6);
            for (int i = 0; i < count; i++)
            {
                GameObject go = props.Get();
                go.transform.SetParent(chunk.Root.transform, true);
                float side = Random.value < 0.5f ? -1f : 1f;
                float x = side * Random.Range(5.6f, 7.4f);
                float z = chunk.Origin + Random.Range(2f, Tune.SegmentLength - 2f);
                int kind = Random.Range(0, 3);
                Transform model = go.transform.GetChild(0);
                MeshFilter filter = model.GetComponent<MeshFilter>();
                if (kind == 0)
                {
                    filter.sharedMesh = MeshLibrary.Cone();
                    model.localScale = new Vector3(1.1f, Random.Range(1.6f, 2.8f), 1.1f);
                    Tint(model.gameObject, painted.PropB);
                }
                else if (kind == 1)
                {
                    filter.sharedMesh = MeshLibrary.Crystal();
                    model.localScale = Vector3.one * Random.Range(1.1f, 1.9f);
                    Tint(model.gameObject, painted.PropA);
                }
                else
                {
                    filter.sharedMesh = MeshLibrary.Rock();
                    model.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
                    Tint(model.gameObject, painted.FogShadow);
                }
                model.localPosition = new Vector3(0f, model.localScale.y * 0.35f, 0f);
                go.transform.position = new Vector3(x, 0f, z);
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                chunk.Props.Add(go);
            }
        }

        void TickContents(Chunk chunk, float dt, PlayerController player)
        {
            bool magnet = player != null && player.MagnetActive && !player.Dead;
            Vector3 magnetPoint = player != null ? player.CollectCenter : Vector3.zero;
            for (int i = 0; i < chunk.Coins.Count; i++)
            {
                Coin coin = chunk.Coins[i];
                if (coin == null || coin.Taken)
                    continue;
                if (coin.Tick(dt, magnet, magnetPoint) && player != null)
                    player.CollectCoin(coin);
            }
            for (int i = 0; i < chunk.Powers.Count; i++)
            {
                if (chunk.Powers[i] != null)
                    chunk.Powers[i].Tick(dt);
            }
        }

        void Release(Chunk chunk)
        {
            for (int i = 0; i < chunk.Coins.Count; i++)
                coins.Release(chunk.Coins[i].gameObject);
            for (int i = 0; i < chunk.Hazards.Count; i++)
                hazards.Release(chunk.Hazards[i].gameObject);
            for (int i = 0; i < chunk.Powers.Count; i++)
                powers.Release(chunk.Powers[i].gameObject);
            for (int i = 0; i < chunk.Props.Count; i++)
                props.Release(chunk.Props[i]);
            chunk.Coins.Clear();
            chunk.Hazards.Clear();
            chunk.Powers.Clear();
            chunk.Props.Clear();
        }

        void LayoutClouds(float playerZ)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * Random.Range(4f, 10f);
                float z = playerZ + i * 18f;
                clouds[i].transform.position = new Vector3(x, Random.Range(6.5f, 9.5f), z);
                Tint(clouds[i], Color.white);
            }
        }

        void DriftClouds(float playerZ, float dt, BiomeLook biome)
        {
            Color cloud = Color.Lerp(Color.white, biome.Sky, 0.25f);
            for (int i = 0; i < clouds.Length; i++)
            {
                Vector3 pos = clouds[i].transform.position;
                pos.z -= dt * 1.5f;
                if (pos.z < playerZ - 20f)
                    pos.z = playerZ + 90f + i * 4f;
                clouds[i].transform.position = pos;
                Tint(clouds[i], cloud);
            }
        }

        void PaintGround(MeshRenderer renderer, BiomeLook biome)
        {
            MeshLibrary.Tint(renderer, biome.Ground, block);
        }

        void Tint(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null && go.transform.childCount > 0)
                renderer = go.transform.GetChild(0).GetComponent<Renderer>();
            MeshLibrary.Tint(renderer, color, block);
        }

        GameObject CreateCoin()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Coin");
            GameObject go = prefab != null ? Instantiate(prefab) : NewCoin();
            EnsureRenderer(go, MeshLibrary.Coin());
            if (go.GetComponent<Coin>() == null)
                go.AddComponent<Coin>();
            if (go.GetComponent<SphereCollider>() == null)
            {
                SphereCollider sphere = go.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.55f;
            }
            EnsureKinematic(go);
            go.SetActive(false);
            return go;
        }

        GameObject CreateHazard()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Hazard");
            GameObject go = prefab != null ? Instantiate(prefab) : NewHazard();
            Transform model = go.transform.childCount > 0 ? go.transform.GetChild(0) : MeshLibrary.Create("Model", MeshLibrary.Box(), material, go.transform).transform;
            EnsureRenderer(model.gameObject, MeshLibrary.Box());
            if (go.GetComponent<Hazard>() == null)
                go.AddComponent<Hazard>();
            if (go.GetComponent<BoxCollider>() == null)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
            }
            EnsureKinematic(go);
            go.SetActive(false);
            return go;
        }

        GameObject CreatePower()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/PowerOrb");
            GameObject go = prefab != null ? Instantiate(prefab) : NewPower();
            EnsureRenderer(go, MeshLibrary.Crystal());
            if (go.GetComponent<PowerOrb>() == null)
                go.AddComponent<PowerOrb>();
            if (go.GetComponent<SphereCollider>() == null)
            {
                SphereCollider sphere = go.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.7f;
            }
            EnsureKinematic(go);
            go.SetActive(false);
            return go;
        }

        GameObject CreateProp()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Prop");
            GameObject go = prefab != null ? Instantiate(prefab) : NewProp();
            Transform model = go.transform.childCount > 0 ? go.transform.GetChild(0) : MeshLibrary.Create("Model", MeshLibrary.Cone(), material, go.transform).transform;
            EnsureRenderer(model.gameObject, MeshLibrary.Cone());
            go.SetActive(false);
            return go;
        }

        void EnsureRenderer(GameObject go, Mesh mesh)
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null)
                filter = go.AddComponent<MeshFilter>();
            if (filter.sharedMesh == null)
                filter.sharedMesh = mesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = go.AddComponent<MeshRenderer>();
            if (renderer.sharedMaterial == null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static void EnsureKinematic(GameObject go)
        {
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body == null)
                body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        GameObject NewCoin()
        {
            GameObject go = MeshLibrary.Create("Coin", MeshLibrary.Coin(), material, transform);
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.55f;
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            go.AddComponent<Coin>();
            return go;
        }

        GameObject NewHazard()
        {
            var go = new GameObject("Hazard");
            go.transform.SetParent(transform, false);
            MeshLibrary.Create("Model", MeshLibrary.Box(), material, go.transform);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            go.AddComponent<Hazard>();
            return go;
        }

        GameObject NewPower()
        {
            GameObject go = MeshLibrary.Create("PowerOrb", MeshLibrary.Crystal(), material, transform);
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.7f;
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            go.AddComponent<PowerOrb>();
            return go;
        }

        GameObject NewProp()
        {
            var go = new GameObject("Prop");
            go.transform.SetParent(transform, false);
            MeshLibrary.Create("Model", MeshLibrary.Cone(), material, go.transform);
            return go;
        }

        sealed class Pool
        {
            readonly Stack<GameObject> stack = new Stack<GameObject>();
            readonly Transform parent;
            readonly System.Func<GameObject> factory;

            public Pool(Transform parent, System.Func<GameObject> factory)
            {
                this.parent = parent;
                this.factory = factory;
            }

            public GameObject Get()
            {
                GameObject go = stack.Count > 0 ? stack.Pop() : factory();
                go.SetActive(true);
                return go;
            }

            public void Release(GameObject go)
            {
                if (go == null)
                    return;
                go.SetActive(false);
                go.transform.SetParent(parent, false);
                stack.Push(go);
            }
        }
    }
}
