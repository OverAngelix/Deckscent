using System.Collections.Generic;

namespace Deckscent
{
    /// <summary>
    /// Traduction des textes affichés, par **codes clés** : le code n'écrit jamais un texte affiché,
    /// mais sa clé (« menu.descendre », « hud.tour »…), traduite au moment de l'affichage par
    /// <see cref="T"/> (ou <see cref="F"/> s'il a des valeurs, `{0}`). Chaque langue a son
    /// dictionnaire : <see cref="Fr"/> (la référence) et <see cref="En"/>.
    ///
    /// Clé = « domaine.nom », en minuscules, sans accents, mots séparés par « _ » ; le domaine dit
    /// où le texte s'affiche (menu, options, carte, legende, refus, forme, hud, message…).
    ///
    /// Pour un nouveau texte : choisir une clé, ajouter sa ligne dans Fr ET dans En (un test
    /// vérifie que les deux ont les mêmes clés), et afficher Loc.T("la.cle").
    /// Une clé absente de la langue en cours retombe sur le français ; absente partout, la clé
    /// elle-même s'affiche (on la repère tout de suite à l'écran).
    /// </summary>
    public static class Loc
    {
        /// <summary>Le texte de la clé dans la langue en cours.</summary>
        public static string T(string key) => In(GameSettings.Locale, key);

        /// <summary>Le texte de la clé, avec ses valeurs ({0}, {1}…).</summary>
        public static string F(string key, params object[] args) => string.Format(T(key), args);

        /// <summary>Le texte de la clé dans une langue donnée (« fr », « en »).</summary>
        public static string In(string locale, string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;
            if (Table(locale).TryGetValue(key, out var text) || Fr.TryGetValue(key, out text))
                return text;
            return key;
        }

        /// <summary>Le dictionnaire d'une langue (le français si elle est inconnue).</summary>
        public static Dictionary<string, string> Table(string locale) => locale == "en" ? En : Fr;

        /// <summary>Nom affiché d'une langue (dans cette langue).</summary>
        public static string LocaleName(string locale) => locale == "en" ? "English" : "Français";

