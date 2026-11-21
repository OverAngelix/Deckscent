using System;
using System.IO;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Progression durable, d'une expédition à l'autre (le hub, GDD §7) : pour le moment, l'or mis à
    /// l'abri. Enregistrée en JSON dans <see cref="Application.persistentDataPath"/>
    /// (progression.json), lue au premier accès. Voir docs/or.md.
    /// </summary>
    public static class Progression
    {
        [Serializable]
        class Data
        {
            public int gold;
        }

        static Data _data;
        static string _path;

        /// <summary>Fichier de la sauvegarde. Les tests le détournent vers un fichier temporaire.</summary>
        public static string FilePath
        {
            get => _path ??= Path.Combine(Application.persistentDataPath, "progression.json");
            set
            {
                _path = value;
                _data = null;
            }
        }

        /// <summary>L'or mis à l'abri (au hub) : ramené d'une remontée, ou la part gardée à la mort.</summary>
        public static int Gold => Current.gold;

        /// <summary>La progression a changé (or mis à l'abri…).</summary>
        public static event Action Changed;

        static Data Current => _data ??= Load();

        public static void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            Current.gold += amount;
            Save();
        }

        /// <summary>Retire de l'or (achat au hub). Faux s'il n'y en a pas assez : rien n'est retiré.</summary>
        public static bool SpendGold(int amount)
        {
            if (amount < 0 || Current.gold < amount)
                return false;
            Current.gold -= amount;
            Save();
            return true;
        }

        /// <summary>Efface toute la progression (nouvelle sauvegarde).</summary>
        public static void Reset()
        {
            _data = new Data();
            Save();
        }

        static Data Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) ?? new Data();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Progression illisible ({FilePath}) : on repart de zéro. {e.Message}");
            }
            return new Data();
        }

        static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Progression non enregistrée ({FilePath}) : {e.Message}");
            }
            Changed?.Invoke();
        }
    }
}
