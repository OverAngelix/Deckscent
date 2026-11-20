using System.Collections;

namespace Deckscent
{
    /// <summary>Règles de combat partagées par le joueur, les ennemis et les pièges.</summary>
    public static class Combat
    {
        /// <summary>
        /// Dégâts réellement subis pour un coup de `amount` contre une défense `defense` : la défense
        /// est retirée, mais un coup qui porte fait toujours au moins 1.
        /// </summary>
        public static int DamageAfterDefense(int amount, int defense) =>
            amount <= 0 ? 0 : System.Math.Max(1, amount - defense);

        /// <summary>Distance (en pas) à laquelle un monstre éveillé met le joueur « en combat » (GDD §2).</summary>
        public const int CombatRange = 3;

        /// <summary>
        /// Le joueur est-il en combat (GDD §2 : on ne pose plus de tuile) ? Oui si un monstre éveillé
        /// peut l'atteindre en <see cref="CombatRange"/> pas au plus, murs contournés : un monstre à
        /// deux cases derrière un mur ne compte pas. Le boss endormi et la mimique qui fait le coffre
        /// ne comptent pas.
        /// </summary>
        public static bool InCombat(PlayerController player)
        {
            if (player == null || player.Maze == null)
                return false;
            foreach (var enemy in GridEntity.AllOf<Enemy>())
            {
                if (!enemy.Threatens)
                    continue;
                if (enemy.Cell == player.Cell || GridPath.Find(player.Maze, enemy.Cell, player.Cell, CombatRange).Count > 0)
                    return true;
            }
            return false;
        }
    }

    /// <summary>Ce qu'on peut frapper (ennemis).</summary>
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }

    /// <summary>Ce qu'on ouvre ou actionne en avançant dessus, ou avec l'action Interact (coffres).</summary>
    public interface IInteractable
    {
        /// <summary>Peut encore servir (un coffre ouvert ne sert plus).</summary>
        bool CanInteract { get; }
        IEnumerator Interact(PlayerController player);
    }
}
