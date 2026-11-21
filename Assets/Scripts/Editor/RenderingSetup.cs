using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deckscent.Editor
{
    /// <summary>
    /// Met en place le rendu gravure : post-process sur les renderers URP, matériaux toon sur les
    /// prefabs, ombres à bords durs. Menu Deckscent > Installer le rendu gravure. Se relance sans
    /// risque : ce qui est déjà en place est gardé (les réglages des matériaux ne sont pas écrasés).
    /// </summary>
    public static class RenderingSetup
    {
        const string MaterialDir = "Assets/Materials";
        const string ToonDir = MaterialDir + "/Toon";
        const string GravureMaterialPath = MaterialDir + "/M_Gravure.mat";
        const string FeatureName = "Gravure";
        static readonly string[] Renderers = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };
        static readonly string[] PipelineAssets = { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" };

        /// <summary>Gain des textures Synty (atlas gris ~0,38) : sans lui, tout se couvre de hachures.</summary>
        const float SyntyTextureGain = 2.5f;

        [MenuItem("Deckscent/Installer le rendu gravure")]
        public static void Install()
        {
            var gravure = GravureMaterial();
            foreach (var path in Renderers)
                AddFeature(path, gravure);
            foreach (var path in PipelineAssets)
                HardShadows(path);
            ConvertPrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("Rendu gravure installé : post-process, matériaux toon, ombres dures.");
        }

        static Material GravureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GravureMaterialPath);
            if (material != null)
                return material;
            material = new Material(Shader.Find("Deckscent/Gravure"));
            AssetDatabase.CreateAsset(material, GravureMaterialPath);
            return material;
        }

        /// <summary>Ajoute le Full Screen Pass « Gravure » au renderer s'il n'y est pas.</summary>
        static void AddFeature(string rendererPath, Material material)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            if (data == null)
            {
                Debug.LogWarning("Rendu gravure : renderer introuvable " + rendererPath);
                return;
            }
            var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == FeatureName);
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = FeatureName;
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
                // La table des identifiants des features doit suivre la liste.
                var so = new SerializedObject(data);
                var map = so.FindProperty("m_RendererFeatureMap");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
        }

        /// <summary>Ombres à bords durs : le post-process cerne leur bord d'un trait net.</summary>
        static void HardShadows(string pipelinePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (asset == null)
                return;
            var so = new SerializedObject(asset);
            so.FindProperty("m_SoftShadowsSupported").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Remplace les matériaux des prefabs par leur équivalent toon (sauf l'unlit de la flamme).</summary>
        internal static void ConvertPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(ToonDir))
                AssetDatabase.CreateFolder(MaterialDir, "Toon");
            var toon = Shader.Find("Deckscent/Toon");
            var cache = new Dictionary<Material, Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                var changed = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    // Les particules gardent leurs matériaux (FxBuilder : mélange alpha, voir docs/effets.md).
                    if (renderer is ParticleSystemRenderer)
                        continue;
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        if (source == null || source.shader == toon || source.shader.name.Contains("Unlit"))
                            continue;
                        if (!cache.TryGetValue(source, out var converted))
                            cache[source] = converted = ToToon(source, toon);
                        materials[i] = converted;
                        changed = true;
                    }
                    renderer.sharedMaterials = materials;
                }
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Nos matériaux gris passent au shader toon en place ; ceux de Synty sont copiés dans
        /// Materials/Toon (texture de l'atlas gardée, en gris et éclaircie).
        /// </summary>
        static Material ToToon(Material source, Shader toon)
        {
            var sourcePath = AssetDatabase.GetAssetPath(source);
            if (sourcePath.StartsWith(MaterialDir))
            {
                var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
                source.shader = toon;
                source.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(source);
                return source;
            }
            var path = ToonDir + "/M_Toon_" + source.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            material = new Material(toon);
            // Atlas des décors (Generic_Basic : _Albedo_Map), des arbres (SyntyStudios_Trees :
            // _Main_Texture) ou d'un matériau URP (_BaseMap).
            Texture texture = null;
            foreach (var property in new[] { "_Albedo_Map", "_Main_Texture", "_BaseMap" })
                if (texture == null && source.HasProperty(property))
                    texture = source.GetTexture(property);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_TextureGain", texture != null ? SyntyTextureGain : 1f);
            material.SetFloat("_TextureSaturation", 0f);
            // Feuillages Synty : des cartes dont la texture découpe les feuilles (alpha), vues des
            // deux côtés. Les atlas sans alpha restent pleins.
            if (texture is Texture2D t2d && UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(t2d.graphicsFormat)
                && (source.shader.name.Contains("Trees") || source.shader.name.Contains("LOD") || source.shader.name.Contains("Foliage")))
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.5f);
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
