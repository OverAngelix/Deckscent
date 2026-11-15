using System;

namespace Deckscent
{
    /// <summary>
    /// Synthétiseur de la musique du donjon (aucun fichier audio), sans MonoBehaviour : appelé
    /// sur le fil audio par <see cref="DungeonMusic"/>, et testable hors Play mode.
    ///
    /// Un seul morceau, en ré dorien, à tempo fixe, découpé en **couches** qui jouent toutes sur la
    /// même horloge (doubles croches). La situation donne une cible à chaque couche et son volume
    /// y glisse doucement :
    /// - **base** (exploration) : basse pincée, arpège de harpe, nappe, frappe de tambour sur cadre
    ///   et une mélodie de flûte (une phrase de 8 mesures, qui se repose un tour sur quatre) ;
    /// - **menace** (entre-deux) : cordes en ostinato, trémolo de deux notes qui frottent, tambour
    ///   feutré à partir de deux monstres ; la flûte baisse, l'arpège s'assombrit ;
    /// - **combat** : tambours, caisse claire, grelot, basse qui martèle (entre à la croche suivante) ;
    /// - **coffre** : un scintillement de glockenspiel par-dessus (entre au temps suivant) ;
    /// - **boss** : forte variation, à la mesure suivante et marquée d'une cymbale : autre suite
    ///   d'accords (ré phrygien), cor grave à la place de la flûte, tambours lourds.
    ///
    /// Une petite réverbération (Freeverb simplifié) donne l'espace d'une salle de pierre.
    /// </summary>
    public sealed class AdventureSynth
    {
        public enum Layer { Base, Threat, Combat, Chest, Boss }

        enum Kind { Pluck, Pad, Flute, Glock, Horn, Kick, Snare, Hat, Frame, Bowed, Crash }

        const float Tau = (float)(Math.PI * 2.0);
        const int MaxVoices = 64;
        const int MaxPlucks = 24;
        const int StepsPerBar = 16;

        // --- Réglages (écrits par le fil principal) ---------------------------------------------

        /// <summary>Tempo en noires par minute.</summary>
        public volatile float Tempo = 100f;
        /// <summary>Volume général du morceau.</summary>
        public volatile float Master = 0.8f;
        /// <summary>Volume maximal de chaque couche (inspecteur).</summary>
        public readonly float[] LayerLevel = { 1f, 1f, 1f, 1f, 1f };
        /// <summary>Secondes pour qu'une couche monte, et pour qu'elle redescende.</summary>
        public volatile float FadeUp = 0.6f, FadeDown = 1.5f;

        // Cibles de la situation (fil principal → fil audio).
        volatile float _threatTarget;
        volatile bool _combatTarget, _chestTarget, _bossTarget;
        volatile float _masterTarget = 1f, _masterSpeed = 1f / 3f;

        // --- État du fil audio ------------------------------------------------------------------

        readonly float _rate;
        readonly Random _rng;
        readonly Voice[] _voices = new Voice[MaxVoices];
        readonly float[][] _pluckBuffers = new float[MaxPlucks][];
        readonly bool[] _pluckUsed = new bool[MaxPlucks];
        readonly float[] _gain = new float[5];
        readonly Reverb _reverbL, _reverbR;
        double _stepClock; // en doubles croches
        long _step = -1;
        float _master;
        bool _combatOn, _chestOn, _bossOn;
        int _phraseBar; // mesure dans la phrase en cours (exploration 8, boss 4)
        int _cycle;     // nombre de phrases d'exploration jouées
        double _droneP1, _droneP2, _droneP3, _tremP;

        // --- Pour les tests ---
        public int DrumHits { get; private set; }
        public int GlockNotes { get; private set; }
        public int FluteNotes { get; private set; }
        public int HornNotes { get; private set; }
        public int Stingers { get; private set; }
        public int StringNotes { get; private set; }
        public int MuffledHits { get; private set; }
        public bool InBoss => _bossOn;
        public bool CombatOn => _combatOn;
        public float LayerGain(Layer layer) => _gain[(int)layer];

        struct Voice
        {
            public bool Alive;
            public Kind Kind;
            public Layer Layer;
            public float Freq, Age, Hold, Amp, Pan, Send;
            public double Phase, Phase2, Phase3;
            public float Low, Band, Aux;
            public int Buffer, Length, Pos;
            public float Decay;
        }

