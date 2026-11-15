using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Donjon de démonstration, autour du départ 0 (carrefour central) : une tuile de chaque sorte
    /// de salle ou presque, posées avec les VRAIES règles du plateau. Sert aux captures et aux tests.
    /// Les portes sont fixées (pas tirées) pour que chaque tuile se raccorde.
    /// </summary>
    public static class DemoDungeon
    {
        public const int Starter = 0;

        readonly struct Entry
        {
            public readonly Vector2Int Pos;
            public readonly string Shape;
            public readonly int Turns;
            public readonly bool Torch;
            public readonly (Vector2Int, int)[] Doors;

            public Entry(int x, int y, string shape, int turns, bool torch, params (Vector2Int, int)[] doors)
            {
                Pos = new Vector2Int(x, y);
                Shape = shape;
                Turns = turns;
                Torch = torch;
                Doors = doors;
            }
        }

        static (Vector2Int, int) D(int x, int y, int dir) => (new Vector2Int(x, y), dir);
        const int N = TileCard.N, E = TileCard.E, S = TileCard.S, W = TileCard.W;

        // Dans l'ordre de pose : chaque tuile se raccorde à une tuile déjà posée.
        // Portes : décalage de l'emplacement (après rotation), direction.
        static readonly Entry[] Tiles =
        {
            new Entry(4, 3, "couloir", 0, true),
            new Entry(3, 1, "grande_salle", 0, true, D(1, 1, S), D(0, 0, W), D(1, 0, E)),
            new Entry(5, 4, "galerie", 0, false, D(0, 0, W), D(2, 0, N), D(1, 0, S)),
            new Entry(5, 5, "salle_piliers", 0, true, D(1, 0, N), D(0, 1, W), D(1, 1, S)),
            new Entry(4, 5, "couloir", 0, false),
            new Entry(4, 6, "t", 2, true),
            new Entry(2, 5, "salle_croix", 0, true, D(1, 1, E), D(-1, 1, W)),
            new Entry(2, 4, "salle", 0, false, D(1, 0, E), D(0, 0, N)),
            new Entry(5, 1, "salle_u", 0, true, D(0, 0, W), D(2, 1, S)),
            new Entry(7, 3, "couloir", 0, false),
            new Entry(2, 0, "longue_galerie", 1, true, D(0, 3, S), D(0, 1, E), D(0, 0, W)),
            new Entry(0, 6, "salle_l", 0, false, D(0, 0, E)),
            new Entry(5, 7, "salle_t", 0, true, D(1, 0, N)),
        };

        /// <summary>
        /// Contenu de chaque tuile de démo (par emplacement) : le Spawner le fait apparaître en jeu,
        /// pour essayer combat, pièges et coffres sans rien poser. Les autres tuiles restent vides.
        /// </summary>
        static readonly Dictionary<Vector2Int, string> Contents = new Dictionary<Vector2Int, string>
        {
            [new Vector2Int(4, 3)] = "piege",          // juste au nord du départ
            [new Vector2Int(4, 5)] = "monstre",        // juste au sud du départ
            [new Vector2Int(7, 3)] = "tresor",
            [new Vector2Int(5, 4)] = "salle_tresor",
            [new Vector2Int(5, 5)] = "salle_pieges",
            [new Vector2Int(2, 4)] = "salle_piegee",
            [new Vector2Int(2, 5)] = "salle_gardee",
            [new Vector2Int(3, 1)] = "salle_monstres",
            [new Vector2Int(5, 1)] = "salle_tresor",
            [new Vector2Int(5, 7)] = "salle_monstres",
        };

        /// <summary>Pose les tuiles de démo sur un plateau déjà au départ 0. Renvoie les refus (vide si tout passe).</summary>
        public static List<string> Build(DungeonBoard board)
        {
            var refusals = new List<string>();
            foreach (var entry in Tiles)
            {
                var card = new TileCard(entry.Shape, entry.Turns, entry.Torch);
                if (Contents.TryGetValue(entry.Pos, out var content))
                    card.Content = content;
                if (card.IsRoom)
                    card.SetDoors(entry.Doors);
                if (!board.Place(entry.Pos, card))
                    refusals.Add($"{card.DisplayName} en {entry.Pos} : {board.CheckPlacement(entry.Pos, card)}");
            }
            return refusals;
        }
    }
}
