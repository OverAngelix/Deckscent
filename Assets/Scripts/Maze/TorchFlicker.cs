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

        [Tooltip("Quand un loup-garou hurle à moins de cette distance (mètres), la flamme s'embrase un instant. 0 = jamais.")]
        [SerializeField, Min(0f)] float howlReach = 20f;

        Light _light;
        float _time;
        float _flare;

        void Awake()
        {
            _light = GetComponent<Light>();
            // Décalage au hasard : deux torches ne vacillent pas ensemble.
            _time = Random.value * 10f;
        }

        void OnEnable() => Werewolf.Howled += OnHowl;
        void OnDisable() => Werewolf.Howled -= OnHowl;

        /// <summary>Le hurlement fait ronfler les flammes : l'intensité monte d'un coup puis retombe.</summary>
        void OnHowl(Vector3 from)
        {
            if (howlReach > 0f && (from - transform.position).sqrMagnitude <= howlReach * howlReach)
                _flare = 1f;
        }

        void Update()
        {
            _time += Time.deltaTime;
            _flare = Mathf.Max(0f, _flare - Time.deltaTime / 1.4f);
            _light.intensity = baseIntensity * (1f + 1.8f * _flare * _flare)
                + Mathf.Sin(_time * 9f) * flicker * 0.5f
                + Mathf.Sin(_time * 23f + 1.3f) * flicker * 0.3f
                + Random.Range(-1f, 1f) * flicker * 0.2f;
        }
    }
}
