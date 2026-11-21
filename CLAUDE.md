# Deckscent (Unity)

Dungeon crawler en vue à la première personne, déplacement case par case **au tour par tour**, où le joueur **construit son labyrinthe** en posant des cartes-tuiles depuis la carte. Rendu stylisé façon BD / Moebius : noir et blanc, contours à l'encre, hachures progressives dans les zones sombres, couleur réservée aux accents.

**Unity 6.3 LTS** (6000.3.11f1), **URP**, C#. Portage du prototype Godot de `C:\Users\OverA\Documents\dungeon-crawler-prototype` : c'est **la référence** (code, réglages, docs `docs/gdd.md`, `docs/todo.md`, `docs/features/`, `docs/monsters/`) tant que ce projet ne l'a pas rattrapé. On ne modifie pas le projet Godot : on le lit pour traduire.

## Manière de travailler (préférences de l'utilisateur)

- **Tout en français** : messages, commentaires, noms affichés dans l'inspecteur (`[Tooltip]`, `[Header]`), messages de commit, docs.
- **Répondre avant d'agir** sur un retour de jeu (« ça fait bizarre quand… ») : proposer la correction et attendre son oui avant de coder.
- **Commiter seulement après validation** : montrer le résultat (captures pour le rendu), attendre le feu vert de l'utilisateur, puis un commit par fonctionnalité et `git push` (remote `origin` = GitHub `OverAngelix/Deckscent`).
- Commits au format **gitmoji** en français (`✨ Ajoute ...`, `🐛 Corrige ...`, `🎨 ...`, `🔧 ...`, `📝 ...`, `♻️ ...`). **Jamais de ligne `Co-Authored-By`** ni autre mention d'attribution.
- Quand une fonctionnalité documentée change, **mettre la doc à jour** dans le même travail.

## Plan du portage

Dans l'ordre (chaque étape donne quelque chose de jouable ou de visible) :

