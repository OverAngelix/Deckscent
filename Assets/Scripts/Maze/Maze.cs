using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Construit un labyrinthe sur grille à partir d'un plan en texte, en posant des prefabs.
    ///
    /// Légende du plan :
    ///   #  mur
    ///   .  sol
    ///   P  départ du joueur
    ///   T  torche accrochée au premier mur voisin (nord, est, sud puis ouest)
    ///
    /// Chaque case de sol reçoit un sol, un plafond et un pan de mur sur chaque côté qui donne
    /// sur un mur : un plan peut être « plein de roche » autour des couloirs sans coût.
    ///
    /// Le labyrinthe se construit aussi dans l'éditeur et se reconstruit quand un réglage change.
    /// Les objets générés ne sont pas enregistrés dans la scène (HideFlags.DontSave) : seul le
    /// plan l'est. Pour régler l'aspect d'un mur, d'un sol ou d'une torche, ouvrir son prefab.
    /// </summary>
    [ExecuteAlways]
    public class Maze : MonoBehaviour
    {
        [Serializable]
        public class PieceSet
        {
            [Tooltip("Pans de mur. Un est tiré pour chaque pan, toujours le même pour une position donnée.")]
            public GameObject[] walls = Array.Empty<GameObject>();
            [Tooltip("Sol d'une case.")]
            public GameObject floor;
            [Tooltip("Plafond d'une case (visible seulement par-dessous).")]
            public GameObject ceiling;

            /// <summary>Vrai si toutes les pièces sont là, maillages compris (pack Synty importé).</summary>
            public bool IsUsable()
            {
                if (walls == null || walls.Length == 0)
                    return false;
                foreach (var wall in walls)
                    if (!HasMeshes(wall))
                        return false;
                return HasMeshes(floor) && HasMeshes(ceiling);
            }

            static bool HasMeshes(GameObject prefab)
            {
                if (prefab == null)
                    return false;
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 0)
                    return false;
                foreach (var filter in filters)
                    if (filter.sharedMesh == null)
                        return false;
                return true;
            }
        }

        [Header("Grille")]
        [Tooltip("Côté d'une case, en mètres. Les prefabs de pièces sont faits pour 3 m : ils sont mis à l'échelle sinon.")]
        [SerializeField, Min(0.5f)] float cellSize = 3f;
        [Tooltip("Hauteur des murs, en mètres. Les prefabs sont faits pour 3,5 m.")]
        [SerializeField, Min(0.5f)] float wallHeight = 3.5f;

        [Header("Plan")]
        [Tooltip("# mur, . sol, P départ, T torche. En jeu, le plateau de tuiles le remplacera à chaque pose.")]
        [SerializeField, TextArea(8, 30)] string layout =
            "###############\n" +
            "#T....#.......#\n" +
            "#.###.#.#####.#\n" +
            "#.#...P.#...#.#\n" +
            "#.#.###.#.#.#.#\n" +
            "#...#T....#...#\n" +
            "###.#######.###\n" +
            "#.....T.......#\n" +
            "###############";

        [Header("Pièces")]
        [Tooltip("Décor Synty (pack POLYGON Dungeon). Si une pièce manque (pack non importé), le jeu de secours est utilisé.")]
        [SerializeField] PieceSet decor = new PieceSet();
        [Tooltip("Blocs gris, sans asset externe : toujours disponibles.")]
        [SerializeField] PieceSet fallback = new PieceSet();
        [Tooltip("Décoché : blocs gris même si le décor est là.")]
        [SerializeField] bool useDecor = true;
        [Tooltip("Torche posée sur les cases T, accrochée au mur.")]
        [SerializeField] GameObject torchPrefab;
        [Tooltip("Hauteur de la torche sur le mur, en mètres.")]
        [SerializeField] float torchHeight = 2.1f;

        /// <summary>Le plan vient d'être reconstruit.</summary>
        public event Action Rebuilt;

        public float CellSize => cellSize;
        public float WallHeight => wallHeight;
        public Vector2Int StartCell { get; private set; } = Vector2Int.one;

        const float PrefabCellSize = 3f;
        const float PrefabWallHeight = 3.5f;
        const string GeneratedName = "Généré";

        readonly List<string> _rows = new List<string>();
        string _planOverride = "";
        Transform _generated;
#if UNITY_EDITOR
        bool _rebuildQueued;
#endif

        // Pas de nettoyage dans OnDisable : les objets générés sont des enfants, détruits avec le
        // labyrinthe, et Build() remplace ceux d'avant (après une recompilation par exemple).
        void OnEnable() => Build();

#if UNITY_EDITOR
        // Reconstruction différée : on ne peut pas créer ni détruire d'objets dans OnValidate,
        // et plusieurs réglages modifiés d'un coup ne reconstruisent qu'une fois.
        void OnValidate()
        {
            if (_rebuildQueued)
                return;
            _rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _rebuildQueued = false;
                if (this != null && isActiveAndEnabled)
                    Build();
            };
        }
