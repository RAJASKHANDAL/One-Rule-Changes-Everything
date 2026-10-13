using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Rand = System.Random;

/// <summary>One-click builder: materials, meshes, prefabs, rule assets and the complete game scene.</summary>
public static class GameBuilder
{
    const string Root = "Assets/_Game";
    const string ScenePath = Root + "/Scenes/Game.unity";

    static readonly Dictionary<string, Material> M = new Dictionary<string, Material>();
    static readonly Dictionary<string, GameObject> P = new Dictionary<string, GameObject>();
    static readonly List<Vector3> keep = new List<Vector3>(); // x, z, radius
    static Rand rng;

    // ------------------------------------------------------------------ layout data

    static readonly Vector2[] PlatformPos = { new Vector2(0, 0), new Vector2(6, 2), new Vector2(-6, -2), new Vector2(2.5f, -5.5f), new Vector2(-2.5f, 5.5f), new Vector2(9.5f, -4), new Vector2(-9, 3.5f), new Vector2(6, 8), new Vector2(-6, -8) };
    static readonly Vector2[] RedTilePos = { new Vector2(3.5f, -1.5f), new Vector2(-4, 2.5f), new Vector2(-1, -4.5f), new Vector2(9.5f, 6.5f), new Vector2(-10, -6.5f), new Vector2(0.5f, 9.5f), new Vector2(5.5f, -10), new Vector2(11, 1.5f) };
    static readonly Vector2[] PlateauPos = { new Vector2(10.5f, -10), new Vector2(-8.5f, 10.5f), new Vector2(-11, -1.5f) };
    static readonly Vector2[] FloorCoins = { new Vector2(-2.5f, -2.5f), new Vector2(5.2f, -3.9f), new Vector2(4.8f, 5.6f), new Vector2(9, 4), new Vector2(-4.5f, -5.5f), new Vector2(-8, 0), new Vector2(-6.5f, 6.5f), new Vector2(1, 6.8f), new Vector2(-1, 12), new Vector2(8, -7.5f), new Vector2(0, -8.5f), new Vector2(5.5f, -1) };
    static readonly Vector2 SpirePos = new Vector2(10, 10);
    static readonly Vector2 PillarPos = new Vector2(-8.5f, -12.5f);
    static readonly Vector2 Bridge1 = new Vector2(3.5f, 12);
    static readonly Vector2 Bridge2 = new Vector2(-1, -11.5f);

    [MenuItem("Tools/One Rule/Build Game Scene")]
    public static void Build()
    {
        rng = new Rand(20240611);
        M.Clear(); P.Clear(); keep.Clear();
        // Generated geometry is rebuilt from scratch so prefab -> mesh references can never go stale.
        AssetDatabase.DeleteAsset(Root + "/Prefabs");
        AssetDatabase.DeleteAsset(Root + "/Meshes");
        AssetDatabase.Refresh();
        EnsureFolders();
        BuildMaterials();
        BuildPrefabs();
        var defs = BuildRuleDefinitions();
        BuildScene(defs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("One Rule Changes Everything: scene built at " + ScenePath);
    }

    static void EnsureFolders()
    {
        foreach (var f in new[] { "Scenes", "Prefabs", "Materials", "ScriptableObjects", "Meshes", "VFX", "Audio" })
            if (!AssetDatabase.IsValidFolder(Root + "/" + f)) AssetDatabase.CreateFolder(Root, f);
    }

    // ------------------------------------------------------------------ materials

    static Material Mat(string name, Color c, float smooth = 0.18f, float metallic = 0f, Color? emission = null, string shader = "Universal Render Pipeline/Lit")
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find(shader);
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else if (m.HasProperty("_EmissionColor"))
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        M[name] = m;
        return m;
    }

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static void BuildMaterials()
    {
        Mat("BlueRock", C(0.17f, 0.25f, 0.66f), 0.12f);
        Mat("VioletRock", C(0.38f, 0.2f, 0.7f), 0.12f);
        Mat("LightRock", C(0.26f, 0.38f, 0.82f), 0.12f);
        Mat("PurpleGround", C(0.31f, 0.2f, 0.52f), 0.08f);
        Mat("PurpleGround2", C(0.33f, 0.215f, 0.54f), 0.08f);
        Mat("PurpleGround3", C(0.29f, 0.19f, 0.5f), 0.08f);
        Mat("PurpleGround4", C(0.345f, 0.22f, 0.5f), 0.08f);
        Mat("CrackDark", C(0.22f, 0.13f, 0.38f), 0.05f);
        Mat("IndigoRock", C(0.22f, 0.17f, 0.56f), 0.12f);
        Mat("MagentaCrystal", C(0.86f, 0.2f, 0.92f), 0.7f, 0.1f, C(0.9f, 0.15f, 0.95f));
        Mat("MauveGround", C(0.5f, 0.35f, 0.58f), 0.08f);
        Mat("OuterGround", C(0.16f, 0.1f, 0.3f), 0.05f);
        Mat("CyanGrass", C(0.1f, 0.78f, 0.88f), 0.25f, 0f, C(0.02f, 0.2f, 0.25f));
        Mat("PinkCrystal", C(1f, 0.3f, 0.78f), 0.7f, 0.1f, C(1.0f, 0.2f, 0.7f));
        Mat("OrangeWood", C(0.95f, 0.5f, 0.14f), 0.15f);
        Mat("DarkWood", C(0.62f, 0.3f, 0.1f), 0.12f);
        Mat("SafeTile", C(0.2f, 0.85f, 1f), 0.5f, 0f, C(0.15f, 1.1f, 1.5f));
        Mat("PlatformBase", C(0.14f, 0.22f, 0.58f), 0.15f);
        Mat("HazardDormant", C(0.42f, 0.1f, 0.22f), 0.1f, 0f, C(0.1f, 0.0f, 0.02f));
        Mat("HazardBorder", C(0.8f, 0.12f, 0.2f), 0.2f, 0f, C(0.7f, 0.05f, 0.1f));
        Mat("TileDark", C(0.2f, 0.07f, 0.16f), 0.1f);
        Mat("HazardWarning", C(0.95f, 0.4f, 0.15f), 0.2f, 0f, C(1.4f, 0.45f, 0.1f));
        Mat("HazardActive", C(1f, 0.08f, 0.14f), 0.3f, 0f, C(2.2f, 0.12f, 0.18f));
        Mat("LavaWarning", C(1f, 0.45f, 0.12f), 0.3f, 0f, C(1.6f, 0.5f, 0.1f));
        Mat("LavaActive", C(1f, 0.2f, 0.06f), 0.4f, 0f, C(2.4f, 0.55f, 0.08f));
        Mat("Gold", C(1f, 0.8f, 0.2f), 0.7f, 0.6f, C(0.5f, 0.35f, 0.04f));
        Mat("PlayerOutfit", C(0.13f, 0.14f, 0.26f), 0.15f);
        Mat("PlayerHood", C(0.2f, 0.18f, 0.42f), 0.15f);
        Mat("PlayerSkin", C(0.98f, 0.78f, 0.62f), 0.2f);
        Mat("PlayerEye", C(0.05f, 0.04f, 0.1f), 0.5f);
        Mat("PlayerOrange", C(1f, 0.52f, 0.12f), 0.15f);
        var ceil = Mat("CeilingGlow", C(0.55f, 0.35f, 1f, 0.16f), 0f, 0f, null, "Universal Render Pipeline/Unlit");
        ceil.SetFloat("_Surface", 1f);
        ceil.SetFloat("_Blend", 0f);
        ceil.SetFloat("_SrcBlend", 5f);
        ceil.SetFloat("_DstBlend", 10f);
        ceil.SetFloat("_ZWrite", 0f);
        ceil.SetOverrideTag("RenderType", "Transparent");
        ceil.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ceil.renderQueue = (int)RenderQueue.Transparent;
        Mat("CoinSpark", C(1f, 0.85f, 0.3f), 0f, 0f, null, "Universal Render Pipeline/Particles/Unlit");
        Mat("DustSpark", C(0.85f, 0.8f, 1f, 0.8f), 0f, 0f, null, "Universal Render Pipeline/Particles/Unlit");
        Mat("PlayerPack", C(0.16f, 0.12f, 0.3f), 0.15f);
    }

