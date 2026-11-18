using System;
using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// L'escalier vers le sous-sol suivant, ouvert par la mort du boss (« le boss est l'escalier »,
    /// GDD §2). À la place du loup-garou, la dalle du sol s'effondre (<see cref="Maze.AddHole"/>) et
    /// découvre un puits où des marches **descendent** ; une lueur monte du fond. On le prend en
    /// avançant dessus ou avec Interact : l'écran se voile, le Donjon passe au sous-sol suivant
    /// (<see cref="Dungeon.NextFloor"/>) et le joueur repart du départ, avec ses PV.
    /// </summary>
    public class Stairs : GridEntity, IInteractable
    {
        [Tooltip("La dalle qui s'effondre dans le puits à l'ouverture (enfant, cachée ensuite).")]
        [SerializeField] Transform slab;
        [Tooltip("Profondeur de la chute de la dalle (mètres).")]
        [SerializeField, Min(0f)] float fallDepth = 4f;
        [Tooltip("Durée de la chute de la dalle (secondes).")]
        [SerializeField, Min(0.01f)] float fallTime = 0.9f;
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
            // Les marches partent d'un passage : on les voit descendre en arrivant.
            for (var i = 0; i < Grid.Dirs.Length; i++)
                if (maze.CanStep(cell, i))
                {
                    transform.rotation = Quaternion.Euler(0f, Grid.Yaw(i) + 180f, 0f);
                    break;
                }
            maze.AddHole(cell);
            if (isActiveAndEnabled)
                StartCoroutine(Collapse());
        }

        /// <summary>La dalle tombe dans le puits en basculant, avec un raclement de pierre.</summary>
        IEnumerator Collapse()
        {
            sounds?.Play(SoundBank.Stairs, 1f);
            if (slab == null)
                yield break;
            slab.gameObject.SetActive(true);
            var start = slab.localPosition;
            var from = slab.localRotation;
            yield return Animate(fallTime, t =>
            {
                slab.localPosition = start + Vector3.down * fallDepth * t * t;
                slab.localRotation = from * Quaternion.Euler(25f * t, 0f, 12f * t);
            });
            slab.gameObject.SetActive(false);
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
