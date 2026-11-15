using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;

namespace Deckscent.Editor
{
    /// <summary>
    /// Menu Deckscent > Construire la scène du menu : le prefab de l'écran titre
    /// (Prefabs/UI/EcranTitre) et la scène Menu (une salle du donjon, la caméra qui tourne, la
    /// lanterne, la musique), mise en tête des scènes du build (Menu, puis Main).
    /// Relancer écrase le prefab et la scène.
    /// </summary>
    public static class MenuBuilder
    {
        public const string ScenePath = SceneFlow.MenuScene;
        public const string PrefabPath = PrototypeBuilder.PrefabDir + "/UI/EcranTitre.prefab";

        /// <summary>La salle du menu (reprise du menu Godot) : une croix de couloirs autour d'une salle, des torches.</summary>
        public const string Plan =
            "#############\n" +
            "######.######\n" +
            "######.######\n" +
            "######T######\n" +
            "####T...T####\n" +
            "####.....####\n" +
            "#.T...P...T.#\n" +
            "####.....####\n" +
            "####T...T####\n" +
            "######T######\n" +
            "######.######\n" +
            "######.######\n" +
            "#############";

        [MenuItem("Deckscent/Construire la scène du menu")]
        public static void Build()
        {
            BuildPrefab();
            BuildScene();
            UpdateBuildScenes();
        }

        /// <summary>Scènes du build : Menu (0), Main (1), puis les autres déjà listées.</summary>
        public static void UpdateBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in new[] { SceneFlow.MenuScene, SceneFlow.GameScene })
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (var other in EditorBuildSettings.scenes)
                if (other.path != SceneFlow.MenuScene && other.path != SceneFlow.GameScene)
                    scenes.Add(other);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void BuildPrefab()
        {
            PrototypeBuilder.EnsureFolder(PrototypeBuilder.PrefabDir + "/UI");
            var root = new GameObject("EcranTitre", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1152f, 648f);
            scaler.matchWidthOrHeight = 0.5f;

            var screen = root.AddComponent<MenuScreen>();
            var so = new SerializedObject(screen);
            SetLayer(so.FindProperty("back"), Layer(root.transform, "Fond", true));
            var cards = so.FindProperty("cards");
            cards.arraySize = 4;
            var cardsGo = new GameObject("Cartes", typeof(RectTransform));
            cardsGo.transform.SetParent(root.transform, false);
            PrototypeBuilder.Stretch((RectTransform)cardsGo.transform);
            for (var i = 0; i < 4; i++)
            {
                // Une carte : un rectangle à la taille des cartes du donjon, agrandi par son échelle.
                var card = new GameObject("Carte " + (i + 1), typeof(RectTransform));
                card.transform.SetParent(cardsGo.transform, false);
                var rt = (RectTransform)card.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = Engraving.CardSize;
                rt.localScale = Vector3.one * 1.3f;
                SetLayer(cards.GetArrayElementAtIndex(i), Layer(card.transform, null, true));
            }
            SetLayer(so.FindProperty("front"), Layer(root.transform, "Dessus", true));
            so.FindProperty("navigate").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Navigate");
            so.FindProperty("submit").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Submit");
            so.FindProperty("cancel").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Cancel");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Un calque : toile d'encre et textes par-dessus, étirés sur le parent (ou dans un enfant `name`).</summary>
        static (InkCanvas ink, TextPool texts) Layer(Transform parent, string name, bool stretch)
        {
            if (name != null)
            {
                var group = new GameObject(name, typeof(RectTransform));
                group.transform.SetParent(parent, false);
                PrototypeBuilder.Stretch((RectTransform)group.transform);
                parent = group.transform;
            }
            var inkGo = new GameObject("Encre", typeof(RectTransform));
            inkGo.transform.SetParent(parent, false);
            PrototypeBuilder.Stretch((RectTransform)inkGo.transform);
            var ink = inkGo.AddComponent<InkCanvas>();
            ink.raycastTarget = false;
            var textsGo = new GameObject("Textes", typeof(RectTransform));
            textsGo.transform.SetParent(parent, false);
            PrototypeBuilder.Stretch((RectTransform)textsGo.transform);
            return (ink, textsGo.AddComponent<TextPool>());
        }

        static void SetLayer(SerializedProperty layer, (InkCanvas ink, TextPool texts) value)
        {
            layer.FindPropertyRelative("ink").objectReferenceValue = value.ink;
            layer.FindPropertyRelative("texts").objectReferenceValue = value.texts;
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrototypeBuilder.SetupLighting();
            var maze = PrototypeBuilder.CreateMaze(Plan);

            var menuGo = new GameObject("Menu");
            var menu = menuGo.AddComponent<MainMenu>();

            // Caméra et lanterne, comme le joueur (Player.prefab).
            var camGo = new GameObject("Caméra");
            camGo.tag = "MainCamera";
            camGo.transform.position = maze.CellToWorld(maze.StartCell) + Vector3.up * 1.6f;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();
            var lantern = new GameObject("Lanterne");
            lantern.transform.SetParent(camGo.transform, false);
            lantern.transform.localPosition = new Vector3(0f, 0.2f, 0.4f);
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 0.9f; // version sombre, comme la lanterne du joueur
            light.range = 6f;
            light.shadows = LightShadows.None;

            var musicGo = new GameObject("Musique");
            var source = musicGo.AddComponent<AudioSource>();
            source.playOnAwake = true;
            source.loop = true;
            source.spatialBlend = 0f;
            var mixer = Resources.Load<AudioMixer>("Mixage");
            source.outputAudioMixerGroup = mixer.FindMatchingGroups("Musique")[0];
            var music = musicGo.AddComponent<MenuMusic>();

            var mso = new SerializedObject(menu);
            mso.FindProperty("maze").objectReferenceValue = maze;
            mso.FindProperty("cameraRig").objectReferenceValue = camGo.transform;
            mso.FindProperty("music").objectReferenceValue = music;
            mso.ApplyModifiedPropertiesWithoutUndo();

            var screen = (GameObject)PrefabUtility.InstantiatePrefab(PrototypeBuilder.Load(PrefabPath), scene);
            var sso = new SerializedObject(screen.GetComponent<MenuScreen>());
            sso.FindProperty("menu").objectReferenceValue = menu;
            sso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Deckscent : scène " + ScenePath + " construite.");
        }
    }
}
