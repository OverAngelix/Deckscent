# Escalier

`Assets/Prefabs/Props/Escalier.prefab` · [← liste des prefabs](README.md)

Escalier vers l'étage suivant (colimaçon Synty `SM_Env_SpiralStairs_01`, ou marches en formes simples), avec une lueur blanche. Sort du sol à la place du boss vaincu ; on le prend en avançant dessus ou avec Interact. Voir docs/antre.md.

![Escalier](images/Escalier.png)

## Hiérarchie

```
Escalier  [DecorFallback, Stairs, AudioSource, SoundEmitter]
├─ Décor
│  └─ Pièce
│     └─ SM_Env_SpiralStairs_01  [MeshFilter, MeshRenderer]
├─ Secours
│  ├─ Pilier  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  ├─ Marche  [MeshFilter, MeshRenderer]
│  └─ Marche  [MeshFilter, MeshRenderer]
└─ Lueur  [Light]
```

## Contenu

### Escalier

- **DecorFallback**
  - `decor` = `Décor` — La pièce Synty (enfant).
  - `fallback` = `Secours` — La version en formes simples, affichée si le décor n'a pas ses maillages.
  - `forceFallback` = non — Coché : toujours le secours, même si le décor est là (pour comparer).
- **Stairs**
  - `riseDepth` = 1.2 — Hauteur dont il sort du sol à son apparition (mètres).
  - `riseTime` = 1.2 — Durée de la sortie du sol (secondes).
  - `sounds` = `Escalier` — Ce qui joue le raclement de pierre. Vide : cherché sur l'escalier.
- **AudioSource**
- **SoundEmitter**
  - `pitchJitter` = 0.08 — Variation de hauteur au hasard, en plus ou en moins (0,08 = ±8 %).

### Escalier / Décor / Pièce

- **Transform**
  - position (1.228, 0.1, 1.277)
  - échelle (0.249, 0.249, 0.249)

### Escalier / Décor / Pièce / SM_Env_SpiralStairs_01

- **MeshFilter**
  - maillage `SM_Env_SpiralStairs_01` (`Assets/Synty/PolygonDungeon/Models/SM_Env_SpiralStairs_01.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonDungeon_Lueur` (Deckscent/Toon)

### Escalier / Secours / Pilier

- **Transform**
  - position (0, 1.6, 0)
  - échelle (0.35, 1.6, 0.35)
- **MeshFilter**
  - maillage `Cylinder` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (0, 0.15, 0.7)
  - rotation (0, 90, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (0.411, 0.45, 0.566)
  - rotation (0, 126, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (0.666, 0.75, 0.216)
  - rotation (0, 162, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (0.666, 1.05, -0.216)
  - rotation (0, 198, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (0.411, 1.35, -0.566)
  - rotation (0, 234, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (0, 1.65, -0.7)
  - rotation (0, 270, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (-0.411, 1.95, -0.566)
  - rotation (0, 306, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (-0.666, 2.25, -0.216)
  - rotation (0, 342, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (-0.666, 2.55, 0.216)
  - rotation (0, 18, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Secours / Marche

- **Transform**
  - position (-0.411, 2.85, 0.566)
  - rotation (0, 54, 0)°
  - échelle (1.1, 0.12, 0.45)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Escalier` (Deckscent/Toon)

### Escalier / Lueur

- **Transform**
  - position (0, 1.2, 0)
- **Light**
  - Point, couleur (1, 1, 1), intensité 2, portée 7 m, ombres None
