using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Pièce du décor du camp posée sur une case (feu de camp, tente, puits…) : elle bloque le
    /// joueur, ne joue pas. Se place sur sa case au lancement, en gardant son orientation.
    /// </summary>
    public class CampObstacle : GridEntity
    {
        [Tooltip("Case occupée (x vers l'est, y vers le sud), dans le plan du labyrinthe du camp.")]
        [SerializeField] Vector2Int cell;
        [Tooltip("Coché : bloque la case. Décoché : décor de passage.")]
        [SerializeField] bool blocks = true;

        public override bool BlocksPlayer => blocks;

        /// <summary>Case visée (réglée par le constructeur du camp).</summary>
        public Vector2Int TargetCell { get => cell; set => cell = value; }

        protected virtual void Start()
        {
            var maze = FindAnyObjectByType<Maze>();
            if (maze != null)
                Place(maze, cell);
        }
    }
}