        /// <summary>Français : la langue de référence.</summary>
        public static readonly Dictionary<string, string> Fr = new Dictionary<string, string>
        {
            // --- Menu (MenuScreen) ---
            { "menu.accroche",       "Construisez le donjon. Survivez-y." },
            { "menu.descendre",      "Descendre" },
            { "menu.options",        "Options" },
            { "menu.commandes",      "Commandes" },
            { "menu.quitter",        "Quitter" },
            { "menu.descendre.note", "nouvelle partie" },
            { "menu.options.note",   "son · langue" },
            { "menu.commandes.note", "les touches" },
            { "menu.quitter.note",   "fermer le jeu" },
            { "menu.aide",           "← →  choisir   ·   Entrée  valider" },
            { "menu.retour",         "Échap : retour" },

            // --- Liste des commandes (MenuScreen) ---
            { "commandes.avancer",         "Avancer / reculer" },
            { "commandes.pas_de_cote",     "Pas de côté" },
            { "commandes.tourner",         "Tourner" },
            { "commandes.attendre",        "Attendre un tour" },
            { "commandes.coffre",          "Ouvrir un coffre" },
            { "commandes.lanterne",        "Lanterne" },
            { "commandes.carte",           "Carte (poser des tuiles)" },
            { "commandes.carte_tourner",   "Carte : tourner la tuile" },
            { "commandes.carte_defausser", "Carte : défausser" },

            // --- Options (MenuScreen) ---
            { "options.general", "Général" },
            { "options.musique", "Musique" },
            { "options.effets",  "Effets sonores" },
            { "options.langue",  "Langue" },
            { "options.muet",    "Muet" },
            { "options.aide",    "↑ ↓  choisir   ·   ← →  régler   ·   Échap  retour" },

            // --- Écran de carte (MapScreen) ---
            { "carte.titre",         "Carte du donjon" },
            { "carte.main",          "La main" },
            { "carte.nb_cartes",     "{0} cartes" },
            { "carte.aucune_place",  "aucune place" },
            { "carte.aide",          "Clic : poser   ·   Clic droit / R / molette : tourner   ·   Suppr : défausser   ·   1-6 : choisir   ·   , / Échap : fermer" },
            { "carte.pose_possible", "Pose possible" },
            { "carte.donjon_ferme",  "Plus aucun passage libre : le donjon est fermé" },
            { "carte.defaussee",     "Carte défaussée" },
            { "carte.tuile_posee",   "Tuile posée : {0}" },

            // --- Étages (MapScreen, champ Biome) ---
            { "biome.sous_sols", "Sous-sols" },

            // --- Légende de la carte (MapScreen) ---
            { "legende.titre",     "Légende" },
            { "legende.roche",     "Mur, roche" },
            { "legende.sol",       "Sol" },
            { "legende.mur_salle", "Mur de salle" },
            { "legende.sortie",    "Sortie libre" },
            { "legende.torche",    "Torche" },
            { "legende.depart",    "Départ" },
            { "legende.vous",      "Vous" },
            { "legende.danger",    "Danger" },

            // --- Refus de pose (DungeonBoard.CheckPlacement) ---
            { "refus.hors_plateau", "Hors du plateau" },
            { "refus.occupe",       "Emplacement déjà occupé" },
            { "refus.isole",        "Doit toucher le labyrinthe" },
            { "refus.sans_passage", "Doit se raccorder par un passage" },

            // --- Formes de tuiles (TileCard : « forme. » + identifiant de la forme) ---
            { "forme.impasse",        "Impasse" },
            { "forme.couloir",        "Couloir" },
            { "forme.virage",         "Virage" },
            { "forme.t",              "Fourche" },
            { "forme.carrefour",      "Carrefour" },
            { "forme.salle",          "Salle" },
            { "forme.galerie",        "Galerie" },
            { "forme.longue_galerie", "Longue galerie" },
            { "forme.salle_l",        "Salle en L" },
            { "forme.salle_t",        "Salle en T" },
            { "forme.salle_z",        "Salle en Z" },
            { "forme.salle_u",        "Salle en U" },
            { "forme.salle_croix",    "Salle en croix" },
            { "forme.grande_salle",   "Grande salle" },
            { "forme.salle_piliers",  "Salle à piliers" },
            { "forme.crypte",         "Crypte" },

            // --- HUD (Hud) ---
            { "hud.pv",        "PV" },
            { "hud.tour",      "Tour {0}" },
            { "hud.atq",       "ATQ {0}" },
            { "hud.def",       "DÉF {0}" },
            { "hud.mort",      "Vous êtes mort" },
            { "hud.mort.aide", "Entrée : recommencer   ·   Échap : menu" },

            // --- Messages du jeu ---
            { "message.piege_franchi", "Piège franchi" },
            { "message.coffre_carte",  "Coffre : une carte de plus dans la main" },
            { "message.coffre_pv",     "Coffre : +{0} PV" },
        };

        /// <summary>Anglais : mêmes clés que Fr.</summary>
        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // --- Menu (MenuScreen) ---
            { "menu.accroche",       "Build the dungeon. Survive it." },
            { "menu.descendre",      "Descend" },
            { "menu.options",        "Options" },
            { "menu.commandes",      "Controls" },
            { "menu.quitter",        "Quit" },
            { "menu.descendre.note", "new game" },
            { "menu.options.note",   "sound · language" },
            { "menu.commandes.note", "key bindings" },
            { "menu.quitter.note",   "close the game" },
            { "menu.aide",           "← →  choose   ·   Enter  confirm" },
            { "menu.retour",         "Esc: back" },

