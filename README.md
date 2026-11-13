# Deckscent

*Deckscent* : **deck** + **descent**. Dungeon crawler dark fantasy en vue subjective, au tour par tour sur une grille, où le joueur construit le donjon qu'il doit survivre en posant des cartes-tuiles. Rendu gravure noir et blanc façon BD.

Portage sous **Unity 6.3 LTS** (6000.3.11f1, URP) du prototype Godot (`dungeon-crawler-prototype`).

## Packages utilisés

Les packs sous licence ne sont **pas versionnés** (`.gitignore`) : la licence interdit de les redistribuer. Sur un nouveau clone, il faut les réimporter à la main avant d'ouvrir les scènes qui s'en servent.

### Packs Synty (importés dans `Assets/Synty/`)

| Pack | Version | Dossier | Pour quoi faire |
|---|---|---|---|
| POLYGON Dungeon | 1.10.2 | `Assets/Synty/PolygonDungeon` | Murs, sols, plafonds, portes, torches, coffres : le décor du labyrinthe. |
| POLYGON Dungeon Realms | 1.4.5 | `Assets/Synty/PolygonDungeonRealms` | Décor de donjon en plus (variantes, structures) pour les salles et les biomes. |
| POLYGON Werewolf | 1.3.1 | `Assets/Synty/PolygonWerewolf` | Le loup-garou (ennemi prévu dans la todo). |
| POLYGON Fantasy Characters | 1.6.1 | `Assets/Synty/PolygonFantasyCharacters` | Personnages : PNJ du hub, héros des classes. |
| POLYGON Fantasy Rivals | 1.4.2 | `Assets/Synty/PolygonFantasyRivals` | Ennemis humanoïdes (rivaux, boss). |
| POLYGON Icons | 1.2.2 | `Assets/Synty/PolygonIcons` | Icônes 3D (objets, butin, UI). |

Fichiers d'origine : `POLYGON_<Pack>_Unity_2022_3_v<version>.unitypackage` (achetés sur le site Synty). Ils sont faits pour Unity 2022.3 et s'importent tels quels dans Unity 6.

Partagés entre tous les packs, et créés par le premier importé :
- `Assets/Synty/PolygonGeneric` : shaders Synty (Shader Graph, compatibles URP), textures et matériaux communs.
- `Assets/Synty/SyntyPackageHelper` : petit outil d'éditeur fourni par Synty.

**Règle (reprise du projet Godot)** : on ne garde que les **maillages et les textures** des pièces Synty, avec notre propre matériau toon par-dessus. Le jeu doit rester jouable sans les packs : rien de versionné ne doit dépendre d'un fichier d'`Assets/Synty/` sans solution de repli.

### Pour réimporter

Unity fermé, depuis la racine du projet :

```bash
unity assets import "<chemin>/POLYGON_Dungeon_Unity_2022_3_v1_10_2.unitypackage"
```

Ou, Unity ouvert : *Assets > Import Package > Custom Package…*

### Packages Unity (Package Manager, versionnés dans `Packages/manifest.json`)

| Package | Pour quoi faire |
|---|---|
| `com.unity.render-pipelines.universal` (URP) | Rendu. Le post-process Moebius sera une Renderer Feature. |
| `com.unity.pipeline` | Pilotage de l'éditeur par le Unity CLI et son serveur MCP (voir plus bas). |

### Prévus, pas encore installés

| Besoin | Package | Notes |
|---|---|---|
| Shake d'écran, recul au coup | **Cinemachine** (`com.unity.cinemachine`, gratuit) | Impulse Source / Impulse Listener sur la caméra du joueur. |
| Tweens (déplacements, flashs, impacts) | **DOTween** (gratuit) ou coroutines | Remplace les `Tween` de Godot. |
| Éclairage toon | Shader maison (portage de `toon.gdshader`) | Réglé avec les seuils des hachures Moebius. |

## Outils

- **Unity CLI** (`winget install Unity.CLI`) : `unity status`, `unity command <groupe>`, `unity assets import`, `unity test`, `unity build`.
- **MCP pour Claude Code** : `.mcp.json` lance `unity mcp --tool-groups` attaché à ce projet. Garder l'éditeur ouvert pour que le serveur réponde (port 7800 par défaut).
