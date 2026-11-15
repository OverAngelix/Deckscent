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
