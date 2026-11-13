using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deckscent.Editor
{
    /// <summary>
    /// Captures de la scène de test depuis des points de vue fixes, pour comparer le rendu avant /
    /// après un changement (shader, éclairage, prefabs). Menu Deckscent > Captures de la scène de test.
    ///
    /// Rendu dans l'éditeur, sans Play mode : les torches ne vacillent pas, deux captures du même
    /// état sont identiques. Images dans Captures/ à la racine du projet, versionnées : le diff
    /// d'un commit montre l'avant / après du rendu.
    /// </summary>
    public static class TestCaptures
    {
        public const string OutputDir = "Captures";
        const int Width = 1280;
        const int Height = 720;

        /// <summary>Points de vue, en cases du plan de test (PrototypeBuilder.TestPlan).</summary>
        static readonly (string name, Vector2Int cell, int facing)[] Poses =
        {
            ("1_depart_est", new Vector2Int(6, 3), 1),        // départ, carrefour en T devant
            ("2_depart_ouest", new Vector2Int(6, 3), 3),      // couloir vers l'ouest
            ("3_couloir_torche_nord", new Vector2Int(1, 4), 0), // long couloir, torche au fond (1, 1)
            ("4_torche_mur_droit", new Vector2Int(8, 5), 3),  // torche (5, 5) sur le mur nord, à droite
            ("5_grand_couloir_sud", new Vector2Int(13, 7), 3), // couloir sud, torche (6, 7) au loin
        };

        [MenuItem("Deckscent/Captures de la scène de test")]
        public static void Capture()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != PrototypeBuilder.TestScenePath)
            {
                if (active.isDirty)
                {
                    Debug.LogError("Captures : la scène ouverte a des modifications non enregistrées. Enregistre-la d'abord.");
                    return;
                }
                if (!File.Exists(PrototypeBuilder.TestScenePath))
                    PrototypeBuilder.BuildTestScene();
                EditorSceneManager.OpenScene(PrototypeBuilder.TestScenePath, OpenSceneMode.Single);
            }

            var maze = Object.FindAnyObjectByType<Maze>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (maze == null || player == null)
            {
                Debug.LogError("Captures : labyrinthe ou joueur introuvable dans la scène de test.");
                return;
            }
            maze.Build();
            var cam = player.GetComponentInChildren<Camera>();
            Directory.CreateDirectory(OutputDir);

            var saved = (player.transform.position, player.transform.rotation);
            foreach (var (name, cell, facing) in Poses)
            {
                player.transform.SetPositionAndRotation(maze.CellToWorld(cell), Quaternion.Euler(0f, Grid.Yaw(facing), 0f));
                Save(cam, name);
            }
            player.transform.SetPositionAndRotation(saved.Item1, saved.Item2);

            SaveTopView(maze);
            // La pose du joueur a été remise : on enregistre pour ne pas laisser la scène « modifiée ».
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Captures : " + (Poses.Length + 1) + " images dans " + Path.GetFullPath(OutputDir));
        }

        /// <summary>Vue de dessus orthogonale, éclairée par un soleil temporaire.</summary>
        static void SaveTopView(Maze maze)
        {
            var size = maze.GridSize;
            var camGo = new GameObject("VueDessus (temporaire)") { hideFlags = HideFlags.HideAndDontSave };
            var sunGo = new GameObject("Soleil (temporaire)") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(size.y, size.x * Height / (float)Width) * maze.CellSize * 0.55f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.farClipPlane = 100f;
                // Le plafond n'est visible que par-dessous : vu d'en haut, on voit l'intérieur.
                var center = maze.transform.TransformPoint(new Vector3(size.x, 0f, -size.y) * maze.CellSize * 0.5f);
                camGo.transform.SetPositionAndRotation(center + Vector3.up * 40f, Quaternion.Euler(90f, 0f, 0f));
                var sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.2f;
                sunGo.transform.rotation = Quaternion.Euler(60f, 30f, 0f);
                Save(cam, "6_vue_dessus");
            }
            finally
            {
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(sunGo);
            }
        }

        static void Save(Camera cam, string name)
        {
            var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                if (DocFiles.SaveImageIfChanged(tex, Path.Combine(OutputDir, name + ".png")))
                    Debug.Log("Captures : " + name + ".png a changé.");
                Object.DestroyImmediate(tex);
            }
            finally
            {
                cam.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
