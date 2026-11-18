using System;
using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// L'escalier vers l'étage suivant, ouvert par la mort du boss (« le boss est l'escalier »,
    /// GDD §2). Il sort du sol à la place du loup-garou ; on le prend en avançant dessus ou avec
    /// Interact : l'écran se voile, le Donjon passe à l'étage suivant (<see cref="Dungeon.NextFloor"/>)
    /// et le joueur repart du départ, avec ses PV.
    /// </summary>
    public class Stairs : GridEntity, IInteractable
    {
        [Tooltip("Hauteur dont il sort du sol à son apparition (mètres).")]
        [SerializeField, Min(0f)] float riseDepth = 1.2f;
        [Tooltip("Durée de la sortie du sol (secondes).")]
        [SerializeField, Min(0.01f)] float riseTime = 1.2f;
        [Tooltip("Ce qui joue le raclement de pierre. Vide : cherché sur l'escalier.")]
        [SerializeField] SoundEmitter sounds;

        /// <summary>Le joueur prend l'escalier (avant le changement d'étage).</summary>
        public static event Action<Stairs> Taken;

        public Dungeon Dungeon { get; set; }
        public bool Used { get; private set; }
        public override bool BlocksPlayer => !Used;
        public bool CanInteract => !Used && IsActive;

        void Awake()
        {
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
        }

        public override void Place(Maze maze, Vector2Int cell)
        {
            base.Place(maze, cell);
            // Il fait face à un passage : on le voit de face en arrivant.
            for (var i = 0; i < Grid.Dirs.Length; i++)
                if (maze.CanStep(cell, i))
                {
                    transform.rotation = Quaternion.Euler(0f, Grid.Yaw(i) + 180f, 0f);
                    break;
                }
            if (isActiveAndEnabled)
                StartCoroutine(Rise());
        }

        IEnumerator Rise()
        {
            sounds?.Play(SoundBank.Stairs, 1f);
            var end = transform.position;
            var start = end + Vector3.down * riseDepth;
            transform.position = start;
            yield return Animate(riseTime, t => transform.position = Vector3.Lerp(start, end, Smooth(t)));
        }

        public IEnumerator Interact(PlayerController player)
        {
            if (!CanInteract)
                yield break;
            Used = true;
            Retire();
            Taken?.Invoke(this);
            var dungeon = Dungeon != null ? Dungeon : FindAnyObjectByType<Dungeon>();
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null)
                yield return hud.FadeOut(0.6f);
            if (dungeon != null)
            {
                dungeon.NextFloor();
                player.PlaceAtStart();
                player.Notify(Loc.F("message.etage", dungeon.Floor));
            }
            if (hud != null)
                yield return hud.FadeIn(0.8f);
        }
    }
}
