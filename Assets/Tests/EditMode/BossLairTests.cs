using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Deckscent.Tests
{
    /// <summary>Antre du boss : où le jeu la pose (logique pure du plateau) et son décor.</summary>
    public class BossLairTests
    {
        const int N = TileCard.N, E = TileCard.E, S = TileCard.S, W = TileCard.W;

        /// <summary>Départ « Couloir nord-sud » (4, 4), prolongé de couloirs vers le nord jusqu'en (4, 1).</summary>
        static DungeonBoard NorthCorridor()
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(1);
            for (var y = 3; y >= 1; y--)
                Assert.That(board.Place(new Vector2Int(4, y), new TileCard("couloir")), Is.True, "couloir en (4, " + y + ")");
            return board;
        }

        static bool AllReachable(DungeonBoard board, Vector2Int from) =>
            board.SlotDistances(from).Count == board.TilePositions.Count();

        [Test]
        public void LAntreVaLeplusLoinPossibleDuJoueur()
        {
            var board = NorthCorridor();
            var player = new Vector2Int(4, 4);
            var plan = board.FindBossLair(player, s => s == player);
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.Card.IsBossLair, Is.True);
            // Le plus loin : au bout du couloir nord (4, 0) ou plus loin, jamais près du départ.
            Assert.That(plan.Distance, Is.GreaterThanOrEqualTo(3), "distance en emplacements");
            board.PlaceBossLair(plan);
            Assert.That(board.BossLair, Is.SameAs(plan.Card));
            Assert.That(AllReachable(board, player), Is.True, "tout reste relié au joueur");
            var lairSlot = board.BossLairOrigin + plan.Card.Slots[0];
            Assert.That(board.SlotDistances(player).ContainsKey(lairSlot), Is.True, "l'antre est atteignable");
        }

        [Test]
        public void LAntreNeRemplaceJamaisUneTuileVisitee()
        {
            var board = NorthCorridor();
            var player = new Vector2Int(4, 4);
            var visited = new HashSet<Vector2Int> { new Vector2Int(4, 4), new Vector2Int(4, 3), new Vector2Int(4, 2), new Vector2Int(4, 1) };
            var plan = board.FindBossLair(player, visited.Contains);
            Assert.That(plan, Is.Not.Null);
            foreach (var offset in plan.Card.Slots)
                Assert.That(visited.Contains(plan.Anchor + offset), Is.False, "emplacement visité " + (plan.Anchor + offset));
            Assert.That(plan.Replaced, Is.Empty, "toutes les tuiles posées sont visitées");
        }

        [Test]
        public void LAntrePeutRemplacerUnCouloirInexplore()
        {
            var board = NorthCorridor();
            var player = new Vector2Int(4, 4);
            // Seul le départ est visité : le couloir nord, jamais parcouru, peut être écrasé.
            var plan = board.FindBossLair(player, s => s == player);
            var removed = new List<TileCard>();
            board.TileRemoved += (_, card) => removed.Add(card);
            board.PlaceBossLair(plan);
            Assert.That(removed.Count, Is.EqualTo(plan.Replaced.Count));
            Assert.That(AllReachable(board, player), Is.True);
        }

        [Test]
        public void LAntrePerceUnMurSiAucunPassageNeDonneSurElle()
        {
            // Une impasse fermée de partout sauf vers le départ : l'antre doit percer un mur.
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(1); // couloir nord-sud en (4, 4)
            Assert.That(board.Place(new Vector2Int(4, 3), new TileCard("impasse", 0)), Is.True); // ouverte au sud
            Assert.That(board.Place(new Vector2Int(4, 5), new TileCard("impasse", 2)), Is.True); // ouverte au nord
            var player = new Vector2Int(4, 4);
            var visited = new HashSet<Vector2Int> { new Vector2Int(4, 3), new Vector2Int(4, 4), new Vector2Int(4, 5) };
            var plan = board.FindBossLair(player, visited.Contains);
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.Breaches.Count, Is.EqualTo(1), "un mur percé");
            Assert.That(plan.Card.DoorCount, Is.EqualTo(1), "une seule porte, celle du mur percé");
            board.PlaceBossLair(plan);
            Assert.That(AllReachable(board, player), Is.True);
        }

        [Test]
        public void PasDeDeuxiemeAntre()
        {
            var board = NorthCorridor();
            var player = new Vector2Int(4, 4);
            board.PlaceBossLair(board.FindBossLair(player, s => s == player));
            Assert.That(board.FindBossLair(player, s => s == player), Is.Null);
        }

        [Test]
        public void LAntreNeComptePasDansLesFormesDeLaPioche()
        {
            Assert.That(TileCard.ShapeList.Any(kv => kv.Value.Boss), Is.False);
            foreach (var kv in TileCard.BossShapeList)
            {
                Assert.That(TileCard.HasShape(kv.Key), Is.True);
                Assert.That(new TileCard(kv.Key).IsBossLair, Is.True);
                Assert.That(kv.Value.NameKey, Is.EqualTo("forme.antre"));
            }
        }

        [Test]
        public void LeDecorLaisseLesPortesEtLeCentreLibres()
        {
            // Antre 3 × 2 emplacements = 9 × 6 cases, une porte au nord de l'emplacement du milieu.
            var cells = new List<Vector2Int>();
            for (var y = 0; y < 6; y++)
                for (var x = 0; x < 9; x++)
                    cells.Add(new Vector2Int(x, y));
            var door = new Vector2Int(4, 0);
            var decor = LairDecor.Layout(cells, c => c == door, new System.Random(3), out var boss);
            var taken = decor.Select(d => d.cell).ToHashSet();
            Assert.That(decor.Count(d => d.kind == "brasero"), Is.EqualTo(6), "un brasero par coin, deux au trône");
            Assert.That(decor.Count(d => d.kind == "trone"), Is.EqualTo(1));
            Assert.That(taken.Contains(door), Is.False, "pas sur la porte");
            Assert.That(taken.Contains(door + Vector2Int.up), Is.False, "pas devant la porte (au sud, vers l'intérieur)");
            Assert.That(taken.Contains(boss), Is.False, "le boss a sa case");
            // Le trône est contre le mur le plus loin de la porte (le sud), le boss devant lui.
            var throne = decor.First(d => d.kind == "trone").cell;
            Assert.That(throne, Is.EqualTo(new Vector2Int(4, 5)));
            Assert.That(boss, Is.EqualTo(new Vector2Int(4, 4)));
            // Tout ce qui ne bloque pas reste relié à la porte.
            var blocking = decor.Where(d => d.kind != "ossements").Select(d => d.cell).ToHashSet();
            var free = cells.Where(c => !blocking.Contains(c)).ToHashSet();
            var seen = new HashSet<Vector2Int> { door };
            var queue = new Queue<Vector2Int>(seen);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Grid.Dirs)
                    if (free.Contains(c + d) && seen.Add(c + d))
                        queue.Enqueue(c + d);
            }
            Assert.That(seen.Count, Is.EqualTo(free.Count), "aucune case libre enfermée");
        }

        [Test]
        public void ExplorationOublieLesEmplacementsDeLAntre()
        {
            var exploration = new Exploration();
            exploration.See(new Vector2Int(1, 1));  // emplacement (0, 0)
            exploration.See(new Vector2Int(4, 1));  // emplacement (1, 0)
            exploration.Forget(new[] { new Vector2Int(1, 0) });
            Assert.That(exploration.IsSeen(new Vector2Int(1, 1)), Is.True);
            Assert.That(exploration.IsSeen(new Vector2Int(4, 1)), Is.False);
            Assert.That(exploration.IsSlotSeen(new Vector2Int(1, 0)), Is.False);
        }
    }
}
