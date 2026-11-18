using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Animation **procédurale** du loup-garou (choix de l'utilisateur : aucun pack n'a
    /// d'animations). On part de la pose de référence du modèle Synty (T-pose) et on tourne
    /// quelques os, **dans le repère du personnage** (x à sa droite, y en haut, z devant lui) : les
    /// angles se lisent sans connaître les axes propres des os. On passe d'une pose à l'autre en
    /// douceur, une respiration s'ajoute par-dessus.
    ///
    /// Repères pour lire la table : +x penche vers l'avant (buste, cou) ou replie le genou ;
    /// −x lève la cuisse ou relève la tête ; bras gauche : +z le baisse, +y le porte en avant ;
    /// bras droit : −z le baisse, −y le porte en avant ; mâchoire : +x l'ouvre.
    /// Planche des poses : Deckscent > Planche du loup-garou (docs/images/loup_garou.png).
    ///
    /// Sans le pack (pas de squelette), le secours en formes simples se penche selon la pose.
    /// Des clips Mixamo pourraient remplacer cette classe plus tard : <see cref="Werewolf"/> ne
    /// connaît que <see cref="SetPose"/>.
    /// </summary>
    public class WerewolfRig : MonoBehaviour
    {
        /// <summary>Les poses du loup-garou.</summary>
        public enum Pose
        {
            /// <summary>Endormi, accroupi, roulé sur lui-même.</summary>
            Sommeil,
            /// <summary>En garde, voûté, griffes en avant.</summary>
            Garde,
            /// <summary>Arme un coup de griffe : bras droit levé, buste tourné.</summary>
            Armer,
            /// <summary>Le coup de griffe, bras droit abattu vers l'avant.</summary>
            Griffer,
            /// <summary>Se ramasse pour charger : accroupi, bras en arrière.</summary>
            Ramasse,
            /// <summary>En pleine charge, penché en avant, en pleine foulée.</summary>
            Charge,
            /// <summary>Hurle, tête levée, gueule ouverte, bras écartés.</summary>
            Hurle,
            /// <summary>Touché : rejeté en arrière.</summary>
            Touche,
            /// <summary>Sonné (charge dans un mur) : tête basse, bras ballants.</summary>
            Sonne,
            /// <summary>Mort : relâché (la chute est animée par <see cref="Werewolf"/>).</summary>
            Mort,
        }

        [Tooltip("Racine du squelette du modèle Synty (le FBX). Vide : secours en formes simples.")]
        [SerializeField] Transform skeleton;
        [Tooltip("Corps du secours en formes simples, penché selon la pose quand le modèle manque.")]
        [SerializeField] Transform fallbackBody;
        [Tooltip("Durée d'un passage de pose par défaut (secondes).")]
        [SerializeField, Min(0.01f)] float blendTime = 0.2f;
        [Tooltip("Amplitude de la respiration (degrés de buste).")]
        [SerializeField, Range(0f, 10f)] float breathing = 3f;
        [Tooltip("Respirations par seconde.")]
        [SerializeField, Min(0f)] float breathRate = 0.6f;

        /// <summary>Os animés, du parent vers l'enfant (chaque os hérite de la rotation de son parent).</summary>
        static readonly string[] Bones =
        {
            "Spine_01", "Spine_02", "Spine_03", "Neck", "Head", "Jaw",
            "Shoulder_L", "Elbow_L", "Hand_L", "Shoulder_R", "Elbow_R", "Hand_R",
            "UpperLeg_L", "LowerLeg_L", "Ankle_L", "UpperLeg_R", "LowerLeg_R", "Ankle_R",
        };

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>Jambes fléchies (cuisse levée vers l'avant, genou replié, cheville) : même angle des deux côtés.</summary>
        static (string, Vector3)[] Legs(float thigh, float knee, float ankle) => new[]
        {
            ("UpperLeg_L", V(-thigh, 0f, 0f)), ("LowerLeg_L", V(knee, 0f, 0f)), ("Ankle_L", V(-ankle, 0f, 0f)),
            ("UpperLeg_R", V(-thigh, 0f, 0f)), ("LowerLeg_R", V(knee, 0f, 0f)), ("Ankle_R", V(-ankle, 0f, 0f)),
        };

        /// <summary>Bras symétriques : baissés de `down`°, portés en avant de `forward`°, coude plié de `elbow`° vers l'avant.</summary>
        static (string, Vector3)[] Arms(float down, float forward, float elbow) => new[]
        {
            ("Shoulder_L", V(0f, forward, down)), ("Elbow_L", V(-elbow, 0f, 0f)),
            ("Shoulder_R", V(0f, -forward, -down)), ("Elbow_R", V(-elbow, 0f, 0f)),
        };

        static (string, Vector3)[] Join(params (string, Vector3)[][] parts)
        {
            var all = new List<(string, Vector3)>();
            foreach (var p in parts)
                all.AddRange(p);
            return all.ToArray();
        }

        /// <summary>Rotations des os (degrés, repère du personnage) pour chaque pose. Un os absent reste comme dans la T-pose.</summary>
        static readonly Dictionary<Pose, (string bone, Vector3 euler)[]> Table = new Dictionary<Pose, (string, Vector3)[]>
        {
            [Pose.Sommeil] = Join(
                new[] { ("Spine_01", V(30, 0, 0)), ("Spine_02", V(25, 0, 0)), ("Spine_03", V(20, 0, 0)), ("Neck", V(15, 0, 0)), ("Head", V(15, 0, 0)) },
                Arms(80, 45, 90), Legs(95, 140, 45)),
            [Pose.Garde] = Join(
                new[] { ("Spine_01", V(15, 0, 0)), ("Spine_02", V(15, 0, 0)), ("Spine_03", V(10, 0, 0)), ("Neck", V(-20, 0, 0)), ("Head", V(-15, 0, 0)), ("Jaw", V(8, 0, 0)) },
                Arms(75, 35, 45), Legs(30, 50, 20)),
            [Pose.Armer] = Join(
                new[] { ("Spine_01", V(5, 10, 0)), ("Spine_02", V(5, 15, 0)), ("Spine_03", V(0, 10, 0)), ("Neck", V(-15, -20, 0)), ("Head", V(-10, -10, 0)), ("Jaw", V(20, 0, 0)) },
                new[] { ("Shoulder_L", V(0, 40, 60)), ("Elbow_L", V(-50, 0, 0)), ("Shoulder_R", V(0, 25, 70)), ("Elbow_R", V(-70, 0, 0)), ("Hand_R", V(-30, 0, 0)) },
                Legs(30, 45, 15)),
            [Pose.Griffer] = Join(
                new[] { ("Spine_01", V(15, -10, 0)), ("Spine_02", V(20, -20, 0)), ("Spine_03", V(10, -10, 0)), ("Neck", V(-25, 15, 0)), ("Head", V(-15, 10, 0)), ("Jaw", V(30, 0, 0)) },
                new[] { ("Shoulder_L", V(0, 0, 70)), ("Elbow_L", V(-40, 0, 0)), ("Shoulder_R", V(0, -80, -30)), ("Elbow_R", V(-10, 0, 0)), ("Hand_R", V(20, 0, 0)) },
                Legs(35, 50, 15)),
            [Pose.Ramasse] = Join(
                new[] { ("Spine_01", V(25, 0, 0)), ("Spine_02", V(20, 0, 0)), ("Spine_03", V(15, 0, 0)), ("Neck", V(-35, 0, 0)), ("Head", V(-20, 0, 0)), ("Jaw", V(15, 0, 0)) },
                Arms(75, -35, 20), Legs(65, 105, 40)),
            [Pose.Charge] = Join(
                new[] { ("Spine_01", V(30, 0, 0)), ("Spine_02", V(20, 0, 0)), ("Spine_03", V(10, 0, 0)), ("Neck", V(-35, 0, 0)), ("Head", V(-25, 0, 0)), ("Jaw", V(25, 0, 0)) },
                Arms(55, 60, 10),
                new[] { ("UpperLeg_L", V(-55, 0, 0)), ("LowerLeg_L", V(60, 0, 0)), ("UpperLeg_R", V(25, 0, 0)), ("LowerLeg_R", V(35, 0, 0)) }),
            [Pose.Hurle] = Join(
                new[] { ("Spine_01", V(-5, 0, 0)), ("Spine_02", V(-10, 0, 0)), ("Spine_03", V(-5, 0, 0)), ("Neck", V(-35, 0, 0)), ("Head", V(-30, 0, 0)), ("Jaw", V(30, 0, 0)) },
                Arms(25, -15, 35), Legs(15, 25, 10)),
            [Pose.Touche] = Join(
                new[] { ("Spine_01", V(-10, 0, 0)), ("Spine_02", V(-12, 0, 0)), ("Neck", V(10, 0, 0)), ("Head", V(15, 0, 0)), ("Jaw", V(25, 0, 0)) },
                Arms(72, 15, 55), Legs(20, 30, 10)),
            [Pose.Sonne] = Join(
                new[] { ("Spine_01", V(30, 0, 0)), ("Spine_02", V(25, 0, 0)), ("Spine_03", V(10, 0, 0)), ("Neck", V(20, 0, 0)), ("Head", V(10, 0, 20)), ("Jaw", V(15, 0, 0)) },
                Arms(85, 10, 5), Legs(30, 40, 10)),
            [Pose.Mort] = Join(
                new[] { ("Neck", V(20, 0, 0)), ("Head", V(10, 0, 25)), ("Jaw", V(30, 0, 0)) },
                Arms(75, 15, 15), Legs(10, 10, 0)),
        };

        /// <summary>De combien le bassin descend (mètres à l'échelle du modèle) : jambes pliées, pieds au sol.</summary>
        static readonly Dictionary<Pose, float> BodyDrop = new Dictionary<Pose, float>
        {
            [Pose.Sommeil] = 0.55f, [Pose.Garde] = 0.1f, [Pose.Armer] = 0.08f, [Pose.Griffer] = 0.11f, [Pose.Ramasse] = 0.32f,
            [Pose.Charge] = 0.15f, [Pose.Hurle] = 0.03f, [Pose.Touche] = 0.05f, [Pose.Sonne] = 0.09f, [Pose.Mort] = 0.01f,
        };

        /// <summary>Penchée du secours (degrés, vers l'avant) pour chaque pose.</summary>
        static readonly Dictionary<Pose, float> FallbackLean = new Dictionary<Pose, float>
        {
            [Pose.Sommeil] = 50f, [Pose.Garde] = 15f, [Pose.Armer] = -10f, [Pose.Griffer] = 30f, [Pose.Ramasse] = 40f,
            [Pose.Charge] = 45f, [Pose.Hurle] = -25f, [Pose.Touche] = -20f, [Pose.Sonne] = 35f, [Pose.Mort] = 0f,
        };

        static Vector3[][] _poses;

        Transform[] _bones;
        Quaternion[] _bind;
        Transform _hips;
        Vector3 _hipsBind;
        Vector3[] _from, _current;
        float _blend = 1f, _blendDuration = 0.2f, _time, _dropFrom, _drop, _leanFrom, _lean;

        public Pose Current { get; private set; } = Pose.Garde;
        /// <summary>Vrai si le modèle Synty est animé (squelette trouvé).</summary>
        public bool Rigged => _bones != null;

        void Awake() => Init();

        void Init()
        {
            BuildTable();
            if (_bones != null || skeleton == null)
                return;
            var found = new Dictionary<string, Transform>();
            foreach (var t in skeleton.GetComponentsInChildren<Transform>(true))
                found.TryAdd(t.name, t);
            if (!found.TryGetValue("Hips", out _hips))
                return;
            _bones = new Transform[Bones.Length];
            _bind = new Quaternion[Bones.Length];
            for (var i = 0; i < Bones.Length; i++)
            {
                found.TryGetValue(Bones[i], out _bones[i]);
                if (_bones[i] != null)
                    _bind[i] = _bones[i].localRotation;
            }
            _hipsBind = _hips.localPosition;
            _current = (Vector3[])_poses[(int)Current].Clone();
            _from = (Vector3[])_current.Clone();
        }

        /// <summary>Les tables de rotations, une fois pour toutes (un tableau par pose, dans l'ordre de <see cref="Bones"/>).</summary>
        static void BuildTable()
        {
            if (_poses != null)
                return;
            var index = new Dictionary<string, int>();
            for (var i = 0; i < Bones.Length; i++)
                index[Bones[i]] = i;
            var poses = (Pose[])System.Enum.GetValues(typeof(Pose));
            _poses = new Vector3[poses.Length][];
            foreach (var pose in poses)
            {
                var angles = new Vector3[Bones.Length];
                foreach (var (bone, euler) in Table[pose])
                    angles[index[bone]] = euler;
                _poses[(int)pose] = angles;
            }
        }

        /// <summary>Passe à une pose en `duration` secondes (négatif : la durée par défaut).</summary>
        public void SetPose(Pose pose, float duration = -1f)
        {
            Current = pose;
            _blend = 0f;
            _blendDuration = duration < 0f ? blendTime : Mathf.Max(0.001f, duration);
            if (_current != null)
                System.Array.Copy(_current, _from, _current.Length);
            _dropFrom = _drop;
            _leanFrom = _lean;
        }

        /// <summary>Prend la pose tout de suite, sans transition (planche des poses, dans l'éditeur aussi).</summary>
        public void ApplyNow(Pose pose)
        {
            Init();
            SetPose(pose, 0.001f);
            _blend = 1f;
            Apply();
        }

        void LateUpdate()
        {
            _time += Time.deltaTime;
            if (_blend < 1f)
                _blend = Mathf.Min(1f, _blend + Time.deltaTime / _blendDuration);
            Apply();
        }

        void Apply()
        {
            var t = _blend * _blend * (3f - 2f * _blend);
            _lean = Mathf.Lerp(_leanFrom, FallbackLean[Current], t);
            _drop = Mathf.Lerp(_dropFrom, BodyDrop[Current], t);
            if (_bones == null)
            {
                if (fallbackBody != null)
                    fallbackBody.localRotation = Quaternion.Euler(_lean, 0f, 0f);
                return;
            }
            var to = _poses[(int)Current];
            for (var i = 0; i < _current.Length; i++)
                _current[i] = Vector3.Lerp(_from[i], to[i], t);
            var breath = Current == Pose.Mort ? 0f : Mathf.Sin(_time * breathRate * Mathf.PI * 2f) * breathing;

            // Repère du personnage : celui de la racine du modèle (z devant lui).
            var frame = skeleton.rotation;
            var inverse = Quaternion.Inverse(frame);
            for (var i = 0; i < _bones.Length; i++)
            {
                var bone = _bones[i];
                if (bone == null)
                    continue;
                bone.localRotation = _bind[i];
                var euler = _current[i];
                if (i == 2)
                    euler.x -= breath; // le haut du buste se gonfle
                if (euler == Vector3.zero)
                    continue;
                bone.rotation = frame * Quaternion.Euler(euler) * inverse * bone.rotation;
            }
            // Le bassin descend (jambes pliées), dans le repère du personnage.
            _hips.localPosition = _hipsBind;
            _hips.position += skeleton.TransformVector(Vector3.down * _drop);
        }
    }
}
