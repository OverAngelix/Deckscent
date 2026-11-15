using System;
using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Piège : dalle à pics, posée au sol d'une case. Les pics suivent un cycle de
    /// <see cref="period"/> tours : sortis pendant <see cref="activeTurns"/> tours en fin de cycle,
    /// et le tour d'avant leurs pointes dépassent (alerte). Par défaut : 1 tour sur 3.
    ///
    /// Blesse le joueur s'il est sur la dalle quand les pics sortent, et les ennemis aussi (on peut
    /// les y attirer). Avancer sur la dalle pics sortis est permis : c'est un tour, donc ils
    /// rentrent (dès le début du pas, voir <see cref="Anticipate"/>).
    ///
    /// Nettoyée (<see cref="Cleared"/>) quand le joueur l'a traversée : il y est passé, puis en est
    /// ressorti. Elle reste dangereuse ensuite.
    /// </summary>
    public class SpikeTrap : GridEntity
    {
        public enum State { Down, Warning, Up }

        [Header("Cycle")]
        [Tooltip("Longueur du cycle, en tours.")]
        [SerializeField, Range(2, 12)] int period = 3;
        [Tooltip("Nombre de tours où les pics restent sortis, à la fin du cycle.")]
        [SerializeField, Range(1, 11)] int activeTurns = 1;
        [Tooltip("Décale le cycle (pour désynchroniser plusieurs dalles voisines).")]
        [SerializeField, Range(0, 11)] int phaseOffset;
        [Tooltip("Le tour avant la sortie, les pointes dépassent de la dalle.")]
        [SerializeField] bool showWarning = true;

        [Header("Combat")]
        [Tooltip("Dégâts quand les pics touchent (joueur ou ennemi), avant la défense.")]
        [SerializeField, Range(0, 20)] int damage = 2;

        [Header("Pics")]
        [Tooltip("Le groupe de pics qui monte et descend (enfant).")]
        [SerializeField] Transform spikes;
        [Tooltip("Hauteur des pics au-dessus du sol quand ils sont sortis (mètres) : leur point le plus haut.")]
        [SerializeField, Min(0.1f)] float spikeHeight = 0.9f;
        [Tooltip("Hauteur des pointes en alerte (mètres).")]
        [SerializeField, Min(0f)] float warningHeight = 0.4f;
        [Tooltip("Durée de la sortie des pics (secondes) : sèche.")]
        [SerializeField, Min(0.01f)] float riseTime = 0.1f;
        [Tooltip("Durée de la rentrée des pics (secondes) : plus lente.")]
        [SerializeField, Min(0.01f)] float lowerTime = 0.25f;

        [Header("Sons")]
        [Tooltip("Ce qui joue le bruit des pics (piste Effets du mixer, son en 3D). Vide : cherché sur la dalle.")]
        [SerializeField] SoundEmitter sounds;
        [Tooltip("Volume du bruit des pics qui sortent (0 à 1).")]
        [SerializeField, Range(0f, 1f)] float spikesVolume = 0.7f;

        /// <summary>Émis une fois, quand le joueur ressort de la dalle pour la première fois.</summary>
        public event Action<SpikeTrap> Cleared;

        public State Current { get; private set; } = State.Down;
        public bool IsCleared { get; private set; }
        public int Period => period;
        public int Damage => damage;

        public int PhaseOffset
        {
            get => phaseOffset;
            set
            {
                phaseOffset = Mathf.Clamp(value, 0, 11);
                Refresh(_turn);
            }
        }

        int _turn;
        Vector2Int _lastPlayerCell = new Vector2Int(int.MinValue, int.MinValue);
        Coroutine _move;

        void Start()
        {
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
            Refresh(_turn);
        }

        // Pas de Refresh ici : dans l'éditeur, le prefab garde ses pics sortis (on les voit et on les règle).
        void OnValidate() => activeTurns = Mathf.Min(activeTurns, period - 1);

        /// <summary>État du cycle pour un numéro de tour donné.</summary>
        public State StateFor(int turn)
        {
            var p = ((turn + phaseOffset) % period + period) % period;
            var upFrom = Mathf.Max(period - activeTurns, 0);
            if (p >= upFrom)
                return State.Up;
            if (showWarning && p == upFrom - 1)
                return State.Warning;
            return State.Down;
        }

        public override void Place(Maze maze, Vector2Int cell)
        {
            base.Place(maze, cell);
            transform.rotation = Quaternion.identity;
        }

        // --- Tour par tour ---------------------------------------------------------

        public override IEnumerator TakeTurn(PlayerController player)
        {
            if (Maze == null || !Maze.IsWalkable(Cell))
                yield break;
            var left = player.Cell != Cell && _lastPlayerCell == Cell;
            _lastPlayerCell = player.Cell;
            if (left && !IsCleared)
            {
                IsCleared = true;
                Cleared?.Invoke(this);
                player.Notify(Loc.T("message.piege_franchi"));
            }
            _turn = player.Turn;
            SetState(StateFor(_turn));
            if (Current != State.Up)
                yield break;
            var enemy = At<Enemy>(Cell);
            if (enemy != null)
                enemy.TakeDamage(damage);
            if (player.Cell == Cell)
            {
                player.TakeDamage(damage);
                // On laisse voir le coup avant que le monde continue.
                yield return new WaitForSeconds(0.15f);
            }
        }

        /// <summary>
        /// Appelé par le joueur au début d'un pas vers la dalle : si les pics vont rentrer au tour
        /// `nextTurn`, ils rentrent tout de suite, pour qu'on ne les traverse pas à l'écran.
        /// </summary>
        public void Anticipate(int nextTurn)
        {
            var next = StateFor(nextTurn);
            if (Current == State.Up && next != State.Up)
                SetState(next);
        }

        void Refresh(int turn)
        {
            Current = StateFor(turn);
            if (spikes != null)
                spikes.localPosition = new Vector3(0f, SpikesY(Current), 0f);
        }

        void SetState(State state)
        {
            if (state == Current)
                return;
            Current = state;
            if (state == State.Up && sounds != null)
                sounds.Play(SoundBank.Spikes, spikesVolume);
            if (spikes == null)
                return;
            if (_move != null)
                StopCoroutine(_move);
            var from = spikes.localPosition.y;
            var to = SpikesY(state);
            _move = state == State.Up
                ? StartCoroutine(Animate(riseTime, t => spikes.localPosition = new Vector3(0f, Mathf.LerpUnclamped(from, to, BackOut(t)), 0f)))
                : StartCoroutine(Animate(lowerTime, t => spikes.localPosition = new Vector3(0f, Mathf.Lerp(from, to, Smooth(t)), 0f)));
        }

        /// <summary>Hauteur du groupe de pics : sous le sol au repos, pointes à `warningHeight` en alerte.</summary>
        float SpikesY(State s)
        {
            switch (s)
            {
                case State.Up: return 0f;
                case State.Warning: return warningHeight - spikeHeight;
                default: return -spikeHeight - 0.05f;
            }
        }

        static float BackOut(float t)
        {
            const float c1 = 1.2f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
