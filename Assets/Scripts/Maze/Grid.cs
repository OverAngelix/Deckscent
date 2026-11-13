using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Conventions de la grille, partagées par le labyrinthe, le joueur et plus tard les tuiles.
    /// Une case (x, y) du plan : x vers l'est, y vers le sud (ligne 0 = tout au nord).
    /// Dans le monde : le nord est +Z (l'avant d'Unity), l'est +X.
    /// </summary>
    public static class Grid
    {
        /// <summary>Directions dans l'ordre nord, est, sud, ouest (indice = orientation).</summary>
        public static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0),
        };

        /// <summary>Une direction de la grille en vecteur monde (y de la grille = -Z).</summary>
        public static Vector3 ToWorldDir(Vector2Int dir) => new Vector3(dir.x, 0f, -dir.y);

        /// <summary>Angle (degrés, autour de Y) d'une orientation : nord 0, est 90, sud 180, ouest 270.</summary>
        public static float Yaw(int facing) => facing * 90f;
    }
}
