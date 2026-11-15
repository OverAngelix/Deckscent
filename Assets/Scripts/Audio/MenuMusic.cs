using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Musique du menu, synthétisée en temps réel (aucun fichier audio) : un bourdon grave en ré
    /// mineur qui respire, des cloches éparses tirées dans une gamme pentatonique avec un écho,
    /// et un souffle de vent. Ambiance « descente » : sombre, lente, beaucoup de silence.
    ///
    /// Générée dans <see cref="OnAudioFilterRead"/> (fil audio) sur une AudioSource sans clip.
    /// Volume : sort sur la piste Musique du mixer (options Général et Musique, <see cref="GameSettings"/>).
    /// Musique provisoire : pour un vrai morceau, retirer ce script et donner un clip à l'AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusic : MonoBehaviour
    {
        [Header("Mélange")]
        [Tooltip("Volume du bourdon grave.")]
        [SerializeField, Range(0f, 1f)] float droneLevel = 0.22f;
        [Tooltip("Volume des cloches.")]
        [SerializeField, Range(0f, 1f)] float bellLevel = 0.16f;
        [Tooltip("Volume du souffle de vent.")]
        [SerializeField, Range(0f, 1f)] float windLevel = 0.05f;
        [Tooltip("Secondes entre deux cloches (tirées au hasard entre ces deux valeurs).")]
        [SerializeField] Vector2 bellInterval = new Vector2(2.2f, 5.5f);
        [Tooltip("Durée du fondu d'entrée (secondes).")]
        [SerializeField, Min(0f)] float fadeIn = 3f;
        [Tooltip("Graine du tirage des cloches (-1 = différent à chaque lancement).")]
        [SerializeField] int seed = -1;

        const float Tau = Mathf.PI * 2f;
        // Bourdon : ré 2, la 2, ré 3 (Hz), chacun doublé et désaccordé pour battre.
        static readonly float[] Drone = { 73.42f, 110f, 146.83f };
        static readonly float[] DroneGain = { 1f, 0.55f, 0.35f };
        const float Detune = 0.18f;
        // Ré mineur pentatonique, octaves 4 et 5 (ré, fa, sol, la, do).
        static readonly float[] BellNotes = { 293.66f, 349.23f, 392f, 440f, 523.25f, 587.33f, 698.46f, 783.99f };
        const float BellDecay = 2.2f;
        const float BellPartial = 2.76f; // rapport inharmonique : timbre de cloche
        const int MaxBells = 6;
        const float EchoTime = 0.42f, EchoFeedback = 0.45f;

        struct Bell { public float F, Age, P1, P2, Amp, Pan; public bool Alive; }

        // Tout ce qui suit est lu et écrit sur le fil audio.
        readonly Bell[] _bells = new Bell[MaxBells];
        readonly double[] _dronePhase = new double[6];
        float[] _echoL, _echoR;
        int _echoPos;
        System.Random _rng;
        double _t;
        float _gain, _nextBell = 1f, _windLp, _windLp2;
        float _sampleRate = 48000f;
        // Écrits par le fil principal.
        volatile float _gainTarget = 1f;
        volatile float _gainSpeed = 1f;

        /// <summary>Volume du fondu en cours, de 0 à 1 (lu pour les tests).</summary>
        public float Gain => _gain;
        public bool IsFadingOut => _gainTarget <= 0f;

        void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _rng = seed >= 0 ? new System.Random(seed) : new System.Random();
            _echoL = new float[Mathf.Max(1, (int)(EchoTime * _sampleRate))];
            _echoR = new float[_echoL.Length];
            _gainSpeed = 1f / Mathf.Max(fadeIn, 0.01f);
            var source = GetComponent<AudioSource>();
            source.clip = null;
            source.loop = true;
            source.spatialBlend = 0f;
            if (!source.isPlaying)
                source.Play();
        }

        /// <summary>Fondu de sortie (avant de lancer la partie).</summary>
        public void FadeOut(float duration)
        {
            _gainSpeed = 1f / Mathf.Max(duration, 0.01f);
            _gainTarget = 0f;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_echoL == null)
                return;
            var dt = 1f / _sampleRate;
            for (var i = 0; i < data.Length; i += channels)
            {
                Sample(dt, out var l, out var r);
                if (channels == 1)
                    data[i] = (l + r) * 0.5f;
                else
                {
                    data[i] = l;
                    data[i + 1] = r;
                    for (var c = 2; c < channels; c++)
                        data[i + c] = 0f;
                }
            }
        }

        void Sample(float dt, out float left, out float right)
        {
            _t += dt;
            _gain = Mathf.MoveTowards(_gain, _gainTarget, _gainSpeed * dt);

            // Bourdon : sinus + un peu d'octave, amplitude qui respire lentement.
            var drone = 0f;
            for (var k = 0; k < Drone.Length; k++)
                for (var side = 0; side < 2; side++)
                {
                    var idx = k * 2 + side;
                    var f = Drone[k] + (side == 1 ? Detune : -Detune) * (k + 1);
                    var ph = _dronePhase[idx] + f * dt;
                    ph -= System.Math.Floor(ph);
                    _dronePhase[idx] = ph;
                    var a = (float)(Tau * ph);
                    drone += (Mathf.Sin(a) + 0.25f * Mathf.Sin(a * 2f)) * DroneGain[k];
                }
            drone *= 0.18f * (0.75f + 0.25f * Mathf.Sin((float)(Tau * _t / 19.0))) * droneLevel / 0.22f;

            // Cloches.
            _nextBell -= dt;
            if (_nextBell <= 0f)
            {
                SpawnBell();
                _nextBell = Range(bellInterval.x, bellInterval.y);
            }
            float bl = 0f, br = 0f;
            for (var i = 0; i < MaxBells; i++)
            {
                if (!_bells[i].Alive)
                    continue;
                ref var b = ref _bells[i];
                b.Age += dt;
                if (b.Age < 0f)
                    continue; // deuxième note d'un accord, pas encore jouée
                var env = Mathf.Min(b.Age / 0.006f, 1f) * Mathf.Exp(-b.Age / BellDecay);
                if (env < 0.0005f && b.Age > 0.1f)
                {
                    b.Alive = false;
                    continue;
                }
                b.P1 = Frac(b.P1 + b.F * dt);
                b.P2 = Frac(b.P2 + b.F * BellPartial * dt);
                // La partielle aiguë s'éteint plus vite.
                var v = (Mathf.Sin(Tau * b.P1) + 0.35f * Mathf.Sin(Tau * b.P2) * Mathf.Exp(-b.Age / 0.5f)) * env * b.Amp;
                bl += v * (1f - b.Pan);
                br += v * b.Pan;
            }

            // Écho, croisé gauche / droite pour élargir.
            var el = _echoL[_echoPos];
            var er = _echoR[_echoPos];
            _echoL[_echoPos] = bl + er * EchoFeedback;
            _echoR[_echoPos] = br + el * EchoFeedback;
            _echoPos = (_echoPos + 1) % _echoL.Length;
            bl = (bl + el * 0.6f) * bellLevel;
            br = (br + er * 0.6f) * bellLevel;

            // Vent : bruit blanc passé deux fois en passe-bas, qui enfle et retombe.
            // (coefficients de Godot à 22 050 Hz, ramenés à la fréquence de sortie)
            var noise = (float)_rng.NextDouble() * 2f - 1f;
            var k1 = 0.02f * 22050f / _sampleRate;
            var k2 = 0.05f * 22050f / _sampleRate;
            _windLp += (noise - _windLp) * k1;
            _windLp2 += (_windLp - _windLp2) * k2;
            var wind = _windLp2 * 6f * windLevel * (0.5f + 0.5f * Mathf.Sin((float)(Tau * _t / 11.0) + 1.3f));

            var g = _gain * _gain;
            left = Mathf.Clamp(drone + bl + wind, -1f, 1f) * g;
            right = Mathf.Clamp(drone + br + wind, -1f, 1f) * g;
        }

        void SpawnBell()
        {
            var f = BellNotes[_rng.Next(BellNotes.Length)];
            if (!Add(f, 0f, Range(0.5f, 1f)))
                return;
            // Parfois une deuxième note, une quinte ou une octave plus bas, juste après.
            if (_rng.NextDouble() < 0.3)
                Add(f / (_rng.NextDouble() < 0.5 ? 1.5f : 2f), -0.35f, 0.6f);
        }

        bool Add(float f, float age, float amp)
        {
            for (var i = 0; i < MaxBells; i++)
                if (!_bells[i].Alive)
                {
                    _bells[i] = new Bell { F = f, Age = age, Amp = amp, Pan = Range(0.25f, 0.75f), Alive = true };
                    return true;
                }
            return false;
        }

        float Range(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
        static float Frac(float x) => x - Mathf.Floor(x);
    }
}
