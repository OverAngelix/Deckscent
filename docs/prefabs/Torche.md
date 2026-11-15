# Torche

`Assets/Prefabs/Maze/Torche.prefab` · [← liste des prefabs](README.md)

Torche murale, posée sur les cases `T`, accrochée au premier mur voisin. Sa racine est sur le mur, +Z vers la case.

![Torche](images/Torche.png)

## Hiérarchie

```
Torche
├─ Bâton
│  └─ Pièce
│     └─ SM_Prop_TorchStick_01  [MeshFilter, MeshRenderer]
├─ Flamme
│  └─ Forme  [MeshFilter, MeshRenderer]
└─ Lumière  [Light, TorchFlicker]
```

## Contenu

### Torche / Bâton

- **Transform**
  - position (0, -0.35, 0.12)
  - rotation (20, 0, 0)°

### Torche / Bâton / Pièce

- **Transform**
  - échelle (0.5, 0.5, 0.5)

### Torche / Bâton / Pièce / SM_Prop_TorchStick_01

- **MeshFilter**
  - maillage `SM_Prop_TorchStick_01` (`Assets/Synty/PolygonDungeon/Models/SM_Prop_TorchStick_01.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonDungeon_01` (Deckscent/Toon)
  - ombres portées : Off

### Torche / Flamme / Forme

- **Transform**
  - position (0, 0.3, 0.36)
  - échelle (0.2, 0.28, 0.2)
- **MeshFilter**
  - maillage `Sphere` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Flamme` (Universal Render Pipeline/Unlit)
  - ombres portées : Off

### Torche / Lumière

- **Transform**
  - position (0, 0.3, 0.6)
- **Light**
  - Point, couleur (1, 0.97, 0.92), intensité 2.5, portée 9 m, ombres Hard
- **TorchFlicker**
  - `baseIntensity` = 2.5 — Intensité moyenne de la torche.
  - `flicker` = 0.35 — Amplitude du vacillement.
