using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Musicien de la musique du donjon : joue en temps réel, sur la piste Musique du mixer, le
    /// morceau à couches d'<see cref="AdventureSynth"/> (exploration, menace, combat, coffre,
    /// boss). Le chef d'orchestre (<see cref="MusicDirector"/>) lui donne la situation ; lui fait
    /// glisser chaque couche vers sa cible, en calant les entrées sur le rythme.
    ///
    /// Musique provisoire, synthétisée : pour de vrais morceaux (une piste par couche, même tempo),
    /// seul ce script changera, le chef d'orchestre restera le même.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class DungeonMusic : MonoBehaviour
    {
        [Header("Morceau")]
        [Tooltip("Tempo, en noires par minute.")]
        [SerializeField, Range(60f, 160f)] float tempo = 100f;
        [Tooltip("Volume général du morceau (avant les options Général et Musique).")]
        [SerializeField, Range(0f, 1f)] float master = 0.8f;
        [Tooltip("Durée du fondu d'entrée au lancement (secondes).")]
        [SerializeField, Min(0f)] float fadeIn = 3f;
        [Tooltip("Graine du tirage (scintillement du coffre…) ; -1 = différent à chaque lancement.")]
        [SerializeField] int seed = -1;

        [Header("Couches (volume maximal de chacune)")]
        [Tooltip("Exploration : basse, harpe, nappe, flûte. Toujours là.")]
        [SerializeField, Range(0f, 1f)] float baseLevel = 1f;
        [Tooltip("Menace (monstres en vue) : cordes en ostinato, trémolo qui frotte, tambour feutré à partir de 2 monstres.")]
        [SerializeField, Range(0f, 1f)] float threatLevel = 1f;
        [Tooltip("Combat (coups échangés) : tambours, caisse claire, basse qui martèle.")]
        [SerializeField, Range(0f, 1f)] float combatLevel = 1f;
        [Tooltip("Coffre proche (sans monstre ni combat) : scintillement de glockenspiel.")]
        [SerializeField, Range(0f, 1f)] float chestLevel = 1f;
        [Tooltip("Boss : cor grave, tambours lourds, autre suite d'accords.")]
        [SerializeField, Range(0f, 1f)] float bossLevel = 1f;

        [Header("Transitions")]
        [Tooltip("Secondes pour qu'une couche monte à son volume.")]
        [SerializeField, Min(0.05f)] float fadeUp = 0.6f;
        [Tooltip("Secondes pour qu'une couche redescende.")]
        [SerializeField, Min(0.05f)] float fadeDown = 1.5f;

        AdventureSynth _synth;

        /// <summary>Le synthétiseur (tests).</summary>
        public AdventureSynth Synth => _synth;

        void Awake()
        {
            _synth = new AdventureSynth(AudioSettings.outputSampleRate, seed >= 0 ? seed : System.Environment.TickCount);
            ApplySettings();
            _synth.FadeTo(1f, fadeIn);
            var source = GetComponent<AudioSource>();
            source.clip = null;
            source.loop = true;
            source.spatialBlend = 0f;
            if (!source.isPlaying)
                source.Play();
        }

        void OnValidate()
        {
            if (_synth != null)
                ApplySettings();
        }

        void ApplySettings()
        {
            _synth.Tempo = tempo;
            _synth.Master = master;
            _synth.FadeUp = fadeUp;
            _synth.FadeDown = fadeDown;
            _synth.LayerLevel[(int)AdventureSynth.Layer.Base] = baseLevel;
            _synth.LayerLevel[(int)AdventureSynth.Layer.Threat] = threatLevel;
            _synth.LayerLevel[(int)AdventureSynth.Layer.Combat] = combatLevel;
            _synth.LayerLevel[(int)AdventureSynth.Layer.Chest] = chestLevel;
            _synth.LayerLevel[(int)AdventureSynth.Layer.Boss] = bossLevel;
        }

        /// <summary>La situation, donnée par le chef d'orchestre.</summary>
        public void SetSituation(float threat, bool combat, bool chest, bool boss) =>
            _synth?.SetSituation(threat, combat, chest, boss);

        /// <summary>Fondu de sortie (mort du joueur).</summary>
        public void FadeOut(float seconds) => _synth?.FadeTo(0f, seconds);

        void OnAudioFilterRead(float[] data, int channels) => _synth?.Render(data, channels);
    }
}
