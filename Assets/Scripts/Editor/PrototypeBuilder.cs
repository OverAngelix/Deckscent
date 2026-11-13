using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit (ou reconstruit) les prefabs du labyrinthe et la scène Main.
    /// Menu Deckscent > … dans l'éditeur. Relancer écrase les prefabs : après l'avoir lancé une
    /// fois, on règle les prefabs à la main dans l'éditeur et on ne le relance que pour repartir
    /// de zéro.
    ///
    /// Les pièces Synty sont copiées (maillage + matériaux) dans nos prefabs, pas imbriquées :
    /// sans le pack, le maillage manque, le labyrinthe le voit et passe aux blocs gris.
    /// </summary>
    public static class PrototypeBuilder
    {
        const string PrefabDir = "Assets/Prefabs";
        const string MazeDir = PrefabDir + "/Maze";
        const string MaterialDir = "Assets/Materials";
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string Synty = "Assets/Synty/PolygonDungeon/Prefabs/";

        // Pièces Synty : grille de 5 m, pivot au coin, étendues vers -X (et +Z pour sol et
        // plafond), face sculptée vers +Z. Nos prefabs sont faits pour des cases de 3 m et des
        // murs de 3,5 m, centrés sur le bord (murs) ou la case (sol, plafond).
        const float Cell = 3f;
        const float WallHeight = 3.5f;
        const float SyntyTile = 5f;

        [MenuItem("Deckscent/Construire les prefabs du labyrinthe")]
        public static void BuildMazePrefabs()
        {
            EnsureFolder(MazeDir);
            EnsureFolder(MaterialDir);

            var wallGrey = LitMaterial("M_Gris_Mur", new Color(0.78f, 0.78f, 0.78f));
            var floorGrey = LitMaterial("M_Gris_Sol", new Color(0.9f, 0.9f, 0.9f));
            var ceilingGrey = LitMaterial("M_Gris_Plafond", new Color(0.6f, 0.6f, 0.6f));
            var flame = UnlitMaterial("M_Flamme", Color.white);

            // Blocs gris (secours, sans asset externe).
            SavePrefab(Primitive("Mur_Gris", PrimitiveType.Cube, wallGrey,
                new Vector3(0f, WallHeight / 2f, -0.1f), Quaternion.identity, new Vector3(Cell, WallHeight, 0.2f)), MazeDir);
            SavePrefab(Primitive("Sol_Gris", PrimitiveType.Cube, floorGrey,
                new Vector3(0f, -0.05f, 0f), Quaternion.identity, new Vector3(Cell, 0.1f, Cell)), MazeDir);
            SavePrefab(Primitive("Plafond_Gris", PrimitiveType.Quad, ceilingGrey,
                Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), new Vector3(Cell, Cell, 1f)), MazeDir);

            // Décor Synty.
            var s = Cell / SyntyTile;
            var wallScale = new Vector3(s, WallHeight / SyntyTile, s);
            var flatScale = new Vector3(s, s, s);
            var wallOffset = new Vector3(Cell / 2f, 0f, 0f);
            var flatOffset = new Vector3(Cell / 2f, 0f, -Cell / 2f);
            foreach (var n in new[] { "01", "03", "04" })
                SavePrefab(FromSynty("Mur_" + n, Synty + "Environments/Walls/SM_Env_Wall_" + n + ".prefab", wallOffset, Quaternion.identity, wallScale), MazeDir);
            SavePrefab(FromSynty("Sol", Synty + "Environments/Optimized/SM_Env_Tiles_Texture_01.prefab", flatOffset, Quaternion.identity, flatScale), MazeDir);
            SavePrefab(FromSynty("Plafond", Synty + "Environments/Walls/SM_Env_Ceiling_Stone_Flat_01.prefab", flatOffset, Quaternion.identity, flatScale), MazeDir);

            SavePrefab(BuildTorch(flame), MazeDir);
            SavePrefab(BuildPlayer(), PrefabDir);

            AssetDatabase.SaveAssets();
            Debug.Log("Deckscent : prefabs du labyrinthe construits dans " + PrefabDir);
        }

        /// <summary>
        /// Plan de la scène de test : figé, c'est le repère des captures et des tests PlayMode.
        /// Ne pas le modifier sans mettre à jour les poses de <see cref="TestCaptures"/> et les tests.
        /// </summary>
        public const string TestPlan =
            "###############\n" +
            "#T....#.......#\n" +
            "#.###.#.#####.#\n" +
            "#.#...P.#...#.#\n" +
            "#.#.###.#.#.#.#\n" +
            "#...#T....#...#\n" +
            "###.#######.###\n" +
            "#.....T.......#\n" +
            "###############";

        public const string TestScenePath = "Assets/Scenes/Tests/Test_Labyrinthe.unity";

        [MenuItem("Deckscent/Construire la scène Main")]
        public static void BuildMainScene()
        {
            BuildScene(ScenePath, null);
            // Main en tête des scènes du build.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var other in EditorBuildSettings.scenes)
                if (other.path != ScenePath)
                    scenes.Add(other);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Deckscent/Construire la scène de test")]
        public static void BuildTestScene() => BuildScene(TestScenePath, TestPlan);

        /// <summary>Une scène labyrinthe + joueur. `plan` null = plan par défaut du labyrinthe.</summary>
        static void BuildScene(string scenePath, string plan)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MazeDir + "/Mur_Gris.prefab") == null)
                BuildMazePrefabs();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.35f);
            RenderSettings.fog = false;

            var mazeGo = new GameObject("Labyrinthe");
            var maze = mazeGo.AddComponent<Maze>();
            var so = new SerializedObject(maze);
            SetPieces(so.FindProperty("decor"), new[] { "Mur_01", "Mur_03", "Mur_04" }, "Sol", "Plafond");
            SetPieces(so.FindProperty("fallback"), new[] { "Mur_Gris" }, "Sol_Gris", "Plafond_Gris");
            so.FindProperty("torchPrefab").objectReferenceValue = Load(MazeDir + "/Torche.prefab");
            if (plan != null)
                so.FindProperty("layout").stringValue = plan;
            so.ApplyModifiedPropertiesWithoutUndo();
            maze.Build();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(Load(PrefabDir + "/Player.prefab"), scene);
            var pso = new SerializedObject(player.GetComponent<PlayerController>());
            pso.FindProperty("maze").objectReferenceValue = maze;
            pso.ApplyModifiedPropertiesWithoutUndo();
            player.transform.position = maze.CellToWorld(maze.StartCell);

            EnsureFolder(Path.GetDirectoryName(scenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("Deckscent : scène " + scenePath + " construite.");
        }

        static void SetPieces(SerializedProperty set, string[] walls, string floor, string ceiling)
        {
            var wallsProp = set.FindPropertyRelative("walls");
            wallsProp.arraySize = walls.Length;
            for (var i = 0; i < walls.Length; i++)
                wallsProp.GetArrayElementAtIndex(i).objectReferenceValue = Load(MazeDir + "/" + walls[i] + ".prefab");
            set.FindPropertyRelative("floor").objectReferenceValue = Load(MazeDir + "/" + floor + ".prefab");
            set.FindPropertyRelative("ceiling").objectReferenceValue = Load(MazeDir + "/" + ceiling + ".prefab");
        }

        /// <summary>Torche murale : sa racine est sur le mur, +Z vers la case.</summary>
        static GameObject BuildTorch(Material flame)
        {
            var root = new GameObject("Torche");

            // Bâton Synty, penché de 20° en s'écartant du mur.
            var stick = FromSynty("Bâton", Synty + "Props/SM_Prop_TorchStick_01.prefab",
                Vector3.zero, Quaternion.identity, Vector3.one * 0.5f);
            stick.transform.SetParent(root.transform, false);
            stick.transform.localPosition = new Vector3(0f, -0.35f, 0.12f);
            stick.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            foreach (var r in stick.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = ShadowCastingMode.Off; // sinon un trait d'ombre parasite sur le mur

            var fire = Primitive("Flamme", PrimitiveType.Sphere, flame,
                new Vector3(0f, 0.3f, 0.36f), Quaternion.identity, new Vector3(0.2f, 0.28f, 0.2f));
            foreach (var r in fire.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
            fire.transform.SetParent(root.transform, false);

            var lightGo = new GameObject("Lumière");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.3f, 0.6f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 2.5f;
            light.range = 9f;
            light.shadows = LightShadows.Hard;
            lightGo.AddComponent<TorchFlicker>();
            return root;
        }

        static GameObject BuildPlayer()
        {
            var root = new GameObject("Player");
            root.AddComponent<PlayerController>();

            var camGo = new GameObject("Caméra");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();

            // Petite lueur autour du joueur, pour qu'un couloir sans torche ne soit pas noir.
            var lantern = new GameObject("Lanterne");
            lantern.transform.SetParent(camGo.transform, false);
            lantern.transform.localPosition = new Vector3(0f, 0.2f, -0.4f);
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 0.9f;
            light.range = 6f;
            light.shadows = LightShadows.None;
            ConfigurePlayer(root);
            return root;
        }

        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>
        /// Branche le PlayerController sur les actions de la map Player d'InputSystem_Actions
        /// et sur sa lanterne. Appelé à la construction du prefab, et à rappeler sur le prefab
        /// existant si les actions changent (sans le reconstruire).
        /// </summary>
        public static void ConfigurePlayer(GameObject root)
        {
            var so = new SerializedObject(root.GetComponent<PlayerController>());
            foreach (var (field, action) in new[] { ("move", "Move"), ("turn", "Turn"), ("wait", "Wait"), ("torch", "Torch") })
                so.FindProperty(field).objectReferenceValue = ActionReference("Player", action);
            var lantern = root.transform.Find("Caméra/Lanterne");
            so.FindProperty("lantern").objectReferenceValue = lantern != null ? lantern.GetComponent<Light>() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static UnityEngine.InputSystem.InputActionReference ActionReference(string map, string action)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
                if (asset is UnityEngine.InputSystem.InputActionReference reference
                    && reference.action != null && reference.action.actionMap.name == map && reference.action.name == action)
                    return reference;
            Debug.LogError($"Deckscent : action {map}/{action} introuvable dans {InputActionsPath}.");
            return null;
        }

        /// <summary>Copie les maillages d'un prefab Synty (sans ses scripts ni collisions) sous une nouvelle racine.</summary>
        static GameObject FromSynty(string name, string syntyPath, Vector3 offset, Quaternion rotation, Vector3 scale)
        {
            var root = new GameObject(name);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(syntyPath);
            if (source == null)
            {
                Debug.LogWarning("Deckscent : " + syntyPath + " introuvable (pack Synty non importé ?). Prefab " + name + " vide.");
                return root;
            }
            var piece = new GameObject("Pièce");
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = offset;
            piece.transform.localRotation = rotation;
            piece.transform.localScale = scale;
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                var part = new GameObject(filter.name);
                part.transform.SetParent(piece.transform, false);
                // Position de la sous-pièce par rapport à la racine du prefab Synty.
                var m = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                part.transform.localPosition = m.GetColumn(3);
                part.transform.localRotation = m.rotation;
                part.transform.localScale = m.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                var sourceRenderer = filter.GetComponent<MeshRenderer>();
                if (sourceRenderer != null)
                    renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            }
            return root;
        }

        /// <summary>Une primitive Unity sans collision, sous une racine vide.</summary>
        static GameObject Primitive(string name, PrimitiveType type, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var root = new GameObject(name);
            var shape = GameObject.CreatePrimitive(type);
            shape.name = "Forme";
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.transform.SetParent(root.transform, false);
            shape.transform.localPosition = position;
            shape.transform.localRotation = rotation;
            shape.transform.localScale = scale;
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            return root;
        }

        static void SavePrefab(GameObject go, string dir)
        {
            PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + go.name + ".prefab");
            Object.DestroyImmediate(go);
        }

        static Material LitMaterial(string name, Color color) =>
            SaveMaterial(name, "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", 0f);
            });

        static Material UnlitMaterial(string name, Color color) =>
            SaveMaterial(name, "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", color));

        static Material SaveMaterial(string name, string shader, System.Action<Material> setup)
        {
            var path = MaterialDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
