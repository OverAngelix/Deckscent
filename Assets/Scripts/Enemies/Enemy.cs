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

        public int Health { get; protected set; }
        public int MaxHealth => maxHealth;
        public int Damage => damage;
        public int Defense => defense;
        public override bool BlocksPlayer => true;

        protected virtual void Awake() => Health = maxHealth;

        public void TakeDamage(int amount)
        {
            if (!IsActive || amount <= 0)
                return;
            Health -= Combat.DamageAfterDefense(amount, defense);
            if (Health <= 0)
            {
                Health = 0;
                Retire();
                Died?.Invoke(this);
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
