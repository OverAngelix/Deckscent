using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Ce qui occupe une case du labyrinthe et joue au tour par tour : ennemis, pièges, coffres.
    /// Tous les objets actifs sont listés dans <see cref="All"/>, dans l'ordre où ils sont apparus ;
    /// c'est aussi l'ordre dans lequel ils jouent après le joueur (<see cref="TakeTurn"/>).
    ///
    /// Pas de physique : la case (<see cref="Cell"/>) fait foi, la position 3D suit.
    /// </summary>
    public abstract class GridEntity : MonoBehaviour
    {
        static readonly List<GridEntity> _all = new List<GridEntity>();

        /// <summary>Les objets de grille actifs, dans l'ordre d'apparition.</summary>
        public static IReadOnlyList<GridEntity> All => _all;

        /// <summary>Case occupée (x vers l'est, y vers le sud).</summary>
        public Vector2Int Cell { get; protected set; }
        public Maze Maze { get; private set; }
        /// <summary>Le joueur ne peut pas entrer sur la case (il attaque un ennemi, ouvre un coffre).</summary>
        public virtual bool BlocksPlayer => false;
        /// <summary>Faux une fois mort, ouvert… : ne joue plus, ne bloque plus, même pendant son animation de sortie.</summary>
        public bool IsActive { get; private set; }

        protected virtual void OnEnable()
        {
            IsActive = true;
            if (!_all.Contains(this))
                _all.Add(this);
        }

        protected virtual void OnDisable() => Retire();

        /// <summary>Quitte la liste tout de suite (mort, coffre vidé), avant l'animation de sortie.</summary>
        protected void Retire()
        {
            IsActive = false;
            _all.Remove(this);
        }

        /// <summary>Pose l'objet sur une case du labyrinthe (position et orientation de départ).</summary>
        public virtual void Place(Maze maze, Vector2Int cell)
        {
            Maze = maze;
            Cell = cell;
            transform.position = maze.CellToWorld(cell);
        }

        /// <summary>
        /// Phase de déplacement, avant les tours (<see cref="TakeTurn"/>) : la case où l'objet veut
        /// aller, ou null s'il reste. Tous les déplacements choisis sont joués **en même temps**.
        /// `occupied` : cases déjà prises (joueur, autres ennemis, cases déjà réservées ce tour),
        /// sans la sienne. Un objet qui se déplace ne joue pas son <see cref="TakeTurn"/> ce tour-là.
        /// </summary>
        public virtual Vector2Int? PlanMove(PlayerController player, ISet<Vector2Int> occupied) => null;

        /// <summary>Anime le déplacement choisi par <see cref="PlanMove"/> (en même temps que les autres).</summary>
        public virtual IEnumerator Move(Vector2Int cell)
        {
            Cell = cell;
            yield break;
        }

        /// <summary>
        /// Son tour, après chaque action du joueur et après les déplacements, l'un après l'autre
        /// (attaques, pièges). Peut durer (animations) : le monde attend. Rendre la main sans rien
        /// animer ne coûte aucune image.
        /// </summary>
        public virtual IEnumerator TakeTurn(PlayerController player)
        {
            yield break;
        }

        /// <summary>Le premier objet actif de type T sur cette case, ou null.</summary>
        public static T At<T>(Vector2Int cell) where T : GridEntity
        {
            foreach (var entity in _all)
                if (entity is T t && entity.IsActive && entity.Cell == cell)
                    return t;
            return null;
        }

        /// <summary>Les objets actifs de type T.</summary>
        public static List<T> AllOf<T>() where T : GridEntity
        {
            var list = new List<T>();
            foreach (var entity in _all)
                if (entity is T t && entity.IsActive)
                    list.Add(t);
            return list;
        }

        /// <summary>Une direction de grille vers un vecteur monde (pour les élans, les bonds).</summary>
        protected static Vector3 WorldDir(Vector2Int dir) => Grid.ToWorldDir(dir);

        /// <summary>Appelle `apply` avec t de 0 à 1 pendant `duration` secondes (temps du jeu : figé carte ouverte).</summary>
        protected static IEnumerator Animate(float duration, System.Action<float> apply)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                apply(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            apply(1f);
        }

        /// <summary>Ease sinusoïdal entrée-sortie.</summary>
        protected static float Smooth(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
    }
}
