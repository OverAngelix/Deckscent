using System;
using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Coffre : apparaît dans les tuiles « trésor ». On l'ouvre en avançant dessus (droit devant)
    /// ou avec l'action Interact ; ça coûte un tour. Butin : une carte de plus dans la main, ou des
    /// PV. Une fois vidé, il s'enfonce dans le sol et libère sa case.
    ///
    /// Doré (couleur saturée) : le post-process gravure le garde en couleur (accent). Il ne bloque
    /// que le joueur : un monstre peut se poser sur sa case, et le cache tant qu'il y est (avancer
    /// vers la case attaque alors le monstre).
    /// </summary>
    public class Chest : GridEntity, IInteractable
    {
        [Header("Butin")]
        [Tooltip("PV rendus quand le butin est des PV.")]
        [SerializeField, Range(0, 20)] int healAmount = 3;
        [Tooltip("Chance que le butin soit une carte (sinon des PV). PV au maximum : une carte ; main pleine (ou pas de pioche) : des PV.")]
        [SerializeField, Range(0f, 1f)] float cardChance = 0.5f;

        [Header("Animation")]
        [Tooltip("Les couvercles, pivot sur leur charnière (celui du décor Synty et celui des formes de secours : seul l'un des deux est affiché).")]
        [SerializeField] Transform[] lids = new Transform[0];
        [Tooltip("Angle d'ouverture du couvercle (degrés, autour de son axe X).")]
        [SerializeField] float openAngle = -105f;
        [Tooltip("Durée de l'ouverture (secondes).")]
        [SerializeField, Min(0.01f)] float openTime = 0.35f;
        [Tooltip("Temps pendant lequel le coffre vide reste visible avant de s'enfoncer (secondes).")]
        [SerializeField, Min(0f)] float vanishDelay = 0.8f;
        [Tooltip("Profondeur à laquelle il s'enfonce dans le sol (mètres).")]
        [SerializeField, Min(0f)] float sinkDepth = 1f;

        /// <summary>Émis à l'ouverture (salles nettoyées, à venir).</summary>
        public event Action<Chest> Cleared;

        public bool Opened { get; private set; }
        public override bool BlocksPlayer => !Opened;
        public bool CanInteract => !Opened && IsActive;
        /// <summary>La pioche, pour le butin « carte ». Null : toujours des PV.</summary>
        public TileDeck Deck { get; set; }
        public float CardChance { get => cardChance; set => cardChance = Mathf.Clamp01(value); }
        public int HealAmount => healAmount;

        Renderer[] _renderers;

        void Awake() => _renderers = GetComponentsInChildren<Renderer>(true);

        void Update()
        {
            // Un monstre sur la case cache le coffre.
            var covered = At<Enemy>(Cell) != null;
            foreach (var r in _renderers)
                if (r != null && r.enabled == covered)
                    r.enabled = !covered;
        }

        /// <summary>Recale le coffre sur sa case et tourne son devant vers un passage ouvert.</summary>
        public override void Place(Maze maze, Vector2Int cell)
        {
            base.Place(maze, cell);
            for (var i = 0; i < Grid.Dirs.Length; i++)
                if (maze.CanStep(cell, i))
                {
                    transform.rotation = Quaternion.Euler(0f, Grid.Yaw(i), 0f);
                    break;
                }
        }

        public IEnumerator Interact(PlayerController player)
        {
            if (!CanInteract)
                yield break;
            Opened = true;
            var from = new Quaternion[lids.Length];
            for (var i = 0; i < lids.Length; i++)
                from[i] = lids[i] != null ? lids[i].localRotation : Quaternion.identity;
            var open = Quaternion.Euler(openAngle, 0f, 0f);
            yield return Animate(openTime, t =>
            {
                for (var i = 0; i < lids.Length; i++)
                    if (lids[i] != null)
                        lids[i].localRotation = Quaternion.SlerpUnclamped(from[i], from[i] * open, BackOut(t));
            });
            GiveLoot(player);
            Retire();
            Cleared?.Invoke(this);
            StartCoroutine(Vanish());
        }

        void GiveLoot(PlayerController player)
        {
            var wantCard = UnityEngine.Random.value < cardChance || player.Health >= player.MaxHealth;
            if (wantCard && Deck != null && Deck.AddCard())
                player.Notify("Coffre : une carte de plus dans la main");
            else
            {
                player.Heal(healAmount);
                player.Notify($"Coffre : +{healAmount} PV");
            }
        }

        /// <summary>Le coffre vide reste un instant, puis s'enfonce dans le sol.</summary>
        IEnumerator Vanish()
        {
            yield return new WaitForSeconds(vanishDelay);
            var from = transform.position;
            var to = from + Vector3.down * sinkDepth;
            yield return Animate(0.5f, t => transform.position = Vector3.Lerp(from, to, t * t));
            Destroy(gameObject);
        }

        static float BackOut(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
