using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Le plateau du donjon : les tuiles posées et les règles de pose (logique pure, sans Unity
    /// autour : testable en EditMode). Une grille de <see cref="Size"/> emplacements ; chaque
    /// tuile y occupe 3 × 3 cases du labyrinthe. <see cref="ToLayout"/> donne le plan texte que le
    /// labyrinthe sait construire (<see cref="Maze.SetPlan"/>).
    /// </summary>
    public class DungeonBoard
    {
        /// <summary>Taille d'une tuile, en cases du labyrinthe.</summary>
        public const int TileCells = 3;

        public readonly struct StarterTile
        {
            public readonly Vector2Int Pos;
            public readonly string Shape;
            public readonly int Turns;
            public readonly bool Torch;

            public StarterTile(int x, int y, string shape, int turns, bool torch)
            {
                Pos = new Vector2Int(x, y);
                Shape = shape;
                Turns = turns;
                Torch = torch;
            }
        }

        public sealed class Starter
        {
            public string Name;
            /// <summary>La PREMIÈRE tuile est celle où apparaît le joueur. Les suivantes se raccordent aux précédentes.</summary>
            public StarterTile[] Tiles;
        }

        /// <summary>Les départs possibles, pour un plateau de 9 × 9 (emplacements de 0 à 8).</summary>
        public static readonly Starter[] Starters =
        {
            new Starter { Name = "Carrefour central", Tiles = new[] { new StarterTile(4, 4, "carrefour", 0, false) } },
            new Starter { Name = "Couloir nord-sud", Tiles = new[] { new StarterTile(4, 4, "couloir", 0, false) } },
            new Starter { Name = "Coin nord-ouest", Tiles = new[] { new StarterTile(1, 1, "virage", 0, true) } },
            new Starter { Name = "Cul-de-sac du sud", Tiles = new[] { new StarterTile(4, 7, "impasse", 2, true) } },
            new Starter { Name = "Fourche de l'est", Tiles = new[] { new StarterTile(7, 4, "t", 1, true) } },
            new Starter { Name = "Grande galerie", Tiles = new[]
            {
                new StarterTile(4, 4, "couloir", 1, false), new StarterTile(3, 4, "couloir", 1, true), new StarterTile(5, 4, "couloir", 1, true),
            } },
            new Starter { Name = "Grande croix", Tiles = new[]
            {
                new StarterTile(4, 4, "carrefour", 0, false), new StarterTile(4, 3, "couloir", 0, true), new StarterTile(5, 4, "couloir", 1, false),
                new StarterTile(4, 5, "couloir", 0, true), new StarterTile(3, 4, "couloir", 1, false),
            } },
            new Starter { Name = "Coin sud-est", Tiles = new[] { new StarterTile(7, 7, "virage", 2, true) } },
            new Starter { Name = "Zigzag", Tiles = new[]
            {
                new StarterTile(2, 2, "virage", 0, false), new StarterTile(3, 2, "virage", 1, true), new StarterTile(3, 3, "virage", 3, false),
                new StarterTile(4, 3, "couloir", 1, true),
            } },
            new Starter { Name = "Bord ouest", Tiles = new[] { new StarterTile(0, 4, "t", 3, true) } },
        };

        /// <summary>Le plateau vient de changer (pose, nouvelle partie).</summary>
        public event Action TilesChanged;
        /// <summary>Une tuile vient d'être posée par le joueur (pas les tuiles du départ).</summary>
        public event Action<Vector2Int, TileCard> TilePlaced;
        /// <summary>Le plateau vient d'être vidé pour une nouvelle partie.</summary>
        public event Action BoardReset;

        public Vector2Int Size { get; }
        /// <summary>Emplacement de la tuile où apparaît le joueur.</summary>
        public Vector2Int StartTile { get; private set; }
        public string StarterName { get; private set; } = "";

        /// <summary>Emplacement → carte. Une salle est inscrite sur chacun des emplacements qu'elle couvre.</summary>
        readonly Dictionary<Vector2Int, TileCard> _tiles = new Dictionary<Vector2Int, TileCard>();
        /// <summary>Emplacement → ancre de la tuile qui le couvre.</summary>
        readonly Dictionary<Vector2Int, Vector2Int> _origins = new Dictionary<Vector2Int, Vector2Int>();

        public DungeonBoard(Vector2Int size)
        {
            Size = size;
        }

        /// <summary>Vide le plateau et pose les tuiles d'un départ (hors liste = tiré avec `rng`).</summary>
        public void NewGame(int index, System.Random rng = null)
        {
            if (index < 0 || index >= Starters.Length)
                index = (rng ?? new System.Random()).Next(Starters.Length);
            var starter = Starters[index];
            StarterName = starter.Name;
            _tiles.Clear();
            _origins.Clear();
            BoardReset?.Invoke();
            foreach (var t in starter.Tiles)
                Put(t.Pos, new TileCard(t.Shape, t.Turns, t.Torch));
            StartTile = starter.Tiles[0].Pos;
            if (OpenExits().Count == 0)
                Debug.LogWarning($"Le départ « {StarterName} » n'a aucun passage vers un emplacement libre : rien ne pourra y être construit.");
            TilesChanged?.Invoke();
        }

        /// <summary>Emplacements libres vers lesquels mène au moins un passage. Vide = plus aucune pose possible.</summary>
        public List<Vector2Int> OpenExits()
        {
            var exits = new List<Vector2Int>();
            foreach (var kv in _tiles)
            {
                var offset = kv.Key - TileOrigin(kv.Key);
                foreach (var dir in TileCard.Dirs)
                {
                    var next = kv.Key + TileCard.Offset(dir);
                    if (kv.Value.HasDoor(offset, dir) && IsInside(next) && !HasTile(next) && !exits.Contains(next))
                        exits.Add(next);
                }
            }
            return exits;
        }

        public bool IsInside(Vector2Int pos) => pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y;

        public bool HasTile(Vector2Int pos) => _tiles.ContainsKey(pos);

        /// <summary>La carte posée sur cet emplacement (une salle en couvre plusieurs), ou null.</summary>
        public TileCard GetTile(Vector2Int pos) => _tiles.TryGetValue(pos, out var c) ? c : null;

        /// <summary>Ancre de la tuile qui couvre cet emplacement.</summary>
        public Vector2Int TileOrigin(Vector2Int pos) => _origins.TryGetValue(pos, out var o) ? o : pos;

        /// <summary>Emplacements occupés (une salle en compte plusieurs).</summary>
        public IEnumerable<Vector2Int> TilePositions => _tiles.Keys;

        /// <summary>Les 3 × 3 caractères posés sur cet emplacement (« P » au centre du départ).</summary>
        public string[] CellsAt(Vector2Int pos)
        {
            var card = GetTile(pos);
            if (card == null)
                return new[] { "###", "###", "###" };
            var rows = card.CellsForSlot(pos - TileOrigin(pos), pos == StartTile ? 'P' : '\0');
            if (card.IsRoom)
                return rows;
            // Un passage tourné vers l'extérieur du plateau ne mène nulle part : c'est un mur.
            for (var i = 0; i < TileCard.Dirs.Length; i++)
            {
                if (IsInside(pos + TileCard.Offset(TileCard.Dirs[i])))
                    continue;
                var c = Vector2Int.one + TileCard.Offset(TileCard.Dirs[i]);
                var row = rows[c.y].ToCharArray();
                row[c.x] = '#';
                rows[c.y] = new string(row);
            }
            return rows;
        }

        /// <summary>
        /// Vrai si la tuile sur `pos` a un passage ou une porte côté `dir` qui mène dans le plateau
        /// (une ouverture tournée vers l'extérieur ne compte pas : c'est un mur).
        /// </summary>
        public bool HasOpening(Vector2Int pos, int dir)
        {
            var card = GetTile(pos);
            return card != null && IsInside(pos + TileCard.Offset(dir)) && card.HasDoor(pos - TileOrigin(pos), dir);
        }

        /// <summary>
        /// Sorties encore libres : (emplacement, direction) d'une ouverture qui mène à un emplacement
        /// vide du plateau. Une sortie raccordée ou tournée vers l'extérieur n'y est pas.
        /// </summary>
        public List<(Vector2Int slot, int dir)> FreeOpenings()
        {
            var free = new List<(Vector2Int, int)>();
            foreach (var pos in _tiles.Keys)
                foreach (var dir in TileCard.Dirs)
                    if (HasOpening(pos, dir) && !HasTile(pos + TileCard.Offset(dir)))
                        free.Add((pos, dir));
            return free;
        }

        /// <summary>
        /// Vérifie si la carte peut être posée avec son ancre sur cet emplacement. Renvoie "" si c'est
        /// autorisé, sinon la raison du refus (affichée sur la carte).
        ///
        /// Règles volontairement permissives : tous les emplacements dans le plateau et libres, la
        /// tuile touche le labyrinthe et s'y raccorde par au moins un passage ou une porte. Un passage
        /// qui donne sur un mur, sur le bord ou sur un emplacement vide devient un cul-de-sac : c'est
        /// au joueur de choisir s'il se bouche le chemin.
        /// </summary>
        public string CheckPlacement(Vector2Int pos, TileCard card)
        {
            foreach (var offset in card.Slots)
                if (!IsInside(pos + offset))
                    return "refus.hors_plateau";
            foreach (var offset in card.Slots)
                if (HasTile(pos + offset))
                    return "refus.occupe";
            var touches = false;
            var connected = false;
            foreach (var offset in card.Slots)
            {
                foreach (var dir in TileCard.Dirs)
                {
                    var step = TileCard.Offset(dir);
                    if (card.HasSlot(offset + step))
                        continue; // intérieur de la salle
                    var next = pos + offset + step;
                    var neighbor = GetTile(next);
                    if (neighbor == null)
                        continue;
                    touches = true;
                    if (card.HasDoor(offset, dir) && neighbor.HasDoor(next - TileOrigin(next), TileCard.Opposite(dir)))
                        connected = true;
                }
            }
            if (!touches)
                return "refus.isole";
            if (!connected)
                return "refus.sans_passage";
            return "";
        }

        public bool CanPlace(Vector2Int pos, TileCard card) => CheckPlacement(pos, card) == "";

        /// <summary>Vrai si la carte peut être posée quelque part, dans au moins une orientation.</summary>
        public bool HasAnyPlacement(TileCard card)
        {
            var turned = card.Copy();
            for (var turn = 0; turn < 4; turn++)
            {
                for (var y = 0; y < Size.y; y++)
                    for (var x = 0; x < Size.x; x++)
                        if (CanPlace(new Vector2Int(x, y), turned))
                            return true;
                turned.Rotate(1);
            }
            return false;
        }

        /// <summary>Pose la carte (ancre sur `pos`) si les règles l'autorisent. Vrai si posée.</summary>
        public bool Place(Vector2Int pos, TileCard card)
        {
            if (!CanPlace(pos, card))
                return false;
            Put(pos, card);
            TilesChanged?.Invoke();
            TilePlaced?.Invoke(pos, card);
            return true;
        }

        /// <summary>Inscrit la carte sur tous les emplacements qu'elle couvre, sans vérifier.</summary>
        void Put(Vector2Int pos, TileCard card)
        {
            foreach (var offset in card.Slots)
            {
                _tiles[pos + offset] = card;
                _origins[pos + offset] = pos;
            }
        }

        /// <summary>
        /// Cases du labyrinthe où le contenu d'une tuile posée peut apparaître : son sol, sans les
        /// portes d'une salle (pour ne pas les boucher).
        /// </summary>
        public List<Vector2Int> TileFloorCells(Vector2Int pos)
        {
            var cells = new List<Vector2Int>();
            var card = GetTile(pos);
            if (card == null)
            {
                cells.Add(TileCenterCell(pos));
                return cells;
            }
            var origin = TileOrigin(pos);
            foreach (var offset in card.Slots)
            {
                var slot = origin + offset;
                var rows = CellsAt(slot);
                for (var ly = 0; ly < 3; ly++)
                    for (var lx = 0; lx < 3; lx++)
                    {
                        if (rows[ly][lx] == '#' || card.IsDoorCell(offset, lx, ly))
                            continue;
                        cells.Add(slot * TileCells + new Vector2Int(lx, ly));
                    }
            }
            return cells;
        }

        /// <summary>Case du labyrinthe au centre d'un emplacement.</summary>
        public static Vector2Int TileCenterCell(Vector2Int pos) => pos * TileCells + Vector2Int.one;

        /// <summary>
        /// Murs fins des salles posées, en cases du labyrinthe : (case, indice de direction dans
        /// <see cref="Grid.Dirs"/>). À passer à <see cref="Maze.SetPlan"/> avec <see cref="ToLayout"/>.
        /// </summary>
        public List<(Vector2Int cell, int dir)> EdgeWalls()
        {
            var walls = new List<(Vector2Int, int)>();
            foreach (var kv in _tiles)
            {
                if (!kv.Value.IsRoom)
                    continue;
                var offset = kv.Key - TileOrigin(kv.Key);
                foreach (var (cell, dir) in kv.Value.EdgeWalls(offset))
                    walls.Add((kv.Key * TileCells + cell, Array.IndexOf(TileCard.Dirs, dir)));
                // Une porte tournée vers l'extérieur du plateau ne mène nulle part : on la mure.
                foreach (var dir in TileCard.Dirs)
                    if (kv.Value.HasDoor(offset, dir) && !kv.Value.HasSlot(offset + TileCard.Offset(dir))
                        && !IsInside(kv.Key + TileCard.Offset(dir)))
                        walls.Add((kv.Key * TileCells + Vector2Int.one + TileCard.Offset(dir), Array.IndexOf(TileCard.Dirs, dir)));
            }
            return walls;
        }

        /// <summary>Plan texte complet du labyrinthe (même légende que Maze). Emplacements vides = roche.</summary>
        public string ToLayout()
        {
            var grid = RockGrid(Size);
            foreach (var pos in _tiles.Keys)
                Stamp(grid, pos, CellsAt(pos));
            return GridToText(grid);
        }

        /// <summary>
        /// Plan des départs côte à côte (aperçu dans l'éditeur) : `columns` plateaux par ligne,
        /// séparés de `gap` tuiles de roche.
        /// </summary>
        public static string AllStartersLayout(Vector2Int boardSize, int columns = 5, int gap = 2)
        {
            var stride = boardSize + Vector2Int.one * gap;
            var rows = (Starters.Length + columns - 1) / columns;
            var grid = RockGrid(new Vector2Int(columns * stride.x - gap, rows * stride.y - gap));
            for (var i = 0; i < Starters.Length; i++)
            {
                var origin = new Vector2Int(i % columns, i / columns) * stride;
                for (var t = 0; t < Starters[i].Tiles.Length; t++)
                {
                    var tile = Starters[i].Tiles[t];
                    var card = new TileCard(tile.Shape, tile.Turns, tile.Torch);
                    Stamp(grid, origin + tile.Pos, card.CellsForSlot(Vector2Int.zero, t == 0 && i == 0 ? 'P' : '\0'));
                }
            }
            return GridToText(grid);
        }

        static char[,] RockGrid(Vector2Int tiles)
        {
            var grid = new char[tiles.y * TileCells, tiles.x * TileCells];
            for (var y = 0; y < grid.GetLength(0); y++)
                for (var x = 0; x < grid.GetLength(1); x++)
                    grid[y, x] = '#';
            return grid;
        }

        static void Stamp(char[,] grid, Vector2Int pos, string[] cells)
        {
            for (var dy = 0; dy < TileCells; dy++)
                for (var dx = 0; dx < TileCells; dx++)
                    grid[pos.y * TileCells + dy, pos.x * TileCells + dx] = cells[dy][dx];
        }

        static string GridToText(char[,] grid)
        {
            var sb = new StringBuilder();
            for (var y = 0; y < grid.GetLength(0); y++)
            {
                if (y > 0)
                    sb.Append('\n');
                for (var x = 0; x < grid.GetLength(1); x++)
                    sb.Append(grid[y, x]);
            }
            return sb.ToString();
        }
    }
}
