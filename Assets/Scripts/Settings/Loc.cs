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

            // --- Menu pause (PauseScreen) ---
            { "pause.titre",       "Pause" },
            { "pause.continuer",   "Continuer" },
            { "pause.sauvegarder", "Sauvegarder" },
            { "pause.a_venir",     "à venir" },
            { "pause.menu",        "Retour au menu" },
            { "pause.bureau",      "Quitter le jeu" },

            // --- Aides « touches + action » (KeyPrompts, selon l'appareil) ---
            { "aide.choisir",     "choisir" },
            { "aide.valider",     "valider" },
            { "aide.regler",      "régler" },
            { "aide.retour",      "retour" },
            { "aide.deplacer",    "déplacer" },
            { "aide.poser",       "poser" },
            { "aide.tourner",     "tourner" },
            { "aide.defausser",   "défausser" },
            { "aide.fermer",      "fermer" },
            { "aide.recommencer", "recommencer" },
            { "aide.menu",        "menu" },
            { "aide.reprendre",   "reprendre" },
            { "aide.rester",      "rester" },
            { "aide.continuer",   "continuer" },
            { "aide.camp",        "retour au camp" },
            { "confirmation.donjon_titre", "Descendre dans le donjon ?" },
            { "confirmation.donjon_texte", "L'expédition commence. On ne revient qu'en remontant… ou mort." },
            { "confirmation.descendre",    "Descendre" },
            { "confirmation.rester",       "Rester au camp" },
            { "remontee.titre",          "Remonter ou descendre ?" },
            { "remontee.sous_titre",     "Le boss du sous-sol {0} est vaincu : un passage remonte à la surface" },
            { "remontee.remonter",       "Remonter" },
            { "remontee.descendre",      "Descendre" },
            { "remontee.or_ramene",      "{0} or pour le camp" },
            { "remontee.fin_expedition", "Fin de l'expédition" },
            { "remontee.or_en_jeu",      "L'or reste en jeu" },
            { "hud.surface",             "De retour à la surface" },
            { "bilan.or_ramene",         "Or ramené : {0}   ·   Or du camp : {1}" },

            // --- Noms de touches (KeyPrompts ; les lettres viennent du clavier) ---
            { "touche.entree",         "Entrée" },
            { "touche.espace",         "Espace" },
            { "touche.echap",          "Échap" },
            { "touche.suppr",          "Suppr" },
            { "touche.retour_arriere", "Retour" },
            { "touche.tab",            "Tab" },
            { "touche.clic",           "Clic" },
            { "touche.clic_droit",     "Clic droit" },
            { "touche.molette",        "Molette" },
            { "touche.chiffres_main",  "1-6" },

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
            { "commandes.pause",           "Pause" },

            // --- Options (MenuScreen) ---
            { "options.general", "Général" },
            { "options.musique", "Musique" },
            { "options.effets",  "Effets sonores" },
            { "options.langue",  "Langue" },
            { "options.muet",    "Muet" },

            // --- Écran de carte (MapScreen) ---
            { "carte.titre",         "Carte du donjon" },
            { "carte.main",          "La main" },
            { "carte.nb_cartes",     "{0} cartes" },
            { "carte.aucune_place",  "aucune place" },
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
            { "legende.pas_vu",    "Pas encore vu" },
            { "legende.sortie",    "Sortie libre" },
            { "legende.torche",    "Torche" },
            { "legende.depart",    "Départ" },
            { "legende.vous",      "Vous" },
            { "legende.danger",    "Danger" },
            { "legende.antre",   "Antre du boss" },

            // --- Refus de pose (DungeonBoard.CheckPlacement) ---
            { "refus.hors_plateau", "Hors du plateau" },
            { "refus.combat", "En combat : un monstre est tout près" },
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
            { "forme.antre",          "Antre du boss" },

            // --- HUD (Hud) ---
            { "hud.pv",        "PV" },
            { "hud.tour",      "Tour {0}" },
            { "hud.atq",       "ATQ {0}" },
            { "hud.def",       "DÉF {0}" },
            { "hud.mort",      "Vous êtes mort" },
            { "hud.niveau",    "Niv. {0}" },
            { "hud.xp",        "{0} / {1} XP" },

            // --- Montée de niveau (LevelUpScreen) ---
            { "niveau.titre",       "Niveau {0} !" },
            { "niveau.choisir",     "Choisissez un bienfait" },
            { "niveau.attaque",     "Attaque" },
            { "niveau.defense",     "Défense" },
            { "niveau.vie",         "Vie" },
            { "niveau.avant_apres", "{0} → {1}" },
            { "niveau.atteint",     "Niveau {0}" },

            // --- Mission de l'étage (Mission, Hud, MapScreen) ---
            { "mission.titre",    "Mission" },
            { "mission.tours",    "Tours survécus" },
            { "mission.salles",   "Salles nettoyées" },
            { "mission.coffres",  "Coffres ouverts" },
            { "mission.monstres", "Monstres tués" },
            { "mission.pieges",   "Pièges franchis" },
            { "mission.remplie",  "Mission remplie" },

            // --- Messages du jeu ---
            { "message.objectif",        "Objectif rempli : {0}" },
            { "message.mission_remplie", "Mission remplie !" },
            { "message.antre",           "Un hurlement monte des profondeurs : l'antre du boss est sur la carte" },
            { "message.boss_eveil",      "Le loup-garou s'éveille !" },
            { "message.boss_charge",     "Le loup-garou se ramasse pour charger !" },
            { "message.boss_sonne",      "Le loup-garou est sonné" },
            { "message.boss_louveteaux", "Le loup-garou appelle sa meute !" },
            { "message.boss_vaincu",     "Le loup-garou est vaincu : l'escalier s'ouvre" },
            { "message.etage",           "Sous-sol {0}" },
            { "message.vase_potion",     "Une potion était cachée dedans : +{0} PV" },
            { "message.vase_carte",      "Une carte était cachée dedans" },
            { "message.vase_or",         "Quelques pièces cachées dedans : +{0} or" },
            { "bilan.or",                "Or ramassé : {0}   ·   Or pour le camp : {1}" },
            { "message.fontaine",        "Vous buvez à la fontaine : +{0} PV" },
            { "message.mimique",         "Ce coffre a des dents !" },
            { "boss.loup_garou",         "Loup-garou" },
            { "hud.etage",               "Sous-sol {0}" },
            { "bilan.monstres",          "Monstres tués : {0}" },
            { "bilan.etage",             "Sous-sol {0} : {1} monstres tués · {2} coffres ouverts · {3} tours" },
            { "bilan.coffres",           "Coffres ouverts : {0}" },
            { "bilan.boss",              "Boss vaincus : {0}" },
            { "bilan.degats",            "Dégâts infligés : {0}" },
            { "message.coffre_carte",  "Coffre : {0} or et une carte de plus dans la main" },
            { "message.coffre_pv",     "Coffre : {1} or et +{0} PV" },
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

            // --- Menu pause (PauseScreen) ---
            { "pause.titre",       "Paused" },
            { "pause.continuer",   "Resume" },
            { "pause.sauvegarder", "Save" },
            { "pause.a_venir",     "coming soon" },
            { "pause.menu",        "Back to menu" },
            { "pause.bureau",      "Quit game" },

            // --- Aides « touches + action » (KeyPrompts, selon l'appareil) ---
            { "aide.choisir",     "choose" },
            { "aide.valider",     "confirm" },
            { "aide.regler",      "adjust" },
            { "aide.retour",      "back" },
            { "aide.deplacer",    "move" },
            { "aide.poser",       "place" },
            { "aide.tourner",     "rotate" },
            { "aide.defausser",   "discard" },
            { "aide.fermer",      "close" },
            { "aide.recommencer", "restart" },
            { "aide.menu",        "menu" },
            { "aide.reprendre",   "resume" },
            { "aide.rester",      "stay" },
            { "aide.continuer",   "continue" },
            { "aide.camp",        "back to camp" },
            { "confirmation.donjon_titre", "Descend into the dungeon?" },
            { "confirmation.donjon_texte", "The expedition begins. You only come back by climbing up… or dead." },
            { "confirmation.descendre",    "Descend" },
            { "confirmation.rester",       "Stay at the camp" },
            { "remontee.titre",          "Ascend or descend?" },
            { "remontee.sous_titre",     "The boss of basement {0} is slain: a passage leads back to the surface" },
            { "remontee.remonter",       "Ascend" },
            { "remontee.descendre",      "Descend" },
            { "remontee.or_ramene",      "{0} gold for the camp" },
            { "remontee.fin_expedition", "End of the expedition" },
            { "remontee.or_en_jeu",      "Your gold stays at risk" },
            { "hud.surface",             "Back at the surface" },
            { "bilan.or_ramene",         "Gold brought back: {0}   ·   Camp gold: {1}" },

            // --- Noms de touches (KeyPrompts ; les lettres viennent du clavier) ---
            { "touche.entree",         "Enter" },
            { "touche.espace",         "Space" },
            { "touche.echap",          "Esc" },
            { "touche.suppr",          "Del" },
            { "touche.retour_arriere", "Backspace" },
            { "touche.tab",            "Tab" },
            { "touche.clic",           "Click" },
            { "touche.clic_droit",     "Right click" },
            { "touche.molette",        "Wheel" },
            { "touche.chiffres_main",  "1-6" },

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
            { "commandes.pause",           "Pause" },

            // --- Options (MenuScreen) ---
            { "options.general", "Master" },
            { "options.musique", "Music" },
            { "options.effets",  "Sound effects" },
            { "options.langue",  "Language" },
            { "options.muet",    "Muted" },

            // --- Écran de carte (MapScreen) ---
            { "carte.titre",         "Dungeon map" },
            { "carte.main",          "Hand" },
            { "carte.nb_cartes",     "{0} cards" },
            { "carte.aucune_place",  "no room" },
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
            { "legende.pas_vu",    "Not seen yet" },
            { "legende.sortie",    "Open exit" },
            { "legende.torche",    "Torch" },
            { "legende.depart",    "Start" },
            { "legende.vous",      "You" },
            { "legende.danger",    "Danger" },
            { "legende.antre",   "Boss lair" },

            // --- Refus de pose (DungeonBoard.CheckPlacement) ---
            { "refus.hors_plateau", "Off the board" },
            { "refus.combat", "In combat: a monster is close" },
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
            { "forme.antre",          "Boss lair" },

            // --- HUD (Hud) ---
            { "hud.pv",        "HP" },
            { "hud.tour",      "Turn {0}" },
            { "hud.atq",       "ATK {0}" },
            { "hud.def",       "DEF {0}" },
            { "hud.mort",      "You are dead" },
            { "hud.niveau",    "Lv. {0}" },
            { "hud.xp",        "{0} / {1} XP" },

            // --- Montée de niveau (LevelUpScreen) ---
            { "niveau.titre",       "Level {0}!" },
            { "niveau.choisir",     "Choose a boon" },
            { "niveau.attaque",     "Attack" },
            { "niveau.defense",     "Defense" },
            { "niveau.vie",         "Health" },
            { "niveau.avant_apres", "{0} → {1}" },
            { "niveau.atteint",     "Level {0}" },

            // --- Mission de l'étage (Mission, Hud, MapScreen) ---
            { "mission.titre",    "Mission" },
            { "mission.tours",    "Turns survived" },
            { "mission.salles",   "Rooms cleared" },
            { "mission.coffres",  "Chests opened" },
            { "mission.monstres", "Monsters slain" },
            { "mission.pieges",   "Traps crossed" },
            { "mission.remplie",  "Mission complete" },

            // --- Messages du jeu ---
            { "message.objectif",        "Goal reached: {0}" },
            { "message.mission_remplie", "Mission complete!" },
            { "message.antre",           "A howl rises from the depths: the boss lair is on the map" },
            { "message.boss_eveil",      "The werewolf awakens!" },
            { "message.boss_charge",     "The werewolf crouches to charge!" },
            { "message.boss_sonne",      "The werewolf is stunned" },
            { "message.boss_louveteaux", "The werewolf calls its pack!" },
            { "message.boss_vaincu",     "The werewolf is slain: the stairs open" },
            { "message.etage",           "Basement {0}" },
            { "message.vase_potion",     "A potion was hidden inside: +{0} HP" },
            { "message.vase_carte",      "A card was hidden inside" },
            { "message.vase_or",         "A few coins were hidden inside: +{0} gold" },
            { "bilan.or",                "Gold collected: {0}   ·   Gold for the camp: {1}" },
            { "message.fontaine",        "You drink from the fountain: +{0} HP" },
            { "message.mimique",         "This chest has teeth!" },
            { "boss.loup_garou",         "Werewolf" },
            { "hud.etage",               "Basement {0}" },
            { "bilan.monstres",          "Monsters slain: {0}" },
            { "bilan.etage",             "Basement {0}: {1} monsters slain · {2} chests opened · {3} turns" },
            { "bilan.coffres",           "Chests opened: {0}" },
            { "bilan.boss",              "Bosses slain: {0}" },
            { "bilan.degats",            "Damage dealt: {0}" },
            { "message.coffre_carte",  "Chest: {0} gold and one more card in hand" },
            { "message.coffre_pv",     "Chest: {1} gold and +{0} HP" },
        };
    }
}
