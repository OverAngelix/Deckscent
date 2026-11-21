# Remontee

`Assets/Prefabs/UI/Remontee.prefab` · [← liste des prefabs](README.md)

Écran de remontée, dessiné en gravure : sur l'escalier du boss du dernier étage d'un sous-biome (tous les 3 étages), le jeu se met en pause et propose deux cartes : Remonter (fin de l'expédition, tout l'or porté va au camp) ou Descendre. Retour : on reste à l'étage. Canvas en surimpression entre l'écran de niveau et la pause. Construit par *Deckscent > Construire l'écran de remontée*. Voir docs/remontee.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
Remontee  [Canvas, CanvasScaler, AscentScreen]
├─ Encre  [CanvasRenderer, InkCanvas]
└─ Textes  [TextPool]
```

## Contenu

### Remontee

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **AscentScreen**
  - `ink` = `Encre`
  - `texts` = `Textes`
  - `navigate` = `UI/Navigate`
  - `submit` = `UI/Submit`
  - `cancel` = `UI/Cancel`
  - `acceptDelay` = 0.4 — Délai (secondes, temps réel) avant d'accepter un choix : la touche qui a fait avancer sur l'escalier ne choisit pas à l'aveugle.
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `accentColor` = (0.85, 0.1, 0.1) — Rouge d'accent : carte choisie.

### Remontee / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### Remontee / Textes

- **TextPool**
