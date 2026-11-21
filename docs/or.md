# Or

Décidé avec l'utilisateur le 2026-11-21 (premier pas vers le hub, voir [todo.md](todo.md) et le [GDD](gdd.md#7-hub-surface)).

## Règles

- On trouve de l'or **dans les coffres** (toujours, 8 à 15, en plus de la carte ou des PV), **dans les vases et caisses** (35 % de chance, 1 à 4, si ni potion ni carte) et **sur les monstres** (la moitié de leur XP, au moins 1 : un monstre dur ou profond rapporte plus).
- L'or ramassé pendant l'expédition est **porté** : il n'est à l'abri qu'une fois au hub.
- **À la mort**, on n'en garde que **10 %** (arrondi en dessous) : de quoi s'améliorer au hub et aller plus loin. Le reste est perdu.
- **Quitter** l'expédition en cours (menu, recommencer, bureau) compte comme une mort.
- **En remontant** (après le boss du 3ᵉ étage, voir [remontee.md](remontee.md)), on garde tout.

## Où on le voit

- **Carte** : dans l'en-tête, après le nom du sous-sol (pièce gravée et montant). **Pas dans le HUD** (choix de l'utilisateur).
- **Messages** : « Coffre : 12 or et +3 PV », « Quelques pièces cachées dedans : +2 or ».
- **Écran de mort** : « Or ramassé : 54 · Or pour le camp : 5 » (sans pièce).
- **Pas d'effet de particules** pour l'or (choix de l'utilisateur).

## Réglages (inspecteur)

| Où | Réglage | Défaut |
|---|---|---|
| Joueur (`PlayerController`) | Part gardée à la mort | 0,1 |
| Coffre (`Chest`) | Or trouvé (bornes comprises) | 8 – 15 |
| Vase, caisse (`Breakable`) | Chance d'or, or trouvé | 35 %, 1 – 4 |
| Carte | Couleur de la pièce | or saturé |

Or des monstres : `Enemy.GoldReward` (moitié de `ExperienceReward`).

## Technique

- `Gold` (`Assets/Scripts/Progression/Gold.cs`) : or porté (`Carried`), `Add`, `EndOnDeath` (part gardée vers la progression), `EndOnReturn` (tout). Remis à zéro par `Dungeon.NewGame`.
- `Progression` (`Progression.cs`) : ce qui dure d'une expédition à l'autre (pour le moment l'or mis à l'abri), en JSON dans `Application.persistentDataPath/progression.json`. Les tests le détournent vers un fichier temporaire (`Progression.FilePath`).
- La mort règle l'or dans `PlayerController.Die` ; un départ de la scène de jeu dans `SceneFlow` (abandon).
- Tests : `GoldTests` (EditMode).