        public AdventureSynth(float sampleRate, int seed = 1)
        {
            _rate = sampleRate;
            _rng = new Random(seed);
            var maxLength = (int)(sampleRate / 30f) + 2;
            for (var i = 0; i < MaxPlucks; i++)
                _pluckBuffers[i] = new float[maxLength];
            _reverbL = new Reverb(sampleRate, 0);
            _reverbR = new Reverb(sampleRate, 23);
        }

        /// <summary>La situation : menace (0 à 1), combat, coffre proche, boss.</summary>
        public void SetSituation(float threat, bool combat, bool chest, bool boss)
        {
            _threatTarget = Math.Max(0f, Math.Min(1f, threat));
            _combatTarget = combat;
            _chestTarget = chest;
            _bossTarget = boss;
        }

        /// <summary>Fondu du morceau entier vers `target` (0 = silence) en `seconds`.</summary>
        public void FadeTo(float target, float seconds)
        {
            _masterSpeed = 1f / Math.Max(seconds, 0.01f);
            _masterTarget = target;
        }

        /// <summary>Remplit un tampon entrelacé (`channels` voies), comme OnAudioFilterRead.</summary>
        public void Render(float[] data, int channels)
        {
            var dt = 1f / _rate;
            for (var i = 0; i < data.Length; i += channels)
            {
                Sample(dt, out var l, out var r);
                if (channels == 1)
                    data[i] = 0.5f * (l + r);
                else
                {
                    data[i] = l;
                    data[i + 1] = r;
                    for (var c = 2; c < channels; c++)
                        data[i + c] = 0f;
                }
            }
        }

        // --- Musique ------------------------------------------------------------------------------

        // Ré dorien. Exploration : Rém, Do, Sol, Rém, Fa, Do, Sol, La (le La majeur relance la phrase).
        static readonly int[] ExploreRoots = { 50, 48, 43, 50, 41, 48, 43, 45 };
        static readonly bool[] ExploreMinor = { true, false, false, true, false, false, false, false };
        // Boss (ré phrygien, plus sombre) : Rém, Mi♭, Si♭, La.
        static readonly int[] BossRoots = { 50, 51, 46, 45 };
        static readonly bool[] BossMinor = { true, false, false, false };

        // Mélodie de flûte : (mesure, pas, note MIDI, durée en pas). Ré 4 = 62.
        static readonly int[,] ExploreMelody =
        {
            { 0, 0, 69, 6 }, { 0, 6, 74, 2 }, { 0, 8, 72, 4 }, { 0, 12, 69, 4 },
            { 1, 0, 67, 6 }, { 1, 6, 64, 2 }, { 1, 8, 67, 8 },
            { 2, 0, 71, 4 }, { 2, 4, 74, 4 }, { 2, 8, 71, 2 }, { 2, 10, 69, 2 }, { 2, 12, 67, 4 },
            { 3, 0, 69, 12 },
            { 4, 0, 65, 4 }, { 4, 4, 69, 4 }, { 4, 8, 72, 6 }, { 4, 14, 74, 2 },
            { 5, 0, 76, 6 }, { 5, 6, 74, 2 }, { 5, 8, 72, 4 }, { 5, 12, 67, 4 },
            { 6, 0, 71, 4 }, { 6, 4, 72, 2 }, { 6, 6, 74, 6 }, { 6, 12, 71, 4 },
            { 7, 0, 73, 8 }, { 7, 8, 76, 4 }, { 7, 12, 69, 4 },
        };

        // Motif du cor (boss), une octave plus bas.
        static readonly int[,] BossMelody =
        {
            { 0, 0, 62, 6 }, { 0, 6, 57, 2 }, { 0, 8, 62, 4 }, { 0, 12, 65, 4 },
            { 1, 0, 63, 8 }, { 1, 8, 62, 4 }, { 1, 12, 58, 4 },
            { 2, 0, 62, 6 }, { 2, 6, 65, 2 }, { 2, 8, 62, 4 }, { 2, 12, 60, 4 },
            { 3, 0, 61, 8 }, { 3, 8, 64, 4 }, { 3, 12, 57, 4 },
        };

        static float Midi(int note) => 440f * (float)Math.Pow(2.0, (note - 69) / 12.0);

