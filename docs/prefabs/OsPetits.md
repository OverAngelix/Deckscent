# OsPetits

`Assets/Prefabs/Props/Decor/OsPetits.prefab` · [← liste des prefabs](README.md)

Quelques os au pied d'un mur (Synty `SM_Env_BonePile_Small_02`). De passage. Voir docs/decor.md.

![OsPetits](images/OsPetits.png)

## Hiérarchie

```
OsPetits  [DecorFallback, LairDecor]
├─ Décor
│  └─ Pièce
│     └─ SM_Env_BonePile_Small_02  [MeshFilter, MeshRenderer]
└─ Secours
   ├─ Os  [MeshFilter, MeshRenderer]
   └─ Os  [MeshFilter, MeshRenderer]
```

## Contenu

### OsPetits

- **DecorFallback**
  - `decor` = `Décor` — La pièce Synty (enfant).
  - `fallback` = `Secours` — La version en formes simples, affichée si le décor n'a pas ses maillages.
  - `forceFallback` = non — Coché : toujours le secours, même si le décor est là (pour comparer).
- **LairDecor**
  - `blocks` = non — Coché : bloque la case (joueur, monstres, charge). Décoché : décor de passage (ossements au sol…).

### OsPetits / Décor / Pièce

- **Transform**
  - position (0, 0.013, 0)
  - échelle (0.604, 0.604, 0.604)

### OsPetits / Décor / Pièce / SM_Env_BonePile_Small_02

- **MeshFilter**
  - maillage `SM_Env_BonePile_Small_02` (`Assets/Synty/PolygonDungeon/Models/SM_Env_BonePile_Small_02.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonDungeon_01` (Deckscent/Toon)

### OsPetits / Secours / Os

- **Transform**
  - position (0, 0.05, 0)
  - rotation (90, 20, 0)°
  - échelle (0.07, 0.22, 0.07)
- **MeshFilter**
  - maillage `Capsule` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Os` (Deckscent/Toon)

### OsPetits / Secours / Os

- **Transform**
  - position (0.15, 0.05, 0.1)
  - rotation (90, 100, 0)°
  - échelle (0.07, 0.18, 0.07)
- **MeshFilter**
  - maillage `Capsule` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Os` (Deckscent/Toon)
