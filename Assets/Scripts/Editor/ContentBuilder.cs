using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit les prefabs du contenu des tuiles (slime, dalle à pics, coffre) et du HUD, et les
    /// branche dans les scènes (Spawner, HUD). Menu Deckscent > … Relancer écrase les prefabs :
    /// une fois réglés à la main, ne le relancer que pour repartir de zéro.
    ///
    /// Dalle et coffre : pièces Synty (POLYGON Dungeon) copiées, avec une version de secours en
    /// formes simples (<see cref="DecorFallback"/>) si le pack manque. Le slime est en formes
    /// simples (aucun slime dans les packs).
    /// </summary>
    public static class ContentBuilder
    {
        const string PrefabDir = "Assets/Prefabs";
        const string EnemyDir = PrefabDir + "/Enemies";
        const string TrapDir = PrefabDir + "/Traps";
        const string PropDir = PrefabDir + "/Props";
        const string UiDir = PrefabDir + "/UI";
        const string MaterialDir = "Assets/Materials";
        const string MeshDir = "Assets/Meshes";
        const string Synty = "Assets/Synty/PolygonDungeon/Prefabs/";
        const string SyntyToon = MaterialDir + "/Toon/M_Toon_PolygonDungeon_01.mat";
        const string Toon = "Deckscent/Toon";

        /// <summary>Lueur propre des slimes et des coffres (Toon._Glow) : lisibles dans le noir.</summary>
        const float SlimeGlow = 0.25f, ChestGlow = 0.2f;

        public const string SlimePath = EnemyDir + "/Slime.prefab";
        public const string SpikeTrapPath = TrapDir + "/DallePics.prefab";
        public const string ChestPath = PropDir + "/Coffre.prefab";
        public const string HudPath = UiDir + "/Hud.prefab";

        [MenuItem("Deckscent/Construire les prefabs du contenu (slime, dalle, coffre, HUD)")]
        public static void BuildAll()
        {
            BuildSlime();
            BuildSpikeTrap();
            BuildChest();
            BuildHud();
            AssetDatabase.SaveAssets();
            Debug.Log("Deckscent : prefabs du contenu construits (slime, dalle à pics, coffre, HUD).");
        }

        /// <summary>Ajoute le Spawner et le HUD aux scènes Main et Test_Donjon (et le HUD à la scène de test), sans les reconstruire.</summary>
        [MenuItem("Deckscent/Ajouter le contenu aux scènes")]
        public static void AddToScenes()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Deckscent : la scène ouverte a des changements non enregistrés. Enregistrer ou annuler, puis relancer.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SlimePath) == null)
                BuildAll();
            foreach (var (path, spawner) in new[] { ("Assets/Scenes/Main.unity", true), (PrototypeBuilder.DemoScenePath, true), (PrototypeBuilder.TestScenePath, false) })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (spawner)
                    AddSpawnerToScene(scene);
                AddHudToScene(scene);
                // Le prefab du joueur a peut-être de nouvelles actions (Interact).
                EditorSceneManager.SaveScene(scene);
            }
            var player = PrefabUtility.LoadPrefabContents(PrefabDir + "/Player.prefab");
            PrototypeBuilder.ConfigurePlayer(player);
            PrefabUtility.SaveAsPrefabAsset(player, PrefabDir + "/Player.prefab");
            PrefabUtility.UnloadPrefabContents(player);
            Debug.Log("Deckscent : Spawner et HUD ajoutés aux scènes.");
        }

        // --- Slime -------------------------------------------------------------------

        static void BuildSlime()
        {
            PrototypeBuilder.EnsureFolder(EnemyDir);
            // Vert : la couleur de chaque sorte passe par un MaterialPropertyBlock (Slime.SetColor).
            var jellyMat = ToonMaterial("M_Toon_Slime", new Color(0.25f, 0.85f, 0.2f), 0f);
            // Lueur propre : sans elle, un slime à deux cases se perd dans le noir (version sombre).
            jellyMat.SetFloat("_Glow", SlimeGlow);
            var white = ToonMaterial("M_Toon_Blanc", Color.white, 0f);
            white.SetFloat("_Glow", SlimeGlow); // les yeux restent blancs (papier) dans le noir
            var ink = ToonMaterial("M_Toon_Encre", new Color(0.02f, 0.02f, 0.02f), 0f);

            var root = new GameObject("Slime");
            // Le corps s'écrase depuis le sol : son pivot est en bas.
            var body = new GameObject("Corps").transform;
            body.SetParent(root.transform, false);
            const float radius = 0.6f, height = 0.9f;
            var jelly = Shape("Gelée", PrimitiveType.Sphere, jellyMat, body,
                new Vector3(0f, height * 0.5f, 0f), new Vector3(radius * 2f, height, radius * 2f));
            // Les yeux regardent vers +Z (l'avant d'Unity) : le slime se tourne vers le joueur.
            foreach (var side in new[] { -1f, 1f })
            {
                var eye = Shape(side < 0 ? "Œil_G" : "Œil_D", PrimitiveType.Sphere, white, body,
                    new Vector3(side * radius * 0.35f, height * 0.62f, radius * 0.82f), Vector3.one * radius * 0.4f);
                var pupil = Shape("Pupille", PrimitiveType.Sphere, ink, eye.transform, new Vector3(0f, 0f, 0.35f), Vector3.one * 0.5f);
                pupil.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                eye.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            var slime = root.AddComponent<Slime>();
            var so = new SerializedObject(slime);
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("jelly").objectReferenceValue = jelly.GetComponent<MeshRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, SlimePath);
            Object.DestroyImmediate(root);
        }

        // --- Dalle à pics --------------------------------------------------------------

        static void BuildSpikeTrap()
        {
            PrototypeBuilder.EnsureFolder(TrapDir);
            var plateGrey = ToonMaterial("M_Toon_Gris_Dalle", new Color(0.5f, 0.5f, 0.5f), 0f);
            var spikeGrey = ToonMaterial("M_Toon_Gris_Pics", new Color(0.85f, 0.85f, 0.85f), 0f);
            var holeInk = ToonMaterial("M_Toon_Encre", new Color(0.02f, 0.02f, 0.02f), 0f);
            const float spikeHeight = 1f;

            var root = new GameObject("DallePics");

            // Dalle : grille Synty, ou plaque grise percée de trous.
            var plate = Pair("Dalle", root.transform,
                FromSynty("Grille", Synty + "Environments/Misc/SM_Trap_Spears_Grate_01.prefab", new Vector3(1f, 0.35f, 1f)),
                PlateFallback(plateGrey, holeInk));

            // Pics : piques Synty, ou cônes gris. Leur point le plus haut est à spikeHeight.
            var spikes = Pair("Pics", root.transform,
                FromSynty("Piques", Synty + "Environments/Misc/SM_Trap_Spears_01.prefab", new Vector3(1.1f, spikeHeight / 2.36f, 1.1f)),
                SpikesFallback(spikeGrey, spikeHeight));
            foreach (var r in spikes.GetComponentsInChildren<MeshRenderer>(true))
                r.shadowCastingMode = ShadowCastingMode.Off; // cachés sous le sol : pas d'ombre parasite

            var trap = root.AddComponent<SpikeTrap>();
            var so = new SerializedObject(trap);
            so.FindProperty("spikes").objectReferenceValue = spikes.transform;
            so.FindProperty("spikeHeight").floatValue = spikeHeight;
            so.ApplyModifiedPropertiesWithoutUndo();
            _ = plate;
            PrefabUtility.SaveAsPrefabAsset(root, SpikeTrapPath);
            Object.DestroyImmediate(root);
        }

        static GameObject PlateFallback(Material grey, Material ink)
        {
            var root = new GameObject("Plaque");
            const float size = 2.4f, perSide = 4;
            Shape("Dalle", PrimitiveType.Cube, grey, root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(size, 0.06f, size));
            var step = size / perSide;
            for (var i = 0; i < perSide; i++)
                for (var j = 0; j < perSide; j++)
                    Shape("Trou", PrimitiveType.Cylinder, ink, root.transform,
                        new Vector3((i + 0.5f) * step - size * 0.5f, 0.061f, (j + 0.5f) * step - size * 0.5f),
                        new Vector3(step * 0.44f, 0.001f, step * 0.44f));
            return root;
        }

        static GameObject SpikesFallback(Material grey, float height)
        {
            var root = new GameObject("Cônes");
            var cone = ConeMesh();
            const float size = 2.4f, perSide = 4;
            var step = size / perSide;
            for (var i = 0; i < perSide; i++)
                for (var j = 0; j < perSide; j++)
                {
                    var spike = new GameObject("Pic");
                    spike.transform.SetParent(root.transform, false);
                    spike.transform.localPosition = new Vector3((i + 0.5f) * step - size * 0.5f, 0f, (j + 0.5f) * step - size * 0.5f);
                    spike.transform.localScale = new Vector3(step * 0.4f, height, step * 0.4f);
                    spike.AddComponent<MeshFilter>().sharedMesh = cone;
                    spike.AddComponent<MeshRenderer>().sharedMaterial = grey;
                }
            return root;
        }

        /// <summary>Cône de diamètre 1, hauteur 1, base à l'origine (enregistré dans Assets/Meshes).</summary>
        static Mesh ConeMesh()
        {
            var path = MeshDir + "/Cone.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
                return mesh;
            PrototypeBuilder.EnsureFolder(MeshDir);
            const int segments = 10;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            // Flancs : un triangle par segment, sommets dupliqués (arêtes nettes, rendu facetté).
            for (var i = 0; i < segments; i++)
            {
                var a0 = i * Mathf.PI * 2f / segments;
                var a1 = (i + 1) * Mathf.PI * 2f / segments;
                var b = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f));
                verts.Add(Vector3.up);
                verts.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f));
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
            mesh = new Mesh { name = "Cone" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // --- Coffre ----------------------------------------------------------------------

        static void BuildChest()
        {
            PrototypeBuilder.EnsureFolder(PropDir);
            var gold = new Color(0.95f, 0.72f, 0.1f);
            var syntyToon = AssetDatabase.LoadAssetAtPath<Material>(SyntyToon);
            var atlas = syntyToon != null ? syntyToon.GetTexture("_BaseMap") : null;
            // Doré saturé : le post-process le garde en couleur. Texture de l'atlas en gris dessous.
            var goldMat = PrototypeBuilder.SaveMaterial("M_Toon_Coffre", Toon, m =>
            {
                m.SetColor("_BaseColor", gold);
                m.SetTexture("_BaseMap", atlas);
                m.SetFloat("_TextureGain", atlas != null ? 2.5f : 1f);
                m.SetFloat("_TextureSaturation", 0f);
                m.SetFloat("_Glow", ChestGlow);
            });
            var plainGold = ToonMaterial("M_Toon_Or", gold, 0f);
            plainGold.SetFloat("_Glow", ChestGlow);
            var iron = ToonMaterial("M_Toon_Encre", new Color(0.02f, 0.02f, 0.02f), 0f);

            var root = new GameObject("Coffre");

            // Décor : caisse et couvercle Synty, le couvercle sous une charnière (son pivot Synty).
            var decor = new GameObject("Décor");
            Transform decorHinge = null;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Synty + "Props/SM_Prop_Chest_03.prefab");
            if (source != null)
            {
                foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
                {
                    var isLid = filter.name.EndsWith("_Lid");
                    var parent = decor.transform;
                    if (isLid)
                    {
                        decorHinge = new GameObject("Charnière").transform;
                        decorHinge.SetParent(decor.transform, false);
                        decorHinge.localPosition = filter.transform.localPosition;
                        parent = decorHinge;
                    }
                    var part = new GameObject(isLid ? "Couvercle" : "Caisse");
                    part.transform.SetParent(parent, false);
                    if (!isLid)
                        part.transform.localPosition = filter.transform.localPosition;
                    part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    part.AddComponent<MeshRenderer>().sharedMaterial = goldMat;
                }
            }
            else
                Debug.LogWarning("Deckscent : coffre Synty introuvable, seul le coffre de secours sera visible.");

            // Secours : caisse en blocs, ferrures et serrure à l'encre. Le devant regarde +Z.
            var fallback = new GameObject("Secours");
            const float W = 1f, D = 0.65f, H = 0.5f, Lid = 0.22f;
            Shape("Caisse", PrimitiveType.Cube, plainGold, fallback.transform, new Vector3(0f, H * 0.5f, 0f), new Vector3(W, H, D));
            Shape("Ferrure_G", PrimitiveType.Cube, iron, fallback.transform, new Vector3(-W * 0.32f, H * 0.5f, 0f), new Vector3(0.08f, H + 0.01f, D + 0.02f));
            Shape("Ferrure_D", PrimitiveType.Cube, iron, fallback.transform, new Vector3(W * 0.32f, H * 0.5f, 0f), new Vector3(0.08f, H + 0.01f, D + 0.02f));
            var hinge = new GameObject("Charnière").transform;
            hinge.SetParent(fallback.transform, false);
            hinge.localPosition = new Vector3(0f, H, -D * 0.5f);
            Shape("Couvercle", PrimitiveType.Cube, plainGold, hinge, new Vector3(0f, Lid * 0.5f, D * 0.5f), new Vector3(W, Lid, D));
            Shape("Ferrure_G", PrimitiveType.Cube, iron, hinge, new Vector3(-W * 0.32f, Lid * 0.5f, D * 0.5f), new Vector3(0.08f, Lid + 0.01f, D + 0.02f));
            Shape("Ferrure_D", PrimitiveType.Cube, iron, hinge, new Vector3(W * 0.32f, Lid * 0.5f, D * 0.5f), new Vector3(0.08f, Lid + 0.01f, D + 0.02f));
            Shape("Serrure", PrimitiveType.Cube, iron, hinge, new Vector3(0f, 0f, D + 0.02f), new Vector3(0.16f, 0.2f, 0.06f));

            Pair(null, root.transform, decor, fallback);
            var chest = root.AddComponent<Chest>();
            var so = new SerializedObject(chest);
            var lids = so.FindProperty("lids");
            lids.arraySize = decorHinge != null ? 2 : 1;
            lids.GetArrayElementAtIndex(0).objectReferenceValue = hinge;
            if (decorHinge != null)
                lids.GetArrayElementAtIndex(1).objectReferenceValue = decorHinge;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ChestPath);
            Object.DestroyImmediate(root);
        }

        // --- HUD -----------------------------------------------------------------------------

        static void BuildHud()
        {
            PrototypeBuilder.EnsureFolder(UiDir);
            var root = new GameObject("Hud", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // sous la carte (100)
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1152f, 648f);
            scaler.matchWidthOrHeight = 0.5f;

            var inkGo = new GameObject("Encre", typeof(RectTransform));
            inkGo.transform.SetParent(root.transform, false);
            PrototypeBuilder.Stretch((RectTransform)inkGo.transform);
            var ink = inkGo.AddComponent<InkCanvas>();
            ink.raycastTarget = false;
            var textsGo = new GameObject("Textes", typeof(RectTransform));
            textsGo.transform.SetParent(root.transform, false);
            PrototypeBuilder.Stretch((RectTransform)textsGo.transform);
            var texts = textsGo.AddComponent<TextPool>();

            var hud = root.AddComponent<Hud>();
            var so = new SerializedObject(hud);
            so.FindProperty("ink").objectReferenceValue = ink;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("restart").objectReferenceValue = PrototypeBuilder.ActionReference("Player", "Wait");
            so.FindProperty("restartAlt").objectReferenceValue = PrototypeBuilder.ActionReference("Player", "Interact");
            so.FindProperty("toMenu").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Cancel");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            Object.DestroyImmediate(root);
        }

        // --- Scènes ----------------------------------------------------------------------------

        /// <summary>Ajoute le Spawner (contenu des tuiles posées) à la scène, s'il n'y est pas.</summary>
        public static void AddSpawnerToScene(Scene scene)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SlimePath) == null)
                BuildAll();
            Spawner spawner = null;
            foreach (var go in scene.GetRootGameObjects())
                spawner ??= go.GetComponentInChildren<Spawner>(true);
            if (spawner == null)
            {
                var go = new GameObject("Contenu");
                SceneManager.MoveGameObjectToScene(go, scene);
                spawner = go.AddComponent<Spawner>();
            }
            var so = new SerializedObject(spawner);
            so.FindProperty("dungeon").objectReferenceValue = Object.FindAnyObjectByType<Dungeon>();
            so.FindProperty("slimePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Slime>(SlimePath);
            so.FindProperty("spikeTrapPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SpikeTrap>(SpikeTrapPath);
            so.FindProperty("chestPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Chest>(ChestPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Ajoute le HUD à la scène, s'il n'y est pas.</summary>
        public static void AddHudToScene(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponentInChildren<Hud>(true) != null)
                    return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null)
                BuildHud();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // --- Outils ------------------------------------------------------------------------------

        /// <summary>Un nœud `name` (ou `parent` lui-même si null) avec le décor et son secours, et le DecorFallback qui choisit.</summary>
        static GameObject Pair(string name, Transform parent, GameObject decor, GameObject fallback)
        {
            GameObject node;
            if (name == null)
                node = parent.gameObject;
            else
            {
                node = new GameObject(name);
                node.transform.SetParent(parent, false);
            }
            decor.name = "Décor";
            fallback.name = "Secours";
            decor.transform.SetParent(node.transform, false);
            fallback.transform.SetParent(node.transform, false);
            var choice = node.AddComponent<DecorFallback>();
            var so = new SerializedObject(choice);
            so.FindProperty("decor").objectReferenceValue = decor;
            so.FindProperty("fallback").objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();
            var synty = HasMeshes(decor);
            decor.SetActive(synty);
            fallback.SetActive(!synty);
            return node;
        }

        static bool HasMeshes(GameObject go)
        {
            var filters = go.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0)
                return false;
            foreach (var f in filters)
                if (f.sharedMesh == null)
                    return false;
            return true;
        }

        /// <summary>Pièce Synty copiée (maillage seul), centrée, à l'échelle donnée, avec le matériau toon de l'atlas.</summary>
        static GameObject FromSynty(string name, string path, Vector3 scale)
        {
            var go = PrototypeBuilder.FromSynty(name, path, Vector3.zero, Quaternion.identity, scale);
            var toon = AssetDatabase.LoadAssetAtPath<Material>(SyntyToon);
            if (toon != null)
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                        mats[i] = toon;
                    r.sharedMaterials = mats;
                }
            return go;
        }

        static GameObject Shape(string name, PrimitiveType type, Material material, Transform parent, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Matériau toon uni (sans texture), créé ou mis à jour dans Assets/Materials.</summary>
        static Material ToonMaterial(string name, Color color, float textureStrength) =>
            PrototypeBuilder.SaveMaterial(name, Toon, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_TextureStrength", textureStrength);
            });
    }
}
