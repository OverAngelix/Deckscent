using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deckscent.Editor
{
    /// <summary>
    /// Menu Deckscent > Planche du loup-garou : chaque pose de <see cref="WerewolfRig"/> rendue
    /// côte à côte, avec le rendu gravure, dans docs/images/loup_garou.png. Sert à régler les poses
    /// (animation procédurale) et à les documenter. Rendu dans l'éditeur, sans Play mode, loin sous
    /// la scène ouverte (objets temporaires, jamais enregistrés).
    /// </summary>
    public static class WerewolfPlate
    {
        public const string OutputPath = "docs/images/loup_garou.png";
        const int Tile = 320, Columns = 5;

        [MenuItem("Deckscent/Planche du loup-garou")]
        public static void Render() => Render(true, OutputPath);

        /// <summary>Allume ou coupe la passe « Gravure » des renderers (le temps de la planche).</summary>
        static void SetGravure(bool on)
        {
            foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>(path);
                if (data == null)
                    continue;
                foreach (var feature in data.rendererFeatures)
                    if (feature != null && feature.name == "Gravure" && feature.isActive != on)
                        feature.SetActive(on);
            }
        }

        /// <summary>La planche, avec ou sans le post-process gravure (sans : pour lire les poses en les réglant).</summary>
        public static void Render(bool gravure, string outputPath, float yaw = 200f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossBuilder.WerewolfPath);
            if (prefab == null)
            {
                Debug.LogError("Planche du loup-garou : construire d'abord les prefabs du boss.");
                return;
            }
            var poses = (WerewolfRig.Pose[])System.Enum.GetValues(typeof(WerewolfRig.Pose));
            var rows = (poses.Length + Columns - 1) / Columns;
            var origin = new Vector3(0f, -200f, 0f);
            var temp = new GameObject("Planche loup-garou (temporaire)") { hideFlags = HideFlags.HideAndDontSave };
            var sheet = new Texture2D(Tile * Columns, Tile * rows, TextureFormat.RGB24, false);
            try
            {
                temp.transform.position = origin;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.transform.SetParent(temp.transform, false);
                floor.transform.localScale = Vector3.one * 2f;
                floor.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Gris_Sol.mat");
                var wolf = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                wolf.hideFlags = HideFlags.HideAndDontSave;
                wolf.transform.SetParent(temp.transform, false);
                // De trois quarts face, comme le joueur qui arrive un peu de côté.
                wolf.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                var lightGo = new GameObject("Lumière");
                lightGo.transform.SetParent(temp.transform, false);
                lightGo.transform.localPosition = new Vector3(-1.2f, 2.6f, -2.2f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 4f;
                light.range = 10f;
                light.shadows = LightShadows.Hard;
                var camGo = new GameObject("Caméra");
                camGo.transform.SetParent(temp.transform, false);
                camGo.transform.localPosition = new Vector3(0f, 1.5f, -3.4f);
                camGo.transform.LookAt(origin + Vector3.up * 1.2f);
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 50f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 30f;

                var rig = wolf.GetComponentInChildren<WerewolfRig>();
                SetGravure(gravure);
                var rt = RenderTexture.GetTemporary(Tile, Tile, 24, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                cam.targetTexture = rt;
                // Dans l'éditeur, un maillage riggé n'est redéformé qu'une fois par image : chaque pose
                // est « cuite » dans un maillage ordinaire, rendu à la place.
                var skins = wolf.GetComponentsInChildren<SkinnedMeshRenderer>();
                var baked = new (MeshFilter filter, Mesh mesh)[skins.Length];
                for (var k = 0; k < skins.Length; k++)
                {
                    var copy = new GameObject("Cuit");
                    copy.transform.SetParent(skins[k].transform, false);
                    var filter = copy.AddComponent<MeshFilter>();
                    copy.AddComponent<MeshRenderer>().sharedMaterials = skins[k].sharedMaterials;
                    baked[k] = (filter, new Mesh());
                    skins[k].enabled = false;
                }
                for (var i = 0; i < poses.Length; i++)
                {
                    rig.ApplyNow(poses[i]);
                    for (var k = 0; k < skins.Length; k++)
                    {
                        skins[k].BakeMesh(baked[k].mesh, true);
                        baked[k].filter.sharedMesh = baked[k].mesh;
                    }
                    cam.Render();
                    RenderTexture.active = rt;
                    sheet.ReadPixels(new Rect(0, 0, Tile, Tile), i % Columns * Tile, (rows - 1 - i / Columns) * Tile);
                }
                foreach (var (_, mesh) in baked)
                    Object.DestroyImmediate(mesh);
                cam.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var changed = DocFiles.SaveImageIfChanged(sheet, outputPath);
                Debug.Log("Planche du loup-garou : " + (changed ? "mise à jour, " + outputPath : "rien n'a changé.") + " Poses : " + string.Join(", ", poses));
            }
            finally
            {
                SetGravure(true);
                Object.DestroyImmediate(temp);
                Object.DestroyImmediate(sheet);
            }
        }
    }
}
