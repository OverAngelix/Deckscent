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
5. **Tour par tour et contenu** : PV / ATQ / DÉF, slimes, dalle à pics, coffre, Spawner, HUD.
6. **Menu, options, musique synthétisée, FR / EN.**
7. Reprise de `docs/todo.md` (tâche 5 : salles nettoyées).

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
Assets/Scenes/Main.unity               Jeu (scène 0 du build)
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
```

**Repères de grille** : case (x, y) du plan, x vers l'est, y vers le sud (ligne 0 = nord). Monde : nord = +Z, est = +X, centre de case = `((x + 0,5) × cellSize, 0, −(y + 0,5) × cellSize)`. Orientation 0 = nord, 1 = est (yaw 90°)…

**Labyrinthe** : chaque case de sol reçoit un `Sol`, un `Plafond` et un `Mur` par côté fermé (variante tirée d'après la position). Les prefabs sont faits pour des cases de 3 m et des murs de 3,5 m : racine d'un mur au milieu du bord, face (+Z) vers la case ; sol et plafond centrés sur la case. Les objets générés vont sous un enfant `Généré` en `HideFlags.DontSave` : **jamais enregistrés dans la scène**, seul le plan l'est. Si une pièce du décor n'a pas de maillage (pack absent), tout le labyrinthe passe au jeu `fallback` (blocs gris). Torche : sur une case `T`, accrochée au premier mur voisin (N, E, S, O), à `torchHeight`.

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
  | `Interact` | F | bouton du bas (Croix / A) | coffres (étape 5) |
  | `Map` | virgule (`m`) | bouton du haut (Triangle / Y) | oui : ouvre / ferme la carte |

  Map **`Map`** (carte ouverte, voir `docs/carte.md`) : `Close` (Échap / Rond-B), `Rotate` (R / Carré-X), `Discard` (Suppr, Retour arrière / Select), `Place` (Entrée, Espace / Croix-A), `NextCard` (Tab / RB), `PreviousCard` (LB), `Cursor` (flèches / croix, stick gauche). Chiffres 1-6 et souris lus directement.

  Les autres actions de base (Attack, Jump, Crouch, Sprint, Look, Previous, Next) ne servent pas et certaines partagent des touches (Entrée = Attack, L3 = Sprint) : en tenir compte avant de les utiliser. Pour changer une touche, modifier le fichier d'actions (pas le code). Le joueur référence ses actions par `InputActionReference` (assignées par `PrototypeBuilder.ConfigurePlayer`, à rappeler sur le prefab si on ajoute une action). Carte (à venir) : R pour tourner une tuile, Suppr pour défausser, à ajouter au fichier.
- **La carte met le jeu en pause** (le joueur ne la connaît pas, l'ouvrir est gratuit) : `Time.timeScale = 0`. Le joueur ne lit pas ses touches en pause (et la lanterne ignore Rond / B, qui ferme la carte). Tout ce qui doit tourner carte ouverte utilise `Time.unscaledTime`.
- **Écran de carte, retours de l'utilisateur** : tout ce qui n'est pas du sol est hachuré pareil (roche non construite = murs, carte d'un seul tenant ; pas de roche pâle) ; proportions de l'écran Godot (CanvasScaler référence 1152 × 648) ; légende en colonne étroite ; pas de rose des vents ; l'aperçu de pose montre une flèche sur chaque entrée.
- **Les cartes gardent le style Godot** (demande de l'utilisateur) : carte à jouer gravée 112 × 172 (double filet à losanges, numéro, emprise, plan fléché, bandeau à queues d'aronde, flamme, crânes). L'UI gravure se dessine avec `InkCanvas` + `TextPool` + `Engraving`, pas avec des sprites.
- **Capturer la carte** : Play mode dans `Test_Donjon`, `script eval` qui appelle `MapScreen.Open()` (et `PreviewSlot`), puis `capture game_view` (le canvas en surimpression n'est pas rendu par `Camera.Render`, donc pas par le menu de captures). Image de la doc : `docs/images/carte.png`.
- **Tuiles** 3×3 cases, ouvertures en bits N=1, E=2, S=4, W=8, rotation horaire = décalage de bits ; salles sur plusieurs emplacements ; règles de pose volontairement permissives ; plateau 9×9 tuiles ; 10 départs. Doc : `docs/tuiles.md`.
- **Salles = vraies pièces** (demande de l'utilisateur, 2026-11-15 : « plus de salles, et variées ») : tout l'emplacement est du sol (3 × 3 cases), le pourtour est fermé par des **murs fins** sur les bords des cases (`TileCard.EdgeWalls`, `DungeonBoard.EdgeWalls`, `Maze.SetPlan(plan, murs)`), troués aux portes. Dans Godot, les salles étaient faites de cases de roche (branches d'une case de large) : abandonné. Le déplacement passe par `Maze.CanStep(case, direction)`, jamais par `IsWalkable` seul. 16 formes, dont 11 salles (salle, galerie, longue galerie, L, T, Z, U, croix, grande salle, salle à piliers, crypte).
- **Rythme façon donjon mystère** (choix de l'utilisateur) : beaucoup de couloirs étroits (la pression), des salles rares mais **denses et dangereuses**. Pioche : `roomChance` 20 % (1 sur 5), **6 salles au plus par étage** (`maxRoomsPerFloor`, posées + en main, `TileDeck.RoomsPlaced` tenu par le Donjon). Couloirs : un élément au plus, surtout vides. Salles : 2 à 7 éléments selon la taille (petite ≤ 2 emplacements, moyenne 3-4, grande ≥ 5 : `ContentDef.Small / Medium / Large`), presque jamais vides. Planche des formes : `docs/images/tuiles.png`, *Deckscent > Planche des tuiles*. Piliers : coins au milieu d'un bloc 2 × 2 d'emplacements (salle à piliers, crypte).
- **Aucun ennemi ni piège fixe dans la scène de jeu** : tout vient du contenu des cartes posées (Spawner). La vitrine sert à les voir et les régler.

## Rendu gravure

Direction (demande de l'utilisateur) : gravure à la plume en noir et blanc, inspirée de *The Secret of Weepstone* (on fait évoluer le style avec le temps, sans reprendre aucun de ses assets). Fort contraste : ce qui est proche et éclairé reste blanc et cerné d'encre, l'ombre se hachure puis tombe au noir avec la distance.

Deux étages, mis en place par *Deckscent > Installer le rendu gravure* (`RenderingSetup.cs`, relançable) :

1. **`Assets/Shaders/Toon.shader`** (`Deckscent/Toon`) sur **les matériaux des objets** : frontière nette éclairé / ombre, toutes les lumières (Forward+, boucle `LIGHT_LOOP_BEGIN`, ombres des lumières ponctuelles), décroissance continue (pas de paliers : anneaux d'encre). Texture passée en gris (`_TextureSaturation` 0) et éclaircie (`_TextureGain` 2,5 pour les atlas Synty). Passes ShadowCaster / DepthOnly / DepthNormals reprises d'URP (`ToonInput.hlsl` = CBUFFER commun). Matériaux : `Materials/Toon/M_Toon_<matériau Synty>`, et les `M_Gris_*` passés au toon. `M_Flamme` reste en Unlit blanc.
2. **`Assets/Shaders/Gravure.shader`** (`Deckscent/Gravure`, matériau `Materials/M_Gravure.mat`), **post-process plein écran** : un *Full Screen Pass Renderer Feature* « Gravure » sur `PC_Renderer` et `Mobile_Renderer` (avant le post-traitement, avec couleur, profondeur et normales). Il s'applique donc à **toute caméra** de ces renderers, pas aux vues d'aperçu (images de `docs/prefabs` : toon seul). **Coupé dans la vue Scène** par défaut (`GravureSceneView.cs` met le global `_GravureOff` à 1 pour les caméras SceneView ; l'obscurité y mettait tout au noir, et la vue Scène sert à voir les assets) ; menu *Deckscent > Gravure dans la vue Scène* pour le rallumer (EditorPrefs). `_GravureOff` ne doit **pas** être déclaré dans les Properties du shader (sinon le matériau masque le global). Réglages dans `M_Gravure.mat` :
   - contours : sauts de profondeur, de normale et de luminosité, tracé légèrement tremblé (`_LineWobble`) ;
   - pas de gris : fond papier, ton rendu **par l'épaisseur des traits** (`_WhitePoint` 0,6, `_BlackPoint` 0,1), deux séries diagonales croisées (`_ThresholdFirst` 0,85, `_ThresholdCross` 0,55). **Pas de trame de points**, pas de couches horizontales + verticales (effet « fil de fer ») ;
   - hachures **ancrées au monde** (elles ne glissent pas quand on bouge), écartement visé **en pixels** (`_HatchSpacingPx` 5) au loin (LOD par puissances de 2), jamais plus serré que `_HatchMinSpacing` sur la surface : **de près, les traits s'élargissent** (gros traits devant la caméra). Écartement mesuré avec `fwidth` (il l'exagère un peu en diagonale : traits plus larges). Ces deux points font partie de la **version sombre choisie par l'utilisateur** : une mesure plus exacte ou un LOD sans plancher donne des traits plus fins, qu'il a écartés ;
   - accents : seules les couleurs dont la saturation dépasse `_AccentSaturation` sortent du papier ;
   - **obscurité avec la distance** (demande de l'utilisateur : « sombre, sombre, plus de quadrillage à un moment ») : l'ambiante est la même partout, donc sans ça un couloir lointain resterait à mi-ton quadrillé. Le ton descend vers le noir entre `_DarkStart` (8 m) et `_DarkEnd` (16 m), jusqu'à un **aplat d'encre** (`_SolidInk` 0,08 : sous ce ton, encre pleine, sans papier entre les traits). Ce qu'une vraie lumière éclaire (luminosité au-dessus de `_LitKeep` 0,6, ex. un mur près d'une torche au loin) y échappe : un point lumineux dans le noir. Le fond (rien de dessiné) est noir.
- **Version sombre, choisie par l'utilisateur** (entre plusieurs essais, le 2026-11-15) : ambiante unie 0,2 (Main et scène de test), lanterne du joueur 0,9 / 6 m, torches 2,5 / 9 m, ombres dures (`m_SoftShadowsSupported` 0). Beaucoup de hachures croisées, noir plein dans les recoins, seuls les murs près d'une torche restent blancs. Un essai plus clair (ambiante 0,3, lanterne 2 / 10 m) a été écarté. Ne pas éclaircir sans lui demander.
- Après un changement de shader, Unity ne réimporte pas toujours tout de suite : forcer `AssetDatabase.ImportAsset(…, ForceUpdate)` avant de refaire les captures, sinon elles montrent l'ancien shader.
- Pour mettre un objet en valeur : `albedo` **saturé**. Le décor reste en niveaux de gris. **Lumières blanches** uniquement. Ombres à bords durs.
- La densité des hachures dépend de la luminosité de la scène : après un changement d'éclairage, recaler les seuils.
- L'UI n'est pas touchée par le post-process : couleurs libres.

## Conventions

- C# : namespace `Deckscent`, `PascalCase` pour types et méthodes, `_camelCase` pour les champs privés, champs exposés en `[SerializeField] private`.
- Commiter les `.meta` à côté de chaque fichier d'`Assets/` (hors dossiers ignorés).
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, fichiers d'IDE : ignorés.
