using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Mémoire de l'étage (brouillard de guerre) : les cases que le joueur a vues, et les
    /// emplacements de tuiles où il a mis les pieds. Logique pure, tenue par le
    /// <see cref="Dungeon"/> ; <see cref="ExplorationTracker"/> la nourrit en jeu.
    ///
    /// Ce qu'on voit depuis une case (<see cref="Look"/>) : la case elle-même, puis tout droit dans
    /// les quatre directions jusqu'à un mur (murs fins compris) ou jusqu'à `range` cases, et toute
    /// la salle où l'on se trouve. On tourne gratuitement : les quatre directions comptent.
    /// </summary>
    public class Exploration
    {
        readonly HashSet<Vector2Int> _seen = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> _seenSlots = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> _visitedSlots = new HashSet<Vector2Int>();

        /// <summary>Augmente à chaque nouvelle case vue (les écrans se redessinent quand elle bouge).</summary>
        public int Version { get; private set; }
        public IReadOnlyCollection<Vector2Int> SeenCells => _seen;

        public bool IsSeen(Vector2Int cell) => _seen.Contains(cell);
        /// <summary>Au moins une case de cet emplacement a été vue.</summary>
        public bool IsSlotSeen(Vector2Int slot) => _seenSlots.Contains(slot);
        /// <summary>Le joueur s'est tenu sur une case de cet emplacement.</summary>
        public bool IsSlotVisited(Vector2Int slot) => _visitedSlots.Contains(slot);

        public static Vector2Int SlotOf(Vector2Int cell) =>
            new Vector2Int(Mathf.FloorToInt(cell.x / (float)DungeonBoard.TileCells), Mathf.FloorToInt(cell.y / (float)DungeonBoard.TileCells));

        /// <summary>Marque une case comme vue. Vrai si elle ne l'était pas.</summary>
        public bool See(Vector2Int cell)
        {
            if (!_seen.Add(cell))
                return false;
            _seenSlots.Add(SlotOf(cell));
            Version++;
            return true;
        }

        /// <summary>
        /// Oublie tout ce qui a été vu sur ces emplacements (l'antre du boss remplace des tuiles
        /// inexplorées : elle reste à découvrir).
        /// </summary>
        public void Forget(IEnumerable<Vector2Int> slots)
        {
            var forget = new HashSet<Vector2Int>(slots);
            if (_seen.RemoveWhere(c => forget.Contains(SlotOf(c))) > 0)
                Version++;
            _seenSlots.ExceptWith(forget);
        }

        /// <summary>
        /// Le joueur est sur `cell` : ce qu'il voit devient connu. `canStep(case, direction)` dit si
        /// on passe d'une case à sa voisine (<see cref="Maze.CanStep"/>). `board` (facultatif) :
        /// toute la salle où il se trouve est vue.
        /// </summary>
        public void Look(Vector2Int cell, Func<Vector2Int, int, bool> canStep, int range, DungeonBoard board = null)
        {
            if (_visitedSlots.Add(SlotOf(cell)))
                Version++;
            See(cell);
            for (var dir = 0; dir < Grid.Dirs.Length; dir++)
            {
                var c = cell;
                for (var i = 0; i < range && canStep(c, dir); i++)
                {
                    c += Grid.Dirs[dir];
                    See(c);
                }
            }
            if (board != null)
                SeeRoom(board, SlotOf(cell));
        }

        /// <summary>Si l'emplacement porte une salle, toutes ses cases de sol deviennent connues.</summary>
        void SeeRoom(DungeonBoard board, Vector2Int slot)
        {
            var card = board.GetTile(slot);
            if (card == null || !card.IsRoom)
                return;
            foreach (var pos in board.TilePositions)
            {
                if (board.GetTile(pos) != card)
                    continue;
                var rows = board.CellsAt(pos);
                for (var dy = 0; dy < rows.Length; dy++)
                    for (var dx = 0; dx < rows[dy].Length; dx++)
                        if (rows[dy][dx] != '#' && rows[dy][dx] != ' ')
                            See(pos * DungeonBoard.TileCells + new Vector2Int(dx, dy));
            }
        }
    }
}
