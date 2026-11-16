using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Suit la mission de l'étage (<see cref="Dungeon.Mission"/>) en jeu : tours du joueur, monstres
    /// tués, coffres ouverts, dalles franchies (rapportés par le <see cref="Spawner"/>) et salles
    /// nettoyées. Prévient le joueur quand un objectif est rempli, puis quand toute la mission l'est.
    ///
    /// Une salle ne compte qu'**une fois nettoyée** (monstres tués, coffres ouverts ; les dalles
    /// n'ont pas à être franchies, voir <see cref="Spawner.TileContents"/>) **et visitée** (le joueur y est entré) : poser une salle
    /// loin et l'éviter ne compte pas, une salle vide compte quand on y entre.
    /// </summary>
    public class MissionTracker : MonoBehaviour
    {
        [Tooltip("Le donjon (sa mission). Vide : cherché dans la scène.")]
        [SerializeField] Dungeon dungeon;
        [Tooltip("Le Spawner (contenu des tuiles). Vide : cherché dans la scène.")]
        [SerializeField] Spawner spawner;
        [Tooltip("Le joueur (tours, salles visitées, messages). Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;

        Mission _mission;
        readonly HashSet<TileCard> _visited = new HashSet<TileCard>();
        readonly HashSet<TileCard> _counted = new HashSet<TileCard>();

        public Mission Mission => _mission;
        /// <summary>Salles comptées comme nettoyées.</summary>
        public int RoomsCleared => _counted.Count;

        void Awake()
        {
            if (dungeon == null)
                dungeon = FindAnyObjectByType<Dungeon>();
            if (spawner == null)
                spawner = FindAnyObjectByType<Spawner>();
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
        }

        void Start()
        {
            if (dungeon != null)
            {
                dungeon.GameStarted += Bind;
                dungeon.MissionChanged += Bind;
            }
            if (spawner != null)
            {
                spawner.ElementCleared += OnElementCleared;
                spawner.TileCleared += OnTileCleared;
            }
            if (player != null)
                player.TurnEnded += OnTurnEnded;
            Bind();
        }

        void OnDestroy()
        {
            if (dungeon != null)
            {
                dungeon.GameStarted -= Bind;
                dungeon.MissionChanged -= Bind;
            }
            if (spawner != null)
            {
                spawner.ElementCleared -= OnElementCleared;
                spawner.TileCleared -= OnTileCleared;
            }
            if (player != null)
                player.TurnEnded -= OnTurnEnded;
            Unbind();
        }

        /// <summary>Nouvelle partie : la nouvelle mission, aucune salle visitée.</summary>
        void Bind()
        {
            Unbind();
            _visited.Clear();
            _counted.Clear();
            _mission = dungeon != null ? dungeon.Mission : null;
            if (_mission == null)
                return;
            _mission.GoalDone += OnGoalDone;
            _mission.Completed += OnCompleted;
            if (player != null)
                Visit(player.Cell);
        }

        void Unbind()
        {
            if (_mission == null)
                return;
            _mission.GoalDone -= OnGoalDone;
            _mission.Completed -= OnCompleted;
            _mission = null;
        }

        void OnElementCleared(GridEntity entity)
        {
            if (_mission == null)
                return;
            switch (entity)
            {
                case Enemy _: _mission.Report(GoalKind.Monsters); break;
                case Chest _: _mission.Report(GoalKind.Chests); break;
                case SpikeTrap _: _mission.Report(GoalKind.Traps); break;
            }
        }

        void OnTileCleared(Spawner.TileContents contents) => TryCount(contents.Card);

        void OnTurnEnded(int turn)
        {
            _mission?.SetTurn(turn);
            if (player != null)
                Visit(player.Cell);
        }

        /// <summary>Le joueur est sur cette case : sa salle est visitée (appelé à chaque fin de tour ; public pour les tests).</summary>
        public void Visit(Vector2Int cell)
        {
            var board = dungeon != null ? dungeon.Board : null;
            if (board == null || cell.x < 0 || cell.y < 0)
                return;
            var card = board.GetTile(new Vector2Int(cell.x / DungeonBoard.TileCells, cell.y / DungeonBoard.TileCells));
            // La salle de départ ne compte pas : on y est déjà.
            if (card == null || !card.IsRoom || card == board.GetTile(board.StartTile) || !_visited.Add(card))
                return;
            TryCount(card);
        }

        /// <summary>Compte la salle si elle est visitée et nettoyée (ou vide), une seule fois.</summary>
        void TryCount(TileCard card)
        {
            if (_mission == null || card == null || !card.IsRoom || !_visited.Contains(card) || _counted.Contains(card))
                return;
            var contents = spawner != null ? spawner.ContentsOf(card) : null;
            if (contents != null && !contents.IsCleared)
                return;
            _counted.Add(card);
            _mission.Report(GoalKind.Rooms);
        }

        void OnGoalDone(MissionGoal goal)
        {
            if (player != null && !_mission.Goals.All(g => g.Done))
                player.Notify(Loc.F("message.objectif", Loc.T(goal.LabelKey)));
        }

        void OnCompleted()
        {
            if (player != null)
                player.Notify(Loc.T("message.mission_remplie"));
        }
    }
}
