using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Le donjon de la partie : le plateau de tuiles (<see cref="DungeonBoard"/>) et la main du
    /// joueur (<see cref="TileDeck"/>). À chaque changement du plateau, il réécrit le plan du
    /// labyrinthe (<see cref="Maze.SetPlan"/>), qui se reconstruit.
    ///
    /// Dans l'éditeur aussi : on y voit le départ choisi, ou les 10 départs côte à côte si
    /// <see cref="starterIndex"/> vaut -1 (au hasard en jeu). Rien n'est enregistré dans la scène
    /// à part les réglages.
    /// </summary>
    [ExecuteAlways]
    public class Dungeon : MonoBehaviour
    {
        [Tooltip("Le labyrinthe à construire d'après le plateau.")]
        [SerializeField] Maze maze;
        [Tooltip("Nombre d'emplacements de tuiles (largeur, hauteur). Les départs sont prévus pour 9 × 9.")]
        [SerializeField] Vector2Int boardSize = new Vector2Int(9, 9);
        [Tooltip("Départ (indice dans DungeonBoard.Starters). -1 = au hasard à chaque partie ; dans l'éditeur, les 10 départs côte à côte.")]
        [SerializeField, Range(-1, 9)] int starterIndex = -1;
        [Tooltip("Graine du hasard (départ, pioche). 0 = différent à chaque partie.")]
        [SerializeField] int seed;
        [Tooltip("Coché : pose le donjon de démo (toutes les sortes de salles) autour du départ 0. Pour les captures et les essais.")]
        [SerializeField] bool demo;
        [Tooltip("Réglages de la pioche : taille de la main, poids des formes et des contenus.")]
        [SerializeField] DeckSettings deck = new DeckSettings();

        public DungeonBoard Board { get; private set; }
        public TileDeck Deck { get; private set; }
        public Maze Maze => maze;

#if UNITY_EDITOR
        bool _refreshQueued;
#endif

        void OnEnable()
        {
            if (maze == null)
                maze = GetComponentInChildren<Maze>() ?? FindAnyObjectByType<Maze>();
            if (maze == null)
            {
                Debug.LogError("Donjon : aucun labyrinthe à construire.", this);
                return;
            }
            if (!Application.isPlaying && starterIndex < 0 && !demo)
            {
                Board = null;
                maze.SetPlan(DungeonBoard.AllStartersLayout(boardSize));
                return;
            }
            NewGame();
        }

        void OnDisable()
        {
            if (Board != null)
                Board.TilesChanged -= Apply;
            // Dans l'éditeur, le labyrinthe revient à son propre plan.
            if (!Application.isPlaying && maze != null)
                maze.SetPlan("");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_refreshQueued)
                return;
            _refreshQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _refreshQueued = false;
                if (this != null && isActiveAndEnabled && !Application.isPlaying)
                {
                    OnDisable();
                    OnEnable();
                }
            };
        }
#endif

        /// <summary>Nouveau plateau : le départ (et le donjon de démo si coché), une main neuve.</summary>
        public void NewGame()
        {
            if (Board != null)
                Board.TilesChanged -= Apply;
            var rng = seed != 0 ? new System.Random(seed) : new System.Random();
            Board = new DungeonBoard(boardSize);
            Board.NewGame(demo ? DemoDungeon.Starter : starterIndex, rng);
            if (demo)
                foreach (var refusal in DemoDungeon.Build(Board))
                    Debug.LogWarning("Donjon de démo : tuile refusée, " + refusal, this);
            Board.TilesChanged += Apply;
            var rooms = new System.Collections.Generic.HashSet<TileCard>();
            foreach (var pos in Board.TilePositions)
                if (Board.GetTile(pos).IsRoom)
                    rooms.Add(Board.GetTile(pos));
            Deck = new TileDeck(deck, seed, rooms.Count);
            // Plafond de salles par étage : la pioche compte celles qu'on pose.
            Board.TilePlaced += (_, card) =>
            {
                if (card.IsRoom)
                    Deck.RoomsPlaced++;
            };
            Apply();
        }

        /// <summary>Pose une carte de la main sur le plateau (ancre sur `pos`). Vrai si posée.</summary>
        public bool Place(Vector2Int pos, TileCard card) => Board != null && Board.Place(pos, card);

        void Apply()
        {
            if (maze != null && Board != null)
                maze.SetPlan(Board.ToLayout(), Board.EdgeWalls());
        }
    }
}
