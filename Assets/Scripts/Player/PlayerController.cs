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
    /// Touches en position physique : ZQSD en AZERTY (WASD en QWERTY). Elles se changent dans
    /// l'inspecteur du prefab Player.
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

        [Header("Touches (position physique, disposition QWERTY)")]
        [SerializeField] InputAction moveForward = Keys("Avancer", "<Keyboard>/w", "<Keyboard>/upArrow");
        [SerializeField] InputAction moveBack = Keys("Reculer", "<Keyboard>/s", "<Keyboard>/downArrow");
        [SerializeField] InputAction strafeLeft = Keys("Pas à gauche", "<Keyboard>/a");
        [SerializeField] InputAction strafeRight = Keys("Pas à droite", "<Keyboard>/d");
        [SerializeField] InputAction turnLeft = Keys("Tourner à gauche", "<Keyboard>/q", "<Keyboard>/leftArrow");
        [SerializeField] InputAction turnRight = Keys("Tourner à droite", "<Keyboard>/e", "<Keyboard>/rightArrow");
        [SerializeField] InputAction waitTurn = Keys("Attendre", "<Keyboard>/space");

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

        bool _busy;

        static InputAction Keys(string actionName, params string[] bindings)
        {
            var action = new InputAction(actionName, InputActionType.Button);
            foreach (var binding in bindings)
                action.AddBinding(binding);
            return action;
        }

        InputAction[] AllActions => new[] { moveForward, moveBack, strafeLeft, strafeRight, turnLeft, turnRight, waitTurn };

        void OnEnable()
        {
            foreach (var action in AllActions)
                action.Enable();
        }

        void OnDisable()
        {
            foreach (var action in AllActions)
                action.Disable();
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
                if (maze.IsWalkable(Cell + Grid.Dirs[i]))
                {
                    Facing = i;
                    break;
                }
            }
            transform.rotation = Quaternion.Euler(0f, Grid.Yaw(Facing), 0f);
        }

        void Update()
        {
            if (_busy || maze == null)
                return;
            var forward = Grid.Dirs[Facing];
            var right = Grid.Dirs[(Facing + 1) % 4];

            if (turnLeft.IsPressed())
                StartCoroutine(TurnBy(-1));
            else if (turnRight.IsPressed())
                StartCoroutine(TurnBy(1));
            else if (moveForward.IsPressed())
                StartCoroutine(Step(forward));
            else if (moveBack.IsPressed())
                StartCoroutine(Step(-forward));
            else if (strafeLeft.IsPressed())
                StartCoroutine(Step(-right));
            else if (strafeRight.IsPressed())
                StartCoroutine(Step(right));
            else if (waitTurn.IsPressed())
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
            if (maze.IsWalkable(target))
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