        /// <summary>Notes de l'accord (fondamentale, tierce, quinte), ramenées autour de `center`.</summary>
        static void Chord(int root, bool minor, int center, int[] notes)
        {
            notes[0] = root;
            notes[1] = root + (minor ? 3 : 4);
            notes[2] = root + 7;
            for (var i = 0; i < 3; i++)
            {
                while (notes[i] < center - 6)
                    notes[i] += 12;
                while (notes[i] >= center + 6)
                    notes[i] -= 12;
            }
            Array.Sort(notes);
        }

        readonly int[] _chord = new int[3];

        void OnStep(long step)
        {
            var inBar = (int)(step % StepsPerBar);
            var beatStart = inBar % 4 == 0;

            // Entrées calées sur le rythme : le combat à la croche, le coffre au temps, le boss à
            // la mesure (sa cymbale tombe sur le premier temps).
            if (inBar % 2 == 0)
                _combatOn = _combatTarget;
            if (beatStart)
                _chestOn = _chestTarget;
            if (inBar == 0)
            {
                if (step > 0)
                    AdvanceBar();
                if (_bossTarget != _bossOn)
                {
                    _bossOn = _bossTarget;
                    _phraseBar = 0;
                    if (_bossOn)
                        Stinger();
                }
            }

            var roots = _bossOn ? BossRoots : ExploreRoots;
            var minor = _bossOn ? BossMinor : ExploreMinor;
            var bar = _phraseBar % roots.Length;
            var root = roots[bar];
            var isMinor = minor[bar];
            var stepTime = 15f / Tempo; // durée d'une double croche
            var threat = _gain[(int)Layer.Threat];

            // --- Base : basse, arpège, nappe, tambour sur cadre, mélodie ---
            var bassRoot = root;
            while (bassRoot > 45)
                bassRoot -= 12;
            if (inBar == 0)
                Pluck(Layer.Base, Midi(bassRoot), 0.55f, 1.6f, 0f, 0.1f, 0.35f);
            else if (inBar == 8)
                Pluck(Layer.Base, Midi(bassRoot + 7), 0.45f, 1.4f, 0f, 0.1f, 0.35f);
            else if (inBar == 14)
                Pluck(Layer.Base, Midi(bassRoot + 12), 0.3f, 0.6f, 0f, 0.1f, 0.35f);

            if (inBar % 2 == 0)
            {
                Chord(root, isMinor, 64, _chord);
                int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };
                var index = pattern[inBar / 2];
                var note = index == 3 ? _chord[0] + 12 : _chord[index];
                // La menace assombrit l'arpège (plus étouffé).
                var brightness = 0.55f - 0.4f * threat;
                Pluck(Layer.Base, Midi(note), 0.22f, 1.2f, inBar % 4 == 0 ? -0.35f : 0.35f, brightness, 0.3f);
            }

            if (inBar == 0)
            {
                Chord(root, isMinor, 57, _chord);
                var hold = StepsPerBar * stepTime;
                for (var i = 0; i < 3; i++)
                    Start(Kind.Pad, Layer.Base, Midi(_chord[i]), 0.07f, hold, (i - 1) * 0.4f, 0.45f);
            }

            if (inBar == 0 || inBar == 10)
                Start(Kind.Frame, Layer.Base, 0f, inBar == 0 ? 0.35f : 0.22f, 0f, 0f, 0.15f);

            if (!_bossOn)
            {
                // La flûte se repose une phrase sur quatre, et monte d'une octave sur la quatrième.
                var turn = _cycle % 4;
                if (turn != 1)
                    PlayMelody(ExploreMelody, Kind.Flute, Layer.Base, inBar, stepTime, turn == 3 ? 12 : 0, turn == 3 ? 0.14f : 0.2f);
            }

            // --- Menace : l'entre-deux entre exploration et combat ---
            // Cordes en ostinato : croches courtes et répétées sur la fondamentale (médium), quinte à la fin.
            // Le trémolo qui frotte est continu, dans Sample.
            if (inBar % 2 == 0)
            {
                var stringRoot = root;
                while (stringRoot >= 55)
                    stringRoot -= 12;
                while (stringRoot < 43)
                    stringRoot += 12;
                var note = inBar == 14 ? stringRoot + 7 : stringRoot + (inBar == 6 ? 12 : 0);
                if (Start(Kind.Bowed, Layer.Threat, Midi(note), beatStart ? 0.5f : 0.34f, stepTime * 1.3f, inBar % 4 == 0 ? -0.2f : 0.2f, 0.2f)
                    && _gain[(int)Layer.Threat] > 0.05f)
                    StringNotes++;
            }
            // Tambour feutré sur les temps 1 et 3, seulement à partir de 2 monstres en vue.
            if ((inBar == 0 || inBar == 8) && _threatTarget >= 0.7f
                && Start(Kind.Frame, Layer.Threat, 150f, 0.5f, 0f, 0f, 0.1f) && _gain[(int)Layer.Threat] > 0.05f)
                MuffledHits++;

