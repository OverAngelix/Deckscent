using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Deckscent.Editor
{
    /// <summary>
    /// Écrit docs/prefabs/ : la liste des prefabs (README.md) et une page par prefab avec sa
    /// hiérarchie, ses composants, ses réglages et une image. Menu Deckscent > Documenter les
    /// prefabs. À relancer après chaque modification d'un prefab (la doc est générée : ne pas
    /// l'éditer à la main, sauf les descriptions ci-dessous).
    /// </summary>
    public static class PrefabDocs
    {
        const string PrefabRoot = "Assets/Prefabs";
        const string DocDir = "docs/prefabs";
        const string ImageDir = DocDir + "/images";
        const int ImageSize = 512;

        /// <summary>À quoi sert chaque prefab (texte libre, affiché en tête de sa page).</summary>
        static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            ["Player"] = "Le joueur : déplacement case par case au tour par tour, caméra à hauteur d'yeux et lanterne. Posé sur la case `P` du plan au démarrage.\n\nCe que voit sa caméra : les captures de la scène de test, par exemple [1_depart_est.png](../../Captures/1_depart_est.png).",
            ["Mur_01"] = "Pan de mur du décor (Synty `SM_Env_Wall_01`). Posé au milieu de chaque bord fermé d'une case de sol, face (+Z) vers la case.",
            ["Mur_03"] = "Variante de pan de mur (Synty `SM_Env_Wall_03`).",
            ["Mur_04"] = "Variante de pan de mur (Synty `SM_Env_Wall_04`).",
            ["Sol"] = "Dalle de sol d'une case (Synty `SM_Env_Tiles_Texture_01`), centrée sur la case.",
            ["Plafond"] = "Plafond d'une case (Synty `SM_Env_Ceiling_Stone_Flat_01`), tourné vers le bas : invisible vu d'en haut.",
            ["Mur_Gris"] = "Pan de mur de secours (bloc gris), utilisé si le pack Synty manque.",
            ["Sol_Gris"] = "Sol de secours (bloc gris).",
            ["Plafond_Gris"] = "Plafond de secours (quad gris tourné vers le bas).",
            ["CarteDuDonjon"] = "Écran de carte : le plateau et la main de cartes-tuiles, dessinés en gravure (InkCanvas + TextPool), pour poser les tuiles. Canvas en surimpression, au-dessus du jeu et hors du post-process. Met le jeu en pause quand il est ouvert. Voir docs/carte.md.",
            ["Slime"] = "Ennemi simple, en formes simples (aucun slime dans les packs) : un dôme de gelée qui rebondit, deux yeux tournés vers le joueur. Trois sortes (vert 3 PV, rouge 6 PV, bleu 2 d'attaque) réglées par `variant` ; couleur saturée, donc gardée par le post-process (accent). Au tour par tour : attaque le joueur à côté, sinon s'en approche s'il est à moins de `aggroRange` pas. Posé par le Spawner (contenu « monstre »). Voir docs/monstres/slime.md.",
            ["DallePics"] = "Piège : dalle à pics (grille et piques Synty `SM_Trap_Spears_Grate_01` / `SM_Trap_Spears_01`, ou plaque grise et cônes si le pack manque). Pics sortis 1 tour sur 3, pointes visibles le tour d'avant, 2 dégâts au joueur comme aux ennemis. Posé par le Spawner (contenu « piege »). Voir docs/pieges.md.",
            ["Coffre"] = "Coffre doré (Synty `SM_Prop_Chest_03`, ou blocs si le pack manque), couvercle sur charnière. S'ouvre en avançant dessus ou avec Interact (un tour) : une carte de plus dans la main ou +3 PV, puis il s'enfonce dans le sol. Posé par le Spawner (contenu « tresor »). Voir docs/coffres.md.",
            ["MusiqueDonjon"] = "Musique du donjon, à couches (exploration, menace, coffre, combat, boss), synthétisée en temps réel sur la piste Musique du mixer. `MusicDirector` regarde la situation du joueur, `DungeonMusic` joue. Voir docs/musique.md.",
            ["Hud"] = "Interface en jeu, dessinée en gravure (InkCanvas + TextPool) : PV, Tour / ATQ / DÉF, messages, voile rouge quand on est touché, écran de mort. Canvas en surimpression sous la carte. Voir docs/tour-par-tour.md.",
            ["EcranTitre"] = "Écran titre du menu, dessiné en gravure (InkCanvas + TextPool) : le nom dans un cartouche, l'accroche, et les choix en main de cartes agrandies (Descendre, Options, Commandes, Quitter), avec les panneaux Options (son, langue FR / EN) et Commandes. Calques : Fond, une toile par carte (échelle 1,3), Dessus (panneaux, fondu). Construit par *Deckscent > Construire la scène du menu*. Voir docs/menu.md.",
            ["Torche"] ="Torche murale, posée sur les cases `T`, accrochée au premier mur voisin. Sa racine est sur le mur, +Z vers la case.",
        };

        [MenuItem("Deckscent/Documenter les prefabs")]
        public static void Generate()
        {
            Directory.CreateDirectory(ImageDir);
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

            var index = new StringBuilder();
            index.AppendLine("# Prefabs");
            index.AppendLine();
            index.AppendLine("> Généré par *Deckscent > Documenter les prefabs* (`Assets/Scripts/Editor/PrefabDocs.cs`). Ne pas éditer à la main : régénérer après chaque modification d'un prefab. Les descriptions sont dans `PrefabDocs.Descriptions`.");
            index.AppendLine();
            index.AppendLine("| Prefab | Chemin | Rôle |");
            index.AppendLine("|---|---|---|");

            // Nombres avec un point décimal, quelle que soit la langue de Windows.
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var changed = new List<string>();
            try
            {
                foreach (var path in paths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var name = prefab.name;
                    Descriptions.TryGetValue(name, out var description);
                    // Dans le tableau, seulement le premier paragraphe (un saut de ligne le casserait).
                    var summary = (description ?? "").Split('\n')[0];
                    index.AppendLine($"| [{name}]({name}.md) | `{path}` | {summary} |");

                    var imagePath = Path.Combine(ImageDir, name + ".png");
                    var hasImage = RenderImage(prefab, imagePath, out var imageChanged);
                    if (!hasImage && File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                        imageChanged = true;
                    }
                    if (imageChanged)
                        changed.Add("images/" + name + ".png");
                    if (DocFiles.SaveTextIfChanged(Page(prefab, path, description, hasImage), Path.Combine(DocDir, name + ".md")))
                        changed.Add(name + ".md");
                }
                if (DocFiles.SaveTextIfChanged(index.ToString(), Path.Combine(DocDir, "README.md")))
                    changed.Add("README.md");
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
            Debug.Log($"Prefabs documentés : {paths.Count} prefabs, "
                + (changed.Count == 0 ? "rien n'a changé." : "mis à jour : " + string.Join(", ", changed)));
        }

        static string Page(GameObject prefab, string path, string description, bool hasImage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + prefab.name);
            sb.AppendLine();
            sb.AppendLine($"`{path}` · [← liste des prefabs](README.md)");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine(description);
                sb.AppendLine();
            }
            if (hasImage)
                sb.AppendLine($"![{prefab.name}](images/{prefab.name}.png)");
            else
                sb.AppendLine("*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*");
            sb.AppendLine();
            sb.AppendLine("## Hiérarchie");
            sb.AppendLine();
            sb.AppendLine("```");
            Tree(prefab.transform, "", true, true, sb);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("## Contenu");
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                var section = new StringBuilder();
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null)
                    {
                        section.AppendLine("- **Script manquant**");
                        continue;
                    }
                    var lines = Describe(c).ToList();
                    // Un Transform à l'origine n'apprend rien : on le saute.
                    if (c is Transform && lines.Count == 0)
                        continue;
                    section.AppendLine("- **" + c.GetType().Name + "**");
                    foreach (var line in lines)
                        section.AppendLine("  - " + line);
                }
                if (section.Length == 0)
                    continue;
                sb.AppendLine();
                sb.AppendLine("### " + HierarchyPath(prefab.transform, t));
                sb.AppendLine();
                sb.Append(section);
            }
            return sb.ToString();
        }

        static void Tree(Transform t, string prefix, bool root, bool last, StringBuilder sb)
        {
            var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name);
            var suffix = comps.Any() ? "  [" + string.Join(", ", comps) + "]" : "";
            sb.AppendLine((root ? "" : prefix + (last ? "└─ " : "├─ ")) + t.name + suffix);
            var childPrefix = root ? "" : prefix + (last ? "   " : "│  ");
            for (var i = 0; i < t.childCount; i++)
                Tree(t.GetChild(i), childPrefix, false, i == t.childCount - 1, sb);
        }

        static string HierarchyPath(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (var cur = t; cur != null; cur = cur.parent)
            {
                parts.Insert(0, cur.name);
                if (cur == root)
                    break;
            }
            return string.Join(" / ", parts);
        }

        static IEnumerable<string> Describe(Component c)
        {
            switch (c)
            {
                case Transform t:
                    if (t.localPosition != Vector3.zero) yield return "position " + V(t.localPosition);
                    if (t.localRotation != Quaternion.identity) yield return "rotation " + V(t.localEulerAngles) + "°";
                    if (t.localScale != Vector3.one) yield return "échelle " + V(t.localScale);
                    yield break;
                case MeshFilter mf:
                    yield return "maillage `" + (mf.sharedMesh ? mf.sharedMesh.name : "manquant") + "`" + Source(mf.sharedMesh);
                    yield break;
                case MeshRenderer mr:
                    yield return "matériaux " + string.Join(", ", mr.sharedMaterials.Select(m => m ? $"`{m.name}` ({m.shader.name})" : "manquant"));
                    if (mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.On) yield return "ombres portées : " + mr.shadowCastingMode;
                    yield break;
                case Light l:
                    yield return $"{l.type}, couleur {C(l.color)}, intensité {l.intensity:0.##}, portée {l.range:0.##} m, ombres {l.shadows}";
                    yield break;
                case Camera cam:
                    yield return $"FOV {cam.fieldOfView:0.#}°, plans {cam.nearClipPlane:0.##}–{cam.farClipPlane:0.#} m, fond {cam.clearFlags} {C(cam.backgroundColor)}";
                    yield break;
                // Nos scripts seulement : les réglages internes d'Unity (URP…) noieraient la page.
                case MonoBehaviour mb when mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("Deckscent"):
                    foreach (var line in SerializedFields(mb))
                        yield return line;
                    yield break;
            }
        }

        /// <summary>Champs exposés d'un script, avec leur infobulle.</summary>
        static IEnumerable<string> SerializedFields(Object target)
        {
            var so = new SerializedObject(target);
            var it = so.GetIterator();
            if (!it.NextVisible(true))
                yield break;
            do
            {
                if (it.name == "m_Script")
                    continue;
                var value = Value(it);
                if (value == null)
                    continue;
                var tooltip = Tooltip(target.GetType(), it.name);
                yield return $"`{it.name}` = {value}" + (tooltip != null ? " — " + tooltip : "");
            } while (it.NextVisible(false));
        }

        static string Value(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Boolean: return p.boolValue ? "oui" : "non";
                case SerializedPropertyType.Integer: return p.intValue.ToString();
                case SerializedPropertyType.Float: return p.floatValue.ToString("0.###");
                case SerializedPropertyType.String: return p.stringValue.Contains("\n") ? "(texte sur plusieurs lignes)" : "`" + p.stringValue + "`";
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue ? "`" + p.objectReferenceValue.name + "`" : "(vide)";
                case SerializedPropertyType.Enum: return p.enumDisplayNames.Length > p.enumValueIndex && p.enumValueIndex >= 0 ? p.enumDisplayNames[p.enumValueIndex] : p.intValue.ToString();
                case SerializedPropertyType.Vector3: return V(p.vector3Value);
                case SerializedPropertyType.Color: return C(p.colorValue);
            }
            // InputAction : on montre ses touches.
            var bindings = p.FindPropertyRelative("m_SingletonActionBindings");
            if (bindings != null && bindings.isArray)
            {
                var keys = new List<string>();
                for (var i = 0; i < bindings.arraySize; i++)
                    keys.Add("`" + bindings.GetArrayElementAtIndex(i).FindPropertyRelative("m_Path").stringValue + "`");
                return "touches " + (keys.Count > 0 ? string.Join(", ", keys) : "(aucune)");
            }
            if (p.isArray)
                return "liste de " + p.arraySize;
            return null;
        }

        static string Tooltip(System.Type type, string field)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var info = t.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (info == null)
                    continue;
                var attr = (TooltipAttribute)System.Attribute.GetCustomAttribute(info, typeof(TooltipAttribute));
                return attr?.tooltip;
            }
            return null;
        }

        static string Source(Object asset)
        {
            if (asset == null)
                return "";
            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? "" : $" (`{path}`)";
        }

        static string V(Vector3 v) => $"({v.x:0.###}, {v.y:0.###}, {v.z:0.###})";
        static string C(Color c) => $"({c.r:0.##}, {c.g:0.##}, {c.b:0.##})";

        /// <summary>
        /// Image du prefab, rendue dans une scène d'aperçu isolée (comme le mode Prefab) : la
        /// scène ouverte n'est pas touchée.
        /// </summary>
        static bool RenderImage(GameObject prefab, string file, out bool changed)
        {
            changed = false;
            // Rien à montrer (le joueur : caméra, lumière, scripts) : pas d'image.
            if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
                return false;
            var origin = Vector3.zero;
            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var temp = new List<GameObject>();
            try
            {
                var instance = (GameObject)Object.Instantiate(prefab, origin, Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, preview);
                temp.Add(instance);
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    behaviour.enabled = false;
                // Le joueur a sa propre caméra : on la coupe pour la photo.
                foreach (var ownCam in instance.GetComponentsInChildren<Camera>(true))
                    ownCam.enabled = false;
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(origin + Vector3.up, Vector3.one * 2f);
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);
                if (renderers.Length == 0)
                    bounds = new Bounds(origin + Vector3.up * 0.9f, new Vector3(1f, 1.8f, 1f));

                var camGo = new GameObject("CameraDoc");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, preview);
                temp.Add(camGo);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = preview;
                cam.cameraType = CameraType.Preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.16f, 0.17f);
                cam.fieldOfView = 30f;
                var radius = Mathf.Max(bounds.extents.magnitude, 0.3f);
                var distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.1f;
                // Vue de trois quarts, depuis l'avant (+Z, la face des pièces) et un peu au-dessus.
                // Les plafonds ne se voient que par-dessous : on les prend d'en bas.
                var fromBelow = prefab.name.StartsWith("Plafond");
                var dir = new Vector3(0.6f, fromBelow ? -0.6f : 0.45f, 1f).normalized;
                camGo.transform.position = bounds.center + dir * distance;
                camGo.transform.LookAt(bounds.center);
                cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
                cam.farClipPlane = distance + radius * 2f;

                var sunGo = new GameObject("SoleilDoc");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sunGo, preview);
                temp.Add(sunGo);
                var sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.3f;
                // Lumière venant du côté de la caméra, un peu décalée pour modeler les reliefs.
                sunGo.transform.rotation = Quaternion.LookRotation(-dir + Vector3.left * 0.4f + (fromBelow ? Vector3.up : Vector3.down) * 0.3f);

                var rt = RenderTexture.GetTemporary(ImageSize, ImageSize, 24, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(ImageSize, ImageSize, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0);
                    tex.Apply();
                    changed = DocFiles.SaveImageIfChanged(tex, file);
                    Object.DestroyImmediate(tex);
                }
                finally
                {
                    cam.targetTexture = null;
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Prefabs documentés : image de " + prefab.name + " impossible (" + e.Message + ")");
                return false;
            }
            finally
            {
                foreach (var go in temp)
                    if (go != null)
                        Object.DestroyImmediate(go);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
