using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Deckscent.Tests
{
    /// <summary>Mémoire de l'étage (brouillard) : ligne de vue, murs, portée, salles, emplacements visités.</summary>
    public class ExplorationTests
    {
        /// <summary>« Peut-on passer » d'après un plan texte et des murs fins (comme Maze.CanStep).</summary>
        static System.Func<Vector2Int, int, bool> CanStep(string[] rows, IEnumerable<(Vector2Int cell, int dir)> edgeWalls = null)
        {
            var walls = new HashSet<(Vector2Int, int)>();
            if (edgeWalls != null)
                foreach (var (cell, dir) in edgeWalls)
                {
                    walls.Add((cell, dir));
                    walls.Add((cell + Grid.Dirs[dir], (dir + 2) % 4));
                }
            bool Walkable(Vector2Int c) => c.y >= 0 && c.y < rows.Length && c.x >= 0 && c.x < rows[c.y].Length && rows[c.y][c.x] != '#' && rows[c.y][c.x] != ' ';
            return (cell, dir) => !walls.Contains((cell, dir)) && Walkable(cell + Grid.Dirs[dir]);
        }

        [Test]
        public void OnVoitToutDroitJusquAuMurOuALaPortee()
        {
            string[] rows =
            {
                "##########",
                "#........#",
                "#.########",
                "#.########",
            };
            var fog = new Exploration();
            fog.Look(new Vector2Int(1, 1), CanStep(rows), 4);
            Assert.That(fog.IsSeen(new Vector2Int(1, 1)), "sa propre case");
            Assert.That(fog.IsSeen(new Vector2Int(5, 1)), "4 cases vers l'est");
            Assert.That(fog.IsSeen(new Vector2Int(6, 1)), Is.False, "au-delà de la portée");
            Assert.That(fog.IsSeen(new Vector2Int(1, 3)), "vers le sud, jusqu'au bout du couloir");
            Assert.That(fog.IsSeen(new Vector2Int(2, 2)), Is.False, "pas en biais");
        }

        [Test]
        public void UnMurFinArreteLaVue()
        {
            string[] rows = { "#####", "#...#", "#####" };
            var fog = new Exploration();
            fog.Look(new Vector2Int(1, 1), CanStep(rows, new[] { (new Vector2Int(2, 1), 1) }), 5);
            Assert.That(fog.IsSeen(new Vector2Int(2, 1)));
            Assert.That(fog.IsSeen(new Vector2Int(3, 1)), Is.False, "derrière le mur fin");
        }

        [Test]
        public void EntrerDansUneSalleLaDevoileEntiere()
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(DemoDungeon.Starter, new System.Random(1));
            DemoDungeon.Build(board);
            var rows = board.ToLayout().Split('\n');
            // Une salle de plusieurs emplacements, loin du départ.
            Vector2Int roomSlot = default;
            TileCard room = null;
            foreach (var pos in board.TilePositions)
            {
                var card = board.GetTile(pos);
                if (card.IsRoom && card.Slots.Count >= 3 && pos != board.StartTile)
                {
                    room = card;
                    roomSlot = pos;
                    break;
                }
            }
            Assert.That(room, Is.Not.Null);

            var fog = new Exploration();
            var version = fog.Version;
            fog.Look(DungeonBoard.TileCenterCell(roomSlot), CanStep(rows, board.EdgeWalls()), 1, board);
            Assert.That(fog.Version, Is.GreaterThan(version));
            Assert.That(fog.IsSlotVisited(roomSlot));
            foreach (var pos in board.TilePositions)
            {
                if (board.GetTile(pos) != room)
                    continue;
                Assert.That(fog.IsSlotSeen(pos), $"emplacement {pos} de la salle");
                Assert.That(fog.IsSeen(DungeonBoard.TileCenterCell(pos)), $"centre de {pos}");
                if (pos != roomSlot)
                    Assert.That(fog.IsSlotVisited(pos), Is.False, "vu, pas visité");
            }
            Assert.That(fog.IsSlotSeen(board.StartTile), Is.False, "le départ est loin");
        }
    }
}
