using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace Deckscent.Editor
{
    /// <summary>
    /// Copie les effets de particules choisis du pack Synty POLYGON Particle FX dans
    /// <c>Assets/Prefabs/FX/</c> (prefabs dépaquetés, pas de prefab imbriqué) et remplit la
    /// bibliothèque <c>Assets/Resources/Effets.asset</c> (<see cref="FxLibrary"/>).
    ///
    /// Le pack est fait pour l'ancien pipeline : ses matériaux « Legacy Shaders/Particles/… »
    /// s'afficheraient en rose sous URP. Chacun est converti en un matériau
    /// <c>Materials/FX/M_FX_&lt;nom&gt;</c> en « Universal Render Pipeline/Particles/Unlit »
    /// (même texture, même teinte, même mélange : additif, alpha, multiplicatif). Les matériaux
    /// déjà compatibles (shader Synty) sont gardés. Relancer écrase les prefabs.
    /// </summary>
    public static class FxBuilder
    {
        const string Source = "Assets/Synty/PolygonParticleFX/Prefabs/";
        const string Dir = "Assets/Prefabs/FX";
        const string MaterialDir = "Assets/Materials/FX";
        const string LibraryPath = "Assets/Resources/Effets.asset";

        /// <summary>Champ de la bibliothèque → (effet Synty, nom de notre prefab, échelle, en boucle).</summary>
        static readonly (string field, string synty, string name, float scale, bool loop)[] Effects =
        {
            // Retirés après essai (retour de l'utilisateur, 2026-11-20) : feu des torches et braseros,
            // coup porté, giclée des slimes, soin, pièces des coffres (idée gardée pour quand il y aura
            // de l'or). Voir docs/effets.md.
            ("puff", "FX_Dust_Small_01", "Bouffee", 1f, false),
            ("stunned", "FX_StarStunned_01", "Etourdi", 1f, true),
            ("bigDust", "FX_Dust_Big_01", "Poussiere", 1f, false),
            ("vaseBreak", "FX_Dust_Small_01", "VaseBrise", 0.8f, false),
            ("crateBreak", "FX_Impact_Wood_01", "CaisseBrisee", 1f, false),
        };

        [MenuItem("Deckscent/Construire les effets de particules (Particle FX)")]
        public static void BuildAll()
        {
            PrototypeBuilder.EnsureFolder(Dir);
            PrototypeBuilder.EnsureFolder(MaterialDir);
            PrototypeBuilder.EnsureFolder("Assets/Resources");
            var library = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<FxLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            var converted = new Dictionary<Material, Material>();
            var so = new SerializedObject(library);
            foreach (var (field, synty, name, scale, loop) in Effects)
            {
                var prefab = BuildEffect(synty, name, scale, loop, converted);
                so.FindProperty(field).objectReferenceValue = prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Deckscent : {Effects.Length} effets de particules construits, {converted.Count} matériaux convertis aux particules URP.");
        }

        static GameObject BuildEffect(string synty, string name, float scale, bool loop, Dictionary<Material, Material> converted)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + synty + ".prefab");
            if (source == null)
            {
                Debug.LogWarning("Deckscent : effet " + synty + " introuvable (pack POLYGON Particle FX non importé ?).");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.localScale *= scale;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy; // l'échelle du prefab compte
                main.playOnAwake = true;
                if (!loop)
                    main.loop = false;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                    continue;
                var mats = renderer.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    mats[i] = Convert(mats[i], converted);
                renderer.sharedMaterials = mats;
                if (renderer.trailMaterial != null)
                    renderer.trailMaterial = Convert(renderer.trailMaterial, converted);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            // Pas de son ni de lumière venus du pack : nos lumières sont blanches et réglées à part.
            foreach (var audio in go.GetComponentsInChildren<AudioSource>(true))
                Object.DestroyImmediate(audio);
            foreach (var light in go.GetComponentsInChildren<Light>(true))
                Object.DestroyImmediate(light);
            var path = Dir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        const string WhitePath = "Assets/Textures/FX_Blanc.png";

        static Texture WhiteTexture()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(WhitePath);
            if (texture != null)
                return texture;
            PrototypeBuilder.EnsureFolder("Assets/Textures");
            var white = new Texture2D(4, 4);
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            white.SetPixels32(pixels);
            System.IO.File.WriteAllBytes(WhitePath, white.EncodeToPNG());
            Object.DestroyImmediate(white);
            AssetDatabase.ImportAsset(WhitePath);
            return AssetDatabase.LoadAssetAtPath<Texture>(WhitePath);
        }

        /// <summary>Un matériau « Legacy Shaders/Particles » converti en particules URP ; les autres sont gardés.</summary>
        static Material Convert(Material source, Dictionary<Material, Material> converted)
        {
            if (source == null || !source.shader.name.StartsWith("Legacy Shaders/Particles"))
                return source;
            if (converted.TryGetValue(source, out var done))
                return done;
            var path = MaterialDir + "/M_FX_" + source.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
                material.shader = shader;
            var name = source.shader.name;
            var texture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
            // Sans texture, l'ancien shader dessinait du blanc uni (feu en maillages colorés par les
            // particules) : une texture blanche à nous, versionnée. Ainsi, une texture manquante
            // veut dire « pack absent » (Fx.Usable).
            if (texture == null)
                texture = WhiteTexture();
            material.SetTexture("_BaseMap", texture);
            // Les anciens shaders additifs prennent une teinte « _TintColor » à 0,5 pour neutre.
            var tint = source.HasProperty("_TintColor") ? source.GetColor("_TintColor") * 2f
                : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            tint.a = Mathf.Clamp01(tint.a);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Surface", 1f); // transparent
            // Mélange URP : 0 alpha, 1 prémultiplié, 2 additif, 3 multiplicatif. Pas d'additif : il
            // éclaircit le décor sans y mettre de couleur, et le post-process gravure n'en garde que du
            // papier. En alpha, la couleur saturée (feu, or, giclée) remplace le fond : un accent.
            var blend = name.Contains("Multiply") ? 3f : 0f;
            material.SetFloat("_Blend", blend);
            material.SetFloat("_ColorMode", 0f); // couleur des particules × texture
            // Pas de particules douces : elles s'effacent près des murs, et nos feux sont contre un mur.
            material.SetFloat("_SoftParticlesEnabled", 0f);
            material.SetFloat("_CameraFadingEnabled", 0f);
            BaseShaderGUI.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(material);
            converted[source] = material;
            return material;
        }
    }
}
