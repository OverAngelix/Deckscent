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
    ///
    /// Au dernier étage d'un sous-biome (<see cref="Dungeon.IsSubBiomeEnd"/>), il propose d'abord de
    /// **remonter** (<see cref="AscentScreen"/>) : fin de l'expédition, tout l'or porté va au camp
    /// (docs/remontee.md). Ni l'un ni l'autre : l'escalier reste ouvert.
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
        /// <summary>Le joueur a choisi de remonter : l'expédition est finie.</summary>
        public static event Action<Stairs> Ascended;

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
            Fx.Play(Fx.Get(l => l.bigDust), transform.position + Vector3.up * 0.3f);
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
            var dungeon = Dungeon != null ? Dungeon : FindAnyObjectByType<Dungeon>();
            var hud = FindAnyObjectByType<Hud>();
            if (dungeon != null && dungeon.IsSubBiomeEnd)
            {
                var screen = FindAnyObjectByType<AscentScreen>();
                if (screen != null)
                {
                    screen.Open(dungeon.Floor, Gold.Carried);
                    while (screen.IsOpen)
                        yield return null;
                    if (screen.Result == AscentScreen.Choice.None)
                        yield break; // on reste à l'étage : l'escalier reste ouvert
                    if (screen.Result == AscentScreen.Choice.Ascend)
                    {
                        Used = true;
                        Retire();
                        Taken?.Invoke(this);
                        yield return Ascend(hud);
                        yield break;
                    }
                }
            }
            Used = true;
            Retire();
            Taken?.Invoke(this);
            if (hud != null)
            {
                // Le bilan du sous-sol qu'on quitte, affiché pendant la descente.
                var floor = dungeon != null ? dungeon.Floor : 1;
                hud.FloorSummary = Loc.F("bilan.etage", floor, RunStats.Kills - RunStats.FloorStartKills,
                    RunStats.Chests - RunStats.FloorStartChests, player.Turn - RunStats.FloorStartTurn);
                yield return hud.FadeOut(0.6f);
            }
            RunStats.BeginFloor(player.Turn);
            if (dungeon != null)
            {
                dungeon.NextFloor();
                player.PlaceAtStart();
                player.Notify(Loc.F("message.etage", dungeon.Floor));
            }
            if (hud != null)
                yield return hud.FadeIn(0.8f);
        }

        /// <summary>
        /// Remontée : tout l'or porté va au camp, le jeu se fige et le HUD montre le retour à la
        /// surface ; de là, on revient au camp.
        /// </summary>
        IEnumerator Ascend(Hud hud)
        {
            var brought = Gold.EndOnReturn();
            Ascended?.Invoke(this);
            if (hud == null)
            {
                SceneFlow.LoadCamp();
                yield break;
            }
            hud.ShowSurface(brought);
            Time.timeScale = 0f;
            // Le HUD ramène au menu : la scène change, la coroutine s'arrête avec elle.
            while (true)
                yield return null;
        }
    }
}
