using System;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Effets sonores synthétisés (aucun fichier audio), fabriqués une fois à la première demande
    /// et gardés : bruits de pas, attaque du joueur, attaque du slime, pics, coup reçu. Joués par
    /// <see cref="SoundEmitter"/>, sur la piste Effets du mixer.
    ///
    /// Comme la musique du menu, c'est provisoire : pour un vrai son, remplacer le clip renvoyé ici
    /// par un AudioClip importé.
    /// </summary>
    public static class SoundBank
    {
        const int Rate = 44100;
        const float Tau = Mathf.PI * 2f;

        static AudioClip[] _steps;
        static AudioClip _attack, _slurp, _spikes, _hit;

        /// <summary>Quatre variantes de pas sur la pierre (tirer au hasard pour ne pas répéter le même son).</summary>
        public static AudioClip[] Steps => _steps ??= new[] { Step(1), Step(2), Step(3), Step(4) };

        /// <summary>Un pas au hasard.</summary>
        public static AudioClip RandomStep() => Steps[UnityEngine.Random.Range(0, Steps.Length)];

        /// <summary>L'attaque du joueur : un « whoosh » d'arme qui fend l'air.</summary>
        public static AudioClip Attack => _attack ??= MakeAttack();

        /// <summary>L'attaque du slime : un « slurp » mouillé.</summary>
        public static AudioClip Slurp => _slurp ??= MakeSlurp();

        /// <summary>Les pics d'une dalle qui sortent : raclement et tintement métalliques.</summary>
        public static AudioClip Spikes => _spikes ??= MakeSpikes();

        /// <summary>Le joueur prend un coup (monstre ou piège) : un impact sourd.</summary>
        public static AudioClip Hit => _hit ??= MakeHit();

        // --- Synthèse ---------------------------------------------------------------------

        /// <summary>
        /// Pas : deux contacts étouffés, le talon puis la pointe (plus faible), faits de bruit filtré
        /// très grave (une semelle sur la pierre) et d'un soupçon de gravier. Pas de note pure (un
        /// sinus sonnait comme une boîte à rythmes) ; chaque contact monte en quelques millisecondes
        /// pour ne pas claquer.
        /// </summary>
        static AudioClip Step(int seed)
        {
            var rng = new System.Random(seed);
            var data = new float[(int)(0.2f * Rate)];
            var toeAt = 0.035f + (float)rng.NextDouble() * 0.02f;       // écart talon → pointe
            var toeLevel = 0.4f + (float)rng.NextDouble() * 0.2f;       // la pointe, plus faible
            var thud = OnePole(150f + (float)rng.NextDouble() * 80f);   // grave de la semelle
            var gritLevel = 0.06f + (float)rng.NextDouble() * 0.04f;
            float low = 0f, low2 = 0f, gLow = 0f, gBand = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = (float)i / Rate;
                var env = Contact(t, 0.045f) + toeLevel * Contact(t - toeAt, 0.03f);
                var noise = (float)rng.NextDouble() * 2f - 1f;
                low += (noise - low) * thud;
                low2 += (low - low2) * thud;
                Filter(noise, 1800f, 0.8f, ref gLow, ref gBand);
                data[i] = (low2 * 6f + gBand * gritLevel) * env;
            }
            return Make("Pas " + seed, data);
        }

        /// <summary>Enveloppe d'un contact : montée douce de 5 ms, puis extinction (constante `decay`, s).</summary>
        static float Contact(float t, float decay)
        {
            if (t < 0f)
                return 0f;
            var rise = Mathf.Min(1f, t / 0.005f);
            return rise * rise * Mathf.Exp(-t / decay);
        }

        /// <summary>Coefficient d'un filtre passe-bas à un pôle de fréquence de coupure `cutoff` (Hz).</summary>
        static float OnePole(float cutoff) => 1f - Mathf.Exp(-Tau * cutoff / Rate);

        /// <summary>Attaque : bruit dans un filtre résonant qui balaie de l'aigu au grave, enveloppe en cloche.</summary>
        static AudioClip MakeAttack()
        {
            var rng = new System.Random(11);
            var length = 0.3f;
            var data = new float[(int)(length * Rate)];
            float low = 0f, band = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = (float)i / Rate;
                var k = t / length;
                var cutoff = Mathf.Lerp(3200f, 500f, Mathf.Sqrt(k));
                var noise = (float)rng.NextDouble() * 2f - 1f;
                Filter(noise, cutoff, 0.5f, ref low, ref band);
                // Monte vite, retombe lentement.
                var env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f)), 2f);
                data[i] = band * env;
            }
            return Make("Attaque", data);
        }

        /// <summary>
        /// Slurp : une note qui glisse vers l'aigu en gargouillant (vibrato rapide, aspiration),
        /// des bulles, et un bruit mouillé.
        /// </summary>
        static AudioClip MakeSlurp()
        {
            var rng = new System.Random(23);
            var length = 0.45f;
            var data = new float[(int)(length * Rate)];
            float phase = 0f, low = 0f, band = 0f;
            var bubbleAt = 0.05f;
            var bubbleFreq = 0f;
            var bubbleStart = -1f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = (float)i / Rate;
                var k = t / length;
                // Glissé 180 → 520 Hz, avec un gargouillis à 28 Hz.
                var f = Mathf.Lerp(180f, 520f, k * k) + Mathf.Sin(Tau * 28f * t) * 60f;
                phase += f / Rate;
                var env = Mathf.Min(1f, t / 0.03f) * Mathf.Pow(1f - k, 1.5f);
                var tone = (Mathf.Sin(Tau * phase) + 0.3f * Mathf.Sin(Tau * phase * 2f)) * env;
                // Aspiration mouillée : bruit filtré qui suit la note.
                var noise = (float)rng.NextDouble() * 2f - 1f;
                Filter(noise, f * 2f, 0.3f, ref low, ref band);
                var wet = band * 0.6f * env;
                // Bulles : petits « plop » qui montent, au hasard.
                if (t >= bubbleAt)
                {
                    bubbleStart = t;
                    bubbleFreq = 600f + (float)rng.NextDouble() * 700f;
                    bubbleAt = t + 0.04f + (float)rng.NextDouble() * 0.07f;
                }
                var bubble = 0f;
                if (bubbleStart >= 0f)
                {
                    var bt = t - bubbleStart;
                    bubble = Mathf.Sin(Tau * bubbleFreq * bt * (1f + bt * 30f)) * Mathf.Exp(-bt / 0.012f) * 0.35f;
                }
                data[i] = tone * 0.7f + wet + bubble * (1f - k);
            }
            return Make("Slurp", data);
        }

        /// <summary>
        /// Pics qui sortent : un raclement métallique bref (bruit aigu qui monte) puis un tintement
        /// de métal (partiels inharmoniques qui s'éteignent vite).
        /// </summary>
        static AudioClip MakeSpikes()
        {
            var rng = new System.Random(31);
            var length = 0.4f;
            var data = new float[(int)(length * Rate)];
            float low = 0f, band = 0f;
            var partials = new[] { 1870f, 2730f, 4120f, 5910f };
            var decays = new[] { 0.16f, 0.11f, 0.08f, 0.05f };
            const float ringAt = 0.035f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = (float)i / Rate;
                // Raclement : 50 ms, filtre qui monte de 2,5 à 6 kHz.
                var noise = (float)rng.NextDouble() * 2f - 1f;
                Filter(noise, Mathf.Lerp(2500f, 6000f, Mathf.Clamp01(t / 0.05f)), 0.6f, ref low, ref band);
                var scrape = band * Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t / 0.025f);
                // Tintement : quand les pics arrivent en haut.
                var ring = 0f;
                var rt = t - ringAt;
                if (rt >= 0f)
                    for (var p = 0; p < partials.Length; p++)
                        ring += Mathf.Sin(Tau * partials[p] * rt) * Mathf.Exp(-rt / decays[p]) / (p + 1f);
                data[i] = scrape * 1.2f + ring * 0.5f * Mathf.Min(1f, rt / 0.002f + 1f);
            }
            return Make("Pics", data);
        }

        /// <summary>
        /// Coup reçu : un impact sourd sur le corps (bruit grave qui chute, un peu écrasé) et un
        /// claquement mat très bref.
        /// </summary>
        static AudioClip MakeHit()
        {
            var rng = new System.Random(41);
            var length = 0.25f;
            var data = new float[(int)(length * Rate)];
            float low = 0f, low2 = 0f, mLow = 0f, mBand = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = (float)i / Rate;
                var noise = (float)rng.NextDouble() * 2f - 1f;
                // Le grave se referme : le coup « s'enfonce ».
                var a = OnePole(Mathf.Lerp(420f, 120f, Mathf.Clamp01(t / 0.08f)));
                low += (noise - low) * a;
                low2 += (low - low2) * a;
                var body = low2 * 7f * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / 0.06f);
                Filter(noise, 1100f, 0.7f, ref mLow, ref mBand);
                var slap = mBand * Mathf.Exp(-t / 0.012f);
                // Écrasé : saturation douce.
                data[i] = (float)Math.Tanh((body + slap * 0.8f) * 2.2f);
            }
            return Make("Coup reçu", data);
        }

        /// <summary>Filtre à variable d'état (passe-bande résonant), un échantillon.</summary>
        static void Filter(float input, float cutoff, float damping, ref float low, ref float band)
        {
            var f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(cutoff, Rate / 6f) / Rate);
            low += f * band;
            var high = input - low - damping * band;
            band += f * high;
        }

        /// <summary>Normalise (pic à 0,9) et crée le clip mono.</summary>
        static AudioClip Make(string name, float[] data)
        {
            var peak = 0f;
            foreach (var v in data)
                peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0f)
                for (var i = 0; i < data.Length; i++)
                    data[i] *= 0.9f / peak;
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Pic d'un clip (tests : un son n'est ni muet ni saturé).</summary>
        public static float Peak(AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            var peak = 0f;
            foreach (var v in data)
                peak = Math.Max(peak, Math.Abs(v));
            return peak;
        }
    }
}
