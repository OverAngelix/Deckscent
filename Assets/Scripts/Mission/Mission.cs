using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>Sortes d'objectifs d'une mission d'étage.</summary>
    public enum GoalKind
    {
        /// <summary>Atteindre un nombre de tours.</summary>
        Turns,
        /// <summary>Nettoyer des salles (tout leur contenu tué, ouvert, franchi, et y être entré).</summary>
        Rooms,
        /// <summary>Ouvrir des coffres.</summary>
        Chests,
        /// <summary>Tuer des monstres.</summary>
        Monsters,
        /// <summary>Franchir des dalles à pics.</summary>
        Traps,
    }

    /// <summary>Un objectif : sa sorte, le compte visé et où on en est.</summary>
    public class MissionGoal
    {
        public GoalKind Kind { get; }
        public int Target { get; }
        public int Progress { get; internal set; }
        public bool Done => Progress >= Target;

        /// <summary>Clé du libellé (Loc) : « mission.salles »…</summary>
        public string LabelKey => LabelKeys[(int)Kind];

        static readonly string[] LabelKeys = { "mission.tours", "mission.salles", "mission.coffres", "mission.monstres", "mission.pieges" };

        public MissionGoal(GoalKind kind, int target)
        {
            Kind = kind;
            Target = Mathf.Max(1, target);
        }
    }

    /// <summary>Réglages du tirage des missions (inspecteur du Donjon).</summary>
    [Serializable]
    public class MissionSettings
    {
        [Tooltip("Nombre d'objectifs, au moins (sortes toutes différentes).")]
        [Range(1, 5)] public int minGoals = 2;
        [Tooltip("Nombre d'objectifs, au plus. Plus il y en a, plus chacun est facile (« longue et facile » contre « courte et dure »).")]
        [Range(1, 5)] public int maxGoals = 3;
        [Tooltip("Tours à atteindre (du plus facile au plus dur).")]
        public Vector2Int turns = new Vector2Int(30, 60);
        [Tooltip("Salles à nettoyer. L'étage en compte 6 au plus (pioche).")]
        public Vector2Int rooms = new Vector2Int(2, 3);
        [Tooltip("Coffres à ouvrir.")]
        public Vector2Int chests = new Vector2Int(2, 3);
        [Tooltip("Monstres à tuer.")]
        public Vector2Int monsters = new Vector2Int(4, 8);
        [Tooltip("Dalles à pics à franchir.")]
        public Vector2Int traps = new Vector2Int(2, 3);

        public Vector2Int RangeOf(GoalKind kind) => kind switch
        {
            GoalKind.Turns => turns,
            GoalKind.Rooms => rooms,
            GoalKind.Chests => chests,
            GoalKind.Monsters => monsters,
            _ => traps,
        };
    }

    /// <summary>
    /// La mission d'un étage (la « recette » du GDD) : quelques objectifs tirés au hasard, à remplir
    /// tous. Logique pure : <see cref="MissionTracker"/> lui rapporte ce qui se passe en jeu.
    /// Tirage : plus d'objectifs = plus faciles chacun (dans la moitié basse de leur fourchette),
    /// moins d'objectifs = plus durs (moitié haute).
    /// </summary>
    public class Mission
    {
        readonly List<MissionGoal> _goals;

        public IReadOnlyList<MissionGoal> Goals => _goals;
        public bool IsComplete { get; private set; }
        /// <summary>Augmente à chaque changement (les écrans se redessinent quand elle bouge).</summary>
        public int Version { get; private set; }

        /// <summary>Un objectif vient d'être rempli.</summary>
        public event Action<MissionGoal> GoalDone;
        /// <summary>Tous les objectifs sont remplis (une seule fois).</summary>
        public event Action Completed;

        public Mission(IEnumerable<MissionGoal> goals) => _goals = new List<MissionGoal>(goals);

        /// <summary>Tire une mission : un nombre d'objectifs, des sortes différentes, des comptes selon le nombre.</summary>
        public static Mission Draw(System.Random rng, MissionSettings settings)
        {
            var kinds = new List<GoalKind>((GoalKind[])Enum.GetValues(typeof(GoalKind)));
            var min = Mathf.Clamp(settings.minGoals, 1, kinds.Count);
            var max = Mathf.Clamp(settings.maxGoals, min, kinds.Count);
            var count = rng.Next(min, max + 1);
            // Peu d'objectifs : moitié haute de la fourchette ; beaucoup : moitié basse.
            var hard = max == min ? 0.5f : 1f - (float)(count - min) / (max - min);
            var goals = new List<MissionGoal>();
            for (var i = 0; i < count; i++)
            {
                var k = rng.Next(kinds.Count);
                var kind = kinds[k];
                kinds.RemoveAt(k);
                var range = settings.RangeOf(kind);
                var t = hard * 0.5f + (float)rng.NextDouble() * 0.5f;
                var target = Mathf.RoundToInt(Mathf.Lerp(range.x, range.y, t));
                if (kind == GoalKind.Turns)
                    target = Mathf.RoundToInt(target / 10f) * 10; // des tours ronds
                goals.Add(new MissionGoal(kind, target));
            }
            goals.Sort((a, b) => a.Kind.CompareTo(b.Kind));
            return new Mission(goals);
        }

        /// <summary>Ajoute `amount` aux objectifs de cette sorte (un monstre tué, un coffre ouvert…).</summary>
        public void Report(GoalKind kind, int amount = 1) => Update(kind, g => g.Progress + amount);

        /// <summary>Le tour en cours (objectif « tours »).</summary>
        public void SetTurn(int turn) => Update(GoalKind.Turns, g => Mathf.Max(g.Progress, turn));

        void Update(GoalKind kind, Func<MissionGoal, int> next)
        {
            var changed = false;
            foreach (var goal in _goals)
            {
                if (goal.Kind != kind || goal.Done)
                    continue;
                var value = Mathf.Min(next(goal), goal.Target);
                if (value == goal.Progress)
                    continue;
                goal.Progress = value;
                changed = true;
                if (goal.Done)
                    GoalDone?.Invoke(goal);
            }
            if (!changed)
                return;
            Version++;
            if (!IsComplete && _goals.TrueForAll(g => g.Done))
            {
                IsComplete = true;
                Completed?.Invoke();
            }
        }
    }
}