            // --- Combat : tambours et basse qui martèle ---
            if (!_bossOn)
            {
                if (inBar == 0 || inBar == 3 || inBar == 8 || inBar == 10)
                    Drum(Kind.Kick, Layer.Combat, 0.7f);
                if (inBar == 4 || inBar == 12)
                    Drum(Kind.Snare, Layer.Combat, 0.5f);
                if (inBar % 2 == 0)
                    Drum(Kind.Hat, Layer.Combat, inBar % 4 == 2 ? 0.22f : 0.12f);
                // Roulement de toms à la fin de chaque groupe de 4 mesures.
                if (_phraseBar % 4 == 3 && inBar >= 12)
                    Start(Kind.Kick, Layer.Combat, 140f - (inBar - 12) * 18f, 0.45f, 0f, (inBar - 13.5f) * 0.25f, 0.12f);
                if (inBar % 2 == 0)
                    Pluck(Layer.Combat, Midi(bassRoot + (inBar % 8 == 6 ? 12 : 0)), 0.35f, 0.3f, 0f, 0.3f, 0.1f);
            }

            // --- Coffre : scintillement de glockenspiel ---
            if (inBar % 2 == 1 && (inBar % 4 == 1 || _rng.NextDouble() < 0.35))
            {
                Chord(root, isMinor, 81, _chord);
                var note = _chord[_rng.Next(3)] + (_rng.NextDouble() < 0.3 ? 12 : 0);
                if (Start(Kind.Glock, Layer.Chest, Midi(note), 0.11f, 0f, (float)(_rng.NextDouble() * 1.2 - 0.6), 0.5f))
                    GlockNotes += _gain[(int)Layer.Chest] > 0.05f ? 1 : 0;
            }

            // --- Boss : cor, tambours lourds ---
            if (_bossOn)
            {
                PlayMelody(BossMelody, Kind.Horn, Layer.Boss, inBar, stepTime, 0, 0.25f);
                if (inBar % 4 == 0 || inBar == 2 || inBar == 10)
                    Drum(Kind.Kick, Layer.Boss, 0.8f);
                if (inBar == 4 || inBar == 12)
                    Drum(Kind.Snare, Layer.Boss, 0.6f);
                Drum(Kind.Hat, Layer.Boss, inBar % 2 == 0 ? 0.18f : 0.08f);
                if (inBar % 2 == 0)
                    Pluck(Layer.Boss, Midi(bassRoot), 0.4f, 0.3f, 0f, 0.25f, 0.1f);
                if (inBar == 0 && _phraseBar % 4 == 0)
                    Start(Kind.Crash, Layer.Boss, 0f, 0.25f, 0f, 0f, 0.4f);
            }
        }

        void AdvanceBar()
        {
            _phraseBar++;
            var length = _bossOn ? BossRoots.Length : ExploreRoots.Length;
            if (_phraseBar >= length)
            {
                _phraseBar = 0;
                if (!_bossOn)
                    _cycle++;
            }
        }

        void PlayMelody(int[,] melody, Kind kind, Layer layer, int inBar, float stepTime, int transpose, float amp)
        {
            var bar = _phraseBar % (kind == Kind.Horn ? BossRoots.Length : ExploreRoots.Length);
            for (var i = 0; i < melody.GetLength(0); i++)
                if (melody[i, 0] == bar && melody[i, 1] == inBar)
                {
                    var hold = melody[i, 3] * stepTime * 0.92f;
                    if (Start(kind, layer, Midi(melody[i, 2] + transpose), amp, hold, 0.1f, 0.4f))
                    {
                        if (kind == Kind.Flute)
                            FluteNotes++;
                        else
                            HornNotes++;
                    }
                }
        }

        void Drum(Kind kind, Layer layer, float amp)
        {
            if (Start(kind, layer, 0f, amp, 0f, kind == Kind.Hat ? 0.3f : 0f, 0.12f) && _gain[(int)layer] > 0.05f)
                DrumHits++;
        }

