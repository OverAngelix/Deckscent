using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Fontaine de pierre (sanctuaire du GDD §5, « tuile sûre ») : contenu rare d'un couloir
    /// (« sanctuaire »). Son eau est bleue et luit (accent : elle sort du papier). Le joueur boit en
    /// s'arrêtant sur sa case : des PV, une seule fois ; l'eau se tarit (elle repasse au gris).
    /// Elle est adossée à un mur de la case et ne bloque pas le passage.
    /// </summary>
    public class Shrine : GridEntity
    {
        [Tooltip("PV rendus en buvant.")]
        [SerializeField, Range(0, 20)] int heal = 4;
        [Tooltip("L'eau (rendu coloré, saturé), qui passe au gris une fois bue.")]
        [SerializeField] Renderer water;
        [Tooltip("La lueur de l'eau (lumière blanche), éteinte une fois bue.")]
        [SerializeField] Light glow;
        [Tooltip("Ce qui joue le bruit de l'eau. Vide : cherché sur l'objet.")]
        [SerializeField] SoundEmitter sounds;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Glow = Shader.PropertyToID("_Glow");

        /// <summary>Le joueur a bu à une fontaine.</summary>
        public static event System.Action<Shrine> Drunk;

        public bool Used { get; private set; }
        public int Heal => heal;

        void Awake()
        {
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
        }

        /// <summary>Le joueur s'est arrêté sur la case : il boit (une fois).</summary>
        public override IEnumerator TakeTurn(PlayerController player)
        {
            if (Used || player.Cell != Cell)
                yield break;
            Used = true;
            player.Heal(heal);
            player.Notify(Loc.F("message.fontaine", heal));
            sounds?.Play(SoundBank.Drink, 0.8f);
            Drunk?.Invoke(this);
            if (glow != null)
                glow.enabled = false;
            if (water != null)
            {
                var block = new MaterialPropertyBlock();
                water.GetPropertyBlock(block);
                block.SetColor(BaseColor, new Color(0.35f, 0.35f, 0.35f));
                block.SetFloat(Glow, 0f);
                water.SetPropertyBlock(block);
            }
            yield return null;
        }

        /// <summary>Adossée au premier mur de sa case (s'il y en a un), tournée vers le passage.</summary>
        public override void Place(Maze maze, Vector2Int cell)
        {
            base.Place(maze, cell);
            for (var d = 0; d < Grid.Dirs.Length; d++)
            {
                if (maze.CanStep(cell, d))
                    continue;
                var back = Grid.ToWorldDir(Grid.Dirs[d]);
                transform.rotation = Quaternion.LookRotation(-back);
                transform.position += back * (maze.CellSize * 0.5f - 0.75f);
                return;
            }
        }
    }
}
