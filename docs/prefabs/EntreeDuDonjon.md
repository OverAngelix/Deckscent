# EntreeDuDonjon

`Assets/Prefabs/Camp/EntreeDuDonjon.prefab` · [← liste des prefabs](README.md)

Entrée du donjon, au nord du camp : arche de rocher (Nature `SM_Rock_CaveEntrance_01`) avec un fond d'encre. Avancer dedans (ou Interact) lance l'expédition (`DungeonEntrance`). Voir docs/camp.md.

![EntreeDuDonjon](images/EntreeDuDonjon.png)

## Hiérarchie

```
EntreeDuDonjon  [DungeonEntrance, AudioSource, SoundEmitter]
├─ Arche
│  └─ Pièce
│     └─ SM_Rock_CaveEntrance_01  [MeshFilter, MeshRenderer]
└─ Fond
   └─ Forme  [MeshFilter, MeshRenderer]
```

## Contenu

### EntreeDuDonjon

- **DungeonEntrance**
  - `blocks` = oui — Coché : bloque la case. Décoché : décor de passage.
  - `sounds` = (vide) — Ce qui joue le raclement de pierre en entrant. Vide : cherché sur l'objet.
- **AudioSource**
- **SoundEmitter**
  - `pitchJitter` = 0.08 — Variation de hauteur au hasard, en plus ou en moins (0,08 = ±8 %).

### EntreeDuDonjon / Arche

- **Transform**
  - position (-0.2, -0.3, 4.4)

### EntreeDuDonjon / Arche / Pièce / SM_Rock_CaveEntrance_01

- **MeshFilter**
  - maillage `SM_Rock_CaveEntrance_01` (`Assets/Synty/PolygonNature/Models/SM_Rock_CaveEntrance_01.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonNature_01` (Deckscent/Toon)

### EntreeDuDonjon / Fond / Forme

- **Transform**
  - position (0, 2.5, 6.5)
  - échelle (6, 6, 0.5)
- **MeshFilter**
  - maillage `Cube` (`Library/unity default resources`)
- **MeshRenderer**
  - matériaux `M_Encre` (Universal Render Pipeline/Unlit)
