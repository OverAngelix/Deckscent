# EcranTitre

`Assets/Prefabs/UI/EcranTitre.prefab` · [← liste des prefabs](README.md)

Écran titre du menu, dessiné en gravure (InkCanvas + TextPool) : le nom dans un cartouche, l'accroche, et les choix en main de cartes agrandies (Descendre, Options, Commandes, Quitter), avec les panneaux Options (son, langue FR / EN) et Commandes. Calques : Fond, une toile par carte (échelle 1,3), Dessus (panneaux, fondu). Construit par *Deckscent > Construire la scène du menu*. Voir docs/menu.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
EcranTitre  [Canvas, CanvasScaler, MenuScreen]
├─ Fond
│  ├─ Encre  [CanvasRenderer, InkCanvas]
│  └─ Textes  [TextPool]
├─ Cartes
│  ├─ Carte 1
│  │  ├─ Encre  [CanvasRenderer, InkCanvas]
│  │  └─ Textes  [TextPool]
│  ├─ Carte 2
│  │  ├─ Encre  [CanvasRenderer, InkCanvas]
│  │  └─ Textes  [TextPool]
│  ├─ Carte 3
│  │  ├─ Encre  [CanvasRenderer, InkCanvas]
│  │  └─ Textes  [TextPool]
│  └─ Carte 4
│     ├─ Encre  [CanvasRenderer, InkCanvas]
│     └─ Textes  [TextPool]
└─ Dessus
   ├─ Encre  [CanvasRenderer, InkCanvas]
   └─ Textes  [TextPool]
```

## Contenu

### EcranTitre

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **MenuScreen**
  - `menu` = (vide) — La scène du menu (caméra, musique). Vide : cherchée dans la scène.
  - `cards` = liste de 4 — Une toile par carte, dans l'ordre Descendre, Options, Commandes, Quitter. Leur échelle agrandit la carte.
  - `navigate` = `UI/Navigate`
  - `submit` = `UI/Submit`
  - `cancel` = `UI/Cancel`
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `accentColor` = (0.85, 0.1, 0.1) — Rouge d'accent : carte choisie, réglage choisi, touches.
  - `vignette` = 0.55 — Assombrit le haut et le bas de l'écran pour détacher le titre et les cartes.
  - `cardScale` = 1.3 — Agrandissement des cartes par rapport à celles de la carte du donjon.

### EcranTitre / Fond / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### EcranTitre / Fond / Textes

- **TextPool**

### EcranTitre / Cartes / Carte 1

- **RectTransform**
  - échelle (1.3, 1.3, 1.3)

### EcranTitre / Cartes / Carte 1 / Encre

- **RectTransform**
  - position (56, -86, 0)
- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### EcranTitre / Cartes / Carte 1 / Textes

- **RectTransform**
  - position (56, -86, 0)
- **TextPool**

### EcranTitre / Cartes / Carte 2

- **RectTransform**
  - échelle (1.3, 1.3, 1.3)

### EcranTitre / Cartes / Carte 2 / Encre

- **RectTransform**
  - position (56, -86, 0)
- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### EcranTitre / Cartes / Carte 2 / Textes

- **RectTransform**
  - position (56, -86, 0)
- **TextPool**

### EcranTitre / Cartes / Carte 3

- **RectTransform**
  - échelle (1.3, 1.3, 1.3)

### EcranTitre / Cartes / Carte 3 / Encre

- **RectTransform**
  - position (56, -86, 0)
- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### EcranTitre / Cartes / Carte 3 / Textes

- **RectTransform**
  - position (56, -86, 0)
- **TextPool**

### EcranTitre / Cartes / Carte 4

- **RectTransform**
  - échelle (1.3, 1.3, 1.3)

### EcranTitre / Cartes / Carte 4 / Encre

- **RectTransform**
  - position (56, -86, 0)
- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### EcranTitre / Cartes / Carte 4 / Textes

- **RectTransform**
  - position (56, -86, 0)
- **TextPool**

### EcranTitre / Dessus / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### EcranTitre / Dessus / Textes

- **TextPool**
