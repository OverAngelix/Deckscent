# Vase

`Assets/Prefabs/Props/Decor/Vase.prefab` · [← liste des prefabs](README.md)

Vase à briser (Synty `SM_Prop_Vase_02`, lueur propre), dans les coins des salles. On le brise en avançant dessus : il explose en éclats ; parfois une potion (+2 PV) ou une carte. Voir docs/decor.md.

![Vase](images/Vase.png)

## Hiérarchie

```
Vase  [Breakable, AudioSource, SoundEmitter]
└─ Corps  [DecorFallback]
   ├─ Décor
   │  └─ Pièce
   │     └─ SM_Prop_Vase_02  [MeshFilter, MeshRenderer]
   └─ Secours
      ├─ Panse  [MeshFilter, MeshRenderer]
      └─ Col  [MeshFilter, MeshRenderer]
```

## Contenu

### Vase

- **Breakable**
  - `potionChance` = 0.3 — Chance qu'il cache une potion (PV).
  - `potionHeal` = 2 — PV rendus par la potion.
  - `cardChance` = 0.06 — Chance qu'il cache une carte (si la potion n'est pas sortie et que la main n'est pas pleine).
  - `body` = `Corps` — Ce qui se brise (le modèle) : il disparaît d'un coup, remplacé par des éclats.
  - `fragments` = 12 — Nombre d'éclats quand il explose.
  - `burst` = 4 — Vitesse maximale des éclats (m/s).
  - `debrisTime` = 1.2 — Durée de vie des éclats, avant de disparaître (secondes).
  - `wooden` = non — Coché : du bois (éclats de bois), sinon de la terre cuite (poussière).
  - `sounds` = `Vase` — Ce qui joue le bruit de casse. Vide : cherché sur l'objet.
- **AudioSource**
- **SoundEmitter**
  - `pitchJitter` = 0.08 — Variation de hauteur au hasard, en plus ou en moins (0,08 = ±8 %).

### Vase / Corps

- **DecorFallback**
  - `decor` = `Décor` — La pièce Synty (enfant).
  - `fallback` = `Secours` — La version en formes simples, affichée si le décor n'a pas ses maillages.
  - `forceFallback` = non — Coché : toujours le secours, même si le décor est là (pour comparer).

### Vase / Corps / Décor / Pièce

- **Transform**
  - échelle (1.5, 1.5, 1.5)

### Vase / Corps / Décor / Pièce / SM_Prop_Vase_02

- **MeshFilter**
  - maillage `SM_Prop_Vase_02` (`Assets/Synty/PolygonDungeon/Models/SM_Prop_Vase_02.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonDungeon_Lueur` (Deckscent/Toon)

### Vase / Corps / Secours / Panse

- **Transform**
  - position (0, 0.45, 0)
  - échelle (0.6, 0.75, 0.6)
- **MeshFilter**
  - maillage `Sphere` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Decor` (Deckscent/Toon)

### Vase / Corps / Secours / Col

- **Transform**
  - position (0, 0.9, 0)
  - échelle (0.3, 0.12, 0.3)
- **MeshFilter**
  - maillage `Cylinder` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Toon_Gris_Decor` (Deckscent/Toon)
