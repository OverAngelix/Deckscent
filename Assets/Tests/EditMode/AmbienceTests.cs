using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Deckscent.Tests
{
    /// <summary>Décor d'ambiance des salles et des couloirs (logique pure).</summary>
    public class AmbienceTests
    {
        /// <summary>Une salle rectangulaire w × h, sans porte : on passe d'une case à l'autre à l'intérieur.</summary>
        static (List<Vector2Int> cells, System.Func<Vector2Int, int, bool> canStep) Room(int w, int h)
        {
            var cells = new List<Vector2Int>();
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    cells.Add(new Vector2Int(x, y));
            var set = new HashSet<Vector2Int>(cells);
            return (cells, (c, d) => set.Contains(c + Grid.Dirs[d]));
        }

        [Test]
        public void DansUneSalleLesPiecesQuiBloquentSontDansLesCoins([Values(1, 2, 3, 4, 5)] int seed)
        {
            var (cells, canStep) = Room(6, 6);
            var pieces = Ambience.Layout(cells, true, canStep, new HashSet<Vector2Int>(), new System.Random(seed));
            var corners = new[] { new Vector2Int(0, 0), new Vector2Int(5, 0), new Vector2Int(0, 5), new Vector2Int(5, 5) };
            foreach (var p in pieces.Where(p => p.Blocks))
                Assert.That(corners, Does.Contain(p.Cell), p.Kind + " hors d'un coin");
            Assert.That(pieces.Select(p => p.Cell).Distinct().Count(), Is.EqualTo(pieces.Count), "une pièce par case");
            // Le reste de la salle reste d'un seul tenant.
            var blocked = pieces.Where(p => p.Blocks).Select(p => p.Cell).ToHashSet();
            var free = cells.Where(c => !blocked.Contains(c)).ToList();
            var seen = new HashSet<Vector2Int> { free[0] };
            var queue = new Queue<Vector2Int>(seen);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                for (var d = 0; d < 4; d++)
                {
                    var n = c + Grid.Dirs[d];
                    if (canStep(c, d) && !blocked.Contains(n) && seen.Add(n))
                        queue.Enqueue(n);
                }
            }
            Assert.That(seen.Count, Is.EqualTo(free.Count));
        }

        [Test]
        public void LesCasesPrisesRestentLibres()
        {
            var (cells, canStep) = Room(3, 3);
            var taken = new HashSet<Vector2Int>(cells);
            for (var seed = 0; seed < 20; seed++)
                Assert.That(Ambience.Layout(cells, true, canStep, taken, new System.Random(seed)), Is.Empty);
        }

        [Test]
        public void UnCouloirNeSeBoucheJamais()
        {
            // Couloir nord-sud de 3 cases.
            var cells = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) };
            var set = new HashSet<Vector2Int>(cells);
            System.Func<Vector2Int, int, bool> canStep = (c, d) => set.Contains(c + Grid.Dirs[d]);
            var placed = 0;
            for (var seed = 0; seed < 50; seed++)
            {
                var pieces = Ambience.Layout(cells, false, canStep, new HashSet<Vector2Int>(), new System.Random(seed));
                Assert.That(pieces.Count, Is.LessThanOrEqualTo(1), "une pièce au plus");
                Assert.That(pieces.All(p => !p.Blocks), Is.True, "rien qui bloque dans un couloir");
                placed += pieces.Count;
            }
            Assert.That(placed, Is.InRange(5, 35), "de temps en temps");
        }
    }
}