0. ✅ Ce fichier.
1. ✅ **Labyrinthe + joueur** : `Maze` (plan texte → murs, sols, plafond, torches Synty), déplacement en grille, tourner, bump.
2. ✅ **Rendu gravure** : shader toon + post-process gravure (Renderer Feature URP), validé par captures (en attente du feu vert de l'utilisateur).
3. ✅ **Tuiles, plateau, pioche** en C# pur, avec tests EditMode (plus de salles, vraies pièces à murs fins).
4. ✅ **Écran de carte** dessiné en gravure, pose à la souris, au clavier et à la manette.
5. ✅ **Tour par tour et contenu** : PV / ATQ / DÉF, slimes, dalle à pics, coffre, Spawner, HUD.
6. ✅ **Menu, options, musique synthétisée, FR / EN**.
7. ✅ **Musique du donjon à couches** (exploration, menace, combat, coffre, boss), voir `docs/musique.md`.

On **ne reprend plus la todo du projet Godot** (demande de l'utilisateur, 2026-11-16) : la suite se décide avec lui et s'écrit dans **`docs/todo.md`** (ordre des tâches, cocher ✅ chaque tâche validée). Les idées lointaines vont dans `docs/gdd.md` §11.

Mettre à jour cette liste (✅) à chaque étape finie.

## Structure visée

```
Assets/Scripts/          Code du jeu (namespace Deckscent), un dossier par domaine :
  Maze/                  Génération du labyrinthe depuis un plan texte
  Player/                Déplacement en grille, tours, PV, attaque
  Tiles/                 TileCard, TileDeck, DungeonBoard (logique pure, sans MonoBehaviour si possible)
  Enemies/ Traps/ Props/ Slime, dalle à pics, coffre
  UI/                    Carte, HUD, menu
Assets/Scripts/Editor/   Outils d'éditeur (captures, construction de prefabs)
Assets/Prefabs/          Un prefab par objet (slime, coffre, dalle, torche, pans de mur)
Assets/Scenes/           Main (jeu), Menu, Vitrine (objets fixes pour les régler)
Assets/Shaders/          Toon, Moebius
Assets/Tests/            Tests EditMode / PlayMode (assembly Deckscent.Tests)
docs/                    Docs portées depuis le projet Godot, à tenir à jour
```

### Ce qui existe

```
Assets/Scripts/Maze/Grid.cs            Conventions de grille : Dirs (N, E, S, O), ToWorldDir, Yaw
Assets/Scripts/Maze/Maze.cs            Labyrinthe [ExecuteAlways] : plan texte → instances de prefabs
Assets/Scripts/Maze/TorchFlicker.cs    Vacillement des torches (en jeu)
Assets/Scripts/Player/PlayerController.cs  Déplacement en grille, tours, lanterne (actions d'InputSystem_Actions)
Assets/InputSystem_Actions.inputactions   Actions du projet (InputSystem.actions) : toutes les touches du jeu
Assets/Scripts/Editor/PrototypeBuilder.cs  Menu Deckscent > Construire… : prefabs, scène Main, scène de test
Assets/Scripts/Editor/TestCaptures.cs  Menu Deckscent > Captures de la scène de test (→ Captures/)
Assets/Scripts/Editor/PrefabDocs.cs    Menu Deckscent > Documenter les prefabs (→ docs/prefabs/)
Assets/Scripts/Editor/DocFiles.cs      Écriture des images et textes générés seulement s'ils ont changé
Assets/Scripts/Editor/RenderingSetup.cs  Menu Deckscent > Installer le rendu gravure (voir « Rendu gravure »)
Assets/Shaders/                        Toon.shader + ToonInput.hlsl (objets), Gravure.shader (post-process)
Assets/Prefabs/Player.prefab           Joueur : PlayerController, Caméra (1,6 m, FOV 75), Lanterne
Assets/Prefabs/Maze/                   Mur_01/03/04, Sol, Plafond (Synty), Mur_Gris, Sol_Gris, Plafond_Gris (secours), Torche
Assets/Materials/                      M_Gris_* (secours), M_Flamme
Assets/Scenes/Main.unity               Jeu (scène 2 du build)
Assets/Scenes/Camp.unity               Camp à la surface (hub, scène 1 du build) : titre → camp → donjon → camp ; docs/camp.md
Assets/Scenes/Menu.unity               Menu (scène 0 du build) : salle qui tourne, musique, écran titre
Assets/Scenes/Tests/Test_Labyrinthe.unity  Scène de test, plan figé (PrototypeBuilder.TestPlan)
Assets/Scenes/Tests/Test_Donjon.unity  Donjon de démo (DemoDungeon, graine 7) : une salle de chaque sorte
Assets/Scripts/Tiles/TileCard.cs       Carte-tuile : formes (ShapeList), contenus, rotation, portes, cases, murs fins
Assets/Scripts/Tiles/TileDeck.cs       Main de 5 cartes, tirage pondéré (DeckSettings, réglable dans l'inspecteur du Donjon)
Assets/Scripts/Tiles/DungeonBoard.cs   Plateau 9 × 9, 10 départs, règles de pose, plan texte + murs fins
Assets/Scripts/Tiles/Dungeon.cs        Objet « Donjon » de la scène : plateau + pioche → Maze.SetPlan (aussi dans l'éditeur)
Assets/Scripts/Tiles/DemoDungeon.cs    Donjon de démo posé avec les vraies règles (captures, tests)
Assets/Tests/PlayMode/                 Tests PlayMode (assembly Deckscent.Tests.PlayMode), clavier virtuel via InputTestFixture
Assets/Tests/EditMode/                 Tests EditMode (assembly Deckscent.Tests.EditMode) : tuiles, pioche, plateau
docs/tuiles.md                         Doc des tuiles, salles, pioche, contenus et règles de pose
Assets/Scripts/UI/MapScreen.cs         Écran de carte : plateau + main, pose, pause (prefab Prefabs/UI/CarteDuDonjon)
Assets/Scripts/UI/InkCanvas.cs         Toile de dessin 2D (Graphic uGUI) : rectangles, traits, pointillés, polygones, hachures
Assets/Scripts/UI/TextPool.cs          Textes de l'UI gravure (police à empattements du système), réutilisés d'une image à l'autre
Assets/Scripts/UI/Engraving.cs         Primitives gravure partagées : cadres, bandeaux, cartes, crânes, flamme, rose des vents
docs/carte.md                          Doc de l'écran de carte (commandes, ce qu'on voit, technique)
Assets/Scripts/Editor/TilePlate.cs     Menu Deckscent > Planche des tuiles (→ docs/images/tuiles.png, dessin par Tools/planche_tuiles.ps1)
Assets/Scripts/Maze/GridEntity.cs      Base de ce qui occupe une case et joue après le joueur (GridEntity.All, TakeTurn, Place)
Assets/Scripts/Player/Combat.cs        Combat.DamageAfterDefense, interfaces IDamageable / IInteractable
Assets/Scripts/Enemies/Enemy.cs        Base des ennemis (PV, attaque, défense, mort) ; Slime.cs ; GridPath.cs (chemin, murs fins compris)
Assets/Scripts/Traps/SpikeTrap.cs      Dalle à pics (cycle 1 tour sur 3, alerte, nettoyée)
Assets/Scripts/Props/Chest.cs          Coffre (butin carte ou PV) ; DecorFallback.cs (décor Synty ou formes de secours)
Assets/Scripts/Tiles/Spawner.cs        Contenu des tuiles posées → instances des prefabs (objet « Contenu » des scènes)
Assets/Scripts/UI/Hud.cs               HUD gravure : PV, Tour / ATQ / DÉF, messages, voile rouge, écran de mort
Assets/Scripts/Editor/ContentBuilder.cs  Menus Deckscent > Construire les prefabs du contenu / Ajouter le contenu aux scènes
Assets/Prefabs/Enemies/Slime, Traps/DallePics, Props/Coffre, UI/Hud
docs/tour-par-tour.md, docs/monstres/slime.md, docs/pieges.md, docs/coffres.md
Assets/Scripts/Settings/GameSettings.cs  Options (volume, langue), PlayerPrefs, appliquées avant la 1re scène
Assets/Scripts/Settings/Loc.cs         Traduction par codes clés : Loc.T("menu.descendre") / Loc.F(…{0}…), dictionnaires Loc.Fr et Loc.En
Assets/Scripts/Audio/MenuMusic.cs      Musique du menu synthétisée (OnAudioFilterRead)
Assets/Scripts/Player/Experience.cs    XP et niveaux (logique pure) : paliers, bienfaits (ATQ, DÉF, PV max) ; PlayerController.Experience
Assets/Scripts/UI/LevelUpScreen.cs     Écran de montée de niveau (pause, 3 cartes gravées) ; prefab UI/MonteeDeNiveau (LevelUpBuilder.cs) ; docs/niveaux.md
Assets/Scripts/UI/MainMenu.cs          Scène du menu (caméra qui tourne, fondu) ; MenuScreen.cs = écran titre (prefab UI/EcranTitre)
Assets/Scripts/UI/SceneFlow.cs         Menu → jeu, recommencer, retour au menu, quitter
Assets/Scripts/UI/PauseScreen.cs       Menu pause (Échap / Start) : Continuer, Options, Sauvegarder (à venir), menu, bureau ; prefab UI/MenuPause (PauseBuilder.cs)
Assets/Scripts/UI/OptionsPanel.cs      Panneau Options (volumes, langue), partagé par le menu titre et le menu pause
Assets/Scripts/Editor/MenuBuilder.cs   Menu Deckscent > Construire la scène du menu (prefab, scène, ordre du build)
docs/menu.md                           Doc du menu, des options, de la traduction et de la musique
Assets/Scripts/UI/ControlDevice.cs     Dernier appareil utilisé (clavier-souris, PlayStation, Xbox, Switch) → touches affichées
Assets/Scripts/UI/KeyPrompts.cs        Touches d'une action selon l'appareil, pictos de manette gravés (InkCanvas), lignes d'aide
Assets/Resources/Mixage.mixer          Mixer : Master (Général) > Musique, Effets ; volumes exposés VolumeGeneral / VolumeMusique / VolumeEffets
Assets/Scripts/Audio/MusicDirector.cs  Chef d'orchestre de la musique du donjon (menace, combat, coffre, boss) ; DungeonMusic.cs = le musicien
Assets/Scripts/Audio/AdventureSynth.cs Synthé du morceau à couches (testable hors Play mode) ; prefab Audio/MusiqueDonjon (MusicBuilder.cs)
docs/musique.md                        Doc de la musique du donjon
Assets/Scripts/Audio/SoundBank.cs      Effets sonores synthétisés (pas, attaque, coup reçu, slurp, pics) ; SoundEmitter.cs les joue sur la piste Effets
docs/sons.md                           Doc des pistes, des volumes et des effets sonores
Assets/Scripts/Mission/Mission.cs      Mission de l'étage : objectifs tirés au hasard (MissionSettings dans l'inspecteur du Donjon), Dungeon.Mission
Assets/Scripts/Mission/MissionTracker.cs  Suit la mission en jeu (objet « Contenu », à côté du Spawner) : tours, monstres, coffres, pièges, salles nettoyées
docs/mission.md                        Doc de la mission de l'étage
Assets/Scripts/Exploration/Exploration.cs  Mémoire de l'étage (brouillard) : cases vues, emplacements visités (Dungeon.Exploration)
Assets/Scripts/Exploration/ExplorationTracker.cs  Nourrit la mémoire en jeu (ligne de vue, 5 cases, salle entière ; objet « Contenu »)
Assets/Scripts/Enemies/Werewolf.cs     Boss loup-garou (réveil, coup armé, charge annoncée, louveteaux) ; WolfCub.cs ; WerewolfRig.cs = poses procédurales os par os
Assets/Scripts/Props/LairDecor.cs      Décor de l'antre (obstacles) et sa disposition ; Stairs.cs = escalier vers l'étage suivant
Assets/Scripts/Editor/BossBuilder.cs   Menus Deckscent > Construire les prefabs du boss / Ajouter le boss aux scènes
Assets/Scripts/Editor/WerewolfPlate.cs Menu Deckscent > Planche du loup-garou (→ docs/images/loup_garou.png)
Assets/Prefabs/Enemies/LoupGarou, Louveteau ; Props/Escalier ; Props/Antre/ (Brasero, Trone, Cage, Os, Ossements)
docs/antre.md, docs/monstres/loup-garou.md   Antre du boss posée par le jeu, escalier, étages ; le boss
Assets/Scripts/Tiles/Ambience.cs       Décor d'ambiance des tuiles (logique pure) ; Props/Breakable.cs (vase, caisse à briser), Shrine.cs (fontaine qui soigne)
Assets/Scripts/Editor/DecorBuilder.cs  Menus Deckscent > Construire les prefabs du décor / Ajouter le décor aux scènes (Prefabs/Props/Decor/)
Assets/Scripts/Player/RunStats.cs      Bilan de la partie (monstres, coffres, boss, dégâts), affiché à la mort
docs/decor.md                          Décor des salles et couloirs, vases et caisses qui explosent (fontaine retirée pour le moment)
Assets/Scripts/Enemies/Mimic.cs        Mimique (coffre qui mord), prefab Enemies/Mimique (DecorBuilder) ; docs/monstres/mimique.md
Assets/Scripts/FX/                     Effets de particules : Fx.Play, FxLibrary (Resources/Effets.asset)
Assets/Scripts/Editor/FxBuilder.cs     Menu Deckscent > Construire les effets de particules (Prefabs/FX/, Materials/FX/) ; docs/effets.md
Assets/Scripts/Progression/Gold.cs     Or porté pendant l'expédition (coffres, vases, monstres), 10 % gardés à la mort ; docs/or.md
Assets/Scripts/Progression/Progression.cs  Progression durable du hub (or mis à l'abri), JSON dans persistentDataPath
Assets/Scripts/UI/AscentScreen.cs     Choix Remonter / Descendre en fin de sous-biome (prefab UI/Remontee, AscentBuilder.cs) ; docs/remontee.md
Assets/Scripts/Camp/                  CampSky (ciel gravé du dehors), CampObstacle (décor qui bloque), DungeonEntrance (la grotte → confirmation → l'expédition) ; UI/ConfirmScreen.cs = demande oui / non gravée
Assets/Scripts/Editor/CampBuilder.cs  Menu Deckscent > Construire le camp (prefabs Prefabs/Camp/, scène Camp) ; docs/camp.md
```

**Repères de grille** : case (x, y) du plan, x vers l'est, y vers le sud (ligne 0 = nord). Monde : nord = +Z, est = +X, centre de case = `((x + 0,5) × cellSize, 0, −(y + 0,5) × cellSize)`. Orientation 0 = nord, 1 = est (yaw 90°)…

**Labyrinthe** (plan : `#` mur, `.` sol, `P` départ, `T` torche, `~` hors du chemin sans mur) : chaque case de sol reçoit un `Sol`, un `Plafond` (facultatif : pas de plafond dehors) et un `Mur` par côté fermé (variante tirée d'après la position). Les prefabs sont faits pour des cases de 3 m et des murs de 3,5 m : racine d'un mur au milieu du bord, face (+Z) vers la case ; sol et plafond centrés sur la case. Les objets générés vont sous un enfant `Généré` en `HideFlags.DontSave` : **jamais enregistrés dans la scène**, seul le plan l'est. Si une pièce du décor n'a pas de maillage (pack absent), tout le labyrinthe passe au jeu `fallback` (blocs gris). Torche : sur une case `T`, accrochée au premier mur voisin (N, E, S, O), à `torchHeight`. Modèle : torche ornée sur plaque de pierre (`SM_Prop_Torch_Ornate_02`, choix de l'utilisateur), plaque **incrustée dans le mur** (`TorchPlateDepth`), flamme blanche et lumière gardées ; *Deckscent > Construire la torche* ne reconstruit qu'elle.

**Prefabs construits par script** : `PrototypeBuilder` copie le maillage et les matériaux des pièces Synty (pas de prefab imbriqué : un prefab Synty manquant ne se détecterait pas). Relancer le menu **écrase** les prefabs : une fois réglés à la main dans l'éditeur, ne le relancer que pour repartir de zéro (ou reporter les réglages dans le script).

**Scène de test** (`Test_Labyrinthe`, plan figé) : repère commun des captures et des tests. Ne pas changer son plan sans mettre à jour les poses de `TestCaptures` et les tests. Captures (`Captures/` à la racine, **versionnées**) : 5 vues joueur + 1 vue de dessus, rendues dans l'éditeur sans Play mode (torches figées, images comparables). Après un changement visuel, les régénérer et les commiter avec le changement : le diff montre l'avant / après.

**Doc des prefabs** (`docs/prefabs/`, demande de l'utilisateur) : `README.md` = la liste, une page par prefab (hiérarchie, composants, réglages de nos scripts avec leur infobulle, image dans `images/`). **Générée** par *Deckscent > Documenter les prefabs* (`PrefabDocs.cs`) : ne pas l'éditer à la main. Après avoir créé ou modifié un prefab, la régénérer dans le même travail. Le rôle de chaque prefab est dans `PrefabDocs.Descriptions` : y ajouter chaque nouveau prefab.

**Fichiers générés écrits seulement s'ils changent** (`DocFiles`) : une image n'est remplacée que si plus de 0,1 % de ses pixels diffèrent (au-delà du bruit du GPU), un texte que s'il diffère. Régénérer sans rien avoir changé ne crée donc aucun diff.

**Tests** : `unity command test run --mode playmode --filter Deckscent.Tests.PlayMode --filter-type Assembly --async-tests true`, puis `unity command test status`. Les tests chargent la scène de test **au début de chaque test** (pas dans `[UnitySetUp]`, qui passe avant la remise à zéro de l'Input System par `InputTestFixture`). Le clavier réel est ignoré quand la vue Game n'a pas le focus : pour simuler des touches, toujours passer par un test avec `InputTestFixture`, pas par `script eval`.

**Un prefab par objet** (demande de l'utilisateur, raison de la migration) : chaque slime, coffre, dalle, torche doit pouvoir s'ouvrir, se voir et se régler dans l'inspecteur. Le code instancie les prefabs, il ne construit pas les objets à la main. Les réglages sont des champs `[SerializeField]` avec `[Tooltip]` en français.

## Lancer / vérifier

- **MCP Unity** : `.mcp.json` lance `unity mcp --tool-groups` (Unity CLI, package `com.unity.pipeline`). L'éditeur doit être **ouvert** sur ce projet. Groupes utiles : `scene`, `gameobject`, `prefab`, `component`, `material`, `script` (recompilation), `console` (erreurs), `editor.playmode`, `capture`, `test`.
- **Sans éditeur ouvert** : `unity test`, `unity run`, `unity assets import` passent par le batch mode, qui refuse de tourner si l'éditeur a le projet ouvert (verrou `Temp/UnityLockfile`).
- Après avoir écrit du C#, **vérifier la compilation** (`unity recompile` ou la console) avant de déclarer fini.
- **Voir le rendu** : toujours vérifier un changement de shader ou d'éclairage par les **captures de la scène de test** (`unity command editor invoke_menu --path "Deckscent/Captures de la scène de test"`, images dans `Captures/`) : l'absence d'erreur ne dit rien du rendu. Pour une vue en jeu : `unity command capture game_view` en Play mode.
- Menus de l'éditeur : `unity command editor invoke_menu --path "Deckscent/…"`. C# ponctuel : `unity command script eval --file <fichier.cs>` (le code finit par `return …`).
- Scènes et prefabs (`.unity`, `.prefab`) : **ne pas les écrire à la main** (YAML plein de `fileID` / GUID). Passer par le MCP ou par un script d'éditeur.

## Assets Synty (`Assets/Synty/`)

Voir `README.md` (packs, versions, réimport).

- **Non versionnés** (`.gitignore`, licence Synty) : rien de versionné ne doit **casser** sans eux. Le code charge les pièces Synty par référence optionnelle et revient à des blocs gris (primitives) si elles manquent, comme `maze.gd` dans Godot.
- Pièces Synty : grille de 5 m, pivot au coin, sculptées sur une seule face. Le labyrinthe a des cases de `cellSize` = 3 m et des murs de 3,5 m : échelle `cellSize / 5` en largeur, `wallHeight / 5` en hauteur. Les murs sont des pans posés sur les bords des cases de sol (pas un bloc par `#`), tournés vers la case.
- On ne garde que le **maillage et la texture** : matériau toon, texture passée en gris et éclaircie (dans Godot : saturation 0, gain ≈ 2,5, l'atlas est un gris ~0,38). Sans ce gain, toute la scène passe sous les seuils et se couvre de hachures croisées.
- Assets de l'Asset Store **partagés par un tiers** (Feel, Toony Colors Pro) : ne pas les importer ni coder dessus (licence, sécurité). Effets : Cinemachine Impulse, DOTween ou coroutines. Toon : shader maison.

## Règles de jeu à conserver (détails dans le projet Godot)

- **Grille, sans physique** : collisions testées sur la grille (`maze.IsWalkable()`), mouvements en tweens, entrées bloquées pendant l'animation.
- **Tour par tour** : après un pas, une attaque (avancer vers un ennemi droit devant) ou une attente, chaque acteur du tour (ennemis, pièges) joue l'un après l'autre (`TakeTurn(player)`, en coroutine / async). Tourner, se cogner et la carte ne coûtent pas de tour. Dégâts : la défense est retirée, minimum 1 par coup.
- **Touches** : toutes dans `Assets/InputSystem_Actions.inputactions` (fichier d'actions du projet, demande de l'utilisateur : partir des boutons de base). Bindings clavier par **position physique** (`<Keyboard>/w` = Z en AZERTY). Map `Player` (choix de l'utilisateur) :

  | Action | Clavier AZERTY (chemin QWERTY) | Manette | Branchée |
  |---|---|---|---|
  | `Move` | ZQSD (`w a s d`), pas de flèches pour l'instant | stick gauche, croix | oui |
  | `Turn` (axe 1D) | A / E (`q` / `e`) | LB / RB | oui |
  | `Wait` | Entrée, Entrée du pavé | L3 | oui |
  | `Torch` | T | bouton droit (Rond / B) | oui : lanterne, gratuit |
  | `Interact` | F | bouton du bas (Croix / A) | oui : ouvre le coffre droit devant (un tour) ; recommencer après la mort |
  | `Map` | virgule (`m`) | bouton du haut (Triangle / Y) | oui : ouvre / ferme la carte |
  | `Pause` | Échap | Start | oui : menu pause (la carte passe d'abord : Échap la ferme seulement) |

  Map **`Map`** (carte ouverte, voir `docs/carte.md`) : `Close` (Échap / Rond-B), `Rotate` (R / Carré-X), `Discard` (Suppr, Retour arrière / Select), `Place` (Entrée, Espace / Croix-A), `NextCard` (Tab / RB), `PreviousCard` (LB), `Cursor` (flèches / croix, stick gauche). Chiffres 1-6 et souris lus directement.

  Map **`UI`** (menu, voir `docs/menu.md`) : `Navigate` (ZQSD, flèches, stick, croix), `Submit` (Entrée / Croix-A), `Cancel` (Échap / Rond-B, aussi « retour au menu » sur l'écran de mort). Chiffres 1-4 et souris lus directement.

  Les autres actions de base (Attack, Jump, Crouch, Sprint, Look, Previous, Next) ne servent pas et certaines partagent des touches (Entrée = Attack, L3 = Sprint) : en tenir compte avant de les utiliser. Pour changer une touche, modifier le fichier d'actions (pas le code). Le joueur référence ses actions par `InputActionReference` (assignées par `PrototypeBuilder.ConfigurePlayer`, à rappeler sur le prefab si on ajoute une action). Carte (à venir) : R pour tourner une tuile, Suppr pour défausser, à ajouter au fichier.
- **Pas de pose en combat** (GDD §2) : `Combat.InCombat` (monstre qui `Threatens` à 3 pas ou moins, murs contournés) ; la carte s'ouvre mais refuse la pose (`refus.combat`).
- **La carte met le jeu en pause** (le joueur ne la connaît pas, l'ouvrir est gratuit) : `Time.timeScale = 0`, comme le **menu pause** (`PauseScreen`, voir `docs/menu.md`). Le joueur ne lit pas ses touches en pause (et la lanterne ignore Rond / B, qui ferme la carte), puis attend qu'on les lâche au sortir de la pause (l'Entrée de « Continuer » ne fait pas attendre un tour). Tout ce qui doit tourner carte ouverte utilise `Time.unscaledTime`.
- **Écran de carte, retours de l'utilisateur** : tout ce qui n'est pas du sol est hachuré pareil (roche non construite = murs, carte d'un seul tenant ; pas de roche pâle) ; proportions de l'écran Godot (CanvasScaler référence 1152 × 648) ; légende en colonne étroite ; pas de rose des vents ; l'aperçu de pose montre une flèche sur chaque entrée ; **brouillard** : ce qui n'est pas encore vu reste en esquisse, sol papier et murs en **pointillés gris** (pas de sol gris, qui jurait avec les tuiles blanches), voir `docs/carte.md`.
- **Les cartes gardent le style Godot** (demande de l'utilisateur) : carte à jouer gravée 112 × 172 (double filet à losanges, numéro, emprise, plan fléché, bandeau à queues d'aronde, crânes ; **pas de torche** sur les cartes à poser, on la découvre en explorant : mystère). L'UI gravure se dessine avec `InkCanvas` + `TextPool` + `Engraving`, pas avec des sprites.
- **Capturer la carte** : Play mode dans `Test_Donjon`, `script eval` qui appelle `MapScreen.Open()` (et `PreviewSlot`), puis `capture game_view` (le canvas en surimpression n'est pas rendu par `Camera.Render`, donc pas par le menu de captures). Image de la doc : `docs/images/carte.png`.
- **Tuiles** 3×3 cases, ouvertures en bits N=1, E=2, S=4, W=8, rotation horaire = décalage de bits ; salles sur plusieurs emplacements ; règles de pose volontairement permissives ; plateau 9×9 tuiles ; 10 départs. Doc : `docs/tuiles.md`.
- **Salles = vraies pièces** (demande de l'utilisateur, 2026-11-15 : « plus de salles, et variées ») : tout l'emplacement est du sol (3 × 3 cases), le pourtour est fermé par des **murs fins** sur les bords des cases (`TileCard.EdgeWalls`, `DungeonBoard.EdgeWalls`, `Maze.SetPlan(plan, murs)`), troués aux portes. Dans Godot, les salles étaient faites de cases de roche (branches d'une case de large) : abandonné. Le déplacement passe par `Maze.CanStep(case, direction)`, jamais par `IsWalkable` seul. 16 formes, dont 11 salles (salle, galerie, longue galerie, L, T, Z, U, croix, grande salle, salle à piliers, crypte).
- **Rythme façon donjon mystère** (choix de l'utilisateur) : beaucoup de couloirs étroits (la pression), des salles rares mais **denses et dangereuses**. Pioche : `roomChance` 20 % (1 sur 5), **6 salles au plus par étage** (`maxRoomsPerFloor`, posées + en main, `TileDeck.RoomsPlaced` tenu par le Donjon). Couloirs : un élément au plus, surtout vides. Salles : 2 à 7 éléments selon la taille (petite ≤ 2 emplacements, moyenne 3-4, grande ≥ 5 : `ContentDef.Small / Medium / Large`), presque jamais vides. Planche des formes : `docs/images/tuiles.png`, *Deckscent > Planche des tuiles*. Piliers : coins au milieu d'un bloc 2 × 2 d'emplacements (salle à piliers, crypte).
- **Sons** (`docs/sons.md`) : toute `AudioSource` sort sur une piste du mixer (`Musique` ou `Effets`), jamais directement. Un objet qui fait du bruit a un `SoundEmitter` (`PrototypeBuilder.AddSoundEmitter`) ; les sons sont synthétisés par `SoundBank` (provisoire, comme la musique). **Musique du donjon** (`docs/musique.md`) : un seul morceau à couches ; `MusicDirector` décide (monstres en vue, coups échangés, coffre proche, `BossMode`), `DungeonMusic` / `AdventureSynth` jouent ; des fichiers audio ne remplaceraient que le musicien.
- **Touches affichées selon l'appareil** (`docs/menu.md`) : jamais de nom de touche en dur dans un texte ; une aide se construit avec `KeyPrompts` (`Of("Map/Place")`, `Confirm()`, `Back()`, `Navigate(axe)`) et se dessine avec `KeyPrompts.DrawHints` (libellés `aide.…`). Un écran qui affiche des touches appelle `ControlDevice.Listen()` à son activation et se redessine sur `ControlDevice.Changed`.
- **Tout texte affiché est un code clé passé à `Loc.T`** (FR / EN, option du menu ; demande de l'utilisateur) : jamais de texte affiché en dur dans le code. Clé `domaine.nom` (minuscules, sans accents, `_`), ajoutée dans `Loc.Fr` **et** `Loc.En` ; `Loc.F` pour les `{0}`. Formes : `forme.<identifiant>` ; refus de pose : `refus.…`. `LocTests` vérifie que les deux dictionnaires ont les mêmes clés et que toute clé du code existe. Les tests qui vérifient des textes forcent le français (`GameSettings.SetLocale("fr", false)`).
- **Aucun ennemi ni piège fixe dans la scène de jeu** : tout vient du contenu des cartes posées (Spawner). Le donjon de démo (`Test_Donjon`) a du contenu sur ses tuiles (`DemoDungeon.Contents`) pour essayer le combat sans rien poser.
- **Antre du boss et étages** (`docs/antre.md`) : mission remplie → le jeu pose l'antre (`DungeonBoard.FindBossLair` : jamais visité, le plus loin du joueur, peut écraser des tuiles inexplorées, contre le labyrinthe, tout reste relié). Boss vaincu → escalier → `Dungeon.NextFloor` (PV gardés, nouvelle main, nouvelle mission ; choix de l'utilisateur). Tous les 3 étages (fin de sous-biome), l'escalier propose de **remonter** (tout l'or au camp, retour au titre) ou de descendre (`docs/remontee.md`). Le loup-garou est animé **par code** (`WerewolfRig`, choix de l'utilisateur : pas d'animations dans les packs) ; régler ses poses sur la planche (*Deckscent > Planche du loup-garou*). Dans l'éditeur, un maillage riggé n'est redéformé qu'une fois par image : pour un rendu hors Play mode, le cuire (`BakeMesh`).
- **Décor d'ambiance** (`docs/decor.md`, demande de l'utilisateur) : chaque tuile posée reçoit du décor après son contenu (`Ambience.Layout`, sur les cases libres) ; ce qui bloque ne va que dans les coins des salles, rien ne bloque un couloir. Les objets à briser ou à boire ont une **lueur propre** (0,25) pour se lire dans le noir.
- **Mission de l'étage** (`docs/mission.md`) : objectifs tirés au hasard, affichés dans le HUD et sur la carte. Le Spawner rattache chaque élément à sa tuile (`TileContents`) ; une salle ne compte qu'une fois **nettoyée** (monstres tués, coffres ouverts ; pas besoin de franchir ses dalles, objectif à part) **et visitée**.
- **Tour par tour** (`docs/tour-par-tour.md`) : ce qui joue après le joueur hérite de `GridEntity` (liste `GridEntity.All`, dans l'ordre d'apparition) ; un ennemi qui meurt appelle `Retire()` avant son animation. Prefabs du contenu construits par `ContentBuilder` (relancer écrase les réglages faits à la main).

## Rendu gravure

Direction (demande de l'utilisateur) : gravure à la plume en noir et blanc, inspirée de *The Secret of Weepstone* (on fait évoluer le style avec le temps, sans reprendre aucun de ses assets). Fort contraste : ce qui est proche et éclairé reste blanc et cerné d'encre, l'ombre se hachure puis tombe au noir avec la distance.

Deux étages, mis en place par *Deckscent > Installer le rendu gravure* (`RenderingSetup.cs`, relançable) :

1. **`Assets/Shaders/Toon.shader`** (`Deckscent/Toon`) sur **les matériaux des objets** : frontière nette éclairé / ombre, toutes les lumières (Forward+, boucle `LIGHT_LOOP_BEGIN`, ombres des lumières ponctuelles), décroissance continue (pas de paliers : anneaux d'encre). Texture passée en gris (`_TextureSaturation` 0) et éclaircie (`_TextureGain` 2,5 pour les atlas Synty). Passes ShadowCaster / DepthOnly / DepthNormals reprises d'URP (`ToonInput.hlsl` = CBUFFER commun). Matériaux : `Materials/Toon/M_Toon_<matériau Synty>`, et les `M_Gris_*` passés au toon. `M_Flamme` reste en Unlit blanc.
2. **`Assets/Shaders/Gravure.shader`** (`Deckscent/Gravure`, matériau `Materials/M_Gravure.mat`), **post-process plein écran** : un *Full Screen Pass Renderer Feature* « Gravure » sur `PC_Renderer` et `Mobile_Renderer` (avant le post-traitement, avec couleur, profondeur et normales). Il s'applique donc à **toute caméra** de ces renderers, pas aux vues d'aperçu (images de `docs/prefabs` : toon seul). **Coupé dans la vue Scène** par défaut (`GravureSceneView.cs` met le global `_GravureOff` à 1 pour les caméras SceneView ; l'obscurité y mettait tout au noir, et la vue Scène sert à voir les assets) ; menu *Deckscent > Gravure dans la vue Scène* pour le rallumer (EditorPrefs). `_GravureOff` ne doit **pas** être déclaré dans les Properties du shader (sinon le matériau masque le global). Réglages dans `M_Gravure.mat` :
   - contours : sauts de profondeur, de normale et de luminosité, tracé légèrement tremblé (`_LineWobble`) ;
   - pas de gris : fond papier, ton rendu **par l'épaisseur des traits** (`_WhitePoint` 0,6, `_BlackPoint` 0,1), deux séries diagonales croisées (`_ThresholdFirst` 0,85, `_ThresholdCross` 0,55). **Pas de trame de points**, pas de couches horizontales + verticales (effet « fil de fer ») ;
   - hachures **ancrées au monde** (elles ne glissent pas quand on bouge), écartement visé **en pixels** (`_HatchSpacingPx` 5) au loin (LOD par puissances de 2), jamais plus serré que `_HatchMinSpacing` sur la surface : **de près, les traits s'élargissent** (gros traits devant la caméra). Écartement mesuré avec `fwidth` (il l'exagère un peu en diagonale : traits plus larges). Ces deux points font partie de la **version sombre choisie par l'utilisateur** : une mesure plus exacte ou un LOD sans plancher donne des traits plus fins, qu'il a écartés ;
   - accents : seules les couleurs dont la saturation dépasse `_AccentSaturation` sortent du papier ;
   - **obscurité avec la distance** (demande de l'utilisateur : « sombre, sombre, plus de quadrillage à un moment ») : l'ambiante est la même partout, donc sans ça un couloir lointain resterait à mi-ton quadrillé. Le ton descend vers le noir entre `_DarkStart` (8 m) et `_DarkEnd` (16 m), jusqu'à un **aplat d'encre** (`_SolidInk` 0,08 : sous ce ton, encre pleine, sans papier entre les traits). Ce qu'une vraie lumière éclaire (luminosité au-dessus de `_LitKeep` 0,6, ex. un mur près d'une torche au loin) y échappe : un point lumineux dans le noir. Le fond (rien de dessiné) est noir, sauf au **camp** : ciel en **aplat d'encre dégradé** (pas de traits : refusés par l'utilisateur), étoiles et croissant de lune (`CampSky` : globaux `_SkyOn`, `_MoonDir`, `_SkyParams`), et obscurité repoussée (`_DarkScale`). Dans une branche du shader, pas de `fwidth` (dérivées indéfinies) ; la projection d'une passe plein écran peut être retournée (`abs(UNITY_MATRIX_P[1][1])`).
- **Version sombre, choisie par l'utilisateur** (entre plusieurs essais, le 2026-11-15) : ambiante unie 0,2 (Main et scène de test), lanterne du joueur 0,9 / 6 m, torches 2,5 / 9 m, ombres dures (`m_SoftShadowsSupported` 0). Beaucoup de hachures croisées, noir plein dans les recoins, seuls les murs près d'une torche restent blancs. Un essai plus clair (ambiante 0,3, lanterne 2 / 10 m) a été écarté. Ne pas éclaircir sans lui demander.
- Après un changement de shader, Unity ne réimporte pas toujours tout de suite : forcer `AssetDatabase.ImportAsset(…, ForceUpdate)` avant de refaire les captures, sinon elles montrent l'ancien shader.
- Pour mettre un objet en valeur : `albedo` **saturé**. Le décor reste en niveaux de gris. **Lumières blanches** uniquement. Ombres à bords durs.
- **Effets de particules** (`docs/effets.md`, pack Synty POLYGON Particle FX, non versionné) : matériaux convertis en particules URP **en mélange alpha** (l'additif disparaît dans la gravure) et **sans particules douces**. **Peu d'effets, choisis par l'utilisateur** (2026-11-20) : poussière, étoiles du boss sonné, éclats des vases et caisses. Refusés : feu des torches et braseros, coup porté, giclée des slimes, soin, **pièces d'or** (2026-11-21 : pas d'effet pour l'or).
- **Lumières du décor** (bougies, braseros) : ombres dures au palier « Low » d'URP (`BossBuilder.LowShadows`) ; une lumière sans ombre traverse les murs fins (retour de l'utilisateur).
- **Lueur propre** (`_Glow` du shader toon, 0 par défaut) : ajoutée à la lumière reçue, seulement sur les slimes (0,25, yeux compris) et les coffres (0,2), réglée dans `ContentBuilder`. Sans elle, dans la version sombre, un slime à deux cases se perdait dans les hachures. Le décor reste à 0 (captures inchangées).
- La densité des hachures dépend de la luminosité de la scène : après un changement d'éclairage, recaler les seuils.
- L'UI n'est pas touchée par le post-process : couleurs libres.

## Conventions

- C# : namespace `Deckscent`, `PascalCase` pour types et méthodes, `_camelCase` pour les champs privés, champs exposés en `[SerializeField] private`.
- Commiter les `.meta` à côté de chaque fichier d'`Assets/` (hors dossiers ignorés).
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, fichiers d'IDE : ignorés.
