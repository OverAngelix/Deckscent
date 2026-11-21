using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Entrée du donjon, au fond du camp : la bouche de la grotte. Avancer dedans (ou Interact)
    /// demande confirmation (<see cref="ConfirmScreen"/>, demande de l'utilisateur), puis lance
    /// l'expédition (scène de jeu). Voir docs/camp.md.
    /// </summary>
    public class DungeonEntrance : CampObstacle, IInteractable
    {
        [Tooltip("Ce qui joue le raclement de pierre en entrant. Vide : cherché sur l'objet.")]
        [SerializeField] SoundEmitter sounds;

        public bool Entered { get; private set; }
        public bool CanInteract => !Entered && IsActive;

        /// <summary>Faux : n'ouvre pas la scène de jeu (tests).</summary>
        public static bool LoadsGame { get; set; } = true;

        public IEnumerator Interact(PlayerController player)
        {
            if (!CanInteract)
                yield break;
            var confirm = FindAnyObjectByType<ConfirmScreen>();
            if (confirm != null)
            {
                confirm.Open("confirmation.donjon_titre", "confirmation.donjon_texte", "confirmation.descendre", "confirmation.rester");
                while (confirm.IsOpen)
                    yield return null;
                if (!confirm.Confirmed)
                    yield break; // on reste au camp
            }
            Entered = true;
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
            sounds?.Play(SoundBank.Stairs, 1f);
            if (LoadsGame)
                SceneFlow.LoadGame();
        }
    }
}
