# Confirmation

`Assets/Prefabs/UI/Confirmation.prefab` · [← liste des prefabs](README.md)

Demande de confirmation gravée (oui / non), par exemple « Descendre dans le donjon ? » en avançant dans la grotte du camp. Met le jeu en pause ; Échap = non. Construit par *Deckscent > Construire le camp*. Voir docs/camp.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
Confirmation  [Canvas, CanvasScaler, ConfirmScreen]
├─ Encre  [CanvasRenderer, InkCanvas]
└─ Textes  [TextPool]
```

## Contenu

### Confirmation

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **ConfirmScreen**
  - `ink` = `Encre`
  - `texts` = `Textes`
  - `navigate` = `UI/Navigate`
  - `submit` = `UI/Submit`
  - `cancel` = `UI/Cancel`
  - `acceptDelay` = 0.35 — Délai (secondes, temps réel) avant d'accepter un choix : la touche qui a fait avancer ne valide pas à l'aveugle.
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `accentColor` = (0.85, 0.1, 0.1) — Rouge d'accent : le bouton choisi.

### Confirmation / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### Confirmation / Textes

- **TextPool**