    // ------------------------------------------------------------------ prefabs

    static GameObject Save(GameObject go, string name)
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        P[name] = prefab;
        return prefab;
    }

    static Mesh SaveMesh(Mesh mesh)
    {
        string path = Root + "/Meshes/" + mesh.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        existing.Clear();
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static GameObject MeshObject(string name, Mesh mesh, Material mat, bool collider)
    {
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
        return go;
    }

    static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void BuildPrefabs()
    {
        // boulders & rock columns
        Save(MeshObject("Rock_A", SaveMesh(MeshFactory.Rock("Rock_A", 11)), M["BlueRock"], true), "Rock_A");
        Save(MeshObject("Rock_B", SaveMesh(MeshFactory.Rock("Rock_B", 23, 0.3f)), M["VioletRock"], true), "Rock_B");
        Save(MeshObject("Rock_C", SaveMesh(MeshFactory.Rock("Rock_C", 37, 0.26f)), M["LightRock"], true), "Rock_C");
        Save(MeshObject("Rock_Pillar_A", SaveMesh(MeshFactory.Pillar("Rock_Pillar_A", 5, 7, 0.55f)), M["BlueRock"], true), "Rock_Pillar_A");
        Save(MeshObject("Rock_Pillar_B", SaveMesh(MeshFactory.Pillar("Rock_Pillar_B", 9, 6, 0.4f)), M["VioletRock"], true), "Rock_Pillar_B");
        Save(MeshObject("Stone", SaveMesh(MeshFactory.Rock("Stone", 51, 0.3f)), M["LightRock"], false), "Stone");
        Save(MeshObject("Plateau", SaveMesh(MeshFactory.Plateau("Plateau", 3, 2f, 1.1f)), M["VioletRock"], true), "Plateau");

        // crystals & grass (decorative, no colliders)
        Save(MeshObject("Crystal_A", SaveMesh(MeshFactory.CrystalCluster("Crystal_A", 3, 5, 1.7f, 0.5f)), M["PinkCrystal"], false), "Crystal_A");
        Save(MeshObject("Crystal_B", SaveMesh(MeshFactory.CrystalCluster("Crystal_B", 8, 4, 1.1f, 0.35f)), M["PinkCrystal"], false), "Crystal_B");
        var spire = MeshObject("Crystal_Spire", SaveMesh(MeshFactory.CrystalCluster("Crystal_Spire", 14, 6, 4.2f, 1.1f)), M["PinkCrystal"], false);
        var spireCol = spire.AddComponent<CapsuleCollider>();
        spireCol.center = new Vector3(0f, 1.5f, 0f); spireCol.radius = 0.75f; spireCol.height = 3f;
        Save(spire, "Crystal_Spire");
        Save(MeshObject("Grass_Patch", SaveMesh(MeshFactory.Grass("Grass_Patch", 4, 9)), M["CyanGrass"], false), "Grass_Patch");

        BuildLadder(); BuildBarrier(); BuildBridge();
        BuildPlatform(); BuildTiles(); BuildCoin(); BuildPlayer();
    }

    static void BuildLadder()
    {
        var root = new GameObject("Ladder");
        var body = new GameObject("Body").transform;
        body.SetParent(root.transform, false);
        body.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        foreach (float x in new[] { -0.38f, 0.38f })
        {
            Prim(body, "Rail", PrimitiveType.Cube, new Vector3(x, 0.85f, 0f), new Vector3(0.12f, 1.7f, 0.14f), M["DarkWood"]);
            Prim(body, "Foot", PrimitiveType.Cube, new Vector3(x, 0.04f, 0.02f), new Vector3(0.17f, 0.08f, 0.2f), M["DarkWood"]);
        }
        for (int i = 0; i < 5; i++)
            Prim(body, "Rung" + i, PrimitiveType.Cube, new Vector3(0f, 0.3f + i * 0.3f, 0f), new Vector3(0.74f, 0.09f, 0.1f), M["OrangeWood"]);
        Save(root, "Ladder");
    }

    static void BuildBarrier()
    {
        var root = new GameObject("Barrier");
        foreach (float x in new[] { -1.05f, 0f, 1.05f })
        {
            Prim(root.transform, "Post", PrimitiveType.Cube, new Vector3(x, 0.45f, 0f), new Vector3(0.18f, 0.9f, 0.18f), M["DarkWood"]);
            Prim(root.transform, "Cap", PrimitiveType.Cube, new Vector3(x, 0.93f, 0f), new Vector3(0.25f, 0.07f, 0.25f), M["OrangeWood"]);
        }
        foreach (float y in new[] { 0.35f, 0.68f })
            Prim(root.transform, "Rail", PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(2.3f, 0.14f, 0.1f), M["OrangeWood"]);
        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.45f, 0f); box.size = new Vector3(2.4f, 0.9f, 0.3f);
        Save(root, "Barrier");
    }

    static void BuildBridge()
    {
        var root = new GameObject("Bridge");
        for (int i = 0; i < 10; i++)
            Prim(root.transform, "Plank" + i, PrimitiveType.Cube, new Vector3(-1.8f + i * 0.4f, 0.35f, 0f), new Vector3(0.36f, 0.2f, 2.1f), i % 2 == 0 ? M["OrangeWood"] : M["DarkWood"]);
        // stringers and support posts so the deck reads as built, not floating
        foreach (float z in new[] { -0.9f, 0.9f })
        {
            Prim(root.transform, "Stringer", PrimitiveType.Cube, new Vector3(0f, 0.2f, z), new Vector3(4.2f, 0.12f, 0.16f), M["DarkWood"]);
            foreach (float x in new[] { -1.7f, 1.7f })
                Prim(root.transform, "Support", PrimitiveType.Cube, new Vector3(x, 0.14f, z), new Vector3(0.22f, 0.3f, 0.22f), M["DarkWood"]);
        }
        var deck = root.AddComponent<BoxCollider>();
        deck.center = new Vector3(0f, 0.35f, 0f); deck.size = new Vector3(4f, 0.2f, 2.2f);
        foreach (int s in new[] { -1, 1 })
        {
            var ramp = Prim(root.transform, "Ramp", PrimitiveType.Cube, new Vector3(s * 2.42f, 0.22f, 0f), new Vector3(0.95f, 0.14f, 2.1f), M["OrangeWood"], true);
            ramp.transform.localRotation = Quaternion.Euler(0f, 0f, -s * 25f);
            foreach (float z in new[] { -1.05f, 1.05f })
            {
                Prim(root.transform, "RailPost", PrimitiveType.Cube, new Vector3(s * 1.9f, 0.8f, z), new Vector3(0.14f, 0.9f, 0.14f), M["DarkWood"]);
                Prim(root.transform, "PostCap", PrimitiveType.Cube, new Vector3(s * 1.9f, 1.28f, z), new Vector3(0.2f, 0.06f, 0.2f), M["OrangeWood"]);
            }
        }
        foreach (float z in new[] { -1.05f, 1.05f })
        {
            Prim(root.transform, "Rail", PrimitiveType.Cube, new Vector3(0f, 1.1f, z), new Vector3(3.8f, 0.1f, 0.1f), M["OrangeWood"]);
            Prim(root.transform, "RailLow", PrimitiveType.Cube, new Vector3(0f, 0.75f, z), new Vector3(3.8f, 0.08f, 0.08f), M["DarkWood"]);
        }
        Save(root, "Bridge");
    }

    static void BuildPlatform()
    {
        var root = new GameObject("SafePlatform");
        Prim(root.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(3f, 0.5f, 3f), M["PlatformBase"]);
        Prim(root.transform, "Chamfer", PrimitiveType.Cube, new Vector3(0f, 0.56f, 0f), new Vector3(2.86f, 0.12f, 2.86f), M["LightRock"]);
        Prim(root.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 0.68f, 0f), new Vector3(2.68f, 0.06f, 2.68f), M["SafeTile"]);
        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.35f, 0f); box.size = new Vector3(3f, 0.7f, 3f);
        Save(root, "SafePlatform");
    }

    static void BuildTiles()
    {
        var safe = new GameObject("SafeTile");
        Prim(safe.transform, "Frame", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(2.2f, 0.06f, 2.2f), M["PlatformBase"]);
        Prim(safe.transform, "Mesh", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(1.9f, 0.08f, 1.9f), M["SafeTile"]);
        Save(safe, "SafeTile");

        var red = new GameObject("RedTile");
        Prim(red.transform, "Frame", PrimitiveType.Cube, new Vector3(0f, 0.035f, 0f), new Vector3(2.5f, 0.07f, 2.5f), M["TileDark"]);
        var mesh = Prim(red.transform, "Mesh", PrimitiveType.Cube, new Vector3(0f, 0.045f, 0f), new Vector3(2.2f, 0.09f, 2.2f), M["HazardDormant"]);
        foreach (float z in new[] { -1.12f, 1.12f })
            Prim(red.transform, "BorderZ", PrimitiveType.Cube, new Vector3(0f, 0.09f, z), new Vector3(2.3f, 0.04f, 0.09f), M["HazardBorder"]);
        foreach (float x in new[] { -1.12f, 1.12f })
            Prim(red.transform, "BorderX", PrimitiveType.Cube, new Vector3(x, 0.09f, 0f), new Vector3(0.09f, 0.04f, 2.3f), M["HazardBorder"]);
        foreach (float a in new[] { 45f, -45f })
        {
            var bar = Prim(red.transform, "Mark", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(1.0f, 0.03f, 0.14f), M["HazardBorder"]);
            bar.transform.localRotation = Quaternion.Euler(0f, a, 0f);
        }
        var trigger = red.AddComponent<BoxCollider>();
        trigger.isTrigger = true; trigger.center = new Vector3(0f, 0.45f, 0f); trigger.size = new Vector3(2.3f, 0.9f, 2.3f);
        var tile = red.AddComponent<HazardTile>();
        tile.tileRenderer = mesh.GetComponent<Renderer>();
        tile.warningMaterial = M["HazardWarning"];
        tile.activeMaterial = M["HazardActive"];
        Save(red, "RedTile");
    }

    static void BuildCoin()
    {
        var root = new GameObject("Coin");
        var mesh = Prim(root.transform, "Mesh", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.6f, 0.05f, 0.6f), M["Gold"]);
        mesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var rim = Prim(root.transform, "Inner", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.4f, 0.065f, 0.4f), M["OrangeWood"]);
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rim.GetComponent<Renderer>().sharedMaterial = M["Gold"];
        var sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 0.6f;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        root.AddComponent<Coin>();
        Save(root, "Coin");
    }

    static void BuildPlayer()
    {
        var root = new GameObject("Player");
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 1f; rb.useGravity = false; rb.freezeRotation = true;
        var cap = root.AddComponent<CapsuleCollider>();
        cap.center = Vector3.zero; cap.height = 2.0f; cap.radius = 0.5f;
        string pmPath = Root + "/Materials/PlayerNoFriction.asset";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (pm == null) { pm = new PhysicsMaterial("PlayerNoFriction"); AssetDatabase.CreateAsset(pm, pmPath); }
        pm.dynamicFriction = 0f; pm.staticFriction = 0f; pm.bounciness = 0f;
        pm.frictionCombine = PhysicsMaterialCombine.Minimum; pm.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(pm);
        cap.sharedMaterial = pm;

        root.AddComponent<PlayerGravityController>();
        root.AddComponent<PlayerHealth>();
        var pc = root.AddComponent<PlayerController>();
        pc.spawnPosition = new Vector3(0f, 1.75f, 0f);
        pc.jumpHeight = 2.3f;

        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        var v = vis.AddComponent<PlayerVisual>();
        v.baseScale = 1.25f;
        var t = vis.transform;

        Prim(t, "Body", PrimitiveType.Capsule, new Vector3(0f, -0.1f, 0f), new Vector3(0.58f, 0.3f, 0.48f), M["PlayerOutfit"]);
        Prim(t, "Chest", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.2f), new Vector3(0.22f, 0.24f, 0.05f), M["PlayerHood"]);
        Prim(t, "Belt", PrimitiveType.Cylinder, new Vector3(0f, -0.21f, 0f), new Vector3(0.62f, 0.04f, 0.52f), M["PlayerOrange"]);
        Prim(t, "Buckle", PrimitiveType.Cube, new Vector3(0f, -0.21f, 0.26f), new Vector3(0.1f, 0.09f, 0.04f), M["Gold"]);
        Prim(t, "Pack", PrimitiveType.Cube, new Vector3(0f, -0.04f, -0.3f), new Vector3(0.38f, 0.42f, 0.18f), M["PlayerPack"]);
        Prim(t, "PackStrap", PrimitiveType.Cube, new Vector3(0f, 0.0f, -0.4f), new Vector3(0.4f, 0.07f, 0.05f), M["PlayerOrange"]);
        Prim(t, "Hood", PrimitiveType.Sphere, new Vector3(0f, 0.43f, -0.03f), new Vector3(0.74f, 0.7f, 0.72f), M["PlayerHood"]);
        Prim(t, "HoodTip", PrimitiveType.Sphere, new Vector3(0f, 0.68f, -0.26f), new Vector3(0.24f, 0.24f, 0.3f), M["PlayerHood"]);
        Prim(t, "Face", PrimitiveType.Sphere, new Vector3(0f, 0.4f, 0.2f), new Vector3(0.5f, 0.44f, 0.32f), M["PlayerSkin"]);
        foreach (float x in new[] { -0.12f, 0.12f })
            Prim(t, "Eye", PrimitiveType.Sphere, new Vector3(x, 0.44f, 0.355f), new Vector3(0.08f, 0.1f, 0.05f), M["PlayerEye"]);
        Prim(t, "Scarf", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.56f, 0.06f, 0.48f), M["PlayerOrange"]);
        Prim(t, "ScarfKnot", PrimitiveType.Sphere, new Vector3(0.14f, 0.1f, 0.2f), new Vector3(0.16f, 0.14f, 0.14f), M["PlayerOrange"]);

        var tail = new GameObject("ScarfTail").transform;
        tail.SetParent(t, false); tail.localPosition = new Vector3(0f, 0.1f, -0.2f);
        Prim(tail, "Tail", PrimitiveType.Cube, new Vector3(0f, 0f, -0.3f), new Vector3(0.2f, 0.06f, 0.6f), M["PlayerOrange"]);

        Transform Limb(string name, float x, float y, Vector3 meshPos, Vector3 meshScale, Material mat, Vector3? tipPos = null, Material tipMat = null, Vector3? tipScale = null)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(t, false); pivot.localPosition = new Vector3(x, y, 0f);
            Prim(pivot, "Mesh", PrimitiveType.Cube, meshPos, meshScale, mat);
            if (tipPos.HasValue) Prim(pivot, "Tip", PrimitiveType.Cube, tipPos.Value, tipScale.Value, tipMat);
            return pivot;
        }
        var armL = Limb("ArmL", -0.39f, 0.05f, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.4f, 0.16f), M["PlayerOutfit"], new Vector3(0f, -0.43f, 0f), M["PlayerSkin"], new Vector3(0.14f, 0.11f, 0.14f));
        var armR = Limb("ArmR", 0.39f, 0.05f, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.4f, 0.16f), M["PlayerOutfit"], new Vector3(0f, -0.43f, 0f), M["PlayerSkin"], new Vector3(0.14f, 0.11f, 0.14f));
        var legL = Limb("LegL", -0.16f, -0.38f, new Vector3(0f, -0.18f, 0f), new Vector3(0.21f, 0.36f, 0.24f), M["PlayerOutfit"], new Vector3(0f, -0.38f, 0.04f), M["PlayerOrange"], new Vector3(0.23f, 0.1f, 0.32f));
        var legR = Limb("LegR", 0.16f, -0.38f, new Vector3(0f, -0.18f, 0f), new Vector3(0.21f, 0.36f, 0.24f), M["PlayerOutfit"], new Vector3(0f, -0.38f, 0.04f), M["PlayerOrange"], new Vector3(0.23f, 0.1f, 0.32f));

        // landing dust
        var dustGo = new GameObject("Dust");
        dustGo.transform.SetParent(root.transform, false);
        var dust = dustGo.AddComponent<ParticleSystem>();
        var dm = dust.main;
        dm.playOnAwake = false; dm.loop = false; dm.duration = 1f;
        dm.startLifetime = 0.4f; dm.startSpeed = 2.2f; dm.startSize = 0.22f; dm.gravityModifier = 0f;
        dm.startColor = new Color(0.85f, 0.8f, 1f, 0.7f);
        dm.simulationSpace = ParticleSystemSimulationSpace.World;
        var de = dust.emission; de.enabled = false;
        var ds = dust.shape; ds.shapeType = ParticleSystemShapeType.Circle; ds.radius = 0.35f;
        ds.rotation = new Vector3(90f, 0f, 0f);
        var dcol = dust.colorOverLifetime; dcol.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        dcol.color = grad;
        dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = M["DustSpark"];
        v.dust = dust;

        v.armL = armL; v.armR = armR; v.legL = legL; v.legR = legR; v.scarfTail = tail;
        var rends = new List<Renderer>();
        foreach (var r in vis.GetComponentsInChildren<Renderer>()) if (!(r is ParticleSystemRenderer)) rends.Add(r);
        v.renderers = rends.ToArray();
        Save(root, "Player");
    }

    // ------------------------------------------------------------------ rule definitions

    static RuleDefinition Def(string file, string id, string name, string sub, string desc, string glyph, Color accent, float duration, float warn, int diff, float minTime, float cooldown, params string[] incompatible)
    {
        string path = Root + "/ScriptableObjects/" + file + ".asset";
        var d = AssetDatabase.LoadAssetAtPath<RuleDefinition>(path);
        if (d == null) { d = ScriptableObject.CreateInstance<RuleDefinition>(); AssetDatabase.CreateAsset(d, path); }
        d.id = id; d.displayName = name; d.subtitle = sub; d.description = desc; d.glyph = glyph; d.accent = accent;
        d.duration = duration; d.warningDuration = warn; d.difficulty = diff; d.minSurvivalTime = minTime; d.cooldown = cooldown;
        d.incompatibleWith = incompatible;
        EditorUtility.SetDirty(d);
        return d;
    }

    static Dictionary<string, RuleDefinition> BuildRuleDefinitions()
    {
        var pink = C(1f, 0.3f, 0.68f); var red = C(1f, 0.25f, 0.3f); var cyan = C(0.25f, 0.88f, 1f);
        var orange = C(1f, 0.6f, 0.2f); var blue = C(0.4f, 0.68f, 1f); var violet = C(0.8f, 0.48f, 1f);
        return new Dictionary<string, RuleDefinition>
        {
            ["never_stop"] = Def("Rule_NeverStop", "never_stop", "NEVER STOP", "KEEP MOVING", "Keep moving. Standing still for too long will hurt you.", ">>", pink, 14f, 2.5f, 1, 0f, 30f),
            ["red_is_dead"] = Def("Rule_RedIsDead", "red_is_dead", "RED IS DEAD", "AVOID RED ZONES", "Avoid red surfaces.", "X", red, 16f, 2.5f, 1, 0f, 30f),
            ["gravity_flip"] = Def("Rule_GravityFlip", "gravity_flip", "GRAVITY FLIP", "GRAVITY REVERSES", "Gravity reverses. Adapt quickly.", "^v", cyan, 18f, 2.5f, 3, 55f, 40f, "floor_is_lava", "keep_jumping"),
            ["floor_is_lava"] = Def("Rule_FloorIsLava", "floor_is_lava", "THE FLOOR IS LAVA", "ONLY SAFE PLATFORMS", "Only safe platforms can protect you.", "~", orange, 16f, 3f, 2, 40f, 35f, "gravity_flip"),
            ["keep_jumping"] = Def("Rule_KeepJumping", "keep_jumping", "KEEP JUMPING", "DON'T STAY GROUNDED", "Do not remain grounded too long.", "^", blue, 14f, 2.5f, 2, 30f, 30f, "gravity_flip"),
            ["controls_reversed"] = Def("Rule_ControlsReversed", "controls_reversed", "CONTROLS REVERSED", "MOVEMENT IS FLIPPED", "Movement directions change.", "<>", violet, 14f, 2.5f, 2, 25f, 30f),
        };
    }

    // ------------------------------------------------------------------ scene

    static GameObject Place(string prefab, Vector3 pos, float yaw, Vector3 scale, Transform parent, string material = null, bool collider = true)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(P[prefab], parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = scale;
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = M[material];
        if (!collider) foreach (var c in go.GetComponents<Collider>()) c.enabled = false;
        return go;
    }

    static void Keep(float x, float z, float r) => keep.Add(new Vector3(x, z, r));

    static bool Free(Vector2 p, float r)
    {
        foreach (var k in keep) if (Vector2.Distance(new Vector2(k.x, k.y), p) < k.z + r) return false;
        return true;
    }

    static bool Sample(float range, float r, out Vector2 p)
    {
        for (int i = 0; i < 80; i++)
        {
            p = new Vector2(((float)rng.NextDouble() * 2f - 1f) * range, ((float)rng.NextDouble() * 2f - 1f) * range);
            if (Free(p, r)) return true;
        }
        p = Vector2.zero;
        return false;
    }

    static float Rf(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    static Transform Group(string name, Transform parent = null)
    {
        var g = new GameObject(name).transform;
        if (parent != null) g.SetParent(parent, false);
        return g;
    }

    static void BuildScene(Dictionary<string, RuleDefinition> defs)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- reserve space so scatter never lands on gameplay objects
        Keep(0, 0, 2.6f);
        foreach (var p in PlatformPos) Keep(p.x, p.y, 2.5f);
        foreach (var p in RedTilePos) Keep(p.x, p.y, 2.1f);
        foreach (var p in PlateauPos) Keep(p.x, p.y, 3.2f);
        foreach (var p in FloorCoins) Keep(p.x, p.y, 1.2f);
        Keep(SpirePos.x, SpirePos.y, 3.2f);
        Keep(PillarPos.x, PillarPos.y, 3f);
        Keep(Bridge1.x, Bridge1.y, 3.4f);
        Keep(Bridge2.x, Bridge2.y, 3.4f);

        var env = Group("Environment");
        var floorGroup = Group("Floor", env);
        var perimeter = Group("Perimeter", env);
        var props = Group("Props", env);
        var platforms = Group("Platforms", env);
        var tiles = Group("Tiles", env);

        // ---- ground
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "ArenaFloor";
        floor.transform.SetParent(floorGroup, false);
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(30f, 1f, 30f);
        floor.GetComponent<Renderer>().sharedMaterial = M["PurpleGround"];
        floor.AddComponent<FloorSurface>();

        var outer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outer.name = "OuterGround";
        outer.transform.SetParent(floorGroup, false);
        outer.transform.position = new Vector3(0f, -1.1f, 0f);
        outer.transform.localScale = new Vector3(120f, 1f, 120f);
        outer.GetComponent<Renderer>().sharedMaterial = M["OuterGround"];
        Object.DestroyImmediate(outer.GetComponent<Collider>());

        var checker = Group("FloorTiles", floorGroup);
        string[] groundMats = { "PurpleGround", "PurpleGround2", "PurpleGround3", "PurpleGround4" };
        for (int ix = 0; ix < 7; ix++)
            for (int iz = 0; iz < 7; iz++)
            {
                var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
                t.name = "FloorTile";
                t.transform.SetParent(checker, false);
                t.transform.position = new Vector3(-12f + ix * 4f, 0.01f, -12f + iz * 4f);
                t.transform.localScale = new Vector3(4f, 0.02f, 4f);
                t.GetComponent<Renderer>().sharedMaterial = M[groundMats[rng.Next(groundMats.Length)]];
                Object.DestroyImmediate(t.GetComponent<Collider>());
            }
        // a few surface cracks
        for (int i = 0; i < 8; i++)
        {
            if (!Sample(12f, 0.8f, out var p)) continue;
            float yaw = Rf(0f, 180f);
            for (int seg = 0; seg < 2; seg++)
            {
                var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crack.name = "Crack";
                crack.transform.SetParent(checker, false);
                var off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, seg * 1.1f);
                crack.transform.SetPositionAndRotation(new Vector3(p.x + off.x, 0.032f, p.y + off.z), Quaternion.Euler(0f, yaw + (seg == 0 ? 0f : Rf(-35f, 35f)), 0f));
                crack.transform.localScale = new Vector3(0.07f, 0.01f, Rf(0.9f, 1.4f));
                crack.GetComponent<Renderer>().sharedMaterial = M["CrackDark"];
                Object.DestroyImmediate(crack.GetComponent<Collider>());
            }
        }
        // irregular dark slabs along the inner edge so the floor does not end in a straight line
        for (int side = 0; side < 4; side++)
            for (float s = -13f; s <= 13f; s += 2.6f)
            {
                float a = s + Rf(-0.6f, 0.6f), d = 13.3f + Rf(-0.5f, 0.4f);
                var pos = side switch { 0 => new Vector3(d, 0.03f, a), 1 => new Vector3(-d, 0.03f, a), 2 => new Vector3(a, 0.03f, d), _ => new Vector3(a, 0.03f, -d) };
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "EdgeSlab";
                slab.transform.SetParent(checker, false);
                slab.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Rf(0f, 90f), 0f));
                slab.transform.localScale = new Vector3(Rf(1.4f, 2.6f), 0.012f, Rf(1.2f, 2.2f));
                slab.GetComponent<Renderer>().sharedMaterial = M["PurpleGround3"];
                Object.DestroyImmediate(slab.GetComponent<Collider>());
            }

        // ---- invisible boundary + ceiling
        var boundary = Group("Boundary", env);
        void Wall(Vector3 pos, Vector3 size)
        {
            var w = new GameObject("Wall");
            w.transform.SetParent(boundary, false);
            w.transform.position = pos;
            w.AddComponent<BoxCollider>().size = size;
        }
        Wall(new Vector3(14.6f, 6f, 0f), new Vector3(1f, 14f, 32f));
        Wall(new Vector3(-14.6f, 6f, 0f), new Vector3(1f, 14f, 32f));
        Wall(new Vector3(0f, 6f, 14.6f), new Vector3(32f, 14f, 1f));
        Wall(new Vector3(0f, 6f, -14.6f), new Vector3(32f, 14f, 1f));
        var ceiling = new GameObject("Ceiling");
        ceiling.transform.SetParent(boundary, false);
        ceiling.transform.position = new Vector3(0f, 6.5f, 0f);
        ceiling.AddComponent<BoxCollider>().size = new Vector3(32f, 1f, 32f);
        var ceilVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceilVis.name = "CeilingVisual";
        ceilVis.transform.SetParent(boundary, false);
        ceilVis.transform.position = new Vector3(0f, 5.98f, 0f);
        ceilVis.transform.localScale = new Vector3(29f, 0.04f, 29f);
        ceilVis.GetComponent<Renderer>().sharedMaterial = M["CeilingGlow"];
        ceilVis.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(ceilVis.GetComponent<Collider>());
        ceilVis.SetActive(false);

        // ---- lava overlay
        var lavaGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lavaGo.name = "LavaOverlay";
        lavaGo.transform.SetParent(env, false);
        lavaGo.transform.position = new Vector3(0f, 0.05f, 0f);
        lavaGo.transform.localScale = new Vector3(29.4f, 0.03f, 29.4f);
        lavaGo.GetComponent<Renderer>().sharedMaterial = M["LavaWarning"];
        lavaGo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(lavaGo.GetComponent<Collider>());
        var lava = lavaGo.AddComponent<LavaFloor>();
        lava.overlay = lavaGo.GetComponent<Renderer>();
        lava.warningMaterial = M["LavaWarning"];
        lava.activeMaterial = M["LavaActive"];
        lavaGo.GetComponent<Renderer>().enabled = false;

        // ---- perimeter: layered rock clusters (low on the camera side, tall on the far side)
        string[] rockPrefabs = { "Rock_A", "Rock_B", "Rock_C", "Rock_Pillar_A", "Rock_Pillar_B" };
        string[] rockMats = { "BlueRock", "VioletRock", "LightRock", "IndigoRock" };
        string[] crystalMats = { "PinkCrystal", "MagentaCrystal" };
        string RockMat() => rockMats[rng.Next(rockMats.Length)];

        void RockCluster(Vector2 pos, bool ring2, int index)
        {
            float far = Mathf.InverseLerp(-20f, 20f, pos.x + pos.y); // 0 = near the camera, 1 = far
            float k = ring2 ? 1.5f : 1f;
            bool solid = !ring2;
            var inward = (-pos).normalized;
            var side = new Vector2(-inward.y, inward.x);

            // base boulder
            float nearScale = Mathf.Lerp(0.7f, 1f, far);
            float w = Rf(3f, 4.2f) * k * nearScale, h = Mathf.Lerp(0.8f, 3.8f, Mathf.Pow(far, 1.3f)) * Rf(0.85f, 1.25f) * k;
            Place(rockPrefabs[rng.Next(3)], new Vector3(pos.x, 0f, pos.y), Rf(0f, 360f), new Vector3(w, h, w * Rf(0.85f, 1.15f)), perimeter, RockMat(), solid).name = "Rock_Base";
            // flanking boulders of different heights
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? -1f : 1f;
                var p2 = pos + side * sgn * Rf(1.9f, 2.6f) * k + inward * Rf(-0.4f, 0.9f);
                float w2 = Rf(1.8f, 2.8f) * k;
                Place(rockPrefabs[rng.Next(3)], new Vector3(p2.x, 0f, p2.y), Rf(0f, 360f), new Vector3(w2, h * Rf(0.4f, 0.8f), w2 * Rf(0.8f, 1.2f)), perimeter, RockMat(), solid).name = "Rock_Flank";
            }
            // stacked cap rock gives the formation a layered silhouette
            if (index % 2 == 0)
            {
                var pc = pos + inward * 0.3f;
                Place(rockPrefabs[rng.Next(3)], new Vector3(pc.x, h * 0.62f, pc.y), Rf(0f, 360f), new Vector3(w * 0.5f, h * 0.45f, w * 0.5f), perimeter, RockMat(), false).name = "Rock_Cap";
            }
            // occasional tall column
            if (index % 3 == 1)
            {
                var pp = pos - inward * 1.2f + side * Rf(-1f, 1f);
                float pw = Rf(1.5f, 2.3f) * k;
                Place(index % 2 == 0 ? "Rock_Pillar_A" : "Rock_Pillar_B", new Vector3(pp.x, 0f, pp.y), Rf(0f, 360f), new Vector3(pw, Mathf.Lerp(1.4f, 6f, Mathf.Pow(far, 1.3f)) * k, pw), perimeter, RockMat(), solid).name = "Rock_Column";
            }
            // low ledge in front of some clusters
            if (!ring2 && index % 3 == 0)
            {
                var pl = pos + inward * Rf(2.2f, 2.8f) + side * Rf(-1.2f, 1.2f);
                Place("Plateau", new Vector3(pl.x, 0f, pl.y), Rf(0f, 360f), new Vector3(0.55f, 0.4f, 0.55f), perimeter, RockMat(), true).name = "Rock_Ledge";
            }
            // crystals tucked against the cluster, decorative only
            if (!ring2 && index % 2 == 1)
            {
                var pcr = pos + inward * Rf(2.6f, 3.4f) + side * Rf(-1.5f, 1.5f);
                if (Free(pcr, 0.6f))
                    Place(rng.Next(2) == 0 ? "Crystal_A" : "Crystal_B", new Vector3(pcr.x, 0f, pcr.y), Rf(0f, 360f), Vector3.one * Rf(0.9f, 1.35f), perimeter, crystalMats[rng.Next(2)], false).name = "Crystal_Edge";
            }
        }

        int clusterIndex = 0;
        for (int ring = 0; ring < 2; ring++)
            for (int side = 0; side < 4; side++)
                for (float s = -15f; s <= 15f; s += ring == 0 ? 3.7f : 5.2f)
                {
                    float along = s + Rf(-0.6f, 0.6f);
                    float dist = ring == 0 ? 15.4f + Rf(-0.4f, 0.5f) : 20.5f + Rf(-1f, 1.5f);
                    Vector2 pos = side switch { 0 => new Vector2(dist, along), 1 => new Vector2(-dist, along), 2 => new Vector2(along, dist), _ => new Vector2(along, -dist) };
                    RockCluster(pos, ring == 1, clusterIndex++);
                }

        // ---- landmarks
        Place("Crystal_Spire", new Vector3(SpirePos.x, 0f, SpirePos.y), 20f, Vector3.one * 1.5f, props);
        Place("Rock_Pillar_A", new Vector3(PillarPos.x, 0f, PillarPos.y), 10f, new Vector3(2.6f, 4.6f, 2.6f), props, "VioletRock");
        Place("Rock_Pillar_B", new Vector3(PillarPos.x + 2.1f, 0f, PillarPos.y + 0.6f), 70f, new Vector3(2f, 3.2f, 2f), props, "BlueRock");
        Place("Rock_Pillar_A", new Vector3(PillarPos.x - 0.6f, 0f, PillarPos.y + 2.2f), 130f, new Vector3(1.7f, 2.4f, 1.7f), props, "LightRock");
        Place("Crystal_A", new Vector3(PillarPos.x + 1.2f, 0f, PillarPos.y + 1.7f), 0f, Vector3.one * 0.9f, props, null, false);

        // ---- platforms (safe from lava) and plateaus
        foreach (var p in PlatformPos) Place("SafePlatform", new Vector3(p.x, 0f, p.y), 0f, Vector3.one, platforms);
        for (int i = 0; i < PlateauPos.Length; i++)
        {
            var p = PlateauPos[i];
            Place("Plateau", new Vector3(p.x, 0f, p.y), i * 40f, Vector3.one, platforms);
            Place("Crystal_B", new Vector3(p.x + 0.9f, 1.1f, p.y + 0.8f), Rf(0f, 360f), Vector3.one * 0.8f, props, null, false);
            // ladder leaning against the plateau, approached from the arena centre
            var dir = (-p).normalized;
            var basePos = p + dir * 2.7f;
            Place("Ladder", new Vector3(basePos.x, 0f, basePos.y), Mathf.Atan2(-dir.x, -dir.y) * Mathf.Rad2Deg, Vector3.one, props);
        }

        // ---- hazard & safe tiles
        var redTiles = new List<HazardTile>();
        foreach (var p in RedTilePos)
            redTiles.Add(Place("RedTile", new Vector3(p.x, 0f, p.y), 0f, Vector3.one, tiles).GetComponent<HazardTile>());
        for (int i = 0; i < 6; i++)
            if (Sample(12.2f, 1.6f, out var p)) { Place("SafeTile", new Vector3(p.x, 0f, p.y), Rf(0f, 90f), Vector3.one, tiles); Keep(p.x, p.y, 1.6f); }

        // ---- bridges & barriers
        Place("Bridge", new Vector3(Bridge1.x, 0f, Bridge1.y), 0f, Vector3.one, props);
        Place("Bridge", new Vector3(Bridge2.x, 0f, Bridge2.y), 0f, Vector3.one, props);
        var barrierSpots = new[] { new Vector3(5f, 10.6f, 0f), new Vector3(2.7f, 10.9f, 0f), new Vector3(-1f, -9.6f, 0f), new Vector3(-4.6f, -10.3f, 90f), new Vector3(8f, -1f, 90f), new Vector3(12f, -6.5f, 0f) };
        foreach (var bs in barrierSpots)
        {
            Place("Barrier", new Vector3(bs.x, 0f, bs.y), bs.z, Vector3.one, props);
            Keep(bs.x, bs.y, 1.3f);
        }

        // ---- boulders (solid obstacles) and decoration
        int placed = 0;
        for (int i = 0; i < 40 && placed < 8; i++)
        {
            if (!Sample(11.5f, 2.2f, out var p)) break;
            float w = Rf(1.8f, 2.7f);
            Place(rockPrefabs[rng.Next(3)], new Vector3(p.x, 0f, p.y), Rf(0f, 360f), new Vector3(w, Rf(1.1f, 1.9f), w * Rf(0.8f, 1.1f)), props, rockMats[rng.Next(3)]);
            Keep(p.x, p.y, w * 0.6f + 1.2f);
            placed++;
        }
        for (int i = 0; i < 10; i++)
            if (Sample(12.6f, 1.2f, out var p)) { Place(i % 2 == 0 ? "Crystal_A" : "Crystal_B", new Vector3(p.x, 0f, p.y), Rf(0f, 360f), Vector3.one * Rf(0.8f, 1.3f), props, null, false); Keep(p.x, p.y, 0.9f); }
        for (int i = 0; i < 24; i++)
            if (Sample(12.8f, 0.9f, out var p)) { Place("Grass_Patch", new Vector3(p.x, 0f, p.y), Rf(0f, 360f), Vector3.one * Rf(1f, 1.6f), props, null, false); Keep(p.x, p.y, 0.7f); }
        // grass hugging platform corners (never near hazard tiles)
        bool NearRed(Vector2 q) { foreach (var r in RedTilePos) if (Vector2.Distance(q, r) < 2.6f) return true; return false; }
        foreach (var pf in PlatformPos)
            for (int i = 0; i < 2; i++)
            {
                var corner = pf + new Vector2(rng.Next(2) == 0 ? -1f : 1f, rng.Next(2) == 0 ? -1f : 1f) * Rf(1.9f, 2.2f);
                if (Mathf.Abs(corner.x) < 12.5f && Mathf.Abs(corner.y) < 12.5f && !NearRed(corner))
                    Place("Grass_Patch", new Vector3(corner.x, 0f, corner.y), Rf(0f, 360f), Vector3.one * Rf(0.9f, 1.3f), props, null, false);
            }
        // grass along the rocky edge
        for (int i = 0; i < 40; i++)
            if (Sample(13.2f, 0.9f, out var pe) && Mathf.Max(Mathf.Abs(pe.x), Mathf.Abs(pe.y)) > 10.8f && !NearRed(pe))
            { Place("Grass_Patch", new Vector3(pe.x, 0f, pe.y), Rf(0f, 360f), Vector3.one * Rf(1.1f, 1.7f), props, null, false); Keep(pe.x, pe.y, 0.7f); }
        for (int i = 0; i < 26; i++)
            if (Sample(13f, 0.6f, out var p)) Place("Stone", new Vector3(p.x, 0f, p.y), Rf(0f, 360f), new Vector3(Rf(0.25f, 0.55f), Rf(0.2f, 0.4f), Rf(0.25f, 0.5f)), props, null, false);

        // ---- coins
        var coinGroup = Group("Coins");
        var coinList = new List<Coin>();
        void AddCoin(Vector3 pos) => coinList.Add(Place("Coin", pos, 0f, Vector3.one, coinGroup).GetComponent<Coin>());
        foreach (var p in FloorCoins) AddCoin(new Vector3(p.x, 0.95f, p.y));
        for (int i = 1; i < PlatformPos.Length; i++) AddCoin(new Vector3(PlatformPos[i].x, 1.6f, PlatformPos[i].y));
        foreach (var p in PlateauPos) AddCoin(new Vector3(p.x, 2.05f, p.y));
        AddCoin(new Vector3(Bridge1.x, 1.4f, Bridge1.y));

        // ---- lighting
        var sunGo = new GameObject("Sun");
        sunGo.transform.rotation = Quaternion.Euler(52f, -25f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = C(1f, 0.93f, 0.85f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = C(0.55f, 0.5f, 0.95f);
        RenderSettings.ambientEquatorColor = C(0.38f, 0.3f, 0.66f);
        RenderSettings.ambientGroundColor = C(0.22f, 0.16f, 0.4f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = C(0.2f, 0.12f, 0.4f);
        RenderSettings.fogDensity = 0.006f;

        void PointLight(string name, Vector3 pos, Color col, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = col; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        }
        PointLight("SpireGlow", new Vector3(SpirePos.x, 3f, SpirePos.y), C(1f, 0.35f, 0.85f), 13f, 4f);
        PointLight("PillarGlow", new Vector3(PillarPos.x + 1.2f, 1.6f, PillarPos.y + 1.7f), C(1f, 0.35f, 0.85f), 8f, 2.5f);
        PointLight("BridgeWarm1", new Vector3(Bridge1.x, 2.4f, Bridge1.y - 0.5f), C(1f, 0.62f, 0.28f), 8f, 2.2f);
        PointLight("BridgeWarm2", new Vector3(Bridge2.x, 2.4f, Bridge2.y + 0.5f), C(1f, 0.62f, 0.28f), 8f, 2.2f);
        PointLight("CenterGlow", new Vector3(0f, 4f, 0f), C(0.35f, 0.85f, 1f), 14f, 1.4f);

        // ---- post processing
        var profilePath = Root + "/Materials/GlobalVolumeProfile.asset";
        AssetDatabase.DeleteAsset(profilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);
        AssetDatabase.SaveAssets();
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f); bloom.intensity.Override(0.3f); bloom.scatter.Override(0.6f);
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.22f); vig.smoothness.Override(0.5f);
        var grade = profile.Add<ColorAdjustments>(true);
        grade.saturation.Override(12f); grade.contrast.Override(8f);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);
        EditorUtility.SetDirty(profile);
        var volGo = new GameObject("Global Volume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true; vol.sharedProfile = profile;

        // ---- player & camera
        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(P["Player"]);
        playerGo.transform.position = new Vector3(0f, 1.75f, 0f);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(0.16f, 0.09f, 0.34f);
        cam.fieldOfView = 34f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 140f;
        camGo.AddComponent<AudioListener>();
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var rig = camGo.AddComponent<CameraRig>();
        rig.target = playerGo.transform;
        rig.distance = 32f; rig.pitch = 50f; rig.fieldOfView = 34f; rig.focusClamp = 5f;
        rig.menuFocus = new Vector3(-3f, 0f, 3f); rig.menuDistance = 36f; rig.menuPitch = 34f;
        camGo.transform.position = new Vector3(-16f, 18f, -16f);
        camGo.transform.rotation = Quaternion.Euler(52f, 45f, 0f);
        playerGo.GetComponent<PlayerController>().cameraRig = rig;

        // ---- systems
        var arena = env.gameObject.AddComponent<ArenaController>();
        arena.hazardTiles = redTiles.ToArray();
        arena.lava = lava;
        arena.ceilingVisual = ceilVis;

        var sysGo = new GameObject("GameSystems");
        sysGo.AddComponent<AudioManager>();
        var rulesGo = new GameObject("Rules");
        rulesGo.transform.SetParent(sysGo.transform, false);
        var ruleComps = new List<RuleBase>();
        void AddRule<T>(string key) where T : RuleBase
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(rulesGo.transform, false);
            var r = go.AddComponent<T>();
            r.definition = defs[key];
            ruleComps.Add(r);
        }
        AddRule<NeverStopRule>("never_stop");
        AddRule<RedIsDeadRule>("red_is_dead");
        AddRule<GravityFlipRule>("gravity_flip");
        AddRule<FloorIsLavaRule>("floor_is_lava");
        AddRule<KeepJumpingRule>("keep_jumping");
        AddRule<ReversedControlsRule>("controls_reversed");
        var ruleManager = rulesGo.AddComponent<RuleManager>();
        ruleManager.rules = ruleComps.ToArray();

        var spawnerGo = new GameObject("CoinSpawner");
        spawnerGo.transform.SetParent(sysGo.transform, false);
        var spawner = spawnerGo.AddComponent<CoinSpawner>();
        spawner.coins = coinList.ToArray();
        var burstGo = new GameObject("CoinBurst");
        burstGo.transform.SetParent(spawnerGo.transform, false);
        var ps = burstGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false; main.loop = false; main.duration = 1f;
        main.startLifetime = 0.55f; main.startSpeed = 3.2f; main.startSize = 0.14f;
        main.startColor = C(1f, 0.85f, 0.3f); main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.15f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = M["CoinSpark"];
        spawner.burst = ps;

        var gm = sysGo.AddComponent<GameManager>();
        gm.player = playerGo.GetComponent<PlayerController>();
        gm.health = playerGo.GetComponent<PlayerHealth>();
        gm.gravity = playerGo.GetComponent<PlayerGravityController>();
        gm.rules = ruleManager;
        gm.coins = spawner;
        gm.cameraRig = rig;
        gm.arena = arena;

        var intensityMgr = sysGo.AddComponent<IntensityManager>();
        gm.intensity = intensityMgr;

        var ruleBurstGo = new GameObject("RuleBurst");
        ruleBurstGo.transform.SetParent(sysGo.transform, false);
        var rb2 = ruleBurstGo.AddComponent<ParticleSystem>();
        var rm = rb2.main;
        rm.playOnAwake = false; rm.loop = false; rm.duration = 1f;
        rm.startLifetime = 0.9f; rm.startSpeed = 4.5f; rm.startSize = 0.22f; rm.gravityModifier = -0.15f;
        rm.simulationSpace = ParticleSystemSimulationSpace.World;
        var rem = rb2.emission; rem.enabled = false;
        var rsh = rb2.shape; rsh.shapeType = ParticleSystemShapeType.Circle; rsh.radius = 0.8f; rsh.rotation = new Vector3(90f, 0f, 0f);
        rb2.GetComponent<ParticleSystemRenderer>().sharedMaterial = M["CoinSpark"];

        var visuals = sysGo.AddComponent<IntensityVisuals>();
        visuals.sun = sun;
        visuals.volume = vol;
        visuals.ruleBurst = rb2;

        var uiGo = new GameObject("UI");
        uiGo.AddComponent<GameUI>();

        // static batching for the environment
        foreach (var t in env.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        // crystals get a per-renderer glow at runtime, so they must not be statically batched
        foreach (var t in env.GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("Crystal")) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        GameObjectUtility.SetStaticEditorFlags(lavaGo, 0);
        GameObjectUtility.SetStaticEditorFlags(ceilVis, 0);
        foreach (var t in tiles.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
