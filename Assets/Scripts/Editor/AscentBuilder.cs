using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Construit le prefab de l'écran de remontée (Prefabs/UI/Remontee : remonter ou descendre, en
    /// fin de sous-biome) et l'ajoute aux scènes de jeu (Main, Test_Donjon). Relancer écrase le
    /// prefab : une fois réglé à la main, ne le relancer que pour repartir de zéro.
    /// </summary>
    public static class AscentBuilder
    {
        public const string PrefabPath = PrototypeBuilder.PrefabDir + "/UI/Remontee.prefab";
        const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Deckscent/Construire l'écran de remontée (prefab, scènes de jeu)")]
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
            Debug.Log("Deckscent : écran de remontée construit et ajouté aux scènes de jeu.");
        }

        /// <summary>Canvas en surimpression au-dessus de l'écran de niveau (150), sous la pause (200).</summary>
        public static void BuildPrefab()
        {
            var root = LevelUpBuilder.OverlayRoot("Remontee", 160, out var ink, out var texts);
            var screen = root.AddComponent<AscentScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("ink").objectReferenceValue = ink;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("navigate").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Navigate");
            so.FindProperty("submit").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Submit");
            so.FindProperty("cancel").objectReferenceValue = PrototypeBuilder.ActionReference("UI", "Cancel");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Ajoute l'écran de remontée à la scène, s'il n'y est pas.</summary>
        public static void AddToScene(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponentInChildren<AscentScreen>(true) != null)
                    return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                BuildPrefab();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
