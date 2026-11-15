using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Scène du menu : une salle du donjon rendue comme en jeu (même post-process gravure, même
    /// ambiance, une lanterne), la caméra qui tourne lentement sur elle-même au centre de la
    /// salle, la musique synthétisée, et l'écran titre (<see cref="MenuScreen"/>) par-dessus.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("La salle du menu (son plan est dans le Maze). La caméra se place sur sa case P.")]
        [SerializeField] Maze maze;
        [Tooltip("La caméra (avec sa lanterne) qui tourne au centre de la salle.")]
        [SerializeField] Transform cameraRig;
        [Tooltip("Musique du menu, coupée en fondu quand on descend.")]
        [SerializeField] MenuMusic music;

        [Header("Caméra")]
        [Tooltip("Secondes pour un tour complet sur elle-même.")]
        [SerializeField, Min(1f)] float turnPeriod = 140f;
        [Tooltip("Amplitude de la respiration de la caméra (mètres).")]
        [SerializeField, Min(0f)] float bobAmount = 0.03f;
        [Tooltip("Période de la respiration (secondes).")]
        [SerializeField, Min(0.1f)] float bobPeriod = 4f;
        [Tooltip("Inclinaison vers le bas (degrés).")]
        [SerializeField, Range(-30f, 30f)] float pitch = 4f;
        [Tooltip("Hauteur des yeux (mètres), comme le joueur.")]
        [SerializeField, Min(0f)] float eyeHeight = 1.6f;

        Vector3 _base;
        float _time;

        void Start()
        {
            if (maze != null)
                _base = maze.CellToWorld(maze.StartCell) + Vector3.up * eyeHeight;
            Place();
        }

        void Update()
        {
            _time += Time.unscaledDeltaTime;
            Place();
        }

        void Place()
        {
            if (cameraRig == null)
                return;
            var yaw = 360f * _time / turnPeriod - 45f;
            cameraRig.SetPositionAndRotation(
                _base + Vector3.up * (Mathf.Sin(2f * Mathf.PI * _time / bobPeriod) * bobAmount),
                Quaternion.Euler(pitch, yaw, 0f));
        }

        /// <summary>Début du fondu au noir avant la partie : la musique s'éteint en même temps.</summary>
        public void PrepareStart(float duration)
        {
            if (music != null)
                music.FadeOut(duration);
        }
    }
}
