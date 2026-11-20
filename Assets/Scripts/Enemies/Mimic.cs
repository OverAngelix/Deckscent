using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Mimique : un monstre qui se fait passer pour un coffre (classique du genre). Même coffre doré
    /// que les vrais ; seul indice, son couvercle **frémit** de temps en temps.
    ///
    /// - **Caché** : il ne joue pas et ne met pas le joueur « en combat ». Tenter de l'ouvrir
    ///   (Interact, ou avancer dessus comme sur un coffre) : il se révèle et **mord** aussitôt.
    ///   Le frapper le révèle aussi (sans morsure gratuite).
    /// - **Révélé** : gueule ouverte, langue rouge (accent) ; il bondit vers le joueur (poursuite
    ///   courte) et mord à côté de lui, comme un slime.
    /// Apparaît à la place d'un coffre de salle (<see cref="Spawner"/>, `Mimic Chance`). Compte comme
    /// un monstre pour la mission.
    /// </summary>
    public class Mimic : Enemy, IInteractable
    {
        [Header("Comportement")]
        [Tooltip("Distance de poursuite une fois révélé (en pas, murs contournés).")]
        [SerializeField, Range(1, 20)] int chaseRange = 5;
        [Tooltip("Dégâts de la morsure de surprise (quand on essaie de l'ouvrir).")]
        [SerializeField, Range(0, 20)] int ambushDamage = 3;
        [Tooltip("Durée d'un bond ou d'une morsure (secondes).")]
        [SerializeField, Min(0.01f)] float moveTime = 0.25f;

        [Header("Apparence")]
        [Tooltip("Les couvercles (décor Synty et secours), pivot sur la charnière.")]
        [SerializeField] Transform[] lids = new Transform[0];
        [Tooltip("Dents et langue (dents du bas et langue dans la caisse, crocs sous le couvercle) : cachées tant qu'il fait le coffre.")]
        [SerializeField] GameObject[] maw = new GameObject[0];
        [Tooltip("Angle de la gueule ouverte (degrés, autour de l'axe X du couvercle).")]
        [SerializeField] float openAngle = -85f;
        [Tooltip("Tous les combien de secondes, en moyenne, son couvercle frémit (l'indice).")]
        [SerializeField, Min(0.5f)] float twitchEvery = 5f;

        [Header("Sons")]
        [SerializeField] SoundEmitter sounds;

        /// <summary>Une mimique vient de se révéler.</summary>
        public static event Action<Mimic> Revealed;

        public bool IsHidden { get; private set; } = true;
        public override bool Threatens => IsActive && !IsHidden;
        public bool CanInteract => IsActive && IsHidden;

        Quaternion[] _rest;
        float _twitchAt;
        Transform _player;
        bool _dying;
        Coroutine _twitch;

        protected override void Awake()
        {
            base.Awake();
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
            _rest = new Quaternion[lids.Length];
            for (var i = 0; i < lids.Length; i++)
                _rest[i] = lids[i] != null ? lids[i].localRotation : Quaternion.identity;
            ShowMaw(false);
            _twitchAt = Time.time + UnityEngine.Random.Range(1f, twitchEvery * 2f);
        }

        void ShowMaw(bool show)
        {
            foreach (var part in maw)
                if (part != null)
                    part.SetActive(show);
        }

        void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                _player = player.transform;
        }

        /// <summary>Posé comme un coffre : face à un passage.</summary>
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

        void Update()
        {
            if (_dying)
                return;
            if (IsHidden)
            {
                // L'indice : un petit frémissement du couvercle, de temps en temps.
                if (Time.time >= _twitchAt)
                {
                    _twitchAt = Time.time + UnityEngine.Random.Range(twitchEvery * 0.5f, twitchEvery * 1.5f);
                    _twitch = StartCoroutine(Twitch());
                }
                return;
            }
            if (_player == null)
                return;
            var to = _player.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Mathf.Min(1f, 6f * Time.deltaTime));
        }

        IEnumerator Twitch()
        {
            yield return Animate(0.25f, t => SetLid(-7f * Mathf.Sin(t * Mathf.PI * 2f)));
            SetLid(0f);
        }

        void SetLid(float angle)
        {
            for (var i = 0; i < lids.Length; i++)
                if (lids[i] != null)
                    lids[i].localRotation = _rest[i] * Quaternion.Euler(angle, 0f, 0f);
        }

        /// <summary>On essaie de l'ouvrir : il se révèle et mord.</summary>
        public IEnumerator Interact(PlayerController player)
        {
            if (!CanInteract)
                yield break;
            yield return Reveal();
            yield return Bite(player, player.Cell - Cell, ambushDamage);
        }

        IEnumerator Reveal()
        {
            if (!IsHidden)
                yield break;
            IsHidden = false;
            // Seulement le frémissement : StopAllCoroutines arrêterait aussi cette révélation.
            if (_twitch != null)
                StopCoroutine(_twitch);
            ShowMaw(true);
            sounds?.Play(SoundBank.Growl, 0.9f);
            FindAnyObjectByType<PlayerController>()?.Notify(Loc.T("message.mimique"));
            Revealed?.Invoke(this);
            yield return Animate(0.2f, t => SetLid(openAngle * t));
        }

        protected override void OnHurt()
        {
            if (IsHidden && isActiveAndEnabled)
                StartCoroutine(Reveal());
        }

        bool CanReach(PlayerController player)
        {
            var dir = Array.IndexOf(Grid.Dirs, player.Cell - Cell);
            return dir >= 0 && Maze.CanStep(Cell, dir);
        }

        public override Vector2Int? PlanMove(PlayerController player, ISet<Vector2Int> occupied)
        {
            if (IsHidden || _dying || Maze == null || CanReach(player))
                return null;
            var path = GridPath.Find(Maze, Cell, player.Cell, chaseRange, occupied);
            return path.Count >= 2 && !occupied.Contains(path[0]) ? path[0] : (Vector2Int?)null;
        }

        public override IEnumerator Move(Vector2Int cell)
        {
            Cell = cell;
            var start = transform.position;
            var end = Maze.CellToWorld(cell);
            yield return Animate(moveTime, t =>
            {
                transform.position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.5f;
                SetLid(openAngle * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI)));
            });
        }

        public override IEnumerator TakeTurn(PlayerController player)
        {
            if (IsHidden || _dying || Maze == null || !CanReach(player))
                yield break;
            yield return Bite(player, player.Cell - Cell, damage);
        }

        IEnumerator Bite(PlayerController player, Vector2Int dir, int amount)
        {
            var start = transform.position;
            var end = start + WorldDir(dir) * 0.8f;
            yield return Animate(moveTime * 0.5f, t =>
            {
                transform.position = Vector3.Lerp(start, end, t);
                SetLid(openAngle * (1f - t * 0.9f));
            });
            if (IsActive)
                player.TakeDamage(amount);
            yield return Animate(moveTime * 0.5f, t =>
            {
                transform.position = Vector3.Lerp(end, start, Smooth(t));
                SetLid(openAngle * (0.1f + 0.9f * t));
            });
        }

        protected override void OnDeath()
        {
            _dying = true;
            Fx.Play(Fx.Get(l => l.puff), transform.position + Vector3.up * 0.3f);
            StartCoroutine(Sink());
        }

        IEnumerator Sink()
        {
            var from = transform.position;
            yield return Animate(0.5f, t =>
            {
                transform.position = from + Vector3.down * t * t;
                SetLid(openAngle * (1f - t));
            });
            Destroy(gameObject);
        }
    }
}
