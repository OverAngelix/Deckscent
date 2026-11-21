using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Ciel de nuit gravé du camp (docs/camp.md) : allume le ciel du post-process gravure
    /// (<c>_SkyOn</c>), qui dessine ce qui n'a pas de décor (aplat d'encre en dégradé, étoiles,
    /// croissant de lune) au lieu du fond noir du donjon. La lune est dans la direction d'où vient
    /// la lumière de <see cref="moonLight"/>. Éteint en quittant la scène : le donjon garde son noir.
    /// </summary>
    [ExecuteAlways]
    public class CampSky : MonoBehaviour
    {
        [Tooltip("Le clair de lune (lumière directionnelle) : la lune est dessinée d'où il vient.")]
        [SerializeField] Light moonLight;
        [Tooltip("Ton du ciel à l'horizon (1 = papier, 0 = encre). Un gris sombre : les silhouettes du décor s'y découpent en noir.")]
        [SerializeField, Range(0f, 1f)] float horizonTone = 0.3f;
        [Tooltip("Ton du ciel au zénith (1 = papier, 0 = encre).")]
        [SerializeField, Range(0f, 1f)] float zenithTone = 0.03f;
        [Tooltip("Finesse de la grille des étoiles : plus grand = plus d'étoiles, plus petites et plus serrées.")]
        [SerializeField, Range(20f, 300f)] float starDensity = 90f;
        [Tooltip("Rayon apparent de la lune (degrés).")]
        [SerializeField, Range(0.5f, 10f)] float moonRadius = 3.5f;
        [Tooltip("Dehors, l'obscurité du donjon (le noir au-delà de 8 à 16 m) recule : distances multipliées par ce facteur.")]
        [SerializeField, Range(1f, 10f)] float darknessScale = 3f;

        static readonly int SkyOn = Shader.PropertyToID("_SkyOn");
        static readonly int MoonDir = Shader.PropertyToID("_MoonDir");
        static readonly int SkyParams = Shader.PropertyToID("_SkyParams");
        static readonly int DarkScale = Shader.PropertyToID("_DarkScale");

        void OnEnable() => Apply();

        void OnValidate() => Apply();

        void Update() => Apply();

        void OnDisable()
        {
            Shader.SetGlobalFloat(SkyOn, 0f);
            Shader.SetGlobalFloat(DarkScale, 0f);
        }

        void Apply()
        {
            var toMoon = moonLight != null ? -moonLight.transform.forward : new Vector3(0.3f, 0.35f, 0.9f).normalized;
            Shader.SetGlobalFloat(SkyOn, isActiveAndEnabled ? 1f : 0f);
            Shader.SetGlobalVector(MoonDir, toMoon.normalized);
            Shader.SetGlobalVector(SkyParams, new Vector4(horizonTone, zenithTone, starDensity, moonRadius * Mathf.Deg2Rad));
            Shader.SetGlobalFloat(DarkScale, isActiveAndEnabled ? darknessScale : 0f);
        }
    }
}
