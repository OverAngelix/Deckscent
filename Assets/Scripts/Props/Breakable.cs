using System.Collections;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Vase ou caisse à briser, dans les coins des salles (<see cref="Ambience"/>). Il bloque sa
    /// case ; on le brise en avançant dessus (comme une attaque, un tour). Parfois il cache quelque
    /// chose : une potion (PV) ou une carte. Pas un ennemi : il ne compte pas dans la mission.
    /// </summary>
    public class Breakable : GridEntity, IDamageable
    {
        [Header("Butin")]
        [Tooltip("Chance qu'il cache une potion (PV).")]
        [SerializeField, Range(0f, 1f)] float potionChance = 0.25f;
        [Tooltip("PV rendus par la potion.")]
        [SerializeField, Range(0, 10)] int potionHeal = 2;
        [Tooltip("Chance qu'il cache une carte (si la potion n'est pas sortie et que la main n'est pas pleine).")]
        [SerializeField, Range(0f, 1f)] float cardChance = 0.08f;

        [Header("Animation")]
        [Tooltip("Ce qui se brise (le modèle) : il disparaît d'un coup, remplacé par des éclats.")]
        [SerializeField] Transform body;
        [Tooltip("Nombre d'éclats quand il explose.")]
        [SerializeField, Range(0, 30)] int fragments = 12;
        [Tooltip("Vitesse maximale des éclats (m/s).")]
        [SerializeField, Min(0.5f)] float burst = 4f;
        [Tooltip("Durée de vie des éclats, avant de disparaître (secondes).")]
        [SerializeField, Min(0.1f)] float debrisTime = 1.2f;
        [Tooltip("Coché : du bois (éclats de bois), sinon de la terre cuite (poussière).")]
        [SerializeField] bool wooden;
        [Tooltip("Ce qui joue le bruit de casse. Vide : cherché sur l'objet.")]
        [SerializeField] SoundEmitter sounds;

        /// <summary>Un vase ou une caisse vient d'être brisé (bilan, tests).</summary>
        public static event System.Action<Breakable> Broken;

        public override bool BlocksPlayer => IsActive;
        public TileDeck Deck { get; set; }
        public bool IsBroken { get; private set; }

        void Awake()
        {
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
            if (body == null && transform.childCount > 0)
                body = transform.GetChild(0);
        }

        public void TakeDamage(int amount)
        {
            if (!IsActive || amount <= 0)
                return;
            IsBroken = true;
            Retire();
            Broken?.Invoke(this);
            sounds?.Play(SoundBank.Shatter, 0.9f);
            Fx.Play(Fx.Get(l => wooden ? l.crateBreak : l.vaseBreak), transform.position + Vector3.up * 0.5f);
            GiveLoot();
            StartCoroutine(Shatter());
        }

        void GiveLoot()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;
            var roll = Random.value;
            if (roll < potionChance && player.Health < player.MaxHealth)
            {
                player.Heal(potionHeal);
                player.Notify(Loc.F("message.vase_potion", potionHeal));
            }
            else if (roll < potionChance + cardChance && Deck != null && Deck.AddCard())
                player.Notify(Loc.T("message.vase_carte"));
        }

        /// <summary>
        /// Il explose : le modèle disparaît d'un coup, des éclats (de sa matière) jaillissent, retombent,
        /// rebondissent au sol, puis rapetissent jusqu'à disparaître.
        /// </summary>
        IEnumerator Shatter()
        {
            var target = body != null ? body : transform;
            var source = target.GetComponentInChildren<Renderer>();
            var material = source != null ? source.sharedMaterial : null;
            var center = source != null ? source.bounds.center : transform.position + Vector3.up * 0.5f;
            target.gameObject.SetActive(false);

            var pieces = new Transform[fragments];
            var velocity = new Vector3[fragments];
            var spin = new Vector3[fragments];
            for (var i = 0; i < fragments; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(piece.GetComponent<Collider>());
                piece.name = "Éclat";
                piece.transform.SetParent(transform, true);
                piece.transform.position = center + Random.insideUnitSphere * 0.2f;
                piece.transform.rotation = Random.rotation;
                piece.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);
                if (material != null)
                    piece.GetComponent<MeshRenderer>().sharedMaterial = material;
                pieces[i] = piece.transform;
                var flat = Random.insideUnitCircle.normalized * Random.Range(1.5f, burst);
                velocity[i] = new Vector3(flat.x, Random.Range(1.5f, burst), flat.y);
                spin[i] = Random.insideUnitSphere * 720f;
            }
            var sizes = new Vector3[fragments];
            for (var i = 0; i < fragments; i++)
                sizes[i] = pieces[i].localScale;
            var floor = transform.position.y + 0.04f;
            // Les éclats restent dans leur case : ils rebondissent sur les murs au lieu de les traverser.
            var cellCenter = Maze != null ? Maze.CellToWorld(Cell) : transform.position;
            var half = (Maze != null ? Maze.CellSize : 3f) * 0.5f - 0.1f;
            for (var t = 0f; t < debrisTime; t += Time.deltaTime)
            {
                var dt = Time.deltaTime;
                var shrink = Mathf.Clamp01((debrisTime - t) / (debrisTime * 0.4f));
                for (var i = 0; i < fragments; i++)
                {
                    velocity[i] += Vector3.down * 12f * dt;
                    var p = pieces[i].position + velocity[i] * dt;
                    if (p.y < floor)
                    {
                        // Rebond amorti, puis il glisse et s'arrête.
                        p.y = floor;
                        velocity[i] = new Vector3(velocity[i].x * 0.5f, -velocity[i].y * 0.35f, velocity[i].z * 0.5f);
                        spin[i] *= 0.5f;
                    }
                    if (Mathf.Abs(p.x - cellCenter.x) > half)
                    {
                        p.x = cellCenter.x + Mathf.Sign(p.x - cellCenter.x) * half;
                        velocity[i].x *= -0.4f;
                    }
                    if (Mathf.Abs(p.z - cellCenter.z) > half)
                    {
                        p.z = cellCenter.z + Mathf.Sign(p.z - cellCenter.z) * half;
                        velocity[i].z *= -0.4f;
                    }
                    pieces[i].position = p;
                    pieces[i].Rotate(spin[i] * dt, Space.World);
                    pieces[i].localScale = sizes[i] * shrink;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
