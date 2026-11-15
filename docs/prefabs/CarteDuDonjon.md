# CarteDuDonjon

`Assets/Prefabs/UI/CarteDuDonjon.prefab` · [← liste des prefabs](README.md)

Écran de carte : le plateau et la main de cartes-tuiles, dessinés en gravure (InkCanvas + TextPool), pour poser les tuiles. Canvas en surimpression, au-dessus du jeu et hors du post-process. Met le jeu en pause quand il est ouvert. Voir docs/carte.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
CarteDuDonjon  [Canvas, CanvasScaler, MapScreen]
├─ Encre  [CanvasRenderer, InkCanvas]
└─ Textes  [TextPool]
```

## Contenu

### CarteDuDonjon

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **MapScreen**
  - `dungeon` = (vide) — Le donjon (plateau + pioche). Vide : cherché dans la scène.
  - `player` = (vide) — Le joueur (flèche rouge sur la carte). Vide : cherché dans la scène.
  - `ink` = `Encre`
  - `texts` = `Textes`
  - `toggle` = `Player/Map` — Ouvrir / fermer la carte (Player/Map).
  - `close` = `Map/Close`
  - `rotate` = `Map/Rotate`
  - `discard` = `Map/Discard`
  - `place` = `Map/Place`
  - `nextCard` = `Map/NextCard`
  - `previousCard` = `Map/PreviousCard`
  - `cursor` = `Map/Cursor`
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `wallHatchSpacing` = 6 — Écartement des hachures de la roche et des murs (plus petit = plus sombre). Tout ce qui n'est pas du sol est hachuré pareil : la carte est d'un seul tenant.
  - `hatchColor` = (0.42, 0.42, 0.43) — Couleur des hachures du plateau : un gris encre, plus doux que l'encre pleine (des traits noirs serrés sur toute la carte papillotent).
  - `validColor` = (0.15, 0.6, 0.25)
  - `invalidColor` = (0.85, 0.12, 0.1)
  - `playerColor` = (0.85, 0.1, 0.1) — Flèche du joueur et carte choisie (rouge d'accent, comme en jeu).
  - `backdropColor` = (0.02, 0.02, 0.03) — Voile sombre derrière la carte.
  - `screenFill` = 0.95 — Part de l'écran occupée par la carte.
  - `biomeKey` = `biome.sous_sols` — Clé de traduction (voir Loc) du nom du sous-biome affiché à côté du titre (provisoire, viendra avec les étages).

### CarteDuDonjon / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### CarteDuDonjon / Textes

- **TextPool**
