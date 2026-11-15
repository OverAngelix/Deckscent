using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Pièce de décor Synty avec sa version de secours : si un maillage du décor manque (pack Synty
    /// non importé, il n'est pas versionné), le décor est caché et le secours (formes simples)
    /// affiché à la place. Comme le labyrinthe, qui passe aux blocs gris.
    /// </summary>
    [ExecuteAlways]
    public class DecorFallback : MonoBehaviour
    {
        [Tooltip("La pièce Synty (enfant).")]
        [SerializeField] GameObject decor;
        [Tooltip("La version en formes simples, affichée si le décor n'a pas ses maillages.")]
        [SerializeField] GameObject fallback;
        [Tooltip("Coché : toujours le secours, même si le décor est là (pour comparer).")]
        [SerializeField] bool forceFallback;

        public bool UsingFallback { get; private set; }

        void OnEnable() => Apply();
#if UNITY_EDITOR
        // SetActive est interdit pendant OnValidate : on attend la fin de l'image de l'éditeur.
        void OnValidate() => UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                Apply();
        };
#endif

        void Apply()
        {
            UsingFallback = forceFallback || !HasMeshes(decor);
            if (decor != null && decor.activeSelf == UsingFallback)
                decor.SetActive(!UsingFallback);
            if (fallback != null && fallback.activeSelf != UsingFallback)
                fallback.SetActive(UsingFallback);
        }

        static bool HasMeshes(GameObject go)
        {
            if (go == null)
                return false;
            var filters = go.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0)
                return false;
            foreach (var filter in filters)
                if (filter.sharedMesh == null)
                    return false;
            return true;
        }
    }
}
