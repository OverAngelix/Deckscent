using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>Un bienfait proposé à la montée de niveau (pour le moment, des stats : GDD §3).</summary>
    public enum LevelBonus { Attack, Defense, Health }

    /// <summary>Réglages de l'XP et des niveaux (inspecteur du joueur). Voir docs/niveaux.md.</summary>
    [Serializable]
    public class ExperienceSettings
    {
        [Tooltip("XP pour passer du niveau 1 au niveau 2.")]
        [Min(1)] public int firstLevel = 10;
        [Tooltip("XP en plus pour chaque niveau suivant (niveau 2 → 3 : firstLevel + perLevel, etc.).")]
        [Min(0)] public int perLevel = 5;
        [Tooltip("Bienfaits proposés à chaque niveau (tirés au hasard parmi tous).")]
        [Range(1, 3)] public int choices = 3;
        [Tooltip("ATQ gagnée avec le bienfait Attaque.")]
        [Range(1, 5)] public int attackGain = 1;
        [Tooltip("DÉF gagnée avec le bienfait Défense.")]
        [Range(1, 5)] public int defenseGain = 1;
        [Tooltip("PV max gagnés avec le bienfait Vie (autant de PV rendus : les nouvelles cases arrivent pleines).")]
        [Range(1, 10)] public int healthGain = 2;
    }

    /// <summary>
    /// XP et niveaux de l'expédition (GDD §3) : on gagne de l'XP en tuant des monstres (les plus
    /// durs en donnent plus), et chaque niveau gagné attend qu'on choisisse un bienfait
    /// (<see cref="Pending"/>). Tout est perdu à la fin de la partie : seule la progression du hub
    /// sera durable. Logique pure, sans MonoBehaviour (testée en EditMode).
    /// </summary>
    public class Experience
    {
        /// <summary>Tous les bienfaits possibles ; on en propose <see cref="ExperienceSettings.choices"/>.</summary>
        public static readonly LevelBonus[] Pool = { LevelBonus.Attack, LevelBonus.Defense, LevelBonus.Health };

        readonly ExperienceSettings _settings;

        public Experience(ExperienceSettings settings) => _settings = settings ?? new ExperienceSettings();

        public ExperienceSettings Settings => _settings;
        /// <summary>Niveau atteint (1 au départ), choix en attente compris.</summary>
        public int Level { get; private set; } = 1;
        /// <summary>XP gagnée depuis le dernier niveau.</summary>
        public int Xp { get; private set; }
        /// <summary>Niveaux gagnés dont le bienfait n'est pas encore choisi.</summary>
        public int Pending { get; private set; }
        /// <summary>XP du niveau en cours au suivant.</summary>
        public int ToNext => NeededFor(Level);

        /// <summary>XP pour passer du niveau `level` au suivant.</summary>
        public int NeededFor(int level) => _settings.firstLevel + _settings.perLevel * Mathf.Max(0, level - 1);

        /// <summary>
        /// XP rapportée par un monstre : ses PV max plus son attaque (après le renfort de l'étage).
        /// Un monstre dur ou profond en rapporte donc plus.
        /// </summary>
        public static int RewardFor(int maxHealth, int damage) => Mathf.Max(1, maxHealth + damage);

        /// <summary>Ajoute de l'XP ; rend le nombre de niveaux gagnés (ils s'ajoutent à <see cref="Pending"/>).</summary>
        public int Gain(int amount)
        {
            if (amount <= 0)
                return 0;
            Xp += amount;
            var gained = 0;
            while (Xp >= ToNext)
            {
                Xp -= ToNext;
                Level++;
                gained++;
            }
            Pending += gained;
            return gained;
        }

        /// <summary>Un bienfait choisi : un niveau en attente de moins.</summary>
        public void Consume()
        {
            if (Pending > 0)
                Pending--;
        }

        /// <summary>Les bienfaits proposés : <see cref="ExperienceSettings.choices"/> tirés sans doublon, dans l'ordre du tirage.</summary>
        public List<LevelBonus> Offer(System.Random rng)
        {
            var pool = new List<LevelBonus>(Pool);
            var count = Mathf.Clamp(_settings.choices, 1, pool.Count);
            var offer = new List<LevelBonus>(count);
            while (offer.Count < count)
            {
                var i = rng.Next(pool.Count);
                offer.Add(pool[i]);
                pool.RemoveAt(i);
            }
            // Toujours le même ordre à l'écran (ATQ, DÉF, Vie) : plus simple à lire d'un niveau à l'autre.
            offer.Sort();
            return offer;
        }

        /// <summary>Gain affiché et appliqué pour un bienfait.</summary>
        public int Amount(LevelBonus bonus) => bonus switch
        {
            LevelBonus.Attack => _settings.attackGain,
            LevelBonus.Defense => _settings.defenseGain,
            _ => _settings.healthGain,
        };
    }
}
