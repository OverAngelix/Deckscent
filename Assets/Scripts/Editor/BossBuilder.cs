using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit les prefabs du boss et de son antre : loup-garou (modèle Synty riggé, animé os par os par
    /// <see cref="WerewolfRig"/>), louveteau, escalier, décor de l'antre (brasero, trône, cage, os,
    /// ossements), et les branche sur le Spawner des scènes. Menu Deckscent > … Relancer écrase les
    /// prefabs.
    ///
    /// Comme le reste du contenu : chaque pièce Synty a sa version de secours en formes simples
    /// (<see cref="DecorFallback"/>) si le pack manque (il n'est pas versionné).
    /// </summary>
    public static class BossBuilder
    {
        const string EnemyDir = "Assets/Prefabs/Enemies";
        const string PropDir = "Assets/Prefabs/Props";
        const string DecorDir = "Assets/Prefabs/Props/Antre";
        const string Toon = "Deckscent/Toon";
        const string SyntyDungeon = "Assets/Synty/PolygonDungeon/Prefabs/";
        const string WerewolfFbx = "Assets/Synty/PolygonWerewolf/Models/SM_Werewolf_01.fbx";
        const string WerewolfTexture = "Assets/Synty/PolygonWerewolf/Textures/PolygonWerewolf_01_A.png";
        const string SyntyToon = "Assets/Materials/Toon/M_Toon_PolygonDungeon_01.mat";

        public const string WerewolfPath = EnemyDir + "/LoupGarou.prefab";
        public const string CubPath = EnemyDir + "/Louveteau.prefab";
        public const string StairsPath = PropDir + "/Escalier.prefab";
        public static string DecorPath(string kind) => DecorDir + "/" + kind + ".prefab";

        /// <summary>Lueur propre du loup-garou : il se voit dans le noir, comme les slimes.</summary>
        const float WolfGlow = 0.35f;

        [MenuItem("Deckscent/Construire les prefabs du boss (loup-garou, antre, escalier)")]
        public static void BuildAll()
        {
            PrototypeBuilder.EnsureFolder(EnemyDir);
            PrototypeBuilder.EnsureFolder(DecorDir);
            BuildWolf(WerewolfPath, "LoupGarou", 1.3f, true);
            BuildWolf(CubPath, "Louveteau", 0.6f, false);
            BuildStairs();
            BuildDecor();
            AssetDatabase.SaveAssets();
            Debug.Log("Deckscent : prefabs du boss construits (loup-garou, louveteau, escalier, décor de l'antre).");
        }

        /// <summary>Branche les prefabs du boss sur le Spawner des scènes Main et Test_Donjon.</summary>
        [MenuItem("Deckscent/Ajouter le boss aux scènes")]
        public static void AddToScenes()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Deckscent : la scène ouverte a des changements non enregistrés. Enregistrer ou annuler, puis relancer.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WerewolfPath) == null)
                BuildAll();
            foreach (var path in new[] { "Assets/Scenes/Main.unity", PrototypeBuilder.DemoScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var spawner in Object.FindObjectsByType<Spawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Wire(spawner);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("Deckscent : boss branché sur le Spawner des scènes.");
        }

        public static void Wire(Spawner spawner)
        {
            var so = new SerializedObject(spawner);
            so.FindProperty("werewolfPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Werewolf>(WerewolfPath);
            so.FindProperty("cubPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WolfCub>(CubPath);
            so.FindProperty("stairsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Stairs>(StairsPath);
            so.FindProperty("braseroPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(DecorPath("Brasero"));
            so.FindProperty("tronePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(DecorPath("Trone"));
            so.FindProperty("cagePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(DecorPath("Cage"));
            so.FindProperty("osPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(DecorPath("Os"));
            so.FindProperty("ossementsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(DecorPath("Ossements"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Loup-garou et louveteau ---------------------------------------------------------

        static void BuildWolf(string path, string name, float scale, bool boss)
        {
            var root = new GameObject(name);
            var model = new GameObject("Modèle");
            model.transform.SetParent(root.transform, false);

            var furMat = PrototypeBuilder.SaveMaterial("M_Toon_LoupGarou", Toon, m =>
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WerewolfTexture);
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_TextureGain", texture != null ? 2.5f : 1f);
                m.SetFloat("_TextureSaturation", 0f);
                m.SetFloat("_Glow", WolfGlow);
            });
            var grey = PrototypeBuilder.SaveMaterial("M_Toon_Gris_Loup", Toon, m =>
            {
                m.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.57f));
                m.SetFloat("_TextureStrength", 0f);
                m.SetFloat("_Glow", WolfGlow);
            });

            // Le modèle Synty : la hiérarchie du FBX (os + maillage), dépaquetée (pas de prefab imbriqué).
            GameObject decor = null;
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(WerewolfFbx);
            if (fbx != null)
            {
                decor = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(decor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                // Pas d'Animator : les poses sont posées os par os (WerewolfRig).
                var animator = decor.GetComponent<Animator>();
                if (animator != null)
                    Object.DestroyImmediate(animator);
                // Une seule peau : la version « mort-vivant » est retirée.
                foreach (var smr in decor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.name.Contains("Undead"))
                    {
                        Object.DestroyImmediate(smr.gameObject);
                        continue;
                    }
                    var mats = smr.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                        mats[i] = furMat;
                    smr.sharedMaterials = mats;
                    smr.updateWhenOffscreen = true; // ses poses sortent des bornes du T-pose
                    smr.shadowCastingMode = ShadowCastingMode.On;
                }
                decor.transform.SetParent(model.transform, false);
                decor.transform.localScale = Vector3.one * scale;
            }
            else
                Debug.LogWarning("Deckscent : loup-garou Synty introuvable, seul le secours sera visible.");

            // Secours : un corps voûté en formes simples (bassin, buste, tête, oreilles, bras).
            var fallback = new GameObject("Secours");
            fallback.transform.SetParent(model.transform, false);
            fallback.transform.localScale = Vector3.one * scale;
            var body = new GameObject("Corps").transform;
            body.SetParent(fallback.transform, false);
            body.localPosition = new Vector3(0f, 0.9f, 0f);
            Shape("Buste", PrimitiveType.Capsule, grey, body, new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.55f, 0.5f));
            Shape("Tête", PrimitiveType.Sphere, grey, body, new Vector3(0f, 0.85f, 0.2f), new Vector3(0.38f, 0.36f, 0.45f));
            Shape("Museau", PrimitiveType.Cube, grey, body, new Vector3(0f, 0.8f, 0.45f), new Vector3(0.16f, 0.14f, 0.25f));
            foreach (var side in new[] { -1f, 1f })
            {
                Shape("Oreille", PrimitiveType.Cube, grey, body, new Vector3(side * 0.12f, 1.05f, 0.15f), new Vector3(0.07f, 0.18f, 0.05f));
                Shape("Bras", PrimitiveType.Capsule, grey, body, new Vector3(side * 0.45f, 0.2f, 0.15f), new Vector3(0.18f, 0.4f, 0.18f));
                Shape("Jambe", PrimitiveType.Capsule, grey, fallback.transform, new Vector3(side * 0.18f, 0.45f, 0f), new Vector3(0.22f, 0.45f, 0.22f));
            }

            var choice = model.AddComponent<DecorFallback>();
            var cso = new SerializedObject(choice);
            cso.FindProperty("decor").objectReferenceValue = decor;
            cso.FindProperty("fallback").objectReferenceValue = fallback;
            cso.ApplyModifiedPropertiesWithoutUndo();
            decor?.SetActive(true);
            fallback.SetActive(decor == null);

            var rig = model.AddComponent<WerewolfRig>();
            var rso = new SerializedObject(rig);
            rso.FindProperty("skeleton").objectReferenceValue = decor != null ? decor.transform : null;
            rso.FindProperty("fallbackBody").objectReferenceValue = body;
            rso.ApplyModifiedPropertiesWithoutUndo();

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var emitter = PrototypeBuilder.AddSoundEmitter(root, 1f);
            if (boss)
            {
                var wolf = root.AddComponent<Werewolf>();
                var so = new SerializedObject(wolf);
                so.FindProperty("maxHealth").intValue = 10;
                so.FindProperty("damage").intValue = 3;
                so.FindProperty("defense").intValue = 0;
                so.FindProperty("rig").objectReferenceValue = rig;
                so.FindProperty("sounds").objectReferenceValue = emitter;
                SetRenderers(so.FindProperty("tinted"), renderers);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var cub = root.AddComponent<WolfCub>();
                var so = new SerializedObject(cub);
                so.FindProperty("maxHealth").intValue = 2;
                so.FindProperty("damage").intValue = 1;
                so.FindProperty("rig").objectReferenceValue = rig;
                so.FindProperty("sounds").objectReferenceValue = emitter;
                SetRenderers(so.FindProperty("tinted"), renderers);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void SetRenderers(SerializedProperty list, Renderer[] renderers)
        {
            list.arraySize = renderers.Length;
            for (var i = 0; i < renderers.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }

        // --- Escalier --------------------------------------------------------------------------

        static void BuildStairs()
        {
            var stone = PrototypeBuilder.SaveMaterial("M_Toon_Gris_Escalier", Toon, m =>
            {
                m.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.75f));
                m.SetFloat("_TextureStrength", 0f);
                m.SetFloat("_Glow", 0.25f);
            });
            var root = new GameObject("Escalier");
            var decor = FitSynty("Décor", SyntyDungeon + "Environments/Floors/SM_Env_SpiralStairs_01.prefab", 2.6f, 3.4f, 0.25f);
            // Secours : un colimaçon de marches autour d'un pilier.
            var fallback = new GameObject("Secours");
            Shape("Pilier", PrimitiveType.Cylinder, stone, fallback.transform, new Vector3(0f, 1.6f, 0f), new Vector3(0.35f, 1.6f, 0.35f));
            for (var i = 0; i < 10; i++)
            {
                var step = Shape("Marche", PrimitiveType.Cube, stone, fallback.transform, Vector3.zero, new Vector3(1.1f, 0.12f, 0.45f));
                var a = i * 36f * Mathf.Deg2Rad;
                step.transform.localPosition = new Vector3(Mathf.Sin(a) * 0.7f, 0.15f + i * 0.3f, Mathf.Cos(a) * 0.7f);
                step.transform.localRotation = Quaternion.Euler(0f, i * 36f + 90f, 0f);
            }
            Pair(root, decor, fallback);
            // Une lueur blanche monte de l'escalier : on le repère de loin dans le noir.
            var lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            light.intensity = 2f;
            light.range = 7f;
            light.shadows = LightShadows.None;
            var stairs = root.AddComponent<Stairs>();
            var so = new SerializedObject(stairs);
            so.FindProperty("sounds").objectReferenceValue = PrototypeBuilder.AddSoundEmitter(root, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, StairsPath);
            Object.DestroyImmediate(root);
        }

        // --- Décor de l'antre --------------------------------------------------------------------

        static void BuildDecor()
        {
            var grey = PrototypeBuilder.SaveMaterial("M_Toon_Gris_Antre", Toon, m =>
            {
                m.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f));
                m.SetFloat("_TextureStrength", 0f);
            });
            var bone = PrototypeBuilder.SaveMaterial("M_Toon_Os", Toon, m =>
            {
                m.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.88f));
                m.SetFloat("_TextureStrength", 0f);
            });
            var flame = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Flamme.mat");

            // Brasero : la vasque Synty, une flamme et sa lumière (blanche, comme les torches).
            {
                var root = new GameObject("Brasero");
                var fallback = new GameObject("Secours");
                Shape("Pied", PrimitiveType.Cylinder, grey, fallback.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.15f, 0.45f, 0.15f));
                Shape("Vasque", PrimitiveType.Cylinder, grey, fallback.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.1f, 0.7f));
                Pair(root, FitSynty("Décor", SyntyDungeon + "Props/SM_Prop_Brazier_01.prefab", 1f, 1.3f), fallback);
                var fire = Shape("Flamme", PrimitiveType.Sphere, flame, root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.35f, 0.5f, 0.35f));
                fire.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var lightGo = new GameObject("Lumière");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 3f;
                light.range = 9f;
                // Sans ombre : jusqu'à six braseros par antre, en plus des torches (atlas d'ombres saturé).
                light.shadows = LightShadows.None;
                lightGo.AddComponent<TorchFlicker>();
                SaveDecor(root, true);
            }
            // Trône.
            {
                var root = new GameObject("Trone");
                var fallback = new GameObject("Secours");
                Shape("Siège", PrimitiveType.Cube, grey, fallback.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 1f));
                Shape("Dossier", PrimitiveType.Cube, grey, fallback.transform, new Vector3(0f, 1.3f, -0.4f), new Vector3(1.2f, 2f, 0.2f));
                Pair(root, FitSynty("Décor", SyntyDungeon + "Props/SM_Prop_Skeleton_Knight_Throne_01.prefab", 1.8f, 2.6f), fallback);
                SaveDecor(root, true);
            }
            // Cage.
            {
                var root = new GameObject("Cage");
                var fallback = new GameObject("Secours");
                for (var i = 0; i < 8; i++)
                {
                    var a = i * 45f * Mathf.Deg2Rad;
                    Shape("Barreau", PrimitiveType.Cylinder, grey, fallback.transform, new Vector3(Mathf.Sin(a) * 0.5f, 1f, Mathf.Cos(a) * 0.5f), new Vector3(0.05f, 1f, 0.05f));
                }
                Pair(root, FitSynty("Décor", SyntyDungeon + "Props/SM_Prop_Toture_Cage_01.prefab", 1.4f, 2.4f), fallback);
                SaveDecor(root, true);
            }
            // Tas d'os.
            {
                var root = new GameObject("Os");
                var fallback = new GameObject("Secours");
                Shape("Tas", PrimitiveType.Sphere, bone, fallback.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.4f, 1.1f));
                Shape("Crâne", PrimitiveType.Sphere, bone, fallback.transform, new Vector3(0.2f, 0.4f, 0.1f), Vector3.one * 0.25f);
                Pair(root, FitSynty("Décor", SyntyDungeon + "Environments/Bones/SM_Env_BonePile_01.prefab", 1.6f, 1.2f), fallback);
                SaveDecor(root, true);
            }
            // Ossements épars (de passage).
            {
                var root = new GameObject("Ossements");
                var fallback = new GameObject("Secours");
                Shape("Os", PrimitiveType.Capsule, bone, fallback.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.08f, 0.25f, 0.08f)).transform.localRotation = Quaternion.Euler(90f, 30f, 0f);
                Shape("Crâne", PrimitiveType.Sphere, bone, fallback.transform, new Vector3(0.3f, 0.1f, 0.2f), Vector3.one * 0.2f);
                Pair(root, FitSynty("Décor", SyntyDungeon + "Environments/Bones/SM_Env_BonePile_Small_01.prefab", 1f, 0.5f), fallback);
                SaveDecor(root, false);
            }
        }

        static void SaveDecor(GameObject root, bool blocks)
        {
            var decor = root.AddComponent<LairDecor>();
            var so = new SerializedObject(decor);
            so.FindProperty("blocks").boolValue = blocks;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, DecorPath(root.name));
            Object.DestroyImmediate(root);
        }

        // --- Outils ----------------------------------------------------------------------------

        /// <summary>
        /// Pièce Synty copiée (maillages seuls, matériau toon de l'atlas), mise à l'échelle pour tenir
        /// dans `width` × `height` mètres, centrée sur sa base. Vide si le pack manque.
        /// </summary>
        static GameObject FitSynty(string name, string path, float width, float height, float glow = 0f)
        {
            var go = PrototypeBuilder.FromSynty(name, path, Vector3.zero, Quaternion.identity, Vector3.one);
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
                return go;
            var toon = AssetDatabase.LoadAssetAtPath<Material>(SyntyToon);
            if (glow > 0f && toon != null)
            {
                var glowing = PrototypeBuilder.SaveMaterial("M_Toon_PolygonDungeon_Lueur", Toon, m =>
                {
                    m.CopyPropertiesFromMaterial(toon);
                    m.SetFloat("_Glow", glow);
                });
                toon = glowing;
            }
            var bounds = new Bounds();
            var first = true;
            foreach (var r in renderers)
            {
                if (toon != null)
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                        mats[i] = toon;
                    r.sharedMaterials = mats;
                }
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;
                var b = TransformBounds(mf.sharedMesh.bounds, go.transform.worldToLocalMatrix * r.transform.localToWorldMatrix);
                if (first)
                    bounds = b;
                else
                    bounds.Encapsulate(b);
                first = false;
            }
            if (first)
                return go;
            var piece = go.transform.GetChild(0);
            var s = Mathf.Min(width / Mathf.Max(bounds.size.x, bounds.size.z, 0.01f), height / Mathf.Max(bounds.size.y, 0.01f));
            piece.localScale = Vector3.one * s;
            piece.localPosition = new Vector3(-bounds.center.x * s, -bounds.min.y * s, -bounds.center.z * s);
            return go;
        }

        static Bounds TransformBounds(Bounds b, Matrix4x4 m)
        {
            var result = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                result.Encapsulate(m.MultiplyPoint3x4(corner));
            }
            return result;
        }

        static void Pair(GameObject root, GameObject decor, GameObject fallback)
        {
            decor.transform.SetParent(root.transform, false);
            fallback.transform.SetParent(root.transform, false);
            decor.name = "Décor";
            fallback.name = "Secours";
            var choice = root.AddComponent<DecorFallback>();
            var so = new SerializedObject(choice);
            so.FindProperty("decor").objectReferenceValue = decor;
            so.FindProperty("fallback").objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();
            var synty = decor.GetComponentsInChildren<MeshFilter>(true).Length > 0;
            decor.SetActive(synty);
            fallback.SetActive(!synty);
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
    }
}
