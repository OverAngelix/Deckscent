using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Fait apparaître le contenu des tuiles posées, tel que la carte l'annonçait
    /// (<see cref="TileCard.Content"/>, tiré par la pioche) : un élément au plus pour un couloir,
    /// plusieurs pour une salle (<see cref="TileCard.SpawnList"/>). Éléments : slime (« monstre »),
    /// dalle à pics (« piege »), coffre (« tresor »). Le boss est à venir. Les tuiles du départ et
    /// les cartes « vide » restent vides.
    ///
    /// Chaque élément est une instance de son prefab, enfant de cet objet. Le placement dans la
    /// tuile (quelle case de sol), la sorte de slime et le décalage des dalles sont tirés avec un
    /// générateur à part : le tirage des cartes n'en dépend pas.
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

        [Header("Réglages")]
        [Tooltip("Décoché : plus rien n'apparaît (tests, essais).")]
        [SerializeField] bool spawnOnPlace = true;
        [Tooltip("Graine du placement. 0 = différent à chaque partie.")]
        [SerializeField] int seed;

        /// <summary>Un élément vient d'apparaître.</summary>
        public event System.Action<GridEntity> Spawned;

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
            if (_board != null)
                _board.TilePlaced -= OnTilePlaced;
        }

        /// <summary>Nouveau plateau : on vide, on suit ses poses, et on remplit les tuiles déjà là (donjon de démo).</summary>
        void OnGameStarted()
        {
            Clear();
            if (_board != null)
                _board.TilePlaced -= OnTilePlaced;
            _board = dungeon.Board;
            if (_board == null)
                return;
            _board.TilePlaced += OnTilePlaced;
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

        /// <summary>Retire tout ce qui est apparu.</summary>
        public void Clear()
        {
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
            for (var i = 0; i < Mathf.Min(elements.Length, cells.Count); i++)
            {
                var entity = SpawnElement(elements[i], cells[i]);
                if (entity == null)
                    continue;
                if (entity is SpikeTrap trap)
                    trap.PhaseOffset = trapPhase++ % trap.Period;
                created.Add(entity);
            }
            return created;
        }

        /// <summary>Un élément (« monstre », « piege », « tresor ») sur cette case. Null pour un élément pas encore géré (le boss).</summary>
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
                    Debug.LogWarning("Spawner : le boss n'existe pas encore, rien n'apparaît.", this);
                    return null;
                default:
                    Debug.LogWarning($"Spawner : élément inconnu « {element} ».", this);
                    return null;
            }
        }

        public Slime SpawnSlime(Vector2Int cell, Slime.Variant variant)
        {
            var slime = Spawn(slimePrefab, cell, "Slime");
            if (slime != null)
                slime.SetVariant(variant);
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
