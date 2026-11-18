using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Louveteau, appelé par le loup-garou à la moitié de ses PV : petit, rapide à tuer, il fonce
    /// sur le joueur et mord. Même modèle que le boss en plus petit (<see cref="WerewolfRig"/>).
    /// Il meurt avec le boss (la meute se disperse).
    /// </summary>
    public class WolfCub : Enemy
    {
        [Header("Comportement")]
        [Tooltip("Distance de poursuite (en pas, murs contournés).")]
        [SerializeField, Range(1, 40)] int chaseRange = 20;
        [Tooltip("Durée d'un bond ou d'une morsure (secondes).")]
        [SerializeField, Min(0.01f)] float moveTime = 0.2f;

        [Header("Animation")]
        [Tooltip("Les poses du modèle.")]
        [SerializeField] WerewolfRig rig;
        [Tooltip("Rendus blanchis quand il est touché.")]
        [SerializeField] Renderer[] tinted;

        [Header("Sons")]
        [SerializeField] SoundEmitter sounds;

        static readonly int Glow = Shader.PropertyToID("_Glow");
        Transform _player;
        MaterialPropertyBlock _block;
        bool _dying;

        protected override void Awake()
        {
            base.Awake();
            if (rig == null)
                rig = GetComponentInChildren<WerewolfRig>();
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
        }

        void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                _player = player.transform;
        }

        void Update()
        {
            if (_player == null || _dying)
                return;
            var to = _player.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Mathf.Min(1f, 6f * Time.deltaTime));
        }

        bool CanReach(PlayerController player)
        {
            var dir = Array.IndexOf(Grid.Dirs, player.Cell - Cell);
            return dir >= 0 && Maze.CanStep(Cell, dir);
        }

        public override Vector2Int? PlanMove(PlayerController player, ISet<Vector2Int> occupied)
        {
            if (_dying || Maze == null || CanReach(player))
                return null;
            var path = GridPath.Find(Maze, Cell, player.Cell, chaseRange, occupied);
            if (path.Count < 2)
                path = GridPath.Find(Maze, Cell, player.Cell, chaseRange);
            return path.Count >= 2 && !occupied.Contains(path[0]) ? path[0] : (Vector2Int?)null;
        }

        public override IEnumerator Move(Vector2Int cell)
        {
            Cell = cell;
            var start = transform.position;
            var end = Maze.CellToWorld(cell);
            yield return Animate(moveTime, t =>
                transform.position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.25f);
        }

        public override IEnumerator TakeTurn(PlayerController player)
        {
            if (_dying || Maze == null || !CanReach(player))
                yield break;
            var start = transform.position;
            var end = start + WorldDir(player.Cell - Cell) * 0.8f;
            rig?.SetPose(WerewolfRig.Pose.Griffer, 0.08f);
            sounds?.Play(SoundBank.Growl, 0.5f);
            yield return Animate(moveTime * 0.5f, t => transform.position = Vector3.Lerp(start, end, t));
            if (IsActive)
                player.TakeDamage(damage);
            yield return Animate(moveTime * 0.5f, t => transform.position = Vector3.Lerp(end, start, Smooth(t)));
            rig?.SetPose(WerewolfRig.Pose.Garde, 0.2f);
        }

        /// <summary>La meute se disperse : il meurt sans attendre de coup (mort du boss).</summary>
        public void Flee() => TakeDamage(Health + Defense + 99);

        protected override void OnHurt()
        {
            if (isActiveAndEnabled)
                StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            SetGlow(0.9f);
            yield return new WaitForSeconds(0.12f);
            if (!_dying)
                SetGlow(-1f);
        }

        void SetGlow(float glow)
        {
            if (tinted == null)
                return;
            _block ??= new MaterialPropertyBlock();
            foreach (var r in tinted)
            {
                if (r == null)
                    continue;
                if (glow < 0f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                r.GetPropertyBlock(_block);
                _block.SetFloat(Glow, glow);
                r.SetPropertyBlock(_block);
            }
        }

        protected override void OnDeath()
        {
            _dying = true;
            rig?.SetPose(WerewolfRig.Pose.Mort, 0.2f);
            StartCoroutine(Shrink());
        }

        IEnumerator Shrink()
        {
            var from = transform.localScale;
            yield return Animate(0.4f, t => transform.localScale = from * (1f - t));
            Destroy(gameObject);
        }
    }
}
