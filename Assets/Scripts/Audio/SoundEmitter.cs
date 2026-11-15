using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Deckscent
{
    /// <summary>
    /// Joue les effets sonores d'un objet (joueur, slime) sur la piste Effets du mixer
    /// (Resources/Mixage) : volume réglé par les options Général et Effets sonores. Chaque son
    /// est légèrement désaccordé au hasard pour qu'une série de pas ne sonne pas mécanique.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        [Tooltip("Variation de hauteur au hasard, en plus ou en moins (0,08 = ±8 %).")]
        [SerializeField, Range(0f, 0.5f)] float pitchJitter = 0.08f;

        AudioSource _source;

        /// <summary>Dernier son joué et nombre de sons joués (tests).</summary>
        public AudioClip LastClip { get; private set; }
        public int PlayCount { get; private set; }

        /// <summary>Les derniers sons joués, du plus ancien au plus récent (tests, 8 au plus).</summary>
        public IReadOnlyList<AudioClip> Recent => _recent;

        readonly List<AudioClip> _recent = new List<AudioClip>();

        public AudioSource Source => _source != null ? _source : _source = GetComponent<AudioSource>();

        void Awake()
        {
            // Par sécurité, si le prefab n'est pas branché : la piste Effets.
            if (Source.outputAudioMixerGroup == null)
            {
                var mixer = Resources.Load<AudioMixer>("Mixage");
                var groups = mixer != null ? mixer.FindMatchingGroups("Effets") : null;
                if (groups != null && groups.Length > 0)
                    Source.outputAudioMixerGroup = groups[0];
            }
            Source.playOnAwake = false;
        }

        /// <summary>Joue un son par-dessus les autres (plusieurs peuvent se chevaucher).</summary>
        public void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
                return;
            Source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            Source.PlayOneShot(clip, volume);
            LastClip = clip;
            PlayCount++;
            _recent.Add(clip);
            if (_recent.Count > 8)
                _recent.RemoveAt(0);
        }
    }
}
