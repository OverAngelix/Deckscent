using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Déplacement case par case, façon dungeon crawler classique, au tour par tour.
    /// Avancer / reculer / pas de côté d'une case, rotation par quart de tour, « bump » contre
    /// les murs. Pas de physique : les collisions sont testées sur la grille du labyrinthe.
    ///
    /// Chaque pas ou attente est un tour (<see cref="TurnEnded"/>). Tourner et se cogner sont
    /// gratuits. Les ennemis et pièges joueront à la fin de chaque tour (étape 5 du portage).
    ///
    /// Touches : actions Move, Turn, Wait, Torch de la map Player d'InputSystem_Actions (clavier
    /// en position physique, ZQSD en AZERTY, et manette). Elles se règlent dans ce fichier.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Le labyrinthe où l'on se déplace.")]
        [SerializeField] Maze maze;

        [Header("Rythme")]
        [Tooltip("Durée d'un pas (secondes).")]
        [SerializeField, Min(0.01f)] float moveTime = 0.22f;
        [Tooltip("Durée d'un quart de tour (secondes).")]
        [SerializeField, Min(0.01f)] float turnTime = 0.18f;
        [Tooltip("Durée minimale d'un tour passé à attendre, pour ne pas enchaîner des dizaines de tours en gardant la touche enfoncée.")]
        [SerializeField, Min(0f)] float waitTime = 0.25f;
        [Tooltip("Coché : tourner sur place coûte un tour.")]
        [SerializeField] bool turningTakesTurn;

        // Actions de la map Player d'InputSystem_Actions (Assets/InputSystem_Actions.inputactions),
        // où se règlent les touches. Clavier en position physique (ZQSD en AZERTY) et manette.
        // Vides : prises par leur nom dans les actions du projet.
        [Header("Actions (InputSystem_Actions, map Player)")]
        [Tooltip("Se déplacer : ZQSD, stick gauche, croix. Avant / arrière, pas de côté.")]
        [SerializeField] InputActionReference move;
        [Tooltip("Tourner d'un quart de tour : A / E, LB / RB.")]
        [SerializeField] InputActionReference turn;
        [Tooltip("Attendre un tour : Entrée, L3.")]
        [SerializeField] InputActionReference wait;
        [Tooltip("Allumer / éteindre la lanterne : T, bouton droit (Rond / B). Ne coûte pas de tour.")]
        [SerializeField] InputActionReference torch;
        [Tooltip("Seuil au-delà duquel un stick compte comme une direction.")]
        [SerializeField, Range(0.1f, 0.9f)] float stickThreshold = 0.5f;

        [Header("Lanterne")]
        [Tooltip("La lumière que l'action Torch allume et éteint.")]
        [SerializeField] Light lantern;

        /// <summary>Un tour complet vient de se terminer (le joueur, puis le monde).</summary>
        public event Action<int> TurnEnded;

        public Vector2Int Cell { get; private set; }
        /// <summary>0 = nord, 1 = est, 2 = sud, 3 = ouest.</summary>
        public int Facing { get; private set; }
        /// <summary>Nombre de tours joués depuis le début.</summary>
        public int Turn { get; private set; }
        public Maze Maze => maze;
        /// <summary>Vrai pendant une animation (pas, quart de tour, bump) : les touches sont ignorées.</summary>
        public bool IsBusy => _busy;
        public bool LanternOn => lantern != null && lantern.enabled;

        bool _busy;
        InputAction _move, _turn, _wait, _torch;

        void OnEnable()
        {
            _move = Resolve(move, "Player/Move");
            _turn = Resolve(turn, "Player/Turn");
            _wait = Resolve(wait, "Player/Wait");
            _torch = Resolve(torch, "Player/Torch");
            foreach (var action in new[] { _move, _turn, _wait, _torch })
                action?.Enable();
            if (_torch != null)
                _torch.performed += OnTorch;
        }

        void OnDisable()
        {
            if (_torch != null)
                _torch.performed -= OnTorch;
            foreach (var action in new[] { _move, _turn, _wait, _torch })
                action?.Disable();
        }

        /// <summary>L'action référencée, sinon celle du même nom dans les actions du projet.</summary>
        InputAction Resolve(InputActionReference reference, string path)
        {
            var action = reference != null ? reference.action : InputSystem.actions?.FindAction(path);
            if (action == null)
                Debug.LogError($"Joueur : action {path} introuvable (InputSystem_Actions).", this);
            return action;
        }

        void OnTorch(InputAction.CallbackContext _) => ToggleLantern();

        /// <summary>Allume ou éteint la lanterne. Gratuit : ne coûte pas de tour.</summary>
        public void ToggleLantern()
        {
            if (lantern != null)
                lantern.enabled = !lantern.enabled;
        }
        void Start()
        {
            if (maze == null)
                maze = FindAnyObjectByType<Maze>();
            PlaceAtStart();
        }

        /// <summary>Pose le joueur sur le départ du labyrinthe, tourné vers le premier couloir ouvert.</summary>
        public void PlaceAtStart()
        {
            Cell = maze.StartCell;
            transform.position = maze.CellToWorld(Cell);
            Facing = 0;
            for (var i = 0; i < Grid.Dirs.Length; i++)
            {
                if (maze.CanStep(Cell, i))
                {
                    Facing = i;
                    break;
                }
            }
            transform.rotation = Quaternion.Euler(0f, Grid.Yaw(Facing), 0f);
        }

        void Update()
        {
            if (_busy || maze == null || _move == null || _turn == null || _wait == null)
                return;
            var forward = Grid.Dirs[Facing];
            var right = Grid.Dirs[(Facing + 1) % 4];
            var turnAxis = _turn.ReadValue<float>();
            var m = _move.ReadValue<Vector2>();

            if (turnAxis <= -stickThreshold)
                StartCoroutine(TurnBy(-1));
            else if (turnAxis >= stickThreshold)
                StartCoroutine(TurnBy(1));
            else if (m.sqrMagnitude >= stickThreshold * stickThreshold)
            {
                // Stick en diagonale : on garde l'axe dominant (pas de pas en biais).
                if (Mathf.Abs(m.y) >= Mathf.Abs(m.x))
                    StartCoroutine(Step(m.y > 0f ? forward : -forward));
                else
                    StartCoroutine(Step(m.x > 0f ? right : -right));
            }
            else if (_wait.IsPressed())
                StartCoroutine(Wait());
        }

        IEnumerator TurnBy(int dir)
        {
            _busy = true;
            Facing = (Facing + dir + 4) % 4;
            var from = transform.rotation;
            var to = Quaternion.Euler(0f, Grid.Yaw(Facing), 0f);
            yield return Animate(turnTime, t => transform.rotation = Quaternion.Slerp(from, to, t));
            transform.rotation = to;
            if (turningTakesTurn)
                yield return EndTurn();
            _busy = false;
        }

        IEnumerator Step(Vector2Int dir)
        {
            _busy = true;
            var target = Cell + dir;
            // Sol devant, et pas de mur fin (pourtour d'une salle) entre les deux cases.
            if (maze.CanStep(Cell, System.Array.IndexOf(Grid.Dirs, dir)))
            {
                Cell = target;
                var from = transform.position;
                var to = maze.CellToWorld(Cell);
                yield return Animate(moveTime, t => transform.position = Vector3.Lerp(from, to, t));
                transform.position = to;
                yield return EndTurn();
            }
            else
            {
                // Petit « bump » contre le mur : ne coûte pas de tour.
                yield return Lunge(dir, 0.25f, moveTime * 0.4f);
            }
            _busy = false;
        }

        IEnumerator Wait()
        {
            _busy = true;
            yield return new WaitForSeconds(waitTime);
            yield return EndTurn();
            _busy = false;
        }

        /// <summary>Aller-retour de `distance` mètres dans la direction `dir`.</summary>
        IEnumerator Lunge(Vector2Int dir, float distance, float halfTime)
        {
            var start = transform.position;
            var end = start + Grid.ToWorldDir(dir) * distance;
            yield return Animate(halfTime, t => transform.position = Vector3.Lerp(start, end, t));
            yield return Animate(halfTime, t => transform.position = Vector3.Lerp(end, start, t));
            transform.position = start;
        }

        /// <summary>Fin du tour du joueur. Les acteurs du tour (ennemis, pièges) joueront ici.</summary>
        IEnumerator EndTurn()
        {
            Turn++;
            TurnEnded?.Invoke(Turn);
            yield break;
        }

        /// <summary>Appelle `apply` avec t de 0 à 1 (sinusoïde entrée-sortie) pendant `duration` secondes.</summary>
        static IEnumerator Animate(float duration, Action<float> apply)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                apply(0.5f - 0.5f * Mathf.Cos(Mathf.PI * elapsed / duration));
                yield return null;
            }
            apply(1f);
        }
    }
}
