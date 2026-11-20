using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit les prefabs du décor d'ambiance des salles et des couloirs (<see cref="Ambience"/>) :
    /// vase et caisse à briser (<see cref="Breakable"/>), statue, bougies, crâne, os
    /// (<see cref="LairDecor"/>), et la fontaine qui soigne (<see cref="Shrine"/>), puis les branche
    /// sur le Spawner des scènes. Pièces Synty (POLYGON Dungeon) avec leur secours en formes simples.
    /// Relancer écrase les prefabs.
    /// </summary>
    public static class DecorBuilder
    {
        const string Dir = "Assets/Prefabs/Props/Decor";
        const string Toon = "Deckscent/Toon";
        const string Synty = "Assets/Synty/PolygonDungeon/Prefabs/";

        static string PathOf(string name) => Dir + "/" + name + ".prefab";

        [MenuItem("Deckscent/Construire les prefabs du décor (vases, caisses, statues, fontaine)")]
        public static void BuildAll()
        {
            PrototypeBuilder.EnsureFolder(Dir);
            var grey = PrototypeBuilder.SaveMaterial("M_Toon_Gris_Decor", Toon, m =>
            {
                m.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f));
                m.SetFloat("_TextureStrength", 0f);
            });
            var bone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Toon_Os.mat") ?? grey;
            var flame = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Flamme.mat");

            // Vase à briser.
            {
                var root = new GameObject("Vase");
                var fallback = new GameObject("Secours");
                BossBuilder.Shape("Panse", PrimitiveType.Sphere, grey, fallback.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.75f, 0.6f));
                BossBuilder.Shape("Col", PrimitiveType.Cylinder, grey, fallback.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.3f, 0.12f, 0.3f));
                SaveBreakable(root, BossBuilder.FitSynty("Décor", Synty + "Props/SM_Prop_Vase_02.prefab", 0.8f, 1.1f, 0.25f), fallback, 0.3f, 0.06f);
            }
            // Caisse à briser.
            {
                var root = new GameObject("Caisse");
                var fallback = new GameObject("Secours");
                BossBuilder.Shape("Caisse", PrimitiveType.Cube, grey, fallback.transform, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.9f);
                SaveBreakable(root, BossBuilder.FitSynty("Décor", Synty + "Props/SM_Prop_Crate_Wood_01.prefab", 1.1f, 1f, 0.25f), fallback, 0.2f, 0.12f, true);
            }
            // Statue (bloque).
            {
                var root = new GameObject("Statue");
                var fallback = new GameObject("Secours");
                BossBuilder.Shape("Socle", PrimitiveType.Cube, grey, fallback.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.9f));
                BossBuilder.Shape("Corps", PrimitiveType.Capsule, grey, fallback.transform, new Vector3(0f, 1.4f, 0f), new Vector3(0.6f, 0.9f, 0.5f));
                SaveDecor(root, BossBuilder.FitSynty("Décor", Synty + "Environments/Misc/SM_Env_Statue_01.prefab", 1.3f, 2.9f), fallback, true);
            }
            // Bougies (de passage), allumées.
            {
                var root = new GameObject("Bougies");
                var fallback = new GameObject("Secours");
                for (var i = 0; i < 3; i++)
                    BossBuilder.Shape("Bougie", PrimitiveType.Cylinder, bone, fallback.transform,
                        new Vector3(-0.15f + i * 0.15f, 0.12f + i * 0.04f, (i % 2) * 0.1f), new Vector3(0.07f, 0.12f + i * 0.04f, 0.07f));
                var decor = BossBuilder.FitSynty("Décor", Synty + "Props/SM_Prop_Candles_01.prefab", 0.6f, 0.5f);
                var fire = BossBuilder.Shape("Flamme", PrimitiveType.Sphere, flame, root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.08f, 0.14f, 0.08f));
                fire.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                // Bougies allumées : une petite lumière blanche qui vacille, ombres en basse résolution
                // (sans ombre, elle traversait les murs).
                var lightGo = new GameObject("Lumière");
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.6f, 0.15f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 1.4f;
                light.range = 3.5f;
                BossBuilder.LowShadows(light);
                var flicker = lightGo.AddComponent<TorchFlicker>();
                var fso = new SerializedObject(flicker);
                fso.FindProperty("baseIntensity").floatValue = 1.4f;
                fso.FindProperty("flicker").floatValue = 0.25f;
                fso.ApplyModifiedPropertiesWithoutUndo();
                SaveDecor(root, decor, fallback, false);
            }
            // Crâne (de passage).
            {
                var root = new GameObject("Crane");
                var fallback = new GameObject("Secours");
                BossBuilder.Shape("Crâne", PrimitiveType.Sphere, bone, fallback.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.28f, 0.25f, 0.32f));
                SaveDecor(root, BossBuilder.FitSynty("Décor", Synty + "Environments/Bones/SM_Env_Bone_Skull_01.prefab", 0.4f, 0.35f), fallback, false);
            }
            // Quelques os (de passage).
            {
                var root = new GameObject("OsPetits");
                var fallback = new GameObject("Secours");
                BossBuilder.Shape("Os", PrimitiveType.Capsule, bone, fallback.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.07f, 0.22f, 0.07f))
                    .transform.localRotation = Quaternion.Euler(90f, 20f, 0f);
                BossBuilder.Shape("Os", PrimitiveType.Capsule, bone, fallback.transform, new Vector3(0.15f, 0.05f, 0.1f), new Vector3(0.07f, 0.18f, 0.07f))
                    .transform.localRotation = Quaternion.Euler(90f, 100f, 0f);
                SaveDecor(root, BossBuilder.FitSynty("Décor", Synty + "Environments/Bones/SM_Env_BonePile_Small_02.prefab", 0.8f, 0.4f), fallback, false);
            }
            BuildShrine(grey);
            BuildMimic();
            AssetDatabase.SaveAssets();
            Debug.Log("Deckscent : prefabs du décor construits (vase, caisse, statue, bougies, crâne, os, fontaine).");
        }

        /// <summary>Fontaine de pierre : la vasque Synty, une eau bleue saturée qui luit (accent), une lueur blanche.</summary>
        static void BuildShrine(Material grey)
        {
            var waterMat = PrototypeBuilder.SaveMaterial("M_Toon_Eau", Toon, m =>
            {
                m.SetColor("_BaseColor", new Color(0.15f, 0.45f, 0.95f));
                m.SetFloat("_TextureStrength", 0f);
                m.SetFloat("_Glow", 0.5f);
            });
            var root = new GameObject("Fontaine");
            var fallback = new GameObject("Secours");
            BossBuilder.Shape("Vasque", PrimitiveType.Cylinder, grey, fallback.transform, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.35f, 1.2f));
            BossBuilder.Pair(root, BossBuilder.FitSynty("Décor", Synty + "Environments/Misc/SM_Env_Stone_Fountain_01.prefab", 1.5f, 1.8f, 0.25f), fallback);
            var water = BossBuilder.Shape("Eau", PrimitiveType.Cylinder, waterMat, root.transform, new Vector3(0f, 0.62f, -0.02f), new Vector3(0.75f, 0.02f, 0.36f));
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0.3f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            light.intensity = 1.5f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            var shrine = root.AddComponent<Shrine>();
            var so = new SerializedObject(shrine);
            so.FindProperty("water").objectReferenceValue = water.GetComponent<MeshRenderer>();
            so.FindProperty("glow").objectReferenceValue = light;
            so.FindProperty("sounds").objectReferenceValue = PrototypeBuilder.AddSoundEmitter(root, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PathOf("Fontaine"));
            Object.DestroyImmediate(root);
        }

        public const string MimicPath = "Assets/Prefabs/Enemies/Mimique.prefab";

        /// <summary>
        /// Mimique : une copie du prefab Coffre (mêmes pièces, même dorure, mêmes couvercles), sans son
        /// script de coffre, avec des dents blanches et une langue rouge (accent) cachées dessous.
        /// </summary>
        static void BuildMimic()
        {
            var root = PrefabUtility.LoadPrefabContents(ContentBuilder.ChestPath);
            try
            {
                root.name = "Mimique";
                var chest = root.GetComponent<Chest>();
                var lids = new System.Collections.Generic.List<Transform>();
                var lidsProp = new SerializedObject(chest).FindProperty("lids");
                for (var i = 0; i < lidsProp.arraySize; i++)
                    if (lidsProp.GetArrayElementAtIndex(i).objectReferenceValue is Transform t)
                        lids.Add(t);
                Object.DestroyImmediate(chest);

                // La gueule (le coffre regarde +Z) : dents du bas sur le bord avant de la caisse, crocs
                // sous le bord avant du couvercle (ils suivent son ouverture), langue au fond de la caisse.
                var hinge = lids.Find(l => l != null && l.gameObject.activeInHierarchy);
                var caisse = new Bounds();
                var lid = new Bounds();
                bool hasCaisse = false, hasLid = false;
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (!r.gameObject.activeInHierarchy)
                        continue;
                    var underLid = hinge != null && r.transform.IsChildOf(hinge);
                    if (underLid)
                    {
                        if (hasLid) lid.Encapsulate(r.bounds); else lid = r.bounds;
                        hasLid = true;
                    }
                    else
                    {
                        if (hasCaisse) caisse.Encapsulate(r.bounds); else caisse = r.bounds;
                        hasCaisse = true;
                    }
                }
                var white = PrototypeBuilder.SaveMaterial("M_Toon_Blanc", Toon, m => { m.SetColor("_BaseColor", Color.white); m.SetFloat("_TextureStrength", 0f); });
                var red = PrototypeBuilder.SaveMaterial("M_Toon_Langue", Toon, m =>
                {
                    m.SetColor("_BaseColor", new Color(0.9f, 0.1f, 0.12f));
                    m.SetFloat("_TextureStrength", 0f);
                    m.SetFloat("_Glow", 0.3f);
                });
                var lower = new GameObject("Dents_Bas");
                lower.transform.SetParent(root.transform, false);
                const int teeth = 6;
                var rim = caisse.max.y;
                var front = caisse.max.z - 0.07f;
                for (var i = 0; i < teeth; i++)
                {
                    var x = Mathf.Lerp(caisse.min.x + 0.12f, caisse.max.x - 0.12f, i / (teeth - 1f));
                    var tooth = BossBuilder.Shape("Dent", PrimitiveType.Cube, white, lower.transform, new Vector3(x, rim + 0.03f, front), new Vector3(0.06f, 0.06f, 0.04f));
                    tooth.transform.localRotation = Quaternion.Euler(0f, 0f, 45f); // pointe vers le haut
                }
                // La langue, couchée au fond de la caisse, vers l'arrière (elle ne traverse pas les parois).
                BossBuilder.Shape("Langue", PrimitiveType.Cube, red, lower.transform,
                    new Vector3(caisse.center.x, Mathf.Lerp(caisse.min.y, rim, 0.6f), caisse.center.z - caisse.extents.z * 0.15f),
                    new Vector3(caisse.size.x * 0.3f, 0.05f, caisse.size.z * 0.45f));
                var parts = new System.Collections.Generic.List<GameObject> { lower };
                if (hinge != null && hasLid)
                {
                    // Crocs sous le bord avant du couvercle, accrochés à la charnière.
                    var upper = new GameObject("Dents_Haut");
                    upper.transform.SetParent(hinge, true);
                    var lidFront = lid.max.z - 0.07f;
                    for (var i = 0; i < teeth - 1; i++)
                    {
                        var x = Mathf.Lerp(lid.min.x + 0.18f, lid.max.x - 0.18f, i / (teeth - 2f));
                        var fang = BossBuilder.Shape("Croc", PrimitiveType.Cube, white, root.transform, new Vector3(x, lid.min.y - 0.03f, lidFront), new Vector3(0.06f, 0.06f, 0.04f));
                        fang.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        fang.transform.SetParent(upper.transform, true);
                    }
                    parts.Add(upper);
                }
                foreach (var part in parts)
                {
                    foreach (var r in part.GetComponentsInChildren<MeshRenderer>())
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    part.SetActive(false);
                }

                var mimic = root.AddComponent<Mimic>();
                var so = new SerializedObject(mimic);
                so.FindProperty("maxHealth").intValue = 5;
                so.FindProperty("damage").intValue = 2;
                var mp = so.FindProperty("maw");
                mp.arraySize = parts.Count;
                for (var i = 0; i < parts.Count; i++)
                    mp.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
                var lp = so.FindProperty("lids");
                lp.arraySize = lids.Count;
                for (var i = 0; i < lids.Count; i++)
                    lp.GetArrayElementAtIndex(i).objectReferenceValue = lids[i];
                so.FindProperty("sounds").objectReferenceValue = root.GetComponentInChildren<SoundEmitter>() ?? PrototypeBuilder.AddSoundEmitter(root, 1f);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, MimicPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SaveBreakable(GameObject root, GameObject decor, GameObject fallback, float potion, float card, bool wooden = false)
        {
            // Le corps (ce qui se brise) regroupe le décor et son secours.
            var body = new GameObject("Corps");
            body.transform.SetParent(root.transform, false);
            BossBuilder.Pair(body, decor, fallback);
            var breakable = root.AddComponent<Breakable>();
            var so = new SerializedObject(breakable);
            so.FindProperty("potionChance").floatValue = potion;
            so.FindProperty("cardChance").floatValue = card;
            so.FindProperty("wooden").boolValue = wooden;
            so.FindProperty("body").objectReferenceValue = body.transform;
            so.FindProperty("sounds").objectReferenceValue = PrototypeBuilder.AddSoundEmitter(root, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PathOf(root.name));
            Object.DestroyImmediate(root);
        }

        static void SaveDecor(GameObject root, GameObject decor, GameObject fallback, bool blocks)
        {
            BossBuilder.Pair(root, decor, fallback);
            var piece = root.AddComponent<LairDecor>();
            var so = new SerializedObject(piece);
            so.FindProperty("blocks").boolValue = blocks;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PathOf(root.name));
            Object.DestroyImmediate(root);
        }

        /// <summary>Branche le décor sur le Spawner des scènes Main et Test_Donjon.</summary>
        [MenuItem("Deckscent/Ajouter le décor aux scènes")]
        public static void AddToScenes()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Deckscent : la scène ouverte a des changements non enregistrés. Enregistrer ou annuler, puis relancer.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PathOf("Vase")) == null)
                BuildAll();
            foreach (var path in new[] { "Assets/Scenes/Main.unity", PrototypeBuilder.DemoScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var spawner in Object.FindObjectsByType<Spawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Wire(spawner);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("Deckscent : décor branché sur le Spawner des scènes.");
        }

        public static void Wire(Spawner spawner)
        {
            var so = new SerializedObject(spawner);
            so.FindProperty("vasePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Breakable>(PathOf("Vase"));
            so.FindProperty("caissePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Breakable>(PathOf("Caisse"));
            so.FindProperty("statuePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(PathOf("Statue"));
            so.FindProperty("bougiesPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(PathOf("Bougies"));
            so.FindProperty("cranePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(PathOf("Crane"));
            so.FindProperty("osPetitsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LairDecor>(PathOf("OsPetits"));
            so.FindProperty("shrinePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shrine>(PathOf("Fontaine"));
            so.FindProperty("mimicPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Mimic>(MimicPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
