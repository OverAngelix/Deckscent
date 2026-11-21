using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit le camp à la surface (hub, docs/camp.md) : les prefabs du camp (Prefabs/Camp/,
    /// une pièce Synty par prefab : herbe, murets, feu de camp, tente, puits, arbres, grotte…) et
    /// la scène Camp (plan, décor, crépuscule, ciel gravé, joueur, pause). Relancer écrase les
    /// prefabs et la scène : une fois réglés à la main, ne le relancer que pour repartir de zéro.
    ///
    /// Packs : POLYGON Nature, Nature Biomes (Meadow Forest), Farm, Dungeon (non versionnés). S'ils
    /// manquent, les prefabs sont vides et le labyrinthe passe aux blocs gris.
    /// </summary>
    public static class CampBuilder
    {
        public const string Dir = PrototypeBuilder.PrefabDir + "/Camp";

        /// <summary>
        /// Plan du camp (cases de 3 m) : # muret, . herbe, ~ hors du chemin sans mur (la bouche de
        /// la grotte), P départ. La grotte est au nord, en (4, 1) ; le feu de camp au centre.
        /// </summary>
        public const string Plan =
            "###~~~###\n" +
            "###~.~###\n" +
            "#.......#\n" +
            "#.......#\n" +
            "#.......#\n" +
            "#.......#\n" +
            "#...P...#\n" +
            "#########";

        public static readonly Vector2Int EntranceCell = new Vector2Int(4, 1);
        public static readonly Vector2Int FireCell = new Vector2Int(4, 4);

        const float Cell = 3f;

        [MenuItem("Deckscent/Construire le camp (prefabs, scène)")]
        public static void BuildAll()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Deckscent : la scène ouverte a des changements non enregistrés. Enregistrer ou annuler, puis relancer.");
                return;
            }
            BuildPrefabs();
            BuildScene();
            MenuBuilder.UpdateBuildScenes();
        }

        // --- Prefabs -------------------------------------------------------------------------

        [MenuItem("Deckscent/Construire les prefabs du camp")]
        public static void BuildPrefabs()
        {
            PrototypeBuilder.EnsureFolder(Dir);
            var flame = AssetDatabase.LoadAssetAtPath<Material>(PrototypeBuilder.MaterialDir + "/M_Flamme.mat");

            // Le labyrinthe du camp : herbe, murets de pierre, clôture.
            Save(Fit("Herbe", "SM_Generic_Ground_Tile_01", Cell / 5f, ground: true));
            Save(Fit("Muret_01", "SM_Prop_Stonewall_Small_01", 1.08f));
            Save(Fit("Muret_02", "SM_Prop_Stonewall_Small_02", 1.08f));
            Save(Fit("Cloture", "SM_Prop_Meadow_Fence_03", 1.1f));

            // Ce qui occupe une case du camp (bloque le joueur).
            Save(Obstacle(BuildFire(flame)));
            Save(Obstacle(Fit("Tente", "SM_Prop_Camp_Tent_01", 1.35f)));
            Save(Obstacle(Fit("Puits", "SM_Prop_Well_01", 1f, pack: "PolygonNatureBiomes")));
            Save(Obstacle(Fit("Charrette", "SM_Prop_HandCart_01", 1f)));
            Save(Obstacle(Fit("EpeeDansLaPierre", "SM_Prop_Sword_Stone_01", 1f)));
            Save(Obstacle(Fit("Sechoir", "SM_Prop_Camp_Tanning_Rack_01", 1f)));
            Save(BuildEntrance());

            // Décor de passage (ne bloque pas) : posé contre les murets.
            Save(Group("Caisses", ("SM_Prop_Camp_Crate_01", new Vector3(-0.4f, 0f, 0f), 15f), ("SM_Prop_Barrel_01", new Vector3(0.45f, 0f, 0.1f), 0f),
                ("SM_Prop_Camp_Bucket_01", new Vector3(0.1f, 0f, 0.6f), 0f)));
            Save(Group("Bois", ("SM_Prop_Wood_Stack_01", Vector3.zero, 0f)));
            foreach (var (name, model, scale) in Ground)
                Save(Fit(name, model, scale));
            Save(BuildStandingTorch(flame));

            // Dehors, au-delà des murets.
            foreach (var (name, model) in Outside)
                Save(Fit(name, model, 1f, center: false));

            RenderingSetup.ConvertPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("Deckscent : prefabs du camp construits dans " + Dir);
        }

        /// <summary>Semis de la clairière (ne bloque pas) : petites touffes d'herbe et fleurs, clairsemées (retour de l'utilisateur : « trop d'herbes au sol »).</summary>
        static readonly (string, string, float)[] Ground =
        {
            ("Touffe_01", "SM_Generic_Grass_Patch_03", 0.25f), ("Touffe_02", "SM_Generic_Grass_Patch_01", 0.6f), ("Fleurs", "SM_Env_Flowers_01", 0.5f),
        };

        /// <summary>Décor du dehors : (prefab, modèle Synty), posés tels quels (pivot au pied).</summary>
        static readonly (string, string)[] Outside =
        {
            ("Pin_01", "SM_Tree_Pine_01"), ("Pin_02", "SM_Tree_Pine_02"), ("GrandPin_01", "SM_Tree_Pine_Large_01"),
            ("GrandPin_02", "SM_Tree_Pine_Large_02"), ("PinMaigre_01", "SM_Tree_PolyPine_Sparse_01"), ("PinMaigre_02", "SM_Tree_PolyPine_Sparse_02"),
            ("PinMort", "SM_Tree_Pine_Dead_01"), ("ArbreMort_01", "SM_Tree_Dead_01"), ("ArbreMort_02", "SM_Tree_Dead_02"), ("BouleauMort", "SM_Tree_Birch_Dead_01"),
            ("Rochers_01", "SM_Rock_Pile_01"), ("Rochers_02", "SM_Rock_Pile_03"), ("Rochers_03", "SM_Rock_Pile_05"),
            ("Falaise_01", "SM_Rock_Cluster_Large_03"), ("Falaise_02", "SM_Rock_Cluster_Large_05"),
            ("Cabane", "SM_Bld_Stone_Cabin_01"), ("ChariotBrise", "SM_Prop_Wagon_Broken_01"), ("Tombe", "SM_Prop_Grave_03"),
            ("Souche", "SM_Tree_Stump_02"), ("Colline", "SM_Generic_Mountains_Grass_01"), ("SolDehors", "SM_Generic_Ground_Tile_01"),
        };

        /// <summary>Feu de camp : pierres, bûches, trépied, flammes blanches, lumière qui vacille.</summary>
        static GameObject BuildFire(Material flame)
        {
            var root = new GameObject("FeuDeCamp");
            Attach(root, Fit("Pierres", "SM_Prop_Camp_Fireplace_Stones_01", 1.2f));
            Attach(root, Fit("Buches", "SM_Prop_Camp_Fireplace_01", 1.2f));
            Attach(root, Fit("Trepied", "SM_Prop_Camp_Fire_Tripod_01", 1.1f));
            foreach (var (pos, size) in new[] { (new Vector3(0f, 0.45f, 0f), new Vector3(0.38f, 0.7f, 0.38f)),
                (new Vector3(0.15f, 0.35f, 0.1f), new Vector3(0.24f, 0.45f, 0.24f)), (new Vector3(-0.14f, 0.32f, -0.08f), new Vector3(0.22f, 0.4f, 0.22f)) })
            {
                var f = PrototypeBuilder.Primitive("Flamme", PrimitiveType.Sphere, flame, pos, Quaternion.identity, size);
                foreach (var r in f.GetComponentsInChildren<MeshRenderer>())
                    r.shadowCastingMode = ShadowCastingMode.Off;
                Attach(root, f);
            }
            var lightGo = new GameObject("Lumière");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 3f;
            light.range = 12f;
            light.shadows = LightShadows.Hard;
            lightGo.AddComponent<TorchFlicker>();
            PrototypeBuilder.AddSoundEmitter(root, 1f);
            return root;
        }

        /// <summary>Torche plantée dans le sol (pieu Synty), avec sa flamme et sa lumière.</summary>
        static GameObject BuildStandingTorch(Material flame)
        {
            var root = new GameObject("TorchePieu");
            Attach(root, Fit("Pieu", "SM_Prop_TorchStick_01", 1.3f, pack: "PolygonNature"));
            var f = PrototypeBuilder.Primitive("Flamme", PrimitiveType.Sphere, flame, new Vector3(0f, 2.3f, 0f), Quaternion.identity, new Vector3(0.18f, 0.3f, 0.18f));
            foreach (var r in f.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
            Attach(root, f);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
            var lightGo = new GameObject("Lumière");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 2f;
            light.range = 8f;
            light.shadows = LightShadows.Hard;
            lightGo.AddComponent<TorchFlicker>();
            return root;
        }

        /// <summary>
        /// Entrée du donjon : l'arche de rocher (ouverte des deux côtés) adossée à la falaise, un fond
        /// d'encre dedans (on y voit le noir, pas le ciel), et la case d'entrée (y avancer = descendre).
        /// La racine est au centre de la case d'entrée ; l'arche est un peu plus au nord.
        /// </summary>
        static GameObject BuildEntrance()
        {
            var root = new GameObject("EntreeDuDonjon");
            var arch = Fit("Arche", "SM_Rock_CaveEntrance_01", 1f, pack: "PolygonNature", center: false);
            arch.transform.SetParent(root.transform, false);
            arch.transform.localPosition = new Vector3(-0.2f, -0.3f, 4.4f);
            var black = AssetDatabase.LoadAssetAtPath<Material>(PrototypeBuilder.MaterialDir + "/M_Encre.mat");
            if (black == null)
                black = PrototypeBuilder.SaveMaterial("M_Encre", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", Color.black));
            var back = PrototypeBuilder.Primitive("Fond", PrimitiveType.Cube, black, new Vector3(0f, 2.5f, 6.5f), Quaternion.identity, new Vector3(6f, 6f, 0.5f));
            back.transform.SetParent(root.transform, false);
            var entrance = root.AddComponent<DungeonEntrance>();
            var so = new SerializedObject(entrance);
            so.FindProperty("cell").vector2IntValue = EntranceCell;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrototypeBuilder.AddSoundEmitter(root, 1f);
            return root;
        }

        static GameObject Obstacle(GameObject go)
        {
            go.AddComponent<CampObstacle>();
            return go;
        }

        static void Attach(GameObject parent, GameObject child) => child.transform.SetParent(parent.transform, false);

        static GameObject Group(string name, params (string model, Vector3 pos, float yaw)[] parts)
        {
            var root = new GameObject(name);
            foreach (var (model, pos, yaw) in parts)
            {
                var p = Fit(model, model, 1f);
                p.transform.SetParent(root.transform, false);
                p.transform.localPosition = pos;
                p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
            return root;
        }

        /// <summary>
        /// Une pièce Synty (maillages copiés, voir <see cref="PrototypeBuilder.FromSynty"/>) à
        /// l'échelle `scale`, recentrée : milieu en x et z sur la racine, pied au sol (sauf `center`
        /// faux : pivot d'origine). `ground` : le dessus à 0 (dalle de sol).
        /// </summary>
        static GameObject Fit(string name, string model, float scale, string pack = null, bool center = true, bool ground = false)
        {
            var go = PrototypeBuilder.FromSynty(name, FindModel(model, pack), Vector3.zero, Quaternion.identity, Vector3.one * scale);
            if (!center && !ground)
                return go;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return go;
            var b = renderers[0].bounds;
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
            var piece = go.transform.GetChild(0);
            piece.localPosition -= new Vector3(b.center.x, ground ? b.max.y : b.min.y, b.center.z);
            return go;
        }

        static readonly Dictionary<string, string> ModelCache = new Dictionary<string, string>();

        /// <summary>Chemin du prefab Synty nommé `model` (dans le pack `pack` s'il est donné).</summary>
        static string FindModel(string model, string pack)
        {
            var key = model + "|" + pack;
            if (ModelCache.TryGetValue(key, out var cached))
                return cached;
            var folder = pack != null ? "Assets/Synty/" + pack : "Assets/Synty";
            var found = "";
            if (AssetDatabase.IsValidFolder(folder))
                foreach (var guid in AssetDatabase.FindAssets(model + " t:Prefab", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(path) == model)
                    {
                        found = path;
                        break;
                    }
                }
            if (found == "")
                found = "Assets/Synty/" + model + ".prefab"; // introuvable : FromSynty prévient, prefab vide
            ModelCache[key] = found;
            return found;
        }

        static void Save(GameObject go) => PrototypeBuilder.SavePrefab(go, Dir);

        static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/" + name + ".prefab");

        // --- Scène ---------------------------------------------------------------------------

        [MenuItem("Deckscent/Construire la scène du camp")]
        public static void BuildScene()
        {
            if (Prefab("Herbe") == null)
                BuildPrefabs();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrototypeBuilder.SetupLighting();

            // Labyrinthe du camp : herbe, murets, pas de plafond (le ciel).
            var mazeGo = new GameObject("Labyrinthe");
            mazeGo.SetActive(false);
            var maze = mazeGo.AddComponent<Maze>();
            var so = new SerializedObject(maze);
            SetPieces(so.FindProperty("decor"), new[] { Prefab("Muret_01"), Prefab("Muret_02"), Prefab("Muret_01"), Prefab("Cloture") }, Prefab("Herbe"));
            SetPieces(so.FindProperty("fallback"), new[] { PrototypeBuilder.Load(PrototypeBuilder.MazeDir + "/Mur_Gris.prefab") },
                PrototypeBuilder.Load(PrototypeBuilder.MazeDir + "/Sol_Gris.prefab"));
            so.FindProperty("torchPrefab").objectReferenceValue = null;
            so.FindProperty("layout").stringValue = Plan;
            so.ApplyModifiedPropertiesWithoutUndo();
            mazeGo.SetActive(true);
            maze.Build();

            // Ce qui occupe une case.
            var props = new GameObject("Camp").transform;
            Place(props, "FeuDeCamp", FireCell, 0f);
            Place(props, "EntreeDuDonjon", EntranceCell, 0f);
            Place(props, "Tente", new Vector2Int(1, 2), 140f);
            Place(props, "Sechoir", new Vector2Int(1, 4), 90f);
            Place(props, "Puits", new Vector2Int(7, 2), 0f);
            Place(props, "Charrette", new Vector2Int(7, 5), -30f);
            Place(props, "EpeeDansLaPierre", new Vector2Int(2, 5), 200f);

            // Décor de passage, contre les murets (décalé vers le bord de sa case).
            PlaceFree(props, "TorchePieu", maze.CellToWorld(new Vector2Int(3, 2)) + new Vector3(-0.6f, 0f, 1.0f), 0f);
            PlaceFree(props, "TorchePieu", maze.CellToWorld(new Vector2Int(5, 2)) + new Vector3(0.6f, 0f, 1.0f), 0f);
            PlaceFree(props, "Caisses", maze.CellToWorld(new Vector2Int(1, 6)) + new Vector3(-0.6f, 0f, -0.7f), 0f);
            PlaceFree(props, "Bois", maze.CellToWorld(new Vector2Int(7, 4)) + new Vector3(0.9f, 0f, 0f), 90f);
            PlaceFree(props, "Caisses", maze.CellToWorld(new Vector2Int(6, 6)) + new Vector3(0.4f, 0f, -0.8f), 180f);

            ScatterGround(props, maze);
            BuildOutside(maze);

            // Crépuscule : un faible clair de lune (blanc, ombres dures), la lune au nord-ouest.
            var moonGo = new GameObject("Clair de lune");
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = Color.white;
            moon.intensity = 0.35f;
            moon.shadows = LightShadows.Hard;
            moonGo.transform.rotation = Quaternion.LookRotation(-SkyDirection(-35f, 24f));
            var sky = new GameObject("Ciel").AddComponent<CampSky>();
            var skySo = new SerializedObject(sky);
            skySo.FindProperty("moonLight").objectReferenceValue = moon;
            skySo.ApplyModifiedPropertiesWithoutUndo();

            PauseBuilder.AddToScene(scene);
            BuildConfirmPrefab();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmPath), scene);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(PrototypeBuilder.Load(PrototypeBuilder.PrefabDir + "/Player.prefab"), scene);
            var pso = new SerializedObject(player.GetComponent<PlayerController>());
            pso.FindProperty("maze").objectReferenceValue = maze;
            pso.ApplyModifiedPropertiesWithoutUndo();
            player.transform.position = maze.CellToWorld(maze.StartCell);

            EditorSceneManager.SaveScene(scene, SceneFlow.CampScene);
            Debug.Log("Deckscent : scène du camp construite.");
        }

        /// <summary>
        /// Semis de la clairière : une touffe ou des fleurs sur une case d'herbe libre sur trois, au
        /// hasard (graine fixe), vers le bord de la case.
        /// </summary>
        static void ScatterGround(Transform parent, Maze maze)
        {
            var group = new GameObject("Herbes").transform;
            group.SetParent(parent, false);
            var taken = new HashSet<Vector2Int> { FireCell, EntranceCell, new Vector2Int(1, 2), new Vector2Int(1, 4), new Vector2Int(7, 2), new Vector2Int(7, 5), new Vector2Int(2, 5) };
            var rng = new System.Random(5);
            var size = maze.GridSize;
            for (var y = 0; y < size.y; y++)
            for (var x = 0; x < size.x; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!maze.IsWalkable(cell))
                    continue;
                var count = !taken.Contains(cell) && rng.NextDouble() < 0.35 ? 1 : 0;
                for (var k = 0; k < count; k++)
                {
                    var (name, _, _) = Ground[rng.Next(Ground.Length)];
                    // Vers les bords de la case : le milieu reste dégagé (là où l'on se tient).
                    var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    var r = Mathf.Lerp(0.8f, 1.35f, (float)rng.NextDouble());
                    var pos = maze.CellToWorld(cell) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    PlaceFree(group, name, pos, (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble() * 0.5f);
                }
            }
        }

        public const string ConfirmPath = PrototypeBuilder.PrefabDir + "/UI/Confirmation.prefab";

        /// <summary>Demande de confirmation (descendre dans le donjon) : canvas au-dessus du jeu, sous la pause.</summary>
        public static void BuildConfirmPrefab()
        {
            var root = LevelUpBuilder.OverlayRoot("Confirmation", 170, out var ink, out var texts);
            var screen = root.AddComponent<ConfirmScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("ink").objectReferenceValue = ink;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("navigate").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Navigate");
            so.FindProperty("submit").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Submit");
            so.FindProperty("cancel").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Cancel");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ConfirmPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Direction du ciel : `azimuth` en degrés depuis le nord (+Z) vers l'est, `elevation` au-dessus de l'horizon.</summary>
        static Vector3 SkyDirection(float azimuth, float elevation) =>
            Quaternion.Euler(-elevation, azimuth, 0f) * Vector3.forward;

        static void SetPieces(SerializedProperty set, GameObject[] walls, GameObject floor)
        {
            var wallsProp = set.FindPropertyRelative("walls");
            wallsProp.arraySize = walls.Length;
            for (var i = 0; i < walls.Length; i++)
                wallsProp.GetArrayElementAtIndex(i).objectReferenceValue = walls[i];
            set.FindPropertyRelative("floor").objectReferenceValue = floor;
            set.FindPropertyRelative("ceiling").objectReferenceValue = null;
        }

        static void Place(Transform parent, string prefab, Vector2Int cell, float yaw)
        {
            var maze = Object.FindAnyObjectByType<Maze>();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab), parent.gameObject.scene);
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(maze.CellToWorld(cell), Quaternion.Euler(0f, yaw, 0f));
            var obstacle = go.GetComponent<CampObstacle>();
            if (obstacle != null)
            {
                var so = new SerializedObject(obstacle);
                so.FindProperty("cell").vector2IntValue = cell;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static GameObject PlaceFree(Transform parent, string prefab, Vector3 position, float yaw, float scale = 1f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab), parent.gameObject.scene);
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>
        /// Dehors : un grand sol, la falaise derrière la grotte, la cabane et le chariot brisé, des
        /// tombes, puis une forêt de pins et d'arbres morts tout autour (au hasard, graine fixe),
        /// jamais dans la clairière ; des collines au loin pour l'horizon.
        /// </summary>
        static void BuildOutside(Maze maze)
        {
            var outside = new GameObject("Dehors").transform;
            var size = new Vector2(9 * Cell, 8 * Cell);
            var center = new Vector3(size.x * 0.5f, 0f, -size.y * 0.5f);
            // 120 m de côté, pivot au coin nord-est : de x −28 à 92, de z −90 à 30, sous la clairière
            // et toute la forêt (position réglée par l'utilisateur dans la scène).
            PlaceFree(outside, "SolDehors", new Vector3(92.1f, -0.03f, -89.5f), 0f, 24f);

            // La falaise derrière la grotte et autour d'elle.
            var cave = maze.CellToWorld(EntranceCell);
            PlaceFree(outside, "Falaise_01", cave + new Vector3(-9f, -2f, 9f), 30f);
            PlaceFree(outside, "Falaise_02", cave + new Vector3(8f, -2.5f, 10f), -20f);
            PlaceFree(outside, "Rochers_01", cave + new Vector3(-7f, 0f, 3.5f), 10f);
            PlaceFree(outside, "Rochers_02", cave + new Vector3(7f, 0f, 3.5f), 200f);
            // Les cases sans mur de part et d'autre de la bouche (« ~ ») : comblées de rochers,
            // collés à l'arche (sinon on voyait une case vide à côté de la grotte).
            PlaceFree(outside, "Rochers_01", cave + new Vector3(-3.4f, -0.3f, 0.6f), 150f, 0.75f);
            PlaceFree(outside, "Rochers_02", cave + new Vector3(3.4f, -0.3f, 0.6f), 20f, 0.75f);

            PlaceFree(outside, "Cabane", new Vector3(-5.5f, 0f, -11f), 90f);
            PlaceFree(outside, "ChariotBrise", new Vector3(31f, 0f, -15f), 20f);
            PlaceFree(outside, "Souche", new Vector3(28f, 0f, -6f), 0f);
            for (var i = 0; i < 4; i++)
                PlaceFree(outside, "Tombe", new Vector3(-3f - (i % 2) * 1.4f, 0f, -20f - i * 1.2f), 180f + i * 7f);
            PlaceFree(outside, "Rochers_03", new Vector3(14f, 0f, -29f), 70f);

            // Forêt : des anneaux d'arbres autour de la clairière.
            var rng = new System.Random(11);
            string[] trees = { "Pin_01", "Pin_02", "GrandPin_01", "GrandPin_02", "PinMaigre_01", "PinMaigre_02", "Pin_01", "GrandPin_01", "PinMort", "ArbreMort_01", "ArbreMort_02", "BouleauMort" };
            var clearing = new Rect(-1.5f, -size.y - 1.5f, size.x + 3f, size.y + 3f); // x, z (z vers le haut du rectangle)
            for (var n = 0; n < 90; n++)
            {
                var x = Mathf.Lerp(-22f, size.x + 22f, (float)rng.NextDouble());
                var z = Mathf.Lerp(-size.y - 20f, 14f, (float)rng.NextDouble());
                if (clearing.Contains(new Vector2(x, z)))
                    continue;
                // Pas devant la falaise ni sur la cabane.
                if (Vector2.Distance(new Vector2(x, z), new Vector2(cave.x, cave.z + 5f)) < 9f)
                    continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(-5.5f, -11f)) < 6f)
                    continue;
                var tree = trees[rng.Next(trees.Length)];
                PlaceFree(outside, tree, new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, 0.85f + (float)rng.NextDouble() * 0.5f);
            }

            // Collines à l'horizon.
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI * 2f / 8f + 0.3f;
                PlaceFree(outside, "Colline", center + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 70f, k * 47f, 2.2f);
            }
        }
    }
}