        void Stinger()
        {
            Stingers++;
            Start(Kind.Crash, Layer.Boss, 0f, 0.5f, 0f, 0f, 0.5f);
            Start(Kind.Kick, Layer.Boss, 70f, 1f, 0f, 0f, 0.3f);
            Start(Kind.Horn, Layer.Boss, Midi(38), 0.35f, 1.6f, 0f, 0.5f);
        }

        // --- Voix -----------------------------------------------------------------------------------

        bool Start(Kind kind, Layer layer, float freq, float amp, float hold, float pan, float send) =>
            StartVoice(kind, layer, freq, amp, hold, pan, send) >= 0;

        /// <summary>Lance une voix ; renvoie sa place, ou -1 si la couche est muette.</summary>
        int StartVoice(Kind kind, Layer layer, float freq, float amp, float hold, float pan, float send)
        {
            // Une couche muette ne joue rien (économise les voix), sauf si elle est en train de monter.
            if (_gain[(int)layer] < 0.001f && !Rising(layer))
                return -1;
            var slot = -1;
            var oldest = -1f;
            for (var i = 0; i < MaxVoices; i++)
            {
                if (!_voices[i].Alive)
                {
                    slot = i;
                    break;
                }
                if (_voices[i].Age > oldest && _voices[i].Kind != Kind.Pad)
                {
                    oldest = _voices[i].Age;
                    slot = i;
                }
            }
            if (slot < 0)
                return -1;
            Release(slot);
            _voices[slot] = new Voice
            {
                Alive = true, Kind = kind, Layer = layer, Freq = freq, Amp = amp, Hold = hold,
                Pan = Math.Max(-1f, Math.Min(1f, pan)), Send = send, Buffer = -1,
            };
            return slot;
        }

        bool Rising(Layer layer)
        {
            switch (layer)
            {
                case Layer.Base: return true;
                case Layer.Threat: return _threatTarget > 0f;
                case Layer.Combat: return _combatOn;
                case Layer.Chest: return _chestOn;
                default: return _bossOn;
            }
        }

