using UnityEngine;

namespace Deckscent
{
    /// <summary>Vacillement d'une lumière de torche (en jeu seulement).</summary>
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [Tooltip("Intensité moyenne de la torche.")]
        [SerializeField, Min(0f)] float baseIntensity = 2.5f;
        [Tooltip("Amplitude du vacillement.")]
        [SerializeField, Min(0f)] float flicker = 0.35f;

        Light _light;
        float _time;

        void Awake()
        {
            _light = GetComponent<Light>();
            // Décalage au hasard : deux torches ne vacillent pas ensemble.
            _time = Random.value * 10f;
        }

        void Update()
        {
            _time += Time.deltaTime;
            _light.intensity = baseIntensity
                + Mathf.Sin(_time * 9f) * flicker * 0.5f
                + Mathf.Sin(_time * 23f + 1.3f) * flicker * 0.3f
                + Random.Range(-1f, 1f) * flicker * 0.2f;
        }
    }
}
