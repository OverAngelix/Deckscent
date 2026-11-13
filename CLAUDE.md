# Deckscent (Unity)

Dungeon crawler en vue à la première personne, déplacement case par case **au tour par tour**, où le joueur **construit son labyrinthe** en posant des cartes-tuiles depuis la carte. Rendu stylisé façon BD / Moebius : noir et blanc, contours à l'encre, hachures progressives dans les zones sombres, couleur réservée aux accents.

**Unity 6.3 LTS** (6000.3.11f1), **URP**, C#. Portage du prototype Godot de `C:\Users\OverA\Documents\dungeon-crawler-prototype` : c'est **la référence** (code, réglages, docs `docs/gdd.md`, `docs/todo.md`, `docs/features/`, `docs/monsters/`) tant que ce projet ne l'a pas rattrapé. On ne modifie pas le projet Godot : on le lit pour traduire.

## Manière de travailler (préférences de l'utilisateur)

- **Tout en français** : messages, commentaires, noms affichés dans l'inspecteur (`[Tooltip]`, `[Header]`), messages de commit, docs.
- **Répondre avant d'agir** sur un retour de jeu (« ça fait bizarre quand… ») : proposer la correction et attendre son oui avant de coder.
- **Commiter quand c'est fini**, sans demander : un commit par fonctionnalité, puis `git push` (remote `origin` = GitHub `OverAngelix/Deckscent`).
- Commits au format **gitmoji** en français (`✨ Ajoute ...`, `🐛 Corrige ...`, `🎨 ...`, `🔧 ...`, `📝 ...`, `♻️ ...`). **Jamais de ligne `Co-Authored-By`** ni autre mention d'attribution.
- Quand une fonctionnalité documentée change, **mettre la doc à jour** dans le même travail.

## Plan du portage

Dans l'ordre (chaque étape donne quelque chose de jouable ou de visible) :

0. ✅ Ce fichier.
1. ✅ **Labyrinthe + joueur** : `Maze` (plan texte → murs, sols, plafond, torches Synty), déplacement en grille, tourner, bump.
2. **Rendu gravure** : shader toon + post-process Moebius (Renderer Feature URP), validé par captures.
3. **Tuiles, plateau, pioche** en C# pur, avec tests EditMode.
4. **Écran de carte** dessiné en gravure, pose à la souris.
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
Assets/Scripts/Player/PlayerController.cs  Déplacement en grille, tours, touches (InputAction dans l'inspecteur)
Assets/Scripts/Editor/PrototypeBuilder.cs  Menu Deckscent > Construire… : prefabs, scène Main, scène de test
Assets/Scripts/Editor/TestCaptures.cs  Menu Deckscent > Captures de la scène de test
Assets/Prefabs/Player.prefab           Joueur : PlayerController, Caméra (1,6 m, FOV 75), Lanterne
Assets/Prefabs/Maze/                   Mur_01/03/04, Sol, Plafond (Synty), Mur_Gris, Sol_Gris, Plafond_Gris (secours), Torche
Assets/Materials/                      M_Gris_* (secours), M_Flamme
Assets/Scenes/Main.unity               Jeu (scène 0 du build)
Assets/Scenes/Tests/Test_Labyrinthe.unity  Scène de test, plan figé (PrototypeBuilder.TestPlan)
Assets/Tests/PlayMode/                 Tests PlayMode (assembly Deckscent.Tests.PlayMode), clavier virtuel via InputTestFixture
```

**Repères de grille** : case (x, y) du plan, x vers l'est, y vers le sud (ligne 0 = nord). Monde : nord = +Z, est = +X, centre de case = `((x + 0,5) × cellSize, 0, −(y + 0,5) × cellSize)`. Orientation 0 = nord, 1 = est (yaw 90°)…

**Labyrinthe** : chaque case de sol reçoit un `Sol`, un `Plafond` et un `Mur` par côté fermé (variante tirée d'après la position). Les prefabs sont faits pour des cases de 3 m et des murs de 3,5 m : racine d'un mur au milieu du bord, face (+Z) vers la case ; sol et plafond centrés sur la case. Les objets générés vont sous un enfant `Généré` en `HideFlags.DontSave` : **jamais enregistrés dans la scène**, seul le plan l'est. Si une pièce du décor n'a pas de maillage (pack absent), tout le labyrinthe passe au jeu `fallback` (blocs gris). Torche : sur une case `T`, accrochée au premier mur voisin (N, E, S, O), à `torchHeight`.

**Prefabs construits par script** : `PrototypeBuilder` copie le maillage et les matériaux des pièces Synty (pas de prefab imbriqué : un prefab Synty manquant ne se détecterait pas). Relancer le menu **écrase** les prefabs : une fois réglés à la main dans l'éditeur, ne le relancer que pour repartir de zéro (ou reporter les réglages dans le script).

**Scène de test** (`Test_Labyrinthe`, plan figé) : repère commun des captures et des tests. Ne pas changer son plan sans mettre à jour les poses de `TestCaptures` et les tests. Captures (`Captures/` à la racine, non versionné) : 5 vues joueur + 1 vue de dessus, rendues dans l'éditeur sans Play mode (torches figées, images comparables).

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
- **Touches** en positions physiques (ZQSD en AZERTY = WASD en QWERTY) : avancer / reculer, pas de côté, A / E (← →) pour tourner, Espace pour attendre, la carte, R pour tourner une tuile, Suppr pour défausser. Utiliser l'Input System avec des bindings par position de touche.
- **La carte met le jeu en pause** (le joueur ne la connaît pas, l'ouvrir est gratuit).
- **Tuiles** 3×3 cases, ouvertures en bits N=1, E=2, S=4, W=8, rotation horaire = décalage de bits ; salles sur plusieurs emplacements ; règles de pose volontairement permissives ; plateau 9×9 tuiles ; 10 départs.
- **Aucun ennemi ni piège fixe dans la scène de jeu** : tout vient du contenu des cartes posées (Spawner). La vitrine sert à les voir et les régler.

## Rendu (cible, depuis `shaders/` du projet Godot)

1. **Toon** sur tous les matériaux : frontière nette éclairé / ombre, décroissance des lumières continue (pas de paliers : ça crée des anneaux d'encre).
2. **Moebius**, post-process plein écran (profondeur + normales : activer la texture de normales URP / prépasse DepthNormals) :
   - contours : sauts de profondeur, de normale et de luminosité ;
   - style gravure : pas de gris, fond papier, ton rendu **par l'épaisseur des traits** (`whitePoint`, `blackPoint`), deux séries diagonales croisées (`thresholdFirst`, `thresholdCross`). **Pas de trame de points**, pas de couches horizontales + verticales (effet « fil de fer ») ;
   - hachures **ancrées au monde** (elles ne glissent pas quand on bouge), écartement constant **en pixels**, un trait sur deux s'efface au loin (LOD par puissances de 2) ;
   - accents : seules les couleurs dont la saturation dépasse un seuil sortent du papier.
- Pour mettre un objet en valeur : `albedo` **saturé**. Le décor reste en niveaux de gris. **Lumières blanches** uniquement. Ombres à bords durs.
- La densité des hachures dépend de la luminosité de la scène : après un changement d'éclairage, recaler les seuils.
- L'UI n'est pas touchée par le post-process : couleurs libres.

## Conventions

- C# : namespace `Deckscent`, `PascalCase` pour types et méthodes, `_camelCase` pour les champs privés, champs exposés en `[SerializeField] private`.
- Commiter les `.meta` à côté de chaque fichier d'`Assets/` (hors dossiers ignorés).
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, fichiers d'IDE : ignorés.
