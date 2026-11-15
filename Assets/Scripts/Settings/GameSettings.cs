using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Deckscent
{
    /// <summary>
    /// Options du jeu : trois volumes (général, musique, effets) et la langue, sauvegardés
    /// (PlayerPrefs) et appliqués dès le lancement. Les volumes règlent les pistes du mixer
    /// Assets/Resources/Mixage.mixer (VolumeGeneral, VolumeMusique, VolumeEffets), en décibels.
    /// La langue est lue par <see cref="Loc"/> à chaque affichage.
    /// </summary>
    public static class GameSettings
    {
        public static readonly string[] Locales = { "fr", "en" };
        const string VolumeKey = "Deckscent.Volume", LocaleKey = "Deckscent.Locale",
            MusicVolumeKey = "Deckscent.MusicVolume", EffectsVolumeKey = "Deckscent.EffectsVolume";
        const float DefaultVolume = 0.8f;

        static AudioMixer _mixer;

        /// <summary>Volume général, de 0 (muet) à 1.</summary>
        public static float Volume { get; private set; } = DefaultVolume;
        /// <summary>Volume de la musique, de 0 (muet) à 1. Le volume général s'applique aussi.</summary>
        public static float MusicVolume { get; private set; } = DefaultVolume;
        /// <summary>Volume des effets sonores, de 0 (muet) à 1. Le volume général s'applique aussi.</summary>
        public static float EffectsVolume { get; private set; } = DefaultVolume;

        /// <summary>Langue en cours : "fr" ou "en".</summary>
        public static string Locale { get; private set; } = "fr";

        /// <summary>Un réglage a changé (pour redessiner les écrans).</summary>
        public static event Action Changed;

        /// <summary>Au lancement, avant la première scène : relit les options (la langue sert dès le premier affichage).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => Load();

        /// <summary>
        /// Réapplique les volumes une image après le chargement de la première scène : avant, Unity
        /// n'a pas encore démarré le mixer et ignore les SetFloat (mesuré : il reste sur son instantané).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void ApplyAfterLoad()
        {
            await Awaitable.NextFrameAsync();
            Apply();
        }

        /// <summary>Relit les options sauvegardées et les applique.</summary>
        public static void Load()
        {
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));
            EffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultVolume));
            var locale = PlayerPrefs.GetString(LocaleKey, "fr");
            Locale = Array.IndexOf(Locales, locale) >= 0 ? locale : "fr";
            Apply();
        }

        /// <summary>Volume de 0 à 1, arrondi au vingtième. `save` faux : seulement pour cette session (tests).</summary>
        public static void SetVolume(float value, bool save = true)
        {
            Volume = RoundVolume(value);
            Apply();
            if (save)
                Save();
        }

        /// <summary>Volume de la musique de 0 à 1, arrondi au vingtième. `save` faux : seulement pour cette session (tests).</summary>
        public static void SetMusicVolume(float value, bool save = true)
        {
            MusicVolume = RoundVolume(value);
            Apply();
            if (save)
                Save();
        }

        /// <summary>Volume des effets de 0 à 1, arrondi au vingtième. `save` faux : seulement pour cette session (tests).</summary>
        public static void SetEffectsVolume(float value, bool save = true)
        {
            EffectsVolume = RoundVolume(value);
            Apply();
            if (save)
                Save();
        }

        /// <summary>Change de langue ("fr" ou "en"). `save` faux : seulement pour cette session (tests).</summary>
        public static void SetLocale(string locale, bool save = true)
        {
            if (Array.IndexOf(Locales, locale) < 0)
                return;
            Locale = locale;
            Apply();
            if (save)
                Save();
        }

        /// <summary>Langue suivante (ou précédente si `step` &lt; 0), en boucle.</summary>
        public static void NextLocale(int step = 1)
        {
            var i = Array.IndexOf(Locales, Locale);
            SetLocale(Locales[((i + step) % Locales.Length + Locales.Length) % Locales.Length]);
        }

        /// <summary>Volume (0 à 1) → décibels du mixer (0 dB = plein volume, -80 dB = silence).</summary>
        static float ToDecibels(float volume) => volume <= 0.0001f ? -80f : 20f * Mathf.Log10(volume);
        /// <summary>Volume borné entre 0 et 1, arrondi au vingtième.</summary>
        static float RoundVolume(float volume) => Mathf.Clamp01(Mathf.Round(volume * 20f) / 20f);

        static void Apply()
        {
            if (_mixer == null)
                _mixer = Resources.Load<AudioMixer>("Mixage");
            if (_mixer != null)
            {
                _mixer.SetFloat("VolumeGeneral", ToDecibels(Volume));
                _mixer.SetFloat("VolumeMusique", ToDecibels(MusicVolume));
                _mixer.SetFloat("VolumeEffets", ToDecibels(EffectsVolume));
            }
            Changed?.Invoke();
        }

        static void Save()
        {
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.SetString(LocaleKey, Locale);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(EffectsVolumeKey, EffectsVolume);
            PlayerPrefs.Save();
        }
    }
}
