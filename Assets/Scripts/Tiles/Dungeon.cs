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
        [Tooltip("Tirage de la mission de l'étage : nombre d'objectifs et comptes visés.")]
        [SerializeField] MissionSettings mission = new MissionSettings();

        public DungeonBoard Board { get; private set; }
        public TileDeck Deck { get; private set; }
        /// <summary>La mission de l'étage, tirée à chaque nouvelle partie (suivie par <see cref="MissionTracker"/>).</summary>
        public Mission Mission { get; private set; }
        /// <summary>Mémoire de l'étage (cases vues, emplacements visités), neuve à chaque partie (<see cref="ExplorationTracker"/>).</summary>
        public Exploration Exploration { get; private set; }
        public Maze Maze => maze;
        /// <summary>Étage en cours (1 = le premier). Monte à chaque boss vaincu (<see cref="NextFloor"/>).</summary>
        public int Floor { get; private set; } = 1;
        /// <summary>Nouveau plateau et nouvelle main (le Spawner remplit alors les tuiles déjà posées). Aussi à chaque nouvel étage.</summary>
        public event System.Action GameStarted;
        /// <summary>Le jeu vient de poser l'antre du boss (mission remplie).</summary>
        public event System.Action<TileCard> BossLairPlaced;

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

        /// <summary>Nouvelle partie : premier étage.</summary>
        public void NewGame()
        {
            Floor = 1;
            if (Application.isPlaying)
            {
                RunStats.Reset();
                Gold.ResetRun();
            }
            StartFloor();
        }

        /// <summary>
        /// Étage suivant (le boss est vaincu, on descend) : nouveau plateau, nouveau départ, nouvelle
        /// main et nouvelle mission. Le joueur garde ses PV (choix de l'utilisateur), c'est à lui de
        /// se replacer au départ (<see cref="GameStarted"/>).
        /// </summary>
        public void NextFloor()
        {
            Floor++;
            StartFloor();
        }

        /// <summary>
        /// Pose l'antre du boss (mission remplie) : sur des emplacements jamais visités, le plus loin
        /// possible du joueur (<see cref="DungeonBoard.FindBossLair"/>). Faux s'il n'y a pas de place
        /// pour l'instant (à réessayer quand le plateau ou le joueur bouge) ou si elle est déjà posée.
        /// </summary>
        public bool TryPlaceBossLair(Vector2Int playerCell)
        {
            if (Board == null || Board.BossLair != null)
                return false;
            var plan = Board.FindBossLair(Exploration.SlotOf(playerCell), s => Exploration != null && Exploration.IsSlotVisited(s));
            if (plan == null)
                return false;
            // Ce qui avait été vu des tuiles remplacées est oublié : l'antre reste à découvrir.
            Exploration?.Forget(plan.Card.Slots.ConvertAll(o => plan.Anchor + o));
            Board.PlaceBossLair(plan);
            BossLairPlaced?.Invoke(plan.Card);
            return true;
        }

        void StartFloor()
        {
            if (Board != null)
                Board.TilesChanged -= Apply;
            if (maze != null)
                maze.ClearHoles(); // l'escalier de l'étage d'avant
            // Graine fixe : chaque étage a quand même son propre tirage.
            var floorSeed = seed != 0 ? seed + (Floor - 1) * 7919 : 0;
            var rng = floorSeed != 0 ? new System.Random(floorSeed) : new System.Random();
            Board = new DungeonBoard(boardSize);
            Board.NewGame(demo && Floor == 1 ? DemoDungeon.Starter : Floor == 1 ? starterIndex : -1, rng);
            if (demo && Floor == 1)
                foreach (var refusal in DemoDungeon.Build(Board))
                    Debug.LogWarning("Donjon de démo : tuile refusée, " + refusal, this);
            Board.TilesChanged += Apply;
            var rooms = new System.Collections.Generic.HashSet<TileCard>();
            foreach (var pos in Board.TilePositions)
                if (Board.GetTile(pos).IsRoom)
                    rooms.Add(Board.GetTile(pos));
            Deck = new TileDeck(deck, floorSeed, rooms.Count);
            // Plafond de salles par étage : la pioche compte celles qu'on pose (pas l'antre du boss).
            Board.TilePlaced += (_, card) =>
            {
                if (card.IsRoom && !card.IsBossLair)
                    Deck.RoomsPlaced++;
            };
            // Hasard à part pour la mission : le tirage des cartes n'en dépend pas.
            Exploration = new Exploration();
            Mission = Mission.Draw(floorSeed != 0 ? new System.Random(floorSeed * 31 + 7) : new System.Random(), mission);
            Apply();
            GameStarted?.Invoke();
        }

        /// <summary>La mission vient d'être remplacée (<see cref="SetMission"/>).</summary>
        public event System.Action MissionChanged;

        /// <summary>Remplace la mission de l'étage (tests, captures).</summary>
        public void SetMission(Mission mission)
        {
            Mission = mission;
            MissionChanged?.Invoke();
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
