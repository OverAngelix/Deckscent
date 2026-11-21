# Sechoir

`Assets/Prefabs/Camp/Sechoir.prefab` · [← liste des prefabs](README.md)

Séchoir à peaux du camp (Meadow Forest `SM_Prop_Camp_Tanning_Rack_01`). Bloque sa case.

![Sechoir](images/Sechoir.png)

## Hiérarchie

```
Sechoir  [CampObstacle]
└─ Pièce
   └─ SM_Prop_Camp_Tanning_Rack_01  [MeshFilter, MeshRenderer]
```

## Contenu

### Sechoir

- **CampObstacle**
  - `blocks` = oui — Coché : bloque la case. Décoché : décor de passage.

### Sechoir / Pièce / SM_Prop_Camp_Tanning_Rack_01

- **MeshFilter**
  - maillage `SM_Prop_Camp_Tanning_Rack_01` (`Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Models/SM_Prop_Camp_Tanning_Rack_01.fbx`)
- **MeshRenderer**
  - matériaux `M_Toon_PolygonNatureBiomesMeadow_Mat_01` (Deckscent/Toon)
