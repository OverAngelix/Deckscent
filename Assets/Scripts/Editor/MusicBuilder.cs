using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Deckscent.Editor
{
    /// <summary>
    /// Menu Deckscent > Construire la musique du donjon : le prefab `Audio/MusiqueDonjon`
    /// (AudioSource sur la piste Musique, DungeonMusic, MusicDirector), ajouté aux scènes Main et
    /// Test_Donjon s'il n'y est pas. Relancer écrase le prefab (et ses réglages faits à la main).
    /// </summary>
    public static class MusicBuilder
    {
        public const string MusicPath = "Assets/Prefabs/Audio/MusiqueDonjon.prefab";

        [MenuItem("Deckscent/Construire la musique du donjon")]
        public static void Build()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Deckscent : la scène ouverte a des changements non enregistrés. Enregistrer ou annuler, puis relancer.");
                return;
            }
            BuildPrefab();
            foreach (var path in new[] { "Assets/Scenes/Main.unity", PrototypeBuilder.DemoScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                AddToScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("Deckscent : musique du donjon construite et ajoutée aux scènes.");
        }

        static void BuildPrefab()
        {
            PrototypeBuilder.EnsureFolder("Assets/Prefabs/Audio");
            var root = new GameObject("MusiqueDonjon");
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            var mixer = Resources.Load<AudioMixer>("Mixage");
            if (mixer != null)
                source.outputAudioMixerGroup = mixer.FindMatchingGroups("Musique")[0];
            var music = root.AddComponent<DungeonMusic>();
            var director = root.AddComponent<MusicDirector>();
            var so = new SerializedObject(director);
            so.FindProperty("music").objectReferenceValue = music;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, MusicPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        static void AddToScene(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponentInChildren<DungeonMusic>(true) != null)
                    return;
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MusicPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
