using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit le prefab de l'écran de montée de niveau (Prefabs/UI/MonteeDeNiveau) et l'ajoute aux
    /// scènes de jeu (Main, Test_Donjon). Relancer écrase le prefab : une fois réglé à la main, ne le
    /// relancer que pour repartir de zéro.
    /// </summary>
    public static class LevelUpBuilder
    {
        public const string PrefabPath = PrototypeBuilder.PrefabDir + "/UI/MonteeDeNiveau.prefab";
        const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Deckscent/Construire l'écran de niveau (prefab, scènes de jeu)")]
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
            Debug.Log("Deckscent : écran de niveau construit et ajouté aux scènes de jeu.");
        }

        /// <summary>
        /// Canvas en surimpression au-dessus du HUD (50) et de la carte (100), sous la pause (200) :
        /// toile d'encre et textes, actions UI branchées. Le joueur est cherché dans la scène.
        /// </summary>
        public static void BuildPrefab()
        {
            var root = OverlayRoot("MonteeDeNiveau", 150, out var ink, out var texts);
            var screen = root.AddComponent<LevelUpScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("ink").objectReferenceValue = ink;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("navigate").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Navigate");
            so.FindProperty("submit").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Submit");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Racine d'un écran en surimpression (référence 1152 × 648, comme les autres) : canvas à
        /// l'ordre `order`, toile d'encre et textes étirés sur tout l'écran.
        /// </summary>
        internal static GameObject OverlayRoot(string name, int order, out InkCanvas ink, out TextPool texts)
        {
            PrototypeBuilder.EnsureFolder(PrototypeBuilder.PrefabDir + "/UI");
            var root = new GameObject(name, typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1152f, 648f);
            scaler.matchWidthOrHeight = 0.5f;

            var inkGo = new GameObject("Encre", typeof(RectTransform));
            inkGo.transform.SetParent(root.transform, false);
            PrototypeBuilder.Stretch((RectTransform)inkGo.transform);
            ink = inkGo.AddComponent<InkCanvas>();
            ink.raycastTarget = false;
            var textsGo = new GameObject("Textes", typeof(RectTransform));
            textsGo.transform.SetParent(root.transform, false);
            PrototypeBuilder.Stretch((RectTransform)textsGo.transform);
            texts = textsGo.AddComponent<TextPool>();
            return root;
        }

        /// <summary>Ajoute l'écran de niveau à la scène, s'il n'y est pas.</summary>
        public static void AddToScene(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponentInChildren<LevelUpScreen>(true) != null)
                    return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                BuildPrefab();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
