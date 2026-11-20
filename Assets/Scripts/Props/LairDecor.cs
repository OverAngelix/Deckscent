using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Pièce du décor de l'antre du boss (brasero, cage, trône…) : elle occupe sa case comme un
    /// obstacle (le joueur, les monstres et la charge du boss s'y arrêtent), ne joue pas.
    /// Placée par le <see cref="Spawner"/> le long des murs (<see cref="Layout"/>).
    /// </summary>
    public class LairDecor : GridEntity
    {
        [Tooltip("Coché : bloque la case (joueur, monstres, charge). Décoché : décor de passage (ossements au sol…).")]
        [SerializeField] bool blocks = true;

        public override bool BlocksPlayer => blocks;

        /// <summary>
        /// Décor de l'antre : (case, sorte) pour chaque pièce. Logique pure (testable).
        /// - **Braseros** dans les quatre coins de l'antre et de part et d'autre du trône (lumière : on y voit le combat) ;
        /// - le **trône** au milieu du mur le plus loin des portes (à vol d'oiseau), le boss dort devant ;
        /// - des **cages** et des **tas d'os** çà et là le long des murs (une case sur trois, jamais deux côte à côte) ;
        /// - des **ossements** épars au sol, au milieu (décor de passage, ils ne bloquent pas) ;
        /// - des **chaînes** qui pendent du plafond au-dessus de la salle (on passe dessous).
        /// Jamais sur une porte ni devant (la case juste à l'intérieur), ni au milieu de la salle :
        /// tout l'intérieur reste libre, l'antre reste traversable d'une porte à l'autre.
        /// `cells` : sol de l'antre ; `isDoor(case)` : porte de l'antre ; `inside(case)` : case de l'antre.
        /// </summary>
        public static List<(Vector2Int cell, string kind)> Layout(List<Vector2Int> cells, System.Func<Vector2Int, bool> isDoor,
            System.Random rng, out Vector2Int bossCell)
        {
            var set = new HashSet<Vector2Int>(cells);
            var doors = new List<Vector2Int>();
            foreach (var c in cells)
                if (isDoor(c))
                    doors.Add(c);
            // Cases à garder libres : les portes et celles qui les touchent.
            var keep = new HashSet<Vector2Int>(doors);
            foreach (var d in doors)
                foreach (var dir in Grid.Dirs)
                    keep.Add(d + dir);
            // Bord : une case de l'antre dont un voisin n'en est pas.
            bool OnEdge(Vector2Int c)
            {
                foreach (var dir in Grid.Dirs)
                    if (!set.Contains(c + dir))
                        return true;
                return false;
            }

            var min = cells[0];
            var max = cells[0];
            foreach (var c in cells)
            {
                min = Vector2Int.Min(min, c);
                max = Vector2Int.Max(max, c);
            }
            var decor = new List<(Vector2Int, string)>();
            var used = new HashSet<Vector2Int>();
            // Pas deux pièces côte à côte le long d'un mur (une rangée de cages faisait mur).
            bool NextToDecor(Vector2Int c)
            {
                foreach (var dir in Grid.Dirs)
                    if (used.Contains(c + dir))
                        return true;
                return false;
            }
            void Add(Vector2Int c, string kind)
            {
                if (set.Contains(c) && !keep.Contains(c) && used.Add(c))
                    decor.Add((c, kind));
            }

            // Braseros aux coins.
            foreach (var corner in new[] { min, new Vector2Int(max.x, min.y), new Vector2Int(min.x, max.y), max })
                Add(corner, "brasero");

            // Le trône : milieu du bord le plus loin de toutes les portes.
            var center = new Vector2Int((min.x + max.x) / 2, (min.y + max.y) / 2);
            var sides = new[]
            {
                new Vector2Int(center.x, min.y), new Vector2Int(max.x, center.y), new Vector2Int(center.x, max.y), new Vector2Int(min.x, center.y),
            };
            var throne = sides[0];
            var bestScore = -1;
            foreach (var s in sides)
            {
                if (keep.Contains(s))
                    continue;
                // Distance à vol d'oiseau (au carré) : le mur d'en face plutôt qu'un coin éloigné.
                var score = int.MaxValue;
                foreach (var d in doors)
                    score = Mathf.Min(score, (d - s).sqrMagnitude);
                if (score > bestScore)
                {
                    bestScore = score;
                    throne = s;
                }
            }
            Add(throne, "trone");
            // Deux braseros encadrent le trône : le boss se découpe dans leur lumière.
            var along = new Vector2Int(throne.y == min.y || throne.y == max.y ? 1 : 0, throne.x == min.x || throne.x == max.x ? 1 : 0);
            Add(throne + along * 2, "brasero");
            Add(throne - along * 2, "brasero");
            // Le boss dort devant son trône (vers le centre).
            var toward = center - throne;
            bossCell = throne + new Vector2Int(System.Math.Sign(toward.x), System.Math.Sign(toward.y));
            if (!set.Contains(bossCell) || used.Contains(bossCell))
                bossCell = center;
            keep.Add(bossCell);

            // Le reste du bord : une case sur trois, cage ou tas d'os, au hasard.
            var edge = new List<Vector2Int>();
            foreach (var c in cells)
                if (OnEdge(c))
                    edge.Add(c);
            edge.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            var big = (max.x - min.x + 1) * (max.y - min.y + 1) >= 36;
            if (big)
                for (var i = 0; i < edge.Count; i++)
                    if (rng.Next(3) == 0 && !NextToDecor(edge[i]))
                        Add(edge[i], rng.Next(2) == 0 ? "cage" : "os");
            // Ossements de passage : une case libre sur huit, hors bord.
            foreach (var c in cells)
                if (!OnEdge(c) && !keep.Contains(c) && rng.Next(8) == 0)
                    Add(c, "ossements");
            // Chaînes qui pendent du plafond, au-dessus de la salle (on passe dessous) : une case sur dix.
            foreach (var c in cells)
                if (!OnEdge(c) && !used.Contains(c) && rng.Next(10) == 0)
                    Add(c, "chaines");
            return decor;
        }
    }
}
