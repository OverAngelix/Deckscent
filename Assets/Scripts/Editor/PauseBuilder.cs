using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit le prefab du menu pause (Prefabs/UI/MenuPause) et l'ajoute aux scènes de jeu
    /// (Main, Test_Donjon). Relancer écrase le prefab : une fois réglé à la main, ne le relancer
    /// que pour repartir de zéro.
    /// </summary>
    public static class PauseBuilder
    {
        public const string PrefabPath = PrototypeBuilder.PrefabDir + "/UI/MenuPause.prefab";
        const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Deckscent/Construire le menu pause (prefab, scènes de jeu)")]
        public static void BuildAndAdd()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Deckscent : la scène ouverte a des changements non enregistrés. Enregistrer ou annuler, puis relancer.");
                return;
            }
            BuildPrefab();
            foreach (var path in new[] { MainScenePath, PrototypeBuilder.DemoScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                AddToScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("Deckscent : menu pause construit et ajouté aux scènes de jeu.");
        }

        /// <summary>
        /// Canvas en surimpression au-dessus de tout (carte 100, HUD 50), toile d'encre et textes,
        /// actions branchées. Le joueur et la carte sont cherchés dans la scène au lancement.
        /// </summary>
        public static void BuildPrefab()
        {
            PrototypeBuilder.EnsureFolder(PrototypeBuilder.PrefabDir + "/UI");
            var root = new GameObject("MenuPause", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
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

            var pause = root.AddComponent<PauseScreen>();
            var so = new SerializedObject(pause);
            so.FindProperty("ink").objectReferenceValue = ink;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("toggle").objectReferenceValue = PrototypeBuilder.ActionReference("Player", "Pause");
            so.FindProperty("navigate").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Navigate");
            so.FindProperty("submit").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Submit");
            so.FindProperty("cancel").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Cancel");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Ajoute le menu pause à la scène, s'il n'y est pas.</summary>
        public static void AddToScene(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponentInChildren<PauseScreen>(true) != null)
                    return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                BuildPrefab();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
