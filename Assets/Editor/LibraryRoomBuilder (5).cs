using UnityEngine;
using UnityEditor;
using System.IO;

public class LibraryRoomBuilder : EditorWindow
{
    [MenuItem("Tools/Build Library Room")]
    public static void BuildRoom()
    {
        // --- Settings ---
        float roomWidth  = 6f;
        float roomLength = 8f;
        float roomHeight = 3.5f;
        float wallThick  = 0.2f;

        // Book is at X: 0.525, Z: 1.615 — center room around it
        Vector3 roomCenter = new Vector3(0.525f, 0f, 1.615f);

        // Colors
        Color woodColor    = new Color(0.42f, 0.26f, 0.12f);
        Color wallColor    = new Color(0.72f, 0.60f, 0.45f);
        Color ceilColor    = new Color(0.18f, 0.12f, 0.08f);
        Color lecternColor = new Color(0.30f, 0.18f, 0.08f);

        // Create materials
        Material woodMat    = CreateMaterial("LibWood",    woodColor);
        Material wallMat    = CreateMaterial("LibWall",    wallColor);
        Material ceilMat    = CreateMaterial("LibCeil",    ceilColor);
        Material shelfMat   = CreateMaterial("LibShelf",   woodColor);
        Material lecternMat = CreateMaterial("LibLectern", lecternColor);

        // Parent object
        GameObject root = new GameObject("LibraryRoom");
        root.transform.position = roomCenter;
        Undo.RegisterCreatedObjectUndo(root, "Build Library Room");

        // ── FLOOR ──
        CreateBox(root, "Floor",
            new Vector3(0, -wallThick / 2f, 0),
            new Vector3(roomWidth, wallThick, roomLength),
            woodMat);

        // ── CEILING ──
        CreateBox(root, "Ceiling",
            new Vector3(0, roomHeight + wallThick / 2f, 0),
            new Vector3(roomWidth, wallThick, roomLength),
            ceilMat);

        // ── WALLS ──
        CreateBox(root, "WallBack",
            new Vector3(0, roomHeight / 2f, -roomLength / 2f - wallThick / 2f),
            new Vector3(roomWidth, roomHeight, wallThick),
            wallMat);

        CreateBox(root, "WallFront",
            new Vector3(0, roomHeight / 2f, roomLength / 2f + wallThick / 2f),
            new Vector3(roomWidth, roomHeight, wallThick),
            wallMat);

        CreateBox(root, "WallLeft",
            new Vector3(-roomWidth / 2f - wallThick / 2f, roomHeight / 2f, 0),
            new Vector3(wallThick, roomHeight, roomLength + wallThick * 2f),
            wallMat);

        CreateBox(root, "WallRight",
            new Vector3(roomWidth / 2f + wallThick / 2f, roomHeight / 2f, 0),
            new Vector3(wallThick, roomHeight, roomLength + wallThick * 2f),
            wallMat);

        // ── BOOKSHELVES — flush against walls ──
        // Two units side by side on the back wall
        BuildShelfUnit(root, shelfMat, woodMat,
            new Vector3(-1.3f, 0, -roomLength / 2f + 0.13f),
            Quaternion.identity);

        BuildShelfUnit(root, shelfMat, woodMat,
            new Vector3(1.3f, 0, -roomLength / 2f + 0.13f),
            Quaternion.identity);

        // Side walls: shelf units rotated to run ALONG the wall (horizontal)
        // Left wall — unit faces inward, runs along Z axis
        BuildShelfUnit(root, shelfMat, woodMat,
            new Vector3(-roomWidth / 2f + 0.13f, 0, -0.8f),
            Quaternion.Euler(0, 0, 0));

        BuildShelfUnit(root, shelfMat, woodMat,
            new Vector3(-roomWidth / 2f + 0.13f, 0, 0.9f),
            Quaternion.Euler(0, 0, 0));

        // Right wall — unit faces inward, runs along Z axis
        BuildShelfUnit(root, shelfMat, woodMat,
            new Vector3(roomWidth / 2f - 0.13f, 0, -0.8f),
            Quaternion.Euler(0, 180, 0));

        BuildShelfUnit(root, shelfMat, woodMat,
            new Vector3(roomWidth / 2f - 0.13f, 0, 0.9f),
            Quaternion.Euler(0, 180, 0));

        // ── LECTERN under the book ──
        // Book world pos: 0.525, 1.41, 1.615 = local 0, 1.41, 0
        GameObject lectern = new GameObject("Lectern");
        lectern.transform.SetParent(root.transform);
        lectern.transform.localPosition = Vector3.zero;

        // Base
        CreateBoxLocal(lectern, "LecternBase",
            new Vector3(0, 0.05f, 0),
            new Vector3(0.5f, 0.1f, 0.4f), lecternMat);

        // Pole
        CreateBoxLocal(lectern, "LecternPole",
            new Vector3(0, 0.7f, 0),
            new Vector3(0.08f, 1.1f, 0.08f), lecternMat);

        // Reading surface
        CreateBoxLocal(lectern, "LecternTop",
            new Vector3(0, 1.25f, 0),
            new Vector3(0.45f, 0.05f, 0.35f), lecternMat);

        // ── WARM POINT LIGHT ──
        GameObject lightObj = new GameObject("LibraryLight");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.localPosition = new Vector3(0, roomHeight - 0.4f, 0);
        Light light = lightObj.AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = new Color(1f, 0.85f, 0.55f);
        light.intensity = 1.8f;
        light.range     = 10f;

        // ── PLACE DANIEL RICHES BOOKS ON SHELVES ──
        PlaceDanielRichesBooks(root);

        Debug.Log("[LibraryRoomBuilder] Library room built successfully!");
        Selection.activeGameObject = root;
    }

