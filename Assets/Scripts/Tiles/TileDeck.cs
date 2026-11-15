using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deckscent
{
    /// <summary>Un poids de tirage, réglable dans l'inspecteur (un dictionnaire ne l'est pas).</summary>
    [Serializable]
    public struct Weight
    {
        public string key;
        [Min(0f)] public float weight;

        public Weight(string key, float weight)
        {
            this.key = key;
            this.weight = weight;
        }
    }

    /// <summary>Réglages de la pioche (dans l'inspecteur du Donjon).</summary>
    [Serializable]
    public class DeckSettings
    {
        [Tooltip("Nombre de cartes en main.")]
        [Min(1)] public int handSize = 5;
        [Tooltip("Taille maximale de la main quand un coffre donne une carte en plus.")]
        [Min(1)] public int maxHandSize = 6;
        [Tooltip("Chance qu'une carte porte une torche (jamais sur un carrefour).")]
        [Range(0f, 1f)] public float torchChance = 0.3f;

        // Rythme voulu (choix de l'utilisateur, façon donjon mystère) : on se balade longtemps dans
        // des couloirs étroits et calmes, et de loin en loin une salle, dense et dangereuse.
        [Header("Salles : rares et dangereuses")]
        [Tooltip("Chance qu'une carte tirée soit une salle, tant que le plafond n'est pas atteint.")]
        [Range(0f, 1f)] public float roomChance = 0.2f;
        [Tooltip("Nombre maximal de salles par étage (posées + en main). Au-delà, la pioche ne donne plus que des couloirs.")]
        [Min(0)] public int maxRoomsPerFloor = 6;

        [Tooltip("Poids de tirage des couloirs (voir TileCard).")]
        public List<Weight> corridorWeights = new List<Weight>
        {
            new Weight("impasse", 1f),
            new Weight("couloir", 3f),
            new Weight("virage", 3f),
            new Weight("t", 2f),
            new Weight("carrefour", 1f),
        };

        [Tooltip("Poids de tirage des salles, quand une salle sort. Les grandes sont plus rares.")]
        public List<Weight> roomWeights = new List<Weight>
        {
            new Weight("salle", 1.5f),
            new Weight("galerie", 1f),
            new Weight("longue_galerie", 0.5f),
            new Weight("salle_l", 1f),
            new Weight("salle_t", 0.8f),
            new Weight("salle_z", 0.7f),
            new Weight("salle_u", 0.6f),
            new Weight("salle_croix", 0.5f),
            new Weight("grande_salle", 1f),
            new Weight("salle_piliers", 0.5f),
            new Weight("crypte", 0.3f),
        };

        [Tooltip("Poids du contenu d'un couloir (TileCard.Contents) : surtout vide, de temps en temps un monstre ou un piège. Le boss reste à 0 : sa carte arrivera avec la recette d'étage.")]
        public List<Weight> contentWeights = new List<Weight>
        {
            new Weight("vide", 7f),
            new Weight("monstre", 2f),
            new Weight("piege", 1.5f),
            new Weight("tresor", 0.5f),
            new Weight("boss", 0f),
        };

        [Tooltip("Poids du thème d'une salle (TileCard.Contents, « salle_… ») : presque jamais vide.")]
        public List<Weight> roomThemeWeights = new List<Weight>
        {
            new Weight("salle_vide", 0.3f),
            new Weight("salle_tresor", 1f),
            new Weight("salle_pieges", 1.5f),
            new Weight("salle_piegee", 1.5f),
            new Weight("salle_gardee", 2f),
            new Weight("salle_monstres", 2f),
        };
    }

    /// <summary>
    /// La main du joueur : des cartes-tuiles tirées au hasard. Quand une carte est posée ou
    /// défaussée, une nouvelle est piochée à sa place.
    /// </summary>
    public class TileDeck
    {
        public event Action HandChanged;

        public List<TileCard> Hand { get; } = new List<TileCard>();
        public DeckSettings Settings { get; }
        /// <summary>Salles déjà posées à cet étage (tenu à jour par le Donjon). Avec celles en main, plafonné par maxRoomsPerFloor.</summary>
        public int RoomsPlaced { get; set; }
        /// <summary>Salles posées + en main.</summary>
        public int RoomsOnFloor => RoomsPlaced + Hand.Count(c => c.IsRoom);

        readonly System.Random _rng;

        /// <param name="roomsPlaced">Salles déjà sur le plateau (plafond de l'étage).</param>
        public TileDeck(DeckSettings settings, int seed = 0, int roomsPlaced = 0)
        {
            Settings = settings ?? new DeckSettings();
            RoomsPlaced = roomsPlaced;
            _rng = seed != 0 ? new System.Random(seed) : new System.Random();
            while (Hand.Count < Settings.handSize)
                Hand.Add(DrawCard());
        }

        /// <summary>
        /// Tire une nouvelle carte (forme et contenu selon les poids, orientation au hasard). Une salle
        /// tire aussi ses portes et un thème.
        /// </summary>
        public TileCard DrawCard()
        {
            // Une salle seulement de temps en temps, et jamais au-delà du plafond de l'étage.
            var room = RoomsOnFloor < Settings.maxRoomsPerFloor && _rng.NextDouble() < Settings.roomChance;
            var shape = room ? Weighted(Settings.roomWeights, "salle") : Weighted(Settings.corridorWeights, "couloir");
            if (!TileCard.HasShape(shape))
            {
                Debug.LogWarning("Pioche : forme inconnue « " + shape + " », couloir à la place.");
                shape = "couloir";
            }
            var card = new TileCard(shape, _rng.Next(4), _rng.NextDouble() < Settings.torchChance);
            if (card.IsRoom)
            {
                card.RandomDoors(_rng);
                card.Content = Weighted(Settings.roomThemeWeights, "salle_vide");
            }
            else
                card.Content = Weighted(Settings.contentWeights, "vide");
            return card;
        }

        /// <summary>Ajoute une carte à la main (butin d'un coffre). Faux si la main est pleine.</summary>
        public bool AddCard()
        {
            if (Hand.Count >= Settings.maxHandSize)
                return false;
            Hand.Add(DrawCard());
            HandChanged?.Invoke();
            return true;
        }

        /// <summary>Remplace la carte d'indice `index` (jouée ou défaussée) par une nouvelle.</summary>
        public void Replace(int index)
        {
            if (index < 0 || index >= Hand.Count)
                return;
            // La carte sort de la main avant le tirage : une salle posée ne compte pas deux fois
            // dans le plafond de l'étage (posées + en main).
            Hand.RemoveAt(index);
            Hand.Insert(index, DrawCard());
            HandChanged?.Invoke();
        }

        string Weighted(List<Weight> weights, string fallback)
        {
            var total = 0f;
            foreach (var w in weights)
                total += Mathf.Max(w.weight, 0f);
            if (total <= 0f)
                return fallback;
            var roll = (float)_rng.NextDouble() * total;
            foreach (var w in weights)
            {
                if (w.weight <= 0f)
                    continue;
                roll -= w.weight;
                if (roll <= 0f)
                    return w.key;
            }
            return fallback;
        }
    }
}
