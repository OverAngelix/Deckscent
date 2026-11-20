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

        /// <summary>Monstres tués et coffres ouverts au début du sous-sol en cours (bilan de fin d'étage).</summary>
        public static int FloorStartKills { get; private set; }
        public static int FloorStartChests { get; private set; }
        public static int FloorStartTurn { get; private set; }

        public static void Reset()
        {
            Kills = 0;
            Chests = 0;
            Bosses = 0;
            DamageDealt = 0;
            BeginFloor(0);
        }

        /// <summary>Un nouveau sous-sol commence (au tour `turn`) : le bilan de l'étage repart de là.</summary>
        public static void BeginFloor(int turn)
        {
            FloorStartKills = Kills;
            FloorStartChests = Chests;
            FloorStartTurn = turn;
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
