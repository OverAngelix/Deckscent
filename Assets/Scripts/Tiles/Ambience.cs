using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Décor d'ambiance des tuiles posées (demande de l'utilisateur : « des décors dans le donjon,
    /// salles et couloirs ») : logique pure, testée en EditMode. Le <see cref="Spawner"/> l'applique
    /// après le contenu de la tuile, sur les cases restées libres.
    ///
    /// - **Salles** : dans les **coins** (deux murs), un vase ou une caisse à briser, parfois une
    ///   statue ; le long des murs, des bougies, un crâne, des os. Un coin bloqué ne coupe jamais
    ///   le passage (ses deux voisins se rejoignent par l'intérieur).
    /// - **Couloirs** : de temps en temps un crâne, des os ou des bougies contre un mur, de passage
    ///   (un couloir ne se bouche jamais).
    ///
    /// Sortes : « vase », « caisse » (à briser), « statue » (bloque) ; « bougies », « crane », « os »
    /// (de passage). `wall` : direction (indice de <see cref="Grid.Dirs"/>) du mur contre lequel se
    /// caler, -1 au milieu.
    /// </summary>
    public static class Ambience
    {
        public readonly struct Piece
        {
            public readonly Vector2Int Cell;
            public readonly string Kind;
            public readonly int Wall;
            public readonly int Wall2;

            public Piece(Vector2Int cell, string kind, int wall, int wall2 = -1)
            {
                Cell = cell;
                Kind = kind;
                Wall = wall;
                Wall2 = wall2;
            }

            /// <summary>Bloque sa case (vase, caisse, statue).</summary>
            public bool Blocks => Kind == "vase" || Kind == "caisse" || Kind == "statue";
        }

        static readonly string[] Small = { "bougies", "crane", "os" };

        /// <summary>
        /// Le décor d'une tuile : `cells` = ses cases de sol, `canStep(case, dir)` = passage ouvert
        /// (<see cref="Maze.CanStep"/>), `taken` = cases déjà prises (contenu, joueur, portes).
        /// </summary>
        public static List<Piece> Layout(IReadOnlyList<Vector2Int> cells, bool isRoom, Func<Vector2Int, int, bool> canStep,
            ISet<Vector2Int> taken, System.Random rng)
        {
            var pieces = new List<Piece>();
            var used = new HashSet<Vector2Int>(taken);
            var walls = new List<int>(4);
            if (isRoom)
            {
                foreach (var c in cells)
                {
                    if (used.Contains(c))
                        continue;
                    Walls(c, canStep, walls);
                    // Un coin : deux murs qui se touchent (pas un passage entre deux murs).
                    var corner = walls.Count == 2 && (walls[1] - walls[0]) % 2 == 1;
                    if (corner && rng.NextDouble() < 0.4)
                    {
                        var roll = rng.NextDouble();
                        var kind = roll < 0.45 ? "vase" : roll < 0.85 ? "caisse" : "statue";
                        pieces.Add(new Piece(c, kind, walls[0], walls[1]));
                        used.Add(c);
                    }
                    else if (walls.Count == 1 && rng.NextDouble() < 0.12)
                    {
                        pieces.Add(new Piece(c, Small[rng.Next(Small.Length)], walls[0]));
                        used.Add(c);
                    }
                }
                return pieces;
            }
            // Couloir : une pièce au plus, contre un mur.
            if (rng.NextDouble() < 0.35)
            {
                var start = rng.Next(cells.Count);
                for (var i = 0; i < cells.Count; i++)
                {
                    var c = cells[(start + i) % cells.Count];
                    if (used.Contains(c))
                        continue;
                    Walls(c, canStep, walls);
                    if (walls.Count == 0 || walls.Count == 4)
                        continue;
                    pieces.Add(new Piece(c, Small[rng.Next(Small.Length)], walls[rng.Next(walls.Count)]));
                    break;
                }
            }
            return pieces;
        }

        /// <summary>Les directions fermées (mur ou roche) autour d'une case, dans l'ordre N, E, S, O.</summary>
        static void Walls(Vector2Int c, Func<Vector2Int, int, bool> canStep, List<int> walls)
        {
            walls.Clear();
            for (var d = 0; d < 4; d++)
                if (!canStep(c, d))
                    walls.Add(d);
        }
    }
}