            // --- Liste des commandes (MenuScreen) ---
            { "commandes.avancer",         "Move forward / back" },
            { "commandes.pas_de_cote",     "Sidestep" },
            { "commandes.tourner",         "Turn" },
            { "commandes.attendre",        "Wait one turn" },
            { "commandes.coffre",          "Open a chest" },
            { "commandes.lanterne",        "Lantern" },
            { "commandes.carte",           "Map (place tiles)" },
            { "commandes.carte_tourner",   "Map: rotate tile" },
            { "commandes.carte_defausser", "Map: discard" },

            // --- Options (MenuScreen) ---
            { "options.general", "Master" },
            { "options.musique", "Music" },
            { "options.effets",  "Sound effects" },
            { "options.langue",  "Language" },
            { "options.muet",    "Muted" },
            { "options.aide",    "↑ ↓  choose   ·   ← →  adjust   ·   Esc  back" },

            // --- Écran de carte (MapScreen) ---
            { "carte.titre",         "Dungeon map" },
            { "carte.main",          "Hand" },
            { "carte.nb_cartes",     "{0} cards" },
            { "carte.aucune_place",  "no room" },
            { "carte.aide",          "Click: place   ·   Right click / R / wheel: rotate   ·   Del: discard   ·   1-6: choose   ·   , / Esc: close" },
            { "carte.pose_possible", "Can be placed here" },
            { "carte.donjon_ferme",  "No open passage left: the dungeon is sealed" },
            { "carte.defaussee",     "Card discarded" },
            { "carte.tuile_posee",   "Tile placed: {0}" },

            // --- Étages (MapScreen, champ Biome) ---
            { "biome.sous_sols", "Cellars" },

            // --- Légende de la carte (MapScreen) ---
            { "legende.titre",     "Legend" },
            { "legende.roche",     "Wall, rock" },
            { "legende.sol",       "Floor" },
            { "legende.mur_salle", "Room wall" },
            { "legende.sortie",    "Open exit" },
            { "legende.torche",    "Torch" },
            { "legende.depart",    "Start" },
            { "legende.vous",      "You" },
            { "legende.danger",    "Danger" },

            // --- Refus de pose (DungeonBoard.CheckPlacement) ---
            { "refus.hors_plateau", "Off the board" },
            { "refus.occupe",       "Slot already taken" },
            { "refus.isole",        "Must touch the maze" },
            { "refus.sans_passage", "Must connect through a passage" },

            // --- Formes de tuiles (TileCard : « forme. » + identifiant de la forme) ---
            { "forme.impasse",        "Dead end" },
            { "forme.couloir",        "Corridor" },
            { "forme.virage",         "Bend" },
            { "forme.t",              "Fork" },
            { "forme.carrefour",      "Crossroads" },
            { "forme.salle",          "Room" },
            { "forme.galerie",        "Gallery" },
            { "forme.longue_galerie", "Long gallery" },
            { "forme.salle_l",        "L-shaped room" },
            { "forme.salle_t",        "T-shaped room" },
            { "forme.salle_z",        "Z-shaped room" },
            { "forme.salle_u",        "U-shaped room" },
            { "forme.salle_croix",    "Cross-shaped room" },
            { "forme.grande_salle",   "Great hall" },
            { "forme.salle_piliers",  "Pillared hall" },
            { "forme.crypte",         "Crypt" },

            // --- HUD (Hud) ---
            { "hud.pv",        "HP" },
            { "hud.tour",      "Turn {0}" },
            { "hud.atq",       "ATK {0}" },
            { "hud.def",       "DEF {0}" },
            { "hud.mort",      "You are dead" },
            { "hud.mort.aide", "Enter: restart   ·   Esc: menu" },

            // --- Messages du jeu ---
            { "message.piege_franchi", "Trap crossed" },
            { "message.coffre_carte",  "Chest: one more card in hand" },
            { "message.coffre_pv",     "Chest: +{0} HP" },
        };
    }
}