        /// <summary>Corde pincée (Karplus-Strong) : bruit filtré qui boucle dans un tampon d'une période.</summary>
        void Pluck(Layer layer, float freq, float amp, float decaySeconds, float pan, float brightness, float send)
        {
            var buffer = -1;
            for (var i = 0; i < MaxPlucks; i++)
                if (!_pluckUsed[i])
                {
                    buffer = i;
                    break;
                }
            if (buffer < 0)
                return;
            var slot = StartVoice(Kind.Pluck, layer, freq, amp, 0f, pan, send);
            if (slot < 0)
                return;
            var data = _pluckBuffers[buffer];
            var length = Math.Max(2, Math.Min(data.Length - 1, (int)Math.Round(_rate / freq)));
            // Excitation : bruit plus ou moins adouci (brightness bas = corde feutrée).
            var low = 0f;
            for (var i = 0; i < length; i++)
            {
                var noise = (float)(_rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * (0.2f + 0.8f * brightness);
                data[i] = low;
            }
            _pluckUsed[buffer] = true;
            ref var v = ref _voices[slot];
            v.Buffer = buffer;
            v.Length = length;
            v.Pos = 0;
            v.Decay = (float)Math.Pow(0.001, 1.0 / (freq * Math.Max(decaySeconds, 0.05f)));
            v.Aux = brightness;
        }

        void Release(int slot)
        {
            if (_voices[slot].Alive && _voices[slot].Buffer >= 0)
                _pluckUsed[_voices[slot].Buffer] = false;
            _voices[slot].Alive = false;
        }

        float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        /// <summary>Un échantillon d'une voix (mono). Met Alive à faux quand elle s'est éteinte.</summary>
        float RenderVoice(ref Voice v, float dt)
        {
            var t = v.Age;
            v.Age += dt;
            float s;
            switch (v.Kind)
            {
                case Kind.Pluck:
                {
                    var data = _pluckBuffers[v.Buffer];
                    var next = (v.Pos + 1) % v.Length;
                    s = data[v.Pos];
                    // Moyenne de deux échantillons = la corde perd ses aigus ; Decay = elle s'éteint.
                    var mix = 0.5f + 0.45f * v.Aux;
                    data[v.Pos] = v.Decay * (mix * data[v.Pos] + (1f - mix) * data[next]);
                    v.Pos = next;
                    if (t > 4f || (t > 0.3f && Math.Abs(s) < 0.0005f && Math.Abs(data[next]) < 0.0005f))
                        return Kill(ref v, s);
                    return s * v.Amp * Math.Min(1f, t / 0.002f);
                }
                case Kind.Pad:
                {
                    // Nappe douce : deux oscillateurs à peine désaccordés, quelques harmoniques, montée lente.
                    v.Phase += v.Freq * 1.003 * dt;
                    v.Phase2 += v.Freq * 0.997 * dt;
                    var a = Tau * (float)(v.Phase % 1.0);
                    var b = Tau * (float)(v.Phase2 % 1.0);
                    s = (float)(Math.Sin(a) + Math.Sin(b) + 0.3 * (Math.Sin(2 * a) + Math.Sin(2 * b)) + 0.1 * Math.Sin(3 * a)) * 0.4f;
                    var env = Envelope(t, 0.6f, v.Hold, 0.9f);
                    if (env < 0f)
                        return Kill(ref v, 0f);
                    return s * env * v.Amp;
                }
                case Kind.Flute:
                case Kind.Horn:
                {
                    var flute = v.Kind == Kind.Flute;
                    var vibrato = 1f + (flute ? 0.005f : 0.003f) * (float)Math.Sin(Tau * 5f * t) * Math.Min(1f, Math.Max(0f, t - 0.15f) / 0.3f);
                    v.Phase += v.Freq * vibrato * dt;
                    var a = Tau * (float)(v.Phase % 1.0);
                    if (flute)
                    {
                        s = (float)(Math.Sin(a) + 0.15 * Math.Sin(2 * a) + 0.05 * Math.Sin(3 * a));
                        v.Low += (Noise() - v.Low) * 0.08f; // souffle
                        s += v.Low * 0.15f;
                    }
                    else
                    {
                        // Cor : les harmoniques arrivent après la note (le cuivre « s'ouvre »).
                        var open = Math.Min(1f, t / 0.15f);
                        s = (float)(Math.Sin(a) + 0.6 * open * Math.Sin(2 * a) + 0.45 * open * Math.Sin(3 * a)
                            + 0.3 * open * Math.Sin(4 * a) + 0.18 * open * Math.Sin(5 * a)) * 0.55f;
                    }
                    var env = Envelope(t, flute ? 0.06f : 0.08f, v.Hold, flute ? 0.15f : 0.25f);
                    if (env < 0f)
                        return Kill(ref v, 0f);
                    return s * env * v.Amp;
                }
                case Kind.Glock:
                {
                    var p = Tau * v.Freq * t;
                    s = (float)(Math.Sin(p) * Math.Exp(-t / 1.1) + 0.4 * Math.Sin(p * 2.756) * Math.Exp(-t / 0.35)
                        + 0.2 * Math.Sin(p * 5.404) * Math.Exp(-t / 0.12));
                    if (t > 3f)
                        return Kill(ref v, 0f);
                    return s * v.Amp * Math.Min(1f, t / 0.001f);
                }
                case Kind.Kick:
                case Kind.Frame:
                {
                    // Tambour grave : la peau tendue chute vers le grave (comme un taïko), et un claquement.
                    var frame = v.Kind == Kind.Frame;
                    var top = v.Freq > 0f ? v.Freq : (frame ? 120f : 95f);
                    var bottom = frame ? top * 0.75f : top * 0.55f;
                    var f = bottom + (top - bottom) * (float)Math.Exp(-t / 0.05);
                    v.Phase += f * dt;
                    v.Low += (Noise() - v.Low) * 0.3f;
                    s = (float)Math.Sin(Tau * (v.Phase % 1.0)) * (float)Math.Exp(-t / (frame ? 0.28 : 0.22))
                        + v.Low * 0.6f * (float)Math.Exp(-t / 0.008);
                    if (t > 1f)
                        return Kill(ref v, 0f);
                    return s * v.Amp * Math.Min(1f, t / 0.002f);
                }
                case Kind.Snare:
                {
                    Filter(Noise(), 1800f, 0.7f, ref v.Low, ref v.Band);
                    v.Phase += 190f * dt;
                    s = v.Band * 1.4f * (float)Math.Exp(-t / 0.12) + (float)Math.Sin(Tau * (v.Phase % 1.0)) * 0.5f * (float)Math.Exp(-t / 0.05);
                    if (t > 0.6f)
                        return Kill(ref v, 0f);
                    return s * v.Amp;
                }
                case Kind.Hat:
                {
                    var n = Noise();
                    v.Low += (n - v.Low) * 0.35f;
                    s = (n - v.Low) * (float)Math.Exp(-t / 0.03);
                    if (t > 0.2f)
                        return Kill(ref v, 0f);
                    return s * v.Amp;
                }
                case Kind.Bowed:
                {
                    // Corde frottée, courte (staccato) : harmoniques en 1/n, adoucies par un filtre.
                    v.Phase += v.Freq * dt;
                    var a = Tau * (float)(v.Phase % 1.0);
                    var raw = (float)(Math.Sin(a) + 0.5 * Math.Sin(2 * a) + 0.33 * Math.Sin(3 * a) + 0.25 * Math.Sin(4 * a)
                        + 0.2 * Math.Sin(5 * a) + 0.16 * Math.Sin(6 * a));
                    v.Low += (raw - v.Low) * 0.35f;
                    var env = Envelope(t, 0.012f, v.Hold, 0.07f);
                    if (env < 0f)
                        return Kill(ref v, 0f);
                    return v.Low * 0.6f * env * v.Amp;
                }
                default: // Crash : bruit aigu qui s'éteint lentement.
                {
                    var n = Noise();
                    v.Low += (n - v.Low) * 0.5f;
                    Filter(n - v.Low, 6000f, 0.9f, ref v.Aux, ref v.Band);
                    s = ((n - v.Low) * 0.6f + v.Band * 0.6f) * (float)Math.Exp(-t / 1.6) * Math.Min(1f, t / 0.002f);
                    if (t > 6f)
                        return Kill(ref v, 0f);
                    return s * v.Amp;
                }
            }
        }

        float Kill(ref Voice v, float _)
        {
            if (v.Buffer >= 0)
                _pluckUsed[v.Buffer] = false;
            v.Alive = false;
            return 0f;
        }

        /// <summary>Enveloppe : montée `attack`, tenue jusqu'à `hold`, extinction `release`. -1 quand c'est fini.</summary>
        static float Envelope(float t, float attack, float hold, float release)
        {
            if (t < attack)
                return t / attack;
            if (t < hold)
                return 1f;
            var r = (t - Math.Max(hold, attack)) / release;
            return r >= 1f ? -1f : 1f - r;
        }

        void Filter(float input, float cutoff, float damping, ref float low, ref float band)
        {
            var f = 2f * (float)Math.Sin(Math.PI * Math.Min(cutoff, _rate / 6f) / _rate);
            low += f * band;
            var high = input - low - damping * band;
            band += f * high;
        }

        // --- Mixage ---------------------------------------------------------------------------------

        void Sample(float dt, out float left, out float right)
        {
            // Horloge : une double croche = 1/4 de temps.
            _stepClock += dt * Tempo / 15.0;
            var step = (long)Math.Floor(_stepClock);
            while (_step < step)
            {
                _step++;
                OnStep(_step);
            }

            // Les couches glissent vers leur cible.
            var up = dt / Math.Max(FadeUp, 0.01f);
            var down = dt / Math.Max(FadeDown, 0.01f);
            var bossMix = _gain[(int)Layer.Boss];
            Glide(Layer.Base, 1f, up, down);
            Glide(Layer.Threat, _bossOn ? 0.6f : _threatTarget, up, down);
            Glide(Layer.Combat, _combatOn && !_bossOn ? 1f : 0f, up * 4f, down);
            Glide(Layer.Chest, _chestOn && !_bossOn ? 1f : 0f, up, down);
            Glide(Layer.Boss, _bossOn ? 1f : 0f, up * 2f, down);
            _master = MoveTowards(_master, _masterTarget, _masterSpeed * dt);

            float l = 0f, r = 0f, send = 0f;
            var threat = _gain[(int)Layer.Threat];
            var combat = _gain[(int)Layer.Combat];
            for (var i = 0; i < MaxVoices; i++)
            {
                if (!_voices[i].Alive)
                    continue;
                ref var v = ref _voices[i];
                var g = _gain[(int)v.Layer] * LayerLevel[(int)v.Layer];
                // La mélodie s'efface devant la menace et le combat ; la basse d'exploration devant celle du combat.
                if (v.Kind == Kind.Flute)
                    g *= (1f - 0.4f * threat) * (1f - combat) * (1f - bossMix);
                else if (v.Kind == Kind.Pluck && v.Layer == Layer.Base && v.Freq < 120f)
                    g *= 1f - 0.8f * Math.Max(combat, bossMix);
                var s = RenderVoice(ref v, dt) * g;
                l += s * (1f - Math.Max(0f, v.Pan));
                r += s * (1f + Math.Min(0f, v.Pan));
                send += s * v.Send;
            }

            // Menace continue : trémolo de deux notes qui frottent (ré 4 et mi bémol 4, dans le
            // médium pour qu'on l'entende partout), sur un ré 3.
            var drone = threat * LayerLevel[(int)Layer.Threat];
            if (drone > 0.001f)
            {
                _droneP1 += 293.66 * dt;
                _droneP2 += 311.13 * dt;
                _droneP3 += 146.83 * dt;
                _tremP += (7.0 + 3.0 * bossMix) * dt;
                var trem = 0.55f + 0.45f * (float)Math.Sin(Tau * (_tremP % 1.0));
                var a = Tau * (float)(_droneP1 % 1.0);
                var b = Tau * (float)(_droneP2 % 1.0);
                var c = Tau * (float)(_droneP3 % 1.0);
                var s = ((float)(Math.Sin(a) + 0.4 * Math.Sin(2 * a) + 0.2 * Math.Sin(3 * a))
                        + 0.8f * (float)(Math.Sin(b) + 0.4 * Math.Sin(2 * b) + 0.2 * Math.Sin(3 * b)))
                    * trem * 0.045f * drone
                    + (float)(Math.Sin(c) + 0.3 * Math.Sin(2 * c)) * 0.04f * drone;
                l += s;
                r += s;
                send += s * 0.3f;
            }

            var wetL = _reverbL.Process(send);
            var wetR = _reverbR.Process(send);
            var master = Master * _master;
            left = SoftClip((l + wetL * 0.5f) * master);
            right = SoftClip((r + wetR * 0.5f) * master);
        }

        void Glide(Layer layer, float target, float up, float down)
        {
            var i = (int)layer;
            _gain[i] = target > _gain[i] ? Math.Min(target, _gain[i] + up) : Math.Max(target, _gain[i] - down);
        }

        static float MoveTowards(float value, float target, float delta) =>
            value < target ? Math.Min(target, value + delta) : Math.Max(target, value - delta);

        static float SoftClip(float x) => (float)Math.Tanh(x);

        /// <summary>Réverbération (Freeverb simplifié) : 4 filtres en peigne en parallèle, 2 passe-tout.</summary>
        sealed class Reverb
        {
            readonly float[][] _combs;
            readonly int[] _combPos;
            readonly float[] _combLow;
            readonly float[][] _allpass;
            readonly int[] _allPos;

            public Reverb(float rate, int spread)
            {
                var scale = rate / 44100f;
                int[] combs = { 1116, 1188, 1277, 1356 };
                int[] alls = { 556, 441 };
                _combs = new float[combs.Length][];
                _combPos = new int[combs.Length];
                _combLow = new float[combs.Length];
                for (var i = 0; i < combs.Length; i++)
                    _combs[i] = new float[(int)((combs[i] + spread) * scale)];
                _allpass = new float[alls.Length][];
                _allPos = new int[alls.Length];
                for (var i = 0; i < alls.Length; i++)
                    _allpass[i] = new float[(int)((alls[i] + spread) * scale)];
            }

            public float Process(float input)
            {
                var output = 0f;
                for (var i = 0; i < _combs.Length; i++)
                {
                    var buffer = _combs[i];
                    var y = buffer[_combPos[i]];
                    _combLow[i] = y * 0.75f + _combLow[i] * 0.25f; // pierre : les aigus s'amortissent
                    buffer[_combPos[i]] = input * 0.15f + _combLow[i] * 0.8f;
                    _combPos[i] = (_combPos[i] + 1) % buffer.Length;
                    output += y;
                }
                for (var i = 0; i < _allpass.Length; i++)
                {
                    var buffer = _allpass[i];
                    var y = buffer[_allPos[i]];
                    buffer[_allPos[i]] = output + y * 0.5f;
                    _allPos[i] = (_allPos[i] + 1) % buffer.Length;
                    output = y - output;
                }
                return output;
            }
        }
    }
}
