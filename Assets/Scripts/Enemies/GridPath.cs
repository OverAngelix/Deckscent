using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Recherche de chemin sur la grille du labyrinthe, partagée par les ennemis : largeur d'abord,
    /// 4 directions, murs fins compris (<see cref="Maze.CanStep"/>).
    /// </summary>
    public static class GridPath
    {
        /// <summary>
        /// Plus court chemin de `from` à `to` (sans `from`, avec `to`), en au plus `maxSteps` pas.
        /// Les cases de `blocked` comptent comme des murs (sauf `to`). Liste vide si `to` est trop
        /// loin ou inaccessible.
        /// </summary>
        public static List<Vector2Int> Find(Maze maze, Vector2Int from, Vector2Int to, int maxSteps, ICollection<Vector2Int> blocked = null)
        {
            var path = new List<Vector2Int>();
            if (from == to || maxSteps <= 0)
                return path;
            var parent = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var frontier = new List<Vector2Int> { from };
            for (var depth = 0; depth < maxSteps && frontier.Count > 0; depth++)
            {
                var next = new List<Vector2Int>();
                foreach (var cell in frontier)
                    for (var d = 0; d < Grid.Dirs.Length; d++)
                    {
                        var n = cell + Grid.Dirs[d];
                        if (parent.ContainsKey(n) || !maze.CanStep(cell, d))
                            continue;
                        if (n != to && blocked != null && blocked.Contains(n))
                            continue;
                        parent[n] = cell;
                        if (n == to)
                        {
                            for (var step = n; step != from; step = parent[step])
                                path.Insert(0, step);
                            return path;
                        }
                        next.Add(n);
                    }
                frontier = next;
            }
            return path;
        }
    }
}
