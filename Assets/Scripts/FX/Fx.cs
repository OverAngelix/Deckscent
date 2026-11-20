using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Fait jouer un effet de particules (<see cref="FxLibrary"/>) : une instance du prefab, posée
    /// où il faut, détruite une fois finie. Sans le pack (champ vide), ne fait rien. Les effets
    /// en boucle (étoiles du boss sonné, feu) renvoient leur instance : à détruire par l'appelant.
    ///
    /// Les particules passent par le post-process gravure : une couleur saturée (or, feu, giclée
    /// d'un slime) sort du papier comme un accent, le reste devient encre et papier.
    /// </summary>
    public static class Fx
    {
        static FxLibrary _library;

        /// <summary>La bibliothèque (Resources/Effets), ou null si elle n'existe pas.</summary>
        public static FxLibrary Library => _library != null ? _library : _library = Resources.Load<FxLibrary>("Effets");

        /// <summary>
        /// Joue l'effet à `position`, tourné selon `rotation`. `tint` (facultatif) recolore les
        /// particules (alpha gardé). `parent` : il suit cet objet. Renvoie l'instance (null sans effet).
        /// </summary>
        public static GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation, Color? tint = null,
            Transform parent = null, float scale = 1f)
        {
            if (prefab == null || !Usable(prefab))
                return null;
            var go = Object.Instantiate(prefab, position, rotation, parent);
            if (!Mathf.Approximately(scale, 1f))
                go.transform.localScale *= scale;
            var longest = 0f;
            var loops = false;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                if (tint.HasValue)
                {
                    var c = tint.Value;
                    var start = main.startColor;
                    c.a = start.mode == ParticleSystemGradientMode.Color ? start.color.a : 1f;
                    main.startColor = c;
                }
                loops |= main.loop;
                longest = Mathf.Max(longest, main.duration + main.startLifetime.constantMax + main.startDelay.constantMax);
            }
            if (!loops)
                Object.Destroy(go, Mathf.Max(0.5f, longest + 0.2f));
            return go;
        }

        public static GameObject Play(GameObject prefab, Vector3 position) => Play(prefab, position, Quaternion.identity);

        static readonly System.Collections.Generic.Dictionary<GameObject, bool> _usable = new System.Collections.Generic.Dictionary<GameObject, bool>();

        /// <summary>
        /// Un effet est jouable si ses matériaux ont leur texture : sans le pack Synty (non
        /// versionné), les textures manquent et les particules dessineraient des carrés blancs.
        /// Toutes nos conversions en ont une (FxBuilder), un emplacement vide ne compte pas.
        /// </summary>
        static bool Usable(GameObject prefab)
        {
            if (_usable.TryGetValue(prefab, out var ok))
                return ok;
            ok = true;
            foreach (var r in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.shader.name.Contains("Particles") && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null)
                        ok = false;
            _usable[prefab] = ok;
            return ok;
        }

        /// <summary>Un effet de la bibliothèque, choisi par une fonction (null sans bibliothèque).</summary>
        public static GameObject Get(System.Func<FxLibrary, GameObject> pick) => Library != null ? pick(Library) : null;
    }
}
