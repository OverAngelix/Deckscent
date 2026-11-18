using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Fait apparaître le contenu des tuiles posées, tel que la carte l'annonçait
    /// (<see cref="TileCard.Content"/>, tiré par la pioche) : un élément au plus pour un couloir,
    /// plusieurs pour une salle (<see cref="TileCard.SpawnList"/>). Éléments : slime (« monstre »),
    /// dalle à pics (« piege »), coffre (« tresor »), loup-garou (« boss », dans l'antre posée par le
    /// jeu, avec son décor : <see cref="LairDecor.Layout"/>). Les tuiles du départ et les cartes
    /// « vide » restent vides. Les ennemis sont renforcés selon l'étage (<see cref="Enemy.ScaleForFloor"/>).
    ///
    /// Le boss vaincu laisse l'**escalier** (<see cref="Stairs"/>) ; ses louveteaux fuient. Une tuile
    /// remplacée par l'antre perd son contenu.
    ///
    /// Chaque élément est une instance de son prefab, enfant de cet objet. Le placement dans la
    /// tuile (quelle case de sol), la sorte de slime et le décalage des dalles sont tirés avec un
    /// générateur à part : le tirage des cartes n'en dépend pas.
    ///
    /// Chaque élément est rattaché à sa tuile (<see cref="TileContents"/>) : la tuile est
    /// nettoyée quand tous ses monstres sont morts et ses coffres ouverts (<see cref="TileCleared"/>,
    /// pour la mission de l'étage). Les dalles n'en font pas partie : les franchir est un objectif
    /// à part, il serait pénible de devoir passer sur toutes.
    ///
    /// En jeu seulement. Aucun ennemi ni piège n'est posé à la main dans la scène de jeu.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Tooltip("Le donjon dont on suit les poses. Vide : cherché dans la scène.")]
        [SerializeField] Dungeon dungeon;

        [Header("Prefabs")]
        [Tooltip("« monstre » : un slime (vert, rouge ou bleu, à chances égales).")]
        [SerializeField] Slime slimePrefab;
        [Tooltip("« piege » : une dalle à pics.")]
        [SerializeField] SpikeTrap spikeTrapPrefab;
        [Tooltip("« tresor » : un coffre.")]
        [SerializeField] Chest chestPrefab;
        [Tooltip("« boss » : le loup-garou, dans son antre.")]
        [SerializeField] Werewolf werewolfPrefab;
        [Tooltip("Louveteau appelé par le loup-garou à la moitié de ses PV.")]
        [SerializeField] WolfCub cubPrefab;
        [Tooltip("L'escalier vers l'étage suivant, laissé par le boss vaincu.")]
        [SerializeField] Stairs stairsPrefab;

        [Header("Décor de l'antre")]
        [Tooltip("Brasero (coins de l'antre, avec sa lumière).")]
        [SerializeField] LairDecor braseroPrefab;
        [Tooltip("Trône du loup-garou (contre le mur le plus loin des portes).")]
        [SerializeField] LairDecor tronePrefab;
        [Tooltip("Cage de torture (le long des murs des grandes antres).")]
        [SerializeField] LairDecor cagePrefab;
        [Tooltip("Tas d'os (le long des murs des grandes antres).")]
        [SerializeField] LairDecor osPrefab;
        [Tooltip("Ossements épars au sol (décor de passage, ne bloque pas).")]
        [SerializeField] LairDecor ossementsPrefab;

        [Header("Réglages")]
        [Tooltip("Décoché : plus rien n'apparaît (tests, essais).")]
        [SerializeField] bool spawnOnPlace = true;
        [Tooltip("Graine du placement. 0 = différent à chaque partie.")]
        [SerializeField] int seed;

        /// <summary>Un élément vient d'apparaître.</summary>
        public event System.Action<GridEntity> Spawned;
        /// <summary>Un élément est réglé : monstre tué, coffre ouvert, dalle franchie.</summary>
        public event System.Action<GridEntity> ElementCleared;
        /// <summary>Tout le contenu d'une tuile est réglé.</summary>
        public event System.Action<TileContents> TileCleared;

        /// <summary>Ce qui est apparu sur une carte posée.</summary>
        public class TileContents
        {
            public Vector2Int Origin;
            public TileCard Card;
            public readonly List<GridEntity> Elements = new List<GridEntity>();
            public readonly HashSet<GridEntity> Cleared = new HashSet<GridEntity>();

            /// <summary>Monstres morts et coffres ouverts (les dalles ne comptent pas).</summary>
            public bool IsCleared => Elements.TrueForAll(e => e is SpikeTrap || Cleared.Contains(e));
        }

        readonly Dictionary<TileCard, TileContents> _contents = new Dictionary<TileCard, TileContents>();

        /// <summary>Le contenu apparu sur cette carte (null si elle n'a rien fait apparaître).</summary>
        public TileContents ContentsOf(TileCard card) => card != null && _contents.TryGetValue(card, out var c) ? c : null;

        public bool SpawnOnPlace { get => spawnOnPlace; set => spawnOnPlace = value; }

        DungeonBoard _board;
        System.Random _rng;

        void Awake()
        {
            _rng = seed != 0 ? new System.Random(seed) : new System.Random();
            if (dungeon == null)
                dungeon = FindAnyObjectByType<Dungeon>();
        }

        void Start()
        {
            if (dungeon == null)
                return;
            dungeon.GameStarted += OnGameStarted;
            OnGameStarted();
        }

        void OnDestroy()
        {
            if (dungeon != null)
                dungeon.GameStarted -= OnGameStarted;
            Unbind();
        }

        void Unbind()
        {
            if (_board == null)
                return;
            _board.TilePlaced -= OnTilePlaced;
            _board.TileRemoved -= OnTileRemoved;
        }

        int Floor => dungeon != null ? dungeon.Floor : 1;

        /// <summary>Nouveau plateau : on vide, on suit ses poses, et on remplit les tuiles déjà là (donjon de démo).</summary>
        void OnGameStarted()
        {
            Clear();
            Unbind();
            _board = dungeon.Board;
            if (_board == null)
                return;
            _board.TilePlaced += OnTilePlaced;
            _board.TileRemoved += OnTileRemoved;
            if (!spawnOnPlace)
                return;
            var seen = new HashSet<TileCard>();
            foreach (var pos in new List<Vector2Int>(_board.TilePositions))
            {
                var card = _board.GetTile(pos);
                if (seen.Add(card))
                    SpawnCard(_board.TileOrigin(pos), card);
            }
        }

        void OnTilePlaced(Vector2Int pos, TileCard card)
        {
            if (spawnOnPlace)
                SpawnCard(pos, card);
        }

        /// <summary>Une tuile inexplorée est remplacée par l'antre du boss : son contenu disparaît avec elle.</summary>
        void OnTileRemoved(Vector2Int pos, TileCard card)
        {
            if (!_contents.TryGetValue(card, out var contents))
                return;
            _contents.Remove(card);
            foreach (var e in contents.Elements)
                if (e != null)
                    Destroy(e.gameObject);
        }

        /// <summary>Retire tout ce qui est apparu.</summary>
        public void Clear()
        {
            _contents.Clear();
            _cubs.Clear();
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        /// <summary>
        /// Fait apparaître le contenu d'une carte posée en `pos` : un élément par case de sol, cases
        /// tirées au hasard (jamais sur une porte de salle). Les dalles d'une même tuile ne sortent
        /// pas au même tour. Renvoie les objets créés.
        /// </summary>
        public List<GridEntity> SpawnCard(Vector2Int pos, TileCard card)
        {
            var created = new List<GridEntity>();
            if (card.IsBossLair && _board != null)
                return SpawnLair(pos, card);
            var elements = card.SpawnList();
            if (elements.Length == 0 || _board == null)
                return created;
            var cells = _board.TileFloorCells(pos);
            for (var i = cells.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }
            var trapPhase = _rng.Next(3);
            var contents = new TileContents { Origin = pos, Card = card };
            for (var i = 0; i < Mathf.Min(elements.Length, cells.Count); i++)
            {
                var entity = SpawnElement(elements[i], cells[i]);
                if (entity == null)
                    continue;
                if (entity is SpikeTrap trap)
                    trap.PhaseOffset = trapPhase++ % trap.Period;
                created.Add(entity);
                Attach(contents, entity);
            }
            if (contents.Elements.Count > 0)
                _contents[card] = contents;
            return created;
        }

        /// <summary>Rattache un élément à sa tuile et suit sa fin (mort, ouverture, franchissement).</summary>
        void Attach(TileContents contents, GridEntity entity)
        {
            contents.Elements.Add(entity);
            void Done(GridEntity e)
            {
                var wasCleared = contents.IsCleared;
                if (!contents.Cleared.Add(e))
                    return;
                ElementCleared?.Invoke(e);
                if (!wasCleared && contents.IsCleared)
                    TileCleared?.Invoke(contents);
            }
            switch (entity)
            {
                case Enemy enemy: enemy.Died += e => Done(e); break;
                case Chest chest: chest.Cleared += c => Done(c); break;
                case SpikeTrap trap: trap.Cleared += t => Done(t); break;
            }
        }

        // --- Antre du boss ------------------------------------------------------------------

        /// <summary>
        /// L'antre du boss : le décor le long des murs (braseros, trône, cages, os), puis le
        /// loup-garou endormi devant son trône. Renvoie tout ce qui est apparu.
        /// </summary>
        List<GridEntity> SpawnLair(Vector2Int pos, TileCard card)
        {
            var created = new List<GridEntity>();
            var cells = new List<Vector2Int>();
            var doors = new HashSet<Vector2Int>();
            foreach (var offset in card.Slots)
                for (var ly = 0; ly < DungeonBoard.TileCells; ly++)
                    for (var lx = 0; lx < DungeonBoard.TileCells; lx++)
                    {
                        var cell = (pos + offset) * DungeonBoard.TileCells + new Vector2Int(lx, ly);
                        cells.Add(cell);
                        if (card.IsDoorCell(offset, lx, ly))
                            doors.Add(cell);
                    }
            var contents = new TileContents { Origin = pos, Card = card };
            var layout = LairDecor.Layout(cells, doors.Contains, _rng, out var bossCell);
            foreach (var (cell, kind) in layout)
            {
                var prefab = kind switch
                {
                    "brasero" => braseroPrefab,
                    "trone" => tronePrefab,
                    "cage" => cagePrefab,
                    "os" => osPrefab,
                    _ => ossementsPrefab,
                };
                var decor = Spawn(prefab, cell, "Décor_" + kind);
                if (decor == null)
                    continue;
                // Tourné vers le centre de l'antre.
                var to = (Vector2)(bossCell - cell);
                if (to.sqrMagnitude > 0f)
                    decor.transform.rotation = Quaternion.LookRotation(new Vector3(to.x, 0f, -to.y));
                created.Add(decor);
            }
            var boss = Spawn(werewolfPrefab, bossCell, "LoupGarou");
            if (boss != null)
            {
                boss.ScaleForFloor(Floor);
                boss.Lair = new HashSet<Vector2Int>(cells);
                boss.CallsCubs += OnCallsCubs;
                boss.Died += OnBossDied;
                // Il dort tourné vers le centre, dos au trône.
                var center = cells[cells.Count / 2];
                var to = (Vector2)(center - bossCell);
                if (to.sqrMagnitude > 0f)
                    boss.transform.rotation = Quaternion.LookRotation(new Vector3(to.x, 0f, -to.y));
                created.Add(boss);
                Attach(contents, boss);
            }
            _contents[card] = contents;
            return created;
        }

        readonly List<WolfCub> _cubs = new List<WolfCub>();

        /// <summary>2ᵉ phase : des louveteaux sur les cases libres les plus proches du boss.</summary>
        void OnCallsCubs(Werewolf boss, int count)
        {
            var maze = boss.Maze;
            var player = FindAnyObjectByType<PlayerController>();
            var taken = new HashSet<Vector2Int>();
            foreach (var e in GridEntity.All)
                if (e.IsActive && e.BlocksPlayer)
                    taken.Add(e.Cell);
            if (player != null)
                taken.Add(player.Cell);
            // Largeur d'abord autour du boss : les cases libres les plus proches.
            var frontier = new Queue<Vector2Int>();
            var seen = new HashSet<Vector2Int> { boss.Cell };
            frontier.Enqueue(boss.Cell);
            while (frontier.Count > 0 && count > 0)
            {
                var c = frontier.Dequeue();
                for (var d = 0; d < Grid.Dirs.Length; d++)
                {
                    var n = c + Grid.Dirs[d];
                    if (!maze.CanStep(c, d) || !seen.Add(n) || (boss.Lair != null && !boss.Lair.Contains(n)))
                        continue;
                    frontier.Enqueue(n);
                    if (taken.Contains(n) || count <= 0)
                        continue;
                    var cub = Spawn(cubPrefab, n, "Louveteau");
                    if (cub == null)
                        return;
                    cub.ScaleForFloor(Floor);
                    taken.Add(n);
                    _cubs.Add(cub);
                    count--;
                }
            }
        }

        /// <summary>Le boss est vaincu : la meute fuit, l'escalier sort du sol à sa place.</summary>
        void OnBossDied(Enemy boss)
        {
            foreach (var cub in _cubs)
                if (cub != null && cub.IsActive)
                    cub.Flee();
            _cubs.Clear();
            FindAnyObjectByType<PlayerController>()?.Notify(Loc.T("message.boss_vaincu"));
            StartCoroutine(OpenStairs(boss.Cell, boss.Maze));
        }

        System.Collections.IEnumerator OpenStairs(Vector2Int cell, Maze maze)
        {
            // Le boss a le temps de tomber.
            yield return new WaitForSeconds(1.2f);
            // Le joueur s'est avancé sur la case : l'escalier sort à côté de lui.
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null && player.Cell == cell)
                for (var d = 0; d < Grid.Dirs.Length; d++)
                {
                    var n = cell + Grid.Dirs[d];
                    if (maze.CanStep(cell, d) && GridEntity.At<GridEntity>(n) == null)
                    {
                        cell = n;
                        break;
                    }
                }
            var stairs = Spawn(stairsPrefab, cell, "Escalier");
            if (stairs != null)
                stairs.Dungeon = dungeon;
        }

        /// <summary>Un élément (« monstre », « piege », « tresor ») sur cette case. Null pour un élément pas encore géré.</summary>
        public GridEntity SpawnElement(string element, Vector2Int cell)
        {
            switch (element)
            {
                case "monstre":
                    return SpawnSlime(cell, (Slime.Variant)_rng.Next(3));
                case "piege":
                    return Spawn(spikeTrapPrefab, cell, "Dalle");
                case "tresor":
                    var chest = Spawn(chestPrefab, cell, "Coffre");
                    if (chest != null)
                        chest.Deck = dungeon != null ? dungeon.Deck : null;
                    return chest;
                case "boss":
                    var boss = Spawn(werewolfPrefab, cell, "LoupGarou");
                    boss?.ScaleForFloor(Floor);
                    return boss;
                default:
                    Debug.LogWarning($"Spawner : élément inconnu « {element} ».", this);
                    return null;
            }
        }

        public Slime SpawnSlime(Vector2Int cell, Slime.Variant variant)
        {
            var slime = Spawn(slimePrefab, cell, "Slime");
            if (slime != null)
            {
                slime.SetVariant(variant);
                slime.ScaleForFloor(Floor);
            }
            return slime;
        }

        T Spawn<T>(T prefab, Vector2Int cell, string label) where T : GridEntity
        {
            if (prefab == null)
            {
                Debug.LogWarning($"Spawner : pas de prefab pour {label}.", this);
                return null;
            }
            var maze = dungeon != null ? dungeon.Maze : FindAnyObjectByType<Maze>();
            var entity = Instantiate(prefab, transform);
            entity.name = $"{label}_{cell.x}_{cell.y}";
            entity.Place(maze, cell);
            Spawned?.Invoke(entity);
            return entity;
        }
    }
}
