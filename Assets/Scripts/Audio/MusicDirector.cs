using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Chef d'orchestre de la musique du donjon : regarde la situation du joueur et dit au musicien
    /// (<see cref="DungeonMusic"/>) quelles couches jouer, et à quel point.
    ///
    /// - **Menace** : des monstres en vue (à moins de <see cref="sightRadius"/> cases, sans mur
    ///   entre eux et le joueur). Plus il y en a, plus elle monte. Qu'ils soient devant ou derrière :
    ///   sinon, se retourner ferait changer la musique.
    /// - **Combat** : un coup échangé (le joueur frappe ou est touché). Dure tant qu'il y a des coups,
    ///   puis s'arrête après <see cref="calmTurns"/> tours sans coup.
    /// - **Coffre** : un coffre fermé en vue à moins de <see cref="chestRadius"/> cases, seulement
    ///   sans menace ni combat (légère variation).
    /// - **Boss** : <see cref="BossMode"/>, allumé par le boss (à venir) ; forte variation.
    ///
    /// Le musicien fait glisser chaque couche vers sa cible et cale les entrées sur le rythme.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [Header("Menace")]
        [Tooltip("Distance (en cases) à laquelle un monstre en vue déclenche la menace.")]
        [SerializeField, Range(1, 12)] int sightRadius = 6;
        [Tooltip("Menace avec un seul monstre en vue (0 à 1). Chaque monstre de plus ajoute « Threat Per Extra Monster ».")]
        [SerializeField, Range(0f, 1f)] float threatOneMonster = 0.5f;
        [Tooltip("Menace ajoutée par monstre en vue au-delà du premier.")]
        [SerializeField, Range(0f, 1f)] float threatPerExtraMonster = 0.25f;

        [Header("Combat")]
        [Tooltip("Tours sans aucun coup échangé avant que la musique de combat s'arrête.")]
        [SerializeField, Range(1, 10)] int calmTurns = 2;

        [Header("Coffre")]
        [Tooltip("Distance (en cases) à laquelle un coffre fermé en vue fait varier la musique.")]
        [SerializeField, Range(1, 8)] int chestRadius = 3;

        [Header("Boss")]
        [Tooltip("Force la musique du boss (pour l'essayer en Play mode, en attendant le boss).")]
        [SerializeField] bool forceBoss;

        [Header("Liens")]
        [Tooltip("Le musicien. Vide : cherché sur le même objet.")]
        [SerializeField] DungeonMusic music;
        [Tooltip("Le joueur. Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;

        /// <summary>Allumé par le boss quand il arrive, éteint quand il meurt.</summary>
        public bool BossMode { get; set; }

        /// <summary>Situation actuelle (0 à 1), envoyée au musicien.</summary>
        public float Threat { get; private set; }
        public bool InCombat => _calm < calmTurns;
        public bool ChestNear { get; private set; }
        public bool Boss => BossMode || forceBoss;
        /// <summary>La variation du coffre : un coffre en vue, sans monstre en vue ni combat.</summary>
        public bool ChestVariation => ChestNear && VisibleMonsters == 0 && !InCombat;
        /// <summary>Monstres en vue au dernier calcul.</summary>
        public int VisibleMonsters { get; private set; }

        int _calm = int.MaxValue / 2;
        bool _blowThisTurn;
        float _nextEvaluate;

        void Awake()
        {
            if (music == null)
                music = GetComponent<DungeonMusic>();
        }

        void Start()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.TurnEnded += OnTurnEnded;
                player.Hurt += OnHurt;
                player.Struck += OnBlow;
                player.Died += OnDied;
            }
            Evaluate();
        }

        void OnDestroy()
        {
            if (player == null)
                return;
            player.TurnEnded -= OnTurnEnded;
            player.Hurt -= OnHurt;
            player.Struck -= OnBlow;
            player.Died -= OnDied;
        }

        // Les monstres bougent pendant l'animation du tour, et la carte peut en faire apparaître :
        // on réévalue souvent (c'est peu coûteux), en temps réel pour suivre aussi carte ouverte.
        void Update()
        {
            if (Time.unscaledTime < _nextEvaluate)
                return;
            _nextEvaluate = Time.unscaledTime + 0.2f;
            Evaluate();
        }

        void OnHurt(int damage) => OnBlow();

        void OnBlow()
        {
            _blowThisTurn = true;
            _calm = 0;
            Evaluate();
        }

        void OnTurnEnded(int turn)
        {
            if (!_blowThisTurn && _calm < int.MaxValue / 2)
                _calm++;
            _blowThisTurn = false;
            Evaluate();
        }

        void OnDied()
        {
            if (music != null)
                music.FadeOut(4f);
        }

        /// <summary>Recalcule la situation et l'envoie au musicien.</summary>
        public void Evaluate()
        {
            VisibleMonsters = 0;
            ChestNear = false;
            if (player != null && player.Maze != null && !player.IsDead)
            {
                foreach (var enemy in GridEntity.AllOf<Enemy>())
                {
                    // Une mimique cachée se fait passer pour un coffre jusque dans la musique.
                    if (enemy is Mimic mimic && mimic.IsHidden)
                    {
                        if (Sees(player.Maze, player.Cell, enemy.Cell, chestRadius))
                            ChestNear = true;
                        continue;
                    }
                    if (enemy.IsActive && Sees(player.Maze, player.Cell, enemy.Cell, sightRadius))
                        VisibleMonsters++;
                }
                foreach (var chest in GridEntity.AllOf<Chest>())
                    if (chest.IsActive && !chest.Opened && Sees(player.Maze, player.Cell, chest.Cell, chestRadius))
                        ChestNear = true;
            }
            Threat = VisibleMonsters == 0 ? 0f : Mathf.Clamp01(threatOneMonster + threatPerExtraMonster * (VisibleMonsters - 1));
            if (music != null)
                music.SetSituation(Threat, InCombat, ChestVariation, Boss);
        }

        /// <summary>
        /// Vrai si `to` est à moins de `radius` cases de `from` (distance à vol d'oiseau) et
        /// qu'aucun mur (case de roche ou mur fin) ne coupe la ligne entre les deux centres.
        /// </summary>
        public static bool Sees(Maze maze, Vector2Int from, Vector2Int to, int radius)
        {
            var delta = to - from;
            if (delta.sqrMagnitude > radius * radius)
                return false;
            // Parcours des cases traversées par le segment (une case à la fois, nord / sud / est / ouest).
            var cell = from;
            var stepX = delta.x > 0 ? 1 : -1;
            var stepY = delta.y > 0 ? 1 : -1;
            float dx = Mathf.Abs(delta.x), dy = Mathf.Abs(delta.y);
            int ix = 0, iy = 0;
            while (ix < dx || iy < dy)
            {
                // Compare où le segment sort de la case : par un bord vertical ou horizontal.
                var nextX = (ix + 0.5f) / Mathf.Max(dx, 1e-6f);
                var nextY = (iy + 0.5f) / Mathf.Max(dy, 1e-6f);
                int dir;
                if (dx > 0 && (dy == 0 || nextX < nextY))
                {
                    dir = stepX > 0 ? 1 : 3;
                    ix++;
                }
                else if (dy > 0 && (dx == 0 || nextY < nextX))
                {
                    dir = stepY > 0 ? 2 : 0;
                    iy++;
                }
                else
                {
                    // Passage exact par un coin : visible si l'un des deux détours est libre.
                    var h = stepX > 0 ? 1 : 3;
                    var v = stepY > 0 ? 2 : 0;
                    var viaH = maze.CanStep(cell, h) && maze.CanStep(cell + Grid.Dirs[h], v);
                    var viaV = maze.CanStep(cell, v) && maze.CanStep(cell + Grid.Dirs[v], h);
                    if (!viaH && !viaV)
                        return false;
                    cell += Grid.Dirs[h] + Grid.Dirs[v];
                    ix++;
                    iy++;
                    continue;
                }
                if (!maze.CanStep(cell, dir))
                    return false;
                cell += Grid.Dirs[dir];
            }
            return true;
        }
    }
}
