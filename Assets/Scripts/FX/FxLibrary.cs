using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Les effets de particules du jeu (pack Synty POLYGON Particle FX, matériaux convertis aux
    /// particules URP par <c>FxBuilder</c>), rangés par usage. Un seul asset :
    /// <c>Assets/Resources/Effets.asset</c>, lu par <see cref="Fx"/>. Un champ vide = pas d'effet
    /// (le jeu marche sans le pack).
    /// </summary>
    [CreateAssetMenu(menuName = "Deckscent/Bibliothèque d'effets", fileName = "Effets")]
    public class FxLibrary : ScriptableObject
    {
        [Header("Combat")]
        [Tooltip("Mort d'un monstre autre que le slime (mimique, louveteau) : nuage de poussière.")]
        public GameObject puff;
        [Tooltip("Boss sonné (charge dans un mur) : étoiles qui tournent au-dessus de sa tête (en boucle).")]
        public GameObject stunned;
        [Tooltip("Charge du boss dans un mur, mort du boss, sol qui s'effondre : gros nuage de poussière.")]
        public GameObject bigDust;

        [Header("Objets")]
        [Tooltip("Vase brisé : poussière.")]
        public GameObject vaseBreak;
        [Tooltip("Caisse brisée : éclats de bois.")]
        public GameObject crateBreak;
    }
}
