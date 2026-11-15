using System.Collections.Generic;

namespace Deckscent
{
    /// <summary>
    /// Traduction des textes affichés. Les textes sont écrits en français dans le code et passent
    /// par <see cref="T"/> (ou <see cref="F"/> s'ils ont des valeurs, `{0}`) au moment de
    /// l'affichage : la clé est le texte français, la table <see cref="En"/> donne l'anglais.
    /// Un texte absent de la table reste en français.
    ///
    /// Pour traduire un nouveau texte : l'entourer de Loc.T(…) et ajouter sa ligne dans En.
    /// Les noms de formes de tuiles (TileCard) et les raisons de refus de pose (DungeonBoard)
    /// restent des clés françaises dans le code : c'est l'écran de carte qui les traduit.
    /// </summary>
    public static class Loc
    {
        /// <summary>Le texte dans la langue en cours.</summary>
        public static string T(string fr)
        {
            if (string.IsNullOrEmpty(fr) || GameSettings.Locale == "fr")
                return fr;
            return En.TryGetValue(fr, out var en) ? en : fr;
        }

        /// <summary>Le texte traduit, avec ses valeurs ({0}, {1}…).</summary>
        public static string F(string fr, params object[] args) => string.Format(T(fr), args);

        /// <summary>Nom affiché d'une langue (dans cette langue).</summary>
        public static string LocaleName(string locale) => locale == "en" ? "English" : "Français";

        /// <summary>Anglais. Clé = texte français exact.</summary>
        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // --- Menu (MenuScreen) ---
            { "Construisez le donjon. Survivez-y.", "Build the dungeon. Survive it." },
            { "Descendre", "Descend" },
            { "Options", "Options" },
            { "Commandes", "Controls" },
            { "Quitter", "Quit" },
            { "nouvelle partie", "new game" },
            { "son · langue", "sound · language" },
            { "les touches", "key bindings" },
            { "fermer le jeu", "close the game" },
            { "← →  choisir   ·   Entrée  valider", "← →  choose   ·   Enter  confirm" },
            { "Avancer / reculer", "Move forward / back" },
            { "Pas de côté", "Sidestep" },
            { "Tourner", "Turn" },
            { "Attendre un tour", "Wait one turn" },
            { "Ouvrir un coffre", "Open a chest" },
            { "Lanterne", "Lantern" },
            { "Carte (poser des tuiles)", "Map (place tiles)" },
            { "Carte : tourner la tuile", "Map: rotate tile" },
            { "Carte : défausser", "Map: discard" },
            { "Échap : retour", "Esc: back" },
            { "Général", "Master" },
            { "Musique", "Music" },
            { "Effets sonores", "Sound effects" },
            { "Langue", "Language" },
            { "Muet", "Muted" },
            { "↑ ↓  choisir   ·   ← →  régler   ·   Échap  retour", "↑ ↓  choose   ·   ← →  adjust   ·   Esc  back" },

            // --- Écran de carte (MapScreen) ---
            { "Carte du donjon", "Dungeon map" },
            { "Sous-sols", "Cellars" },
            { "La main", "Hand" },
            { "{0} cartes", "{0} cards" },
            { "aucune place", "no room" },
            { "Légende", "Legend" },
            { "Mur, roche", "Wall, rock" },
            { "Sol", "Floor" },
            { "Mur de salle", "Room wall" },
            { "Sortie libre", "Open exit" },
            { "Torche", "Torch" },
            { "Départ", "Start" },
            { "Vous", "You" },
            { "Danger", "Danger" },
            { "Clic : poser   ·   Clic droit / R / molette : tourner   ·   Suppr : défausser   ·   1-6 : choisir   ·   , / Échap : fermer",
              "Click: place   ·   Right click / R / wheel: rotate   ·   Del: discard   ·   1-6: choose   ·   , / Esc: close" },
            { "Pose possible", "Can be placed here" },
            { "Plus aucun passage libre : le donjon est fermé", "No open passage left: the dungeon is sealed" },
            { "Carte défaussée", "Card discarded" },
            { "Tuile posée : {0}", "Tile placed: {0}" },
            // Refus de pose (DungeonBoard.CheckPlacement).
            { "Hors du plateau", "Off the board" },
            { "Emplacement déjà occupé", "Slot already taken" },
            { "Doit toucher le labyrinthe", "Must touch the maze" },
            { "Doit se raccorder par un passage", "Must connect through a passage" },
            // Formes de tuiles (TileCard).
            { "Impasse", "Dead end" },
            { "Couloir", "Corridor" },
            { "Virage", "Bend" },
            { "Fourche", "Fork" },
            { "Carrefour", "Crossroads" },
            { "Salle", "Room" },
            { "Galerie", "Gallery" },
            { "Longue galerie", "Long gallery" },
            { "Salle en L", "L-shaped room" },
            { "Salle en T", "T-shaped room" },
            { "Salle en Z", "Z-shaped room" },
            { "Salle en U", "U-shaped room" },
            { "Salle en croix", "Cross-shaped room" },
            { "Grande salle", "Great hall" },
            { "Salle à piliers", "Pillared hall" },
            { "Crypte", "Crypt" },

            // --- HUD et messages du jeu ---
            { "PV", "HP" },
            { "Tour {0}", "Turn {0}" },
            { "ATQ {0}", "ATK {0}" },
            { "DÉF {0}", "DEF {0}" },
            { "Vous êtes mort", "You are dead" },
            { "Entrée : recommencer   ·   Échap : menu", "Enter: restart   ·   Esc: menu" },
            { "Piège franchi", "Trap crossed" },
            { "Coffre : une carte de plus dans la main", "Chest: one more card in hand" },
            { "Coffre : +{0} PV", "Chest: +{0} HP" },
        };
    }
}
