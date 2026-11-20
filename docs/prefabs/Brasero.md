# Brasero

`Assets/Prefabs/Props/Antre/Brasero.prefab` · [← liste des prefabs](README.md)

Décor de l'antre du boss : brasero (Synty `SM_Prop_Brazier_01`), flamme et lumière blanche sans ombre, dans les coins et de part et d'autre du trône. Bloque sa case. Voir docs/antre.md.

![Brasero](images/Brasero.png)

## Hiérarchie

```
Brasero  [DecorFallback, LairDecor]
├─ Décor
│  └─ Pièce
│     └─ SM_Prop_Brazier_01  [MeshFilter, MeshRenderer]
├─ Secours
│  ├─ Pied  [MeshFilter, MeshRenderer]
│  └─ Vasque  [MeshFilter, MeshRenderer]
├─ Flamme  [MeshFilter, MeshRenderer]
└─ Lumière  [Light, UniversalAdditionalLightData, TorchFlicker]
```

## Contenu

### Brasero

- **DecorFallback**
  - `decor` = `Décor` — La pièce Synty (enfant).
  - `fallback` = `Secours` — La version en formes simples, affichée si le décor n'a pas ses maillages.
  - `forceFallback` = non — Coché : toujours le secours, même si le décor est là (pour comparer).
- **LairDecor**
  - `blocks` = oui — Coché : bloque la case (joueur, monstres, charge). Décoché : décor de passage (ossements au sol…).

### Brasero / Décor / Pièce

- **Transform**
  - position (0, 0.006, 0.004)
  - échelle (0.895, 0.895, 0.895)

### Brasero / Décor / Pièce / SM_Prop_Brazier_01

- **MeshFilter**
  - maillage `SM_Prop_Brazier_01` (`Assets/Synty/PolygonDungeon/Models/SM_Prop_Brazier_01.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonDungeon_01` (Deckscent/Toon)

### Brasero / Secours / Pied

- **Transform**
  - position (0, 0.45, 0)
  - échelle (0.15, 0.45, 0.15)
- **MeshFilter**
  - maillage `Cylinder` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Antre` (Deckscent/Toon)

### Brasero / Secours / Vasque

- **Transform**
  - position (0, 0.95, 0)
  - échelle (0.7, 0.1, 0.7)
- **MeshFilter**
  - maillage `Cylinder` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Antre` (Deckscent/Toon)

### Brasero / Flamme

- **Transform**
  - position (0, 1.25, 0)
  - échelle (0.35, 0.5, 0.35)
- **MeshFilter**
  - maillage `Sphere` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Flamme` (Universal Render Pipeline/Unlit)
  - ombres portées : Off

### Brasero / Lumière

- **Transform**
  - position (0, 1.7, 0)
- **Light**
  - Point, couleur (1, 1, 1), intensité 3, portée 9 m, ombres Hard
- **UniversalAdditionalLightData**
- **TorchFlicker**
  - `baseIntensity` = 2.5 — Intensité moyenne de la torche.
  - `flicker` = 0.35 — Amplitude du vacillement.
  - `howlReach` = 20 — Quand un loup-garou hurle à moins de cette distance (mètres), la flamme s'embrase un instant. 0 = jamais.
