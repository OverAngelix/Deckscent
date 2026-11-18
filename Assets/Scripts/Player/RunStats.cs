using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Bilan de la partie (affiché sur l'écran de mort) : monstres tués, coffres ouverts, boss
    /// vaincus, dégâts infligés. Compté sur toute la partie, tous étages confondus ; remis à zéro à
    /// chaque nouvelle partie (<see cref="Dungeon.NewGame"/>). Nourri par les événements statiques
    /// <see cref="Enemy.Damaged"/>, <see cref="Enemy.Killed"/> et <see cref="Chest.AnyOpened"/>.
    /// </summary>
    public static class RunStats
    {
        public static int Kills { get; private set; }
        public static int Chests { get; private set; }
        public static int Bosses { get; private set; }
        public static int DamageDealt { get; private set; }

        public static void Reset()
        {
            Kills = 0;
            Chests = 0;
            Bosses = 0;
            DamageDealt = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            Reset();
            Enemy.Damaged -= OnDamaged;
            Enemy.Damaged += OnDamaged;
            Enemy.Killed -= OnKilled;
            Enemy.Killed += OnKilled;
            Chest.AnyOpened -= OnOpened;
            Chest.AnyOpened += OnOpened;
        }

        static void OnDamaged(Enemy enemy, int amount) => DamageDealt += amount;

        static void OnKilled(Enemy enemy)
        {
            Kills++;
            if (enemy is Werewolf)
                Bosses++;
        }

        static void OnOpened(Chest chest) => Chests++;
    }
}