    // ── Builds a shelf unit: frame + 3 horizontal shelves ──
    static void BuildShelfUnit(GameObject parent, Material frameMat, Material shelfMat,
                                Vector3 pos, Quaternion rot)
    {
        GameObject unit = new GameObject("ShelfUnit");
        unit.transform.SetParent(parent.transform);
        unit.transform.localPosition = pos;
        unit.transform.localRotation = rot;

        float w = 1.4f, h = 2.6f, d = 0.3f, thick = 0.05f;

        // Back panel
        CreateBoxLocal(unit, "Back",
            new Vector3(0, h / 2f, d / 2f - thick / 2f),
            new Vector3(w, h, thick), frameMat);

        // Left side
        CreateBoxLocal(unit, "SideL",
            new Vector3(-w / 2f + thick / 2f, h / 2f, 0),
            new Vector3(thick, h, d), frameMat);

        // Right side
        CreateBoxLocal(unit, "SideR",
            new Vector3(w / 2f - thick / 2f, h / 2f, 0),
            new Vector3(thick, h, d), frameMat);

        // Three shelves at different heights
        float[] shelfHeights = { 0.6f, 1.3f, 2.0f };
        foreach (float sy in shelfHeights)
        {
            CreateBoxLocal(unit, "Shelf",
                new Vector3(0, sy, 0),
                new Vector3(w - thick * 2f, thick, d), shelfMat);
        }
    }

    // ── Attempt to place DanielRiches book prefabs on shelves ──
    static void PlaceDanielRichesBooks(GameObject parent)
    {
        // Find all prefabs in the DanielRiches folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/DanielRiches" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[LibraryRoomBuilder] No DanielRiches prefabs found.");
            return;
        }

        // Shelf world positions (approximate top surfaces)
        Vector3[] shelfSlots = new Vector3[]
        {
            new Vector3(-1.5f, 0.63f, -3.7f),
            new Vector3(-1.1f, 0.63f, -3.7f),
            new Vector3(-0.7f, 0.63f, -3.7f),
            new Vector3(-1.5f, 1.33f, -3.7f),
            new Vector3(-1.1f, 1.33f, -3.7f),
            new Vector3(-0.7f, 1.33f, -3.7f),
            new Vector3( 1.5f, 0.63f, -3.7f),
            new Vector3( 1.1f, 0.63f, -3.7f),
            new Vector3( 0.7f, 0.63f, -3.7f),
            new Vector3( 1.5f, 1.33f, -3.7f),
            new Vector3( 1.1f, 1.33f, -3.7f),
            new Vector3( 0.7f, 1.33f, -3.7f),
        };

        GameObject bookParent = new GameObject("DecorativeBooks");
        bookParent.transform.SetParent(parent.transform);

        int prefabIndex = 0;
        foreach (Vector3 slot in shelfSlots)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[prefabIndex % guids.Length]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { prefabIndex++; continue; }

            GameObject book = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            book.transform.SetParent(bookParent.transform);
            book.transform.position = slot;
            // Stand books upright, slight random rotation for naturalness
            float yRot = Random.Range(-8f, 8f);
            book.transform.rotation = Quaternion.Euler(0, yRot, 0);
            book.transform.localScale = Vector3.one * 0.8f;

            Undo.RegisterCreatedObjectUndo(book, "Place Book");
            prefabIndex++;
        }

        Debug.Log($"[LibraryRoomBuilder] Placed {shelfSlots.Length} decorative books.");
    }

    // ── Helpers ──
    static void CreateBox(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;
        go.GetComponent<Renderer>().material = mat;
        // Remove collider from purely decorative pieces to keep things clean
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    static void CreateBoxLocal(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;
        go.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    static Material CreateMaterial(string name, Color color)
    {
        // Ensure the LibraryMaterials folder exists
        if (!AssetDatabase.IsValidFolder("Assets/LibraryMaterials"))
            AssetDatabase.CreateFolder("Assets", "LibraryMaterials");

        string path = $"Assets/LibraryMaterials/{name}.mat";

        // Reuse if already exists
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            return existing;
        }

        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
