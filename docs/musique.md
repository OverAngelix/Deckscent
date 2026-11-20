# Musique du donjon : un thème qui suit la situation

En jeu, une musique d'**aventure et d'exploration** joue en continu. C'est **un seul morceau**, découpé en **couches** qui jouent toutes en même temps, sur la même horloge. Selon la situation, chaque couche monte ou s'efface : le thème ne s'arrête jamais, il s'enrichit ou s'allège. Elle est **synthétisée en temps réel**, sans fichier audio, sur la piste **Musique** du mixer (options Général et Musique, voir [sons.md](sons.md)).

Les sons graves (sous 100 Hz) passent mal dans les casques et les enceintes : tout ce qui doit s'entendre est dans le médium. Une première version de la menace (battement de cœur à 55 Hz, notes à 73 Hz) passait inaperçue.

Le menu garde sa propre ambiance, plus sombre ([menu.md](menu.md#musique)).

## Les situations

| Couche | Quand | Ce qu'on entend |
|---|---|---|
| **Exploration** (base) | toujours | basse pincée, arpège de harpe, nappe douce, tambour sur cadre, et une mélodie de flûte sur 8 mesures |
| **Menace** | un ou plusieurs **monstres en vue** | l'**entre-deux** entre exploration et combat : des cordes en ostinato (croches courtes et répétées dans le médium), un trémolo de deux notes qui frottent (ré 4 et mi♭ 4) ; à partir de **2 monstres**, un tambour feutré sur les temps 1 et 3. La flûte continue plus bas (60 % au plus fort), la harpe s'assombrit. Plus il y a de monstres en vue, plus elle monte |
| **Combat** | un **coup échangé** : le joueur frappe ou est touché (monstre ou piège) | tambours façon taïko, caisse claire, grelot, basse qui martèle, roulement de toms toutes les 4 mesures ; la flûte se tait |
| **Coffre** | un **coffre fermé en vue**, à 3 cases au plus, **sans monstre en vue ni combat** | légère variation : un scintillement de glockenspiel par-dessus |
| **Boss** | le boss est là (`MusicDirector.BossMode`) | forte variation : autre suite d'accords, plus sombre (ré phrygien), cor grave à la place de la flûte, tambours lourds, cymbale à l'entrée |

**Le morceau.** Ré dorien, 100 à la noire.
- **Exploration** : la suite d'accords est Rém, Do, Sol, Rém, Fa, Do, Sol, La ; le La majeur relance la phrase.
- **Variations de la flûte** : elle se repose une phrase sur quatre, et la quatrième monte d'une octave, plus douce. Il faut environ 77 s pour retomber exactement sur la même chose.
- **Boss** : la suite d'accords est Rém, Mi♭, Si♭, La.

**Les transitions** restent musicales :
- **les volumes glissent vite** : une couche monte en 0,6 s et redescend en 1,5 s ; le combat monte en 0,15 s (retour de l'utilisateur : les passages d'un état à l'autre étaient trop lents) ;
- **les entrées sont calées sur le rythme** : le combat démarre à la croche suivante (0,3 s au plus), le coffre au temps suivant (0,6 s au plus), le boss à la mesure suivante (2,4 s au plus) : sa cymbale et son coup de tambour grave tombent sur le premier temps ;
- **à la mort du joueur**, le morceau s'éteint en 4 s.

## Qui fait quoi

Le prefab `Assets/Prefabs/Audio/MusiqueDonjon.prefab` porte trois composants, posés dans les scènes `Main` et `Test_Donjon` :

- **`MusicDirector`**, le **chef d'orchestre** (`Assets/Scripts/Audio/MusicDirector.cs`). Il regarde la situation et la donne au musicien. Il recalcule après chaque tour, à chaque coup échangé, et 5 fois par seconde (les monstres bougent, la carte en fait apparaître).
  - **Monstre en vue** : à moins de `Sight Radius` cases (6) à vol d'oiseau, sans mur entre lui et le joueur. La ligne passe de case en case et s'arrête à une case de roche ou à un mur fin, comme `Maze.CanStep` (`MusicDirector.Sees`). Un monstre dans le dos compte aussi : sinon, se retourner ferait changer la musique.
  - **Niveau de menace** : `Threat One Monster` (0,5) pour un monstre, plus `Threat Per Extra Monster` (0,25) pour chaque monstre en plus.
  - **Combat** : il commence au premier coup (`PlayerController.Struck` ou `Hurt`) et s'arrête après `Calm Turns` (2) tours sans aucun coup.
  - **Coffre** : un coffre fermé en vue à moins de `Chest Radius` cases (3).
  - Le **combat** se déclenche aux coups échangés avec un **ennemi** (`PlayerController.Struck`, `Hurt`) : briser un vase ou une caisse ne compte pas.
  - **Boss** : `BossMode`, que le [loup-garou](monstres/loup-garou.md) allume à son réveil et éteint à sa mort. La case **`Force Boss`** permet de l'essayer en Play mode sans lui.
- **`DungeonMusic`**, le **musicien** (`Assets/Scripts/Audio/DungeonMusic.cs`). Il reçoit la situation et joue, dans `OnAudioFilterRead` d'une `AudioSource` sans clip. Il porte les réglages :
  - `Tempo` ;
  - `Master` ;
  - `Fade In` ;
  - le volume maximal de chaque couche (`Base Level`, `Threat Level`, `Combat Level`, `Chest Level`, `Boss Level`) ;
  - les durées de transition (`Fade Up`, `Fade Down`).
- **`AdventureSynth`**, le **synthétiseur** (`Assets/Scripts/Audio/AdventureSynth.cs`). C'est une classe sans MonoBehaviour, testable hors Play mode :
  - l'horloge, en doubles croches ;
  - la partition : accords, mélodies, rythmes ;
  - les instruments :
    - cordes pincées (algorithme de Karplus-Strong : un bruit qui boucle et s'adoucit, d'où un son de vraie corde) ;
    - cordes frottées courtes (ostinato de la menace) ;
    - nappe ;
    - flûte avec vibrato et souffle ;
    - cor dont les harmoniques s'ouvrent ;
    - glockenspiel ;
    - tambours (peau qui chute vers le grave) ;
  - une réverbération de salle de pierre (Freeverb simplifié).

## Changer la musique

- **Le dosage** : les volumes de couche sur `DungeonMusic`, les seuils sur `MusicDirector` (dans le prefab).
- **Les notes** : les tableaux de `AdventureSynth`.
  - `ExploreRoots` et `ExploreMinor` (accords), `ExploreMelody` (mesure, pas, note MIDI, durée) ;
  - pour le boss, `BossRoots`, `BossMinor` et `BossMelody` ;
  - les rythmes sont dans `OnStep`, avec une ligne par instrument et par couche.
- **De vrais morceaux** : seul le musicien changera, le chef d'orchestre restera le même. Il faudra une piste par couche (exploration, menace, combat, coffre, boss), toutes au **même tempo**, dans la **même tonalité**, de la **même durée** et coupées pour boucler. On les lancera ensemble (`AudioSource.PlayScheduled`) et on ne jouera que sur leurs volumes. Un morceau entier différent pour le boss, en fondu enchaîné, marcherait aussi.

## Tests

- `AdventureSynthTests` (EditMode, rendu hors Play mode) :
  - l'exploration joue seule au départ ;
  - le combat entre puis repart ;
  - la menace suit sa cible en moins d'une seconde ; c'est un entre-deux (cordes en ostinato, sans les tambours du combat), avec un tambour feutré à partir de 2 monstres ;
  - le coffre scintille seulement quand il est proche ;
  - le boss arrive à la mesure suivante avec une cymbale ;
  - ni NaN ni saturation ;
  - le fondu de sortie mène au silence.
- `MusicTests` (PlayMode, scène de test) :
  - la musique sort sur la piste Musique ;
  - un monstre derrière la roche ne compte pas, un monstre en vue oui, et deux font monter la menace ;
  - le combat dure 2 tours calmes après le dernier coup ;
  - la variation du coffre disparaît si un monstre est en vue ;
  - `BossMode` allume le boss.

Les tests vérifient la logique, pas l'oreille : le rendu se juge en jouant.
