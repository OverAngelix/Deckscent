using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Ennemi simple : un slime qui rebondit et suit le joueur du regard. Sa couleur est saturée :
    /// le post-process gravure la garde (accent). Trois sortes : vert (3 PV), rouge (6 PV),
    /// bleu (2 d'attaque), choisies avec <see cref="variant"/>.
    ///
    /// À son tour : il attaque le joueur s'il est sur une case voisine (sans mur entre eux), sinon
    /// s'en approche d'une case s'il est à moins de <see cref="aggroRange"/> pas (murs contournés),
    /// en même temps que les autres monstres (<see cref="PlanMove"/>).
    /// </summary>
    public class Slime : Enemy
    {
        public enum Variant { Vert, Rouge, Bleu }

        [Serializable]
        public struct VariantDef
        {
            [Tooltip("Couleur du corps. Doit rester saturée pour sortir du papier (accent du post-process).")]
            public Color color;
            [Range(1, 99)] public int maxHealth;
            [Range(0, 20)] public int damage;
        }

        [Header("Sorte")]
        [Tooltip("Vert : 3 PV, 1 d'attaque. Rouge : 6 PV. Bleu : 2 d'attaque. Applique la couleur, les PV et l'attaque de la table ci-dessous.")]
        [SerializeField] Variant variant = Variant.Vert;
        [Tooltip("Couleur et stats de chaque sorte, dans l'ordre Vert, Rouge, Bleu.")]
        [SerializeField] VariantDef[] variants =
        {
            new VariantDef { color = new Color(0.25f, 0.85f, 0.2f), maxHealth = 3, damage = 1 },
            new VariantDef { color = new Color(0.9f, 0.14f, 0.1f), maxHealth = 6, damage = 1 },
            new VariantDef { color = new Color(0.15f, 0.4f, 0.95f), maxHealth = 3, damage = 2 },
        };

        [Header("Comportement")]
        [Tooltip("Distance (en pas, murs contournés) à laquelle il repère le joueur et le suit. 0 = ne bouge jamais (attaque quand même si on est à côté).")]
        [SerializeField, Range(0, 20)] int aggroRange = 6;
        [Tooltip("Durée d'un bond ou d'une attaque (secondes).")]
        [SerializeField, Min(0.01f)] float moveTime = 0.25f;

        [Header("Apparence")]
        [Tooltip("Le corps qui s'écrase et s'étire (enfant).")]
        [SerializeField] Transform body;
        [Tooltip("Le rendu de la gelée, coloré selon la sorte.")]
        [SerializeField] Renderer jelly;
        [Tooltip("Rebonds par seconde.")]
        [SerializeField, Min(0f)] float bounceSpeed = 1.6f;
        [Tooltip("Écrasement / étirement du corps (0 = immobile).")]
        [SerializeField, Range(0f, 0.5f)] float squash = 0.15f;
        [Tooltip("Vitesse à laquelle il se tourne vers le joueur.")]
        [SerializeField, Min(0f)] float turnSpeed = 4f;
        [Tooltip("Durée du flash blanc quand il est touché (secondes).")]
        [SerializeField, Min(0f)] float hitFlash = 0.12f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public Variant Kind => variant;
        public Color BodyColor => VariantIndexValid ? variants[(int)variant].color : Color.green;

        MaterialPropertyBlock _block;
        Transform _player;
        float _time;
        bool _dying;

        bool VariantIndexValid => variants != null && (int)variant < variants.Length;

        protected override void Awake()
        {
            ApplyVariant();
            base.Awake();
        }

        /// <summary>Change de sorte (couleur, PV, attaque). À appeler avant qu'il ne joue.</summary>
        public void SetVariant(Variant value)
        {
            variant = value;
            ApplyVariant();
            Health = maxHealth;
        }

        void ApplyVariant()
        {
            if (!VariantIndexValid)
                return;
            var def = variants[(int)variant];
            maxHealth = def.maxHealth;
            damage = def.damage;
            SetColor(def.color);
        }

        void OnValidate() => ApplyVariant();

        void SetColor(Color color)
        {
            if (jelly == null)
                return;
            _block ??= new MaterialPropertyBlock();
            jelly.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            jelly.SetPropertyBlock(_block);
        }

        void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                _player = player.transform;
        }

        void Update()
        {
            if (body == null || _dying)
                return;
            _time += Time.deltaTime;
            // Écrase et étire le corps en gardant son volume à peu près constant.
            var s = Mathf.Sin(_time * bounceSpeed * Mathf.PI * 2f) * squash;
            body.localScale = new Vector3(1f - s * 0.5f, 1f + s, 1f - s * 0.5f);

            if (_player != null)
            {
                var to = _player.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to),
                        Mathf.Min(1f, turnSpeed * Time.deltaTime));
            }
        }

        public override void Place(Maze maze, Vector2Int cell)
        {
            base.Place(maze, cell);
            _time = UnityEngine.Random.value * 10f; // rebonds désynchronisés
        }

        // --- Tour par tour ---------------------------------------------------------

        /// <summary>Peut-il frapper le joueur d'ici (case voisine, sans mur fin entre eux) ?</summary>
        bool CanReach(PlayerController player)
        {
            var dir = Array.IndexOf(Grid.Dirs, player.Cell - Cell);
            return dir >= 0 && Maze.CanStep(Cell, dir);
        }

        /// <summary>
        /// Phase de déplacement : s'il ne peut pas frapper, un bond d'une case vers le joueur s'il le
        /// repère, en évitant les cases déjà prises (deux slimes ne se marchent jamais dessus).
        /// </summary>
        public override Vector2Int? PlanMove(PlayerController player, ISet<Vector2Int> occupied)
        {
            if (Maze == null || !Maze.IsWalkable(Cell) || aggroRange <= 0 || CanReach(player))
                return null;
            // D'abord un chemin qui contourne les autres monstres ; sinon (couloir bouché par celui
            // de devant), le chemin à travers eux, si sa première case est libre : une file avance d'un bloc.
            var path = GridPath.Find(Maze, Cell, player.Cell, aggroRange, occupied);
            if (path.Count < 2)
                path = GridPath.Find(Maze, Cell, player.Cell, aggroRange);
            return path.Count >= 2 && !occupied.Contains(path[0]) ? path[0] : (Vector2Int?)null;
        }

        public override IEnumerator Move(Vector2Int cell) => Hop(cell);

        /// <summary>Son tour, après les déplacements : il attaque s'il est à côté du joueur.</summary>
        public override IEnumerator TakeTurn(PlayerController player)
        {
            if (Maze == null || !Maze.IsWalkable(Cell) || !CanReach(player))
                yield break;
            yield return AttackPlayer(player, player.Cell - Cell);
        }

        IEnumerator AttackPlayer(PlayerController player, Vector2Int dir)
        {
            var start = transform.position;
            var end = start + WorldDir(dir) * 0.9f;
            yield return Animate(moveTime * 0.5f, t => transform.position = Vector3.Lerp(start, end, BackOut(t)));
            if (IsActive)
                player.TakeDamage(damage);
            yield return Animate(moveTime * 0.5f, t => transform.position = Vector3.Lerp(end, start, Smooth(t)));
            transform.position = start;
        }

        /// <summary>Un bond d'une case (petit saut en arc).</summary>
        IEnumerator Hop(Vector2Int target)
        {
            Cell = target;
            var start = transform.position;
            var end = Maze.CellToWorld(target);
            yield return Animate(moveTime, t =>
                transform.position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.4f);
        }

        // --- Coups et mort ---------------------------------------------------------

        protected override void OnHurt()
        {
            if (isActiveAndEnabled)
                StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            SetColor(Color.white);
            yield return new WaitForSeconds(hitFlash);
            if (!_dying)
                SetColor(BodyColor);
        }

        /// <summary>Il blanchit, s'étale en flaque et disparaît.</summary>
        protected override void OnDeath()
        {
            _dying = true;
            SetColor(Color.white);
            StartCoroutine(Melt());
        }

        IEnumerator Melt()
        {
            var from = transform.localScale;
            var to = new Vector3(from.x * 1.6f, from.y * 0.05f, from.z * 1.6f);
            yield return Animate(0.35f, t => transform.localScale = Vector3.Lerp(from, to, t * t));
            Destroy(gameObject);
        }

        /// <summary>Ease « back out » : dépasse un peu puis revient (coup sec).</summary>
        static float BackOut(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
