using System;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// L'or de l'expédition en cours (GDD §4) : on en trouve dans les coffres, les vases et caisses,
    /// et sur les monstres. Il n'est à l'abri qu'une fois ramené au hub (<see cref="Progression"/>) :
    /// à la mort, on n'en garde qu'une part (<see cref="KeptShare"/>, 10 %), en remontant on garde
    /// tout. Remis à zéro à chaque nouvelle partie (<see cref="Dungeon.NewGame"/>). Voir docs/or.md.
    /// </summary>
    public static class Gold
    {
        /// <summary>Part de l'or gardée à la mort (ou en abandonnant l'expédition). Réglée par le joueur (inspecteur).</summary>
        public static float KeptShare { get; set; } = 0.1f;

        /// <summary>L'or porté pendant l'expédition, perdu à la mort sauf <see cref="KeptShare"/>.</summary>
        public static int Carried { get; private set; }

        /// <summary>De l'or vient d'être ramassé (montant) : le HUD l'affiche à côté du compteur.</summary>
        public static event Action<int> Gained;

        /// <summary>À la dernière mort : l'or qu'on portait, et la part gardée (écran de mort).</summary>
        public static int LastLost { get; private set; }
        public static int LastKept { get; private set; }

        public static void ResetRun()
        {
            Carried = 0;
            LastLost = 0;
            LastKept = 0;
        }

        public static void Add(int amount)
        {
            if (amount <= 0)
                return;
            Carried += amount;
            Gained?.Invoke(amount);
        }

        /// <summary>Or gardé sur `carried` quand l'expédition finit mal : arrondi à l'unité inférieure.</summary>
        public static int KeptOnDeath(int carried, float share) => Mathf.FloorToInt(Mathf.Max(0, carried) * Mathf.Clamp01(share) + 1e-4f);

        /// <summary>
        /// L'expédition finit mal (mort, abandon) : la part gardée va au hub, le reste est perdu.
        /// Renvoie l'or gardé. Sans effet si l'or a déjà été réglé.
        /// </summary>
        public static int EndOnDeath()
        {
            var kept = KeptOnDeath(Carried, KeptShare);
            LastLost = Carried;
            LastKept = kept;
            Carried = 0;
            Progression.AddGold(kept);
            return kept;
        }

        /// <summary>Remontée réussie : tout l'or porté va au hub. Renvoie le montant.</summary>
        public static int EndOnReturn()
        {
            var all = Carried;
            Carried = 0;
            Progression.AddGold(all);
            return all;
        }
    }
}
