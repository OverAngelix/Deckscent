using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Une carte de la main = une tuile à poser sur le plateau.
    ///
    /// Une tuile occupe un ou plusieurs **emplacements** du plateau (<see cref="Slots"/>), chacun
    /// de 3 × 3 cases du labyrinthe. Le premier emplacement est l'ancre, toujours en (0, 0) :
    /// c'est lui qui est sous la souris quand on pose la carte. Décalages en emplacements :
    /// x vers l'est, y vers le sud.
    ///
    /// Couloir (1 emplacement) : coins en mur, centre en sol, un passage possible au milieu de
    /// chaque côté.
    ///
    ///   # N #
    ///   O . E      N, E, S, O : « . » si le côté est ouvert, « # » sinon
    ///   # S #
    ///
    /// Salle (plusieurs emplacements) : une vraie pièce, tout l'emplacement est du sol. Le pourtour
    /// est fermé par des **murs fins** (sur les bords des cases, <see cref="EdgeWalls"/>), percés
    /// au milieu de certains côtés par des **portes** (tirées au hasard à la pioche,
    /// <see cref="RandomDoors"/>). La salle garde sa forme quoi qu'on pose autour : un couloir
    /// qui arrive face à un mur sans porte finit en cul-de-sac. Une salle « à piliers » garde un
    /// pilier à chaque jonction de 2 × 2 emplacements.
    ///
    /// Ouvertures : masques de bits N = 1, E = 2, S = 4, O = 8, par emplacement (<see cref="Doors"/>).
    /// Un quart de tour horaire décale les bits (N → E → S → O).
    /// </summary>
    public sealed class TileCard
    {
        public const int N = 1, E = 2, S = 4, W = 8;
        /// <summary>Ordre de parcours des côtés : nord, est, sud, ouest.</summary>
        public static readonly int[] Dirs = { N, E, S, W };
        public const int MaxDanger = 3;

        public sealed class ShapeDef
        {
            /// <summary>Clé de traduction du nom (« forme. » + identifiant), voir Loc.</summary>
            public string NameKey;
            public bool Room;
            /// <summary>Couloir : ouvertures avant rotation.</summary>
            public int Openings;
            /// <summary>Salle : emplacements avant rotation, le premier en (0, 0).</summary>
            public Vector2Int[] Slots;
            /// <summary>Salle : piliers aux jonctions de 2 × 2 emplacements.</summary>
            public bool Pillars;
        }

        static Vector2Int V(int x, int y) => new Vector2Int(x, y);

        /// <summary>
        /// Formes de tuiles, par identifiant. L'ordre sert de référence (pioche, doc). Les salles
        /// sont variées (demande de l'utilisateur : « plus de salles, et variées »).
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, ShapeDef>> ShapeList = new List<KeyValuePair<string, ShapeDef>>
        {
            Shape("impasse", new ShapeDef { Openings = S }),
            Shape("couloir", new ShapeDef { Openings = N | S }),
            Shape("virage", new ShapeDef { Openings = E | S }),
            Shape("t", new ShapeDef { Openings = E | S | W }),
            Shape("carrefour", new ShapeDef { Openings = N | E | S | W }),
            Shape("salle", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(1, 0) } }),
            Shape("galerie", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(1, 0), V(2, 0) } }),
            Shape("longue_galerie", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(1, 0), V(2, 0), V(3, 0) } }),
            Shape("salle_l", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(0, 1), V(1, 1) } }),
            Shape("salle_t", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(1, 0), V(2, 0), V(1, 1) } }),
            Shape("salle_z", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(1, 0), V(1, 1), V(2, 1) } }),
            Shape("salle_u", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(0, 1), V(1, 1), V(2, 1), V(2, 0) } }),
            Shape("salle_croix", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(0, 1), V(-1, 1), V(1, 1), V(0, 2) } }),
            Shape("grande_salle", new ShapeDef { Room = true, Slots = new[] { V(0, 0), V(1, 0), V(0, 1), V(1, 1) } }),
            Shape("salle_piliers", new ShapeDef { Room = true, Pillars = true, Slots = new[] { V(0, 0), V(1, 0), V(2, 0), V(0, 1), V(1, 1), V(2, 1) } }),
            Shape("crypte", new ShapeDef { Room = true, Pillars = true, Slots = new[] { V(0, 0), V(1, 0), V(2, 0), V(0, 1), V(1, 1), V(2, 1), V(0, 2), V(1, 2), V(2, 2) } }),
        };

        static KeyValuePair<string, ShapeDef> Shape(string id, ShapeDef def)
        {
            def.NameKey = "forme." + id;
            return new KeyValuePair<string, ShapeDef>(id, def);
        }
        static readonly Dictionary<string, ShapeDef> Shapes = ShapeList.ToDictionary(kv => kv.Key, kv => kv.Value);

        public static bool HasShape(string id) => Shapes.ContainsKey(id);
        public static ShapeDef GetShape(string id) => Shapes[id];

        public sealed class ContentDef
        {
            public int Danger;
            public bool Room;
            /// <summary>Couloir : ce qui apparaît (un élément au plus).</summary>
            public string[] Spawn = Array.Empty<string>();
            /// <summary>Petite salle (2 emplacements).</summary>
            public string[] Small = Array.Empty<string>();
            /// <summary>Salle moyenne (3 ou 4 emplacements).</summary>
            public string[] Medium = Array.Empty<string>();
            /// <summary>Grande salle (5 emplacements ou plus).</summary>
            public string[] Large = Array.Empty<string>();
        }

        static string[] L(params string[] items) => items;
        const string M = "monstre", P = "piege", T = "tresor";

        /// <summary>
        /// Contenus possibles et leur niveau de danger (0 à 3), le seul indice affiché sur la carte :
        /// le joueur ne sait pas exactement ce qu'il pose. Éléments : « monstre » (slime),
        /// « piege » (dalle à pics), « tresor » (coffre, le loot), « boss » (pas encore).
        ///
        /// Rythme voulu (façon donjon mystère) : un couloir porte au plus un élément ; une salle est
        /// rare mais **dense et dangereuse**, d'autant plus qu'elle est grande (2 à 7 éléments).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, ContentDef> Contents = new Dictionary<string, ContentDef>
        {
            ["vide"] = new ContentDef { Danger = 0 },
            ["tresor"] = new ContentDef { Danger = 0, Spawn = L(T) },
            ["piege"] = new ContentDef { Danger = 1, Spawn = L(P) },
            ["monstre"] = new ContentDef { Danger = 2, Spawn = L(M) },
            ["boss"] = new ContentDef { Danger = 3, Spawn = L("boss") },
            // Salle vide : une respiration, rare.
            ["salle_vide"] = new ContentDef { Danger = 0, Room = true },
            // Salle au trésor : du loot, à peine gardé.
            ["salle_tresor"] = new ContentDef { Danger = 1, Room = true, Small = L(T, T), Medium = L(T, T, M), Large = L(T, T, T, M, M) },
            ["salle_pieges"] = new ContentDef { Danger = 2, Room = true, Small = L(P, P), Medium = L(P, P, P, T), Large = L(P, P, P, P, T, T) },
            // Salle piégée : le trésor au milieu des pièges.
            ["salle_piegee"] = new ContentDef { Danger = 2, Room = true, Small = L(T, P, P), Medium = L(T, T, P, P, P), Large = L(T, T, P, P, P, M) },
            // Salle gardée : le trésor et ses gardiens.
            ["salle_gardee"] = new ContentDef { Danger = 3, Room = true, Small = L(T, M, M), Medium = L(T, T, M, M, M), Large = L(T, T, T, M, M, M, M) },
            // Repaire : des monstres, un peu de butin.
            ["salle_monstres"] = new ContentDef { Danger = 3, Room = true, Small = L(M, M, M), Medium = L(M, M, M, M, T), Large = L(M, M, M, M, M, M, T) },
        };

        public string ShapeId { get; }
        public bool Torch { get; set; }
        /// <summary>Ce qui apparaît dans la tuile à la pose : une clé de <see cref="Contents"/>.</summary>
        public string Content { get; set; } = "vide";
        /// <summary>Emplacements occupés, relatifs à l'ancre (le premier, toujours (0, 0)).</summary>
        public List<Vector2Int> Slots { get; private set; } = new List<Vector2Int> { Vector2Int.zero };
        /// <summary>Ouvertures : emplacement relatif → masque N / E / S / O des côtés ouverts.</summary>
        public Dictionary<Vector2Int, int> Doors { get; private set; } = new Dictionary<Vector2Int, int>();
        /// <summary>Taille de la tuile en emplacements (largeur, hauteur).</summary>
        public Vector2Int Footprint { get; private set; } = Vector2Int.one;

        /// <summary>Masque d'ouvertures de l'ancre (pratique pour un couloir).</summary>
        public int Openings => Doors.TryGetValue(Vector2Int.zero, out var m) ? m : 0;
        public bool IsRoom => Def.Room;
        /// <summary>Nom de la forme dans la langue en cours (clé Def.NameKey).</summary>
        public string DisplayName => Loc.T(Def.NameKey);
        public ShapeDef Def => Shapes[ShapeId];

        public TileCard(string shape = "couloir", int quarterTurns = 0, bool torch = false)
        {
            if (!Shapes.ContainsKey(shape))
                throw new ArgumentException("Forme de tuile inconnue : " + shape, nameof(shape));
            ShapeId = shape;
            if (Def.Room)
                Slots = new List<Vector2Int>(Def.Slots);
            else
                Doors[Vector2Int.zero] = Def.Openings;
            Torch = torch;
            Rotate(quarterTurns);
            // Une torche s'accroche à un mur du centre : impossible sur un carrefour.
            if (!IsRoom && CountOpenings(Openings) >= 4)
                Torch = false;
        }

        /// <summary>Niveau de danger affiché sur la carte (0 = rien à craindre… a priori).</summary>
        public int Danger => (Contents.TryGetValue(Content, out var c) ? c : Contents["vide"]).Danger;

        /// <summary>Ce qui apparaît dans la tuile à la pose (voir <see cref="Contents"/>).</summary>
        public string[] SpawnList()
        {
            var def = Contents.TryGetValue(Content, out var c) ? c : Contents["vide"];
            if (!def.Room)
                return def.Spawn;
            return Slots.Count <= 2 ? def.Small : Slots.Count <= 4 ? def.Medium : def.Large;
        }

        /// <summary>Copie indépendante (forme, orientation, portes, torche, contenu).</summary>
        public TileCard Copy()
        {
            var c = new TileCard(ShapeId) { Torch = Torch, Content = Content };
            c.Slots = new List<Vector2Int>(Slots);
            c.Doors = new Dictionary<Vector2Int, int>(Doors);
            c.Footprint = Footprint;
            return c;
        }

        /// <summary>Tourne la tuile d'un ou plusieurs quarts de tour (horaire si positif), autour de l'ancre.</summary>
        public void Rotate(int quarterTurns = 1)
        {
            for (var i = 0; i < Mod(quarterTurns, 4); i++)
            {
                Slots = Slots.Select(Rot).ToList();
                Doors = Doors.ToDictionary(kv => Rot(kv.Key), kv => RotateMask(kv.Value, 1));
            }
            Footprint = Bounds().size;
        }

        /// <summary>Quart de tour horaire d'un décalage (y vers le sud) : nord → est.</summary>
        static Vector2Int Rot(Vector2Int v) => new Vector2Int(-v.y, v.x);

        /// <summary>Rectangle des emplacements occupés (relatif à l'ancre).</summary>
        public RectInt Bounds()
        {
            var lo = Slots[0];
            var hi = Slots[0];
            foreach (var s in Slots)
            {
                lo = Vector2Int.Min(lo, s);
                hi = Vector2Int.Max(hi, s);
            }
            return new RectInt(lo, hi - lo + Vector2Int.one);
        }

        public bool HasSlot(Vector2Int offset) => Slots.Contains(offset);

        public bool HasDoor(Vector2Int offset, int dir) => Doors.TryGetValue(offset, out var m) && (m & dir) != 0;

        /// <summary>Ouverture de l'ancre (couloir) dans cette direction.</summary>
        public bool HasOpening(int dir) => HasDoor(Vector2Int.zero, dir);

        /// <summary>Côtés du pourtour : (emplacement, direction) dont le voisin n'est pas la salle.</summary>
        public List<(Vector2Int slot, int dir)> PerimeterSides()
        {
            var sides = new List<(Vector2Int, int)>();
            foreach (var s in Slots)
                foreach (var dir in Dirs)
                    if (!Slots.Contains(s + Offset(dir)))
                        sides.Add((s, dir));
            return sides;
        }

        /// <summary>Nombre maximal de portes : 3, et 4 pour les grandes salles (5 emplacements ou plus).</summary>
        public int MaxDoors => Slots.Count >= 5 ? 4 : 3;

        /// <summary>Tire les portes d'une salle : de 1 à <see cref="MaxDoors"/>, au milieu de côtés du pourtour.</summary>
        public void RandomDoors(System.Random rng)
        {
            var sides = PerimeterSides();
            for (var i = sides.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (sides[i], sides[j]) = (sides[j], sides[i]);
            }
            var count = rng.Next(1, Math.Min(MaxDoors, sides.Count) + 1);
            Doors = new Dictionary<Vector2Int, int>();
            for (var k = 0; k < count; k++)
                AddDoor(sides[k].slot, sides[k].dir);
        }

        /// <summary>Remplace les portes (tuiles fixes : donjon de démo, départs).</summary>
        public void SetDoors(IEnumerable<(Vector2Int slot, int dir)> doors)
        {
            Doors = new Dictionary<Vector2Int, int>();
            foreach (var (slot, dir) in doors)
                AddDoor(slot, dir);
        }

        void AddDoor(Vector2Int slot, int dir) => Doors[slot] = (Doors.TryGetValue(slot, out var m) ? m : 0) | dir;

        /// <summary>Nombre total d'ouvertures (portes ou passages).</summary>
        public int DoorCount => Doors.Values.Sum(CountOpenings);

        /// <summary>
        /// Les 3 lignes de 3 caractères de l'emplacement `offset` (légende du labyrinthe). `centerChar`
        /// remplace le centre de l'ancre (ex. « P » pour le départ).
        /// </summary>
        public string[] CellsForSlot(Vector2Int offset, char centerChar = '\0')
        {
            var rows = new string[3];
            for (var ly = 0; ly < 3; ly++)
            {
                var row = new char[3];
                for (var lx = 0; lx < 3; lx++)
                    row[lx] = CellChar(offset, lx - 1, ly - 1);
                rows[ly] = new string(row);
            }
            if (offset == Vector2Int.zero && centerChar != '\0')
                rows[1] = $"{rows[1][0]}{centerChar}{rows[1][2]}";
            else if (Torch && !IsRoom && offset == Vector2Int.zero)
                rows[1] = $"{rows[1][0]}T{rows[1][2]}";
            else if (Torch && IsRoom)
            {
                // Salle : la torche va sur la case du milieu d'un côté muré (le labyrinthe
                // l'accroche au premier mur voisin, ici le mur fin).
                var (slot, dir) = TorchSide();
                if (slot == offset && dir != 0)
                {
                    var c = Vector2Int.one + Offset(dir);
                    var row = rows[c.y].ToCharArray();
                    row[c.x] = 'T';
                    rows[c.y] = new string(row);
                }
            }
            return rows;
        }

        /// <summary>Plan complet de la tuile : Footprint × 3 cases, « » (espace) hors de la tuile.</summary>
        public string[] FullRows()
        {
            var b = Bounds();
            var grid = new char[b.height * 3, b.width * 3];
            for (var y = 0; y < b.height * 3; y++)
                for (var x = 0; x < b.width * 3; x++)
                    grid[y, x] = ' ';
            foreach (var s in Slots)
            {
                var cells = CellsForSlot(s);
                var o = (s - b.position) * 3;
                for (var dy = 0; dy < 3; dy++)
                    for (var dx = 0; dx < 3; dx++)
                        grid[o.y + dy, o.x + dx] = cells[dy][dx];
            }
            var rows = new string[b.height * 3];
            for (var y = 0; y < rows.Length; y++)
            {
                var row = new char[b.width * 3];
                for (var x = 0; x < row.Length; x++)
                    row[x] = grid[y, x];
                rows[y] = new string(row);
            }
            return rows;
        }

        /// <summary>
        /// Vrai si la case (lx, ly) de l'emplacement `offset` est une porte de salle (milieu d'un
        /// côté du pourtour qui a une porte) : on n'y pose rien.
        /// </summary>
        public bool IsDoorCell(Vector2Int offset, int lx, int ly)
        {
            var side = new Vector2Int(lx - 1, ly - 1);
            if (!IsRoom || (side.x != 0) == (side.y != 0))
                return false;
            return !Slots.Contains(offset + side) && HasDoor(offset, DirOf(side));
        }

        /// <summary>
        /// Caractère d'une case de l'emplacement `offset`, à (dx, dy) de son centre.
        ///
        /// Couloir : côté ouvert = sol, sinon mur ; coins en mur.
        ///
        /// Salle : **tout l'emplacement est du sol** (une vraie pièce de 3 × 3 cases par
        /// emplacement). Le pourtour est fermé par des **murs fins** sur les bords des cases
        /// (<see cref="EdgeWalls"/>), pas par des cases de roche. Seule exception, les piliers d'une
        /// salle à piliers : les coins au milieu d'un bloc 2 × 2 d'emplacements.
        /// </summary>
        char CellChar(Vector2Int offset, int dx, int dy)
        {
            if (dx == 0 && dy == 0)
                return '.';
            if (IsRoom)
            {
                if (dx == 0 || dy == 0 || !Def.Pillars)
                    return '.';
                var inBlock = Slots.Contains(offset + new Vector2Int(dx, 0)) && Slots.Contains(offset + new Vector2Int(0, dy))
                    && Slots.Contains(offset + new Vector2Int(dx, dy));
                return inBlock ? '#' : '.';
            }
            if (dx == 0 || dy == 0)
                return HasDoor(offset, DirOf(new Vector2Int(dx, dy))) ? '.' : '#';
            return '#';
        }

        /// <summary>
        /// Murs fins d'une salle sur l'emplacement `offset` : (case locale (lx, ly) de 0 à 2,
        /// direction) pour chaque bord de case du pourtour, sauf la case du milieu d'un côté qui a une
        /// porte. Vide pour un couloir (ses murs sont des cases de roche).
        /// </summary>
        public List<(Vector2Int cell, int dir)> EdgeWalls(Vector2Int offset)
        {
            var walls = new List<(Vector2Int, int)>();
            if (!IsRoom)
                return walls;
            foreach (var dir in Dirs)
            {
                if (Slots.Contains(offset + Offset(dir)))
                    continue; // l'emplacement voisin fait partie de la salle
                var door = HasDoor(offset, dir);
                var side = Offset(dir);
                // Les 3 cases du côté : celle du milieu, puis les deux le long du côté.
                var along = new Vector2Int(Mathf.Abs(side.y), Mathf.Abs(side.x));
                for (var k = -1; k <= 1; k++)
                {
                    if (k == 0 && door)
                        continue;
                    var c = Vector2Int.one + side + along * k;
                    walls.Add((c, dir));
                }
            }
            return walls;
        }

        /// <summary>
        /// Où accrocher la torche d'une salle : l'emplacement et le côté du premier mur du pourtour
        /// sans porte (la torche est sur la case du milieu de ce côté, contre le mur fin).
        /// </summary>
        (Vector2Int slot, int dir) TorchSide()
        {
            foreach (var s in Slots)
                foreach (var dir in Dirs)
                    if (!Slots.Contains(s + Offset(dir)) && !HasDoor(s, dir))
                        return (s, dir);
            return (Slots[0], 0);
        }

        public static Vector2Int Offset(int dir)
        {
            switch (dir)
            {
                case N: return new Vector2Int(0, -1);
                case E: return new Vector2Int(1, 0);
                case S: return new Vector2Int(0, 1);
                case W: return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        static int DirOf(Vector2Int v)
        {
            foreach (var dir in Dirs)
                if (Offset(dir) == v)
                    return dir;
            return 0;
        }

        public static int RotateMask(int mask, int quarterTurns)
        {
            for (var i = 0; i < Mod(quarterTurns, 4); i++)
                // Sens horaire : N → E → S → O → N (bits 1 → 2 → 4 → 8 → 1).
                mask = ((mask << 1) | (mask >> 3)) & 15;
            return mask;
        }

        public static int Opposite(int dir) => RotateMask(dir, 2);

        public static int CountOpenings(int mask)
        {
            var count = 0;
            foreach (var dir in Dirs)
                if ((mask & dir) != 0)
                    count++;
            return count;
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
