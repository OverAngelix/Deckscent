using System;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Options du jeu : volume général et langue, sauvegardés (PlayerPrefs) et appliqués dès le
    /// lancement, avant la première scène. Le volume agit sur <see cref="AudioListener.volume"/> :
    /// la musique du menu le suit, les futurs sons du jeu aussi. La langue est lue par
    /// <see cref="Loc"/> à chaque affichage.
    /// </summary>
    public static class GameSettings
    {
        public static readonly string[] Locales = { "fr", "en" };
        const string VolumeKey = "Deckscent.Volume", LocaleKey = "Deckscent.Locale";
        const float DefaultVolume = 0.8f;

        /// <summary>Volume général, de 0 (muet) à 1.</summary>
        public static float Volume { get; private set; } = DefaultVolume;
        /// <summary>Langue en cours : "fr" ou "en".</summary>
        public static string Locale { get; private set; } = "fr";

        /// <summary>Un réglage a changé (pour redessiner les écrans).</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => Load();

        /// <summary>Relit les options sauvegardées et les applique.</summary>
        public static void Load()
        {
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
            var locale = PlayerPrefs.GetString(LocaleKey, "fr");
            Locale = Array.IndexOf(Locales, locale) >= 0 ? locale : "fr";
            Apply();
        }

        /// <summary>Volume de 0 à 1, arrondi au vingtième. `save` faux : seulement pour cette session (tests).</summary>
        public static void SetVolume(float value, bool save = true)
        {
            Volume = Mathf.Clamp01(Mathf.Round(value * 20f) / 20f);
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

        static void Apply()
        {
            AudioListener.volume = Volume;
            Changed?.Invoke();
        }

        static void Save()
        {
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.SetString(LocaleKey, Locale);
            PlayerPrefs.Save();
        }
    }
}
