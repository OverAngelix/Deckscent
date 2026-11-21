using System;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Un ennemi : occupe sa case (le joueur ne la traverse pas, il l'attaque en avançant vers lui),
    /// a des PV, une attaque et une défense. À sa mort, il quitte tout de suite la liste des objets
    /// de grille (il ne joue plus, ne bloque plus) puis joue son animation de sortie.
    /// </summary>
    public abstract class Enemy : GridEntity, IDamageable
    {
        [Header("Combat")]
        [Tooltip("Points de vie au départ.")]
        [SerializeField, Range(1, 99)] protected int maxHealth = 3;
        [Tooltip("Dégâts infligés au joueur par attaque, avant sa défense.")]
        [SerializeField, Range(0, 20)] protected int damage = 1;
        [Tooltip("Retirée des dégâts reçus. Un coup fait toujours au moins 1.")]
        [SerializeField, Range(0, 20)] protected int defense;

        /// <summary>L'ennemi vient de mourir (avant son animation de sortie).</summary>
        public event Action<Enemy> Died;
        /// <summary>Un ennemi, n'importe lequel, vient de perdre des PV (le HUD affiche les dégâts au-dessus de lui).</summary>
        public static event Action<Enemy, int> Damaged;
        /// <summary>Un ennemi, n'importe lequel, vient de mourir (bilan de la partie).</summary>
        public static event Action<Enemy> Killed;

        public int Health { get; protected set; }
        public int MaxHealth => maxHealth;
        public int Damage => damage;
        public int Defense => defense;
        /// <summary>XP rapportée au joueur à sa mort : PV max + attaque (les monstres durs en donnent plus).</summary>
        public virtual int ExperienceReward => Experience.RewardFor(maxHealth, damage);
        /// <summary>Or laissé à sa mort : la moitié de son XP (au moins 1), donc plus chez les monstres durs ou profonds.</summary>
        public virtual int GoldReward => Mathf.Max(1, ExperienceReward / 2);
        public override bool BlocksPlayer => true;
        /// <summary>Menace le joueur (combat, GDD §2) : faux pour un monstre endormi ou caché.</summary>
        public virtual bool Threatens => IsActive;

        protected virtual void Awake() => Health = maxHealth;

        /// <summary>
        /// Renforce l'ennemi pour un étage plus profond (GDD : la difficulté monte avec la
        /// profondeur) : +1 PV par étage sous le premier, +1 d'attaque tous les 3 étages.
        /// À appeler à l'apparition, avant qu'il ne joue.
        /// </summary>
        public virtual void ScaleForFloor(int floor)
        {
            var extra = Mathf.Max(0, floor - 1);
            maxHealth += extra;
            damage += extra / 3;
            Health = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (!IsActive || amount <= 0)
                return;
            // Pas plus que ce qu'il lui reste (bilan, chiffres de dégâts).
            var dealt = Mathf.Min(Combat.DamageAfterDefense(amount, defense), Health);
            Health -= dealt;
            Damaged?.Invoke(this, dealt);
            if (Health <= 0)
            {
                Health = 0;
                Retire();
                Died?.Invoke(this);
                Killed?.Invoke(this);
                OnDeath();
            }
            else
                OnHurt();
        }

        /// <summary>Touché sans mourir (un flash, un recul…).</summary>
        protected virtual void OnHurt() { }

        /// <summary>Mort : l'animation de sortie, puis l'objet se détruit.</summary>
        protected virtual void OnDeath() => Destroy(gameObject);
    }
}