#endif

        /// <summary>Remplace le plan (sans toucher à `layout`) et reconstruit. Vide = revenir à `layout`.</summary>
        public void SetPlan(string plan)
        {
            _planOverride = plan ?? "";
            Build();
        }

        public bool IsWalkable(Vector2Int cell) => GetCell(cell) != '#';

        /// <summary>Caractère du plan pour une case (« # » hors du plan).</summary>
        public char GetCell(Vector2Int cell)
        {
            if (cell.y < 0 || cell.y >= _rows.Count)
                return '#';
            var row = _rows[cell.y];
            if (cell.x < 0 || cell.x >= row.Length)
                return '#';
            return row[cell.x];
        }

        /// <summary>Taille du plan en cases (ligne la plus longue, nombre de lignes).</summary>
        public Vector2Int GridSize
        {
            get
            {
                var width = 0;
                foreach (var row in _rows)
                    width = Mathf.Max(width, row.Length);
                return new Vector2Int(width, _rows.Count);
            }
        }

        /// <summary>Centre d'une case, au niveau du sol, en coordonnées monde.</summary>
        public Vector3 CellToWorld(Vector2Int cell) =>
            transform.TransformPoint(CellToLocal(cell));

        Vector3 CellToLocal(Vector2Int cell) =>
            new Vector3((cell.x + 0.5f) * cellSize, 0f, -(cell.y + 0.5f) * cellSize);

        /// <summary>Case la plus proche d'un point du monde.</summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(-local.z / cellSize));
        }

        public void Build()
        {
            ClearGenerated();
            ParsePlan();
            if (_rows.Count == 0)
                return;

            var pieces = useDecor && decor.IsUsable() ? decor : fallback;
            if (useDecor && pieces == fallback && Application.isPlaying)
                Debug.LogWarning("Labyrinthe : décor Synty introuvable, blocs gris utilisés (pack non importé ?).");
            if (!pieces.IsUsable())
            {
                Debug.LogError("Labyrinthe : le jeu de pièces de secours est incomplet.", this);
                return;
            }

            var root = new GameObject(GeneratedName);
            _generated = root.transform;
            _generated.SetParent(transform, false);

            var size = GridSize;
            for (var y = 0; y < size.y; y++)
            for (var x = 0; x < _rows[y].Length; x++)
            {
                var cell = new Vector2Int(x, y);
                var c = _rows[y][x];
                if (c == '#')
                    continue;
                if (c == 'P')
                    StartCell = cell;
                AddCellPieces(cell, pieces);
                if (c == 'T')
                    AddTorch(cell);
            }

            if (!Application.isPlaying)
                MarkDontSave(root);
            Rebuilt?.Invoke();
        }

        void ParsePlan()
        {
            _rows.Clear();
            var plan = string.IsNullOrEmpty(_planOverride) ? layout : _planOverride;
            if (string.IsNullOrEmpty(plan))
                return;
            foreach (var line in plan.Split('\n'))
            {
                var row = line.Trim();
                if (row.Length > 0)
                    _rows.Add(row);
            }
        }

        /// <summary>Sol, plafond, et un pan de mur sur chaque côté fermé.</summary>
        void AddCellPieces(Vector2Int cell, PieceSet pieces)
        {
            var center = CellToLocal(cell);
            var flat = new Vector3(cellSize / PrefabCellSize, 1f, cellSize / PrefabCellSize);
            Spawn(pieces.floor, center, Quaternion.identity, flat, $"Sol_{cell.x}_{cell.y}");
            Spawn(pieces.ceiling, center + Vector3.up * wallHeight, Quaternion.identity, flat,
                $"Plafond_{cell.x}_{cell.y}");

            var wallScale = new Vector3(cellSize / PrefabCellSize, wallHeight / PrefabWallHeight, 1f);
            for (var i = 0; i < Grid.Dirs.Length; i++)
            {
                var d = Grid.Dirs[i];
                if (IsWalkable(cell + d))
                    continue;
                var outward = Grid.ToWorldDir(d);
                // Le prefab de mur est posé au milieu du bord, sa face (+Z) tournée vers la case.
                var position = center + outward * (cellSize * 0.5f);
                var rotation = Quaternion.LookRotation(-outward, Vector3.up);
                var prefab = pieces.walls[Variant(cell, i, pieces.walls.Length)];
                Spawn(prefab, position, rotation, wallScale, $"Mur_{cell.x}_{cell.y}_{"NESO"[i]}");
            }
        }

        void AddTorch(Vector2Int cell)
        {
            if (torchPrefab == null)
                return;
            // Accrochée au premier mur voisin trouvé.
            for (var i = 0; i < Grid.Dirs.Length; i++)
            {
                var d = Grid.Dirs[i];
                if (IsWalkable(cell + d))
                    continue;
                var outward = Grid.ToWorldDir(d);
                var position = CellToLocal(cell) + outward * (cellSize * 0.5f) + Vector3.up * torchHeight;
                Spawn(torchPrefab, position, Quaternion.LookRotation(-outward, Vector3.up), Vector3.one,
                    $"Torche_{cell.x}_{cell.y}");
                return;
            }
        }

        void Spawn(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3 scale, string objectName)
        {
            var instance = Instantiate(prefab, _generated);
            instance.name = objectName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);
        }

        /// <summary>Variante tirée d'après la position : la même à chaque reconstruction.</summary>
        static int Variant(Vector2Int cell, int side, int count)
        {
            unchecked
            {
                var h = cell.x * 73856093 ^ cell.y * 19349663 ^ side * 83492791;
                return (int)((uint)h % (uint)count);
            }
        }

        void ClearGenerated()
        {
            if (_generated == null)
                _generated = transform.Find(GeneratedName);
            if (_generated == null)
                return;
            if (Application.isPlaying)
                Destroy(_generated.gameObject);
            else
                DestroyImmediate(_generated.gameObject);
            _generated = null;
        }

        static void MarkDontSave(GameObject go)
        {
            go.hideFlags = HideFlags.DontSave;
            foreach (Transform child in go.transform)
                MarkDontSave(child.gameObject);
        }
    }
}
