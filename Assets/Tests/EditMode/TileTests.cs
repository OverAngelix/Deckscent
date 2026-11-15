using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Deckscent.Tests
{
    /// <summary>Cartes-tuiles, pioche et plateau : logique pure, sans scène.</summary>
    public class TileTests
    {
        const int N = TileCard.N, E = TileCard.E, S = TileCard.S, W = TileCard.W;

        static IEnumerable<string> RoomShapes => TileCard.ShapeList.Where(kv => kv.Value.Room).Select(kv => kv.Key);
        static IEnumerable<string> AllShapes => TileCard.ShapeList.Select(kv => kv.Key);

        [Test]
        public void UnQuartDeTourDecaleLesOuvertures()
        {
            Assert.That(TileCard.RotateMask(N, 1), Is.EqualTo(E));
            Assert.That(TileCard.RotateMask(W, 1), Is.EqualTo(N));
            Assert.That(TileCard.RotateMask(N | E, 3), Is.EqualTo(W | N));
            Assert.That(TileCard.Opposite(E), Is.EqualTo(W));
        }

        [Test]
        public void UnVirageTourneDonneLeBonPlan([Values(0, 1, 2, 3)] int turns)
        {
            var card = new TileCard("virage", turns);
            var expected = TileCard.RotateMask(E | S, turns);
            Assert.That(card.Openings, Is.EqualTo(expected));
            var rows = card.CellsForSlot(Vector2Int.zero);
            Assert.That(rows[0][1] == '.', Is.EqualTo((expected & N) != 0), "nord");
            Assert.That(rows[1][2] == '.', Is.EqualTo((expected & E) != 0), "est");
            Assert.That(rows[2][1] == '.', Is.EqualTo((expected & S) != 0), "sud");
            Assert.That(rows[1][0] == '.', Is.EqualTo((expected & W) != 0), "ouest");
            Assert.That(rows[0][0], Is.EqualTo('#'), "coin");
        }

        [Test]
        public void QuatreQuartsDeTourReviennentAuDepart([ValueSource(nameof(AllShapes))] string shape)
        {
            var card = new TileCard(shape);
            if (card.IsRoom)
                card.RandomDoors(new System.Random(3));
            var turned = card.Copy();
            turned.Rotate(4);
            Assert.That(turned.Slots, Is.EquivalentTo(card.Slots));
            Assert.That(turned.Doors, Is.EquivalentTo(card.Doors));
            Assert.That(turned.Slots[0], Is.EqualTo(Vector2Int.zero), "l'ancre reste en (0, 0)");
        }

        [Test]
        public void UneSalleTourneGardeSaFormeEtSonAncre([ValueSource(nameof(RoomShapes))] string shape)
        {
            var card = new TileCard(shape, 1);
            Assert.That(card.Slots[0], Is.EqualTo(Vector2Int.zero));
            Assert.That(card.Slots.Count, Is.EqualTo(TileCard.GetShape(shape).Slots.Length));
            Assert.That(card.Slots.Distinct().Count(), Is.EqualTo(card.Slots.Count));
            var b = card.Bounds();
            Assert.That(card.Footprint, Is.EqualTo(b.size));
        }

        [Test]
        public void LesPortesTireesSontSurLePourtour([ValueSource(nameof(RoomShapes))] string shape)
        {
            for (var seed = 1; seed <= 30; seed++)
            {
                var card = new TileCard(shape, seed % 4);
                card.RandomDoors(new System.Random(seed));
                Assert.That(card.DoorCount, Is.InRange(1, card.MaxDoors));
                var perimeter = card.PerimeterSides();
                foreach (var kv in card.Doors)
                    foreach (var dir in TileCard.Dirs)
                        if ((kv.Value & dir) != 0)
                            Assert.That(perimeter.Contains((kv.Key, dir)), $"porte {kv.Key} {dir} hors du pourtour");
            }
        }

        /// <summary>Tout le sol d'une salle est d'un seul tenant (piliers et creux compris).</summary>
        [Test]
        public void LeSolDUneSalleEstDUnSeulTenant([ValueSource(nameof(RoomShapes))] string shape)
        {
            var rows = new TileCard(shape).FullRows();
            var floor = new List<Vector2Int>();
            for (var y = 0; y < rows.Length; y++)
                for (var x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == '.' || rows[y][x] == 'T')
                        floor.Add(new Vector2Int(x, y));
            Assert.That(Reachable(rows, floor[0]).Count, Is.EqualTo(floor.Count), string.Join("\n", rows));
        }

        [Test]
        public void LesSallesAPiliersOntDesPiliers()
        {
            var rows = new TileCard("salle_piliers").FullRows();
            // 3 × 2 emplacements : deux jonctions, chacune un pilier de 2 × 2 cases.
            Assert.That(rows[2][2], Is.EqualTo('#'));
            Assert.That(rows[3][3], Is.EqualTo('#'));
            Assert.That(rows[2][5], Is.EqualTo('#'));
            var plain = new TileCard("grande_salle").FullRows();
            Assert.That(plain[2][2], Is.EqualTo('.'), "une grande salle sans piliers est ouverte au centre");
        }

        /// <summary>Une salle est une vraie pièce : tout l'emplacement est du sol (pas de piliers).</summary>
        [Test]
        public void UneSalleEstEnSolPlein([Values("salle", "galerie", "salle_l", "salle_u", "salle_croix", "grande_salle")] string shape)
        {
            var card = new TileCard(shape);
            var rows = card.FullRows();
            var floor = rows.Sum(r => r.Count(c => c == '.' || c == 'T'));
            Assert.That(floor, Is.EqualTo(card.Slots.Count * 9), string.Join("\n", rows));
        }

        /// <summary>Le pourtour d'une salle est fermé de murs fins, sauf aux portes.</summary>
        [Test]
        public void LePourtourDUneSalleEstFermeSaufAuxPortes()
        {
            var card = new TileCard("salle"); // (0, 0) et (1, 0)
            card.SetDoors(new[] { (new Vector2Int(1, 0), E) });
            var walls = card.EdgeWalls(new Vector2Int(1, 0));
            Assert.That(walls, Has.Member((new Vector2Int(2, 0), E)), "coin du côté est");
            Assert.That(walls, Has.No.Member((new Vector2Int(2, 1), E)), "porte au milieu du côté est");
            Assert.That(walls, Has.No.Member((new Vector2Int(0, 1), W)), "pas de mur vers l'autre emplacement de la salle");
            Assert.That(walls.Count(w => w.dir == N), Is.EqualTo(3), "côté nord entièrement fermé");
            Assert.That(new TileCard("couloir").EdgeWalls(Vector2Int.zero), Is.Empty, "un couloir n'a pas de murs fins");
        }

        /// <summary>Un couloir qui arrive face au mur d'une salle sans porte reste un cul-de-sac.</summary>
        [Test]
        public void UnCouloirFaceAUnMurSansPorteResteUnCulDeSac()
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(0); // carrefour (4, 4), ouvert de partout
            var room = new TileCard("grande_salle"); // (5, 3) à (6, 4)
            room.SetDoors(new[] { (new Vector2Int(0, 1), W) }); // seule porte : (5, 4) côté ouest, face au carrefour
            Assert.That(board.Place(new Vector2Int(5, 3), room), Is.True);
            // Fourche en (4, 3) : raccordée au carrefour par le sud, son passage est donne sur la salle
            // en (5, 3), où il n'y a pas de porte.
            var fork = new TileCard("t");
            Assert.That(fork.Openings, Is.EqualTo(E | S | W));
            Assert.That(board.Place(new Vector2Int(4, 3), fork), Is.True);

            var rows = board.ToLayout().Split('\n');
            var walls = WallSet(board);
            Assert.That(rows[10][14], Is.EqualTo('.'), "le passage est de la fourche est du sol");
            Assert.That(rows[10][15], Is.EqualTo('.'), "la case de la salle en face est du sol");
            Assert.That(walls.Contains((new Vector2Int(15, 10), 3)), "un mur fin les sépare (côté salle, vers l'ouest)");
            Assert.That(walls.Contains((new Vector2Int(14, 10), 1)), "et dans l'autre sens");
            Assert.That(walls.Contains((new Vector2Int(15, 13), 3)), Is.False, "pas de mur à la porte");
            Assert.That(Reachable(rows, new Vector2Int(13, 13), walls).Contains(new Vector2Int(16, 13)), "on entre par la porte");
        }

        /// <summary>Murs fins du plateau, dans les deux sens, comme le labyrinthe les voit.</summary>
        static HashSet<(Vector2Int, int)> WallSet(DungeonBoard board)
        {
            var set = new HashSet<(Vector2Int, int)>();
            foreach (var (cell, dir) in board.EdgeWalls())
            {
                set.Add((cell, dir));
                set.Add((cell + Grid.Dirs[dir], (dir + 2) % 4));
            }
            return set;
        }

        [Test]
        public void LaPiocheDonneSurtoutDesCouloirs()
        {
            // Sans plafond, pour mesurer la part de salles : environ 1 carte sur 7.
            var settings = new DeckSettings { maxRoomsPerFloor = int.MaxValue };
            var deck = new TileDeck(settings, 42);
            var rooms = 0;
            var shapes = new HashSet<string>();
            for (var i = 0; i < 4000; i++)
            {
                var card = deck.DrawCard();
                shapes.Add(card.ShapeId);
                if (!card.IsRoom)
                {
                    Assert.That(card.SpawnList().Length, Is.LessThanOrEqualTo(1), "un couloir porte au plus un élément");
                    continue;
                }
                rooms++;
                Assert.That(card.DoorCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(card.Content, Does.StartWith("salle_"));
            }
            Assert.That(rooms / 4000f, Is.InRange(0.11f, 0.19f));
            Assert.That(shapes, Is.SupersetOf(AllShapes), "toutes les formes sortent");
        }

        [Test]
        public void PasPlusDeSixSallesParEtage([Values(1, 2, 3)] int seed)
        {
            var deck = new TileDeck(new DeckSettings(), seed);
            // On pose la première carte de la main, encore et encore.
            for (var i = 0; i < 300; i++)
            {
                if (deck.Hand[0].IsRoom)
                    deck.RoomsPlaced++;
                deck.Replace(0);
                Assert.That(deck.RoomsOnFloor, Is.LessThanOrEqualTo(6));
            }
            Assert.That(deck.RoomsPlaced, Is.InRange(5, 6), "on finit par avoir 5 ou 6 salles posées");
            Assert.That(deck.Hand.Count(c => c.IsRoom), Is.LessThanOrEqualTo(6 - deck.RoomsPlaced));
        }

        [Test]
        public void LesSallesSontDensesSelonLeurTaille()
        {
            TileCard Room(string shape, string content) => new TileCard(shape) { Content = content };
            Assert.That(Room("salle", "salle_monstres").SpawnList().Length, Is.EqualTo(3), "petite salle (2 emplacements)");
            Assert.That(Room("galerie", "salle_monstres").SpawnList().Length, Is.EqualTo(5), "salle moyenne (3 emplacements)");
            Assert.That(Room("grande_salle", "salle_gardee").SpawnList().Length, Is.EqualTo(5), "salle moyenne (4 emplacements)");
            Assert.That(Room("crypte", "salle_monstres").SpawnList().Length, Is.EqualTo(7), "grande salle (9 emplacements)");
            Assert.That(Room("salle", "salle_vide").SpawnList(), Is.Empty);
            foreach (var kv in TileCard.Contents.Where(c => c.Value.Room && c.Key != "salle_vide"))
                Assert.That(kv.Value.Small.Length, Is.GreaterThanOrEqualTo(2), kv.Key + " : même petite, une salle est dense");
        }

        [Test]
        public void ChaqueDepartGardeUnPassageLibre([Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)] int starter)
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(starter);
            Assert.That(board.OpenExits(), Is.Not.Empty);
            Assert.That(board.ToLayout().Count(c => c == 'P'), Is.EqualTo(1));
        }

        [Test]
        public void LesReglesDePoseDonnentLaRaison()
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(0); // carrefour en (4, 4)
            Assert.That(board.CheckPlacement(new Vector2Int(4, 4), new TileCard("couloir")), Is.EqualTo("Emplacement déjà occupé"));
            Assert.That(board.CheckPlacement(new Vector2Int(8, 8), new TileCard("couloir")), Is.EqualTo("Doit toucher le labyrinthe"));
            Assert.That(board.CheckPlacement(new Vector2Int(4, 3), new TileCard("couloir", 1)), Is.EqualTo("Doit se raccorder par un passage"));
            Assert.That(board.CheckPlacement(new Vector2Int(8, 4), new TileCard("galerie")), Is.EqualTo("Hors du plateau"));
            Assert.That(board.CheckPlacement(new Vector2Int(4, 3), new TileCard("couloir")), Is.Empty);
        }

        [Test]
        public void UneSalleSePoseSurPlusieursEmplacements()
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(0);
            var room = new TileCard("grande_salle");
            room.SetDoors(new[] { (new Vector2Int(0, 0), W) });
            Assert.That(board.Place(new Vector2Int(5, 3), room), Is.False, "la porte ne donne pas sur le carrefour");
            room.SetDoors(new[] { (new Vector2Int(0, 1), W) });
            Assert.That(board.Place(new Vector2Int(5, 3), room), Is.True, board.CheckPlacement(new Vector2Int(5, 3), room));
            foreach (var p in new[] { new Vector2Int(5, 3), new Vector2Int(6, 3), new Vector2Int(5, 4), new Vector2Int(6, 4) })
            {
                Assert.That(board.GetTile(p), Is.SameAs(room));
                Assert.That(board.TileOrigin(p), Is.EqualTo(new Vector2Int(5, 3)));
            }
            Assert.That(board.TileFloorCells(new Vector2Int(6, 4)), Has.None.EqualTo(new Vector2Int(15, 13)), "pas de contenu sur la porte");
        }

        [Test]
        public void LeDonjonDeDemoSePoseEntierementEtToutEstAccessible()
        {
            var board = new DungeonBoard(new Vector2Int(9, 9));
            board.NewGame(DemoDungeon.Starter);
            var refusals = DemoDungeon.Build(board);
            Assert.That(refusals, Is.Empty, string.Join("\n", refusals));

            var rows = board.ToLayout().Split('\n');
            var start = Enumerable.Range(0, rows.Length).SelectMany(y => Enumerable.Range(0, rows[y].Length).Select(x => new Vector2Int(x, y)))
                .First(p => rows[p.y][p.x] == 'P');
            var reached = Reachable(rows, start, WallSet(board));
            foreach (var pos in board.TilePositions)
                Assert.That(reached.Contains(DungeonBoard.TileCenterCell(pos)), $"emplacement {pos} inaccessible depuis le départ\n{string.Join("\n", rows)}");
        }

        static HashSet<Vector2Int> Reachable(string[] rows, Vector2Int from, HashSet<(Vector2Int, int)> walls = null)
        {
            var seen = new HashSet<Vector2Int> { from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                for (var i = 0; i < Grid.Dirs.Length; i++)
                {
                    if (walls != null && walls.Contains((c, i)))
                        continue;
                    var n = c + Grid.Dirs[i];
                    if (n.y < 0 || n.y >= rows.Length || n.x < 0 || n.x >= rows[n.y].Length)
                        continue;
                    var ch = rows[n.y][n.x];
                    if (ch == '#' || ch == ' ' || !seen.Add(n))
                        continue;
                    queue.Enqueue(n);
                }
            }
            return seen;
        }
    }
}
