using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Nourrit la mémoire de l'étage (<see cref="Dungeon.Exploration"/>) : à chaque case où se
    /// trouve le joueur, et à chaque tuile posée (une tuile posée juste à côté peut se voir tout de
    /// suite), ce qu'il voit devient connu. La carte dessine le reste en esquisse.
    /// </summary>
    public class ExplorationTracker : MonoBehaviour
    {
        [Tooltip("Le donjon (plateau, labyrinthe, mémoire de l'étage). Vide : cherché dans la scène.")]
        [SerializeField] Dungeon dungeon;
        [Tooltip("Le joueur. Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;
        [Tooltip("Jusqu'où on voit tout droit (en cases de 3 m). Au-delà, l'obscurité du rendu gravure est complète.")]
        [SerializeField, Range(1, 12)] int sightRange = 5;

        public int SightRange => sightRange;

        DungeonBoard _board;
        Exploration _exploration;
        Vector2Int _lastCell = new Vector2Int(int.MinValue, int.MinValue);
        bool _dirty = true;

        void Awake()
        {
            if (dungeon == null)
                dungeon = FindAnyObjectByType<Dungeon>();
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
        }

        void OnDestroy()
        {
            if (_board != null)
                _board.TilesChanged -= MarkDirty;
        }

        void MarkDirty() => _dirty = true;

        void LateUpdate()
        {
            if (dungeon == null || player == null || dungeon.Maze == null)
                return;
            // Nouvelle partie : nouveau plateau, nouvelle mémoire.
            if (dungeon.Board != _board || dungeon.Exploration != _exploration)
            {
                if (_board != null)
                    _board.TilesChanged -= MarkDirty;
                _board = dungeon.Board;
                _exploration = dungeon.Exploration;
                if (_board != null)
                    _board.TilesChanged += MarkDirty;
                _dirty = true;
            }
            if (_exploration == null || (!_dirty && player.Cell == _lastCell))
                return;
            Look();
        }

        /// <summary>Ce que le joueur voit de sa case devient connu (appelé tout seul ; public pour les tests).</summary>
        public void Look()
        {
            if (_exploration == null || player == null)
                return;
            _dirty = false;
            _lastCell = player.Cell;
            var maze = dungeon.Maze;
            _exploration.Look(player.Cell, maze.CanStep, sightRange, _board);
        }
    }
}
